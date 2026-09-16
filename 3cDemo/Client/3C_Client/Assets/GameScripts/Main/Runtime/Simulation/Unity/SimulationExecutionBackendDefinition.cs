using System.Collections.Generic;
using ThirdPersonSimulation;
using BTSMTL.Timeline.Runtime;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class SimulationExecutionBackendDefinition : ScriptableObject
    {
        public abstract TimelineRuntimeNumericTarget TimelineNumericTarget { get; }

        public abstract SimulationExecutionTargetManifest BuildExecutionTargetManifest();
        public abstract SimulationExecutionBackendDescriptor BuildPortableDescriptor();
        public abstract SimulationPipelinePassFactoryCatalog BuildPortableFactoryCatalog(
            SimulationPipelineDefinition pipeline);
        public abstract ISimulationSessionCompositionPreparation CreateSessionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations);
    }

}
