using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GalacticScale.NEB;

namespace ProjectOrbitalRing.Patches.Logic.AddVein
{
    internal class OilVeinPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), "InternalUpdate")]
        public static void InternalUpdatePatch(MinerComponent __instance, VeinData[] veinPool, ref float miningRate)
        {
            if (__instance.type == EMinerType.Oil)
            {
                if (veinPool[__instance.veins[0]].type == EVeinType.DeepMagma)
                {
                    miningRate = 0f; // 深层熔岩不衰减
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static void _OnUpdatePatch(ref UIVeinDetailNode __instance)
        {
            VeinGroup veinGroup = __instance.inspectFactory.veinGroups[__instance.veinGroupIndex];
            if (veinGroup.count == 0 || veinGroup.type == EVeinType.None)
            {
                __instance._Close();
                return;
            }

            if (__instance.counter % 4 == 0 && __instance.showingAmount != veinGroup.amount)
            {
                __instance.showingAmount = veinGroup.amount;
                if (veinGroup.type == EVeinType.DeepMagma || veinGroup.type == EVeinType.Ice)
                {
                    __instance.infoText.text = veinGroup.count + "空格个".Translate() + __instance.veinProto.name + "产量".Translate() + ((float)veinGroup.amount * VeinData.oilSpeedMultiplier).ToString("0.0000") + "/s";
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIVeinDetailNode), "Refresh")]
        public static void RefreshPatch(ref UIVeinDetailNode __instance)
        {
            if (__instance.inspectFactory == null)
            {
                return;
            }

            if (__instance.veinGroupIndex >= __instance.inspectFactory.veinGroups.Length)
            {
                __instance._Close();
                return;
            }

            VeinGroup veinGroup = __instance.inspectFactory.veinGroups[__instance.veinGroupIndex];
            if (veinGroup.count == 0 || veinGroup.type == EVeinType.None)
            {
                __instance._Close();
                return;
            }

            __instance.veinProto = LDB.veins.Select((int)veinGroup.type);
            if (__instance.veinProto != null)
            {
                __instance.veinIcon.sprite = __instance.veinProto.iconSprite;
                __instance.showingAmount = veinGroup.amount;
                if (veinGroup.type == EVeinType.DeepMagma || veinGroup.type == EVeinType.Ice)
                {
                    __instance.infoText.text = veinGroup.count + "空格个".Translate() + __instance.veinProto.name + "产量".Translate() + ((float)veinGroup.amount * VeinData.oilSpeedMultiplier).ToString("0.0000") + "/s";
                    return;
                }
            }
            else
            {
                __instance.veinIcon.sprite = null;
                __instance.showingAmount = veinGroup.amount;
                if (__instance.menuButton != null)
                {
                    __instance.menuButton.gameObject.SetActive(value: false);
                }

                if (veinGroup.type == EVeinType.DeepMagma || veinGroup.type == EVeinType.Ice)
                {
                    __instance.infoText.text = veinGroup.count + "空格个".Translate() + " ? " + "产量".Translate() + ((float)veinGroup.amount * VeinData.oilSpeedMultiplier).ToString("0.0000") + "/s";
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIResAmountEntry), "SetInfo")]
        public static void SetInfoPatch(ref UIResAmountEntry __instance, string label, ref string strBuilderFormat)
        {
            if (label.Equals("深层熔岩") || label.Equals("水"))
            {
                strBuilderFormat = "         /s";
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
        public static bool RefreshDynamicPropertiesPatch(ref UIPlanetDetail __instance)
        {
            if (__instance.veinAmounts == null)
            {
                __instance.veinAmounts = new long[64];
            }

            if (__instance.veinCounts == null)
            {
                __instance.veinCounts = new int[64];
            }

            Array.Clear(__instance.veinAmounts, 0, __instance.veinAmounts.Length);
            Array.Clear(__instance.veinCounts, 0, __instance.veinCounts.Length);
            bool isInfiniteResource = GameMain.data.gameDesc.isInfiniteResource;
            if (__instance.planet == null)
            {
                return true;
            }

            if (!__instance._scanned && __instance.planet.scanned)
            {
                __instance.OnPlanetDataSet();
                return true;
            }

            __instance._scanned = __instance.planet.scanned;
            int num = ((__instance.planet == GameMain.localPlanet) ? 1 : ((__instance.planet.star == GameMain.localStar) ? 2 : (((GameMain.mainPlayer.uPosition - __instance.planet.uPosition).magnitude < 14400000.0) ? 3 : 4)));
            bool flag = GameMain.history.universeObserveLevel >= num;
            if (__instance.planet.factory != null && GameMain.history.universeObserveLevel >= 1)
            {
                flag = true;
            }

            if (__instance._scanned && flag)
            {
                __instance.planet.SummarizeVeinAmountsByFilter(ref __instance.veinAmounts, __instance.tmp_ids, __instance.uiGame.veinAmountDisplayFilter);
                __instance.planet.SummarizeVeinCountsByFilter(ref __instance.veinCounts, __instance.tmp_ids, __instance.uiGame.veinAmountDisplayFilter);
            }

            foreach (UIResAmountEntry entry in __instance.entries)
            {
                if (entry.refId <= 0)
                {
                    continue;
                }

                if (flag)
                {
                    if (entry.refId == 7 || entry.refId == (int)EVeinType.DeepMagma || entry.refId == (int)EVeinType.Ice)
                    {
                        double num2 = (double)__instance.veinAmounts[entry.refId] * (double)VeinData.oilSpeedMultiplier;
                        if (__instance.uiGame.veinAmountDisplayFilter == 1)
                        {
                            num2 *= (double)GameMain.history.miningSpeedScale;
                        }

                        StringBuilderUtility.WritePositiveFloat(entry.sb, 0, 8, (float)num2);
                        entry.DisplayStringBuilder();
                    }
                    else
                    {
                        long num3 = __instance.veinAmounts[entry.refId];
                        int num4 = __instance.veinCounts[entry.refId];
                        if (isInfiniteResource)
                        {
                            StringBuilderUtility.WriteCommaULong(entry.sb, 0, 16, (ulong)num4);
                        }
                        else if (num3 < 1000000000)
                        {
                            StringBuilderUtility.WriteCommaULong(entry.sb, 0, 16, (ulong)num3);
                        }
                        else
                        {
                            StringBuilderUtility.WriteKMG(entry.sb, 15, num3);
                        }

                        entry.DisplayStringBuilder();
                    }

                    entry.SetObserved(_observed: true);
                }
                else
                {
                    entry.valueString = "未知".Translate();
                    if (entry.refId > 7)
                    {
                        entry.overrideLabel = "未知珍奇信号".Translate();
                    }

                    if (entry.refId > 7)
                    {
                        entry.SetObserved(_observed: false);
                    }
                    else
                    {
                        entry.SetObserved(_observed: true);
                    }
                }
            }

            if (__instance.tipEntry != null)
            {
                if (!flag)
                {
                    __instance.tipEntry.valueString = "宇宙探索等级".Translate() + num;
                }
                else
                {
                    __instance.tipEntry.valueString = "";
                }

                __instance.SetResCount(flag ? (__instance.entries.Count - 1) : __instance.entries.Count);
            }
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarDetail), "RefreshDynamicProperties")]
        public static bool UIStarDetailRefreshDynamicPropertiesPatch(ref UIStarDetail __instance)
        {
            bool isInfiniteResource = GameMain.data.gameDesc.isInfiniteResource;
            if (__instance.veinAmounts == null)
            {
                __instance.veinAmounts = new long[64];
            }

            if (__instance.veinCounts == null)
            {
                __instance.veinCounts = new int[64];
            }

            Array.Clear(__instance.veinAmounts, 0, __instance.veinAmounts.Length);
            Array.Clear(__instance.veinCounts, 0, __instance.veinCounts.Length);
            if (__instance.star == null)
            {
                return true;
            }

            if (!__instance.calculated && __instance.star.scanned)
            {
                __instance.OnStarDataSet();
                return false;
            }

            __instance.calculated = __instance.star.scanned;
            bool num = __instance.observed;
            double magnitude = (__instance.star.uPosition - GameMain.mainPlayer.uPosition).magnitude;
            int num2 = ((__instance.star == GameMain.localStar) ? 2 : ((magnitude < 14400000.0) ? 3 : 4));
            __instance.observed = GameMain.history.universeObserveLevel >= num2;
            if (num != __instance.observed)
            {
                __instance.OnStarDataSet();
                return false;
            }

            __instance.loadingTextGo.SetActive(__instance.observed && !__instance.calculated);
            if (__instance.calculated && __instance.observed)
            {
                __instance.star.CalcVeinAmounts(ref __instance.veinAmounts, __instance.tmp_ids, __instance.uiGame.veinAmountDisplayFilter);
                __instance.star.CalcVeinCounts(ref __instance.veinCounts, __instance.tmp_ids, __instance.uiGame.veinAmountDisplayFilter);
            }

            foreach (UIResAmountEntry entry in __instance.entries)
            {
                if (entry.refId <= 0)
                {
                    continue;
                }

                if (__instance.observed)
                {
                    long num3 = __instance.veinAmounts[entry.refId];
                    long value = __instance.veinCounts[entry.refId];
                    if (entry.refId == 7 || entry.refId == (int)EVeinType.DeepMagma || entry.refId == (int)EVeinType.Ice)
                    {
                        double num4 = (double)num3 * (double)VeinData.oilSpeedMultiplier;
                        if (__instance.uiGame.veinAmountDisplayFilter == 1)
                        {
                            num4 *= (double)GameMain.history.miningSpeedScale;
                        }

                        StringBuilderUtility.WritePositiveFloat(entry.sb, 0, 8, (float)num4, 2, ' ');
                        entry.DisplayStringBuilder();
                    }
                    else
                    {
                        if (isInfiniteResource)
                        {
                            StringBuilderUtility.WriteCommaULong(entry.sb, 0, 16, (ulong)value, 1, ' ');
                        }
                        else if (num3 < 1000000000)
                        {
                            StringBuilderUtility.WriteCommaULong(entry.sb, 0, 16, (ulong)num3, 1, ' ');
                        }
                        else
                        {
                            StringBuilderUtility.WriteKMG(entry.sb, 15, num3, false, '\u2009', ' ');
                        }

                        entry.DisplayStringBuilder();
                    }

                    entry.SetObserved(_observed: true);
                }
                else
                {
                    entry.valueString = "未知".Translate();
                    if (entry.refId != 15)
                    {
                        entry.overrideLabel = "未知珍奇信号".Translate();
                    }

                    if (entry.refId != 15)
                    {
                        entry.SetObserved(_observed: false);
                    }
                    else
                    {
                        entry.SetObserved(_observed: true);
                    }
                }
            }

            if (__instance.tipEntry != null)
            {
                if (!__instance.observed)
                {
                    __instance.tipEntry.valueString = "宇宙探索等级".Translate() + num2;
                }
                else
                {
                    __instance.tipEntry.valueString = "";
                }

                __instance.SetResCount(__instance.observed ? (__instance.entries.Count - 1) : __instance.entries.Count);
            }
            return false;
        }
    
    }
}
