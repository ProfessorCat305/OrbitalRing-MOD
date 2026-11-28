using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Patches.UI.Utils;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OrbitalRingStorageData = ProjectOrbitalRing.Patches.Logic.OrbitalRing.OrbitalRingStorage;
using static ProjectOrbitalRing.Patches.UI.Utils.MyWindowCtl;
using static ProjectOrbitalRing.Patches.UI.Utils.Util;

namespace ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow
{
    public class UIOrbitalRingStorageWindow : ManualBehaviour, IPointerDownHandler
    {
        private const float WindowPadding = 30f;
        private const float ScreenMargin = 12f;
        private const float MinimumHeight = 480f;
        private const float CargoTop = 184f;
        private const float FooterGap = 12f;
        private const float FooterControlsHeight = 28f;
        private const float FooterHeight = FooterGap + FooterControlsHeight + WindowPadding;
        private const int CargoColumns = 10;
        private const int MaxPageSize = 100;
        private const float CargoSize = 50f;
        private const float CargoGap = 8f;
        private const float CargoStride = CargoSize + CargoGap;
        private const float CargoPadding = 4f;
        private const float SelectionSize = CargoSize + 6f;
        private const float ListWidth = CargoColumns * CargoStride - CargoGap + 2f * CargoPadding;
        private const float SideLeft = WindowPadding + ListWidth + 30f;
        private const float SideWidth = 204f;
        private const float Width = SideLeft + SideWidth + WindowPadding;

        private static readonly Color TextColor = new Color(0.88f, 0.92f, 0.96f);
        private static readonly Color Secondary = new Color(0.68f, 0.76f, 0.83f);
        private static readonly Color Accent = new Color(0.55f, 0.83f, 0.92f);
        private static readonly Color SelectionOutline = new Color(0.65f, 0.94f, 1f);
        private static readonly Color SelectionFill = new Color(0.26f, 0.59f, 0.73f, 0.22f);
        private static readonly Color Warning = new Color(1f, 0.77f, 0.48f);
        private static readonly Color Border = new Color(0.48f, 0.62f, 0.70f, 0.48f);
        private static readonly Color Divider = new Color(0.48f, 0.62f, 0.70f, 0.26f);
        private static readonly Color InputFill = new Color(0.07f, 0.12f, 0.16f, 0.8f);

        private readonly struct StorageEntry
        {
            internal StorageEntry(OrbitalRingStorageData storage, int itemId, int[] stack)
            {
                ItemId = itemId;
                Count = stack?[0] ?? 0;
                Inc = stack?[1] ?? 0;
                Limit = OrbitalRingStorageCalculate.GetEffectiveItemStorageLimit(storage, itemId);
                HasCustomLimit = storage.storageLimit.ContainsKey(itemId);
            }

            internal int ItemId { get; }
            internal int Count { get; }
            internal int Inc { get; }
            internal int Limit { get; }
            internal bool HasCustomLimit { get; }
            internal bool IsOverflow => OrbitalRingStorageCalculate.IsQianKunOverflow(ItemId, Count);
        }

        private sealed class CargoTile
        {
            internal RectTransform Rect;
            internal CargoSlot Cargo;
            internal int ItemId;
        }

        private sealed class CargoSlot
        {
            internal UIControlPanelStorageItem View;
            internal Image[] IncMarks;
        }

        private readonly List<CargoTile> _tiles = new List<CargoTile>(MaxPageSize);
        private readonly List<StorageEntry> _entries = new List<StorageEntry>();
        private readonly List<StorageEntry> _filteredEntries = new List<StorageEntry>();
        private readonly Dictionary<(int PlanetId, int RingIndex), int> _pageAnchors = new Dictionary<(int, int), int>();
        private readonly Vector3[] _inventoryCorners = new Vector3[4];
        private Vector2 _layoutBoundsSize;

        private RectTransform _window;
        private Font _bodyFont;
        private InputField _inputPrefab;
        private UIControlPanelStorageItem _cargoPrefab;
        private Image[] _incPrefabs;
        private InputField _search;
        private RectTransform _depositSurface;
        private CargoSlot _heldCargo;
        private UIButton _emptySlot;
        private Text _depositTitle;
        private Text _depositHint;
        private RectTransform _cargoContent;
        private RectTransform _selectionFill;
        private RectTransform _selectionFrame;
        private Text _listEmptyTitle;
        private Text _listEmptyBody;
        private RectTransform _cargoFooter;
        private Text _itemCount;
        private Button _previousPage;
        private Button _nextPage;
        private Text _qianKunTitle;
        private Text _qianKunCount;
        private Text _qianKunHeld;
        private RectTransform _editor;
        private Text _editorIdle;
        private Image _editorIcon;
        private Text _editorTitle;
        private Text _currentLimit;
        private Text _defaultLimit;
        private Text _editorHint;
        private InputField _limitInput;
        private Button _apply;
        private Button _reset;
        private int _appliedLimit;
        private int _planetId;
        private int _ringIndex;
        private int _page;
        private int _pageSize;
        private int _pageAnchorItemId;
        private int _selectedItemId;
        private int _splitItemId;
        private int _lastHandItemId;
        private int _lastHandCount;
        private int _lastHandInc;
        private int _refreshFrame;
        private bool _ownsInventory;
        private bool _pickerOpen;
        private GameObject _pickerDragHandle;

        private static string T(string suffix) => ("OrbitalRingStorageWindow" + suffix).TranslateFromJson();

        internal static UIOrbitalRingStorageWindow CreateWindow() =>
            CreateWindow<UIOrbitalRingStorageWindow>("UIOrbitalRingStorageWindow", T("Title"));

