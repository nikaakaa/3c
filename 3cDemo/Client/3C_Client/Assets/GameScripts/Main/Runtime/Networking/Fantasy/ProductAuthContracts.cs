using System;

namespace ThirdPersonGameplay.Networking.Fantasy
{
    public readonly struct GuestLoginCommand
    {
        public GuestLoginCommand(
            string guestAccountId,
            string clientInstanceId,
            string clientBuildVersion,
            uint authProtocolVersion)
        {
            GuestAccountId = guestAccountId ?? throw new ArgumentNullException(nameof(guestAccountId));
            ClientInstanceId = clientInstanceId ?? throw new ArgumentNullException(nameof(clientInstanceId));
            ClientBuildVersion = clientBuildVersion ?? throw new ArgumentNullException(nameof(clientBuildVersion));
            AuthProtocolVersion = authProtocolVersion;
        }

        public string GuestAccountId { get; }
        public string ClientInstanceId { get; }
        public string ClientBuildVersion { get; }
        public uint AuthProtocolVersion { get; }
    }

    public readonly struct AuthenticatedGuestSessionState
    {
        public AuthenticatedGuestSessionState(string accountId, ulong generation, long tokenExpiresAt)
        {
            AccountId = accountId;
            Generation = generation;
            TokenExpiresAt = tokenExpiresAt;
        }

        public string AccountId { get; }
        public ulong Generation { get; }
        public long TokenExpiresAt { get; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccountId) && Generation != 0;
    }

    public readonly struct ProductAuthError
    {
        public ProductAuthError(int code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        public int Code { get; }
        public string Message { get; }
        public bool HasError => Code != 0;
    }

    public readonly struct GuestLoginResult
    {
        public GuestLoginResult(AuthenticatedGuestSessionState state, ProductAuthError error)
        {
            State = state;
            Error = error;
        }

        public AuthenticatedGuestSessionState State { get; }
        public ProductAuthError Error { get; }
        public bool Succeeded => !Error.HasError && State.IsAuthenticated;
    }

    public readonly struct ProductAuthEvent
    {
        public ProductAuthEvent(string reason, ulong newGeneration)
        {
            Reason = reason ?? string.Empty;
            NewGeneration = newGeneration;
        }

        public string Reason { get; }
        public ulong NewGeneration { get; }
    }
}
