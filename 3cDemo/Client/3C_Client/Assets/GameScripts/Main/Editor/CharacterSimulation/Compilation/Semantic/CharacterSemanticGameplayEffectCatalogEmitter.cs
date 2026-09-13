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
        readonly CharacterSimulationCatalogIndex m_Index;

        public CharacterSemanticGameplayEffectCatalogEmitter(
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
                SemanticDataDocument definition = EncodeEffect(effect, source, m_Report);
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
            CharacterSimulationCompileReport report) =>
            EncodeEffect(effect, source, report);

        static SemanticDataDocument EncodeEffect(
            GameplayEffectDefinition effect,
            CharacterSimulationSourceLocation source,
            CharacterSimulationCompileReport report)
        {
            try
            {
                var writer = new SemanticDataWriter();
                writer.WriteInt32(1);
                writer.WriteString(effect.EffectId.Value);
                writer.WriteUInt32(effect.DefinitionRevision);
                writer.WriteInt32((int)effect.DurationPolicy);
                WriteMagnitude(writer, effect.DurationMagnitude, source, "Duration");
                writer.WriteBoolean(effect.HasPeriod);
                WriteMagnitude(writer, effect.PeriodMagnitude, source, "Period");
                writer.WriteBoolean(effect.ExecuteOnApplication);
                writer.WriteInt32((int)effect.StackingPolicy);
                writer.WriteInt32(effect.MaxStacks);
                writer.WriteInt32((int)effect.DurationUpdate);
                writer.WriteInt32((int)effect.PeriodUpdate);
                writer.WriteInt32((int)effect.OverflowPolicy);
                writer.WriteInt32(effect.SetByCallerParameters.Count);
                for (int i = 0; i < effect.SetByCallerParameters.Count; i++)
                    writer.WriteString(effect.SetByCallerParameters[i]?.ParameterId);
                writer.WriteInt32(effect.Components.Count);
                for (int i = 0; i < effect.Components.Count; i++)
                    WriteComponent(writer, effect.Components[i], source, i);
                return writer.Build();
            }
            catch (Exception exception)
            {
                report.Error("gameplay_effect_compile_failed", source.Identity, exception.Message);
                return SemanticDataDocument.Empty;
            }
        }

        static void WriteComponent(
            SemanticDataWriter writer,
            GameplayEffectComponentDefinition component,
            CharacterSimulationSourceLocation source,
            int index)
        {
            if (component == null)
                throw new InvalidOperationException($"Gameplay Effect component #{index} is missing.");
            writer.WriteString(component.GetType().FullName);
            switch (component)
            {
                case GameplayModifierComponentDefinition modifier:
                    writer.WriteString(modifier.AttributeId.Value);
                    writer.WriteInt32((int)modifier.Application);
                    writer.WriteInt32((int)modifier.Operation);
                    WriteMagnitude(writer, modifier.Magnitude, source, $"Component[{index}].Magnitude");
                    writer.WriteInt32(modifier.Priority);
                    writer.WriteInt32((int)modifier.ClampBound);
                    writer.WriteBoolean(modifier.ScaleWithStack);
                    break;
                case GrantedTagsComponentDefinition granted:
                    WriteTags(writer, granted.Tags);
                    break;
                case GameplayTagRequirementsComponentDefinition tags:
                    writer.WriteInt32((int)tags.Phase);
                    WriteQuery(writer, tags.Source);
                    WriteQuery(writer, tags.Target);
                    break;
                case GameplayAttributeRequirementsComponentDefinition attributes:
                    writer.WriteInt32((int)attributes.Phase);
                    writer.WriteInt32((int)attributes.Source);
                    writer.WriteString(attributes.AttributeId.Value);
                    writer.WriteInt32((int)attributes.Comparison);
                    WriteMagnitude(writer, attributes.Threshold, source, $"Component[{index}].Threshold");
                    break;
                case GameplayEffectExecutionComponentDefinition execution:
                    writer.WriteInt32(execution.Mutations.Count);
                    for (int i = 0; i < execution.Mutations.Count; i++)
                    {
                        GameplayExecutionMutationDefinition mutation = execution.Mutations[i] ??
                            throw new InvalidOperationException($"Execution mutation #{i} is missing.");
                        writer.WriteString(mutation.AttributeId.Value);
                        writer.WriteInt32((int)mutation.Operation);
                        WriteMagnitude(writer, mutation.Magnitude, source, $"Component[{index}].Mutation[{i}]");
                        writer.WriteInt32((int)mutation.ClampBound);
                    }
                    break;
                case AdditionalEffectsComponentDefinition additional:
                    writer.WriteInt32(additional.Effects.Count);
                    for (int i = 0; i < additional.Effects.Count; i++)
                    {
                        GameplayAdditionalEffectDefinition child = additional.Effects[i] ??
                            throw new InvalidOperationException($"Additional Effect #{i} is missing.");
                        writer.WriteInt32((int)child.Trigger);
                        writer.WriteString(child.Effect ? child.Effect.EffectId.Value : string.Empty);
                        writer.WriteInt32(child.ParameterBindings.Count);
                        for (int bindingIndex = 0; bindingIndex < child.ParameterBindings.Count; bindingIndex++)
                        {
                            GameplayAdditionalEffectParameterBindingDefinition binding = child.ParameterBindings[bindingIndex] ??
                                throw new InvalidOperationException($"Additional Effect binding #{bindingIndex} is missing.");
                            writer.WriteString(binding.ChildParameterId);
                            writer.WriteInt32((int)binding.Source);
                            writer.WriteString(binding.ParentParameterId);
                            writer.WriteNumber(binding.Constant, $"{source.Identity}/Component[{index}].Effect[{i}].Binding[{bindingIndex}]");
                        }
                    }
                    break;
                case GameplayCueBindingComponentDefinition cue:
                    writer.WriteString(cue.CueId);
                    writer.WriteInt32((int)cue.Trigger);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Effect component '{component.GetType().FullName}' has no portable compiler.");
            }
        }

        static void WriteMagnitude(
            SemanticDataWriter writer,
            GameplayMagnitudeDefinition magnitude,
            CharacterSimulationSourceLocation source,
            string field)
        {
            if (magnitude == null)
                throw new InvalidOperationException($"Magnitude '{field}' is missing.");
            writer.WriteInt32((int)magnitude.Source);
            writer.WriteNumber(magnitude.Constant, $"{source.Identity}/{field}.Constant");
            writer.WriteString(magnitude.SetByCallerParameterId);
            writer.WriteString(magnitude.AttributeId.Value);
            writer.WriteNumber(magnitude.Coefficient, $"{source.Identity}/{field}.Coefficient");
            writer.WriteNumber(magnitude.PostAdd, $"{source.Identity}/{field}.PostAdd");
        }

        static void WriteQuery(SemanticDataWriter writer, GameplayTagQuery query)
        {
            if (query == null)
                throw new InvalidOperationException("Gameplay Tag query is missing.");
            WriteTags(writer, query.All);
            WriteTags(writer, query.Any);
            WriteTags(writer, query.None);
        }

        static void WriteTags(SemanticDataWriter writer, IReadOnlyList<GameplayTagId> tags)
        {
            writer.WriteInt32(tags.Count);
            for (int i = 0; i < tags.Count; i++)
                writer.WriteString(tags[i].Value);
        }

        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
