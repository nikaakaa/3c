using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "LocalImmediateOutputPass", menuName = "3C/Simulation/Passes/Local Immediate Output")]
    public sealed class LocalImmediateOutputPassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Egress;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.LocalImmediateOutput;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new LocalImmediateOutputPassRuntimeFactory();
    }
}
