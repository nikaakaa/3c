using ThirdPersonSimulation.DeterministicRollback;

namespace ThirdPerson.Development.Gm.Rollback;

public sealed class RollbackRelayQuerySource
{
    readonly DeterministicRollbackServerManifest m_Manifest;
    readonly RollbackInputRelayRuntime m_Runtime;
    readonly int m_OwnerThreadId = Environment.CurrentManagedThreadId;

    public RollbackRelayQuerySource(DeterministicRollbackServerManifest manifest, RollbackInputRelayRuntime runtime)
    {
        m_Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public RollbackGmSessionSnapshot CaptureSession()
    {
        RequireOwnerThread();
        return new RollbackGmSessionSnapshot(
            m_Manifest.candidate.candidateId,
            m_Manifest.runId,
            m_Manifest.sessionId,
            m_Manifest.candidate.relayServerPeerId,
            m_Runtime.LocalEndPoint.ToString(),
            $"{m_Manifest.candidate.modelId}@{m_Manifest.candidate.modelVersion}/{m_Manifest.candidate.modelConfigurationHash}",
            $"{m_Manifest.candidate.protocolId}@{m_Manifest.candidate.protocolVersion}/{m_Manifest.candidate.protocolSchemaHash}",
            m_Manifest.candidate.programId,
            m_Manifest.candidate.fixedProgramHash,
            m_Manifest.candidate.tickRate,
            m_Manifest.candidate.maximumPredictionLeadTicks,
            m_Manifest.candidate.confirmationDelayTicks);
    }

    public RollbackGmActorSnapshot[] CaptureActors()
    {
        RequireOwnerThread();
        IReadOnlyList<RollbackRelayPeerStatus> peers = m_Runtime.CapturePeerStatus();
        var result = new RollbackGmActorSnapshot[peers.Count];
        for (int i = 0; i < result.Length; i++)
        {
            RollbackRelayPeerStatus peer = peers[i];
            result[i] = new RollbackGmActorSnapshot(
                peer.PeerId,
                peer.PlayerId,
                peer.ActorId.Value,
                peer.HandshakeAccepted,
                m_Runtime.IsRosterLocked,
                peer.HasInputFrontier,
                peer.InputFrontier);
        }
        return result;
    }

    public RollbackGmRuntimeSnapshot CaptureRuntime()
    {
        RequireOwnerThread();
        return new RollbackGmRuntimeSnapshot(
            m_Runtime.IsRosterLocked,
            m_Runtime.TotalReceivedDatagrams,
            m_Runtime.TotalSentDatagrams,
            m_Runtime.InputBatchCount,
            m_Runtime.ExplicitRelayBroadcastCount,
            m_Runtime.DeduplicatedInputCount,
            m_Runtime.InvalidInputCount,
            m_Runtime.CanonicalBundleCount,
            m_Runtime.NextCanonicalTick.Value,
            m_Runtime.ConfirmedCanonicalTick,
            m_Runtime.ConfirmationBroadcastCount,
            m_Runtime.HashReportCount,
            m_Runtime.PendingReliableCount,
            m_Runtime.DroppedReceivedDatagrams,
            m_Runtime.ReceiveQueueDepth,
            m_Runtime.SendQueueDepth);
    }

    void RequireOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != m_OwnerThreadId)
            throw new InvalidOperationException("GM Relay 快照必须在创建查询端口的 Relay 运行线程读取。");
    }
}
