using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct ScreenSpaceDotTransparencySettings
    {
        public const float MinCoverage = 0f;
        public const float MaxCoverage = 1f;
        public const float MinSpacingPixels = 2f;
        public const float MaxSpacingPixels = 96f;
        public const float MinRadius = 0f;
        public const float MaxRadius = 1f;
        public const float MinHardness = 0f;
        public const float MaxHardness = 1f;
        public const float MaxOffsetPixels = 4096f;

        public static readonly ScreenSpaceDotTransparencySettings Disabled = new ScreenSpaceDotTransparencySettings(
            false,
            0f,
            12f,
            0.45f,
            1f,
            Vector2.zero);

        public ScreenSpaceDotTransparencySettings(
            bool enabled,
            float coverage,
            float spacingPixels,
            float radius,
            float hardness,
            Vector2 offsetPixels)
        {
            Coverage = Clamp(coverage, MinCoverage, MaxCoverage);
            SpacingPixels = Clamp(spacingPixels, MinSpacingPixels, MaxSpacingPixels);
            Radius = Clamp(radius, MinRadius, MaxRadius);
            Hardness = Clamp(hardness, MinHardness, MaxHardness);
            OffsetPixels = ClampOffset(offsetPixels);
            Enabled = enabled && Coverage > 0f && Radius > 0f;
        }

        public bool Enabled { get; }
        public float Coverage { get; }
        public float SpacingPixels { get; }
        public float Radius { get; }
        public float Hardness { get; }
        public Vector2 OffsetPixels { get; }
        public bool IsActive => Enabled && Coverage > 0f && Radius > 0f;

        public Vector4 BuildPrimaryParams()
        {
            return new Vector4(IsActive ? 1f : 0f, Coverage, SpacingPixels, Radius);
        }

        public Vector4 BuildSecondaryParams()
        {
            return new Vector4(Hardness, OffsetPixels.x, OffsetPixels.y, 0f);
        }

        static float Clamp(float value, float min, float max)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? min
                : Mathf.Clamp(value, min, max);
        }

        static Vector2 ClampOffset(Vector2 value)
        {
            return new Vector2(
                Clamp(value.x, -MaxOffsetPixels, MaxOffsetPixels),
                Clamp(value.y, -MaxOffsetPixels, MaxOffsetPixels));
        }
    }
}
