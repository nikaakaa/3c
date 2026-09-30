Shader "miHoYo/Scene/Lit_PropFXSimplify"
{
    Properties
    {
        _group_basic ("Basic@基本参数", Float) = 0.0
        _CustomData1X ("CustomData1X", Float) = 0.0
        _group_main ("Main Maps", Float) = 0.0
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BaseMap ("Base Map", 2D) = "white" {}
        _MaskTex ("Mask Tex", 2D) = "red" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Float) = 0.0
        _Glossiness ("Smoothness", Float) = 0.1
        _Emission ("Emission", Float) = 0.0
        _EmissionColor ("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _EmissionOverAlbedo ("Emission Over Albedo", Float) = 0.0
        _UseUV3ForEmission ("使用第四套UV采样Mask图的自发光通道", Float) = 0.0
        _Fresnel ("Fresnel", Float) = 0.0
        _FresnelColor ("Fresnel Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FresnelPower ("Fresnel Power", Float) = 5.0
        _FresnelScale ("Fresnel Scale", Float) = 1.0
        _FresnelOffset ("Fresnel Offset", Float) = 0.0
        _FlowTexWithFresnel ("FlowTex", Float) = 0.0
        _FlowTex ("Flow Texture", 2D) = "black" {}
        _FlowTexSpeed ("FlowTex XYZSpeed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _FlowTexSize ("FlowTex Size", Float) = 5.0
        _FlowTexTransition ("FlowTex Transition", Float) = 1.0
        _FlowTexFresnelColor ("FlowTex Fresnel Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FlowTexFresnelPower ("FlowTex Fresnel Power", Float) = 5.0
        _FlowTexFresnelScale ("FlowTex Fresnel Scale", Float) = 1.0
        _FlowTexFresnelOffset ("FlowTex Fresnel Offset", Float) = 0.0
        _RigidAnimation ("Rigid Animation", Float) = 0.0
        _PositionTexture ("Position Texture", 2D) = "linearGray" {}
        _RotationTexture ("Rotation Texture", 2D) = "black" {}
        _BoundaryMin ("Boundary Min", Vector) = (-1.0, -1.0, -1.0, 1.0)
        _BoundaryMax ("Boundary Max", Vector) = (1.0, 1.0, 1.0, 1.0)
        _Frame ("Frame", Float) = 0.0
        _LastFrame ("Last Frame", Float) = 0.0
        _WrapMode ("Wrap Mode", Float) = 0.0
        _VertexAnimation ("Vertex Animation", Float) = 0.0
        _VertexPositionTex ("Vertex Position", 2D) = "black" {}
        _VertexNormalTex ("Vertex Normal", 2D) = "bump" {}
        _CurrentFrame ("Current Frame", Float) = 0.0
        _VAT_LastFrame ("Last Frame", Float) = 0.0
        _Frames ("Frames", Float) = 24.0
        _CollideWithAvatar ("Collide with Avatar", Float) = 0.0
        _CollisionRadius ("Collision Radius", Vector) = (1.0, 1.5, 0.0, 0.0)
        _UseParticleCenter ("Use Particle Center", Float) = 0.0
        _AvatarYOffset ("Avatar Y Offset", Float) = 1.0
        _group_dither ("Dither", Float) = 0.0
        _DitherAlpha ("Dither Alpha", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2", Float) = 1.0
        _DirectionalAlpha ("Directional Alpha@方向透明", Float) = 0.0
        _DirectionalAlphaPivot ("Dither Pivot@起始点(物体坐标)", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DirectionalAlphaDirection ("Dither Direction@方向", Vector) = (0.0, 1.0, 0.0, 0.0)
        _DirectionalAlphaEdgeSoftness ("Dither Edge Softness@过渡距离", Float) = 1.0
        _DirectionalAlphaEdgeOffset ("Dither Edge Offset@过渡位移", Float) = 0.0
        _group_mat_cap_on ("Map Cap", Float) = 0.0
        _MatCapTex ("MatCap Tex", 2D) = "black" {}
        _MatCapTint ("MatCap Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _MatCapRoughness ("MatCap Roughness", Float) = 0.1
        _MatCapPow ("MatCap Pow", Float) = 1.0
        _group_screenImage ("Screen Image@屏幕贴图", Float) = 0.0
        _ScreenImage ("On@开关", Float) = 1.0
        _MultiplySrcColor ("Multiply Source Color@正片叠底", Float) = 0.0
        _ScreenColor ("Screen Color@贴图颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        _ScreenTex ("Screen Texture@屏幕贴图", 2D) = "gray" {}
        _ScreenTexRotation ("Rotation@贴图旋转", Float) = 0.0
        _ScreenTexRotationAxis ("Rotation Axis@旋转锚点", Vector) = (0.5, 0.5, 0.0, 0.0)
        _ScreenMask ("Screen Mask@屏幕遮罩", 2D) = "white" {}
        _ScreenMaskUV ("Screen Mask UV@遮罩贴图UV", Float) = 0.0
        _UseInvSecondaryEmissionMask ("Use Inverted Secondary Emission Mask@使用次级发光的反相遮罩", Float) = 0.0
        _ScreenImageUvMove ("UV Speed And Offset@屏幕贴图UV速度和偏移", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MatCapNormalVSpeedFx ("遮罩U速度", Float) = 0.0
        _MatCapBlendModeFx ("遮罩V速度", Float) = 0.0
        _Blink ("Blink@闪烁", Float) = 0.0
        _BlinkFrequency ("Blink Frequency@闪烁频率", Float) = 1.0
        _BlinkOpacity ("Blink Opacity(z: Normal Distortion)@闪烁透明度(z: 法线扭曲强度)", Vector) = (0.0, 1.0, 0.0, 0.0)
        _GroupFishAnim ("Fish Anim@鱼动画选项", Float) = 0.0
        _FishAnimFrequency ("Frequency", Float) = 16.0
        _FishAnimSpeed ("Speed", Float) = 16.0
        _FishAnimAmplitude ("Amplitude", Float) = 0.1
        _StencilClip ("Stencil Clip", Float) = 0.0
        _Stencil ("Stencil 是否受特效贴花影响", Float) = 4.0
        _Group_SecondaryEmission ("Secondary Emission@次级发光", Float) = 0.0
        _SecondaryEmission ("On@开关", Float) = 1.0
        _SecondaryEmissionColor ("Emission Color@次级发光颜色", Color) = (1.0, 1.0, 1.0, 1.0)
        _SecondaryEmissionTex ("Emission Texture@次级发光贴图", 2D) = "white" {}
        _SecondaryEmissionUseUV2 ("Using UV2@使用UV2", Float) = 1.0
        _SecondaryEmissionChannel ("Emission Channel@发光通道", Float) = 0.0
        _MultiplyAlbedo ("Multiply Albedo@使用主贴图颜色", Float) = 1.0
        _SecondaryEmissionMaskTex ("Mask Tex@遮罩贴图", 2D) = "white" {}
        _SecondaryEmissionMaskChannel ("Mask Channel@遮罩通道", Float) = 0.0
        _SecondaryEmissionTexSpeed ("Speed@流动速度(XY_发光图,ZW_遮罩)", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SecondaryEmissionBlendMode ("Emission Blend Mode@混合模式(混合到自发光上)", Float) = 0.0
        _group_AdvancedOptions ("Advanced Options", Float) = 0.0
        _AffectedByFxSaturation ("AffectedByFxSaturation", Float) = 0.0
        _Cull ("Cull", Float) = 2.0
        _DoubleSided ("Double Sided", Float) = 0.0
        _IgnoreTimeScale ("Ignore Time Scale", Float) = 0.0
    }
    SubShader
    {
        Pass
        {
            Name "OpaqueForwardAfterDeferredShading"
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
    column_major float4x4 _PrevViewProjMatrix : packoffset(c110);
    column_major float4x4 _NonJitteredViewProjMatrix : packoffset(c118);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
    column_major float4x4 unity_WorldToObject : packoffset(c4);
    column_major float4x4 unity_MatrixPreviousM : packoffset(c20);
    float4 unity_WorldTransformParams : packoffset(c9);
    float4 unity_SHAr : packoffset(c13);
    float4 unity_SHAg : packoffset(c14);
    float4 unity_SHAb : packoffset(c15);
    float4 unity_SHBr : packoffset(c16);
    float4 unity_SHBg : packoffset(c17);
    float4 unity_SHBb : packoffset(c18);
    float4 unity_SHC : packoffset(c19);
}


struct CorinVertexOut {
    float4 output0 : TEXCOORD0;
    float4 output1 : TEXCOORD1;
    float4 output2 : TEXCOORD2;
    float4 output3 : TEXCOORD3;
    float4 output4 : TEXCOORD4;
    float4 output5 : TEXCOORD5;
    float3 output6 : TEXCOORD6;
    float2 output7 : TEXCOORD8;
    float4 output8 : SV_POSITION0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float4 input4 : TEXCOORD0, float2 input5 : TEXCOORD1)
{
    uint4 r0, r1, r2, r3, o0, o1, o2, o3, o4, o5, o6, o7, o8, v0, v1, v2, v3, v4, v5;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xy = asuint(input5);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_MatrixPreviousM[0][1], unity_MatrixPreviousM[1][1], unity_MatrixPreviousM[2][1], unity_MatrixPreviousM[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][0], unity_MatrixPreviousM[1][0], unity_MatrixPreviousM[2][0], unity_MatrixPreviousM[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][2], unity_MatrixPreviousM[1][2], unity_MatrixPreviousM[2][2], unity_MatrixPreviousM[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixPreviousM[0][3], unity_MatrixPreviousM[1][3], unity_MatrixPreviousM[2][3], unity_MatrixPreviousM[3][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(_PrevViewProjMatrix[0][1], _PrevViewProjMatrix[1][1], _PrevViewProjMatrix[3][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][0], _PrevViewProjMatrix[1][0], _PrevViewProjMatrix[3][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][2], _PrevViewProjMatrix[1][2], _PrevViewProjMatrix[3][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][3], _PrevViewProjMatrix[1][3], _PrevViewProjMatrix[3][3])))), asfloat(r0.www), asfloat(r0.xyz)));
    o0.zw = r0.xy;
    o1.z = r0.z;
    o0.xy = v4.xy;
    r0.xyz = asuint((asfloat(v0.xxx) * asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]))))));
    r1.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(r1.xyz)));
    r1.xyz = asuint((asfloat(v0.zzz) * asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2]))))));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(r1.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][1], _NonJitteredViewProjMatrix[1][1], _NonJitteredViewProjMatrix[3][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][0], _NonJitteredViewProjMatrix[1][0], _NonJitteredViewProjMatrix[3][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][2], _NonJitteredViewProjMatrix[1][2], _NonJitteredViewProjMatrix[3][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    o1.xyw = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][3], _NonJitteredViewProjMatrix[1][3], _NonJitteredViewProjMatrix[3][3]))))));
    o2.w = r0.x;
    r1.x = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r1.y = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r1.z = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r0.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r1.xyz)));
    o2.xyz = r1.xyz;
    r2.xyz = asuint((asfloat(v2.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v2.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v2.zzz), asfloat(r2.xyz)));
    r0.w = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r2.xyz = asuint((asfloat(r0.www) * asfloat(r2.xyz)));
    o3.xyz = r2.xyz;
    o3.w = r0.y;
    r3.xyz = asuint((asfloat(r1.zxy) * asfloat(r2.yzx)));
    r2.xyz = asuint(mad(asfloat(r1.yzx), asfloat(r2.zxy), asfloat((r3.xyz ^ 0x80000000u))));
    r0.w = asuint((asfloat(v2.w) * asfloat(asuint((unity_WorldTransformParams.w)))));
    o4.xyz = asuint((asfloat(r0.www) * asfloat(r2.xyz)));
    o4.w = r0.z;
    o5.xyzw = v3.xyzw;
    r0.w = asuint((asfloat(r1.y) * asfloat(r1.y)));
    r0.w = asuint(mad(asfloat(r1.x), asfloat(r1.x), asfloat((r0.w ^ 0x80000000u))));
    r2.xyzw = asuint((asfloat(r1.yzzx) * asfloat(r1.xyzz)));
    r3.x = asuint(dot(asfloat(asuint((float4(unity_SHBr.x, unity_SHBr.y, unity_SHBr.z, unity_SHBr.w)))), asfloat(r2.xyzw)));
    r3.y = asuint(dot(asfloat(asuint((float4(unity_SHBg.x, unity_SHBg.y, unity_SHBg.z, unity_SHBg.w)))), asfloat(r2.xyzw)));
    r3.z = asuint(dot(asfloat(asuint((float4(unity_SHBb.x, unity_SHBb.y, unity_SHBb.z, unity_SHBb.w)))), asfloat(r2.xyzw)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_SHC.x, unity_SHC.y, unity_SHC.z)))), asfloat(r0.www), asfloat(r3.xyz)));
    r1.w = 0x3f800000u;
    r3.x = asuint(dot(asfloat(asuint((float4(unity_SHAr.x, unity_SHAr.y, unity_SHAr.z, unity_SHAr.w)))), asfloat(r1.xyzw)));
    r3.y = asuint(dot(asfloat(asuint((float4(unity_SHAg.x, unity_SHAg.y, unity_SHAg.z, unity_SHAg.w)))), asfloat(r1.xyzw)));
    r3.z = asuint(dot(asfloat(asuint((float4(unity_SHAb.x, unity_SHAb.y, unity_SHAb.z, unity_SHAb.w)))), asfloat(r1.xyzw)));
    r1.xyz = asuint((asfloat(r2.xyz) + asfloat(r3.xyz)));
    o6.xyz = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    o7.xy = v5.xy;
    r1.xyzw = asuint((asfloat(r0.xxxx) * asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0]))))));
    r2.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r0.xyzw = asuint((asfloat(r0.zzzz) * asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2]))))));
    r1.xyzw = asuint((asfloat(r1.xyzw) + asfloat(r2.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(r1.xyzw)));
    o8.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyz);
    result.output7 = asfloat(o7.xy);
    result.output8 = asfloat(o8.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart1 : packoffset(c31);
    float3 _WorldSpaceCameraPos : packoffset(c30);
    float _GlobalMipBias : packoffset(c61);
}

