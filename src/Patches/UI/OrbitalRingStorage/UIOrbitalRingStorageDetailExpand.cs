using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Patches.UI.PlanetFocus;
using ProjectOrbitalRing.Patches.UI.Utils;
using ProjectOrbitalRing.Utils;
using System;
using System.Security.Cryptography;
using Unity.Jobs.LowLevel.Unsafe;
using static ProjectOrbitalRing.ProjectOrbitalRing;

// ReSharper disable InconsistentNaming

namespace ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow
{
    public static class UIOrbitalRingStorageDetailExpand
    {
        private static UIButton _planetFocusBtn;
        private static int inspectOrbitalRingStorage;

        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnInit))]
        [HarmonyPostfix]
        public static void Init(UIGame __instance)
        {
            ProjectOrbitalRing.OrbitalRingStorageWindow = UIOrbitalRingStorageWindow.CreateWindow();
        }

        [HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        [HarmonyPostfix]
        public static void OnPlayerInspecteeChange_Patch(UIGame __instance, EObjectType objType, int objId)
        {
            var factory = GameMain.mainPlayer.factory;
            if (factory == null) return;
            if (objId >= factory.entityPool.Length) return;
            if (factory.entityPool[objId].protoId == ProtoID.I天枢座) {
                __instance.ShutAllFunctionWindow();
                __instance.ShutPlayerInventory();

                //ProjectOrbitalRing.OrbitalRingStorageWindow.OpenWindow();

                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
                if (planetOrbitalRingData == null) return;
                for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                    if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                    for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                        var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                        if (pair.OrbitalStationPoolId == objId) {
                            ProjectOrbitalRing.OrbitalRingStorageWindow.OpenWindow();
                            ProjectOrbitalRing.OrbitalRingStorageWindow.SetStorageData(planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem);
                            if (UIOrbitalRingStorageWindow.CurPlanetId != factory.planetId || UIOrbitalRingStorageWindow.CurStorageIndex != ringId) {
                                UIOrbitalRingStorageWindow.CurPlanetId = factory.planetId;
                                UIOrbitalRingStorageWindow.CurStorageIndex = ringId;
                                //ProjectOrbitalRing.CurPlanetIdAndStorageIndex.OnPlanetChanged(UIOrbitalRingStorageWindow.CurPlanetIdAndStorageIndex);
                            }
                            return;
                        }
                    }
                }
            } else {
                ProjectOrbitalRing.OrbitalRingStorageWindow._Close();
            }
        }
    }
}
