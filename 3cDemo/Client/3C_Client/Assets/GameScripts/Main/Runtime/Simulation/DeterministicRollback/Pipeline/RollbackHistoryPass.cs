using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackHistoryPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;
        readonly RollbackRuntimeState m_State;

        public RollbackHistoryPassRuntimeFactory(
            SimulationPipelinePassFactoryDescriptor descriptor,
            RollbackRuntimeState state)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RollbackHistoryReadPorts(
                context.Products.BindAppendReader<FixedFinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            return new FixedStepPassRuntimeAdapter<RollbackHistoryReadPorts, RollbackHistoryWritePorts>(
                new RollbackHistoryPassRuntime(context.Pass.Descriptor, m_State),
                reads,
                RollbackHistoryWritePorts.Instance);
        }
    }

    public sealed class RollbackHistoryPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<RollbackHistoryReadPorts, RollbackHistoryWritePorts>,
        ISimulationPipelineStateParticipant
    {
        readonly RollbackRuntimeState m_State;

        public RollbackHistoryPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            RollbackRuntimeState state) : base(descriptor)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                RollbackPipelinePassIds.HistoryStateOwner,
                RollbackPipelinePassIds.HistoryStateSchema,
                3);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            RollbackHistoryReadPorts readPorts,
            RollbackHistoryWritePorts writePorts)
        {
            RequireExecution();
            m_State.RecordAppliedInput(context.Tick);
            m_State.RecordCompletedTick(context.Tick);
        }

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            RollbackRuntimeState.TransactionCheckpoint before = m_State.CaptureTransactionCheckpoint();
            return new SimulationPipelinePassStateCheckpoint(
                StateIdentity,
                () => m_State.RestoreTransactionCheckpoint(before));
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            byte[] payload = m_State.CaptureSimulationProjection();
            return new SimulationPipelinePassStateSnapshot(
                StateIdentity.PassId,
                StateIdentity.ImplementationVersion,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                StateIdentity.StateSchemaVersion,
                SimulationCanonicalPayloadHash.Compute(payload),
                payload);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(
            SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new RollbackPipelineStateRestoreTransaction(StateIdentity, snapshot, m_State);
        }
    }

    public sealed class RollbackHistoryReadPorts : ISimulationPipelineReadPortSet
    {
        public RollbackHistoryReadPorts(
            IReadOnlySimulationPipelineAppendPort<FixedFinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IReadOnlySimulationPipelineAppendPort<FixedFinalizedActorResult> Results { get; }
    }

    public sealed class RollbackHistoryWritePorts : ISimulationPipelineWritePortSet
    {
        public static readonly RollbackHistoryWritePorts Instance = new RollbackHistoryWritePorts();
        RollbackHistoryWritePorts() { }
    }

    sealed class RollbackPipelineStateRestoreTransaction : ISimulationPipelinePassRestoreTransaction
    {
        readonly SimulationPipelinePassStateSnapshot m_Snapshot;
        readonly RollbackRuntimeState m_State;
        readonly RollbackRuntimeState.TransactionCheckpoint m_Before;
        bool m_Applied;
        bool m_Completed;

        public RollbackPipelineStateRestoreTransaction(
            SimulationPipelineStateParticipantIdentity participant,
            SimulationPipelinePassStateSnapshot snapshot,
            RollbackRuntimeState state)
        {
            Participant = participant;
            m_Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            if (!snapshot.PassId.Equals(participant.PassId) ||
                !snapshot.ImplementationVersion.Equals(participant.ImplementationVersion) ||
                !string.Equals(snapshot.StateOwner, participant.StateOwner, StringComparison.Ordinal) ||
                !string.Equals(snapshot.StateSchemaId, participant.StateSchemaId, StringComparison.Ordinal) ||
                snapshot.StateSchemaVersion != participant.StateSchemaVersion)
            {
                throw new InvalidOperationException("Rollback Pipeline state snapshot identity does not match the active Pass.");
            }
            m_Before = state.CaptureTransactionCheckpoint();
        }

        public SimulationPipelineStateParticipantIdentity Participant { get; }

        public void Apply()
        {
            RequireOpen();
            if (m_Applied)
                throw new InvalidOperationException("Rollback Pipeline state restore is already applied.");
            m_State.RestoreSimulationProjection(m_Snapshot.CopyPayload());
            m_Applied = true;
        }

        public void ValidateApplied()
        {
            RequireOpen();
            if (!m_Applied || !SimulationCanonicalPayloadHash.Compute(m_State.CaptureSimulationProjection()).Equals(m_Snapshot.StateHash))
                throw new InvalidOperationException("Rollback Pipeline state restore hash does not match the requested snapshot.");
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (!m_Applied)
                throw new InvalidOperationException("Rollback Pipeline state restore is not applied.");
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed || !m_Applied)
                return;
            m_State.RestoreTransactionCheckpoint(m_Before);
            m_Applied = false;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
            m_Completed = true;
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(RollbackPipelineStateRestoreTransaction));
        }
    }
}
