using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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
            if (theme.PlanetType == EPlanetType.Gas) {
                GasGiantModify(ref theme.GasItems, ref theme.GasSpeeds);
            }
            else
            {
                ModifyThemeData(theme.ID, ref theme.VeinSpot, ref theme.VeinCount, ref theme.VeinOpacity,
                    ref theme.RareVeins, ref theme.RareSettings);

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
                        theme.displayName = theme.DisplayName.TranslateFromJson();
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

        [HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.CalcWaterPercent))]
        [HarmonyPrefix]
        public static void PlanetAlgorithm_RegenerateTerrain(PlanetAlgorithm __instance)
        {
            PlanetData planet = __instance.planet;

            switch (planet.waterItemId) {
                case ProtoID.I原油:
                    var planetAlgorithm3 = new PlanetAlgorithm3();
                    planetAlgorithm3.Reset(planet.seed, planet);
                    planetAlgorithm3.GenerateTerrain(planet.mod_x, planet.mod_y);
                    break;
            }
        }

        internal static void GasGiantModify(ref int[] items, ref float[] speeds)
        {
            if (items.Length != 2)
            {
                return;
            }

            if (items[0] == ProtoID.I可燃冰 && items[1] == ProtoID.I氢)
            {
                items = new[] { ProtoID.I氢, ProtoID.I甲烷, ProtoID.I气云, };
                speeds = new float[] { speeds[0], speeds[1], (speeds[1] / 18) + (speeds[0] / 18), };
            } else if (items[0] == ProtoID.I氢 && items[1] == ProtoID.I重氢) {
                items = new[] { ProtoID.I氢, ProtoID.I重氢, ProtoID.I气云, };
                speeds = new float[] { speeds[0], speeds[1], (speeds[1] / 18) + (speeds[0] / 18), };
            }
        }

        internal static void ModifyThemeData(int themeId, ref int[] spots, ref float[] counts,
            ref float[] richness, ref int[] rareVeins, ref float[] rareSettings)
        {
            Array.Resize(ref spots, 16);
            Array.Resize(ref counts, 16);
            Array.Resize(ref richness, 16);

            if (ThemeDatas.TryGetValue(themeId, out ThemeData value))
            {

                for (int i = 0; i < value.VeinId.Length; i++)
                {
                    spots[value.VeinId[i]] = value.VeinSpot[i];
                    counts[value.VeinId[i]] = value.VeinCount[i];
                    richness[value.VeinId[i]] = value.VeinOpacity[i];
                }
                for (int i = 0; i < value.VanillaRareVeins.Length; i++)
                {
                    rareSettings[value.VanillaRareVeins[i] * 4] = value.VanillaRareSettings[i * 4];
                    rareSettings[value.VanillaRareVeins[i] * 4 + 1] = value.VanillaRareSettings[i * 4 + 1];
                    rareSettings[value.VanillaRareVeins[i] * 4 + 2] = value.VanillaRareSettings[i * 4 + 2];
                    rareSettings[value.VanillaRareVeins[i] * 4 + 3] = value.VanillaRareSettings[i * 4 + 3];
                }
                rareVeins = rareVeins.Concat(value.RareVeins).ToArray();
                rareSettings = rareSettings.Concat(value.RareSettings).ToArray();
            }
        }
    }
}
