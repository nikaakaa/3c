using System;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public enum ServerAuthoritativeInputValueKind
    {
        None,
        Bool,
        Float,
        Vector2
    }

    public readonly struct ServerAuthoritativeInputValue
    {
        ServerAuthoritativeInputValue(string inputValueId, ServerAuthoritativeInputValueKind valueKind, bool boolValue, float floatValue, Vector2 vector2Value)
        {
            InputValueId = inputValueId ?? string.Empty;
            ValueKind = valueKind;
            BoolValue = boolValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
        }

        public string InputValueId { get; }
        public ServerAuthoritativeInputValueKind ValueKind { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }
        public Vector2 Vector2Value { get; }

        public static ServerAuthoritativeInputValue Bool(string inputValueId, bool value)
        {
            return new ServerAuthoritativeInputValue(inputValueId, ServerAuthoritativeInputValueKind.Bool, value, 0f, Vector2.zero);
        }

        public static ServerAuthoritativeInputValue Float(string inputValueId, float value)
        {
            return new ServerAuthoritativeInputValue(inputValueId, ServerAuthoritativeInputValueKind.Float, false, value, Vector2.zero);
        }

        public static ServerAuthoritativeInputValue Vector2ValueInput(string inputValueId, Vector2 value)
        {
            return new ServerAuthoritativeInputValue(inputValueId, ServerAuthoritativeInputValueKind.Vector2, false, 0f, value);
        }
    }

    public readonly struct ServerAuthoritativeInputRequest
    {
        public ServerAuthoritativeInputRequest(
            string requestId,
            ulong createdLocalLogicTick,
            ulong inputSequence,
            ulong expireLocalLogicTick,
            float bufferSeconds,
            int priority,
            bool consumed)
        {
            RequestId = requestId ?? string.Empty;
            CreatedLocalLogicTick = createdLocalLogicTick;
            InputSequence = inputSequence;
            ExpireLocalLogicTick = expireLocalLogicTick;
            BufferSeconds = bufferSeconds;
            Priority = priority;
            Consumed = consumed;
        }

        public string RequestId { get; }
        public ulong CreatedLocalLogicTick { get; }
        public ulong InputSequence { get; }
        public ulong ExpireLocalLogicTick { get; }
        public float BufferSeconds { get; }
        public int Priority { get; }
        public bool Consumed { get; }
    }

    public readonly struct ServerAuthoritativeMotionCommand
    {
        public ServerAuthoritativeMotionCommand(
            ServerAuthoritativeInputValue[] continuousInputValues,
            ServerAuthoritativeInputRequest[] actionRequests,
            Vector3 appliedDisplacement,
            float appliedYawDegrees,
            Vector3 resolvedPosition,
            Quaternion resolvedRotation,
            bool grounded,
            bool hasMotion,
            float horizontalSpeed)
        {
            ContinuousInputValues = continuousInputValues ?? Array.Empty<ServerAuthoritativeInputValue>();
            ActionRequests = actionRequests ?? Array.Empty<ServerAuthoritativeInputRequest>();
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
            ResolvedPosition = resolvedPosition;
            ResolvedRotation = resolvedRotation;
            Grounded = grounded;
            HasMotion = hasMotion;
            HorizontalSpeed = horizontalSpeed;
        }

        public ServerAuthoritativeInputValue[] ContinuousInputValues { get; }
        public ServerAuthoritativeInputRequest[] ActionRequests { get; }
        public Vector3 AppliedDisplacement { get; }
        public float AppliedYawDegrees { get; }
        public Vector3 ResolvedPosition { get; }
        public Quaternion ResolvedRotation { get; }
        public bool Grounded { get; }
        public bool HasMotion { get; }
        public float HorizontalSpeed { get; }
        public int ContinuousCommandCount => ContinuousInputValues.Length;
        public int ActionRequestCount => ActionRequests.Length;
    }

    public readonly struct ServerAuthoritativeActionActivation
    {
        public ServerAuthoritativeActionActivation(ulong actionInstanceId, string actionId, string sourceInputRequestId, string targetKey, string targetStableId)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId ?? string.Empty;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            TargetStableId = targetStableId ?? string.Empty;
        }

        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public string SourceInputRequestId { get; }
        public string TargetKey { get; }
        public string TargetStableId { get; }
    }

    public readonly struct ServerAuthoritativeActionLifecycleTransition
    {
        public ServerAuthoritativeActionLifecycleTransition(
            ulong actionInstanceId,
            ServerAuthoritativeActionLifecycleTransitionKind transitionKind,
            string reason,
            string sourceGraphId,
            string sourceNodeId,
            string sourceName,
            ulong correctionId)
        {
            ActionInstanceId = actionInstanceId;
            TransitionKind = transitionKind;
            Reason = reason ?? string.Empty;
            SourceGraphId = sourceGraphId ?? string.Empty;
            SourceNodeId = sourceNodeId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            CorrectionId = correctionId;
        }

        public ulong ActionInstanceId { get; }
        public ServerAuthoritativeActionLifecycleTransitionKind TransitionKind { get; }
        public string Reason { get; }
        public string SourceGraphId { get; }
        public string SourceNodeId { get; }
        public string SourceName { get; }
        public ulong CorrectionId { get; }
    }

    public readonly struct ServerAuthoritativeActionInstanceDecision
    {
        public ServerAuthoritativeActionInstanceDecision(
            ulong actionInstanceId,
            string actionId,
            ulong predictionKey,
            ulong inputSequence,
            ulong localLogicTick,
            ulong serverTick,
            ServerAuthoritativeActionDecisionKind decision,
            string reason,
            bool defenseFavorApplied)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId ?? string.Empty;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            LocalLogicTick = localLogicTick;
            ServerTick = serverTick;
            Decision = decision;
            Reason = reason ?? string.Empty;
            DefenseFavorApplied = defenseFavorApplied;
        }

        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public ulong ServerTick { get; }
        public ServerAuthoritativeActionDecisionKind Decision { get; }
        public string Reason { get; }
        public bool DefenseFavorApplied { get; }
    }

    public readonly struct ServerAuthoritativeActionWindowDigest
    {
        public ServerAuthoritativeActionWindowDigest(
            ulong actionInstanceId,
            string windowId,
            string windowType,
            ulong startLocalLogicTick,
            ulong endLocalLogicTick,
            ulong digest,
            string rawDigest)
        {
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            WindowType = windowType ?? string.Empty;
            StartLocalLogicTick = startLocalLogicTick;
            EndLocalLogicTick = endLocalLogicTick;
            Digest = digest;
            RawDigest = rawDigest ?? string.Empty;
        }

        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string WindowType { get; }
        public ulong StartLocalLogicTick { get; }
        public ulong EndLocalLogicTick { get; }
        public ulong Digest { get; }
        public string RawDigest { get; }
    }

    public readonly struct ServerAuthoritativeActionMotionDigest
    {
        public ServerAuthoritativeActionMotionDigest(ulong actionInstanceId, string sourceType)
        {
            ActionInstanceId = actionInstanceId;
            SourceType = sourceType ?? string.Empty;
        }

        public ulong ActionInstanceId { get; }
        public string SourceType { get; }
    }

    public readonly struct ServerAuthoritativeGameplayResult
    {
        public ServerAuthoritativeGameplayResult(
            ulong resultId,
            ulong actionInstanceId,
            string windowId,
            string sourceActorId,
            string targetActorId,
            string resultType,
            string reason)
        {
            ResultId = resultId;
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            SourceActorId = sourceActorId ?? string.Empty;
            TargetActorId = targetActorId ?? string.Empty;
            ResultType = resultType ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public ulong ResultId { get; }
        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string SourceActorId { get; }
        public string TargetActorId { get; }
        public string ResultType { get; }
        public string Reason { get; }
    }

    public readonly struct ServerAuthoritativeGameplayEffectLifecycle
    {
        public ServerAuthoritativeGameplayEffectLifecycle(
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
            GameplaySetByCallerValue[] setByCallerValues)
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
        public GameplaySetByCallerValue[] SetByCallerValues { get; }
    }

    public readonly struct ServerAuthoritativeGameplayAttributeValue
    {
        public ServerAuthoritativeGameplayAttributeValue(
            GameplayAttributeId attributeId,
            float beforeBase,
            float baseValue,
            float beforeCurrent,
            float currentValue,
            ulong valueRevision,
            GameplayEffectId causeEffectId,
            GameplayEffectInstanceId causeEffectInstanceId,
            GameplayEffectContext causeContext)
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
    }

    public readonly struct ServerAuthoritativeGameplayCue
    {
        public ServerAuthoritativeGameplayCue(
            string behaviorId,
            string cueId,
            string cueType,
            ulong sourceActionInstanceId,
            GameplayEffectId sourceEffectId,
            GameplayEffectInstanceId sourceEffectInstanceId,
            GameplayEffectContext context)
        {
            BehaviorId = behaviorId ?? string.Empty;
            CueId = cueId ?? string.Empty;
            CueType = cueType ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            SourceEffectId = sourceEffectId;
            SourceEffectInstanceId = sourceEffectInstanceId;
            Context = context;
        }

        public string BehaviorId { get; }
        public string CueId { get; }
        public string CueType { get; }
        public ulong SourceActionInstanceId { get; }
        public GameplayEffectId SourceEffectId { get; }
        public GameplayEffectInstanceId SourceEffectInstanceId { get; }
        public GameplayEffectContext Context { get; }
    }

    public readonly struct ServerAuthoritativeMotionSnapshot
    {
        public ServerAuthoritativeMotionSnapshot(ulong serverTick, Vector3 position, Quaternion rotation, string stateId)
        {
            ServerTick = serverTick;
            Position = position;
            Rotation = rotation;
            StateId = stateId ?? string.Empty;
        }

        public ulong ServerTick { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public string StateId { get; }
    }

    public readonly struct ServerAuthoritativeMotionCorrection
    {
        public ServerAuthoritativeMotionCorrection(ulong inputSequence, ulong serverTick, Vector3 position, Quaternion rotation)
        {
            InputSequence = inputSequence;
            ServerTick = serverTick;
            Position = position;
            Rotation = rotation;
        }

        public ulong InputSequence { get; }
        public ulong ServerTick { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }

    public readonly struct ServerAuthoritativeMotionCorrectionAcknowledgement
    {
        public ServerAuthoritativeMotionCorrectionAcknowledgement(ulong inputSequence, ulong serverTick)
        {
            InputSequence = inputSequence;
            ServerTick = serverTick;
        }

        public ulong InputSequence { get; }
        public ulong ServerTick { get; }
    }
}
