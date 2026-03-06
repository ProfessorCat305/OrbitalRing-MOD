using UnityEngine.UI;
using UnityEngine;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using UnityEngine.EventSystems;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;

namespace ProjectOrbitalRing.Patches.UI.OrbitalRingStorage
{
    internal static class UIStationWindowPatch
    {
        private static GameObject _obj;

        private static GameObject _iconObj;

        private static Text _text;

        private static Text _count;

        private static Image _icon;


        private static UIShareStorage[] _uiShareStorage = new UIShareStorage[5];

        private static Button _button;

        private static Sprite _tagNotSelectedSprite;

        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        [HarmonyPostfix]
        [HarmonyPriority(0)]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            bool flag = UIStationWindowPatch._obj == null;
            if (flag) {
                Transform transform = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window").transform;
                UIStationWindowPatch._obj = new GameObject {
                    name = "shareStorageCheck"
                };
                UIStationWindowPatch._obj.transform.SetParent(transform, false);
                UIStationWindowPatch._obj.transform.localPosition = new Vector3(412f, -50f, 0f);
                Transform transform2 = UIStationWindowPatch._obj.transform;
                GameObject original2 = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/panel-down/station-settings/warper-necessary");

                for (int i = 0; i < 5; i++) {
                    GameObject gameObject1 = UnityEngine.Object.Instantiate<GameObject>(original2, transform2);
                    gameObject1.transform.localScale = Vector3.one;
                    gameObject1.transform.localPosition = new Vector3(-140f, -75f - 75f * i, 0f);
                    _uiShareStorage[i] = new UIShareStorage(i, gameObject1);
                }


                //UIStationWindow._uiButton.tips.itemCount = 0;
                //UIStationWindow._uiButton.tips.itemInc = 0;
                //UIStationWindow._uiButton.tips.type = UIButton.ItemTipType.IgnoreIncPoint;
                //UIStationWindowPatch._button = gameObject2.GetComponent<Button>();
                //UIStationWindowPatch._button.onClick.RemoveAllListeners();
                //UIStationWindowPatch._button.onClick.AddListener(new UnityAction(UIStationWindowPatch.OnShareStorageClick));
                //Image component = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/panel-down/station-settings/warper-necessary").GetComponent<Image>();
                //Image component2 = UIStationWindow._obj.GetComponent<Image>();
                //component2.sprite = component.sprite;
                //component2.color = component.color;
                //UIStationWindow._obj.GetComponent<RectTransform>().sizeDelta = new Vector2(68f, 68f);
                //UIStationWindow._obj.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                //UIStationWindow._obj.transform.localPosition = new Vector3(15f, -120f, 0f);
                //GameObject original = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/catalytic/eff-text");
                //GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
                //gameObject.transform.localScale = Vector3.one;
                //gameObject.transform.localPosition = new Vector3(0f, -48f, 0f);
                //UIStationWindow._text = gameObject.transform.GetComponent<Text>();
                //UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Localizer>());
                //UIStationWindow._text.text = "伺服器".TranslateFromJson();
                //UIStationWindow._text.fontSize = 16;
                //original = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/produce-2/fuel/fuel-icon");
                //UIStationWindow._iconObj = UnityEngine.Object.Instantiate<GameObject>(original, transform2);
                //UIStationWindow._iconObj.transform.localScale = Vector3.one;
                //UIStationWindow._icon = UIStationWindow._iconObj.GetComponent<Image>();
                //_tagNotSelectedSprite = UIStationWindow._icon.sprite;
                //UIStationWindow._count = UIStationWindow._iconObj.transform.Find("cnt-text").GetComponent<Text>();
                //UIStationWindow._count.text = "0";
                //UIStationWindow._count.fontSize = 16;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        public static void UIStationWindow_OnStationIdChange_Postfix(ref UIStationWindow __instance)
        {
            __instance.event_lock = true;
            if (__instance.active) {
                if (__instance.stationId == 0 || __instance.factory == null) {
                    __instance._Close();
                    __instance.event_lock = false;
                    return;
                }
                StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
                if (stationComponent == null || stationComponent.id != __instance.stationId) {
                    __instance._Close();
                    __instance.event_lock = false;
                    return;
                }
                EventSystem.current.SetSelectedGameObject(null);
                ItemProto itemProto = LDB.items.Select((int)__instance.factory.entityPool[stationComponent.entityId].protoId);
                if (itemProto == null) {
                    __instance._Close();
                    __instance.event_lock = false;
                    return;
                }
                if (itemProto.ID != ProtoID.I深空物流港 && itemProto.ID != ProtoID.I太空物流港 && itemProto.ID != ProtoID.I太空电梯) {
                    _obj.SetActive(false);
                    __instance.event_lock = false;
                    return;
                }
                _obj.SetActive(true);
                for (int i = 0; i < stationComponent.storage.Length; i++) {
                    if (stationComponent.storage[i].itemId == 0) {
                        _uiShareStorage[i].SetEnable(false, __instance.factory.planetId, stationComponent.id, i);
                    } else {
                        _uiShareStorage[i].SetEnable(true, __instance.factory.planetId, stationComponent.id, i);
                    }
                }
            }
            __instance.event_lock = false;
        }

    }

