using ProjectOrbitalRing.Utils;
using UnityEngine;
using static ProjectOrbitalRing.Rendering.OrbitalFacilityAttachments;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalFacilityGeometry
    {
        internal enum SeatState
        {
            Unavailable,
            Vacant,
            Connected,
        }

        internal static void Seat(OrbitalAttachmentMesh[] surfaces, Vector3 center, Vector3 across,
            Vector3 up, Vector3 along, SeatState state)
        {
            Surface indicator = state == SeatState.Unavailable ? Surface.UnavailableLight
                : state == SeatState.Connected ? Surface.ConnectedLight : Surface.ServiceLight;
            AddAssembly(surfaces, "seat-" + state, center, across, up, along,
                Surface.Armor, Surface.Recess, Surface.Metal, indicator);
        }

        internal static void AddAssembly(OrbitalAttachmentMesh[] surfaces, string name, Vector3 center,
            Vector3 across, Vector3 up, Vector3 along, params Surface[] parts)
        {
            foreach (Surface surface in parts)
            {
                OrbitalAttachmentMesh builder = surfaces[(int)surface];
                string address = name + "-lod" + (builder.IsDistant ? 1 : 0) + "-surface" + (int)surface;
                builder.AddPart(address, center, across, up, along);
            }
        }

        internal static Vector2 SeatLongitudes(PlanetFactory factory, int slot)
        {
            return SeatLongitudes(factory.planet, slot);
        }

        internal static Vector2 SeatLongitudes(PlanetData planet, int slot)
        {
            float center = OrbitalTextureLayout.FacilityLongitude(planet, slot);
            Vector3 anchor = OrbitalTextureLayout.TrackCenterOnPlanet(planet, 0, 1, center, ProtoID.I轨道连接组件);
            float radius = new Vector2(anchor.x, anchor.z).magnitude;
            var longitudes = new Vector2(center, center);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1 : 1;
                for (int step = 1; step <= 4; step++)
                {
                    float coordinate = slot + sign * step / OrbitalTextureLayout.RepeatsPerFacility;
                    float longitude = OrbitalTextureLayout.FacilityLongitude(planet, coordinate);
                    float angle = Mathf.DeltaAngle(center * Mathf.Rad2Deg, longitude * Mathf.Rad2Deg);
                    float distance = Mathf.Abs(angle) * Mathf.Deg2Rad * radius;
                    if (distance > 14.8f)
                    {
                        break;
                    }

                    longitudes[side] = longitude;
                }
            }

            return longitudes;
        }

        internal static void Support(OrbitalAttachmentMesh[] surfaces, PlanetFactory factory, int ring,
            float longitude, EntityData entity, int tracks, Vector2 seatLongitudes)
        {
            Vector3 up = entity.pos.normalized;
            Vector3 along = Vector3.Cross(up, Vector3.up).normalized;
            Vector3 across = Vector3.Cross(along, up).normalized;
            Mesh mesh = LDB.models.Select(entity.modelIndex)?.prefabDesc?.lodMeshes?[0];
            if (mesh == null || !mesh.isReadable)
            {
                return;
            }

            Vector3[] vertices = mesh.vertices;
            OrbitalAttachmentMesh armor = surfaces[(int)Surface.Armor];
            OrbitalAttachmentMesh recess = surfaces[(int)Surface.Recess];
            OrbitalAttachmentMesh metal = surfaces[(int)Surface.Metal];
            for (int band = 1; band <= 2; band++)
            {
                if ((tracks & band) == 0)
                {
                    continue;
                }
                Vector3 bandCenter = OrbitalTextureLayout.TrackCenter(factory, ring, band, longitude, ProtoID.I轨道连接组件);
                float lateral = Vector3.Dot(bandCenter - entity.pos, across);
                foreach (float sign in new[] { -1f, 1f })
                {
                    Vector3 footCenter = OrbitalTextureLayout.TrackCenter(factory, ring, band,
                        sign < 0 ? seatLongitudes.x : seatLongitudes.y, ProtoID.I轨道连接组件);
                    Vector3 foot = footCenter + footCenter.normalized * 1.32f;
                    Vector3 target = entity.pos + up * 30.8f + along * (sign * 13.2f) + across * lateral;
                    Vector3 root = ClosestVertex(vertices, entity, target);
                    recess.BeveledBeam(foot, root, across, 0.44f, 0.48f, 0.08f);
                    armor.BeveledBeam(foot + across * 0.17f, root + across * 0.17f, across, 0.27f, 0.15f, 0.04f);
                    armor.BeveledBeam(foot - across * 0.17f, root - across * 0.17f, across, 0.27f, 0.15f, 0.04f);
                    armor.BeveledBox(root - up * 0.08f, across, up, along, new Vector3(0.95f, 0.20f, 0.76f), 0.08f);
                    metal.AddAxialPart("support-hinge", foot, across, up);
                    Vector3 outwardSide = across * (lateral >= 0 ? 1 : -1);
                    Vector3 firstLight = Vector3.Lerp(foot, root, 0.38f) + outwardSide * 0.27f;
                    Vector3 lastLight = Vector3.Lerp(foot, root, 0.65f) + outwardSide * 0.27f;
                    surfaces[(int)Surface.ConnectedLight].AddRod("support-light", firstLight, lastLight);
                }
            }
        }

        private static Vector3 ClosestVertex(Vector3[] vertices, EntityData entity, Vector3 target)
        {
            Vector3 local = Quaternion.Inverse(entity.rot) * (target - entity.pos);
            float nearest = float.PositiveInfinity;
            Vector3 result = Vector3.zero;
            foreach (Vector3 vertex in vertices)
            {
                float distance = (vertex - local).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    result = vertex;
                }
            }

            return entity.pos + entity.rot * result;
        }
    }
}
