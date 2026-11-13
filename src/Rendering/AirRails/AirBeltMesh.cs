using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class AirBeltMesh
    {
        internal const float BeamHalfWidth = 0.11f;
        internal const float BeamBottom = -0.15f;
        internal const float BeamTop = 0f;
        internal const float BeamOffset = 0.04f;
        internal const float WireSide = 0f;
        // The conductor clears the native four-stack cargo envelope at 0.45 m.
        internal const float WireHeight = 0.58f;
        internal const float WireHalfWidth = 0.008f;
        private static readonly Color steel = new Color(0.68f, 0.72f, 0.76f);
        internal static readonly Vector2[] BeamSection =
        {
            new Vector2(-BeamHalfWidth, BeamTop), new Vector2(BeamHalfWidth, BeamTop),
            new Vector2(0.065f, -0.055f), new Vector2(0.065f, -0.115f),
            new Vector2(0.10f, BeamBottom), new Vector2(-0.10f, BeamBottom),
            new Vector2(-0.065f, -0.115f), new Vector2(-0.065f, -0.055f),
        };
        internal static readonly Color DirectionColor = new Color(0.16f, 0.64f, 1.4f, 1f);
        private static Material directionMaterial;

        internal static Material BeamMaterial(Material source)
        {
            var material = new Material(source) { name = "Air belt monorail" };
            material.SetColor("_Color", Color.white);
            material.SetColor("_EmissionColor", Color.white * 0.35f);
            material.SetTexture("_MainTex", Texture("palette"));
            material.SetTexture("_NormalTex", Texture("flatNormal"));
            material.SetTexture("_MSTex", Texture("surfaceProperties"));
            material.SetTexture("_EmissionTex", Texture("emission"));
            material.SetTexture("_ClipTex", Texture2D.whiteTexture);
            material.SetFloat("_UVSpeed", 0);
            return material;
        }

        internal static Material CapMaterial(Material source)
        {
            var material = new Material(source) { name = "Air belt monorail end" };
            material.SetColor("_Color", steel);
            material.SetColor("_Color1", steel);
            material.SetColor("_Color2", steel);
            material.SetColor("_Color3", steel);
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            material.SetTexture("_NormalTex", Texture("flatNormal"));
            material.SetTexture("_MSTex", Texture("surfaceProperties"));
            material.SetTexture("_EmissionTex", Texture2D.blackTexture);
            material.SetTexture("_ClipTex", Texture2D.whiteTexture);
            return material;
        }

        internal static Material DirectionMaterial
        {
            get
            {
                EnsureDirectionMaterial();
                return directionMaterial;
            }
        }

        private static void EnsureDirectionMaterial()
        {
            if (directionMaterial != null)
            {
                return;
            }

            Shader shader = TrackModelAssets.AirDirectionShader;
            Texture2D texture = TrackModelAssets.GetTexture("air-direction");
            directionMaterial = new Material(shader)
            {
                name = "AirBelt.Direction",
                mainTexture = texture,
                renderQueue = 3005,
            };
        }

        internal static void ReleaseDirectionMaterial()
        {
            Object.Destroy(directionMaterial);
            directionMaterial = null;
        }

        private static Texture2D Texture(string channel) =>
            TrackModelAssets.GetTexture("air-PoweredRail-" + channel);

        internal static Mesh Beam(int anchorCount) =>
            TrackModelAssets.Decoration("air-PoweredRail-beam-" + anchorCount);

        internal static Mesh Cap(int index) =>
            TrackModelAssets.Decoration("air-PoweredRail-cap-" + index);
    }
}
