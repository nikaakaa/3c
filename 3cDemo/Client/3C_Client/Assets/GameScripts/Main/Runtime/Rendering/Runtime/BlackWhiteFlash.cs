using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Black White Flash", typeof(UniversalRenderPipeline))]
    public sealed class BlackWhiteFlash : VolumeComponent, IPostProcessComponent
    {
        public BlackWhiteFlashModeParameter mode = new BlackWhiteFlashModeParameter(BlackWhiteFlashMode.FullScreen);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, BlackWhiteFlashSettings.MinIntensity, BlackWhiteFlashSettings.MaxIntensity);
        public ClampedFloatParameter threshold = new ClampedFloatParameter(0.5f, BlackWhiteFlashSettings.MinThreshold, BlackWhiteFlashSettings.MaxThreshold);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(8f, BlackWhiteFlashSettings.MinContrast, BlackWhiteFlashSettings.MaxContrast);
        public ClampedFloatParameter whiteBoost = new ClampedFloatParameter(1.1f, BlackWhiteFlashSettings.MinWhiteBoost, BlackWhiteFlashSettings.MaxWhiteBoost);
        public ClampedFloatParameter blackCrush = new ClampedFloatParameter(0.45f, BlackWhiteFlashSettings.MinBlackCrush, BlackWhiteFlashSettings.MaxBlackCrush);
        public ClampedFloatParameter invertAmount = new ClampedFloatParameter(0f, BlackWhiteFlashSettings.MinInvertAmount, BlackWhiteFlashSettings.MaxInvertAmount);
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.55f, BlackWhiteFlashSettings.MinRadius, BlackWhiteFlashSettings.MaxRadius);
        public ClampedFloatParameter softness = new ClampedFloatParameter(0.25f, BlackWhiteFlashSettings.MinSoftness, BlackWhiteFlashSettings.MaxSoftness);

        public BlackWhiteFlashSettings NormalizedSettings => new BlackWhiteFlashSettings(
            mode.value,
            intensity.value,
            threshold.value,
            contrast.value,
            whiteBoost.value,
            blackCrush.value,
            invertAmount.value,
            center.value,
            radius.value,
            softness.value);

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
