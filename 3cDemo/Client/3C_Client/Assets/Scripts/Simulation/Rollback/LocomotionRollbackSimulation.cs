using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocomotionRollbackSimulation : MonoBehaviour, ILocalRollbackSynctestSimulation, ILocalRollbackDebugRestoreCleanup
    {
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField, Min(1)] int ticksPerSecond = SimulationTickRate.DefaultTicksPerSecond;

        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }

        void Reset()
        {
            ResolveReferences();
        }

        public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
        {
            ResolveReferences();
            return locomotionController != null
                ? locomotionController.CaptureSimulationSnapshot(tick)
                : default;
        }

        public void Restore(in CharacterSimulationSnapshot snapshot)
        {
            ResolveReferences();
            if (locomotionController != null)
                locomotionController.RestoreSimulationSnapshot(in snapshot);
        }

        public void Advance(in PredictionInputFrame input)
        {
            ResolveReferences();
            if (locomotionController == null)
                return;

            if (input.HasCameraBasis)
                locomotionController.RollbackCameraBasisProvider.Override(input.CameraBasisState);

            locomotionController.Tick(input.ToLocomotionInput(ResolveDeltaTime()), input.Tick.Value);
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
            if (locomotionController == null)
            {
                locomotionController = GetComponent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInParent<PlayerLocomotionController>();
            }
        }
    }

}
