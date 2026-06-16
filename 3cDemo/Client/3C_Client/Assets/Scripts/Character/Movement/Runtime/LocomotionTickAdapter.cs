using ThirdPersonAction;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class LocomotionTickAdapter : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] PlayerLocomotionController locomotionController;
        bool loggedRetiredDriver;

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public bool IsRegistered => false;

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
            ResolveReferences();
            HasConflictingFullBodyTickAdapter();
            ReportRetiredDriver();
            return false;
        }

        public void Unregister()
        {
        }

        public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
        {
            if (phase != SimulationTickPhase.ExecuteMotion)
                return;

            ReportRetiredDriver();
        }

        public bool UsesLocomotionController(PlayerLocomotionController controller)
        {
            if (controller == null)
                return false;

            if (locomotionController == null)
                ResolveReferences();

            return locomotionController == controller;
        }

        bool HasConflictingFullBodyTickAdapter()
        {
            if (locomotionController == null)
                return false;

            FullBodyActionTickAdapter[] adapters = GetComponentsInParent<FullBodyActionTickAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
            {
                if (IsActiveFullBodyDriver(adapters[i]) &&
                    adapters[i].FullBodyActionController != null &&
                    adapters[i].FullBodyActionController.LocomotionController == locomotionController)
                {
                    ReportDriverConflict(adapters[i]);
                    return true;
                }
            }

            adapters = GetComponentsInChildren<FullBodyActionTickAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
            {
                if (IsActiveFullBodyDriver(adapters[i]) &&
                    adapters[i].FullBodyActionController != null &&
                    adapters[i].FullBodyActionController.LocomotionController == locomotionController)
                {
                    ReportDriverConflict(adapters[i]);
                    return true;
                }
            }

            return false;
        }

        void ReportDriverConflict(FullBodyActionTickAdapter adapter)
        {
            string targetName = locomotionController != null ? locomotionController.name : name;
            LocomotionDiagnostics.SubmitDriverConflict(targetName, adapter != null ? adapter.name : string.Empty);
        }

        void ReportRetiredDriver()
        {
            if (loggedRetiredDriver)
                return;

            loggedRetiredDriver = true;
            string targetName = locomotionController != null ? locomotionController.name : name;
            LocomotionDiagnostics.SubmitRetiredTickAdapter(targetName);
        }

        static bool IsActiveFullBodyDriver(FullBodyActionTickAdapter adapter)
        {
            return adapter != null && (adapter.IsRegistered || adapter.isActiveAndEnabled);
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
