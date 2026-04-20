Shader "Neko Legends/Cel Shader/Outline WebGL"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0.0, 0.1)) = 0.03
    }
        SubShader
    {
        // Render after opaque objects (or adjust the Queue as needed).
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }
        Pass
        {
            Name "Outline"
            // Use the URP forward lighting pass.
            Tags { "LightMode" = "UniversalForward" }
        // Cull Front so that we render the back faces (the extruded outline).
        Cull Front
        ZWrite On
        ZTest LEqual

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        // URP includes – adjust the path if your URP version differs.
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
        };

        float _OutlineWidth;
        float4 _OutlineColor;

        Varyings vert(Attributes v)
        {
            Varyings o;
            // Transform the vertex position and normal into world space.
            float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
            float3 worldNormal = normalize(TransformObjectToWorldNormal(v.normalOS));
            // Offset the vertex along its normal to create the outline.
            worldPos += worldNormal * _OutlineWidth;
            // Convert from world space to clip space.
            o.positionHCS = TransformWorldToHClip(worldPos);
            return o;
        }

        half4 frag(Varyings i) : SV_Target
        {
            return _OutlineColor;
        }
        ENDHLSL
    }
    }
        // You can remove the fallback if you are exclusively targeting URP.
            FallBack "Hidden/Universal Forward"
}
