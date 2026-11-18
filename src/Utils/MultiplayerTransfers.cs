using System;
using System.Collections.Generic;
using NebulaAPI;
using NebulaAPI.Interfaces;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;

namespace ProjectOrbitalRing.Utils
{
    /// <summary>仅协调玩家与插件、共享空间之间的实物转移；自动生产和搬运仍由各端计算。</summary>
    internal static class MultiplayerTransfers
    {
        internal const int InstallModule = 0;
        internal const int RemoveModule = 1;
        internal const int DepositStorage = 2;
        private static long nextRequest;
        private static readonly Dictionary<long, OrbitalRingItemRequest> Pending = new Dictionary<long, OrbitalRingItemRequest>();

        /// <summary>丢弃结束会话的请求。未收到回执时不擅自退款，避免已在主机入库的物品被复制。</summary>
        internal static void Reset() => Pending.Clear();

        /// <summary>处理联机界面的插件点击；返回 true 时调用方不再执行本地取放。</summary>
        /// <param name="factory">目标工厂。</param>
        /// <param name="assemblerId">制造台编号。</param>
        /// <param name="player">实际操作者。</param>
        /// <returns>是否已由联机流程接管。</returns>
        internal static bool ClickModule(FactorySystem factory, int assemblerId, Player player)
        {
            if (!NebulaModAPI.IsMultiplayerActive) return false;
            if (player == null) return true;
            if (player.inhandItemId == 0 || player.inhandItemCount <= 0) {
                Remove(factory, assemblerId, player, true);
            } else {
                Install(factory, assemblerId, player, false, null);
            }
            return true;
        }

        /// <summary>预留本端插件实物，由主机决定能否安装；拒绝部分按回执返还。</summary>
        /// <param name="factory">目标工厂。</param>
        /// <param name="assemblerId">制造台编号。</param>
        /// <param name="player">实际操作者。</param>
        /// <param name="fromPackage">是否沿用原版从背包取物的方式。</param>
        /// <param name="bundle">原版快捷填充携带的物品包。</param>
        internal static void Install(FactorySystem factory, int assemblerId, Player player, bool fromPackage, ItemBundle bundle)
        {
            if (MultiplayerSync.IsRemoteFactoryRequest || !TryGetAssembler(factory?.planet.id ?? 0, assemblerId, out _)) return;
            ref AssemblerComponent assembler = ref factory.assemblerPool[assemblerId];
            AssemblerModuleData data = AssemblerModulePatches.GetAssemblerModuleData(factory, assemblerId);
            int required = AssemblerModulePatches.GetModuleId(assembler.recipeId);
            int itemId = player.inhandItemId;
            if (itemId != required && !(required == 7617 && itemId == 7618)) {
                UIRealtimeTip.Popup("不相符的物品".TranslateFromJson());
                return;
            }
            if (data.ItemCount > 0) {
                UIRealtimeTip.Popup("栏位已满".TranslateFromJson());
                return;
            }
            int count = 1;
            player.TakeItemFromPlayer(ref itemId, ref count, out int inc, fromPackage, bundle);
            if (count <= 0) return;
            Submit(new OrbitalRingItemRequest {
                Kind = InstallModule, PlanetId = factory.planet.id, TargetId = assemblerId,
                EntityId = assembler.entityId, ItemId = itemId, Count = count, Inc = inc, ToHand = true
            }, player);
        }

        /// <summary>卸下插件；远端建筑操作的返还由主机送给包作者，不使用主机玩家背包。</summary>
        /// <param name="factory">目标工厂。</param>
        /// <param name="assemblerId">制造台编号。</param>
        /// <param name="player">本地操作时的玩家。</param>
        /// <param name="toHand">是否优先返还至手持槽。</param>
        internal static void Remove(FactorySystem factory, int assemblerId, Player player, bool toHand)
        {
            if (!TryGetAssembler(factory?.planet.id ?? 0, assemblerId, out _)) return;
            var request = new OrbitalRingItemRequest {
                Kind = RemoveModule, PlanetId = factory.planet.id, TargetId = assemblerId,
                EntityId = factory.assemblerPool[assemblerId].entityId, ToHand = toHand
            };
            if (MultiplayerSync.IsRemoteFactoryRequest) {
                if (!MultiplayerSync.IsHost) return;
                int author = NebulaModAPI.MultiplayerSession.Factories.PacketAuthor;
                OrbitalRingItemResult result = Execute(request);
                NebulaModAPI.MultiplayerSession.Network.SendToMatching(result, p => p.Id == author);
            } else {
                Submit(request, player);
            }
        }

