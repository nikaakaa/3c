using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Network;

namespace Fantasy;

public enum ServerAuthoritativeProcessRole : int
{
    None = 0,
    AuthorityWorker = 1,
    ClientA = 2,
    ClientB = 3
}

public enum ServerAuthoritativeAuthorityHostRouteKind : int
{
    None = 0,
    ExternalAuthorityWorker = 1,
    InProcessAuthorityScene = 2
}

public enum ServerAuthoritativeAuthorityHostLifecycleState : int
{
    None = 0,
    Registered = 1,
    Active = 2,
    Failed = 3,
    Closed = 4
}

public enum ServerAuthoritativeErrorCode : int
{
    Success = 0,
    InvalidProtocol = 1,
    InvalidIdentity = 2,
    WorkerUnavailable = 3,
    WorkerAlreadyRegistered = 4,
    RoomFull = 5,
    RosterIncomplete = 6,
    OwnerMismatch = 7,
    QueueOverflow = 8,
    SessionClosed = 9,
    TicketInvalid = 10,
    TicketExpired = 11,
    TicketReused = 12,
    DataPlaneFailed = 13,
    CheckpointRejected = 14
}

public sealed class ServerAuthoritativeRoomRegistry : Entity
{
    public ServerAuthoritativeRoom? Room;
}

public sealed class ServerAuthoritativeRoom : Entity
{
    public const string DemoRoomId = "corin-server-authoritative-demo";
    public const string ModelId = "thirdperson.network-model.server-authoritative-hybrid";
    public const uint ModelProtocolVersion = 3;
    public const string ExpectedEndpointId = "thirdperson.endpoint.fantasy.server-authoritative-hybrid";
    public const string ExpectedPredictionPipelineId = "thirdperson.simulation.pipeline.server-authoritative-prediction";
    public const string ExpectedAuthorityPipelineId = "thirdperson.simulation.pipeline.server-authoritative-authority";
    public const string ExpectedBackendId = "thirdperson.simulation.float32-pass-backend";
    public const string ExpectedOperationSetId = "character-gameplay-operations";
    public const string PlayerAId = "corin-player-a";
    public const string PlayerBId = "corin-player-b";
    public const string ActorAId = "corin-actor-a";
    public const string ActorBId = "corin-actor-b";
    public const int PlayerCapacity = 2;
    public const long TicketLifetimeMilliseconds = 15000;

    public string RoomId = DemoRoomId;
    public string SessionId = string.Empty;
    public ulong Revision = 1;
    public bool RosterLocked;
    public bool RosterPublished;
    public bool Failed;
    public bool Terminating;
    public ulong FullCheckpointRequestSequence;
    public ServerAuthoritativeAuthorityHostRoute? AuthorityRoute;
    public readonly Dictionary<string, ServerAuthoritativeRoomPlayer> PlayersById = new(StringComparer.Ordinal);
    public readonly Dictionary<string, ServerAuthoritativeRoomPlayer> PlayersByActor = new(StringComparer.Ordinal);
    public readonly Dictionary<string, ServerAuthoritativeDataPlaneTicket> TicketsById = new(StringComparer.Ordinal);
    public readonly Dictionary<string, ulong> LastReliableEventSequenceByActor = new(StringComparer.Ordinal);
}

public sealed class ServerAuthoritativeAuthorityHostRoute : Entity
{
    public string HostProductId = string.Empty;
    public string HostId = string.Empty;
    public ServerAuthoritativeAuthorityHostRouteKind RouteKind;
    public ServerAuthoritativeAuthorityHostLifecycleState LifecycleState;
    public IServerAuthoritativeAuthorityHostEndpoint? Endpoint;
    public string EndpointId = string.Empty;
    public string ModelConfigurationHash = string.Empty;
    public string ProgramId = string.Empty;
    public string ProgramHash = string.Empty;
    public string LayoutHash = string.Empty;
    public string OperationSetId = string.Empty;
    public string OperationSetVersion = string.Empty;
    public string AuthorityPipelineId = string.Empty;
    public string AuthorityPipelineHash = string.Empty;
    public string PredictionPipelineId = string.Empty;
    public string PredictionPipelineHash = string.Empty;
    public string BackendId = string.Empty;
    public uint TickRate;
    public string SolverId = string.Empty;
    public string SolverVersion = string.Empty;
    public ulong SolverCapabilities;
    public ulong SolverFeatures;
    public string WorldId = string.Empty;
    public string MapId = string.Empty;
    public string WorldRevision = string.Empty;
    public string WorldConfigurationHash = string.Empty;
    public string NavigationSurfaceArtifactHash = string.Empty;
    public string QueryProfileHash = string.Empty;
    public string DataHost = string.Empty;
    public uint DataPort;
    public ulong LatestAuthorityTick;
}

public sealed class ServerAuthoritativeAuthorityHostRegistration
{
    public string RoomId = string.Empty;
    public string HostProductId = string.Empty;
    public string HostId = string.Empty;
    public ServerAuthoritativeAuthorityHostRouteKind RouteKind;
    public IServerAuthoritativeAuthorityHostEndpoint? Endpoint;
    public string EndpointId = string.Empty;
    public uint ModelProtocolVersion;
    public string ModelId = string.Empty;
    public string ModelConfigurationHash = string.Empty;
    public string ProgramId = string.Empty;
    public string ProgramHash = string.Empty;
    public string LayoutHash = string.Empty;
    public string OperationSetId = string.Empty;
    public string OperationSetVersion = string.Empty;
    public string AuthorityPipelineId = string.Empty;
    public string AuthorityPipelineHash = string.Empty;
    public string PredictionPipelineId = string.Empty;
    public string PredictionPipelineHash = string.Empty;
    public string BackendId = string.Empty;
    public uint TickRate;
    public string SolverId = string.Empty;
    public string SolverVersion = string.Empty;
    public ulong SolverCapabilities;
    public ulong SolverFeatures;
    public string WorldId = string.Empty;
    public string MapId = string.Empty;
    public string WorldRevision = string.Empty;
    public string WorldConfigurationHash = string.Empty;
    public string NavigationSurfaceArtifactHash = string.Empty;
    public string QueryProfileHash = string.Empty;
    public string DataHost = string.Empty;
    public uint DataPort;
}

public sealed class ServerAuthoritativeRoomPlayer : Entity, ISupportedMultiEntity
{
    public string PlayerId = string.Empty;
    public string ActorId = string.Empty;
    public ServerAuthoritativeProcessRole ProcessRole;
    public Session? Session;
    public bool JoinAccepted;
    public string TicketId = string.Empty;
    public ulong PendingCheckpointRequestSequence;
}

public sealed class ServerAuthoritativeDataPlaneTicket : Entity, ISupportedMultiEntity
{
    public string TicketId = string.Empty;
    public string PlayerId = string.Empty;
    public string ActorId = string.Empty;
    public string Nonce = string.Empty;
    public long ExpiresAtUnixMilliseconds;
    public bool AuthorityConsumed;
    public bool ClientConsumed;
    public bool Revoked;
}

public sealed class ServerAuthoritativeConnectionBinding : Entity
{
    public ServerAuthoritativeRoom? Room;
    public ServerAuthoritativeProcessRole ProcessRole;
    public string ParticipantId = string.Empty;
}
