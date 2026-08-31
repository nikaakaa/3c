namespace ThirdPerson.Development.Gm.Rollback;

public interface IRollbackGmQuerySource
{
    RollbackGmSessionSnapshot CaptureSession();
    IReadOnlyList<RollbackGmActorSnapshot> CaptureActors();
    RollbackGmRuntimeSnapshot CaptureRuntime();
}

public sealed record RollbackGmSessionSnapshot(
    string BuildId,
    string SessionId,
    string RelayPeerId,
    string Endpoint,
    string ModelIdentity,
    string ProtocolIdentity,
    string ProgramId,
    string ProgramHash,
    int TickRate,
    int MaximumPredictionLeadTicks,
    int ConfirmationDelayTicks);

public sealed record RollbackGmActorSnapshot(
    string PeerId,
    string PlayerId,
    string ActorId,
    bool HandshakeAccepted,
    bool RosterLocked,
    bool HasInputFrontier,
    ulong InputFrontier);

public sealed record RollbackGmRuntimeSnapshot(
    bool RosterLocked,
    long ReceivedDatagrams,
    long SentDatagrams,
    ulong InputBatches,
    ulong ForwardedBatches,
    ulong DeduplicatedInputs,
    ulong InvalidInputs,
    ulong CanonicalBundles,
    ulong NextCanonicalTick,
    ulong ConfirmedTick,
    ulong ConfirmationBroadcasts,
    ulong HashReports,
    int PendingReliable,
    long DroppedDatagrams,
    int ReceiveQueueDepth,
    int SendQueueDepth);
