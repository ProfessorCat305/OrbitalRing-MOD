using System.Diagnostics;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalMeshBuildBudget
    {
        private static int buildFrame = -1;
        private static long buildTicks;

        internal static bool HasBuildBudget
        {
            get
            {
                if (buildFrame != Time.frameCount)
                {
                    buildFrame = Time.frameCount;
                    buildTicks = 0;
                }

                return buildTicks < Stopwatch.Frequency * 0.002;
            }
        }

        internal static void RecordBuildTime(long started)
        {
            buildTicks += Stopwatch.GetTimestamp() - started;
        }
    }
}
