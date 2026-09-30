Shader "miHoYo/Particles/Distortion UVMove_Deprecated"
{
    Properties
    {
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "linearGray" {}
        _DTTexUVMode ("UV Mode", Float) = 0.0
        _DTTexClampU ("Clamp U", Float) = 0.0
        _DTTexClampV ("Clamp V", Float) = 0.0
        _DTTexFlip ("Flip", Float) = 0.0
        _DTTexRotation ("Rotation", Float) = 0.0
        _DTIntensity ("Distortion Intensity", Float) = 0.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
        _DtUvMove ("UV Move", Float) = 0.0
        _UVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SeparateRGBIntensity ("Separate RGB Intensity", Float) = 0.0
        _DtMask ("Distortion Mask", Float) = 0.0
        _DtMaskTex ("Mask Texture", 2D) = "white" {}
        _DtMaskTexUVMode ("UV Mode", Float) = 0.0
        _DtMaskTexClampU ("Clamp U", Float) = 0.0
        _DtMaskTexClampV ("Clamp V", Float) = 0.0
        _DtMaskTexFlip ("Flip", Float) = 0.0
        _DtMaskTexRotation ("Rotation", Float) = 0.0
        _DtMaskTexChannel ("Mask Channel", Float) = 0.0
        _MaskUVSpeed ("Mask UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _ScreenEffects ("Screen Effects", Float) = 0.0
        _SE_UVMode ("UV Mode", Float) = 0.0
        _SE_SquarePixels ("Square Pixels", Float) = 1.0
        _SE_Boundary ("Boundary", Float) = 0.3
        _SE_BoundaryAspect ("Boundary Aspect", Float) = 1.0
        _SE_Feather ("Feather", Float) = 1.0
        _SE_MaxOpacity ("Max Opacity", Float) = 1.0
        _SE_Invert ("Invert", Float) = 0.0
        _OffsetFactor ("Offset Factor", Float) = 0.0
        _OffsetUnits ("Offset Units", Float) = 0.0
        _ZOffset ("Z Offset", Float) = 0.0
        _RenderOnTop ("Render On Top", Float) = 0.0
        _Cull ("Cull", Float) = 0.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _TimeOffset ("Time Offset", Float) = 0.0
        _UiPreTransformFix ("用于UI层(修复Android上UI特效不显示)", Float) = 0.0
    }
    SubShader
    {
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
    column_major float4x4 glstate_matrix_projection : packoffset(c89);
    column_major float4x4 unity_MatrixV : packoffset(c93);
    float4 _ProjectionParams : packoffset(c59);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _ZOffset : packoffset(c2.w);
    float _DTIntensity : packoffset(c8.z);
    float _OpaquenessFadeByScript : packoffset(c9.w);
    float _Dist_Intensity_PostProcessing : packoffset(c10);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float2 output2 : TEXCOORD1;
    float output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float2 input2 : TEXCOORD1, float4 input3 : COLOR0)
{
    uint4 r0, r1, r2, o0, o1, o2, o3, v0, v1, v2, v3;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xy = asuint(input2);
    v3.xyzw = asuint(input3);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r0.xyw)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset)))), asfloat(r0.xyz)));
    r1.x = asuint((glstate_matrix_projection[2][0]));
    r1.y = asuint((glstate_matrix_projection[2][1]));
    r1.z = asuint((glstate_matrix_projection[2][2]));
    r1.w = asuint((glstate_matrix_projection[2][3]));
    r0.w = 0x3f800000u;
    r1.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r2.x = asuint((glstate_matrix_projection[0][0]));
    r2.y = asuint((glstate_matrix_projection[0][2]));
    r2.z = asuint((glstate_matrix_projection[0][3]));
    r1.x = asuint(dot(asfloat(r2.xyz), asfloat(r0.xzw)));
    r2.x = asuint((glstate_matrix_projection[1][1]));
    r2.y = asuint((glstate_matrix_projection[1][2]));
    r2.z = asuint((glstate_matrix_projection[1][3]));
    r1.y = asuint(dot(asfloat(r2.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    r1.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    o0.xyzw = r1.xyzw;
    o3.zw = r1.zw;
    r0.xz = asuint((asfloat(r1.xw) * asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r0.y = asuint((asfloat(r1.y) * asfloat(asuint((_ProjectionParams.x)))));
    r0.w = asuint((asfloat(r0.y) * asfloat(0x3f000000u)));
    o3.xy = asuint((asfloat(r0.zz) + asfloat(r0.xw)));
    o1.xyzw = v1.xyzw;
    r0.x = asuint((asfloat(v3.w) * asfloat(asuint((_DTIntensity)))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    o2.z = asuint((asfloat(r0.x) * asfloat(asuint((_Dist_Intensity_PostProcessing)))));
    o2.xy = v2.xy;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xy);
    result.output3 = asfloat(o2.z);
    result.output4 = asfloat(o3.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    float4 _ZBufferParams : packoffset(c61);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _DTTex_ST : packoffset(c0);
    float4 _DtMaskTex_ST : packoffset(c1);
    float2 _UVSpeed : packoffset(c3.z);
    float2 _MaskUVSpeed : packoffset(c4);
    float _DistortionMode : packoffset(c5.w);
    float _DTTexUVMode : packoffset(c6);
    float _DTTexClampU : packoffset(c6.y);
    float _DTTexClampV : packoffset(c6.z);
    float _DTTexFlip : packoffset(c6.w);
    float _DTTexRotation : packoffset(c7);
    float _DtMaskTexUVMode : packoffset(c7.y);
    float _DtMaskTexClampU : packoffset(c7.z);
    float _DtMaskTexClampV : packoffset(c7.w);
    float _DtMaskTexFlip : packoffset(c8);
    float _DtMaskTexRotation : packoffset(c8.y);
    float _DtUvMove : packoffset(c8.w);
    float _DtMask : packoffset(c9);
    float _DtMaskTexChannel : packoffset(c9.y);
    float _SeparateRGBIntensity : packoffset(c9.z);
    float _RenderOnTop : packoffset(c10.y);
    float _IgnoreTimeScale : packoffset(c10.z);
    float _TimeOffset : packoffset(c12.z);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_DepthMipChain;
SamplerState sampler_DTTex;
SamplerState sampler_DtMaskTex;
Texture2D<float4> _DepthMipChain : register(t0);
Texture2D<float4> _DTTex : register(t1);
Texture2D<float4> _DtMaskTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float2 input2 : TEXCOORD1, float input3 : TEXCOORD2, float4 input4 : TEXCOORD3)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, v0, v1, v2, v3;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xy = asuint(input2);
    v2.z = asuint(input3);
    v3.xyzw = asuint(input4);
    r0.xy = asuint(mad(asfloat(v1.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.z = asuint(min(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint(max(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r0.z = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r0.w = asuint((asfloat(r0.z) * asfloat(r0.z)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(0x3caaae5fu), asfloat(0xbdae5a36u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3e3876e2u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0xbea91d04u)));
    r0.w = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3f7ff738u)));
    r1.x = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r1.y = ((asfloat((r0.x & 0x7fffffffu)) < asfloat((r0.y & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.x = asuint(mad(asfloat(r1.x), asfloat(0xc0000000u), asfloat(0x3fc90fdbu)));
    r1.x = (r1.y & r1.x);
    r0.z = asuint(mad(asfloat(r0.z), asfloat(r0.w), asfloat(r1.x)));
    r0.w = ((asfloat(r0.x) < asfloat((r0.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & 0xc0490fdbu);
    r0.z = asuint((asfloat(r0.w) + asfloat(r0.z)));
    r0.w = asuint(min(asfloat(r0.x), asfloat(r0.y)));
    r1.x = asuint(max(asfloat(r0.x), asfloat(r0.y)));
    r0.w = ((asfloat(r0.w) < asfloat((r0.w ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.x = ((asfloat(r1.x) >= asfloat((r1.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & r1.x);
    r1.x = ((r0.w != 0u) ? (r0.z ^ 0x80000000u) : r0.z);
    r0.x = asuint(dot(asfloat(r0.xy), asfloat(r0.xy)));
    r0.x = asuint(sqrt(asfloat(r0.x)));
    r1.y = asuint((asfloat(r0.x) * asfloat(0x3f000000u)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.y = ((asfloat(asuint((_DTTexUVMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xy = ((r0.yy != 0u) ? v1.xy : r1.xy);
    r2.zw = asuint((asfloat((r2.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat((r2.xyzw ^ 0x80000000u)) + asfloat(r2.yzwx)));
    r2.xyzw = asuint(mad(asfloat(asuint((float4(_DTTexRotation, _DTTexRotation, _DTTexRotation, _DTTexRotation)))), asfloat(r3.xyzw), asfloat(r2.xyzw)));
    r3.xyzw = ((asfloat(asuint((float4(_DTTexFlip, _DTTexFlip, _DTTexFlip, _DTTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r4.xyzw = (r3.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.yzw = ((r3.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r0.yzw = asuint((asfloat(r0.yzw) + asfloat(r4.yzw)));
    r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xy = r2.zw;
    r3.z = 0x3f800000u;
    r3.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xy = r2.xw;
    r5.z = 0x00000000u;
    r5.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r5.xyzw)));
    r6.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
    r2.xz = r3.xz;
    r2.w = asuint((_DTTexRotation));
    r3.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
    r2.xz = r5.xz;
    r2.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r4.xxxx), asfloat(r3.xyzw)));
    r0.yz = asuint((asfloat((r2.zw ^ 0x80000000u)) + asfloat(r2.xy)));
    r0.yz = asuint(mad(asfloat(r0.yz), asfloat(asuint((float2(_DTTex_ST.x, _DTTex_ST.y)))), asfloat(asuint((float2(_DTTex_ST.z, _DTTex_ST.w))))));
    r0.yz = asuint((asfloat(r2.zw) + asfloat(r0.yz)));
    r1.zw = asuint(mad(asfloat(asuint((float2(_UVSpeed.x, _UVSpeed.y)))), asfloat(r0.xx), asfloat(v1.zw)));
    r0.yz = asuint(mad(asfloat(r1.zw), asfloat(asuint((float2(_DtUvMove, _DtUvMove)))), asfloat(r0.yz)));
    r1.zw = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_DTTexClampU, _DTTexClampV))))) ? 0xffffffffu : 0u);
    r2.xy = asuint(saturate(asfloat(r0.yz)));
    r0.yz = ((r1.zw != 0u) ? r2.xy : r0.yz);
    r2.xyz = asuint((asfloat(v3.xyz) / asfloat(v3.www)));
    r3.xyzw = asuint(_DepthMipChain.SampleLevel(sampler_DepthMipChain, asfloat(r2.xy), asfloat(0x00000000u)).xyzw);
    r0.w = asuint(mad(asfloat(asuint((_ZBufferParams.z))), asfloat(r3.x), asfloat(asuint((_ZBufferParams.w)))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r1.z = asuint(mad(asfloat(asuint((_ZBufferParams.z))), asfloat(r2.z), asfloat(asuint((_ZBufferParams.w)))));
    r1.z = asuint((asfloat(0x3f800000u) / asfloat(r1.z)));
    r1.w = ((asfloat(asuint((_RenderOnTop))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.w = asuint((asfloat(r0.w) + asfloat((r1.z ^ 0x80000000u))));
    r0.w = ((asfloat(r0.w) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    r0.w = (r1.w & r0.w);
    if (r0.w != 0u) discard;
    r2.xyzw = asuint(_DTTex.Sample(sampler_DTTex, asfloat(r0.yz)).xyzw);
    r3.xyzw = asuint(_DTTex.SampleLevel(sampler_DTTex, asfloat(r0.yz), asfloat(0x00000000u)).xyzw);
    r0.yzw = asuint((asfloat((r2.xyz ^ 0x80000000u)) + asfloat(r3.xyz)));
    r0.yzw = asuint(mad(asfloat(asuint((float3(_DTTexUVMode, _DTTexUVMode, _DTTexUVMode)))), asfloat(r0.yzw), asfloat(r2.xyz)));
    r0.yz = asuint((asfloat(r0.yz) + asfloat(uint2(0xbefefeffu, 0xbefefeffu))));
    r0.yz = asuint((asfloat(r0.yz) * asfloat(v2.zz)));
    r2.xy = asuint((asfloat(r0.yz) + asfloat(r0.yz)));
    r2.z = asuint((asfloat(r0.w) * asfloat(asuint((_SeparateRGBIntensity)))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint((_DtMask)))) ? 0xffffffffu : 0u);
    if (r0.y != 0u) {
        r0.y = ((asfloat(asuint((_DtMaskTexUVMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
        r1.xy = ((r0.yy != 0u) ? v1.xy : r1.xy);
        r1.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
        r3.xyzw = asuint((asfloat((r1.xyzw ^ 0x80000000u)) + asfloat(r1.yzwx)));
        r1.xyzw = asuint(mad(asfloat(asuint((float4(_DtMaskTexRotation, _DtMaskTexRotation, _DtMaskTexRotation, _DtMaskTexRotation)))), asfloat(r3.xyzw), asfloat(r1.xyzw)));
        r3.xyzw = ((asfloat(asuint((float4(_DtMaskTexFlip, _DtMaskTexFlip, _DtMaskTexFlip, _DtMaskTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
        r4.xyzw = (r3.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
        r0.yzw = ((r3.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
        r0.yzw = asuint((asfloat(r0.yzw) + asfloat(r4.yzw)));
        r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
        r3.xy = r1.zw;
        r3.z = 0x3f800000u;
        r3.w = asuint((asfloat((asuint((_DtMaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r5.xy = r1.xw;
        r5.z = 0x00000000u;
        r5.w = asuint((asfloat((asuint((_DtMaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r5.xyzw)));
        r6.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
        r1.xz = r3.xz;
        r1.w = asuint((_DtMaskTexRotation));
        r3.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
        r1.xz = r5.xz;
        r1.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r4.xxxx), asfloat(r3.xyzw)));
        r0.yz = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
        r0.yz = asuint(mad(asfloat(r0.yz), asfloat(asuint((float2(_DtMaskTex_ST.x, _DtMaskTex_ST.y)))), asfloat(asuint((float2(_DtMaskTex_ST.z, _DtMaskTex_ST.w))))));
        r0.yz = asuint((asfloat(r1.zw) + asfloat(r0.yz)));
        r0.xw = asuint(mad(asfloat(asuint((float2(_MaskUVSpeed.x, _MaskUVSpeed.y)))), asfloat(r0.xx), asfloat(v2.xy)));
        r0.xy = asuint((asfloat(r0.xw) + asfloat(r0.yz)));
        r0.zw = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_DtMaskTexClampU, _DtMaskTexClampV))))) ? 0xffffffffu : 0u);
        r1.xy = asuint(saturate(asfloat(r0.xy)));
        r0.xy = ((r0.zw != 0u) ? r1.xy : r0.xy);
        r0.xyzw = asuint(_DtMaskTex.Sample(sampler_DtMaskTex, asfloat(r0.xy)).xyzw);
        r1.x = (uint)(asfloat(asuint((_DtMaskTexChannel))));
        r0.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
        r2.xyz = asuint((asfloat(r0.xxx) * asfloat(r2.xyz)));
    }
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionMode)))) ? 0xffffffffu : 0u);
    r2.w = 0x3f800000u;
    o1.xyzw = ((r0.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u) : r2.xyzw);
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "Distortion"
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
    column_major float4x4 glstate_matrix_projection : packoffset(c89);
    column_major float4x4 unity_MatrixV : packoffset(c93);
    float4 _ProjectionParams : packoffset(c59);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _ZOffset : packoffset(c2.w);
    float _DTIntensity : packoffset(c8.z);
    float _OpaquenessFadeByScript : packoffset(c9.w);
    float _Dist_Intensity_PostProcessing : packoffset(c10);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float2 output2 : TEXCOORD1;
    float output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float2 input2 : TEXCOORD1, float4 input3 : COLOR0)
{
    uint4 r0, r1, r2, o0, o1, o2, o3, v0, v1, v2, v3;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xy = asuint(input2);
    v3.xyzw = asuint(input3);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r0.xyw)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset)))), asfloat(r0.xyz)));
    r1.x = asuint((glstate_matrix_projection[2][0]));
    r1.y = asuint((glstate_matrix_projection[2][1]));
    r1.z = asuint((glstate_matrix_projection[2][2]));
    r1.w = asuint((glstate_matrix_projection[2][3]));
    r0.w = 0x3f800000u;
    r1.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r2.x = asuint((glstate_matrix_projection[0][0]));
    r2.y = asuint((glstate_matrix_projection[0][2]));
    r2.z = asuint((glstate_matrix_projection[0][3]));
    r1.x = asuint(dot(asfloat(r2.xyz), asfloat(r0.xzw)));
    r2.x = asuint((glstate_matrix_projection[1][1]));
    r2.y = asuint((glstate_matrix_projection[1][2]));
    r2.z = asuint((glstate_matrix_projection[1][3]));
    r1.y = asuint(dot(asfloat(r2.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    r1.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    o0.xyzw = r1.xyzw;
    o3.zw = r1.zw;
    r0.xz = asuint((asfloat(r1.xw) * asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r0.y = asuint((asfloat(r1.y) * asfloat(asuint((_ProjectionParams.x)))));
    r0.w = asuint((asfloat(r0.y) * asfloat(0x3f000000u)));
    o3.xy = asuint((asfloat(r0.zz) + asfloat(r0.xw)));
    o1.xyzw = v1.xyzw;
    r0.x = asuint((asfloat(v3.w) * asfloat(asuint((_DTIntensity)))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    o2.z = asuint((asfloat(r0.x) * asfloat(asuint((_Dist_Intensity_PostProcessing)))));
    o2.xy = v2.xy;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xy);
    result.output3 = asfloat(o2.z);
    result.output4 = asfloat(o3.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    float4 _ZBufferParams : packoffset(c61);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _DTTex_ST : packoffset(c0);
    float4 _DtMaskTex_ST : packoffset(c1);
    float2 _UVSpeed : packoffset(c3.z);
    float2 _MaskUVSpeed : packoffset(c4);
    float _DistortionMode : packoffset(c5.w);
    float _DTTexUVMode : packoffset(c6);
    float _DTTexClampU : packoffset(c6.y);
    float _DTTexClampV : packoffset(c6.z);
    float _DTTexFlip : packoffset(c6.w);
    float _DTTexRotation : packoffset(c7);
    float _DtMaskTexUVMode : packoffset(c7.y);
    float _DtMaskTexClampU : packoffset(c7.z);
    float _DtMaskTexClampV : packoffset(c7.w);
    float _DtMaskTexFlip : packoffset(c8);
    float _DtMaskTexRotation : packoffset(c8.y);
    float _DtUvMove : packoffset(c8.w);
    float _DtMask : packoffset(c9);
    float _DtMaskTexChannel : packoffset(c9.y);
    float _SeparateRGBIntensity : packoffset(c9.z);
    float _RenderOnTop : packoffset(c10.y);
    float _IgnoreTimeScale : packoffset(c10.z);
    float _TimeOffset : packoffset(c12.z);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_DepthMipChain;
SamplerState sampler_DTTex;
SamplerState sampler_DtMaskTex;
Texture2D<float4> _DepthMipChain : register(t0);
Texture2D<float4> _DTTex : register(t1);
Texture2D<float4> _DtMaskTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float2 input2 : TEXCOORD1, float input3 : TEXCOORD2, float4 input4 : TEXCOORD3)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, v0, v1, v2, v3;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xy = asuint(input2);
    v2.z = asuint(input3);
    v3.xyzw = asuint(input4);
    r0.xy = asuint(mad(asfloat(v1.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.z = asuint(min(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint(max(asfloat((r0.x & 0x7fffffffu)), asfloat((r0.y & 0x7fffffffu))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r0.z = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r0.w = asuint((asfloat(r0.z) * asfloat(r0.z)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(0x3caaae5fu), asfloat(0xbdae5a36u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3e3876e2u)));
    r1.x = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0xbea91d04u)));
    r0.w = asuint(mad(asfloat(r0.w), asfloat(r1.x), asfloat(0x3f7ff738u)));
    r1.x = asuint((asfloat(r0.w) * asfloat(r0.z)));
    r1.y = ((asfloat((r0.x & 0x7fffffffu)) < asfloat((r0.y & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.x = asuint(mad(asfloat(r1.x), asfloat(0xc0000000u), asfloat(0x3fc90fdbu)));
    r1.x = (r1.y & r1.x);
    r0.z = asuint(mad(asfloat(r0.z), asfloat(r0.w), asfloat(r1.x)));
    r0.w = ((asfloat(r0.x) < asfloat((r0.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & 0xc0490fdbu);
    r0.z = asuint((asfloat(r0.w) + asfloat(r0.z)));
    r0.w = asuint(min(asfloat(r0.x), asfloat(r0.y)));
    r1.x = asuint(max(asfloat(r0.x), asfloat(r0.y)));
    r0.w = ((asfloat(r0.w) < asfloat((r0.w ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.x = ((asfloat(r1.x) >= asfloat((r1.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r0.w = (r0.w & r1.x);
    r1.x = ((r0.w != 0u) ? (r0.z ^ 0x80000000u) : r0.z);
    r0.x = asuint(dot(asfloat(r0.xy), asfloat(r0.xy)));
    r0.x = asuint(sqrt(asfloat(r0.x)));
    r1.y = asuint((asfloat(r0.x) * asfloat(0x3f000000u)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.y = ((asfloat(asuint((_DTTexUVMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xy = ((r0.yy != 0u) ? v1.xy : r1.xy);
    r2.zw = asuint((asfloat((r2.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat((r2.xyzw ^ 0x80000000u)) + asfloat(r2.yzwx)));
    r2.xyzw = asuint(mad(asfloat(asuint((float4(_DTTexRotation, _DTTexRotation, _DTTexRotation, _DTTexRotation)))), asfloat(r3.xyzw), asfloat(r2.xyzw)));
    r3.xyzw = ((asfloat(asuint((float4(_DTTexFlip, _DTTexFlip, _DTTexFlip, _DTTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r4.xyzw = (r3.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.yzw = ((r3.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r0.yzw = asuint((asfloat(r0.yzw) + asfloat(r4.yzw)));
    r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xy = r2.zw;
    r3.z = 0x3f800000u;
    r3.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xy = r2.xw;
    r5.z = 0x00000000u;
    r5.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r5.xyzw)));
    r6.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
    r2.xz = r3.xz;
    r2.w = asuint((_DTTexRotation));
    r3.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
    r2.xz = r5.xz;
    r2.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r4.xxxx), asfloat(r3.xyzw)));
    r0.yz = asuint((asfloat((r2.zw ^ 0x80000000u)) + asfloat(r2.xy)));
    r0.yz = asuint(mad(asfloat(r0.yz), asfloat(asuint((float2(_DTTex_ST.x, _DTTex_ST.y)))), asfloat(asuint((float2(_DTTex_ST.z, _DTTex_ST.w))))));
    r0.yz = asuint((asfloat(r2.zw) + asfloat(r0.yz)));
    r1.zw = asuint(mad(asfloat(asuint((float2(_UVSpeed.x, _UVSpeed.y)))), asfloat(r0.xx), asfloat(v1.zw)));
    r0.yz = asuint(mad(asfloat(r1.zw), asfloat(asuint((float2(_DtUvMove, _DtUvMove)))), asfloat(r0.yz)));
    r1.zw = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_DTTexClampU, _DTTexClampV))))) ? 0xffffffffu : 0u);
    r2.xy = asuint(saturate(asfloat(r0.yz)));
    r0.yz = ((r1.zw != 0u) ? r2.xy : r0.yz);
    r2.xyz = asuint((asfloat(v3.xyz) / asfloat(v3.www)));
    r3.xyzw = asuint(_DepthMipChain.SampleLevel(sampler_DepthMipChain, asfloat(r2.xy), asfloat(0x00000000u)).xyzw);
    r0.w = asuint(mad(asfloat(asuint((_ZBufferParams.z))), asfloat(r3.x), asfloat(asuint((_ZBufferParams.w)))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r1.z = asuint(mad(asfloat(asuint((_ZBufferParams.z))), asfloat(r2.z), asfloat(asuint((_ZBufferParams.w)))));
    r1.z = asuint((asfloat(0x3f800000u) / asfloat(r1.z)));
    r1.w = ((asfloat(asuint((_RenderOnTop))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.w = asuint((asfloat(r0.w) + asfloat((r1.z ^ 0x80000000u))));
    r0.w = ((asfloat(r0.w) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    r0.w = (r1.w & r0.w);
    if (r0.w != 0u) discard;
    r2.xyzw = asuint(_DTTex.Sample(sampler_DTTex, asfloat(r0.yz)).xyzw);
    r3.xyzw = asuint(_DTTex.SampleLevel(sampler_DTTex, asfloat(r0.yz), asfloat(0x00000000u)).xyzw);
    r0.yzw = asuint((asfloat((r2.xyz ^ 0x80000000u)) + asfloat(r3.xyz)));
    r0.yzw = asuint(mad(asfloat(asuint((float3(_DTTexUVMode, _DTTexUVMode, _DTTexUVMode)))), asfloat(r0.yzw), asfloat(r2.xyz)));
    r0.yz = asuint((asfloat(r0.yz) + asfloat(uint2(0xbefefeffu, 0xbefefeffu))));
    r0.yz = asuint((asfloat(r0.yz) * asfloat(v2.zz)));
    r2.xy = asuint((asfloat(r0.yz) + asfloat(r0.yz)));
    r2.z = asuint((asfloat(r0.w) * asfloat(asuint((_SeparateRGBIntensity)))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint((_DtMask)))) ? 0xffffffffu : 0u);
    if (r0.y != 0u) {
        r0.y = ((asfloat(asuint((_DtMaskTexUVMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
        r1.xy = ((r0.yy != 0u) ? v1.xy : r1.xy);
        r1.zw = asuint((asfloat((r1.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
        r3.xyzw = asuint((asfloat((r1.xyzw ^ 0x80000000u)) + asfloat(r1.yzwx)));
        r1.xyzw = asuint(mad(asfloat(asuint((float4(_DtMaskTexRotation, _DtMaskTexRotation, _DtMaskTexRotation, _DtMaskTexRotation)))), asfloat(r3.xyzw), asfloat(r1.xyzw)));
        r3.xyzw = ((asfloat(asuint((float4(_DtMaskTexFlip, _DtMaskTexFlip, _DtMaskTexFlip, _DtMaskTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
        r4.xyzw = (r3.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
        r0.yzw = ((r3.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
        r0.yzw = asuint((asfloat(r0.yzw) + asfloat(r4.yzw)));
        r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
        r3.xy = r1.zw;
        r3.z = 0x3f800000u;
        r3.w = asuint((asfloat((asuint((_DtMaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r5.xy = r1.xw;
        r5.z = 0x00000000u;
        r5.w = asuint((asfloat((asuint((_DtMaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
        r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r5.xyzw)));
        r6.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
        r1.xz = r3.xz;
        r1.w = asuint((_DtMaskTexRotation));
        r3.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
        r1.xz = r5.xz;
        r1.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r4.xxxx), asfloat(r3.xyzw)));
        r0.yz = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
        r0.yz = asuint(mad(asfloat(r0.yz), asfloat(asuint((float2(_DtMaskTex_ST.x, _DtMaskTex_ST.y)))), asfloat(asuint((float2(_DtMaskTex_ST.z, _DtMaskTex_ST.w))))));
        r0.yz = asuint((asfloat(r1.zw) + asfloat(r0.yz)));
        r0.xw = asuint(mad(asfloat(asuint((float2(_MaskUVSpeed.x, _MaskUVSpeed.y)))), asfloat(r0.xx), asfloat(v2.xy)));
        r0.xy = asuint((asfloat(r0.xw) + asfloat(r0.yz)));
        r0.zw = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_DtMaskTexClampU, _DtMaskTexClampV))))) ? 0xffffffffu : 0u);
        r1.xy = asuint(saturate(asfloat(r0.xy)));
        r0.xy = ((r0.zw != 0u) ? r1.xy : r0.xy);
        r0.xyzw = asuint(_DtMaskTex.Sample(sampler_DtMaskTex, asfloat(r0.xy)).xyzw);
        r1.x = (uint)(asfloat(asuint((_DtMaskTexChannel))));
        r0.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
        r2.xyz = asuint((asfloat(r0.xxx) * asfloat(r2.xyz)));
    }
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionMode)))) ? 0xffffffffu : 0u);
    r2.w = 0x3f800000u;
    o0.xyzw = ((r0.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u) : r2.xyzw);
    o1.xyzw = (r0.xxxx & r2.xyzw);
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
