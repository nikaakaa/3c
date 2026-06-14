Shader "Hidden/3C/PostProcessing/Glitch"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment FragGlitch
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _GlitchParams;
            float4 _GlitchSecondaryParams;
            float4 _GlitchMaskParams;
            float4 _GlitchSlashParams;
            float4 _GlitchModeParams;
            float _GlitchTime;
            TEXTURE2D_X(_GlitchMaskTexture);
            SAMPLER(sampler_GlitchMaskTexture);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 FragGlitch(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float intensity = saturate(_GlitchParams.x);
                float blockSize = max(_GlitchParams.y, 1.0);
                float horizontalJitter = _GlitchParams.z;
                float rgbSplit = _GlitchParams.w;
                float scanLineIntensity = saturate(_GlitchSecondaryParams.x);
                float speed = _GlitchSecondaryParams.y;
                float useTargetMask = saturate(_GlitchSecondaryParams.z);
                float maskInfluence = saturate(_GlitchSecondaryParams.w);
                float maskExpansion = _GlitchMaskParams.x;
                float glitchMode = _GlitchModeParams.x;
                float slashBlend = saturate(_GlitchModeParams.y);
                float timeStep = floor(_GlitchTime * speed);

                float2 blockUv = floor(uv * blockSize);
                float blockNoise = Hash21(float2(blockUv.y, timeStep));
                float rowNoise = Hash21(float2(floor(uv.y * blockSize * 3.0), timeStep * 1.37));
                float glitchMask = lerp(0.25, 1.0, step(0.5, blockNoise)) * intensity;
                float rowMask = step(0.45, rowNoise) * intensity;
                float horizontalOffset = ((blockNoise - 0.5) * horizontalJitter * glitchMask * 2.5) + ((rowNoise - 0.5) * horizontalJitter * rowMask);
                float maskOffset = horizontalOffset + (blockNoise - 0.5) * maskExpansion * intensity;

                float scanLine = sin((uv.y + _GlitchTime * speed * 0.005) * 900.0);
                float scanDarken = 1.0 - saturate(scanLine * 0.5 + 0.5) * scanLineIntensity * intensity * 0.25;

                float2 distortedUv = saturate(uv + float2(horizontalOffset, 0.0));
                float rawMask = SAMPLE_TEXTURE2D_X(_GlitchMaskTexture, sampler_GlitchMaskTexture, uv).r;
                float shiftedMask = SAMPLE_TEXTURE2D_X(_GlitchMaskTexture, sampler_GlitchMaskTexture, saturate(uv + float2(maskOffset, 0.0))).r;
                float splitMask = SAMPLE_TEXTURE2D_X(_GlitchMaskTexture, sampler_GlitchMaskTexture, saturate(uv + float2(rgbSplit * intensity, 0.0))).r;
                float targetMask = max(rawMask, max(shiftedMask, splitMask));
                float finalInfluence = lerp(1.0, saturate(targetMask * maskInfluence), useTargetMask);

                float split = rgbSplit * (glitchMask + rowMask + intensity);
                half red = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(distortedUv + float2(split, 0.0))).r;
                half green = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUv).g;
                half blue = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(distortedUv - float2(split, 0.0))).b;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 digitalGlitch = half4(red, green, blue, source.a);
                digitalGlitch.rgb *= scanDarken;

                float noiseFlash = (Hash21(blockUv + timeStep) - 0.5) * 0.2 * glitchMask;
                digitalGlitch.rgb += noiseFlash;

                float slashModeWeight = step(0.5, glitchMode) * slashBlend;
                half4 glitched = digitalGlitch;
                if (slashModeWeight > 0.0)
                {
                    float slashSliceDensity = max(_GlitchSlashParams.x, 1.0);
                    float slashSmearWidth = _GlitchSlashParams.y;
                    float slashHighlightStretch = _GlitchSlashParams.z;
                    float slashDirection = _GlitchSlashParams.w >= 0.0 ? 1.0 : -1.0;
                    float slashLine = floor(uv.y * slashSliceDensity);
                    float slashNoise = Hash21(float2(slashLine, timeStep * 1.91));
                    float slashGate = step(0.28, slashNoise);
                    float slashOffset = (slashNoise * 2.0 - 1.0) * slashSmearWidth * intensity * slashGate * slashDirection;
                    float2 slashUv = saturate(uv + float2(slashOffset, 0.0));
                    float smearStep = slashSmearWidth * intensity * slashDirection;

                    half4 smearA = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, slashUv);
                    half4 smearB = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(slashUv + float2(smearStep * 0.5, 0.0)));
                    half4 smearC = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(slashUv + float2(smearStep, 0.0)));
                    half4 smearD = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(slashUv + float2(smearStep * 1.75, 0.0)));
                    half4 slashTear = smearA * 0.42 + smearB * 0.25 + smearC * 0.2 + smearD * 0.13;
                    float highlight = saturate(max(max(slashTear.r, slashTear.g), slashTear.b) - 0.62);
                    slashTear.rgb += highlight * slashHighlightStretch * slashGate;
                    slashTear.rgb *= scanDarken;
                    slashTear.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(slashUv + float2(rgbSplit * intensity, 0.0))).r;
                    slashTear.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(slashUv - float2(rgbSplit * intensity, 0.0))).b;
                    slashTear.a = source.a;
                    glitched = lerp(digitalGlitch, slashTear, slashModeWeight);
                }
                return lerp(source, glitched, intensity * finalInfluence);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
