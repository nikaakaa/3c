using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct BlockImpactPostProcessPulseState
    {
        public BlockImpactPostProcessPulseState(Vector2 center, float intensity, float duration, float elapsed)
            : this(
                center,
                intensity,
                duration,
                elapsed,
                1.2f,
                0.018f,
                0.7f,
                new Color(1f, 0.42f, 0.28f, 1f),
                1f,
                1f,
                1f,
                1f)
        {
        }

        public BlockImpactPostProcessPulseState(
            Vector2 center,
            float intensity,
            float duration,
            float elapsed,
            float streakLength,
            float streakThickness,
            float streakSoftness,
            Color streakColor,
            float flashWeight,
            float radialWeight,
            float streakWeight,
            float chromaticWeight)
        {
            Center = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));
            Intensity = Mathf.Clamp(intensity, 0f, 3f);
            Duration = Mathf.Clamp(duration, 0.02f, 2f);
            Elapsed = Mathf.Max(0f, elapsed);
            StreakLength = Mathf.Clamp(streakLength, BlockImpactPostProcessSettings.MinStreakLength, BlockImpactPostProcessSettings.MaxStreakLength);
            StreakThickness = Mathf.Clamp(streakThickness, BlockImpactPostProcessSettings.MinStreakThickness, BlockImpactPostProcessSettings.MaxStreakThickness);
            StreakSoftness = Mathf.Clamp(streakSoftness, BlockImpactPostProcessSettings.MinStreakSoftness, BlockImpactPostProcessSettings.MaxStreakSoftness);
            StreakColor = streakColor;
            FlashWeight = Mathf.Clamp(flashWeight, 0f, 3f);
            RadialWeight = Mathf.Clamp(radialWeight, 0f, 3f);
            StreakWeight = Mathf.Clamp(streakWeight, 0f, 3f);
            ChromaticWeight = Mathf.Clamp(chromaticWeight, 0f, 3f);
        }

        public Vector2 Center { get; }
        public float Intensity { get; }
        public float Duration { get; }
        public float Elapsed { get; }
        public float StreakLength { get; }
        public float StreakThickness { get; }
        public float StreakSoftness { get; }
        public Color StreakColor { get; }
        public float FlashWeight { get; }
        public float RadialWeight { get; }
        public float StreakWeight { get; }
        public float ChromaticWeight { get; }
        public bool IsActive => Intensity > 0.0001f && Elapsed < Duration;
        public float Fade => IsActive ? 1f - Mathf.Clamp01(Elapsed / Duration) : 0f;
    }

    public static class BlockImpactPostProcessPulse
    {
        static BlockImpactPostProcessPulseState state = new BlockImpactPostProcessPulseState(new Vector2(0.5f, 0.5f), 0f, 0.2f, 0.2f);

        public static BlockImpactPostProcessPulseState Current => state;

        public static void Submit(Vector2 center, float intensity, float duration)
        {
            Submit(
                center,
                intensity,
                duration,
                1.2f,
                0.018f,
                0.7f,
                new Color(1f, 0.42f, 0.28f, 1f),
                1f,
                1f,
                1f,
                1f);
        }

        public static void Submit(
            Vector2 center,
            float intensity,
            float duration,
            float streakLength,
            float streakThickness,
            float streakSoftness,
            Color streakColor,
            float flashWeight,
            float radialWeight,
            float streakWeight,
            float chromaticWeight)
        {
            if (intensity <= 0f)
                return;

            if (state.IsActive && intensity < state.Intensity)
                intensity = state.Intensity;

            state = new BlockImpactPostProcessPulseState(
                center,
                intensity,
                duration,
                0f,
                streakLength,
                streakThickness,
                streakSoftness,
                streakColor,
                flashWeight,
                radialWeight,
                streakWeight,
                chromaticWeight);
        }

        public static void Tick(float deltaTime)
        {
            if (!state.IsActive)
                return;

            state = new BlockImpactPostProcessPulseState(
                state.Center,
                state.Intensity,
                state.Duration,
                state.Elapsed + Mathf.Max(0f, deltaTime),
                state.StreakLength,
                state.StreakThickness,
                state.StreakSoftness,
                state.StreakColor,
                state.FlashWeight,
                state.RadialWeight,
                state.StreakWeight,
                state.ChromaticWeight);
        }

        public static void Reset()
        {
            state = new BlockImpactPostProcessPulseState(new Vector2(0.5f, 0.5f), 0f, 0.2f, 0.2f);
        }
    }
}
