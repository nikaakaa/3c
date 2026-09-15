using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionInstallationSet
    {
        readonly ReadOnlyCollection<FixedGameplayAbilityExecutionInstallation> m_Installations;
        readonly Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionInstallation> m_ByAbility;

        public FixedGameplayAbilityExecutionInstallationSet(
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> data,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var values = new List<FixedGameplayAbilityExecutionInstallation>();
            m_ByAbility = new Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionInstallation>();
            bool requiresGameplayEffects = false;
            bool requiresEquipment = false;
            for (int i = 0; i < data.Data.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = data.Data[i];
                bool abilityRequiresGameplayEffects = ability.Capabilities.HasGameplayCapability("GameplayEffect");
                requiresGameplayEffects |= abilityRequiresGameplayEffects;
                requiresEquipment |= ability.Capabilities.HasGameplayCapability("Equipment");
                CharacterGameplayEffectRuntimeBinding abilityEffectBinding = abilityRequiresGameplayEffects
                    ? gameplayEffectBinding ?? throw new ArgumentNullException(nameof(gameplayEffectBinding))
                    : null;
                var installation = new FixedGameplayAbilityExecutionInstallation(ability, abilityEffectBinding);
                values.Add(installation);
                m_ByAbility.Add(installation.Data.AbilityId, installation);
            }
            values.Sort((left, right) => left.Data.AbilityId.CompareTo(right.Data.AbilityId));
            m_Installations = values.AsReadOnly();
            RequiresGameplayEffects = requiresGameplayEffects;
            RequiresEquipment = requiresEquipment;
        }

        public IReadOnlyList<FixedGameplayAbilityExecutionInstallation> Installations => m_Installations;
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }

        public FixedGameplayAbilityExecutionInstallation Require(CharacterSkillId abilityId) =>
            abilityId.IsValid && m_ByAbility.TryGetValue(abilityId, out FixedGameplayAbilityExecutionInstallation installation)
                ? installation
                : throw new InvalidOperationException($"Fixed Ability '{abilityId}' is not installed.");

        public bool TryGet(CharacterSkillId abilityId, out FixedGameplayAbilityExecutionInstallation installation)
        {
            if (!abilityId.IsValid)
            {
                installation = null;
                return false;
            }
            return m_ByAbility.TryGetValue(abilityId, out installation);
        }
    }
}