cbuffer UnityPerDraw : register(b1)
{
    float4 unity_MotionVectorsParams : packoffset(c28);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _BaseMap_ST : packoffset(c1);
    float4 _Color : packoffset(c3);
    float4 _EmissionColor : packoffset(c4);
    float4 _FresnelColor : packoffset(c5);
    float _Emission : packoffset(c6.z);
    float _Fresnel : packoffset(c7);
    float _FresnelPower : packoffset(c7.y);
    float _FresnelScale : packoffset(c7.z);
    float _FresnelOffset : packoffset(c7.w);
}



SamplerState sampler_BaseMap;
SamplerState sampler_BumpMap;
SamplerState sampler_MaskTex;
Texture2D<float4> _BaseMap : register(t0);
Texture2D<float4> _BumpMap : register(t1);
Texture2D<float4> _MaskTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
};

CorinFragmentOut CorinPixel(float4 input0 : TEXCOORD0, float4 input1 : TEXCOORD1, float4 input2 : TEXCOORD2, float4 input3 : TEXCOORD3, float4 input4 : TEXCOORD4, float4 input5 : TEXCOORD5, float3 input6 : TEXCOORD6, float2 input7 : TEXCOORD8, float4 input8 : SV_POSITION0, bool input9 : SV_IsFrontFace0)
{
    uint4 r0, r1, r2, o0, o1, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyz = asuint(input6);
    v7.xy = asuint(input7);
    v8.xyzw = asuint(input8);
    v9.x = (input9 ? 0xffffffffu : 0u);
    r0.x = v2.w;
    r0.y = v3.w;
    r0.z = v4.w;
    r0.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z))))));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(r0.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r0.xyz = asuint((asfloat(r0.www) * asfloat(r0.xyz)));
    r1.xy = asuint(mad(asfloat(v0.xy), asfloat(asuint((float2(_BaseMap_ST.x, _BaseMap_ST.y)))), asfloat(asuint((float2(_BaseMap_ST.z, _BaseMap_ST.w))))));
    r2.xyz = asuint(_BumpMap.SampleBias(sampler_BumpMap, asfloat(r1.xy), asfloat(asuint((_GlobalMipBias)))).xyw);
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.z)));
    r1.zw = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r0.w = asuint(dot(asfloat(r1.zw), asfloat(r1.zw)));
    r0.w = asuint(min(asfloat(r0.w), asfloat(0x3f800000u)));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.w = asuint(sqrt(asfloat(r0.w)));
    r0.w = ((v9.x != 0u) ? r0.w : (r0.w ^ 0x80000000u));
    r2.xyz = asuint((asfloat(r1.www) * asfloat(v4.xyz)));
    r2.xyz = asuint(mad(asfloat(r1.zzz), asfloat(v3.xyz), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(r0.www), asfloat(v2.xyz), asfloat(r2.xyz)));
    r0.w = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r2.xyz = asuint((asfloat(r0.www) * asfloat(r2.xyz)));
    r0.x = asuint(saturate(dot(asfloat(r2.xyz), asfloat(r0.xyz))));
    r0.x = asuint((asfloat((r0.x ^ 0x80000000u)) + asfloat(asuint((_FresnelOffset)))));
    r0.x = asuint(saturate((asfloat(r0.x) + asfloat(0x3f800000u))));
    r0.x = asuint(log2(asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_FresnelPower)))));
    r0.x = asuint(exp2(asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_FresnelScale)))));
    r0.xyz = asuint((asfloat(r0.xxx) * asfloat(asuint((float3(_FresnelColor.x, _FresnelColor.y, _FresnelColor.z))))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(asuint((float3(_FresnelColor.w, _FresnelColor.w, _FresnelColor.w))))));
    r0.xyz = asuint((asfloat(r0.xyz) * asfloat(asuint((float3(_Fresnel, _Fresnel, _Fresnel))))));
    r2.xyz = asuint(_BaseMap.SampleBias(sampler_BaseMap, asfloat(r1.xy), asfloat(asuint((_GlobalMipBias)))).xyz);
    r0.w = asuint(_MaskTex.SampleBias(sampler_MaskTex, asfloat(r1.xy), asfloat(asuint((_GlobalMipBias)))).z);
    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_Emission)))));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(asuint((float3(_EmissionColor.x, _EmissionColor.y, _EmissionColor.z))))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(asuint((float3(_Color.x, _Color.y, _Color.z))))));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(v5.xyz)));
    r0.xyz = asuint(mad(asfloat(r1.xyz), asfloat(r2.xyz), asfloat(r0.xyz)));
    r0.w = asuint(dot(asfloat(r0.xyz), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.xyz = asuint((asfloat((r0.www ^ 0x80000000u)) + asfloat(r0.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.xyz), asfloat(r0.www)));
    r0.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r0.w = ((asfloat(0x3a83126fu) < asfloat((r0.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    o0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    o0.w = 0x3f800000u;
    r0.xy = asuint((asfloat(v1.xy) / asfloat(v1.ww)));
    r0.zw = asuint((asfloat(v0.zw) / asfloat(v1.zz)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.z = (r0.y ^ 0x80000000u);
    r1.xy = ((asfloat(uint2(0x00000000u, 0x00000000u)) < asfloat(r0.xz)) ? 0xffffffffu : 0u);
    r0.zw = ((asfloat(r0.xz) < asfloat(uint2(0x00000000u, 0x00000000u))) ? 0xffffffffu : 0u);
    r0.xy = asuint((asfloat(r0.xy) * asfloat(uint2(0x3f000000u, 0xbf000000u))));
    r0.xy = asuint(sqrt(asfloat((r0.xy & 0x7fffffffu))));
    r0.zw = ((0u - r1.xy) + r0.zw);
    r0.zw = asuint((float2)(asint(r0.zw)));
    r0.xy = asuint((asfloat(r0.zw) * asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(uint2(0x3f000000u, 0x3f000000u)), asfloat(uint2(0x3efefeffu, 0x3efefeffu))));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((unity_MotionVectorsParams.y)))) ? 0xffffffffu : 0u);
    r0.zw = uint2(0x00000000u, 0x00000000u);
    o1.xyzw = ((r1.xxxx != 0u) ? r0.xyzw : uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "GeometryPass"
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
    column_major float4x4 _PrevViewProjMatrix : packoffset(c110);
    column_major float4x4 _NonJitteredViewProjMatrix : packoffset(c118);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
    column_major float4x4 unity_WorldToObject : packoffset(c4);
    column_major float4x4 unity_MatrixPreviousM : packoffset(c20);
    float4 unity_WorldTransformParams : packoffset(c9);
    float4 unity_SHAr : packoffset(c13);
    float4 unity_SHAg : packoffset(c14);
    float4 unity_SHAb : packoffset(c15);
    float4 unity_SHBr : packoffset(c16);
    float4 unity_SHBg : packoffset(c17);
    float4 unity_SHBb : packoffset(c18);
    float4 unity_SHC : packoffset(c19);
}


struct CorinVertexOut {
    float4 output0 : TEXCOORD0;
    float4 output1 : TEXCOORD1;
    float4 output2 : TEXCOORD2;
    float4 output3 : TEXCOORD3;
    float4 output4 : TEXCOORD4;
    float4 output5 : TEXCOORD5;
    float3 output6 : TEXCOORD6;
    float2 output7 : TEXCOORD8;
    float4 output8 : SV_POSITION0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float4 input4 : TEXCOORD0, float2 input5 : TEXCOORD1)
{
    uint4 r0, r1, r2, r3, o0, o1, o2, o3, o4, o5, o6, o7, o8, v0, v1, v2, v3, v4, v5;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xy = asuint(input5);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_MatrixPreviousM[0][1], unity_MatrixPreviousM[1][1], unity_MatrixPreviousM[2][1], unity_MatrixPreviousM[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][0], unity_MatrixPreviousM[1][0], unity_MatrixPreviousM[2][0], unity_MatrixPreviousM[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][2], unity_MatrixPreviousM[1][2], unity_MatrixPreviousM[2][2], unity_MatrixPreviousM[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixPreviousM[0][3], unity_MatrixPreviousM[1][3], unity_MatrixPreviousM[2][3], unity_MatrixPreviousM[3][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(_PrevViewProjMatrix[0][1], _PrevViewProjMatrix[1][1], _PrevViewProjMatrix[3][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][0], _PrevViewProjMatrix[1][0], _PrevViewProjMatrix[3][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][2], _PrevViewProjMatrix[1][2], _PrevViewProjMatrix[3][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewProjMatrix[0][3], _PrevViewProjMatrix[1][3], _PrevViewProjMatrix[3][3])))), asfloat(r0.www), asfloat(r0.xyz)));
    o0.zw = r0.xy;
    o1.z = r0.z;
    o0.xy = v4.xy;
    r0.xyz = asuint((asfloat(v0.xxx) * asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]))))));
    r1.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(r1.xyz)));
    r1.xyz = asuint((asfloat(v0.zzz) * asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2]))))));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(r1.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][1], _NonJitteredViewProjMatrix[1][1], _NonJitteredViewProjMatrix[3][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][0], _NonJitteredViewProjMatrix[1][0], _NonJitteredViewProjMatrix[3][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][2], _NonJitteredViewProjMatrix[1][2], _NonJitteredViewProjMatrix[3][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    o1.xyw = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(_NonJitteredViewProjMatrix[0][3], _NonJitteredViewProjMatrix[1][3], _NonJitteredViewProjMatrix[3][3]))))));
    o2.w = r0.x;
    r1.x = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r1.y = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r1.z = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r0.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r1.xyz)));
    o2.xyz = r1.xyz;
    r2.xyz = asuint((asfloat(v2.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v2.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v2.zzz), asfloat(r2.xyz)));
    r0.w = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    r2.xyz = asuint((asfloat(r0.www) * asfloat(r2.xyz)));
    o3.xyz = r2.xyz;
    o3.w = r0.y;
    r3.xyz = asuint((asfloat(r1.zxy) * asfloat(r2.yzx)));
    r2.xyz = asuint(mad(asfloat(r1.yzx), asfloat(r2.zxy), asfloat((r3.xyz ^ 0x80000000u))));
    r0.w = asuint((asfloat(v2.w) * asfloat(asuint((unity_WorldTransformParams.w)))));
    o4.xyz = asuint((asfloat(r0.www) * asfloat(r2.xyz)));
    o4.w = r0.z;
    o5.xyzw = v3.xyzw;
    r0.w = asuint((asfloat(r1.y) * asfloat(r1.y)));
    r0.w = asuint(mad(asfloat(r1.x), asfloat(r1.x), asfloat((r0.w ^ 0x80000000u))));
    r2.xyzw = asuint((asfloat(r1.yzzx) * asfloat(r1.xyzz)));
    r3.x = asuint(dot(asfloat(asuint((float4(unity_SHBr.x, unity_SHBr.y, unity_SHBr.z, unity_SHBr.w)))), asfloat(r2.xyzw)));
    r3.y = asuint(dot(asfloat(asuint((float4(unity_SHBg.x, unity_SHBg.y, unity_SHBg.z, unity_SHBg.w)))), asfloat(r2.xyzw)));
    r3.z = asuint(dot(asfloat(asuint((float4(unity_SHBb.x, unity_SHBb.y, unity_SHBb.z, unity_SHBb.w)))), asfloat(r2.xyzw)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_SHC.x, unity_SHC.y, unity_SHC.z)))), asfloat(r0.www), asfloat(r3.xyz)));
    r1.w = 0x3f800000u;
    r3.x = asuint(dot(asfloat(asuint((float4(unity_SHAr.x, unity_SHAr.y, unity_SHAr.z, unity_SHAr.w)))), asfloat(r1.xyzw)));
    r3.y = asuint(dot(asfloat(asuint((float4(unity_SHAg.x, unity_SHAg.y, unity_SHAg.z, unity_SHAg.w)))), asfloat(r1.xyzw)));
    r3.z = asuint(dot(asfloat(asuint((float4(unity_SHAb.x, unity_SHAb.y, unity_SHAb.z, unity_SHAb.w)))), asfloat(r1.xyzw)));
    r1.xyz = asuint((asfloat(r2.xyz) + asfloat(r3.xyz)));
    o6.xyz = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    o7.xy = v5.xy;
    r1.xyzw = asuint((asfloat(r0.xxxx) * asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0]))))));
    r2.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r0.xyzw = asuint((asfloat(r0.zzzz) * asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2]))))));
    r1.xyzw = asuint((asfloat(r1.xyzw) + asfloat(r2.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(r1.xyzw)));
    o8.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyz);
    result.output7 = asfloat(o7.xy);
    result.output8 = asfloat(o8.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
cbuffer ZZZGlobals : register(b0)
{
    column_major float4x4 _SceneWeatherParamsPart3 : packoffset(c39);
    column_major float4x4 _SceneWeatherParamsPart6 : packoffset(c51);
    float _GlobalMipBias : packoffset(c61);
}

cbuffer UnityPerDraw : register(b1)
{
    float4 unity_SHAr : packoffset(c13);
    float4 unity_SHAg : packoffset(c14);
    float4 unity_SHAb : packoffset(c15);
    float4 unity_SHBr : packoffset(c16);
    float4 unity_SHBg : packoffset(c17);
    float4 unity_SHBb : packoffset(c18);
    float4 unity_SHC : packoffset(c19);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _BaseMap_ST : packoffset(c1);
    float4 _Color : packoffset(c3);
    float _Metallic : packoffset(c6);
    float _Glossiness : packoffset(c6.y);
    float _Emission : packoffset(c6.z);
    float _EmissionOverAlbedo : packoffset(c6.w);
}



SamplerState sampler_BaseMap;
SamplerState sampler_BumpMap;
SamplerState sampler_MaskTex;
Texture2D<float4> _BaseMap : register(t0);
Texture2D<float4> _BumpMap : register(t1);
Texture2D<float4> _MaskTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
    float4 output1 : SV_Target1;
    float4 output2 : SV_Target2;
    float4 output3 : SV_Target3;
};

