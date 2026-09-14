using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32AbilityEvaluatePass", menuName = "3C/Simulation/Passes/Float32 Ability Evaluate")]
    public sealed class Float32AbilityEvaluatePassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Step;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.AbilityEvaluate;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new Float32AbilityEvaluatePassRuntimeFactory();
    }
}
