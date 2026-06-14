using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public readonly struct LocomotionFootPhaseSample
    {
        public LocomotionFootPhaseSample(
            bool isValid,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            float normalizedTime,
            LocomotionFootPhase footPhase,
            int sourceStep)
        {
            IsValid = isValid && footPhase != LocomotionFootPhase.Unknown && !string.IsNullOrWhiteSpace(aliasKey);
            Phase = phase;
            Gait = gait;
            AliasKey = aliasKey ?? string.Empty;
            NormalizedTime = normalizedTime < 0f ? 0f : normalizedTime;
            FootPhase = IsValid ? footPhase : LocomotionFootPhase.Unknown;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public bool IsValid { get; }
        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public string AliasKey { get; }
        public float NormalizedTime { get; }
        public LocomotionFootPhase FootPhase { get; }
        public int SourceStep { get; }

        public static LocomotionFootPhaseSample Invalid(BasicMovementPhase phase)
        {
            return new LocomotionFootPhaseSample(false, phase, BasicMovementGait.Walk, string.Empty, 0f, LocomotionFootPhase.Unknown, 0);
        }

        public static LocomotionFootPhaseSample Invalid(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            float normalizedTime,
            int sourceStep)
        {
            return new LocomotionFootPhaseSample(false, phase, gait, aliasKey, normalizedTime, LocomotionFootPhase.Unknown, sourceStep);
        }

        public LocomotionFootPhaseSample WithSourceStep(int sourceStep)
        {
            return new LocomotionFootPhaseSample(IsValid, Phase, Gait, AliasKey, NormalizedTime, FootPhase, sourceStep);
        }
    }
}
