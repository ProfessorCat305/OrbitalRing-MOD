using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class TrackLighting
    {
        private static readonly int reflectionCube0 = Shader.PropertyToID("unity_SpecCube0");
        private static readonly int reflectionCube1 = Shader.PropertyToID("unity_SpecCube1");
        private static readonly int reflectionCube0Hdr = Shader.PropertyToID("unity_SpecCube0_HDR");
        private static readonly int reflectionCube1Hdr = Shader.PropertyToID("unity_SpecCube1_HDR");
        private static readonly int reflectionBoxMin = Shader.PropertyToID("unity_SpecCube0_BoxMin");
        private static readonly int reflectionProbe0 = Shader.PropertyToID("unity_SpecCube0_ProbePosition");
        private static readonly int reflectionProbe1 = Shader.PropertyToID("unity_SpecCube1_ProbePosition");
        private static MaterialPropertyBlock steelLighting;

        internal static MaterialPropertyBlock SteelLighting()
        {
            if (steelLighting == null)
            {
                var properties = new MaterialPropertyBlock();
                Apply(properties);
                steelLighting = properties;
            }
            return steelLighting;
        }

        internal static void Apply(MaterialPropertyBlock properties)
        {
            Cubemap environment = TrackModelAssets.SteelEnvironment;
            properties.SetTexture(reflectionCube0, environment);
            properties.SetTexture(reflectionCube1, environment);
            properties.SetVector(reflectionCube0Hdr, new Vector4(1, 1, 0, 0));
            properties.SetVector(reflectionCube1Hdr, new Vector4(1, 1, 0, 0));
            properties.SetVector(reflectionBoxMin, new Vector4(0, 0, 0, 1));
            properties.SetVector(reflectionProbe0, Vector4.zero);
            properties.SetVector(reflectionProbe1, Vector4.zero);
        }

        internal static void Dispose() => steelLighting = null;
    }
}
