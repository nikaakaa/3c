using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePassStateRestoreTransaction : ISimulationPipelinePassRestoreTransaction
    {
        readonly Action<byte[]> m_Restore;
        readonly byte[] m_Previous;
        readonly byte[] m_Next;
        readonly Func<byte[]> m_Capture;
        bool m_Applied;
        bool m_Validated;
        bool m_Completed;

        public ServerAuthoritativePassStateRestoreTransaction(
            SimulationPipelineStateParticipantIdentity participant,
            SimulationPipelinePassStateSnapshot snapshot,
            Func<byte[]> capture,
            Action<byte[]> restore)
        {
            Participant = participant;
            if (snapshot == null || !snapshot.PassId.Equals(participant.PassId) ||
                !snapshot.ImplementationVersion.Equals(participant.ImplementationVersion) ||
                !string.Equals(snapshot.StateOwner, participant.StateOwner, StringComparison.Ordinal) ||
                !string.Equals(snapshot.StateSchemaId, participant.StateSchemaId, StringComparison.Ordinal) ||
                snapshot.StateSchemaVersion != participant.StateSchemaVersion)
            {
                throw new InvalidOperationException("ServerAuthoritative Pass state snapshot identity does not match its participant.");
            }
            m_Capture = capture ?? throw new ArgumentNullException(nameof(capture));
            m_Restore = restore ?? throw new ArgumentNullException(nameof(restore));
            m_Previous = m_Capture();
            m_Next = snapshot.CopyPayload();
        }

        public SimulationPipelineStateParticipantIdentity Participant { get; }

        public void Apply()
        {
            RequireOpen();
            if (m_Applied)
                throw new InvalidOperationException("Pass state restore is already applied.");
            m_Restore(m_Next);
            m_Applied = true;
            m_Validated = false;
        }

        public void ValidateApplied()
        {
            RequireOpen();
            if (!m_Applied || SimulationCanonicalPayloadHash.Compute(m_Capture()) != SimulationCanonicalPayloadHash.Compute(m_Next))
                throw new InvalidOperationException("Pass state restore did not apply the canonical payload.");
            m_Validated = true;
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (!m_Applied || !m_Validated)
                throw new InvalidOperationException("Pass state restore was not applied and validated before Session publish.");
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed || !m_Applied)
                return;
            m_Restore(m_Previous);
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
                throw new ObjectDisposedException(nameof(ServerAuthoritativePassStateRestoreTransaction));
        }
    }
}
