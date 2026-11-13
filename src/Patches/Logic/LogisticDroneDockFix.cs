using HarmonyLib;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic
{
    // 物流运输机的起降段位置由 ItemProto 5001 的 shader
    // （VF Shaders/Forward/Logistic Drone Instancing）在 GPU 上计算，
    // 该 shader 把“星球表面半径”硬编码成了 200：
    //
    //   radius(t) = lerp(|begin|, |end|, smoothstep(t / maxt))
    //             + 8
    //             - 0.4 * |t - clamp(t, 0, maxt)|^1.5 * (lerp(|begin|, |end|, ...) - 200)
    //
    // t 在 [0, maxt] 的飞行段只和 begin/end 有关，是正常的；
    // t < 0（起飞）和 t > maxt（抵达）的升降段则被那个 200 影响：
    // 半径 100 的星球上，运输机会先出现在离地约 80 的位置再下降到停机点，
    // 抵达时反过来上升到同样的高度才消失。
    //
    // DroneData.begin/end 是给 GPU 的只读数据，游戏逻辑并不使用渲染器里的副本，
    // 所以这里在 LogisticDroneRenderer.Update 把数据写进 ComputeBuffer 之后，
    // 只修改渲染副本：把升降段的 t 夹到 0 / maxt，并按真实星球半径重新缩放
    // begin / end 的模长，让 shader 算出来的升降终点落在建筑内部。
    // 飞行段（0 <= t <= maxt）完全不动。
    internal static class LogisticDroneDockFix
    {
        private const float VanillaPlanetRadius = 200f;
        private const float DockPhaseDuration = 1.5f;
        // 0.4 * 1.5^1.5，即原 shader 在 |t| = 1.5 时的漂移系数
        private const float DockDriftAtEnd = 0.7348469f;
        // 原 shader 给飞行弧线额外抬高的距离
        private const float ArcHeight = 8f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticDroneRenderer), nameof(LogisticDroneRenderer.Update))]
        public static void LogisticDroneRenderer_Update_Postfix(LogisticDroneRenderer __instance)
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return;
            }

            PlanetTransport transport = __instance.transport;
            if (transport == null) {
                return;
            }

            PlanetData planet = transport.planet;
            if (planet == null) {
                return;
            }

            float planetRadius = planet.realRadius;
            // 原版 200 半径星球上 shader 的硬编码值正好等于真实半径，不需要改
            if (Mathf.Abs(planetRadius - VanillaPlanetRadius) < 0.01f) {
                return;
            }

            DroneData[] drones = __instance.dronesArr;
            int count = __instance.droneCount;
            if (drones == null || count <= 0 || count > drones.Length) {
                return;
            }

            bool changed = false;
            for (int i = 0; i < count; i++) {
                DroneData drone = drones[i];
                float maxt = drone.maxt;
                if (maxt <= 0f) {
                    continue;
                }

                if (drone.t < 0f) {
                    // 起飞段：从 begin 所在建筑内部升到 begin 上方的飞行弧线起点
                    if (!TryFixDockRadius(ref drone.begin, planetRadius, 1f + drone.t / DockPhaseDuration)) {
                        continue;
                    }
                    drone.t = 0f;
                } else if (drone.t > maxt) {
                    // 抵达段：从 end 上方的飞行弧线终点降入 end 所在建筑内部
                    if (!TryFixDockRadius(ref drone.end, planetRadius, 1f - (drone.t - maxt) / DockPhaseDuration)) {
                        continue;
                    }
                    drone.t = maxt;
                } else {
                    continue;
                }

                drones[i] = drone;
                changed = true;
            }

            if (changed && __instance.dronesBuffer != null) {
                __instance.dronesBuffer.SetData(drones, 0, 0, count);
            }
        }

        // 把停机点向量缩放到“建筑内部点 -> 停机点”的插值位置。
        // progress: 0 = 建筑内部，1 = 停机点（飞行弧线起点/终点）。
        // shader 在 t 被夹到 0 / maxt 时输出 dir * (|dock| + 8)，
        // 所以这里把模长设成 desiredRadius - 8 即可。
        private static bool TryFixDockRadius(ref Vector3 dock, float planetRadius, float progress)
        {
            float dockRadius = dock.magnitude;
            if (dockRadius <= ArcHeight || dockRadius <= 0.0001f) {
                return false;
            }

            float arcRadius = dockRadius + ArcHeight;
            // 原版 200 半径星球在 t = ±1.5 时的落点：
            //   radius = dockRadius + 8 - 0.4 * 1.5^1.5 * (dockRadius - 200)
            // 换成真实星球半径，保持同样的“深入建筑内部”比例
            float innerRadius = arcRadius - DockDriftAtEnd * (dockRadius - planetRadius);

            float t = Mathf.Clamp01(progress);
            float radius = Mathf.Lerp(innerRadius, arcRadius, Mathf.SmoothStep(0f, 1f, t));
            float targetMagnitude = radius - ArcHeight;
            if (targetMagnitude < 0f) {
                targetMagnitude = 0f;
            }

            dock *= targetMagnitude / dockRadius;
            return true;
        }
    }
}
