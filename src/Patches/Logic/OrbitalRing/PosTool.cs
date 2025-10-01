using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    static class PosTool
    {
        // 预计算所有标记角度（单位：度），赤道上下两圈，一圈40个
        private static readonly double[] MarkerAngles = new double[40];
        private const double AngleTolerance = 0.25; // 允许的角度误差（度）

        public static void InitializeMarkerAngles()
        {
            for (int i = 0; i < 40; i++) {
                MarkerAngles[i] = (i * 9.0) % 360.0; // 每9度一个标记
            }
        }

        public static int IsBuildingPosXZCorrect(double x, double y)
        {
            // 计算向量角度（0°~360°）
            double angle = (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;

            // 检查是否接近任意标记
            for (int i = 0; i < MarkerAngles.Length; i++) {
                double diff = Math.Abs(angle - MarkerAngles[i]);
                diff = Math.Min(diff, 360.0 - diff); // 处理环形差值
                if (diff <= AngleTolerance) {
                    return i;
                }
            }
            return -1;
        }

        public static Vector3 ShouldBeXZAdsorb(Vector3 vector)
        {
            float length = vector.magnitude - 0.4f; // 别问，问就不知道为什么用原长度电梯吸附后比地面高0.3层，导致过近的带子无法连接
            // 计算向量角度（0°~360°）
            double angle = (Math.Atan2(vector.z, vector.x) * 180.0 / Math.PI + 360.0) % 360.0;
            double minDiff = double.MaxValue;
            double closest = 0;
            foreach (double marker in MarkerAngles) {
                double diff = Math.Abs(angle - marker);
                diff = Math.Min(diff, 360 - diff); // 环形差值处理

                if (diff < minDiff) {
                    minDiff = diff;
                    closest = marker;
                }
            }

            double markerRad = closest * Math.PI / 180;
            float newX = (float)(length * Math.Cos(markerRad));
            float newZ = (float)(length * Math.Sin(markerRad));
            return new Vector3(newX, vector.y, newZ);
        }

        public static (Vector3 pos, bool isAdsorb) ShouldBeAdsorb(Vector3 vector)
        {
            // 获取原始向量长度
            float originalLength = vector.magnitude;

            // 归一化原始向量
            Vector3 normalized = Vector3.Normalize(vector);
            double latitudeRad = Math.Asin(normalized.y);
            double latitudeDeg = latitudeRad * (180.0 / Math.PI);
            double border = 20 * 0.36 + 0.18;
            if (-border <= latitudeDeg && latitudeDeg <= border) {
                // 确定目标纬度方向（保持原始符号）
                double targetLatDeg = Math.Sign(latitudeDeg) * 3.6;
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
            } else {
                return (vector, false);
            }
        }

        public static int isBuildingPosYCorrect(Vector3 vector)
        {
            int ret = -1;
            // 归一化向量
            Vector3 normalized = Vector3.Normalize(vector);
            // 计算纬度角度（使用asin直接获取极角）
            double latitudeRad = Math.Asin(normalized.y);
            double latitudeDeg = latitudeRad * (180.0 / Math.PI);
            if (latitudeDeg >= 0) {
                ret = 0;
            } else {
                ret = 1;
            }
            latitudeDeg = Math.Abs(latitudeDeg);

            // 计算目标格边界
            double lower = 9 * 0.36 + 0.18;
            double upper = 10 * 0.36 + 0.18;

            // 包含边界检查
            if (latitudeDeg > lower && latitudeDeg < upper) {
                return ret;
            } else {
                return -1;
            }
        }
    }
}
