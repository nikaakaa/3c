using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class Float32CharacterEvaluationResult
    {
        readonly IReadOnlyList<GameplayFact> m_GameplayFacts;
        readonly IReadOnlyList<PresentationCommand> m_PresentationCommands;
        readonly IReadOnlyList<SimulationTraceRecord> m_TraceRecords;
        readonly IReadOnlyList<AbilityTimelineAdvancePending> m_TimelineAdvances;
        readonly IReadOnlyList<AbilityTimelineStopPending> m_TimelineStops;
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        Float32CharacterRuntimeState m_CandidateState;
        bool m_Consumed;

        internal Float32CharacterEvaluationResult(
            ActorId actorId,
            SimulationTick tick,
            Float32CharacterRuntimeState candidateState,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords,
            IAbilityTimelineRuntime timelineRuntime,            IEnumerable<AbilityTimelineAdvancePending> timelineAdvances,
            IEnumerable<AbilityTimelineStopPending> timelineStops)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Float32 Character evaluation result identity is incomplete.");
            m_CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
            if (candidateState.LastCompletedTick != tick.Value)
            {
                throw new InvalidOperationException("Float32 Character evaluation result binding is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            m_GameplayFacts = Copy(gameplayFacts);
            m_PresentationCommands = Copy(presentationCommands);
            m_TraceRecords = Copy(traceRecords);
            m_TimelineRuntime = timelineRuntime;
            m_TimelineAdvances = Copy(timelineAdvances);
            m_TimelineStops = Copy(timelineStops);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        internal Float32CharacterRuntimeState CandidateState => m_CandidateState;
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        internal void Consume()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Float32 Character evaluation result has already been consumed.");
            CompleteTimelineOutputs(true);
            for (int i = 0; i < m_TimelineAdvances.Count; i++)
            {
                if (m_TimelineRuntime == null)
                    throw new InvalidOperationException("Float32 Character evaluation has Timeline advances without a Timeline runtime.");
                m_CandidateState = m_CandidateState.WithTimelineSnapshot(m_TimelineRuntime.Capture(m_TimelineAdvances[i].RuntimeHandle));
            }
            for (int i = 0; i < m_TimelineStops.Count; i++)
                m_CandidateState = m_CandidateState.WithoutTimelineSnapshot(m_TimelineStops[i].RuntimeHandle);
            m_CandidateState = m_CandidateState.WithoutUnownedTerminalTimelines();
            m_Consumed = true;
        }

        internal void DiscardUnconsumed()
        {
            if (m_Consumed)
                return;
            CompleteTimelineOutputs(false);
            m_Consumed = true;
        }

        void CompleteTimelineOutputs(bool commit)
        {
            if (m_TimelineRuntime == null)
                return;
            for (int i = 0; i < m_TimelineAdvances.Count; i++)
            {
                if (!m_TimelineAdvances[i].IsValid)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline advance.");
                if (commit)
                    m_TimelineRuntime.Commit(m_TimelineAdvances[i]);
                else
                    m_TimelineRuntime.Discard(m_TimelineAdvances[i]);
            }
            for (int i = 0; i < m_TimelineStops.Count; i++)
            {
                if (!m_TimelineStops[i].IsValid)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline stop.");
                if (commit)
                    m_TimelineRuntime.CommitStop(m_TimelineStops[i]);
                else
                    m_TimelineRuntime.DiscardStop(m_TimelineStops[i]);
            }
        }

        static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) where T : struct
        {
            var result = values == null ? new List<T>() : new List<T>(values);
            return result.AsReadOnly();
        }
    }
}
