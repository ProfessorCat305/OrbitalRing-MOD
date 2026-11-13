using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using HarmonyLib;
using static ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine.UI;
using UnityEngine;
using xiaoye97;
using PluginInfo = BepInEx.PluginInfo;
using UnityEngine.UI;
using ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine;
using ProjectOrbitalRing.Utils;
using static GalacticScale.NEB;

// ReSharper disable InconsistentNaming
// ReSharper disable LoopCanBePartlyConvertedToQuery

namespace ProjectOrbitalRing.Compatibility
{
    internal static class MoreMegaStructure
    {
        internal const string GUID = "Gnimaerd.DSP.plugin.MoreMegaStructure";

        private static readonly Harmony HarmonyPatch = new Harmony("ProjectOrbitalRing.Compatibility." + GUID);

        private static readonly int[] AddedRecipes =
        {
            530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 565, 571, 572
        };

        private static FieldInfo HashGenDivisor;
        private static FieldInfo WarpAccDivisor;
        private static FieldInfo multifunctionComponentHeat;
        private static FieldInfo energyPerTickRequiredByLevel;

        private static bool _finished;

        internal static void Awake()
        {
            if (!Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo pluginInfo)) return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.ReceiverPatchers"), "InitRawData"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(InitRawData_Transpiler)));
            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.UIReceiverPatchers"), "InitDict"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(InitDict_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "RefreshUILabels", new[] { typeof(StarData), typeof(bool) }), null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(RefreshUILabels_Postfix)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "UIValueUpdate"), null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(UIValueUpdate_Postfix)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "SetMegaStructure"), null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(SetMegaStructure_Postfix)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "DysonSphereGameTickPostPatch"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(DysonSphereGameTickPostPatch_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded)), null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(LDBToolOnPostAddDataAction))
                {
                    after = new[] { LDBToolPlugin.MODGUID, },
                });

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.StarAssembly"), "Import"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(Import_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.StarAssembly"), "IntoOtherSave"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(IntoOtherSave_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("MoreMegaStructure.MMSProtos"), "AddNewItems"), null, null,
                new HarmonyMethod(typeof(MoreMegaStructure), nameof(AddNewItems_Transpiler)));

            HashGenDivisor = AccessTools.Field(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "HashGenDivisor");
            HashGenDivisor.SetValue(null, 200000000L);

            WarpAccDivisor = AccessTools.Field(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "WarpAccDivisor");
            WarpAccDivisor.SetValue(null, 50000000L);

            multifunctionComponentHeat = AccessTools.Field(assembly.GetType("MoreMegaStructure.MoreMegaStructure"), "multifunctionComponentHeat");
            multifunctionComponentHeat.SetValue(null, 22500000000L);

            energyPerTickRequiredByLevel = AccessTools.Field(assembly.GetType("MoreMegaStructure.StarCannon"), "energyPerTickRequiredByLevel");
            List<long> newEnergyPerTickRequiredByLevel = new List<long> { 0, 500000000, 2500000000, 10000000000, 20000000000, 45000000000, 45000000000 };
            energyPerTickRequiredByLevel.SetValue(null, newEnergyPerTickRequiredByLevel);

            ProjectOrbitalRing.MoreMegaStructureCompatibility = true;
        }

        public static void LDBToolOnPostAddDataAction()
        {
            if (_finished) return;

            foreach (int recipeID in AddedRecipes)
            {
                RecipeProto recipeProto = LDB.recipes.Select(recipeID);

                if (recipeProto == null) continue;

                recipeProto.Type = (ERecipeType)10;

                switch (recipeProto.ID)
                {
                    case 533: //量子隧穿
                        recipeProto.Items[0] = 1206; // 粒子容器
                        recipeProto.Items[1] = 1404; // 光子合并器
                        recipeProto.ItemCounts[1] = 1;
                        break;

                    case 534: //谐振盘
                        recipeProto.Items[2] = 7805; // 量子芯片换成量子计算机
                        break;

                    case 536: //量子服务集群
                        recipeProto.Items[0] = 7805; // 量子芯片换成量子计算机
                        recipeProto.Items[1] = 7019; // 粒子宽带换成高速连接器
                        recipeProto.ItemCounts[1] = 8;
                        break;

                    case 538: //物质解压器运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9482, 9484, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 2, 1, 2, 4 };
                        break;

                    case 539: //科学枢纽运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9481, 9486, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 3, 1, 2, 4 };
                        break;

                    case 540: //谐振发射器运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9480, 9484, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 1, 4, 2, 4 };
                        break;

                    case 541: //星际组装厂运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9487, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 2, 2, 4 };
                        break;

                    case 542: //晶体重构器运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9485, 7805, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 1, 2, 2, 4 };
                        break;

                    case 572: //恒星炮运载火箭
                        recipeProto.Type = (ERecipeType)9;
                        recipeProto.Items = new int[] { 9509, 7805, 1802, 6277 };
                        recipeProto.ItemCounts = new int[] { 2, 2, 2, 4 };
                        break;
                }
            }

            ItemProto itemProto = LDB.items.Select(9500);
            itemProto.recipes = null;
            itemProto.FindRecipes();
            itemProto.isRaw = true;

            itemProto = LDB.items.Select(9486); //量子服务器群集
            itemProto.Name = "量子服务器群集".TranslateFromJson();
            itemProto.RefreshTranslation();

            TechProto techProto = LDB.techs.Select(1918);
            techProto.Position = new Vector2(80, 8);

            techProto = LDB.techs.Select(1963);
            techProto.UnlockRecipes = new int[] {
                120,
                160,
                352
            };

            _finished = true;
        }

        public static IEnumerable<CodeInstruction> InitRawData_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 1126));
            matcher.SetOperandAndAdvance(1127);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 6000000));
            matcher.SetOperandAndAdvance(60000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 6000000));
            matcher.SetOperandAndAdvance(60000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 6000000));
            matcher.SetOperandAndAdvance(60000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 12000000));
            matcher.SetOperandAndAdvance(120000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 12000000));
            matcher.SetOperandAndAdvance(120000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 12000000));
            matcher.SetOperandAndAdvance(120000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5000000));
            matcher.SetOperandAndAdvance(50000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5000000));
            matcher.SetOperandAndAdvance(50000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5000000));
            matcher.SetOperandAndAdvance(50000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5000000));
            matcher.SetOperandAndAdvance(50000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5000000));
            matcher.SetOperandAndAdvance(50000000);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 6000000));
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, 3000000000L));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 1126));
            matcher.SetOperandAndAdvance(1127);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 120000000));
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, 9000000000L));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 12000000));
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I8, 1000000000L));

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> InitDict_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 1126));

            matcher.SetOperandAndAdvance(1127);

            return matcher.InstructionEnumeration();
        }


        public static void RefreshUILabels_Postfix(StarData star)
        {
            if (star == null) return;
            if (star.type == EStarType.BlackHole)
            {
                Type targetType = AccessTools.TypeByName("MoreMegaStructure.MoreMegaStructure");
                if (targetType == null) return;

                FieldInfo RightStarPowRatioTextField = AccessTools.Field(targetType, "RightStarPowRatioText");
                if (RightStarPowRatioTextField == null) return;
                Text RightStarPowRatioText = (Text)RightStarPowRatioTextField.GetValue(null);

                RightStarPowRatioText.text = "引力系数".TranslateFromJson();

                // 2. 获取字段引用
                FieldInfo set2DysonButtonTextTransField = AccessTools.Field(targetType, "set2DysonButtonTextTrans");
                if (set2DysonButtonTextTransField == null) return;

                //var set2MatDecomButtonTextTrans = AccessTools.Field(TargetType, "set2MatDecomButtonTextTrans");
                Transform set2DysonButtonTextTrans = (Transform)set2DysonButtonTextTransField.GetValue(null);
                if (!GameMain.history.TechUnlocked(1802))
                {
                    set2DysonButtonTextTrans.GetComponent<Text>().text = "规划".TranslateFromJson() + "???".Translate();
                }
                else if (!GameMain.history.TechUnlocked(1952))
                {
                    set2DysonButtonTextTrans.GetComponent<Text>().text = "规划".TranslateFromJson() + "共鸣阵列".TranslateFromJson();
                }
                else
                {
                    set2DysonButtonTextTrans.GetComponent<Text>().text = "规划".TranslateFromJson() + "数学率引擎".TranslateFromJson();
                }

                FieldInfo StarMegaStructureTypeField = AccessTools.Field(targetType, "StarMegaStructureType");
                if (StarMegaStructureTypeField == null) return;

                int[] StarMegaStructureType = (int[])StarMegaStructureTypeField.GetValue(null);

                FieldInfo RightDysonTitleField = AccessTools.Field(targetType, "RightDysonTitle");
                Text RightDysonTitle = (Text)RightDysonTitleField.GetValue(null);

                FieldInfo RightMaxPowGenTextField = AccessTools.Field(targetType, "RightMaxPowGenText");
                Text RightMaxPowGenText = (Text)RightMaxPowGenTextField.GetValue(null);

                int idx = star.id - 1;
                idx = idx < 0 ? 0 : (idx > 999 ? 999 : idx);
                int curtype = StarMegaStructureType[idx];
                if (curtype == 0)
                {
                    if (!GameMain.history.TechUnlocked(1802))
                    {
                        RightDysonTitle.text = "???".Translate() + " " + star.displayName;
                        set2DysonButtonTextTrans.GetComponent<Text>().text = "当前".TranslateFromJson() + " " + "???".Translate();
                        RightMaxPowGenText.text = "???".Translate();
                    }
                    else if (!GameMain.history.TechUnlocked(1952))
                    {
                        RightDysonTitle.text = "共鸣阵列".TranslateFromJson() + " " + star.displayName;
                        set2DysonButtonTextTrans.GetComponent<Text>().text = "当前".TranslateFromJson() + " " + "共鸣阵列".TranslateFromJson();
                        RightMaxPowGenText.text = "引力共鸣".TranslateFromJson();
                    }
                    else
                    {
                        RightDysonTitle.text = "数学率引擎".TranslateFromJson() + " " + star.displayName;
                        set2DysonButtonTextTrans.GetComponent<Text>().text = "当前".TranslateFromJson() + " " + "数学率引擎".TranslateFromJson();
                        RightMaxPowGenText.text = "现实重构".TranslateFromJson();
                    }
                }
            }
        }

        public static void UIValueUpdate_Postfix()
        {
            Type targetType = AccessTools.TypeByName("MoreMegaStructure.MoreMegaStructure");
            if (targetType == null) return;

            // 2. 获取字段引用
            FieldInfo curDysonSphereField = AccessTools.Field(targetType, "curDysonSphere");
            if (curDysonSphereField == null) return;
            DysonSphere curDysonSphere = (DysonSphere)curDysonSphereField.GetValue(null);

            FieldInfo StarMegaStructureTypeField = AccessTools.Field(targetType, "StarMegaStructureType");
            if (StarMegaStructureTypeField == null) return;
            int[] StarMegaStructureType = (int[])StarMegaStructureTypeField.GetValue(null);

            FieldInfo RightMaxPowGenValueTextField = AccessTools.Field(targetType, "RightMaxPowGenValueText");
            if (RightMaxPowGenValueTextField == null) return;
            Text RightMaxPowGenValueText = (Text)RightMaxPowGenValueTextField.GetValue(null);

            if (StarMegaStructureType[curDysonSphere.starData.id - 1] == 0) //如果是数学率引擎
            {
                if (curDysonSphere.starData.type == EStarType.BlackHole) {
                    long DysonEnergy = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick);
                    if (!GameMain.history.TechUnlocked(1802)) {
                        RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy) + "?";
                    } else if (!GameMain.history.TechUnlocked(1952)) {
                        RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy) + " g";
                    } else if (!GameMain.history.TechUnlocked(1960)) {
                        RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy / 10000) + "休谟".TranslateFromJson();
                    } else {
                        long ThirdLevelEnergy = DysonEnergy - EnergyCalculate.SecondLevelEnergy;
                        double coefficient = ThirdLevelEnergy / EnergyCalculate.ThirdLevelRatio;
                        RightMaxPowGenValueText.text = Capacity2Str((long)((EnergyCalculate.SecondLevelEnergy / 10000) * coefficient)) + "休谟".TranslateFromJson();
                        //RightMaxPowGenValueText.text = coefficient.ToString() + "休谟";
                    }
                }
            }
        }


        public static void SetMegaStructure_Postfix(int type)
        {
            Type targetType = AccessTools.TypeByName("MoreMegaStructure.MoreMegaStructure");
            if (targetType == null) return;

            FieldInfo curStarField = AccessTools.Field(targetType, "curStar");
            if (curStarField == null) return;
            StarData curStar = (StarData)curStarField.GetValue(null);

            if (curStar.type == EStarType.BlackHole)
            {
                if (type == 0)
                { // 改成数学率引擎后删除所有太阳帆
                    curDysonSphere.swarm.RemoveSailsByOrbit(-1);
                }
            }
        }


        public static void SiloUpdatePatch_Postfix()
        {
            Type targetType = AccessTools.TypeByName("MoreMegaStructure.MoreMegaStructure");
            if (targetType == null) return;

            FieldInfo starIndexField = AccessTools.Field(targetType, "starIndex");
            if (starIndexField == null) return;
            int starIndex = (int)starIndexField.GetValue(null);
            
            FieldInfo StarMegaStructureTypeField = AccessTools.Field(targetType, "StarMegaStructureType");
            if (StarMegaStructureTypeField == null) return;
            int[] StarMegaStructureType = (int[])StarMegaStructureTypeField.GetValue(null);

            FieldInfo __instanceField = AccessTools.Field(targetType, "__instance");
            if (__instanceField == null) return;
            SiloComponent __instance = (SiloComponent)__instanceField.GetValue(null);

            if (GameMain.galaxy.stars[starIndex].type == EStarType.BlackHole)
            {
                if (StarMegaStructureType[starIndex] == 0)
                {
                    int bulletIdExpected = 1503;
                    if (!GameMain.history.TechUnlocked(1952))
                    {
                        bulletIdExpected = 6228;
                    }
                    else if (!GameMain.history.TechUnlocked(1960))
                    {
                        bulletIdExpected = 6504;
                    }
                    else
                    {
                        bulletIdExpected = 6502;
                    }
                    if (__instance.bulletId != bulletIdExpected)
                    {
                        __instance.bulletCount = 0;
                        __instance.bulletInc = 0;
                        __instance.bulletId = bulletIdExpected;
                    }
                }
            }
        }

        // 科学枢纽，在判断消耗物是黑糖就返回前，插入，如果科技IsLabTech为false，就直接返回
        public static IEnumerable<CodeInstruction> DysonSphereGameTickPostPatch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try {
                var matcher = new CodeMatcher(instructions);

                matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 5201));

                object techProto = matcher.Advance(-4).Operand;
                object returnLabel = matcher.Advance(9).Operand;

                matcher.Advance(-11).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, techProto),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TechProto), nameof(TechProto.IsLabTech))),
                    new CodeInstruction(OpCodes.Brfalse_S, returnLabel)
                );

                return matcher.InstructionEnumeration();
            } catch {
                // 如果后续巨构mod改了，任意匹配/偏移异常，原样返回IL，静默失败
                return instructions;
            }
        }

        public static IEnumerable<CodeInstruction> Import_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 20000.0));
            matcher.SetOperandAndAdvance(100000.0);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 100000));
            matcher.SetOperandAndAdvance(500000);

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> IntoOtherSave_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 20000.0));
            matcher.SetOperandAndAdvance(100000.0);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 100000));
            matcher.SetOperandAndAdvance(500000);

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> AddNewItems_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 1607));

            matcher.SetOperandAndAdvance(1963);

            return matcher.InstructionEnumeration();
        }
    }
}
