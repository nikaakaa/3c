
    column_major float4x4 unity_MatrixVP;
    float3 _WorldSpaceCameraPos;
    int _RenderedEntityCount;
    float _GlobalAdditionalLightIntensity;



    column_major float4x4 unity_ObjectToWorld;
    column_major float4x4 unity_WorldToObject;



    float4 _MiddlePointPosition;
    float4 _PackedParams1;
    float4 _AmbientLights[16];




struct _NapEntityGPUData_Element { uint words[32]; };
StructuredBuffer<_NapEntityGPUData_Element> _NapEntityGPUData : register(t0);
Texture2D<float4> _EyeColorMap : register(t1);
struct ShaderOutput {
    float4 output0 : TEXCOORD0;
    float4 output1 : TEXCOORD1;
    float3 output2 : TEXCOORD2;
    float output3 : TEXCOORD5;
    float3 output4 : TEXCOORD3;
    float3 output5 : TEXCOORD4;
    float4 output6 : TEXCOORD6;
    float4 output7 : TEXCOORD7;
    float3 output8 : TEXCOORD8;
    float4 output9 : SV_POSITION0;
};
ShaderOutput main(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float2 input4 : TEXCOORD0, float2 input5 : TEXCOORD1, float2 input6 : TEXCOORD2, float2 input7 : TEXCOORD3, float3 input8 : TEXCOORD4)
{
    uint4 r0, r1, r2, r3, r4, r5, r6, r7, r8, o0, o1, o2, o3, o4, o5, o6, o7, o8, v0, v1, v2, v3, v4, v5, v6, v7, v8;
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
    o8.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    r1.x = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0]))))));
    r1.y = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r1.z = asuint(dot(asfloat(v1.xyz), asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2]))))));
    r0.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
    r0.w = asuint(rsqrt(asfloat(r0.w)));
    o2.xyz = asuint((asfloat(r0.www) * asfloat(r1.xyz)));
    o3.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z))))));
    r0.w = ((asint(0x00000000u) < asint(asuint((asfloat(_RenderedEntityCount))))) ? 0xffffffffu : 0u);
    if (r0.w != 0u) {
        r1.x = asuint((int)(asfloat(asuint((_PackedParams1.z)))));
        r1.y = (asuint((asfloat(_RenderedEntityCount))) + 0xffffffffu);
        r1.x = asuint(min(asint(r1.y), asint(r1.x)));
        r1.x = asuint(max(asint(r1.x), asint(0x00000000u)));
        r1.xyz = uint3(_NapEntityGPUData[r1.x].words[(0x00000010u / 4u) + 0u], _NapEntityGPUData[r1.x].words[(0x00000010u / 4u) + 1u], _NapEntityGPUData[r1.x].words[(0x00000010u / 4u) + 2u]);
    } else {
        r1.xyz = uint3(0x00000000u, 0x00000000u, 0x00000000u);
    }
    r1.xyz = asuint((asfloat(r1.xyz) + asfloat((asuint((float3(_MiddlePointPosition.x, _MiddlePointPosition.y, _MiddlePointPosition.z))) ^ 0x80000000u))));
    r1.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r1.xyz = asuint((asfloat(r1.www) * asfloat(r1.xyz)));
    r1.w = asuint(dot(asfloat((asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]))) ^ 0x80000000u)), asfloat((asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]))) ^ 0x80000000u))));
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r2.xyz = asuint((asfloat(r1.www) * asfloat((asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]))) ^ 0x80000000u))));
    r1.w = asuint(dot(asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1])))), asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r1.w = asuint(max(asfloat(r1.w), asfloat(0x00800000u)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r3.xy = asuint((asfloat(r1.ww) * asfloat(asuint((float2(unity_ObjectToWorld[0][1], unity_ObjectToWorld[2][1]))))));
    r1.w = asuint(dot(asfloat(r2.xyz), asfloat(r1.xyz)));
    r1.w = asuint((asfloat((r1.w & 0x7fffffffu)) * asfloat(0x3f000000u)));
    r1.xz = asuint(mad(asfloat(r1.ww), asfloat(r3.xy), asfloat(r1.xz)));
    r1.y = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.y = asuint(rsqrt(asfloat(r1.y)));
    r1.xz = asuint((asfloat(r1.yy) * asfloat(r1.xz)));
    r1.y = 0x3727c5acu;
    r1.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r1.xyz = asuint((asfloat(r1.www) * asfloat(r1.xyz)));
    r2.xyz = asuint((asfloat(r1.yyy) * asfloat(asuint((float3(unity_WorldToObject[0][1], unity_WorldToObject[1][1], unity_WorldToObject[2][1]))))));
    r1.xyw = asuint(mad(asfloat(asuint((float3(unity_WorldToObject[0][0], unity_WorldToObject[1][0], unity_WorldToObject[2][0])))), asfloat(r1.xxx), asfloat(r2.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_WorldToObject[0][2], unity_WorldToObject[1][2], unity_WorldToObject[2][2])))), asfloat(r1.zzz), asfloat(r1.xyw)));
    r1.x = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r1.x = asuint(rsqrt(asfloat(r1.x)));
    r1.xy = asuint((asfloat(r1.xx) * asfloat(r1.zy)));
    r1.z = asuint(min(asfloat((r1.y & 0x7fffffffu)), asfloat((r1.x & 0x7fffffffu))));
    r1.w = asuint(max(asfloat((r1.y & 0x7fffffffu)), asfloat((r1.x & 0x7fffffffu))));
    r1.w = asuint((asfloat(0x3f800000u) / asfloat(r1.w)));
    r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
    r1.w = asuint((asfloat(r1.z) * asfloat(r1.z)));
    r2.x = asuint(mad(asfloat(r1.w), asfloat(0x3caaae5fu), asfloat(0xbdae5a36u)));
    r2.x = asuint(mad(asfloat(r1.w), asfloat(r2.x), asfloat(0x3e3876e2u)));
    r2.x = asuint(mad(asfloat(r1.w), asfloat(r2.x), asfloat(0xbea91d04u)));
    r1.w = asuint(mad(asfloat(r1.w), asfloat(r2.x), asfloat(0x3f7ff738u)));
    r2.x = asuint((asfloat(r1.w) * asfloat(r1.z)));
    r2.y = ((asfloat((r1.y & 0x7fffffffu)) < asfloat((r1.x & 0x7fffffffu))) ? 0xffffffffu : 0u);
    r2.x = asuint(mad(asfloat(r2.x), asfloat(0xc0000000u), asfloat(0x3fc90fdbu)));
    r2.x = (r2.y & r2.x);
    r1.z = asuint(mad(asfloat(r1.z), asfloat(r1.w), asfloat(r2.x)));
    r1.w = ((asfloat(r1.y) < asfloat((r1.y ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.w = (r1.w & 0xc0490fdbu);
    r1.z = asuint((asfloat(r1.w) + asfloat(r1.z)));
    r1.w = asuint(min(asfloat(r1.y), asfloat((r1.x ^ 0x80000000u))));
    r1.x = asuint(max(asfloat(r1.y), asfloat((r1.x ^ 0x80000000u))));
    r1.y = ((asfloat(r1.w) < asfloat((r1.w ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.x = ((asfloat(r1.x) >= asfloat((r1.x ^ 0x80000000u))) ? 0xffffffffu : 0u);
    r1.x = (r1.x & r1.y);
    r1.x = ((r1.x != 0u) ? (r1.z ^ 0x80000000u) : r1.z);
    o0.w = asuint((asfloat(r1.x) * asfloat(0x3ea2f983u)));
    r1.y = asuint((asfloat(v3.x) * asfloat(0x437f0000u)));
    r1.z = asuint(mad(asfloat(v3.z), asfloat(0x437f0000u), asfloat(0x3f000000u)));
    r1.z = (uint)(asfloat(r1.z));
    r1.zw = (r1.zz & uint2(0x00000010u, 0x00000003u));
    r1.x = ((asfloat(0x00000000u) < asfloat(r1.x)) ? 0xffffffffu : 0u);
    r2.x = asuint((asfloat((v4.x ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r2.z = v4.x;
    r2.yw = v7.yx;
    r2.xy = ((r1.xx != 0u) ? r2.xy : r2.zw);
    r1.xz = asuint((float2)(r1.zw));
    o2.w = asuint(mad(asfloat((r1.z ^ 0x80000000u)), asfloat(0x3e4ccccdu), asfloat(0x3f666666u)));
    if (r0.w != 0u) {
        r0.w = asuint((int)(asfloat(asuint((_PackedParams1.z)))));
        r1.z = (asuint((asfloat(_RenderedEntityCount))) + 0xffffffffu);
        r0.w = asuint(min(asint(r0.w), asint(r1.z)));
        r0.w = asuint(max(asint(r0.w), asint(0x00000000u)));
        r0.w = _NapEntityGPUData[r0.w].words[(0x00000034u / 4u) + 0u];
    } else {
        r0.w = 0x00000000u;
    }
    o6.x = asuint((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u)));
    r0.w = asuint(min(asfloat(asuint((_PackedParams1.x))), asfloat(0x40800000u)));
    r0.w = asuint((int)(asfloat(r0.w)));
    r1.z = ((asint(0x00000000u) < asint(r0.w)) ? 0xffffffffu : 0u);
    if (r1.z != 0u) {
        r3.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[0].x, _AmbientLights[0].y, _AmbientLights[0].z))))));
        r1.z = asuint(dot(asfloat(r3.xyz), asfloat(r3.xyz)));
        r1.z = asuint(max(asfloat(r1.z), asfloat(0x00800000u)));
        r1.w = asuint(rsqrt(asfloat(r1.z)));
        r3.xyz = asuint((asfloat(r1.www) * asfloat(r3.xyz)));
        r4.y = asuint((asfloat(r1.z) * asfloat(asuint((_AmbientLights[2].x)))));
        r1.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[2].y)))) ? 0xffffffffu : 0u);
        r5.x = asuint((asfloat(0x3f800000u) / asfloat(r1.z)));
        r5.y = asuint((asfloat(r4.y) * asfloat(r4.y)));
        r4.x = 0x3f800000u;
        r1.zw = ((r1.ww != 0u) ? r5.xy : r4.xy);
        r2.z = ((asfloat(asuint((_AmbientLights[2].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
        r2.w = ((asfloat(r1.w) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
        r2.w = (r2.w & 0x3f800000u);
        r1.w = ((r2.z != 0u) ? r2.w : r1.w);
        r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
        r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
        r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
        r1.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[1].x, _AmbientLights[1].y, _AmbientLights[1].z)))), asfloat(r3.xyz)));
        r1.w = asuint(saturate(mad(asfloat(r1.w), asfloat(asuint((_AmbientLights[2].z))), asfloat(asuint((_AmbientLights[2].w))))));
        r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
        r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
        r1.z = asuint((asfloat(r1.z) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
        r3.xyz = asuint((asfloat(r1.zzz) * asfloat(asuint((float3(_AmbientLights[3].x, _AmbientLights[3].y, _AmbientLights[3].z))))));
        r1.z = ((asint(0x00000001u) < asint(r0.w)) ? 0xffffffffu : 0u);
        if (r1.z != 0u) {
            r4.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[4].x, _AmbientLights[4].y, _AmbientLights[4].z))))));
            r1.z = asuint(dot(asfloat(r4.xyz), asfloat(r4.xyz)));
            r1.z = asuint(max(asfloat(r1.z), asfloat(0x00800000u)));
            r1.w = asuint(rsqrt(asfloat(r1.z)));
            r4.xyz = asuint((asfloat(r1.www) * asfloat(r4.xyz)));
            r5.y = asuint((asfloat(r1.z) * asfloat(asuint((_AmbientLights[6].x)))));
            r1.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[6].y)))) ? 0xffffffffu : 0u);
            r6.x = asuint((asfloat(0x3f800000u) / asfloat(r1.z)));
            r6.y = asuint((asfloat(r5.y) * asfloat(r5.y)));
            r5.x = 0x3f800000u;
            r1.zw = ((r1.ww != 0u) ? r6.xy : r5.xy);
            r2.z = ((asfloat(asuint((_AmbientLights[6].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
            r2.w = ((asfloat(r1.w) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
            r2.w = (r2.w & 0x3f800000u);
            r1.w = ((r2.z != 0u) ? r2.w : r1.w);
            r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
            r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
            r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
            r1.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[5].x, _AmbientLights[5].y, _AmbientLights[5].z)))), asfloat(r4.xyz)));
            r1.w = asuint(saturate(mad(asfloat(r1.w), asfloat(asuint((_AmbientLights[6].z))), asfloat(asuint((_AmbientLights[6].w))))));
            r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
            r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
            r1.z = asuint((asfloat(r1.z) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
            r4.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[7].x, _AmbientLights[7].y, _AmbientLights[7].z)))), asfloat(r1.zzz), asfloat(r3.xyz)));
            r1.z = ((asint(0x00000002u) < asint(r0.w)) ? 0xffffffffu : 0u);
            if (r1.z != 0u) {
                r5.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[8].x, _AmbientLights[8].y, _AmbientLights[8].z))))));
                r1.z = asuint(dot(asfloat(r5.xyz), asfloat(r5.xyz)));
                r1.z = asuint(max(asfloat(r1.z), asfloat(0x00800000u)));
                r1.w = asuint(rsqrt(asfloat(r1.z)));
                r5.xyz = asuint((asfloat(r1.www) * asfloat(r5.xyz)));
                r6.y = asuint((asfloat(r1.z) * asfloat(asuint((_AmbientLights[10].x)))));
                r1.w = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[10].y)))) ? 0xffffffffu : 0u);
                r7.x = asuint((asfloat(0x3f800000u) / asfloat(r1.z)));
                r7.y = asuint((asfloat(r6.y) * asfloat(r6.y)));
                r6.x = 0x3f800000u;
                r1.zw = ((r1.ww != 0u) ? r7.xy : r6.xy);
                r2.z = ((asfloat(asuint((_AmbientLights[10].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                r2.w = ((asfloat(r1.w) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                r2.w = (r2.w & 0x3f800000u);
                r1.w = ((r2.z != 0u) ? r2.w : r1.w);
                r1.w = asuint(saturate((asfloat((r1.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
                r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
                r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
                r1.w = asuint(dot(asfloat(asuint((float3(_AmbientLights[9].x, _AmbientLights[9].y, _AmbientLights[9].z)))), asfloat(r5.xyz)));
                r1.w = asuint(saturate(mad(asfloat(r1.w), asfloat(asuint((_AmbientLights[10].z))), asfloat(asuint((_AmbientLights[10].w))))));
                r1.w = asuint((asfloat(r1.w) * asfloat(r1.w)));
                r1.z = asuint((asfloat(r1.w) * asfloat(r1.z)));
                r1.z = asuint((asfloat(r1.z) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                r5.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[11].x, _AmbientLights[11].y, _AmbientLights[11].z)))), asfloat(r1.zzz), asfloat(r4.xyz)));
                r0.w = ((asint(0x00000003u) < asint(r0.w)) ? 0xffffffffu : 0u);
                if (r0.w != 0u) {
                    r6.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_AmbientLights[12].x, _AmbientLights[12].y, _AmbientLights[12].z))))));
                    r0.w = asuint(dot(asfloat(r6.xyz), asfloat(r6.xyz)));
                    r0.w = asuint(max(asfloat(r0.w), asfloat(0x00800000u)));
                    r1.z = asuint(rsqrt(asfloat(r0.w)));
                    r6.xyz = asuint((asfloat(r1.zzz) * asfloat(r6.xyz)));
                    r7.y = asuint((asfloat(r0.w) * asfloat(asuint((_AmbientLights[14].x)))));
                    r1.z = ((asfloat(0xbf000000u) < asfloat(asuint((_AmbientLights[14].y)))) ? 0xffffffffu : 0u);
                    r8.x = asuint((asfloat(0x3f800000u) / asfloat(r0.w)));
                    r8.y = asuint((asfloat(r7.y) * asfloat(r7.y)));
                    r7.x = 0x3f800000u;
                    r1.zw = ((r1.zz != 0u) ? r8.xy : r7.xy);
                    r0.w = ((asfloat(asuint((_AmbientLights[14].y))) < asfloat(0xbfc00000u)) ? 0xffffffffu : 0u);
                    r2.z = ((asfloat(r1.w) >= asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
                    r2.z = (r2.z & 0x3f800000u);
                    r0.w = ((r0.w != 0u) ? r2.z : r1.w);
                    r0.w = asuint(saturate((asfloat((r0.w ^ 0x80000000u)) + asfloat(0x3f800000u))));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r0.w)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r1.z)));
                    r1.z = asuint(dot(asfloat(asuint((float3(_AmbientLights[13].x, _AmbientLights[13].y, _AmbientLights[13].z)))), asfloat(r6.xyz)));
                    r1.z = asuint(saturate(mad(asfloat(r1.z), asfloat(asuint((_AmbientLights[14].z))), asfloat(asuint((_AmbientLights[14].w))))));
                    r1.z = asuint((asfloat(r1.z) * asfloat(r1.z)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(r1.z)));
                    r0.w = asuint((asfloat(r0.w) * asfloat(asuint((_GlobalAdditionalLightIntensity)))));
                    o7.xyz = asuint(mad(asfloat(asuint((float3(_AmbientLights[15].x, _AmbientLights[15].y, _AmbientLights[15].z)))), asfloat(r0.www), asfloat(r5.xyz)));
                } else {
                    o7.xyz = r5.xyz;
                }
            } else {
                o7.xyz = r4.xyz;
            }
        } else {
            o7.xyz = r3.xyz;
        }
    } else {
        o7.xyz = uint3(0x00000000u, 0x00000000u, 0x00000000u);
    }
    r0.w = (uint)(asfloat(r1.y));
    r3.x = (r0.w & 0x0000000fu);
    r0.w = (r0.w >> (0x00000004u & 31u));
    r3.y = ((0u - r0.w) + 0x0000000fu);
    r3.zw = uint2(0x00000000u, 0x00000000u);
    r3.xyzw = asuint(_EyeColorMap.Load(asint(r3.xyw)).xyzw);
    o0.xy = v4.xy;
    o0.z = r2.x;
    o1.xy = v6.xy;
    o1.zw = v7.xy;
    o5.xyzw = asuint((asfloat(r3.xyzw) * asfloat(uint4(0x40000000u, 0x40000000u, 0x40000000u, 0x3f800000u))));
    o6.y = r1.x;
    o6.z = r2.y;
    o6.w = v3.w;
    o4.xyz = r0.xyz;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.xyzw);
    result.output2 = asfloat(o2.xyz);
    result.output3 = asfloat(o2.w);
    result.output4 = asfloat(o3.xyz);
    result.output5 = asfloat(o4.xyz);
    result.output6 = asfloat(o5.xyzw);
    result.output7 = asfloat(o6.xyzw);
    result.output8 = asfloat(o7.xyz);
    result.output9 = asfloat(o8.xyzw);
    return result;
}
