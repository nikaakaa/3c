Shader "miHoYo/Particles/Particles_UVMove"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _USpeed ("U Speed", Float) = 0.0
        _VSpeed ("V Speed", Float) = 0.0
        _UVMove ("UV Move", Float) = 0.0
        _RandomUV ("Random UV", Float) = 0.0
        _UseCustomCurve ("Use Custom Curve", Float) = 0.0
        _Mask ("Mask", 2D) = "white" {}
        _Burst ("Burst", Float) = 1.0
        _TwistTex ("Twist Texture", 2D) = "white" {}
        _TwistStrength ("Twist Strength", Float) = 0.0
        _TwistUSpeedR ("U Speed R", Float) = 0.0
        _TwistVSpeedR ("V Speed R", Float) = 0.0
        _TwistUSpeedG ("U Speed G", Float) = 0.0
        _TwistVSpeedG ("V Speed G", Float) = 0.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.0
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "linearGray" {}
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
        _TimeOffset ("TimeOffset", Float) = 0.0
        _ApplySceneFog ("Apply Scene Fog", Float) = 0.0
        _DebugOctagonShape ("Debug Octagon Shape", Float) = 0.0
        _OctagonClipErrorColor ("Octagon Clip Error Color", Color) = (1.0, 0.0, 1.0, 1.0)
        _DebugOctagonDistortion ("Debug Octagon Distortion", Float) = 0.0
        _UiPreTransformFix ("用于UI层(修复Android上UI特效不显示)", Float) = 0.0
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
    column_major float4x4 glstate_matrix_projection : packoffset(c89);
    column_major float4x4 unity_MatrixV : packoffset(c93);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainTex_ST : packoffset(c0);
    float4 _Mask_ST : packoffset(c1);
    float _USpeed : packoffset(c4);
    float _VSpeed : packoffset(c4.y);
    float _ZOffset : packoffset(c5.y);
    float _UVMove : packoffset(c7.z);
    float _RandomUV : packoffset(c7.w);
    float _UseCustomCurve : packoffset(c8);
    float _IgnoreTimeScale : packoffset(c12.y);
    float _TimeOffset : packoffset(c13);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : COLOR0;
    float4 output2 : TEXCOORD0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float2 input3 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2, v3;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
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
    o1.xyzw = v1.xyzw;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.xy = asuint((asfloat(r0.xx) * asfloat(asuint((float2(_USpeed, _VSpeed))))));
    r0.xy = asuint(frac(asfloat(r0.xy)));
    r0.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_UVMove, _RandomUV))))) ? 0xffffffffu : 0u);
    r0.xy = ((r1.xx != 0u) ? r0.xy : r0.zw);
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v2.zw)));
    r0.xy = ((r1.yy != 0u) ? r0.zw : r0.xy);
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v3.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UseCustomCurve)))) ? 0xffffffffu : 0u);
    o2.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o2.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_Mask_ST.x, _Mask_ST.y)))), asfloat(asuint((float2(_Mask_ST.z, _Mask_ST.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c165);
    float4 _NapEffectBrightnessExtraParams : packoffset(c166);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _Burst : packoffset(c4.z);
    float4 _TintColor : packoffset(c6);
    float _OpaquenessFadeByScript : packoffset(c11);
    float _BlendMode : packoffset(c12);
}


SamplerState sampler_MainTex;
SamplerState sampler_Mask;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _Mask : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.xyzw = asuint(_Mask.Sample(sampler_Mask, asfloat(v2.zw)).xyzw);
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(v2.xy)).xyzw);
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(v1.xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(asuint((float4(_TintColor.x, _TintColor.y, _TintColor.z, _TintColor.w))))));
    r0.w = asuint((asfloat(r0.x) * asfloat(r1.w)));
    r0.xyz = asuint((asfloat(r1.xyz) * asfloat(asuint((float3(_Burst, _Burst, _Burst))))));
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
    r0.w = asuint(saturate(asfloat(r0.w)));
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
    column_major float4x4 glstate_matrix_projection : packoffset(c89);
    column_major float4x4 unity_MatrixV : packoffset(c93);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainTex_ST : packoffset(c0);
    float4 _Mask_ST : packoffset(c1);
    float _USpeed : packoffset(c4);
    float _VSpeed : packoffset(c4.y);
    float _ZOffset : packoffset(c5.y);
    float _UVMove : packoffset(c7.z);
    float _RandomUV : packoffset(c7.w);
    float _UseCustomCurve : packoffset(c8);
    float _IgnoreTimeScale : packoffset(c12.y);
    float _TimeOffset : packoffset(c13);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : COLOR0;
    float4 output2 : TEXCOORD0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float2 input3 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2, v3;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
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
    o1.xyzw = v1.xyzw;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.xy = asuint((asfloat(r0.xx) * asfloat(asuint((float2(_USpeed, _VSpeed))))));
    r0.xy = asuint(frac(asfloat(r0.xy)));
    r0.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_UVMove, _RandomUV))))) ? 0xffffffffu : 0u);
    r0.xy = ((r1.xx != 0u) ? r0.xy : r0.zw);
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v2.zw)));
    r0.xy = ((r1.yy != 0u) ? r0.zw : r0.xy);
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v3.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UseCustomCurve)))) ? 0xffffffffu : 0u);
    o2.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o2.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_Mask_ST.x, _Mask_ST.y)))), asfloat(asuint((float2(_Mask_ST.z, _Mask_ST.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c165);
    float4 _NapEffectBrightnessExtraParams : packoffset(c166);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _Burst : packoffset(c4.z);
    float4 _TintColor : packoffset(c6);
    float _OpaquenessFadeByScript : packoffset(c11);
    float _BlendMode : packoffset(c12);
}


SamplerState sampler_MainTex;
SamplerState sampler_Mask;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _Mask : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.x = asuint(_Mask.Sample(sampler_Mask, asfloat(v2.zw)).x);
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(v2.xy)).xyzw);
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(v1.xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(asuint((float4(_TintColor.x, _TintColor.y, _TintColor.z, _TintColor.w))))));
    r0.x = asuint((asfloat(r0.x) * asfloat(r1.w)));
    r0.yzw = asuint((asfloat(r1.xyz) * asfloat(asuint((float3(_Burst, _Burst, _Burst))))));
    r1.xyzw = asuint(max(asfloat(r0.xyzw), asfloat(uint4(0x3a83126fu, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
    r1.yzw = asuint(log2(asfloat(r1.yzw)));
    r2.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.x = asuint(mad(asfloat(r2.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.yzw = asuint((asfloat(r1.yzw) * asfloat(r2.xxx)));
    r1.yzw = asuint(exp2(asfloat(r1.yzw)));
    r2.x = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.yzw = ((r2.xxx != 0u) ? r1.yzw : r0.yzw);
    r1.yzw = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r0.x = asuint(saturate(asfloat(r0.x)));
    r2.xyz = asuint(mad(asfloat(r1.yzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.xxx), asfloat((r0.yzw ^ 0x80000000u))));
    r3.xyz = ((asfloat(r1.yzw) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.yzw)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.yzw ^ 0x80000000u))));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.y)));
    r1.x = asuint(max(asfloat(r1.w), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(r2.xyz), asfloat(r0.yzw)));
    r1.x = asuint(dot(asfloat(r0.yzw), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.yzw) + asfloat((r1.xxx ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.yzw), asfloat(r1.xxx)));
    r1.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.w = ((asfloat(0x3a83126fu) < asfloat((r1.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.yzw = ((r1.www != 0u) ? r1.xyz : r0.yzw);
    o0.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r0.y = asuint((asfloat(asuint((_BlendMode))) + asfloat(0xbf800000u)));
    o0.w = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat(0x3f800000u)));
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
    column_major float4x4 glstate_matrix_projection : packoffset(c89);
    column_major float4x4 unity_MatrixV : packoffset(c93);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _DTTex_ST : packoffset(c3);
    float _DtUSpeed : packoffset(c4.w);
    float _DtVSpeed : packoffset(c5);
    float _ZOffset : packoffset(c5.y);
    float _DTIntensity : packoffset(c9.z);
    float _DtUvMove : packoffset(c10);
    float _Dist_Intensity_PostProcessing : packoffset(c10.y);
    float _OpaquenessFadeByScript : packoffset(c11);
    float _IgnoreTimeScale : packoffset(c12.y);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float2 input3 : TEXCOORD1)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
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
    r1.x = asuint((asfloat(r0.x) * asfloat(asuint((_DtUSpeed)))));
    r1.y = asuint((asfloat(r0.x) * asfloat(asuint((_DtVSpeed)))));
    r0.xy = asuint(frac(asfloat(r1.xy)));
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
    float4 _ZBufferParams : packoffset(c61);
    float4 _ScreenSize : packoffset(c137);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _Distortion : packoffset(c9.w);
    float _DistortionMode : packoffset(c12.z);
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
    r0.xy = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r0.xyzw = asuint(_DepthMipChain.SampleLevel(sampler_DepthMipChain, asfloat(r0.xy), asfloat(0x00000000u)).xyzw);
    r0.y = v0.z;
    r0.xy = asuint(mad(asfloat(asuint((float2(_ZBufferParams.z, _ZBufferParams.z)))), asfloat(r0.xy), asfloat(asuint((float2(_ZBufferParams.w, _ZBufferParams.w))))));
    r0.xy = asuint((asfloat(uint2(0x3f800000u, 0x3f800000u)) / asfloat(r0.xy)));
    r0.x = asuint((asfloat((r0.y ^ 0x80000000u)) + asfloat(r0.x)));
    r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) discard;
    r0.xyzw = asuint(_DTTex.Sample(sampler_DTTex, asfloat(v1.xy)).xyzw);
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
