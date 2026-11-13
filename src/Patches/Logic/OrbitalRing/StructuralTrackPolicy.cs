using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    [HarmonyPatch]
    internal static class StructuralTrackPolicy
    {
        internal static bool Enabled { get; private set; }

        internal static void Configure(ConfigFile config)
        {
            Enabled = config.Bind("Orbital tracks", "StructuralTracks", true,
                "Build orbital tracks as structural links with automatic direction and disabled cargo transport. " +
                "Restart the game to apply changes.\n轨道按结构连接建造，自动整理方向并关闭货物运输。重启游戏后生效。").Value;
        }

        internal static bool IsTrackItem(int itemId) => TryGetTrackPlacement(itemId, out _, out _);

        internal static bool IsTrackObject(PlanetFactory factory, int objectId)
        {
            return TryReadObject(factory, objectId, out int itemId, out _) && IsTrackItem(itemId);
        }

        internal static bool IsTrackBelt(CargoTraffic traffic, int beltId)
        {
            return traffic?.beltPool != null && beltId > 0 && beltId < traffic.beltCursor &&
                traffic.beltPool[beltId].id == beltId &&
                IsTrackObject(traffic.factory, traffic.beltPool[beltId].entityId);
        }

        internal static bool CanConnect(PlanetFactory factory, int firstObject, int secondObject,
            out Vector3 firstPosition, out Vector3 secondPosition, out bool structural)
        {
            TryReadObject(factory, firstObject, out int firstItem, out firstPosition);
            TryReadObject(factory, secondObject, out int secondItem, out secondPosition);
            structural = firstObject != 0 && secondObject != 0 && (IsTrackItem(firstItem) || IsTrackItem(secondItem));
            return !structural || SameTrackBand(factory, firstItem, firstPosition, secondItem, secondPosition);
        }

        internal static bool CanConnectPreview(PlanetFactory factory, int firstItem, Vector3 firstPosition,
            int secondItem, Vector3 secondPosition)
        {
            return !Enabled || (!IsTrackItem(firstItem) && !IsTrackItem(secondItem)) ||
                SameTrackBand(factory, firstItem, firstPosition, secondItem, secondPosition);
        }

        private static bool SameTrackBand(PlanetFactory factory, int firstItem, Vector3 firstPosition,
            int secondItem, Vector3 secondPosition)
        {
            if (firstItem != secondItem || factory?.planet == null)
            {
                return false;
            }

            var layout = GetLayout(factory.planet);
            var firstBand = CalculateRingPosMark(firstPosition, layout);
            var secondBand = CalculateRingPosMark(secondPosition, layout);
            return firstBand.ringIndex >= 0 && firstBand.ringBeltIndex >= 0 &&
                firstBand.ringIndex == secondBand.ringIndex && firstBand.ringBeltIndex == secondBand.ringBeltIndex;
        }

        internal static bool TryReadObject(PlanetFactory factory, int objectId, out int itemId, out Vector3 position)
        {
            itemId = 0;
            position = Vector3.zero;
            if (factory == null || objectId == 0 || objectId == int.MinValue)
            {
                return false;
            }

            if (objectId > 0)
            {
                if (factory.entityPool == null || objectId >= factory.entityCursor ||
                    factory.entityPool[objectId].id != objectId)
                {
                    return false;
                }
                itemId = factory.entityPool[objectId].protoId;
                position = factory.entityPool[objectId].pos;
            }
            else
            {
                int prebuildId = -objectId;
                if (factory.prebuildPool == null || prebuildId >= factory.prebuildCursor ||
                    factory.prebuildPool[prebuildId].id != prebuildId)
                {
                    return false;
                }
                itemId = factory.prebuildPool[prebuildId].protoId;
                position = factory.prebuildPool[prebuildId].pos;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.AlterBeltConnections))]
        private static void FilterBeltConnections(CargoTraffic __instance, int beltId, ref int outputId,
            ref int input0Id, ref int input1Id, ref int input2Id)
        {
            if (!Enabled || beltId <= 0 || beltId >= __instance.beltCursor ||
                __instance.beltPool[beltId].id != beltId)
            {
                return;
            }

            int entityId = __instance.beltPool[beltId].entityId;
            FilterNeighbor(__instance, entityId, ref outputId);
            FilterNeighbor(__instance, entityId, ref input0Id);
            FilterNeighbor(__instance, entityId, ref input1Id);
            FilterNeighbor(__instance, entityId, ref input2Id);
        }

        private static void FilterNeighbor(CargoTraffic traffic, int entityId, ref int neighborBelt)
        {
            if (neighborBelt <= 0 || neighborBelt >= traffic.beltCursor ||
                traffic.beltPool[neighborBelt].id != neighborBelt)
            {
                return;
            }
            if (!CanConnect(traffic.factory, entityId, traffic.beltPool[neighborBelt].entityId, out _, out _, out _))
            {
                neighborBelt = 0;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIBeltWindow), "_OnOpen")]
        private static void CloseCargoWindow(UIBeltWindow __instance)
        {
            if (Enabled && IsTrackBelt(__instance.traffic, __instance.beltId))
            {
                __instance._beltId = 0;
                __instance._Close();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), nameof(UIGame.OnPlayerInspecteeChange))]
        private static void InspectStructure(UIGame __instance)
        {
            var traffic = GameMain.localPlanet?.factory?.cargoTraffic;
            if (Enabled && IsTrackBelt(traffic, __instance.inspectBeltId))
            {
                __instance.inspectBeltId = 0;
                __instance.beltWindow._beltId = 0;
                __instance.beltWindow._Close();
            }
        }
    }
}
