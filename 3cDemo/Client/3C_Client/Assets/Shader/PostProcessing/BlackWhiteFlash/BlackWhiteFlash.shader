Shader "Hidden/3C/PostProcessing/BlackWhiteFlash"
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
            #pragma fragment FragBlackWhiteFlash
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _BlackWhiteFlashToneParams;
            float4 _BlackWhiteFlashRangeParams;
            float4 _BlackWhiteFlashStyleParams;

            half4 FragBlackWhiteFlash(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float intensity = saturate(_BlackWhiteFlashToneParams.x);
                float threshold = saturate(_BlackWhiteFlashToneParams.y);
                float contrast = max(_BlackWhiteFlashToneParams.z, 0.0001);
                float invertAmount = saturate(_BlackWhiteFlashToneParams.w);
                float whiteBoost = max(_BlackWhiteFlashStyleParams.x, 0.0);
                float blackCrush = saturate(_BlackWhiteFlashStyleParams.y);
                float radialMode = step(0.5, _BlackWhiteFlashStyleParams.z);

                float luma = dot(source.rgb, half3(0.2126, 0.7152, 0.0722));
                float contrastLuma = saturate((luma - threshold) * contrast + 0.5);
                float whiteBand = step(0.5, contrastLuma);
                float blackLevel = contrastLuma * (1.0 - blackCrush);
                float bwValue = lerp(blackLevel, whiteBoost, whiteBand);
                half3 flashColor = half3(bwValue, bwValue, bwValue);
                flashColor = lerp(flashColor, 1.0 - saturate(flashColor), invertAmount);

                float2 center = _BlackWhiteFlashRangeParams.xy;
                float radius = max(_BlackWhiteFlashRangeParams.z, 0.0001);
                float softness = max(_BlackWhiteFlashRangeParams.w, 0.0001);
                float radialMask = 1.0 - smoothstep(radius, radius + softness, length(uv - center));
                float mask = lerp(1.0, radialMask, radialMode);

                source.rgb = lerp(source.rgb, flashColor, intensity * mask);
                return source;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
