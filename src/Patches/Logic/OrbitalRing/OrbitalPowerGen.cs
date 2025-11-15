using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using UnityEngine;
using ProjectOrbitalRing.Utils;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalPowerGen
    {
        public static void OrbitalPowerGenInternalUpdate(ref PowerGeneratorComponent __instance, int planetId)
        {
            if (__instance.fuelCount >= 10) return;
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    if (pair.stationType == StationType.PowerGenCore && pair.elevatorPoolId != -1 && pair.OrbitalCorePoolId == __instance.id) {
                        var storage = ring.GetElevatorStorage(j);
                        for (int k = 0; k < storage.Length; k++) {
                            if (storage[k].itemId == __instance.fuelId && storage[k].count >= 1) {
                                __instance.fuelCount += 1;
                                //storage[k].count -= 1;
                                int inc = split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                __instance.fuelInc += (short)inc;
                                //storage[k].inc -= inc;
                            } else if (__instance.fuelId == 0) {
                                int[] array = ItemProto.fuelNeeds[(int)__instance.fuelMask];
                                if (array == null || array.Length == 0) {
                                    return;
                                }
                                for (int z = 0; z < array.Length; z++) {
                                    if (array[z] == storage[k].itemId && storage[k].count >= 1) {
                                        int inc = split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                        __instance.SetNewFuel(storage[k].itemId, 1, (short)inc);
                                        //storage[k].count -= 1;
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        [HarmonyPrefix]
        public static void GameTickPatch(ref PowerSystem __instance)
        {
            for (int i = 0; i < __instance.genPool.Length; i++) {
                if (__instance.factory.entityPool[__instance.genPool[i].entityId].protoId == ProtoID.I轨道反物质堆核心) {
                    OrbitalPowerGenInternalUpdate(ref __instance.genPool[i], __instance.planet.id);
                }
            }

        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        [HarmonyPrefix]
        public static void CreateEntityLogicComponentsPatch(ref PlanetFactory __instance, int entityId)
        {
            int modelId = __instance.entityPool[entityId].modelIndex;
            if (modelId == ProtoID.M轨道反物质堆基座) {
                Vector3 thisPos = __instance.entityPool[entityId].pos;
                int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
                int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
                OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                // 在赤道上/下圈？号位置添加轨道设施
                if (modelId == ProtoID.M轨道反物质堆基座) {
                    planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, entityId, StationType.PowerGenBase);
                }
            }
        }

        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.NewGeneratorComponent))]
        [HarmonyPostfix]
        public static void NewGeneratorComponentPatch(ref PowerSystem __instance, int entityId, int __result)
        {
            if (__instance.factory.entityPool[entityId].protoId == 6261) {
                Vector3 thisPos = __instance.factory.entityPool[entityId].pos;
                int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
                int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                // 在赤道上/下圈？号位置添加轨道设施
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalCore(position, __result, StationType.PowerGenCore);
            }
        }
    }
}
