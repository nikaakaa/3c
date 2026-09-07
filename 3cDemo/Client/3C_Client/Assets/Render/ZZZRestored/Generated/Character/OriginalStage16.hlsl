struct ShaderOutput {
    float4 o0 : SV_Target0;
};
ShaderOutput main(float4 input0 : TEXCOORD1, float4 input1 : SV_POSITION0)
{
    uint4 o0, v0, v1;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.o0 = asfloat(o0.xyzw);
    return result;
}
