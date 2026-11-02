using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using WinAPI;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    public static class OrbitalConnect
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        public static void BuildTool_Path_DeterminePreviews_PrePatch(BuildTool_Path __instance)
        {
            int handItem = __instance.handItem.ID;
            if (StructuralTrackPolicy.Enabled && TryGetTrackPlacement(handItem, out int side, out int altitude))
            {
                __instance.altitude = altitude;
                var layout = GetLayout(__instance.planet);
                float radius = __instance.planet.realRadius + 0.2f + __instance.altitude * 1.3333333f;
                Vector3 anchor = Vector3.zero;
                if (__instance.controller.cmd.stage == 1)
                {
                    Vector3 target = __instance.startTarget;
                    if (StructuralTrackPolicy.TryReadObject(__instance.factory, __instance.startObjectId,
                        out int itemId, out Vector3 position) && itemId == handItem)
                    {
                        target = position;
                    }
                    anchor = SnapTrackTarget(target, Vector3.zero, side, layout, radius);
                    __instance.startTarget = anchor;
                }
                __instance.cursorTarget = SnapTrackTarget(__instance.cursorTarget, anchor, side, layout, radius);
            }
            else if (handItem == ProtoID.I空轨 && __instance.altitude == 0)
            {
                __instance.altitude = 17;
            }
        }

        private static Vector3 SnapTrackTarget(Vector3 position, Vector3 anchor, int side,
            OrbitalRingLayout layout, float radius)
        {
            if (position.sqrMagnitude < 0.000001f)
            {
                return position;
            }
            Vector3 snapped = BeltShouldBeAdsorb(position, anchor, side, layout);
            if (isBeltBuildingPosYCorrect(snapped, side, layout) < 0)
            {
                return position;
            }
            // Native endpoint handling receives the start and cursor targets at the final ring latitude.
            return snapped.normalized * radius;
        }

        // Path-tool transpilers can use the native sampling call as an anchor.
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.DeterminePreviews))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_DeterminePreviews_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var nativeSampler = AccessTools.Method(typeof(PlanetAuxData), nameof(PlanetAuxData.SnapLineNonAlloc));
            var trackSampler = AccessTools.Method(typeof(OrbitalConnect), nameof(SampleTrackPath));
            foreach (CodeInstruction instruction in instructions)
            {
                if (!instruction.Calls(nativeSampler))
                {
                    yield return instruction;
                    continue;
                }
                var loadTool = new CodeInstruction(OpCodes.Ldarg_0);
                loadTool.labels.AddRange(instruction.labels);
                instruction.labels.Clear();
                yield return loadTool;
                instruction.opcode = OpCodes.Call;
                instruction.operand = trackSampler;
                yield return instruction;
            }
        }

        private static int SampleTrackPath(PlanetAuxData planetAux, Vector3 begin, Vector3 end, int path,
            bool geodesic, bool beginFlat, Vector3[] snaps, bool forceVertical, ref float maxGridSlope,
            bool useOldPath, BuildTool_Path tool)
        {
            if (!StructuralTrackPolicy.Enabled || !StructuralTrackPolicy.IsTrackItem(tool.handItem.ID))
            {
                return planetAux.SnapLineNonAlloc(begin, end, path, geodesic, beginFlat, snaps,
                    forceVertical, ref maxGridSlope, useOldPath);
            }
            int capacity = Math.Max(0, snaps.Length - 10);
            if (capacity == 0)
            {
                return 0;
            }
            Array.Clear(snaps, 0, snaps.Length);
            maxGridSlope = 0;
            var layout = GetLayout(tool.planet);
            double longitudeStep = layout.AngleUnit * (layout.HasLowerStationRing ? 1d : 2d);
            double startLongitude = Math.Atan2(begin.x, begin.z);
            double endLongitude = Math.Atan2(end.x, end.z);
            double longitudeDelta = Math.Atan2(Math.Sin(endLongitude - startLongitude),
                Math.Cos(endLongitude - startLongitude));
            if (longitudeDelta == 0d || capacity == 1)
            {
                snaps[0] = begin;
                return 1;
            }
            int segmentCount = Mathf.RoundToInt((float)(Math.Abs(longitudeDelta) * 180d / Math.PI / longitudeStep));
            segmentCount = Math.Min(Math.Max(1, segmentCount), capacity - 1);

            // Sampling longitude at one latitude keeps neighboring ring nodes evenly spaced.
            double radius = tool.planet.realRadius + 0.2d + tool.altitude * 1.3333333d;
            double height = begin.normalized.y * radius;
            double horizontalRadius = Math.Sqrt(Math.Max(0d, radius * radius - height * height));
            for (int index = 0; index <= segmentCount; index++)
            {
                double longitude = startLongitude + longitudeDelta * index / segmentCount;
                snaps[index] = new Vector3((float)(Math.Sin(longitude) * horizontalRadius),
                    (float)height, (float)(Math.Cos(longitude) * horizontalRadius));
            }
            return segmentCount + 1;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        public static void BuildTool_Path_DeterminePreviews_PostPatch(BuildTool_Path __instance)
        {
            int handItem = __instance.handItem.ID;
            if (!TryGetTrackPlacement(handItem, out int side, out int altitude))
            {
                if (handItem == ProtoID.I空轨)
                {
                    __instance.altitude = Math.Max(16, __instance.altitude);
                }
                else if (handItem == 2001 || handItem == 2002 || handItem == 2003)
                {
                    __instance.altitude = Math.Min(40, __instance.altitude);
                }
                return;
            }

            __instance.altitude = altitude;
            var layout = GetLayout(__instance.planet);
            float planetRadius = __instance.planet.realRadius;
            float trackRadius = planetRadius + 0.2f + altitude * 1.3333333f;
            Vector3 anchor = Vector3.zero;
            if (__instance.controller.cmd.stage == 1 && __instance.startTarget.sqrMagnitude > 0)
            {
                Vector3 start = BeltShouldBeAdsorb(__instance.startTarget, Vector3.zero, side, layout);
                if (isBeltBuildingPosYCorrect(start, side, layout) >= 0)
                {
                    anchor = start;
                }
            }

            var previews = __instance.buildPreviews;
            bool structural = StructuralTrackPolicy.Enabled;
            foreach (BuildPreview preview in previews)
            {
                if (!TryGetTrackPlacement(preview.item?.ID ?? 0, out int previewSide, out _))
                {
                    continue;
                }
                Vector3 snapped = BeltShouldBeAdsorb(preview.lpos, anchor, previewSide, layout);
                float magnitude = snapped.magnitude;
                bool useTrackRadius = magnitude > 0 && (structural
                    ? isBeltBuildingPosYCorrect(snapped, previewSide, layout) >= 0
                    : magnitude - planetRadius <= 40);
                preview.lpos = useTrackRadius ? snapped.normalized * trackRadius : snapped;
                preview.lpos2 = preview.lpos;
                if (structural)
                {
                    preview.tilt = 0;
                    preview.lrot = TrackRotation(preview.lpos, preview.lrot, Vector3.zero);
                    preview.lrot2 = preview.lrot;
                }
            }

            if (!structural)
            {
                AlignDirectedPreviews(previews);
            }
        }

        private static void AlignDirectedPreviews(List<BuildPreview> previews)
        {
            for (int index = 0; index < previews.Count; index++)
            {
                BuildPreview preview = previews[index];
                int itemId = preview.item?.ID ?? 0;
                if (!StructuralTrackPolicy.IsTrackItem(itemId))
                {
                    continue;
                }

                Vector3 forward = preview.lrot * Vector3.forward;
                if (preview.output?.item?.ID == itemId)
                {
                    forward = preview.output.lpos - preview.lpos;
                }
                else if (preview.input?.item?.ID == itemId)
                {
                    forward = preview.lpos - preview.input.lpos;
                }
                else if (previews.Count > 1)
                {
                    int neighborIndex = index + 1 < previews.Count ? index + 1 : index - 1;
                    Vector3 span = previews[neighborIndex].lpos - preview.lpos;
                    forward = Vector3.Dot(forward, span) < 0 ? -span : span;
                }

                preview.lrot = TrackRotation(preview.lpos, preview.lrot, forward);
                preview.lrot2 = preview.lrot;
            }
        }

        public static bool isOrbitalBelt(BuildPreview preview)
        {
            int itemId = preview?.item?.ID ?? 0;
            return itemId == ProtoID.I空轨 || StructuralTrackPolicy.IsTrackItem(itemId);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        public static IEnumerable<CodeInstruction> BuildTool_BlueprintPaste_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            // 跳过蓝图轨道连接组件太高不造的判定
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Mul)
                );
            object IL_0ABA = matcher.Advance(3).Operand;
            object V_9 = matcher.Advance(1).Operand;

            matcher.Advance(-6).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, V_9),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt))),
                new CodeInstruction(OpCodes.Brtrue_S, IL_0ABA)
            );
            // ===============
            // 跳过蓝图轨道设施碰撞检测的判定
            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))));
            object V_78 = matcher.Advance(1).Operand; // prefabDesc2变量索引
            CodeMatcher matcher2 = matcher.Clone();
            matcher2.MatchForward(false, new CodeMatch(OpCodes.Brtrue));
            object IL_1A89 = matcher2.Operand; // prefabDesc2变量索引

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_78),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1A89)
            );

            matcher.MatchForward(false, new CodeMatch(OpCodes.Brfalse), new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))));
            object IL_1CE3 = matcher.Operand;
            object V_116 = matcher.Advance(4).Operand; // prefabDesc2变量索引


            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_9),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1CE3)
            );

            //matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Call),
            //    new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_0));
            //object IL_1E57 = matcher.Advance(5).Operand;


            //matcher.Advance(-2).InsertAndAdvance(
            //    new CodeInstruction(OpCodes.Ldloc_S, V_9),
            //    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt)
            //    )),
            //    new CodeInstruction(OpCodes.Brtrue_S, IL_1E57)
            //);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetData), nameof(PlanetData.realRadius))),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Bge_Un));
            object IL_354F = matcher.Advance(3).Operand;
            //object V_116 = matcher.Advance(4).Operand; // prefabDesc2变量索引


            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_116),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_354F)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        public static bool isCollideShouldIgnore(PrefabDesc prefabDesc2)
        {
            int modelindex = prefabDesc2.modelIndex;
            switch (modelindex) {
                case ProtoID.M轨道熔炼站:
                case ProtoID.M太空物流港:
                case ProtoID.M天枢座:
                case ProtoID.M太空船坞:
                case ProtoID.M深空物流港:
                case ProtoID.M轨道反物质堆基座:
                case ProtoID.M轨道观测站:
                case ProtoID.M星环对撞机:
                case ProtoID.M轨道水培舱:
                    return true;
            }
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Br), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Brfalse));
            object label = matcher.Advance(3).Operand;
            //matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            matcher.Advance(12).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            //matcher.Advance(10);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Add), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Callvirt));

            object label1 = matcher.Advance(11).Operand;

            matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label1));
            // 上面两个修改解除传送带的高度受科技限制，为了适配小塔刚解锁时17层接口就可以用和轨道连接组件

            // 下面修改是为了让轨道熔炼站等同步轨道设施不参与碰撞检测
            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))),
                new CodeMatch(OpCodes.Stloc_S));
            object V_64 = matcher.Advance(1).Operand; // prefabDesc2变量索引
            //CodeMatcher matcher2 = matcher.Clone();
            //matcher2.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetObjectPose))));
            //object V_67 = matcher2.Advance(3).Operand; // prefabDesc2变量索引

            object IL_1001 = matcher.Advance(5).Operand; // IL_1001变量索引

            matcher.Advance(-4);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_64),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                //new System.Type[] {
                //    typeof(PrefabDesc),
                //}
                )),
                //new CodeInstruction(OpCodes.Stloc_S, V_67),
                //new CodeInstruction(OpCodes.Ldloc_S, V_67),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1001)
            );


            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        private static IEnumerable<CodeInstruction> PreserveTrackPathAltitude(IEnumerable<CodeInstruction> instructions)
        {
            // 碰撞查询可由其它模组包装；球形和胶囊查询共用自动升降入口。
            var entry = new[] {
                new CodeMatch(instruction => instruction.IsStloc()),
                new CodeMatch(instruction => instruction.IsLdloc()),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Brtrue ||
                    instruction.opcode == OpCodes.Brtrue_S),
                new CodeMatch(instruction => instruction.IsLdloc()),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Brtrue ||
                    instruction.opcode == OpCodes.Brtrue_S),
                new CodeMatch(instruction => instruction.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ble ||
                    instruction.opcode == OpCodes.Ble_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(instruction => instruction.IsStloc()),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(instruction => instruction.IsStloc())
            };
            var matcher = new CodeMatcher(instructions).MatchForward(false, entry);
            if (matcher.IsInvalid ||
                PathLocalIndex(matcher.InstructionAt(0)) != PathLocalIndex(matcher.InstructionAt(5)))
            {
                throw new InvalidOperationException(
                    "BuildTool_Path.CheckBuildConditions automatic altitude adjustment block has changed.");
            }
            if (matcher.Clone().Advance(1).MatchForward(false, entry).IsValid)
            {
                throw new InvalidOperationException(
                    "BuildTool_Path.CheckBuildConditions automatic altitude adjustment block is ambiguous.");
            }

            var firstExit = (Label)matcher.InstructionAt(2).operand;
            var secondExit = (Label)matcher.InstructionAt(4).operand;
            var overlapExit = (Label)matcher.InstructionAt(7).operand;
            bool hasCommonExit = false;
            foreach (CodeInstruction instruction in matcher.InstructionEnumeration())
            {
                if (instruction.labels.Contains(firstExit) && instruction.labels.Contains(secondExit) &&
                    instruction.labels.Contains(overlapExit))
                {
                    hasCommonExit = true;
                    break;
                }
            }
            if (!hasCommonExit)
            {
                throw new InvalidOperationException(
                    "BuildTool_Path.CheckBuildConditions altitude adjustment branches have different targets.");
            }

            matcher.Advance(1);
            var loadTool = new CodeInstruction(OpCodes.Ldarg_0);
            loadTool.MoveLabelsFrom(matcher.Instruction);
            loadTool.MoveBlocksFrom(matcher.Instruction);
            matcher.InsertAndAdvance(loadTool,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(CanAutoLift))),
                new CodeInstruction(OpCodes.Brfalse, firstExit));
            return matcher.InstructionEnumeration();
        }

        private static int PathLocalIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Stloc_0) return 0;
            if (instruction.opcode == OpCodes.Ldloc_1 || instruction.opcode == OpCodes.Stloc_1) return 1;
            if (instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Stloc_2) return 2;
            if (instruction.opcode == OpCodes.Ldloc_3 || instruction.opcode == OpCodes.Stloc_3) return 3;
            return instruction.operand is LocalVariableInfo local ? local.LocalIndex : Convert.ToInt32(instruction.operand);
        }

        private static bool CanAutoLift(BuildTool_Path tool) =>
            !StructuralTrackPolicy.Enabled || !StructuralTrackPolicy.IsTrackItem(tool.handItem.ID);

        public static bool CheckBeltId(BuildTool_Path instance, ref int num2)
        {
            int modelIndex = instance.GetPrefabDesc(num2).modelIndex;
            if (instance.handItem.ID == ProtoID.I轨道连接组件) {
                if (modelIndex != ProtoID.M轨道连接组件) {
                    num2 = 0;
                    return true;
                }
            } else if (instance.handItem.ID == ProtoID.I粒子加速轨道) {
                if (modelIndex != ProtoID.M粒子加速轨道) {
                    num2 = 0;
                    return true;
                }
            } else if (instance.handItem.ID == ProtoID.I星环电网组件) {
                if (modelIndex != ProtoID.M星环电网组件) {
                    num2 = 0;
                    return true;
                }
            }
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_UpdateRaycast_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelema), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Br));
            object IL_0451 = matcher.Advance(-14).Operand;
            object V_10 = matcher.Advance(16).Operand;
            //matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca_S, V_10),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(CheckBeltId),
                    new System.Type[] {
                        typeof(BuildTool_Path),
                        typeof(int).MakeByRefType(),
                    }
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_0451)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(BuildTool_Path), "UpdateRaycast")]
        //public static void UpdateRaycastPatch(BuildTool_Path __instance)
        //{
        //    bool flag = false;
        //    if (__instance.castObject == false) {
        //        return;
        //    }
        //    if (__instance.handItem.ID == ProtoID.I轨道连接组件) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M轨道连接组件) {
        //            flag = true;
        //        }
        //    } else if (__instance.handItem.ID == ProtoID.I粒子加速轨道) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M粒子加速轨道) {
        //            flag = true;
        //        }
        //    } else if (__instance.handItem.ID == ProtoID.I星环电网组件) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M星环电网组件) {
        //            flag = true;
        //        }
        //    }
        //    if (flag) {
        //        __instance.castObject = false;
        //        __instance.castObjectId = 0;
        //        __instance.castObjectPos = Vector3.zero;
        //    }
        //}

        //[HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        //[HarmonyPostfix]
        //public static void SetStationStorage_Patch(ref PlanetTransport __instance)
        //{
        //    OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
        //    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
        //    LogError($" 0 ring not complete inside {planetOrbitalRingData.Rings[0].isInsideRingComplete} outside {planetOrbitalRingData.Rings[0].isOutsideRingComplete}");
        //    LogError($" 0 ring not complete low inside {planetOrbitalRingData.Rings[0].isLowInsideRingComplete} outside {planetOrbitalRingData.Rings[0].isLowOutsideRingComplete}");
        //    if (__instance.planet.radius > 100f) {
        //        LogError($" 1 ring not complete inside {planetOrbitalRingData.Rings[1].isInsideRingComplete} outside {planetOrbitalRingData.Rings[1].isOutsideRingComplete}");
        //        LogError($" 1 ring not complete low inside {planetOrbitalRingData.Rings[1].isLowInsideRingComplete} outside {planetOrbitalRingData.Rings[1].isLowOutsideRingComplete}");
        //    }
        //    //planetOrbitalRingData.Rings[0].CheckRingComplete(true);
        //    //planetOrbitalRingData.Rings[1].CheckRingComplete(true);
        //}



        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.BuildFinally))]
        //[HarmonyPrefix]
        //public static void BuildFinallyPrePatch(ref PlanetFactory __instance, int prebuildId)
        //{
        //    if (prebuildId != 0) {
        //        PrebuildData prebuildData = __instance.prebuildPool[prebuildId];
        //        if (prebuildData.id == prebuildId) {
        //            if (prebuildData.protoId == ProtoID.I轨道连接组件 || prebuildData.protoId == ProtoID.I粒子加速轨道 || prebuildData.protoId == ProtoID.I星环电网组件) {
        //                //LogError($"BuildFinallyPostPatch");
        //                Vector3 pos = prebuildData.pos;
        //                (int positionIndex, int ringBeltIndex, int ringIndex) = CalculateRingPosMark(pos, __instance.planet.radius == 100f);
        //                OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
        //                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
        //                if (prebuildData.protoId == ProtoID.I轨道连接组件) {
        //                    planetOrbitalRingData.Rings[ringIndex].AddRing(positionIndex, ringBeltIndex, false);
        //                } else if (prebuildData.protoId == ProtoID.I粒子加速轨道 || prebuildData.protoId == ProtoID.I星环电网组件) {
        //                    planetOrbitalRingData.Rings[ringIndex].AddRing(positionIndex, ringBeltIndex, true);
        //                }
        //            }
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.OnBeltBuilt))]
        [HarmonyPostfix]
        public static void OnBeltBuiltPostPatch(ref PlanetFactory __instance, int beltEntityId)
        {
            int beltId = __instance.entityPool[beltEntityId].beltId;
            if (beltId == 0) {
                return;
            }
            int itemId = __instance.entityPool[beltEntityId].protoId;
            if (!StructuralTrackPolicy.IsTrackItem(itemId)) {
                return;
            }
            (int ringBeltIndex, int ringIndex) = CalculateRingPosMark(__instance.entityPool[beltEntityId].pos, GetLayout(__instance.planet));
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            planetOrbitalRingData.Rings[ringIndex].CheckRingIsComplete(__instance, beltId, ringBeltIndex, itemId);
        }

        // 拆除/击毁函数移动到OrbitalBuild里，待删除
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.DismantleFinally))]
        //[HarmonyPrefix]
        //public static void DismantleFinallyPatch(PlanetFactory __instance, int objId, ref int protoId)
        //{
        //    if (objId > 0) {
        //        if (protoId == ProtoID.I轨道连接组件 || protoId == ProtoID.I粒子加速轨道) {
        //            Vector3 thisPos = __instance.entityPool[objId].pos;
        //            (int positionIndex, int ringBeltIndex, int ringIndex) = CalculateRingPosMark(thisPos, __instance.planet.radius == 100f);
        //            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
        //            if (protoId == ProtoID.I轨道连接组件) {
        //                planetOrbitalRingData.Rings[ringIndex].DelRing(positionIndex, ringBeltIndex, false);
        //            } else if (protoId == ProtoID.I粒子加速轨道) {
        //                planetOrbitalRingData.Rings[ringIndex].DelRing(positionIndex, ringBeltIndex, true);
        //            }
        //        }
        //    }
        //}

        // 范围拆除时过滤轨道连接组件，粒子加速轨道，星环电网组件，空轨，太空船坞，轨道熔炼站，天枢座，轨道反物质堆基座，轨道观测站，星环对撞机，轨道水培舱
        // 轨道反物质堆核心，重型电浆炮，重型电磁弹射器，星环护盾组件，伺服天穹组件，智能方尖碑，亿万械国，突触凝练机，欺骗型广播塔
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelem_I4), new CodeMatch(OpCodes.Call, AccessTools.Field(typeof(BuildTool), nameof(BuildTool.GetItemProto))));
            object V_14 = matcher.Advance(2).Operand;

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isStation))));
            object IL_06C2 = matcher.Advance(1).Operand;

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6515),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6516),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6517),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6235),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6257),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6501),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6506),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6259),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6265),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6273),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6264),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6261),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 3004),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6513),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6518),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 7619),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 7620),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 7621),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 7803),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6533),
                new CodeInstruction(OpCodes.Beq, IL_06C2)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }
    }
}
