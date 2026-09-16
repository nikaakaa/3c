using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionWorkspace
    {
        readonly Float32GameplayEffectExecutionScratch m_GameplayEffects;
        readonly Float32MotionExecutionScratch m_Motion =
            new Float32MotionExecutionScratch();
        readonly ActorExecutionWorkspace<
            GameplayFact,
            PresentationCommand,
            SimulationTraceRecord,
            TimelineSegment<Float32Scalar>,
            Float32GameplayEffectExecutionScratch,
            Float32MotionExecutionScratch> m_Shared;

        public Float32AbilityExecutionWorkspace()
            : this(new Float32GameplayEffectExecutionScratch())
        {
        }

        internal Float32AbilityExecutionWorkspace(
            Float32GameplayEffectExecutionScratch gameplayEffects)
        {
            m_GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            m_Shared = new ActorExecutionWorkspace<
                GameplayFact,
                PresentationCommand,
                SimulationTraceRecord,
                TimelineSegment<Float32Scalar>,
                Float32GameplayEffectExecutionScratch,
                Float32MotionExecutionScratch>(m_GameplayEffects, m_Motion);
        }

        public List<GameplayFact> Facts => m_Shared.Facts.Values;
        public List<PresentationCommand> Presentation => m_Shared.Presentation.Values;
        public List<SimulationTraceRecord> Trace => m_Shared.Trace.Values;
        public NestedExecutionWorkspaceBuffer<TimelineSegment<Float32Scalar>> TimelineSegments =>
            m_Shared.TimelineSegments;
        public Float32GameplayEffectExecutionScratch GameplayEffects => m_GameplayEffects;
        public HashSet<Float32ValueEvaluationKey> ValueStack { get; } =
            new HashSet<Float32ValueEvaluationKey>();
        public List<Float32ValueInputBuffer> ValueBuffers { get; } =
            new List<Float32ValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions => m_Motion.Contributions;
        public List<MotionWarpSample<Float32Scalar, Float32ActionInstanceState>> MotionWarpSamples =>
            m_Motion.WarpSamples;
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } =
            new HashSet<string>(StringComparer.Ordinal);
        public List<IAbilityTimelinePending> TimelineAdvances { get; } =
            new List<IAbilityTimelinePending>();
        public List<IAbilityTimelineStopPending> TimelineStops { get; } =
            new List<IAbilityTimelineStopPending>();
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
            TimelineStops.Clear();
            TimelineBlackboardContexts.Clear();
            m_GameplayEffects.Reset();
            m_Motion.Reset();
        }
    }
}
