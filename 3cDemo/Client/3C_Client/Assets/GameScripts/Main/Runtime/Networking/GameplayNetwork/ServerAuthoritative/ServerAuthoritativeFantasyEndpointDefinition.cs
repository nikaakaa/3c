using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeFantasyEndpoint",
        menuName = "3C/Networking/Server Authoritative Fantasy Endpoint")]
    public sealed class ServerAuthoritativeFantasyEndpointDefinition : ScriptableObject
    {
        public const string EndpointId = "thirdperson.endpoint.fantasy.server-authoritative-hybrid";
        public const string SemanticVersion = "2";

        [SerializeField] string m_Host = string.Empty;
        [SerializeField, Min(1)] int m_Port;
        [SerializeField, Min(1)] int m_ConnectTimeoutTicks;
        [SerializeField, Min(1)] int m_ControlHeartbeatTicks;
        [SerializeField, Min(1)] int m_ReliableQueueCapacity;
        [SerializeField, Min(1)] int m_DatagramQueueCapacity;

        public string Host => string.IsNullOrWhiteSpace(m_Host)
            ? throw new InvalidOperationException($"Fantasy Endpoint '{name}' requires an explicit host.")
            : m_Host.Trim();
        public int Port => RequirePositive(m_Port, "port");
        public int ConnectTimeoutTicks => RequirePositive(m_ConnectTimeoutTicks, "connect timeout");
        public int ControlHeartbeatTicks => RequirePositive(m_ControlHeartbeatTicks, "control heartbeat cadence");
        public int ReliableQueueCapacity => RequirePositive(m_ReliableQueueCapacity, "reliable queue capacity");
        public int DatagramQueueCapacity => RequirePositive(m_DatagramQueueCapacity, "datagram queue capacity");

        public SimulationComponentIdentity BuildIdentity()
        {
            return new SimulationComponentIdentity(
                SimulationComponentRole.Endpoint,
                EndpointId,
                SemanticVersion,
                StableHash.Compute(
                    "server-authoritative-fantasy-endpoint/2",
                    Host,
                    Port.ToString(),
                    ConnectTimeoutTicks.ToString(),
                    ControlHeartbeatTicks.ToString(),
                    ReliableQueueCapacity.ToString(),
                    DatagramQueueCapacity.ToString(),
                    ServerAuthoritativeModelIdentity.ProtocolId,
                    ServerAuthoritativeModelIdentity.ProtocolVersion.ToString()));
        }

        internal IServerAuthoritativeEndpointConnection CreateConnection(
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeModelPolicy policy,
            CharacterSimulationProgram program,
            ServerAuthoritativeDataPlaneLaunch dataPlane,
            SimulationWorldIdentityDescriptor worldIdentity,
            StableHash modelConfigurationHash,
            ISimulationDiagnosticsSink diagnostics)
        {
            return ServerAuthoritativeFantasyEndpointRuntime.Create(
                this,
                process,
                compatibility,
                policy,
                program,
                dataPlane,
                worldIdentity,
                modelConfigurationHash,
                diagnostics);
        }

        int RequirePositive(int value, string field) => value > 0
            ? value
            : throw new InvalidOperationException($"Fantasy Endpoint '{name}' requires an explicit {field}.");

#if UNITY_EDITOR
        public void SetAuthoring(
            string host,
            int port,
            int connectTimeoutTicks,
            int controlHeartbeatTicks,
            int reliableQueueCapacity,
            int datagramQueueCapacity)
        {
            m_Host = host ?? string.Empty;
            m_Port = port;
            m_ConnectTimeoutTicks = connectTimeoutTicks;
            m_ControlHeartbeatTicks = controlHeartbeatTicks;
            m_ReliableQueueCapacity = reliableQueueCapacity;
            m_DatagramQueueCapacity = datagramQueueCapacity;
            _ = BuildIdentity();
        }
#endif
    }
}
