namespace ThirdPersonMovement
{
    public readonly struct BasicMovementPhaseTiming
    {
        const float TimeEpsilon = 0.000001f;

        BasicMovementPhaseTiming(bool exitsAfterDuration, float exitDuration)
        {
            ExitsAfterDuration = exitsAfterDuration;
            ExitDuration = exitDuration < 0f ? 0f : exitDuration;
        }

        public bool ExitsAfterDuration { get; }
        public float ExitDuration { get; }

        public static BasicMovementPhaseTiming Manual => new BasicMovementPhaseTiming(false, 0f);

        public static BasicMovementPhaseTiming AfterDuration(float exitDuration)
        {
            return new BasicMovementPhaseTiming(true, exitDuration);
        }

        public bool IsExitTimeReached(float phaseTime)
        {
            return ExitsAfterDuration && phaseTime + TimeEpsilon >= ExitDuration;
        }
    }
}
