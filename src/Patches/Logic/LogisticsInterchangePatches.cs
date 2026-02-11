using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ProjectOrbitalRing.Utils;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using static System.Reflection.Emit.OpCodes;
using System.IO;

namespace ProjectOrbitalRing.Patches.Logic
{
    public static class LogisticsInterchangePatches
    {
        private static void DelStationPose(int modelIndex)
        {
            PrefabDesc megaPumper = LDB.models.Select(modelIndex).prefabDesc;
            Pose[] newPortPoses = new Pose[] { };
            megaPumper.portPoses = newPortPoses;
        }

        private static void DelSlotPose(int modelIndex)
        {
            PrefabDesc megaPumper = LDB.models.Select(modelIndex).prefabDesc;
            Pose[] newSlotPoses = new Pose[] { };
            megaPumper.slotPoses = newSlotPoses;
        }

        internal static void StationPrefabDescPostAdd()
        {
            PrefabDesc megaPumper = LDB.models.Select(ProtoID.M物流立交).prefabDesc;
            Pose[] newPortPoses = { new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation),
                new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation)
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 17 * 1.3333333f - 0.01f;
            }

            //newPortPoses[12].position.z += 0.4f;
            //newPortPoses[13].position.z += 0.4f;
            //newPortPoses[14].position.z += 0.4f;
            //newPortPoses[15].position.x += -0.4f;
            //newPortPoses[16].position.x += -0.4f;
            //newPortPoses[17].position.x += -0.4f;
            //newPortPoses[18].position.z += -0.4f;
            //newPortPoses[19].position.z += -0.4f;
            //newPortPoses[20].position.z += -0.4f;
            //newPortPoses[21].position.x += 0.4f;
            //newPortPoses[22].position.x += 0.4f;
            //newPortPoses[23].position.x += 0.4f;

            megaPumper.portPoses = newPortPoses;

            DelStationPose(50); // 太空物流港

        }

        internal static void StationPrefabDescPostAdd810()
        {
            PrefabDesc megaPumper = LDB.models.Select(ProtoID.M太空电梯).prefabDesc;
            Pose[] newPortPoses = { new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation),
                new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation)
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 17 * 1.3333333f;
            }
