using HarmonyLib;

namespace ProjectOrbitalRing.Rendering
{
    [HarmonyPatch]
    internal static class TrackRenderPatches
    {
        private static bool IsTrackBelt(CargoTraffic traffic, int beltId)
        {
            if (traffic?.beltPool == null || beltId <= 0 || beltId >= traffic.beltPool.Length ||
                traffic.beltPool[beltId].id != beltId)
            {
                return false;
            }

            int entityId = traffic.beltPool[beltId].entityId;
            return entityId > 0 && entityId < traffic.factory.entityPool.Length &&
                traffic.factory.entityPool[entityId].id == entityId &&
                TrackRenderer.IsRegistered(traffic.factory.entityPool[entityId].protoId);
        }

        internal static bool IsTrackPath(CargoTraffic traffic, int pathId)
        {
            var path = traffic.GetCargoPath(pathId);
            if (path == null || path.belts.Count == 0)
            {
                return false;
            }

            foreach (int beltId in path.belts)
            {
                if (!IsTrackBelt(traffic, beltId))
                {
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltState))]
        private static bool SetState(CargoTraffic __instance, int beltId, int state)
        {
            if (!IsTrackBelt(__instance, beltId))
            {
                return true;
            }
            TrackRenderer.SetSelection(__instance.factory, __instance.beltPool[beltId].entityId, state);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.ClearStates))]
        private static void ClearStates(CargoTraffic __instance) => TrackRenderer.ClearSelection(__instance.factory);

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltRenderer))]
        private static void BeltVisual(CargoTraffic __instance, int beltId)
        {
            if (!IsTrackBelt(__instance, beltId))
            {
                return;
            }

            // Native geometry generation also owns collision updates and runs before mesh removal.
            __instance.RemoveBeltRenderer(beltId);
            TrackRenderer.Dirty(__instance.factory, __instance.beltPool[beltId].entityId);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterPathRenderer))]
        private static bool PathVisual(CargoTraffic __instance, int pathId)
        {
            if (!IsTrackPath(__instance, pathId))
            {
                return true;
            }

            __instance.RemovePathRenderer(pathId);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        private static void Created(PlanetFactory __instance, int entityId)
        {
            if (TrackTopology.IsNode(__instance, entityId))
            {
                TrackRenderer.Dirty(__instance, entityId);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
        private static void Removing(CargoTraffic __instance, int id, out int __state) =>
            __state = IsTrackBelt(__instance, id) ? __instance.beltPool[id].entityId : 0;

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.RemoveBeltComponent))]
        private static void Removed(CargoTraffic __instance, int __state)
        {
            TrackRenderer.ForgetSelection(__instance.factory, __state);
            TrackRenderer.Dirty(__instance.factory, __state);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConnDirect))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConnDirect))]
        private static void ConnectionChanged(PlanetFactory __instance, int objId, int slot)
        {
            if (slot >= 0 && slot < 4 && TrackTopology.IsNode(__instance, objId))
            {
                TrackRenderer.Dirty(__instance, objId);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.UpgradeEntityWithComponents))]
        private static void BeforeUpgrade(PlanetFactory __instance, int entityId, out bool __state) =>
            __state = TrackTopology.IsNode(__instance, entityId);

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.UpgradeEntityWithComponents))]
        private static void Upgraded(PlanetFactory __instance, int entityId, bool __state)
        {
            if (__state || TrackTopology.IsNode(__instance, entityId))
            {
                TrackRenderer.Dirty(__instance, entityId);
            }
        }
    }
}
