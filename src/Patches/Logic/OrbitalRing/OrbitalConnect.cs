using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using WinAPI;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    public static class OrbitalConnect
    {
        static Vector3 startPos = new Vector3(0, 0, 0);
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Path), "ConfirmOperation")]
        public static void ConfirmOperation_Patch(BuildTool_Path __instance)
        {
            if (__instance.handItem.ID == ProtoID.I轨道连接组件 || __instance.handItem.ID == ProtoID.I粒子加速轨道 || __instance.handItem.ID == ProtoID.I星环电网组件) {
                if (__instance.controller.cmd.stage == 1) {
                    for (int i = 0; i < __instance.buildPreviews.Count; i++) {
                        BuildPreview preview = __instance.buildPreviews[i];

                        if (preview.item.ID == ProtoID.I轨道连接组件 || preview.item.ID == ProtoID.I粒子加速轨道 || preview.item.ID == ProtoID.I星环电网组件) {
                            startPos = preview.lpos;
                        }
                    }
                } else {
                    startPos = new Vector3(0, 0, 0);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        public static void BuildTool_Path_DeterminePreviews_PrePatch(BuildTool_Path __instance)
        {
            if (__instance.handItem.ID == ProtoID.I空轨) {
                if (__instance.altitude == 0) {
                    __instance.altitude = 17;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        public static void BuildTool_Path_DeterminePreviews_PostPatch(BuildTool_Path __instance)
        {
            if (__instance.handItem.ID == ProtoID.I轨道连接组件) {
                __instance.altitude = 48;
            } else if (__instance.handItem.ID == ProtoID.I粒子加速轨道 || __instance.handItem.ID == ProtoID.I星环电网组件) {
                __instance.altitude = 43;
            } else if (__instance.handItem.ID == ProtoID.I空轨) {
                if (__instance.altitude < 16) {
                    __instance.altitude = 16;
                }
            } else if (__instance.handItem.ID == 2001 || __instance.handItem.ID == 2002 || __instance.handItem.ID == 2003 || __instance.handItem.ID == ProtoID.I空轨) {
                if (__instance.altitude > 40) {
                    __instance.altitude = 40;
                }
            }

            int count = __instance.buildPreviews.Count;
            for (int i = 0; i < count; i++) {
                BuildPreview preview = __instance.buildPreviews[i];

                if (preview.item.ID == ProtoID.I轨道连接组件 || __instance.handItem.ID == ProtoID.I粒子加速轨道 || __instance.handItem.ID == ProtoID.I星环电网组件) {
                    Vector3 pos = new Vector3(0, 0, 0);
                    if (preview.item.ID == ProtoID.I轨道连接组件) {
                        pos = BeltShouldBeAdsorb(preview.lpos, startPos, 0, __instance.planet.radius == 100f);
                    } else if (preview.item.ID == ProtoID.I粒子加速轨道) {
                        pos = BeltShouldBeAdsorb(preview.lpos, startPos, 1, __instance.planet.radius == 100f);
                    } else if (preview.item.ID == ProtoID.I星环电网组件) {
                        pos = BeltShouldBeAdsorb(preview.lpos, startPos, 2, __instance.planet.radius == 100f);
                    }

                    preview.lpos = pos;

                    // 计算原向量长度
                    float originalMagnitude = preview.lpos.magnitude;
                    if (originalMagnitude == 0 || originalMagnitude - __instance.planet.realRadius > 40) {
                        continue; // 避免除以零
                    }
                    // 获取单位向量（原方向）
                    Vector3 normalized = preview.lpos.normalized;
                    // 计算新长度并返回结果
                    preview.lpos = normalized * (__instance.planet.realRadius + 0.2f + __instance.altitude * 1.3333333f);
                    //Debug.LogFormat("length {0}  altitude {1} ", __instance.planet.realRadius + 0.2f + __instance.altitude * 1.3333333f, __instance.altitude);
                    //preview.lrot2 *= Quaternion.AngleAxis(180, Vector3.right);

                }

            }
        }

        public static bool isOrbitalBelt(BuildPreview buildPreview2)
        {
            if (buildPreview2 == null) {
                return false;
            }
            int id = buildPreview2.item.ID;
            switch (id) {
                case ProtoID.I轨道连接组件:
                case ProtoID.I粒子加速轨道:
                case ProtoID.I星环电网组件:
                case ProtoID.I空轨:
                    return true;
            }
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        public static IEnumerable<CodeInstruction> BuildTool_BlueprintPaste_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            // 跳过蓝图轨道连接组件太高不造的判定
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Mul)
                );
            object IL_0ABA = matcher.Advance(3).Operand;
            object V_9 = matcher.Advance(1).Operand;

            matcher.Advance(-6).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, V_9),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt))),
                new CodeInstruction(OpCodes.Brtrue_S, IL_0ABA)
            );
            // ===============
            // 跳过蓝图轨道设施碰撞检测的判定
            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))));
            object V_78 = matcher.Advance(1).Operand; // prefabDesc2变量索引
            CodeMatcher matcher2 = matcher.Clone();
            matcher2.MatchForward(false, new CodeMatch(OpCodes.Brtrue));
            object IL_1A89 = matcher2.Operand; // prefabDesc2变量索引

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_78),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1A89)
            );

            matcher.MatchForward(false, new CodeMatch(OpCodes.Brfalse), new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))));
            object IL_1CE3 = matcher.Operand;
            object V_116 = matcher.Advance(4).Operand; // prefabDesc2变量索引


            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_9),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1CE3)
            );

            //matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_2), new CodeMatch(OpCodes.Call),
            //    new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Ldloc_S), new CodeMatch(OpCodes.Ldc_I4_0));
            //object IL_1E57 = matcher.Advance(5).Operand;


            //matcher.Advance(-2).InsertAndAdvance(
            //    new CodeInstruction(OpCodes.Ldloc_S, V_9),
            //    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt)
            //    )),
            //    new CodeInstruction(OpCodes.Brtrue_S, IL_1E57)
            //);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetData), nameof(PlanetData.realRadius))),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Bge_Un));
            object IL_354F = matcher.Advance(3).Operand;
            //object V_116 = matcher.Advance(4).Operand; // prefabDesc2变量索引


            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_116),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isOrbitalBelt)
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_354F)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        public static bool isCollideShouldIgnore(PrefabDesc prefabDesc2)
        {
            int modelindex = prefabDesc2.modelIndex;
            switch (modelindex) {
                case ProtoID.M轨道熔炼站:
                case ProtoID.M太空物流港:
                case ProtoID.M天枢座:
                case ProtoID.M太空船坞:
                case ProtoID.M深空物流港:
                case ProtoID.M轨道反物质堆基座:
                case ProtoID.M轨道观测站:
                case ProtoID.M星环对撞机:
                    return true;
            }
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Br), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Brfalse));
            object label = matcher.Advance(3).Operand;
            //matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            matcher.Advance(12).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            //matcher.Advance(10);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Add), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Callvirt));

            object label1 = matcher.Advance(11).Operand;

            matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label1));
            // 上面两个修改解除传送带的高度受科技限制，为了适配小塔刚解锁时17层接口就可以用和轨道连接组件

            // 下面修改是为了让轨道熔炼站等同步轨道设施不参与碰撞检测
            matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetPrefabDesc))),
                new CodeMatch(OpCodes.Stloc_S));
            object V_64 = matcher.Advance(1).Operand; // prefabDesc2变量索引
            //CodeMatcher matcher2 = matcher.Clone();
            //matcher2.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.GetObjectPose))));
            //object V_67 = matcher2.Advance(3).Operand; // prefabDesc2变量索引

            object IL_1001 = matcher.Advance(5).Operand; // IL_1001变量索引

            matcher.Advance(-4);
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_64),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(isCollideShouldIgnore)
                //new System.Type[] {
                //    typeof(PrefabDesc),
                //}
                )),
                //new CodeInstruction(OpCodes.Stloc_S, V_67),
                //new CodeInstruction(OpCodes.Ldloc_S, V_67),
                new CodeInstruction(OpCodes.Brtrue_S, IL_1001)
            );


            return matcher.InstructionEnumeration();
        }

        public static bool CheckBeltId(BuildTool_Path instance, ref int num2)
        {
            int modelIndex = instance.GetPrefabDesc(num2).modelIndex;
            if (instance.handItem.ID == ProtoID.I轨道连接组件) {
                if (modelIndex != ProtoID.M轨道连接组件) {
                    num2 = 0;
                    return true;
                }
            } else if (instance.handItem.ID == ProtoID.I粒子加速轨道) {
                if (modelIndex != ProtoID.M粒子加速轨道) {
                    num2 = 0;
                    return true;
                }
            } else if (instance.handItem.ID == ProtoID.I星环电网组件) {
                if (modelIndex != ProtoID.M星环电网组件) {
                    num2 = 0;
                    return true;
                }
            }
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.UpdateRaycast))]
        public static IEnumerable<CodeInstruction> BuildTool_Path_UpdateRaycast_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelema), new CodeMatch(OpCodes.Ldfld), new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Br));
            object IL_0451 = matcher.Advance(-14).Operand;
            object V_10 = matcher.Advance(16).Operand;
            //matcher.Advance(-5).InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, label));

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca_S, V_10),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrbitalConnect), nameof(CheckBeltId),
                    new System.Type[] {
                        typeof(BuildTool_Path),
                        typeof(int).MakeByRefType(),
                    }
                )),
                new CodeInstruction(OpCodes.Brtrue_S, IL_0451)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(BuildTool_Path), "UpdateRaycast")]
        //public static void UpdateRaycastPatch(BuildTool_Path __instance)
        //{
        //    bool flag = false;
        //    if (__instance.castObject == false) {
        //        return;
        //    }
        //    if (__instance.handItem.ID == ProtoID.I轨道连接组件) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M轨道连接组件) {
        //            flag = true;
        //        }
        //    } else if (__instance.handItem.ID == ProtoID.I粒子加速轨道) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M粒子加速轨道) {
        //            flag = true;
        //        }
        //    } else if (__instance.handItem.ID == ProtoID.I星环电网组件) {
        //        if (__instance.GetPrefabDesc(__instance.castObjectId).modelIndex != ProtoID.M星环电网组件) {
        //            flag = true;
        //        }
        //    }
        //    if (flag) {
        //        __instance.castObject = false;
        //        __instance.castObjectId = 0;
        //        __instance.castObjectPos = Vector3.zero;
        //    }
        //}

        //[HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        //[HarmonyPostfix]
        //public static void SetStationStorage_Patch(ref PlanetTransport __instance)
        //{
        //    OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
        //    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
        //    LogError($" 0 ring not complete inside {planetOrbitalRingData.Rings[0].isInsideRingComplete} outside {planetOrbitalRingData.Rings[0].isOutsideRingComplete}");
        //    LogError($" 0 ring not complete low inside {planetOrbitalRingData.Rings[0].isLowInsideRingComplete} outside {planetOrbitalRingData.Rings[0].isLowOutsideRingComplete}");
        //    if (__instance.planet.radius > 100f) {
        //        LogError($" 1 ring not complete inside {planetOrbitalRingData.Rings[1].isInsideRingComplete} outside {planetOrbitalRingData.Rings[1].isOutsideRingComplete}");
        //        LogError($" 1 ring not complete low inside {planetOrbitalRingData.Rings[1].isLowInsideRingComplete} outside {planetOrbitalRingData.Rings[1].isLowOutsideRingComplete}");
        //    }
        //    //planetOrbitalRingData.Rings[0].CheckRingComplete(true);
        //    //planetOrbitalRingData.Rings[1].CheckRingComplete(true);
        //}



        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.BuildFinally))]
        [HarmonyPrefix]
        public static void BuildFinallyPrePatch(ref PlanetFactory __instance, int prebuildId)
        {
            if (prebuildId != 0) {
                PrebuildData prebuildData = __instance.prebuildPool[prebuildId];
                if (prebuildData.id == prebuildId) {
                    if (prebuildData.protoId == ProtoID.I轨道连接组件 || prebuildData.protoId == ProtoID.I粒子加速轨道 || prebuildData.protoId == ProtoID.I星环电网组件) {
                        //LogError($"BuildFinallyPostPatch");
                        Vector3 pos = prebuildData.pos;
                        (int positionIndex, int ringBeltIndex, int ringIndex) = CalculateRingPosMark(pos, __instance.planet.radius == 100f);
                        OrbitalStationManager.Instance.AddPlanetId(__instance.planet.id, __instance.planet.radius == 100f);
                        var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                        if (prebuildData.protoId == ProtoID.I轨道连接组件) {
                            planetOrbitalRingData.Rings[ringIndex].AddRing(positionIndex, ringBeltIndex, false);
                        } else if (prebuildData.protoId == ProtoID.I粒子加速轨道 || prebuildData.protoId == ProtoID.I星环电网组件) {
                            planetOrbitalRingData.Rings[ringIndex].AddRing(positionIndex, ringBeltIndex, true);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.DismantleFinally))]
        [HarmonyPrefix]
        public static void DismantleFinallyPatch(PlanetFactory __instance, int objId, ref int protoId)
        {
            if (objId > 0) {
                if (protoId == ProtoID.I轨道连接组件 || protoId == ProtoID.I粒子加速轨道) {
                    Vector3 thisPos = __instance.entityPool[objId].pos;
                    (int positionIndex, int ringBeltIndex, int ringIndex) = CalculateRingPosMark(thisPos, __instance.planet.radius == 100f);
                    var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
                    if (protoId == ProtoID.I轨道连接组件) {
                        planetOrbitalRingData.Rings[ringIndex].DelRing(positionIndex, ringBeltIndex, false);
                    } else if (protoId == ProtoID.I粒子加速轨道) {
                        planetOrbitalRingData.Rings[ringIndex].DelRing(positionIndex, ringBeltIndex, true);
                    }
                }
            }
        }

        // 范围拆除时过滤轨道连接组件，粒子加速轨道，星环电网组件
        [HarmonyPatch(typeof(BuildTool_Dismantle), nameof(BuildTool_Dismantle.DeterminePreviews))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BuildTool_Dismantle_DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldelem_I4), new CodeMatch(OpCodes.Call, AccessTools.Field(typeof(BuildTool), nameof(BuildTool.GetItemProto))));
            object V_14 = matcher.Advance(2).Operand;

            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PrefabDesc), nameof(PrefabDesc.isStation))));
            object IL_06C2 = matcher.Advance(1).Operand;

            matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6515),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6516),
                new CodeInstruction(OpCodes.Beq, IL_06C2),
                new CodeInstruction(OpCodes.Ldloc_S, V_14),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemProto), nameof(ItemProto.ID))),
                new CodeInstruction(OpCodes.Ldc_I4, 6517),
                new CodeInstruction(OpCodes.Beq, IL_06C2)
            );
            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }
    }
}
