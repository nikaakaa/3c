using System;

namespace ThirdPerson.Development.Gm
{
    public static class GmHttpProtocol
    {
        public const int Version = 2;
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

    public static class GmToolCatalog
    {
        public const string ToolId = "thirdperson.rollback-gm";
        public const string ToolVersion = "1";
    }

    [Serializable]
    public sealed class GmToolIdentity
    {
        public string toolId = string.Empty;
        public string toolVersion = string.Empty;
        public int protocolVersion;
        public string commandCatalogHash = string.Empty;
        public string bundleHash = string.Empty;

        public void RequireValid()
        {
            if (toolId != GmToolCatalog.ToolId || toolVersion != GmToolCatalog.ToolVersion ||
                protocolVersion != GmHttpProtocol.Version || !IsHash(commandCatalogHash) || !IsHash(bundleHash))
                throw new ArgumentException("GM Tool Identity不完整或版本无效。");
        }

        static bool IsHash(string value)
        {
            if (value == null || value.Length != 64)
                return false;
            foreach (char item in value)
            {
                if (!(item >= '0' && item <= '9') && !(item >= 'a' && item <= 'f'))
                    return false;
            }
            return true;
        }
    }

    [Serializable]
    public sealed class GmToolPolicy
    {
        public int maximumMessageBytes;
        public int maximumServerRequests;
        public int maximumQueuedQueries;
        public int maximumQueriesPerPump;
        public int relayTimeoutMilliseconds;
        public int serverTimeoutMilliseconds;
        public int clientTimeoutMilliseconds;
        public int maximumClientRequests;
        public int historyCapacity;
        public int outputCapacity;
        public int maximumOutputCharacters;

        public void RequireValid()
        {
            if (maximumMessageBytes < 1024 || maximumMessageBytes > 65536 ||
                maximumServerRequests <= 0 || maximumServerRequests > 64 ||
                maximumQueuedQueries <= 0 || maximumQueuedQueries > 64 ||
                maximumQueriesPerPump <= 0 || maximumQueriesPerPump > maximumQueuedQueries ||
                relayTimeoutMilliseconds < 100 || serverTimeoutMilliseconds <= relayTimeoutMilliseconds ||
                clientTimeoutMilliseconds <= serverTimeoutMilliseconds || clientTimeoutMilliseconds > 10000 ||
                maximumClientRequests <= 0 || maximumClientRequests > 16 ||
                historyCapacity <= 0 || historyCapacity > 128 || outputCapacity <= 0 || outputCapacity > 128 ||
                maximumOutputCharacters < 256 || maximumOutputCharacters > 16384)
                throw new ArgumentException("GM Tool Policy容量或超时无效。");
        }
    }

    [Serializable]
    public sealed class GmToolManifest
    {
        public int schemaVersion;
        public string toolId = string.Empty;
        public string toolVersion = string.Empty;
        public int protocolVersion;
        public string commandCatalogHash = string.Empty;

        public void RequireValid()
        {
            var identity = new GmToolIdentity
            {
                toolId = toolId,
                toolVersion = toolVersion,
                protocolVersion = protocolVersion,
                commandCatalogHash = commandCatalogHash,
                bundleHash = new string('0', 64)
            };
            identity.RequireValid();
            if (schemaVersion != 1)
                throw new ArgumentException("GM Tool Manifest schema无效。");
        }
    }

    [Serializable]
    public sealed class GmRunRequest
    {
        public int schemaVersion;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string slotId = string.Empty;
        public string gmAddress = string.Empty;
        public int gmPort;
        public string relayQueryAddress = string.Empty;
        public int relayQueryPort;
        public string toolBundleHash = string.Empty;

        public void RequireValid()
        {
            if (schemaVersion != 1 || string.IsNullOrWhiteSpace(candidateId) || string.IsNullOrWhiteSpace(runId) ||
                string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(slotId) ||
                gmAddress != "127.0.0.1" || relayQueryAddress != "127.0.0.1" ||
                gmPort <= 0 || gmPort > 65535 || relayQueryPort <= 0 || relayQueryPort > 65535 ||
                gmPort == relayQueryPort || toolBundleHash == null || toolBundleHash.Length != 64)
                throw new ArgumentException("GM Run Request不完整或endpoint无效。");
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
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string slotId = string.Empty;
        public GmToolIdentity tool = new GmToolIdentity();
        public GmHttpServerConfiguration http;
        public string relayQueryEndpoint = string.Empty;
        public string relayQueryToken = string.Empty;
        public int relayQueryTimeoutMilliseconds;

        public void RequireValid()
        {
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(candidateId) ||
                string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(slotId) || tool == null || http == null || relayQueryTimeoutMilliseconds < 100 ||
                relayQueryTimeoutMilliseconds >= http.requestTimeoutMilliseconds)
                throw new ArgumentException("GM 服务 manifest 不完整或版本无效。");
            tool.RequireValid();
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
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string slotId = string.Empty;
        public GmToolIdentity tool = new GmToolIdentity();
        public GmHttpServerConfiguration http;
        public int maximumQueuedQueries;
        public int maximumQueriesPerPump;

        public void RequireValid()
        {
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(candidateId) ||
                string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(slotId) || tool == null || http == null || maximumQueuedQueries <= 0 ||
                maximumQueuedQueries > 64 || maximumQueriesPerPump <= 0 || maximumQueriesPerPump > maximumQueuedQueries)
                throw new ArgumentException("Relay 查询 manifest 不完整或容量无效。");
            tool.RequireValid();
            http.RequireValid();
        }
    }

    [Serializable]
    public sealed class GmClientManifest
    {
        public int schemaVersion;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string slotId = string.Empty;
        public GmToolIdentity tool = new GmToolIdentity();
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
            if (schemaVersion != GmHttpProtocol.Version || string.IsNullOrWhiteSpace(candidateId) ||
                string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(slotId) || tool == null || maximumMessageBytes < 1024 || maximumMessageBytes > 65536 ||
                maximumPendingRequests <= 0 || maximumPendingRequests > 16 || requestTimeoutMilliseconds < 100 ||
                requestTimeoutMilliseconds > 10000 || historyCapacity <= 0 || historyCapacity > 128 ||
                outputCapacity <= 0 || outputCapacity > 128 || maximumOutputCharacters < 256 || maximumOutputCharacters > 16384)
                throw new ArgumentException("GM 客户端 manifest 不完整或容量无效。");
            tool.RequireValid();
            GmHttpProtocol.RequireEndpoint(endpoint);
            GmHttpProtocol.RequireToken(accessToken);
        }
    }
}
