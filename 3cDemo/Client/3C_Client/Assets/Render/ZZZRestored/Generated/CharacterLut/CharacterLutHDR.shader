Shader "Hidden/ZZZ/Restored/CharacterLutHDR"
{
    Properties
    {
        [HideInInspector] _Lut_Params("_Lut_Params", Vector) = (32,0.00048828125,0.015625,1.0322580337524414)
        [HideInInspector] _ColorBalance("_ColorBalance", Vector) = (1,1,1,1)
        [HideInInspector] _ColorFilter("_ColorFilter", Vector) = (1,1,1,1)
        [HideInInspector] _ChannelMixerRed("_ChannelMixerRed", Vector) = (1,0,0,0)
        [HideInInspector] _ChannelMixerGreen("_ChannelMixerGreen", Vector) = (0,1,0,0)
        [HideInInspector] _ChannelMixerBlue("_ChannelMixerBlue", Vector) = (0,0,1,0)
        [HideInInspector] _HueSatCon("_HueSatCon", Vector) = (0,1,1,0)
        [HideInInspector] _Lift("_Lift", Vector) = (-0.0005417913198471069,-0.0017117112874984741,0.018556833267211914,0)
        [HideInInspector] _Gamma("_Gamma", Vector) = (0.9900990128517151,0.9900990128517151,0.9900990128517151,0)
        [HideInInspector] _Gain("_Gain", Vector) = (1.0099999904632568,1.0099999904632568,1.0099999904632568,0)
        [HideInInspector] _Shadows("_Shadows", Vector) = (1,1,1,0)
        [HideInInspector] _Midtones("_Midtones", Vector) = (1,1,1,0)
        [HideInInspector] _Highlights("_Highlights", Vector) = (1,1,1,0)
        [HideInInspector] _ShaHiLimits("_ShaHiLimits", Vector) = (0,0.30000001192092896,0.550000011920929,1)
        [HideInInspector] _SplitShadows("_SplitShadows", Vector) = (0.5,0.5,0.5,0)
        [HideInInspector] _SplitHighlights("_SplitHighlights", Vector) = (0.5,0.5,0.5,0)
        [HideInInspector] _CustomToneCurve("_CustomToneCurve", Vector) = (0.7017073631286621,0.07635896652936935,0.5827464461326599,0)
        [HideInInspector] _ToeSegmentA("_ToeSegmentA", Vector) = (0,0,1,1)
        [HideInInspector] _ToeSegmentB("_ToeSegmentB", Vector) = (0.5346906185150146,1.1111111640930176,0,0)
        [HideInInspector] _MidSegmentA("_MidSegmentA", Vector) = (0.007635899819433689,0,1,1)
        [HideInInspector] _MidSegmentB("_MidSegmentB", Vector) = (0.3542387783527374,1,0,0)
        [HideInInspector] _ShoSegmentA("_ShoSegmentA", Vector) = (1,1,-1,-1)
        [HideInInspector] _ShoSegmentB("_ShoSegmentB", Vector) = (1.168330192565918,3.2959237098693848,0,0)
        [HideInInspector] _Desaturate("_Desaturate", Float) = 1
        [HideInInspector] _RevertSaturation("_RevertSaturation", Float) = 0
        [HideInInspector] _UserLut_Params("_UserLut_Params", Vector) = (0,0,0,0)
        [HideInInspector] _CurveBlue("_CurveBlue", 2D) = "" {}
        [HideInInspector] _CurveGreen("_CurveGreen", 2D) = "" {}
        [HideInInspector] _CurveHueVsHue("_CurveHueVsHue", 2D) = "" {}
        [HideInInspector] _CurveHueVsSat("_CurveHueVsSat", 2D) = "" {}
        [HideInInspector] _CurveLumVsSat("_CurveLumVsSat", 2D) = "" {}
        [HideInInspector] _CurveMaster("_CurveMaster", 2D) = "" {}
        [HideInInspector] _CurveRed("_CurveRed", 2D) = "" {}
        [HideInInspector] _CurveSatVsSat("_CurveSatVsSat", 2D) = "" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "LutBuilderHdr"
            Cull Off
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11
            #pragma vertex main
            #pragma fragment main
            #if defined(SHADER_STAGE_VERTEX)
                #include "OriginalLutVertex.hlsl"
            #elif defined(SHADER_STAGE_FRAGMENT)
                #include "OriginalLutFragment.hlsl"
            #endif
            ENDHLSL
        }
    }
}
