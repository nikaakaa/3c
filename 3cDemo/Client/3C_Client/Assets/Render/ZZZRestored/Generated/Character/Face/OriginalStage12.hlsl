
    column_major float4x4 unity_CameraProjection;
    column_major float4x4 glstate_matrix_projection;
    column_major float4x4 unity_MatrixV;
    column_major float4x4 _NonJitteredProjMatrix;
    column_major float4x4 _PrevViewMatrix;
    column_major float4x4 _PrevProjMatrix;
    float4 _CharStyleParams;
    float4 unity_OrthoParams;
    float _GlobalAdditionalLightIntensity;



    column_major float4x4 unity_ObjectToWorld;
    column_major float4x4 unity_WorldToObject;
    column_major float4x4 unity_MatrixPreviousM;
    column_major float4x4 unity_MatrixPreviousMI;
    float4 unity_MotionVectorsParams;



    column_major float4x4 _MainLightWorldToShadow[5];
    float4 _CascadeShadowSplitSpheres0;
    float4 _CascadeShadowSplitSpheres1;
    float4 _CascadeShadowSplitSpheres2;
    float4 _CascadeShadowSplitSpheres3;
    float4 _CascadeShadowSplitSphereRadii;



    float4 _PackedParams1;
    float4 _AmbientLights[16];



    float4 _MainTex_ST;
    float _MaxOutlineZOffset;
    float _OutlineWidth;






