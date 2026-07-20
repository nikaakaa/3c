using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32ProgramFinalizePass", menuName = "3C/Simulation/Passes/Float32 Program Finalize")]
    public sealed class Float32ProgramFinalizePassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Step;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.ProgramFinalize;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new Float32ProgramFinalizePassRuntimeFactory();
    }
}
