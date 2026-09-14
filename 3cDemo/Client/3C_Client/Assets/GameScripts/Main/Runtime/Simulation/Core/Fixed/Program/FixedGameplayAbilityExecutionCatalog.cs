using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedGameplayAbilityExecutionCatalog
    {
        readonly ReadOnlyCollection<FixedGameplayAbilityExecutionData> m_Abilities;
        readonly Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionData> m_ById;

        public FixedGameplayAbilityExecutionCatalog(IEnumerable<FixedGameplayAbilityExecutionData> abilities)
        {
            var values = new List<FixedGameplayAbilityExecutionData>(abilities ?? throw new ArgumentNullException(nameof(abilities)));
            values.Sort((left, right) => left.AbilityId.CompareTo(right.AbilityId));
            m_ById = new Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionData>();
            for (int i = 0; i < values.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = values[i] ??
                    throw new ArgumentException("Fixed Ability execution catalog contains a missing entry.", nameof(abilities));
                if (!m_ById.TryAdd(ability.AbilityId, ability))
                    throw new ArgumentException($"Fixed Ability execution catalog contains duplicate AbilityId '{ability.AbilityId}'.", nameof(abilities));
            }
            m_Abilities = values.AsReadOnly();
        }

        public IReadOnlyList<FixedGameplayAbilityExecutionData> Abilities => m_Abilities;

        public FixedGameplayAbilityExecutionData Require(CharacterSkillId abilityId)
        {
            if (!m_ById.TryGetValue(abilityId, out FixedGameplayAbilityExecutionData ability))
                throw new InvalidOperationException($"Fixed Ability execution data '{abilityId}' is absent.");
            return ability;
        }
    }

    internal static class FixedGameplayAbilityExecutionCatalogFactory
    {
        public static FixedGameplayAbilityExecutionCatalog FromProgram(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract
                .Create(program.CatalogEntries, index => program.Constants[index].Int32);
            var values = new List<FixedGameplayAbilityExecutionData>(program.AbilityPrograms.Bindings.Count);
            for (int i = 0; i < program.AbilityPrograms.Bindings.Count; i++)
                values.Add(FixedGameplayAbilityExecutionDataFactory.Create(
                    program,
                    program.AbilityPrograms.Bindings[i].SkillId,
                    providerContract));
            return new FixedGameplayAbilityExecutionCatalog(values);
        }
    }
}
