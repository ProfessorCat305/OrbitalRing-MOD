using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using static ProjectOrbitalRing.Patches.UI.UIAssemblerWindowPatch;
using static UIPlayerDeliveryPanel;
using static UnityEngine.PostProcessing.MotionBlurComponent.FrameBlendingFilter;
using System.Collections;
using System.Threading;

namespace ProjectOrbitalRing.Patches.Logic.AssemblerModule
{
    // Token: 0x02000038 RID: 56
    public static class AssemblerModulePatches
    {
        internal static int GetIncLevel(int moduleItemId)
        {
            if (moduleItemId == 7617) {
                return 2;
            } else if (moduleItemId == 7618) {
                return 4;
            }
            return 0;
        }

        private static int[] incModuleExtraPowerRatioTable = new int[]
        {
            0,
            0,
            84000,
            0,
            210000
        };

        internal static int GetModuleId(int recipeId)
        {
            int result;
            switch (recipeId) {
                case 51:
                case 54:
                case 508:
                case 26:
                case 710:
                case 29:
                case 52:
                case 521:
                    // 下面的是三级种田
                case 775:
                case 778:
                case 847:
                case 848:
                case 784:
                    result = 7616; // 超净室空气过滤器
                    break;
                default:
                    result = 7617;
                    break;
            }
            return result;
        }



        private static Dictionary<ValueTuple<int, int>, AssemblerModuleData> ImportAssemblerModuleData = new Dictionary<ValueTuple<int, int>, AssemblerModuleData>();
        private static ConcurrentDictionary<FactorySystem, ConcurrentDictionary<int, AssemblerModuleData>> AssemblerModuleData = new ConcurrentDictionary<FactorySystem, ConcurrentDictionary<int, AssemblerModuleData>>();

        public static readonly ReaderWriterLockSlim AssemblerDataLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly ConcurrentDictionary<int, object> _instanceLocks = new ConcurrentDictionary<int, object>();

        // 辅助方法：获取/创建__instance的专属锁（原子操作，确保锁唯一）
        private static object GetInstanceLock(int assemblerId)
        {
            return _instanceLocks.GetOrAdd(assemblerId, id => new object());
        }

        // Token: 0x0600015C RID: 348 RVA: 0x0000FE60 File Offset: 0x0000E060
        public static void Export(BinaryWriter w)
        {
            int count = 0;
            foreach (var factoryPair in AssemblerModulePatches.AssemblerModuleData) {
                count += factoryPair.Value.Count;
            }
            w.Write(count);
            foreach (var factoryPair in AssemblerModulePatches.AssemblerModuleData) {
                foreach (var assemblerPair in factoryPair.Value) {
                    w.Write(factoryPair.Key.planet.id);
                    w.Write(assemblerPair.Key);
                    w.Write(assemblerPair.Value.ItemId);
                    w.Write(assemblerPair.Value.ItemCount);
                    w.Write(assemblerPair.Value.ItemInc);
                    w.Write(assemblerPair.Value.NeedCount);
                }
            }
        }

        // Token: 0x0600015D RID: 349 RVA: 0x0000FF30 File Offset: 0x0000E130
        public static void Import(BinaryReader r)
        {
            AssemblerModulePatches.ReInitAll();
            try {
                int num = r.ReadInt32();
                for (int i = 0; i < num; i++) {
                    int item = r.ReadInt32();
                    int item2 = r.ReadInt32();
                    AssemblerModulePatches.ImportAssemblerModuleData.TryAdd(new ValueTuple<int, int>(item, item2), new AssemblerModuleData {
                        ItemId = r.ReadInt32(),
                        ItemCount = r.ReadInt32(),
                        ItemInc = r.ReadInt32(),
                        NeedCount = r.ReadInt32()
                    });
                }
            } catch (EndOfStreamException) {
            }
        }

        // Token: 0x0600015E RID: 350 RVA: 0x0000FFD4 File Offset: 0x0000E1D4
        public static void IntoOtherSave()
        {
            AssemblerModulePatches.ReInitAll();
        }

