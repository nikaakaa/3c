using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    public sealed class GameplayEffectRuntimeDefinition
    {
        readonly Dictionary<GameplayEffectId, GameplayEffectDefinitionData> m_Effects;

        internal GameplayEffectRuntimeDefinition(
            int logicTickRate,
            GameplayTagCatalogRuntimeData tagCatalog,
            GameplayAttributeDefinitionData[] attributes,
            GameplayAttributeInitialValueData[] initialAttributes,
            GameplayTagId[] initialTags,
            Dictionary<GameplayEffectId, GameplayEffectDefinitionData> effects)
        {
            LogicTickRate = logicTickRate;
            TagCatalog = tagCatalog;
            Attributes = attributes;
            InitialAttributes = initialAttributes;
            InitialTags = initialTags;
            m_Effects = effects;
        }

        public int LogicTickRate { get; }
        public GameplayTagCatalogRuntimeData TagCatalog { get; }
        internal GameplayAttributeDefinitionData[] Attributes { get; }
        internal GameplayAttributeInitialValueData[] InitialAttributes { get; }
        internal GameplayTagId[] InitialTags { get; }
        internal bool TryGetEffect(GameplayEffectId effectId, out GameplayEffectDefinitionData data) => m_Effects.TryGetValue(effectId, out data);
        public bool ContainsEffect(GameplayEffectId effectId) => m_Effects.ContainsKey(effectId);
        public bool ContainsAttribute(GameplayAttributeId attributeId)
        {
            for (int i = 0; i < Attributes.Length; i++)
            {
                if (Attributes[i].AttributeId == attributeId)
                    return true;
            }
            return false;
        }
    }

    public static class GameplayEffectRuntimeDefinitionBuilder
    {
        public static bool TryBuild(
            int logicTickRate,
            GameplayTagCatalog tagCatalog,
            IReadOnlyList<GameplayAttributeDefinition> attributeDefinitions,
            IReadOnlyList<InitialGameplayAttributeValue> initialAttributes,
            IReadOnlyList<GameplayTagId> initialTags,
            IReadOnlyList<GameplayEffectDefinition> effectDefinitions,
            out GameplayEffectRuntimeDefinition definition,
            List<string> errors)
        {
            definition = null;
            bool valid = true;
            if (logicTickRate <= 0)
            {
                errors?.Add("Gameplay Effect logic tick rate must be greater than zero.");
                valid = false;
            }
            if (!GameplayTagCatalogRuntimeData.TryBuild(tagCatalog, out GameplayTagCatalogRuntimeData tagData, errors))
                return false;
            if (attributeDefinitions == null)
            {
                errors?.Add("Gameplay Attribute definitions are missing.");
                return false;
            }
            if (initialAttributes == null)
            {
                errors?.Add("Initial Gameplay Attribute values are missing.");
                return false;
            }
            if (effectDefinitions == null)
            {
                errors?.Add("Gameplay Effect registry is missing.");
                return false;
            }
            if (effectDefinitions.Count == 0)
            {
                errors?.Add("Gameplay Effect registry is empty.");
                return false;
            }

            var attributeIds = new HashSet<GameplayAttributeId>();
            for (int i = 0; i < attributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition value = attributeDefinitions[i];
                if (!value)
                {
                    errors?.Add($"Gameplay Attribute definition #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (!value.AttributeId.IsValid || !attributeIds.Add(value.AttributeId))
                {
                    errors?.Add($"Duplicate or missing Gameplay Attribute id '{value.AttributeId}'.");
                    valid = false;
                }
            }

            var attributeData = new GameplayAttributeDefinitionData[attributeDefinitions.Count];
            var attributeDependencies = new List<AttributeDependency>();
            for (int i = 0; i < attributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition value = attributeDefinitions[i];
                if (!value)
                    continue;
                valid &= value.CollectConfigurationErrors(attributeIds, errors);
                GameplayAttributeBoundData minimum = BuildBound(value.Minimum);
                GameplayAttributeBoundData maximum = BuildBound(value.Maximum);
                attributeData[i] = new GameplayAttributeDefinitionData(
                    value.AttributeId,
                    value.DisplayName,
                    value.DebugCategory,
                    minimum,
                    maximum);
                if (minimum.Enabled && minimum.Source == GameplayAttributeBoundSource.Attribute)
                    attributeDependencies.Add(new AttributeDependency(minimum.AttributeId, value.AttributeId));
                if (maximum.Enabled && maximum.Source == GameplayAttributeBoundSource.Attribute)
                    attributeDependencies.Add(new AttributeDependency(maximum.AttributeId, value.AttributeId));
            }

            var initialData = new List<GameplayAttributeInitialValueData>();
            var initialized = new HashSet<GameplayAttributeId>();
            for (int i = 0; i < initialAttributes.Count; i++)
            {
                InitialGameplayAttributeValue value = initialAttributes[i];
                if (value == null || !value.Definition)
                {
                    errors?.Add($"Initial Gameplay Attribute #{i} has no definition.");
                    valid = false;
                    continue;
                }
                GameplayAttributeId id = value.Definition.AttributeId;
                if (!attributeIds.Contains(id))
                {
                    errors?.Add($"Initial Gameplay Attribute '{id}' is not in the registry.");
                    valid = false;
                    continue;
                }
                if (!initialized.Add(id))
                {
                    errors?.Add($"Initial Gameplay Attribute '{id}' is duplicated.");
                    valid = false;
                    continue;
                }
                if (!GameplayNumber.IsFinite(value.BaseValue))
                {
                    errors?.Add($"Initial Gameplay Attribute '{id}' must be finite.");
                    valid = false;
                    continue;
                }
                initialData.Add(new GameplayAttributeInitialValueData(id, value.BaseValue));
            }
            foreach (GameplayAttributeId id in attributeIds)
            {
                if (!initialized.Contains(id))
                {
                    errors?.Add($"Gameplay Attribute '{id}' has no initial value.");
                    valid = false;
                }
            }

            GameplayTagId[] initialTagData = Copy(initialTags);
            var initialTagSet = new HashSet<GameplayTagId>();
            for (int i = 0; i < initialTagData.Length; i++)
            {
                if (!tagData.Contains(initialTagData[i]))
                {
                    errors?.Add($"Initial Gameplay Tag '{initialTagData[i]}' is not registered.");
                    valid = false;
                }
                else if (!initialTagSet.Add(initialTagData[i]))
                {
                    errors?.Add($"Initial Gameplay Tag '{initialTagData[i]}' is duplicated.");
                    valid = false;
                }
            }

            var effectIds = new HashSet<GameplayEffectId>();
            for (int i = 0; i < effectDefinitions.Count; i++)
            {
                GameplayEffectDefinition value = effectDefinitions[i];
                if (!value)
                {
                    errors?.Add($"Gameplay Effect definition #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (!value.EffectId.IsValid || !effectIds.Add(value.EffectId))
                {
                    errors?.Add($"Duplicate or missing Gameplay Effect id '{value.EffectId}'.");
                    valid = false;
                }
            }

            var collectors = new Dictionary<GameplayEffectId, GameplayEffectReferenceCollector>();
            var effects = new Dictionary<GameplayEffectId, GameplayEffectDefinitionData>();
            for (int i = 0; i < effectDefinitions.Count; i++)
            {
                GameplayEffectDefinition value = effectDefinitions[i];
                if (!value || !value.EffectId.IsValid)
                    continue;

                var collector = new GameplayEffectReferenceCollector();
                collectors[value.EffectId] = collector;
                valid &= ValidateEffect(value, tagData, attributeIds, collector, errors);
                attributeDependencies.AddRange(collector.AttributeDependencies);
                GameplayEffectDefinitionData data = BuildEffectData(value, collector, errors);
                if (data != null && !effects.ContainsKey(value.EffectId))
                    effects.Add(value.EffectId, data);
            }

            foreach (KeyValuePair<GameplayEffectId, GameplayEffectReferenceCollector> pair in collectors)
            {
                for (int i = 0; i < pair.Value.AdditionalEffects.Count; i++)
                {
                    GameplayEffectId referenced = pair.Value.AdditionalEffects[i];
                    if (!referenced.IsValid || !effectIds.Contains(referenced))
                    {
                        errors?.Add($"Gameplay Effect '{pair.Key}' references missing additional effect '{referenced}'.");
                        valid = false;
                    }
                }
            }

            if (!ValidateEffectGraph(effectIds, collectors, errors))
                valid = false;
            if (!ValidateAttributeGraph(attributeIds, attributeDependencies, errors))
                valid = false;
            if (!valid)
                return false;

            definition = new GameplayEffectRuntimeDefinition(
                logicTickRate,
                tagData,
                attributeData,
                initialData.ToArray(),
                initialTagData,
                effects);
            return true;
        }

        static bool ValidateEffect(
            GameplayEffectDefinition definition,
            GameplayTagCatalogRuntimeData tags,
            ISet<GameplayAttributeId> attributes,
            GameplayEffectReferenceCollector collector,
            List<string> errors)
        {
            bool valid = true;
            if (definition.DefinitionRevision == 0)
            {
                errors?.Add($"{definition.name}: definition revision must be non-zero.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                errors?.Add($"{definition.name}: display name is missing.");
                valid = false;
            }
            if (definition.MaxStacks < 1)
            {
                errors?.Add($"{definition.name}: max stacks must be at least one.");
                valid = false;
            }

            var declaredParameters = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definition.SetByCallerParameters.Count; i++)
            {
                GameplaySetByCallerParameterDefinition parameter = definition.SetByCallerParameters[i];
                string id = parameter?.ParameterId ?? string.Empty;
                if (string.IsNullOrEmpty(id) || !declaredParameters.Add(id))
                {
                    errors?.Add($"{definition.name}: duplicate or missing SetByCaller parameter '{id}'.");
                    valid = false;
                }
            }

            for (int i = 0; i < definition.Tags.Count; i++)
            {
                if (!tags.Contains(definition.Tags[i]))
                {
                    errors?.Add($"{definition.name}: effect tag '{definition.Tags[i]}' is not registered.");
                    valid = false;
                }
            }

            ValidateMagnitude(definition, definition.DurationMagnitude, attributes, declaredParameters, "duration", errors, ref valid);
            collector.AddMagnitude(definition.DurationMagnitude, default);
            if (definition.HasPeriod)
            {
                ValidateMagnitude(definition, definition.PeriodMagnitude, attributes, declaredParameters, "period", errors, ref valid);
                collector.AddMagnitude(definition.PeriodMagnitude, default);
            }

            for (int i = 0; i < definition.Components.Count; i++)
            {
                GameplayEffectComponentDefinition component = definition.Components[i];
                if (component == null)
                {
                    errors?.Add($"{definition.name}: component #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (definition.DurationPolicy == GameplayEffectDurationPolicy.Instant &&
                    (component is GrantedTagsComponentDefinition ||
                     component is GameplayModifierComponentDefinition modifier && modifier.Application == GameplayModifierApplication.CurrentValue ||
                     component is GameplayTagRequirementsComponentDefinition tagRequirement && tagRequirement.Phase != GameplayEffectRequirementPhase.Application ||
                     component is GameplayAttributeRequirementsComponentDefinition attributeRequirement && attributeRequirement.Phase != GameplayEffectRequirementPhase.Application))
                {
                    errors?.Add($"{definition.name}: Instant Effect component #{i} requires an Active Effect lifecycle.");
                    valid = false;
                }
                component.CollectReferences(collector);
            }

            for (int i = 0; i < collector.Errors.Count; i++)
            {
                errors?.Add($"{definition.name}: {collector.Errors[i]}.");
                valid = false;
            }
            for (int i = 0; i < collector.Tags.Count; i++)
            {
                if (!tags.Contains(collector.Tags[i]))
                {
                    errors?.Add($"{definition.name}: component tag '{collector.Tags[i]}' is not registered.");
                    valid = false;
                }
            }
            for (int i = 0; i < collector.Attributes.Count; i++)
            {
                if (!attributes.Contains(collector.Attributes[i]))
                {
                    errors?.Add($"{definition.name}: component attribute '{collector.Attributes[i]}' is not registered.");
                    valid = false;
                }
            }
            for (int i = 0; i < collector.Magnitudes.Count; i++)
                ValidateMagnitude(definition, collector.Magnitudes[i], attributes, declaredParameters, "component", errors, ref valid);
            return valid;
        }

        static void ValidateMagnitude(
            GameplayEffectDefinition owner,
            GameplayMagnitudeDefinition magnitude,
            ISet<GameplayAttributeId> attributes,
            ISet<string> declaredParameters,
            string label,
            List<string> errors,
            ref bool valid)
        {
            if (magnitude == null)
            {
                errors?.Add($"{owner.name}: {label} magnitude is missing.");
                valid = false;
                return;
            }
            if ((magnitude.Source == GameplayMagnitudeSource.Constant && !GameplayNumber.IsFinite(magnitude.Constant)) ||
                !GameplayNumber.IsFinite(magnitude.Coefficient) ||
                !GameplayNumber.IsFinite(magnitude.PostAdd))
            {
                errors?.Add($"{owner.name}: {label} magnitude contains a non-finite number.");
                valid = false;
            }
            if (magnitude.Source == GameplayMagnitudeSource.SetByCaller &&
                (string.IsNullOrEmpty(magnitude.SetByCallerParameterId) || !declaredParameters.Contains(magnitude.SetByCallerParameterId)))
            {
                errors?.Add($"{owner.name}: {label} magnitude uses undeclared SetByCaller '{magnitude.SetByCallerParameterId}'.");
                valid = false;
            }
            if ((magnitude.Source == GameplayMagnitudeSource.SourceAttributeSnapshot ||
                 magnitude.Source == GameplayMagnitudeSource.TargetAttributeSnapshot ||
                 magnitude.Source == GameplayMagnitudeSource.TargetAttributeLive) &&
                (!magnitude.AttributeId.IsValid || !attributes.Contains(magnitude.AttributeId)))
            {
                errors?.Add($"{owner.name}: {label} magnitude references missing Attribute '{magnitude.AttributeId}'.");
                valid = false;
            }
        }

        static GameplayEffectDefinitionData BuildEffectData(
            GameplayEffectDefinition definition,
            GameplayEffectReferenceCollector collector,
            List<string> errors)
        {
            var parameters = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < definition.SetByCallerParameters.Count; i++)
            {
                GameplaySetByCallerParameterDefinition parameter = definition.SetByCallerParameters[i];
                if (parameter != null && !string.IsNullOrEmpty(parameter.ParameterId) && !parameters.ContainsKey(parameter.ParameterId))
                    parameters.Add(parameter.ParameterId, parameter.Required);
            }
            var components = new GameplayEffectComponentData[definition.Components.Count];
            for (int i = 0; i < components.Length; i++)
            {
                GameplayEffectComponentDefinition component = definition.Components[i];
                components[i] = component?.BuildData();
            }
            var sourceSnapshots = new HashSet<GameplayAttributeId>();
            var targetSnapshots = new HashSet<GameplayAttributeId>();
            for (int i = 0; i < collector.Magnitudes.Count; i++)
            {
                GameplayMagnitudeDefinition magnitude = collector.Magnitudes[i];
                if (magnitude == null)
                    continue;
                if (magnitude.Source == GameplayMagnitudeSource.SourceAttributeSnapshot)
                    sourceSnapshots.Add(magnitude.AttributeId);
                else if (magnitude.Source == GameplayMagnitudeSource.TargetAttributeSnapshot)
                    targetSnapshots.Add(magnitude.AttributeId);
            }
            return new GameplayEffectDefinitionData(
                definition.EffectId,
                definition.DefinitionRevision,
                definition.DisplayName,
                definition.DebugCategory,
                Copy(definition.Tags),
                definition.DurationPolicy,
                GameplayMagnitudeData.From(definition.DurationMagnitude),
                definition.HasPeriod,
                GameplayMagnitudeData.From(definition.PeriodMagnitude),
                definition.ExecuteOnApplication,
                definition.StackingPolicy,
                definition.MaxStacks,
                definition.DurationUpdate,
                definition.PeriodUpdate,
                definition.OverflowPolicy,
                parameters,
                Copy(new List<GameplayAttributeId>(sourceSnapshots)),
                Copy(new List<GameplayAttributeId>(targetSnapshots)),
                components);
        }

        static GameplayAttributeBoundData BuildBound(GameplayAttributeBoundDefinition bound)
        {
            return bound == null
                ? default
                : new GameplayAttributeBoundData(bound.Enabled, bound.Source, bound.Constant, bound.AttributeId);
        }

        static bool ValidateEffectGraph(
            ISet<GameplayEffectId> effectIds,
            IReadOnlyDictionary<GameplayEffectId, GameplayEffectReferenceCollector> collectors,
            List<string> errors)
        {
            var state = new Dictionary<GameplayEffectId, byte>();
            var path = new List<GameplayEffectId>();
            foreach (GameplayEffectId id in effectIds)
            {
                if (!VisitEffect(id, collectors, state, path, errors))
                    return false;
            }
            return true;
        }

        static bool VisitEffect(
            GameplayEffectId id,
            IReadOnlyDictionary<GameplayEffectId, GameplayEffectReferenceCollector> collectors,
            Dictionary<GameplayEffectId, byte> state,
            List<GameplayEffectId> path,
            List<string> errors)
        {
            if (state.TryGetValue(id, out byte value))
            {
                if (value == 2)
                    return true;
                if (value == 1)
                {
                    int start = path.IndexOf(id);
                    var cycle = new List<string>();
                    for (int i = Math.Max(0, start); i < path.Count; i++)
                        cycle.Add(path[i].Value);
                    cycle.Add(id.Value);
                    errors?.Add($"Additional Effect cycle: {string.Join(" -> ", cycle)}.");
                    return false;
                }
            }
            state[id] = 1;
            path.Add(id);
            if (collectors.TryGetValue(id, out GameplayEffectReferenceCollector collector))
            {
                for (int i = 0; i < collector.AdditionalEffects.Count; i++)
                {
                    GameplayEffectId child = collector.AdditionalEffects[i];
                    if (collectors.ContainsKey(child) && !VisitEffect(child, collectors, state, path, errors))
                        return false;
                }
            }
            path.RemoveAt(path.Count - 1);
            state[id] = 2;
            return true;
        }

        static bool ValidateAttributeGraph(
            ISet<GameplayAttributeId> attributeIds,
            IReadOnlyList<AttributeDependency> dependencies,
            List<string> errors)
        {
            var graph = new Dictionary<GameplayAttributeId, List<GameplayAttributeId>>();
            foreach (GameplayAttributeId id in attributeIds)
                graph[id] = new List<GameplayAttributeId>();
            for (int i = 0; i < dependencies.Count; i++)
            {
                AttributeDependency dependency = dependencies[i];
                if (graph.TryGetValue(dependency.Source, out List<GameplayAttributeId> values) && !values.Contains(dependency.Dependent))
                    values.Add(dependency.Dependent);
            }

            var state = new Dictionary<GameplayAttributeId, byte>();
            foreach (GameplayAttributeId id in attributeIds)
            {
                if (!VisitAttribute(id, graph, state))
                {
                    errors?.Add($"Gameplay Attribute dependency cycle contains '{id}'.");
                    return false;
                }
            }
            return true;
        }

        static bool VisitAttribute(
            GameplayAttributeId id,
            IReadOnlyDictionary<GameplayAttributeId, List<GameplayAttributeId>> graph,
            Dictionary<GameplayAttributeId, byte> state)
        {
            if (state.TryGetValue(id, out byte value))
                return value == 2;
            state[id] = 1;
            IReadOnlyList<GameplayAttributeId> children = graph[id];
            for (int i = 0; i < children.Count; i++)
            {
                GameplayAttributeId child = children[i];
                if (state.TryGetValue(child, out byte childState) && childState == 1)
                    return false;
                if (childState != 2 && !VisitAttribute(child, graph, state))
                    return false;
            }
            state[id] = 2;
            return true;
        }

        static T[] Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<T>();
            var copy = new T[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }
    }
}
