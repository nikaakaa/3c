#if defined(SHADER_STAGE_VERTEX)
#define main ZZZOriginalDepthVertex
#define ShaderOutput ZZZOriginalDepthVertexOutput
#include "OriginalStage4020.hlsl"
#undef ShaderOutput
#undef main
struct ZZZDepthNormalsVertexOutput
{
    ZZZOriginalDepthVertexOutput original;
    float3 normalWS : TEXCOORD15;
};
ZZZDepthNormalsVertexOutput ZZZDepthNormalsVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float2 input4 : TEXCOORD0, float2 input5 : TEXCOORD1, float2 input6 : TEXCOORD2, float2 input7 : TEXCOORD3, float3 input8 : TEXCOORD4)
{
    ZZZDepthNormalsVertexOutput result;
    result.original = ZZZOriginalDepthVertex(input0, input1, input2, input3, input4, input5, input6, input7, input8);
    result.normalWS = normalize(mul(input1, (float3x3)unity_WorldToObject));
    return result;
}
#elif defined(SHADER_STAGE_FRAGMENT)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#define main ZZZOriginalDepthFragment
#define ShaderOutput ZZZOriginalDepthFragmentOutput
#include "OriginalStage4024.hlsl"
#undef ShaderOutput
#undef main
float4 ZZZDepthNormalsFragment(float3 normalWS : TEXCOORD15, float4 input0 : TEXCOORD0, float4 input1 : TEXCOORD1, float4 input2 : TEXCOORD2, float4 input3 : TEXCOORD3, float4 input4 : TEXCOORD4, float4 input5 : TEXCOORD5, float4 input6 : TEXCOORD6, float4 input7 : TEXCOORD7, float3 input8 : TEXCOORD8, float4 input9 : SV_POSITION0, bool input10 : SV_IsFrontFace0) : SV_Target0
{
    ZZZOriginalDepthFragment(input0, input1, input2, input3, input4, input5, input6, input7, input8, input9, input10);
    float3 normal = normalize(normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    return float4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5)), 0);
#else
    return float4(normal, 0);
#endif
}
#endif