        internal void OpenWindow(int planetId, int ringIndex)
        {
            _planetId = planetId;
            _ringIndex = ringIndex;
            _refreshFrame = 0;
            _selectedItemId = 0;
            _pageAnchors.TryGetValue((_planetId, _ringIndex), out _pageAnchorItemId);
            _lastHandItemId = -1;
            _lastHandCount = -1;
            _lastHandInc = -1;
            _search.SetTextWithoutNotify("");
            MyWindowCtl.OpenWindow(this);
            OpenInventory();
            LayoutWindow();
            if (MultiplayerSync.IsClient) MultiplayerSync.Send(new OrbitalRingPlanetLoadRequest(planetId));
        }

        public override void _OnCreate()
        {
            _window = GetRectTransform(this);
            _window.anchorMin = _window.anchorMax = new Vector2(0.5f, 0.5f);
            _window.pivot = new Vector2(0f, 1f);
            _window.sizeDelta = new Vector2(Width, MinimumHeight);
            UIGame ui = UIRoot.instance.uiGame;
            UIControlPanelWindow logistics = ui.controlPanelWindow;
            UIControlPanelStationStorage station = logistics.stationInspector.storageUIPrefab;
            foreach (UIControlPanelObjectEntry entry in logistics.objectEntryPrefabs) {
                if (entry is UIControlPanelStationEntry stationEntry) {
                    _cargoPrefab = stationEntry.storageItem0;
                    break;
                }
            }
            _incPrefabs = station.itemIncs;
            _bodyFont = station.orderLabelText.font;
            _inputPrefab = ui.monitorWindow.cargoFlowInputField;
            Text title = GetTitleText(this);
            if (title != null) {
                title.font = _bodyFont;
            }
            var tankMenu = GetComponentInChildren<UIGenericMenuButton>(true);
            if (tankMenu != null) tankMenu.gameObject.SetActive(false);

            _depositSurface = Panel("deposit-surface", _window, 0f, 0f, 0f, 0f);
            NormalizeRectWithMargin(_depositSurface, 44f, 14f, 14f, 14f);
            _depositSurface.gameObject.AddComponent<Image>().color = Color.clear;

            _search = CreateInput(_window, "item-search", T("Search"), WindowPadding, 54f, ListWidth, false);
            _search.onValueChanged.AddListener(_ => ChangeSearch());

            RectTransform slot = Panel("vacant-slot", _window, WindowPadding, 104f, ListWidth, 66f);
            CreateSignalIcon("", "", out _emptySlot, out _);
            _emptySlot.name = "empty-item-slot";
            RectTransform emptySlotRect = NormalizeRectWithTopLeft(_emptySlot, 12f, 12f, slot);
            emptySlotRect.sizeDelta = new Vector2(40f, 40f);
            emptySlotRect.localScale = Vector3.one;
            if (_emptySlot.button != null) _emptySlot.button.onClick = new Button.ButtonClickedEvent();
            _emptySlot.onMouseDown += (_, button) => {
                if (button == PointerEventData.InputButton.Left) DepositHeldItem();
            };
            Label(emptySlotRect, "+", 20, 0f, 0f, 40f, 40f, Accent, TextAnchor.MiddleCenter);
            _heldCargo = CreateCargoSlot(slot, 12f, 12f, 1f, () => 0);
            _depositTitle = Label(slot, "", 16, 68f, 6f, ListWidth - 80f, 28f, TextColor);
            _depositHint = Label(slot, "", 13, 68f, 34f, ListWidth - 80f, 24f, Secondary);

            CreateItemList();
            CreateQianKunPanel();
            CreateLimitPanel();
        }

        private void CreateItemList()
        {
            _cargoContent = Panel("inventory-list", _window, WindowPadding, CargoTop, ListWidth, 0f);
            _cargoContent.gameObject.AddComponent<Image>();
            // The native proliferation material uses stencil clipping.
            _cargoContent.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            _selectionFill = Panel("selected-cargo-fill", _cargoContent, 0f, 0f, SelectionSize, SelectionSize);
            Image selectionFill = _selectionFill.gameObject.AddComponent<Image>();
            selectionFill.color = SelectionFill;
            selectionFill.raycastTarget = false;
            _selectionFill.gameObject.SetActive(false);
            _selectionFrame = Panel("selected-cargo-frame", _cargoContent, 0f, 0f, SelectionSize, SelectionSize);
            CreateFrame(_selectionFrame, SelectionSize, SelectionSize, SelectionOutline, 2f);
            _selectionFrame.gameObject.SetActive(false);

            _listEmptyTitle = Label(_cargoContent, "", 19, 28f, 74f, ListWidth - 56f, 36f, TextColor, TextAnchor.MiddleCenter);
            _listEmptyBody = Label(_cargoContent, "", 15, 28f, 118f, ListWidth - 56f, 88f, Secondary, TextAnchor.UpperCenter);
            _cargoFooter = Panel("cargo-footer", _window, WindowPadding, 0f, ListWidth, FooterControlsHeight);
            _previousPage = CreateButton(_cargoFooter, T("PreviousPage"), 0f, 0f, 82f, () => ChangePage(-1), FooterControlsHeight);
            _itemCount = Label(_cargoFooter, "", 13, 90f, 0f, ListWidth - 180f, FooterControlsHeight, Secondary, TextAnchor.MiddleCenter);
            _nextPage = CreateButton(_cargoFooter, T("NextPage"), ListWidth - 82f, 0f, 82f, () => ChangePage(1), FooterControlsHeight);
        }

