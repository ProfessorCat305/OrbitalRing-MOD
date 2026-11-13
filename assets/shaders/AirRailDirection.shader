Shader "OrbitalRing/AirRailDirection"
{
    Properties
    {
        _MainTex ("Arrow mask", 2D) = "white" {}
        _Opacity ("Body opacity", Range(0,1)) = 0.85
        _GlowIntensity ("Glow intensity", Range(0,2)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ColorMask RGB
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Opacity;
            half _GlowIntensity;

            struct VertexInput
            {
                float4 position : POSITION;
                float2 coordinate : TEXCOORD0;
                half4 color : COLOR;
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 coordinate : TEXCOORD0;
                half4 color : COLOR;
            };

            FragmentInput vert(VertexInput input)
            {
                FragmentInput output;
                output.position = UnityObjectToClipPos(input.position);
                output.coordinate = TRANSFORM_TEX(input.coordinate, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(FragmentInput input) : SV_Target
            {
                half mask = tex2D(_MainTex, input.coordinate).a;
                half edgeWidth = max(fwidth(mask), 0.001);
                half coverage = smoothstep(0.188 - edgeWidth, 0.188 + edgeWidth, mask);
                half opacity = coverage * input.color.a * _Opacity;
                // Premultiplied emission keeps the soft halo independent of the body opacity.
                half3 emission = input.color.rgb * (opacity + mask * _GlowIntensity);
                return half4(emission, opacity);
            }
            ENDCG
        }
    }
    Fallback Off
}
