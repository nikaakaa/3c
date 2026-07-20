using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;

namespace Fantasy;

public sealed class ServerAuthoritativeGateSceneCreated : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        Scene scene = self.Scene;
        if (scene.SceneType != SceneType.Gate)
            return;
        var registry = scene.AddComponent<ServerAuthoritativeRoomRegistry>();
        registry.Room = registry.AddComponent<ServerAuthoritativeRoom>();
        Log.Info($"ServerAuthoritative Room '{ServerAuthoritativeRoom.DemoRoomId}' created in Gate Scene '{scene.Id}'.");
        await FTask.CompletedTask;
    }
}

public sealed class ServerAuthoritativeRoomRegistryDestroySystem : DestroySystem<ServerAuthoritativeRoomRegistry>
{
    protected override void Destroy(ServerAuthoritativeRoomRegistry self) => self.Room = null;
}

public sealed class ServerAuthoritativeRoomDestroySystem : DestroySystem<ServerAuthoritativeRoom>
{
    protected override void Destroy(ServerAuthoritativeRoom self)
    {
        self.Terminating = true;
        ServerAuthoritativeAuthorityHostRoutePort.ReleaseHost(self);
        self.SessionId = string.Empty;
        self.RosterLocked = false;
        self.Failed = false;
        self.FullCheckpointRequestSequence = 0;
        self.AuthorityRoute = null;
        self.PlayersById.Clear();
        self.PlayersByActor.Clear();
        self.TicketsById.Clear();
        self.LastReliableEventSequenceByActor.Clear();
        self.Terminating = false;
    }
}

public sealed class ServerAuthoritativeAuthorityHostRouteDestroySystem : DestroySystem<ServerAuthoritativeAuthorityHostRoute>
{
    protected override void Destroy(ServerAuthoritativeAuthorityHostRoute self)
    {
        self.LifecycleState = ServerAuthoritativeAuthorityHostLifecycleState.Closed;
        self.Endpoint = null;
        self.HostProductId = string.Empty;
        self.HostId = string.Empty;
        self.RouteKind = ServerAuthoritativeAuthorityHostRouteKind.None;
    }
}

public sealed class ServerAuthoritativeRoomUpdateSystem : UpdateSystem<ServerAuthoritativeRoom>
{
    protected override void Update(ServerAuthoritativeRoom self)
    {
        if (self.Failed || self.Terminating || !self.RosterLocked)
            return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (ServerAuthoritativeDataPlaneTicket ticket in self.TicketsById.Values)
        {
            if (ticket.Revoked || ticket.AuthorityConsumed && ticket.ClientConsumed)
                continue;
            if (now <= ticket.ExpiresAtUnixMilliseconds)
                continue;
            ServerAuthoritativeRoomRuntime.Fail(
                self,
                ServerAuthoritativeErrorCode.TicketExpired,
                $"Data-plane ticket '{ticket.TicketId}' expired before both endpoints confirmed it.",
                "control.ticket.timeout",
                ticket.ActorId);
            return;
        }
    }
}

public sealed class ServerAuthoritativeRoomPlayerDestroySystem : DestroySystem<ServerAuthoritativeRoomPlayer>
{
    protected override void Destroy(ServerAuthoritativeRoomPlayer self)
    {
        self.PlayerId = string.Empty;
        self.ActorId = string.Empty;
        self.ProcessRole = ServerAuthoritativeProcessRole.None;
        self.Session = null;
        self.TicketId = string.Empty;
        self.PendingCheckpointRequestSequence = 0;
    }
}

public sealed class ServerAuthoritativeDataPlaneTicketDestroySystem : DestroySystem<ServerAuthoritativeDataPlaneTicket>
{
    protected override void Destroy(ServerAuthoritativeDataPlaneTicket self)
    {
        self.TicketId = string.Empty;
        self.PlayerId = string.Empty;
        self.ActorId = string.Empty;
        self.Nonce = string.Empty;
        self.ExpiresAtUnixMilliseconds = 0;
        self.AuthorityConsumed = false;
        self.ClientConsumed = false;
        self.Revoked = false;
    }
}

public sealed class ServerAuthoritativeConnectionBindingDestroySystem : DestroySystem<ServerAuthoritativeConnectionBinding>
{
    protected override void Destroy(ServerAuthoritativeConnectionBinding self)
    {
        ServerAuthoritativeRoom? room = self.Room;
        string participant = self.ParticipantId;
        self.Room = null;
        self.ProcessRole = ServerAuthoritativeProcessRole.None;
        self.ParticipantId = string.Empty;
        if (room is { IsDisposed: false, Terminating: false, Failed: false })
        {
            string actorId = room.PlayersById.TryGetValue(participant, out ServerAuthoritativeRoomPlayer? player)
                ? player.ActorId
                : "all";
            ServerAuthoritativeRoomRuntime.Fail(
                room,
                ServerAuthoritativeErrorCode.SessionClosed,
                $"Participant '{participant}' control connection closed.",
                "control.connection",
                actorId);
        }
    }
}
