using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        [SerializeField] GameplayAbilityDataAsset[] m_Float32AbilityData = Array.Empty<GameplayAbilityDataAsset>();
        [SerializeField] FixedGameplayAbilityDataAsset[] m_FixedAbilityData = Array.Empty<FixedGameplayAbilityDataAsset>();

        public IReadOnlyList<GameplayAbilityDataAsset> Float32AbilityData =>
            m_Float32AbilityData ?? Array.Empty<GameplayAbilityDataAsset>();
        public IReadOnlyList<FixedGameplayAbilityDataAsset> FixedAbilityData =>
            m_FixedAbilityData ?? Array.Empty<FixedGameplayAbilityDataAsset>();

        public GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> LoadFloat32AbilitySet()
        {
            RequireAbilityDataCoverage(Float32AbilityData, asset => asset?.AbilityId, "Float32");
            return LoadFloat32GameplayAbilities(Float32AbilityData);
        }

        public GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> LoadFixedAbilitySet()
        {
            RequireAbilityDataCoverage(FixedAbilityData, asset => asset?.AbilityId, "Fixed");
            return this.LoadFixedGameplayAbilities(FixedAbilityData);
        }

#if UNITY_EDITOR
        public void SetFloat32AbilityData(IEnumerable<GameplayAbilityDataAsset> assets)
        {
            m_Float32AbilityData = (assets ?? Enumerable.Empty<GameplayAbilityDataAsset>()).ToArray();
        }

        public void SetFixedAbilityData(IEnumerable<FixedGameplayAbilityDataAsset> assets)
        {
            m_FixedAbilityData = (assets ?? Enumerable.Empty<FixedGameplayAbilityDataAsset>()).ToArray();
        }
#endif

        void RequireAbilityDataCoverage<TAsset>(
            IReadOnlyList<TAsset> assets,
            Func<TAsset, string> readAbilityId,
            string numericTarget)
            where TAsset : UnityEngine.Object
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < AbilityGrants.Count; i++)
            {
                AbilityGrant grant = AbilityGrants[i];
                if (grant == null || string.IsNullOrEmpty(grant.AbilityId) || !expected.Add(grant.AbilityId))
                    throw new InvalidOperationException($"Character Pipeline Definition '{name}' has an invalid or duplicated Ability grant.");
            }
            if (expected.Count != assets.Count)
                throw new InvalidOperationException(
                    $"Character Pipeline Definition '{name}' requires one {numericTarget} Ability Data asset per Ability grant.");
            for (int i = 0; i < assets.Count; i++)
            {
                TAsset asset = assets[i];
                string abilityId = asset ? readAbilityId(asset) : string.Empty;
                if (string.IsNullOrEmpty(abilityId) || !expected.Remove(abilityId))
                    throw new InvalidOperationException(
                        $"Character Pipeline Definition '{name}' has an unexpected or duplicated {numericTarget} Ability Data asset.");
            }
            if (expected.Count != 0)
                throw new InvalidOperationException(
                    $"Character Pipeline Definition '{name}' is missing a {numericTarget} Ability Data asset.");
        }
    }
}
