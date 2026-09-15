using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32AbilityFinalizePass", menuName = "3C/Simulation/Passes/Float32 Ability Finalize")]
    public sealed class Float32AbilityFinalizePassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Step;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.AbilityFinalize;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new Float32AbilityFinalizePassRuntimeFactory();
    }
}
