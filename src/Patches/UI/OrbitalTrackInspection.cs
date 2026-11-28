using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow;
using ProjectOrbitalRing.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;

namespace ProjectOrbitalRing.Patches.UI
{
    [HarmonyPatch]
    internal static class OrbitalTrackInspection
    {
        private enum TrackWindow
        {
            None,
            Storage,
            Power
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        private static void ResolveTrack(EObjectType objType, ref int objId,
            out (TrackWindow window, int planetId, int ringIndex, int nodeId) __state)
        {
            __state = default;
            PlanetFactory factory = GameMain.mainPlayer?.factory;
            if (objType != EObjectType.Entity || objId <= 0 ||
                !StructuralTrackPolicy.TryReadObject(factory, objId, out int itemId, out var position) ||
                !StructuralTrackPolicy.IsTrackItem(itemId) || !HasClosedPath(factory, objId)) return;

            var planet = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
            if (planet == null) return;

            var (band, ringIndex) = PosTool.CalculateRingPosMark(position, PosTool.GetLayout(factory.planet));
            if (ringIndex < 0 || ringIndex >= planet.Rings.Count) return;
            EquatorRing ring = planet.Rings[ringIndex];

            switch (itemId) {
                case ProtoID.I轨道连接组件 when IsConnectorComplete(ring, band) && HasTianshu(factory, ring):
                    __state = (TrackWindow.Storage, factory.planetId, ringIndex, 0);
                    objId = 0;
                    break;
                case ProtoID.I粒子加速轨道 when ring.isLowInsideRingComplete:
                    int controller = ring.FindParticleColliderEntity(factory);
                    if (controller > 0) objId = controller;
                    break;
                case ProtoID.I星环电网组件 when ring.isLowOutsideRingComplete:
                    int nodeId = GlobalPowerSupplyPatches.FindGlobalPowerNode(factory);
                    if (nodeId > 0) {
                        __state = (TrackWindow.Power, factory.planetId, ringIndex, nodeId);
                        objId = 0;
                    }
                    break;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        private static void OpenTrackWindow(UIGame __instance, (TrackWindow window, int planetId, int ringIndex, int nodeId) __state)
        {
            switch (__state.window) {
                case TrackWindow.Storage:
                    UIOrbitalRingStorageDetailExpand.OpenStorageWindow(__instance, __state.planetId, __state.ringIndex);
                    break;
                case TrackWindow.Power:
                    __instance.ShutAllFunctionWindow();
                    __instance.ShutPlayerInventory();
                    __instance.inspectNodeId = __state.nodeId;
                    __instance.nodeWindow.nodeId = __state.nodeId;
                    __instance.OpenNodeWindow();
                    break;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(UIPowerNodeWindow), nameof(UIPowerNodeWindow.OnNodeIdChange))]
        private static IEnumerable<CodeInstruction> ResolvePowerNodeTitle(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(EntityData), nameof(EntityData.protoId))));
            return matcher.ThrowIfInvalid("Power node title item").Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalTrackInspection), nameof(GetPowerNodeTitleItem)))
            ).InstructionEnumeration();
        }

        private static int GetPowerNodeTitleItem(int protoId, UIPowerNodeWindow window)
        {
            // The planet-wide node has no entity prototype.
            return protoId == 0 && GlobalPowerSupplyPatches.IsGlobalPowerNode(in window.powerSystem.nodePool[window.nodeId])
                ? ProtoID.I星环电网组件
                : protoId;
        }

        private static bool HasClosedPath(PlanetFactory factory, int entityId)
        {
            CargoTraffic traffic = factory.cargoTraffic;
            int beltId = factory.entityPool[entityId].beltId;
            if (traffic == null || beltId <= 0 || beltId >= traffic.beltCursor) return false;
            ref BeltComponent belt = ref traffic.beltPool[beltId];
            int pathId = belt.segPathId;
            return belt.id == beltId && belt.entityId == entityId && pathId > 0 && pathId < traffic.pathCursor &&
                traffic.GetCargoPath(pathId)?.closed == true;
        }

        private static bool IsConnectorComplete(EquatorRing ring, int band) =>
            band == 1 && ring.isInsideRingComplete || band == 2 && ring.isOutsideRingComplete;

        private static bool HasTianshu(PlanetFactory factory, EquatorRing ring)
        {
            for (int slot = 0; slot < ring.Capacity; slot++) {
                var pair = ring.GetPair(slot);
                switch (pair.stationType) {
                    case StationType.GlobalSupportBase:
                    case StationType.GlobalIncCore:
                    case StationType.ATFeildCore:
                    case StationType.SynapticLathe:
                    case StationType.BanDFTinderDispatch:
                        if (pair.OrbitalStationPoolId > 0 &&
                            StructuralTrackPolicy.TryReadObject(factory, pair.OrbitalStationPoolId, out int itemId, out _) &&
                            itemId == ProtoID.I天枢座) return true;
                        break;
                }
            }
            return false;
        }
    }
}
