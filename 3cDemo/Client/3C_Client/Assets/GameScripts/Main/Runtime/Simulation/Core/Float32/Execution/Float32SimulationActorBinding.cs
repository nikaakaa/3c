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
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData,
            IAbilityTimelineRuntime timelineRuntime)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Actor identity is invalid.", nameof(actorId));
            ActorId = actorId;
            WorldBodyBindingId = SimulationIdentity.Require(worldBodyBindingId, nameof(worldBodyBindingId));
            ControlRuntimeBinding = controlRuntimeBinding ?? throw new ArgumentNullException(nameof(controlRuntimeBinding));
            BodyMotionBinding = bodyMotionBinding ?? throw new ArgumentNullException(nameof(bodyMotionBinding));
            AbilityInstallations = new Float32GameplayAbilityExecutionInstallationSet(
                abilityData ?? throw new ArgumentNullException(nameof(abilityData)),
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding);
            for (int i = 0; i < AbilityInstallations.Installations.Count; i++)
            {
                Float32GameplayAbilityExecutionData data = AbilityInstallations.Installations[i].Data;
                if (data.NumericProfile != Float32SimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Ability '{data.AbilityId}' does not target Float32.");
            }
            GameplayEffectRuntimeBinding = gameplayEffectRuntimeBinding;
            EquipmentRuntimeBinding = equipmentRuntimeBinding;
            TimelineRuntime = timelineRuntime;
            TimelineMotionReader = timelineRuntime is IAbilityTimelineLogicMotionReader motionReader
                ? motionReader
                : throw new ArgumentException("Float32 Timeline runtime must expose pending logic motion.", nameof(timelineRuntime));
            StateSchemaHash = ComputeStateSchemaHash(
                controlRuntimeBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                AbilityInstallations);
            GameplayContentHash = ComputeGameplayContentHash(
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                AbilityInstallations);
        }

        public ActorId ActorId { get; }
        public string WorldBodyBindingId { get; }
        public CharacterControlRuntimeBinding ControlRuntimeBinding { get; }
        public CharacterBodyMotionBinding BodyMotionBinding { get; }
        public CharacterGameplayEffectRuntimeBinding GameplayEffectRuntimeBinding { get; }
        public CharacterEquipmentRuntimeBinding EquipmentRuntimeBinding { get; }
        public IAbilityTimelineRuntime TimelineRuntime { get; }
        public IAbilityTimelineLogicMotionReader TimelineMotionReader { get; }
        public Float32GameplayAbilityExecutionInstallationSet AbilityInstallations { get; }
        public StableHash StateSchemaHash { get; }
        public StableHash GameplayContentHash { get; }

        static StableHash ComputeStateSchemaHash(
            CharacterControlRuntimeBinding control,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            Float32GameplayAbilityExecutionInstallationSet abilities)
        {
            var parts = new List<string>
            {
                "float32-character-state-schema/1",
                control.BindingHash.ToString(),
                abilities.GameplayEffectCatalog != null ? gameplayEffects.BindingHash.ToString() : string.Empty,
                abilities.RequiresEquipment ? equipment.BindingHash.ToString() : string.Empty
            };
            for (int i = 0; i < abilities.Installations.Count; i++)
            {
                Float32GameplayAbilityExecutionData data = abilities.Installations[i].Data;
                parts.Add(data.AbilityId.Value);
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.OperationSetVersion.Value);
                parts.Add(data.NumericProfile.Id.ToString());
            }
            return StableHash.Compute(parts.ToArray());
        }

        static StableHash ComputeGameplayContentHash(
            CharacterControlRuntimeBinding control,
            CharacterBodyMotionBinding bodyMotion,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            Float32GameplayAbilityExecutionInstallationSet abilities)
        {
            var parts = new List<string>
            {
                "float32-simulation-actor-content/1",
                control.BindingHash.ToString(),
                bodyMotion.BindingHash.ToString(),
                abilities.GameplayEffectCatalog != null ? gameplayEffects.BindingHash.ToString() : string.Empty,
                abilities.RequiresEquipment ? equipment.BindingHash.ToString() : string.Empty
            };
            for (int i = 0; i < abilities.Installations.Count; i++)
            {
                Float32GameplayAbilityExecutionData data = abilities.Installations[i].Data;
                parts.Add(data.AbilityId.Value);
                parts.Add(data.ContentHash.ToString());
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.ExecutionIdentity);
            }
            return StableHash.Compute(parts.ToArray());
        }
    }
}
