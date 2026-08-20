// Sample terrain shader: vertex displacement + albedo from the global
// heightmap (_GiHeightmap), lit by the rebuilt GI lightmap (_GILightMap).
Shader "SandcastleWaterGi/GiTerrain"
{
	Properties
	{
		_AlbedoScale ("Albedo scale", Float) = 1.2
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _GiHeightmap;
			sampler2D _GILightMap;
			float _AlbedoScale;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				float h = tex2Dlod(_GiHeightmap, float4(v.uv, 0, 0)).r;
				float3 world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
				world.y = h;
				o.worldPos = world;
				o.uv = v.uv;
				o.pos = UnityWorldToClipPos(world);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 hmap = tex2D(_GiHeightmap, i.uv);
				float3 gi = tex2D(_GILightMap, i.uv).rgb;
				return fixed4(hmap.gba * gi * _AlbedoScale, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
