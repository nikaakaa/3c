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

        public string ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? string.Empty : m_ParameterId.Trim();
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

    }

    [Serializable]
    public sealed class GrantedTagsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayTagId[] m_Tags = Array.Empty<GameplayTagId>();

        public IReadOnlyList<GameplayTagId> Tags => m_Tags ?? Array.Empty<GameplayTagId>();

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

    }

    public enum GameplayAdditionalEffectTrigger : byte
    {
        Applied,
        Period,
        Removed,
        Overflow
    }

    public enum GameplayAdditionalEffectParameterSource : byte
    {
        ParentSetByCaller,
        Constant
    }

    [Serializable]
    public sealed class GameplayAdditionalEffectParameterBindingDefinition
    {
        [SerializeField] string m_ChildParameterId;
        [SerializeField] GameplayAdditionalEffectParameterSource m_Source;
        [SerializeField] string m_ParentParameterId;
        [SerializeField] float m_Constant;

        public string ChildParameterId => string.IsNullOrWhiteSpace(m_ChildParameterId) ? string.Empty : m_ChildParameterId.Trim();
        public GameplayAdditionalEffectParameterSource Source => m_Source;
        public string ParentParameterId => string.IsNullOrWhiteSpace(m_ParentParameterId) ? string.Empty : m_ParentParameterId.Trim();
        public float Constant => m_Constant;
    }

    [Serializable]
    public sealed class GameplayAdditionalEffectDefinition
    {
        [SerializeField] GameplayAdditionalEffectTrigger m_Trigger;
        [SerializeField] GameplayEffectDefinition m_Effect;
        [SerializeField] GameplayAdditionalEffectParameterBindingDefinition[] m_ParameterBindings = Array.Empty<GameplayAdditionalEffectParameterBindingDefinition>();

        public GameplayAdditionalEffectTrigger Trigger => m_Trigger;
        public GameplayEffectDefinition Effect => m_Effect;
        public IReadOnlyList<GameplayAdditionalEffectParameterBindingDefinition> ParameterBindings => m_ParameterBindings ?? Array.Empty<GameplayAdditionalEffectParameterBindingDefinition>();
    }

    [Serializable]
    public sealed class AdditionalEffectsComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayAdditionalEffectDefinition[] m_Effects = Array.Empty<GameplayAdditionalEffectDefinition>();

        public IReadOnlyList<GameplayAdditionalEffectDefinition> Effects => m_Effects ?? Array.Empty<GameplayAdditionalEffectDefinition>();

    }

    [Serializable]
    public sealed class GameplayCueBindingComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] string m_CueId;
        [SerializeField] GameplayCueTrigger m_Trigger;

        public string CueId => string.IsNullOrWhiteSpace(m_CueId) ? string.Empty : m_CueId.Trim();
        public GameplayCueTrigger Trigger => m_Trigger;

    }
}
