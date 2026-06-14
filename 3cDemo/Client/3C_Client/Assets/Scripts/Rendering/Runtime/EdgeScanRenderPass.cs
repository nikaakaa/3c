using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class EdgeScanRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_EdgeScanCopyTexture";
        static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        static readonly int EdgeScanOriginRadiusId = Shader.PropertyToID("_EdgeScanOriginRadius");
        static readonly int EdgeScanParamsId = Shader.PropertyToID("_EdgeScanParams");
        static readonly int EdgeScanEdgeParamsId = Shader.PropertyToID("_EdgeScanEdgeParams");
        static readonly int EdgeScanColorId = Shader.PropertyToID("_EdgeScanColor");
        static readonly int EdgeScanDirectionArcId = Shader.PropertyToID("_EdgeScanDirectionArc");
        static readonly int EdgeScanLineParamsId = Shader.PropertyToID("_EdgeScanLineParams");
        static readonly int EdgeScanToneParamsId = Shader.PropertyToID("_EdgeScanToneParams");
        static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        readonly Material material;
        readonly ProfilingSampler edgeScanProfilingSampler = new ProfilingSampler("3C Edge Scan");
        RTHandle source;
        RTHandle copyTexture;
        EdgeScanSettings settings;

        public EdgeScanRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
            ConfigureInput(RequiredInputs);
        }

        public static ScriptableRenderPassInput RequiredInputs =>
            ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive;

        public void Configure(RTHandle cameraColorTarget, ref RTHandle copyTarget, RenderTextureDescriptor descriptor, EdgeScanSettings edgeScanSettings)
        {
            source = cameraColorTarget;
            settings = edgeScanSettings;

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
            using (new ProfilingScope(cmd, edgeScanProfilingSampler))
            {
                material.SetVector(EdgeScanOriginRadiusId, settings.OriginRadiusParams);
                material.SetVector(EdgeScanParamsId, settings.ScanParams);
                material.SetVector(EdgeScanEdgeParamsId, settings.EdgeParams);
                material.SetVector(EdgeScanColorId, settings.ColorParams);
                material.SetVector(EdgeScanDirectionArcId, settings.DirectionArcParams);
                material.SetVector(EdgeScanLineParamsId, settings.LineParams);
                material.SetVector(EdgeScanToneParamsId, settings.ToneParams);

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
