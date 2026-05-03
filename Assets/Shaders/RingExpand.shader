Shader "G1/RingExpand"
{
    Properties
    {
        _Color      ("Color",       Color)          = (1, 1, 1, 1)
        // 링 중심 반지름 (UV 기준 0~1). C#에서 0→1로 애니메이션
        _Radius     ("Radius",      Range(0, 1))    = 0.5
        // 링 두께 (UV 기준). 작을수록 얇은 링
        _Thickness  ("Thickness",   Range(0, 0.5))  = 0.1
        // 전체 투명도
        _Alpha      ("Alpha",       Range(0, 1))    = 1
        // 엣지 부드러움 (0 = 완전 선명)
        _Smoothness ("Smoothness",  Range(0, 0.05)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half   _Radius;
                half   _Thickness;
                half   _Alpha;
                half   _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // UV 중심 기준 거리 (0=중심 ~ 1=모서리)
                float dist = length(IN.uv - 0.5) * 2.0;

                // 링: _Radius를 중심으로 _Thickness 너비의 띠
                float half_t = _Thickness * 0.5;
                float inner  = _Radius - half_t;
                float outer  = _Radius + half_t;

                float sm = max(_Smoothness, 0.0001);
                float ring = smoothstep(inner - sm, inner + sm, dist)
                           * smoothstep(outer + sm, outer - sm, dist);

                half4 col = _Color;
                col.a = ring * _Alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
