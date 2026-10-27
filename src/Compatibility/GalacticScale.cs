using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.AddVein.ModifyPlanetTheme;
using PluginInfo = BepInEx.PluginInfo;

// ReSharper disable InconsistentNaming

namespace ProjectOrbitalRing.Compatibility
{
    internal static class GalacticScale
    {
        internal const string GUID = "dsp.galactic-scale.2";

        private static readonly Harmony HarmonyPatch = new Harmony("ProjectOrbitalRing.Compatibility." + GUID);

        internal static void Awake()
        {
            if (!Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo pluginInfo)) return;

            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            foreach (FieldInfo fi in AccessTools.GetDeclaredFields(assembly.GetType("GalacticScale.Themes")))
            {
                ref GSTheme theme = ref AccessTools.StaticFieldRefAccess<GSTheme>(fi)();

                GSThemeModify.ModifyTheme(ref theme);
            }

            GSVeinType.saneVeinTypes["Graphite"] = EVeinType.Graphite;
            GSVeinType.saneVeinTypes["DeepMagma"] = EVeinType.DeepMagma;
            GSVeinType.saneVeinTypes["Radioactive"] = EVeinType.Radioactive;
            GSVeinType.saneVeinTypes["Water"] = EVeinType.Ice;
            GSVeinType.saneVeinTypes["Ice"] = EVeinType.Ice;
            GSVeinType.saneVeinTypes["IronPyrite"] = EVeinType.IronPyrite;

            GSVeinType.insaneVeinTypes[EVeinType.Graphite] = "Graphite";
            GSVeinType.insaneVeinTypes[EVeinType.DeepMagma] = "DeepMagma";
            GSVeinType.insaneVeinTypes[EVeinType.Radioactive] = "Radioactive";
            GSVeinType.insaneVeinTypes[EVeinType.Ice] = "Water";
            GSVeinType.insaneVeinTypes[EVeinType.IronPyrite] = "IronPyrite";

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("GalacticScale.GS2"), "SetPlanetTheme"), null,
                new HarmonyMethod(typeof(GalacticScale), nameof(SetPlanetTheme_Postfix)),
                new HarmonyMethod(typeof(GalacticScale), nameof(SetPlanetTheme_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("GalacticScale.GSTheme"), "ToProto"), null,
                new HarmonyMethod(typeof(GalacticScale), nameof(GSTheme_ToProto_Postfix)));

            HarmonyPatch.Patch(AccessTools.Method(assembly.GetType("GalacticScale.PatchOnPlayerMove_Fly"), "GameTick"),
                transpiler: new HarmonyMethod(typeof(GalacticScale), nameof(PlayerMove_Fly_GameTick_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.DetermineDivideLine)),
                prefix: new HarmonyMethod(typeof(GalacticScale), nameof(BuildTool_BlueprintCopy_DetermineDivideLine_Prefix)));

            PatchGenerator(assembly, "GalacticScale.Generators.GS2Generator2");
            PatchGenerator(assembly, "GalacticScale.Generators.GS2Generator3");

