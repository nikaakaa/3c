using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Radial Blur", typeof(UniversalRenderPipeline))]
    public sealed class RadialBlur : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, RadialBlurSettings.MinIntensity, RadialBlurSettings.MaxIntensity);
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.35f, RadialBlurSettings.MinRadius, RadialBlurSettings.MaxRadius);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(8, RadialBlurSettings.MinSampleCount, RadialBlurSettings.MaxSampleCount);

        public RadialBlurSettings NormalizedSettings => new RadialBlurSettings(
            intensity.value,
            center.value,
            radius.value,
            sampleCount.value);

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
