using HarmonyLib;
using Newtonsoft.Json;
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;
using System;
using System.Collections.Generic;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    public class StationStoreIsShares // 用class而非struct，避免值类型拷贝导致修改失效
    {
        // 5个bool属性，对应第1-5位
        public bool StoreIsShare1 { get; set; }
        public bool StoreIsShare2 { get; set; }
        public bool StoreIsShare3 { get; set; }
        public bool StoreIsShare4 { get; set; }
        public bool StoreIsShare5 { get; set; }
        // 可选：提供按索引访问的方法，方便通过"第x位"直接操作
        public bool this[int index] {
            get {
                // 传统switch语句（C# 7.3支持）
                switch (index) {
                    case 0: return StoreIsShare1;
                    case 1: return StoreIsShare2;
                    case 2: return StoreIsShare3;
                    case 3: return StoreIsShare4;
                    case 4: return StoreIsShare5;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index), "只能是1-5");
                }
            }
            set {
                switch (index) {
                    case 0: StoreIsShare1 = value; break;
                    case 1: StoreIsShare2 = value; break;
                    case 2: StoreIsShare3 = value; break;
                    case 3: StoreIsShare4 = value; break;
                    case 4: StoreIsShare5 = value; break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index), "只能是1-5");
                }
            }
        }
    }
    public class OrbitalRingStorage
    {
        public Dictionary<int, int[]> storageItem = new Dictionary<int, int[]>(); // itemId itemCount
        public Dictionary<int, StationStoreIsShares> storageShare = new Dictionary<int, StationStoreIsShares>(); // stationId storeShare
    }

    internal class OrbitalRingStorageCalculate
    {
        private static int OrbitalRingStorageMax = 200000;

        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        [HarmonyPostfix]
        public static void PlanetTransport_GameTick_Patch(ref PlanetTransport __instance, long time)
        {
            int num = (int)(time % 60);
            if (num != 0) {
                return;
            }
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            if (planetOrbitalRingData == null) return;
            //LogError($"planetOrbitalRingData.Rings.Count {planetOrbitalRingData.Rings.Count}");
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                    if (pair.stationType == StationType.Station) {
                        if (__instance.stationPool == null) return;
                        StationComponent stationComponent = __instance.stationPool[pair.OrbitalStationPoolId];
                        if (stationComponent != null) {
                            //LogError($"ringId {ringId} i {i}");
                            ShareStorageForOrbitalStation(ref stationComponent, ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage);
                        }
                    } else if (pair.elevatorPoolId != -1) {
                        if (__instance.stationPool == null) return;
                        StationComponent stationComponent = __instance.stationPool[pair.elevatorPoolId];
                        if (stationComponent != null) {
                            ShareStorageForOrbitalStation(ref stationComponent, ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage);
                        }
                    }
                }
            }
        }

        public static void ShareStorageForOrbitalStation(ref StationComponent stationComponent, ref OrbitalRingStorage orbitalRingStorage)
        {
            lock (stationComponent.storage) {
                for (int j = 0; j < stationComponent.storage.Length; j++) {
                    StationStore storage = stationComponent.storage[j];
                    if (orbitalRingStorage.storageShare.TryGetValue(stationComponent.id, out var storeShare)) {
                        if (!storeShare[j]) {
                            continue;
                        }

                        if (storage.count < storage.max / 2) {
                            lock (orbitalRingStorage.storageItem) {
                                if (orbitalRingStorage.storageItem.ContainsKey(storage.itemId)) {
                                    int count = (storage.max / 2) - storage.count;
                                    //int[] countAndInc = storageItem.GetValueOrDefault(storage.itemId);
                                    if (orbitalRingStorage.storageItem[storage.itemId][0] > count) {
                                        //storageItem[storage.itemId][0] -= count;
                                        int inc = split_inc(ref orbitalRingStorage.storageItem[storage.itemId][0], ref orbitalRingStorage.storageItem[storage.itemId][1], count);
                                        stationComponent.storage[j].count += count;
                                        stationComponent.storage[j].inc += (short)inc;
                                    } else {
                                        stationComponent.storage[j].count += orbitalRingStorage.storageItem[storage.itemId][0];
                                        orbitalRingStorage.storageItem[storage.itemId][0] = 0;
                                        stationComponent.storage[j].inc += (short)orbitalRingStorage.storageItem[storage.itemId][1];
                                        orbitalRingStorage.storageItem[storage.itemId][1] = 0;
                                        orbitalRingStorage.storageItem.Remove(storage.itemId);
                                    }
                                }
                            }
                        } else if (storage.count > storage.max / 2) {
                            lock (orbitalRingStorage.storageItem) {
                                if (!orbitalRingStorage.storageItem.ContainsKey(storage.itemId)) {
                                    orbitalRingStorage.storageItem[storage.itemId] = new int[] { 0, 0 };
                                }
                                int count = storage.count - (storage.max / 2);
                                if (storage.itemId == 1210) {
                                    OrbitalRingStorageMax = 200;
                                } else {
                                    OrbitalRingStorageMax = 200000;
                                }
                                if (orbitalRingStorage.storageItem[storage.itemId][0] < OrbitalRingStorageMax) {
                                    count = (OrbitalRingStorageMax - orbitalRingStorage.storageItem[storage.itemId][0]) > count ? count : (OrbitalRingStorageMax - orbitalRingStorage.storageItem[storage.itemId][0]);
                                    orbitalRingStorage.storageItem[storage.itemId][0] += count;
                                    //storage.count -= count;
                                    int inc = split_inc(ref stationComponent.storage[j].count, ref stationComponent.storage[j].inc, count);
                                    orbitalRingStorage.storageItem[storage.itemId][1] += inc;
                                    //LogError($"add count {stationComponent.storage[j].count} storageItem {storageItem[storage.itemId][0]}");
                                }
                            }
                        }
                    }
                }
                // 只有深空物流港warperMaxCount是50其他都是0，如果共享空间有翘曲器就填进去
                if (stationComponent.warperMaxCount != 0 && stationComponent.warperCount < stationComponent.warperMaxCount) {
                    lock (orbitalRingStorage.storageItem) {
                        if (orbitalRingStorage.storageItem.ContainsKey(1210)) {
                            int count = stationComponent.warperMaxCount - stationComponent.warperCount;
                            if (orbitalRingStorage.storageItem[1210][0] > count) {
                                int inc = split_inc(ref orbitalRingStorage.storageItem[1210][0], ref orbitalRingStorage.storageItem[1210][1], count);
                                stationComponent.warperCount += count;
                            } else {
                                stationComponent.warperCount += orbitalRingStorage.storageItem[1210][0];
                                orbitalRingStorage.storageItem[1210][0] = 0;
                                //stationComponent.storage[j].inc += (short)orbitalRingStorage.storageItem[storage.itemId][1];
                                orbitalRingStorage.storageItem[1210][1] = 0;
                                orbitalRingStorage.storageItem.Remove(1210);
                            }
                        }
                    }
                }
            }
        }


        //[HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick))]
        //[HarmonyPostfix]
        //public static void FactorySystem_GameTick_Patch(ref FactorySystem __instance, long time)
        //{
        //    int num = (int)(time % 60);
        //    if (num != 0) {
        //        return;
        //    }
        //    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
        //    if (planetOrbitalRingData == null) return;
        //    for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
        //        for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
        //            var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
        //            if (pair.stationType == StationType.Assembler) {
        //                if (__instance.assemblerPool == null) return;
        //                AssemblerComponent assemblerComponent = __instance.assemblerPool[pair.OrbitalStationPoolId];
        //                if (assemblerComponent.recipeId != 0) {
        //                    LogError($"ShareStorageForOrbitalAssembler");
        //                    ShareStorageForOrbitalAssembler(ref assemblerComponent, ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem);
        //                }
        //            }
        //        }
        //    }
        //}

        public static void ShareStorageForOrbitalAssembler(ref AssemblerComponent assemblerComponent, FactorySystem factory)
        {
            int itemId = 0;
            int count = 0;
            int inc = 0;
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planet.id);
            if (planetOrbitalRingData == null) return;
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int j = 0; j < planetOrbitalRingData.Rings[ringId].Capacity; j++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(j);
                    if (pair.stationType == StationType.Assembler) {
                        if (pair.OrbitalStationPoolId == assemblerComponent.id) {
                            ref Dictionary<int, int[]> storageItem = ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem;
                            int num = assemblerComponent.speedOverride * 180 / assemblerComponent.recipeExecuteData.timeSpend + 1;
                            if (num < 2) {
                                num = 2;
                            }
                            for (int i = 0; i < assemblerComponent.needs.Length; i++) {
                                itemId = assemblerComponent.needs[i];
                                lock (storageItem) {
                                    if (storageItem.ContainsKey(itemId)) {
                                        count = assemblerComponent.recipeExecuteData.requireCounts[i] * num;
                                        if (storageItem[itemId][0] >= count) {
                                            assemblerComponent.served[i] += count;
                                            inc = split_inc(ref storageItem[itemId][0], ref storageItem[itemId][1], count);
                                            assemblerComponent.incServed[i] += inc;
                                        } else {
                                            assemblerComponent.served[i] += storageItem[itemId][0];
                                            inc = storageItem[itemId][1];
                                            assemblerComponent.incServed[i] += inc;
                                            storageItem[itemId][0] = 0;
                                            storageItem[itemId][1] = 0;
                                            storageItem.Remove(itemId);
                                        }
                                        // 当星环对撞机执行起义物质配方时，记录输入的电池增产的值
                                        if (assemblerComponent.recipeId == 104 && itemId == 2207) {
                                            ValueTuple<int, int> key = new ValueTuple<int, int>(factory.planet.id, assemblerComponent.id);
                                            MoonPatch.ColliderAccumulatorIncData.AddOrUpdate(key, inc, (k, v) => v + inc);
                                        }
                                    }
                                }
                            }
                            for (int i = 0; i < assemblerComponent.recipeExecuteData.products.Length; i++) {
                                if (assemblerComponent.produced[i] == 0) {
                                    continue;
                                }
                                if (pair.elevatorPoolId != -1) {
                                    StationStore[] storage = factory.factory.transport.stationPool[pair.elevatorPoolId].storage;
                                    bool flag = false;
                                    for (int z = 0; z < storage.Length; z++) {
                                        if (storage[z].itemId == assemblerComponent.recipeExecuteData.products[i]) {
                                            if (!planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare[pair.elevatorPoolId][z]) {
                                                flag = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (flag) {
                                        continue; // 太空电梯对该物品设置不接入共享空间时，则轨道设施产物不输入到共享空间
                                    }
                                }
                                itemId = assemblerComponent.recipeExecuteData.products[i];
                                lock (storageItem) {
                                    if (!storageItem.ContainsKey(itemId)) {
                                        storageItem[itemId] = new int[] { 0, 0 };
                                    }
                                    if (storageItem[itemId][0] < OrbitalRingStorageMax) {
                                        count = assemblerComponent.produced[i];
                                        storageItem[itemId][0] += count;
                                        assemblerComponent.produced[i] = 0;
                                        if (assemblerComponent.recipeType == ERecipeType.Smelt) {
                                            storageItem[itemId][1] += 4 * count;
                                        }
                                        // 当星环对撞机执行起义物质配方时，按记录输入的电池增产的值赋给输出的空电池增产
                                        if (assemblerComponent.recipeId == 104 && itemId == 2206) {
                                            ValueTuple<int, int> key = new ValueTuple<int, int>(factory.planet.id, assemblerComponent.id);
                                            bool flag = MoonPatch.ColliderAccumulatorIncData.ContainsKey(key) && MoonPatch.ColliderAccumulatorIncData[key] >= 4 * count;
                                            if (flag) {
                                                MoonPatch.ColliderAccumulatorIncData[key] -= 4 * count;
                                                storageItem[itemId][1] += 4 * count;
                                                if (MoonPatch.ColliderAccumulatorIncData[key] < 0) {
                                                    MoonPatch.ColliderAccumulatorIncData[key] = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        [HarmonyPostfix]
        public static void EjectorComponent_InternalUpdate_Patch(ref EjectorComponent __instance, long tick)
        {
            int num = (int)(tick % 60);
            if (num != 0) {
                return;
            }
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planetId);
            if (planetOrbitalRingData == null) return;
            //LogError($"planetOrbitalRingData.Rings.Count {planetOrbitalRingData.Rings.Count}");
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                    if (pair.stationType == StationType.EjectorCore) {
                        if (pair.OrbitalCorePoolId == __instance.id) {
                            if (__instance.bulletCount == 0) {
                                ref Dictionary<int, int[]> storageItem = ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem;
                                lock (storageItem) {
                                    if (storageItem.ContainsKey(__instance.bulletId)) {
                                        int count = (storageItem[__instance.bulletId][0] >= 40) ? 40 : storageItem[__instance.bulletId][0];
                                        __instance.bulletCount += count;
                                        int inc = split_inc(ref storageItem[__instance.bulletId][0], ref storageItem[__instance.bulletId][1], count);
                                        __instance.bulletInc += inc;
                                        if (storageItem[__instance.bulletId][0] == 0) {
                                            storageItem.Remove(__instance.bulletId);
                                        }
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.InternalUpdate))]
        [HarmonyPostfix]
        public static void TurretComponent_InternalUpdate_Patch(ref TurretComponent __instance, long time, PlanetFactory factory)
        {
            int num = (int)(time % 60);
            if (num != 0) {
                return;
            }
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
            if (planetOrbitalRingData == null) return;
            //LogError($"planetOrbitalRingData.Rings.Count {planetOrbitalRingData.Rings.Count}");
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                    if (pair.stationType == StationType.TurretCore) {
                        if (pair.OrbitalCorePoolId == __instance.id) {
                            if (__instance.itemCount <= 5) {
                                ref Dictionary<int, int[]> storageItem = ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem;
                                if (__instance.itemId != 0) {
                                    lock (storageItem) {
                                        if (storageItem.ContainsKey(__instance.itemId)) {
                                            int count = (storageItem[__instance.itemId][0] >= 5) ? 5 : storageItem[__instance.itemId][0];
                                            __instance.itemCount += (short)count;
                                            int inc = split_inc(ref storageItem[__instance.itemId][0], ref storageItem[__instance.itemId][1], count);
                                            __instance.itemInc += (short)inc;
                                            if (storageItem[__instance.itemId][0] == 0) {
                                                storageItem.Remove(__instance.itemId);
                                            }
                                        }
                                    }
                                } else {
                                    int[] array = ItemProto.turretNeeds[(int)__instance.ammoType];
                                    if (array == null || array.Length == 0) {
                                        return;
                                    }
                                    for (int z = 0; z < array.Length; z++) {
                                        lock (storageItem) {
                                            if (storageItem.ContainsKey(array[z]) && storageItem[array[z]][0] >= 1) {
                                                int inc = split_inc(ref storageItem[array[z]][0], ref storageItem[array[z]][1], 1);
                                                __instance.SetNewItem(array[z], 1, (short)inc);
                                                if (storageItem[array[z]][0] == 0) {
                                                    storageItem.Remove(array[z]);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        [HarmonyPostfix]
        public static void PowerSystem_GameTick_Patch(ref PowerSystem __instance, long time)
        {
            int num = (int)(time % 60);
            if (num != 0) {
                return;
            }
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            if (planetOrbitalRingData == null) return;
            //LogError($"planetOrbitalRingData.Rings.Count {planetOrbitalRingData.Rings.Count}");
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                    if (pair.stationType == StationType.PowerGenCore) {
                        if (pair.OrbitalCorePoolId < __instance.genPool.Length) {
                            int j = pair.OrbitalCorePoolId;
                            if (__instance.genPool[j].fuelCount <= 10) {
                                ref Dictionary<int, int[]> storageItem = ref planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageItem;
                                if (__instance.genPool[j].fuelId != 0) {
                                    int fuelId = __instance.genPool[j].fuelId;
                                    lock (storageItem) {
                                        if (storageItem.ContainsKey(fuelId)) {
                                            int count = (storageItem[fuelId][0] >= 10) ? 10 : storageItem[fuelId][0];
                                            __instance.genPool[j].fuelCount += (short)count;
                                            int inc = split_inc(ref storageItem[fuelId][0], ref storageItem[fuelId][1], count);
                                            __instance.genPool[j].fuelInc += (short)inc;
                                            if (storageItem[fuelId][0] == 0) {
                                                storageItem.Remove(fuelId);
                                            }
                                        }
                                    }
                                } else {
                                    int[] array = ItemProto.fuelNeeds[(int)__instance.genPool[j].fuelMask];
                                    if (array == null || array.Length == 0) {
                                        return;
                                    }
                                    for (int z = 0; z < array.Length; z++) {
                                        lock (storageItem) {
                                            if (storageItem.ContainsKey(array[z]) && storageItem[array[z]][0] >= 1) {
                                                int inc = split_inc(ref storageItem[array[z]][0], ref storageItem[array[z]][1], 1);
                                                __instance.genPool[j].SetNewFuel(array[z], 1, (short)inc);
                                                if (storageItem[array[z]][0] == 0) {
                                                    storageItem.Remove(array[z]);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
