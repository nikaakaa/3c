using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
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
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilityData)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Actor identity is invalid.", nameof(actorId));
            ActorId = actorId;
            WorldBodyBindingId = SimulationIdentity.Require(worldBodyBindingId, nameof(worldBodyBindingId));
            ControlRuntimeBinding = controlRuntimeBinding ?? throw new ArgumentNullException(nameof(controlRuntimeBinding));
            BodyMotionBinding = bodyMotionBinding ?? throw new ArgumentNullException(nameof(bodyMotionBinding));
            AbilityData = abilityData ?? throw new ArgumentNullException(nameof(abilityData));
            bool requiresGameplayEffects = false;
            bool requiresEquipment = false;
            for (int i = 0; i < abilityData.Data.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = abilityData.Data[i];
                if (data.NumericProfile != FixedSimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Ability '{data.AbilityId}' does not target Fixed.");
                requiresGameplayEffects |= data.Capabilities.HasGameplayCapability("GameplayEffect");
                requiresEquipment |= data.Capabilities.HasGameplayCapability("Equipment");
            }
            if (requiresGameplayEffects != (gameplayEffectRuntimeBinding != null))
                throw new ArgumentException("Fixed Actor Gameplay Effect binding does not match installed Ability capabilities.", nameof(gameplayEffectRuntimeBinding));
            if (requiresEquipment != (equipmentRuntimeBinding != null))
                throw new ArgumentException("Fixed Actor Equipment binding does not match installed Ability capabilities.", nameof(equipmentRuntimeBinding));
            GameplayEffectRuntimeBinding = gameplayEffectRuntimeBinding;
            EquipmentRuntimeBinding = equipmentRuntimeBinding;
            AbilityInstallations = new FixedGameplayAbilityExecutionInstallationSet(
                abilityData,
                gameplayEffectRuntimeBinding);
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
        public GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> AbilityData { get; }
        public FixedGameplayAbilityExecutionInstallationSet AbilityInstallations { get; }
        public StableHash GameplayContentHash { get; }

        static StableHash ComputeGameplayContentHash(
            CharacterControlRuntimeBinding control,
            CharacterBodyMotionBinding bodyMotion,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilities)
        {
            var parts = new List<string>
            {
                "fixed-simulation-actor-content/1",
                control.BindingHash.ToString(),
                bodyMotion.BindingHash.ToString(),
                gameplayEffects?.BindingHash.ToString() ?? string.Empty,
                equipment?.BindingHash.ToString() ?? string.Empty
            };
            for (int i = 0; i < abilities.Data.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = abilities.Data[i];
                parts.Add(data.AbilityId.Value);
                parts.Add(data.ContentHash.ToString());
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.ExecutionIdentity);
            }
            return StableHash.Compute(parts.ToArray());
        }
    }
}
