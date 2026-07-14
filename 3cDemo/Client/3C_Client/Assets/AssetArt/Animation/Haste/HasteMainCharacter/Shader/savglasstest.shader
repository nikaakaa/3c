Shader "savglasstest" {
	Properties {
		_Cutoff ("Mask Clip Value", Float) = 0.5
		_linesparallaxdistance ("lines parallax distance", Float) = 1.03
		_circleparallaxdistance ("circle parallax distance", Float) = 0.1
		[HDR] _highlightcolor ("highlight color", Vector) = (0.6320754,0.8709478,1,0)
		_circleoffset ("circle offset", Float) = 0
		_circlesizeouter ("circle size outer", Float) = 1.05
		_circlesizeinner ("circle size inner", Float) = 0.23
		[HideInInspector] _ScreenDotTransparencyEnabled ("Screen Dot Transparency Enabled", Float) = 0
		[HideInInspector] _ScreenDotCoverage ("Screen Dot Coverage", Range(0, 1)) = 0
		[HideInInspector] _ScreenDotSpacingPixels ("Screen Dot Spacing Pixels", Float) = 12
		[HideInInspector] _ScreenDotRadius ("Screen Dot Radius", Range(0, 1)) = 0.45
		[HideInInspector] _ScreenDotHardness ("Screen Dot Hardness", Range(0, 1)) = 1
		[HideInInspector] _ScreenDotOffsetPixels ("Screen Dot Offset Pixels", Vector) = (0,0,0,0)
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float _ScreenDotTransparencyEnabled;
			float _ScreenDotCoverage;
			float _ScreenDotSpacingPixels;
			float _ScreenDotRadius;
			float _ScreenDotHardness;
			float4 _ScreenDotOffsetPixels;

			#include "Assets/Shader/ScreenSpaceDotTransparency/ScreenSpaceDotTransparency.hlsl"

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				ApplyScreenDotTransparencyClip(input.pos);
				return float4(1.0, 1.0, 1.0, 1.0);
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}
