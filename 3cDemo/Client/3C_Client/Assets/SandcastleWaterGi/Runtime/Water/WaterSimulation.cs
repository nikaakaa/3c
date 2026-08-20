using UnityEngine;

namespace SandcastleWaterGi
{
    /// <summary>
    /// Rebuilt GPU shallow-water simulation (mirrors the Sandcastle Demo
    /// Water layer: VelocityIntegration / HeightIntegration /
    /// UpdateTangent kernels). Reads the shared sand heightmap, publishes
    /// the cell buffer for the water shader via SetGlobalBuffer.
    /// </summary>
    [ExecuteAlways]
    public class WaterSimulation : MonoBehaviour
    {
        [Header("Grid")]
        public Vector2Int resolution = new Vector2Int(128, 128);
        public Vector2 worldSize = new Vector2(60f, 60f);
        public Vector2 originOffset = new Vector2(-30f, -30f);

        [Header("Heightmap (shared with GiLighting)")]
        public RenderTexture heightmap;

        [Header("Sand coupling")]
        public GroundSimulation groundSimulation;

        public ComputeBuffer CellsA => _cellsA;
        public bool IsReady => _initialized;

        [Header("Water (original Water class fields)")]
        [Range(0f, 20f)] public float gravityForce = 4f;
        [Range(0f, 2f)] public float evaporationFactor = 0.05f;
        [Range(0f, 10f)] public float damping = 0.8f;
        public float waterLevel = 0.35f;
        public float maxVelocity = 4f;

        [Header("Waves")]
        public bool enableWaves = true;
        public float waveAmplitude = 0.05f;
        public float waveSpeed = 0.8f;

        public ComputeShader compute;

        ComputeBuffer _cellsA;
        ComputeBuffer _cellsB;
        int _kInit, _kVel, _kHeight, _kTangent, _kAdv, _kFloor, _kBlur;
        float _phase;
        bool _initialized;

        void OnEnable()
        {
            if (compute == null)
            {
                Shader.DisableKeyword("_SIM_BUFFER");
                return;
            }
            Shader.EnableKeyword("_SIM_BUFFER");
            _kInit = compute.FindKernel("Initialize");
            _kVel = compute.FindKernel("VelocityIntegration");
            _kHeight = compute.FindKernel("HeightIntegration");
            _kTangent = compute.FindKernel("UpdateTangent");
            _kAdv = compute.FindKernel("Advection");
            _kFloor = compute.FindKernel("UpdateFloor");
            _kBlur = compute.FindKernel("BlurFloor");
            Allocate();
            DispatchInit();
            _initialized = true;
        }

        void OnDisable()
        {
            Shader.DisableKeyword("_SIM_BUFFER");
            _initialized = false;
            if (_cellsA != null) { _cellsA.Release(); _cellsA = null; }
            if (_cellsB != null) { _cellsB.Release(); _cellsB = null; }
        }

        void Allocate()
        {
            int count = resolution.x * resolution.y;
            if (_cellsA == null || _cellsA.count != count)
            {
                if (_cellsA != null) _cellsA.Release();
                if (_cellsB != null) _cellsB.Release();
                _cellsA = new ComputeBuffer(count, 16);
                _cellsB = new ComputeBuffer(count, 16);
                DispatchInit();
            }
        }

        void DispatchInit()
        {
            if (compute == null || _cellsA == null) return;
            compute.SetBuffer(_kInit, "_CellsA", _cellsA);
            compute.SetBuffer(_kInit, "_CellsB", _cellsB);
            if (heightmap != null)
                compute.SetTexture(_kInit, "_Heightmap", heightmap);
            compute.SetFloat("_WaterLevel", waterLevel);
            compute.SetInts("_Resolution", resolution.x, resolution.y);
            compute.Dispatch(_kInit, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
        }

        void Update()
        {
            // compute may be assigned after AddComponent (e.g. SampleBeach)
            if (!_initialized && compute != null)
                OnEnable();
            if (!_initialized || compute == null) return;
            Allocate();

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _phase += dt * waveSpeed;
            if (_phase > 1000f) _phase -= 1000f;

            Vector2 res = new Vector2(resolution.x, resolution.y);
            ComputeBuffer[] buffers = { _cellsA, _cellsB };

            foreach (int k in new[] { _kVel, _kHeight, _kTangent })
            {
                compute.SetBuffer(k, "_CellsA", _cellsA);
                compute.SetBuffer(k, "_CellsB", _cellsB);
                if (heightmap != null)
                    compute.SetTexture(k, "_Heightmap", heightmap);
                compute.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
                compute.SetVector("_OriginOffset", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
                compute.SetInts("_Resolution", resolution.x, resolution.y);
                compute.SetFloat("_Gravity", gravityForce);
                compute.SetFloat("_Evaporation", evaporationFactor);
                compute.SetFloat("_Damping", damping);
                compute.SetFloat("_DeltaTime", dt);
                compute.SetFloat("_MaxVelocity", maxVelocity);
                compute.SetFloat("_WaterLevel", waterLevel);
                compute.SetFloat("_WaveAmplitude", enableWaves ? waveAmplitude : 0f);
                compute.SetFloat("_WavePhase", _phase);
            }

            compute.Dispatch(_kVel, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
            compute.Dispatch(_kHeight, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);

            // sand coupling: read the ground simulation into the water floor
            if (groundSimulation != null && groundSimulation.IsReady && groundSimulation.CellsA != null)
            {
                foreach (int k in new[] { _kFloor, _kBlur })
                {
                    compute.SetBuffer(k, "_CellsA", _cellsA);
                    compute.SetBuffer(k, "_CellsB", _cellsB);
                    compute.SetBuffer(k, "_GroundCells", groundSimulation.CellsA);
                    compute.SetInts("_Resolution", resolution.x, resolution.y);
                    compute.SetInts("_GroundRes", groundSimulation.resolution.x, groundSimulation.resolution.y);
                }
                compute.Dispatch(_kFloor, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
                compute.Dispatch(_kBlur, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);
            }

            // semi-Lagrangian advection of the velocity field
            compute.SetBuffer(_kAdv, "_CellsA", _cellsA);
            compute.SetInts("_Resolution", resolution.x, resolution.y);
            compute.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            compute.SetFloat("_DeltaTime", dt);
            compute.Dispatch(_kAdv, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);

            compute.Dispatch(_kTangent, Mathf.CeilToInt(resolution.x / 32f), Mathf.CeilToInt(resolution.y / 32f), 1);

            // publish for the water shader
            Shader.SetGlobalBuffer("_WaterCellsA", _cellsA);
            Shader.SetGlobalBuffer("_WaterCellsB", _cellsB);
            Shader.SetGlobalVector("_WaterSimRes", new Vector4(resolution.x, resolution.y, 0f, 0f));
            Shader.SetGlobalVector("_WaterSimSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            Shader.SetGlobalVector("_WaterSimOrigin", new Vector4(originOffset.x, originOffset.y, 0f, 0f));
            Shader.SetGlobalFloat("_WaterSimLevel", waterLevel);
        }
    }
}
