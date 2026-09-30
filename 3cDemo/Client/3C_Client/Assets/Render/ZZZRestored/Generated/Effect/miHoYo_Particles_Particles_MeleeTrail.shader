Shader "miHoYo/Particles/Particles_MeleeTrail"
{
    Properties
    {
        _BlendMode ("Blend Mode", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _Cull ("Cull", Float) = 0.0
        _MainTex ("Main Tex", 2D) = "white" {}
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _ColorChannelMapping ("Color Channel Mapping", Float) = 0.0
        _AlphaChannelMapping ("Alpha Channel Mapping", Float) = 0.0
        _MultiplyParticleColor ("Multiply Particle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Mask ("Mask", Float) = 0.0
        _MaskTex ("Mask Tex", 2D) = "white" {}
        _MaskTexClampU ("Clamp U", Float) = 0.0
        _MaskTexClampV ("Clamp V", Float) = 0.0
        _MaskChannelMapping ("Mask Channel Mapping", Float) = 0.0
        _MaskMoveAxis ("Mask Move Axis", Float) = 0.0
        _Dissolve ("Dissolve", Float) = 0.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _SoftRange ("Soft Range", Float) = 0.0
        _UsePunctualLighting ("Use Punctual Lighting", Float) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _LightIntensity ("Light Intensity", Float) = 1.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _AlphaFade ("Alpha Fade", Float) = 1.0
        _ZOffset ("Z Offset", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ZTest ("Render On Top", Float) = 4.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
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
    float4 _MultiplyParticleColor : packoffset(c5);
    float _SoftRange : packoffset(c8.z);
    float _ZOffset : packoffset(c9.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float2 input3 : TEXCOORD2, float4 input4 : COLOR0)
{
    uint4 r0, r1, o0, o1, o2, o3, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
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
    r0.xy = asuint(mad(asfloat(v1.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    o1.xy = asuint((asfloat(r0.xy) + asfloat(v1.zw)));
    r0.x = asuint(max(asfloat(asuint((_SoftRange))), asfloat(0x38d1b717u)));
    r0.y = asuint((asfloat(r0.x) + asfloat(0x3f800000u)));
    o2.w = asuint(mad(asfloat(v2.x), asfloat(r0.y), asfloat((r0.x ^ 0x80000000u))));
    o2.xy = v2.zw;
    o2.z = v3.x;
    o3.xyz = asuint((asfloat(v4.xyz) * asfloat(asuint((float3(_MultiplyParticleColor.x, _MultiplyParticleColor.y, _MultiplyParticleColor.z))))));
    o3.w = asuint((asfloat(v3.y) * asfloat(v4.w)));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
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
    float _MainTexClampU : packoffset(c6);
    float _MainTexClampV : packoffset(c6.y);
    float _ColorChannelMapping : packoffset(c6.z);
    float _AlphaChannelMapping : packoffset(c6.w);
    float _AlphaFade : packoffset(c8.w);
    float _BlendMode : packoffset(c9);
    float _OpaquenessFadeByScript : packoffset(c9.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    r0.xyz = asuint((asfloat((v2.xyz ^ 0x80000000u)) + asfloat(v3.xyz)));
    r1.xy = asuint(max(asfloat(v1.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r1.xy = asuint(min(asfloat(r1.xy), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r2.xyz = ((asfloat(uint3(0x3f000000u, 0x3f000000u, 0x40600000u)) < asfloat(asuint((float3(_MainTexClampU, _MainTexClampV, _ColorChannelMapping))))) ? 0xffffffffu : 0u);
    r1.xy = ((r2.xy != 0u) ? r1.xy : v1.xy);
    r1.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r1.xy)).xyzw);
    r2.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r2.xy = min(r2.xy, uint2(0x00000003u, 0x00000003u));
    r0.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.x+0].xyzw)));
    r1.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.y+0].xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(v3.xyzw)));
    r1.w = asuint((asfloat(r1.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r0.xyz), asfloat(v2.xyz)));
    r0.xyz = ((r2.zzz != 0u) ? r1.xyz : r0.xyz);
    r1.xyz = asuint(max(asfloat(r0.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyz = asuint(log2(asfloat(r1.xyz)));
    r0.w = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.w = asuint(mad(asfloat(r0.w), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(r0.www)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r0.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    r1.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r0.w = asuint(max(asfloat(r1.w), asfloat(0x3a83126fu)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(asuint((_AlphaFade))))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r0.www), asfloat((r0.xyz ^ 0x80000000u))));
    r3.xyz = ((asfloat(r1.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r0.w = asuint(max(asfloat(r1.y), asfloat(r1.x)));
    r0.w = asuint(max(asfloat(r1.z), asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(r0.xyz)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.xyz = asuint((asfloat((r0.www ^ 0x80000000u)) + asfloat(r0.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.xyz), asfloat(r0.www)));
    r0.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r0.w = ((asfloat(0x3a83126fu) < asfloat((r0.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    o0.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    o0.w = r1.w;
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
    float4 _MultiplyParticleColor : packoffset(c5);
    float _SoftRange : packoffset(c8.z);
    float _ZOffset : packoffset(c9.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
};

CorinVertexOut CorinVertex(float4 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float2 input3 : TEXCOORD2, float4 input4 : COLOR0)
{
    uint4 r0, r1, o0, o1, o2, o3, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xy = asuint(input3);
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
    r0.xy = asuint(mad(asfloat(v1.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    o1.xy = asuint((asfloat(r0.xy) + asfloat(v1.zw)));
    r0.x = asuint(max(asfloat(asuint((_SoftRange))), asfloat(0x38d1b717u)));
    r0.y = asuint((asfloat(r0.x) + asfloat(0x3f800000u)));
    o2.w = asuint(mad(asfloat(v2.x), asfloat(r0.y), asfloat((r0.x ^ 0x80000000u))));
    o2.xy = v2.zw;
    o2.z = v3.x;
    o3.xyz = asuint((asfloat(v4.xyz) * asfloat(asuint((float3(_MultiplyParticleColor.x, _MultiplyParticleColor.y, _MultiplyParticleColor.z))))));
    o3.w = asuint((asfloat(v3.y) * asfloat(v4.w)));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xy);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
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
    float _MainTexClampU : packoffset(c6);
    float _MainTexClampV : packoffset(c6.y);
    float _ColorChannelMapping : packoffset(c6.z);
    float _AlphaChannelMapping : packoffset(c6.w);
    float _AlphaFade : packoffset(c8.w);
    float _BlendMode : packoffset(c9);
    float _OpaquenessFadeByScript : packoffset(c9.y);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    r0.xyz = asuint((asfloat((v2.xyz ^ 0x80000000u)) + asfloat(v3.xyz)));
    r1.xy = asuint(max(asfloat(v1.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r1.xy = asuint(min(asfloat(r1.xy), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r2.xyz = ((asfloat(uint3(0x3f000000u, 0x3f000000u, 0x40600000u)) < asfloat(asuint((float3(_MainTexClampU, _MainTexClampV, _ColorChannelMapping))))) ? 0xffffffffu : 0u);
    r1.xy = ((r2.xy != 0u) ? r1.xy : v1.xy);
    r1.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r1.xy)).xyzw);
    r2.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r2.xy = min(r2.xy, uint2(0x00000003u, 0x00000003u));
    r0.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.x+0].xyzw)));
    r1.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r2.y+0].xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) * asfloat(v3.xyzw)));
    r1.w = asuint((asfloat(r1.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r0.xyz), asfloat(v2.xyz)));
    r0.xyz = ((r2.zzz != 0u) ? r1.xyz : r0.xyz);
    r1.xyz = asuint(max(asfloat(r0.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyz = asuint(log2(asfloat(r1.xyz)));
    r0.w = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.w = asuint(mad(asfloat(r0.w), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(r0.www)));
    r1.xyz = asuint(exp2(asfloat(r1.xyz)));
    r0.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    r1.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r0.w = asuint(max(asfloat(r1.w), asfloat(0x3a83126fu)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(asuint((_AlphaFade))))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r0.www), asfloat((r0.xyz ^ 0x80000000u))));
    r3.xyz = ((asfloat(r1.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.xyz)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.xyz ^ 0x80000000u))));
    r0.w = asuint(max(asfloat(r1.y), asfloat(r1.x)));
    r0.w = asuint(max(asfloat(r1.z), asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.xyz = asuint(mad(asfloat(r0.www), asfloat(r2.xyz), asfloat(r0.xyz)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.xyz = asuint((asfloat((r0.www ^ 0x80000000u)) + asfloat(r0.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.xyz), asfloat(r0.www)));
    r0.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r0.w = ((asfloat(0x3a83126fu) < asfloat((r0.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    o0.xyz = asuint((asfloat(r1.www) * asfloat(r0.xyz)));
    r0.x = asuint((asfloat(asuint((_BlendMode))) + asfloat(0xbf800000u)));
    o0.w = asuint(mad(asfloat(r1.w), asfloat(r0.x), asfloat(0x3f800000u)));
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
