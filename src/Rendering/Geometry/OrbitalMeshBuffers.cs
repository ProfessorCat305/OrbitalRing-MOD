using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalMeshBuffers
    {
        internal static Mesh Build(string name, List<Vector3> positions, List<Vector3> normals,
            List<Vector2> coordinates, IReadOnlyList<List<int>> surfaces,
            List<Color> colors = null, bool tangentSpace = false, List<Vector4> tangents = null,
            bool whiteVertices = false)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            try
            {
                mesh.SetVertices(positions);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, coordinates);
                if (colors != null)
                {
                    mesh.SetColors(colors);
                }
                else if (whiteVertices)
                {
                    var white = new Color32[positions.Count];
                    for (int index = 0; index < white.Length; index++)
                    {
                        white[index] = new Color32(255, 255, 255, 255);
                    }
                    mesh.colors32 = white;
                }

                mesh.subMeshCount = surfaces.Count;
                for (int surface = 0; surface < surfaces.Count; surface++)
                {
                    mesh.SetTriangles(surfaces[surface], surface, false);
                }
                mesh.RecalculateBounds();
                if (tangents != null)
                {
                    mesh.SetTangents(tangents);
                }
                else if (tangentSpace)
                {
                    mesh.RecalculateTangents();
                }
                return mesh;
            }
            catch
            {
                Object.Destroy(mesh);
                throw;
            }
        }

        internal static Mesh Combine(string name, IEnumerable<Mesh> meshes) =>
            Combine(name, meshes.Select(mesh => new CombineInstance { mesh = mesh }).ToArray(), useMatrices: false);

        internal static Mesh Combine(string name, CombineInstance[] parts, bool mergeSurfaces = true,
            bool useMatrices = true)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            try
            {
                mesh.CombineMeshes(parts, mergeSurfaces, useMatrices);
                return mesh;
            }
            catch
            {
                Object.Destroy(mesh);
                throw;
            }
        }
    }
}
