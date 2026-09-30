Shader "miHoYo/Particles/Particles_Dissolve_Edge"
{
    Properties
    {
        _Dissolve ("Dissolve", Float) = 0.0
        _DissolveNoise ("Dissolve Noise", 2D) = "white" {}
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _DissolveRandomUV ("Random UV", Float) = 0.0
        _DissolveUSpeed ("U Speed", Float) = 0.0
        _DissolveVSpeed ("V Speed", Float) = 0.0
        _EnableEdge ("Enable Edge", Float) = 0.0
        _EdgeRate ("Edge Rate", Float) = 0.01
        _TwistTex ("Twist Texture", 2D) = "white" {}
        _TwistStrength ("Twist Strength", Float) = 0.0
        _TwistUSpeedR ("U Speed R", Float) = 0.0
        _TwistVSpeedR ("V Speed R", Float) = 0.0
        _TwistUSpeedG ("U Speed G", Float) = 0.0
        _TwistVSpeedG ("V Speed G", Float) = 0.0
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskChannel ("Mask Channel", Float) = 0.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.0
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "linearGray" {}
        _DTIntensity ("Distortion Intensity", Float) = 5.0
        _DtUvMove ("Distortion UV Move", Float) = 0.0
        _DtUSpeed ("Distortion U Speed", Float) = 1.0
        _DtVSpeed ("Distortion V Speed", Float) = 1.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
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
    float4 _MaskTex_ST : packoffset(c0);
    float4 _DissolveNoise_ST : packoffset(c2);
    float _ZOffset : packoffset(c3);
    float _DissolveRandomUV : packoffset(c3.y);
    float _DissolveUSpeed : packoffset(c3.z);
    float _DissolveVSpeed : packoffset(c3.w);
    float _IgnoreTimeScale : packoffset(c5);
    float _TimeOffset : packoffset(c11);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float output4 : TEXCOORD4;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float4 input4 : TEXCOORD2)
{
    uint4 r0, r1, o0, o1, o2, o3, o4, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
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
    o1.w = v1.w;
    r0.xyzw = asuint((asfloat(v1.wxyz) * asfloat(v3.ywww)));
    o1.xyz = r0.yzw;
    o2.w = r0.x;
    o2.xy = v2.zw;
    o2.z = v3.x;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveRandomUV)))) ? 0xffffffffu : 0u);
    r0.xy = (r0.xx & v4.zw);
    r0.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_DissolveNoise_ST.x, _DissolveNoise_ST.y)))), asfloat(asuint((float2(_DissolveNoise_ST.z, _DissolveNoise_ST.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    o3.zw = asuint(mad(asfloat(r0.zz), asfloat(asuint((float2(_DissolveUSpeed, _DissolveVSpeed)))), asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    o3.xy = asuint((asfloat(r0.xy) + asfloat(v4.xy)));
    o4.x = v3.z;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.x);
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
    float _Dissolve : packoffset(c5.y);
    float _DissolveChannel : packoffset(c5.z);
    float _EnableEdge : packoffset(c6);
    float _EdgeRate : packoffset(c6.y);
    float _MaskChannel : packoffset(c6.z);
    float _OpaquenessFadeByScript : packoffset(c7);
    float _BlendMode : packoffset(c7.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MaskTex;
SamplerState sampler_DissolveNoise;
Texture2D<float4> _MaskTex : register(t0);
Texture2D<float4> _DissolveNoise : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float input4 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.x = asuint(input4);
    r0.x = (uint)(asfloat(asuint((_DissolveChannel))));
    r0.x = min(r0.x, 0x00000003u);
    r1.xyzw = asuint(_DissolveNoise.Sample(sampler_DissolveNoise, asfloat(v3.zw)).xyzw);
    r0.x = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.x+0].xyzw)));
    r0.y = asuint((asfloat(r0.x) + asfloat(asuint((_EdgeRate)))));
    r0.yz = ((asfloat(r0.yx) >= asfloat(v4.xx)) ? 0xffffffffu : 0u);
    r1.xyz = asuint((asfloat(r0.xxx) * asfloat(v1.xyz)));
    r0.x = ((r0.z != 0u) ? 0xbf800000u : 0x80000000u);
    r0.yz = (r0.yz & uint2(0x3f800000u, 0x3f800000u));
    r0.x = asuint((asfloat(r0.x) + asfloat(r0.y)));
    r0.x = asuint(max(asfloat(r0.x), asfloat(0x00000000u)));
    r2.xyz = asuint((asfloat(r0.xxx) * asfloat(v2.xyz)));
    r3.xyz = asuint(mad(asfloat((v2.xyz ^ 0x80000000u)), asfloat(r0.xxx), asfloat(v1.xyz)));
    r2.xyz = asuint(mad(asfloat(r0.zzz), asfloat(r3.xyz), asfloat(r2.xyz)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_EnableEdge)))) ? 0xffffffffu : 0u);
    r2.xyz = ((r0.xxx != 0u) ? r2.xyz : v1.xyz);
    r0.x = asuint((asfloat(v1.w) + asfloat((v2.w ^ 0x80000000u))));
    r0.x = asuint(mad(asfloat(r0.z), asfloat(r0.x), asfloat(v2.w)));
    r2.w = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_Dissolve)))) ? 0xffffffffu : 0u);
    r1.w = v1.w;
    r0.xyzw = ((r0.xxxx != 0u) ? r2.xyzw : r1.xyzw);
    r1.x = (uint)(asfloat(asuint((_MaskChannel))));
    r1.x = min(r1.x, 0x00000003u);
    r2.xyzw = asuint(_MaskTex.Sample(sampler_MaskTex, asfloat(v3.xy)).xyzw);
    r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(r1.xxxx)));
    r1.xyz = asuint(max(asfloat(r0.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyz = asuint(log2(asfloat(r1.xyz)));
    r1.w = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.w = asuint(mad(asfloat(r1.w), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(r1.www)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r1.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = asuint(max(asfloat(r0.w), asfloat(0x3a83126fu)));
    r1.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
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
    float4 _MaskTex_ST : packoffset(c0);
    float4 _DissolveNoise_ST : packoffset(c2);
    float _ZOffset : packoffset(c3);
    float _DissolveRandomUV : packoffset(c3.y);
    float _DissolveUSpeed : packoffset(c3.z);
    float _DissolveVSpeed : packoffset(c3.w);
    float _IgnoreTimeScale : packoffset(c5);
    float _TimeOffset : packoffset(c11);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float output4 : TEXCOORD4;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float4 input4 : TEXCOORD2)
{
    uint4 r0, r1, o0, o1, o2, o3, o4, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
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
    o1.w = v1.w;
    r0.xyzw = asuint((asfloat(v1.wxyz) * asfloat(v3.ywww)));
    o1.xyz = r0.yzw;
    o2.w = r0.x;
    o2.xy = v2.zw;
    o2.z = v3.x;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveRandomUV)))) ? 0xffffffffu : 0u);
    r0.xy = (r0.xx & v4.zw);
    r0.zw = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_DissolveNoise_ST.x, _DissolveNoise_ST.y)))), asfloat(asuint((float2(_DissolveNoise_ST.z, _DissolveNoise_ST.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    o3.zw = asuint(mad(asfloat(r0.zz), asfloat(asuint((float2(_DissolveUSpeed, _DissolveVSpeed)))), asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(v2.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    o3.xy = asuint((asfloat(r0.xy) + asfloat(v4.xy)));
    o4.x = v3.z;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.x);
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
    float _Dissolve : packoffset(c5.y);
    float _DissolveChannel : packoffset(c5.z);
    float _EnableEdge : packoffset(c6);
    float _EdgeRate : packoffset(c6.y);
    float _MaskChannel : packoffset(c6.z);
    float _OpaquenessFadeByScript : packoffset(c7);
    float _BlendMode : packoffset(c7.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MaskTex;
SamplerState sampler_DissolveNoise;
Texture2D<float4> _MaskTex : register(t0);
Texture2D<float4> _DissolveNoise : register(t1);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float input4 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.x = asuint(input4);
    r0.x = (uint)(asfloat(asuint((_DissolveChannel))));
    r0.x = min(r0.x, 0x00000003u);
    r1.xyzw = asuint(_DissolveNoise.Sample(sampler_DissolveNoise, asfloat(v3.zw)).xyzw);
    r0.x = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.x+0].xyzw)));
    r0.y = asuint((asfloat(r0.x) + asfloat(asuint((_EdgeRate)))));
    r0.yz = ((asfloat(r0.yx) >= asfloat(v4.xx)) ? 0xffffffffu : 0u);
    r1.xyz = asuint((asfloat(r0.xxx) * asfloat(v1.xyz)));
    r0.x = ((r0.z != 0u) ? 0xbf800000u : 0x80000000u);
    r0.yz = (r0.yz & uint2(0x3f800000u, 0x3f800000u));
    r0.x = asuint((asfloat(r0.x) + asfloat(r0.y)));
    r0.x = asuint(max(asfloat(r0.x), asfloat(0x00000000u)));
    r2.xyz = asuint((asfloat(r0.xxx) * asfloat(v2.xyz)));
    r3.xyz = asuint(mad(asfloat((v2.xyz ^ 0x80000000u)), asfloat(r0.xxx), asfloat(v1.xyz)));
    r2.xyz = asuint(mad(asfloat(r0.zzz), asfloat(r3.xyz), asfloat(r2.xyz)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_EnableEdge)))) ? 0xffffffffu : 0u);
    r2.xyz = ((r0.xxx != 0u) ? r2.xyz : v1.xyz);
    r0.x = asuint((asfloat(v1.w) + asfloat((v2.w ^ 0x80000000u))));
    r0.x = asuint(mad(asfloat(r0.z), asfloat(r0.x), asfloat(v2.w)));
    r2.w = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_Dissolve)))) ? 0xffffffffu : 0u);
    r1.w = v1.w;
    r0.xyzw = ((r0.xxxx != 0u) ? r2.xyzw : r1.xyzw);
    r1.x = (uint)(asfloat(asuint((_MaskChannel))));
    r1.x = min(r1.x, 0x00000003u);
    r2.xyzw = asuint(_MaskTex.Sample(sampler_MaskTex, asfloat(v3.xy)).xyzw);
    r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(r1.xxxx)));
    r1.xyz = asuint(max(asfloat(r0.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyz = asuint(log2(asfloat(r1.xyz)));
    r1.w = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.w = asuint(mad(asfloat(r1.w), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(r1.www)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r1.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.www != 0u) ? r1.xyz : r0.xyz);
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = asuint(max(asfloat(r0.w), asfloat(0x3a83126fu)));
    r1.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
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
    float _ZOffset : packoffset(c3);
    float _IgnoreTimeScale : packoffset(c5);
    float _OpaquenessFadeByScript : packoffset(c7);
    float4 _DTTex_ST : packoffset(c8);
    float _DTIntensity : packoffset(c9);
    float _DtUvMove : packoffset(c9.z);
    float _DtUSpeed : packoffset(c9.w);
    float _DtVSpeed : packoffset(c10);
    float _Dist_Intensity_PostProcessing : packoffset(c10.y);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float4 input4 : TEXCOORD2)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
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
    float4 _ZBufferParams : packoffset(c62);
    float4 _ScreenSize : packoffset(c138);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _Distortion : packoffset(c9.y);
    float _DistortionMode : packoffset(c10.z);
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
