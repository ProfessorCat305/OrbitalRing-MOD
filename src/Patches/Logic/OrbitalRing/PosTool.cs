using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    static class PosTool
    {
        // 预计算所有标记角度（单位：度），赤道上下两圈，一圈40个
        private static readonly double[] MarkerAngles = new double[40];
        private static readonly double[] RingMarkerAngles = new double[1000];
        private const double AngleTolerance = 0.25; // 允许的角度误差（度）

        public static void InitializeMarkerAngles()
        {
            for (int i = 0; i < 40; i++) {
                MarkerAngles[i] = (i * 9.0) % 360.0; // 每9度一个标记
            }
            for (int i = 0; i < 1000; i++) {
                RingMarkerAngles[i] = (i * 0.36) % 360.0; // 每0.36度一个标记
            }
        }

        public static int IsBuildingPosXZCorrect(double x, double y, bool isFullRing, bool isMoon)
        {
            // 计算向量角度（0°~360°）
            double angle = (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;

            // 检查是否接近任意标记
            for (int i = 0; i < MarkerAngles.Length; i++) {
                if (!isFullRing && (i % 2) != 0) {
                    continue;
                }
                double diff = Math.Abs(angle - MarkerAngles[i]);
                diff = Math.Min(diff, 360.0 - diff); // 处理环形差值
                if (diff <= AngleTolerance) {
                    if (isMoon) {
                        return i / 2;
                    }
                    return i;
                }
            }
            return -1;
        }
        public static int isBuildingPosYCorrect(Vector3 vector, bool isMoon)
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
            if (isMoon) {
                lower = -0.18;
                upper = 0 * 0.36 + 0.18;
                ret = 0;
            }

            // 包含边界检查
            if (latitudeDeg > lower && latitudeDeg < upper) {
                return ret;
            } else {
                return -1;
            }
        }

        public static Vector3 ShouldBeXZAdsorb(Vector3 vector, bool isFullRing, bool isMoon)
        {
            float length = vector.magnitude - 0.4f; // 别问，问就不知道为什么用原长度电梯吸附后比地面高0.3层，导致过近的带子无法连接
            // 计算向量角度（0°~360°）
            double angle = (Math.Atan2(vector.z, vector.x) * 180.0 / Math.PI + 360.0) % 360.0;
            double minDiff = double.MaxValue;
            double closest = 0;
            int index = 0;
            foreach (double marker in MarkerAngles) {
                if (isMoon) {
                    if (index % (isFullRing ? 2 : 4) != 0) {
                        index++;
                        continue;
                    }
                } else {
                    if (index % ((isFullRing) ? 0 : 2) != 0) {
                        index++;
                        continue;
                    }
                }
                double diff = Math.Abs(angle - marker);
                diff = Math.Min(diff, 360 - diff); // 环形差值处理

                if (diff < minDiff) {
                    minDiff = diff;
                    closest = marker;
                }
                index++;
            }

            double markerRad = closest * Math.PI / 180;
            float newX = (float)(length * Math.Cos(markerRad));
            float newZ = (float)(length * Math.Sin(markerRad));
            return new Vector3(newX, vector.y, newZ);
        }

        public static (Vector3 pos, bool isAdsorb) ShouldBeAdsorb(Vector3 vector, bool isMoon)
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
                if (latitudeDeg == 0) latitudeDeg = 1;
                double targetLatDeg = Math.Sign(latitudeDeg) * (isMoon ? 0 : 3.6);
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

        public static Vector3 BeltShouldBeAdsorb(Vector3 vector, Vector3 startPos, int islimitSide, bool isMoon)
        {
            // 获取原始向量长度
            float originalLength = vector.magnitude;

            double startLatitudeDeg = 114514;
            if (startPos.sqrMagnitude != 0) {
                Vector3 startNormalized = Vector3.Normalize(startPos);
                double startLatitudeRad = Math.Asin(startNormalized.y);
                startLatitudeDeg = startLatitudeRad * (180.0 / Math.PI);
            }
            
            // 归一化原始向量
            Vector3 normalized = Vector3.Normalize(vector);
            double latitudeRad = Math.Asin(normalized.y);
            double latitudeDeg = latitudeRad * (180.0 / Math.PI);
            double border = 25 * 0.36 + 0.18;
            double targetBorder = 0d;
            if (isMoon) {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    if (latitudeDeg >= 0) {
                        targetBorder = 3 * 0.36;
                    } else {
                        targetBorder = -3 * 0.36;
                    }
                    if (islimitSide == 2) {
                        targetBorder = Math.Sign(targetBorder) * 3 * 0.36;
                    } else if (islimitSide == 1) {
                        targetBorder = Math.Sign(targetBorder) * -3 * 0.36;
                    }
                }
            } else {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    if (latitudeDeg >= 10 * 0.36) {
                        targetBorder = 12 * 0.36;
                    } else if (0 <= latitudeDeg && latitudeDeg < 10 * 0.36) {
                        targetBorder = 8 * 0.36;
                    } else if (-10 * 0.36 < latitudeDeg && latitudeDeg < 0) {
                        targetBorder = -8 * 0.36;
                    } else {
                        targetBorder = -12 * 0.36;
                    }
                    if (islimitSide == 2) {
                        targetBorder = Math.Sign(targetBorder) * 12 * 0.36;
                    } else if (islimitSide == 1) {
                        targetBorder = Math.Sign(targetBorder) * 8 * 0.36;
                    }
                }
            }
            if (startLatitudeDeg != 114514) {
                targetBorder = startLatitudeDeg;
            }
            if (targetBorder != 0d) {
                // 确定目标纬度方向（保持原始符号）
                double targetLatDeg = targetBorder;
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

                return adjusted;
            } else {
                return new Vector3(300, 300, 300);
            }
        }

        public static (int positionIndex, int ringBeltIndex, int ringIndex) CalculateRingPosMark(Vector3 pos, bool isMoon)
        {
            int index = -1;
            int ringIndex = -1;
            int ringBeltIndex = -1;
            // 计算向量角度（0°~360°）
            double angle = (Math.Atan2(pos.z, pos.x) * 180.0 / Math.PI + 360.0) % 360.0;
            // 检查是否接近任意标记
            for (int i = 0; i < RingMarkerAngles.Length; i++) {
                double diff = Math.Abs(angle - RingMarkerAngles[i]);
                diff = Math.Min(diff, 360.0 - diff); // 处理环形差值
                if (diff <= AngleTolerance) {
                    index = i;
                    //LogError($"CalculateRingPosMark index {index}");
                }
            }
            Vector3 normalized = Vector3.Normalize(pos);
            double latitudeRad = Math.Asin(normalized.y);
            double latitudeDeg = latitudeRad * (180.0 / Math.PI);
            double border = 25 * 0.36 + 0.18;
            //LogError($"CalculateRingPosMark latitudeDeg {latitudeDeg}");
            if (isMoon) {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    if (latitudeDeg >= 0) {
                        ringIndex = 0;
                        ringBeltIndex = 1;
                    } else {
                        ringIndex = 0;
                        ringBeltIndex = 2;
                    }
                }
            } else {
                if (-border <= latitudeDeg && latitudeDeg <= border) {
                    if (latitudeDeg >= 10 * 0.36) {
                        ringIndex = 0;
                        ringBeltIndex = 2;
                    } else if (0 <= latitudeDeg) {
                        ringIndex = 0;
                        ringBeltIndex = 1;
                    } else if (-10 * 0.36 < latitudeDeg) {
                        ringIndex = 1;
                        ringBeltIndex = 1;
                    } else {
                        ringIndex = 1;
                        ringBeltIndex = 2;
                    }
                }
            }
            return (index, ringBeltIndex, ringIndex);
        }

        public static int isBeltBuildingPosYCorrect(Vector3 vector, int islimitSide, bool isMoon)
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

            if (isMoon) {
                // 计算目标格边界
                double lower = 2 * 0.36 + 0.18;
                double upper = 3 * 0.36 + 0.18;
                // 包含边界检查
                if (latitudeDeg > lower && latitudeDeg < upper) {
                    return 0;
                } else {
                    return -1;
                }
            } else {
                // 计算目标格边界
                double lower = 7 * 0.36 + 0.18;
                double upper = 8 * 0.36 + 0.18;

                // 包含边界检查
                if (latitudeDeg > lower && latitudeDeg < upper) {
                    if (islimitSide == 2) {
                        return -1;
                    }
                    return ret;
                } else if (latitudeDeg > 4.14d && latitudeDeg < 4.5d) {
                    if (islimitSide == 1) {
                        return -1;
                    }
                    return ret;
                } else {
                    return -1;
                }
            }
        }

        public static int split_inc(ref int n, ref int m, int p)
        {
            if (n == 0) {
                return 0;
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
