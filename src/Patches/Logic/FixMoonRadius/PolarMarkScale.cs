using HarmonyLib;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.FixMoonRadius
{
    /// <summary>
    /// 行星视图/蓝图视图里，南北极那两条蓝红色的“地轴”指示线是 <see cref="UIGame.polarMark"/>。
    /// 原版这个物体的缩放固定为 200（对应默认半径 200 的星球），所以半径 100 的卫星上，
    /// 轴线会停在半径 200 的位置，看起来比地面高 100。
    /// 这里按照当前星球的实际半径缩放它；GalacticScale 也是这样处理的。
    /// </summary>
    internal static class PolarMarkScale
    {
        private const float DefaultRadius = 200f;

        private static PlanetData currentPlanet;
        private static float currentRadius = -1f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        public static void UIGame_OnUpdate_Postfix(UIGame __instance)
        {
            if (__instance.polarMark == null) return;

            PlanetData planet = GameMain.localPlanet;
            if (planet == null) {
                // 离开星球（比如在太空里）时恢复原版缩放，避免下次进入星球前残留上一个星球的值
                if (currentPlanet != null) {
                    currentPlanet = null;
                    currentRadius = -1f;
                    __instance.polarMark.transform.localScale = new Vector3(DefaultRadius, DefaultRadius, DefaultRadius);
                }
                return;
            }

            float radius = planet.realRadius;
            if (planet == currentPlanet && Mathf.Approximately(radius, currentRadius)) return;

            currentPlanet = planet;
            currentRadius = radius;
            __instance.polarMark.transform.localScale = new Vector3(radius, radius, radius);
        }
    }
}
