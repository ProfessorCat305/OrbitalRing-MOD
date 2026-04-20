using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOrbitalRing.Patches.Logic.Farm
{
    internal class SyncFarmAssemblerRecipes
    {
        public static void SyncRecipes(PlanetFactory factory, int assemblerId, bool syncToThisId)
        {
            int entityId = factory.factorySystem.assemblerPool[assemblerId].entityId;
            if (entityId == 0) {
                return;
            }
            int num = entityId;
            int num2 = 0;
            int assemblerId2 = 0;
            bool is15 = false;
            for (; ; )
            {
                bool flag;
                int num3;
                int num4;
                factory.ReadObjectConn(num, 14, out flag, out num3, out num4);
                num = num3;
                if (num > 0) {
                    assemblerId2 = factory.entityPool[num].assemblerId;
                    if (assemblerId2 > 0 && factory.factorySystem.assemblerPool[assemblerId2].id == assemblerId2) {
                        if (syncToThisId) {
                            if (factory.factorySystem.assemblerPool[assemblerId2].recipeId > 0) {
                                is15 = false;
                                break;
                            }
                        } else {
                            factory.factorySystem.TakeBackItems_Assembler(GameMain.mainPlayer, assemblerId2);
                            factory.factorySystem.assemblerPool[assemblerId2].SetRecipe(factory.factorySystem.assemblerPool[assemblerId].recipeId, factory.entitySignPool);
                            is15 = true;
                        }
                        
                    }
                }
                if (num2 > 256 || num == 0) {
                    is15 = true;
                    break;
                }
            }
            if (!is15 && assemblerId2 != 0) {
                factory.factorySystem.TakeBackItems_Assembler(GameMain.mainPlayer, assemblerId);
                factory.factorySystem.assemblerPool[assemblerId].SetRecipe(factory.factorySystem.assemblerPool[assemblerId2].recipeId, factory.entitySignPool);
            }

            num = entityId;
            num2 = 0;
            int assemblerId3;
            for (; ; )
            {
                bool flag;
                int num3;
                int num4;
                factory.ReadObjectConn(num, 15, out flag, out num3, out num4);
                num = num3;
                if (num > 0) {
                    assemblerId3 = factory.entityPool[num].assemblerId;
                    if (assemblerId3 > 0 && factory.factorySystem.assemblerPool[assemblerId3].id == assemblerId3) {
                        if (syncToThisId) {
                            if (factory.factorySystem.assemblerPool[assemblerId3].recipeId > 0) {
                                break;
                            }
                        } else {
                            factory.factorySystem.TakeBackItems_Assembler(GameMain.mainPlayer, assemblerId3);
                            factory.factorySystem.assemblerPool[assemblerId3].SetRecipe(factory.factorySystem.assemblerPool[assemblerId].recipeId, factory.entitySignPool);
                        }
                    }
                }
                if (num2 > 256 || num == 0) {
                    return;
                }
            }
            factory.factorySystem.TakeBackItems_Assembler(GameMain.mainPlayer, assemblerId);
            factory.factorySystem.assemblerPool[assemblerId].SetRecipe(factory.factorySystem.assemblerPool[assemblerId3].recipeId, factory.entitySignPool);

            return;
        }

        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        [HarmonyPostfix]
        public static void CreateEntityLogicComponentsPatch(ref PlanetFactory __instance, int entityId, PrefabDesc desc, int prebuildId)
        {
            if (desc.isAssembler) {
                if (__instance.entityPool[entityId].protoId == ProtoID.I生态温室) {
                    SyncRecipes(__instance, __instance.entityPool[entityId].assemblerId, true);
                }
            }
        }

        [HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow.OnRecipeResetClick))]
        [HarmonyPostfix]
        public static void OnRecipeResetClickPatch(UIAssemblerWindow __instance)
        {
            if (__instance.assemblerId == 0 || __instance.factory == null) {
                return;
            }
            if (__instance.factorySystem.assemblerPool[__instance.assemblerId].id != __instance.assemblerId) {
                return;
            }
            PlanetFactory factory = __instance.factory;
            if (factory.entityPool[factory.factorySystem.assemblerPool[__instance.assemblerId].entityId].protoId == ProtoID.I生态温室) {
                SyncRecipes(factory, __instance.assemblerId, false);
            }
        }

        [HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow.OnRecipePickerReturn))]
        [HarmonyPostfix]
        public static void OnRecipePickerReturnPatch(UIAssemblerWindow __instance, RecipeProto recipe)
        {
            if (__instance.assemblerId == 0 || __instance.factory == null) {
                return;
            }
            if (__instance.factorySystem.assemblerPool[__instance.assemblerId].id != __instance.assemblerId) {
                return;
            }
            PlanetFactory factory = __instance.factory;
            if (factory.entityPool[factory.factorySystem.assemblerPool[__instance.assemblerId].entityId].protoId == ProtoID.I生态温室) {
                SyncRecipes(factory, __instance.assemblerId, false);
            }
        }


    }
}
