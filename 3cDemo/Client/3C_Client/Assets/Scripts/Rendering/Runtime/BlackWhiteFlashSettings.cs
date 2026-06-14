using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct BlackWhiteFlashSettings
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 1f;
        public const float MinThreshold = 0f;
        public const float MaxThreshold = 1f;
        public const float MinContrast = 0.1f;
        public const float MaxContrast = 16f;
        public const float MinWhiteBoost = 0f;
        public const float MaxWhiteBoost = 2f;
        public const float MinBlackCrush = 0f;
        public const float MaxBlackCrush = 1f;
        public const float MinInvertAmount = 0f;
        public const float MaxInvertAmount = 1f;
        public const float MinRadius = 0.01f;
        public const float MaxRadius = 2f;
        public const float MinSoftness = 0.001f;
        public const float MaxSoftness = 1f;
        public const float ActivationThreshold = 0.0001f;

        public static readonly BlackWhiteFlashSettings Disabled = new BlackWhiteFlashSettings(
            BlackWhiteFlashMode.FullScreen,
            0f,
            0.5f,
            8f,
            1.1f,
            0.45f,
            0f,
            new Vector2(0.5f, 0.5f),
            0.55f,
            0.25f);

        public BlackWhiteFlashSettings(
            BlackWhiteFlashMode mode,
            float intensity,
            float threshold,
            float contrast,
            float whiteBoost,
            float blackCrush,
            float invertAmount,
            Vector2 center,
            float radius,
            float softness)
        {
            Mode = NormalizeMode(mode);
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            Threshold = Mathf.Clamp(threshold, MinThreshold, MaxThreshold);
            Contrast = Mathf.Clamp(contrast, MinContrast, MaxContrast);
            WhiteBoost = Mathf.Clamp(whiteBoost, MinWhiteBoost, MaxWhiteBoost);
            BlackCrush = Mathf.Clamp(blackCrush, MinBlackCrush, MaxBlackCrush);
            InvertAmount = Mathf.Clamp(invertAmount, MinInvertAmount, MaxInvertAmount);
            Center = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));
            Radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            Softness = Mathf.Clamp(softness, MinSoftness, MaxSoftness);
        }

        public BlackWhiteFlashMode Mode { get; }
        public float Intensity { get; }
        public float Threshold { get; }
        public float Contrast { get; }
        public float WhiteBoost { get; }
        public float BlackCrush { get; }
        public float InvertAmount { get; }
        public Vector2 Center { get; }
        public float Radius { get; }
        public float Softness { get; }
        public bool IsActive => Intensity > ActivationThreshold;
        public Vector4 ToneParams => new Vector4(Intensity, Threshold, Contrast, InvertAmount);
        public Vector4 RangeParams => new Vector4(Center.x, Center.y, Radius, Softness);
        public Vector4 StyleParams => new Vector4(WhiteBoost, BlackCrush, (float)Mode, 0f);

        static BlackWhiteFlashMode NormalizeMode(BlackWhiteFlashMode mode)
        {
            return mode >= BlackWhiteFlashMode.FullScreen && mode <= BlackWhiteFlashMode.RadialImpact
                ? mode
                : BlackWhiteFlashMode.FullScreen;
        }
    }
}
