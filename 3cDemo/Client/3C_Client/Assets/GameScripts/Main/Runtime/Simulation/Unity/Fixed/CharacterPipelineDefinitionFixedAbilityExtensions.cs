using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;

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

        public static GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> LoadFixedGameplayAbilities(
            this CharacterPipelineDefinition definition,
            IEnumerable<FixedGameplayAbilityDataAsset> assets)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));
            GameplayAbilityProviderBinding providerBinding = definition.BuildGameplayAbilityProviderBinding();
            var data = new List<FixedGameplayAbilityExecutionData>();
            foreach (FixedGameplayAbilityDataAsset asset in assets)
                data.Add(asset.Load(providerBinding));
            return new GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData>(data, value => value.AbilityId);
        }
    }
}
