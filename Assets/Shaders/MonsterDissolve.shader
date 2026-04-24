Shader "G1/MonsterDissolve"
{
    Properties
    {
        _MainTex        ("Main Texture",    2D)             = "white" {}
        _Color          ("Color",           Color)          = (1, 1, 1, 1)
        // 디졸브 진행도: 0 = 완전히 보임, 1 = 완전히 사라짐
        _DissolveAmount ("Dissolve Amount", Range(0, 1))    = 0
        // 디졸브 경계 발광 색상
        _EdgeColor      ("Edge Color",      Color)          = (0.2, 0.8, 1, 1)
        // 디졸브 경계 발광 두께 (0~0.2)
        _EdgeWidth      ("Edge Width",      Range(0, 0.2))  = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half4  _EdgeColor;
                half   _EdgeWidth;
            CBUFFER_END

            // 해시 기반 의사난수 — 노이즈 텍스처 없이 UV에서 디졸브 패턴 생성
            float hash(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // 격자 노이즈 (인접 셀 보간)
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(
                    lerp(hash(i),              hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x),
                    u.y
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 멀티스케일 노이즈로 자연스러운 디졸브 패턴 생성
                float n = noise(IN.uv * 8.0) * 0.6
                        + noise(IN.uv * 16.0) * 0.3
                        + noise(IN.uv * 32.0) * 0.1;

                // 노이즈값이 디졸브 진행도보다 작으면 클립
                clip(n - _DissolveAmount);

                // 경계(엣지) 영역 판정 — 클립 직전 _EdgeWidth 범위
                half edgeMask = step(n - _DissolveAmount, _EdgeWidth);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // 간단한 람버트 조명
                Light mainLight = GetMainLight();
                half  NdotL     = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                half3 lighting  = mainLight.color * (NdotL * 0.8 + 0.2); // 0.2 ambient

                half3 col = texColor.rgb * lighting;
                // 경계에 엣지 색상 합성
                col = lerp(col, _EdgeColor.rgb, edgeMask * _EdgeColor.a);

                col = MixFog(col, IN.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // 그림자 패스 — 디졸브된 부분은 그림자도 사라짐
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half4  _EdgeColor;
                half   _EdgeWidth;
            CBUFFER_END

            float hash(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i),               hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)),  hash(i + float2(1,1)), u.x),
                    u.y
                );
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                // URP 표준 그림자 바이어스 적용
                float4 posCS  = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                OUT.positionHCS = posCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target
            {
                float n = noise(IN.uv * 8.0) * 0.6
                        + noise(IN.uv * 16.0) * 0.3
                        + noise(IN.uv * 32.0) * 0.1;
                clip(n - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }
}
