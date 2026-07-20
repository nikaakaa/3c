namespace Fantasy;

public static class ServerAuthoritativeAuthorityHostRoutePort
{
    public static void SendRoster(ServerAuthoritativeRoom room)
    {
        ServerAuthoritativeRosterMessage roster = ServerAuthoritativeRoomRuntime.BuildRoster(room);
        foreach (ServerAuthoritativeRoomPlayer player in room.PlayersById.Values)
        {
            if (player.Session is { IsDisposed: false })
                player.Session.Send(new G2C_ServerAuthoritativeRosterChanged { Roster = roster });
        }
        RequireAdapter(room).SendRoster(room, roster);
    }

    public static void SendTicket(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket)
    {
        if (!room.PlayersById.TryGetValue(ticket.PlayerId, out ServerAuthoritativeRoomPlayer? player) ||
            player.Session is not { IsDisposed: false })
        {
            throw new InvalidOperationException($"Data-plane ticket '{ticket.TicketId}' has no active Client route.");
        }
        ServerAuthoritativeDataPlaneTicketMessage outer = BuildOuterTicket(room, ticket);
        player.Session.Send(new G2C_ServerAuthoritativeDataPlaneTicketIssued { Ticket = outer });
        RequireAdapter(room).SendTicket(room, ticket, outer);
    }

    public static void SendHostHeartbeatAck(
        ServerAuthoritativeRoom room,
        ulong sequence,
        long sentUnixMilliseconds,
        long serverUnixMilliseconds) =>
        RequireAdapter(room).SendHeartbeatAck(room, sequence, sentUnixMilliseconds, serverUnixMilliseconds);

    public static void SendFullCheckpointRequest(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeRoomPlayer player,
        ulong requestSequence,
        ulong lastUsableSnapshotSequence,
        string reason) =>
        RequireAdapter(room).SendFullCheckpointRequest(
            room,
            player,
            requestSequence,
            lastUsableSnapshotSequence,
            reason);

    public static void SendReliableEvents(
        ServerAuthoritativeRoomPlayer recipient,
        string roomId,
        string sessionId,
        IReadOnlyList<ServerAuthoritativeReliableGameplayEventMessage> events)
    {
        if (recipient.Session is not { IsDisposed: false })
            throw new InvalidOperationException($"Reliable event recipient '{recipient.ActorId}' is disconnected.");
        var message = new G2C_ServerAuthoritativeReliableGameplayEventBatch
        {
            RoomId = roomId,
            SessionId = sessionId
        };
        for (int i = 0; i < events.Count; i++)
            message.Events.Add(events[i]);
        recipient.Session.Send(message);
    }

    public static void SendFullCheckpoint(
        ServerAuthoritativeRoomPlayer recipient,
        string roomId,
        string sessionId,
        string actorId,
        ulong authorityTick,
        ulong confirmedInputSequence,
        ulong reliableEventHorizon,
        string checkpointLayoutHash,
        string checkpointHash,
        byte[] checkpoint,
        ulong snapshotSequence)
    {
        if (recipient.Session is not { IsDisposed: false })
            throw new InvalidOperationException($"Checkpoint recipient '{recipient.ActorId}' is disconnected.");
        recipient.Session.Send(new G2C_ServerAuthoritativeFullCheckpointResponse
        {
            RoomId = roomId,
            SessionId = sessionId,
            ActorId = actorId,
            AuthorityTick = authorityTick,
            ConfirmedInputSequence = confirmedInputSequence,
            ReliableEventHorizon = reliableEventHorizon,
            CheckpointLayoutHash = checkpointLayoutHash,
            CheckpointHash = checkpointHash,
            CheckpointLength = checked((uint)checkpoint.Length),
            Checkpoint = checkpoint,
            SnapshotSequence = snapshotSequence
        });
    }

    public static void SendFailure(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeErrorCode code,
        string reason,
        bool notifyAuthorityHost)
    {
        foreach (ServerAuthoritativeRoomPlayer player in room.PlayersById.Values)
        {
            if (player.Session is { IsDisposed: false })
            {
                player.Session.Send(new G2C_ServerAuthoritativeSessionFailed
                {
                    RoomId = room.RoomId,
                    SessionId = room.SessionId,
                    ResultCode = (int)code,
                    Reason = reason
                });
            }
        }
        if (notifyAuthorityHost && room.AuthorityRoute is { IsDisposed: false })
            RequireAdapter(room).SendFailure(room, code, reason);
    }

