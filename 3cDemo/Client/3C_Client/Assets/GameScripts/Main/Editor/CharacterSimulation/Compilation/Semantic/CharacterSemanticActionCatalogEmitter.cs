using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonSimulation;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticActionCatalogEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly GameplayAbilityCatalogIndex m_Index;

        public CharacterSemanticActionCatalogEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            GameplayAbilityCatalogIndex index)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public void Emit()
        {
            foreach (GameplayAbilityAdmissionProfile profile in m_Model.AdmissionProfiles)
            {
                if (!profile || string.IsNullOrEmpty(profile.ActionId))
                    continue;
                m_Index.Actions.Add(profile.ActionId);
                m_Index.Behaviors.Add(profile.BehaviorId);
                CharacterSimulationSourceLocation source =
                    CharacterSemanticSourceFactory.Asset(m_Model, profile, $"action:{profile.ActionId}");
                var fields = CharacterSemanticBehaviorCatalogFields.Emit(profile, m_Builder, source).ToList();
                fields.Add(m_Builder.ConstantField(source, "TargetRequirement", profile.TargetRequirement));
                fields.Add(m_Builder.ConstantField(source, "MaxConcurrentInstances", profile.MaxConcurrentInstances));
                AddQueryFields(fields, source, "Required", profile.RequiredTags);
                AddQueryFields(fields, source, "Block", profile.BlockTags);
                AddQueryFields(fields, source, "Cancel", profile.CancelTags);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.Action,
                    $"action:{profile.ActionId}",
                    4,
                    Fields(fields),
                    source);
            }

            foreach (GameplayBehaviorProfile profile in m_Model.BehaviorProfiles)
            {
                if (!profile || string.IsNullOrEmpty(profile.BehaviorId))
                    continue;
                m_Index.Behaviors.Add(profile.BehaviorId);
                CharacterSimulationSourceLocation source =
                    CharacterSemanticSourceFactory.Asset(m_Model, profile, $"behavior:{profile.BehaviorId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.Behavior,
                    $"behavior:{profile.BehaviorId}",
                    1,
                    CharacterSemanticBehaviorCatalogFields.Emit(profile, m_Builder, source),
                    source);
            }
        }

        void AddQueryFields(
            List<ProgramCatalogField> fields,
            CharacterSimulationSourceLocation source,
            string prefix,
            GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("tag_query_missing", source.Identity, $"{prefix} tag query is missing.");
                return;
            }
            AddTags(fields, $"{prefix}:All", query.All);
            AddTags(fields, $"{prefix}:Any", query.Any);
            AddTags(fields, $"{prefix}:None", query.None);
        }

        void AddTags(List<ProgramCatalogField> fields, string prefix, IReadOnlyList<GameplayTagId> tags)
        {
            for (int i = 0; i < tags.Count; i++)
                fields.Add(m_Builder.IdentityField($"{prefix}:{i:D4}", $"tag:{tags[i].Value}"));
        }

        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
