using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class GlitchRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        [SerializeField] Shader maskShader;
        [SerializeField] uint targetRenderingLayerMask = 2u;
        [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        Material material;
        Material maskMaterial;
        GlitchRenderPass pass;
        GlitchMaskRenderPass maskPass;
        RTHandle copyTexture;
        RTHandle maskTexture;

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
        public bool HasMaskMaterial => maskMaterial != null;
        public bool HasPass => pass != null;
        public bool HasMaskPass => maskPass != null;
        public RenderPassEvent MaskInjectionPoint => maskPass?.renderPassEvent ?? RenderPassEvent.AfterRendering;
        public uint TargetRenderingLayerMask => targetRenderingLayerMask;

        public override void Create()
        {
            RebuildResources();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            GlitchSettings settings = ResolveSettings();
            if (!ShouldEnqueue(settings))
                return;

            if (settings.UseTargetMask)
                renderer.EnqueuePass(maskPass);

            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            GlitchSettings settings = ResolveSettings();
            if (!CanRender(settings))
                return;

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            if (settings.UseTargetMask)
                maskPass.Configure(ref maskTexture, renderer.cameraDepthTargetHandle, descriptor, targetRenderingLayerMask);

            pass.Configure(renderer.cameraColorTargetHandle, ref copyTexture, settings.UseTargetMask ? maskTexture : null, descriptor, settings);
        }

        public bool CanRender(GlitchSettings settings)
        {
            if (material == null || pass == null || !settings.IsActive)
                return false;

            return !settings.UseTargetMask || (maskMaterial != null && maskPass != null && targetRenderingLayerMask != 0u);
        }

        public bool ShouldEnqueue()
        {
            return ShouldEnqueue(ResolveSettings());
        }

        public bool ShouldEnqueue(GlitchSettings settings)
        {
            return CanRender(settings);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            CoreUtils.Destroy(maskMaterial);
            copyTexture?.Release();
            maskTexture?.Release();
            material = null;
            maskMaterial = null;
            pass = null;
            maskPass = null;
            copyTexture = null;
            maskTexture = null;
        }

        void RebuildMaterial()
        {
            CoreUtils.Destroy(material);
            CoreUtils.Destroy(maskMaterial);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            maskMaterial = maskShader != null ? CoreUtils.CreateEngineMaterial(maskShader) : null;
        }

        void RebuildResources()
        {
            RebuildMaterial();
            pass = material != null ? new GlitchRenderPass(material, injectionPoint) : null;
            maskPass = maskMaterial != null ? new GlitchMaskRenderPass(maskMaterial, injectionPoint) : null;
        }

        static GlitchSettings ResolveSettings()
        {
            Glitch glitch = VolumeManager.instance.stack?.GetComponent<Glitch>();
            return glitch != null && glitch.IsActive()
                ? glitch.NormalizedSettings
                : GlitchSettings.Disabled;
        }
    }
}
