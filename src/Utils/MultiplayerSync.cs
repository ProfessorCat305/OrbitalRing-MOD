using System;
using System.IO;
using NebulaAPI;
using NebulaAPI.Interfaces;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine;
using ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;

namespace ProjectOrbitalRing.Utils
{
    /// <summary>管理现有 Nebula 接入的会话、首次载入和操作转发；不接管各端日常模拟。</summary>
    internal static class MultiplayerSync
    {
        /// <summary>附加到模组握手版本的联机协议修订。</summary>
        internal const int ProtocolVersion = 3;
        private static bool requestedGlobal;

        /// <summary>当前是否为联机主机。</summary>
        internal static bool IsHost => NebulaModAPI.IsMultiplayerActive &&
            NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost;

        /// <summary>当前是否为联机客户端。</summary>
        internal static bool IsClient => NebulaModAPI.IsMultiplayerActive &&
            NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;

        /// <summary>当前建筑操作是否由其他玩家的数据包重放。</summary>
        internal static bool IsRemoteFactoryRequest => NebulaModAPI.IsMultiplayerActive &&
            NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.Value &&
            NebulaModAPI.MultiplayerSession.Factories.PacketAuthor != NebulaModAPI.MultiplayerSession.LocalPlayer.Id;

        /// <summary>注册生命周期；插件销毁时由 Shutdown 移除相同的委托。</summary>
        internal static void Initialize()
        {
            NebulaModAPI.OnMultiplayerGameStarted += Reset;
            NebulaModAPI.OnMultiplayerGameEnded += Reset;
            NebulaModAPI.OnPlanetLoadRequest += RequestPlanet;
            NebulaModAPI.OnPlanetLoadFinished += OrbitalRingPlanetDataProcessor.PlanetLoaded;
        }

        /// <summary>移除会话订阅并丢弃已结束房间的临时数据。</summary>
        internal static void Shutdown()
        {
            NebulaModAPI.OnMultiplayerGameStarted -= Reset;
            NebulaModAPI.OnMultiplayerGameEnded -= Reset;
            NebulaModAPI.OnPlanetLoadRequest -= RequestPlanet;
            NebulaModAPI.OnPlanetLoadFinished -= OrbitalRingPlanetDataProcessor.PlanetLoaded;
            Reset();
        }

        /// <summary>清理网络缓存；不改变存档内的业务状态。</summary>
        internal static void Reset()
        {
            requestedGlobal = false;
            OrbitalRingPlanetDataProcessor.Reset();
            MultiplayerTransfers.Reset();
        }

        /// <summary>游戏就绪后仅请求一次全局数据，避免在星系尚未初始化时导入。</summary>
        internal static void Update()
        {
            if (!IsClient || requestedGlobal || !GameMain.isRunning || GameMain.galaxy == null) return;
            requestedGlobal = true;
            Send(new OrbitalRingGlobalRequest());
        }

        /// <summary>请求原版工厂数据之外的星球附加状态。</summary>
        /// <param name="planetId">即将载入的星球。</param>
        internal static void RequestPlanet(int planetId)
        {
            if (!IsClient) return;
            OrbitalRingPlanetDataProcessor.Loading(planetId);
            Send(new OrbitalRingPlanetLoadRequest(planetId));
        }

        /// <summary>发送一次本地操作；在接收端应用数据时不调用此方法。</summary>
        /// <typeparam name="T">Nebula 已注册的数据包类型。</typeparam>
        /// <param name="packet">操作或状态包。</param>
        internal static void Send<T>(T packet) where T : class, new()
        {
            if (NebulaModAPI.IsMultiplayerActive) NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
        }

        /// <summary>主机将旧式增量包转发给其余玩家，发起端已应用的变化不回送。</summary>
        /// <typeparam name="T">Nebula 已注册的数据包类型。</typeparam>
        /// <param name="packet">已在主机应用的操作。</param>
        /// <param name="connection">原发起连接。</param>
        internal static void Relay<T>(T packet, INebulaConnection connection) where T : class, new()
        {
            if (IsHost) NebulaModAPI.MultiplayerSession.Network.SendPacketExclude(packet, connection);
        }

