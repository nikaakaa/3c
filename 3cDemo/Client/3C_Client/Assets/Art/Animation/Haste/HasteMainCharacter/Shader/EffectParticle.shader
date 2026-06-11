Shader "EffectParticle" {
	Properties {
		_Cutoff ("Mask Clip Value", Float) = 0.5
		[HDR] _Color1 ("Color 1", Vector) = (1,1,1,1)
		[HDR] _Color2 ("Color 2", Vector) = (0,0,0,1)
		[RemapSliders] _Remap ("Remap", Vector) = (0,1,0,0)
		[RemapSliders] _AlphaRemap ("AlphaRemap", Vector) = (0,1,0,0)
		_Texture ("Texture", 2D) = "white" {}
		_Noise ("Noise", 2D) = "white" {}
		_NoiseScroll ("NoiseScroll", Vector) = (0,0,0,0)
		_Scroll1 ("Scroll", Vector) = (0,0,0,0)
		_NoiseStr ("NoiseStr", Float) = 1
		_Round ("Round", Range(0, 1)) = 1
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
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}