using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using OrbitalRingStorageData = ProjectOrbitalRing.Patches.Logic.OrbitalRing.OrbitalRingStorage;
using ProjectOrbitalRing.Patches.UI.Utils;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static ProjectOrbitalRing.Patches.Logic.TheMountainMovingMappings;
using static ProjectOrbitalRing.Patches.UI.Utils.MyWindowCtl;
using static ProjectOrbitalRing.Patches.UI.Utils.Util;

namespace ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow
{
    public class UIOrbitalRingStorageWindow: ManualBehaviour
    {
        private readonly struct SelectedItemContext
        {
            public SelectedItemContext(
                OrbitalRingStorageData orbitalRingStorage,
                int[] countAndInc,
                int defaultLimit,
                int effectiveLimit,
                bool hasCustomLimit,
                int limitUpperBound)
            {
                OrbitalRingStorage = orbitalRingStorage;
                CountAndInc = countAndInc;
                DefaultLimit = defaultLimit;
                EffectiveLimit = effectiveLimit;
                HasCustomLimit = hasCustomLimit;
                LimitUpperBound = limitUpperBound;
            }

            public OrbitalRingStorageData OrbitalRingStorage { get; }
            public int[] CountAndInc { get; }
            public int DefaultLimit { get; }
            public int EffectiveLimit { get; }
            public bool HasCustomLimit { get; }
            public int LimitUpperBound { get; }
        }

        const int StorageXCount = 10;
        const int StorageYCount = 10;
        const int StorageCount = StorageXCount * StorageYCount;
        private static readonly Color SelectedSlotFrameColor = new Color(0.72f, 0.84f, 1f, 0.95f);
        private static readonly Color NormalSlotColor = Color.white;

        private readonly UIButton[] _iconBtns = new UIButton[StorageCount];
        private readonly Image[] _iconImgs = new Image[StorageCount];
        private readonly Text[] _iconTexts = new Text[StorageCount];
        private readonly GameObject[] _iconSelectedFrames = new GameObject[StorageCount];
        private readonly int[] _displayItemIds = new int[StorageCount];

        public RectTransform windowTrans;
        private RectTransform _tab1;
        private RectTransform _detailPanel;
        private Image _limitInputFrameImg;
        private UIButton _selectedIconBtn;
        private Image _selectedIconImg;
        private Text _selectedNameText;
        private Text _selectedCountText;
        private Text _selectedLimitText;
        private Text _limitRangeText;
        private Text _selectedHintText;
        private InputField _limitInputField;
        private UIButton _applyLimitBtn;
        private UIButton _useDefaultLimitBtn;

        private static Sprite _tagNotSelectedSprite;

        private int _selectedItemId;
        private int _limitInputItemId;
        private int _refreshFrame;
        private bool _changingLimitInput;

        internal static int CurPlanetId;
        internal static int CurStorageIndex;

        internal static UIOrbitalRingStorageWindow CreateWindow() =>
            CreateWindow<UIOrbitalRingStorageWindow>("UIOrbitalRingStorageWindow", "OrbitalRingStorageWindowTitle".TranslateFromJson());

        public void OpenWindow()
        {
            _refreshFrame = 0;
            MyWindowCtl.OpenWindow(this);
            RefreshCurrentStorage();
        }

        public override void _OnCreate()
        {
            windowTrans = GetRectTransform(this);
            windowTrans.sizeDelta = new Vector2(1170f, 930f);

            CreateUI();
        }

        public override void _OnUpdate()
        {
            if (!active) {
                return;
            }
            if (VFInput.escape) {
                VFInput.UseEscape();
                _Close();
                return;
            }
            _refreshFrame++;
            if (_refreshFrame % 15 == 0) {
                RefreshCurrentStorage();
            }
        }

