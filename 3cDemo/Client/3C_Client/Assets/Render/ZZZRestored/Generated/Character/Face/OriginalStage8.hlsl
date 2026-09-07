struct ShaderOutput {
    float4 output0 : SV_Target0;
};
ShaderOutput main()
{
    uint4 o0;
    ShaderOutput result;
    o0.xyzw = uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u);
    result.output0 = asfloat(o0.xyzw);
    return result;
}
