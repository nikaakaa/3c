Shader "miHoYo/Particles/CustomEnvLit"
{
    Properties
    {
        _BaseColor ("BaseColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _CustomIBLDiffuse ("ShadowColor", Color) = (0.2, 0.2, 0.2, 1.0)
        _UsingNonPSR ("是否用于非粒子系统", Float) = 1.0
        _Cull ("Cull@背面剔除模式", Float) = 2.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Pass
        {
            Name "GeometryPass"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 unity_MatrixVP : packoffset(c102);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
    column_major float4x4 unity_WorldToObject : packoffset(c4);
    float4 unity_WorldTransformParams : packoffset(c9);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _BaseMap_ST : packoffset(c4);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float3 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float3 output4 : TEXCOORD4;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float2 input3 : TEXCOORD0, float2 input4 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, o2, o3, o4, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
    v4.xy = asuint(input4);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o4.xyz = r0.xyz;
    o0.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    o1.xy = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_BaseMap_ST.x, _BaseMap_ST.y)))), asfloat(asuint((float2(_BaseMap_ST.z, _BaseMap_ST.w))))));
    r0.x = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r0.y = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r0.z = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    o2.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(v2.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v2.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v2.zzz), asfloat(r0.xyz)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    o3.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o3.w = asuint((asfloat(v2.w) * asfloat(asuint((unity_WorldTransformParams.w)))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    result.output2 = asfloat(o2.xyz);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyz);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    column_major float4x4 _SceneWeatherParamsPart2 : packoffset(c35);
    column_major float4x4 _SceneWeatherParamsPart3 : packoffset(c39);
    column_major float4x4 _SceneWeatherParamsPart6 : packoffset(c51);
    column_major float4x4 _SceneWeatherParamsPart7 : packoffset(c55);
    float4 _SceneSampleTextureBias : packoffset(c167);
    float4 _CustomIBLDiffuse : packoffset(c169);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _BaseColor : packoffset(c5);
    float4 _EmissionColor : packoffset(c6);
    float _Smoothness : packoffset(c11.z);
    float _BumpScale : packoffset(c11.w);
    float _WetnessNormalAffection : packoffset(c12);
}


