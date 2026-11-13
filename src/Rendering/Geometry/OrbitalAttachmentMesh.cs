using System.Collections.Generic;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal sealed class OrbitalAttachmentMesh
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> coordinates = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<CombineInstance> parts = new List<CombineInstance>();
        private int partTriangles;
        private readonly int detailLevel;

        internal OrbitalAttachmentMesh(int detailLevel = 0)
        {
            this.detailLevel = detailLevel;
        }

        internal int TriangleCount => triangles.Count / 3 + partTriangles;
        internal bool IsDistant => detailLevel > 0;

        internal void AddPart(string name, Vector3 center, Vector3 side, Vector3 up, Vector3 forward)
        {
            var transform = new Matrix4x4(side, up, forward, new Vector4(center.x, center.y, center.z, 1));
            Mesh mesh = TrackModelAssets.Decoration(name);
            parts.Add(new CombineInstance { mesh = mesh, transform = transform });
            partTriangles += (int)mesh.GetIndexCount(0) / 3;
        }

        internal void Box(Vector3 center, Vector3 side, Vector3 up, Vector3 forward, Vector3 size)
        {
            Vector3[] axes =
            {
                side * size.x * 0.5f,
                up * size.y * 0.5f,
                forward * size.z * 0.5f
            };
            for (int face = 0; face < 3; face++)
            {
                Vector3 first = axes[(face + 1) % 3];
                Vector3 second = axes[(face + 2) % 3];
                foreach (float sign in new[] { -1f, 1f })
                {
                    Vector3 normal = axes[face].normalized * sign;
                    Vector3 offset = center + axes[face] * sign;
                    Quad(offset - first - second, offset + first - second,
                        offset + first + second, offset - first + second, normal);
                }
            }
        }

        internal void BeveledBox(Vector3 center, Vector3 side, Vector3 up, Vector3 forward, Vector3 size, float bevel)
        {
            if (IsDistant)
            {
                Box(center, side, up, forward, size);
                return;
            }

            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            bevel = Mathf.Min(bevel, Mathf.Min(halfWidth, halfHeight));
            Vector2[] section =
            {
                new Vector2(-halfWidth + bevel, -halfHeight),
                new Vector2(halfWidth - bevel, -halfHeight),
                new Vector2(halfWidth, -halfHeight + bevel),
                new Vector2(halfWidth, halfHeight - bevel),
                new Vector2(halfWidth - bevel, halfHeight),
                new Vector2(-halfWidth + bevel, halfHeight),
                new Vector2(-halfWidth, halfHeight - bevel),
                new Vector2(-halfWidth, -halfHeight + bevel),
            };
            Vector3 depth = forward * size.z * 0.5f;
            for (int index = 0; index < section.Length; index++)
            {
                Vector2 first = section[index];
                Vector2 next = section[(index + 1) % section.Length];
                Vector3 firstPoint = center + side * first.x + up * first.y;
                Vector3 nextPoint = center + side * next.x + up * next.y;
                Vector2 edge = next - first;
                Vector3 normal = (side * edge.y - up * edge.x).normalized;
                Quad(firstPoint - depth, nextPoint - depth, nextPoint + depth, firstPoint + depth, normal);
                Triangle(center - depth, firstPoint - depth, nextPoint - depth, -forward);
                Triangle(center + depth, firstPoint + depth, nextPoint + depth, forward);
            }
        }

        internal void AddAxialPart(string name, Vector3 center, Vector3 axis, Vector3 up, float lengthScale = 1)
        {
            Vector3 side = Vector3.Cross(up, axis).normalized;
            up = Vector3.Cross(axis, side).normalized;
            AddPart(name + "-lod" + detailLevel, center, side, up, axis * lengthScale);
        }

        // Rod templates have unit length along their local Z axis.
        internal void AddRod(string name, Vector3 first, Vector3 last)
        {
            Vector3 axis = (last - first).normalized;
            Vector3 up = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            AddAxialPart(name, (first + last) * 0.5f, axis, up, Vector3.Distance(first, last));
        }

        internal void BeveledBeam(Vector3 first, Vector3 last, Vector3 normal, float width, float height, float bevel)
        {
            Vector3 forward = (last - first).normalized;
            Vector3 side = Vector3.Cross(normal, forward).normalized;
            Vector3 up = Vector3.Cross(forward, side).normalized;
            BeveledBox((first + last) * 0.5f, side, up, forward,
                new Vector3(width, height, Vector3.Distance(first, last)), bevel);
        }

        internal void TubeLining(Vector3 center, Vector3 axis, Vector3 up, float radius, float length)
        {
            Vector3 side = Vector3.Cross(up, axis).normalized;
            up = Vector3.Cross(axis, side).normalized;
            Vector3 depth = axis * length * 0.5f;
            int segments = IsDistant ? 16 : 48;
            for (int index = 0; index < segments; index++)
            {
                float firstAngle = index * Mathf.PI * 2 / segments;
                float nextAngle = (index + 1) * Mathf.PI * 2 / segments;
                Vector3 first = side * Mathf.Cos(firstAngle) + up * Mathf.Sin(firstAngle);
                Vector3 next = side * Mathf.Cos(nextAngle) + up * Mathf.Sin(nextAngle);
                Quad(center + first * radius - depth, center + next * radius - depth,
                    center + next * radius + depth, center + first * radius + depth,
                    -first, -next, -next, -first);
            }
        }

        internal void Sweep(IReadOnlyList<Vector3> path, Vector3 referenceUp, float radius)
        {
            int segments = IsDistant ? 6 : 16;
            if (path.Count < 2)
            {
                return;
            }

            if (IsDistant && path.Count > 9)
            {
                var simplified = new Vector3[9];
                for (int index = 0; index < simplified.Length; index++)
                {
                    int source = Mathf.RoundToInt((float)index * (path.Count - 1) / (simplified.Length - 1));
                    simplified[index] = path[source];
                }
                path = simplified;
            }

            var tangents = new Vector3[path.Count];
            var sides = new Vector3[path.Count];
            var across = new Vector3[path.Count];
            for (int index = 0; index < path.Count; index++)
            {
                tangents[index] = (path[Mathf.Min(index + 1, path.Count - 1)]
                    - path[Mathf.Max(0, index - 1)]).normalized;
                Vector3 side = index == 0 ? Vector3.Cross(referenceUp, tangents[index]) :
                    Vector3.ProjectOnPlane(sides[index - 1], tangents[index]);
                if (side.sqrMagnitude < 0.000001f)
                {
                    side = Vector3.Cross(Mathf.Abs(tangents[index].y) < 0.9f ? Vector3.up : Vector3.right,
                        tangents[index]);
                }

                sides[index] = side.normalized;
                across[index] = Vector3.Cross(tangents[index], sides[index]).normalized;
            }

            for (int index = 0; index < path.Count - 1; index++)
            {
                for (int column = 0; column < segments; column++)
                {
                    float angle = column * Mathf.PI * 2 / segments;
                    float nextAngle = (column + 1) * Mathf.PI * 2 / segments;
                    Vector3 first = sides[index] * Mathf.Cos(angle) + across[index] * Mathf.Sin(angle);
                    Vector3 second = sides[index] * Mathf.Cos(nextAngle) + across[index] * Mathf.Sin(nextAngle);
                    Vector3 third = sides[index + 1] * Mathf.Cos(nextAngle) + across[index + 1] * Mathf.Sin(nextAngle);
                    Vector3 fourth = sides[index + 1] * Mathf.Cos(angle) + across[index + 1] * Mathf.Sin(angle);
                    Quad(path[index] + first * radius, path[index] + second * radius,
                        path[index + 1] + third * radius, path[index + 1] + fourth * radius,
                        first, second, third, fourth);
                }
            }

            foreach (int index in new[] { 0, path.Count - 1 })
            {
                Vector3 normal = tangents[index] * (index == 0 ? -1 : 1);
                for (int column = 0; column < segments; column++)
                {
                    float angle = column * Mathf.PI * 2 / segments;
                    float nextAngle = (column + 1) * Mathf.PI * 2 / segments;
                    Vector3 first = sides[index] * Mathf.Cos(angle) + across[index] * Mathf.Sin(angle);
                    Vector3 last = sides[index] * Mathf.Cos(nextAngle) + across[index] * Mathf.Sin(nextAngle);
                    Triangle(path[index], path[index] + first * radius, path[index] + last * radius, normal);
                }
            }
        }

        internal void ArcBand(Vector3 center, Vector3 axis, Vector3 forward, float innerRadius, float outerRadius,
            float height, float halfAngle, int segments = 16)
        {
            if (IsDistant)
            {
                segments = Mathf.Clamp(segments / 3, 6, 12);
            }

            Vector3 side = Vector3.Cross(axis, forward).normalized;
            forward = Vector3.Cross(side, axis).normalized;
            Vector3 lower = center - axis * (height * 0.5f);
            Vector3 upper = center + axis * (height * 0.5f);
            for (int index = 0; index < segments; index++)
            {
                float firstAngle = Mathf.Lerp(-halfAngle, halfAngle, (float)index / segments);
                float lastAngle = Mathf.Lerp(-halfAngle, halfAngle, (float)(index + 1) / segments);
                Vector3 first = forward * Mathf.Cos(firstAngle) + side * Mathf.Sin(firstAngle);
                Vector3 last = forward * Mathf.Cos(lastAngle) + side * Mathf.Sin(lastAngle);
                Quad(lower + first * outerRadius, lower + last * outerRadius,
                    upper + last * outerRadius, upper + first * outerRadius, first, last, last, first);
                Quad(lower + first * innerRadius, lower + last * innerRadius,
                    upper + last * innerRadius, upper + first * innerRadius, -first, -last, -last, -first);
                Quad(upper + first * innerRadius, upper + last * innerRadius,
                    upper + last * outerRadius, upper + first * outerRadius, axis);
                Quad(lower + first * innerRadius, lower + last * innerRadius,
                    lower + last * outerRadius, lower + first * outerRadius, -axis);
                if (index == 0)
                {
                    Vector3 normal = forward * Mathf.Sin(firstAngle) - side * Mathf.Cos(firstAngle);
                    Quad(lower + first * innerRadius, lower + first * outerRadius,
                        upper + first * outerRadius, upper + first * innerRadius, normal);
                }

                if (index == segments - 1)
                {
                    Vector3 normal = -forward * Mathf.Sin(lastAngle) + side * Mathf.Cos(lastAngle);
                    Quad(lower + last * innerRadius, lower + last * outerRadius,
                        upper + last * outerRadius, upper + last * innerRadius, normal);
                }
            }
        }

        private void Triangle(Vector3 first, Vector3 second, Vector3 third, Vector3 normal)
        {
            int start = positions.Count;
            positions.Add(first);
            positions.Add(second);
            positions.Add(third);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            coordinates.Add(Vector2.zero);
            coordinates.Add(Vector2.right);
            coordinates.Add(Vector2.up);
            Face(start, start + 1, start + 2, Vector3.Dot(Vector3.Cross(second - first, third - first), normal) < 0);
        }

        private void Quad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth, Vector3 normal) =>
            Quad(first, second, third, fourth, normal, normal, normal, normal);

        private void Quad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth,
            Vector3 firstNormal, Vector3 secondNormal, Vector3 thirdNormal, Vector3 fourthNormal)
        {
            int start = positions.Count;
            positions.Add(first);
            positions.Add(second);
            positions.Add(third);
            positions.Add(fourth);
            normals.Add(firstNormal);
            normals.Add(secondNormal);
            normals.Add(thirdNormal);
            normals.Add(fourthNormal);
            float width = Vector3.Distance(first, second) / 4;
            float height = Vector3.Distance(second, third) / 4;
            coordinates.Add(Vector2.zero);
            coordinates.Add(new Vector2(width, 0));
            coordinates.Add(new Vector2(width, height));
            coordinates.Add(new Vector2(0, height));
            bool reverse = Vector3.Dot(Vector3.Cross(second - first, third - first),
                firstNormal + secondNormal + thirdNormal) < 0;
            Face(start, start + 1, start + 2, reverse);
            Face(start, start + 2, start + 3, reverse);
        }

        private void Face(int first, int second, int third, bool reverse)
        {
            triangles.Add(first);
            triangles.Add(reverse ? third : second);
            triangles.Add(reverse ? second : third);
        }

        internal Mesh Build(bool tangentSpace = false)
        {
            if (positions.Count == 0 && parts.Count > 0)
            {
                return OrbitalMeshBuffers.Combine("OrbitalRing.Attachment", parts.ToArray());
            }

            Mesh generated = OrbitalMeshBuffers.Build("OrbitalRing.Attachment", positions, normals, coordinates,
                new[] { triangles }, tangentSpace: tangentSpace || parts.Count > 0);
            if (parts.Count == 0)
            {
                return generated;
            }

            try
            {
                var instances = new CombineInstance[parts.Count + 1];
                parts.CopyTo(instances);
                instances[parts.Count] = new CombineInstance { mesh = generated, transform = Matrix4x4.identity };
                return OrbitalMeshBuffers.Combine("OrbitalRing.Attachment", instances);
            }
            finally
            {
                Object.Destroy(generated);
            }
        }
    }
}
