using System.Collections.Generic;
using System.Reflection;
using CommonAPI.Systems;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;
using xiaoye97;
using static ProjectOrbitalRing.ProjectOrbitalRing;

// ReSharper disable CommentTypo
// ReSharper disable LoopCanBePartlyConvertedToQuery
// ReSharper disable Unity.UnknownResource
// ReSharper disable Unity.PreferAddressByIdToGraphicsParams

namespace ProjectOrbitalRing.Utils
{
    internal static class CopyModelUtils
    {
        internal static void AddCopiedModelProto()
        {
            //CopyModelProto(50, ProtoID.M轨道熔炼站, Color.HSVToRGB(0.0710f, 0.7412f, 0.8941f));
            CopyModelProto(117, ProtoID.M超空间中继器, null);
            //CopyModelProto(50, ProtoID.M太空船坞, Color.HSVToRGB(0.5571f, 0.3188f, 0.8980f));
            //CopyModelProto(50, ProtoID.M轨道观测站, Color.HSVToRGB(0.2275f, 0.3804f, 0.6431f));
            //CopyModelProto(49, ProtoID.M太空电梯, Color.HSVToRGB(0.1404f, 0.8294f, 0.9882f));
            //CopyModelProto(50, ProtoID.M深空物流港, new Color32(60, 179, 113, 255));
            CopyModelProto(56, ProtoID.M轨道反物质堆核心);
            CopyModelProto(56, ProtoID.M超空间中继器核心);
            CopyModelProto(68, ProtoID.M勘察卫星, Color.HSVToRGB(0.0833f, 0.8f, 1.0f));
            CopyModelProto(46, ProtoID.M同位素温差发电机, Color.HSVToRGB(0.4174f, 0.742f, 0.9686f));
            //CopyModelProto(50, ProtoID.M轨道水培舱, new Color(0.3216F, 0.8157F, 0.09020F));
            //CopyModelProto(50, ProtoID.M星环对撞机, new Color(0.3059F, 0.2196F, 0.4941F));
            //CopyModelProto(50, ProtoID.M轨道反物质堆基座);
            CopyModelProto(432, ProtoID.M启示录聚变弹, new Color(0.3059F, 0.2196F, 0.4941F));
            CopyModelProto(375, ProtoID.M聚爆加农炮MK2, new Color(0.2275f, 0.3804f, 0.6431f));
            CopyModelProto(373, ProtoID.M高频激光塔MK2, new Color(0.5765f, 0.4392f, 0.8588f));
            CopyModelProto(490, ProtoID.M战术核导弹);
            CopyModelProto(488, ProtoID.M反物质炮弹);
            CopyModelProto(46, ProtoID.M蓄电器, Color.HSVToRGB(0.0833f, 0.8f, 1.0f));
            CopyModelProto(48, ProtoID.M深空货舰);
            //CopyModelProto(50, ProtoID.M天枢座, new Color(0.7373f, 0.2118f, 0.8510f));
            CopyModelProto(36, ProtoID.M星环电网组件, new Color(0.7373f, 0.2118f, 0.8510f)); 
            CopyModelProto(72, ProtoID.M轨道弹射器, new Color(0.1404f, 0.8294f, 0.9882f)); 
            CopyModelProto(49, ProtoID.M轨道空投站, Color.HSVToRGB(0.0710f, 0.7412f, 0.8941f));
            CopyModelProto(35, ProtoID.M轨道连接组件, new Color(1f, 1f, 1f));
            CopyModelProto(37, ProtoID.M粒子加速轨道, new Color(1f, 1f, 1f));
            CopyModelProto(402, ProtoID.M星环护盾组件, new Color(1f, 1f, 1f));
            CopyModelProto(51, ProtoID.M黑盒, new Color(0f, 0f, 0f));
            CopyModelProto(37, ProtoID.M空轨, new Color(0f, 0f, 0f));
            CopyModelProto(403, ProtoID.M伺服天穹组件);
            CopyModelProto(403, ProtoID.M智能方尖碑, new Color(1f, 1f, 1f));
            CopyModelProto(403, ProtoID.M亿万械国, new Color(0f, 0f, 0f));
            CopyModelProto(56, ProtoID.MSCP310恒燃之火, new Color(0f, 0f, 0f));
            CopyModelProto(403, ProtoID.M突触凝练机, Color.red);
            CopyModelProto(455, ProtoID.M生态温室, Color.HSVToRGB(0.4174f, 0.742f, 0.9686f));
            CopyModelProto(64, ProtoID.M低温工厂, new Color(1f, 1f, 1f));
            CopyModelProto(432, ProtoID.M朗基努斯之枪, Color.red);
            CopyModelProto(403, ProtoID.M欺骗型广播塔, new Color(0f, 0f, 0f));
            CopyModelProto(121, ProtoID.M无尽水罐, new Color(0f, 0f, 0f));

            AddNewBuildingModel();
            ChangeAccumulatorColor();
        }


