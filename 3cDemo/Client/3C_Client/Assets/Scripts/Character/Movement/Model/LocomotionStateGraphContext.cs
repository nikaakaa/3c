namespace ThirdPersonMovement
{
    public readonly struct LocomotionStateGraphContext
    {
        public LocomotionStateGraphContext(
            bool hasMoveIntent,
            float phaseTime,
            float deltaTime,
            in BasicMovementSettings settings)
        {
            HasMoveIntent = hasMoveIntent;
            PhaseTime = phaseTime < 0f ? 0f : phaseTime;
            DeltaTime = deltaTime < 0f ? 0f : deltaTime;
            Settings = settings;
        }

        public bool HasMoveIntent { get; }
        public float PhaseTime { get; }
        public float DeltaTime { get; }
        public BasicMovementSettings Settings { get; }
    }
}
