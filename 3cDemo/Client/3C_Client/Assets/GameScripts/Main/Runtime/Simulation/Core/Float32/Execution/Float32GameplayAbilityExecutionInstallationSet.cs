using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public sealed class Float32GameplayAbilityExecutionInstallationSet : IFloat32AbilityInstallationProvider
    {
        readonly ReadOnlyCollection<Float32GameplayAbilityExecutionInstallation> m_Installations;
        readonly Dictionary<CharacterSkillId, Float32GameplayAbilityExecutionInstallation> m_ByAbility;

        public Float32GameplayAbilityExecutionInstallationSet(
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> data,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding,
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            var values = new List<Float32GameplayAbilityExecutionInstallation>();
            m_ByAbility = new Dictionary<CharacterSkillId, Float32GameplayAbilityExecutionInstallation>();
            bool hasGameplayEffectAbility = false;
            bool hasEquipmentAbility = false;
            for (int i = 0; i < data.Data.Count; i++)
            {
                Float32GameplayAbilityExecutionData ability = data.Data[i];
                if (ability == null)
                    throw new ArgumentException("Ability execution data contains a missing Ability.", nameof(data));
                hasGameplayEffectAbility |= ability.Capabilities.HasGameplayCapability("GameplayEffect");
                hasEquipmentAbility |= ability.Capabilities.HasGameplayCapability("Equipment");
            }
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog = hasGameplayEffectAbility
                ? new Float32GameplayEffectRuntimeCatalog(gameplayEffectBinding ??
                    throw new ArgumentNullException(nameof(gameplayEffectBinding)))
                : null;
            if (hasEquipmentAbility && equipmentRuntimeBinding == null)
                throw new ArgumentNullException(nameof(equipmentRuntimeBinding));
            bool requiresGameplayEffects = false;
            bool requiresEquipment = false;
            for (int i = 0; i < data.Data.Count; i++)
            {
                Float32GameplayAbilityExecutionData ability = data.Data[i];
                var installation = new Float32GameplayAbilityExecutionInstallation(
                    ability,
                    gameplayEffectCatalog,
                    equipmentRuntimeBinding);
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

        public IReadOnlyList<Float32GameplayAbilityExecutionInstallation> Installations => m_Installations;
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }
        internal Float32GameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }

        public Float32GameplayAbilityExecutionInstallation Require(CharacterSkillId abilityId) =>
            abilityId.IsValid && m_ByAbility.TryGetValue(abilityId, out Float32GameplayAbilityExecutionInstallation installation)
                ? installation
                : throw new InvalidOperationException($"Float32 Ability '{abilityId}' is not installed.");

        public bool TryGet(CharacterSkillId abilityId, out Float32GameplayAbilityExecutionInstallation installation)
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
