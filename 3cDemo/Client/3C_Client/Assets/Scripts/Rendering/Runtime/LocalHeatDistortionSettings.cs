using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct LocalHeatDistortionSettings
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 1f;
        public const float MinSpeed = 0f;
        public const float MaxSpeed = 48f;
        public const float MinNoiseScale = 2f;
        public const float MaxNoiseScale = 96f;
        public const float MinDistortionStrength = 0f;
        public const float MaxDistortionStrength = 0.08f;
        public const float MinAreaSoftness = 0.01f;
        public const float MaxAreaSoftness = 0.8f;
        public const float MinParticleVisibility = 0f;
        public const float MaxParticleVisibility = 1f;
        public const float ActivationThreshold = 0.0001f;

        public static readonly LocalHeatDistortionSettings Disabled = new LocalHeatDistortionSettings(
            0f,
            LocalHeatDistortionMode.HeatHaze,
            12f,
            24f,
            0.018f,
            0.25f,
            1f,
            false);

        public LocalHeatDistortionSettings(
            float intensity,
            LocalHeatDistortionMode mode,
            float speed,
            float noiseScale,
            float distortionStrength,
            float areaSoftness,
            float particleVisibility,
            bool previewDebug = false)
        {
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            Mode = NormalizeMode(mode);
            Speed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
            NoiseScale = Mathf.Clamp(noiseScale, MinNoiseScale, MaxNoiseScale);
            DistortionStrength = Mathf.Clamp(distortionStrength, MinDistortionStrength, MaxDistortionStrength);
            AreaSoftness = Mathf.Clamp(areaSoftness, MinAreaSoftness, MaxAreaSoftness);
            ParticleVisibility = Mathf.Clamp(particleVisibility, MinParticleVisibility, MaxParticleVisibility);
            PreviewDebug = previewDebug;
        }

        public float Intensity { get; }
        public LocalHeatDistortionMode Mode { get; }
        public float Speed { get; }
        public float NoiseScale { get; }
        public float DistortionStrength { get; }
        public float AreaSoftness { get; }
        public float ParticleVisibility { get; }
        public bool PreviewDebug { get; }
        public bool IsActive => PreviewDebug || (Intensity > ActivationThreshold && DistortionStrength > ActivationThreshold);

        public Vector4 ShaderParams => new Vector4(Intensity, Speed, NoiseScale, DistortionStrength);
        public Vector4 DebugParams => new Vector4(PreviewDebug ? 1f : 0f, 0f, 0f, 0f);

        public LocalHeatDistortionSettings WithMode(LocalHeatDistortionMode mode)
        {
            return new LocalHeatDistortionSettings(
                Intensity,
                mode,
                Speed,
                NoiseScale,
                DistortionStrength,
                AreaSoftness,
                ParticleVisibility,
                PreviewDebug);
        }

        static LocalHeatDistortionMode NormalizeMode(LocalHeatDistortionMode mode)
        {
            return mode >= LocalHeatDistortionMode.HeatHaze && mode <= LocalHeatDistortionMode.VerticalFlow
                ? mode
                : LocalHeatDistortionMode.HeatHaze;
        }
    }
}
