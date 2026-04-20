using System.Collections.Generic;
using BepInEx.Bootstrap;
using GalacticScale;

namespace ProjectOrbitalRing.Utils
{
    internal static class PlanetThemeUtils
    {
        private const string GS2_GUID = "dsp.galactic-scale.2";

        private static readonly Dictionary<string, int> ThemeNameToVanillaThemeId = new Dictionary<string, int>
        {
            ["Mediterranean"] = 1,
            ["AshenGelisol"] = 1,
            ["GasGiant"] = 2,
            ["GasGiant2"] = 3,
            ["GasGiant3"] = 2,
            ["GasGiant4"] = 3,
            ["IceGiant"] = 4,
            ["IceGiant2"] = 5,
            ["IceGiant3"] = 4,
            ["IceGiant4"] = 5,
            ["AridDesert"] = 6,
            ["AridDesert2"] = 6,
            ["OceanicJungle"] = 8,
            ["Lava"] = 9,
            ["IceGelisol"] = 10,
            ["IceGelisol2"] = 10,
            ["IceGelisol3"] = 10,
            ["Barren"] = 11,
            ["Gobi"] = 12,
            ["VolcanicAsh"] = 13,
            ["RedStone"] = 14,
            ["Prairie"] = 15,
            ["OceanWorld"] = 16,
            ["SaltLake"] = 17,
            ["Sakura"] = 18,
            ["Hurricane"] = 19,
            ["IceLake"] = 20,
            ["GasGiant5"] = 21,
            ["Savanna"] = 22,
            ["CrystalDesert"] = 23,
            ["FrozenTundra"] = 24,
            ["PandoraSwamp"] = 25,
            ["PandoraSwamp2"] = 25,
        };

        internal static int GetVanillaThemeId(PlanetData planet)
        {
            if (planet == null) return 0;

            if (!Chainloader.PluginInfos.ContainsKey(GS2_GUID))
            {
                return planet.theme;
            }

            try
            {
                GSPlanet gsPlanet = GS2.GetGSPlanet(planet);
                if (gsPlanet != null && TryResolveVanillaThemeId(gsPlanet.Theme, out int vanillaThemeId))
                {
                    return vanillaThemeId;
                }
            }
            catch
            {
                // 防御性兜底，避免 Touhma 版 GS2 未来改变 API 时引发新崩溃
            }

            return planet.theme;
        }

        internal static string GetThemeDebugInfo(PlanetData planet)
        {
            if (planet == null) return "planet=<null>";

            string gsThemeName = null;
            string gsBaseName = null;
            int gsThemeLdbThemeId = -1;
            string gsError = null;

            if (Chainloader.PluginInfos.ContainsKey(GS2_GUID))
            {
                try
                {
                    GSPlanet gsPlanet = GS2.GetGSPlanet(planet);
                    gsThemeName = gsPlanet?.Theme;
                    GSTheme gsTheme = gsPlanet?.GsTheme;
                    gsBaseName = gsTheme?.BaseName;
                    if (gsTheme != null) gsThemeLdbThemeId = gsTheme.LDBThemeId;
                }
                catch (System.Exception ex)
                {
                    gsError = ex.GetType().Name + ": " + ex.Message;
                }
            }

            return $"planetId={planet.id}, displayName={planet.displayName}, planet.theme={planet.theme}, planet.type={planet.type}, gsTheme={gsThemeName ?? "<null>"}, gsBase={gsBaseName ?? "<null>"}, gsTheme.LDBThemeId={gsThemeLdbThemeId}, resolvedVanillaTheme={GetVanillaThemeId(planet)}{(string.IsNullOrEmpty(gsError) ? string.Empty : ", gsError=" + gsError)}";
        }

        private static bool TryResolveVanillaThemeId(string themeName, out int vanillaThemeId)
        {
            return TryResolveVanillaThemeId(themeName, new HashSet<string>(), out vanillaThemeId);
        }

        private static bool TryResolveVanillaThemeId(string themeName, HashSet<string> visited, out int vanillaThemeId)
        {
            vanillaThemeId = 0;

            if (string.IsNullOrEmpty(themeName)) return false;
            if (!visited.Add(themeName)) return false;

            if (ThemeNameToVanillaThemeId.TryGetValue(themeName, out vanillaThemeId))
            {
                return true;
            }

            if (!GSSettings.ThemeLibrary.ContainsKey(themeName)) return false;

            GSTheme theme = GSSettings.ThemeLibrary[themeName];
            if (theme == null) return false;

            if (ThemeNameToVanillaThemeId.TryGetValue(theme.Name, out vanillaThemeId))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(theme.BaseName))
            {
                return TryResolveVanillaThemeId(theme.BaseName, visited, out vanillaThemeId);
            }

            return false;
        }
    }
}
