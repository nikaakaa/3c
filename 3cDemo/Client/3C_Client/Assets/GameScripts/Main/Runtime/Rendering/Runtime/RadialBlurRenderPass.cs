using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class RadialBlurRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_RadialBlurCopyTexture";
        static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        static readonly int RadialBlurParamsId = Shader.PropertyToID("_RadialBlurParams");
        static readonly int RadialBlurSampleCountId = Shader.PropertyToID("_RadialBlurSampleCount");
        static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        readonly Material material;
        readonly ProfilingSampler radialBlurProfilingSampler = new ProfilingSampler("3C Radial Blur");
        RTHandle source;
        RTHandle copyTexture;
        RadialBlurSettings settings;

        public RadialBlurRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
        }

        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive;

        public void Configure(RTHandle cameraColorTarget, ref RTHandle copyTarget, RenderTextureDescriptor descriptor, RadialBlurSettings radialBlurSettings)
        {
            source = cameraColorTarget;
            settings = radialBlurSettings;

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
            using (new ProfilingScope(cmd, radialBlurProfilingSampler))
            {
                material.SetVector(RadialBlurParamsId, settings.ShaderParams);
                material.SetInt(RadialBlurSampleCountId, settings.SampleCount);

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
