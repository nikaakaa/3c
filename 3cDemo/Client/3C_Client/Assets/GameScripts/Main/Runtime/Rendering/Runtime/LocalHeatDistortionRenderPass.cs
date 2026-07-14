using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class LocalHeatDistortionRenderPass : ScriptableRenderPass
    {
        const string CopyTextureName = "_3C_LocalHeatDistortionCopyTexture";
        static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        static readonly int HeatParamsId = Shader.PropertyToID("_LocalHeatDistortionParams");
        static readonly int HeatAreaParamsId = Shader.PropertyToID("_LocalHeatDistortionAreaParams");
        static readonly int HeatAreaExtraParamsId = Shader.PropertyToID("_LocalHeatDistortionAreaExtraParams");
        static readonly int HeatAreaDepthParamsId = Shader.PropertyToID("_LocalHeatDistortionAreaDepthParams");
        static readonly int HeatDebugParamsId = Shader.PropertyToID("_LocalHeatDistortionDebugParams");
        static readonly int HeatModeId = Shader.PropertyToID("_LocalHeatDistortionMode");
        static readonly int HeatTimeId = Shader.PropertyToID("_LocalHeatDistortionTime");
        static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        readonly Material material;
        readonly ProfilingSampler heatDistortionProfilingSampler = new ProfilingSampler("3C Local Heat Distortion");
        RTHandle source;
        RTHandle copyTexture;
        LocalHeatDistortionSettings settings;
        LocalHeatDistortionAreaSettings areaSettings;

        public LocalHeatDistortionRenderPass(Material material, RenderPassEvent passEvent)
        {
            this.material = material;
            renderPassEvent = passEvent;
            ConfigureInput(RequiredInputs);
        }

        public static ScriptableRenderPassInput RequiredInputs => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth;
        public bool HasValidConfiguration => material != null && source != null && copyTexture != null && settings.IsActive && areaSettings.IsValid;

        public void Configure(
            RTHandle cameraColorTarget,
            ref RTHandle copyTarget,
            RenderTextureDescriptor descriptor,
            LocalHeatDistortionSettings heatSettings,
            LocalHeatDistortionAreaSettings heatAreaSettings)
        {
            source = cameraColorTarget;
            settings = heatSettings;
            areaSettings = heatAreaSettings;

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
            using (new ProfilingScope(cmd, heatDistortionProfilingSampler))
            {
                material.SetVector(HeatParamsId, settings.ShaderParams);
                material.SetVector(HeatAreaParamsId, areaSettings.AreaParams);
                material.SetVector(HeatAreaExtraParamsId, areaSettings.AreaExtraParams);
                material.SetVector(HeatAreaDepthParamsId, areaSettings.AreaDepthParams);
                material.SetVector(HeatDebugParamsId, settings.DebugParams);
                material.SetInt(HeatModeId, (int)settings.Mode);
                material.SetFloat(HeatTimeId, Time.time);

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
