Shader "3C/VFX/BlockImpact/Additive"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1, 0.65, 0.25, 1)
        _Intensity ("Intensity", Float) = 8
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Softness ("Softness", Range(0.001, 1)) = 0.28
        _UvScaleOffset ("UV Scale Offset", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "BlockImpactAdditive"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half _Intensity;
                half _Alpha;
                half _Softness;
                float4 _UvScaleOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _UvScaleOffset.xy + _UvScaleOffset.zw;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float2 centeredUv = input.uv * 2.0 - 1.0;
                half radialMask = smoothstep(0.0, max(_Softness, 0.0001), saturate(1.0 - length(centeredUv)));
                half shape = max(max(tex.r, tex.g), tex.b) * tex.a;
                half alpha = shape * radialMask * _TintColor.a * _Alpha;
                return half4(_TintColor.rgb * _Intensity * alpha, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
