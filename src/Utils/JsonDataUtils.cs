using System;
using System.Collections.Generic;
using System.Linq;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using xiaoye97;
using static ProjectOrbitalRing.Utils.JsonHelper;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using GalacticScale;
using Newtonsoft.Json.Linq;

// ReSharper disable RemoveRedundantBraces

namespace ProjectOrbitalRing.Utils
{
    internal static class JsonDataUtils
    {
        static bool IsMoreMegaStructureItem(int itemID)
        {
            if (itemID > 9000)
            {
                LDB.items.Exist(itemID);
                return true;
            }
            return false;
        }
        internal static void ImportJson(int[] tableID)
        {
            ref Dictionary<int, IconToolNew.IconDesc> itemIconDescs =
                ref AccessTools.StaticFieldRefAccess<Dictionary<int, IconToolNew.IconDesc>>(typeof(ProtoRegistry), "itemIconDescs");

            #region TechProto
            foreach (TechProtoJson protoJson in GetJsonContent<TechProtoJson>("techs")) {
                if (LDB.techs.Exist(protoJson.ID)) {
                    protoJson.ToProto(LDB.techs.Select(protoJson.ID));
                } else {
                    LDBTool.PreAddProto(protoJson.ToProto());
                }
            }

            #endregion

            #region Mod ItemProto

            foreach (ItemProtoJson protoJson in GetJsonContent<ItemProtoJson>("items_mod")) {
                if (!ProjectOrbitalRing.MoreMegaStructureCompatibility || !IsMoreMegaStructureItem(protoJson.ID)) {
                    protoJson.GridIndex = GetTableID(protoJson.GridIndex);
                    itemIconDescs.Add(protoJson.ID, IconDescUtils.GetIconDesc(protoJson.ID));
                    LDBTool.PreAddProto(protoJson.ToProto());
                }
            }

            #endregion

            #region Vanilla ItemProto

            foreach (ItemProtoJson protoJson in GetJsonContent<ItemProtoJson>("items_vanilla")) {
                protoJson.GridIndex = GetTableID(protoJson.GridIndex);
                ItemProto proto = LDB.items.Select(protoJson.ID);

                if (proto.IconPath != protoJson.IconPath) {
                    itemIconDescs.Add(protoJson.ID, IconDescUtils.GetIconDesc(protoJson.ID));
                }

                protoJson.ToProto(proto);
            }

            #endregion

            #region RecipeProto

            RecipeProto.recipeExecuteData = new Dictionary<int, RecipeExecuteData>();
            foreach (RecipeProtoJson protoJson in GetJsonContent<RecipeProtoJson>("recipes")) {
                if (!ProjectOrbitalRing.MoreMegaStructureCompatibility || !(protoJson.ID >= 530 && protoJson.ID <= 550)) {
                    protoJson.GridIndex = GetTableID(protoJson.GridIndex);
                    if (LDB.recipes.Exist(protoJson.ID)) {
                        protoJson.ToProto(LDB.recipes.Select(protoJson.ID));
                    } else {
                        LDBTool.PreAddProto(protoJson.ToProto());
                    }
                    RecipeExecuteData recipeExecuteData = new RecipeExecuteData(protoJson.Input, protoJson.InCounts, protoJson.Output, protoJson.OutCounts, protoJson.Time * 10000, protoJson.Time * 100000, !protoJson.NonProductive);
                    RecipeProto.recipeExecuteData.Add(protoJson.ID, recipeExecuteData);
                }
            }


            #endregion

            #region TutorialProto

            foreach (TutorialProtoJson protoJson in GetJsonContent<TutorialProtoJson>("tutorials")) {
                LDBTool.PreAddProto(protoJson.ToProto());
            }

            #endregion

            int GetTableID(int gridIndex)
            {
                if (gridIndex >= 5000) { return (tableID[2] - 5) * 1000 + gridIndex; }

                if (gridIndex >= 4000) { return (tableID[1] - 4) * 1000 + gridIndex; }

                if (gridIndex >= 3000) { return (tableID[0] - 3) * 1000 + gridIndex; }

                return gridIndex;
            }
        }

        internal static void PrefabDescPostFix()
        {
            PrefabDescJson[] prefabDescs = GetJsonContent<PrefabDescJson>("prefabDescs");

            foreach (PrefabDescJson json in prefabDescs) {
                json.ToPrefabDesc(LDB.models.Select(json.ModelID).prefabDesc);
            }

            PrefabDesc megaPumper = LDB.models.Select(ProtoID.M伺服天穹组件).prefabDesc;
            megaPumper.beaconSignalRadius = 0f;
            megaPumper = LDB.models.Select(ProtoID.M智能方尖碑).prefabDesc;
            megaPumper.beaconSignalRadius = 0f;
            megaPumper = LDB.models.Select(ProtoID.M亿万械国).prefabDesc;
            megaPumper.beaconSignalRadius = 0f;
            megaPumper = LDB.models.Select(ProtoID.M突触凝练机).prefabDesc;
            megaPumper.beaconSignalRadius = 0f;
            megaPumper = LDB.models.Select(ProtoID.M欺骗型广播塔).prefabDesc;
            megaPumper.beaconSignalRadius = 0f;

            megaPumper = LDB.models.Select(ProtoID.M生态温室).prefabDesc;
            megaPumper.isLab = false;

            //PrefabDesc megaPumper = LDB.models.Select(ProtoID.M大抽水机).prefabDesc;
            //megaPumper.waterPoints = new[] { Vector3.zero, };
            //megaPumper.portPoses = new[] { megaPumper.portPoses[0], };
        }
    }
}
