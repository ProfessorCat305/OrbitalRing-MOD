using System.Collections.Generic;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalTrackIntersection
    {
        internal sealed class TowerSurface
        {
            private readonly Quaternion inverse;
            private readonly Vector3 towerUp;
            private readonly Vector3[] vertices;
            private readonly int[] triangles;

            internal TowerSurface(EntityData entity)
            {
                inverse = Quaternion.Inverse(entity.rot);
                towerUp = entity.pos.normalized;
                Mesh mesh = LDB.models.Select(entity.modelIndex)?.prefabDesc?.lodMeshes?[0];
                if (mesh != null && mesh.isReadable)
                {
                    vertices = mesh.vertices;
                    triangles = mesh.GetTriangles(0);
                }
            }

            internal float Radius(float height, Vector3 outward, Vector3 lateralOffset = default(Vector3))
            {
                if (vertices == null || triangles == null)
                {
                    return 3.65f;
                }

                Vector3 origin = inverse * (towerUp * height + lateralOffset);
                Vector3 direction = (inverse * outward).normalized;
                float radius = 0;
                for (int index = 0; index < triangles.Length; index += 3)
                {
                    Vector3 first = vertices[triangles[index]];
                    Vector3 firstEdge = vertices[triangles[index + 1]] - first;
                    Vector3 secondEdge = vertices[triangles[index + 2]] - first;
                    Vector3 cross = Vector3.Cross(direction, secondEdge);
                    float determinant = Vector3.Dot(firstEdge, cross);
                    if (Mathf.Abs(determinant) < 0.000001f)
                    {
                        continue;
                    }

                    float reciprocal = 1 / determinant;
                    Vector3 distance = origin - first;
                    float firstWeight = Vector3.Dot(distance, cross) * reciprocal;
                    if (firstWeight < 0 || firstWeight > 1)
                    {
                        continue;
                    }

                    Vector3 perpendicular = Vector3.Cross(distance, firstEdge);
                    float secondWeight = Vector3.Dot(direction, perpendicular) * reciprocal;
                    if (secondWeight < 0 || firstWeight + secondWeight > 1)
                    {
                        continue;
                    }

                    float hit = Vector3.Dot(secondEdge, perpendicular) * reciprocal;
                    if (hit > radius && hit <= 6)
                    {
                        radius = hit;
                    }
                }

                return radius > 0 ? radius : 3.65f;
            }
        }

        internal static bool TryMeasure(EntityData entity, Vector3 center,
            Vector3 along, Vector3 across, Vector3 up, float envelopeRadius, out Vector2 interval)
        {
            interval = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
            Mesh mesh = LDB.models.Select(entity.modelIndex)?.prefabDesc?.lodMeshes?[0];
            if (mesh == null || !mesh.isReadable)
            {
                return false;
            }

            Vector3[] positions = mesh.vertices;
            for (int index = 0; index < positions.Length; index++)
            {
                Vector3 delta = entity.pos + entity.rot * positions[index] - center;
                positions[index] = new Vector3(Vector3.Dot(delta, across),
                    Vector3.Dot(delta, up), Vector3.Dot(delta, along));
            }

            const int planes = 16;
            var polygon = new List<Vector3>(20);
            var clipped = new List<Vector3>(20);
            int[] indices = mesh.triangles;
            for (int triangle = 0; triangle < indices.Length; triangle += 3)
            {
                Vector3 first = positions[indices[triangle]];
                Vector3 second = positions[indices[triangle + 1]];
                Vector3 third = positions[indices[triangle + 2]];
                float minimumX = Mathf.Min(first.x, Mathf.Min(second.x, third.x));
                float maximumX = Mathf.Max(first.x, Mathf.Max(second.x, third.x));
                float minimumY = Mathf.Min(first.y, Mathf.Min(second.y, third.y));
                float maximumY = Mathf.Max(first.y, Mathf.Max(second.y, third.y));
                if (minimumX > envelopeRadius || maximumX < -envelopeRadius ||
                    minimumY > envelopeRadius || maximumY < -envelopeRadius)
                {
                    continue;
                }

                polygon.Clear();
                polygon.Add(first);
                polygon.Add(second);
                polygon.Add(third);
                for (int plane = 0; plane < planes && polygon.Count > 0; plane++)
                {
                    float angle = plane * Mathf.PI * 2 / planes;
                    Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    clipped.Clear();
                    Vector3 previous = polygon[polygon.Count - 1];
                    float previousDistance = envelopeRadius - normal.x * previous.x - normal.y * previous.y;
                    foreach (Vector3 current in polygon)
                    {
                        float distance = envelopeRadius - normal.x * current.x - normal.y * current.y;
                        if ((distance >= 0) != (previousDistance >= 0))
                        {
                            clipped.Add(Vector3.LerpUnclamped(previous, current,
                                previousDistance / (previousDistance - distance)));
                        }

                        if (distance >= 0)
                        {
                            clipped.Add(current);
                        }

                        previous = current;
                        previousDistance = distance;
                    }

                    List<Vector3> temporary = polygon;
                    polygon = clipped;
                    clipped = temporary;
                }

                foreach (Vector3 point in polygon)
                {
                    interval.x = Mathf.Min(interval.x, point.z);
                    interval.y = Mathf.Max(interval.y, point.z);
                }
            }

            return interval.x <= interval.y;
        }
    }
}
