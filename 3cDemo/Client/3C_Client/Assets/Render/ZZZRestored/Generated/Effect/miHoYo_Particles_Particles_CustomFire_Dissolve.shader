Shader "miHoYo/Particles/Particles_CustomFire_Dissolve"
{
    Properties
    {
        _AlphaFade ("Alpha Fade", Float) = 1.0
        _EmissionColor ("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _NoiseTex1 ("Noise Tex 1", 2D) = "white" {}
        _Noise1Channel ("Noise1 Channel", Float) = 0.0
        _Noise1USpeed ("U Speed", Float) = 0.0
        _Noise1VSpeed ("V Speed", Float) = 0.0
        _Noise01RandomUV ("Random UV", Float) = 0.0
        _Noise1DistortionIntensity ("Noise1 Distortion Intensity", Float) = 0.0
        _NoiseTex2 ("Noise Tex 2", 2D) = "white" {}
        _Noise2Channel ("Noise2 Channel", Float) = 0.0
        _Noise2USpeed ("U Speed", Float) = 0.0
        _Noise2VSpeed ("V Speed", Float) = 0.0
        _Noise02RandomUV ("Random UV", Float) = 0.0
        _Noise2DistortionIntensity ("Noise2 Distortion Intensity", Float) = 0.0
        _UseCustomData1ZWAsNoise_DST_INT ("使用 CustomData1.ZW 作为 Noise 1&2 的Distortion Intensity的倍率", Float) = 0.0
        _MaskTex ("Mask Tex", 2D) = "white" {}
        _MaskChannel ("Mask Channel", Float) = 0.0
        _MaskTexFlip ("Mask Tex Flip", Float) = 0.0
        _MaskTexClamp ("Mask Tex Clamp", Float) = 0.0
        _MaskUSpeed1 ("U Speed", Float) = 0.0
        _MaskVSpeed1 ("V Speed", Float) = 0.0
        _MaskDistortionIntensity ("Mask Distortion Intensity", Float) = 0.0
        _RandomFromMaskSequence ("Random From Mask Sequence", Float) = 0.0
        _MaskSequenceRows ("Mask Sequence Rows", Float) = 1.0
        _MaskSequenceColumns ("Mask Sequence Columns", Float) = 1.0
        _MaskSequenceBlankNum ("Mask Sequence Blank Num", Float) = 0.0
        _NoiseTexX ("Noise Tex X", 2D) = "white" {}
        _DistortionChannel ("Distortion Channel", Float) = 0.0
        _DistortionTillingOffset ("Distortion Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _DistortionUSpeed ("U Speed", Float) = 0.0
        _DistortionVSpeed ("V Speed", Float) = 0.0
        _DistortionMaskChannel ("Distortion Mask Channel", Float) = 0.0
        _DistortionMaskTillingOffset ("Distortion Mask Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _EdgeWidth ("InColor Edge Width", Float) = 0.1
        _FireColor1 ("InColor", Color) = (1.0, 1.0, 1.0, 0.0)
        _FireColor2 ("InColorHit", Color) = (0.6792453, 0.6792453, 0.6792453, 0.0)
        _EdgeWidthInGlow ("HitColor Edge", Float) = 0.0
        _SoftnessInGlow ("Softness", Float) = 0.0
        _StepSmooth ("Step Smooth", Float) = 0.0
        _OutsideSmoothstep ("OutsideSmoothstep", Float) = 0.0
        _insideSmoothstep ("insideSmoothstep", Float) = 0.0
        _FireNoise ("Fire Noise", Float) = 0.0
        _FireNoiseChannel ("Fire Noise Channel", Float) = 0.0
        _FireNoiseTillingOffset ("Fire Noise Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _FireNoiseUSpeed ("U Speed", Float) = 0.0
        _FireNoiseVSpeed ("V Speed", Float) = 0.0
        _FireNoiseRandomUV ("Random UV", Float) = 0.0
        _UseGradient ("Use Gradient", Float) = 0.0
        _GradientChannel ("Gradient Channel", Float) = 0.0
        _GradientTillingOffset ("Gradient Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _GradientColor ("Gradient Color", Color) = (1.0, 1.0, 1.0, 0.0)
        _GradientBlendMode ("Gradient Blend Mode", Float) = 0.0
        _UseDissolveTex ("Dissolve", Float) = 0.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveChannel ("Mask Channel", Float) = 0.0
        _DissolveUVSpeed ("Dissolve UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DissolveRandomUV ("Random UV", Float) = 0.0
        _SoftEdge ("Soft Edge", Float) = 0.0
        _SoftRange ("Soft Range", Float) = 0.0
        _SoftEdgeUsingOldFunction ("使用旧版溶解方法", Float) = 0.0
        _UsingNonPSR ("用于非粒子系统Renderer", Float) = 0.0
        _DissolveProgress ("Dissolve Progress", Float) = 0.0
        _InvFresnel ("Inv Fresnel", Float) = 0.0
        _FresnelBias ("Fresnel Bias", Float) = 0.0
        _FresnelScale ("Fresnel Scale", Float) = 1.0
        _FresnelPower ("Fresnel Power", Float) = 5.0
        _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.0
        _DitherAlpha ("Dither Alpha", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2", Float) = 1.0
        _EnableAlphaDiscard ("启用低Alpha值剔除", Float) = 0.0
        _Distortion ("Distortion", Float) = 0.0
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "linearGray" {}
        _DTIntensity ("Distortion Intensity", Float) = 0.0
        _SeparateRGBIntensity ("Separate RGB Intensity", Float) = 0.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
        _DtUvMove ("Distortion UV Move", Float) = 0.0
        _DtUSpeed ("Distortion U Speed", Float) = 1.0
        _DtVSpeed ("Distortion V Speed", Float) = 1.0
        _BlendMode ("Blend Mode", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _Cull ("Cull", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 0.0
        _ZTest ("Render On Top", Float) = 4.0
        _ZOffset ("Z Offset", Float) = 0.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _TimeOffset ("Time Offset", Float) = 0.0
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
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
    float4 _ClipSpaceOffset : packoffset(c178);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _EnableAlphaDiscard : packoffset(c0);
    float4 _FireNoiseTillingOffset : packoffset(c1);
    float4 _GradientTillingOffset : packoffset(c4);
    float4 _DistortionTillingOffset : packoffset(c8);
    float4 _DistortionMaskTillingOffset : packoffset(c10);
    float _FireNoiseRandomUV : packoffset(c12.w);
    float _FireNoiseUSpeed : packoffset(c13);
    float _FireNoiseVSpeed : packoffset(c13.y);
    float _DistortionUSpeed : packoffset(c17.z);
    float _DistortionVSpeed : packoffset(c18);
    float _ZOffset : packoffset(c22.z);
    float _IgnoreTimeScale : packoffset(c23.z);
    float _TimeOffset : packoffset(c25.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
    float4 output7 : TEXCOORD6;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, r4, o0, o1, o2, o3, o4, o5, o6, o7, v0, v1, v2, v3, v4, v5;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    r0.x = asuint(saturate((asfloat(v5.y) * asfloat(0x40a00000u))));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.w = asuint((asfloat(r0.x) * asfloat(v5.y)));
    r1.x = asuint(saturate((asfloat(v2.w) * asfloat(0x40a00000u))));
    r1.x = asuint((asfloat(r1.x) * asfloat(r1.x)));
    r0.z = asuint((asfloat(r1.x) * asfloat(v2.w)));
    r1.x = asuint(min(asfloat(r0.w), asfloat(r0.z)));
    r1.x = ((asfloat(r1.x) < asfloat(0x3dcccccdu)) ? 0xffffffffu : 0u);
    r1.yzw = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r1.yzw)));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r1.yzw)));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r2.xyz = asuint((asfloat(r1.zzz) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r1.yyy), asfloat(r2.xyz)));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r1.www), asfloat(r2.xyz)));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r2.x = asuint(dot(asfloat(r1.yzw), asfloat(r1.yzw)));
    r2.x = asuint(rsqrt(asfloat(r2.x)));
    r2.xyz = asuint((asfloat(r1.yzw) * asfloat(r2.xxx)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset)))), asfloat(r1.yzw)));
    r3.x = asuint((glstate_matrix_projection[2][0]));
    r3.y = asuint((glstate_matrix_projection[2][1]));
    r3.z = asuint((glstate_matrix_projection[2][2]));
    r3.w = asuint((glstate_matrix_projection[2][3]));
    r2.w = 0x3f800000u;
    r3.x = asuint(dot(asfloat(r3.xyzw), asfloat(r2.xyzw)));
    r4.x = asuint((glstate_matrix_projection[0][0]));
    r4.y = asuint((glstate_matrix_projection[0][2]));
    r4.z = asuint((glstate_matrix_projection[0][3]));
    r3.z = asuint(dot(asfloat(r4.xyz), asfloat(r2.xzw)));
    r4.x = asuint((glstate_matrix_projection[1][1]));
    r4.y = asuint((glstate_matrix_projection[1][2]));
    r4.z = asuint((glstate_matrix_projection[1][3]));
    r3.w = asuint(dot(asfloat(r4.xyz), asfloat(r2.yzw)));
    r2.x = asuint((glstate_matrix_projection[3][2]));
    r2.y = asuint((glstate_matrix_projection[3][3]));
    r3.y = asuint(dot(asfloat(r2.xy), asfloat(r2.zw)));
    r1.xyzw = ((r1.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) : r3.zwxy);
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_EnableAlphaDiscard)))) ? 0xffffffffu : 0u);
    r1.xy = ((r2.xx != 0u) ? r1.xy : r3.zw);
    r0.xy = r1.zw;
    r3.z = v2.w;
    r3.w = v5.y;
    r0.xyzw = ((r2.xxxx != 0u) ? r0.xyzw : r3.xyzw);
    o0.xy = asuint(mad(asfloat(asuint((float2(_ClipSpaceOffset.x, _ClipSpaceOffset.y)))), asfloat(r0.yy), asfloat(r1.xy)));
    o0.zw = r0.xy;
    o1.w = r0.z;
    o3.y = r0.w;
    o1.xyz = v2.xyz;
    o2.xyzw = v4.xyzw;
    o3.xzw = v5.xzw;
    o4.xyzw = v3.xyzw;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r1.x = asuint((asfloat(r0.x) * asfloat(asuint((_DistortionUSpeed)))));
    r1.y = asuint((asfloat(r0.x) * asfloat(asuint((_DistortionVSpeed)))));
    r0.xy = asuint((asfloat(r0.xx) * asfloat(asuint((float2(_FireNoiseUSpeed, _FireNoiseVSpeed))))));
    r0.xy = asuint(frac(asfloat(r0.xy)));
    r0.zw = asuint(frac(asfloat(r1.xy)));
    r1.xy = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_DistortionTillingOffset.x, _DistortionTillingOffset.y)))), asfloat(asuint((float2(_DistortionTillingOffset.z, _DistortionTillingOffset.w))))));
    o5.xy = asuint((asfloat(r0.zw) + asfloat(r1.xy)));
    o5.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_DistortionMaskTillingOffset.x, _DistortionMaskTillingOffset.y)))), asfloat(asuint((float2(_DistortionMaskTillingOffset.z, _DistortionMaskTillingOffset.w))))));
    o6.xyzw = uint4(0x3f800000u, 0x3f800000u, 0x00000000u, 0x00000000u);
    r0.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_FireNoiseTillingOffset.x, _FireNoiseTillingOffset.y)))), asfloat(asuint((float2(_FireNoiseTillingOffset.z, _FireNoiseTillingOffset.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v5.zw)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_FireNoiseRandomUV)))) ? 0xffffffffu : 0u);
    o7.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o7.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_GradientTillingOffset.x, _GradientTillingOffset.y)))), asfloat(asuint((float2(_GradientTillingOffset.z, _GradientTillingOffset.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    result.output7 = asfloat(o7.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c172);
    float4 _NapEffectBrightnessExtraParams : packoffset(c173);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _FireColor1 : packoffset(c2);
    float4 _FireColor2 : packoffset(c3);
    float4 _NoiseTex1_ST : packoffset(c6);
    float4 _NoiseTex2_ST : packoffset(c7);
    float4 _MaskTex_ST : packoffset(c9);
    float _MaskTexFlip : packoffset(c11);
    float _MaskDistortionIntensity : packoffset(c11.y);
    float _MaskTexClamp : packoffset(c11.z);
    float _UseGradient : packoffset(c12.z);
    float _StepSmooth : packoffset(c13.z);
    float _EdgeWidth : packoffset(c13.w);
    float _insideSmoothstep : packoffset(c14);
    float _GradientBlendMode : packoffset(c14.y);
    float _GradientChannel : packoffset(c14.z);
    float _FireNoiseChannel : packoffset(c14.w);
    float _MaskVSpeed1 : packoffset(c15);
    float _Noise2VSpeed : packoffset(c15.y);
    float _MaskChannel : packoffset(c15.z);
    float _FireNoise : packoffset(c15.w);
    float _SoftnessInGlow : packoffset(c16);
    float _EdgeWidthInGlow : packoffset(c16.y);
    float _Noise1Channel : packoffset(c16.z);
    float _Noise01RandomUV : packoffset(c16.w);
    float _DistortionChannel : packoffset(c17.y);
    float _MaskUSpeed1 : packoffset(c17.w);
    float _Noise1DistortionIntensity : packoffset(c18.y);
    float _UseCustomData1ZWAsNoise_DST_INT : packoffset(c18.z);
    float _Noise1USpeed : packoffset(c18.w);
    float _Noise1VSpeed : packoffset(c19);
    float _Noise2Channel : packoffset(c19.y);
    float _Noise02RandomUV : packoffset(c19.z);
    float _Noise2DistortionIntensity : packoffset(c19.w);
    float _Noise2USpeed : packoffset(c20);
    float _DistortionMaskChannel : packoffset(c20.y);
    float _OutsideSmoothstep : packoffset(c20.z);
    float _BlendMode : packoffset(c22.w);
    float _OpaquenessFadeByScript : packoffset(c23);
    float _AlphaCutoff : packoffset(c23.y);
    float _IgnoreTimeScale : packoffset(c23.z);
    float _TimeOffset : packoffset(c25.z);
    float _AlphaFade : packoffset(c31.z);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_NoiseTex1;
SamplerState sampler_NoiseTexX;
SamplerState sampler_MaskTex;
SamplerState sampler_NoiseTex2;
Texture2D<float4> _NoiseTex1 : register(t0);
Texture2D<float4> _NoiseTexX : register(t1);
Texture2D<float4> _MaskTex : register(t2);
Texture2D<float4> _NoiseTex2 : register(t3);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5, float4 input7 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4, v5, v6, v7;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    r0.xy = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_NoiseTex1_ST.x, _NoiseTex1_ST.y)))), asfloat(asuint((float2(_NoiseTex1_ST.z, _NoiseTex1_ST.w))))));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_Noise01RandomUV)))) ? 0xffffffffu : 0u);
    r1.xy = asuint((asfloat(r0.xy) + asfloat(v3.zw)));
    r0.xy = ((r0.zz != 0u) ? r1.xy : r0.xy);
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.w = (uint)(asfloat(asuint((_DistortionChannel))));
    r1.xyzw = asuint(_NoiseTexX.Sample(sampler_NoiseTex1, asfloat(v5.xy)).xyzw);
    r0.w = min(r0.w, 0x00000003u);
    r0.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.w+0].xyzw)));
    r1.x = (uint)(asfloat(asuint((_DistortionMaskChannel))));
    r2.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(v5.zw)).xyzw);
    r1.x = min(r1.x, 0x00000003u);
    r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UseCustomData1ZWAsNoise_DST_INT)))) ? 0xffffffffu : 0u);
    r1.xy = ((r1.xx != 0u) ? v2.xy : uint2(0x3f800000u, 0x3f800000u));
    r1.z = asuint((asfloat(r0.w) * asfloat(asuint((_Noise1DistortionIntensity)))));
    r2.x = asuint((asfloat(r0.z) * asfloat(asuint((_Noise1USpeed)))));
    r2.y = asuint((asfloat(r0.z) * asfloat(asuint((_Noise1VSpeed)))));
    r2.xy = asuint(frac(asfloat(r2.xy)));
    r1.xz = asuint(mad(asfloat(r1.zz), asfloat(r1.xx), asfloat(r2.xy)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r1.xz)));
    r1.x = (uint)(asfloat(asuint((_Noise1Channel))));
    r2.xyzw = asuint(_NoiseTex1.Sample(sampler_MaskTex, asfloat(r0.xy)).xyzw);
    r0.x = min(r1.x, 0x00000003u);
    r0.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r0.x+0].xyzw)));
    r1.xz = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_NoiseTex2_ST.x, _NoiseTex2_ST.y)))), asfloat(asuint((float2(_NoiseTex2_ST.z, _NoiseTex2_ST.w))))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint((_Noise02RandomUV)))) ? 0xffffffffu : 0u);
    r2.xy = asuint((asfloat(r1.xz) + asfloat(v3.zw)));
    r1.xz = ((r0.yy != 0u) ? r2.xy : r1.xz);
    r0.y = asuint((asfloat(r0.w) * asfloat(asuint((_Noise2DistortionIntensity)))));
    r2.x = asuint((asfloat(r0.z) * asfloat(asuint((_Noise2USpeed)))));
    r2.yw = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_Noise2VSpeed, _MaskVSpeed1))))));
    r2.xy = asuint(frac(asfloat(r2.xy)));
    r1.yw = asuint(mad(asfloat(r0.yy), asfloat(r1.yy), asfloat(r2.xy)));
    r1.xy = asuint((asfloat(r1.yw) + asfloat(r1.xz)));
    r0.y = (uint)(asfloat(asuint((_Noise2Channel))));
    r1.xyzw = asuint(_NoiseTex2.Sample(sampler_NoiseTex2, asfloat(r1.xy)).xyzw);
    r0.y = min(r0.y, 0x00000003u);
    r0.y = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.x = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r1.xy = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    r2.z = asuint((asfloat(r0.z) * asfloat(asuint((_MaskUSpeed1)))));
    r0.yz = asuint(frac(asfloat(r2.zw)));
    r0.yz = asuint((asfloat(r0.yz) + asfloat(r1.xy)));
    r1.xy = ((asfloat(uint2(0x3fc00000u, 0x3fc00000u)) < asfloat(asuint((float2(_MaskTexFlip, _MaskTexClamp))))) ? 0xffffffffu : 0u);
    r2.xyzw = ((asfloat(asuint((float4(_MaskTexFlip, _MaskTexFlip, _MaskTexFlip, _MaskTexClamp)))) < asfloat(uint4(0x3f000000u, 0x40200000u, 0x3fc00000u, 0x3f000000u))) ? 0xffffffffu : 0u);
    r1.x = (r1.x & r2.y);
    r1.x = (r1.x | r2.x);
    r1.zw = asuint((asfloat((r0.yz ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r2.x = ((r1.x != 0u) ? r0.y : r1.z);
    r2.y = ((r2.z != 0u) ? r0.z : r1.w);
    r0.yz = asuint(mad(asfloat(r0.ww), asfloat(asuint((float2(_MaskDistortionIntensity, _MaskDistortionIntensity)))), asfloat(r2.xy)));
    r1.xz = asuint(saturate(asfloat(r0.yz)));
    r2.xy = ((asfloat(asuint((float2(_MaskTexClamp, _MaskTexClamp)))) < asfloat(uint2(0x40200000u, 0x3fc00000u))) ? 0xffffffffu : 0u);
    r0.w = (r1.y & r2.x);
    r0.w = (r0.w | r2.w);
    r1.x = ((r0.w != 0u) ? r0.y : r1.x);
    r1.y = ((r2.y != 0u) ? r0.z : r1.z);
    r0.yz = asuint(mad(asfloat(r1.xy), asfloat(v6.xy), asfloat(v6.zw)));
    r0.w = (uint)(asfloat(asuint((_MaskChannel))));
    r1.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(r0.yz)).xyzw);
    r0.y = min(r0.w, 0x00000003u);
    r0.y = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.z = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.w = asuint((asfloat(v1.w) + asfloat(0xbc656042u)));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_FireNoise)))) ? 0xffffffffu : 0u);
    if (r1.x != 0u) {
        r1.x = (uint)(asfloat(asuint((_FireNoiseChannel))));
        r2.xyzw = asuint(_NoiseTexX.Sample(sampler_NoiseTex1, asfloat(v7.xy)).xyzw);
        r1.x = min(r1.x, 0x00000003u);
        r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    } else {
        r1.y = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat((asuint((_EdgeWidthInGlow)) ^ 0x80000000u))));
        r1.y = asuint((asfloat(r1.y) + asfloat(0x3f800000u)));
        r1.y = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r1.y)));
        r1.y = asuint((asfloat(r1.y) + asfloat(0xbdcccccdu)));
        r1.z = asuint((asfloat(0x3f800000u) / asfloat(asuint((_SoftnessInGlow)))));
        r1.y = asuint(saturate((asfloat(r1.z) * asfloat(r1.y))));
        r1.z = asuint(mad(asfloat(r1.y), asfloat(0xc0000000u), asfloat(0x40400000u)));
        r1.y = asuint((asfloat(r1.y) * asfloat(r1.y)));
        r1.x = asuint((asfloat(r1.y) * asfloat(r1.z)));
    }
    r1.yzw = asuint(mad(asfloat(asuint((float3(_FireColor2.x, _FireColor2.y, _FireColor2.z)))), asfloat(v1.xyz), asfloat((asuint((float3(_FireColor1.x, _FireColor1.y, _FireColor1.z))) ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(r1.xxx), asfloat(r1.yzw), asfloat(asuint((float3(_FireColor1.x, _FireColor1.y, _FireColor1.z))))));
    r1.w = asuint((asfloat(r0.w) + asfloat(asuint((_EdgeWidth)))));
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_StepSmooth)))) ? 0xffffffffu : 0u);
    r2.y = asuint(mad(asfloat((r0.x ^ 0x80000000u)), asfloat(r0.y), asfloat(r1.w)));
    r2.z = asuint((asfloat(0x3f800000u) / asfloat(asuint((_insideSmoothstep)))));
    r2.y = asuint(saturate((asfloat(r2.z) * asfloat(r2.y))));
    r2.z = asuint(mad(asfloat(r2.y), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r2.y = asuint((asfloat(r2.y) * asfloat(r2.y)));
    r2.y = asuint((asfloat(r2.y) * asfloat(r2.z)));
    r1.w = ((asfloat(r1.w) >= asfloat(r0.z)) ? 0xffffffffu : 0u);
    r1.w = (r1.w & 0x3f800000u);
    r1.w = ((r2.x != 0u) ? r2.y : r1.w);
    r2.yzw = asuint((asfloat(r1.xyz) * asfloat(v1.xyz)));
    r3.xy = v2.zw;
    r3.z = v3.x;
    r1.xyz = asuint(mad(asfloat((r1.xyz ^ 0x80000000u)), asfloat(v1.xyz), asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r1.www), asfloat(r1.xyz), asfloat(r2.yzw)));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_UseGradient)))) ? 0xffffffffu : 0u);
    if (r1.w != 0u) {
        r3.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(v7.zw)).xyzw);
        r1.w = ((asfloat(asuint((_GradientChannel))) < asfloat(0x40600000u)) ? 0xffffffffu : 0u);
        if (r1.w != 0u) {
            r1.w = (uint)(asfloat(asuint((_GradientChannel))));
            r1.w = min(r1.w, 0x00000003u);
            r3.z = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r1.w+0].xyzw)));
            r3.xyz = r3.zzz;
        }
        r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_GradientBlendMode)))) ? 0xffffffffu : 0u);
        r2.yzw = asuint((asfloat(r1.xyz) * asfloat(r3.xyz)));
        r3.xyz = asuint((asfloat(r1.xyz) + asfloat(r3.xyz)));
        r1.xyz = ((r1.www != 0u) ? r2.yzw : r3.xyz);
    }
    r1.w = asuint((asfloat(v4.w) + asfloat(asuint((_OutsideSmoothstep)))));
    r0.x = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat((r0.w ^ 0x80000000u))));
    r0.y = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r0.x = asuint(saturate((asfloat(r0.y) * asfloat(r0.x))));
    r0.y = asuint(mad(asfloat(r0.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.y)));
    r0.y = ((asfloat(r0.z) >= asfloat(r0.w)) ? 0xffffffffu : 0u);
    r0.y = (r0.y & 0x3f800000u);
    r0.y = asuint((asfloat(r0.y) * asfloat(v1.w)));
    r0.y = ((asfloat(r0.y) >= asfloat(0x3c23d70au)) ? 0xffffffffu : 0u);
    r0.y = (r0.y & 0x3f800000u);
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v3.yy)));
    r0.x = ((r2.x != 0u) ? r0.x : r0.y);
    r0.y = asuint((asfloat(asuint((_OpaquenessFadeByScript))) * asfloat(asuint((_AlphaFade)))));
    r0.x = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.y = ((asfloat(r0.x) < asfloat(asuint((_AlphaCutoff)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.y != 0u) ? 0x00000000u : r0.x);
    r0.y = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xyz = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.z = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(mad(asfloat(r0.z), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r2.xyz = asuint(log2(asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r0.zzz) * asfloat(r2.xyz)));
    r2.xyz = asuint(exp2(asfloat(r2.xyz)));
    r0.yzw = ((r0.yyy != 0u) ? r2.xyz : r1.xyz);
    r1.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r1.w = asuint(max(asfloat(r0.x), asfloat(0x3a83126fu)));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r2.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r3.xyz = ((asfloat(r1.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.www), asfloat((r0.yzw ^ 0x80000000u))));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.yzw)));
    r1.x = asuint(max(asfloat(r1.y), asfloat(r1.x)));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r1.yzw = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.yzw ^ 0x80000000u))));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(r1.yzw), asfloat(r0.yzw)));
    r1.x = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.x = ((asfloat(0x3a83126fu) < asfloat((r1.x & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.y = asuint(dot(asfloat(r0.yzw), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r2.xyz = asuint((asfloat(r0.yzw) + asfloat((r1.yyy ^ 0x80000000u))));
    r1.yzw = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r2.xyz), asfloat(r1.yyy)));
    r0.yzw = ((r1.xxx != 0u) ? r1.yzw : r0.yzw);
    o0.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    o0.w = r0.x;
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
    float4 _ClipSpaceOffset : packoffset(c178);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _EnableAlphaDiscard : packoffset(c0);
    float4 _FireNoiseTillingOffset : packoffset(c1);
    float4 _GradientTillingOffset : packoffset(c4);
    float4 _DistortionTillingOffset : packoffset(c8);
    float4 _DistortionMaskTillingOffset : packoffset(c10);
    float _FireNoiseRandomUV : packoffset(c12.w);
    float _FireNoiseUSpeed : packoffset(c13);
    float _FireNoiseVSpeed : packoffset(c13.y);
    float _DistortionUSpeed : packoffset(c17.z);
    float _DistortionVSpeed : packoffset(c18);
    float _ZOffset : packoffset(c22.z);
    float _IgnoreTimeScale : packoffset(c23.z);
    float _TimeOffset : packoffset(c25.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
    float4 output7 : TEXCOORD6;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0, float4 input3 : TEXCOORD0, float4 input4 : TEXCOORD1, float4 input5 : TEXCOORD2)
{
    uint4 r0, r1, r2, r3, r4, o0, o1, o2, o3, o4, o5, o6, o7, v0, v1, v2, v3, v4, v5;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    r0.x = asuint(saturate((asfloat(v5.y) * asfloat(0x40a00000u))));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.w = asuint((asfloat(r0.x) * asfloat(v5.y)));
    r1.x = asuint(saturate((asfloat(v2.w) * asfloat(0x40a00000u))));
    r1.x = asuint((asfloat(r1.x) * asfloat(r1.x)));
    r0.z = asuint((asfloat(r1.x) * asfloat(v2.w)));
    r1.x = asuint(min(asfloat(r0.w), asfloat(r0.z)));
    r1.x = ((asfloat(r1.x) < asfloat(0x3dcccccdu)) ? 0xffffffffu : 0u);
    r1.yzw = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r1.yzw)));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r1.yzw)));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r2.xyz = asuint((asfloat(r1.zzz) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r1.yyy), asfloat(r2.xyz)));
    r1.yzw = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r1.www), asfloat(r2.xyz)));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r2.x = asuint(dot(asfloat(r1.yzw), asfloat(r1.yzw)));
    r2.x = asuint(rsqrt(asfloat(r2.x)));
    r2.xyz = asuint((asfloat(r1.yzw) * asfloat(r2.xxx)));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset)))), asfloat(r1.yzw)));
    r3.x = asuint((glstate_matrix_projection[2][0]));
    r3.y = asuint((glstate_matrix_projection[2][1]));
    r3.z = asuint((glstate_matrix_projection[2][2]));
    r3.w = asuint((glstate_matrix_projection[2][3]));
    r2.w = 0x3f800000u;
    r3.x = asuint(dot(asfloat(r3.xyzw), asfloat(r2.xyzw)));
    r4.x = asuint((glstate_matrix_projection[0][0]));
    r4.y = asuint((glstate_matrix_projection[0][2]));
    r4.z = asuint((glstate_matrix_projection[0][3]));
    r3.z = asuint(dot(asfloat(r4.xyz), asfloat(r2.xzw)));
    r4.x = asuint((glstate_matrix_projection[1][1]));
    r4.y = asuint((glstate_matrix_projection[1][2]));
    r4.z = asuint((glstate_matrix_projection[1][3]));
    r3.w = asuint(dot(asfloat(r4.xyz), asfloat(r2.yzw)));
    r2.x = asuint((glstate_matrix_projection[3][2]));
    r2.y = asuint((glstate_matrix_projection[3][3]));
    r3.y = asuint(dot(asfloat(r2.xy), asfloat(r2.zw)));
    r1.xyzw = ((r1.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) : r3.zwxy);
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_EnableAlphaDiscard)))) ? 0xffffffffu : 0u);
    r1.xy = ((r2.xx != 0u) ? r1.xy : r3.zw);
    r0.xy = r1.zw;
    r3.z = v2.w;
    r3.w = v5.y;
    r0.xyzw = ((r2.xxxx != 0u) ? r0.xyzw : r3.xyzw);
    o0.xy = asuint(mad(asfloat(asuint((float2(_ClipSpaceOffset.x, _ClipSpaceOffset.y)))), asfloat(r0.yy), asfloat(r1.xy)));
    o0.zw = r0.xy;
    o1.w = r0.z;
    o3.y = r0.w;
    o1.xyz = v2.xyz;
    o2.xyzw = v4.xyzw;
    o3.xzw = v5.xzw;
    o4.xyzw = v3.xyzw;
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r1.x = asuint((asfloat(r0.x) * asfloat(asuint((_DistortionUSpeed)))));
    r1.y = asuint((asfloat(r0.x) * asfloat(asuint((_DistortionVSpeed)))));
    r0.xy = asuint((asfloat(r0.xx) * asfloat(asuint((float2(_FireNoiseUSpeed, _FireNoiseVSpeed))))));
    r0.xy = asuint(frac(asfloat(r0.xy)));
    r0.zw = asuint(frac(asfloat(r1.xy)));
    r1.xy = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_DistortionTillingOffset.x, _DistortionTillingOffset.y)))), asfloat(asuint((float2(_DistortionTillingOffset.z, _DistortionTillingOffset.w))))));
    o5.xy = asuint((asfloat(r0.zw) + asfloat(r1.xy)));
    o5.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_DistortionMaskTillingOffset.x, _DistortionMaskTillingOffset.y)))), asfloat(asuint((float2(_DistortionMaskTillingOffset.z, _DistortionMaskTillingOffset.w))))));
    o6.xyzw = uint4(0x3f800000u, 0x3f800000u, 0x00000000u, 0x00000000u);
    r0.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_FireNoiseTillingOffset.x, _FireNoiseTillingOffset.y)))), asfloat(asuint((float2(_FireNoiseTillingOffset.z, _FireNoiseTillingOffset.w))))));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r0.zw)));
    r0.zw = asuint((asfloat(r0.xy) + asfloat(v5.zw)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_FireNoiseRandomUV)))) ? 0xffffffffu : 0u);
    o7.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    o7.zw = asuint(mad(asfloat(v3.xy), asfloat(asuint((float2(_GradientTillingOffset.x, _GradientTillingOffset.y)))), asfloat(asuint((float2(_GradientTillingOffset.z, _GradientTillingOffset.w))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    result.output7 = asfloat(o7.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _GlobalTimeParamsA : packoffset(c13);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c17);
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float4 _NapEffectBrightnessParams4 : packoffset(c172);
    float4 _NapEffectBrightnessExtraParams : packoffset(c173);
}

cbuffer UnityPerMaterial : register(b1)
{
    float4 _FireColor1 : packoffset(c2);
    float4 _FireColor2 : packoffset(c3);
    float4 _NoiseTex1_ST : packoffset(c6);
    float4 _NoiseTex2_ST : packoffset(c7);
    float4 _MaskTex_ST : packoffset(c9);
    float _MaskTexFlip : packoffset(c11);
    float _MaskDistortionIntensity : packoffset(c11.y);
    float _MaskTexClamp : packoffset(c11.z);
    float _UseGradient : packoffset(c12.z);
    float _StepSmooth : packoffset(c13.z);
    float _EdgeWidth : packoffset(c13.w);
    float _insideSmoothstep : packoffset(c14);
    float _GradientBlendMode : packoffset(c14.y);
    float _GradientChannel : packoffset(c14.z);
    float _FireNoiseChannel : packoffset(c14.w);
    float _MaskVSpeed1 : packoffset(c15);
    float _Noise2VSpeed : packoffset(c15.y);
    float _MaskChannel : packoffset(c15.z);
    float _FireNoise : packoffset(c15.w);
    float _SoftnessInGlow : packoffset(c16);
    float _EdgeWidthInGlow : packoffset(c16.y);
    float _Noise1Channel : packoffset(c16.z);
    float _Noise01RandomUV : packoffset(c16.w);
    float _DistortionChannel : packoffset(c17.y);
    float _MaskUSpeed1 : packoffset(c17.w);
    float _Noise1DistortionIntensity : packoffset(c18.y);
    float _UseCustomData1ZWAsNoise_DST_INT : packoffset(c18.z);
    float _Noise1USpeed : packoffset(c18.w);
    float _Noise1VSpeed : packoffset(c19);
    float _Noise2Channel : packoffset(c19.y);
    float _Noise02RandomUV : packoffset(c19.z);
    float _Noise2DistortionIntensity : packoffset(c19.w);
    float _Noise2USpeed : packoffset(c20);
    float _DistortionMaskChannel : packoffset(c20.y);
    float _OutsideSmoothstep : packoffset(c20.z);
    float _BlendMode : packoffset(c22.w);
    float _OpaquenessFadeByScript : packoffset(c23);
    float _AlphaCutoff : packoffset(c23.y);
    float _IgnoreTimeScale : packoffset(c23.z);
    float _TimeOffset : packoffset(c25.z);
    float _AlphaFade : packoffset(c31.z);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_NoiseTex1;
SamplerState sampler_NoiseTexX;
SamplerState sampler_MaskTex;
SamplerState sampler_NoiseTex2;
Texture2D<float4> _NoiseTex1 : register(t0);
Texture2D<float4> _NoiseTexX : register(t1);
Texture2D<float4> _MaskTex : register(t2);
Texture2D<float4> _NoiseTex2 : register(t3);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5, float4 input7 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, o0, v0, v1, v2, v3, v4, v5, v6, v7;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    r0.xy = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_NoiseTex1_ST.x, _NoiseTex1_ST.y)))), asfloat(asuint((float2(_NoiseTex1_ST.z, _NoiseTex1_ST.w))))));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_Noise01RandomUV)))) ? 0xffffffffu : 0u);
    r1.xy = asuint((asfloat(r0.xy) + asfloat(v3.zw)));
    r0.xy = ((r0.zz != 0u) ? r1.xy : r0.xy);
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.w = (uint)(asfloat(asuint((_DistortionChannel))));
    r1.xyzw = asuint(_NoiseTexX.Sample(sampler_NoiseTex1, asfloat(v5.xy)).xyzw);
    r0.w = min(r0.w, 0x00000003u);
    r0.w = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.w+0].xyzw)));
    r1.x = (uint)(asfloat(asuint((_DistortionMaskChannel))));
    r2.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(v5.zw)).xyzw);
    r1.x = min(r1.x, 0x00000003u);
    r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.x)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_UseCustomData1ZWAsNoise_DST_INT)))) ? 0xffffffffu : 0u);
    r1.xy = ((r1.xx != 0u) ? v2.xy : uint2(0x3f800000u, 0x3f800000u));
    r1.z = asuint((asfloat(r0.w) * asfloat(asuint((_Noise1DistortionIntensity)))));
    r2.x = asuint((asfloat(r0.z) * asfloat(asuint((_Noise1USpeed)))));
    r2.y = asuint((asfloat(r0.z) * asfloat(asuint((_Noise1VSpeed)))));
    r2.xy = asuint(frac(asfloat(r2.xy)));
    r1.xz = asuint(mad(asfloat(r1.zz), asfloat(r1.xx), asfloat(r2.xy)));
    r0.xy = asuint((asfloat(r0.xy) + asfloat(r1.xz)));
    r1.x = (uint)(asfloat(asuint((_Noise1Channel))));
    r2.xyzw = asuint(_NoiseTex1.Sample(sampler_MaskTex, asfloat(r0.xy)).xyzw);
    r0.x = min(r1.x, 0x00000003u);
    r0.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r0.x+0].xyzw)));
    r1.xz = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_NoiseTex2_ST.x, _NoiseTex2_ST.y)))), asfloat(asuint((float2(_NoiseTex2_ST.z, _NoiseTex2_ST.w))))));
    r0.y = ((asfloat(0x3f000000u) < asfloat(asuint((_Noise02RandomUV)))) ? 0xffffffffu : 0u);
    r2.xy = asuint((asfloat(r1.xz) + asfloat(v3.zw)));
    r1.xz = ((r0.yy != 0u) ? r2.xy : r1.xz);
    r0.y = asuint((asfloat(r0.w) * asfloat(asuint((_Noise2DistortionIntensity)))));
    r2.x = asuint((asfloat(r0.z) * asfloat(asuint((_Noise2USpeed)))));
    r2.yw = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_Noise2VSpeed, _MaskVSpeed1))))));
    r2.xy = asuint(frac(asfloat(r2.xy)));
    r1.yw = asuint(mad(asfloat(r0.yy), asfloat(r1.yy), asfloat(r2.xy)));
    r1.xy = asuint((asfloat(r1.yw) + asfloat(r1.xz)));
    r0.y = (uint)(asfloat(asuint((_Noise2Channel))));
    r1.xyzw = asuint(_NoiseTex2.Sample(sampler_NoiseTex2, asfloat(r1.xy)).xyzw);
    r0.y = min(r0.y, 0x00000003u);
    r0.y = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.x = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r1.xy = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_MaskTex_ST.x, _MaskTex_ST.y)))), asfloat(asuint((float2(_MaskTex_ST.z, _MaskTex_ST.w))))));
    r2.z = asuint((asfloat(r0.z) * asfloat(asuint((_MaskUSpeed1)))));
    r0.yz = asuint(frac(asfloat(r2.zw)));
    r0.yz = asuint((asfloat(r0.yz) + asfloat(r1.xy)));
    r1.xy = ((asfloat(uint2(0x3fc00000u, 0x3fc00000u)) < asfloat(asuint((float2(_MaskTexFlip, _MaskTexClamp))))) ? 0xffffffffu : 0u);
    r2.xyzw = ((asfloat(asuint((float4(_MaskTexFlip, _MaskTexFlip, _MaskTexFlip, _MaskTexClamp)))) < asfloat(uint4(0x3f000000u, 0x40200000u, 0x3fc00000u, 0x3f000000u))) ? 0xffffffffu : 0u);
    r1.x = (r1.x & r2.y);
    r1.x = (r1.x | r2.x);
    r1.zw = asuint((asfloat((r0.yz ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r2.x = ((r1.x != 0u) ? r0.y : r1.z);
    r2.y = ((r2.z != 0u) ? r0.z : r1.w);
    r0.yz = asuint(mad(asfloat(r0.ww), asfloat(asuint((float2(_MaskDistortionIntensity, _MaskDistortionIntensity)))), asfloat(r2.xy)));
    r1.xz = asuint(saturate(asfloat(r0.yz)));
    r2.xy = ((asfloat(asuint((float2(_MaskTexClamp, _MaskTexClamp)))) < asfloat(uint2(0x40200000u, 0x3fc00000u))) ? 0xffffffffu : 0u);
    r0.w = (r1.y & r2.x);
    r0.w = (r0.w | r2.w);
    r1.x = ((r0.w != 0u) ? r0.y : r1.x);
    r1.y = ((r2.y != 0u) ? r0.z : r1.z);
    r0.yz = asuint(mad(asfloat(r1.xy), asfloat(v6.xy), asfloat(v6.zw)));
    r0.w = (uint)(asfloat(asuint((_MaskChannel))));
    r1.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(r0.yz)).xyzw);
    r0.y = min(r0.w, 0x00000003u);
    r0.y = asuint(dot(asfloat(r1.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.z = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.w = asuint((asfloat(v1.w) + asfloat(0xbc656042u)));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_FireNoise)))) ? 0xffffffffu : 0u);
    if (r1.x != 0u) {
        r1.x = (uint)(asfloat(asuint((_FireNoiseChannel))));
        r2.xyzw = asuint(_NoiseTexX.Sample(sampler_NoiseTex1, asfloat(v7.xy)).xyzw);
        r1.x = min(r1.x, 0x00000003u);
        r1.x = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r1.x+0].xyzw)));
    } else {
        r1.y = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat((asuint((_EdgeWidthInGlow)) ^ 0x80000000u))));
        r1.y = asuint((asfloat(r1.y) + asfloat(0x3f800000u)));
        r1.y = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r1.y)));
        r1.y = asuint((asfloat(r1.y) + asfloat(0xbdcccccdu)));
        r1.z = asuint((asfloat(0x3f800000u) / asfloat(asuint((_SoftnessInGlow)))));
        r1.y = asuint(saturate((asfloat(r1.z) * asfloat(r1.y))));
        r1.z = asuint(mad(asfloat(r1.y), asfloat(0xc0000000u), asfloat(0x40400000u)));
        r1.y = asuint((asfloat(r1.y) * asfloat(r1.y)));
        r1.x = asuint((asfloat(r1.y) * asfloat(r1.z)));
    }
    r1.yzw = asuint(mad(asfloat(asuint((float3(_FireColor2.x, _FireColor2.y, _FireColor2.z)))), asfloat(v1.xyz), asfloat((asuint((float3(_FireColor1.x, _FireColor1.y, _FireColor1.z))) ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(r1.xxx), asfloat(r1.yzw), asfloat(asuint((float3(_FireColor1.x, _FireColor1.y, _FireColor1.z))))));
    r1.w = asuint((asfloat(r0.w) + asfloat(asuint((_EdgeWidth)))));
    r2.x = ((asfloat(0x3f000000u) < asfloat(asuint((_StepSmooth)))) ? 0xffffffffu : 0u);
    r2.y = asuint(mad(asfloat((r0.x ^ 0x80000000u)), asfloat(r0.y), asfloat(r1.w)));
    r2.z = asuint((asfloat(0x3f800000u) / asfloat(asuint((_insideSmoothstep)))));
    r2.y = asuint(saturate((asfloat(r2.z) * asfloat(r2.y))));
    r2.z = asuint(mad(asfloat(r2.y), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r2.y = asuint((asfloat(r2.y) * asfloat(r2.y)));
    r2.y = asuint((asfloat(r2.y) * asfloat(r2.z)));
    r1.w = ((asfloat(r1.w) >= asfloat(r0.z)) ? 0xffffffffu : 0u);
    r1.w = (r1.w & 0x3f800000u);
    r1.w = ((r2.x != 0u) ? r2.y : r1.w);
    r2.yzw = asuint((asfloat(r1.xyz) * asfloat(v1.xyz)));
    r3.xy = v2.zw;
    r3.z = v3.x;
    r1.xyz = asuint(mad(asfloat((r1.xyz ^ 0x80000000u)), asfloat(v1.xyz), asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r1.www), asfloat(r1.xyz), asfloat(r2.yzw)));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_UseGradient)))) ? 0xffffffffu : 0u);
    if (r1.w != 0u) {
        r3.xyzw = asuint(_MaskTex.Sample(sampler_NoiseTexX, asfloat(v7.zw)).xyzw);
        r1.w = ((asfloat(asuint((_GradientChannel))) < asfloat(0x40600000u)) ? 0xffffffffu : 0u);
        if (r1.w != 0u) {
            r1.w = (uint)(asfloat(asuint((_GradientChannel))));
            r1.w = min(r1.w, 0x00000003u);
            r3.z = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r1.w+0].xyzw)));
            r3.xyz = r3.zzz;
        }
        r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_GradientBlendMode)))) ? 0xffffffffu : 0u);
        r2.yzw = asuint((asfloat(r1.xyz) * asfloat(r3.xyz)));
        r3.xyz = asuint((asfloat(r1.xyz) + asfloat(r3.xyz)));
        r1.xyz = ((r1.www != 0u) ? r2.yzw : r3.xyz);
    }
    r1.w = asuint((asfloat(v4.w) + asfloat(asuint((_OutsideSmoothstep)))));
    r0.x = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat((r0.w ^ 0x80000000u))));
    r0.y = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r0.x = asuint(saturate((asfloat(r0.y) * asfloat(r0.x))));
    r0.y = asuint(mad(asfloat(r0.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.y)));
    r0.y = ((asfloat(r0.z) >= asfloat(r0.w)) ? 0xffffffffu : 0u);
    r0.y = (r0.y & 0x3f800000u);
    r0.y = asuint((asfloat(r0.y) * asfloat(v1.w)));
    r0.y = ((asfloat(r0.y) >= asfloat(0x3c23d70au)) ? 0xffffffffu : 0u);
    r0.y = (r0.y & 0x3f800000u);
    r0.xy = asuint((asfloat(r0.xy) * asfloat(v3.yy)));
    r0.x = ((r2.x != 0u) ? r0.x : r0.y);
    r0.y = asuint((asfloat(asuint((_OpaquenessFadeByScript))) * asfloat(asuint((_AlphaFade)))));
    r0.x = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.y = ((asfloat(r0.x) < asfloat(asuint((_AlphaCutoff)))) ? 0xffffffffu : 0u);
    r0.x = ((r0.y != 0u) ? 0x00000000u : r0.x);
    r0.y = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xyz = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.z = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(mad(asfloat(r0.z), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r2.xyz = asuint(log2(asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r0.zzz) * asfloat(r2.xyz)));
    r2.xyz = asuint(exp2(asfloat(r2.xyz)));
    r0.yzw = ((r0.yyy != 0u) ? r2.xyz : r1.xyz);
    r1.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r1.w = asuint(max(asfloat(r0.x), asfloat(0x3a83126fu)));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r2.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r3.xyz = ((asfloat(r1.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.www), asfloat((r0.yzw ^ 0x80000000u))));
    r2.xyz = asuint(mad(asfloat(r3.xyz), asfloat(r2.xyz), asfloat(r0.yzw)));
    r1.x = asuint(max(asfloat(r1.y), asfloat(r1.x)));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r1.yzw = asuint(mad(asfloat(r2.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.yzw ^ 0x80000000u))));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(r1.yzw), asfloat(r0.yzw)));
    r1.x = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.x = ((asfloat(0x3a83126fu) < asfloat((r1.x & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r1.y = asuint(dot(asfloat(r0.yzw), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r2.xyz = asuint((asfloat(r0.yzw) + asfloat((r1.yyy ^ 0x80000000u))));
    r1.yzw = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r2.xyz), asfloat(r1.yyy)));
    r0.yzw = ((r1.xxx != 0u) ? r1.yzw : r0.yzw);
    o0.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r0.y = asuint((asfloat(asuint((_BlendMode))) + asfloat(0xbf800000u)));
    o0.w = asuint(mad(asfloat(r0.x), asfloat(r0.y), asfloat(0x3f800000u)));
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
