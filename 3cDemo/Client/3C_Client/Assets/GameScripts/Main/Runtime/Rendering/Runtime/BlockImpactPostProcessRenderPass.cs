using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class BlockImpactPostProcessRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_BlockImpactCopyTexture";
        static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        static readonly int BlockImpactParamsId = Shader.PropertyToID("_BlockImpactParams");
        static readonly int BlockImpactEffectParamsId = Shader.PropertyToID("_BlockImpactEffectParams");
        static readonly int BlockImpactStreakParamsId = Shader.PropertyToID("_BlockImpactStreakParams");
        static readonly int BlockImpactStreakColorId = Shader.PropertyToID("_BlockImpactStreakColor");
        static readonly int BlockImpactSampleCountId = Shader.PropertyToID("_BlockImpactSampleCount");
        static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        readonly Material material;
        readonly ProfilingSampler profilingSampler = new ProfilingSampler("3C Block Impact");
        RTHandle source;
        RTHandle copyTexture;
        BlockImpactPostProcessSettings settings;
        BlockImpactPostProcessPulseState pulse;

        public BlockImpactPostProcessRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
            ConfigureInput(RequiredInputs);
        }

        public static ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.Color;
        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive && pulse.IsActive;

        public void Configure(
            RTHandle cameraColorTarget,
            ref RTHandle copyTarget,
            RenderTextureDescriptor descriptor,
            BlockImpactPostProcessSettings postProcessSettings,
            BlockImpactPostProcessPulseState pulseState)
        {
            source = cameraColorTarget;
            settings = postProcessSettings;
            pulse = pulseState;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref copyTarget, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: CopyTextureName);
            copyTexture = copyTarget;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!HasValidConfiguration)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                material.SetVector(BlockImpactParamsId, settings.BuildPrimaryParams(pulse));
                material.SetVector(BlockImpactEffectParamsId, settings.BuildEffectParams(pulse));
                material.SetVector(BlockImpactStreakParamsId, settings.BuildStreakParams(pulse));
                material.SetColor(BlockImpactStreakColorId, pulse.StreakColor);
                material.SetInt(BlockImpactSampleCountId, settings.SampleCount);

                CoreUtils.SetRenderTarget(cmd, copyTexture);
                Blitter.BlitTexture(cmd, source, new Vector4(1f, 1f, 0f, 0f), 0f, false);

                SharedPropertyBlock.Clear();
                SharedPropertyBlock.SetTexture(BlitTextureId, copyTexture);
                SharedPropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));

                CoreUtils.SetRenderTarget(cmd, source);
                cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, SharedPropertyBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
