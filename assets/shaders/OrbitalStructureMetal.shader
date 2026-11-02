Shader "OrbitalRing/StructureMetal"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo and tint mask", 2D) = "white" {}
        _MS_Tex ("Metallic and smoothness mask", 2D) = "white" {}
        _EmissionTex ("Emission", 2D) = "black" {}
        _EmissionMask ("Emission tint", Color) = (1,1,1,1)
        _AlbedoMultiplier ("Albedo", Float) = 1
        _MetallicMultiplier ("Metallic", Range(0,1)) = 0.25
        _SmoothMultiplier ("Smoothness", Range(0,1)) = 0.4
        _EmissionMultiplier ("Emission intensity", Float) = 1
        _EmissionMipBias ("Emission mip bias", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StructureMetal addshadow noforwardadd exclude_path:deferred exclude_path:prepass
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex, _MS_Tex, _EmissionTex;
        half4 _Color, _EmissionMask;
        half _AlbedoMultiplier, _MetallicMultiplier, _SmoothMultiplier;
        half _EmissionMultiplier, _EmissionMipBias;
        half4 _Global_AmbientColor0, _Global_AmbientColor1, _Global_AmbientColor2;
        float _OrbitalPlanetRadius;

        struct Input
        {
            float2 uv_MainTex;
        };

        inline half4 LightingStructureMetal(SurfaceOutputStandard surface, half3 viewDirection, UnityGI lighting)
        {
            return LightingStandard(surface, viewDirection, lighting);
        }

        inline void LightingStructureMetal_GI(SurfaceOutputStandard surface, UnityGIInput input, inout UnityGI lighting)
        {
            LightingStandard_GI(surface, input, lighting);
            float sunDistance = dot(input.worldPos, input.light.dir);
            // A ray from the surface toward the sun is occluded if its closest point lies inside the planet.
            float3 closestPoint = input.worldPos - min(sunDistance, 0) * input.light.dir;
            float clearance = length(closestPoint) - _OrbitalPlanetRadius;
            float transition = max(1, _OrbitalPlanetRadius * 0.04);
            half sunlight = smoothstep(-transition, transition, clearance);
            lighting.light.color *= sunlight;

            half elevation = sunDistance / length(input.worldPos);
            half3 dusk = lerp(_Global_AmbientColor2.rgb, _Global_AmbientColor1.rgb, smoothstep(-0.3, 0, elevation));
            lighting.indirect.diffuse = lerp(dusk, _Global_AmbientColor0.rgb, smoothstep(0, 0.3, elevation));
            lighting.indirect.specular *= lerp(0.25h, 1.0h, sunlight);
        }

        void surf(Input input, inout SurfaceOutputStandard surface)
        {
            half4 albedo = tex2D(_MainTex, input.uv_MainTex);
            half2 metalSmoothness = tex2D(_MS_Tex, input.uv_MainTex).rb;
            half tint = saturate((albedo.a - 0.1h) * 1.25h);
            surface.Albedo = albedo.rgb * lerp(1.0h, _Color.rgb, tint) * _AlbedoMultiplier;
            surface.Metallic = metalSmoothness.x * _MetallicMultiplier;
            surface.Smoothness = metalSmoothness.y * _SmoothMultiplier;
            surface.Emission = tex2Dbias(_EmissionTex, float4(input.uv_MainTex, 0, _EmissionMipBias)).rgb
                * _EmissionMask.rgb * _EmissionMultiplier;
            surface.Alpha = 1;
        }
        ENDCG
    }
    Fallback Off
}
