using Compressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.GlobalPowerSupplyPatches;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;

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

        public void AddPlanetId(int planetId, int ringCapacity = 40)
        {
            if (AllplanetsOrbitalRings.ContainsKey(planetId)) return;
            AllplanetsOrbitalRings[planetId] = new PlanetOrbitalRingData(planetId, ringCapacity);
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
                    }
                }
            }
        }
        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try {
                // 读取外层字典条目数
                int AllplanetsOrbitalRingsCount = r.ReadInt32();

                for (int i = 0; i < AllplanetsOrbitalRingsCount; i++) {
                    int planetId = r.ReadInt32();
                    PlanetOrbitalRingData data = new PlanetOrbitalRingData(planetId);
                    // 读取内层字典条目数
                    int OnePlanetRingsCount = r.ReadInt32();
                    for (var j = 0; j < OnePlanetRingsCount; j++) {
                        EquatorRing OnePlanetOneRing = new EquatorRing(r.ReadInt32(), planetId);
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
                        }
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

        public int split_inc(ref int n, ref int m, int p)
        {
            if (n == 0) {
                return 0;
            }
            int num = m / n;
            int num2 = m - num * n;
            n -= p;
            num2 -= n;
            num = ((num2 > 0) ? (num * p + num2) : (num * p));
            m -= num;
            return num;
        }
    }

    public class PlanetOrbitalRingData
    {
        public int PlanetId { get; }
        public List<EquatorRing> Rings { get; } = new List<EquatorRing>();

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
            Rings.Add(new EquatorRing(ringCapacity, planetId));
        }

        public bool IsRingFull()
        {
            for (int ringIndex = 0; ringIndex < Rings.Count; ringIndex++) {
                if (Rings[ringIndex].IsFull()) {
                    return true;
                }
            }
            return false;
        }

        public bool IsGlobalPowerActive()
        {
            for (int ringIndex = 0; ringIndex < Rings.Count; ringIndex++) {
                if (Rings[ringIndex].IsFull()) {
                    for (int i = 0; i < Rings[ringIndex].Capacity; i++) {
                        var pair = Rings[ringIndex].GetPair(i);
                        if (pair.stationType == EquatorRing.StationType.GlobalPower) {
                            return true;
                        }
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
        //private readonly Dictionary<int, int> elevatorToPosition = new Dictionary<int, int>();
        //private readonly Dictionary<int, int> stationToPosition = new Dictionary<int, int>();

        public EquatorRing(int capacity, int planetId)
        {
            lock (_lock) {
                this.planetId = planetId;
                Capacity = capacity;
                positions = new Position[capacity];
                for (int i = 0; i < capacity; i++) {
                    positions[i] = new Position(-1, -1, StationType.None, -1);
                }
            }
        }

        private void isOrbitalRingTechunlock()
        {
            if (GameMain.history == null) { return; }
            if (GameMain.history.TechUnlocked(1951)) { return; }
            if (IsFull()) {
                Debug.LogFormat("scpppppppppppppppppppp spaceStationCount {0} Capacity {1}", spaceStationCount, Capacity);
                GameMain.history.UnlockTech(1951);
            }
        }

        public bool AddElevator(int position, int elevatorId)
        {
            if (position < 0 || position >= Capacity) return false;

            lock (_lock) {
                positions[position].ElevatorPoolId = elevatorId;
                //elevatorToPosition[elevatorId] = position;
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
                //stationToPosition[stationId] = position;

                isOrbitalRingTechunlock();
                if (IsFull()) {
                    for (int i = 0; i < Capacity; i++) {
                        if (positions[i].OrbitalStationType == StationType.GlobalPower) {
                            GlobalPowerActive(planetId);
                        }
                    }
                }
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

                isOrbitalRingTechunlock();
                if (IsFull()) {
                    for (int i = 0; i < Capacity; i++) {
                        if (positions[i].OrbitalStationType == StationType.GlobalPower) {
                            GlobalPowerActive(planetId);
                            break;
                        }
                    }
                }
            }
            return true;
        }

        public bool RemoveElevator(int position)
        {
            if (position < 0 || position >= Capacity) return false;

            lock (_lock) {
                positions[position].ElevatorPoolId = -1;
                SetElevatorStorage(position, null);
            }
            return true;
        }

        public bool RemoveOrbitalStation(int position)
        {
            if (position < 0 || position >= Capacity) return false;
            lock (_lock) {
                positions[position].OrbitalStationPoolId = -1;
                if (!OrbitalStationManager.StationTypeIsBase(positions[position].OrbitalStationType)) {
                    spaceStationCount--;
                }
                positions[position].OrbitalStationType = StationType.None;

                bool flag = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId).IsGlobalPowerActive();
                if (!flag) {
                    GlobalPowerInActive(planetId);
                }
            }
            return true;
        }

        public bool RemoveOrbitalCore(int position)
        {
            if (position < 0 || position >= Capacity) return false;
            lock (_lock) {
                positions[position].OrbitalCorePoolId = -1;
                positions[position].OrbitalStationType = (positions[position].OrbitalStationType - 1); // core的枚举减一就是对应的base
                spaceStationCount--;
                bool flag = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId).IsGlobalPowerActive();
                if (!flag) {
                    GlobalPowerInActive(planetId);
                }
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

        public bool IsFull() => spaceStationCount >= Capacity;

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

        public enum StationType
        {
            None = 0,
            Station = 1,
            Assembler = 2,
            GlobalPower = 3,
            PowerGenBase = 4,
            PowerGenCore = 5,
            TurretBase = 6,
            TurretCore = 7,
            EjectorCore = 9,
        }
    }
}
