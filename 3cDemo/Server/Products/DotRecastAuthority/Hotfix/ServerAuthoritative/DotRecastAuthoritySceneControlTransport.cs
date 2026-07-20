using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecastAuthority;
using ThirdPersonSimulation.ServerAuthoritative;

namespace Fantasy;

public sealed class DotRecastAuthoritySceneControlTransport : IServerAuthoritativeAuthorityControlTransport
{
    readonly Scene m_Scene;
    readonly long m_GateAddress;
    readonly long m_AuthorityAddress;
    readonly ServerAuthoritativeAuthorityHostIdentity m_Host;
    readonly int m_HeartbeatTicks;
    readonly NetworkCheckpointLayout m_CheckpointLayout;
    readonly Queue<ServerAuthoritativeAuthorityRegistrationResult> m_Registrations = new();
    readonly Queue<ServerAuthoritativeAuthorityRosterLock> m_Rosters = new();
    readonly Queue<ServerAuthoritativeAuthorityDataPlaneTicket> m_Tickets = new();
    readonly Queue<ServerAuthoritativeAuthorityHeartbeatAck> m_HeartbeatAcks = new();
    readonly Queue<ServerAuthoritativeAuthorityFullCheckpointRequest> m_CheckpointRequests = new();
    ServerAuthoritativeSessionId m_SessionId;
    ServerAuthoritativeAuthorityControlFailure? m_Failure;
    ulong m_HeartbeatSequence;
    bool m_RegistrationAccepted;
    bool m_LeaveSent;
    bool m_Disposed;

    public DotRecastAuthoritySceneControlTransport(
        Scene scene,
        long gateAddress,
        long authorityAddress,
        LoadedDotRecastAuthoritySceneManifest loaded)
    {
        m_Scene = scene ?? throw new ArgumentNullException(nameof(scene));
        if (gateAddress == 0)
            throw new ArgumentOutOfRangeException(nameof(gateAddress));
        if (authorityAddress == 0)
            throw new ArgumentOutOfRangeException(nameof(authorityAddress));
        m_GateAddress = gateAddress;
        m_AuthorityAddress = authorityAddress;
        DotRecastAuthoritySceneManifest manifest = loaded?.Manifest ?? throw new ArgumentNullException(nameof(loaded));
        m_Host = new ServerAuthoritativeAuthorityHostIdentity(
            manifest.HostProductId,
            manifest.HostId,
            ThirdPersonSimulation.ServerAuthoritative.ServerAuthoritativeAuthorityHostRouteKind.InProcessAuthorityScene,
            manifest.RoomId);
        m_HeartbeatTicks = manifest.Pipeline.SourcePolicy.ControlHeartbeatTicks;
        m_CheckpointLayout = new NetworkCheckpointLayout(loaded.Program);
    }

    public ServerAuthoritativeAuthorityControlTransportStatus ControlStatus => m_Failure != null
        ? ServerAuthoritativeAuthorityControlTransportStatus.Failed
        : m_RegistrationAccepted
            ? ServerAuthoritativeAuthorityControlTransportStatus.Ready
            : ServerAuthoritativeAuthorityControlTransportStatus.Pending;

    public ServerAuthoritativeAuthorityControlFailure? ControlFailure => m_Failure;
    public ServerAuthoritativeAuthorityHostIdentity Host => m_Host;
    public long AuthorityAddress => m_AuthorityAddress;
    public ServerAuthoritativeSessionId SessionId => m_SessionId;

    public void AcceptRegistration(G2A_ServerAuthoritativeAuthoritySceneRegisterResponse response)
    {
        ThrowIfDisposed();
        if (response == null || response.ResultCode != 0 || string.IsNullOrWhiteSpace(response.SessionId) || response.RoomRevision == 0)
        {
            Fail(
                "authority_scene_register_rejected",
                response == null ? "Gate returned no Authority Scene registration response." : response.FailureReason);
            return;
        }
        if (m_RegistrationAccepted)
            throw new InvalidOperationException("Authority Scene registration was accepted more than once.");
        m_SessionId = new ServerAuthoritativeSessionId(response.SessionId);
        m_Registrations.Enqueue(new ServerAuthoritativeAuthorityRegistrationResult(m_SessionId, m_Host));
        m_RegistrationAccepted = true;
    }

    public void ReceiveRoster(G2A_ServerAuthoritativeAuthoritySceneRosterLock message)
    {
        RequireRoute(message?.Host, message?.SessionId);
        if (message!.RoomRevision == 0 || message.Members.Count == 0 || m_Rosters.Count != 0)
        {
            Fail("authority_scene_roster_invalid", "Gate supplied an invalid or duplicate Authority roster.");
            return;
        }
        var roster = new ServerAuthoritativeRosterEntry[message.Members.Count];
        for (int i = 0; i < roster.Length; i++)
        {
            ServerAuthoritativeInnerRosterMember member = message.Members[i];
            roster[i] = new ServerAuthoritativeRosterEntry(
                new ServerAuthoritativePlayerId(member.PlayerId),
                new ActorId(member.ActorId),
                (ThirdPersonSimulation.ServerAuthoritative.ServerAuthoritativeProcessRole)member.ProcessRole);
        }
        m_Rosters.Enqueue(new ServerAuthoritativeAuthorityRosterLock(
            m_SessionId,
            m_Host,
            message.RoomRevision,
            roster));
    }

