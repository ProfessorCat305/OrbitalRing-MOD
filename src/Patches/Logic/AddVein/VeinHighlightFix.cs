using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Patches.Logic.AddVein
{
    // 原版更新后给矿脉 shader 加了一个名为 "OUTLINE_VEIN_HIGHLIGHT_MASK" 的 pass。
    // 行星面板打开矿物高亮时：
    //   PlayerControlGizmo.DrawHighlightedVeinOutlines
    //     -> GPUInstancingManager.RenderSelectedModelInstancesWithCommandBuffer(..., "OUTLINE_VEIN_HIGHLIGHT_MASK")
    //        -> material.FindPass("OUTLINE_VEIN_HIGHLIGHT_MASK")
    // 把矿脉模型画进高亮遮罩。mod 的替换 shader 是旧版复刻的，没有这个 pass，
    // FindPass 返回 -1，于是“有颜色但不高亮”。
    //
    // 这里不改替换 shader：在录制高亮遮罩期间，临时把矿脉材质的 shader 换成原版 shader
    // （原版 shader 带这个 pass），等 CommandBuffer 执行完（OutlineRenderer.OnPostRender 结束）再换回来。
    // 高亮遮罩是在 OnPostRender 里、主画面渲染完之后录制的，因此不会影响正常渲染；
    // 另外在 OnPostRender 开头也会兜底还原一次，避免上一帧没还原导致矿脉变回灰色。
    internal static class VeinHighlightFix
    {
        private const string MaskPassName = "OUTLINE_VEIN_HIGHLIGHT_MASK";

        private static readonly List<Material> swappedMaterials = new List<Material>();
        private static readonly List<Shader> originalShaders = new List<Shader>();
        private static Shader vanillaStoneShader;
        private static Shader vanillaMetalShader;
        private static bool logged;
        private static bool warned;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GPUInstancingManager), nameof(GPUInstancingManager.RenderSelectedModelInstancesWithCommandBuffer))]
        public static void RenderSelectedModelInstancesWithCommandBuffer_Prefix(
            GPUInstancingManager __instance, int modelIndex, int[] modelIds, uint count, int lodIndex, string shaderPassName)
        {
            if (shaderPassName != MaskPassName) return;
            if (__instance == null || modelIds == null || count == 0U) return;

            ObjectRenderer objectRenderer = __instance.GetObjectRenderer(modelIndex);
            if (objectRenderer == null || objectRenderer.lodBatches == null || objectRenderer.lodBatches.Length == 0) return;

            if (lodIndex == -1) lodIndex = objectRenderer.lodBatches.Length - 1;
            if (lodIndex < 0 || lodIndex >= objectRenderer.lodBatches.Length) return;

            BatchRenderer batch = objectRenderer.lodBatches[lodIndex];
            if (batch == null || batch.materials == null) return;

            int materialCount = Mathf.Min(batch.subCount, batch.materials.Length);
            for (int i = 0; i < materialCount; i++) {
                Material material = batch.materials[i];
                if (material == null || material.shader == null) continue;
                if (material.FindPass(MaskPassName) >= 0) continue;

                Shader vanillaShader = PickVanillaShader(material.shader.name);
                if (vanillaShader == null) continue;

                swappedMaterials.Add(material);
                originalShaders.Add(material.shader);
                material.shader = vanillaShader;

                if (!logged) {
                    logged = true;
                    Debug.Log("[ProjectOrbitalRing] 矿物高亮：临时使用原版矿脉 shader 绘制高亮遮罩");
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OutlineRenderer), nameof(OutlineRenderer.OnPostRender))]
        public static void OutlineRenderer_OnPostRender_Prefix()
        {
            // 上一帧如果因为异常/提前 return 没有还原，这里兜底
            RestoreAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OutlineRenderer), nameof(OutlineRenderer.OnPostRender))]
        public static void OutlineRenderer_OnPostRender_Postfix()
        {
            // CommandBuffer 在 OnPostRender 内部执行，执行完再换回替换 shader
            RestoreAll();
        }

        private static void RestoreAll()
        {
            if (swappedMaterials.Count == 0) return;

            for (int i = 0; i < swappedMaterials.Count; i++) {
                if (swappedMaterials[i] != null) swappedMaterials[i].shader = originalShaders[i];
            }

            swappedMaterials.Clear();
            originalShaders.Clear();
        }

        private static Shader PickVanillaShader(string shaderName)
        {
            if (shaderName != null && shaderName.IndexOf("Metal", StringComparison.OrdinalIgnoreCase) >= 0) {
                if (vanillaMetalShader == null) vanillaMetalShader = Shader.Find("VF Shaders/Forward/PBR Standard Vein Metal");
                if (vanillaMetalShader == null && !warned) {
                    warned = true;
                    Debug.LogWarning("[ProjectOrbitalRing] 找不到原版 VF Shaders/Forward/PBR Standard Vein Metal，矿物高亮兜底失效");
                }
                return vanillaMetalShader;
            }

            if (vanillaStoneShader == null) vanillaStoneShader = Shader.Find("VF Shaders/Forward/PBR Standard Vein Stone");
            if (vanillaStoneShader == null && !warned) {
                warned = true;
                Debug.LogWarning("[ProjectOrbitalRing] 找不到原版 VF Shaders/Forward/PBR Standard Vein Stone，矿物高亮兜底失效");
            }
            return vanillaStoneShader;
        }
    }
}
