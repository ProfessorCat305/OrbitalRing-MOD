using System.Collections.Generic;
using System.Linq;
using CommonAPI.Systems.ModLocalization;
using UnityEngine;
using static ProjectOrbitalRing.Utils.JsonHelper;

namespace ProjectOrbitalRing.Utils
{
    internal static class TranslateUtils
    {
        private static readonly Dictionary<string, StringProtoJson> StringProtoJsons =
            GetJsonContent<StringProtoJson>("strings").ToDictionary(i => i.Name);

        internal static void RegisterStrings()
        {
            foreach ((string key, StringProtoJson value) in StringProtoJsons)
                LocalizationModule.RegisterTranslation(key, new Dictionary<string, string> {
                    ["2052"] = value.ZHCN, ["1033"] = value.ENUS,
                    ["1036"] = value.FRFR, ["1031"] = value.DEDE, ["1041"] = value.JAJP
                });
        }

        public static string TranslateFromJsonSpecial(this string s) => s.TranslateFromJson();

        public static string TranslateFromJson(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (!StringProtoJsons.TryGetValue(s, out StringProtoJson stringProtoJson)) return s;
            switch (CurrentLanguage()) {
                case 2052: return stringProtoJson.ZHCN;
                case 1036: return stringProtoJson.FRFR;
                case 1031: return stringProtoJson.DEDE;
                case 1041: return stringProtoJson.JAJP;
                default: return stringProtoJson.ENUS;
            }
        }

        private static int CurrentLanguage()
        {
            if (Localization.Loaded) return Localization.CurrentLanguageLCID;
            int selected = PlayerPrefs.GetInt("language", 0);
            if (selected > 2) return selected;
            switch (Application.systemLanguage) {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional: return 2052;
                case SystemLanguage.French: return 1036;
                case SystemLanguage.German: return 1031;
                case SystemLanguage.Japanese: return 1041;
                default: return 1033;
            }
        }
    }
}
