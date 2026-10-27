using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Compatibility
{
    internal static class GalacticScaleGalaxy
    {
        internal static void PatchGenerator(Harmony harmony, Type generatorType)
        {
            harmony.Patch(AccessTools.Method(generatorType, "InitForcedStars"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(InitForcedStars_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "GenerateStars"),
                prefix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(GenerateStars_Prefix)),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(GenerateStars_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "GetStarPlanetCount"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(GetStarPlanetCount_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "CalculateThemeHeat"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(CalculateThemeHeat_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "CalculateMinimumOrbit"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(CalculateMinimumOrbit_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "CalculateMaximumOrbit"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(CalculateMaximumOrbit_Postfix)));
            harmony.Patch(AccessTools.Method(generatorType, "InitThemes"),
                postfix: new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(InitThemes_Postfix)));
        }

        internal static GSGenPreferences GetPreferences(object generator)
        {
            return (GSGenPreferences)AccessTools.Field(generator.GetType(), "preferences").GetValue(generator);
        }

        private static bool UsesDefaultOrbits(object generator, GSStar star, out string settingsPrefix)
        {
            var method = AccessTools.Method(generator.GetType(), "GetTypeLetterFromStar");
            settingsPrefix = (string)method.Invoke(generator, new object[] { star });
            return !GetPreferences(generator).GetBool(settingsPrefix + "orbitOverride") &&
                !(bool)star.genData.Get("hasBinary", false) && !IsCompactStar(star);
        }

        public static void CalculateMinimumOrbit_Postfix(object __instance, GSStar star, ref float __result)
        {
            if (!UsesDefaultOrbits(__instance, star, out _))
            {
                return;
            }

            float innerZone = Math.Max(0.01f, (float)star.genData.Get("minHZ") - star.RadiusAU);
            __result = Math.Max(star.RadiusAU * 1.1f, star.RadiusAU + innerZone * 0.1f);
            star.genData.Set("minOrbit", __result);
        }

        public static void CalculateThemeHeat_Postfix(GSStar star, float OrbitRadius, ref EThemeHeat __result)
        {
            if (__result != EThemeHeat.Temperate)
            {
                return;
            }

            object generator = GS2.ActiveGenerator;
            if (!UsesDefaultOrbits(generator, star, out string settingsPrefix))
            {
                return;
            }
            if (GetPreferences(generator).GetBool(settingsPrefix + "hzOverride"))
            {
                return;
            }

            float minimum = star.genData.Get("minHZ");
            float maximum = star.genData.Get("maxHZ");
            // Theme climate uses a narrower outer temperate band without changing orbital geometry.
            if (OrbitRadius >= maximum - (maximum - minimum) * 0.2f)
            {
                __result = EThemeHeat.Cold;
            }
        }

        public static void CalculateMaximumOrbit_Postfix(object __instance, GSStar star, ref float __result)
        {
            if (!UsesDefaultOrbits(__instance, star, out _))
            {
                return;
            }

            float minimum = star.genData.Get("minOrbit");
            float outerZone = Math.Max(0.01f, (float)star.genData.Get("maxHZ") - star.RadiusAU);
            float spacing = GetPreferences(__instance).GetFloat("orbitSpacing", 0.05f);
            // Orbit splitting reserves twice each system radius on both sides of a body.
            float capacity = star.Planets.Sum(body => body.SystemRadius * 4f + spacing * 2f);
            __result = Mathf.Min(star.MaxOrbit, Math.Max(minimum + capacity, star.RadiusAU + outerZone * 1.4f));
            star.genData.Set("maxOrbit", __result);
        }

        public static void InitThemes_Postfix()
        {
            // Obsidian has no liquid surface; its resources follow the rocky desert theme mapping.
            foreach (string name in new[] { "Obsidian", "HotObsidian" }) {
                if (!GSSettings.ThemeLibrary.TryGetValue(name, out var theme))
                {
                    continue;
                }

                theme.PlanetType = EPlanetType.Desert;
                theme.WaterItemId = 0;
            }
        }

        public static IEnumerable<CodeInstruction> SelectPlanetThemes_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var query = AccessTools.Method(typeof(ThemeLibrary), nameof(ThemeLibrary.Query), new[] {
                typeof(GS2.Random), typeof(EThemeType), typeof(EThemeHeat), typeof(int),
                typeof(EThemeDistribute), typeof(bool),
            });
            var select = AccessTools.Method(typeof(GalacticScaleGalaxy), nameof(SelectResourceTheme));
            int replacementCount = 0;
            foreach (var instruction in instructions) {
                if (instruction.Calls(query)) {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = select;
                    replacementCount++;
                }
                yield return instruction;
            }
            if (replacementCount == 0)
            {
                throw new InvalidOperationException("Unsupported GS theme selection method.");
            }
        }

        public static string SelectResourceTheme(ThemeLibrary library, GS2.Random random, EThemeType type,
            EThemeHeat heat, int radius, EThemeDistribute distribute, bool habitable)
        {
            List<string> candidates = library.Query(type, heat, radius, distribute, habitable);
            if (type == EThemeType.Gas || habitable)
            {
                return random.Item(candidates);
            }

            // Theme variants share a resource-family weight without changing eligibility filters.
            var families = candidates.GroupBy(name => {
                int id = PlanetThemeUtils.ResolveGSThemeId(name, library);
                return id == 0 ? name : id.ToString();
            }).Select(group => group.ToList()).ToList();

            return random.Item(random.Item(families));
        }

        public static void InitForcedStars_Postfix(List<string> ____forcedStars)
        {
            if (!____forcedStars.Contains("BH"))
            {
                ____forcedStars.Add("BH");
            }

            if (!____forcedStars.Contains("NS"))
            {
                ____forcedStars.Add("NS");
            }
        }

        public static void GenerateStars_Prefix(ref int starCount, List<string> ____forcedStars)
        {
            starCount = Math.Max(starCount, Math.Max(3, ____forcedStars.Count + 1));
        }

        public static void GenerateStars_Postfix(object __instance, ref GSStar ___birthStar)
        {
            GSGenPreferences preferences = GetPreferences(__instance);
            var stars = GSSettings.Stars;
            if (IsCompactStar(___birthStar)) {
                var candidates = stars.Where(star => !star.Decorative && star.Type == EStarType.MainSeqStar).ToList();
                if (candidates.Count == 0)
                {
                    candidates = stars.Where(star => !star.Decorative && !IsCompactStar(star)).ToList();
                }

                if (candidates.Count == 0) {
                    GSStar source = stars.FirstOrDefault(star => IsCompactStar(star) &&
                        stars.Count(other => !other.Decorative && other.Type == star.Type) > 1);
                    if (source == null)
                    {
                        throw new InvalidOperationException("GS galaxy cannot accommodate a regular birth star and both compact star types.");
                    }

                    var regularBirthStar = new GSStar(source.Seed, source.Name, ESpectrType.G, EStarType.MainSeqStar, new GSPlanets()) {
                        position = source.position,
                        BinaryCompanion = source.BinaryCompanion,
                        MaxOrbit = source.MaxOrbit,
                    };
                    foreach (var entry in source.genData)
                    {
                        regularBirthStar.genData.Set(entry.Key, entry.Value);
                    }
                    regularBirthStar.radius *= preferences.GetFloat("starSizeMulti", 10f);
                    stars[stars.IndexOf(source)] = regularBirthStar;
                    candidates.Add(regularBirthStar);
                }

                ___birthStar = new GS2.Random(GSSettings.Seed).Item(candidates);
                ProjectOrbitalRing.LogWarning("OrbitalRing birth system assigned to " + ___birthStar.Name + " (" + ___birthStar.Type + ").");
            }

            GSStar birthStar = ___birthStar;
            foreach (EStarType type in new[] { EStarType.BlackHole, EStarType.NeutronStar })
            {
                if (stars.Any(star => !star.Decorative && star.Type == type))
                {
                    continue;
                }

                GSStar candidate = stars.LastOrDefault(star => star != birthStar && !star.Decorative &&
                    !IsCompactStar(star) && string.IsNullOrEmpty(star.BinaryCompanion));
                GSStar source = candidate ?? ___birthStar ?? stars.FirstOrDefault();
                if (source == null)
                {
                    throw new InvalidOperationException("GS galaxy has no primary star.");
                }

                var replacement = new GSStar(source.Seed, source.Name, ESpectrType.X, type, new GSPlanets())
                {
                    position = source.position,
                };
                float starSizeMultiplier = preferences.GetFloat("starSizeMulti", 10f);
                if (type != EStarType.BlackHole || starSizeMultiplier < 2.01f)
                {
                    replacement.radius *= starSizeMultiplier;
                }
                if (candidate != null)
                {
                    stars[stars.IndexOf(candidate)] = replacement;
                }
                else
                {
                    replacement.Name = source.Name + (type == EStarType.BlackHole ? " BH" : " NS");
                    stars.Add(replacement);
                }
            }
        }

        internal static bool IsCompactStar(GSStar star)
        {
            return star != null && !star.Decorative &&
                (star.Type == EStarType.BlackHole || star.Type == EStarType.NeutronStar);
        }

        public static void GetStarPlanetCount_Postfix(GSStar star, ref int __result)
        {
            if (IsCompactStar(star))
            {
                __result = Math.Max(1, __result);
            }
        }
    }
}
