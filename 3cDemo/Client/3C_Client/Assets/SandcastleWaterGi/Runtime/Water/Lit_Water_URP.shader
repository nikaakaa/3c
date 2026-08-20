// ============================================================
// Lit/Water URP version (for Unity 2022.3 URP14 / Unity 6000 URP17)
// Same rebuilt algorithm as Lit_Water.shader, but uses the URP
// opaque texture for refraction (SampleSceneColor) and the URP
// main light (GetMainLight). Selects automatically when the
// project uses the Universal Render Pipeline.
// ============================================================
Shader "SandcastleWaterGi/WaterURP"
{
	Properties
	{
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
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _SIM_BUFFER
			#pragma target 4.5
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			TEXTURE2D(waterNormalMap);       SAMPLER(sampler_waterNormalMap);
			TEXTURE2D(_GiHeightmap);         SAMPLER(sampler_GiHeightmap);
			float4 _GiWorldSize;
			float4 _GiOriginOffset;

			#ifdef _SIM_BUFFER
			StructuredBuffer<float4> _WaterCellsA;
			StructuredBuffer<float4> _WaterCellsB;
			float4 _WaterSimRes;
			float4 _WaterSimSize;
			float4 _WaterSimOrigin;
			float _WaterSimLevel;
			#endif

			CBUFFER_START(UnityPerMaterial)
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
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float waveY : TEXCOORD2;
			};

			float WaterHeight(float2 xz, float t)
			{
				float2 k = xz * _waveFrequency;
				float h = sin(k.x + t * _waveSpeed) * _waveAmplitude;
				h += sin(k.y * 1.7 - t * _waveSpeed * 0.75) * _waveAmplitude * 0.6;
				h += sin(dot(k, float2(1.0, 0.6)) * 0.5 + t * _waveSpeed * 1.3) * _waveAmplitude * 0.3;
				return h;
			}

			#ifdef _SIM_BUFFER
			float SimHeightAt(float2 xz)
			{
				float2 uv = (xz - _WaterSimOrigin.xy) / _WaterSimSize.xy;
				int2 c = clamp(int2(uv * _WaterSimRes.xy), int2(0, 0), int2(_WaterSimRes.xy) - 1);
				return _WaterCellsA[c.y * (int)_WaterSimRes.x + c.x].x;
			}
			#endif

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				float3 world = TransformObjectToWorld(IN.positionOS.xyz);
				float h;
				#ifdef _SIM_BUFFER
				float2 cellSize = _WaterSimSize.xy / _WaterSimRes.xy;
				h = (SimHeightAt(world.xz + float2(-1, 0) * cellSize)
				   + SimHeightAt(world.xz + float2(1, 0) * cellSize)
				   + SimHeightAt(world.xz + float2(0, -1) * cellSize)
				   + SimHeightAt(world.xz + float2(0, 1) * cellSize)) * 0.25;
				float2 suv = (world.xz - _WaterSimOrigin.xy) / _WaterSimSize.xy;
				int2 sc = clamp(int2(suv * _WaterSimRes.xy), int2(0, 0), int2(_WaterSimRes.xy) - 1);
				float4 simB = _WaterCellsB[sc.y * (int)_WaterSimRes.x + sc.x];
				OUT.normalWS = normalize(float3(-simB.x, 1.0, -simB.y));
				#else
				h = _waterLevel + WaterHeight(world.xz, _Time.y);
				float e = 0.08;
				float hx1 = WaterHeight(world.xz + float2(e, 0), _Time.y);
				float hx0 = WaterHeight(world.xz - float2(e, 0), _Time.y);
				float hz1 = WaterHeight(world.xz + float2(0, e), _Time.y);
				float hz0 = WaterHeight(world.xz - float2(0, e), _Time.y);
				OUT.normalWS = normalize(float3(hx0 - hx1, 2.0 * e, hz0 - hz1));
				#endif
				world.y = h;
				OUT.waveY = h;
				OUT.positionWS = world;
				OUT.positionCS = TransformWorldToHClip(world);
				return OUT;
			}

			float TriWave(float x) { return abs(frac(x + 0.5) * 2.0 - 1.0); }

			half4 frag(Varyings IN) : SV_Target
			{
				float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);

				// two-layer scrolling normal map (original manual unpack)
				float scroll = _Time.y * _waveSpeed;
				float2 t1 = float2(TriWave(scroll * 0.8), TriWave(scroll * 0.62)) * 2.0 - 1.0;
				float2 uv1 = IN.positionWS.xz * 0.05 + t1 * 0.25;
				float2 uv2 = IN.positionWS.xz * -0.035 + t1.yx * 0.25 - 0.25;
				float3 nrm1 = SAMPLE_TEXTURE2D(waterNormalMap, sampler_waterNormalMap, uv1).rgb * 2.0 - 1.0;
				float3 nrm2 = SAMPLE_TEXTURE2D(waterNormalMap, sampler_waterNormalMap, uv2).rgb * 2.0 - 1.0;
				float3 nrm = normalize(float3(nrm1.xy + nrm2.xy * 0.6, nrm1.z + nrm2.z * 0.6));
				float3 N = normalize(float3(IN.normalWS.x + nrm.x * 0.35, IN.normalWS.y, IN.normalWS.z + nrm.y * 0.35));

				// screen-space refraction through the URP opaque texture
				float2 screenUV = IN.positionCS.xy * _ScreenParams.zw;
				float2 grabUV = screenUV + nrm.xz * refractionFactor;
				half3 refr = SampleSceneColor(grabUV).rgb;

				// underwater distance via the URP depth texture
				float sceneDepth = SampleSceneDepth(screenUV);
				float eyeDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
				float3 underwaterPos = ComputeWorldSpacePosition(screenUV, sceneDepth, UNITY_MATRIX_I_VP);
				float waterDist = length(underwaterPos - IN.positionWS);

				// heightfield fallback when the depth texture misses
				float bedH = 0.0;
				if (_GiWorldSize.x > 0.001)
					bedH = SAMPLE_TEXTURE2D(_GiHeightmap, sampler_GiHeightmap,
						(IN.positionWS.xz - _GiOriginOffset.xy) / _GiWorldSize.xy).r;
				float waterDepth = max(_waterLevel - bedH, 0.001);
				if (eyeDepth > _ProjectionParams.z - 10.0) waterDist = waterDepth;
				waterDist = max(waterDist, 0.001);

				// Beer-Lambert absorption
				float3 absorb = exp(-waterDist * 8.656 * _waterColor.rgb);
				float3 underwater = refr * _waterScatteringColor.rgb * absorb;
				underwater = lerp(_sandColor.rgb, underwater, saturate(IN.waveY / 0.15));

				// Fresnel + Blinn-Phong with the URP main light
				Light mainLight = GetMainLight();
				float fresnel = 0.75 * pow(1.0 - saturate(dot(N, viewDir)), _fresnelPower);
				float3 H = normalize(mainLight.direction + viewDir);
				float spec = pow(saturate(dot(N, H)), _smoothness);
				spec *= fresnel * mainLight.shadowAttenuation;
				float3 col = underwater + spec * _specColor.rgb;

				// shallow-water fade
				float shallow = saturate(waterDepth * 500.0);
				float fade = smoothstep(0.0, 0.3, waterDepth * 2.0 - 0.5) * shallow;
				if (_GiWorldSize.x <= 0.001)
				{
					shallow = saturate(IN.waveY * 500.0);
					fade = smoothstep(0.0, 0.3, IN.waveY * 2.0 - 0.5) * shallow;
				}
				return half4(col, fade);
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}
