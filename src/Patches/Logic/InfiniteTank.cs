using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System.Collections.Generic;
using System.IO;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic
{
    internal class InfiniteTank
    {
        private static HashSet<(int, int)> InfiniteTankDate = new HashSet<(int, int)>();

        private static readonly int INFINITE_NUM= 5000;

        [HarmonyPatch(typeof(TankComponent), nameof(TankComponent.GameTick))]
        [HarmonyPrefix]
        public static bool TankComponent_GameTick_PrePatch(ref TankComponent __instance, PlanetFactory factory)
        {
            if (__instance.fluidInc < 0) {
                __instance.fluidInc = 0;
            }
            if (!__instance.isBottom) {
                return false;
            }
            if (factory.entityPool[__instance.entityId].protoId != ProtoID.I无尽水罐) {
                return true;
            }
            CargoTraffic cargoTraffic = factory.cargoTraffic;
            TankComponent[] tankPool = factory.factoryStorage.tankPool;
            int num = __instance.inputSwitch ? __instance.id : 0;
            if (__instance.belt0 > 0) {
                if (__instance.isOutput0 && __instance.outputSwitch) {
                    if (__instance.fluidId > 0 && __instance.fluidCount > 0) {
                        int num2 = (__instance.fluidInc == 0) ? 0 : (__instance.fluidInc / __instance.fluidCount);
                        if (cargoTraffic.TryInsertItemAtHead(__instance.belt0, __instance.fluidId, 1, (byte)num2)) {
                            if (!InfiniteTankDate.Contains((factory.planetId, __instance.id))) {
                                __instance.fluidCount--;
                                __instance.fluidInc -= num2;
                            }
                        }
                    }
                } else if (!__instance.isOutput0 && __instance.inputSwitch) {
                    byte b;
                    byte b2;
                    //if (!InfiniteTankDate.Contains((factory.planetId, __instance.id))) {
                    if (__instance.fluidId > 0 && __instance.fluidCount < __instance.fluidCapacity && cargoTraffic.TryPickItemAtRear(__instance.belt0, __instance.fluidId, null, out b, out b2) > 0) {
                        if (!InfiniteTankDate.Contains((factory.planetId, __instance.id))) {
                            __instance.fluidCount += (int)b;
                            __instance.fluidInc += (int)b2;
                            if (__instance.fluidCount > INFINITE_NUM) {
                                InfiniteTankDate.Add((factory.planetId, __instance.id));
                                __instance.fluidInc = (int)b2 / __instance.fluidCount;
                                __instance.fluidCount = 1;
                            }
                        }
                    }
                    if (__instance.fluidId == 0) {
                        int num3 = cargoTraffic.TryPickItemAtRear(__instance.belt0, 0, ItemProto.fluids, out b, out b2);
                        if (num3 > 0) {
                            __instance.fluidId = num3;
                            __instance.fluidCount += (int)b;
                            __instance.fluidInc += (int)b2;
                        }
                    }
                    if (__instance.fluidCount >= __instance.fluidCapacity && cargoTraffic.GetItemIdAtRear(__instance.belt0) == __instance.fluidId) {
                        __instance.TryPickItem(tankPool, cargoTraffic, ref num, __instance.belt0);
                    }
                    //}
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(FactoryStorage), nameof(FactoryStorage.RemoveTankComponent))]
        [HarmonyPrefix]
        public static void FactoryStorage_RemoveTankComponent_PrePatch(ref FactoryStorage __instance, int id)
        {
            if (InfiniteTankDate.Contains((__instance.factory.planetId, id))) {
                InfiniteTankDate.Remove((__instance.factory.planetId, id));
            }
        }

        [HarmonyPatch(typeof(UITankWindow), nameof(UITankWindow._OnUpdate))]
        [HarmonyPostfix]
        public static void UITankWindow_UITankWindow_PostPatch(ref UITankWindow __instance)
        {
            if (__instance.tankId == 0 || __instance.factory == null) {
                __instance._Close();
                return;
            }
            ref TankComponent tankComponent = ref __instance.storage.tankPool[__instance.tankId];
            if (tankComponent.id != __instance.tankId) {
                __instance._Close();
                return;
            }
            if (__instance.factory.entityPool[tankComponent.entityId].protoId == ProtoID.I无尽水罐) {
                if (tankComponent.fluidCount > INFINITE_NUM) {
                    if (!InfiniteTankDate.Contains((__instance.factory.planetId, tankComponent.id))) {
                        InfiniteTankDate.Add((__instance.factory.planetId, tankComponent.id));
                        tankComponent.fluidInc = tankComponent.fluidInc / tankComponent.fluidCount;
                        tankComponent.fluidCount = 1;
                    }
                } else if (tankComponent.fluidCount == 0) {
                    if (InfiniteTankDate.Contains((__instance.factory.planetId, tankComponent.id))) {
                        InfiniteTankDate.Remove((__instance.factory.planetId, tankComponent.id));
                    }
                }
            }
            if (InfiniteTankDate.Contains((__instance.factory.planetId, tankComponent.id))) {
                __instance.countText.text = "∞".Translate();
            }
        }

        internal static void Export(BinaryWriter w)
        {
            w.Write(InfiniteTankDate.Count);
            foreach (var item in InfiniteTankDate) {
                w.Write(item.Item1); // 逐个写入元素
                w.Write(item.Item2);
            }
        }

        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try {
                int count = 0;
                count = r.ReadInt32(); // 先读取元素数量
                for (int i = 0; i < count; i++) {
                    InfiniteTankDate.Add((r.ReadInt32(), r.ReadInt32())); // 逐个读取元素
                }
            } catch (EndOfStreamException) {
                // ignored
            }
        }

        internal static void IntoOtherSave()
        {
            InfiniteTankDate.Clear();
        }
    }
}
