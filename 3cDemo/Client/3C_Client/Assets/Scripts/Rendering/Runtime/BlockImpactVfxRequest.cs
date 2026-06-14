using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct BlockImpactVfxRequest
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 3f;
        public const float MinDuration = 0.02f;
        public const float MaxDuration = 2f;

        public static readonly BlockImpactVfxRequest Default = new BlockImpactVfxRequest(
            Vector3.zero,
            Vector3.forward,
            new Vector2(0.5f, 0.5f),
            1f,
            0.28f,
            0,
            true,
            true,
            true,
            true,
            true);

        public BlockImpactVfxRequest(
            Vector3 worldHitPoint,
            Vector3 attackDirection,
            Vector2 screenCenter,
            float intensity,
            float duration,
            int randomSeed,
            bool flashEnabled,
            bool sparksEnabled,
            bool arcsEnabled,
            bool streakEnabled,
            bool screenImpactEnabled)
        {
            WorldHitPoint = worldHitPoint;
            AttackDirection = NormalizeDirection(attackDirection);
            HitNormal = NormalizeDirection(-AttackDirection);
            ScreenCenter = new Vector2(Mathf.Clamp01(screenCenter.x), Mathf.Clamp01(screenCenter.y));
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            Duration = Mathf.Clamp(duration, MinDuration, MaxDuration);
            RandomSeed = randomSeed;
            FlashEnabled = flashEnabled;
            SparksEnabled = sparksEnabled;
            ArcsEnabled = arcsEnabled;
            StreakEnabled = streakEnabled;
            ScreenImpactEnabled = screenImpactEnabled;
        }

        public Vector3 WorldHitPoint { get; }
        public Vector3 AttackDirection { get; }
        public Vector3 HitNormal { get; }
        public Vector2 ScreenCenter { get; }
        public float Intensity { get; }
        public float Duration { get; }
        public int RandomSeed { get; }
        public bool FlashEnabled { get; }
        public bool SparksEnabled { get; }
        public bool ArcsEnabled { get; }
        public bool StreakEnabled { get; }
        public bool ScreenImpactEnabled { get; }

        public BlockImpactVfxRequest WithWorldHitPoint(Vector3 worldHitPoint)
        {
            return new BlockImpactVfxRequest(
                worldHitPoint,
                AttackDirection,
                ScreenCenter,
                Intensity,
                Duration,
                RandomSeed,
                FlashEnabled,
                SparksEnabled,
                ArcsEnabled,
                StreakEnabled,
                ScreenImpactEnabled);
        }

        public static Vector3 NormalizeDirection(Vector3 direction)
        {
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
