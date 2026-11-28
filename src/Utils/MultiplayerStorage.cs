using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;

namespace ProjectOrbitalRing.Utils
{
    /// <summary>主机确认的制造台插件状态。接收端不扣物品、不返还物品。</summary>
    public sealed class SyncAssemblerModuleData
    {
        /// <summary>目标星球。</summary>
        public int PlanetId { get; set; }
        /// <summary>制造台组件编号。</summary>
        public int AssemblerId { get; set; }
        /// <summary>制造台实体编号。</summary>
        public int EntityId { get; set; }
        /// <summary>插件物品编号。</summary>
        public int ItemId { get; set; }
        /// <summary>已装插件数量。</summary>
        public int Count { get; set; }
        /// <summary>插件携带的总增产点数。</summary>
        public int Inc { get; set; }
        /// <summary>插件槽需求数量。</summary>
        public int Need { get; set; }
    }

    /// <summary>应用插件变化；未加载星球由下一次工厂快照恢复。</summary>
    [RegisterPacketProcessor]
    public sealed class SyncAssemblerModuleDataProcessor : BasePacketProcessor<SyncAssemblerModuleData>
    {
        /// <inheritdoc />
        public override void ProcessPacket(SyncAssemblerModuleData packet, INebulaConnection connection)
        {
            if (IsHost || packet == null || packet.Count < 0 || packet.Count > 1 || packet.Inc < 0 ||
                packet.Inc > packet.Count * 10 || packet.Need < 0 || packet.Need > 1 ||
                !MultiplayerTransfers.TryGetAssembler(packet.PlanetId, packet.AssemblerId, out FactorySystem factory) ||
                factory.assemblerPool[packet.AssemblerId].entityId != packet.EntityId) return;
            AssemblerModulePatches.ApplyNetworkData(factory, packet.AssemblerId, new AssemblerModuleData {
                ItemId = packet.ItemId, ItemCount = packet.Count, ItemInc = packet.Inc, NeedCount = packet.Need
            });
        }
    }

    /// <summary>公共仓库的共享开关或物品上限变化；开关传目标值，重复应用不会反向切换。</summary>
    public sealed class SyncOrbitalStorageSetting
    {
        /// <summary>0 表示共享开关，1 表示物品上限。</summary>
        public int Kind { get; set; }
        /// <summary>目标星球。</summary>
        public int PlanetId { get; set; }
        /// <summary>目标环索引。</summary>
        public int RingId { get; set; }
        /// <summary>共享开关对应的物流站。</summary>
        public int StationId { get; set; }
        /// <summary>物流站储物槽索引。</summary>
        public int Slot { get; set; }
        /// <summary>上限对应的物品。</summary>
        public int ItemId { get; set; }
        /// <summary>共享值为0或1；上限为非负整数，-1表示恢复默认。</summary>
        public int Value { get; set; }

        /// <summary>应用单项设置，不触碰库存数量。</summary>
        /// <param name="validateAgainstStock">主机校验环的完成状态和当前库存。</param>
        /// <returns>是否成功定位并更新设置。</returns>
        internal bool Apply(bool validateAgainstStock)
        {
            OrbitalRingStorage storage = MultiplayerSync.GetStorage(PlanetId, RingId, requireComplete: validateAgainstStock && Kind == 1);
            if (storage == null) return false;
            if (Kind == 1) {
                if (LDB.items.Select(ItemId) == null || Value < -1 || Value > OrbitalRingStorageCalculate.StorageHardLimit) return false;
                int limit = Value == -1 ? OrbitalRingStorageCalculate.GetDefaultItemStorageLimit(ItemId) : Value;
                return OrbitalRingStorageCalculate.TrySetItemStorageLimit(storage, ItemId, limit, validateAgainstStock);
            }
            if (Kind != 0 || Slot < 0 || Slot >= 5 || (Value != 0 && Value != 1) ||
                !storage.storageShare.TryGetValue(StationId, out StationStoreIsShares shares)) return false;
            shares[Slot] = Value == 1;
            EquatorRing ring = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(PlanetId).Rings[RingId];
            for (int i = 0; i < ring.Capacity; i++) {
                var pair = ring.GetPair(i);
                int other = pair.elevatorPoolId == StationId ? pair.OrbitalStationPoolId :
                    pair.OrbitalStationPoolId == StationId ? pair.elevatorPoolId : -1;
                if (other > 0 && storage.storageShare.TryGetValue(other, out StationStoreIsShares linked)) linked[Slot] = Value == 1;
            }
            return true;
        }
    }

