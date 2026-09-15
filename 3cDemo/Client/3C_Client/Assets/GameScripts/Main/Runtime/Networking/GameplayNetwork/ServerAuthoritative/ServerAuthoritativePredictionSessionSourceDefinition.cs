using System;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(fileName = "ServerAuthoritativePredictionSessionSource", menuName = "3C/Networking/Server Authoritative Prediction Source")]
    public sealed class ServerAuthoritativePredictionSessionSourceDefinition : GameplayNetworkModelSessionSourceDefinition
    {
        public const string ComponentId = "thirdperson.session-source.server-authoritative-prediction";
        public const string SemanticVersion = "2";

        [SerializeField] ServerAuthoritativeSessionConfigurationDefinition m_Configuration;
        [SerializeField] ServerAuthoritativeLaunchDefinition m_Launch;

        public ServerAuthoritativeSessionConfigurationDefinition Configuration => m_Configuration
            ? m_Configuration
            : throw new InvalidOperationException($"Prediction Source '{name}' requires an explicit Session Configuration.");
        public ServerAuthoritativeLaunchDefinition Launch => m_Launch
            ? m_Launch
            : throw new InvalidOperationException($"Prediction Source '{name}' requires an explicit Launch Definition.");

        protected override GameplayNetworkModelSourceRequirements BuildRequirements()
        {
            ServerAuthoritativeProcessIdentity process = Launch.BuildProcessIdentity();
            if (process.Role == ServerAuthoritativeProcessRole.AuthorityWorker)
                throw new InvalidOperationException($"Prediction Source '{name}' requires a Client launch role.");
            return Configuration.BuildPredictionSourceRequirements();
        }

        protected override ISimulationSessionSourcePreparation CreateModelPreparation(
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements) =>
            new ServerAuthoritativePredictionSourcePreparation(Configuration, Launch, context, requirements);

#if UNITY_EDITOR
        public void SetAuthoring(ServerAuthoritativeSessionConfigurationDefinition configuration, ServerAuthoritativeLaunchDefinition launch)
        {
            m_Configuration = configuration ? configuration : throw new ArgumentNullException(nameof(configuration));
            m_Launch = launch ? launch : throw new ArgumentNullException(nameof(launch));
            _ = BuildAuthoringDescriptor();
        }
#endif
    }
}
