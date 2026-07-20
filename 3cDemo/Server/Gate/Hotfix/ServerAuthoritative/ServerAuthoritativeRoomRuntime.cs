using Fantasy.Network;

using ThirdPersonSimulation.ServerAuthoritative;

namespace Fantasy;

public static class ServerAuthoritativeRoomRuntime
{
    public static bool TryGetRoom(
        Session session,
        string roomId,
        out ServerAuthoritativeRoom room,
        out string reason)
    {
        room = null!;
        if (session == null || session.IsDisposed)
        {
            reason = "Control Session is unavailable.";
            return false;
        }
        return TryGetRoom(session.Scene, roomId, out room, out reason);
    }

    public static bool TryGetRoom(
        Scene scene,
        string roomId,
        out ServerAuthoritativeRoom room,
        out string reason)
    {
        room = null!;
        if (scene == null || scene.IsDisposed || scene.SceneType != SceneType.Gate)
        {
            reason = "ServerAuthoritative Room is only owned by the Gate Scene.";
            return false;
        }
        ServerAuthoritativeRoomRegistry? registry = scene.GetComponent<ServerAuthoritativeRoomRegistry>();
        if (registry?.Room is not { IsDisposed: false } value)
        {
            reason = "ServerAuthoritative Room registry is unavailable.";
            return false;
        }
        if (!string.Equals(roomId, value.RoomId, StringComparison.Ordinal))
        {
            reason = $"Room '{roomId}' is unknown.";
            return false;
        }
        room = value;
        reason = string.Empty;
        return true;
    }

    public static ServerAuthoritativeErrorCode RegisterHost(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeAuthorityHostRegistration registration,
        out string reason)
    {
        if (!CanMutate(room, out reason))
            return ServerAuthoritativeErrorCode.SessionClosed;
        if (room.AuthorityRoute is { IsDisposed: false })
        {
            reason = "Room already owns an Authority Host route.";
            return ServerAuthoritativeErrorCode.WorkerAlreadyRegistered;
        }
        if (!ValidateRegistration(room, registration, out reason))
            return ServerAuthoritativeErrorCode.InvalidIdentity;

        var route = room.AddComponent<ServerAuthoritativeAuthorityHostRoute>();
        route.HostProductId = registration.HostProductId;
        route.HostId = registration.HostId;
        route.RouteKind = registration.RouteKind;
        route.LifecycleState = ServerAuthoritativeAuthorityHostLifecycleState.Registered;
        route.Endpoint = registration.Endpoint;
        route.EndpointId = registration.EndpointId;
        route.ModelConfigurationHash = registration.ModelConfigurationHash;
        route.ProgramId = registration.ProgramId;
        route.ProgramHash = registration.ProgramHash;
        route.LayoutHash = registration.LayoutHash;
        route.OperationSetId = registration.OperationSetId;
        route.OperationSetVersion = registration.OperationSetVersion;
        route.AuthorityPipelineId = registration.AuthorityPipelineId;
        route.AuthorityPipelineHash = registration.AuthorityPipelineHash;
        route.PredictionPipelineId = registration.PredictionPipelineId;
        route.PredictionPipelineHash = registration.PredictionPipelineHash;
        route.BackendId = registration.BackendId;
        route.TickRate = registration.TickRate;
        route.SolverId = registration.SolverId;
        route.SolverVersion = registration.SolverVersion;
        route.SolverCapabilities = registration.SolverCapabilities;
        route.SolverFeatures = registration.SolverFeatures;
        route.WorldId = registration.WorldId;
        route.MapId = registration.MapId;
        route.WorldRevision = registration.WorldRevision;
        route.WorldConfigurationHash = registration.WorldConfigurationHash;
        route.NavigationSurfaceArtifactHash = registration.NavigationSurfaceArtifactHash;
        route.QueryProfileHash = registration.QueryProfileHash;
        route.DataHost = registration.DataHost;
        route.DataPort = registration.DataPort;
        room.AuthorityRoute = route;
        room.SessionId = Guid.NewGuid().ToString("N");
        room.Revision = checked(room.Revision + 1);

        reason = string.Empty;
        Log.Info($"Authority Host '{registration.HostId}' registered Room '{room.RoomId}' through '{registration.RouteKind}'.");
        return ServerAuthoritativeErrorCode.Success;
    }

