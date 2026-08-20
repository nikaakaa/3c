using UnityEngine;

namespace SandcastleWaterGi
{
    /// <summary>
    /// Rebuilt sand-pile simulation (mirrors the Sandcastle Demo Ground
    /// layer: velocity/height integration with noise, wetness mobility
    /// and angle of repose). Publishes _GroundA/B buffers and enables
    /// the _GROUND_BUFFER shader keyword for the Lit/Sand shader.
    /// </summary>
    [ExecuteAlways]
    public class GroundSimulation : MonoBehaviour
    {
        [Header("Grid")]
        public Vector2Int resolution = new Vector2Int(256, 256);
        public Vector2 worldSize = new Vector2(60f, 60f);
        public Vector2 originOffset = new Vector2(-30f, -30f);

        [Header("Heightmap (shared with GiLighting / WaterSimulation)")]
        public RenderTexture heightmap;

        [Header("Water coupling (erosion)")]
        public WaterSimulation waterSimulation;
        [Range(0f, 4f)] public float erosionFactor = 0.6f;
        [Range(0f, 1f)] public float sedimentCapacity = 0.2f;

        public ComputeBuffer CellsA => _groundA;
        public bool IsReady => _initialized;

        [Header("Sand (original Ground class fields)")]
        [Range(0f, 20f)] public float gravityForce = 6f;
        [Range(0f, 0.001f)] public float noiseFactor = 0.0003f;
        [Range(0.1f, 2f)] public float reposeAngle = 0.58f;
        [Range(0.1f, 4f)] public float wetMobility = 1.5f;
        public float maxVelocity = 2f;
        public float waterLevel = 0.35f;
        [Range(0.1f, 20f)] public float dryRate = 2f;

        public ComputeShader compute;

        ComputeBuffer _groundA;
        ComputeBuffer _groundB;
        int _kInit, _kVel, _kHeight, _kTangent, _kErosion;
        bool _initialized;

        void OnEnable()
        {
            if (compute == null)
            {
                Shader.DisableKeyword("_GROUND_BUFFER");
                return;
            }
            Shader.EnableKeyword("_GROUND_BUFFER");
            _kInit = compute.FindKernel("Initialize");
            _kVel = compute.FindKernel("VelocityIntegration");
            _kHeight = compute.FindKernel("HeightIntegration");
            _kTangent = compute.FindKernel("UpdateTangent");
            _kErosion = compute.FindKernel("Erosion");
            Allocate();
            DispatchInit();
            _initialized = true;
        }

        void OnDisable()
        {
            Shader.DisableKeyword("_GROUND_BUFFER");
            _initialized = false;
            if (_groundA != null) { _groundA.Release(); _groundA = null; }
            if (_groundB != null) { _groundB.Release(); _groundB = null; }
        }

        void Allocate()
        {
            int count = resolution.x * resolution.y;
            if (_groundA == null || _groundA.count != count)
            {
                if (_groundA != null) _groundA.Release();
                if (_groundB != null) _groundB.Release();
                _groundA = new ComputeBuffer(count, 16);
                _groundB = new ComputeBuffer(count, 16);
                DispatchInit();
            }
        }

        void DispatchInit()
        {
            if (compute == null || _groundA == null) return;
            compute.SetBuffer(_kInit, "_GroundA", _groundA);
            compute.SetBuffer(_kInit, "_GroundB", _groundB);
            if (heightmap != null)
                compute.SetTexture(_kInit, "_Heightmap", heightmap);
            compute.SetFloat("_WaterLevel", waterLevel);
            compute.SetInts("_Resolution", resolution.x, resolution.y);
            compute.Dispatch(_kInit, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
        }

        void Update()
        {
            if (!_initialized && compute != null)
                OnEnable();
            if (!_initialized || compute == null) return;
            Allocate();

            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            foreach (int k in new[] { _kVel, _kHeight, _kTangent })
            {
                compute.SetBuffer(k, "_GroundA", _groundA);
                compute.SetBuffer(k, "_GroundB", _groundB);
                if (heightmap != null)
                    compute.SetTexture(k, "_Heightmap", heightmap);
                compute.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
                compute.SetVector("_OriginOffset", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
                compute.SetInts("_Resolution", resolution.x, resolution.y);
                compute.SetFloat("_Gravity", gravityForce);
                compute.SetFloat("_NoiseFactor", noiseFactor);
                compute.SetFloat("_ReposeAngle", reposeAngle);
                compute.SetFloat("_WetMobility", wetMobility);
                compute.SetFloat("_DeltaTime", dt);
                compute.SetFloat("_MaxVelocity", maxVelocity);
                compute.SetFloat("_WaterLevel", waterLevel);
                compute.SetFloat("_DryRate", dryRate);
            }

            compute.Dispatch(_kVel, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
            compute.Dispatch(_kHeight, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);

            // water erosion: read the water simulation velocity
            if (waterSimulation != null && waterSimulation.IsReady && waterSimulation.CellsA != null)
            {
                compute.SetBuffer(_kErosion, "_GroundA", _groundA);
                compute.SetBuffer(_kErosion, "_GroundB", _groundB);
                compute.SetBuffer(_kErosion, "_WaterCells", waterSimulation.CellsA);
                if (heightmap != null)
                    compute.SetTexture(_kErosion, "_Heightmap", heightmap);
                compute.SetInts("_Resolution", resolution.x, resolution.y);
                compute.SetInts("_WaterRes", waterSimulation.resolution.x, waterSimulation.resolution.y);
                compute.SetFloat("_DeltaTime", dt);
                compute.SetFloat("_ErosionFactor", erosionFactor);
                compute.SetFloat("_SedimentCapacity", sedimentCapacity);
                compute.Dispatch(_kErosion, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
            }

            compute.Dispatch(_kTangent, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);

            Shader.SetGlobalBuffer("_GroundA", _groundA);
            Shader.SetGlobalBuffer("_GroundB", _groundB);
            Shader.SetGlobalVector("_GroundSimRes", new Vector4(resolution.x, resolution.y, 0f, 0f));
            Shader.SetGlobalVector("_GroundSimSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            Shader.SetGlobalVector("_GroundSimOrigin", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
        }
    }
}
