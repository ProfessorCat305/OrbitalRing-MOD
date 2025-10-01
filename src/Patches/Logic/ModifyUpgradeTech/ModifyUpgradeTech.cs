using CommonAPI.Systems;
using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Compatibility;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using WinAPI;
using static GalacticScale.PatchOnUIGalaxySelect;
using static ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech.AddUpgradeTech;

namespace ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech
{
    internal static class ModifyUpgradeTech
    {
        private static readonly int[] Items5 =
                                      {
                                          6001, 6002, 6003, 6004,
                                          6005,
                                      },
                                      Items4 =
                                      {
                                          6001, 6002, 6003, 6004,
                                      },
                                      Items3 = { 6001, 6002, 6003, },
                                      Items2 = { 6001, 6002, };

        internal static void ModifyUpgradeTeches()
        {
            TechProto tech = LDB.techs.Select(ProtoID.T批量建造1);
            tech.HashNeeded = 1200;
            tech.UnlockValues = new[] { 450.0, 3600.0, };

            tech = LDB.techs.Select(ProtoID.T批量建造2);
            tech.UnlockValues = new[] { 900.0, 7200.0, };

            tech = LDB.techs.Select(ProtoID.T批量建造3);
            tech.UnlockValues = new[] { 1800.0, 14400.0, };

            tech = LDB.techs.Select(ProtoID.T能量回路4);
            tech.Items = Items4;
            tech.ItemPoints = Enumerable.Repeat(12, 4).ToArray();

            tech = LDB.techs.Select(ProtoID.T驱动引擎4);
            tech.Items = Items4;
            tech.ItemPoints = Enumerable.Repeat(10, 4).ToArray();

            tech = LDB.techs.Select(ProtoID.T驱动引擎5);
            tech.Items = Items5;
            tech.ItemPoints = Enumerable.Repeat(10, 5).ToArray();

            tech = LDB.techs.Select(ProtoID.T垂直建造3);
            tech.Items = Items3;
            tech.ItemPoints = new[] { 20, 20, 10, };

            tech = LDB.techs.Select(ProtoID.T垂直建造6);
            tech.Items = Items5;
            tech.ItemPoints = Enumerable.Repeat(6, 5).ToArray();

            tech = LDB.techs.Select(ProtoID.T集装分拣6);
            tech.Items = Items5;
            tech.ItemPoints = Enumerable.Repeat(6, 5).ToArray();


            //    TechProto techProto = LDB.techs.Select(2101);
            //    Debug.LogFormat("scppppppppppppperppppppppp");
            //    Debug.LogFormat("techProto.ID {0} techProto.Name {1} techProto.pos {2}, {3}", techProto.ID, techProto.Name, techProto.Position.x, techProto.Position.y);

            //techProto = LDB.techs.Select(2801);
            //Debug.LogFormat("scppppppppppppperppppppppp");
            //Debug.LogFormat("techProto.ID {0} techProto.Name {1} techProto.pos {2}, {3}", techProto.ID, techProto.Name, techProto.Position.x, techProto.Position.y);

            //techProto = LDB.techs.Select(3101);
            //Debug.LogFormat("scppppppppppppperppppppppp");
            //Debug.LogFormat("techProto.ID {0} techProto.Name {1} techProto.pos {2}, {3}", techProto.ID, techProto.Name, techProto.Position.x, techProto.Position.y);

            //techProto = LDB.techs.Select(3901);
            //Debug.LogFormat("scppppppppppppperppppppppp");
            //Debug.LogFormat("techProto.ID {0} techProto.Name {1} techProto.pos {2}, {3}", techProto.ID, techProto.Name, techProto.Position.x, techProto.Position.y);

            //techProto = LDB.techs.Select(5001);
            //Debug.LogFormat("scppppppppppppperppppppppp");
            //Debug.LogFormat("techProto.ID {0} techProto.Name {1} techProto.pos {2}, {3}", techProto.ID, techProto.Name, techProto.Position.x, techProto.Position.y);

            ModifyAllUpgradeTechs();

            ModifyObservedTechs();
            ModifyCoreUpgradeTechs();
            ModifyMoveUpgradeTechs();
            ModifyPackageUpgradeTechs();
            ModifyBuilderNumberUpgradeTechs();
            ModifyReBuildUpgradeTechs();
            ModifyCombustionPowerUpgradeTechs();
            ModifyBuilderSpeedUpgradeTechs();
            ModifyFlySpeedUpgradeTechs();
            ModifySolarSailingLifeUpgradeTechs();
            ModifySolarSailingAdsorbSpeedUpgradeTechs();
            ModifyRayEfficiencyUpgradeTechs();
            ModifyWhiteGrabUpgradeTechs();
            ModifySpacecraftSpeedUpgradeTechs();
            ModifySpacecraftExpansionUpgradeTechs();
            ModifyMinerUpgradeTechs();
            ModifyStationStackingUpgradeTechs();
            ModifyDamageUpgradeTechs();
            ModifyWreckageRecoveryUpgradeTechs();
            ModifyUAVHPAndfiringRateUpgradeTechs();
            ModifyGroundFormationExpansionUpgradeTechs();
            ModifySpaceFormationExpansionUpgradeTechs();
            ModifyPlanetFieldUpgradeTechs();
            ModifyFleetUpgradeTechs();
            ModifyECMUpgradeTechs();

            AddUpgradeTechs();

            for (int i = 3701; i <= 3706; i++)
            {
                TechProto techProto = LDB.techs.Select(i);
                techProto.UnlockValues = new[] { 1d, 1d };
            }
        }

