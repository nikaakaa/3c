using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    [CreateAssetMenu(fileName = "CharacterGameplayEffectProfile", menuName = "3C/Character/Gameplay Effect Profile")]
    public sealed class CharacterGameplayEffectProfile : ScriptableObject
    {
        [SerializeField] GameplayTagCatalog m_TagCatalog;
        [SerializeField] GameplayAttributeDefinition[] m_AttributeDefinitions = Array.Empty<GameplayAttributeDefinition>();
        [SerializeField] InitialGameplayAttributeValue[] m_InitialAttributes = Array.Empty<InitialGameplayAttributeValue>();
        [SerializeField] GameplayTagId[] m_InitialTags = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayEffectDefinition[] m_EffectDefinitions = Array.Empty<GameplayEffectDefinition>();

        public GameplayTagCatalog TagCatalog => m_TagCatalog;
        public IReadOnlyList<GameplayAttributeDefinition> AttributeDefinitions => m_AttributeDefinitions ?? Array.Empty<GameplayAttributeDefinition>();
        public IReadOnlyList<InitialGameplayAttributeValue> InitialAttributes => m_InitialAttributes ?? Array.Empty<InitialGameplayAttributeValue>();
        public IReadOnlyList<GameplayTagId> InitialTags => m_InitialTags ?? Array.Empty<GameplayTagId>();
        public IReadOnlyList<GameplayEffectDefinition> EffectDefinitions => m_EffectDefinitions ?? Array.Empty<GameplayEffectDefinition>();

        public bool CollectConfigurationErrors(
            out GameplayTagCatalogRuntimeData tagCatalog,
            List<string> errors)
        {
            bool valid = GameplayTagCatalogRuntimeData.TryBuild(m_TagCatalog, out tagCatalog, errors);
            var attributeIds = new HashSet<GameplayAttributeId>();
            for (int i = 0; i < AttributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition definition = AttributeDefinitions[i];
                if (!definition)
                {
                    errors?.Add($"{name}: Gameplay Attribute definition #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (!definition.AttributeId.IsValid || !attributeIds.Add(definition.AttributeId))
                {
                    errors?.Add($"{name}: duplicate or missing Gameplay Attribute id '{definition.AttributeId}'.");
                    valid = false;
                }
            }

            for (int i = 0; i < AttributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition definition = AttributeDefinitions[i];
                if (definition)
                    valid &= definition.CollectConfigurationErrors(attributeIds, errors);
            }

            var initializedAttributes = new HashSet<GameplayAttributeId>();
            for (int i = 0; i < InitialAttributes.Count; i++)
            {
                InitialGameplayAttributeValue initial = InitialAttributes[i];
                if (initial == null || !initial.Definition)
                {
                    errors?.Add($"{name}: initial Gameplay Attribute #{i} has no definition.");
                    valid = false;
                    continue;
                }
                GameplayAttributeId id = initial.Definition.AttributeId;
                if (!attributeIds.Contains(id) || !initializedAttributes.Add(id) || float.IsNaN(initial.BaseValue) || float.IsInfinity(initial.BaseValue))
                {
                    errors?.Add($"{name}: initial Gameplay Attribute '{id}' is missing, duplicated, unregistered, or non-finite.");
                    valid = false;
                }
            }
            foreach (GameplayAttributeId id in attributeIds)
            {
                if (!initializedAttributes.Contains(id))
                {
                    errors?.Add($"{name}: Gameplay Attribute '{id}' has no initial value.");
                    valid = false;
                }
            }

            var initialTags = new HashSet<GameplayTagId>();
            for (int i = 0; i < InitialTags.Count; i++)
            {
                GameplayTagId id = InitialTags[i];
                if (tagCatalog == null || !tagCatalog.Contains(id) || !initialTags.Add(id))
                {
                    errors?.Add($"{name}: initial Gameplay Tag '{id}' is missing, duplicated, or unregistered.");
                    valid = false;
                }
            }

            if (EffectDefinitions.Count == 0)
            {
                errors?.Add($"{name}: Gameplay Effect registry is empty.");
                valid = false;
            }
            var effectIds = new HashSet<GameplayEffectId>();
            for (int i = 0; i < EffectDefinitions.Count; i++)
            {
                GameplayEffectDefinition definition = EffectDefinitions[i];
                if (!definition || !definition.EffectId.IsValid || !effectIds.Add(definition.EffectId))
                {
                    errors?.Add($"{name}: Gameplay Effect definition #{i} is missing or has a duplicate id.");
                    valid = false;
                }
            }
            return valid;
        }
    }
}
