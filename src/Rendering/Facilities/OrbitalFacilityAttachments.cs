using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalFacilityAttachments
    {
        private const float DistantOrthographicSize = 120;
        private const float DistantCameraAltitude = 160;

        internal enum Surface
        {
            Armor,
            Recess,
            Metal,
            ServiceLight,
            BeamLight,
            PowerLight,
            Ceramic,
            Conductor,
            BeamGuide0,
            BeamGuide1,
            BeamGuide2,
            PowerPanel,
            ConnectedLight,
            UnavailableLight,
        }

        private readonly struct SlotState
        {
            internal readonly int Ring;
            internal readonly int Slot;
            internal readonly EntityData Facility;
            internal readonly int Tracks;
            internal readonly bool Available;

            internal SlotState(PlanetFactory factory, int ringIndex, int slot, EquatorRing ring, int functionalTracks)
            {
                Ring = ringIndex;
                Slot = slot;
                Facility = factory.entityPool[ResolveEntity(factory, ring, slot)];
                Available = OrbitalTextureLayout.IsFacilitySlotAvailable(factory.planet, ringIndex, slot);
                Tracks = ((ring.isInsideRingComplete ? 1 : 0) | (ring.isOutsideRingComplete ? 2 : 0)
                    | (Facility.id > 0 ? functionalTracks : 0)) & RemodelSettings.AttachmentTracks;
            }

            internal bool Matches(SlotState other)
            {
                return Ring == other.Ring && Slot == other.Slot && Tracks == other.Tracks
                    && Available == other.Available && Facility.id == other.Facility.id
                    && (Facility.id == 0 || (Facility.protoId == other.Facility.protoId
                        && Facility.modelIndex == other.Facility.modelIndex
                        && Facility.pos == other.Facility.pos && Facility.rot == other.Facility.rot));
            }
        }

        private struct SurfaceMeshes
        {
            internal Mesh Combined;
            internal List<Mesh> Chunks;
        }

        private sealed class Region
        {
            internal readonly List<SlotState> Slots;
            internal readonly Mesh[] Near = new Mesh[SurfaceCount];
            internal readonly Mesh[] Distant = new Mesh[SurfaceCount];

            internal Region(List<SlotState> slots)
            {
                Slots = slots;
            }
        }

        private sealed class AttachmentBuild
        {
            internal PlanetFactory Factory;
            internal SlotState[] Slots;
            internal Dictionary<int, Region> Regions;
            internal readonly SurfaceMeshes[] Near = new SurfaceMeshes[SurfaceCount];
            internal readonly SurfaceMeshes[] Distant = new SurfaceMeshes[SurfaceCount];
            internal IEnumerator<object> Work;
        }

        private const int SurfaceCount = (int)Surface.UnavailableLight + 1;
        private static PlanetFactory observedFactory;
        private static readonly List<SlotState> pending = new List<SlotState>();
        private static readonly List<Mesh> drawMeshes = new List<Mesh>();
        private static readonly Material[] materials = new Material[SurfaceCount];
        private static Camera detailCamera;
        private static bool useDistantGeometry;
        private static int[] localTrackCounts;
        private static readonly MaterialPropertyBlock lighting = new MaterialPropertyBlock();
        private static float nextProbe;
        private static AttachmentBuild build;
        private static AttachmentBuild published;

        internal static void Update()
        {
            if (!RemodelSettings.FacilityAttachments || !RemodelSettings.AnyTrack)
            {
                return;
            }

            PlanetFactory factory = GameMain.isRunning ? GameMain.localPlanet?.factory : null;
            if (factory == null)
            {
                if (observedFactory != null)
                {
                    Clear();
                }
                return;
            }
            if (!ReferenceEquals(factory, observedFactory))
            {
                Clear();
                observedFactory = factory;
            }

            if (Time.unscaledTime >= nextProbe)
            {
                ObserveSlots(factory);
            }

            AdvanceBuild(factory);
        }

        private static void ObserveSlots(PlanetFactory factory)
        {
            nextProbe = Time.unscaledTime + 0.25f;
            PlanetOrbitalRingData data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
            pending.Clear();
            if (data != null)
            {
                var layout = GetLayout(factory.planet);
                if (localTrackCounts == null)
                {
                    IndexLocalTracks(factory, layout);
                }
                for (int ringIndex = 0; ringIndex < data.Rings.Count; ringIndex++)
                {
                    EquatorRing ring = data.Rings[ringIndex];
                    for (int slot = 0; slot < ring.Capacity; slot++)
                    {
                        int index = (ringIndex * layout.StationCapacity + slot) * 2;
                        int presence = index < localTrackCounts.Length
                            ? (localTrackCounts[index] > 0 ? 4 : 0) | (localTrackCounts[index + 1] > 0 ? 8 : 0) : 0;
                        pending.Add(new SlotState(factory, ringIndex, slot, ring, presence));
                    }
                }
            }

            if (SlotsMatch(pending, (build ?? published)?.Slots ?? Array.Empty<SlotState>()))
            {
                return;
            }
            CancelBuild();
            var candidate = new AttachmentBuild
            {
                Factory = factory,
                Slots = pending.ToArray(),
                Regions = GroupSlots(factory, pending, published),
            };
            candidate.Work = Rebuild(candidate).GetEnumerator();
            build = candidate;
        }

        private static void AdvanceBuild(PlanetFactory factory)
        {
            while (build != null && OrbitalMeshBuildBudget.HasBuildBudget)
            {
                var candidate = build;
                long started = Stopwatch.GetTimestamp();
                try
                {
                    if (candidate.Work.MoveNext())
                    {
                        continue;
                    }

                    // The snapshot is published only if its slots still match the current factory.
                    ObserveSlots(factory);
                    if (ReferenceEquals(build, candidate))
                    {
                        candidate.Work.Dispose();
                        candidate.Work = null;
                        Release(published, candidate);
                        published = candidate;
                        build = null;
                    }
                }
                catch
                {
                    if (ReferenceEquals(build, candidate))
                    {
                        CancelBuild();
                    }
                    throw;
                }
                finally
                {
                    OrbitalMeshBuildBudget.RecordBuildTime(started);
                }
            }
        }

        private static void CancelBuild()
        {
            Release(build, published);
            build = null;
        }

        private static void Release(AttachmentBuild geometry, AttachmentBuild retained = null)
        {
            if (geometry == null)
            {
                return;
            }

            geometry.Work?.Dispose();
            for (int index = 0; index < SurfaceCount; index++)
            {
                if (!ReferenceEquals(geometry.Near[index].Combined, retained?.Near[index].Combined))
                {
                    UnityEngine.Object.Destroy(geometry.Near[index].Combined);
                }
                if (!ReferenceEquals(geometry.Distant[index].Combined, retained?.Distant[index].Combined))
                {
                    UnityEngine.Object.Destroy(geometry.Distant[index].Combined);
                }
            }
            foreach (var entry in geometry.Regions)
            {
                if (retained != null && retained.Regions.TryGetValue(entry.Key, out Region region)
                    && ReferenceEquals(entry.Value, region))
                {
                    continue;
                }
                for (int index = 0; index < SurfaceCount; index++)
                {
                    UnityEngine.Object.Destroy(entry.Value.Near[index]);
                    UnityEngine.Object.Destroy(entry.Value.Distant[index]);
                }
            }
        }

        private static bool SlotsMatch(IReadOnlyList<SlotState> first, IReadOnlyList<SlotState> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }
            for (int index = 0; index < first.Count; index++)
            {
                if (!first[index].Matches(second[index]))
                {
                    return false;
                }
            }
            return true;
        }

        private static Dictionary<int, Region> GroupSlots(PlanetFactory factory, List<SlotState> slots,
            AttachmentBuild previous)
        {
            float circle = 2 * Mathf.PI;
            int sectors = Mathf.CeilToInt(circle * (factory.planet.realRadius + 64) / 64);
            var slotsByRegion = new Dictionary<int, List<SlotState>>();
            foreach (SlotState slot in slots)
            {
                if (slot.Tracks == 0)
                {
                    continue;
                }

                float longitude = OrbitalTextureLayout.FacilityLongitude(factory.planet, slot.Slot);
                int key = slot.Ring * sectors + Mathf.FloorToInt(Mathf.Repeat(longitude, circle) * sectors / circle);
                if (!slotsByRegion.TryGetValue(key, out var regionSlots))
                {
                    slotsByRegion.Add(key, regionSlots = new List<SlotState>());
                }
                regionSlots.Add(slot);
            }

            var regions = new Dictionary<int, Region>(slotsByRegion.Count);
            foreach (var entry in slotsByRegion)
            {
                Region region = previous != null && previous.Regions.TryGetValue(entry.Key, out Region retained)
                    && SlotsMatch(entry.Value, retained.Slots) ? retained : new Region(entry.Value);
                regions.Add(entry.Key, region);
            }
            return regions;
        }

        private static int ResolveEntity(PlanetFactory factory, EquatorRing ring, int slot)
        {
            var pair = ring.GetPair(slot);
            int poolId = pair.OrbitalStationPoolId;
            if (poolId <= 0)
            {
                return 0;
            }
            int entityId;
            if (pair.stationType == EquatorRing.StationType.Station)
            {
                var pool = factory.transport.stationPool;
                entityId = poolId < pool.Length && pool[poolId] != null ? pool[poolId].entityId : 0;
            }
            else if (pair.stationType == EquatorRing.StationType.Assembler)
            {
                var pool = factory.factorySystem.assemblerPool;
                entityId = poolId < pool.Length ? pool[poolId].entityId : 0;
            }
            else
            {
                entityId = poolId;
            }

            return entityId > 0 && entityId < factory.entityPool.Length && factory.entityPool[entityId].id == entityId
                ? entityId : 0;
        }

        internal static void UpdateTrackPresence(PlanetFactory factory, int previousItem, Vector3 previousPosition,
            int currentItem, Vector3 currentPosition)
        {
            if (factory != observedFactory || localTrackCounts == null)
            {
                return;
            }
            var layout = GetLayout(factory.planet);
            int previous = TrackSlotIndex(factory.planet, layout, previousItem, previousPosition);
            int current = TrackSlotIndex(factory.planet, layout, currentItem, currentPosition);
            if (previous == current)
            {
                return;
            }
            if (previous >= 0 && --localTrackCounts[previous] == 0)
            {
                nextProbe = 0;
            }
            if (current >= 0 && localTrackCounts[current]++ == 0)
            {
                nextProbe = 0;
            }
        }

        private static void IndexLocalTracks(PlanetFactory factory, OrbitalRingLayout layout)
        {
            // Station annexes follow local built segments; ring suspension follows circuit completion.
            localTrackCounts = new int[layout.StationCapacity * layout.StationRingCount * 2];
            CargoTraffic traffic = factory.cargoTraffic;
            for (int beltId = 1; beltId < traffic.beltCursor; beltId++)
            {
                ref BeltComponent belt = ref traffic.beltPool[beltId];
                if (belt.id != beltId || belt.entityId <= 0 || belt.entityId >= factory.entityPool.Length)
                {
                    continue;
                }

                ref EntityData entity = ref factory.entityPool[belt.entityId];
                if (entity.id != belt.entityId || entity.beltId != beltId)
                {
                    continue;
                }
                int index = TrackSlotIndex(factory.planet, layout, entity.protoId, entity.pos);
                if (index >= 0)
                {
                    localTrackCounts[index]++;
                }
            }
        }

        private static int TrackSlotIndex(PlanetData planet, OrbitalRingLayout layout, int itemId, Vector3 position)
        {
            if (itemId != ProtoID.I粒子加速轨道 && itemId != ProtoID.I星环电网组件)
            {
                return -1;
            }
            int ring = CalculateRingPosMark(position, layout).ringIndex;
            if (ring < 0)
            {
                return -1;
            }

            float coordinate = OrbitalTextureLayout.FacilityCoordinate(planet, position);
            int slot = Mathf.RoundToInt(coordinate) % layout.StationCapacity;
            float longitude = OrbitalTextureLayout.FacilityLongitude(planet, slot);
            float actualLongitude = Mathf.Atan2(position.z, position.x);
            float angle = Mathf.DeltaAngle(longitude * Mathf.Rad2Deg, actualLongitude * Mathf.Rad2Deg);
            float distance = Mathf.Abs(angle) * Mathf.Deg2Rad * new Vector2(position.x, position.z).magnitude;
            int trackOffset = itemId == ProtoID.I粒子加速轨道 ? 0 : 1;
            return distance <= 14f ? (ring * layout.StationCapacity + slot) * 2 + trackOffset : -1;
        }

        private static IEnumerable<object> Rebuild(AttachmentBuild candidate)
        {
            var uploads = new List<Mesh>();
            foreach (var entry in candidate.Regions)
            {
                Region region = entry.Value;
                if (published != null && published.Regions.TryGetValue(entry.Key, out Region retained)
                    && ReferenceEquals(region, retained))
                {
                    continue;
                }

                var near = new OrbitalAttachmentMesh[SurfaceCount];
                var distant = new OrbitalAttachmentMesh[SurfaceCount];
                for (int index = 0; index < SurfaceCount; index++)
                {
                    near[index] = new OrbitalAttachmentMesh();
                    distant[index] = new OrbitalAttachmentMesh(1);
                }
                var levels = new[] { near, distant };
                foreach (SlotState slot in region.Slots)
                {
                    foreach (object step in BuildSlot(candidate.Factory, slot, levels))
                    {
                        yield return step;
                    }
                }

                for (int level = 0; level < levels.Length; level++)
                {
                    Mesh[] meshes = level == 0 ? region.Near : region.Distant;
                    for (int index = 0; index < meshes.Length; index++)
                    {
                        if (levels[level][index].TriangleCount == 0)
                        {
                            continue;
                        }
                        Mesh mesh = levels[level][index].Build(index == (int)Surface.PowerPanel);
                        meshes[index] = mesh;
                        mesh.name = SurfaceName(index, level != 0) + ".Chunk." + entry.Key;
                        uploads.Add(mesh);
                        yield return null;
                    }
                }
            }

            foreach (bool distant in new[] { false, true })
            {
                foreach (object step in CombineSurfaces(candidate, distant))
                {
                    yield return step;
                }
            }
            foreach (Mesh mesh in uploads)
            {
                // Region meshes keep their CPU data for future aggregate combinations.
                mesh.UploadMeshData(false);
                yield return null;
            }
        }

        private static IEnumerable<object> CombineSurfaces(AttachmentBuild candidate, bool distant)
        {
            SurfaceMeshes[] destination = distant ? candidate.Distant : candidate.Near;
            SurfaceMeshes[] previous = distant ? published?.Distant : published?.Near;
            for (int index = 0; index < destination.Length; index++)
            {
                var chunks = new List<Mesh>();
                destination[index].Chunks = chunks;
                foreach (Region region in candidate.Regions.Values)
                {
                    Mesh mesh = (distant ? region.Distant : region.Near)[index];
                    if (mesh != null)
                    {
                        chunks.Add(mesh);
                    }
                }

                if (chunks.Count > 1)
                {
                    if (previous != null && MeshesMatch(chunks, previous[index].Chunks))
                    {
                        destination[index].Combined = previous[index].Combined;
                        continue;
                    }

                    Mesh mesh = OrbitalMeshBuffers.Combine(SurfaceName(index, distant), chunks);
                    destination[index].Combined = mesh;
                    mesh.UploadMeshData(true);
                    yield return null;
                }
            }
        }

        private static string SurfaceName(int index, bool distant) =>
            "OrbitalRing.FacilityAttachments." + (Surface)index + (distant ? ".Distant" : ".Near");

        private static bool MeshesMatch(List<Mesh> first, List<Mesh> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }
            for (int index = 0; index < first.Count; index++)
            {
                if (!ReferenceEquals(first[index], second[index]))
                {
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<object> BuildSlot(PlanetFactory factory, SlotState slot,
            OrbitalAttachmentMesh[][] levels)
        {
            bool occupied = slot.Facility.id > 0;
            OrbitalFacilityGeometry.SeatState seatState = occupied ? OrbitalFacilityGeometry.SeatState.Connected
                : !slot.Available ? OrbitalFacilityGeometry.SeatState.Unavailable
                : OrbitalFacilityGeometry.SeatState.Vacant;
            Vector2 seatLongitudes = OrbitalFacilityGeometry.SeatLongitudes(factory, slot.Slot);
            for (int band = 1; band <= 2; band++)
            {
                if ((slot.Tracks & band) == 0)
                {
                    continue;
                }

                for (int side = 0; side < 2; side++)
                {
                    Vector3 center = OrbitalTextureLayout.TrackCenter(factory, slot.Ring, band,
                        seatLongitudes[side], ProtoID.I轨道连接组件);
                    Vector3 up = center.normalized;
                    Vector3 along = Vector3.Cross(up, Vector3.up).normalized * (side == 0 ? -1 : 1);
                    Vector3 across = Vector3.Cross(along, up).normalized;
                    foreach (OrbitalAttachmentMesh[] surfaces in levels)
                    {
                        OrbitalFacilityGeometry.Seat(surfaces, center, across, up, along, seatState);
                    }
                    yield return null;
                }
            }

            if (!occupied)
            {
                yield break;
            }
            float longitude = OrbitalTextureLayout.FacilityLongitude(factory.planet, slot.Slot);
            if ((slot.Tracks & 3) != 0)
            {
                foreach (object step in OrbitalFacilityGeometry.Support(levels, factory, slot.Ring, longitude,
                    slot.Facility, slot.Tracks, seatLongitudes))
                {
                    yield return step;
                }
            }

            if ((slot.Tracks & 4) != 0)
            {
                foreach (object step in OrbitalFacilityBays.Beam(levels, factory, slot.Ring, longitude, slot.Facility))
                {
                    yield return step;
                }
            }

            if ((slot.Tracks & 8) != 0)
            {
                foreach (object step in OrbitalFacilityBays.Power(levels, factory, slot.Ring, longitude, slot.Facility))
                {
                    yield return step;
                }
            }
        }

        internal static void Draw(PlanetFactory factory, Camera camera)
        {
            var model = factory?.planet?.factoryModel;
            if (published == null || factory != observedFactory || camera == null || model == null
                || model.disableFactoryEntities || model.gpuiManager == null || !model.gpuiManager.renderEntity)
            {
                return;
            }
            float planetRadius = factory.planet.realRadius - 2;
            bool distant = UseDistantGeometry(camera, factory.planet.realRadius);
            bool shadowsEnabled = QualitySettings.shadows != ShadowQuality.Disable;
            SurfaceMeshes[] surfaces = distant ? published.Distant : published.Near;
            for (int index = 0; index < surfaces.Length; index++)
            {
                List<Mesh> chunks = surfaces[index].Chunks;
                if (chunks.Count == 0)
                {
                    continue;
                }
                bool guide = index >= (int)Surface.BeamGuide0 && index <= (int)Surface.BeamGuide2;
                bool signal = IsSignalSurface((Surface)index);
                Material material = MaterialFor((Surface)index);
                lighting.Clear();
                TrackLighting.Apply(lighting);
                if (signal)
                {
                    Color tint = SignalTint((Surface)index);
                    float intensity = index == (int)Surface.UnavailableLight
                        ? 1 : 1.2f + Mathf.Sin(Time.time * 1.4f) * 0.15f;
                    if (guide)
                    {
                        int phase = index - (int)Surface.BeamGuide0;
                        float wave = 0.5f + Mathf.Sin(Time.time * 2.2f - phase * Mathf.PI * 2 / 3) * 0.5f;
                        intensity = 0.8f + Mathf.Pow(wave, 3) * 2.1f;
                    }

                    lighting.SetColor("_EmissionColor", tint * intensity);
                }

                // Shadow geometry remains fixed across camera detail levels and screen-space visibility.
                SurfaceMeshes shadow = published.Distant[index];
                if (shadowsEnabled && !signal && shadow.Chunks.Count > 0)
                {
                    Graphics.DrawMesh(shadow.Combined ?? shadow.Chunks[0], Matrix4x4.identity, material,
                        0, camera, 0, lighting, ShadowCastingMode.ShadowsOnly, false);
                }

                drawMeshes.Clear();
                foreach (Mesh chunk in chunks)
                {
                    if (OrbitalRenderBounds.IsVisible(camera, chunk.bounds, planetRadius))
                    {
                        drawMeshes.Add(chunk);
                    }
                }

                if (surfaces[index].Combined != null && drawMeshes.Count * 3 > chunks.Count)
                {
                    Graphics.DrawMesh(surfaces[index].Combined, Matrix4x4.identity, material, 0, camera, 0, lighting,
                        ShadowCastingMode.Off, !signal);
                    continue;
                }

                foreach (Mesh chunk in drawMeshes)
                {
                    Graphics.DrawMesh(chunk, Matrix4x4.identity, material, 0, camera, 0, lighting,
                        ShadowCastingMode.Off, !signal);
                }
            }
        }

        private static bool UseDistantGeometry(Camera camera, float planetRadius)
        {
            float distance = camera.orthographic ? camera.orthographicSize
                : camera.transform.position.magnitude - planetRadius;
            float threshold = camera.orthographic ? DistantOrthographicSize : DistantCameraAltitude;
            float margin = camera.orthographic ? 8 : 12;
            if (detailCamera != camera)
            {
                detailCamera = camera;
                useDistantGeometry = distance >= threshold;
            }
            else if (useDistantGeometry)
            {
                useDistantGeometry = distance >= threshold - margin;
            }
            else
            {
                useDistantGeometry = distance >= threshold + margin;
            }
            return useDistantGeometry;
        }

        private static Material MaterialFor(Surface surface)
        {
            int index = (int)surface;
            if (materials[index] == null)
            {
                materials[index] = TrackModelAssets.CreateMaterial("facility-" + surface);
            }
            return materials[index];
        }

        private static bool IsSignalSurface(Surface surface)
        {
            return surface == Surface.ServiceLight || surface == Surface.BeamLight || surface == Surface.PowerLight
                || surface == Surface.ConnectedLight || surface == Surface.UnavailableLight
                || (surface >= Surface.BeamGuide0 && surface <= Surface.BeamGuide2);
        }

        private static Color SignalTint(Surface surface)
        {
            switch (surface)
            {
                case Surface.ServiceLight:
                    return new Color(1, 0.58f, 0.09f);
                case Surface.PowerLight:
                case Surface.ConnectedLight:
                    return new Color(0.1f, 0.95f, 0.26f);
                case Surface.UnavailableLight:
                    return new Color(0.12f, 0.13f, 0.14f);
                default:
                    return new Color(0.62f, 0.27f, 1);
            }
        }

        internal static void Clear()
        {
            CancelBuild();
            Release(published);
            published = null;
            drawMeshes.Clear();
            pending.Clear();
            observedFactory = null;
            detailCamera = null;
            useDistantGeometry = false;
            nextProbe = 0;
            localTrackCounts = null;
        }

        internal static void Dispose()
        {
            Clear();
            for (int index = 0; index < materials.Length; index++)
            {
                if (materials[index] != null)
                {
                    UnityEngine.Object.Destroy(materials[index]);
                }
                materials[index] = null;
            }

        }
    }
}
