using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOrbitalRing.Patches.Logic.Farm
{
    internal class FarmAssembler
    {
        private static Dictionary<(int, int), int> NextAssemblerId = new Dictionary<(int, int), int>();

        private static readonly Dictionary<int, int> AutoBackfill = new Dictionary<int, int> {
            { 409, 1 },
            { 518, 2 },
            { 550, 1 },
            { 705, 1 },
            { 775, 1 },
            { 777, 2 },
            { 797, 1 },
            { 813, 1 }
        };

        // 连接时记录堆叠建筑的上下游关系
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ApplyInsertTarget))]
        [HarmonyPostfix]
        public static void ApplyInsertTargetPatch(ref PlanetFactory __instance, int entityId, int insertTarget, int slotId, int offset)
        {
            if (entityId == 0) {
                return;
            }
            if (insertTarget < 0) {
                Assert.CannotBeReached();
                insertTarget = 0;
            }

            int assemblerId = __instance.entityPool[entityId].assemblerId;
            int assemblerId2 = __instance.entityPool[insertTarget].assemblerId;
            if (assemblerId > 0 && assemblerId2 > 0) {
                if (assemblerId != 0 && __instance.factorySystem.assemblerPool[assemblerId].id == assemblerId) {
                    if (insertTarget == 0) {
                        if (NextAssemblerId.ContainsKey((__instance.planetId, assemblerId))) {
                            NextAssemblerId.Remove((__instance.planetId, assemblerId));
                        }
                        return;
                    }

                    NextAssemblerId[(__instance.planetId, assemblerId)] = __instance.entityPool[insertTarget].assemblerId;
                }
            }
        }

        // 拆除时清理堆叠建筑的记录
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ApplyEntityDisconnection))]
        [HarmonyPostfix]
        public static void ApplyEntityDisconnectionPatch(ref PlanetFactory __instance, int otherEntityId, int removingEntityId, int otherSlotId, int removingSlotId)
        {
            if (otherEntityId == 0) {
                return;
            }

            int assemblerId = __instance.entityPool[otherEntityId].assemblerId;
            if (assemblerId > 0) {
                int assemblerId2 = __instance.entityPool[removingEntityId].assemblerId;
                if (NextAssemblerId.ContainsKey((__instance.planetId, assemblerId)) && NextAssemblerId[(__instance.planetId, assemblerId)] == assemblerId2) {
                    NextAssemblerId.Remove((__instance.planetId, assemblerId));
                }
            }
        }

        // 生态温室，从爪子需求的量为三倍，多一倍给上传原料用
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))]
        [HarmonyPostfix]
        public static void AssemblerComponent_UpdateNeeds_Postfix(ref AssemblerComponent __instance)
        {
            if (__instance.recipeType != (ERecipeType)14) {
                return;
            }
            int[] requireCounts = __instance.recipeExecuteData.requireCounts;
            int[] requires = __instance.recipeExecuteData.requires;
            int num = requires.Length;
            int num2 = 3;
            __instance.needs[0] = ((0 < num && __instance.served[0] < requireCounts[0] * num2) ? requires[0] : 0);
            __instance.needs[1] = ((1 < num && __instance.served[1] < requireCounts[1] * num2) ? requires[1] : 0);
            __instance.needs[2] = ((2 < num && __instance.served[2] < requireCounts[2] * num2) ? requires[2] : 0);
            __instance.needs[3] = ((3 < num && __instance.served[3] < requireCounts[3] * num2) ? requires[3] : 0);
            __instance.needs[4] = ((4 < num && __instance.served[4] < requireCounts[4] * num2) ? requires[4] : 0);
            __instance.needs[5] = ((5 < num && __instance.served[5] < requireCounts[5] * num2) ? requires[5] : 0);
        }

        // 生态温室，上传原料下传产物
        public static void GameTickAssemblerOutputToNext(ref AssemblerComponent __instance, PlanetFactory factory)
        {
            if (!NextAssemblerId.ContainsKey((factory.planetId, __instance.id))) {
                return;
            }
            int nextAssemblerId = NextAssemblerId[(factory.planetId, __instance.id)];
            AssemblerComponent[] assemblerPool = factory.factorySystem.assemblerPool;
            if (assemblerPool[nextAssemblerId].id == 0 || assemblerPool[nextAssemblerId].id != nextAssemblerId) {
                Assert.CannotBeReached();
                NextAssemblerId.Remove((factory.planetId, __instance.id));
            }
            if (assemblerPool[nextAssemblerId].needs != null && __instance.recipeId == assemblerPool[nextAssemblerId].recipeId) {
                if (__instance.served != null && assemblerPool[nextAssemblerId].served != null) {
                    int[] array5 = (__instance.entityId > assemblerPool[nextAssemblerId].entityId) ? __instance.served : assemblerPool[nextAssemblerId].served;
                    int[] array6 = (__instance.entityId > assemblerPool[nextAssemblerId].entityId) ? assemblerPool[nextAssemblerId].served : __instance.served;
                    int[] array3 = array5;
                    lock (array3) {
                        int[] array4 = array6;
                        lock (array4) {
                            int num13 = __instance.served.Length;
                            int num14 = (__instance.recipeExecuteData.timeSpend > 5400000) ? 1 : (1 + __instance.speedOverride / 20000);
                            num14 = 0;
                            for (int i = 0; i < num13; i++) {
                                if (assemblerPool[nextAssemblerId].needs[i] == __instance.recipeExecuteData.requires[i] && __instance.served[i] > __instance.recipeExecuteData.requireCounts[i] * 2 + num14) {
                                    int num15 = __instance.served[i] - __instance.recipeExecuteData.requireCounts[i] * 2 - num14;
                                    if (num15 > 5) {
                                        num15 = 5;
                                    }
                                    int num16 = num15 * __instance.incServed[i] / __instance.served[i];
                                    __instance.served[i] -= num15;
                                    __instance.incServed[i] -= num16;
                                    assemblerPool[nextAssemblerId].served[i] += num15;
                                    assemblerPool[nextAssemblerId].incServed[i] += num16;
                                }
                            }
                        }
                    }
                }
                if (__instance.produced != null && assemblerPool[nextAssemblerId].produced != null) {
                    int[] array7 = (__instance.entityId > assemblerPool[nextAssemblerId].entityId) ? __instance.produced : assemblerPool[nextAssemblerId].produced;
                    int[] array8 = (__instance.entityId > assemblerPool[nextAssemblerId].entityId) ? assemblerPool[nextAssemblerId].produced : __instance.produced;
                    int[] array3 = array7;
                    lock (array3) {
                        int[] array4 = array8;
                        lock (array4) {
                            int num17;
                            for (int i = 0; i < __instance.produced.Length; i++) {
                                num17 = 2 * __instance.recipeExecuteData.productCounts[i];
                                if (__instance.produced[i] < num17 && assemblerPool[nextAssemblerId].produced[i] > 0) {
                                    if (AutoBackfill.ContainsKey(__instance.recipeId) && i == AutoBackfill[__instance.recipeId]) {
                                        if (assemblerPool[nextAssemblerId].produced[i] < num17 + 1) {
                                            continue;
                                        }
                                    }
                                    int num18 = (num17 - __instance.produced[i] < assemblerPool[nextAssemblerId].produced[i]) ? (num17 - __instance.produced[i]) : assemblerPool[nextAssemblerId].produced[i];
                                    __instance.produced[i] += num18;
                                    assemblerPool[nextAssemblerId].produced[i] -= num18;
                                }
                            }
                        }
                    }
                }
            }
        }


        public static void Export(BinaryWriter w)
        {
            w.Write(FarmAssembler.NextAssemblerId.Count);
            foreach (var Pair in FarmAssembler.NextAssemblerId) {
                w.Write(Pair.Key.Item1);
                w.Write(Pair.Key.Item2);
                w.Write(Pair.Value);
            }
        }

        // Token: 0x0600015D RID: 349 RVA: 0x0000FF30 File Offset: 0x0000E130
        public static void Import(BinaryReader r)
        {
            FarmAssembler.IntoOtherSave();
            try {
                int num = r.ReadInt32();
                for (int i = 0; i < num; i++) {
                    int item = r.ReadInt32();
                    int item2 = r.ReadInt32();
                    FarmAssembler.NextAssemblerId.TryAdd((item, item2), r.ReadInt32());
                }
            } catch (EndOfStreamException) {
            }
        }

        // Token: 0x0600015E RID: 350 RVA: 0x0000FFD4 File Offset: 0x0000E1D4
        public static void IntoOtherSave()
        {
            FarmAssembler.NextAssemblerId = new Dictionary<(int, int), int>();
        }


        

    }
}
