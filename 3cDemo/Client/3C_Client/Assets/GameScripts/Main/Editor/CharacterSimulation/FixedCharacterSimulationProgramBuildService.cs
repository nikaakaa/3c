using System;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation.Fixed;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class FixedCharacterSimulationProgramBuildService
    {
        public static FixedCharacterSimulationProgramAsset Build(
            CharacterPipelineDefinition definition,
            string unityWrapperDestination)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            var request = new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                new ICharacterSimulationTargetBuildAdapter[]
                {
                    new FixedCharacterSimulationTargetBuildAdapter(unityWrapperDestination)
                });
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(request);
            if (!result.IsValid)
                throw new InvalidOperationException("Fixed Character Simulation build failed.");
            return AssetDatabase.LoadAssetAtPath<FixedCharacterSimulationProgramAsset>(unityWrapperDestination) ??
                throw new InvalidOperationException("Published Fixed Character Program wrapper is missing.");
        }
    }
}
