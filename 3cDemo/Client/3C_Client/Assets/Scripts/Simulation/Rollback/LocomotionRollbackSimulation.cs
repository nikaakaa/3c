using ThirdPersonAction;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocomotionRollbackSimulation : MonoBehaviour, ILocalRollbackSynctestSimulation, ILocalRollbackDebugRestoreCleanup
    {
        [SerializeField] CharacterFrameRuntimeController runtimeController;
        [SerializeField, Min(1)] int ticksPerSecond = SimulationTickRate.DefaultTicksPerSecond;

        public CharacterFrameRuntimeController RuntimeController { get => runtimeController; set => runtimeController = value; }

        void Reset()
        {
            ResolveReferences();
        }

        public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
        {
            ResolveReferences();
            if (runtimeController != null)
                return runtimeController.CaptureSimulationSnapshot(tick);

            return default;
        }

        public void Restore(in CharacterSimulationSnapshot snapshot)
        {
            ResolveReferences();
            if (runtimeController != null)
                runtimeController.RestoreSimulationSnapshot(in snapshot);
        }

        public void Advance(in PredictionInputFrame input)
        {
            ResolveReferences();
            if (runtimeController != null)
            {
                if (input.HasCameraBasis)
                    runtimeController.RollbackCameraBasisProvider.Override(input.CameraBasisState);

                CharacterFrameInput frameInput = CharacterFrameInput.FromPredictionInputFrame(in input, ResolveDeltaTime());
                runtimeController.Tick(in frameInput);
                return;
            }
        }

        public void CompleteDebugRestore()
        {
            ResolveReferences();
            if (runtimeController != null)
                runtimeController.ReleaseRollbackCameraBasisOverride();
        }

        float ResolveDeltaTime()
        {
            int safeTicks = ticksPerSecond < 1 ? 1 : ticksPerSecond;
            return 1f / safeTicks;
        }

        void ResolveReferences()
        {
            if (runtimeController == null)
            {
                runtimeController = GetComponent<CharacterFrameRuntimeController>();
                if (runtimeController == null)
                    runtimeController = GetComponentInParent<CharacterFrameRuntimeController>();
            }

        }
    }

}
