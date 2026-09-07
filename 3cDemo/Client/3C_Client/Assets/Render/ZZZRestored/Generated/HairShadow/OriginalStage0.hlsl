
    column_major float4x4 unity_MatrixVP;
    float3 _WorldSpaceCameraPos;
    float4 _AvatarMainLightPosition;
    float _StencilShadowBlendDebugMode;
    float4 _ClipSpaceOffset;



    column_major float4x4 unity_ObjectToWorld;



    float _Length;
    float _XOffsetNew;
    float _YOffset;
    float _ZOffset;
    float _XOffsetEnter;
    float _YOffsetEnter;



    float4 _PackedParams0;





struct ShaderOutput {
    float4 output0 : SV_POSITION0;
    float output1 : TEXCOORD0;
};
ShaderOutput main(float4 input0 : POSITION0, float3 input1 : NORMAL0, float4 input2 : COLOR0)
{
    uint4 r0, r1, r2, o0, o1, v0, v1, v2;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xyz = asuint(input1);
    v2.xyzw = asuint(input2);
    r0.x = asuint(dot(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2]))))));
    r0.x = asuint(max(asfloat(r0.x), asfloat(0x00800000u)));
    r0.x = asuint(rsqrt(asfloat(r0.x)));
    r0.xyz = asuint((asfloat(r0.xxx) * asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2]))))));
    r0.w = ((asfloat(0x00000000u) < asfloat(asuint((_PackedParams0.w)))) ? 0xffffffffu : 0u);
    r1.xyz = ((r0.www != 0u) ? asuint((float3(_PackedParams0.x, _PackedParams0.y, _PackedParams0.z))) : asuint((float3(_AvatarMainLightPosition.x, _AvatarMainLightPosition.y, _AvatarMainLightPosition.z))));
    r0.w = asuint(dot(asfloat(r1.xyz), asfloat(r0.xyz)));
    r0.x = asuint(dot(asfloat((r0.xyz ^ 0x80000000u)), asfloat(r1.xyz)));
    r0.y = asuint((asfloat(r0.w) * asfloat(v2.x)));
    r0.yzw = asuint((asfloat(r1.xyz) * asfloat(r0.yyy)));
    r1.xyz = asuint((asfloat(v0.yyy) * asfloat(asuint((float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]))))));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0])))), asfloat(v0.xxx), asfloat(r1.xyz)));
    r1.xyz = asuint(mad(asfloat(asuint((float3(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2])))), asfloat(v0.zzz), asfloat(r1.xyz)));
    r1.xyz = asuint((asfloat(r1.xyz) + asfloat(asuint((float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]))))));
    r2.xyz = asuint((asfloat((r1.xyz ^ 0x80000000u)) + asfloat(asuint((float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z))))));
    r1.w = asuint(dot(asfloat(r2.xyz), asfloat(r2.xyz)));
    r1.w = asuint(rsqrt(asfloat(r1.w)));
    r2.xyz = asuint((asfloat(r1.www) * asfloat(r2.xyz)));
    r2.xyz = asuint((asfloat(r2.xyz) * asfloat(asuint((float3(_ZOffset, _ZOffset, _ZOffset))))));
    r1.xyz = asuint(mad(asfloat(r2.xyz), asfloat(v2.xxx), asfloat(r1.xyz)));
    r0.yzw = asuint(mad(asfloat(r0.yzw), asfloat(uint3(0x41000000u, 0x41000000u, 0x41000000u)), asfloat(r1.xyz)));
    r2.xy = asuint((asfloat(r0.zz) * asfloat(asuint((float2(unity_MatrixVP[0][1], unity_MatrixVP[1][1]))))));
    r0.yz = asuint(mad(asfloat(asuint((float2(unity_MatrixVP[0][0], unity_MatrixVP[1][0])))), asfloat(r0.yy), asfloat(r2.xy)));
    r0.yz = asuint(mad(asfloat(asuint((float2(unity_MatrixVP[0][2], unity_MatrixVP[1][2])))), asfloat(r0.ww), asfloat(r0.yz)));
    r0.yz = asuint((asfloat(r0.yz) + asfloat(asuint((float2(unity_MatrixVP[0][3], unity_MatrixVP[1][3]))))));
    r2.xyzw = asuint((asfloat(r1.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r2.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r1.xxxx), asfloat(r2.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r1.zzzz), asfloat(r2.xyzw)));
    r1.xyzw = asuint((asfloat(r1.xyzw) + asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]))))));
    r0.yz = asuint((asfloat(r0.yz) + asfloat((r1.xy ^ 0x80000000u))));
    r2.xy = ((asfloat(uint2(0x00000000u, 0x00000000u)) < asfloat(r0.yz)) ? 0xffffffffu : 0u);
    r2.zw = ((asfloat(r0.yz) < asfloat(uint2(0x00000000u, 0x00000000u))) ? 0xffffffffu : 0u);
    r0.yz = asuint((asfloat(r0.yz) * asfloat(uint2(0x3dcccccdu, 0x3dcccccdu))));
    r0.yz = asuint(exp2(asfloat(((r0.yz & 0x7fffffffu) ^ 0x80000000u))));
    r0.yz = asuint((asfloat((r0.yz ^ 0x80000000u)) + asfloat(uint2(0x3f800000u, 0x3f800000u))));
    r2.xy = ((0u - r2.xy) + r2.zw);
    r2.xy = asuint((float2)(asint(r2.xy)));
    r0.yz = asuint((asfloat(r0.yz) * asfloat(r2.xy)));
    r0.yz = asuint((asfloat(r0.yz) * asfloat(asuint((float2(_Length, _Length))))));
    r0.w = ((asfloat(0x00000000u) < asfloat(r0.x)) ? 0xffffffffu : 0u);
    r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    r0.x = ((0u - r0.w) + r0.x);
    r0.x = asuint((float)(asint(r0.x)));
    r0.xy = asuint(mad(asfloat(r0.yz), asfloat(r0.xx), asfloat(r1.xy)));
    r0.zw = asuint((asfloat((asuint((float2(_XOffsetNew, _YOffset))) ^ 0x80000000u)) + asfloat(asuint((float2(_XOffsetEnter, _YOffsetEnter))))));
    r0.zw = asuint(mad(asfloat(asuint((float2(_PackedParams0.w, _PackedParams0.w)))), asfloat(r0.zw), asfloat(asuint((float2(_XOffsetNew, _YOffset))))));
    r0.xy = asuint(mad(asfloat(v2.xx), asfloat(r0.zw), asfloat(r0.xy)));
    r0.xy = asuint(mad(asfloat(asuint((float2(_ClipSpaceOffset.x, _ClipSpaceOffset.y)))), asfloat(r1.ww), asfloat(r0.xy)));
    r0.z = ((asfloat(asuint((_StencilShadowBlendDebugMode))) < asfloat(0x3f000000u)) ? 0xffffffffu : 0u);
    o0.xy = ((r0.zz != 0u) ? r0.xy : r1.xy);
    o0.zw = r1.zw;
    o1.x = v2.x;
    result.output0 = asfloat(o0.xyzw);
    result.output1 = asfloat(o1.x);
    return result;
}
