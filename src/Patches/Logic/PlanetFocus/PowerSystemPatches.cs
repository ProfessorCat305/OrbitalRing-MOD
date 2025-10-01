using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProjectOrbitalRing.Patches.Logic.PlanetFocus {
    public static partial class PlanetFocusPatches {
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> EnergyCap_Transpiler(IEnumerable<CodeInstruction> instructions) {
            // var matcher = new CodeMatcher(instructions);
            //
            // // wind
            // matcher.MatchForward(false,
            //     new CodeMatch(OpCodes.Call,
            //         AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Wind))));
            // matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            // matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call,
            //     AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_Wind))));
            //
            // //pv
            // matcher.MatchForward(false,
            //     new CodeMatch(OpCodes.Call,
            //         AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_PV))));
            // matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            // matcher.InsertAndAdvance(
            //     new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_PV))));
            //
            // //fuel
            // matcher.MatchForward(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Ldloc_S),
            //     new CodeMatch(OpCodes.Ldelema),
            //     new CodeMatch(OpCodes.Call,
            //         AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Fuel))));
            //
            // List<CodeInstruction> ins = matcher.InstructionsWithOffsets(0, 3);
            //
            // matcher.Advance(5).InsertAndAdvance(ins);
            // matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            // matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call,
            //     AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_Fuel))));
            //
            // return matcher.InstructionEnumeration();

            var matcher = new CodeMatcher(instructions);

            // wind - 匹配 EnergyCap_Wind 调用
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(PowerGeneratorComponent),
                        nameof(PowerGeneratorComponent.EnergyCap_Wind))));

            if (matcher.IsValid) {
                matcher.Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_Wind))));
            }

            // pv - 匹配 EnergyCap_PV 调用
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_PV))));

            if (matcher.IsValid) {
                matcher.Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_PV))));
            }

            // fuel - 需要特殊处理，因为我们需要 PowerGeneratorComponent 引用
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    AccessTools.Method(typeof(PowerGeneratorComponent),
                        nameof(PowerGeneratorComponent.EnergyCap_Fuel))));

            if (matcher.IsValid) {
                // 找到调用前的 PowerGeneratorComponent 加载指令
                // 通常是 ldloc.s 或类似的指令
                var fuelCallPos = matcher.Pos;

                // 向前查找加载 PowerGeneratorComponent 的指令
                matcher.Start();
                matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldloc_S),// 或其他加载局部变量的指令
                    new CodeMatch(OpCodes.Ldelema),
                    new CodeMatch(OpCodes.Call,
                        AccessTools.Method(typeof(PowerGeneratorComponent),
                            nameof(PowerGeneratorComponent.EnergyCap_Fuel))));

                if (matcher.IsValid) {
                    // 保存加载 PowerGeneratorComponent 的指令
                    var loadComponentInstr = matcher.Instruction.Clone();
                    var ldelemaInstr = matcher.InstructionAt(1).Clone();

                    // 移动到调用后
                    matcher.Advance(3)
                        .InsertAndAdvance(loadComponentInstr)// 重新加载 PowerGeneratorComponent
                        .InsertAndAdvance(ldelemaInstr)// 重新执行 ldelema
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))// 加载 PowerSystem
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                            AccessTools.Method(typeof(PlanetFocusPatches), nameof(EnergyCap_Fuel))));
                }
            }

            return matcher.InstructionEnumeration();
        }

        public static long EnergyCap_Wind(long power, PowerSystem powerSystem) {
            bool exist = ContainsFocus(powerSystem.factory.planetId, 6525);

            return exist ? (long)(power * 1.2) : power;
        }

        public static long EnergyCap_PV(long power, PowerSystem powerSystem) {
            bool exist = ContainsFocus(powerSystem.factory.planetId, 6526);

            return exist ? (long)(power * 1.2) : power;
        }

        public static long EnergyCap_Fuel(long power, ref PowerGeneratorComponent component, PowerSystem powerSystem) {
            int focusId;
            double extra;

            switch (component.fuelMask) {
                case 1:
                    focusId = 6524;
                    extra = 1.2;

                    break;

                case 2:
                    focusId = 6529;
                    extra = 1.2;

                    break;

                case 16:
                    focusId = 6527;
                    extra = 1.1;

                    break;

                default: return power;
            }

            bool exist = ContainsFocus(powerSystem.factory.planetId, focusId);

            return exist ? (long)(power * extra) : power;
        }

        [HarmonyPatch(typeof(UIPowerGeneratorWindow), nameof(UIPowerGeneratorWindow._OnUpdate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UIPowerGeneratorWindow_OnUpdate_Transpiler(
            IEnumerable<CodeInstruction> instructions) {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld,
                    AccessTools.Field(typeof(PowerGeneratorComponent),
                        nameof(PowerGeneratorComponent.capacityCurrentTick))));

            CodeInstruction comp = matcher.Instruction;

            matcher.Advance(2).InsertAndAdvance(comp);

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(UIPowerGeneratorWindow), nameof(UIPowerGeneratorWindow.factory))));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(PlanetFocusPatches), nameof(UIPowerGeneratorWindow_OnUpdate))));

            return matcher.InstructionEnumeration();
        }

        public static long UIPowerGeneratorWindow_OnUpdate(long power, PowerGeneratorComponent component,
            PlanetFactory factory) {
            int focusId;
            double extra;

            if (component.wind) {
                focusId = 6525;
                extra = 1.2;
            } else if (component.photovoltaic) {
                focusId = 6526;
                extra = 1.2;
            } else {
                switch (component.fuelMask) {
                    case 1:
                        focusId = 6524;
                        extra = 1.2;

                        break;

                    case 2:
                        focusId = 6529;
                        extra = 1.2;

                        break;

                    case 16:
                        focusId = 6527;
                        extra = 1.1;

                        break;

                    default: return power;
                }
            }

            bool exist = ContainsFocus(factory.planetId, focusId);

            return exist ? (long)(power * extra) : power;
        }
    }
}
