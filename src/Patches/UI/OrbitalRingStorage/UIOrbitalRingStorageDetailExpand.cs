using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow
{
    public static class UIOrbitalRingStorageDetailExpand
    {
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnInit))]
        [HarmonyPostfix]
        public static void Init()
        {
            ProjectOrbitalRing.OrbitalRingStorageWindow = UIOrbitalRingStorageWindow.CreateWindow();
        }

        [HarmonyPatch(typeof(UIGame), nameof(UIGame.ShutAllFunctionWindow))]
        [HarmonyPrefix]
        public static void CloseStorageWindow()
        {
            ProjectOrbitalRing.OrbitalRingStorageWindow?._Close();
        }

        [HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        [HarmonyPrefix]
        public static void BeforeInspecteeChange() => CloseStorageWindow();

        [HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        [HarmonyPostfix]
        public static void OnPlayerInspecteeChange_Patch(UIGame __instance, EObjectType objType, int objId)
        {
            PlanetFactory factory = GameMain.mainPlayer?.factory;
            if (objType != EObjectType.Entity || FindStorageRing(factory, objId, out int ringIndex) == null) return;
            OpenStorageWindow(__instance, factory.planetId, ringIndex);
        }

        internal static void OpenStorageWindow(UIGame ui, int planetId, int ringIndex)
        {
            ui.ShutAllFunctionWindow();
            ProjectOrbitalRing.OrbitalRingStorageWindow.OpenWindow(planetId, ringIndex);
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
        [HarmonyPrefix]
        private static bool FastDepositIntoSharedStorage(PlanetFactory __instance, int entityId, bool fromPackage,
            ref ItemBundle itemBundle, ref ItemBundle ____tmp_items)
        {
            Player player = GameMain.mainPlayer;
            if (fromPackage || player?.factory != __instance || player.inhandItemId <= 0 || player.inhandItemCount <= 0 ||
                !VFInput._fastTransferWithEntityPress) return true;

            EquatorRing ring = FindStorageRing(__instance, entityId, out int ringIndex);
            if (ring == null) return true;

            itemBundle = ____tmp_items ?? (____tmp_items = new ItemBundle());
            itemBundle.Clear();
            if (NebulaAPI.NebulaModAPI.IsMultiplayerActive) {
                MultiplayerTransfers.Deposit(__instance.planetId, ringIndex, player);
                return false;
            }

            int itemId = player.inhandItemId;
            int deposited = OrbitalRingStorageCalculate.DepositHandItems(ring.orbitalRingStorage, player);
            if (deposited > 0) {
                itemBundle.Alter(itemId, deposited);
            } else if (VFInput._fastTransferWithEntityDown) {
                UIRealtimeTip.Popup("OrbitalRingStorageWindowDepositBlocked".TranslateFromJson());
            }
            return false;
        }

        private static EquatorRing FindStorageRing(PlanetFactory factory, int entityId, out int ringIndex)
        {
            ringIndex = -1;
            if (factory == null || entityId <= 0 || entityId >= factory.entityPool.Length ||
                factory.entityPool[entityId].id != entityId || factory.entityPool[entityId].protoId != ProtoID.I天枢座) return null;

            var planet = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
            if (planet == null) return null;
            for (int index = 0; index < planet.Rings.Count; index++) {
                var ring = planet.Rings[index];
                if (!ring.IsOneFull()) continue;
                for (int slot = 0; slot < ring.Capacity; slot++) {
                    if (ring.GetPair(slot).OrbitalStationPoolId != entityId) continue;
                    ringIndex = index;
                    return ring;
                }
            }
            return null;
        }

        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        [HarmonyPrefix]
        public static void BeforeGameUpdate()
        {
            ProjectOrbitalRing.OrbitalRingStorageWindow?.HandleSplitEscape();
        }

        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        [HarmonyPostfix]
        public static void UIGame_OnUpdate_Postfix()
        {
            ProjectOrbitalRing.OrbitalRingStorageWindow?._Update();
        }
    }
}
