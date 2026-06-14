Shader "3C/VFX/BlockImpact/Spark"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1, 0.5, 0.1, 1)
        _Intensity ("Intensity", Float) = 6
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            Name "BlockImpactSpark"
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
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half shape = max(max(tex.r, tex.g), tex.b) * tex.a;
                half alpha = shape * input.color.a * _TintColor.a * _Alpha;
                half3 color = input.color.rgb * _TintColor.rgb * _Intensity * alpha;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
