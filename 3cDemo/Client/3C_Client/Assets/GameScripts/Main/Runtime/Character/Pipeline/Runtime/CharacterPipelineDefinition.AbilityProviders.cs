using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using UnityEngine;

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
            AddProvider(
                providers,
                GameplayAbilityProviderKind.Input,
                m_InputProfile,
                m_AbilityInputProviderOwner,
                "Input");
            AddProvider(
                providers,
                GameplayAbilityProviderKind.GameplayEffect,
                m_GameplayEffectProfile,
                m_AbilityGameplayEffectProviderOwner,
                "Gameplay Effect");
            AddProvider(
                providers,
                GameplayAbilityProviderKind.Equipment,
                m_EquipmentProfile,
                m_AbilityEquipmentProviderOwner,
                "Equipment");
            if (!string.IsNullOrEmpty(ControlModuleId))
                providers.Add(new GameplayAbilityProviderBindingEntry(
                    GameplayAbilityProviderKind.CharacterState,
                    CharacterStateProviderFields.Owner(ControlModuleId)));
            return new GameplayAbilityProviderBinding(providers);
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

        static void AddProvider(
            List<GameplayAbilityProviderBindingEntry> providers,
            GameplayAbilityProviderKind kind,
            UnityEngine.Object asset,
            string providerOwner,
            string label)
        {
            if (!asset)
                return;
            if (string.IsNullOrEmpty(providerOwner))
                throw new InvalidOperationException($"Character Pipeline Definition {label} provider owner is missing.");
            providers.Add(new GameplayAbilityProviderBindingEntry(kind, providerOwner));
        }
    }
}
