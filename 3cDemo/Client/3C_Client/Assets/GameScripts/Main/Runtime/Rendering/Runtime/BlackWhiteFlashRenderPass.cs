using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class BlackWhiteFlashRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_BlackWhiteFlashCopyTexture";
        static readonly int ToneParamsId = Shader.PropertyToID("_BlackWhiteFlashToneParams");
        static readonly int RangeParamsId = Shader.PropertyToID("_BlackWhiteFlashRangeParams");
        static readonly int StyleParamsId = Shader.PropertyToID("_BlackWhiteFlashStyleParams");

        readonly Material material;
        readonly ProfilingSampler blackWhiteFlashProfilingSampler = new ProfilingSampler("3C Black White Flash");
        RTHandle source;
        RTHandle copyTexture;
        BlackWhiteFlashSettings settings;

        public BlackWhiteFlashRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
            ConfigureInput(RequiredInputs);
        }

        public static ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.Color;
        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive;

        public void Configure(RTHandle cameraColorTarget, ref RTHandle copyTarget, RenderTextureDescriptor descriptor, BlackWhiteFlashSettings flashSettings)
        {
            source = cameraColorTarget;
            settings = flashSettings;

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
            using (new ProfilingScope(cmd, blackWhiteFlashProfilingSampler))
            {
                material.SetVector(ToneParamsId, settings.ToneParams);
                material.SetVector(RangeParamsId, settings.RangeParams);
                material.SetVector(StyleParamsId, settings.StyleParams);

                CoreUtils.SetRenderTarget(cmd, copyTexture);
                Blitter.BlitTexture(cmd, source, new Vector4(1f, 1f, 0f, 0f), 0f, false);

                Blitter.BlitCameraTexture(cmd, copyTexture, source, material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
