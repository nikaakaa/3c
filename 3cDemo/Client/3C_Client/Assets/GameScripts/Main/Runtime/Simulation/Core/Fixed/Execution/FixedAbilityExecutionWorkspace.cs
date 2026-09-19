using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionWorkspace
    {
        public FixedAbilityExecutionWorkspace(
            FixedGameplayEffectExecutionScratch gameplayEffects,
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
        public NestedExecutionWorkspaceBuffer<TimelineSegment<FixedScalar>> TimelineSegments { get; } =
            new NestedExecutionWorkspaceBuffer<TimelineSegment<FixedScalar>>();
        public FixedGameplayEffectExecutionScratch GameplayEffects { get; }
        public HashSet<FixedValueEvaluationKey> ValueStack { get; } = new HashSet<FixedValueEvaluationKey>();
        public List<FixedValueInputBuffer> ValueBuffers { get; } = new List<FixedValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions { get; } = new List<SimulationMotionContribution>();
        public List<MotionWarpSample<FixedScalar, FixedActionInstanceState>> MotionWarpSamples { get; } =
            new List<MotionWarpSample<FixedScalar, FixedActionInstanceState>>();
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<IAbilityTimelinePending> TimelineAdvances { get; }
        public List<IAbilityTimelineStopPending> TimelineStops { get; }
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();
    }
}
