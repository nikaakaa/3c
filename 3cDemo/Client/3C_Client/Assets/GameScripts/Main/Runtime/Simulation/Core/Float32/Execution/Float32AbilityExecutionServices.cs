using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionServiceSet : IFloat32AbilityExecutionServices
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32ActionStateStore m_ActionStore;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32ValueRuntime m_Values;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly IReadOnlyList<IAbilityTimelinePending> m_TimelineAdvances;
        readonly IReadOnlyList<IAbilityTimelineStopPending> m_TimelineStops;

        public Float32AbilityExecutionServiceSet(
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionTarget target,
            Float32ActionStateStore actionStore,
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32ValueRuntime values,
            Float32BlackboardRuntime blackboard,
            IReadOnlyList<IAbilityTimelinePending> timelineAdvances,
            IReadOnlyList<IAbilityTimelineStopPending> timelineStops)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            Target = target;
            m_ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            m_GameplayEffects = gameplayEffects;
            m_Equipment = equipment;
            m_Values = values ?? throw new ArgumentNullException(nameof(values));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_TimelineAdvances = timelineAdvances ?? throw new ArgumentNullException(nameof(timelineAdvances));
            m_TimelineStops = timelineStops ?? throw new ArgumentNullException(nameof(timelineStops));
        }

        public Float32AbilityExecutionTarget Target { get; }
        public IReadOnlyList<IAbilityTimelinePending> TimelineAdvances => m_TimelineAdvances;
        public IReadOnlyList<IAbilityTimelineStopPending> TimelineStops => m_TimelineStops;

        public void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow)
        {
            m_Frame.Trace.Begin(diagnosticsEnabled, captureValues, captureControlFlow);
            m_ActionStore.BeginEvaluation();
            m_Values.BeginEvaluation();
            m_GameplayEffects?.BeginEvaluation();
            m_Equipment?.BeginEvaluation();
            m_Blackboard.BeginFrame();
        }

        public void EndEvaluation()
        {
            m_Equipment?.EndEvaluation();
            m_Blackboard.EndFrame();
            m_GameplayEffects?.EndEvaluation();
            m_ActionStore.EndEvaluation();
        }

    }
}
