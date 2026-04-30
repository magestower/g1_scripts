Shader "Neko Legends/Cel Shader/Outline" {
    Properties{
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineSize("Outline Size", Range(.001, 0.5)) = 0.25
        _OutlineMultiplier("Outline Multiplier", Range(0, 1)) = .03
        _OutlineGrowth("Outline Growth", Range(-.01, -.00015)) = -0.0069
        _OutlineAutoShrink("Outline Auto Shrink", Float) = 0
        _OutlineInitialSize("Outline Initial Size", Range(.001, .5)) = 0.33
        _OutlineShrinkFactor("Outline Shrink Factor", Range(0, 10)) = 7
        _OutlineGrowthShrinkFactor("Outline Growth Shrink Factor", Range(0, 10)) = 5
        _OutlineGrowthShrinkSensitivity("Outline Growth Shrink Sensitivity", Range(-25, 25)) = 0
        _OutlineMaxDistance("Outline Max Distance", Range(0, 100)) = 50
    }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
    };

    struct v2f {
        float4 pos : SV_POSITION;
        float4 color : COLOR;
    };

    float _OutlineSize;
    float _OutlineGrowth;
    float4 _OutlineColor;
    float _OutlineMultiplier;
    float _OutlineAutoShrink;
    float _OutlineShrinkFactor;
    float _OutlineInitialSize;
    float _OutlineGrowthShrinkFactor;
    float _OutlineGrowthShrinkSensitivity;
    float _OutlineMaxDistance;

    v2f vert(appdata v) {
        v2f o;
        o.pos = TransformObjectToHClip(v.vertex.xyz);
        float3 scale = float3(
            length(unity_ObjectToWorld._m00_m10_m20),
            length(unity_ObjectToWorld._m01_m11_m21),
            length(unity_ObjectToWorld._m02_m12_m22)
            );
        float averageScale = (scale.x + scale.y + scale.z) / 3;
        float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal)) * scale;
        float3 vert = normalize(mul((float4x4)UNITY_MATRIX_IT_MV, v.vertex)) * scale;
        float2 offset = mul((float2x2)UNITY_MATRIX_P, float2(lerp(norm.x, vert.x, 0), lerp(norm.y, vert.y, 0)));
        float distance = length(unity_ObjectToWorld._m03_m13_m23 - _WorldSpaceCameraPos);

        // Calculate shrink factor based on distance
        float shrinkFactor = 1.0;
        if (_OutlineAutoShrink > 0.5) {
            float normalizedDistance = distance * _OutlineShrinkFactor;
            shrinkFactor = 1.0 / (1.0 + exp(-normalizedDistance)) * _OutlineInitialSize;
        }
        o.pos.xy += offset * _OutlineSize * averageScale * _OutlineMultiplier * shrinkFactor;

        // Calculate growth shrink factor based on distance
        float growthShrinkFactor = 1.0;
        if (_OutlineAutoShrink > 0.5) {
            if (distance <= _OutlineMaxDistance) {
                growthShrinkFactor = 1.0 / (1.0 + exp(-_OutlineGrowthShrinkSensitivity * (distance - _OutlineGrowthShrinkFactor)));
                o.pos.z += _OutlineGrowth * growthShrinkFactor;
            }
            else
            {
                o.pos.z = 0;//wont render lines based on max distance slider
            }
        }
        else {
            o.pos.z += _OutlineGrowth;
        }



        // Output color
        o.color = _OutlineColor;
        return o;
    }
    ENDHLSL

        SubShader{
            Pass{
                Name "OUTLINE"
                Tags { "LightMode" = "UniversalForward" }
                Cull Off
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                float4 frag(v2f i) : SV_Target
                {
                    return i.color;
                }
                ENDHLSL
            }

            Pass{
                Name "OUTLINE"
                Tags { "LightMode" = "UniversalGBuffer" }
                Cull Off
                Stencil {
                    Ref 1
                    Comp Always
                    Pass Replace
                }
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                float4 frag(v2f i) : SV_Target
                {
                    return i.color;
                }
                ENDHLSL
            }
    }

        CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
                    CustomEditorForRenderPipeline "NekoLegends.CelShaderInspectorOutline" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
                    FallBack "Hidden/Shader Graph/FallbackError"
}
