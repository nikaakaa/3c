Shader "Hidden/3C/CharacterShapeProjectionComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "CharacterShapeProjectionComposite"
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ShapeLoop
            {
                int RegionIndex;
                int PointStart;
                int PointCount;
            };

            struct ShapeCompositeRegion
            {
                float4 ScreenRect;
                float4 AtlasRect;
                float4 Color;
                float4 LoopRange;
            };

            StructuredBuffer<float2> _ShapePoints;
            StructuredBuffer<uint> _ShapeAnchorFlags;
            StructuredBuffer<ShapeLoop> _ShapeLoops;
            StructuredBuffer<ShapeCompositeRegion> _ShapeCompositeRegions;
            Texture2D<float> _ShapeDepthAtlas;
            Texture2D<float> _ShapeMaskAtlas;
            float4 _ShapeScreenSize;
            float4 _ShapeAtlasSize;
            float4 _ShapeOutlineColor;
            float _ShapeOutlineWidth;
            int _ShapeDebugView;

            struct Varyings
            {
                float4 Position : SV_POSITION;
                nointerpolation uint RegionIndex : TEXCOORD0;
                float2 ScreenPixel : TEXCOORD1;
                float2 AtlasPixel : TEXCOORD2;
            };

            struct FragmentOutput
            {
                float4 Color : SV_Target;
                float Depth : SV_Depth;
            };

            float2 QuadCorner(uint vertexId)
            {
                const float2 corners[6] =
                {
                    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
                    float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
                };
                return corners[vertexId];
            }

            Varyings Vert(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
            {
                ShapeCompositeRegion region = _ShapeCompositeRegions[instanceId];
                float2 corner = QuadCorner(vertexId);
                float2 sourcePixel = region.ScreenRect.xy + corner * region.ScreenRect.zw;
                float2 atlasPixel = region.AtlasRect.xy + corner * region.AtlasRect.zw;
                float2 displayPixel = sourcePixel;
                if (_ShapeDebugView == 2)
                    displayPixel = atlasPixel * _ShapeAtlasSize.zw * _ShapeScreenSize.xy;
                float2 uv = displayPixel * _ShapeScreenSize.zw;
                Varyings output;
                output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.RegionIndex = instanceId;
                output.ScreenPixel = sourcePixel;
                output.AtlasPixel = atlasPixel;
                return output;
            }

            float DistanceToSegment(float2 samplePosition, float2 a, float2 b)
            {
                float2 segment = b - a;
                float denominator = dot(segment, segment);
                float t = denominator > 0.000001 ? saturate(dot(samplePosition - a, segment) / denominator) : 0.0;
                return distance(samplePosition, a + segment * t);
            }

            FragmentOutput Frag(Varyings input)
            {
                ShapeCompositeRegion region = _ShapeCompositeRegions[input.RegionIndex];
                int2 atlasPixel = int2(input.AtlasPixel);
                if (_ShapeDebugView == 1)
                {
                    float2 local = input.ScreenPixel - region.ScreenRect.xy;
                    float edge = min(min(local.x, local.y), min(region.ScreenRect.z - local.x, region.ScreenRect.w - local.y));
                    if (edge > 1.5)
                        discard;
                    FragmentOutput boundsOutput;
                    boundsOutput.Color = float4(0.0, 1.0, 1.0, 1.0);
#if UNITY_REVERSED_Z
                    boundsOutput.Depth = 1.0;
#else
                    boundsOutput.Depth = 0.0;
#endif
                    return boundsOutput;
                }
                if (_ShapeDebugView == 2)
                {
                    float mask = _ShapeMaskAtlas.Load(int3(atlasPixel, 0));
                    FragmentOutput maskOutput;
                    maskOutput.Color = mask > 0.5 ? float4(1.0, 1.0, 1.0, 1.0) : float4(0.02, 0.04, 0.08, 1.0);
#if UNITY_REVERSED_Z
                    maskOutput.Depth = 1.0;
#else
                    maskOutput.Depth = 0.0;
#endif
                    return maskOutput;
                }

                int loopStart = (int)region.LoopRange.x;
                int loopCount = (int)region.LoopRange.y;
                bool inside = false;
                float outlineDistance = 1000000.0;
                float anchorDistance = 1000000.0;
                for (int loopOffset = 0; loopOffset < loopCount; loopOffset++)
                {
                    ShapeLoop loop = _ShapeLoops[loopStart + loopOffset];
                    bool loopInside = false;
                    for (int pointOffset = 0; pointOffset < loop.PointCount; pointOffset++)
                    {
                        float2 a = _ShapePoints[loop.PointStart + pointOffset];
                        float2 b = _ShapePoints[loop.PointStart + (pointOffset + 1) % loop.PointCount];
                        if (_ShapeAnchorFlags[loop.PointStart + pointOffset] != 0)
                            anchorDistance = min(anchorDistance, distance(input.ScreenPixel, a));
                        bool crosses = (a.y > input.ScreenPixel.y) != (b.y > input.ScreenPixel.y);
                        if (crosses)
                        {
                            float x = (b.x - a.x) * (input.ScreenPixel.y - a.y) / (b.y - a.y) + a.x;
                            if (input.ScreenPixel.x < x)
                                loopInside = !loopInside;
                        }
                        outlineDistance = min(outlineDistance, DistanceToSegment(input.ScreenPixel, a, b));
                    }
                    if (loopInside)
                        inside = !inside;
                }
                float depth = _ShapeDepthAtlas.Load(int3(atlasPixel, 0));
                if (depth < 0.0 || depth > 1.0)
                    discard;

                FragmentOutput output;
                if (_ShapeDebugView == 3)
                {
                    if (anchorDistance <= 3.0)
                        output.Color = float4(1.0, 0.0, 1.0, 1.0);
                    else if (outlineDistance <= 1.0)
                        output.Color = float4(0.05, 0.05, 0.05, 1.0);
                    else
                        discard;
                }
                else if (_ShapeDebugView == 4)
                {
                    if (outlineDistance > max(1.0, _ShapeOutlineWidth))
                        discard;
                    output.Color = float4(1.0, 0.55, 0.0, 1.0);
                }
                else
                {
                    if (!inside)
                        discard;
                    if (_ShapeDebugView == 5)
                    {
#if UNITY_REVERSED_Z
                        float linearized = depth;
#else
                        float linearized = 1.0 - depth;
#endif
                        output.Color = float4(linearized.xxx, 1.0);
                    }
                    else
                        output.Color = outlineDistance <= _ShapeOutlineWidth ? _ShapeOutlineColor : region.Color;
                }
                output.Depth = depth;
                return output;
            }
            ENDHLSL
        }
    }
}
