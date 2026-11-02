using System;
using System.Runtime.CompilerServices;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Rendering
{
    internal readonly struct OrbitalTextureLayout
    {
        internal const float CoordinatePeriod = 512f;
        internal const int RepeatsPerFacility = 10;
        private static readonly ConditionalWeakTable<PlanetData, FacilityAngles> angles =
            new ConditionalWeakTable<PlanetData, FacilityAngles>();

        internal readonly int RepeatCount;
        internal readonly float RepeatLength;
        internal readonly float UvScale;

        private OrbitalTextureLayout(double circumference, int facilityCount)
        {
            RepeatCount = checked(facilityCount * RepeatsPerFacility);
            RepeatLength = (float)(circumference / RepeatCount);
            UvScale = RepeatCount / CoordinatePeriod;
        }

        private sealed class FacilityAngles
        {
            internal readonly int Count;
            internal readonly double AngleUnit;
            internal readonly bool HasLowerStationRing;
            internal readonly double[] Degrees;

            internal FacilityAngles(OrbitalRingLayout layout)
            {
                Count = layout.StationCapacity;
                AngleUnit = layout.AngleUnit;
                HasLowerStationRing = layout.HasLowerStationRing;
                Degrees = new double[Count + 1];
                for (int index = 0; index < Count; index++)
                {
                    Degrees[index] = layout.GetStationAngle(index);
                }

                Degrees[Count] = 360;
            }
        }

        private static FacilityAngles GetFacilityAngles(PlanetData planet)
        {
            if (planet == null)
            {
                throw new ArgumentNullException(nameof(planet));
            }

            var layout = GetLayout(planet);
            if (angles.TryGetValue(planet, out var cached) && cached.Count == layout.StationCapacity
                && cached.AngleUnit == layout.AngleUnit && cached.HasLowerStationRing == layout.HasLowerStationRing)
            {
                return cached;
            }

            angles.Remove(planet);
            cached = new FacilityAngles(layout);
            angles.Add(planet, cached);
            return cached;
        }

        internal static float FacilityCoordinate(PlanetData planet, Vector3 position)
        {
            var slots = GetFacilityAngles(planet);
            double longitude = Math.Atan2(position.z, position.x) * 180 / Math.PI;
            if (double.IsNaN(longitude))
            {
                throw new InvalidOperationException("Track longitude must be finite.");
            }
            if (longitude < 0)
            {
                longitude += 360;
            }

            int slot = Math.Min(slots.Count - 1, (int)(longitude * slots.Count / 360));
            while (slot > 0 && longitude < slots.Degrees[slot])
            {
                slot--;
            }

            while (slot + 1 < slots.Count && longitude >= slots.Degrees[slot + 1])
            {
                slot++;
            }

            double fraction = (longitude - slots.Degrees[slot]) / (slots.Degrees[slot + 1] - slots.Degrees[slot]);
            return (float)(slot + fraction);
        }

        internal static float FacilityLongitude(PlanetData planet, float facilityCoordinate)
        {
            var slots = GetFacilityAngles(planet);
            float coordinate = Mathf.Repeat(facilityCoordinate, slots.Count);
            int slot = Mathf.FloorToInt(coordinate);
            double interval = slots.Degrees[slot + 1] - slots.Degrees[slot];
            double longitude = slots.Degrees[slot] + interval * (coordinate - slot);
            return (float)longitude * Mathf.Deg2Rad;
        }

        internal static bool IsFacilitySlotAvailable(PlanetData planet, int ring, int slot)
        {
            var layout = GetLayout(planet);
            if (ring < 0 || ring >= layout.StationRingCount || slot < 0 || slot >= layout.StationCapacity)
            {
                return false;
            }

            var data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(planet.id);
            if (data == null || ring >= data.Rings.Count)
            {
                return false;
            }

            if (data.Rings[ring].IsAllFull())
            {
                return true;
            }

            int anchor = slot / 2;
            return layout.MapElevatorAnchorToStationIndex(anchor) == slot;
        }

        internal static OrbitalTextureLayout FromBand(PlanetData planet, int itemId, int band, float centerHeight)
        {
            if (band != 1 && band != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(band));
            }

            if (!TryGetTrackPlacement(itemId, out _, out int altitude))
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            var layout = GetLayout(planet);
            double latitudeSteps = layout.HasLowerStationRing ? (band == 1 ? 8 : 12) : 3;
            double latitude = latitudeSteps * layout.AngleUnit * Math.PI / 180;
            double centerRadius = GetSurfaceRadius(planet) + 0.2 + altitude * 1.3333333 + centerHeight;
            double circumference = Math.PI * 2 * centerRadius * Math.Cos(latitude);
            if (double.IsNaN(circumference) || double.IsInfinity(circumference) || circumference <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(planet));
            }

            return new OrbitalTextureLayout(circumference, layout.StationCapacity);
        }

        internal float DistanceToPhase(float worldDistance) => worldDistance / RepeatLength;

        internal static Vector3 TrackCenter(PlanetFactory factory, int ring, int band, float longitude, int itemId)
        {
            return TrackCenterOnPlanet(factory.planet, ring, band, longitude, itemId);
        }

        internal static Vector3 TrackCenterOnPlanet(PlanetData planet, int ring, int band, float longitude, int itemId)
        {
            if (!TryGetTrackPlacement(itemId, out _, out int altitude))
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }
            float radius = GetSurfaceRadius(planet) + 0.2f + altitude * 1.3333333f;
            float sign = ring == 0 ? 1 : -1;
            Vector3 radialDirection = new Vector3(Mathf.Cos(longitude), sign * 0.05f, Mathf.Sin(longitude)).normalized;
            Vector3 candidate = radialDirection * radius;
            Vector3 position = BeltShouldBeAdsorb(candidate, Vector3.zero, band, GetLayout(planet));
            float height = TrackDefinition.Get(itemId)?.CenterHeight ?? 0;
            return position + position.normalized * height;
        }
    }
}
