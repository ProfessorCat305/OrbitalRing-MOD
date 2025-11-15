using System;
using System.Linq;
using System.Reflection;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.AddVein
{
    internal static partial class ModifyPlanetTheme
    {
        internal static void ModifyPlanetThemeDataVanilla()
        {
            foreach (ThemeProto theme in LDB.themes.dataArray) ModifyThemeVanilla(theme);
        }

        private static void ModifyThemeVanilla(ThemeProto theme)
        {
            if (theme.PlanetType == EPlanetType.Gas) { GasGiantModify(theme); }
            else
            {
                ModifyThemeData(theme);

                switch (theme.ID)
                {
                    case 8:
                        theme.BriefIntroduction = "海洋丛林介绍";
                        break;

                    case 9:
                        theme.WaterItemId = ProtoID.I岩浆;
                        break;

                    case 10:
                        theme.BriefIntroduction = "冰原冻土介绍";
                        theme.WaterItemId = 6205; // 甲烷
                        theme.WaterHeight = -0.5f;
                        theme.Distribute = EThemeDistribute.Interstellar;
                        break;

                    case 17:
                        theme.DisplayName = "黑海盐滩";
                        theme.displayName = theme.DisplayName.Translate();
                        theme.WaterItemId = ProtoID.I原油;
                        theme.WaterHeight = 0.0f;
                        theme.Distribute = EThemeDistribute.Interstellar;
                        //theme.Algos = new[] { 3, };
                        theme.oceanMat = LDB.themes.Select(8).oceanMat;

                        break;

                    case 19:
                        theme.Wind = 8;
                        break;
                    case 25:
                        theme.BriefIntroduction = "潘多拉沼泽介绍";
                        break;
                }
            }
        }

        private static void GasGiantModify(ThemeProto theme)
        {
            if (theme.GasItems.Length != 2) return;

            if (theme.GasItems[0] == ProtoID.I可燃冰 && theme.GasItems[1] == ProtoID.I氢)
            {
                theme.GasItems = new[] { ProtoID.I氢, ProtoID.I甲烷, ProtoID.I气云, };
                theme.GasSpeeds = new float[] { theme.GasSpeeds[0], theme.GasSpeeds[1], theme.GasSpeeds[1] + (theme.GasSpeeds[0] / 4), };
            } else if (theme.GasItems[0] == ProtoID.I氢 && theme.GasItems[1] == ProtoID.I重氢) {
                theme.GasItems = new[] { ProtoID.I氢, ProtoID.I重氢, ProtoID.I气云, };
                theme.GasSpeeds = new float[] { theme.GasSpeeds[0], theme.GasSpeeds[1], theme.GasSpeeds[1] + (theme.GasSpeeds[0] / 4), };
            }
        }

        private static void ModifyThemeData(ThemeProto theme)
        {
            Array.Resize(ref theme.VeinSpot, 16);
            Array.Resize(ref theme.VeinCount, 16);
            Array.Resize(ref theme.VeinOpacity, 16);

            if (ThemeDatas.TryGetValue(theme.ID, out ThemeData value))
            {

                for (int i = 0; i < value.VeinId.Length; i++)
                {
                    theme.VeinSpot[value.VeinId[i]] = value.VeinSpot[i];
                    theme.VeinCount[value.VeinId[i]] = value.VeinCount[i];
                    theme.VeinOpacity[value.VeinId[i]] = value.VeinOpacity[i];
                }
                for (int i = 0; i < value.VanillaRareVeins.Length; i++)
                {
                    theme.RareSettings[value.VanillaRareVeins[i] * 4] = value.VanillaRareSettings[i * 4];
                    theme.RareSettings[value.VanillaRareVeins[i] * 4 + 1] = value.VanillaRareSettings[i * 4 + 1];
                    theme.RareSettings[value.VanillaRareVeins[i] * 4 + 2] = value.VanillaRareSettings[i * 4 + 2];
                    theme.RareSettings[value.VanillaRareVeins[i] * 4 + 3] = value.VanillaRareSettings[i * 4 + 3];
                }
                theme.RareVeins = theme.RareVeins.Concat(value.RareVeins).ToArray();
                theme.RareSettings = theme.RareSettings.Concat(value.RareSettings).ToArray();
            }
        }
    }
}