    /// <summary>主机验证上限后向所有客户端广播结果，接收端无需再次发送。</summary>
    [RegisterPacketProcessor]
    public sealed class SyncOrbitalStorageSettingProcessor : BasePacketProcessor<SyncOrbitalStorageSetting>
    {
        /// <inheritdoc />
        public override void ProcessPacket(SyncOrbitalStorageSetting packet, INebulaConnection connection)
        {
            if (packet == null) return;
            if (!IsHost) {
                OrbitalRingPlanetDataProcessor.ApplyStorageUpdate(packet.PlanetId, () => packet.Apply(validateAgainstStock: false));
                return;
            }

            if (!packet.Apply(validateAgainstStock: true)) return;
            if (packet.Kind == 1) MultiplayerSync.Send(packet);
            else MultiplayerSync.Relay(packet, connection);
        }
    }

    /// <summary>一次手动转移后的物品余额；自动搬运不逐帧广播。</summary>
    public sealed class SyncOrbitalStorageItem
    {
        /// <summary>目标星球。</summary>
        public int PlanetId { get; set; }
        /// <summary>目标环。</summary>
        public int RingId { get; set; }
        /// <summary>库存中的物品编号。</summary>
        public int ItemId { get; set; }
        /// <summary>该物品的当前数量。</summary>
        public int Count { get; set; }
        /// <summary>该物品携带的总增产点数。</summary>
        public int Inc { get; set; }
    }

    /// <summary>客户端应用一次主机库存余额，不产生玩家物品。</summary>
    [RegisterPacketProcessor]
    public sealed class SyncOrbitalStorageItemProcessor : BasePacketProcessor<SyncOrbitalStorageItem>
    {
        /// <inheritdoc />
        public override void ProcessPacket(SyncOrbitalStorageItem packet, INebulaConnection connection)
        {
            if (IsHost || packet == null || packet.Count < 0 || packet.Inc < 0 || LDB.items.Select(packet.ItemId) == null) return;
            OrbitalRingPlanetDataProcessor.ApplyStorageUpdate(packet.PlanetId, () => {
                OrbitalRingStorage storage = MultiplayerSync.GetStorage(packet.PlanetId, packet.RingId);
                if (storage == null) return;
                lock (storage.storageItem) {
                    if (packet.Count == 0) storage.storageItem.Remove(packet.ItemId);
                    else storage.storageItem[packet.ItemId] = new[] { packet.Count, packet.Inc };
                }
            });
        }
    }

    /// <summary>玩家修改生态温室配方时，把联动操作传给其余工厂副本。</summary>
    public sealed class SyncFarmRecipes
    {
        /// <summary>目标星球。</summary>
        public int PlanetId { get; set; }
        /// <summary>操作源制造台。</summary>
        public int AssemblerId { get; set; }
        /// <summary>目标配方，0 表示清空。</summary>
        public int RecipeId { get; set; }
    }

    /// <summary>重放温室联动配方；原料返还只在操作发起端发生。</summary>
    [RegisterPacketProcessor]
    public sealed class SyncFarmRecipesProcessor : BasePacketProcessor<SyncFarmRecipes>
    {
        /// <inheritdoc />
        public override void ProcessPacket(SyncFarmRecipes packet, INebulaConnection connection)
        {
            if (packet == null || packet.RecipeId < 0 ||
                (packet.RecipeId != 0 && LDB.recipes.Select(packet.RecipeId) == null) ||
                !MultiplayerTransfers.TryGetAssembler(packet.PlanetId, packet.AssemblerId, out FactorySystem system)) return;
            PlanetFactory factory = system.factory;
            int entityId = system.assemblerPool[packet.AssemblerId].entityId;
            if (factory.entityPool[entityId].protoId != ProtoID.I生态温室) return;
            system.assemblerPool[packet.AssemblerId].SetRecipe(packet.RecipeId, factory.entitySignPool);
            Patches.Logic.Farm.SyncFarmAssemblerRecipes.SyncRecipes(factory, packet.AssemblerId, false, false);
            MultiplayerSync.Relay(packet, connection);
        }
    }
}
