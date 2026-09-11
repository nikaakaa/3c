using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonGameplay.Tags
{
    [CreateAssetMenu(fileName = "GameplayTagCatalog", menuName = "3C/Gameplay/Tag Catalog")]
    public sealed class GameplayTagCatalog : ScriptableObject
    {
        [SerializeField] GameplayTagDefinition[] m_Tags = Array.Empty<GameplayTagDefinition>();

        public IReadOnlyList<GameplayTagDefinition> Tags => m_Tags ?? Array.Empty<GameplayTagDefinition>();

        public void ConfigureTags(IEnumerable<GameplayTagDefinition> tags)
        {
            m_Tags = (tags ?? Array.Empty<GameplayTagDefinition>()).ToArray();
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            return GameplayTagCatalogRuntimeData.TryBuild(this, out _, errors);
        }
    }
}
