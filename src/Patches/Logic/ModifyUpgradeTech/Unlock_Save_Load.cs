using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectOrbitalRing.Utils.RandomUtils;

namespace ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech
{
    internal class Unlock_Save_Load
    {
        private static readonly int[] unlockHandcraftRecipes =
        {
            21, 26, 28, 29, 34, 36, 38, 39, 41, 42, 43, 44, 47, 51, 52, 53,
            54, 57, 70, 71, 72, 73, 80, 81, 99, 100, 101, 105, 109, 115, 116, 119,
            124, 128, 132, 135, 140, 141, 142, 143, 145, 146, 153, 154, 155, 156, 157, 159,
            402, 403, 408, 416, 418, 424, 425, 519, 523, 802, 709, 710, 716, 751, 752, 754, 771,
            772, 783, 785, 789, 793, 794, 795,
        };

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
        private static readonly int[] unlockWreckFallingItemIdLevel2 =
        {
            1103, 1105, 1111, 1131, 1203, 1401,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel2 =
        {
            1.56f, 1.8f, 1.4f, 2.0f, 1.2f, 1.3f
        };

        private static readonly int[] unlockWreckFallingItemIdLevel3 =
        {
            1106, 1115, 1123, 1204, 1404, 1407,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel3 =
        {
            2.2f, 1.0f, 1.0f, 0.8f, 1.4f, 1.0f
        };
        private static readonly int[] unlockWreckFallingItemIdLevel4 =
        {
            1107, 1113, 1119, 1124, 1205, 1405, 5201
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel4 =
        {
            1.1f, 1.2f, 1.4f, 0.8f, 0.5f, 0.6f, 2.5f
        };
        private static readonly int[] unlockWreckFallingItemIdLevel5 =
        {
            1118, 1126, 1303, 5203, 6277, 7804,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel5 =
        {
            0.8f, 0.55f, 0.75f, 0.3f, 1.2f, 0.7f,
        };
        private static readonly int[] unlockWreckFallingItemIdLevel6 =
        {
            1125, 1127, 1206, 1402, 5202, 6201,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel6 =
        {
            0.4f, 0.4f, 0.4f, 0.25f, 1.5f, 0.6f,
        };
        private static readonly int[] unlockWreckFallingItemIdLevel7 =
        {
            1014, 1210, 1304, 1406, 5204, 6243,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel7 =
        {
            2f, 0.3f, 0.4f, 0.3f, 1.4f, 0.4f,
        };
        private static readonly int[] unlockWreckFallingItemIdLevel8 =
        {
            1016, 1209, 5205, 6271, 7805,
        };
        private static readonly float[] unlockWreckFallingItemDropCountLevel8 =
        {
            2f, 0.4f, 1.5f, 0.3f, 0.3f,
        };

        private static int WreckFallingLevel = 0;

        private static int UAVHPAndfiringRateUpgradeLevel = 0;

        private static bool isUnlockRecipesHandcraft = false;

        private static bool isUnlockCrackingRay = false;

        private static int vanillaTechSpeed = 1;
        private static int synapticLatheTechSpeed = 1;

        private static bool isItemGetHashUnlock = false;

        private static int[] WhichFleetUpgradeChoose = { 0, 0, 0 };

        private static int whichTechChoose = 0;

        private static int[] NewGameCompletionTech = { 1508, 1802, 1952, 1960 };
        private static int NewGameCompletionLevel = 0;

        private static int ECMUpgradeLevel = 0;

        public static int AntiMatterOutCounts = 2;

        private static int[] UniverseObserveBuilding = { 0, 0, 0 };

        private static bool isOrbitalTurretUnlock = false;

        public static int PilerEjectorLevel = 1;

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
            UnlockRecipesHandcraft(_techId, true);
            UpdateTechSpeed(_techId);
            ItemGetHash(_techId);
            CraftUnitAttackRangeUpgrade(_techId);
            AorB(_techId);
            NewGameCompletion(_techId);
            NewECMUpgradeTechs(_techId);
            AntiMatterOutCountsTech(_techId);
            OrbitalTurretTech(_techId);
            PilerEjectorTechs(_techId);
        }

        static void UAVHPAndfiringRateUpgrade(int level)
        {
            ItemProto itemProto;
            RecipeProto recipeProto;

            if (level == 0)
            {
                recipeProto = LDB.recipes.Select(147);
                recipeProto.Items = new int[] {1103, 1407, 1301, 1401};
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(148);
                recipeProto.Items = new int[] { 5101, 1405, 1404, 6256 };
                recipeProto.Type = ERecipeType.None;
                recipeProto = LDB.recipes.Select(149);
                recipeProto.Items = new int[] { 5101, 1303, 1404, 6256};
                recipeProto.Type = ERecipeType.None;

                itemProto = LDB.items.Select(5102);
                itemProto.Name = "攻击无人机";
                itemProto.Description = "I攻击无人机";
                itemProto.RefreshTranslation();

                itemProto = LDB.items.Select(5103);
                itemProto.Name = "精准无人机";
                itemProto.Description = "I精准无人机";
                itemProto.RefreshTranslation();
            } else if (level == 1)
            {
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
            }
            else if (level == 2)
            {
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
            }
            else if (level == 3)
            {
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
            }
            else if (level == 4)
            {
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
            }
            else if (level == 5)
            {
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
            if (techId == 5601)
            {
                UAVHPAndfiringRateUpgrade(1);
                UAVHPAndfiringRateUpgradeLevel = 1;
            }
            else if (techId == 5602)
            {
                UAVHPAndfiringRateUpgrade(2);
                UAVHPAndfiringRateUpgradeLevel = 2;
            }
            else if (techId == 5603)
            {
                UAVHPAndfiringRateUpgrade(3);
                UAVHPAndfiringRateUpgradeLevel = 3;
            }
            else if (techId == 5604)
            {
                UAVHPAndfiringRateUpgrade(4);
                UAVHPAndfiringRateUpgradeLevel = 4;
            }
            else if (techId == 5605)
            {
                UAVHPAndfiringRateUpgrade(5);
                UAVHPAndfiringRateUpgradeLevel = 5;
            }
        }

        static void UnlockWreckFalling(int unlockLevel)
        {
            ItemProto itemProto;
            if (unlockLevel == 0) // 传入0重置黑雾掉落为0
            {
                for (int z = 0; z < unlockWreckFallingItemIdLevel.Length; z++)
                {
                    for (int i = 0; i < unlockWreckFallingItemIdLevel[z].Length; i++)
                    {
                        itemProto = LDB.items.Select(unlockWreckFallingItemIdLevel[z][i]);
                        itemProto.EnemyDropCount = 0;
                    }
                }
                ItemProto.InitEnemyDropTables();
                return;
            }

            for (int i = 0; i < unlockWreckFallingItemIdLevel[unlockLevel - 1].Length; i++)
            {
                itemProto = LDB.items.Select(unlockWreckFallingItemIdLevel[unlockLevel - 1][i]);
                itemProto.EnemyDropCount = unlockWreckFallingItemDropCountLevel[unlockLevel - 1][i];
            }
            ItemProto.InitEnemyDropTables();
        }

        static void WreckFalling(int techId)
        {
            if (techId == 5301)
            {
                // 解锁3级的黑雾掉落
                UnlockWreckFalling(1);
                WreckFallingLevel = 1;
            }
            else if (techId == 5302)
            {
                // 解锁6级的黑雾掉落
                UnlockWreckFalling(2);
                WreckFallingLevel = 2;
            }
            else if (techId == 5303)
            {
                // 解锁9级的黑雾掉落
                UnlockWreckFalling(3);
                WreckFallingLevel = 3;
            }
            else if (techId == 5304)
            {
                // 解锁12级的黑雾掉落
                UnlockWreckFalling(4);
                WreckFallingLevel = 4;
            }
            else if (techId == 5305)
            {
                // 解锁15级的黑雾掉落
                UnlockWreckFalling(5);
                WreckFallingLevel = 5;
            }
            else if (techId == 5306)
            {
                // 解锁18级的黑雾掉落
                UnlockWreckFalling(6);
                WreckFallingLevel = 6;
            }
            else if (techId == 5307)
            {
                // 解锁21级的黑雾掉落
                UnlockWreckFalling(7);
                WreckFallingLevel = 7;
            }
            else if (techId == 5308)
            {
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
            if (techId == 1945)
            {
                UnlockCrackingRayTech();
                isUnlockCrackingRay = true;
            }
        }

        static void UnlockRecipesHandcraft(int techId, bool isUnlock)
        {
            RecipeProto recipeProto;
            if (techId == 1945)
            {
                for (int i = 0; i < unlockHandcraftRecipes.Length; i++)
                {
                    recipeProto = LDB.recipes.Select(unlockHandcraftRecipes[i]);
                    recipeProto.Handcraft = isUnlock;
                }
                isUnlockRecipesHandcraft = isUnlock;
            }
        }

        static void UpdateTechSpeed(int techId)
        {
            TechProto techProto = LDB.techs.Select(techId);

            if (techProto.UnlockFunctions.Length > 0 && techProto.UnlockFunctions[0] == 22)
            {
                vanillaTechSpeed++;
            }
        }


        [HarmonyPatch(typeof(Player), nameof(Player.TryAddItemToPackage))]
        [HarmonyPrefix]
        public static bool TryAddItemToPackagePatch(ref Player __instance, int itemId, int count, ref int __result)
        {
            if (itemId == 6254 && count > 0)
            {
                RecipeProto recipeProto;

                if (__instance.mecha.gameData.history.currentTech > 0)
                {
                    if (LDB.techs.Select(__instance.mecha.gameData.history.currentTech).IsLabTech == false)
                    {
                        recipeProto = LDB.recipes.Select(642);
                        if (recipeProto.ItemCounts[0] == 2)
                        {
                            synapticLatheTechSpeed = 1;
                            vanillaTechSpeed = __instance.mecha.gameData.history.techSpeed;
                        }
                        recipeProto.ItemCounts[0] = recipeProto.ItemCounts[0] * 2;
                        synapticLatheTechSpeed = synapticLatheTechSpeed * 2;
                        __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed + synapticLatheTechSpeed;
                        __result = 0;
                        return false;
                    }
                    else
                    {
                        recipeProto = LDB.recipes.Select(642);
                        recipeProto.ItemCounts[0] = 2;
                        synapticLatheTechSpeed = 1;
                        __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed;
                        __result = 0;
                        return false;
                    }
                }
                else
                {
                    recipeProto = LDB.recipes.Select(642);
                    if (recipeProto.ItemCounts[0] == 2)
                    {
                        synapticLatheTechSpeed = 1;
                        vanillaTechSpeed = __instance.mecha.gameData.history.techSpeed;
                    }
                    recipeProto.ItemCounts[0] = recipeProto.ItemCounts[0] * 2;
                    synapticLatheTechSpeed = synapticLatheTechSpeed * 2;
                    __instance.mecha.gameData.history.techSpeed = vanillaTechSpeed + synapticLatheTechSpeed;
                    __result = 0;
                    return false;
                }
            }
            else if (itemId == 6255 && count > 0)
            {
                __instance.mecha.gameData.history.AddTechHash(count * 1200000);
                return false;
            }
            return true;
        }

        // 重置 突出凝练机 的加速和消耗翻倍
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.RemoveTechInQueue))]
        [HarmonyPrefix]
        public static void RemoveTechInQueuePatch(GameHistoryData __instance, int index)
        {
            if (index == 0)
            {
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
            if (__instance.techQueue[0] == 0)
            {
                if (LDB.techs.Select(techId).IsLabTech == true)
                {
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
            if (techId == 1962)
            {
                ItemProto itemProto = LDB.items.Select(6234);
                itemProto.Name = "十七公斤重的文明";
                itemProto.Description = "I十七公斤重的文明";
                itemProto.RefreshTranslation();
                isItemGetHashUnlock = true;
            }
        }

        static void SetUnclockValuesIneffective(int techId)
        {
            TechProto techProto;
            techProto = LDB.techs.Select(techId);
            techProto.UnlockValues = new double[] { 0, 0 };
            techProto.RefreshTranslation();
        }

        static void CraftUnitAttackRangeUpgrade(int techId)
        {
            switch (techId)
            {
                case 5401:
                    if (WhichFleetUpgradeChoose[0] == 0)
                    {
                        SetUnclockValuesIneffective(5404);
                        WhichFleetUpgradeChoose[0] = 5404;
                    }
                    break;
                case 5402:
                    if (WhichFleetUpgradeChoose[1] == 0)
                    {
                        SetUnclockValuesIneffective(5405);
                        WhichFleetUpgradeChoose[1] = 5405;
                    }
                    break;
                case 5403:
                    if (WhichFleetUpgradeChoose[2] == 0)
                    {
                        SetUnclockValuesIneffective(5406);
                        WhichFleetUpgradeChoose[2] = 5406;
                    }
                    break;
                case 5404:
                    if (WhichFleetUpgradeChoose[0] == 0)
                    {
                        SetUnclockValuesIneffective(5401);
                        WhichFleetUpgradeChoose[0] = 5401;
                    }
                    break;
                case 5405:
                    if (WhichFleetUpgradeChoose[1] == 0)
                    {
                        SetUnclockValuesIneffective(5402);
                        WhichFleetUpgradeChoose[1] = 5402;
                    }
                    break;
                case 5406:
                    if (WhichFleetUpgradeChoose[2] == 0)
                    {
                        SetUnclockValuesIneffective(5403);
                        WhichFleetUpgradeChoose[2] = 5403;
                        ModelProto modelProto;
                        modelProto = LDB.models.Select(451); // 护卫
                        modelProto.prefabDesc.craftUnitAttackRange0 = 4000f;
                        modelProto.prefabDesc.craftUnitSensorRange = 4500f;
                        modelProto = LDB.models.Select(452); // 驱逐
                        modelProto.prefabDesc.craftUnitAttackRange0 = 10000f;
                        modelProto.prefabDesc.craftUnitAttackRange1 = 10000f;
                        modelProto.prefabDesc.craftUnitSensorRange = 12000f;
                    }
                    break;
                default:
                    break;
            }
        }

        static void AorB(int techId)
        {
            RecipeProto recipeProto;
            if (techId == 1937)
            {
                if (whichTechChoose == 0)
                {
                    whichTechChoose = 1937;
                    recipeProto = LDB.recipes.Select(26); // 碳纳米管（粒子打印）
                    recipeProto.TimeSpend = 120;
                }
            }
            else if (techId == 1954)
            {
                if (whichTechChoose == 0)
                {

                    recipeProto = LDB.recipes.Select(97); // 电动机
                    recipeProto.TimeSpend = 60;

                    recipeProto = LDB.recipes.Select(98); // 电磁涡轮
                    recipeProto.TimeSpend = 60;

                    TechProto techProto = LDB.techs.Select(1937);
                    techProto.UnlockRecipes = new int[] { }; // 删除 石墨烯（碳解离）配方的解锁
                    whichTechChoose = 1954;
                }
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
                    techProto.IsHiddenTech = false;
                    break;
                case 1960:
                    techProto = LDB.techs.Select(1814);
                    techProto.IsHiddenTech = false;
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
            if (techId == 1811)
            {
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


        internal static void Export(BinaryWriter w)
        {
            w.Write(WreckFallingLevel);
            w.Write(isUnlockCrackingRay);
            w.Write(isUnlockRecipesHandcraft);
            w.Write(UAVHPAndfiringRateUpgradeLevel);
            w.Write(vanillaTechSpeed);
            w.Write(synapticLatheTechSpeed);
            w.Write(isItemGetHashUnlock);
            for (int i = 0; i < 3; i++)
            {
                w.Write(WhichFleetUpgradeChoose[i]);
            }
            w.Write(whichTechChoose);
            w.Write(NewGameCompletionLevel);
            w.Write(ECMUpgradeLevel);
            w.Write(AntiMatterOutCounts);
            for (int i = 0; i < 3; i++)
            {
                w.Write(UniverseObserveBuilding[i]);
            }
            w.Write(isOrbitalTurretUnlock);
            w.Write(PilerEjectorLevel);
        }

        internal static void Import(BinaryReader r)
        {
            try
            {
                ItemProto itemProto;
                TechProto techProto;
                RecipeProto recipeProto;
                WreckFallingLevel = r.ReadInt32();
                UnlockWreckFalling(0);
                for (int i = 1; i <= WreckFallingLevel; i++)
                {
                    UnlockWreckFalling(i);
                }

                isUnlockCrackingRay = r.ReadBoolean();
                if (isUnlockCrackingRay)
                {
                    UnlockCrackingRayTech();
                }
                else
                {
                    NotUnlockCrackingRayTech();
                }

                isUnlockRecipesHandcraft = r.ReadBoolean();
                if (isUnlockRecipesHandcraft)
                {
                    UnlockRecipesHandcraft(1945, true);
                } else
                {
                    UnlockRecipesHandcraft(1945, false);
                }

                UAVHPAndfiringRateUpgradeLevel = r.ReadInt32();
                UAVHPAndfiringRateUpgrade(0);
                for (int i = 1; i <= UAVHPAndfiringRateUpgradeLevel; i++)
                {
                    UAVHPAndfiringRateUpgrade(i);
                }

                vanillaTechSpeed = r.ReadInt32();
                synapticLatheTechSpeed = r.ReadInt32();

                isItemGetHashUnlock = r.ReadBoolean();
                if (isUnlockRecipesHandcraft)
                {
                    itemProto = LDB.items.Select(6234);
                    itemProto.Name = "十七公斤重的文明";
                    itemProto.Description = "I十七公斤重的文明";
                    itemProto.RefreshTranslation();
                } else
                {
                    itemProto = LDB.items.Select(6234);
                    itemProto.Name = "文明遗物";
                    itemProto.Description = "I文明遗物";
                    itemProto.RefreshTranslation();
                }

                for (int i = 0; i < 3; i++)
                {
                    WhichFleetUpgradeChoose[i] = r.ReadInt32();
                    if (WhichFleetUpgradeChoose[i] != 0)
                    {
                        techProto = LDB.techs.Select(WhichFleetUpgradeChoose[i]);
                        techProto.UnlockValues = new double[] { 0, 0 };
                        if (i == 2 && WhichFleetUpgradeChoose[i] == 5403)
                        {
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
                }

                whichTechChoose = r.ReadInt32();
                if (whichTechChoose == 1937)
                {
                    recipeProto = LDB.recipes.Select(26); // 碳纳米管（粒子打印）
                    recipeProto.TimeSpend = 120;
                }
                else
                {
                    recipeProto = LDB.recipes.Select(26); // 碳纳米管（粒子打印）
                    recipeProto.TimeSpend = 240;
                }
                if (whichTechChoose == 1954)
                {
                    recipeProto = LDB.recipes.Select(97); // 电动机
                    recipeProto.TimeSpend = 60;

                    recipeProto = LDB.recipes.Select(98); // 电磁涡轮
                    recipeProto.TimeSpend = 60;

                    techProto = LDB.techs.Select(1937);
                    techProto.UnlockRecipes = new int[] { }; // 删除 石墨烯（碳解离）配方的解锁
                }
                else
                {
                    recipeProto = LDB.recipes.Select(97); // 电动机
                    recipeProto.TimeSpend = 120;

                    recipeProto = LDB.recipes.Select(98); // 电磁涡轮
                    recipeProto.TimeSpend = 120;
                }

                NewGameCompletionLevel = r.ReadInt32();
                lockBecauseItIsThere();
                for (int i = 1; i <= NewGameCompletionLevel; i++)
                {
                    BecauseItIsThere(NewGameCompletionTech[NewGameCompletionLevel - 1]);
                }

                ECMUpgradeLevel = r.ReadInt32();
                itemProto = LDB.items.Select(1612);
                itemProto.HpMax = 60;
                itemProto.HpMax += ECMUpgradeLevel * 5;
                itemProto = LDB.items.Select(1613);
                itemProto.HpMax = 60;
                itemProto.HpMax += ECMUpgradeLevel * 5;

                AntiMatterOutCounts = r.ReadInt32();
                if (AntiMatterOutCounts < 1 || AntiMatterOutCounts > 4) {
                    AntiMatterOutCounts = 2;
                }
                recipeProto = LDB.recipes.Select(74);
                recipeProto.ResultCounts[0] = AntiMatterOutCounts;
                recipeProto.ResultCounts[1] = AntiMatterOutCounts;

                for (int i = 0; i < 3; i++)
                {
                    UniverseObserveBuilding[i] = r.ReadInt32();
                }

                isOrbitalTurretUnlock = r.ReadBoolean();
                if (isOrbitalTurretUnlock)
                {
                    itemProto = LDB.items.Select(6273);
                    itemProto.Name = "轨道炮座";
                    itemProto.Description = "I轨道炮座";
                    itemProto.RefreshTranslation();
                } else
                {
                    itemProto = LDB.items.Select(6273);
                    itemProto.Name = "轨道观测站";
                    itemProto.Description = "I轨道观测站";
                    itemProto.RefreshTranslation();
                }
                PilerEjectorLevel = r.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                // ignored
            }
        }

        internal static void IntoOtherSave() {
            AntiMatterOutCounts = 2;
        }

        public static void AddUniverseObserveBuilding(int itemId)
        {
            if (itemId == 3009)
            {
                UniverseObserveBuilding[0]++;
            }
            else if (itemId == 6266)
            {
                UniverseObserveBuilding[1]++;
            }
            else if (itemId == 6273)
            {
                UniverseObserveBuilding[2]++;
            }
        }

        public static void DelUniverseObserveBuilding(int itemId)
        {
            if (itemId == 3009)
            {
                UniverseObserveBuilding[0]--;
            }
            else if (itemId == 6266)
            {
                UniverseObserveBuilding[1]--;
            }
            else if (itemId == 6273)
            {
                UniverseObserveBuilding[2]--;
            }
        }

        public static int GetUniverseObserveBuilding(int index)
        {
            return UniverseObserveBuilding[index];
        }
    }
}

