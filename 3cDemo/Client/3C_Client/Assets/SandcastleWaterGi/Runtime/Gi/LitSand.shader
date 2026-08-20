// ============================================================
// Lit/Sand (package rebuild)
// Translated instruction-by-instruction from the shipped SM5.0
// bytecode of Sandcastle Demo's Lit/Sand shader:
//
//   - world-space UV -> sandMap, triangular-wave scroll (frc fold)
//   - 3D caustics texture sampled with TIME as the third axis,
//     two offset samples blended by a second triangular wave;
//     strength = exp(-1.731234 * wetness) * sun intensity
//   - wet/dry/underwater colour: smoothstep(wetness*52.63) lerp
//     between sandColor and wetSandColor, then lerp toward the
//     underwater colour by depth (v5.w * 20 in the original)
//   - sandMap brightness modulation
//   - sun diffuse + fill light + the rebuilt GI lightmap (_GILightMap)
//
// The original reads wetness/flow from the Ground compute buffer
// (bilinear-interpolated 24-bit fields). This rebuild derives
// wetness from the heightfield vs tide level and flow from waves.
// ============================================================
Shader "SandcastleWaterGi/LitSand"
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
		Tags { "RenderType"="Opaque" }
		Pass
		{
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma shader_feature _GROUND_BUFFER
			#pragma target 4.5
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D sandMap;
			sampler2D causticsMap;
			sampler2D _GiHeightmap;
			sampler2D _GILightMap;
			float4 _GiWorldSize;
			float4 _GiOriginOffset;

			// sand simulation buffers (GroundSimulation component)
			#ifdef _GROUND_BUFFER
			StructuredBuffer<float4> _GroundA; // x=height y=vx z=vy w=wetness
			StructuredBuffer<float4> _GroundB; // xy=tangent z=depth
			float4 _GroundSimRes;
			float4 _GroundSimSize;
			float4 _GroundSimOrigin;
			#endif

			float4 _sandColor;
			float4 _sandColor2;
			float4 _wetSandColor;
			float4 _underwaterColor;
			float _causticsIntensity;
			float _causticsSpeed;
			float _tideLevel;
			float _wetRange;
			float _giBoost;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float wetness : TEXCOORD2;  // original v4.w (Ground buffer wetness)
				float depth : TEXCOORD3;    // underwater depth (original v5.w)
				float2 flow : TEXCOORD4;    // original v5.xy (bilinear flow tangent)
				float2 uv : TEXCOORD5;
				SHADOW_COORDS(6)
			};

			float HeightAt(float2 xz)
			{
				float2 uv = (xz - _GiOriginOffset.xy) / _GiWorldSize.xy;
				return tex2Dlod(_GiHeightmap, float4(uv, 0, 0)).r;
			}

			#ifdef _GROUND_BUFFER
			// sample the sand simulation (4-neighbour average, like the
			// original vertex shader reading the Ground buffer)
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

			v2f vert (appdata v)
			{
				v2f o;
				float3 world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
				#ifdef _GROUND_BUFFER
				// original data flow: heights / wetness / flow from the
				// Ground compute buffer, 4-neighbour averaged
				float2 stepX = float2(_GroundSimSize.x / _GroundSimRes.x, 0);
				float2 stepZ = float2(0, _GroundSimSize.y / _GroundSimRes.y);
				float4 g = GroundAt(world.xz);
				float4 gL = GroundAt(world.xz - stepX);
				float4 gR = GroundAt(world.xz + stepX);
				float4 gD = GroundAt(world.xz - stepZ);
				float4 gU = GroundAt(world.xz + stepZ);
				float h = (gL.x + gR.x + gD.x + gU.x) * 0.25;
				o.wetness = g.w;
				float4 b = GroundBAt(world.xz);
				float2 tangent = b.xy;
				o.worldNormal = normalize(float3(-tangent.x, 1.0, -tangent.y));
				o.depth = b.z;
				o.flow = float2(g.y, g.z);
				#else
				float h = HeightAt(world.xz);
				float tide = _tideLevel;
				o.wetness = saturate((tide - h) / _wetRange);
				o.depth = max(tide - h, 0.0);
				o.flow = float2(sin(world.x * 0.15 + _Time.y * 0.4),
				                sin(world.z * 0.13 - _Time.y * 0.35)) * 0.5;
				float e = 0.1;
				float3 n = normalize(float3(HeightAt(world.xz - float2(e, 0)) - HeightAt(world.xz + float2(e, 0)),
				                            2.0 * e,
				                            HeightAt(world.xz - float2(0, e)) - HeightAt(world.xz + float2(0, e))));
				o.worldNormal = n;
				#endif
				world.y = h;
				o.worldPos = world;
				o.uv = v.uv;

				o.pos = UnityWorldToClipPos(world);
				TRANSFER_SHADOW(o);
				return o;
			}

			float TriWave(float x)
			{
				return abs(frac(x + 0.5) * 2.0 - 1.0);
			}

			// the original causticsMap is a 256^3 texture stored as a
			// 256 x 16384 atlas (64 slices across). Sample it like a 3D
			// texture with trilinear slice interpolation.
			float SampleCaustics3D(float3 uvw)
			{
				float slice = saturate(uvw.z) * 63.999;
				float s0 = floor(slice);
				float f = slice - s0;
				float2 uvA = float2(fmod(s0, 64.0), floor(s0 / 64.0)) / float2(64.0, 256.0);
				float2 uvB = float2(fmod(s0 + 1.0, 64.0), floor((s0 + 1.0) / 64.0)) / float2(64.0, 256.0);
				float2 inUV = saturate(uvw.xy);
				float a = tex2Dlod(causticsMap, float4(uvA + inUV / float2(64.0, 256.0), 0, 0)).r;
				float b = tex2Dlod(causticsMap, float4(uvB + inUV / float2(64.0, 256.0), 0, 0)).r;
				return lerp(a, b, f);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// ---- caustics (translated from the ps_5_0 disassembly) ----
				// sandMap drives the first triangular scroll, then the flow
				// tangent offsets the 3D caustic UVs, two samples blended.
				float3 sand = tex2D(sandMap, i.worldPos.xz * 0.3).rgb;
				float t = _Time.y * _causticsSpeed;
				float tri1 = TriWave(t * 0.5 + sand.g);
				float2 cuv = i.worldPos.xz * 0.25 + tri1 * i.flow * 0.1;
				float c1 = SampleCaustics3D(float3(cuv * 0.2 - 0.5, TriWave(t + 0.5)));
				float c2 = SampleCaustics3D(float3(cuv * 0.2 + 0.5, TriWave(t)));
				float tri2 = TriWave(t * 2.0);
				float caustic = lerp(c1, c2, tri2);

				// strength = exp(-1.731234 * wetness) * sun intensity
				caustic *= exp(-1.731234 * i.wetness) * length(_LightColor0.rgb) * _causticsIntensity;

				// ---- wet / dry / underwater colour ----
				float w = saturate(i.wetness * 52.63158);
				float w2 = w * w * (3.0 - 2.0 * w); // smoothstep
				float3 col = lerp(_sandColor.rgb, _wetSandColor.rgb, w2);
				col = lerp(col, _underwaterColor.rgb, saturate(i.depth * 20.0));

				// sandMap brightness modulation (original: col * sand - col)
				col += (col * sand.r - col) * 0.25;

				// ---- lighting: rebuilt GI lightmap + sun + fill ----
				float3 gi = tex2D(_GILightMap, i.uv).rgb * _giBoost;
				float3 N = normalize(i.worldNormal);
				float3 L = normalize(_WorldSpaceLightPos0.xyz);
				float ndl = max(dot(N, L), 0.0);
				float3 sun = ndl * _LightColor0.rgb * SHADOW_ATTENUATION(i);
				// fill from below/back like the original cb0[124/126] term
				float3 fill = max(dot(N, -normalize(_WorldSpaceLightPos0.xyz + float3(0, -0.5, 0))), 0.0)
				              * _LightColor0.rgb * 0.25;

				float3 lit = col * (gi + sun + fill);
				lit += caustic * col * 0.5; // caustics modulate albedo

				return fixed4(lit, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
