using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionWorkspace
    {
        SimulationMotionContribution[] m_MotionContributions = Array.Empty<SimulationMotionContribution>();
        int m_MotionContributionCount;

        public Float32AbilityExecutionWorkspace(
            Float32GameplayEffectExecutionScratch gameplayEffects,
            List<AbilityTimelineAdvancePending> timelineAdvances,
            List<AbilityTimelineStopPending> timelineStops,
            Float32GraphValueWorkspace values)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
        }

        public List<GameplayFact> Facts { get; } = new List<GameplayFact>();
        public List<PresentationCommand> Presentation { get; } = new List<PresentationCommand>();
        public List<SimulationTraceRecord> Trace { get; } = new List<SimulationTraceRecord>();
        public Float32GameplayEffectExecutionScratch GameplayEffects { get; }
        public Float32GraphValueWorkspace Values { get; }
        public List<AbilityTimelineLogicMotionWarp> TimelineMotionWarps { get; } =
            new List<AbilityTimelineLogicMotionWarp>();
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<AbilityTimelineAdvancePending> TimelineAdvances { get; }
        public List<AbilityTimelineStopPending> TimelineStops { get; }
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();

        public void SubmitMotionContribution(in SimulationMotionContribution contribution)
        {
            if (m_MotionContributionCount == m_MotionContributions.Length)
            {
                int capacity = Math.Max(4, m_MotionContributions.Length * 2);
                var values = new SimulationMotionContribution[capacity];
                Array.Copy(m_MotionContributions, values, m_MotionContributionCount);
                m_MotionContributions = values;
            }

            m_MotionContributions[m_MotionContributionCount++] = contribution;
        }

        public void CopyMotionContributionsTo(List<SimulationMotionContribution> contributions)
        {
            for (int i = 0; i < m_MotionContributionCount; i++)
                contributions.Add(m_MotionContributions[i]);
        }

        public void ClearMotionContributions()
        {
            Array.Clear(m_MotionContributions, 0, m_MotionContributionCount);
            m_MotionContributionCount = 0;
        }
    }
}
