using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    public enum SimulationInputValueKind : byte
    {
        Boolean = 1,
        Scalar = 2,
        Vector2 = 3,
        Vector3 = 4,
        Yaw = 5,
        ActionTargetSnapshot = 6
    }

    public readonly struct SimulationInputValue
    {
        SimulationInputValue(
            string inputId,
            SimulationInputValueKind kind,
            bool boolean,
            Float32Scalar scalar,
            Float32Vector2 vector2,
            Float32Vector3 vector3,
            Float32Yaw yaw,
            SimulationActionTargetSnapshot actionTargetSnapshot)
        {
            InputId = SimulationIdentity.Require(inputId, nameof(inputId));
            Kind = kind;
            Boolean = boolean;
            Scalar = scalar;
            Vector2 = vector2;
            Vector3 = vector3;
            Yaw = yaw;
            ActionTargetSnapshot = actionTargetSnapshot;
        }

        public string InputId { get; }
        public SimulationInputValueKind Kind { get; }
        public bool Boolean { get; }
        public Float32Scalar Scalar { get; }
        public Float32Vector2 Vector2 { get; }
        public Float32Vector3 Vector3 { get; }
        public Float32Yaw Yaw { get; }
        public SimulationActionTargetSnapshot ActionTargetSnapshot { get; }
        public static SimulationInputValue FromBoolean(string inputId, bool value) => new SimulationInputValue(inputId, SimulationInputValueKind.Boolean, value, default, default, default, default, default);
        public static SimulationInputValue FromScalar(string inputId, Float32Scalar value) => new SimulationInputValue(inputId, SimulationInputValueKind.Scalar, default, value, default, default, default, default);
        public static SimulationInputValue FromVector2(string inputId, Float32Vector2 value) => new SimulationInputValue(inputId, SimulationInputValueKind.Vector2, default, default, value, default, default, default);
        public static SimulationInputValue FromVector3(string inputId, Float32Vector3 value) => new SimulationInputValue(inputId, SimulationInputValueKind.Vector3, default, default, default, value, default, default);
        public static SimulationInputValue FromYaw(string inputId, Float32Yaw value) => new SimulationInputValue(inputId, SimulationInputValueKind.Yaw, default, default, default, default, value, default);
        public static SimulationInputValue FromActionTargetSnapshot(string inputId, SimulationActionTargetSnapshot value) => new SimulationInputValue(inputId, SimulationInputValueKind.ActionTargetSnapshot, default, default, default, default, default, value);
    }

    public readonly struct SimulationInputRequest
    {
        public SimulationInputRequest(string requestId, ulong sequence, ulong sourceTick, ulong expireSimulationTick, int priority)
        {
            RequestId = SimulationIdentity.Require(requestId, nameof(requestId));
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            SourceTick = sourceTick;
            ExpireSimulationTick = expireSimulationTick;
            Priority = priority;
        }

        public string RequestId { get; }
        public ulong Sequence { get; }
        public ulong SourceTick { get; }
        public ulong ExpireSimulationTick { get; }
        public int Priority { get; }
    }

    public sealed class CharacterSimulationInput
    {
        readonly SimulationInputValue[] m_Values;
        readonly SimulationInputRequest[] m_Requests;

        public CharacterSimulationInput(
            SimulationNumericProfile numericProfile,
            SimulationTickSourceIdentity tickSource,
            string inputSourceIdentity,
            ulong sequence,
            IEnumerable<SimulationInputValue> values,
            IEnumerable<SimulationInputRequest> requests)
        {
            if (!numericProfile.IsValid || sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            NumericProfile = numericProfile;
            TickSource = tickSource;
            InputSourceIdentity = SimulationIdentity.Require(inputSourceIdentity, nameof(inputSourceIdentity));
            Sequence = sequence;
            m_Values = SortValues(values);
            m_Requests = SortRequests(requests);
        }

        public SimulationNumericProfile NumericProfile { get; }
        public SimulationTickSourceIdentity TickSource { get; }
        public string InputSourceIdentity { get; }
        public ulong Sequence { get; }
        public IReadOnlyList<SimulationInputValue> Values => m_Values;
        public IReadOnlyList<SimulationInputRequest> Requests => m_Requests;

        static SimulationInputValue[] SortValues(IEnumerable<SimulationInputValue> values)
        {
            SimulationInputValue[] result = Copy(values);
            Array.Sort(result, (left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            for (int i = 1; i < result.Length; i++)
            {
                if (string.Equals(result[i - 1].InputId, result[i].InputId, StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate simulation input value '{result[i].InputId}'.", nameof(values));
            }
            return result;
        }

        static SimulationInputRequest[] SortRequests(IEnumerable<SimulationInputRequest> requests)
        {
            SimulationInputRequest[] result = Copy(requests);
            Array.Sort(result, (left, right) =>
            {
                int bySequence = left.Sequence.CompareTo(right.Sequence);
                return bySequence != 0 ? bySequence : string.CompareOrdinal(left.RequestId, right.RequestId);
            });
            for (int i = 1; i < result.Length; i++)
            {
                if (result[i - 1].Sequence == result[i].Sequence)
                    throw new ArgumentException($"Duplicate simulation input request sequence '{result[i].Sequence}'.", nameof(requests));
            }
            return result;
        }

        static T[] Copy<T>(IEnumerable<T> source)
        {
            if (source == null)
                return Array.Empty<T>();
            if (source is ICollection<T> collection)
            {
                if (collection.Count == 0)
                    return Array.Empty<T>();
                var result = new T[collection.Count];
                collection.CopyTo(result, 0);
                return result;
            }
            return new List<T>(source).ToArray();
        }
    }

    public enum SimulationIngressKind : byte
    {
        ActionLifecycle = 1,
        GameplayResult = 2,
        GameplayEffectLifecycle = 3,
        AttributeValue = 4
    }

    public enum SimulationActionPhase : byte
    {
        Startup = 0,
        Active = 1,
        Recovery = 2,
        Cancel = 3,
        Ended = 4
    }

    public enum SimulationActionState : byte
    {
        Requested = 0,
        Predicted = 1,
        Confirmed = 2,
        Rejected = 3,
        Cancelled = 4,
        Interrupted = 5,
        Aborted = 6,
        Ended = 7,
        Corrected = 8
    }

    public enum SimulationActionLifecycleTransitionType : byte
    {
        None = 0,
        Confirm = 1,
        Complete = 2,
        Cancel = 3,
        Interrupt = 4,
        Reject = 5,
        Correct = 6,
        Abort = 7
    }

    public readonly struct SimulationActionTargetSnapshot
    {
        public SimulationActionTargetSnapshot(string targetId, Float32Vector3 position, Float32Yaw yaw)
        {
            TargetId = targetId ?? string.Empty;
            Position = position;
            Yaw = yaw;
        }

        public string TargetId { get; }
        public Float32Vector3 Position { get; }
        public Float32Yaw Yaw { get; }
        public bool HasTarget => !string.IsNullOrEmpty(TargetId);
        public static SimulationActionTargetSnapshot None => new SimulationActionTargetSnapshot(string.Empty, Float32Vector3.Zero, Float32Yaw.Zero);
    }

    public static class SimulationActionTargetSnapshotCodec
    {
        const uint Magic = 0x504E5354;
        const int Version = 1;

        public static byte[] Write(SimulationActionTargetSnapshot value)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(value.TargetId);
            writer.WriteVector3(value.Position);
            writer.WriteYaw(value.Yaw);
            return writer.ToArray();
        }

        public static SimulationActionTargetSnapshot Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return SimulationActionTargetSnapshot.None;
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Action target snapshot header is invalid.");
            var value = new SimulationActionTargetSnapshot(reader.ReadString(), reader.ReadVector3(), reader.ReadYaw());
            reader.RequireComplete();
            return value;
        }
    }

    public readonly struct SimulationActionLifecycleIngress
    {
        public SimulationActionLifecycleIngress(
            ulong actionInstanceId,
            ulong predictionKey,
            ulong inputSequence,
            SimulationActionLifecycleTransitionType transitionType,
            string reason)
        {
            if (actionInstanceId == 0 && predictionKey == 0 && inputSequence == 0)
                throw new ArgumentException("Action lifecycle ingress requires an instance, prediction, or input identity.");
            if (transitionType == SimulationActionLifecycleTransitionType.None)
                throw new ArgumentOutOfRangeException(nameof(transitionType));
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            TransitionType = transitionType;
            Reason = reason ?? string.Empty;
        }

        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public SimulationActionLifecycleTransitionType TransitionType { get; }
        public string Reason { get; }
        public bool IsValid =>
            (ActionInstanceId != 0 || PredictionKey != 0 || InputSequence != 0) &&
            TransitionType != SimulationActionLifecycleTransitionType.None;
    }

    public readonly struct SimulationIngressHeader
    {
        public SimulationIngressHeader(
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            ulong sourceTick,
            ulong sequence,
            StableHash factIdentity,
            SimulationIngressKind kind)
        {
            if (!numericProfile.IsValid || !actorId.IsValid || sourceTick == 0 || sequence == 0 || !factIdentity.IsValid)
                throw new ArgumentException("Simulation ingress header is incomplete.");
            NumericProfile = numericProfile;
            ActorId = actorId;
            SourceTick = sourceTick;
            Sequence = sequence;
            FactIdentity = factIdentity;
            Kind = kind;
        }

        public SimulationNumericProfile NumericProfile { get; }
        public ActorId ActorId { get; }
        public ulong SourceTick { get; }
        public ulong Sequence { get; }
        public StableHash FactIdentity { get; }
        public SimulationIngressKind Kind { get; }
    }

    public readonly struct SimulationIngress
    {
        public SimulationIngress(SimulationIngressHeader header, SimulationActionLifecycleIngress actionLifecycle)
        {
            if (header.Kind != SimulationIngressKind.ActionLifecycle || !actionLifecycle.IsValid)
                throw new ArgumentException("Action lifecycle ingress header and payload do not match.");
            Header = header;
            ActionLifecycle = actionLifecycle;
            GameplayResult = default;
            GameplayEffectLifecycle = null;
            AttributeValue = default;
        }

        public SimulationIngress(SimulationIngressHeader header, SimulationGameplayResultIngress gameplayResult)
        {
            if (header.Kind != SimulationIngressKind.GameplayResult || !gameplayResult.IsValid)
                throw new ArgumentException("Gameplay Result ingress header and payload do not match.");
            Header = header;
            ActionLifecycle = default;
            GameplayResult = gameplayResult;
            GameplayEffectLifecycle = null;
            AttributeValue = default;
        }

        public SimulationIngress(SimulationIngressHeader header, SimulationGameplayEffectLifecycleIngress gameplayEffectLifecycle)
        {
            if (header.Kind != SimulationIngressKind.GameplayEffectLifecycle || gameplayEffectLifecycle == null || !gameplayEffectLifecycle.IsValid)
                throw new ArgumentException("Gameplay Effect lifecycle ingress header and payload do not match.");
            Header = header;
            ActionLifecycle = default;
            GameplayResult = default;
            GameplayEffectLifecycle = gameplayEffectLifecycle;
            AttributeValue = default;
        }

        public SimulationIngress(SimulationIngressHeader header, SimulationAttributeValueIngress attributeValue)
        {
            if (header.Kind != SimulationIngressKind.AttributeValue || !attributeValue.IsValid)
                throw new ArgumentException("Attribute value ingress header and payload do not match.");
            Header = header;
            ActionLifecycle = default;
            GameplayResult = default;
            GameplayEffectLifecycle = null;
            AttributeValue = attributeValue;
        }

        public SimulationIngressHeader Header { get; }
        public SimulationActionLifecycleIngress ActionLifecycle { get; }
        public SimulationGameplayResultIngress GameplayResult { get; }
        public SimulationGameplayEffectLifecycleIngress GameplayEffectLifecycle { get; }
        public SimulationAttributeValueIngress AttributeValue { get; }
    }

    public readonly struct SimulationEventHeader
    {
        public SimulationEventHeader(
            SimulationNumericProfile numericProfile,
            EventId eventId,
            ActorId actorId,
            SimulationTick tick,
            ActivationId activation,
            ulong sequence,
            string channel)
        {
            if (!numericProfile.IsValid || !eventId.IsValid || !actorId.IsValid || !tick.IsValid || !activation.IsValid || sequence == 0)
                throw new ArgumentException("Simulation event header is incomplete.");
            NumericProfile = numericProfile;
            EventId = eventId;
            ActorId = actorId;
            Tick = tick;
            Activation = activation;
            Sequence = sequence;
            Channel = SimulationIdentity.Require(channel, nameof(channel));
        }

        public SimulationNumericProfile NumericProfile { get; }
        public EventId EventId { get; }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ActivationId Activation { get; }
        public ulong Sequence { get; }
        public string Channel { get; }
    }

    public enum GameplayFactKind : byte
    {
        Action = 1,
        Effect = 2,
        Attribute = 3,
        Cue = 4,
        Motion = 5,
        State = 6,
        ActionWindow = 7
    }

    public readonly struct ActionFact
    {
        public ActionFact(
            ulong actionInstanceId,
            ulong predictionKey,
            ulong inputSequence,
            string actionId,
            SimulationActionLifecycleTransitionType transitionType,
            SimulationActionPhase phase,
            SimulationActionState state,
            string reason)
        {
            if (actionInstanceId == 0 || predictionKey == 0 || inputSequence == 0)
                throw new ArgumentException("Action fact identity is incomplete.");
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            TransitionType = transitionType;
            Phase = phase;
            State = state;
            Reason = reason ?? string.Empty;
        }

        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public string ActionId { get; }
        public SimulationActionLifecycleTransitionType TransitionType { get; }
        public SimulationActionPhase Phase { get; }
        public SimulationActionState State { get; }
        public string Reason { get; }
        public bool IsValid => ActionInstanceId != 0 && PredictionKey != 0 && InputSequence != 0 && !string.IsNullOrEmpty(ActionId);
    }

    public readonly struct ActionWindowFact
    {
        public ActionWindowFact(
            ulong actionInstanceId,
            string actionId,
            string windowId,
            string windowType,
            ulong startTick,
            ulong endTick,
            ulong digest)
        {
            if (actionInstanceId == 0 || startTick == 0 || endTick < startTick)
                throw new ArgumentException("Action Window fact identity is incomplete.");
            ActionInstanceId = actionInstanceId;
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            WindowId = SimulationIdentity.Require(windowId, nameof(windowId));
            WindowType = SimulationIdentity.Require(windowType, nameof(windowType));
            StartTick = startTick;
            EndTick = endTick;
            Digest = digest;
        }

        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public string WindowId { get; }
        public string WindowType { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public ulong Digest { get; }
        public bool IsValid => ActionInstanceId != 0 && StartTick != 0;
    }

    public readonly struct GameplayFact
    {
        public GameplayFact(SimulationEventHeader header, GameplayFactKind kind, string subjectId, string stateId, Float32Scalar scalar)
        {
            if (kind == GameplayFactKind.Action || kind == GameplayFactKind.ActionWindow ||
                kind == GameplayFactKind.Effect || kind == GameplayFactKind.Attribute || kind == GameplayFactKind.Cue)
                throw new ArgumentException($"'{kind}' facts require a typed payload.", nameof(kind));
            Header = header;
            Kind = kind;
            SubjectId = SimulationIdentity.Require(subjectId, nameof(subjectId));
            StateId = stateId ?? string.Empty;
            Scalar = scalar;
            Action = default;
            ActionWindow = default;
            Effect = default;
            Attribute = default;
            Cue = default;
        }

        public GameplayFact(SimulationEventHeader header, ActionFact action)
        {
            if (!action.IsValid)
                throw new ArgumentException("Action fact payload is invalid.", nameof(action));
            Header = header;
            Kind = GameplayFactKind.Action;
            SubjectId = action.ActionId;
            StateId = action.TransitionType == SimulationActionLifecycleTransitionType.None
                ? "Activated"
                : action.TransitionType.ToString();
            Scalar = Float32Scalar.Zero;
            Action = action;
            ActionWindow = default;
            Effect = default;
            Attribute = default;
            Cue = default;
        }

        public GameplayFact(SimulationEventHeader header, ActionWindowFact actionWindow)
        {
            if (!actionWindow.IsValid)
                throw new ArgumentException("Action Window fact payload is invalid.", nameof(actionWindow));
            Header = header;
            Kind = GameplayFactKind.ActionWindow;
            SubjectId = actionWindow.WindowId;
            StateId = actionWindow.WindowType;
            Scalar = Float32Scalar.Zero;
            Action = default;
            ActionWindow = actionWindow;
            Effect = default;
            Attribute = default;
            Cue = default;
        }

        public GameplayFact(SimulationEventHeader header, GameplayEffectFact effect)
        {
            if (!effect.IsValid)
                throw new ArgumentException("Gameplay Effect fact payload is invalid.", nameof(effect));
            Header = header;
            Kind = GameplayFactKind.Effect;
            SubjectId = effect.EffectId;
            StateId = effect.Operation.ToString();
            Scalar = Float32Scalar.Zero;
            Action = default;
            ActionWindow = default;
            Effect = effect;
            Attribute = default;
            Cue = default;
        }

        public GameplayFact(SimulationEventHeader header, GameplayAttributeFact attribute)
        {
            if (!attribute.IsValid)
                throw new ArgumentException("Gameplay Attribute fact payload is invalid.", nameof(attribute));
            Header = header;
            Kind = GameplayFactKind.Attribute;
            SubjectId = attribute.AttributeId;
            StateId = "Changed";
            Scalar = attribute.CurrentValue;
            Action = default;
            ActionWindow = default;
            Effect = default;
            Attribute = attribute;
            Cue = default;
        }

        public GameplayFact(SimulationEventHeader header, GameplayCueFact cue)
        {
            if (!cue.IsValid)
                throw new ArgumentException("Gameplay Cue fact payload is invalid.", nameof(cue));
            Header = header;
            Kind = GameplayFactKind.Cue;
            SubjectId = cue.CueId;
            StateId = cue.TriggerId;
            Scalar = Float32Scalar.Zero;
            Action = default;
            ActionWindow = default;
            Effect = default;
            Attribute = default;
            Cue = cue;
        }
        public SimulationEventHeader Header { get; }
        public GameplayFactKind Kind { get; }
        public string SubjectId { get; }
        public string StateId { get; }
        public Float32Scalar Scalar { get; }
        public ActionFact Action { get; }
        public ActionWindowFact ActionWindow { get; }
        public GameplayEffectFact Effect { get; }
        public GameplayAttributeFact Attribute { get; }
        public GameplayCueFact Cue { get; }
    }

    public enum PresentationCommandKind : byte
    {
        SelectProducer = 1,
        SampleProducer = 2,
        CompleteProducer = 3,
        ReleaseProducer = 4,
        Camera = 5,
        Cue = 6,
        Vfx = 7,
        Ui = 8
    }

    public readonly struct PresentationCommand
    {
        public PresentationCommand(
            SimulationEventHeader header,
            PresentationCommandKind kind,
            string producerId,
            Float32Scalar sampleTime,
            Float32Scalar weight,
            ulong producerGeneration = 0,
            int cycle = 0)
        {
            Header = header;
            Kind = kind;
            ProducerId = SimulationIdentity.Require(producerId, nameof(producerId));
            SampleTime = sampleTime;
            Weight = weight;
            ProducerGeneration = producerGeneration;
            Cycle = cycle;
            if (IsPlaybackCommand(kind) && producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
        }
        public SimulationEventHeader Header { get; }
        public PresentationCommandKind Kind { get; }
        public string ProducerId { get; }
        public Float32Scalar SampleTime { get; }
        public Float32Scalar Weight { get; }
        public ulong ProducerGeneration { get; }
        public int Cycle { get; }

        static bool IsPlaybackCommand(PresentationCommandKind kind)
        {
            return kind == PresentationCommandKind.SelectProducer ||
                   kind == PresentationCommandKind.SampleProducer ||
                   kind == PresentationCommandKind.CompleteProducer ||
                   kind == PresentationCommandKind.ReleaseProducer;
        }
    }

    public enum SimulationTraceSeverity : byte
    {
        Detail = 1,
        Information = 2,
        Warning = 3,
        Error = 4
    }

    public readonly struct SimulationTraceRecord
    {
        public SimulationTraceRecord(SimulationEventHeader header, SimulationTraceSeverity severity, string boundary, string code, string detail)
        {
            Header = header;
            Severity = severity;
            Boundary = SimulationIdentity.Require(boundary, nameof(boundary));
            Code = SimulationIdentity.Require(code, nameof(code));
            Detail = detail ?? string.Empty;
        }
        public SimulationEventHeader Header { get; }
        public SimulationTraceSeverity Severity { get; }
        public string Boundary { get; }
        public string Code { get; }
        public string Detail { get; }
    }
}
