using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProjectOrbitalRing.Patches
{
    public static class DysonSpherePatches
    {
        [HarmonyPatch(typeof(DysonFrame), nameof(DysonFrame.segCount), MethodType.Getter)]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DysonFrame_segCount_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Mul), new CodeMatch(OpCodes.Stloc_0));

            // DysonFrame count /= 2
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop)).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(DysonShell), nameof(DysonShell.GenerateGeometry))]
        [HarmonyPatch(typeof(DysonShell), nameof(DysonShell.Import))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DysonShell_cpPerVertex_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonShell), nameof(DysonShell.cpPerVertex))));

            // DysonShell count /= 2
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Div));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Init))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DysonSphere_Init_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            // energyGenPerSail * 2
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.energyGenPerSail))));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4, 10), new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Mul));

            // energyGenPerNode * 2
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.energyGenPerNode))));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_2), new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Mul));

            // energyGenPerFrame * 2
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.energyGenPerFrame))));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_2), new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Mul));

            // energyGenPerShell * 2
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonSphere), nameof(DysonSphere.energyGenPerShell))));

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4, 10), new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Mul));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.RequiresCurrent_Gamma))]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.MaxOutputCurrent_Gamma))]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Gamma_Req))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> EnergyCap_Gamma_Req_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            // 射线接收站，光子模式功率拉齐发电模式1倍，然后在prefabDescs.json统一改成800倍
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 8f));
            matcher.SetOperandAndAdvance(1f);
            return matcher.InstructionEnumeration();
        }

    }
}
