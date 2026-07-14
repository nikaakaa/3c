using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Glitch", typeof(UniversalRenderPipeline))]
    public sealed class Glitch : VolumeComponent, IPostProcessComponent
    {
        public GlitchModeParameter mode = new GlitchModeParameter(GlitchMode.DigitalGlitch);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, GlitchSettings.MinIntensity, GlitchSettings.MaxIntensity);
        public ClampedFloatParameter blockSize = new ClampedFloatParameter(48f, GlitchSettings.MinBlockSize, GlitchSettings.MaxBlockSize);
        public ClampedFloatParameter horizontalJitter = new ClampedFloatParameter(0.02f, GlitchSettings.MinHorizontalJitter, GlitchSettings.MaxHorizontalJitter);
        public ClampedFloatParameter rgbSplit = new ClampedFloatParameter(0.01f, GlitchSettings.MinRgbSplit, GlitchSettings.MaxRgbSplit);
        public ClampedFloatParameter scanLineIntensity = new ClampedFloatParameter(0.35f, GlitchSettings.MinScanLineIntensity, GlitchSettings.MaxScanLineIntensity);
        public ClampedFloatParameter speed = new ClampedFloatParameter(24f, GlitchSettings.MinSpeed, GlitchSettings.MaxSpeed);
        public BoolParameter useTargetMask = new BoolParameter(false);
        public ClampedFloatParameter maskInfluence = new ClampedFloatParameter(1f, GlitchSettings.MinMaskInfluence, GlitchSettings.MaxMaskInfluence);
        public ClampedFloatParameter maskExpansion = new ClampedFloatParameter(0.04f, GlitchSettings.MinMaskExpansion, GlitchSettings.MaxMaskExpansion);
        public ClampedFloatParameter slashSliceDensity = new ClampedFloatParameter(360f, GlitchSettings.MinSlashSliceDensity, GlitchSettings.MaxSlashSliceDensity);
        public ClampedFloatParameter slashSmearWidth = new ClampedFloatParameter(0.04f, GlitchSettings.MinSlashSmearWidth, GlitchSettings.MaxSlashSmearWidth);
        public ClampedFloatParameter slashHighlightStretch = new ClampedFloatParameter(0.8f, GlitchSettings.MinSlashHighlightStretch, GlitchSettings.MaxSlashHighlightStretch);
        public ClampedFloatParameter slashDirection = new ClampedFloatParameter(1f, GlitchSettings.MinSlashDirection, GlitchSettings.MaxSlashDirection);
        public ClampedFloatParameter slashBlend = new ClampedFloatParameter(1f, GlitchSettings.MinSlashBlend, GlitchSettings.MaxSlashBlend);

        public GlitchSettings NormalizedSettings => new GlitchSettings(
            mode.value,
            intensity.value,
            blockSize.value,
            horizontalJitter.value,
            rgbSplit.value,
            scanLineIntensity.value,
            speed.value,
            useTargetMask.value,
            maskInfluence.value,
            maskExpansion.value,
            slashSliceDensity.value,
            slashSmearWidth.value,
            slashHighlightStretch.value,
            slashDirection.value,
            slashBlend.value);

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
