Shader "ZZZ/Restored/NapStencilShadowCaster"
{
    Properties
    {
        [HideInInspector] _Length("_Length", Float) = 0.8
        [HideInInspector] _XOffsetNew("_XOffsetNew", Float) = 0
        [HideInInspector] _YOffset("_YOffset", Float) = 0
        [HideInInspector] _XOffsetEnter("_XOffsetEnter", Float) = -0.7
        [HideInInspector] _YOffsetEnter("_YOffsetEnter", Float) = 0.75
        [HideInInspector] _ZOffset("_ZOffset", Float) = 0.5
        [HideInInspector] _UI("_UI", Float) = 0
        [HideInInspector] _DitherAlpha("_DitherAlpha", Float) = 1
        [HideInInspector] _DitherAlpha2("_DitherAlpha2", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            Name "StencilShadowCaster"
            Tags { "LightMode"="StencilShadowCaster" }
            Cull Back
            ZTest LEqual
            ZWrite Off
            Blend 0 Zero One, Zero One
            Stencil
            {
                Ref [_StencilShadowStencilRef]
                ReadMask 128
                WriteMask [_StencilShadowStencil]
                Comp Equal
                Pass Replace
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalStage0.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalStage6.hlsl"
            #endif
            ENDHLSL
        }
    }
}
