using System.Text;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using Utils_ERecipeType = ProjectOrbitalRing.Utils.ERecipeType;

// ReSharper disable InconsistentNaming

namespace ProjectOrbitalRing.Patches.UI.DisplayText
{
    internal static class DisplayTextPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RecipeProto), nameof(RecipeProto.madeFromString), MethodType.Getter)]
        public static void RecipeProto_madeFromString(RecipeProto __instance, ref string __result)
        {
            var type = (Utils_ERecipeType)__instance.Type;

            switch (type)
            {
                case Utils_ERecipeType.Chemical:
                    __result = "化工厂".Translate();

                    break;

                case Utils_ERecipeType.Refine:
                    __result = "原油精炼厂".Translate();

                    break;

                case Utils_ERecipeType.Assemble:
                    __result = "基础制造台".TranslateFromJson();

                    break;

                case Utils_ERecipeType.太空船坞:
                    __result = "太空船坞".TranslateFromJson();

                    break;

                case Utils_ERecipeType.粒子打印:
                    __result = "粒子打印车间".TranslateFromJson();

                    break;

                case Utils_ERecipeType.等离子熔炼:
                    __result = "等离子熔炼".TranslateFromJson();

                    break;

                case Utils_ERecipeType.物质重组:
                    __result = "物质重组".TranslateFromJson();

                    break;

                case Utils_ERecipeType.生物化工:
                    __result = "生态穹顶".TranslateFromJson();

                    break;

                case Utils_ERecipeType.高分子化工:
                    __result = "先进化学反应釜".TranslateFromJson();

                    break;

                case Utils_ERecipeType.黑盒:
                    __result = "黑盒".TranslateFromJson();

                    break;

                case (Utils_ERecipeType)21:
                    __result = "星际组装厂（巨构）".TranslateFromJson();

                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.fuelTypeString), MethodType.Getter)]
        public static void ItemProto_fuelTypeString(ItemProto __instance, ref string __result)
        {
            int type = __instance.FuelType;

            switch (type)
            {
                case 1:
                    __result = "化学能".TranslateFromJson();
                    return;

                case 2:
                    __result = "裂变能".TranslateFromJson();
                    return;

                case 16:
                    __result = "聚变能".TranslateFromJson();
                    return;

                case 31:
                    __result = "黑雾能".TranslateFromJson();
                    return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.typeString), MethodType.Getter)]
        public static void ItemProto_typeString(ItemProto __instance, ref string __result)
        {
            if (__instance.Type != EItemType.Production) return;

            switch ((Utils_ERecipeType)__instance.prefabDesc.assemblerRecipeType)
            {
                case Utils_ERecipeType.Assemble:
                    __result = "基础制造".TranslateFromJson();
                    return;

                case Utils_ERecipeType.太空船坞:
                    __result = "太空船坞".TranslateFromJson();
                    return;

                case Utils_ERecipeType.粒子打印:
                    __result = "粒子打印".TranslateFromJson();
                    return;

                case Utils_ERecipeType.等离子熔炼:
                    __result = "等离子熔炼".TranslateFromJson();
                    return;

                case Utils_ERecipeType.物质重组:
                    __result = "物质重组".TranslateFromJson();
                    return;

                case Utils_ERecipeType.生物化工:
                    __result = "T人造生态圈".TranslateFromJson();
                    return;

                case Utils_ERecipeType.高分子化工:
                    __result = "T先进化工".TranslateFromJson();
                    return;

                case Utils_ERecipeType.所有化工:
                    __result = "复合化工".TranslateFromJson();
                    return;

                case Utils_ERecipeType.黑盒:
                    __result = "黑盒".TranslateFromJson();
                    return;

                case Utils_ERecipeType.所有熔炉:
                    __result = "复合冶炼".TranslateFromJson();
                    return;
            }
        }

        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
        [HarmonyPostfix]
        public static void GetPropValuePatch(ItemProto __instance, int index, ref string __result)
        {
            if (index >= __instance.DescFields.Length) return;

            switch (__instance.DescFields[index])
            {
                case 4:
                    if (!__instance.prefabDesc.isPowerGen) return;

                    switch (__instance.prefabDesc.fuelMask)
                    {
                        case 1:
                            __result = "化学能".TranslateFromJson();
                            return;

                        case 2:
                            __result = "裂变能".TranslateFromJson();
                            return;

                        case 4:
                            __result = "质能转换".TranslateFromJson();
                            return;

                        case 16:
                            __result = "聚变能".TranslateFromJson();
                            return;
                    }

                    if (__instance.ModelIndex == ProtoID.M同位素温差发电机) __result = "裂变能".TranslateFromJson();

                    return;

                //case 18:
                //    if (__instance.prefabDesc.isCollectStation && __instance.ID == ProtoID.I大气采集器) __result = "行星大气".TranslateFromJson();

                //    return;

                case 19:
                    if (__instance.prefabDesc.minerType == EMinerType.Oil)
                        __result = (600000.0 / __instance.prefabDesc.minerPeriod * GameMain.history.miningSpeedScale).ToString("0.##")
                                 + "x";

                    return;
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ItemProto), nameof(ItemProto.FindRecipes))]
        //public static void ItemProto_FindRecipes(ItemProto __instance) => __instance.isRaw = __instance.recipes.Count == 0;
    }
}
