using System;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace ThirdPersonGameplay.Networking.Fantasy
{
    public sealed class ProductAuthSessionOwner : FantasyClientSessionOwner
    {
        private readonly object m_EventGate = new object();
        private readonly Queue<ProductAuthEvent> m_Events = new Queue<ProductAuthEvent>();
        private readonly FantasyConnectionSettings m_Settings;
        private string m_SessionToken = string.Empty;

        public ProductAuthSessionOwner(Uri authEndpoint, int connectTimeoutMilliseconds)
        {
            if (authEndpoint == null || !authEndpoint.IsAbsoluteUri ||
                !string.Equals(authEndpoint.Scheme, "wss", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(authEndpoint.Host) ||
                !string.Equals(authEndpoint.AbsolutePath, "/", StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(authEndpoint.Query) ||
                !string.IsNullOrEmpty(authEndpoint.Fragment))
            {
                throw new ArgumentException("AuthEndpoint must be an absolute wss:// URI.", nameof(authEndpoint));
            }

            if (connectTimeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectTimeoutMilliseconds));
            }

            int port = authEndpoint.IsDefaultPort ? 443 : authEndpoint.Port;
            m_Settings = new FantasyConnectionSettings(
                authEndpoint.Host,
                port,
                FantasyClientProtocol.WebSocket,
                true,
                connectTimeoutMilliseconds);
        }

        public AuthenticatedGuestSessionState AuthenticatedState { get; private set; }

        public async FTask<Session> ConnectAsync(
            Action onConnectComplete,
            Action onConnectFail,
            Action onConnectDisconnect)
        {
            RevokeAuthentication();
            Session session = await ConnectOwnedAsync(
                m_Settings,
                onConnectComplete,
                () =>
                {
                    RevokeAuthentication();
                    onConnectFail?.Invoke();
                },
                () =>
                {
                    RevokeAuthentication();
                    onConnectDisconnect?.Invoke();
                });
            var binding = session.AddComponent<ProductAuthSessionBinding>();
            binding.Owner = this;
            return session;
        }

        public async FTask<GuestLoginResult> LoginAsync(GuestLoginCommand command)
        {
            if (!IsConnected)
            {
                return new GuestLoginResult(default, new ProductAuthError(4, "Auth Session is not connected."));
            }

            using var request = C2G_GuestLoginRequest.Create();
            request.GuestAccountId = command.GuestAccountId;
            request.ClientInstanceId = command.ClientInstanceId;
            request.ClientBuildVersion = command.ClientBuildVersion;
            request.AuthProtocolVersion = command.AuthProtocolVersion;
            G2C_GuestLoginResponse response;
            try
            {
                response = await Session.Call(request) as G2C_GuestLoginResponse;
            }
            catch (Exception exception)
            {
                RevokeAuthentication();
                return new GuestLoginResult(default, new ProductAuthError(4, exception.Message));
            }
            if (response == null)
            {
                return new GuestLoginResult(default, new ProductAuthError(4, "Auth Server returned an unexpected response."));
            }

            int code = response.ResultCode != 0 ? response.ResultCode : (int)response.ErrorCode;
            if (code != 0)
            {
                RevokeAuthentication();
                return new GuestLoginResult(default, new ProductAuthError(code, MapError(code)));
            }

            var state = new AuthenticatedGuestSessionState(
                response.AccountId,
                response.SessionGeneration,
                response.TokenExpiresAt);
            if (!state.IsAuthenticated || string.IsNullOrWhiteSpace(response.SessionToken))
            {
                RevokeAuthentication();
                return new GuestLoginResult(default, new ProductAuthError(4, "Auth Server returned incomplete session identity."));
            }

            m_SessionToken = response.SessionToken;
            AuthenticatedState = state;
            return new GuestLoginResult(state, default);
        }

        public bool TryTakeEvent(out ProductAuthEvent value)
        {
            lock (m_EventGate)
            {
                if (m_Events.Count == 0)
                {
                    value = default;
                    return false;
                }

                value = m_Events.Dequeue();
                return true;
            }
        }

        public void RevokeAuthentication()
        {
            m_SessionToken = string.Empty;
            AuthenticatedState = default;
        }

        internal void EnqueueSessionReplaced(string reason, ulong newGeneration)
        {
            lock (m_EventGate)
            {
                m_Events.Enqueue(new ProductAuthEvent(reason, newGeneration));
            }
        }

        protected override void OnDisposing()
        {
            RevokeAuthentication();
            lock (m_EventGate)
            {
                m_Events.Clear();
            }
        }

        private static string MapError(int code)
        {
            return code switch
            {
                1 => "Guest identity is invalid.",
                2 => "Client build is not supported.",
                3 => "Auth protocol version is incompatible.",
                _ => "Auth Gateway is unavailable."
            };
        }
    }

    public sealed class ProductAuthSessionBinding : Entity
    {
        public ProductAuthSessionOwner Owner;
    }

    public sealed class ProductAuthSessionBindingDestroySystem : DestroySystem<ProductAuthSessionBinding>
    {
        protected override void Destroy(ProductAuthSessionBinding self)
        {
            self.Owner = null;
        }
    }

    public sealed class AccountSessionReplacedHandler : Message<G2C_AccountSessionReplaced>
    {
        protected override async FTask Run(Session session, G2C_AccountSessionReplaced message)
        {
            ProductAuthSessionOwner owner = session?.GetComponent<ProductAuthSessionBinding>()?.Owner;
            if (owner == null)
            {
                Log.Error("Auth Session has no ProductAuthSessionOwner binding.");
                await FTask.CompletedTask;
                return;
            }

            owner.EnqueueSessionReplaced(message?.Reason, message?.NewSessionGeneration ?? 0);
            await FTask.CompletedTask;
        }
    }
}
