using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativePredictionHistoryEgressPass", menuName = "3C/Simulation/Passes/Server Authoritative/Prediction History Egress")]
    public sealed class ServerAuthoritativePredictionHistoryEgressPassDefinition : ServerAuthoritativePipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Egress;
        protected override SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy) => ServerAuthoritativePipelinePassContracts.HistoryEgress(policy);
        protected override IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy) => new PredictionHistoryEgressPassRuntimeFactory(policy);
    }
}