        // Token: 0x0600015F RID: 351 RVA: 0x0000FFDC File Offset: 0x0000E1DC
        private static void ReInitAll()
        {
            AssemblerModulePatches.ImportAssemblerModuleData = new Dictionary<ValueTuple<int, int>, AssemblerModuleData>();
            AssemblerModulePatches.AssemblerModuleData = new ConcurrentDictionary<FactorySystem, ConcurrentDictionary<int, AssemblerModuleData>>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        public static void GameData_Import_AssemblerModulePatch(GameData __instance)
        {
            List<int> planetId = new List<int>();
            FactorySystem factorySystem = null;
            foreach (var pair in AssemblerModulePatches.ImportAssemblerModuleData) {
                factorySystem = __instance.galaxy.PlanetById(pair.Key.Item1).factory.factorySystem;
                if (!planetId.Contains(pair.Key.Item1)) {
                    planetId.Add(pair.Key.Item1);
                    AssemblerModuleData[factorySystem] = new ConcurrentDictionary<int, AssemblerModuleData>();
                }
                AssemblerModuleData[factorySystem][pair.Key.Item2] = new AssemblerModuleData {
                    ItemId = pair.Value.ItemId,
                    ItemCount = pair.Value.ItemCount,
                    ItemInc = pair.Value.ItemInc,
                    NeedCount = pair.Value.NeedCount
                };
                
            }
            AssemblerModulePatches.ImportAssemblerModuleData = null;
        }

        // Token: 0x06000160 RID: 352 RVA: 0x0000FFE8 File Offset: 0x0000E1E8
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), "TakeBackItems_Assembler")]
        public static void FactorySystem_TakeBackItems_Assembler(ref FactorySystem __instance, Player player, int asmId)
        {
            AssemblerModulePatches.SetEmpty(__instance, asmId, true);
        }

        // Token: 0x06000161 RID: 353 RVA: 0x00010000 File Offset: 0x0000E200
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), "EntityFastFillIn")]
        public static void PlanetFactory_EntityFastFillIn_Postfix(PlanetFactory __instance, int entityId, bool fromPackage, ItemBundle itemBundle)
        {
            bool flag = __instance.factorySystem == null;
            if (!flag) {
                EntityData entityData = __instance.entityPool[entityId];
                bool flag2 = entityData.id != entityId;
                if (!flag2) {
                    ref AssemblerComponent ptr = ref __instance.factorySystem.assemblerPool[entityData.assemblerId];
                    bool flag3 = ptr.id != entityData.assemblerId;
                    if (!flag3) {
                        bool flag4 = ShouldModuleButtonActive(ptr.recipeType, ptr.recipeId, ptr.speed);
                        if (flag4) {
                            AssemblerModuleData AssemblerModuleData = GetAssemblerModuleData(__instance.factorySystem, entityData.assemblerId);
                            AssemblerModuleData.NeedCount = 1;
                            int moduleId = AssemblerModulePatches.GetModuleId(ptr.recipeId);
                            if (AssemblerModuleData.ItemCount == 0) {
                                Player mainPlayer = GameMain.mainPlayer;
                                int handItemId = mainPlayer.inhandItemId;
                                if (handItemId != 0 && mainPlayer.inhandItemCount > 0) {
                                    int itemCount = 1;
                                    int itemInc = 0;
                                    if ((handItemId == moduleId || moduleId == 7617 && handItemId == 7618)) {
                                        mainPlayer.TakeItemFromPlayer(ref handItemId, ref itemCount, out itemInc, fromPackage, itemBundle);
                                        if (itemCount > 0) {
                                            AssemblerModuleData.ItemId = handItemId;
                                            AssemblerModuleData.ItemCount += itemCount;
                                            AssemblerModuleData.ItemInc = itemInc;
                                            SetAssemblerModuleData(__instance.factorySystem, entityData.assemblerId, AssemblerModuleData);
                                        }
                                    }
                                } else {

                                }
                            }
                        }
                    }
                }
            }
        }

        // Token: 0x06000162 RID: 354 RVA: 0x000101A7 File Offset: 0x0000E3A7
        //internal static void SyncLithography(ValueTuple<int, int> id, AssemblerModuleData AssemblerModuleData)
        //{
        //    AssemblerModulePatches.AssemblerModuleData[id] = AssemblerModuleData;
        //}

        // Token: 0x06000163 RID: 355 RVA: 0x000101B8 File Offset: 0x0000E3B8
        internal static AssemblerModuleData GetAssemblerModuleData(FactorySystem factorySystem, int assemblerId)
        {
            // 原子操作：获取或创建 factorySystem 对应的字典
            var assemblerDict = AssemblerModuleData.GetOrAdd(
                factorySystem,
                _ => new ConcurrentDictionary<int, AssemblerModuleData>()
            ); 

            // 原子操作：获取或创建 assemblerId 对应的数据
            return assemblerDict.GetOrAdd(
                assemblerId,
                _ => new AssemblerModuleData()
            );
        }

        // Token: 0x06000164 RID: 356 RVA: 0x00010204 File Offset: 0x0000E404
        internal static void SetAssemblerModuleData(FactorySystem factorySystem, int assemblerId, AssemblerModuleData data)
        {
            if (AssemblerModuleData.ContainsKey(factorySystem)) {
                if (AssemblerModuleData[factorySystem].ContainsKey(assemblerId)) {
                    AssemblerModuleData[factorySystem][assemblerId] = data;
                }
                //SyncAssemblerModuleData.Sync(planetId, assemblerId, AssemblerModulePatches.AssemblerModuleData[key]);
            }
        }

        // Token: 0x06000165 RID: 357 RVA: 0x00010250 File Offset: 0x0000E450
        internal static void SetEmpty(FactorySystem factorySystem, int assemblerId, bool pop = true)
        {
            if (AssemblerModuleData.ContainsKey(factorySystem)) {
                if (!AssemblerModulePatches.AssemblerModuleData[factorySystem].ContainsKey(assemblerId)) {
                    return;
                }
                AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.AssemblerModuleData[factorySystem][assemblerId];
                bool flag2 = AssemblerModuleData.ItemId == 0 || AssemblerModuleData.ItemCount == 0;
                if (!flag2) {
                    Player mainPlayer = GameMain.mainPlayer;
                    if (pop) {
                        int upCount = mainPlayer.TryAddItemToPackage(AssemblerModuleData.ItemId, AssemblerModuleData.ItemCount, AssemblerModuleData.ItemInc, true, 0);
                        UIItemup.Up(AssemblerModuleData.ItemId, upCount);
                    } else {
                        mainPlayer.SetHandItemId_Unsafe(AssemblerModuleData.ItemId);
                        mainPlayer.SetHandItemCount_Unsafe(AssemblerModuleData.ItemCount);
                        mainPlayer.SetHandItemInc_Unsafe(AssemblerModuleData.ItemInc);
                    }
                    AssemblerModulePatches.AssemblerModuleData[factorySystem][assemblerId] = new AssemblerModuleData {
                        NeedCount = AssemblerModuleData.NeedCount
                    };
                    //SyncAssemblerModuleData.Sync(planetId, assemblerId, AssemblerModuleData);
                }
            }
        }
        
        public static void AssemblerFilterModuleProcess(FactorySystem factorySystem, int poolId, ref float power)
        {
            if (factorySystem.assemblerPool[poolId].speed >= 40000) {
                if (factorySystem.assemblerPool[poolId].recipeType != (ERecipeType)14) {
                    return;
                }
            }
            AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(factorySystem, poolId);
            if (GetModuleId(factorySystem.assemblerPool[poolId].recipeId) == 7616 && factorySystem.assemblerPool[poolId].speed < 40000) {
                if (AssemblerModuleData.ItemCount == 0) {
                    power = 0.0f;
                }
            }
        }

        private static void MakeAssemblerExtra(ref AssemblerComponent __instance, int incLevel)
        {
            if (__instance.recipeExecuteData.productive && !__instance.forceAccMode) {
                __instance.extraSpeed = (int)((double)__instance.speed * Cargo.incTableMilli[incLevel] * 10.0 + 0.1);
                __instance.speedOverride = __instance.speed;
                __instance.extraPowerRatio = Cargo.powerTable[incLevel];
            } else {
                __instance.extraSpeed = 0;
                __instance.speedOverride = (int)((double)__instance.speed * (1.0 + Cargo.accTableMilli[incLevel]) + 0.1);
                __instance.extraPowerRatio = Cargo.powerTable[incLevel];
            }
        }

        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AssemblerComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(AssemblerComponent), nameof(AssemblerComponent.replicating))));

            //matcher.Advance(10).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarga, (byte)0));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(AssemblerModulePatches), nameof(AssemblerModuleProcess))));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        public static void AssemblerModuleProcess(ref AssemblerComponent __instance)
        {
            //if (__instance == null) return;

            // ========== 关键1：锁内获取实例锁，避免id变化导致锁不匹配 ==========
            object instanceLock = null;
            int stableAssemblerId = -1;
            // 临时锁保护id读取和锁创建
            lock (typeof(AssemblerModulePatches)) {
                stableAssemblerId = __instance.id;
                instanceLock = AssemblerModulePatches.GetInstanceLock(stableAssemblerId);
            }

            // ========== 关键2：实例锁包裹所有__instance操作，且先校验基础条件 ==========
            lock (instanceLock) {
                // 再次校验id是否一致（防止中途被改）
                if (__instance.id != stableAssemblerId) {
                    LogError($"assemblerId已变化，旧：{stableAssemblerId} → 新：{__instance.id}，放弃处理");
                    return;
                }

                // 基础条件校验（锁内执行，确保原子性）
                if (__instance.recipeType != ERecipeType.Assemble &&
                    __instance.recipeType != (ERecipeType)10 &&
                    __instance.recipeType != (ERecipeType)12 &&
                    __instance.recipeType != (ERecipeType)14) {
                    return;
                }

                // ========== 关键3：共享资源加读锁，避免遍历异常 ==========
                try {
                    AssemblerModulePatches.AssemblerDataLock.EnterReadLock();
                    try {
                        // ========== 关键4：替换return为continue，保留遍历逻辑 ==========
                        foreach (var planetValuePair in AssemblerModulePatches.AssemblerModuleData) {
                            // 再次校验id（遍历过程中可能被改）
                            if (__instance.id != stableAssemblerId) break;

                            // 关键5：消除ContainsKey和索引访问的竞态
                            if (!planetValuePair.Value.TryGetValue(__instance.id, out _)) {
                                continue; // 不再return，仅跳过当前项
                            }

                            // 关键6：先校验assemblerPool是否存在该id，再访问
                            //if (!planetValuePair.Key.assemblerPool.ContainsKey(__instance.id)) {
                            //    continue;
                            //}

                            if (planetValuePair.Key.assemblerPool[__instance.id].Equals(__instance)) {
                                // 所有修改__instance的逻辑都在锁内，确保原子性
                                BioChemicalProcess(planetValuePair.Key.factory.planet.theme, ref __instance);
                                AssemblerExtraProcess(planetValuePair.Key.factory, ref __instance);
                            }
                        }
                    } finally {
                        if (AssemblerModulePatches.AssemblerDataLock.IsReadLockHeld) {
                            AssemblerModulePatches.AssemblerDataLock.ExitReadLock();
                        }
                    }
                } catch (Exception ex) {
                    LogError($"处理assemblerId {stableAssemblerId} 异常：{ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // 102 653   102 232

        private static void BioChemicalProcess(int theme, ref AssemblerComponent __instance)
        {
            // 樱林海
            if (theme == 18) {
                if (__instance.recipeType == (ERecipeType)14) {
                    if (__instance.replicating == true) {
                        if (__instance.extraPowerRatio < Cargo.powerTable[4]) {
                            MakeAssemblerExtra(ref __instance, 4);
                        }
                    }
                }
            }
        }

        private static void AssemblerExtraProcess(PlanetFactory factory, ref AssemblerComponent __instance)
        {
            if ((__instance.recipeType == ERecipeType.Assemble || __instance.recipeType == (ERecipeType)10 || __instance.recipeType == (ERecipeType)12) &&
                __instance.speed < 40000) {


                int PowerRatio = 1000;
                if (factory.planet.theme == 7 || factory.planet.theme == 10 || factory.planet.theme == 20 || factory.planet.theme == 24) {
                    PowerRatio = 500;
                }
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
                int orbitalRingIncLevel = planetOrbitalRingData.planetIncLevel;
                AssemblerModuleData AssemblerModuleData = GetAssemblerModuleData(factory.factorySystem, __instance.id);
                if ((AssemblerModuleData.ItemId == 0 || AssemblerModuleData.ItemCount == 0) && orbitalRingIncLevel == 0) {
                    return;
                }
                int ModuleIncLevel = GetIncLevel(AssemblerModuleData.ItemId);
                int incLevel = Math.Max(ModuleIncLevel, orbitalRingIncLevel);
                if (__instance.replicating == true) {
                    if (__instance.extraPowerRatio < Cargo.powerTable[incLevel]) {
                        MakeAssemblerExtra(ref __instance, incLevel);
                        if (ModuleIncLevel > orbitalRingIncLevel) {
                            __instance.extraPowerRatio += (int)((double)incModuleExtraPowerRatioTable[incLevel] / factory.powerSystem.consumerPool[__instance.pcId].workEnergyPerTick * PowerRatio);
                        }
                    }
                }
            }
        }
    }
}
