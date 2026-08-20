using UnityEngine;

namespace SandcastleWaterGi
{
    /// <summary>
    /// Rebuilt heightfield GI. Mirrors the Sandcastle Demo Gi system:
    /// per frame it raymarches the heightmap (DDA) for every cell,
    /// integrates horizon occlusion + sky/bounce colour, blurs the
    /// lightmap and publishes it as the global texture _GILightMap.
    /// Also publishes the heightmap as _GiHeightmap for terrain shaders.
    /// </summary>
    [ExecuteAlways]
    public class GiLighting : MonoBehaviour
    {
        [Header("Heightmap")]
        [Tooltip("RGBA: R = world height, GBA = albedo colour")]
        public RenderTexture heightmap;
        public Vector2 worldSize = new Vector2(50f, 50f);
        public Vector2 originOffset = new Vector2(-25f, -25f);

        [Header("Sun & Sky")]
        public Vector3 sunDirection = new Vector3(0.5f, 1f, 0.3f);
        public Color sunColor = new Color(1f, 0.95f, 0.85f);
        public Color groundColor = new Color(0.25f, 0.35f, 0.45f);

        [Header("Quality")]
        [Range(1, 64)] public int sampleCount = 12;
        [Range(16, 512)] public int maxSteps = 128;
        [Range(0f, 1f)] public float temporalBlend = 0.6f;

        public ComputeShader compute;

        RenderTexture _lightMap;
        RenderTexture _lightMapBlurred;
        ComputeBuffer _raycastResults;
        int _kernelRaycast, _kernelGi, _kernelBlurGi;
        bool _initialized;

        void OnEnable()
        {
            if (compute == null)
                compute = Resources.Load<ComputeShader>("GiLighting");
            if (compute == null) return;
            _kernelRaycast = compute.FindKernel("Raycast");
            _kernelGi = compute.FindKernel("Gi");
            _kernelBlurGi = compute.FindKernel("BlurGi");
            Allocate();
            _initialized = true;
        }

        void OnDisable()
        {
            _initialized = false;
            if (_raycastResults != null) { _raycastResults.Release(); _raycastResults = null; }
            if (_lightMap != null) { _lightMap.Release(); _lightMap = null; }
            if (_lightMapBlurred != null) { _lightMapBlurred.Release(); _lightMapBlurred = null; }
        }

        void Allocate()
        {
            if (heightmap == null) return;
            int w = heightmap.width, h = heightmap.height;
            if (_lightMap == null || _lightMap.width != w || _lightMap.height != h)
            {
                if (_lightMap != null) _lightMap.Release();
                if (_lightMapBlurred != null) _lightMapBlurred.Release();
                _lightMap = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat)
                { enableRandomWrite = true, filterMode = FilterMode.Bilinear };
                _lightMap.Create();
                _lightMapBlurred = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat)
                { enableRandomWrite = true, filterMode = FilterMode.Bilinear };
                _lightMapBlurred.Create();
            }
            int count = w * h;
            if (_raycastResults == null || _raycastResults.count != count)
            {
                if (_raycastResults != null) _raycastResults.Release();
                _raycastResults = new ComputeBuffer(count, 16); // stride 16 like the original
            }
        }

        void Update()
        {
            // compute may be assigned after AddComponent (e.g. SampleBeach)
            if (!_initialized && compute != null)
                OnEnable();
            if (!_initialized || heightmap == null) return;
            Allocate();

            sunDirection = sunDirection.normalized;
            if (sunDirection.sqrMagnitude < 0.01f) sunDirection = new Vector3(0.5f, 1f, 0.3f);

            Vector2 res = new Vector2(heightmap.width, heightmap.height);

            // --- Raycast (sun-occlusion query; the Gi kernel also marches
            //     its own rays, this one mirrors the original extra kernel) ---
            compute.SetBuffer(_kernelRaycast, "_RaycastResults", _raycastResults);
            compute.SetTexture(_kernelRaycast, "_Heightmap", heightmap);
            compute.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            compute.SetVector("_OriginOffset", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
            compute.SetInts("_Resolution", heightmap.width, heightmap.height);
            compute.SetInt("_MaxSteps", maxSteps);
            compute.SetVector("_SunDirection", new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
            compute.Dispatch(_kernelRaycast, Mathf.CeilToInt(heightmap.width * heightmap.height / 64f), 1, 1);

            // --- Gi ---
            compute.SetTexture(_kernelGi, "_Heightmap", heightmap);
            compute.SetTexture(_kernelGi, "_GILightMap", _lightMap);
            compute.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            compute.SetVector("_OriginOffset", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
            compute.SetInts("_Resolution", heightmap.width, heightmap.height);
            compute.SetInt("_SampleCount", sampleCount);
            compute.SetInt("_MaxSteps", maxSteps);
            compute.SetFloat("_TemporalBlend", temporalBlend);
            compute.SetVector("_SunDirection", new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
            compute.SetVector("_SunColor", new Vector4(sunColor.r, sunColor.g, sunColor.b, 1f));
            compute.SetVector("_GroundColor", new Vector4(groundColor.r, groundColor.g, groundColor.b, 1f));
            compute.Dispatch(_kernelGi, Mathf.CeilToInt(heightmap.width / 32f), Mathf.CeilToInt(heightmap.height / 32f), 1);

            // --- BlurGi ---
            compute.SetTexture(_kernelBlurGi, "_GILightMap", _lightMap);
            compute.SetTexture(_kernelBlurGi, "_GILightMapBlurred", _lightMapBlurred);
            compute.SetInts("_Resolution", heightmap.width, heightmap.height);
            compute.Dispatch(_kernelBlurGi, Mathf.CeilToInt(heightmap.width / 32f), Mathf.CeilToInt(heightmap.height / 32f), 1);

            // --- publish for shaders ---
            Shader.SetGlobalTexture("_GILightMap", _lightMapBlurred);
            Shader.SetGlobalTexture("_GiHeightmap", heightmap);
            Shader.SetGlobalVector("_GiWorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            Shader.SetGlobalVector("_GiOriginOffset", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
        }
    }
}
