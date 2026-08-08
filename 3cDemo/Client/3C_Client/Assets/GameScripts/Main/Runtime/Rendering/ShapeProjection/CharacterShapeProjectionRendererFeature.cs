using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.ShapeProjection
{
    public sealed class CharacterShapeProjectionRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] ComputeShader maskCompute;
        [SerializeField] Material compositeMaterial;
        [SerializeField, Min(1)] int maxCameraSourceWorkspaces = 16;

        CharacterShapeProjectionWorkspacePool pool;
        CharacterShapeProjectionMaskPass maskPass;
        CharacterShapeProjectionCompositePass compositePass;

        public override void Create()
        {
            DisposeResources();
            if (maskCompute == null || compositeMaterial == null)
                return;
            compositeMaterial.enableInstancing = true;
            pool = new CharacterShapeProjectionWorkspacePool(maxCameraSourceWorkspaces);
            maskPass = new CharacterShapeProjectionMaskPass(maskCompute, pool);
            compositePass = new CharacterShapeProjectionCompositePass(compositeMaterial, pool);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pool == null || maskPass == null || compositePass == null)
                return;
            renderer.EnqueuePass(maskPass);
            renderer.EnqueuePass(compositePass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (compositePass != null)
                compositePass.Configure(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeResources();
        }

        void DisposeResources()
        {
            pool?.Dispose();
            pool = null;
            maskPass = null;
            compositePass = null;
        }
    }
}
