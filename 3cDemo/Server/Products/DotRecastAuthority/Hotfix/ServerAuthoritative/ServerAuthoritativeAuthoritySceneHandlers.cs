using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class A2G_ServerAuthoritativeAuthoritySceneRegisterRequestHandler :
    AddressRPC<Scene, A2G_ServerAuthoritativeAuthoritySceneRegisterRequest, G2A_ServerAuthoritativeAuthoritySceneRegisterResponse>
{
    protected override async FTask Run(
        Scene scene,
        A2G_ServerAuthoritativeAuthoritySceneRegisterRequest request,
        G2A_ServerAuthoritativeAuthoritySceneRegisterResponse response,
        Action reply)
    {
        string roomId = request.Host?.RoomId ?? string.Empty;
        ServerAuthoritativeErrorCode code;
        string reason;
        if (!ServerAuthoritativeRoomRuntime.TryGetRoom(scene, roomId, out ServerAuthoritativeRoom room, out reason))
        {
            code = ServerAuthoritativeErrorCode.InvalidIdentity;
        }
        else if (!DotRecastAuthorityRegistrationMapper.TryBuild(scene, request, out ServerAuthoritativeAuthorityHostRegistration registration, out reason))
        {
            code = ServerAuthoritativeErrorCode.InvalidIdentity;
        }
        else
        {
            code = ServerAuthoritativeRoomRuntime.RegisterHost(room, registration, out reason);
        }
        response.ErrorCode = (uint)code;
        response.ResultCode = (int)code;
        response.RoomRevision = room?.Revision ?? 0;
        response.SessionId = room?.SessionId ?? string.Empty;
        response.FailureReason = reason;
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneTicketConsumedHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneTicketConsumed>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneTicketConsumed message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.MarkAuthorityHostTicketConsumed(room, message.SessionId, message.TicketId, message.PlayerId);
        else
            Log.Info($"Rejected DotRecast Authority ticket confirmation: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneHeartbeatHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneHeartbeat>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneHeartbeat message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason))
            ServerAuthoritativeRoomRuntime.AcknowledgeAuthorityHostHeartbeat(room, message.SessionId, message.Sequence, message.SentUnixMilliseconds, message.LatestAuthorityTick);
        else
            Log.Info($"Rejected DotRecast Authority heartbeat: {reason}");
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatchHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason))
        {
            var events = new List<ServerAuthoritativeReliableGameplayEventMessage>(message.Events.Count);
            for (int i = 0; i < message.Events.Count; i++)
            {
                ServerAuthoritativeInnerReliableGameplayEvent value = message.Events[i];
                events.Add(new ServerAuthoritativeReliableGameplayEventMessage
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
            ServerAuthoritativeRoomRuntime.RouteAuthorityReliableEvents(room, message.SessionId, message.RecipientActorId, events);
        }
        else
        {
            Log.Info($"Rejected DotRecast Authority reliable event batch: {reason}");
        }
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponseHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason))
        {
            ServerAuthoritativeRoomRuntime.RouteAuthorityFullCheckpoint(
                room, message.SessionId, message.PlayerId, message.ActorId, message.RequestSequence,
                message.AuthorityTick, message.ConfirmedInputSequence, message.ReliableEventHorizon,
                message.CheckpointLayoutHash, message.CheckpointHash, message.CheckpointLength,
                message.Checkpoint, message.SnapshotSequence);
        }
        else
        {
            Log.Info($"Rejected DotRecast Authority full checkpoint: {reason}");
        }
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneLeaveHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneLeave>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneLeave message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason) &&
            string.Equals(room.SessionId, message.SessionId, StringComparison.Ordinal))
        {
            ServerAuthoritativeRoomRuntime.Fail(room, ServerAuthoritativeErrorCode.SessionClosed,
                $"DotRecast Authority Scene '{message.Host.HostId}' left: {message.Reason}",
                "control.leave.authority", "all", notifyAuthorityHost: false);
        }
        else
        {
            Log.Info($"Rejected DotRecast Authority leave: {reason}");
        }
        await FTask.CompletedTask;
    }
}

public sealed class A2G_ServerAuthoritativeAuthoritySceneFailureHandler : Address<Scene, A2G_ServerAuthoritativeAuthoritySceneFailure>
{
    protected override async FTask Run(Scene scene, A2G_ServerAuthoritativeAuthoritySceneFailure message)
    {
        if (DotRecastAuthorityGateRouteHandlerHelper.TryGetRoom(scene, message.Host, message.AuthorityAddress, out ServerAuthoritativeRoom room, out string reason) &&
            string.Equals(room.SessionId, message.SessionId, StringComparison.Ordinal))
        {
            ServerAuthoritativeRoomRuntime.Fail(room, ServerAuthoritativeErrorCode.SessionClosed,
                $"DotRecast Authority Scene reported '{message.Code}': {message.Reason}",
                "authority.runtime", "all", notifyAuthorityHost: false);
        }
        else
        {
            Log.Info($"Rejected DotRecast Authority failure: {reason}");
        }
        await FTask.CompletedTask;
    }
}

static class DotRecastAuthorityGateRouteHandlerHelper
{
    public static bool TryGetRoom(
        Scene scene,
        ServerAuthoritativeInnerHostIdentity? host,
        long authorityAddress,
        out ServerAuthoritativeRoom room,
        out string reason)
    {
        if (host == null)
        {
            room = null!;
            reason = "DotRecast Authority Host identity is absent.";
            return false;
        }
        if (!ServerAuthoritativeRoomRuntime.TryGetRoom(scene, host.RoomId, out room, out reason))
            return false;
        if (DotRecastAuthorityHostEndpoint.Matches(room, scene, authorityAddress, host))
            return true;
        reason = "DotRecast Authority Scene does not own the active Room route.";
        return false;
    }
}

static class DotRecastAuthorityRegistrationMapper
{
    public static bool TryBuild(
        Scene scene,
        A2G_ServerAuthoritativeAuthoritySceneRegisterRequest request,
        out ServerAuthoritativeAuthorityHostRegistration registration,
        out string reason)
    {
        registration = null!;
        if (request.Host == null || request.Protocol == null || request.Program == null ||
            request.AuthorityPipeline == null || request.DataEndpoint == null || request.World == null)
        {
            reason = "DotRecast Authority Scene register message is incomplete.";
            return false;
        }
        registration = ServerAuthoritativeRegistrationMapper.Build(
            new DotRecastAuthorityHostEndpoint(scene, request.AuthorityAddress, request.Host.HostId),
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
