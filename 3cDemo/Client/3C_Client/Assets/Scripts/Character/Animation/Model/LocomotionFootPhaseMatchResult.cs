namespace ThirdPersonAnimation
{
    public readonly struct LocomotionFootPhaseMatchResult
    {
        public LocomotionFootPhaseMatchResult(
            bool isValid,
            LocomotionFootPhase matchedPhase,
            float startNormalizedTime,
            string targetAliasKey,
            string reason)
        {
            IsValid = isValid && matchedPhase != LocomotionFootPhase.Unknown;
            MatchedPhase = IsValid ? matchedPhase : LocomotionFootPhase.Unknown;
            StartNormalizedTime = startNormalizedTime < 0f ? 0f : startNormalizedTime > 1f ? 1f : startNormalizedTime;
            TargetAliasKey = targetAliasKey ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public bool IsValid { get; }
        public LocomotionFootPhase MatchedPhase { get; }
        public float StartNormalizedTime { get; }
        public string TargetAliasKey { get; }
        public string Reason { get; }

        public static LocomotionFootPhaseMatchResult Invalid(string reason)
        {
            return new LocomotionFootPhaseMatchResult(false, LocomotionFootPhase.Unknown, 0f, string.Empty, reason);
        }

        public static LocomotionFootPhaseMatchResult NotRequested => Invalid("not-requested");
    }
}
