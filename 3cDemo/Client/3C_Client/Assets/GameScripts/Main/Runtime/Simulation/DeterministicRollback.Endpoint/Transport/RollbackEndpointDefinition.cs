using System;
using System.Net;

namespace ThirdPersonSimulation.DeterministicRollback
{
    static class RollbackEndpointIdentity
    {
        public static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Rollback endpoint identity is invalid.", parameterName);
            return value;
        }
    }

    public sealed class RollbackEndpointDefinition
    {
        public RollbackEndpointDefinition(
            string address,
            int port,
            string sessionId,
            int maximumDatagramBytes,
            int maximumQueuedMessages,
            int maximumFragmentsPerMessage,
            int reliableResendMilliseconds)
        {
            if (!IPAddress.TryParse(address, out IPAddress parsed) || port <= 0 || port > 65535 ||
                maximumDatagramBytes < 256 || maximumDatagramBytes > 1200 || maximumQueuedMessages <= 0 ||
                maximumFragmentsPerMessage <= 0 || maximumFragmentsPerMessage > 4096 ||
                reliableResendMilliseconds <= 0)
            {
                throw new ArgumentException("Rollback Endpoint definition is invalid.");
            }
            Address = parsed;
            Port = port;
            SessionId = new SimulationSessionId(sessionId).Value;
            MaximumDatagramBytes = maximumDatagramBytes;
            MaximumQueuedMessages = maximumQueuedMessages;
            MaximumFragmentsPerMessage = maximumFragmentsPerMessage;
            ReliableResendMilliseconds = reliableResendMilliseconds;
            ConfigurationHash = ComputeConfigurationHash(
                maximumDatagramBytes,
                maximumQueuedMessages,
                maximumFragmentsPerMessage,
                reliableResendMilliseconds);
            Identity = new SimulationComponentIdentity(
                SimulationComponentRole.Endpoint,
                DeterministicRollbackModelIdentity.EndpointId,
                DeterministicRollbackModelIdentity.EndpointVersion,
                ConfigurationHash);
        }

        public IPAddress Address { get; }
        public int Port { get; }
        public string SessionId { get; }
        public int MaximumDatagramBytes { get; }
        public int MaximumQueuedMessages { get; }
        public int MaximumFragmentsPerMessage { get; }
        public int ReliableResendMilliseconds { get; }
        public StableHash ConfigurationHash { get; }
        public SimulationComponentIdentity Identity { get; }

        public static StableHash ComputeConfigurationHash(
            int maximumDatagramBytes,
            int maximumQueuedMessages,
            int maximumFragmentsPerMessage,
            int reliableResendMilliseconds) => StableHash.Compute(
            "deterministic-rollback-endpoint/3",
            maximumDatagramBytes.ToString(),
            maximumQueuedMessages.ToString(),
            maximumFragmentsPerMessage.ToString(),
            reliableResendMilliseconds.ToString());
    }
}
