using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal readonly struct TrackSample
    {
        internal readonly Vector3 Position;
        internal readonly Vector3 Tangent;
        internal readonly Vector3 Up;

        internal TrackSample(Vector3 position, Vector3 tangent, Vector3 up)
        {
            Position = position;
            Tangent = tangent;
            Up = up;
        }
    }

    internal sealed class TrackMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> coordinates = new List<Vector2>();
        private readonly List<Vector3> detailCoordinates = new List<Vector3>();
        internal const int ShadowSurface = 5;
        private readonly List<List<int>> indices = new List<List<int>>(ShadowSurface + 1)
        {
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<int>(),
        };
        private readonly float radius;
        private readonly Vector3 origin;
        private readonly int detailLevel;
        internal int VertexCount => vertices.Count;

        private readonly struct TubeRow
        {
            internal readonly Vector3 Position;
            internal readonly Vector3 Tangent;
            internal readonly Vector3 Side;
            internal readonly Vector3 Radial;
            internal readonly float Coordinate;

            internal TubeRow(TrackSample sample, float coordinate)
            {
                Position = sample.Position;
                Tangent = sample.Tangent.normalized;
                Side = Vector3.Cross(sample.Up, Tangent).normalized;
                Radial = Vector3.Cross(Tangent, Side).normalized;
                Coordinate = coordinate;
            }
        }

        internal TrackMesh(float radius = 0.32f, Vector3 origin = default(Vector3), int detailLevel = 0)
        {
            this.radius = radius;
            this.origin = origin;
            this.detailLevel = detailLevel;
        }

        internal int AddVertex(Vector3 position, Vector3 normal, Vector2 coordinate,
            Vector3 detailCoordinate = default(Vector3))
        {
            int index = vertices.Count;
            vertices.Add(position);
            normals.Add(normal);
            coordinates.Add(coordinate);
            detailCoordinates.Add(detailCoordinate);
            return index;
        }

        internal void Triangle(int surface, int first, int second, int third)
        {
            if (Vector3.Cross(vertices[second] - vertices[first], vertices[third] - vertices[first]).sqrMagnitude == 0)
            {
                return;
            }

            indices[surface].Add(first);
            indices[surface].Add(second);
            indices[surface].Add(third);
        }

        internal void Path(IList<TrackSample> samples, float[] phases, float textureScale,
            bool capStart = false, bool capEnd = false)
        {
            if (samples.Count < 2)
            {
                return;
            }

            foreach (float phase in phases)
            {
                if (float.IsNaN(phase) || float.IsInfinity(phase))
                {
                    throw new InvalidOperationException("Track texture coordinates must be finite.");
                }
            }

            var retained = SimplifyRows(samples);
            var rows = new TubeRow[retained.Count];
            for (int row = 0; row < rows.Length; row++)
            {
                int index = retained[row];
                rows[row] = new TubeRow(samples[index], phases[index] * textureScale);
            }
            int sides = detailLevel == 0 ? 32 : detailLevel == 1 ? 24 : 16;
            for (int layer = 0; layer < 2; layer++)
            {
                int first = vertices.Count;
                float layerRadius = radius * (layer == 0 ? 1 : 0.92f);
                for (int row = 0; row < rows.Length; row++)
                {
                    var frame = rows[row];
                    for (int column = 0; column <= sides; column++)
                    {
                        float angle = column * Mathf.PI * 2 / sides;
                        Vector3 normal = frame.Side * Mathf.Cos(angle) + frame.Radial * Mathf.Sin(angle);
                        AddVertex(frame.Position + normal * layerRadius, layer == 0 ? normal : -normal,
                            new Vector2((float)column / sides, frame.Coordinate));
                    }
                }

                int surface = layer == 0 ? 0 : 2;
                for (int row = 1; row < rows.Length; row++)
                {
                    for (int column = 0; column < sides; column++)
                    {
                        int upper = first + row * (sides + 1) + column;
                        int lower = upper - sides - 1;
                        if (layer == 0)
                        {
                            Triangle(surface, lower, lower + 1, upper);
                            Triangle(surface, lower + 1, upper + 1, upper);
                        }
                        else
                        {
                            Triangle(surface, lower, upper, lower + 1);
                            Triangle(surface, lower + 1, upper, upper + 1);
                        }
                    }
                }

                if (capStart)
                {
                    Cap(rows[0].Position, -rows[0].Tangent, first, sides, surface, layer != 0);
                }

                if (capEnd)
                {
                    int last = rows.Length - 1;
                    int lastRing = first + last * (sides + 1);
                    Cap(rows[last].Position, rows[last].Tangent, lastRing, sides, surface, layer != 0);
                }
            }
        }

        private List<int> SimplifyRows(IList<TrackSample> samples)
        {
            // At the 24/6-pixel LOD transitions, the 1.65 m accelerator envelope stays within 0.2 pixels.
            float tolerance = detailLevel == 0 ? 0.002f : detailLevel == 1 ? 0.012f : 0.05f;
            var outline = new List<Vector3>(samples.Count);
            foreach (TrackSample sample in samples)
            {
                Vector3 world = sample.Position + origin;
                Vector3 outward = new Vector3(world.x, 0, world.z).normalized;
                // Fixed-latitude ring samples share an angular parameter; the outer contour bounds the tube.
                outline.Add(sample.Position + outward * radius);
            }
            var retained = new List<int>();
            LineUtility.Simplify(outline, tolerance * 0.99f, retained);
            return retained;
        }

        private void Cap(Vector3 center, Vector3 normal, int ring, int sides, int surface, bool reverse)
        {
            int middle = AddVertex(center, reverse ? -normal : normal, new Vector2(0.5f, 0.5f));
            for (int column = 0; column < sides; column++)
            {
                int first = AddVertex(vertices[ring + column], reverse ? -normal : normal, coordinates[ring + column]);
                int second = AddVertex(vertices[ring + column + 1], reverse ? -normal : normal,
                    coordinates[ring + column + 1]);
                if (Vector3.Dot(Vector3.Cross(vertices[first] - center, vertices[second] - center),
                    reverse ? -normal : normal) >= 0)
                {
                    Triangle(surface, middle, first, second);
                }
                else
                {
                    Triangle(surface, middle, second, first);
                }
            }
        }

        internal void AddShadowSurface(bool opaqueCore)
        {
            // Distant shadows include the opaque fittings and detail, plus the core on non-accelerator tracks.
            var shadow = new List<int>((opaqueCore ? indices[0].Count : 0) + indices[1].Count + indices[4].Count);
            if (opaqueCore)
            {
                shadow.AddRange(indices[0]);
            }
            shadow.AddRange(indices[1]);
            shadow.AddRange(indices[4]);
            indices.Add(shadow);
        }

        internal Mesh Build(string name)
        {
            return OrbitalMeshBuffers.Build(name, vertices, normals, coordinates, indices,
                detailCoordinates, tangentSpace: true, whiteVertices: true);
        }
    }
}
