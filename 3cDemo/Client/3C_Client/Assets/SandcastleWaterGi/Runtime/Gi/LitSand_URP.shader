// ============================================================
// Lit/Sand URP version (Unity 2022.3 URP14 / Unity 6000 URP17).
// Same rebuilt caustics + wetness algorithm as LitSand.shader, lit
// with the URP main light and the rebuilt GI lightmap.
// ============================================================
Shader "SandcastleWaterGi/LitSandURP"
{
	Properties
	{
		[NoScaleOffset] sandMap ("Sand", 2D) = "white" {}
		[NoScaleOffset] causticsMap ("Caustics (3D atlas)", 2D) = "white" {}
		_sandColor ("Sand color", Color) = (0.9254902, 0.8862745, 0.7254902, 1)
		_sandColor2 ("Sand color 2", Color) = (0.92941177, 0.8980392, 0.7529412, 1)
		_wetSandColor ("Wet sand color", Color) = (0.90599996, 0.81037575, 0.633294, 1)
		_underwaterColor ("Underwater sand", Color) = (0.4, 0.52, 0.48, 1)
		_causticsIntensity ("Caustics intensity", Float) = 1
		_causticsSpeed ("Caustics speed", Float) = 0.5
		_tideLevel ("Tide level", Float) = 0.35
		_wetRange ("Wet range", Float) = 0.3
		_giBoost ("GI boost", Float) = 1
	}
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
			#pragma shader_feature _GROUND_BUFFER
			#pragma target 4.5
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(sandMap);      SAMPLER(sampler_sandMap);
			TEXTURE2D(causticsMap);  SAMPLER(sampler_causticsMap);
			TEXTURE2D(_GiHeightmap); SAMPLER(sampler_GiHeightmap);
			TEXTURE2D(_GILightMap);  SAMPLER(sampler_GILightMap);
			float4 _GiWorldSize;
			float4 _GiOriginOffset;

			#ifdef _GROUND_BUFFER
			StructuredBuffer<float4> _GroundA;
			StructuredBuffer<float4> _GroundB;
			float4 _GroundSimRes;
			float4 _GroundSimSize;
			float4 _GroundSimOrigin;
			#endif

			CBUFFER_START(UnityPerMaterial)
			float4 _sandColor;
			float4 _sandColor2;
			float4 _wetSandColor;
			float4 _underwaterColor;
			float _causticsIntensity;
			float _causticsSpeed;
			float _tideLevel;
			float _wetRange;
			float _giBoost;
			CBUFFER_END

			struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float wetness : TEXCOORD2;
				float depth : TEXCOORD3;
				float2 flow : TEXCOORD4;
				float2 uv : TEXCOORD5;
			};

			float HeightAt(float2 xz)
			{
				float2 uv = (xz - _GiOriginOffset.xy) / _GiWorldSize.xy;
				return SAMPLE_TEXTURE2D_LOD(_GiHeightmap, sampler_GiHeightmap, uv, 0).r;
			}

			#ifdef _GROUND_BUFFER
			float4 GroundAt(float2 xz)
			{
				float2 uv = (xz - _GroundSimOrigin.xy) / _GroundSimSize.xy;
				int2 c = clamp(int2(uv * _GroundSimRes.xy), int2(0, 0), int2(_GroundSimRes.xy) - 1);
				return _GroundA[c.y * (int)_GroundSimRes.x + c.x];
			}
			float4 GroundBAt(float2 xz)
			{
				float2 uv = (xz - _GroundSimOrigin.xy) / _GroundSimSize.xy;
				int2 c = clamp(int2(uv * _GroundSimRes.xy), int2(0, 0), int2(_GroundSimRes.xy) - 1);
				return _GroundB[c.y * (int)_GroundSimRes.x + c.x];
			}
			#endif

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				float3 world = TransformObjectToWorld(IN.positionOS.xyz);
				float h;
				#ifdef _GROUND_BUFFER
				float2 stepX = float2(_GroundSimSize.x / _GroundSimRes.x, 0);
				float2 stepZ = float2(0, _GroundSimSize.y / _GroundSimRes.y);
				float4 g = GroundAt(world.xz);
				float4 gL = GroundAt(world.xz - stepX);
				float4 gR = GroundAt(world.xz + stepX);
				float4 gD = GroundAt(world.xz - stepZ);
				float4 gU = GroundAt(world.xz + stepZ);
				h = (gL.x + gR.x + gD.x + gU.x) * 0.25;
				OUT.wetness = g.w;
				float4 b = GroundBAt(world.xz);
				OUT.normalWS = normalize(float3(-b.x, 1.0, -b.y));
				OUT.depth = b.z;
				OUT.flow = float2(g.y, g.z);
				#else
				h = HeightAt(world.xz);
				OUT.wetness = saturate((_tideLevel - h) / _wetRange);
				OUT.depth = max(_tideLevel - h, 0.0);
				OUT.flow = float2(sin(world.x * 0.15 + _Time.y * 0.4), sin(world.z * 0.13 - _Time.y * 0.35)) * 0.5;
				float e = 0.1;
				OUT.normalWS = normalize(float3(HeightAt(world.xz - float2(e, 0)) - HeightAt(world.xz + float2(e, 0)),
				                               2.0 * e,
				                               HeightAt(world.xz - float2(0, e)) - HeightAt(world.xz + float2(0, e))));
				#endif
				world.y = h;
				OUT.positionWS = world;
				OUT.uv = IN.uv;
				OUT.positionCS = TransformWorldToHClip(world);
				return OUT;
			}

			float TriWave(float x) { return abs(frac(x + 0.5) * 2.0 - 1.0); }

			float SampleCaustics3D(float3 uvw)
			{
				float slice = saturate(uvw.z) * 63.999;
				float s0 = floor(slice);
				float f = slice - s0;
				float2 uvA = float2(fmod(s0, 64.0), floor(s0 / 64.0)) / float2(64.0, 256.0);
				float2 uvB = float2(fmod(s0 + 1.0, 64.0), floor((s0 + 1.0) / 64.0)) / float2(64.0, 256.0);
				float2 inUV = saturate(uvw.xy);
				float a = SAMPLE_TEXTURE2D_LOD(causticsMap, sampler_causticsMap, uvA + inUV / float2(64.0, 256.0), 0).r;
				float b = SAMPLE_TEXTURE2D_LOD(causticsMap, sampler_causticsMap, uvB + inUV / float2(64.0, 256.0), 0).r;
				return lerp(a, b, f);
			}

			half4 frag(Varyings IN) : SV_Target
			{
				float3 sand = SAMPLE_TEXTURE2D(sandMap, sampler_sandMap, IN.positionWS.xz * 0.3).rgb;
				float t = _Time.y * _causticsSpeed;
				float tri1 = TriWave(t * 0.5 + sand.g);
				float2 cuv = IN.positionWS.xz * 0.25 + tri1 * IN.flow * 0.1;
				float c1 = SampleCaustics3D(float3(cuv * 0.2 - 0.5, TriWave(t + 0.5)));
				float c2 = SampleCaustics3D(float3(cuv * 0.2 + 0.5, TriWave(t)));
				float caustic = lerp(c1, c2, TriWave(t * 2.0));

				Light mainLight = GetMainLight();
				caustic *= exp(-1.731234 * IN.wetness) * length(mainLight.color) * _causticsIntensity;

				float w = saturate(IN.wetness * 52.63158);
				float w2 = w * w * (3.0 - 2.0 * w);
				float3 col = lerp(_sandColor.rgb, _wetSandColor.rgb, w2);
				col = lerp(col, _underwaterColor.rgb, saturate(IN.depth * 20.0));
				col += (col * sand.r - col) * 0.25;

				float3 gi = SAMPLE_TEXTURE2D(_GILightMap, sampler_GILightMap, IN.uv).rgb * _giBoost;
				float3 N = normalize(IN.normalWS);
				float ndl = max(dot(N, mainLight.direction), 0.0);
				float3 sun = ndl * mainLight.color.rgb * mainLight.shadowAttenuation;
				float3 fill = max(dot(N, -normalize(mainLight.direction + float3(0, -0.5, 0))), 0.0)
				              * mainLight.color.rgb * 0.25;

				float3 lit = col * (gi + sun + fill) + caustic * col * 0.5;
				return half4(lit, 1);
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}
