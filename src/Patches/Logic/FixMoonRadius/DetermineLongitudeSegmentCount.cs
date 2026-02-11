using HarmonyLib;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.FixMoonRadius
{
    public partial class PatchOnPlanetGrid
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetGrid), "DetermineLongitudeSegmentCount")]
        public static bool DetermineLongitudeSegmentCount(PlanetGrid __instance, int latitudeIndex, int segment, ref int __result)
        {
            if (!DSPGame.IsMenuDemo) {
                if (segment < 4) segment = 4;
                if (!Lut.keyedLUTs.ContainsKey(segment))
                    Lut.SetLuts(segment, segment);
                if (Lut.keyedLUTs.ContainsKey(segment)) {
                    var index = Mathf.Abs(latitudeIndex) % (segment / 2);

                    if (index >= segment / 4) index = segment / 4 - index;
                    if (index < 0 || Lut.keyedLUTs[segment].Length == 0) {
                        __result = 4;
                        return false;
                    }

                    if (index > Lut.keyedLUTs[segment].Length - 1) __result = Lut.keyedLUTs[segment][Lut.keyedLUTs[segment].Length - 1];
                    else __result = Lut.keyedLUTs[segment][index];
                    //a = 1 * 1;
                }
                if (__result < 4) __result = 4;
                return false;
            }
            return true;
        }
    }
}