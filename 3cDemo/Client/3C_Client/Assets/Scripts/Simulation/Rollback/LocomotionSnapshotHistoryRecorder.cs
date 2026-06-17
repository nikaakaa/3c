using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocomotionSnapshotHistoryRecorder : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] CharacterFrameRuntimeController runtimeController;
        [SerializeField] InputRequestBufferComponent inputBufferComponent;
        [SerializeField, Min(1)] int capacity = 120;

        PredictionSnapshotHistory history;
        bool registered;

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public CharacterFrameRuntimeController RuntimeController { get => runtimeController; set => runtimeController = value; }
        public InputRequestBufferComponent InputBufferComponent { get => inputBufferComponent; set => inputBufferComponent = value; }
        public PredictionSnapshotHistory History => history ?? (history = new PredictionSnapshotHistory(Mathf.Max(1, capacity)));
        public bool IsRegistered => registered;

        void Reset()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        public bool Register()
        {
            if (registered)
                return true;

            ResolveReferences();
            if (tickDriver == null || runtimeController == null)
                return false;

            tickDriver.Runner.Register(SimulationTickPhase.WriteSnapshotAndEvents, this);
            registered = true;
            return true;
        }

        public void Unregister()
        {
            if (!registered)
                return;

            if (tickDriver != null)
                tickDriver.Runner.Unregister(SimulationTickPhase.WriteSnapshotAndEvents, this);

            registered = false;
        }

        public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
        {
            if (phase != SimulationTickPhase.WriteSnapshotAndEvents)
                return;

            if (runtimeController == null)
                ResolveReferences();

            if (runtimeController != null)
            {
                CharacterSimulationSnapshot snapshot = runtimeController.CaptureSimulationSnapshot(context.Tick);
                History.Write(in snapshot);
            }
        }

        void ResolveReferences()
        {
            if (runtimeController == null)
                runtimeController = GetComponent<CharacterFrameRuntimeController>();

            if (inputBufferComponent == null)
                inputBufferComponent = runtimeController != null ? runtimeController.InputBufferComponent : null;
            if (inputBufferComponent == null)
                inputBufferComponent = GetComponent<InputRequestBufferComponent>();

            if (tickDriver == null)
                tickDriver = GetComponent<UnitySimulationTickDriver>();
        }
    }
}
