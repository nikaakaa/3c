using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class GlitchMaskRenderPass : ScriptableRenderPass
    {
        const string MaskTextureName = "_3C_GlitchMaskTexture";
        static readonly List<ShaderTagId> ShaderTagIds = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly")
        };

        readonly Material material;
        readonly ProfilingSampler maskProfilingSampler = new ProfilingSampler("3C Glitch Mask");
        FilteringSettings filteringSettings;
        RTHandle maskTexture;

        public GlitchMaskRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
        }

        public bool HasValidConfiguration => material != null && maskTexture != null;

        public static RenderTextureDescriptor CreateMaskDescriptor(RenderTextureDescriptor cameraDescriptor)
        {
            cameraDescriptor.depthBufferBits = 0;
            cameraDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
            cameraDescriptor.msaaSamples = 1;
            return cameraDescriptor;
        }

        public void Configure(ref RTHandle targetMask, RTHandle cameraDepthTarget, RenderTextureDescriptor descriptor, uint renderingLayerMask)
        {
            descriptor = CreateMaskDescriptor(descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref targetMask, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: MaskTextureName);
            maskTexture = targetMask;
            filteringSettings = new FilteringSettings(RenderQueueRange.all, -1, renderingLayerMask, 0);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!HasValidConfiguration)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, maskProfilingSampler))
            {
                CoreUtils.SetRenderTarget(cmd, maskTexture, ClearFlag.Color, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(ShaderTagIds, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = material;
                drawingSettings.overrideMaterialPassIndex = 0;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                cmd.Clear();
                ResetTarget();
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
