using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;

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

        private static readonly ConcurrentDictionary<string, int?> ResolvedThemeNameCache = new ConcurrentDictionary<string, int?>();
        private static readonly ConditionalWeakTable<PlanetData, CachedPlanetThemeValue> CachedPlanetVanillaThemeIds =
            new ConditionalWeakTable<PlanetData, CachedPlanetThemeValue>();
        private static readonly object GS2ReflectionLock = new object();
        private static readonly BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static bool GS2ReflectionInitialized;
        private static bool GS2ReflectionAvailable;

        private static MethodInfo GS2GetGSPlanetMethod;
        private static MemberInfo GSPlanetThemeMember;
        private static MemberInfo GSPlanetGsThemeMember;
        private static MemberInfo GSThemeNameMember;
        private static MemberInfo GSThemeBaseNameMember;
        private static MemberInfo GSThemeLdbThemeIdMember;
        private static MemberInfo GSSettingsThemeLibraryMember;

        private sealed class CachedPlanetThemeValue
        {
            internal CachedPlanetThemeValue(int vanillaThemeId)
            {
                VanillaThemeId = vanillaThemeId;
            }

            internal int VanillaThemeId { get; }
        }

        internal static int GetVanillaThemeId(PlanetData planet)
        {
            if (planet == null) return 0;

            if (!Chainloader.PluginInfos.ContainsKey(GS2_GUID))
            {
                return planet.theme;
            }

            if (CachedPlanetVanillaThemeIds.TryGetValue(planet, out CachedPlanetThemeValue cachedPlanetThemeValue))
            {
                return cachedPlanetThemeValue.VanillaThemeId;
            }

            try
            {
                int? vanillaThemeId = GetVanillaThemeIdFromGS2(planet);
                if (vanillaThemeId.HasValue)
                {
                    CachedPlanetVanillaThemeIds.GetValue(planet, _ => new CachedPlanetThemeValue(vanillaThemeId.Value));
                    return vanillaThemeId.Value;
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
                    FillThemeDebugInfoFromGS2(planet, out gsThemeName, out gsBaseName, out gsThemeLdbThemeId);
                }
                catch (Exception ex)
                {
                    gsError = ex.GetType().Name + ": " + ex.Message;
                }
            }

            return $"planetId={planet.id}, displayName={planet.displayName}, planet.theme={planet.theme}, planet.type={planet.type}, gsTheme={gsThemeName ?? "<null>"}, gsBase={gsBaseName ?? "<null>"}, gsTheme.LDBThemeId={gsThemeLdbThemeId}, resolvedVanillaTheme={GetVanillaThemeId(planet)}{(string.IsNullOrEmpty(gsError) ? string.Empty : ", gsError=" + gsError)}";
        }

        private static int? GetVanillaThemeIdFromGS2(PlanetData planet)
        {
            if (!EnsureGS2Reflection()) return null;

            object gsPlanet = InvokeGS2GetGSPlanet(planet);
            if (gsPlanet == null) return null;

            string themeName = GetMemberValue<string>(gsPlanet, GSPlanetThemeMember);
            if (TryResolveVanillaThemeIdFromGS2(themeName, out int vanillaThemeId))
            {
                return vanillaThemeId;
            }

            return null;
        }

        private static void FillThemeDebugInfoFromGS2(PlanetData planet, out string gsThemeName, out string gsBaseName,
            out int gsThemeLdbThemeId)
        {
            gsThemeName = null;
            gsBaseName = null;
            gsThemeLdbThemeId = -1;

            if (!EnsureGS2Reflection()) return;

            object gsPlanet = InvokeGS2GetGSPlanet(planet);
            if (gsPlanet == null) return;

            gsThemeName = GetMemberValue<string>(gsPlanet, GSPlanetThemeMember);

            object gsTheme = GetMemberValue<object>(gsPlanet, GSPlanetGsThemeMember);
            if (gsTheme == null) return;

            gsBaseName = GetMemberValue<string>(gsTheme, GSThemeBaseNameMember);
            int? ldbThemeId = GetMemberValue<int?>(gsTheme, GSThemeLdbThemeIdMember);
            if (ldbThemeId.HasValue)
            {
                gsThemeLdbThemeId = ldbThemeId.Value;
            }
        }

        private static bool TryResolveVanillaThemeIdFromGS2(string themeName, out int vanillaThemeId)
        {
            vanillaThemeId = 0;

            if (string.IsNullOrEmpty(themeName)) return false;

            if (ResolvedThemeNameCache.TryGetValue(themeName, out int? cachedVanillaThemeId))
            {
                if (cachedVanillaThemeId.HasValue)
                {
                    vanillaThemeId = cachedVanillaThemeId.Value;
                    return true;
                }

                return false;
            }

            IDictionary themeLibrary = GetGS2ThemeLibrary();
            bool resolved = TryResolveVanillaThemeIdFromGS2(themeName, new HashSet<string>(), themeLibrary, out vanillaThemeId);

            ResolvedThemeNameCache[themeName] = resolved ? (int?)vanillaThemeId : null;
            return resolved;
        }

        private static bool TryResolveVanillaThemeIdFromGS2(string themeName, HashSet<string> visited, IDictionary themeLibrary,
            out int vanillaThemeId)
        {
            vanillaThemeId = 0;

            if (string.IsNullOrEmpty(themeName)) return false;
            if (!visited.Add(themeName)) return false;

            if (ThemeNameToVanillaThemeId.TryGetValue(themeName, out vanillaThemeId))
            {
                return true;
            }

            if (TryResolveSmolThemeIdFromGS2(themeName, visited, themeLibrary, out vanillaThemeId))
            {
                return true;
            }

            if (themeLibrary == null || !themeLibrary.Contains(themeName)) return false;

            object theme = themeLibrary[themeName];
            if (theme == null) return false;

            string reflectedThemeName = GetMemberValue<string>(theme, GSThemeNameMember);
            if (!string.IsNullOrEmpty(reflectedThemeName) &&
                ThemeNameToVanillaThemeId.TryGetValue(reflectedThemeName, out vanillaThemeId))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(reflectedThemeName) &&
                TryResolveSmolThemeIdFromGS2(reflectedThemeName, visited, themeLibrary, out vanillaThemeId))
            {
                return true;
            }

            string baseName = GetMemberValue<string>(theme, GSThemeBaseNameMember);
            if (!string.IsNullOrEmpty(baseName))
            {
                return TryResolveVanillaThemeIdFromGS2(baseName, visited, themeLibrary, out vanillaThemeId);
            }

            return false;
        }

        private static bool TryResolveSmolThemeIdFromGS2(string themeName, HashSet<string> visited, IDictionary themeLibrary,
            out int vanillaThemeId)
        {
            vanillaThemeId = 0;

            if (string.IsNullOrEmpty(themeName)) return false;
            if (!themeName.EndsWith("smol", StringComparison.Ordinal)) return false;
            if (themeName.Length <= 4) return false;

            string trimmedThemeName = themeName.Substring(0, themeName.Length - 4);
            if (string.IsNullOrEmpty(trimmedThemeName)) return false;

            if (ThemeNameToVanillaThemeId.TryGetValue(trimmedThemeName, out vanillaThemeId))
            {
                return true;
            }

            return TryResolveVanillaThemeIdFromGS2(trimmedThemeName, visited, themeLibrary, out vanillaThemeId);
        }

        private static IDictionary GetGS2ThemeLibrary()
        {
            if (!EnsureGS2Reflection()) return null;
            return GetStaticMemberValue<object>(GSSettingsThemeLibraryMember) as IDictionary;
        }

        private static bool EnsureGS2Reflection()
        {
            if (GS2ReflectionInitialized) return GS2ReflectionAvailable;

            lock (GS2ReflectionLock)
            {
                if (GS2ReflectionInitialized) return GS2ReflectionAvailable;

                GS2ReflectionAvailable = InitializeGS2Reflection();
                GS2ReflectionInitialized = true;
                return GS2ReflectionAvailable;
            }
        }

        private static bool InitializeGS2Reflection()
        {
            if (!Chainloader.PluginInfos.TryGetValue(GS2_GUID, out BepInEx.PluginInfo pluginInfo)) return false;

            Assembly assembly = pluginInfo.Instance.GetType().Assembly;
            Type gs2Type = assembly.GetType("GalacticScale.GS2");
            Type gsPlanetType = assembly.GetType("GalacticScale.GSPlanet");
            Type gsThemeType = assembly.GetType("GalacticScale.GSTheme");
            Type gsSettingsType = assembly.GetType("GalacticScale.GSSettings");

            if (gs2Type == null || gsPlanetType == null || gsThemeType == null || gsSettingsType == null)
            {
                return false;
            }

            GS2GetGSPlanetMethod = FindStaticMethod(gs2Type, "GetGSPlanet", typeof(PlanetData));
            GSPlanetThemeMember = FindMember(gsPlanetType, "Theme", false);
            GSPlanetGsThemeMember = FindMember(gsPlanetType, "GsTheme", false);
            GSThemeNameMember = FindMember(gsThemeType, "Name", false);
            GSThemeBaseNameMember = FindMember(gsThemeType, "BaseName", false);
            GSThemeLdbThemeIdMember = FindMember(gsThemeType, "LDBThemeId", false);
            GSSettingsThemeLibraryMember = FindMember(gsSettingsType, "ThemeLibrary", true);

            return GS2GetGSPlanetMethod != null &&
                   GSPlanetThemeMember != null &&
                   GSPlanetGsThemeMember != null &&
                   GSThemeNameMember != null &&
                   GSThemeBaseNameMember != null &&
                   GSThemeLdbThemeIdMember != null &&
                   GSSettingsThemeLibraryMember != null;
        }

        private static MethodInfo FindStaticMethod(Type type, string name, Type parameterType)
        {
            foreach (MethodInfo method in type.GetMethods(AnyStatic))
            {
                if (method.Name != name) continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1) continue;
                if (parameters[0].ParameterType != parameterType) continue;

                return method;
            }

            return null;
        }

        private static MemberInfo FindMember(Type type, string name, bool isStatic)
        {
            BindingFlags flags = isStatic ? AnyStatic : AnyInstance;

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.GetGetMethod(true) != null)
            {
                return property;
            }

            FieldInfo field = type.GetField(name, flags);
            if (field != null)
            {
                return field;
            }

            return null;
        }

        private static object InvokeGS2GetGSPlanet(PlanetData planet)
        {
            if (GS2GetGSPlanetMethod == null) return null;
            return GS2GetGSPlanetMethod.Invoke(null, new object[] { planet });
        }

        private static T GetMemberValue<T>(object instance, MemberInfo member)
        {
            if (instance == null || member == null) return default(T);

            object value = null;
            if (member is PropertyInfo property)
            {
                value = property.GetValue(instance, null);
            }
            else if (member is FieldInfo field)
            {
                value = field.GetValue(instance);
            }

            if (value == null) return default(T);
            if (value is T typedValue) return typedValue;

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }

        private static T GetStaticMemberValue<T>(MemberInfo member)
        {
            if (member == null) return default(T);

            object value = null;
            if (member is PropertyInfo property)
            {
                value = property.GetValue(null, null);
            }
            else if (member is FieldInfo field)
            {
                value = field.GetValue(null);
            }

            if (value == null) return default(T);
            if (value is T typedValue) return typedValue;

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }
    }
}
