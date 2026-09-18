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

        public GameplayMagnitudeDefinition()
        {
        }

        public GameplayMagnitudeDefinition(
            GameplayMagnitudeSource source,
            float constant,
            string setByCallerParameterId,
            GameplayAttributeId attributeId,
            float coefficient,
            float postAdd)
        {
            m_Source = source;
            m_Constant = constant;
            m_SetByCallerParameterId = setByCallerParameterId ?? string.Empty;
            m_AttributeId = attributeId;
            m_Coefficient = coefficient;
            m_PostAdd = postAdd;
        }

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

    public enum GameplayAttackCollisionKind : byte
    {
        Box,
        BoxContinuous,
        FanWithHeight
    }

    [Serializable]
    public sealed class GameplayAttackCollisionComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] GameplayAttackCollisionKind m_Kind;
        [SerializeField] Vector3 m_CenterOffset;
        [SerializeField] float m_Width;
        [SerializeField] float m_Height;
        [SerializeField] float m_Depth;
        [SerializeField] float m_FanAngle;
        [SerializeField] float m_Radius;
        [SerializeField] float m_InvalidRadius;
        [SerializeField] float m_InvalidAngle;
        [SerializeField] int m_FollowDirectionType;
        [SerializeField] int m_CoreDistance;
        [SerializeField] bool m_IsSubtractive;
        [SerializeField] float m_HitInterval;
        [SerializeField] int m_AliveMaxHitCount;
        [SerializeField] int m_UnitMaxHitCount;
        [SerializeField] bool m_FollowAttacker;

        public GameplayAttackCollisionComponentDefinition(
            GameplayAttackCollisionKind kind,
            Vector3 centerOffset,
            float width,
            float height,
            float depth,
            float fanAngle,
            float radius,
            float invalidRadius,
            float invalidAngle,
            int followDirectionType,
            int coreDistance,
            bool isSubtractive,
            float hitInterval,
            int aliveMaxHitCount,
            int unitMaxHitCount,
            bool followAttacker)
        {
            m_Kind = kind;
            m_CenterOffset = centerOffset;
            m_Width = width;
            m_Height = height;
            m_Depth = depth;
            m_FanAngle = fanAngle;
            m_Radius = radius;
            m_InvalidRadius = invalidRadius;
            m_InvalidAngle = invalidAngle;
            m_FollowDirectionType = followDirectionType;
            m_CoreDistance = coreDistance;
            m_IsSubtractive = isSubtractive;
            m_HitInterval = hitInterval;
            m_AliveMaxHitCount = aliveMaxHitCount;
            m_UnitMaxHitCount = unitMaxHitCount;
            m_FollowAttacker = followAttacker;
        }

        public GameplayAttackCollisionKind Kind => m_Kind;
        public Vector3 CenterOffset => m_CenterOffset;
        public float Width => m_Width;
        public float Height => m_Height;
        public float Depth => m_Depth;
        public float FanAngle => m_FanAngle;
        public float Radius => m_Radius;
        public float InvalidRadius => m_InvalidRadius;
        public float InvalidAngle => m_InvalidAngle;
        public int FollowDirectionType => m_FollowDirectionType;
        public int CoreDistance => m_CoreDistance;
        public bool IsSubtractive => m_IsSubtractive;
        public float HitInterval => m_HitInterval;
        public int AliveMaxHitCount => m_AliveMaxHitCount;
        public int UnitMaxHitCount => m_UnitMaxHitCount;
        public bool FollowAttacker => m_FollowAttacker;
    }

    [Serializable]
    public sealed class GameplayAttackPropertyComponentDefinition : GameplayEffectComponentDefinition
    {
        [SerializeField] string m_SourceKey;
        [SerializeField] int m_HitType;
        [SerializeField] int m_HitStrengthType;
        [SerializeField] string[] m_CombatTags = Array.Empty<string>();
        [SerializeField] GameplayMagnitudeDefinition m_DamagePercentage = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_AddedDamage = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_BreakStunRatio = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_ElementAccumulation = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_ExhaustedAccumulation = new GameplayMagnitudeDefinition();
        [SerializeField] GameplayMagnitudeDefinition m_ExhaustedChase = new GameplayMagnitudeDefinition();
        [SerializeField] int m_DamageElement;
        [SerializeField] int m_DamageHitType;
        [SerializeField] int m_DamageBreakLevel;
        [SerializeField] GameplayMagnitudeDefinition m_DamageBreakLevelProbability = new GameplayMagnitudeDefinition();
        [SerializeField] int m_TriggerBuffLevel;
        [SerializeField] int m_DestructionClass;
        [SerializeField] int m_DestructionDurability;
        [SerializeField] int m_OverrideDamageStaggerLevel;
        [SerializeField] int m_DamageTextId;
        [SerializeField] int m_DamageTextWaitMilliseconds;
        [SerializeField] int m_FrameHalt;
        [SerializeField] int m_AttackerFrameHalt;
        [SerializeField] int m_GroundHitEffectId;
        [SerializeField] int m_SkyHitEffectId;
        [SerializeField] int m_DownHitEffectId;
        [SerializeField] string m_StandardConfigKey;
        [SerializeField] string m_AbilityTargetKey;
        [SerializeField] bool m_IsCauseStun;
        [SerializeField] bool m_IsHeavyAttack;
        [SerializeField] bool m_IsCauseExhausted;
        [SerializeField] bool m_IsHeal;
        [SerializeField] bool m_IsIndirect;
        [SerializeField] bool m_HitsEnemy;
        [SerializeField] bool m_HitsAllied;
        [SerializeField] bool m_HitsNeutral;
        [SerializeField] bool m_UseAbilityTargetKey;
        [SerializeField] bool m_BanDamage;

        public GameplayAttackPropertyComponentDefinition(
            string sourceKey,
            int hitType,
            int hitStrengthType,
            string[] combatTags,
            GameplayMagnitudeDefinition damagePercentage,
            GameplayMagnitudeDefinition addedDamage,
            GameplayMagnitudeDefinition breakStunRatio,
            GameplayMagnitudeDefinition elementAccumulation,
            GameplayMagnitudeDefinition exhaustedAccumulation,
            GameplayMagnitudeDefinition exhaustedChase,
            int damageElement,
            int damageHitType,
            int damageBreakLevel,
            GameplayMagnitudeDefinition damageBreakLevelProbability,
            int triggerBuffLevel,
            int destructionClass,
            int destructionDurability,
            int overrideDamageStaggerLevel,
            int damageTextId,
            int damageTextWaitMilliseconds,
            int frameHalt,
            int attackerFrameHalt,
            int groundHitEffectId,
            int skyHitEffectId,
            int downHitEffectId,
            string standardConfigKey,
            string abilityTargetKey,
            bool isCauseStun,
            bool isHeavyAttack,
            bool isCauseExhausted,
            bool isHeal,
            bool isIndirect,
            bool hitsEnemy,
            bool hitsAllied,
            bool hitsNeutral,
            bool useAbilityTargetKey,
            bool banDamage)
        {
            m_SourceKey = sourceKey;
            m_HitType = hitType;
            m_HitStrengthType = hitStrengthType;
            m_CombatTags = combatTags ?? Array.Empty<string>();
            m_DamagePercentage = damagePercentage;
            m_AddedDamage = addedDamage;
            m_BreakStunRatio = breakStunRatio;
            m_ElementAccumulation = elementAccumulation;
            m_ExhaustedAccumulation = exhaustedAccumulation;
            m_ExhaustedChase = exhaustedChase;
            m_DamageElement = damageElement;
            m_DamageHitType = damageHitType;
            m_DamageBreakLevel = damageBreakLevel;
            m_DamageBreakLevelProbability = damageBreakLevelProbability;
            m_TriggerBuffLevel = triggerBuffLevel;
            m_DestructionClass = destructionClass;
            m_DestructionDurability = destructionDurability;
            m_OverrideDamageStaggerLevel = overrideDamageStaggerLevel;
            m_DamageTextId = damageTextId;
            m_DamageTextWaitMilliseconds = damageTextWaitMilliseconds;
            m_FrameHalt = frameHalt;
            m_AttackerFrameHalt = attackerFrameHalt;
            m_GroundHitEffectId = groundHitEffectId;
            m_SkyHitEffectId = skyHitEffectId;
            m_DownHitEffectId = downHitEffectId;
            m_StandardConfigKey = standardConfigKey ?? string.Empty;
            m_AbilityTargetKey = abilityTargetKey ?? string.Empty;
            m_IsCauseStun = isCauseStun;
            m_IsHeavyAttack = isHeavyAttack;
            m_IsCauseExhausted = isCauseExhausted;
            m_IsHeal = isHeal;
            m_IsIndirect = isIndirect;
            m_HitsEnemy = hitsEnemy;
            m_HitsAllied = hitsAllied;
            m_HitsNeutral = hitsNeutral;
            m_UseAbilityTargetKey = useAbilityTargetKey;
            m_BanDamage = banDamage;
        }

        public string SourceKey => m_SourceKey ?? string.Empty;
        public int HitType => m_HitType;
        public int HitStrengthType => m_HitStrengthType;
        public IReadOnlyList<string> CombatTags => m_CombatTags ?? Array.Empty<string>();
        public GameplayMagnitudeDefinition DamagePercentage => m_DamagePercentage;
        public GameplayMagnitudeDefinition AddedDamage => m_AddedDamage;
        public GameplayMagnitudeDefinition BreakStunRatio => m_BreakStunRatio;
        public GameplayMagnitudeDefinition ElementAccumulation => m_ElementAccumulation;
        public GameplayMagnitudeDefinition ExhaustedAccumulation => m_ExhaustedAccumulation;
        public GameplayMagnitudeDefinition ExhaustedChase => m_ExhaustedChase;
        public int DamageElement => m_DamageElement;
        public int DamageHitType => m_DamageHitType;
        public int DamageBreakLevel => m_DamageBreakLevel;
        public GameplayMagnitudeDefinition DamageBreakLevelProbability => m_DamageBreakLevelProbability;
        public int TriggerBuffLevel => m_TriggerBuffLevel;
        public int DestructionClass => m_DestructionClass;
        public int DestructionDurability => m_DestructionDurability;
        public int OverrideDamageStaggerLevel => m_OverrideDamageStaggerLevel;
        public int DamageTextId => m_DamageTextId;
        public int DamageTextWaitMilliseconds => m_DamageTextWaitMilliseconds;
        public int FrameHalt => m_FrameHalt;
        public int AttackerFrameHalt => m_AttackerFrameHalt;
        public int GroundHitEffectId => m_GroundHitEffectId;
        public int SkyHitEffectId => m_SkyHitEffectId;
        public int DownHitEffectId => m_DownHitEffectId;
        public string StandardConfigKey => m_StandardConfigKey ?? string.Empty;
        public string AbilityTargetKey => m_AbilityTargetKey ?? string.Empty;
        public bool IsCauseStun => m_IsCauseStun;
        public bool IsHeavyAttack => m_IsHeavyAttack;
        public bool IsCauseExhausted => m_IsCauseExhausted;
        public bool IsHeal => m_IsHeal;
        public bool IsIndirect => m_IsIndirect;
        public bool HitsEnemy => m_HitsEnemy;
        public bool HitsAllied => m_HitsAllied;
        public bool HitsNeutral => m_HitsNeutral;
        public bool UseAbilityTargetKey => m_UseAbilityTargetKey;
        public bool BanDamage => m_BanDamage;
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
