using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativeInputCommandEgressPass", menuName = "3C/Simulation/Passes/Server Authoritative/Input Command Egress")]
    public sealed class ServerAuthoritativeInputCommandEgressPassDefinition : ServerAuthoritativePipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Egress;
        protected override SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy) => ServerAuthoritativePipelinePassContracts.InputCommandEgress(policy);
        protected override IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy) => new PredictionInputCommandEgressPassRuntimeFactory(policy);
    }
}
