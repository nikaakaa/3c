using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Networking;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeHybridModelDefinition",
        menuName = "3C/Networking/Server Authoritative Hybrid Model")]
    public sealed class ServerAuthoritativeHybridModelDefinition : GameplayNetworkModelDefinition
    {
        [SerializeField] ServerAuthoritativeEndpointDefinition m_EndpointDefinition;
        [SerializeField, Min(1)] int m_OutgoingQueueCapacity = 256;
        [SerializeField, Min(1)] int m_IncomingPerActorQueueCapacity = 128;
        [SerializeField, Min(1)] int m_HistoryCapacity = 256;
        [SerializeField, Min(1)] int m_DebugCapacity = 64;

        public override string ModelId => ServerAuthoritativeHybridSession.StableModelId;
        public ServerAuthoritativeEndpointDefinition EndpointDefinition => m_EndpointDefinition;
        public int OutgoingQueueCapacity => m_OutgoingQueueCapacity;
        public int IncomingPerActorQueueCapacity => m_IncomingPerActorQueueCapacity;
        public int HistoryCapacity => m_HistoryCapacity;
        public int DebugCapacity => m_DebugCapacity;

        public override bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (m_OutgoingQueueCapacity < 1)
            {
                errors?.Add($"{name}: outgoing queue capacity must be positive.");
                valid = false;
            }
            if (m_IncomingPerActorQueueCapacity < 1)
            {
                errors?.Add($"{name}: incoming per-actor queue capacity must be positive.");
                valid = false;
            }
            if (m_HistoryCapacity < 1)
            {
                errors?.Add($"{name}: history capacity must be positive.");
                valid = false;
            }
            if (m_DebugCapacity < 1)
            {
                errors?.Add($"{name}: debug capacity must be positive.");
                valid = false;
            }
            if (m_EndpointDefinition)
            {
                if (string.IsNullOrWhiteSpace(m_EndpointDefinition.EndpointId))
                {
                    errors?.Add($"{name}: endpoint definition '{m_EndpointDefinition.name}' has no endpoint id.");
                    valid = false;
                }
                valid &= m_EndpointDefinition.CollectConfigurationErrors(errors);
            }

            return valid;
        }

        public override IGameplayNetworkModelSession CreateSession()
        {
            var errors = new List<string>();
            if (!CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));

            IServerAuthoritativeEndpoint endpoint = null;
            string endpointId = string.Empty;
            if (m_EndpointDefinition)
            {
                endpointId = m_EndpointDefinition.EndpointId;
                endpoint = m_EndpointDefinition.CreateEndpoint();
                if (endpoint == null)
                    throw new System.InvalidOperationException(
                        $"Endpoint definition '{m_EndpointDefinition.name}' returned no endpoint.");
            }

            return new ServerAuthoritativeHybridSession(
                endpointId,
                endpoint,
                m_OutgoingQueueCapacity,
                m_IncomingPerActorQueueCapacity,
                m_HistoryCapacity,
                m_DebugCapacity);
        }
    }
}
