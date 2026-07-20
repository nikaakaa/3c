using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativePredictionOutputDispositionPass", menuName = "3C/Simulation/Passes/Server Authoritative/Prediction Output Disposition")]
    public sealed class ServerAuthoritativePredictionOutputDispositionPassDefinition : ServerAuthoritativePipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Egress;
        protected override SimulationPipelinePassDescriptor BuildCanonicalDescriptor(ServerAuthoritativeModelPolicy policy) => ServerAuthoritativePipelinePassContracts.OutputDisposition(policy);
        protected override IFloat32PipelinePassRuntimeFactory BuildRuntimeFactory(ServerAuthoritativeModelPolicy policy) => new PredictionOutputDispositionPassRuntimeFactory(policy);
    }
}
