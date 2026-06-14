using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class EdgeScanRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        Material material;
        EdgeScanRenderPass pass;
        RTHandle copyTexture;

        public Shader Shader
        {
            get => shader;
            set
            {
                if (shader == value)
                    return;

                shader = value;
                RebuildResources();
            }
        }

        public RenderPassEvent InjectionPoint => injectionPoint;
        public bool HasMaterial => material != null;
        public bool HasPass => pass != null;

        public override void Create()
        {
            RebuildResources();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldEnqueue())
                return;

            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            EdgeScanSettings settings = ResolveSettings();
            if (!CanRender(settings))
                return;

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            pass.Configure(renderer.cameraColorTargetHandle, ref copyTexture, descriptor, settings);
        }

        public bool CanRender(EdgeScanSettings settings)
        {
            return material != null && pass != null && settings.IsActive;
        }

        public bool ShouldEnqueue()
        {
            return CanRender(ResolveSettings());
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            copyTexture?.Release();
            material = null;
            pass = null;
            copyTexture = null;
        }

        void RebuildMaterial()
        {
            CoreUtils.Destroy(material);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
        }

        void RebuildResources()
        {
            RebuildMaterial();
            pass = material != null ? new EdgeScanRenderPass(material, injectionPoint) : null;
        }

        static EdgeScanSettings ResolveSettings()
        {
            EdgeScan edgeScan = VolumeManager.instance.stack?.GetComponent<EdgeScan>();
            return edgeScan != null && edgeScan.IsActive()
                ? edgeScan.NormalizedSettings
                : EdgeScanSettings.Disabled;
        }
    }
}
