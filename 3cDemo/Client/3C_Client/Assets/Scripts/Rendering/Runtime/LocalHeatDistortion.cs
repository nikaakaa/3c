using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Local Heat Distortion", typeof(UniversalRenderPipeline))]
    public sealed class LocalHeatDistortion : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, LocalHeatDistortionSettings.MinIntensity, LocalHeatDistortionSettings.MaxIntensity);
        public LocalHeatDistortionModeParameter mode = new LocalHeatDistortionModeParameter(LocalHeatDistortionMode.HeatHaze);
        public ClampedFloatParameter speed = new ClampedFloatParameter(12f, LocalHeatDistortionSettings.MinSpeed, LocalHeatDistortionSettings.MaxSpeed);
        public ClampedFloatParameter noiseScale = new ClampedFloatParameter(24f, LocalHeatDistortionSettings.MinNoiseScale, LocalHeatDistortionSettings.MaxNoiseScale);
        public ClampedFloatParameter distortionStrength = new ClampedFloatParameter(0.018f, LocalHeatDistortionSettings.MinDistortionStrength, LocalHeatDistortionSettings.MaxDistortionStrength);
        public ClampedFloatParameter areaSoftness = new ClampedFloatParameter(0.25f, LocalHeatDistortionSettings.MinAreaSoftness, LocalHeatDistortionSettings.MaxAreaSoftness);
        public ClampedFloatParameter particleVisibility = new ClampedFloatParameter(1f, LocalHeatDistortionSettings.MinParticleVisibility, LocalHeatDistortionSettings.MaxParticleVisibility);
        public BoolParameter previewDebug = new BoolParameter(false);

        public LocalHeatDistortionSettings NormalizedSettings => new LocalHeatDistortionSettings(
            intensity.value,
            mode.value,
            speed.value,
            noiseScale.value,
            distortionStrength.value,
            areaSoftness.value,
            particleVisibility.value,
            previewDebug.value);

        public bool IsActive()
        {
            return active && NormalizedSettings.IsActive;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
