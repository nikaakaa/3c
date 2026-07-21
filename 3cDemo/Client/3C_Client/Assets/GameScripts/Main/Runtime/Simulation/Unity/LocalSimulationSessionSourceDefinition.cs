using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "LocalSimulationSessionSource", menuName = "3C/Simulation/Local Session Source")]
    public sealed class LocalSimulationSessionSourceDefinition : SimulationSessionSourceDefinition
    {
        public const string ComponentId = "thirdperson.simulation.session-source.local";
        public const string SemanticVersion = "1";

        public override SimulationSessionSourceAuthoringDescriptor BuildAuthoringDescriptor()
        {
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                ComponentId,
                SemanticVersion,
                StableHash.Compute(
                    "local-session-source-authoring/1",
                    ComponentId,
                    SemanticVersion,
                    StandardLocalSimulationPipelineDefinition.StandardPipelineId,
                    Float32LocalInputSourcePortContract.PortId));
            SimulationSessionSourceDescriptor source = CreateDescriptor(
                identity,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion);
            var port = new SimulationPortDescriptor(
                Float32LocalInputSourcePortContract.PortId,
                Float32LocalInputSourcePortContract.SchemaId,
                Float32LocalInputSourcePortContract.SchemaVersion,
                SimulationPortDirection.Input,
                ComponentId,
                StableHash.Compute("local-session-source-authoring-port/1", identity.ToString()));
            return new SimulationSessionSourceAuthoringDescriptor(source, new[] { port });
        }

        internal static SimulationSessionSourceDescriptor CreateDescriptor(
            SimulationComponentIdentity identity,
            NumericProfileId numericProfileId,
            TargetAbiVersion targetAbiVersion)
        {
            return new SimulationSessionSourceDescriptor(
                identity,
                numericProfileId,
                targetAbiVersion,
                SimulationTickSourceKind.LocalLogic,
                SimulationPipelineExecutionSupport.Forward,
                false,
                Float32PassExecutionBackend.BackendId,
                new SimulationPipelineId(StandardLocalSimulationPipelineDefinition.StandardPipelineId),
                requiredPipelinePasses: new[]
                {
                    new SimulationPipelinePassRequirement(
                        StandardFloat32PipelinePassContracts.LocalInputIngress.PassId,
                        StandardFloat32PipelinePassContracts.LocalInputIngress.ImplementationVersion,
                        SimulationPipelinePhase.Ingress),
                    new SimulationPipelinePassRequirement(
                        StandardFloat32PipelinePassContracts.LocalSingleStepSchedule.PassId,
                        StandardFloat32PipelinePassContracts.LocalSingleStepSchedule.ImplementationVersion,
                        SimulationPipelinePhase.Schedule),
                    new SimulationPipelinePassRequirement(
                        StandardFloat32PipelinePassContracts.LocalImmediateOutput.PassId,
                        StandardFloat32PipelinePassContracts.LocalImmediateOutput.ImplementationVersion,
                        SimulationPipelinePhase.Egress)
                },
                requiredPipelineSourcePorts: new[] { Float32LocalInputSourcePortContract.Requirement });
        }

        protected override ISimulationSessionSourcePreparation CreatePreparationCore(
            SimulationSessionSourcePreparationContext context)
        {
            return new LocalSimulationSessionSourcePreparation(context);
        }
    }

    internal sealed class LocalSimulationSessionSourcePreparation : ISimulationSessionSourcePreparation
    {
        readonly SimulationSessionSourcePreparationContext m_Context;
        readonly SimulationSessionSourceDescriptor m_Descriptor;
        LocalSimulationSessionPreparedSource m_PreparedSource;
        bool m_Disposed;
        bool m_Stepped;

        public LocalSimulationSessionSourcePreparation(SimulationSessionSourcePreparationContext context)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!context.ProgramRuntime.NumericProfileId.Equals(Float32SimulationNumericProfile.Value.Id) ||
                !context.ProgramRuntime.TargetAbiVersion.Equals(Float32SimulationNumericProfile.Value.AbiVersion))
            {
                throw new InvalidOperationException("Local Session Source requires the installed Float32 Program Runtime.");
            }
            var inputBindings = new LocalSimulationInputBinding[context.Registrations.Count];
            var identityParts = new string[context.Registrations.Count + 4];
            identityParts[0] = LocalSimulationSessionSourceDefinition.ComponentId;
            identityParts[1] = LocalSimulationSessionSourceDefinition.SemanticVersion;
            identityParts[2] = context.SourceClockId.Value;
            identityParts[3] = context.TickRate.ToString();
            var roster = new ActorId[context.Registrations.Count];
            for (int i = 0; i < context.Registrations.Count; i++)
            {
                ISimulationActorRegistration registration = context.Registrations[i] ??
                    throw new ArgumentException("Local Session Source roster contains a missing registration.", nameof(context));
                roster[i] = registration.ActorId;
            }
            for (int i = 0; i < context.Registrations.Count; i++)
            {
                ISimulationActorRegistration registration = context.Registrations[i] ??
                    throw new ArgumentException("Local Session Source roster contains a missing registration.", nameof(context));
                if (registration is not ILocalSimulationActorRegistration local || local.LocalControlSource == null)
                    throw new InvalidOperationException($"Local Actor '{registration.ActorId}' has no Character Control Source.");
                ICharacterControlSourceRuntime controlSource = local.LocalControlSource;
                if (controlSource.NumericProfile != Float32SimulationNumericProfile.Value ||
                    !controlSource.CharacterProgramId.Equals(local.Program.Manifest.ProgramId) ||
                    !controlSource.CharacterProgramHash.Equals(local.Program.ProgramHash))
                {
                    throw new InvalidOperationException($"Local Actor '{registration.ActorId}' Control Source does not match its Character Program.");
                }
                const CharacterControlSourceCapability knownCapabilities =
                    CharacterControlSourceCapability.CommittedObservation |
                    CharacterControlSourceCapability.TransactionalState;
                if ((controlSource.Capabilities & ~knownCapabilities) != 0)
                    throw new InvalidOperationException($"Local Actor '{registration.ActorId}' Control Source declares unknown capabilities '{controlSource.Capabilities & ~knownCapabilities}'.");
                if (controlSource.Capabilities.HasFlag(CharacterControlSourceCapability.CommittedObservation))
                {
                    if (controlSource is not ICharacterControlSourceRosterRuntime rosterRuntime)
                        throw new InvalidOperationException($"Local Actor '{registration.ActorId}' Control Source requires committed observation without a roster contract.");
                    rosterRuntime.ValidateRoster(
                        registration.ActorId,
                        roster,
                        CommittedActorObservationSchema.CapabilityHash);
                }
                inputBindings[i] = new LocalSimulationInputBinding(registration.ActorId, controlSource);
                string stateIdentity = controlSource is ICharacterControlSourceStateRuntime stateful
                    ? $"{stateful.StateSchemaId}:{stateful.StateSchemaVersion}"
                    : "stateless";
                identityParts[i + 4] = $"{registration.ActorId}:{controlSource.SourceIdentity}:{controlSource.Capabilities}:{stateIdentity}:{local.Program.ProgramHash}";
            }
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                LocalSimulationSessionSourceDefinition.ComponentId,
                LocalSimulationSessionSourceDefinition.SemanticVersion,
                StableHash.Compute(identityParts));
            m_Descriptor = LocalSimulationSessionSourceDefinition.CreateDescriptor(
                identity,
                context.ProgramRuntime.NumericProfileId,
                context.ProgramRuntime.TargetAbiVersion);
            var inputPort = new Float32LocalInputSourcePort(identity, inputBindings);
            m_PreparedSource = new LocalSimulationSessionPreparedSource(
                m_Descriptor,
                new SimulationRuntimePortSet(new ISimulationRuntimePort[] { inputPort }));
        }

        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionSourceDescriptor Descriptor => m_Descriptor;

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(LocalSimulationSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Pending)
                return Status;
            if (m_Stepped || context.Source.Kind != SimulationTickSourceKind.LocalLogic ||
                !string.Equals(context.Source.ClockId, m_Context.SourceClockId.Value, StringComparison.Ordinal))
            {
                Failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Preparation,
                    "local_source_clock_mismatch",
                    "Local Session Source preparation requires its configured LocalLogic source clock.",
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
                throw new ObjectDisposedException(nameof(LocalSimulationSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_PreparedSource == null)
                throw new InvalidOperationException("Local prepared Source is not available.");
            LocalSimulationSessionPreparedSource source = m_PreparedSource;
            m_PreparedSource = null;
            return source;
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

    internal sealed class LocalSimulationSessionPreparedSource : IFloat32SimulationSessionPreparedSource
    {
        bool m_Disposed;

        public LocalSimulationSessionPreparedSource(
            SimulationSessionSourceDescriptor descriptor,
            SimulationRuntimePortSet runtimePorts)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            RuntimePorts = runtimePorts ?? throw new ArgumentNullException(nameof(runtimePorts));
            RuntimeLauncher = new Float32StandardSessionRuntimeLauncher(Descriptor);
        }

        public SimulationSessionSourceDescriptor Descriptor { get; }
        public SimulationRuntimePortSet RuntimePorts { get; }
        public IFloat32SimulationSessionRuntimeLauncher RuntimeLauncher { get; }
        public IFloat32SimulationRestoreSource RestoreSource => null;
        public IFloat32SourceEgressOutputPort SourceEgress => NullFloat32SourceEgressOutputPort.Instance;

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
        }
    }
}
