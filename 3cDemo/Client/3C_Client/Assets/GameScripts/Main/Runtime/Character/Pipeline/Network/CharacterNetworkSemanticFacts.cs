using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public readonly struct IncomingGameplayEffectApplication
    {
        public IncomingGameplayEffectApplication(
            GameplayEffectId effectId,
            GameplayEffectInstanceId instanceId,
            ulong lifecycleRevision,
            uint definitionRevision,
            ulong predictionKey,
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues)
        {
            EffectId = effectId;
            InstanceId = instanceId;
            LifecycleRevision = lifecycleRevision;
            DefinitionRevision = definitionRevision;
            PredictionKey = predictionKey;
            SetByCallerValues = setByCallerValues ?? Array.Empty<GameplaySetByCallerValue>();
        }

        public GameplayEffectId EffectId { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public ulong LifecycleRevision { get; }
        public uint DefinitionRevision { get; }
        public ulong PredictionKey { get; }
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues { get; }
        public bool IsPresent => EffectId.IsValid;
        public bool IsValid => EffectId.IsValid && InstanceId.IsValid && LifecycleRevision != 0 && DefinitionRevision != 0;
    }

    public readonly struct IncomingGameplayResult
    {
        public IncomingGameplayResult(
            ulong resultId,
            ulong actionInstanceId,
            string windowId,
            string sourceActorId,
            string targetActorId,
            string resultType,
            string reason,
            ulong sourceTick,
            IncomingGameplayEffectApplication effectApplication)
        {
            ResultId = resultId;
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            SourceActorId = sourceActorId ?? string.Empty;
            TargetActorId = targetActorId ?? string.Empty;
            ResultType = resultType ?? string.Empty;
            Reason = reason ?? string.Empty;
            SourceTick = sourceTick;
            EffectApplication = effectApplication;
        }

        public ulong ResultId { get; }
        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string SourceActorId { get; }
        public string TargetActorId { get; }
        public string ResultType { get; }
        public string Reason { get; }
        public ulong SourceTick { get; }
        public IncomingGameplayEffectApplication EffectApplication { get; }
    }

    public readonly struct GameplayEffectLifecycleFact
    {
        public GameplayEffectLifecycleFact(
            GameplayEffectId effectId,
            GameplayEffectInstanceId instanceId,
            GameplayEffectLifecycleOperation operation,
            GameplayEffectContext context,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong lifecycleRevision,
            uint definitionRevision,
            bool instant,
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues,
            ulong localLogicTick)
        {
            EffectId = effectId;
            InstanceId = instanceId;
            Operation = operation;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            DefinitionRevision = definitionRevision;
            Instant = instant;
            SetByCallerValues = setByCallerValues ?? Array.Empty<GameplaySetByCallerValue>();
            LocalLogicTick = localLogicTick;
        }

        public string BehaviorId => EffectId.Value;
        public GameplayEffectId EffectId { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public GameplayEffectLifecycleOperation Operation { get; }
        public GameplayEffectContext Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public uint DefinitionRevision { get; }
        public bool Instant { get; }
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues { get; }
        public ulong LocalLogicTick { get; }
        public bool IsValid => EffectId.IsValid && InstanceId.IsValid && LifecycleRevision != 0 && DefinitionRevision != 0;
    }

    public readonly struct GameplayAttributeValueFact
    {
        public GameplayAttributeValueFact(
            GameplayAttributeId attributeId,
            float beforeBase,
            float baseValue,
            float beforeCurrent,
            float currentValue,
            ulong valueRevision,
            GameplayEffectId causeEffectId,
            GameplayEffectInstanceId causeEffectInstanceId,
            GameplayEffectContext causeContext,
            ulong localLogicTick)
        {
            AttributeId = attributeId;
            BeforeBase = beforeBase;
            BaseValue = baseValue;
            BeforeCurrent = beforeCurrent;
            CurrentValue = currentValue;
            ValueRevision = valueRevision;
            CauseEffectId = causeEffectId;
            CauseEffectInstanceId = causeEffectInstanceId;
            CauseContext = causeContext;
            LocalLogicTick = localLogicTick;
        }

        public GameplayAttributeId AttributeId { get; }
        public float BeforeBase { get; }
        public float BaseValue { get; }
        public float BeforeCurrent { get; }
        public float CurrentValue { get; }
        public ulong ValueRevision { get; }
        public GameplayEffectId CauseEffectId { get; }
        public string CauseBehaviorId => CauseEffectId.Value;
        public GameplayEffectInstanceId CauseEffectInstanceId { get; }
        public GameplayEffectContext CauseContext { get; }
        public ulong LocalLogicTick { get; }
        public bool IsValid => AttributeId.IsValid && ValueRevision != 0;
    }

    public readonly struct GameplayCueFact
    {
        public GameplayCueFact(
            string behaviorId,
            string cueId,
            string cueType,
            ulong sourceActionInstanceId,
            GameplayEffectId sourceEffectId,
            GameplayEffectInstanceId sourceEffectInstanceId,
            GameplayEffectContext context,
            ulong localLogicTick)
        {
            BehaviorId = behaviorId ?? string.Empty;
            CueId = cueId ?? string.Empty;
            CueType = cueType ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            SourceEffectId = sourceEffectId;
            SourceEffectInstanceId = sourceEffectInstanceId;
            Context = context;
            LocalLogicTick = localLogicTick;
        }

        public string BehaviorId { get; }
        public string CueId { get; }
        public string CueType { get; }
        public ulong SourceActionInstanceId { get; }
        public GameplayEffectId SourceEffectId { get; }
        public GameplayEffectInstanceId SourceEffectInstanceId { get; }
        public GameplayEffectContext Context { get; }
        public ulong LocalLogicTick { get; }
        public bool IsValid => !string.IsNullOrEmpty(CueId) && !string.IsNullOrEmpty(CueType);
    }
}
