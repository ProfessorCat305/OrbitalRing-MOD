using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.CurvatureLogistics
{
    internal class CurvatureLogistics
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "OnPlayerIntendToTransferItems")]
        public static bool OnPlayerIntendToTransferItemsPatch(UIControlPanelStationInspector __instance, int _itemId, int _itemCount, int _itemInc)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }
            if (_itemId == 5001)
            {
                if (__instance.droneIconButton.button.interactable)
                {
                    __instance.OnDroneIconClick(_itemId);
                    return false;
                }
            }
            else if (stationComponent.isStellar && (_itemId == ProtoID.I太空运输船 || _itemId == ProtoID.I深空货舰))
            {
                //Debug.LogFormat("scppppppp114514 _itemId {0}", _itemId);
                if (__instance.shipIconButton.button.interactable)
                {
                    __instance.OnShipIconClick(_itemId);
                    return false;
                }
            }
            else if (stationComponent.isStellar && _itemId == 1210 && __instance.warperIconButton.button.interactable)
            {
                __instance.OnWarperIconClick(_itemId);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "OnShipIconClick")]
        public static bool OnShipIconClickPatch(UIControlPanelStationInspector __instance, int obj)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }
            if (!stationComponent.isStellar)
            {
                return false;
            }
            if (stationComponent.isCollector || stationComponent.isVeinCollector)
            {
                return false;
            }
            if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount == 0)
            {
                __instance.player.SetHandItems(0, 0, 0);
                return false;
            }
            if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount > 0)
            {
                //Debug.LogFormat("scppppppppppp112 inhandItemId {0}", __instance.player.inhandItemId);
                int num = ProtoID.I太空运输船;
                if (__instance.station.energyMax == 12000000000)
                {
                    num = ProtoID.I深空货舰;
                }

                ItemProto itemProto = LDB.items.Select(num);
                if (__instance.player.inhandItemId != num)
                {
                    UIRealtimeTip.Popup("只能放入".Translate() + itemProto.name, true, 0);
                    return false;
                }
                ItemProto itemProto2 = LDB.items.Select((int)__instance.factory.entityPool[stationComponent.entityId].protoId);
                int num2 = (itemProto2 != null) ? itemProto2.prefabDesc.stationMaxShipCount : 10;
                int num3 = stationComponent.idleShipCount + stationComponent.workShipCount;
                int num4 = num2 - num3;
                if (num4 < 0)
                {
                    num4 = 0;
                }
                int num5 = (__instance.player.inhandItemCount < num4) ? __instance.player.inhandItemCount : num4;
                if (num5 <= 0)
                {
                    UIRealtimeTip.Popup("栏位已满".Translate(), true, 0);
                    return false;
                }
                int inhandItemCount = __instance.player.inhandItemCount;
                int inhandItemInc = __instance.player.inhandItemInc;
                int num6 = num5;
                int num7 = __instance.split_inc(ref inhandItemCount, ref inhandItemInc, num6);
                stationComponent.idleShipCount += num6;
                __instance.player.AddHandItemCount_Unsafe(-num6);
                __instance.player.SetHandItemInc_Unsafe(__instance.player.inhandItemInc - num7);
                if (__instance.player.inhandItemCount <= 0)
                {
                    __instance.player.SetHandItemId_Unsafe(0);
                    __instance.player.SetHandItemCount_Unsafe(0);
                    __instance.player.SetHandItemInc_Unsafe(0);
                    return false;
                }
            }
            else if (__instance.player.inhandItemId == 0 && __instance.player.inhandItemCount == 0)
            {
                if (!__instance.isLocal)
                {
                    UIRealtimeTip.Popup("非本地星球拿取提示".Translate(), true, 0);
                    return false;
                }
                int idleShipCount = stationComponent.idleShipCount;
                if (idleShipCount <= 0)
                {
                    return false;
                }
                if (VFInput.shift || VFInput.control)
                {
                    if (__instance.station.energyMax == 12000000000)
                    {
                        int upCount = __instance.player.TryAddItemToPackage(ProtoID.I深空货舰, idleShipCount, 0, false, 0, false);
                        UIItemup.Up(ProtoID.I深空货舰, upCount);
                    }
                    else
                    {
                        int upCount = __instance.player.TryAddItemToPackage(ProtoID.I太空运输船, idleShipCount, 0, false, 0, false);
                        UIItemup.Up(ProtoID.I太空运输船, upCount);
                    }
                }
                else
                {
                    if (__instance.station.energyMax == 12000000000)
                    {
                        __instance.player.SetHandItemId_Unsafe(ProtoID.I深空货舰);
                    }
                    else
                    {
                        __instance.player.SetHandItemId_Unsafe(ProtoID.I太空运输船);
                    }
                    __instance.player.SetHandItemCount_Unsafe(idleShipCount);
                    __instance.player.SetHandItemInc_Unsafe(0);
                }
                stationComponent.idleShipCount = 0;
            }
            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnPlayerIntendToTransferItems")]
        public static bool OnPlayerIntendToTransferItemsPatch(UIStationWindow __instance, int _itemId, int _itemCount, int _itemInc)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }

            if (_itemId == 5001)
            {
                if (__instance.droneIconButton.button.interactable)
                {
                    __instance.OnDroneIconClick(_itemId);
                }
            }
            else if (stationComponent.isStellar && (_itemId == ProtoID.I太空运输船 || _itemId == ProtoID.I深空货舰))
            {
                
                if (__instance.shipIconButton.button.interactable)
                {
                    //Debug.LogFormat("scppppppp1145141919 _itemId {0}", _itemId);
                    __instance.OnShipIconClick(_itemId);
                }
            }
            else if (stationComponent.isStellar && _itemId == 1210 && __instance.warperIconButton.button.interactable)
            {
                __instance.OnWarperIconClick(_itemId);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnShipIconClick")]
        public static bool OnShipIconClickPatch(UIStationWindow __instance, int obj)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId || !stationComponent.isStellar || stationComponent.isCollector || stationComponent.isVeinCollector)
            {
                return false;
            }

            if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount == 0)
            {
                __instance.player.SetHandItems(0, 0);
            }
            else if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount > 0)
            {
                int num = ProtoID.I太空运输船;
                if (stationComponent.energyMax == 12000000000)
                {
                    num = ProtoID.I深空货舰;
                }
                ItemProto itemProto = LDB.items.Select(num);
                if (__instance.player.inhandItemId != num)
                {
                    UIRealtimeTip.Popup("只能放入".Translate() + itemProto.name);
                    return false;
                }

                int num2 = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId)?.prefabDesc.stationMaxShipCount ?? 10;
                int num3 = stationComponent.idleShipCount + stationComponent.workShipCount;
                int num4 = num2 - num3;
                if (num4 < 0)
                {
                    num4 = 0;
                }

                int num5 = ((__instance.player.inhandItemCount < num4) ? __instance.player.inhandItemCount : num4);
                if (num5 <= 0)
                {
                    UIRealtimeTip.Popup("栏位已满".Translate());
                    return false;
                }

                int n = __instance.player.inhandItemCount;
                int m = __instance.player.inhandItemInc;
                int num6 = num5;
                int num7 = __instance.split_inc(ref n, ref m, num6);
                stationComponent.idleShipCount += num6;
                __instance.player.AddHandItemCount_Unsafe(-num6);
                __instance.player.SetHandItemInc_Unsafe(__instance.player.inhandItemInc - num7);
                if (__instance.player.inhandItemCount <= 0)
                {
                    __instance.player.SetHandItemId_Unsafe(0);
                    __instance.player.SetHandItemCount_Unsafe(0);
                    __instance.player.SetHandItemInc_Unsafe(0);
                }
            }
            else
            {
                if (__instance.player.inhandItemId != 0 || __instance.player.inhandItemCount != 0)
                {
                    return false;
                }

                int idleShipCount = stationComponent.idleShipCount;
                if (idleShipCount > 0)
                {
                    if (VFInput.shift || VFInput.control)
                    {
                        if (stationComponent.energyMax == 12000000000)
                        {
                            int upCount = __instance.player.TryAddItemToPackage(ProtoID.I深空货舰, idleShipCount, 0, throwTrash: false);
                            UIItemup.Up(ProtoID.I深空货舰, upCount);
                        }
                        else
                        {
                            int upCount = __instance.player.TryAddItemToPackage(ProtoID.I太空运输船, idleShipCount, 0, throwTrash: false);
                            UIItemup.Up(ProtoID.I太空运输船, upCount);
                        }
                    }
                    else
                    {
                        if (stationComponent.energyMax == 12000000000)
                        {
                            __instance.player.SetHandItemId_Unsafe(ProtoID.I深空货舰);
                        }
                        else
                        {
                            __instance.player.SetHandItemId_Unsafe(ProtoID.I太空运输船);
                        }
                        __instance.player.SetHandItemCount_Unsafe(idleShipCount);
                        __instance.player.SetHandItemInc_Unsafe(0);
                    }

                    stationComponent.idleShipCount = 0;
                }
            }
            return false;
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        public static void OnStationIdChangePatch(UIStationWindow __instance)
        {
            __instance.event_lock = true;
            if (__instance.active)
            {
                if (__instance.stationId == 0 || __instance.factory == null)
                {
                    return;
                }
                StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
                if (stationComponent.isStellar)
                {
                    if (stationComponent.energyMax == 12000000000)
                    {
                        
                        __instance.shipIconButton.tips.itemId = ProtoID.I深空货舰;
                        ItemProto itemProto2 = LDB.items.Select(ProtoID.I深空货舰);
                        __instance.shipIconImage.sprite = itemProto2.iconSprite;
                    }
                    else
                    {
                        __instance.shipIconButton.tips.itemId = ProtoID.I太空运输船;
                        ItemProto itemProto2 = LDB.items.Select(ProtoID.I太空运输船);
                        __instance.shipIconImage.sprite = itemProto2.iconSprite;
                        __instance.warperIconButton.gameObject.SetActive(false);
                        __instance.powerGroupRect.sizeDelta = new Vector2(380, 40f);
                    }
                }
            }
            __instance.event_lock = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        public static void _OnUpdatePatch(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            bool logisticShipWarpDrive = GameMain.history.logisticShipWarpDrive;
            if (stationComponent.isStellar && logisticShipWarpDrive)
            {
                if (stationComponent.energyMax == 12000000000)
                {
                    __instance.warperIconButton.gameObject.SetActive(value: true);
                }
                else
                {
                    __instance.warperIconButton.gameObject.SetActive(value: false);
                }
                //__instance.droneIconButton.gameObject.SetActive(value: false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Init")]
        public static void InitPatch(StationComponent __instance, bool _logisticShipWarpDrive)
        {
            __instance.warperMaxCount = (__instance.isStellar ? (_logisticShipWarpDrive ? ((__instance.energyMax == 12000000000) ? 50 : 0) : 0) : 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "OnStationIdChange")]
        public static void OnStationIdChangePatch(UIControlPanelStationInspector __instance)
        {
            __instance.event_lock = true;
            if (__instance.active)
            {
                if (__instance.stationId == 0 || __instance.factory == null)
                {
                    return;
                }
                StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
                if (stationComponent.isStellar)
                {
                    if (stationComponent.energyMax == 12000000000)
                    {
                        __instance.shipIconButton.tips.itemId = ProtoID.I深空货舰;
                        ItemProto itemProto2 = LDB.items.Select(ProtoID.I深空货舰);
                        __instance.shipIconImage.sprite = itemProto2.iconSprite;
                    }
                    else
                    {
                        __instance.shipIconButton.tips.itemId = ProtoID.I太空运输船;
                        ItemProto itemProto2 = LDB.items.Select(ProtoID.I太空运输船);
                        __instance.shipIconImage.sprite = itemProto2.iconSprite;
                        __instance.warperIconButton.gameObject.SetActive(false);
                        __instance.powerGroupRect.sizeDelta = new Vector2(380, 40f);
                    }
                }
            }
            __instance.event_lock = false;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "UpdateDelivery")]
        public static void UpdateDeliveryPatch(UIControlPanelStationInspector __instance)
        {
            bool logisticShipWarpDrive = GameMain.history.logisticShipWarpDrive;
            if (__instance.station.isStellar && logisticShipWarpDrive)
            {
                if (__instance.station.energyMax == 12000000000)
                {
                    __instance.warperIconButton.gameObject.SetActive(value: true);
                }
                else
                {
                    __instance.warperIconButton.gameObject.SetActive(value: false);
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationEntry), "_OnUpdate")]
        public static void _OnUpdatePatch(UIControlPanelStationEntry __instance)
        {
            if (__instance.factory.entityPool[__instance.station.entityId].protoId == 2104)
            {
                __instance.shipIconButton.tips.itemId = ProtoID.I太空运输船;
                ItemProto itemProto2 = LDB.items.Select(ProtoID.I太空运输船);
                __instance.shipIconImage.sprite = itemProto2.iconSprite;
                __instance.warperIconButton.gameObject.SetActive(false);
            }
            else if (__instance.factory.entityPool[__instance.station.entityId].protoId == 6267)
            {
                __instance.shipIconButton.tips.itemId = ProtoID.I深空货舰;
                ItemProto itemProto2 = LDB.items.Select(ProtoID.I深空货舰);
                __instance.shipIconImage.sprite = itemProto2.iconSprite;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
        public static bool PlanetFactory_StationAutoReplenishIfNeeded_Patch(PlanetFactory __instance, int entityId, Vector2 tipUiPos, bool isDrone, bool tipOnMouse = false)
        {
            if (__instance.gameData.guideRunning) {
                return false;
            }
            Player mainPlayer = __instance.gameData.mainPlayer;
            string text = "";
            bool flag = false;
            ref EntityData ptr = ref __instance.entityPool[entityId];
            Vector3 vector = ptr.pos;
            if (ptr.stationId > 0) {
                StationComponent stationComponent = __instance.transport.stationPool[ptr.stationId];
                if (stationComponent == null || stationComponent.isCollector || stationComponent.isVeinCollector) {
                    return false;
                }
                vector += vector.normalized * 13f;
                if (isDrone && stationComponent.droneAutoReplenish) {
                    int num = stationComponent.workDroneDatas.Length - (stationComponent.idleDroneCount + stationComponent.workDroneCount);
                    if (num > 0) {
                        int itemCount = mainPlayer.package.GetItemCount(5001);
                        if (itemCount > 0) {
                            int num2 = (itemCount < num) ? itemCount : num;
                            int num3 = 5001;
                            int num4 = num2;
                            int num5 = 0;
                            mainPlayer.package.TakeTailItems(ref num3, ref num4, out num5, false);
                            if (num3 > 0 && num4 > 0) {
                                stationComponent.idleDroneCount += num4;
                                text += string.Format("已自动补充提示".Translate(), num4, LDB.items.Select(num3).name);
                                if (num4 < num) {
                                    text += "自动补充物品不足0".Translate();
                                }
                                text += "\r\n";
                                flag = true;
                            }
                        } else {
                            text = text + string.Format("自动补充物品不足1".Translate(), LDB.items.Select(5001).name) + "\r\n";
                        }
                    }
                }
                if (!isDrone && stationComponent.isStellar && stationComponent.shipAutoReplenish) {
                    int num6 = stationComponent.workShipDatas.Length - (stationComponent.idleShipCount + stationComponent.workShipCount);
                    if (num6 > 0) {
                        int shipItemId = ProtoID.I太空运输船;
                        if (__instance.entityPool[entityId].protoId == ProtoID.I深空物流港) {
                            shipItemId = ProtoID.I深空货舰;
                        }
                        int itemCount2 = mainPlayer.package.GetItemCount(shipItemId);
                        if (itemCount2 > 0) {
                            int num7 = (itemCount2 < num6) ? itemCount2 : num6;
                            int num8 = shipItemId;
                            int num9 = num7;
                            int num10 = 0;
                            mainPlayer.package.TakeTailItems(ref num8, ref num9, out num10, false);
                            if (num8 > 0 && num9 > 0) {
                                stationComponent.idleShipCount += num9;
                                text += string.Format("已自动补充提示".Translate(), num9, LDB.items.Select(num8).name);
                                if (num9 < num6) {
                                    text += "自动补充物品不足0".Translate();
                                }
                                text += "\r\n";
                                flag = true;
                            }
                        } else {
                            text = text + string.Format("自动补充物品不足1".Translate(), LDB.items.Select(shipItemId).name) + "\r\n";
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(text)) {
                if (tipOnMouse) {
                    UIRealtimeTip.Popup(text, false, 0);
                } else if (tipUiPos.sqrMagnitude < 0.1f) {
                    UIRealtimeTip.Popup(text, vector, false, 0);
                } else {
                    UIRealtimeTip.Popup(text, tipUiPos, false);
                }
                if (flag) {
                    VFAudio.Create("equip-1", mainPlayer.transform, Vector3.zero, true, 4, -1, -1L);
                }
            }
            return false;
        }

        private static void ChangeShipItemId(ref int shipItemId, StationComponent station, PlanetFactory factory)
        {
            if (factory.entityPool[station.entityId].protoId == ProtoID.I太空物流港) {
                shipItemId = ProtoID.I太空运输船;
            } else if (factory.entityPool[station.entityId].protoId == ProtoID.I深空物流港) {
                shipItemId = ProtoID.I深空货舰;
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RunBehavior_Engage_AttackLaser_Ground_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_I4, 5002));
            object V_95 = matcher.Advance(1).Operand;
            object V_85 = matcher.Advance(1).Operand;

            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloca, V_95),
                new CodeInstruction(OpCodes.Ldloc_S, V_85),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CurvatureLogistics), nameof(ChangeShipItemId)))
                );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }
    }
}
