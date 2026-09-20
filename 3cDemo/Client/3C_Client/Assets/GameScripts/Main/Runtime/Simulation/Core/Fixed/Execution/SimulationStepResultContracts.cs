using System;
using System.Collections.Generic;
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
        readonly IReadOnlyList<GameplayFact> m_GameplayFacts;
        readonly IReadOnlyList<PresentationCommand> m_PresentationCommands;
        readonly IReadOnlyList<SimulationTraceRecord> m_TraceRecords;
        CharacterStateHash m_StateHash;

        public SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            IReadOnlyList<GameplayFact> gameplayFacts,
            IReadOnlyList<PresentationCommand> presentationCommands,
            IReadOnlyList<SimulationTraceRecord> traceRecords)
            : this(
                actorId,
                tick,
                state,
                stateHash,
                bodySample,
                motion,
                Copy(gameplayFacts),
                Copy(presentationCommands),
                Copy(traceRecords),
                true)
        {
        }

        SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            GameplayFact[] gameplayFacts,
            PresentationCommand[] presentationCommands,
            SimulationTraceRecord[] traceRecords,
            bool _)
        {
            if (!actorId.IsValid || !tick.IsValid || bodySample.ActorId != actorId || bodySample.Tick != tick)
                throw new ArgumentException("Actor Tick result identity is incomplete.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (!stateHash.IsValid)
                throw new ArgumentException("Fixed Character runtime state hash is invalid.", nameof(stateHash));
            m_StateHash = stateHash;
            if (state.LastCompletedTick != tick.Value)
                throw new ArgumentException("Actor state Tick does not match result Tick.", nameof(state));
            ActorId = actorId;
            Tick = tick;
            BodySample = bodySample;
            Motion = motion;
            m_GameplayFacts = gameplayFacts;
            m_PresentationCommands = presentationCommands;
            m_TraceRecords = traceRecords;
            ValidateHeaders(m_GameplayFacts, value => value.Header, state.NumericProfile, actorId, tick, "Gameplay fact");
            ValidateHeaders(m_PresentationCommands, value => value.Header, state.NumericProfile, actorId, tick, "Presentation command");
            ValidateHeaders(m_TraceRecords, value => value.Header, state.NumericProfile, actorId, tick, "Trace record");
        }

        internal static SimulationActorTickResult FromOwnedOutputs(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            GameplayFact[] gameplayFacts,
            PresentationCommand[] presentationCommands,
            SimulationTraceRecord[] traceRecords) =>
            new SimulationActorTickResult(
                actorId,
                tick,
                state,
                stateHash,
                bodySample,
                motion,
                gameplayFacts ?? throw new ArgumentNullException(nameof(gameplayFacts)),
                presentationCommands ?? throw new ArgumentNullException(nameof(presentationCommands)),
                traceRecords ?? throw new ArgumentNullException(nameof(traceRecords)),
                true);

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public FixedCharacterRuntimeState State { get; }
        public CharacterStateHash StateHash => m_StateHash;
        public CharacterBodySample BodySample { get; }
        public CharacterMotionRequest Motion { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        public IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        public IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        static T[] Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<T>();
            var result = new T[values.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = values[i];
            return result;
        }

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
