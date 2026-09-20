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
        readonly IReadOnlyList<AbilityTimelineAdvancePending> m_TimelineAdvances;
        readonly IReadOnlyList<AbilityTimelineStopPending> m_TimelineStops;
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        FixedCharacterRuntimeState m_CandidateState;
        bool m_Consumed;

        internal FixedCharacterEvaluationResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState candidateState,
            IAbilityTimelineRuntime timelineRuntime,
            GameplayFact[] gameplayFacts,
            PresentationCommand[] presentationCommands,
            SimulationTraceRecord[] traceRecords,
            AbilityTimelineAdvancePending[] timelineAdvances,
            AbilityTimelineStopPending[] timelineStops)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Character evaluation result identity is incomplete.");
            m_CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
            if (candidateState.LastCompletedTick != tick.Value)
                throw new InvalidOperationException("Fixed Character evaluation result binding is invalid.");
            ActorId = actorId;
            Tick = tick;
            m_TimelineRuntime = timelineRuntime;
            m_GameplayFacts = gameplayFacts ?? throw new ArgumentNullException(nameof(gameplayFacts));
            m_PresentationCommands = presentationCommands ?? throw new ArgumentNullException(nameof(presentationCommands));
            m_TraceRecords = traceRecords ?? throw new ArgumentNullException(nameof(traceRecords));
            m_TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            m_TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        internal FixedCharacterRuntimeState CandidateState => m_CandidateState;
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;
        internal IReadOnlyList<AbilityTimelineAdvancePending> TimelineAdvances => m_TimelineAdvances;

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
            if (m_TimelineRuntime == null || m_TimelineAdvances == null)
                return;
            for (int i = 0; i < m_TimelineAdvances.Count; i++)
            {
                if (!m_TimelineAdvances[i].IsValid)
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline advance.");
                if (commit)
                    m_TimelineRuntime.Commit(m_TimelineAdvances[i]);
                else
                    m_TimelineRuntime.Discard(m_TimelineAdvances[i]);
            }
            for (int i = 0; i < m_TimelineStops.Count; i++)
            {
                if (!m_TimelineStops[i].IsValid)
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline stop.");
                if (commit)
                    m_TimelineRuntime.CommitStop(m_TimelineStops[i]);
                else
                    m_TimelineRuntime.DiscardStop(m_TimelineStops[i]);
            }
        }
    }
}
