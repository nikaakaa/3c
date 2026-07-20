using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativeRemotePresentationEgressPass", menuName = "3C/Simulation/Passes/Server Authoritative/Remote Presentation Egress")]
    public sealed class ServerAuthoritativeRemotePresentationEgressPassDefinition : ServerAuthoritativePipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Egress;
        protected override SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy) => ServerAuthoritativePipelinePassContracts.RemotePresentationEgress(policy);
        protected override IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy) => new RemotePresentationEgressPassRuntimeFactory(policy);
    }
}
