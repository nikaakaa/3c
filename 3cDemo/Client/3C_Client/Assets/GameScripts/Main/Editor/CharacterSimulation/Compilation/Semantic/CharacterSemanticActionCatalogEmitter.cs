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
        readonly CharacterSimulationCatalogIndex m_Index;

        public CharacterSemanticActionCatalogEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex index)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public void Emit()
        {
            foreach (ActionProfile profile in m_Model.ActionProfiles)
            {
                if (!profile || string.IsNullOrEmpty(profile.ActionId))
                    continue;
                m_Index.Actions.Add(profile.ActionId);
                m_Index.Behaviors.Add(profile.BehaviorId);
                CharacterSimulationSourceLocation source =
                    CharacterSemanticSourceFactory.Asset(m_Model, profile, $"action:{profile.ActionId}");
                var fields = CharacterSemanticBehaviorCatalogFields.Emit(profile, m_Builder, source).ToList();
                fields.Add(m_Builder.ConstantField(source, "TargetRequirement", profile.TargetRequirement));
                AddQueryFields(fields, source, "Required", profile.RequiredTags);
                AddQueryFields(fields, source, "Block", profile.BlockTags);
                AddQueryFields(fields, source, "Cancel", profile.CancelTags);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.Action,
                    $"action:{profile.ActionId}",
                    3,
                    Fields(fields),
                    source);
                m_Builder.DeclareStandaloneStateSlot(
                    source,
                    ProgramStateValueKind.ActionActivationRequest,
                    ProgramStateOwnerKind.Action,
                    ProgramStateSemantic.ActionRequestBuffer,
                    $"action:{profile.ActionId}");
                m_Builder.DeclareStandaloneStateSlot(
                    source,
                    ProgramStateValueKind.ActionInstance,
                    ProgramStateOwnerKind.Action,
                    ProgramStateSemantic.ActionInstance,
                    $"action:{profile.ActionId}");
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