            Type VeinAlgorithms = assembly.GetType("GalacticScale.VeinAlgorithms");
            GalacticScaleVeins.BindGenerationMethods(VeinAlgorithms);

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "DisableVeins"), null, null,
                new HarmonyMethod(typeof(GalacticScale), nameof(DisableVeins_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "DistributeVeinTypes"),
                new HarmonyMethod(typeof(GalacticScale), nameof(DistributeVeinTypes_Prefix)));

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "GenBirthPoints"), null,
                new HarmonyMethod(typeof(GalacticScale), nameof(GenBirthPoints_Postfix)));

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "CalculateVectorsGS2"), null,
                new HarmonyMethod(typeof(GalacticScale), nameof(CalculateVectorsGS2_Postfix)),
                new HarmonyMethod(typeof(GalacticScale), nameof(OilLikeVeinType_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "AddVeinsToPlanetGS2"), null,
                new HarmonyMethod(typeof(GalacticScaleVeins), nameof(GalacticScaleVeins.EnsureGeneratedResources)),
                new HarmonyMethod(typeof(GalacticScale), nameof(OilLikeVeinType_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(VeinAlgorithms, "InitBirthVeinVectors"),
                postfix: new HarmonyMethod(typeof(GalacticScale), nameof(InitBirthVeinVectors_Postfix)));

            HarmonyPatch.Patch(AccessTools.PropertyGetter(assembly.GetType("GalacticScale.GS2MainSettings"), "VeinTips"), null, null,
                new HarmonyMethod(typeof(GalacticScale), nameof(GS2MainSettings_VeinTips_Getter_Transpiler)));

            HarmonyPatch.Patch(AccessTools.Method(typeof(GameData), nameof(GameData.Import)), null,
                new HarmonyMethod(typeof(GalacticScale), nameof(GameData_Import_Postfix)));

            MethodInfo OnPlanetDataSet7Prefix =
                AccessTools.Method(assembly.GetType("GalacticScale.PatchOnUIPlanetDetail"), "OnPlanetDataSet7Prefix");

            if (OnPlanetDataSet7Prefix != null)
            {
                HarmonyPatch.Patch(OnPlanetDataSet7Prefix, null, null,
                    new HarmonyMethod(typeof(GalacticScale), nameof(OnPlanetDataSet_Transpiler)));

                //HarmonyPatch.Patch(OnPlanetDataSet7Prefix, null, null,
                //    new HarmonyMethod(typeof(PlanetGasPatches), nameof(PlanetGasPatches.OnDataSet_ChangeWaterId_Transpiler)));

                HarmonyPatch.Patch(OnPlanetDataSet7Prefix, null, null,
                    new HarmonyMethod(typeof(GalacticScale), nameof(OnPlanetDataSet_ChangeVeinData_Transpiler)));
            }

            MethodInfo OnStarDataSet2 = AccessTools.Method(assembly.GetType("GalacticScale.PatchOnUIStarDetail"), "OnStarDataSet2");

            if (OnStarDataSet2 != null)
            {
                //HarmonyPatch.Patch(OnStarDataSet2, null, null,
                //    new HarmonyMethod(typeof(PlanetGasPatches), nameof(PlanetGasPatches.OnDataSet_ChangeWaterId_Transpiler)));

                //HarmonyPatch.Patch(OnStarDataSet2, null, null,
                //    new HarmonyMethod(typeof(PlanetGasPatches), nameof(PlanetGasPatches.PlanetGen_SetPlanetTheme_Transpiler)));

                //HarmonyPatch.Patch(OnStarDataSet2, null, null,
                //    new HarmonyMethod(typeof(PlanetGasPatches), nameof(PlanetGasPatches.OnStarDataSet_Transpiler)));

                HarmonyPatch.Patch(OnStarDataSet2, null, null,
                    new HarmonyMethod(typeof(GalacticScale), nameof(OnStarDataSet_ChangeVeinData_Transpiler)));
            }
            ProjectOrbitalRing.GalacticScale2Compatibility = true;
        }

        public static void BuildTool_BlueprintCopy_DetermineDivideLine_Prefix(BuildTool_BlueprintCopy __instance)
        {
            if (__instance == null || __instance.equatorWidth <= 0)
            {
                return;
            }

            int requiredLength = GetBlueprintReformBufferLength(__instance.equatorWidth);
            if (requiredLength == 0)
            {
                return;
            }

            requiredLength = Math.Max(requiredLength, Math.Max(__instance.selectedReformIds?.Length ?? 0, __instance.preSelectedReformIds?.Length ?? 0));
            bool changed = EnsureReformBuffer(ref __instance.selectedReformIds, ref __instance.selectedReformBuffer, requiredLength);
            changed |= EnsureReformBuffer(ref __instance.preSelectedReformIds, ref __instance.preSelectedReformBuffer, requiredLength);
            if (changed)
            {
                __instance.reformFlagIsDirty = true;
            }
        }

        private static int GetBlueprintReformBufferLength(int equatorWidth)
        {
            if (equatorWidth <= 0)
            {
                return 0;
            }

            // Divide-line scanning addresses a half-sphere bitmap with equatorWidth-bit rows.
            long requiredLength = (long)equatorWidth * equatorWidth / 2 / 32 + 1;
            return requiredLength <= int.MaxValue ? (int)requiredLength : 0;
        }

        private static bool EnsureReformBuffer(ref uint[] ids, ref ComputeBuffer buffer, int requiredLength)
        {
            if (requiredLength <= 0)
            {
                return false;
            }

            bool resized = false;
            if (ids == null) {
                ids = new uint[requiredLength];
                resized = true;
            } else if (ids.Length < requiredLength) {
                Array.Resize(ref ids, requiredLength);
                resized = true;
            }

            if (buffer == null || buffer.count < requiredLength) {
                buffer?.Dispose();
                buffer = new ComputeBuffer(requiredLength, 4);
                resized = true;
            }

            if (resized) {
                buffer.SetData((Array)ids);
            }
            return resized;
        }

        public static IEnumerable<CodeInstruction> PlayerMove_Fly_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Mecha), nameof(Mecha.thrusterLevel))),
                new CodeMatch(OpCodes.Ldc_I4_2));
            if (matcher.IsValid)
            {
                matcher.Advance(2).MatchForward(false, new CodeMatch(instruction =>
                        instruction.opcode == OpCodes.Blt || instruction.opcode == OpCodes.Blt_S ||
                        instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S));
            }
            if (!matcher.IsValid)
            {
                throw new InvalidOperationException("Unsupported GS sail transition.");
            }

            object skipSail = matcher.Operand;
            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldind_Ref),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SailNeedFuel), "CheckSailFuel")),
                new CodeInstruction(OpCodes.Brfalse, skipSail));
            return matcher.InstructionEnumeration();
        }

        private static void PatchGenerator(Assembly assembly, string typeName)
        {
            Type generatorType = assembly.GetType(typeName);
            MethodInfo selectPlanetThemes = AccessTools.Method(generatorType, "SelectPlanetThemes");
            if (selectPlanetThemes == null)
            {
                return;
            }

            GalacticScaleBirthSystem.PatchGenerator(HarmonyPatch, generatorType);
            HarmonyPatch.Patch(selectPlanetThemes, null,
                new HarmonyMethod(typeof(GalacticScale), nameof(SelectPlanetThemes_Postfix)),
                new HarmonyMethod(typeof(GalacticScaleGalaxy), nameof(GalacticScaleGalaxy.SelectPlanetThemes_Transpiler)));
        }

        public static IEnumerable<CodeInstruction> SetPlanetTheme_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.type))),
                new CodeMatch(OpCodes.Ldc_I4_5));
            matcher.Advance(-1);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);
            matcher.SetAndAdvance(OpCodes.Nop, null);

            return matcher.InstructionEnumeration();
        }

        public static void DistributeVeinTypes_Prefix(GSPlanet gsPlanet)
        {
            if (gsPlanet?.veinSettings?.VeinTypes == null)
            {
                return;
            }
            if (gsPlanet.Scale == 10f)
            {
                return;
            }

            if (gsPlanet.planetData?.star != null) {
                bool birthStar = GS2.GetGSStar(gsPlanet.planetData.star).Bodies.Contains(GSSettings.BirthPlanet);
                GalacticScaleVeins.InitializePlanetResources(gsPlanet, birthStar);
            }
        }

        public static void SelectPlanetThemes_Postfix(object __instance, GSStar star)
        {
            if (star?.Planets == null)
            {
                return;
            }

            FieldInfo birthStarField = AccessTools.Field(__instance.GetType(), "birthStar");
            FieldInfo birthPlanetField = AccessTools.Field(__instance.GetType(), "birthPlanet");
            if (birthStarField?.GetValue(__instance) == star)
            {
                GalacticScaleBirthSystem.ApplyThemes(star, birthPlanetField?.GetValue(__instance) as GSPlanet);
            }
        }

        private static bool IsOilLikeVeinType(EVeinType type)
        {
            return type == EVeinType.Oil || type == EVeinType.DeepMagma || type == EVeinType.Ice;
        }

        public static IEnumerable<CodeInstruction> OilLikeVeinType_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var codes = instructions.ToList();
            MethodInfo isOilLikeVeinType = AccessTools.Method(typeof(GalacticScale), nameof(IsOilLikeVeinType));
            IList<LocalVariableInfo> locals = __originalMethod.GetMethodBody().LocalVariables;
            int replacements = 0;

            for (var index = 1; index < codes.Count - 1; index++)
            {
                if (!LoadsOilVeinType(codes[index]) || !LoadsVeinType(codes[index - 1], locals))
                {
                    continue;
                }
                OpCode comparison = codes[index + 1].opcode;
                if (comparison == OpCodes.Ceq)
                {
                    codes[index + 1].opcode = OpCodes.Nop;
                    codes[index + 1].operand = null;
                }
                else if (comparison == OpCodes.Beq || comparison == OpCodes.Beq_S)
                {
                    codes[index + 1].opcode = OpCodes.Brtrue;
                }
                else if (comparison == OpCodes.Bne_Un || comparison == OpCodes.Bne_Un_S)
                {
                    codes[index + 1].opcode = OpCodes.Brfalse;
                }
                else
                {
                    continue;
                }

                codes[index].opcode = OpCodes.Call;
                codes[index].operand = isOilLikeVeinType;
                replacements++;
            }

            int expected = __originalMethod.Name == "CalculateVectorsGS2" ? 1 : 5;
            if (replacements != expected)
            {
                throw new InvalidOperationException("Unsupported GS oil comparisons in " + __originalMethod.Name +
                        ": expected " + expected + ", found " + replacements);
            }
            return codes;
        }

        private static bool LoadsVeinType(CodeInstruction instruction, IList<LocalVariableInfo> locals)
        {
            if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field)
            {
                return field.FieldType == typeof(EVeinType);
            }

            int index;
            if (instruction.opcode == OpCodes.Ldloc_0)
            {
                index = 0;
            }
            else if (instruction.opcode == OpCodes.Ldloc_1)
            {
                index = 1;
            }
            else if (instruction.opcode == OpCodes.Ldloc_2)
            {
                index = 2;
            }
            else if (instruction.opcode == OpCodes.Ldloc_3)
            {
                index = 3;
            }
            else if (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S)
            {
                index = instruction.operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(instruction.operand);
            }
            else
            {
                return false;
            }
            return index >= 0 && index < locals.Count && locals[index].LocalType == typeof(EVeinType);
        }

        private static bool LoadsOilVeinType(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_7)
            {
                return true;
            }
            if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int intValue)
            {
                return intValue == 7;
            }
            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte sbyteValue)
            {
                return sbyteValue == 7;
            }

            return false;
        }

        public static void CalculateVectorsGS2_Postfix(GSPlanet gsPlanet, List<GSVeinDescriptor> __result)
        {
            if (gsPlanet.planetData.id == GSSettings.BirthPlanetId)
            {
                __result.Add(new GSVeinDescriptor
                    {
                        count = 6,
                        position = gsPlanet.planetData.birthResourcePoint2,
                        rare = false,
                        type = EVeinType.Graphite,
                        richness = 0.1f,
                    });
            }
        }

        public static void GenBirthPoints_Postfix(GSPlanet gsPlanet)
        {
            VeinAlgorithms.random = new GS2.Random(GSSettings.Seed);
            PlanetData planetData = gsPlanet.planetData;
            double num1 = 85.0 / planetData.orbitalPeriod + planetData.orbitPhase / 360.0;
            var num2 = (int)(num1 + 0.1);
            double num3 = (num1 - num2) * (2.0 * Math.PI);
            double num4 = 85.0 / planetData.rotationPeriod + planetData.rotationPhase / 360.0;
            var num5 = (int)(num4 + 0.1);
            double angle = (num4 - num5) * 360.0;
            var v = new Vector3((float)Math.Cos(num3) * planetData.orbitRadius, 0.0f, (float)Math.Sin(num3) * planetData.orbitRadius);
            Vector3 position = Maths.QRotate(planetData.runtimeOrbitRotation, v);
            Pose pose;

            if (planetData.orbitAroundPlanet != null)
            {
                pose = planetData.orbitAroundPlanet.PredictPose(85.0);
                position.x += pose.position.x;
                position.y += pose.position.y;
                position.z += pose.position.z;
            }

            pose = new Pose(position, planetData.runtimeSystemRotation * Quaternion.AngleAxis((float)angle, Vector3.down));
            Vector3 vector3_1 = Maths.QInvRotateLF(pose.rotation, planetData.star.uPosition - (VectorLF3)pose.position * 40000.0);
            vector3_1.Normalize();
            var vector3_2 = Vector3.Cross(vector3_1, Vector3.up);
            Vector3 normalized1 = vector3_2.normalized;
            vector3_2 = Vector3.Cross(normalized1, vector3_1);
            Vector3 normalized2 = vector3_2.normalized;
            var num6 = 0;

            while (num6++ < 256)
            {
                float num7 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.5f;
                float num8 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.5f;
                Vector3 vector3_3 = vector3_1 + num7 * normalized1 + num8 * normalized2;
                vector3_3.Normalize();
                planetData.birthPoint = vector3_3 * (float)(planetData.realRadius + 0.20000000298023224 + 1.5800000429153442);
                vector3_2 = Vector3.Cross(vector3_3, Vector3.up);
                normalized1 = vector3_2.normalized;
                vector3_2 = Vector3.Cross(normalized1, vector3_3);
                normalized2 = vector3_2.normalized;
                var flag = false;

                for (var index = 0; index < 10; ++index)
                {
                    Vector2 vector2_1 = new Vector2((float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0),
                        (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0)).normalized * 0.1f;
                    Vector2 vector2_2 = AddVeinPatches.Rotate(vector2_1, 120);
                    float num9 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.006f;
                    float num10 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.006f;
                    vector2_2.x += num9;
                    vector2_2.y += num10;
                    Vector2 vector2_3 = AddVeinPatches.Rotate(vector2_1, 240);
                    float num51 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.006f;
                    float num61 = (float)(VeinAlgorithms.random.NextDouble() * 2.0 - 1.0) * 0.006f;
                    vector2_3.x += num51;
                    vector2_3.y += num61;
                    vector3_2 = vector3_3 + vector2_1.x * normalized1 + vector2_1.y * normalized2;
                    Vector3 normalized3 = vector3_2.normalized;
                    vector3_2 = vector3_3 + vector2_2.x * normalized1 + vector2_2.y * normalized2;
                    Vector3 normalized4 = vector3_2.normalized;
                    vector3_2 = vector3_3 + vector2_3.x * normalized1 + vector2_3.y * normalized2;
                    Vector3 normalized5 = vector3_2.normalized;
                    planetData.birthResourcePoint0 = normalized3.normalized;
                    planetData.birthResourcePoint1 = normalized4.normalized;
                    planetData.birthResourcePoint2 = normalized5.normalized;
                    float num11 = planetData.realRadius;

                    if (planetData.data.QueryHeight(vector3_3) > (double)num11 && planetData.data.QueryHeight(normalized3) > (double)num11
                                                                               && planetData.data.QueryHeight(normalized4) > (double)num11)
                    {
                        Vector3 vpos1 = normalized3 + normalized1 * 0.03f;
                        Vector3 vpos2 = normalized3 - normalized1 * 0.03f;
                        Vector3 vpos3 = normalized3 + normalized2 * 0.03f;
                        Vector3 vpos4 = normalized3 - normalized2 * 0.03f;
                        Vector3 vpos5 = normalized4 + normalized1 * 0.03f;
                        Vector3 vpos6 = normalized4 - normalized1 * 0.03f;
                        Vector3 vpos7 = normalized4 + normalized2 * 0.03f;
                        Vector3 vpos8 = normalized4 - normalized2 * 0.03f;
                        Vector3 vpos9 = normalized5 + normalized1 * 0.03f;
                        Vector3 vpos10 = normalized5 - normalized1 * 0.03f;
                        Vector3 vpos11 = normalized5 + normalized2 * 0.03f;
                        Vector3 vpos12 = normalized5 - normalized2 * 0.03f;

                        if (planetData.data.QueryHeight(vpos1) > (double)num11 && planetData.data.QueryHeight(vpos2) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos3) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos4) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos5) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos6) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos7) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos8) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos9) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos10) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos11) > (double)num11
                                                                               && planetData.data.QueryHeight(vpos12) > (double)num11)
                        {
                            flag = true;

                            break;
                        }
                    }
                }

                if (flag) break;
            }
        }

        public static void InitBirthVeinVectors_Postfix(GSPlanet gsPlanet)
        {
            PlanetData planetData = gsPlanet.planetData;
            gsPlanet.veinData.types[2] = EVeinType.Graphite;
            gsPlanet.veinData.vectors[2] = planetData.birthResourcePoint2;
            gsPlanet.veinData.count = 3;
        }

        public static void SetPlanetTheme_Postfix(PlanetData planet)
        {
            if (planet.type != EPlanetType.Gas)
            {
                planet.gasHeatValues = new float[planet.gasItems.Length];
                planet.gasTotalHeat = 0;
            }

            ApplyPlanetThemeData(planet);
        }

        public static void GSTheme_ToProto_Postfix(ThemeProto __result)
        {
            switch (__result.name)
            {
                case "Lava":
                    __result.WaterItemId = ProtoID.I岩浆;

                    break;

                case "SaltLake":
                    __result.oceanMat = LDB.themes.Select(8).oceanMat;

                    break;
            }
        }

        private static void ApplyPlanetThemeData(PlanetData planet)
        {
            if (planet == null || planet.type == EPlanetType.Gas)
            {
                return;
            }

            if (PlanetThemeUtils.GetVanillaThemeId(planet) == 9)
            {
                planet.waterItemId = ProtoID.I岩浆;
            }
        }

        public static void GameData_Import_Postfix(GameData __instance)
        {
            GalaxyData galaxy = __instance?.galaxy;
            if (galaxy?.stars == null)
            {
                return;
            }

            foreach (StarData star in galaxy.stars)
            {
                if (star?.planets == null)
                {
                    continue;
                }

                foreach (PlanetData planet in star.planets)
                {
                    ApplyPlanetThemeData(planet);
                }
            }
        }

        public static IEnumerable<CodeInstruction> GS2MainSettings_VeinTips_Getter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15));

            matcher.SetOperandAndAdvance(AddVeinPatches.VeinTypeCount);

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> DisableVeins_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)16));

            int veinTypeArrayLength = AddVeinPatches.VeinTypeCount + 1;

            matcher.SetOperandAndAdvance(veinTypeArrayLength);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)16));

            matcher.SetOperandAndAdvance(veinTypeArrayLength);

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> OnPlanetDataSet_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "实际采集速度"))
               .MatchForward(true, new CodeMatch(OpCodes.Nop), new CodeMatch(OpCodes.Br)).SetInstruction(new CodeInstruction(OpCodes.Nop));

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StationComponent), nameof(StationComponent.collectionPerTick))));

            //matcher.Advance(-2).InsertAndAdvance(new CodeInstruction(OpCodes.Call,
            //    AccessTools.Method(typeof(PlanetGasPatches), nameof(PlanetGasPatches.GetMiningSpeedScale))));

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> OnPlanetDataSet_ChangeVeinData_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_6), new CodeMatch(OpCodes.Clt));

            object index = matcher.Operand;
            object jump = matcher.Advance(5).Operand;
            object label = matcher.Advance(2).Labels.First();

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index), new CodeInstruction(OpCodes.Ldc_I4, 15),
                new CodeInstruction(OpCodes.Beq, label), new CodeInstruction(OpCodes.Ldc_I4, 14),
                new CodeInstruction(OpCodes.Stloc_S, index), new CodeInstruction(OpCodes.Br, jump));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15),
                new CodeMatch(OpCodes.Clt));

            index = matcher.Operand;

            object addLabel = matcher.Clone().MatchBack(false, new CodeMatch(OpCodes.Brfalse)).Operand;

            matcher.Advance(2).InsertAndAdvance(new CodeInstruction(OpCodes.Beq, addLabel), new CodeInstruction(OpCodes.Ldloc_S, index),
                new CodeInstruction(OpCodes.Ldc_I4_S, AddVeinPatches.VeinTypeCount));

            return matcher.InstructionEnumeration();
        }

        public static IEnumerable<CodeInstruction> OnStarDataSet_ChangeVeinData_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7), new CodeMatch(OpCodes.Clt));

            matcher.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(AddVeinPatches), nameof(AddVeinPatches.OnStarDataSet_ChangeVeinData_HighlightPatches))))
               .SetOpcodeAndAdvance(OpCodes.Nop);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7), new CodeMatch(OpCodes.Clt));

            matcher.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(AddVeinPatches), nameof(AddVeinPatches.OnStarDataSet_ChangeVeinData_HighlightPatches))))
               .SetOpcodeAndAdvance(OpCodes.Nop);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15),
                new CodeMatch(OpCodes.Clt));

            object index = matcher.Operand;

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(AddVeinPatches), nameof(AddVeinPatches.OnStarDataSet_ChangeVeinData_IndexPatches))),
                new CodeInstruction(OpCodes.Stloc_S, index));

            matcher.Advance(1).SetOperandAndAdvance(AddVeinPatches.VeinTypeCount);

            return matcher.InstructionEnumeration();
        }
    }

    internal static class GSThemeModify
    {
        internal static void ModifyTheme(ref GSTheme theme)
        {
            if (theme.PlanetType == EPlanetType.Gas) {
                GasGiantModify(ref theme.GasItems, ref theme.GasSpeeds);
            }
            else
            {
                ModifyThemeData(GetThemeId(theme), ref theme.VeinSpot, ref theme.VeinCount,
                    ref theme.VeinOpacity, ref theme.RareVeins, ref theme.RareSettings);

                switch (GetThemeId(theme))
                {
                    case 8:
                        theme.WaterItemId = ProtoID.I水;
                        theme.Distribute = EThemeDistribute.Interstellar;

                        break;

                    case 9:
                        theme.WaterItemId = ProtoID.I岩浆;
                        theme.Distribute = EThemeDistribute.Interstellar;
                        theme.PlanetType = EPlanetType.Vocano;

                        break;

                    case 10:
                        theme.WaterItemId = ProtoID.I甲烷;
                        theme.WaterHeight = -0.5f;
                        theme.Distribute = EThemeDistribute.Interstellar;

                        break;

                    case 13:
                        theme.Distribute = EThemeDistribute.Interstellar;
                        theme.PlanetType = EPlanetType.Vocano;

                        break;

                    case 17:
                        theme.WaterItemId = ProtoID.I原油;
                        theme.WaterHeight = 0f;
                        theme.Distribute = EThemeDistribute.Interstellar;
                        theme.Algo = 3;
                        Themes.OceanicJungle.InitMaterials();
                        theme.oceanMat = Themes.OceanicJungle.oceanMat;

                        break;

                    case 19:
                        theme.Wind = 8f;
                        theme.Distribute = EThemeDistribute.Default;

                        break;

                    case 25:
                        theme.Distribute = EThemeDistribute.Default;

                        break;
                }
            }
        }

        private static int GetThemeId(GSTheme theme)
        {
            int knownId = PlanetThemeUtils.ResolveGSThemeId(theme.Name, null);
            return knownId != 0 ? knownId : theme.LDBThemeId;
        }
    }
}
