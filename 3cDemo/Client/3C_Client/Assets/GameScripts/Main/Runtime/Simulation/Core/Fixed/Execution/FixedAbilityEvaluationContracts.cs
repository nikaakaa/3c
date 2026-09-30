using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedCharacterEvaluationResult
    {
        GameplayFact[] m_GameplayFacts;
        PresentationCommand[] m_PresentationCommands;
        SimulationTraceRecord[] m_TraceRecords;
        readonly List<AbilityTimelineAdvancePending> m_TimelineAdvances;
        readonly List<AbilityTimelineStopPending> m_TimelineStops;
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        FixedCharacterRuntimeState m_CandidateState;
        bool m_Consumed;
        bool m_OutputsCommitted;
        bool m_OutputsTaken;

        internal FixedCharacterEvaluationResult(
            ActorId actorId,
            IAbilityTimelineRuntime timelineRuntime,
            int timelineRequestCapacity)
        {
            ActorId = actorId;
            m_TimelineRuntime = timelineRuntime;
            m_TimelineAdvances = new List<AbilityTimelineAdvancePending>(timelineRequestCapacity);
            m_TimelineStops = new List<AbilityTimelineStopPending>(timelineRequestCapacity);
        }

        internal FixedCharacterEvaluationResult Reset(
            SimulationTick tick,
            FixedCharacterRuntimeState candidateState,
            GameplayFact[] gameplayFacts,
            PresentationCommand[] presentationCommands,
            SimulationTraceRecord[] traceRecords,
            List<AbilityTimelineAdvancePending> timelineAdvances,
            List<AbilityTimelineStopPending> timelineStops)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Fixed Character evaluation result identity is incomplete.");
            m_CandidateState = candidateState ?? throw new ArgumentNullException(nameof(candidateState));
            if (candidateState.LastCompletedTick != tick.Value)
                throw new InvalidOperationException("Fixed Character evaluation result binding is invalid.");
            Tick = tick;
            m_GameplayFacts = gameplayFacts ?? throw new ArgumentNullException(nameof(gameplayFacts));
            m_PresentationCommands = presentationCommands ?? throw new ArgumentNullException(nameof(presentationCommands));
            m_TraceRecords = traceRecords ?? throw new ArgumentNullException(nameof(traceRecords));
            m_TimelineAdvances.AddRange(timelineAdvances);
            m_TimelineStops.AddRange(timelineStops);
            m_Consumed = false;
            m_OutputsCommitted = false;
            m_OutputsTaken = false;
            return this;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; private set; }
        internal FixedCharacterRuntimeState CandidateState => m_CandidateState;

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
            m_OutputsCommitted = true;
            m_Consumed = true;
            m_TimelineAdvances.Clear();
            m_TimelineStops.Clear();
        }

        internal void TakeOutputs(
            out GameplayFact[] gameplayFacts,
            out PresentationCommand[] presentationCommands,
            out SimulationTraceRecord[] traceRecords)
        {
            if (!m_OutputsCommitted || m_OutputsTaken)
                throw new InvalidOperationException("Fixed Character evaluation outputs are not available for transfer.");
            gameplayFacts = m_GameplayFacts;
            presentationCommands = m_PresentationCommands;
            traceRecords = m_TraceRecords;
            m_GameplayFacts = Array.Empty<GameplayFact>();
            m_PresentationCommands = Array.Empty<PresentationCommand>();
            m_TraceRecords = Array.Empty<SimulationTraceRecord>();
            m_OutputsTaken = true;
        }

        internal void DiscardUnconsumed()
        {
            if (m_Consumed)
                return;
            CompleteTimelineOutputs(false);
            m_Consumed = true;
            m_TimelineAdvances.Clear();
            m_TimelineStops.Clear();
        }

        void CompleteTimelineOutputs(bool commit)
        {
            if (m_TimelineRuntime == null)
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
