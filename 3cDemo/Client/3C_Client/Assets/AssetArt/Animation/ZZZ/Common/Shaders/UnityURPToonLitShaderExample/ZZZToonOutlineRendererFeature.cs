using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZZZ.Rendering
{
    public sealed class ZZZToonOutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Material outlineMaterial;

        OutlinePass pass;

        public override void Create()
        {
            pass = outlineMaterial == null ? null : new OutlinePass(outlineMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass != null)
                renderer.EnqueuePass(pass);
        }

        sealed class OutlinePass : ScriptableRenderPass
        {
            static readonly ShaderTagId SourcePass = new ShaderTagId("ZZZToonOutlineSource");
            static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("ZZZ Toon Outline");

            readonly Material material;
            FilteringSettings filteringSettings;

            public OutlinePass(Material material)
            {
                this.material = material;
                filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var drawingSettings = CreateDrawingSettings(SourcePass, ref renderingData, SortingCriteria.CommonOpaque);
                drawingSettings.overrideMaterial = material;
                drawingSettings.overrideMaterialPassIndex = 0;

                var commandBuffer = CommandBufferPool.Get();
                using (new ProfilingScope(commandBuffer, ProfilingSampler))
                {
                    context.ExecuteCommandBuffer(commandBuffer);
                    commandBuffer.Clear();
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                }
                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }
        }
    }
}
