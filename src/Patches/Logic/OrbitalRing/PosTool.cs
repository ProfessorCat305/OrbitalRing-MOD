using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    static class PosTool
    {
        // 预计算所有标记角度（单位：度），赤道上下两圈，一圈40个
        private static readonly double[] MarkerAngles = new double[40];
        private static readonly Dictionary<PlanetData, OrbitalRingLayout> LayoutCache = new Dictionary<PlanetData, OrbitalRingLayout>();
        private const int MinElevatorAnchorCount = 5;
        private const float DualStationRingRadius = 150f;
        private const int RadiusUnitsPerElevatorAnchor = 10;
        private const double StandardAngleUnit = 0.36d;
        private const double AngleTolerance = 0.25d; // 允许的角度误差（度）

        public static void InitializeMarkerAngles()
        {
            for (int i = 0; i < 40; i++) {
                MarkerAngles[i] = (i * 9.0) % 360.0; // 每9度一个标记
            }
        }

        // 星环布局把设施点、电梯锚点和轨道传送带边分开离散化。设施槽数量是电梯锚点的两倍，
        // 二者按实际经度吸附网格分布，电梯锚点复用偶数设施槽。
        internal struct OrbitalRingLayout
        {
            public readonly int StationCapacity;
            public readonly int StationRingCount;
            public readonly double AngleUnit;

            public int ElevatorAnchorCount
            {
                get
                {
                    return StationCapacity / 2;
                }
            }

            public bool HasLowerStationRing
            {
                get
                {
                    return StationRingCount > 1;
                }
            }

            public OrbitalRingLayout(float radius, int stationCapacity, int stationRings)
            {
                StationCapacity = stationCapacity;
                StationRingCount = stationRings;
                int gridSegments = Math.Max(4, Mathf.FloorToInt(radius / 4f + 0.1f) * 4);
                // 半径 100/200 的星球继续使用 0.36 度轨道网格，
                // 以保持吸附和轨道分段行为与已有建筑兼容。
                AngleUnit = 360d / (gridSegments * 5d) * (stationRings > 1 ? 1d : 0.5d);
            }

            public int MapElevatorAnchorToStationIndex(int anchorIndex)
            {
                if (anchorIndex < 0 || anchorIndex >= ElevatorAnchorCount)
                {
                    return -1;
                }

                return anchorIndex * 2;
            }

            public double GetStationAngle(int stationIndex)
            {
                if (stationIndex < 0 || stationIndex >= StationCapacity)
                {
                    return 0d;
                }

                double angle = stationIndex * (360d / StationCapacity);
                double longitudeStep = AngleUnit * (HasLowerStationRing ? 1d : 2d);
                return Math.Round(angle / longitudeStep) * longitudeStep;
            }

            public double GetElevatorAnchorAngle(int anchorIndex)
            {
                if (anchorIndex < 0 || anchorIndex >= ElevatorAnchorCount)
                {
                    return 0d;
                }

                return GetStationAngle(MapElevatorAnchorToStationIndex(anchorIndex));
            }
        }

        public static float GetSurfaceRadius(PlanetData planet)
        {
            if (planet == null)
            {
                return 200f;
            }
            if (!float.IsNaN(planet.realRadius) && !float.IsInfinity(planet.realRadius) && planet.realRadius > 0f)
            {
                return planet.realRadius;
            }

            return !float.IsNaN(planet.radius) && !float.IsInfinity(planet.radius) && planet.radius > 0f ? planet.radius : 200f;
        }

        public static OrbitalRingLayout GetLayout(PlanetData planet)
        {
            if (planet != null && LayoutCache.TryGetValue(planet, out var cachedLayout))
            {
                return cachedLayout;
            }

            float radius = GetSurfaceRadius(planet);
            var ringData = planet == null ? null : OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planet.id);
            int stationCapacity;
            int stationRingCount;
            if (ringData != null) {
                stationCapacity = ringData.Rings[0].Capacity;
                stationRingCount = ringData.Rings.Count;
            } else {
                stationCapacity = Math.Max(MinElevatorAnchorCount, Mathf.RoundToInt(radius / RadiusUnitsPerElevatorAnchor)) * 2;
                stationRingCount = radius >= DualStationRingRadius ? 2 : 1;
            }

            var layout = new OrbitalRingLayout(radius, stationCapacity, stationRingCount);
            if (planet != null)
            {
                LayoutCache.Add(planet, layout);
            }

            return layout;
        }

        internal static void ClearLayoutCache()
        {
            LayoutCache.Clear();
        }

        private static double GetLongitudeAngle(double z, double x)
        {
            return (Math.Atan2(z, x) * 180.0 / Math.PI + 360.0) % 360.0;
        }

        private static double GetLatitudeAngle(Vector3 vector)
        {
            Vector3 normalized = Vector3.Normalize(vector);
            return Math.Asin(normalized.y) * (180.0 / Math.PI);
        }

        private static double GetAngleDiff(double angle, double marker)
        {
            double diff = Math.Abs(angle - marker);
            return Math.Min(diff, 360.0 - diff);
        }

        public static int IsBuildingPosXZCorrect(double x, double y, bool isFullRing, OrbitalRingLayout layout)
        {
            // 计算向量角度（0°~360°）
            double angle = GetLongitudeAngle(y, x);

            // 检查是否接近任意标记
            if (!ProjectOrbitalRing.GalacticScale2Compatibility && !layout.HasLowerStationRing) {
                for (int i = 0; i < MarkerAngles.Length; i++) {
                    if (!isFullRing && (i % 2) != 0)
                    {
                        continue;
                    }
                    double diff = GetAngleDiff(angle, MarkerAngles[i]); // 处理环形差值
                    if (diff <= AngleTolerance)
                    {
                        return i / 2;
                    }
                }
                return -1;
            }

            int markerCount = isFullRing ? layout.StationCapacity : layout.ElevatorAnchorCount;
            for (int i = 0; i < markerCount; i++) {
                double marker = isFullRing ? layout.GetStationAngle(i) : layout.GetElevatorAnchorAngle(i);
                double diff = GetAngleDiff(angle, marker); // 处理环形差值
                if (diff <= AngleTolerance) {
                    return isFullRing ? i : layout.MapElevatorAnchorToStationIndex(i);
                }
            }
            return -1;
        }

        public static int isBuildingPosYCorrect(Vector3 vector, OrbitalRingLayout layout)
        {
            int ret = -1;
            // 计算纬度角度（使用asin直接获取极角）
            double latitudeDeg = GetLatitudeAngle(vector);
            if (latitudeDeg >= 0) {
                ret = 0;
            } else {
                ret = 1;
            }
            latitudeDeg = Math.Abs(latitudeDeg);

            // 计算目标格边界
            double lower = 9.5d * layout.AngleUnit;
            double upper = 10.5d * layout.AngleUnit;
            if (!layout.HasLowerStationRing) {
                lower = -0.5d * layout.AngleUnit;
                upper = 0.5d * layout.AngleUnit;
                ret = 0;
            }

            // 包含边界检查
            return latitudeDeg > lower && latitudeDeg < upper ? ret : -1;
        }

        public static Vector3 ShouldBeXZAdsorb(Vector3 vector, bool isFullRing, OrbitalRingLayout layout)
        {
            float correction = layout.HasLowerStationRing
                ? 0.4f * (float)(layout.AngleUnit / StandardAngleUnit) : 0.15f;
            float xzLength = vector.magnitude - correction; // 别问，问就不知道为什么用原长度电梯吸附后比地面高0.3层，导致过近的带子无法连接
            // 计算向量角度（0°~360°）
            double angle = GetLongitudeAngle(vector.z, vector.x);
            double minDiff = double.MaxValue;
            double closest = 0;

            int markerCount = isFullRing ? layout.StationCapacity : layout.ElevatorAnchorCount;
            for (int index = 0; index < markerCount; index++) {
                double marker = isFullRing ? layout.GetStationAngle(index) : layout.GetElevatorAnchorAngle(index);
                double diff = GetAngleDiff(angle, marker); // 环形差值处理
                if (diff < minDiff) {
                    minDiff = diff;
                    closest = marker;
                }
            }

            double markerRad = closest * Math.PI / 180;
            float newX = (float)(xzLength * Math.Cos(markerRad));
            float newZ = (float)(xzLength * Math.Sin(markerRad));
            return new Vector3(newX, vector.y, newZ);
        }

        public static (Vector3 pos, bool isAdsorb) ShouldBeAdsorb(Vector3 vector, OrbitalRingLayout layout)
        {
            // 获取原始向量长度
            float originalLength = vector.magnitude;
            // 归一化原始向量
            Vector3 normalized = Vector3.Normalize(vector);
            double latitudeDeg = GetLatitudeAngle(vector);
            double border = 20.5d * layout.AngleUnit;
            if (-border <= latitudeDeg && latitudeDeg <= border) {
                // 确定目标纬度方向（保持原始符号）
                if (latitudeDeg == 0) latitudeDeg = 1;
                double targetLatDeg = layout.HasLowerStationRing ? Math.Sign(latitudeDeg) * 10d * layout.AngleUnit : 0d;
                double targetLatRad = targetLatDeg * Math.PI / 180;
                // 计算目标y分量（正弦值）
                double targetY = Math.Sin(targetLatRad);
                // 计算x-z平面的缩放因子
                double xzScale = Math.Sqrt((1 - targetY * targetY) /
                                           (normalized.x * normalized.x + normalized.z * normalized.z));

                // 构建新向量
                Vector3 adjusted = new Vector3(
                    (float)(normalized.x * xzScale * originalLength),
                    (float)(targetY * originalLength),
                    (float)(normalized.z * xzScale * originalLength)
                );

                return (adjusted, true);
            }
            return (vector, false);
        }

        public static Vector3 BeltShouldBeAdsorb(Vector3 vector, Vector3 startPos, int islimitSide, OrbitalRingLayout layout)
        {
            // 获取原始向量长度
            float originalLength = vector.magnitude;

            double startLatitudeDeg = 114514;
            if (startPos.sqrMagnitude != 0) {
                startLatitudeDeg = GetLatitudeAngle(startPos);
            }

            // 归一化原始向量
            Vector3 normalized = Vector3.Normalize(vector);
            double latitudeDeg = GetLatitudeAngle(vector);
            double border = 25.5d * layout.AngleUnit;
            double targetBorder = 0d;
            if (!layout.HasLowerStationRing) {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    targetBorder = latitudeDeg >= 0 ? 3d * layout.AngleUnit : -3d * layout.AngleUnit;
                    if (islimitSide == 2) {
                        targetBorder = 3d * layout.AngleUnit;
                    } else if (islimitSide == 1) {
                        targetBorder = -3d * layout.AngleUnit;
                    }
                }
            } else if (-border <= latitudeDeg && latitudeDeg <= border) {
                if (latitudeDeg >= 10d * layout.AngleUnit) {
                    targetBorder = 12d * layout.AngleUnit;
                } else if (0 <= latitudeDeg && latitudeDeg < 10d * layout.AngleUnit) {
                    targetBorder = 8d * layout.AngleUnit;
                } else if (-10d * layout.AngleUnit < latitudeDeg && latitudeDeg < 0) {
                    targetBorder = -8d * layout.AngleUnit;
                } else {
                    targetBorder = -12d * layout.AngleUnit;
                }
                if (islimitSide == 2) {
                    targetBorder = Math.Sign(targetBorder) * 12d * layout.AngleUnit;
                } else if (islimitSide == 1) {
                    targetBorder = Math.Sign(targetBorder) * 8d * layout.AngleUnit;
                }
            }

            if (startLatitudeDeg != 114514) {
                targetBorder = startLatitudeDeg;
            }
            if (targetBorder == 0d) {
                return new Vector3(300, 300, 300);
            }

            // 确定目标纬度方向（保持原始符号）
            double targetLatRad = targetBorder * Math.PI / 180;
            // 计算目标y分量（正弦值）
            double targetY = Math.Sin(targetLatRad);
            // 计算x-z平面的缩放因子
            double xzScale = Math.Sqrt((1 - targetY * targetY) /
                                       (normalized.x * normalized.x + normalized.z * normalized.z));

            // 构建新向量
            return new Vector3(
                (float)(normalized.x * xzScale * originalLength),
                (float)(targetY * originalLength),
                (float)(normalized.z * xzScale * originalLength)
            );
        }

        public static (int ringBeltIndex, int ringIndex) CalculateRingPosMark(Vector3 pos, OrbitalRingLayout layout)
        {
            int ringIndex = -1;
            int ringBeltIndex = -1;
            // 坐标归一化后计算纬度角度（使用asin直接获取极角）
            double latitudeDeg = GetLatitudeAngle(pos);
            double border = 25.5d * layout.AngleUnit;

            // ringIndex 表示上/下设施圈；ringBeltIndex 表示该设施圈周围的内/外轨道带。
            if (!layout.HasLowerStationRing) {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    ringIndex = 0;
                    ringBeltIndex = latitudeDeg >= 0 ? 2 : 1;
                }
            } else if (-border <= latitudeDeg && latitudeDeg <= border) {
                if (latitudeDeg >= 10d * layout.AngleUnit) {
                    ringIndex = 0;
                    ringBeltIndex = 2;
                } else if (0 <= latitudeDeg) {
                    ringIndex = 0;
                    ringBeltIndex = 1;
                } else if (-10d * layout.AngleUnit < latitudeDeg) {
                    ringIndex = 1;
                    ringBeltIndex = 1;
                } else {
                    ringIndex = 1;
                    ringBeltIndex = 2;
                }
            }
            return (ringBeltIndex, ringIndex);
        }

        public static int isBeltBuildingPosYCorrect(Vector3 vector, int islimitSide, OrbitalRingLayout layout)
        {
            int ret = -1;
            // 归一化向量
            // 计算纬度角度（使用asin直接获取极角）
            double latitudeDeg = GetLatitudeAngle(vector);
            if (latitudeDeg >= 0) {
                ret = 0;
            } else {
                ret = 1;
            }
            latitudeDeg = Math.Abs(latitudeDeg);

            if (!layout.HasLowerStationRing) {
                // 计算目标格边界
                double lower = 2.5d * layout.AngleUnit;
                double upper = 3.5d * layout.AngleUnit;
                // 包含边界检查
                return latitudeDeg > lower && latitudeDeg < upper ? 0 : -1;
            }

            // 计算目标格边界
            double insideLower = 7.5d * layout.AngleUnit;
            double insideUpper = 8.5d * layout.AngleUnit;
            // 包含边界检查
            if (latitudeDeg > insideLower && latitudeDeg < insideUpper) {
                if (islimitSide == 2)
                {
                    return -1;
                }
                return ret;
            }

            double outsideLower = 11.5d * layout.AngleUnit;
            double outsideUpper = 12.5d * layout.AngleUnit;
            if (latitudeDeg > outsideLower && latitudeDeg < outsideUpper) {
                if (islimitSide == 1)
                {
                    return -1;
                }
                return ret;
            }

            return -1;
        }

        public static int split_inc(ref int n, ref int m, int p)
        {
            if (n == 0) {
                return 0;
            }
            if (n - p < 0) {
                p = n;
            }
            int num = m / n;
            int num2 = m - num * n;
            n -= p;
            num2 -= n;
            num = ((num2 > 0) ? (num * p + num2) : (num * p));
            m -= num;
            return num;
        }
    }
}
