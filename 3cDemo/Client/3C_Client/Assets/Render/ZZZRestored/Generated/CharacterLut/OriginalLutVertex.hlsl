    column_major float4x4 unity_MatrixVP;

    column_major float4x4 unity_ObjectToWorld;


struct ShaderOutput {
    float4 o0 : SV_POSITION0;
    float2 o1 : TEXCOORD0;
    float4 o2 : TEXCOORD1;
};
ShaderOutput main(float4 input0 : POSITION0, float2 input1 : TEXCOORD0)
{
    uint4 r0, r1, o0, o1, o2, v0, v1;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    r0.xyzw = asuint((asfloat(v0.yyyy) * asfloat(asuint((float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]))))));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0])))), asfloat(v0.xxxx), asfloat(r0.xyzw)));
    r0.xyzw = asuint(mad(asfloat(asuint((float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2])))), asfloat(v0.zzzz), asfloat(r0.xyzw)));
    r0.xyzw = asuint((asfloat(r0.xyzw) + asfloat(asuint((float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]))))));
    r1.xyzw = asuint((asfloat(r0.yyyy) * asfloat(asuint((float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]))))));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0])))), asfloat(r0.xxxx), asfloat(r1.xyzw)));
    r1.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2])))), asfloat(r0.zzzz), asfloat(r1.xyzw)));
    o0.xyzw = asuint(mad(asfloat(asuint((float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3])))), asfloat(r0.wwww), asfloat(r1.xyzw)));
    o1.xy = v1.xy;
    o2.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.o0 = asfloat(o0.xyzw);
    result.o1 = asfloat(o1.xy);
    result.o2 = asfloat(o2.xyzw);
    return result;
}
