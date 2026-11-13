using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal sealed class TrackModelTemplate
    {
        internal readonly Vector3[] Vertices;
        internal readonly Vector3[] Normals;
        internal readonly Vector2[] Coordinates;
        internal readonly int[][] Indices;
        internal readonly float ReferenceArcLength;
        internal readonly float ReferenceCoordinateSpan;
        internal readonly Vector2 LongitudinalBounds;

        internal TrackModelTemplate(Mesh mesh, float arcLength, float longitudeSpan)
        {
            Vertices = mesh.vertices;
            Normals = mesh.normals;
            Coordinates = mesh.uv;
            Indices = new int[mesh.subMeshCount][];
            for (int surface = 0; surface < Indices.Length; surface++)
            {
                Indices[surface] = mesh.GetTriangles(surface);
            }

            Bounds bounds = mesh.bounds;
            LongitudinalBounds = new Vector2(bounds.min.z, bounds.max.z);
            ReferenceArcLength = arcLength;
            ReferenceCoordinateSpan = longitudeSpan / (Mathf.PI * 2) * OrbitalTextureLayout.CoordinatePeriod;
        }
    }

    internal static class TrackModelAssets
    {
        private const string ResourceName = "ProjectOrbitalRing.assets.models.orbital-track-models";
        private static AssetBundle bundle;
        private static readonly Dictionary<string, UnityEngine.Object> sharedAssets =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);

        private static T SharedAsset<T>(string name) where T : UnityEngine.Object
        {
            if (!sharedAssets.TryGetValue(name, out var asset))
            {
                asset = OpenBundle().LoadAsset<T>(name);
                if (asset == null)
                {
                    throw new InvalidDataException("The orbital resource is unavailable: " + name);
                }
                sharedAssets.Add(name, asset);
            }
            return (T)asset;
        }

        internal static Material CreateMaterial(string name)
        {
            Material template = SharedAsset<Material>(name);
            Shader shader = template.shader.name == "Standard" ? Shader.Find("Standard") : template.shader;
            if (shader == null || !shader.isSupported)
            {
                throw new InvalidDataException("The orbital material shader is unavailable: " + name);
            }
            var material = new Material(template) { shader = shader };
            return material;
        }

        internal static Mesh Decoration(string name) => SharedAsset<Mesh>(name);

        internal static Cubemap SteelEnvironment => SharedAsset<Cubemap>("orbital-steel-environment");

        internal static Shader AirDirectionShader
        {
            get
            {
                Shader shader = SharedAsset<Shader>("air-rail-direction-shader");
                if (!shader.isSupported)
                {
                    throw new InvalidDataException("The air-rail direction shader is unavailable.");
                }
                return shader;
            }
        }

        private static readonly Dictionary<int, TrackModelTemplate> models = new Dictionary<int, TrackModelTemplate>();

        private sealed class Manifest
        {
            public Segment[] Segments { get; set; }
        }

        private sealed class Segment
        {
            public int ItemId { get; set; }
            public int Lod { get; set; }
            public float ReferenceArcLength { get; set; }
            public float ReferenceLongitudeSpanRadians { get; set; }
        }

        internal static TrackModelTemplate Get(int itemId, int detailLevel)
        {
            if (models.Count == 0)
            {
                Initialize();
            }

            return models[itemId * 3 + detailLevel];
        }

        internal static Texture2D GetTexture(string name)
        {
            Texture2D texture = SharedAsset<Texture2D>(name);
            if (name == "orbital-final-6516-acceleratorliner-workingindicators" &&
                texture.format != TextureFormat.RGBAHalf)
            {
                throw new InvalidDataException("Accelerator working indicators require an HDR texture.");
            }
            return texture;
        }

        internal static void Initialize()
        {
            if (models.Count > 0)
            {
                return;
            }

            AssetBundle assets = OpenBundle();
            var importedMeshes = new List<Mesh>();
            TextAsset metadata = null;
            try
            {
                metadata = assets.LoadAsset<TextAsset>("segments");
                var manifest = metadata == null ? null : JsonConvert.DeserializeObject<Manifest>(metadata.text);
                if (manifest?.Segments == null || manifest.Segments.Length != 9)
                {
                    throw new InvalidDataException("The orbital track model manifest requires nine detail meshes.");
                }

                foreach (Segment segment in manifest.Segments)
                {
                    string name = "track-" + segment.ItemId + "-lod" + segment.Lod;
                    Mesh mesh = assets.LoadAsset<Mesh>(name);
                    if (mesh != null)
                    {
                        importedMeshes.Add(mesh);
                    }
                    if (mesh == null || segment.ReferenceArcLength <= 0 || segment.ReferenceLongitudeSpanRadians <= 0)
                    {
                        throw new InvalidDataException("Invalid orbital model resource: " + name);
                    }

                    var template = new TrackModelTemplate(
                        mesh, segment.ReferenceArcLength, segment.ReferenceLongitudeSpanRadians);
                    models.Add(segment.ItemId * 3 + segment.Lod, template);
                }

            }
            catch
            {
                models.Clear();
                throw;
            }
            finally
            {
                // Templates retain managed vertex channels; imported Unity meshes are transient.
                foreach (Mesh mesh in importedMeshes)
                {
                    UnityEngine.Object.Destroy(mesh);
                }

                UnityEngine.Object.Destroy(metadata);
            }
        }

        private static AssetBundle OpenBundle()
        {
            if (bundle != null)
            {
                return bundle;
            }

            byte[] data;
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            {
                if (resource == null)
                {
                    throw new InvalidDataException("The orbital track model resource is absent.");
                }

                using (var buffer = new MemoryStream())
                {
                    resource.CopyTo(buffer);
                    data = buffer.ToArray();
                }
            }

            bundle = AssetBundle.LoadFromMemory(data);
            if (bundle == null)
            {
                throw new InvalidDataException("The orbital track model bundle could not be loaded.");
            }

            return bundle;
        }

        internal static void Dispose()
        {
            models.Clear();
            sharedAssets.Clear();
            if (bundle != null)
            {
                bundle.Unload(true);
                bundle = null;
            }
        }
    }
}
