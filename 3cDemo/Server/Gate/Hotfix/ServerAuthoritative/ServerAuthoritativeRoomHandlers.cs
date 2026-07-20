using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_ServerAuthoritativeClientJoinRequestHandler :
    MessageRPC<C2G_ServerAuthoritativeClientJoinRequest, G2C_ServerAuthoritativeClientJoinResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_ServerAuthoritativeClientJoinRequest request,
        G2C_ServerAuthoritativeClientJoinResponse response,
        Action reply)
    {
        ServerAuthoritativeErrorCode code;
        ServerAuthoritativeRoomPlayer? player = null;
        string reason;
        if (!ServerAuthoritativeRoomRuntime.TryGetRoom(session, request.RoomId, out ServerAuthoritativeRoom room, out reason))
        {
            code = ServerAuthoritativeErrorCode.InvalidIdentity;
        }
        else
        {
            code = ServerAuthoritativeRoomRuntime.JoinClient(room, session, request, out player, out reason);
        }
        response.ErrorCode = (uint)code;
        response.ResultCode = (int)code;
        response.SessionId = room?.SessionId ?? string.Empty;
        response.OwnedActorId = player?.ActorId ?? string.Empty;
        response.Roster = room == null ? new ServerAuthoritativeRosterMessage() : ServerAuthoritativeRoomRuntime.BuildRoster(room);
        response.LatestAuthorityTick = room?.AuthorityRoute?.LatestAuthorityTick ?? 0;
        response.FailureReason = reason;
        response.AuthorityHost = room?.AuthorityRoute == null
            ? new ServerAuthoritativeAuthorityHostIdentityMessage()
            : ServerAuthoritativeAuthorityHostRoutePort.BuildOuterHost(room.AuthorityRoute, room.RoomId);
        response.AuthorityWorld = room?.AuthorityRoute == null
            ? new ServerAuthoritativeWorldIdentityMessage()
            : ServerAuthoritativeAuthorityHostRoutePort.BuildOuterWorld(room.AuthorityRoute);
        response.AuthorityPipeline = room?.AuthorityRoute == null
            ? new ServerAuthoritativePipelineIdentityMessage()
            : ServerAuthoritativeAuthorityHostRoutePort.BuildOuterPipeline(room.AuthorityRoute);
        await FTask.CompletedTask;
    }
}

public sealed class C2G_ServerAuthoritativeClientJoinAcceptedHandler : Message<C2G_ServerAuthoritativeClientJoinAccepted>
{
    protected override async FTask Run(Session session, C2G_ServerAuthoritativeClientJoinAccepted message)
    {
        if (ServerAuthoritativeRoomRuntime.TryGetRoom(session, message.RoomId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.AcceptClientJoin(room, session, message);
        else
            Log.Info($"Rejected Client join acceptance: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class C2G_ServerAuthoritativeDataPlaneTicketConsumedHandler : Message<C2G_ServerAuthoritativeDataPlaneTicketConsumed>
{
    protected override async FTask Run(Session session, C2G_ServerAuthoritativeDataPlaneTicketConsumed message)
    {
        if (ServerAuthoritativeRoomRuntime.TryGetRoom(session, message.RoomId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.MarkClientTicketConsumed(room, session, message);
        else
            Log.Info($"Rejected Client data-plane ticket confirmation: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class C2G_ServerAuthoritativeControlHeartbeatHandler : Message<C2G_ServerAuthoritativeControlHeartbeat>
{
    protected override async FTask Run(Session session, C2G_ServerAuthoritativeControlHeartbeat message)
    {
        if (ServerAuthoritativeRoomRuntime.TryGetRoom(session, message.RoomId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.AcknowledgeClientHeartbeat(room, session, message);
        else
            Log.Info($"Rejected Client control heartbeat: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class C2G_ServerAuthoritativeFullCheckpointRequestHandler : Message<C2G_ServerAuthoritativeFullCheckpointRequest>
{
    protected override async FTask Run(Session session, C2G_ServerAuthoritativeFullCheckpointRequest message)
    {
        if (ServerAuthoritativeRoomRuntime.TryGetRoom(session, message.RoomId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.RequestFullCheckpoint(room, session, message);
        else
            Log.Info($"Rejected full checkpoint request: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class C2G_ServerAuthoritativeLeaveHandler : Message<C2G_ServerAuthoritativeLeave>
{
    protected override async FTask Run(Session session, C2G_ServerAuthoritativeLeave message)
    {
        if (ServerAuthoritativeRoomRuntime.TryGetRoom(session, message.RoomId, out ServerAuthoritativeRoom room, out string reason) &&
            string.Equals(room.SessionId, message.SessionId, StringComparison.Ordinal) &&
            room.PlayersById.TryGetValue(message.PlayerId, out ServerAuthoritativeRoomPlayer? player) && player.Session == session)
        {
            ServerAuthoritativeRoomRuntime.Fail(
                room,
                ServerAuthoritativeErrorCode.SessionClosed,
                $"Player '{message.PlayerId}' left: {message.Reason}",
                "control.leave.client",
                player.ActorId);
        }
        else
        {
            Log.Info($"Rejected ServerAuthoritative Client leave: {reason}");
        }
        await FTask.CompletedTask;
    }
}
