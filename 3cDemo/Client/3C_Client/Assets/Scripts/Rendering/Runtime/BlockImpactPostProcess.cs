using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Block Impact", typeof(UniversalRenderPipeline))]
    public sealed class BlockImpactPostProcess : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter globalIntensity = new ClampedFloatParameter(0f, BlockImpactPostProcessSettings.MinGlobalIntensity, BlockImpactPostProcessSettings.MaxGlobalIntensity);
        public ClampedFloatParameter flashIntensity = new ClampedFloatParameter(1.1f, BlockImpactPostProcessSettings.MinFlashIntensity, BlockImpactPostProcessSettings.MaxFlashIntensity);
        public ClampedFloatParameter radialStrength = new ClampedFloatParameter(0.35f, BlockImpactPostProcessSettings.MinRadialStrength, BlockImpactPostProcessSettings.MaxRadialStrength);
        public ClampedFloatParameter streakIntensity = new ClampedFloatParameter(1.1f, BlockImpactPostProcessSettings.MinStreakIntensity, BlockImpactPostProcessSettings.MaxStreakIntensity);
        public ClampedFloatParameter streakLength = new ClampedFloatParameter(1.2f, BlockImpactPostProcessSettings.MinStreakLength, BlockImpactPostProcessSettings.MaxStreakLength);
        public ClampedFloatParameter streakThickness = new ClampedFloatParameter(0.018f, BlockImpactPostProcessSettings.MinStreakThickness, BlockImpactPostProcessSettings.MaxStreakThickness);
        public ClampedFloatParameter streakSoftness = new ClampedFloatParameter(0.7f, BlockImpactPostProcessSettings.MinStreakSoftness, BlockImpactPostProcessSettings.MaxStreakSoftness);
        public ClampedFloatParameter chromaticStrength = new ClampedFloatParameter(0.012f, BlockImpactPostProcessSettings.MinChromaticStrength, BlockImpactPostProcessSettings.MaxChromaticStrength);
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.85f, BlockImpactPostProcessSettings.MinRadius, BlockImpactPostProcessSettings.MaxRadius);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(8, BlockImpactPostProcessSettings.MinSampleCount, BlockImpactPostProcessSettings.MaxSampleCount);

        public BlockImpactPostProcessSettings NormalizedSettings => new BlockImpactPostProcessSettings(
            globalIntensity.value,
            flashIntensity.value,
            radialStrength.value,
            streakIntensity.value,
            chromaticStrength.value,
            radius.value,
            sampleCount.value,
            streakLength.value,
            streakThickness.value,
            streakSoftness.value);

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
