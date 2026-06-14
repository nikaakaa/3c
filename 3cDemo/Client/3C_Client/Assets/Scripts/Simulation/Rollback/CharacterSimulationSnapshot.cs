using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterSimulationSnapshot
    {
        public CharacterSimulationSnapshot(
            SimulationTick tick,
            Vector3 position,
            float yaw,
            CharacterStateMachineSnapshot stateMachine,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            string animationKey,
            float animationNormalizedTime)
            : this(
                tick,
                position,
                yaw,
                new CharacterStateMachineRestoreState(stateMachine, Vector3.zero, false, false, false, false),
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
                locomotionPhase,
                locomotionGait,
                animationKey,
                animationNormalizedTime,
                CharacterRuntimeBlackboardRestoreState.Empty,
                FullBodyActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty,
                0f)
        {
        }

        public CharacterSimulationSnapshot(
            SimulationTick tick,
            Vector3 position,
            float yaw,
            CharacterStateMachineRestoreState stateMachineRestoreState,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            string animationKey,
            float animationNormalizedTime)
            : this(
                tick,
                position,
                yaw,
                stateMachineRestoreState,
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
                locomotionPhase,
                locomotionGait,
                animationKey,
                animationNormalizedTime,
                CharacterRuntimeBlackboardRestoreState.Empty,
                FullBodyActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty,
                0f)
        {
        }

        public CharacterSimulationSnapshot(
            SimulationTick tick,
            Vector3 position,
            float yaw,
            CharacterStateMachineRestoreState stateMachineRestoreState,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            string animationKey,
            float animationNormalizedTime,
            CharacterRuntimeBlackboardRestoreState runtimeBlackboardRestoreState)
            : this(
                tick,
                position,
                yaw,
                stateMachineRestoreState,
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
                locomotionPhase,
                locomotionGait,
                animationKey,
                animationNormalizedTime,
                runtimeBlackboardRestoreState,
                FullBodyActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty,
                0f)
        {
        }

        public CharacterSimulationSnapshot(
            SimulationTick tick,
            Vector3 position,
            float yaw,
            CharacterStateMachineRestoreState stateMachineRestoreState,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            string animationKey,
            float animationNormalizedTime,
            CharacterRuntimeBlackboardRestoreState runtimeBlackboardRestoreState,
            float cameraYaw)
            : this(
                tick,
                position,
                yaw,
                stateMachineRestoreState,
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
                locomotionPhase,
                locomotionGait,
                animationKey,
                animationNormalizedTime,
                runtimeBlackboardRestoreState,
                FullBodyActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty,
                cameraYaw)
        {
        }

        public CharacterSimulationSnapshot(
            SimulationTick tick,
            Vector3 position,
            float yaw,
            CharacterStateMachineRestoreState stateMachineRestoreState,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            string animationKey,
            float animationNormalizedTime,
            CharacterRuntimeBlackboardRestoreState runtimeBlackboardRestoreState,
            FullBodyActionRestoreState fullBodyRestoreState,
            InputRequestBufferComponentRestoreState inputBufferRestoreState,
            float cameraYaw = 0f,
            RollbackCameraBasisState cameraBasisState = default,
            LocomotionRuntimeRollbackState locomotionRuntimeState = default,
            MotionExecutorRollbackState motionExecutorState = default)
        {
            Tick = tick;
            Position = Sanitize(position);
            Yaw = SanitizeAngle(yaw);
            StateMachineRestoreState = stateMachineRestoreState;
            RuntimeBlackboardRestoreState = runtimeBlackboardRestoreState;
            FullBodyRestoreState = fullBodyRestoreState;
            InputBufferRestoreState = inputBufferRestoreState;
            RunLatchActive = runLatchActive;
            LastMovingGait = lastMovingGait;
            CurrentWorldDirection = NormalizePlanarOrZero(currentWorldDirection);
            LocomotionPhase = locomotionPhase;
            LocomotionGait = locomotionGait;
            AnimationKey = animationKey ?? string.Empty;
            AnimationNormalizedTime = SanitizeNonNegative(animationNormalizedTime);
            float sanitizedCameraYaw = SanitizeAngle(cameraYaw);
            CameraBasisState = cameraBasisState.Equals(default)
                ? new RollbackCameraBasisState(
                    Quaternion.Euler(0f, sanitizedCameraYaw, 0f) * Vector3.forward,
                    Quaternion.Euler(0f, sanitizedCameraYaw, 0f) * Vector3.right,
                    sanitizedCameraYaw)
                : cameraBasisState;
            LocomotionRuntimeState = locomotionRuntimeState.Equals(default)
                ? LocomotionRuntimeRollbackState.Empty
                : locomotionRuntimeState;
            MotionExecutorState = motionExecutorState.Equals(default)
                ? MotionExecutorRollbackState.Empty
                : motionExecutorState;
        }

        public SimulationTick Tick { get; }

        public Vector3 Position { get; }
        public float Yaw { get; }

        public CharacterStateMachineRestoreState StateMachineRestoreState { get; }
        public CharacterRuntimeBlackboardRestoreState RuntimeBlackboardRestoreState { get; }
        public FullBodyActionRestoreState FullBodyRestoreState { get; }
        public InputRequestBufferComponentRestoreState InputBufferRestoreState { get; }
        public CharacterStateMachineSnapshot StateMachine => StateMachineRestoreState.Snapshot;
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboard => RuntimeBlackboardRestoreState.Snapshot;

        public bool RunLatchActive { get; }
        public BasicMovementGait LastMovingGait { get; }
        public Vector3 CurrentWorldDirection { get; }
        public BasicMovementPhase LocomotionPhase { get; }
        public BasicMovementGait LocomotionGait { get; }

        public string AnimationKey { get; }
        public float AnimationNormalizedTime { get; }

        public RollbackCameraBasisState CameraBasisState { get; }
        public LocomotionRuntimeRollbackState LocomotionRuntimeState { get; }
        public MotionExecutorRollbackState MotionExecutorState { get; }

        public CharacterSimulationSnapshot WithFullBodyState(
            in FullBodyActionRestoreState fullBodyRestoreState,
            in InputRequestBufferComponentRestoreState inputBufferRestoreState)
        {
            return new CharacterSimulationSnapshot(
                Tick,
                Position,
                Yaw,
                StateMachineRestoreState,
                RunLatchActive,
                LastMovingGait,
                CurrentWorldDirection,
                LocomotionPhase,
                LocomotionGait,
                AnimationKey,
                AnimationNormalizedTime,
                RuntimeBlackboardRestoreState,
                fullBodyRestoreState,
                inputBufferRestoreState,
                CameraBasisState.Yaw,
                CameraBasisState,
                LocomotionRuntimeState,
                MotionExecutorState);
        }

        public CharacterSimulationSnapshot WithCameraBasis(in RollbackCameraBasisState cameraBasisState)
        {
            return new CharacterSimulationSnapshot(
                Tick,
                Position,
                Yaw,
                StateMachineRestoreState,
                RunLatchActive,
                LastMovingGait,
                CurrentWorldDirection,
                LocomotionPhase,
                LocomotionGait,
                AnimationKey,
                AnimationNormalizedTime,
                RuntimeBlackboardRestoreState,
                FullBodyRestoreState,
                InputBufferRestoreState,
                cameraBasisState.Yaw,
                cameraBasisState,
                LocomotionRuntimeState,
                MotionExecutorState);
        }

        public CharacterSimulationSnapshot WithLocomotionRuntimeState(in LocomotionRuntimeRollbackState locomotionRuntimeState)
        {
            return new CharacterSimulationSnapshot(
                Tick,
                Position,
                Yaw,
                StateMachineRestoreState,
                RunLatchActive,
                LastMovingGait,
                CurrentWorldDirection,
                LocomotionPhase,
                LocomotionGait,
                AnimationKey,
                AnimationNormalizedTime,
                RuntimeBlackboardRestoreState,
                FullBodyRestoreState,
                InputBufferRestoreState,
                CameraBasisState.Yaw,
                CameraBasisState,
                locomotionRuntimeState,
                MotionExecutorState);
        }

        public CharacterSimulationSnapshot WithMotionExecutorState(in MotionExecutorRollbackState motionExecutorState)
        {
            return new CharacterSimulationSnapshot(
                Tick,
                Position,
                Yaw,
                StateMachineRestoreState,
                RunLatchActive,
                LastMovingGait,
                CurrentWorldDirection,
                LocomotionPhase,
                LocomotionGait,
                AnimationKey,
                AnimationNormalizedTime,
                RuntimeBlackboardRestoreState,
                FullBodyRestoreState,
                InputBufferRestoreState,
                CameraBasisState.Yaw,
                CameraBasisState,
                LocomotionRuntimeState,
                motionExecutorState);
        }

        static Vector3 Sanitize(Vector3 value)
        {
            return new Vector3(SanitizeFinite(value.x), SanitizeFinite(value.y), SanitizeFinite(value.z));
        }

        static float SanitizeAngle(float value)
        {
            value = SanitizeFinite(value);
            value %= 360f;
            if (value < 0f)
                value += 360f;

            return value;
        }

        static float SanitizeNonNegative(float value)
        {
            value = SanitizeFinite(value);
            return value < 0f ? 0f : value;
        }

        static float SanitizeFinite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
