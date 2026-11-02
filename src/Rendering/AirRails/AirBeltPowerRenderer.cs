using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static ProjectOrbitalRing.Rendering.AirBeltAppearance;

namespace ProjectOrbitalRing.Rendering
{
    [HarmonyPatch]
    internal static partial class AirBeltPowerRenderer
    {
        private const float SupportSpacing = 12f;
        private const float CornerAngle = 12f;
        private const float CornerSpacing = 2f;
        private const float GradeBreakAngle = 3f;
        private const float GradeSupportSpacing = 0.65f;
        private const int SupportSlots = 4;
        private const float DirectionTexturePeriod = 80f;
        private const float CellSize = 64f;
        private const float DetailedDistance = 70f;
        private const float VisibleDistance = 150f;
        private const float JunctionClearance = 1.2f;
        private static readonly ConditionalWeakTable<CargoTraffic, PowerState> states =
            new ConditionalWeakTable<CargoTraffic, PowerState>();
        private static readonly AccessTools.FieldRef<CargoTraffic, BeltRenderingBatch[]> renderingBatches =
            AccessTools.FieldRefAccess<CargoTraffic, BeltRenderingBatch[]>("beltRenderingBatch");
        private static readonly Plane[] frustum = new Plane[6];
        private static Material material;
        private static int stateCount;
        private static readonly MaterialPropertyBlock directionProperties = new MaterialPropertyBlock();
        private static readonly int textureTransform = Shader.PropertyToID("_MainTex_ST");

        private enum SupportSurface
        {
            Frame,
            Steel,
            Ceramic,
            Red,
            Indicator,
        }

