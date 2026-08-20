// ============================================================
// Lit/Water (package version)
// Rebuilt from disassembly of the shipped SM5.0 bytecode
// (Sandcastle Demo, Bubblebird Studio, Unity 6000.4.10f1).
// Original pipeline: URP + GPU shallow-water compute sim.
// This build: built-in RP (GrabPass refraction), procedural waves.
// ============================================================
Shader "Lit/Water" {
	Properties {
		refractionFactor ("Refraction factor", Range(0, 0.1)) = 0.0121
		[NoScaleOffset] waterNormalMap ("Water normal", 2D) = "bump" {}
		_waterColor ("Water color (absorb)", Color) = (0.9603443, 0.9857986, 0.99954206, 1)
		_waterScatteringColor ("Water scattering", Color) = (0.56838256, 0.35686398, 0, 1)
		_sandColor ("Sand color", Color) = (0.25, 0.5, 0.5, 1)
		_waterLevel ("Water level", Float) = 0
		_waveAmplitude ("Wave amplitude", Float) = 0.04
		_waveFrequency ("Wave frequency", Float) = 0.35
		_waveSpeed ("Wave speed", Float) = 0.9
		_smoothness ("Specular exponent", Float) = 1500
		_fresnelPower ("Fresnel power", Float) = 5
		_specColor ("Specular color", Color) = (1, 1, 1, 1)
		_absorbDistance ("Absorption distance", Float) = 1.2
		[Toggle(_USE_SCENE_DEPTH)] _UseSceneDepth ("Use scene depth", Float) = 0
		[Toggle(_ALPHA_BLEND)] _AlphaBlend ("Alpha blend", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
		GrabPass { "_WaterGrab" }

		Pass {
			Name "FORWARD"
			Tags { "LightMode"="ForwardBase" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma shader_feature _USE_SCENE_DEPTH
			#pragma shader_feature _ALPHA_BLEND
			#pragma shader_feature _SIM_BUFFER
			#pragma target 4.5
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D waterNormalMap;
			sampler2D _WaterGrab;
			float4 _WaterGrab_TexelSize;
			sampler2D _CameraDepthTexture;
			sampler2D _GiHeightmap;   // optional: supplied by SandcastleWaterGi.GiLighting
			float4 _GiWorldSize;
			float4 _GiOriginOffset;

			// shallow-water simulation buffers (WaterSimulation component)
			#ifdef _SIM_BUFFER
			StructuredBuffer<float4> _WaterCellsA; // x=height y=vx z=vy w=wave
			StructuredBuffer<float4> _WaterCellsB; // xy=tangent z=floor w=crest
			float4 _WaterSimRes;
			float4 _WaterSimSize;
			float4 _WaterSimOrigin;
			float _WaterSimLevel;
			#endif

			float refractionFactor;
			float4 _waterColor;
			float4 _waterScatteringColor;
			float4 _sandColor;
			float _waterLevel;
			float _waveAmplitude;
			float _waveFrequency;
			float _waveSpeed;
			float _smoothness;
			float _fresnelPower;
			float4 _specColor;
			float _absorbDistance;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float4 grabUV : TEXCOORD2;
				float3 rayDir : TEXCOORD3;
				float waveY : TEXCOORD4;
				SHADOW_COORDS(5)
			};

			// Procedural waves stand in for the original GPU shallow-water
			// simulation (see GiLighting.compute for the rebuilt GI kernels).
			float WaterHeight(float2 xz, float t) {
				float2 k = xz * _waveFrequency;
				float h = sin(k.x + t * _waveSpeed) * _waveAmplitude;
				h += sin(k.y * 1.7 - t * _waveSpeed * 0.75) * _waveAmplitude * 0.6;
				h += sin(dot(k, float2(1.0, 0.6)) * 0.5 + t * _waveSpeed * 1.3) * _waveAmplitude * 0.3;
				return h;
			}

			#ifdef _SIM_BUFFER
			// sample the simulated heightfield (4-neighbour average, like the
			// original vertex shader) and the simulated tangent
			float SimHeightAt(float2 xz)
			{
				float2 uv = (xz - _WaterSimOrigin.xy) / _WaterSimSize.xy;
				int2 c = clamp(int2(uv * _WaterSimRes.xy), int2(0, 0), int2(_WaterSimRes.xy) - 1);
				return _WaterCellsA[c.y * (int)_WaterSimRes.x + c.x].x;
			}
			#endif

			v2f vert (appdata v) {
				v2f o;
				float3 world = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
				float h;
				#ifdef _SIM_BUFFER
				h = (SimHeightAt(world.xz + float2(-1, 0) * (_WaterSimSize.x / _WaterSimRes.x))
				   + SimHeightAt(world.xz + float2(1, 0) * (_WaterSimSize.x / _WaterSimRes.x))
				   + SimHeightAt(world.xz + float2(0, -1) * (_WaterSimSize.y / _WaterSimRes.y))
				   + SimHeightAt(world.xz + float2(0, 1) * (_WaterSimSize.y / _WaterSimRes.y))) * 0.25;
				float2 suv = (world.xz - _WaterSimOrigin.xy) / _WaterSimSize.xy;
				int2 sc = clamp(int2(suv * _WaterSimRes.xy), int2(0, 0), int2(_WaterSimRes.xy) - 1);
				float4 simB = _WaterCellsB[sc.y * (int)_WaterSimRes.x + sc.x];
				float2 simT = simB.xy;
				o.worldNormal = normalize(float3(-simT.x, 1.0, -simT.y));
				#else
				h = _waterLevel + WaterHeight(world.xz, _Time.y);
				float e = 0.08;
				float hx1 = WaterHeight(world.xz + float2(e, 0), _Time.y);
				float hx0 = WaterHeight(world.xz - float2(e, 0), _Time.y);
				float hz1 = WaterHeight(world.xz + float2(0, e), _Time.y);
				float hz0 = WaterHeight(world.xz - float2(0, e), _Time.y);
				o.worldNormal = normalize(float3(hx0 - hx1, 2.0 * e, hz0 - hz1));
				#endif
				o.waveY = h;
				world.y = h;
				o.worldPos = world;
				o.rayDir = world - _WorldSpaceCameraPos;

				o.pos = UnityWorldToClipPos(world);
				o.grabUV = ComputeGrabScreenPos(o.pos);
				TRANSFER_SHADOW(o);
				return o;
			}

			float TriWave(float x) {
				return abs(frac(x + 0.5) * 2.0 - 1.0);
			}

			fixed4 frag (v2f i) : SV_Target {
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

				// two-layer scrolling normal map (from ps_5_0 disassembly)
				float scroll = _Time.y * _waveSpeed;
				float2 t1 = float2(TriWave(scroll * 0.8), TriWave(scroll * 0.62)) * 2.0 - 1.0;
				float2 uv1 = i.worldPos.xz * 0.05 + t1 * 0.25;
				float2 uv2 = i.worldPos.xz * -0.035 + t1.yx * 0.25 - 0.25;
				// original unpacks manually (tex * 2 - 1) because the shipped
				// normal map is not flagged as a normal map on import
				float3 nrm1 = tex2D(waterNormalMap, uv1).rgb * 2.0 - 1.0;
				float3 nrm2 = tex2D(waterNormalMap, uv2).rgb * 2.0 - 1.0;
				float3 nrm = normalize(float3(nrm1.xy + nrm2.xy * 0.6, nrm1.z + nrm2.z * 0.6));

				float3 N = normalize(float3(i.worldNormal.x + nrm.x * 0.35, i.worldNormal.y, i.worldNormal.z + nrm.y * 0.35));

				// refraction (grab pass)
				float2 grabUV = i.grabUV.xy / i.grabUV.w + nrm.xz * refractionFactor;
				fixed4 refr = tex2D(_WaterGrab, grabUV);

				// distance from water surface to the underwater point
				float waterDist = _absorbDistance;
				#ifdef _USE_SCENE_DEPTH
				float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.grabUV.xy / i.grabUV.w));
				float3 underwaterPos = _WorldSpaceCameraPos + normalize(i.rayDir) * sceneDepth;
				waterDist = max(length(underwaterPos - i.worldPos), 0.001);
				#endif

				// optional heightfield: water depth = level - bed height
				float bedH = 0.0;
				if (_GiWorldSize.x > 0.001)
					bedH = tex2D(_GiHeightmap, (i.worldPos.xz - _GiOriginOffset.xy) / _GiWorldSize.xy).r;
				float waterDepth = max(_waterLevel - bedH, 0.001);
				#ifndef _USE_SCENE_DEPTH
				if (_GiWorldSize.x > 0.001) waterDist = waterDepth;
				#endif

				// Beer-Lambert absorption (from disassembly)
				float3 absorb = exp(-waterDist * 8.656 * _waterColor.rgb);
				float3 underwater = refr.rgb * _waterScatteringColor.rgb * absorb;

				// shallow water reveals the sand
				underwater = lerp(_sandColor.rgb, underwater, saturate(i.waveY / 0.15));

				// Fresnel + Blinn-Phong specular (pow 1500 in original)
				float fresnel = 0.75 * pow(1.0 - saturate(dot(N, viewDir)), _fresnelPower);
				float3 L = normalize(_WorldSpaceLightPos0.xyz);
				float3 H = normalize(L + viewDir);
				float spec = pow(saturate(dot(N, H)), _smoothness);
				spec *= fresnel * SHADOW_ATTENUATION(i);
				float3 col = underwater + spec * _specColor.rgb;

				// shallow-water fade (from disassembly); uses the heightfield
				// depth when available, otherwise the wave height
				float shallow = saturate(waterDepth * 500.0);
				float fade = smoothstep(0.0, 0.3, waterDepth * 2.0 - 0.5) * shallow;
				if (_GiWorldSize.x <= 0.001) {
					shallow = saturate(i.waveY * 500.0);
					fade = smoothstep(0.0, 0.3, i.waveY * 2.0 - 0.5) * shallow;
				}

				#ifdef _ALPHA_BLEND
				return fixed4(col, fade);
				#else
				return fixed4(col, 1.0);
				#endif
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
