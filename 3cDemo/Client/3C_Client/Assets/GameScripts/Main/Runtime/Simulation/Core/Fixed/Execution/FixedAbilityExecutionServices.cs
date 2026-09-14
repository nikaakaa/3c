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
            FixedInputRuntime input,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard,
            FixedMotionAccumulator motion)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            Target = target;
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
        }

        public FixedAbilityExecutionTarget Target { get; }
        public FixedActionRuntime Actions { get; }
        public FixedActionStateStore ActionStore { get; }
        public FixedInputRuntime Input { get; }
        public FixedGameplayEffectOperationRuntime GameplayEffects { get; }
        public FixedEquipmentRuntime Equipment { get; }
        public FixedValueRuntime Values { get; }
        public FixedBlackboardRuntime Blackboard { get; }
        public FixedMotionAccumulator Motion { get; }

        public void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow)
        {
            m_Frame.Trace.Begin(diagnosticsEnabled, captureValues, captureControlFlow);
            ActionStore.BeginEvaluation();
            Values.BeginEvaluation();
            GameplayEffects.BeginEvaluation();
            Equipment.BeginEvaluation();
            Blackboard.BeginFrame();
        }

        public void EndEvaluation()
        {
            Equipment.EndEvaluation();
            Blackboard.EndFrame();
            GameplayEffects.EndEvaluation();
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
                    GameplayEffects.ApplyIngress(ingress);
            }
        }

        public void AdvanceGameplayEffects() => GameplayEffects.Advance();
    }
}
