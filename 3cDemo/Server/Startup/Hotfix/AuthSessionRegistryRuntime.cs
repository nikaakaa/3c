using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fantasy.Network;

namespace Fantasy;

public static partial class AuthSessionRegistryRuntime
{
    public const uint CurrentAuthProtocolVersion = 1;
    public const int CurrentClientBuildMajorVersion = 1;
    public const long SessionTokenLifetimeMilliseconds = 15 * 60 * 1000;

    public static bool TryReplaceCurrent(
        AuthSessionRegistryComponent registry,
        string accountId,
        Session session,
        string clientInstanceId,
        string tokenIdentity,
        out AuthSessionRegistryEntry? previous,
        out ulong generation)
    {
        registry.CurrentByAccount.TryGetValue(accountId, out previous);
        registry.LastGenerationByAccount.TryGetValue(accountId, out ulong lastGeneration);
        if (lastGeneration == ulong.MaxValue)
        {
            generation = 0;
            return false;
        }

        generation = lastGeneration + 1;
        var current = new AuthSessionRegistryEntry(
            accountId,
            session,
            session.RuntimeId,
            clientInstanceId,
            generation,
            tokenIdentity);
        registry.LastGenerationByAccount[accountId] = generation;
        registry.CurrentByAccount[accountId] = current;
        return true;
    }

    public static bool TryRemoveCurrent(
        AuthSessionRegistryComponent registry,
        string accountId,
        ulong generation,
        long sessionRuntimeId)
    {
        if (!registry.CurrentByAccount.TryGetValue(accountId, out AuthSessionRegistryEntry? current) ||
            current.SessionGeneration != generation || current.SessionRuntimeId != sessionRuntimeId)
        {
            return false;
        }

        return registry.CurrentByAccount.Remove(accountId);
    }

    public static AuthErrorCode Validate(
        C2G_GuestLoginRequest request,
        out string canonicalAccountId,
        out string reason)
    {
        canonicalAccountId = string.Empty;
        reason = string.Empty;
        string guestAccountId = request?.GuestAccountId ?? string.Empty;
        string clientInstanceId = request?.ClientInstanceId ?? string.Empty;
        if (request == null || !GuestAccountPattern().IsMatch(guestAccountId) ||
            !ClientInstancePattern().IsMatch(clientInstanceId))
        {
            reason = "GuestAccountId or ClientInstanceId is invalid.";
            return AuthErrorCode.InvalidRequest;
        }

        if (!Version.TryParse(request.ClientBuildVersion, out Version? buildVersion) ||
            buildVersion.Major != CurrentClientBuildMajorVersion)
        {
            reason = "ClientBuildVersion is unsupported.";
            return AuthErrorCode.ClientBuildUnsupported;
        }

        if (request.AuthProtocolVersion != CurrentAuthProtocolVersion)
        {
            reason = "AuthProtocolVersion is incompatible.";
            return AuthErrorCode.ProtocolMismatch;
        }

        canonicalAccountId = $"guest:{guestAccountId.Trim().ToLowerInvariant()}";
        return AuthErrorCode.Success;
    }

    public static string CreateSessionToken()
    {
        string value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string ComputeTokenIdentity(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex GuestAccountPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]{7,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex ClientInstancePattern();
}