SamplerState sampler_WetnessNoise;
SamplerState sampler_SceneHeightMap;
SamplerComparisonState sampler_SceneWaterPoolMap;
SamplerState sampler_BaseMap;
SamplerState sampler_BumpMap;
SamplerState sampler_MetallicGlossMap;
Texture2D<float4> _WetnessNoise : register(t0);
Texture2D<float4> _SceneHeightMap : register(t1);
Texture2D<float4> _SceneWaterPoolMap : register(t2);
Texture2D<float4> _BaseMap : register(t3);
Texture2D<float4> _BumpMap : register(t4);
Texture2D<float4> _MetallicGlossMap : register(t5);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
    float4 output2 : SV_Target2;
    float4 output3 : SV_Target3;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0, float3 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float3 input4 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, o1, o2, o3, v0, v1, v2, v3, v4;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    v2.xyz = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyz = asuint(input4);
    r0.xyzw = asuint(_BumpMap.SampleBias(sampler_BumpMap, asfloat(v1.xy), asfloat(asuint((_SceneSampleTextureBias.z)))).xyzw);
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.w)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.xy = asuint((asfloat(r0.xy) * asfloat(asuint((float2(_BumpScale, _BumpScale))))));
    r0.w = asuint(dot(asfloat(r0.xy), asfloat(r0.xy)));
    r0.w = asuint(min(asfloat(r0.w), asfloat(0x3f800000u)));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(sqrt(asfloat(r0.w)));
    r1.xyzw = asuint(_BaseMap.SampleBias(sampler_BaseMap, asfloat(v1.xy), asfloat(asuint((_SceneSampleTextureBias.y)))).xyzw);
    r2.xyzw = asuint(_MetallicGlossMap.SampleBias(sampler_MetallicGlossMap, asfloat(v1.xy), asfloat(asuint((_SceneSampleTextureBias.w)))).xyzw);
    r3.xyz = asuint((asfloat(r1.xyz) * asfloat(asuint((float3(_BaseColor.x, _BaseColor.y, _BaseColor.z))))));
    r0.w = asuint(saturate((asfloat((r2.y ^ 0x80000000u)) + asfloat(0x3f800000u))));
    r0.w = asuint(min(asfloat(r0.w), asfloat(0x3f7d70a4u)));
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_Smoothness)))));
    r2.xyw = asuint((asfloat(r3.xyz) * asfloat(asuint((float3(_EmissionColor.x, _EmissionColor.y, _EmissionColor.z))))));
    r4.xyz = asuint((asfloat(v2.zxy) * asfloat(v3.yzx)));
    r4.xyz = asuint(mad(asfloat(v2.yzx), asfloat(v3.zxy), asfloat((r4.xyz ^ 0x80000000u))));
    r4.xyz = asuint((asfloat(r4.xyz) * asfloat(v3.www)));
    r1.w = asuint(max(asfloat(r2.y), asfloat(r2.x)));
    r1.w = asuint(max(asfloat(r2.w), asfloat(r1.w)));
    r2.z = asuint((asfloat(r1.w) * asfloat(r2.z)));
    r3.w = ((asfloat(0x00000000u) < asfloat(r1.w)) ? 0xffffffffu : 0u);
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x38d1b717u)));
    r2.xyw = asuint((asfloat(r2.xyw) / asfloat(r1.www)));
    r2.xyw = (r2.xyw & r3.www);
    r1.w = asuint(saturate((asfloat(r2.z) * asfloat(asuint((_SceneWeatherParamsPart3[2][2]))))));
    r1.xyz = asuint(mad(asfloat((r1.xyz ^ 0x80000000u)), asfloat(asuint((float3(_BaseColor.x, _BaseColor.y, _BaseColor.z)))), asfloat(r2.xyw)));
    r1.xyz = asuint(mad(asfloat(r1.www), asfloat(r1.xyz), asfloat(r3.xyz)));
    r1.w = asuint(saturate(asfloat(asuint((_SceneWeatherParamsPart2[0][2])))));
    r2.x = ((asfloat(asuint((_SceneWeatherParamsPart2[1][3]))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    if (r2.x != 0u) {
        r2.x = ((asfloat(asuint((_SceneWeatherParamsPart6[1][2]))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
        if (r2.x != 0u) {
            r2.xyz = asuint(mad(asfloat(v2.xyz), asfloat(uint3(0x3ee66666u, 0x3ee66666u, 0x3ee66666u)), asfloat(v4.xyz)));
            r2.x = asuint(mad(asfloat(r2.x), asfloat(asuint((_SceneWeatherParamsPart1[3][0]))), asfloat(asuint((_SceneWeatherParamsPart1[3][2])))));
            r2.z = asuint(mad(asfloat(r2.z), asfloat(asuint((_SceneWeatherParamsPart1[3][1]))), asfloat(asuint((_SceneWeatherParamsPart1[3][3])))));
            r2.y = asuint((asfloat(r2.y) + asfloat(0x3d4ccccdu)));
            r2.y = asuint((asfloat(r2.y) + asfloat((asuint((_SceneWeatherParamsPart7[2][3])) ^ 0x80000000u))));
            r2.y = asuint((asfloat(r2.y) + asfloat(0x42c80000u)));
            r2.y = asuint((asfloat(r2.y) * asfloat(0x3b5a740eu)));
            r3.x = asuint((asfloat(r2.x) * asfloat(asuint((_SceneWeatherParamsPart1[0][2])))));
            r3.y = asuint((asfloat(r2.z) * asfloat(asuint((_SceneWeatherParamsPart1[0][3])))));
            r2.xz = asuint((asfloat(r3.xy) + asfloat(uint2(0x3f000000u, 0x3f000000u))));
            r2.xz = asuint(floor(asfloat(r2.xz)));
            r3.xy = asuint((asfloat((r2.xz ^ 0x80000000u)) + asfloat(r3.xy)));
            r5.xyzw = asuint((asfloat(r3.xxyy) + asfloat(uint4(0x3f000000u, 0x3f800000u, 0x3f000000u, 0x3f800000u))));
            r6.xw = asuint((asfloat(r5.xz) * asfloat(r5.xz)));
            r3.zw = asuint(mad(asfloat(r6.xw), asfloat(uint2(0x3f000000u, 0x3f000000u)), asfloat((r3.xy ^ 0x80000000u))));
            r5.xz = asuint((asfloat((r3.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
            r7.xy = asuint(min(asfloat(r3.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
            r5.xz = asuint(mad(asfloat((r7.xy ^ 0x80000000u)), asfloat(r7.xy), asfloat(r5.xz)));
            r3.xy = asuint(max(asfloat(r3.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
            r3.xy = asuint(mad(asfloat((r3.xy ^ 0x80000000u)), asfloat(r3.xy), asfloat(r5.yw)));
            r7.y = r5.x;
            r7.xz = r3.zx;
            r7.w = r6.x;
            r7.xyzw = asuint((asfloat(r7.xyzw) * asfloat(uint4(0x3ee38da4u, 0x3ee38da4u, 0x3ee38da4u, 0x3e638da4u))));
            r6.y = r5.z;
            r6.xz = r3.wy;
            r3.xyzw = asuint((asfloat(r6.xyzw) * asfloat(uint4(0x3ee38da4u, 0x3ee38da4u, 0x3ee38da4u, 0x3e638da4u))));
            r5.xyzw = asuint((asfloat(r7.ywyw) + asfloat(r7.xzxz)));
            r6.xyzw = asuint((asfloat(r3.yyww) + asfloat(r3.xxzz)));
            r3.xz = asuint((asfloat(r7.yw) / asfloat(r5.zw)));
            r3.yw = asuint((asfloat(r3.yw) / asfloat(r6.yw)));
            r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat(uint4(0xbfc00000u, 0xbfc00000u, 0x3f000000u, 0x3f000000u))));
            r7.xy = asuint((asfloat(r3.xz) * asfloat(asuint((float2(_SceneWeatherParamsPart1[0][0], _SceneWeatherParamsPart1[0][0]))))));
            r7.zw = asuint((asfloat(r3.yw) * asfloat(asuint((float2(_SceneWeatherParamsPart1[0][1], _SceneWeatherParamsPart1[0][1]))))));
            r3.x = asuint((_SceneWeatherParamsPart1[0][0]));
            r3.y = asuint((_SceneWeatherParamsPart1[0][1]));
            r8.xyzw = asuint(mad(asfloat(r2.xzxz), asfloat(r3.xyxy), asfloat(r7.xzyz)));
            r3.xyzw = asuint(mad(asfloat(r2.xzxz), asfloat(r3.xyxy), asfloat(r7.xwyw)));
            r5.xyzw = asuint((asfloat(r5.xyzw) * asfloat(r6.xyzw)));
            r2.x = asuint(_SceneHeightMap.SampleCmpLevelZero(sampler_SceneWaterPoolMap, asfloat(r8.xy), asfloat(r2.y)));
            r2.z = asuint(_SceneHeightMap.SampleCmpLevelZero(sampler_SceneWaterPoolMap, asfloat(r8.zw), asfloat(r2.y)));
            r2.z = asuint((asfloat(r2.z) * asfloat(r5.y)));
            r2.x = asuint(mad(asfloat(r5.x), asfloat(r2.x), asfloat(r2.z)));
            r2.z = asuint(_SceneHeightMap.SampleCmpLevelZero(sampler_SceneWaterPoolMap, asfloat(r3.xy), asfloat(r2.y)));
            r2.x = asuint(mad(asfloat(r5.z), asfloat(r2.z), asfloat(r2.x)));
            r2.y = asuint(_SceneHeightMap.SampleCmpLevelZero(sampler_SceneWaterPoolMap, asfloat(r3.zw), asfloat(r2.y)));
            r2.x = asuint(mad(asfloat(r5.w), asfloat(r2.y), asfloat(r2.x)));
            r2.y = asuint(saturate(asfloat(v2.y)));
            r2.z = asuint((asfloat((r2.y ^ 0x80000000u)) + asfloat(0x3f800000u)));
            r2.z = asuint(sqrt(asfloat(r2.z)));
            r2.w = asuint(mad(asfloat(r2.y), asfloat(0xbc996e30u), asfloat(0x3d981627u)));
            r2.w = asuint(mad(asfloat(r2.w), asfloat(r2.y), asfloat(0xbe593484u)));
            r2.y = asuint(mad(asfloat(r2.w), asfloat(r2.y), asfloat(0x3fc90da4u)));
            r2.y = asuint(mad(asfloat((r2.y ^ 0x80000000u)), asfloat(r2.z), asfloat(0x3fc90fdbu)));
            r2.y = asuint(mad(asfloat(r2.y), asfloat(0x40000000u), asfloat(0xbfc90fdbu)));
            uint4 sincosBits88 = r2.yyyy;
            r2.y = asuint(sin(asfloat(sincosBits88.y)));
            r2.y = asuint((asfloat(r2.y) + asfloat(0x3f800000u)));
            r2.x = asuint((asfloat(r2.x) * asfloat(r2.y)));
            r2.x = asuint((asfloat(r2.x) * asfloat(0x3f000000u)));
        } else {
            r2.x = 0x3f800000u;
        }
        r2.yz = asuint((asfloat(v4.xz) * asfloat(asuint((float2(_SceneWeatherParamsPart2[1][0], _SceneWeatherParamsPart2[1][0]))))));
        r3.xyzw = asuint(_WetnessNoise.Sample(sampler_SceneHeightMap, asfloat(r2.yz)).xyzw);
        r2.y = asuint(mad(asfloat(r3.x), asfloat(asuint((_SceneWeatherParamsPart2[1][1]))), asfloat(asuint((_SceneWeatherParamsPart2[1][2])))));
        r2.z = ((asfloat(asuint((_SceneWeatherParamsPart1[2][0]))) < asfloat(v4.y)) ? 0xffffffffu : 0u);
        r2.w = ((asfloat(v4.y) < asfloat(asuint((_SceneWeatherParamsPart1[2][1])))) ? 0xffffffffu : 0u);
        r2.z = (r2.w & r2.z);
        if (r2.z != 0u) {
            r3.x = asuint(mad(asfloat(v4.x), asfloat(asuint((_SceneWeatherParamsPart1[1][0]))), asfloat(asuint((_SceneWeatherParamsPart1[1][2])))));
            r3.y = asuint(mad(asfloat(v4.z), asfloat(asuint((_SceneWeatherParamsPart1[1][1]))), asfloat(asuint((_SceneWeatherParamsPart1[1][3])))));
            r3.xyzw = asuint(_SceneWaterPoolMap.Sample(sampler_WetnessNoise, asfloat(r3.xy)).xyzw);
            r2.z = asuint(dot(asfloat(r3.xyzw), asfloat(uint4(0x3e800000u, 0x3e800000u, 0x3e800000u, 0x3e800000u))));
        } else {
            r2.z = 0x00000000u;
        }
        r2.z = asuint(saturate(mad(asfloat(r2.z), asfloat(0x3f814afdu), asfloat(0xbc257eb5u))));
        r2.y = asuint(max(asfloat(r2.z), asfloat(r2.y)));
        r2.y = asuint(min(asfloat(r2.y), asfloat(0x3f800000u)));
        r2.y = asuint((asfloat(r1.w) * asfloat(r2.y)));
        r1.w = asuint(saturate((asfloat(r2.x) * asfloat(r2.y))));
    }
    r2.x = ((asfloat(0x3a83126fu) < asfloat(r1.w)) ? 0xffffffffu : 0u);
    r0.w = asuint(mad(asfloat(r0.w), asfloat(asuint((_SceneWeatherParamsPart2[0][1]))), asfloat(asuint((_SceneWeatherParamsPart2[0][0])))));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r2.yzw = asuint(mad(asfloat(r1.xyz), asfloat(r1.xyz), asfloat((r1.xyz ^ 0x80000000u))));
    r2.yzw = asuint(mad(asfloat(r0.www), asfloat(r2.yzw), asfloat(r1.xyz)));
    r0.w = ((asfloat(asuint((_SceneWeatherParamsPart6[1][2]))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r3.x = asuint((asfloat(r1.w) * asfloat(asuint((_WetnessNormalAffection)))));
    r1.w = ((r0.w != 0u) ? r3.x : r1.w);
    r1.w = asuint((asfloat(r1.w) + asfloat(0xbee66666u)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(0x40000000u))));
    r3.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(uint3(0x00000000u, 0x00000000u, 0x3f800000u))));
    r3.xyz = asuint(mad(asfloat(r1.www), asfloat(r3.xyz), asfloat(r0.xyz)));
    r3.xyz = ((r0.www != 0u) ? r3.xyz : r0.xyz);
    o1.xyz = ((r2.xxx != 0u) ? r2.yzw : r1.xyz);
    r0.xyz = ((r2.xxx != 0u) ? r3.xyz : r0.xyz);
    r1.xyz = asuint((asfloat(r4.xyz) * asfloat(r0.yyy)));
    r0.xyw = asuint(mad(asfloat(r0.xxx), asfloat(v3.xyz), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(r0.zzz), asfloat(v2.xyz), asfloat(r0.xyw)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o3.xyz = asuint(mad(asfloat(r0.xyz), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u)), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u))));
    o0.xyz = asuint((float3(_CustomIBLDiffuse.x, _CustomIBLDiffuse.y, _CustomIBLDiffuse.z)));
    o0.w = 0x3f800000u;
    o1.w = 0x00000000u;
    o2.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f2b851fu);
    o3.w = 0x00000000u;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "TransparentFullRes"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 unity_MatrixVP : packoffset(c102);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}


struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3])))), asfloat(r0.wwww), asfloat(r1.xyzw)));
    o1.xyzw = v1.xyzw;
    o2.xyzw = v2.xyzw;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    float4 _ScreenSize : packoffset(c138);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _BaseColor : packoffset(c0);
    float3 _CustomIBLDiffuse : packoffset(c1);
    float _UsingNonPSR : packoffset(c1.w);
}


SamplerState sampler_ScreenSpaceShadowTexture;
Texture2D<float4> _ScreenSpaceShadowTexture : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1)
{
    uint4 r0, r1, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.xy = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r0.xyzw = asuint(_ScreenSpaceShadowTexture.Sample(sampler_ScreenSpaceShadowTexture, asfloat(r0.xy)).xyzw);
    r0.yzw = asuint((asfloat(asuint((float3(_BaseColor.x, _BaseColor.y, _BaseColor.z)))) + asfloat((asuint((float3(_CustomIBLDiffuse.x, _CustomIBLDiffuse.y, _CustomIBLDiffuse.z))) ^ 0x80000000u))));
    r0.xyz = asuint(mad(asfloat(r0.xxx), asfloat(r0.yzw), asfloat(asuint((float3(_CustomIBLDiffuse.x, _CustomIBLDiffuse.y, _CustomIBLDiffuse.z))))));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UsingNonPSR)))) ? 0xffffffffu : 0u);
    r1.xyzw = ((r1.xxxx != 0u) ? uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u) : v2.xyzw);
    r0.w = 0x3f800000u;
    o0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(r1.xyzw)));
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "TransparentHalfRes"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 unity_MatrixVP : packoffset(c102);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}


struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3])))), asfloat(r0.wwww), asfloat(r1.xyzw)));
    o1.xyzw = v1.xyzw;
    o2.xyzw = v2.xyzw;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    float4 _ScreenSize : packoffset(c138);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _BaseColor : packoffset(c0);
    float3 _CustomIBLDiffuse : packoffset(c1);
    float _UsingNonPSR : packoffset(c1.w);
}


SamplerState sampler_ScreenSpaceShadowTexture;
Texture2D<float4> _ScreenSpaceShadowTexture : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1)
{
    uint4 r0, r1, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.xy = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r0.xyzw = asuint(_ScreenSpaceShadowTexture.Sample(sampler_ScreenSpaceShadowTexture, asfloat(r0.xy)).xyzw);
    r0.yzw = asuint((asfloat(asuint((float3(_BaseColor.x, _BaseColor.y, _BaseColor.z)))) + asfloat((asuint((float3(_CustomIBLDiffuse.x, _CustomIBLDiffuse.y, _CustomIBLDiffuse.z))) ^ 0x80000000u))));
    r0.xyz = asuint(mad(asfloat(r0.xxx), asfloat(r0.yzw), asfloat(asuint((float3(_CustomIBLDiffuse.x, _CustomIBLDiffuse.y, _CustomIBLDiffuse.z))))));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UsingNonPSR)))) ? 0xffffffffu : 0u);
    r1.xyzw = ((r1.xxxx != 0u) ? uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u) : v2.xyzw);
    r0.w = 0x3f800000u;
    o0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(r1.xyzw)));
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Blend One Zero
            BlendOp Add
            ZTest LEqual
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex CorinVertex
            #pragma fragment CorinPixel
            #if defined(SHADER_STAGE_VERTEX)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 unity_MatrixVP : packoffset(c102);
    float _ShadowPancaking : packoffset(c27);
    float4 _ShadowBias : packoffset(c169);
    float3 _LightDirection : packoffset(c171);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _BaseMap_ST : packoffset(c4);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float2 input1 : TEXCOORD0)
{
    uint4 r0, r1, o0, o1, v0, v1;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_LightDirection.x, _LightDirection.y, _LightDirection.z)))), asfloat(asuint((float3(_ShadowBias.x, _ShadowBias.x, _ShadowBias.x)))), asfloat(r0.xyz)));
    r0.w = ((asfloat(asuint((_ShadowPancaking))) == asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    r0.x = asuint(min(asfloat(r1.w), asfloat(r1.z)));
    o0.z = ((r0.w != 0u) ? r0.x : r1.z);
    o0.xyw = r1.xyw;
    o1.xy = asuint(mad(asfloat(v1.xy), asfloat(asuint((float2(_BaseMap_ST.x, _BaseMap_ST.y)))), asfloat(asuint((float2(_BaseMap_ST.z, _BaseMap_ST.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
struct CorinFragmentOut {
    float4 output0 : SV_TARGET0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0)
{
    uint4 o0, v0, v1;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
