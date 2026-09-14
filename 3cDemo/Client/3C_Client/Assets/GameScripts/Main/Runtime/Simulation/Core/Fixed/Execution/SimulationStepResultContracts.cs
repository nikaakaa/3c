using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct CharacterBodySample
    {
        public CharacterBodySample(
            ActorId actorId,
            SimulationTick tick,
            WorldBodyState beforeBody,
            WorldBodyState finalBody,
            FixedVector3 appliedDisplacement,
            FixedScalar appliedYawDegrees)
        {
            if (!actorId.IsValid || !tick.IsValid || beforeBody.ActorId != actorId || finalBody.ActorId != actorId)
                throw new ArgumentException("Body sample identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            BeforeBody = beforeBody;
            FinalBody = finalBody;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public WorldBodyState BeforeBody { get; }
        public WorldBodyState FinalBody { get; }
        public FixedVector3 AppliedDisplacement { get; }
        public FixedScalar AppliedYawDegrees { get; }
    }

    public sealed class SimulationActorTickResult
    {
        readonly ReadOnlyCollection<GameplayFact> m_GameplayFacts;
        readonly ReadOnlyCollection<PresentationCommand> m_PresentationCommands;
        readonly ReadOnlyCollection<SimulationTraceRecord> m_TraceRecords;
        CharacterStateHash m_StateHash;

        public SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationState state,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords)
        {
            if (!actorId.IsValid || !tick.IsValid || bodySample.ActorId != actorId || bodySample.Tick != tick)
                throw new ArgumentException("Actor Tick result identity is incomplete.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (state.LastCompletedTick != tick.Value)
                throw new ArgumentException("Actor state Tick does not match result Tick.", nameof(state));
            ActorId = actorId;
            Tick = tick;
            BodySample = bodySample;
            Motion = motion;
            m_GameplayFacts = Copy(gameplayFacts).AsReadOnly();
            m_PresentationCommands = Copy(presentationCommands).AsReadOnly();
            m_TraceRecords = Copy(traceRecords).AsReadOnly();
            ValidateHeaders(m_GameplayFacts, value => value.Header, state.NumericProfile, actorId, tick, "Gameplay fact");
            ValidateHeaders(m_PresentationCommands, value => value.Header, state.NumericProfile, actorId, tick, "Presentation command");
            ValidateHeaders(m_TraceRecords, value => value.Header, state.NumericProfile, actorId, tick, "Trace record");
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterSimulationState State { get; }
        public CharacterStateHash StateHash
        {
            get
            {
                if (!m_StateHash.IsValid)
                    m_StateHash = CharacterSimulationStateCodec.ComputeHash(State);
                return m_StateHash;
            }
        }
        public CharacterBodySample BodySample { get; }
        public CharacterMotionRequest Motion { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        public IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        public IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        static List<T> Copy<T>(IEnumerable<T> values) =>
            values == null ? new List<T>() : new List<T>(values);

        static void ValidateHeaders<T>(
            IReadOnlyList<T> values,
            Func<T, SimulationEventHeader> headerSelector,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader header = headerSelector(values[i]);
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }
    }
}