        private void CreateUI()
        {
            var tab = new GameObject();
            _tab1 = tab.AddComponent<RectTransform>();
            NormalizeRectWithMargin(_tab1, 40, 40, 40, 40, windowTrans);
            tab.name = "tab-1";

            for (var i = 0; i < StorageXCount; ++i) {
                for (var j = 0; j < StorageYCount; ++j) {
                    CreateSignalIcon("", "", out UIButton iconBtn, out Image iconImage);
                    int slotIndex = i * StorageYCount + j;
                    _iconBtns[slotIndex] = iconBtn;
                    _iconImgs[slotIndex] = iconImage;
                    _iconTexts[slotIndex] = CreateText("", 16);
                    _iconSelectedFrames[slotIndex] = CreateSelectedFrame(iconBtn.transform);

                    NormalizeRectWithTopLeft(iconBtn.transform, j * 110, 60 + i * 60, _tab1);
                    NormalizeRectWithTopLeft(_iconTexts[slotIndex].transform, 55 + j * 110, 72 + i * 60, _tab1);

                    int id = slotIndex;
                    iconBtn.onClick += _ => OnIconBtnClick(id);
                }
            }

            _tagNotSelectedSprite = _iconImgs[0].sprite;

            GameObject detailObject = new GameObject("detail-panel");
            _detailPanel = detailObject.AddComponent<RectTransform>();
            detailObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            NormalizeRectWithTopLeft(_detailPanel, 0f, 660f, _tab1);
            _detailPanel.sizeDelta = new Vector2(1090f, 185f);

            CreateSignalIcon("", "", out UIButton selectedIconBtn, out Image selectedIconImage);
            _selectedIconBtn = selectedIconBtn;
            _selectedIconImg = selectedIconImage;
            NormalizeRectWithTopLeft(_selectedIconBtn.transform, 18f, 18f, _detailPanel);

            _selectedNameText = CreateText("OrbitalRingStorageWindowSelectItem".TranslateFromJson(), 18);
            NormalizeRectWithTopLeft(_selectedNameText.transform, 88f, 18f, _detailPanel);

            _selectedCountText = CreateText("", 16);
            NormalizeRectWithTopLeft(_selectedCountText.transform, 88f, 50f, _detailPanel);

            _selectedLimitText = CreateText("", 16);
            NormalizeRectWithTopLeft(_selectedLimitText.transform, 88f, 78f, _detailPanel);

            Text limitInputLabelText = CreateText("OrbitalRingStorageWindowManualInput".TranslateFromJson(), 16);
            NormalizeRectWithTopLeft(limitInputLabelText.transform, 330f, 42f, _detailPanel);

            _limitInputField = CreateLimitInputField(418f, 30f, 138f);

            _applyLimitBtn = CreateAdjustButton(572f, 32f, 72f, out Text applyText, () => ApplySelectedLimitFromInput(true));
            applyText.text = "OrbitalRingStorageWindowApply".TranslateFromJson();

            _useDefaultLimitBtn = CreateAdjustButton(660f, 32f, 84f, out Text useDefaultText, ResetSelectedLimit);
            useDefaultText.text = "OrbitalRingStorageWindowDefault".TranslateFromJson();

            _limitRangeText = CreateText("", 14);
            _limitRangeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _limitRangeText.verticalOverflow = VerticalWrapMode.Overflow;
            ((RectTransform)_limitRangeText.transform).sizeDelta = new Vector2(730f, 20f);
            NormalizeRectWithTopLeft(_limitRangeText.transform, 330f, 74f, _detailPanel);

            _selectedHintText = CreateText("", 14);
            _selectedHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _selectedHintText.verticalOverflow = VerticalWrapMode.Overflow;
            ((RectTransform)_selectedHintText.transform).sizeDelta = new Vector2(1040f, 40f);
            NormalizeRectWithTopLeft(_selectedHintText.transform, 18f, 132f, _detailPanel);

            ClearSelection();
        }

        private InputField CreateLimitInputField(float left, float top, float width)
        {
            GameObject frameObject = new GameObject("limit-input-frame");
            RectTransform frameRect = frameObject.AddComponent<RectTransform>();
            _limitInputFrameImg = frameObject.AddComponent<Image>();
            _limitInputFrameImg.color = new Color(0f, 0f, 0f, 0.45f);
            _limitInputFrameImg.raycastTarget = false;
            Outline outline = frameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);
            NormalizeRectWithTopLeft(frameRect, left, top, _detailPanel);
            frameRect.sizeDelta = new Vector2(width, 32f);

