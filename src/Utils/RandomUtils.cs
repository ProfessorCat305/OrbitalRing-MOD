using System;

namespace ProjectOrbitalRing.Utils {
    internal static class RandomUtils {
        /// <summary>
        /// 戴森球的随机数发生器。
        /// </summary>
        public static double GetRandDouble(ref uint seed) {
            seed = (uint)((seed % 2147483646U + 1U) * 48271UL % int.MaxValue) - 1U;
            return seed / 2147483646.0;
        }

        public static uint randSeed = (uint)new Random().Next(1, 2147483646);

        public static int GetRandInt(int min, int max) {
            // if (min >= max) return min;
            randSeed = (uint)((randSeed % 2147483646U + 1U) * 48271UL % int.MaxValue) - 1U;
            double randomValue = randSeed / 2147483646.0;
            return (int)(randomValue * (max - min)) + min;
        }

        public static double GetRandDouble() {
            randSeed = (uint)((randSeed % 2147483646U + 1U) * 48271UL % int.MaxValue) - 1U;
            return randSeed / 2147483646.0;
        }
    }
}
