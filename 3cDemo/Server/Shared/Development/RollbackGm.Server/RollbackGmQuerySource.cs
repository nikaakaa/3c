using ThirdPersonSimulation.DeterministicRollback;

namespace ThirdPerson.Development.Gm.Rollback;

public sealed class RollbackGmQuerySource : IRollbackGmQuerySource
{
    readonly DeterministicRollbackServerManifest m_Manifest;
    readonly RollbackInputRelayRuntime m_Runtime;
    readonly int m_OwnerThreadId = Environment.CurrentManagedThreadId;

    public RollbackGmQuerySource(DeterministicRollbackServerManifest manifest, RollbackInputRelayRuntime runtime)
    {
        m_Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public RollbackGmSessionSnapshot CaptureSession()
    {
        RequireOwnerThread();
        return new RollbackGmSessionSnapshot(
            m_Manifest.buildId,
            m_Manifest.sessionId,
            m_Manifest.relayServerPeerId,
            m_Runtime.LocalEndPoint.ToString(),
            $"{m_Manifest.modelId}@{m_Manifest.modelVersion}/{m_Manifest.modelConfigurationHash}",
            $"{m_Manifest.protocolId}@{m_Manifest.protocolVersion}/{m_Manifest.protocolSchemaHash}",
            m_Manifest.programId,
            m_Manifest.fixedProgramHash,
            m_Manifest.tickRate,
            m_Manifest.maximumPredictionLeadTicks,
            m_Manifest.confirmationDelayTicks);
    }

    public IReadOnlyList<RollbackGmActorSnapshot> CaptureActors()
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
