using HarmonyLib;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic
{
    // 配送机（ItemProto 5003）的三维位置不是在 C# 里逐帧移动的：
    // DispenserComponent.InternalTick 只维护 CourierData（begin/end/direction/maxt/t/...），
    // 真正的轨迹由 LogisticCourierRenderer 写入 ComputeBuffer 后，
    // 交给 GPU shader "VF Shaders/Forward/Logistic Courier Instancing" 计算。
    //
    // 反汇编该 shader（VS 关键指令）后确认，飞行弧线里有一段"最低绝对半径"的硬编码：
    //     lift = max(h0 * 3.5 + 202.6 - 0.5 * (|begin| + |end|), 0)
    //     pos += radial * lift * arcProfile
    // 202.6 是按 200 半径星球写的：
    //   - 200 半径星球：lift ≈ 7，配送机贴地飞行，正常；
    //   - 100 半径星球：lift ≈ 107，配送机飞出配送器后先爬到约 200 半径的高度再降下来。
    // 拖尾 shader（Logistic Courier Effect Instancing）里是同一个常量。
    //
    // 这个 lift 只取决于常量 202.6 和 |begin|+|end|，而 |begin|/|end| 同时决定整条轨迹的
    // 半径，数据层没有自由度把弧线单独压下来（把 begin/end 放大到 200 只会让整条轨迹也飞到 200）。
    // 因此这里把两个 shader 换成按反汇编复刻的替换版：
    //   assets/shaders/LogisticCourier.shader        （本体）
    //   assets/shaders/LogisticCourierEffect.shader  （拖尾）
    // 替换版只在顶点路径里把 202.6 换成 _PlanetRadius + 2.6，其余逻辑与原版一致。
    //
    // 只对非 200 半径星球生效：200 半径星球保持原版 shader，观感不变。
    internal static class LogisticCourierHeightFix
    {
        private const float VanillaPlanetRadius = 200f;

        // 到达目标时的悬停高度（原版反汇编里是 +1.13）。
        // 这个值同时抬高整条轨迹；调它就是改“到达目标时离地多高”，改完重编 DLL 即可，不用重打 AB。
        private const float CourierHoverHeight = 1.13f;

        private const string CourierShaderName = "VF Shaders/Forward/Logistic Courier Instancing";
        private const string CourierEffectShaderName = "VF Shaders/Forward/Logistic Courier Effect Instancing";

        private static Shader courierShader;
        private static Shader courierEffectShader;

        public static void SetShaders(Shader body, Shader effect)
        {
            courierShader = body;
            courierEffectShader = effect;
        }

        private static bool IsOurShader(Shader shader)
        {
            return shader != null && (shader == courierShader || shader == courierEffectShader);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticCourierRenderer), MethodType.Constructor, new[] { typeof(PlanetTransport) })]
        public static void LogisticCourierRenderer_Ctor_Postfix(LogisticCourierRenderer __instance)
        {
            if (DSPGame.IsMenuDemo) return;

            PlanetTransport transport = __instance.transport;
            if (transport == null || transport.planet == null) return;

            // 200 半径星球上原版 shader 的 202.6 正好是“200 + 一点”，不需要改
            float planetRadius = transport.planet.realRadius;
            if (Mathf.Abs(planetRadius - VanillaPlanetRadius) < 1f) return;

            if (courierShader == null) {
                Debug.LogWarning("[ProjectOrbitalRing] 配送机替换 shader 未加载，高度修正未生效");
                return;
            }

            Material[] materials = __instance.courierMats;
            if (materials == null) return;

            int swapped = 0;
            for (int i = 0; i < materials.Length; i++) {
                Material material = materials[i];
                if (material == null || material.shader == null) continue;

                string shaderName = material.shader.name;
                if (shaderName == CourierShaderName) {
                    material.shader = courierShader;
                    material.SetFloat("_PlanetRadius", planetRadius);
                    material.SetFloat("_HoverHeight", CourierHoverHeight);
                    swapped++;
                } else if (shaderName == CourierEffectShaderName && courierEffectShader != null) {
                    material.shader = courierEffectShader;
                    material.SetFloat("_PlanetRadius", planetRadius);
                    material.SetFloat("_HoverHeight", CourierHoverHeight);
                    swapped++;
                }
            }

            if (swapped == 0) {
                Debug.LogWarning("[ProjectOrbitalRing] 配送机材质里没有找到 " + CourierShaderName);
            }
        }

        // 星球半径/速度在 shader 里是材质属性，绘制前再确认一次（避免材质被复用或参数被重置）
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LogisticCourierRenderer), nameof(LogisticCourierRenderer.Draw))]
        public static void LogisticCourierRenderer_Draw_Prefix(LogisticCourierRenderer __instance)
        {
            Material[] materials = __instance.courierMats;
            if (materials == null) return;

            PlanetTransport transport = __instance.transport;
            if (transport == null || transport.planet == null) return;

            float planetRadius = transport.planet.realRadius;
            float speed = GameMain.history.logisticCourierSpeedModified;

            for (int i = 0; i < materials.Length; i++) {
                Material material = materials[i];
                if (material == null || !IsOurShader(material.shader)) continue;

                material.SetFloat("_PlanetRadius", planetRadius);
                material.SetFloat("_HoverHeight", CourierHoverHeight);
                material.SetFloat("_CourierSpeed", speed);
            }
        }
    }
}