            InputField inputField = Instantiate(UIRoot.instance.uiGame.monitorWindow.cargoFlowInputField);
            NormalizeRectWithMargin(inputField, 2f, 2f, 2f, 2f, frameRect);
            Image inputImage = inputField.GetComponent<Image>();
            if (inputImage != null) {
                _limitInputFrameImg.sprite = inputImage.sprite;
                _limitInputFrameImg.type = inputImage.type;
            }
            RectTransform textViewportRect = null;
            if (inputField.textComponent != null) {
                textViewportRect = inputField.textComponent.transform.parent as RectTransform;
            }
            if (textViewportRect != null) {
                NormalizeRectWithMargin(textViewportRect, 3f, 8f, 3f, 8f, inputField.transform);
            }
            inputField.onValueChanged = new InputField.OnChangeEvent();
            inputField.onEndEdit = new InputField.EndEditEvent();
            inputField.characterLimit = 10;
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.customCaretColor = true;
            inputField.caretColor = new Color(1f, 1f, 1f, 0.95f);
            inputField.selectionColor = new Color(1f, 1f, 1f, 0.18f);
            inputField.caretBlinkRate = 0.85f;
            inputField.caretWidth = 2;
            inputField.text = "";
            inputField.onEndEdit.AddListener(OnLimitInputEndEdit);
            Text placeholder = inputField.placeholder as Text;
            if (placeholder != null) {
                placeholder.text = "OrbitalRingStorageWindowInputLimit".TranslateFromJson();
                placeholder.fontSize = 14;
                placeholder.alignment = TextAnchor.MiddleLeft;
                placeholder.color = new Color(1f, 1f, 1f, 0.35f);
                if (textViewportRect != null) {
                    NormalizeRectWithMargin(placeholder, 0f, 0f, 0f, 0f, textViewportRect);
                }
            }
            if (inputField.textComponent != null) {
                inputField.textComponent.fontSize = 16;
                inputField.textComponent.alignment = TextAnchor.MiddleLeft;
                inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                inputField.textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                if (textViewportRect != null) {
                    NormalizeRectWithMargin(inputField.textComponent, 0f, 0f, 0f, 0f, textViewportRect);
                }
            }
            if (inputImage != null) {
                inputImage.color = new Color(1f, 1f, 1f, 0.08f);
            }
            return inputField;
        }

