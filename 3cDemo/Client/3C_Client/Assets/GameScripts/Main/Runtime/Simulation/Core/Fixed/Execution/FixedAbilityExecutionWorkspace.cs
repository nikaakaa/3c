using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionWorkspace
    {
        SimulationMotionContribution[] m_MotionContributions = Array.Empty<SimulationMotionContribution>();
        int m_MotionContributionCount;

        public FixedAbilityExecutionWorkspace(
            FixedGameplayEffectExecutionScratch gameplayEffects,
            FixedGameplayAbilityExecutionData data,
            GameplayAbilityExecutionLayout layout,
            List<AbilityTimelineAdvancePending> timelineAdvances,
            List<AbilityTimelineStopPending> timelineStops)
        {
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
            int depthCapacity = checked(data.Operations.Count + 1);
            int inputCapacity = 0;
            for (int index = 0; index < data.Operations.Count; index++)
                inputCapacity = Math.Max(inputCapacity, layout.ValueInputs(data.Operations[index].Handle).Length);
            ValueStack.EnsureCapacity(depthCapacity);
            while (ValueBuffers.Count < depthCapacity)
                ValueBuffers.Add(new FixedValueInputBuffer());
            for (int index = 0; index < ValueBuffers.Count; index++)
                if (ValueBuffers[index].Values.Capacity < inputCapacity)
                    ValueBuffers[index].Values.Capacity = inputCapacity;
        }

        public List<GameplayFact> Facts { get; } = new List<GameplayFact>();
        public List<PresentationCommand> Presentation { get; } = new List<PresentationCommand>();
        public List<SimulationTraceRecord> Trace { get; } = new List<SimulationTraceRecord>();
        public FixedGameplayEffectExecutionScratch GameplayEffects { get; }
        public HashSet<FixedValueEvaluationKey> ValueStack { get; } = new HashSet<FixedValueEvaluationKey>();
        public List<FixedValueInputBuffer> ValueBuffers { get; } = new List<FixedValueInputBuffer>();
        public List<AbilityTimelineLogicMotionWarp> TimelineMotionWarps { get; } =
            new List<AbilityTimelineLogicMotionWarp>();
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<SimulationActionWindowProjectionKey> ActionWindowProjectionKeys { get; } = new();
        public List<AbilityTimelineAdvancePending> TimelineAdvances { get; }
        public List<AbilityTimelineStopPending> TimelineStops { get; }
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();

        public void Reset()
        {
            Facts.Clear();
            Presentation.Clear();
            Trace.Clear();
            ValueStack.Clear();
            for (int i = 0; i < ValueBuffers.Count; i++)
                ValueBuffers[i].Clear();
            TimelineMotionWarps.Clear();
            ActionWindowProjections.Clear();
            ActionWindowProjectionKeys.Clear();
            TimelineBlackboardContexts.Clear();
            ClearMotionContributions();
        }

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

        public void CopyMotionContributionsTo(FixedMotionContributionScratch contributions)
        {
            for (int i = 0; i < m_MotionContributionCount; i++)
                contributions.Append(m_MotionContributions[i]);
        }

        public void ClearMotionContributions()
        {
            Array.Clear(m_MotionContributions, 0, m_MotionContributionCount);
            m_MotionContributionCount = 0;
        }
    }
}
