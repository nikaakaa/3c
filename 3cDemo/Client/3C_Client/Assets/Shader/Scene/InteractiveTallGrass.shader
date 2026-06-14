Shader "3C/Scene/Interactive Tall Grass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.16, 0.34, 0.12, 1)
        _TopColor ("Top Color", Color) = (0.45, 0.75, 0.28, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.45
        _ToonStrength ("Toon Strength", Range(0, 1)) = 0.65
        _EdgeStrength ("Edge Strength", Range(0, 1)) = 0.4
        _WindStrength ("Wind Strength", Range(0, 1.2)) = 0.22
        _WindFrequency ("Wind Frequency", Range(0, 8)) = 1.8
        _WindDirection ("Wind Direction", Vector) = (1, 0.35, 0, 0)
        _InteractionPosition ("Interaction Position", Vector) = (0, 0, 0, 0)
        _InteractionRadius ("Interaction Radius", Range(0, 4)) = 1.1
        _BendStrength ("Bend Strength", Range(0, 1.5)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half _Cutoff;
                half _ToonStrength;
                half _EdgeStrength;
                half _WindStrength;
                half _WindFrequency;
                float4 _WindDirection;
                float4 _InteractionPosition;
                half _InteractionRadius;
                half _BendStrength;
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
                half heightMask : TEXCOORD1;
                half3 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float heightMask = saturate(input.uv.y);
                float topMask = heightMask * heightMask;

                float2 windDirection = normalize(_WindDirection.xy + float2(0.0001, 0.0));
                float windWave = sin(_Time.y * _WindFrequency + world.x * 1.7 + world.z * 2.1);
                float2 windOffset = windDirection * windWave * _WindStrength * topMask;

                float2 toBlade = world.xz - _InteractionPosition.xz;
                float distanceToSource = length(toBlade);
                float radius = max(_InteractionRadius, 0.0001);
                float interactionMask = saturate(1.0 - distanceToSource / radius) * _InteractionPosition.w;
                float2 bendDirection = distanceToSource > 0.0001 ? toBlade / distanceToSource : windDirection;
                float2 bendOffset = bendDirection * interactionMask * _BendStrength * topMask;

                world.xz += windOffset + bendOffset;
                world.y -= interactionMask * _BendStrength * topMask * 0.18;

                half3 gradient = lerp(_BaseColor.rgb, _TopColor.rgb, heightMask);
                half toonBand = floor(heightMask * 3.0) / 3.0;
                half3 toonColor = lerp(_BaseColor.rgb, _TopColor.rgb, toonBand);
                half edge = saturate(abs(input.uv.x - 0.5) * 2.0);
                half edgeDarken = 1.0 - edge * _EdgeStrength * 0.28;

                output.positionCS = TransformWorldToHClip(world);
                output.uv = input.uv;
                output.heightMask = heightMask;
                output.color = lerp(gradient, toonColor, _ToonStrength) * edgeDarken;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half centerDistance = abs(input.uv.x - 0.5) * 2.0;
                half widthAtHeight = lerp(0.92, 0.18, input.uv.y);
                half bladeShape = saturate((widthAtHeight - centerDistance) * 3.0);
                half tipFade = saturate((1.05 - input.uv.y) * 8.0);
                half alpha = bladeShape * tipFade;

                clip(alpha - _Cutoff);
                return half4(input.color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
