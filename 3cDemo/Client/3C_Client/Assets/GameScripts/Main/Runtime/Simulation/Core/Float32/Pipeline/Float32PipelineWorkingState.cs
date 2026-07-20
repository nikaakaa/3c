using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class Float32PipelineWorkingState
    {
        SimulationWorldStateSet m_Current;

        public Float32PipelineWorkingState(SimulationWorldStateSet state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            m_Current = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationWorldStateSet Current => m_Current;
        public ulong LastCompletedTick => m_Current.LastCompletedTick;
        public IReadOnlyList<SimulationActorState> Actors => m_Current.Actors;
        public WorldSimulationState World => m_Current.WorldState;

        public void Replace(SimulationWorldStateSet state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            ReplaceCandidate(state);
        }

        void ReplaceCandidate(SimulationWorldStateSet candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));
            if (candidate.Actors.Count != m_Current.Actors.Count)
                throw new InvalidOperationException("Working state candidate changed the locked Actor roster.");
            for (int i = 0; i < candidate.Actors.Count; i++)
            {
                if (candidate.Actors[i].ActorId != m_Current.Actors[i].ActorId)
                    throw new InvalidOperationException("Working state candidate changed the locked Actor order.");
            }
            m_Current = candidate;
        }
    }

    public sealed class Float32CharacterRestoreTransaction : ISimulationSessionRestoreParticipantTransaction
    {
        readonly Float32PipelineWorkingState m_Working;
        readonly SimulationWorldStateSet m_Previous;
        readonly SimulationWorldStateSet m_Restored;
        bool m_Applied;
        bool m_Validated;
        bool m_Completed;

        public Float32CharacterRestoreTransaction(
            Float32PipelineWorkingState working,
            SimulationWorldStateSet restored,
            string identity)
        {
            m_Working = working ?? throw new ArgumentNullException(nameof(working));
            if (restored == null)
                throw new ArgumentNullException(nameof(restored));
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            m_Previous = working.Current;
            m_Restored = restored;
        }

        public SimulationSessionRestoreParticipantKind Kind => SimulationSessionRestoreParticipantKind.Character;
        public string Identity { get; }

        public void Apply()
        {
            RequireOpen();
            if (m_Applied)
                throw new InvalidOperationException("Character restore transaction is already applied.");
            m_Working.Replace(m_Restored);
            m_Applied = true;
            m_Validated = false;
        }

        public void ValidateApplied()
        {
            RequireOpen();
            if (!m_Applied || !ReferenceEquals(m_Working.Current, m_Restored))
                throw new InvalidOperationException("Character restore transaction is not fully applied.");
            m_Validated = true;
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (!m_Applied || !m_Validated)
                throw new InvalidOperationException("Character restore transaction was not applied and validated before Session publish.");
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed || !m_Applied)
                return;
            m_Working.Replace(m_Previous);
            m_Applied = false;
            m_Validated = false;
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
                throw new ObjectDisposedException(nameof(Float32CharacterRestoreTransaction));
        }
    }

    public sealed class Float32WorldRestoreTransaction : ISimulationSessionRestoreParticipantTransaction
    {
        readonly Float32PipelineWorkingState m_Working;
        readonly ICharacterWorldSolver m_Solver;
        readonly SimulationWorldStateSet m_Previous;
        readonly SimulationWorldStateSet m_Restored;
        readonly StableHash m_RestoreHash;
        bool m_Applied;
        bool m_Validated;
        bool m_Completed;

        public Float32WorldRestoreTransaction(
            Float32PipelineWorkingState working,
            ICharacterWorldSolver solver,
            SimulationWorldStateSet restored,
            string identity)
        {
            m_Working = working ?? throw new ArgumentNullException(nameof(working));
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            if (restored == null)
                throw new ArgumentNullException(nameof(restored));
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            m_Previous = working.Current;
            m_Restored = restored;
            m_RestoreHash = SimulationCanonicalPayloadHash.Compute(WorldSimulationStateCodec.Write(restored.WorldState));
        }

        public SimulationSessionRestoreParticipantKind Kind => SimulationSessionRestoreParticipantKind.World;
        public string Identity { get; }

        public void Apply()
        {
            RequireOpen();
            if (m_Applied)
                throw new InvalidOperationException("World restore transaction is already applied.");
            m_Solver.Restore(m_Restored.WorldState);
            m_Applied = true;
            m_Validated = false;
        }

        public void ValidateApplied()
        {
            RequireOpen();
            if (!m_Applied || !ReferenceEquals(m_Working.Current, m_Restored))
                throw new InvalidOperationException("World restore transaction is not fully applied.");
            WorldSimulationState captured = m_Solver.Capture(m_Restored.WorldState.WorldRevision);
            StableHash capturedHash = SimulationCanonicalPayloadHash.Compute(WorldSimulationStateCodec.Write(captured));
            if (!capturedHash.Equals(m_RestoreHash))
                throw new InvalidOperationException("World Solver state does not match the restored canonical World state.");
            m_Validated = true;
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (!m_Applied || !m_Validated)
                throw new InvalidOperationException("World restore transaction was not applied and validated before Session publish.");
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed || !m_Applied)
                return;
            m_Solver.Restore(m_Previous.WorldState);
            m_Applied = false;
            m_Validated = false;
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
                throw new ObjectDisposedException(nameof(Float32WorldRestoreTransaction));
        }
    }
}
