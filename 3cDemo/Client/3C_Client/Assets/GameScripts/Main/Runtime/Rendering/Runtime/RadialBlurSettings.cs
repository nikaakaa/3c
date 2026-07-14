using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct RadialBlurSettings
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 1f;
        public const float MinRadius = 0.01f;
        public const float MaxRadius = 1.5f;
        public const int MinSampleCount = 2;
        public const int MaxSampleCount = 16;
        public const float ActivationThreshold = 0.0001f;

        public static readonly RadialBlurSettings Disabled = new RadialBlurSettings(
            0f,
            new Vector2(0.5f, 0.5f),
            0.35f,
            8);

        public RadialBlurSettings(float intensity, Vector2 center, float radius, int sampleCount)
        {
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            Center = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));
            Radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            SampleCount = Mathf.Clamp(sampleCount, MinSampleCount, MaxSampleCount);
        }

        public float Intensity { get; }
        public Vector2 Center { get; }
        public float Radius { get; }
        public int SampleCount { get; }
        public bool IsActive => Intensity > ActivationThreshold;

        public Vector4 ShaderParams => new Vector4(Center.x, Center.y, Intensity, Radius);
    }
}
