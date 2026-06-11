Shader "Hidden/3C/PostProcessing/RadialBlur"
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
            #pragma fragment FragRadialBlur
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _RadialBlurParams;
            int _RadialBlurSampleCount;

            half4 FragRadialBlur(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 center = _RadialBlurParams.xy;
                float intensity = saturate(_RadialBlurParams.z);
                float radius = max(_RadialBlurParams.w, 0.0001);
                int sampleCount = clamp(_RadialBlurSampleCount, 2, 16);
                float2 radial = uv - center;
                float distanceWeight = saturate(length(radial) / radius);
                float2 stepUv = radial * intensity * distanceWeight / sampleCount;

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 sum = color;

                [unroll(16)]
                for (int i = 1; i < 16; i++)
                {
                    if (i >= sampleCount)
                        break;

                    float sampleOffset = i - (sampleCount - 1) * 0.5;
                    float2 sampleUv = saturate(uv + stepUv * sampleOffset);
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv);
                }

                half4 blurred = sum / sampleCount;
                return lerp(color, blurred, intensity);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
