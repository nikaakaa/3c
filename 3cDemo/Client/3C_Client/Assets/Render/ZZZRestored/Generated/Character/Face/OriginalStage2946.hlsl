
    float _CorinGlobalMipBias;



    float4 _TransitionTex_ST;
    float4 _OverrideOutlineTex_ST;
    float _DitherAlpha;
    float _DitherAlpha2;
    float _Transition;
    float _TransitionWidth;
    float _TransitionCompletion;
    float _OverrideOutlineUseUV2;

static const uint4 icb[16] = { uint4(0x3d70f0f1u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f078788u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3e34b4b5u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f25a5a6u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f43c3c4u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3e969697u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f61e1e2u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3ed2d2d3u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3e70f0f1u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f34b4b5u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3df0f0f1u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f169697u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f70f0f1u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3ef0f0f1u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3f52d2d3u, 0x00000000u, 0x00000000u, 0x00000000u), uint4(0x3eb4b4b5u, 0x00000000u, 0x00000000u, 0x00000000u) };


SamplerState sampler_OverrideOutlineTex;
SamplerState sampler_TransitionTex;
Texture2D<float4> _OverrideOutlineTex : register(t0);
Texture2D<float4> _TransitionTex : register(t1);
struct ShaderOutput {
};
ShaderOutput main(float4 input0 : TEXCOORD0, float4 input1 : TEXCOORD1, float3 input2 : TEXCOORD2, float3 input3 : TEXCOORD3, float3 input4 : TEXCOORD4, float4 input5 : TEXCOORD5, float4 input6 : TXCOORDD6, float4 input7 : TEXCOORD7, float3 input8 : TEXCOORD8, float4 input9 : SV_POSITION0, bool input10 : SV_IsFrontFace0)
{
    uint4 r0, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyz = asuint(input2);
    v3.xyz = asuint(input3);
    v4.xyz = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyz = asuint(input8);
    v9.xyzw = asuint(input9);
    v10.x = (input10 ? 0xffffffffu : 0u);
    r0.x = asuint((asfloat(asuint((_DitherAlpha2))) * asfloat(asuint((_DitherAlpha)))));
    r0.x = ((asfloat(r0.x) < asfloat(0x3f70f0f1u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        r0.xy = (uint2)(asfloat(v9.xy));
        r0.x = (((r0.x << 0x00000002u) & (0x00000003u << 0x00000002u)) | (0x00000000u & ~(0x00000003u << 0x00000002u)));
        r0.x = (((r0.y << 0x00000000u) & (0x00000003u << 0x00000000u)) | (r0.x & ~(0x00000003u << 0x00000000u)));
        r0.x = asuint(mad(asfloat(asuint((_DitherAlpha))), asfloat(asuint((_DitherAlpha2))), asfloat((icb[r0.x+0].x ^ 0x80000000u))));
        r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
        if (r0.x != 0u) discard;
    }
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_Transition)))) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_OverrideOutlineUseUV2)))) ? 0xffffffffu : 0u);
        r0.xy = ((r0.xx != 0u) ? v1.xy : v0.xy);
        r0.zw = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_TransitionTex_ST.x, _TransitionTex_ST.y)))), asfloat(asuint((float2(_TransitionTex_ST.z, _TransitionTex_ST.w))))));
        r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_OverrideOutlineTex_ST.x, _OverrideOutlineTex_ST.y)))), asfloat(asuint((float2(_OverrideOutlineTex_ST.z, _OverrideOutlineTex_ST.w))))));
        r0.z = asuint(_TransitionTex.SampleBias(sampler_TransitionTex, asfloat(r0.zw), asfloat(asuint((_CorinGlobalMipBias)))).x);
        r0.x = asuint(_OverrideOutlineTex.SampleBias(sampler_OverrideOutlineTex, asfloat(r0.xy), asfloat(asuint((_CorinGlobalMipBias)))).x);
        r0.y = asuint((asfloat(asuint((_TransitionWidth))) + asfloat(0x3f800000u)));
        r0.y = asuint(mad(asfloat(asuint((_TransitionCompletion))), asfloat(r0.y), asfloat((asuint((_TransitionWidth)) ^ 0x80000000u))));
        r0.x = asuint(mad(asfloat(r0.z), asfloat(r0.x), asfloat((r0.y ^ 0x80000000u))));
        r0.x = asuint((asfloat(r0.x) + asfloat(0xba83126fu)));
        r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
        if (r0.x != 0u) discard;
    }
    r0.x = ((asfloat(v5.z) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
    if (r0.x != 0u) discard;
    return result;
}
