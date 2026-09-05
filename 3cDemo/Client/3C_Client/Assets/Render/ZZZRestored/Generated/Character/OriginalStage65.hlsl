
    column_major float4x4 unity_MatrixVP;
    column_major float4x4 _PrevViewProjMatrix;
    column_major float4x4 _NonJitteredViewProjMatrix;
    int _RenderedEntityCount;
    float _GlobalAdditionalLightIntensity;



    column_major float4x4 unity_ObjectToWorld;
    column_major float4x4 unity_WorldToObject;
    column_major float4x4 unity_MatrixPreviousM;
    float4 unity_WorldTransformParams;
    float4 unity_MotionVectorsParams;



    float4 _PackedParams1;
    float4 _AmbientLights[16];




struct _NapEntityGPUData_Element { uint words[32]; };
StructuredBuffer<_NapEntityGPUData_Element> _NapEntityGPUData : register(t0);
struct ShaderOutput {
    float4 o0 : TEXCOORD0;
    float4 o1 : TEXCOORD1;
    float4 o2 : TEXCOORD2;
    float4 o3 : TEXCOORD3;
    float4 o4 : TEXCOORD4;
    float4 o5 : TEXCOORD5;
    float4 o6 : TEXCOORD6;
    float4 o7 : TEXCOORD7;
    float3 o8 : TEXCOORD8;
    float4 o9 : SV_POSITION0;
};
ShaderOutput main(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float2 input4 : TEXCOORD0, float2 input5 : TEXCOORD1, float2 input6 : TEXCOORD2, float2 input7 : TEXCOORD3, float3 input8 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, o1, o2, o3, o4, o5, o6, o7, o8, o9, v0, v1, v2, v3, v4, v5, v6, v7, v8;
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
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o9.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    r0.w = asuint((asfloat(v2.w) * asfloat(asuint((unity_WorldTransformParams.w)))));
    r1.x = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r1.y = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r1.z = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r1.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r1.xyz = asuint((asfloat(r1.www) * asfloat(r1.xyz)));
    r2.xyz = asuint((asfloat(v2.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v2.xxx), asfloat(r2.xyz)));
    r2.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v2.zzz), asfloat(r2.xyz)));
    r1.w = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r2.xyz = asuint((asfloat(r1.www) * asfloat(r2.xyz)));
    r3.xyz = asuint((asfloat(r1.zxy) * asfloat(r2.yzx)));
    r3.xyz = asuint(mad(asfloat(r1.yzx), asfloat(r2.zxy), asfloat((r3.xyz ^ 0x80000000u))));
    o4.xyz = asuint((asfloat(r0.www) * asfloat(r3.xyz)));
    r0.w = asuint(mad(asfloat(v3.z), asfloat(0x437f0000u), asfloat(0x3f000000u)));
    r0.w = asuint((int)(asfloat(r0.w)));
    r0.w = asuint((asint(r0.w) >> (0x00000005u & 31u)));
    r0.w = (r0.w & 0x00000001u);
    r0.w = asuint((float)(asint(r0.w)));
    r0.w = ((asfloat(0x00000000u) != asfloat(r0.w)) ? 0xffffffffu : 0u);
    o7.y = ((r0.w != 0u) ? 0x00000000u : v3.w);
    o7.z = (r0.w & 0x3f800000u);
    r3.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]))))));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0])))), asfloat(v0.xxxx), asfloat(r3.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2])))), asfloat(v0.zzzz), asfloat(r3.xyzw)));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat(asuint((float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]))))));
    r4.xyzw = asuint((asfloat(r3.yyyy) * asfloat(asuint((float4(_NonJitteredViewProjMatrix[0][1], _NonJitteredViewProjMatrix[1][1], _NonJitteredViewProjMatrix[2][1], _NonJitteredViewProjMatrix[3][1]))))));
    r4.xyzw = asuint(mad(asfloat(asuint((float4(_NonJitteredViewProjMatrix[0][0], _NonJitteredViewProjMatrix[1][0], _NonJitteredViewProjMatrix[2][0], _NonJitteredViewProjMatrix[3][0])))), asfloat(r3.xxxx), asfloat(r4.xyzw)));
    r4.xyzw = asuint(mad(asfloat(asuint((float4(_NonJitteredViewProjMatrix[0][2], _NonJitteredViewProjMatrix[1][2], _NonJitteredViewProjMatrix[2][2], _NonJitteredViewProjMatrix[3][2])))), asfloat(r3.zzzz), asfloat(r4.xyzw)));
    o5.xyzw = asuint(mad(asfloat(asuint((float4(_NonJitteredViewProjMatrix[0][3], _NonJitteredViewProjMatrix[1][3], _NonJitteredViewProjMatrix[2][3], _NonJitteredViewProjMatrix[3][3])))), asfloat(r3.wwww), asfloat(r4.xyzw)));
    r0.w = ((asfloat(0x00000000u) < asfloat(asuint((unity_MotionVectorsParams.x)))) ? 0xffffffffu : 0u);
    r3.xyz = ((r0.www != 0u) ? v8.xyz : v0.xyz);
    r4.xyzw = asuint((asfloat(r3.yyyy) * asfloat(asuint((float4(unity_MatrixPreviousM[0][1], unity_MatrixPreviousM[1][1], unity_MatrixPreviousM[2][1], unity_MatrixPreviousM[3][1]))))));
    r4.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][0], unity_MatrixPreviousM[1][0], unity_MatrixPreviousM[2][0], unity_MatrixPreviousM[3][0])))), asfloat(r3.xxxx), asfloat(r4.xyzw)));
    r3.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixPreviousM[0][2], unity_MatrixPreviousM[1][2], unity_MatrixPreviousM[2][2], unity_MatrixPreviousM[3][2])))), asfloat(r3.zzzz), asfloat(r4.xyzw)));
    r3.xyzw = asuint((asfloat(r3.xyzw) + asfloat(asuint((float4(unity_MatrixPreviousM[0][3], unity_MatrixPreviousM[1][3], unity_MatrixPreviousM[2][3], unity_MatrixPreviousM[3][3]))))));
    r4.xyzw = asuint((asfloat(r3.yyyy) * asfloat(asuint((float4(_PrevViewProjMatrix[0][1], _PrevViewProjMatrix[1][1], _PrevViewProjMatrix[2][1], _PrevViewProjMatrix[3][1]))))));
    r4.xyzw = asuint(mad(asfloat(asuint((float4(_PrevViewProjMatrix[0][0], _PrevViewProjMatrix[1][0], _PrevViewProjMatrix[2][0], _PrevViewProjMatrix[3][0])))), asfloat(r3.xxxx), asfloat(r4.xyzw)));
    r4.xyzw = asuint(mad(asfloat(asuint((float4(_PrevViewProjMatrix[0][2], _PrevViewProjMatrix[1][2], _PrevViewProjMatrix[2][2], _PrevViewProjMatrix[3][2])))), asfloat(r3.zzzz), asfloat(r4.xyzw)));
    o6.xyzw = asuint(mad(asfloat(asuint((float4(_PrevViewProjMatrix[0][3], _PrevViewProjMatrix[1][3], _PrevViewProjMatrix[2][3], _PrevViewProjMatrix[3][3])))), asfloat(r3.wwww), asfloat(r4.xyzw)));
    r0.w = ((asint(0x00000000u) < asint(asuint((asfloat(_RenderedEntityCount))))) ? 0xffffffffu : 0u);
    if (r0.w != 0u) {
        r0.w = asuint((int)(asfloat(asuint((_PackedParams1.z)))));
        r1.w = (asuint((asfloat(_RenderedEntityCount))) + 0xffffffffu);
        r0.w = asuint(min(asint(r0.w), asint(r1.w)));
        r0.w = asuint(max(asint(r0.w), asint(0x00000000u)));
        r0.w = _NapEntityGPUData[r0.w].words[(0x00000034u / 4u) + 0u];
    } else {
        r0.w = 0x00000000u;
    }
    o7.x = asuint(mad(asfloat(r0.w), asfloat((v3.w ^ 0x80000000u)), asfloat(0x3f800000u)));
    r0.w = asuint(min(asfloat(asuint((_PackedParams1.x))), asfloat(0x40800000u)));
    r0.w = asuint((int)(asfloat(r0.w)));
    r1.w = ((asint(0x00000000u) < asint(r0.w)) ? 0xffffffffu : 0u);
    if (r1.w != 0u) {
        r3.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[0].x, _AmbientLights[0].y, _AmbientLights[0].z))))));
        r1.w = asuint(dot(asfloat(r3.xyz), asfloat(r3.xyz)));
        r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
        r2.w = asuint(rsqrt(asfloat(r1.w)));
        r3.xyz = asuint((asfloat(r2.www) * asfloat(r3.xyz)));
        r4.y = asuint((asfloat(r1.w) * asfloat(asuint((_AmbientLights[2].x)))));
        r2.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[2].y)))) ? 0xffffffffu : 0u);
        r5.x = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
        r5.y = asuint((asfloat(r4.y) * asfloat(r4.y)));
        r4.x = 0x3f800000u;
        r4.xy = ((r2.ww != 0u) ? r5.xy : r4.xy);
        r1.w = ((asfloat(asuint((_AmbientLights[2].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
        r2.w = ((asfloat(r4.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
        r2.w = (r2.w & 0x3f800000u);
        r1.w = ((r1.w != 0u) ? r2.w : r4.y);
        r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
        r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
        r1.w = asuint((asfloat(r1.w) * asfloat(r4.x)));
        r2.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[1].x, _AmbientLights[1].y, _AmbientLights[1].z)))), asfloat(r3.xyz)));
        r2.w = asuint(saturate(mad(asfloat(r2.w), asfloat(asuint((_AmbientLights[2].z))), asfloat(asuint((_AmbientLights[2].w))))));
        r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
        r1.w = asuint((asfloat(r1.w) * asfloat(r2.w)));
        r1.w = asuint((asfloat(r1.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
        r3.xyz = asuint((asfloat(r1.www) * asfloat(asuint((float3(_AmbientLights[3].x, _AmbientLights[3].y, _AmbientLights[3].z))))));
        r1.w = ((asint(0x00000001u) < asint(r0.w)) ? 0xffffffffu : 0u);
        if (r1.w != 0u) {
            r4.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[4].x, _AmbientLights[4].y, _AmbientLights[4].z))))));
            r1.w = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
            r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
            r2.w = asuint(rsqrt(asfloat(r1.w)));
            r4.xyz = asuint((asfloat(r2.www) * asfloat(r4.xyz)));
            r5.y = asuint((asfloat(r1.w) * asfloat(asuint((_AmbientLights[6].x)))));
            r2.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[6].y)))) ? 0xffffffffu : 0u);
            r6.x = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
            r6.y = asuint((asfloat(r5.y) * asfloat(r5.y)));
            r5.x = 0x3f800000u;
            r5.xy = ((r2.ww != 0u) ? r6.xy : r5.xy);
            r1.w = ((asfloat(asuint((_AmbientLights[6].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
            r2.w = ((asfloat(r5.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
            r2.w = (r2.w & 0x3f800000u);
            r1.w = ((r1.w != 0u) ? r2.w : r5.y);
            r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
            r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
            r1.w = asuint((asfloat(r1.w) * asfloat(r5.x)));
            r2.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[5].x, _AmbientLights[5].y, _AmbientLights[5].z)))), asfloat(r4.xyz)));
            r2.w = asuint(saturate(mad(asfloat(r2.w), asfloat(asuint((_AmbientLights[6].z))), asfloat(asuint((_AmbientLights[6].w))))));
            r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
            r1.w = asuint((asfloat(r1.w) * asfloat(r2.w)));
            r1.w = asuint((asfloat(r1.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
            r4.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[7].x, _AmbientLights[7].y, _AmbientLights[7].z)))), asfloat(r1.www), asfloat(r3.xyz)));
            r1.w = ((asint(0x00000002u) < asint(r0.w)) ? 0xffffffffu : 0u);
            if (r1.w != 0u) {
                r5.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[8].x, _AmbientLights[8].y, _AmbientLights[8].z))))));
                r1.w = asuint(dot(asfloat(r5.xyz), asfloat(r5.xyz)));
                r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
                r2.w = asuint(rsqrt(asfloat(r1.w)));
                r5.xyz = asuint((asfloat(r2.www) * asfloat(r5.xyz)));
                r6.y = asuint((asfloat(r1.w) * asfloat(asuint((_AmbientLights[10].x)))));
                r2.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[10].y)))) ? 0xffffffffu : 0u);
                r7.x = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
                r7.y = asuint((asfloat(r6.y) * asfloat(r6.y)));
                r6.x = 0x3f800000u;
                r6.xy = ((r2.ww != 0u) ? r7.xy : r6.xy);
                r1.w = ((asfloat(asuint((_AmbientLights[10].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                r2.w = ((asfloat(r6.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                r2.w = (r2.w & 0x3f800000u);
                r1.w = ((r1.w != 0u) ? r2.w : r6.y);
                r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
                r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
                r1.w = asuint((asfloat(r1.w) * asfloat(r6.x)));
                r2.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[9].x, _AmbientLights[9].y, _AmbientLights[9].z)))), asfloat(r5.xyz)));
                r2.w = asuint(saturate(mad(asfloat(r2.w), asfloat(asuint((_AmbientLights[10].z))), asfloat(asuint((_AmbientLights[10].w))))));
                r2.w = asuint((asfloat(r2.w) * asfloat(r2.w)));
                r1.w = asuint((asfloat(r1.w) * asfloat(r2.w)));
                r1.w = asuint((asfloat(r1.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                r5.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[11].x, _AmbientLights[11].y, _AmbientLights[11].z)))), asfloat(r1.www), asfloat(r4.xyz)));
                r0.w = ((asint(0x00000003u) < asint(r0.w)) ? 0xffffffffu : 0u);
                if (r0.w != 0u) {
                    r6.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[12].x, _AmbientLights[12].y, _AmbientLights[12].z))))));
                    r0.w = asuint(dot(asfloat(r6.xyz), asfloat(r6.xyz)));
                    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
                    r1.w = asuint(rsqrt(asfloat(r0.w)));
                    r6.xyz = asuint((asfloat(r1.www) * asfloat(r6.xyz)));
                    r7.y = asuint((asfloat(r0.w) * asfloat(asuint((_AmbientLights[14].x)))));
                    r1.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[14].y)))) ? 0xffffffffu : 0u);
                    r8.x = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
                    r8.y = asuint((asfloat(r7.y) * asfloat(r7.y)));
                    r7.x = 0x3f800000u;
                    r7.xy = ((r1.ww != 0u) ? r8.xy : r7.xy);
                    r0.w = ((asfloat(asuint((_AmbientLights[14].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                    r1.w = ((asfloat(r7.y) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                    r1.w = (r1.w & 0x3f800000u);
                    r0.w = ((r0.w != 0u) ? r1.w : r7.y);
                    r0.w = asuint(saturate((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r7.x)));
                    r1.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[13].x, _AmbientLights[13].y, _AmbientLights[13].z)))), asfloat(r6.xyz)));
                    r1.w = asuint(saturate(mad(asfloat(r1.w), asfloat(asuint((_AmbientLights[14].z))), asfloat(asuint((_AmbientLights[14].w))))));
                    r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r1.w)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                    o8.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[15].x, _AmbientLights[15].y, _AmbientLights[15].z)))), asfloat(r0.www), asfloat(r5.xyz)));
                } else {
                    o8.xyz = r5.xyz;
                }
            } else {
                o8.xyz = r4.xyz;
            }
        } else {
            o8.xyz = r3.xyz;
        }
    } else {
        o8.xyz = uint3(0x00000000u, 0x00000000u, 0x00000000u);
    }
    o0.xy = v4.xy;
    o0.zw = v7.xy;
    o1.xy = v6.xy;
    o1.zw = uint2(0x00000000u, 0x00000000u);
    o2.w = r0.x;
    o2.xyz = r1.xyz;
    o3.w = r0.y;
    o3.xyz = r2.xyz;
    o4.w = r0.z;
    o7.w = 0x00000000u;
    result.o0 = asfloat(o0.xyzw);
    result.o1 = asfloat(o1.xyzw);
    result.o2 = asfloat(o2.xyzw);
    result.o3 = asfloat(o3.xyzw);
    result.o4 = asfloat(o4.xyzw);
    result.o5 = asfloat(o5.xyzw);
    result.o6 = asfloat(o6.xyzw);
    result.o7 = asfloat(o7.xyzw);
    result.o8 = asfloat(o8.xyz);
    result.o9 = asfloat(o9.xyzw);
    return result;
}
