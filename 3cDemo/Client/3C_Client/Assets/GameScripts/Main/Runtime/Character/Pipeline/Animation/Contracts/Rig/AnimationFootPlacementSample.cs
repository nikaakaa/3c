using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationFootPlacementSample
    {
        const float NormalizationTolerance = 0.00001f;

        readonly bool m_IsSpecified;

        public AnimationFootPlacementSample(
            float weight,
            in AnimationFootFeatureSample left,
            in AnimationFootFeatureSample right)
        {
            Weight = RequireWeight(weight, nameof(weight));
            if (!left.IsValid || !right.IsValid)
                throw new ArgumentException("Animation Foot Placement sample requires both generated foot features.");
            Left = left;
            Right = right;
            m_IsSpecified = true;
        }

        public float Weight { get; }
        public readonly AnimationFootFeatureSample Left;
        public readonly AnimationFootFeatureSample Right;
        public bool IsValid => m_IsSpecified && Left.IsValid && Right.IsValid;

        static float RequireWeight(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                value < -NormalizationTolerance || value > 1f + NormalizationTolerance)
                throw new ArgumentOutOfRangeException(field, value, "Animation Foot Placement sample must be normalized.");
            return value <= 0f ? 0f : value >= 1f ? 1f : value;
        }
    }
}
