using System.IO;
using ProjectOrbitalRing.Utils;

namespace ProjectOrbitalRing.Patches.Logic.AssemblerModule
{
    public static partial class AssemblerModulePatches
    {
        /// <summary>导出一个星球的插件数据，供工厂载入后恢复。</summary>
        /// <param name="planetId">目标星球。</param>
        /// <param name="w">目标数据流。</param>
        internal static void ExportPlanetData(int planetId, BinaryWriter w)
        {
            FactorySystem factory = GameMain.galaxy?.PlanetById(planetId)?.factory?.factorySystem;
            if (factory == null || !AssemblerModuleData.TryGetValue(factory, out var modules)) {
                w.Write(0);
                return;
            }
            var snapshot = modules.ToArray();
            w.Write(snapshot.Length);
            foreach (var pair in snapshot) {
                w.Write(pair.Key);
                w.Write(pair.Value.ItemId);
                w.Write(pair.Value.ItemCount);
                w.Write(pair.Value.ItemInc);
                w.Write(pair.Value.NeedCount);
            }
        }

        /// <summary>替换指定工厂的插件状态，不触碰玩家物品和其他星球。</summary>
        /// <param name="planetId">目标星球。</param>
        /// <param name="r">来源数据流。</param>
        internal static void ImportPlanetData(int planetId, BinaryReader r)
        {
            FactorySystem factory = GameMain.galaxy?.PlanetById(planetId)?.factory?.factorySystem;
            int count = r.ReadInt32();
            if (count < 0 || factory == null || count > factory.assemblerCapacity) {
                throw new InvalidDataException("Invalid assembler module snapshot.");
            }
            AssemblerModuleData.TryRemove(factory, out _);
            for (int i = 0; i < count; i++) {
                int assemblerId = r.ReadInt32();
                var data = new AssemblerModuleData {
                    ItemId = r.ReadInt32(), ItemCount = r.ReadInt32(),
                    ItemInc = r.ReadInt32(), NeedCount = r.ReadInt32()
                };
                if (assemblerId > 0 && assemblerId < factory.assemblerCursor &&
                    factory.assemblerPool[assemblerId].id == assemblerId) {
                    ApplyNetworkData(factory, assemblerId, data);
                }
            }
        }

        /// <summary>应用已确认的插件状态；网络回放只修改建筑，不执行取物或返还。</summary>
        /// <param name="factory">插件所在工厂。</param>
        /// <param name="assemblerId">制造台组件编号。</param>
        /// <param name="data">插件的绝对状态。</param>
        internal static void ApplyNetworkData(FactorySystem factory, int assemblerId, AssemblerModuleData data)
        {
            GetAssemblerModuleData(factory, assemblerId);
            AssemblerModuleData[factory][assemblerId] = data;
        }

        /// <summary>在主机上广播一次插件变化，日常生产计算不产生同步包。</summary>
        /// <param name="factory">插件所在工厂。</param>
        /// <param name="assemblerId">制造台组件编号。</param>
        internal static void BroadcastModule(FactorySystem factory, int assemblerId)
        {
            if (!MultiplayerSync.IsHost) return;
            AssemblerModuleData data = GetAssemblerModuleData(factory, assemblerId);
            MultiplayerSync.Send(new SyncAssemblerModuleData {
                PlanetId = factory.planet.id, AssemblerId = assemblerId,
                EntityId = factory.assemblerPool[assemblerId].entityId,
                ItemId = data.ItemId, Count = data.ItemCount, Inc = data.ItemInc, Need = data.NeedCount
            });
        }
    }
}
