using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic
{
    public static class SpaceMissilePatches
    {
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.UpdateOtherGlobalTargets))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DefenseSystem_UpdateOtherGlobalTargets_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_0), new CodeMatch(OpCodes.Ldnull));

            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_3), new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldelem_Ref));

            matcher.Advance(1).SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop));

            return matcher.InstructionEnumeration();
        }

        // ===================== 启示录聚变弹 / 战术核导弹 的爆炸特效 =====================
        // 原版 GeneralMissile.TickSkillLogic 里，命中时塞进 skillSystem.hitEffects 的 ParticleData 字段是写死的：
        //   duration：全方法 8 处赋值，7 处是常量 50（另 1 处是算出来的，不碰）；
        //   size：全方法 10 处赋值，紧跟在 ldc.r4 常量后的有 7 处 ——
        //         地表命中 1.8f（IL_08EC / IL_0CD6 / IL_1057 / IL_1A41 / IL_24BA，共 5 处）、
        //         太空命中 6f（IL_10B9 / IL_2519，共 2 处）；
        //         另外 3 处不是常量（打护盾 1.2f、打中玩家 1.3f、以及一处变量 num46），保持原样。
        // 这两型弹药现在都复制原版模型 491，外观一致，区别只在本文件下面这几个常数。
        // 只有 813 / 818 会走这套换算，其它弹药仍然是原版常量
        // （size 和 duration 都是每条 ParticleData 各自的字段，不是共享材质参数，所以不会连累原版弹药）。

        // ===== 调「爆炸大小」就改这两个数：数值就是原版的 size，原版导弹地表命中是 1.8 =====
        // 启示录聚变弹（模型 813，伤害半径 115）
        private const float ApocalypseHitEffectSize = 5.5f;
        // 战术核导弹（模型 818，伤害半径 50）
        private const float NukeHitEffectSize = 3.5f;

        // ===== 调「爆炸时长」就改这两个数：单位 tick，60 tick = 1 秒，原版是 50（约 0.83 秒）=====
        // duration 同时是 shader 的动画长度，调大 = 放慢 + 停留更久
        // 启示录聚变弹
        private const int ApocalypseHitEffectDuration = 100;
        // 战术核导弹
        private const int NukeHitEffectDuration = 75;

        // 原版常量，用于识别替换点，以及给其它弹药返回原值
        private const float VanillaPlanetHitEffectSize = 1.8f;
        private const float VanillaSpaceHitEffectSize = 6f;
        private const int VanillaHitEffectDuration = 50;

        // 太空命中沿用原版 6 : 1.8 的比例，从地表常数派生，避免再多两个旋钮
        private const float SpaceToPlanetSizeRatio = VanillaSpaceHitEffectSize / VanillaPlanetHitEffectSize;

        // 启动日志自检用：期望替换点数量（尺寸 5 处 1.8 + 2 处 6；时长 7 处常量 50）
        private const int ExpectedSizeReplacedCount = 7;
        private const int ExpectedDurationReplacedCount = 7;

        /// <summary>地表命中的爆炸 size。只有这两型弹药返回自定义值，其它弹药返回原版 1.8f。</summary>
        public static float GetPlanetHitEffectSize(int modelIndex)
        {
            switch (modelIndex) {
                case ProtoID.M启示录聚变弹:
                    return ApocalypseHitEffectSize;
                case ProtoID.M战术核导弹:
                    return NukeHitEffectSize;
                default:
                    return VanillaPlanetHitEffectSize;
            }
        }

        /// <summary>太空命中的爆炸 size，按原版 6 : 1.8 的比例从地表值派生。</summary>
        public static float GetSpaceHitEffectSize(int modelIndex) =>
            GetPlanetHitEffectSize(modelIndex) * SpaceToPlanetSizeRatio;

        /// <summary>命中爆炸特效的存活帧数。只有这两型弹药返回自定义值，其它弹药返回原版 50。</summary>
        public static int GetHitEffectDuration(int modelIndex)
        {
            switch (modelIndex) {
                case ProtoID.M启示录聚变弹:
                    return ApocalypseHitEffectDuration;
                case ProtoID.M战术核导弹:
                    return NukeHitEffectDuration;
                default:
                    return VanillaHitEffectDuration;
            }
        }

        private static bool IsParticleFieldStore(CodeInstruction instruction, string fieldName)
        {
            if (instruction.opcode != OpCodes.Stfld) {
                return false;
            }
            var field = instruction.operand as FieldInfo;
            return field != null && field.DeclaringType == typeof(ParticleData) && field.Name == fieldName;
        }

        [HarmonyPatch(typeof(GeneralMissile), nameof(GeneralMissile.TickSkillLogic))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GeneralMissile_TickSkillLogic_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var result = new List<CodeInstruction>(instructions);
            FieldInfo modelIndexField = AccessTools.Field(typeof(GeneralMissile), nameof(GeneralMissile.modelIndex));
            MethodInfo planetSize = AccessTools.Method(typeof(SpaceMissilePatches), nameof(GetPlanetHitEffectSize));
            MethodInfo spaceSize = AccessTools.Method(typeof(SpaceMissilePatches), nameof(GetSpaceHitEffectSize));
            MethodInfo effectDuration = AccessTools.Method(typeof(SpaceMissilePatches), nameof(GetHitEffectDuration));

            var patches = new List<(int Index, MethodInfo Method, bool IsSize)>();
            var sizeSequence = new List<string>();
            for (int i = 1; i < result.Count; i++) {
                if (IsParticleFieldStore(result[i], nameof(ParticleData.size))) {
                    CodeInstruction previous = result[i - 1];
                    if (previous.opcode != OpCodes.Ldc_R4) {
                        continue;
                    }
                    float vanillaSize = (float)previous.operand;
                    if (vanillaSize == VanillaPlanetHitEffectSize) {
                        patches.Add((i - 1, planetSize, true));
                        sizeSequence.Add(vanillaSize.ToString("0.###"));
                    } else if (vanillaSize == VanillaSpaceHitEffectSize) {
                        patches.Add((i - 1, spaceSize, true));
                        sizeSequence.Add(vanillaSize.ToString("0.###"));
                    }
                } else if (IsParticleFieldStore(result[i], nameof(ParticleData.duration))) {
                    CodeInstruction previous = result[i - 1];
                    if (previous.opcode != OpCodes.Ldc_I4_S && previous.opcode != OpCodes.Ldc_I4) {
                        continue;
                    }
                    if (Convert.ToInt32(previous.operand) != VanillaHitEffectDuration) {
                        continue;
                    }
                    patches.Add((i - 1, effectDuration, false));
                }
            }

            // 从后往前替换，插入指令不会让前面待处理的位置失效
            patches.Sort((a, b) => b.Index.CompareTo(a.Index));
            int sizeCount = 0;
            int durationCount = 0;
            foreach (var patch in patches) {
                CodeInstruction original = result[patch.Index];
                // 把常量换成：ldarg.0（取 this）→ ldfld modelIndex → call 换算方法，
                // 栈上仍然是「ParticleData 引用 + 一个数值」，后面的 stfld 不受影响
                result[patch.Index] = new CodeInstruction(OpCodes.Ldarg_0) { labels = original.labels, blocks = original.blocks };
                result.Insert(patch.Index + 1, new CodeInstruction(OpCodes.Ldfld, modelIndexField));
                result.Insert(patch.Index + 2, new CodeInstruction(OpCodes.Call, patch.Method));
                if (patch.IsSize) {
                    sizeCount++;
                } else {
                    durationCount++;
                }
            }

            string message = $"[MissileExplosion] GeneralMissile.TickSkillLogic 替换点: 尺寸 {sizeCount} 处（原常量 {string.Join(", ", sizeSequence)}）、" +
                             $"时长 {durationCount} 处";
            if (sizeCount == ExpectedSizeReplacedCount && durationCount == ExpectedDurationReplacedCount) {
                global::ProjectOrbitalRing.ProjectOrbitalRing.LogInfo(message);
            } else {
                global::ProjectOrbitalRing.ProjectOrbitalRing.LogWarning(
                    message + $"，预期 尺寸 {ExpectedSizeReplacedCount} / 时长 {ExpectedDurationReplacedCount}，" +
                    "放大或延时可能不完整，请把该函数的反编译 IL 提供给作者核对");
            }
            return result;
        }
    }
}
