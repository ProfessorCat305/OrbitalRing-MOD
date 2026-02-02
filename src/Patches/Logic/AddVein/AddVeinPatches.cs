using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CommonAPI;
using GalacticScale;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;

// ReSharper disable CommentTypo
// ReSharper disable InconsistentNaming
// ReSharper disable Unity.UnknownResource

namespace ProjectOrbitalRing.Patches.Logic.AddVein
{
    public static partial class AddVeinPatches
    {
        internal const sbyte VeinTypeCount = 20;

        private static readonly Color32[] VeinColors =
        {
            new Color(0.538f, 0.538f, 0.538f), // Default
            new Color(0.288f, 0.587f, 0.858f), // 铁 1
            new Color(1.000f, 0.490f, 0.307f), // 铜
            new Color(0.214f, 0.745f, 0.531f), // 硅
            new Color(1.000f, 1.000f, 1.000f), // 钛
            new Color(0.483f, 0.461f, 0.444f), // 石
            new Color(0.113f, 0.130f, 0.140f), // 煤
            new Color(0.000f, 0.000f, 0.000f), // 油
            new Color(1.000f, 1.000f, 1.000f), // 可燃冰 8
            new Color(0.489f, 0.601f, 0.745f), // 金伯利 9
            new Color(0.066f, 0.290f, 0.160f), // 莫桑石 10
            new Color(0.538f, 0.613f, 0.078f), // 有机 11
            new Color(0.575f, 0.270f, 0.830f), // 光栅 12
            new Color(0.571f, 0.708f, 0.647f), // 刺笋 13
            new Color(0.349f, 0.222f, 0.247f), // 单极 14
            new Color(1.000f, 1.000f, 1.000f), // 辉银矿 15
            new Color(0.538f, 0.538f, 0.538f), // 深层熔岩 16
            new Color(0.685f, 0.792f, 0.000f), // 放射 17
            new Color(0.965f, 0.867f, 0.352f), // 黄铁 18
            new Color(0.000f, 0.000f, 0.000f), // 冰 19
        };

        internal static void ModifyVeinData()
        {
            AddVeinProtos(
                NewVein(15, "辉银矿脉", "I辉银矿", "Assets/texpack/铝矿脉", ProtoID.I石墨矿, 34, 3, 60),
                NewVein(16, "深层熔岩", "I深层熔岩", "Icons/Vein/oil-vein", ProtoID.I深层熔岩, 0, 6, 60),
                NewVein(17, "铀矿脉", "I铀矿", "Assets/texpack/V铀矿脉", ProtoID.I放射性矿物, 35, 2, 90),
                NewVein(18, "黄铁矿脉", "I黄铁矿", "Assets/texpack/V黄铁矿脉", ProtoID.I黄铁矿, 36, 1, 90),
                NewVein(19, "地下冰层", "I地下冰层", "Icons/Vein/oil-vein", ProtoID.I水, 0, 6, 60));
            return;

            VeinProto NewVein(int id, string name, string description, string iconPath, int miningItem, int miningEffect, int modelIndex,
                int miningTime) =>
                new VeinProto
                {
                    ID = id,
                    Name = name,
                    Description = description,
                    IconPath = iconPath,
                    MiningItem = miningItem,
                    MiningEffect = miningEffect,
                    ModelIndex = modelIndex,
                    MiningTime = miningTime,
                    CircleRadius = 1,
                    MinerBaseModelIndex = 58,
                    MinerCircleModelIndex = 59,
                    MiningAudio = 122,
                    ModelCount = 1,
                };
        }

        private static void AddVeinProtos(params VeinProto[] protos)
        {
            VeinProtoSet veins = LDB.veins;

            int dataArrayLength = veins.dataArray.Length;

            Array.Resize(ref veins.dataArray, dataArrayLength + protos.Length);

            for (var index = 0; index < dataArrayLength; ++index)
            {
                if (veins.dataArray[index].Name == "分形硅矿")
                {
                    veins.dataArray[index].Name = "莫桑石矿";
                    veins.dataArray[index].Description = "I莫桑石";
                }
                Debug.LogFormat("veins id {0} {1}, MinerBaseModelIndex{2} MiningEffect{3}", index, veins.dataArray[index].Name, veins.dataArray[index].MinerBaseModelIndex, veins.dataArray[index].MiningEffect);
                Debug.LogFormat("MinerCircleModelIndex {0} ModelIndex{1} MiningAudio {2} ModelCount{3}", veins.dataArray[index].MinerCircleModelIndex, veins.dataArray[index].ModelIndex, veins.dataArray[index].MiningAudio, veins.dataArray[index].ModelCount);
                Debug.LogFormat("CircleRadius {0} MiningItem {1} MiningTime {2}", veins.dataArray[index].CircleRadius, veins.dataArray[index].MiningItem, veins.dataArray[index].MiningTime);
                Debug.LogFormat("IconPath {0}", veins.dataArray[index].IconPath);
            }

            for (var index = 0; index < protos.Length; ++index)
            {
                if (protos[index].ID == 16 || protos[index].ID == 19)
                {
                    protos[index].MinerBaseModelIndex = 0;
                    protos[index].MinerCircleModelIndex = 0;
                    protos[index].CircleRadius = 1.5f;
                }
                veins.dataArray[dataArrayLength + index] = protos[index];
            }

            veins.OnAfterDeserialize();
        }

