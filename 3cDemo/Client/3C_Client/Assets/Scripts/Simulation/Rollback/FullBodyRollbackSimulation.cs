using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class FullBodyRollbackSimulation : MonoBehaviour, ILocalRollbackSynctestSimulation, ILocalRollbackDebugRestoreCleanup
    {
        [SerializeField] PlayerFullBodyActionController fullBodyActionController;
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField] InputRequestBufferComponent inputBufferComponent;
        [SerializeField, Min(1)] int ticksPerSecond = SimulationTickRate.DefaultTicksPerSecond;

        public PlayerFullBodyActionController FullBodyActionController { get => fullBodyActionController; set => fullBodyActionController = value; }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public InputRequestBufferComponent InputBufferComponent { get => inputBufferComponent; set => inputBufferComponent = value; }

        void Reset()
        {
            ResolveReferences();
        }

        public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
        {
            ResolveReferences();
            if (locomotionController == null)
                return default;

            CharacterSimulationSnapshot snapshot = locomotionController.CaptureSimulationSnapshot(tick);
            FullBodyActionRestoreState fullBodyState = fullBodyActionController != null
                ? fullBodyActionController.CaptureRestoreState()
                : FullBodyActionRestoreState.Inactive;
            InputRequestBufferComponentRestoreState inputBufferState = inputBufferComponent != null
                ? inputBufferComponent.CaptureRestoreState()
                : InputRequestBufferComponentRestoreState.Empty;

            return snapshot.WithFullBodyState(in fullBodyState, in inputBufferState);
        }

        public void Restore(in CharacterSimulationSnapshot snapshot)
        {
            ResolveReferences();
            if (fullBodyActionController != null && snapshot.FullBodyRestoreState.Snapshot.ActiveState.IsValid)
                fullBodyActionController.Restore(snapshot.FullBodyRestoreState);
            if (inputBufferComponent != null)
                inputBufferComponent.Restore(snapshot.InputBufferRestoreState);
            if (locomotionController != null)
                locomotionController.RestoreSimulationSnapshot(in snapshot);
            if (fullBodyActionController != null)
                fullBodyActionController.RestoreActionAnimationPlayback(
                    snapshot.RuntimeBlackboard.Animation.ActionProgress,
                    snapshot.RuntimeBlackboard.Animation.ActionAnimationName);

        }

        public void Advance(in PredictionInputFrame input)
        {
            ResolveReferences();
            if (fullBodyActionController == null)
                return;

            if (input.HasCameraBasis && locomotionController != null)
                locomotionController.RollbackCameraBasisProvider.Override(input.CameraBasisState);

            FullBodyFrameInput frameInput = FullBodyFrameInput.FromPredictionInputFrame(in input, ResolveDeltaTime());
            fullBodyActionController.Tick(in frameInput);
        }

        public void CompleteDebugRestore()
        {
            ResolveReferences();
            if (locomotionController != null)
                locomotionController.ReleaseRollbackCameraBasisOverride();
        }

        float ResolveDeltaTime()
        {
            int safeTicks = ticksPerSecond < 1 ? 1 : ticksPerSecond;
            return 1f / safeTicks;
        }

        void ResolveReferences()
        {
            if (fullBodyActionController == null)
            {
                fullBodyActionController = GetComponent<PlayerFullBodyActionController>();
                if (fullBodyActionController == null)
                    fullBodyActionController = GetComponentInParent<PlayerFullBodyActionController>();
                if (fullBodyActionController == null)
                    fullBodyActionController = GetComponentInChildren<PlayerFullBodyActionController>(true);
            }

            if (locomotionController == null && fullBodyActionController != null)
                locomotionController = fullBodyActionController.LocomotionController;
            if (locomotionController == null)
            {
                locomotionController = GetComponent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInParent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInChildren<PlayerLocomotionController>(true);
            }

            if (inputBufferComponent == null && fullBodyActionController != null)
                inputBufferComponent = fullBodyActionController.InputBufferComponent;
            if (inputBufferComponent == null)
            {
                inputBufferComponent = GetComponent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInParent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInChildren<InputRequestBufferComponent>(true);
            }
        }
    }
}
