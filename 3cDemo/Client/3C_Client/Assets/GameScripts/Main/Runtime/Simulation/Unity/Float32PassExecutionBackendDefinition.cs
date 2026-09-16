using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

using BTSMTL.Timeline.Runtime;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32PassExecutionBackend", menuName = "3C/Simulation/Float32 Pass Execution Backend")]
    public sealed class Float32PassExecutionBackendDefinition :
        SimulationExecutionBackendDefinition
    {
        public override TimelineRuntimeNumericTarget TimelineNumericTarget => TimelineRuntimeNumericTarget.Float32;

        public override SimulationExecutionTargetManifest BuildExecutionTargetManifest() =>
            Float32SimulationTarget.Manifest.ExecutionTarget;

        public override SimulationExecutionBackendDescriptor BuildPortableDescriptor()
        {
            return Float32PassExecutionBackend.Descriptor;
        }

        public override SimulationPipelinePassFactoryCatalog BuildPortableFactoryCatalog(
            SimulationPipelineDefinition pipeline)
        {
            if (pipeline is not IFloat32SimulationPipelineRuntimePackageProvider provider)
                throw new System.InvalidOperationException($"Pipeline Definition '{pipeline?.name}' has no Float32 runtime package provider.");
            return provider.BuildRuntimePackage().PassFactories;
        }

        public override ISimulationSessionCompositionPreparation CreateSessionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations) =>
            new Float32SimulationSessionCompositionPreparation(definition, registrations);
    }
}
