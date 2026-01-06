using GalacticScale;
using HarmonyLib;
using MoreMegaStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine
{
    internal class UI
    {
        public static StarData curStar;           //编辑巨构建筑页面当前显示的恒星的数据
        public static DysonSphere curDysonSphere; //戴森球编辑界面正在浏览的戴森球

        public static Text RightDysonTitle;
        public static Text RightStarPowRatioText;
        public static Text RightMaxPowGenText;
        public static Text RightMaxPowGenValueText;
        public static Text RightDysonBluePrintText;
        public static void GetDysonVanillaUITexts()
        {
            try
            {
                RightDysonTitle = GameObject
                                 .Find(
                                      "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/name-text")
                                 .GetComponent<Text>();
                RightStarPowRatioText = GameObject
                                       .Find(
                                            "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/star-label")
                                       .GetComponent<Text>();
                RightMaxPowGenText = GameObject
                                    .Find(
                                         "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/gen-label")
                                    .GetComponent<Text>();
                RightMaxPowGenValueText = GameObject
                                         .Find(
                                              "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/gen-value")
                                         .GetComponent<Text>();
                RightDysonBluePrintText = GameObject
                                         .Find(
                                              "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/blueprint")
                                         .GetComponent<Text>();
                //戴森球+星系名称  恒星光度系数  最大发电性能 最大发电性能的值 戴森球蓝图  
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 3 ERROR");
            }
        }

        /// <summary>
        /// 下面三个是在戴森球界面进行操作时需要重置UI文本、按钮等操作，貌似改游戏本身的stringproto也可以，但是没改
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void SetTextOnOpen(UIDysonEditor __instance)
        {
            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "OnViewStarChange")]
        public static void SetTextOnViewStarChange(UIDysonEditor __instance)
        {
            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar);
            }

            //RefreshButtonPos();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "OnSelectionChange")]
        public static void SetTextOnSelectionChange(UIDysonEditor __instance)
        {
            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar);
            }
        }

        public static void RefreshUILabels(StarData star) //改变UI中显示的文本，不能再叫戴森球了。另外改变新增的设置巨构建筑类型的按钮的状态
        {
            try
            {
                if (!ProjectOrbitalRing.MoreMegaStructureCompatibility)
                {
                    if (star == null) return;
                    curStar = star;
                    int idx = star.id - 1;
                    idx = idx < 0 ? 0 : (idx > 999 ? 999 : idx);


                    //RightStarPowRatioText.text = "恒星功效系数".Translate();
                    //RightMaxPowGenText.text = "最大工作效率".Translate();
                    //RightDysonBluePrintText.text = "巨构建筑蓝图".Translate();

                    if (curStar.type == EStarType.BlackHole)
                    {
                        RightStarPowRatioText.text = "引力系数".Translate();

                        if (!GameMain.history.TechUnlocked(1802))
                        {
                            RightDysonTitle.text = "???".Translate() + " " + star.displayName;
                            RightMaxPowGenText.text = "???".Translate();
                        }
                        else if (!GameMain.history.TechUnlocked(1952))
                        {
                            RightDysonTitle.text = "共鸣阵列".Translate() + " " + star.displayName;
                            RightMaxPowGenText.text = "引力共鸣".Translate();
                        }
                        else
                        {
                            RightDysonTitle.text = "数学率引擎".Translate() + " " + star.displayName;
                            RightMaxPowGenText.text = "现实重构".Translate();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEOverview), "_OnUpdate")]
        public static void UIValueUpdate()
        {
            try
            {
                if (!ProjectOrbitalRing.MoreMegaStructureCompatibility)
                {
                    if (curStar.type == EStarType.BlackHole) {
                        long DysonEnergy = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick);

                        if (!GameMain.history.TechUnlocked(1802)) {
                            RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy) + "?";
                        } else if (!GameMain.history.TechUnlocked(1952)) {
                            RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy) + " g";
                        } else if (!GameMain.history.TechUnlocked(1960)) {
                            RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy / 2000) + "休谟";
                        } else {
                            long ThirdLevelEnergy = DysonEnergy - EnergyCalculate.SecondLevelEnergy;
                            double coefficient = ThirdLevelEnergy / 4000000.0;
                            RightMaxPowGenValueText.text = Capacity2Str((long)((EnergyCalculate.SecondLevelEnergy / 2000) * coefficient)) + "休谟";
                            //RightMaxPowGenValueText.text = coefficient.ToString() + "休谟";
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Debug.LogWarning("Unable to edit the DysonUI's PowerGen Value.");
            }
        }

        public static string Capacity2Str(long capacityPerSecond)
        {
            long midValue;
            string unitStr = "";
            if (capacityPerSecond >= 1000000000000000000)
            {
                midValue = capacityPerSecond / 1000000000000000;
                //return (midValue / 1000.0).ToString("G3") + " E";
                unitStr = " E";
            }
            else if (capacityPerSecond >= 1000000000000000)
            {
                midValue = capacityPerSecond / 1000000000000;
                //return (midValue / 1000.0).ToString("G3") + " P";
                unitStr = " P";
            }
            else if (capacityPerSecond >= 1000000000000)
            {
                midValue = capacityPerSecond / 1000000000;
                //return (midValue / 1000.0).ToString("G3") + " T";
                unitStr = " T";
            }
            else if (capacityPerSecond >= 1000000000)
            {
                midValue = capacityPerSecond / 1000000;
                //return (midValue / 1000.0).ToString("G3") + " G";
                unitStr = " G";
            }
            else if (capacityPerSecond >= 1000000)
            {
                midValue = capacityPerSecond / 1000;
                //return (midValue / 1000.0).ToString("G3") + " M";
                unitStr = " M";
            }
            else if (capacityPerSecond >= 1000)
            {
                midValue = capacityPerSecond;
                //return (midValue / 1000.0).ToString("G3") + " k";
                unitStr = " k";
            }
            else
            {
                return (capacityPerSecond + " ");
            }

            if (midValue >= 100000)
            {
                return (midValue / 1000) + unitStr;
            }

            if (midValue >= 10000)
            {
                return ((midValue / 100) / 10.0) + unitStr;
            }

            return ((midValue / 10) / 100.0) + unitStr;
        }
    }
}
