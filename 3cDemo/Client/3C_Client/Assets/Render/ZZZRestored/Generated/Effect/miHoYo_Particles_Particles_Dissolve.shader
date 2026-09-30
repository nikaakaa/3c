Shader "miHoYo/Particles/Particles_Dissolve"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _MainTexFlip ("Flip", Float) = 0.0
        _MainTexRotation ("Rotation", Float) = 0.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _ColorChannelMapping ("Color Channel Mapping", Float) = 0.0
        _AlphaChannelMapping ("Alpha Channel Mapping", Float) = 3.0
        _ParticleColorBrightness ("Particle Color Brightness", Float) = 1.0
        _LerpValue ("Lerp Value", Float) = 1.0
        _UVMove ("UV Move", Float) = 0.0
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskChannelMapping ("Mask Channel Mapping", Float) = 0.0
        _MaskTexClampU ("Clamp U", Float) = 0.0
        _MaskTexClampV ("Clamp V", Float) = 0.0
        _MaskTexFlip ("Flip", Float) = 0.0
        _MaskTexRotation ("Rotation", Float) = 0.0
        _MaskUSpeed ("Mask U Speed", Float) = 0.0
        _MaskVSpeed ("Mask V Speed", Float) = 0.0
        _UseDissolveTex ("Use Dissolve Tex", Float) = 1.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _DissolveTexClampU ("Clamp U", Float) = 0.0
        _DissolveTexClampV ("Clamp V", Float) = 0.0
        _DissolveTexFlip ("Flip", Float) = 0.0
        _DissolveTexRotation ("Rotation", Float) = 0.0
        _DissolveUSpeed ("Dissolve U Speed", Float) = 0.0
        _DissolveVSpeed ("Dissolve V Speed", Float) = 0.0
        _DissolveRandomUV ("Dissolve Random UV", Float) = 0.0
        _SoftEdge ("Soft Edge", Float) = 0.0
        _UseDistortionTexture ("Use Distortion Texture", Float) = 0.0
        _DistortionTex ("Distortion Tex", 2D) = "bump" {}
        _DistortionChannel ("Distortion Channel", Float) = 0.0
        _DistortionTexClampU ("Clamp U", Float) = 0.0
        _DistortionTexClampV ("Clamp V", Float) = 0.0
        _DistortionTexFlip ("Flip", Float) = 0.0
        _DistortionTexRotation ("Rotation", Float) = 0.0
        _DistortionUSpeed ("Distortion U Speed", Float) = 0.0
        _DistortionVSpeed ("Distortion V Speed", Float) = 0.0
        _DistortionRandomUV ("Distortion Random UV", Float) = 0.0
        _DistortionIntensity ("Distortion Intensity", Float) = 0.0
        _DissolveDistortionIntensity ("Dissolve Distortion Intensity", Float) = 0.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.0
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "bump" {}
        _DTIntensity ("Distortion Intensity", Float) = 0.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
        _DtUvMove ("Distortion UV Move", Float) = 0.0
        _DtUSpeed ("Distortion U Speed", Float) = 1.0
        _DtVSpeed ("Distortion V Speed", Float) = 1.0
        _DitherAlpha ("Dither Alpha", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2", Float) = 1.0
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.0
        _BlendMode ("Blend Mode", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _OffsetFactor ("Offset Factor", Float) = 0.0
        _OffsetUnits ("Offset Units", Float) = 0.0
        _ZOffset ("Z Offset", Float) = 0.0
        _Cull ("Cull", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ZTest ("Render On Top", Float) = 4.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _TimeOffset ("Time Offset", Float) = 0.0
    }
    SubShader
    {
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
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainTex_ST : packoffset(c0);
    float4 _MaskTex_ST : packoffset(c1);
    float4 _DissolveTex_ST : packoffset(c2);
    float4 _DistortionTex_ST : packoffset(c3);
    float _ZOffset : packoffset(c6);
    float _MainTexFlip : packoffset(c7.y);
    float _MainTexRotation : packoffset(c7.z);
    float _ParticleColorBrightness : packoffset(c8.y);
    float _UVMove : packoffset(c8.w);
    float _MaskTexFlip : packoffset(c9.w);
    float _MaskTexRotation : packoffset(c10);
    float _MaskUSpeed : packoffset(c10.y);
    float _MaskVSpeed : packoffset(c10.z);
    float _DissolveTexFlip : packoffset(c11.z);
    float _DissolveTexRotation : packoffset(c11.w);
    float _DissolveUSpeed : packoffset(c12);
    float _DissolveVSpeed : packoffset(c12.y);
    float _DissolveRandomUV : packoffset(c12.z);
    float _DistortionTexFlip : packoffset(c13.w);
    float _DistortionTexRotation : packoffset(c14);
    float _DistortionUSpeed : packoffset(c14.y);
    float _DistortionVSpeed : packoffset(c14.z);
    float _DistortionRandomUV : packoffset(c14.w);
    float _TimeOffset : packoffset(c18.y);
    float _IgnoreTimeScale : packoffset(c19);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float output5 : TEXCOORD5;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float2 input4 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, o2, o3, o4, o5, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xy = asuint(input4);
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
    o0.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r1.x = asuint((glstate_matrix_projection[0][0]));
    r1.y = asuint((glstate_matrix_projection[0][2]));
    r1.z = asuint((glstate_matrix_projection[0][3]));
    o0.x = asuint(dot(asfloat(r1.xyz), asfloat(r0.xzw)));
    r1.x = asuint((glstate_matrix_projection[1][1]));
    r1.y = asuint((glstate_matrix_projection[1][2]));
    r1.z = asuint((glstate_matrix_projection[1][3]));
    o0.y = asuint(dot(asfloat(r1.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    o0.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    r0.xyzw = ((asfloat(asuint((float4(_MainTexFlip, _MainTexFlip, _MainTexFlip, _MainTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.xyz = ((r0.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r0.xyzw = (r0.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.yzw = asuint((asfloat(r1.xyz) + asfloat(r0.yzw)));
    r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.z = 0x00000000u;
    r3.xyzw = asuint(mad(asfloat(v2.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v2.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat((r4.xyzw ^ 0x80000000u))));
    r5.xyzw = asuint(mad(asfloat(asuint((float4(_MainTexRotation, _MainTexRotation, _MainTexRotation, _MainTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r5.xw;
    r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r2.xyzw)));
    r1.z = 0x3f800000u;
    r1.xy = r5.zw;
    r6.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
    r5.xz = r1.xz;
    r5.w = asuint((_MainTexRotation));
    r1.xyzw = asuint(mad(asfloat(r5.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
    r2.yw = r5.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r1.x = v3.w;
    r1.y = v4.x;
    r0.zw = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UVMove)))) ? 0xffffffffu : 0u);
    o1.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    r0.w = asuint((asfloat((asuint((_MaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.xyzw = ((asfloat(asuint((float4(_MaskTexFlip, _MaskTexFlip, _MaskTexFlip, _MaskTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r2.xyz = ((r1.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r1.xyzw = (r1.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r2.xyz) + asfloat(r1.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r2.w = asuint((asfloat((asuint((_MaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = asuint(mad(asfloat(asuint((float4(_MaskTexRotation, _MaskTexRotation, _MaskTexRotation, _MaskTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r5.xw;
    r2.z = 0x00000000u;
    r6.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.xy = r5.zw;
    r0.z = 0x3f800000u;
    r6.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r6.xyzw)));
    r5.xz = r0.xz;
    r5.w = asuint((_MaskTexRotation));
    r0.xyzw = asuint(mad(asfloat(r5.xyzw), asfloat(r1.yyyy), asfloat(r6.xyzw)));
    r2.yw = r5.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r1.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r1.xy = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_MaskUSpeed, _MaskVSpeed))))));
    r1.xy = asuint(frac(asfloat(r1.xy)));
    o1.zw = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r0.xyw = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.xyw = asuint((asfloat(r0.xyw) + asfloat(r5.yzw)));
    r0.xyw = asuint(max(asfloat(r0.xyw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r6.xyzw = asuint(mad(asfloat(asuint((float4(_DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r6.xw;
    r2.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r0.yyyy) * asfloat(r2.xyzw)));
    r1.xy = r6.zw;
    r1.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.wwww), asfloat(r4.xyzw)));
    r6.xz = r1.xz;
    r6.w = asuint((_DissolveTexRotation));
    r1.xyzw = asuint(mad(asfloat(r6.xyzw), asfloat(r0.xxxx), asfloat(r4.xyzw)));
    r2.yw = r6.yw;
    r1.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.xxxx), asfloat(r1.xyzw)));
    r0.xy = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex_ST.x, _DissolveTex_ST.y)))), asfloat(asuint((float2(_DissolveTex_ST.z, _DissolveTex_ST.w))))));
    r0.xy = asuint((asfloat(r1.zw) + asfloat(r0.xy)));
    r1.xy = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_DissolveUSpeed, _DissolveVSpeed))))));
    r0.zw = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_DistortionUSpeed, _DistortionVSpeed))))));
    r0.zw = asuint(frac(asfloat(r0.zw)));
    r1.xy = asuint(frac(asfloat(r1.xy)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.w = v4.y;
    r2.xyz = asuint((asfloat(v4.yyy) * asfloat(uint3(0x41f10a3du, 0x414c28f6u, 0x40490e56u))));
    r1.xyz = asuint(frac(asfloat(r2.xyz)));
    r1.xw = asuint((asfloat(r0.xy) + asfloat(r1.wx)));
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveRandomUV)))) ? 0xffffffffu : 0u);
    o2.xy = ((r2.xx != 0u) ? r1.xw : r0.xy);
    r2.xy = r3.zw;
    r2.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.xy = r3.xw;
    r4.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r6.xyz = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r5.yzw = asuint((asfloat(r6.xyz) + asfloat(r5.yzw)));
    r5.yzw = asuint(max(asfloat(r5.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r4.z = 0x00000000u;
    r6.xyzw = asuint((asfloat(r4.xyzw) * asfloat(r5.zzzz)));
    r2.z = 0x3f800000u;
    r6.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.wwww), asfloat(r6.xyzw)));
    r3.xz = r2.xz;
    r3.w = asuint((_DistortionTexRotation));
    r2.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r5.yyyy), asfloat(r6.xyzw)));
    r4.yw = r3.yw;
    r2.xyzw = asuint(mad(asfloat(r4.xyzw), asfloat(r5.xxxx), asfloat(r2.xyzw)));
    r0.xy = asuint((asfloat((r2.zw ^ 0x80000000u)) + asfloat(r2.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DistortionTex_ST.x, _DistortionTex_ST.y)))), asfloat(asuint((float2(_DistortionTex_ST.z, _DistortionTex_ST.w))))));
    r0.xy = asuint((asfloat(r2.zw) + asfloat(r0.xy)));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r1.yz) + asfloat(r0.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionRandomUV)))) ? 0xffffffffu : 0u);
    o2.zw = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o3.xyz = asuint((asfloat(v1.xyz) * asfloat(asuint((float3(_ParticleColorBrightness, _ParticleColorBrightness, _ParticleColorBrightness))))));
    o3.w = v1.w;
    o4.xy = v2.zw;
    o4.zw = v3.xy;
    o5.x = v3.z;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.x);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c169);
    float4 _NapEffectBrightnessExtraParams : packoffset(c170);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _MainTexClampU : packoffset(c6.w);
    float _MainTexClampV : packoffset(c7);
    float _ColorChannelMapping : packoffset(c7.w);
    float _LerpValue : packoffset(c8.z);
    float _MaskChannelMapping : packoffset(c9);
    float _MaskTexClampU : packoffset(c9.y);
    float _MaskTexClampV : packoffset(c9.z);
    float _OpaquenessFadeByScript : packoffset(c15.z);
    float _BlendMode : packoffset(c15.w);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
SamplerState sampler_MaskTex;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _MaskTex : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float input5 : TEXCOORD5)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4, v5;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.x = asuint(input5);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampU)))) ? 0xffffffffu : 0u);
    r1.xyzw = asuint(max(asfloat(v1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyzw = asuint(min(asfloat(r1.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampV)))) ? 0xffffffffu : 0u);
    r0.xy = ((r0.xz != 0u) ? r1.xy : v1.xy);
    r0.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r0.xy)).xyzw);
    r1.x = (uint)(asfloat(asuint((_ColorChannelMapping))));
    r1.x = min(r1.x, 0x00000003u);
    r0.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(asuint((_LerpValue))))));
    r2.xyzw = asuint((asfloat(v3.xyzw) + asfloat((v4.xyzw ^ 0x80000000u))));
    r0.xyzw = asuint(mad(asfloat(r0.xxxx), asfloat(r2.xyzw), asfloat(v4.xyzw)));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_MaskTexClampU, _MaskTexClampV))))) ? 0xffffffffu : 0u);
    r1.xy = ((r1.xy != 0u) ? r1.zw : v1.zw);
    r1.xyzw = asuint(_MaskTex.Sample(sampler_MaskTex, asfloat(r1.xy)).xyzw);
    r2.x = (uint)(asfloat(asuint((_MaskChannelMapping))));
    r2.x = min(r2.x, 0x00000003u);
    r1.x = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.x+0].xyzw)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.xyzw = asuint(max(asfloat(r0.wxyz), asfloat(uint4(0x3a83126fu, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
    r1.yzw = asuint(log2(asfloat(r1.yzw)));
    r2.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.x = asuint(mad(asfloat(r2.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.yzw = asuint((asfloat(r1.yzw) * asfloat(r2.xxx)));
    r1.yzw = asuint(exp2(asfloat(r1.yzw)));
    r2.x = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r2.xxx != 0u) ? r1.yzw : r0.xyz);
    r1.yzw = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r1.yzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.xxx), asfloat((r0.xyz ^ 0x80000000u))));
    r3.xyz = ((asfloat(r1.yzw) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.y)));
    r1.x = asuint(max(asfloat(r1.w), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r1.xxx), asfloat(r2.xyz), asfloat(r0.xyz)));
    r1.x = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.xyz) + asfloat((r1.xxx ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.yzw), asfloat(r1.xxx)));
    r1.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.w = ((asfloat(0x3a83126fu) < asfloat((r1.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    o0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    o0.w = r0.w;
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
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainTex_ST : packoffset(c0);
    float4 _MaskTex_ST : packoffset(c1);
    float4 _DissolveTex_ST : packoffset(c2);
    float4 _DistortionTex_ST : packoffset(c3);
    float _ZOffset : packoffset(c6);
    float _MainTexFlip : packoffset(c7.y);
    float _MainTexRotation : packoffset(c7.z);
    float _ParticleColorBrightness : packoffset(c8.y);
    float _UVMove : packoffset(c8.w);
    float _MaskTexFlip : packoffset(c9.w);
    float _MaskTexRotation : packoffset(c10);
    float _MaskUSpeed : packoffset(c10.y);
    float _MaskVSpeed : packoffset(c10.z);
    float _DissolveTexFlip : packoffset(c11.z);
    float _DissolveTexRotation : packoffset(c11.w);
    float _DissolveUSpeed : packoffset(c12);
    float _DissolveVSpeed : packoffset(c12.y);
    float _DissolveRandomUV : packoffset(c12.z);
    float _DistortionTexFlip : packoffset(c13.w);
    float _DistortionTexRotation : packoffset(c14);
    float _DistortionUSpeed : packoffset(c14.y);
    float _DistortionVSpeed : packoffset(c14.z);
    float _DistortionRandomUV : packoffset(c14.w);
    float _TimeOffset : packoffset(c18.y);
    float _IgnoreTimeScale : packoffset(c19);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float output5 : TEXCOORD5;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float2 input4 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, o2, o3, o4, o5, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xy = asuint(input4);
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
    o0.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r1.x = asuint((glstate_matrix_projection[0][0]));
    r1.y = asuint((glstate_matrix_projection[0][2]));
    r1.z = asuint((glstate_matrix_projection[0][3]));
    o0.x = asuint(dot(asfloat(r1.xyz), asfloat(r0.xzw)));
    r1.x = asuint((glstate_matrix_projection[1][1]));
    r1.y = asuint((glstate_matrix_projection[1][2]));
    r1.z = asuint((glstate_matrix_projection[1][3]));
    o0.y = asuint(dot(asfloat(r1.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    o0.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    r0.xyzw = ((asfloat(asuint((float4(_MainTexFlip, _MainTexFlip, _MainTexFlip, _MainTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.xyz = ((r0.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r0.xyzw = (r0.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.yzw = asuint((asfloat(r1.xyz) + asfloat(r0.yzw)));
    r0.yzw = asuint(max(asfloat(r0.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.z = 0x00000000u;
    r3.xyzw = asuint(mad(asfloat(v2.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v2.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat((r4.xyzw ^ 0x80000000u))));
    r5.xyzw = asuint(mad(asfloat(asuint((float4(_MainTexRotation, _MainTexRotation, _MainTexRotation, _MainTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r5.xw;
    r6.xyzw = asuint((asfloat(r0.zzzz) * asfloat(r2.xyzw)));
    r1.z = 0x3f800000u;
    r1.xy = r5.zw;
    r6.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.wwww), asfloat(r6.xyzw)));
    r5.xz = r1.xz;
    r5.w = asuint((_MainTexRotation));
    r1.xyzw = asuint(mad(asfloat(r5.xyzw), asfloat(r0.yyyy), asfloat(r6.xyzw)));
    r2.yw = r5.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r1.x = v3.w;
    r1.y = v4.x;
    r0.zw = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UVMove)))) ? 0xffffffffu : 0u);
    o1.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    r0.w = asuint((asfloat((asuint((_MaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.xyzw = ((asfloat(asuint((float4(_MaskTexFlip, _MaskTexFlip, _MaskTexFlip, _MaskTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r2.xyz = ((r1.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r1.xyzw = (r1.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r2.xyz) + asfloat(r1.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r2.w = asuint((asfloat((asuint((_MaskTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = asuint(mad(asfloat(asuint((float4(_MaskTexRotation, _MaskTexRotation, _MaskTexRotation, _MaskTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r5.xw;
    r2.z = 0x00000000u;
    r6.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.xy = r5.zw;
    r0.z = 0x3f800000u;
    r6.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r6.xyzw)));
    r5.xz = r0.xz;
    r5.w = asuint((_MaskTexRotation));
    r0.xyzw = asuint(mad(asfloat(r5.xyzw), asfloat(r1.yyyy), asfloat(r6.xyzw)));
    r2.yw = r5.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r1.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r1.xy = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_MaskUSpeed, _MaskVSpeed))))));
    r1.xy = asuint(frac(asfloat(r1.xy)));
    o1.zw = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r0.xyw = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r0.xyw = asuint((asfloat(r0.xyw) + asfloat(r5.yzw)));
    r0.xyw = asuint(max(asfloat(r0.xyw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r6.xyzw = asuint(mad(asfloat(asuint((float4(_DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r6.xw;
    r2.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r0.yyyy) * asfloat(r2.xyzw)));
    r1.xy = r6.zw;
    r1.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r0.wwww), asfloat(r4.xyzw)));
    r6.xz = r1.xz;
    r6.w = asuint((_DissolveTexRotation));
    r1.xyzw = asuint(mad(asfloat(r6.xyzw), asfloat(r0.xxxx), asfloat(r4.xyzw)));
    r2.yw = r6.yw;
    r1.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.xxxx), asfloat(r1.xyzw)));
    r0.xy = asuint((asfloat((r1.zw ^ 0x80000000u)) + asfloat(r1.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex_ST.x, _DissolveTex_ST.y)))), asfloat(asuint((float2(_DissolveTex_ST.z, _DissolveTex_ST.w))))));
    r0.xy = asuint((asfloat(r1.zw) + asfloat(r0.xy)));
    r1.xy = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_DissolveUSpeed, _DissolveVSpeed))))));
    r0.zw = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_DistortionUSpeed, _DistortionVSpeed))))));
    r0.zw = asuint(frac(asfloat(r0.zw)));
    r1.xy = asuint(frac(asfloat(r1.xy)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r1.xy)));
    r1.w = v4.y;
    r2.xyz = asuint((asfloat(v4.yyy) * asfloat(uint3(0x41f10a3du, 0x414c28f6u, 0x40490e56u))));
    r1.xyz = asuint(frac(asfloat(r2.xyz)));
    r1.xw = asuint((asfloat(r0.xy) + asfloat(r1.wx)));
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveRandomUV)))) ? 0xffffffffu : 0u);
    o2.xy = ((r2.xx != 0u) ? r1.xw : r0.xy);
    r2.xy = r3.zw;
    r2.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.xy = r3.xw;
    r4.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r6.xyz = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r5.yzw = asuint((asfloat(r6.xyz) + asfloat(r5.yzw)));
    r5.yzw = asuint(max(asfloat(r5.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r4.z = 0x00000000u;
    r6.xyzw = asuint((asfloat(r4.xyzw) * asfloat(r5.zzzz)));
    r2.z = 0x3f800000u;
    r6.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.wwww), asfloat(r6.xyzw)));
    r3.xz = r2.xz;
    r3.w = asuint((_DistortionTexRotation));
    r2.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r5.yyyy), asfloat(r6.xyzw)));
    r4.yw = r3.yw;
    r2.xyzw = asuint(mad(asfloat(r4.xyzw), asfloat(r5.xxxx), asfloat(r2.xyzw)));
    r0.xy = asuint((asfloat((r2.zw ^ 0x80000000u)) + asfloat(r2.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DistortionTex_ST.x, _DistortionTex_ST.y)))), asfloat(asuint((float2(_DistortionTex_ST.z, _DistortionTex_ST.w))))));
    r0.xy = asuint((asfloat(r2.zw) + asfloat(r0.xy)));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r1.yz) + asfloat(r0.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionRandomUV)))) ? 0xffffffffu : 0u);
    o2.zw = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o3.xyz = asuint((asfloat(v1.xyz) * asfloat(asuint((float3(_ParticleColorBrightness, _ParticleColorBrightness, _ParticleColorBrightness))))));
    o3.w = v1.w;
    o4.xy = v2.zw;
    o4.zw = v3.xy;
    o5.x = v3.z;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.x);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c169);
    float4 _NapEffectBrightnessExtraParams : packoffset(c170);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _MainTexClampU : packoffset(c6.w);
    float _MainTexClampV : packoffset(c7);
    float _ColorChannelMapping : packoffset(c7.w);
    float _LerpValue : packoffset(c8.z);
    float _MaskChannelMapping : packoffset(c9);
    float _MaskTexClampU : packoffset(c9.y);
    float _MaskTexClampV : packoffset(c9.z);
    float _OpaquenessFadeByScript : packoffset(c15.z);
    float _BlendMode : packoffset(c15.w);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
SamplerState sampler_MaskTex;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _MaskTex : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float input5 : TEXCOORD5)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4, v5;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.x = asuint(input5);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampU)))) ? 0xffffffffu : 0u);
    r1.xyzw = asuint(max(asfloat(v1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyzw = asuint(min(asfloat(r1.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampV)))) ? 0xffffffffu : 0u);
    r0.xy = ((r0.xz != 0u) ? r1.xy : v1.xy);
    r0.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r0.xy)).xyzw);
    r1.x = (uint)(asfloat(asuint((_ColorChannelMapping))));
    r1.x = min(r1.x, 0x00000003u);
    r0.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(asuint((_LerpValue))))));
    r2.xyzw = asuint((asfloat(v3.xyzw) + asfloat((v4.xyzw ^ 0x80000000u))));
    r0.xyzw = asuint(mad(asfloat(r0.xxxx), asfloat(r2.xyzw), asfloat(v4.xyzw)));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_MaskTexClampU, _MaskTexClampV))))) ? 0xffffffffu : 0u);
    r1.xy = ((r1.xy != 0u) ? r1.zw : v1.zw);
    r1.xyzw = asuint(_MaskTex.Sample(sampler_MaskTex, asfloat(r1.xy)).xyzw);
    r2.x = (uint)(asfloat(asuint((_MaskChannelMapping))));
    r2.x = min(r2.x, 0x00000003u);
    r1.x = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.x+0].xyzw)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.xyzw = asuint(max(asfloat(r0.wxyz), asfloat(uint4(0x3a83126fu, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
    r1.yzw = asuint(log2(asfloat(r1.yzw)));
    r2.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.x = asuint(mad(asfloat(r2.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.yzw = asuint((asfloat(r1.yzw) * asfloat(r2.xxx)));
    r1.yzw = asuint(exp2(asfloat(r1.yzw)));
    r2.x = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r2.xxx != 0u) ? r1.yzw : r0.xyz);
    r1.yzw = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r1.yzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.xxx), asfloat((r0.xyz ^ 0x80000000u))));
    r3.xyz = ((asfloat(r1.yzw) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.y)));
    r1.x = asuint(max(asfloat(r1.w), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r1.xxx), asfloat(r2.xyz), asfloat(r0.xyz)));
    r1.x = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.xyz) + asfloat((r1.xxx ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.yzw), asfloat(r1.xxx)));
    r1.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.w = ((asfloat(0x3a83126fu) < asfloat((r1.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    o0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r0.x = asuint((asfloat(asuint((_BlendMode))) + asfloat(0xbf800000u)));
    o0.w = asuint(mad(asfloat(r0.w), asfloat(r0.x), asfloat(0x3f800000u)));
    result.output0 = asfloat(o0.xyzw);
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
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _DTTex_ST : packoffset(c4);
    float _DtUSpeed : packoffset(c5.z);
    float _DtVSpeed : packoffset(c5.w);
    float _ZOffset : packoffset(c6);
    float _OpaquenessFadeByScript : packoffset(c15.z);
    float _DTIntensity : packoffset(c17.z);
    float _DtUvMove : packoffset(c17.w);
    float _Dist_Intensity_PostProcessing : packoffset(c18);
    float _IgnoreTimeScale : packoffset(c19);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float2 input4 : TEXCOORD2)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xy = asuint(input4);
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
    o0.z = asuint(dot(asfloat(r1.xyzw), asfloat(r0.xyzw)));
    r1.x = asuint((glstate_matrix_projection[0][0]));
    r1.y = asuint((glstate_matrix_projection[0][2]));
    r1.z = asuint((glstate_matrix_projection[0][3]));
    o0.x = asuint(dot(asfloat(r1.xyz), asfloat(r0.xzw)));
    r1.x = asuint((glstate_matrix_projection[1][1]));
    r1.y = asuint((glstate_matrix_projection[1][2]));
    r1.z = asuint((glstate_matrix_projection[1][3]));
    o0.y = asuint(dot(asfloat(r1.xyz), asfloat(r0.yzw)));
    r0.x = asuint((glstate_matrix_projection[3][2]));
    r0.y = asuint((glstate_matrix_projection[3][3]));
    o0.w = asuint(dot(asfloat(r0.xy), asfloat(r0.zw)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.xy = asuint((asfloat(r0.xx) * asfloat(asuint((float2(_DtUSpeed, _DtVSpeed))))));
    r0.xy = asuint(frac(asfloat(r0.xy)));
    r0.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_DTTex_ST.x, _DTTex_ST.y)))), asfloat(asuint((float2(_DTTex_ST.z, _DTTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DtUvMove)))) ? 0xffffffffu : 0u);
    o1.xy = ((r1.xx != 0u) ? r0.xy : r0.zw);
    r0.x = asuint((asfloat(v1.w) * asfloat(asuint((_DTIntensity)))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    o1.z = asuint((asfloat(r0.x) * asfloat(asuint((_Dist_Intensity_PostProcessing)))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    result.output2 = asfloat(o1.z);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    float4 _ZBufferParams : packoffset(c62);
    float4 _ScreenSize : packoffset(c138);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _Distortion : packoffset(c16.y);
    float _DTTexClampU : packoffset(c16.z);
    float _DTTexClampV : packoffset(c16.w);
    float _DistortionMode : packoffset(c19.y);
}


SamplerState sampler_DepthMipChain;
SamplerState sampler_DTTex;
Texture2D<float4> _DepthMipChain : register(t0);
Texture2D<float4> _DTTex : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0, float input2 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, v0, v1;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    v1.z = asuint(input2);
    r0.x = ((asfloat(asuint((_Distortion))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
        o1.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
        result.output0 = asfloat(o0.xyzw);
        result.output1 = asfloat(o1.xyzw);
        return result;
    }
    r0.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_DTTexClampU, _DTTexClampV))))) ? 0xffffffffu : 0u);
    r0.zw = asuint(max(asfloat(v1.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r0.zw = asuint(min(asfloat(r0.zw), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r0.xy = ((r0.xy != 0u) ? r0.zw : v1.xy);
    r0.zw = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r1.xyzw = asuint(_DepthMipChain.SampleLevel(sampler_DepthMipChain, asfloat(r0.zw), asfloat(0x00000000u)).xyzw);
    r1.y = v0.z;
    r0.zw = asuint(mad(asfloat(asuint((float2(_ZBufferParams.z, _ZBufferParams.z)))), asfloat(r1.xy), asfloat(asuint((float2(_ZBufferParams.w, _ZBufferParams.w))))));
    r0.zw = asuint((asfloat(uint2(0x3f800000u, 0x3f800000u)) / asfloat(r0.zw)));
    r0.z = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r0.z)));
    r0.z = ((asfloat(r0.z) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    if (r0.z != 0u) discard;
    r0.xyzw = asuint(_DTTex.Sample(sampler_DTTex, asfloat(r0.xy)).xyzw);
    r0.xy = asuint((asfloat(r0.xy) + asfloat(uint2(0xbefefeffu, 0xbefefeffu))));
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v1.zz)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionMode)))) ? 0xffffffffu : 0u);
    r0.zw = uint2(0x00000000u, 0x3f800000u);
    o0.xyzw = ((r1.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u) : r0.xyzw);
    o1.xyzw = (r0.xyzw & r1.xxxx);
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
