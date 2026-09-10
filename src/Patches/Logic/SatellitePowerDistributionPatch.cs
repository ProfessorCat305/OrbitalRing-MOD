using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic
{
    public static class SatellitePowerDistributionPatch
    {
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BuildTool_Click_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            // 匹配 ldc.r4 4900 指令
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 4900f));

            // 将操作数从 4900 修改为 32580f，将电力连线距离最大从70改为180.5
            matcher.SetAndAdvance(OpCodes.Ldc_R4, 25600f);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 4900f));
            matcher.SetAndAdvance(OpCodes.Ldc_R4, 25600f);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 4900f));
            matcher.SetAndAdvance(OpCodes.Ldc_R4, 25600f);

            return matcher.InstructionEnumeration();
        }

        public static void ChangeWeiXinPowerPoint()
        {
            ModelProto oriModel = LDB.models.Select(68);
            oriModel.prefabDesc.powerPoint.y += 55; // 将卫星的电点上移55
            oriModel = LDB.models.Select(ProtoID.M勘察卫星);
            oriModel.prefabDesc.powerPoint.y += 55; // 将卫星的电点上移55

            oriModel = LDB.models.Select(ProtoID.M天枢座);
            oriModel.prefabDesc.powerPoint.y += 20; // 将卫星的电点上移20

            oriModel = LDB.models.Select(ProtoID.M超空间中继器);
            oriModel.prefabDesc.powerPoint = new UnityEngine.Vector3
            {
                x = 0,
                y = 0,
                z = 0
            };
        }
    }
}
