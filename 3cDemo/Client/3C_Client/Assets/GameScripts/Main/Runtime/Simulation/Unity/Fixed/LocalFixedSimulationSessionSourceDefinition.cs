using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [CreateAssetMenu(fileName = "LocalFixedSimulationSessionSource", menuName = "3C/Simulation/Fixed/Local Session Source")]
    public sealed class LocalFixedSimulationSessionSourceDefinition : SimulationSessionSourceDefinition
    {
        public const string ComponentId = "thirdperson.simulation.session-source.fixed-local";
        public const string SemanticVersion = "1";

        [SerializeField, Min(1)] int m_MaximumPendingRequests = 64;

        public override SimulationSessionSourceAuthoringDescriptor BuildAuthoringDescriptor()
        {
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                ComponentId,
                SemanticVersion,
                StableHash.Compute(
                    "fixed-local-session-source-authoring/1",
                    ComponentId,
                    SemanticVersion,
                    RequireMaximumPendingRequests().ToString(),
                    StandardFixedLocalPipeline.PipelineId,
                    FixedLocalInputSourcePortContract.PortId));
            SimulationSessionSourceDescriptor source = CreateDescriptor(identity);
            var port = new SimulationPortDescriptor(
                FixedLocalInputSourcePortContract.PortId,
                FixedLocalInputSourcePortContract.SchemaId,
                FixedLocalInputSourcePortContract.SchemaVersion,
                SimulationPortDirection.Input,
                ComponentId,
                StableHash.Compute("fixed-local-session-source-authoring-port/1", identity.ToString()));
            return new SimulationSessionSourceAuthoringDescriptor(source, new[] { port });
        }

        internal static SimulationSessionSourceDescriptor CreateDescriptor(SimulationComponentIdentity identity)
        {
            return new SimulationSessionSourceDescriptor(
                identity,
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion,
                SimulationTickSourceKind.LocalLogic,
                SimulationPipelineExecutionSupport.Forward,
                true,
                FixedPassExecutionBackend.BackendId,
                new SimulationPipelineId(StandardFixedLocalPipeline.PipelineId),
                requiredSolverCapabilities: FixedLocalSimulationRuntimeLauncher.RequiredWorldCapabilities,
                requiredPipelinePasses: new[]
                {
                    new SimulationPipelinePassRequirement(
                        StandardFixedLocalPipelinePassContracts.LocalInputIngress.PassId,
                        StandardFixedLocalPipelinePassContracts.LocalInputIngress.ImplementationVersion,
                        SimulationPipelinePhase.Ingress),
                    new SimulationPipelinePassRequirement(
                        StandardFixedLocalPipelinePassContracts.LocalSingleStepSchedule.PassId,
                        StandardFixedLocalPipelinePassContracts.LocalSingleStepSchedule.ImplementationVersion,
                        SimulationPipelinePhase.Schedule),
                    new SimulationPipelinePassRequirement(
                        StandardFixedLocalPipelinePassContracts.LocalImmediateOutput.PassId,
                        StandardFixedLocalPipelinePassContracts.LocalImmediateOutput.ImplementationVersion,
                        SimulationPipelinePhase.Egress)
                },
                requiredPipelineSourcePorts: new[] { FixedLocalInputSourcePortContract.Requirement });
        }

        protected override ISimulationSessionSourcePreparation CreatePreparationCore(
            SimulationSessionSourcePreparationContext context)
        {
            return new LocalFixedSimulationSessionSourcePreparation(
                context,
                RequireMaximumPendingRequests());
        }

        int RequireMaximumPendingRequests()
        {
            return m_MaximumPendingRequests > 0
                ? m_MaximumPendingRequests
                : throw new InvalidOperationException($"Fixed Local Session Source '{name}' requires positive pending request capacity.");
        }
    }

    sealed class LocalFixedSimulationSessionSourcePreparation : ISimulationSessionSourcePreparation
    {
        readonly SimulationSessionSourcePreparationContext m_Context;
        readonly SimulationSessionSourceDescriptor m_Descriptor;
        LocalFixedSimulationSessionPreparedSource m_PreparedSource;
        bool m_Disposed;
        bool m_Stepped;

        public LocalFixedSimulationSessionSourcePreparation(
            SimulationSessionSourcePreparationContext context,
            int maximumPendingRequests)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!context.ProgramRuntime.NumericProfileId.Equals(FixedSimulationNumericProfile.Value.Id) ||
                !context.ProgramRuntime.TargetAbiVersion.Equals(FixedSimulationNumericProfile.Value.AbiVersion) ||
                context.ExecutionBackend is not FixedPassExecutionBackendDefinition)
            {
                throw new InvalidOperationException("Fixed Local Session Source requires the installed Fixed Program Runtime, Backend, and Standard Local Pipeline.");
            }
            var inputBindings = new FixedLocalSimulationInputBinding[context.Registrations.Count];
            var identityParts = new string[context.Registrations.Count + 5];
            identityParts[0] = LocalFixedSimulationSessionSourceDefinition.ComponentId;
            identityParts[1] = LocalFixedSimulationSessionSourceDefinition.SemanticVersion;
            identityParts[2] = context.SourceClockId.Value;
            identityParts[3] = context.TickRate.ToString();
            identityParts[4] = maximumPendingRequests.ToString();
            for (int i = 0; i < context.Registrations.Count; i++)
            {
                if (context.Registrations[i] is not IFixedLocalSimulationActorRegistration registration ||
                    registration.FixedControlSource == null)
                {
                    throw new InvalidOperationException($"Fixed Local Actor '{context.Registrations[i]?.ActorId}' has no formal Fixed Control Source.");
                }
                IFixedCharacterControlSourceRuntime controlSource = registration.FixedControlSource;
                if (!controlSource.CharacterProgramId.Equals(registration.Program.Manifest.ProgramId) ||
                    !controlSource.CharacterProgramHash.Equals(registration.Program.ProgramHash))
                {
                    throw new InvalidOperationException($"Fixed Local Actor '{registration.ActorId}' Control Source does not match its Fixed Program.");
                }
                inputBindings[i] = new FixedLocalSimulationInputBinding(registration.ActorId, controlSource);
                identityParts[i + 5] = $"{registration.ActorId}:{controlSource.SourceIdentity}:{registration.Program.ProgramHash}";
            }
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                LocalFixedSimulationSessionSourceDefinition.ComponentId,
                LocalFixedSimulationSessionSourceDefinition.SemanticVersion,
                StableHash.Compute(identityParts));
            m_Descriptor = LocalFixedSimulationSessionSourceDefinition.CreateDescriptor(identity);
            var inputPort = new FixedLocalInputSourcePort(identity, inputBindings, 0, maximumPendingRequests);
            m_PreparedSource = new LocalFixedSimulationSessionPreparedSource(
                m_Descriptor,
                new SimulationRuntimePortSet(new ISimulationRuntimePort[] { inputPort }));
        }

        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionSourceDescriptor Descriptor => m_Descriptor;

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(LocalFixedSimulationSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Pending)
                return Status;
            if (m_Stepped || context.Source.Kind != SimulationTickSourceKind.LocalLogic ||
                !string.Equals(context.Source.ClockId, m_Context.SourceClockId.Value, StringComparison.Ordinal))
            {
                Failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Preparation,
                    "fixed_local_source_clock_mismatch",
                    "Fixed Local Session Source preparation requires its configured LocalLogic source clock.",
                    Descriptor.Identity.ToString());
                Status = SimulationSessionPreparationStatus.Failed;
                return Status;
            }
            m_Stepped = true;
            Status = SimulationSessionPreparationStatus.Ready;
            return Status;
        }

        public ISimulationSessionPreparedSource TakePreparedSource()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(LocalFixedSimulationSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_PreparedSource == null)
                throw new InvalidOperationException("Fixed Local prepared Source is not available.");
            LocalFixedSimulationSessionPreparedSource result = m_PreparedSource;
            m_PreparedSource = null;
            return result;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_PreparedSource?.Dispose();
            m_PreparedSource = null;
        }
    }

    sealed class LocalFixedSimulationSessionPreparedSource : IFixedSimulationPreparedSource
    {
        bool m_Disposed;
        bool m_Bound;

        public LocalFixedSimulationSessionPreparedSource(
            SimulationSessionSourceDescriptor descriptor,
            SimulationRuntimePortSet runtimePorts)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            RuntimePorts = runtimePorts ?? throw new ArgumentNullException(nameof(runtimePorts));
        }

        public SimulationSessionSourceDescriptor Descriptor { get; }
        public SimulationRuntimePortSet RuntimePorts { get; }
        public int MaximumBodySamplesPerActor => 1;

        public FixedSimulationSourceRuntimeBinding BindRuntime(FixedSimulationSourceRuntimeBindingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(LocalFixedSimulationSessionPreparedSource));
            if (m_Bound)
                throw new InvalidOperationException("Fixed Local prepared Source Runtime is already bound.");
            if (request.PipelineDefinition is not StandardFixedLocalSimulationPipelineDefinition pipeline)
                throw new InvalidOperationException("Fixed Local prepared Source requires the Standard Fixed Local Pipeline Definition.");
            m_Bound = true;
            return new FixedSimulationSourceRuntimeBinding(
                new FixedLocalSimulationRuntimeLauncher(Descriptor),
                pipeline.BuildRuntimePackage(),
                null,
                new FixedImmediateOutputCommitter(request.CommitterIdentity, request.Output),
                ThirdPersonSimulation.Fixed.SimulationPipelineInitialStateSource.CaptureActivatedDefaults);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
        }
    }
}
