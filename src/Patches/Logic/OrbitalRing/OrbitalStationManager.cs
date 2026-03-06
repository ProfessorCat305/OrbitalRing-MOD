using GalacticScale;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WinAPI;
using static ProjectOrbitalRing.Patches.Logic.GlobalPowerSupplyPatches;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalStationManager
    {
        // 静态只读实例，通过属性暴露
        private static readonly OrbitalStationManager _instance = new OrbitalStationManager();

        // 私有构造函数，禁止外部实例化
        private OrbitalStationManager() { }

        // 全局访问点,太空电梯轨道设施配对管理器
        public static OrbitalStationManager Instance => _instance;

        private static readonly Dictionary<int, PlanetOrbitalRingData> AllplanetsOrbitalRings = new Dictionary<int, PlanetOrbitalRingData>();

        public static bool StationTypeIsBase(StationType type)
        {
            switch (type)
            {
                case StationType.PowerGenBase:
                case StationType.TurretBase:
                    return true;
                default:
                    return false;
            }
        }

        public void AddPlanetId(int planetId, bool isMoon)
        {
            if (AllplanetsOrbitalRings.ContainsKey(planetId)) return;
            if (isMoon) {
                AllplanetsOrbitalRings[planetId] = new PlanetOrbitalRingData(planetId, 20);
            } else {
                AllplanetsOrbitalRings[planetId] = new PlanetOrbitalRingData(planetId, 40);
            }
        }

        public PlanetOrbitalRingData GetPlanetOrbitalRingData(int planetId) => AllplanetsOrbitalRings.GetValueOrDefault(planetId);


        internal static void Export(BinaryWriter w)
        {
            w.Write(AllplanetsOrbitalRings.Count);
            foreach (var outerPair in AllplanetsOrbitalRings) {
                // 写入planetId
                w.Write(outerPair.Key);
                // 写入PlanetOrbitalRingData里Rings个数
                w.Write(outerPair.Value.Rings.Count);
                // 写入planetIncLevel
                w.Write(outerPair.Value.planetIncLevel);

                for (int i = 0; i < outerPair.Value.Rings.Count; i++) {
                    w.Write(outerPair.Value.Rings[i].Capacity);
                    w.Write(outerPair.Value.Rings[i].isParticleCollider);
                    //w.Write(outerPair.Value.Rings[i].SpaceStationCount);
                    for (int j = 0; j < outerPair.Value.Rings[i].Capacity; j++) {
                        var pair = outerPair.Value.Rings[i].GetPair(j);
                        w.Write(pair.elevatorPoolId);
                        w.Write(pair.OrbitalStationPoolId);
                        w.Write((int)pair.stationType);
                        w.Write(pair.OrbitalCorePoolId);
                        w.Write(outerPair.Value.Rings[i].incCoreLevel[j]);
                    }
                    w.Write(outerPair.Value.Rings[i].orbitalRingStorage.storageItem.Count);
                    foreach (var storageItem in outerPair.Value.Rings[i].orbitalRingStorage.storageItem) {
                        int itemId = storageItem.Key;
                        int[] countInc = storageItem.Value;
                        w.Write(itemId);
                        w.Write(countInc[0]);
                        w.Write(countInc[1]);
                    }
                    w.Write(outerPair.Value.Rings[i].orbitalRingStorage.storageShare.Count);
                    foreach (var storageShare in outerPair.Value.Rings[i].orbitalRingStorage.storageShare) {
                        int stationId = storageShare.Key;
                        StationStoreIsShares storeShare = storageShare.Value;
                        w.Write(stationId);
                        w.Write(storeShare[0]);
                        w.Write(storeShare[1]);
                        w.Write(storeShare[2]);
                        w.Write(storeShare[3]);
                        w.Write(storeShare[4]);
                    }

                    for (int j = 0; j < 1000; j++) {
                        w.Write(outerPair.Value.Rings[i].insideRingPositions[j]);
                        w.Write(outerPair.Value.Rings[i].outsideRingPositions[j]);
                    }
                    w.Write(outerPair.Value.Rings[i].isInsideRingComplete);
                    w.Write(outerPair.Value.Rings[i].isOutsideRingComplete);
                    for (int j = 0; j < 1000; j++) {
                        w.Write(outerPair.Value.Rings[i].lowInsideRingPositions[j]);
                        w.Write(outerPair.Value.Rings[i].lowOutsideRingPositions[j]);
                    }
                    w.Write(outerPair.Value.Rings[i].isLowInsideRingComplete);
                    w.Write(outerPair.Value.Rings[i].isLowOutsideRingComplete);
                }
            }
        }
        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try {
                // 读取外层字典条目数
                int AllplanetsOrbitalRingsCount = r.ReadInt32();
                LogError($"scppppppppppppppppppppppppppppppppppppppppppp importVersion {ProjectOrbitalRing.importVersion}");
                for (int i = 0; i < AllplanetsOrbitalRingsCount; i++) {
                    int planetId = r.ReadInt32();
                    PlanetOrbitalRingData data = new PlanetOrbitalRingData(planetId);
                    // 读取内层字典条目数
                    int OnePlanetRingsCount = r.ReadInt32();
                    if (ProjectOrbitalRing.importVersion < 524314) {
                        data.planetIncLevel = 0;
                    } else {
                        data.planetIncLevel = r.ReadInt32();
                    }
                    for (var j = 0; j < OnePlanetRingsCount; j++) {
                        int Capacity = r.ReadInt32();
                        EquatorRing OnePlanetOneRing = new EquatorRing(Capacity, planetId);
                        //OnePlanetOneRingnew.spaceStationCount = r.ReadInt32();
                        OnePlanetOneRing.isParticleCollider = r.ReadBoolean();

                        for (int k = 0; k < OnePlanetOneRing.Capacity; k++) {
                            int elevatorPoolId = r.ReadInt32();
                            int OrbitalStationPoolId = r.ReadInt32();
                            EquatorRing.StationType stationType = (EquatorRing.StationType)r.ReadInt32();
                            int OrbitalCorePoolId = r.ReadInt32();
                            OnePlanetOneRing.AddElevator(k, elevatorPoolId);
                            OnePlanetOneRing.AddOrbitalStation(k, OrbitalStationPoolId, stationType);
                            OnePlanetOneRing.AddOrbitalCore(k, OrbitalCorePoolId, stationType);
                            if (ProjectOrbitalRing.importVersion < 524314) {
                                OnePlanetOneRing.incCoreLevel[k] = 0;
                            } else {
                                OnePlanetOneRing.incCoreLevel[k] = r.ReadInt32();
                            }
                            
                        }
                        int storageItemCount = r.ReadInt32();
                        for (int y = 0; y < storageItemCount; y++) {
                            OnePlanetOneRing.orbitalRingStorage.storageItem.TryAdd(r.ReadInt32(), new int[] { r.ReadInt32(), r.ReadInt32() });
                        }
                        if (ProjectOrbitalRing.importVersion < 589858) {
                            for (int y = 0; y < OnePlanetOneRing.Capacity; y++) {
                                var pair = OnePlanetOneRing.GetPair(y);
                                if (pair.stationType == StationType.Station) {
                                    OnePlanetOneRing.orbitalRingStorage.storageShare.TryAdd(pair.OrbitalStationPoolId, new StationStoreIsShares {
                                        StoreIsShare1 = false,
                                        StoreIsShare2 = false,
                                        StoreIsShare3 = false,
                                        StoreIsShare4 = false,
                                        StoreIsShare5 = false
                                    });

                                }
                                if (pair.elevatorPoolId != -1) {
                                    OnePlanetOneRing.orbitalRingStorage.storageShare.TryAdd(pair.elevatorPoolId, new StationStoreIsShares {
                                        StoreIsShare1 = false,
                                        StoreIsShare2 = false,
                                        StoreIsShare3 = false,
                                        StoreIsShare4 = false,
                                        StoreIsShare5 = false
                                    });
                                }
                            }
                        } else {
                            int storageShareCount = r.ReadInt32();
                            for (int y = 0; y < storageShareCount; y++) {
                                OnePlanetOneRing.orbitalRingStorage.storageShare[r.ReadInt32()] = new StationStoreIsShares {
                                    StoreIsShare1 = r.ReadBoolean(),
                                    StoreIsShare2 = r.ReadBoolean(),
                                    StoreIsShare3 = r.ReadBoolean(),
                                    StoreIsShare4 = r.ReadBoolean(),
                                    StoreIsShare5 = r.ReadBoolean()
                                };
                            }
                        }
                        for (int y = 0; y < 1000; y++) {
                            OnePlanetOneRing.insideRingPositions[y] = r.ReadBoolean();
                            OnePlanetOneRing.outsideRingPositions[y] = r.ReadBoolean();
                        }
                        OnePlanetOneRing.isInsideRingComplete = r.ReadBoolean();
                        OnePlanetOneRing.isOutsideRingComplete = r.ReadBoolean();
                        for (int y = 0; y < 1000; y++) {
                            OnePlanetOneRing.lowInsideRingPositions[y] = r.ReadBoolean();
                            OnePlanetOneRing.lowOutsideRingPositions[y] = r.ReadBoolean();
                        }
                        OnePlanetOneRing.isLowInsideRingComplete = r.ReadBoolean();
                        OnePlanetOneRing.isLowOutsideRingComplete = r.ReadBoolean();
                        data.Rings.Add(OnePlanetOneRing);
                    }
                    AllplanetsOrbitalRings[planetId] = data;
                }
            } catch (EndOfStreamException) {
                // ignored
            }
        }

        internal static void IntoOtherSave()
        {
            AllplanetsOrbitalRings.Clear();
        }

    }

    public class PlanetOrbitalRingData
    {
        public int PlanetId { get; }
        public List<EquatorRing> Rings { get; } = new List<EquatorRing>();

        public int planetIncLevel = 0;

        // 赤道上下两圈的索引常量
        public const int UpperRingIndex = 0;
        public const int LowerRingIndex = 1;

        public PlanetOrbitalRingData(int planetId)
        {
            PlanetId = planetId;
        }

        public PlanetOrbitalRingData(int planetId, int ringCapacity)
        {
            PlanetId = planetId;
            // 初始创建两圈（可根据需求动态调整）
            Rings.Add(new EquatorRing(ringCapacity, planetId));
            if (ringCapacity > 20) {
                Rings.Add(new EquatorRing(ringCapacity, planetId));
            }
        }

        public bool IsRingFull()
        {
            for (int ringIndex = 0; ringIndex < Rings.Count; ringIndex++) {
                if (Rings[ringIndex].IsOneFull()) {
                    return true;
                }
            }
            return false;
        }

        public bool IsGlobalPowerActive()
        {
            for (int ringIndex = 0; ringIndex < Rings.Count; ringIndex++) {
                if (Rings[ringIndex].IsOneFull()) {
                    if (Rings[ringIndex].isLowOutsideRingComplete) {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class EquatorRing
    {
        private readonly object _lock = new object();

        private int planetId;
        public int Capacity { get; }
        public int SpaceStationCount => spaceStationCount;

        public bool isParticleCollider = false;

        private int spaceStationCount;
        private readonly Position[] positions;

        public int[] incCoreLevel;

        public OrbitalRingStorage orbitalRingStorage;

        public readonly bool[] insideRingPositions = new bool[1000];
        public readonly bool[] outsideRingPositions = new bool[1000];
        public bool isInsideRingComplete = false;
        public bool isOutsideRingComplete = false;

        public readonly bool[] lowInsideRingPositions = new bool[1000];
        public readonly bool[] lowOutsideRingPositions = new bool[1000];
        public bool isLowInsideRingComplete = false;
        public bool isLowOutsideRingComplete = false;

        //private readonly Dictionary<int, int> elevatorToPosition = new Dictionary<int, int>();
        //private readonly Dictionary<int, int> stationToPosition = new Dictionary<int, int>();

        public EquatorRing(int capacity, int planetId)
        {
            lock (_lock) {
                this.planetId = planetId;
                Capacity = capacity;
                positions = new Position[capacity];
                incCoreLevel = new int[capacity];
                for (int i = 0; i < capacity; i++) {
                    positions[i] = new Position(-1, -1, StationType.None, -1);
                    incCoreLevel[i] = 0;
                }
                orbitalRingStorage = new OrbitalRingStorage();
                for (int i = 0; i < 1000; i++) {
                    insideRingPositions[i] = false;
                    outsideRingPositions[i] = false;
                    lowInsideRingPositions[i] = false; 
                    lowOutsideRingPositions[i] = false;
                }
            }
        }

        private void isOrbitalRingTechunlock()
        {
            if (GameMain.history == null) { return; }
            if (GameMain.history.TechUnlocked(1951)) { return; }
            if (IsOneFull()) {
                GameMain.history.UnlockTech(1951);
            }
        }

        public bool AddElevator(int position, int elevatorId)
        {
            if (position < 0 || position >= Capacity) return false;

            lock (_lock) {
                positions[position].ElevatorPoolId = elevatorId;
                //elevatorToPosition[elevatorId] = position;
                if (elevatorId != -1) {
                    if (positions[position].OrbitalStationType == StationType.Station) {
                        orbitalRingStorage.storageShare.TryAdd(elevatorId, new StationStoreIsShares {
                            StoreIsShare1 = orbitalRingStorage.storageShare[positions[position].OrbitalStationPoolId][0],
                            StoreIsShare2 = orbitalRingStorage.storageShare[positions[position].OrbitalStationPoolId][1],
                            StoreIsShare3 = orbitalRingStorage.storageShare[positions[position].OrbitalStationPoolId][2],
                            StoreIsShare4 = orbitalRingStorage.storageShare[positions[position].OrbitalStationPoolId][3],
                            StoreIsShare5 = orbitalRingStorage.storageShare[positions[position].OrbitalStationPoolId][4]
                        });
                    } else {
                        orbitalRingStorage.storageShare.TryAdd(elevatorId, new StationStoreIsShares {
                            StoreIsShare1 = false,
                            StoreIsShare2 = false,
                            StoreIsShare3 = false,
                            StoreIsShare4 = false,
                            StoreIsShare5 = false
                        });
                    }
                }
            }
            return true;
        }



        public bool AddOrbitalStation(int position, int stationId, StationType stationType)
        {
            if (position < 0 || position >= Capacity) return false;

            lock (_lock) {
                if (positions[position].OrbitalStationType == StationType.None && !OrbitalStationManager.StationTypeIsBase(stationType)) {
                    spaceStationCount++;
                }
                positions[position].OrbitalStationPoolId = stationId;
                positions[position].OrbitalStationType = stationType;

                if (stationType == StationType.Station) {
                    if (positions[position].ElevatorPoolId == -1) {
                        orbitalRingStorage.storageShare.TryAdd(stationId, new StationStoreIsShares {
                            StoreIsShare1 = false,
                            StoreIsShare2 = false,
                            StoreIsShare3 = false,
                            StoreIsShare4 = false,
                            StoreIsShare5 = false
                        });
                    } else {
                        orbitalRingStorage.storageShare.TryAdd(stationId, new StationStoreIsShares {
                            StoreIsShare1 = orbitalRingStorage.storageShare[positions[position].ElevatorPoolId][0],
                            StoreIsShare2 = orbitalRingStorage.storageShare[positions[position].ElevatorPoolId][1],
                            StoreIsShare3 = orbitalRingStorage.storageShare[positions[position].ElevatorPoolId][2],
                            StoreIsShare4 = orbitalRingStorage.storageShare[positions[position].ElevatorPoolId][3],
                            StoreIsShare5 = orbitalRingStorage.storageShare[positions[position].ElevatorPoolId][4]
                        });
                    }
                }
                //stationToPosition[stationId] = position;

                //isOrbitalRingTechunlock();
            }
            return true;
        }

        public bool AddOrbitalCore(int position, int stationId, StationType type)
        {
            if (position < 0 || position >= Capacity) return false;
            lock (_lock) {

                if (OrbitalStationManager.StationTypeIsBase(positions[position].OrbitalStationType)) {
                    spaceStationCount++;
                }
                positions[position].OrbitalCorePoolId = stationId;
                positions[position].OrbitalStationType = type;

                //isOrbitalRingTechunlock();
                
            }
            return true;
        }

        public bool RemoveElevator(int position)
        {
            if (position < 0 || position >= Capacity) return false;

            lock (_lock) {
                orbitalRingStorage.storageShare.Remove(positions[position].ElevatorPoolId);

                positions[position].ElevatorPoolId = -1;
                SetElevatorStorage(position, null);
            }
            return true;
        }

        public bool RemoveOrbitalStation(int position)
        {
            if (position < 0 || position >= Capacity) return false;
            lock (_lock) {
                if (positions[position].OrbitalStationType == StationType.Station) {
                    orbitalRingStorage.storageShare.Remove(positions[position].OrbitalStationPoolId);
                }

                positions[position].OrbitalStationPoolId = -1;
                if (!OrbitalStationManager.StationTypeIsBase(positions[position].OrbitalStationType)) {
                    spaceStationCount--;
                }
                positions[position].OrbitalStationType = StationType.None;
            }
            return true;
        }

        public bool RemoveOrbitalCore(int position)
        {
            if (position < 0 || position >= Capacity) return false;
            lock (_lock) {
                positions[position].OrbitalCorePoolId = -1;
                positions[position].OrbitalStationType = (GetBase(positions[position].OrbitalStationType));
                incCoreLevel[position] = 0;
                spaceStationCount--;
            }
            return true;
        }

        public void SetElevatorStorage(int position, StationStore[] storage)
        {
            if (position < 0 || position >= Capacity) return;
            lock (_lock) {
                positions[position].ElevatorStorage = storage;
            }
        }

        public StationStore[] GetElevatorStorage(int position)
        {
            if (position < 0 || position >= Capacity) return null;
            return positions[position].ElevatorStorage;
        }

        public void AddRing(int ringPosition, int ringIndex, bool isLowRing)
        {
            if (ringIndex == 1) {
                if (isLowRing) {
                    lowInsideRingPositions[ringPosition] = true;
                    ChecRingComplete(lowInsideRingPositions, ref isLowInsideRingComplete);
                } else {
                    insideRingPositions[ringPosition] = true;
                    ChecRingComplete(insideRingPositions, ref isInsideRingComplete);
                    isOrbitalRingTechunlock();
                }
            } else if (ringIndex == 2) {
                if (isLowRing) {
                    lowOutsideRingPositions[ringPosition] = true;
                    ChecRingComplete(lowOutsideRingPositions, ref isLowOutsideRingComplete);
                } else {
                    outsideRingPositions[ringPosition] = true;
                    ChecRingComplete(outsideRingPositions, ref isOutsideRingComplete);
                    isOrbitalRingTechunlock();
                }
            }
            if (IsOneFull()) {
                if (isLowOutsideRingComplete) {
                    GlobalPowerActive(planetId);
                }
            }
        }
        public void DelRing(int ringPosition, int ringIndex, bool isLowRing)
        {
            if (ringIndex == 1) {
                if (isLowRing) {
                    lowInsideRingPositions[ringPosition] = false;
                    isLowInsideRingComplete = false;
                } else {
                    insideRingPositions[ringPosition] = false;
                    isInsideRingComplete = false;
                }
            } else if (ringIndex == 2) {
                if (isLowRing) {
                    lowOutsideRingPositions[ringPosition] = false;
                    isLowOutsideRingComplete = false;
                } else {
                    outsideRingPositions[ringPosition] = false;
                    isOutsideRingComplete = false;
                }
            }
            bool flag = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId).IsGlobalPowerActive();
            if (!flag) {
                GlobalPowerInActive(planetId);
            }
        }



        public void CheckRingComplete(bool flag)
        {
            int count = 0;
            isInsideRingComplete = true;
            for (int i = 0; i < insideRingPositions.Length; i++) {
                if (Capacity == 20 && (i % 2 != 0)) continue; 
                if (!insideRingPositions[i]) {
                    isInsideRingComplete = false;
                    count++;
                    //break;
                }
            }
            if (flag) {
                LogError($" 1 ring not complete {count}");
            }

            //LogError($" 1 ring count {count}");
            count = 0;
            isOutsideRingComplete = true;
            for (int i = 0; i < outsideRingPositions.Length; i++) {
                if (Capacity == 20 && (i % 2 != 0)) continue;
                if (!outsideRingPositions[i]) {
                    isOutsideRingComplete = false;
                    count++;
                    //break;
                }
            }
            if (flag) {
                LogError($" 1 ring not complete {count}");
            }
        }

        public void ChecRingComplete(bool[] ring, ref bool theCompleteStatue)
        {
            theCompleteStatue = true;
            for (int i = 0; i < ring.Length; i++) {
                if (Capacity == 20 && (i % 2 != 0)) continue;
                if (!ring[i]) {
                    theCompleteStatue = false;
                    break;
                }
            }
        }

        public bool IsOneFull() => (isInsideRingComplete || isOutsideRingComplete);

        public bool IsAllFull() => (isInsideRingComplete && isOutsideRingComplete);

        public (int elevatorPoolId, int OrbitalStationPoolId, StationType stationType, int OrbitalCorePoolId) GetPair(int position)
        {
            if (position < 0 || position >= Capacity) return (-1, -1, StationType.None, -1);
            return (positions[position].ElevatorPoolId, positions[position].OrbitalStationPoolId, positions[position].OrbitalStationType, positions[position].OrbitalCorePoolId);
        }

        private class Position
        {
            public int ElevatorPoolId { get; set; }
            public int OrbitalStationPoolId { get; set; }

            public StationType OrbitalStationType { get; set; }

            public StationStore[] ElevatorStorage = null;

            public int OrbitalCorePoolId { get; set; }

            public Position(int elevatorId, int stationId, StationType type, int CoreId)
            {
                ElevatorPoolId = elevatorId;
                OrbitalStationPoolId = stationId;
                OrbitalStationType = type;
                OrbitalCorePoolId = CoreId;
            }
        }

        private StationType GetBase(StationType type)
        {
            switch (type) {
                case StationType.PowerGenCore:
                    return StationType.PowerGenBase;
                case StationType.TurretCore:
                case StationType.EjectorCore:
                    return StationType.TurretBase;
                case StationType.ATFeildCore:
                case StationType.GlobalIncCore:
                case StationType.SynapticLathe:
                case StationType.BanDFTinderDispatch:
                    return StationType.GlobalSupportBase;
                default:
                    return type;
            }
        }
    

        public enum StationType
        {
            None = 0,
            Station = 1,
            Assembler = 2,
            SynapticLathe = 3,
            PowerGenBase = 4,
            PowerGenCore = 5,
            TurretBase = 6,
            TurretCore = 7,
            EjectorCore = 9,
            GlobalSupportBase = 10,
            ATFeildCore = 11,
            GlobalIncCore = 12,
            BanDFTinderDispatch = 13
        }
    }
}
