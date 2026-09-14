using System;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public static class CharacterPipelineDefinitionFixedAbilityExtensions
    {
        public static FixedGameplayAbilityExecutionData LoadFixedGameplayAbility(
            this CharacterPipelineDefinition definition,
            FixedGameplayAbilityDataAsset asset)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            return asset.Load(definition.BuildGameplayAbilityProviderBinding());
        }
    }
}