    public class UIShareStorage
    {
        private int storeIndex;

        private UIButton _uiButton; // Removed static modifier to make it an instance field

        private int LocalStationId;
        private int LocalPlanetId;

        public UIShareStorage(int index, GameObject gameObject1)
        {
            storeIndex = index;
            _uiButton = gameObject1.GetComponent<UIButton>();
            _uiButton.tips.tipText = "共享储存".TranslateFromJson();
            _uiButton.tips.tipTitle = "T接入共享储存".TranslateFromJson();
            _uiButton.transform.GetChild(1).gameObject.SetActive(false);
            _uiButton.onClick += OnShareStorageClick; // Updated to use instance method  
        }

        public void OnShareStorageClick(int obj)
        {
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(LocalPlanetId);
            if (planetOrbitalRingData == null) return;
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                if (planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare.ContainsKey(LocalStationId)) {
                    bool flag = planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare[LocalStationId][storeIndex];
                    planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare[LocalStationId][storeIndex] = !flag;
                    for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                        var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                        if (pair.OrbitalStationPoolId == LocalStationId && pair.elevatorPoolId != -1) {
                            planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare[pair.elevatorPoolId][storeIndex] = !flag;
                            break;
                        } else if (pair.elevatorPoolId == LocalStationId && pair.stationType == StationType.Station) {
                            planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare[pair.OrbitalStationPoolId][storeIndex] = !flag;
                            break;
                        }
                    }
                    _uiButton.transform.GetChild(2).GetComponent<Image>().enabled = !flag;
                }
            }
        }

        public void SetEnable(bool enable, int planetId, int stationId, int index)
        {
            LocalStationId = stationId;
            LocalPlanetId = planetId;
            _uiButton.enabled = enable;
            _uiButton.transform.GetChild(0).GetComponent<Image>().enabled = enable;
            if (!enable) {
                _uiButton.transform.GetChild(2).GetComponent<Image>().enabled = false;
                return;
            }
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planetId);
            if (planetOrbitalRingData == null) return;
            for (int ringId = 0; ringId < planetOrbitalRingData.Rings.Count; ringId++) {
                if (!planetOrbitalRingData.Rings[ringId].IsOneFull()) continue;
                for (int i = 0; i < planetOrbitalRingData.Rings[ringId].Capacity; i++) {
                    var pair = planetOrbitalRingData.Rings[ringId].GetPair(i);
                    if (pair.stationType == StationType.Station && pair.OrbitalStationPoolId == stationId) {
                        if (planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare.TryGetValue(stationId, out var flags)) {
                            _uiButton.transform.GetChild(2).GetComponent<Image>().enabled = flags[index];
                            return;
                        }

                    } else if (pair.elevatorPoolId != -1 && pair.elevatorPoolId == stationId) {
                        if (planetOrbitalRingData.Rings[ringId].orbitalRingStorage.storageShare.TryGetValue(stationId, out var flags)) {
                            _uiButton.transform.GetChild(2).GetComponent<Image>().enabled = flags[index];
                            return;
                        }
                    }
                }
            }
        }
    }
}
