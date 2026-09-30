Shader "miHoYo/Particles/Particles_Decal"
{
    Properties
    {
        _Mode ("Mode", Float) = 0.0
        _UsingNonPSR ("Non Particle System Parameters@非粒子系统模式#用于模型的时候勾上", Float) = 0.0
        _DissolveProgress_NonPSR ("Dissolve Progress@溶解值", Float) = 1.0
        _DissolveEmissionProgress_NonPSR ("Dissolve Emission Progress@亮度溶解值", Float) = 1.0
        _CustomDataColorB ("Custom Data ColorB@边缘颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        _ScaleTexProgress ("ScaleTex Progress", Float) = 1.0
        _ZNetBakedVertexColor ("_ZNetBakedVertexColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _IsometricScaleUV ("Isometric Scaling UV@等比缩放UV", Float) = 0.0
        _group_custom_data ("CustomData", Float) = 0.0
        _CustomData1X ("CustomData1 X", Float) = 0.0
        _CustomData1Y ("CustomData1 Y", Float) = 0.0
        _CustomData1Z ("CustomData1 Z", Float) = 0.0
        _CustomData1W ("CustomData1 W", Float) = 0.0
        _group_maintex ("Main Maps", Float) = 0.0
        _MainTex ("Main Tex", 2D) = "white" {}
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _MainTexFlip ("Flip", Float) = 0.0
        _MainTexRotation ("Rotation", Float) = 0.0
        _UsePolarUV ("UsePolarUV", Float) = 0.0
        _UVMove ("UV Move", Float) = 0.0
        _UVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MultiplyParticleColor ("Multiply Particle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AlphaFade ("Alpha Fade", Float) = 1.0
        _VerticalAlphaFade ("Vertical Alpha Fade", Float) = 0.0
        _DepthShrinkToCenterUV ("Depth Shrink To Center UV@按深度向中心收缩UV", Float) = 0.0
        _group_parallax ("Parallax Mapping", Float) = 0.0
        _HeightChannelMapping ("Height Channel Mapping", Float) = 0.0
        _MaskChannelMapping ("Mask Channel Mapping", Float) = 0.0
        _ClampParallaxMappedUV ("Clamp Parallax Mapped UV", Float) = 0.0
        _ParallaxScale ("Parallax Scale", Float) = 0.0
        _RefPlane ("Ref Plane", Float) = 0.0
        _SidewallSteps ("Sidewall Steps", Float) = 2.0
        _group_pbr ("PBR Map", Float) = 0.0
        _BlendWithScene ("BlendWithScene", Float) = 0.0
        _BumpMap ("Bump Map", 2D) = "bump" {}
        _BumpMapScale ("Bump Scale", Float) = 1.0
        _MaskMap ("MaskMap(Metallic, Roughness, Emission, Occlusion)", 2D) = "red" {}
        _OcclusionScale ("Occlusion", Float) = 1.0
        _Roughness ("Roughness", Float) = 1.0
        _Metallic ("Metallic", Float) = 1.0
        _UseParallaxMap ("Use Parallax Map", Float) = 0.0
        _Parallax ("Scale", Float) = 0.005
        _ParallaxMap ("Height Map", 2D) = "black" {}
        _EmissionBlendBaseColor ("EmissionBlendBaseColor", Float) = 1.0
        _EmissionColor ("EmissionColor", Color) = (0.0, 0.0, 0.0, 0.0)
        _UseEmissionRampTex ("Use EmissionColor Ramp", Float) = 0.0
        _EmissionRampTex ("Ramp Tex", 2D) = "white" {}
        _EmissionRampV ("V Position", Float) = 0.0
        _EmissionRampMulti ("Correction", Float) = 1.0
        _EmissionRampTexUVMove ("UV Move", Float) = 0.0
        _EmissionRampTexUVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _group_2tone ("2 Tone", Float) = 0.0
        _ColorChannelMapping ("Color Channel Mapping", Float) = 0.0
        _AlphaChannelMapping ("Alpha Channel Mapping", Float) = 0.0
        _LerpBrightness ("Lerp Brightness", Float) = 1.0
        _group_scaleTex ("Scale Tex", Float) = 0.0
        _ScaleTex ("Scale Tex", 2D) = "white" {}
        _ScaleTexChannel ("Scale Tex Channel", Float) = 0.0
        _Scaler ("Scaler", Float) = 3.0
        _AutoScale ("AutoScale", Float) = 0.0
        _AutoSpeed ("AutoSpeed", Float) = 1.0
        _group_maskTex ("Mask Tex", Float) = 0.0
        _MaskTex ("Mask Tex", 2D) = "white" {}
        _MaskTexClampU ("Clamp U", Float) = 0.0
        _MaskTexClampV ("Clamp V", Float) = 0.0
        _MaskTexFlip ("Flip", Float) = 0.0
        _MaskTexRotation ("Rotation", Float) = 0.0
        _MaskTexChannel ("Mask Channel", Float) = 0.0
        _UseDissolveTex ("Dissolve", Float) = 0.0
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveTexClampU ("Clamp U", Float) = 0.0
        _DissolveTexClampV ("Clamp V", Float) = 0.0
        _DissolveTexFlip ("Flip", Float) = 0.0
        _DissolveTexRotation ("Rotation", Float) = 0.0
        _DissolveChannel ("Dissolve Channel", Float) = 0.0
        _DissolveUVSpeed ("Dissolve UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DissolveRandomUV ("Dissolve Random UV", Float) = 0.0
        _ParallaxBrightColor ("Parallax Bright Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SoftEdge ("Soft Edge", Float) = 0.0
        _DissolveEdgeRange ("Dissolve Edge Range", Float) = 0.1
        _BrightEdgeRange ("Bright Edge Range", Float) = 0.1
        _EdgeColorOverride ("Edge Color Override", Color) = (0.0, 0.0, 0.0, 0.0)
        _DissolveAffects2Tone ("Dissolve Affects 2 Tone", Float) = 1.0
        _UseDistortionTexture ("Distortion", Float) = 0.0
        _DistortionTex ("Distortion Tex", 2D) = "bump" {}
        _DistortionChannel ("Distortion Channel", Float) = 0.0
        _DistortionTexClampU ("Clamp U", Float) = 0.0
        _DistortionTexClampV ("Clamp V", Float) = 0.0
        _DistortionTexFlip ("Flip", Float) = 0.0
        _DistortionTexRotation ("Rotation", Float) = 0.0
        _DistortionUVSpeed ("Distortion UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DistortionRandomUV ("Distortion Random UV", Float) = 0.0
        _DistortionIntensity ("Distortion Intensity", Float) = 0.0
        _DistortionEmissionChannel ("Channel", Float) = 0.0
        _DistortionEffectEmission ("Intensity", Float) = 0.0
        _UseSphereFade ("Sphere Fade@球形遮罩", Float) = 0.0
        _AlphaSphereFadeUseLocal ("Use Local Position@使用本地(粒子)坐标", Float) = 0.0
        _AlphaSphereFadeCenter ("Center@中心点", Vector) = (0.0, 0.0, 0.0, 0.0)
        _AlphaSphereFadeOut ("Fade Out@外半径虚化强度", Float) = 0.0
        _AlphaSphereFadeIn ("Fade In@内半径虚化强度", Float) = 0.0
        _HollowMask ("Hollow Mask@中间镂空遮罩", Float) = 0.0
        _SphereFadeUseScreenUV ("Use Screen UV@使用屏幕UV", Float) = 0.0
        _SphereFadeKeepAspect ("Keep Aspect@保持宽高比", Float) = 0.0
        _FadeFromCameraOn ("FadeFromCamera@按相机距离渐隐", Float) = 0.0
        _FadeFromCamera_Invert ("Invert Alpha@反转Fade透明", Float) = 0.0
        _FadeFromCamera ("   Fade消失距离(离相机)", Float) = 1.0
        _RcpFadeLength ("   Fade过渡距离软硬参数", Float) = 1.0
        _group_normal_clip ("Clip", Float) = 0.0
        _NormalClip ("Normal Clip", Float) = 0.5
        _SoftClip ("Soft Clip", Float) = 1.0
        _MaskPassAlphaClip ("Alpha Clip", Float) = 0.01
        _group_render_state ("Render State", Float) = 0.0
        _BlendMode ("Blend Mode", Float) = 0.0
        _SrcFactor ("Src Factor", Float) = 1.0
        _DstFactor ("Dst Factor", Float) = 10.0
        _HalfResSrcFactor ("Src Factor", Float) = 1.0
        _HalfResDstFactor ("Dst Factor", Float) = 5.0
        _HalfResSrcAlphaFactor ("Src Factor", Float) = 7.0
        _HalfResDstAlphaFactor ("Dst Factor", Float) = 0.0
        _StencilComp ("Decal Objects", Float) = 6.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
        _TimeOffset ("TimeOffset", Float) = 0.0
        _ParticleDecalScale ("_ParticleDecalScale", Vector) = (1.0, 1.0, 1.0, 1.0)
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _ParticlesDecalParams ("Particle Decal Params", Vector) = (1.0, 1.0, 1.0, 0.0)
    }
    SubShader
    {
        Pass
        {
            Name "TransparentHalfResDecal"
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
    column_major float4x4 unity_MatrixVP : packoffset(c103);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float _UsingNonPSR : packoffset(c0);
    float _DissolveProgress_NonPSR : packoffset(c0.y);
    float _DissolveEmissionProgress_NonPSR : packoffset(c0.z);
    float4 _CustomDataColorB : packoffset(c1);
    float4 _ParticleDecalScale : packoffset(c15);
    float4 _MultiplyParticleColor : packoffset(c16);
    float _CustomData1X : packoffset(c19.z);
    float _CustomData1Y : packoffset(c19.w);
    float _CustomData1Z : packoffset(c20);
    float _CustomData1W : packoffset(c20.y);
    float4 _ZNetBakedVertexColor : packoffset(c29);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float3 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD5;
    float4 output7 : TEXCOORD6;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : COLOR0, float4 input2 : TEXCOORD0, float4 input3 : TEXCOORD1, float4 input4 : TEXCOORD2, float4 input5 : TEXCOORD3, float4 input6 : TEXCOORD4, float input7 : TEXCOORD5)
{
    uint4 r0, r1, r2, o0, o1, o2, o3, o4, o5, o6, o7, v0, v1, v2, v3, v4, v5, v6, v7;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.x = asuint(input7);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3])))), asfloat(r0.wwww), asfloat(r1.xyzw)));
    o1.xyz = v2.xyz;
    o2.xyz = v4.yzw;
    o2.w = v5.x;
    r0.xy = ((asfloat(asuint((float2(_CustomData1X, _CustomData1Y)))) < asfloat(uint2(0x3fc00000u, 0x3fc00000u))) ? 0xffffffffu : 0u);
    r1.xyz = ((asfloat(uint3(0x3f000000u, 0x3fc00000u, 0x3f000000u)) < asfloat(asuint((float3(_CustomData1X, _CustomData1X, _CustomData1Y))))) ? 0xffffffffu : 0u);
    r0.xy = (r0.xy & r1.xz);
    r0.x = (r0.x | r1.y);
    r1.xy = ((r0.xy != 0u) ? uint2(0x00000000u, 0x00000000u) : v5.yz);
    r0.x = ((asfloat(asuint((_UsingNonPSR))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    o3.xy = (r1.xy & r0.xx);
    r0.yz = ((asfloat(asuint((float2(_CustomData1Z, _CustomData1W)))) < asfloat(uint2(0x3fc00000u, 0x3fc00000u))) ? 0xffffffffu : 0u);
    r2.xyz = ((asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3fc00000u)) < asfloat(asuint((float3(_CustomData1Z, _CustomData1W, _CustomData1W))))) ? 0xffffffffu : 0u);
    r0.yz = (r0.yz & r2.xy);
    r0.z = (r0.z | r2.z);
    r1.z = ((r0.y != 0u) ? 0x00000000u : v5.w);
    r1.w = ((r0.z != 0u) ? 0x00000000u : v6.x);
    o3.zw = ((r0.xx != 0u) ? r1.zw : asuint((float2(_DissolveProgress_NonPSR, _DissolveEmissionProgress_NonPSR))));
    r0.yzw = asuint((asfloat(asuint((float3(_ZNetBakedVertexColor.x, _ZNetBakedVertexColor.y, _ZNetBakedVertexColor.z)))) + asfloat(uint3(0x3d6147aeu, 0x3d6147aeu, 0x3d6147aeu))));
    r0.yzw = asuint((asfloat(r0.yzw) * asfloat(uint3(0x3f72a76fu, 0x3f72a76fu, 0x3f72a76fu))));
    r0.yzw = asuint(log2(asfloat((r0.yzw & 0x7fffffffu))));
    r0.yzw = asuint((asfloat(r0.yzw) * asfloat(uint3(0x4019999au, 0x4019999au, 0x4019999au))));
    r0.yzw = asuint(exp2(asfloat(r0.yzw)));
    r1.xyz = asuint((asfloat(asuint((float3(_ZNetBakedVertexColor.x, _ZNetBakedVertexColor.y, _ZNetBakedVertexColor.z)))) * asfloat(uint3(0x3d9e8391u, 0x3d9e8391u, 0x3d9e8391u))));
    r2.xyz = ((asfloat(uint3(0x3d25aee6u, 0x3d25aee6u, 0x3d25aee6u)) >= asfloat(asuint((float3(_ZNetBakedVertexColor.x, _ZNetBakedVertexColor.y, _ZNetBakedVertexColor.z))))) ? 0xffffffffu : 0u);
    r0.yzw = ((r2.xyz != 0u) ? r1.xyz : r0.yzw);
    r0.yzw = ((r0.xxx != 0u) ? v1.xyz : r0.yzw);
    o4.xyz = asuint((asfloat(r0.yzw) * asfloat(asuint((float3(_MultiplyParticleColor.x, _MultiplyParticleColor.y, _MultiplyParticleColor.z))))));
    o4.w = ((r0.x != 0u) ? v1.w : asuint((_ZNetBakedVertexColor.w)));
    r1.xyz = v6.yzw;
    r1.w = v7.x;
    o5.xyzw = ((r0.xxxx != 0u) ? r1.xyzw : asuint((float4(_CustomDataColorB.x, _CustomDataColorB.y, _CustomDataColorB.z, _CustomDataColorB.w))));
    o6.xyz = v5.yzw;
    o6.w = v6.x;
    r1.x = v2.w;
    r1.yz = v3.xy;
    r0.yzw = asuint((asfloat(asuint((float3(_ParticleDecalScale.x, _ParticleDecalScale.y, _ParticleDecalScale.z)))) / asfloat(r1.xyz)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_ParticleDecalScale.w)))) ? 0xffffffffu : 0u);
    r0.yzw = ((r1.xxx != 0u) ? r0.yzw : uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    o7.xyz = ((r0.xxx != 0u) ? r0.yzw : uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    o7.w = 0x3f800000u;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyz);
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
    column_major float4x4 _GlobalTimeParamsA : packoffset(c14);
    column_major float4x4 _GlobalTimeParamsB : packoffset(c18);
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c32);
    column_major float4x4 _InvViewProjMatrix : packoffset(c135);
    float4 _ScreenSize : packoffset(c139);
    float4 _NapEffectBrightnessParams4 : packoffset(c170);
    float4 _NapEffectBrightnessExtraParams : packoffset(c171);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
    column_major float4x4 unity_WorldToObject : packoffset(c4);
}

