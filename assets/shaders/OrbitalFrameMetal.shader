Shader "OrbitalRing/FrameMetal"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Surface normal", 2D) = "bump" {}
        _BumpScale ("Surface normal scale", Float) = 1
        _DetailNormalMap ("Frame normal", 2D) = "bump" {}
        _DetailNormalMapScale ("Frame normal scale", Float) = 1
        _EmissionMap ("Indicators", 2D) = "black" {}
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Metallic ("Metallic", Range(0,1)) = 0.94
        _Glossiness ("Smoothness", Range(0,1)) = 0.56
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #define _SPECULARHIGHLIGHTS_OFF
        #include "UnityStandardUtils.cginc"

        sampler2D _MainTex, _BumpMap, _DetailNormalMap, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        half _BumpScale, _DetailNormalMapScale, _Metallic, _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_DetailNormalMap;
            float frameAxialScale;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.frameAxialScale = vertex.texcoord1.z;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            output.Albedo = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb;
            half3 surface = UnpackScaleNormal(tex2D(_BumpMap, input.uv_MainTex), _BumpScale);
            half3 frame = UnpackScaleNormal(tex2D(_DetailNormalMap, input.uv2_DetailNormalMap),
                _DetailNormalMapScale);
            // Axial placement scales normal covectors by the inverse segment length.
            frame.y *= max(input.frameAxialScale, 0.0001);
            frame = normalize(frame);
            output.Normal = BlendNormals(surface, frame);
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Emission = tex2D(_EmissionMap, input.uv_MainTex).rgb * _EmissionColor.rgb;
            output.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Standard"
}
