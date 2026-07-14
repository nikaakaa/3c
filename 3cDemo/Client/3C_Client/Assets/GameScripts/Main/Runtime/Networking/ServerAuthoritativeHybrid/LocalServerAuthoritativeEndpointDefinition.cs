using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    [CreateAssetMenu(
        fileName = "LocalServerAuthoritativeEndpointDefinition",
        menuName = "3C/Networking/Server Authoritative Local Loopback Endpoint")]
    public sealed class LocalServerAuthoritativeEndpointDefinition : ServerAuthoritativeEndpointDefinition
    {
        [SerializeField] LocalServerAuthoritativeEndpointSettings m_Settings = new LocalServerAuthoritativeEndpointSettings();

        public override string EndpointId => LocalServerAuthoritativeEndpoint.StableEndpointId;
        public LocalServerAuthoritativeEndpointSettings Settings => m_Settings;

        public override bool CollectConfigurationErrors(List<string> errors)
        {
            if (m_Settings != null)
                return m_Settings.CollectConfigurationErrors(errors);

            errors?.Add($"{name}: LocalLoopback endpoint settings are missing.");
            return false;
        }

        public override IServerAuthoritativeEndpoint CreateEndpoint()
        {
            return new LocalServerAuthoritativeEndpoint(m_Settings);
        }
    }
}
