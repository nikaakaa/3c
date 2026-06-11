using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public readonly struct AnimationPhasePlaybackProgress
    {
        public AnimationPhasePlaybackProgress(
            BasicMovementPhase phase,
            string aliasKey,
            float normalizedTime,
            bool hasValidPlayback,
            bool isEnded)
        {
            Phase = phase;
            AliasKey = aliasKey ?? string.Empty;
            NormalizedTime = normalizedTime < 0f ? 0f : normalizedTime;
            HasValidPlayback = hasValidPlayback;
            IsEnded = isEnded;
        }

        public BasicMovementPhase Phase { get; }
        public string AliasKey { get; }
        public float NormalizedTime { get; }
        public bool HasValidPlayback { get; }
        public bool IsEnded { get; }

        public static AnimationPhasePlaybackProgress Invalid(BasicMovementPhase phase)
        {
            return new AnimationPhasePlaybackProgress(phase, string.Empty, 0f, false, false);
        }
    }
}
