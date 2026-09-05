#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
struct ZZZFullscreenOutput
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};
ZZZFullscreenOutput ZZZFullscreenVertex(uint vertexID : SV_VertexID)
{
    ZZZFullscreenOutput result;
    result.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    result.uv = GetFullScreenTriangleTexCoord(vertexID);
    return result;
}
