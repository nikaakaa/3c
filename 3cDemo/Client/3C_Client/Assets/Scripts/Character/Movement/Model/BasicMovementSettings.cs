namespace ThirdPersonMovement
{
    public readonly struct BasicMovementSettings
    {
        public BasicMovementSettings(float maxPlanarSpeed, float inputDeadZone, float rotationSpeed, float moveStartMinTime, float moveStopMinTime)
            : this(
                maxPlanarSpeed,
                maxPlanarSpeed,
                inputDeadZone,
                rotationSpeed,
                moveStartMinTime,
                moveStopMinTime,
                BasicMovementPhaseTiming.Manual,
                BasicMovementPhaseTiming.AfterDuration(moveStartMinTime),
                BasicMovementPhaseTiming.Manual,
                BasicMovementPhaseTiming.AfterDuration(moveStopMinTime),
                BasicMovementPhaseTiming.Manual)
        {
        }

        public BasicMovementSettings(
            float maxPlanarSpeed,
            float inputDeadZone,
            float rotationSpeed,
            float moveStartMinTime,
            float moveStopMinTime,
            float moveStopExitDuration)
            : this(
                maxPlanarSpeed,
                maxPlanarSpeed,
                inputDeadZone,
                rotationSpeed,
                moveStartMinTime,
                moveStopMinTime,
                BasicMovementPhaseTiming.Manual,
                BasicMovementPhaseTiming.AfterDuration(moveStartMinTime),
                BasicMovementPhaseTiming.Manual,
                BasicMovementPhaseTiming.AfterDuration(moveStopExitDuration < 0f ? moveStopMinTime : moveStopExitDuration),
                BasicMovementPhaseTiming.Manual)
        {
        }

        public BasicMovementSettings(
            float maxPlanarSpeed,
            float inputDeadZone,
            float rotationSpeed,
            float moveStartMinTime,
            float moveStopMinTime,
            BasicMovementPhaseTiming moveStartTiming,
            BasicMovementPhaseTiming moveStopTiming)
            : this(
                maxPlanarSpeed,
                maxPlanarSpeed,
                inputDeadZone,
                rotationSpeed,
                moveStartMinTime,
                moveStopMinTime,
                moveStartTiming,
                moveStopTiming)
        {
        }

        public BasicMovementSettings(
            float walkPlanarSpeed,
            float runPlanarSpeed,
            float inputDeadZone,
            float rotationSpeed,
            float moveStartMinTime,
            float moveStopMinTime,
            BasicMovementPhaseTiming moveStartTiming,
            BasicMovementPhaseTiming moveStopTiming)
            : this(
                walkPlanarSpeed,
                runPlanarSpeed,
                inputDeadZone,
                rotationSpeed,
                moveStartMinTime,
                moveStopMinTime,
                BasicMovementPhaseTiming.Manual,
                moveStartTiming,
                BasicMovementPhaseTiming.Manual,
                moveStopTiming,
                BasicMovementPhaseTiming.Manual)
        {
        }

        BasicMovementSettings(
            float walkPlanarSpeed,
            float runPlanarSpeed,
            float inputDeadZone,
            float rotationSpeed,
            float moveStartMinTime,
            float moveStopMinTime,
            BasicMovementPhaseTiming idleTiming,
            BasicMovementPhaseTiming moveStartTiming,
            BasicMovementPhaseTiming moveLoopTiming,
            BasicMovementPhaseTiming moveStopTiming,
            BasicMovementPhaseTiming turnBackTiming)
        {
            WalkPlanarSpeed = ClampNonNegative(walkPlanarSpeed);
            RunPlanarSpeed = ClampNonNegative(runPlanarSpeed);
            MaxPlanarSpeed = RunPlanarSpeed;
            InputDeadZone = inputDeadZone < 0f ? 0f : inputDeadZone > 1f ? 1f : inputDeadZone;
            RotationSpeed = ClampNonNegative(rotationSpeed);
            MoveStartMinTime = ClampNonNegative(moveStartMinTime);
            MoveStopMinTime = ClampNonNegative(moveStopMinTime);
            this.idleTiming = idleTiming;
            this.moveStartTiming = moveStartTiming;
            this.moveLoopTiming = moveLoopTiming;
            this.moveStopTiming = moveStopTiming;
            this.turnBackTiming = turnBackTiming;
        }

        readonly BasicMovementPhaseTiming idleTiming;
        readonly BasicMovementPhaseTiming moveStartTiming;
        readonly BasicMovementPhaseTiming moveLoopTiming;
        readonly BasicMovementPhaseTiming moveStopTiming;
        readonly BasicMovementPhaseTiming turnBackTiming;

        public float MaxPlanarSpeed { get; }
        public float WalkPlanarSpeed { get; }
        public float RunPlanarSpeed { get; }
        public float InputDeadZone { get; }
        public float RotationSpeed { get; }
        public float MoveStartMinTime { get; }
        public float MoveStopMinTime { get; }
        public float MoveStopExitDuration => moveStopTiming.ExitsAfterDuration ? moveStopTiming.ExitDuration : MoveStopMinTime;

        public BasicMovementSettings WithMoveStopExitDuration(float moveStopExitDuration)
        {
            return WithPhaseTiming(BasicMovementPhase.MoveStop, BasicMovementPhaseTiming.AfterDuration(moveStopExitDuration < 0f ? MoveStopMinTime : moveStopExitDuration));
        }

        public BasicMovementSettings WithPhaseTiming(BasicMovementPhase phase, BasicMovementPhaseTiming timing)
        {
            BasicMovementPhaseTiming nextIdle = idleTiming;
            BasicMovementPhaseTiming nextMoveStart = moveStartTiming;
            BasicMovementPhaseTiming nextMoveLoop = moveLoopTiming;
            BasicMovementPhaseTiming nextMoveStop = moveStopTiming;
            BasicMovementPhaseTiming nextTurnBack = turnBackTiming;

            switch (phase)
            {
                case BasicMovementPhase.MoveStart:
                    nextMoveStart = timing;
                    break;
                case BasicMovementPhase.MoveLoop:
                    nextMoveLoop = timing;
                    break;
                case BasicMovementPhase.MoveStop:
                    nextMoveStop = timing;
                    break;
                case BasicMovementPhase.TurnBack:
                    nextTurnBack = timing;
                    break;
                default:
                    nextIdle = timing;
                    break;
            }

            return new BasicMovementSettings(
                WalkPlanarSpeed,
                RunPlanarSpeed,
                InputDeadZone,
                RotationSpeed,
                MoveStartMinTime,
                MoveStopMinTime,
                nextIdle,
                nextMoveStart,
                nextMoveLoop,
                nextMoveStop,
                nextTurnBack);
        }

        public BasicMovementPhaseTiming ResolvePhaseTiming(BasicMovementPhase phase)
        {
            return phase switch
            {
                BasicMovementPhase.MoveStart => moveStartTiming,
                BasicMovementPhase.MoveLoop => moveLoopTiming,
                BasicMovementPhase.MoveStop => moveStopTiming,
                BasicMovementPhase.TurnBack => turnBackTiming,
                _ => idleTiming
            };
        }

        public float ResolvePlanarSpeed(BasicMovementGait gait)
        {
            return gait == BasicMovementGait.Run ? RunPlanarSpeed : WalkPlanarSpeed;
        }

        public bool IsPhaseExitTimeReached(BasicMovementPhase phase, float phaseTime)
        {
            return ResolvePhaseTiming(phase).IsExitTimeReached(phaseTime < 0f ? 0f : phaseTime);
        }

        public static BasicMovementSettings FromConfig(BasicMovementConfigSO config)
        {
            return config != null
                ? new BasicMovementSettings(
                    config.WalkPlanarSpeed,
                    config.RunPlanarSpeed,
                    config.InputDeadZone,
                    config.RotationSpeed,
                    config.MoveStartMinTime,
                    config.MoveStopMinTime,
                    BasicMovementPhaseTiming.Manual,
                    BasicMovementPhaseTiming.AfterDuration(config.MoveStartMinTime),
                    BasicMovementPhaseTiming.Manual,
                    BasicMovementPhaseTiming.AfterDuration(config.MoveStopMinTime),
                    BasicMovementPhaseTiming.Manual)
                : new BasicMovementSettings(
                    2f,
                    4f,
                    0.1f,
                    720f,
                    0.08f,
                    0.08f,
                    BasicMovementPhaseTiming.Manual,
                    BasicMovementPhaseTiming.AfterDuration(0.08f),
                    BasicMovementPhaseTiming.Manual,
                    BasicMovementPhaseTiming.AfterDuration(0.08f),
                    BasicMovementPhaseTiming.Manual);
        }

        static float ClampNonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }
    }
}
