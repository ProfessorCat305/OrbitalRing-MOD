using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    [HarmonyPatch]
    internal static class AirBeltAppearance
    {
        private const int NativeBeltBatchCount = 12;
        private const int NativePathBatchCount = 2;
        private const int FastBeltBatchStart = 8;
        private const int AirBeltBatchCount = 4;
        private const int NativeRenderSpeed = 5;
        private static readonly ConditionalWeakTable<CargoTraffic, AirBeltBatches> airBatches =
            new ConditionalWeakTable<CargoTraffic, AirBeltBatches>();
        private static readonly AccessTools.FieldRef<CargoTraffic, BeltRenderingBatch[]> beltBatches =
            AccessTools.FieldRefAccess<CargoTraffic, BeltRenderingBatch[]>("beltRenderingBatch");
        private static readonly AccessTools.FieldRef<CargoTraffic, PathRenderingBatch[]> pathBatches =
            AccessTools.FieldRefAccess<CargoTraffic, PathRenderingBatch[]>("pathRenderingBatch");
        private static readonly AccessTools.FieldRef<BeltRenderingBatch, Material> beltMaterial =
            AccessTools.FieldRefAccess<BeltRenderingBatch, Material>("beltMat");
        private static readonly AccessTools.FieldRef<PathRenderingBatch, Material> pathMaterial =
            AccessTools.FieldRefAccess<PathRenderingBatch, Material>("pathMat");

        [HarmonyPrepare]
        private static bool Enabled() => RemodelSettings.AirRail;

        private interface IAirRailBatch
        {
            Mesh RailMesh { get; }
        }

        private sealed class AirRailBody : BeltRenderingBatch, IAirRailBatch
        {
            public Mesh RailMesh { get; }

            internal AirRailBody(int index, int anchorCount) : base(index, anchorCount)
            {
                RailMesh = AirBeltMesh.Beam(anchorCount);
                beltMaterial(this) = AirBeltMesh.BeamMaterial(Configs.builtin.beltMat[index]);
            }
        }

        private sealed class AirRailEnd : PathRenderingBatch, IAirRailBatch
        {
            public Mesh RailMesh { get; }

            internal AirRailEnd(int index) : base(index)
            {
                RailMesh = AirBeltMesh.Cap(index);
                pathMaterial(this) = AirBeltMesh.CapMaterial(Configs.builtin.pathMat[index]);
            }
        }

        private sealed class AirBeltBatches
        {
            public AirBeltBatches()
            {
            }

            internal int BeltStart = -1;
            internal int PathStart = -1;
        }

        internal static bool IsAirBelt(CargoTraffic traffic, int beltId)
        {
            if (!RemodelSettings.AirRail)
            {
                return false;
            }

            if (traffic?.beltPool == null || beltId <= 0 || beltId >= traffic.beltPool.Length ||
                traffic.beltPool[beltId].id != beltId)
            {
                return false;
            }

            int entityId = traffic.beltPool[beltId].entityId;
            return entityId > 0 && entityId < traffic.factory.entityPool.Length &&
                traffic.factory.entityPool[entityId].id == entityId &&
                traffic.factory.entityPool[entityId].protoId == ProtoID.I空轨;
        }

        private static int BeltBatchIndex(int nativeIndex, CargoTraffic traffic, int beltId)
        {
            if (nativeIndex < FastBeltBatchStart || nativeIndex >= NativeBeltBatchCount || !IsAirBelt(traffic, beltId))
            {
                return nativeIndex;
            }

            var batches = airBatches.GetOrCreateValue(traffic);
            if (batches.BeltStart < 0)
            {
                ref var renderingBatches = ref beltBatches(traffic);
                batches.BeltStart = renderingBatches.Length;
                Array.Resize(ref renderingBatches, batches.BeltStart + AirBeltBatchCount);
                for (int curve = 0; curve < AirBeltBatchCount; curve++)
                {
                    int meshIndex = FastBeltBatchStart + curve;
                    renderingBatches[batches.BeltStart + curve] =
                        new AirRailBody(meshIndex, renderingBatches[meshIndex].nodeWidth);
                }
            }
            return batches.BeltStart + nativeIndex - FastBeltBatchStart;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltRenderer))]
        private static IEnumerable<CodeInstruction> RouteBeltBatch(IEnumerable<CodeInstruction> instructions)
        {
            var batchIndex = AccessTools.Field(typeof(BeltComponent), nameof(BeltComponent.modelBatchIndex));
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add), new CodeMatch(instruction => instruction.StoresField(batchIndex)));
            matcher.ThrowIfInvalid("Native belt batch selection is unavailable.");

            CodeInstruction loadIndex = matcher.Instruction;
            matcher.Advance(1);
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(matcher.Instruction.ExtractLabels()),
                new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(AirBeltAppearance), nameof(BeltBatchIndex)),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc_S, loadIndex.operand));
            return matcher.InstructionEnumeration();
        }

        private static bool IsAirPathEnd(CargoTraffic traffic, CargoPath path, bool head)
        {
            if (path.belts.Count == 0)
            {
                return false;
            }

            int last = path.belts.Count - 1;
            int beltId = path.belts[head ? 0 : last];
            if (IsPathEnd(traffic.beltPool[beltId], path.pathLength, head))
            {
                return IsAirBelt(traffic, beltId);
            }

            beltId = path.belts[head ? last : 0];
            if (IsPathEnd(traffic.beltPool[beltId], path.pathLength, head))
            {
                return IsAirBelt(traffic, beltId);
            }

            // Opening a closed path can leave the endpoints inside the related-belt list.
            for (int index = 1; index < last; index++)
            {
                beltId = path.belts[index];
                if (IsPathEnd(traffic.beltPool[beltId], path.pathLength, head))
                {
                    return IsAirBelt(traffic, beltId);
                }
            }

            return false;
        }

        private static bool IsPathEnd(BeltComponent belt, int pathLength, bool head) =>
            head ? belt.segIndex == 0 : belt.segIndex + belt.segLength == pathLength;

        private static int PathBatchIndex(int nativeIndex, CargoTraffic traffic, CargoPath path, bool head)
        {
            if (nativeIndex < 0 || nativeIndex >= NativePathBatchCount || !IsAirPathEnd(traffic, path, head))
            {
                return nativeIndex;
            }

            var batches = airBatches.GetOrCreateValue(traffic);
            if (batches.PathStart < 0)
            {
                ref var renderingBatches = ref pathBatches(traffic);
                batches.PathStart = renderingBatches.Length;
                Array.Resize(ref renderingBatches, batches.PathStart + NativePathBatchCount);
                for (int meshIndex = 0; meshIndex < NativePathBatchCount; meshIndex++)
                {
                    renderingBatches[batches.PathStart + meshIndex] = new AirRailEnd(meshIndex);
                }
            }
            return batches.PathStart + nativeIndex;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterPathRenderer))]
        private static IEnumerable<CodeInstruction> RoutePathBatches(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var pathLength = AccessTools.PropertyGetter(typeof(CargoPath), nameof(CargoPath.pathLength));
            var selectBatch = AccessTools.Method(typeof(AirBeltAppearance), nameof(PathBatchIndex));
            var headBatch = generator.DeclareLocal(typeof(int));
            var matcher = new CodeMatcher(instructions);
            for (int endpoint = 0; endpoint < 2; endpoint++)
            {
                matcher.MatchForward(true, new CodeMatch(instruction => instruction.IsLdloc()),
                    new CodeMatch(instruction => instruction.Calls(pathLength)),
                    new CodeMatch(instruction => instruction.LoadsConstant(10L)),
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Ble || instruction.opcode == OpCodes.Ble_S),
                    new CodeMatch(instruction => instruction.LoadsConstant(1L)),
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Br || instruction.opcode == OpCodes.Br_S),
                    new CodeMatch(instruction => instruction.LoadsConstant(0L)),
                    new CodeMatch(instruction => instruction.IsStloc()));
                matcher.ThrowIfInvalid("Native path endpoint batch selection is unavailable.");
                bool head = endpoint == 0;
                CodeInstruction loadPath = matcher.InstructionAt(-7);
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0).WithLabels(matcher.Instruction.ExtractLabels()),
                    new CodeInstruction(loadPath.opcode, loadPath.operand),
                    new CodeInstruction(head ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Call, selectBatch));
                if (head)
                {
                    matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Stloc, headBatch));
                }
                matcher.Advance(1);
            }

            // The rear endpoint links to the head batch selected by the first allocation.
            matcher.MatchForward(false, new CodeMatch(instruction => instruction.IsLdloc()),
                new CodeMatch(instruction => instruction.LoadsConstant(1L)),
                new CodeMatch(OpCodes.Add), new CodeMatch(OpCodes.Ldc_I4), new CodeMatch(OpCodes.Mul));
            matcher.ThrowIfInvalid("Native path endpoint link is unavailable.");
            matcher.Set(OpCodes.Ldloc, headBatch);
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PathRenderingBatch), nameof(PathRenderingBatch.AddNode))]
        [HarmonyPatch(typeof(PathRenderingBatch), nameof(PathRenderingBatch.AddNodeNoRefresh))]
        private static void UseRailEndSpeed(PathRenderingBatch __instance, ref int speed)
        {
            if (__instance is AirRailEnd)
            {
                speed = NativeRenderSpeed;
            }
        }

        private static Mesh RailMesh(Mesh nativeMesh, object batch) =>
            batch is IAirRailBatch rail ? rail.RailMesh : nativeMesh;

        private static Camera RailCamera(Camera nativeCamera, object batch, Camera camera) =>
            batch is IAirRailBatch ? camera : nativeCamera;

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BeltRenderingBatch), nameof(BeltRenderingBatch.Draw))]
        [HarmonyPatch(typeof(PathRenderingBatch), nameof(PathRenderingBatch.Draw))]
        [HarmonyPatch(typeof(BeltRenderingBatch), nameof(BeltRenderingBatch.GpuAnalysis))]
        [HarmonyPatch(typeof(PathRenderingBatch), nameof(PathRenderingBatch.GpuAnalysis))]
        private static IEnumerable<CodeInstruction> UseRailMesh(IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            bool body = original.DeclaringType == typeof(BeltRenderingBatch);
            var meshes = AccessTools.Field(typeof(BuiltinConfig), body ? nameof(BuiltinConfig.beltMesh)
                : nameof(BuiltinConfig.pathMesh));
            var batchIndex = AccessTools.Field(original.DeclaringType, "batchIndex");
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true, new CodeMatch(instruction => instruction.LoadsField(meshes)),
                new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(instruction => instruction.LoadsField(batchIndex)),
                new CodeMatch(OpCodes.Ldelem_Ref));
            matcher.ThrowIfInvalid("Native belt mesh lookup is unavailable.");
            matcher.Advance(1).Insert(new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(AirBeltAppearance), nameof(RailMesh)));

            if (original.Name == nameof(BeltRenderingBatch.Draw))
            {
                matcher.MatchForward(false, new CodeMatch(OpCodes.Ldnull), new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(instruction => instruction.operand is MethodInfo method &&
                        method.DeclaringType == typeof(Graphics) &&
                        method.Name == nameof(Graphics.DrawMeshInstancedIndirect)));
                matcher.ThrowIfInvalid("Native belt draw camera is unavailable.");
                // Air-rail batches draw into the same camera as their wire and direction layers.
                matcher.Advance(1).Insert(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
                    CodeInstruction.Call(typeof(AirBeltAppearance), nameof(RailCamera)));
            }
            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.Draw))]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.ClearStates))]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.DestroyRenderingBatches))]
        private static IEnumerable<CodeInstruction> IterateAllBatches(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            for (int group = 0; group < 2; group++)
            {
                bool body = group == 0;
                int nativeCount = body ? NativeBeltBatchCount : NativePathBatchCount;
                var batches = AccessTools.Field(typeof(CargoTraffic), body ? "beltRenderingBatch" : "pathRenderingBatch");

                // The native loops own the entire batch array, including appended rail batches.
                matcher.MatchForward(false, new CodeMatch(instruction => instruction.LoadsConstant(nativeCount)),
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Blt ||
                        instruction.opcode == OpCodes.Blt_S));
                matcher.ThrowIfInvalid("Native belt batch loop bound is unavailable.");
                matcher.Set(OpCodes.Ldarg_0, null).Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldfld, batches), new CodeInstruction(OpCodes.Ldlen),
                    new CodeInstruction(OpCodes.Conv_I4));
            }
            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.DestroyRenderingBatches))]
        private static void ForgetBatchIndices(CargoTraffic __instance) => airBatches.Remove(__instance);

        // Cargo speeds remain unchanged; native selection and blueprint shaders use their own speed codes.
        private static int RenderSpeed(PrefabDesc desc) =>
            RemodelSettings.AirRail && desc.modelIndex == ProtoID.M空轨 ? NativeRenderSpeed : desc.beltSpeed;

        [HarmonyTranspiler, HarmonyPatch(typeof(ConnGizmoRenderer), nameof(ConnGizmoRenderer.Update))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateGizmos))]
        [HarmonyPatch(typeof(BuildTool_BlueprintCopy), nameof(BuildTool_BlueprintCopy.UpdatePreviewModels))]
        private static IEnumerable<CodeInstruction> RenderPreviewSpeed(IEnumerable<CodeInstruction> instructions)
        {
            var speedField = AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.beltSpeed));
            var renderSpeed = AccessTools.Method(typeof(AirBeltAppearance), nameof(RenderSpeed));
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsField(speedField))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = renderSpeed;
                }

                yield return instruction;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SetBeltSelected))]
        private static bool SelectAirBelt(CargoTraffic __instance, int beltId)
        {
            if (!IsAirBelt(__instance, beltId))
            {
                return true;
            }

            __instance.SetBeltState(beltId, NativeRenderSpeed);
            return false;
        }
    }
}
