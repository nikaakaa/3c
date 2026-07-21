using System;
using Float32CommittedActorPoseSnapshot = ThirdPersonSimulation.CommittedActorPoseSnapshot<ThirdPersonSimulation.Float32Vector3, ThirdPersonSimulation.Float32Yaw>;

namespace ThirdPersonSimulation
{
    public sealed class LocalInputIngressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.LocalInputIngress);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new LocalInputIngressReadPorts(
                context.BindSourcePort<IFloat32LocalInputSourcePort>(Float32LocalInputSourcePortContract.PortId),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFloat32CommittedActorObservationReadPort>(Float32PipelineRuntimePortIds.CommittedObservation));
            var writes = new LocalInputIngressWritePorts(
                context.Products.BindExclusiveWriter<Float32CanonicalInputBatch>(SimulationPipelineProducts.CanonicalInputs),
                context.Products.BindExclusiveWriter<Float32TypedIngressBatch>(SimulationPipelineProducts.TypedIngress));
            return new Float32IngressPassRuntimeAdapter<LocalInputIngressReadPorts, LocalInputIngressWritePorts>(
                new LocalInputIngressPassRuntime(context.Pass.Descriptor, reads.Source),
                reads,
                writes);
        }
    }

    public sealed class LocalInputIngressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationIngressPassRuntime<LocalInputIngressReadPorts, LocalInputIngressWritePorts>,
        ISimulationPipelineStateParticipant
    {
        IFloat32LocalInputSourcePort m_Source;

        public LocalInputIngressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            IFloat32LocalInputSourcePort source)
            : base(descriptor)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                descriptor.StateOwner,
                StandardFloat32PipelinePassContracts.LocalControlInputStateSchemaId,
                StandardFloat32PipelinePassContracts.LocalControlInputStateSchemaVersion);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public void Execute(
            SimulationPipelineIngressContext context,
            LocalInputIngressReadPorts readPorts,
            LocalInputIngressWritePorts writePorts)
        {
            RequireExecution();
            if (!ReferenceEquals(m_Source, readPorts.Source))
                throw new InvalidOperationException("Local Control Input Ingress source port changed after activation.");
            SimulationProgramCatalog catalog = readPorts.ProgramRuntime.Catalog;
            var nextTick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            Float32CommittedActorPoseSnapshot observation = readPorts.CommittedObservation.Read();
            Float32LocalInputFrame frame = readPorts.Source.Read(
                context.Source,
                nextTick,
                catalog.NumericProfile,
                catalog.TickRate,
                readPorts.ProgramRuntime.Roster,
                observation);
            writePorts.CanonicalInputs.Write(frame.CanonicalInputs);
            writePorts.TypedIngress.Write(frame.TypedIngress);
        }

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            byte[] state = RequireSource().CaptureState();
            return new LocalControlInputCheckpoint(StateIdentity, RequireSource(), state);
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            byte[] payload = RequireSource().CaptureState();
            return new SimulationPipelinePassStateSnapshot(
                Descriptor.PassId,
                Descriptor.ImplementationVersion,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                StateIdentity.StateSchemaVersion,
                SimulationCanonicalPayloadHash.Compute(payload),
                payload);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new LocalControlInputRestoreTransaction(StateIdentity, snapshot, RequireSource());
        }

        IFloat32LocalInputSourcePort RequireSource() => m_Source;

    }

    sealed class LocalControlInputCheckpoint : ISimulationPipelinePassStateCheckpoint
    {
        IFloat32LocalInputSourcePort m_Source;
        byte[] m_State;
        bool m_Restored;

        public LocalControlInputCheckpoint(
            SimulationPipelineStateParticipantIdentity participant,
            IFloat32LocalInputSourcePort source,
            byte[] state)
        {
            Participant = participant;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationPipelineStateParticipantIdentity Participant { get; }

        public void Restore()
        {
            if (m_Source == null)
                throw new ObjectDisposedException(nameof(LocalControlInputCheckpoint));
            if (m_Restored)
                return;
            m_Source.RestoreState(m_State);
            m_Source.NotifyStateDisposition(CharacterControlSourceStateDisposition.Discarded);
            m_Restored = true;
        }

        public void Dispose()
        {
            if (m_Source == null)
                return;
            if (!m_Restored)
                m_Source.NotifyStateDisposition(CharacterControlSourceStateDisposition.Committed);
            m_Source = null;
            m_State = null;
        }
    }

    public sealed class LocalInputIngressReadPorts : ISimulationPipelineReadPortSet
    {
        public LocalInputIngressReadPorts(
            IFloat32LocalInputSourcePort source,
            IFloat32ProgramRuntimePort programRuntime,
            IFloat32CommittedActorObservationReadPort committedObservation)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            CommittedObservation = committedObservation ?? throw new ArgumentNullException(nameof(committedObservation));
        }

        public IFloat32LocalInputSourcePort Source { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
        public IFloat32CommittedActorObservationReadPort CommittedObservation { get; }
    }

    public sealed class LocalInputIngressWritePorts : ISimulationPipelineWritePortSet
    {
        public LocalInputIngressWritePorts(
            IExclusiveSimulationPipelineProductWriter<Float32CanonicalInputBatch> canonicalInputs,
            IExclusiveSimulationPipelineProductWriter<Float32TypedIngressBatch> typedIngress)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
        }

        public IExclusiveSimulationPipelineProductWriter<Float32CanonicalInputBatch> CanonicalInputs { get; }
        public IExclusiveSimulationPipelineProductWriter<Float32TypedIngressBatch> TypedIngress { get; }
    }

    sealed class LocalControlInputRestoreTransaction : ISimulationPipelinePassRestoreTransaction
    {
        readonly IFloat32LocalInputSourcePort m_Source;
        readonly byte[] m_Before;
        readonly byte[] m_Replacement;
        bool m_Applied;

        public LocalControlInputRestoreTransaction(
            SimulationPipelineStateParticipantIdentity participant,
            SimulationPipelinePassStateSnapshot snapshot,
            IFloat32LocalInputSourcePort source)
        {
            Participant = participant;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_Before = source.CaptureState();
            m_Replacement = snapshot?.CopyPayload() ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public SimulationPipelineStateParticipantIdentity Participant { get; }

        public void Apply()
        {
            m_Source.RestoreState(m_Replacement);
            m_Applied = true;
        }

        public void ValidateApplied()
        {
            if (!m_Applied || !SimulationCanonicalPayloadHash.Compute(m_Source.CaptureState()).Equals(
                    SimulationCanonicalPayloadHash.Compute(m_Replacement)))
            {
                throw new InvalidOperationException("Local Control Input state restore did not reproduce canonical bytes.");
            }
        }

        public void CompleteAfterSessionPublish()
        {
        }

        public void Rollback()
        {
            if (m_Applied)
                m_Source.RestoreState(m_Before);
            m_Applied = false;
        }

        public void Dispose()
        {
        }
    }
}
