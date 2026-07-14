using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct BlockImpactPostProcessSettings
    {
        public const float MinGlobalIntensity = 0f;
        public const float MaxGlobalIntensity = 3f;
        public const float MinFlashIntensity = 0f;
        public const float MaxFlashIntensity = 3f;
        public const float MinRadialStrength = 0f;
        public const float MaxRadialStrength = 1f;
        public const float MinStreakIntensity = 0f;
        public const float MaxStreakIntensity = 3f;
        public const float MinStreakLength = 0.05f;
        public const float MaxStreakLength = 2f;
        public const float MinStreakThickness = 0.001f;
        public const float MaxStreakThickness = 0.2f;
        public const float MinStreakSoftness = 0f;
        public const float MaxStreakSoftness = 1f;
        public const float MinChromaticStrength = 0f;
        public const float MaxChromaticStrength = 0.08f;
        public const float MinRadius = 0.01f;
        public const float MaxRadius = 2f;
        public const int MinSampleCount = 2;
        public const int MaxSampleCount = 16;
        public const float ActivationThreshold = 0.0001f;

        public static readonly BlockImpactPostProcessSettings Disabled = new BlockImpactPostProcessSettings(
            0f,
            1.1f,
            0.35f,
            1.1f,
            0.012f,
            0.85f,
            8,
            1.2f,
            0.018f,
            0.7f);

        public BlockImpactPostProcessSettings(
            float globalIntensity,
            float flashIntensity,
            float radialStrength,
            float streakIntensity,
            float chromaticStrength,
            float radius,
            int sampleCount)
            : this(
                globalIntensity,
                flashIntensity,
                radialStrength,
                streakIntensity,
                chromaticStrength,
                radius,
                sampleCount,
                1.2f,
                0.018f,
                0.7f)
        {
        }

        public BlockImpactPostProcessSettings(
            float globalIntensity,
            float flashIntensity,
            float radialStrength,
            float streakIntensity,
            float chromaticStrength,
            float radius,
            int sampleCount,
            float streakLength,
            float streakThickness,
            float streakSoftness)
        {
            GlobalIntensity = Mathf.Clamp(globalIntensity, MinGlobalIntensity, MaxGlobalIntensity);
            FlashIntensity = Mathf.Clamp(flashIntensity, MinFlashIntensity, MaxFlashIntensity);
            RadialStrength = Mathf.Clamp(radialStrength, MinRadialStrength, MaxRadialStrength);
            StreakIntensity = Mathf.Clamp(streakIntensity, MinStreakIntensity, MaxStreakIntensity);
            ChromaticStrength = Mathf.Clamp(chromaticStrength, MinChromaticStrength, MaxChromaticStrength);
            Radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            SampleCount = Mathf.Clamp(sampleCount, MinSampleCount, MaxSampleCount);
            StreakLength = Mathf.Clamp(streakLength, MinStreakLength, MaxStreakLength);
            StreakThickness = Mathf.Clamp(streakThickness, MinStreakThickness, MaxStreakThickness);
            StreakSoftness = Mathf.Clamp(streakSoftness, MinStreakSoftness, MaxStreakSoftness);
        }

        public float GlobalIntensity { get; }
        public float FlashIntensity { get; }
        public float RadialStrength { get; }
        public float StreakIntensity { get; }
        public float ChromaticStrength { get; }
        public float Radius { get; }
        public int SampleCount { get; }
        public float StreakLength { get; }
        public float StreakThickness { get; }
        public float StreakSoftness { get; }

        public bool IsActive => GlobalIntensity > ActivationThreshold
            && (FlashIntensity > ActivationThreshold
                || RadialStrength > ActivationThreshold
                || StreakIntensity > ActivationThreshold
                || ChromaticStrength > ActivationThreshold);

        public Vector4 BuildPrimaryParams(BlockImpactPostProcessPulseState pulse)
        {
            float strength = GlobalIntensity * pulse.Intensity * pulse.Fade;
            return new Vector4(pulse.Center.x, pulse.Center.y, strength, Radius);
        }

        public Vector4 BuildEffectParams(BlockImpactPostProcessPulseState pulse)
        {
            return new Vector4(
                FlashIntensity * pulse.FlashWeight,
                RadialStrength * pulse.RadialWeight,
                StreakIntensity * pulse.StreakWeight,
                ChromaticStrength * pulse.ChromaticWeight);
        }

        public Vector4 BuildStreakParams(BlockImpactPostProcessPulseState pulse)
        {
            float length = pulse.StreakLength > 0f ? pulse.StreakLength : StreakLength;
            float thickness = pulse.StreakThickness > 0f ? pulse.StreakThickness : StreakThickness;
            float softness = pulse.StreakSoftness >= 0f ? pulse.StreakSoftness : StreakSoftness;
            return new Vector4(length, thickness, softness, pulse.Fade);
        }
    }
}
