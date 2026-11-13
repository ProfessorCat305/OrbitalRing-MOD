using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Rendering
{
    [HarmonyPatch]
    internal static class OrbitalConstructionOrientation
    {
        private static bool HasNeighbors(PlanetFactory factory, int entityId)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                factory.ReadObjectConn(entityId, slot, out _, out int neighbor, out _);
                if (neighbor != 0)
                {
                    return true;
                }
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.AddPrebuildDataWithComponents))]
        private static void AlignPrebuild(ref PrebuildData prebuild)
        {
            bool structural = StructuralTrackPolicy.Enabled && StructuralTrackPolicy.IsTrackItem(prebuild.protoId);
            if (structural || TrackRenderer.IsRegistered(prebuild.protoId))
            {
                Vector3 direction = structural ? Vector3.zero : prebuild.rot * Vector3.forward;
                prebuild.rot = TrackRotation(prebuild.pos, prebuild.rot, direction);
            }
            if (structural)
            {
                prebuild.rot2 = prebuild.rot;
                prebuild.tilt = 0;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.AddEntityDataWithComponents))]
        private static void AlignEntity(ref EntityData entity)
        {
            bool structural = StructuralTrackPolicy.Enabled && StructuralTrackPolicy.IsTrackItem(entity.protoId);
            if (structural || TrackRenderer.IsRegistered(entity.protoId))
            {
                Vector3 direction = structural ? Vector3.zero : entity.rot * Vector3.forward;
                entity.rot = TrackRotation(entity.pos, entity.rot, direction);
            }
            if (structural)
            {
                entity.tilt = 0;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltRenderer))]
        private static void AlignNativeIsolated(CargoTraffic __instance, int beltId)
        {
            if (beltId > 0 && beltId < __instance.beltPool.Length)
            {
                AlignIsolated(__instance.factory, __instance.beltPool[beltId].entityId);
            }
        }

        internal static void AlignIsolated(PlanetFactory factory, int entityId)
        {
            if (entityId <= 0 || entityId >= factory.entityPool.Length || factory.entityPool[entityId].id != entityId)
            {
                return;
            }

            ref var entity = ref factory.entityPool[entityId];
            if (!TrackRenderer.IsRegistered(entity.protoId) || HasNeighbors(factory, entityId))
            {
                return;
            }

            Quaternion rotation = TrackRotation(entity.pos, entity.rot, entity.rot * Vector3.forward);
            if (Quaternion.Angle(entity.rot, rotation) < 0.01f)
            {
                return;
            }

            entity.rot = rotation;
            if (entity.colliderId > 0 && factory.planet.physics?.colChunks != null)
            {
                int chunk = entity.colliderId >> 20;
                int index = entity.colliderId & 0xFFFFF;
                factory.planet.physics.colChunks[chunk].colliderPool[index].q = rotation;
                factory.planet.physics.SetPlanetPhysicsColliderDirty();
            }
        }
    }
}
