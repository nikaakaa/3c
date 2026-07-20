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
        public const string SemanticVersion = "1";

        [SerializeField] ServerAuthoritativeHybridModelDefinition m_Model;
        [SerializeField] ServerAuthoritativeLaunchDefinition m_Launch;

        public ServerAuthoritativeHybridModelDefinition Model => m_Model
            ? m_Model
            : throw new InvalidOperationException($"Prediction Source '{name}' requires an explicit Model Definition.");
        public ServerAuthoritativeLaunchDefinition Launch => m_Launch
            ? m_Launch
            : throw new InvalidOperationException($"Prediction Source '{name}' requires an explicit Launch Definition.");

        protected override GameplayNetworkModelSourceRequirements BuildRequirements()
        {
            ServerAuthoritativeProcessIdentity process = Launch.BuildProcessIdentity();
            if (process.Role == ServerAuthoritativeProcessRole.AuthorityWorker)
                throw new InvalidOperationException($"Prediction Source '{name}' requires a Client launch role.");
            return Model.BuildPredictionSourceRequirements();
        }

        protected override ISimulationSessionSourcePreparation CreateModelPreparation(
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements) =>
            new ServerAuthoritativePredictionSourcePreparation(Model, Launch, context, requirements);

#if UNITY_EDITOR
        public void SetAuthoring(ServerAuthoritativeHybridModelDefinition model, ServerAuthoritativeLaunchDefinition launch)
        {
            m_Model = model ? model : throw new ArgumentNullException(nameof(model));
            m_Launch = launch ? launch : throw new ArgumentNullException(nameof(launch));
            _ = BuildAuthoringDescriptor();
        }
#endif
    }
}
