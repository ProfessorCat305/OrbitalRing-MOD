using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GalacticScale;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Compatibility
{
    internal static class GalacticScaleVeins
    {
        private const string ResourcesInitializedKey = "OrbitalRingResourcesInitialized";
        private static Action<int, EVeinType, Vector3, PlanetData> initializeGroup;
        private static Action<int, EVeinType, Vector3, short, PlanetData> addVein;
        private delegate void InitializeResources(GSPlanet planet, ThemeProto theme, out int[] spots,
            out float[] counts, out float[] richness);
        private static InitializeResources initializeResources;
        private const int PlanetPlacementBudget = 65536;
        private const int GroupPlacementAttempts = 64;
        private const int NodePlacementAttempts = 4096;
        private const float VeinNodeSpacing = 2.1f;
        private const float ExtractorSpacing = 6f;

        internal static void BindGenerationMethods(Type algorithms)
        {
            initializeGroup = Bind<Action<int, EVeinType, Vector3, PlanetData>>(algorithms, "InitializeVeinGroup",
                typeof(int), typeof(EVeinType), typeof(Vector3), typeof(PlanetData));
            addVein = Bind<Action<int, EVeinType, Vector3, short, PlanetData>>(algorithms, "AddVeinToPlanet",
                typeof(int), typeof(EVeinType), typeof(Vector3), typeof(short), typeof(PlanetData));
            initializeResources = Bind<InitializeResources>(algorithms, "InitializeFromThemeProto",
                typeof(GSPlanet), typeof(ThemeProto), typeof(int[]).MakeByRefType(),
                typeof(float[]).MakeByRefType(), typeof(float[]).MakeByRefType());
        }

        private static T Bind<T>(Type algorithms, string name, params Type[] parameters) where T : class
        {
            var method = algorithms?.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, parameters, null);
            if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
            {
                throw new MissingMethodException("Unsupported GS resource generation method: " + name);
            }

            return (T)(object)Delegate.CreateDelegate(typeof(T), method);
        }

        private static string GetPlacementAlgorithm(GSPlanet body, GSVeinSettings settings)
        {
            return settings.Algorithm == "GS2W" || body.GsTheme.VeinSettings.Algorithm == "GS2W" ? "GS2W" : "GS2";
        }

        internal static void InitializePlanetResources(GSPlanet body, bool birthStar)
        {
            if ((bool)body.genData.Get(ResourcesInitializedKey, false))
            {
                return;
            }

            GSVeinSettings settings = (body.veinSettings ?? body.GsTheme.VeinSettings).Clone();
            settings.Algorithm = GetPlacementAlgorithm(body, settings);
            bool allowRares = AllowsRareResources(birthStar);
            if (!allowRares)
            {
                settings.VeinTypes.RemoveAll(vein => (int)vein.type >= 9 && (int)vein.type <= 14);
            }

            ThemeProto theme = GetResourceTheme(body);
            if (theme != null && theme.PlanetType != EPlanetType.Gas) {
                // GS only reads the vein arrays; rare chances are rewritten below.
                var resourceTheme = new ThemeProto {
                    VeinSpot = theme.VeinSpot,
                    VeinCount = theme.VeinCount,
                    VeinOpacity = theme.VeinOpacity,
                    RareVeins = theme.RareVeins,
                    RareSettings = (float[])theme.RareSettings.Clone(),
                };
                // GS identifies its birth star independently of the native star index.
                for (int i = 0; i < resourceTheme.RareVeins.Length; i++) {
                    float chance = resourceTheme.RareSettings[i * 4 + (birthStar ? 0 : 1)];
                    resourceTheme.RareSettings[i * 4] = resourceTheme.RareSettings[i * 4 + 1] = chance;
                }
                initializeResources(body, resourceTheme, out var spots, out var counts, out var richness);
                bool[] birthOverrides = ApplyBirthResources(body, birthStar, spots, counts, richness);
                for (int type = 1; type < spots.Length; type++) {
                    if (spots[type] <= 0)
                    {
                        continue;
                    }
                    if (!allowRares && type >= 9 && type <= 14 && !birthOverrides[type])
                    {
                        continue;
                    }
                    if (birthOverrides[type])
                    {
                        settings.VeinTypes.RemoveAll(vein => (int)vein.type == type);
                    }

                    int rareIndex = Array.IndexOf(resourceTheme.RareVeins, type);
                    bool required = birthOverrides[type] || (type >= 15 && rareIndex >= 0 &&
                        resourceTheme.RareSettings[rareIndex * 4 + (birthStar ? 0 : 1)] >= 1f);
                    if (required) {
                        settings.VeinTypes.RemoveAll(vein => (int)vein.type == type && vein.veins.Count == 0);
                        foreach (var existing in settings.VeinTypes.Where(vein => (int)vein.type == type))
                        {
                            existing.rare = false;
                        }
                    }
                    if (settings.VeinTypes.Any(vein => (int)vein.type == type))
                    {
                        continue;
                    }
                    bool rare = !required && type >= 9 && type <= 14 && resourceTheme.RareVeins.Contains(type) &&
                        (type > resourceTheme.VeinSpot.Length || resourceTheme.VeinSpot[type - 1] == 0);
                    AddMissingResourceType(settings, (EVeinType)type, spots[type], GetNodeCount(counts[type]), richness[type], rare);
                }
            }
            if (allowRares)
            {
                AddStarResources(settings, body.planetData.star.type);
            }
            body.veinSettings = settings;
            body.genData.Set(ResourcesInitializedKey, true);
        }

        private static ThemeProto GetResourceTheme(GSPlanet body)
        {
            int themeId = PlanetThemeUtils.ResolveGSThemeId(body.Theme, GSSettings.ThemeLibrary);
            return themeId == 0 ? null : LDB.themes.Select(themeId);
        }

        private static bool AllowsRareResources(bool birthStar)
        {
            return !birthStar || GalacticScaleGalaxy.GetPreferences(GS2.ActiveGenerator).GetBool("noRaresStartingSystem", false);
        }

        private static bool[] ApplyBirthResources(GSPlanet body, bool birthStar, int[] spots, float[] counts, float[] richness)
        {
            var birthOverrides = new bool[spots.Length];
            int role = birthStar ? GalacticScaleBirthSystem.GetBirthRole(body) : -1;
            if (role < 0)
            {
                return birthOverrides;
            }

            var originalSpots = (int[])spots.Clone();
            var originalCounts = (float[])counts.Clone();
            var originalRichness = (float[])richness.Clone();
            AddVeinPatches.ApplyBirthGalaxyVeins(body.Radius, role, true, spots, counts, richness);
            for (int i = 1; i < spots.Length; i++)
            {
                birthOverrides[i] = spots[i] != originalSpots[i] || counts[i] != originalCounts[i] || richness[i] != originalRichness[i];
            }

            return birthOverrides;
        }

        private static GSVeinSettings GetRequiredResources(GSPlanet body, EStarType starType, bool birthStar)
        {
            var settings = new GSVeinSettings { VeinTypes = new GSVeinTypes() };
            ThemeProto theme = GetResourceTheme(body);
            if (theme != null && theme.PlanetType != EPlanetType.Gas)
            {
                AddThemeResources(settings, theme, body, birthStar);
            }
            AddStarResources(settings, starType);
            return settings;
        }

        private static void AddThemeResources(GSVeinSettings settings, ThemeProto theme, GSPlanet body, bool birthStar)
        {
            int resourceArrayLength = AddVeinPatches.VeinTypeCount + 1;
            var spots = new int[resourceArrayLength];
            var counts = new float[resourceArrayLength];
            var richness = new float[resourceArrayLength];
            Array.Copy(theme.VeinSpot, 0, spots, 1, Math.Min(theme.VeinSpot.Length, resourceArrayLength - 1));
            Array.Copy(theme.VeinCount, 0, counts, 1, Math.Min(theme.VeinCount.Length, resourceArrayLength - 1));
            Array.Copy(theme.VeinOpacity, 0, richness, 1, Math.Min(theme.VeinOpacity.Length, resourceArrayLength - 1));
            ApplyBirthResources(body, birthStar, spots, counts, richness);

            settings.Algorithm = "GS2";
            for (int i = 0; i < theme.RareVeins.Length; i++)
            {
                int type = theme.RareVeins[i];
                if (spots[type] > 0)
                {
                    continue;
                }

                float opacity = theme.RareSettings[i * 4 + 3];
                bool guaranteed = theme.RareSettings[i * 4 + (birthStar ? 0 : 1)] >= 1f;
                if (guaranteed)
                {
                    AddMissingResourceType(settings, (EVeinType)type, 1, GetNodeCount(opacity), opacity);
                }
            }
            for (int i = 1; i < resourceArrayLength; i++)
            {
                if (spots[i] == 0)
                {
                    continue;
                }
                AddMissingResourceType(settings, (EVeinType)i, spots[i], GetNodeCount(counts[i]), richness[i]);
            }
        }

        private static int GetNodeCount(float count)
        {
            return Math.Max(1, (int)(count * 25f));
        }

        private static void AddStarResources(GSVeinSettings settings, EStarType starType)
        {
            if (starType == EStarType.BlackHole || starType == EStarType.NeutronStar)
            {
                EnsureStarResource(settings, (EVeinType)14, 0.3f);
            }
            else if (starType == EStarType.WhiteDwarf)
            {
                EnsureStarResource(settings, (EVeinType)9, 1f);
                EnsureStarResource(settings, (EVeinType)10, 1f);
                EnsureStarResource(settings, (EVeinType)12, 0.3f);
            }
        }

        private static void EnsureStarResource(GSVeinSettings settings, EVeinType type, float richness)
        {
            if (!settings.VeinTypes.Any(vein => vein.type == type && vein.veins.Count > 0))
            {
                settings.VeinTypes.Add(GSVeinType.Generate(type, 1, 1, richness, richness, 18, 18, false));
            }
        }

        private static void AddMissingResourceType(GSVeinSettings settings, EVeinType type, int spots, int count, float richness, bool rare = false)
        {
            bool singlePoint = IsSinglePoint(type);
            if (settings.VeinTypes.Any(vein => vein.type == type))
            {
                return;
            }

            if (spots > 0)
            {
                settings.VeinTypes.Add(GSVeinType.Generate(type, spots, spots, richness, richness,
                        singlePoint ? 1 : count, singlePoint ? 1 : count, rare));
            }
        }

        internal static bool IsSinglePoint(EVeinType type)
        {
            return type == EVeinType.Oil || type == EVeinType.DeepMagma || type == EVeinType.Ice;
        }

        public static void EnsureGeneratedResources(GSPlanet gsPlanet)
        {
            if (gsPlanet?.planetData?.data == null || gsPlanet.Scale == 10f || gsPlanet.veinSettings == null)
            {
                return;
            }

            PlanetData planet = gsPlanet.planetData;
            var generatedTypes = new HashSet<EVeinType>();
            var occupied = new List<VeinData>();
            for (int i = 1; i < planet.data.veinCursor; i++) {
                var vein = planet.data.veinPool[i];
                if (vein.id != i)
                {
                    continue;
                }

                occupied.Add(vein);
                generatedTypes.Add(vein.type);
            }

            bool birthStar = GS2.GetGSStar(planet.star).Bodies.Contains(GSSettings.BirthPlanet);
            bool allowRares = AllowsRareResources(birthStar);
            int resourceArrayLength = AddVeinPatches.VeinTypeCount + 1;
            var birthSpots = new int[resourceArrayLength];
            ApplyBirthResources(gsPlanet, birthStar, birthSpots, new float[resourceArrayLength], new float[resourceArrayLength]);
            GSVeinSettings requiredResources = GetRequiredResources(gsPlanet, planet.star.type, birthStar);
            var random = new System.Random(gsPlanet.Seed ^ 0x4F524753);
            int placementBudget = PlanetPlacementBudget;
            foreach (var requiredType in requiredResources.VeinTypes.Where(vein => vein.type != EVeinType.None)
                .GroupBy(vein => vein.type)) {
                EVeinType type = requiredType.Key;
                if (!allowRares && (int)type >= 9 && (int)type <= 14 && birthSpots[(int)type] == 0)
                {
                    continue;
                }
                if (generatedTypes.Contains(type))
                {
                    continue;
                }

                GSVein requirement = gsPlanet.veinSettings.VeinTypes.Where(vein => vein.type == type && !vein.rare)
                    .SelectMany(vein => vein.veins).OrderByDescending(vein => vein.count).FirstOrDefault();
                bool starResource = ((planet.star.type == EStarType.BlackHole || planet.star.type == EStarType.NeutronStar) && (int)type == 14) ||
                    (planet.star.type == EStarType.WhiteDwarf && ((int)type == 9 || (int)type == 10 || (int)type == 12));
                if (requirement == null && starResource)
                {
                    requirement = requiredType.SelectMany(vein => vein.veins).OrderByDescending(vein => vein.count).FirstOrDefault();
                }
                if (requirement == null)
                {
                    continue;
                }

                int count = IsSinglePoint(type) ? 1 : Math.Max(1, requirement.count);
                int amount = GetRequiredAmount(planet, type, requirement.richness);
                Vector3 center = Vector3.zero;
                List<Vector3> planned = null;
                // GS2 supplemental nodes use land; extractor spacing is enforced separately.
                float minimumHeight = gsPlanet.veinSettings.Algorithm == "GS2W" ? 0f : planet.radius;
                for (int attempt = 0; attempt < GroupPlacementAttempts; attempt++) {
                    if (!TryFindResourcePoint(planet, type, Vector3.zero, count, minimumHeight, occupied, null, random, ref placementBudget, out var point))
                    {
                        break;
                    }

                    center = point.normalized;
                    if (TryPlanResourceNodes(planet, type, center, count, minimumHeight, occupied, random, ref placementBudget, out planned))
                    {
                        break;
                    }

                    planned = null;
                }

                if (planned == null) {
                    ProjectOrbitalRing.LogWarning("Cannot place missing " + type + " resources on " + planet.name
                        + " (theme " + gsPlanet.Theme + ", radius " + planet.radius
                        + ", maximum terrain height " + planet.data.heightData.Max() * 0.01f
                        + ", remaining attempts " + placementBudget
                        + "). Terrain placement attempt limit reached.");
                    continue;
                }

                int groupIndex = planet.veinGroups.Length;
                if (groupIndex > short.MaxValue)
                {
                    throw new InvalidOperationException("GS resource group capacity exceeded.");
                }

                Array.Resize(ref planet.veinGroups, groupIndex + 1);
                // GS helper indices are zero-based; the raw planet pool reserves group zero.
                initializeGroup(groupIndex - 1, type, center, planet);
                foreach (Vector3 point in planned) {
                    planet.data.EraseVegetableAtPoint(point);
                    int node = planet.data.veinCursor;
                    addVein(amount, type, point, (short)(groupIndex - 1), planet);
                    occupied.Add(planet.data.veinPool[node]);
                }
                if (planet.veinGroups[groupIndex].count < count)
                {
                    throw new InvalidOperationException("GS resource group does not satisfy its generation plan.");
                }
            }
        }

        private static bool TryPlanResourceNodes(PlanetData planet, EVeinType type, Vector3 center, int count,
            float minimumHeight, List<VeinData> occupied, System.Random random, ref int placementBudget, out List<Vector3> planned)
        {
            planned = new List<Vector3>();
            for (int i = 0; i < count; i++) {
                if (!TryFindResourcePoint(planet, type, center, count, minimumHeight, occupied, planned, random, ref placementBudget, out var point))
                {
                    return false;
                }

                planned.Add(point);
            }

            return true;
        }

        private static int GetRequiredAmount(PlanetData planet, EVeinType type, float richness)
        {
            bool singlePoint = IsSinglePoint(type);
            if (!singlePoint && DSPGame.GameDesc.resourceMultiplier >= 99.5f)
            {
                return 1000000000;
            }

            int amount = Mathf.RoundToInt(Math.Max(0f, richness) * 100000f * planet.star.resourceCoef);
            if (singlePoint)
            {
                amount *= 2;
            }
            amount = Math.Max(20, amount);
            if (!singlePoint)
            {
                amount = Mathf.RoundToInt(amount * DSPGame.GameDesc.resourceMultiplier);
            }

            return Math.Max(1, amount);
        }

        private static bool TryFindResourcePoint(PlanetData planet, EVeinType type, Vector3 center, int count,
            float minimumHeight, List<VeinData> occupied, List<Vector3> planned, System.Random random,
            ref int placementBudget, out Vector3 result)
        {
            bool singlePoint = IsSinglePoint(type);
            float spacing = singlePoint ? ExtractorSpacing : VeinNodeSpacing;
            Vector3 axis = Math.Abs(center.y) < 0.9f ? Vector3.up : Vector3.right;
            Vector3 tangent = Vector3.Cross(center, axis).normalized;
            Vector3 bitangent = Vector3.Cross(center, tangent).normalized;
            float extent = spacing * (Mathf.Sqrt(count) + 1f) / planet.radius;
            for (int attempt = 0; attempt < NodePlacementAttempts && placementBudget > 0; attempt++) {
                placementBudget--;
                Vector3 direction;
                if (center == Vector3.zero) {
                    float y = (float)(random.NextDouble() * 2d - 1d);
                    double longitude = random.NextDouble() * Math.PI * 2d;
                    float projection = Mathf.Sqrt(1f - y * y);
                    direction = new Vector3(projection * (float)Math.Cos(longitude), y, projection * (float)Math.Sin(longitude));
                } else if (attempt == 0) {
                    direction = center;
                } else {
                    double angle = random.NextDouble() * Math.PI * 2d;
                    float offset = (float)Math.Sqrt(random.NextDouble()) * extent;
                    direction = (center + offset * ((float)Math.Cos(angle) * tangent + (float)Math.Sin(angle) * bitangent)).normalized;
                }
                float height = planet.data.QueryHeight(direction);
                if (float.IsNaN(height) || float.IsInfinity(height) || height < minimumHeight)
                {
                    continue;
                }

                Vector3 point = direction * height;
                bool collision = false;
                foreach (var existing in occupied) {
                    float separation = singlePoint || IsSinglePoint(existing.type) ? ExtractorSpacing : spacing;
                    if ((point - existing.pos).sqrMagnitude < separation * separation) {
                        collision = true;
                        break;
                    }
                }
                if (!collision && planned != null) {
                    foreach (Vector3 existing in planned) {
                        if ((point - existing).sqrMagnitude < spacing * spacing) {
                            collision = true;
                            break;
                        }
                    }
                }
                if (!collision) {
                    result = point;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }
    }
}
