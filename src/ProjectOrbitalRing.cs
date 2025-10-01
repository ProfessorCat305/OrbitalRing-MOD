using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using xiaoye97;
using crecheng.DSPModSave;
using NebulaAPI;
using NebulaAPI.Interfaces;
using ProjectOrbitalRing.Compatibility;
using ProjectOrbitalRing.Patches.Logic;
using ProjectOrbitalRing.Patches.Logic.AddVein;
using ProjectOrbitalRing.Patches.Logic.MegaAssembler;
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;
using ProjectOrbitalRing.Patches.Logic.QuantumStorage;
using ProjectOrbitalRing.Patches.Logic.BattleRelated;
using ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech;
using ProjectOrbitalRing.Patches.UI;
using ProjectOrbitalRing.Patches.UI.PlanetFocus;
using ProjectOrbitalRing.Utils;
using static ProjectOrbitalRing.Utils.JsonDataUtils;
using static ProjectOrbitalRing.Utils.CopyModelUtils;
using static ProjectOrbitalRing.Utils.TranslateUtils;
using static ProjectOrbitalRing.Patches.Logic.AddVein.AddVeinPatches;
using static ProjectOrbitalRing.Patches.Logic.AddVein.ModifyPlanetTheme;
using static ProjectOrbitalRing.Patches.Logic.LogisticsInterchangePatches;
using static ProjectOrbitalRing.Patches.UI.ChemicalRecipeFcolPatches;
using static ProjectOrbitalRing.Patches.Logic.BattleRelated.HPAdjust;
using static ProjectOrbitalRing.Patches.Logic.ModifyUpgradeTech.ModifyUpgradeTech;
using static ProjectOrbitalRing.Patches.Logic.MathematicalRateEngine.UI;
using static ProjectOrbitalRing.Patches.Logic.SatellitePowerDistributionPatch;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
//ProjectGenesis

// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBePartlyConvertedToQuery

