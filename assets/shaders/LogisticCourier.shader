// 配送机（ItemProto 5003）本体替换 shader。
//
// 原版 "VF Shaders/Forward/Logistic Courier Instancing" 的顶点路径里有一段硬编码：
//     lift = max(h0 * 3.5 + 202.6 - 0.5 * (|begin| + |end|), 0)
//     pos += radial * lift * arcProfile
// 其中 202.6 是按 200 半径星球写的“最低绝对半径”：
//   - 200 半径星球：lift ≈ 7，正常贴地飞行；
//   - 100 半径星球：lift ≈ 107，配送机先爬到约 200 半径再降下来。
//
// 这里把顶点逻辑按反汇编逐条复刻，只把 202.6 换成 _PlanetRadius + 2.6，
// 这样任何半径的星球上 lift 都 ≈ 7，和原版 200 半径星球的表现一致。
// 片元用 Standard 风格的 PBR（albedo / normal / metallic / smoothness / emission），
// 贴图与属性名和原版 shader 保持一致，原材质不需要改。
Shader "ProjectOrbitalRing/LogisticCourier"
{
    Properties
    {
        _Color ("Color 颜色", Vector) = (1,1,1,1)
        _SpecularColor ("Specular Color", Vector) = (1,1,1,1)
        _EmissionMask ("Emission Mask", Vector) = (1,1,1,1)
        _MainTex ("Albedo (RGB) 漫反射 (A) 颜色遮罩", 2D) = "white" {}
        _NormalTex ("Normal 法线", 2D) = "bump" {}
        _MS_Tex ("Metallic (R) 透贴 (G) 金属 (A) 高光", 2D) = "black" {}
        _EmissionTex ("Emission (RGB) 自发光 (A) 抖动遮罩", 2D) = "black" {}
        _EmissionJitterTex ("自发光抖动色条", 2D) = "white" {}
        _AlbedoMultiplier ("漫反射倍率", Float) = 1
        _NormalMultiplier ("法线倍率", Float) = 1
        _MetallicMultiplier ("金属倍率", Float) = 1
        _SmoothMultiplier ("高光倍率", Float) = 1
        _EmissionMultiplier ("自发光倍率", Float) = 5.5
        _EmissionJitter ("自发光抖动倍率", Float) = 0
        _AlphaClip ("透明通道剪切", Float) = 0
        _CullMode ("剔除模式", Float) = 2
        _NoiseMap ("噪声贴图", 2D) = "white" {}
        _CourierSpeed ("运输机速度", Float) = 10
        _RotateSelfAngleSpeed ("自转角速度", Float) = 360
        _PropellerAngleSpeed ("螺旋桨角速度", Float) = 360
        _PlanetRadius ("星球半径", Float) = 200
        _HoverHeight ("离地悬停高度", Float) = 1.13
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_CullMode]

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

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
            sampler2D _NormalTex;
            sampler2D _MS_Tex;
            sampler2D _EmissionTex;
            sampler2D _EmissionJitterTex;
            sampler2D _NoiseMap;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _SpecularColor;
            float4 _EmissionMask;
            float _AlbedoMultiplier;
            float _NormalMultiplier;
            float _MetallicMultiplier;
            float _SmoothMultiplier;
            float _EmissionMultiplier;
            float _EmissionJitter;
            float _AlphaClip;
            float _CourierSpeed;
            float _RotateSelfAngleSpeed;
            float _PropellerAngleSpeed;
            float _PlanetRadius;
            float _HoverHeight;
            float4 _Global_AmbientColor0;
            float4 _Global_AmbientColor1;
            float4 _Global_AmbientColor2;

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
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 tangent : TEXCOORD2;
                float2 uv : TEXCOORD3;
                UNITY_FOG_COORDS(4)
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

                // 大圆插值方向（原版用四元数旋转实现，这里用等价的 Rodrigues 旋转）
                // 注意：epP 是原版 r5 = (end.y, end.z, end.x) 的“旋转副本”，只用于算 delta；
                // 插值目标方向必须用未旋转的 end（原版 r8 = r5.zxy / |end|）。
                float3 a = bp / R0;
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

                // 送给玩家（endId < 0 且 direction > 0）时直接朝 end 方向
                if (d.endId < 0 && dir > 0.0) dirv = b;

                // 原版到达目标时的半径就是 R1 + 1.13（p2 在两端为 0，lift 不参与端点），
                // 1.13 换成了材质属性 _HoverHeight，方便不改 shader 直接调到达高度。
                float radius = lerp(R0, R1, sm2) + _HoverHeight;
                float3 pos = dirv * radius;

                // 每个配送机的随机相位噪声（原版同款）
                float2 noiseUV = float2(gene * 0.0078125 + 0.00390625, gene * 0.001);
                float3 noise = tex2Dlod(_NoiseMap, float4(noiseUV, 0.0, 0.0)).rgb - 0.5;

                // 弧线参数（原版同款）
                float a0 = maxt * 0.05;
                float c0 = max(maxt - 15.0, 0.0) * 0.05;
                float e0 = (maxt > 20.0) ? sqrt(max(log(a0) + 1.0, 0.0)) : a0;
                float h0 = (max(maxt - 15.0, 0.0) > 20.0) ? sqrt(max(e0, 0.0)) : c0;

                // 原版三个弧线轮廓分别是 smooth(t)、smooth((t-m)/(1-2m))、smooth(t/maxt) 的抛物线包络
                // （反汇编里 r3.y = s2²，r4.y = 3-2·s2，相乘就是 smooth(s2)，再减 0.5）
                float t1 = 2.0 * (s - 0.5);
                float t2 = 2.0 * (sm2 - 0.5);
                float t3 = 2.0 * (sm - 0.5);
                float p1 = 1.0 - t1 * t1;
                float p2 = 1.0 - t2 * t2;
                float p3 = 1.0 - t3 * t3;

                // 方向坐标系
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

                // 噪声扰动（原版同款）
                pos += dir * binormal * (noise.x * (e0 * 6.0)) * p1;
                pos += dir * radial * (noise.y * h0) * p2;
                pos += noise.z * dir * tangent * (e0 * 2.0) * p3;

                // 关键修正：原版是 202.6，这里换成真实星球半径 + 2.6
                float f = h0 * 3.5 + (_PlanetRadius + 2.6);
                float lift = max(f - 0.5 * (R0 + R1), 0.0);
                pos += radial * lift * p2;

                // 自转 / 螺旋桨角度（原版同款公式：基频由 _CourierSpeed 决定，再由 _RotateSelfAngleSpeed 缩放）
                float spd = _CourierSpeed;
                float angBase = spd / 6.0;
                if (spd > 6.0) angBase = log(spd / 6.0) + 1.0;
                angBase = (noise.x * 0.1 + angBase) * 0.28 * _RotateSelfAngleSpeed;
                angBase = min(angBase, 540.0);
                float spin = (_Time.y * angBase + gene * 0.01) * 0.0174532925;

                float sn = sin(spin);
                float cn = cos(spin);
                float3 vv = v.vertex.xyz * 0.7;
                float vx = -sn * vv.x + cn * vv.z;
                float vz =  cn * vv.x + sn * vv.z;

                float3 worldPos = pos + vx * tangent + vv.y * radial + vz * binormal;

                o.worldPos = worldPos;
                o.pos = UnityWorldToClipPos(worldPos);
                o.normal = normalize(v.normal.x * binormal + v.normal.y * radial + v.normal.z * tangent);
                o.tangent = float4(normalize(v.tangent.xyz.x * binormal + v.tangent.xyz.y * radial + v.tangent.xyz.z * tangent), v.tangent.w);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 T = normalize(i.tangent.xyz);
                float3 B = cross(N, T) * i.tangent.w;
                float3 nt = UnpackNormal(tex2D(_NormalTex, i.uv));
                nt.xy *= _NormalMultiplier;
                float3 Nw = normalize(T * nt.x + B * nt.y + N * nt.z);

                float4 albedoSample = tex2D(_MainTex, i.uv);
                float3 albedo = albedoSample.rgb * _AlbedoMultiplier * _Color.rgb;

                float2 ms = tex2D(_MS_Tex, i.uv).rb;
                float metallic = saturate(ms.x * _MetallicMultiplier);
                float smoothness = saturate(ms.y * _SmoothMultiplier);

                float3 emission = tex2D(_EmissionTex, i.uv).rgb * _EmissionMask.rgb * _EmissionMultiplier;

                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(Nw, L));
                float NdotV = saturate(dot(Nw, V)) + 1e-4;

                float3 diffuseColor = albedo * (1.0 - metallic);
                float3 specColor = lerp(0.04, albedo, metallic);
                float roughness = saturate(1.0 - smoothness);
                float a2 = roughness * roughness;
                a2 *= a2;

                float3 H = normalize(L + V);
                float NdotH = saturate(dot(Nw, H));
                float dd = NdotH * NdotH * (a2 - 1.0) + 1.0;
                float D = a2 / (3.14159265 * dd * dd);
                float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
                float G = (NdotL / (NdotL * (1.0 - k) + k)) * (NdotV / (NdotV * (1.0 - k) + k));
                float3 F = specColor + (1.0 - specColor) * pow(1.0 - saturate(dot(V, H)), 5.0);
                float3 spec = D * G * F / (4.0 * NdotV * NdotL + 1e-4) * NdotL;

                // DSP 的 PBR shader 用 _Global_AmbientColor0 作为行星环境光（而不是 SH）
                float3 ambient = _Global_AmbientColor0.rgb;

                float3 color = diffuseColor * (_LightColor0.rgb * NdotL + ambient)
                             + spec * _LightColor0.rgb
                             + emission;

                UNITY_APPLY_FOG(i.fogCoord, color);
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
