using ThirdPersonSimulation.DotRecastAuthority;

namespace Fantasy;

public sealed class DotRecastAuthorityHostEndpoint : IServerAuthoritativeAuthorityHostEndpoint
{
    public DotRecastAuthorityHostEndpoint(Scene gateScene, long authorityAddress, string hostId)
    {
        GateScene = gateScene is { IsDisposed: false }
            ? gateScene
            : throw new ArgumentException("DotRecast Gate Scene is unavailable.", nameof(gateScene));
        AuthorityAddress = authorityAddress != 0
            ? authorityAddress
            : throw new ArgumentOutOfRangeException(nameof(authorityAddress));
        EndpointIdentity = string.IsNullOrWhiteSpace(hostId)
            ? throw new ArgumentException("DotRecast Authority HostId is required.", nameof(hostId))
            : hostId.Trim();
    }

    public Scene GateScene { get; }
    public long AuthorityAddress { get; }
    public string EndpointIdentity { get; }

    public static bool Matches(
        ServerAuthoritativeRoom room,
        Scene gateScene,
        long authorityAddress,
        ServerAuthoritativeInnerHostIdentity? host) =>
        host != null &&
        room.AuthorityRoute is { IsDisposed: false, Endpoint: DotRecastAuthorityHostEndpoint endpoint } route &&
        endpoint.GateScene == gateScene && endpoint.AuthorityAddress == authorityAddress &&
        string.Equals(endpoint.EndpointIdentity, host.HostId, StringComparison.Ordinal) &&
        string.Equals(route.HostProductId, host.HostProductId, StringComparison.Ordinal) &&
        string.Equals(route.HostId, host.HostId, StringComparison.Ordinal) &&
        string.Equals(room.RoomId, host.RoomId, StringComparison.Ordinal) &&
        host.RouteKind == (int)route.RouteKind;
}

public sealed class DotRecastAuthorityHostRouteAdapter : IServerAuthoritativeAuthorityHostRouteAdapter
{
    public string ProductId => DotRecastAuthorityHostProduct.ServerProductId;
    public ServerAuthoritativeAuthorityHostRouteKind RouteKind => ServerAuthoritativeAuthorityHostRouteKind.InProcessAuthorityScene;
    public string HostProductId => DotRecastAuthorityHostProduct.ProductId.Value;

    public bool ValidateRegistration(ServerAuthoritativeAuthorityHostRegistration registration, out string reason)
    {
        bool valid = registration.Endpoint is DotRecastAuthorityHostEndpoint endpoint &&
            endpoint.GateScene is { IsDisposed: false } && endpoint.AuthorityAddress != 0 &&
            string.Equals(endpoint.EndpointIdentity, registration.HostId, StringComparison.Ordinal) &&
            string.Equals(registration.SolverId, DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverId.Value, StringComparison.Ordinal) &&
            string.Equals(registration.SolverVersion, DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverVersion, StringComparison.Ordinal) &&
            registration.SolverCapabilities == (ulong)DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverCapabilities &&
            registration.SolverFeatures == (ulong)DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverFeatures;
        reason = valid ? string.Empty : "DotRecast Authority registration does not match its Product-owned Scene endpoint and Solver declaration.";
        return valid;
    }

    public void SendRoster(ServerAuthoritativeRoom room, ServerAuthoritativeRosterMessage roster)
    {
        DotRecastAuthorityHostEndpoint endpoint = Require(room);
        var message = new G2A_ServerAuthoritativeAuthoritySceneRosterLock
        {
            Host = BuildHost(room),
            SessionId = room.SessionId,
            RoomRevision = room.Revision
        };
        for (int i = 0; i < roster.Members.Count; i++)
        {
            ServerAuthoritativeRosterMemberMessage member = roster.Members[i];
            message.Members.Add(new ServerAuthoritativeInnerRosterMember
            {
                PlayerId = member.PlayerId,
                ActorId = member.ActorId,
                ProcessRole = member.ProcessRole
            });
        }
        endpoint.GateScene.Send(endpoint.AuthorityAddress, message);
    }

    public void SendTicket(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeDataPlaneTicket ticket,
        ServerAuthoritativeDataPlaneTicketMessage outerTicket)
    {
        DotRecastAuthorityHostEndpoint endpoint = Require(room);
        endpoint.GateScene.Send(endpoint.AuthorityAddress, new G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket
        {
            Ticket = new ServerAuthoritativeInnerDataPlaneTicket
            {
                TicketId = ticket.TicketId,
                RoomId = room.RoomId,
                SessionId = room.SessionId,
                PlayerId = ticket.PlayerId,
                ActorId = ticket.ActorId,
                Host = BuildHost(room),
                Nonce = ticket.Nonce,
                ExpiresAtUnixMilliseconds = ticket.ExpiresAtUnixMilliseconds
            }
        });
    }

    public void SendHeartbeatAck(
        ServerAuthoritativeRoom room,
        ulong sequence,
        long sentUnixMilliseconds,
        long serverUnixMilliseconds)
    {
        DotRecastAuthorityHostEndpoint endpoint = Require(room);
        endpoint.GateScene.Send(endpoint.AuthorityAddress, new G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck
        {
            Host = BuildHost(room),
            SessionId = room.SessionId,
            Sequence = sequence,
            SentUnixMilliseconds = sentUnixMilliseconds,
            ServerUnixMilliseconds = serverUnixMilliseconds
        });
    }

    public void SendFullCheckpointRequest(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeRoomPlayer player,
        ulong requestSequence,
        ulong lastUsableSnapshotSequence,
        string reason)
    {
        DotRecastAuthorityHostEndpoint endpoint = Require(room);
        endpoint.GateScene.Send(endpoint.AuthorityAddress, new G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest
        {
            Host = BuildHost(room),
            SessionId = room.SessionId,
            PlayerId = player.PlayerId,
            ActorId = player.ActorId,
            RequestSequence = requestSequence
        });
    }

    public void SendFailure(ServerAuthoritativeRoom room, ServerAuthoritativeErrorCode code, string reason)
    {
        DotRecastAuthorityHostEndpoint endpoint = Require(room);
        endpoint.GateScene.Send(endpoint.AuthorityAddress, new G2A_ServerAuthoritativeAuthoritySceneFailure
        {
            Host = BuildHost(room),
            SessionId = room.SessionId,
            ResultCode = (int)code,
            Reason = reason
        });
    }

    public void SendTicketRevoked(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket, string reason)
    {
    }

    public void ReleaseHost(ServerAuthoritativeRoom room)
    {
    }

    static DotRecastAuthorityHostEndpoint Require(ServerAuthoritativeRoom room) =>
        room.AuthorityRoute?.Endpoint is DotRecastAuthorityHostEndpoint endpoint &&
        endpoint.GateScene is { IsDisposed: false } && endpoint.AuthorityAddress != 0
            ? endpoint
            : throw new InvalidOperationException("DotRecast Authority Scene route is unavailable.");

    static ServerAuthoritativeInnerHostIdentity BuildHost(ServerAuthoritativeRoom room)
    {
        ServerAuthoritativeAuthorityHostRoute route = room.AuthorityRoute ??
            throw new InvalidOperationException("Room has no DotRecast Authority route.");
        return new ServerAuthoritativeInnerHostIdentity
        {
            HostProductId = route.HostProductId,
            HostId = route.HostId,
            RouteKind = (int)route.RouteKind,
            RoomId = room.RoomId
        };
    }
}
