using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Rendering
{
    internal static partial class TrackRenderer
    {
        private const int DetailLevelCount = 3;
        private const float DistantChunkSize = 128;
        private const float FullDetailRadiusPixels = 24;
        private const float MiddleDetailRadiusPixels = 6;
        private const float MergeTangentDotThreshold = -0.995f;
        private static readonly HashSet<Chunk> dirtyChunks = new HashSet<Chunk>();
        private static readonly LinkedList<DetailBuild> detailBuildQueue = new LinkedList<DetailBuild>();

        private struct DetailBuild
        {
            internal Chunk Chunk;
            internal int Level;
            internal IEnumerator<object> Work;
        }

        private static void RequestDetailBuild(PlanetFactory factory, Chunk chunk, int level, bool urgent = false)
        {
            LinkedListNode<DetailBuild> pending = chunk.PendingDetails[level];
            if (pending == null)
            {
                var build = new DetailBuild
                {
                    Chunk = chunk,
                    Level = level,
                    Work = BuildDetailMeshes(factory, chunk, level).GetEnumerator(),
                };
                chunk.PendingDetails[level] = urgent ? detailBuildQueue.AddFirst(build) : detailBuildQueue.AddLast(build);
            }
            else if (urgent && pending != detailBuildQueue.First)
            {
                detailBuildQueue.Remove(pending);
                detailBuildQueue.AddFirst(pending);
            }
        }

        private static void CompleteDetailBuild(LinkedListNode<DetailBuild> pending)
        {
            DetailBuild build = pending.Value;
            build.Work.Dispose();
            build.Chunk.PendingDetails[build.Level] = null;
            detailBuildQueue.Remove(pending);
        }

        private static bool NeedsDetailBuild(Chunk chunk, int level) =>
            chunk.DetailMeshes[level]?.Revision != chunk.Revision;

        internal static void PrewarmStructureMeshes(PlanetFactory factory, Camera camera)
        {
            if (factory?.cargoTraffic == null || camera == null)
            {
                return;
            }

            Initialize(factory);
            foreach (Chunk chunk in dirtyChunks)
            {
                InvalidateDetailMeshes(chunk);
                RequestDetailBuild(factory, chunk, chunk.Key.Distant ? 2 : 1);
            }
            dirtyChunks.Clear();

            while (detailBuildQueue.Count > 0 && OrbitalMeshBuildBudget.HasBuildBudget)
            {
                LinkedListNode<DetailBuild> pending = detailBuildQueue.First;
                long started = Stopwatch.GetTimestamp();
                try
                {
                    if (!pending.Value.Work.MoveNext())
                    {
                        CompleteDetailBuild(pending);
                    }
                }
                catch
                {
                    CompleteDetailBuild(pending);
                    throw;
                }
                finally
                {
                    OrbitalMeshBuildBudget.RecordBuildTime(started);
                }
            }
        }

        private static void ClearChunks()
        {
            foreach (Chunk parent in distantChunks.Values)
            {
                FreeDetailMeshes(parent);
                foreach (Chunk chunk in parent.Children.Values)
                {
                    FreeDetailMeshes(chunk);
                }
            }

            distantChunks.Clear();
            detailBuildQueue.Clear();
            dirtyChunks.Clear();
        }

        private static IEnumerable<Chunk> RenderChunks(Camera camera)
        {
            foreach (Chunk parent in distantChunks.Values)
            {
                var definition = GetDefinition(parent.Key.ItemId);
                if (StructureDetailLevel(camera, WorldBounds(parent, definition, true), definition) == 2)
                {
                    yield return parent;
                }
                else
                {
                    foreach (Chunk chunk in parent.Children.Values)
                    {
                        yield return chunk;
                    }
                }
            }
        }

        private static Bounds WorldBounds(Chunk chunk, TrackDefinition definition, bool usePublishedBounds)
        {
            Vector3 origin = Origin(chunk.Key);
            if (usePublishedBounds && chunk.HasBounds)
            {
                Bounds bounds = chunk.Bounds;
                bounds.center += origin;
                return bounds;
            }

            float span = chunk.Key.Distant ? DistantChunkSize : StructureChunkSize;
            return new Bounds(origin + Vector3.one * span * 0.5f,
                Vector3.one * (span + 8 + definition.CenterHeight + definition.Radius * 2));
        }

        private sealed class StructureLodMeshes
        {
            internal int Revision;
            internal Mesh Wall;
            internal Mesh Core;
        }

        private static void DrawStructureShadows(Camera camera, int itemId)
        {
            foreach (Chunk parent in distantChunks.Values)
            {
                Mesh core = parent.DetailMeshes[2]?.Core;
                if (core == null || (itemId > 0 && parent.Key.ItemId != itemId))
                {
                    continue;
                }

                var surfaces = OrbitalTrackAppearance.GetMaterials(GetDefinition(parent.Key.ItemId));
                SubmitStructureShadow(core, surfaces[(int)TrackSurfaceRole.Fittings], TrackMesh.ShadowSurface,
                    Matrix4x4.Translate(Origin(parent.Key)), camera);
            }
        }

        private static void SubmitStructureShadow(Mesh mesh, Material material, int submesh,
            Matrix4x4 transform, Camera camera)
        {
            if (mesh.GetIndexCount(submesh) > 0)
            {
                Graphics.DrawMesh(mesh, transform, material, 0, camera, submesh, null,
                    ShadowCastingMode.ShadowsOnly, false);
            }
        }

        private static ChunkKey ParentKey(ChunkKey key)
        {
            Vector3 center = Origin(key) + Vector3.one * StructureChunkSize * 0.5f;
            return new ChunkKey(new Vector3Int(Mathf.FloorToInt(center.x / DistantChunkSize),
                Mathf.FloorToInt(center.y / DistantChunkSize), Mathf.FloorToInt(center.z / DistantChunkSize)),
                key.ItemId, key.Group, true);
        }

        private static int StructureDetailLevel(Camera camera, Bounds bounds,
            TrackDefinition dimensions)
        {
            float pixelsPerUnit;
            if (camera.orthographic)
            {
                pixelsPerUnit = camera.pixelHeight / (2 * camera.orthographicSize);
            }
            else
            {
                Vector3 forward = camera.transform.forward;
                Vector3 extents = bounds.extents;
                float halfDepth = Mathf.Abs(forward.x) * extents.x + Mathf.Abs(forward.y) * extents.y +
                    Mathf.Abs(forward.z) * extents.z;
                float depth = Vector3.Dot(bounds.center - camera.transform.position, forward) - halfDepth;
                if (depth <= 0)
                {
                    return 0;
                }

                pixelsPerUnit = camera.pixelHeight / (2 * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
            }

            float projectedRadius = dimensions.Radius * pixelsPerUnit;
            return projectedRadius >= FullDetailRadiusPixels ? 0 : projectedRadius >= MiddleDetailRadiusPixels ? 1 : 2;
        }

        private static void CancelDetailBuilds(Chunk chunk)
        {
            foreach (LinkedListNode<DetailBuild> pending in chunk.PendingDetails)
            {
                if (pending != null)
                {
                    CompleteDetailBuild(pending);
                }
            }
        }

        private static void InvalidateDetailMeshes(Chunk chunk)
        {
            chunk.Revision++;
            chunk.Dirty = true;
            CancelDetailBuilds(chunk);
        }

        private static void DestroyDetailMeshes(StructureLodMeshes meshes)
        {
            UnityEngine.Object.Destroy(meshes.Wall);
            UnityEngine.Object.Destroy(meshes.Core);
        }

        private static void FreeDetailMeshes(Chunk chunk)
        {
            CancelDetailBuilds(chunk);
            foreach (StructureLodMeshes meshes in chunk.DetailMeshes)
            {
                if (meshes != null)
                {
                    DestroyDetailMeshes(meshes);
                }
            }
            Array.Clear(chunk.DetailMeshes, 0, DetailLevelCount);
        }

        private static void PublishDetailMeshes(Chunk chunk, int level, StructureLodMeshes replacement)
        {
            // Keep the previous generation drawable until a complete replacement is available.
            chunk.Dirty = false;
            chunk.HasBounds = false;
            for (int previousLevel = 0; previousLevel < DetailLevelCount; previousLevel++)
            {
                StructureLodMeshes meshes = chunk.DetailMeshes[previousLevel];
                if (meshes != null && (previousLevel == level || meshes.Revision != chunk.Revision))
                {
                    DestroyDetailMeshes(meshes);
                    meshes = null;
                }
                if (previousLevel == level)
                {
                    meshes = replacement;
                }
                chunk.DetailMeshes[previousLevel] = meshes;

                if (meshes != null)
                {
                    Encapsulate(meshes.Wall);
                    Encapsulate(meshes.Core);
                }
            }
            if (chunk.HasBounds)
            {
                chunk.Bounds.Expand(0.2f);
            }

            void Encapsulate(Mesh mesh)
            {
                if (mesh == null)
                {
                    return;
                }
                if (chunk.HasBounds)
                {
                    chunk.Bounds.Encapsulate(mesh.bounds);
                }
                else
                {
                    chunk.Bounds = mesh.bounds;
                    chunk.HasBounds = true;
                }
            }
        }

        private static IEnumerable<object> BuildDetailMeshes(PlanetFactory factory, Chunk chunk, int detailLevel)
        {
            ChunkKey key = chunk.Key;
            int revision = chunk.Revision;
            Vector3 origin = Origin(key);
            var definition = GetDefinition(key.ItemId);
            var walls = definition.HasGlassWall
                ? new TrackMesh(definition.WallRadius, origin, detailLevel) : null;
            var core = new TrackMesh();
            var eligible = new HashSet<int>();
            var entities = chunk.Entities ?? chunk.Children.Values.SelectMany(child => child.Entities);
            foreach (int entityId in entities)
            {
                var shape = GetNodeShape(factory, entityId);
                if (shape.IsThroughNode &&
                    Vector3.Dot((shape.Ends[0] - shape.Center).normalized,
                        (shape.Ends[1] - shape.Center).normalized) < MergeTangentDotThreshold)
                {
                    eligible.Add(entityId);
                }
                else
                {
                    foreach (var step in AppendNodeModel(factory, key, core, walls, entityId, detailLevel))
                    {
                        yield return step;
                    }
                }

                yield return null;
            }

            var visited = new HashSet<int>();
            foreach (int entityId in eligible)
            {
                if (!eligible.Contains(GetNodeShape(factory, entityId).InputNeighbor))
                {
                    foreach (var step in AppendModelChain(factory, key, core, walls, eligible, visited,
                        entityId, detailLevel))
                    {
                        yield return step;
                    }
                }
            }
            // A closed native path has no entry node outside the current chunk.
            foreach (int entityId in eligible)
            {
                foreach (var step in AppendModelChain(factory, key, core, walls, eligible, visited,
                    entityId, detailLevel))
                {
                    yield return step;
                }
            }

            var candidate = new StructureLodMeshes { Revision = revision };
            try
            {
                if (walls != null && walls.VertexCount > 0)
                {
                    candidate.Wall = walls.Build("OrbitalRing.TrackLOD.Wall");
                }
                yield return null;
                if (core.VertexCount > 0)
                {
                    if (key.Distant)
                    {
                        core.AddShadowSurface(!definition.HasGlassWall);
                    }
                    candidate.Core = core.Build("OrbitalRing.TrackLOD.Core");
                }
                yield return null;
                if (revision != chunk.Revision)
                {
                    yield break;
                }
                PublishDetailMeshes(chunk, detailLevel, candidate);
                candidate = null;
            }
            finally
            {
                if (candidate != null)
                {
                    DestroyDetailMeshes(candidate);
                }
            }
        }

        private static IEnumerable<object> AppendModelChain(PlanetFactory factory, ChunkKey key, TrackMesh target,
            TrackMesh walls, HashSet<int> eligible, HashSet<int> visited, int start, int detailLevel)
        {
            if (visited.Contains(start))
            {
                yield break;
            }

            Vector3 origin = Origin(key);
            var samples = new List<TrackSample>();
            NodeShape last = null;
            Vector3 offset = Vector3.zero;
            int entityId = start;
            // Reciprocal native input/output ports provide the path order.
            while (eligible.Contains(entityId) && visited.Add(entityId))
            {
                NodeShape shape = GetNodeShape(factory, entityId);
                offset = factory.entityPool[entityId].pos - origin;
                if (last == null)
                {
                    samples.Add(new TrackSample(shape.Ends[shape.InputEnd] + offset,
                        -shape.EndAxis(shape.InputEnd), shape.EndUp(shape.InputEnd)));
                }
                samples.Add(new TrackSample(shape.Center + offset,
                    (shape.Ends[shape.OutputEnd] - shape.Ends[shape.InputEnd]).normalized, shape.Up));
                last = shape;
                entityId = shape.OutputNeighbor;
            }
            samples.Add(new TrackSample(last.Ends[last.OutputEnd] + offset,
                last.EndAxis(last.OutputEnd), last.EndUp(last.OutputEnd)));

            foreach (var step in AppendPath(factory, key, target, walls, detailLevel, samples))
            {
                yield return step;
            }
        }

        private static IEnumerable<object> AppendNodeModel(PlanetFactory factory, ChunkKey key,
            TrackMesh target, TrackMesh walls, int entityId, int detailLevel)
        {
            NodeShape shape = GetNodeShape(factory, entityId);
            Vector3 offset = factory.entityPool[entityId].pos - Origin(key);
            if (shape.Ends.Count == 2)
            {
                foreach (var step in AppendPath(factory, key, target, walls, detailLevel,
                    new[]
                    {
                        new TrackSample(shape.Ends[0] + offset, -shape.EndAxis(0), shape.EndUp(0)),
                        new TrackSample(shape.Center + offset, (shape.Ends[1] - shape.Ends[0]).normalized, shape.Up),
                        new TrackSample(shape.Ends[1] + offset, shape.EndAxis(1), shape.EndUp(1)),
                    }))
                {
                    yield return step;
                }
            }
            else if (shape.Ends.Count == 0)
            {
                Vector3 direction = shape.Forward.normalized;
                const float halfLength = TrackDefinition.IsolatedHalfLength;
                foreach (var step in AppendPath(factory, key, target, walls, detailLevel,
                    new[]
                    {
                        new TrackSample(shape.Center + offset - direction * halfLength, direction, shape.Up),
                        new TrackSample(shape.Center + offset + direction * halfLength, direction, shape.Up),
                    }, true, true))
                {
                    yield return step;
                }
            }
            else
            {
                for (int end = 0; end < shape.Ends.Count; end++)
                {
                    Vector3 axis = shape.EndAxis(end);
                    foreach (var step in AppendPath(factory, key, target, walls, detailLevel,
                        new[]
                        {
                            new TrackSample(shape.Center + offset, axis, shape.Up),
                            new TrackSample(shape.Ends[end] + offset, axis, shape.EndUp(end)),
                        }, shape.Ends.Count == 1))
                    {
                        yield return step;
                    }
                }
            }
        }

        private static IEnumerable<object> AppendPath(PlanetFactory factory, ChunkKey key, TrackMesh target,
            TrackMesh walls, int detailLevel, IList<TrackSample> samples, bool capStart = false, bool capEnd = false)
        {
            Vector3 origin = Origin(key);
            var phases = new float[samples.Count];
            int capacity = PosTool.GetLayout(factory.planet).StationCapacity;
            for (int index = 0; index < samples.Count; index++)
            {
                float phase = OrbitalTextureLayout.FacilityCoordinate(factory.planet, samples[index].Position + origin);
                if (index > 0)
                {
                    phase += Mathf.Round((phases[index - 1] - phase) / capacity) * capacity;
                }
                phases[index] = phase;
            }

            float textureScale = OrbitalTextureLayout.CoordinatePeriod / capacity;
            walls?.Path(samples, phases, textureScale, capStart, capEnd);
            foreach (var step in TrackModelWarp.Append(target, key.ItemId, detailLevel, samples, phases, textureScale))
            {
                yield return step;
            }
        }
    }
}
