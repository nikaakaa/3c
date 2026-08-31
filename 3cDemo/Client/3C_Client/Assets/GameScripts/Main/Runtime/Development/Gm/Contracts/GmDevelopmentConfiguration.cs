using System;

namespace ThirdPerson.Development.Gm
{
    public static class GmHttpProtocol
    {
        public const int Version = 1;
        public const string ServicePath = "/v1/service";
        public const string CommandsPath = "/v1/commands";
        public const string RelayInstanceHeader = "X-Relay-Instance";
        public const string ConsoleManifestFileName = "GmConsoleManifest.json";

        public static void RequireEndpoint(string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri value) || value.Scheme != "http" ||
                value.Host != "127.0.0.1" || value.Port <= 0 || value.AbsolutePath != "/" ||
                value.UserInfo.Length != 0 || value.Query.Length != 0 || value.Fragment.Length != 0)
                throw new ArgumentException("GM 开发 endpoint 必须是显式端口的本机 HTTP 根地址。");
        }

        public static void RequireToken(string token)
        {
            if (token == null || token.Length != 64)
                throw new ArgumentException("GM 开发访问凭据缺失或格式无效。");
            foreach (char value in token)
            {
                if (!(value >= '0' && value <= '9') && !(value >= 'a' && value <= 'f'))
                    throw new ArgumentException("GM 开发访问凭据格式无效。");
            }
        }
    }

    [Serializable]
    public sealed class GmHttpServerConfiguration
    {
        public string listenAddress = string.Empty;
        public int listenPort;
        public string accessToken = string.Empty;
        public int maximumMessageBytes;
        public int maximumConcurrentRequests;
        public int requestTimeoutMilliseconds;

        public string Endpoint => $"http://{listenAddress}:{listenPort}/";

        public void RequireValid()
        {
            if (listenAddress != "127.0.0.1" || listenPort < 1024 || listenPort > 65535 ||
                maximumMessageBytes < 1024 || maximumMessageBytes > 65536 ||
                maximumConcurrentRequests <= 0 || maximumConcurrentRequests > 64 ||
                requestTimeoutMilliseconds < 100 || requestTimeoutMilliseconds > 10000)
                throw new ArgumentException("GM HTTP 容量、地址或超时配置无效。");
            GmHttpProtocol.RequireToken(accessToken);
        }
    }

    [Serializable]
    public sealed class GmServerManifest
    {
        public int schemaVersion;
        public string buildId = string.Empty;
        public string sessionId = string.Empty;
        public GmHttpServerConfiguration http;
        public string relayQueryEndpoint = string.Empty;
        public string relayQueryToken = string.Empty;
        public int relayQueryTimeoutMilliseconds;

        public void RequireValid()
        {
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(buildId) ||
                string.IsNullOrWhiteSpace(sessionId) || http == null || relayQueryTimeoutMilliseconds < 100 ||
                relayQueryTimeoutMilliseconds >= http.requestTimeoutMilliseconds)
                throw new ArgumentException("GM 服务 manifest 不完整或版本无效。");
            http.RequireValid();
            GmHttpProtocol.RequireEndpoint(relayQueryEndpoint);
            GmHttpProtocol.RequireToken(relayQueryToken);
            if (http.accessToken == relayQueryToken || http.Endpoint == relayQueryEndpoint)
                throw new ArgumentException("GM 与 Relay 查询必须使用不同 endpoint 和访问凭据。");
        }
    }

    [Serializable]
    public sealed class RelayQueryManifest
    {
        public int schemaVersion;
        public string buildId = string.Empty;
        public string sessionId = string.Empty;
        public GmHttpServerConfiguration http;
        public int maximumQueuedQueries;
        public int maximumQueriesPerPump;

        public void RequireValid()
        {
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(buildId) ||
                string.IsNullOrWhiteSpace(sessionId) || http == null || maximumQueuedQueries <= 0 ||
                maximumQueuedQueries > 64 || maximumQueriesPerPump <= 0 || maximumQueriesPerPump > maximumQueuedQueries)
                throw new ArgumentException("Relay 查询 manifest 不完整或容量无效。");
            http.RequireValid();
        }
    }

    [Serializable]
    public sealed class GmClientManifest
    {
        public int schemaVersion;
        public string buildId = string.Empty;
        public string sessionId = string.Empty;
        public string endpoint = string.Empty;
        public string accessToken = string.Empty;
        public int maximumMessageBytes;
        public int maximumPendingRequests;
        public int requestTimeoutMilliseconds;
        public int historyCapacity;
        public int outputCapacity;
        public int maximumOutputCharacters;

        public void RequireValid()
        {
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(buildId) ||
                string.IsNullOrWhiteSpace(sessionId) || maximumMessageBytes < 1024 || maximumMessageBytes > 65536 ||
                maximumPendingRequests <= 0 || maximumPendingRequests > 16 || requestTimeoutMilliseconds < 100 ||
                requestTimeoutMilliseconds > 10000 || historyCapacity <= 0 || historyCapacity > 128 ||
                outputCapacity <= 0 || outputCapacity > 128 || maximumOutputCharacters < 256 || maximumOutputCharacters > 16384)
                throw new ArgumentException("GM 客户端 manifest 不完整或容量无效。");
            GmHttpProtocol.RequireEndpoint(endpoint);
            GmHttpProtocol.RequireToken(accessToken);
        }
    }
}
