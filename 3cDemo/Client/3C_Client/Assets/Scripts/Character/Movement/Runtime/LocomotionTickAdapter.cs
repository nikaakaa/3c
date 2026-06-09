using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class LocomotionTickAdapter : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField] bool restoreAutoUpdateOnDisable = true;

        bool registered;
        bool hadPreviousAutoUpdate;
        bool previousAutoUpdate;

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public bool RestoreAutoUpdateOnDisable { get => restoreAutoUpdateOnDisable; set => restoreAutoUpdateOnDisable = value; }
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

            previousAutoUpdate = locomotionController.AutoUpdate;
            hadPreviousAutoUpdate = true;
            locomotionController.AutoUpdate = false;
            tickDriver.Runner.Register(SimulationTickPhase.ExecuteMotion, this);
            registered = true;
            return true;
        }

        public void Unregister()
        {
            if (!registered)
                return;

            if (tickDriver != null)
                tickDriver.Runner.Unregister(SimulationTickPhase.ExecuteMotion, this);

            if (restoreAutoUpdateOnDisable && hadPreviousAutoUpdate && locomotionController != null)
                locomotionController.AutoUpdate = previousAutoUpdate;

            registered = false;
            hadPreviousAutoUpdate = false;
        }

        public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
        {
            if (phase != SimulationTickPhase.ExecuteMotion)
                return;

            if (locomotionController == null)
                ResolveReferences();

            if (locomotionController != null)
                locomotionController.TickFromInputSource(context.FixedDeltaSecondsFloat);
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
