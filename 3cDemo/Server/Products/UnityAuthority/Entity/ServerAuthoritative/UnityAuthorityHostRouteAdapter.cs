using Fantasy.Network;

using ThirdPersonSimulation.UnityAuthority;

namespace Fantasy;

public sealed class UnityAuthorityHostEndpoint : IServerAuthoritativeAuthorityHostEndpoint
{
    public UnityAuthorityHostEndpoint(Session session, string hostId)
    {
        Session = session is { IsDisposed: false }
            ? session
            : throw new ArgumentException("Unity Authority Worker Session is unavailable.", nameof(session));
        EndpointIdentity = string.IsNullOrWhiteSpace(hostId)
            ? throw new ArgumentException("Unity Authority Worker HostId is required.", nameof(hostId))
            : hostId.Trim();
    }

    public Session Session { get; }
    public string EndpointIdentity { get; }

    public static bool Matches(ServerAuthoritativeRoom room, Session session, string hostId) =>
        room.AuthorityRoute is { IsDisposed: false, Endpoint: UnityAuthorityHostEndpoint endpoint } route &&
        endpoint.Session == session &&
        string.Equals(endpoint.EndpointIdentity, hostId, StringComparison.Ordinal) &&
        string.Equals(route.HostId, hostId, StringComparison.Ordinal);
}

public sealed class UnityAuthorityHostRouteAdapter : IServerAuthoritativeAuthorityHostRouteAdapter
{
    public string ProductId => UnityAuthorityHostProduct.ServerProductId;
    public ServerAuthoritativeAuthorityHostRouteKind RouteKind => ServerAuthoritativeAuthorityHostRouteKind.ExternalAuthorityWorker;
    public string HostProductId => UnityAuthorityHostProduct.ProductId.Value;

    public bool ValidateRegistration(ServerAuthoritativeAuthorityHostRegistration registration, out string reason)
    {
        bool valid = registration.Endpoint is UnityAuthorityHostEndpoint endpoint &&
            endpoint.Session is { IsDisposed: false } &&
            string.Equals(endpoint.EndpointIdentity, registration.HostId, StringComparison.Ordinal) &&
            string.Equals(registration.SolverId, UnityAuthorityHostProduct.Descriptor.AuthoritySolverId.Value, StringComparison.Ordinal) &&
            string.Equals(registration.SolverVersion, UnityAuthorityHostProduct.Descriptor.AuthoritySolverVersion, StringComparison.Ordinal) &&
            registration.SolverCapabilities == (ulong)UnityAuthorityHostProduct.Descriptor.AuthoritySolverCapabilities &&
            registration.SolverFeatures == (ulong)UnityAuthorityHostProduct.Descriptor.AuthoritySolverFeatures;
        reason = valid ? string.Empty : "Unity Authority registration does not match its Product-owned Worker endpoint and Solver declaration.";
        return valid;
    }

    public void SendRoster(ServerAuthoritativeRoom room, ServerAuthoritativeRosterMessage roster) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeRosterChanged { Roster = roster });

    public void SendTicket(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeDataPlaneTicket ticket,
        ServerAuthoritativeDataPlaneTicketMessage outerTicket) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeDataPlaneTicketIssued { Ticket = outerTicket });

    public void SendHeartbeatAck(
        ServerAuthoritativeRoom room,
        ulong sequence,
        long sentUnixMilliseconds,
        long serverUnixMilliseconds) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeControlHeartbeatAck
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            Sequence = sequence,
            ClientUnixMilliseconds = sentUnixMilliseconds,
            ServerUnixMilliseconds = serverUnixMilliseconds
        });

    public void SendFullCheckpointRequest(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeRoomPlayer player,
        ulong requestSequence,
        ulong lastUsableSnapshotSequence,
        string reason) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeFullCheckpointRequest
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            PlayerId = player.PlayerId,
            ActorId = player.ActorId,
            RequestSequence = requestSequence,
            LastUsableSnapshotSequence = lastUsableSnapshotSequence,
            Reason = reason
        });

    public void SendFailure(ServerAuthoritativeRoom room, ServerAuthoritativeErrorCode code, string reason) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeSessionFailed
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            ResultCode = (int)code,
            Reason = reason
        });

    public void SendTicketRevoked(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket, string reason) =>
        Require(room).Session.Send(new G2W_ServerAuthoritativeDataPlaneTicketRevoked
        {
            RoomId = room.RoomId,
            SessionId = room.SessionId,
            TicketId = ticket.TicketId,
            Reason = reason
        });

    public void ReleaseHost(ServerAuthoritativeRoom room)
    {
    }

    static UnityAuthorityHostEndpoint Require(ServerAuthoritativeRoom room) =>
        room.AuthorityRoute?.Endpoint is UnityAuthorityHostEndpoint endpoint && endpoint.Session is { IsDisposed: false }
            ? endpoint
            : throw new InvalidOperationException("Unity Authority Worker route is unavailable.");
}
