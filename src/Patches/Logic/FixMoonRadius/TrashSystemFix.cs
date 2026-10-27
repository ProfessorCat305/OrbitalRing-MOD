using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.FixMoonRadius
{
    internal class TrashSystemFix
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            return !GalacticScale2Compatibility;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.Gravity))]
        public static IEnumerable<CodeInstruction> TrashSystem_Gravity_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try {
                // Fix planet loop for outer planets
                // Change: for (int i = nearStarId + 1; i <= nearStarId + 8; i++)
                // To:     for (int i = nearStarId + 1; i <= nearStarId + GameMain.galaxy.StarById(nearStarId / 100).planetCount; i++)
                var matcher = new CodeMatcher(instructions)
                    .MatchForward(true,
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(OpCodes.Add),
                        new CodeMatch(OpCodes.Stloc_S),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldc_I4_8), // replace target 
                        new CodeMatch(OpCodes.Add),
                        new CodeMatch(OpCodes.Ble));
                if (matcher.IsValid) {
                    matcher
                        .Advance(-2)
                        .RemoveInstructions(2)
                        .Insert(Transpilers.EmitDelegate<Func<int, int>>(nearStarId => {
                            return nearStarId + GameMain.galaxy.StarById(nearStarId / 100)?.planetCount ?? nearStarId + 8;
                        }));
                } else {
                }

                var oprand_lPos = AccessTools.Field(typeof(TrashData), nameof(TrashData.lPos));
                matcher.MatchBack(false,
                new CodeMatch(OpCodes.Ldarg_1),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Stfld, oprand_lPos));
                var oprand_vectorLF3 = matcher.InstructionAt(1).operand;
                var oprand_convert = matcher.InstructionAt(2).operand;

                matcher.Advance(-2)
                    .MatchBack(false,
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(TrashData), nameof(TrashData.uVel))))
                    .Advance(1)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_S, oprand_vectorLF3),
                        new CodeInstruction(OpCodes.Call, oprand_convert),
                        new CodeInstruction(OpCodes.Stfld, oprand_lPos)
                    );

                return matcher.InstructionEnumeration();
            } catch (Exception e) {
                return instructions;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.AddTrash))]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.AddTrashFromGroundEnemy))]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.AddTrashOnPlanet))]
        public static IEnumerable<CodeInstruction> AddTrashTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            // Change: if (vectorLF.magnitude < 170.0)
            // To:     if (vectorLF.magnitude < @planet.realRadius - 30.0)
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldloca_S),
                        new CodeMatch(op => op.opcode == OpCodes.Call && ((MethodInfo)op.operand).Name == "get_magnitude"),
                        new CodeMatch(op => op.opcode == OpCodes.Ldc_R8 && op.OperandIs(170f))
                    );
            matcher.Advance(2);
            matcher.RemoveInstruction();

            switch (__originalMethod.Name) {
                case "AddTrash": // GameMain.localPlanet.realRadius - 30.0
                    matcher.Insert(
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(GameMain), nameof(GameMain.localPlanet))),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                        new CodeInstruction(OpCodes.Ldc_R8, 30.0),
                        new CodeInstruction(OpCodes.Sub));
                    break;
                case "AddTrashFromGroundEnemy": // factory.planet - 30.0
                    matcher.Insert(
                        new CodeInstruction(OpCodes.Ldarg_S, 5), // factory
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetFactory), nameof(PlanetFactory.planet))),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                        new CodeInstruction(OpCodes.Ldc_R8, 30.0),
                        new CodeInstruction(OpCodes.Sub));
                    break;
                case "AddTrashOnPlanet": // planet - 30.0
                    matcher.Insert(
                        new CodeInstruction(OpCodes.Ldarg_S, 5), // planet
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetData), nameof(PlanetData.realRadius))),
                        new CodeInstruction(OpCodes.Ldc_R8, 30.0),
                        new CodeInstruction(OpCodes.Sub));
                    break;
            }
            return matcher.InstructionEnumeration();
        }
    }
}
