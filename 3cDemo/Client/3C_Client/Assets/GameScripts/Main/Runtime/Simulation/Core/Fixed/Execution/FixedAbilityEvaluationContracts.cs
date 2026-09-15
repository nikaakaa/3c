using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedCharacterEvaluationResult
    {
        readonly IReadOnlyList<GameplayFact> m_GameplayFacts;
        readonly IReadOnlyList<PresentationCommand> m_PresentationCommands;
        readonly IReadOnlyList<SimulationTraceRecord> m_TraceRecords;
        bool m_Consumed;

        internal FixedCharacterEvaluationResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState candidateState,
            CharacterWorldSolveRequest worldRequest,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords,
            bool diagnosticsEnabled)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Character evaluation result identity is incomplete.");
            CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            if (worldRequest.ActorId != actorId || worldRequest.Tick != tick ||
                candidateState.LastCompletedTick != tick.Value)
            {
                throw new InvalidOperationException("Fixed Character evaluation result binding is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            m_GameplayFacts = Copy(gameplayFacts);
            m_PresentationCommands = Copy(presentationCommands);
            m_TraceRecords = Copy(traceRecords);
            DiagnosticsEnabled = diagnosticsEnabled;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterWorldSolveRequest WorldRequest { get; }
        public bool DiagnosticsEnabled { get; }
        internal FixedCharacterRuntimeState CandidateState { get; }
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        internal void Consume()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Fixed Character evaluation result has already been consumed.");
            m_Consumed = true;
        }

        internal void AbortUnconsumed()
        {
            if (m_Consumed)
                return;
            m_Consumed = true;
        }

        static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
        {
            var result = values == null ? new List<T>() : new List<T>(values);
            for (int i = 0; i < result.Count; i++)
                if (result[i] == null)
                    throw new ArgumentException("Fixed Character evaluation result contains a missing output.", nameof(values));
            return result.AsReadOnly();
        }
    }
}
