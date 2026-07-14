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

        public bool TryBuildRuntimeDefinition(
            int logicTickRate,
            out GameplayEffectRuntimeDefinition definition,
            List<string> errors)
        {
            return GameplayEffectRuntimeDefinitionBuilder.TryBuild(
                logicTickRate,
                m_TagCatalog,
                AttributeDefinitions,
                InitialAttributes,
                InitialTags,
                EffectDefinitions,
                out definition,
                errors);
        }
    }
}
