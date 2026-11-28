using ProjectOrbitalRing.Utils;
using System.Collections.Generic;
using System.IO;
using static ProjectOrbitalRing.Patches.Logic.GlobalPowerSupplyPatches;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
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

        public void AddPlanetId(int planetId, OrbitalRingLayout layout)
        {
            if (!AllplanetsOrbitalRings.ContainsKey(planetId))
            {
                AllplanetsOrbitalRings[planetId] = new PlanetOrbitalRingData(planetId, layout);
            }
        }

        public PlanetOrbitalRingData GetPlanetOrbitalRingData(int planetId) => AllplanetsOrbitalRings.GetValueOrDefault(planetId);


        /// <summary>导出全部星环，或只导出指定星球；省略星球时保持磁盘存档布局。</summary>
        /// <param name="w">目标数据流。</param>
        /// <param name="planetId">指定星球，0 表示全部星球。</param>
        internal static void Export(BinaryWriter w, int planetId = 0)
        {
            w.Write(planetId == 0 ? AllplanetsOrbitalRings.Count : AllplanetsOrbitalRings.ContainsKey(planetId) ? 1 : 0);
            foreach (var outerPair in AllplanetsOrbitalRings) {
                if (planetId != 0 && outerPair.Key != planetId) continue;
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
                    var items = outerPair.Value.Rings[i].orbitalRingStorage.storageItem;
                    lock (items) {
                        w.Write(items.Count);
                        foreach (var storageItem in items) {
                            int itemId = storageItem.Key;
                            int[] countInc = storageItem.Value;
                            w.Write(itemId);
                            w.Write(countInc[0]);
                            w.Write(countInc[1]);
                        }
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
                    w.Write(outerPair.Value.Rings[i].orbitalRingStorage.storageLimit.Count);
                    foreach (var storageItem in outerPair.Value.Rings[i].orbitalRingStorage.storageLimit) {
                        int itemId = storageItem.Key;
                        int customLimit = storageItem.Value;
                        w.Write(itemId);
                        w.Write(customLimit);
                    }

                    w.Write(outerPair.Value.Rings[i].isInsideRingComplete);
                    w.Write(outerPair.Value.Rings[i].isOutsideRingComplete);
                    w.Write(outerPair.Value.Rings[i].isLowInsideRingComplete);
                    w.Write(outerPair.Value.Rings[i].isLowOutsideRingComplete);
                }
            }
        }
        /// <summary>读取星环；局部联机快照保留其他星球，按发送端的存档版本解释字段。</summary>
        /// <param name="r">来源数据流。</param>
        /// <param name="clear">是否先清空全部星球。</param>
        /// <param name="version">发送端版本；0 表示当前磁盘导入版本。</param>
        internal static void Import(BinaryReader r, bool clear = true, int version = 0)
        {
            if (clear) IntoOtherSave();
            if (version == 0) version = ProjectOrbitalRing.importVersion;
            try {
                // 读取外层字典条目数
                int AllplanetsOrbitalRingsCount = r.ReadInt32();

                for (int i = 0; i < AllplanetsOrbitalRingsCount; i++) {
                    int planetId = r.ReadInt32();
                    PlanetOrbitalRingData data = new PlanetOrbitalRingData(planetId);
                    // 读取内层字典条目数
                    int OnePlanetRingsCount = r.ReadInt32();
                    if (version < 524314) {
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
                            if (version < 524314) {
                                OnePlanetOneRing.incCoreLevel[k] = 0;
                            } else {
                                OnePlanetOneRing.incCoreLevel[k] = r.ReadInt32();
                            }
                            
                        }
                        int storageItemCount = r.ReadInt32();
                        for (int y = 0; y < storageItemCount; y++) {
                            OnePlanetOneRing.orbitalRingStorage.storageItem.TryAdd(r.ReadInt32(), new int[] { r.ReadInt32(), r.ReadInt32() });
                        }
                        if (version < 589858) {
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
                        // 16777222 = 1.0.6版本，1.0.6之前没有storageLimit
                        if (version > 16777222) {
                            int storageLimitCount = r.ReadInt32();
                            for (int y = 0; y < storageLimitCount; y++) {
                                OnePlanetOneRing.orbitalRingStorage.storageLimit[r.ReadInt32()] = r.ReadInt32();
                            }
                        }

                        // 1.0.9重构了是否成环判定的逻辑，不再需要记录每个环带子的坐标是否有带子，久版本存档的数据读出来后直接丢弃
                        if (version <= 16777224) {
                            for (int y = 0; y < 1000; y++) {
                                r.ReadBoolean();
                                r.ReadBoolean();
                            }
                        }
                        OnePlanetOneRing.isInsideRingComplete = r.ReadBoolean();
                        OnePlanetOneRing.isOutsideRingComplete = r.ReadBoolean();
                        if (version <= 16777224) {
                            for (int y = 0; y < 1000; y++) {
                                r.ReadBoolean();
                                r.ReadBoolean();
                            }
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

        /// <summary>替换一个已加载星球的星环，并重新关联原版物流站的储物槽引用。</summary>
        /// <param name="r">仅包含该星球的导出数据。</param>
        /// <param name="planetId">快照所属星球。</param>
        /// <param name="version">发送端存档版本。</param>
        internal static void ImportPlanetData(BinaryReader r, int planetId, int version)
        {
            AllplanetsOrbitalRings.Remove(planetId);
            Import(r, false, version);
            PlanetFactory factory = GameMain.galaxy?.PlanetById(planetId)?.factory;
            PlanetOrbitalRingData data = Instance.GetPlanetOrbitalRingData(planetId);
            if (factory?.transport == null || data == null) return;
            foreach (EquatorRing ring in data.Rings) {
                for (int i = 0; i < ring.Capacity; i++) {
                    var pair = ring.GetPair(i);
                    int stationId = pair.elevatorPoolId;
                    if (stationId <= 0 || stationId >= factory.transport.stationCursor) continue;
                    StationComponent station = factory.transport.stationPool[stationId];
                    if (station == null || station.id != stationId) continue;
                    ring.SetElevatorStorage(i, station.storage);
                    int orbitalId = pair.OrbitalStationPoolId;
                    if (pair.stationType == StationType.Station && orbitalId > 0 && orbitalId < factory.transport.stationCursor) {
                        StationComponent orbital = factory.transport.stationPool[orbitalId];
                        if (orbital != null && orbital.id == orbitalId) orbital.storage = station.storage;
                    }
                }
            }
        }

        internal static void IntoOtherSave()
        {
            AllplanetsOrbitalRings.Clear();
            ClearLayoutCache();
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

        internal PlanetOrbitalRingData(int planetId, OrbitalRingLayout layout)
        {
            PlanetId = planetId;
            for (int i = 0; i < layout.StationRingCount; i++)
            {
                Rings.Add(new EquatorRing(layout.StationCapacity, planetId));
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

        public bool isInsideRingComplete = false;
        public bool isOutsideRingComplete = false;

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
                // 清零全球增产，反正全球增产每秒一刷，拆的如果不是全球增产那增产状态会刷回来，清零无所谓的
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

        public void CheckRingIsComplete(PlanetFactory factory, int beltId, int ringBeltIndex, int itemId)
        {
            CargoPath cargoPath = factory.cargoTraffic.GetCargoPath(factory.cargoTraffic.beltPool[beltId].segPathId);
            if (cargoPath == null) {
                return;
            }
            if (cargoPath.closed == true) {
                if (itemId == ProtoID.I轨道连接组件) {
                    if (ringBeltIndex == 1) {
                        isInsideRingComplete = true;
                    } else if (ringBeltIndex == 2) {
                        isOutsideRingComplete = true;
                    }
                    isOrbitalRingTechunlock();
                } else if (itemId == ProtoID.I粒子加速轨道) {
                    isLowInsideRingComplete = true;
                } else if (itemId == ProtoID.I星环电网组件) {
                    isLowOutsideRingComplete = true;
                }
            }
            if (IsOneFull()) {
                if (isLowOutsideRingComplete) {
                    GlobalPowerActive(planetId);
                }
            }
        }

        public void DelRing(int ringIndex, bool isLowRing)
        {
            LogError($"DelRing ringIndex {ringIndex} isLowRing {isLowRing}");
            if (ringIndex == 1) {
                if (isLowRing) {
                    isLowInsideRingComplete = false;
                } else {
                    isInsideRingComplete = false;
                }
            } else if (ringIndex == 2) {
                if (isLowRing) {
                    isLowOutsideRingComplete = false;
                } else {
                    isOutsideRingComplete = false;
                }
            }
            bool flag = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId).IsGlobalPowerActive();
            if (!flag) {
                GlobalPowerInActive(planetId);
            }
        }

        internal int FindParticleColliderEntity(PlanetFactory factory)
        {
            var assemblers = factory.factorySystem?.assemblerPool;
            if (assemblers == null) return 0;
            for (int slot = 0; slot < Capacity; slot++) {
                var pair = GetPair(slot);
                int assemblerId = pair.OrbitalStationPoolId;
                if (pair.stationType != StationType.Assembler || assemblerId <= 0 || assemblerId >= assemblers.Length)
                    continue;

                ref AssemblerComponent assembler = ref assemblers[assemblerId];
                int entityId = assembler.entityId;
                if (assembler.id != assemblerId || entityId <= 0 || entityId >= factory.entityPool.Length) continue;
                ref EntityData entity = ref factory.entityPool[entityId];
                if (entity.id == entityId && entity.protoId == ProtoID.I星环对撞机 && entity.assemblerId == assemblerId)
                    return entityId;
            }
            return 0;
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