        internal static void ModifyAllUpgradeTechs()
        {
            foreach (TechProto techProto in LDB.techs.dataArray)
            {
                if (techProto.ID < 2000) continue;

                int[] items = techProto.Items;

                if (items.SequenceEqual(Items5))
                {
                    techProto.Items = new[] { 6279, 6004 };
                    techProto.ItemPoints = new[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                    continue;
                }

                if (items.SequenceEqual(Items4))
                {
                    techProto.Items = new int[] { 6003, 6278 };
                    techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                    continue;
                }

                if (items.SequenceEqual(Items3))
                {
                    techProto.Items = new[] { 6003, };
                    techProto.ItemPoints = new[] { techProto.ItemPoints[0] };
                    continue;
                }
            }
        }

        internal static void ModifyObservedTechs()
        {
            TechProto techProto;
            for (int i = ProtoID.T宇宙探索1; i <= ProtoID.T宇宙探索3; i++)
            {
                techProto = LDB.techs.Select(i);
                if (i == ProtoID.T宇宙探索1)
                {
                    techProto.Items = new[] { 6507, };
                    techProto.Desc = "T宇宙探索1";
                    techProto.RefreshTranslation();
                }
                else if (i == ProtoID.T宇宙探索2)
                {
                    techProto.Items = new[] { 6508, };
                    techProto.Desc = "T宇宙探索2";
                    techProto.RefreshTranslation();
                }
                else if (i == ProtoID.T宇宙探索3)
                {
                    techProto.Items = new[] { 6509, };
                    techProto.Desc = "T宇宙探索3";
                    techProto.RefreshTranslation();
                }
                techProto.ItemPoints = new[] { 1, };
                techProto.HashNeeded = 3600;
                techProto.IsLabTech = false;
            }
            techProto = LDB.techs.Select(ProtoID.T宇宙探索4);
            techProto.Desc = "T宇宙探索4";
            techProto.RefreshTranslation();
            techProto.Items = new[] { 6280, };
            techProto.ItemPoints = new[] { 60, };
            techProto.HashNeeded = 6000;
            techProto.IsLabTech = false;
        }

        internal static void ModifyCoreUpgradeTechs()
        {
            TechProto techProto;
            double coreNenergy = 0;
            for (int i = 2101; i <= 2105; i++)
            {
                techProto = LDB.techs.Select(i);
                coreNenergy = techProto.UnlockValues[0];
                techProto.UnlockFunctions = new[] { 6, 82, 83, };
                techProto.UnlockValues = new double[] { coreNenergy, 4d, 1000d, };

                switch (i)
                {
                    case 2101:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        techProto.IsLabTech = true;
                        break;
                    case 2102:
                        break;
                    case 2103:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        break;
                    case 2104:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2105:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    default:
                        break;
                }
            }
            TechProto coreInfinItetechProto = LDB.techs.Select(2106);
            coreNenergy = coreInfinItetechProto.UnlockValues[0];
            coreInfinItetechProto.UnlockFunctions = new[] { 6, 83, };
            coreInfinItetechProto.UnlockValues = new double[] { coreNenergy, 200d, };
        }

        internal static void ModifyMoveUpgradeTechs()
        {
            TechProto techProto;
            double moveSpped = 0;
            for (int i = 2201; i <= 2208; i++)
            {
                techProto = LDB.techs.Select(i);
                moveSpped = techProto.UnlockValues[0];
                techProto.UnlockFunctions = new int[] { 3, 81 };
                techProto.UnlockValues = new double[] { moveSpped, moveSpped * 75000 };
                switch (i)
                {
                    case 2201:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        techProto.IsLabTech = true;
                        break;
                    case 2202:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        break;
                    case 2203:
                        break;
                    case 2204:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2205:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2206:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2207:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2208:
                        techProto.Items = new int[] { 6279, 6004, 6005 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    default:
                        break;
                }
            }
        }

        internal static void ModifyPackageUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 2301; i <= 2307; i++)
            {
                techProto = LDB.techs.Select(i);
                switch (i)
                {
                    case 2301:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        techProto.IsLabTech = true;
                        break;
                    case 2302:
                        break;
                    case 2303:
                        techProto.Items = new int[] { 6001, 6002 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2304:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        break;
                    case 2305:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2306:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2307:
                        techProto.Items = new int[] { 6279, 6004, 6005 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    default:
                        break;
                }
            }
        }

        internal static void ModifyBuilderNumberUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 2404; i <= 2406; i++)
            {
                techProto = LDB.techs.Select(i);
                switch (i)
                {
                    case 2404:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        break;
                    case 2305:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2306:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    default:
                        break;
                }
            }
        }

