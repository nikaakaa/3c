using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Glitch", typeof(UniversalRenderPipeline))]
    public sealed class Glitch : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, GlitchSettings.MinIntensity, GlitchSettings.MaxIntensity);
        public ClampedFloatParameter blockSize = new ClampedFloatParameter(48f, GlitchSettings.MinBlockSize, GlitchSettings.MaxBlockSize);
        public ClampedFloatParameter horizontalJitter = new ClampedFloatParameter(0.02f, GlitchSettings.MinHorizontalJitter, GlitchSettings.MaxHorizontalJitter);
        public ClampedFloatParameter rgbSplit = new ClampedFloatParameter(0.01f, GlitchSettings.MinRgbSplit, GlitchSettings.MaxRgbSplit);
        public ClampedFloatParameter scanLineIntensity = new ClampedFloatParameter(0.35f, GlitchSettings.MinScanLineIntensity, GlitchSettings.MaxScanLineIntensity);
        public ClampedFloatParameter speed = new ClampedFloatParameter(24f, GlitchSettings.MinSpeed, GlitchSettings.MaxSpeed);
        public BoolParameter useTargetMask = new BoolParameter(false);
        public ClampedFloatParameter maskInfluence = new ClampedFloatParameter(1f, GlitchSettings.MinMaskInfluence, GlitchSettings.MaxMaskInfluence);
        public ClampedFloatParameter maskExpansion = new ClampedFloatParameter(0.04f, GlitchSettings.MinMaskExpansion, GlitchSettings.MaxMaskExpansion);

        public GlitchSettings NormalizedSettings => new GlitchSettings(
            intensity.value,
            blockSize.value,
            horizontalJitter.value,
            rgbSplit.value,
            scanLineIntensity.value,
            speed.value,
            useTargetMask.value,
            maskInfluence.value,
            maskExpansion.value);

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
