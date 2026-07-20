using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public enum SimulationGameplayEffectApplicationMode : byte
    {
        Confirmed = 0,
        Predicted = 1
    }

    public enum SimulationGameplayEffectLifecycleOperation : byte
    {
        Applied = 0,
        Confirmed = 1,
        Rejected = 2,
        StackChanged = 3,
        Inhibited = 4,
        Resumed = 5,
        PeriodExecuted = 6,
        Removed = 7,
        Expired = 8,
        Corrected = 9,
        Overflow = 10
    }

    public enum SimulationGameplayEffectApplyResultCode : byte
    {
        Applied = 0,
        Rejected = 1,
        MissingDefinition = 2,
        InvalidContext = 3,
        InvalidPrediction = 4,
        MissingSetByCaller = 5,
        UndeclaredSetByCaller = 6,
        RequirementFailed = 7,
        OverflowRejected = 8,
        DefinitionRevisionMismatch = 9
    }

    public readonly struct SimulationSetByCallerValue
    {
        public SimulationSetByCallerValue(string parameterId, Float32Scalar value)
        {
            ParameterId = SimulationIdentity.Require(parameterId, nameof(parameterId));
            Value = value;
        }

        public string ParameterId { get; }
        public Float32Scalar Value { get; }
    }

    public readonly struct SimulationAttributeCapture
    {
        public SimulationAttributeCapture(string attributeId, Float32Scalar value)
        {
            AttributeId = SimulationIdentity.Require(attributeId, nameof(attributeId));
            Value = value;
        }

        public string AttributeId { get; }
        public Float32Scalar Value { get; }
    }

    public readonly struct SimulationGameplayEffectContext
    {
        public SimulationGameplayEffectContext(
            ActorId sourceActorId,
            ActorId targetActorId,
            ulong sourceActionInstanceId,
            ulong predictionKey,
            ulong gameplayResultId,
            ulong sourceTick,
            SimulationGameplayEffectApplicationMode applicationMode)
        {
            if (!sourceActorId.IsValid || !targetActorId.IsValid || sourceTick == 0)
                throw new ArgumentException("Gameplay Effect context identity is incomplete.");
            if (applicationMode == SimulationGameplayEffectApplicationMode.Predicted &&
                (sourceActionInstanceId == 0 || predictionKey == 0))
            {
                throw new ArgumentException("Predicted Gameplay Effect context requires Action and prediction identities.");
            }
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceActionInstanceId = sourceActionInstanceId;
            PredictionKey = predictionKey;
            GameplayResultId = gameplayResultId;
            SourceTick = sourceTick;
            ApplicationMode = applicationMode;
        }

        public ActorId SourceActorId { get; }
        public ActorId TargetActorId { get; }
        public ulong SourceActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong GameplayResultId { get; }
        public ulong SourceTick { get; }
        public SimulationGameplayEffectApplicationMode ApplicationMode { get; }
        public bool IsPredicted => ApplicationMode == SimulationGameplayEffectApplicationMode.Predicted;
        public bool IsValid => SourceActorId.IsValid && TargetActorId.IsValid && SourceTick != 0;
    }

    public sealed class SimulationGameplayEffectApplication
    {
        readonly SimulationSetByCallerValue[] m_SetByCallerValues;
        readonly SimulationAttributeCapture[] m_SourceAttributeSnapshots;
        readonly string[] m_SourceTagSnapshot;

        public SimulationGameplayEffectApplication(
            string effectId,
            uint definitionRevision,
            SimulationGameplayEffectContext context,
            IEnumerable<SimulationSetByCallerValue> setByCallerValues = null,
            IEnumerable<SimulationAttributeCapture> sourceAttributeSnapshots = null,
            IEnumerable<string> sourceTagSnapshot = null,
            ulong authoritativeInstanceId = 0,
            ulong authoritativeLifecycleRevision = 0)
        {
            EffectId = SimulationIdentity.Require(effectId, nameof(effectId));
            if (definitionRevision == 0 || !context.IsValid)
                throw new ArgumentException("Gameplay Effect application is incomplete.");
            DefinitionRevision = definitionRevision;
            Context = context;
            AuthoritativeInstanceId = authoritativeInstanceId;
            AuthoritativeLifecycleRevision = authoritativeLifecycleRevision;
            m_SetByCallerValues = CopySetByCaller(setByCallerValues);
            m_SourceAttributeSnapshots = CopyAttributes(sourceAttributeSnapshots);
            m_SourceTagSnapshot = CopyIdentities(sourceTagSnapshot, nameof(sourceTagSnapshot));
        }

        SimulationGameplayEffectApplication(
            string effectId,
            uint definitionRevision,
            SimulationGameplayEffectContext context,
            SimulationSetByCallerValue[] compiledSetByCallerValues)
        {
            EffectId = SimulationIdentity.Require(effectId, nameof(effectId));
            if (definitionRevision == 0 || !context.IsValid)
                throw new ArgumentException("Gameplay Effect application is incomplete.");
            DefinitionRevision = definitionRevision;
            Context = context;
            m_SetByCallerValues = compiledSetByCallerValues ?? Array.Empty<SimulationSetByCallerValue>();
            m_SourceAttributeSnapshots = Array.Empty<SimulationAttributeCapture>();
            m_SourceTagSnapshot = Array.Empty<string>();
        }

        internal static SimulationGameplayEffectApplication FromCompiled(
            string effectId,
            uint definitionRevision,
            SimulationGameplayEffectContext context,
            SimulationSetByCallerValue[] setByCallerValues) =>
            new SimulationGameplayEffectApplication(effectId, definitionRevision, context, setByCallerValues);

        public string EffectId { get; }
        public uint DefinitionRevision { get; }
        public SimulationGameplayEffectContext Context { get; }
        public IReadOnlyList<SimulationSetByCallerValue> SetByCallerValues => m_SetByCallerValues;
        public IReadOnlyList<SimulationAttributeCapture> SourceAttributeSnapshots => m_SourceAttributeSnapshots;
        public IReadOnlyList<string> SourceTagSnapshot => m_SourceTagSnapshot;
        public ulong AuthoritativeInstanceId { get; }
        public ulong AuthoritativeLifecycleRevision { get; }

        static SimulationSetByCallerValue[] CopySetByCaller(IEnumerable<SimulationSetByCallerValue> source)
        {
            var values = source == null ? new List<SimulationSetByCallerValue>() : new List<SimulationSetByCallerValue>(source);
            values.Sort((left, right) => string.CompareOrdinal(left.ParameterId, right.ParameterId));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1].ParameterId, values[i].ParameterId, StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate SetByCaller parameter '{values[i].ParameterId}'.", nameof(source));
            }
            return values.ToArray();
        }

        static SimulationAttributeCapture[] CopyAttributes(IEnumerable<SimulationAttributeCapture> source)
        {
            var values = source == null ? new List<SimulationAttributeCapture>() : new List<SimulationAttributeCapture>(source);
            values.Sort((left, right) => string.CompareOrdinal(left.AttributeId, right.AttributeId));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1].AttributeId, values[i].AttributeId, StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate source Attribute snapshot '{values[i].AttributeId}'.", nameof(source));
            }
            return values.ToArray();
        }

        static string[] CopyIdentities(IEnumerable<string> source, string parameterName)
        {
            var values = source == null ? new List<string>() : new List<string>(source);
            for (int i = 0; i < values.Count; i++)
                values[i] = SimulationIdentity.Require(values[i], parameterName);
            values.Sort(StringComparer.Ordinal);
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate identity '{values[i]}'.", parameterName);
            }
            return values.ToArray();
        }
    }

    public readonly struct SimulationGameplayResultIngress
    {
        public SimulationGameplayResultIngress(SimulationGameplayEffectApplication application)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public SimulationGameplayEffectApplication Application { get; }
        public bool IsValid => Application != null;
    }

    public sealed class SimulationGameplayEffectLifecycleIngress
    {
        readonly SimulationSetByCallerValue[] m_SetByCallerValues;

        public SimulationGameplayEffectLifecycleIngress(
            SimulationGameplayEffectLifecycleOperation operation,
            string effectId,
            ulong instanceId,
            SimulationGameplayEffectContext context,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong lifecycleRevision,
            uint definitionRevision,
            IEnumerable<SimulationSetByCallerValue> setByCallerValues = null)
        {
            Operation = operation;
            EffectId = effectId ?? string.Empty;
            InstanceId = instanceId;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            DefinitionRevision = definitionRevision;
            m_SetByCallerValues = setByCallerValues == null
                ? Array.Empty<SimulationSetByCallerValue>()
                : new List<SimulationSetByCallerValue>(setByCallerValues).ToArray();
            if (!IsValid)
                throw new ArgumentException("Gameplay Effect lifecycle ingress is incomplete.");
        }

        public SimulationGameplayEffectLifecycleOperation Operation { get; }
        public string EffectId { get; }
        public ulong InstanceId { get; }
        public SimulationGameplayEffectContext Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public uint DefinitionRevision { get; }
        public IReadOnlyList<SimulationSetByCallerValue> SetByCallerValues => m_SetByCallerValues;
        public bool IsValid
        {
            get
            {
                if (Operation == SimulationGameplayEffectLifecycleOperation.Confirmed ||
                    Operation == SimulationGameplayEffectLifecycleOperation.Rejected)
                    return Context.IsValid && Context.PredictionKey != 0;
                if (Operation == SimulationGameplayEffectLifecycleOperation.Applied ||
                    Operation == SimulationGameplayEffectLifecycleOperation.Corrected)
                    return !string.IsNullOrEmpty(EffectId) && Context.IsValid && DefinitionRevision != 0 && LifecycleRevision != 0;
                return !string.IsNullOrEmpty(EffectId) && InstanceId != 0 && LifecycleRevision != 0;
            }
        }
    }

    public readonly struct SimulationAttributeValueIngress
    {
        public SimulationAttributeValueIngress(
            string attributeId,
            Float32Scalar baseValue,
            Float32Scalar currentValue,
            ulong valueRevision,
            string causeEffectId,
            ulong causeEffectInstanceId,
            SimulationGameplayEffectContext causeContext)
        {
            AttributeId = SimulationIdentity.Require(attributeId, nameof(attributeId));
            if (valueRevision == 0)
                throw new ArgumentOutOfRangeException(nameof(valueRevision));
            BaseValue = baseValue;
            CurrentValue = currentValue;
            ValueRevision = valueRevision;
            CauseEffectId = causeEffectId ?? string.Empty;
            CauseEffectInstanceId = causeEffectInstanceId;
            CauseContext = causeContext;
        }

        public string AttributeId { get; }
        public Float32Scalar BaseValue { get; }
        public Float32Scalar CurrentValue { get; }
        public ulong ValueRevision { get; }
        public string CauseEffectId { get; }
        public ulong CauseEffectInstanceId { get; }
        public SimulationGameplayEffectContext CauseContext { get; }
        public bool IsValid => !string.IsNullOrEmpty(AttributeId) && ValueRevision != 0;
    }

    public readonly struct GameplayEffectFact
    {
        public GameplayEffectFact(
            string effectId,
            ulong instanceId,
            SimulationGameplayEffectLifecycleOperation operation,
            SimulationGameplayEffectContext context,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong lifecycleRevision,
            uint definitionRevision,
            bool instant)
        {
            EffectId = SimulationIdentity.Require(effectId, nameof(effectId));
            if (instanceId == 0 || lifecycleRevision == 0 || definitionRevision == 0 || !context.IsValid)
                throw new ArgumentException("Gameplay Effect fact identity is incomplete.");
            InstanceId = instanceId;
            Operation = operation;
            Context = context;
            StartTick = startTick;
            EndTick = endTick;
            StackCount = stackCount;
            LifecycleRevision = lifecycleRevision;
            DefinitionRevision = definitionRevision;
            Instant = instant;
        }

        public string EffectId { get; }
        public ulong InstanceId { get; }
        public SimulationGameplayEffectLifecycleOperation Operation { get; }
        public SimulationGameplayEffectContext Context { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public int StackCount { get; }
        public ulong LifecycleRevision { get; }
        public uint DefinitionRevision { get; }
        public bool Instant { get; }
        public bool IsValid => !string.IsNullOrEmpty(EffectId) && InstanceId != 0 && LifecycleRevision != 0;
    }

    public readonly struct GameplayAttributeFact
    {
        public GameplayAttributeFact(
            string attributeId,
            Float32Scalar beforeBase,
            Float32Scalar baseValue,
            Float32Scalar beforeCurrent,
            Float32Scalar currentValue,
            ulong valueRevision,
            string causeEffectId,
            ulong causeEffectInstanceId,
            SimulationGameplayEffectContext causeContext)
        {
            AttributeId = SimulationIdentity.Require(attributeId, nameof(attributeId));
            if (valueRevision == 0)
                throw new ArgumentOutOfRangeException(nameof(valueRevision));
            BeforeBase = beforeBase;
            BaseValue = baseValue;
            BeforeCurrent = beforeCurrent;
            CurrentValue = currentValue;
            ValueRevision = valueRevision;
            CauseEffectId = causeEffectId ?? string.Empty;
            CauseEffectInstanceId = causeEffectInstanceId;
            CauseContext = causeContext;
        }

        public string AttributeId { get; }
        public Float32Scalar BeforeBase { get; }
        public Float32Scalar BaseValue { get; }
        public Float32Scalar BeforeCurrent { get; }
        public Float32Scalar CurrentValue { get; }
        public ulong ValueRevision { get; }
        public string CauseEffectId { get; }
        public ulong CauseEffectInstanceId { get; }
        public SimulationGameplayEffectContext CauseContext { get; }
        public bool IsValid => !string.IsNullOrEmpty(AttributeId) && ValueRevision != 0;
    }

    public readonly struct GameplayCueFact
    {
        public GameplayCueFact(
            string cueId,
            string triggerId,
            string sourceId,
            ulong sourceInstanceId,
            SimulationGameplayEffectContext effectContext = default)
        {
            CueId = SimulationIdentity.Require(cueId, nameof(cueId));
            TriggerId = SimulationIdentity.Require(triggerId, nameof(triggerId));
            SourceId = SimulationIdentity.Require(sourceId, nameof(sourceId));
            SourceInstanceId = sourceInstanceId;
            EffectContext = effectContext;
        }

        public string CueId { get; }
        public string TriggerId { get; }
        public string SourceId { get; }
        public ulong SourceInstanceId { get; }
        public SimulationGameplayEffectContext EffectContext { get; }
        public bool IsValid => !string.IsNullOrEmpty(CueId) && !string.IsNullOrEmpty(TriggerId) && !string.IsNullOrEmpty(SourceId);
    }
}
