using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using static ProjectOrbitalRing.Patches.UI.UIAssemblerWindowPatch;

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
                case 775:
                case 778:
                case 800:
                case 624:
                    result = 7616; // 超净室空气过滤器
                    break;
                default:
                    result = 7617;
                    break;
            }
            return result;
        }

        // Token: 0x0600015C RID: 348 RVA: 0x0000FE60 File Offset: 0x0000E060
        public static void Export(BinaryWriter w)
        {
            w.Write(AssemblerModulePatches._AssemblerModuleData.Count);
            foreach (KeyValuePair<ValueTuple<int, int>, AssemblerModuleData> keyValuePair in AssemblerModulePatches._AssemblerModuleData) {
                w.Write(keyValuePair.Key.Item1);
                w.Write(keyValuePair.Key.Item2);
                w.Write(keyValuePair.Value.ItemId);
                w.Write(keyValuePair.Value.ItemCount);
                w.Write(keyValuePair.Value.ItemInc);
                w.Write(keyValuePair.Value.NeedCount);
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
                    AssemblerModulePatches._AssemblerModuleData.TryAdd(new ValueTuple<int, int>(item, item2), new AssemblerModuleData {
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
            AssemblerModulePatches._AssemblerModuleData = new ConcurrentDictionary<ValueTuple<int, int>, AssemblerModuleData>();
        }

        // Token: 0x06000160 RID: 352 RVA: 0x0000FFE8 File Offset: 0x0000E1E8
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), "TakeBackItems_Assembler")]
        public static void FactorySystem_TakeBackItems_Assembler(ref FactorySystem __instance, Player player, int asmId)
        {
            AssemblerModulePatches.SetEmpty(__instance.planet.id, asmId, true);
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
                            AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(__instance.factorySystem.planet.id, entityData.assemblerId);
                            AssemblerModuleData.NeedCount = 1;
                            int moduleId = AssemblerModulePatches.GetModuleId(ptr.recipeId);
                            if (AssemblerModuleData.ItemCount == 0) {
                                Player mainPlayer = GameMain.mainPlayer;
                                int handItemId = mainPlayer.inhandItemId;
                                if (handItemId != 0) {
                                    int itemCount = 1;
                                    int itemInc = 0;
                                    if ((handItemId == moduleId || moduleId == 7617 && handItemId == 7618)) {
                                        mainPlayer.TakeItemFromPlayer(ref handItemId, ref itemCount, out itemInc, fromPackage, itemBundle);

                                        if (itemCount > 0) {
                                            AssemblerModuleData.ItemId = mainPlayer.inhandItemId;
                                            AssemblerModuleData.ItemCount += itemCount;
                                            AssemblerModuleData.ItemInc = itemInc;
                                            AssemblerModulePatches.SetAssemblerModuleData(__instance.factorySystem.planet.id, entityData.assemblerId, AssemblerModuleData);
                                        }
                                    }
                                } else {

                                }
                            }


                            //bool flag5 = false;
                            //if (!flag5) {
                            //    Player mainPlayer = GameMain.mainPlayer;
                            //    bool flag6 = (moduleId == 7616 && moduleId != AssemblerModuleData.ItemId) || (moduleId == 7617 && AssemblerModuleData.ItemId != 7617 && AssemblerModuleData.ItemId != 7618);
                            //    if (flag6) {
                            //        int upCount = mainPlayer.TryAddItemToPackage(AssemblerModuleData.ItemId, AssemblerModuleData.ItemCount, AssemblerModuleData.ItemInc, true, 0);
                            //        AssemblerModuleData.ItemCount = 0;
                            //        UIItemup.Up(AssemblerModuleData.ItemId, upCount);
                            //    }
                            //    int num = AssemblerModuleData.NeedCount - AssemblerModuleData.ItemCount;
                            //    int itemInc = 0;
                            //    if (num > 0 && (mainPlayer.inhandItemId == moduleId || moduleId == 7617 && mainPlayer.inhandItemId == 7618)) {
                            //        int itemId = mainPlayer.inhandItemId;
                            //        mainPlayer.TakeItemFromPlayer(ref itemId, ref num, out itemInc, fromPackage, itemBundle);
                            //    }
                            //    bool flag8 = num > 0;
                            //    if (flag8) {
                            //        AssemblerModuleData.ItemId = mainPlayer.inhandItemId;
                            //        AssemblerModuleData.ItemCount += num;
                            //        AssemblerModuleData.ItemInc = itemInc;
                            //        AssemblerModulePatches.SetAssemblerModuleData(__instance.factorySystem.planet.id, entityData.assemblerId, AssemblerModuleData);
                            //    }
                            //}
                        }
                    }
                }
            }
        }

        // Token: 0x06000162 RID: 354 RVA: 0x000101A7 File Offset: 0x0000E3A7
        internal static void SyncLithography(ValueTuple<int, int> id, AssemblerModuleData AssemblerModuleData)
        {
            AssemblerModulePatches._AssemblerModuleData[id] = AssemblerModuleData;
        }

        // Token: 0x06000163 RID: 355 RVA: 0x000101B8 File Offset: 0x0000E3B8
        internal static AssemblerModuleData GetAssemblerModuleData(int planetId, int assemblerId)
        {
            ValueTuple<int, int> key = new ValueTuple<int, int>(planetId, assemblerId);
            bool flag = !AssemblerModulePatches._AssemblerModuleData.ContainsKey(key);
            if (flag) {
                AssemblerModulePatches._AssemblerModuleData[key] = new AssemblerModuleData();
            }
            return AssemblerModulePatches._AssemblerModuleData[key];
        }

        // Token: 0x06000164 RID: 356 RVA: 0x00010204 File Offset: 0x0000E404
        internal static void SetAssemblerModuleData(int planetId, int assemblerId, AssemblerModuleData data)
        {
            ValueTuple<int, int> key = new ValueTuple<int, int>(planetId, assemblerId);
            bool flag = !AssemblerModulePatches._AssemblerModuleData.ContainsKey(key);
            if (!flag) {
                AssemblerModulePatches._AssemblerModuleData[key] = data;
                //SyncAssemblerModuleData.Sync(planetId, assemblerId, AssemblerModulePatches._AssemblerModuleData[key]);
            }
        }

        // Token: 0x06000165 RID: 357 RVA: 0x00010250 File Offset: 0x0000E450
        internal static void SetEmpty(int planetId, int assemblerId, bool pop = true)
        {
            ValueTuple<int, int> key = new ValueTuple<int, int>(planetId, assemblerId);
            bool flag = !AssemblerModulePatches._AssemblerModuleData.ContainsKey(key);
            if (!flag) {
                AssemblerModuleData AssemblerModuleData = AssemblerModulePatches._AssemblerModuleData[key];
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
                    AssemblerModulePatches._AssemblerModuleData[key] = new AssemblerModuleData {
                        NeedCount = AssemblerModuleData.NeedCount
                    };
                    //SyncAssemblerModuleData.Sync(planetId, assemblerId, AssemblerModuleData);
                }
            }
        }

        // Token: 0x06000166 RID: 358 RVA: 0x00010334 File Offset: 0x0000E534
        public static void ExportPlanetData(int planetId, BinaryWriter w)
        {
            KeyValuePair<ValueTuple<int, int>, AssemblerModuleData>[] array = (from pair in AssemblerModulePatches._AssemblerModuleData
                                                                           where pair.Key.Item1 == planetId
                                                                           select pair).ToArray<KeyValuePair<ValueTuple<int, int>, AssemblerModuleData>>();
            w.Write(array.Length);
            w.Write(planetId);
            foreach (KeyValuePair<ValueTuple<int, int>, AssemblerModuleData> keyValuePair in array) {
                w.Write(keyValuePair.Key.Item2);
                w.Write(keyValuePair.Value.ItemId);
                w.Write(keyValuePair.Value.ItemCount);
                w.Write(keyValuePair.Value.ItemInc);
                w.Write(keyValuePair.Value.NeedCount);
            }
        }

        // Token: 0x06000167 RID: 359 RVA: 0x00010400 File Offset: 0x0000E600
        public static void ImportPlanetData(BinaryReader r)
        {
            int num = r.ReadInt32();
            int item = r.ReadInt32();
            for (int i = 0; i < num; i++) {
                int item2 = r.ReadInt32();
                AssemblerModulePatches._AssemblerModuleData[new ValueTuple<int, int>(item, item2)] = new AssemblerModuleData {
                    ItemId = r.ReadInt32(),
                    ItemCount = r.ReadInt32(),
                    ItemInc = r.ReadInt32(),
                    NeedCount = r.ReadInt32()
                };
            }
        }

        // Token: 0x040000D2 RID: 210
        private static ConcurrentDictionary<ValueTuple<int, int>, AssemblerModuleData> _AssemblerModuleData = new ConcurrentDictionary<ValueTuple<int, int>, AssemblerModuleData>();

        public static void AssemblerFilterModuleProcess(FactorySystem factorySystem, int poolId, ref float power)
        {
            if (factorySystem.assemblerPool[poolId].speed == 40000) {
                return;
            }
            AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(factorySystem.planet.id, poolId);
            if (GetModuleId(factorySystem.assemblerPool[poolId].recipeId) == 7616) {
                if (AssemblerModuleData.ItemCount == 0) {
                    power = 0.0f;
                }
            }
        }

        private static void MakeAssemblerExtra(ref AssemblerComponent __instance, int incLevel)
        {
            if (__instance.productive && !__instance.forceAccMode) {
                __instance.extraSpeed = (int)((double)__instance.speed * Cargo.incTableMilli[incLevel] * 10.0 + 0.1);
                __instance.speedOverride = __instance.speed;
                __instance.extraPowerRatio = Cargo.powerTable[incLevel];
            } else {
                __instance.extraSpeed = 0;
                __instance.speedOverride = (int)((double)__instance.speed * (1.0 + Cargo.accTableMilli[incLevel]) + 0.1);
                __instance.extraPowerRatio = Cargo.powerTable[incLevel];
            }
        }

        public static void AssemblerModuleProcess(PlanetFactory factory, ref AssemblerComponent __instance)
        {
            BioChemicalProcess(factory.planet.theme, ref __instance);
            AssemblerExtraProcess(factory, ref __instance);
        }

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
                AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(factory.planet.id, __instance.id);
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
