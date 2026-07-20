using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_GuestLoginRequestHandler :
    MessageRPC<C2G_GuestLoginRequest, G2C_GuestLoginResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_GuestLoginRequest request,
        G2C_GuestLoginResponse response,
        Action reply)
    {
        AuthErrorCode code = AuthSessionRegistryRuntime.Validate(request, out string accountId, out _);
        AuthSessionRegistryComponent? registry = session.Scene.GetComponent<AuthSessionRegistryComponent>();
        if (code == AuthErrorCode.Success && registry == null)
        {
            code = AuthErrorCode.GatewayUnavailable;
        }

        if (code == AuthErrorCode.Success && session.GetComponent<AuthenticatedGuestComponent>() != null)
        {
            code = AuthErrorCode.InvalidRequest;
        }

        response.ErrorCode = (uint)code;
        response.ResultCode = (int)code;
        if (code != AuthErrorCode.Success)
        {
            await FTask.CompletedTask;
            return;
        }

        string token = AuthSessionRegistryRuntime.CreateSessionToken();
        string tokenIdentity = AuthSessionRegistryRuntime.ComputeTokenIdentity(token);
        if (!AuthSessionRegistryRuntime.TryReplaceCurrent(
            registry!,
            accountId,
            session,
            request.ClientInstanceId,
            tokenIdentity,
            out AuthSessionRegistryEntry? previous,
            out ulong generation))
        {
            response.ErrorCode = (uint)AuthErrorCode.GatewayUnavailable;
            response.ResultCode = (int)AuthErrorCode.GatewayUnavailable;
            await FTask.CompletedTask;
            return;
        }
        var authenticated = session.AddComponent<AuthenticatedGuestComponent>();
        authenticated.Registry = registry;
        authenticated.AccountId = accountId;
        authenticated.SessionGeneration = generation;
        authenticated.SessionRuntimeId = session.RuntimeId;
        response.AccountId = accountId;
        response.SessionGeneration = generation;
        response.SessionToken = token;
        response.TokenExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                                  AuthSessionRegistryRuntime.SessionTokenLifetimeMilliseconds;

        if (previous?.Session is { IsDisposed: false } previousSession &&
            previous.SessionRuntimeId != session.RuntimeId)
        {
            using var replaced = G2C_AccountSessionReplaced.Create();
            replaced.Reason = "Another client authenticated the same Guest Demo Identity.";
            replaced.NewSessionGeneration = generation;
            previousSession.Send(replaced);
            previousSession.Dispose();
        }

        await FTask.CompletedTask;
    }
}