namespace ProjectOrbitalRing
{
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency(LDBToolPlugin.MODGUID)]
    [BepInDependency(NebulaModAPI.API_GUID)]
    [BepInDependency(InstallationCheckPlugin.MODGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(TabSystem), nameof(LocalizationModule))]
    [ModSaveSettings(LoadOrder = LoadOrder.Preload)]
    public class ProjectOrbitalRing : BaseUnityPlugin, IModCanSave, IMultiplayerMod
    {
        public const string MODGUID = "org.ProfessorCat305.OrbitalRing";
        public const string MODNAME = "OrbitalRing";
        public const string VERSION = "0.8.15";
        public const string DEBUGVERSION = "";

        public static bool LoadCompleted;

        public static bool MoreMegaStructureCompatibility = false;

        internal static ManualLogSource logger;
        internal static ConfigFile configFile;
        internal static UIPlanetFocusWindow PlanetFocusWindow;

        internal static int[] TableID;

        internal static string ModPath;

        internal static ConfigEntry<bool> LDBToolCacheEntry, HideTechModeEntry, ShowMessageBoxEntry;

        internal static ConfigEntry<int> ProductOverflowEntry;

        internal static ConfigEntry<KeyboardShortcut> QToolsHotkey;

        private Harmony Harmony;

        public void Awake()
        {
        #region Logger

            logger = Logger;
            logger.Log(LogLevel.Info, "OrbitalRing Awake");

        #endregion Logger

        #region Configs

            configFile = Config;

            LDBToolCacheEntry = Config.Bind("config", "UseLDBToolCache", false,
                "Enable LDBTool Cache, which allows you use config to fix some compatibility issues.\n启用LDBTool缓存，允许使用配置文件修复部分兼容性问题");

            //HideTechModeEntry = Config.Bind("config", "HideTechMode", true,
            //    "Enable Tech Exploration Mode, which will hide locked techs in tech tree.\n启用科技探索模式，启用后将隐藏未解锁的科技");

            ShowMessageBoxEntry = Config.Bind("config", "ShowMessageBox", true,
                "Show message when OrbitalRing is loaded.\n首次加载时的提示信息");

            //ProductOverflowEntry = Config.Bind("config", "ProductOverflow", 0,
            //    "Changing the condition for stopping production of some recipes from single product pile up to all product pile up.\n将部分配方停止生产的条件由单产物堆积改为所有产物均堆积");

            QToolsHotkey = Config.Bind("config", "QToolsHotkey", KeyboardShortcut.Deserialize("BackQuote"),
                "Shortcut to open QTools window");

            Config.Save();

        #endregion Configs

        #region ResourceData

            var executingAssembly = Assembly.GetExecutingAssembly();

            ModPath = Path.GetDirectoryName(executingAssembly.Location);

            var resources = new ResourceData("org.ProfessorCat305.OrbitalRing", "texpack", ModPath);
            resources.LoadAssetBundle("texpack");
            ProtoRegistry.AddResource(resources);

            var resources_models = new ResourceData("org.ProfessorCat305.OrbitalRing", "genesis-models", ModPath);
            resources_models.LoadAssetBundle("genesis-models");
            ProtoRegistry.AddResource(resources_models);

            var resources_lab = new ResourceData("org.ProfessorCat305.OrbitalRing", "genesis-models-lab", ModPath);
            resources_lab.LoadAssetBundle("genesis-models-lab");
            ProtoRegistry.AddResource(resources_lab);

            Shader stoneVeinShader =
                resources_models.bundle.LoadAsset<Shader>("Assets/genesis-models/shaders/PBR Standard Vein Stone COLOR.shader");
            SwapShaderPatches.AddSwapShaderMapping("VF Shaders/Forward/PBR Standard Vein Stone", stoneVeinShader);

            Shader metalVeinShader =
                resources_models.bundle.LoadAsset<Shader>("Assets/genesis-models/shaders/PBR Standard Vein Metal COLOR.shader");
            SwapShaderPatches.AddSwapShaderMapping("VF Shaders/Forward/PBR Standard Vein Metal", metalVeinShader);

            Shader labToggleShader =
                resources_lab.bundle.LoadAsset<Shader>("Assets/genesis-models/shaders/PBR Standard Vertex Toggle Lab REPLACE.shader");
            SwapShaderPatches.AddSwapShaderMapping("VF Shaders/Forward/PBR Standard Vertex Toggle Lab", labToggleShader);

        #endregion ResourceData

        #region NebulaModAPI

            NebulaModAPI.RegisterPackets(executingAssembly);

            NebulaModAPI.OnPlanetLoadRequest += planetId =>
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new OrbitalRingPlanetLoadRequest(planetId));
            };

            NebulaModAPI.OnPlanetLoadFinished += OrbitalRingPlanetDataProcessor.ProcessBytesLater;

        #endregion NebulaModAPI

            Harmony = new Harmony(MODGUID);

            foreach (Type type in executingAssembly.GetTypes())
            {
                if (type.Namespace?.StartsWith("ProjectOrbitalRing.Patches", StringComparison.Ordinal) == true) { Harmony.PatchAll(type); }
            }

            TableID = new int[]
            {
                TabSystem.RegisterTab($"{MODGUID}:{MODGUID}Tab1",
                    new TabData("精炼页面".TranslateFromJsonSpecial(), "Assets/texpack/矿物处理")),
                TabSystem.RegisterTab($"{MODGUID}:{MODGUID}Tab2",
                    new TabData("化工页面".TranslateFromJsonSpecial(), "Assets/texpack/化工科技")),
                TabSystem.RegisterTab($"{MODGUID}:{MODGUID}Tab3", new TabData("防御页面".TranslateFromJsonSpecial(), "Assets/texpack/防御")),
            };

            RegisterStrings();
            ModifyVeinData();

            LDBTool.PreAddDataAction += PreAddDataAction;
            LDBTool.PostAddDataAction += PostAddDataAction;

            LoadCompleted = true;
        }

        private void Update()
        {
            if (VFInput.inputing) return;
        }

        public void Export(BinaryWriter w)
        {
            w.Write(VersionNumber());
            MegaAssemblerPatches.Export(w);
            PlanetFocusPatches.Export(w);
            QuantumStoragePatches.Export(w);
            AdvancedLaserPatches.Export(w);
            GlobalPowerSupplyPatches.Export(w);
            Unlock_Save_Load.Export(w);
            StarGate.Export(w);
            OrbitalStationManager.Export(w);
        }

        public void Import(BinaryReader r)
        {
            int version = r.ReadInt32();
            MegaAssemblerPatches.Import(r);
            PlanetFocusPatches.Import(r);
            QuantumStoragePatches.Import(r);
            AdvancedLaserPatches.Import(r);
            GlobalPowerSupplyPatches.Import(r);
            Unlock_Save_Load.Import(r);
            StarGate.Import(r);
            OrbitalStationManager.Import(r);
        }

        public void IntoOtherSave()
        {
            MegaAssemblerPatches.IntoOtherSave();
            PlanetFocusPatches.IntoOtherSave();
            QuantumStoragePatches.IntoOtherSave();
            AdvancedLaserPatches.IntoOtherSave();
            GlobalPowerSupplyPatches.IntoOtherSave();
            Unlock_Save_Load.IntoOtherSave();
        }

        public string Version => VERSION;

        public bool CheckVersion(string hostVersion, string clientVersion) => hostVersion.Equals(clientVersion);

        private void PreAddDataAction()
        {
            InitializeMarkerAngles();
            GetDysonVanillaUITexts();
            LDB.items.OnAfterDeserialize();
            ModifyPlanetThemeDataVanilla();
            StationPrefabDescPostAdd();
            //StationPrefabDescPostAdd810();
            AddCopiedModelProto();
            AddEffectEmitterProto();
            ImportJson(TableID);
            ModifyUpgradeTeches();
        }

        private void PostAddDataAction()
        {
            //飞行舱拆除
            VegeProto vegeProto = LDB.veges.Select(9999);
            vegeProto.MiningItem = new[] { 2303, 2001, 2011, 7609, 2204 }; // 4黄台，500黄带，300黄爪，1零素矢，1火电
            vegeProto.MiningCount = new[] { 4, 500, 300, 1, 1 };
            vegeProto.MiningChance = new float[] { 1, 1, 1, 1, 1 };
            vegeProto.Preload();

            LabComponent.matrixIds = new[]
            {
                ProtoID.I电磁矩阵, ProtoID.I能量矩阵, ProtoID.I结构矩阵, ProtoID.I信息矩阵,
                ProtoID.I引力矩阵, ProtoID.I宇宙矩阵, ProtoID.I通量矩阵, ProtoID.I张量矩阵,
                ProtoID.I奇点矩阵,
            };

            LabComponent.matrixShaderStates = new[]
            {
                0.0f, 11111.2f, 22222.2f, 33333.2f,
                44444.2f, 55555.2f, 66666.2f, 77777.2f,
                88888.2f, 99999.2f,
            };

            LDB.items.OnAfterDeserialize();
            LDB.recipes.OnAfterDeserialize();
            LDB.techs.OnAfterDeserialize();
            LDB.models.OnAfterDeserialize();
            LDB.milestones.OnAfterDeserialize();
            LDB.journalPatterns.OnAfterDeserialize();
            LDB.themes.OnAfterDeserialize();
            LDB.veins.OnAfterDeserialize();

            if (GameMain.instance != null)
            {
                GameMain.instance.CreateGPUInstancing();
                GameMain.instance.CreateBPGPUInstancing();
                // GameMain.instance.CreateMultithreadSystem();
            }

            PrefabDescPostFix();
            StationPrefabDescPostAdd810();
            OurSideHPAdjust();
            ModelPostFix();

            ProtoPreload();

            ModifyEnemyHpUpgrade();
            SetMinerMk2Color();
            SetChemicalRecipeFcol();
            SetEffectEmitterProto();

            ChangeWeiXinPowerPoint();

            VFEffectEmitter.Init();

            ItemProto.InitFuelNeeds();
            ItemProto.InitTurretNeeds();
            ItemProto.InitFluids();
            ItemProto.InitTurrets();
            ItemProto.InitEnemyDropTables();
            ItemProto.InitConstructableItems();
            ItemProto.InitItemIds();
            ItemProto.InitItemIndices();
            ItemProto.InitMechaMaterials();
            ItemProto.InitFighterIndices();
            ModelProto.InitMaxModelIndex();
            ModelProto.InitModelIndices();
            ModelProto.InitModelOrders();
            RecipeProto.InitFractionatorNeeds();
            RaycastLogic.LoadStatic();

            

            ItemProto.stationCollectorId = ProtoID.I轨道采集器;

            ItemPostFix();

            StorageComponent.staticLoaded = false;
            StorageComponent.LoadStatic();

            UIBuildMenu.staticLoaded = false;
            UIBuildMenu.StaticLoad();

            SpaceSector.PrefabDescByModelIndex = null;
            SpaceSector.InitPrefabDescArray();

            PlanetFactory.PrefabDescByModelIndex = null;
            PlanetFactory.InitPrefabDescArray();

            ref MechaMaterialSetting material = ref Configs.builtin.mechaArmorMaterials[21];
            material.itemId = ProtoID.I钨块;
            material.density = 19.35f;
            material.durability = 4.35f;

            // JsonHelper.ExportAsJson(@"D:\Git\ProjectOrbitalRing\data");
        }

        private static void ProtoPreload()
        {
            foreach (MilestoneProto milestone in LDB.milestones.dataArray) milestone.Preload();

            foreach (JournalPatternProto journalPattern in LDB.journalPatterns.dataArray) journalPattern.Preload();

            foreach (VeinProto proto in LDB.veins.dataArray) proto.Preload();

            foreach (TechProto proto in LDB.techs.dataArray) proto.Preload();

            for (var i = 0; i < LDB.items.dataArray.Length; ++i)
            {
                LDB.items.dataArray[i].recipes = null;
                LDB.items.dataArray[i].rawMats = null;
                LDB.items.dataArray[i].Preload(i);
            }

            for (var i = 0; i < LDB.recipes.dataArray.Length; ++i) LDB.recipes.dataArray[i].Preload(i);

            foreach (TechProto proto in LDB.techs.dataArray)
            {
                proto.PreTechsImplicit = proto.PreTechsImplicit.Except(proto.PreTechs).ToArray();
                proto.UnlockRecipes = proto.UnlockRecipes.Distinct().ToArray();
                proto.Preload2();
            }
        }

        internal static void SetConfig(bool currentLDBToolCache, bool currentShowMessageBox,
            int currentProductOverflow)
        {
            LDBToolCacheEntry.Value = currentLDBToolCache;
            //HideTechModeEntry.Value = currentHideTechMode;
            ShowMessageBoxEntry.Value = currentShowMessageBox;
            //ProductOverflowEntry.Value = currentProductOverflow;
            logger.LogInfo("SettingChanged");
            configFile.Save();
        }

        internal static void LogInfo(object data) => logger.LogInfo(data);
        internal static void LogWarning(object data) => logger.LogWarning(data);
        internal static void LogError(object data) => logger.LogError(data);

        internal static int VersionNumber()
        {
            var version = new Version();
            version.FromFullString(VERSION);
            return version.sig;
        }
    }
}
