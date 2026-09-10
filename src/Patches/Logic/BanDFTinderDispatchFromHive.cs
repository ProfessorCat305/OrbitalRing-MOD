using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic
{
    internal class BanDFTinderDispatchFromHive
    {
        public static HashSet<int> DFTinderShouldNotDispatchStarId = new HashSet<int>();

        private static bool CheckStarIdCanDspatch(ref int starIndex)
        {
            int starId = starIndex + 1;
            //LogError($"scppppppppppppppppppppppppp CheckStarIdCanDspatch starId {starId}");
            if (DFTinderShouldNotDispatchStarId.Contains(starId)) {
                starIndex = -1;
                return false;
            }
            return true;
        }
        
        [HarmonyPatch(typeof(DFTinderComponent), nameof(DFTinderComponent.PrepareDispatchLogic))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DFTinderComponent_PrepareDispatchLogic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DFTinderComponent), nameof(DFTinderComponent.sortedStarIndices)))
                );
            object IL_023B = matcher.Advance(-2).Operand;
            object num15 = matcher.Advance(5).Operand;

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloca, num15),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BanDFTinderDispatchFromHive), nameof(CheckStarIdCanDspatch))),
                new CodeInstruction(OpCodes.Brfalse, IL_023B)
            );
            return matcher.InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.GameTickLogic))]
        public static void EnemyDFHiveSystem_GameTickLogic_Patch(EnemyDFHiveSystem __instance, long gameTick)
        {
            int num = (int)(gameTick % 600);
            if (num != 0) {
                return;
            }
            if (__instance.ticks <= 0) {
                return;
            }
            int cursor2 = __instance.tinders.cursor;
            DFTinderComponent[] buffer2 = __instance.tinders.buffer;
            for (int j = 1; j < cursor2; j++) {
                DFTinderComponent ptr4 = buffer2[j];
                if (ptr4.id == j) {
                    if (ptr4.targetHiveAstroId == 0) {
                        continue;
                    }
                    EnemyDFHiveSystem hiveByAstroId = __instance.sector.GetHiveByAstroId(ptr4.targetHiveAstroId);
                    if (DFTinderShouldNotDispatchStarId.Contains(hiveByAstroId.starData.id)) {
                        //LogError($"scppppppppppppppppppppppppp EnemyDFHiveSystem_KeyTickLogic_Patch starId {hiveByAstroId.starData.id}");
                        __instance.sector.KillEnemyFinal(ptr4.enemyId, ref CombatStat.empty);
                    }
                }
            }
        }
    }
}
