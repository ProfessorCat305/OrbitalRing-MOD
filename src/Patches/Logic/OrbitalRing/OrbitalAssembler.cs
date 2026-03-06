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
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;
using ProjectOrbitalRing.Utils;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;

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
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    if (pair.OrbitalCorePoolId == __instance.id && pair.elevatorPoolId != -1) {
                        var storage = ring.GetElevatorStorage(j);
                        for (int k = 0; k < storage.Length; k++) {
                            if (storage[k].itemId == __instance.bulletId && storage[k].count >= 1) {
                                __instance.bulletCount += 1;
                                int inc = split_inc(ref storage[k].count, ref storage[k].inc, 1);
                                __instance.bulletInc += inc;
                            }
                        }
                    }

                }
            }
        }


        public static void CheckParticleColliderShouldRunning(FactorySystem factorySystem, int poolId, ref float power)
        {
            //LogError($"CheckParticleColliderShouldRunning poolId {poolId} power {power}");
            //LogError($"CheckParticleColliderShouldRunning recipeType {factorySystem.assemblerPool[poolId].recipeType} speed {factorySystem.assemblerPool[poolId].speed}");
            //if (factorySystem.assemblerPool[poolId].recipeType == ERecipeType.Particle && factorySystem.assemblerPool[poolId].speed >= 100000) {
            EntityData entityData = factorySystem.factory.entityPool[factorySystem.assemblerPool[poolId].entityId];
            if (entityData.protoId == ProtoID.I星环对撞机) { 
                //LogError($"CheckParticleColliderShouldRunning");
                Vector3 pos = entityData.pos;
                int ringIndex = isBuildingPosYCorrect(pos, (factorySystem.planet.radius == 100f));
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factorySystem.planet.id);
                if (planetOrbitalRingData != null) {
                    //LogError($" {ringIndex} ring not complete IsOneFull {planetOrbitalRingData.Rings[ringIndex].IsOneFull()} isLowInsideRingComplete {planetOrbitalRingData.Rings[ringIndex].isLowInsideRingComplete}");
                    if (!planetOrbitalRingData.Rings[ringIndex].IsOneFull() || !planetOrbitalRingData.Rings[ringIndex].isLowInsideRingComplete) {
                        // 星环不完整，星环粒子对撞机不运行
                        power = 0.0f;
                    }
                }
            } else {
                AssemblerModulePatches.AssemblerFilterModuleProcess(factorySystem, poolId, ref power);
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

        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._assembler_parallel))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GameLogic_assembler_parallel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelem_Ref), new CodeMatch(OpCodes.Stloc_S));
            object V_6 = matcher.Advance(1).Operand; // planetFactory变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelema));
            object V_22 = matcher.Advance(-1).Operand; // poolId变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerConsumerComponent), nameof(PowerConsumerComponent.networkId))));
            object V_26 = matcher.Advance(2).Operand; // power变量索引

            matcher.Advance(4).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, V_6), new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeInstruction(OpCodes.Ldloc_S, V_22),
                new CodeInstruction(OpCodes.Ldloca_S, V_26),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalAssembler), nameof(CheckParticleColliderShouldRunning),
                new System.Type[] {
                    typeof(FactorySystem),
                    typeof(int),    // ref int
                    typeof(float).MakeByRefType(),  // ref float
                }
            )));

            matcher.Advance(5);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PowerConsumerComponent), nameof(PowerConsumerComponent.networkId))));
            object V_27 = matcher.Advance(2).Operand; // power变量索引

            matcher.Advance(4).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, V_6), new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetFactory), nameof(PlanetFactory.factorySystem))),
                new CodeInstruction(OpCodes.Ldloc_S, V_22),
                new CodeInstruction(OpCodes.Ldloca_S, V_27),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalAssembler), nameof(CheckParticleColliderShouldRunning),
                new System.Type[] {
                    typeof(FactorySystem),
                    typeof(int),    // ref int
                    typeof(float).MakeByRefType(),  // ref float
                }
            )));
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        public static void OrbitalAssemblerInternalUpdate(ref AssemblerComponent __instance, int planetId)
        {
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planetOrbitalRingData == null) return;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    if (pair.stationType == StationType.Assembler && pair.elevatorPoolId != -1 && pair.OrbitalStationPoolId == __instance.id) {
                        var storage = ring.GetElevatorStorage(j);
                        for (int z = 0; z < storage.Length; z++) {
                            for (int needIdx = 0; needIdx < __instance.needs.Length; needIdx++) {
                                if (storage[z].itemId == __instance.needs[needIdx] && storage[z].count >= 4) {
                                    __instance.served[needIdx] += 4;
                                    //storage[k].count -= 1;
                                    int inc = split_inc(ref storage[z].count, ref storage[z].inc, 4);
                                    __instance.incServed[needIdx] += inc;
                                    //storage[k].inc -= inc;
                                    // 当星环对撞机执行起义物质配方时，记录输入的电池增产的值
                                    if (__instance.recipeId == 104 && storage[z].itemId == 2207) {
                                        ValueTuple<int, int> key = new ValueTuple<int, int>(planetId, __instance.id);
                                        MoonPatch.ColliderAccumulatorIncData.AddOrUpdate(key, inc, (k, v) => v + inc);
                                    }
                                }
                            }
                            for (int productIndex = 0; productIndex < __instance.recipeExecuteData.products.Length; productIndex++) {
                                if (storage[z].itemId == __instance.recipeExecuteData.products[productIndex] && __instance.produced[productIndex] > 0 && storage[z].count <= storage[z].max) {
                                    __instance.produced[productIndex] -= 1;
                                    storage[z].count += 1;
                                    if (__instance.recipeType == ERecipeType.Smelt) {
                                        storage[z].inc += 4;
                                    }
                                    // 当星环对撞机执行起义物质配方时，按记录输入的电池增产的值赋给输出的空电池增产
                                    if (__instance.recipeId == 104 && storage[z].itemId == 2206) {
                                        ValueTuple<int, int> key = new ValueTuple<int, int>(planetId, __instance.id);
                                        bool flag = MoonPatch.ColliderAccumulatorIncData.ContainsKey(key) && MoonPatch.ColliderAccumulatorIncData[key] >= 4;
                                        if (flag) {
                                            MoonPatch.ColliderAccumulatorIncData[key] -= 4;
                                            storage[z].inc += 4;
                                            if (MoonPatch.ColliderAccumulatorIncData[key] < 0) {
                                                MoonPatch.ColliderAccumulatorIncData[key] = 0;
                                            }
                                        }
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
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true, __instance.planet.radius == 100f);
            int ringIndex = isBuildingPosYCorrect(thisPos, __instance.planet.radius == 100f);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加轨道设施
            if (itemId != ProtoID.I重型电磁弹射器) {
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, thisAssemblerId, StationType.Assembler);
            } else { // 轨道弹射器
                planetOrbitalRingData.Rings[ringIndex].AddOrbitalCore(position, thisAssemblerId, StationType.EjectorCore);
            }
            if (itemId == ProtoID.I星环对撞机) {
                planetOrbitalRingData.Rings[ringIndex].isParticleCollider = true;
            }
        }

        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewAssemblerComponent))]
        [HarmonyPostfix]
        public static void NewAssemblerComponentPostPatch(ref FactorySystem __instance, int __result, int entityId)
        {
            var itemId = __instance.factory.entityPool[__instance.assemblerPool[__result].entityId].protoId;
            if (itemId == ProtoID.I太空船坞 || itemId == ProtoID.I轨道熔炼站 || itemId == ProtoID.I星环对撞机) {
                BuildOrbitalAssembler(__instance, __result, entityId, itemId);
            }

            if (itemId == ProtoID.I星环对撞机) {
                if (__instance.planet.radius == 100f) {
                    __instance.assemblerPool[__result].speed = 250000;
                }
            }
        }

        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewEjectorComponent))]
        [HarmonyPostfix]
        public static void NewEjectorComponentPostPatch(ref FactorySystem __instance, int __result, int entityId)
        {
            var itemId = __instance.factory.entityPool[__instance.ejectorPool[__result].entityId].protoId;
            if (itemId == ProtoID.I重型电磁弹射器) {
                BuildOrbitalAssembler(__instance, __result, entityId, itemId);
            }
        }

        // 更新了recipeExecuteData后，修改recipeExecuteData会导致所有使用recipeExecuteData的配方全都被修改，所以把配方改为30倍堆叠生产的此函数暂时废弃
        public static void CheckRecipeCount(ref AssemblerComponent __instance, int ProductionMultiplier, bool isMoon)
        {
            ProductionMultiplier = isMoon ? (ProductionMultiplier / 2) : ProductionMultiplier;
            RecipeProto recipeProto = null;
            if (__instance.recipeId != 0) {
                recipeProto = LDB.recipes.Select(__instance.recipeId);
                if (__instance.recipeExecuteData.requireCounts[0] == recipeProto.ItemCounts[0]) {
                    for (int i = 0; i < __instance.recipeExecuteData.requireCounts.Length; i++) {
                        __instance.recipeExecuteData.requireCounts[i] *= ProductionMultiplier;
                    }
                    for (int i = 0; i < __instance.recipeExecuteData.productCounts.Length; i++) {
                        __instance.recipeExecuteData.productCounts[i] *= ProductionMultiplier;
                    }
                }
            }
        }

        
    }
}
