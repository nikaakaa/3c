using System;
using UnityEngine;
using ThirdPerson.Development.Gm;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [CreateAssetMenu(menuName = "3C/Development/Rollback GM Build Profile")]
    public sealed class RollbackGmBuildProfile : ScriptableObject
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

        public int GmPort => m_GmPort;
        public int RelayQueryPort => m_RelayQueryPort;

        public void RequireValid()
        {
            if (m_GmPort == m_RelayQueryPort || m_ServerTimeoutMilliseconds >= m_ClientTimeoutMilliseconds)
                throw new InvalidOperationException("GM 构建配置中的端口或超时无效。");
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
