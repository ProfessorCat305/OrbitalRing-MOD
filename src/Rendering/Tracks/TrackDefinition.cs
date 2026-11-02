using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal sealed class TrackDefinition
    {
        private static readonly TrackDefinition structure = new TrackDefinition(
            ProtoID.I轨道连接组件, new Color(1f, 0.60f, 0.08f), 2f, 1.08f);
        private static readonly TrackDefinition power = new TrackDefinition(
            ProtoID.I星环电网组件, new Color(0.10f, 1f, 0.28f), 1.35f, 0.98f);
        private static readonly TrackDefinition accelerator = new TrackDefinition(
            ProtoID.I粒子加速轨道, new Color(0.58f, 0.24f, 0.88f), 1.65f, 1.28f);

        internal const float IsolatedHalfLength = 0.7f;
        internal int ItemId { get; }
        internal Color LightColor { get; }
        internal float Radius { get; }
        internal float CenterHeight { get; }
        internal bool HasGlassWall => ItemId == ProtoID.I粒子加速轨道;
        internal float WallRadius => Radius * 0.66f;

        private TrackDefinition(int itemId, Color lightColor, float radius, float centerHeight)
        {
            ItemId = itemId;
            LightColor = lightColor;
            Radius = radius;
            CenterHeight = centerHeight;
        }

        internal static TrackDefinition Get(int itemId)
        {
            switch (itemId)
            {
                case ProtoID.I轨道连接组件:
                    return RemodelSettings.Structure ? structure : null;
                case ProtoID.I星环电网组件:
                    return RemodelSettings.Power ? power : null;
                case ProtoID.I粒子加速轨道:
                    return RemodelSettings.Accelerator ? accelerator : null;
                default:
                    return null;
            }
        }
    }

    internal enum TrackSurfaceRole
    {
        Wall,
        InnerWall,
        Fittings,
        Core,
        Detail,
    }
}
