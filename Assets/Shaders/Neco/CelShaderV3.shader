Shader "Neko Legends/Cel Shader/Anime Shader v3"
{
    Properties
    {
        [Header(Outlines)]
        // Outline Properties
        [Toggle(_USE_OUTLINES_ON)] _use_outlines("Use Outlines", Float ) = 0
        _ASEOutlineWidth( "Outline Width", Range(0,.005) ) = 0.002
        _ASEOutlineColor( "Outline Color", Color ) = (0.0,0.0,0,1)
        [HideInInspector]_ASEOutalpha( "_ASEOutalpha", Range(-1,0) ) = 0

        [Header(Main)]
        // General Properties
        _MainTex("Main Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        _Cutoff( "Mask Alpha Clip Cutoff", Float ) = 0.5
        [HideInInspector] _texcoord( "", 2D ) = "white" {}

        // Shadow Properties
        _Cel_Shader_Offset("Cel Shader Offset", Range( 0 , 1)) = 0.64
        _Cel_Ramp_Smoothness("Cel Ramp Smoothness", Range( 0 , 1)) = 0

        // Lighting Color Properties
        _light("Light", Color ) = (1,1,1,0)
        _dark("Dark", Color ) = (0,0,0,0)

        _Contrast("Contrast", Range(0.1, 3)) = 1

        [Header(Main Emissive)]
        [Toggle(_USE_MAIN_EMISSIVE_ON)] _use_main_emissive("Use Main Emissive", Float ) = 0
        _Main_Emissive_Tex("Main Emissive Texture", 2D) = "white" {}
        _Main_Emissve_color("Main Emissive Color", Color ) = (1,1,1,0)
        _Main_Emissve_power("Main Emissive Power", Range( -1 , 5)) = 1

        // Second Texture Properties
        [Toggle(_USE_SECOND_TEX_ON)] _use_second_tex("Use Second Texture", Float ) = 0
        _BuffTex("Second Texture", 2D) = "white" {}
        _BuffTex_switch("Second Switch Mask", 2D) = "white" {}
        _BuffTex_switch_edge_hardness("Second Switch Edge Hardness", Range( 0 , 22)) = 1
        _BuffTex_switch_dissolve("Second Switch Dissolve", Range( 0 , 1)) = 1

        [Header(Second Emissive)]
        [Toggle(_USE_SECOND_EMISSIVE_ON)] _use_second_emissive("Use Second Emissive", Float ) = 0
        _Second_Emissive_Tex("Second Emissive Texture", 2D) = "white" {}
        _Second_Emissve_color("Second Emissive Color", Color ) = (0,0,0,0)
        _Second_Emissve_power("Second Emissive Power", Range( -1 , 3)) = 0

        // Matcap Properties
        [Toggle(_USE_MATCAT_ON)] _use_matcat("Use Mat Cap", Float ) = 0
        _matcap("Matcap Texture", 2D) = "white" {}
        _special_buff_switch("Matcap Switch Mask", 2D) = "white" {}
        _special_buff_switch_edge_hardness("Matcap Switch Edge Hardness", Range( 0 , 22)) = 1
        _special_buff_dissolve("Matcap Switch Dissolve", Range( 0 , 1)) = 1
        [Header(Matcap Controls)]
        _MatcapIntensity("Matcap Intensity", Range(0,1)) = 1      // blend amount
        _MatcapObjectSpace("Matcap Object-Space", Float) = 0       // 0 = view-space (current), 1 = object-space
        [Toggle(_USE_MATCAP_REFLECTION_ON)] _use_matcap_reflection("Matcap Reflection Mode", Float ) = 1

        [Header(Matcap Emissive)]
        [Toggle(_USE_MATCAP_EMISSIVE_ON)] _use_matcap_emissive("Use Matcap Emissive", Float ) = 0
        _Matcap_Emissive_Tex("Matcap Emissive Texture", 2D) = "white" {}
        _Matcap_Emissve_color("Matcap Emissive Color", Color ) = (0,0,0,0)
        _Matcap_Emissve_power("Matcap Emissive Power", Range( -1 , 3)) = 0

        [Header(Matcap Animation)]
        [Toggle(_USE_MATCAP_ANIMATION_ON)] _use_matcap_animation("Animate Matcap Texture", Float ) = 0
        _matcap_animation_speed("Matcap Animation Speed", Range(0,10)) = 1

        // Fresnel Properties
        [Toggle(_USE_FRENSEL_ON)] _use_frensel("Use Fresnel", Float ) = 0
        _frensel_range("Fresnel Range", Range( -1 , 1)) = .6
        _frensel_hard("Fresnel Hardness", Range( 0 , 1)) = .8
        _frensel_power("Fresnel Power", Range( 0 , 3)) = 1
        [HDR]_frensel_color("Fresnel Color", Color ) = (0,0,0,0)

        // Dissolve Properties
        [Toggle(_USE_DISSOLVE_ON)] _use_dissolve("Use Dissolve", Float ) = 0
        _dissolve("Dissolve Texture", 2D) = "white" {}
        _dissolve_edge("Dissolve Edge", Range( 0 , 1)) = 1
        _edge_width("Edge Width", Range( 0 , 1)) = 0
        _edge_clip("Edge Clip", Range( 0 , 1)) = 0
        _dissolve_Emissve_color("Dissolve Emissive Color", Color ) = (0,0,0,0)
        _dissolve_Emissve_power("Dissolve Emissive Power", Range( 0 , 3)) = 0
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "Queue" = "AlphaTest+0" "RenderPipeline" = "UniversalPipeline"}

        // Outline Pass
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #pragma shader_feature_local _USE_OUTLINES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _ASEOutlineWidth;
            float4 _ASEOutlineColor;
            float _ASEOutalpha;
            float _MatcapIntensity;
            float _MatcapObjectSpace;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            #ifdef _USE_OUTLINES_ON
            Varyings outlineVert (Attributes input)
            {
                Varyings output;
                input.positionOS.xyz += input.normalOS * _ASEOutlineWidth;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 outlineFrag (Varyings input) : SV_Target
            {
                clip(_ASEOutalpha);
                return half4(_ASEOutlineColor.rgb, 1);
            }
            #else
            // Degenerate case: collapse to a point to avoid rasterization and fragment compute
            Varyings outlineVert (Attributes input)
            {
                Varyings output;
                output.positionCS = float4(0, 0, 0, 1); // All vertices same point -> zero-area triangles
                return output;
            }

            half4 outlineFrag (Varyings input) : SV_Target
            {
                discard; // Safety, though no frags should run
                return 0;
            }
            #endif
            ENDHLSL
        }

        // Main Forward Pass
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _USE_SECOND_TEX_ON
            #pragma shader_feature_local _USE_MATCAT_ON
            #pragma shader_feature_local _USE_FRENSEL_ON
            #pragma shader_feature_local _USE_MAIN_EMISSIVE_ON
            #pragma shader_feature_local _USE_SECOND_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_EMISSIVE_ON
            #pragma shader_feature_local _USE_DISSOLVE_ON
            #pragma shader_feature_local _USE_MATCAP_REFLECTION_ON
            #pragma shader_feature_local _USE_MATCAP_ANIMATION_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _BuffTex_ST;
            float4 _BuffTex_switch_ST;
            float4 _NormalMap_ST;
            float4 _special_buff_switch_ST;
            float4 _Main_Emissive_Tex_ST;
            float4 _Second_Emissive_Tex_ST;
            float4 _Matcap_Emissive_Tex_ST;
            float4 _dissolve_ST;
            float _Cel_Shader_Offset;
            float _Cel_Ramp_Smoothness;
            float4 _light;
            float4 _dark;
            float _BuffTex_switch_edge_hardness;
            float _BuffTex_switch_dissolve;
            float _special_buff_switch_edge_hardness;
            float _special_buff_dissolve;
            float _frensel_range;
            float _frensel_hard;
            float _frensel_power;
            float4 _frensel_color;
            float4 _Main_Emissve_color;
            float _Main_Emissve_power;
            float4 _Second_Emissve_color;
            float _Second_Emissve_power;
            float4 _Matcap_Emissve_color;
            float _Matcap_Emissve_power;
            float _dissolve_edge;
            float _edge_width;
            float _edge_clip;
            float4 _dissolve_Emissve_color;
            float _dissolve_Emissve_power;
            float _Cutoff;
            float _MatcapObjectSpace;
            float _MatcapIntensity;
            float _matcap_animation_speed;
            float _Contrast;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BuffTex); SAMPLER(sampler_BuffTex);
            TEXTURE2D(_BuffTex_switch); SAMPLER(sampler_BuffTex_switch);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_matcap); SAMPLER(sampler_matcap);
            TEXTURE2D(_special_buff_switch); SAMPLER(sampler_special_buff_switch);
            TEXTURE2D(_Main_Emissive_Tex); SAMPLER(sampler_Main_Emissive_Tex);
            TEXTURE2D(_Second_Emissive_Tex); SAMPLER(sampler_Second_Emissive_Tex);
            TEXTURE2D(_Matcap_Emissive_Tex); SAMPLER(sampler_Matcap_Emissive_Tex);
            TEXTURE2D(_dissolve); SAMPLER(sampler_dissolve);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.bitangentWS = normalInput.bitangentWS;

                output.uv = input.uv;

                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Dissolve alpha
                float2 uv_dissolve = TRANSFORM_TEX(input.uv, _dissolve);
                float dissolveVal = SAMPLE_TEXTURE2D(_dissolve, sampler_dissolve, uv_dissolve).r;
                float dissolveHard = 2.0;
                float dissolveLerp = lerp(dissolveHard, -1.0, _dissolve_edge);
                float alpha = saturate( (dissolveVal * dissolveHard) - dissolveLerp );
                clip(alpha - _Cutoff);

                // Albedo
                float2 uv_MainTex = TRANSFORM_TEX(input.uv, _MainTex);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_MainTex);

                // Main Emissive
                #if defined(_USE_MAIN_EMISSIVE_ON)
                float2 uv_Main_Emissive = TRANSFORM_TEX(input.uv, _Main_Emissive_Tex);
                float mainEmisVal = SAMPLE_TEXTURE2D(_Main_Emissive_Tex, sampler_Main_Emissive_Tex, uv_Main_Emissive).r;
                float4 mainEmisCol = mainEmisVal * (_Main_Emissve_color * _Main_Emissve_power);
                baseTex += mainEmisCol;
                #endif

                float4 albedo = baseTex;
                #if defined(_USE_SECOND_TEX_ON)
                float2 uv_BuffTex = TRANSFORM_TEX(input.uv, _BuffTex);
                float4 buffTex = SAMPLE_TEXTURE2D(_BuffTex, sampler_BuffTex, uv_BuffTex);

                // Second Emissive
                #if defined(_USE_SECOND_EMISSIVE_ON)
                float2 uv_Second_Emissive = TRANSFORM_TEX(input.uv, _Second_Emissive_Tex);
                float secondEmisVal = SAMPLE_TEXTURE2D(_Second_Emissive_Tex, sampler_Second_Emissive_Tex, uv_Second_Emissive).r;
                float4 secondEmisCol = secondEmisVal * (_Second_Emissve_color * _Second_Emissve_power);
                buffTex += secondEmisCol;
                #endif

                float2 uv_BuffTex_switch = TRANSFORM_TEX(input.uv, _BuffTex_switch);
                float buffSwitchVal = SAMPLE_TEXTURE2D(_BuffTex_switch, sampler_BuffTex_switch, uv_BuffTex_switch).r;
                float buffHard = _BuffTex_switch_edge_hardness;
                float buffLerp = lerp(buffHard, -1.0, _BuffTex_switch_dissolve);
                float buffMask = saturate( (buffSwitchVal * buffHard) - buffLerp );
                albedo = lerp(baseTex, buffTex, buffMask);
                #endif

                // Normal
                float2 uv_NormalMap = TRANSFORM_TEX(input.uv, _NormalMap);
                float3 tangentNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv_NormalMap).xyz * 2.0 - 1.0;
                float3x3 tbn = float3x3(input.tangentWS.xyz, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(tangentNormal, tbn));

                // View dir
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Lighting accumulation (start with dark tint)
                float4 litColor = albedo * _dark;

                // Main light
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, 1);
                if (mainLight.color.r + mainLight.color.g + mainLight.color.b > 0.0001) // Check if light exists
                {
                    float ndl = dot(normalWS, mainLight.direction);
                    float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                    float hl = (ndl * atten + 1.0) * 0.5;
                    float s = smoothstep(_Cel_Shader_Offset, _Cel_Shader_Offset + _Cel_Ramp_Smoothness, hl);
                    litColor += albedo * (_light - _dark) * s;
                }

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                int addLightCount = GetAdditionalLightsCount();
                for (int li = 0; li < addLightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS, 1);
                    if (addLight.color.r + addLight.color.g + addLight.color.b > 0.0001)
                    {
                        float ndl = dot(normalWS, addLight.direction);
                        float atten = addLight.shadowAttenuation * addLight.distanceAttenuation;
                        float hl = (ndl * atten + 1.0) * 0.5;
                        float s = smoothstep(_Cel_Shader_Offset, _Cel_Shader_Offset + _Cel_Ramp_Smoothness, hl);
                        litColor += albedo * (_light - _dark) * s;
                    }
                }
                #endif

                // ──────────────────────────────────────────
                // Matcap
                float4 basePart = litColor;
                #if defined(_USE_MATCAT_ON)

                    // ─── UV generation (object-space vs view-space / reflection) ───
                    float2 matcapUV;
                    if (_MatcapObjectSpace > 0.5)               // checkbox → locks highlight
                    {
                        float3 nObj  = normalize(input.normalWS);
                        matcapUV     = nObj.xy * 0.5 + 0.5;
                    }
                    else
                    {
                        #ifdef _USE_MATCAP_REFLECTION_ON
                            float3 reflectDir  = reflect(-viewDirWS, normalWS);
                            float3 viewReflect = mul((float3x3)UNITY_MATRIX_V, reflectDir);
                            float  m           = 2.828427 * sqrt(viewReflect.z + 1.0);
                            matcapUV = viewReflect.xy / m + 0.5;
                        #else
                            float3 normalView = mul((float3x3)UNITY_MATRIX_V, normalWS);
                            matcapUV = normalView.xy * 0.5 + 0.5;
                        #endif
                    }

                    // ─── Animation (rotate UV if enabled) ───
                    #if defined(_USE_MATCAP_ANIMATION_ON)
                        float angle = _Time.y * _matcap_animation_speed;
                        float ca = cos(angle);
                        float sa = sin(angle);
                        float2x2 rot = float2x2(ca, -sa, sa, ca);
                        matcapUV = mul(rot, (matcapUV - 0.5)) + 0.5;
                    #endif

                    // ─── Sample & optional emissive ───
                    float4 matcapCol = SAMPLE_TEXTURE2D(_matcap, sampler_matcap, matcapUV);

                    #if defined(_USE_MATCAP_EMISSIVE_ON)
                        float2 uv_Matcap_Emissive = TRANSFORM_TEX(input.uv, _Matcap_Emissive_Tex);
                        float  matcapEmisVal      = SAMPLE_TEXTURE2D(_Matcap_Emissive_Tex,
                                                                    sampler_Matcap_Emissive_Tex,
                                                                    uv_Matcap_Emissive).r;
                        float4 matcapEmisCol      = matcapEmisVal *
                                                    (_Matcap_Emissve_color * _Matcap_Emissve_power);
                        matcapCol += matcapEmisCol;
                    #endif

                    // ─── Scale by intensity slider ───
                    matcapCol *= _MatcapIntensity;

                    // ─── Mask & additive blend ───
                    float2 uv_special_buff_switch = TRANSFORM_TEX(input.uv, _special_buff_switch);
                    float  specialVal   = SAMPLE_TEXTURE2D(_special_buff_switch,
                                                        sampler_special_buff_switch,
                                                        uv_special_buff_switch).r;
                    float  specialHard  = _special_buff_switch_edge_hardness;
                    float  specialLerp  = lerp(specialHard, -1.0, _special_buff_dissolve);
                    float  specialMask  = saturate((specialVal * specialHard) - specialLerp);

                    // Additive tint instead of full replace
                    basePart = lerp(basePart, basePart + matcapCol, specialMask);
                #endif

                // ──────────────────────────────────────────
                // Fresnel
                float4 color = basePart;
                #if defined(_USE_FRENSEL_ON)
                    float nv         = dot(normalWS, viewDirWS);
                    float rim        = 1.0 - nv;
                    float rimSmooth  = smoothstep(_frensel_range, _frensel_range + _frensel_hard, rim);
                    float4 fresnelCol = (_frensel_color * _frensel_power) * saturate(rimSmooth);
                    color += fresnelCol;
                #endif

                // ──────────────────────────────────────────
                // Dissolve edge
                #if defined(_USE_DISSOLVE_ON)
                    float edgeSmooth = smoothstep(_edge_clip,
                                                _edge_clip + _edge_width,
                                                1.0 - alpha);
                    float4 edgeCol   = (_dissolve_Emissve_color * _dissolve_Emissve_power) *
                                    edgeSmooth;
                    color += edgeCol;
                #endif

                color.rgb = (color.rgb - 0.5) * _Contrast + 0.5;

                return color;

            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _dissolve_ST;
            float _dissolve_edge;
            float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_dissolve); SAMPLER(sampler_dissolve);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _LightDirection; // For normal bias

            Varyings shadowVert (Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(float3(0,0,1)); // Simple, since no normal needed for clip
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                output.positionCS = clipPos;
                output.uv = input.uv;
                return output;
            }

            half4 shadowFrag (Varyings input) : SV_Target
            {
                float2 uv_dissolve = TRANSFORM_TEX(input.uv, _dissolve);
                float dissolveVal = SAMPLE_TEXTURE2D(_dissolve, sampler_dissolve, uv_dissolve).r;
                float dissolveHard = 2.0;
                float dissolveLerp = lerp(dissolveHard, -1.0, _dissolve_edge);
                float alpha = saturate( (dissolveVal * dissolveHard) - dissolveLerp );
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "NekoLegends.AnimeCelShaderInspectorV3"
}