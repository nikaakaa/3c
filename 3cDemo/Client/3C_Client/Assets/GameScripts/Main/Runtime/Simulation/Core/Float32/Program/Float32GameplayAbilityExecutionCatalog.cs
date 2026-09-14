using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    internal sealed class Float32GameplayAbilityExecutionCatalog
    {
        readonly ReadOnlyCollection<Float32GameplayAbilityExecutionData> m_Abilities;
        readonly Dictionary<CharacterSkillId, Float32GameplayAbilityExecutionData> m_ById;

        public Float32GameplayAbilityExecutionCatalog(IEnumerable<Float32GameplayAbilityExecutionData> abilities)
        {
            var values = new List<Float32GameplayAbilityExecutionData>(abilities ?? throw new ArgumentNullException(nameof(abilities)));
            values.Sort((left, right) => left.AbilityId.CompareTo(right.AbilityId));
            m_ById = new Dictionary<CharacterSkillId, Float32GameplayAbilityExecutionData>();
            for (int i = 0; i < values.Count; i++)
            {
                Float32GameplayAbilityExecutionData ability = values[i] ??
                    throw new ArgumentException("Float32 Ability execution catalog contains a missing entry.", nameof(abilities));
                if (!m_ById.TryAdd(ability.AbilityId, ability))
                    throw new ArgumentException($"Float32 Ability execution catalog contains duplicate AbilityId '{ability.AbilityId}'.", nameof(abilities));
            }
            m_Abilities = values.AsReadOnly();
        }

        public IReadOnlyList<Float32GameplayAbilityExecutionData> Abilities => m_Abilities;

        public Float32GameplayAbilityExecutionData Require(CharacterSkillId abilityId)
        {
            if (!m_ById.TryGetValue(abilityId, out Float32GameplayAbilityExecutionData ability))
                throw new InvalidOperationException($"Float32 Ability execution data '{abilityId}' is absent.");
            return ability;
        }
    }

    internal static class Float32GameplayAbilityExecutionCatalogFactory
    {
        public static Float32GameplayAbilityExecutionCatalog FromProgram(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract
                .Create(program.CatalogEntries, index => program.Constants[index].Int32);
            var values = new List<Float32GameplayAbilityExecutionData>(program.AbilityPrograms.Bindings.Count);
            for (int i = 0; i < program.AbilityPrograms.Bindings.Count; i++)
                values.Add(Float32GameplayAbilityExecutionDataFactory.Create(
                    program,
                    program.AbilityPrograms.Bindings[i].SkillId,
                    providerContract));
            return new Float32GameplayAbilityExecutionCatalog(values);
        }
    }
}
