Shader "Hidden/3C/PostProcessing/EdgeScan"
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
            #pragma fragment FragEdgeScan
            #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _EdgeScanOriginRadius;
            float4 _EdgeScanParams;
            float4 _EdgeScanEdgeParams;
            float4 _EdgeScanColor;
            float4 _EdgeScanDirectionArc;
            float4 _EdgeScanLineParams;
            float4 _EdgeScanToneParams;

            float RawDepthAt(float2 uv)
            {
                return SampleSceneDepth(saturate(uv));
            }

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(RawDepthAt(uv), _ZBufferParams);
            }

            float DeviceDepth(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                return rawDepth;
                #else
                return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif
            }

            float3 WorldPositionAt(float2 uv)
            {
                return ComputeWorldSpacePosition(uv, DeviceDepth(RawDepthAt(uv)), UNITY_MATRIX_I_VP);
            }

            float NormalEdgeAt(float3 centerNormal, float2 uv, float2 offset)
            {
                float3 normalA = normalize(SampleSceneNormals(saturate(uv + offset)));
                float3 normalB = normalize(SampleSceneNormals(saturate(uv - offset)));
                float normalDelta = max(length(centerNormal - normalA), length(centerNormal - normalB));
                return smoothstep(_EdgeScanEdgeParams.y, _EdgeScanEdgeParams.y * 2.0, normalDelta);
            }

            float DepthEdgeAt(float centerDepth, float2 uv, float2 offset)
            {
                float depthA = LinearDepthAt(uv + offset);
                float depthB = LinearDepthAt(uv - offset);
                float depthDelta = max(abs(centerDepth - depthA), abs(centerDepth - depthB));
                float scaledDelta = depthDelta / max(centerDepth * 0.02, 0.0001);
                return smoothstep(_EdgeScanEdgeParams.x, _EdgeScanEdgeParams.x * 2.0, scaledDelta);
            }

            float ScanArcMask(float3 worldPosition, float3 origin, out float horizontalDistance)
            {
                float2 originToPixel = worldPosition.xz - origin.xz;
                horizontalDistance = length(originToPixel);

                if (horizontalDistance <= 0.0001)
                    return 1.0;

                float2 toPixel = originToPixel / horizontalDistance;
                float2 scanDirection = _EdgeScanDirectionArc.xz;
                scanDirection = dot(scanDirection, scanDirection) > 0.0001 ? normalize(scanDirection) : float2(0.0, 1.0);
                float halfArcCos = _EdgeScanDirectionArc.w;

                if (halfArcCos <= -0.999)
                    return 1.0;

                float angleDot = dot(toPixel, scanDirection);
                return smoothstep(halfArcCos - 0.035, halfArcCos + 0.035, angleDot);
            }

            float ScanLineMask(float horizontalDistance)
            {
                float spacing = max(_EdgeScanLineParams.x, 0.0001);
                float lineWidth = min(_EdgeScanLineParams.y, spacing * 0.45);
                float distanceToLine = abs(frac(horizontalDistance / spacing) - 0.5) * spacing;
                float antiAlias = max(fwidth(horizontalDistance) * 1.5, 0.001);
                return 1.0 - smoothstep(lineWidth, lineWidth + antiAlias, distanceToLine);
            }

            half4 FragEdgeScan(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float rawDepth = RawDepthAt(uv);

                #if UNITY_REVERSED_Z
                if (rawDepth <= 0.000001)
                    return source;
                #else
                if (rawDepth >= 0.999999)
                    return source;
                #endif

                float intensity = _EdgeScanParams.x;
                float scanWidth = max(_EdgeScanParams.y, 0.0001);
                float edgeStrength = _EdgeScanParams.z;
                float distanceFade = _EdgeScanParams.w;
                float scanRadius = _EdgeScanOriginRadius.w;
                float3 origin = _EdgeScanOriginRadius.xyz;
                float3 worldPosition = WorldPositionAt(uv);
                float horizontalDistance;
                float arcMask = ScanArcMask(worldPosition, origin, horizontalDistance);
                float distanceToOrigin = distance(worldPosition, origin);
                float insideScan = 1.0 - smoothstep(scanRadius, scanRadius + scanWidth, horizontalDistance);
                float outerRange = distanceFade > 0.0001 ? 1.0 - smoothstep(distanceFade * 0.92, distanceFade, distanceToOrigin) : 1.0;
                float bodyMask = arcMask * insideScan * outerRange;
                float frontDistance = abs(horizontalDistance - scanRadius);
                float frontMask = arcMask * outerRange * (1.0 - smoothstep(scanWidth * 0.15, scanWidth, frontDistance));

                if (max(bodyMask, frontMask) <= 0.0001 || intensity <= 0.0001)
                    return source;

                float2 texel = 1.0 / _ScaledScreenParams.xy;
                float centerDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 centerNormal = normalize(SampleSceneNormals(uv));
                float depthEdge = max(DepthEdgeAt(centerDepth, uv, float2(texel.x, 0.0)), DepthEdgeAt(centerDepth, uv, float2(0.0, texel.y)));
                float normalEdge = max(NormalEdgeAt(centerNormal, uv, float2(texel.x, 0.0)), NormalEdgeAt(centerNormal, uv, float2(0.0, texel.y)));
                float edge = saturate(max(depthEdge, normalEdge) * edgeStrength);
                float lineMask = ScanLineMask(horizontalDistance) * bodyMask;
                float trailStart = max(scanRadius - scanWidth * 8.0, 0.0);
                float trailEnd = max(scanRadius, trailStart + 0.0001);
                float trailingFrontMask = bodyMask * smoothstep(trailStart, trailEnd, horizontalDistance);
                float lineHighlight = lineMask * _EdgeScanLineParams.z;
                float edgeHighlight = edge * bodyMask;
                float frontHighlight = frontMask * _EdgeScanLineParams.w;
                float darkenMask = trailingFrontMask * _EdgeScanToneParams.x;
                float highlight = saturate((lineHighlight * 0.42 + edgeHighlight + frontHighlight * 0.7) * intensity);
                half3 scanColor = _EdgeScanColor.rgb * max(_EdgeScanColor.a, 0.0);
                half3 frontColor = lerp(scanColor, half3(1.0, 1.0, 1.0), 0.72);
                half3 result = source.rgb * (1.0 - darkenMask * 0.28);
                result += scanColor * saturate((lineHighlight * 0.55 + edgeHighlight) * intensity);
                result += frontColor * saturate(frontHighlight * intensity * 0.72);
                result += scanColor * highlight * 0.12;
                return half4(result, source.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
