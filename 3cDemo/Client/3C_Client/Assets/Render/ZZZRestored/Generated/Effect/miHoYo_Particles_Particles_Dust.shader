Shader "miHoYo/Particles/Particles_Dust"
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
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.0
        _MainTex ("Particle Texture", 2D) = "white" {}
        _MainTexUVSpeed ("Main Tex UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _UVMoveUsingCustomData ("UV Move Using CustomData", Float) = 0.0
        _ColorChannelMapping ("Color Channel Mapping", Float) = 0.0
        _AlphaChannelMapping ("Alpha Channel Mapping", Float) = 0.0
        _MultiplyParticleColor ("Multiply Particle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LerpBrightness ("Lerp Brightness", Float) = 1.0
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _MainTexFlip ("Flip", Float) = 0.0
        _MainTexRotation ("Rotation", Float) = 0.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _DissolveUVSpeed ("Dissolve UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DissolveRandomUV ("Dissolve Random UV", Float) = 0.0
        _DissolveAffects2Tone ("Dissolve Affects 2 Tone", Float) = 0.0
        _SoftRange ("Soft Range", Float) = 0.1
        _UsingAlphaAsDissolve ("Using Alpha As Dissolve", Float) = 1.0
        _DistortionTex ("Distortion Tex", 2D) = "bump" {}
        _DistortionChannel ("Distortion Channel", Float) = 0.0
        _DistortionUVSpeed ("Distortion UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DistortionRandomUV ("Distortion Random UV", Float) = 0.0
        _DistortionIntensity ("Distortion Intensity", Float) = 0.0
        _DissolveDistortionIntensity ("Dissolve Distortion Intensity", Float) = 0.0
        _VertexExtrusionTex ("Vertex Extrusion Tex", 2D) = "black" {}
        _VertexExtrusionIntensityChannel ("Vertex Extrusion Intensity Channel", Float) = 0.0
        _VertexExtrusionIntensityChannel2 ("Vertex Extrusion Intensity Channel 2", Float) = 1.0
        _VertexExtrusionIntensity ("Vertex Extrusion Intensity", Float) = 1.0
        _VertexExtrusionIntensity2 ("Vertex Extrusion Intensity 2", Float) = -1.0
        _VertexExtrusionUVSpeed ("Vertex Extrusion UV Speed", Vector) = (0.0, 0.0, 0.0, 1.0)
        _InvFresnel ("Inv Fresnel", Float) = 0.0
        _FresnelBias ("Fresnel Bias", Float) = 0.0
        _FresnelScale ("Fresnel Scale", Float) = 1.0
        _FresnelPower ("Fresnel Power", Float) = 5.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _UseMask ("Use Mask", Float) = 0.0
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskChannelMapping ("Mask Channel Mapping", Float) = 0.0
        _MaskDistortionChannelMapping ("Mask Distortion Channel Mapping", Float) = 0.0
        _MaskTexClampU ("Clamp U", Float) = 0.0
        _MaskTexClampV ("Clamp V", Float) = 0.0
        _MaskTexFlip ("Flip", Float) = 0.0
        _MaskTexRotation ("Rotation", Float) = 0.0
        _MaskTexUVSpeed ("Mask Tex UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _AlphaFade ("Alpha Fade", Float) = 1.0
        _DitherAlpha ("Dither Alpha", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2", Float) = 1.0
        _ZOffset ("Z Offset", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ZTest ("Render On Top", Float) = 4.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _TimeOffset ("TimeOffset", Float) = 0.0
        _ApplySceneFog ("Apply Scene Fog", Float) = 0.0
        _DebugOctagonShape ("Debug Octagon Shape", Float) = 0.0
        _OctagonClipErrorColor ("Octagon Clip Error Color", Color) = (1.0, 0.0, 1.0, 1.0)
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
    float4 _MultiplyParticleColor : packoffset(c7);
    float _ZOffset : packoffset(c17.w);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2, float input6 : TEXCOORD3)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3, v4, v5, v6;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.x = asuint(input6);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r0.xyw)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
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
    o1.xyzw = asuint((asfloat(v2.xyzw) * asfloat(asuint((float4(_MultiplyParticleColor.x, _MultiplyParticleColor.y, _MultiplyParticleColor.z, _MultiplyParticleColor.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c167);
    float4 _NapEffectBrightnessExtraParams : packoffset(c168);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _BlendMode : packoffset(c17.z);
    float _AlphaCutoff : packoffset(c18);
    float _OpaquenessFadeByScript : packoffset(c18.y);
    float _AlphaFade : packoffset(c19.y);
}


struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0)
{
    uint4 r0, r1, r2, r3, o0, v0, v1;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    r0.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.x = asuint(mad(asfloat(r0.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r0.yzw = asuint(max(asfloat(v1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.yzw = asuint(log2(asfloat(r0.yzw)));
    r0.xyz = asuint((asfloat(r0.yzw) * asfloat(r0.xxx)));
    r0.xyz = asuint(exp2(asfloat(r0.xyz)));
    r0.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r0.xyz : v1.xyz);
    r0.w = asuint((asfloat(v1.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = ((asfloat(r0.w) < asfloat(asuint((_AlphaCutoff)))) ? 0xffffffffu : 0u);
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_AlphaFade))))));
    r0.w = ((r1.x != 0u) ? 0x00000000u : r0.w);
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
    float4 _MultiplyParticleColor : packoffset(c7);
    float _ZOffset : packoffset(c17.w);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2, float input6 : TEXCOORD3)
{
    uint4 r0, r1, o0, o1, v0, v1, v2, v3, v4, v5, v6;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.x = asuint(input6);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r0.xyw)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
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
    o1.xyzw = asuint((asfloat(v2.xyzw) * asfloat(asuint((float4(_MultiplyParticleColor.x, _MultiplyParticleColor.y, _MultiplyParticleColor.z, _MultiplyParticleColor.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c167);
    float4 _NapEffectBrightnessExtraParams : packoffset(c168);
}

cbuffer UnityPerMaterial : register(b1)
{
    float _BlendMode : packoffset(c17.z);
    float _AlphaCutoff : packoffset(c18);
    float _OpaquenessFadeByScript : packoffset(c18.y);
    float _AlphaFade : packoffset(c19.y);
}


struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0)
{
    uint4 r0, r1, r2, r3, o0, v0, v1;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    r0.x = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.x = asuint(mad(asfloat(r0.x), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r0.yzw = asuint(max(asfloat(v1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.yzw = asuint(log2(asfloat(r0.yzw)));
    r0.xyz = asuint((asfloat(r0.yzw) * asfloat(r0.xxx)));
    r0.xyz = asuint(exp2(asfloat(r0.xyz)));
    r0.w = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r0.xyz : v1.xyz);
    r0.w = asuint((asfloat(v1.w) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r1.x = ((asfloat(r0.w) < asfloat(asuint((_AlphaCutoff)))) ? 0xffffffffu : 0u);
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(asuint((_AlphaFade))))));
    r0.w = ((r1.x != 0u) ? 0x00000000u : r0.w);
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
