using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        [SerializeField, HideInInspector] string m_AbilityInputProviderOwner = string.Empty;
        [SerializeField, HideInInspector] string m_AbilityGameplayEffectProviderOwner = string.Empty;
        [SerializeField, HideInInspector] string m_AbilityEquipmentProviderOwner = string.Empty;

        public GameplayAbilityProviderBinding BuildGameplayAbilityProviderBinding()
        {
            var providers = new List<GameplayAbilityProviderBindingEntry>();
            if (m_InputProfile)
                providers.Add(BuildInputProvider(m_InputProfile, m_AbilityInputProviderOwner));
            if (m_GameplayEffectProfile)
                providers.Add(BuildGameplayEffectProvider(m_GameplayEffectProfile, m_AbilityGameplayEffectProviderOwner));
            if (m_EquipmentProfile)
                providers.Add(BuildEquipmentProvider(m_EquipmentProfile, m_AbilityEquipmentProviderOwner));
            if (!string.IsNullOrEmpty(ControlModuleId))
                providers.Add(BuildCharacterStateProvider(ControlModuleId));
            return new GameplayAbilityProviderBinding(providers);
        }

        public Float32GameplayAbilityExecutionData LoadFloat32AbilityExecutionData(
            ThirdPersonCharacter.Pipeline.Simulation.GameplayAbilityDataAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            return asset.Load(BuildGameplayAbilityProviderBinding());
        }

        public GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> LoadFloat32AbilityExecutionDataSet(
            IEnumerable<ThirdPersonCharacter.Pipeline.Simulation.GameplayAbilityDataAsset> assets)
        {
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));
            GameplayAbilityProviderBinding providerBinding = BuildGameplayAbilityProviderBinding();
            var data = new List<Float32GameplayAbilityExecutionData>();
            foreach (ThirdPersonCharacter.Pipeline.Simulation.GameplayAbilityDataAsset asset in assets)
                data.Add(asset.Load(providerBinding));
            return new GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData>(data, value => value.AbilityId);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_AbilityInputProviderOwner = ReadProviderOwner(m_InputProfile);
            m_AbilityGameplayEffectProviderOwner = ReadProviderOwner(m_GameplayEffectProfile);
            m_AbilityEquipmentProviderOwner = ReadProviderOwner(m_EquipmentProfile);
        }

        static string ReadProviderOwner(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = UnityEditor.AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path)
                ? string.Empty
                : UnityEditor.AssetDatabase.AssetPathToGUID(path);
            return CharacterSkillProviderOwners.Asset(guid);
        }
