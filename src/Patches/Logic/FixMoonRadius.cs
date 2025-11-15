using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HashSystem;
using ProjectOrbitalRing.Utils;
using static UnityEngine.GraphicsBuffer;
using UnityEngine;
using GalacticScale;
using System.Reflection;
using static System.Reflection.Emit.OpCodes;

namespace ProjectOrbitalRing.Patches.Logic
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
        private static float GetRadiusFromFactory(float t, PlanetFactory factory)
        {
            float realRadius = ModifyRadius(t, factory?.planet?.realRadius ?? 200.0f);
            return realRadius;
        }

        private static float GetRadiusFromAstroId(float t, int id)
        {
            float realRadius = ModifyRadius(t, GameMain.data.galaxy?.astrosFactory[id]?.planet?.realRadius ?? 200.0f);
            return realRadius;
        }
        private static float GetRadiusFromEnemyData(float t, ref EnemyData enemyData)
        {
            var realRadius = ModifyRadius(t, GameMain.galaxy.PlanetById(enemyData.astroId)?.realRadius ?? 200.0f);
            return realRadius;
        }
        private static float GetRadiusFromMecha(float t, Mecha mecha)
        {
            var realRadius = ModifyRadius(t, mecha?.player?.planetData?.realRadius ?? 200.0f);
            return realRadius;
        }
        private static float GetRadiusFromAltitude(float t, float alt) //Original / Altitude
        {
            var realRadius = ModifyRadius(t, alt);
            return realRadius;
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
        public static IEnumerable<CodeInstruction> Aim_Transpiler(IEnumerable<CodeInstruction> instructions)
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

                        // var mi = AccessTools.Method(typeof(PatchOnDFRelayComponent), nameof(Utils.GetRadiusFromAstroId)).MakeGenericMethod(matcher.Operand?.GetType() ?? typeof(float));
                    //var mi = matcher.GetRadiusFromAstroId();
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_0), new CodeInstruction(Ldfld, astroId));
                    //matcher.InsertAndAdvance(Utils.LoadField(typeof(LocalLaserContinuous), nameof(LocalLaserContinuous.astroId)));

                    matcher.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAstroId))));

                //matcher.LogInstructionEnumeration();
                    instructions = matcher.InstructionEnumeration();
                    return instructions;
                }
                return instructions;
            } catch {
                //Bootstrap.Logger.LogInfo("PatchOnLocalLaserContinuous.Aim_Transpiler failed");
                return instructions;
            }
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Defense_Ground))] //225
        public static IEnumerable<CodeInstruction> Fix225(IEnumerable<CodeInstruction> instructions)
        {
            // Bootstrap.DumpInstructions(instructions, nameof(EnemyUnitComponent.RunBehavior_Defense_Ground),290, 20);
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) && Math.Abs(Convert.ToDouble(i.operand ?? 0.0) - 225.0) < 0.01f)
                )
                .Repeat(matcher => {
                    // Bootstrap.Logger.LogInfo($"Found value {matcher.Operand} at {matcher.Pos} type {matcher.Operand?.GetType()}");
                    //var mi = matcher.GetRadiusFromAltitude();
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg, (byte)5));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAltitude))));
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_GRaider))] //200 206 202
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_GRanger))] //200 212 225
        public static IEnumerable<CodeInstruction> Fix200_225(IEnumerable<CodeInstruction> instructions)
        {
            // var methodInfo = AccessTools.Method(typeof(EnemyUnitComponentTranspiler), nameof(Utils.GetRadiusFromFactory));

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
                                    Convert.ToDouble(i.operand ?? 0.0) == 225.0

                            );
                    })
                )
                .Repeat(matcher => {
                    //var mi = matcher.GetRadiusFromFactory();
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_1));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromFactory))));
                }).InstructionEnumeration();

            return instructions;
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_Engage_SHumpback))] //200 but need to find the planet...
        // [HarmonyPatch(typeof(EnemyUnitComponent),  nameof(EnemyUnitComponent.RunBehavior_OrbitTarget_SLancer))] //200 but need to find the planet...
        public static IEnumerable<CodeInstruction> Fix200(IEnumerable<CodeInstruction> instructions)
        {
            // var methodInfo = AccessTools.Method(typeof(EnemyUnitComponentTranspiler), nameof(Utils.GetRadiusFromEnemyData));
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldc_R8 && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    //var mi = matcher.GetRadiusFromEnemyData();
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_S, (sbyte)3));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromEnemyData))));

                    // Bootstrap.DumpMatcherPost(matcher, 3, 5, 5);
                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_OrbitTarget_SLancer))] //200 but need to find the planet...
        public static IEnumerable<CodeInstruction> Fix200Slancer(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldc_R8 && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    // var mi = methodInfo.MakeGenericMethod(matcher.Operand?.GetType() ?? typeof(float));
                    //var mi = matcher.GetRadiusFromEnemyData();
                    matcher.Advance(1);
                    matcher.InsertAndAdvance(new CodeInstruction(Ldarg_S, (sbyte)4));
                    matcher.Insert(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromEnemyData))));
                }).InstructionEnumeration();

            return instructions;
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackLaser_Ground))] //225f 212f
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackPlasma_Ground))]//225f 212f
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_DefenseShield_Ground))]
        public static IEnumerable<CodeInstruction> UnitComponent_Fix200_225(IEnumerable<CodeInstruction> instructions)
        {
            // var methodInfo = AccessTools.Method(typeof(EnemyUnitComponentTranspiler), nameof(Utils.GetRadiusFromFactory));

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
                    // Bootstrap.Logger.LogInfo($"Found value {matcher.Operand} at {matcher.Pos} type {matcher.Operand?.GetType()}");
                    // var mi = methodInfo.MakeGenericMethod(matcher.Operand?.GetType() ?? typeof(float));
                    //var mi = matcher.GetRadiusFromFactory();
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
        public static IEnumerable<CodeInstruction> UnitComponent_Fix200(IEnumerable<CodeInstruction> instructions)
        {
            // var methodInfo = AccessTools.Method(typeof(UnitComponentTranspiler), nameof(UnitComponentTranspiler.GetRadiusFromMecha));

            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) && Convert.ToDouble(i.operand ?? 0.0) == 200.0)
                )
                .Repeat(matcher => {
                    // Bootstrap.Logger.LogInfo($"Found value {matcher.Operand} at {matcher.Pos} type {matcher.Operand?.GetType()}");
                    // var mi = methodInfo.MakeGenericMethod(matcher.Operand?.GetType() ?? typeof(float));
                    //var mi = matcher.GetRadiusFromMecha();
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
        public static IEnumerable<CodeInstruction> FixRadius_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
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
                //m.InsertAndAdvance(Utils.LoadField(__originalMethod.DeclaringType, "nearPlanetAstroId"));
                    m.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadiusFromAstroId))));
                });
                return matcher.InstructionEnumeration();
            } catch (Exception e) {
                //GS2.Warn("FixRadius_Transpiler failed!" + __originalMethod.Name);
                //GS2.Warn(e.ToString());
                return instructions;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.RelaySailLogic))]
        public static IEnumerable<CodeInstruction> DFRelayComponent_RelaySailLogic_Patch(IEnumerable<CodeInstruction> instructions)
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




        

        /*

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DefenseSystem),  nameof(DefenseSystem.NewTurretComponent))]
        public static IEnumerable<CodeInstruction> NewTurretComponentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
             matcher.MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldarg_0),
                    new CodeMatch(i => i.opcode == Ldfld && i.operand.ToString().Contains("TurretComponent")),
                    new CodeMatch(i => i.opcode == Callvirt),
                    new CodeMatch(i => i.opcode == Stloc_0)
                    );
                 if (!matcher.IsInvalid)
                 {
                     matcher.Advance(1)
                         .InsertAndAdvance(new CodeInstruction(Ldarg_0))
                         .InsertAndAdvance(new CodeInstruction(Ldloc_0))
                         .InsertAndAdvance(new CodeInstruction(Call,
                             AccessTools.Method(typeof(TurretComponentTranspiler),
                                 nameof(TurretComponentTranspiler.AddTurret)))
                         );
                     instructions = matcher.InstructionEnumeration();
                     return instructions;
                 }

                 Bootstrap.Logger.LogInfo("Transpiler failed!  ");
        
                 return instructions;
        }






        


        

        public static Dictionary<TurretComponent, float> Radii = new Dictionary<TurretComponent, float>();

        public static float GetRadius(ref TurretComponent turret)
        {
            return !Radii.ContainsKey(turret) ? 200f : Radii[turret];
        }

        public static void AddTurret(DefenseSystem defenseSystem, ref TurretComponent turret)
        {
            GS2.Log($"Added Turret {turret.id} from DefenseSystem {defenseSystem.planet.name}");
            Radii[turret] = defenseSystem.planet.realRadius + 1;
        }

        public static void RemoveTurret(ref TurretComponent turret)
        {
            GS2.Log($"Removed Turret {turret.id}");
            Radii.Remove(turret);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.BeforeRemove))]
        public static IEnumerable<CodeInstruction> BeforeRemoveTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions).Advance(1)
                .InsertAndAdvance(new CodeInstruction(Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(Call,
                    AccessTools.Method(typeof(FixMoonRadius), nameof(RemoveTurret))))
                .InstructionEnumeration();
            return instructions;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.CheckEnemyIsInAttackRange))]
        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.Shoot_Plasma))]
        public static IEnumerable<CodeInstruction> Shoot_PlasmaTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            try {
                var matcher = new CodeMatcher(instructions)
                    .MatchForward(
                        true,
                        new CodeMatch(i => (i.opcode == Ldc_R4 || i.opcode == Ldc_R8 || i.opcode == Ldc_I4) &&
                                           Math.Abs(Convert.ToDouble(i.operand ?? 0.0) - 200.0) < 0.01f)
                    );
                if (!matcher.IsInvalid) {
                    matcher.Repeat(matcher => {
                        matcher.SetInstructionAndAdvance(new CodeInstruction(Ldarg_0));
                        matcher.InsertAndAdvance(new CodeInstruction(Call, AccessTools.Method(typeof(FixMoonRadius), nameof(GetRadius))));
                    });
                    instructions = matcher.InstructionEnumeration();
                    return instructions;
                }
                return instructions;
            } catch {
                Bootstrap.Logger.LogInfo("TurretComponentTranspiler.Shoot_PlasmaTranspiler failed");
                return instructions;
            }
        }





        
        
        // Fix 2.5f star radius multiplier to 0.5f, same as we did for logistics ships
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.RelaySailLogic))]
        public static IEnumerable<CodeInstruction> FixStarMultiplier(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions).MatchForward(
                false, 
                new CodeMatch(op => op.opcode == Ldc_R4 && op.OperandIs(2.5f))
            );
            
            if (codeMatcher.IsInvalid)
            {
                GS2.Warn("RelaySailLogic 2.5f multiplier transpiler failed");
                return instructions;
            }
            
            instructions = codeMatcher.Repeat(z => z.Set(OpCodes.Ldc_R4, 0.5f))
                .InstructionEnumeration();
                
            return instructions;
        }
        
        // Fix star radius for pathing calculations - more robust approach
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.RelaySailLogic))]
        public static IEnumerable<CodeInstruction> FixStarRadiusForPathing(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            
            // Look for patterns that indicate radius loading in the obstacle detection code
            // Try different approaches to find the right spot
            
            // Look for patterns specific to "num18 = reference3.uRadius;" followed by star radius branching
            for (int i = 0; i < codes.Count - 5; i++)
            {
                // Log found patterns for debugging
                if (codes[i].LoadsField(AccessTools.Field(typeof(AstroData), "uRadius")))
                {
                    GS2.Log($"Found uRadius load at index {i}, next instruction: {codes[i+1].opcode}");
                    
                    if (i < codes.Count - 4 && (
                        // Match the pattern for star detection by modulo check
                        (codes[i+3].opcode == OpCodes.Rem || 
                         codes[i+4].opcode == OpCodes.Rem || 
                         (i < codes.Count - 5 && codes[i+5].opcode == OpCodes.Rem)) ||
                        // Or match the pattern for radius modification
                        (codes[i+2].opcode == OpCodes.Mul && 
                         codes[i+1].opcode == OpCodes.Stloc_S)))
                    {
                        // Found a radius load followed by star type check (modulo 100) or multiplication
                        codes.Insert(i+1, new CodeInstruction(OpCodes.Call, 
                            AccessTools.Method(typeof(PatchOnDFRelayComponent), nameof(AdjustRadiusForPathingDF))));
                        
                        patched = true;
                        i += 2; // Skip ahead
                        GS2.Log($"Inserted radius adjustment at index {i}");
                    }
                }
            }
            
            // If first approach failed, try looking at 5000f safety distance
            if (!patched)
            {
                GS2.Log("First approach failed, trying alternate method");
                
                // Find the safety distance near uRadius loads
                for (int i = 0; i < codes.Count - 10; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && 
                        (float)codes[i].operand >= 4900f && 
                        (float)codes[i].operand <= 5100f)
                    {
                        // Look for uRadius references nearby
                        for (int j = i-5; j < i+10 && j < codes.Count; j++)
                        {
                            if (j >= 0 && codes[j].LoadsField(AccessTools.Field(typeof(AstroData), "uRadius")))
                            {
                                // Insert our adjustment right after loading uRadius
                                codes.Insert(j+1, new CodeInstruction(OpCodes.Call, 
                                    AccessTools.Method(typeof(PatchOnDFRelayComponent), nameof(AdjustRadiusForPathingDF))));
                                
                                patched = true;
                                i = j + 2; // Skip ahead
                                GS2.Log($"Inserted radius adjustment at index {j+1} (second approach)");
                                break;
                            }
                        }
                    }
                }
            }
            
            if (patched)
            {
                GS2.Log("Successfully patched DFRelayComponent.RelaySailLogic for better pathing with large stars");
            }
            else
            {
                GS2.Warn("Failed to find insertion point for radius adjustment in DFRelayComponent.RelaySailLogic - will try alternative approach");
                
                // As a last resort, try to patch all uRadius loads
                int count = 0;
                for (int i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].LoadsField(AccessTools.Field(typeof(AstroData), "uRadius")))
                    {
                        // Insert our adjustment call after every uRadius load
                        codes.Insert(i+1, new CodeInstruction(OpCodes.Call, 
                            AccessTools.Method(typeof(PatchOnDFRelayComponent), nameof(AdjustRadiusForPathingDF))));
                        
                        i++; // Skip the instruction we just added
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    GS2.Log($"Patched all {count} uRadius loads in DFRelayComponent.RelaySailLogic");
                    patched = true;
                }
            }
            
            if (!patched)
            {
                GS2.Error("All approaches failed to patch DFRelayComponent.RelaySailLogic");
            }
            
            return codes;
        }
        
        // Also patch the fixed safety distance value (5000f)
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.RelaySailLogic))]
        public static IEnumerable<CodeInstruction> FixSafetyDistance(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            
            // Find the safety distance calculation (typically around 5000f + speed)
            for (int i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && 
                    (float)codes[i].operand >= 4900f && (float)codes[i].operand <= 5100f)
                {
                    // Add our adjustment method call
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, 
                        AccessTools.Method(typeof(PatchOnDFRelayComponent), nameof(AdjustSafetyDistanceForPathCalculationDF))));
                    
                    patched = true;
                    i += 2; // Skip ahead since we modified the list
                    GS2.Log($"Patched safety distance at index {i}");
                }
            }
            
            if (patched)
            {
                GS2.Log("Patched DFRelayComponent.RelaySailLogic safety distances for large stars");
            }
            else
            {
                GS2.Warn("Failed to find safety distance in DFRelayComponent.RelaySailLogic");
            }
            
            return codes;
        }
        
        // Scaling function for avoidance calculations (similar to the one used for logistics ships)
        public static float AdjustRadiusForPathingDF(float originalRadius)
        {
            // For small stars (default game sizes), keep the original radius
            if (originalRadius < 1000f)
                return originalRadius;
                
            // For larger stars, apply a logarithmic scale to avoid excessive avoidance distances
            float adjustedRadius = 1000f + (float)(Math.Log10(originalRadius / 1000f + 1) * 1000f);
            
            // Ensure we never go below 60% of the original radius to avoid ships hitting stars
            return Math.Max(originalRadius * 0.6f, adjustedRadius);
        }
        
        // Adjust safety distance similar to the logistics ship patch
        public static float AdjustSafetyDistanceForPathCalculationDF(float originalDistance)
        {
            // Scale safety distance based on largest star radius in the galaxy
            float largestStarRadius = GetLargestStarRadiusDF();
            
            if (largestStarRadius <= 1000f)
                return originalDistance;
                
            // Scale the safety distance with diminishing returns
            float factor = 1f + Mathf.Log10(largestStarRadius / 1000f) * 0.75f;
            return originalDistance * factor;
        }
        
        // Helper to find the largest star radius in the galaxy (cached version)
        private static float largestCachedRadiusDF = 0f;
        private static int lastUpdateFrameDF = -1;
        
        private static float GetLargestStarRadiusDF()
        {
            // Cache this value as it's expensive to calculate
            if (Time.frameCount - lastUpdateFrameDF > 300 || largestCachedRadiusDF <= 0f)
            {
                lastUpdateFrameDF = Time.frameCount;
                largestCachedRadiusDF = 0f;
                
                if (GameMain.galaxy?.stars != null)
                {
                    foreach (var star in GameMain.galaxy.stars)
                    {
                        if (star != null)
                        {
                            // Use appropriate radius field based on what's available
                            float radius = 0f;
                            
                            // Try to access the radius using reflection to avoid compilation errors
                            if (star.GetType().GetField("physicsRadius") != null)
                            {
                                radius = (float)star.GetType().GetField("physicsRadius").GetValue(star);
                            }
                            else if (star.GetType().GetField("radius") != null)
                            {
                                radius = (float)star.GetType().GetField("radius").GetValue(star);
                            }
                            else
                            {
                                // Fallback if we can't find the right field
                                radius = 200f; // Default size
                            }
                            
                            if (radius > largestCachedRadiusDF)
                            {
                                largestCachedRadiusDF = radius;
                            }
                        }
                    }
                }
                
                // Fallback if we couldn't find a valid radius
                if (largestCachedRadiusDF <= 0f)
                    largestCachedRadiusDF = 1000f;
            }
            
            return largestCachedRadiusDF;
        }






        public static double ModifyRadius(double vanilla, double realRadius)
        {
            var negative = false;
            if (realRadius < 0)
            {
                negative = true;
                realRadius *= -1;
            }
            var diff = vanilla - 200.0;
            realRadius += diff;
            if (negative) realRadius *= -1;
            return realRadius;
        }
        public static T GetRadiusFromMecha<T>(T t, Mecha mecha)
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), mecha?.player?.planetData?.realRadius ?? 200.0);
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }
        public static T GetRadiusFromFactory<T>(T t, PlanetFactory factory)
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), factory?.planet?.realRadius ?? 200.0);
            // if (VFInput.alt) GS2.Log($"GetRadiusFromFactory Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} orig:{Convert.ToDouble(t)} returning {realRadius}");
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }
        public static float GetSquareRadiusFromAstroFactoryId(int id)
        {
            var factory = GameMain.data.spaceSector.skillSystem.astroFactories[id];
            var realRadius = factory.planet.realRadius * factory.planet.realRadius;
            if (VFInput.alt) GS2.Log($"GetRadiusSquareFromFactory Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} returning {realRadius}");
            return realRadius;
        }
        public static T GetRadiusFromAstroId<T>(T t, int id)
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), GameMain.data.galaxy?.astrosFactory[id]?.planet?.realRadius ?? 200.0);
            // if (VFInput.alt) GS2.Log($"GetRadiusFromAstroId Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} returning {realRadius}");
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }

        public static T GetRadiusFromLocalPlanet<T>(T t)
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), GameMain.localPlanet?.realRadius ?? 200.0);
            // if (VFInput.alt) GS2.Log($"GetRadius Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} orig:{Convert.ToDouble(t)} returning {realRadius}");
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }
        public static T GetRadiusFromEnemyData<T>(T t, ref EnemyData enemyData)
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), GameMain.galaxy.PlanetById(enemyData.astroId)?.realRadius??200.0);
            // if (VFInput.alt) GS2.Log($"GetRadiusFromFactory Called By {GS2.GetCaller(0)} {GS2.GetCaller(1)} {GS2.GetCaller(2)} orig:{Convert.ToDouble(t)} returning {realRadius}");
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }
        public static T GetRadiusFromAltitude<T>(T t, float alt) //Original / Altitude
        {
            var realRadius = ModifyRadius(Convert.ToDouble(t), Convert.ToDouble(alt));
            return (T)Convert.ChangeType(realRadius, typeof(T));
        }
        */
    }
}
