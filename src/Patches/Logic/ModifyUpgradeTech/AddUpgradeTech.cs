using CommonAPI.Systems;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech
{
    internal class AddUpgradeTech
    {
        internal static void AddUpgradeTechs()
        {
            AddCoreUpgradeTechs();
            AddCombustionPowerUpgradeTechs();
            AddDamageUpgradeTechs();
            AddWreckageRecoveryUpgradeTechs();
            AddFleetUpgradeTechs();
            //Add2choose1Techs();
            AddPilerEjectorTechs();
            //AddWarpEngineTechs();
            //AddTempBugFixTechs();
        }

        internal static TechProto AddOneUpgradeTech(int id, string name, string description, string conclusion, string iconPath)
        {
            Vector2 oldPosition = new Vector2(0, 0);
            TechProto tech = LDB.techs.Select(id - 1);
            oldPosition = tech.Position;
            tech.Position = new Vector2(oldPosition.x + 4, oldPosition.y);
            tech.Level = 7;
            tech.PreTechs = new[] { id, };

            int[] preTechs = new[] { id - 2 };
            int[] costItems = new[] { 6279, 6004, 6005, };
            long costHash = tech.HashNeeded / 10;
            int[] costItemsPoints = new[] { tech.ItemPoints[0], tech.ItemPoints[0], tech.ItemPoints[0], };
            int[] unlockRecipes = new int[] { };
            Vector2 position = oldPosition;
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 6;
            NewTechProto.MaxLevel = 6;

            return NewTechProto;
        }

        internal static void AddWreckageRecoveryUpgradeTech(int id, string name, string description, string conclusion, string iconPath)
        {
            Vector2 oldPosition = new Vector2(0, 0);
            TechProto tech = LDB.techs.Select(id - 1);
            oldPosition = tech.Position;
            int techLevel = tech.Level;

            int[] preTechs = new[] { id - 1 };
            int[] costItems = new[] { 6279, 6004, 6005, };
            long costHash = 144000;
            int[] costItemsPoints = new[] { 10, 8, 6 };
            int[] unlockRecipes = new int[] { };
            Vector2 position = new Vector2(oldPosition.x + 4, oldPosition.y);
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = techLevel + 1;
            NewTechProto.MaxLevel = techLevel + 1;
            NewTechProto.UnlockFunctions = new int[] { 101 };
            NewTechProto.UnlockValues = new double[] { 18 };

            preTechs = new[] { id };
            costItems = new[] { 6006, };
            costHash = 180000;
            costItemsPoints = new[] { 8 };
            unlockRecipes = new int[] { };
            position = new Vector2(oldPosition.x + 8, oldPosition.y);
            NewTechProto = ProtoRegistry.RegisterTech(id + 1, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = techLevel + 2;
            NewTechProto.MaxLevel = techLevel + 2;
            NewTechProto.UnlockFunctions = new int[] { 101 };
            NewTechProto.UnlockValues = new double[] { 21 };

            preTechs = new[] { id + 1 };
            costItems = new[] { 6006, };
            costHash = 225000;
            costItemsPoints = new[] { 8 };
            unlockRecipes = new int[] { };
            position = new Vector2(oldPosition.x + 12, oldPosition.y);
            NewTechProto = ProtoRegistry.RegisterTech(id + 2, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = techLevel + 3;
            NewTechProto.MaxLevel = techLevel + 3;
            NewTechProto.UnlockFunctions = new int[] { 101 };
            NewTechProto.UnlockValues = new double[] { 24 };

            return;
        }


        internal static void AddCoreUpgradeTechs()
        {
            int id = 2107;
            string name = "T机甲核心";
            string description = "机甲核心描述";
            string conclusion = "机甲核心结果";
            string iconPath = "Icons/Tech/2106";
            TechProto NewTechProto = AddOneUpgradeTech(id, name, description, conclusion, iconPath);
            NewTechProto.UnlockFunctions = new int[] { 6, 82, 83, };
            NewTechProto.UnlockValues = new double[] { 3200000000d, 4d, 1000d, };
        }


        internal static void AddCombustionPowerUpgradeTechs()
        {
            int id = 2507;
            string name = "T能量回路";
            string description = "能量回路描述";
            string conclusion = "能量回路结果";
            string iconPath = "Icons/Tech/2506";
            TechProto NewTechProto = AddOneUpgradeTech(id, name, description, conclusion, iconPath);
            NewTechProto.UnlockFunctions = new int[] { 2 };
            NewTechProto.UnlockValues = new double[] { 1600000 };
        }


        internal static void AddDamageUpgradeTechs()
        {
            int id = 5007;
            string name = "T动能武器伤害";
            string description = "动能武器伤害描述";
            string conclusion = "动能武器伤害结果";
            string iconPath = "Icons/Tech/5006";
            TechProto NewTechProto = AddOneUpgradeTech(id, name, description, conclusion, iconPath);
            NewTechProto.ItemPoints = new int[] { 4, 4, 4 };
            NewTechProto.UnlockFunctions = new int[] { 61 };
            NewTechProto.UnlockValues = new double[] { 0.2 };
            NewTechProto.HashNeeded = 3600000;

            id = 5107;
            name = "T能量武器伤害";
            description = "能量武器伤害描述";
            conclusion = "能量武器伤害结果";
            iconPath = "Icons/Tech/5106";
            NewTechProto = AddOneUpgradeTech(id, name, description, conclusion, iconPath);
            NewTechProto.ItemPoints = new int[] { 4, 4, 4 };
            NewTechProto.UnlockFunctions = new int[] { 62 };
            NewTechProto.UnlockValues = new double[] { 0.2 };
            NewTechProto.HashNeeded = 5400000;

            id = 5207;
            name = "T爆炸武器伤害";
            description = "爆炸武器伤害描述";
            conclusion = "爆炸武器伤害结果";
            iconPath = "Icons/Tech/5206";
            NewTechProto = AddOneUpgradeTech(id, name, description, conclusion, iconPath);
            NewTechProto.ItemPoints = new int[] { 4, 4, 4 };
            NewTechProto.UnlockFunctions = new int[] { 63 };
            NewTechProto.UnlockValues = new double[] { 0.2 };
            NewTechProto.HashNeeded = 3600000;
        }

        internal static void AddWreckageRecoveryUpgradeTechs()
        {
            int id = 5306;
            string name = "残骸回收分析";
            string description = "T残骸回收分析";
            string conclusion = "T残骸回收分析";
            string iconPath = "Assets/texpack/回收科技";
            AddWreckageRecoveryUpgradeTech(id, name, description, conclusion, iconPath);
        }

        internal static void AddFleetUpgradeTechs()
        {
            int id = 5406;
            string name = "太空舰队火力升级";
            string description = "T太空舰队火力升级";
            string conclusion = "T太空舰队火力升级";
            string iconPath = "Icons/Tech/5303";

            Vector2 oldPosition = new Vector2(0, 0);
            TechProto tech = LDB.techs.Select(id - 1);
            oldPosition = tech.Position;

            int[] preTechs = new[] { id - 1 };
            int[] costItems = new[] { 6279, 6004, 6005, };
            long costHash = 360000;
            int[] costItemsPoints = new int[] { 10, 10, 10 };
            int[] unlockRecipes = new int[] { };
            Vector2 position = new Vector2(oldPosition.x + 4, oldPosition.y);
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 3;
            NewTechProto.MaxLevel = 3;
            NewTechProto.UnlockFunctions = new int[] { 72 };
            NewTechProto.UnlockValues = new double[] { 0.4 };
        }

        internal static void Add2choose1Techs()
        {
            int id = 5407;
            string name = "二选一科技";
            string description = "T二选一科技";
            string conclusion = "T二选一科技";
            string iconPath = "Icons/Tech/5303";

            Vector2 oldPosition = new Vector2(0, 0);
            TechProto tech = LDB.techs.Select(5401);
            oldPosition = tech.Position;

            int[] preTechs = new int[] { };
            int[] costItems = new[] { 1101 };
            long costHash = 3600;
            int[] costItemsPoints = new int[] { 1 };
            int[] unlockRecipes = new int[] { };
            Vector2 position = new Vector2(oldPosition.x - 4, oldPosition.y);
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 0;
            NewTechProto.MaxLevel = 0;
            NewTechProto.IsLabTech = false;
        }

        internal static void AddPilerEjectorTechs()
        {
            int id = 3151;
            string name = "电磁霰射";
            string description = "T电磁霰射";
            string conclusion = "T电磁霰射";
            string iconPath = "Icons/Tech/1503";

            int[] preTechs = new int[] {  };
            int[] costItems = new[] { 6279, 6004, };
            long costHash = 108000;
            int[] costItemsPoints = new int[] { 10, 10 };
            int[] unlockRecipes = new int[] { };
            Vector2 position = new Vector2(9, -11);
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.PreTechsImplicit = new int[] { 1503 };
            NewTechProto.Level = 1;
            NewTechProto.MaxLevel = 1;

            preTechs = new int[] { id };
            costHash = 216000;
            position = new Vector2(13, -11);
            NewTechProto = ProtoRegistry.RegisterTech(id + 1, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 2;
            NewTechProto.MaxLevel = 2;

            preTechs = new int[] { id + 1 };
            costItems = new[] { 6279, 6004, 6005 };
            costHash = 216000;
            costItemsPoints = new int[] { 10, 10, 10 };
            position = new Vector2(17, -11);
            NewTechProto = ProtoRegistry.RegisterTech(id + 2, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 3;
            NewTechProto.MaxLevel = 3;
        }

        


        internal static void AddTempBugFixTechs()
        {

            Vector2 oldPosition = new Vector2(0, 0);
            TechProto tech = LDB.techs.Select(2101);
            oldPosition = tech.Position;

            int id = 2108;
            string name = "修复bug";
            string description = "仅适用于0.6.11版本前就已经开始游戏的存档，修复深空物流港无法装填翘曲器，运输船与货舰运量初始非1200，初始速度未翻倍问题。";
            string conclusion = "T残骸回收分析";
            string iconPath = "Assets/texpack/回收科技";

            int[] preTechs = new int[] {  };
            int[] costItems = new[] { 6521 };
            long costHash = 360;
            int[] costItemsPoints = new[] { 10 };
            int[] unlockRecipes = new int[] { };
            Vector2 position = new Vector2(oldPosition.x - 4, oldPosition.y);
            TechProto NewTechProto = ProtoRegistry.RegisterTech(id, name, description, conclusion, iconPath, preTechs, costItems, costItemsPoints, costHash, unlockRecipes, position);
            NewTechProto.Level = 0;
            NewTechProto.MaxLevel = 0;

            NewTechProto.IsLabTech = false;

                NewTechProto.UnlockFunctions = new int[] { 15,
            16,
            17,
            19 };
                NewTechProto.UnlockValues = new double[] { 0.5,
            1.0,
            0,
            1000, };

        }
    }
}