    public static ServerAuthoritativeErrorCode JoinClient(
        ServerAuthoritativeRoom room,
        Session session,
        C2G_ServerAuthoritativeClientJoinRequest request,
        out ServerAuthoritativeRoomPlayer? player,
        out string reason)
    {
        player = null;
        if (!CanMutate(room, out reason))
            return ServerAuthoritativeErrorCode.SessionClosed;
        if (room.AuthorityRoute is not { IsDisposed: false } route ||
            route.LifecycleState is not (ServerAuthoritativeAuthorityHostLifecycleState.Registered or
                ServerAuthoritativeAuthorityHostLifecycleState.Active))
        {
            reason = "Authority Host is not registered.";
            return ServerAuthoritativeErrorCode.WorkerUnavailable;
        }
        if (room.RosterLocked)
        {
            reason = "Room roster is already locked.";
            return ServerAuthoritativeErrorCode.RoomFull;
        }
        if (!ResolveClientIdentity(request.PlayerId, request.ProcessRole, out string actorId, out ServerAuthoritativeProcessRole role, out reason))
            return ServerAuthoritativeErrorCode.InvalidIdentity;
        if (room.PlayersById.ContainsKey(request.PlayerId) || room.PlayersByActor.ContainsKey(actorId))
        {
            reason = $"Player '{request.PlayerId}' is already joined.";
            return ServerAuthoritativeErrorCode.OwnerMismatch;
        }
        if (room.PlayersById.Count >= ServerAuthoritativeRoom.PlayerCapacity)
        {
            reason = "Room is full.";
            return ServerAuthoritativeErrorCode.RoomFull;
        }
        if (!ValidateClientIdentity(room, request, route, out reason))
            return ServerAuthoritativeErrorCode.InvalidIdentity;

        player = room.AddComponent<ServerAuthoritativeRoomPlayer>();
        player.PlayerId = request.PlayerId;
        player.ActorId = actorId;
        player.ProcessRole = role;
        player.Session = session;
        room.PlayersById.Add(player.PlayerId, player);
        room.PlayersByActor.Add(player.ActorId, player);
        BindConnection(session, room, role, player.PlayerId);
        room.Revision = checked(room.Revision + 1);

        if (room.PlayersById.Count == ServerAuthoritativeRoom.PlayerCapacity)
            PrepareRosterLock(room);
        reason = string.Empty;
        return ServerAuthoritativeErrorCode.Success;
    }

    public static void BindAuthorityControlSession(
        ServerAuthoritativeRoom room,
        Session session,
        string hostId) =>
        BindConnection(session, room, ServerAuthoritativeProcessRole.AuthorityWorker, hostId);

    public static void AcceptClientJoin(
        ServerAuthoritativeRoom room,
        Session session,
        C2G_ServerAuthoritativeClientJoinAccepted message)
    {
        if (!CanMutate(room, out string reason) ||
            !MatchesSession(room, message.SessionId) ||
            room.AuthorityRoute is not { IsDisposed: false } route ||
            !string.Equals(route.HostId, message.HostId, StringComparison.Ordinal) ||
            !TryGetPlayer(room, session, message.PlayerId, out ServerAuthoritativeRoomPlayer player))
        {
            Fail(
                room,
                ServerAuthoritativeErrorCode.InvalidIdentity,
                string.IsNullOrWhiteSpace(reason) ? "Client join acceptance identity is invalid." : reason,
                "control.join.accepted",
                ResolveActor(room, message.PlayerId));
            return;
        }
        if (player.JoinAccepted)
        {
            Fail(
                room,
                ServerAuthoritativeErrorCode.InvalidIdentity,
                $"Player '{player.PlayerId}' accepted the join handshake more than once.",
                "control.join.accepted",
                player.ActorId);
            return;
        }
        player.JoinAccepted = true;
        if (!room.RosterLocked || room.RosterPublished)
            return;
        foreach (ServerAuthoritativeRoomPlayer candidate in room.PlayersById.Values)
        {
            if (!candidate.JoinAccepted)
                return;
        }
        PublishRosterLock(room);
    }

