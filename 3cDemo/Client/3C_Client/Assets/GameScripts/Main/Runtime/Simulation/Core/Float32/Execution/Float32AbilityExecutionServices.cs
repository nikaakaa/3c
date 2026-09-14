using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionServiceSet : IFloat32AbilityExecutionServices
    {
        readonly Float32AbilityExecutionFrame m_Frame;

        public Float32AbilityExecutionServiceSet(
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionTarget target,
            Float32ActionRuntime actions,
            Float32ActionStateStore actionStore,
            Float32InputRuntime input,
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32ValueRuntime values,
            Float32BlackboardRuntime blackboard,
            Float32MotionAccumulator motion)
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

        public Float32AbilityExecutionTarget Target { get; }
        public Float32ActionRuntime Actions { get; }
        public Float32ActionStateStore ActionStore { get; }
        public Float32InputRuntime Input { get; }
        public Float32GameplayEffectOperationRuntime GameplayEffects { get; }
        public Float32EquipmentRuntime Equipment { get; }
        public Float32ValueRuntime Values { get; }
        public Float32BlackboardRuntime Blackboard { get; }
        public Float32MotionAccumulator Motion { get; }

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

        public void ApplyInputRequests()
        {
            Input.ApplyRequests();
            Input.ApplyBlackboardInputBindings(Blackboard);
        }

        public void AdvanceGameplayEffects() => GameplayEffects.Advance();
    }
}