        internal static void SetMinerMk2Color()
        {
            Texture texture = TextureHelper.GetTexture("矿机渲染索引");
            int veinColorTex = Shader.PropertyToID("_VeinColorTex");

            ref PrefabDesc prefabDesc = ref LDB.models.Select(256).prefabDesc;
            prefabDesc.materials[0].SetTexture(veinColorTex, texture);
            ref Material[] prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            prefabDescLODMaterial[0].SetTexture(veinColorTex, texture);
            prefabDescLODMaterial[1].SetTexture(veinColorTex, texture);
            prefabDescLODMaterial[2].SetTexture(veinColorTex, texture);

            prefabDesc = ref LDB.models.Select(59).prefabDesc;
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            prefabDescLODMaterial[1].SetTexture(veinColorTex, texture);
            prefabDescLODMaterial[2].SetTexture(veinColorTex, texture);
            prefabDescLODMaterial[3].SetTexture(veinColorTex, texture);
        }

        [HarmonyPatch(typeof(UISandboxMenu), nameof(UISandboxMenu.StaticLoad))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UISandboxMenu_StaticLoad_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(LDB), nameof(LDB.veins))), new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldlen));

            matcher.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)14))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop)).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop));

            return matcher.InstructionEnumeration();
        }


        [HarmonyPatch(typeof(UISandboxMenu), nameof(UISandboxMenu.StaticLoad))]
        [HarmonyPostfix]
        public static void UISandboxMenu_StaticLoad_Postfix(ref VeinProto[,] ___veinProtos)
        {
            ___veinProtos[1, 7] = LDB.veins.Select(15);
            ___veinProtos[1, 8] = LDB.veins.Select(16);
        }


        [HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm7), nameof(PlanetAlgorithm7.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm11), nameof(PlanetAlgorithm11.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm12), nameof(PlanetAlgorithm12.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm13), nameof(PlanetAlgorithm13.GenerateVeins))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetAlgorithm_GenerateVeins_ResizeVeinList_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15));
            matcher.SetOperandAndAdvance(20);

            return matcher.InstructionEnumeration();
        }

        
        //[HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm11), nameof(PlanetAlgorithm11.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm12), nameof(PlanetAlgorithm12.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm13), nameof(PlanetAlgorithm13.GenerateVeins))]
        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> PlanetAlgorithm_GenerateVeins_RemoveHeightLimit_Transpiler(
        //    IEnumerable<CodeInstruction> instructions)
        //{
        //    var matcher = new CodeMatcher(instructions);
        //    matcher.MatchForward(true, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.radius))),
        //        new CodeMatch
        //        {
        //            opcodes = new List<OpCode>
        //            {
        //                OpCodes.Blt, OpCodes.Blt_S,
        //            },
        //        });

        //    CodeMatcher matcher2 = matcher.Clone();
        //    matcher2.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Stloc_S));
        //    Label label = matcher2.Labels.First();

        //    matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Pop));
        //    matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Pop));
        //    matcher.SetAndAdvance(OpCodes.Br, label);

        //    matcher.MatchForward(false,
        //        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), nameof(PlanetData.waterItemId))));
        //    matcher.Advance(1).SetOpcodeAndAdvance(OpCodes.Br);

        //    return matcher.InstructionEnumeration();
        //}

        //[HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm7), nameof(PlanetAlgorithm7.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm11), nameof(PlanetAlgorithm11.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm12), nameof(PlanetAlgorithm12.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm13), nameof(PlanetAlgorithm13.GenerateVeins))]
        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> PlanetAlgorithm_GenerateVeins_RemoveVeinPositionBias_Transpiler(
        //    IEnumerable<CodeInstruction> instructions)
        //{
        //    var matcher = new CodeMatcher(instructions);

        //    matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));

        //    matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop)).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
        //       .SetOpcodeAndAdvance(OpCodes.Br_S);

        //    return matcher.InstructionEnumeration();
        //}


        [HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm11), nameof(PlanetAlgorithm11.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm12), nameof(PlanetAlgorithm12.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm13), nameof(PlanetAlgorithm13.GenerateVeins))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetAlgorithm_InitialVeins_Transpiler(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            var matcher = new CodeMatcher(instructions);

            Type type = original.DeclaringType;

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldc_I4_2),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(type, "veinVectorCount")));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddVeinPatches), nameof(InitBirthVeinVectors))));

            matcher.Advance(1).SetOpcodeAndAdvance(OpCodes.Ldc_I4_5);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_2));

            matcher.SetOpcodeAndAdvance(OpCodes.Ldc_I4_5);

            return matcher.InstructionEnumeration();
        }

        public static void InitBirthVeinVectors(PlanetAlgorithm algorithm)
        {
            //algorithm.veinVectorTypes[2] = EVeinType.Aluminum;
            //algorithm.veinVectors[2] = algorithm.planet.birthResourcePoint2;
            algorithm.veinVectorTypes[3] = EVeinType.Coal;
            algorithm.veinVectors[3] = algorithm.planet.birthResourcePoint3;
            algorithm.veinVectorTypes[4] = EVeinType.Stone;
            algorithm.veinVectors[4] = algorithm.planet.birthResourcePoint4;
        }


        [HarmonyPatch(typeof(PlanetData), nameof(PlanetData.GenBirthPoints), typeof(PlanetRawData), typeof(int))]
        [HarmonyPostfix]
        public static void PlanetData_GenBirthPoints_Postfix(PlanetData __instance, PlanetRawData rawData, int _birthSeed)
        {
            var dotNet35Random = new DotNet35Random(_birthSeed);
            Pose pose = __instance.PredictPose(85.0);
            Vector3 vector3 = Maths.QInvRotateLF(pose.rotation, __instance.star.uPosition - (VectorLF3)pose.position * 40000.0);
            vector3.Normalize();
            Vector3 x_direction = Vector3.Cross(vector3, Vector3.up).normalized;
            Vector3 y_direction = Vector3.Cross(x_direction, vector3).normalized;
            var num1 = 0;
            const int num2 = 512;

            for (; num1 < num2; ++num1)
            {
                float num3 = (float)(dotNet35Random.NextDouble() * 2.0 - 1.0) * 0.5f;
                float num4 = (float)(dotNet35Random.NextDouble() * 2.0 - 1.0) * 0.5f;
                Vector3 random = vector3 + num3 * x_direction + num4 * y_direction;
                random.Normalize();
                __instance.birthPoint = random * (float)(__instance.realRadius + 0.20000000298023224 + 1.4500000476837158);
                var tmpVector3 = Vector3.Cross(random, Vector3.up);
                x_direction = tmpVector3.normalized;
                tmpVector3 = Vector3.Cross(x_direction, random);
                y_direction = tmpVector3.normalized;

                for (var index = 0; index < 10; ++index)
                {
                    Vector2 rotate_0 = new Vector2((float)(dotNet35Random.NextDouble() * 2.0 - 1.0),
                        (float)(dotNet35Random.NextDouble() * 2.0 - 1.0)).normalized * 0.1f;
                    Vector2 rotate_1 = Rotate(rotate_0, 120);
                    Modify(dotNet35Random, ref rotate_1);
                    Vector2 rotate_2 = Rotate(rotate_0, 240);
                    Modify(dotNet35Random, ref rotate_2);
                    Vector2 rotate_3 = Rotate(rotate_0, 60);
                    Modify(dotNet35Random, ref rotate_3);

                    tmpVector3 = random + rotate_0.x * x_direction + rotate_0.y * y_direction;
                    __instance.birthResourcePoint0 = tmpVector3.normalized;

                    tmpVector3 = random + rotate_1.x * x_direction + rotate_1.y * y_direction;
                    __instance.birthResourcePoint1 = tmpVector3.normalized;

                    tmpVector3 = random + rotate_2.x * x_direction + rotate_2.y * y_direction;
                    __instance.birthResourcePoint2 = tmpVector3.normalized;

                    tmpVector3 = random + rotate_0.x * -2 * x_direction + rotate_0.y * -2 * y_direction;
                    __instance.birthResourcePoint3 = tmpVector3.normalized;

                    tmpVector3 = random + rotate_3.x * 2 * x_direction + rotate_3.y * 2 * y_direction;
                    __instance.birthResourcePoint4 = tmpVector3.normalized;

                    if (QueryHeightsNear(rawData, x_direction, y_direction, __instance.realRadius, random, __instance.birthResourcePoint0,
                        __instance.birthResourcePoint1, __instance.birthResourcePoint2, __instance.birthResourcePoint3,
                        __instance.birthResourcePoint4))
                        return;
                }
            }
        }


        internal static Vector2 Rotate(Vector2 v, float angle)
        {
            float delta = angle * Mathf.PI / 180;

            return new Vector2(v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta), v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta));
        }
        private static void Modify(DotNet35Random dotNet35Random, ref Vector2 vector)
        {
            vector.x += (float)(dotNet35Random.NextDouble() * 2.0 - 1.0) * 0.006f;
            vector.y += (float)(dotNet35Random.NextDouble() * 2.0 - 1.0) * 0.006f;
        }

        private static bool QueryHeights(PlanetRawData rawData, float radius, params Vector3[] points)
        {
            return points.All(point => rawData.QueryHeight(point) > radius);
        }

        private static bool QueryHeightsNear(PlanetRawData rawData, Vector3 x_vector, Vector3 y_vector, float radius,
            params Vector3[] points)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Vector3 point in points)
            {
                Vector3 pos1 = point + x_vector * 0.03f;
                Vector3 pos2 = point - x_vector * 0.03f;
                Vector3 pos3 = point + y_vector * 0.03f;
                Vector3 pos4 = point - y_vector * 0.03f;

                if (!QueryHeights(rawData, radius, point, pos1, pos2, pos3, pos4)) return false;
            }

            return true;
        }

        private static bool VeinTypeIsDeepMagmaOrIce(EVeinType type)
        {
            if (type == EVeinType.Oil || type == EVeinType.DeepMagma || type == EVeinType.Ice) return true;
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        public static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeMatch(OpCodes.Ldc_I4_7));

            object V_29 = matcher.Advance(-5).Operand;
            object V_31 = matcher.Advance(2).Operand;
            object IL_046F = matcher.Advance(5).Operand;
            Type veinDataType = typeof(VeinData);

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_29),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool._tmp_ids))),
                new CodeInstruction(OpCodes.Ldloc_S, V_31),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Ldelema, veinDataType),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16),
                new CodeInstruction(OpCodes.Beq, IL_046F),
                new CodeInstruction(OpCodes.Ldloc_S, V_29),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool._tmp_ids))),
                new CodeInstruction(OpCodes.Ldloc_S, V_31),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Ldelema, veinDataType),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19),
                new CodeInstruction(OpCodes.Beq, IL_046F)
            );

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeMatch(OpCodes.Ldc_I4_7));

            object V_34 = matcher.Advance(-5).Operand;
            object V_36 = matcher.Advance(2).Operand;
            object IL_05BA = matcher.Advance(5).Operand;

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_34),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool._tmp_ids))),
                new CodeInstruction(OpCodes.Ldloc_S, V_36),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Ldelema, veinDataType),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16),
                new CodeInstruction(OpCodes.Beq, IL_05BA),
                new CodeInstruction(OpCodes.Ldloc_S, V_34),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BuildTool), nameof(BuildTool._tmp_ids))),
                new CodeInstruction(OpCodes.Ldloc_S, V_36),
                new CodeInstruction(OpCodes.Ldelem_I4),
                new CodeInstruction(OpCodes.Ldelema, veinDataType),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19),
                new CodeInstruction(OpCodes.Beq, IL_05BA)
            );

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))),
                new CodeMatch(OpCodes.Ldc_I4_7));
            object IL_07E1 = matcher.Advance(2).Operand;

            matcher.Advance(-1).SetInstruction(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddVeinPatches), nameof(VeinTypeIsDeepMagmaOrIce)))
            );
            matcher.Advance(1).SetInstruction(new CodeInstruction(OpCodes.Brfalse_S, IL_07E1));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        public static void EliminateMoonTitaniumFloating(PlanetAlgorithm algorithm, ref int floating)
        {
            if (algorithm.planet.star.id == 1) {
                if (algorithm.planet.index == 2) {
                    floating = 0;
                }
            }
        }

        public static bool EliminateMoonTitaniumSpotFloating(PlanetAlgorithm algorithm, int eVeinType)
        {
            if (algorithm.planet.star.id == 1) {
                if (algorithm.planet.index == 2) {
                    if ((EVeinType)eVeinType == EVeinType.Titanium) {
                        return true;
                    }
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetAlgorithm_GenerateVeins_ForMoon_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Conv_U1));
            object V_36 = matcher.Advance(3).Operand;
            object IL_06EA = matcher.Advance(5).Operand;
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, V_36),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddVeinPatches), nameof(EliminateMoonTitaniumSpotFloating))),
                new CodeInstruction(OpCodes.Brtrue, IL_06EA)
            );

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 15000));

            object V_59 = matcher.Advance(7).Operand; // 变量索引

            //matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldc_I4_0));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca_S, V_59),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddVeinPatches), nameof(EliminateMoonTitaniumFloating),
                new System.Type[] {
                    typeof(PlanetAlgorithm),
                    typeof(int).MakeByRefType(),
                }
            )));
            return matcher.InstructionEnumeration();
        }

        public static void AddBirthGalaxyRareVein(PlanetAlgorithm algorithm, ref int[] array, ref float[] array2, ref float[] array3)
        {
            // 月球的矿物削减
            if (algorithm.planet.radius == 100f) {
                array3[1] = 0.24f;
                array3[2] = 0.24f;
                array3[3] = 0.36f;
                array3[4] = 0.36f;
                array3[5] = 0.6f;
                array3[15] = 0.12f;
                array3[8] = 0.24f;
                array3[12] = 0.2f;
                array3[18] = 0.04f;
                array3[19] = 0.12f;
            }
            //star id是1，star index是0
            if (algorithm.planet.star.id == 1) {
                if (algorithm.planet.index == 0) {
                    array[17] = 5;
                    array2[17] = 0.35f;
                    array3[17] = 0.5f;
                }
                if (algorithm.planet.index == 2) {
                    array[4] = 3;
                    //array2[4] = 0.6f;
                    array3[4] = 0.035f;
                    array[19] = 12;
                    array2[19] = 1.0f;
                    array3[19] = 1.0f;
                }
                if (algorithm.planet.index == 4) {
                    array[3] = 3;
                    array2[3] = 0.8f;
                    array3[3] = 1.2f;

                    array[8] = 7;
                    array2[8] = 0.4f;
                    array3[8] = 0.9f;

                    array[15] = 5;
                    array2[15] = 0.5f;
                    array3[15] = 0.5f;

                    array[17] = 4;
                    array2[17] = 0.2f;
                    array3[17] = 0.5f;

                    array[18] = 5;
                    array2[18] = 0.3f;
                    array3[18] = 0.5f;

                    array[19] = 12;
                    array2[19] = 1.0f;
                    array3[19] = 1.0f;
                }
            }
        }

        [HarmonyPatch(typeof(PlanetAlgorithm), nameof(PlanetAlgorithm.GenerateVeins))]
        //[HarmonyPatch(typeof(PlanetAlgorithm7), nameof(PlanetAlgorithm7.GenerateVeins))]  PlanetAlgorithm7的OIL特判只有7处，其他都是8处
        [HarmonyPatch(typeof(PlanetAlgorithm11), nameof(PlanetAlgorithm11.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm12), nameof(PlanetAlgorithm12.GenerateVeins))]
        [HarmonyPatch(typeof(PlanetAlgorithm13), nameof(PlanetAlgorithm13.GenerateVeins))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetAlgorithm_GenerateVeins_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Newarr));

            object V_11 = matcher.Advance(1).Operand; // 变量索引
            object V_12 = matcher.Advance(5).Operand; // 变量索引
            object V_13 = matcher.Advance(4).Operand; // 变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldc_I4_0));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca_S, V_11),
                new CodeInstruction(OpCodes.Ldloca_S, V_12),
                new CodeInstruction(OpCodes.Ldloca_S, V_13),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddVeinPatches), nameof(AddBirthGalaxyRareVein),
                new System.Type[] {
                    typeof(PlanetAlgorithm),
                    typeof(int[]).MakeByRefType(),    // ref int[]
                    typeof(float[]).MakeByRefType(),  // ref float[]
                    typeof(float[]).MakeByRefType()   // ref float[])));
                }
            )));



            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            object index = matcher.Operand;
            object jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(3).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(3).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PlanetAlgorithm7), nameof(PlanetAlgorithm7.GenerateVeins))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetAlgorithm7_GenerateVeins_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            object index = matcher.Operand;
            object jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index)).InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(2).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));


            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(3).Operand;
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19)).InsertAndAdvance(new CodeInstruction(OpCodes.Beq_S, jumpTo));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))), new CodeMatch(OpCodes.Ldc_I4_7));
            index = matcher.Operand;
            jumpTo = matcher.Advance(3).Operand;

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ceq));
            matcher
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)16))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, index))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VeinData), nameof(VeinData.type))))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)19))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Ceq)).InsertAndAdvance(new CodeInstruction(OpCodes.Or))
               .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, jumpTo));

            return matcher.InstructionEnumeration();
        }

    }
}
