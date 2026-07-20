using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem
{
    [CreateAssetMenu(fileName = "ActionProfile", menuName = "3C/Character/Action Profile")]
    public sealed class ActionProfile : ScriptableObject, IGameplayBehaviorProfile
    {
        [SerializeField] string m_ActionId;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_DebugCategory;
        [SerializeField] GameplayTagId[] m_Tags = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayTagQuery m_RequiredTags = new GameplayTagQuery();
        [SerializeField] GameplayTagQuery m_BlockTags = new GameplayTagQuery();
        [SerializeField] GameplayTagQuery m_CancelTags = new GameplayTagQuery();
        [SerializeField] ActionTargetRequirement m_TargetRequirement;

        public string ActionId => m_ActionId;
        public string BehaviorId => m_ActionId;
        public GameplayBehaviorKind BehaviorKind => GameplayBehaviorKind.Transaction;
        public string DisplayName => m_DisplayName;
        public string DebugCategory => m_DebugCategory;
        public IReadOnlyList<GameplayTagId> Tags => m_Tags ?? Array.Empty<GameplayTagId>();
        public GameplayTagQuery RequiredTags => m_RequiredTags;
        public GameplayTagQuery BlockTags => m_BlockTags;
        public GameplayTagQuery CancelTags => m_CancelTags;
        public ActionTargetRequirement TargetRequirement => m_TargetRequirement;

        public bool ContainsTag(GameplayTagId tag)
        {
            return Contains(m_Tags, tag);
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrEmpty(m_ActionId))
            {
                errors?.Add($"{name}: action id is missing.");
                valid = false;
            }
            if (string.IsNullOrEmpty(m_DisplayName))
            {
                errors?.Add($"{name}: display name is missing.");
                valid = false;
            }
            if (!Enum.IsDefined(typeof(ActionTargetRequirement), m_TargetRequirement))
            {
                errors?.Add($"{name}: target requirement '{(int)m_TargetRequirement}' is invalid.");
                valid = false;
            }

            valid &= ValidateUniqueTags(Tags, errors);
            return valid;
        }

        public bool CollectTagConfigurationErrors(GameplayTagCatalogRuntimeData catalog, List<string> errors)
        {
            bool valid = true;
            for (int i = 0; i < Tags.Count; i++)
            {
                if (!catalog.Contains(Tags[i]))
                {
                    errors?.Add($"{name}: action tag '{Tags[i]}' is not registered.");
                    valid = false;
                }
            }
            valid &= m_RequiredTags != null && m_RequiredTags.CollectConfigurationErrors(catalog, name, errors);
            valid &= m_BlockTags != null && m_BlockTags.CollectConfigurationErrors(catalog, name, errors);
            valid &= m_CancelTags != null && m_CancelTags.CollectConfigurationErrors(catalog, name, errors);
            return valid;
        }

        void OnValidate()
        {
            m_ActionId = Normalize(m_ActionId);
            m_DisplayName = Normalize(m_DisplayName);
            m_DebugCategory = Normalize(m_DebugCategory);
        }

        bool ValidateUniqueTags(IReadOnlyList<GameplayTagId> values, List<string> errors)
        {
            bool valid = true;
            var ids = new HashSet<GameplayTagId>();
            for (int i = 0; i < values.Count; i++)
            {
                GameplayTagId value = values[i];
                if (!value.IsValid)
                {
                    errors?.Add($"{name}: tag #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (!ids.Add(value))
                {
                    errors?.Add($"{name}: duplicate tag '{value}'.");
                    valid = false;
                }
            }

            return valid;
        }

        static bool Contains(IReadOnlyList<GameplayTagId> values, GameplayTagId value)
        {
            if (!value.IsValid)
                return false;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                    return true;
            }

            return false;
        }

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
