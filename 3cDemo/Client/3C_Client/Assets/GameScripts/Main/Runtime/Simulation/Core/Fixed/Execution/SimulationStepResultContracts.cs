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
        readonly GameplayFact[] m_GameplayFacts;
        readonly PresentationCommand[] m_PresentationCommands;
        readonly SimulationTraceRecord[] m_TraceRecords;
        CharacterStateHash m_StateHash;

        SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            GameplayFact[] gameplayFacts,
            PresentationCommand[] presentationCommands,
            SimulationTraceRecord[] traceRecords)
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
            m_GameplayFacts = gameplayFacts ?? throw new ArgumentNullException(nameof(gameplayFacts));
            m_PresentationCommands = presentationCommands ?? throw new ArgumentNullException(nameof(presentationCommands));
            m_TraceRecords = traceRecords ?? throw new ArgumentNullException(nameof(traceRecords));
            ValidateHeaders(m_GameplayFacts, state.NumericProfile, actorId, tick, "Gameplay fact");
            ValidateHeaders(m_PresentationCommands, state.NumericProfile, actorId, tick, "Presentation command");
            ValidateHeaders(m_TraceRecords, state.NumericProfile, actorId, tick, "Trace record");
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
            SimulationTraceRecord[] traceRecords)
        {
            return new SimulationActorTickResult(
                actorId,
                tick,
                state,
                stateHash,
                bodySample,
                motion,
                gameplayFacts,
                presentationCommands,
                traceRecords);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public FixedCharacterRuntimeState State { get; }
        public CharacterStateHash StateHash => m_StateHash;
        public CharacterBodySample BodySample { get; }
        public CharacterMotionRequest Motion { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        public IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        public IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        static void ValidateHeaders(
            GameplayFact[] values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }

        static void ValidateHeaders(
            PresentationCommand[] values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }

        static void ValidateHeaders(
            SimulationTraceRecord[] values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }
    }
}