struct ShaderOutput {
    float4 output0 : SV_POSITION0;
    float4 output1 : TEXCOORD0;
    float4 output2 : TEXCOORD1;
    float3 output3 : TEXCOORD5;
    float4 output4 : TEXCOORD6;
    float4 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD7;
    float3 output7 : TEXCOORD8;
};
float4 ZZZRead__MainLightWorldToShadow(uint columnIndex)
{
    uint packed = columnIndex - 0;
    uint matrixIndex = packed / 4;
    uint column = packed % 4;
    return float4(_MainLightWorldToShadow[matrixIndex][0][column], _MainLightWorldToShadow[matrixIndex][1][column], _MainLightWorldToShadow[matrixIndex][2][column], _MainLightWorldToShadow[matrixIndex][3][column]);
}
ShaderOutput main(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float2 input4 : TEXCOORD0, float2 input5 : TEXCOORD1, float2 input6 : TEXCOORD2, float2 input7 : TEXCOORD3, float3 input8 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, o1, o2, o3, o4, o5, o6, o7, v0, v1, v2, v3, v4, v5, v6, v7, v8;
    ShaderOutput result;
    v0.xyz = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xy = asuint(input4);
    v5.xy = asuint(input5);
    v6.xy = asuint(input6);
    v7.xy = asuint(input7);
    v8.xyz = asuint(input8);
    r0.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r0.xyz)));
    r0.xyz = asuint((asfloat(r0.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r0.w = asuint(dot(asfloat(v5.xy), asfloat(v5.xy)));
    r0.w = asuint(min(asfloat(r0.w), asfloat(0x3f800000u)));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.w = asuint(sqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(v1.zxy) * asfloat(v2.yzx)));
    r1.xyz = asuint(mad(asfloat(v1.yzx), asfloat(v2.zxy), asfloat((r1.xyz ^ 0x80000000u))));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(v2.www)));
    r1.xyz = asuint((asfloat(r1.xyz) * asfloat(v5.yyy)));
    r1.xyz = asuint(mad(asfloat(v5.xxx), asfloat(v2.xyz), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(r0.www), asfloat(v1.xyz), asfloat(r1.xyz)));
    r0.w = asuint((asfloat(v3.y) + asfloat(0xbf008312u)));
    r2.xyz = asuint((asfloat(r0.yyy) * asfloat(asuint((float3(unity_MatrixV[0][1], unity_MatrixV[1][1], unity_MatrixV[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][0], unity_MatrixV[1][0], unity_MatrixV[2][0])))), asfloat(r0.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixV[0][2], unity_MatrixV[1][2], unity_MatrixV[2][2])))), asfloat(r0.zzz), asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r2.xyz) + asfloat(asuint((float3(unity_MatrixV[0][3], unity_MatrixV[1][3], unity_MatrixV[2][3]))))));
    r3.x = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r3.y = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r3.z = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r1.w = asuint(dot(asfloat(r3.xyz), asfloat(r3.xyz)));
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r3.xyz = asuint((asfloat(r1.www) * asfloat(r3.xyz)));
    r4.xy = asuint((asfloat(r3.yy) * asfloat(asuint((float2(unity_MatrixV[0][1], unity_MatrixV[1][1]))))));
    r4.xy = asuint(mad(asfloat(asuint((float2(unity_MatrixV[0][0], unity_MatrixV[1][0])))), asfloat(r3.xx), asfloat(r4.xy)));
    r4.xy = asuint(mad(asfloat(asuint((float2(unity_MatrixV[0][2], unity_MatrixV[1][2])))), asfloat(r3.zz), asfloat(r4.xy)));
    r4.z = 0x3727c5acu;
    r1.w = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r4.xy = asuint((asfloat(r1.ww) * asfloat(r4.xy)));
    r1.w = asuint(rcp(asfloat(asuint((unity_CameraProjection[1][1])))));
    r2.w = asuint((asfloat(r1.w) * asfloat((r2.z ^ 0x80000000u))));
    r5.xyz = asuint(saturate(mad(asfloat(r2.www), asfloat(uint3(0x41200000u, 0x3f800000u, 0x3f000000u)), asfloat(uint3(0x00000000u, 0xbdcccccdu, 0xbf866666u)))));
    r2.w = asuint((asfloat(r5.x) * asfloat(0x3a89a027u)));
    r3.w = asuint(mad(asfloat((r5.x ^ 0x80000000u)), asfloat(0x3a89a027u), asfloat(0x3ba3d70au)));
    r2.w = asuint(mad(asfloat(r5.y), asfloat(r3.w), asfloat(r2.w)));
    r3.w = asuint((asfloat((r2.w ^ 0x80000000u)) + asfloat(0x3b656042u)));
    r2.w = asuint(mad(asfloat(r5.z), asfloat(r3.w), asfloat(r2.w)));
    r3.w = asuint(mad(asfloat(asuint((unity_OrthoParams.y))), asfloat(0x3ba3d70au), asfloat((r2.w ^ 0x80000000u))));
    r2.w = asuint(mad(asfloat(asuint((unity_OrthoParams.w))), asfloat(r3.w), asfloat(r2.w)));
    r3.w = asuint((asfloat(v3.x) * asfloat(asuint((_OutlineWidth)))));
    r2.w = asuint((asfloat(r2.w) * asfloat(r3.w)));
    r4.xy = asuint((asfloat(r2.ww) * asfloat(r4.xy)));
    r2.w = ((asfloat(0x3f000000u) < asfloat(asuint((_CharStyleParams.w)))) ? 0xffffffffu : 0u);
    r4.z = asuint(mad(asfloat(r2.z), asfloat(r1.w), asfloat(asuint((_CharStyleParams.x)))));
    r4.z = asuint((asfloat(r4.z) / asfloat(asuint((_CharStyleParams.y)))));
    r4.z = asuint(min(asfloat((r4.z & 0x7fffffffu)), asfloat(0x3f800000u)));
    r4.w = asuint((asfloat((asuint((_CharStyleParams.z)) ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r4.z = asuint(mad(asfloat(r4.z), asfloat(r4.w), asfloat(asuint((_CharStyleParams.z)))));
    r4.z = ((r2.w != 0u) ? r4.z : 0x3f800000u);
    r2.xy = asuint(mad(asfloat(r4.xy), asfloat(r4.zz), asfloat(r2.xy)));
    r4.x = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r4.x = asuint(rsqrt(asfloat(r4.x)));
    r4.xyz = asuint((asfloat(r2.xyz) * asfloat(r4.xxx)));
    r4.xyz = asuint((asfloat(r0.www) * asfloat(r4.xyz)));
    r5.xyz = asuint(mad(asfloat(r4.xyz), asfloat(asuint((float3(_MaxOutlineZOffset, _MaxOutlineZOffset, _MaxOutlineZOffset)))), asfloat(r2.xyz)));
    r2.x = asuint((glstate_matrix_projection[0][0]));
    r2.y = asuint((glstate_matrix_projection[0][2]));
    r2.z = asuint((glstate_matrix_projection[0][3]));
    r5.w = 0x3f800000u;
    o0.x = asuint(dot(asfloat(r2.xyz), asfloat(r5.xzw)));
    r2.x = asuint((glstate_matrix_projection[1][1]));
    r2.y = asuint((glstate_matrix_projection[1][2]));
    r2.z = asuint((glstate_matrix_projection[1][3]));
    o0.y = asuint(dot(asfloat(r2.xyz), asfloat(r5.yzw)));
    r6.x = asuint((glstate_matrix_projection[2][0]));
    r6.y = asuint((glstate_matrix_projection[2][1]));
    r6.z = asuint((glstate_matrix_projection[2][2]));
    r6.w = asuint((glstate_matrix_projection[2][3]));
    o0.z = asuint(dot(asfloat(r6.xyzw), asfloat(r5.xyzw)));
    r2.x = asuint((glstate_matrix_projection[3][2]));
    r2.y = asuint((glstate_matrix_projection[3][3]));
    o0.w = asuint(dot(asfloat(r2.xy), asfloat(r5.zw)));
    o1.xy = asuint(mad(asfloat(v4.xy), asfloat(asuint((float2(_MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float2(_MainTex_ST.z, _MainTex_ST.w))))));
    r2.xyz = asuint((asfloat(r5.yyy) * asfloat(asuint((float3(_NonJitteredProjMatrix[0][1], _NonJitteredProjMatrix[1][1], _NonJitteredProjMatrix[3][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredProjMatrix[0][0], _NonJitteredProjMatrix[1][0], _NonJitteredProjMatrix[3][0])))), asfloat(r5.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(_NonJitteredProjMatrix[0][2], _NonJitteredProjMatrix[1][2], _NonJitteredProjMatrix[3][2])))), asfloat(r5.zzz), asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r2.xyz) + asfloat(asuint((float3(_NonJitteredProjMatrix[0][3], _NonJitteredProjMatrix[1][3], _NonJitteredProjMatrix[3][3]))))));
    r4.x = ((asfloat(0x00000000u) < asfloat(asuint((unity_MotionVectorsParams.x)))) ? 0xffffffffu : 0u);
    r4.xyz = ((r4.xxx != 0u) ? v8.xyz : v0.xyz);
    r5.xyz = asuint((asfloat(r4.yyy) * asfloat(asuint((float3(unity_MatrixPreviousM[0][1], unity_MatrixPreviousM[1][1], unity_MatrixPreviousM[2][1]))))));
    r5.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixPreviousM[0][0], unity_MatrixPreviousM[1][0], unity_MatrixPreviousM[2][0])))), asfloat(r4.xxx), asfloat(r5.xyz)));
    r4.xyz = asuint(mad(asfloat(asuint((float3(unity_MatrixPreviousM[0][2], unity_MatrixPreviousM[1][2], unity_MatrixPreviousM[2][2])))), asfloat(r4.zzz), asfloat(r5.xyz)));
    r4.xyz = asuint((asfloat(r4.xyz) + asfloat(asuint((float3(unity_MatrixPreviousM[0][3], unity_MatrixPreviousM[1][3], unity_MatrixPreviousM[2][3]))))));
    r5.xyz = asuint((asfloat(r4.yyy) * asfloat(asuint((float3(_PrevViewMatrix[0][1], _PrevViewMatrix[1][1], _PrevViewMatrix[2][1]))))));
    r5.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewMatrix[0][0], _PrevViewMatrix[1][0], _PrevViewMatrix[2][0])))), asfloat(r4.xxx), asfloat(r5.xyz)));
    r4.xyz = asuint(mad(asfloat(asuint((float3(_PrevViewMatrix[0][2], _PrevViewMatrix[1][2], _PrevViewMatrix[2][2])))), asfloat(r4.zzz), asfloat(r5.xyz)));
    r4.xyz = asuint((asfloat(r4.xyz) + asfloat(asuint((float3(_PrevViewMatrix[0][3], _PrevViewMatrix[1][3], _PrevViewMatrix[2][3]))))));
    r5.x = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_MatrixPreviousMI[0][0], unity_MatrixPreviousMI[1][0], unity_MatrixPreviousMI[2][0]))))));
    r5.y = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_MatrixPreviousMI[0][1], unity_MatrixPreviousMI[1][1], unity_MatrixPreviousMI[2][1]))))));
    r5.z = asuint(dot(asfloat(r1.xyz), asfloat(asuint((float3(unity_MatrixPreviousMI[0][2], unity_MatrixPreviousMI[1][2], unity_MatrixPreviousMI[2][2]))))));
    r1.x = asuint(dot(asfloat(r5.xyz), asfloat(r5.xyz)));
    r1.x = asuint(max(asfloat(r1.x), asfloat(0x00800000u)));
    r1.x = asuint(rsqrt(asfloat(r1.x)));
    r1.xyz = asuint((asfloat(r1.xxx) * asfloat(r5.xyz)));
    r5.xy = asuint((asfloat(r1.yy) * asfloat(asuint((float2(_PrevViewMatrix[0][1], _PrevViewMatrix[1][1]))))));
    r1.xy = asuint(mad(asfloat(asuint((float2(_PrevViewMatrix[0][0], _PrevViewMatrix[1][0])))), asfloat(r1.xx), asfloat(r5.xy)));
    r1.xy = asuint(mad(asfloat(asuint((float2(_PrevViewMatrix[0][2], _PrevViewMatrix[1][2])))), asfloat(r1.zz), asfloat(r1.xy)));
    r1.z = 0x3727c5acu;
    r1.z = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.z = asuint(rsqrt(asfloat(r1.z)));
    r1.xy = asuint((asfloat(r1.zz) * asfloat(r1.xy)));
    r1.z = asuint((asfloat(r1.w) * asfloat((r4.z ^ 0x80000000u))));
    r5.xyz = asuint(saturate(mad(asfloat(r1.zzz), asfloat(uint3(0x41200000u, 0x3f800000u, 0x3f000000u)), asfloat(uint3(0x00000000u, 0xbdcccccdu, 0xbf866666u)))));
    r1.z = asuint((asfloat(r5.x) * asfloat(0x3a89a027u)));
    r5.x = asuint(mad(asfloat((r5.x ^ 0x80000000u)), asfloat(0x3a89a027u), asfloat(0x3ba3d70au)));
    r1.z = asuint(mad(asfloat(r5.y), asfloat(r5.x), asfloat(r1.z)));
    r5.x = asuint((asfloat((r1.z ^ 0x80000000u)) + asfloat(0x3b656042u)));
    r1.z = asuint(mad(asfloat(r5.z), asfloat(r5.x), asfloat(r1.z)));
    r5.x = asuint(mad(asfloat(asuint((unity_OrthoParams.y))), asfloat(0x3ba3d70au), asfloat((r1.z ^ 0x80000000u))));
    r1.z = asuint(mad(asfloat(asuint((unity_OrthoParams.w))), asfloat(r5.x), asfloat(r1.z)));
    r1.z = asuint((asfloat(r1.z) * asfloat(r3.w)));
    r1.xy = asuint((asfloat(r1.zz) * asfloat(r1.xy)));
    r1.z = asuint(mad(asfloat(r4.z), asfloat(r1.w), asfloat(asuint((_CharStyleParams.x)))));
    r1.z = asuint((asfloat(r1.z) / asfloat(asuint((_CharStyleParams.y)))));
    r1.z = asuint(min(asfloat((r1.z & 0x7fffffffu)), asfloat(0x3f800000u)));
    r1.z = asuint(mad(asfloat(r1.z), asfloat(r4.w), asfloat(asuint((_CharStyleParams.z)))));
    r1.z = ((r2.w != 0u) ? r1.z : 0x3f800000u);
    r4.xy = asuint(mad(asfloat(r1.xy), asfloat(r1.zz), asfloat(r4.xy)));
    r1.x = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    r1.x = asuint(rsqrt(asfloat(r1.x)));
    r1.xyz = asuint((asfloat(r1.xxx) * asfloat(r4.xyz)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(r1.xyz), asfloat(asuint((float3(_MaxOutlineZOffset, _MaxOutlineZOffset, _MaxOutlineZOffset)))), asfloat(r4.xyz)));
    r4.xyz = asuint((asfloat(r1.yyy) * asfloat(asuint((float3(_PrevProjMatrix[0][1], _PrevProjMatrix[1][1], _PrevProjMatrix[3][1]))))));
    r1.xyw = asuint(mad(asfloat(asuint((float3(_PrevProjMatrix[0][0], _PrevProjMatrix[1][0], _PrevProjMatrix[3][0])))), asfloat(r1.xxx), asfloat(r4.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(_PrevProjMatrix[0][2], _PrevProjMatrix[1][2], _PrevProjMatrix[3][2])))), asfloat(r1.zzz), asfloat(r1.xyw)));
    o2.yzw = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(_PrevProjMatrix[0][3], _PrevProjMatrix[1][3], _PrevProjMatrix[3][3]))))));
    r0.w = asuint((asfloat(v3.z) * asfloat(0x437f0000u)));
    r0.w = asuint((int)(asfloat(r0.w)));
    r0.w = (r0.w & 0x00000003u);
    r0.w = asuint((float)(asint(r0.w)));
    o6.w = asuint(mad(asfloat((r0.w ^ 0x80000000u)), asfloat(0x3e4ccccdu), asfloat(0x3f666666u)));
    o5.xyzw = asuint(mad(asfloat(v7.xyxy), asfloat(asuint((float4(_MainTex_ST.x, _MainTex_ST.y, _MainTex_ST.x, _MainTex_ST.y)))), asfloat(asuint((float4(_MainTex_ST.z, _MainTex_ST.w, _MainTex_ST.z, _MainTex_ST.w))))));
    r1.xyz = asuint((asfloat(r0.xyz) + asfloat((asuint((float3(_CascadeShadowSplitSpheres0.x, _CascadeShadowSplitSpheres0.y, _CascadeShadowSplitSpheres0.z))) ^ 0x80000000u))));
    r4.xyz = asuint((asfloat(r0.xyz) + asfloat((asuint((float3(_CascadeShadowSplitSpheres1.x, _CascadeShadowSplitSpheres1.y, _CascadeShadowSplitSpheres1.z))) ^ 0x80000000u))));
    r5.xyz = asuint((asfloat(r0.xyz) + asfloat((asuint((float3(_CascadeShadowSplitSpheres2.x, _CascadeShadowSplitSpheres2.y, _CascadeShadowSplitSpheres2.z))) ^ 0x80000000u))));
    r6.xyz = asuint((asfloat(r0.xyz) + asfloat((asuint((float3(_CascadeShadowSplitSpheres3.x, _CascadeShadowSplitSpheres3.y, _CascadeShadowSplitSpheres3.z))) ^ 0x80000000u))));
    r1.x = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.y = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
    r1.z = asuint(dot(asfloat(r5.xyz), asfloat(r5.xyz)));
    r1.w = asuint(dot(asfloat(r6.xyz), asfloat(r6.xyz)));
    r1.xyzw = ((asfloat(r1.xyzw) < asfloat(asuint((float4(_CascadeShadowSplitSphereRadii.x, _CascadeShadowSplitSphereRadii.y, _CascadeShadowSplitSphereRadii.z, _CascadeShadowSplitSphereRadii.w))))) ? 0xffffffffu : 0u);
    r4.xyzw = (r1.xyzw & uint4(0x3f800000u, 0x3f800000u, 0x3f800000u, 0x3f800000u));
    r1.xyz = ((r1.xyz != 0u) ? uint3(0xbf800000u, 0xbf800000u, 0xbf800000u) : uint3(0x80000000u, 0x80000000u, 0x80000000u));
    r1.xyz = asuint((asfloat(r1.xyz) + asfloat(r4.yzw)));
    r4.yzw = asuint(max(asfloat(r1.xyz), asfloat(uint3(0x00000000u, 0x00000000u, 0x00000000u))));
    r0.w = asuint(dot(asfloat(r4.xyzw), asfloat(uint4(0x40800000u, 0x40400000u, 0x40000000u, 0x3f800000u))));
    r0.w = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x40800000u)));
    r1.x = (uint)(asfloat(r0.w));
    r1.x = (r1.x << (0x00000002u & 31u));
    r1.yzw = asuint((asfloat(r0.yyy) * asfloat(asuint(ZZZRead__MainLightWorldToShadow(r1.x+1).xyz))));
    r1.yzw = asuint(mad(asfloat(asuint(ZZZRead__MainLightWorldToShadow(r1.x+0).xyz)), asfloat(r0.xxx), asfloat(r1.yzw)));
    r1.yzw = asuint(mad(asfloat(asuint(ZZZRead__MainLightWorldToShadow(r1.x+2).xyz)), asfloat(r0.zzz), asfloat(r1.yzw)));
    o4.xyz = asuint((asfloat(r1.yzw) + asfloat(asuint(ZZZRead__MainLightWorldToShadow(r1.x+3).xyz))));
    r1.x = asuint(min(asfloat(asuint((_PackedParams1.x))), asfloat(0x40800000u)));
    r1.x = asuint((int)(asfloat(r1.x)));
    r1.y = ((asint(0x00000000u) < asint(r1.x)) ? 0xffffffffu : 0u);
    if (r1.y != 0u) {
        r1.yzw = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[0].x, _AmbientLights[0].y, _AmbientLights[0].z))))));
        r2.w = asuint(dot(asfloat(r1.yzw), asfloat(r1.yzw)));
        r2.w = asuint(max(asfloat(r2.w), asfloat(0x00800000u)));
        r3.w = asuint(rsqrt(asfloat(r2.w)));
        r1.yzw = asuint((asfloat(r1.yzw) * asfloat(r3.www)));
        r4.y = asuint((asfloat(r2.w) * asfloat(asuint((_AmbientLights[2].x)))));
        r3.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[2].y)))) ? 0xffffffffu : 0u);
        r5.x = asuint((asfloat(0x3f800000u) / asfloat(r2.w)));
        r5.y = asuint((asfloat(r4.y) * asfloat(r4.y)));
        r4.x = 0x3f800000u;
        r4.xy = ((r3.ww != 0u) ? r5.xy : r4.xy);
        r2.w = ((asfloat(asuint((_AmbientLights[2].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
        r3.w = ((asfloat(r4.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
        r3.w = (r3.w & 0x3f800000u);
        r2.w = ((r2.w != 0u) ? r3.w : r4.y);
        r2.w = asuint(saturate((asfloat((r2.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
        r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
        r2.w = asuint((asfloat(r2.w) * asfloat(r4.x)));
        r1.y = asuint(dot(asfloat(asuint((float3(_AmbientLights[1].x, _AmbientLights[1].y, _AmbientLights[1].z)))), asfloat(r1.yzw)));
        r1.y = asuint(saturate(mad(asfloat(r1.y), asfloat(asuint((_AmbientLights[2].z))), asfloat(asuint((_AmbientLights[2].w))))));
        r1.y = asuint((asfloat(r1.y) * asfloat(r1.y)));
        r1.y = asuint((asfloat(r1.y) * asfloat(r2.w)));
        r1.y = asuint((asfloat(r1.y) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
        r1.yzw = asuint((asfloat(r1.yyy) * asfloat(asuint((float3(_AmbientLights[3].x, _AmbientLights[3].y, _AmbientLights[3].z))))));
        r2.w = ((asint(0x00000001u) < asint(r1.x)) ? 0xffffffffu : 0u);
        if (r2.w != 0u) {
            r4.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[4].x, _AmbientLights[4].y, _AmbientLights[4].z))))));
            r2.w = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
            r2.w = asuint(max(asfloat(r2.w), asfloat(0x00800000u)));
            r3.w = asuint(rsqrt(asfloat(r2.w)));
            r4.xyz = asuint((asfloat(r3.www) * asfloat(r4.xyz)));
            r5.y = asuint((asfloat(r2.w) * asfloat(asuint((_AmbientLights[6].x)))));
            r3.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[6].y)))) ? 0xffffffffu : 0u);
            r6.x = asuint((asfloat(0x3f800000u) / asfloat(r2.w)));
            r6.y = asuint((asfloat(r5.y) * asfloat(r5.y)));
            r5.x = 0x3f800000u;
            r5.xy = ((r3.ww != 0u) ? r6.xy : r5.xy);
            r2.w = ((asfloat(asuint((_AmbientLights[6].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
            r3.w = ((asfloat(r5.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
            r3.w = (r3.w & 0x3f800000u);
            r2.w = ((r2.w != 0u) ? r3.w : r5.y);
            r2.w = asuint(saturate((asfloat((r2.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
            r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
            r2.w = asuint((asfloat(r2.w) * asfloat(r5.x)));
            r3.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[5].x, _AmbientLights[5].y, _AmbientLights[5].z)))), asfloat(r4.xyz)));
            r3.w = asuint(saturate(mad(asfloat(r3.w), asfloat(asuint((_AmbientLights[6].z))), asfloat(asuint((_AmbientLights[6].w))))));
            r3.w = asuint((asfloat(r3.w) * asfloat(r3.w)));
            r2.w = asuint((asfloat(r2.w) * asfloat(r3.w)));
            r2.w = asuint((asfloat(r2.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
            r4.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[7].x, _AmbientLights[7].y, _AmbientLights[7].z)))), asfloat(r2.www), asfloat(r1.yzw)));
            r2.w = ((asint(0x00000002u) < asint(r1.x)) ? 0xffffffffu : 0u);
            if (r2.w != 0u) {
                r5.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[8].x, _AmbientLights[8].y, _AmbientLights[8].z))))));
                r2.w = asuint(dot(asfloat(r5.xyz), asfloat(r5.xyz)));
                r2.w = asuint(max(asfloat(r2.w), asfloat(0x00800000u)));
                r3.w = asuint(rsqrt(asfloat(r2.w)));
                r5.xyz = asuint((asfloat(r3.www) * asfloat(r5.xyz)));
                r6.y = asuint((asfloat(r2.w) * asfloat(asuint((_AmbientLights[10].x)))));
                r3.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[10].y)))) ? 0xffffffffu : 0u);
                r7.x = asuint((asfloat(0x3f800000u) / asfloat(r2.w)));
                r7.y = asuint((asfloat(r6.y) * asfloat(r6.y)));
                r6.x = 0x3f800000u;
                r6.xy = ((r3.ww != 0u) ? r7.xy : r6.xy);
                r2.w = ((asfloat(asuint((_AmbientLights[10].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                r3.w = ((asfloat(r6.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                r3.w = (r3.w & 0x3f800000u);
                r2.w = ((r2.w != 0u) ? r3.w : r6.y);
                r2.w = asuint(saturate((asfloat((r2.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
                r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
                r2.w = asuint((asfloat(r2.w) * asfloat(r6.x)));
                r3.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[9].x, _AmbientLights[9].y, _AmbientLights[9].z)))), asfloat(r5.xyz)));
                r3.w = asuint(saturate(mad(asfloat(r3.w), asfloat(asuint((_AmbientLights[10].z))), asfloat(asuint((_AmbientLights[10].w))))));
                r3.w = asuint((asfloat(r3.w) * asfloat(r3.w)));
                r2.w = asuint((asfloat(r2.w) * asfloat(r3.w)));
                r2.w = asuint((asfloat(r2.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                r5.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[11].x, _AmbientLights[11].y, _AmbientLights[11].z)))), asfloat(r2.www), asfloat(r4.xyz)));
                r1.x = ((asint(0x00000003u) < asint(r1.x)) ? 0xffffffffu : 0u);
                if (r1.x != 0u) {
                    r6.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[12].x, _AmbientLights[12].y, _AmbientLights[12].z))))));
                    r1.x = asuint(dot(asfloat(r6.xyz), asfloat(r6.xyz)));
                    r1.x = asuint(max(asfloat(r1.x), asfloat(0x00800000u)));
                    r2.w = asuint(rsqrt(asfloat(r1.x)));
                    r6.xyz = asuint((asfloat(r2.www) * asfloat(r6.xyz)));
                    r7.y = asuint((asfloat(r1.x) * asfloat(asuint((_AmbientLights[14].x)))));
                    r2.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[14].y)))) ? 0xffffffffu : 0u);
                    r8.x = asuint((asfloat(0x3f800000u) / asfloat(r1.x)));
                    r8.y = asuint((asfloat(r7.y) * asfloat(r7.y)));
                    r7.x = 0x3f800000u;
                    r7.xy = ((r2.ww != 0u) ? r8.xy : r7.xy);
                    r1.x = ((asfloat(asuint((_AmbientLights[14].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                    r2.w = ((asfloat(r7.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                    r2.w = (r2.w & 0x3f800000u);
                    r1.x = ((r1.x != 0u) ? r2.w : r7.y);
                    r1.x = asuint(saturate((asfloat((r1.x ^ 0x80000000u)) + asfloat(0x3f800000u))));
                    r1.x = asuint((asfloat(r1.x) * asfloat(r1.x)));
                    r1.x = asuint((asfloat(r1.x) * asfloat(r7.x)));
                    r2.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[13].x, _AmbientLights[13].y, _AmbientLights[13].z)))), asfloat(r6.xyz)));
                    r2.w = asuint(saturate(mad(asfloat(r2.w), asfloat(asuint((_AmbientLights[14].z))), asfloat(asuint((_AmbientLights[14].w))))));
                    r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
                    r1.x = asuint((asfloat(r1.x) * asfloat(r2.w)));
                    r1.x = asuint((asfloat(r1.x) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                    o7.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[15].x, _AmbientLights[15].y, _AmbientLights[15].z)))), asfloat(r1.xxx), asfloat(r5.xyz)));
                } else {
                    o7.xyz = r5.xyz;
                }
            } else {
                o7.xyz = r4.xyz;
            }
        } else {
            o7.xyz = r1.yzw;
        }
    } else {
        o7.xyz = uint3(0x00000000u, 0x00000000u, 0x00000000u);
    }
    o1.zw = r2.xy;
    o2.x = r2.z;
    o4.w = r0.w;
    o6.xyz = r3.xyz;
    o3.xyz = r0.xyz;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyzw);
    result.output3 = asfloat(o3.xyz);
    result.output4 = asfloat(o4.xyzw);
    result.output5 = asfloat(o5.xyzw);
    result.output6 = asfloat(o6.xyzw);
    result.output7 = asfloat(o7.xyz);
    return result;
}
