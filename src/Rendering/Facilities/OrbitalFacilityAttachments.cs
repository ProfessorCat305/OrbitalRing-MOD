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
                    | functionalTracks) & RemodelSettings.AttachmentTracks;
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

        private sealed class AttachmentBuild
        {
            internal PlanetFactory Factory;
            internal SlotState[] Slots;
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
        private static int[] localTracks = new int[0];
        private static bool tracksDirty = true;
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
                if (tracksDirty)
                {
                    ReadLocalTracks(factory);
                    tracksDirty = false;
                }

                for (int ringIndex = 0; ringIndex < data.Rings.Count; ringIndex++)
                {
                    EquatorRing ring = data.Rings[ringIndex];
                    for (int slot = 0; slot < ring.Capacity; slot++)
                    {
                        int index = ringIndex * GetLayout(factory.planet).StationCapacity + slot;
                        int presence = index < localTracks.Length ? localTracks[index] : 0;
                        pending.Add(new SlotState(factory, ringIndex, slot, ring, presence));
                    }
                }
            }

            SlotState[] expected = (build ?? published)?.Slots ?? Array.Empty<SlotState>();
            bool changed = pending.Count != expected.Length;
            for (int index = 0; !changed && index < pending.Count; index++)
            {
                changed = !pending[index].Matches(expected[index]);
            }

            if (!changed)
            {
                return;
            }
            CancelBuild();
            var candidate = new AttachmentBuild
            {
                Factory = factory,
                Slots = pending.ToArray(),
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
                        Release(published);
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
            Release(build);
            build = null;
        }

        private static void Release(AttachmentBuild geometry)
        {
            if (geometry == null)
            {
                return;
            }

            geometry.Work?.Dispose();
            foreach (SurfaceMeshes[] level in new[] { geometry.Near, geometry.Distant })
            {
                foreach (SurfaceMeshes surface in level)
                {
                    UnityEngine.Object.Destroy(surface.Combined);
                    if (surface.Chunks == null)
                    {
                        continue;
                    }
                    foreach (Mesh chunk in surface.Chunks)
                    {
                        UnityEngine.Object.Destroy(chunk);
                    }
                }
            }
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

        internal static void InvalidateTracks(PlanetFactory factory)
        {
            if (factory == observedFactory)
            {
                tracksDirty = true;
                nextProbe = 0;
            }
        }

        internal static bool HasBay(int itemId) =>
            itemId == ProtoID.I粒子加速轨道 || itemId == ProtoID.I星环电网组件;

        private static void ReadLocalTracks(PlanetFactory factory)
        {
            // Station annexes follow local built segments; ring suspension follows circuit completion.
            var layout = GetLayout(factory.planet);
            int count = layout.StationCapacity * layout.StationRingCount;
            if (localTracks.Length != count)
            {
                localTracks = new int[count];
            }
            else
            {
                Array.Clear(localTracks, 0, localTracks.Length);
            }

            CargoTraffic traffic = factory.cargoTraffic;
            for (int beltId = 1; beltId < traffic.beltCursor; beltId++)
            {
                ref BeltComponent belt = ref traffic.beltPool[beltId];
                if (belt.id != beltId || belt.entityId <= 0 || belt.entityId >= factory.entityPool.Length)
                {
                    continue;
                }

                ref EntityData entity = ref factory.entityPool[belt.entityId];
                if (entity.id != belt.entityId || entity.beltId != beltId || !HasBay(entity.protoId))
                {
                    continue;
                }

                var position = CalculateRingPosMark(entity.pos, layout);
                if (position.ringIndex < 0)
                {
                    continue;
                }

                float coordinate = OrbitalTextureLayout.FacilityCoordinate(factory.planet, entity.pos);
                int slot = Mathf.RoundToInt(coordinate) % layout.StationCapacity;
                float longitude = OrbitalTextureLayout.FacilityLongitude(factory.planet, slot);
                float actualLongitude = Mathf.Atan2(entity.pos.z, entity.pos.x);
                float angle = Mathf.DeltaAngle(longitude * Mathf.Rad2Deg, actualLongitude * Mathf.Rad2Deg);
                float distance = Mathf.Abs(angle) * Mathf.Deg2Rad * new Vector2(entity.pos.x, entity.pos.z).magnitude;
                if (distance <= 14f)
                {
                    localTracks[position.ringIndex * layout.StationCapacity + slot] |=
                        entity.protoId == ProtoID.I粒子加速轨道 ? 4 : 8;
                }
            }
        }

        private static IEnumerable<object> Rebuild(AttachmentBuild candidate)
        {
            foreach (bool distant in new[] { false, true })
            {
                foreach (object step in RebuildLevel(candidate, distant))
                {
                    yield return step;
                }
            }
        }

        private static IEnumerable<object> RebuildLevel(AttachmentBuild candidate, bool distant)
        {
            PlanetFactory factory = candidate.Factory;
            SurfaceMeshes[] destination = distant ? candidate.Distant : candidate.Near;
            int sectors = Mathf.CeilToInt(2 * Mathf.PI * (factory.planet.realRadius + 64) / 64);
            var regions = new Dictionary<int, OrbitalAttachmentMesh[]>();
            foreach (SlotState slot in candidate.Slots)
            {
                if (slot.Tracks == 0)
                {
                    continue;
                }

                float longitude = OrbitalTextureLayout.FacilityLongitude(factory.planet, slot.Slot);
                int region = slot.Ring * sectors + Mathf.FloorToInt(Mathf.Repeat(longitude, 2 * Mathf.PI)
                    * sectors / (2 * Mathf.PI));
                if (!regions.TryGetValue(region, out OrbitalAttachmentMesh[] builders))
                {
                    builders = new OrbitalAttachmentMesh[SurfaceCount];
                    for (int index = 0; index < builders.Length; index++)
                    {
                        builders[index] = new OrbitalAttachmentMesh(distant ? 1 : 0);
                    }
                    regions.Add(region, builders);
                }

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

                    foreach (float sign in new[] { -1f, 1f })
                    {
                        Vector3 center = OrbitalTextureLayout.TrackCenter(factory, slot.Ring, band,
                            sign < 0 ? seatLongitudes.x : seatLongitudes.y, ProtoID.I轨道连接组件);
                        Vector3 up = center.normalized;
                        Vector3 along = Vector3.Cross(up, Vector3.up).normalized * sign;
                        Vector3 across = Vector3.Cross(along, up).normalized;
                        OrbitalFacilityGeometry.Seat(builders, center, across, up, along, seatState);
                        yield return null;
                    }
                }

                if (occupied && (slot.Tracks & 3) != 0)
                {
                    OrbitalFacilityGeometry.Support(builders, factory, slot.Ring, longitude,
                        slot.Facility, slot.Tracks, seatLongitudes);
                    yield return null;
                }

                if (occupied && (slot.Tracks & 4) != 0)
                {
                    OrbitalFacilityBays.Beam(builders, factory, slot.Ring, longitude, slot.Facility);
                    yield return null;
                }

                if (occupied && (slot.Tracks & 8) != 0)
                {
                    OrbitalFacilityBays.Power(builders, factory, slot.Ring, longitude, slot.Facility);
                    yield return null;
                }

                yield return null;
            }

            for (int index = 0; index < destination.Length; index++)
            {
                string name = "OrbitalRing.FacilityAttachments." + (Surface)index + (distant ? ".Distant" : ".Near");
                var chunks = new List<Mesh>();
                destination[index].Chunks = chunks;
                foreach (OrbitalAttachmentMesh[] builders in regions.Values)
                {
                    OrbitalAttachmentMesh builder = builders[index];
                    if (builder.TriangleCount == 0)
                    {
                        continue;
                    }
                    Mesh mesh = builder.Build(index == (int)Surface.PowerPanel);
                    mesh.name = name + ".Chunk." + chunks.Count;
                    chunks.Add(mesh);
                    yield return null;
                }
                if (chunks.Count == 0)
                {
                    continue;
                }

                if (chunks.Count > 1)
                {
                    Mesh combined = OrbitalMeshBuffers.Combine(name, chunks);
                    destination[index].Combined = combined;
                    combined.UploadMeshData(true);
                    yield return null;
                }
                foreach (Mesh chunk in chunks)
                {
                    chunk.UploadMeshData(true);
                    yield return null;
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
            OrbitalRenderBounds.Prepare(camera);
            float planetRadius = factory.planet.realRadius - 2;
            bool distant = UseDistantGeometry(camera, factory.planet.realRadius);
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
                if (!signal && shadow.Chunks.Count > 0)
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
            tracksDirty = true;
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
