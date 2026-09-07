
    column_major float4x4 unity_MatrixVP;
    float4 _PerObjectShadowPassOffsetWS;
    float _ShadowPancaking;
    float3 _WorldSpaceCameraPos;
    float4 _ShadowBias;
    float _PerObjectShadowCasting;
    float3 _LightDirection;



    column_major float4x4 unity_ObjectToWorld;



    float _ShadowZOffset;




struct ShaderOutput {
    float4 output0 : SV_POSITION0;
};
ShaderOutput main(float3 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : TANGENT0, float4 input3 : COLOR0, float2 input4 : TEXCOORD0, float2 input5 : TEXCOORD1, float2 input6 : TEXCOORD2, float2 input7 : TEXCOORD3, float3 input8 : TEXCOORD4)
{
    uint4 r0, r1, o0, v0, v1, v2, v3, v4, v5, v6, v7, v8;
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
    r0.w = ((asfloat(asuint((_ShadowPancaking))) == asfloat(0x3f800000u)) ? 0xffffffffu : 0u);
    if (r0.w != 0u) {
        r1.xyz = asuint(mad(asfloat((asuint((float3(_ShadowZOffset, _ShadowZOffset, _ShadowZOffset))) ^ 0x80000000u)), asfloat(asuint((float3(_LightDirection.x, _LightDirection.y, _LightDirection.z)))), asfloat(r0.xyz)));
        r0.xyz = asuint(mad(asfloat(asuint((float3(_LightDirection.x, _LightDirection.y, _LightDirection.z)))), asfloat(asuint((float3(_ShadowBias.x, _ShadowBias.x, _ShadowBias.x)))), asfloat(r1.xyz)));
    }
    r1.xyz = asuint((asfloat((r0.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z))))));
    r0.w = asuint(dot(asfloat(r1.xyz), asfloat(r1.xyz)));
    r0.w = asuint(sqrt(asfloat(r0.w)));
    r0.w = asuint(sqrt(asfloat(r0.w)));
    r1.xyz = asuint((asfloat(r0.www) * asfloat(asuint((float3(_LightDirection.x, _LightDirection.y, _LightDirection.z))))));
    r0.xyz = asuint(mad(asfloat((r1.xyz ^ 0x80000000u)), asfloat(uint3(0x3c23d70au, 0x3c23d70au, 0x3c23d70au)), asfloat(r0.xyz)));
    r0.xyz = asuint(mad(asfloat((asuint((float3(_PerObjectShadowPassOffsetWS.x, _PerObjectShadowPassOffsetWS.y, _PerObjectShadowPassOffsetWS.z))) ^ 0x80000000u)), asfloat(asuint((float3(_PerObjectShadowCasting, _PerObjectShadowCasting, _PerObjectShadowCasting)))), asfloat(r0.xyz)));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    o0.z = asuint(min(asfloat(r0.w), asfloat(r0.z)));
    o0.xyw = r0.xyw;
    result.output0 = asfloat(o0.xyzw);
    return result;
}