        private void CreateQianKunPanel()
        {
            RectTransform panel = CreateSidePanel("qiankun-status", T("QianKunTitle"), 54f, 132f);
            UIButton help = CreateButton(panel, "?", SideWidth - 24f, 8f, 24f, null, 24f)
                .GetComponent<UIButton>();
            help.name = "qiankun-help";
            SetButtonTooltip(help, T("QianKunHelpTitle"),
                string.Format(T("QianKunHelp"), OrbitalRingStorageCalculate.StorageHardLimit.ToString("N0")));

            _qianKunTitle = Label(panel, "", 16, 0f, 44f, SideWidth, 26f, Accent);
            _qianKunTitle.resizeTextForBestFit = true;
            _qianKunTitle.resizeTextMinSize = 11;
            _qianKunTitle.resizeTextMaxSize = 16;
            _qianKunCount = Label(panel, "", 13, 0f, 71f, SideWidth, 21f, Secondary);
            _qianKunHeld = Label(panel, "", 13, 0f, 97f, SideWidth, 29f, Secondary, TextAnchor.UpperLeft);
        }

        private void CreateLimitPanel()
        {
            RectTransform panel = CreateSidePanel("limit-panel", T("LimitColumn"), CargoTop, 254f);
            const float pickerWidth = 104f;
            UIButton picker = CreateButton(panel, T("PickItem"), SideWidth - pickerWidth, 8f, pickerWidth,
                PickItem, 24f).GetComponent<UIButton>();
            picker.name = "pick-limit-item";
            SetButtonTooltip(picker, T("PickItem"), T("PickItemHint"));
            _editorIdle = Label(panel, T("LimitIdle"), 14, 0f, 48f, SideWidth, 104f, Secondary, TextAnchor.UpperLeft);
            _editor = Panel("limit-editor", panel, 0f, 44f, SideWidth, 206f);
            _editorIcon = Panel("selected-item-icon", _editor, 0f, 0f, 32f, 32f).gameObject.AddComponent<Image>();
            _editorIcon.raycastTarget = false;
            _editorIcon.preserveAspect = true;
            _editorTitle = Label(_editor, "", 16, 40f, 0f, SideWidth - 40f, 32f, TextColor);
            _editorTitle.resizeTextForBestFit = true;
            _editorTitle.resizeTextMinSize = 11;
            _editorTitle.resizeTextMaxSize = 16;

            const float columnGap = 12f;
            const float columnWidth = (SideWidth - columnGap) / 2f;
            const float secondColumn = columnWidth + columnGap;
            Label(_editor, T("CurrentLimit"), 13, 0f, 40f, columnWidth, 20f, Secondary);
            Label(_editor, T("DefaultLimit"), 13, secondColumn, 40f, columnWidth, 20f, Secondary);
            _currentLimit = Label(_editor, "", 16, 0f, 58f, columnWidth, 30f, Accent);
            _defaultLimit = Label(_editor, "", 16, secondColumn, 58f, columnWidth, 30f, TextColor);
            _currentLimit.horizontalOverflow = HorizontalWrapMode.Overflow;
            _defaultLimit.horizontalOverflow = HorizontalWrapMode.Overflow;
            _currentLimit.verticalOverflow = VerticalWrapMode.Overflow;
            _defaultLimit.verticalOverflow = VerticalWrapMode.Overflow;
            Line(_editor, 0f, 89f, SideWidth, 1f, Divider);

            Label(_editor, T("NewLimit"), 13, 0f, 94f, SideWidth, 20f, Secondary);
            _limitInput = CreateInput(_editor, "limit-input", T("InputLimit"), 0f, 115f, SideWidth, true);
            _limitInput.onValueChanged.AddListener(_ => RefreshApplyButton());
            _reset = CreateButton(_editor, T("Default"), 0f, 154f, columnWidth,
                () => SetLimit(OrbitalRingStorageCalculate.GetDefaultItemStorageLimit(_selectedItemId)), 28f);
            _apply = CreateButton(_editor, T("Apply"), secondColumn, 154f, columnWidth, ApplyLimit, 28f);
            _editorHint = Label(_editor, "", 12, 0f, 185f, SideWidth, 20f, Secondary);
        }

        private RectTransform CreateSidePanel(string name, string title, float top, float height)
        {
            RectTransform panel = Panel(name, _window, SideLeft, top, SideWidth, height);
            Label(panel, title, 16, 0f, 8f, SideWidth - 32f, 24f, TextColor);
            Line(panel, 0f, 36f, SideWidth, 1f, Divider);
            return panel;
        }

        public override void _OnClose()
        {
            CancelSplit();
            if (_pickerOpen) {
                UIItemPicker.Close();
                _pickerOpen = false;
            }
            if (_ownsInventory) {
                _ownsInventory = false;
                UIRoot.instance.uiGame.ShutPlayerInventory();
            }
        }

        internal void HandleSplitEscape()
        {
            if (_splitItemId == 0 || !VFInput.escape) return;
            VFInput.UseEscape();
            CancelSplit();
        }

        public override void _OnUpdate()
        {
            OrbitalRingStorageData storage = GetCurrentStorage();
            if (storage == null || VFInput.escape) {
                if (VFInput.escape) VFInput.UseEscape();
                _Close();
                return;
            }

            if (_splitItemId != 0) {
                if (!UIRoot.instance.uiGame.gridSplit.active) {
                    _splitItemId = 0;
                } else if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
                    FinishSplit();
                }
            }

            if (((RectTransform)_window.parent).rect.size != _layoutBoundsSize) {
                LayoutWindow();
                _refreshFrame = 0;
            } else if (++_refreshFrame % 15 == 0) {
                RefreshStorage();
            }
            UpdateHeldItem(storage);
            if (!UIRoot.instance.uiGame.inventoryWindow.active) _ownsInventory = false;
        }

