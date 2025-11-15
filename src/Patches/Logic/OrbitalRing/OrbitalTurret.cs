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
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalTurret
    {
        public static void OrbitalTurretInternalUpdate(ref TurretComponent __instance, int planetId)
        {
            if (__instance.itemCount >= 5) return;
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    if (pair.stationType == StationType.TurretCore && pair.elevatorPoolId != -1 && pair.OrbitalCorePoolId == __instance.id) {
                        var storage = ring.GetElevatorStorage(j);
                        for (int k = 0; k < storage.Length; k++) {
                            if (storage[k].itemId == __instance.itemId && storage[k].count >= 1) {
                                __instance.itemCount += 1;
                                int inc = split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                __instance.itemInc += (short)inc;
                            } else if (__instance.itemId == 0) {
                                int[] array = ItemProto.turretNeeds[(int)__instance.ammoType];
                                if (array == null || array.Length == 0) {
                                    return;
                                }
                                for (int z = 0; z < array.Length; z++) {
                                    if (array[z] == storage[k].itemId && storage[k].count >= 1) {
                                        int inc = split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                        __instance.SetNewItem(storage[k].itemId, 1, (short)inc);
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        [HarmonyPrefix]
        public static void GameTickPatch(ref DefenseSystem __instance)
        {
            for (int i = 0; i < __instance.turrets.buffer.Length; i++) {
                if (__instance.factory.entityPool[__instance.turrets.buffer[i].entityId].protoId == ProtoID.I重型电浆炮) {
                    OrbitalTurretInternalUpdate(ref __instance.turrets.buffer[i], __instance.planet.id);
                }
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        [HarmonyPrefix]
        public static void CreateEntityLogicComponentsPatch(ref PlanetFactory __instance, int entityId)
        {
            int modelId = __instance.entityPool[entityId].modelIndex;
            if (modelId == ProtoID.M轨道观测站 || modelId == ProtoID.M天枢座) {
                Vector3 thisPos = __instance.entityPool[entityId].pos;
                int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
                int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
                OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                // 在赤道上/下圈？号位置添加轨道设施
                if (modelId == ProtoID.M轨道观测站) {
                    planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, entityId, StationType.TurretBase);
                } else if (modelId == ProtoID.M天枢座) {
                    planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, entityId, StationType.GlobalSupportBase);
                }
            }
        }

        public static void BuildOrbitalDefense(DefenseSystem __instance, int thisTurretId, int thisEntityId, int itemId)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
            int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加轨道设施
            if (itemId == ProtoID.I重型电浆炮) {
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalCore(position, thisTurretId, StationType.TurretCore);
            } else if (itemId == ProtoID.I星环护盾组件) {
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalCore(position, thisTurretId, StationType.ATFeildCore);
            }

        }

        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.NewTurretComponent))]
        [HarmonyPostfix]
        public static void NewTurretComponentPatch(ref DefenseSystem __instance, int entityId, PrefabDesc desc, int __result)
        {
            var itemId = __instance.factory.entityPool[entityId].protoId;
            if (itemId == ProtoID.I重型电浆炮) {
                BuildOrbitalDefense(__instance, __result, entityId, itemId);
            }
        }

        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.NewFieldGeneratorComponent))]
        [HarmonyPostfix]
        public static void NewFieldGeneratorComponentPatch(ref DefenseSystem __instance, int entityId, PrefabDesc desc, int __result)
        {
            var itemId = __instance.factory.entityPool[entityId].protoId;
            if (itemId == ProtoID.I星环护盾组件) {
                BuildOrbitalDefense(__instance, __result, entityId, itemId);
            }
        }
    }
}
