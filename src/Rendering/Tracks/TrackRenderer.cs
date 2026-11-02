using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Rendering
{
    internal static partial class TrackRenderer
    {
        private const float StructureChunkSize = 64;
        private static readonly Dictionary<ChunkKey, Chunk> distantChunks = new Dictionary<ChunkKey, Chunk>();
        private static readonly Dictionary<int, NodeShape> geometry = new Dictionary<int, NodeShape>();
        private static readonly object changesLock = new object();
        private static HashSet<int> changes = new HashSet<int>();
        private static HashSet<int> processingChanges = new HashSet<int>();
        private static readonly HashSet<int> changedNodes = new HashSet<int>();
        private static readonly Dictionary<EffectKey, EffectState> effectStates =
            new Dictionary<EffectKey, EffectState>();
        private static PlanetFactory current;

        internal static bool IsRegistered(int itemId) => GetDefinition(itemId) != null;

        internal static TrackDefinition GetDefinition(int itemId) => TrackDefinition.Get(itemId);

        private readonly struct ChunkKey : IEquatable<ChunkKey>
        {
            internal readonly Vector3Int Cell;
            internal readonly int ItemId;
            internal readonly int Group;
            internal readonly bool Distant;

            internal ChunkKey(Vector3Int cell, int itemId, int group, bool distant = false)
            {
                Cell = cell;
                ItemId = itemId;
                Group = group;
                Distant = distant;
            }

            public bool Equals(ChunkKey other) =>
                Cell == other.Cell && ItemId == other.ItemId && Group == other.Group && Distant == other.Distant;

            public override bool Equals(object other) => other is ChunkKey key && Equals(key);

            public override int GetHashCode() =>
                unchecked(((Cell.GetHashCode() * 397 ^ ItemId) * 397 ^ Group) * 397 ^ Distant.GetHashCode());
        }

        private readonly struct EffectKey : IEquatable<EffectKey>
        {
            private readonly int itemId;
            private readonly int group;
            private readonly TrackSurfaceRole role;

            internal EffectKey(int itemId, int group, TrackSurfaceRole role)
            {
                this.itemId = itemId;
                this.group = group;
                this.role = role;
            }

            public bool Equals(EffectKey other) => itemId == other.itemId && group == other.group && role == other.role;

            public override bool Equals(object other) => other is EffectKey key && Equals(key);

            public override int GetHashCode() => unchecked((itemId * 397 ^ group) * 397 ^ (int)role);
        }

        private sealed class EffectState
        {
            // Ring effects share one activation state and animation phase across chunks in the same group.
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            internal int Frame = -1;
        }

        private sealed class Chunk
        {
            internal readonly ChunkKey Key;
            internal readonly Dictionary<ChunkKey, Chunk> Children;
            internal readonly HashSet<int> Entities;
            internal int Revision;
            internal readonly StructureLodMeshes[] DetailMeshes = new StructureLodMeshes[DetailLevelCount];
            internal readonly LinkedListNode<DetailBuild>[] PendingDetails =
                new LinkedListNode<DetailBuild>[DetailLevelCount];
            internal bool Dirty = true;
            internal Bounds Bounds;
            internal bool HasBounds;

            internal Chunk(ChunkKey key)
            {
                Key = key;
                if (key.Distant)
                {
                    Children = new Dictionary<ChunkKey, Chunk>();
                }
                else
                {
                    Entities = new HashSet<int>();
                }
            }
        }

        private sealed class NodeShape
        {
            internal ChunkKey Key;
            internal int ItemId;
            internal Vector3 Position;
            internal Vector3 Center;
            internal Vector3 Up;
            internal Vector3 Forward;
            internal readonly List<Vector3> Ends = new List<Vector3>(4);
            internal readonly List<int> Neighbors = new List<int>(4);
            internal int InputEnd;
            internal int OutputEnd;
            internal bool IsThroughNode => Ends.Count == 2 && InputEnd >= 0 && OutputEnd >= 0;
            internal int InputNeighbor => Neighbors[InputEnd];
            internal int OutputNeighbor => Neighbors[OutputEnd];
            internal Vector3 EndAxis(int end) => (Ends[end] - Center).normalized;
            internal Vector3 EndUp(int end) => (Position + Ends[end]).normalized;

            internal bool Matches(NodeShape other)
            {
                if (other == null || ItemId != other.ItemId || !Position.Equals(other.Position) ||
                    !Center.Equals(other.Center) ||
                    !Up.Equals(other.Up) || InputEnd != other.InputEnd || OutputEnd != other.OutputEnd ||
                    Ends.Count != other.Ends.Count ||
                    (Ends.Count == 0 && !Forward.Equals(other.Forward)))
                {
                    return false;
                }

                for (int index = 0; index < Ends.Count; index++)
                {
                    if (!Ends[index].Equals(other.Ends[index]) || Neighbors[index] != other.Neighbors[index])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static Vector3 Origin(ChunkKey key) =>
            new Vector3(key.Cell.x, key.Cell.y, key.Cell.z) * (key.Distant ? DistantChunkSize : StructureChunkSize);

        private static ChunkKey Key(PlanetFactory factory, int entityId)
        {
            var entity = factory.entityPool[entityId];
            Vector3 position = entity.pos;
            return new ChunkKey(new Vector3Int(
                Mathf.FloorToInt(position.x / StructureChunkSize), Mathf.FloorToInt(position.y / StructureChunkSize),
                Mathf.FloorToInt(position.z / StructureChunkSize)), entity.protoId,
                OrbitalTrackVisuals.BatchGroup(factory, entityId));
        }

        internal static void Dirty(PlanetFactory factory, int entityId)
        {
            if (factory == null || entityId <= 0)
            {
                return;
            }

            lock (changesLock)
            {
                if (factory == current)
                {
                    changes.Add(entityId);
                }
            }
        }

        private static void FlushChanges()
        {
            lock (changesLock)
            {
                if (changes.Count == 0)
                {
                    return;
                }
                (changes, processingChanges) = (processingChanges, changes);
            }

            changedNodes.Clear();
            try
            {
                foreach (int entityId in processingChanges)
                {
                    changedNodes.Add(entityId);
                    // A connection change can affect both former and current neighbors.
                    if (geometry.TryGetValue(entityId, out var cached))
                    {
                        changedNodes.UnionWith(cached.Neighbors);
                    }

                    for (int slot = 0; slot < 4; slot++)
                    {
                        if (TrackTopology.TryReadLink(current, entityId, slot, out _, out int neighbor))
                        {
                            changedNodes.Add(neighbor);
                        }
                    }
                }

                foreach (int entityId in changedNodes)
                {
                    OrbitalConstructionOrientation.AlignIsolated(current, entityId);
                    Mark(entityId);
                }
            }
            finally
            {
                processingChanges.Clear();
            }
        }

        private static void Mark(int entityId)
        {
            geometry.TryGetValue(entityId, out NodeShape previous);

            if (!TrackTopology.IsNode(current, entityId))
            {
                if (previous != null)
                {
                    geometry.Remove(entityId);
                    RemoveChunkEntity(previous.Key, entityId);
                    if (OrbitalFacilityAttachments.HasBay(previous.ItemId))
                    {
                        OrbitalFacilityAttachments.InvalidateTracks(current);
                    }
                }
                return;
            }

            NodeShape shape = CreateShape(current, entityId);
            ChunkKey key = shape.Key;
            if (previous != null)
            {
                if (!previous.Key.Equals(key))
                {
                    RemoveChunkEntity(previous.Key, entityId);
                }
                else if (shape.Matches(previous))
                {
                    return;
                }
            }
            geometry[entityId] = shape;

            if ((OrbitalFacilityAttachments.HasBay(shape.ItemId) ||
                 (previous != null && OrbitalFacilityAttachments.HasBay(previous.ItemId))) &&
                (previous == null || shape.ItemId != previous.ItemId || !shape.Position.Equals(previous.Position)))
            {
                OrbitalFacilityAttachments.InvalidateTracks(current);
            }

            ChunkKey parentKey = ParentKey(key);
            if (!distantChunks.TryGetValue(parentKey, out Chunk parent))
            {
                distantChunks.Add(parentKey, parent = new Chunk(parentKey));
            }
            if (!parent.Children.TryGetValue(key, out Chunk chunk))
            {
                parent.Children.Add(key, chunk = new Chunk(key));
            }

            chunk.Entities.Add(entityId);
            dirtyChunks.Add(chunk);
            dirtyChunks.Add(parent);
        }

        private static void RemoveChunkEntity(ChunkKey key, int entityId)
        {
            ChunkKey parentKey = ParentKey(key);
            Chunk parent = distantChunks[parentKey];
            Chunk chunk = parent.Children[key];
            chunk.Entities.Remove(entityId);
            if (chunk.Entities.Count > 0)
            {
                dirtyChunks.Add(chunk);
            }
            else
            {
                FreeDetailMeshes(chunk);
                parent.Children.Remove(key);
                dirtyChunks.Remove(chunk);
            }

            if (parent.Children.Count > 0)
            {
                dirtyChunks.Add(parent);
            }
            else
            {
                FreeDetailMeshes(parent);
                distantChunks.Remove(parentKey);
                dirtyChunks.Remove(parent);
            }
        }

        private static void Initialize(PlanetFactory factory)
        {
            if (current == factory)
            {
                return;
            }

            FreeMeshes();
            lock (changesLock)
            {
                current = factory;
            }
            if (factory?.cargoTraffic == null)
            {
                return;
            }

            for (int beltId = 1; beltId < factory.cargoTraffic.beltCursor; beltId++)
            {
                if (factory.cargoTraffic.beltPool[beltId].id != beltId)
                {
                    continue;
                }

                int entityId = factory.cargoTraffic.beltPool[beltId].entityId;
                if (TrackTopology.IsNode(factory, entityId))
                {
                    OrbitalConstructionOrientation.AlignIsolated(factory, entityId);
                    factory.cargoTraffic.RemoveBeltRenderer(beltId);
                    Mark(entityId);
                }
            }

            for (int pathId = 1; pathId < factory.cargoTraffic.pathCursor; pathId++)
            {
                if (TrackRenderPatches.IsTrackPath(factory.cargoTraffic, pathId))
                {
                    factory.cargoTraffic.RemovePathRenderer(pathId);
                }
            }
        }

        private static NodeShape GetNodeShape(PlanetFactory factory, int entityId)
        {
            if (!geometry.TryGetValue(entityId, out NodeShape shape) ||
                shape.ItemId != factory.entityPool[entityId].protoId)
            {
                shape = CreateShape(factory, entityId);
            }
            return shape;
        }

        private static NodeShape CreateShape(PlanetFactory factory, int entityId)
        {
            var entity = factory.entityPool[entityId];
            var definition = GetDefinition(entity.protoId);
            Vector3 center = entity.pos + entity.pos.normalized * definition.CenterHeight;
            var shape = new NodeShape
            {
                Key = Key(factory, entityId),
                ItemId = entity.protoId,
                Position = entity.pos,
                Center = center - entity.pos,
                Up = entity.pos.normalized,
                Forward = entity.rot * Vector3.forward,
                InputEnd = -1,
                OutputEnd = -1,
            };
            for (int slot = 0; slot < 4; slot++)
            {
                if (!TrackTopology.TryReadLink(factory, entityId, slot, out bool output, out int other) ||
                    shape.Neighbors.Contains(other))
                {
                    continue;
                }

                var peer = factory.entityPool[other];
                Vector3 peerCenter = peer.pos + peer.pos.normalized * definition.CenterHeight;
                Vector3 end = (center + peerCenter) * 0.5f;
                if ((end - center).sqrMagnitude < 0.00001f)
                {
                    continue;
                }

                shape.Neighbors.Add(other);
                shape.Ends.Add(end - entity.pos);
                if (output)
                {
                    shape.OutputEnd = shape.Ends.Count - 1;
                }
                else
                {
                    shape.InputEnd = shape.Ends.Count - 1;
                }
            }

            return shape;
        }

        internal static void Update(PlanetFactory factory, Camera camera)
        {
            FlushChanges();
            PrewarmStructureMeshes(factory, camera);
        }

        internal static int Draw(PlanetFactory factory, Camera camera, int itemId = 0)
        {
            if (factory?.cargoTraffic == null || camera == null)
            {
                return 0;
            }

            var model = factory.planet?.factoryModel;
            if (model == null || model.disableFactoryEntities ||
                model.gpuiManager == null || !model.gpuiManager.renderEntity)
            {
                return 0;
            }

            Initialize(factory);
            FlushChanges();
            OrbitalRenderBounds.Prepare(camera);
            int drawn = 0;
            foreach (Chunk chunk in RenderChunks(camera))
            {
                ChunkKey key = chunk.Key;
                if (itemId > 0 && key.ItemId != itemId)
                {
                    continue;
                }

                var definition = GetDefinition(key.ItemId);
                Vector3 origin = Origin(key);
                Bounds bounds = WorldBounds(chunk, definition, !chunk.Dirty);
                if (!OrbitalRenderBounds.IsVisible(camera, bounds, factory.planet.realRadius - 2))
                {
                    continue;
                }

                int level = key.Distant ? 2 : StructureDetailLevel(camera, bounds, definition);
                if (NeedsDetailBuild(chunk, level))
                {
                    RequestDetailBuild(factory, chunk, level, true);
                }

                var details = chunk.DetailMeshes;
                StructureLodMeshes meshes = details[level] ?? details[1] ?? details[0] ?? details[2];
                if (meshes == null || (meshes.Wall == null && meshes.Core == null))
                {
                    continue;
                }

                var surfaces = OrbitalTrackAppearance.GetMaterials(definition);
                Matrix4x4 transform = Matrix4x4.Translate(origin);
                if (meshes.Wall != null)
                {
                    DrawSurface(factory, camera, key, surfaces, meshes.Wall, transform,
                        2, TrackSurfaceRole.InnerWall);
                    DrawSurface(factory, camera, key, surfaces, meshes.Wall, transform,
                        0, TrackSurfaceRole.Wall);
                }

                if (meshes.Core != null)
                {
                    DrawSurface(factory, camera, key, surfaces, meshes.Core, transform,
                        0, TrackSurfaceRole.Core);
                    DrawSurface(factory, camera, key, surfaces, meshes.Core, transform,
                        1, TrackSurfaceRole.Fittings);
                    DrawSurface(factory, camera, key, surfaces, meshes.Core, transform,
                        4, TrackSurfaceRole.Detail);
                }

                drawn++;
            }

            DrawStructureShadows(camera, itemId);
            return drawn;
        }

        private static void DrawSurface(PlanetFactory factory, Camera camera, ChunkKey key,
            Material[] surfaces, Mesh mesh, Matrix4x4 transform,
            int submesh, TrackSurfaceRole role)
        {
            if (submesh >= mesh.subMeshCount || mesh.GetIndexCount(submesh) == 0)
            {
                return;
            }

            Material material = surfaces[(int)role];
            if (material == null)
            {
                return;
            }

            var definition = GetDefinition(key.ItemId);
            var effectKey = new EffectKey(key.ItemId, key.Group, role);
            if (!effectStates.TryGetValue(effectKey, out var state))
            {
                effectStates.Add(effectKey, state = new EffectState());
            }

            if (state.Frame != Time.frameCount)
            {
                state.Properties.Clear();
                TrackLighting.Apply(state.Properties);
                OrbitalTrackVisuals.ApplyEffects(factory, definition, key.Group, role, state.Properties);
                state.Frame = Time.frameCount;
            }

            Graphics.DrawMesh(mesh, transform, material, 0, camera, submesh,
                state.Properties, ShadowCastingMode.Off, false);
        }

        private static void FreeMeshes()
        {
            ClearChunks();
            effectStates.Clear();
            geometry.Clear();
            lock (changesLock)
            {
                changes.Clear();
                current = null;
            }
        }

        internal static void Reset()
        {
            FreeMeshes();
            changedNodes.Clear();
        }

        internal static void Dispose()
        {
            Reset();
            TrackModelAssets.Dispose();
            TrackLighting.Dispose();
        }
    }
}
