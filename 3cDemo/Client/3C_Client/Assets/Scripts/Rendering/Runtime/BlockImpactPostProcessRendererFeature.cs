using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class BlockImpactPostProcessRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        Material material;
        BlockImpactPostProcessRenderPass pass;
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
            BlockImpactPostProcessPulse.Tick(Time.unscaledDeltaTime);
            if (!ShouldEnqueue())
                return;

            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            BlockImpactPostProcessSettings settings = ResolveSettings();
            BlockImpactPostProcessPulseState pulse = BlockImpactPostProcessPulse.Current;
            if (!CanRender(settings, pulse))
                return;

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            pass.Configure(renderer.cameraColorTargetHandle, ref copyTexture, descriptor, settings, pulse);
        }

        public bool CanRender(BlockImpactPostProcessSettings settings, BlockImpactPostProcessPulseState pulse)
        {
            return material != null && pass != null && settings.IsActive && pulse.IsActive;
        }

        public bool ShouldEnqueue()
        {
            return CanRender(ResolveSettings(), BlockImpactPostProcessPulse.Current);
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
            pass = material != null ? new BlockImpactPostProcessRenderPass(material, injectionPoint) : null;
        }

        static BlockImpactPostProcessSettings ResolveSettings()
        {
            BlockImpactPostProcess blockImpact = VolumeManager.instance.stack?.GetComponent<BlockImpactPostProcess>();
            return blockImpact != null && blockImpact.IsActive()
                ? blockImpact.NormalizedSettings
                : BlockImpactPostProcessSettings.Disabled;
        }
    }
}
