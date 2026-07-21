using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedLocalInputIngressPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedLocalPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedLocalPipelinePassContracts.LocalInputIngress);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedLocalInputIngressReadPorts(
                context.BindSourcePort<IFixedLocalInputSourcePort>(FixedLocalInputSourcePortContract.PortId),
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFixedCommittedActorObservationReadPort>(FixedPipelineRuntimePortIds.CommittedObservation));
            var writes = new FixedLocalInputIngressWritePorts(
                context.Products.BindExclusiveWriter<FixedCanonicalInputBatch>(SimulationPipelineProducts.CanonicalInputs),
                context.Products.BindExclusiveWriter<FixedTypedIngressBatch>(SimulationPipelineProducts.TypedIngress));
            return new FixedIngressPassRuntimeAdapter<FixedLocalInputIngressReadPorts, FixedLocalInputIngressWritePorts>(
                new FixedLocalInputIngressPassRuntime(context.Pass.Descriptor, reads.Source),
                reads,
                writes);
        }
    }

    public sealed class FixedLocalInputIngressPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationIngressPassRuntime<FixedLocalInputIngressReadPorts, FixedLocalInputIngressWritePorts>,
        ISimulationPipelineStateParticipant
    {
        readonly IFixedLocalInputSourcePort m_Source;

        public FixedLocalInputIngressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            IFixedLocalInputSourcePort source)
            : base(descriptor)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                descriptor.StateOwner,
                StandardFixedLocalPipelinePassContracts.LocalControlInputStateSchemaId,
                StandardFixedLocalPipelinePassContracts.LocalControlInputStateSchemaVersion);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public void Execute(
            SimulationPipelineIngressContext context,
            FixedLocalInputIngressReadPorts readPorts,
            FixedLocalInputIngressWritePorts writePorts)
        {
            RequireExecution();
            if (!ReferenceEquals(m_Source, readPorts.Source))
                throw new InvalidOperationException("Fixed Local input Source port changed after activation.");
            SimulationProgramCatalog catalog = readPorts.ProgramRuntime.Catalog;
            var nextTick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            FixedLocalInputFrame frame = readPorts.Source.Read(
                context.Source,
                nextTick,
                catalog.TickRate,
                readPorts.ProgramRuntime.Roster,
                readPorts.CommittedObservation.Read());
            writePorts.CanonicalInputs.Write(frame.CanonicalInputs);
            writePorts.TypedIngress.Write(frame.TypedIngress);
        }

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            return new FixedLocalControlInputCheckpoint(StateIdentity, m_Source, m_Source.CaptureState());
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            byte[] payload = m_Source.CaptureState();
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
            return new FixedLocalControlInputRestoreTransaction(StateIdentity, snapshot, m_Source);
        }
    }

    public sealed class FixedLocalInputIngressReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedLocalInputIngressReadPorts(
            IFixedLocalInputSourcePort source,
            IFixedProgramRuntimePort programRuntime,
            IFixedCommittedActorObservationReadPort committedObservation)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            CommittedObservation = committedObservation ?? throw new ArgumentNullException(nameof(committedObservation));
        }

        public IFixedLocalInputSourcePort Source { get; }
        public IFixedProgramRuntimePort ProgramRuntime { get; }
        public IFixedCommittedActorObservationReadPort CommittedObservation { get; }
    }

    public sealed class FixedLocalInputIngressWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedLocalInputIngressWritePorts(
            IExclusiveSimulationPipelineProductWriter<FixedCanonicalInputBatch> canonicalInputs,
            IExclusiveSimulationPipelineProductWriter<FixedTypedIngressBatch> typedIngress)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
        }

        public IExclusiveSimulationPipelineProductWriter<FixedCanonicalInputBatch> CanonicalInputs { get; }
        public IExclusiveSimulationPipelineProductWriter<FixedTypedIngressBatch> TypedIngress { get; }
    }

    sealed class FixedLocalControlInputCheckpoint : ISimulationPipelinePassStateCheckpoint
    {
        IFixedLocalInputSourcePort m_Source;
        byte[] m_State;
        bool m_Restored;

        public FixedLocalControlInputCheckpoint(
            SimulationPipelineStateParticipantIdentity participant,
            IFixedLocalInputSourcePort source,
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
                throw new ObjectDisposedException(nameof(FixedLocalControlInputCheckpoint));
            if (m_Restored)
                return;
            m_Source.RestoreState(m_State);
            m_Source.NotifyStateDisposition(FixedCharacterControlSourceStateDisposition.Discarded);
            m_Restored = true;
        }

        public void Dispose()
        {
            if (m_Source == null)
                return;
            if (!m_Restored)
                m_Source.NotifyStateDisposition(FixedCharacterControlSourceStateDisposition.Committed);
            m_Source = null;
            m_State = null;
        }
    }

    sealed class FixedLocalControlInputRestoreTransaction : ISimulationPipelinePassRestoreTransaction
    {
        readonly IFixedLocalInputSourcePort m_Source;
        readonly byte[] m_Before;
        readonly byte[] m_Replacement;
        bool m_Applied;

        public FixedLocalControlInputRestoreTransaction(
            SimulationPipelineStateParticipantIdentity participant,
            SimulationPipelinePassStateSnapshot snapshot,
            IFixedLocalInputSourcePort source)
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
            m_Source.NotifyStateDisposition(FixedCharacterControlSourceStateDisposition.Restored);
            m_Applied = true;
        }

        public void ValidateApplied()
        {
            if (!m_Applied || !SimulationCanonicalPayloadHash.Compute(m_Source.CaptureState()).Equals(
                    SimulationCanonicalPayloadHash.Compute(m_Replacement)))
            {
                throw new InvalidOperationException("Fixed Local Control Source state restore did not reproduce canonical bytes.");
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
