using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;
using UnityEngine;

namespace ThirdPersonCharacter.Behavior
{
    [CreateAssetMenu(fileName = "GameplayBehaviorProfile", menuName = "3C/Character/Gameplay Behavior Definition")]
    public sealed class GameplayBehaviorProfile : ScriptableObject, IGameplayBehaviorProfile
    {
        [SerializeField] string m_BehaviorId;
        [SerializeField] GameplayBehaviorKind m_BehaviorKind = GameplayBehaviorKind.Stream;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_DebugCategory;
        [SerializeField] GameplayTagId[] m_Tags = Array.Empty<GameplayTagId>();

        public string BehaviorId => m_BehaviorId;
        public GameplayBehaviorKind BehaviorKind => m_BehaviorKind;
        public string DisplayName => m_DisplayName;
        public string DebugCategory => m_DebugCategory;
        public IReadOnlyList<GameplayTagId> Tags => m_Tags ?? Array.Empty<GameplayTagId>();

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrEmpty(m_BehaviorId))
            {
                errors?.Add($"{name}: behavior id is missing.");
                valid = false;
            }
            if (string.IsNullOrEmpty(m_DisplayName))
            {
                errors?.Add($"{name}: display name is missing.");
                valid = false;
            }
            if (m_BehaviorKind == GameplayBehaviorKind.Transaction)
            {
                errors?.Add($"{name}: transaction behavior must use ActionProfile.");
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
                    errors?.Add($"{name}: behavior tag '{Tags[i]}' is not registered.");
                    valid = false;
                }
            }
            return valid;
        }

        void OnValidate()
        {
            m_BehaviorId = Normalize(m_BehaviorId);
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

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
