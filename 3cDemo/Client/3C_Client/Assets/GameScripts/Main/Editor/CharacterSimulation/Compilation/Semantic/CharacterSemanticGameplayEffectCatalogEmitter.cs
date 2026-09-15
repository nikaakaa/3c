using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticGameplayEffectCatalogEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly GameplayAbilityCatalogIndex m_Index;

        public CharacterSemanticGameplayEffectCatalogEmitter(
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
            CharacterGameplayEffectProfile profile = m_Model.GameplayEffectProfile;
            if (!profile)
                return;
            foreach (GameplayEffectDefinition effect in m_Model.EffectDefinitions)
            {
                if (!effect || !effect.EffectId.IsValid)
                    continue;
                m_Index.GameplayEffects.Add(effect.EffectId.Value);
                m_Index.Behaviors.Add(effect.BehaviorId);
                CharacterSimulationSourceLocation source =
                    CharacterSemanticSourceFactory.Asset(m_Model, effect, $"effect:{effect.EffectId.Value}");
                SemanticDataDocument definition = EncodeDefinition(effect, source, m_Report);
                var fields = CharacterSemanticBehaviorCatalogFields.Emit(effect, m_Builder, source).ToList();
                fields.Add(m_Builder.ConstantField(source, "Definition", definition));
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.GameplayEffect,
                    $"effect:{effect.EffectId.Value}",
                    checked((int)effect.DefinitionRevision),
                    Fields(fields),
                    source);
                for (int i = 0; i < effect.Components.Count; i++)
                {
                    if (effect.Components[i] is not GameplayCueBindingComponentDefinition cue || string.IsNullOrEmpty(cue.CueId))
                        continue;
                    m_Builder.DeclareProducer(
                        $"producer:effect:{effect.EffectId.Value}:cue:{i}:{cue.CueId}",
                        new AnimationChannelId("Cue"),
                        $"effect:{effect.EffectId.Value}",
                        ProgramOutputChannelKind.Presentation,
                        source);
                }
            }
        }

        internal static SemanticDataDocument EncodeDefinition(
            GameplayEffectDefinition effect,
            CharacterSimulationSourceLocation source,
            CharacterSimulationCompileReport report)
        {
            try
            {
                return GameplayEffectRuntimeDefinitionCodec.EncodeDefinition(effect, source.Identity);
            }
            catch (Exception exception)
            {
                report.Error("gameplay_effect_compile_failed", source.Identity, exception.Message);
                return SemanticDataDocument.Empty;
            }
        }

        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
