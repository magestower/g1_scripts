void CelShading_float(
    in float3 Normal,
    in float ToonRampSmoothness,
    in float3 ClipSpacePos,
    in float3 WorldPos,
    in float3 ToonRampTinting,
    in float ToonRampOffset,
    in float ToonRampOffsetPoint,
    in float Ambient,
    out float3 ToonRampOutput,
    out float3 Direction)
{
#ifdef SHADERGRAPH_PREVIEW
    ToonRampOutput = float3(0.5, 0.5, 0);
    Direction = float3(0.5, 0.5, 0);
#else
#if SHADOWS_SCREEN
    half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif

#if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
    Light light = GetMainLight(shadowCoord);
#else
    Light light = GetMainLight();
#endif

    // Compute main light influence (clamped)
    half dMain = saturate(dot(Normal, light.direction) * 0.5 + 0.5);
    half toonRampMain = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, dMain);

    // Process additional (pixel) lights
    float3 extraLights = float3(0, 0, 0);
    int pixelLightCount = GetAdditionalLightsCount();
    for (int j = 0; j < pixelLightCount; ++j) {
        Light aLight = GetAdditionalLight(j, WorldPos, half4(1, 1, 1, 1));
        float3 attenuatedLightColor = aLight.color * (aLight.distanceAttenuation * aLight.shadowAttenuation);
        half dExtra = saturate(dot(Normal, aLight.direction) * 0.5 + 0.5);
        half toonRampExtra = smoothstep(ToonRampOffsetPoint, ToonRampOffsetPoint + ToonRampSmoothness, dExtra);
        extraLights += attenuatedLightColor * toonRampExtra;
    }

    // Combine main light (with its own shadow attenuation) with extra lights and ambient term
    toonRampMain *= light.shadowAttenuation;
    ToonRampOutput = light.color * (toonRampMain + ToonRampTinting) + Ambient;
    ToonRampOutput += extraLights;

#if MAIN_LIGHT
    Direction = normalize(light.direction);
#else
    Direction = float3(0.5, 0.5, 0);
#endif

#endif
}