    public void ReceiveTicket(G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket message)
    {
        ServerAuthoritativeInnerDataPlaneTicket? ticket = message?.Ticket;
        RequireRoute(ticket?.Host, ticket?.SessionId);
        if (ticket == null || m_Tickets.Count >= 2)
        {
            Fail("authority_scene_ticket_queue_overflow", "Authority Scene ticket queue is invalid or full.");
            return;
        }
        m_Tickets.Enqueue(new ServerAuthoritativeAuthorityDataPlaneTicket(
            m_SessionId,
            m_Host,
            new ServerAuthoritativePlayerId(ticket.PlayerId),
            new ActorId(ticket.ActorId),
            ticket.TicketId,
            ticket.Nonce,
            ticket.ExpiresAtUnixMilliseconds));
    }

    public void ReceiveHeartbeatAck(G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck message)
    {
        RequireRoute(message?.Host, message?.SessionId);
        if (message == null || m_HeartbeatAcks.Count >= 8)
        {
            Fail("authority_scene_heartbeat_queue_overflow", "Authority Scene heartbeat ack queue is full.");
            return;
        }
        m_HeartbeatAcks.Enqueue(new ServerAuthoritativeAuthorityHeartbeatAck(
            message.Sequence,
            message.SentUnixMilliseconds,
            message.ServerUnixMilliseconds));
    }

    public void ReceiveFullCheckpointRequest(G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest message)
    {
        RequireRoute(message?.Host, message?.SessionId);
        if (message == null || m_CheckpointRequests.Count >= 8)
        {
            Fail("authority_scene_checkpoint_queue_overflow", "Authority Scene full checkpoint request queue is full.");
            return;
        }
        m_CheckpointRequests.Enqueue(new ServerAuthoritativeAuthorityFullCheckpointRequest(
            new ServerAuthoritativePlayerId(message.PlayerId),
            new ActorId(message.ActorId),
            message.RequestSequence));
    }

    public void ReceiveFailure(G2A_ServerAuthoritativeAuthoritySceneFailure message)
    {
        RequireRoute(message?.Host, message?.SessionId);
        Fail(
            "authority_scene_gate_failed",
            message == null ? "Gate sent an empty failure." : $"{message.ResultCode}: {message.Reason}");
    }

