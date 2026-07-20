using System;
using Fantasy.Async;
using Fantasy.Network;

namespace ThirdPersonGameplay.Networking.Fantasy
{
    public sealed class ServerAuthoritativeControlSessionOwner : FantasyClientSessionOwner
    {
        public FTask<Session> ConnectAsync(
            string host,
            int port,
            int connectTimeoutMilliseconds,
            Action onConnectComplete,
            Action onConnectFail,
            Action onConnectDisconnect)
        {
            var settings = new FantasyConnectionSettings(
                host,
                port,
                FantasyClientProtocol.Kcp,
                false,
                connectTimeoutMilliseconds);
            return ConnectOwnedAsync(settings, onConnectComplete, onConnectFail, onConnectDisconnect);
        }
    }
}
