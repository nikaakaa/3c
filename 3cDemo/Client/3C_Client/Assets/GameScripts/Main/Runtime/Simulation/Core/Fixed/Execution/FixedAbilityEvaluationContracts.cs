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
        readonly IReadOnlyList<IAbilityTimelinePending> m_TimelineAdvances;
        readonly IReadOnlyList<IAbilityTimelineStopPending> m_TimelineStops;
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        FixedCharacterRuntimeState m_CandidateState;
        bool m_Consumed;

        internal FixedCharacterEvaluationResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState candidateState,
            IAbilityTimelineRuntime timelineRuntime,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords,
            IReadOnlyList<IAbilityTimelinePending> timelineAdvances,
            IReadOnlyList<IAbilityTimelineStopPending> timelineStops)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Character evaluation result identity is incomplete.");
            m_CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
            if (candidateState.LastCompletedTick != tick.Value)
                throw new InvalidOperationException("Fixed Character evaluation result binding is invalid.");
            ActorId = actorId;
            Tick = tick;
            m_TimelineRuntime = timelineRuntime;
            m_GameplayFacts = Copy(gameplayFacts);
            m_PresentationCommands = Copy(presentationCommands);
            m_TraceRecords = Copy(traceRecords);
            m_TimelineAdvances = Copy(timelineAdvances);
            m_TimelineStops = Copy(timelineStops);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        internal FixedCharacterRuntimeState CandidateState => m_CandidateState;
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;
        internal IReadOnlyList<IAbilityTimelinePending> TimelineAdvances => m_TimelineAdvances;

        internal void Consume()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Fixed Character evaluation result has already been consumed.");
            CompleteTimelineOutputs(true);
            for (int i = 0; i < m_TimelineAdvances.Count; i++)
            {
                if (m_TimelineRuntime == null)
                    throw new InvalidOperationException("Fixed Character evaluation has Timeline advances without a Timeline runtime.");
                m_CandidateState = m_CandidateState.WithTimelineSnapshot(m_TimelineRuntime.Capture(m_TimelineAdvances[i].RuntimeHandle));
            }
            for (int i = 0; i < m_TimelineStops.Count; i++)
                m_CandidateState = m_CandidateState.WithoutTimelineSnapshot(m_TimelineStops[i].RuntimeHandle);
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
            if (m_TimelineRuntime == null || m_TimelineAdvances == null)
                return;
            for (int i = 0; i < m_TimelineAdvances.Count; i++)
            {
                if (m_TimelineAdvances[i] == null)
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline advance.");
                if (commit)
                    m_TimelineRuntime.Commit(m_TimelineAdvances[i]);
                else
                    m_TimelineRuntime.Discard(m_TimelineAdvances[i]);
            }
            for (int i = 0; i < m_TimelineStops.Count; i++)
            {
                if (m_TimelineStops[i] == null)
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline stop.");
                if (commit)
                    m_TimelineRuntime.CommitStop(m_TimelineStops[i]);
                else
                    m_TimelineRuntime.DiscardStop(m_TimelineStops[i]);
            }
        }

        static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
        {
            var result = values == null ? new List<T>(0) : new List<T>(values);
            for (int i = 0; i < result.Count; i++)
                if (result[i] == null)
                    throw new ArgumentException("Fixed Character evaluation result contains a missing output.", nameof(values));
            return result.AsReadOnly();
        }
    }
}