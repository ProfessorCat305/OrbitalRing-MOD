using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using static UIPlayerDeliveryPanel;
using CommonAPI;
using ProjectOrbitalRing.Utils;
using MoreMegaStructure;
using System.IO;
using System.Runtime.Remoting.Messaging;
using UnityEngine.Playables;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;

namespace ProjectOrbitalRing.Patches.Logic
{
    internal class StarGate
    {
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.GameTick))]
        //[HarmonyPostfix]
        //public static void PlanetFactory_GameTick_Patch(PlanetFactory __instance)
        //{
        //    for (int i = 0; i < __instance.entityPool.Length; i++)
        //    {
        //        if (__instance.entityPool[i].protoId == 6511)
        //        {
        //            __instance.entityAnimPool[i].state = 1U;
        //            __instance.entityAnimPool[i].Step2(1U, 0.016666668f, 25000f, 1.0f);
        //            //__instance.entityAnimPool[i].power = 25000f;
        //        }
        //    }
        //}

        private static bool CheckBuildingPos(Vector3 hasBuildPos, Vector3 wantBuildPos, float threshold = 0.9999f)
        {
            // 排除零向量（长度为0的向量无方向）
            if (hasBuildPos.sqrMagnitude == 0 || wantBuildPos.sqrMagnitude == 0)
            {
                Debug.LogWarning("零向量无方向定义");
                return false;
            }

            // 归一化向量（获取单位向量）
            Vector3 normalizedA = hasBuildPos.normalized;
            Vector3 normalizedB = wantBuildPos.normalized;

            // 计算点积（即夹角余弦值）
            float dotProduct = Vector3.Dot(normalizedA, normalizedB);

            // 判断是否超过阈值（允许一定误差）
            return dotProduct >= threshold;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        public static bool CheckBuildConditionsPrePatch(BuildTool_Click __instance, ref bool __result)
        {
            if (__instance.planet.type != EPlanetType.Gas)
            {
                return true;
            }
            int count = __instance.buildPreviews.Count;
            if (count == 0)
            {
                __result = false;
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                BuildPreview buildPreview = __instance.buildPreviews[i];
                if (buildPreview.item.ID == 6511)
                {
                    for (int j = 0; j < __instance.factory.prebuildPool.Length; j++)
                    {
                        if (__instance.factory.prebuildPool[j].protoId != 0 && __instance.factory.prebuildPool[j].protoId != 6281)
                        {
                            if (CheckBuildingPos(__instance.factory.prebuildPool[j].pos, buildPreview.lpos))
                            {
                                buildPreview.condition = EBuildCondition.Collide;
                                __result = false;
                                return false;
                            }
                        }
                    }
                    if (count > 1)
                    {
                        buildPreview.condition = EBuildCondition.Failure;
                        __result = false;
                        return false;
                    }

                    var entityPool = __instance.planet.factory.entityPool;
                    bool flag = false;
                    for (int y = 0; y < entityPool.Length; y++)
                    {
                        if (entityPool[y].protoId == 0)
                        {
                            continue;
                        }
                        if (CheckBuildingPos(entityPool[y].pos, buildPreview.lpos))
                        {
                            if (entityPool[y].protoId != 6281)
                            {
                                buildPreview.condition = EBuildCondition.Collide;
                                __result = false;
                                return false;
                            } else
                            {
                                flag = true;
                            }
                        }
                        
                    }
                    if (!flag)
                    {
                        buildPreview.condition = (EBuildCondition)98;
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        [HarmonyPostfix]
        public static void CheckBuildConditionsPostPatch(BuildTool_Click __instance, ref bool __result)
        {
            if (__instance.planet.type != EPlanetType.Gas)
            {
                for (int i = 0; i < __instance.buildPreviews.Count; i++)
                {
                    BuildPreview buildPreview = __instance.buildPreviews[i];
                    if (buildPreview.item.ID == ProtoID.I超空间中继器基座)
                    {
                        buildPreview.condition = EBuildCondition.Failure;
                        __result = false;
                        return;
                    }
                }
            }
            int count = __instance.buildPreviews.Count;
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < __instance.buildPreviews.Count; i++)
            {
                BuildPreview buildPreview = __instance.buildPreviews[i];
                if (buildPreview.item.ID == 6511)
                {
                    if (buildPreview.condition == EBuildCondition.OutOfReach || buildPreview.condition == EBuildCondition.OutOfVerticalConstructionHeight ||
                        buildPreview.condition == EBuildCondition.NeedGround)
                    {
                        buildPreview.condition = EBuildCondition.Ok;
                        __result = true;
                        __instance.actionBuild.model.cursorState = 0;
                        string text = ((__instance.dotCount > 1) ? ("    (" + __instance.dotCount + ")") : "");
                        __instance.actionBuild.model.cursorText = "点击鼠标建造".Translate() + text;
                    }
                }
                if (buildPreview.item.ID == ProtoID.I超空间中继器基座)
                {
                    if (buildPreview.lpos.y != 0)
                    {
                        buildPreview.condition = EBuildCondition.BuildInEquator;
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DismantleAction))]
        [HarmonyPrefix]
        public static bool DismantleActionPatch(BuildTool_Dismantle __instance)
        {
            if (((VFInput._buildConfirm.onDown && __instance.cursorType == 0) || (VFInput._buildConfirm.pressing && __instance.cursorType == 1)) && __instance.buildPreviews.Count > 0)
            {
                foreach (BuildPreview buildPreview in __instance.buildPreviews)
                {
                    if (buildPreview.condition == EBuildCondition.Ok)
                    {
                        if (BuildTool_Dismantle.showDemolishContainerQuery)
                        {
                            if (buildPreview.objId > 0 && buildPreview.item.ID == 6281)
                            {
                                var entityPool = __instance.planet.factory.entityPool;

                                for (int y = 0; y < entityPool.Length; y++)
                                {
                                    if (entityPool[y].protoId == 0)
                                    {
                                        continue;
                                    }
                                    if (CheckBuildingPos(entityPool[y].pos, buildPreview.lpos) && buildPreview.objId != y)
                                    {
                                        __instance.dismantleQueryBox = UIMessageBox.Show("拆除基座标题".Translate(), "拆除基座文字".Translate(), "确定".Translate(), 1, new UIMessageBox.Response(__instance.DismantleQueryCancel));
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static List<int> starGateList = new List<int>();
        //StationComponent  DetermineDispatch
        public static void RecordStarGate(int itemId, int plantId)
        {
            if (itemId == 6511)
            {
                int starIndex = plantId / 100;
                starGateList.Add(starIndex);
            }
        }

        public static void DismantleStarGate(int itemId, int plantId)
        {
            if (itemId == 6511)
            {
                int starIndex = plantId / 100;
                starGateList.Remove(starIndex);
            }
        }

        public static bool StarGateExit(int plantId)
        {
            int starIndex = plantId / 100;
            return starGateList.Contains(starIndex);
        }


        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        [HarmonyPrefix]
        public static void DetermineDispatchPrefixPatch(ref StationComponent __instance)
        {
            // 有中继器航线可以免除翘曲器跨星系物流，为了绕过翘曲器小于两个就跳过的逻辑，先加20个翘曲器，函数结束时再回收
            __instance.warperCount += 20;
        }

        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        [HarmonyPostfix]
        public static void DetermineDispatchPostfixPatch(ref StationComponent __instance)
        {
            __instance.warperCount -= 20;
        }

        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DispatchSupplyShip))]
        [HarmonyPrefix]
        public static bool DispatchSupplyShipPrefixPatch(ref StationComponent __instance, bool takeWarper, StationComponent other, bool __result)
        {
            // 如果本身不需要翘曲器，说明是星系内物流，直接返回不拦截
            if (!takeWarper)
            {
                return true;
            }
            if (StarGateExit(__instance.planetId) && StarGateExit(other.planetId))
            {
                int num2 = __instance.QueryIdleShip(__instance.nextShipIndex);
                if (num2 >= 0)
                {
                    // 如果是星际物流，且两端都有中继器，则增加两个翘曲器以供消耗，达成表面不消耗翘曲器的逻辑，不拦截，处理原本发船逻辑
                    __instance.warperCount += 2;
                    return true;
                }
            }

            if (__instance.energyMax != 12000000000)
            {
                // 没有中继器，又不是深空物流港，不允许翘曲，直接拦截，返回false
                __result = false;
                return false;
            }
            if (__instance.warperCount <= 21)
            {
                // 没有中继器，是深空物流港，但翘曲器小于21，说明原本翘曲器只有1或者0，不允许翘曲，直接拦截，返回false
                __result = false;
                return false;
            }
            // 没有中继器，是深空物流港，且翘曲器大于1，允许翘曲，不拦截，处理原本发船逻辑
            __result = false;
            return true;
        }



        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DispatchDemandShip))]
        [HarmonyPrefix]
        public static bool DispatchDemandShipPrefixPatch(ref StationComponent __instance, bool takeWarper, StationComponent other, bool __result)
        {
            if (!takeWarper)
            {
                return true;
            }
            if (StarGateExit(__instance.planetId) && StarGateExit(other.planetId))
            {
                int num = __instance.QueryIdleShip(__instance.nextShipIndex);
                if (num >= 0)
                {
                    __instance.warperCount += 2;
                    return true;
                }
            }

            if (__instance.energyMax != 12000000000)
            {
                __result = false;
                return false;
            }
            if (__instance.warperCount < 21)
            {
                __result = false;
                return false;
            }
            __result = false;
            return true;
        }



        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineFramingDispatchTime))]
        [HarmonyPrefix]
        public static bool DetermineFramingDispatchTimePrefixPatch(ref bool __result)
        {
            __result = true;
            return false;
        }


        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StationComponent_DetermineDispatch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Add), new CodeMatch(OpCodes.Ldloc_S));
            object V_29 = matcher.Advance(2).Operand; // 变量索引

            matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldc_I4_0),
                 new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld));
            object V_34 = matcher.Operand; // 变量索引

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, V_29),
                new CodeInstruction(OpCodes.Ldloca_S, V_34),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StarGate), nameof(StarGateEnergyReduction),
                new Type[] {
                    typeof(StationComponent),
                    typeof(StationComponent),
                    typeof(long).MakeByRefType(),
                }
            )));

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_0), new CodeMatch(OpCodes.Call));
            object V_42 = matcher.Advance(2).Operand; // 变量索引
            object V_38 = matcher.Advance(1).Operand; // 变量索引

            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, V_38),
                new CodeInstruction(OpCodes.Ldloca_S, V_42),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StarGate), nameof(StarGateEnergyReduction),
                new Type[] {
                    typeof(StationComponent),
                    typeof(StationComponent),
                    typeof(long).MakeByRefType()
                }
            )));

            return matcher.InstructionEnumeration();
        }
        public static void StarGateEnergyReduction(StationComponent thisStationComponent, StationComponent otherStationComponent, ref long energy)
        {
            if (ProjectOrbitalRing.MoreMegaStructureCompatibility)
            {
                int thisStarIndex = thisStationComponent.planetId / 100;
                try
                {
                    var mmType = Type.GetType("MoreMegaStructure.WarpArray, MoreMegaStructure");
                    var starIsInWhichWarpArray = mmType?.GetField("starIsInWhichWarpArray")?.GetValue(null) as int[];
                    var tripEnergyCostRatioByStarIndex = mmType?.GetField("tripEnergyCostRatioByStarIndex")?.GetValue(null) as double[];

                    if (starIsInWhichWarpArray[thisStarIndex] >= 0)
                    {
                        if (tripEnergyCostRatioByStarIndex[thisStarIndex] > 0.2)
                        {
                            if (StarGateExit(thisStationComponent.planetId) && StarGateExit(otherStationComponent.planetId))
                            {
                                energy = (long)((energy / tripEnergyCostRatioByStarIndex[thisStarIndex]) * 0.2); // 中继器航线能量消耗降低为原来的20%
                            }
                        }
                        return;
                    }
                }
                catch (Exception ex) { }
            }
            if (StarGateExit(thisStationComponent.planetId) && StarGateExit(otherStationComponent.planetId))
            {
                energy = (long)(energy * 0.2); // 中继器航线能量消耗降低为原来的20%
            }
        }



        internal static void Export(BinaryWriter w)
        {
            w.Write(starGateList.Count);
            foreach (var item in starGateList)
            {
                w.Write(item); // 逐个写入元素
            }
        }

        internal static void Import(BinaryReader r)
        {
            IntoOtherSave();
            try
            {
                int count = 0;
                count = r.ReadInt32(); // 先读取元素数量
                for (int i = 1; i < count; i++)
                {
                    starGateList.Add(r.ReadInt32()); // 逐个读取元素
                }
            }
            catch (EndOfStreamException)
            {
                // ignored
            }
        }

        internal static void IntoOtherSave()
        {
            starGateList.Clear();
        }
    }
}
