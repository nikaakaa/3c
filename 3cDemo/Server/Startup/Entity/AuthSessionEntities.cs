using Fantasy.Entitas;
using Fantasy.Network;

namespace Fantasy;

public enum AuthErrorCode : int
{
    Success = 0,
    InvalidRequest = 1,
    ClientBuildUnsupported = 2,
    ProtocolMismatch = 3,
    GatewayUnavailable = 4
}

public sealed record AuthSessionRegistryEntry(
    string AccountId,
    Session Session,
    long SessionRuntimeId,
    string ClientInstanceId,
    ulong SessionGeneration,
    string SessionTokenIdentity);

public sealed class AuthSessionRegistryComponent : Entity
{
    public readonly Dictionary<string, AuthSessionRegistryEntry> CurrentByAccount =
        new(StringComparer.Ordinal);
    public readonly Dictionary<string, ulong> LastGenerationByAccount =
        new(StringComparer.Ordinal);
}

public sealed class AuthenticatedGuestComponent : Entity
{
    public AuthSessionRegistryComponent? Registry;
    public string AccountId = string.Empty;
    public ulong SessionGeneration;
    public long SessionRuntimeId;
}
