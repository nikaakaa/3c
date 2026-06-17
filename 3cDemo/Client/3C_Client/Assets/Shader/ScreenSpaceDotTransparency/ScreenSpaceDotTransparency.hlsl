#ifndef SCREEN_SPACE_DOT_TRANSPARENCY_INCLUDED
#define SCREEN_SPACE_DOT_TRANSPARENCY_INCLUDED

float ScreenDotTransparencyOpaqueMask(float4 positionCS)
{
    float enabled = step(0.5, _ScreenDotTransparencyEnabled);
    float coverage = saturate(_ScreenDotCoverage) * enabled;
    if (coverage <= 0.0001)
        return 1.0;

    float spacing = max(_ScreenDotSpacingPixels, 1.0);
    float2 pixelPosition = positionCS.xy + _ScreenDotOffsetPixels.xy;
    float2 cellPosition = frac(pixelPosition / spacing) - 0.5;
    float dotRadius = saturate(_ScreenDotRadius) * coverage * 0.70710678;
    float edgeWidth = lerp(0.08, 0.001, saturate(_ScreenDotHardness));
    return smoothstep(dotRadius, dotRadius + edgeWidth, length(cellPosition));
}

void ApplyScreenDotTransparencyClip(float4 positionCS)
{
    clip(ScreenDotTransparencyOpaqueMask(positionCS) - 0.5);
}

#endif
