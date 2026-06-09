using System.Collections.Generic;

namespace ThirdPersonMovement
{
    public static class LocomotionStateGraphValidator
    {
        public static LocomotionStateGraphValidationResult Validate(LocomotionStateGraphDefinition definition)
        {
            LocomotionStateGraphValidationResult result = new LocomotionStateGraphValidationResult();

            if (definition == null)
            {
                result.AddError("State graph definition is missing.");
                return result;
            }

            BasicMovementPhase[] states = definition.EnabledStates;
            LocomotionStateGraphTransitionConfig[] transitions = definition.Transitions;
            HashSet<BasicMovementPhase> stateSet = new HashSet<BasicMovementPhase>(states);

            if (states.Length == 0)
                result.AddError("State graph has no enabled states.");

            if (!stateSet.Contains(definition.InitialState))
                result.AddError($"Initial state '{definition.InitialState}' is not enabled.");

            ValidateTransitions(transitions, stateSet, result);
            ValidateReachability(definition.InitialState, states, transitions, stateSet, result);
            ValidateRequiredDefaultTransitions(stateSet, transitions, result);

            return result;
        }

        static void ValidateTransitions(
            LocomotionStateGraphTransitionConfig[] transitions,
            HashSet<BasicMovementPhase> states,
            LocomotionStateGraphValidationResult result)
        {
            HashSet<string> transitionKeys = new HashSet<string>();
            HashSet<string> priorityKeys = new HashSet<string>();

            for (int i = 0; i < transitions.Length; i++)
            {
                LocomotionStateGraphTransitionConfig transition = transitions[i];
                if (!states.Contains(transition.From))
                    result.AddError($"Transition {i} source state '{transition.From}' is not enabled.");
                if (!states.Contains(transition.To))
                    result.AddError($"Transition {i} target state '{transition.To}' is not enabled.");

                LocomotionStateGraphCondition[] conditions = transition.Conditions;
                if (conditions.Length == 0)
                    result.AddError($"Transition {i} '{transition.From}->{transition.To}' has no conditions.");

                string transitionKey = $"{transition.From}->{transition.To}";
                if (!transitionKeys.Add(transitionKey))
                    result.AddError($"Duplicate transition '{transitionKey}'.");

                string priorityKey = $"{transition.From}:{transition.Priority}";
                if (!priorityKeys.Add(priorityKey))
                    result.AddError($"Priority conflict on state '{transition.From}' with priority {transition.Priority}.");
            }
        }

        static void ValidateReachability(
            BasicMovementPhase initialState,
            BasicMovementPhase[] states,
            LocomotionStateGraphTransitionConfig[] transitions,
            HashSet<BasicMovementPhase> stateSet,
            LocomotionStateGraphValidationResult result)
        {
            if (!stateSet.Contains(initialState))
                return;

            HashSet<BasicMovementPhase> visited = new HashSet<BasicMovementPhase>();
            Queue<BasicMovementPhase> queue = new Queue<BasicMovementPhase>();
            visited.Add(initialState);
            queue.Enqueue(initialState);

            while (queue.Count > 0)
            {
                BasicMovementPhase current = queue.Dequeue();
                for (int i = 0; i < transitions.Length; i++)
                {
                    if (transitions[i].From != current)
                        continue;

                    BasicMovementPhase target = transitions[i].To;
                    if (stateSet.Contains(target) && visited.Add(target))
                        queue.Enqueue(target);
                }
            }

            for (int i = 0; i < states.Length; i++)
            {
                if (!visited.Contains(states[i]))
                    result.AddWarning($"Enabled state '{states[i]}' is unreachable from '{initialState}'.");
            }
        }

        static void ValidateRequiredDefaultTransitions(
            HashSet<BasicMovementPhase> states,
            LocomotionStateGraphTransitionConfig[] transitions,
            LocomotionStateGraphValidationResult result)
        {
            if (!states.Contains(BasicMovementPhase.Idle) ||
                !states.Contains(BasicMovementPhase.MoveStart) ||
                !states.Contains(BasicMovementPhase.MoveLoop) ||
                !states.Contains(BasicMovementPhase.MoveStop))
            {
                return;
            }

            RequireTransition(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, transitions, result);
            RequireTransition(BasicMovementPhase.MoveStart, BasicMovementPhase.MoveStop, transitions, result);
            RequireTransition(BasicMovementPhase.MoveStart, BasicMovementPhase.MoveLoop, transitions, result);
            RequireTransition(BasicMovementPhase.MoveLoop, BasicMovementPhase.MoveStop, transitions, result);
            RequireTransition(BasicMovementPhase.MoveStop, BasicMovementPhase.MoveStart, transitions, result);
            RequireTransition(BasicMovementPhase.MoveStop, BasicMovementPhase.Idle, transitions, result);
        }

        static void RequireTransition(
            BasicMovementPhase from,
            BasicMovementPhase to,
            LocomotionStateGraphTransitionConfig[] transitions,
            LocomotionStateGraphValidationResult result)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i].From == from && transitions[i].To == to)
                    return;
            }

            result.AddError($"Required transition '{from}->{to}' is missing.");
        }
    }
}
