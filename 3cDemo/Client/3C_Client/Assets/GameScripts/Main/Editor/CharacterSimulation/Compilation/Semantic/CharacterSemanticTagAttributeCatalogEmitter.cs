using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticTagAttributeCatalogEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly GameplayAbilityCatalogIndex m_Index;

        public CharacterSemanticTagAttributeCatalogEmitter(
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
            EmitTags();
            EmitAttributes();
        }

        void EmitTags()
        {
            CharacterGameplayEffectProfile profile = m_Model.GameplayEffectProfile;
            if (!profile)
            {
                m_Report.Error("gameplay_effect_profile_missing", DefinitionSource.Identity, "Character Gameplay Effect Profile is missing.");
                return;
            }
            if (!profile.TagCatalog)
                return;
            var initialTags = new HashSet<GameplayTagId>(m_Model.InitialTags);
            foreach (GameplayTagDefinition tag in m_Model.TagDefinitions)
            {
                if (tag == null || !tag.TagId.IsValid)
                    continue;
                m_Index.GameplayTags.Add(tag.TagId.Value);
                CharacterSimulationSourceLocation source = AssetSource(profile.TagCatalog, $"tag:{tag.TagId.Value}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.GameplayTag,
                    $"tag:{tag.TagId.Value}",
                    1,
                    Fields(
                        m_Builder.ConstantField(source, "DisplayName", tag.DisplayName),
                        m_Builder.ConstantField(source, "DebugCategory", tag.DebugCategory),
                        m_Builder.ConstantField(source, "Initial", initialTags.Contains(tag.TagId)),
                        m_Builder.IdentityField("Parent", tag.ParentTag.IsValid ? $"tag:{tag.ParentTag.Value}" : string.Empty)),
                    source);
            }
        }

        void EmitAttributes()
        {
            var initialAttributes = new Dictionary<GameplayAttributeId, float>();
            for (int i = 0; i < m_Model.InitialAttributes.Count; i++)
            {
                InitialGameplayAttributeValue initial = m_Model.InitialAttributes[i];
                if (initial?.Definition && initial.Definition.AttributeId.IsValid)
                    initialAttributes[initial.Definition.AttributeId] = initial.BaseValue;
            }
            foreach (GameplayAttributeDefinition attribute in m_Model.AttributeDefinitions)
            {
                if (!attribute || !attribute.AttributeId.IsValid)
                    continue;
                m_Index.Attributes.Add(attribute.AttributeId.Value);
                CharacterSimulationSourceLocation source = AssetSource(attribute, $"attribute:{attribute.AttributeId.Value}");
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.ConstantField(source, "DisplayName", attribute.DisplayName),
                    m_Builder.ConstantField(source, "DebugCategory", attribute.DebugCategory),
                    m_Builder.ConstantField(source, "InitialBase", initialAttributes.TryGetValue(attribute.AttributeId, out float initial) ? initial : 0f)
                };
                AddBoundFields(fields, source, "Minimum", attribute.Minimum);
                AddBoundFields(fields, source, "Maximum", attribute.Maximum);
                m_Builder.DeclareCatalogEntry(ProgramCatalogEntryKind.Attribute, $"attribute:{attribute.AttributeId.Value}", 1, Fields(fields), source);
            }
        }

        void AddBoundFields(List<ProgramCatalogField> fields, CharacterSimulationSourceLocation source, string prefix, GameplayAttributeBoundDefinition bound)
        {
            fields.Add(m_Builder.ConstantField(source, $"{prefix}:Enabled", bound?.Enabled ?? false));
            if (bound == null || !bound.Enabled)
                return;
            fields.Add(m_Builder.ConstantField(source, $"{prefix}:Source", bound.Source));
            if (bound.Source == GameplayAttributeBoundSource.Constant)
                fields.Add(m_Builder.ConstantField(source, $"{prefix}:Constant", bound.Constant));
            else
                fields.Add(m_Builder.IdentityField($"{prefix}:Attribute", $"attribute:{bound.AttributeId.Value}"));
        }

        CharacterSimulationSourceLocation DefinitionSource => AssetSource(m_Model.Definition, $"definition:{m_Model.Definition.name}");

        CharacterSimulationSourceLocation AssetSource(UnityEngine.Object asset, string identity) =>
            CharacterSemanticSourceFactory.Asset(m_Model, asset, identity);

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] fields) => Fields((IEnumerable<ProgramCatalogField>)fields);
        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
