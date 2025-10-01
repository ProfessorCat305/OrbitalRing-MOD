using System.Text;
using HarmonyLib;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech {
    internal class ModifyTechText {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), nameof(TechProto.UnlockFunctionText))]
        public static void TechProto_UnlockFunctionText_Postfix(TechProto __instance, ref string __result,
            StringBuilder sb) {
            switch (__instance.ID) {
                case ProtoID.T坐标引擎:
                    __result = "坐标引擎文字描述".TranslateFromJson();
                    break;
                case 6101:
                case 6102:
                case 6103:
                case 6104:
                case 6105:
                case 6106:
                    __result = "电磁武器升级描述".TranslateFromJson();
                    break;
                case 1802:
                    __result = "数学率引擎零阶".TranslateFromJson();
                    break;
                case 1952:
                    __result = "数学率引擎一阶".TranslateFromJson();
                    break;
                case 1960:
                    __result = "数学率引擎二阶".TranslateFromJson();
                    break;
                case 1814:
                    __result = "数学率引擎三阶".TranslateFromJson();
                    break;
                case 1937:
                    __result = "卷碳管时间减半".TranslateFromJson();
                    break;
                case 1947:
                    __result += "\r\n" + "解锁手搓".TranslateFromJson();
                    break;
                case 1951:
                    __result = "\r\n" + "星环解锁".TranslateFromJson();
                    break;
                case 1954:
                    __result = "电动机电磁涡轮时间减半".TranslateFromJson();
                    break;
                case 1959:
                    double[] upgrades = Unlock_Save_Load.AntiMatterOutCountsUpgrades;
                    int count = Unlock_Save_Load.AntiMatterOutCounts;
                    if (count == 1) {
                        __result = string.Format("开弦修正描述".TranslateFromJson(), upgrades[0], 0);
                    } else if (count == 4) {
                        __result = string.Format("开弦修正描述".TranslateFromJson(), 0, 0);
                    } else {
                        __result = string.Format("开弦修正描述".TranslateFromJson(), upgrades[count - 1],
                            1 - upgrades[count - 1]);
                    }
                    break;
                case 3151:
                case 3152:
                case 3153:
                    __result = "使弹射器发射太阳帆数量加一".TranslateFromJson();
                    break;
                case 5406:
                    __result += "\r\n" + "驱逐舰护卫舰射程增加".TranslateFromJson();
                    break;
                case ProtoID.T宇宙探索1:
                    __result += "\r\n" + "宇宙探索1解锁".TranslateFromJson();
                    break;
                case ProtoID.T宇宙探索2:
                    __result += "\r\n" + "宇宙探索2解锁".TranslateFromJson();
                    break;
                case ProtoID.T宇宙探索3:
                    __result += "\r\n" + "宇宙探索3解锁".TranslateFromJson();
                    break;
                case ProtoID.T宇宙探索4:
                    __result += "\r\n" + "宇宙探索4解锁".TranslateFromJson();
                    break;
            }

            string text = "";
            if (__instance.UnlockFunctions.Length > 0) {
                if (__instance.UnlockFunctions[0] == 101) {
                    text = text + "黑雾".Translate() + __instance.UnlockValues[0] + "级残骸物品掉落".Translate();
                    __result += text;
                }
                //if (__instance.UnlockFunctions.Length > 1)
                //{
                //    if (__instance.UnlockFunctions[0] == 7 && __instance.UnlockFunctions[1] == 102)
                //    {
                //        text = text + "+" + __instance.UnlockValues[0].ToString("0%") + "手动合成速度".Translate();
                //        text += "\r\n";
                //        text = text + "解锁手搓".Translate();
                //        __result = text;
                //    }
                //    else if (__instance.UnlockFunctions[0] == 103 && __instance.UnlockFunctions[1] == 72)
                //    {
                //        text = text + "驱逐舰射程增加至".Translate() + __instance.UnlockValues[0] + "，护卫舰射程增加至".Translate() + __instance.UnlockValues[0] * 0.4;
                //        text += "\r\n";
                //        text += string.Format("太空战斗机攻速升级".Translate(), __instance.UnlockValues[1]);
                //        __result = text;
                //    }
                //}
            }
        }

        //[HarmonyPatch(typeof(TechProto), nameof(TechProto.UnlockFunctionText))]
        //[HarmonyPrefix]
        //public static bool UnlockFunctionTextPatch(TechProto __instance, StringBuilder sb, ref string __result)
        //{
        //    string text = "";
        //    if (__instance.UnlockFunctions.Length > 0)
        //    {
        //        if (__instance.UnlockFunctions[0] == 101)
        //        {
        //            text = text + "黑雾".Translate() + __instance.UnlockValues[0] + "级残骸物品掉落".Translate();
        //            __result = text;
        //            return false;
        //        }
        //        if (__instance.UnlockFunctions.Length > 1)
        //        {
        //            if (__instance.UnlockFunctions[0] == 7 && __instance.UnlockFunctions[1] == 102)
        //            {
        //                text = text + "+" + __instance.UnlockValues[0].ToString("0%") + "手动合成速度".Translate();
        //                text += "\r\n";
        //                text = text + "解锁手搓".Translate();
        //                __result = text;
        //                return false;
        //            }
        //            else if (__instance.UnlockFunctions[0] == 103 && __instance.UnlockFunctions[1] == 72)
        //            {
        //                text = text + "驱逐舰射程增加至".Translate() + __instance.UnlockValues[0] + "，护卫舰射程增加至".Translate() + __instance.UnlockValues[0] * 0.4;
        //                text += "\r\n";
        //                text += string.Format("太空战斗机攻速升级".Translate(), __instance.UnlockValues[1]);
        //                __result = text;
        //                return false;
        //            }
        //        }
        //    }
        //    return true;
        //}
    }
}
