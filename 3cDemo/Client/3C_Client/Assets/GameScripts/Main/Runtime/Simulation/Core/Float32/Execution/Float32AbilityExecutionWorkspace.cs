using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionWorkspace
    {
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
