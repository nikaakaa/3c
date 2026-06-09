namespace ThirdPersonMovement
{
    public readonly struct BasicMovementSettings
    {
        public BasicMovementSettings(float maxPlanarSpeed, float inputDeadZone, float rotationSpeed, float moveStartMinTime, float moveStopMinTime)
            : this(maxPlanarSpeed, inputDeadZone, rotationSpeed, moveStartMinTime, moveStopMinTime, moveStopMinTime)
        {
        }

        public BasicMovementSettings(
            float maxPlanarSpeed,
            float inputDeadZone,
            float rotationSpeed,
            float moveStartMinTime,
            float moveStopMinTime,
            float moveStopExitDuration)
        {
            MaxPlanarSpeed = maxPlanarSpeed < 0f ? 0f : maxPlanarSpeed;
            InputDeadZone = inputDeadZone < 0f ? 0f : inputDeadZone > 1f ? 1f : inputDeadZone;
            RotationSpeed = rotationSpeed < 0f ? 0f : rotationSpeed;
            MoveStartMinTime = moveStartMinTime < 0f ? 0f : moveStartMinTime;
            MoveStopMinTime = moveStopMinTime < 0f ? 0f : moveStopMinTime;
            MoveStopExitDuration = moveStopExitDuration < 0f ? MoveStopMinTime : moveStopExitDuration;
        }

        public float MaxPlanarSpeed { get; }
        public float InputDeadZone { get; }
        public float RotationSpeed { get; }
        public float MoveStartMinTime { get; }
        public float MoveStopMinTime { get; }
        public float MoveStopExitDuration { get; }

        public BasicMovementSettings WithMoveStopExitDuration(float moveStopExitDuration)
        {
            return new BasicMovementSettings(MaxPlanarSpeed, InputDeadZone, RotationSpeed, MoveStartMinTime, MoveStopMinTime, moveStopExitDuration);
        }

        public static BasicMovementSettings FromConfig(BasicMovementConfigSO config)
        {
            return config != null
                ? new BasicMovementSettings(config.MaxPlanarSpeed, config.InputDeadZone, config.RotationSpeed, config.MoveStartMinTime, config.MoveStopMinTime)
                : new BasicMovementSettings(4f, 0.1f, 720f, 0.08f, 0.08f);
        }
    }
}