        private OrbitalRingStorageData GetCurrentStorage()
        {
            if (GameMain.mainPlayer?.factory?.planetId != _planetId) return null;
            PlanetOrbitalRingData planet = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(_planetId);
            if (planet == null || _ringIndex < 0 || _ringIndex >= planet.Rings.Count) return null;
            var ring = planet.Rings[_ringIndex];
            return ring.IsOneFull() ? ring.orbitalRingStorage : null;
        }

        private void RefreshStorage(bool resetLimitInput = false)
        {
            OrbitalRingStorageData storage = GetCurrentStorage();
            if (storage == null) return;

            _entries.Clear();
            StorageEntry selected = default;
            lock (storage.storageItem) {
                foreach (var pair in storage.storageItem) {
                    if (pair.Key > 0 && (pair.Value[0] > 0 || storage.storageLimit.ContainsKey(pair.Key))) {
                        _entries.Add(new StorageEntry(storage, pair.Key, pair.Value));
                    }
                }
                foreach (int itemId in storage.storageLimit.Keys) {
                    if (itemId > 0 && !storage.storageItem.ContainsKey(itemId)) {
                        _entries.Add(new StorageEntry(storage, itemId, null));
                    }
                }
                if (_selectedItemId != 0) {
                    storage.storageItem.TryGetValue(_selectedItemId, out int[] stack);
                    selected = new StorageEntry(storage, _selectedItemId, stack);
                }
            }

            _entries.Sort((a, b) => a.ItemId.CompareTo(b.ItemId));
            RefreshList();
            UpdateQianKunStatus();
            UpdateHeldItem(storage, true);
            UpdateEditor(selected, resetLimitInput);
        }

        private void RefreshList()
        {
            _filteredEntries.Clear();
            string query = _search.text.Trim();
            foreach (StorageEntry entry in _entries) {
                if (query.Length != 0 && ItemName(entry.ItemId).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    entry.ItemId.ToString().IndexOf(query, StringComparison.Ordinal) < 0) continue;
                _filteredEntries.Add(entry);
            }

            int count = _filteredEntries.Count;
            _pageSize = GetPageSize();
            int pageCount = Math.Max(1, (count + _pageSize - 1) / _pageSize);
            int anchorIndex = _filteredEntries.FindIndex(entry => entry.ItemId >= _pageAnchorItemId);
            _page = (anchorIndex < 0 ? Math.Max(0, count - 1) : anchorIndex) / _pageSize;
            int first = _page * _pageSize;
            int visibleCount = Math.Min(_pageSize, count - first);
            ResizeCargoArea(visibleCount);
            BindPage(first, visibleCount);

            _listEmptyTitle.gameObject.SetActive(count == 0);
            _listEmptyBody.gameObject.SetActive(count == 0);
            if (count == 0) {
                bool filtered = query.Length != 0;
                _listEmptyTitle.text = filtered ? T("NoMatches") : T("Empty");
                _listEmptyBody.text = filtered ? T("NoMatchesHint") : T("EmptyHint");
            }
            _previousPage.gameObject.SetActive(pageCount > 1);
            _nextPage.gameObject.SetActive(pageCount > 1);
            _previousPage.interactable = _page > 0;
            _nextPage.interactable = _page + 1 < pageCount;
            _itemCount.text = pageCount > 1 ? string.Format(T("Page"), count, _page + 1, pageCount)
                : string.Format(T("ItemCount"), count);
        }

        private void ChangeSearch()
        {
            _pageAnchorItemId = 0;
            if (string.IsNullOrWhiteSpace(_search.text)) {
                _pageAnchors.TryGetValue((_planetId, _ringIndex), out _pageAnchorItemId);
            }
            RefreshList();
        }

        private void ChangePage(int delta)
        {
            int first = (_page + delta) * _pageSize;
            if (first < 0 || first >= _filteredEntries.Count) return;

            _pageAnchorItemId = _filteredEntries[first].ItemId;
            if (string.IsNullOrWhiteSpace(_search.text)) {
                _pageAnchors[(_planetId, _ringIndex)] = _pageAnchorItemId;
            }
            RefreshList();
        }

        private int GetPageSize()
        {
            var parent = (RectTransform)_window.parent;
            float availableHeight = (parent.rect.height - 2f * ScreenMargin) / _window.localScale.y;
            int rows = Mathf.FloorToInt((availableHeight - CargoTop - FooterHeight) / CargoStride);
            return Mathf.Clamp(rows, 1, MaxPageSize / CargoColumns) * CargoColumns;
        }

        private void ResizeCargoArea(int itemCount)
        {
            int rows = (itemCount + CargoColumns - 1) / CargoColumns;
            float viewportHeight = rows == 0 ? MinimumHeight - CargoTop - FooterHeight : rows * CargoStride;
            _cargoContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);

