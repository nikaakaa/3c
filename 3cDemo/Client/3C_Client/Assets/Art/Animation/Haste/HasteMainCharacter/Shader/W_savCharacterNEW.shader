Shader "W/savCharacterNEW" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_NormalStr ("NormalStr", Range(0, 1)) = 1
		_Normal ("Normal", 2D) = "white" {}
		_SpecularTintColor ("SpecularTintColor", Vector) = (0.6886792,0.6886792,0.6886792,0)
		_ShadowColor ("ShadowColor", Vector) = (0.6886792,0.6886792,0.6886792,0)
		[Toggle(_DOTSONLY_ON)] _DotsOnly ("DotsOnly", Float) = 0
		_UVScale ("UV Scale", Float) = 1
		_Counter ("Counter", Range(0, 1)) = 0
		[RemapSliders] _Shadows ("Shadows", Vector) = (0,0.2,0,0)
		_ShadowsHSV ("ShadowsHSV", Vector) = (0,0,0,0)
		[Header(__________________________)] [Header(HSV Edits)] _H ("H", Range(-1, 1)) = 0
		_V ("V", Range(0, 4)) = 1
		_S ("S", Range(0, 2)) = 1
		[HideInInspector] [HDR] _ShaderColor ("ShaderColor", Vector) = (2.670157,2.670157,2.670157,1)
		_Metal ("Metal", 2D) = "black" {}
		_RimPower ("Rim Power", Range(0, 3)) = 1.89
		[HideInInspector] _ShieldAmount ("ShieldAmount", Float) = 0
		_RimAmount ("Rim Amount", Range(0, 1)) = 0.51
		_SpecularAmount ("Specular Amount", Float) = 3.1
		[HideInInspector] _ShineAmount ("ShineAmount", Float) = 0
		_SpecularOpacity ("Specular Opacity", Range(0, 1)) = 0.29
		_SpecularTintIntensity ("Specular Tint Intensity", Range(0, 1)) = 0.09
		_curvaturemask ("curvature mask", 2D) = "white" {}
		_ConfettiScale ("Confetti Scale", Float) = 35.43
		[Toggle(_CONFETTITIME_ON)] _CONFETTITIME ("CONFETTI TIME", Float) = 0
		_ConfettiScatter ("Confetti Scatter", Float) = 0.3
		_SpecularRange ("Specular Range", Range(0, 1)) = 0.85
		_Emission ("Emission", 2D) = "white" {}
		_EmissionIntensity ("Emission Intensity", Float) = 0
		_RimColorIntensity ("Rim Color Intensity", Range(0, 1)) = 0.252771
		[HDR] _RimColor ("Rim Color", Vector) = (0.6075471,0.6075471,0.6075471,0)
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}