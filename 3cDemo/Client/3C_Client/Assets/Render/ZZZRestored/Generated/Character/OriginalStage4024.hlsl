
    column_major float4x4 unity_MatrixV;
    float4 _ScreenParams;
    float4 _ScreenSize;



    float4 _TransitionTex_ST;
    float4 _OverrideOutlineTex_ST;
    float4 _DitherCenter;
    float4 _FxUVDitherValue;
    int _UseDitherCenter;
    float _CenterMinAlpha;
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
ShaderOutput main(float4 input0 : TEXCOORD0, float4 input1 : TEXCOORD1, float4 input2 : TEXCOORD2, float4 input3 : TEXCOORD3, float4 input4 : TEXCOORD4, float4 input5 : TEXCOORD5, float4 input6 : TEXCOORD6, float4 input7 : TEXCOORD7, float3 input8 : TEXCOORD8, float4 input9 : SV_POSITION0, bool input10 : SV_IsFrontFace0)
{
    uint4 r0, r1, v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10;
    ShaderOutput result;
    v0.xyzw = asuint(input0);
    v1.xyzw = asuint(input1);
    v2.xyzw = asuint(input2);
    v3.xyzw = asuint(input3);
    v4.xyzw = asuint(input4);
    v5.xyzw = asuint(input5);
    v6.xyzw = asuint(input6);
    v7.xyzw = asuint(input7);
    v8.xyz = asuint(input8);
    v9.xyzw = asuint(input9);
    v10.x = (input10 ? 0xffffffffu : 0u);
    r0.x = asuint((float)(asint(asuint((asfloat(_UseDitherCenter))))));
    r0.x = ((asfloat(0x3f000000u) < asfloat(r0.x)) ? 0xffffffffu : 0u);
    r0.yz = asuint(mad(asfloat((v9.xy ^ 0x80000000u)), asfloat(asuint((float2(_ScreenSize.z, _ScreenSize.w)))), asfloat(asuint((float2(_DitherCenter.x, _DitherCenter.y))))));
    r1.y = asuint((asfloat(asuint((_ScreenParams.y))) / asfloat(asuint((_ScreenParams.x)))));
    r1.x = 0x3f800000u;
    r0.yz = asuint((asfloat(r0.yz) * asfloat(r1.xy)));
    r0.w = asuint((asfloat(v3.w) * asfloat(asuint((unity_MatrixV[2][1])))));
    r0.w = asuint(mad(asfloat(asuint((unity_MatrixV[2][0]))), asfloat(v2.w), asfloat(r0.w)));
    r0.w = asuint(mad(asfloat(asuint((unity_MatrixV[2][2]))), asfloat(v4.w), asfloat(r0.w)));
    r0.w = asuint((asfloat(r0.w) + asfloat(asuint((unity_MatrixV[2][3])))));
    r0.w = asuint((asfloat(r0.w) * asfloat(0xbdcccccdu)));
    r0.w = asuint((asfloat(r0.w) / asfloat(asuint((_ScreenSize.z)))));
    r0.y = asuint(dot(asfloat(r0.yz), asfloat(r0.yz)));
    r0.y = asuint(sqrt(asfloat(r0.y)));
    r0.z = asuint((asfloat(asuint((_DitherCenter.z))) / asfloat(r0.w)));
    r0.z = asuint(max(asfloat(r0.z), asfloat(0x00800000u)));
    r0.y = asuint((asfloat(r0.y) / asfloat(r0.z)));
    r0.y = asuint(min(asfloat(r0.y), asfloat(0x3f800000u)));
    r0.y = asuint(log2(asfloat(r0.y)));
    r0.y = asuint((asfloat(r0.y) * asfloat(asuint((_DitherCenter.w)))));
    r0.y = asuint(exp2(asfloat(r0.y)));
    r0.y = asuint(max(asfloat(r0.y), asfloat(asuint((_CenterMinAlpha)))));
    r0.y = asuint((asfloat(r0.y) * asfloat(asuint((_DitherAlpha2)))));
    r0.x = ((r0.x != 0u) ? r0.y : asuint((_DitherAlpha2)));
    r0.y = asuint((asfloat(v1.y) + asfloat((asuint((_FxUVDitherValue.w)) ^ 0x80000000u))));
    r0.z = asuint((asfloat((asuint((_FxUVDitherValue.z)) ^ 0x80000000u)) + asfloat((asuint((_FxUVDitherValue.w)) ^ 0x80000000u))));
    r0.z = asuint((asfloat(r0.z) + asfloat(0x3f800000u)));
    r0.z = asuint(max(asfloat(r0.z), asfloat(0x38d1b717u)));
    r0.y = asuint(saturate((asfloat(r0.y) / asfloat(r0.z))));
    r0.z = asuint((asfloat((asuint((_FxUVDitherValue.y)) ^ 0x80000000u)) + asfloat(asuint((_FxUVDitherValue.x)))));
    r0.y = asuint(mad(asfloat(r0.y), asfloat(r0.z), asfloat(asuint((_FxUVDitherValue.y)))));
    r0.x = asuint((asfloat(r0.y) * asfloat(r0.x)));
    r0.y = asuint((asfloat(r0.x) * asfloat(asuint((_DitherAlpha)))));
    r0.y = ((asfloat(r0.y) < asfloat(0x3f70f0f1u)) ? 0xffffffffu : 0u);
    if (r0.y != 0u) {
        r0.yz = (uint2)(asfloat(v9.xy));
        r0.y = (((r0.y << 0x00000002u) & (0x00000003u << 0x00000002u)) | (0x00000000u & ~(0x00000003u << 0x00000002u)));
        r0.y = (((r0.z << 0x00000000u) & (0x00000003u << 0x00000000u)) | (r0.y & ~(0x00000003u << 0x00000000u)));
        r0.x = asuint(mad(asfloat(asuint((_DitherAlpha))), asfloat(r0.x), asfloat((icb[r0.y+0].x ^ 0x80000000u))));
        r0.x = ((asfloat(r0.x) < asfloat(0x00000000u)) ? 0xffffffffu : 0u);
        if (r0.x != 0u) discard;
    }
    r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_Transition)))) ? 0xffffffffu : 0u);
    if (r0.x != 0u) {
        r0.x = ((asfloat(0x3f000000u) < asfloat(asuint((_OverrideOutlineUseUV2)))) ? 0xffffffffu : 0u);
        r0.xy = ((r0.xx != 0u) ? v1.xy : v0.xy);
        r0.zw = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_TransitionTex_ST.x, _TransitionTex_ST.y)))), asfloat(asuint((float2(_TransitionTex_ST.z, _TransitionTex_ST.w))))));
        r0.xy = asuint(mad(asfloat(r0.xy), asfloat(asuint((float2(_OverrideOutlineTex_ST.x, _OverrideOutlineTex_ST.y)))), asfloat(asuint((float2(_OverrideOutlineTex_ST.z, _OverrideOutlineTex_ST.w))))));
        r0.z = asuint(_TransitionTex.Sample(sampler_TransitionTex, asfloat(r0.zw)).x);
        r0.x = asuint(_OverrideOutlineTex.Sample(sampler_OverrideOutlineTex, asfloat(r0.xy)).x);
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
