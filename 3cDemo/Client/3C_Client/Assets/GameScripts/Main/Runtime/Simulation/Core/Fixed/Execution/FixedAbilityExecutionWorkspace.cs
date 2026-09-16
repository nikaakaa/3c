using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionWorkspace
    {
        readonly FixedGameplayEffectExecutionScratch m_GameplayEffects;
        readonly FixedMotionExecutionScratch m_Motion =
            new FixedMotionExecutionScratch();
        readonly ActorExecutionWorkspace<
            GameplayFact,
            PresentationCommand,
            SimulationTraceRecord,
            TimelineSegment<FixedScalar>,
            FixedGameplayEffectExecutionScratch,
            FixedMotionExecutionScratch> m_Shared;

        public FixedAbilityExecutionWorkspace()
            : this(new FixedGameplayEffectExecutionScratch())
        {
        }

        internal FixedAbilityExecutionWorkspace(
            FixedGameplayEffectExecutionScratch gameplayEffects)
        {
            m_GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            m_Shared = new ActorExecutionWorkspace<
                GameplayFact,
                PresentationCommand,
                SimulationTraceRecord,
                TimelineSegment<FixedScalar>,
                FixedGameplayEffectExecutionScratch,
                FixedMotionExecutionScratch>(m_GameplayEffects, m_Motion);
        }

        public List<GameplayFact> Facts => m_Shared.Facts.Values;
        public List<PresentationCommand> Presentation => m_Shared.Presentation.Values;
        public List<SimulationTraceRecord> Trace => m_Shared.Trace.Values;
        public NestedExecutionWorkspaceBuffer<TimelineSegment<FixedScalar>> TimelineSegments =>
            m_Shared.TimelineSegments;
        public FixedGameplayEffectExecutionScratch GameplayEffects => m_GameplayEffects;
        public HashSet<FixedValueEvaluationKey> ValueStack { get; } =
            new HashSet<FixedValueEvaluationKey>();
        public List<FixedValueInputBuffer> ValueBuffers { get; } =
            new List<FixedValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions => m_Motion.Contributions;
        public List<MotionWarpSample<FixedScalar, FixedActionInstanceState>> MotionWarpSamples =>
            m_Motion.WarpSamples;
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } =
            new HashSet<string>(StringComparer.Ordinal);
        public List<IAbilityTimelinePending> TimelineAdvances { get; } =
            new List<IAbilityTimelinePending>();
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();

        internal void Reset()
        {
            Facts.Clear();
            Presentation.Clear();
            Trace.Clear();
            ValueStack.Clear();
            for (int i = 0; i < ValueBuffers.Count; i++)
                ValueBuffers[i].Clear();
            MotionContributions.Clear();
            MotionWarpSamples.Clear();
            ActionWindowProjections.Clear();
            ActionWindowProjectionKeys.Clear();
            TimelineAdvances.Clear();
            TimelineBlackboardContexts.Clear();
            m_GameplayEffects.Reset();
            m_Motion.Reset();
        }
    }
}
