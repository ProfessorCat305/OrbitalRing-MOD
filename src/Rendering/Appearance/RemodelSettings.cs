using BepInEx.Configuration;

namespace ProjectOrbitalRing.Rendering
{
    internal static class RemodelSettings
    {
        internal static bool Structure { get; private set; } = true;
        internal static bool Power { get; private set; } = true;
        internal static bool Accelerator { get; private set; } = true;
        internal static bool AirRail { get; private set; } = true;
        internal static bool RingBraces { get; private set; } = true;
        internal static bool FacilityAttachments { get; private set; } = true;
        internal static bool AnyTrack => Structure || Power || Accelerator;
        internal static int AttachmentTracks => (Structure ? 3 : 0) | (Accelerator ? 4 : 0) | (Power ? 8 : 0);

        internal static void Configure(ConfigFile config)
        {
            Structure = Bind(config, "Structure", "轨道连接组件改模");
            Power = Bind(config, "Power", "星环电网组件改模");
            Accelerator = Bind(config, "Accelerator", "粒子加速轨道改模");
            AirRail = Bind(config, "AirRail", "空轨改模");
            RingBraces = Bind(config, "RingBraces", "完整环之间的连接支架");
            FacilityAttachments = Bind(config, "FacilityAttachments", "轨道设施接口和环位配套模型");
        }

        private static bool Bind(ConfigFile config, string key, string description) =>
            config.Bind("Remodels", key, true, "启用" + description + "。重启游戏后生效。").Value;
    }
}
