using HarmonyLib;
using ProjectOrbitalRing.Patches.UI.Utils;
using ProjectOrbitalRing.Utils;

// ReSharper disable InconsistentNaming

namespace ProjectOrbitalRing.Patches.UI.PlanetFocus
{
    public static class UIPlanetDetailExpand
    {
        private static UIButton _planetFocusBtn;

        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnInit))]
        [HarmonyPostfix]
        public static void Init(UIGame __instance)
        {
            if (_planetFocusBtn) return;

            ProjectOrbitalRing.PlanetFocusWindow = UIPlanetFocusWindow.CreateWindow();

            _planetFocusBtn = Util.CreateButton("星球特质".TranslateFromJson());
            Util.NormalizeRectWithTopLeft(_planetFocusBtn, 5, -40, __instance.planetDetail.rectTrans);
            _planetFocusBtn.onClick += _ => ProjectOrbitalRing.PlanetFocusWindow.OpenWindow();
        }

        [HarmonyPatch(typeof(UIPlanetDetail), nameof(UIPlanetDetail.OnPlanetDataSet))]
        [HarmonyPostfix]
        public static void OnPlanetDataSet_Postfix(UIPlanetDetail __instance)
        {
            if (__instance.planet == null)
            {
                ProjectOrbitalRing.PlanetFocusWindow._Close();

                return;
            }

            bool notgas = __instance.planet.type != EPlanetType.Gas;

            if (_planetFocusBtn) _planetFocusBtn.gameObject.SetActive(notgas);

            if (notgas)
            {
                ProjectOrbitalRing.PlanetFocusWindow.nameText.text = __instance.planet.displayName + " - " + "星球特质".TranslateFromJson();
                switch (__instance.planet.theme) {
                    case 1:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 6526;
                        break;
                    case 16:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 6524;
                        break;
                    case 18:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 6527;
                        break;
                    case 7:
                    case 10:
                    case 20:
                    case 24:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 6525;
                        break;
                    case 6:
                    case 8:
                    case 14:
                    case 15:
                    case 17:
                    case 22:
                    case 25:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 6529;
                        break;
                    default:
                        ProjectOrbitalRing.PlanetFocusWindow.FocusId = 0;
                        break;
                }


                if (UIPlanetFocusWindow.CurPlanetId != __instance.planet.id) {
                    UIPlanetFocusWindow.CurPlanetId = __instance.planet.id;
                    ProjectOrbitalRing.PlanetFocusWindow.OnPlanetChanged(UIPlanetFocusWindow.CurPlanetId);
                }
            }
        }
    }
}
