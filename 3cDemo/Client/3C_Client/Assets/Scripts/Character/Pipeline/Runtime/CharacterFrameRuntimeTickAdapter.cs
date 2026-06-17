using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonAction
{
    [DisallowMultipleComponent]
    public sealed class CharacterFrameRuntimeTickAdapter : MonoBehaviour, ISimulationTickPhaseHandler
    {
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField] CharacterFrameRuntimeController runtimeController;
        [SerializeField] UnityInputSystemRequestBufferAdapter requestBufferAdapter;
        [SerializeField] bool restoreAutoUpdateOnDisable = true;
        [SerializeField] bool restoreRequestBufferAdvanceOnDisable = true;

        bool registered;
        bool hadPreviousAutoUpdate;
        bool previousAutoUpdate;
        bool hadPreviousRequestBufferAdvance;
        bool previousRequestBufferAdvance;
        CharacterFrameContext frameContext;
        CharacterFrameResult lastFrameResult;
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
        public CharacterFrameRuntimeController RuntimeController { get => runtimeController; set => runtimeController = value; }
        public UnityInputSystemRequestBufferAdapter RequestBufferAdapter { get => requestBufferAdapter; set => requestBufferAdapter = value; }
        public bool RestoreAutoUpdateOnDisable { get => restoreAutoUpdateOnDisable; set => restoreAutoUpdateOnDisable = value; }
        public bool RestoreRequestBufferAdvanceOnDisable { get => restoreRequestBufferAdvanceOnDisable; set => restoreRequestBufferAdvanceOnDisable = value; }
        public bool IsRegistered => registered;
        public CharacterFrameResult LastFrameResult => lastFrameResult;

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

            previousAutoUpdate = runtimeController.AutoUpdate;
            hadPreviousAutoUpdate = true;
            runtimeController.AutoUpdate = false;

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

            if (restoreAutoUpdateOnDisable && hadPreviousAutoUpdate && runtimeController != null)
                runtimeController.AutoUpdate = previousAutoUpdate;

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
                if (runtimeController == null ||
                    !runtimeController.TryReadFrameInputFromSource(
                        context.FixedDeltaSecondsFloat,
                        context.TickValue,
                        out CharacterFrameInput input))
                {
                    hasFrameContext = false;
                    return;
                }

                hasFrameContext = true;
                frameContext = runtimeController.BeginFrame(in input);
            }

            if (!hasFrameContext)
                return;

            if (phase == SimulationTickPhase.UpdateInputBuffer && requestBufferAdapter != null)
                requestBufferAdapter.Tick(context.TickValue);

            if (runtimeController == null)
                return;

            runtimeController.RunPhase(phase, ref frameContext, out lastFrameResult);
            if (phase == SimulationTickPhase.WriteSnapshotAndEvents ||
                frameContext.CurrentStep == CharacterFramePipelineStep.Failed)
            {
                hasFrameContext = false;
            }
        }

        void ResolveReferences()
        {
            if (runtimeController == null)
            {
                runtimeController = GetComponent<CharacterFrameRuntimeController>();
                if (runtimeController == null)
                    runtimeController = GetComponentInParent<CharacterFrameRuntimeController>();
                if (runtimeController == null)
                    runtimeController = GetComponentInChildren<CharacterFrameRuntimeController>(true);
            }

            if (requestBufferAdapter == null && runtimeController != null)
                requestBufferAdapter = runtimeController.RequestBufferAdapter;

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
