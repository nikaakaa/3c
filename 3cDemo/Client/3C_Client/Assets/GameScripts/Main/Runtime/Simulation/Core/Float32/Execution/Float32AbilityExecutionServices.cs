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
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32ValueRuntime values,
            Float32BlackboardRuntime blackboard)
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

        public Float32AbilityExecutionTarget Target { get; }
        public Float32ActionRuntime Actions { get; }
        public Float32ActionStateStore ActionStore { get; }
        public Float32GameplayEffectOperationRuntime GameplayEffects { get; }
        public Float32EquipmentRuntime Equipment { get; }
        public Float32ValueRuntime Values { get; }
        public Float32BlackboardRuntime Blackboard { get; }

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
