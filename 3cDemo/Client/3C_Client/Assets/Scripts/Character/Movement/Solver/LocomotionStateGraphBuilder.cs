using System;
using UnityHFSM;

namespace ThirdPersonMovement
{
    public static class LocomotionStateGraphBuilder
    {
        public static StateMachine<BasicMovementPhase> Build(
            LocomotionStateGraphDefinition definition,
            Func<LocomotionStateGraphContext> contextProvider,
            Action onTransition)
        {
            LocomotionStateGraphValidationResult validation = LocomotionStateGraphValidator.Validate(definition);
            if (validation.HasErrors)
                throw new InvalidOperationException(validation.DescribeErrors());

            StateMachine<BasicMovementPhase> fsm = new StateMachine<BasicMovementPhase>();
            BasicMovementPhase[] states = definition.EnabledStates;
            for (int i = 0; i < states.Length; i++)
                fsm.AddState(states[i]);

            fsm.SetStartState(definition.InitialState);

            LocomotionStateGraphTransitionConfig[] transitions = definition.Transitions;
            Array.Sort(transitions, CompareTransitionPriority);
            for (int i = 0; i < transitions.Length; i++)
            {
                LocomotionStateGraphTransitionConfig transition = transitions[i];
                fsm.AddTransition(
                    transition.From,
                    transition.To,
                    _ => ConditionsMet(transition.Conditions, contextProvider()),
                    onTransition: _ => onTransition?.Invoke());
            }

            return fsm;
        }

        static int CompareTransitionPriority(
            LocomotionStateGraphTransitionConfig left,
            LocomotionStateGraphTransitionConfig right)
        {
            int fromCompare = left.From.CompareTo(right.From);
            return fromCompare != 0 ? fromCompare : right.Priority.CompareTo(left.Priority);
        }

        static bool ConditionsMet(LocomotionStateGraphCondition[] conditions, in LocomotionStateGraphContext context)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!LocomotionStateGraphConditionEvaluator.Evaluate(conditions[i], in context))
                    return false;
            }

            return true;
        }
    }
}
