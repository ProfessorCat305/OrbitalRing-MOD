using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using static ProjectOrbitalRing.Patches.Logic.FixMoonRadius.FixMoonRadius;

namespace ProjectOrbitalRing.Patches.Logic.FixMoonRadius
{
    internal class RadiusFixTranspiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GetNormalizedDir))]
        [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GetNormalizedPos))]
        [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GetExtendedGratBox), typeof(BPGratBox), typeof(float))]
        [HarmonyPatch(typeof(BlueprintUtils), nameof(BlueprintUtils.GetExtendedGratBox), typeof(BPGratBox), typeof(float), typeof(float))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.GenerateBlueprintGratBoxes))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.GetGridWidth))]
        [HarmonyPatch(typeof(PlayerNavigation), nameof(PlayerNavigation.Init))]
        [HarmonyPatch(typeof(PlayerNavigation), nameof(PlayerNavigation.DetermineArrive))]
        [HarmonyPatch(typeof(PlanetEnvironment), nameof(PlanetEnvironment.LateUpdate))]
        [HarmonyPatch(typeof(PlayerAction_Combat), nameof(PlayerAction_Combat.Shoot_Gauss_Space))]
        [HarmonyPatch(typeof(PlayerAction_Combat), nameof(PlayerAction_Combat.Shoot_Plasma))]
        [HarmonyPatch(typeof(PlayerAction_Plant), nameof(PlayerAction_Plant.UpdateRaycast))]
        [HarmonyPatch(typeof(PlayerAction_Navigate), nameof(PlayerAction_Navigate.GameTick))]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.CalculateGeothermalStrength))]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.IsTargetVeinInRange))]
        [HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.UpdateRaycastAndReform))]
        [HarmonyPatch(typeof(BuildTool_Upgrade), nameof(BuildTool_Upgrade.UpdateRaycast))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.GetGridWidth))]
        [HarmonyPatch(typeof(SpraycoaterComponent), nameof(SpraycoaterComponent.GetReshapeData))]
        [HarmonyPatch(typeof(SpraycoaterComponent), nameof(SpraycoaterComponent.Reshape))]
        [HarmonyPatch(typeof(SpaceCapsule), nameof(SpaceCapsule.LateUpdate))]
        public static IEnumerable<CodeInstruction> RadiusFix_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //var methodInfo = AccessTools.Method(typeof(Utils), nameof(Utils.GetRadiusFromLocalPlanet));
            try {
                var matcher = new CodeMatcher(instructions);
                matcher.MatchForward(
                    true,
                    new CodeMatch(i => {
                        return (i.opcode == OpCodes.Ldc_R4 || i.opcode == OpCodes.Ldc_R8 || i.opcode == OpCodes.Ldc_I4) &&
                               (
                                    Convert.ToDouble(i.operand ?? 0.0) == 196.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 197.5 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 197.6 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 198.5 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 200.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 200.22 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 200.5 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 202.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 206.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 212.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 225.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 228.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 255.0
                            );
                    })
                );
                matcher.Repeat(m => {
                    if (!matcher.IsInvalid) {
                        var operandType = matcher.Operand?.GetType() ?? typeof(float);
                        m.Advance(1);
                        m.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromLocalPlanet)).MakeGenericMethod(operandType)));
                    }
                });
                //matcher.LogInstructionEnumeration();
                instructions = matcher.InstructionEnumeration();
                return instructions;
            } catch (Exception e) {
                return instructions;
            }

        }
    }
}
