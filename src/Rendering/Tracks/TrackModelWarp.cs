using System;
using System.Collections.Generic;
using ProjectOrbitalRing.Utils;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class TrackModelWarp
    {
        private const int TrianglesPerBuildStep = 256;

        private struct ModelVertex
        {
            internal Vector3 Position;
            internal Vector3 Normal;
            internal Vector2 Coordinate;
            internal Vector2 DetailCoordinate;

            internal static ModelVertex Lerp(ModelVertex first, ModelVertex second, float fraction) => new ModelVertex
            {
                Position = Vector3.LerpUnclamped(first.Position, second.Position, fraction),
                Normal = Vector3.LerpUnclamped(first.Normal, second.Normal, fraction),
                Coordinate = Vector2.LerpUnclamped(first.Coordinate, second.Coordinate, fraction),
                DetailCoordinate = Vector2.LerpUnclamped(first.DetailCoordinate, second.DetailCoordinate, fraction),
            };
        }

        private readonly struct WarpFrame
        {
            internal readonly Vector3 Center;
            internal readonly Vector3 Tangent;
            internal readonly Vector3 Up;
            internal readonly Vector3 Side;
            internal readonly float InverseLongitudinalScale;

            internal WarpFrame(Vector3 center, Vector3 tangent, Vector3 up, Vector3 side,
                float inverseLongitudinalScale)
            {
                Center = center;
                Tangent = tangent;
                Up = up;
                Side = side;
                InverseLongitudinalScale = inverseLongitudinalScale;
            }
        }

        private sealed class Path
        {
            private readonly IList<TrackSample> samples;
            private readonly bool reversed;
            private readonly float textureScale;
            internal readonly float[] Phases;
            private readonly Dictionary<float, WarpFrame> frames = new Dictionary<float, WarpFrame>();

            internal Path(IList<TrackSample> samples, float[] phases, float textureScale)
            {
                this.samples = samples;
                this.textureScale = textureScale;
                Phases = phases;
                reversed = phases[phases.Length - 1] < phases[0];
                if (reversed)
                {
                    Array.Reverse(phases);
                }
            }

            internal void BeginCycle()
            {
                // The same model Z maps to a different path frame in each geographic period.
                frames.Clear();
            }

            internal int Add(TrackMesh target, ModelVertex vertex, float cycle, float referenceLength,
                float referenceCoordinateSpan, bool localCoordinates)
            {
                if (!frames.TryGetValue(vertex.Position.z, out var frame))
                {
                    frame = CreateFrame(vertex.Position.z, cycle, referenceLength);
                    frames.Add(vertex.Position.z, frame);
                }

                Vector3 crossSectionNormal = frame.Side * vertex.Normal.x + frame.Up * vertex.Normal.y;
                Vector3 longitudinalNormal = frame.Tangent * (vertex.Normal.z * frame.InverseLongitudinalScale);
                Vector3 normal = (crossSectionNormal + longitudinalNormal).normalized;
                Vector2 coordinate = vertex.Coordinate;
                // Continuous UVs follow the actual facility spacing; frame windows stay local.
                if (!localCoordinates)
                {
                    float facilityCoordinate = coordinate.y / referenceCoordinateSpan + cycle;
                    coordinate.y = facilityCoordinate * textureScale;
                }

                Vector3 position = frame.Center + frame.Side * vertex.Position.x + frame.Up * vertex.Position.y;
                return target.AddVertex(position, normal, coordinate,
                    new Vector3(vertex.DetailCoordinate.x, vertex.DetailCoordinate.y, frame.InverseLongitudinalScale));
            }

            private WarpFrame CreateFrame(float modelLength, float cycle, float referenceLength)
            {
                float phase = cycle + modelLength / referenceLength;
                int lowerIndex = 0;
                int upperIndex = Phases.Length - 1;
                while (upperIndex - lowerIndex > 1)
                {
                    int middleIndex = (lowerIndex + upperIndex) / 2;
                    if (Phases[middleIndex] <= phase)
                    {
                        lowerIndex = middleIndex;
                    }
                    else
                    {
                        upperIndex = middleIndex;
                    }
                }

                float interval = Phases[upperIndex] - Phases[lowerIndex];
                float fraction = interval > 0.0000001f ? Mathf.Clamp01((phase - Phases[lowerIndex]) / interval) : 0;
                TrackSample lower = samples[reversed ? samples.Count - 1 - lowerIndex : lowerIndex];
                TrackSample upper = samples[reversed ? samples.Count - 1 - upperIndex : upperIndex];
                Vector3 center = Vector3.Lerp(lower.Position, upper.Position, fraction);
                Vector3 tangent = Vector3.Lerp(lower.Tangent, upper.Tangent, fraction).normalized;
                if (tangent.sqrMagnitude < 0.5f)
                {
                    tangent = (upper.Position - lower.Position).normalized;
                }
                else if (reversed)
                {
                    tangent = -tangent;
                }

                Vector3 up = Vector3.Lerp(lower.Up, upper.Up, fraction).normalized;
                Vector3 side = Vector3.Cross(up, tangent).normalized;
                up = Vector3.Cross(tangent, side).normalized;
                float lengthScale = 1;
                if (interval > 0.0000001f)
                {
                    float segmentLength = Vector3.Distance(lower.Position, upper.Position);
                    lengthScale = segmentLength / (interval * referenceLength);
                }
                return new WarpFrame(center, tangent, up, side, 1f / Mathf.Max(0.0001f, lengthScale));
            }
        }

        internal static IEnumerable<object> Append(TrackMesh target, int itemId, int detailLevel,
            IList<TrackSample> samples, float[] phases, float textureScale)
        {
            var path = new Path(samples, phases, textureScale);
            if (path.Phases[path.Phases.Length - 1] - path.Phases[0] < 0.0000001f)
            {
                yield break;
            }

            var model = TrackModelAssets.Get(itemId, detailLevel);
            var mapping = new int[model.Vertices.Length];
            var polygon = new ModelVertex[6];
            var clipped = new ModelVertex[6];
            int firstCycle = Mathf.FloorToInt(path.Phases[0]);
            int lastCycle = Mathf.CeilToInt(path.Phases[path.Phases.Length - 1]) - 1;
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                path.BeginCycle();
                float minimumLength = (path.Phases[0] - cycle) * model.ReferenceArcLength;
                float maximumLength = (path.Phases[path.Phases.Length - 1] - cycle) * model.ReferenceArcLength;
                bool clipCycle = minimumLength > model.LongitudinalBounds.x ||
                    maximumLength < model.LongitudinalBounds.y;
                for (int surface = 0; surface < model.Indices.Length; surface++)
                {
                    var triangles = model.Indices[surface];
                    if (triangles.Length == 0)
                    {
                        continue;
                    }

                    Array.Clear(mapping, 0, mapping.Length);
                    bool localCoordinates = itemId == ProtoID.I粒子加速轨道 && (surface == 1 || surface == 4);
                    for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                    {
                        if (triangle > 0 && triangle % (TrianglesPerBuildStep * 3) == 0)
                        {
                            yield return null;
                        }

                        int first = triangles[triangle];
                        int second = triangles[triangle + 1];
                        int third = triangles[triangle + 2];
                        if (clipCycle)
                        {
                            float firstLength = model.Vertices[first].z;
                            float secondLength = model.Vertices[second].z;
                            float thirdLength = model.Vertices[third].z;
                            float lower = Mathf.Min(firstLength, Mathf.Min(secondLength, thirdLength));
                            float upper = Mathf.Max(firstLength, Mathf.Max(secondLength, thirdLength));
                            if (upper < minimumLength || lower > maximumLength)
                            {
                                continue;
                            }
                            if (lower < minimumLength || upper > maximumLength)
                            {
                                AppendClipped(first, second, third);
                                continue;
                            }
                        }

                        target.Triangle(surface, Map(first), Map(second), Map(third));
                    }

                    yield return null;

                    void AppendClipped(int first, int second, int third)
                    {
                        polygon[0] = Read(first);
                        polygon[1] = Read(second);
                        polygon[2] = Read(third);
                        int count = Clip(polygon, 3, clipped, minimumLength, true);
                        count = Clip(clipped, count, polygon, maximumLength, false);
                        if (count < 3)
                        {
                            return;
                        }

                        int anchor = Add(polygon[0]);
                        int previous = Add(polygon[1]);
                        for (int corner = 2; corner < count; corner++)
                        {
                            int next = Add(polygon[corner]);
                            // A boundary intersection can repeat a corner at floating-point precision.
                            Vector3 firstEdge = polygon[corner - 1].Position - polygon[0].Position;
                            Vector3 secondEdge = polygon[corner].Position - polygon[0].Position;
                            if (Vector3.Cross(firstEdge, secondEdge).sqrMagnitude > 1e-16f)
                            {
                                target.Triangle(surface, anchor, previous, next);
                            }
                            previous = next;
                        }
                    }

                    int Add(ModelVertex vertex) => path.Add(target, vertex, cycle,
                        model.ReferenceArcLength, model.ReferenceCoordinateSpan, localCoordinates);

                    ModelVertex Read(int index) => new ModelVertex
                    {
                        Position = model.Vertices[index],
                        Normal = model.Normals[index],
                        Coordinate = model.Coordinates[index],
                        DetailCoordinate = model.DetailCoordinates[index],
                    };
                    int Map(int index)
                    {
                        if (mapping[index] == 0)
                        {
                            mapping[index] = Add(Read(index)) + 1;
                        }

                        return mapping[index] - 1;
                    }
                }
            }
        }

        private static int Clip(ModelVertex[] input, int count, ModelVertex[] output, float limit, bool keepGreater)
        {
            if (count == 0)
            {
                return 0;
            }

            int outputCount = 0;
            var previous = input[count - 1];
            bool previousInside = keepGreater ? previous.Position.z >= limit : previous.Position.z <= limit;
            for (int index = 0; index < count; index++)
            {
                var current = input[index];
                bool currentInside = keepGreater ? current.Position.z >= limit : current.Position.z <= limit;
                if (previousInside != currentInside)
                {
                    float fraction = (limit - previous.Position.z) / (current.Position.z - previous.Position.z);
                    var intersection = ModelVertex.Lerp(previous, current, fraction);
                    intersection.Position.z = limit;
                    output[outputCount++] = intersection;
                }

                if (currentInside)
                {
                    output[outputCount++] = current;
                }

                previous = current;
                previousInside = currentInside;
            }

            return outputCount;
        }
    }
}
