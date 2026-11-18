using System;
using System.Collections.Generic;
using System.IO;
using NebulaAPI;
using NebulaAPI.Interfaces;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.Farm;
using ProjectOrbitalRing.Patches.Logic.MegaAssembler;
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;
using ProjectOrbitalRing.Patches.Logic.PowerExchangerOutput;
using ProjectOrbitalRing.Patches.Logic.QuantumStorage;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace ProjectOrbitalRing.Utils
{
    public class SyncSlotsData
    {
        public SyncSlotsData() {}

        public SyncSlotsData(int planetId, int entityId, SlotData[] data)
        {
            PlanetId = planetId;
            EntityId = entityId;

            using (IWriterProvider p = NebulaModAPI.GetBinaryWriter())
            {
                BinaryWriter w = p.BinaryWriter;

                w.Write(data.Length);

                for (var i = 0; i < data.Length; i++)
                {
                    w.Write((int)data[i].dir);
                    w.Write(data[i].beltId);
                    w.Write(data[i].storageIdx);
                    w.Write(data[i].counter);
                }

                SlotsData = p.CloseAndGetBytes();
            }
        }

        public int PlanetId { get; set; }
        public int EntityId { get; set; }
        public byte[] SlotsData { get; set; }

        internal static void Sync(int planetId, int entityId, SlotData[] slotsData)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncSlotsData(planetId, entityId, slotsData));
        }

        internal static void OnReceive(int planetId, int entityId, byte[] data)
        {
            SlotData[] slotsData;

            using (IReaderProvider p = NebulaModAPI.GetBinaryReader(data))
            using (BinaryReader r = p.BinaryReader)
            {
                int length = r.ReadInt32();
                if (length < 0 || length > 16 || data.Length != 4 + length * 16) return;
                slotsData = new SlotData[length];

                for (var i = 0; i < length; i++)
                {
                    slotsData[i] = new SlotData
                    {
                        dir = (IODir)r.ReadInt32(),
                        beltId = r.ReadInt32(),
                        storageIdx = r.ReadInt32(),
                        counter = r.ReadInt32(),
                    };
                }

                p.BinaryReader.Close();
            }

            MegaAssemblerPatches.SyncSlots((planetId, entityId), slotsData);
        }
    }

    [RegisterPacketProcessor]
    public class SyncSlotsDataProcessor : BasePacketProcessor<SyncSlotsData>
    {
        public override void ProcessPacket(SyncSlotsData packet, INebulaConnection conn)
        {
            if (packet?.SlotsData == null || packet.SlotsData.Length < 4 || packet.SlotsData.Length > 260) return;
            SyncSlotsData.OnReceive(packet.PlanetId, packet.EntityId, packet.SlotsData);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncSlotData
    {
        public SyncSlotData() {}

        public SyncSlotData(int planetId, int slotId, int entityId, SlotData data)
        {
            PlanetId = planetId;
            SlotId = slotId;
            EntityId = entityId;

            using (IWriterProvider p = NebulaModAPI.GetBinaryWriter())
            {
                BinaryWriter w = p.BinaryWriter;

                w.Write((int)data.dir);
                w.Write(data.beltId);
                w.Write(data.storageIdx);
                w.Write(data.counter);

                SlotData = p.CloseAndGetBytes();
            }
        }

        public int PlanetId { get; set; }
        public int SlotId { get; set; }
        public int EntityId { get; set; }

        public byte[] SlotData { get; set; }

        internal static void Sync(int planetId, int slotId, int entityId, SlotData slotData)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncSlotData(planetId, slotId, entityId, slotData));
        }

        internal static void OnReceive(int planetId, int slotId, int entityId, byte[] data)
        {
            SlotData slotData;

            using (IReaderProvider p = NebulaModAPI.GetBinaryReader(data))
            using (BinaryReader r = p.BinaryReader)
            {
                slotData = new SlotData
                {
                    dir = (IODir)r.ReadInt32(),
                    beltId = r.ReadInt32(),
                    storageIdx = r.ReadInt32(),
                    counter = r.ReadInt32(),
                };


                p.BinaryReader.Close();
            }

            MegaAssemblerPatches.SyncSlot((planetId, entityId), slotId, slotData);
        }
    }

    [RegisterPacketProcessor]
    public class SyncSlotDataProcessor : BasePacketProcessor<SyncSlotData>
    {
        public override void ProcessPacket(SyncSlotData packet, INebulaConnection conn)
        {
            if (packet?.SlotData == null || packet.SlotData.Length != 16 || packet.SlotId < 0 || packet.SlotId >= 16) return;
            SyncSlotData.OnReceive(packet.PlanetId, packet.SlotId, packet.EntityId, packet.SlotData);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncPlanetFocusData
    {
        public SyncPlanetFocusData() {}

        public SyncPlanetFocusData(int planetId, int index, int focusId)
        {
            PlanetId = planetId;
            Index = index;
            FocusId = focusId;
        }

        public int PlanetId { get; set; }
        public int Index { get; set; }
        public int FocusId { get; set; }

        internal static void Sync(int planetId, int index, int focusId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncPlanetFocusData(planetId, index, focusId));
        }

        internal static void OnReceive(int planetId, int index, int focusId) =>
            PlanetFocusPatches.SyncPlanetFocus(planetId, index, focusId);
    }

    [RegisterPacketProcessor]
    public class SyncPlanetFocusDataProcessor : BasePacketProcessor<SyncPlanetFocusData>
    {
        public override void ProcessPacket(SyncPlanetFocusData packet, INebulaConnection conn)
        {
            if (packet == null || packet.Index < 0 || packet.Index >= PlanetFocusPatches.FocusMaxCount ||
                !PlanetFocusPatches.FocusIds.ContainsKey(packet.FocusId)) return;
            SyncPlanetFocusData.OnReceive(packet.PlanetId, packet.Index, packet.FocusId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncNewQuantumStorageData
    {
        public SyncNewQuantumStorageData() {}

        public SyncNewQuantumStorageData(int planetId, int storageId, int orbitId)
        {
            PlanetId = planetId;
            StorageId = storageId;
            OrbitId = orbitId;
        }

        public int PlanetId { get; set; }
        public int StorageId { get; set; }
        public int OrbitId { get; set; }

        internal static void Sync(int planetId, int storageId, int orbitId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncNewQuantumStorageData(planetId, storageId, orbitId));
        }

        internal static void OnReceive(int planetId, int storageId, int orbitId) =>
            QuantumStoragePatches.SyncNewQuantumStorage(planetId, storageId, orbitId);
    }

    [RegisterPacketProcessor]
    public class SyncNewQuantumStorageDataProcessor : BasePacketProcessor<SyncNewQuantumStorageData>
    {
        public override void ProcessPacket(SyncNewQuantumStorageData packet, INebulaConnection conn)
        {
            if (packet == null || packet.OrbitId < 1 || packet.OrbitId > 10 || packet.StorageId <= 0) return;
            SyncNewQuantumStorageData.OnReceive(packet.PlanetId, packet.StorageId, packet.OrbitId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncRemoveQuantumStorageData
    {
        public SyncRemoveQuantumStorageData() {}

        public SyncRemoveQuantumStorageData(int planetId, int storageId, int orbitId)
        {
            PlanetId = planetId;
            StorageId = storageId;
            OrbitId = orbitId;
        }

        public int PlanetId { get; set; }
        public int StorageId { get; set; }
        public int OrbitId { get; set; }

        internal static void Sync(int planetId, int storageId, int orbitId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncRemoveQuantumStorageData(planetId, storageId, orbitId));
        }

        internal static void OnReceive(int planetId, int storageId, int orbitId) =>
            QuantumStoragePatches.SyncRemoveQuantumStorage(planetId, storageId, orbitId);
    }

    [RegisterPacketProcessor]
    public class SyncRemoveQuantumStorageDataProcessor : BasePacketProcessor<SyncRemoveQuantumStorageData>
    {
        public override void ProcessPacket(SyncRemoveQuantumStorageData packet, INebulaConnection conn)
        {
            if (packet == null || packet.OrbitId < 1 || packet.OrbitId > 10 || packet.StorageId <= 0) return;
            SyncRemoveQuantumStorageData.OnReceive(packet.PlanetId, packet.StorageId, packet.OrbitId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncQuantumStorageOrbitChangeData
    {
        public SyncQuantumStorageOrbitChangeData() {}

        public SyncQuantumStorageOrbitChangeData(int planetId, int storageId, int orbitId)
        {
            PlanetId = planetId;
            StorageId = storageId;
            OrbitId = orbitId;
        }

        public int PlanetId { get; set; }
        public int StorageId { get; set; }
        public int OrbitId { get; set; }

        internal static void Sync(int planetId, int storageId, int orbitId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncQuantumStorageOrbitChangeData(planetId, storageId, orbitId));
        }

        internal static void OnReceive(int planetId, int storageId, int orbitId) =>
            QuantumStoragePatches.SyncQuantumStorageOrbitChange(planetId, storageId, orbitId);
    }

    [RegisterPacketProcessor]
    public class SyncQuantumStorageOrbitChangeDataProcessor : BasePacketProcessor<SyncQuantumStorageOrbitChangeData>
    {
        public override void ProcessPacket(SyncQuantumStorageOrbitChangeData packet, INebulaConnection conn)
        {
            if (packet == null || packet.OrbitId < 1 || packet.OrbitId > 10 || packet.StorageId <= 0) return;
            SyncQuantumStorageOrbitChangeData.OnReceive(packet.PlanetId, packet.StorageId, packet.OrbitId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncGlobalPowerSupplyNodeIdData
    {
        public SyncGlobalPowerSupplyNodeIdData() {}

        public SyncGlobalPowerSupplyNodeIdData(int planetId, int nodeId)
        {
            PlanetId = planetId;
            NodeId = nodeId;
        }

        public int PlanetId { get; set; }
        public int NodeId { get; set; }

        internal static void Sync(int planetId, int nodeId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncGlobalPowerSupplyNodeIdData(planetId, nodeId));
        }

        internal static void OnReceive(int planetId, int nodeId) => GlobalPowerSupplyPatches.SyncGlobalPowerSupplyNodeId(planetId, nodeId);
    }

    [RegisterPacketProcessor]
    public class SyncGlobalPowerSupplyNodeIdDataProcessor : BasePacketProcessor<SyncGlobalPowerSupplyNodeIdData>
    {
        public override void ProcessPacket(SyncGlobalPowerSupplyNodeIdData packet, INebulaConnection conn)
        {
            if (packet == null || packet.NodeId <= 0) return;
            SyncGlobalPowerSupplyNodeIdData.OnReceive(packet.PlanetId, packet.NodeId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class SyncSyncGlobalPowerSupplyRemoveData
    {
        public SyncSyncGlobalPowerSupplyRemoveData() {}

        public SyncSyncGlobalPowerSupplyRemoveData(int planetId) => PlanetId = planetId;

        public int PlanetId { get; set; }

        internal static void Sync(int planetId)
        {
            if (NebulaModAPI.IsMultiplayerActive && !MultiplayerSync.IsRemoteFactoryRequest)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncSyncGlobalPowerSupplyRemoveData(planetId));
        }

        internal static void OnReceive(int planetId) => GlobalPowerSupplyPatches.SyncGlobalPowerSupplyRemoved(planetId);
    }

    [RegisterPacketProcessor]
    public class SyncSyncGlobalPowerSupplyRemoveDataProcessor : BasePacketProcessor<SyncSyncGlobalPowerSupplyRemoveData>
    {
        public override void ProcessPacket(SyncSyncGlobalPowerSupplyRemoveData packet, INebulaConnection conn)
        {
            if (packet == null) return;
            SyncSyncGlobalPowerSupplyRemoveData.OnReceive(packet.PlanetId);
            MultiplayerSync.Relay(packet, conn);
        }
    }

    public class OrbitalRingPlanetLoadRequest
    {
        public OrbitalRingPlanetLoadRequest() {}

        public OrbitalRingPlanetLoadRequest(int planetId) => PlanetId = planetId;

        public int PlanetId { get; set; }
    }

    [RegisterPacketProcessor]
    public class OrbitalRingPlanetLoadRequestProcessor : BasePacketProcessor<OrbitalRingPlanetLoadRequest>
    {
        public override void ProcessPacket(OrbitalRingPlanetLoadRequest packet, INebulaConnection conn)
        {
            if (!IsHost || packet == null || GameMain.galaxy?.PlanetById(packet.PlanetId) == null) return;

            byte[] data;

            using (IWriterProvider p = NebulaModAPI.GetBinaryWriter())
            {
                MegaAssemblerPatches.ExportPlanetData(packet.PlanetId, p.BinaryWriter);
                PlanetFocusPatches.ExportPlanetFocus(packet.PlanetId, p.BinaryWriter);
                QuantumStoragePatches.ExportPlanetQuantumStorage(packet.PlanetId, p.BinaryWriter);
                p.BinaryWriter.Write(ProjectOrbitalRing.VersionNumber());
                OrbitalStationManager.Export(p.BinaryWriter, packet.PlanetId);
                AssemblerModulePatches.ExportPlanetData(packet.PlanetId, p.BinaryWriter);
                FarmAssembler.Export(p.BinaryWriter);
                InfiniteTank.Export(p.BinaryWriter);
                data = p.CloseAndGetBytes();
            }

            conn.SendPacket(new OrbitalRingPlanetData(packet.PlanetId, data));
        }
    }

    public class OrbitalRingPlanetData
    {
        public OrbitalRingPlanetData() {}

        public OrbitalRingPlanetData(int id, byte[] data)
        {
            PlanetId = id;
            BinaryData = data;
        }

        public int PlanetId { get; set; }
        public byte[] BinaryData { get; set; }
    }

    [RegisterPacketProcessor]
    public class OrbitalRingPlanetDataProcessor : BasePacketProcessor<OrbitalRingPlanetData>
    {
        internal static readonly Dictionary<int, byte[]> PendingData = new Dictionary<int, byte[]>();
        private static readonly HashSet<int> LoadingPlanets = new HashSet<int>();

        /// <inheritdoc />
        public override void ProcessPacket(OrbitalRingPlanetData packet, INebulaConnection conn)
        {
            if (IsHost || packet?.BinaryData == null || GameMain.galaxy?.PlanetById(packet.PlanetId) == null) return;
            PendingData[packet.PlanetId] = packet.BinaryData;
            ProcessBytesLater(packet.PlanetId);
        }

        /// <summary>工厂导入前暂存附加状态，避免读到旧的组件池。</summary>
        /// <param name="planetId">开始载入的星球。</param>
        internal static void Loading(int planetId)
        {
            LoadingPlanets.Add(planetId);
            PendingData.Remove(planetId);
        }

        /// <summary>原版工厂载入完成后恢复插件和星环，包晚到时也能直接恢复。</summary>
        /// <param name="planetId">已完成载入的星球。</param>
        internal static void PlanetLoaded(int planetId)
        {
            LoadingPlanets.Remove(planetId);
            ProcessBytesLater(planetId);
        }

        /// <summary>清除上一房间的载入缓存。</summary>
        internal static void Reset()
        {
            PendingData.Clear();
            LoadingPlanets.Clear();
        }

        /// <summary>仅在对应工厂就绪后消费附加快照。</summary>
        /// <param name="planetId">快照所属星球。</param>
        public static void ProcessBytesLater(int planetId)
        {
            if (!MultiplayerSync.IsClient || LoadingPlanets.Contains(planetId) ||
                GameMain.galaxy?.PlanetById(planetId)?.factory == null ||
                !PendingData.Remove(planetId, out byte[] bytes)) return;
            try {
                using (IReaderProvider p = NebulaModAPI.GetBinaryReader(bytes)) {
                    MegaAssemblerPatches.ImportPlanetData(p.BinaryReader);
                    PlanetFocusPatches.ImportPlanetFocus(p.BinaryReader);
                    QuantumStoragePatches.ImportPlanetQuantumStorage(p.BinaryReader);
                    int version = p.BinaryReader.ReadInt32();
                    OrbitalStationManager.ImportPlanetData(p.BinaryReader, planetId, version);
                    AssemblerModulePatches.ImportPlanetData(planetId, p.BinaryReader);
                    FarmAssembler.Import(p.BinaryReader);
                    InfiniteTank.Import(p.BinaryReader);
                }
            } catch (Exception e) when (e is IOException || e is ArgumentException) {
                ProjectOrbitalRing.LogError($"Cannot import OrbitalRing multiplayer data for planet {planetId}: {e.Message}");
            }
        }
    }
}
