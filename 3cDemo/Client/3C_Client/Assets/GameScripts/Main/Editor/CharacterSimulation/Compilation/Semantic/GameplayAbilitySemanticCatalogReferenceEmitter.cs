using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class GameplayAbilitySemanticCatalogReferenceEmitter
    {
        readonly GameplayAbilityCatalogIndex m_CatalogIndex;
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;

        public GameplayAbilitySemanticCatalogReferenceEmitter(
            GameplayAbilityCatalogIndex catalogIndex,
            GameplayAbilitySemanticBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_CatalogIndex = catalogIndex ?? throw new ArgumentNullException(nameof(catalogIndex));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void Bind(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            ProgramCatalogEntryKind kind,
            string identity,
            bool known,
            string suffix = "catalog")
        {
            if (!known || !m_Builder.TryGetCatalogEntry(kind, identity, out int catalog))
            {
                m_Report.Error("catalog_reference_invalid", source.Identity, $"Node references unknown catalog entry '{identity}'.");
                return;
            }
            m_Builder.DeclareReference(
                $"{route}/node:{source.NodeId}/{suffix}",
                operation,
                ProgramReferenceKind.CatalogEntry,
                catalog,
                identity,
                source);
        }

        public void BindTagQuery(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("gameplay_tag_query_missing", source.Identity, "Gameplay Tag query is missing.");
                return;
            }
            int suffix = 0;
            Bind(query.All);
            Bind(query.Any);
            Bind(query.None);

            void Bind(IReadOnlyList<GameplayTagId> values)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    string tagId = values[i].Value;
                    this.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayTag,
                        $"tag:{tagId}",
                        m_CatalogIndex.GameplayTags.Contains(tagId),
                        $"tag-{suffix++:D4}");
                }
            }
        }
    }
}
