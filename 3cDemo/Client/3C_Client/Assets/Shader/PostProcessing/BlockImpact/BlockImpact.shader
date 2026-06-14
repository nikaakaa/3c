Shader "Hidden/3C/PostProcessing/BlockImpact"
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
            #pragma fragment FragBlockImpact
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _BlockImpactParams;
            float4 _BlockImpactEffectParams;
            float4 _BlockImpactStreakParams;
            float4 _BlockImpactStreakColor;
            int _BlockImpactSampleCount;

            half4 FragBlockImpact(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 center = _BlockImpactParams.xy;
                float pulse = saturate(_BlockImpactParams.z);
                float radius = max(_BlockImpactParams.w, 0.0001);
                float flashIntensity = _BlockImpactEffectParams.x;
                float radialStrength = _BlockImpactEffectParams.y;
                float streakIntensity = _BlockImpactEffectParams.z;
                float chromaticStrength = _BlockImpactEffectParams.w;
                float streakLength = max(_BlockImpactStreakParams.x, 0.0001);
                float streakThickness = max(_BlockImpactStreakParams.y, 0.0001);
                float streakSoftness = saturate(_BlockImpactStreakParams.z);
                int sampleCount = clamp(_BlockImpactSampleCount, 2, 16);

                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 radial = uv - center;
                float distanceToCenter = length(radial);
                float radiusMask = saturate(1.0 - distanceToCenter / radius);
                float2 direction = distanceToCenter > 0.0001 ? radial / distanceToCenter : float2(0.0, 0.0);
                float2 stepUv = direction * radialStrength * pulse * radiusMask * 0.035;

                half4 radialSum = source;
                [unroll(16)]
                for (int i = 1; i < 16; i++)
                {
                    if (i >= sampleCount)
                        break;

                    float sampleWeight = i / max(sampleCount - 1.0, 1.0);
                    float2 sampleUv = saturate(uv - stepUv * sampleWeight);
                    radialSum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv);
                }

                half4 radialColor = radialSum / sampleCount;
                half4 color = lerp(source, radialColor, saturate(radialStrength * pulse * radiusMask));

                float2 streakUv = uv - center;
                float vertical = exp(-pow(abs(streakUv.y) / streakThickness, 2.0));
                float horizontalPower = lerp(1.15, 2.8, streakSoftness);
                float horizontal = exp(-pow(abs(streakUv.x) / streakLength, horizontalPower));
                float coreLine = exp(-dot(streakUv, streakUv) / max(radius * radius * 0.045, 0.0001));
                float streakMask = saturate(vertical * horizontal + coreLine * 0.32);
                color.rgb += _BlockImpactStreakColor.rgb * streakMask * streakIntensity * pulse * _BlockImpactStreakColor.a;

                float flash = radiusMask * radiusMask * flashIntensity * pulse;
                color.rgb += half3(1.0, 0.82, 0.55) * flash;

                float chromaticOffset = chromaticStrength * pulse * radiusMask;
                if (chromaticOffset > 0.00001)
                {
                    half red = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv + direction * chromaticOffset)).r;
                    half blue = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv - direction * chromaticOffset)).b;
                    color.r = lerp(color.r, red, pulse);
                    color.b = lerp(color.b, blue, pulse);
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
