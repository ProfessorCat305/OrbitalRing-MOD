using HarmonyLib;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.CurvatureLogistics
{
    // 加了环的模型，环不能用其他材质，只能用船自己的材质才能显示，不知道为什么
    // 并且加了环后，喷口火焰特效的面对应贴图消失，变成纯白，不知道为什么，哪怕prefab的结构已经还原的和原版一样了
    // 当前只能把喷口火焰特效的面去掉，直接不显示
    internal class DeepSpaceLogisticShipRenderer
    {
        public static ShipRenderingData[] DeepSpaceShipsArr;

        public static ComputeBuffer DeepSpaceShipsBuffer;

        public static Material[] DeepSpaceShipMats;

        public static Mesh DeepSpaceShipMesh;

        public static int DeepSpaceShipCount;

        public static ComputeBuffer DeepSpaceArgBuffer;

        public static uint[] argArr = new uint[25];

        public static int DeepSpaceCapacity {
            get {
                if (DeepSpaceShipsArr != null) {
                    return DeepSpaceShipsArr.Length;
                }
                return 0;
            }
        }

        public static void DeepSpaceSetCapacity(int newCap)
        {
            if (newCap < 512) {
                newCap = 512;
            }

            if (DeepSpaceShipsArr == null) {
                DeepSpaceShipsArr = new ShipRenderingData[newCap];
                DeepSpaceShipsBuffer = new ComputeBuffer(newCap, 64, ComputeBufferType.Default);
                return;
            }

            int num = DeepSpaceShipsArr.Length;
            if (newCap <= num) {
                return;
            }

            Array array = DeepSpaceShipsArr;
            DeepSpaceShipsArr = new ShipRenderingData[newCap];
            if (DeepSpaceShipsBuffer != null) {
                DeepSpaceShipsBuffer.Release();
            }

            DeepSpaceShipsBuffer = new ComputeBuffer(newCap, 64, ComputeBufferType.Default);
            Array.Copy(array, DeepSpaceShipsArr, num);
        }

        public static void DeepSpaceExpand2x()
        {
            DeepSpaceSetCapacity(DeepSpaceCapacity * 2);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipRenderer), MethodType.Constructor, new[] { typeof(GalacticTransport) })]
        public static void LogisticShipRenderer_Patch(LogisticShipRenderer __instance)
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null) {
                return;
            }
            PrefabDesc prefabDesc = LDB.items.Select(6230).prefabDesc; //6230
            DeepSpaceShipMesh = prefabDesc.lodMeshes[0];
            Material[] array = prefabDesc.lodMaterials[0];
            DeepSpaceShipMats = new Material[array.Length];
            int num = 0;
            while (num < array.Length) {
                DeepSpaceShipMats[num] = UnityEngine.Object.Instantiate<Material>(array[num]);
                num++;
            }
            DeepSpaceArgBuffer = new ComputeBuffer(25, 4, ComputeBufferType.DrawIndirect);
            DeepSpaceShipCount = 0;
            //LogError($"scpppppppppppppppppppppppp LogisticShipRenderer_Patch");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipRenderer), nameof(LogisticShipRenderer.Destroy))]
        public static void LogisticShipRenderer_Destroy_Patch()
        {
            DeepSpaceShipsArr = null;
            DeepSpaceShipCount = 0;
            if (DeepSpaceShipsBuffer != null) {
                DeepSpaceShipsBuffer.Release();
                DeepSpaceShipsBuffer = null;
            }
            if (DeepSpaceArgBuffer != null) {
                DeepSpaceArgBuffer.Release();
                DeepSpaceArgBuffer = null;
            }
            if (DeepSpaceShipMats != null) {
                for (int i = 0; i < DeepSpaceShipMats.Length; i++) {
                    UnityEngine.Object.Destroy(DeepSpaceShipMats[i]);
                }
                DeepSpaceShipMats = null;
            }
            //LogError($"scpppppppppppppppppppppppp LogisticShipRenderer_Destroy_Patch");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LogisticShipRenderer), nameof(LogisticShipRenderer.Update))]
        public static bool LogisticShipRenderer_Update_Patch(LogisticShipRenderer __instance)
        {
            __instance.shipCount = 0;
            DeepSpaceShipCount = 0;
            if (__instance.transport == null) {
                return false;
            }

            for (int i = 1; i < __instance.transport.stationCursor; i++) {
                StationComponent stationComponent = __instance.transport.stationPool[i];
                if (stationComponent != null && stationComponent.gid == i) {
                    int renderCnt = stationComponent.renderShipCount;
                    if (renderCnt <= 0) continue;
                    // 判断分支：按stationComponent的某个标记，选择A原版 / B新飞船
                    bool useDeepSpaceShip = stationComponent.energyMax == 12000000000;

                    if (!useDeepSpaceShip) {
                        int num = __instance.shipCount + renderCnt;
                        if (num > 0) {
                            while (__instance.capacity < num) {
                                __instance.Expand2x();
                            }
                            Array.Copy(stationComponent.shipRenderers, 0, __instance.shipsArr, __instance.shipCount, renderCnt);
                            __instance.shipCount = num;
                        }
                    } else {
                        // ---- B新飞船组，拷贝到B独立数组，扩容B
                        int numB = DeepSpaceShipCount + renderCnt;
                        if (numB > 0) {
                            while (DeepSpaceCapacity < numB) {
                                DeepSpaceExpand2x();
                            }

                            Array.Copy(stationComponent.shipRenderers, 0, DeepSpaceShipsArr, DeepSpaceShipCount, renderCnt);
                            DeepSpaceShipCount = numB;
                        }
                    }
                }
            }

            if (__instance.shipsBuffer != null) {
                __instance.shipsBuffer.SetData(__instance.shipsArr, 0, 0, __instance.shipCount);
            }

            if (DeepSpaceShipsBuffer != null && DeepSpaceShipCount > 0) {
                DeepSpaceShipsBuffer.SetData(DeepSpaceShipsArr, 0, 0, DeepSpaceShipCount);
            }
            return false;
        }

        static bool wasVisibleLastFrame;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipRenderer), nameof(LogisticShipRenderer.Draw))]
        public static void LogisticShipRenderer_Draw_Patch(LogisticShipRenderer __instance)
        {
            if (DeepSpaceShipCount <= 0) {
                return;
            }

            argArr = new uint[25];
            for (int i = 0; i < DeepSpaceShipMats.Length; i++) {
                argArr[i * 5] = DeepSpaceShipMesh.GetIndexCount(i);
                argArr[1 + i * 5] = (uint)DeepSpaceShipCount;
                argArr[2 + i * 5] = DeepSpaceShipMesh.GetIndexStart(i);
                argArr[3 + i * 5] = DeepSpaceShipMesh.GetBaseVertex(i);
                argArr[4 + i * 5] = 0U;
            }
            DeepSpaceArgBuffer.SetData(argArr);
            for (int j = 0; j < DeepSpaceShipMats.Length; j++) {
                DeepSpaceShipMats[j].SetBuffer("_ShipBuffer", DeepSpaceShipsBuffer);
                Graphics.DrawMeshInstancedIndirect(DeepSpaceShipMesh, j, DeepSpaceShipMats[j], new Bounds(Vector3.zero, new Vector3(200000f, 200000f, 200000f)), DeepSpaceArgBuffer, j * 5 * 4, null, (j == 0) ? ShadowCastingMode.On : ShadowCastingMode.Off, j == 0, 0, null, LightProbeUsage.BlendProbes);
            }
        }
    }
}
