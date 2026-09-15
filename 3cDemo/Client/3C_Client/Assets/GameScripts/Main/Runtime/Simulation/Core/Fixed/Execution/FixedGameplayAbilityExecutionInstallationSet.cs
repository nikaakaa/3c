using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionInstallationSet : IFixedAbilityActionBindingProvider
    {
        readonly ReadOnlyCollection<FixedGameplayAbilityExecutionInstallation> m_Installations;
        readonly Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionInstallation> m_ByAbility;

        public FixedGameplayAbilityExecutionInstallationSet(
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> data,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding,
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var values = new List<FixedGameplayAbilityExecutionInstallation>();
            m_ByAbility = new Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionInstallation>();
            bool hasGameplayEffectAbility = false;
            bool hasEquipmentAbility = false;
            for (int i = 0; i < data.Data.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = data.Data[i];
                if (ability == null)
                    throw new ArgumentException("Ability execution data contains a missing Ability.", nameof(data));
                hasGameplayEffectAbility |= ability.Capabilities.HasGameplayCapability("GameplayEffect");
                hasEquipmentAbility |= ability.Capabilities.HasGameplayCapability("Equipment");
            }
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog = hasGameplayEffectAbility
                ? new FixedGameplayEffectRuntimeCatalog(gameplayEffectBinding ??
                    throw new ArgumentNullException(nameof(gameplayEffectBinding)))
                : null;
            if (hasEquipmentAbility && equipmentRuntimeBinding == null)
                throw new ArgumentNullException(nameof(equipmentRuntimeBinding));
            bool requiresGameplayEffects = false;
            bool requiresEquipment = false;
            for (int i = 0; i < data.Data.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = data.Data[i];
                EquipmentProgramLayout equipmentLayout = ability.Capabilities.HasGameplayCapability("Equipment")
                    ? EquipmentProgramLayoutCompiler.Compile(
                        equipmentRuntimeBinding,
                        ability.CatalogEntries,
                        ability.References,
                        ability.Producers)
                    : null;
                var installation = new FixedGameplayAbilityExecutionInstallation(
                    ability,
                    gameplayEffectCatalog,
                    equipmentLayout);
                requiresGameplayEffects |= installation.RequiresGameplayEffects;
                requiresEquipment |= installation.RequiresEquipment;
                values.Add(installation);
                m_ByAbility.Add(installation.Data.AbilityId, installation);
            }
            values.Sort((left, right) => left.Data.AbilityId.CompareTo(right.Data.AbilityId));
            m_Installations = values.AsReadOnly();
            RequiresGameplayEffects = requiresGameplayEffects;
            RequiresEquipment = requiresEquipment;
            GameplayEffectCatalog = gameplayEffectCatalog;
        }

        public IReadOnlyList<FixedGameplayAbilityExecutionInstallation> Installations => m_Installations;
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }
        internal FixedGameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }

        public FixedGameplayAbilityExecutionInstallation Require(CharacterSkillId abilityId) =>
            abilityId.IsValid && m_ByAbility.TryGetValue(abilityId, out FixedGameplayAbilityExecutionInstallation installation)
                ? installation
                : throw new InvalidOperationException($"Fixed Ability '{abilityId}' is not installed.");

        public GameplayAbilityExecutionBinding RequireActionBinding(CharacterSkillId abilityId) =>
            Require(abilityId).Data.Binding;

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
