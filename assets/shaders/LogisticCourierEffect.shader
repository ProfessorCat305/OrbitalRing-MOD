// 配送机拖尾/光效替换 shader。
//
// 原版 "VF Shaders/Forward/Logistic Courier Effect Instancing" 的顶点路径和本体 shader
// 共用同一段代码，同样带有 202.6 的硬编码（在第 183 条指令处）：
//     lift = max(h0 * 3.5 + 202.6 - 0.5 * (|begin| + |end|), 0)
// 所以 100 半径星球上拖尾也会飞到约 200 半径的高度。
//
// 这里同样逐条复刻路径，把 202.6 换成 _PlanetRadius + 2.6，
// 片元用最简单的加法混合光效（贴图 × 颜色 × 透明度），
// 属性名沿用原版 effect shader 的 _MainTex / _MaskTex / _TintColor / _AlphaMultiplier。
Shader "ProjectOrbitalRing/LogisticCourierEffect"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _Color0 ("Color", Vector) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NoiseMap ("Noise", 2D) = "white" {}
        _AlphaMultiplier ("Alpha Multiplier", Float) = 1
        _UVSpeed ("UV Speed", Float) = 0
        _CourierSpeed ("运输机速度", Float) = 10
        _RotateSelfAngleSpeed ("自转角速度", Float) = 360
        _PropellerAngleSpeed ("螺旋桨角速度", Float) = 360
        _PlanetRadius ("星球半径", Float) = 200
        _HoverHeight ("离地悬停高度", Float) = 1.13
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct CourierData
            {
                float3 begin;
                float3 end;
                int    endId;
                float  direction;
                float  maxt;
                float  t;
                int    itemId;
                int    itemCount;
                int    inc;
                int    gene;
            };

            StructuredBuffer<CourierData> _CourierBuffer;

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _NoiseMap;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            float4 _TintColor;
            float4 _Color0;
            float _AlphaMultiplier;
            float _UVSpeed;
            float _CourierSpeed;
            float _RotateSelfAngleSpeed;
            float _PropellerAngleSpeed;
            float _PlanetRadius;
            float _HoverHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvMask : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                CourierData d = _CourierBuffer[v.instanceID];

                float3 bp = d.begin;
                float3 ep = d.end;
                float  dir = d.direction;
                float  maxt = max(d.maxt, 1e-4);
                float  t = d.t;
                float  gene = (float)d.gene;

                float R0 = max(length(bp), 1e-4);
                float3 epP = float3(ep.y, ep.z, ep.x);
                float R1 = max(length(epP), 1e-4);

                float s = saturate(t / maxt);
                float m = clamp(5.0 / maxt, 0.0, 0.3);
                float s2 = saturate((s - m) / max(1.0 - 2.0 * m, 1e-4));
                float sm2 = (3.0 - 2.0 * s2) * s2 * s2;
                float sm = (3.0 - 2.0 * s) * s * s;

                float3 a = bp / R0;
                // epP 是原版 r5 = (end.y, end.z, end.x) 的旋转副本，只用于算 delta；
                // 插值目标方向要用未旋转的 end（原版 r8 = r5.zxy / |end|）
                float3 b = ep / R1;
                float cosA = dot(a, b);
                float angle = acos(clamp(cosA, -1.0, 1.0));
                float3 axis = cross(a, b);
                float al = length(axis);
                if (al < 1e-5)
                {
                    axis = normalize(cross(a, abs(a.y) < 0.99 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0)));
                }
                else
                {
                    axis = axis / al;
                }
                float ang = angle * sm;
                float3 dirv = a * cos(ang) + cross(axis, a) * sin(ang) + axis * dot(axis, a) * (1.0 - cos(ang));
                if (d.endId < 0 && dir > 0.0) dirv = b;

                float radius = lerp(R0, R1, sm2) + _HoverHeight;
                float3 pos = dirv * radius;

                float2 noiseUV = float2(gene * 0.0078125 + 0.00390625, gene * 0.001);
                float3 noise = tex2Dlod(_NoiseMap, float4(noiseUV, 0.0, 0.0)).rgb - 0.5;

                float a0 = maxt * 0.05;
                float c0 = max(maxt - 15.0, 0.0) * 0.05;
                float e0 = (maxt > 20.0) ? sqrt(max(log(a0) + 1.0, 0.0)) : a0;
                float h0 = (max(maxt - 15.0, 0.0) > 20.0) ? sqrt(max(e0, 0.0)) : c0;

                float t1 = 2.0 * (s - 0.5);
                float t2 = 2.0 * (sm2 - 0.5);
                float t3 = 2.0 * (sm - 0.5);
                float p1 = 1.0 - t1 * t1;
                float p2 = 1.0 - t2 * t2;
                float p3 = 1.0 - t3 * t3;

                float3 radial = normalize(pos);
                float3 deltaP = epP - float3(bp.y, bp.z, bp.x);
                float dl = max(length(deltaP), 1e-4);
                float3 binormal = cross(radial, (deltaP / dl) * dir);
                if (length(binormal) < 1e-5)
                {
                    binormal = cross(radial, abs(radial.y) < 0.99 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0));
                }
                binormal = normalize(binormal);
                float3 tangent = normalize(cross(binormal, radial));

                pos += dir * binormal * (noise.x * (e0 * 6.0)) * p1;
                pos += dir * radial * (noise.y * h0) * p2;
                pos += noise.z * dir * tangent * (e0 * 2.0) * p3;

                float f = h0 * 3.5 + (_PlanetRadius + 2.6);
                float lift = max(f - 0.5 * (R0 + R1), 0.0);
                pos += radial * lift * p2;

                float spd = _CourierSpeed;
                float angBase = spd / 6.0;
                if (spd > 6.0) angBase = log(spd / 6.0) + 1.0;
                angBase = (noise.x * 0.1 + angBase) * 0.28 * _RotateSelfAngleSpeed;
                angBase = min(angBase, 540.0);
                float spin = (_Time.y * angBase + gene * 0.01) * 0.0174532925;

                float sn = sin(spin);
                float cn = cos(spin);
                float3 vv = v.vertex.xyz * 0.8;
                float vx = -sn * vv.x + cn * vv.z;
                float vz =  cn * vv.x + sn * vv.z;

                float3 worldPos = pos + vx * tangent + vv.y * radial + vz * binormal;

                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uvMask = TRANSFORM_TEX(v.texcoord, _MaskTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mainTex = tex2D(_MainTex, i.uv);
                float4 maskTex = tex2D(_MaskTex, i.uvMask);
                float3 color = mainTex.rgb * maskTex.rgb * _TintColor.rgb * _Color0.rgb * _AlphaMultiplier;
                float alpha = mainTex.a * maskTex.a * _TintColor.a * _AlphaMultiplier;
                return float4(color, saturate(alpha));
            }
            ENDCG
        }
    }
    Fallback Off
}
