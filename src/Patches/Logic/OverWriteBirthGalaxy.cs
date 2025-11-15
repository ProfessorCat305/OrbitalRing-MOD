using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;
using GalacticScale;

namespace ProjectOrbitalRing.Patches.Logic
{
    [HarmonyPatch]
    public static class OverWriteBirthGalaxy
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StarGen), "CreateStarPlanets")]
        public static bool CreateStarPlanetsPatch(ref double[] ___pGas, GalaxyData galaxy, ref StarData star, GameDesc gameDesc)
        {
            ModCreateStarPlanets(ref ___pGas, ref galaxy, ref star, ref gameDesc);
            return false;
        }

        public static void ModCreateStarPlanets(ref double[] ___pGas, ref GalaxyData galaxy, ref StarData star, ref GameDesc gameDesc)
        {
            DotNet35Random dotNet35Random = new DotNet35Random(star.seed);
            dotNet35Random.Next();
            dotNet35Random.Next();
            dotNet35Random.Next();
            DotNet35Random dotNet35Random2 = new DotNet35Random(dotNet35Random.Next());
            double num = dotNet35Random2.NextDouble();
            double num2 = dotNet35Random2.NextDouble();
            double num3 = dotNet35Random2.NextDouble();
            double num4 = dotNet35Random2.NextDouble();
            double num5 = dotNet35Random2.NextDouble();
            double num6 = dotNet35Random2.NextDouble() * 0.2 + 0.9;
            double num7 = dotNet35Random2.NextDouble() * 0.2 + 0.9;
            DotNet35Random dotNet35Random3 = new DotNet35Random(dotNet35Random.Next());
            StarGen.SetHiveOrbitsConditionsTrue();
            if (star.type == EStarType.BlackHole) {
                star.planetCount = 2;
                star.planets = new PlanetData[star.planetCount];
                int info_seed = dotNet35Random2.Next();
                int gen_seed = dotNet35Random2.Next();
                star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 3, 1, gasGiant: false, info_seed, gen_seed);
                star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 1, 1, 0, gasGiant: false, info_seed, gen_seed);
            } else if (star.type == EStarType.NeutronStar) {
                star.planetCount = 1;
                star.planets = new PlanetData[star.planetCount];
                int info_seed2 = dotNet35Random2.Next();
                int gen_seed2 = dotNet35Random2.Next();
                star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 3, 1, gasGiant: false, info_seed2, gen_seed2);
            } else if (star.type == EStarType.WhiteDwarf) {
                if (num < 0.699999988079071) {
                    star.planetCount = 1;
                    star.planets = new PlanetData[star.planetCount];
                    int info_seed3 = dotNet35Random2.Next();
                    int gen_seed3 = dotNet35Random2.Next();
                    star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 3, 1, gasGiant: false, info_seed3, gen_seed3);
                } else {
                    star.planetCount = 2;
                    star.planets = new PlanetData[star.planetCount];
                    int num8 = 0;
                    int num9 = 0;
                    if (num2 < 0.30000001192092896) {
                        num8 = dotNet35Random2.Next();
                        num9 = dotNet35Random2.Next();
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 3, 1, gasGiant: false, num8, num9);
                        num8 = dotNet35Random2.Next();
                        num9 = dotNet35Random2.Next();
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 0, 4, 2, gasGiant: false, num8, num9);
                    } else {
                        num8 = dotNet35Random2.Next();
                        num9 = dotNet35Random2.Next();
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 4, 1, gasGiant: true, num8, num9);
                        num8 = dotNet35Random2.Next();
                        num9 = dotNet35Random2.Next();
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 1, 1, 1, gasGiant: false, num8, num9);
                    }
                }
            } else if (star.type == EStarType.GiantStar) {
                if (num < 0.30000001192092896) {
                    star.planetCount = 1;
                    star.planets = new PlanetData[star.planetCount];
                    int info_seed4 = dotNet35Random2.Next();
                    int gen_seed4 = dotNet35Random2.Next();
                    int orbitIndex = ((num3 > 0.5) ? 3 : 2);
                    star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, orbitIndex, 1, gasGiant: false, info_seed4, gen_seed4);
                } else if (num < 0.800000011920929) {
                    star.planetCount = 2;
                    star.planets = new PlanetData[star.planetCount];
                    int num10 = 0;
                    int num11 = 0;
                    if (num2 < 0.25) {
                        num10 = dotNet35Random2.Next();
                        num11 = dotNet35Random2.Next();
                        int orbitIndex2 = ((num3 > 0.5) ? 3 : 2);
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, orbitIndex2, 1, gasGiant: false, num10, num11);
                        num10 = dotNet35Random2.Next();
                        num11 = dotNet35Random2.Next();
                        orbitIndex2 = ((num3 > 0.5) ? 4 : 3);
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 0, orbitIndex2, 2, gasGiant: false, num10, num11);
                    } else {
                        num10 = dotNet35Random2.Next();
                        num11 = dotNet35Random2.Next();
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 3, 1, gasGiant: true, num10, num11);
                        num10 = dotNet35Random2.Next();
                        num11 = dotNet35Random2.Next();
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 1, 1, 1, gasGiant: false, num10, num11);
                    }
                } else {
                    star.planetCount = 3;
                    star.planets = new PlanetData[star.planetCount];
                    int num12 = 0;
                    int num13 = 0;
                    if (num2 < 0.15000000596046448) {
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        int orbitIndex3 = ((num3 > 0.5) ? 3 : 2);
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, orbitIndex3, 1, gasGiant: false, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        orbitIndex3 = ((num3 > 0.5) ? 4 : 3);
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 0, orbitIndex3, 2, gasGiant: false, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        orbitIndex3 = ((num3 > 0.5) ? 5 : 4);
                        star.planets[2] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 2, 2, 1, 1, gasGiant: false, num12, num13);
                    } else if (num2 < 0.75) {
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        int orbitIndex4 = ((num3 > 0.5) ? 3 : 2);
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, orbitIndex4, 1, gasGiant: false, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 0, 4, 2, gasGiant: true, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        star.planets[2] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 2, 2, 1, 1, gasGiant: false, num12, num13);
                    } else {
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        int orbitIndex5 = ((num3 > 0.5) ? 4 : 3);
                        star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, orbitIndex5, 1, gasGiant: true, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 1, 1, 1, gasGiant: false, num12, num13);
                        num12 = dotNet35Random2.Next();
                        num13 = dotNet35Random2.Next();
                        star.planets[2] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 2, 1, 2, 2, gasGiant: false, num12, num13);
                    }
                }
            } else if (star.index == 0) {
                star.planetCount = 5;
                star.planets = new PlanetData[star.planetCount];

                int info_seed5 = dotNet35Random2.Next();
                int gen_seed5 = dotNet35Random2.Next();
                int info_seed6 = dotNet35Random2.Next();
                int gen_seed6 = dotNet35Random2.Next();
                int info_seed7 = dotNet35Random2.Next();
                int gen_seed7 = dotNet35Random2.Next();
                info_seed6 = dotNet35Random2.Next();
                gen_seed6 = dotNet35Random2.Next();
                star.planets[0] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 0, 0, 1, 0, false, info_seed6, gen_seed6);
                info_seed6 = dotNet35Random2.Next();
                gen_seed6 = dotNet35Random2.Next();
                star.planets[1] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 1, 0, 2, 1, false, info_seed6, gen_seed6);
                info_seed6 = dotNet35Random2.Next();
                gen_seed6 = dotNet35Random2.Next();
                star.planets[2] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 2, 1, 1, 0, false, info_seed6, gen_seed6);
                info_seed6 = dotNet35Random2.Next();
                gen_seed6 = dotNet35Random2.Next();
                star.planets[3] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 3, 0, 3, 4, true, info_seed6, gen_seed6);
                info_seed6 = dotNet35Random2.Next();
                gen_seed6 = dotNet35Random2.Next();
                star.planets[4] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, 4, 0, 4, 0, false, info_seed6, gen_seed6);
            } else {
                Array.Clear(___pGas, 0, ___pGas.Length);
                if (star.spectr == ESpectrType.M) {
                    if (num < 0.1) {
                        star.planetCount = 1;
                    } else if (num < 0.3) {
                        star.planetCount = 2;
                    } else if (num < 0.8) {
                        star.planetCount = 3;
                    } else {
                        star.planetCount = 4;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.2;
                        ___pGas[1] = 0.2;
                    } else {
                        ___pGas[0] = 0.0;
                        ___pGas[1] = 0.2;
                        ___pGas[2] = 0.3;
                    }
                } else if (star.spectr == ESpectrType.K) {
                    if (num < 0.1) {
                        star.planetCount = 1;
                    } else if (num < 0.2) {
                        star.planetCount = 2;
                    } else if (num < 0.7) {
                        star.planetCount = 3;
                    } else if (num < 0.95) {
                        star.planetCount = 4;
                    } else {
                        star.planetCount = 5;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.18;
                        ___pGas[1] = 0.18;
                    } else {
                        ___pGas[0] = 0.0;
                        ___pGas[1] = 0.18;
                        ___pGas[2] = 0.28;
                        ___pGas[3] = 0.28;
                    }
                } else if (star.spectr == ESpectrType.G) {
                    if (num < 0.4) {
                        star.planetCount = 3;
                    } else if (num < 0.9) {
                        star.planetCount = 4;
                    } else {
                        star.planetCount = 5;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.18;
                        ___pGas[1] = 0.18;
                    } else {
                        ___pGas[0] = 0.0;
                        ___pGas[1] = 0.2;
                        ___pGas[2] = 0.3;
                        ___pGas[3] = 0.3;
                    }
                } else if (star.spectr == ESpectrType.F) {
                    if (num < 0.35) {
                        star.planetCount = 3;
                    } else if (num < 0.8) {
                        star.planetCount = 4;
                    } else {
                        star.planetCount = 5;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.2;
                        ___pGas[1] = 0.2;
                    } else {
                        ___pGas[0] = 0.0;
                        ___pGas[1] = 0.22;
                        ___pGas[2] = 0.31;
                        ___pGas[3] = 0.31;
                    }
                } else if (star.spectr == ESpectrType.A) {
                    if (num < 0.3) {
                        star.planetCount = 3;
                    } else if (num < 0.75) {
                        star.planetCount = 4;
                    } else {
                        star.planetCount = 5;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.2;
                        ___pGas[1] = 0.2;
                    } else {
                        ___pGas[0] = 0.1;
                        ___pGas[1] = 0.28;
                        ___pGas[2] = 0.3;
                        ___pGas[3] = 0.35;
                    }
                } else if (star.spectr == ESpectrType.B) {
                    if (num < 0.3) {
                        star.planetCount = 4;
                    } else if (num < 0.75) {
                        star.planetCount = 5;
                    } else {
                        star.planetCount = 6;
                    }

                    if (star.planetCount <= 3) {
                        ___pGas[0] = 0.2;
                        ___pGas[1] = 0.2;
                    } else {
                        ___pGas[0] = 0.1;
                        ___pGas[1] = 0.22;
                        ___pGas[2] = 0.28;
                        ___pGas[3] = 0.35;
                        ___pGas[4] = 0.35;
                    }
                } else if (star.spectr == ESpectrType.O) {
                    if (num < 0.5) {
                        star.planetCount = 5;
                    } else {
                        star.planetCount = 6;
                    }

                    ___pGas[0] = 0.1;
                    ___pGas[1] = 0.2;
                    ___pGas[2] = 0.25;
                    ___pGas[3] = 0.3;
                    ___pGas[4] = 0.32;
                    ___pGas[5] = 0.35;
                } else {
                    star.planetCount = 1;
                }

                star.planetCount++;
                //NextShouldBeMoon()

                star.planets = new PlanetData[star.planetCount];
                int num14 = 0;
                int num15 = 0;
                int num16 = 0;
                int num17 = 1;
                bool isAddMoon = false;
                int aroundPlanetIndex = 0;
                if (num < 0.5) {
                    aroundPlanetIndex++;
                }
                for (int i = 0; i < star.planetCount; i++) {
                    int info_seed5 = dotNet35Random2.Next();
                    int gen_seed5 = dotNet35Random2.Next();
                    double num18 = dotNet35Random2.NextDouble();
                    double num19 = dotNet35Random2.NextDouble();
                    bool flag = false;
                    if (num16 == 0) {
                        num14++;
                        if (i < star.planetCount - 1 && num18 < ___pGas[i]) {
                            flag = true;
                            if (num17 < 3) {
                                num17 = 3;
                            }
                        }

                        while (true) {
                            if (star.index == 0 && num17 == 3) {
                                flag = true;
                                break;
                            }

                            int num20 = star.planetCount - i;
                            int num21 = 9 - num17;
                            if (num21 <= num20) {
                                break;
                            }

                            float a = (float)num20 / (float)num21;
                            a = ((num17 <= 3) ? (Mathf.Lerp(a, 1f, 0.15f) + 0.01f) : (Mathf.Lerp(a, 1f, 0.45f) + 0.01f));
                            if (dotNet35Random2.NextDouble() < (double)a) {
                                break;
                            }

                            num17++;
                        }
                    } else {
                        num15++;
                        flag = false;
                    }

                    star.planets[i] = PlanetGen.CreatePlanet(galaxy, star, gameDesc.savedThemeIds, i, num16, (num16 == 0) ? num17 : num15, (num16 == 0) ? num14 : num15, flag, info_seed5, gen_seed5);
                    num17++;
                    if (flag) {
                        num16 = num14;
                        num15 = 0;
                    }
                    if (num16 == 0) {
                        if (isAddMoon == false) {
                            if ((star.planetCount >= 5 && i > 1 + aroundPlanetIndex) || (star.planetCount <= 4 && i > 0 + aroundPlanetIndex)) {
                                num16 = num14;
                                num15 = 0;
                                isAddMoon = true;
                            }
                        }
                    }

                    if (num15 >= 1) {
                        if (star.planets[num14].radius == 100f && num19 < 0.9) {
                            num16 = 0;
                            num15 = 0;
                        }
                        if (star.planets[num14].type == EPlanetType.Gas && num19 < 0.85) {
                            num16 = 0;
                            num15 = 0;
                        }
                        if (num15 >= 2) {
                            isAddMoon = true;
                            if (num19 < 0.95) {
                                num16 = 0;
                                num15 = 0;
                            }
                        }
                    }
                }
            }

            int num22 = 0;
            int num23 = 0;
            int num24 = 0;
            int num25 = 0;
            for (int j = 0; j < star.planetCount; j++) {
                if (star.planets[j].type == EPlanetType.Gas) {
                    num22 = star.planets[j].orbitIndex;
                    break;
                }
            }

            for (int k = 0; k < star.planetCount; k++) {
                if (star.planets[k].orbitAround == 0) {
                    num23 = star.planets[k].orbitIndex;
                }
            }

            if (num22 > 0) {
                int num26 = num22 - 1;
                bool flag2 = true;
                for (int l = 0; l < star.planetCount; l++) {
                    if (star.planets[l].orbitAround == 0 && star.planets[l].orbitIndex == num22 - 1) {
                        flag2 = false;
                        break;
                    }
                }

                if (flag2 && num4 < 0.2 + (double)num26 * 0.2) {
                    num24 = num26;
                }
            }

            num25 = ((num5 < 0.2) ? (num23 + 3) : ((num5 < 0.4) ? (num23 + 2) : ((num5 < 0.8) ? (num23 + 1) : 0)));
            if (num25 != 0 && num25 < 5) {
                num25 = 5;
            }

            star.asterBelt1OrbitIndex = num24;
            star.asterBelt2OrbitIndex = num25;
            if (num24 > 0) {
                star.asterBelt1Radius = StarGen.orbitRadius[num24] * (float)num6 * star.orbitScaler;
            }

            if (num25 > 0) {
                star.asterBelt2Radius = StarGen.orbitRadius[num25] * (float)num7 * star.orbitScaler;
            }

            for (int m = 0; m < star.planetCount; m++) {
                PlanetData planetData = star.planets[m];
                int orbitIndex6 = planetData.orbitIndex;
                int orbitAroundOrbitIndex = ((planetData.orbitAroundPlanet != null) ? planetData.orbitAroundPlanet.orbitIndex : 0);
                //AccessTools.Method(typeof(StarGen), "SetHiveOrbitConditionFalse").Invoke(null, new object[] { orbitIndex6, orbitAroundOrbitIndex, planetData.sunDistance / star.orbitScaler, star.index });
                StarGen.SetHiveOrbitConditionFalse(orbitIndex6, orbitAroundOrbitIndex, planetData.sunDistance / star.orbitScaler, star.index);
            }

            star.hiveAstroOrbits = new AstroOrbitData[8];
            AstroOrbitData[] hiveAstroOrbits = star.hiveAstroOrbits;
            int num27 = 0;
            for (int n = 0; n < StarGen.hiveOrbitCondition.Length; n++) {
                if (StarGen.hiveOrbitCondition[n]) {
                    num27++;
                }
            }

            for (int num28 = 0; num28 < 8; num28++) {
                double value = dotNet35Random3.NextDouble() * 2.0 - 1.0;
                double num29 = dotNet35Random3.NextDouble();
                double num30 = dotNet35Random3.NextDouble();
                value = (double)Math.Sign(value) * Math.Pow(Math.Abs(value), 0.7) * 90.0;
                num29 *= 360.0;
                num30 *= 360.0;
                float num31 = 0.3f;
                Assert.Positive(num27);
                if (num27 > 0) {
                    int num32 = ((star.index != 0) ? 5 : 2);
                    num32 = ((num27 > num32) ? num32 : num27);
                    int num33 = num32 * 100;
                    int num34 = num33 * 100;
                    int num35 = dotNet35Random3.Next(num33);
                    int num36 = num35 * num35 / num34;
                    for (int num37 = 0; num37 < StarGen.hiveOrbitCondition.Length; num37++) {
                        if (StarGen.hiveOrbitCondition[num37]) {
                            if (num36 == 0) {
                                num31 = StarGen.hiveOrbitRadius[num37];
                                StarGen.hiveOrbitCondition[num37] = false;
                                num27--;
                                break;
                            }

                            num36--;
                        }
                    }
                }

                float num38 = num31 * star.orbitScaler;
                hiveAstroOrbits[num28] = new AstroOrbitData();
                hiveAstroOrbits[num28].orbitRadius = num38;
                hiveAstroOrbits[num28].orbitInclination = (float)value;
                hiveAstroOrbits[num28].orbitLongitude = (float)num29;
                hiveAstroOrbits[num28].orbitPhase = (float)num30;
                if (gameDesc.creationVersion.Build < 20700) {
                    hiveAstroOrbits[num28].orbitalPeriod = Math.Sqrt(39.478417604357432 * (double)num31 * (double)num31 * (double)num31 / (1.3538551990520382E-06 * (double)star.mass));
                } else {
                    hiveAstroOrbits[num28].orbitalPeriod = Math.Sqrt(39.478417604357432 * (double)num38 * (double)num38 * (double)num38 / (5.4154207962081527E-06 * (double)star.mass));
                }

                hiveAstroOrbits[num28].orbitRotation = Quaternion.AngleAxis(hiveAstroOrbits[num28].orbitLongitude, Vector3.up) * Quaternion.AngleAxis(hiveAstroOrbits[num28].orbitInclination, Vector3.forward);
                hiveAstroOrbits[num28].orbitNormal = Maths.QRotateLF(hiveAstroOrbits[num28].orbitRotation, new VectorLF3(0f, 1f, 0f)).normalized;
            }
        }

        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetGen), "SetPlanetTheme")]
        public static void SetPlanetThemePatch(ref PlanetData planet, int[] themeIds, double rand1, double rand2, double rand3, double rand4, int theme_seed)
        {
            if (planet.star.index == 0)
            {
                switch (planet.index)
                {
                    case 0:
                        planet.type = EPlanetType.Vocano;
                        break;
                    case 1:
                        planet.type = EPlanetType.Ocean;
                        if (planet.singularity == EPlanetSingularity.TidalLocked || planet.singularity == EPlanetSingularity.TidalLocked2 || planet.singularity == EPlanetSingularity.TidalLocked4) {
                            planet.singularity = EPlanetSingularity.None;
                            planet.rotationPeriod *= 0.45f;
                        }
                        break;
                    case 2:
                        planet.type = EPlanetType.Desert;
                        planet.radius = 100f;
                        planet.precision = 100;
                        break;
                    case 3:
                        break;
                    case 4:
                        planet.type = EPlanetType.Ice;
                        break;
                }
            }

            //bool isAroundGasGiant = planet.orbitAroundPlanet != null && planet.orbitAroundPlanet.type == EPlanetType.Gas;

            //for (int i = 0; i < planet.star.planetCount; i++) {
            //List<int> orbitAroundPlanet = new List<int>();
            DotNet35Random DotNet35Random = new DotNet35Random();
            double Random = DotNet35Random.NextDouble();
            bool thisPlanetIsMoon = false;
            if (planet.orbitAroundPlanet != null) {
                if (planet.orbitAroundPlanet.type != EPlanetType.Gas) {
                    thisPlanetIsMoon = true;
                } else {
                    if (planet.orbitIndex >= 2) {
                        thisPlanetIsMoon = true;
                    }
                    for (int i = 0; i < planet.id; i++) {
                        if (planet.star.planets[i] == null) break;
                        if (planet.star.planets[i].radius == 100f && Random < 0.7) {
                            break;
                        }
                        if (planet.star.planetCount - 1 - i <= 1) {
                            if (Random < 0.99) {
                                thisPlanetIsMoon = true;
                            }
                        } else if (planet.star.planetCount - 1 - i <= 2) {
                            if (Random < 0.7) {
                                thisPlanetIsMoon = true;
                            }
                        } else if (planet.star.planetCount - 1 - i <= 3) {
                            if (Random < 0.5) {
                                thisPlanetIsMoon = true;
                            }
                        }
                    }
                }
                if (thisPlanetIsMoon) {
                    planet.radius = 100f;
                    planet.precision = 100;
                }
            }
            //}
        }

        public static void SetMoonTheme(ref PlanetData planet)
        {
            if (planet.radius == 100f) {
                planet.theme = 11;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetGen), nameof(PlanetGen.SetPlanetTheme))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Method(typeof(PlanetGen), nameof(PlanetGen.tmp_theme))),
                 new CodeMatch(OpCodes.Ldarg_2));

            matcher.Advance(13).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarga_S, 0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OverWriteBirthGalaxy), nameof(SetMoonTheme),
                new System.Type[] {
                    typeof(PlanetData).MakeByRefType(),
                    }
            )));
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        


    }
}
