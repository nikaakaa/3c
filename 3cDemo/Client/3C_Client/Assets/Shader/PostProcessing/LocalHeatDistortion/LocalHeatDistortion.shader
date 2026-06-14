Shader "Hidden/3C/PostProcessing/LocalHeatDistortion"
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
            #pragma fragment FragLocalHeatDistortion
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _LocalHeatDistortionParams;
            float4 _LocalHeatDistortionAreaParams;
            float4 _LocalHeatDistortionAreaExtraParams;
            float4 _LocalHeatDistortionAreaDepthParams;
            float4 _LocalHeatDistortionDebugParams;
            int _LocalHeatDistortionMode;
            float _LocalHeatDistortionTime;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float2 Rotate(float2 p, float sinAngle, float cosAngle)
            {
                return float2(
                    p.x * cosAngle - p.y * sinAngle,
                    p.x * sinAngle + p.y * cosAngle);
            }

            half4 FragLocalHeatDistortion(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float intensity = saturate(_LocalHeatDistortionParams.x);
                float speed = _LocalHeatDistortionParams.y;
                float noiseScale = max(_LocalHeatDistortionParams.z, 0.0001);
                float distortionStrength = _LocalHeatDistortionParams.w;

                float2 center = _LocalHeatDistortionAreaParams.xy;
                float radius = max(_LocalHeatDistortionAreaParams.z, 0.0001);
                float softness = saturate(_LocalHeatDistortionAreaParams.w);
                float aspect = max(_LocalHeatDistortionAreaExtraParams.x, 0.0001);
                float sinAngle = _LocalHeatDistortionAreaExtraParams.y;
                float cosAngle = _LocalHeatDistortionAreaExtraParams.z;
                float shape = _LocalHeatDistortionAreaExtraParams.w;
                float sourceDepth = max(_LocalHeatDistortionAreaDepthParams.x, 0.0001);
                float depthFade = max(_LocalHeatDistortionAreaDepthParams.y, 0.0001);
                float previewDebug = saturate(_LocalHeatDistortionDebugParams.x);

                float2 local = Rotate(uv - center, -sinAngle, cosAngle);
                local.x /= aspect;
                float normalizedDistance = length(local) / radius;
                float edge = max(softness, 0.0001);
                float areaMask = 1.0 - smoothstep(1.0 - edge, 1.0, normalizedDistance);
                half4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (areaMask <= 0.0001)
                    return original;

                float sceneDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float depthMask = smoothstep(0.0, depthFade, sceneDepth - sourceDepth);
                areaMask *= depthMask;
                if (areaMask <= 0.0001)
                    return original;

                if (intensity <= 0.0001)
                {
                    half4 debugOnlyColor = half4(0.1, 0.75, 1.0, original.a);
                    return lerp(original, debugOnlyColor, areaMask * previewDebug * 0.45);
                }

                float time = _LocalHeatDistortionTime * speed;
                float2 flowUv = local * noiseScale;
                float noiseA = ValueNoise(flowUv + float2(0.0, time * 0.08));
                float noiseB = ValueNoise(flowUv * 1.7 + float2(time * 0.05, -time * 0.04));
                float2 noiseVector = float2(noiseA - 0.5, noiseB - 0.5);

                float2 direction = normalize(local + 0.0001);
                float2 tangent = float2(-direction.y, direction.x);
                float ring = sin((normalizedDistance - frac(time * 0.08)) * 28.0);
                float verticalBand = sin((local.y * noiseScale * 1.7 + time * 0.18) * 6.2831853);

                float2 offset = noiseVector;
                if (_LocalHeatDistortionMode == 1)
                    offset = tangent * (noiseA * 2.0 - 1.0) + direction * 0.2 * (noiseB - 0.5);
                else if (_LocalHeatDistortionMode == 2)
                    offset = direction * ring + noiseVector * 0.35;
                else if (_LocalHeatDistortionMode == 3)
                    offset = float2(noiseA - 0.5, verticalBand * 0.7 + noiseB * 0.3);

                float cylinderBoost = lerp(1.0, saturate(1.25 - abs(local.x / max(radius * aspect, 0.0001))), step(0.5, shape));
                float2 distortedUv = saturate(uv + offset * distortionStrength * intensity * areaMask * cylinderBoost);
                half4 distorted = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUv);
                half4 result = lerp(original, distorted, areaMask * intensity);
                half4 debugColor = half4(0.1, 0.75, 1.0, result.a);
                return lerp(result, debugColor, areaMask * previewDebug * 0.45);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
