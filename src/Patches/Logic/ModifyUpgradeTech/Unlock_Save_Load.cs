using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using static ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech.ModifyUpgradeTech;
using static ProjectOrbitalRing.Utils.RandomUtils;
using ProjectOrbitalRing.Utils;
using static ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine.EnergyCalculate;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech
{
    internal class Unlock_Save_Load
    {
        private static readonly int[][] unlockWreckFallingItemIdLevel =
        {
            new int [] { 1108, 1109, 1112, 1202, 1301, 5206, },
            new int [] { 1103, 1105, 1111, 1131, 1203, 1401, },
            new int [] { 1106, 1115, 1123, 1204, 1404, 1407, },
            new int [] { 1107, 1113, 1119, 1124, 1205, 1405, 5201 },
            new int [] { 1118, 1126, 1303, 5203, 6277, 7804, },
            new int [] { 1125, 1127, 1206, 1402, 5202, 6201, },
            new int [] { 1014, 1210, 1304, 1406, 5204, 6243, },
            new int [] { 1016, 1209, 5205, 6271, 7805, },
        };
        private static readonly float[][] unlockWreckFallingItemDropCountLevel =
        {
            new float [] { 1.8f, 1.6f, 1.0f, 2.0f, 2.0f, 2.5f },
            new float [] { 1.56f, 1.8f, 1.4f, 2.0f, 1.2f, 1.3f },
            new float [] { 2.2f, 1.0f, 1.0f, 0.8f, 1.4f, 1.0f },
            new float [] { 1.1f, 1.2f, 1.4f, 0.8f, 0.5f, 0.6f, 2.5f },
            new float [] { 0.8f, 0.55f, 0.75f, 0.3f, 1.2f, 0.7f, },
            new float [] { 0.4f, 0.4f, 0.4f, 0.25f, 1.5f, 0.6f, },
            new float [] { 2f, 0.3f, 0.4f, 0.3f, 1.4f, 0.4f, },
            new float [] { 2f, 0.4f, 1.5f, 0.3f, 0.3f, },
        };

        private static int WreckFallingLevel;

        private static int UAVHPAndfiringRateUpgradeLevel;

        private static bool isUnlockCrackingRay;

        private static int vanillaTechSpeed;
        private static int synapticLatheTechSpeed;

        private static bool isItemGetHashUnlock;

        private static bool isFleetRangeUpgrade;

        private static int[] NewGameCompletionTech;
        private static int NewGameCompletionLevel;

        private static int ECMUpgradeLevel;

        public static int AntiMatterOutCounts;

        private static int[] UniverseObserveBuilding;

        private static bool isOrbitalTurretUnlock;

        public static int PilerEjectorLevel;

        struct gachaData { public int item; public int count; }
        private static readonly gachaData[] gachaDatas = {
           new gachaData { item = ProtoID.I氢, count = 114 },
           new gachaData { item = ProtoID.I单极磁石, count = 1000 },
           new gachaData { item =  ProtoID.I反物质, count = 514 },
           new gachaData { item = ProtoID.I负熵奇点, count = 173 },
           new gachaData { item = ProtoID.I奇夸克样本, count = 16 },
           new gachaData { item = ProtoID.I量子虚拟生命, count = 1 },
           new gachaData { item = ProtoID.ISCP310恒燃之火, count = 1 },
           new gachaData { item = ProtoID.I黑萝卜, count = 1 },
        };
        private static int gachaIndex = 0;

        private static float[] StarLuminosity = new float[0];

        private static void InitData()
        {
            WreckFallingLevel = 0;

            UAVHPAndfiringRateUpgradeLevel = 0;

            isUnlockCrackingRay = false;

            vanillaTechSpeed = 1;
            synapticLatheTechSpeed = 1;

            isItemGetHashUnlock = false;

            isFleetRangeUpgrade = false;

            NewGameCompletionTech = new int[]{ 1508, 1802, 1952, 1960 };
            NewGameCompletionLevel = 0;

            ECMUpgradeLevel = 0;

            AntiMatterOutCounts = 2;

            UniverseObserveBuilding = new int[]{ 0, 0, 0 };

            isOrbitalTurretUnlock = false;

            PilerEjectorLevel = 1;
        }

        public static int GetPilerEjectorLevel()
        {
            return PilerEjectorLevel;
        }

        // 下面是解锁不同的科技后要做的操作
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.NotifyTechUnlock))]
        [HarmonyPrefix]
        public static void NotifyTechUnlockPatch(GameHistoryData __instance, int _techId)
        {
            //Debug.LogFormat("scpppppppeopppppppppp UnlockTech techId {0}", _techId);
            CombatDroneMotify(_techId);
            WreckFalling(_techId);
            CrackingRayTechAndItemModify(_techId);
            UpdateTechSpeed(_techId);
            ItemGetHash(_techId);
            CraftUnitAttackRangeUpgrade(_techId);
            NewGameCompletion(_techId);
            NewECMUpgradeTechs(_techId);
            AntiMatterOutCountsTech(_techId);
            OrbitalTurretTech(_techId);
            PilerEjectorTechs(_techId);
            Gacha(_techId);
            SpeedOfLightModification(_techId);
        }

        static void UAVHPAndfiringRateUpgrade(int level)
        {
            ItemProto itemProto;
            RecipeProto recipeProto;

            if (level == 0) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 1103, 1407, 1301, 1401 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 1404, 6256 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 1404, 6256 };
                recipeProto.Type = ERecipeType.None;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机";
                itemProto.Description = "I攻击无人机";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机";
                itemProto.Description = "I精准无人机";
                itemProto.RefreshTranslation();
            } else if (level == 1) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 1103, 1407, 1303, 1401 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 1404, 1113 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 1404, 1113 };
                recipeProto.Type = ERecipeType.None;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机A型";
                itemProto.Description = "I攻击无人机A型";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机A型";
                itemProto.Description = "I精准无人机A型";
                itemProto.RefreshTranslation();
            } else if (level == 2) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 1107, 1407, 1303, 1126 };
                recipeProto.Type = ERecipeType.None;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机B型";
                itemProto.Description = "I攻击无人机B型";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机B型";
                itemProto.Description = "I精准无人机B型";
                itemProto.RefreshTranslation();
            } else if (level == 3) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 6225, 1407, 1303, 1126 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 1206, 1113 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 1206, 1113 };
                recipeProto.Type = ERecipeType.None;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机C型";
                itemProto.Description = "I攻击无人机C型";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机C型";
                itemProto.Description = "I精准无人机C型";
                itemProto.RefreshTranslation();
            } else if (level == 4) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 6225, 1407, 1303, 1126 };
                recipeProto.Type = ERecipeType.Assemble;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 1206, 1113 };
                recipeProto.Type = ERecipeType.Assemble;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 1206, 1113 };
                recipeProto.Type = ERecipeType.Assemble;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机D型";
                itemProto.Description = "I攻击无人机D型";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机D型";
                itemProto.Description = "I精准无人机D型";
                itemProto.RefreshTranslation();
            } else if (level == 5) {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] { 6225, 1407, 1303, 1126 };
                recipeProto.Type = ERecipeType.Assemble;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 6243, 1118 };
                recipeProto.Type = ERecipeType.Assemble;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 6243, 1118 };
                recipeProto.Type = ERecipeType.Assemble;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机E型";
                itemProto.Description = "I攻击无人机E型";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机E型";
                itemProto.Description = "I精准无人机E型";
                itemProto.RefreshTranslation();
            }
        }

        static void CombatDroneMotify(int techId)
        {
            if (techId == 5601) {
                UAVHPAndfiringRateUpgrade(1);
                UAVHPAndfiringRateUpgradeLevel = 1;
            } else if (techId == 5602) {
                UAVHPAndfiringRateUpgrade(2);
                UAVHPAndfiringRateUpgradeLevel = 2;
            } else if (techId == 5603) {
                UAVHPAndfiringRateUpgrade(3);
                UAVHPAndfiringRateUpgradeLevel = 3;
            } else if (techId == 5604) {
                UAVHPAndfiringRateUpgrade(4);
                UAVHPAndfiringRateUpgradeLevel = 4;
            } else if (techId == 5605) {
                UAVHPAndfiringRateUpgrade(5);
                UAVHPAndfiringRateUpgradeLevel = 5;
            }
        }

        static void UnlockWreckFalling(int unlockLevel)
        {
            ItemProto itemProto;
            if (unlockLevel == 0) // 传入0重置黑雾掉落为0
            {
                for (int z = 0; z < unlockWreckFallingItemIdLevel.Length; z++) {
                    for (int i = 0; i < unlockWreckFallingItemIdLevel[z].Length; i++) {
                        itemProto = LDB.items.Select(unlockWreckFallingItemIdLevel[z][i]);
                        itemProto.EnemyDropCount = 0;
                    }
                }
                ItemProto.InitEnemyDropTables();
                return;
            }

            for (int i = 0; i < unlockWreckFallingItemIdLevel[unlockLevel - 1].Length; i++) {
                itemProto = LDB.items.Select(unlockWreckFallingItemIdLevel[unlockLevel - 1][i]);
                itemProto.EnemyDropCount = unlockWreckFallingItemDropCountLevel[unlockLevel - 1][i];
            }
            ItemProto.InitEnemyDropTables();
        }

        static void WreckFalling(int techId)
        {
            if (techId == 5301) {
                // 解锁3级的黑雾掉落
                UnlockWreckFalling(1);
                WreckFallingLevel = 1;
            } else if (techId == 5302) {
                // 解锁6级的黑雾掉落
                UnlockWreckFalling(2);
                WreckFallingLevel = 2;
            } else if (techId == 5303) {
                // 解锁9级的黑雾掉落
                UnlockWreckFalling(3);
                WreckFallingLevel = 3;
            } else if (techId == 5304) {
                // 解锁12级的黑雾掉落
                UnlockWreckFalling(4);
                WreckFallingLevel = 4;
            } else if (techId == 5305) {
                // 解锁15级的黑雾掉落
                UnlockWreckFalling(5);
                WreckFallingLevel = 5;
            } else if (techId == 5306) {
                // 解锁18级的黑雾掉落
                UnlockWreckFalling(6);
                WreckFallingLevel = 6;
            } else if (techId == 5307) {
                // 解锁21级的黑雾掉落
                UnlockWreckFalling(7);
                WreckFallingLevel = 7;
            } else if (techId == 5308) {
                // 解锁24级的黑雾掉落
                UnlockWreckFalling(8);
                WreckFallingLevel = 8;
            }
        }

        static void UnlockCrackingRayTech()
        {
            ItemProto itemProto;
            TechProto techProto;
            itemProto = LDB.items.Select(6216);
            itemProto.Name = "终末螺旋";
            itemProto.Description = "I终末螺旋";
            itemProto.RefreshTranslation();

            techProto = LDB.techs.Select(1945);
            techProto.Name = "绿色的午夜";
            techProto.Desc = "T绿色的午夜";
            techProto.RefreshTranslation();
        }

        static void NotUnlockCrackingRayTech()
        {
            ItemProto itemProto;
            TechProto techProto;
            itemProto = LDB.items.Select(6216);
            itemProto.Name = "未知射线遗留样本";
            itemProto.Description = "I未知射线遗留样本";
            itemProto.RefreshTranslation();

            techProto = LDB.techs.Select(1945);
            techProto.Name = "未知射线研究";
            techProto.Desc = "T未知射线研究";
            techProto.RefreshTranslation();
        }

        static void CrackingRayTechAndItemModify(int techId)
        {
            if (techId == 1945) {
                UnlockCrackingRayTech();
                isUnlockCrackingRay = true;
            }
        }

        static bool isRecipesHandcraftUnlock(bool Handcraft)
        {
            // 解锁了次级维度工厂后，所有配方全可手搓
            if (GameMain.history.TechUnlocked(1947)) {
                return true;
            }
            return Handcraft;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.OnOkButtonClick))]
        public static IEnumerable<CodeInstruction> UIReplicatorWindow_OnOkButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Brtrue));

            matcher.Advance(2);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Unlock_Save_Load), nameof(isRecipesHandcraftUnlock))));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow.RefreshRecipeIcons))]
        public static IEnumerable<CodeInstruction> UIReplicatorWindow_RefreshRecipeIcons_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Stloc_S));

            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Unlock_Save_Load), nameof(isRecipesHandcraftUnlock))));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIReplicatorWindow), nameof(UIReplicatorWindow._OnUpdate))]
        public static IEnumerable<CodeInstruction> UIReplicatorWindow__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Brfalse));

            matcher.Advance(2);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Unlock_Save_Load), nameof(isRecipesHandcraftUnlock))));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        static void UpdateTechSpeed(int techId)
        {
            TechProto techProto = LDB.techs.Select(techId);

            if (techProto.UnlockFunctions.Length > 0 && techProto.UnlockFunctions[0] == 22) {
                vanillaTechSpeed++;
            }
        }


        [HarmonyPatch(typeof(Player), nameof(Player.TryAddItemToPackage))]
        [HarmonyPrefix]
        public static bool TryAddItemToPackagePatch(ref Player __instance, int itemId, ref int count, ref int __result)
        {
            if (itemId == 6254 && count > 0) {
                RecipeProto recipeProto;

                if (__instance.mecha.gameData.history.currentTech > 0) {
                    if (LDB.techs.Select(__instance.mecha.gameData.history.currentTech).IsLabTech == false) {
                        recipeProto = LDB.recipes.Select(642);
                        if (recipeProto.ItemCounts[0] == 2) {
                            synapticLatheTechSpeed = 1;
                            vanillaTechSpeed = __instance.mecha.gameData.history.techSpeed;
                        }
                        recipeProto.ItemCounts[0] = recipeProto.ItemCounts[0] * 2;
                        synapticLatheTechSpeed = synapticLatheTechSpeed * 2;
                        __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed + synapticLatheTechSpeed;
                        __result = 0;
                        return false;
                    } else {
                        recipeProto = LDB.recipes.Select(642);
                        recipeProto.ItemCounts[0] = 2;
                        synapticLatheTechSpeed = 1;
                        __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed;
                        __result = 0;
                        return false;
                    }
                } else {
                    recipeProto = LDB.recipes.Select(642);
                    if (recipeProto.ItemCounts[0] == 2) {
                        synapticLatheTechSpeed = 1;
                        vanillaTechSpeed = __instance.mecha.gameData.history.techSpeed;
                    }
                    recipeProto.ItemCounts[0] = recipeProto.ItemCounts[0] * 2;
                    synapticLatheTechSpeed = synapticLatheTechSpeed * 2;
                    __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed + synapticLatheTechSpeed;
                    __result = 0;
                    return false;
                }
            } else if (itemId == 6255 && count > 0) {
                __instance.mecha.gameData.history.AddTechHash(count * 1200000);
                return false;
            } else if (itemId == 1099 && count > 0) {
                count *= 50; // 沙土加到背包里，一个沙土对应50个沙土
                return true;
            }
            return true;
        }

        // 沙土加到背包里，一个沙土对应50个沙土
        [HarmonyPatch(typeof(Player), nameof(Player.ExchangeSand))]
        [HarmonyPrefix]
        public static bool ExchangeSandPatch(Player __instance)
        {
            int num = 0;
            for (int i = 0; i < __instance.package.size; i++) {
                if (__instance.package.grids[i].itemId == 1099) {
                    num += __instance.package.grids[i].count * 50;
                    __instance.package.grids[i].itemId = 0;
                    __instance.package.grids[i].filter = 0;
                    __instance.package.grids[i].count = 0;
                    __instance.package.grids[i].inc = 0;
                    __instance.package.grids[i].stackSize = 0;
                }
            }
            __instance.SetSandCount(__instance.sandCount + (long)num);
            return false;
        }

        // 重置 突出凝练机 的加速和消耗翻倍
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.RemoveTechInQueue))]
        [HarmonyPrefix]
        public static void RemoveTechInQueuePatch(GameHistoryData __instance, int index)
        {
            if (index == 0) {
                RecipeProto recipeProto;
                recipeProto = LDB.recipes.Select(642);
                recipeProto.ItemCounts[0] = 2;
                synapticLatheTechSpeed = 1;
                __instance.techSpeed = vanillaTechSpeed;
            }
        }

        // 重置 突出凝练机 的加速和消耗翻倍
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.DequeueTech))]
        [HarmonyPrefix]
        public static void DequeueTechPatch(GameHistoryData __instance)
        {
            RecipeProto recipeProto;
            recipeProto = LDB.recipes.Select(642);
            recipeProto.ItemCounts[0] = 2;
            synapticLatheTechSpeed = 1;
            __instance.techSpeed = vanillaTechSpeed;
        }

        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.EnqueueTech))]
        [HarmonyPrefix]
        public static void EnqueueTechPatch(GameHistoryData __instance, int techId)
        {
            if (__instance.techQueue[0] == 0) {
                if (LDB.techs.Select(techId).IsLabTech == true) {
                    RecipeProto recipeProto;
                    recipeProto = LDB.recipes.Select(642);
                    recipeProto.ItemCounts[0] = 2;
                    synapticLatheTechSpeed = 1;
                    __instance.techSpeed = vanillaTechSpeed;
                }
            }
        }

        static void ItemGetHash(int techId)
        {
            if (techId == 1962) {
                ItemProto itemProto = LDB.items.Select(6234);
                itemProto.Name = "十七公斤重的文明";
                itemProto.Description = "I十七公斤重的文明";
                itemProto.RefreshTranslation();
                isItemGetHashUnlock = true;
            }
        }

        static void CraftUnitAttackRangeUpgrade(int techId)
        {
             if(techId == 5406) {
                isFleetRangeUpgrade = true;
                ModelProto modelProto;
                modelProto = LDB.models.Select(451); // 护卫
                modelProto.prefabDesc.craftUnitAttackRange0 = 4000f;
                modelProto.prefabDesc.craftUnitSensorRange = 4500f;
                modelProto = LDB.models.Select(452); // 驱逐
                modelProto.prefabDesc.craftUnitAttackRange0 = 10000f;
                modelProto.prefabDesc.craftUnitAttackRange1 = 10000f;
                modelProto.prefabDesc.craftUnitSensorRange = 12000f;
             }  
        }

        

        static void NewGameCompletion(int techId)
        {
            BecauseItIsThere(techId);
            switch (techId)
            {
                case 1508:
                    NewGameCompletionLevel = 1;
                    break;
                case 1802:
                    NewGameCompletionLevel = 2;
                    break;
                case 1952:
                    NewGameCompletionLevel = 3;
                    break;
                case 1960:
                    NewGameCompletionLevel = 4;
                    break;
            }
        }

        static void BecauseItIsThere(int techId)
        {
            TechProto techProto;
            switch (techId)
            {
                case 1508:
                    techProto = LDB.techs.Select(1802);
                    techProto.IsHiddenTech = false;

                    break;
                case 1802:
                    techProto = LDB.techs.Select(techId);
                    techProto.Name = "因为，山就在那里".Translate();
                    techProto.RefreshTranslation();

                    techProto = LDB.techs.Select(1952);
                    techProto.IsHiddenTech = false;
                    break;
                case 1952:
                    techProto = LDB.techs.Select(1960);
                    techProto.IsHiddenTech = false; // 解除宇宙的齿轮的隐藏状态，3阶
                    break;
                case 1960:
                    techProto = LDB.techs.Select(1814);
                    techProto.IsHiddenTech = false;

                    MathematicalRateEngineRemoveSails();
                    CalculateThirdLevelMathematicalRateEngine();
                    break;
            }
        }

        static void lockBecauseItIsThere()
        {
            TechProto techProto;
            techProto = LDB.techs.Select(1802);
            techProto.IsHiddenTech = true;
            techProto.Name = "为何攀登高峰".Translate();
            techProto.RefreshTranslation();

            techProto = LDB.techs.Select(1952);
            techProto.IsHiddenTech = true;
            techProto = LDB.techs.Select(1960);
            techProto.IsHiddenTech = true;
            techProto = LDB.techs.Select(1814);
            techProto.IsHiddenTech = true;
        }

        // 新ECM升级科技,每级增加胶囊5点弹药量
        static void NewECMUpgradeTechs(int techId)
        {
            ItemProto itemProto;
            switch (techId)
            {
                case 6101:
                case 6102:
                case 6103:
                case 6104:
                case 6105:
                case 6106:
                    itemProto = LDB.items.Select(1612);
                    itemProto.HpMax += 5;
                    itemProto = LDB.items.Select(1613);
                    itemProto.HpMax += 5;
                    ECMUpgradeLevel++;
                    break;
            }
        }

        public static readonly double[] AntiMatterOutCountsUpgrades = { 0.6, 0.4, 0.2 };

        static void AntiMatterOutCountsTech(int techId) {
            if (techId == 1959) {
                RecipeProto recipeProto = LDB.recipes.Select(74);
                if (AntiMatterOutCounts < 4) {
                    double randDouble = GetRandDouble();
                    if (randDouble >= AntiMatterOutCountsUpgrades[AntiMatterOutCounts - 1]) {
                        AntiMatterOutCounts++;
                    } else if (AntiMatterOutCounts > 1) {
                        AntiMatterOutCounts--;
                    }
                }
                recipeProto.ResultCounts[0] = AntiMatterOutCounts;
                recipeProto.ResultCounts[1] = AntiMatterOutCounts;
            }
        }

        static void OrbitalTurretTech(int techId)
        {
            if (techId == 1811) {
                ItemProto itemProto = LDB.items.Select(6273);
                itemProto.Name = "轨道炮座";
                itemProto.Description = "I轨道炮座";
                itemProto.RefreshTranslation();
                isOrbitalTurretUnlock = true;
            }
        }

        static void PilerEjectorTechs(int techId)
        {
            switch (techId)
            {
                case 3151:
                    PilerEjectorLevel = 2;
                    break;
                case 3152:
                    PilerEjectorLevel = 3;
                    break;
                case 3153:
                    PilerEjectorLevel = 4;
                    break;
            }
        }

        static void Gacha(int techId)
        {
            if (techId == 1987) {
                TechProto techProto;
                techProto = LDB.techs.Select(1987);
                techProto.Conclusion = "无限应用课题完成".TranslateFromJson() + "\n" + gachaDatas[gachaIndex].count.ToString() + " " + LDB.items.Select(gachaDatas[gachaIndex].item).Name + "\n" + "无限应用课题完成英文结尾".TranslateFromJson();
                techProto.RefreshTranslation();
                int package = GameMain.mainPlayer.TryAddItemToPackage(gachaDatas[gachaIndex].item, gachaDatas[gachaIndex].count, 0, true);

                gachaIndex = GetRandInt(0, gachaDatas.Length);
            }
        }

        static void SpeedOfLightModification(int techId)
        {
            if (techId == 1989) {
                float luminosityChange = (float)(GetRandDouble() / 3);
                for (int i = 0; i < GameMain.galaxy.starCount; i++) {
                    if (GameMain.galaxy.stars[i].type != EStarType.BlackHole && GameMain.galaxy.stars[i].type != EStarType.NeutronStar) {
                        GameMain.galaxy.stars[i].luminosity += luminosityChange;
                    }
                }
            }
        }

        internal static void Export(BinaryWriter w)
        {
            w.Write(WreckFallingLevel);
            w.Write(isUnlockCrackingRay);
            w.Write(UAVHPAndfiringRateUpgradeLevel);
            w.Write(vanillaTechSpeed);
            w.Write(synapticLatheTechSpeed);
            w.Write(isItemGetHashUnlock);
            w.Write(isFleetRangeUpgrade);
            w.Write(NewGameCompletionLevel);
            w.Write(ECMUpgradeLevel);
            w.Write(AntiMatterOutCounts);
            for (int i = 0; i < 3; i++) {
                w.Write(UniverseObserveBuilding[i]);
            }
            w.Write(isOrbitalTurretUnlock);
            w.Write(PilerEjectorLevel);
            w.Write(GameMain.galaxy.starCount);
            for (int i = 0; i < GameMain.galaxy.starCount; i++) {
                w.Write(GameMain.galaxy.stars[i].luminosity);
            }
        }

        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try {
                ItemProto itemProto;
                RecipeProto recipeProto;
                WreckFallingLevel = r.ReadInt32();
                for (int i = 1; i <= WreckFallingLevel; i++) {
                    UnlockWreckFalling(i);
                }

                isUnlockCrackingRay = r.ReadBoolean();
                if (isUnlockCrackingRay) {
                    UnlockCrackingRayTech();
                }

                UAVHPAndfiringRateUpgradeLevel = r.ReadInt32();
                for (int i = 1; i <= UAVHPAndfiringRateUpgradeLevel; i++) {
                    UAVHPAndfiringRateUpgrade(i);
                }

                vanillaTechSpeed = r.ReadInt32();
                synapticLatheTechSpeed = r.ReadInt32();

                isItemGetHashUnlock = r.ReadBoolean();
                if (isItemGetHashUnlock) {
                    itemProto = LDB.items.Select(6234);
                    itemProto.Name = "十七公斤重的文明";
                    itemProto.Description = "I十七公斤重的文明";
                    itemProto.RefreshTranslation();
                }

                isFleetRangeUpgrade = r.ReadBoolean();
                if (isFleetRangeUpgrade) {
                    ModelProto modelProto;
                    modelProto = LDB.models.Select(451); // 护卫
                    modelProto.prefabDesc.craftUnitAttackRange0 = 4000f;
                    modelProto.prefabDesc.craftUnitSensorRange = 4500f;
                    modelProto = LDB.models.Select(452); // 驱逐
                    modelProto.prefabDesc.craftUnitAttackRange0 = 10000f;
                    modelProto.prefabDesc.craftUnitAttackRange1 = 10000f;
                    modelProto.prefabDesc.craftUnitSensorRange = 12000f;
                }


                NewGameCompletionLevel = r.ReadInt32();
                for (int i = 1; i <= NewGameCompletionLevel; i++) {
                    BecauseItIsThere(NewGameCompletionTech[NewGameCompletionLevel - 1]);
                }

                ECMUpgradeLevel = r.ReadInt32();
                itemProto = LDB.items.Select(1612);
                itemProto.HpMax += ECMUpgradeLevel * 5;
                itemProto = LDB.items.Select(1613);
                itemProto.HpMax += ECMUpgradeLevel * 5;

                AntiMatterOutCounts = r.ReadInt32();
                if (AntiMatterOutCounts < 1 || AntiMatterOutCounts > 4) {
                    AntiMatterOutCounts = 2;
                }
                recipeProto = LDB.recipes.Select(74);
                recipeProto.ResultCounts[0] = AntiMatterOutCounts;
                recipeProto.ResultCounts[1] = AntiMatterOutCounts;

                for (int i = 0; i < 3; i++) {
                    UniverseObserveBuilding[i] = r.ReadInt32();
                }

                isOrbitalTurretUnlock = r.ReadBoolean();
                if (isOrbitalTurretUnlock) {
                    itemProto = LDB.items.Select(6273);
                    itemProto.Name = "轨道炮座";
                    itemProto.Description = "I轨道炮座";
                    itemProto.RefreshTranslation();
                }
                PilerEjectorLevel = r.ReadInt32();

                int starCount;
                if (ProjectOrbitalRing.importVersion < 524320) {
                    starCount = 0;
                } else {
                    starCount = r.ReadInt32();
                }
                StarLuminosity = new float[starCount];
                for (int i = 0; i < starCount; i++) {
                    StarLuminosity[i] = r.ReadSingle();
                }
            }
            catch (EndOfStreamException)
            {
                // ignored
            }
        }

        internal static void IntoOtherSave()
        {
            InitData();
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return;
            }
            UnlockWreckFalling(0);
            NotUnlockCrackingRayTech();
            UAVHPAndfiringRateUpgrade(0);

            ItemProto itemProto;
            itemProto = LDB.items.Select(6234);
            itemProto.Name = "文明遗物";
            itemProto.Description = "I文明遗物";
            itemProto.RefreshTranslation();

            ModifyFleetUpgradeTechs();

            lockBecauseItIsThere();

            itemProto = LDB.items.Select(1612);
            itemProto.HpMax = 60;
            itemProto = LDB.items.Select(1613);
            itemProto.HpMax = 60;

            itemProto = LDB.items.Select(6273);
            itemProto.Name = "轨道观测站";
            itemProto.Description = "I轨道观测站";
            itemProto.RefreshTranslation();

            gachaIndex = GetRandInt(0, gachaDatas.Length);
            TechProto techProto;
            techProto = LDB.techs.Select(1987);
            techProto.Conclusion = "无限应用课题完成".TranslateFromJson() + "\n" + gachaDatas[gachaIndex].count.ToString() + " " + LDB.items.Select(gachaDatas[gachaIndex].item).Name + "\n" + "无限应用课题完成英文结尾".TranslateFromJson();
            techProto.RefreshTranslation();

            StarLuminosity = null;
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        [HarmonyPostfix]
        public static void GameData_Import_Patch(GameData __instance)
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return;
            }
            if (StarLuminosity == null) {
                return;
            }
            if (StarLuminosity.Length == GameMain.galaxy.starCount) {
                for (int i = 0; i < GameMain.galaxy.starCount; i++) {
                    GameMain.galaxy.stars[i].luminosity = StarLuminosity[i];
                }
            }
        }

        public static void AddUniverseObserveBuilding(int itemId)
        {
            if (itemId == 3009) {
                UniverseObserveBuilding[0]++;
            } else if (itemId == 6266) {
                UniverseObserveBuilding[1]++;
            } else if (itemId == 6273) {
                UniverseObserveBuilding[2]++;
            }
        }

        public static void DelUniverseObserveBuilding(int itemId)
        {
            if (itemId == 3009) {
                UniverseObserveBuilding[0]--;
            } else if (itemId == 6266) {
                UniverseObserveBuilding[1]--;
            } else if (itemId == 6273) {
                UniverseObserveBuilding[2]--;
            }
        }

        public static int GetUniverseObserveBuilding(int index)
        {
            return UniverseObserveBuilding[index];
        }
    }
}

