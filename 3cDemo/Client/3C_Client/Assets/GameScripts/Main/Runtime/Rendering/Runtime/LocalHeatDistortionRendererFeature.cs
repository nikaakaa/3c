using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    public sealed class LocalHeatDistortionRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        [SerializeField] RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        Material material;
        LocalHeatDistortionRenderPass pass;
        RTHandle copyTexture;

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
        public bool HasPass => pass != null;

        public override void Create()
        {
            RebuildResources();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            LocalHeatDistortionSettings settings = ResolveSettings();
            if (!TryResolveArea(renderingData.cameraData.camera, settings, out LocalHeatDistortionAreaSettings areaSettings, out LocalHeatDistortionAreaSource source))
                return;

            if (!ShouldEnqueue(settings, areaSettings, source))
                return;

            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            LocalHeatDistortionSettings settings = ResolveSettings();
            if (!TryResolveArea(renderingData.cameraData.camera, settings, out LocalHeatDistortionAreaSettings areaSettings, out LocalHeatDistortionAreaSource source))
                return;

            if (!CanRender(settings, areaSettings, source))
                return;

            if (source.OverrideMode)
                settings = settings.WithMode(source.Mode);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            pass.Configure(renderer.cameraColorTargetHandle, ref copyTexture, descriptor, settings, areaSettings);
        }

        public bool CanRender(LocalHeatDistortionSettings settings, LocalHeatDistortionAreaSettings areaSettings, LocalHeatDistortionAreaSource source)
        {
            return material != null && pass != null && settings.IsActive && areaSettings.IsValid && source != null && source.isActiveAndEnabled;
        }

        public bool ShouldEnqueue()
        {
            return ShouldEnqueue(ResolveSettings(), LocalHeatDistortionAreaSettings.Invalid, null);
        }

        public bool ShouldEnqueue(LocalHeatDistortionSettings settings, LocalHeatDistortionAreaSettings areaSettings, LocalHeatDistortionAreaSource source)
        {
            return CanRender(settings, areaSettings, source);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            copyTexture?.Release();
            material = null;
            pass = null;
            copyTexture = null;
        }

        void RebuildMaterial()
        {
            CoreUtils.Destroy(material);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
        }

        void RebuildResources()
        {
            RebuildMaterial();
            pass = material != null ? new LocalHeatDistortionRenderPass(material, injectionPoint) : null;
        }

        static LocalHeatDistortionSettings ResolveSettings()
        {
            LocalHeatDistortion heatDistortion = VolumeManager.instance.stack?.GetComponent<LocalHeatDistortion>();
            return heatDistortion != null && heatDistortion.IsActive()
                ? heatDistortion.NormalizedSettings
                : LocalHeatDistortionSettings.Disabled;
        }

        static bool TryResolveArea(
            Camera camera,
            LocalHeatDistortionSettings settings,
            out LocalHeatDistortionAreaSettings areaSettings,
            out LocalHeatDistortionAreaSource source)
        {
            if (!settings.IsActive)
            {
                areaSettings = LocalHeatDistortionAreaSettings.Invalid;
                source = null;
                return false;
            }

            return LocalHeatDistortionAreaSource.TryResolveArea(camera, settings.AreaSoftness, out areaSettings, out source);
        }
    }
}