    public static void SendTicketRevoked(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeDataPlaneTicket ticket,
        string reason)
    {
        if (room.PlayersById.TryGetValue(ticket.PlayerId, out ServerAuthoritativeRoomPlayer? player) &&
            player.Session is { IsDisposed: false })
        {
            player.Session.Send(new G2C_ServerAuthoritativeDataPlaneTicketRevoked
            {
                RoomId = room.RoomId,
                SessionId = room.SessionId,
                TicketId = ticket.TicketId,
                Reason = reason
            });
        }
        if (room.AuthorityRoute is { IsDisposed: false })
            RequireAdapter(room).SendTicketRevoked(room, ticket, reason);
    }

    public static void ReleaseHost(ServerAuthoritativeRoom room)
    {
        if (room.AuthorityRoute is { IsDisposed: false })
            RequireAdapter(room).ReleaseHost(room);
    }

    public static ServerAuthoritativeAuthorityHostIdentityMessage BuildOuterHost(
        ServerAuthoritativeAuthorityHostRoute route,
        string roomId) => new()
    {
        HostProductId = route.HostProductId,
        HostId = route.HostId,
        RouteKind = (int)route.RouteKind,
        RoomId = roomId
    };

    public static ServerAuthoritativeWorldIdentityMessage BuildOuterWorld(ServerAuthoritativeAuthorityHostRoute route) => new()
    {
        SolverId = route.SolverId,
        SolverVersion = route.SolverVersion,
        SolverCapabilities = route.SolverCapabilities,
        SolverFeatures = route.SolverFeatures,
        WorldId = route.WorldId,
        MapId = route.MapId,
        WorldRevision = route.WorldRevision,
        WorldConfigurationHash = route.WorldConfigurationHash,
        NavigationSurfaceArtifactHash = route.NavigationSurfaceArtifactHash,
        QueryProfileHash = route.QueryProfileHash
    };

    public static ServerAuthoritativePipelineIdentityMessage BuildOuterPipeline(ServerAuthoritativeAuthorityHostRoute route) => new()
    {
        PipelineId = route.AuthorityPipelineId,
        PipelineHash = route.AuthorityPipelineHash,
        BackendId = route.BackendId,
        SolverId = route.SolverId,
        SolverVersion = route.SolverVersion,
        TickRate = route.TickRate,
        SolverCapabilities = route.SolverCapabilities,
        SolverFeatures = route.SolverFeatures
    };

    static ServerAuthoritativeDataPlaneTicketMessage BuildOuterTicket(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeDataPlaneTicket ticket)
    {
        ServerAuthoritativeAuthorityHostRoute route = RequireRoute(room);
        return new ServerAuthoritativeDataPlaneTicketMessage
        {
            TicketId = ticket.TicketId,
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            PlayerId = ticket.PlayerId,
            ActorId = ticket.ActorId,
            HostId = route.HostId,
            AuthorityEndpoint = new ServerAuthoritativeDataEndpointMessage
            {
                Host = route.DataHost,
                Port = route.DataPort
            },
            Nonce = ticket.Nonce,
            ExpiresAtUnixMilliseconds = ticket.ExpiresAtUnixMilliseconds
        };
    }

    static IServerAuthoritativeAuthorityHostRouteAdapter RequireAdapter(ServerAuthoritativeRoom room)
    {
        ServerAuthoritativeAuthorityHostRoute route = RequireRoute(room);
        IServerAuthoritativeAuthorityHostRouteAdapter adapter =
            ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Adapter;
        if (adapter.RouteKind != route.RouteKind ||
            !string.Equals(adapter.HostProductId, route.HostProductId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Active Room Authority Host route does not match the installed server product adapter.");
        }
        return adapter;
    }

    static ServerAuthoritativeAuthorityHostRoute RequireRoute(ServerAuthoritativeRoom room) =>
        room.AuthorityRoute is { IsDisposed: false } route
            ? route
            : throw new InvalidOperationException("Room has no active Authority Host route.");
}