        /// <summary>预留手中的乾坤物品，主机按共享空间的当前容量决定实际接收数量。</summary>
        /// <param name="planetId">目标星球。</param>
        /// <param name="ringId">目标环。</param>
        /// <param name="player">实际操作者。</param>
        internal static void Deposit(int planetId, int ringId, Player player)
        {
            int itemId = player.inhandItemId;
            int count = player.inhandItemCount;
            player.TakeItemFromPlayer(ref itemId, ref count, out int inc, false, null);
            if (count <= 0) return;
            Submit(new OrbitalRingItemRequest {
                Kind = DepositStorage, PlanetId = planetId, TargetId = ringId,
                ItemId = itemId, Count = count, Inc = inc, ToHand = true
            }, player);
        }

        /// <summary>发送有回执的操作；主机本地操作复用相同执行路径。</summary>
        /// <param name="request">已预留实物的请求，或无需预留的取出请求。</param>
        /// <param name="player">本地操作者。</param>
        private static void Submit(OrbitalRingItemRequest request, Player player)
        {
            request.RequestId = ++nextRequest;
            if (MultiplayerSync.IsClient) {
                Pending.Add(request.RequestId, request);
                MultiplayerSync.Send(request);
            } else {
                Deliver(Execute(request), player);
            }
        }

        /// <summary>主机执行一次实物操作，返回未接收的预留物或取出的插件。</summary>
        /// <param name="request">玩家提交的操作。</param>
        /// <returns>只交给操作者的物品回执。</returns>
        internal static OrbitalRingItemResult Execute(OrbitalRingItemRequest request)
        {
            var result = new OrbitalRingItemResult {
                RequestId = request.RequestId, ItemId = request.ItemId,
                Count = request.Count, Inc = request.Inc, ToHand = request.ToHand
            };
            if (request.Count < 0 || request.Inc < 0 || (long)request.Inc > (long)request.Count * 10) {
                result.Count = 0;
                result.Inc = 0;
                return result;
            }
            if (request.Kind == DepositStorage) {
                DepositIntoStorage(request, result);
                return result;
            }
            if (!TryGetAssembler(request.PlanetId, request.TargetId, out FactorySystem factory) ||
                factory.assemblerPool[request.TargetId].entityId != request.EntityId) return result;
            AssemblerModuleData data = AssemblerModulePatches.GetAssemblerModuleData(factory, request.TargetId);
            if (request.Kind == InstallModule) {
                ref AssemblerComponent assembler = ref factory.assemblerPool[request.TargetId];
                int required = AssemblerModulePatches.GetModuleId(assembler.recipeId);
                if (request.Count != 1 || data.ItemCount != 0 ||
                    !Patches.UI.UIAssemblerWindowPatch.ShouldModuleButtonActive(assembler.recipeType, assembler.recipeId, assembler.speed) ||
                    (request.ItemId != required && !(required == 7617 && request.ItemId == 7618))) return result;
                AssemblerModulePatches.ApplyNetworkData(factory, request.TargetId, new AssemblerModuleData {
                    ItemId = request.ItemId, ItemCount = 1, ItemInc = request.Inc, NeedCount = 1
                });
                result.Count = 0;
                result.Inc = 0;
            } else if (request.Kind == RemoveModule) {
                result.ItemId = data.ItemId;
                result.Count = data.ItemCount;
                result.Inc = data.ItemInc;
                AssemblerModulePatches.ApplyNetworkData(factory, request.TargetId, new AssemblerModuleData { NeedCount = data.NeedCount });
            } else {
                return result;
            }
            AssemblerModulePatches.BroadcastModule(factory, request.TargetId);
            return result;
        }

        /// <summary>按既有规则存入乾坤原矿；共享库存的自动搬运不经过此请求。</summary>
        /// <param name="request">预留实物的存入请求。</param>
        /// <param name="result">写入未接收的剩余物品。</param>
        private static void DepositIntoStorage(OrbitalRingItemRequest request, OrbitalRingItemResult result)
        {
            OrbitalRingStorage storage = MultiplayerSync.GetStorage(request.PlanetId, request.TargetId);
            int oreId = TheMountainMovingMappings.GetOreId(request.ItemId);
            if (storage == null || request.Count == 0 || oreId == request.ItemId || LDB.items.Select(oreId) == null) return;
            SyncOrbitalStorageItem balance;
            lock (storage.storageItem) {
                storage.storageItem.TryGetValue(oreId, out int[] current);
                int currentCount = current?[0] ?? 0;
                int accepted = (int)Math.Min(request.Count, 2147480000L - currentCount);
                if (accepted <= 0) return;
                if ((long)currentCount + accepted > OrbitalRingStorageCalculate.StorageHardLimit) {
                    foreach (var pair in storage.storageItem) {
                        if (pair.Key != oreId && pair.Value[0] > OrbitalRingStorageCalculate.StorageHardLimit &&
                            TheMountainMovingMappings.GetQianKunItemId(pair.Key) != pair.Key) return;
                    }
                }
                int remaining = request.Count;
                int remainingInc = request.Inc;
                int acceptedInc = PosTool.split_inc(ref remaining, ref remainingInc, accepted);
                if (current == null) storage.storageItem[oreId] = new[] { accepted, acceptedInc };
                else {
                    current[0] += accepted;
                    current[1] += acceptedInc;
                }
                result.Count = remaining;
                result.Inc = remainingInc;
                int[] stored = storage.storageItem[oreId];
                balance = new SyncOrbitalStorageItem {
                    PlanetId = request.PlanetId, RingId = request.TargetId, ItemId = oreId,
                    Count = stored[0], Inc = stored[1]
                };
            }
            MultiplayerSync.Send(balance);
        }

