using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    [HarmonyPatch]
    internal static class StructuralTrackCargo
    {
        private sealed class PathState
        {
            internal readonly bool ContainsTrack;

            internal PathState(CargoPath path, CargoTraffic traffic)
            {
                var belts = path.belts;
                ContainsTrack = belts == null || belts.Count == 0;
                if (belts != null)
                {
                    foreach (int beltId in belts)
                    {
                        if (StructuralTrackPolicy.IsTrackBelt(traffic, beltId))
                        {
                            ContainsTrack = true;
                            break;
                        }
                    }
                }
            }
        }

        private static readonly ConditionalWeakTable<CargoContainer, CargoTraffic> owners =
            new ConditionalWeakTable<CargoContainer, CargoTraffic>();
        // Native phase barriers separate path membership changes from transport workers.
        private static readonly ConditionalWeakTable<CargoPath, PathState> paths =
            new ConditionalWeakTable<CargoPath, PathState>();

        private static void Bind(CargoTraffic traffic)
        {
            if (traffic.container != null)
            {
                owners.GetValue(traffic.container, _ => traffic);
            }
        }

        private static void Invalidate(CargoPath path)
        {
            if (path != null)
            {
                paths.Remove(path);
            }
        }

        private static bool BlocksCargo(CargoPath path)
        {
            if (!StructuralTrackPolicy.Enabled || path?.cargoContainer == null || path.id <= 0)
            {
                return false;
            }
            if (!paths.TryGetValue(path, out PathState state))
            {
                if (!owners.TryGetValue(path.cargoContainer, out CargoTraffic owner))
                {
                    return false;
                }
                state = paths.GetValue(path, current => new PathState(current, owner));
            }
            return state.ContainsTrack;
        }

        internal static bool CanNormalize(CargoTraffic traffic, HashSet<int> paths)
        {
            Bind(traffic);
            if (traffic.container == null || traffic.container.tmpEnabled || traffic.container.tmpCargoCount > 0)
            {
                return false;
            }
            foreach (int pathId in paths)
            {
                if (pathId <= 0 || pathId >= traffic.pathPool.Length)
                {
                    continue;
                }
                CargoPath path = traffic.GetCargoPath(pathId);
                if (path == null)
                {
                    continue;
                }
                lock (path.buffer)
                {
                    for (int index = 4; index + 5 < path.bufferLength; index++)
                    {
                        if (path.buffer[index] == CargoPath.kCargoSign)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // Import allocates new CargoPath instances, so their classifications are initially uncached.
        [HarmonyPrefix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.Import))]
        private static void Importing(CargoTraffic __instance) => Bind(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.NewCargoPath))]
        private static void CreatedPath(CargoTraffic __instance, CargoPath __result)
        {
            Bind(__instance);
            Invalidate(__result);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpgradeBeltComponent))]
        private static void UpgradedBelt(CargoTraffic __instance, int beltId)
        {
            if (beltId > 0 && beltId < __instance.beltPool.Length)
            {
                int pathId = __instance.beltPool[beltId].segPathId;
                if (pathId > 0 && pathId < __instance.pathPool.Length)
                {
                    Invalidate(__instance.pathPool[pathId]);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.AddRelatedBelt))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.RemoveRelatedBelt))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TruncBuffer))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Clear))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Import))]
        private static void ChangedPath(CargoPath __instance) => Invalidate(__instance);

        [HarmonyPatch]
        private static class CargoAvailability
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> Targets()
            {
                var names = new HashSet<string> { "TryInsertCargo", "TryInsertCargoNoSqueeze", "TryInsertItem",
                    "TryInsertItemAtHead", "TryInsertItemAtHeadAndFillBlank", "TryUpdateItemAtHeadAndFillBlank",
                    "InsertItemDirectWithFastCheck", "HasCargoAtRear", "TestCargoAtRear", "CanPickItemFromRear" };
                foreach (MethodInfo method in typeof(CargoPath).GetMethods())
                {
                    if (names.Contains(method.Name))
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyPrefix]
            private static bool Before(CargoPath __instance, ref bool __result)
            {
                if (!BlocksCargo(__instance))
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryInsertItemWithStackIncreasement))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Update))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.PresentCargos))]
        private static bool AllowCargo(CargoPath __instance) => !BlocksCargo(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TestBlankAtHead))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.GetCargoIdAtRear))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.GetCargoIdAtIndex))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickCargoAtEnd))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickCargo))]
        private static bool CargoIdOrSpace(CargoPath __instance, ref int __result)
        {
            if (!BlocksCargo(__instance))
            {
                return true;
            }
            __result = -1;
            return false;
        }

        [HarmonyPatch]
        private static class ItemQuery
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> Targets()
            {
                foreach (MethodInfo method in typeof(CargoPath).GetMethods())
                {
                    if (method.Name == "TryPickItem" || method.Name == "TryPickFuel" ||
                        method.Name == "QueryItemAtIndex")
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyPrefix]
            private static bool Before(CargoPath __instance, ref byte stack, ref byte inc, ref int __result)
            {
                if (!BlocksCargo(__instance))
                {
                    return true;
                }
                stack = 0;
                inc = 0;
                __result = 0;
                return false;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickItemAtRear))]
        private static bool RearItem(CargoPath __instance, ref int needIdx, ref byte stack, ref byte inc,
            ref int __result)
        {
            if (!BlocksCargo(__instance))
            {
                return true;
            }
            needIdx = -1;
            stack = 0;
            inc = 0;
            __result = 0;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CargoPath), nameof(CargoPath.GetCargoAtIndex))]
        private static bool CargoAtIndex(CargoPath __instance, ref Cargo cargo, ref int cargoId, ref int offset,
            ref bool __result)
        {
            if (!BlocksCargo(__instance))
            {
                return true;
            }
            cargo = default;
            cargoId = -1;
            offset = -1;
            __result = false;
            return false;
        }
    }
}
