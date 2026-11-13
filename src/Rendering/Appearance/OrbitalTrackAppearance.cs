using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalTrackAppearance
    {
        internal const float StructureLightIntensity = 1.6f;
        private static readonly Dictionary<int, Material[]> materialSets =
            new Dictionary<int, Material[]>();

        internal static Texture2D StandbyBeamTexture { get; private set; }
        internal static Texture2D WorkingBeamTexture { get; private set; }
        internal static Texture2D AcceleratorStandbyEmission { get; private set; }
        internal static Texture2D AcceleratorWorkingEmission { get; private set; }

        internal static Material[] GetMaterials(TrackDefinition definition)
        {
            if (materialSets.TryGetValue(definition.ItemId, out Material[] materials))
            {
                return materials;
            }

            materials = new Material[(int)TrackSurfaceRole.Detail + 1];
            string prefix = "track-" + definition.ItemId + "-";
            try
            {
                materials[(int)TrackSurfaceRole.Fittings] = TrackModelAssets.CreateMaterial(prefix + "Fittings");
                materials[(int)TrackSurfaceRole.Detail] = TrackModelAssets.CreateMaterial(prefix + "Detail");
                materials[(int)TrackSurfaceRole.Core] = definition.HasGlassWall ? CreateParticleBeam()
                    : TrackModelAssets.CreateMaterial(prefix + "Core");
                if (definition.HasGlassWall)
                {
                    Material wall = CreateGlassWall();
                    materials[(int)TrackSurfaceRole.Wall] = wall;
                    materials[(int)TrackSurfaceRole.InnerWall] = new Material(wall) { renderQueue = 2999 };
                    const string liner = "orbital-final-6516-acceleratorliner-";
                    AcceleratorStandbyEmission = TrackModelAssets.GetTexture(liner + "indicators");
                    AcceleratorWorkingEmission = TrackModelAssets.GetTexture(liner + "workingindicators");
                }
                materialSets.Add(definition.ItemId, materials);
                return materials;
            }
            catch
            {
                DestroyMaterials(materials);
                throw;
            }
        }

        private static Material CreateGlassWall()
        {
            Material body = null;
            try
            {
                Shader shader = Shader.Find("Standard");
                var glass = TankGlassMaterial();
                var glassShader = Shader.Find("VF Shaders/Forward/PBR Standard Glass")
                    ?? Shader.Find("Standard (Specular setup)") ?? shader;
                body = new Material(glassShader);
                body.name = "OrbitalRing.GlassWall";
                body.color = new Color(0.88f, 0.94f, 0.98f, 0.16f);
                body.SetTexture("_MainTex", Texture2D.whiteTexture);
                Transparent(body);
                Color specular = new Color(0.07f, 0.08f, 0.09f);
                body.SetColor("_SpecularColor", specular);
                body.SetColor("_SpecColor", specular);
                body.SetFloat("_Glossiness", 0.62f);
                body.SetFloat("_SmoothMultiplier", 0.72f);
                if (body.HasProperty("_RimColor"))
                {
                    var rimColor = glass?.GetColor("_RimColor") ?? new Color(0.604f, 0.846f, 0.859f);
                    rimColor.a = 0.3f;
                    body.SetColor("_RimColor", rimColor);
                }

                if (body.HasProperty("_MetallicMultiplier"))
                {
                    body.SetFloat("_MetallicMultiplier", 0);
                }

                if (body.HasProperty("_MS_Tex"))
                {
                    body.SetTexture("_MS_Tex", Texture2D.whiteTexture);
                }

                if (body.HasProperty("_Metallic"))
                {
                    body.SetFloat("_Metallic", 0.02f);
                }

                return body;
            }
            catch
            {
                UnityEngine.Object.Destroy(body);
                throw;
            }
        }

        private static Material TankGlassMaterial()
        {
            var prefab = LDB.items.Select(2106)?.prefabDesc?.prefab;
            if (prefab == null)
            {
                return null;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && material.shader.name.Contains("PBR Standard Tank"))
                    {
                        return material;
                    }
                }
            }

            return null;
        }

        private static void Transparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            material.renderQueue = 3000;
        }

        private static Material CreateParticleBeam()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive");
            if (shader == null || !shader.isSupported)
            {
                throw new InvalidOperationException("The additive particle shader is unavailable.");
            }

            var material = new Material(shader)
            {
                name = "OrbitalRing.Particle.Beam",
                renderQueue = 3005,
            };
            try
            {
                StandbyBeamTexture = TrackModelAssets.GetTexture("orbital-final-6516-particle-beam");
                WorkingBeamTexture = TrackModelAssets.GetTexture("orbital-final-6516-particle-beamworking");
                material.SetTexture("_MainTex", StandbyBeamTexture);
                material.SetColor("_TintColor", Color.white);
                material.DisableKeyword("SOFTPARTICLES_ON");
                return material;
            }
            catch
            {
                UnityEngine.Object.Destroy(material);
                throw;
            }
        }

        private static void DestroyMaterials(Material[] materials)
        {
            foreach (Material material in materials)
            {
                UnityEngine.Object.Destroy(material);
            }
        }

        internal static void Clear()
        {
            foreach (Material[] materials in materialSets.Values)
            {
                DestroyMaterials(materials);
            }

            materialSets.Clear();
            AcceleratorStandbyEmission = null;
            AcceleratorWorkingEmission = null;
            StandbyBeamTexture = null;
            WorkingBeamTexture = null;
        }
    }
}
