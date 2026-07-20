using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativeOwnerInputIngressPass", menuName = "3C/Simulation/Passes/Server Authoritative/Owner Input Ingress")]
    public sealed class ServerAuthoritativeOwnerInputIngressPassDefinition : ServerAuthoritativePipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Ingress;
        protected override SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy) => ServerAuthoritativePipelinePassContracts.OwnerInputIngress(policy);
        protected override IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy) => new ServerAuthoritativeOwnerInputIngressPassRuntimeFactory(policy);
    }
}