cbuffer UnityPerMaterial : register(b2)
{
    column_major float4x4 _ParticleDecalWorldToLocalMatrix : packoffset(c7);
    column_major float4x4 _ParticleDecalLocalToWorldMatrix : packoffset(c11);
    float _UsingNonPSR : packoffset(c0);
    float4 _MainTex_ST : packoffset(c2);
    float2 _UVSpeed : packoffset(c20.z);
    float _MainTexClampU : packoffset(c21);
    float _MainTexClampV : packoffset(c21.y);
    float _MainTexFlip : packoffset(c21.z);
    float _MainTexRotation : packoffset(c21.w);
    float _UVMove : packoffset(c22);
    float _ColorChannelMapping : packoffset(c22.y);
    float _AlphaChannelMapping : packoffset(c22.z);
    float _LerpBrightness : packoffset(c22.w);
    float _NormalClip : packoffset(c23);
    float _MaskPassAlphaClip : packoffset(c23.z);
    float _BlendMode : packoffset(c23.w);
    float _OpaquenessFadeByScript : packoffset(c24);
    float _IgnoreTimeScale : packoffset(c24.y);
    float _TimeOffset : packoffset(c24.z);
    float _AlphaFade : packoffset(c25);
    float _VerticalAlphaFade : packoffset(c25.y);
    float _DepthShrinkToCenterUV : packoffset(c25.z);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };



