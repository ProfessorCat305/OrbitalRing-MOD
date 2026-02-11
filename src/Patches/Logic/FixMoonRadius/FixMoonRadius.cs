using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using System.Reflection;
using static System.Reflection.Emit.OpCodes;

namespace ProjectOrbitalRing.Patches.Logic.FixMoonRadius
{
    internal class FixMoonRadius
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetRawData), "GetModPlane")]
        public static void GetModPlanePatch(ref PlanetRawData __instance, ref short __result)
        {
            if (__instance.precision == 100)
                __result -= 10010;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetData), "get_atmosphereHeight")]
        public static void atmosphereHeightPatch(PlanetData __instance, ref float __result)
        {
            if (__instance.radius == 100f)
                __result -= 100f;
        }


        public static float ModifyRadius(float vanilla, float realRadius)
        {
            float diff = vanilla - 200.0f;
            realRadius += diff;
            return realRadius;
        }
        public static float GetRadiusFromFactory(float t, PlanetFactory factory)
        {
            float realRadius = ModifyRadius(t, factory?.planet?.realRadius ?? 200.0f);
            return realRadius;
        }

        public static float GetRadiusFromAstroId(float t, int id)
        {
            float realRadius = ModifyRadius(t, GameMain.data.galaxy?.astrosFactory[id]?.planet?.realRadius ?? 200.0f);
            return realRadius;
        }
        public static float GetRadiusFromEnemyData(float t, ref EnemyData enemyData)
        {
            var realRadius = ModifyRadius(t, GameMain.galaxy.PlanetById(enemyData.astroId)?.realRadius ?? 200.0f);
            return realRadius;
        }
        public static float GetRadiusFromMecha(float t, Mecha mecha)
        {
            var realRadius = ModifyRadius(t, mecha?.player?.planetData?.realRadius ?? 200.0f);
            return realRadius;
        }
        public static float GetRadiusFromAltitude(float t, float alt) //Original / Altitude
        {
            var realRadius = ModifyRadius(t, alt);
            return realRadius;
        }
        //public static float GetRadiusFromLocalPlanet(float t)
        //{
        //    var realRadius = ModifyRadius(t, GameMain.localPlanet?.realRadius ?? 200f);
        //    return realRadius;
        //}

        public static T GetRadiusFromLocalPlanet<T>(T t)
        {
            var realRadius = ModifyRadius(Convert.ToSingle(t), GameMain.localPlanet?.realRadius ?? 200f);
            // if (VFInput.alt) GS2.Log($"GetRadius Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} orig:{Convert.ToDouble(t)} returning {realRadius}");
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }


        //private static float ReturnFixRadius(PlanetFactory factory)
        //{
        //    if (factory == null) {
        //        return 197.6f;
        //    }
        //    if (factory.planet.radius == 100f) {
        //        return 97.6f;
        //    }
        //    return 197.6f;
        //}

        //[HarmonyTranspiler]
        //[HarmonyPatch(typeof(DFGTurretComponent), nameof(DFGTurretComponent.Aim))]
        //public static IEnumerable<CodeInstruction> DFGTurretComponent_Aim_Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var matcher = new CodeMatcher(instructions);

        //    matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4), new CodeMatch(OpCodes.Call), new CodeMatch(OpCodes.Ldc_R4));

        //    matcher.Advance(2);
        //    matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_1));
        //    matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixMoonRadius), nameof(ReturnFixRadius))));

        //    //matcher.LogInstructionEnumeration();
        //    return matcher.InstructionEnumeration();
        //}

        private static float GetSquareRadiusFromAstroFactoryId(int astroId)
        {
            if (GameMain.data.galaxy?.astrosFactory[astroId]?.planet?.radius == 100f) {
                return 97.5f * 97.5f;
            } else {
                return 197.5f * 197.5f;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LocalGeneralProjectile), nameof(LocalGeneralProjectile.TickSkillLogic))] // 修复月球黑雾防御塔子弹
        public static IEnumerable<CodeInstruction> LocalGeneralProjectile_TickSkillLogic_Patch(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld),
               new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld));

            var astroId = matcher.Advance(2).Operand;

            matcher.MatchForward(true,
                    new CodeMatch(i => {
                        return (i.opcode == Ldc_R4) &&
                               (
                                   Math.Abs(Convert.ToDouble(i.operand ?? 0.0) - 39006.25) < 1
                               );
                    })
                );
            if (matcher.IsInvalid) {
                return instructions;
            }

            matcher.InsertAndAdvance(new CodeInstruction(Ldarg_0), new CodeInstruction(Ldfld, astroId));
            matcher.SetInstruction(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetSquareRadiusFromAstroFactoryId))));

            return matcher.InstructionEnumeration();
        }


        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LocalLaserContinuous), nameof(LocalLaserContinuous.TickSkillLogic))]
        [HarmonyPatch(typeof(LocalLaserOneShot), nameof(LocalLaserOneShot.TickSkillLogic))]
        public static IEnumerable<CodeInstruction> TickSkillLogic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try {
                var matcher = new CodeMatcher(instructions);
                matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld));

                var astroId = matcher.Advance(2).Operand;

                matcher.MatchForward(true,
                        new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) &&
                                           (Convert.ToDouble(i.operand ?? 0.0) == 197.5 ||
                                            Convert.ToDouble(i.operand ?? 0.0) == 198.5)
                    ));
                if (!matcher.IsInvalid) {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_0), new CodeInstruction(Ldfld, astroId));

                    matcher.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAstroId))));

                    instructions = matcher.InstructionEnumeration();
                    return instructions;
                }
                return instructions;
            } catch {
                return instructions;
            }
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Defense_Ground))] //225
        public static IEnumerable<CodeInstruction> RunBehavior_Defense_Ground_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) && Math.Abs(Convert.ToDouble(i.operand ?? 0.0) - 225.0) < 0.01f)
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg, (byte)5));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAltitude))));
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_GRaider))] //200 206 202
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_GRanger))] //200 212 225
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_GGuardian))]
        public static IEnumerable<CodeInstruction> RunBehavior_Engage_EnemyUnit_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => {
                        return (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) &&
                               (
                                    Convert.ToDouble(i.operand ?? 0.0) == 200.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 202.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 206.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 212.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 225.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 255.0 ||
                                    Convert.ToDouble(i.operand ?? 0.0) == 228.0

                            );
                    })
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_1));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromFactory))));
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_SHumpback))] //200 but need to find the planet...
        public static IEnumerable<CodeInstruction> RunBehavior_Engage_SHumpback_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // var methodInfo = AccessTools.Method(typeof(EnemyUnitComponentTranspiler), nameof(Utils.GetRadiusFromEnemyData));
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldc_R8 && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_S, (sbyte)3));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromEnemyData))));

                    // Bootstrap.DumpMatcherPost(matcher, 3, 5, 5);
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_OrbitTarget_SLancer))] //200 but need to find the planet...
        public static IEnumerable<CodeInstruction> RunBehavior_OrbitTarget_SLancer_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldc_R8 && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_S, (sbyte)4));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromEnemyData))));
                }).InstructionEnumeration();

            return instructions;
        }


        [HarmonyTranspiler]
        //[HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackLaser_Ground))] //225f 212f
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackPlasma_Ground))]//225f 212f
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_DefenseShield_Ground))]
        public static IEnumerable<CodeInstruction> UnitComponent_RunBehavior_Engage_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => {
                        return (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) &&
                               (
                                   Convert.ToDouble(i.operand ?? 0.0) == 212.0 ||
                                   Convert.ToDouble(i.operand ?? 0.0) == 225.0

                               );
                    })
                )
                .Repeat(matcher => {
                    matcher.Advance(1); 
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_1));
                    matcher.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromFactory))));
                }).InstructionEnumeration();

            return instructions;
        }

        // Mecha
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_SAttackLaser_Large))]//
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_SAttackPlasma_Small))]//
        public static IEnumerable<CodeInstruction> UnitComponent_RunBehavior_Engage_S_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_2));
                    matcher.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromMecha))));
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Bomb_Explosive), nameof(Bomb_Explosive.TickSkillLogic))]
        [HarmonyPatch(typeof(Bomb_Explosive), nameof(Bomb_Explosive.BombFactoryObjects))]
        [HarmonyPatch(typeof(Bomb_EMCapsule), nameof(Bomb_EMCapsule.TickSkillLogic))]
        [HarmonyPatch(typeof(Bomb_EMCapsule), nameof(Bomb_EMCapsule.BombFactoryObjects))]
        [HarmonyPatch(typeof(Bomb_Liquid), nameof(Bomb_Explosive.TickSkillLogic))]
        public static IEnumerable<CodeInstruction> Bomb_Explosive_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            try {
                Type currentClass = __originalMethod.DeclaringType;
                var matcher = new CodeMatcher(instructions)
                    .MatchForward(
                        true,
                        new CodeMatch(i => (i.opcode == Ldc_R4 &&
                            (i.OperandIs(200f) || i.OperandIs(250f) || i.OperandIs(270f)))
                    ));
                //var mi = matcher.GetRadiusFromAstroId();
                matcher.Repeat(m => {
                    m.Advance(1);
                    m.InsertAndAdvance(new CodeInstruction(Ldarg_0));
                    m.InsertAndAdvance(new CodeInstruction(Ldfld, AccessTools.Field(currentClass, "nearPlanetAstroId")));
                    m.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAstroId))));
                });
                return matcher.InstructionEnumeration();
            } catch (Exception e) {
                return instructions;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.RelaySailLogic))]
        public static IEnumerable<CodeInstruction> DFRelayComponent_RelaySailLogic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => {
                        return (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) &&
                            (
                                Convert.ToDouble(i.operand ?? 0.0) == 200.0
                            );
                    })
                )
                .Repeat(matcher => {
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_0));
                    matcher.InsertAndAdvance(new CodeInstruction(Ldfld, AccessTools.Field(typeof(DFRelayComponent), nameof(DFRelayComponent.targetAstroId))));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAstroId))));
                }).InstructionEnumeration();

            return instructions;
        }


    }
}
