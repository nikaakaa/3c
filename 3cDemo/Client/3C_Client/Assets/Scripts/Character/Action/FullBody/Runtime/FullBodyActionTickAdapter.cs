using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonDiagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonAction
{
    [DisallowMultipleComponent]
    public sealed class FullBodyActionTickAdapter : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] PlayerFullBodyActionController fullBodyActionController;
        [SerializeField] UnityInputSystemRequestBufferAdapter requestBufferAdapter;
        [SerializeField] bool restoreAutoUpdateOnDisable = true;
        [SerializeField] bool restoreRequestBufferAdvanceOnDisable = true;

        bool registered;
        bool hadPreviousAutoUpdate;
        bool previousAutoUpdate;
        bool hadPreviousRequestBufferAdvance;
        bool previousRequestBufferAdvance;
        readonly FullBodyFramePipeline framePipeline = new FullBodyFramePipeline();
        FullBodyFrameContext frameContext;
        FullBodyFrameResult lastFrameResult;
        bool hasFrameContext;

        static readonly SimulationTickPhase[] RegisteredPhases =
        {
            SimulationTickPhase.ReadInput,
            SimulationTickPhase.UpdateInputBuffer,
            SimulationTickPhase.GameplayDecision,
            SimulationTickPhase.BuildMotion,
            SimulationTickPhase.ExecuteMotion,
            SimulationTickPhase.PresentationBridge,
            SimulationTickPhase.WriteSnapshotAndEvents
        };

        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => tickDriver = value; }
        public PlayerFullBodyActionController FullBodyActionController { get => fullBodyActionController; set => fullBodyActionController = value; }
        public UnityInputSystemRequestBufferAdapter RequestBufferAdapter { get => requestBufferAdapter; set => requestBufferAdapter = value; }
        public bool RestoreAutoUpdateOnDisable { get => restoreAutoUpdateOnDisable; set => restoreAutoUpdateOnDisable = value; }
        public bool RestoreRequestBufferAdvanceOnDisable { get => restoreRequestBufferAdvanceOnDisable; set => restoreRequestBufferAdvanceOnDisable = value; }
        public bool IsRegistered => registered;
        public FullBodyFrameResult LastFrameResult => lastFrameResult;

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
            if (tickDriver == null || fullBodyActionController == null)
                return false;

            if (HasConflictingLocomotionTickAdapter())
                return false;

            previousAutoUpdate = fullBodyActionController.AutoUpdate;
            hadPreviousAutoUpdate = true;
            fullBodyActionController.AutoUpdate = false;

            if (requestBufferAdapter != null)
            {
                previousRequestBufferAdvance = requestBufferAdapter.AdvanceStepOnUpdate;
                hadPreviousRequestBufferAdvance = true;
                requestBufferAdapter.AdvanceStepOnUpdate = false;
            }

            for (int i = 0; i < RegisteredPhases.Length; i++)
                tickDriver.Runner.Register(RegisteredPhases[i], this);

            registered = true;
            return true;
        }

        public void Unregister()
        {
            if (!registered)
                return;

            if (tickDriver != null)
            {
                for (int i = 0; i < RegisteredPhases.Length; i++)
                    tickDriver.Runner.Unregister(RegisteredPhases[i], this);
            }

            if (restoreAutoUpdateOnDisable && hadPreviousAutoUpdate && fullBodyActionController != null)
                fullBodyActionController.AutoUpdate = previousAutoUpdate;

            if (restoreRequestBufferAdvanceOnDisable && hadPreviousRequestBufferAdvance && requestBufferAdapter != null)
                requestBufferAdapter.AdvanceStepOnUpdate = previousRequestBufferAdvance;

            registered = false;
            hadPreviousAutoUpdate = false;
            hadPreviousRequestBufferAdvance = false;
            hasFrameContext = false;
        }

        public void Tick(SimulationTickPhase phase, in SimulationTickContext context)
        {
            ResolveReferences();
            if (phase == SimulationTickPhase.ReadInput)
            {
                if (fullBodyActionController == null ||
                    !fullBodyActionController.TryReadFrameInputFromSource(
                        context.FixedDeltaSecondsFloat,
                        context.TickValue,
                        out FullBodyFrameInput input))
                {
                    hasFrameContext = false;
                    return;
                }

                hasFrameContext = true;
                frameContext = framePipeline.BeginFrame(in input);
            }

            if (!hasFrameContext)
                return;

            if (phase == SimulationTickPhase.UpdateInputBuffer && requestBufferAdapter != null)
                requestBufferAdapter.Tick(context.TickValue);

            if (fullBodyActionController == null)
                return;

            framePipeline.RunPhase(fullBodyActionController, phase, ref frameContext, out lastFrameResult);
            if (phase == SimulationTickPhase.WriteSnapshotAndEvents ||
                frameContext.CurrentStep == FullBodyFramePipelineStep.Failed)
            {
                hasFrameContext = false;
            }
        }

        bool HasConflictingLocomotionTickAdapter()
        {
            if (fullBodyActionController == null || fullBodyActionController.LocomotionController == null)
                return false;

            LocomotionTickAdapter[] adapters = GetComponentsInParent<LocomotionTickAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
            {
                LocomotionTickAdapter adapter = adapters[i];
                if (IsActiveLocomotionDriver(adapter) &&
                    adapter.UsesLocomotionController(fullBodyActionController.LocomotionController))
                {
                    ReportDriverConflict(adapter);
                    return true;
                }
            }

            adapters = GetComponentsInChildren<LocomotionTickAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
            {
                LocomotionTickAdapter adapter = adapters[i];
                if (IsActiveLocomotionDriver(adapter) &&
                    adapter.UsesLocomotionController(fullBodyActionController.LocomotionController))
                {
                    ReportDriverConflict(adapter);
                    return true;
                }
            }

            return false;
        }

        void ReportDriverConflict(LocomotionTickAdapter adapter)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Error,
                "fullbody-driver-conflict",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"FullBodyActionTickAdapter and LocomotionTickAdapter both target {fullBodyActionController.LocomotionController.name}. Disable one gameplay driver. conflict={adapter.name}"));
        }

        static bool IsActiveLocomotionDriver(LocomotionTickAdapter adapter)
        {
            return adapter != null && (adapter.IsRegistered || adapter.isActiveAndEnabled);
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

            if (requestBufferAdapter == null)
            {
                requestBufferAdapter = GetComponent<UnityInputSystemRequestBufferAdapter>();
                if (requestBufferAdapter == null)
                    requestBufferAdapter = GetComponentInParent<UnityInputSystemRequestBufferAdapter>();
                if (requestBufferAdapter == null)
                    requestBufferAdapter = GetComponentInChildren<UnityInputSystemRequestBufferAdapter>(true);
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
