using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionWorkspace
    {
        public FixedAbilityExecutionWorkspace(
            FixedGameplayEffectExecutionScratch gameplayEffects,
            List<AbilityTimelineAdvancePending> timelineAdvances,
            List<AbilityTimelineStopPending> timelineStops)
        {
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
        }

        public List<GameplayFact> Facts { get; } = new List<GameplayFact>();
        public List<PresentationCommand> Presentation { get; } = new List<PresentationCommand>();
        public List<SimulationTraceRecord> Trace { get; } = new List<SimulationTraceRecord>();
        public FixedGameplayEffectExecutionScratch GameplayEffects { get; }
        public HashSet<FixedValueEvaluationKey> ValueStack { get; } = new HashSet<FixedValueEvaluationKey>();
        public List<FixedValueInputBuffer> ValueBuffers { get; } = new List<FixedValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions { get; } = new List<SimulationMotionContribution>();
        public List<AbilityTimelineLogicMotionWarp> TimelineMotionWarps { get; } =
            new List<AbilityTimelineLogicMotionWarp>();
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<AbilityTimelineAdvancePending> TimelineAdvances { get; }
        public List<AbilityTimelineStopPending> TimelineStops { get; }
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();
    }
}