CorinFragmentOut CorinPixel(float4 input0 : TEXCOORD0, float4 input1 : TEXCOORD1, float4 input2 : TEXCOORD2, float4 input3 : TEXCOORD3, float4 input4 : TEXCOORD4, float4 input5 : TEXCOORD5, float3 input6 : TEXCOORD6, float2 input7 : TEXCOORD8, float4 input8 : SV_POSITION0, bool input9 : SV_IsFrontFace0)
{
    uint4 r0, r1, r2, r3, r4, r5, o0, o1, o2, o3, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyz = asuint(input6);
    v7.xy = asuint(input7);
    v8.xyzw = asuint(input8);
    v9.x = (input9 ? 0xffffffffu : 0u);
    r0.xy = asuint(mad(asfloat(v0.xy), asfloat(asuint((float2(_BaseMap_ST.x, _BaseMap_ST.y)))), asfloat(asuint((float2(_BaseMap_ST.z, _BaseMap_ST.w))))));
    r1.xyz = asuint(_MaskTex.SampleBias(sampler_MaskTex, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyz);
    r0.z = asuint((asfloat((r1.y ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.z = asuint(mad(asfloat((r0.z ^ 0x80000000u)), asfloat(asuint((_Glossiness))), asfloat(0x3f800000u)));
    r0.w = asuint((asfloat(r1.x) * asfloat(asuint((_Metallic)))));
    r1.xyw = asuint(_BaseMap.SampleBias(sampler_BaseMap, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyz);
    r1.xyw = asuint((asfloat(r1.xyw) * asfloat(asuint((float3(_Color.x, _Color.y, _Color.z))))));
    r1.xyw = asuint((asfloat(r1.xyw) * asfloat(v5.xyz)));
    r2.xyz = asuint(_BumpMap.SampleBias(sampler_BumpMap, asfloat(r0.xy), asfloat(asuint((_GlobalMipBias)))).xyw);
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.z)));
    r0.xy = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x40000000u, 0x40000000u)), asfloat(uint2(0xbf800000u, 0xbf800000u))));
    r2.x = asuint(dot(asfloat(r0.xy), asfloat(r0.xy)));
    r2.x = asuint(min(asfloat(r2.x), asfloat(0x3f800000u)));
    r2.x = asuint((asfloat((r2.x ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.x = asuint(sqrt(asfloat(r2.x)));
    r2.x = ((v9.x != 0u) ? r2.x : (r2.x ^ 0x80000000u));
    r2.yzw = asuint((asfloat(r0.yyy) * asfloat(v4.xyz)));
    r2.yzw = asuint(mad(asfloat(r0.xxx), asfloat(v3.xyz), asfloat(r2.yzw)));
    r2.xyz = asuint(mad(asfloat(r2.xxx), asfloat(v2.xyz), asfloat(r2.yzw)));
    r0.x = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r0.x = asuint(rsqrt(asfloat(r0.x)));
    r2.xyz = asuint((asfloat(r0.xxx) * asfloat(r2.xyz)));
    r0.x = asuint(mad(asfloat((r1.z ^ 0x80000000u)), asfloat(asuint((_Emission))), asfloat(0x3f800000u)));
    r0.y = asuint((asfloat((r0.x ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.x = asuint(mad(asfloat(asuint((_EmissionOverAlbedo))), asfloat(r0.y), asfloat(r0.x)));
    o1.xyz = asuint((asfloat(r0.xxx) * asfloat(r1.xyw)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_SceneWeatherParamsPart3[2][1])))) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        r1.xyz = uint3(0x00000000u, 0x447a0000u, 0x3f800000u);
    } else {
        r0.x = ((asfloat(asuint((_SceneWeatherParamsPart6[1][3]))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
        if (r0.x != 0u) {
            r2.w = 0x3f800000u;
            r3.x = asuint(dot(asfloat(asuint((float4(unity_SHAr.x, unity_SHAr.y, unity_SHAr.z, unity_SHAr.w)))), asfloat(r2.xyzw)));
            r3.y = asuint(dot(asfloat(asuint((float4(unity_SHAg.x, unity_SHAg.y, unity_SHAg.z, unity_SHAg.w)))), asfloat(r2.xyzw)));
            r3.z = asuint(dot(asfloat(asuint((float4(unity_SHAb.x, unity_SHAb.y, unity_SHAb.z, unity_SHAb.w)))), asfloat(r2.xyzw)));
            r4.xyzw = asuint((asfloat(r2.yzzx) * asfloat(r2.xyzz)));
            r5.x = asuint(dot(asfloat(asuint((float4(unity_SHBr.x, unity_SHBr.y, unity_SHBr.z, unity_SHBr.w)))), asfloat(r4.xyzw)));
            r5.y = asuint(dot(asfloat(asuint((float4(unity_SHBg.x, unity_SHBg.y, unity_SHBg.z, unity_SHBg.w)))), asfloat(r4.xyzw)));
            r5.z = asuint(dot(asfloat(asuint((float4(unity_SHBb.x, unity_SHBb.y, unity_SHBb.z, unity_SHBb.w)))), asfloat(r4.xyzw)));
            r0.x = asuint((asfloat(r2.y) * asfloat(r2.y)));
            r0.x = asuint(mad(asfloat(r2.x), asfloat(r2.x), asfloat((r0.x ^ 0x80000000u))));
            r4.xyz = asuint(mad(asfloat(asuint((float3(unity_SHC.x, unity_SHC.y, unity_SHC.z)))), asfloat(r0.xxx), asfloat(r5.xyz)));
            r3.xyz = asuint((asfloat(r3.xyz) + asfloat(r4.xyz)));
            r1.xyz = asuint(max(asfloat(r3.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
        } else {
            r1.xyz = uint3(0x00000000u, 0x00000000u, 0x00000000u);
        }
    }
    o2.y = asuint((asfloat((r0.z ^ 0x80000000u)) + asfloat(0x3f800000u)));
    o3.xyz = asuint(mad(asfloat(r2.xyz), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u)), asfloat(uint3(0x3f000000u, 0x3f000000u, 0x3f000000u))));
    o0.xyz = r1.xyz;
    o0.w = 0x3f800000u;
    o1.w = 0x00000000u;
    o2.xw = uint2(0x00000000u, 0x3f2b851fu);
    o2.z = r0.w;
    o3.w = 0x00000000u;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
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
    float _ShadowPancaking : packoffset(c27);
    float4 _ShadowBias : packoffset(c163);
    float3 _LightDirection : packoffset(c168);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}


struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0)
{
    uint4 r0, r1, o0, v0;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_LightDirection.x, _LightDirection.y, _LightDirection.z)))), asfloat(asuint((float3(_ShadowBias.x, _ShadowBias.x, _ShadowBias.x)))), asfloat(r0.xyz)));
    r0.w = ((asfloat(asuint((_ShadowPancaking))) == asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
    r0.xyz = ((r0.www != 0u) ? r1.xyz : r0.xyz);
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    o0.z = asuint(min(asfloat(r0.w), asfloat(r0.z)));
    o0.xyw = r0.xyw;
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #elif defined(SHADER_STAGE_FRAGMENT)
struct CorinFragmentOut {
    float4 output0 : SV_TARGET0;
};

CorinFragmentOut CorinPixel()
{
    uint4 o0;
    CorinFragmentOut result;
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.output0 = asfloat(o0.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
