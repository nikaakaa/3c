using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public abstract class ServerAuthoritativeEndpointDefinition : ScriptableObject
    {
        public abstract string EndpointId { get; }
        public abstract bool CollectConfigurationErrors(List<string> errors);
        public abstract IServerAuthoritativeEndpoint CreateEndpoint();
    }
}
