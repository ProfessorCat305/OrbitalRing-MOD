using CommonAPI;
using GalacticScale;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using NGPT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using System.Reflection;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using ProjectOrbitalRing.Utils;
using System.Numerics;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalSpaceStation
    {
        // <planetId, HashSet<(ringIndex, position, isCore)>>
        private static Dictionary<int, HashSet<(int, int, bool)>> PreBuild = new Dictionary<int, HashSet<(int, int, bool)>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.AddPrebuildData))]
        public static void AddPrebuildDataPatch(PlanetFactory __instance, PrebuildData prebuild)
        {
            // 当protoId是轨道设施时，记录预建造的pos，下面CheckBuildConditionsPrePatch时检查pos有无重合，有重叠的就不让建造
            bool flag1 = IsBuildingItemIdisOrbitalStation(prebuild.protoId, false);
            bool flag2 = IsBuildingItemIdisOrbitalCore(prebuild.protoId);
            if (flag1 || flag2) {
                int planetId = __instance.planet.id;
                OrbitalStationManager.Instance.AddPlanetId(planetId);
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
                int ringIndex = isBuildingPosYCorrect(prebuild.pos);
                if (planetOrbitalRingData != null) {
                    int position = -1;
                    if (ringIndex != -1) {
                        bool flag = planetOrbitalRingData.Rings[ringIndex].IsAllFull() && prebuild.protoId != ProtoID.I太空电梯;
                        position = IsBuildingPosXZCorrect(prebuild.pos.x, prebuild.pos.z, flag);
                    }
                    if (!PreBuild.ContainsKey(planetId)) {
                        PreBuild[planetId] = new HashSet<(int, int, bool)>();
                    }
                    if (flag2) {
                        PreBuild[planetId].Add((ringIndex, position, true));
                    } else {
                        PreBuild[planetId].Add((ringIndex, position, false));
                    }
                }
                if (prebuild.protoId == ProtoID.I星环对撞机) { // 星环对撞机，从点击建造，建设无人机还在飞开始，就不许再建 
                    if (planetOrbitalRingData != null) {
                        planetOrbitalRingData.Rings[ringIndex].isParticleCollider = true;
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemovePrebuildWithComponents))]
        public static void RemovePrebuildWithComponentsPatch(PlanetFactory __instance, int id)
        {
            if (id != 0 && __instance.prebuildPool[id].id != 0) {
                bool flag1 = IsBuildingItemIdisOrbitalStation(__instance.prebuildPool[id].protoId, false);
                bool flag2 = IsBuildingItemIdisOrbitalCore(__instance.prebuildPool[id].protoId);
                if (flag1 || flag2) {
                    PrebuildData data = __instance.prebuildPool[id];
                    int position = IsBuildingPosXZCorrect(data.pos.x, data.pos.z, true);
                    int ringIndex = isBuildingPosYCorrect(data.pos);
                    if (PreBuild.TryGetValue(__instance.planetId, out var values)) {
                        if (flag2) {
                            values.Remove((ringIndex, position, true));
                        } else {
                            values.Remove((ringIndex, position, false));
                        }
                    }
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), "DeterminePreviews")]
        [HarmonyPatch(typeof(BuildTool_Addon), "DeterminePreviews")]
        public static void DeterminePreviewsPatch(BuildTool_Click __instance)
        {
            int count = __instance.buildPreviews.Count;
            for (int i = 0; i < count; i++) {
                BuildPreview preview = __instance.buildPreviews[i];
                if (IsBuildingItemIdisOrbitalStation(preview.item.ID, true) || IsBuildingItemIdisOrbitalCore(preview.item.ID)) {
                    double vanillaY = preview.lpos.y;
                    var (pos, flag) = ShouldBeAdsorb(preview.lpos);
                    if (flag) {
                        preview.lpos = pos;
                        int planetId = __instance.planet.id;
                        OrbitalStationManager.Instance.AddPlanetId(planetId);
                        var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
                        int ringIndex = isBuildingPosYCorrect(preview.lpos);
                        if (planetOrbitalRingData != null) {
                            if (ringIndex != -1) {
                                bool flag1 = planetOrbitalRingData.Rings[ringIndex].IsAllFull() && preview.item.ID != ProtoID.I太空电梯;
                                preview.lpos = ShouldBeXZAdsorb(preview.lpos, flag1);
                            }
                        }
                        preview.lrot = Maths.SphericalRotation(preview.lpos, __instance.yaw);
                    }
                }
                if (IsBuildingItemIdisOrbitalStation(preview.item.ID, false) || IsBuildingItemIdisOrbitalCore(preview.item.ID)) {
                    // 计算原向量长度
                    float originalMagnitude = preview.lpos.magnitude;
                    if (originalMagnitude == 0 || originalMagnitude - __instance.planet.realRadius > 40) {
                        continue; // 避免除以零
                    }
                    // 获取单位向量（原方向）
                    Vector3 normalized = preview.lpos.normalized;
                    // 计算新长度并返回结果
                    preview.lpos = normalized * (originalMagnitude + (IsBuildingItemIdisOrbitalCore(preview.item.ID) ? 72 : 40));
                }

                if (preview.item.ID == 6511) { // 超空间中继器核心
                    float originalMagnitude = preview.lpos.magnitude;
                    if (originalMagnitude == 0 || originalMagnitude - __instance.planet.realRadius > 30) {
                        continue; // 避免除以零
                    }
                    // 获取单位向量（原方向）
                    Vector3 normalized = preview.lpos.normalized;
                    // 计算新长度并返回结果
                    preview.lpos = normalized * (originalMagnitude + 33);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        public static bool BlueprintPaste_CheckBuildConditionsPrePatch(BuildTool_BlueprintPaste __instance, ref bool __result)
        {
            int count = __instance.bpPool.Length;
            BuildPreview buildPreview;
            int previewItem;
            for (int i = 0; i < count; i++) {
                buildPreview = __instance.bpPool[i];
                if (buildPreview == null || buildPreview.item == null) continue;
                previewItem = buildPreview.item.ID;
                bool flag1 = IsBuildingItemIdisOrbitalStation(previewItem, true);
                bool flag2 = IsBuildingItemIdisOrbitalCore(previewItem);
                bool flag = false;
                if (flag1 || flag2) {
                    
                    int planetId = __instance.planet.id;
                    OrbitalStationManager.Instance.AddPlanetId(planetId);
                    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
                    int ringIndex = isBuildingPosYCorrect(buildPreview.lpos);
                    if (planetOrbitalRingData != null) {
                        int position = -1;
                        if (ringIndex != -1) {
                            bool flag3 = planetOrbitalRingData.Rings[ringIndex].IsAllFull() && buildPreview.item.ID != ProtoID.I太空电梯;
                            position = IsBuildingPosXZCorrect(buildPreview.lpos.x, buildPreview.lpos.z, flag3);
                        }
                        if (position == -1 || ringIndex == -1) {
                            buildPreview.condition = (EBuildCondition)99;
                            __instance.AddErrorMessage((EBuildCondition)99, buildPreview);
                            continue;
                            //__result = false;
                            //return false;
                        }

                        OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id);
                        if (previewItem != ProtoID.I太空电梯) { // 太空电梯不检查重合
                            flag = false;

                            if (flag2) {
                                if (PreBuild.TryGetValue(__instance.planet.id, out var values) && values.Contains((ringIndex, position, true))) {
                                    flag = true;
                                }
                                if (planetOrbitalRingData != null) {
                                    var pair = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                                    if (pair.OrbitalCorePoolId != -1) {
                                        flag = true;
                                    }
                                }
                            } else {
                                if (PreBuild.TryGetValue(__instance.planet.id, out var values) && values.Contains((ringIndex, position, false))) {
                                    flag = true;
                                }
                                if (planetOrbitalRingData != null) {
                                    var pair = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                                    if (pair.OrbitalStationPoolId != -1) {
                                        flag = true;
                                    }
                                }
                            }
                            if (flag) {
                                buildPreview.condition = EBuildCondition.Collide;
                                __instance.AddErrorMessage(EBuildCondition.Collide, buildPreview);
                                continue;
                                //__result = false;
                                //return false;
                            }
                        }
                        if (IsBuildingItemIdisOrbitalCore(previewItem)) {
                            flag = false;
                            if (planetOrbitalRingData == null) {
                                buildPreview.condition = (EBuildCondition)98;
                                __instance.AddErrorMessage((EBuildCondition)98, buildPreview);
                                continue;
                            }
                            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                            if (previewItem == ProtoID.I轨道反物质堆核心) {
                                if (result.stationType != StationType.PowerGenBase) {
                                    flag = true;
                                }
                            } else if (previewItem == ProtoID.I重型电浆炮 || previewItem == ProtoID.I重型电磁弹射器) {
                                if (result.stationType != StationType.TurretBase) {
                                    flag = true;
                                }
                            } else if (previewItem == ProtoID.I星环护盾组件) {
                                if (result.stationType != StationType.GlobalSupportBase) {
                                    flag = true;
                                }
                            }
                            if (flag) {
                                buildPreview.condition = (EBuildCondition)98;
                                __instance.AddErrorMessage((EBuildCondition)98, buildPreview);
                                continue;
                            }
                        }

                        if (buildPreview.item.ID == ProtoID.I星环对撞机) { // 星环对撞机，检查该圈有无
                            if (planetOrbitalRingData.Rings[ringIndex].isParticleCollider) {
                                buildPreview.condition = (EBuildCondition)97;
                                __instance.AddErrorMessage((EBuildCondition)97, buildPreview);
                                continue;
                                //__result = false;
                                //return false;
                            }
                        }
                    }
                    // 继续原可否建造判断流程
                    return true;
                }
                flag = false;
                if (previewItem == ProtoID.I轨道连接组件) {
                    int beltYPosition = isBeltBuildingPosYCorrect(buildPreview.lpos, 0);
                    if (beltYPosition == -1) {
                        flag = true;
                    }
                }
                if (previewItem == ProtoID.I星环电网组件) {
                    int beltYPosition = isBeltBuildingPosYCorrect(buildPreview.lpos, 2);
                    if (beltYPosition == -1) {
                        flag = true;
                    }
                }
                if (previewItem == ProtoID.I粒子加速轨道) {
                    int beltYPosition = isBeltBuildingPosYCorrect(buildPreview.lpos, 1);
                    if (beltYPosition == -1) {
                        flag = true;
                    }
                }
                if (flag) {
                    buildPreview.condition = (EBuildCondition)99;
                    __instance.AddErrorMessage((EBuildCondition)99, buildPreview);
                    continue;
                }
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_Addon), "CheckBuildConditions")]
        public static bool CheckBuildConditionsPrePatch(BuildTool_Click __instance, ref bool __result)
        {
            int count = __instance.buildPreviews.Count;
            if (count == 0) {
                __result = false;
                return false;
            }
            BuildPreview buildPreview;
            int previewItem;
            for (int i = 0; i < __instance.buildPreviews.Count; i++) {
                buildPreview = __instance.buildPreviews[i];
                previewItem = buildPreview.item.ID;
                bool flag1 = IsBuildingItemIdisOrbitalStation(previewItem, true);
                bool flag2 = IsBuildingItemIdisOrbitalCore(previewItem);
                if (flag1 || flag2) {
                    if (count > 1) {
                        buildPreview.condition = EBuildCondition.Failure;
                        __result = false;
                        return false;
                    }
                    int planetId = __instance.planet.id;
                    OrbitalStationManager.Instance.AddPlanetId(planetId);
                    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
                    int ringIndex = isBuildingPosYCorrect(buildPreview.lpos);
                    if (planetOrbitalRingData != null) {
                        int position = -1;
                        if (ringIndex != -1) {
                            bool flag3 = planetOrbitalRingData.Rings[ringIndex].IsAllFull() && previewItem != ProtoID.I太空电梯;
                            position = IsBuildingPosXZCorrect(buildPreview.lpos.x, buildPreview.lpos.z, flag3);
                        }
                        if (position == -1 || ringIndex == -1) {
                            buildPreview.condition = (EBuildCondition)99;
                            __result = false;
                            return false;
                        }

                        if (previewItem != ProtoID.I太空电梯) { // 太空电梯不检查重合
                            bool flag = false;

                            if (flag2) {
                                if (PreBuild.TryGetValue(__instance.planet.id, out var values) && values.Contains((ringIndex, position, true))) {
                                    flag = true;
                                }
                                if (planetOrbitalRingData != null) {
                                    var pair = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                                    if (pair.OrbitalCorePoolId != -1) {
                                        flag = true;
                                    }
                                }
                            } else {
                                if (PreBuild.TryGetValue(__instance.planet.id, out var values) && values.Contains((ringIndex, position, false))) {
                                    flag = true;
                                }
                                if (planetOrbitalRingData != null) {
                                    var pair = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                                    if (pair.OrbitalStationPoolId != -1) {
                                        flag = true;
                                    }
                                }
                            }
                            if (flag) {
                                buildPreview.condition = EBuildCondition.Collide;
                                __result = false;
                                return false;
                            }
                        }

                        if (IsBuildingItemIdisOrbitalCore(previewItem)) {
                            if (planetOrbitalRingData == null) {
                                buildPreview.condition = (EBuildCondition)98;
                                __result = false;
                                return false;
                            }
                            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                            if (previewItem == ProtoID.I轨道反物质堆核心) {
                                if (result.stationType != StationType.PowerGenBase) {
                                    buildPreview.condition = (EBuildCondition)98;
                                    __result = false;
                                    return false;
                                }
                            } else if (previewItem == ProtoID.I重型电浆炮 || previewItem == ProtoID.I重型电磁弹射器) {
                                if (result.stationType != StationType.TurretBase) {
                                    buildPreview.condition = (EBuildCondition)98;
                                    __result = false;
                                    return false;
                                }
                            } else if (previewItem == ProtoID.I星环护盾组件) {
                                if (result.stationType != StationType.GlobalSupportBase) {
                                    buildPreview.condition = (EBuildCondition)98;
                                    __result = false;
                                    return false;
                                }
                            }
                        }

                        if (buildPreview.item.ID == ProtoID.I星环对撞机) { // 星环对撞机，检查该圈有无
                            if (planetOrbitalRingData.Rings[ringIndex].isParticleCollider) {
                                buildPreview.condition = (EBuildCondition)97;
                                __result = false;
                                return false;
                            }
                        }
                    }
                    // 继续原可否建造判断流程
                    return true;
                }
            }
            return true;
        }

        private static bool IsBuildingItemIdisOrbitalCore(int itemId)
        {
            switch (itemId) {
                case ProtoID.I轨道反物质堆核心: // 轨道反物质堆核心
                //case 6511: // 超空间中继器核心
                case ProtoID.I重型电浆炮: // 重型电浆炮
                case ProtoID.I重型电磁弹射器: // 重型电磁弹射器
                case ProtoID.I星环护盾组件: // 重型电磁弹射器
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBuildingItemIdisOrbitalStation(int itemId, bool isContainElevator)
        {
            switch (itemId) {
                case ProtoID.I太空物流港: // 太空物流港
                case ProtoID.I深空物流港: // 深空物流港
                case ProtoID.I太空船坞: // 太空船坞
                case ProtoID.I轨道熔炼站: // 轨道熔炼站
                case ProtoID.I轨道反物质堆基座: // 轨道反物质堆基座
                case ProtoID.I天枢座: // 天枢座
                case ProtoID.I星环对撞机: // 星环对撞机总控站
                case ProtoID.I轨道观测站: // 轨道观测站
                case ProtoID.I组装厂交互塔:
                    return true;
                case ProtoID.I太空电梯: // 太空电梯
                    return isContainElevator;
                default:
                    return false;
            }
        }

        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        [HarmonyPostfix]
        public static void BlueprintPaste_CheckBuildConditionsPostPatch(BuildTool_BlueprintPaste __instance, ref bool __result)
        {
            int count = __instance.bpPool.Length;
            if (count == 0) {
                return;
            }
            for (int i = 0; i < count; i++) {
                BuildPreview buildPreview = __instance.bpPool[i];
                if (buildPreview.item == null) continue;
                if (IsBuildingItemIdisOrbitalStation(buildPreview.item.ID, false) || IsBuildingItemIdisOrbitalCore(buildPreview.item.ID)) {
                    if (buildPreview.condition == EBuildCondition.OutOfReach || buildPreview.condition == EBuildCondition.OutOfVerticalConstructionHeight ||
                        buildPreview.condition == EBuildCondition.NeedGround) {
                        buildPreview.condition = EBuildCondition.Ok;
                        __instance.actionBuild.model.cursorState = 0;
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_Addon), "CheckBuildConditions")]
        //[HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        [HarmonyPostfix]
        public static void CheckBuildConditionsPostPatch(BuildTool_Click __instance, ref bool __result)
        {
            int count = __instance.buildPreviews.Count;
            if (count == 0) {
                return;
            }
            for (int i = 0; i < __instance.buildPreviews.Count; i++) {
                BuildPreview buildPreview = __instance.buildPreviews[i];
                if (IsBuildingItemIdisOrbitalStation(buildPreview.item.ID, false) || IsBuildingItemIdisOrbitalCore(buildPreview.item.ID)) {
                    if (buildPreview.condition == EBuildCondition.OutOfReach || buildPreview.condition == EBuildCondition.OutOfVerticalConstructionHeight ||
                        buildPreview.condition == EBuildCondition.NeedGround) {
                        buildPreview.condition = EBuildCondition.Ok;
                        __result = true;
                        __instance.actionBuild.model.cursorState = 0;
                        string text = ((__instance.dotCount > 1) ? ("    (" + __instance.dotCount + ")") : "");
                        __instance.actionBuild.model.cursorText = "点击鼠标建造".Translate() + text;
                    }
                }
            }
        }

        public static void BuildElevator(PlanetTransport __instance, int thisEntityId, ref StationComponent thisStation)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, false);
            int ringIndex = isBuildingPosYCorrect(thisPos);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加电梯
            planetOrbitalRingData.Rings[ringIndex].AddElevator(position, thisStation.id);
            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
            if (result.stationType == StationType.Station) {
                thisStation.storage = __instance.stationPool[result.OrbitalStationPoolId].storage;
                planetOrbitalRingData.Rings[ringIndex].SetElevatorStorage(position, thisStation.storage);
                return;
            } else {
                planetOrbitalRingData.Rings[ringIndex].SetElevatorStorage(position, thisStation.storage);
                return;
            }
        }

        public static void BuildOrbitalStation(PlanetTransport __instance, int thisEntityId, ref StationComponent thisStation)
        {
            Vector3 thisPos = __instance.factory.entityPool[thisEntityId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true);
            int ringIndex = isBuildingPosYCorrect(thisPos);
            OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            // 在赤道上/下圈？号位置添加轨道设施
            planetOrbitalRingData.Rings[ringIndex].AddOrbitalStation(position, thisStation.id, StationType.Station);
            var result = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
            if (result.elevatorPoolId == -1) {
                return;
            } else {
                thisStation.storage = __instance.stationPool[result.elevatorPoolId].storage;
            }
        }

        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
        [HarmonyPostfix]
        public static void NewStationComponentPostPatch(ref PlanetTransport __instance, ref StationComponent __result, int _entityId, PrefabDesc _desc)
        {
            int itemid = __instance.factory.entityPool[_entityId].protoId;
            if (itemid == ProtoID.I太空电梯) { // 太空电梯
                BuildElevator(__instance, _entityId, ref __result);
            }
            if (itemid == ProtoID.I太空物流港 || itemid == ProtoID.I深空物流港 || itemid == ProtoID.I组装厂交互塔) { // 太空物流港、深空物流港
                BuildOrbitalStation(__instance, _entityId, ref __result);
            }
        }

        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RematchLocalPairs))]
        [HarmonyPrefix]
        public static bool RematchLocalPairsPatch(ref StationComponent __instance)
        {
            // 屏蔽轨道物流港的行星内运输航线计算，让运输机只送太空电梯
            if (!__instance.isVeinCollector && __instance.workDroneDatas.Length <= 0) {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        [HarmonyPostfix]
        public static void ImportPatch(PlanetFactory __instance)
        {
            int planetId = __instance.planet.id;
            PlanetOrbitalRingData data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (data == null) return;
            for (int i = 0; i < data.Rings.Count; i++) {
                EquatorRing ring = data.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    var pair = ring.GetPair(j);
                    StationStore[] storage = null;
                    if (pair.elevatorPoolId != -1) {
                        if (__instance.transport.stationPool == null) continue;
                        if (__instance.transport.stationPool.Length == 0) continue;
                        if (__instance.transport.stationPool.Length <= pair.elevatorPoolId) continue;
                        if (__instance.transport.stationPool[pair.elevatorPoolId] == null) continue;
                        storage = __instance.transport.stationPool[pair.elevatorPoolId].storage;
                        data.Rings[i].SetElevatorStorage(j, storage);
                    }
                    if (pair.OrbitalStationPoolId != -1 && pair.stationType == StationType.Station && storage != null) {
                        __instance.transport.stationPool[pair.OrbitalStationPoolId].storage = storage;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.DismantleFinally))]
        [HarmonyPrefix]
        public static void DismantleFinallyPatch(PlanetFactory __instance, int objId, ref int protoId)
        {
            bool flag1 = IsBuildingItemIdisOrbitalStation(protoId, true);
            bool flag2 = IsBuildingItemIdisOrbitalCore(protoId);
            if (!(flag1 || flag2)) {
                return;
            }

            if (objId <= 0) {
                if (protoId == ProtoID.I星环对撞机) // 星环对撞机，拆除，放开再建
                {
                    PrebuildData preBuildData = __instance.prebuildPool[-objId];
                    int preBuildringIndex = isBuildingPosYCorrect(preBuildData.pos);
                    var data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                    data.Rings[preBuildringIndex].isParticleCollider = false;
                }
                return;
            }
            Vector3 thisPos = __instance.entityPool[objId].pos;
            int position = IsBuildingPosXZCorrect(thisPos.x, thisPos.z, true);
            int ringIndex = isBuildingPosYCorrect(thisPos);
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            if (protoId == ProtoID.I太空电梯) {
                if (planetOrbitalRingData == null)
                    return;
                planetOrbitalRingData.Rings[ringIndex].RemoveElevator(position);
            } else if (flag1) {
                if (planetOrbitalRingData == null)
                    return;
                planetOrbitalRingData.Rings[ringIndex].RemoveOrbitalStation(position);
            } else if (flag2) {
                if (planetOrbitalRingData == null)
                    return;
                planetOrbitalRingData.Rings[ringIndex].RemoveOrbitalCore(position);
            }
            if (protoId == ProtoID.I星环对撞机) // 星环对撞机，拆除，放开再建
            {
                if (planetOrbitalRingData == null)
                    return;
                planetOrbitalRingData.Rings[ringIndex].isParticleCollider = false;
            }
        }

        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DismantleAction))]
        [HarmonyPrefix]
        public static bool DismantleActionPatch(BuildTool_Dismantle __instance)
        {
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            if (planetOrbitalRingData == null) return true;

            if (((VFInput._buildConfirm.onDown && __instance.cursorType == 0) || (VFInput._buildConfirm.pressing && __instance.cursorType == 1)) && __instance.buildPreviews.Count > 0) {
                foreach (BuildPreview buildPreview in __instance.buildPreviews) {
                    if (buildPreview.condition == EBuildCondition.Ok) {
                        if (BuildTool_Dismantle.showDemolishContainerQuery) {
                            if (buildPreview.objId > 0 && buildPreview.item.ID == ProtoID.I轨道反物质堆基座 || buildPreview.item.ID == ProtoID.I轨道观测站 ||
                                buildPreview.item.ID == ProtoID.I天枢座) {
                                int ringIndex = isBuildingPosYCorrect(buildPreview.lpos);
                                int position = IsBuildingPosXZCorrect(buildPreview.lpos.x, buildPreview.lpos.z, true);
                                var pair = planetOrbitalRingData.Rings[ringIndex].GetPair(position);
                                if (!OrbitalStationManager.StationTypeIsBase(pair.stationType)) {
                                    if (pair.OrbitalCorePoolId == -1)
                                        return true;
                                    if (pair.stationType == StationType.PowerGenCore) {
                                        int colId = __instance.factory.entityPool[__instance.factory.powerSystem.genPool[pair.OrbitalCorePoolId].entityId].colliderId;
                                        ColliderData colliderData = __instance.actionBuild.planetPhysics.GetColliderData(colId);

                                        __instance.actionBuild.DoDismantleObject(colliderData.objId);
                                    }

                                    if (pair.stationType == StationType.TurretCore) {
                                        int colId = __instance.factory.entityPool[__instance.factory.defenseSystem.turrets.buffer[pair.OrbitalCorePoolId].entityId].colliderId;
                                        ColliderData colliderData = __instance.actionBuild.planetPhysics.GetColliderData(colId);
                                        __instance.actionBuild.DoDismantleObject(colliderData.objId);
                                    }

                                    if (pair.stationType == StationType.EjectorCore) {
                                        int colId = __instance.factory.entityPool[__instance.factory.factorySystem.ejectorPool[pair.OrbitalCorePoolId].entityId].colliderId;
                                        ColliderData colliderData = __instance.actionBuild.planetPhysics.GetColliderData(colId);
                                        __instance.actionBuild.DoDismantleObject(colliderData.objId);
                                    }

                                    if (pair.stationType == StationType.ATFeildCore) {
                                        int colId = __instance.factory.entityPool[__instance.factory.defenseSystem.fieldGenerators.buffer[pair.OrbitalCorePoolId].entityId].colliderId;
                                        ColliderData colliderData = __instance.actionBuild.planetPhysics.GetColliderData(colId);
                                        __instance.actionBuild.DoDismantleObject(colliderData.objId);
                                    }

                                    //__instance.actionBuild.DoDismantleObject(pair.OrbitalCorePoolId);
                                    //__instance.dismantleQueryBox = UIMessageBox.Show("拆除基座标题".Translate(), "拆除基座文字".Translate(), "确定".Translate(), 1, new UIMessageBox.Response(__instance.DismantleQueryCancel));
                                    //return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }



    }
}