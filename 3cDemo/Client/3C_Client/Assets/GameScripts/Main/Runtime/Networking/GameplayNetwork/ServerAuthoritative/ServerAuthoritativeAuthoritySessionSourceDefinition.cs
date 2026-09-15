using System;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(fileName = "ServerAuthoritativeAuthoritySessionSource", menuName = "3C/Networking/Server Authoritative Authority Source")]
    public sealed class ServerAuthoritativeAuthoritySessionSourceDefinition : GameplayNetworkModelSessionSourceDefinition
    {
        public const string ComponentId = "thirdperson.session-source.server-authoritative-authority";
        public const string SemanticVersion = "2";

        [SerializeField] ServerAuthoritativeSessionConfigurationDefinition m_Configuration;
        [SerializeField] ServerAuthoritativeLaunchDefinition m_Launch;
        [SerializeField, Min(1)] int m_MaxCatchUpTicksPerPump;
        [SerializeField, Min(1)] int m_MaxClockLagTicks;

        public ServerAuthoritativeSessionConfigurationDefinition Configuration => m_Configuration
            ? m_Configuration
            : throw new InvalidOperationException($"Authority Source '{name}' requires an explicit Session Configuration.");
        public ServerAuthoritativeLaunchDefinition Launch => m_Launch
            ? m_Launch
            : throw new InvalidOperationException($"Authority Source '{name}' requires an explicit Launch Definition.");
        public int MaxCatchUpTicksPerPump => RequirePositive(m_MaxCatchUpTicksPerPump, "maximum catch-up ticks per pump");
        public int MaxClockLagTicks => RequirePositive(m_MaxClockLagTicks, "maximum clock lag ticks");

        public ServerAuthoritativeAuthoritySourcePolicy BuildPolicy()
        {
            if (MaxClockLagTicks < MaxCatchUpTicksPerPump)
                throw new InvalidOperationException($"Authority Source '{name}' clock lag must cover one catch-up pump.");
            return new ServerAuthoritativeAuthoritySourcePolicy(
                Configuration.Policy,
                Configuration.Endpoint.DatagramQueueCapacity,
                Configuration.Endpoint.ReliableQueueCapacity,
                Configuration.Endpoint.ReliableQueueCapacity,
                Configuration.Endpoint.ConnectTimeoutTicks,
                Configuration.Endpoint.ControlHeartbeatTicks,
                MaxCatchUpTicksPerPump,
                MaxClockLagTicks);
        }

        protected override GameplayNetworkModelSourceRequirements BuildRequirements()
        {
            ServerAuthoritativeProcessIdentity process = Launch.BuildProcessIdentity();
            if (process.Role != ServerAuthoritativeProcessRole.AuthorityWorker)
                throw new InvalidOperationException($"Authority Source '{name}' requires an AuthorityWorker launch role.");
            return Configuration.BuildAuthoritySourceRequirements();
        }

        protected override ISimulationSessionSourcePreparation CreateModelPreparation(
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements) =>
            new ServerAuthoritativeAuthoritySourcePreparation(this, context, requirements);

        int RequirePositive(int value, string field) => value > 0
            ? value
            : throw new InvalidOperationException($"Authority Source '{name}' requires an explicit {field}.");

#if UNITY_EDITOR
        public void SetAuthoring(
            ServerAuthoritativeSessionConfigurationDefinition configuration,
            ServerAuthoritativeLaunchDefinition launch,
            int maxCatchUpTicksPerPump,
            int maxClockLagTicks)
        {
            m_Configuration = configuration ? configuration : throw new ArgumentNullException(nameof(configuration));
            m_Launch = launch ? launch : throw new ArgumentNullException(nameof(launch));
            m_MaxCatchUpTicksPerPump = maxCatchUpTicksPerPump;
            m_MaxClockLagTicks = maxClockLagTicks;
            _ = BuildPolicy();
            _ = BuildAuthoringDescriptor();
        }
#endif
    }
}
