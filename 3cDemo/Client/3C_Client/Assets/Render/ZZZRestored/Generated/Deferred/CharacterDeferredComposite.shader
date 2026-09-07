Shader "ZZZ/Restored/CharacterDeferredComposite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "DeferredShadingForCharacter"
            Cull Off
            ZTest Always
            ZWrite Off
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex ZZZFullscreenVertex
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "FullscreenVertex.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalDeferredShading.hlsl"
            #endif
            ENDHLSL
        }
        Pass
        {
            Name "CharacterPostProcess"
            Cull Off
            ZTest Always
            ZWrite Off
            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11
            #pragma vertex ZZZFullscreenVertex
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "FullscreenVertex.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalCharacterPostProcess.hlsl"
            #endif
            ENDHLSL
        }
    }
}
