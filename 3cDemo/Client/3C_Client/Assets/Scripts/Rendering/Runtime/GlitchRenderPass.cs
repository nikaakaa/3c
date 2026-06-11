using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class GlitchRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_GlitchCopyTexture";
        static readonly int GlitchParamsId = Shader.PropertyToID("_GlitchParams");
        static readonly int GlitchSecondaryParamsId = Shader.PropertyToID("_GlitchSecondaryParams");
        static readonly int GlitchMaskParamsId = Shader.PropertyToID("_GlitchMaskParams");
        static readonly int GlitchMaskTextureId = Shader.PropertyToID("_GlitchMaskTexture");
        static readonly int GlitchTimeId = Shader.PropertyToID("_GlitchTime");

        readonly Material material;
        readonly ProfilingSampler glitchProfilingSampler = new ProfilingSampler("3C Glitch");
        RTHandle source;
        RTHandle copyTexture;
        RTHandle maskTexture;
        GlitchSettings settings;

        public GlitchRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive && (!settings.UseTargetMask || maskTexture != null);

        public void Configure(RTHandle cameraColorTarget, ref RTHandle copyTarget, RTHandle targetMask, RenderTextureDescriptor descriptor, GlitchSettings glitchSettings)
        {
            source = cameraColorTarget;
            maskTexture = targetMask;
            settings = glitchSettings;

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
            using (new ProfilingScope(cmd, glitchProfilingSampler))
            {
                material.SetVector(GlitchParamsId, settings.PrimaryParams);
                material.SetVector(GlitchSecondaryParamsId, settings.SecondaryParams);
                material.SetVector(GlitchMaskParamsId, settings.MaskParams);
                material.SetTexture(GlitchMaskTextureId, settings.UseTargetMask ? maskTexture : Texture2D.whiteTexture);
                material.SetFloat(GlitchTimeId, Time.time);

                CoreUtils.SetRenderTarget(cmd, copyTexture);
                Blitter.BlitTexture(cmd, source, new Vector4(1f, 1f, 0f, 0f), 0f, false);

                Blitter.BlitCameraTexture(cmd, copyTexture, source, material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