        internal static void ModifyReBuildUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 2953; i <= 2956; i++)
            {
                techProto = LDB.techs.Select(i);

                switch (i)
                {
                    case 2953:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        break;
                    case 2954:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2955:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2956:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    default:
                        break;
                }
            }
        }

        internal static void ModifyCombustionPowerUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 2501; i <= 2506; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.UnlockValues = new double[] { techProto.UnlockValues[0] * 2 };

                switch (i)
                {
                    case 2501:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0] };
                        techProto.IsLabTech = true;
                        break;
                    case 2502:
                    case 2503:
                        break;
                    case 2504:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
                        break;
                    case 2506:
                        break;
                    default:
                        break;
                }
            }
        }


        internal static void ModifyBuilderSpeedUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(2606);
            techProto.MaxLevel = 21;
        }

        internal static void ModifyFlySpeedUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(2901);
            techProto.Items = new int[] { 1407 };
            techProto.ItemPoints = new int[] { 200 };
            techProto.HashNeeded = 450;

            techProto = LDB.techs.Select(2902);
            techProto.Items = new int[] { 1405 };
            techProto.ItemPoints = new int[] { 150 };
            techProto.HashNeeded = 600;
            techProto.IsLabTech = false;

            techProto = LDB.techs.Select(2903);
            techProto.Items = new int[] { 6277 };
            techProto.ItemPoints = new int[] { 100 };
            techProto.HashNeeded = 1800;
            techProto.IsLabTech = false;

            techProto = LDB.techs.Select(2904);
            techProto.Items = new int[] { 6227 };
            techProto.ItemPoints = new int[] { 1 };
            techProto.HashNeeded = 3600;
            techProto.IsLabTech = false;

            techProto = LDB.techs.Select(2905);
            techProto.Items = new int[] { 6227 };
            techProto.ItemPoints = new int[] { 2 };
            techProto.HashNeeded = 7200;
            techProto.IsLabTech = false;

            techProto = LDB.techs.Select(2906);
            techProto.Items = new int[] { 6227 };
            techProto.ItemPoints = new int[] { 1 };
            techProto.HashNeeded = -90000;
            techProto.LevelCoef1 = 18000;
            techProto.LevelCoef2 = 0;
            techProto.IsLabTech = false;
        }

        internal static void ModifySolarSailingLifeUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3106);
            techProto.Items = new int[] { 6279, 6004 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifySolarSailingAdsorbSpeedUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 4201; i <= 4206; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.Items = new int[] { 6279, 6004, 6005 };
                techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
            }
        }

        internal static void ModifyRayEfficiencyUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3207);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifyWhiteGrabUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 3313; i <= 3314; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.Items = new int[] { 6003, 6278 };
                techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
            }
            techProto = LDB.techs.Select(3315);
            techProto.Items = new int[] { 6279, 6004 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };

            techProto = LDB.techs.Select(3316);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifySpacecraftSpeedUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3404);
            techProto.UnlockFunctions = new int[] { 15, 34, 16 };
            techProto.UnlockValues = new double[] { 0.3, 0.15, 0.5 };
            techProto.PreTechsImplicit = new int[] { };
            techProto.Desc = "T航天器速度提升";

            techProto = LDB.techs.Select(3406);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifySpacecraftExpansionUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3508);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifyMinerUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3604);
            techProto.Items = new int[] { 6279, 6004 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };

            techProto = LDB.techs.Select(3605);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };

            techProto = LDB.techs.Select(3606);
            techProto.MaxLevel = 11;
        }

        // 出塔堆叠三个科技需求减半
        internal static void ModifyStationStackingUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(3801);
            techProto.HashNeeded = 4800000;

            techProto = LDB.techs.Select(3802);
            techProto.HashNeeded = 14400000;

            techProto = LDB.techs.Select(3803);
            techProto.HashNeeded = 21600000;
        }

        internal static void ModifyDamageUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 5004; i <= 5005; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.UnlockValues = new double[] { 0.2 };
            }
            techProto = LDB.techs.Select(5006);
            techProto.Items = new int[] { 5201 };
            techProto.IsLabTech = false;
            techProto.HashNeeded = techProto.HashNeeded / 10;
            techProto.LevelCoef1 = techProto.LevelCoef1 / 10;
            techProto.LevelCoef2 = techProto.LevelCoef2 / 10;
            techProto.Desc = "T动能武器伤害无限";
            techProto.RefreshTranslation();

            for (int i = 5104; i <= 5105; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.UnlockValues = new double[] { 0.2 };
            }
            techProto = LDB.techs.Select(5106);
            techProto.Items = new int[] { 5201 };
            techProto.IsLabTech = false;
            techProto.HashNeeded = techProto.HashNeeded / 10;
            techProto.LevelCoef1 = techProto.LevelCoef1 / 10;
            techProto.LevelCoef2 = techProto.LevelCoef2 / 10;
            techProto.Desc = "T能量武器伤害无限";
            techProto.RefreshTranslation();

            for (int i = 5204; i <= 5205; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.UnlockValues = new double[] { 0.2 };
            }
            techProto = LDB.techs.Select(5206);
            techProto.Items = new int[] { 5201 };
            techProto.IsLabTech = false;
            techProto.HashNeeded = techProto.HashNeeded / 10;
            techProto.LevelCoef1 = techProto.LevelCoef1 / 10;
            techProto.LevelCoef2 = techProto.LevelCoef2 / 10;
            techProto.Desc = "T爆破武器伤害无限";
            techProto.RefreshTranslation();

            techProto = LDB.techs.Select(5201);
            techProto.PreTechsImplicit = new[] { 1807, };
        }

        internal static void ModifyWreckageRecoveryUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 5301; i <= 5305; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.Name = "残骸回收分析";
                techProto.Desc = "T残骸回收分析";
                techProto.RefreshTranslation();
                techProto.IconPath = "Assets/texpack/回收科技";

                switch (i)
                {
                    case 5301:
                        techProto.Items = new int[] { 6001 };
                        techProto.ItemPoints = new int[] { 10 };
                        techProto.HashNeeded = 18000;
                        techProto.UnlockFunctions = new int[] { 101 };
                        techProto.UnlockValues = new double[] { 3 };
                        break;
                    case 5302:
                        techProto.Items = new int[] { 6001, 6002 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 36000;
                        techProto.UnlockFunctions = new int[] { 101 };
                        techProto.UnlockValues = new double[] { 6 };
                        break;
                    case 5303:
                        techProto.Items = new int[] { 6003 };
                        techProto.ItemPoints = new int[] { 10 };
                        techProto.HashNeeded = 54000;
                        techProto.UnlockFunctions = new int[] { 101 };
                        techProto.UnlockValues = new double[] { 9 };
                        break;
                    case 5304:
                        techProto.Items = new int[] { 6003, 6278 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 72000;
                        techProto.UnlockFunctions = new int[] { 101 };
                        techProto.UnlockValues = new double[] { 12 };
                        break;
                    case 5305:
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { 10, 8 };
                        techProto.HashNeeded = 108000;
                        techProto.LevelCoef1 = 0;
                        techProto.LevelCoef2 = 0;
                        techProto.UnlockFunctions = new int[] { 101 };
                        techProto.UnlockValues = new double[] { 15 };
                        break;
                    default:
                        break;
                }
            }
            techProto = LDB.techs.Select(5301);
            techProto.PreTechsImplicit = new[] { 1826, };

            techProto = LDB.techs.Select(5305);
            techProto.MaxLevel = 5;

        }

        internal static void ModifyFleetUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 5401; i <= 5405; i++)
            {
                techProto = LDB.techs.Select(i);
                switch (i)
                {
                    case 5401:
                        techProto.Name = "太空舰队结构优化";
                        techProto.Desc = "T太空舰队结构优化";
                        techProto.IconPath = "Icons/Tech/5601";
                        techProto.RefreshTranslation();
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 216000;
                        techProto.UnlockFunctions = new int[] { 73, 72 };
                        techProto.UnlockValues = new double[] { 0.25, 0.3 };
                        techProto.PreTechsImplicit = new[] { 1822, };
                        techProto.IsHiddenTech = false;
                        break;
                    case 5402:
                        techProto.Name = "太空舰队结构优化";
                        techProto.Desc = "T太空舰队结构优化";
                        techProto.IconPath = "Icons/Tech/5602";
                        techProto.RefreshTranslation();
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 288000;
                        techProto.UnlockFunctions = new int[] { 73, 72 };
                        techProto.UnlockValues = new double[] { 0.3, 0.3 };
                        break;
                    case 5403:
                        techProto.Name = "太空舰队结构优化";
                        techProto.Desc = "T太空舰队结构优化";
                        techProto.IconPath = "Icons/Tech/5603";
                        techProto.RefreshTranslation();
                        techProto.Items = new int[] { 6279, 6004, 6005 };
                        techProto.ItemPoints = new int[] { 10, 10, 10 };
                        techProto.HashNeeded = 360000;
                        techProto.UnlockFunctions = new int[] { 73, 72 };
                        techProto.UnlockValues = new double[] { 0.35, 0.4 };
                        break;
                    case 5404:
                        techProto.Name = "太空舰队火力升级";
                        techProto.Desc = "T太空舰队火力升级";
                        techProto.IconPath = "Icons/Tech/5301";
                        techProto.RefreshTranslation();
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 216000;
                        techProto.Level = 1;
                        techProto.MaxLevel = 1;
                        techProto.UnlockFunctions = new int[] { 71, 72 };
                        techProto.UnlockValues = new double[] { 0.4, 0.3 };
                        techProto.PreTechs = new int[] { };
                        techProto.PreTechsImplicit = new[] { 1822 }; //1822,
                        techProto.IsHiddenTech = false;
                        break;
                    case 5405:
                        techProto.Name = "太空舰队火力升级";
                        techProto.Desc = "T太空舰队火力升级";
                        techProto.IconPath = "Icons/Tech/5302";
                        techProto.RefreshTranslation();
                        techProto.Items = new int[] { 6279, 6004 };
                        techProto.ItemPoints = new int[] { 10, 10 };
                        techProto.HashNeeded = 288000;
                        techProto.UnlockFunctions = new int[] { 71, 72 };
                        techProto.UnlockValues = new double[] { 0.6, 0.3 };
                        techProto.Level = 2;
                        techProto.MaxLevel = 2;
                        break;
                }
            }
        }

        internal static void ModifyUAVHPAndfiringRateUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 5601; i <= 5605; i++)
            {
                techProto = LDB.techs.Select(i);
                switch (i)
                {
                    case 5601:
                        techProto.Name = "机兵升级计划";
                        techProto.Desc = "T机兵升级计划";
                        techProto.RefreshTranslation();
                        techProto.UnlockFunctions = new int[] { 68, 69 };
                        techProto.UnlockValues = new double[] { 0.1, 0.05 };
                        break;
                    case 5602:
                        techProto.Name = "迭代升级";
                        techProto.Desc = "T迭代升级";
                        techProto.RefreshTranslation();
                        techProto.UnlockFunctions = new int[] { 68, 69 };
                        techProto.UnlockValues = new double[] { 0.1, 0.1 };
                        break;
                    case 5603:
                        techProto.Name = "迭代升级";
                        techProto.Desc = "T迭代升级";
                        techProto.RefreshTranslation();
                        techProto.UnlockFunctions = new int[] { 68, 69 };
                        techProto.UnlockValues = new double[] { 0.2, 0.1 };
                        break;
                    case 5604:
                        techProto.Name = "军械量产方案";
                        techProto.Desc = "T军械量产方案";
                        techProto.RefreshTranslation();
                        techProto.UnlockFunctions = new int[] { 68, 69 };
                        techProto.UnlockValues = new double[] { 0.3, 0.05 };
                        break;
                    case 5605:
                        techProto.Name = "迭代升级";
                        techProto.Desc = "T迭代升级";
                        techProto.RefreshTranslation();
                        techProto.UnlockFunctions = new int[] { 68, 69 };
                        techProto.UnlockValues = new double[] { 0.3, 0.2 };
                        break;
                }

            }
            techProto = LDB.techs.Select(5601);
            techProto.PreTechsImplicit = new[] { 1819, };
        }

        internal static void ModifyGroundFormationExpansionUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(5801);
            techProto.UnlockFunctions = new int[] { 77 };
            techProto.UnlockValues = new double[] { 1 };

            techProto = LDB.techs.Select(5803);
            techProto.UnlockFunctions = new int[] { 78 };
            techProto.UnlockValues = new double[] { 2 };

            techProto = LDB.techs.Select(5806);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        internal static void ModifySpaceFormationExpansionUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 5901; i <= 5903; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.Items = new int[] { 6279, 6004, };
                techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };
            }
            for (int i = 5904; i <= 5905; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.Items = new int[] { 6279, 6004, 6005 };
                techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
            }
        }

        internal static void ModifyPlanetFieldUpgradeTechs()
        {
            TechProto techProto = LDB.techs.Select(5702);
            techProto.Items = new int[] { 6003, 6278 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };

            techProto = LDB.techs.Select(5703);
            techProto.Items = new int[] { 6279, 6004, };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0] };

            techProto = LDB.techs.Select(5704);
            techProto.Items = new int[] { 6279, 6004, 6005 };
            techProto.ItemPoints = new int[] { techProto.ItemPoints[0], techProto.ItemPoints[0], techProto.ItemPoints[0] };
        }

        // 清空原ECM升级科技效果
        internal static void ModifyECMUpgradeTechs()
        {
            TechProto techProto;
            for (int i = 6101; i <= 6106; i++)
            {
                techProto = LDB.techs.Select(i);
                techProto.UnlockFunctions = new int[] { };
            }
        }

        // 检测黑洞巨构功率，达成目标解锁通关科技
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.BeforeGameTick))]
        [HarmonyPostfix]
        public static void BeforeGameTickPatch(DysonSphere __instance)
        {
            if (__instance.starData.type == EStarType.BlackHole)
            {
                if (!GameMain.history.TechUnlocked(1952))
                {
                    if (__instance.energyGenCurrentTick > 8000000)
                    {
                        GameMain.history.UnlockTech(1952);
                    }
                }
                else if (LDB.techs.Select(1934).IsHiddenTech == true)
                {
                    if (__instance.energyGenCurrentTick > 10000000)
                    {
                        LDB.techs.Select(1934).IsHiddenTech = false;
                    }
                }
                else if (LDB.techs.Select(1959).IsHiddenTech == true)
                {
                    if (__instance.energyGenCurrentTick > 10000000)
                    {
                        LDB.techs.Select(1959).IsHiddenTech = false;
                    }
                }
                else if (!GameMain.history.TechUnlocked(1960))
                {
                    if (__instance.energyGenCurrentTick > 40000000)
                    {
                        GameMain.history.UnlockTech(1960);
                    }
                }
                else if (!GameMain.history.TechUnlocked(1814))
                {
                    if (__instance.energyGenCurrentTick > 80000000)
                    {
                        GameMain.history.UnlockTech(1814);
                    }
                }
            }
        }
    }
}
