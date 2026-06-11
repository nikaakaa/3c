Shader "sav_CHAREYESHELLS" {
	Properties {
		_Cutoff ("Mask Clip Value", Float) = 0.5
		_bigcircleparallaxdistance ("big circle parallax distance", Float) = 0.1
		_smallercircleparallaxdistance ("smaller circle parallax distance", Float) = 0.1
		_circleparallaxdistance1 ("circle parallax distance", Float) = 0.1
		[HDR] _highlightcolor ("highlight color", Vector) = (0.6320754,0.8709478,1,0)
		_bigcirccoordtest ("big circ coord test", Vector) = (0.5,0.5,0,0)
		_smallercirccoordtest ("smaller circ coord test", Vector) = (0.5,0.5,0,0)
		_circcoordtest1 ("circ coord test", Vector) = (0.5,0.5,0,0)
		_smallercircleoffset ("smaller circle offset", Float) = 0
		_circleoffset1 ("circle offset", Float) = 0
		_bigcircleoffset ("big circle offset", Float) = 0
		_bigcirclesize ("big circle size", Float) = 0.1
		_circlesize1 ("circle size", Float) = 0.1
		_smallercirclesize ("smaller circle size", Float) = 0.1
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