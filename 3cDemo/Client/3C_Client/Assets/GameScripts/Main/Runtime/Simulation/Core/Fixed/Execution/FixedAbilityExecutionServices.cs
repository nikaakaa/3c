using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionServiceSet : IFixedAbilityExecutionServices
    {
        readonly FixedAbilityExecutionFrame m_Frame;

        public FixedAbilityExecutionServiceSet(
            FixedAbilityExecutionFrame frame,
            FixedAbilityExecutionTarget target,
            FixedActionRuntime actions,
            FixedActionStateStore actionStore,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            Target = target;
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            GameplayEffects = gameplayEffects;
            Equipment = equipment;
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        public FixedAbilityExecutionTarget Target { get; }
        public FixedActionRuntime Actions { get; }
        public FixedActionStateStore ActionStore { get; }
        public FixedGameplayEffectOperationRuntime GameplayEffects { get; }
        public FixedEquipmentRuntime Equipment { get; }
        public FixedValueRuntime Values { get; }
        public FixedBlackboardRuntime Blackboard { get; }

        public void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow)
        {
            m_Frame.Trace.Begin(diagnosticsEnabled, captureValues, captureControlFlow);
            ActionStore.BeginEvaluation();
            Values.BeginEvaluation();
            GameplayEffects?.BeginEvaluation();
            Equipment?.BeginEvaluation();
            Blackboard.BeginFrame();
        }

        public void EndEvaluation()
        {
            Equipment?.EndEvaluation();
            Blackboard.EndFrame();
            GameplayEffects?.EndEvaluation();
            ActionStore.EndEvaluation();
        }

        public void ApplyIngress()
        {
            for (int i = 0; i < m_Frame.Ingress.Count; i++)
            {
                SimulationIngress ingress = m_Frame.Ingress[i];
                if (ingress.Header.Kind == SimulationIngressKind.ActionLifecycle)
                    Actions.ApplyIngress(ingress);
                else
                    (GameplayEffects ?? throw new InvalidOperationException(
                        "Ability execution received Gameplay Effect ingress without the declared Gameplay Effect service.")).ApplyIngress(ingress);
            }
        }

        public void AdvanceGameplayEffects() =>
            (GameplayEffects ?? throw new InvalidOperationException(
                "Ability execution attempted to advance Gameplay Effects without the declared Gameplay Effect service.")).Advance();
    }
}