        private static GameObject CreateSelectedFrame(Transform parent)
        {
            GameObject frameObject = new GameObject("selected-frame");
            RectTransform frameRect = frameObject.AddComponent<RectTransform>();

            frameRect.SetParent(parent, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.offsetMin = new Vector2(-2f, -2f);
            frameRect.offsetMax = new Vector2(2f, 2f);
            frameRect.SetAsLastSibling();
            CreateSelectedFrameBar(frameRect, "top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 0f));
            CreateSelectedFrameBar(frameRect, "bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f));
            CreateSelectedFrameBar(frameRect, "left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f));
            CreateSelectedFrameBar(frameRect, "right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f, 0f));

            frameObject.SetActive(false);
            return frameObject;
        }

        private static void CreateSelectedFrameBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject borderObject = new GameObject(name);
            RectTransform borderRect = borderObject.AddComponent<RectTransform>();
            Image borderImage = borderObject.AddComponent<Image>();

            borderRect.SetParent(parent, false);
            borderRect.anchorMin = anchorMin;
            borderRect.anchorMax = anchorMax;
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.offsetMin = offsetMin;
            borderRect.offsetMax = offsetMax;

            borderImage.color = SelectedSlotFrameColor;
            borderImage.raycastTarget = false;
        }

        private UIButton CreateAdjustButton(float left, float top, out Text buttonText, Action onClick)
        {
            return CreateAdjustButton(left, top, 80f, out buttonText, onClick);
        }

        private UIButton CreateAdjustButton(float left, float top, float width, out Text buttonText, Action onClick)
        {
            UIButton button = CreateButton("", width, 26f);
            NormalizeRectWithTopLeft(button.transform, left, top, _detailPanel);
            buttonText = button.transform.Find("Text").GetComponent<Text>();
            buttonText.fontSize = 14;
            button.onClick += _ => onClick();
            return button;
        }

        private void OnIconBtnClick(int slotIndex)
        {
            // 手持乾坤物品时，格子点击优先尝试存入共享空间。
            if (TryInsertInhandQianKunItem()) {
                return;
            }

            int itemId = _displayItemIds[slotIndex];
            if (itemId != 0) {
                SelectItem(itemId);
                return;
            }
        }

        private bool TryInsertInhandQianKunItem()
        {
            Player player = GameMain.mainPlayer;
            if (player == null || player.inhandItemCount == 0) {
                return false;
            }

            int oreItemId = TheMountainMovingMappings.GetOreId(player.inhandItemId);
            if (player.inhandItemId == oreItemId) {
                return false;
            }
            if (!TryGetCurrentStorage(out OrbitalRingStorageData orbitalRingStorage)) {
                return false;
            }

            Dictionary<int, int[]> storageItem = orbitalRingStorage.storageItem;
            int acceptedCount = player.inhandItemCount;
            int acceptedInc = player.inhandItemInc;
            if (storageItem.ContainsKey(oreItemId)) {
                if (storageItem[oreItemId][0] + player.inhandItemCount > GetStorageHardLimit() &&
                    HasOtherQianKunItemOverLimit(storageItem, oreItemId)) {
                    return true;
                }

                acceptedCount = 2147480000 - storageItem[oreItemId][0];
                if (acceptedCount > player.inhandItemCount) {
                    acceptedCount = player.inhandItemCount;
                }
                if (acceptedCount <= 0) {
                    return true;
                }
                int remainCount = player.inhandItemCount;
                int remainInc = player.inhandItemInc;
                acceptedInc = PosTool.split_inc(ref remainCount, ref remainInc, acceptedCount);
                storageItem[oreItemId][0] += acceptedCount;
                storageItem[oreItemId][1] += acceptedInc;
            } else {
                if (acceptedCount > GetStorageHardLimit() &&
                    HasOtherQianKunItemOverLimit(storageItem, oreItemId)) {
                    return true;
                }
                if (acceptedCount > 2147480000) {
                    acceptedCount = 2147480000;
                }
                if (acceptedCount <= 0) {
                    return true;
                }
                int remainCount = player.inhandItemCount;
                int remainInc = player.inhandItemInc;
                acceptedInc = PosTool.split_inc(ref remainCount, ref remainInc, acceptedCount);
                storageItem.Add(oreItemId, new[] { acceptedCount, acceptedInc });
            }

            player.AddHandItemCount_Unsafe(-acceptedCount);
            player.SetHandItemInc_Unsafe(player.inhandItemInc - acceptedInc);
            if (player.inhandItemCount <= 0) {
                player.SetHandItemId_Unsafe(0);
                player.SetHandItemCount_Unsafe(0);
                player.SetHandItemInc_Unsafe(0);
            }

            RefreshCurrentStorage();
            SelectItem(oreItemId);
            return true;
        }

        public void RefreshCurrentStorage()
        {
            if (!TryGetCurrentStorage(out OrbitalRingStorageData orbitalRingStorage)) {
                SetStorageData(null);
                return;
            }

            SetStorageData(orbitalRingStorage);
        }

        internal void SetCurrentStorage(int planetId, int storageIndex)
        {
            CurPlanetId = planetId;
            CurStorageIndex = storageIndex;
        }

        public void SetStorageData(OrbitalRingStorageData orbitalRingStorage)
        {
            Dictionary<int, int[]> storageItem = orbitalRingStorage?.storageItem;
            Dictionary<int, int> storageLimit = orbitalRingStorage?.storageLimit;
            int i = 0;
            HashSet<int> displayedItemIds = new HashSet<int>();
            if (storageItem != null) {
                foreach (var kvp in storageItem) {
                    if (i >= StorageCount) {
                        break;
                    }

                    ItemProto proto = LDB.items.Select(kvp.Key);
                    _displayItemIds[i] = kvp.Key;
                    _iconTexts[i].text = kvp.Value[0].ToString();
                    _iconImgs[i].sprite = proto?.iconSprite ?? _tagNotSelectedSprite;
                    _iconBtns[i].tips.itemId = kvp.Key;
                    _iconBtns[i].tips.itemCount = kvp.Value[0];
                    _iconBtns[i].tips.itemInc = kvp.Value[1];
                    _iconBtns[i].tips.type = UIButton.ItemTipType.Item;
                    displayedItemIds.Add(kvp.Key);
                    i++;
                }
            }

            // 设置了非默认上限的物品也需要展示在详情界面。
            if (storageLimit != null) {
                foreach (var kvp in storageLimit) {
                    if (i >= StorageCount) {
                        break;
                    }
                    if (displayedItemIds.Contains(kvp.Key)) {
                        continue;
                    }

                    ItemProto proto = LDB.items.Select(kvp.Key);
                    _displayItemIds[i] = kvp.Key;
                    _iconTexts[i].text = "0";
                    _iconImgs[i].sprite = proto?.iconSprite ?? _tagNotSelectedSprite;
                    _iconBtns[i].tips.itemId = kvp.Key;
                    _iconBtns[i].tips.itemCount = 0;
                    _iconBtns[i].tips.itemInc = 0;
                    _iconBtns[i].tips.type = UIButton.ItemTipType.Item;
                    displayedItemIds.Add(kvp.Key);
                    i++;
                }
            }

            for (; i < StorageCount; i++) {
                _displayItemIds[i] = 0;
                _iconImgs[i].sprite = _tagNotSelectedSprite;
                _iconImgs[i].color = NormalSlotColor;
                _iconTexts[i].text = "";
                _iconBtns[i].tips.itemId = 0;
                _iconBtns[i].tips.itemCount = 0;
                _iconBtns[i].tips.itemInc = 0;
                _iconBtns[i].tips.type = UIButton.ItemTipType.IgnoreIncPoint;
            }

            if (orbitalRingStorage == null || _selectedItemId == 0 || !ContainsDisplayItem(orbitalRingStorage, _selectedItemId)) {
                _selectedItemId = 0;
                if (storageItem != null) {
                    foreach (var kvp in storageItem) {
                        _selectedItemId = kvp.Key;
                        break;
                    }
                }
                if (_selectedItemId == 0 && storageLimit != null) {
                    foreach (var kvp in storageLimit) {
                        _selectedItemId = kvp.Key;
                        break;
                    }
                }
            }

            RefreshSelectionVisual();
            UpdateSelectionPanel();
        }

        private void SelectItem(int itemId)
        {
            _selectedItemId = itemId;
            RefreshSelectionVisual();
            UpdateSelectionPanel();
        }

        private void RefreshSelectionVisual()
        {
            for (int i = 0; i < StorageCount; i++) {
                bool selected = _displayItemIds[i] != 0 && _displayItemIds[i] == _selectedItemId;
                _iconImgs[i].color = NormalSlotColor;
                if (_iconSelectedFrames[i] != null) {
                    _iconSelectedFrames[i].SetActive(selected);
                }
            }
        }

        private void ClearSelection()
        {
            _selectedItemId = 0;
            RefreshSelectionVisual();
            UpdateSelectionPanel();
        }

        private void UpdateSelectionPanel()
        {
            if (!TryGetSelectedItemContext(out SelectedItemContext context)) {
                _limitInputItemId = 0;
                _selectedIconImg.sprite = _tagNotSelectedSprite;
                _selectedIconBtn.tips.itemId = 0;
                _selectedIconBtn.tips.itemCount = 0;
                _selectedIconBtn.tips.itemInc = 0;
                _selectedNameText.text = "OrbitalRingStorageWindowSelectItem".TranslateFromJson();
                _selectedCountText.text = string.Format("OrbitalRingStorageWindowCurrentCount".TranslateFromJson(), "-");
                _selectedLimitText.text = string.Format("OrbitalRingStorageWindowSharedLimitDefault".TranslateFromJson(), "-");
                _limitRangeText.text = string.Format("OrbitalRingStorageWindowInputRange".TranslateFromJson(), "-");
                _selectedHintText.text = string.Format("OrbitalRingStorageWindowNoSelectionHint".TranslateFromJson(), GetStorageHardLimit());
                SetLimitInputValue("");
                SetLimitControlsEnabled(false);
                return;
            }

            ItemProto proto = LDB.items.Select(_selectedItemId);
            int currentCount = context.CountAndInc[0];
            bool qianKunItemOverHardLimit =
                TheMountainMovingMappings.GetQianKunItemId(_selectedItemId) != _selectedItemId &&
                currentCount > GetStorageHardLimit();

            _selectedIconImg.sprite = proto?.iconSprite ?? _tagNotSelectedSprite;
            _selectedIconBtn.tips.itemId = _selectedItemId;
            _selectedIconBtn.tips.itemCount = currentCount;
            _selectedIconBtn.tips.itemInc = context.CountAndInc[1];
            _selectedIconBtn.tips.type = UIButton.ItemTipType.Item;
            _selectedNameText.text = proto != null ? proto.name : _selectedItemId.ToString();
            _selectedCountText.text = string.Format("OrbitalRingStorageWindowCurrentCount".TranslateFromJson(), currentCount);
            _selectedLimitText.text = context.HasCustomLimit
                ? string.Format("OrbitalRingStorageWindowSharedLimitCustom".TranslateFromJson(), context.EffectiveLimit, context.DefaultLimit)
                : string.Format("OrbitalRingStorageWindowSharedLimitDefault".TranslateFromJson(), context.DefaultLimit);
            _limitRangeText.text = qianKunItemOverHardLimit
                ? string.Format("OrbitalRingStorageWindowInputRange".TranslateFromJson(), "-")
                : string.Format("OrbitalRingStorageWindowInputRange".TranslateFromJson(), $"0 - {FormatNumber(context.LimitUpperBound)}");
            // 已通过乾坤原矿存入突破上限的矿物只保留展示，不再允许继续改上限。
            if (qianKunItemOverHardLimit) {
                _limitInputItemId = _selectedItemId;
                SetLimitInputValue("");
            } else if (_limitInputItemId != _selectedItemId || !_limitInputField.isFocused) {
                _limitInputItemId = _selectedItemId;
                SetLimitInputValue(context.EffectiveLimit.ToString());
            }
            _selectedHintText.text = GetLimitHintText(currentCount, context.EffectiveLimit, qianKunItemOverHardLimit);

            SetLimitControlsEnabled(!qianKunItemOverHardLimit);
            if (!qianKunItemOverHardLimit) {
                _limitInputField.interactable = true;
                _applyLimitBtn.button.interactable = true;
                _useDefaultLimitBtn.button.interactable = context.HasCustomLimit;
            }
        }

        private void OnLimitInputEndEdit(string text)
        {
            if (_changingLimitInput) return;
            ApplySelectedLimitFromInput(false);
        }

        private void ApplySelectedLimitFromInput(bool showErrorTips)
        {
            if (_limitInputField == null) return;
            if (!TryGetSelectedItemContext(out SelectedItemContext context)) return;
            if (TheMountainMovingMappings.GetQianKunItemId(_selectedItemId) != _selectedItemId &&
                context.CountAndInc[0] > GetStorageHardLimit()) return;
            string inputText = _limitInputField.text;
            if (string.IsNullOrWhiteSpace(inputText)) {
                if (showErrorTips) {
                    UIRealtimeTip.Popup("OrbitalRingStorageWindowInvalidLimit".TranslateFromJson());
                }
                UpdateSelectionPanel();
                return;
            }
            if (!int.TryParse(inputText, out int newLimit)) {
                if (showErrorTips) {
                    UIRealtimeTip.Popup("OrbitalRingStorageWindowInvalidLimit".TranslateFromJson());
                }
                UpdateSelectionPanel();
                return;
            }

            if (newLimit < 0) {
                newLimit = 0;
            } else if (newLimit > context.LimitUpperBound) {
                newLimit = context.LimitUpperBound;
            }
            ApplySelectedLimit(context.OrbitalRingStorage, context.DefaultLimit, newLimit);
        }

        private void ResetSelectedLimit()
        {
            if (!TryGetCurrentStorage(out OrbitalRingStorageData orbitalRingStorage)) return;
            if (_selectedItemId == 0) return;
            if (orbitalRingStorage.storageItem.TryGetValue(_selectedItemId, out int[] countAndInc) &&
                TheMountainMovingMappings.GetQianKunItemId(_selectedItemId) != _selectedItemId &&
                countAndInc[0] > GetStorageHardLimit()) return;

            orbitalRingStorage.storageLimit.Remove(_selectedItemId);
            UpdateSelectionPanel();
        }

        private void ApplySelectedLimit(OrbitalRingStorageData orbitalRingStorage, int defaultLimit, int newLimit)
        {
            if (_selectedItemId == 0) return;

            if (newLimit == defaultLimit) {
                orbitalRingStorage.storageLimit.Remove(_selectedItemId);
            } else {
                orbitalRingStorage.storageLimit[_selectedItemId] = newLimit;
            }

            UpdateSelectionPanel();
        }

        private void SetLimitControlsEnabled(bool enabled)
        {
            if (_limitInputField != null) {
                _limitInputField.interactable = enabled;
            }
            if (_limitInputFrameImg != null) {
                _limitInputFrameImg.color = enabled
                    ? new Color(0f, 0f, 0f, 0.45f)
                    : new Color(0f, 0f, 0f, 0.2f);
            }
            if (_applyLimitBtn != null) {
                _applyLimitBtn.button.interactable = enabled;
            }
            _useDefaultLimitBtn.button.interactable = enabled;
        }

        private void SetLimitInputValue(string text)
        {
            _changingLimitInput = true;
            _limitInputField.text = text;
            _changingLimitInput = false;
        }

        private static int GetLimitUpperBound(int itemId)
        {
            return GetStorageHardLimit();
        }

        private static string GetLimitHintText(int currentCount, int currentLimit, bool qianKunItemOverHardLimit)
        {
            int storageHardLimit = GetStorageHardLimit();
            if (qianKunItemOverHardLimit) {
                return string.Format("OrbitalRingStorageWindowQianKunItemLimitExceeded".TranslateFromJson(), storageHardLimit);
            }
            return string.Format("OrbitalRingStorageWindowLimitReached".TranslateFromJson(), storageHardLimit);
        }

        private static string FormatNumber(int value) => value.ToString();

        private static int GetStorageHardLimit() => OrbitalRingStorageCalculate.StorageHardLimit;

        private static bool HasOtherQianKunItemOverLimit(Dictionary<int, int[]> storageItem, int currentOreItemId)
        {
            for (int i = 0; i < TheMountainMovingMappings.OreIdList.Length; i++) {
                int oreItemId = TheMountainMovingMappings.OreIdList[i];
                if (oreItemId == currentOreItemId) {
                    continue;
                }
                if (storageItem.ContainsKey(oreItemId) && storageItem[oreItemId][0] > GetStorageHardLimit() + 1000) {
                    return true;
                }
            }
            return false;
        }

        private bool TryGetSelectedItemContext(out SelectedItemContext context)
        {
            context = default;

            if (_selectedItemId == 0) {
                return false;
            }
            if (!TryGetCurrentStorage(out OrbitalRingStorageData orbitalRingStorage)) {
                return false;
            }
            bool hasStorageItem = orbitalRingStorage.storageItem.TryGetValue(_selectedItemId, out int[] countAndInc);
            bool hasCustomLimit = orbitalRingStorage.storageLimit.TryGetValue(_selectedItemId, out int customLimit);
            if (!hasStorageItem && !hasCustomLimit) {
                return false;
            }
            if (!hasStorageItem) {
                countAndInc = new[] { 0, 0 };
            }

            int defaultLimit = OrbitalRingStorageCalculate.GetDefaultItemStorageLimit(_selectedItemId);
            bool qianKunItemOverHardLimit =
                TheMountainMovingMappings.GetQianKunItemId(_selectedItemId) != _selectedItemId &&
                countAndInc[0] > GetStorageHardLimit();
            hasCustomLimit = !qianKunItemOverHardLimit && hasCustomLimit;
            int effectiveLimit = OrbitalRingStorageCalculate.GetEffectiveItemStorageLimit(orbitalRingStorage, _selectedItemId);
            int limitUpperBound = GetLimitUpperBound(_selectedItemId);
            context = new SelectedItemContext(orbitalRingStorage, countAndInc, defaultLimit, effectiveLimit, hasCustomLimit, limitUpperBound);
            return true;
        }

        private static bool ContainsDisplayItem(OrbitalRingStorageData orbitalRingStorage, int itemId)
        {
            return orbitalRingStorage.storageItem.ContainsKey(itemId) || orbitalRingStorage.storageLimit.ContainsKey(itemId);
        }

        private static bool TryGetCurrentStorage(out OrbitalRingStorageData orbitalRingStorage)
        {
            orbitalRingStorage = null;
            PlanetOrbitalRingData planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(CurPlanetId);
            if (planetOrbitalRingData == null || CurStorageIndex < 0 || CurStorageIndex >= planetOrbitalRingData.Rings.Count) {
                return false;
            }

            orbitalRingStorage = planetOrbitalRingData.Rings[CurStorageIndex].orbitalRingStorage;
            return orbitalRingStorage != null;
        }
    }
}
