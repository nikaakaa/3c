Shader "AirWave" {
	Properties {
		_Tex ("Tex", 2D) = "white" {}
		_Speed ("Speed", Vector) = (0,0,0,0)
		_T ("T", Float) = 1
		[HDR] _Color ("Color", Vector) = (0,0,0,0.2)
		_RainbowEmiss ("RainbowEmiss", Float) = 5
		_Loops ("Loops", Float) = 5
		_Rainbowtime ("Rainbow time", Float) = 5
		[RemapSliders] _Rainbow ("Rainbow", Vector) = (0,1,0,0)
		[RemapSliders] _AlphaRemap ("AlphaRemap", Vector) = (0.7,0.8,0,0)
		_HeightPow ("HeightPow", Float) = 1
		_Fade ("Fade", Range(0, 1)) = 1
		_DetailAmount ("DetailAmount", Float) = 0
		_AlphaOffset ("AlphaOffset", Float) = 0
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