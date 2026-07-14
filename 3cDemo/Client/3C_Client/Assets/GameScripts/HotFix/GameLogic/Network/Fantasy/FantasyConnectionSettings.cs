using Fantasy.Network;

namespace GameLogic.Network.Fantasy
{
    public readonly struct FantasyConnectionSettings
    {
        public FantasyConnectionSettings(
            string host,
            int port,
            FantasyClientProtocol protocol = FantasyClientProtocol.Kcp,
            bool enableHttps = false,
            int connectTimeoutMilliseconds = 5000)
        {
            Host = host;
            Port = port;
            Protocol = protocol;
            EnableHttps = enableHttps;
            ConnectTimeoutMilliseconds = connectTimeoutMilliseconds;
        }

        public string Host { get; }

        public int Port { get; }

        public FantasyClientProtocol Protocol { get; }

        public bool EnableHttps { get; }

        public int ConnectTimeoutMilliseconds { get; }

        internal NetworkProtocolType ToFantasyProtocol()
        {
            return Protocol switch
            {
                FantasyClientProtocol.Tcp => NetworkProtocolType.TCP,
                FantasyClientProtocol.WebSocket => NetworkProtocolType.WebSocket,
                _ => NetworkProtocolType.KCP
            };
        }
    }
}
