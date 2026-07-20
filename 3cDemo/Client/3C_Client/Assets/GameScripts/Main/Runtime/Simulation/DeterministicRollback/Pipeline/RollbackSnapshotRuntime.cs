using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackSnapshotRestoreSource : IFixedSimulationRestoreSource
    {
        RollbackRuntimeState m_State;

        public RollbackSnapshotRestoreSource(SimulationComponentIdentity sourceIdentity)
        {
            if (!sourceIdentity.IsValid || sourceIdentity.Role != SimulationComponentRole.SessionSource)
                throw new ArgumentException("Rollback restore Source identity is invalid.", nameof(sourceIdentity));
            Descriptor = CreateDescriptor(sourceIdentity);
        }

        public SimulationPortDescriptor Descriptor { get; }

        public static SimulationPortDescriptor CreateDescriptor(SimulationComponentIdentity sourceIdentity)
        {
            if (!sourceIdentity.IsValid || sourceIdentity.Role != SimulationComponentRole.SessionSource)
                throw new ArgumentException("Rollback restore Source identity is invalid.", nameof(sourceIdentity));
            return new SimulationPortDescriptor(
                "deterministic-rollback.source.restore",
                "deterministic-rollback-session-snapshot-source",
                2,
                SimulationPortDirection.Input,
                sourceIdentity.ComponentId,
                StableHash.Compute("deterministic-rollback-restore-source/2", sourceIdentity.ConfigurationHash.Value));
        }

        public void Bind(RollbackRuntimeState state)
        {
            if (m_State != null)
                throw new InvalidOperationException("Rollback restore Source is already bound.");
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public FixedSimulationSessionSnapshot GetRequiredSnapshot(SimulationRestoreDirective directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));
            RollbackRuntimeState state = m_State ??
                throw new InvalidOperationException("Rollback restore Source is not bound to its runtime state.");
            FixedSimulationSessionSnapshot snapshot = state.Snapshots.GetRequired(directive.Tick);
            if (!snapshot.SnapshotHash.Equals(directive.SnapshotHash))
                throw new InvalidOperationException($"Rollback snapshot Tick '{directive.Tick}' does not match the restore directive hash.");
            return snapshot;
        }
    }

    public sealed class RollbackHistoryCommitter : IFixedSimulationCommitter
    {
        readonly IFixedSimulationCommitter m_Downstream;
        readonly RollbackRuntimeState m_State;
        readonly int m_HistoryLength;

        public RollbackHistoryCommitter(
            IFixedSimulationCommitter downstream,
            RollbackRuntimeState state,
            int historyLength)
        {
            m_Downstream = downstream ?? throw new ArgumentNullException(nameof(downstream));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            if (historyLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(historyLength));
            m_HistoryLength = historyLength;
            Identity = new SimulationComponentIdentity(
                SimulationComponentRole.Committer,
                "thirdperson.simulation.committer.deterministic-rollback",
                "2",
                StableHash.Compute(
                    "deterministic-rollback-committer/2",
                    downstream.Identity.ToString(),
                    historyLength.ToString()));
        }

        public SimulationComponentIdentity Identity { get; }

        public void Commit(FixedSimulationCommitBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            m_Downstream.Commit(batch);
            for (int i = 0; i < batch.Steps.Count; i++)
            {
                FixedSimulationStepSnapshot step = batch.Steps[i].StepSnapshot ??
                    throw new InvalidOperationException("Rollback commit requires a canonical Step snapshot.");
                var snapshot = new FixedSimulationSessionSnapshot(
                    step.CompositionIdentity,
                    step.World,
                    step.PipelineProjection);
                ulong floor = snapshot.Tick.Value >= (ulong)m_HistoryLength
                    ? snapshot.Tick.Value - (ulong)m_HistoryLength + 1
                    : 1;
                m_State.Snapshots.DiscardBefore(floor);
                bool replaceExisting = batch.Steps[i].Step.ExecutionKind ==
                                       SimulationPipelineStepExecutionKind.Replay;
                m_State.CaptureCommittedSnapshot(snapshot, replaceExisting);
            }
        }
    }
}
