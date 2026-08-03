using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    public static class ServerAuthoritativeProducts
    {
        const string Owner = "thirdperson.network-model.server-authoritative-hybrid";

        static readonly SimulationPipelineProductContract s_OwnerCanonicalInputBatch = Exclusive(
            "server-authoritative.owner-canonical-input-batch",
            "server-authoritative-owner-input/1",
            "actor/source-tick/input-sequence/input",
            SimulationPipelinePhaseMask.Ingress,
            SimulationPipelinePhaseMask.Schedule | SimulationPipelinePhaseMask.Egress);

        static readonly SimulationPipelineProductContract s_AuthoritativeObservationBatch = Exclusive(
            "server-authoritative.authoritative-observation-batch",
            "server-authoritative-observation/1",
            "receive-sequence/baseline/remote/event",
            SimulationPipelinePhaseMask.Ingress,
            SimulationPipelinePhaseMask.Schedule | SimulationPipelinePhaseMask.Egress);

        static readonly SimulationPipelineProductContract s_AuthoritativeActorBaseline = Exclusive(
            "server-authoritative.actor-baseline",
            "server-authoritative-baseline/1",
            "actor/tick/program/layout/state/body/ack/horizon",
            SimulationPipelinePhaseMask.Egress,
            SimulationPipelinePhaseMask.Egress);

        static readonly SimulationPipelineProductContract s_PredictionCorrectionDecision = Exclusive(
            "server-authoritative.prediction-correction-decision",
            "server-authoritative-correction-decision/1",
            "kind/reason/baseline/restore/replay/error",
            SimulationPipelinePhaseMask.Schedule,
            SimulationPipelinePhaseMask.Egress,
            SimulationPipelineProductConsumption.BackendTerminal);

        static readonly SimulationPipelineProductContract s_AcceptedAuthorityInputBatch = Exclusive(
            "server-authoritative.accepted-authority-input-batch",
            "server-authoritative-accepted-input/1",
            "authority-tick/actor/input-sequence/input",
            SimulationPipelinePhaseMask.Ingress,
            SimulationPipelinePhaseMask.Schedule | SimulationPipelinePhaseMask.Egress);

        static readonly SimulationPipelineProductContract s_AuthorityReplicationBatch = Exclusive(
            "server-authoritative.authority-replication-batch",
            "server-authoritative-replication/1",
            "authority-tick/baseline/body/reliable-event",
            SimulationPipelinePhaseMask.Egress,
            SimulationPipelinePhaseMask.Egress,
            SimulationPipelineProductConsumption.CommitTerminal);

        static readonly SimulationPipelineProductContract s_RemotePresentationBatch = Exclusive(
            "server-authoritative.remote-presentation-batch",
            "server-authoritative-remote-presentation/1",
            "actor/body/producer/fact/event",
            SimulationPipelinePhaseMask.Ingress,
            SimulationPipelinePhaseMask.Egress,
            SimulationPipelineProductConsumption.CommitTerminal);

        static readonly SimulationPipelineProductContract s_SelectedRemoteBodyBatch = Exclusive(
            "server-authoritative.selected-remote-body-batch",
            "server-authoritative-selected-remote-body/1",
            "actor/current-step/body/reset",
            SimulationPipelinePhaseMask.Schedule,
            SimulationPipelinePhaseMask.Egress);

        static readonly ReadOnlyCollection<SimulationPipelineProductContract> s_All =
            new List<SimulationPipelineProductContract>
            {
                s_OwnerCanonicalInputBatch,
                s_AuthoritativeObservationBatch,
                s_AuthoritativeActorBaseline,
                s_PredictionCorrectionDecision,
                s_AcceptedAuthorityInputBatch,
                s_AuthorityReplicationBatch,
                s_RemotePresentationBatch,
                s_SelectedRemoteBodyBatch
            }.AsReadOnly();

        public static SimulationPipelineProductContract OwnerCanonicalInputBatch => s_OwnerCanonicalInputBatch;
        public static SimulationPipelineProductContract AuthoritativeObservationBatch => s_AuthoritativeObservationBatch;
        public static SimulationPipelineProductContract AuthoritativeActorBaseline => s_AuthoritativeActorBaseline;
        public static SimulationPipelineProductContract PredictionCorrectionDecision => s_PredictionCorrectionDecision;
        public static SimulationPipelineProductContract AcceptedAuthorityInputBatch => s_AcceptedAuthorityInputBatch;
        public static SimulationPipelineProductContract AuthorityReplicationBatch => s_AuthorityReplicationBatch;
        public static SimulationPipelineProductContract RemotePresentationBatch => s_RemotePresentationBatch;
        public static SimulationPipelineProductContract SelectedRemoteBodyBatch => s_SelectedRemoteBodyBatch;
        public static IReadOnlyList<SimulationPipelineProductContract> All => s_All;

        static SimulationPipelineProductContract Exclusive(
            string id,
            string canonicalIdentity,
            string diagnosticsShape,
            SimulationPipelinePhaseMask producerPhases,
            SimulationPipelinePhaseMask consumerPhases,
            SimulationPipelineProductConsumption consumption = SimulationPipelineProductConsumption.InternalRequired)
        {
            return new SimulationPipelineProductContract(
                new SimulationPipelineProductId(id),
                new SimulationPipelineProductSchemaVersion(1),
                Owner,
                SimulationPipelineProductMultiplicity.Exclusive,
                canonicalIdentity,
                diagnosticsShape,
                producerPhases,
                consumerPhases,
                consumption);
        }
    }

    public sealed class OwnerCanonicalInputBatch
    {
        public OwnerCanonicalInputBatch(ActorId actorId, ulong sourceTick, ulong inputSequence, CharacterSimulationInput input)
        {
            if (!actorId.IsValid || sourceTick == 0 || inputSequence == 0)
                throw new ArgumentException("Owner canonical input identity is incomplete.");
            Input = input ?? throw new ArgumentNullException(nameof(input));
            if (input.Sequence != inputSequence || input.TickSource.SourceTick != sourceTick)
                throw new ArgumentException("Owner canonical input metadata does not match the canonical input.", nameof(input));
            ActorId = actorId;
            SourceTick = sourceTick;
            InputSequence = inputSequence;
        }

        public ActorId ActorId { get; }
        public ulong SourceTick { get; }
        public ulong InputSequence { get; }
        public CharacterSimulationInput Input { get; }
    }

    public readonly struct ServerAuthoritativeEventHorizon
    {
        public ServerAuthoritativeEventHorizon(ulong sequence, EventId eventId)
        {
            if ((sequence == 0) != !eventId.IsValid)
                throw new ArgumentException("Confirmed EventId horizon is incomplete.");
            Sequence = sequence;
            EventId = eventId;
        }

        public ulong Sequence { get; }
        public EventId EventId { get; }
        public bool IsEmpty => Sequence == 0;
        public static ServerAuthoritativeEventHorizon Empty => default;
    }

    public sealed class AuthoritativeActorBaseline
    {
        readonly byte[] m_CharacterStateBytes;

        public AuthoritativeActorBaseline(
            ActorId actorId,
            SimulationTick authorityTick,
            SimulationNumericProfile numericProfile,
            TargetAbiVersion targetAbiVersion,
            string stateCodecIdentity,
            ProgramHash programHash,
            LayoutHash layoutHash,
            OperationSetVersion operationSetVersion,
            byte[] characterStateBytes,
            CharacterStateHash stateHash,
            WorldRevision worldRevision,
            SolverImplementationId solverId,
            string solverVersion,
            WorldCapability solverCapabilities,
            WorldBodyState body,
            ulong confirmedInputSequence,
            ServerAuthoritativeEventHorizon confirmedEventHorizon)
        {
            if (!actorId.IsValid || !authorityTick.IsValid || !numericProfile.IsValid ||
                !targetAbiVersion.Equals(numericProfile.AbiVersion) ||
                !string.Equals(stateCodecIdentity, CharacterSimulationStateCodec.CodecIdentity, StringComparison.Ordinal) ||
                !programHash.IsValid || !layoutHash.IsValid ||
                !operationSetVersion.IsValid || !stateHash.IsValid || string.IsNullOrEmpty(worldRevision.Value) ||
                string.IsNullOrEmpty(solverId.Value) || solverCapabilities == WorldCapability.None || body.ActorId != actorId)
            {
                throw new ArgumentException("Authoritative baseline identity is incomplete.");
            }
            if (characterStateBytes == null || characterStateBytes.Length == 0)
                throw new ArgumentException("Authoritative baseline requires full Character state bytes.", nameof(characterStateBytes));
            ActorId = actorId;
            AuthorityTick = authorityTick;
            NumericProfile = numericProfile;
            TargetAbiVersion = targetAbiVersion;
            StateCodecIdentity = stateCodecIdentity;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            OperationSetVersion = operationSetVersion;
            m_CharacterStateBytes = (byte[])characterStateBytes.Clone();
            StateHash = stateHash;
            WorldRevision = worldRevision;
            SolverId = solverId;
            SolverVersion = string.IsNullOrWhiteSpace(solverVersion)
                ? throw new ArgumentException("Solver version is required.", nameof(solverVersion))
                : solverVersion.Trim();
            SolverCapabilities = solverCapabilities;
            Body = body;
            BodyHash = ServerAuthoritativeCanonicalCodec.ComputeBodyHash(body);
            ConfirmedInputSequence = confirmedInputSequence;
            ConfirmedEventHorizon = confirmedEventHorizon;
        }

        public ActorId ActorId { get; }
        public SimulationTick AuthorityTick { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public TargetAbiVersion TargetAbiVersion { get; }
        public string StateCodecIdentity { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public ReadOnlyMemory<byte> CharacterStateBytes => m_CharacterStateBytes;
        public CharacterStateHash StateHash { get; }
        public WorldRevision WorldRevision { get; }
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldCapability SolverCapabilities { get; }
        public WorldBodyState Body { get; }
        public StableHash BodyHash { get; }
        public ulong ConfirmedInputSequence { get; }
        public ServerAuthoritativeEventHorizon ConfirmedEventHorizon { get; }
        public byte[] CopyCharacterStateBytes() => (byte[])m_CharacterStateBytes.Clone();
    }

    public enum PredictionCorrectionDecisionKind : byte
    {
        NoCorrection = 1,
        RestoreReplay = 2,
        HardRecovery = 3
    }

    public enum PredictionCorrectionReason : byte
    {
        StateAndBodyMatch = 1,
        CharacterStateMismatch = 2,
        BodyPositionMismatch = 3,
        BodyYawMismatch = 4,
        HistoryUnavailable = 5,
        ReplayLimitExceeded = 6,
        NoAuthoritativeBaseline = 7
    }

    public sealed class PredictionCorrectionDecision
    {
        public PredictionCorrectionDecision(
            PredictionCorrectionDecisionKind kind,
            PredictionCorrectionReason reason,
            SimulationTick baselineTick,
            SimulationTick restoreTick,
            SimulationTick replayStart,
            SimulationTick replayEnd,
            Float32Scalar positionError,
            Float32Scalar yawError)
        {
            if (!Enum.IsDefined(typeof(PredictionCorrectionDecisionKind), kind) ||
                !Enum.IsDefined(typeof(PredictionCorrectionReason), reason) || !baselineTick.IsValid)
            {
                throw new ArgumentException("Prediction correction decision is incomplete.");
            }
            bool restores = kind != PredictionCorrectionDecisionKind.NoCorrection;
            if (restores != restoreTick.IsValid)
                throw new ArgumentException("Prediction correction restore identity does not match its kind.");
            if (replayStart.IsValid != replayEnd.IsValid ||
                replayStart.IsValid && replayEnd.CompareTo(replayStart) < 0)
            {
                throw new ArgumentException("Prediction correction replay range is invalid.");
            }
            Kind = kind;
            Reason = reason;
            BaselineTick = baselineTick;
            RestoreTick = restoreTick;
            ReplayStart = replayStart;
            ReplayEnd = replayEnd;
            PositionError = positionError;
            YawError = yawError;
        }

        public PredictionCorrectionDecisionKind Kind { get; }
        public PredictionCorrectionReason Reason { get; }
        public SimulationTick BaselineTick { get; }
        public SimulationTick RestoreTick { get; }
        public SimulationTick ReplayStart { get; }
        public SimulationTick ReplayEnd { get; }
        public Float32Scalar PositionError { get; }
        public Float32Scalar YawError { get; }
    }

    public sealed class AcceptedAuthorityInput
    {
        public AcceptedAuthorityInput(ActorId actorId, ulong inputSequence, CharacterSimulationInput input)
        {
            if (!actorId.IsValid || inputSequence == 0)
                throw new ArgumentException("Accepted authority input identity is incomplete.");
            Input = input ?? throw new ArgumentNullException(nameof(input));
            if (input.Sequence != inputSequence)
                throw new ArgumentException("Accepted authority input sequence does not match its canonical input.", nameof(input));
            ActorId = actorId;
            InputSequence = inputSequence;
        }

        public ActorId ActorId { get; }
        public ulong InputSequence { get; }
        public CharacterSimulationInput Input { get; }
    }

    public sealed class AcceptedAuthorityInputBatch
    {
        readonly ReadOnlyCollection<AcceptedAuthorityInput> m_Inputs;

        public AcceptedAuthorityInputBatch(SimulationTick authorityTick, IEnumerable<AcceptedAuthorityInput> inputs)
        {
            if (!authorityTick.IsValid)
                throw new ArgumentException("Authority input Tick is invalid.", nameof(authorityTick));
            AuthorityTick = authorityTick;
            m_Inputs = ServerAuthoritativeProductOrder.FreezeByActor(inputs, value => value.ActorId, nameof(inputs));
            if (m_Inputs.Count == 0)
                throw new ArgumentException("Authority input batch cannot be empty.", nameof(inputs));
        }

        public SimulationTick AuthorityTick { get; }
        public IReadOnlyList<AcceptedAuthorityInput> Inputs => m_Inputs;
    }

    public readonly struct ServerAuthoritativeReliableEvent
    {
        public ServerAuthoritativeReliableEvent(GameplayFact fact)
        {
            GameplayFact = fact;
            PresentationCommand = default;
            IsGameplay = true;
        }

        public ServerAuthoritativeReliableEvent(PresentationCommand command)
        {
            GameplayFact = default;
            PresentationCommand = command;
            IsGameplay = false;
        }

        public GameplayFact GameplayFact { get; }
        public PresentationCommand PresentationCommand { get; }
        public bool IsGameplay { get; }
        public SimulationEventHeader Header => IsGameplay ? GameplayFact.Header : PresentationCommand.Header;
    }

    public sealed class RemotePresentationBatch
    {
        readonly ReadOnlyCollection<CharacterBodySample> m_BodySamples;
        readonly ReadOnlyCollection<PresentationCommand> m_SampleCommands;
        readonly ReadOnlyCollection<ServerAuthoritativeReliableEvent> m_ReliableEvents;

        public RemotePresentationBatch(
            ActorId actorId,
            IEnumerable<CharacterBodySample> bodySamples,
            IEnumerable<PresentationCommand> sampleCommands,
            IEnumerable<ServerAuthoritativeReliableEvent> reliableEvents,
            bool resetBodyStream)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Remote presentation ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            ResetBodyStream = resetBodyStream;
            var bodies = bodySamples == null ? new List<CharacterBodySample>() : new List<CharacterBodySample>(bodySamples);
            bodies.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].ActorId != actorId || i > 0 && bodies[i - 1].Tick == bodies[i].Tick)
                    throw new ArgumentException("Remote body stream contains an invalid Actor or duplicate Tick.", nameof(bodySamples));
            }
            m_BodySamples = bodies.AsReadOnly();
            var samples = sampleCommands == null ? new List<PresentationCommand>() : new List<PresentationCommand>(sampleCommands);
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Header.ActorId != actorId || samples[i].Kind != PresentationCommandKind.SampleProducer)
                    throw new ArgumentException("Remote presentation sample stream contains an invalid Actor or command kind.", nameof(sampleCommands));
            }
            samples.Sort(ServerAuthoritativeProductOrder.CompareCommands);
            var latestSamples = new List<PresentationCommand>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                PresentationCommand sample = samples[i];
                int lastIndex = latestSamples.Count - 1;
                if (lastIndex >= 0 &&
                    string.Equals(latestSamples[lastIndex].ProducerId, sample.ProducerId, StringComparison.Ordinal) &&
                    latestSamples[lastIndex].ProducerGeneration == sample.ProducerGeneration)
                {
                    if (latestSamples[lastIndex].SourceActionInstanceId != sample.SourceActionInstanceId)
                        throw new ArgumentException(
                            "Remote presentation playback changed its source Action instance.",
                            nameof(sampleCommands));
                    latestSamples[lastIndex] = sample;
                }
                else
                {
                    latestSamples.Add(sample);
                }
            }
            m_SampleCommands = latestSamples.AsReadOnly();
            var reliable = reliableEvents == null
                ? new List<ServerAuthoritativeReliableEvent>()
                : new List<ServerAuthoritativeReliableEvent>(reliableEvents);
            reliable.Sort(ServerAuthoritativeProductOrder.CompareEvents);
            for (int i = 0; i < reliable.Count; i++)
            {
                if (reliable[i].Header.ActorId != actorId || i > 0 && reliable[i - 1].Header.EventId.Equals(reliable[i].Header.EventId))
                    throw new ArgumentException("Remote reliable events contain an invalid Actor or duplicate EventId.", nameof(reliableEvents));
            }
            m_ReliableEvents = reliable.AsReadOnly();
        }

        public ActorId ActorId { get; }
        public bool ResetBodyStream { get; }
        public IReadOnlyList<CharacterBodySample> BodySamples => m_BodySamples;
        public IReadOnlyList<PresentationCommand> SampleCommands => m_SampleCommands;
        public IReadOnlyList<ServerAuthoritativeReliableEvent> ReliableEvents => m_ReliableEvents;
    }

    public sealed class SelectedRemoteBodyBatch
    {
        readonly ReadOnlyCollection<CharacterBodySample> m_BodySamples;

        public SelectedRemoteBodyBatch(
            ActorId actorId,
            SimulationTick tick,
            IEnumerable<CharacterBodySample> bodySamples,
            bool resetStream)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Selected remote body ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            Tick = tick;
            ResetStream = resetStream;
            var values = bodySamples == null
                ? throw new ArgumentNullException(nameof(bodySamples))
                : new List<CharacterBodySample>(bodySamples);
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].ActorId != actorId || i > 0 && values[i - 1].Tick == values[i].Tick)
                    throw new ArgumentException("Selected remote body stream is invalid.", nameof(bodySamples));
            }
            m_BodySamples = values.AsReadOnly();
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public IReadOnlyList<CharacterBodySample> BodySamples => m_BodySamples;
        public bool ResetStream { get; }
    }

    public sealed class AuthoritativeInputAck
    {
        public AuthoritativeInputAck(
            ActorId actorId,
            SimulationTick authorityTick,
            ulong confirmedInputSequence,
            ServerAuthoritativeEventHorizon confirmedEventHorizon)
        {
            if (!actorId.IsValid || !authorityTick.IsValid)
                throw new ArgumentException("Authority input ack identity is incomplete.");
            ActorId = actorId;
            AuthorityTick = authorityTick;
            ConfirmedInputSequence = confirmedInputSequence;
            ConfirmedEventHorizon = confirmedEventHorizon;
        }

        public ActorId ActorId { get; }
        public SimulationTick AuthorityTick { get; }
        public ulong ConfirmedInputSequence { get; }
        public ServerAuthoritativeEventHorizon ConfirmedEventHorizon { get; }
    }

    public sealed class AuthoritativeObservationBatch
    {
        readonly ReadOnlyCollection<AuthoritativeActorBaseline> m_Baselines;
        readonly ReadOnlyCollection<RemotePresentationBatch> m_RemotePresentation;

        public AuthoritativeObservationBatch(
            ulong receiveSequence,
            ulong authorityTickEstimate,
            AuthoritativeInputAck ownerAck,
            IEnumerable<AuthoritativeActorBaseline> baselines,
            IEnumerable<RemotePresentationBatch> remotePresentation)
        {
            if (receiveSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(receiveSequence));
            ReceiveSequence = receiveSequence;
            AuthorityTickEstimate = authorityTickEstimate;
            OwnerAck = ownerAck;
            m_Baselines = ServerAuthoritativeProductOrder.FreezeByActor(baselines, value => value.ActorId, nameof(baselines));
            m_RemotePresentation = ServerAuthoritativeProductOrder.FreezeByActor(remotePresentation, value => value.ActorId, nameof(remotePresentation));
        }

        public ulong ReceiveSequence { get; }
        public ulong AuthorityTickEstimate { get; }
        public AuthoritativeInputAck OwnerAck { get; }
        public IReadOnlyList<AuthoritativeActorBaseline> Baselines => m_Baselines;
        public IReadOnlyList<RemotePresentationBatch> RemotePresentation => m_RemotePresentation;
    }

    public sealed class AuthorityReplicationBatch
    {
        readonly ReadOnlyCollection<AuthoritativeInputAck> m_Acks;
        readonly ReadOnlyCollection<AuthoritativeActorBaseline> m_Baselines;
        readonly ReadOnlyCollection<RemotePresentationBatch> m_RemotePresentation;

        public AuthorityReplicationBatch(
            SimulationTick authorityTick,
            IEnumerable<AuthoritativeInputAck> acks,
            IEnumerable<AuthoritativeActorBaseline> baselines,
            IEnumerable<RemotePresentationBatch> remotePresentation)
        {
            if (!authorityTick.IsValid)
                throw new ArgumentException("Authority replication Tick is invalid.", nameof(authorityTick));
            AuthorityTick = authorityTick;
            m_Acks = ServerAuthoritativeProductOrder.FreezeByActor(acks, value => value.ActorId, nameof(acks));
            m_Baselines = ServerAuthoritativeProductOrder.FreezeByActor(baselines, value => value.ActorId, nameof(baselines));
            m_RemotePresentation = ServerAuthoritativeProductOrder.FreezeByActor(remotePresentation, value => value.ActorId, nameof(remotePresentation));
            if (m_Acks.Count == 0 || m_RemotePresentation.Count == 0)
                throw new ArgumentException("Authority replication requires Actor acks and presentation streams.", nameof(acks));
            for (int i = 0; i < m_Acks.Count; i++)
            {
                if (m_Acks[i].AuthorityTick != authorityTick)
                    throw new ArgumentException("Authority replication ack Tick does not match the batch.", nameof(acks));
            }
            for (int i = 0; i < m_Baselines.Count; i++)
            {
                if (m_Baselines[i].AuthorityTick != authorityTick)
                    throw new ArgumentException("Authority replication baseline Tick does not match the batch.", nameof(baselines));
            }
        }

        public SimulationTick AuthorityTick { get; }
        public IReadOnlyList<AuthoritativeInputAck> Acks => m_Acks;
        public IReadOnlyList<AuthoritativeActorBaseline> Baselines => m_Baselines;
        public IReadOnlyList<RemotePresentationBatch> RemotePresentation => m_RemotePresentation;
    }

    static class ServerAuthoritativeProductOrder
    {
        public static ReadOnlyCollection<T> FreezeByActor<T>(
            IEnumerable<T> source,
            Func<T, ActorId> actor,
            string parameter) where T : class
        {
            var values = source == null ? new List<T>() : new List<T>(source);
            values.Sort((left, right) => actor(left).CompareTo(actor(right)));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || !actor(values[i]).IsValid || i > 0 && actor(values[i - 1]) == actor(values[i]))
                    throw new ArgumentException("Actor-scoped product contains a missing or duplicate ActorId.", parameter);
            }
            return values.AsReadOnly();
        }

        public static int CompareEvents(ServerAuthoritativeReliableEvent left, ServerAuthoritativeReliableEvent right)
        {
            int tick = left.Header.Tick.CompareTo(right.Header.Tick);
            if (tick != 0)
                return tick;
            int sequence = left.Header.Sequence.CompareTo(right.Header.Sequence);
            return sequence != 0 ? sequence : left.Header.EventId.CompareTo(right.Header.EventId);
        }

        public static int CompareCommands(PresentationCommand left, PresentationCommand right)
        {
            int producer = string.Compare(left.ProducerId, right.ProducerId, StringComparison.Ordinal);
            if (producer != 0)
                return producer;
            int generation = left.ProducerGeneration.CompareTo(right.ProducerGeneration);
            if (generation != 0)
                return generation;
            return left.Header.Sequence.CompareTo(right.Header.Sequence);
        }
    }
}