#endif

        static GameplayAbilityProviderBindingEntry BuildInputProvider(
            CharacterInputProfile profile,
            string providerOwner)
        {
            RequireProviderOwner(providerOwner, "Input");
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join(" ", errors));
            var members = new List<GameplayAbilityProviderMemberBinding>();
            for (int i = 0; i < profile.InputValues.Count; i++)
            {
                CharacterInputValueDefinition value = profile.InputValues[i];
                members.Add(new GameplayAbilityProviderMemberBinding(
                    GameplayAbilityProviderKind.Input,
                    providerOwner,
                    $"input:value:{value.InputValueId}",
                    InputValueKind(value.ValueType),
                    2,
                    InputRuntimeHandle(value.SourceAction)));
            }
            for (int i = 0; i < profile.ActionRequests.Count; i++)
            {
                CharacterActionRequestDefinition request = profile.ActionRequests[i];
                members.Add(new GameplayAbilityProviderMemberBinding(
                    GameplayAbilityProviderKind.Input,
                    providerOwner,
                    $"input:request:{request.RequestId}",
                    GameplayAbilityProviderValueKind.None,
                    2,
                    InputRuntimeHandle(request.SourceAction)));
            }
            return new GameplayAbilityProviderBindingEntry(
                GameplayAbilityProviderKind.Input,
                providerOwner,
                GameplayAbilityProviderContractVersions.Input,
                members);
        }

        static GameplayAbilityProviderBindingEntry BuildGameplayEffectProvider(
            CharacterGameplayEffectProfile profile,
            string providerOwner)
        {
            RequireProviderOwner(providerOwner, "Gameplay Effect");
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(out _, errors))
                throw new InvalidOperationException(string.Join(" ", errors));
            var members = new List<GameplayAbilityProviderMemberBinding>();
            for (int i = 0; i < profile.TagCatalog.Tags.Count; i++)
            {
                GameplayTagDefinition tag = profile.TagCatalog.Tags[i];
                members.Add(Member(
                    GameplayAbilityProviderKind.GameplayEffect,
                    providerOwner,
                    $"tag:{tag.TagId.Value}",
                    GameplayAbilityProviderValueKind.Boolean,
                    1,
                    $"gameplay-effect:tag:{tag.TagId.Value}"));
            }
            for (int i = 0; i < profile.AttributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition attribute = profile.AttributeDefinitions[i];
                members.Add(Member(
                    GameplayAbilityProviderKind.GameplayEffect,
                    providerOwner,
                    $"attribute:{attribute.AttributeId.Value}",
                    GameplayAbilityProviderValueKind.Number,
                    1,
                    $"gameplay-effect:attribute:{attribute.AttributeId.Value}"));
            }
            for (int i = 0; i < profile.EffectDefinitions.Count; i++)
            {
                GameplayEffectDefinition effect = profile.EffectDefinitions[i];
                int revision = checked((int)effect.DefinitionRevision);
                members.Add(Member(
                    GameplayAbilityProviderKind.GameplayEffect,
                    providerOwner,
                    $"effect:{effect.EffectId.Value}",
                    GameplayAbilityProviderValueKind.None,
                    revision,
                    $"gameplay-effect:effect:{effect.EffectId.Value}"));
            }
            return new GameplayAbilityProviderBindingEntry(
                GameplayAbilityProviderKind.GameplayEffect,
                providerOwner,
                GameplayAbilityProviderContractVersions.GameplayEffect,
                members);
        }

        GameplayAbilityProviderBindingEntry BuildEquipmentProvider(
            CharacterEquipmentProfile profile,
            string providerOwner)
        {
            RequireProviderOwner(providerOwner, "Equipment");
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(this, errors))
                throw new InvalidOperationException(string.Join(" ", errors));
            var members = new List<GameplayAbilityProviderMemberBinding>();
            for (int i = 0; i < profile.Slots.Count; i++)
            {
                EquipmentSlotDefinition slot = profile.Slots[i];
                members.Add(EquipmentMember(providerOwner, $"equipment:slot:{slot.SlotIdValue}"));
            }
            for (int i = 0; i < profile.Routes.Count; i++)
            {
                EquipmentActionRouteDefinition route = profile.Routes[i];
                members.Add(EquipmentMember(providerOwner, $"equipment:route:{route.RouteIdValue}"));
            }
            for (int i = 0; i < profile.Features.Count; i++)
            {
                CharacterEquipmentFeatureDefinition feature = profile.Features[i];
                string featureIdentity = $"equipment:feature:{feature.FeatureIdValue}";
                members.Add(EquipmentMember(providerOwner, featureIdentity));
                for (int parameterIndex = 0; parameterIndex < feature.Parameters.Count; parameterIndex++)
                    members.Add(EquipmentMember(providerOwner, $"{featureIdentity}:parameter:{feature.Parameters[parameterIndex].ParameterIdValue}"));
                for (int stateIndex = 0; stateIndex < feature.LocalStates.Count; stateIndex++)
                    members.Add(EquipmentMember(providerOwner, $"{featureIdentity}:state:{feature.LocalStates[stateIndex].StateIdValue}"));
                for (int routeIndex = 0; routeIndex < feature.RouteImplementations.Count; routeIndex++)
                    members.Add(EquipmentMember(providerOwner, $"{featureIdentity}:route:{feature.RouteImplementations[routeIndex].RouteIdValue}"));
            }
            for (int i = 0; i < profile.Equipment.Count; i++)
            {
                EquipmentDefinition equipment = profile.Equipment[i];
                string equipmentIdentity = $"equipment:item:{equipment.EquipmentIdValue}";
                members.Add(EquipmentMember(providerOwner, equipmentIdentity));
                members.Add(EquipmentMember(providerOwner, $"equipment:visual:{equipment.VisualBindingIdValue}"));
                for (int parameterIndex = 0; parameterIndex < equipment.ParameterValues.Count; parameterIndex++)
                    members.Add(EquipmentMember(providerOwner, $"{equipmentIdentity}:parameter:{equipment.ParameterValues[parameterIndex].ParameterIdValue}"));
            }
            for (int i = 0; i < profile.InitialLoadout.Count; i++)
            {
                InitialEquipmentLoadoutEntry loadout = profile.InitialLoadout[i];
                members.Add(EquipmentMember(providerOwner, $"equipment:loadout:{loadout.SlotIdValue}"));
            }
            return new GameplayAbilityProviderBindingEntry(
                GameplayAbilityProviderKind.Equipment,
                providerOwner,
                GameplayAbilityProviderContractVersions.Equipment,
                members);
        }

        static GameplayAbilityProviderBindingEntry BuildCharacterStateProvider(string controlModuleId)
        {
            string providerOwner = CharacterStateProviderFields.Owner(controlModuleId);
            var members = new List<GameplayAbilityProviderMemberBinding>();
            AddCharacterStateMember(members, providerOwner, CharacterStateProviderFields.Position);
            AddCharacterStateMember(members, providerOwner, CharacterStateProviderFields.Velocity);
            AddCharacterStateMember(members, providerOwner, CharacterStateProviderFields.VerticalVelocity);
            AddCharacterStateMember(members, providerOwner, CharacterStateProviderFields.BodyYaw);
            AddCharacterStateMember(members, providerOwner, CharacterStateProviderFields.Grounded);
            members.Add(Member(
                GameplayAbilityProviderKind.CharacterState,
                providerOwner,
                "character-state:move-facing-angle",
                GameplayAbilityProviderValueKind.Number,
                1,
                $"control-state:{controlModuleId}:move-facing-angle"));
            return new GameplayAbilityProviderBindingEntry(
                GameplayAbilityProviderKind.CharacterState,
                providerOwner,
                GameplayAbilityProviderContractVersions.CharacterState,
                members);
        }

        static void AddCharacterStateMember(
            List<GameplayAbilityProviderMemberBinding> members,
            string providerOwner,
            string field)
        {
            members.Add(Member(
                GameplayAbilityProviderKind.CharacterState,
                providerOwner,
                $"character-state:{field}",
                ToProviderValueKind(CharacterStateProviderFields.ValueKind(field)),
                1,
                $"control-state:{providerOwner.Substring(CharacterStateProviderFields.OwnerPrefix.Length)}:{field}"));
        }

        static GameplayAbilityProviderMemberBinding EquipmentMember(string providerOwner, string memberIdentity) =>
            Member(
                GameplayAbilityProviderKind.Equipment,
                providerOwner,
                memberIdentity,
                GameplayAbilityProviderValueKind.None,
                1,
                $"equipment:{memberIdentity.Substring("equipment:".Length)}");

        static GameplayAbilityProviderMemberBinding Member(
            GameplayAbilityProviderKind kind,
            string providerOwner,
            string memberIdentity,
            GameplayAbilityProviderValueKind valueKind,
            int revision,
            string runtimeHandle) =>
            new GameplayAbilityProviderMemberBinding(kind, providerOwner, memberIdentity, valueKind, revision, runtimeHandle);

        static GameplayAbilityProviderValueKind InputValueKind(CharacterInputValueType valueType) => valueType switch
        {
            CharacterInputValueType.Bool => GameplayAbilityProviderValueKind.Boolean,
            CharacterInputValueType.Float => GameplayAbilityProviderValueKind.Number,
            CharacterInputValueType.Vector2 => GameplayAbilityProviderValueKind.Vector2,
            _ => throw new ArgumentOutOfRangeException(nameof(valueType))
        };

        static GameplayAbilityProviderValueKind ToProviderValueKind(SemanticValueKind valueKind) => valueKind switch
        {
            SemanticValueKind.Boolean => GameplayAbilityProviderValueKind.Boolean,
            SemanticValueKind.Number => GameplayAbilityProviderValueKind.Number,
            SemanticValueKind.Vector2 => GameplayAbilityProviderValueKind.Vector2,
            SemanticValueKind.Vector3 => GameplayAbilityProviderValueKind.Vector3,
            SemanticValueKind.Yaw => GameplayAbilityProviderValueKind.Yaw,
            _ => throw new ArgumentOutOfRangeException(nameof(valueKind))
        };

        static string InputRuntimeHandle(InputActionReference reference) =>
            reference?.action == null ? string.Empty : $"input-action:{reference.action.id}";

        static void RequireProviderOwner(string providerOwner, string label)
        {
            if (string.IsNullOrEmpty(providerOwner))
                throw new InvalidOperationException($"Character Pipeline Definition {label} provider owner is missing.");
        }
    }
}