            float height = Mathf.Max(MinimumHeight, CargoTop + viewportHeight + FooterHeight);
            if (_window.rect.height != height) {
                _window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                ClampWindowPosition();
            }
            _cargoFooter.anchoredPosition = new Vector2(WindowPadding, FooterControlsHeight + WindowPadding - height);
        }

        private void BindPage(int first, int count)
        {
            while (_tiles.Count < count) _tiles.Add(CreateCargoTile(_tiles.Count));
            CargoTile selectedTile = null;
            for (int i = 0; i < _tiles.Count; i++) {
                CargoTile tile = _tiles[i];
                bool visible = i < count;
                tile.Rect.gameObject.SetActive(visible);
                if (!visible) continue;

                BindTile(tile, _filteredEntries[first + i]);
                if (tile.ItemId == _selectedItemId) selectedTile = tile;
            }
            if (selectedTile != null) {
                // The native stock bar extends past the icon on its right edge.
                _selectionFill.anchoredPosition = _selectionFrame.anchoredPosition =
                    selectedTile.Rect.anchoredPosition + new Vector2(-2f, 2f);
            }
            _selectionFill.gameObject.SetActive(selectedTile != null);
            _selectionFrame.gameObject.SetActive(selectedTile != null);
        }

        private CargoTile CreateCargoTile(int index)
        {
            float x = CargoPadding + index % CargoColumns * CargoStride;
            float y = CargoPadding + index / CargoColumns * CargoStride;
            RectTransform rect = Panel("item-tile-" + index, _cargoContent, x, y, CargoSize, CargoSize);
            rect.SetSiblingIndex(index + 1);
            var tile = new CargoTile { Rect = rect };
            tile.Cargo = CreateCargoSlot(rect, 4f, 4f, 1f, () => tile.ItemId);
            return tile;
        }

        private void BindTile(CargoTile tile, StorageEntry entry)
        {
            tile.ItemId = entry.ItemId;
            BindCargoSlot(tile.Cargo, entry.ItemId, entry.Count, entry.Inc, entry.Limit);
        }

        private void UpdateQianKunStatus()
        {
            StorageEntry first = default;
            int count = 0;
            foreach (StorageEntry entry in _entries) {
                if (!entry.IsOverflow) continue;
                if (count++ == 0) first = entry;
            }

            _qianKunTitle.text = count == 0 ? T("NoOverflow") : string.Format(T("CurrentOverflow"), ItemName(first.ItemId));
            _qianKunCount.text = count == 0 ? "" : string.Format(T("OverflowQuantity"), first.Count.ToString("N0"));
            if (count > 1) _qianKunCount.text += string.Format(T("OtherOverflow"), count - 1);
            _qianKunTitle.color = count == 0 ? TextColor : Warning;
        }

        private void UpdateHeldItem(OrbitalRingStorageData storage, bool force = false)
        {
            Player player = GameMain.mainPlayer;
            int count = player?.inhandItemCount ?? 0;
            int itemId = count > 0 ? player.inhandItemId : 0;
            int inc = count > 0 ? player.inhandItemInc : 0;
            if (!force && itemId == _lastHandItemId && count == _lastHandCount && inc == _lastHandInc) return;
            _lastHandItemId = itemId;
            _lastHandCount = count;
            _lastHandInc = inc;

            _emptySlot.gameObject.SetActive(itemId == 0);
            _heldCargo.View.gameObject.SetActive(itemId != 0);
            _depositTitle.text = itemId == 0 ? T("DepositTitle") : string.Format(T("DepositHeldTitle"), ItemName(itemId));
            _depositHint.text = itemId == 0 ? T("DepositHint") : T("DepositHeldHint");
            if (itemId != 0) BindCargoSlot(_heldCargo, itemId, count, inc, 0);

            int oreId = TheMountainMovingMappings.GetOreId(itemId);
            _qianKunHeld.text = "";
            if (oreId == itemId) return;

            int available;
            lock (storage.storageItem) {
                int limit = OrbitalRingStorageCalculate.GetQianKunDepositLimit(storage, oreId);
                storage.storageItem.TryGetValue(oreId, out int[] stack);
                available = limit - (stack?[0] ?? 0);
            }
            _qianKunHeld.text = available > 0 && available < count
                ? string.Format(T("HeldQianKunPartial"), available.ToString("N0"), count.ToString("N0"))
                : string.Format(available > 0 ? T("HeldQianKunAllowed") : T("HeldQianKunBlocked"), ItemName(itemId));
            _qianKunHeld.color = available > 0 ? Accent : Warning;
        }

        private void SelectItem(int itemId)
        {
            if (itemId == 0 || itemId == _selectedItemId) return;
            _selectedItemId = itemId;
            RefreshStorage(true);
        }

        private void PickItem()
        {
            if (_pickerOpen) {
                UIItemPicker.Close();
                return;
            }

            RectTransform picker = UIRoot.instance.uiGame.itemPicker.pickerTrans;
            var parent = (RectTransform)picker.parent;
            Rect bounds = parent.rect;
            Vector3 windowCenter = _window.TransformPoint(_window.rect.center);
            Vector2 centerOffset = (Vector2)parent.InverseTransformPoint(windowCenter) - bounds.center;
            Vector2 available = Vector2.Max(Vector2.zero, (bounds.size - picker.rect.size) / 2f);
            centerOffset.x = Mathf.Clamp(centerOffset.x, -available.x, available.x);
            centerOffset.y = Mathf.Clamp(centerOffset.y, -available.y, available.y);

            Vector2 pickerCenter = parent.InverseTransformPoint(picker.TransformPoint(picker.rect.center));
            Vector2 position = picker.anchoredPosition + (bounds.center + centerOffset - pickerCenter);
            _pickerOpen = true;
            UIItemPicker.Popup(position, item => {
                _pickerOpen = false;
                if (_pickerDragHandle != null) _pickerDragHandle.SetActive(false);
                if (active && item != null) SelectItem(item.ID);
            });
            if (_pickerOpen) {
                if (_pickerDragHandle == null) _pickerDragHandle = CreatePickerDragHandle(picker, parent);
                _pickerDragHandle.SetActive(true);
            }
        }

        private static GameObject CreatePickerDragHandle(RectTransform picker, RectTransform bounds)
        {
            const float titleHeight = 40f;
            const float closeButtonArea = 48f;
            RectTransform handle = Panel("storage-picker-drag", picker, 0f, 0f, 0f, titleHeight);
            handle.anchorMax = Vector2.one;
            handle.sizeDelta = new Vector2(-closeButtonArea, titleHeight);

            GameObject target = handle.gameObject;
            target.SetActive(false);
            target.AddComponent<Image>().color = Color.clear;
            var trigger = target.AddComponent<EventTrigger>();
            var drag = target.AddComponent<UIWindowDrag>();
            drag.dragTrigger = trigger;
            drag.dragTrans = drag.refTrans = picker;
            drag.screenRect = bounds;
            drag.hasPopupAudio = false;
            return target;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                RectTransformUtility.RectangleContainsScreenPoint(_depositSurface, eventData.position, eventData.pressEventCamera)) {
                DepositHeldItem();
            }
        }

        private void DepositHeldItem()
        {
            Player player = GameMain.mainPlayer;
            OrbitalRingStorageData storage = GetCurrentStorage();
            if (storage == null || player == null || player.inhandItemId <= 0 || player.inhandItemCount <= 0) return;

            if (NebulaAPI.NebulaModAPI.IsMultiplayerActive) {
                MultiplayerTransfers.Deposit(_planetId, _ringIndex, player);
                RefreshStorage();
                return;
            }

            if (OrbitalRingStorageCalculate.DepositHandItems(storage, player) == 0) {
                UIRealtimeTip.Popup(T("DepositBlocked"));
                return;
            }
            RefreshStorage();
        }

        private void StartSplit(int itemId, Vector2 position)
        {
            Player player = GameMain.mainPlayer;
            OrbitalRingStorageData storage = GetCurrentStorage();
            UIGame ui = UIRoot.instance.uiGame;
            if (storage == null || player == null || player.inhandItemCount > 0 || ui.gridSplit.active) return;

            int count;
            lock (storage.storageItem) {
                if (!storage.storageItem.TryGetValue(itemId, out int[] stack) || stack[0] <= 0) return;
                count = stack[0];
            }
            ui.OpenGridSplit(itemId, count, position);
            if (ui.gridSplit.active) _splitItemId = itemId;
        }

        private void FinishSplit()
        {
            int itemId = _splitItemId;
            _splitItemId = 0;
            UIGame ui = UIRoot.instance.uiGame;
            if (itemId != 0 && ui.gridSplit.active) WithdrawToHand(itemId, ui.CloseGridSplit());
        }

        private void CancelSplit()
        {
            if (_splitItemId == 0) return;
            _splitItemId = 0;
            if (UIRoot.instance.uiGame.gridSplit.active) UIRoot.instance.uiGame.CloseGridSplit();
        }

        private void WithdrawToHand(int itemId, int requestedCount)
        {
            Player player = GameMain.mainPlayer;
            OrbitalRingStorageData storage = GetCurrentStorage();
            if (storage == null || player == null || player.inhandItemCount > 0) return;
            if (NebulaAPI.NebulaModAPI.IsMultiplayerActive) {
                MultiplayerTransfers.Withdraw(_planetId, _ringIndex, itemId, requestedCount, player);
                RefreshStorage();
                return;
            }

            int count = OrbitalRingStorageCalculate.WithdrawItem(storage, itemId, requestedCount, out int inc);
            if (count <= 0) return;

            player.SetHandItemId_Unsafe(itemId);
            player.SetHandItemCount_Unsafe(count);
            player.SetHandItemInc_Unsafe(inc);
            RefreshStorage();
        }

        private void UpdateEditor(StorageEntry item, bool resetInput)
        {
            bool hasSelection = item.ItemId != 0;
            _editorIdle.gameObject.SetActive(!hasSelection);
            _editor.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            _editorIcon.sprite = LDB.items.Select(item.ItemId)?.iconSprite;
            _editorIcon.enabled = _editorIcon.sprite != null;
            _editorTitle.text = ItemName(item.ItemId);
            _appliedLimit = item.Limit;
            _currentLimit.text = _appliedLimit.ToString("N0");
            _defaultLimit.text = OrbitalRingStorageCalculate.GetDefaultItemStorageLimit(item.ItemId).ToString("N0");
            _editorHint.text = item.IsOverflow ? T("Overflow") : string.Format(T("LimitRange"),
                OrbitalRingStorageCalculate.StorageHardLimit.ToString("N0"));
            _editorHint.color = item.IsOverflow ? Warning : Secondary;
            if (resetInput) _limitInput.SetTextWithoutNotify(_appliedLimit.ToString());
            _limitInput.interactable = !item.IsOverflow;
            _reset.interactable = !item.IsOverflow && item.HasCustomLimit;
            RefreshApplyButton();
        }

        private void RefreshApplyButton()
        {
            _apply.interactable = _limitInput.interactable && long.TryParse(_limitInput.text, out long limit) &&
                ClampLimit(limit) != _appliedLimit;
        }

        private void ApplyLimit()
        {
            if (!_limitInput.interactable) return;
            if (!long.TryParse(_limitInput.text, out long limit)) {
                UIRealtimeTip.Popup(T("InvalidLimit"));
                return;
            }
            SetLimit(ClampLimit(limit));
        }

        private static int ClampLimit(long limit) =>
            (int)Math.Max(0, Math.Min(OrbitalRingStorageCalculate.StorageHardLimit, limit));

        private void SetLimit(int limit)
        {
            OrbitalRingStorageData storage = GetCurrentStorage();
            if (storage == null || _selectedItemId == 0) return;
            var setting = new SyncOrbitalStorageSetting {
                Kind = 1, PlanetId = _planetId, RingId = _ringIndex, ItemId = _selectedItemId,
                Value = limit == OrbitalRingStorageCalculate.GetDefaultItemStorageLimit(_selectedItemId) ? -1 : limit
            };
            if (MultiplayerSync.IsClient) {
                _limitInput.SetTextWithoutNotify(limit.ToString());
                RefreshApplyButton();
                MultiplayerSync.Send(setting);
                return;
            }

            bool applied = OrbitalRingStorageCalculate.TrySetItemStorageLimit(storage, _selectedItemId, limit);
            if (applied) MultiplayerSync.Send(setting);
            RefreshStorage(applied);
        }

        private void OpenInventory()
        {
            UIGame ui = UIRoot.instance.uiGame;
            if (!ui.inventoryWindow.active) {
                ui.OpenPlayerInventory();
                _ownsInventory = ui.inventoryWindow.active;
            }
        }

        private void LayoutWindow()
        {
            var parent = (RectTransform)_window.parent;
            UIInventoryWindow inventory = UIRoot.instance.uiGame.inventoryWindow;
            Rect bounds = parent.rect;
            _layoutBoundsSize = bounds.size;
            float scale = Mathf.Min(1f, (bounds.width - 2f * ScreenMargin) / Width,
                (bounds.height - 2f * ScreenMargin) / MinimumHeight);
            float leftEdge = bounds.xMin + ScreenMargin;
            float x = bounds.center.x - Width * scale / 2f;
            float top = bounds.center.y;
            if (inventory.active) {
                Rect inventoryBounds = ClampInventoryPosition(parent, inventory.windowTrans, bounds);
                float spaceLeft = inventoryBounds.xMin - leftEdge - ScreenMargin;
                float spaceRight = bounds.xMax - inventoryBounds.xMax - 2f * ScreenMargin;
                bool dockRight = spaceRight >= spaceLeft;
                float dockSpace = dockRight ? spaceRight : spaceLeft;
                float maxDockSpace = bounds.width - inventoryBounds.width - 3f * ScreenMargin;
                float minimumDockSpace = Width * 0.7f;
                if (dockSpace < minimumDockSpace && maxDockSpace >= minimumDockSpace) {
                    float inventoryLeft = dockRight ? leftEdge : bounds.xMax - ScreenMargin - inventoryBounds.width;
                    float offset = inventoryLeft - inventoryBounds.xMin;
                    inventory.windowTrans.position += parent.TransformVector(new Vector3(offset, 0f, 0f));
                    inventoryBounds.x = inventoryLeft;
                    dockSpace = maxDockSpace;
                }
                if (dockSpace >= minimumDockSpace) scale = Mathf.Min(scale, dockSpace / Width);
                x = dockRight ? inventoryBounds.xMax + ScreenMargin : inventoryBounds.xMin - ScreenMargin - Width * scale;
                top = inventoryBounds.yMax;
            }

            _window.localScale = Vector3.one * scale;
            RefreshStorage();
            if (!inventory.active) top += _window.rect.height * scale / 2f;
            _window.anchoredPosition = new Vector2(x, top) - bounds.center;
            ClampWindowPosition();
        }

        private Rect ClampInventoryPosition(RectTransform parent, RectTransform inventory, Rect bounds)
        {
            inventory.GetWorldCorners(_inventoryCorners);
            Vector2 min = parent.InverseTransformPoint(_inventoryCorners[0]);
            Vector2 max = parent.InverseTransformPoint(_inventoryCorners[2]);
            Vector2 size = max - min;
            Vector2 lower = bounds.min + Vector2.one * ScreenMargin;
            Vector2 upper = Vector2.Max(lower, bounds.max - size - Vector2.one * ScreenMargin);
            var position = new Vector2(Mathf.Clamp(min.x, lower.x, upper.x), Mathf.Clamp(min.y, lower.y, upper.y));
            Vector2 offset = position - min;
            if (offset != Vector2.zero) inventory.position += parent.TransformVector(offset);
            return new Rect(position, size);
        }

        private void ClampWindowPosition()
        {
            Rect bounds = ((RectTransform)_window.parent).rect;
            Vector2 size = _window.rect.size * _window.localScale.x;
            Vector2 position = _window.anchoredPosition + bounds.center;
            position.x = Mathf.Clamp(position.x, bounds.xMin + ScreenMargin, bounds.xMax - size.x - ScreenMargin);
            position.y = Mathf.Clamp(position.y, bounds.yMin + size.y + ScreenMargin, bounds.yMax - ScreenMargin);
            _window.anchoredPosition = position - bounds.center;
        }

        private static string ItemName(int itemId) => LDB.items.Select(itemId)?.name ?? itemId.ToString();

        private CargoSlot CreateCargoSlot(RectTransform parent, float x, float y, float scale, Func<int> itemId)
        {
            UIControlPanelStorageItem slot = Instantiate(_cargoPrefab, parent, false);
            // The control panel entry owns the station binding. The cargo graphics bind to ring storage.
            slot.enabled = false;
            slot.name = "cargo-slot";
            NormalizeRectWithTopLeft(slot.rectTransform, x, y).localScale = Vector3.one * scale;
            slot.SetVisible(true);
            slot.inTransitImage.gameObject.SetActive(false);
            slot.logiStateText.gameObject.SetActive(false);
            slot.itemIconImage.raycastTarget = true;
            slot.itemAmountText.raycastTarget = false;
            if (slot.itemButton.button != null) slot.itemButton.button.onClick = new Button.ButtonClickedEvent();
            slot.itemButton.onMouseDown += (_, button) => {
                if (button == PointerEventData.InputButton.Left) {
                    int targetId = itemId();
                    if (targetId != 0 && GameMain.mainPlayer?.inhandItemCount == 0) {
                        SelectItem(targetId);
                    } else {
                        DepositHeldItem();
                    }
                }
                if (button == PointerEventData.InputButton.Right) StartSplit(itemId(), Input.mousePosition);
            };
            slot.gameObject.SetActive(true);

            var marks = new Image[_incPrefabs.Length];
            for (int i = 0; i < marks.Length; i++) {
                Image mark = Instantiate(_incPrefabs[i], slot.rectTransform, false);
                Vector2 size = _incPrefabs[i].rectTransform.rect.size;
                NormalizeRectWithTopLeft(mark, 1f, 1f).sizeDelta = size * Mathf.Min(1f, 17f / Mathf.Max(size.x, size.y));
                mark.raycastTarget = false;
                mark.enabled = false;
                mark.gameObject.SetActive(true);
                marks[i] = mark;
            }
            return new CargoSlot { View = slot, IncMarks = marks };
        }

        private static void BindCargoSlot(CargoSlot cargo, int itemId, int count, int inc, int limit)
        {
            UIControlPanelStorageItem slot = cargo.View;
            slot.itemIconImage.sprite = LDB.items.Select(itemId)?.iconSprite;
            slot.itemIconImage.enabled = slot.itemIconImage.sprite != null;
            slot.itemAmountText.text = count > 0 ? count.ToString() : "";
            RectTransform bar = slot.currentStorageImage.rectTransform;
            bar.gameObject.SetActive(limit > 0);
            if (limit > 0) {
                bar.sizeDelta = new Vector2(bar.sizeDelta.x, Mathf.Clamp01((float)count / limit) * slot.rectTransform.rect.height);
            }
            int pointsPerItem = count > 0 ? Mathf.Clamp(inc / count, 0, 10) : 0;
            int arrows = Cargo.fastIncArrowTable[pointsPerItem];
            for (int i = 0; i < cargo.IncMarks.Length; i++) {
                cargo.IncMarks[i].enabled = arrows == i + 1;
            }

            UIButton button = slot.itemButton;
            bool tipChanged = button.tips.itemId != itemId || button.tips.itemCount != count || button.tips.itemInc != inc;
            button.tips.itemId = itemId;
            button.tips.itemCount = count;
            button.tips.itemInc = inc;
            button.tips.type = UIButton.ItemTipType.Item;
            if (tipChanged && button.tipShowing) button.UpdateTip();
        }

        private InputField CreateInput(RectTransform parent, string name, string placeholder, float x, float y, float width, bool numeric)
        {
            RectTransform rect = Panel(name, parent, x, y, width, 32f);
            InputField input = Instantiate(_inputPrefab, rect, false);
            if (input.targetGraphic is Image inputBackground) {
                inputBackground.sprite = null;
                inputBackground.overrideSprite = null;
                inputBackground.type = Image.Type.Simple;
                inputBackground.material = Graphic.defaultGraphicMaterial;
                inputBackground.color = InputFill;
                input.transition = Selectable.Transition.None;
            }
            CreateFrame(rect, width, 32f, Border);
            NormalizeRectWithMargin(input, 0f, 0f, 0f, 0f);
            RectTransform inputRect = (RectTransform)input.transform;
            var viewport = input.textComponent.rectTransform.parent as RectTransform;
            if (viewport != inputRect) NormalizeRectWithMargin(viewport, 0f, 8f, 0f, 8f, inputRect);
            float padding = viewport == inputRect ? 8f : 0f;
            NormalizeRectWithMargin(input.textComponent, 0f, padding, 0f, padding, viewport);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            if (input.placeholder != null) input.placeholder.gameObject.SetActive(false);
            input.placeholder = Label(inputRect, placeholder, 14, 8f, 0f, width - 16f, 32f, Secondary);
            input.onValueChanged = new InputField.OnChangeEvent();
            input.onEndEdit = new InputField.EndEditEvent();
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = numeric ? 10 : 40;
            input.contentType = numeric ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
            input.SetTextWithoutNotify("");
            if (numeric) {
                input.onEndEdit.AddListener(_ => {
                    if (!input.wasCanceled && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) {
                        ApplyLimit();
                    }
                });
            }
            return input;
        }

        private static RectTransform Panel(string name, RectTransform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            NormalizeRectWithTopLeft(rect, x, y, parent);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private void Line(RectTransform parent, float x, float y, float width, float height, Color color)
        {
            var line = Panel("line", parent, x, y, width, height).gameObject.AddComponent<Image>();
            line.color = color;
            line.raycastTarget = false;
        }

        private void CreateFrame(RectTransform parent, float width, float height, Color color, float thickness = 1f)
        {
            Line(parent, 0f, 0f, width, thickness, color);
            Line(parent, 0f, height - thickness, width, thickness, color);
            Line(parent, 0f, thickness, thickness, height - 2f * thickness, color);
            Line(parent, width - thickness, thickness, thickness, height - 2f * thickness, color);
        }

        private Text Label(RectTransform parent, string value, int size, float x, float y, float width, float height,
            Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Panel("text", parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _bodyFont;
            text.fontSize = size;
            text.text = value;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private Button CreateButton(RectTransform parent, string label, float x, float y, float width, Action action, float height = 32f)
        {
            UIButton nativeButton = Util.CreateButton(label, width, height);
            NormalizeRectWithTopLeft(nativeButton, x, y, parent);
            var text = nativeButton.transform.Find("Text").GetComponent<Text>();
            text.font = _bodyFont;
            text.fontSize = 14;
            nativeButton.button.onClick = new Button.ButtonClickedEvent();
            if (action != null) nativeButton.button.onClick.AddListener(() => action());
            return nativeButton.button;
        }

        private static void SetButtonTooltip(UIButton button, string title, string content)
        {
            button.tips.tipTitle = title;
            button.tips.tipText = content;
            button.tips.corner = 1;
            button.tips.offset = new Vector2(-8f, -4f);
            button.tips.width = 310;
            button.tips.delay = 0.25f;
            button.tips.topLevel = true;
        }
    }
}
