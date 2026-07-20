using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "LocalSingleStepSchedulePass", menuName = "3C/Simulation/Passes/Local Single Step Schedule")]
    public sealed class LocalSingleStepSchedulePassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Schedule;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.LocalSingleStepSchedule;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new LocalSingleStepSchedulePassRuntimeFactory();
    }
}
