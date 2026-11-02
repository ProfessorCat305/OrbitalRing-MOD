using System.Collections.Generic;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static partial class AirBeltPowerRenderer
    {
        private sealed class Geometry
        {
            private readonly List<Vector3> positions = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> coordinates = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();
            private readonly List<Color> colors = new List<Color>();
            private readonly Dictionary<int, List<int>> directionTriangles = new Dictionary<int, List<int>>();

            internal void AddJunction(Junction junction)
            {
                // Native side-entry paths stop short of the main rail's centerline.
                // Recess the overlapping bridge surface to keep the adjoining rail faces free of depth conflicts.
                Vector3 height = Vector3.up * (AirBeltMesh.BeamOffset - 0.0015f);
                var railStart = new Support(junction.Start.Point(height), junction.Start.Rotation);
                var railEnd = new Support(junction.End.Point(height), junction.Start.Rotation);
                Section(railStart, railEnd, AirBeltMesh.BeamSection, SupportSurface.Steel);
                if (!junction.HasWire)
                {
                    return;
                }

                var offset = new Vector3(AirBeltMesh.WireSide, AirBeltMesh.WireHeight, 0);
                Vector3 start = junction.Start.Point(offset);
                Vector3 end = junction.WireEnd;
                Vector3 direction = end - start;
                if (direction.sqrMagnitude < 0.00000001f)
                {
                    return;
                }
                Quaternion rotation = Quaternion.LookRotation(direction, (start + end).normalized);
                Section(new Support(start, rotation), new Support(end, rotation),
                    -AirBeltMesh.WireHalfWidth, AirBeltMesh.WireHalfWidth,
                    -AirBeltMesh.WireHalfWidth, AirBeltMesh.WireHalfWidth);
            }
            private void Section(Support start, Support end, float left, float right, float bottom, float top)
            {
                Section(start, end, new[]
                {
                    new Vector2(left, top), new Vector2(right, top),
                    new Vector2(right, bottom), new Vector2(left, bottom),
                }, SupportSurface.Frame);
            }

            private void Section(Support start, Support end, Vector2[] corners, SupportSurface surface)
            {
                for (int side = 0; side < corners.Length; side++)
                {
                    Vector3 first = start.Point(corners[side]);
                    Vector3 second = start.Point(corners[(side + 1) % corners.Length]);
                    Vector3 third = end.Point(corners[(side + 1) % corners.Length]);
                    Vector3 fourth = end.Point(corners[side]);
                    Vector3 normal = Vector3.Cross(fourth - first, second - first).normalized;
                    Quad(first, second, third, fourth, normal, surface);
                }
            }

            internal void AddDirectionLane(BeltAnchor[] anchors, int speed)
            {
                if (!directionTriangles.TryGetValue(speed, out List<int> indices))
                {
                    indices = new List<int>();
                    directionTriangles.Add(speed, indices);
                }

                int vertex = positions.Count;
                DirectionRow(anchors[0]);
                for (int index = 1; index < anchors.Length; index++, vertex += 4)
                {
                    DirectionRow(anchors[index]);
                    Vector3 normal = normals[vertex];
                    for (int strip = 0; strip < 3; strip++)
                    {
                        AddQuadIndices(indices, vertex + strip, vertex + strip + 1,
                            vertex + strip + 5, vertex + strip + 4, normal);
                    }
                }
            }

            private void DirectionRow(BeltAnchor anchor)
            {
                const float halfWidth = 0.475f;
                const float clearance = 0.08f;
                float height = AirBeltMesh.BeamTop + AirBeltMesh.BeamOffset + clearance;
                Vector2[] section = AirBeltMesh.BeamSection;
                Vector3 normal = anchor.rot * Vector3.up;
                float textureCoordinate = -anchor.t / DirectionTexturePeriod;
                for (int column = 0; column < 4; column++)
                {
                    float transverse = column == 0 ? -halfWidth : column == 3 ? halfWidth : section[column - 1].x;
                    // The central strip shares the rail top's edges and triangle diagonal at every native anchor.
                    positions.Add(anchor.pos + anchor.rot * new Vector3(transverse, height, 0));
                    normals.Add(normal);
                    coordinates.Add(new Vector2((transverse + halfWidth) / (2 * halfWidth), textureCoordinate));
                    colors.Add(AirBeltMesh.DirectionColor);
                }
            }

            private void Quad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth,
                Vector3 normal, SupportSurface surface)
            {
                int start = positions.Count;
                positions.Add(first);
                positions.Add(second);
                positions.Add(third);
                positions.Add(fourth);
                for (int corner = 0; corner < 4; corner++)
                {
                    normals.Add(normal);
                    coordinates.Add(new Vector2(((int)surface + 0.5f) / 5, 0.5f));
                    colors.Add(new Color32(255, 255, 255, 255));
                }

                AddQuadIndices(triangles, start, start + 1, start + 2, start + 3, normal);
            }

            private void AddQuadIndices(List<int> indices, int first, int second, int third, int fourth,
                Vector3 normal)
            {
                bool reverse = Vector3.Dot(Vector3.Cross(positions[second] - positions[first],
                    positions[third] - positions[first]), normal) < 0;
                indices.Add(first);
                indices.Add(reverse ? third : second);
                indices.Add(reverse ? second : third);
                indices.Add(first);
                indices.Add(reverse ? fourth : third);
                indices.Add(reverse ? third : fourth);
            }

            internal Mesh BuildJunctions() => triangles.Count == 0 ? null :
                OrbitalMeshBuffers.Build("AirBelt.Power.Junctions", positions, normals, coordinates,
                    new[] { triangles }, colors: colors);

            internal Mesh BuildDirections(out int[] speeds)
            {
                speeds = new int[directionTriangles.Count];
                if (speeds.Length == 0)
                {
                    return null;
                }

                directionTriangles.Keys.CopyTo(speeds, 0);
                System.Array.Sort(speeds);
                var surfaces = new List<int>[speeds.Length];
                for (int index = 0; index < speeds.Length; index++)
                {
                    surfaces[index] = directionTriangles[speeds[index]];
                }
                return OrbitalMeshBuffers.Build("AirBelt.Power.Directions", positions, normals, coordinates,
                    surfaces, colors: colors);
            }
        }
    }
}
