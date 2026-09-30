Shader "miHoYo/Particles/Particles_CrossShining"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex ("Main Tex", 2D) = "white" {}
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _MainTexFlip ("Flip", Float) = 0.0
        _MainTexRotation ("Rotation", Float) = 0.0
        _ColorChannelMapping ("Color Channel Mapping", Float) = 4.0
        _AlphaChannelMapping ("Alpha Channel Mapping", Float) = 0.0
        _MaskSize ("Mask Size", Float) = 0.0
        _BlendMode ("Blend Mode", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _UiPreTransformFix ("用于UI层(修复Android上UI特效不显示)", Float) = 0.0
        _Cull ("Cull", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ZTest ("Render On Top", Float) = 4.0
        _ZOffset ("Z Offset", Float) = 0.0
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
    float4 _MainColor : packoffset(c1);
    float _ZOffset : packoffset(c2.w);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float3 output1 : TEXCOORD0;
    float4 output2 : COLOR0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : TEXCOORD0, float4 input2 : COLOR0)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
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
    o1.xyz = v1.xyz;
    o2.xyzw = asuint((asfloat(v2.xyzw) * asfloat(asuint((float4(_MainColor.x, _MainColor.y, _MainColor.z, _MainColor.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyz);
    result.output2 = asfloat(o2.xyzw);
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
    float4 _MainTex_ST : packoffset(c0);
    float _ColorChannelMapping : packoffset(c2);
    float _AlphaChannelMapping : packoffset(c2.y);
    float _MaskSize : packoffset(c2.z);
    float _MainTexClampU : packoffset(c3.y);
    float _MainTexClampV : packoffset(c3.z);
    float _MainTexFlip : packoffset(c3.w);
    float _MainTexRotation : packoffset(c4);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float3 input1 : TEXCOORD0, float4 input2 : COLOR0)
{
    uint4 r0, r1, r2, r3, r4, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.xyzw = ((asfloat(asuint((float4(_MainTexFlip, _MainTexFlip, _MainTexFlip, _MainTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r3.xyz = ((r2.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r2.xyzw = (r2.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.yzw = asuint((asfloat(r3.xyz) + asfloat(r2.yzw)));
    r2.yzw = asuint(max(asfloat(r2.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xyzw = asuint(mad(asfloat(v1.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v1.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat((r4.xyzw ^ 0x80000000u))));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_MainTexRotation, _MainTexRotation, _MainTexRotation, _MainTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r1.xy = r3.xw;
    r1.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r1.xyzw) * asfloat(r2.zzzz)));
    r0.xy = r3.zw;
    r0.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r2.wwww), asfloat(r4.xyzw)));
    r3.xz = r0.xz;
    r3.w = asuint((_MainTexRotation));
    r0.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r2.yyyy), asfloat(r4.xyzw)));
    r1.yw = r3.yw;
    r0.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r2.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint(max(asfloat(r0.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r0.zw = asuint(min(asfloat(r0.zw), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_MainTexClampU, _MainTexClampV))))) ? 0xffffffffu : 0u);
    r0.xy = ((r1.xy != 0u) ? r0.zw : r0.xy);
    r0.xy = asuint((asfloat(r0.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v1.zz)));
    r0.zw = asuint((asfloat(v1.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r0.z = asuint(dot(asfloat(r0.zw), asfloat(r0.zw)));
    r0.z = asuint(sqrt(asfloat(r0.z)));
    r0.z = asuint((asfloat((r0.z ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(saturate((asfloat(r0.z) + asfloat((asuint((_MaskSize)) ^ 0x80000000u)))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(r0.zz), asfloat(v1.xy)));
    r0.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r0.xy)).xyzw);
    r1.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r1.xy = min(r1.xy, uint2(0x00000003u, 0x00000003u));
    r1.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.w = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.y+0].xyzw)));
    r1.y = ((asfloat(0x40600000u) < asfloat(asuint((_ColorChannelMapping)))) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.yyy != 0u) ? r0.xyz : r1.xxx);
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(v2.xyzw)));
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
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainColor : packoffset(c1);
    float _ZOffset : packoffset(c2.w);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float3 output1 : TEXCOORD0;
    float4 output2 : COLOR0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : TEXCOORD0, float4 input2 : COLOR0)
{
    uint4 r0, r1, o0, o1, o2, v0, v1, v2;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
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
    o1.xyz = v1.xyz;
    o2.xyzw = asuint((asfloat(v2.xyzw) * asfloat(asuint((float4(_MainColor.x, _MainColor.y, _MainColor.z, _MainColor.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyz);
    result.output2 = asfloat(o2.xyzw);
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
    float4 _MainTex_ST : packoffset(c0);
    float _ColorChannelMapping : packoffset(c2);
    float _AlphaChannelMapping : packoffset(c2.y);
    float _MaskSize : packoffset(c2.z);
    float _BlendMode : packoffset(c3);
    float _MainTexClampU : packoffset(c3.y);
    float _MainTexClampV : packoffset(c3.z);
    float _MainTexFlip : packoffset(c3.w);
    float _MainTexRotation : packoffset(c4);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
Texture2D<float4> _MainTex : register(t0);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float3 input1 : TEXCOORD0, float4 input2 : COLOR0)
{
    uint4 r0, r1, r2, r3, r4, o0, v0, v1, v2;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.xyzw = ((asfloat(asuint((float4(_MainTexFlip, _MainTexFlip, _MainTexFlip, _MainTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r3.xyz = ((r2.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r2.xyzw = (r2.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.yzw = asuint((asfloat(r3.xyz) + asfloat(r2.yzw)));
    r2.yzw = asuint(max(asfloat(r2.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xyzw = asuint(mad(asfloat(v1.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v1.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat((r4.xyzw ^ 0x80000000u))));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_MainTexRotation, _MainTexRotation, _MainTexRotation, _MainTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r1.xy = r3.xw;
    r1.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r1.xyzw) * asfloat(r2.zzzz)));
    r0.xy = r3.zw;
    r0.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r2.wwww), asfloat(r4.xyzw)));
    r3.xz = r0.xz;
    r3.w = asuint((_MainTexRotation));
    r0.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r2.yyyy), asfloat(r4.xyzw)));
    r1.yw = r3.yw;
    r0.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r2.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint(max(asfloat(r0.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r0.zw = asuint(min(asfloat(r0.zw), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r1.xy = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_MainTexClampU, _MainTexClampV))))) ? 0xffffffffu : 0u);
    r0.xy = ((r1.xy != 0u) ? r0.zw : r0.xy);
    r0.xy = asuint((asfloat(r0.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v1.zz)));
    r0.zw = asuint((asfloat(v1.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r0.z = asuint(dot(asfloat(r0.zw), asfloat(r0.zw)));
    r0.z = asuint(sqrt(asfloat(r0.z)));
    r0.z = asuint((asfloat((r0.z ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(saturate((asfloat(r0.z) + asfloat((asuint((_MaskSize)) ^ 0x80000000u)))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(r0.zz), asfloat(v1.xy)));
    r0.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r0.xy)).xyzw);
    r1.xy = (uint2)(asfloat(asuint((float2(_ColorChannelMapping, _AlphaChannelMapping)))));
    r1.xy = min(r1.xy, uint2(0x00000003u, 0x00000003u));
    r1.x = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.w = asuint(dot(asfloat(r0.xyzw), asfloat(icb[r1.y+0].xyzw)));
    r1.y = ((asfloat(0x40600000u) < asfloat(asuint((_ColorChannelMapping)))) ? 0xffffffffu : 0u);
    r0.xyz = ((r1.yyy != 0u) ? r0.xyz : r1.xxx);
    r0.xyzw = asuint((asfloat(r0.xyzw) * asfloat(v2.xyzw)));
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
    }
}
