using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOrbitalRing.Patches.Logic
{
    internal class BanDFTinderDispatchFromHive
    {
        public static HashSet<int> DFTinderShouldNotDispatchStarId = new HashSet<int>();
        private static bool CheckStarIdCanDspatch(int starIndex)
        {
            int starId = starIndex + 1;
            if (DFTinderShouldNotDispatchStarId.Contains(starId)) {
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
                new CodeInstruction(OpCodes.Ldloc, num15),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BanDFTinderDispatchFromHive), nameof(CheckStarIdCanDspatch))),
                new CodeInstruction(OpCodes.Brfalse, IL_023B)
            );
            return matcher.InstructionEnumeration();
        }
    }
}