    public void Step(SimulationTickSourceIdentity source)
    {
        ThrowIfDisposed();
        if (source.Kind != SimulationTickSourceKind.Authoritative || source.SourceTick == 0)
            throw new InvalidOperationException("Authority Scene control transport requires an Authoritative source Tick.");
        if (!m_RegistrationAccepted || source.SourceTick % (ulong)m_HeartbeatTicks != 0)
            return;
        m_Scene.Send(m_GateAddress, new A2G_ServerAuthoritativeAuthoritySceneHeartbeat
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            Sequence = checked(++m_HeartbeatSequence),
            SentUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LatestAuthorityTick = source.SourceTick - 1,
            AuthorityAddress = m_AuthorityAddress
        });
    }

    public bool TryTakeRegistration(out ServerAuthoritativeAuthorityRegistrationResult value) =>
        TryDequeue(m_Registrations, out value);

    public bool TryTakeRoster(out ServerAuthoritativeAuthorityRosterLock value) =>
        TryDequeue(m_Rosters, out value);

    public bool TryTakeTicket(out ServerAuthoritativeAuthorityDataPlaneTicket value) =>
        TryDequeue(m_Tickets, out value);

    public bool TryTakeHeartbeatAck(out ServerAuthoritativeAuthorityHeartbeatAck value) =>
        TryDequeue(m_HeartbeatAcks, out value);

    public bool TryTakeFullCheckpointRequest(out ServerAuthoritativeAuthorityFullCheckpointRequest value) =>
        TryDequeue(m_CheckpointRequests, out value);

    public void SendTicketConsumed(ServerAuthoritativeAuthorityDataPlaneTicket ticket)
    {
        RequireOperational();
        m_Scene.Send(m_GateAddress, new A2G_ServerAuthoritativeAuthoritySceneTicketConsumed
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            PlayerId = ticket.PlayerId.Value,
            TicketId = ticket.TicketId,
            AuthorityAddress = m_AuthorityAddress
        });
    }

    public void SendReliableEvents(ServerAuthoritativeAuthorityReliableEventBatchOutput value)
    {
        RequireOperational();
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        var message = new A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            RecipientActorId = value.RecipientActorId.Value,
            AuthorityAddress = m_AuthorityAddress
        };
        for (int i = 0; i < value.Events.Count; i++)
        {
            ServerAuthoritativeAuthorityReliableEventOutput output = value.Events[i];
            ServerAuthoritativeReliableEvent reliable = output.Value;
            byte[] payload = output.Payload;
            message.Events.Add(new ServerAuthoritativeInnerReliableGameplayEvent
            {
                ActorId = output.SourceActorId.Value,
                EventId = reliable.Header.EventId.ToString(),
                EventSequence = reliable.Header.Sequence,
                AuthorityTick = reliable.Header.Tick.Value,
                EventKind = reliable.IsGameplay ? "gameplay" : "presentation",
                PayloadSchemaVersion = 1,
                PayloadLength = checked((uint)payload.Length),
                Payload = payload
            });
        }
        m_Scene.Send(m_GateAddress, message);
    }

    public void SendFullCheckpoint(ServerAuthoritativeAuthorityFullCheckpointOutput value)
    {
        RequireOperational();
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        m_Scene.Send(m_GateAddress, new A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            PlayerId = value.PlayerId.Value,
            ActorId = value.ActorId.Value,
            RequestSequence = value.RequestSequence,
            AuthorityTick = value.Checkpoint.Baseline.AuthorityTick.Value,
            ConfirmedInputSequence = value.Checkpoint.Baseline.ConfirmedInputSequence,
            ReliableEventHorizon = value.Checkpoint.Baseline.ConfirmedEventHorizon.Sequence,
            CheckpointLayoutHash = m_CheckpointLayout.LayoutIdentity.ToString(),
            CheckpointHash = value.Checkpoint.CheckpointHash.ToString(),
            CheckpointLength = checked((uint)value.Payload.Length),
            Checkpoint = value.Payload,
            SnapshotSequence = value.SnapshotSequence,
            AuthorityAddress = m_AuthorityAddress
        });
    }

    public void SendLeave(string reason)
    {
        if (m_LeaveSent || !m_RegistrationAccepted || m_Disposed)
            return;
        m_LeaveSent = true;
        m_Scene.Send(m_GateAddress, new A2G_ServerAuthoritativeAuthoritySceneLeave
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            Reason = Require(reason, nameof(reason)),
            AuthorityAddress = m_AuthorityAddress
        });
    }

    public void SendFailure(string code, string message)
    {
        if (m_Failure != null || m_Disposed)
            return;
        m_Failure = new ServerAuthoritativeAuthorityControlFailure(code, message);
        if (!m_RegistrationAccepted)
            return;
        m_Scene.Send(m_GateAddress, new A2G_ServerAuthoritativeAuthoritySceneFailure
        {
            Host = BuildHost(),
            SessionId = m_SessionId.Value,
            Code = code,
            Reason = message,
            AuthorityAddress = m_AuthorityAddress
        });
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;
        SendLeave("authority_scene_disposed");
        m_Disposed = true;
        m_Registrations.Clear();
        m_Rosters.Clear();
        m_Tickets.Clear();
        m_HeartbeatAcks.Clear();
        m_CheckpointRequests.Clear();
    }

    void RequireRoute(ServerAuthoritativeInnerHostIdentity? host, string? sessionId)
    {
        ThrowIfDisposed();
        if (!m_RegistrationAccepted || host == null ||
            !string.Equals(host.HostProductId, m_Host.HostProductId.Value, StringComparison.Ordinal) ||
            !string.Equals(host.HostId, m_Host.HostId, StringComparison.Ordinal) ||
            host.RouteKind != (int)ThirdPersonSimulation.ServerAuthoritative.ServerAuthoritativeAuthorityHostRouteKind.InProcessAuthorityScene ||
            !string.Equals(host.RoomId, m_Host.RoomId.Value, StringComparison.Ordinal) ||
            !string.Equals(sessionId, m_SessionId.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Gate control message targets another Authority Host route.");
        }
    }

    void RequireOperational()
    {
        ThrowIfDisposed();
        if (m_Failure != null || !m_RegistrationAccepted)
            throw new InvalidOperationException("Authority Scene control transport is not operational.");
    }

    void Fail(string code, string message)
    {
        m_Failure = new ServerAuthoritativeAuthorityControlFailure(
            Require(code, nameof(code)),
            string.IsNullOrWhiteSpace(message) ? "Authority Scene control transport failed." : message.Trim());
    }

    ServerAuthoritativeInnerHostIdentity BuildHost() => new()
    {
        HostProductId = m_Host.HostProductId.Value,
        HostId = m_Host.HostId,
        RouteKind = (int)m_Host.RouteKind,
        RoomId = m_Host.RoomId.Value
    };

    void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(DotRecastAuthoritySceneControlTransport));
    }

    static bool TryDequeue<T>(Queue<T> queue, out T value)
    {
        if (queue.Count == 0)
        {
            value = default!;
            return false;
        }
        value = queue.Dequeue();
        return true;
    }

    static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Authority Scene control value is required.", parameter)
        : value.Trim();
}
