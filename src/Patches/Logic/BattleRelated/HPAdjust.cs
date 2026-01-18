using HarmonyLib;
using System.Reflection.Emit;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.BattleRelated
{
    internal class HPAdjust
    {
        internal static void OurSideHPAdjust()
        {
            ModelProto modelProto = LDB.models.Select(374); // 机枪塔
            modelProto.HpMax = 80000;

            modelProto = LDB.models.Select(373); // 激光塔
            modelProto.HpMax = 70000;

            modelProto = LDB.models.Select(375); // 加农炮
            modelProto.HpMax = 20000;

            modelProto = LDB.models.Select(448); // 原型机
            modelProto.HpMax = 45000;
            modelProto.prefabDesc.craftUnitAttackDamage0 = 2000;
            modelProto = LDB.models.Select(449); // 攻击（原精准
            modelProto.HpMax = 75000;
            modelProto.prefabDesc.craftUnitAttackDamage0 = 2500;

            modelProto = LDB.models.Select(450); // 精准（原攻击
            modelProto.HpMax = 24000;
            modelProto.prefabDesc.craftUnitAttackDamage0 = 7500;
            modelProto.prefabDesc.craftUnitAttackRange0 = 85f;
            modelProto.prefabDesc.craftUnitSensorRange = 100f;
            modelProto.prefabDesc.craftUnitMaxMovementSpeed = 15f;
            
            modelProto = LDB.models.Select(451); // 护卫
            modelProto.HpMax = 450000;
            modelProto.prefabDesc.craftUnitAttackDamage0 = 12500;
            modelProto = LDB.models.Select(452); // 驱逐
            modelProto.HpMax = 1600000;
            modelProto.prefabDesc.craftUnitAttackDamage0 = 15000;
            modelProto.prefabDesc.craftUnitAttackDamage1 = 150000;
            modelProto.prefabDesc.craftUnitAttackRange0 = 3800f;

            modelProto = LDB.models.Select(482); // 地面电浆
            modelProto.HpMax = 120000;

        }


        internal static void ModifyEnemyHpUpgrade()
        {
            //273 displayName 中枢核心
            //274 displayName 日蚀要塞装配港
            //275 displayName 巨鲸装配港
            //276 displayName 枪骑装配港
            //277 displayName 光能接收站
            //278 displayName 等离子发生塔
            //279 displayName 相位激光塔
            //280 displayName 种子节点
            //281 displayName 水平连接桥
            //282 displayName 纵向通道
            //283 displayName 日蚀要塞
            //284 displayName 巨鲸
            //285 displayName 枪骑
            //286 displayName 高速拦截机
            //287 displayName 小型炸弹机
            //288 displayName 中继站
            //289 displayName 工蚁
            //290 displayName 重型运输船
            //291 displayName 火种
            //292 displayName 行星基地
            //293 displayName 强袭者营地
            //294 displayName 游骑兵营地
            //295 displayName 守卫者营地
            //296 displayName 高能激光塔
            //297 displayName 等离子哨戒塔
            //298 displayName 等离子护盾
            //299 displayName 导轨
            //300 displayName 强袭者
            //301 displayName 游骑兵
            //302 displayName 守卫者
            //303 displayName 运输车

            ModelProto modelProto;
            // 黑雾建筑掉落的沙土乘以5倍，单位掉落沙土清零
            for (int i = 292; i <= 299; i++)
            {
                modelProto = LDB.models.Select(i);
                modelProto.prefabDesc.enemySandCount *= 5;
            }

            modelProto = LDB.models.Select(ProtoID.M导轨);
            modelProto.HpMax *= 3;
            modelProto.HpUpgrade *= 3;
            modelProto.HpRecover *= 3;

            modelProto = LDB.models.Select(ProtoID.M强袭者);
            modelProto.HpUpgrade = 2500;
            modelProto.prefabDesc.unitAttackRange0 = 10;
            modelProto.prefabDesc.enemySandCount = 0;

            modelProto = LDB.models.Select(ProtoID.M游骑兵);
            modelProto.HpUpgrade = 1000;
            modelProto.prefabDesc.unitAttackDamageInc0 = 250;
            modelProto.prefabDesc.enemySandCount = 0;

            modelProto = LDB.models.Select(ProtoID.M守卫者);
            modelProto.HpMax = 62000;
            modelProto.HpUpgrade = 4500;
            modelProto.prefabDesc.unitMaxMovementSpeed = 5;
            modelProto.prefabDesc.unitMarchMovementSpeed = 5;
            modelProto.prefabDesc.unitAttackDamage0 = 36000;
            modelProto.prefabDesc.unitAttackDamageInc0 = 2500;
            modelProto.prefabDesc.unitAttackRange0 = 10;
            modelProto.prefabDesc.enemySandCount = 0;

            modelProto = LDB.models.Select(284);
            modelProto.HpMax = 750000;
            modelProto.HpUpgrade = 30000;

            modelProto = LDB.models.Select(285);
            modelProto.HpMax = 450000;
            modelProto.HpUpgrade = 25000;

            modelProto = LDB.models.Select(ProtoID.M高能激光塔);
            modelProto.prefabDesc.dfTurretAttackDamage = 26000;
            modelProto.prefabDesc.dfTurretAttackDamageInc = 3000;
        }

        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.CalculateDamageIncoming))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillSystem_CalculateDamageIncoming_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Div));

            // 太空黑雾护甲，每级0.5提升到每级2
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_2))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Mul));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_5), new CodeMatch(OpCodes.Div));

            // 地面黑雾护甲，每级0.2提升到每级1
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Mul));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByLocalCaster))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByRemoteCaster))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillSystem_DamageGroundObjectByCaster_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_5), new CodeMatch(OpCodes.Div));

            // 地面黑雾护甲，每级0.2提升到每级1
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Mul));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageObject))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillSystem_DamageObject_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Div));

            // 太空黑雾护甲，每级0.5提升到每级2
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_2))
               .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Mul));

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(UIEnemyBriefInfo), nameof(UIEnemyBriefInfo._OnUpdate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UIEnemyBriefInfo__OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 0.2f), new CodeMatch(OpCodes.Stloc_S));

            // 地面黑雾护甲，每级0.2提升到每级1
            matcher.SetOperandAndAdvance(1f);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 0.5f), new CodeMatch(OpCodes.Stloc_S));

            // 太空黑雾护甲，每级0.5提升到每级2
            matcher.SetOperandAndAdvance(2f);

            return matcher.InstructionEnumeration();
        }
    }
}
