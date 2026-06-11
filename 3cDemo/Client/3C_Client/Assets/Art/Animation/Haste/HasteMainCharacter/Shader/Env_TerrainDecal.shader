Shader "Env/TerrainDecal" {
	Properties {
		[SingleLineTexture] _Texture ("Texture", 2D) = "white" {}
		[Toggle(_SHAPEFLIP_ON)] _ShapeFlip ("ShapeFlip", Float) = 0
		[Toggle(_FLIP_ON)] _Flip ("Flip", Float) = 0
		[HDR] _Color ("Color", Vector) = (1,1,1,1)
		[HDR] _Color2 ("Color 2", Vector) = (1,1,1,1)
		[RemapSliders] _TexColorRemap ("TexColorRemap", Vector) = (0,1,0,0)
		[RemapSliders] _Remap ("Remap", Vector) = (0,1,0,0)
		_Scale ("Scale", Float) = 0
		_DistancePow ("DistancePow", Float) = 1
		_TextureShapeStr ("TextureShapeStr", Range(0, 1)) = 0.2
		_FadeMin ("FadeMin", Float) = 1
		_FadeSmooth ("FadeSmooth", Float) = 1
		[SingleLineTexture] _Noise ("Noise", 2D) = "white" {}
		_NoiseScroll ("NoiseScroll", Float) = 0
		_NoiseStr ("NoiseStr", Float) = 0
		_NoiseScale ("NoiseScale", Float) = 1
		_Fade ("Fade", Range(0, 1)) = 1
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

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}