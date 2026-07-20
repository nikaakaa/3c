using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class W2G_ServerAuthoritativeAuthorityRegisterRequestHandler :
    MessageRPC<W2G_ServerAuthoritativeAuthorityRegisterRequest, G2W_ServerAuthoritativeAuthorityRegisterResponse>
{
    protected override async FTask Run(
        Session session,
        W2G_ServerAuthoritativeAuthorityRegisterRequest request,
        G2W_ServerAuthoritativeAuthorityRegisterResponse response,
        Action reply)
    {
        ServerAuthoritativeErrorCode code;
        string reason;
        if (!ServerAuthoritativeRoomRuntime.TryGetRoom(session, request.RoomId, out ServerAuthoritativeRoom room, out reason))
        {
            code = ServerAuthoritativeErrorCode.InvalidIdentity;
        }
        else if (request.ProcessRole != (int)ServerAuthoritativeProcessRole.AuthorityWorker ||
                 !UnityAuthorityRegistrationMapper.TryBuild(session, request, out ServerAuthoritativeAuthorityHostRegistration registration, out reason))
        {
            code = ServerAuthoritativeErrorCode.InvalidIdentity;
        }
        else
        {
            code = ServerAuthoritativeRoomRuntime.RegisterHost(room, registration, out reason);
            if (code == ServerAuthoritativeErrorCode.Success)
                ServerAuthoritativeRoomRuntime.BindAuthorityControlSession(room, session, registration.HostId);
        }
        response.ErrorCode = (uint)code;
        response.ResultCode = (int)code;
        response.RoomRevision = room?.Revision ?? 0;
        response.SessionId = room?.SessionId ?? string.Empty;
        response.FailureReason = reason;
        await FTask.CompletedTask;
    }
}

