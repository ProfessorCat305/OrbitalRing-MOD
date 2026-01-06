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
                
                case ProtoID.T驱动引擎2:
                    __result += "\r\n" + "离星燃料消耗描述".TranslateFromJson();
                    break;
                case ProtoID.T驱动引擎3:
                    __result += "\r\n" + "离星燃料消耗减少描述".TranslateFromJson();
                    break;
                case ProtoID.T驱动引擎4:
                    __result += "\r\n" + "离星燃料消耗取消描述".TranslateFromJson();
                    break;
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
                case 1947:
                    __result += "\r\n" + "解锁手搓".TranslateFromJson();
                    break;
                case 1951:
                    __result = "\r\n" + "星环解锁".TranslateFromJson();
                    break;

                case 1959:
                    double[] upgrades = Unlock_Save_Load.AntiMatterOutCountsUpgrades;
                    int count = Unlock_Save_Load.AntiMatterOutCounts;
                    double arg0;
                    int arg1;
                    double arg2;
                    int arg3;
                    if (count == 1) {
                        arg0 = 1 - upgrades[0];
                        arg1 = count;
                        arg2 = upgrades[0];
                        arg3 = count + 1;
                    } else if (count == 4) {
                        arg0 = 0;
                        arg1 = count - 1;
                        arg2 = 1;
                        arg3 = count;
                    } else {
                        arg0 = 1 - upgrades[count - 1];
                        arg1 = count - 1;
                        arg2 = upgrades[count - 1];
                        arg3 = count + 1;
                    }
                    __result = string.Format("开弦修正描述".TranslateFromJson(), arg0, arg1, arg2, arg3);
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
            }
        }
    }
}
