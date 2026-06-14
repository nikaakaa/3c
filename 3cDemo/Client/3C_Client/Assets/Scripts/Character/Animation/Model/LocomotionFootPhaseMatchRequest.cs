using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public readonly struct LocomotionFootPhaseMatchRequest
    {
        public LocomotionFootPhaseMatchRequest(
            LocomotionFootPhaseSample exitFootPhase,
            BasicMovementPhase targetPhase,
            BasicMovementGait targetGait,
            string targetAliasKey)
        {
            ExitFootPhase = exitFootPhase;
            TargetPhase = targetPhase;
            TargetGait = targetGait;
            TargetAliasKey = targetAliasKey ?? string.Empty;
        }

        public LocomotionFootPhaseSample ExitFootPhase { get; }
        public BasicMovementPhase TargetPhase { get; }
        public BasicMovementGait TargetGait { get; }
        public string TargetAliasKey { get; }
        public bool IsValid => ExitFootPhase.IsValid && !string.IsNullOrWhiteSpace(TargetAliasKey);

        public static LocomotionFootPhaseMatchRequest Invalid =>
            new LocomotionFootPhaseMatchRequest(default, BasicMovementPhase.Idle, BasicMovementGait.Walk, string.Empty);
    }
}