public sealed class W2G_ServerAuthoritativeDataPlaneTicketConsumedHandler : Message<W2G_ServerAuthoritativeDataPlaneTicketConsumed>
{
    protected override async FTask Run(Session session, W2G_ServerAuthoritativeDataPlaneTicketConsumed message)
    {
        if (UnityAuthorityHandlerHelper.TryGetRoom(session, message.RoomId, message.HostId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.MarkAuthorityHostTicketConsumed(room, message.SessionId, message.TicketId, message.PlayerId);
        else
            Log.Info($"Rejected Unity Authority ticket confirmation: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class W2G_ServerAuthoritativeControlHeartbeatHandler : Message<W2G_ServerAuthoritativeControlHeartbeat>
{
    protected override async FTask Run(Session session, W2G_ServerAuthoritativeControlHeartbeat message)
    {
        if (UnityAuthorityHandlerHelper.TryGetRoom(session, message.RoomId, message.HostId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.AcknowledgeAuthorityHostHeartbeat(room, message.SessionId, message.Sequence, message.ClientUnixMilliseconds, room.AuthorityRoute?.LatestAuthorityTick ?? 0);
        else
            Log.Info($"Rejected Unity Authority heartbeat: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class W2G_ServerAuthoritativeReliableGameplayEventBatchHandler : Message<W2G_ServerAuthoritativeReliableGameplayEventBatch>
{
    protected override async FTask Run(Session session, W2G_ServerAuthoritativeReliableGameplayEventBatch message)
    {
        string hostId = session.GetComponent<ServerAuthoritativeConnectionBinding>()?.ParticipantId ?? string.Empty;
        if (UnityAuthorityHandlerHelper.TryGetRoom(session, message.RoomId, hostId, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.RouteAuthorityReliableEvents(room, message.SessionId, message.RecipientActorId, ServerAuthoritativeRoomRuntime.CloneEvents(message.Events));
        else
            Log.Info($"Rejected Unity Authority reliable event batch: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class W2G_ServerAuthoritativeFullCheckpointResponseHandler : Message<W2G_ServerAuthoritativeFullCheckpointResponse>
{
    protected override async FTask Run(Session session, W2G_ServerAuthoritativeFullCheckpointResponse message)
    {
        string hostId = session.GetComponent<ServerAuthoritativeConnectionBinding>()?.ParticipantId ?? string.Empty;
        if (UnityAuthorityHandlerHelper.TryGetRoom(session, message.RoomId, hostId, out ServerAuthoritativeRoom room, out string reason))
        {
            ServerAuthoritativeRoomRuntime.RouteAuthorityFullCheckpoint(
                room, message.SessionId, message.PlayerId, message.ActorId, message.RequestSequence,
                message.AuthorityTick, message.ConfirmedInputSequence, message.ReliableEventHorizon,
                message.CheckpointLayoutHash, message.CheckpointHash, message.CheckpointLength,
                message.Checkpoint, message.SnapshotSequence);
        }
        else
        {
            Log.Info($"Rejected Unity Authority full checkpoint: {reason}");
        }
        await FTask.CompletedTask;
    }
}

public sealed class W2G_ServerAuthoritativeLeaveHandler : Message<W2G_ServerAuthoritativeLeave>
{
    protected override async FTask Run(Session session, W2G_ServerAuthoritativeLeave message)
    {
        if (UnityAuthorityHandlerHelper.TryGetRoom(session, message.RoomId, message.HostId, out ServerAuthoritativeRoom room, out string reason) &&
            string.Equals(room.SessionId, message.SessionId, StringComparison.Ordinal))
        {
            ServerAuthoritativeRoomRuntime.Fail(room, ServerAuthoritativeErrorCode.SessionClosed,
                $"Unity Authority Worker '{message.HostId}' left: {message.Reason}",
                "control.leave.authority", "all", notifyAuthorityHost: false);
        }
        else
        {
            Log.Info($"Rejected Unity Authority leave: {reason}");
        }
        await FTask.CompletedTask;
    }
}

static class UnityAuthorityHandlerHelper
{
    public static bool TryGetRoom(
        Session session,
        string roomId,
        string hostId,
        out ServerAuthoritativeRoom room,
        out string reason)
    {
        if (!ServerAuthoritativeRoomRuntime.TryGetRoom(session, roomId, out room, out reason))
            return false;
        if (UnityAuthorityHostEndpoint.Matches(room, session, hostId))
            return true;
        reason = "Unity Authority Worker Session does not own the active Room route.";
        return false;
    }
}

static class UnityAuthorityRegistrationMapper
{
    public static bool TryBuild(
        Session session,
        W2G_ServerAuthoritativeAuthorityRegisterRequest request,
        out ServerAuthoritativeAuthorityHostRegistration registration,
        out string reason)
    {
        registration = null!;
        if (request.Host == null || request.Protocol == null || request.Program == null ||
            request.AuthorityPipeline == null || request.DataEndpoint == null || request.World == null)
        {
            reason = "Unity Authority Worker register message is incomplete.";
            return false;
        }
        registration = ServerAuthoritativeRegistrationMapper.Build(
            new UnityAuthorityHostEndpoint(session, request.Host.HostId),
            request.Host.HostProductId, request.Host.HostId,
            (ServerAuthoritativeAuthorityHostRouteKind)request.Host.RouteKind, request.Host.RoomId,
            request.Protocol.ModelProtocolVersion, request.Protocol.ModelId, request.Protocol.ModelConfigurationHash,
            request.Protocol.EndpointId, request.Program.ProgramId, request.Program.ProgramHash, request.Program.LayoutHash,
            request.Program.OperationSetId, request.Program.OperationSetVersion,
            request.AuthorityPipeline.PipelineId, request.AuthorityPipeline.PipelineHash,
            request.PredictionPipelineId, request.PredictionPipelineHash,
            request.AuthorityPipeline.BackendId, request.AuthorityPipeline.TickRate,
            request.World.SolverId, request.World.SolverVersion, request.World.SolverCapabilities, request.World.SolverFeatures,
            request.World.WorldId, request.World.MapId, request.World.WorldRevision, request.World.WorldConfigurationHash,
            request.World.NavigationSurfaceArtifactHash, request.World.QueryProfileHash,
            request.DataEndpoint.Host, request.DataEndpoint.Port);
        reason = string.Empty;
        return true;
    }
}