        /// <summary>序列化不随原版星球工厂传输的全局附加状态。</summary>
        /// <returns>同版本客户端可导入的状态包。</returns>
        internal static OrbitalRingGlobalData ExportGlobal()
        {
            using (IWriterProvider provider = NebulaModAPI.GetBinaryWriter()) {
                BinaryWriter w = provider.BinaryWriter;
                w.Write(ProjectOrbitalRing.VersionNumber());
                Unlock_Save_Load.Export(w);
                StarGate.Export(w);
                EnergyCalculate.Export(w);
                GlobalPowerSupplyPatches.Export(w);
                MoonPatch.Export(w);
                return new OrbitalRingGlobalData { Data = provider.CloseAndGetBytes() };
            }
        }

        /// <summary>在新玩家载入后或非推导状态改变时发送全局快照。</summary>
        internal static void BroadcastGlobal()
        {
            if (IsHost && GameMain.isRunning) Send(ExportGlobal());
        }

        /// <summary>应用主机的全局状态，不改变任何玩家背包。</summary>
        /// <param name="data">主机导出的字节。</param>
        internal static void ImportGlobal(byte[] data)
        {
            using (IReaderProvider provider = NebulaModAPI.GetBinaryReader(data)) {
                BinaryReader r = provider.BinaryReader;
                int previousVersion = ProjectOrbitalRing.importVersion;
                try {
                    ProjectOrbitalRing.importVersion = r.ReadInt32();
                    Unlock_Save_Load.Import(r);
                    StarGate.Import(r);
                    EnergyCalculate.Import(r);
                    GlobalPowerSupplyPatches.Import(r);
                    MoonPatch.Import(r);
                    Unlock_Save_Load.ApplyImportedLuminosity();
                } finally {
                    ProjectOrbitalRing.importVersion = previousVersion;
                }
            }
        }

        /// <summary>获取指定环的公共仓库；不存在的星球或环不创建业务数据。</summary>
        /// <param name="planetId">目标星球。</param>
        /// <param name="ringId">环索引。</param>
        /// <returns>已存在的仓库，或 null。</returns>
        internal static OrbitalRingStorage GetStorage(int planetId, int ringId, bool requireComplete = false)
        {
            PlanetOrbitalRingData planet = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planet == null || ringId < 0 || ringId >= planet.Rings.Count) return null;

            EquatorRing ring = planet.Rings[ringId];
            return !requireComplete || ring.IsOneFull() ? ring.orbitalRingStorage : null;
        }
    }

    /// <summary>客户端在游戏就绪后请求全局附加状态。</summary>
    public sealed class OrbitalRingGlobalRequest { }

    /// <summary>主机发送的全局附加状态。</summary>
    public sealed class OrbitalRingGlobalData
    {
        /// <summary>按模组当前存档版本编排的全局数据。</summary>
        public byte[] Data { get; set; }
    }

    /// <summary>只由主机响应首次全局状态请求。</summary>
    [RegisterPacketProcessor]
    public sealed class OrbitalRingGlobalRequestProcessor : BasePacketProcessor<OrbitalRingGlobalRequest>
    {
        /// <inheritdoc />
        public override void ProcessPacket(OrbitalRingGlobalRequest packet, INebulaConnection connection)
        {
            if (IsHost && GameMain.galaxy != null) connection.SendPacket(MultiplayerSync.ExportGlobal());
        }
    }

    /// <summary>客户端应用全局状态；错误包含数据来源，便于定位联机版本不匹配。</summary>
    [RegisterPacketProcessor]
    public sealed class OrbitalRingGlobalDataProcessor : BasePacketProcessor<OrbitalRingGlobalData>
    {
        /// <inheritdoc />
        public override void ProcessPacket(OrbitalRingGlobalData packet, INebulaConnection connection)
        {
            if (IsHost || packet?.Data == null || GameMain.galaxy == null) return;
            try {
                MultiplayerSync.ImportGlobal(packet.Data);
            } catch (Exception e) when (e is IOException || e is ArgumentException) {
                ProjectOrbitalRing.LogError($"Cannot import OrbitalRing global multiplayer data: {e.Message}");
            }
        }
    }
}
