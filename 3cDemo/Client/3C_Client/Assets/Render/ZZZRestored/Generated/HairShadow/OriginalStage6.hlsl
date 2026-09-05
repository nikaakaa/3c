struct ShaderOutput {
    float4 output0 : SV_Target0;
};
ShaderOutput main(float4 input0 : SV_POSITION0, float input1 : TEXCOORD0)
{
    uint4 o0, v0, v1;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.x = asuint(input1);
    o0.xyz = v1.xxx;
    o0.w = 0x3f800000u;
    result.output0 = asfloat(o0.xyzw);
    return result;
}
