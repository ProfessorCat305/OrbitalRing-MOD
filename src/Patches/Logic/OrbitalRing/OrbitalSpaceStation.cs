using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using ProjectOrbitalRing.Utils;
using System.Reflection.Emit;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using static DebugFactoryData;
using static UIPlayerDeliveryPanel;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalSpaceStation
    {
        public static void BuildElevator(PlanetTransport __instance, int thisEntityId, ref StationComponent thisStation)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, false, __instance.planet.radius == 100f);
            int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加电梯
            planetOrbitalRingData.Rings[ringIndex].AddElevator(position, thisStation.id);
            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
            if (result.stationType == StationType.Station) {
                thisStation.storage = __instance.stationPool[result.OrbitalStationPoolId].storage;
                planetOrbitalRingData.Rings[ringIndex].SetElevatorStorage(position, thisStation.storage);
                return;
            } else {
                planetOrbitalRingData.Rings[ringIndex].SetElevatorStorage(position, thisStation.storage);
                return;
            }
        }

        public static void BuildOrbitalStation(PlanetTransport __instance, int thisEntityId, ref StationComponent thisStation)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
            int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加轨道设施
            planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, thisStation.id, StationType.Station);
            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
            if (result.elevatorPoolId == -1) {
                return;
            } else {
                thisStation.storage = __instance.stationPool[result.elevatorPoolId].storage;
            }
        }

        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
        [HarmonyPostfix]
        public static void NewStationComponentPostPatch(ref PlanetTransport __instance, ref StationComponent __result, int _entityId, PrefabDesc _desc)
        {
            int itemid = __instance.factory.entityPool[_entityId].protoId;
            if (itemid == ProtoID.I太空电梯) { // 太空电梯
                BuildElevator(__instance, _entityId, ref __result);
            }
            if (itemid == ProtoID.I太空物流港 || itemid == ProtoID.I深空物流港 || itemid == ProtoID.I组装厂交互塔) { // 太空物流港、深空物流港
                BuildOrbitalStation(__instance, _entityId, ref __result);
            }
        }

        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RematchLocalPairs))]
        [HarmonyPrefix]
        public static bool RematchLocalPairsPatch(ref StationComponent __instance)
        {
            // 屏蔽轨道物流港的行星内运输航线计算，让运输机只送太空电梯
            if (!__instance.isVeinCollector && __instance.workDroneDatas.Length <= 0) {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RematchLocalPairs))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StationComponent_RematchLocalPairs_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldelem_Ref),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.id))));

            object IL_00B5 = matcher.Advance(2).Operand;

            // 本地供应判断，需求遍历遇到是星际物流港就跳过
            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.isStellar))),
                new CodeInstruction(OpCodes.Brtrue, IL_00B5)
            );

            return matcher.InstructionEnumeration();
        }

        public static bool ShouldRemoteLogicBeNull(StationComponent station)
        {
            if (station.isStellar) {
                return true;
            }
            // 太空电梯是唯一一个不是isStellar但是仓储量从15000起跳的设施，用仓储量来判断不清空星际供需逻辑
            for (int i = 0; i < station.storage.Length; i++) {
                if (station.storage[i].max >= 15000) {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetTransport_SetStationStorage_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.isStellar))));
            object IL_0030 = matcher.Advance(1).Operand;

            matcher.Advance(-1).SetAndAdvance(
                OpCodes.Call, AccessTools.Method(typeof(OrbitalSpaceStation), nameof(ShouldRemoteLogicBeNull))
            );

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        [HarmonyPostfix]
        public static void ImportPatch(PlanetFactory __instance)
        {
            int planetId = __instance.planet.id;
            PlanetOrbitalRingData data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (data == null) return;
            for (int i = 0; i < data.Rings.Count; i++) {
                EquatorRing ring = data.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    StationStore[] storage = null;
                    if (pair.elevatorPoolId != -1) {
                        if (__instance.transport.stationPool == null) continue;
                        if (__instance.transport.stationPool.Length == 0) continue;
                        if (__instance.transport.stationPool.Length <= pair.elevatorPoolId) continue;
                        if (__instance.transport.stationPool[pair.elevatorPoolId] == null) continue;
                        storage = __instance.transport.stationPool[pair.elevatorPoolId].storage;
                        data.Rings[i].SetElevatorStorage(j, storage);
                    }
                    if (pair.OrbitalStationPoolId != -1 && pair.stationType == StationType.Station && storage != null) {
                        __instance.transport.stationPool[pair.OrbitalStationPoolId].storage = storage;
                    }
                }
            }
        }

        // 修复轨道空投引导站 拥有星际物流配对的情况下展开配对报错的问题，因为空投引导站没有运输船
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.CalcRemoteSingleTripTime))]
        [HarmonyPrefix]
        public static bool CalcRemoteSingleTripTimePatch(StationComponent __instance, int __result)
        {
            if (__instance.shipDiskPos.Length == 0) {
                __result = 0;
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick_UpdateNeeds))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetTransport_GameTick_UpdateNeeds_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(StationComponent), nameof(StationComponent.UpdateNeeds)))
                );

            matcher.Advance(3).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetTransport), nameof(PlanetTransport.factory))),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalSpaceStation), nameof(SyncWarpDrive)))
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._station_input_parallel))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GameLogic_station_input_parallel_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            matcher.MatchForward(false, 
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameLogic), nameof(GameLogic.factories)))
                );

            object factory = matcher.Advance(3).Operand;

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(StationComponent), nameof(StationComponent.UpdateNeeds)))
                );

            object stationPoolId = matcher.Advance(-5).Operand;

            matcher.Advance(6).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, factory),
                new CodeInstruction(OpCodes.Ldloc_S, stationPoolId),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalSpaceStation), nameof(SyncWarpDrive)))
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        private static void SyncWarpDrive(PlanetFactory factory, int stationPoolId)
        {
            ref StationComponent station = ref factory.transport.stationPool[stationPoolId];
            if (factory.entityPool[station.entityId].protoId != ProtoID.I太空电梯) return;
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planet.id);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    if (pair.elevatorPoolId == station.id) {
                        if (pair.stationType == StationType.Station) {
                            StationComponent stellarStation = factory.transport.stationPool[pair.OrbitalStationPoolId];
                            if (factory.entityPool[stellarStation.entityId].protoId == ProtoID.I深空物流港) {
                                stellarStation.warperCount += station.warperCount;
                                station.warperCount = 0;
                                if (stellarStation.warperCount < stellarStation.warperMaxCount) {
                                    station.needs[5] = 1210;
                                }
                                station.warperCount = 0;
                            }
                        }
                    }
                }
            }
        }

    }
}