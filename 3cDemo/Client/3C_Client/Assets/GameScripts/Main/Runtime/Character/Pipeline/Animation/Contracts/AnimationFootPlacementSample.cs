using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationFootPlacementSample
    {
        const float NormalizationTolerance = 0.00001f;

        readonly bool m_IsSpecified;

        public AnimationFootPlacementSample(float weight)
        {
            Weight = RequireWeight(weight, nameof(weight));
            m_IsSpecified = true;
        }

        public float Weight { get; }
        public bool IsValid => m_IsSpecified;

        static float RequireWeight(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                value < -NormalizationTolerance || value > 1f + NormalizationTolerance)
                throw new ArgumentOutOfRangeException(field, value, "Animation Foot Placement sample must be normalized.");
            return value <= 0f ? 0f : value >= 1f ? 1f : value;
        }
    }
}
