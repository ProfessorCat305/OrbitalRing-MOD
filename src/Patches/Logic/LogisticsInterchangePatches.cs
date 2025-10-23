using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Cryptography;
using HarmonyLib;
using UnityEngine;
using ProjectOrbitalRing.Utils;
using GalacticScale;
using UnityEngine.Playables;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using static ProjectOrbitalRing.ProjectOrbitalRing;

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
                //new Pose(megaPumper.portPoses[0].position, megaPumper.portPoses[0].rotation),
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                //new Pose(megaPumper.portPoses[2].position, megaPumper.portPoses[2].rotation),
                //new Pose(megaPumper.portPoses[3].position, megaPumper.portPoses[3].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                //new Pose(megaPumper.portPoses[5].position, megaPumper.portPoses[5].rotation),
                //new Pose(megaPumper.portPoses[6].position, megaPumper.portPoses[6].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                //new Pose(megaPumper.portPoses[8].position, megaPumper.portPoses[8].rotation),
                //new Pose(megaPumper.portPoses[9].position, megaPumper.portPoses[9].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
                //new Pose(megaPumper.portPoses[11].position, megaPumper.portPoses[11].rotation)
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 22.5f;
            }
            megaPumper.portPoses = newPortPoses;

            DelStationPose(50); // 太空物流港

        }

       
       //[HarmonyPostfix]
       //[HarmonyPatch(typeof(BuildTool_Path), "CheckBuildConditions")]
       // public static void CheckBuildConditionsPatch(BuildTool_Path __instance, ref bool __result)
       // {
       //     GameHistoryData history = __instance.actionBuild.history;
       //     int count = __instance.buildPreviews.Count;
       //     int num = count - 1;
       //     for (int i = 0; i < count; i++) {
       //         BuildPreview buildPreview = __instance.buildPreviews[i];
       //         if (buildPreview.item.ID == ProtoID.I轨道连接组件 || buildPreview.item.ID == ProtoID.I粒子加速轨道 || buildPreview.item.ID == ProtoID.I星环电网组件) {
       //             if (buildPreview.condition == EBuildCondition.Collide) {
       //                 if (history.buildMaxHeight + 0.5f <= 64f) {
       //                     buildPreview.condition = EBuildCondition.Ok;
       //                     __result = true;
       //                     __instance.actionBuild.model.cursorState = 0;
       //                     __instance.actionBuild.model.cursorText = BuildPreview.GetConditionText(EBuildCondition.Ok);
       //                     float num36 = __instance.altitude;
       //                     float num37 = __instance.altitude;
       //                     float num38 = __instance.tilt;
       //                     float num39 = __instance.tilt;
       //                     if (count > 0) {
       //                         num36 = (__instance.buildPreviews[0].lpos.magnitude - __instance.planet.realRadius - 0.2f) / 1.33333325f;
       //                         num37 = (__instance.buildPreviews[count - 1].lpos.magnitude - __instance.planet.realRadius - 0.2f) / 1.33333325f;
       //                         num36 = Mathf.Round(num36 * 100f) / 100f;
       //                         num37 = Mathf.Round(num37 * 100f) / 100f;
       //                         if ((double)num36 < 0.041) {
       //                             num36 = 0f;
       //                         }

       //                         if ((double)num37 < 0.041) {
       //                             num37 = 0f;
       //                         }

       //                         num38 = __instance.buildPreviews[0].tilt;
       //                         num39 = __instance.buildPreviews[count - 1].tilt;
       //                     }

       //                     if (num36 > 0f || num37 > 0f || num38 != 0f || num39 != 0f || Mathf.Abs(__instance.maxSlope) > 1E-06f) {
       //                         __instance.actionBuild.model.cursorText += "<size=12>";
       //                         if (num36 > 0f || num37 > 0f) {
       //                             string arg = ((num36 != num37) ? $"{num36:0.##}\u2006～\u2006{num37:0.##}" : $"{num36:0.##}");
       //                             __instance.actionBuild.model.cursorText += string.Format("传送带高度提示".Translate(), arg);
       //                         }

       //                         if (num38 != 0f || num39 != 0f) {
       //                             string arg2 = ((num38 != num39) ? $"{0f - num38:+0.#;-0.#;0}\u2006～\u2006{0f - num39:+0.#;-0.#;0}" : $"{0f - num38:+0.#;-0.#;0}°");
       //                             __instance.actionBuild.model.cursorText += string.Format("传送带倾斜提示".Translate(), arg2);
       //                         }

       //                         if (Mathf.Abs(__instance.maxSlope) > 0.01f) {
       //                             string arg3 = ((!(Mathf.Abs(__instance.maxSlope) < 500f)) ? "几乎垂直".Translate() : $"{__instance.maxSlope:0.##}");
       //                             __instance.actionBuild.model.cursorText += string.Format("传送带坡度提示".Translate(), arg3);
       //                         }
       //                     }
       //                 }
       //             }
       //         }
       //     }

       // }

        internal static void StationPrefabDescPostAdd810()
        {
            PrefabDesc megaPumper = LDB.models.Select(810).prefabDesc;
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
                new Pose(megaPumper.portPoses[1].position, megaPumper.portPoses[1].rotation),
                new Pose(megaPumper.portPoses[4].position, megaPumper.portPoses[4].rotation),
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
                new Pose(megaPumper.portPoses[10].position, megaPumper.portPoses[10].rotation),
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 22.5f;
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

            megaPumper = LDB.models.Select(805).prefabDesc;
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
                new Pose(megaPumper.portPoses[7].position, megaPumper.portPoses[7].rotation),
            };
            for (int i = 12; i < newPortPoses.Length; i++)
            {
                newPortPoses[i].position.y = 22.5f;
            }
            megaPumper.portPoses = newPortPoses;


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

        }

        /*
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
                                Debug.LogFormat("scppppppppppppppppppppppppp114514 has parent");
                            }
                            newPortsGizmos[__instance.portsGizmos.Length + i] = instantiatedGameObject.transform;
                        }
                        __instance.portsGizmos = newPortsGizmos;
                        Debug.LogFormat("scppppppppppppppppppppppppp114514 portsGizmos {0}, array2 {1}", __instance.portsGizmos.Length, array2.Length);
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

        // BuildTool_Path的DeterminePreviews，EBuildCondition.Occupied时提示接口占用，由PlanetFactory entityConnPool 数组决定，起码是决定一半
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

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemoveEntityWithComponents))]
        [HarmonyPostfix]
        public static void RemoveEntityWithComponentsPatch(PlanetFactory __instance, int id, bool isKill)
        {
            bool flag = false;
            if (id != 0 && __instance.entityPool[id].id != 0)
            {
                Array.Clear(__instance.entityConnPool, id * 24, 24);
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ReadObjectConn))]
        [HarmonyPostfix]
        public static void ReadObjectConnPatch(PlanetFactory __instance, int objId, int slot, out bool isOutput, out int otherObjId, out int otherSlot)
        {
            isOutput = false;
            otherObjId = 0;
            otherSlot = 0;
            if (objId > 0)
            {
                int num = __instance.entityConnPool[objId * 24 + slot];
                if (num != 0)
                {
                    bool num2 = num > 0;
                    num = (num2 ? num : (-num));
                    isOutput = (num & 0x20000000) == 0;
                    otherObjId = num & 0xFFFFFF;
                    otherSlot = (num & 0x1FFFFFFF) >> 24;
                    if (!num2)
                    {
                        otherObjId = -otherObjId;
                    }
                }
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
                __instance.prebuildConnPool[-objId * 16 + slot] = num;
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
                    __instance.prebuildConnPool[-objId * 16 + slot] = 0;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConn), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        public static bool ClearObjectConnPatch(PlanetFactory __instance, int objId)
        {
            if (objId > 0)
            {
                int num = objId * 16;
                for (int i = 0; i < 16; i++)
                {
                    if (__instance.entityConnPool[num + i] != 0)
                    {
                        __instance.ClearObjectConn(objId, i);
                    }
                }
            }
            else
            {
                if (objId >= 0)
                {
                    return false;
                }

                int num2 = -objId * 16;
                for (int j = 0; j < 16; j++)
                {
                    if (__instance.prebuildConnPool[num2 + j] != 0)
                    {
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
                        if (__instance.prebuildConnPool[-otherObjId * 16 + j] == 0)
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
                Array.Clear(__instance.prebuildConnPool, -oldId * 16, 16);
            }
            return false;
        }
        */
    }
}