    static void PublishRosterLock(ServerAuthoritativeRoom room)
    {
        if (!room.RosterLocked || room.RosterPublished ||
            room.AuthorityRoute is not { IsDisposed: false, LifecycleState: ServerAuthoritativeAuthorityHostLifecycleState.Active } ||
            room.TicketsById.Count != ServerAuthoritativeRoom.PlayerCapacity ||
            room.PlayersById.Count != ServerAuthoritativeRoom.PlayerCapacity ||
            room.PlayersById.Values.Any(player => !player.JoinAccepted))
        {
            throw new InvalidOperationException("Prepared roster lock is incomplete.");
        }

        ServerAuthoritativeAuthorityHostRoutePort.SendRoster(room);
        foreach (ServerAuthoritativeDataPlaneTicket ticket in room.TicketsById.Values.OrderBy(value => value.ActorId, StringComparer.Ordinal))
            ServerAuthoritativeAuthorityHostRoutePort.SendTicket(room, ticket);
        room.RosterPublished = true;
    }

    public static ServerAuthoritativeRosterMessage BuildRoster(ServerAuthoritativeRoom room)
    {
        var roster = new ServerAuthoritativeRosterMessage
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            Revision = room.Revision,
            Locked = room.RosterLocked,
            HostId = room.AuthorityRoute?.HostId ?? string.Empty
        };
        foreach (ServerAuthoritativeRoomPlayer player in room.PlayersByActor.Values.OrderBy(value => value.ActorId, StringComparer.Ordinal))
        {
            roster.Members.Add(new ServerAuthoritativeRosterMemberMessage
            {
                PlayerId = player.PlayerId,
                ActorId = player.ActorId,
                ProcessRole = (int)player.ProcessRole
            });
        }
        return roster;
    }

    public static void MarkClientTicketConsumed(
        ServerAuthoritativeRoom room,
        Session session,
        C2G_ServerAuthoritativeDataPlaneTicketConsumed message)
    {
        if (!TryGetPlayer(room, session, message.PlayerId, out ServerAuthoritativeRoomPlayer player) ||
            !MatchesSession(room, message.SessionId) ||
            !TryGetUsableTicket(room, message.TicketId, player.PlayerId, out ServerAuthoritativeDataPlaneTicket ticket))
        {
            Fail(room, ServerAuthoritativeErrorCode.TicketInvalid, "Client confirmed an invalid data-plane ticket.", "control.ticket.client", ResolveActor(room, message.PlayerId));
            return;
        }
        if (ticket.ClientConsumed)
        {
            Fail(room, ServerAuthoritativeErrorCode.TicketReused, $"Client reused ticket '{ticket.TicketId}'.", "control.ticket.client", ticket.ActorId);
            return;
        }
        ticket.ClientConsumed = true;
    }

    public static void MarkAuthorityHostTicketConsumed(
        ServerAuthoritativeRoom room,
        string sessionId,
        string ticketId,
        string playerId)
    {
        if (!MatchesSession(room, sessionId) ||
            !TryGetUsableTicket(room, ticketId, playerId, out ServerAuthoritativeDataPlaneTicket ticket))
        {
            Fail(room, ServerAuthoritativeErrorCode.TicketInvalid, "Authority Host confirmed an invalid data-plane ticket.", "control.ticket.authority", ResolveActor(room, playerId));
            return;
        }
        MarkAuthorityTicketConsumed(room, ticket);
    }

    public static void AcknowledgeClientHeartbeat(
        ServerAuthoritativeRoom room,
        Session session,
        C2G_ServerAuthoritativeControlHeartbeat message)
    {
        if (!MatchesSession(room, message.SessionId) ||
            !TryGetPlayer(room, session, message.PlayerId, out ServerAuthoritativeRoomPlayer player))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Client heartbeat route is invalid.", "control.heartbeat.client", ResolveActor(room, message.PlayerId));
            return;
        }
        player.Session!.Send(new G2C_ServerAuthoritativeControlHeartbeatAck
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            Sequence = message.Sequence,
            ClientUnixMilliseconds = message.ClientUnixMilliseconds,
            ServerUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public static void AcknowledgeAuthorityHostHeartbeat(
        ServerAuthoritativeRoom room,
        string sessionId,
        ulong sequence,
        long sentUnixMilliseconds,
        ulong latestAuthorityTick)
    {
        if (!MatchesSession(room, sessionId))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Authority Host heartbeat Session is invalid.", "control.heartbeat.authority", "all", latestAuthorityTick);
            return;
        }
        AcknowledgeHostHeartbeat(room, sequence, sentUnixMilliseconds, latestAuthorityTick);
    }

    public static void RouteAuthorityReliableEvents(
        ServerAuthoritativeRoom room,
        string sessionId,
        string recipientActorId,
        IReadOnlyList<ServerAuthoritativeReliableGameplayEventMessage> events)
    {
        if (!MatchesSession(room, sessionId))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Authority reliable event Session is invalid.", "control.reliable.authority", recipientActorId);
            return;
        }
        RouteReliableEvents(room, recipientActorId, events);
    }

    public static void RequestFullCheckpoint(
        ServerAuthoritativeRoom room,
        Session session,
        C2G_ServerAuthoritativeFullCheckpointRequest message)
    {
        if (!MatchesSession(room, message.SessionId) ||
            !TryGetPlayer(room, session, message.PlayerId, out ServerAuthoritativeRoomPlayer player) ||
            !string.Equals(player.ActorId, message.ActorId, StringComparison.Ordinal))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Full checkpoint request route is invalid.", "control.checkpoint.client", message.ActorId);
            return;
        }
        if (player.PendingCheckpointRequestSequence != 0)
        {
            Fail(room, ServerAuthoritativeErrorCode.CheckpointRejected, $"Player '{player.PlayerId}' already has a pending full checkpoint request.", "control.checkpoint.client", player.ActorId);
            return;
        }
        ulong sequence = checked(++room.FullCheckpointRequestSequence);
        player.PendingCheckpointRequestSequence = sequence;
        ServerAuthoritativeAuthorityHostRoutePort.SendFullCheckpointRequest(
            room,
            player,
            sequence,
            message.LastUsableSnapshotSequence,
            message.Reason);
    }

    public static void RouteAuthorityFullCheckpoint(
        ServerAuthoritativeRoom room,
        string sessionId,
        string playerId,
        string actorId,
        ulong requestSequence,
        ulong authorityTick,
        ulong confirmedInputSequence,
        ulong reliableEventHorizon,
        string checkpointLayoutHash,
        string checkpointHash,
        uint checkpointLength,
        byte[] checkpoint,
        ulong snapshotSequence)
    {
        if (!MatchesSession(room, sessionId))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Authority full checkpoint Session is invalid.", "control.checkpoint.authority", actorId, authorityTick);
            return;
        }
        RouteFullCheckpoint(
            room,
            playerId,
            actorId,
            requestSequence,
            authorityTick,
            confirmedInputSequence,
            reliableEventHorizon,
            checkpointLayoutHash,
            checkpointHash,
            checkpointLength,
            checkpoint,
            snapshotSequence);
    }

    public static void Fail(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeErrorCode code,
        string reason,
        string channel,
        string actorId,
        ulong? tick = null,
        bool notifyAuthorityHost = true)
    {
        if (room == null || room.IsDisposed || room.Failed || room.Terminating)
            return;
        room.Failed = true;
        if (room.AuthorityRoute is { IsDisposed: false } route)
            route.LifecycleState = ServerAuthoritativeAuthorityHostLifecycleState.Failed;
        foreach (ServerAuthoritativeDataPlaneTicket ticket in room.TicketsById.Values)
        {
            if (ticket.Revoked)
                continue;
            ticket.Revoked = true;
            ServerAuthoritativeAuthorityHostRoutePort.SendTicketRevoked(room, ticket, reason);
        }
        ServerAuthoritativeAuthorityHostRoutePort.SendFailure(room, code, reason, notifyAuthorityHost);
        ServerAuthoritativeAuthorityHostRoute? failureRoute = room.AuthorityRoute;
        Log.Error($"ServerAuthoritative Room failed: room={room.RoomId} session={room.SessionId} " +
            $"actor={NormalizeFailureValue(actorId)} tick={tick ?? failureRoute?.LatestAuthorityTick ?? 0} channel={NormalizeFailureValue(channel)} " +
            $"hostProduct={failureRoute?.HostProductId ?? "absent"} host={failureRoute?.HostId ?? "absent"} code={code} reason={reason}");
    }

    static void PrepareRosterLock(ServerAuthoritativeRoom room)
    {
        if (!room.PlayersById.ContainsKey(ServerAuthoritativeRoom.PlayerAId) ||
            !room.PlayersById.ContainsKey(ServerAuthoritativeRoom.PlayerBId) ||
            room.AuthorityRoute is not { IsDisposed: false } route)
        {
            throw new InvalidOperationException("Fixed two-player roster is incomplete.");
        }
        room.RosterLocked = true;
        route.LifecycleState = ServerAuthoritativeAuthorityHostLifecycleState.Active;
        room.Revision = checked(room.Revision + 1);
        long expiresAt = checked(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ServerAuthoritativeRoom.TicketLifetimeMilliseconds);
        foreach (ServerAuthoritativeRoomPlayer player in room.PlayersByActor.Values.OrderBy(value => value.ActorId, StringComparer.Ordinal))
        {
            var ticket = room.AddComponent<ServerAuthoritativeDataPlaneTicket>();
            ticket.TicketId = Guid.NewGuid().ToString("N");
            ticket.PlayerId = player.PlayerId;
            ticket.ActorId = player.ActorId;
            ticket.Nonce = Guid.NewGuid().ToString("N");
            ticket.ExpiresAtUnixMilliseconds = expiresAt;
            room.TicketsById.Add(ticket.TicketId, ticket);
            player.TicketId = ticket.TicketId;
        }
    }

    static void RouteReliableEvents(
        ServerAuthoritativeRoom room,
        string recipientActorId,
        IReadOnlyList<ServerAuthoritativeReliableGameplayEventMessage> events)
    {
        if (!room.RosterLocked || !room.PlayersByActor.TryGetValue(recipientActorId, out ServerAuthoritativeRoomPlayer? recipient) ||
            events.Count == 0)
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Reliable event batch route is invalid.", "control.reliable.authority", recipientActorId);
            return;
        }
        for (int i = 0; i < events.Count; i++)
        {
            ServerAuthoritativeReliableGameplayEventMessage value = events[i];
            if (string.IsNullOrWhiteSpace(value.ActorId) || string.IsNullOrWhiteSpace(value.EventId) ||
                value.EventSequence == 0 || value.AuthorityTick == 0 || string.IsNullOrWhiteSpace(value.EventKind) ||
                value.Payload == null || value.PayloadLength != value.Payload.Length ||
                !room.PlayersByActor.ContainsKey(value.ActorId) || string.Equals(value.ActorId, recipientActorId, StringComparison.Ordinal))
            {
                Fail(room, ServerAuthoritativeErrorCode.InvalidIdentity, "Reliable event batch contains an invalid event.", "control.reliable.authority", string.IsNullOrWhiteSpace(value.ActorId) ? "unknown" : value.ActorId, value.AuthorityTick);
                return;
            }
            room.LastReliableEventSequenceByActor.TryGetValue(value.ActorId, out ulong last);
            if (value.EventSequence <= last)
            {
                Fail(room, ServerAuthoritativeErrorCode.InvalidIdentity, $"Reliable event sequence '{value.EventSequence}' is stale for Actor '{value.ActorId}'.", "control.reliable.authority", value.ActorId, value.AuthorityTick);
                return;
            }
            room.LastReliableEventSequenceByActor[value.ActorId] = value.EventSequence;
        }
        ServerAuthoritativeAuthorityHostRoutePort.SendReliableEvents(recipient, room.RoomId, room.SessionId, events);
    }

    static void RouteFullCheckpoint(
        ServerAuthoritativeRoom room,
        string playerId,
        string actorId,
        ulong requestSequence,
        ulong authorityTick,
        ulong confirmedInputSequence,
        ulong reliableEventHorizon,
        string checkpointLayoutHash,
        string checkpointHash,
        uint checkpointLength,
        byte[] checkpoint,
        ulong snapshotSequence)
    {
        if (!room.PlayersById.TryGetValue(playerId, out ServerAuthoritativeRoomPlayer? player) ||
            !string.Equals(player.ActorId, actorId, StringComparison.Ordinal))
        {
            Fail(room, ServerAuthoritativeErrorCode.OwnerMismatch, "Authority returned a full checkpoint for an unknown owner.", "control.checkpoint.authority", actorId, authorityTick);
            return;
        }
        if (authorityTick == 0 || snapshotSequence == 0 || string.IsNullOrWhiteSpace(checkpointLayoutHash) ||
            string.IsNullOrWhiteSpace(checkpointHash) || checkpoint == null || checkpoint.Length == 0 || checkpointLength != checkpoint.Length)
        {
            Fail(
                room,
                ServerAuthoritativeErrorCode.CheckpointRejected,
                $"Authority returned an invalid full checkpoint payload: authorityTick={authorityTick};snapshotSequence={snapshotSequence};layout={checkpointLayoutHash};hash={checkpointHash};declaredLength={checkpointLength};actualLength={checkpoint?.Length ?? 0}.",
                "control.checkpoint.authority",
                actorId,
                authorityTick);
            return;
        }
        if (requestSequence == 0)
        {
            if (player.PendingCheckpointRequestSequence != 0)
            {
                Fail(room, ServerAuthoritativeErrorCode.CheckpointRejected, "Authority returned a bootstrap full checkpoint while a requested checkpoint is pending.", "control.checkpoint.authority", actorId, authorityTick);
                return;
            }
        }
        else
        {
            if (player.PendingCheckpointRequestSequence != requestSequence)
            {
                Fail(
                    room,
                    ServerAuthoritativeErrorCode.CheckpointRejected,
                    $"Authority full checkpoint request sequence does not match: expected={player.PendingCheckpointRequestSequence};actual={requestSequence}.",
                    "control.checkpoint.authority",
                    actorId,
                    authorityTick);
                return;
            }
            player.PendingCheckpointRequestSequence = 0;
        }
        ServerAuthoritativeAuthorityHostRoutePort.SendFullCheckpoint(
            player,
            room.RoomId,
            room.SessionId,
            actorId,
            authorityTick,
            confirmedInputSequence,
            reliableEventHorizon,
            checkpointLayoutHash,
            checkpointHash,
            (byte[])checkpoint.Clone(),
            snapshotSequence);
    }

    static void AcknowledgeHostHeartbeat(
        ServerAuthoritativeRoom room,
        ulong sequence,
        long sentUnixMilliseconds,
        ulong latestAuthorityTick)
    {
        if (sequence == 0 || sentUnixMilliseconds <= 0 || room.AuthorityRoute is not { IsDisposed: false } route ||
            latestAuthorityTick < route.LatestAuthorityTick)
        {
            Fail(room, ServerAuthoritativeErrorCode.InvalidIdentity, "Authority Host heartbeat is invalid or stale.", "control.heartbeat.authority", "all", latestAuthorityTick);
            return;
        }
        route.LatestAuthorityTick = latestAuthorityTick;
        ServerAuthoritativeAuthorityHostRoutePort.SendHostHeartbeatAck(
            room,
            sequence,
            sentUnixMilliseconds,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    static void MarkAuthorityTicketConsumed(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket)
    {
        if (ticket.AuthorityConsumed)
        {
            Fail(room, ServerAuthoritativeErrorCode.TicketReused, $"Authority Host reused ticket '{ticket.TicketId}'.", "control.ticket.authority", ticket.ActorId);
            return;
        }
        ticket.AuthorityConsumed = true;
    }

    static bool TryGetUsableTicket(
        ServerAuthoritativeRoom room,
        string ticketId,
        string playerId,
        out ServerAuthoritativeDataPlaneTicket ticket)
    {
        if (room.TicketsById.TryGetValue(ticketId, out ServerAuthoritativeDataPlaneTicket? value) &&
            !value.Revoked && string.Equals(value.PlayerId, playerId, StringComparison.Ordinal) &&
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= value.ExpiresAtUnixMilliseconds)
        {
            ticket = value;
            return true;
        }
        ticket = null!;
        return false;
    }

    static string ResolveActor(ServerAuthoritativeRoom room, string playerId) =>
        room.PlayersById.TryGetValue(playerId, out ServerAuthoritativeRoomPlayer? player)
            ? player.ActorId
            : "unknown";

    static string NormalizeFailureValue(string value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    static bool TryGetPlayer(
        ServerAuthoritativeRoom room,
        Session session,
        string playerId,
        out ServerAuthoritativeRoomPlayer player)
    {
        if (room.PlayersById.TryGetValue(playerId, out ServerAuthoritativeRoomPlayer? value) && value.Session == session)
        {
            player = value;
            return true;
        }
        player = null!;
        return false;
    }

    static void BindConnection(
        Session session,
        ServerAuthoritativeRoom room,
        ServerAuthoritativeProcessRole role,
        string participantId)
    {
        if (session.GetComponent<ServerAuthoritativeConnectionBinding>() != null)
            throw new InvalidOperationException("Control Session is already bound to a ServerAuthoritative participant.");
        var binding = session.AddComponent<ServerAuthoritativeConnectionBinding>();
        binding.Room = room;
        binding.ProcessRole = role;
        binding.ParticipantId = participantId;
    }

    static bool ValidateRegistration(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeAuthorityHostRegistration value,
        out string reason)
    {
        reason = string.Empty;
        IServerAuthoritativeAuthorityHostRouteAdapter adapter =
            ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Adapter;
        if (value.Endpoint == null || value.RouteKind != adapter.RouteKind ||
            !string.Equals(value.HostProductId, adapter.HostProductId, StringComparison.Ordinal) ||
            !adapter.ValidateRegistration(value, out reason))
        {
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Authority Host registration does not match the installed server product.";
            return false;
        }
        bool valid =
            string.Equals(value.RoomId, room.RoomId, StringComparison.Ordinal) &&
            value.ModelProtocolVersion == ServerAuthoritativeRoom.ModelProtocolVersion &&
            string.Equals(value.ModelId, ServerAuthoritativeRoom.ModelId, StringComparison.Ordinal) &&
            string.Equals(value.EndpointId, ServerAuthoritativeRoom.ExpectedEndpointId, StringComparison.Ordinal) &&
            string.Equals(value.AuthorityPipelineId, ServerAuthoritativeRoom.ExpectedAuthorityPipelineId, StringComparison.Ordinal) &&
            string.Equals(value.PredictionPipelineId, ServerAuthoritativeRoom.ExpectedPredictionPipelineId, StringComparison.Ordinal) &&
            string.Equals(value.BackendId, ServerAuthoritativeRoom.ExpectedBackendId, StringComparison.Ordinal) &&
            string.Equals(value.OperationSetId, ServerAuthoritativeRoom.ExpectedOperationSetId, StringComparison.Ordinal) &&
            Required(value.HostId, value.ModelConfigurationHash, value.ProgramId, value.ProgramHash, value.LayoutHash,
                value.OperationSetVersion, value.AuthorityPipelineHash, value.PredictionPipelineHash,
                value.SolverId, value.SolverVersion, value.WorldId, value.MapId, value.WorldRevision,
                value.WorldConfigurationHash, value.NavigationSurfaceArtifactHash, value.QueryProfileHash,
                value.DataHost) &&
            value.TickRate > 0 && value.DataPort is > 0 and <= 65535;
        if (!valid)
        {
            reason = "Authority Host registration identity is incomplete or incompatible with the Room contract.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    static bool ValidateClientIdentity(
        ServerAuthoritativeRoom room,
        C2G_ServerAuthoritativeClientJoinRequest request,
        ServerAuthoritativeAuthorityHostRoute route,
        out string reason)
    {
        ServerAuthoritativeProtocolIdentityMessage? protocol = request.Protocol;
        ServerAuthoritativeProgramIdentityMessage? program = request.Program;
        ServerAuthoritativeWorldIdentityMessage? world = request.PredictionWorld;
        bool valid = protocol != null && program != null && world != null &&
            protocol.ModelProtocolVersion == ServerAuthoritativeRoom.ModelProtocolVersion &&
            string.Equals(protocol.ModelId, ServerAuthoritativeRoom.ModelId, StringComparison.Ordinal) &&
            string.Equals(protocol.ModelConfigurationHash, route.ModelConfigurationHash, StringComparison.Ordinal) &&
            string.Equals(protocol.EndpointId, route.EndpointId, StringComparison.Ordinal) &&
            string.Equals(program.ProgramId, route.ProgramId, StringComparison.Ordinal) &&
            string.Equals(program.ProgramHash, route.ProgramHash, StringComparison.Ordinal) &&
            string.Equals(program.LayoutHash, route.LayoutHash, StringComparison.Ordinal) &&
            string.Equals(program.OperationSetId, route.OperationSetId, StringComparison.Ordinal) &&
            string.Equals(program.OperationSetVersion, route.OperationSetVersion, StringComparison.Ordinal) &&
            string.Equals(request.PredictionPipelineId, route.PredictionPipelineId, StringComparison.Ordinal) &&
            string.Equals(request.PredictionPipelineHash, route.PredictionPipelineHash, StringComparison.Ordinal) &&
            PredictionWorldMatches(route, world);
        reason = valid ? string.Empty : "Client Program, Pipeline, prediction Solver capability, or World contract does not match the locked Authority Host.";
        return valid;
    }

    static bool PredictionWorldMatches(ServerAuthoritativeAuthorityHostRoute route, ServerAuthoritativeWorldIdentityMessage world) =>
        Required(world.SolverId, world.SolverVersion, world.WorldConfigurationHash,
            world.NavigationSurfaceArtifactHash, world.QueryProfileHash) &&
        (world.SolverCapabilities & (ulong)ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities) ==
            (ulong)ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities &&
        string.Equals(world.WorldId, route.WorldId, StringComparison.Ordinal) &&
        string.Equals(world.MapId, route.MapId, StringComparison.Ordinal) &&
        string.Equals(world.WorldRevision, route.WorldRevision, StringComparison.Ordinal);

    static bool ResolveClientIdentity(
        string playerId,
        int processRole,
        out string actorId,
        out ServerAuthoritativeProcessRole role,
        out string reason)
    {
        role = (ServerAuthoritativeProcessRole)processRole;
        if (string.Equals(playerId, ServerAuthoritativeRoom.PlayerAId, StringComparison.Ordinal) &&
            role == ServerAuthoritativeProcessRole.ClientA)
        {
            actorId = ServerAuthoritativeRoom.ActorAId;
            reason = string.Empty;
            return true;
        }
        if (string.Equals(playerId, ServerAuthoritativeRoom.PlayerBId, StringComparison.Ordinal) &&
            role == ServerAuthoritativeProcessRole.ClientB)
        {
            actorId = ServerAuthoritativeRoom.ActorBId;
            reason = string.Empty;
            return true;
        }
        actorId = string.Empty;
        reason = $"Player '{playerId}' does not match process role '{processRole}'.";
        return false;
    }

    static bool CanMutate(ServerAuthoritativeRoom room, out string reason)
    {
        if (room == null || room.IsDisposed || room.Terminating || room.Failed)
        {
            reason = "Room is unavailable or failed.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    static bool MatchesSession(ServerAuthoritativeRoom room, string sessionId) =>
        !string.IsNullOrWhiteSpace(room.SessionId) && string.Equals(room.SessionId, sessionId, StringComparison.Ordinal);

    static bool Required(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                return false;
        }
        return true;
    }

    public static List<ServerAuthoritativeReliableGameplayEventMessage> CloneEvents(
        IReadOnlyList<ServerAuthoritativeReliableGameplayEventMessage> source)
    {
        var values = new List<ServerAuthoritativeReliableGameplayEventMessage>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            ServerAuthoritativeReliableGameplayEventMessage value = source[i];
            values.Add(new ServerAuthoritativeReliableGameplayEventMessage
            {
                ActorId = value.ActorId,
                EventId = value.EventId,
                EventSequence = value.EventSequence,
                AuthorityTick = value.AuthorityTick,
                EventKind = value.EventKind,
                PayloadSchemaVersion = value.PayloadSchemaVersion,
                PayloadLength = value.PayloadLength,
                Payload = value.Payload == null ? Array.Empty<byte>() : (byte[])value.Payload.Clone()
            });
        }
        return values;
    }
}
