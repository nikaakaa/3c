using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;
using UnityEngine;

namespace ThirdPersonGameplay.Effects
{
    public enum GameplayEffectDurationPolicy : byte
    {
        Instant,
        Duration,
        Infinite
    }

    public enum GameplayMagnitudeSource : byte
    {
        Constant,
        SetByCaller,
        SourceAttributeSnapshot,
        TargetAttributeSnapshot,
        TargetAttributeLive
    }

    [Serializable]
    public sealed class GameplayMagnitudeDefinition
    {
        [SerializeField] GameplayMagnitudeSource m_Source;
        [SerializeField] float m_Constant;
        [SerializeField] string m_SetByCallerParameterId;
        [SerializeField] GameplayAttributeId m_AttributeId;
        [SerializeField] float m_Coefficient = 1f;
        [SerializeField] float m_PostAdd;

        public GameplayMagnitudeSource Source => m_Source;
        public float Constant => m_Constant;
        public string SetByCallerParameterId => string.IsNullOrWhiteSpace(m_SetByCallerParameterId) ? string.Empty : m_SetByCallerParameterId.Trim();
        public GameplayAttributeId AttributeId => m_AttributeId;
        public float Coefficient => m_Coefficient;
        public float PostAdd => m_PostAdd;
    }

    [Serializable]
    public sealed class GameplaySetByCallerParameterDefinition
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] bool m_Required = true;

        public string ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? string.Empty : m_ParameterId.Trim();
        public bool Required => m_Required;
    }

    public enum GameplayEffectStackingPolicy : byte
    {
        Independent,
        AggregateBySource,
        AggregateByTarget
    }

    public enum GameplayEffectDurationUpdatePolicy : byte
    {
        Keep,
        Refresh,
        Extend
    }

    public enum GameplayEffectPeriodUpdatePolicy : byte
    {
        Keep,
        Reset
    }

    public enum GameplayEffectOverflowPolicy : byte
    {
        Reject,
        ReplaceOldest,
        ApplyOverflowEffects
    }

    [CreateAssetMenu(fileName = "GameplayEffectDefinition", menuName = "3C/Gameplay/Effect Definition")]
    public sealed class GameplayEffectDefinition : ScriptableObject, IGameplayBehaviorProfile
    {
        [SerializeField] GameplayEffectId m_EffectId;
        [SerializeField] uint m_DefinitionRevision = 1;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_DebugCategory;
        [SerializeField] GameplayTagId[] m_EffectTags = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayEffectDurationPolicy m_DurationPolicy;
        [SerializeField] GameplayMagnitudeDefinition m_DurationMagnitude = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_PeriodMagnitude = new GameplayMagnitudeDefinition();
        [SerializeField] bool m_HasPeriod;
        [SerializeField] bool m_ExecuteOnApplication;
        [SerializeField] GameplayEffectStackingPolicy m_StackingPolicy;
        [SerializeField] int m_MaxStacks = 1;
        [SerializeField] GameplayEffectDurationUpdatePolicy m_DurationUpdate;
        [SerializeField] GameplayEffectPeriodUpdatePolicy m_PeriodUpdate;
        [SerializeField] GameplayEffectOverflowPolicy m_OverflowPolicy;
        [SerializeField] GameplaySetByCallerParameterDefinition[] m_SetByCallerParameters = Array.Empty<GameplaySetByCallerParameterDefinition>();
        [SerializeReference] List<GameplayEffectComponentDefinition> m_Components = new List<GameplayEffectComponentDefinition>();

        public GameplayEffectId EffectId => m_EffectId;
        public uint DefinitionRevision => m_DefinitionRevision;
        public string BehaviorId => m_EffectId.Value;
        public GameplayBehaviorKind BehaviorKind => GameplayBehaviorKind.Effect;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public string DebugCategory => m_DebugCategory ?? string.Empty;
        public IReadOnlyList<GameplayTagId> Tags => m_EffectTags ?? Array.Empty<GameplayTagId>();
        public GameplayEffectDurationPolicy DurationPolicy => m_DurationPolicy;
        public GameplayMagnitudeDefinition DurationMagnitude => m_DurationMagnitude;
        public bool HasPeriod => m_HasPeriod;
        public GameplayMagnitudeDefinition PeriodMagnitude => m_PeriodMagnitude;
        public bool ExecuteOnApplication => m_ExecuteOnApplication;
        public GameplayEffectStackingPolicy StackingPolicy => m_StackingPolicy;
        public int MaxStacks => m_MaxStacks;
        public GameplayEffectDurationUpdatePolicy DurationUpdate => m_DurationUpdate;
        public GameplayEffectPeriodUpdatePolicy PeriodUpdate => m_PeriodUpdate;
        public GameplayEffectOverflowPolicy OverflowPolicy => m_OverflowPolicy;
        public IReadOnlyList<GameplaySetByCallerParameterDefinition> SetByCallerParameters => m_SetByCallerParameters ?? Array.Empty<GameplaySetByCallerParameterDefinition>();
        public IReadOnlyList<GameplayEffectComponentDefinition> Components => m_Components ?? (IReadOnlyList<GameplayEffectComponentDefinition>)Array.Empty<GameplayEffectComponentDefinition>();
    }

    [Serializable]
    public abstract class GameplayEffectComponentDefinition
    {
        internal abstract GameplayEffectComponentData BuildData();
        internal abstract void CollectReferences(GameplayEffectReferenceCollector collector);
    }

    public enum GameplayModifierApplication : byte
    {
        BaseValue,
        CurrentValue
    }

    [Serializable]
    public sealed class GameplayModifierComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayAttributeId m_AttributeId;
        [SerializeField] GameplayModifierApplication m_Application;
        [SerializeField] GameplayModifierOperation m_Operation;
        [SerializeField] GameplayMagnitudeDefinition m_Magnitude = new GameplayMagnitudeDefinition();
        [SerializeField] int m_Priority;
        [SerializeField] GameplayClampBound m_ClampBound;
        [SerializeField] bool m_ScaleWithStack = true;

        public GameplayAttributeId AttributeId => m_AttributeId;
        public GameplayModifierApplication Application => m_Application;
        public GameplayModifierOperation Operation => m_Operation;
        public GameplayMagnitudeDefinition Magnitude => m_Magnitude;
        public int Priority => m_Priority;
        public GameplayClampBound ClampBound => m_ClampBound;
        public bool ScaleWithStack => m_ScaleWithStack;

        internal override GameplayEffectComponentData BuildData()
        {
            return new GameplayModifierComponentData(
                m_AttributeId,
                m_Application,
                m_Operation,
                GameplayMagnitudeData.From(m_Magnitude),
                m_Priority,
                m_ClampBound,
                m_ScaleWithStack);
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            collector.AddAttribute(m_AttributeId);
            collector.AddMagnitude(m_Magnitude, m_Application == GameplayModifierApplication.CurrentValue ? m_AttributeId : default);
        }
    }

    [Serializable]
    public sealed class GrantedTagsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayTagId[] m_Tags = Array.Empty<GameplayTagId>();

        public IReadOnlyList<GameplayTagId> Tags => m_Tags ?? Array.Empty<GameplayTagId>();

        internal override GameplayEffectComponentData BuildData()
        {
            return new GrantedTagsComponentData(Copy(Tags));
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            for (int i = 0; i < Tags.Count; i++)
                collector.AddTag(Tags[i]);
        }

        static GameplayTagId[] Copy(IReadOnlyList<GameplayTagId> values)
        {
            var copy = new GameplayTagId[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            return copy;
        }
    }

    public enum GameplayEffectRequirementPhase : byte
    {
        Application,
        Ongoing,
        Removal
    }

    [Serializable]
    public sealed class GameplayTagRequirementsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayEffectRequirementPhase m_Phase;
        [SerializeField] GameplayTagQuery m_Source = new GameplayTagQuery();
        [SerializeField] GameplayTagQuery m_Target = new GameplayTagQuery();

        public GameplayEffectRequirementPhase Phase => m_Phase;
        public GameplayTagQuery Source => m_Source;
        public GameplayTagQuery Target => m_Target;

        internal override GameplayEffectComponentData BuildData()
        {
            return new GameplayTagRequirementsComponentData(m_Phase, m_Source, m_Target);
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            collector.AddQuery(m_Source);
            collector.AddQuery(m_Target);
        }
    }

    public enum GameplayEffectAttributeSource : byte
    {
        SourceSnapshot,
        Target
    }

    public enum GameplayAttributeComparison : byte
    {
        Less,
        LessOrEqual,
        Equal,
        GreaterOrEqual,
        Greater,
        NotEqual
    }

    [Serializable]
    public sealed class GameplayAttributeRequirementsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayEffectRequirementPhase m_Phase;
        [SerializeField] GameplayEffectAttributeSource m_Source;
        [SerializeField] GameplayAttributeId m_AttributeId;
        [SerializeField] GameplayAttributeComparison m_Comparison;
        [SerializeField] GameplayMagnitudeDefinition m_Threshold = new GameplayMagnitudeDefinition();

        public GameplayEffectRequirementPhase Phase => m_Phase;
        public GameplayEffectAttributeSource Source => m_Source;
        public GameplayAttributeId AttributeId => m_AttributeId;
        public GameplayAttributeComparison Comparison => m_Comparison;
        public GameplayMagnitudeDefinition Threshold => m_Threshold;

        internal override GameplayEffectComponentData BuildData()
        {
            return new GameplayAttributeRequirementsComponentData(
                m_Phase,
                m_Source,
                m_AttributeId,
                m_Comparison,
                GameplayMagnitudeData.From(m_Threshold));
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            collector.AddAttribute(m_AttributeId);
            collector.AddMagnitude(m_Threshold, default);
        }
    }

    [Serializable]
    public sealed class GameplayExecutionMutationDefinition
    {
        [SerializeField] GameplayAttributeId m_AttributeId;
        [SerializeField] GameplayModifierOperation m_Operation;
        [SerializeField] GameplayMagnitudeDefinition m_Magnitude = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayClampBound m_ClampBound;

        public GameplayAttributeId AttributeId => m_AttributeId;
        public GameplayModifierOperation Operation => m_Operation;
        public GameplayMagnitudeDefinition Magnitude => m_Magnitude;
        public GameplayClampBound ClampBound => m_ClampBound;
    }

    [Serializable]
    public sealed class GameplayEffectExecutionComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayExecutionMutationDefinition[] m_Mutations = Array.Empty<GameplayExecutionMutationDefinition>();

        public IReadOnlyList<GameplayExecutionMutationDefinition> Mutations => m_Mutations ?? Array.Empty<GameplayExecutionMutationDefinition>();

        internal override GameplayEffectComponentData BuildData()
        {
            var values = new GameplayExecutionMutationData[Mutations.Count];
            for (int i = 0; i < values.Length; i++)
            {
                GameplayExecutionMutationDefinition value = Mutations[i];
                values[i] = new GameplayExecutionMutationData(
                    value.AttributeId,
                    value.Operation,
                    GameplayMagnitudeData.From(value.Magnitude),
                    value.ClampBound);
            }
            return new GameplayEffectExecutionComponentData(values);
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            for (int i = 0; i < Mutations.Count; i++)
            {
                GameplayExecutionMutationDefinition value = Mutations[i];
                if (value == null)
                    continue;
                collector.AddAttribute(value.AttributeId);
                collector.AddMagnitude(value.Magnitude, default);
            }
        }
    }

    public enum GameplayAdditionalEffectTrigger : byte
    {
        Applied,
        Period,
        Removed,
        Overflow
    }

    [Serializable]
    public sealed class GameplayAdditionalEffectDefinition
    {
        [SerializeField] GameplayAdditionalEffectTrigger m_Trigger;
        [SerializeField] GameplayEffectDefinition m_Effect;

        public GameplayAdditionalEffectTrigger Trigger => m_Trigger;
        public GameplayEffectDefinition Effect => m_Effect;
    }

    [Serializable]
    public sealed class AdditionalEffectsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayAdditionalEffectDefinition[] m_Effects = Array.Empty<GameplayAdditionalEffectDefinition>();

        public IReadOnlyList<GameplayAdditionalEffectDefinition> Effects => m_Effects ?? Array.Empty<GameplayAdditionalEffectDefinition>();

        internal override GameplayEffectComponentData BuildData()
        {
            var values = new GameplayAdditionalEffectData[Effects.Count];
            for (int i = 0; i < values.Length; i++)
            {
                GameplayAdditionalEffectDefinition value = Effects[i];
                values[i] = new GameplayAdditionalEffectData(
                    value != null ? value.Trigger : GameplayAdditionalEffectTrigger.Applied,
                    value != null && value.Effect ? value.Effect.EffectId : default);
            }
            return new AdditionalEffectsComponentData(values);
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                GameplayAdditionalEffectDefinition value = Effects[i];
                collector.AddAdditionalEffect(value != null && value.Effect ? value.Effect.EffectId : default);
            }
        }
    }

    [Serializable]
    public sealed class GameplayCueBindingComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] string m_CueId;
        [SerializeField] GameplayCueTrigger m_Trigger;

        public string CueId => string.IsNullOrWhiteSpace(m_CueId) ? string.Empty : m_CueId.Trim();
        public GameplayCueTrigger Trigger => m_Trigger;

        internal override GameplayEffectComponentData BuildData()
        {
            return new GameplayCueBindingComponentData(CueId, m_Trigger);
        }

        internal override void CollectReferences(GameplayEffectReferenceCollector collector)
        {
            if (string.IsNullOrEmpty(CueId))
                collector.AddError("cue id is missing");
        }
    }

    internal sealed class GameplayEffectReferenceCollector
    {
        public List<GameplayTagId> Tags { get; } = new List<GameplayTagId>();
        public List<GameplayAttributeId> Attributes { get; } = new List<GameplayAttributeId>();
        public List<GameplayEffectId> AdditionalEffects { get; } = new List<GameplayEffectId>();
        public List<AttributeDependency> AttributeDependencies { get; } = new List<AttributeDependency>();
        public List<GameplayMagnitudeDefinition> Magnitudes { get; } = new List<GameplayMagnitudeDefinition>();
        public List<string> Errors { get; } = new List<string>();

        public void AddTag(GameplayTagId tagId) => Tags.Add(tagId);
        public void AddAttribute(GameplayAttributeId attributeId) => Attributes.Add(attributeId);
        public void AddAdditionalEffect(GameplayEffectId effectId) => AdditionalEffects.Add(effectId);
        public void AddQuery(GameplayTagQuery query)
        {
            if (query == null)
                return;
            for (int i = 0; i < query.All.Count; i++) AddTag(query.All[i]);
            for (int i = 0; i < query.Any.Count; i++) AddTag(query.Any[i]);
            for (int i = 0; i < query.None.Count; i++) AddTag(query.None[i]);
        }
        public void AddMagnitude(GameplayMagnitudeDefinition magnitude, GameplayAttributeId dependent)
        {
            if (magnitude == null)
            {
                AddError("magnitude is missing");
                return;
            }
            Magnitudes.Add(magnitude);
            if (magnitude.Source == GameplayMagnitudeSource.SourceAttributeSnapshot ||
                magnitude.Source == GameplayMagnitudeSource.TargetAttributeSnapshot ||
                magnitude.Source == GameplayMagnitudeSource.TargetAttributeLive)
                AddAttribute(magnitude.AttributeId);
            if (dependent.IsValid && magnitude.Source == GameplayMagnitudeSource.TargetAttributeLive)
                AttributeDependencies.Add(new AttributeDependency(magnitude.AttributeId, dependent));
        }
        public void AddError(string error) => Errors.Add(error ?? string.Empty);
    }

    internal readonly struct AttributeDependency
    {
        public AttributeDependency(GameplayAttributeId source, GameplayAttributeId dependent)
        {
            Source = source;
            Dependent = dependent;
        }
        public GameplayAttributeId Source { get; }
        public GameplayAttributeId Dependent { get; }
    }
}