        private static void ChangeAccumulatorColor()
        {
            ModelProto oriModel = LDB.models.Select(46);
            PrefabDesc desc = oriModel.prefabDesc;
            ref PrefabDesc modelPrefabDesc = ref oriModel.prefabDesc;
            foreach (Material[] lodMaterial in modelPrefabDesc.lodMaterials)
            {
                if (lodMaterial == null) continue;

                for (var j = 0; j < lodMaterial.Length; j++)
                {
                    ref Material material = ref lodMaterial[j];

                    if (material == null) continue;

                    material = new Material(material);

                    material.SetColor("_Color", new Color(0.3529f, 0.8235f, 1.0f));
                }
            }
        }

        private static void AddNewBuildingModel()
        {
            string prefabPath;
            prefabPath = "Assets/orbitalrings-models/entities/prefabs/space-elevator";
            RegisterNewBuildingModel(ProtoID.M物流立交, ProtoID.M太空电梯, prefabPath, Color.HSVToRGB(0.1404f, 0.8294f, 0.9882f));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/space-dock";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M太空船坞, prefabPath, Color.HSVToRGB(0.5571f, 0.3188f, 0.8980f));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/orbital-antimatter-reactor";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M轨道反物质堆基座, prefabPath);

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/orbital-observation-station";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M轨道观测站, prefabPath, Color.HSVToRGB(0.2275f, 0.3804f, 0.6431f));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/orbital-smelting-station1-J";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M轨道熔炼站, prefabPath, Color.HSVToRGB(0.0710f, 0.7412f, 0.8941f));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/orbital-hydroponic-cabin";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M轨道水培舱, prefabPath, new Color(0.3216F, 0.8157F, 0.09020F));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/deep-space-station";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M深空物流港, prefabPath, new Color32(60, 179, 113, 255));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/tianzhu-seat";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M天枢座, prefabPath, new Color(0.7373f, 0.2118f, 0.8510f));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/orbitalring-particle-accelerator-collider1-JJ";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M星环对撞机, prefabPath, new Color(0.3059F, 0.2196F, 0.4941F));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/space-logistics-port";
            RegisterNewBuildingModel(ProtoID.M太空物流港, ProtoID.M太空物流港, prefabPath, new Color(0.8275F, 0.8275F, 0.8275F));

            prefabPath = "Assets/orbitalrings-models/entities/prefabs/deep-space-Cargo-Ship1-toab";
            RegisterNewBuildingModel(ProtoID.M太空运输船, ProtoID.M深空货舰, prefabPath);
        }

        private static void RegisterNewBuildingModel(int oriModelId, int registerModelId, string prefabPath, Color? color = null)
        {
            ModelProto oriModel = LDB.models.Select(oriModelId);
            PrefabDesc desc = oriModel.prefabDesc;

            var newMats = new List<Material>();

            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;

                foreach (Material mat in lodMats) {
                    if (mat == null) continue;

                    var newMaterial = new Material(mat);
                    if (color.HasValue) {
                        newMaterial.SetColor("_Color", color.Value);
                    }
                    newMats.Add(newMaterial);
                }
            }

            switch (registerModelId) {
                case ProtoID.M深空物流港:
                    AddMaterial806(ref newMats);
                    break;

                case ProtoID.M轨道熔炼站:
                    AddMaterial801(ref newMats);
                    break;

                case ProtoID.M轨道水培舱:
                    AddMaterial810(ref newMats);
                    break;

                case ProtoID.M轨道观测站:
                    AddMaterial804(ref newMats);
                    break;

                case ProtoID.M星环对撞机:
                    AddMaterial811(ref newMats);
                    break;

                case ProtoID.M天枢座:
                    AddMaterial820(ref newMats);
                    break;

                case ProtoID.M太空船坞:
                    AddMaterial803(ref newMats);
                    break;

                // 明明模型加了环的不知道为什么环就是不出来，力竭了
                //case ProtoID.M深空货舰:
                //    AddMaterial819(ref newMats);
                //    break;
            }

            ModelProto registerModel = ProtoRegistry.RegisterModel(registerModelId, prefabPath, newMats.ToArray());

            registerModel.HpMax = oriModel.HpMax;
            registerModel.RuinId = oriModel.RuinId;
            registerModel.RuinType = ERuinType.Normal;
            registerModel.RuinCount = 1;
        }

        private static void AddMaterial806(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(63); // 原油精炼厂
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;

                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        private static void AddMaterial801(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(62); // 电弧熔炉
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;

                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }


        private static void AddMaterial810(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(64); // 化工厂
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    // 导入ab包的化工球模型的材质没有使用 chemical-plant-black，所以这里踢掉
                    if (i == 2) {
                        i++;
                        continue;
                    }

                    var newMaterial = new Material(mat);
                    if (i == 1) {
                        newMaterial.SetVector("_SpherePos", new Vector4(0f, 32.22f, 0f, 7.2f));
                    }
                    if (i == 3) {
                        newMaterial.SetVector("_SpherePos", new Vector4(0f, 32.22f, 0f, 7.2f));
                    }
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }


        private static void AddMaterial804(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(453); // 战场分析基站
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    if (i == 1) {
                        i++;
                        continue;
                    }
                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        private static void AddMaterial811(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(69); // 微型粒子对撞机
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        private static void AddMaterial820(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(403); // 信号塔
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        private static void AddMaterial803(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(61); // 激光钻井平台
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            // 导入ab包的大塔模型的材质没有station back，所以这里踢掉
            newMats.RemoveAt(1);
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        private static void AddMaterial819(ref List<Material> newMats)
        {
            ModelProto oriModel = LDB.models.Select(117); // 轨道采集器
            PrefabDesc desc = oriModel.prefabDesc;
            int i = 0;
            foreach (Material[] lodMats in desc.lodMaterials) {
                if (lodMats == null) continue;
                foreach (Material mat in lodMats) {
                    if (mat == null) continue;
                    var newMaterial = new Material(mat);
                    newMats.Add(newMaterial);
                    i++;
                }
            }
        }

        //private static void AddAtmosphericCollectStation()
        //{
        //    ModelProto oriModel = LDB.models.Select(ProtoID.M星际物流运输站);
        //    PrefabDesc desc = oriModel.prefabDesc;

        //    var newMats = new List<Material>();

        //    foreach (Material[] lodMats in desc.lodMaterials)
        //    {
        //        if (lodMats == null) continue;

        //        foreach (Material mat in lodMats)
        //        {
        //            if (mat == null) continue;

        //            var newMaterial = new Material(mat);
        //            newMaterial.SetColor("_Color", new Color32(60, 179, 113, 255));
        //            newMats.Add(newMaterial);
        //        }
        //    }

        //    //oriModel = LDB.models.Select(ProtoID.M射线接收站); // ray receiver
        //    //var collectEffectMat = new Material(oriModel.prefabDesc.lodMaterials[0][3]);

        //    //collectEffectMat.SetColor("_TintColor", new Color32(131, 127, 197, 255));
        //    //collectEffectMat.SetColor("_PolarColor", new Color32(234, 255, 253, 170));
        //    //collectEffectMat.SetVector("_Aurora", new Vector4(75f, 1f, 20f, 0.1f));
        //    //collectEffectMat.SetVector("_Beam", new Vector4(12f, 78f, 24f, 1f));
        //    //collectEffectMat.SetVector("_Particle", new Vector4(2f, 30f, 5f, 0.8f));
        //    //collectEffectMat.SetVector("_Circle", new Vector4(2.5f, 34f, 1f, 0.04f));

        //    //newMats.Add(collectEffectMat);

        //    ModelProto registerModel = ProtoRegistry.RegisterModel(ProtoID.M深空物流港,
        //        "Assets/genesis-models/entities/prefabs/atmospheric-collect-station", newMats.ToArray());

        //    registerModel.HpMax = 300000;
        //    registerModel.RuinId = 384;
        //    registerModel.RuinType = ERuinType.Normal;
        //    registerModel.RuinCount = 1;
        //}

        private static void CopyModelProto(int oriId, int id, Color? color = null)
        {
            ModelProto oriModel = LDB.models.Select(oriId);
            ModelProto model = oriModel.Copy();
            model.Name = id.ToString();
            model.ID = id;

            PrefabDesc desc = oriModel.prefabDesc;
            GameObject prefab = desc.prefab ? desc.prefab : Resources.Load<GameObject>(oriModel.PrefabPath);
            GameObject colliderPrefab = desc.colliderPrefab ? desc.colliderPrefab : Resources.Load<GameObject>(oriModel._colliderPath);

            ref PrefabDesc modelPrefabDesc = ref model.prefabDesc;
            modelPrefabDesc = prefab == null ? PrefabDesc.none :
                colliderPrefab == null ? new PrefabDesc(id, prefab) : new PrefabDesc(id, prefab, colliderPrefab);

            foreach (Material[] lodMaterial in modelPrefabDesc.lodMaterials)
            {
                if (lodMaterial == null) continue;

                for (var j = 0; j < lodMaterial.Length; j++)
                {
                    ref Material material = ref lodMaterial[j];

                    if (material == null) continue;

                    material = new Material(material);

                    if (!color.HasValue) continue;

                    material.SetColor("_Color", color.Value);
                }
            }

            modelPrefabDesc.modelIndex = id;
            modelPrefabDesc.hasBuildCollider = desc.hasBuildCollider;
            modelPrefabDesc.colliders = desc.colliders;
            modelPrefabDesc.buildCollider = desc.buildCollider;
            modelPrefabDesc.buildColliders = desc.buildColliders;
            modelPrefabDesc.colliderPrefab = desc.colliderPrefab;
            modelPrefabDesc.dragBuild = desc.dragBuild;
            modelPrefabDesc.dragBuildDist = desc.dragBuildDist;
            modelPrefabDesc.blueprintBoxSize = desc.blueprintBoxSize;
            modelPrefabDesc.roughHeight = desc.roughHeight;
            modelPrefabDesc.roughWidth = desc.roughWidth;
            modelPrefabDesc.roughRadius = desc.roughRadius;
            modelPrefabDesc.barHeight = desc.barHeight;
            modelPrefabDesc.barWidth = desc.barWidth;

            model.sid = "";
            model.SID = "";

            LDBTool.PreAddProto(model);
        }

        private static ModelProto Copy(this ModelProto proto) =>
            new ModelProto
            {
                ObjectType = proto.ObjectType,
                RuinType = proto.RuinType,
                RendererType = proto.RendererType,
                HpMax = proto.HpMax,
                HpUpgrade = proto.HpUpgrade,
                HpRecover = proto.HpRecover,
                RuinId = proto.RuinId,
                RuinCount = proto.RuinCount,
                RuinLifeTime = proto.RuinLifeTime,
                PrefabPath = proto.PrefabPath,
                _colliderPath = proto._colliderPath,
                _ruinPath = proto._ruinPath,
                _wreckagePath = proto._wreckagePath,
                _ruinOriginModelIndex = proto._ruinOriginModelIndex,
            };

        internal static void ModelPostFix()
        {
            //ModelProto modelProto = LDB.models.Select(ProtoID.M深空物流港);
            //modelProto._ruinPath = "Entities/Prefabs/Ruins/interstellar-logistic-station-ruins";
            //modelProto._wreckagePath = "Entities/Prefabs/Wreckages/interstellar-logistic-station-wreckages";

            PrefabDesc prefabDesc = LDB.models.Select(ProtoID.M同位素温差发电机).prefabDesc;
            ref Material[] prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            prefabDescLODMaterial[2].SetColor("_TintColor", new Color(0.2715f, 1.7394f, 0.1930f));

            // 这是电流颜色
            //prefabDesc = LDB.models.Select(ProtoID.M蓄电器).prefabDesc;
            //prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            //prefabDescLODMaterial[2].SetColor("_TintColor", new Color(1.0000f, 0.6800f, 0.2267f));

            prefabDesc = LDB.models.Select(ProtoID.M轨道反物质堆核心).prefabDesc;
            Texture texture = TextureHelper.GetTexture("人造恒星MK2材质");
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            prefabDescLODMaterial[0].SetTexture("_EmissionTex", texture);
            prefabDescLODMaterial[1].SetColor("_TintColor", new Color(0.1804f, 0.4953f, 1.3584f));  // 亮部
            prefabDescLODMaterial[1].SetColor("_TintColor1", new Color(0.1294f, 0.3130f, 1.1508f)); // 暗部
            prefabDescLODMaterial[1].SetColor("_RimColor", new Color(0.4157f, 0.6784f, 1.0000f));   // 边缘特效

            prefabDesc = LDB.models.Select(ProtoID.M超空间中继器核心).prefabDesc;
            //Texture texture = Resources.Load<Texture>("Assets/texpack/人造恒星MK2材质");
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            prefabDescLODMaterial[0].SetTexture("_EmissionTex", texture);
            prefabDescLODMaterial[1].SetColor("_TintColor", new Color(0.0f, 0.1f, 0f));  // 亮部
            prefabDescLODMaterial[1].SetColor("_TintColor1", new Color(0.0f, 0.0f, 0.0f)); // 暗部
            prefabDescLODMaterial[1].SetColor("_RimColor", new Color(0.1f, 1.084f, 0.1000f));   // 边缘特效

            prefabDesc = LDB.models.Select(45).prefabDesc;
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            texture = TextureHelper.GetTexture("能量枢纽贴图");
            prefabDescLODMaterial[0].SetTexture("_MainTex", texture);

            prefabDesc = LDB.models.Select(828).prefabDesc;
            // 原lodDistances数据 85 5000 0 0，镜头在拉到85之外会导致黑色贴图修改失效，重新变成白色，原因未知，只能把lodDistances改大
            // lodDistances应该是用于优化性能，镜头拉远就切换低精度贴图，这么改可能会损失性能，不过黑盒不会大批量使用，应该还好
            prefabDesc.lodDistances = new float[] { 20000f, 20000f, 0f, 0f };
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            texture = TextureHelper.GetTexture("黑盒-a");
            prefabDescLODMaterial[0].SetTexture("_MainTex", texture);

            prefabDesc = LDB.models.Select(ProtoID.M矩阵研究站).prefabDesc;
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            ModifyLabColor(prefabDescLODMaterial[0]);
            ModifyLabColor(prefabDescLODMaterial[2]);
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[1];
            ModifyLabColor(prefabDescLODMaterial[0]);
            ModifyLabColor(prefabDescLODMaterial[2]);
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[2];
            ModifyLabColor(prefabDescLODMaterial[0]);

            prefabDesc = LDB.models.Select(ProtoID.M自演化研究站).prefabDesc;
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[0];
            ModifyLabColor(prefabDescLODMaterial[0]);
            ModifyLabColor(prefabDescLODMaterial[2]);
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[1];
            ModifyLabColor(prefabDescLODMaterial[0]);
            ModifyLabColor(prefabDescLODMaterial[2]);
            prefabDescLODMaterial = ref prefabDesc.lodMaterials[2];
            ModifyLabColor(prefabDescLODMaterial[0]);
        }

        private static void ModifyLabColor(Material material)
        {
            material.SetColor("_LabColor7", new Color(1f, 0.4510f, 0.0039f));
            material.SetColor("_LabColor8", new Color(1f, 0.0431f, 0.5843f));
            material.SetColor("_LabColor9", new Color(0.4020f, 0.4020f, 0.4020f));
        }

        internal static void ItemPostFix()
        {
            ref int[] turretNeed = ref ItemProto.turretNeeds[(int)EAmmoType.Bullet];
            turretNeed[1] = ProtoID.I钢芯弹箱;
            turretNeed[2] = ProtoID.I超合金弹箱;

            turretNeed = ref ItemProto.turretNeeds[(int)EAmmoType.LocalPlasma];
            turretNeed = new int[] { ProtoID.I氘核轨道弹, 0, 0 };

            // 添加轨道空投引导站的接收运输船位置，并置于轨道高空
            ItemProto item = LDB.items.Select(ProtoID.I轨道空投引导站);
            item.prefabDesc.stationShipPos = new Vector3(0f, 100f, 0f);

            item = LDB.items.Select(3010);
            item.prefabDesc.turretAmmoType = EAmmoType.LocalPlasma;
            //LDB.items.Select(ProtoID.I水).recipes = new List<RecipeProto> { LDB.recipes.Select(ProtoID.R海水淡化), };
            //LDB.items.Select(ProtoID.I氢).isRaw = true;

            // 战场分析基站建造范围从60改为200
            item = LDB.items.Select(3009);
            item.prefabDesc.constructionRange = 200f;
        }


    }
}
