using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
                case 624:
                case 775:
                case 778:
                case 784:
                case 800:
                    return true;
                default:
                    break;
            }
            return false;
        }


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
            AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(__instance.factorySystem.planet.id, __instance.assemblerId);
            int moduleId = AssemblerModulePatches.GetModuleId(assemblerComponent.recipeId);
            bool flag = (AssemblerModuleData.ItemId == moduleId || moduleId == 7617 && AssemblerModuleData.ItemId == 7618);
            if (!flag) {
                bool flag2 = AssemblerModuleData.ItemCount != 0;
                if (flag2) {
                    AssemblerModulePatches.SetEmpty(__instance.factorySystem.planet.id, __instance.assemblerId, true);
                }
                AssemblerModuleData.ItemId = 0;
                AssemblerModuleData.ItemCount = 0;
                AssemblerModuleData.ItemInc = 0;
                AssemblerModuleData.NeedCount = 1;
                AssemblerModulePatches.SetAssemblerModuleData(__instance.factorySystem.planet.id, __instance.assemblerId, AssemblerModuleData);
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
                    bool flag2 = ptr.id != __instance.assemblerId;
                    if (!flag2) {
                        AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(__instance.factorySystem.planet.id, __instance.assemblerId);
                        ref AssemblerComponent Assembler = ref __instance.factorySystem.assemblerPool[__instance.assemblerId];
                        int moduleId = AssemblerModulePatches.GetModuleId(Assembler.recipeId);
                        bool flag3 = (moduleId == 7616) && (AssemblerModuleData.ItemCount < AssemblerModuleData.NeedCount);
                        if (flag3) {
                            __instance.stateText.text = "缺少过滤器".TranslateFromJson();
                            __instance.stateText.color = __instance.workStoppedColor;
                        }
                        ItemProto itemProto = LDB.items.Select(moduleId);
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
                        AssemblerModuleData AssemblerModuleData = AssemblerModulePatches.GetAssemblerModuleData(assemblerWindow.factorySystem.planet.id, assemblerWindow.assemblerId);
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
                                    UIRealtimeTip.Popup(StringTranslate.Translate("栏位已满"), true, 0);
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
                                    AssemblerModulePatches.SetAssemblerModuleData(assemblerWindow.factorySystem.planet.id, assemblerWindow.assemblerId, AssemblerModuleData);
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
