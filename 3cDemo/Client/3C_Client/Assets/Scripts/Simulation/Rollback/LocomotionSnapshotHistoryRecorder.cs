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
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField] PlayerFullBodyActionController fullBodyActionController;
        [SerializeField] InputRequestBufferComponent inputBufferComponent;
        [SerializeField, Min(1)] int capacity = 120;

        PredictionSnapshotHistory history;
        bool registered;

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public PlayerFullBodyActionController FullBodyActionController { get => fullBodyActionController; set => fullBodyActionController = value; }
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
            if (tickDriver == null || locomotionController == null)
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

            if (locomotionController == null)
                ResolveReferences();

            if (locomotionController != null)
            {
                CharacterSimulationSnapshot snapshot = locomotionController.CaptureSimulationSnapshot(context.Tick);
                snapshot = EnrichFullBodyState(in snapshot);
                History.Write(in snapshot);
            }
        }

        CharacterSimulationSnapshot EnrichFullBodyState(in CharacterSimulationSnapshot snapshot)
        {
            if (fullBodyActionController == null)
                ResolveReferences();

            FullBodyActionRestoreState fullBodyState = fullBodyActionController != null
                ? fullBodyActionController.CaptureRestoreState()
                : FullBodyActionRestoreState.Inactive;
            InputRequestBufferComponentRestoreState inputBufferState = inputBufferComponent != null
                ? inputBufferComponent.CaptureRestoreState()
                : InputRequestBufferComponentRestoreState.Empty;

            return snapshot.WithFullBodyState(in fullBodyState, in inputBufferState);
        }

        void ResolveReferences()
        {
            if (locomotionController == null)
            {
                locomotionController = GetComponent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInParent<PlayerLocomotionController>();
                if (locomotionController == null)
                    locomotionController = GetComponentInChildren<PlayerLocomotionController>(true);
            }

            if (fullBodyActionController == null)
            {
                fullBodyActionController = GetComponent<PlayerFullBodyActionController>();
                if (fullBodyActionController == null)
                    fullBodyActionController = GetComponentInParent<PlayerFullBodyActionController>();
                if (fullBodyActionController == null)
                    fullBodyActionController = GetComponentInChildren<PlayerFullBodyActionController>(true);
            }

            if (inputBufferComponent == null)
            {
                inputBufferComponent = GetComponent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInParent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInChildren<InputRequestBufferComponent>(true);
            }

            if (tickDriver == null)
            {
                tickDriver = GetComponent<UnitySimulationTickDriver>();
                if (tickDriver == null)
                    tickDriver = GetComponentInParent<UnitySimulationTickDriver>();
                if (tickDriver == null)
                    tickDriver = GetComponentInChildren<UnitySimulationTickDriver>(true);
            }
        }
    }
}