SamplerState sampler_CameraDepthTexture;
SamplerState sampler_CameraNormalTexture;
SamplerState sampler_MainTex;
Texture2D<float4> _CameraDepthTexture : register(t0);
Texture2D<float4> _CameraNormalTexture : register(t1);
Texture2D<float4> _MainTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float3 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float4 input5 : TEXCOORD4, float4 input6 : TEXCOORD5, float4 input7 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, v0, v1, v2, v3, v4, v5, v6, v7;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    r0.xy = asuint((asfloat(v0.xy) * asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w))))));
    r1.xyzw = asuint(_CameraNormalTexture.Sample(sampler_CameraDepthTexture, asfloat(r0.xy)).xyzw);
    r0.z = ((asfloat(0x3f000000u) < asfloat(r1.w)) ? 0xffffffffu : 0u);
    if (r0.z != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u);
        result.output0 = asfloat(o0.xyzw);
        return result;
    }
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.z = asuint((asfloat(r0.z) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r0.w = asuint(_CameraDepthTexture.SampleLevel(sampler_CameraNormalTexture, asfloat(r0.xy), asfloat(0x00000000u)).x);
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r2.xyzw = asuint((asfloat((r0.yyyy ^ 0x80000000u)) * asfloat(asuint((float4(_InvViewProjMatrix[0][1], _InvViewProjMatrix[1][1], _InvViewProjMatrix[2][1], _InvViewProjMatrix[3][1]))))));
    r2.xyzw = asuint(mad(asfloat(asuint((float4(_InvViewProjMatrix[0][0], _InvViewProjMatrix[1][0], _InvViewProjMatrix[2][0], _InvViewProjMatrix[3][0])))), asfloat(r0.xxxx), asfloat(r2.xyzw)));
    r2.xyzw = asuint(mad(asfloat(asuint((float4(_InvViewProjMatrix[0][2], _InvViewProjMatrix[1][2], _InvViewProjMatrix[2][2], _InvViewProjMatrix[3][2])))), asfloat(r0.wwww), asfloat(r2.xyzw)));
    r2.xyzw = asuint((asfloat(r2.xyzw) + asfloat(asuint((float4(_InvViewProjMatrix[0][3], _InvViewProjMatrix[1][3], _InvViewProjMatrix[2][3], _InvViewProjMatrix[3][3]))))));
    r0.xyw = asuint((asfloat(r2.xyz) / asfloat(r2.www)));
    r1.w = ((asfloat(asuint((_UsingNonPSR))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xyz = asuint((asfloat(v1.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r3.x = ((r1.w != 0u) ? r2.x : asuint((unity_ObjectToWorld[3][0])));
    r3.y = ((r1.w != 0u) ? r2.y : asuint((unity_ObjectToWorld[3][1])));
    r3.z = ((r1.w != 0u) ? r2.z : asuint((unity_ObjectToWorld[3][2])));
    r0.xyw = asuint((asfloat(r0.xyw) + asfloat((r3.xyz ^ 0x80000000u))));
    r2.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(_ParticleDecalWorldToLocalMatrix[0][1], _ParticleDecalWorldToLocalMatrix[1][1], _ParticleDecalWorldToLocalMatrix[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_ParticleDecalWorldToLocalMatrix[0][0], _ParticleDecalWorldToLocalMatrix[1][0], _ParticleDecalWorldToLocalMatrix[2][0])))), asfloat(r0.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_ParticleDecalWorldToLocalMatrix[0][2], _ParticleDecalWorldToLocalMatrix[1][2], _ParticleDecalWorldToLocalMatrix[2][2])))), asfloat(r0.www), asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r2.xyz) + asfloat(asuint((float3(_ParticleDecalWorldToLocalMatrix[0][3], _ParticleDecalWorldToLocalMatrix[1][3], _ParticleDecalWorldToLocalMatrix[2][3]))))));
    r3.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r3.xyz = asuint(mad(asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0])))), asfloat(r0.xxx), asfloat(r3.xyz)));
    r0.xyw = asuint(mad(asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2])))), asfloat(r0.www), asfloat(r3.xyz)));
    r0.xyw = asuint((asfloat(r0.xyw) + asfloat(asuint((float3(unity_WorldToObject[0][3], unity_WorldToObject[1][3], unity_WorldToObject[2][3]))))));
    r0.xyw = ((r1.www != 0u) ? r2.xyz : r0.xyw);
    r0.xyw = asuint((asfloat(r0.xyw) * asfloat(v7.xyz)));
    r2.xyz = asuint((asfloat(r0.xyw) * asfloat(uint3(0x3f800000u, 0xbf800000u, 0x3f800000u))));
    r0.xyw = asuint(mad(asfloat(r0.xyw), asfloat(uint3(0x3f800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_DepthShrinkToCenterUV)))) ? 0xffffffffu : 0u);
    r2.w = asuint((asfloat((r2.y & 0x7fffffffu)) + asfloat((r2.y & 0x7fffffffu))));
    r2.w = asuint(mad(asfloat((r2.w ^ 0x80000000u)), asfloat(r2.w), asfloat(0x3f800000u)));
    r2.w = asuint(max(asfloat(r2.w), asfloat(0x00000000u)));
    r2.w = asuint(sqrt(asfloat(r2.w)));
    r2.w = asuint(max(asfloat(r2.w), asfloat(0x3a83126fu)));
    r2.xz = asuint((asfloat(r2.xz) / asfloat(r2.ww)));
    r2.xz = asuint((asfloat(r2.xz) + asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r3.xy = ((r1.ww != 0u) ? r2.xz : r0.xw);
    r1.w = asuint(dot(asfloat(asuint((float3(_ParticleDecalLocalToWorldMatrix[0][1], _ParticleDecalLocalToWorldMatrix[1][1], _ParticleDecalLocalToWorldMatrix[2][1])))), asfloat(asuint((float3(_ParticleDecalLocalToWorldMatrix[0][1], _ParticleDecalLocalToWorldMatrix[1][1], _ParticleDecalLocalToWorldMatrix[2][1]))))));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r2.xzw = asuint((asfloat(r1.www) * asfloat(asuint((float3(_ParticleDecalLocalToWorldMatrix[0][1], _ParticleDecalLocalToWorldMatrix[1][1], _ParticleDecalLocalToWorldMatrix[2][1]))))));
    r3.zw = asuint((asfloat((r3.xy ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r4.xyzw = asuint((asfloat((r3.xyzw ^ 0x80000000u)) + asfloat(r3.yzwx)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_MainTexRotation, _MainTexRotation, _MainTexRotation, _MainTexRotation)))), asfloat(r4.xyzw), asfloat(r3.xyzw)));
    r4.xyzw = ((asfloat(asuint((float4(_MainTexFlip, _MainTexFlip, _MainTexFlip, _MainTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r5.xyzw = (r4.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r4.xyz = ((r4.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r4.xyz = asuint((asfloat(r4.xyz) + asfloat(r5.yzw)));
    r4.xyz = asuint(max(asfloat(r4.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r6.xy = r3.zw;
    r6.z = 0x3f800000u;
    r6.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r7.xy = r3.xw;
    r7.z = 0x00000000u;
    r7.w = asuint((asfloat((asuint((_MainTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r8.xyzw = asuint((asfloat(r4.yyyy) * asfloat(r7.xyzw)));
    r8.xyzw = asuint(mad(asfloat(r6.xyzw), asfloat(r4.zzzz), asfloat(r8.xyzw)));
    r3.xz = r6.xz;
    r3.w = asuint((_MainTexRotation));
    r4.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r4.xxxx), asfloat(r8.xyzw)));
    r3.xz = r7.xz;
    r3.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r5.xxxx), asfloat(r4.xyzw)));
    r3.xy = asuint((asfloat((r3.zw ^ 0x80000000u)) + asfloat(r3.xy)));
    r3.xy = asuint(mad(asfloat(r3.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r3.xy = asuint((asfloat(r3.zw) + asfloat(r3.xy)));
    r3.zw = ((asfloat(uint2(0x3f000000u, 0x3f000000u)) < asfloat(asuint((float2(_MainTexClampU, _MainTexClampV))))) ? 0xffffffffu : 0u);
    r4.xy = asuint(max(asfloat(r3.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r4.xy = asuint(min(asfloat(r4.xy), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r3.xy = ((r3.zw != 0u) ? r4.xy : r3.xy);
    r3.zw = asuint(mad(asfloat(asuint((float2(_UVSpeed.x, _UVSpeed.y)))), asfloat(r0.zz), asfloat(v3.xy)));
    r3.xy = asuint(mad(asfloat(asuint((float2(_UVMove, _UVMove)))), asfloat(r3.zw), asfloat(r3.xy)));
    r3.xyzw = asuint(_MainTex.SampleLevel(sampler_MainTex, asfloat(r3.xy), asfloat(0x00000000u)).xyzw);
    r0.z = (uint)(asfloat(asuint((_AlphaChannelMapping))));
    r0.z = min(r0.z, 0x00000003u);
    r0.z = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r0.z+0].xyzw)));
    r1.w = ((asfloat(r0.z) < asfloat(asuint((_MaskPassAlphaClip)))) ? 0xffffffffu : 0u);
    if (r1.w != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u);
        result.output0 = asfloat(o0.xyzw);
        return result;
    }
    r4.xyz = ((asfloat(r0.xyw) < asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))) ? 0xffffffffu : 0u);
    r1.w = (r4.y | r4.x);
    r1.w = (r4.z | r1.w);
    r0.xyw = ((asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u)) < asfloat(r0.xyw)) ? 0xffffffffu : 0u);
    r0.x = (r0.y | r0.x);
    r0.x = (r0.w | r0.x);
    r0.x = (r0.x | r1.w);
    if (r0.x != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u);
        result.output0 = asfloat(o0.xyzw);
        return result;
    }
    r0.xyw = asuint(mad(asfloat(r1.xyz), asfloat(uint3(0x40000000u, 0x40000000u, 0x40000000u)), asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r0.x = asuint(dot(asfloat(r0.xyw), asfloat(r2.xzw)));
    r0.x = asuint(saturate(mad(asfloat(r0.x), asfloat(0x3f000000u), asfloat(0x3f000000u))));
    r0.x = asuint((asfloat(r0.x) + asfloat((asuint((_NormalClip)) ^ 0x80000000u))));
    r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u);
        result.output0 = asfloat(o0.xyzw);
        return result;
    }
    r0.xyw = asuint(mad(asfloat(r1.xyz), asfloat(uint3(0x40000000u, 0x40000000u, 0x40000000u)), asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r0.x = asuint(dot(asfloat(r0.xyw), asfloat(r2.xzw)));
    r0.x = asuint(saturate(mad(asfloat(r0.x), asfloat(0x3f000000u), asfloat(0x3f000000u))));
    r0.y = ((asfloat(0x40600000u) < asfloat(asuint((_ColorChannelMapping)))) ? 0xffffffffu : 0u);
    r0.w = (uint)(asfloat(asuint((_ColorChannelMapping))));
    r0.w = min(r0.w, 0x00000003u);
    r0.w = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r0.w+0].xyzw)));
    r0.y = ((r0.y != 0u) ? 0x3f800000u : r0.w);
    r0.z = asuint((asfloat(r0.z) * asfloat(v4.w)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.z)));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r0.y = asuint(saturate((asfloat(r0.y) * asfloat(asuint((_LerpBrightness))))));
    r0.z = ((asfloat(asuint((_ColorChannelMapping))) < asfloat(0x40600000u)) ? 0xffffffffu : 0u);
    if (r0.z != 0u) {
        r1.xyz = asuint((asfloat(v4.xyz) + asfloat((v5.xyz ^ 0x80000000u))));
        r1.xyz = asuint(mad(asfloat(r0.yyy), asfloat(r1.xyz), asfloat(v5.xyz)));
    } else {
        r2.xzw = asuint((asfloat(v4.xyz) + asfloat((v5.xyz ^ 0x80000000u))));
        r0.yzw = asuint(mad(asfloat(r0.yyy), asfloat(r2.xzw), asfloat(v5.xyz)));
        r1.xyz = asuint((asfloat(r0.yzw) * asfloat(r3.xyz)));
    }
    r0.y = ((asfloat(asuint((_BlendMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.xzw = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.z = asuint((asfloat((asuint((_OpaquenessFadeByScript)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(mad(asfloat(r0.z), asfloat(0x3e99999au), asfloat(0x3f800000u)));
    r2.xzw = asuint(log2(asfloat(r2.xzw)));
    r2.xzw = asuint((asfloat(r0.zzz) * asfloat(r2.xzw)));
    r2.xzw = asuint(exp2(asfloat(r2.xzw)));
    r0.yzw = ((r0.yyy != 0u) ? r2.xzw : r1.xyz);
    r1.xyz = asuint((asfloat(r0.xxx) * asfloat(r0.yzw)));
    r1.w = asuint(max(asfloat(r0.x), asfloat(0x3a83126fu)));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r2.xzw = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r3.xyz = ((asfloat(r1.xyz) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r3.xyz = (r3.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.xzw = asuint(mad(asfloat(r2.xzw), asfloat(r1.www), asfloat((r0.yzw ^ 0x80000000u))));
    r2.xzw = asuint(mad(asfloat(r3.xyz), asfloat(r2.xzw), asfloat(r0.yzw)));
    r1.x = asuint(max(asfloat(r1.y), asfloat(r1.x)));
    r1.x = asuint(max(asfloat(r1.z), asfloat(r1.x)));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r1.x = asuint(saturate((asfloat(r1.x) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r1.yzw = asuint(mad(asfloat(r2.xzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r0.yzw ^ 0x80000000u))));
    r0.yzw = asuint(mad(asfloat(r1.xxx), asfloat(r1.yzw), asfloat(r0.yzw)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(asuint((_AlphaFade))))));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_VerticalAlphaFade)))) ? 0xffffffffu : 0u);
    r1.y = asuint(mad(asfloat(((r2.y & 0x7fffffffu) ^ 0x80000000u)), asfloat(0x40000000u), asfloat(0x3f800000u)));
    r1.y = asuint((asfloat(r0.x) * asfloat(r1.y)));
    r0.x = ((r1.x != 0u) ? r1.y : r0.x);
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
