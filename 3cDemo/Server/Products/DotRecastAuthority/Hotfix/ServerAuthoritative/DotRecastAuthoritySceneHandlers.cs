using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class G2A_ServerAuthoritativeAuthoritySceneRosterLockHandler :
    Address<DotRecastAuthorityHost, G2A_ServerAuthoritativeAuthoritySceneRosterLock>
{
    protected override async FTask Run(DotRecastAuthorityHost host, G2A_ServerAuthoritativeAuthoritySceneRosterLock message)
    {
        DotRecastAuthoritySceneHandlerHelper.RequireControl(host).ReceiveRoster(message);
        await FTask.CompletedTask;
    }
}

public sealed class G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicketHandler :
    Address<DotRecastAuthorityHost, G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket>
{
    protected override async FTask Run(DotRecastAuthorityHost host, G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket message)
    {
        DotRecastAuthoritySceneHandlerHelper.RequireControl(host).ReceiveTicket(message);
        await FTask.CompletedTask;
    }
}

public sealed class G2A_ServerAuthoritativeAuthoritySceneHeartbeatAckHandler :
    Address<DotRecastAuthorityHost, G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck>
{
    protected override async FTask Run(DotRecastAuthorityHost host, G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck message)
    {
        DotRecastAuthoritySceneHandlerHelper.RequireControl(host).ReceiveHeartbeatAck(message);
        await FTask.CompletedTask;
    }
}

public sealed class G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequestHandler :
    Address<DotRecastAuthorityHost, G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest>
{
    protected override async FTask Run(DotRecastAuthorityHost host, G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest message)
    {
        DotRecastAuthoritySceneHandlerHelper.RequireControl(host).ReceiveFullCheckpointRequest(message);
        await FTask.CompletedTask;
    }
}

public sealed class G2A_ServerAuthoritativeAuthoritySceneFailureHandler :
    Address<DotRecastAuthorityHost, G2A_ServerAuthoritativeAuthoritySceneFailure>
{
    protected override async FTask Run(DotRecastAuthorityHost host, G2A_ServerAuthoritativeAuthoritySceneFailure message)
    {
        DotRecastAuthoritySceneHandlerHelper.RequireControl(host).ReceiveFailure(message);
        host.Failed = true;
        host.FailureReason = message.Reason;
        host.Runtime?.Dispose();
        await FTask.CompletedTask;
    }
}

static class DotRecastAuthoritySceneHandlerHelper
{
    public static DotRecastAuthoritySceneControlTransport RequireControl(DotRecastAuthorityHost host)
    {
        if (host == null || host.IsDisposed || host.Failed ||
            host.ControlTransport is not DotRecastAuthoritySceneControlTransport control)
        {
            throw new InvalidOperationException("DotRecast Authority Host control adapter is unavailable.");
        }
        return control;
    }
}
