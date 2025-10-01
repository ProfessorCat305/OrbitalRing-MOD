using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using UnityEngine;
using System.Reflection.Emit;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalAssembler
    {
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        [HarmonyPrefix]
        public static void InternalUpdatePatch(ref EjectorComponent __instance)
        {
            if (__instance.bulletCount >= 20) return;
            if (__instance.coldSpend > 60000) return;
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planetId);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++)
            {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++)
                {
                    var pair = ring.GetPair(j);
                    if (pair.OrbitalCorePoolId == __instance.id && pair.elevatorPoolId != -1)
                    {
                        var storage = ring.GetElevatorStorage(j);
                        for (int k = 0; k < storage.Length; k++)
                        {
                            if (storage[k].itemId == __instance.bulletId && storage[k].count >= 1)
                            {
                                __instance.bulletCount += 1;
                                int inc = OrbitalStationManager.Instance.split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                __instance.bulletInc += inc;
                            }
                        }
                    }

                }
            }
        }


        public static void CheckParticleColliderShouldRunning(FactorySystem factorySystem, int poolId, ref float power)
        {
            if (factorySystem.assemblerPool[poolId].recipeType == ERecipeType.Particle && factorySystem.assemblerPool[poolId].speed >= 100000)
            {
                Vector3 pos = factorySystem.factory.entityPool[factorySystem.assemblerPool[poolId].entityId].pos;
                int ringIndex = isBuildingPosYCorrect(pos);
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factorySystem.planet.id);
                if (planetOrbitalRingData != null)
                {
                    if (!planetOrbitalRingData.Rings[ringIndex].IsFull())
                    {
                        // 星环不完整，星环粒子对撞机不运行
                        power = 0.0f;
                    }
                }
            }
        }

        // 两个IL补丁，负责在星环不完整时拦截粒子对撞机的运行
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick), typeof(long), typeof(bool))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FactorySystem_isParticleColliderRunning_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.assemblerPool))));
            object V_28 = matcher.Advance(1).Operand; // 变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))));
            object V_32 = matcher.Advance(2).Operand; // 变量索引

            matcher.Advance(-3).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, V_28),
                new CodeInstruction(OpCodes.Ldloca_S, V_32),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalAssembler), nameof(CheckParticleColliderShouldRunning),
                new System.Type[] {
                    typeof(FactorySystem),
                    typeof(int),    // ref int
                    typeof(float).MakeByRefType(),  // ref float
                }
            )));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), nameof(FactorySystem.assemblerPool))));
            object V_33 = matcher.Advance(1).Operand; // 变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))));
            object V_36 = matcher.Advance(2).Operand; // 变量索引

            matcher.Advance(-3).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, V_33),
                new CodeInstruction(OpCodes.Ldloca_S, V_36),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalAssembler), nameof(CheckParticleColliderShouldRunning),
                new System.Type[] {
                    typeof(FactorySystem),
                    typeof(int),    // ref int
                    typeof(float).MakeByRefType(),  // ref float
                }
            )));

            return matcher.InstructionEnumeration();
        }

        public static void OrbitalAssemblerInternalUpdate(ref AssemblerComponent __instance, int planetId)
        {
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++)
            {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++)
                {
                    var pair = ring.GetPair(j);
                    if (pair.stationType == StationType.Assembler && pair.elevatorPoolId != -1 && pair.OrbitalStationPoolId == __instance.id)
                    {
                        var storage = ring.GetElevatorStorage(j);
                        for (int k = 0; k < storage.Length; k++)
                        {
                            for (int needIdx = 0; needIdx < __instance.needs.Length; needIdx++)
                            {
                                if (storage[k].itemId == __instance.needs[needIdx] && storage[k].count >= 4)
                                {
                                    __instance.served[needIdx] += 4;
                                    //storage[k].count -= 1;
                                    int inc = OrbitalStationManager.Instance.split_inc(ref storage[k].count, ref storage[k].inc, 4);
                                    __instance.incServed[needIdx] += inc;
                                    //storage[k].inc -= inc;
                                }
                            }
                            for (int productIndex = 0; productIndex < __instance.products.Length; productIndex++)
                            {
                                if (storage[k].itemId == __instance.products[productIndex] && __instance.produced[productIndex] > 0 && storage[k].count <= storage[k].max)
                                {
                                    __instance.produced[productIndex] -= 1;
                                    storage[k].count += 1;
                                    if (__instance.recipeType == ERecipeType.Smelt)
                                    {
                                        storage[k].inc += 4;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void BuildOrbitalAssembler(FactorySystem __instance, int thisAssemblerId, int thisEntityId, int itemId)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z);
            int ringIndex = isBuildingPosYCorrect(thisPos);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加轨道设施
            if (itemId != 6513)
            {
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, thisAssemblerId, StationType.Assembler);
            } else { // 轨道弹射器
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalCore(position, thisAssemblerId, StationType.EjectorCore);
            }
            if (itemId == 6265)
            {
                planetOrbitalRingData.Rings[ringIndex].isParticleCollider = true;
            }
        }

        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewAssemblerComponent))]
        [HarmonyPostfix]
        public static void NewAssemblerComponentPostPatch(ref FactorySystem __instance, int __result, int entityId)
        {
            var itemId = __instance.factory.entityPool[__instance.assemblerPool[__result].entityId].protoId;
            if (itemId == 6257 || itemId == 6501 || itemId == 6265)
            {
                BuildOrbitalAssembler(__instance, __result, entityId, itemId);
            }
        }

        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewEjectorComponent))]
        [HarmonyPostfix]
        public static void NewEjectorComponentPostPatch(ref FactorySystem __instance, int __result, int entityId)
        {
            var itemId = __instance.factory.entityPool[__instance.ejectorPool[__result].entityId].protoId;
            if (itemId == 6513)
            {
                BuildOrbitalAssembler(__instance, __result, entityId, itemId);
            }
        }
    }
}