        private readonly struct Support
        {
            internal readonly Vector3 Position;
            internal readonly Quaternion Rotation;

            internal Support(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            internal Vector3 Point(Vector3 local) => Position + Rotation * local;
        }

        private sealed class SupportGroup
        {
            internal readonly Support[] Entries = new Support[SupportSlots];
            internal int Mask;

            internal void Add(int slot, Support support, float clearance)
            {
                var offset = new Vector3(AirBeltMesh.WireSide, AirBeltMesh.WireHeight, 0);
                Vector3 wire = support.Point(offset);
                for (int previous = 0; previous < SupportSlots; previous++)
                {
                    if ((Mask & (1 << previous)) != 0 &&
                        (Entries[previous].Point(offset) - wire).sqrMagnitude < clearance * clearance)
                    {
                        return;
                    }
                }

                Entries[slot] = support;
                Mask |= 1 << slot;
            }

            internal bool Matches(BeltGeometry previous)
            {
                if (Mask != previous.SupportMask)
                {
                    return false;
                }

                for (int slot = 0; slot < SupportSlots; slot++)
                {
                    if ((Mask & (1 << slot)) == 0)
                    {
                        continue;
                    }

                    Support before = previous.Supports[slot];
                    Support after = Entries[slot];
                    if ((before.Position - after.Position).sqrMagnitude >= 0.00000001f ||
                        Quaternion.Angle(before.Rotation, after.Rotation) >= 0.001f)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private sealed class Junction
        {
            internal readonly Support Start;
            internal readonly Support End;
            internal readonly Vector3 WireEnd;
            internal readonly bool HasWire;

            internal Junction(Support start, Support end, Vector3 wireEnd, bool hasWire)
            {
                Start = start;
                End = end;
                WireEnd = wireEnd;
                HasWire = hasWire;
            }

            internal bool Matches(Junction other) => other != null && HasWire == other.HasWire &&
                Start.Position.Equals(other.Start.Position) && Start.Rotation.Equals(other.Start.Rotation) &&
                End.Position.Equals(other.End.Position) && End.Rotation.Equals(other.End.Rotation) &&
                WireEnd.Equals(other.WireEnd);
        }

        private sealed class BeltGeometry
        {
            internal Vector3Int Cell;
            internal BeltAnchor[] Anchors;
            internal int Speed;
            internal Support[] Supports;
            internal int SupportMask;
            internal Junction Junction;

            internal bool MatchesDirection(BeltAnchor[] source, int first, int count, int speed)
            {
                if (Anchors == null || Speed != speed || Anchors.Length != count)
                {
                    return false;
                }
                for (int index = 0; index < count; index++)
                {
                    BeltAnchor previous = Anchors[index];
                    BeltAnchor next = source[first + index];
                    if (previous.t != next.t || !previous.pos.Equals(next.pos) || !previous.rot.Equals(next.rot))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private sealed class Chunk
        {
            internal readonly HashSet<int> Belts = new HashSet<int>();
            internal Mesh Detailed;
            internal Mesh Distant;
            internal Mesh Directions;
            internal int[] DirectionSpeeds;
            internal Bounds Bounds;

            internal void Free()
            {
                DestroyGeometry(Detailed, Distant, Directions);
                Detailed = null;
                Distant = null;
                Directions = null;
            }
        }

        private sealed class PowerState
        {
            internal readonly Dictionary<int, BeltGeometry> Belts = new Dictionary<int, BeltGeometry>();
            internal readonly SupportGroup SupportScratch = new SupportGroup();
            internal readonly Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();
            internal readonly HashSet<int> DirtyBelts = new HashSet<int>();
            internal readonly Queue<int> BeltQueue = new Queue<int>();
            internal readonly HashSet<Vector3Int> DirtyCells = new HashSet<Vector3Int>();
            internal readonly Queue<Vector3Int> CellQueue = new Queue<Vector3Int>();

            internal void Dirty(int beltId)
            {
                if (DirtyBelts.Add(beltId))
                {
                    BeltQueue.Enqueue(beltId);
                }
            }

            internal void Dirty(Vector3Int cell)
            {
                if (DirtyCells.Add(cell))
                {
                    CellQueue.Enqueue(cell);
                }
            }
        }

        private static PowerState CreateState(CargoTraffic traffic)
        {
            stateCount++;
            return new PowerState();
        }

        private static bool IsBelt(CargoTraffic traffic, int beltId) =>
            traffic?.beltPool != null && beltId > 0 && beltId < traffic.beltPool.Length &&
            traffic.beltPool[beltId].id == beltId;

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltRenderer))]
        private static void Changed(CargoTraffic __instance, int beltId)
        {
            if (IsAirBelt(__instance, beltId))
            {
                states.GetValue(__instance, CreateState);
            }

            if (states.TryGetValue(__instance, out var state))
            {
                DirtyConnected(__instance, state, beltId);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltRenderer))]
        private static void Removed(CargoTraffic __instance, int beltId)
        {
            if (states.TryGetValue(__instance, out var state))
            {
                DirtyConnected(__instance, state, beltId);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RefreshPathUV))]
        private static void PathCoordinatesChanged(CargoTraffic __instance, int pathId)
        {
            CargoPath path = __instance.GetCargoPath(pathId);
            if (path == null)
            {
                return;
            }

            if (states.TryGetValue(__instance, out PowerState state))
            {
                foreach (int beltId in path.belts)
                {
                    DirtyNeighbor(__instance, state, beltId);
                }
            }
        }

        private static void DirtyConnected(CargoTraffic traffic, PowerState state, int beltId)
        {
            if (beltId <= 0 || beltId >= traffic.beltPool.Length)
            {
                return;
            }

            BeltComponent belt = traffic.beltPool[beltId];
            DirtyNeighbor(traffic, state, beltId);
            DirtyNeighbor(traffic, state, belt.outputId);
            DirtyNeighbor(traffic, state, belt.backInputId);
            DirtyNeighbor(traffic, state, belt.leftInputId);
            DirtyNeighbor(traffic, state, belt.rightInputId);
            DirtyCornerDependencies(traffic, state, belt.outputId);
            DirtyCornerDependencies(traffic, state, belt.backInputId);
            DirtyCornerDependencies(traffic, state, belt.leftInputId);
            DirtyCornerDependencies(traffic, state, belt.rightInputId);
        }

        private static void DirtyCornerDependencies(CargoTraffic traffic, PowerState state, int beltId)
        {
            if (beltId <= 0 || beltId >= traffic.beltPool.Length ||
                traffic.beltPool[beltId].id != beltId)
            {
                return;
            }

            // Neighbor peak selection also reads the grade window of each adjacent belt.
            BeltComponent belt = traffic.beltPool[beltId];
            DirtyNeighbor(traffic, state, belt.mainInputId);
            DirtyNeighbor(traffic, state, belt.outputId);
        }

        private static void DirtyNeighbor(CargoTraffic traffic, PowerState state, int beltId)
        {
            if (IsAirBelt(traffic, beltId) || state.Belts.ContainsKey(beltId) ||
                IsBelt(traffic, beltId) && IsAirBelt(traffic, traffic.beltPool[beltId].outputId))
            {
                state.Dirty(beltId);
            }
        }

        private static void RefreshBelt(CargoTraffic traffic, PowerState state, int beltId)
        {
            state.Belts.TryGetValue(beltId, out BeltGeometry previous);
            SupportGroup supports = state.SupportScratch;
            supports.Mask = 0;
            BeltAnchor[] anchors = null;
            int speed = 0;
            if (TryAnchorSpan(traffic, beltId, out BeltRenderingBatch batch, out int first, out int last))
            {
                BeltComponent belt = traffic.beltPool[beltId];
                CargoPath path = traffic.GetCargoPath(belt.segPathId);
                if (path != null)
                {
                    int count = last - first + 1;
                    speed = belt.speed;
                    if (previous != null && previous.MatchesDirection(batch.nodePool, first, count, speed))
                    {
                        anchors = previous.Anchors;
                    }
                    else
                    {
                        anchors = new BeltAnchor[count];
                        Array.Copy(batch.nodePool, first, anchors, 0, count);
                    }

                    FindSupports(traffic, belt, path, supports, batch, first, last);
                }
            }

            Junction junction = FindJunction(traffic, beltId);
            if (previous != null && anchors == previous.Anchors && supports.Matches(previous) &&
                (junction == previous.Junction || junction?.Matches(previous.Junction) == true))
            {
                return;
            }

            if (previous != null)
            {
                state.Chunks[previous.Cell].Belts.Remove(beltId);
                state.Dirty(previous.Cell);
            }
            if (anchors == null && junction == null)
            {
                state.Belts.Remove(beltId);
                return;
            }

            // The belt and its attachments share a cell. Culling uses their combined mesh bounds.
            Vector3 position = anchors != null ? anchors[(anchors.Length - 1) / 2].pos : junction.Start.Position;
            var cell = new Vector3Int(Mathf.FloorToInt(position.x / CellSize),
                Mathf.FloorToInt(position.y / CellSize), Mathf.FloorToInt(position.z / CellSize));
            if (!state.Chunks.TryGetValue(cell, out Chunk chunk))
            {
                chunk = new Chunk();
                state.Chunks.Add(cell, chunk);
            }

            state.Belts[beltId] = new BeltGeometry
            {
                Cell = cell,
                Anchors = anchors,
                Speed = speed,
                Supports = supports.Mask == 0 ? null : (Support[])supports.Entries.Clone(),
                SupportMask = supports.Mask,
                Junction = junction,
            };
            chunk.Belts.Add(beltId);
            state.Dirty(cell);
        }

        private static void DestroyGeometry(Mesh detailed, Mesh distant, Mesh directions)
        {
            Object.Destroy(detailed);
            if (!ReferenceEquals(detailed, distant))
            {
                Object.Destroy(distant);
            }
            Object.Destroy(directions);
        }

        private static void RefreshChunk(PowerState state, Vector3Int cell)
        {
            Chunk chunk = state.Chunks[cell];
            if (chunk.Belts.Count == 0)
            {
                chunk.Free();
                state.Chunks.Remove(cell);
                return;
            }

            var junctionGeometry = new Geometry();
            var directionGeometry = new Geometry();
            var instances = new List<CombineInstance>();
            foreach (int beltId in chunk.Belts)
            {
                BeltGeometry belt = state.Belts[beltId];
                if (belt.Junction != null)
                {
                    junctionGeometry.AddJunction(belt.Junction);
                }
                if (belt.Anchors != null)
                {
                    directionGeometry.AddDirectionLane(belt.Anchors, belt.Speed);
                }
                for (int slot = 0; slot < SupportSlots; slot++)
                {
                    if ((belt.SupportMask & (1 << slot)) != 0)
                    {
                        Support support = belt.Supports[slot];
                        instances.Add(new CombineInstance
                        {
                            transform = Matrix4x4.TRS(support.Position, support.Rotation, Vector3.one),
                        });
                    }
                }
            }

            Mesh junctions = junctionGeometry.BuildJunctions();
            Mesh detailed = null;
            Mesh distant = null;
            Mesh directions = null;
            try
            {
                int firstSupport = junctions != null ? 1 : 0;
                if (junctions != null)
                {
                    instances.Insert(0, new CombineInstance { mesh = junctions, transform = Matrix4x4.identity });
                }
                detailed = distant = junctions;
                if (instances.Count > firstSupport)
                {
                    CombineInstance[] parts = instances.ToArray();
                    detailed = CombineSupports(parts, firstSupport, "air-support-lod0", "AirBelt.Power.Detail");
                    distant = CombineSupports(parts, firstSupport, "air-support-lod1", "AirBelt.Power.Distant");
                }
                directions = directionGeometry.BuildDirections(out int[] speeds);

                Bounds bounds = detailed != null ? detailed.bounds : directions.bounds;
                if (directions != null)
                {
                    bounds.Encapsulate(directions.bounds);
                    directions.UploadMeshData(true);
                }
                detailed?.UploadMeshData(true);
                if (!ReferenceEquals(detailed, distant))
                {
                    distant.UploadMeshData(true);
                }

                chunk.Free();
                chunk.Detailed = detailed;
                chunk.Distant = distant;
                chunk.Directions = directions;
                chunk.DirectionSpeeds = speeds;
                chunk.Bounds = bounds;
            }
            catch
            {
                DestroyGeometry(detailed, distant, directions);
                throw;
            }
            finally
            {
                if (!ReferenceEquals(junctions, detailed) && !ReferenceEquals(junctions, distant))
                {
                    Object.Destroy(junctions);
                }
            }
        }

        private static Mesh CombineSupports(CombineInstance[] parts, int firstSupport, string model, string name)
        {
            Mesh support = TrackModelAssets.Decoration(model);
            for (int index = firstSupport; index < parts.Length; index++)
            {
                parts[index].mesh = support;
            }
            return OrbitalMeshBuffers.Combine(name, parts);
        }

        private static void Refresh(CargoTraffic traffic, PowerState state)
        {
            while (state.BeltQueue.Count > 0 && OrbitalMeshBuildBudget.HasBuildBudget)
            {
                long started = Stopwatch.GetTimestamp();
                int beltId = state.BeltQueue.Dequeue();
                state.DirtyBelts.Remove(beltId);
                try
                {
                    RefreshBelt(traffic, state, beltId);
                }
                finally
                {
                    OrbitalMeshBuildBudget.RecordBuildTime(started);
                }
            }

            while (state.CellQueue.Count > 0 && OrbitalMeshBuildBudget.HasBuildBudget)
            {
                long started = Stopwatch.GetTimestamp();
                Vector3Int cell = state.CellQueue.Dequeue();
                state.DirtyCells.Remove(cell);
                try
                {
                    RefreshChunk(state, cell);
                }
                finally
                {
                    OrbitalMeshBuildBudget.RecordBuildTime(started);
                }
            }
        }

        private static Material SupportMaterial
        {
            get
            {
                if (material == null)
                {
                    material = TrackModelAssets.CreateMaterial("air-support");
                }
                return material;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.Draw))]
        private static void Draw(CargoTraffic __instance, Camera _cam)
        {
            if (_cam == null || !states.TryGetValue(__instance, out var state))
            {
                return;
            }

            Refresh(__instance, state);
            if (state.Chunks.Count == 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(_cam, frustum);
            Vector3 cameraPosition = _cam.transform.position;
            foreach (Chunk chunk in state.Chunks.Values)
            {
                if (chunk.DirectionSpeeds == null || !GeometryUtility.TestPlanesAABB(frustum, chunk.Bounds))
                {
                    continue;
                }

                float distanceSquared = chunk.Bounds.SqrDistance(cameraPosition);
                if (distanceSquared > VisibleDistance * VisibleDistance)
                {
                    continue;
                }

                Mesh mesh = distanceSquared <= DetailedDistance * DetailedDistance ? chunk.Detailed : chunk.Distant;
                if (mesh != null)
                {
                    Graphics.DrawMesh(mesh, Matrix4x4.identity, SupportMaterial, 0, _cam, 0,
                        TrackLighting.SteelLighting(), ShadowCastingMode.Off, false);
                }
                for (int index = 0; index < chunk.DirectionSpeeds.Length; index++)
                {
                    double phase = GameMain.gameTick * (double)chunk.DirectionSpeeds[index] / DirectionTexturePeriod;
                    directionProperties.SetVector(textureTransform, new Vector4(1, 1, 0, (float)(phase % 1)));
                    Graphics.DrawMesh(chunk.Directions, Matrix4x4.identity, AirBeltMesh.DirectionMaterial,
                        0, _cam, index, directionProperties, ShadowCastingMode.Off, false);
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.DestroyRenderingBatches))]
        private static void Free(CargoTraffic __instance)
        {
            if (!states.TryGetValue(__instance, out var state))
            {
                return;
            }

            foreach (Chunk chunk in state.Chunks.Values)
            {
                chunk.Free();
            }

            states.Remove(__instance);
            stateCount--;
            if (stateCount == 0)
            {
                UnityEngine.Object.Destroy(material);
                AirBeltMesh.ReleaseDirectionMaterial();
                material = null;
            }
        }
    }
}
