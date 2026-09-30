Shader "miHoYo/Particles/Particles_Explosion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTex1_ST ("Inner Tone Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _MainTexClampU ("Clamp U", Float) = 0.0
        _MainTexClampV ("Clamp V", Float) = 0.0
        _MainTexFlip ("Flip", Float) = 0.0
        _MainTexRotation ("Rotation", Float) = 0.0
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTexChannelMapping ("Color Channel Mapping", Float) = 0.0
        _DissolveAlphaMapping ("Dissolve Alpha Channel", Float) = 0.0
        _MainTexUVMove ("UV Move", Float) = 0.0
        _MainTexUVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MainTex0SpherizeUV ("Spherize UV", Float) = 0.0
        _MainTex0SphereScale ("Sphere Scale", Float) = 10.0
        _MainTex0SpherizeSmoothStep ("Spherize Smooth Step", Vector) = (-1.0, 0.0, 0.0, 0.0)
        _InnerColor ("Inner Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex1ChannelMapping ("Color Channel Mapping", Float) = 0.0
        _DissolveInnerAlphaMapping ("Dissolve Alpha Channel", Float) = 0.0
        _MainTex1UVMove ("UV Move", Float) = 0.0
        _MainTex1UVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _UseCustomColor ("Use Custom2.rgb as Inner Tone Color", Float) = 1.0
        _MainTex1SpherizeUV ("Spherize UV", Float) = 0.0
        _MainTex1SphereScale ("Sphere Scale", Float) = 10.0
        _MainTex1SpherizeSmoothStep ("Spherize Smooth Step", Vector) = (-1.0, 0.0, 0.0, 0.0)
        _DissolveTex ("Dissolve Tex", 2D) = "white" {}
        _DissolveTex1_ST ("Dissolve Inner Tilling Offset", Vector) = (1.0, 1.0, 0.0, 0.0)
        _DissolveTexClampU ("Clamp U", Float) = 0.0
        _DissolveTexClampV ("Clamp V", Float) = 0.0
        _DissolveTexFlip ("Flip", Float) = 0.0
        _DissolveTexRotation ("Rotation", Float) = 0.0
        _DissolveChannelMapping ("Main Dissolve Channel", Float) = 0.0
        _DissolveUVMove ("UV Move", Float) = 0.0
        _DissolveUVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DissolveRandomUV ("Random UV", Float) = 0.0
        _UseCustomCurve1X ("Use Custom1.x as Dissolve Progress", Float) = 0.0
        _DissolveProgress ("Dissolve Progress", Float) = 0.5
        _SoftRange ("Soft Range", Float) = 0.5
        _DissolveTex0SpherizeUV ("Spherize UV", Float) = 0.0
        _DissolveTex0SphereScale ("Sphere Scale", Float) = 10.0
        _DissolveTex0SpherizeSmoothStep ("Spherize Smooth Step", Vector) = (-1.0, 0.0, 0.0, 0.0)
        _DissolveChannelMapping1 ("Inner Dissolve Channel", Float) = 0.0
        _Dissolve1UVMove ("UV Move", Float) = 0.0
        _Dissolve1UVSpeed ("UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _Dissolve1RandomUV ("Random UV", Float) = 0.0
        _UseCustomCurve1X1Y ("Use Custom1.y as Dissolve Progress", Float) = 0.0
        _DissolveProgress1 ("Dissolve Progress", Float) = 0.5
        _SoftRange1 ("Soft Range", Float) = 0.5
        _DissolveTex1SpherizeUV ("Spherize UV", Float) = 0.0
        _DissolveTex1SphereScale ("Sphere Scale", Float) = 10.0
        _DissolveTex1SpherizeSmoothStep ("Spherize Smooth Step", Vector) = (-1.0, 0.0, 0.0, 0.0)
        _DistortionTex ("Distortion Tex", 2D) = "white" {}
        _DistortionTexClampU ("Clamp U", Float) = 0.0
        _DistortionTexClampV ("Clamp V", Float) = 0.0
        _DistortionTexFlip ("Flip", Float) = 0.0
        _DistortionTexRotation ("Rotation", Float) = 0.0
        _DistortionTexSpherizeUV ("Spherize UV", Float) = 0.0
        _DistortionTexSphereScale ("Sphere Scale", Float) = 10.0
        _DistortionTexSpherizeSmoothStep ("Spherize Smooth Step", Vector) = (-1.0, 0.0, 0.0, 0.0)
        _DistortionUVMove ("Dissolve UV Move", Float) = 0.0
        _DistortionUVSpeed ("Dissolve UV Speed", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DistortionIntensity ("Distortion Intensity", Float) = 1.0
        _DistortionAffectsMainTone ("Distortion Affects Main Tone", Float) = 1.0
        _DistortionAffectsInnerTone ("Distortion Affects Inner Tone", Float) = 1.0
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.0
        _SoftParticles ("Soft Particles@软粒子", Float) = 0.0
        _SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
        _SoftParticlesRcpDistance ("Soft Particles Rcp Distance", Float) = 1.0
        _OpaquenessFadeByScript ("Opaqueness Fade By Script", Float) = 1.0
        _group_dither ("Dither Clip@点状透明", Float) = 0.0
        _DitherAlpha ("Dither Alpha@点状透明", Float) = 1.0
        _DitherAlpha2 ("Dither Alpha 2@点状透明2", Float) = 1.0
        _FadeFromCameraOn ("按照离相机距离进行局部dither/Fade", Float) = 0.0
        _FadeFromCamera_Invert ("Invert Alpha@反转Fade透明", Float) = 0.0
        _FadeFromCameraType ("   Fade Type@模式", Float) = 0.0
        _FadeFromCamera ("   Fade消失距离(离相机)", Float) = 1.0
        _RcpFadeLength ("   Fade过渡距离软硬参数", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.0
        _DistortionMode ("Distortion Mode", Float) = 0.0
        _DTTex ("Distortion Tex", 2D) = "linearGray" {}
        _DTTexClampU ("Clamp U", Float) = 0.0
        _DTTexClampV ("Clamp V", Float) = 0.0
        _DTTexFlip ("Flip", Float) = 0.0
        _DTTexRotation ("Rotation", Float) = 0.0
        _DTIntensity ("Distortion Intensity", Float) = 0.0
        _Dist_Intensity_PostProcessing ("Distortion Intensity Post Processing", Float) = 1.0
        _DtUvMove ("Distortion UV Move", Float) = 0.0
        _DtUSpeed ("Distortion U Speed", Float) = 1.0
        _DtVSpeed ("Distortion V Speed", Float) = 1.0
        _SeparateRGBIntensity ("Separate RGB Intensity", Float) = 0.0
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
        _TimeOffset ("TimeOffset", Float) = 0.0
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
    float4 _MainTex1_ST : packoffset(c1);
    float4 _DissolveTex_ST : packoffset(c2);
    float4 _DissolveTex1_ST : packoffset(c3);
    float4 _DistortionTex_ST : packoffset(c4);
    float2 _MainTexUVSpeed : packoffset(c9);
    float2 _MainTex1UVSpeed : packoffset(c9.z);
    float2 _DissolveUVSpeed : packoffset(c10);
    float2 _Dissolve1UVSpeed : packoffset(c10.z);
    float2 _DistortionUVSpeed : packoffset(c11);
    float _MainTexUVMove : packoffset(c15.z);
    float _MainTex1UVMove : packoffset(c15.w);
    float _DissolveUVMove : packoffset(c16);
    float _Dissolve1UVMove : packoffset(c16.y);
    float _DissolveRandomUV : packoffset(c16.z);
    float _Dissolve1RandomUV : packoffset(c16.w);
    float _DistortionUVMove : packoffset(c17);
    float _MainTexFlip : packoffset(c20.y);
    float _MainTexRotation : packoffset(c20.z);
    float _DissolveTexFlip : packoffset(c22.y);
    float _DissolveTexRotation : packoffset(c22.z);
    float _DistortionTexFlip : packoffset(c24.y);
    float _DistortionTexRotation : packoffset(c24.z);
    float _ZOffset : packoffset(c28.z);
    float _IgnoreTimeScale : packoffset(c28.w);
    float _TimeOffset : packoffset(c29.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float3 output5 : TEXCOORD4;
    float3 output6 : TEXCOORD6;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float3 input3 : TEXCOORD2, float4 input4 : COLOR0)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, o2, o3, o4, o5, o6, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyz = asuint(input3);
    v4.xyzw = asuint(input4);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    o6.xyz = r0.xyz;
    r0.xyz = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
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
    r3.xyzw = asuint(mad(asfloat(v1.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v1.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
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
    r1.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex1_ST.x, _MainTex1_ST.y)))), asfloat(asuint((float2(_MainTex1_ST.z, _MainTex1_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r0.zw) + asfloat(r1.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r1.x = ((r1.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r2.xyzw = asuint((asfloat(r1.xxxx) * asfloat(asuint((float4(_MainTexUVMove, _MainTexUVMove, _MainTex1UVMove, _MainTex1UVMove))))));
    o1.xy = asuint(mad(asfloat(r2.xy), asfloat(asuint((float2(_MainTexUVSpeed.x, _MainTexUVSpeed.y)))), asfloat(r0.zw)));
    o1.zw = asuint(mad(asfloat(r2.zw), asfloat(asuint((float2(_MainTex1UVSpeed.x, _MainTex1UVSpeed.y)))), asfloat(r0.xy)));
    r0.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.yzw = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(r5.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r6.xyzw = asuint(mad(asfloat(asuint((float4(_DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r6.xw;
    r2.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.xy = r6.zw;
    r0.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r4.xyzw)));
    r6.xz = r0.xz;
    r6.w = asuint((_DissolveTexRotation));
    r0.xyzw = asuint(mad(asfloat(r6.xyzw), asfloat(r1.yyyy), asfloat(r4.xyzw)));
    r2.yw = r6.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r1.yz = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex_ST.x, _DissolveTex_ST.y)))), asfloat(asuint((float2(_DissolveTex_ST.z, _DissolveTex_ST.w))))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex1_ST.x, _DissolveTex1_ST.y)))), asfloat(asuint((float2(_DissolveTex1_ST.z, _DissolveTex1_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r0.zw) + asfloat(r1.yz)));
    r2.xyzw = asuint((asfloat(r1.xxxx) * asfloat(asuint((float4(_DissolveUVMove, _DissolveUVMove, _Dissolve1UVMove, _Dissolve1UVMove))))));
    r1.x = asuint((asfloat(r1.x) * asfloat(asuint((_DistortionUVMove)))));
    r2.xyzw = asuint((asfloat(r2.xyzw) * asfloat(asuint((float4(_DissolveUVSpeed.x, _DissolveUVSpeed.y, _Dissolve1UVSpeed.x, _Dissolve1UVSpeed.y))))));
    r1.yz = asuint(mad(asfloat(asuint((float2(_DissolveRandomUV, _DissolveRandomUV)))), asfloat(v1.zw), asfloat(r2.xy)));
    r2.xy = asuint(mad(asfloat(asuint((float2(_Dissolve1RandomUV, _Dissolve1RandomUV)))), asfloat(v2.xy), asfloat(r2.zw)));
    o2.zw = asuint((asfloat(r0.xy) + asfloat(r2.xy)));
    o2.xy = asuint((asfloat(r0.zw) + asfloat(r1.yz)));
    r0.xy = r3.zw;
    r0.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.xy = r3.xw;
    r2.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.xyzw = ((asfloat(asuint((float4(_DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.yzw = ((r4.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r4.xyzw = (r4.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(r4.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r2.z = 0x00000000u;
    r5.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.z = 0x3f800000u;
    r5.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r5.xyzw)));
    r3.xz = r0.xz;
    r3.w = asuint((_DistortionTexRotation));
    r0.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r1.yyyy), asfloat(r5.xyzw)));
    r2.yw = r3.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r4.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DistortionTex_ST.x, _DistortionTex_ST.y)))), asfloat(asuint((float2(_DistortionTex_ST.z, _DistortionTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    o3.xy = asuint(mad(asfloat(r1.xx), asfloat(asuint((float2(_DistortionUVSpeed.x, _DistortionUVSpeed.y)))), asfloat(r0.xy)));
    o3.zw = v2.zw;
    o4.xyzw = v4.xyzw;
    o5.xyz = v3.xyz;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyz);
    result.output6 = asfloat(o6.xyz);
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
    float3 _Color : packoffset(c7);
    float3 _InnerColor : packoffset(c8);
    float2 _MainTex0SpherizeSmoothStep : packoffset(c11.z);
    float2 _MainTex1SpherizeSmoothStep : packoffset(c12);
    float2 _DissolveTex0SpherizeSmoothStep : packoffset(c12.z);
    float2 _DissolveTex1SpherizeSmoothStep : packoffset(c13);
    float2 _DistortionTexSpherizeSmoothStep : packoffset(c13.z);
    float _MainTexChannelMapping : packoffset(c14);
    float _DissolveAlphaMapping : packoffset(c14.y);
    float _MainTex1ChannelMapping : packoffset(c14.z);
    float _DissolveInnerAlphaMapping : packoffset(c14.w);
    float _DissolveChannelMapping : packoffset(c15);
    float _DissolveChannelMapping1 : packoffset(c15.y);
    float _DistortionIntensity : packoffset(c17.y);
    float _DistortionAffectsMainTone : packoffset(c17.z);
    float _DistortionAffectsInnerTone : packoffset(c17.w);
    float _DissolveProgress : packoffset(c18);
    float _DissolveProgress1 : packoffset(c18.y);
    float _UseCustomCurve1X : packoffset(c18.z);
    float _UseCustomCurve1X1Y : packoffset(c18.w);
    float _SoftRange : packoffset(c19);
    float _SoftRange1 : packoffset(c19.y);
    float _UseCustomColor : packoffset(c19.z);
    float _MainTexClampU : packoffset(c19.w);
    float _MainTexClampV : packoffset(c20);
    float _MainTex0SpherizeUV : packoffset(c20.w);
    float _MainTex0SphereScale : packoffset(c21);
    float _MainTex1SpherizeUV : packoffset(c21.y);
    float _MainTex1SphereScale : packoffset(c21.z);
    float _DissolveTexClampU : packoffset(c21.w);
    float _DissolveTexClampV : packoffset(c22);
    float _DissolveTex0SpherizeUV : packoffset(c22.w);
    float _DissolveTex0SphereScale : packoffset(c23);
    float _DissolveTex1SpherizeUV : packoffset(c23.y);
    float _DissolveTex1SphereScale : packoffset(c23.z);
    float _DistortionTexClampU : packoffset(c23.w);
    float _DistortionTexClampV : packoffset(c24);
    float _DistortionTexSpherizeUV : packoffset(c24.w);
    float _DistortionTexSphereScale : packoffset(c25);
    float _OpaquenessFadeByScript : packoffset(c29);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
SamplerState sampler_DissolveTex;
SamplerState sampler_DistortionTex;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _DissolveTex : register(t1);
Texture2D<float4> _DistortionTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float3 input5 : TEXCOORD4, float3 input6 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, v0, v1, v2, v3, v4, v5, v6;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyz = asuint(input5);
    v6.xyz = asuint(input6);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveTexClampU)))) ? 0xffffffffu : 0u);
    r1.xyzw = asuint(max(asfloat(v2.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyzw = asuint(min(asfloat(r1.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r0.xy = ((r0.xx != 0u) ? r1.xz : v2.xz);
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveTexClampV)))) ? 0xffffffffu : 0u);
    r0.zw = ((r1.xx != 0u) ? r1.yw : v2.yw);
    r1.xyzw = asuint((asfloat(r0.xzyw) + asfloat(uint4(0xbf000000u, 0xbf000000u, 0xbf000000u, 0xbf000000u))));
    r2.x = asuint(dot(asfloat(r1.zw), asfloat(r1.zw)));
    r2.xy = asuint(mad(asfloat((r2.xx ^ 0x80000000u)), asfloat(asuint((float2(_DissolveTex1SphereScale, _DissolveTex1SphereScale)))), asfloat(asuint((float2(_DissolveTex1SpherizeSmoothStep.x, _DissolveTex1SpherizeSmoothStep.y))))));
    r2.x = asuint((asfloat(0x3f800000u) / asfloat(r2.x)));
    r2.x = asuint(saturate((asfloat(r2.x) * asfloat(r2.y))));
    r2.y = asuint(mad(asfloat(r2.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.x)));
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.y)));
    r1.zw = asuint(mad(asfloat(r1.zw), asfloat(r2.xx), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r2.x = ((asfloat(asuint((_DissolveTex1SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.yw = ((r2.xx != 0u) ? r0.yw : r1.zw);
    r2.xyzw = asuint(_DissolveTex.Sample(sampler_DissolveTex, asfloat(r0.yw)).xyzw);
    r0.yw = (uint2)(asfloat(asuint((float2(_DissolveChannelMapping, _DissolveChannelMapping1)))));
    r0.w = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r0.w+0].xyzw)));
    r1.zw = asuint(mad(asfloat(asuint((float2(_SoftRange, _SoftRange1)))), asfloat(uint2(0x3efae148u, 0x3efae148u)), asfloat(uint2(0x3c23d70au, 0x3c23d70au))));
    r1.w = asuint((asfloat(r0.w) + asfloat((r1.w ^ 0x80000000u))));
    r0.w = asuint((asfloat(r0.w) + asfloat((r1.w ^ 0x80000000u))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r2.xy = asuint((asfloat(v3.zw) + asfloat((asuint((float2(_DissolveProgress, _DissolveProgress1))) ^ 0x80000000u))));
    r2.xy = asuint(mad(asfloat(asuint((float2(_UseCustomCurve1X, _UseCustomCurve1X1Y)))), asfloat(r2.xy), asfloat(asuint((float2(_DissolveProgress, _DissolveProgress1))))));
    r2.xy = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x3fc00000u, 0x3fc00000u)), asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r1.w = asuint((asfloat((r1.w ^ 0x80000000u)) + asfloat(r2.y)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(r1.w))));
    r1.w = asuint(mad(asfloat(r0.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionTexClampU)))) ? 0xffffffffu : 0u);
    r2.yz = asuint(max(asfloat(v3.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r2.yz = asuint(min(asfloat(r2.yz), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r3.x = ((r1.w != 0u) ? r2.y : v3.x);
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionTexClampV)))) ? 0xffffffffu : 0u);
    r3.y = ((r1.w != 0u) ? r2.z : v3.y);
    r2.yz = asuint((asfloat(r3.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r1.w = asuint(dot(asfloat(r2.yz), asfloat(r2.yz)));
    r3.zw = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_DistortionTexSphereScale, _DistortionTexSphereScale)))), asfloat(asuint((float2(_DistortionTexSpherizeSmoothStep.x, _DistortionTexSpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r3.z)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r3.w))));
    r2.w = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.w)));
    r2.yz = asuint(mad(asfloat(r2.yz), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_DistortionTexSpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yz = ((r1.ww != 0u) ? r3.xy : r2.yz);
    r3.xyzw = asuint(_DistortionTex.Sample(sampler_DistortionTex, asfloat(r2.yz)).xyzw);
    r3.xyzw = asuint(mad(asfloat(r3.xyxy), asfloat(uint4(0x40000000u, 0x40000000u, 0x40000000u, 0x40000000u)), asfloat(uint4(0xbf800000u, 0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) * asfloat(asuint((float4(_DistortionIntensity, _DistortionIntensity, _DistortionIntensity, _DistortionIntensity))))));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampU)))) ? 0xffffffffu : 0u);
    r4.xyzw = asuint(max(asfloat(v1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r4.xyzw = asuint(min(asfloat(r4.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r5.xy = ((r1.ww != 0u) ? r4.xz : v1.xz);
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampV)))) ? 0xffffffffu : 0u);
    r5.zw = ((r1.ww != 0u) ? r4.yw : v1.yw);
    r4.xyzw = asuint((asfloat(r5.xzyw) + asfloat(uint4(0xbf000000u, 0xbf000000u, 0xbf000000u, 0xbf000000u))));
    r1.w = asuint(dot(asfloat(r4.zw), asfloat(r4.zw)));
    r2.yz = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_MainTex1SphereScale, _MainTex1SphereScale)))), asfloat(asuint((float2(_MainTex1SpherizeSmoothStep.x, _MainTex1SpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r2.z))));
    r2.y = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.y)));
    r2.yz = asuint(mad(asfloat(r4.zw), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_MainTex1SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yz = ((r1.ww != 0u) ? r5.yw : r2.yz);
    r2.yz = asuint(mad(asfloat(r3.zw), asfloat(asuint((float2(_DistortionAffectsInnerTone, _DistortionAffectsInnerTone)))), asfloat(r2.yz)));
    r6.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r2.yz)).xyzw);
    r7.xyzw = (uint4)(asfloat(asuint((float4(_MainTexChannelMapping, _DissolveAlphaMapping, _MainTex1ChannelMapping, _DissolveInnerAlphaMapping)))));
    r1.w = asuint(dot(asfloat(r6.xyzw), asfloat(icb[r7.w+0].xyzw)));
    r2.y = asuint(dot(asfloat(r6.xyzw), asfloat(icb[r7.z+0].xyzw)));
    r6.xyz = asuint((asfloat((r2.yyy ^ 0x80000000u)) + asfloat(r6.xyz)));
    r1.w = asuint((asfloat(r1.w) + asfloat((r2.y ^ 0x80000000u))));
    r2.zw = ((asfloat(asuint((float2(_MainTexChannelMapping, _MainTex1ChannelMapping)))) >= asfloat(uint2(0x40600000u, 0x40600000u))) ? 0xffffffffu : 0u);
    r2.zw = (r2.zw & uint2(0x3f800000u, 0x3f800000u));
    r1.w = asuint(mad(asfloat(r2.w), asfloat(r1.w), asfloat(r2.y)));
    r6.xyz = asuint(mad(asfloat(r2.www), asfloat(r6.xyz), asfloat(r2.yyy)));
    r6.xyz = asuint((asfloat(r6.xyz) * asfloat(asuint((float3(_InnerColor.x, _InnerColor.y, _InnerColor.z))))));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(v4.w)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(0x40400000u))));
    r8.xyz = asuint((asfloat(v5.xyz) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r8.xyz = asuint(mad(asfloat(asuint((float3(_UseCustomColor, _UseCustomColor, _UseCustomColor)))), asfloat(r8.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r6.xyz = asuint((asfloat(r6.xyz) * asfloat(r8.xyz)));
    r6.xyz = asuint((asfloat(r0.www) * asfloat(r6.xyz)));
    r0.w = asuint(dot(asfloat(r4.xy), asfloat(r4.xy)));
    r2.yw = asuint(mad(asfloat((r0.ww ^ 0x80000000u)), asfloat(asuint((float2(_MainTex0SphereScale, _MainTex0SphereScale)))), asfloat(asuint((float2(_MainTex0SpherizeSmoothStep.x, _MainTex0SpherizeSmoothStep.y))))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(r2.w))));
    r1.w = asuint(mad(asfloat(r0.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r2.yw = asuint(mad(asfloat(r4.xy), asfloat(r0.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r0.w = ((asfloat(asuint((_MainTex0SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yw = ((r0.ww != 0u) ? r5.xz : r2.yw);
    r2.yw = asuint(mad(asfloat(r3.xy), asfloat(asuint((float2(_DistortionAffectsMainTone, _DistortionAffectsMainTone)))), asfloat(r2.yw)));
    r3.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r2.yw)).xyzw);
    r0.w = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r7.x+0].xyzw)));
    r1.w = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r7.y+0].xyzw)));
    r3.xyz = asuint((asfloat((r0.www ^ 0x80000000u)) + asfloat(r3.xyz)));
    r3.xyz = asuint(mad(asfloat(r2.zzz), asfloat(r3.xyz), asfloat(r0.www)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint((float3(_Color.x, _Color.y, _Color.z)))), asfloat(r6.xyz)));
    r3.xyz = asuint((asfloat(r3.xyz) * asfloat(v4.xyz)));
    r1.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r1.w)));
    r0.w = asuint(mad(asfloat(r2.z), asfloat(r1.w), asfloat(r0.w)));
    r1.w = asuint(dot(asfloat(r1.xy), asfloat(r1.xy)));
    r2.yz = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_DissolveTex0SphereScale, _DissolveTex0SphereScale)))), asfloat(asuint((float2(_DissolveTex0SpherizeSmoothStep.x, _DissolveTex0SpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r2.z))));
    r2.y = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.y)));
    r1.xy = asuint(mad(asfloat(r1.xy), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_DissolveTex0SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xz = ((r1.ww != 0u) ? r0.xz : r1.xy);
    r4.xyzw = asuint(_DissolveTex.Sample(sampler_DissolveTex, asfloat(r0.xz)).xyzw);
    r0.x = asuint(dot(asfloat(r4.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.y = asuint((asfloat((r1.z ^ 0x80000000u)) + asfloat(r0.x)));
    r0.x = asuint((asfloat((r0.y ^ 0x80000000u)) + asfloat(r0.x)));
    r0.y = asuint((asfloat((r0.y ^ 0x80000000u)) + asfloat(r2.x)));
    r0.x = asuint((asfloat(0x3f800000u) / asfloat(r0.x)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(r0.y))));
    r0.y = asuint(mad(asfloat(r0.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.y)));
    r0.x = asuint((asfloat(r0.w) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(v4.w)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(0x40400000u))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r0.yzw = asuint((asfloat(r0.xxx) * asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r0.yzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r1.w = asuint(max(asfloat(r0.x), asfloat(0x3a83126fu)));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r1.xyz = asuint(mad(asfloat(r1.xyz), asfloat(r1.www), asfloat((r3.xyz ^ 0x80000000u))));
    r2.xyz = ((asfloat(r0.yzw) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r2.xyz = (r2.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.xyz), asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r3.xyz ^ 0x80000000u))));
    r0.y = asuint(max(asfloat(r0.z), asfloat(r0.y)));
    r0.y = asuint(max(asfloat(r0.w), asfloat(r0.y)));
    r0.y = asuint((asfloat(r0.y) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.y = asuint(saturate((asfloat(r0.y) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.yzw = asuint(mad(asfloat(r0.yyy), asfloat(r1.xyz), asfloat(r3.xyz)));
    r1.x = asuint(dot(asfloat(r0.yzw), asfloat(uint3(0x3e59c6edu, 0x3f371437u, 0x3d93d07du))));
    r1.yzw = asuint((asfloat(r0.yzw) + asfloat((r1.xxx ^ 0x80000000u))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2], _SceneWeatherParamsPart1[2][2])))), asfloat(r1.yzw), asfloat(r1.xxx)));
    r1.w = asuint((asfloat(asuint((_SceneWeatherParamsPart1[2][2]))) + asfloat(0xbf800000u)));
    r1.w = ((asfloat(0x3a83126fu) < asfloat((r1.w & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r0.yzw = ((r1.www != 0u) ? r1.xyz : r0.yzw);
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
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _MainTex_ST : packoffset(c0);
    float4 _MainTex1_ST : packoffset(c1);
    float4 _DissolveTex_ST : packoffset(c2);
    float4 _DissolveTex1_ST : packoffset(c3);
    float4 _DistortionTex_ST : packoffset(c4);
    float2 _MainTexUVSpeed : packoffset(c9);
    float2 _MainTex1UVSpeed : packoffset(c9.z);
    float2 _DissolveUVSpeed : packoffset(c10);
    float2 _Dissolve1UVSpeed : packoffset(c10.z);
    float2 _DistortionUVSpeed : packoffset(c11);
    float _MainTexUVMove : packoffset(c15.z);
    float _MainTex1UVMove : packoffset(c15.w);
    float _DissolveUVMove : packoffset(c16);
    float _Dissolve1UVMove : packoffset(c16.y);
    float _DissolveRandomUV : packoffset(c16.z);
    float _Dissolve1RandomUV : packoffset(c16.w);
    float _DistortionUVMove : packoffset(c17);
    float _MainTexFlip : packoffset(c20.y);
    float _MainTexRotation : packoffset(c20.z);
    float _DissolveTexFlip : packoffset(c22.y);
    float _DissolveTexRotation : packoffset(c22.z);
    float _DistortionTexFlip : packoffset(c24.y);
    float _DistortionTexRotation : packoffset(c24.z);
    float _ZOffset : packoffset(c28.z);
    float _IgnoreTimeScale : packoffset(c28.w);
    float _TimeOffset : packoffset(c29.z);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float4 output3 : TEXCOORD2;
    float4 output4 : TEXCOORD3;
    float3 output5 : TEXCOORD4;
    float3 output6 : TEXCOORD6;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float3 input3 : TEXCOORD2, float4 input4 : COLOR0)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, o0, o1, o2, o3, o4, o5, o6, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyz = asuint(input3);
    v4.xyzw = asuint(input4);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r1.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r1.xyz)));
    o6.xyz = r0.xyz;
    r0.xyz = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
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
    r3.xyzw = asuint(mad(asfloat(v1.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v1.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
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
    r1.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_MainTex1_ST.x, _MainTex1_ST.y)))), asfloat(asuint((float2(_MainTex1_ST.z, _MainTex1_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r0.zw) + asfloat(r1.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r1.x = ((r1.x != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r1.x = asuint((asfloat(r1.x) + asfloat((asuint((_TimeOffset)) ^ 0x80000000u))));
    r2.xyzw = asuint((asfloat(r1.xxxx) * asfloat(asuint((float4(_MainTexUVMove, _MainTexUVMove, _MainTex1UVMove, _MainTex1UVMove))))));
    o1.xy = asuint(mad(asfloat(r2.xy), asfloat(asuint((float2(_MainTexUVSpeed.x, _MainTexUVSpeed.y)))), asfloat(r0.zw)));
    o1.zw = asuint(mad(asfloat(r2.zw), asfloat(asuint((float2(_MainTex1UVSpeed.x, _MainTex1UVSpeed.y)))), asfloat(r0.xy)));
    r0.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.w = asuint((asfloat((asuint((_DissolveTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r5.xyzw = ((asfloat(asuint((float4(_DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip, _DissolveTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.yzw = ((r5.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r5.xyzw = (r5.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(r5.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r6.xyzw = asuint(mad(asfloat(asuint((float4(_DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation, _DissolveTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation, _DistortionTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r2.xy = r6.xw;
    r2.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.xy = r6.zw;
    r0.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r4.xyzw)));
    r6.xz = r0.xz;
    r6.w = asuint((_DissolveTexRotation));
    r0.xyzw = asuint(mad(asfloat(r6.xyzw), asfloat(r1.yyyy), asfloat(r4.xyzw)));
    r2.yw = r6.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r5.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r1.yz = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex_ST.x, _DissolveTex_ST.y)))), asfloat(asuint((float2(_DissolveTex_ST.z, _DissolveTex_ST.w))))));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DissolveTex1_ST.x, _DissolveTex1_ST.y)))), asfloat(asuint((float2(_DissolveTex1_ST.z, _DissolveTex1_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.zw = asuint((asfloat(r0.zw) + asfloat(r1.yz)));
    r2.xyzw = asuint((asfloat(r1.xxxx) * asfloat(asuint((float4(_DissolveUVMove, _DissolveUVMove, _Dissolve1UVMove, _Dissolve1UVMove))))));
    r1.x = asuint((asfloat(r1.x) * asfloat(asuint((_DistortionUVMove)))));
    r2.xyzw = asuint((asfloat(r2.xyzw) * asfloat(asuint((float4(_DissolveUVSpeed.x, _DissolveUVSpeed.y, _Dissolve1UVSpeed.x, _Dissolve1UVSpeed.y))))));
    r1.yz = asuint(mad(asfloat(asuint((float2(_DissolveRandomUV, _DissolveRandomUV)))), asfloat(v1.zw), asfloat(r2.xy)));
    r2.xy = asuint(mad(asfloat(asuint((float2(_Dissolve1RandomUV, _Dissolve1RandomUV)))), asfloat(v2.xy), asfloat(r2.zw)));
    o2.zw = asuint((asfloat(r0.xy) + asfloat(r2.xy)));
    o2.xy = asuint((asfloat(r0.zw) + asfloat(r1.yz)));
    r0.xy = r3.zw;
    r0.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.xy = r3.xw;
    r2.w = asuint((asfloat((asuint((_DistortionTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.xyzw = ((asfloat(asuint((float4(_DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip, _DistortionTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r1.yzw = ((r4.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r4.xyzw = (r4.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.yzw = asuint((asfloat(r1.yzw) + asfloat(r4.yzw)));
    r1.yzw = asuint(max(asfloat(r1.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r2.z = 0x00000000u;
    r5.xyzw = asuint((asfloat(r1.zzzz) * asfloat(r2.xyzw)));
    r0.z = 0x3f800000u;
    r5.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r1.wwww), asfloat(r5.xyzw)));
    r3.xz = r0.xz;
    r3.w = asuint((_DistortionTexRotation));
    r0.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r1.yyyy), asfloat(r5.xyzw)));
    r2.yw = r3.yw;
    r0.xyzw = asuint(mad(asfloat(r2.xyzw), asfloat(r4.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DistortionTex_ST.x, _DistortionTex_ST.y)))), asfloat(asuint((float2(_DistortionTex_ST.z, _DistortionTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    o3.xy = asuint(mad(asfloat(r1.xx), asfloat(asuint((float2(_DistortionUVSpeed.x, _DistortionUVSpeed.y)))), asfloat(r0.xy)));
    o3.zw = v2.zw;
    o4.xyzw = v4.xyzw;
    o5.xyz = v3.xyz;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyzw);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyz);
    result.output6 = asfloat(o6.xyz);
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
    float3 _Color : packoffset(c7);
    float3 _InnerColor : packoffset(c8);
    float2 _MainTex0SpherizeSmoothStep : packoffset(c11.z);
    float2 _MainTex1SpherizeSmoothStep : packoffset(c12);
    float2 _DissolveTex0SpherizeSmoothStep : packoffset(c12.z);
    float2 _DissolveTex1SpherizeSmoothStep : packoffset(c13);
    float2 _DistortionTexSpherizeSmoothStep : packoffset(c13.z);
    float _MainTexChannelMapping : packoffset(c14);
    float _DissolveAlphaMapping : packoffset(c14.y);
    float _MainTex1ChannelMapping : packoffset(c14.z);
    float _DissolveInnerAlphaMapping : packoffset(c14.w);
    float _DissolveChannelMapping : packoffset(c15);
    float _DissolveChannelMapping1 : packoffset(c15.y);
    float _DistortionIntensity : packoffset(c17.y);
    float _DistortionAffectsMainTone : packoffset(c17.z);
    float _DistortionAffectsInnerTone : packoffset(c17.w);
    float _DissolveProgress : packoffset(c18);
    float _DissolveProgress1 : packoffset(c18.y);
    float _UseCustomCurve1X : packoffset(c18.z);
    float _UseCustomCurve1X1Y : packoffset(c18.w);
    float _SoftRange : packoffset(c19);
    float _SoftRange1 : packoffset(c19.y);
    float _UseCustomColor : packoffset(c19.z);
    float _MainTexClampU : packoffset(c19.w);
    float _MainTexClampV : packoffset(c20);
    float _MainTex0SpherizeUV : packoffset(c20.w);
    float _MainTex0SphereScale : packoffset(c21);
    float _MainTex1SpherizeUV : packoffset(c21.y);
    float _MainTex1SphereScale : packoffset(c21.z);
    float _DissolveTexClampU : packoffset(c21.w);
    float _DissolveTexClampV : packoffset(c22);
    float _DissolveTex0SpherizeUV : packoffset(c22.w);
    float _DissolveTex0SphereScale : packoffset(c23);
    float _DissolveTex1SpherizeUV : packoffset(c23.y);
    float _DissolveTex1SphereScale : packoffset(c23.z);
    float _DistortionTexClampU : packoffset(c23.w);
    float _DistortionTexClampV : packoffset(c24);
    float _DistortionTexSpherizeUV : packoffset(c24.w);
    float _DistortionTexSphereScale : packoffset(c25);
    float _BlendMode : packoffset(c28.y);
    float _OpaquenessFadeByScript : packoffset(c29);
}
static const uint4 icb[4] = { uint4(0x3f800000u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x3f800000u, 0x00000000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x00000000u), uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) };


SamplerState sampler_MainTex;
SamplerState sampler_DissolveTex;
SamplerState sampler_DistortionTex;
Texture2D<float4> _MainTex : register(t0);
Texture2D<float4> _DissolveTex : register(t1);
Texture2D<float4> _DistortionTex : register(t2);
struct CorinFragmentOut {
    float4 output0 : SV_Target0;
};

CorinFragmentOut CorinPixel(float4 input0 : SV_POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float4 input3 : TEXCOORD2, float4 input4 : TEXCOORD3, float3 input5 : TEXCOORD4, float3 input6 : TEXCOORD6)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, v0, v1, v2, v3, v4, v5, v6;
    CorinFragmentOut result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyz = asuint(input5);
    v6.xyz = asuint(input6);
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveTexClampU)))) ? 0xffffffffu : 0u);
    r1.xyzw = asuint(max(asfloat(v2.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r1.xyzw = asuint(min(asfloat(r1.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r0.xy = ((r0.xx != 0u) ? r1.xz : v2.xz);
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DissolveTexClampV)))) ? 0xffffffffu : 0u);
    r0.zw = ((r1.xx != 0u) ? r1.yw : v2.yw);
    r1.xyzw = asuint((asfloat(r0.xzyw) + asfloat(uint4(0xbf000000u, 0xbf000000u, 0xbf000000u, 0xbf000000u))));
    r2.x = asuint(dot(asfloat(r1.zw), asfloat(r1.zw)));
    r2.xy = asuint(mad(asfloat((r2.xx ^ 0x80000000u)), asfloat(asuint((float2(_DissolveTex1SphereScale, _DissolveTex1SphereScale)))), asfloat(asuint((float2(_DissolveTex1SpherizeSmoothStep.x, _DissolveTex1SpherizeSmoothStep.y))))));
    r2.x = asuint((asfloat(0x3f800000u) / asfloat(r2.x)));
    r2.x = asuint(saturate((asfloat(r2.x) * asfloat(r2.y))));
    r2.y = asuint(mad(asfloat(r2.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.x)));
    r2.x = asuint((asfloat(r2.x) * asfloat(r2.y)));
    r1.zw = asuint(mad(asfloat(r1.zw), asfloat(r2.xx), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r2.x = ((asfloat(asuint((_DissolveTex1SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.yw = ((r2.xx != 0u) ? r0.yw : r1.zw);
    r2.xyzw = asuint(_DissolveTex.Sample(sampler_DissolveTex, asfloat(r0.yw)).xyzw);
    r0.yw = (uint2)(asfloat(asuint((float2(_DissolveChannelMapping, _DissolveChannelMapping1)))));
    r0.w = asuint(dot(asfloat(r2.xyzw), asfloat(icb[r0.w+0].xyzw)));
    r1.zw = asuint(mad(asfloat(asuint((float2(_SoftRange, _SoftRange1)))), asfloat(uint2(0x3efae148u, 0x3efae148u)), asfloat(uint2(0x3c23d70au, 0x3c23d70au))));
    r1.w = asuint((asfloat(r0.w) + asfloat((r1.w ^ 0x80000000u))));
    r0.w = asuint((asfloat(r0.w) + asfloat((r1.w ^ 0x80000000u))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
    r2.xy = asuint((asfloat(v3.zw) + asfloat((asuint((float2(_DissolveProgress, _DissolveProgress1))) ^ 0x80000000u))));
    r2.xy = asuint(mad(asfloat(asuint((float2(_UseCustomCurve1X, _UseCustomCurve1X1Y)))), asfloat(r2.xy), asfloat(asuint((float2(_DissolveProgress, _DissolveProgress1))))));
    r2.xy = asuint(mad(asfloat(r2.xy), asfloat(uint2(0x3fc00000u, 0x3fc00000u)), asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r1.w = asuint((asfloat((r1.w ^ 0x80000000u)) + asfloat(r2.y)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(r1.w))));
    r1.w = asuint(mad(asfloat(r0.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionTexClampU)))) ? 0xffffffffu : 0u);
    r2.yz = asuint(max(asfloat(v3.xy), asfloat(uint2(0x00000000u, 0x00000000u))));
    r2.yz = asuint(min(asfloat(r2.yz), asfloat(uint2(0x3f7f7ceeu, 0x3f7f7ceeu))));
    r3.x = ((r1.w != 0u) ? r2.y : v3.x);
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionTexClampV)))) ? 0xffffffffu : 0u);
    r3.y = ((r1.w != 0u) ? r2.z : v3.y);
    r2.yz = asuint((asfloat(r3.xy) + asfloat(uint2(0xbf000000u, 0xbf000000u))));
    r1.w = asuint(dot(asfloat(r2.yz), asfloat(r2.yz)));
    r3.zw = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_DistortionTexSphereScale, _DistortionTexSphereScale)))), asfloat(asuint((float2(_DistortionTexSpherizeSmoothStep.x, _DistortionTexSpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r3.z)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r3.w))));
    r2.w = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.w)));
    r2.yz = asuint(mad(asfloat(r2.yz), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_DistortionTexSpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yz = ((r1.ww != 0u) ? r3.xy : r2.yz);
    r2.yz = asuint(_DistortionTex.Sample(sampler_DistortionTex, asfloat(r2.yz)).xy);
    r3.xyzw = asuint(mad(asfloat(r2.yzyz), asfloat(uint4(0x40000000u, 0x40000000u, 0x40000000u, 0x40000000u)), asfloat(uint4(0xbf800000u, 0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) * asfloat(asuint((float4(_DistortionIntensity, _DistortionIntensity, _DistortionIntensity, _DistortionIntensity))))));
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampU)))) ? 0xffffffffu : 0u);
    r4.xyzw = asuint(max(asfloat(v1.xyzw), asfloat(uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u))));
    r4.xyzw = asuint(min(asfloat(r4.xyzw), asfloat(uint4(0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu, 0x3f7f7ceeu))));
    r5.xy = ((r1.ww != 0u) ? r4.xz : v1.xz);
    r1.w = ((asfloat(0x3f000000u) < asfloat(asuint((_MainTexClampV)))) ? 0xffffffffu : 0u);
    r5.zw = ((r1.ww != 0u) ? r4.yw : v1.yw);
    r4.xyzw = asuint((asfloat(r5.xzyw) + asfloat(uint4(0xbf000000u, 0xbf000000u, 0xbf000000u, 0xbf000000u))));
    r1.w = asuint(dot(asfloat(r4.zw), asfloat(r4.zw)));
    r2.yz = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_MainTex1SphereScale, _MainTex1SphereScale)))), asfloat(asuint((float2(_MainTex1SpherizeSmoothStep.x, _MainTex1SpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r2.z))));
    r2.y = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.y)));
    r2.yz = asuint(mad(asfloat(r4.zw), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_MainTex1SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yz = ((r1.ww != 0u) ? r5.yw : r2.yz);
    r2.yz = asuint(mad(asfloat(r3.zw), asfloat(asuint((float2(_DistortionAffectsInnerTone, _DistortionAffectsInnerTone)))), asfloat(r2.yz)));
    r6.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r2.yz)).xyzw);
    r7.xyzw = (uint4)(asfloat(asuint((float4(_MainTexChannelMapping, _DissolveAlphaMapping, _MainTex1ChannelMapping, _DissolveInnerAlphaMapping)))));
    r1.w = asuint(dot(asfloat(r6.xyzw), asfloat(icb[r7.w+0].xyzw)));
    r2.y = asuint(dot(asfloat(r6.xyzw), asfloat(icb[r7.z+0].xyzw)));
    r6.xyz = asuint((asfloat((r2.yyy ^ 0x80000000u)) + asfloat(r6.xyz)));
    r1.w = asuint((asfloat(r1.w) + asfloat((r2.y ^ 0x80000000u))));
    r2.zw = ((asfloat(asuint((float2(_MainTexChannelMapping, _MainTex1ChannelMapping)))) >= asfloat(uint2(0x40600000u, 0x40600000u))) ? 0xffffffffu : 0u);
    r2.zw = (r2.zw & uint2(0x3f800000u, 0x3f800000u));
    r1.w = asuint(mad(asfloat(r2.w), asfloat(r1.w), asfloat(r2.y)));
    r6.xyz = asuint(mad(asfloat(r2.www), asfloat(r6.xyz), asfloat(r2.yyy)));
    r6.xyz = asuint((asfloat(r6.xyz) * asfloat(asuint((float3(_InnerColor.x, _InnerColor.y, _InnerColor.z))))));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(v4.w)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(0x40400000u))));
    r8.xyz = asuint((asfloat(v5.xyz) + asfloat(uint3(0xbf800000u, 0xbf800000u, 0xbf800000u))));
    r8.xyz = asuint(mad(asfloat(asuint((float3(_UseCustomColor, _UseCustomColor, _UseCustomColor)))), asfloat(r8.xyz), asfloat(uint3(0x3f800000u, 0x3f800000u, 0x3f800000u))));
    r6.xyz = asuint((asfloat(r6.xyz) * asfloat(r8.xyz)));
    r6.xyz = asuint((asfloat(r0.www) * asfloat(r6.xyz)));
    r0.w = asuint(dot(asfloat(r4.xy), asfloat(r4.xy)));
    r2.yw = asuint(mad(asfloat((r0.ww ^ 0x80000000u)), asfloat(asuint((float2(_MainTex0SphereScale, _MainTex0SphereScale)))), asfloat(asuint((float2(_MainTex0SpherizeSmoothStep.x, _MainTex0SpherizeSmoothStep.y))))));
    r0.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r0.w = asuint(saturate((asfloat(r0.w) * asfloat(r2.w))));
    r1.w = asuint(mad(asfloat(r0.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
    r2.yw = asuint(mad(asfloat(r4.xy), asfloat(r0.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r0.w = ((asfloat(asuint((_MainTex0SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r2.yw = ((r0.ww != 0u) ? r5.xz : r2.yw);
    r2.yw = asuint(mad(asfloat(r3.xy), asfloat(asuint((float2(_DistortionAffectsMainTone, _DistortionAffectsMainTone)))), asfloat(r2.yw)));
    r3.xyzw = asuint(_MainTex.Sample(sampler_MainTex, asfloat(r2.yw)).xyzw);
    r0.w = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r7.x+0].xyzw)));
    r1.w = asuint(dot(asfloat(r3.xyzw), asfloat(icb[r7.y+0].xyzw)));
    r3.xyz = asuint((asfloat((r0.www ^ 0x80000000u)) + asfloat(r3.xyz)));
    r3.xyz = asuint(mad(asfloat(r2.zzz), asfloat(r3.xyz), asfloat(r0.www)));
    r3.xyz = asuint(mad(asfloat(r3.xyz), asfloat(asuint((float3(_Color.x, _Color.y, _Color.z)))), asfloat(r6.xyz)));
    r3.xyz = asuint((asfloat(r3.xyz) * asfloat(v4.xyz)));
    r1.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(r1.w)));
    r0.w = asuint(mad(asfloat(r2.z), asfloat(r1.w), asfloat(r0.w)));
    r1.w = asuint(dot(asfloat(r1.xy), asfloat(r1.xy)));
    r2.yz = asuint(mad(asfloat((r1.ww ^ 0x80000000u)), asfloat(asuint((float2(_DissolveTex0SphereScale, _DissolveTex0SphereScale)))), asfloat(asuint((float2(_DissolveTex0SpherizeSmoothStep.x, _DissolveTex0SpherizeSmoothStep.y))))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r2.y)));
    r1.w = asuint(saturate((asfloat(r1.w) * asfloat(r2.z))));
    r2.y = asuint(mad(asfloat(r1.w), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
    r1.w = asuint((asfloat(r1.w) * asfloat(r2.y)));
    r1.xy = asuint(mad(asfloat(r1.xy), asfloat(r1.ww), asfloat(uint2(0x3f000000u, 0x3f000000u))));
    r1.w = ((asfloat(asuint((_DissolveTex0SpherizeUV))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    r0.xz = ((r1.ww != 0u) ? r0.xz : r1.xy);
    r4.xyzw = asuint(_DissolveTex.Sample(sampler_DissolveTex, asfloat(r0.xz)).xyzw);
    r0.x = asuint(dot(asfloat(r4.xyzw), asfloat(icb[r0.y+0].xyzw)));
    r0.y = asuint((asfloat((r1.z ^ 0x80000000u)) + asfloat(r0.x)));
    r0.x = asuint((asfloat((r0.y ^ 0x80000000u)) + asfloat(r0.x)));
    r0.y = asuint((asfloat((r0.y ^ 0x80000000u)) + asfloat(r2.x)));
    r0.x = asuint((asfloat(0x3f800000u) / asfloat(r0.x)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(r0.y))));
    r0.y = asuint(mad(asfloat(r0.x), asfloat(0xc0000000u), asfloat(0x40400000u)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(r0.y)));
    r0.x = asuint((asfloat(r0.w) * asfloat(r0.x)));
    r0.x = asuint((asfloat(r0.x) * asfloat(v4.w)));
    r0.x = asuint(saturate((asfloat(r0.x) * asfloat(0x40400000u))));
    r0.x = asuint((asfloat(r0.x) * asfloat(asuint((_OpaquenessFadeByScript)))));
    r0.yzw = asuint((asfloat(r0.xxx) * asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r0.yzw), asfloat(asuint((float3(_NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x, _NapEffectBrightnessParams4.x)))), asfloat(asuint((float3(_NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y, _NapEffectBrightnessParams4.y))))));
    r1.w = asuint(max(asfloat(r0.x), asfloat(0x3a83126fu)));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r1.xyz = asuint(mad(asfloat(r1.xyz), asfloat(r1.www), asfloat((r3.xyz ^ 0x80000000u))));
    r2.xyz = ((asfloat(r0.yzw) >= asfloat(asuint((float3(_NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w, _NapEffectBrightnessParams4.w))))) ? 0xffffffffu : 0u);
    r2.xyz = (r2.xyz & uint3(0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.xyz = asuint(mad(asfloat(r2.xyz), asfloat(r1.xyz), asfloat(r3.xyz)));
    r1.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z, _NapEffectBrightnessParams4.z)))), asfloat((r3.xyz ^ 0x80000000u))));
    r0.y = asuint(max(asfloat(r0.z), asfloat(r0.y)));
    r0.y = asuint(max(asfloat(r0.w), asfloat(r0.y)));
    r0.y = asuint((asfloat(r0.y) + asfloat((asuint((_NapEffectBrightnessExtraParams.x)) ^ 0x80000000u))));
    r0.y = asuint(saturate((asfloat(r0.y) * asfloat(asuint((_NapEffectBrightnessExtraParams.y))))));
    r0.yzw = asuint(mad(asfloat(r0.yyy), asfloat(r1.xyz), asfloat(r3.xyz)));
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
    column_major float4x4 glstate_matrix_projection : packoffset(c90);
    column_major float4x4 unity_MatrixV : packoffset(c94);
}

cbuffer UnityPerDraw : register(b1)
{
    column_major float4x4 unity_ObjectToWorld : packoffset(c0);
}

cbuffer UnityPerMaterial : register(b2)
{
    float4 _DTTex_ST : packoffset(c5);
    float _DTTexFlip : packoffset(c26);
    float _DTTexRotation : packoffset(c26.y);
    float _DTIntensity : packoffset(c26.z);
    float _Dist_Intensity_PostProcessing : packoffset(c26.w);
    float _DtUvMove : packoffset(c27);
    float _DtUSpeed : packoffset(c27.y);
    float _DtVSpeed : packoffset(c27.z);
    float _ZOffset : packoffset(c28.z);
    float _IgnoreTimeScale : packoffset(c28.w);
    float _OpaquenessFadeByScript : packoffset(c29);
}



struct CorinVertexOut {
    float4 output0 : SV_POSITION0;
    float2 output1 : TEXCOORD0;
    float output2 : TEXCOORD1;
};

CorinVertexOut CorinVertex(float3 input0 : POSITION0, float4 input1 : TEXCOORD0, float4 input2 : TEXCOORD1, float3 input3 : TEXCOORD2, float4 input4 : COLOR0)
{
    uint4 r0, r1, r2, r3, r4, o0, o1, v0, v1, v2, v3, v4;
    CorinVertexOut result;
    v0.xyz = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyz = asuint(input3);
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
    r0.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r1.w = asuint((asfloat((asuint((_DTTexRotation)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.xyzw = ((asfloat(asuint((float4(_DTTexFlip, _DTTexFlip, _DTTexFlip, _DTTexFlip)))) < asfloat(uint4(0x3f000000u, 0x3fc00000u, 0x40200000u, 0x477fe000u))) ? 0xffffffffu : 0u);
    r3.xyz = ((r2.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r2.xyzw = (r2.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r2.yzw = asuint((asfloat(r3.xyz) + asfloat(r2.yzw)));
    r2.yzw = asuint(max(asfloat(r2.yzw), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r3.xyzw = asuint(mad(asfloat(v1.yxyx), asfloat(uint4(0x3f800000u, 0xbf800000u, 0xbf800000u, 0x3f800000u)), asfloat(uint4(0x00000000u, 0x3f800000u, 0x3f800000u, 0x00000000u))));
    r4.xyzw = asuint(mad(asfloat(v1.xyxy), asfloat(uint4(0x3f800000u, 0x3f800000u, 0xbf800000u, 0xbf800000u)), asfloat(uint4(0x00000000u, 0x00000000u, 0x3f800000u, 0x3f800000u))));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat((r4.xyzw ^ 0x80000000u))));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(_DTTexRotation, _DTTexRotation, _DTTexRotation, _DTTexRotation)))), asfloat(r3.xyzw), asfloat(r4.xyzw)));
    r1.xy = r3.xw;
    r1.z = 0x00000000u;
    r4.xyzw = asuint((asfloat(r1.xyzw) * asfloat(r2.zzzz)));
    r0.xy = r3.zw;
    r0.z = 0x3f800000u;
    r4.xyzw = asuint(mad(asfloat(r0.xyzw), asfloat(r2.wwww), asfloat(r4.xyzw)));
    r3.xz = r0.xz;
    r3.w = asuint((_DTTexRotation));
    r0.xyzw = asuint(mad(asfloat(r3.xyzw), asfloat(r2.yyyy), asfloat(r4.xyzw)));
    r1.yw = r3.yw;
    r0.xyzw = asuint(mad(asfloat(r1.xyzw), asfloat(r2.xxxx), asfloat(r0.xyzw)));
    r0.xy = asuint((asfloat((r0.zw ^ 0x80000000u)) + asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_DTTex_ST.x, _DTTex_ST.y)))), asfloat(asuint((float2(_DTTex_ST.z, _DTTex_ST.w))))));
    r0.xy = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r0.z = ((asfloat(0x3f000000u) < asfloat(asuint((_IgnoreTimeScale)))) ? 0xffffffffu : 0u);
    r0.z = ((r0.z != 0u) ? asuint((_GlobalTimeParamsA[0][1])) : asuint((_GlobalTimeParamsB[1][0])));
    r0.zw = asuint((asfloat(r0.zz) * asfloat(asuint((float2(_DtUSpeed, _DtVSpeed))))));
    r0.zw = asuint(frac(asfloat(r0.zw)));
    r0.zw = asuint((asfloat(r0.zw) + asfloat(r0.xy)));
    r1.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DtUvMove)))) ? 0xffffffffu : 0u);
    o1.xy = ((r1.xx != 0u) ? r0.zw : r0.xy);
    r0.x = asuint((asfloat(v4.w) * asfloat(asuint((_DTIntensity)))));
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
    float _Distortion : packoffset(c25.y);
    float _DTTexClampU : packoffset(c25.z);
    float _DTTexClampV : packoffset(c25.w);
    float _SeparateRGBIntensity : packoffset(c27.w);
    float _DistortionMode : packoffset(c29.y);
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
    r1.xy = asuint((asfloat(r0.xy) + asfloat(r0.xy)));
    r0.x = asuint((asfloat(r0.z) * asfloat(asuint((_SeparateRGBIntensity)))));
    r1.z = asuint((asfloat(r0.x) * asfloat(v1.z)));
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_DistortionMode)))) ? 0xffffffffu : 0u);
    r1.w = 0x3f800000u;
    o0.xyzw = ((r0.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u) : r1.xyzw);
    o1.xyzw = (r0.xxxx & r1.xyzw);
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    return result;
}

            #endif
            ENDHLSL
        }
    }
}
