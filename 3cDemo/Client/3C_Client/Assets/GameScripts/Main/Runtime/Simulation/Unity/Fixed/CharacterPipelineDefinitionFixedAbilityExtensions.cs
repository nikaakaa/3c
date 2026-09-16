using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public static class CharacterPipelineDefinitionFixedAbilityExtensions
    {
        public static GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> LoadFixedAbilitySet(
            this CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            var assets = new List<FixedGameplayAbilityDataAsset>(
                definition.FixedAbilityData.Count);
            for (int i = 0; i < definition.FixedAbilityData.Count; i++)
            {
                UnityEngine.Object value = definition.FixedAbilityData[i];
                if (!(value is FixedGameplayAbilityDataAsset asset))
                {
                    throw new InvalidOperationException(
                        $"Character Pipeline Definition '{definition.name}' Fixed Ability Data entry #{i} is not a Fixed Gameplay Ability Data asset.");
                }
                assets.Add(asset);
            }
            RequireAbilityDataCoverage(definition, assets);
            return definition.LoadFixedAbilityExecutionDataSet(assets);
        }

        public static FixedGameplayAbilityExecutionData LoadFixedAbilityExecutionData(
            this CharacterPipelineDefinition definition,
            FixedGameplayAbilityDataAsset asset)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            return asset.Load(definition.BuildGameplayAbilityProviderBinding());
        }

        public static GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> LoadFixedAbilityExecutionDataSet(
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

        static void RequireAbilityDataCoverage(
            CharacterPipelineDefinition definition,
            IReadOnlyList<FixedGameplayAbilityDataAsset> assets)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definition.AbilityGrants.Count; i++)
            {
                AbilityGrant grant = definition.AbilityGrants[i];
                if (grant == null || string.IsNullOrEmpty(grant.AbilityId) ||
                    !expected.Add(grant.AbilityId))
                {
                    throw new InvalidOperationException(
                        $"Character Pipeline Definition '{definition.name}' has an invalid or duplicated Ability grant.");
                }
            }
            if (expected.Count != assets.Count)
            {
                throw new InvalidOperationException(
                    $"Character Pipeline Definition '{definition.name}' requires one Fixed Ability Data asset per Ability grant.");
            }
            for (int i = 0; i < assets.Count; i++)
            {
                if (!expected.Remove(assets[i].AbilityId))
                {
                    throw new InvalidOperationException(
                        $"Character Pipeline Definition '{definition.name}' has an unexpected or duplicated Fixed Ability Data asset.");
                }
            }
            if (expected.Count != 0)
            {
                throw new InvalidOperationException(
                    $"Character Pipeline Definition '{definition.name}' is missing a Fixed Ability Data asset.");
            }
        }
    }
}
