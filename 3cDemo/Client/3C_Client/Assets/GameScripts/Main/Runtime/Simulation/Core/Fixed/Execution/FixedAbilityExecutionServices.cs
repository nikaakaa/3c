using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionServiceSet : IFixedAbilityExecutionServices
    {
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedActionStateStore m_ActionStore;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;

        public FixedAbilityExecutionServiceSet(
            FixedAbilityExecutionFrame frame,
            FixedAbilityExecutionTarget target,
            FixedActionStateStore actionStore,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            Target = target;
            m_ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            m_GameplayEffects = gameplayEffects;
            m_Equipment = equipment;
            m_Values = values ?? throw new ArgumentNullException(nameof(values));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        public FixedAbilityExecutionTarget Target { get; }

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
