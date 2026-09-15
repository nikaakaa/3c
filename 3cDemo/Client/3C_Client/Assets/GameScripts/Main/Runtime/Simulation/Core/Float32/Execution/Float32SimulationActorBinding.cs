using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class SimulationActorBinding
    {
        public SimulationActorBinding(
            ActorId actorId,
            string worldBodyBindingId,
            CharacterControlRuntimeBinding controlRuntimeBinding,
            CharacterBodyMotionBinding bodyMotionBinding,
            CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding,
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding,
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Actor identity is invalid.", nameof(actorId));
            ActorId = actorId;
            WorldBodyBindingId = SimulationIdentity.Require(worldBodyBindingId, nameof(worldBodyBindingId));
            ControlRuntimeBinding = controlRuntimeBinding ?? throw new ArgumentNullException(nameof(controlRuntimeBinding));
            BodyMotionBinding = bodyMotionBinding ?? throw new ArgumentNullException(nameof(bodyMotionBinding));
            AbilityData = abilityData ?? throw new ArgumentNullException(nameof(abilityData));
            for (int i = 0; i < abilityData.Data.Count; i++)
            {
                Float32GameplayAbilityExecutionData data = abilityData.Data[i];
                if (data.NumericProfile != Float32SimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Ability '{data.AbilityId}' does not target Float32.");
            }
            GameplayEffectRuntimeBinding = gameplayEffectRuntimeBinding;
            EquipmentRuntimeBinding = equipmentRuntimeBinding;
            AbilityInstallations = new Float32GameplayAbilityExecutionInstallationSet(
                abilityData,
                gameplayEffectRuntimeBinding);
            RequiresGameplayEffects = AbilityInstallations.RequiresGameplayEffects;
            if (AbilityInstallations.RequiresEquipment && equipmentRuntimeBinding == null)
                throw new ArgumentException("Float32 Actor Equipment service is required by an installed Ability.", nameof(equipmentRuntimeBinding));
            RequiresEquipment = AbilityInstallations.RequiresEquipment;
            GameplayContentHash = ComputeGameplayContentHash(
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                abilityData);
        }

        public ActorId ActorId { get; }
        public string WorldBodyBindingId { get; }
        public CharacterControlRuntimeBinding ControlRuntimeBinding { get; }
        public CharacterBodyMotionBinding BodyMotionBinding { get; }
        public CharacterGameplayEffectRuntimeBinding GameplayEffectRuntimeBinding { get; }
        public CharacterEquipmentRuntimeBinding EquipmentRuntimeBinding { get; }
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }
        public GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> AbilityData { get; }
        public Float32GameplayAbilityExecutionInstallationSet AbilityInstallations { get; }
        public StableHash GameplayContentHash { get; }

        static StableHash ComputeGameplayContentHash(
            CharacterControlRuntimeBinding control,
            CharacterBodyMotionBinding bodyMotion,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilities)
        {
            var parts = new List<string>
            {
                "float32-simulation-actor-content/1",
                control.BindingHash.ToString(),
                bodyMotion.BindingHash.ToString(),
                gameplayEffects?.BindingHash.ToString() ?? string.Empty,
                equipment?.BindingHash.ToString() ?? string.Empty
            };
            for (int i = 0; i < abilities.Data.Count; i++)
            {
                Float32GameplayAbilityExecutionData data = abilities.Data[i];
                parts.Add(data.AbilityId.Value);
                parts.Add(data.ContentHash.ToString());
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.ExecutionIdentity);
            }
            return StableHash.Compute(parts.ToArray());
        }
    }
}
