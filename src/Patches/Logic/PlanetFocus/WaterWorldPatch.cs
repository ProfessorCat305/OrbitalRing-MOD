using System;
using ProjectOrbitalRing.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOrbitalRing.Patches.Logic.PlanetFocus
{
    internal class WaterWorldPatch
    {
        private static readonly int[] UseWaterRecipes = { 5, 16, 24, 64, 403, 404, 512, 513, 517, 518, 702, 708, 711, 714, 797, 813, 836 };
        public static void SetAssmeblerWaterFull(ref AssemblerComponent __instance, PlanetFactory factory)
        {
            if (PlanetThemeUtils.GetVanillaThemeId(factory.planet) != 16) {
                return;
            }
            for (int i = 0; i < UseWaterRecipes.Length; i++) {
                if (__instance.recipeId == UseWaterRecipes[i]) {
                    for (int j = 0; j < __instance.recipeExecuteData.requires.Length; j++) {
                        if (__instance.needs[j] == 1000) {
                            //int num2 = __instance.speedOverride * 180 / __instance.timeSpend + 1;
                            //if (num2 < 2) {
                            //    num2 = 2;
                            //}
                            int num2 = 2;
                            __instance.served[j] = __instance.recipeExecuteData.requireCounts[j] * num2;
                            return;
                        }
                    }
                }
            }
        }
    }
}
