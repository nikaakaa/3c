Shader "miHoYo/Particles/MeshDecal"
{
    Properties
    {
        _Mode ("Mode", Float) = 0.0
        _group_main ("Surface Input", Float) = 0.0
        _BaseColor ("BaseColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _UseCustomData2Color ("Use CustomData2 Color", Float) = 0.0
        _BaseColorMap ("BaseMap", 2D) = "white" {}
        _NormalMap ("NormalMap", 2D) = "bump" {}
        _NormalIntensity ("Normal Intensity", Float) = 1.0
        _MaskMap ("MaskMap", 2D) = "red" {}
        _BlendWithScene ("Blend With Scene", Float) = 1.0
        _DecalFadeFactor ("_DecalFadeFactor", Float) = 0.0
        _DecalEmissionIntensity ("EmissionIntensity", Float) = 0.0
        _EmissionColor ("EmissionColor", Color) = (0.0, 0.0, 0.0, 0.0)
        _EmissionBlendBaseColor ("EmissionBlendWith", Float) = 1.0
        _EmissionTex ("Emission Tex", 2D) = "white" {}
        _EmissionRampTex ("Ramp Tex", 2D) = "white" {}
        _EmissionRampV ("V Position", Float) = 0.0
        _EmissionRampMulti ("Correction", Float) = 1.0
        _EmissionRampTexUVMove ("UV Move", Float) = 0.0
        _EmissionRampTexUVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _UseDissolveTex ("Dissolve", Float) = 0.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveTexFlip ("Flip", Float) = 0.0
        _DissolveTexRotate ("Rotate", Float) = 0.0
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _SoftEdge ("Soft Edge", Float) = 0.0
        _SoftRange ("   Soft Range", Float) = 0.0
        _SoftEdgeUsingOldFunction ("   使用旧版溶解方法", Float) = 0.0
        _UsingNonPSR ("用于非粒子系统Renderer", Float) = 0.0
        _DissolveProgress ("   Dissolve Progress", Float) = 0.0
        _UseMaskTex ("Mask Tex", Float) = 0.0
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskChannelMapping ("Mask Channel Mapping", Float) = 0.0
        _MaskTexFlip ("Flip", Float) = 0.0
        _MaskTexRotate ("Rotate", Float) = 0.0
        _MaskTexUVSpeed ("Mask Tex UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _group_settings ("Settings", Float) = 0.0
        _TimeScaleSpeed ("Time Scale Speed", Float) = 0.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _DitherAlpha ("Dither Alpha", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2", Float) = 1.0
        _TransZWrite ("ZWrite", Float) = 0.0
    }
    SubShader
    {
        Pass
        {
            Name "DBufferMesh_D"
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
    float4 _BaseColorMap_ST : packoffset(c2);
    float _UseCustomData2Color : packoffset(c6.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : COLOR0;
    float4 output2 : TEXCOORD0;
    float4 output3 : TEXCOORD1;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float2 input3 : TEXCOORD2, float3 input4 : NORMAL0, float4 input5 : TANGENT0, float4 input6 : COLOR0)
{
    uint4 r0, r1, o0, o1, o2, o3, o4, o5, o6, v0, v1, v2, v3, v4, v5, v6;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
    v4.xyz = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o2.xyz = r0.xyz;
    o0.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    o1.xyzw = v6.xyzw;
    o2.w = 0x00000000u;
    o3.xy = asuint(mad(asfloat(v1.xy), asfloat(asuint((float2(_BaseColorMap_ST.x, _BaseColorMap_ST.y)))), asfloat(asuint((float2(_BaseColorMap_ST.z, _BaseColorMap_ST.w))))));
    o3.zw = uint2(0x00000000u, 0x00000000u);
    r0.x = asuint(dot(asfloat(v4.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r0.y = asuint(dot(asfloat(v4.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r0.z = asuint(dot(asfloat(v4.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    o4.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o4.w = 0x00000000u;
    r0.xyz = asuint((asfloat(v5.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v5.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v5.zzz), asfloat(r0.xyz)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    o5.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o5.w = asuint((asfloat(v5.w) * asfloat(asuint((unity_WorldTransformParams.w)))));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UseCustomData2Color)))) ? 0xffffffffu : 0u);
    r1.xy = v2.zw;
    r1.zw = v3.xy;
    o6.xyzw = ((r0.xxxx != 0u) ? r1.xyzw : uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    float4 _GlobalDecalBlendParameter : packoffset(c170);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _BaseColor : packoffset(c1);
    float _DecalEmissionIntensity : packoffset(c3);
    float _BlendWithScene : packoffset(c3.z);
    float _NormalIntensity : packoffset(c3.w);
    float _DitherAlpha : packoffset(c9.w);
    float _DitherAlpha2 : packoffset(c10);
    float _Mode : packoffset(c10.y);
    float3 _EmissionColor : packoffset(c11);
    float _EmissionBlendBaseColor : packoffset(c11.w);
}


SamplerState sampler_BaseColorMap;
SamplerState sampler_NormalMap;
SamplerState sampler_MaskMap;
Texture2D<float4> _BaseColorMap : register(t0);
Texture2D<float4> _NormalMap : register(t1);
Texture2D<float4> _MaskMap : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
    float4 output2 : SV_Target2;
    float4 output3 : SV_Target3;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5)
{
    uint4 r0, r1, r2, r3, o0, o1, o2, o3, v0, v1, v2, v3, v4, v5, v6;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    r0.x = asuint(log2(asfloat(v6.w)));
    r0.x = asuint((asfloat(r0.x) * asfloat(0x41400000u)));
    r0.x = asuint(exp2(asfloat(r0.x)));
    r1.xyzw = asuint(_MaskMap.Sample(sampler_MaskMap, asfloat(v3.xy)).xyzw);
    r0.yzw = asuint((asfloat(r1.zzz) * asfloat(v1.xyz)));
    r0.yzw = asuint((asfloat(r0.yzw) * asfloat(asuint((float3(_DecalEmissionIntensity, _DecalEmissionIntensity, _DecalEmissionIntensity))))));
    r0.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r0.w = ((asfloat(0x3f000000u) < asfloat(asuint((_EmissionBlendBaseColor)))) ? 0xffffffffu : 0u);
    r2.xyzw = asuint((asfloat(v6.xyzw) * asfloat(asuint((float4(_BaseColor.x, _BaseColor.y, _BaseColor.z, _BaseColor.w))))));
    r3.xyzw = asuint(_BaseColorMap.Sample(sampler_BaseColorMap, asfloat(v3.xy)).xyzw);
    r2.xyzw = asuint((asfloat(r2.xyzw) * asfloat(r3.xyzw)));
    r3.xyz = asuint((asfloat(r2.xyz) * asfloat(asuint((float3(_EmissionColor.x, _EmissionColor.y, _EmissionColor.z))))));
    r3.xyz = ((r0.www != 0u) ? r3.xyz : asuint((float3(_EmissionColor.x, _EmissionColor.y, _EmissionColor.z))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(r3.xyz)));
    r3.xyz = asuint((asfloat(r0.xyz) * asfloat(v1.www)));
    r0.w = asuint(max(asfloat(r3.y), asfloat(r3.x)));
    r0.w = asuint(max(asfloat(r3.z), asfloat(r0.w)));
    r1.x = asuint(dot(asfloat(r3.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r0.w = asuint(min(asfloat(r0.w), asfloat(0x40a00000u)));
    r0.w = asuint(sqrt(asfloat(r0.w)));
    r3.x = asuint((asfloat(r0.w) * asfloat(0x3d4ccccdu)));
    r0.w = asuint((asfloat(asuint((_DitherAlpha))) * asfloat(asuint((_DitherAlpha2)))));
    r2.w = asuint((asfloat(r0.w) * asfloat(r2.w)));
    r2.xyzw = asuint((asfloat(r2.xyzw) * asfloat(v6.xyzw)));
    r0.xyz = asuint(mad(asfloat(r0.xyz), asfloat(v1.www), asfloat((r2.xyz ^ 0x80000000u))));
    o1.xyz = asuint(mad(asfloat(r3.xxx), asfloat(r0.xyz), asfloat(r2.xyz)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_Mode)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalDecalBlendParameter.z)) : asuint((_GlobalDecalBlendParameter.x)));
    r0.x = asuint((asfloat((r0.x ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_BlendWithScene)))));
    r0.y = asuint(mad(asfloat((r2.w ^ 0x80000000u)), asfloat(r0.x), asfloat(r1.x)));
    r3.w = asuint((asfloat(r0.x) * asfloat(r2.w)));
    r0.x = asuint((asfloat(r2.w) * asfloat(asuint((_NormalIntensity)))));
    r0.z = asuint(log2(asfloat(r1.z)));
    r0.w = asuint((asfloat((r1.y ^ 0x80000000u)) + asfloat(0x3f800000u)));
    o2.y = asuint((asfloat(r3.w) * asfloat(r0.w)));
    r0.z = asuint((asfloat(r0.z) * asfloat(0x3f400000u)));
    r0.z = asuint(exp2(asfloat(r0.z)));
    o1.w = asuint(mad(asfloat(r0.z), asfloat(r0.y), asfloat(r3.w)));
    o2.xw = r3.xw;
    o2.z = 0x00000000u;
    r0.yzw = asuint((asfloat(v4.zxy) * asfloat(v5.yzx)));
    r0.yzw = asuint(mad(asfloat(v4.yzx), asfloat(v5.zxy), asfloat((r0.yzw ^ 0x80000000u))));
    r0.yzw = asuint((asfloat(r0.yzw) * asfloat(v5.www)));
    r1.xyzw = asuint(_NormalMap.Sample(sampler_NormalMap, asfloat(v3.xy)).xyzw);
    r1.x = asuint((asfloat(r1.x) * asfloat(r1.w)));
    r1.xy = asuint(mad(asfloat(r1.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.yzw = asuint((asfloat(r0.yzw) * asfloat(r1.yyy)));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(v5.xyz), asfloat(r0.yzw)));
    r1.x = asuint(dot(asfloat(r1.xy), asfloat(r1.xy)));
    r1.x = asuint(min(asfloat(r1.x), asfloat(0x3f800000u)));
    r1.x = asuint((asfloat((r1.x ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.x = asuint(sqrt(asfloat(r1.x)));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(v4.xyz), asfloat(r0.yzw)));
    r0.yzw = asuint(saturate(mad(asfloat(r0.yzw), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u)), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u)))));
    o3.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    o3.w = r0.x;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
