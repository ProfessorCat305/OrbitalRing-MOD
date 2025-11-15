using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic
{
    internal class SailNeedFuel
    {
        private static bool UseFuelRod(Player player, int count)
        {
            int itemId = ProtoID.I化学燃料罐;
            int useCount = count;
            player.TakeItemFromPlayer(ref itemId, ref count, out _, true, null);

            if (itemId != ProtoID.I化学燃料罐) return false;

            if (count != useCount) return false;

            player.mecha.AddConsumptionStat(itemId, count, player.nearestFactory);

            return true;
        }
        private static bool CheckSailFuel(PlayerMove_Fly instance)
        {
            double rodNeeded = 15;
            //Debug.LogFormat("T驱动引擎4TechUnlocked {0} T驱动引擎3TechUnlocked {1}", GameMain.history.TechUnlocked(ProtoID.T驱动引擎4), GameMain.history.TechUnlocked(ProtoID.T驱动引擎3));
            if (GameMain.history.TechUnlocked(ProtoID.T驱动引擎4)) {
                return true;
            }
            if (GameMain.history.TechUnlocked(ProtoID.T驱动引擎3)) {
                rodNeeded = 7.5;
            }
            double weight = 0;
            int rodCount = 0;
            ItemProto item;
            int itemId = 0;
            for (int i = 0; i < instance.player.package.size; i++) {
                itemId = instance.player.package.grids[i].itemId;
                if (itemId != 0) {
                    if (itemId != ProtoID.I化学燃料罐) {
                        item = LDB.items.Select(instance.player.package.grids[i].itemId);
                        weight += (double)instance.player.package.grids[i].count / item.StackSize;
                    } else {
                        rodCount += instance.player.package.grids[i].count;
                    }
                }
            }
            for (int i = 0; i < instance.player.deliveryPackage.gridLength; i++) {
                itemId = instance.player.deliveryPackage.grids[i].itemId;
                if (itemId != 0) {
                    item = LDB.items.Select(instance.player.deliveryPackage.grids[i].itemId);
                    weight += (double)instance.player.deliveryPackage.grids[i].count / item.StackSize;
                }
            }
            if (instance.player.inhandItemId != 0) {
                item = LDB.items.Select(instance.player.inhandItemId);
                weight += (double)instance.player.inhandItemCount / item.StackSize;
            }
            if (instance.player.planetData.radius == 100f) {
                weight *= 0.1d;
            }
            int count = (int)Math.Ceiling(weight * rodNeeded);
            double extraFuelWeight = (double)(rodCount - count) / 30;
            if (extraFuelWeight > 0) {
                if (instance.player.planetData.radius == 100f) {
                    extraFuelWeight *= 0.1;
                }
                count += (int)Math.Ceiling(extraFuelWeight * rodNeeded);
            }
            //Debug.LogFormat("weight {0} count {1} rodNeeded {2}", weight, count, rodNeeded);
            if (instance.player.package.GetItemCount(ProtoID.I化学燃料罐) < count) {
                UIRealtimeTip.Popup("背包化学燃料棒不足，需要".TranslateFromJson() + count + "个".TranslateFromJson());

                return false;
            }
            if (count > 0) {
                if (!UseFuelRod(instance.player, count)) {
                    UIRealtimeTip.Popup("B背包化学燃料棒不足".TranslateFromJson());

                    return false;
                }
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerMove_Fly), nameof(PlayerMove_Fly.GameTick))]
        public static IEnumerable<CodeInstruction> PlayerMove_Fly_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Blt));

            object IL_03CE = matcher.Operand; // 变量索引

            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SailNeedFuel), nameof(CheckSailFuel))),
                new CodeInstruction(OpCodes.Brfalse_S, IL_03CE)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }
    }
}
