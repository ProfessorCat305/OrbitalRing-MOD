using ProjectOrbitalRing.Patches.UI.Utils;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static ProjectOrbitalRing.Patches.UI.Utils.MyWindowCtl;
using static ProjectOrbitalRing.Patches.UI.Utils.Util;

namespace ProjectOrbitalRing.Patches.UI.UIOrbitalRingStorageWindow
{
    public class UIOrbitalRingStorageWindow: ManualBehaviour
    {
        const int StorageXCount = 10;
        const int StorageYCount = 10;
        private readonly UIButton[] _iconBtns = new UIButton[StorageXCount * StorageYCount];
        private readonly Image[] _iconImgs = new Image[StorageXCount * StorageYCount];
        private readonly Text[] _iconTexts = new Text[StorageXCount * StorageYCount];
        public RectTransform windowTrans;
        private RectTransform _tab1;

        private static Sprite _tagNotSelectedSprite;

        internal static UIOrbitalRingStorageWindow CreateWindow() =>
            CreateWindow<UIOrbitalRingStorageWindow>("UIOrbitalRingStorageWindow", "星环共享空间".TranslateFromJson());

        public void OpenWindow() => MyWindowCtl.OpenWindow(this);

        public override void _OnCreate()
        {
            windowTrans = GetRectTransform(this);
            windowTrans.sizeDelta = new Vector2(1170f, 750f);

            CreateUI();
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
                    _iconBtns[i * 10 + j] = iconBtn;
                    _iconImgs[i * 10 + j] = iconImage;
                    _iconTexts[i * 10 + j] = CreateText("", 16);

                    NormalizeRectWithTopLeft(iconBtn.transform, 0 + j * 110, 60 + i * 60, _tab1);
                    NormalizeRectWithTopLeft(_iconTexts[i * 10 + j].transform, 55 + j * 110, 72 + i * 60, _tab1);
                }
            }
            _tagNotSelectedSprite = _iconImgs[0].sprite;
        }

        public void SetStorageData(Dictionary<int, int[]> storageItem)
        {
            int count = Math.Min(storageItem.Count, StorageXCount * StorageYCount);
            int i = 0;
            foreach (var kvp in storageItem) {
                if (i >= StorageXCount * StorageYCount) {
                    break;
                }
                ItemProto proto = LDB.items.Select(kvp.Key);
                _iconTexts[i].text = kvp.Value[0].ToString().Translate();
                Sprite sprite = proto.iconSprite;

                if (sprite != null) _iconImgs[i].sprite = sprite;
                _iconBtns[i].tips.itemId = kvp.Key;
                i++;
            }
            for (; i < StorageXCount * StorageYCount; i++) {
                _iconImgs[i].sprite = _tagNotSelectedSprite;
                _iconTexts[i].text = "";
                _iconBtns[i].tips.itemId = 0;
            }
        }
    }
}
