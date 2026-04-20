using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.UI
{
    internal static class UIAssemblerWindowPatch
    {
        private static GameObject _obj;

        private static GameObject _iconObj;

        private static Text _text;

        private static Text _count;

        private static Image _icon;

        private static UIButton _uiButton;

        private static Button _button;

        private static Sprite _tagNotSelectedSprite;

        public static bool ShouldModuleButtonActive(ERecipeType recipeType, int recipeId, int speed)
        {
            if (!GameMain.history.TechUnlocked(1311)) {
                return false;
            }
            if (speed >= 40000) {
                return false;
            }
            switch (recipeType) {
                case ERecipeType.Assemble:
                case (ERecipeType)10:
                case (ERecipeType)12:
                    return true;
                default:
                    break;
            }
            switch (recipeId) {
                case 775:
                case 778:
                case 784:
                case 847:
                case 848:
                    return true;
                default:
                    break;
            }
            return false;
        }

        //[HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.Import))]
        //[HarmonyPrefix]
        //public static bool AssemblerComponent_Import_Prefix(AssemblerComponent __instance, BinaryReader r)
        //{
        //    int num = r.ReadInt32();
        //    bool flag = num >= 1;
        //    __instance.id = r.ReadInt32();
        //    __instance.entityId = r.ReadInt32();
        //    __instance.pcId = r.ReadInt32();
        //    __instance.replicating = r.ReadBoolean();
        //    if (num < 6) {
        //        r.ReadBoolean();
        //    }
        //    __instance.speed = r.ReadInt32();
        //    __instance.time = r.ReadInt32();
        //    if (num >= 2) {
        //        __instance.speedOverride = r.ReadInt32();
        //        __instance.extraTime = r.ReadInt32();
        //        __instance.extraSpeed = r.ReadInt32();
        //        __instance.extraPowerRatio = r.ReadInt32();
        //        if (num < 6) {
        //            r.ReadBoolean();
        //        }
        //        if (num >= 3) {
        //            __instance.forceAccMode = r.ReadBoolean();
        //        } else {
        //            __instance.forceAccMode = false;
        //        }
        //        if (num >= 4) {
        //            __instance.incUsed = r.ReadBoolean();
        //        } else {
        //            __instance.incUsed = false;
        //        }
        //    } else {
        //        __instance.speedOverride = __instance.speed;
        //        __instance.extraTime = 0;
        //        __instance.extraSpeed = 0;
        //        __instance.extraPowerRatio = 0;
        //        __instance.forceAccMode = false;
        //        __instance.incUsed = false;
        //    }
        //    if (num < 4 && __instance.incServed != null) {
        //        for (int i = 0; i < __instance.incServed.Length; i++) {
        //            if (__instance.incServed[i] > 0) {
        //                __instance.incUsed = true;
        //                break;
        //            }
        //        }
        //    }
        //    if (num < 5) {
        //        __instance.cycleCount = 0;
        //        __instance.extraCycleCount = 0;
        //    } else {
        //        __instance.cycleCount = r.ReadInt32();
        //        __instance.extraCycleCount = r.ReadInt32();
        //    }
        //    if (flag) {
        //        __instance.recipeId = (int)r.ReadInt16();
        //    } else {
        //        __instance.recipeId = r.ReadInt32();
        //    }
        //    if (__instance.recipeId > 0) {
        //        RecipeProto recipeProto = LDB.recipes.Select(__instance.recipeId);
        //        if (flag) {
        //            __instance.recipeType = (ERecipeType)r.ReadByte();
        //            int num2;
        //            int[] array;
        //            if (num < 6) {
        //                r.ReadInt32();
        //                if (num >= 2) {
        //                    r.ReadInt32();
        //                }
        //                num2 = (int)r.ReadByte();
        //                array = new int[num2];
        //                for (int j = 0; j < num2; j++) {
        //                    array[j] = (int)r.ReadInt16();
        //                }
        //                num2 = (int)r.ReadByte();
        //                int[] array2 = new int[num2];
        //                for (int k = 0; k < num2; k++) {
        //                    array2[k] = (int)r.ReadInt16();
        //                }
        //            } else {
        //                num2 = (int)r.ReadByte();
        //                array = new int[num2];
        //                for (int l = 0; l < num2; l++) {
        //                    array[l] = (int)r.ReadInt16();
        //                }
        //            }
        //            num2 = (int)r.ReadByte();
        //            __instance.served = new int[num2];
        //            for (int m = 0; m < num2; m++) {
        //                __instance.served[m] = r.ReadInt32();
        //            }
        //            if (num >= 2) {
        //                num2 = (int)r.ReadByte();
        //                __instance.incServed = new int[num2];
        //                for (int n = 0; n < num2; n++) {
        //                    __instance.incServed[n] = r.ReadInt32();
        //                }
        //            } else {
        //                __instance.incServed = new int[__instance.served.Length];
        //                for (int num3 = 0; num3 < __instance.served.Length; num3++) {
        //                    __instance.incServed[num3] = 0;
        //                }
        //            }
        //            num2 = (int)r.ReadByte();
        //            __instance.needs = new int[num2];
        //            for (int num4 = 0; num4 < num2; num4++) {
        //                __instance.needs[num4] = (int)r.ReadInt16();
        //            }
        //            int[] array3;
        //            if (num < 6) {
        //                num2 = (int)r.ReadByte();
        //                array3 = new int[num2];
        //                for (int num5 = 0; num5 < num2; num5++) {
        //                    array3[num5] = (int)r.ReadInt16();
        //                }
        //                num2 = (int)r.ReadByte();
        //                int[] array4 = new int[num2];
        //                for (int num6 = 0; num6 < num2; num6++) {
        //                    array4[num6] = (int)r.ReadInt16();
        //                }
        //            } else {
        //                num2 = (int)r.ReadByte();
        //                array3 = new int[num2];
        //                for (int num7 = 0; num7 < num2; num7++) {
        //                    array3[num7] = (int)r.ReadInt16();
        //                }
        //            }
        //            num2 = (int)r.ReadByte();
        //            __instance.produced = new int[num2];
        //            for (int num8 = 0; num8 < num2; num8++) {
        //                __instance.produced[num8] = r.ReadInt32();
        //            }
        //            if (recipeProto != null) {
        //                __instance.recipeExecuteData = RecipeProto.recipeExecuteData[recipeProto.ID];
        //                int num9 = __instance.recipeExecuteData.requires.Length;
        //                if (array.Length != __instance.recipeExecuteData.requires.Length) {
        //                    LogError($"recipeProto.ID {recipeProto.ID} array.Length {array.Length} requires.Length {__instance.recipeExecuteData.requires.Length} !!!!!!!!!!!!!!!!!!!!!!!!!");

        //                }
        //                for (int num10 = 0; num10 < array.Length; num10++) {
        //                    if (num10 > num9 || array[num10] != __instance.recipeExecuteData.requires[num10]) {
        //                        __instance.served[num10] = (__instance.incServed[num10] = 0);
        //                    }
        //                }
        //                num9 = __instance.recipeExecuteData.products.Length;
        //                for (int num11 = 0; num11 < array3.Length; num11++) {
        //                    if (num11 > num9 || array3[num11] != __instance.recipeExecuteData.products[num11]) {
        //                        __instance.produced[num11] = 0;
        //                    }
        //                }
        //                return false;
        //            }
        //        } else {
        //            __instance.recipeType = (ERecipeType)r.ReadInt32();
        //            int num12;
        //            if (num < 6) {
        //                num12 = (int)r.ReadByte();
        //                int[] array = new int[num12];
        //                for (int num13 = 0; num13 < num12; num13++) {
        //                    array[num13] = (int)r.ReadInt16();
        //                }
        //                num12 = (int)r.ReadByte();
        //                int[] array5 = new int[num12];
        //                for (int num14 = 0; num14 < num12; num14++) {
        //                    array5[num14] = (int)r.ReadInt16();
        //                }
        //            }
        //            num12 = r.ReadInt32();
        //            __instance.served = new int[num12];
        //            for (int num15 = 0; num15 < num12; num15++) {
        //                __instance.served[num15] = r.ReadInt32();
        //            }
        //            if (num >= 2) {
        //                num12 = (int)r.ReadByte();
        //                __instance.incServed = new int[num12];
        //                for (int num16 = 0; num16 < num12; num16++) {
        //                    __instance.incServed[num16] = r.ReadInt32();
        //                }
        //            } else {
        //                __instance.incServed = new int[__instance.served.Length];
        //                for (int num17 = 0; num17 < __instance.served.Length; num17++) {
        //                    __instance.incServed[num17] = 0;
        //                }
        //            }
        //            num12 = r.ReadInt32();
        //            __instance.needs = new int[num12];
        //            for (int num18 = 0; num18 < num12; num18++) {
        //                __instance.needs[num18] = r.ReadInt32();
        //            }
        //            if (num < 6) {
        //                num12 = (int)r.ReadByte();
        //                int[] array3 = new int[num12];
        //                for (int num19 = 0; num19 < num12; num19++) {
        //                    array3[num19] = (int)r.ReadInt16();
        //                }
        //                num12 = (int)r.ReadByte();
        //                int[] array6 = new int[num12];
        //                for (int num20 = 0; num20 < num12; num20++) {
        //                    array6[num20] = (int)r.ReadInt16();
        //                }
        //                if (recipeProto != null) {
        //                    __instance.recipeExecuteData = RecipeProto.recipeExecuteData[recipeProto.ID];
        //                }
        //            }
        //            num12 = r.ReadInt32();
        //            __instance.produced = new int[num12];
        //            for (int num21 = 0; num21 < num12; num21++) {
        //                __instance.produced[num21] = r.ReadInt32();
        //            }
        //        }
        //    }
        //    return false;
        //}


        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        [HarmonyPostfix]
        [HarmonyPriority(0)]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            bool flag = UIAssemblerWindowPatch._obj == null;
            if (flag) {
                Transform transform = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce").transform;
                UIAssemblerWindowPatch._obj = new GameObject {
                    name = "lithography"
                };
                UIAssemblerWindowPatch._obj.transform.SetParent(transform, false);
                UIAssemblerWindowPatch._obj.AddComponent<Image>();
                Image component = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel").GetComponent<Image>();
                Image component2 = UIAssemblerWindowPatch._obj.GetComponent<Image>();
                component2.sprite = component.sprite;
                component2.color = component.color;
                UIAssemblerWindowPatch._obj.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 68f);
                UIAssemblerWindowPatch._obj.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                UIAssemblerWindowPatch._obj.transform.localPosition = new Vector3(15f, -120f, 0f);
                Transform transform2 = UIAssemblerWindowPatch._obj.transform;
                GameObject original = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/catalytic/eff-text");
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.localPosition = new Vector3(0f, -48f, 0f);
                UIAssemblerWindowPatch._text = gameObject.transform.GetComponent<Text>();
                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Localizer>());
                UIAssemblerWindowPatch._text.text = "伺服器".TranslateFromJson();
                UIAssemblerWindowPatch._text.fontSize = 16;
                original = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel/fuel-icon");
                UIAssemblerWindowPatch._iconObj = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
                UIAssemblerWindowPatch._iconObj.transform.localScale = Vector3.one;
                UIAssemblerWindowPatch._icon = UIAssemblerWindowPatch._iconObj.GetComponent<Image>();
                _tagNotSelectedSprite = UIAssemblerWindowPatch._icon.sprite;
                //UIAssemblerWindowPatch._count = UIAssemblerWindowPatch._iconObj.transform.Find("cnt-text").GetComponent<Text>();
                //UIAssemblerWindowPatch._count.text = "0";
                //UIAssemblerWindowPatch._count.fontSize = 16;
                GameObject original2 = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel/button");
                GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(original2, transform2);
                gameObject2.transform.localScale = Vector3.one;
                UIAssemblerWindowPatch._uiButton = gameObject2.GetComponent<UIButton>();
                UIAssemblerWindowPatch._uiButton.tips.itemCount = 0;
                UIAssemblerWindowPatch._uiButton.tips.itemInc = 0;
                UIAssemblerWindowPatch._uiButton.tips.type = UIButton.ItemTipType.IgnoreIncPoint;
                UIAssemblerWindowPatch._button = gameObject2.GetComponent<Button>();
                UIAssemblerWindowPatch._button.onClick.RemoveAllListeners();
                UIAssemblerWindowPatch._button.onClick.AddListener(new UnityAction(UIAssemblerWindowPatch.OnLithographyIconClick));
            }
        }

        // Token: 0x06000094 RID: 148 RVA: 0x00006544 File Offset: 0x00004744
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAssemblerWindow), "OnServingBoxChange")]
        public static void UIAssemblerWindow_OnServingBoxChange_Postfix(ref UIAssemblerWindow __instance)
        {
            bool flag = __instance.assemblerId == 0 || __instance.factorySystem == null;
            if (flag) {
                UIAssemblerWindowPatch._obj.SetActive(false);
            } else {
                AssemblerComponent assemblerComponent = __instance.factorySystem.assemblerPool[__instance.assemblerId];
                bool flag2 = assemblerComponent.id != __instance.assemblerId;
                if (flag2) {
                    UIAssemblerWindowPatch._obj.SetActive(false);

                } else {
                    bool flag3 = ShouldModuleButtonActive(assemblerComponent.recipeType, assemblerComponent.recipeId, assemblerComponent.speed);
                    if (flag3) {
                        UIAssemblerWindowPatch.ChangeAssemblerModuleData(__instance, assemblerComponent);
                    } else {
                        UIAssemblerWindowPatch._uiButton.tips.itemId = 0;
                    }
                    UIAssemblerWindowPatch._iconObj.SetActive(flag3);
                    UIAssemblerWindowPatch._obj.SetActive(flag3);
                }
            }
        }

        // Token: 0x06000095 RID: 149 RVA: 0x00006604 File Offset: 0x00004804
        public static void ChangeAssemblerModuleData(UIAssemblerWindow __instance, AssemblerComponent assemblerComponent)
        {
            LogError($"planet {__instance.factorySystem.planet.id} assemblerId {__instance.assemblerId}");
            AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(__instance.factorySystem, __instance.assemblerId);
            int moduleId = AssemblerModulePatches.GetModuleId(assemblerComponent.recipeId);
            bool flag = (AssemblerModuleData.ItemId == moduleId || moduleId == 7617 && AssemblerModuleData.ItemId == 7618);
            if (!flag) {
                bool flag2 = AssemblerModuleData.ItemCount != 0;
                if (flag2) {
                    AssemblerModulePatches.SetEmpty(__instance.factorySystem, __instance.assemblerId, true);
                }
                AssemblerModuleData.ItemId = 0;
                AssemblerModuleData.ItemCount = 0;
                AssemblerModuleData.ItemInc = 0;
                AssemblerModuleData.NeedCount = 1;
                AssemblerModulePatches.SetAssemblerModuleData(__instance.factorySystem, __instance.assemblerId, AssemblerModuleData);
            }
            ItemProto itemProto = LDB.items.Select(moduleId);
            UIAssemblerWindowPatch._icon.sprite = (AssemblerModuleData.ItemCount > 0) ? itemProto._iconSprite : _tagNotSelectedSprite;
            UIAssemblerWindowPatch._icon.enabled = (AssemblerModuleData.ItemCount > 0);
            UIAssemblerWindowPatch._text.text = (moduleId == 7616 || AssemblerModuleData.ItemCount > 0) ? itemProto.name : "伺服器".TranslateFromJson();
            UIAssemblerWindowPatch._uiButton.tips.itemId = (AssemblerModuleData.ItemCount > 0) ? AssemblerModuleData.ItemId : 0;
            //UIAssemblerWindowPatch._count.text = AssemblerModuleData.ItemCount.ToString();
            //UIAssemblerWindowPatch._count.color = ((AssemblerModuleData.NeedCount == AssemblerModuleData.ItemCount) ? __instance.workNormalColor : __instance.workStoppedColor);
        }

        // Token: 0x06000096 RID: 150 RVA: 0x00006760 File Offset: 0x00004960
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAssemblerWindow), "_OnUpdate")]
        public static void UIAssemblerWindow_OnUpdate_Postfix(ref UIAssemblerWindow __instance)
        {
            bool activeSelf = UIAssemblerWindowPatch._obj.activeSelf;
            if (activeSelf) {
                bool flag = __instance.assemblerId == 0 || __instance.factorySystem == null;
                if (!flag) {
                    ref AssemblerComponent ptr = ref __instance.factorySystem.assemblerPool[__instance.assemblerId];
                    bool flag2 = ptr.id == __instance.assemblerId;
                    if (flag2) {
                        AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(__instance.factorySystem, __instance.assemblerId);
                        ref AssemblerComponent Assembler = ref __instance.factorySystem.assemblerPool[__instance.assemblerId];
                        int moduleId = AssemblerModulePatches.GetModuleId(Assembler.recipeId);
                        bool flag3 = (moduleId == 7616) && (AssemblerModuleData.ItemCount < AssemblerModuleData.NeedCount);
                        if (flag3) {
                            __instance.stateText.text = "缺少过滤器".TranslateFromJson();
                            __instance.stateText.color = __instance.workStoppedColor;
                        }
                        ItemProto itemProto = LDB.items.Select((AssemblerModuleData.ItemCount > 0) ? AssemblerModuleData.ItemId : moduleId);
                        UIAssemblerWindowPatch._icon.sprite = (AssemblerModuleData.ItemCount > 0) ? itemProto._iconSprite : _tagNotSelectedSprite;
                        UIAssemblerWindowPatch._icon.enabled = (AssemblerModuleData.ItemCount > 0);
                        UIAssemblerWindowPatch._text.text = (moduleId == 7616 || AssemblerModuleData.ItemCount > 0) ? itemProto.name : "伺服器".TranslateFromJson();
                        UIAssemblerWindowPatch._uiButton.tips.itemId = (AssemblerModuleData.ItemCount > 0) ? AssemblerModuleData.ItemId : 0;
                    }
                }
            }
        }

        // Token: 0x06000097 RID: 151 RVA: 0x00006834 File Offset: 0x00004A34
        private static void OnLithographyIconClick()
        {
            UIAssemblerWindow assemblerWindow = UIRoot.instance.uiGame.assemblerWindow;
            Player player = assemblerWindow.player;
            bool flag = assemblerWindow.assemblerId == 0 || assemblerWindow.factorySystem == null || player == null;
            if (!flag) {
                ref AssemblerComponent ptr = ref assemblerWindow.factorySystem.assemblerPool[assemblerWindow.assemblerId];
                bool flag2 = ptr.id != assemblerWindow.assemblerId;
                if (!flag2) {
                    bool flag3 = player.inhandItemId > 0 && player.inhandItemCount == 0;
                    if (flag3) {
                        player.SetHandItems(0, 0, 0);
                    } else {
                        AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(assemblerWindow.factorySystem, assemblerWindow.assemblerId);
                        bool flag4 = player.inhandItemId == 0 || player.inhandItemCount <= 0;
                        if (!flag4) {
                            int moduleId = AssemblerModulePatches.GetModuleId(ptr.recipeId);
                            bool flag5 = (moduleId == 7616 && moduleId == player.inhandItemId) || (moduleId != 7616 && (player.inhandItemId == 7617 || player.inhandItemId == 7618));
                            bool flag6 = !flag5;
                            if (flag6) {
                                UIRealtimeTip.Popup("不相符的物品".TranslateFromJson(), true, 0);
                            } else {
                                bool flag7 = AssemblerModuleData.NeedCount == 0;
                                if (flag7) {
                                    AssemblerModuleData.NeedCount = 1;
                                }
                                bool flag8 = AssemblerModuleData.ItemCount >= AssemblerModuleData.NeedCount;
                                if (flag8) {
                                    UIRealtimeTip.Popup("栏位已满".TranslateFromJson(), true, 0);
                                } else {
                                    int inhandItemCount = player.inhandItemCount;
                                    int num = AssemblerModuleData.NeedCount - AssemblerModuleData.ItemCount;
                                    num = ((num > inhandItemCount) ? inhandItemCount : num);
                                    int inhandItemInc = player.inhandItemInc;
                                    int num2 = (inhandItemCount == 0) ? 0 : (inhandItemInc * num / inhandItemCount);
                                    AssemblerModuleData.ItemId = player.inhandItemId;
                                    AssemblerModuleData.ItemCount += num;
                                    AssemblerModuleData.ItemInc = num2;
                                    //UIAssemblerWindowPatch._count.text = AssemblerModuleData.ItemCount.ToString();
                                    //UIAssemblerWindowPatch._count.color = ((AssemblerModuleData.NeedCount == AssemblerModuleData.ItemCount) ? assemblerWindow.workNormalColor : assemblerWindow.workStoppedColor);
                                    AssemblerModulePatches.SetAssemblerModuleData(assemblerWindow.factorySystem, assemblerWindow.assemblerId, AssemblerModuleData);
                                    player.AddHandItemCount_Unsafe(-num);
                                    player.SetHandItemInc_Unsafe(player.inhandItemInc - num2);
                                    bool flag9 = player.inhandItemCount <= 0;
                                    if (flag9) {
                                        player.SetHandItemId_Unsafe(0);
                                        player.SetHandItemCount_Unsafe(0);
                                        player.SetHandItemInc_Unsafe(0);
                                    }
                                }
                            }
                        } else {
                            if (AssemblerModuleData.ItemCount > 0) {
                                player.inhandItemId = AssemblerModuleData.ItemId;
                                player.inhandItemCount = AssemblerModuleData.ItemCount;
                                player.inhandItemInc = AssemblerModuleData.ItemInc;
                                AssemblerModuleData.ItemId = 0;
                                AssemblerModuleData.ItemCount = 0;
                                AssemblerModuleData.ItemInc = 0;
                            }
                        }
                        ItemProto itemProto = LDB.items.Select(AssemblerModuleData.ItemId);
                        UIAssemblerWindowPatch._icon.sprite = (AssemblerModuleData.ItemCount > 0) ? itemProto._iconSprite : _tagNotSelectedSprite;
                        UIAssemblerWindowPatch._icon.enabled = (AssemblerModuleData.ItemCount > 0);
                        UIAssemblerWindowPatch._text.text = (AssemblerModuleData.ItemCount > 0) ? itemProto.name : "伺服器".TranslateFromJson();
                        if (AssemblerModulePatches.GetModuleId(ptr.recipeId) == 7616) {
                            UIAssemblerWindowPatch._text.text = LDB.items.Select(7616).name;
                        }
                        UIAssemblerWindowPatch._uiButton.tips.itemId = (AssemblerModuleData.ItemCount > 0) ? AssemblerModuleData.ItemId : 0;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow.RefreshIncUIs))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UIAssemblerWindow_RefreshIncUIs_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 1151));

            matcher.SetAndAdvance(OpCodes.Ldc_I4, 1311);

            return matcher.InstructionEnumeration();
        }
        
    }
}
