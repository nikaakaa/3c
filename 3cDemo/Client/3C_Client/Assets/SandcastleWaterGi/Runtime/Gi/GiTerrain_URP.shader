// Sample terrain shader (URP): heightfield displacement + rebuilt GI.
Shader "SandcastleWaterGi/GiTerrainURP"
{
	Properties { _AlbedoScale ("Albedo scale", Float) = 1.2 }
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_GiHeightmap); SAMPLER(sampler_GiHeightmap);
			TEXTURE2D(_GILightMap);  SAMPLER(sampler_GILightMap);
			float _AlbedoScale;

			struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
			struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				float h = SAMPLE_TEXTURE2D_LOD(_GiHeightmap, sampler_GiHeightmap, IN.uv, 0).r;
				float3 world = TransformObjectToWorld(IN.positionOS.xyz);
				world.y = h;
				OUT.positionWS = world;
				OUT.uv = IN.uv;
				OUT.positionCS = TransformWorldToHClip(world);
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target
			{
				float4 hmap = SAMPLE_TEXTURE2D(_GiHeightmap, sampler_GiHeightmap, IN.uv);
				float3 gi = SAMPLE_TEXTURE2D(_GILightMap, sampler_GILightMap, IN.uv).rgb;
				return half4(hmap.gba * gi * _AlbedoScale, 1);
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}
