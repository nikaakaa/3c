using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionWorkspace
    {
        public Float32AbilityExecutionWorkspace(
            Float32GameplayEffectExecutionScratch gameplayEffects,
            List<IAbilityTimelinePending> timelineAdvances,
            List<IAbilityTimelineStopPending> timelineStops)
        {
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
        }

        public List<GameplayFact> Facts { get; } = new List<GameplayFact>();
        public List<PresentationCommand> Presentation { get; } = new List<PresentationCommand>();
        public List<SimulationTraceRecord> Trace { get; } = new List<SimulationTraceRecord>();
        public NestedExecutionWorkspaceBuffer<TimelineSegment<Float32Scalar>> TimelineSegments { get; } =
            new NestedExecutionWorkspaceBuffer<TimelineSegment<Float32Scalar>>();
        public Float32GameplayEffectExecutionScratch GameplayEffects { get; }
        public HashSet<Float32ValueEvaluationKey> ValueStack { get; } = new HashSet<Float32ValueEvaluationKey>();
        public List<Float32ValueInputBuffer> ValueBuffers { get; } = new List<Float32ValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions { get; } = new List<SimulationMotionContribution>();
        public List<MotionWarpSample<Float32Scalar, Float32ActionInstanceState>> MotionWarpSamples { get; } =
            new List<MotionWarpSample<Float32Scalar, Float32ActionInstanceState>>();
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<IAbilityTimelinePending> TimelineAdvances { get; }
        public List<IAbilityTimelineStopPending> TimelineStops { get; }
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();
    }
}
