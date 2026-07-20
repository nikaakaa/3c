using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32WorldResolveBatchPass", menuName = "3C/Simulation/Passes/Float32 World Resolve Batch")]
    public sealed class Float32WorldResolveBatchPassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Step;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.WorldResolveBatch;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new Float32WorldResolveBatchPassRuntimeFactory();
    }
}
