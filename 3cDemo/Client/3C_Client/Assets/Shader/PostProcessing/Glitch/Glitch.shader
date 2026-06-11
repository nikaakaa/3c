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
                half4 glitched = half4(red, green, blue, source.a);
                glitched.rgb *= scanDarken;

                float noiseFlash = (Hash21(blockUv + timeStep) - 0.5) * 0.2 * glitchMask;
                glitched.rgb += noiseFlash;

                return lerp(source, glitched, intensity * finalInfluence);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
