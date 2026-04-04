using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine
{
    internal class EnergyCalculate
    {
        public static DysonSphere MathematicalRateEngineDysonSphere = null;
        private static int MathematicalRateEngineStarIndex = -1;
        public static long SecondLevelEnergy = 0;
        public static int[] SecondLevelLayer = new int[10];

        public static readonly double ThirdLevelRatio = 150000000.0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "BeforeGameTick")]
        public static void BeforeGameTickPostPatch(ref DysonSphere __instance)
        {
            if (!GameMain.history.TechUnlocked(1952)) {
                return; // 未解锁数学率引擎二阶
            }
            if (__instance.starData.type != EStarType.BlackHole) {
                return; // 仅修改黑洞恒星的戴森球
            }
            if (ProjectOrbitalRing.MoreMegaStructureCompatibility) {
                try {
                    // 使用反射动态获取类型
                    var mmType = Type.GetType("MoreMegaStructure.MoreMegaStructure, MoreMegaStructure");
                    var starMegaType = mmType?.GetField("StarMegaStructureType")?.GetValue(null) as int[];

                    if (starMegaType?[__instance.starData.index] != 0) {
                        return; // 如果是巨构类型，则不修改
                    }
                } catch (Exception ex) {
                }
            }
            //二阶及以上数学率引擎不计算游离帆的功率
            __instance.energyGenCurrentTick = (long)((double)(__instance.energyGenOriginalCurrentTick - __instance.energyGenCurrentTick_Swarm) * __instance.energyDFHivesDebuffCoef);
        }

        /// <summary>
        /// 用于修复非戴森球 黑屋窃取能量显示不正常的bug
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEPowerDesc), "UpdateUI")]
        public static void UIDEPowerDescUpdateUIPostPatch(ref UIDEPowerDesc __instance)
        {
            int StarIndex = __instance.dysonSphere.starData.index;
            if (!GameMain.history.TechUnlocked(1952)) {
                return; // 未解锁数学率引擎二阶
            }
            if (__instance.dysonSphere.starData.type != EStarType.BlackHole) {
                return; // 仅修改黑洞恒星的戴森球
            }
            if (ProjectOrbitalRing.MoreMegaStructureCompatibility) {
                try {
                    // 使用反射动态获取类型
                    var mmType = Type.GetType("MoreMegaStructure.MoreMegaStructure, MoreMegaStructure");
                    var starMegaType = mmType?.GetField("StarMegaStructureType")?.GetValue(null) as int[];

                    if (starMegaType?[__instance.dysonSphere.starData.index] != 0) {
                        return; // 如果是巨构类型，则不修改
                    }
                } catch (Exception ex) {
                }
            }
            var _this = __instance;
            long num5 = (_this.dysonSphere.energyGenOriginalCurrentTick - _this.dysonSphere.energyGenCurrentTick_Swarm - _this.dysonSphere.energyGenCurrentTick) * 60L;
            StringBuilderUtility.WriteKMG(_this.sb, 8, -num5, true);
            _this.valueText4.text = _this.sb.ToString();
        }


        public static void MathematicalRateEngineRemoveSails()
        {
            if (GameMain.galaxy == null) {
                return;
            }
            for (int i = 0; i < GameMain.galaxy.stars.Length; i++) {
                StarData star = GameMain.galaxy.stars[i];
                if (star.type != EStarType.BlackHole) continue;
                if (ProjectOrbitalRing.MoreMegaStructureCompatibility) {
                    try {
                        // 使用反射动态获取类型
                        var mmType = Type.GetType("MoreMegaStructure.MoreMegaStructure, MoreMegaStructure");
                        var starMegaType = mmType?.GetField("StarMegaStructureType")?.GetValue(null) as int[];
                        if (starMegaType?[star.index] != 0) {
                            return; // 如果是巨构类型，则不修改
                        }
                    } catch (Exception ex) {
                    }
                }
                for (int j = 0; j < GameMain.data.dysonSpheres.Length; j++) {
                    if (GameMain.data.dysonSpheres[j] == null) continue;
                    if (GameMain.data.dysonSpheres[j].starData.index == star.index) {
                        GameMain.data.dysonSpheres[j].swarm.RemoveSailsByOrbit(-1);
                        MathematicalRateEngineStarIndex = star.index;
                        if (MathematicalRateEngineDysonSphere == null) {
                            MathematicalRateEngineDysonSphere = GameMain.data.dysonSpheres[j];
                        }
                    }
                }

            }
        }

        public static void CalculateThirdLevelMathematicalRateEngine()
        {
            if (MathematicalRateEngineDysonSphere == null) {
                return;
            }
            SecondLevelEnergy = MathematicalRateEngineDysonSphere.energyGenCurrentTick - MathematicalRateEngineDysonSphere.energyReqCurrentTick;
            for (int i = 0; i < MathematicalRateEngineDysonSphere.layerCount; i++) {
                if (MathematicalRateEngineDysonSphere.layersSorted[i] == null) continue;
                SecondLevelLayer[i] = MathematicalRateEngineDysonSphere.layersSorted[i].id;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.OrderConstructCp))]
        public static bool OrderConstructCpPatch(DysonNode __instance)
        {
            for (int i = 0; i < SecondLevelLayer.Length; i++) {
                if (SecondLevelLayer[i] != 0 && SecondLevelLayer[i] == __instance.layerId) {
                    return false; // 禁止吸附
                }
            }
            return true;
        }

        internal static void Export(BinaryWriter w)
        {
            if (MathematicalRateEngineStarIndex == -1) {
                w.Write(-1);
                return;
            }
            w.Write(MathematicalRateEngineStarIndex);
            w.Write(SecondLevelEnergy);
            for (int i = 0; i < SecondLevelLayer.Length; i++) {
                w.Write(SecondLevelLayer[i]);
            }
        }

        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try {
                MathematicalRateEngineStarIndex = r.ReadInt32();
                if (MathematicalRateEngineStarIndex == -1) {
                    MathematicalRateEngineDysonSphere = null;
                    return;
                }
                SecondLevelEnergy = r.ReadInt64();
                for (int i = 0; i < SecondLevelLayer.Length; i++) {
                    SecondLevelLayer[i] = r.ReadInt32();
                }
            } catch (EndOfStreamException) {
                // ignored
            }
        }

        internal static void IntoOtherSave()
        {
            MathematicalRateEngineDysonSphere = null;
            SecondLevelEnergy = 0;
            SecondLevelLayer = new int[10];
        }
    }
}