        /// <summary>核对网络请求引用的制造台身份，禁止用池中的空槽执行操作。</summary>
        /// <param name="planetId">星球编号。</param>
        /// <param name="assemblerId">制造台编号。</param>
        /// <param name="factory">成功时返回所属工厂。</param>
        /// <returns>是否仍是有效的制造台。</returns>
        internal static bool TryGetAssembler(int planetId, int assemblerId, out FactorySystem factory)
        {
            factory = GameMain.galaxy?.PlanetById(planetId)?.factory?.factorySystem;
            return factory != null && assemblerId > 0 && assemblerId < factory.assemblerCursor &&
                factory.assemblerPool[assemblerId].id == assemblerId;
        }

        /// <summary>只消费仍在等待的本端回执，重复回执不再次发物品。</summary>
        /// <param name="result">主机确认的返还物品。</param>
        internal static void Receive(OrbitalRingItemResult result)
        {
            if (result.RequestId != 0 && !Pending.Remove(result.RequestId)) return;
            Deliver(result, GameMain.mainPlayer);
        }

        /// <summary>把回执实物交给本地操作者，手持槽不兼容时沿用背包返还。</summary>
        /// <param name="result">主机确认的实物。</param>
        /// <param name="player">本地操作者。</param>
        private static void Deliver(OrbitalRingItemResult result, Player player)
        {
            if (player == null || result.ItemId <= 0 || result.Count <= 0) return;
            if (result.ToHand && player.inhandItemId == 0) {
                player.SetHandItems(result.ItemId, result.Count, result.Inc);
            } else {
                player.TryAddItemToPackage(result.ItemId, result.Count, result.Inc, true, 0);
            }
        }
    }

    /// <summary>插件装卸或乾坤物品存入请求；身份使用星球和组件，不传玩家背包对象。</summary>
    public sealed class OrbitalRingItemRequest
    {
        /// <summary>操作者在当前进程中分配的回执编号。</summary>
        public long RequestId { get; set; }
        /// <summary>0 安装插件，1 卸下插件，2 存入乾坤物品。</summary>
        public int Kind { get; set; }
        /// <summary>目标星球。</summary>
        public int PlanetId { get; set; }
        /// <summary>制造台编号，或存入请求的环索引。</summary>
        public int TargetId { get; set; }
        /// <summary>制造台当前实体身份，防止拆除后的组件编号被复用。</summary>
        public int EntityId { get; set; }
        /// <summary>已预留的实物编号。</summary>
        public int ItemId { get; set; }
        /// <summary>已预留的实物数量。</summary>
        public int Count { get; set; }
        /// <summary>已预留实物携带的总增产点数。</summary>
        public int Inc { get; set; }
        /// <summary>返回物品时是否优先使用空手持槽。</summary>
        public bool ToHand { get; set; }
    }

    /// <summary>只发给操作者的实际返还；状态广播本身不产生玩家物品。</summary>
    public sealed class OrbitalRingItemResult
    {
        /// <summary>对应请求；0 表示原版建筑拆除触发的直接返还。</summary>
        public long RequestId { get; set; }
        /// <summary>返还实物编号。</summary>
        public int ItemId { get; set; }
        /// <summary>返还数量，0 表示无需返还。</summary>
        public int Count { get; set; }
        /// <summary>返还实物携带的总增产点数。</summary>
        public int Inc { get; set; }
        /// <summary>是否优先使用空手持槽。</summary>
        public bool ToHand { get; set; }
    }

    /// <summary>主机处理实物请求，并仅向该连接返还实物。</summary>
    [RegisterPacketProcessor]
    public sealed class OrbitalRingItemRequestProcessor : BasePacketProcessor<OrbitalRingItemRequest>
    {
        /// <inheritdoc />
        public override void ProcessPacket(OrbitalRingItemRequest packet, INebulaConnection connection)
        {
            if (IsHost && packet != null && packet.RequestId > 0) connection.SendPacket(MultiplayerTransfers.Execute(packet));
        }
    }

    /// <summary>客户端消费自己的实物回执。</summary>
    [RegisterPacketProcessor]
    public sealed class OrbitalRingItemResultProcessor : BasePacketProcessor<OrbitalRingItemResult>
    {
        /// <inheritdoc />
        public override void ProcessPacket(OrbitalRingItemResult packet, INebulaConnection connection)
        {
            if (!IsHost && packet != null) MultiplayerTransfers.Receive(packet);
        }
    }
}
