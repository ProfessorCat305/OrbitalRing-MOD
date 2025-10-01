using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ProjectOrbitalRing.Patches.UI {
    public static class IconSetPatches {
        // [HarmonyPatch(typeof(IconSet), nameof(IconSet.Create))]
        // [HarmonyTranspiler]
        // public static IEnumerable<CodeInstruction> IconSet_Create_Transpiler(IEnumerable<CodeInstruction> instructions)
        // {
        //     var matcher = new CodeMatcher(instructions);
        //
        //     matcher.MatchForward(false,
        //         new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TechProto), nameof(TechProto.iconSprite))));
        //
        //     object label = matcher.InstructionAt(5).operand;
        //
        //     object index_V_23 = matcher.Advance(-2).Operand;
        //     object dataArray3 = matcher.Advance(-1).Operand;
        //
        //     matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, dataArray3), new CodeInstruction(OpCodes.Ldloc_S, index_V_23),
        //         new CodeInstruction(OpCodes.Ldelem_Ref),
        //         new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IconSetPatches), nameof(IconSet_Create_Patch))),
        //         new CodeInstruction(OpCodes.Brtrue_S, label));
        //
        //     return matcher.InstructionEnumeration();
        // }
        //
        // public static bool IconSet_Create_Patch(TechProto proto) => proto.ID < 2000;

        /// <summary>
        /// 升级科技（也就是ID>2000的科技）仅添加最后一个图标到图标集。
        /// </summary>
        /// <details>
        /// 图标集指传送带的可选显示图标，不影响游戏其他显示。
        /// </details>
        [HarmonyPatch(typeof(IconSet), nameof(IconSet.Create))]
        [HarmonyPrefix]
        public static bool IconSet_Create_Prefix(ref IconSet __instance) {
            if (__instance.loaded)
                return false;
            HighStopwatch highStopwatch = new HighStopwatch();
            highStopwatch.Begin();
            __instance.texture = new Texture2D(2000, 2000, TextureFormat.ARGB32, true);
            __instance.itemIconIndex = new uint[12000];
            __instance.veinIconIndex = new uint[256 /*0x0100*/];
            __instance.recipeIconIndex = new uint[12000];
            __instance.techIconIndex = new uint[12000];
            __instance.signalIconIndex = new uint[60000];
            __instance.itemDescArr = new float[25000];
            __instance.itemIconIndexBuffer = new ComputeBuffer(12000, 4, ComputeBufferType.Default);
            __instance.veinIconIndexBuffer = new ComputeBuffer(256 /*0x0100*/, 4, ComputeBufferType.Default);
            __instance.recipeIconIndexBuffer = new ComputeBuffer(12000, 4, ComputeBufferType.Default);
            __instance.techIconIndexBuffer = new ComputeBuffer(12000, 4, ComputeBufferType.Default);
            __instance.signalIconIndexBuffer = new ComputeBuffer(60000, 4, ComputeBufferType.Default);
            __instance.itemIconDescBuffer = new ComputeBuffer(25000, 4, ComputeBufferType.Default);
            __instance.spriteIndexMap = new Dictionary<Sprite, uint>();
            __instance.texture.SetPixels(new Color[4000000]);
            __instance.texture.Apply();
            uint num1 = 0;
            ItemProto[] dataArray1 = LDB.items.dataArray;
            int length1 = dataArray1.Length;
            for (int index = 0; index < length1; ++index) {
                uint num2 = 0;
                Sprite iconSprite = dataArray1[index].iconSprite;
                if (iconSprite != null) {
                    if (__instance.spriteIndexMap.ContainsKey(iconSprite)) {
                        num2 = __instance.spriteIndexMap[iconSprite];
                    } else {
                        num2 = ++num1;
                        if (num2 >= 625U) {
                            num2 = 0U;
                            Debug.LogWarning("图标图集空间不足！");
                        } else {
                            int num3 = (int)num2 % 25;
                            int num4 = (int)num2 / 25;
                            Graphics.CopyTexture(dataArray1[index].iconSprite.texture, 0, 0, 0, 0, 80/*0x50*/,
                                80/*0x50*/, __instance.texture, 0, 0, num3 * 80/*0x50*/, num4 * 80/*0x50*/);
                            //LogDebug($"添加图标到图集，idx={num2}，物品{dataArray1[index].name}({dataArray1[index].ID})");
                        }
                        __instance.spriteIndexMap[iconSprite] = num2;
                    }
                }
                __instance.itemIconIndex[dataArray1[index].ID] = num2;
                __instance.signalIconIndex[dataArray1[index].ID] = num2;
            }
            __instance.itemIconIndexBuffer.SetData(__instance.itemIconIndex);
            VeinProto[] dataArray2 = LDB.veins.dataArray;
            int length2 = dataArray2.Length;
            for (int index = 0; index < length2; ++index) {
                uint num5 = 0;
                Sprite iconSprite = dataArray2[index].iconSprite;
                if (iconSprite != null) {
                    if (__instance.spriteIndexMap.ContainsKey(iconSprite)) {
                        num5 = __instance.spriteIndexMap[iconSprite];
                    } else {
                        num5 = ++num1;
                        if (num5 >= 625U) {
                            num5 = 0U;
                            Debug.LogWarning("图标图集空间不足！");
                        } else {
                            int num6 = (int)num5 % 25;
                            int num7 = (int)num5 / 25;
                            try {
                                Graphics.CopyTexture(dataArray2[index].iconSprite80px.texture, 0, 0, 0, 0, 80/*0x50*/,
                                    80/*0x50*/, __instance.texture, 0, 0, num6 * 80/*0x50*/, num7 * 80/*0x50*/);
                            }
                            catch (Exception ex) {
                                Debug.LogWarning($"拷贝 vein {dataArray2[index].name} 图标到图集时出错：{ex.Message}");
                            }
                        }
                        __instance.spriteIndexMap[iconSprite] = num5;
                    }
                }
                __instance.veinIconIndex[dataArray2[index].ID] = num5;
                __instance.signalIconIndex[dataArray2[index].ID + 12000] = num5;
            }
            __instance.veinIconIndexBuffer.SetData(__instance.veinIconIndex);
            RecipeProto[] dataArray3 = LDB.recipes.dataArray;
            int length3 = dataArray3.Length;
            for (int index = 0; index < length3; ++index) {
                uint num8 = 0;
                Sprite iconSprite = dataArray3[index].iconSprite;
                if (iconSprite != null) {
                    if (__instance.spriteIndexMap.ContainsKey(iconSprite)) {
                        num8 = __instance.spriteIndexMap[iconSprite];
                    } else {
                        num8 = ++num1;
                        if (num8 >= 625U) {
                            num8 = 0U;
                            Debug.LogWarning("图标图集空间不足！");
                        } else {
                            int num9 = (int)num8 % 25;
                            int num10 = (int)num8 / 25;
                            Graphics.CopyTexture(dataArray3[index].iconSprite.texture, 0, 0, 0, 0, 80/*0x50*/,
                                80/*0x50*/, __instance.texture, 0, 0, num9 * 80/*0x50*/, num10 * 80/*0x50*/);
                        }
                        __instance.spriteIndexMap[iconSprite] = num8;
                    }
                }
                __instance.recipeIconIndex[dataArray3[index].ID] = num8;
                __instance.signalIconIndex[dataArray3[index].ID + 20000] = num8;
            }
            __instance.recipeIconIndexBuffer.SetData(__instance.recipeIconIndex);
            TechProto[] dataArray4 = LDB.techs.dataArray;
            int length4 = dataArray4.Length;
            int lastTechId = 0;
            List<string> iconPathSet = new List<string>();
            for (int index = length4 - 1; index >= 0; --index) {
                int techId = dataArray4[index].ID;
                uint num11 = 0;
                Sprite iconSprite = dataArray4[index].iconSprite;
                if (iconSprite != null) {
                    if (__instance.spriteIndexMap.ContainsKey(iconSprite)) {
                        num11 = __instance.spriteIndexMap[iconSprite];
                    } else {
                        if (techId > 2000 && techId == lastTechId - 1) {
                            //LogInfo($"添加图标到图集，跳过无限科技{dataArray4[index].name}({dataArray4[index].ID})");
                            lastTechId = techId;
                            continue;
                        }
                        lastTechId = techId;
                        if (iconPathSet.Contains(dataArray4[index].IconPath)) {
                            // LogInfo($"添加图标到图集，跳过相同图标科技{dataArray4[index].name}({dataArray4[index].ID})");
                            continue;
                        }
                        iconPathSet.Add(dataArray4[index].IconPath);
                        num11 = ++num1;
                        // Utils.Utils.LogInfo($"添加图标到图集，num11={num11}，科技{dataArray4[index].name}({dataArray4[index].ID})，IconPath:{dataArray4[index].IconPath}");
                        if (num11 >= 625U) {
                            num11 = 0U;
                            Debug.LogWarning("图标图集空间不足！");
                        } else {
                            int num12 = (int)num11 % 25;
                            int num13 = (int)num11 / 25;
                            Graphics.CopyTexture(dataArray4[index].iconSprite.texture, 0, 0, 0, 0, 80/*0x50*/,
                                80/*0x50*/, __instance.texture, 0, 0, num12 * 80/*0x50*/, num13 * 80/*0x50*/);
                        }
                        __instance.spriteIndexMap[iconSprite] = num11;
                    }
                }
                __instance.techIconIndex[dataArray4[index].ID] = num11;
                __instance.signalIconIndex[dataArray4[index].ID + 40000] = num11;
            }
            __instance.techIconIndexBuffer.SetData(__instance.techIconIndex);
            SignalProto[] dataArray5 = LDB.signals.dataArray;
            int length5 = dataArray5.Length;
            for (int index = 0; index < length5; ++index) {
                uint num14 = 0;
                Sprite iconSprite = dataArray5[index].iconSprite;
                if (iconSprite != null) {
                    if (__instance.spriteIndexMap.ContainsKey(iconSprite)) {
                        num14 = __instance.spriteIndexMap[iconSprite];
                    } else {
                        num14 = ++num1;
                        if (num14 >= 625U) {
                            num14 = 0U;
                            Debug.LogWarning("图标图集空间不足！");
                        } else {
                            int num15 = (int)num14 % 25;
                            int num16 = (int)num14 / 25;
                            Graphics.CopyTexture(dataArray5[index].iconSprite.texture, 0, 0, 0, 0, 80/*0x50*/,
                                80/*0x50*/, __instance.texture, 0, 0, num15 * 80/*0x50*/, num16 * 80/*0x50*/);
                        }
                        __instance.spriteIndexMap[iconSprite] = num14;
                    }
                }
                __instance.signalIconIndex[dataArray5[index].ID] = num14;
            }
            __instance.signalIconIndexBuffer.SetData(__instance.signalIconIndex);
            __instance.texture.Apply(true);
            Array.Clear(__instance.itemDescArr, 0, __instance.itemDescArr.Length);
            TextAsset textAsset = Resources.Load<TextAsset>(IconSet.iconDescPath);
            if (textAsset != null) {
                using (MemoryStream input = new MemoryStream(textAsset.bytes)) {
                    using (BinaryReader binaryReader = new BinaryReader(input)) {
                        binaryReader.ReadInt32();
                        int num17 = binaryReader.ReadInt32();
                        for (int index1 = 0; index1 < num17; ++index1) {
                            binaryReader.ReadInt32();
                            int index2 = binaryReader.ReadInt32();
                            uint num18 = index2 >= 12000 || index2 <= 0 ? 0U : __instance.itemIconIndex[index2];
                            for (int index3 = 0; index3 < 40; ++index3) {
                                float num19 = binaryReader.ReadSingle();
                                if (num18 > 0U)
                                    __instance.itemDescArr[num18 * 40U + index3] = num19;
                            }
                        }
                    }
                }
            }
            __instance.itemIconDescBuffer.SetData(__instance.itemDescArr);
            __instance.loaded = true;
            Debug.Log($"添加图标到图集，共计添加{num1}（需要<=625）");
            Debug.Log($"Icon set generated. Time cost: {highStopwatch.duration:0.000} s");
            return false;
        }
    }
}
