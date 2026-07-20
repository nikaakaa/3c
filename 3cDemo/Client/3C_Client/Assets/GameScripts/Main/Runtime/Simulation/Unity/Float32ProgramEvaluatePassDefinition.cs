using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32ProgramEvaluatePass", menuName = "3C/Simulation/Passes/Float32 Program Evaluate")]
    public sealed class Float32ProgramEvaluatePassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Step;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.ProgramEvaluate;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new Float32ProgramEvaluatePassRuntimeFactory();
    }
}
