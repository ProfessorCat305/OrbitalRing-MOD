using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class UI
    {
        [HarmonyPatch(typeof(BuildPreview), nameof(BuildPreview.GetConditionText))]
        [HarmonyPostfix]
        public static void GetConditionTextPatch(BuildPreview __instance, EBuildCondition _condition, ref String __result)
        {
            if (_condition == (EBuildCondition)99) {
                __result = "同步轨道设施只能建设在特定位置".Translate();
            } else if (_condition == (EBuildCondition)98) {
                __result = "同步轨道核心设施只能建设在对应基座上".Translate();
            } else if (_condition == (EBuildCondition)97) {
                __result = "一个星环只能建造一座星环对撞机总控站".Translate();
            }
        }
    }
}
