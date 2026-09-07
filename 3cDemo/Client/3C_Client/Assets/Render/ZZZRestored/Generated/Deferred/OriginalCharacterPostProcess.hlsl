
    float4 _ExposureParams;


SamplerState sampler_InputTex;
Texture2D<float4> _InputTex : register(t0);
struct ShaderOutput {
    float4 output0 : SV_Target0;
};
ShaderOutput main(float4 input0 : SV_POSITION0, float2 input1 : TEXCOORD0)
{
    uint4 r0, r1, r2, o0, v0, v1;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xy = asuint(input1);
    r0.xyzw = asuint(_InputTex.Sample(sampler_InputTex, asfloat(v1.xy)).xyzw);
    r1.xyzw = (r0.xyzw & uint4(0x7fffffffu, 0x7fffffffu, 0x7fffffffu, 0x7fffffffu));
    r2.xyzw = ((uint4(0x7f800000u, 0x7f800000u, 0x7f800000u, 0x7f800000u) < r1.xyzw) ? 0xffffffffu : 0u);
    r1.xyzw = ((asint(r1.xyzw) == asint(uint4(0x7f800000u, 0x7f800000u, 0x7f800000u, 0x7f800000u))) ? 0xffffffffu : 0u);
    r2.x = (r2.y | r2.x);
    r2.x = (r2.z | r2.x);
    r2.x = (r2.w | r2.x);
    r1.x = (r1.y | r1.x);
    r1.x = (r1.z | r1.x);
    r1.x = (r1.w | r1.x);
    r1.x = (r1.x | r2.x);
    r0.xyzw = ((r1.xxxx != 0u) ? uint4(0x00000000u, 0x00000000u, 0x00000000u, 0x3f800000u) : r0.xyzw);
    r1.x = asuint((asfloat(asuint((_ExposureParams.x))) + asfloat(0xbf800000u)));
    r1.x = asuint(mad(asfloat(asuint((_ExposureParams.z))), asfloat(r1.x), asfloat(0x3f800000u)));
    o0.xyz = asuint((asfloat(r0.xyz) * asfloat(r1.xxx)));
    o0.w = r0.w;
    result.output0 = asfloat(o0.xyzw);
    return result;
}
