namespace Fantasy;

public interface IServerAuthoritativeAuthorityHostEndpoint
{
    string EndpointIdentity { get; }
}

public interface IServerAuthoritativeAuthorityHostRouteAdapter
{
    string ProductId { get; }
    ServerAuthoritativeAuthorityHostRouteKind RouteKind { get; }
    string HostProductId { get; }
    bool ValidateRegistration(ServerAuthoritativeAuthorityHostRegistration registration, out string reason);
    void SendRoster(ServerAuthoritativeRoom room, ServerAuthoritativeRosterMessage roster);
    void SendTicket(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket, ServerAuthoritativeDataPlaneTicketMessage outerTicket);
    void SendHeartbeatAck(ServerAuthoritativeRoom room, ulong sequence, long sentUnixMilliseconds, long serverUnixMilliseconds);
    void SendFullCheckpointRequest(
        ServerAuthoritativeRoom room,
        ServerAuthoritativeRoomPlayer player,
        ulong requestSequence,
        ulong lastUsableSnapshotSequence,
        string reason);
    void SendFailure(ServerAuthoritativeRoom room, ServerAuthoritativeErrorCode code, string reason);
    void SendTicketRevoked(ServerAuthoritativeRoom room, ServerAuthoritativeDataPlaneTicket ticket, string reason);
    void ReleaseHost(ServerAuthoritativeRoom room);
}

public static class ServerAuthoritativeAuthorityHostRouteAdapterRegistry
{
    static IServerAuthoritativeAuthorityHostRouteAdapter? s_Adapter;

    public static IServerAuthoritativeAuthorityHostRouteAdapter Adapter => s_Adapter ??
        throw new InvalidOperationException("Server product did not install an Authority Host route adapter.");

    public static void Install(IServerAuthoritativeAuthorityHostRouteAdapter adapter)
    {
        if (adapter == null)
            throw new ArgumentNullException(nameof(adapter));
        if (s_Adapter != null)
            throw new InvalidOperationException("Authority Host route adapter is already installed.");
        if (string.IsNullOrWhiteSpace(adapter.ProductId) || string.IsNullOrWhiteSpace(adapter.HostProductId) ||
            adapter.RouteKind == ServerAuthoritativeAuthorityHostRouteKind.None)
        {
            throw new InvalidOperationException("Authority Host route adapter identity is incomplete.");
        }
        s_Adapter = adapter;
    }
}
