namespace ThirdPersonMovement
{
    public readonly struct BasicMovementPhaseFacts
    {
        public BasicMovementPhaseFacts(bool phaseCanExit)
        {
            PhaseCanExit = phaseCanExit;
        }

        public bool PhaseCanExit { get; }

        public static BasicMovementPhaseFacts None => new BasicMovementPhaseFacts(false);

        public static BasicMovementPhaseFacts FromTiming(
            BasicMovementPhase phase,
            float phaseTime,
            in BasicMovementSettings settings)
        {
            return new BasicMovementPhaseFacts(settings.IsPhaseExitTimeReached(phase, phaseTime));
        }
    }
}
