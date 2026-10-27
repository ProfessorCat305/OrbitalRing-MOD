using System;
using System.Collections;
using System.Linq;
using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Compatibility
{
    internal static class GalacticScaleBirthSystem
    {
        private static readonly int[] ThemeIds = { 9, 1, 11, 2, 10 };
        private static readonly string[] ThemeNames = { "Lava", "Mediterranean", "Barren", "GasGiant", "IceGelisol" };

        internal static void PatchGenerator(Harmony harmony, Type generatorType)
        {
            if (generatorType == null)
            {
                return;
            }

            harmony.Patch(AccessTools.Method(generatorType, "AssignMoonOrbits"),
                prefix: new HarmonyMethod(typeof(GalacticScaleBirthSystem), nameof(AssignMoonOrbits_Prefix)));
            harmony.Patch(AccessTools.Method(generatorType, "AssignPlanetOrbits"),
                postfix: new HarmonyMethod(typeof(GalacticScaleBirthSystem), nameof(AssignPlanetOrbits_Postfix)));
            GalacticScaleGalaxy.PatchGenerator(harmony, generatorType);
        }

        public static void AssignMoonOrbits_Prefix(object __instance, GSStar star,
            GSStar ___birthStar, GSPlanet ___birthPlanet)
        {
            if (star != ___birthStar || ___birthPlanet == null ||
                ___birthPlanet.Moons.Any(body => body.Scale != 10f))
            {
                return;
            }

            var random = new GS2.Random(star.Seed);
            var sizeMethod = AccessTools.Method(__instance.GetType(), "GetStarMoonSize",
                new[] { typeof(GSStar), typeof(int), typeof(bool), typeof(GS2.Random) });
            object[] arguments = { star, ___birthPlanet.Radius, false, random };
            if (sizeMethod == null) {
                sizeMethod = AccessTools.Method(__instance.GetType(), "GetStarMoonSize",
                    new[] { typeof(GSStar), typeof(int), typeof(bool) });
                arguments = new object[] { star, ___birthPlanet.Radius, false };
            }
            if (sizeMethod == null)
            {
                throw new MissingMethodException("Unsupported GS moon size method.");
            }

            int radius = (int)sizeMethod.Invoke(__instance, arguments);
            string name = ___birthPlanet.Name + " - Moon " + ___birthPlanet.MoonCount;
            var moon = new GSPlanet(name, "Barren", radius, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f);
            moon.Scale = 1f;
            moon.genData.Set("hosttype", "planet");
            moon.genData.Set("hostname", ___birthPlanet.Name);
            // GS assigns moon orbits before reserving the host system's stellar orbit.
            ___birthPlanet.Moons.Add(moon);
        }

        public static void AssignPlanetOrbits_Postfix(object __instance, GSStar star,
            GSStar ___birthStar, GSPlanet ___birthPlanet, IDictionary ___starOrbits)
        {
            bool needsPlanet = GalacticScaleGalaxy.IsCompactStar(star) && !star.Bodies.Any(body => body.Scale != 10f);
            bool isBirthStar = star == ___birthStar && ___birthPlanet != null;
            int missingSolidBodyCount = isBirthStar ? Math.Max(0, 3 - star.Bodies.Count(body => body != ___birthPlanet && body.Scale != 10f)) :
                needsPlanet ? 1 : 0;
            bool needsGas = isBirthStar && !star.Bodies.Any(body => body.Scale == 10f);
            GSPlanet birthHost = isBirthStar ? star.Planets.First(host => host.Bodies.Contains(___birthPlanet)) : null;

            bool needsIce = isBirthStar && !star.Planets.Any(body => body.Scale != 10f && body.OrbitRadius > birthHost.OrbitRadius);
            bool needsLava = isBirthStar && !star.Planets.Any(body => body.Scale != 10f && body.OrbitRadius < birthHost.OrbitRadius);
            if (missingSolidBodyCount == 0 && !needsGas && !needsIce && !needsLava)
            {
                return;
            }

            var sizeRandom = new GS2.Random(star.Seed);
            var orbits = (IList)___starOrbits[star];
            float spacing = GalacticScaleGalaxy.GetPreferences(__instance).GetFloat("orbitSpacing", 0.05f);
            for (int i = 0; i < missingSolidBodyCount; i++)
            {
                AddBody(star, GetBodySize(__instance, star, false, sizeRandom), false, orbits, spacing);
            }

            if (needsIce && missingSolidBodyCount == 0)
            {
                AddBody(star, GetBodySize(__instance, star, false, sizeRandom), false, orbits, spacing);
            }

            if (needsGas)
            {
                AddBody(star, GetBodySize(__instance, star, true, sizeRandom), true, orbits, spacing);
            }

            if (needsLava)
            {
                AddBody(star, GetBodySize(__instance, star, false, sizeRandom), false, orbits, spacing, true);
            }

            star.Planets.Sort((left, right) => left.OrbitRadius.CompareTo(right.OrbitRadius));
        }

        private static int GetBodySize(object generator, GSStar star, bool gas, GS2.Random random)
        {
            string name = gas ? "GetStarGasSize" : "GetStarPlanetSize";
            var sizeMethod = AccessTools.Method(generator.GetType(), name, new[] { typeof(GSStar), typeof(GS2.Random) });
            object[] arguments = { star, random };
            if (sizeMethod == null) {
                sizeMethod = AccessTools.Method(generator.GetType(), name, new[] { typeof(GSStar) });
                arguments = new object[] { star };
            }
            if (sizeMethod == null)
            {
                throw new MissingMethodException("Unsupported GS planet size method: " + name);
            }

            int size = (int)sizeMethod.Invoke(generator, arguments);
            if (!gas)
            {
                return size;
            }

            return GalacticScaleGalaxy.GetPreferences(generator).GetBool("hugeGasGiants", true) ?
                Mathf.RoundToInt(size / 10f) : 80;
        }

        private static void AddBody(GSStar star, int radius, bool gas, IList orbits, float spacing, bool inner = false)
        {
            int nameIndex = star.Planets.Count;
            while (star.Planets.Any(planet => planet.Name == "planet_" + nameIndex))
            {
                nameIndex++;
            }

            var body = new GSPlanet("planet_" + nameIndex, gas ? "GasGiant" : "Barren", radius,
                -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f);
            body.Scale = gas ? 10f : 1f;
            body.genData.Set("hosttype", "star");
            body.genData.Set("hostname", star.Name);

            // Separate stellar orbits reserve clearance for each body's satellite system.
            float outerEdge = Math.Max(star.RadiusAU * 1.1f, (float)star.genData.Get("minOrbit", star.RadiusAU));
            foreach (GSPlanet host in star.Planets)
            {
                outerEdge = Math.Max(outerEdge, host.OrbitRadius + host.SystemRadius * 2f);
            }

            body.OrbitRadius = outerEdge + spacing + body.SystemRadius * 2f;
            Type orbitType = orbits.GetType().GetGenericArguments()[0];
            if (inner) {
                body.OrbitRadius = Math.Max(star.RadiusAU * 1.1f, (float)star.genData.Get("minOrbit", star.RadiusAU))
                    + spacing + body.SystemRadius * 2f;
                float innerEdge = star.Planets.Min(host => host.OrbitRadius - host.SystemRadius * 2f);
                float shift = Math.Max(0f, body.OrbitRadius + body.SystemRadius * 2f + spacing - innerEdge);
                if (shift > 0f) {
                    // Moving complete stellar systems preserves satellite orbits and their mutual clearance.
                    foreach (GSPlanet host in star.Planets) {
                        host.OrbitRadius += shift;
                        host.OrbitalPeriod = global::GalacticScale.Utils.CalculateOrbitPeriod(host.OrbitRadius);
                    }
                    var orbitRadius = AccessTools.Field(orbitType, "radius");
                    foreach (object existing in orbits)
                    {
                        orbitRadius.SetValue(existing, (float)orbitRadius.GetValue(existing) + shift);
                    }
                }
            }
            body.OrbitalPeriod = global::GalacticScale.Utils.CalculateOrbitPeriod(body.OrbitRadius);
            object orbit = Activator.CreateInstance(orbitType, body.OrbitRadius);
            ((GSPlanets)AccessTools.Field(orbitType, "planets").GetValue(orbit)).Add(body);
            orbits.Add(orbit);
            star.Planets.Add(body);
        }

        internal static bool ApplyThemes(GSStar star, GSPlanet birthPlanet)
        {
            if (birthPlanet == null || !star.Bodies.Contains(birthPlanet))
            {
                return false;
            }

            GSPlanet[] roles = SelectBodies(star, birthPlanet);
            if (roles == null)
            {
                throw new InvalidOperationException("GS birth system cannot accommodate OrbitalRing resource roles.");
            }

            for (int role = 0; role < roles.Length; role++)
            {
                AssignRole(roles[role], role);
            }

            return true;
        }

        internal static int GetBirthRole(GSPlanet body)
        {
            GSStar star = GS2.GetGSStar(body.planetData.star);
            GSPlanet birthPlanet = GSSettings.BirthPlanet;
            if (birthPlanet == null || !star.Bodies.Contains(birthPlanet))
            {
                return -1;
            }

            var roles = SelectBodies(star, birthPlanet);
            return roles == null ? -1 : Array.IndexOf(roles, body);
        }

        private static GSPlanet[] SelectBodies(GSStar star, GSPlanet birthPlanet)
        {
            GSPlanet barren = birthPlanet.Moons.FirstOrDefault(body => body.Scale != 10f && ResolveTheme(body) == 11) ??
                birthPlanet.Moons.FirstOrDefault(body => body.Scale != 10f);
            GSPlanet birthHost = star.Planets.First(host => host.Bodies.Contains(birthPlanet));
            var outerPlanets = star.Planets.Where(body => body.Scale != 10f && body.OrbitRadius > birthHost.OrbitRadius)
                .OrderBy(body => body.OrbitRadius).ToList();
            GSPlanet ice = outerPlanets.LastOrDefault(body => MatchesRole(body, 4)) ?? outerPlanets.LastOrDefault();
            var candidates = star.Planets.Where(body => body.Scale != 10f && body.OrbitRadius < birthHost.OrbitRadius)
                .OrderBy(body => body.OrbitRadius).ToList();
            if (barren == null || ice == null || candidates.Count < 1 || !star.Bodies.Any(body => body.Scale == 10f))
            {
                return null;
            }

            GSPlanet lava = candidates.FirstOrDefault(body => MatchesRole(body, 0)) ?? candidates[0];
            GSPlanet gas = star.Bodies.First(body => body.Scale == 10f);
            return new[] { lava, birthPlanet, barren, gas, ice };
        }

        private static int ResolveTheme(GSPlanet body)
        {
            return PlanetThemeUtils.ResolveGSThemeId(body.Theme, GSSettings.ThemeLibrary);
        }

        private static void AssignRole(GSPlanet body, int role)
        {
            if (!MatchesRole(body, role))
            {
                string name = ThemeNames[role];
                string small = name + "smol";
                if (body.Radius < 50 && GSSettings.ThemeLibrary.ContainsKey(small))
                {
                    name = small;
                }

                if (!GSSettings.ThemeLibrary.ContainsKey(name))
                {
                    throw new InvalidOperationException("GS theme library is missing " + name);
                }

                body.Theme = name;
                body.veinSettings = null;
            }
        }

        private static bool MatchesRole(GSPlanet body, int role)
        {
            if (role == 4)
            {
                return body.Theme == ThemeNames[4] || body.Theme == ThemeNames[4] + "smol";
            }

            int themeId = ResolveTheme(body);
            bool matches = role == 3 ? themeId == 2 || themeId == 3 || themeId == 21 : themeId == ThemeIds[role];
            return matches && (role != 0 || body.GsTheme.WaterItemId == ProtoID.I岩浆);
        }
    }
}