//[Error: OrbitalRing] portPoses x 1.256 y - 0.01 z 2.7
//[Error: OrbitalRing] portPoses x 0 y - 0.01 z 2.7
//[Error: OrbitalRing] portPoses x -1.256 y - 0.01 z 2.7
//[Error: OrbitalRing] portPoses x -2.7 y - 0.01 z 1.256
//[Error: OrbitalRing] portPoses x -2.7 y - 0.01 z 0
//[Error: OrbitalRing] portPoses x -2.7 y - 0.01 z - 1.256
//[Error: OrbitalRing] portPoses x -1.256 y - 0.01 z - 2.7
//[Error: OrbitalRing] portPoses x 0 y - 0.01 z - 2.7
//[Error: OrbitalRing] portPoses x 1.256 y - 0.01 z - 2.7
//[Error: OrbitalRing] portPoses x 2.7 y - 0.01 z - 1.256
//[Error: OrbitalRing] portPoses x 2.7 y - 0.01 z 0
//[Error: OrbitalRing] portPoses x 2.7 y - 0.01 z 1.256

            megaPumper.portPoses = newPortPoses;

            megaPumper = LDB.models.Select(ProtoID.M轨道空投站).prefabDesc;
            newPortPoses = new Pose[]{ new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation),
                new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation)
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 17 * 1.3333333f;
            }
            megaPumper.portPoses = newPortPoses;

            DelStationPose(801); // 轨道熔炼站
            DelStationPose(803); // 太空船坞
            DelStationPose(804); // 轨道观测站
            DelStationPose(806); // 深空物流港
            DelSlotPose(807); // 轨道反物质堆核心
            DelStationPose(811); // 星环对撞机总控站
            DelStationPose(814); // 轨道反物质堆基座
            DelStationPose(820); // 星环电网枢纽
            DelStationPose(821); // 超空间中继器核心
            DelSlotPose(822); // 重型电磁弹射器

            // 化工厂和量子化工厂新增前方两个爪子口
            megaPumper = LDB.models.Select(64).prefabDesc;
            Pose[] newSlotPoses = new Pose[]{
                new Pose(megaPumper.slotPoses[0].position, megaPumper.slotPoses[0].rotation),
                new Pose(megaPumper.slotPoses[1].position, megaPumper.slotPoses[1].rotation),
                new Pose(megaPumper.slotPoses[2].position, megaPumper.slotPoses[2].rotation),
                new Pose(megaPumper.slotPoses[3].position, megaPumper.slotPoses[3].rotation),
                new Pose(megaPumper.slotPoses[4].position, megaPumper.slotPoses[4].rotation),
                new Pose(megaPumper.slotPoses[5].position, megaPumper.slotPoses[5].rotation),
                new Pose(megaPumper.slotPoses[6].position, megaPumper.slotPoses[6].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
            };
            newSlotPoses[8].position = new Vector3(3.8f, -0.02f, 0.1f);
            newSlotPoses[8].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            newSlotPoses[9].position = new Vector3(3.8f, -0.02f, 1.1f);
            newSlotPoses[9].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            megaPumper.slotPoses = newSlotPoses;

            megaPumper = LDB.models.Select(376).prefabDesc;
            newSlotPoses = new Pose[]{
                new Pose(megaPumper.slotPoses[0].position, megaPumper.slotPoses[0].rotation),
                new Pose(megaPumper.slotPoses[1].position, megaPumper.slotPoses[1].rotation),
                new Pose(megaPumper.slotPoses[2].position, megaPumper.slotPoses[2].rotation),
                new Pose(megaPumper.slotPoses[3].position, megaPumper.slotPoses[3].rotation),
                new Pose(megaPumper.slotPoses[4].position, megaPumper.slotPoses[4].rotation),
                new Pose(megaPumper.slotPoses[5].position, megaPumper.slotPoses[5].rotation),
                new Pose(megaPumper.slotPoses[6].position, megaPumper.slotPoses[6].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
            };
            newSlotPoses[8].position = new Vector3(3.8f, -0.02f, 0.1f);
            newSlotPoses[8].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            newSlotPoses[9].position = new Vector3(3.8f, -0.02f, 1.1f);
            newSlotPoses[9].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            megaPumper.slotPoses = newSlotPoses;

            megaPumper = LDB.models.Select(836).prefabDesc;
            newSlotPoses = new Pose[]{
                new Pose(megaPumper.slotPoses[0].position, megaPumper.slotPoses[0].rotation),
                new Pose(megaPumper.slotPoses[1].position, megaPumper.slotPoses[1].rotation),
                new Pose(megaPumper.slotPoses[2].position, megaPumper.slotPoses[2].rotation),
                new Pose(megaPumper.slotPoses[3].position, megaPumper.slotPoses[3].rotation),
                new Pose(megaPumper.slotPoses[4].position, megaPumper.slotPoses[4].rotation),
                new Pose(megaPumper.slotPoses[5].position, megaPumper.slotPoses[5].rotation),
                new Pose(megaPumper.slotPoses[6].position, megaPumper.slotPoses[6].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
                new Pose(megaPumper.slotPoses[7].position, megaPumper.slotPoses[7].rotation),
            };
            newSlotPoses[8].position = new Vector3(3.8f, -0.02f, 0.1f);
            newSlotPoses[8].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            newSlotPoses[9].position = new Vector3(3.8f, -0.02f, 1.1f);
            newSlotPoses[9].rotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
            megaPumper.slotPoses = newSlotPoses;

        }

        
        [HarmonyPatch(typeof(BuildingGizmo), nameof(BuildingGizmo.SetGizmoDesc))]
        [HarmonyPrefix]
        public static bool BuildingGizmo_SetGizmoDesc_Prefix(BuildingGizmo __instance, ref BuildGizmoDesc _desc)
        {
            __instance.desc = _desc;
            PrefabDesc prefabDesc = __instance.desc.desc;
            bool isBelt = prefabDesc.isBelt;
            bool isInserter = prefabDesc.isInserter;
            bool flag = prefabDesc.minerType == EMinerType.Vein;
            Material material = __instance.mr.sharedMaterial;
            if (!isBelt && !isInserter)
            {
                __instance.transform.localPosition = __instance.desc.wpos;
                __instance.transform.localRotation = __instance.desc.wrot;
                if (prefabDesc.hasObject && prefabDesc.lodCount > 0 && prefabDesc.lodMeshes[0] != null)
                {
                    __instance.mf.sharedMesh = prefabDesc.lodMeshes[0];
                    if (material == null)
                    {
                        material = UnityEngine.Object.Instantiate(Configs.builtin.previewGizmoMat);
                    }

                    Pose[] array = prefabDesc.slotPoses;
                    Pose[] array2 = prefabDesc.portPoses;
                    if (GameMain.localPlanet != null && prefabDesc.multiLevel && !prefabDesc.multiLevelAllowPortsOrSlots)
                    {
                        GameMain.localPlanet.factory.ReadObjectConn(__instance.desc.objId, 14, out var _, out var otherObjId, out var _);
                        if (otherObjId != 0)
                        {
                            array = new Pose[0];
                            array2 = new Pose[0];
                        }
                    }

                    for (int i = 0; i < array.Length; i++)
                    {
                        __instance.slotGizmos[i].localPosition = array[i].position - array[i].forward;
                        __instance.slotGizmos[i].localRotation = array[i].rotation;
                    }

                    if (__instance.portsGizmos.Length < array2.Length)
                    {
                        Transform[] newPortsGizmos = new Transform[array2.Length];
                        Array.Copy(__instance.portsGizmos, newPortsGizmos, __instance.portsGizmos.Length);
                        for (int i = 0;i < newPortsGizmos.Length - __instance.portsGizmos.Length; i++)
                        {
                            GameObject instantiatedGameObject = GameObject.Instantiate(__instance.portsGizmos[__instance.portsGizmos.Length -1 -i].gameObject, __instance.portsGizmos[__instance.portsGizmos.Length - 1 - i].position, __instance.portsGizmos[i].rotation) as GameObject;
                            if (__instance.portsGizmos[__instance.portsGizmos.Length - 1 - i].parent != null)
                            {
                                instantiatedGameObject.transform.SetParent(__instance.portsGizmos[__instance.portsGizmos.Length - 1 - i].parent);
                                //Debug.LogFormat("scppppppppppppppppppppppppp114514 has parent");
                            }
                            newPortsGizmos[__instance.portsGizmos.Length + i] = instantiatedGameObject.transform;
                        }
                        __instance.portsGizmos = newPortsGizmos;
                        //Debug.LogFormat("scppppppppppppppppppppppppp114514 portsGizmos {0}, array2 {1}", __instance.portsGizmos.Length, array2.Length);
                    }
                    for (int j = 0; j < array2.Length; j++)
                    {
                        __instance.portsGizmos[j].localPosition = array2[j].position - array2[j].forward;
                        __instance.portsGizmos[j].localRotation = array2[j].rotation;
                    }

                }
            }
            else if (isBelt)
            {
                    __instance.transform.localPosition = Vector3.zero;
                    __instance.transform.localRotation = Quaternion.identity;
            }
            else if (isInserter && prefabDesc.hasObject && prefabDesc.lodCount > 0 && prefabDesc.lodMeshes[0] != null)
            {
                    __instance.transform.localPosition = Vector3.zero;
                    __instance.transform.localRotation = Quaternion.identity;
                __instance.mf.sharedMesh = prefabDesc.lodMeshes[0];
                if (material == null)
                {
                    material = UnityEngine.Object.Instantiate(Configs.builtin.previewGizmoMat_Inserter);
                }

                material.SetVector("_Position1", __instance.Vector3BoolToVector4(Vector3.zero, __instance.desc.t1));
                material.SetVector("_Position2", __instance.Vector3BoolToVector4(Quaternion.Inverse(__instance.desc.wrot) * (__instance.desc.wpos2 - __instance.desc.wpos), __instance.desc.t2));
                material.SetVector("_Rotation1", __instance.QuaternionToVector4(Quaternion.identity));
                material.SetVector("_Rotation2", __instance.QuaternionToVector4(Quaternion.Inverse(__instance.desc.wrot) * __instance.desc.wrot2));
            }

            if (flag)
            {
                if (prefabDesc.isVeinCollector)
                {
                    __instance.minerFan.gameObject.SetActive(value: true);
                    __instance.minerFanRenderer0.gameObject.SetActive(value: false);
                    __instance.minerFanRenderer1.gameObject.SetActive(value: true);
                    __instance.minerFan.localEulerAngles = new Vector3(0f, 180f, 0f);
                    if (__instance.minerFanMat1 == null)
                    {
                        __instance.minerFanMat1 = UnityEngine.Object.Instantiate(__instance.minerFanRenderer1.sharedMaterial);
                        __instance.minerFanRenderer1.sharedMaterial = __instance.minerFanMat1;
                    }
                }
                else
                {
                    __instance.minerFan.gameObject.SetActive(value: true);
                    __instance.minerFanRenderer0.gameObject.SetActive(value: true);
                    __instance.minerFanRenderer1.gameObject.SetActive(value: false);
                    float num = 15.686f;
                    __instance.minerFan.localScale = new Vector3(num, num, num);
                    __instance.minerFan.localPosition = new Vector3(0f, 0f, -1.2f);
                    __instance.minerFan.localEulerAngles = new Vector3(0f, 180f, 0f);
                    if (__instance.minerFanMat0 == null)
                    {
                        __instance.minerFanMat0 = UnityEngine.Object.Instantiate(__instance.minerFanRenderer0.sharedMaterial);
                        __instance.minerFanRenderer0.sharedMaterial = __instance.minerFanMat0;
                    }
                }
            }

            __instance.mr.sharedMaterial = material;

            return false;
        }

        public static bool IsBeltCanTooFar(int itemId)
        {
            if (itemId == ProtoID.I物流立交 || itemId == ProtoID.I太空电梯 || itemId == ProtoID.I轨道空投引导站) {
                return true;
            }
            return false;
        }

        // 修复17层接口在蓝图粘贴时报距离太远无法建造的问题
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BuildTool_BlueprintPaste_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_I4, 2307));
            object V_9 = matcher.Advance(-4).Operand;
            object IL_0C59 = matcher.Advance(-1).Operand;

            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, V_9),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.output))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.item))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Proto), nameof(Proto.ID))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LogisticsInterchangePatches), nameof(IsBeltCanTooFar))),
                new CodeInstruction(OpCodes.Brtrue, IL_0C59)
            );

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_I4, 2307));
            matcher.Advance(1);

            matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_I4, 2307));
            object IL_0D1D = matcher.Advance(-5).Operand;

            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc, V_9),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.input))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.item))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Proto), nameof(Proto.ID))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LogisticsInterchangePatches), nameof(IsBeltCanTooFar))),
                new CodeInstruction(OpCodes.Brtrue, IL_0D1D)
            );

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        // 修复空轨可以用蓝图粘贴建造在16层以下的问题
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

                if (previewItem == ProtoID.I空轨) {
                    float num11 = 16 * 1.3333333f + __instance.planet.realRadius;
                    if (buildPreview.lpos.sqrMagnitude < num11 * num11) {
                        buildPreview.condition = (EBuildCondition)95;
                        __instance.AddErrorMessage((EBuildCondition)95, buildPreview);
                        continue;
                    }
                }
            }
            return true;
        }

        // 修复空轨可以通过吸附建造在16层以下的问题
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Path), "CheckBuildConditions")]
        public static void BuildTool_Path_CheckBuildConditionsPrePatch(BuildTool_Path __instance, ref bool __result)
        {
            if (__result == false) return;

            int count = __instance.buildPreviews.Count;
            for (int i = 0; i < count; i++) {
                BuildPreview buildPreview = __instance.buildPreviews[i];
                if (buildPreview.item.ID == ProtoID.I空轨) {
                    if (__instance.controller.cmd.stage == 0) {
                        continue;
                    }
                    float num11 = 16 * 1.3333333f + __instance.planet.realRadius;
                    if (buildPreview.lpos.sqrMagnitude < num11 * num11) {
                        buildPreview.condition = (EBuildCondition)95;
                        __result = false;
                    }
                }
            }
        }

        /*
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.SetEntityCapacity))]
        [HarmonyPrefix]
        public static bool SetEntityCapacityPatch(PlanetFactory __instance, int newCapacity)
        {
            EntityData[] array = __instance.entityPool;
            __instance.entityPool = new EntityData[newCapacity];
            __instance.entityRecycle = new int[newCapacity];
            if (array != null)
            {
                Array.Copy(array, __instance.entityPool, (newCapacity > __instance.entityCapacity) ? __instance.entityCapacity : newCapacity);
            }

            AnimData[] array2 = __instance.entityAnimPool;
            __instance.entityAnimPool = new AnimData[newCapacity];
            if (array2 != null)
            {
                Array.Copy(array2, __instance.entityAnimPool, (newCapacity > __instance.entityCapacity) ? __instance.entityCapacity : newCapacity);
            }

            SignData[] array3 = __instance.entitySignPool;
            __instance.entitySignPool = new SignData[newCapacity];
            if (array3 != null)
            {
                Array.Copy(array3, __instance.entitySignPool, (newCapacity > __instance.entityCapacity) ? __instance.entityCapacity : newCapacity);
            }

            int[] array4 = __instance.entityConnPool;
            __instance.entityConnPool = new int[newCapacity * 24];
            if (array4 != null)
            {
                Array.Copy(array4, __instance.entityConnPool, ((newCapacity > __instance.entityCapacity) ? __instance.entityCapacity : newCapacity) * 24);
            }

            Mutex[] array5 = __instance.entityMutexs;
            __instance.entityMutexs = new Mutex[newCapacity];
            if (array5 != null)
            {
                Array.Copy(array5, __instance.entityMutexs, __instance.entityCapacity);
            }

            int[][] array6 = __instance.entityNeeds;
            __instance.entityNeeds = new int[newCapacity][];
            if (array6 != null)
            {
                Array.Copy(array6, __instance.entityNeeds, (newCapacity > __instance.entityCapacity) ? __instance.entityCapacity : newCapacity);
            }

            __instance.entityCapacity = newCapacity;

            return false;
        }
        // BuildTool_Path的DeterminePreviews，EBuildCondition.Occupied时提示接口占用，由PlanetFactory entityConnPool 数组决定，起码是决定一半

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemoveEntityWithComponents))]
        [HarmonyPostfix]
        public static void RemoveEntityWithComponentsPatch(PlanetFactory __instance, int id)
        {
            if (id != 0 && __instance.entityPool[id].id != 0)
            {
                Array.Clear(__instance.entityConnPool, id * 24, 24);
            }
        }



        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConnDirect))]
        [HarmonyPrefix]
        public static bool WriteObjectConnDirectPatch(PlanetFactory __instance, int objId, int slot, bool isOutput, int otherObjId, int otherSlot)
        {
            if (objId == 0)
            {
                return false;
            }

            int num = 0;
            if (otherObjId != 0)
            {
                bool num2 = otherObjId > 0;
                otherObjId = (num2 ? otherObjId : (-otherObjId));
                num = otherObjId | (otherSlot << 24) | (((!isOutput) ? 1 : 0) << 29);
                if (!num2)
                {
                    num = -num;
                }
            }
            if (objId > 0)
            {
                __instance.entityConnPool[objId * 24 + slot] = num;
            }
            else if (objId < 0)
            {
                __instance.prebuildConnPool[-objId * 24 + slot] = num;
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConnDirect))]
        [HarmonyPrefix]
        public static bool ClearObjectConnDirectPatch(PlanetFactory __instance, int objId, int slot)
        {
            if (objId != 0)
            {
                if (objId > 0)
                {
                    __instance.entityConnPool[objId * 24 + slot] = 0;
                }
                else if (objId < 0)
                {
                    __instance.prebuildConnPool[-objId * 24 + slot] = 0;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConn), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static bool ClearObjectConnPatch(PlanetFactory __instance, int objId)
        {
            if (objId > 0) {
                int num = objId * 24;
                for (int i = 0; i < 24; i++) {
                    if (__instance.entityConnPool[num + i] != 0) {
                        __instance.ClearObjectConn(objId, i);
                    }
                }
            } else {
                if (objId >= 0) {
                    return false;
                }

                int num2 = -objId * 24;
                for (int j = 0; j < 24; j++) {
                    if (__instance.prebuildConnPool[num2 + j] != 0) {
                        __instance.ClearObjectConn(objId, j);
                    }
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConn))]
        [HarmonyPrefix]
        public static bool WriteObjectConnPatch(PlanetFactory __instance, int objId, int slot, bool isOutput, int otherObjId, int otherSlot)
        {
            if (otherSlot == -1)
            {
                if (otherObjId > 0)
                {
                    for (int i = 4; i < 12; i++)
                    {
                        if (__instance.entityConnPool[otherObjId * 24 + i] == 0)
                        {
                            otherSlot = i;
                            break;
                        }
                    }
                }
                else if (otherObjId < 0)
                {
                    for (int j = 4; j < 12; j++)
                    {
                        if (__instance.prebuildConnPool[-otherObjId * 24 + j] == 0)
                        {
                            otherSlot = j;
                            break;
                        }
                    }
                }
            }

            if (otherSlot >= 0)
            {
                __instance.ClearObjectConn(objId, slot);
                __instance.ClearObjectConn(otherObjId, otherSlot);
                __instance.WriteObjectConnDirect(objId, slot, isOutput, otherObjId, otherSlot);
                __instance.WriteObjectConnDirect(otherObjId, otherSlot, !isOutput, objId, slot);
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.HandleObjectConnChangeWhenBuild))]
        [HarmonyPrefix]
        public static bool HandleObjectConnChangeWhenBuildPatch(PlanetFactory __instance, int oldId, int newId)
        {
            for (int i = 0; i < 24; i++)
            {
                __instance.ReadObjectConn(oldId, i, out var isOutput, out var otherObjId, out var otherSlot);
                if (otherObjId != 0)
                {
                    __instance.WriteObjectConn(newId, i, isOutput, otherObjId, otherSlot);
                }
            }

            if (oldId > 0)
            {
                Array.Clear(__instance.entityConnPool, oldId * 24, 24);
            }
            else
            {
                Array.Clear(__instance.prebuildConnPool, -oldId * 24, 24);
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ReadObjectConn))]
        [HarmonyPrefix]
        public static bool ReadObjectConnPatch(PlanetFactory __instance, int objId, int slot, out bool isOutput, out int otherObjId, out int otherSlot)
        {
            isOutput = false;
            otherObjId = 0;
            otherSlot = 0;
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return true;
            }
            if (objId > 0) {
                if (objId >= __instance.entityCapacity) {
                    __instance.SetEntityCapacity(__instance.entityCapacity * 2);
                }
                int num = __instance.entityConnPool[objId * 24 + slot];
                if (num == 0) {
                    return false;
                }
                bool flag = num > 0;
                num = (flag ? num : (-num));
                isOutput = ((num & 536870912) == 0);
                otherObjId = (num & 16777215);
                otherSlot = (num & 536870911) >> 24;
                if (!flag) {
                    otherObjId = -otherObjId;
                    return false;
                }
            } else if (objId < 0) {
                int num2 = __instance.prebuildConnPool[-objId * 24 + slot];
                if (num2 == 0) {
                    return false;
                }
                bool flag2 = num2 > 0;
                num2 = (flag2 ? num2 : (-num2));
                isOutput = ((num2 & 536870912) == 0);
                otherObjId = (num2 & 16777215);
                otherSlot = (num2 & 536870911) >> 24;
                if (!flag2) {
                    otherObjId = -otherObjId;
                }
            }
            return false;
        }

        */

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        [HarmonyPrefix]
        public static bool ImportPatch(PlanetFactory __instance, int _index, GameData _gameData, Stream s, BinaryReader r)
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return true;
            }
            __instance.index = _index;
            __instance.gameData = _gameData;
            __instance.sector = __instance.gameData.spaceSector;
            __instance.skillSystem = __instance.gameData.spaceSector.skillSystem;
            int num = r.ReadInt32();
            bool flag = num >= 2;
            bool flag2 = num >= 4;
            PerformanceMonitor.BeginData(ESaveDataEntry.Planet);
            int planetId = r.ReadInt32();
            __instance.planet = __instance.gameData.galaxy.PlanetById(planetId);
            __instance.planet.factory = __instance;
            __instance.planet.factoryIndex = _index;
            int[] savedThemeIds = __instance.gameData.gameDesc.savedThemeIds;
            if (num >= 5) {
                r.ReadInt32();
                r.ReadInt32();
                int style = r.ReadInt32();
                __instance.planet.style = style;
            } else {
                __instance.planet.style = 0;
            }
            __instance.planet.ImportRuntime(r);
            if (num >= 3) {
                __instance.landed = r.ReadBoolean();
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Planet);
            PerformanceMonitor.BeginData(ESaveDataEntry.Factory);
            if (num >= 8) {
                __instance.hashSystemDynamic = new HashSystem(true);
                __instance.hashSystemDynamic.Import(r);
                __instance.hashSystemStatic = new HashSystem(true);
                __instance.hashSystemStatic.Import(r);
            } else {
                __instance.hashSystemDynamic = new HashSystem();
                __instance.hashSystemStatic = new HashSystem();
            }
            __instance.spaceHashSystemDynamic = new DFSDynamicHashSystem();
            __instance.spaceHashSystemDynamic.Init(__instance.planet);
            PerformanceMonitor.BeginData(ESaveDataEntry.Entity);
            int num2;
            if (flag2) {
                num2 = r.ReadInt32();
                __instance.SetEntityCapacity(num2);
                __instance.entityCursor = r.ReadInt32();
                __instance.entityRecycleCursor = r.ReadInt32();
                if (num <= 8) {
                    for (int i = 1; i < __instance.entityCursor; i++) {
                        __instance.entityPool[i].Import(s, r);
                        if (__instance.entityPool[i].id != 0) {
                            bool flag3 = false;
                            __instance.entityAnimPool[i].time = r.ReadSingle();
                            __instance.entityAnimPool[i].prepare_length = r.ReadSingle();
                            __instance.entityAnimPool[i].working_length = r.ReadSingle();
                            __instance.entityAnimPool[i].state = r.ReadUInt32();
                            __instance.entityAnimPool[i].power = r.ReadSingle();
                            __instance.entitySignPool[i].signType = (uint)r.ReadByte();
                            __instance.entitySignPool[i].iconType = (uint)r.ReadByte();
                            if (__instance.entitySignPool[i].iconType >= 128U) {
                                flag3 = true;
                                SignData[] array = __instance.entitySignPool;
                                int num3 = i;
                                array[num3].iconType = array[num3].iconType - 128U;
                            }
                            __instance.entitySignPool[i].iconId0 = (uint)r.ReadUInt16();
                            if (flag3) {
                                __instance.entitySignPool[i].count0 = r.ReadSingle();
                            }
                            __instance.entitySignPool[i].x = r.ReadSingle();
                            __instance.entitySignPool[i].y = r.ReadSingle();
                            __instance.entitySignPool[i].z = r.ReadSingle();
                            __instance.entitySignPool[i].w = r.ReadSingle();
                            int num4 = i * 24;
                            int num5 = num4 + 24;
                            for (int j = num4; j < num5; j++) {
                                if (r.ReadByte() == 0) {
                                    __instance.entityConnPool[j] = 0;
                                } else {
                                    __instance.entityConnPool[j] = r.ReadInt32();
                                }
                            }
                            if (__instance.entityPool[i].beltId == 0 && __instance.entityPool[i].inserterId == 0 && __instance.entityPool[i].splitterId == 0 && __instance.entityPool[i].monitorId == 0 && __instance.entityPool[i].spraycoaterId == 0 && __instance.entityPool[i].pilerId == 0) {
                                __instance.entityMutexs[i] = new Mutex(i);
                            }
                        }
                    }
                } else {
                    for (int k = 1; k < __instance.entityCursor; k++) {
                        __instance.entityPool[k].Import(s, r);
                        if (__instance.entityPool[k].id != 0 && __instance.entityPool[k].beltId == 0 && __instance.entityPool[k].inserterId == 0 && __instance.entityPool[k].splitterId == 0 && __instance.entityPool[k].monitorId == 0 && __instance.entityPool[k].spraycoaterId == 0 && __instance.entityPool[k].pilerId == 0) {
                            __instance.entityMutexs[k] = new Mutex(k);
                        }
                    }
                    UnsafeIO.ReadMassive<AnimData>(s, __instance.entityAnimPool, __instance.entityCursor);
                    for (int l = 1; l < __instance.entityCursor; l++) {
                        if (__instance.entityPool[l].id != 0) {
                            bool flag4 = false;
                            __instance.entitySignPool[l].signType = (uint)r.ReadByte();
                            __instance.entitySignPool[l].iconType = (uint)r.ReadByte();
                            if (__instance.entitySignPool[l].iconType >= 128U) {
                                flag4 = true;
                                SignData[] array2 = __instance.entitySignPool;
                                int num6 = l;
                                array2[num6].iconType = array2[num6].iconType - 128U;
                            }
                            __instance.entitySignPool[l].iconId0 = (uint)r.ReadUInt16();
                            if (flag4) {
                                __instance.entitySignPool[l].count0 = r.ReadSingle();
                            }
                            UnsafeIO.Read<SignData>(s, ref __instance.entitySignPool[l], 40, 24);
                        }
                    }
                    for (int m = 24; m < __instance.entityCursor * 24; m++) {
                        if (__instance.entityPool[m / 24].id != 0) {
                            if (r.ReadByte() == 0) {
                                __instance.entityConnPool[m] = 0;
                            } else {
                                __instance.entityConnPool[m] = r.ReadInt32();
                            }
                        }
                    }
                }
                for (int n = 0; n < __instance.entityRecycleCursor; n++) {
                    __instance.entityRecycle[n] = r.ReadInt32();
                }
                num2 = r.ReadInt32();
                __instance.SetPrebuildCapacity(num2);
                __instance.prebuildCursor = r.ReadInt32();
                __instance.prebuildRecycleCursor = r.ReadInt32();
                for (int num7 = 1; num7 < __instance.prebuildCursor; num7++) {
                    __instance.prebuildPool[num7].Import(r);
                    if (__instance.prebuildPool[num7].id != 0) {
                        int num8 = num7 * 24;
                        int num9 = num8 + 24;
                        for (int num10 = num8; num10 < num9; num10++) {
                            if (r.ReadByte() == 0) {
                                __instance.prebuildConnPool[num10] = 0;
                            } else {
                                __instance.prebuildConnPool[num10] = r.ReadInt32();
                            }
                        }
                    }
                }
                for (int num11 = 0; num11 < __instance.prebuildRecycleCursor; num11++) {
                    __instance.prebuildRecycle[num11] = r.ReadInt32();
                }
            } else {
                num2 = r.ReadInt32();
                __instance.SetEntityCapacity(num2);
                __instance.entityCursor = r.ReadInt32();
                __instance.entityRecycleCursor = r.ReadInt32();
                for (int num12 = 1; num12 < __instance.entityCursor; num12++) {
                    __instance.entityPool[num12].Import(s, r);
                    if (__instance.entityPool[num12].id != 0 && __instance.entityPool[num12].beltId == 0 && __instance.entityPool[num12].inserterId == 0 && __instance.entityPool[num12].splitterId == 0 && __instance.entityPool[num12].monitorId == 0 && __instance.entityPool[num12].spraycoaterId == 0 && __instance.entityPool[num12].pilerId == 0) {
                        __instance.entityMutexs[num12] = new Mutex(num12);
                    }
                }
                for (int num13 = 1; num13 < __instance.entityCursor; num13++) {
                    __instance.entityAnimPool[num13].time = r.ReadSingle();
                    __instance.entityAnimPool[num13].prepare_length = r.ReadSingle();
                    __instance.entityAnimPool[num13].working_length = r.ReadSingle();
                    __instance.entityAnimPool[num13].state = r.ReadUInt32();
                    __instance.entityAnimPool[num13].power = r.ReadSingle();
                }
                if (flag) {
                    for (int num14 = 1; num14 < __instance.entityCursor; num14++) {
                        __instance.entitySignPool[num14].signType = (uint)r.ReadByte();
                        __instance.entitySignPool[num14].iconType = (uint)r.ReadByte();
                        __instance.entitySignPool[num14].iconId0 = (uint)r.ReadUInt16();
                        __instance.entitySignPool[num14].x = r.ReadSingle();
                        __instance.entitySignPool[num14].y = r.ReadSingle();
                        __instance.entitySignPool[num14].z = r.ReadSingle();
                        __instance.entitySignPool[num14].w = r.ReadSingle();
                    }
                } else {
                    for (int num15 = 1; num15 < __instance.entityCursor; num15++) {
                        __instance.entitySignPool[num15].signType = r.ReadUInt32();
                        __instance.entitySignPool[num15].iconType = r.ReadUInt32();
                        __instance.entitySignPool[num15].iconId0 = r.ReadUInt32();
                        __instance.entitySignPool[num15].iconId1 = r.ReadUInt32();
                        __instance.entitySignPool[num15].iconId2 = r.ReadUInt32();
                        __instance.entitySignPool[num15].iconId3 = r.ReadUInt32();
                        __instance.entitySignPool[num15].count0 = r.ReadSingle();
                        __instance.entitySignPool[num15].count1 = r.ReadSingle();
                        __instance.entitySignPool[num15].count2 = r.ReadSingle();
                        __instance.entitySignPool[num15].count3 = r.ReadSingle();
                        __instance.entitySignPool[num15].x = r.ReadSingle();
                        __instance.entitySignPool[num15].y = r.ReadSingle();
                        __instance.entitySignPool[num15].z = r.ReadSingle();
                        __instance.entitySignPool[num15].w = r.ReadSingle();
                    }
                }
                int num16 = __instance.entityCursor * 24;
                for (int num17 = 24; num17 < num16; num17++) {
                    __instance.entityConnPool[num17] = r.ReadInt32();
                }
                for (int num18 = 0; num18 < __instance.entityRecycleCursor; num18++) {
                    __instance.entityRecycle[num18] = r.ReadInt32();
                }
                num2 = r.ReadInt32();
                __instance.SetPrebuildCapacity(num2);
                __instance.prebuildCursor = r.ReadInt32();
                __instance.prebuildRecycleCursor = r.ReadInt32();
                for (int num19 = 1; num19 < __instance.prebuildCursor; num19++) {
                    __instance.prebuildPool[num19].Import(r);
                }
                int num20 = __instance.prebuildCursor * 24;
                for (int num21 = 24; num21 < num20; num21++) {
                    __instance.prebuildConnPool[num21] = r.ReadInt32();
                }
                for (int num22 = 0; num22 < __instance.prebuildRecycleCursor; num22++) {
                    __instance.prebuildRecycle[num22] = r.ReadInt32();
                }
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Entity);
            if (num >= 8) {
                num2 = r.ReadInt32();
                __instance.SetCraftCapacity(num2);
                __instance.craftCursor = r.ReadInt32();
                __instance.craftRecycleCursor = r.ReadInt32();
                for (int num23 = 1; num23 < __instance.craftCursor; num23++) {
                    __instance.craftPool[num23].Import(r);
                }
                for (int num24 = 0; num24 < __instance.craftRecycleCursor; num24++) {
                    __instance.craftRecycle[num24] = r.ReadInt32();
                }
                for (int num25 = 1; num25 < __instance.craftCursor; num25++) {
                    __instance.craftAnimPool[num25].time = r.ReadSingle();
                    __instance.craftAnimPool[num25].prepare_length = r.ReadSingle();
                    __instance.craftAnimPool[num25].working_length = r.ReadSingle();
                    __instance.craftAnimPool[num25].state = r.ReadUInt32();
                    __instance.craftAnimPool[num25].power = r.ReadSingle();
                }
                if (__instance.gameData.patch < 10) {
                    for (int num26 = 1; num26 < __instance.craftCursor; num26++) {
                        ref CraftData ptr = ref __instance.craftPool[num26];
                        if (ptr.fleetId > 0 && ptr.owner > 0 && ptr.port == 1) {
                            ptr.port = 0;
                        }
                    }
                }
            } else {
                __instance.SetCraftCapacity(64);
                __instance.craftCursor = 1;
                __instance.craftRecycleCursor = 0;
            }
            PerformanceMonitor.BeginData(ESaveDataEntry.Combat);
            if (num >= 8) {
                num2 = r.ReadInt32();
                __instance.SetEnemyCapacity(num2);
                __instance.enemyCursor = r.ReadInt32();
                __instance.enemyRecycleCursor = r.ReadInt32();
                for (int num27 = 1; num27 < __instance.enemyCursor; num27++) {
                    __instance.enemyPool[num27].Import(r);
                }
                for (int num28 = 0; num28 < __instance.enemyRecycleCursor; num28++) {
                    __instance.enemyRecycle[num28] = r.ReadInt32();
                }
                for (int num29 = 1; num29 < __instance.enemyCursor; num29++) {
                    __instance.enemyAnimPool[num29].time = r.ReadSingle();
                    __instance.enemyAnimPool[num29].prepare_length = r.ReadSingle();
                    __instance.enemyAnimPool[num29].working_length = r.ReadSingle();
                    __instance.enemyAnimPool[num29].state = r.ReadUInt32();
                    __instance.enemyAnimPool[num29].power = r.ReadSingle();
                }
            } else {
                __instance.SetEnemyCapacity(24);
                __instance.enemyCursor = 1;
                __instance.enemyRecycleCursor = 0;
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Combat);
            PerformanceMonitor.EndData(ESaveDataEntry.Factory);
            PerformanceMonitor.BeginData(ESaveDataEntry.Planet);
            num2 = r.ReadInt32();
            __instance.SetVegeCapacity(num2);
            __instance.vegeCursor = r.ReadInt32();
            __instance.vegeRecycleCursor = r.ReadInt32();
            for (int num30 = 1; num30 < __instance.vegeCursor; num30++) {
                __instance.vegePool[num30].Import(r);
            }
            for (int num31 = 0; num31 < __instance.vegeRecycleCursor; num31++) {
                __instance.vegeRecycle[num31] = r.ReadInt32();
            }
            num2 = r.ReadInt32();
            __instance.SetVeinCapacity(num2);
            __instance.veinCursor = r.ReadInt32();
            __instance.veinRecycleCursor = r.ReadInt32();
            int num32 = 0;
            for (int num33 = 1; num33 < __instance.veinCursor; num33++) {
                __instance.veinPool[num33].Import(r);
                if (num < 7) {
                    VeinData[] array3 = __instance.veinPool;
                    int num34 = num33;
                    array3[num34].groupIndex = (short)(array3[num34].groupIndex + 1);
                }
                if (__instance.veinPool[num33].groupIndex < 0) {
                    __instance.veinPool[num33].groupIndex = 0;
                }
                if ((int)__instance.veinPool[num33].groupIndex > num32) {
                    num32 = (int)__instance.veinPool[num33].groupIndex;
                }
            }
            for (int num35 = 0; num35 < __instance.veinRecycleCursor; num35++) {
                __instance.veinRecycle[num35] = r.ReadInt32();
            }
            for (int num36 = 1; num36 < __instance.veinCursor; num36++) {
                __instance.veinAnimPool[num36].time = r.ReadSingle();
                __instance.veinAnimPool[num36].prepare_length = r.ReadSingle();
                __instance.veinAnimPool[num36].working_length = r.ReadSingle();
                __instance.veinAnimPool[num36].state = r.ReadUInt32();
                __instance.veinAnimPool[num36].power = r.ReadSingle();
            }
            __instance.InitVeinGroups(num32);
            __instance.RecalculateAllVeinGroups();
            PerformanceMonitor.EndData(ESaveDataEntry.Planet);
            PerformanceMonitor.BeginData(ESaveDataEntry.Factory);
            if (num < 8) {
                __instance.RefreshHashSystems();
            }
            PerformanceMonitor.BeginData(ESaveDataEntry.Ruin);
            if (num >= 8) {
                num2 = r.ReadInt32();
                __instance.SetRuinCapacity(num2);
                __instance.ruinCursor = r.ReadInt32();
                __instance.ruinRecycleCursor = r.ReadInt32();
                for (int num37 = 1; num37 < __instance.ruinCursor; num37++) {
                    __instance.ruinPool[num37].Import(r);
                }
                for (int num38 = 0; num38 < __instance.ruinRecycleCursor; num38++) {
                    __instance.ruinRecycle[num38] = r.ReadInt32();
                }
            } else {
                __instance.SetRuinCapacity(24);
                __instance.ruinCursor = 1;
                __instance.ruinRecycleCursor = 0;
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Ruin);
            PerformanceMonitor.BeginData(ESaveDataEntry.BeltAndCargo);
            __instance.cargoContainer = new CargoContainer(true);
            __instance.cargoContainer.Import(r);
            __instance.cargoTraffic = new CargoTraffic();
            __instance.cargoTraffic.planet = __instance.planet;
            __instance.cargoTraffic.factory = __instance;
            __instance.cargoTraffic.container = __instance.cargoContainer;
            __instance.cargoTraffic.Import(r);
            PerformanceMonitor.EndData(ESaveDataEntry.BeltAndCargo);
            __instance.blockContainer = new MiniBlockContainer();
            PerformanceMonitor.BeginData(ESaveDataEntry.Storage);
            __instance.factoryStorage = new FactoryStorage(__instance.planet, true);
            __instance.factoryStorage.Import(r);
            PerformanceMonitor.EndData(ESaveDataEntry.Storage);
            PerformanceMonitor.BeginData(ESaveDataEntry.PowerSystem);
            __instance.powerSystem = new PowerSystem(__instance.planet, true);
            __instance.powerSystem.Import(r);
            PerformanceMonitor.EndData(ESaveDataEntry.PowerSystem);
            PerformanceMonitor.BeginData(ESaveDataEntry.Facility);
            __instance.factorySystem = new FactorySystem(__instance.planet, true);
            __instance.factorySystem.Import(r);
            PerformanceMonitor.EndData(ESaveDataEntry.Facility);
            PerformanceMonitor.BeginData(ESaveDataEntry.Combat);
            if (num >= 8) {
                __instance.enemySystem = new EnemyDFGroundSystem(__instance.planet, true);
                __instance.enemySystem.Import(r);
                __instance.combatGroundSystem = new CombatGroundSystem(__instance.planet, true);
                __instance.combatGroundSystem.Import(r);
                PerformanceMonitor.BeginData(ESaveDataEntry.Defense);
                __instance.defenseSystem = new DefenseSystem(__instance.planet, true);
                __instance.defenseSystem.Import(r);
                __instance.planetATField = new PlanetATField(__instance.planet, true);
                __instance.planetATField.Import(r);
                __instance.planetATField.UpdatePhysicsShape(true);
                PerformanceMonitor.EndData(ESaveDataEntry.Defense);
                PerformanceMonitor.BeginData(ESaveDataEntry.Construction);
                __instance.constructionSystem = new ConstructionSystem(__instance.planet, true);
                __instance.constructionSystem.Import(r);
                PerformanceMonitor.EndData(ESaveDataEntry.Construction);
                if (__instance.gameData.patch < 12) {
                    DFGBaseComponent[] buffer = __instance.enemySystem.bases.buffer;
                    int cursor = __instance.enemySystem.bases.cursor;
                    for (int num39 = 1; num39 < cursor; num39++) {
                        if (buffer[num39] != null && buffer[num39].id == num39 && buffer[num39].ruinId > 0) {
                            if (__instance.ruinPool[buffer[num39].ruinId].id != buffer[num39].ruinId) {
                                buffer[num39].ruinId = 0;
                            } else {
                                Vector3 normalized = __instance.ruinPool[buffer[num39].ruinId].pos.normalized;
                                Vector3 a = __instance.enemyPool[buffer[num39].enemyId].pos.normalized;
                                if ((normalized * 200f - a * 200f).magnitude > 10f) {
                                    buffer[num39].ruinId = 0;
                                }
                            }
                        }
                    }
                    PowerGeneratorComponent[] genPool = __instance.powerSystem.genPool;
                    int genCursor = __instance.powerSystem.genCursor;
                    for (int num40 = 1; num40 < genCursor; num40++) {
                        ref PowerGeneratorComponent ptr2 = ref genPool[num40];
                        if (ptr2.id == num40 && ptr2.geothermal && genPool[num40].baseRuinId > 0) {
                            if (__instance.ruinPool[genPool[num40].baseRuinId].id != genPool[num40].baseRuinId) {
                                genPool[num40].baseRuinId = 0;
                            } else {
                                Vector3 normalized2 = __instance.ruinPool[genPool[num40].baseRuinId].pos.normalized;
                                Vector3 normalized3 = __instance.entityPool[genPool[num40].entityId].pos.normalized;
                                if ((normalized2 * 200f - normalized3 * 200f).magnitude > 10f) {
                                    genPool[num40].baseRuinId = 0;
                                }
                            }
                        }
                    }
                }
                int patch = __instance.gameData.patch;
                DataPool<UnitComponent> units = __instance.combatGroundSystem.units;
                for (int num41 = 1; num41 < units.cursor; num41++) {
                    ref UnitComponent ptr3 = ref units.buffer[num41];
                    if (ptr3.id == num41) {
                        ref CraftData ptr4 = ref __instance.craftPool[ptr3.craftId];
                        if (ptr4.id == ptr3.craftId) {
                            if (ptr4.owner <= 0) {
                                __instance.RemoveCraftWithComponents(ptr4.id);
                            } else if (__instance.craftPool[ptr4.owner].id != ptr4.owner) {
                                __instance.RemoveCraftWithComponents(ptr4.id);
                            }
                        }
                    }
                }
                DataPool<FleetComponent> fleets = __instance.combatGroundSystem.fleets;
                for (int num42 = 1; num42 < fleets.cursor; num42++) {
                    ref FleetComponent ptr5 = ref fleets.buffer[num42];
                    if (ptr5.id == num42) {
                        ref CraftData ptr6 = ref __instance.craftPool[ptr5.craftId];
                        if (ptr6.id == ptr5.craftId && !ptr5.CheckOwnerExist(ref ptr6, __instance, __instance.gameData.mainPlayer.mecha)) {
                            __instance.RemoveCraftWithComponents(ptr5.craftId);
                        }
                    }
                }
                ObjectPool<CombatModuleComponent> combatModules = __instance.combatGroundSystem.combatModules;
                for (int num43 = 1; num43 < combatModules.cursor; num43++) {
                    ref CombatModuleComponent ptr7 = ref combatModules.buffer[num43];
                    if (ptr7 != null && ptr7.id == num43 && __instance.entityPool[ptr7.entityId].id == ptr7.entityId) {
                        ModuleFleet[] moduleFleets = ptr7.moduleFleets;
                        int fleetCount = ptr7.fleetCount;
                        for (int num44 = 0; num44 < fleetCount; num44++) {
                            int fleetId = moduleFleets[num44].fleetId;
                            if (fleetId != 0) {
                                fleets = __instance.combatGroundSystem.fleets;
                                ref FleetComponent ptr8 = ref fleets.buffer[fleetId];
                                if (ptr8.owner != ptr7.entityId) {
                                    moduleFleets[num44].ClearFleetForeignKey();
                                } else {
                                    ref CraftData ptr9 = ref __instance.craftPool[ptr8.craftId];
                                    if (ptr9.id != ptr8.craftId || ptr9.owner != ptr8.owner) {
                                        moduleFleets[num44].ClearFleetForeignKey();
                                    } else {
                                        ModuleFighter[] fighters = moduleFleets[num44].fighters;
                                        int num45 = fighters.Length;
                                        for (int num46 = 0; num46 < num45; num46++) {
                                            int craftId = fighters[num46].craftId;
                                            if (craftId != 0) {
                                                ref CraftData ptr10 = ref __instance.craftPool[craftId];
                                                if (ptr10.id != craftId || ptr10.unitId == 0) {
                                                    fighters[num46].ClearFighterForeignKey();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } else {
                __instance.enemySystem = new EnemyDFGroundSystem(__instance.planet);
                __instance.combatGroundSystem = new CombatGroundSystem(__instance.planet);
                __instance.defenseSystem = new DefenseSystem(__instance.planet);
                __instance.planetATField = new PlanetATField(__instance.planet);
                __instance.constructionSystem = new ConstructionSystem(__instance.planet);
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Combat);
            PerformanceMonitor.BeginData(ESaveDataEntry.Transport);
            __instance.transport = new PlanetTransport(__instance.gameData, __instance.planet, true);
            __instance.transport.Init();
            __instance.transport.Import(r);
            PerformanceMonitor.EndData(ESaveDataEntry.Transport);
            if (num < 4) {
                r.ReadInt32();
                r.ReadInt32();
                r.ReadInt32();
                r.ReadInt32();
            }
            PerformanceMonitor.BeginData(ESaveDataEntry.Platform);
            if (num >= 1) {
                __instance.platformSystem = new PlatformSystem(__instance.planet, true);
                __instance.platformSystem.Import(r);
            } else {
                __instance.platformSystem = new PlatformSystem(__instance.planet);
            }
            __instance.enemySystem.RefreshPlanetReformState();
            PerformanceMonitor.EndData(ESaveDataEntry.Platform);
            PerformanceMonitor.BeginData(ESaveDataEntry.Digital);
            if (num >= 6) {
                __instance.digitalSystem = new DigitalSystem(__instance.planet, true);
                __instance.digitalSystem.Import(r);
            } else {
                __instance.digitalSystem = new DigitalSystem(__instance.planet);
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Digital);
            ProductionStatistics production = __instance.gameData.statistics.production;
            if (production.factoryStatPool[__instance.index] == null) {
                production.CreateFactoryStat(__instance.index);
            }
            if (__instance.entityCount > 0 || __instance.prebuildCount > 0 || __instance.veinRecycleCursor > 0 || __instance.vegeRecycleCursor > 0 || __instance.planet.id == _gameData.galaxy.birthPlanetId) {
                __instance.landed = true;
            }
            PerformanceMonitor.EndData(ESaveDataEntry.Factory);

            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.SetPrebuildCapacity))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.SetEntityCapacity))]
        //[HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        public static IEnumerable<CodeInstruction> PlanetFactory_SetPrebuildCapacity_Patch(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)16))
                .Repeat(matcher => {
                    matcher.SetOperandAndAdvance((sbyte)24);
                })
                .InstructionEnumeration();

            return instructions;
        }



        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ValidateConns))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.AddPrebuildData))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EnsureObjectConn))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.KillEntityFinally))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemovePrebuildWithComponents))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ReadObjectConn))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemoveEntityWithComponents))]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.SetEntityCapacity))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConnDirect))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConnDirect))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConn))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConn), new Type[] { typeof(int) })]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.HandleObjectConnChangeWhenBuild))]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.SetPrebuildCapacity))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.DeterminePreviews))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Patch(IEnumerable<CodeInstruction> instructions)
        {
            instructions = new CodeMatcher(instructions)
                .MatchForward(
                    true,
                    new CodeMatch(i => i.opcode == Ldc_I4_S && Convert.ToInt32(i.operand ?? 0.0) == 16)
                )
                .Repeat(matcher => {
                    matcher.SetInstruction(new CodeInstruction(Ldc_I4_S, (sbyte)24));

                }).InstructionEnumeration();

            return instructions;
        }

        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.DeterminePreviews))]
        [HarmonyPrefix]
        public static void BuildTool_Path_DeterminePreviews_PrePatch(BuildTool_Path __instance)
        {
            if (__instance.tmp_conn.Length < 24) {
                __instance.tmp_conn = new int[24];
            }
        }


    }
}
