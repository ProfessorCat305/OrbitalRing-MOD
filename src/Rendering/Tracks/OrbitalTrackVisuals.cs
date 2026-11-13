using System;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalTrackVisuals
    {
        private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int emissionMultiplier = Shader.PropertyToID("_EmissionMultiplier");
        private static readonly int beamTexture = Shader.PropertyToID("_MainTex");
        private static readonly int emissionTexture = Shader.PropertyToID("_EmissionMap");
        private static readonly int textureTransform = Shader.PropertyToID("_MainTex_ST");
        private static GameData observedGame;
        private static bool registered;

        internal static void Register()
        {
            if (RemodelSettings.AnyTrack)
            {
                TrackModelAssets.Initialize();
            }
            registered = true;
        }

        private static int RingGroup(PlanetFactory factory, int entityId)
        {
            var position = CalculateRingPosMark(factory.entityPool[entityId].pos, GetLayout(factory.planet));
            return position.ringIndex * 4 + position.ringBeltIndex;
        }

        internal static int BatchGroup(PlanetFactory factory, int entityId)
        {
            bool particleTrack = factory.entityPool[entityId].protoId == ProtoID.I粒子加速轨道;
            int direction = particleTrack ? TravelDirection(factory, entityId) : 1;
            // A draw batch shares one animation direction and one ring activation state.
            return RingGroup(factory, entityId) * 2 + (direction < 0 ? 1 : 0);
        }

        private static int TravelDirection(PlanetFactory factory, int entityId)
        {
            if (StructuralTrackPolicy.Enabled)
            {
                var position = CalculateRingPosMark(factory.entityPool[entityId].pos, GetLayout(factory.planet));
                return position.ringIndex == 1 ? -1 : 1;
            }

            for (int slot = 0; slot < 4; slot++)
            {
                factory.ReadObjectConn(entityId, slot, out bool output, out int neighbor, out _);
                if (!IsEntity(factory, neighbor) || factory.entityPool[neighbor].protoId != ProtoID.I粒子加速轨道)
                {
                    continue;
                }

                Vector3 from = factory.entityPool[output ? entityId : neighbor].pos;
                Vector3 to = factory.entityPool[output ? neighbor : entityId].pos;
                float delta = Mathf.DeltaAngle(Longitude(from), Longitude(to));
                return delta < 0 ? -1 : 1;
            }

            return 1;
        }

        internal static void ApplyEffects(PlanetFactory factory, TrackDefinition definition, int group,
            TrackSurfaceRole surface, MaterialPropertyBlock properties)
        {
            int itemId = definition.ItemId;
            Color lightColor = definition.LightColor;
            float time = Time.time;
            bool wallSurface =
                surface == TrackSurfaceRole.Wall || surface == TrackSurfaceRole.InnerWall;
            bool frameSurface =
                surface == TrackSurfaceRole.Fittings || surface == TrackSurfaceRole.Detail;
            if (surface != TrackSurfaceRole.Core && !wallSurface && !frameSurface)
            {
                return;
            }

            int ringGroup = group / 2;
            int band = ringGroup % 4;
            if (group < 0 || (band != 1 && band != 2))
            {
                return;
            }

            var textureLayout = OrbitalTextureLayout.FromBand(
                factory.planet, itemId, band, definition.CenterHeight);
            bool particle = itemId == ProtoID.I粒子加速轨道;
            var data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planet.id);
            int ringIndex = ringGroup / 4;
            EquatorRing ring = data != null && ringIndex < data.Rings.Count ? data.Rings[ringIndex] : null;
            bool complete = ring != null && IsComplete(ring, band, itemId);
            bool active = complete && (!particle || IsColliderWorking(factory, ring));
            float speed = active ? 2.5f : 0.5f;
            float direction = group % 2 == 0 ? 1 : -1;
            float offset = 0;
            if (particle && surface == TrackSurfaceRole.Core)
            {
                float beamPhaseSpeed = textureLayout.DistanceToPhase(1.8f);
                offset = -time * beamPhaseSpeed * direction * speed;
            }

            float uvScale = textureLayout.UvScale;
            if (particle && frameSurface)
            {
                uvScale = 1;
            }
            else if (itemId == ProtoID.I星环电网组件 && surface == TrackSurfaceRole.Core)
            {
                int repeats = Math.Max(1, Mathf.RoundToInt(textureLayout.RepeatCount / 2f));
                uvScale = repeats / OrbitalTextureLayout.CoordinatePeriod;
            }
            else if (itemId == ProtoID.I轨道连接组件 && surface == TrackSurfaceRole.Fittings)
            {
                // Panel patterns span two truss bays, with five periods per facility interval.
                uvScale /= 2;
            }

            properties.SetVector(textureTransform, new Vector4(1, uvScale, 0, offset));
            float pulse = 0.86f + Mathf.Sin(time * 1.8f) * 0.14f;
            float intensity = 1.3f;
            if (frameSurface)
            {
                intensity = 0.8f;
            }
            else if (particle)
            {
                intensity = 4.8f;
            }
            else if (itemId == ProtoID.I轨道连接组件)
            {
                intensity = OrbitalTrackAppearance.StructureLightIntensity;
            }

            bool whiteEmission = frameSurface || itemId == ProtoID.I星环电网组件 || particle;
            Color emissionTint = whiteEmission ? Color.white : lightColor;
            float standbyIntensity = particle && frameSurface ? 0.55f : 0.18f;
            float emissionStrength = 0;
            if (complete)
            {
                emissionStrength = active ? intensity * pulse : standbyIntensity;
            }

            properties.SetColor(emissionColor, emissionTint * emissionStrength);
            properties.SetFloat(emissionMultiplier, emissionStrength);
            if (particle && surface == TrackSurfaceRole.Core)
            {
                Texture texture = active ? OrbitalTrackAppearance.WorkingBeamTexture
                    : OrbitalTrackAppearance.StandbyBeamTexture;

                if (texture != null)
                {
                    properties.SetTexture(beamTexture, texture);
                }

                float beamIntensity = complete ? 0.95f : 0;
                if (active)
                {
                    beamIntensity = 1.65f * (1 + Mathf.Sin(time * 5) * 0.06f);
                }

                properties.SetColor("_TintColor", Color.white * beamIntensity);
            }

            if (particle && surface == TrackSurfaceRole.Detail)
            {
                Texture texture = active ? OrbitalTrackAppearance.AcceleratorWorkingEmission
                    : OrbitalTrackAppearance.AcceleratorStandbyEmission;

                if (texture != null)
                {
                    properties.SetTexture(emissionTexture, texture);
                }
            }
        }

        private static bool IsComplete(EquatorRing ring, int band, int itemId)
        {
            if (itemId == ProtoID.I粒子加速轨道)
            {
                return ring.isLowInsideRingComplete;
            }

            if (itemId == ProtoID.I星环电网组件)
            {
                return ring.isLowOutsideRingComplete;
            }

            return band == 1 ? ring.isInsideRingComplete : ring.isOutsideRingComplete;
        }

        private static bool IsColliderWorking(PlanetFactory factory, EquatorRing ring)
        {
            var assemblers = factory.factorySystem?.assemblerPool;
            if (assemblers == null || !ring.IsOneFull() || !ring.isLowInsideRingComplete)
            {
                return false;
            }

            for (int slot = 0; slot < ring.Capacity; slot++)
            {
                var pair = ring.GetPair(slot);
                int assemblerId = pair.OrbitalStationPoolId;
                if (pair.stationType != EquatorRing.StationType.Assembler ||
                    assemblerId <= 0 || assemblerId >= assemblers.Length)
                {
                    continue;
                }

                ref AssemblerComponent assembler = ref assemblers[assemblerId];
                int entityId = assembler.entityId;
                if (assembler.id != assemblerId || !IsEntity(factory, entityId))
                {
                    continue;
                }

                ref EntityData entity = ref factory.entityPool[entityId];
                if (entity.protoId != ProtoID.I星环对撞机 || entity.assemblerId != assemblerId)
                {
                    continue;
                }

                return assembler.recipeId > 0 && entityId < factory.entityAnimPool.Length &&
                    factory.entityAnimPool[entityId].state == 1 && factory.entityAnimPool[entityId].power >= 0.1f;
            }
            return false;
        }

        internal static void Update()
        {
            if (!ReferenceEquals(observedGame, GameMain.data))
            {
                Reset();
            }
            if (registered && GameMain.isRunning)
            {
                OrbitalRingBraces.Update(GameMain.localPlanet?.factory);
                OrbitalFacilityAttachments.Update();
            }
        }

        private static float Longitude(Vector3 point)
        {
            return Mathf.Atan2(point.z, point.x) * Mathf.Rad2Deg;
        }

        private static bool IsEntity(PlanetFactory factory, int entityId)
        {
            return factory != null && entityId > 0 && entityId < factory.entityPool.Length
                && factory.entityPool[entityId].id == entityId;
        }

        internal static void Reset()
        {
            OrbitalFacilityAttachments.Clear();
            OrbitalRingBraces.Clear();
            observedGame = GameMain.data;
            TrackRenderer.Reset();
        }

        internal static void Dispose()
        {
            OrbitalFacilityAttachments.Dispose();
            OrbitalRingBraces.Dispose();

            OrbitalTrackAppearance.Clear();
            TrackRenderer.Dispose();
            registered = false;
        }
    }
}
