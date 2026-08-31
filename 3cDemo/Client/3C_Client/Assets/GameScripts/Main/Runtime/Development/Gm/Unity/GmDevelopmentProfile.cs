using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPerson.Development.Gm
{
    [CreateAssetMenu(menuName = "3C/Development/GM Profile")]
    public sealed class GmDevelopmentProfile : ScriptableObject
    {
        [SerializeField] int m_GmPort = 24200;
        [SerializeField] int m_RelayQueryPort = 24201;
        [SerializeField] int m_MaximumMessageBytes = 65536;
        [SerializeField] int m_MaximumServerRequests = 16;
        [SerializeField] int m_MaximumQueuedQueries = 32;
        [SerializeField] int m_MaximumQueriesPerPump = 2;
        [SerializeField] int m_RelayTimeoutMilliseconds = 2000;
        [SerializeField] int m_ServerTimeoutMilliseconds = 4000;
        [SerializeField] int m_ClientTimeoutMilliseconds = 5000;
        [SerializeField] int m_MaximumClientRequests = 8;
        [SerializeField] int m_HistoryCapacity = 32;
        [SerializeField] int m_OutputCapacity = 64;
        [SerializeField] int m_MaximumOutputCharacters = 4096;
        [SerializeField] InputActionAsset m_Actions;
        [SerializeField] string m_ToggleActionId;
        [SerializeField] string m_FontFamily = "Microsoft YaHei";
        [SerializeField] int m_FontSize = 16;

        public int GmPort => m_GmPort;
        public int RelayQueryPort => m_RelayQueryPort;
        public InputActionAsset Actions => m_Actions;
        public string ToggleActionId => m_ToggleActionId;
        public string FontFamily => m_FontFamily;
        public int FontSize => m_FontSize;

        public void RequireValid()
        {
            if (m_GmPort == m_RelayQueryPort || !m_Actions || !Guid.TryParse(m_ToggleActionId, out _) ||
                m_Actions.FindAction(m_ToggleActionId, false) == null || string.IsNullOrWhiteSpace(m_FontFamily) ||
                m_FontSize < 10 || m_FontSize > 32 || m_ServerTimeoutMilliseconds >= m_ClientTimeoutMilliseconds)
                throw new InvalidOperationException("GM Profile 的端口、输入、字体或超时配置无效。");
        }

        public GmServerManifest BuildServerManifest(string buildId, string sessionId, string clientToken, string relayToken)
        {
            RequireValid();
            var value = new GmServerManifest
            {
                schemaVersion = GmHttpProtocol.Version, buildId = buildId, sessionId = sessionId,
                http = BuildHttp(m_GmPort, clientToken), relayQueryEndpoint = $"http://127.0.0.1:{m_RelayQueryPort}/",
                relayQueryToken = relayToken, relayQueryTimeoutMilliseconds = m_RelayTimeoutMilliseconds
            };
            value.RequireValid();
            return value;
        }

        public RelayQueryManifest BuildRelayManifest(string buildId, string sessionId, string relayToken)
        {
            RequireValid();
            var value = new RelayQueryManifest
            {
                schemaVersion = GmHttpProtocol.Version, buildId = buildId, sessionId = sessionId,
                http = BuildHttp(m_RelayQueryPort, relayToken),
                maximumQueuedQueries = m_MaximumQueuedQueries, maximumQueriesPerPump = m_MaximumQueriesPerPump
            };
            value.RequireValid();
            return value;
        }

        public GmClientManifest BuildClientManifest(string buildId, string sessionId, string clientToken)
        {
            RequireValid();
            var value = new GmClientManifest
            {
                schemaVersion = GmHttpProtocol.Version, buildId = buildId, sessionId = sessionId,
                endpoint = $"http://127.0.0.1:{m_GmPort}/", accessToken = clientToken,
                maximumMessageBytes = m_MaximumMessageBytes, maximumPendingRequests = m_MaximumClientRequests,
                requestTimeoutMilliseconds = m_ClientTimeoutMilliseconds, historyCapacity = m_HistoryCapacity,
                outputCapacity = m_OutputCapacity, maximumOutputCharacters = m_MaximumOutputCharacters
            };
            value.RequireValid();
            return value;
        }

        GmHttpServerConfiguration BuildHttp(int port, string token) => new GmHttpServerConfiguration
        {
            listenAddress = "127.0.0.1", listenPort = port, accessToken = token,
            maximumMessageBytes = m_MaximumMessageBytes, maximumConcurrentRequests = m_MaximumServerRequests,
            requestTimeoutMilliseconds = m_ServerTimeoutMilliseconds
        };
    }
}
