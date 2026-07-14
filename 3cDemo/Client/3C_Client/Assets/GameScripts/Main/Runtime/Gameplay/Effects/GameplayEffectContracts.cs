using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;
using UnityEngine;

namespace ThirdPersonGameplay.Effects
{
    [Serializable]
    public struct GameplayEffectId : IEquatable<GameplayEffectId>, IComparable<GameplayEffectId>
    {
        [SerializeField] string m_Value;

        public GameplayEffectId(string value)
        {
            m_Value = Normalize(value);
        }

        public string Value => Normalize(m_Value);
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(GameplayEffectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayEffectId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(GameplayEffectId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;
        public static bool operator ==(GameplayEffectId left, GameplayEffectId right) => left.Equals(right);
        public static bool operator !=(GameplayEffectId left, GameplayEffectId right) => !left.Equals(right);

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct GameplayEffectTickContext
    {
        public GameplayEffectTickContext(ulong localLogicTick, float fixedDeltaSeconds)
        {
            LocalLogicTick = localLogicTick;
            FixedDeltaSeconds = fixedDeltaSeconds;
        }

        public ulong LocalLogicTick { get; }
        public float FixedDeltaSeconds { get; }
        public bool IsValid => LocalLogicTick != 0 && GameplayNumber.IsFinite(FixedDeltaSeconds) && FixedDeltaSeconds > 0f;
    }

    public enum GameplayEffectApplicationMode : byte
    {
        Confirmed,
        Predicted
    }

    public readonly struct GameplayEffectContext
    {
        public GameplayEffectContext(
            string sourceActorId,
            string targetActorId,
            ulong sourceActionInstanceId,
            ulong predictionKey,
            ulong gameplayResultId,
            ulong sourceLogicTick,
            GameplayEffectApplicationMode applicationMode)
        {
            SourceActorId = sourceActorId ?? string.Empty;
            TargetActorId = targetActorId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            PredictionKey = predictionKey;
            GameplayResultId = gameplayResultId;
            SourceLogicTick = sourceLogicTick;
            ApplicationMode = applicationMode;
        }

        public string SourceActorId { get; }
        public string TargetActorId { get; }
        public ulong SourceActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong GameplayResultId { get; }
        public ulong SourceLogicTick { get; }
        public GameplayEffectApplicationMode ApplicationMode { get; }
        public bool IsPredicted => ApplicationMode == GameplayEffectApplicationMode.Predicted;
        public bool HasPredictionIdentity => SourceActionInstanceId != 0 && PredictionKey != 0;
    }

    public readonly struct GameplayEffectInstanceId : IEquatable<GameplayEffectInstanceId>
    {
        public GameplayEffectInstanceId(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(GameplayEffectInstanceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayEffectInstanceId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(GameplayEffectInstanceId left, GameplayEffectInstanceId right) => left.Equals(right);
        public static bool operator !=(GameplayEffectInstanceId left, GameplayEffectInstanceId right) => !left.Equals(right);
    }

    public readonly struct GameplayEffectHandle : IEquatable<GameplayEffectHandle>
    {
        public GameplayEffectHandle(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(GameplayEffectHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayEffectHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(GameplayEffectHandle left, GameplayEffectHandle right) => left.Equals(right);
        public static bool operator !=(GameplayEffectHandle left, GameplayEffectHandle right) => !left.Equals(right);
    }

    [Serializable]
    public struct GameplaySetByCallerValue
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] float m_Value;

        public GameplaySetByCallerValue(string parameterId, float value)
        {
            m_ParameterId = parameterId ?? string.Empty;
            m_Value = value;
        }

        public string ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? string.Empty : m_ParameterId.Trim();
        public float Value => m_Value;
    }

    public readonly struct GameplayAttributeCapture
    {
        public GameplayAttributeCapture(GameplayAttributeId attributeId, float value)
        {
            AttributeId = attributeId;
            Value = value;
        }

        public GameplayAttributeId AttributeId { get; }
        public float Value { get; }
    }

    public sealed class GameplayEffectApplyRequest
    {
        public GameplayEffectApplyRequest(
            GameplayEffectId effectId,
            GameplayEffectContext context,
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues = null,
            IReadOnlyList<GameplayAttributeCapture> sourceAttributeSnapshots = null,
            IReadOnlyList<GameplayTagId> sourceTagSnapshot = null,
            GameplayEffectInstanceId authoritativeInstanceId = default,
            ulong authoritativeLifecycleRevision = 0,
            uint definitionRevision = 0)
        {
            EffectId = effectId;
            Context = context;
            SetByCallerValues = Copy(setByCallerValues);
            SourceAttributeSnapshots = Copy(sourceAttributeSnapshots);
            SourceTagSnapshot = Copy(sourceTagSnapshot);
            AuthoritativeInstanceId = authoritativeInstanceId;
            AuthoritativeLifecycleRevision = authoritativeLifecycleRevision;
            DefinitionRevision = definitionRevision;
        }

        public GameplayEffectId EffectId { get; }
        public GameplayEffectContext Context { get; }
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues { get; }
        public IReadOnlyList<GameplayAttributeCapture> SourceAttributeSnapshots { get; }
        public IReadOnlyList<GameplayTagId> SourceTagSnapshot { get; }
        public GameplayEffectInstanceId AuthoritativeInstanceId { get; }
        public ulong AuthoritativeLifecycleRevision { get; }
        public uint DefinitionRevision { get; }

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

    public enum GameplayEffectApplyResultCode : byte
    {
        Applied,
        Rejected,
        MissingDefinition,
        InvalidContext,
        InvalidPrediction,
        MissingSetByCaller,
        UndeclaredSetByCaller,
        RequirementFailed,
        OverflowRejected,
        DefinitionRevisionMismatch,
        Disposed
    }

    public readonly struct GameplayEffectCanApplyResult
    {
        public GameplayEffectCanApplyResult(bool allowed, GameplayEffectApplyResultCode code, string reason)
        {
            Allowed = allowed;
            Code = code;
            Reason = reason ?? string.Empty;
        }

        public bool Allowed { get; }
        public GameplayEffectApplyResultCode Code { get; }
        public string Reason { get; }
    }

    public readonly struct GameplayEffectApplyResult
    {
        public GameplayEffectApplyResult(
            GameplayEffectApplyResultCode code,
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            string reason)
        {
            Code = code;
            Handle = handle;
            InstanceId = instanceId;
            Reason = reason ?? string.Empty;
        }

        public GameplayEffectApplyResultCode Code { get; }
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public string Reason { get; }
        public bool Succeeded => Code == GameplayEffectApplyResultCode.Applied;
    }

    public enum GameplayEffectRemoveSelector : byte
    {
        Handle,
        EffectId,
        SourceActorId,
        EffectTagQuery
    }

    public readonly struct GameplayEffectRemoveRequest
    {
        public GameplayEffectRemoveRequest(
            GameplayEffectRemoveSelector selector,
            GameplayEffectHandle handle = default,
            GameplayEffectId effectId = default,
            string sourceActorId = "",
            GameplayTagQuery effectTagQuery = null)
        {
            Selector = selector;
            Handle = handle;
            EffectId = effectId;
            SourceActorId = sourceActorId ?? string.Empty;
            EffectTagQuery = effectTagQuery;
        }

        public GameplayEffectRemoveSelector Selector { get; }
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectId EffectId { get; }
        public string SourceActorId { get; }
        public GameplayTagQuery EffectTagQuery { get; }
        public static GameplayEffectRemoveRequest ByHandle(GameplayEffectHandle handle) => new GameplayEffectRemoveRequest(GameplayEffectRemoveSelector.Handle, handle);
        public static GameplayEffectRemoveRequest ByEffect(GameplayEffectId effectId) => new GameplayEffectRemoveRequest(GameplayEffectRemoveSelector.EffectId, effectId: effectId);
        public static GameplayEffectRemoveRequest BySource(string sourceActorId) => new GameplayEffectRemoveRequest(GameplayEffectRemoveSelector.SourceActorId, sourceActorId: sourceActorId);
        public static GameplayEffectRemoveRequest ByTags(GameplayTagQuery query) => new GameplayEffectRemoveRequest(GameplayEffectRemoveSelector.EffectTagQuery, effectTagQuery: query);
    }

    public readonly struct GameplayEffectRemoveResult
    {
        public GameplayEffectRemoveResult(IReadOnlyList<GameplayEffectHandle> removedHandles)
        {
            RemovedHandles = removedHandles ?? Array.Empty<GameplayEffectHandle>();
        }

        public IReadOnlyList<GameplayEffectHandle> RemovedHandles { get; }
        public bool RemovedAny => RemovedHandles.Count > 0;
    }

    public enum GameplayEffectLifecycleOperation : byte
    {
        Applied,
        Confirmed,
        Rejected,
        StackChanged,
        Inhibited,
        Resumed,
        PeriodExecuted,
        Removed,
        Expired,
        Corrected,
        Overflow
    }

    public readonly struct GameplayEffectLifecycleChange
    {
        public GameplayEffectLifecycleChange(
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
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues = null)
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
        }

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
    }

    public enum GameplayCueTrigger : byte
    {
        OnActive,
        Executed,
        WhileActive,
        Removed,
        Expired
    }

    public readonly struct GameplayCueChange
    {
        public GameplayCueChange(
            string cueId,
            GameplayCueTrigger trigger,
            GameplayEffectId effectId,
            GameplayEffectInstanceId instanceId,
            GameplayEffectContext context)
        {
            CueId = cueId ?? string.Empty;
            Trigger = trigger;
            EffectId = effectId;
            InstanceId = instanceId;
            Context = context;
        }

        public string CueId { get; }
        public GameplayCueTrigger Trigger { get; }
        public GameplayEffectId EffectId { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public GameplayEffectContext Context { get; }
    }

    public readonly struct GameplayEffectAttributeChange
    {
        public GameplayEffectAttributeChange(
            GameplayAttributeChange value,
            GameplayEffectId causeEffectId,
            GameplayEffectInstanceId causeEffectInstanceId,
            GameplayEffectContext causeContext)
        {
            Value = value;
            CauseEffectId = causeEffectId;
            CauseEffectInstanceId = causeEffectInstanceId;
            CauseContext = causeContext;
        }

        public GameplayAttributeChange Value { get; }
        public GameplayEffectId CauseEffectId { get; }
        public GameplayEffectInstanceId CauseEffectInstanceId { get; }
        public GameplayEffectContext CauseContext { get; }
    }

    public readonly struct GameplayEffectExecutionFailure
    {
        public GameplayEffectExecutionFailure(
            GameplayEffectId ownerEffectId,
            GameplayEffectInstanceId ownerInstanceId,
            GameplayEffectLifecycleOperation trigger,
            GameplayEffectId requestedEffectId,
            GameplayEffectApplyResultCode code,
            string reason)
        {
            OwnerEffectId = ownerEffectId;
            OwnerInstanceId = ownerInstanceId;
            Trigger = trigger;
            RequestedEffectId = requestedEffectId;
            Code = code;
            Reason = reason ?? string.Empty;
        }

        public GameplayEffectId OwnerEffectId { get; }
        public GameplayEffectInstanceId OwnerInstanceId { get; }
        public GameplayEffectLifecycleOperation Trigger { get; }
        public GameplayEffectId RequestedEffectId { get; }
        public GameplayEffectApplyResultCode Code { get; }
        public string Reason { get; }
    }

    public sealed class GameplayEffectChangeSet
    {
        public ulong LocalLogicTick { get; internal set; }
        public List<GameplayEffectLifecycleChange> EffectChanges { get; } = new List<GameplayEffectLifecycleChange>();
        public List<GameplayEffectAttributeChange> AttributeChanges { get; } = new List<GameplayEffectAttributeChange>();
        public List<GameplayTagCountChange> TagChanges { get; } = new List<GameplayTagCountChange>();
        public List<GameplayCueChange> CueChanges { get; } = new List<GameplayCueChange>();
        public List<GameplayEffectExecutionFailure> ExecutionFailures { get; } = new List<GameplayEffectExecutionFailure>();
        public bool HasChanges => EffectChanges.Count > 0 || AttributeChanges.Count > 0 || TagChanges.Count > 0 || CueChanges.Count > 0 || ExecutionFailures.Count > 0;
    }

    public enum GameplayEffectAuthorityInputKind : byte
    {
        Lifecycle,
        AttributeValue,
        ConfirmPrediction,
        RejectPrediction,
        CorrectPrediction
    }

    public sealed class GameplayEffectAuthorityInput
    {
        public GameplayEffectAuthorityInput(
            GameplayEffectAuthorityInputKind kind,
            GameplayEffectLifecycleOperation operation = GameplayEffectLifecycleOperation.Applied,
            GameplayEffectId effectId = default,
            GameplayEffectInstanceId instanceId = default,
            GameplayEffectContext context = default,
            ulong startTick = 0,
            ulong endTick = 0,
            int stackCount = 0,
            ulong lifecycleRevision = 0,
            uint definitionRevision = 0,
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues = null,
            GameplayAttributeId attributeId = default,
            float baseValue = 0f,
            float currentValue = 0f,
            ulong valueRevision = 0,
            GameplayEffectInstanceId causeEffectInstanceId = default,
            ulong predictionKey = 0,
            ulong actionInstanceId = 0)
        {
            Kind = kind;
            Operation = operation;
            EffectId = effectId;
            InstanceId = instanceId;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            DefinitionRevision = definitionRevision;
            SetByCallerValues = setByCallerValues ?? Array.Empty<GameplaySetByCallerValue>();
            AttributeId = attributeId;
            BaseValue = baseValue;
            CurrentValue = currentValue;
            ValueRevision = valueRevision;
            CauseEffectInstanceId = causeEffectInstanceId;
            PredictionKey = predictionKey;
            ActionInstanceId = actionInstanceId;
        }

        public GameplayEffectAuthorityInputKind Kind { get; }
        public GameplayEffectLifecycleOperation Operation { get; }
        public GameplayEffectId EffectId { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public GameplayEffectContext Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public uint DefinitionRevision { get; }
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues { get; }
        public GameplayAttributeId AttributeId { get; }
        public float BaseValue { get; }
        public float CurrentValue { get; }
        public ulong ValueRevision { get; }
        public GameplayEffectInstanceId CauseEffectInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong ActionInstanceId { get; }
    }

    public enum GameplayEffectReconcileResult : byte
    {
        Applied,
        IgnoredStaleRevision,
        MissingDefinition,
        DefinitionRevisionMismatch,
        PredictionNotFound,
        Conflict,
        InvalidInput,
        Disposed
    }
}
