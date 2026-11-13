using ProjectOrbitalRing.Utils;
using UnityEngine;
using static ProjectOrbitalRing.Rendering.OrbitalFacilityAttachments;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalFacilityBays
    {
        private readonly struct BayFrame
        {
            internal readonly Vector3 Center;
            internal readonly Vector3 Along;
            internal readonly Vector3 Up;
            internal readonly Vector3 Outward;
            internal readonly Vector3 Station;
            internal readonly float Length;

            internal BayFrame(Vector3 first, Vector3 last, Vector3 station)
            {
                Center = (first + last) * 0.5f;
                Along = (last - first).normalized;
                Up = Center.normalized;
                Vector3 across = Vector3.Cross(Along, Up).normalized;
                Outward = Vector3.Dot(Center - station, across) >= 0 ? across : -across;
                Station = station;
                Length = Vector3.Distance(first, last);
            }

            internal Vector3 Point(float outward, float height, float along = 0)
            {
                return Center + Outward * outward + Up * height + Along * along;
            }
        }

        internal static void Beam(OrbitalAttachmentMesh[] surfaces, PlanetFactory factory, int ring,
            float longitude, EntityData entity)
        {
            if (!TryFrame(factory, ring, 1, longitude, ProtoID.I粒子加速轨道, entity, 1.4f, out BayFrame frame))
            {
                return;
            }

            AcceleratorHousing(surfaces, frame);
            if (entity.protoId == ProtoID.I星环对撞机)
            {
                var towerSurface = new OrbitalTrackIntersection.TowerSurface(entity);
                ColliderPortSaddle(surfaces, towerSurface, frame);
                ColliderTransfer(surfaces, towerSurface, frame);
            }
        }

        private static void AcceleratorHousing(OrbitalAttachmentMesh[] surfaces, BayFrame frame)
        {
            OrbitalAttachmentMesh armor = surfaces[(int)Surface.Armor];
            OrbitalAttachmentMesh recess = surfaces[(int)Surface.Recess];
            OrbitalAttachmentMesh lights = surfaces[(int)Surface.BeamLight];
            armor.AddAxialPart("accelerator-shell", frame.Center, frame.Along, frame.Up, frame.Length);
            // The lining uses world-length UVs; the outer shell uses normalized UVs.
            armor.TubeLining(frame.Center, frame.Along, frame.Up, 1.16f, frame.Length);
            recess.AddAxialPart("accelerator-seam", frame.Center, frame.Along, frame.Up);
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector3 endpoint = frame.Point(0, 0, sign * (frame.Length * 0.5f - 0.11f));
                OrbitalFacilityGeometry.AddAssembly(surfaces, "accelerator-end", endpoint,
                    frame.Outward, frame.Up, frame.Along * sign, Surface.Armor, Surface.Recess, Surface.Metal);

                Vector3 firstLight = frame.Point(1.20f, sign * 0.40f, -frame.Length * 0.43f);
                Vector3 lastLight = frame.Point(1.20f, sign * 0.40f, frame.Length * 0.43f);
                lights.AddRod("accelerator-light", firstLight, lastLight);
            }
        }

        internal static void Power(OrbitalAttachmentMesh[] surfaces, PlanetFactory factory, int ring,
            float longitude, EntityData entity)
        {
            if (!TryFrame(factory, ring, 2, longitude, ProtoID.I星环电网组件, entity, 0.95f, out BayFrame frame))
            {
                return;
            }

            OrbitalAttachmentMesh armor = surfaces[(int)Surface.Armor];
            OrbitalAttachmentMesh recess = surfaces[(int)Surface.Recess];
            OrbitalAttachmentMesh metal = surfaces[(int)Surface.Metal];
            OrbitalAttachmentMesh ceramic = surfaces[(int)Surface.Ceramic];
            OrbitalAttachmentMesh conductor = surfaces[(int)Surface.Conductor];
            OrbitalAttachmentMesh lights = surfaces[(int)Surface.PowerLight];
            OrbitalAttachmentMesh panel = surfaces[(int)Surface.PowerPanel];
            recess.BeveledBox(frame.Center, frame.Outward, frame.Up, frame.Along,
                new Vector3(1.80f, 1.15f, frame.Length), 0.18f);
            armor.BeveledBox(frame.Point(-0.02f, 0.35f), frame.Outward, frame.Up, frame.Along,
                new Vector3(1.64f, 0.50f, frame.Length * 0.92f), 0.15f);
            armor.BeveledBox(frame.Point(-0.02f, -0.40f), frame.Outward, frame.Up, frame.Along,
                new Vector3(1.58f, 0.38f, frame.Length * 0.92f), 0.12f);
            foreach (float sign in new[] { -1f, 1f })
            {
                metal.BeveledBox(frame.Point(0.90f, sign * 0.37f), frame.Outward, frame.Up, frame.Along,
                    new Vector3(0.045f, 0.07f, frame.Length * 0.80f), 0.02f);
                Vector3 endpoint = frame.Point(0, 0, sign * (frame.Length * 0.5f - 0.08f));
                OrbitalFacilityGeometry.AddAssembly(surfaces, "power-end", endpoint,
                    frame.Outward, frame.Up, frame.Along * sign,
                    Surface.Armor, Surface.Metal, Surface.Ceramic, Surface.Conductor);
            }

            bool reflected = Vector3.Dot(Vector3.Cross(frame.Along, frame.Up), frame.Outward) < 0;
            panel.AddPart(reflected ? "power-panel-reflected" : "power-panel", frame.Point(0.934f, 0),
                frame.Along * (frame.Length * 0.90f), frame.Up * 0.74f, frame.Outward);
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector3 statusLamp = frame.Point(0.95f, 0.18f, sign * frame.Length * 0.46f);
                OrbitalFacilityGeometry.AddAssembly(surfaces, "power-status", statusLamp,
                    Vector3.Cross(frame.Up, frame.Outward).normalized, frame.Up, frame.Outward,
                    Surface.Metal, Surface.PowerLight);
            }

            Vector3 towerUp = frame.Station.normalized;
            Vector3 outward = Vector3.ProjectOnPlane(frame.Outward, towerUp).normalized;
            float terminalHeight = Vector3.Dot(frame.Center - frame.Station, towerUp) + 3.25f;
            var towerSurface = new OrbitalTrackIntersection.TowerSurface(entity);
            float terminalRadius = towerSurface.Radius(terminalHeight, outward);
            armor.ArcBand(frame.Station + towerUp * terminalHeight, towerUp, outward,
                terminalRadius - 0.05f, terminalRadius + 0.20f, 0.58f, 20 * Mathf.Deg2Rad, 12);
            foreach (float lateral in new[] { -0.3645f, 0, 0.3645f })
            {
                Vector3 lateralOffset = frame.Along * (lateral * 1.4f);
                float radius = MountingDepth(towerSurface, terminalHeight, outward, lateralOffset,
                    terminalRadius + 0.20f);
                Vector3 terminal = frame.Station + towerUp * terminalHeight + lateralOffset + outward * radius;
                Vector3 contact = frame.Point(0.65f, 0.64f, lateral * 1.4f);
                Vector3[] busbar = TransferCurve(contact, contact + towerUp * 0.65f + outward * 0.25f,
                    terminal + outward * 0.60f, terminal);
                conductor.Sweep(busbar, frame.Along, 0.055f);
                Vector3 bushingAxis = (busbar[5] - busbar[3]).normalized;
                ceramic.AddAxialPart("power-bushing", busbar[4], bushingAxis, frame.Along);
                armor.BeveledBox(terminal - outward * 0.03f, frame.Along, towerUp, outward,
                    new Vector3(0.35f, 0.42f, 0.24f), 0.06f);
                metal.AddAxialPart("power-contact", contact, towerUp, frame.Along);
                lights.AddAxialPart("power-terminal-light", terminal + outward * 0.115f, outward, towerUp);
            }
        }

        private static void ColliderPortSaddle(OrbitalAttachmentMesh[] surfaces,
            OrbitalTrackIntersection.TowerSurface towerSurface, BayFrame frame)
        {
            Vector3 towerUp = frame.Station.normalized;
            Vector3 outward = Vector3.ProjectOnPlane(frame.Outward, towerUp).normalized;
            float height = Vector3.Dot(frame.Center - frame.Station, towerUp) + 2.8f;
            float radius = towerSurface.Radius(height, outward);
            Vector3 center = frame.Station + towerUp * height;
            OrbitalAttachmentMesh armor = surfaces[(int)Surface.Armor];
            OrbitalAttachmentMesh recess = surfaces[(int)Surface.Recess];
            float halfAngle = 55 * Mathf.Deg2Rad;
            armor.ArcBand(center, towerUp, outward, radius - 0.08f, radius + 0.20f, 0.86f, halfAngle);
            foreach (float sign in new[] { -1f, 1f })
            {
                recess.ArcBand(center + towerUp * (sign * 0.28f), towerUp, outward,
                    radius + 0.18f, radius + 0.22f, 0.07f, halfAngle);
            }
        }

        private static void ColliderTransfer(OrbitalAttachmentMesh[] surfaces,
            OrbitalTrackIntersection.TowerSurface towerSurface, BayFrame frame)
        {
            OrbitalAttachmentMesh armor = surfaces[(int)Surface.Armor];
            OrbitalAttachmentMesh recess = surfaces[(int)Surface.Recess];
            OrbitalAttachmentMesh metal = surfaces[(int)Surface.Metal];
            Vector3 towerUp = frame.Station.normalized;
            Vector3 outward = Vector3.ProjectOnPlane(frame.Outward, towerUp).normalized;
            float trackHeight = Vector3.Dot(frame.Center - frame.Station, towerUp);
            float portHeight = trackHeight + 2.8f;
            float saddleRadius = towerSurface.Radius(portHeight, outward) + 0.20f;
            foreach (bool injection in new[] { true, false })
            {
                float side = injection ? 1 : -1;
                float height = portHeight;
                Vector3 lateralOffset = frame.Along * (side * 0.85f);
                float radius = MountingDepth(towerSurface, height, outward, lateralOffset, saddleRadius);
                Vector3 root = frame.Station + towerUp * height + lateralOffset + outward * radius;
                string port = injection ? "collider-injection-port" : "collider-extraction-port";
                OrbitalFacilityGeometry.AddAssembly(surfaces, port, root, frame.Along, towerUp, outward,
                    Surface.Armor, Surface.Recess, Surface.Metal);

                Vector3 towerPort = root + outward * 0.72f;
                float spacing = Mathf.Min(3.2f, frame.Length * 0.38f);
                Vector3 ringPort = frame.Point(1.04f, 0.92f, side * spacing);
                Vector3 radial = (frame.Outward * 1.04f + frame.Up * 0.92f).normalized;
                Vector3 socketSide = Vector3.Cross(radial, frame.Along).normalized;
                armor.BeveledBox(ringPort - radial * 0.19f, frame.Along, socketSide, radial,
                    new Vector3(1.12f, 0.54f, 0.37f), 0.065f);
                recess.AddAxialPart("collider-neck", ringPort, frame.Along, frame.Up);
                metal.AddAxialPart("collider-flange", ringPort - frame.Along * (side * 0.24f), frame.Along, frame.Up);
                float controlLength = Mathf.Min(spacing * 0.64f, 1.85f);
                Vector3 first = injection ? towerPort : ringPort;
                Vector3 firstControl = injection ? towerPort + outward * 1.5f : ringPort + frame.Along * controlLength;
                Vector3 lastControl = injection ? ringPort - frame.Along * controlLength : towerPort + outward * 1.5f;
                Vector3 last = injection ? ringPort : towerPort;
                Vector3[] path = TransferCurve(first, firstControl, lastControl, last);
                armor.Sweep(path, towerUp, 0.15f);
                for (int marker = 0; marker < 4; marker++)
                {
                    int index = 2 + marker * 6;
                    Vector3 tangent = (path[index + 1] - path[index - 1]).normalized;
                    metal.AddAxialPart("collider-marker-metal", path[index], tangent, towerUp);
                    surfaces[(int)Surface.BeamGuide0 + marker % 3].AddAxialPart(
                        "collider-marker-light", path[index], tangent, towerUp);
                }
            }
        }

        private static float MountingDepth(OrbitalTrackIntersection.TowerSurface towerSurface, float height,
            Vector3 outward, Vector3 lateralOffset, float saddleRadius)
        {
            float nativeDepth = towerSurface.Radius(height, outward, lateralOffset) + 0.20f;
            // Ports share the generated saddle surface, whose radius can exceed the native tower mesh.
            float saddleDepth = Mathf.Sqrt(Mathf.Max(0, saddleRadius * saddleRadius - lateralOffset.sqrMagnitude));
            return Mathf.Max(nativeDepth, saddleDepth);
        }

        private static Vector3[] TransferCurve(Vector3 first, Vector3 firstControl, Vector3 lastControl, Vector3 last)
        {
            var points = new Vector3[25];
            for (int index = 0; index < points.Length; index++)
            {
                float amount = (float)index / (points.Length - 1);
                float remaining = 1 - amount;
                Vector3 point = first * (remaining * remaining * remaining)
                    + firstControl * (3 * remaining * remaining * amount);
                point += lastControl * (3 * remaining * amount * amount);
                points[index] = point + last * (amount * amount * amount);
            }

            return points;
        }

        private static bool TryFrame(PlanetFactory factory, int ring, int band, float longitude,
            int itemId, EntityData entity, float envelopeRadius, out BayFrame frame)
        {
            Vector3 anchor = OrbitalTextureLayout.TrackCenter(factory, ring, band, longitude, itemId);
            Vector3 up = anchor.normalized;
            Vector3 along = Vector3.Cross(Vector3.up, up).normalized;
            Vector3 across = Vector3.Cross(along, up).normalized;
            if (!OrbitalTrackIntersection.TryMeasure(entity, anchor, along, across, up,
                envelopeRadius, out Vector2 interval))
            {
                frame = default(BayFrame);
                return false;
            }

            float radius = new Vector2(anchor.x, anchor.z).magnitude;
            Vector3 first = OrbitalTextureLayout.TrackCenter(factory, ring, band,
                longitude + (interval.x - 0.4f) / radius, itemId);
            Vector3 last = OrbitalTextureLayout.TrackCenter(factory, ring, band,
                longitude + (interval.y + 0.4f) / radius, itemId);
            frame = new BayFrame(first, last, entity.pos);
            return true;
        }
    }
}
