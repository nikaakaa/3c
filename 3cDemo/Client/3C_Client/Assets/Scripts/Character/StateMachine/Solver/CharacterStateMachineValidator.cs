using System.Collections.Generic;
using ThirdPersonAction;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateMachineValidator
    {
        public static CharacterStateMachineValidationResult Validate(CharacterStateMachineDefinition definition)
        {
            CharacterStateMachineValidationResult result = new CharacterStateMachineValidationResult();
            if (definition == null)
            {
                result.AddError("Character state machine definition is missing.");
                return result;
            }

            HashSet<CharacterStateId> ids = new HashSet<CharacterStateId>();
            IReadOnlyList<CharacterStateNodeDefinition> nodes = definition.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                CharacterStateNodeDefinition node = nodes[i];
                if (node == null)
                {
                    result.AddError($"Node[{i}] is missing.");
                    continue;
                }

                CharacterStateId id = node.StateId;
                if (!id.IsValid)
                {
                    result.AddError($"Node[{i}] state id is missing.");
                    continue;
                }

                if (!ids.Add(id))
                    result.AddError($"Node[{i}] duplicates state '{id.Value}'.");

                ValidateActionMovementNode(node, result);
            }

            StateTimelinePolicyValidationResult timelineValidation = StateTimelinePolicyValidator.Validate(definition.TimelinePolicies);
            for (int i = 0; i < timelineValidation.Errors.Count; i++)
                result.AddError(timelineValidation.Errors[i]);

            if (ids.Contains(CharacterStateIds.TurnBack) &&
                !definition.TryGetTimelinePolicy(CharacterStateIds.TurnBack, out _))
            {
                result.AddError("TurnBack timeline policy is missing.");
            }

            if (!ids.Contains(definition.InitialState))
                result.AddError($"Initial state '{definition.InitialState.Value}' is not declared.");

            IReadOnlyList<CharacterStateTransitionDefinition> transitions = definition.Transitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                CharacterStateTransitionDefinition transition = transitions[i];
                if (transition == null)
                {
                    result.AddError($"Transition[{i}] is missing.");
                    continue;
                }

                if (!IsWildcardSource(transition.FromStateId) && !ids.Contains(new CharacterStateId(transition.FromStateId)))
                    result.AddError($"Transition[{i}] source '{transition.FromStateId}' is not declared.");

                if (!ids.Contains(transition.ToStateId))
                    result.AddError($"Transition[{i}] target '{transition.ToStateId.Value}' is not declared.");
            }

            return result;
        }

        static void ValidateActionMovementNode(CharacterStateNodeDefinition node, CharacterStateMachineValidationResult result)
        {
            if (node.Output.MotionOutput != CharacterStateMotionOutputKind.ConfiguredActionMovement)
                return;

            if (!node.HasTag(CharacterStateTag.Action))
                result.AddError($"State '{node.StateId.Value}' outputs action movement but is not tagged as Action.");

            if (node.Output.ActionMovements.Count == 0)
            {
                result.AddError($"State '{node.StateId.Value}' outputs action movement but has no movement definitions.");
                return;
            }

            for (int i = 0; i < node.Output.ActionMovements.Count; i++)
                ValidateActionMovementAnimation(node, node.Output.ActionMovements[i], result);
        }

        static void ValidateActionMovementAnimation(
            CharacterStateNodeDefinition node,
            CharacterActionMovementDefinition movement,
            CharacterStateMachineValidationResult result)
        {
            CharacterStateVariant variant = movement.Variant;
            if (variant == CharacterStateVariant.None)
            {
                if (!node.Animation.HasKey || !node.Animation.HasAnimationReference)
                    result.AddError($"State '{node.StateId.Value}' action movement animation binding is missing.");
                return;
            }

            if (!node.TryResolveVariant(variant, out CharacterStateVariantDefinition definition))
            {
                result.AddError($"State '{node.StateId.Value}' action movement variant '{variant}' is missing.");
                return;
            }

            if (!definition.Animation.HasKey || !definition.Animation.HasAnimationReference)
                result.AddError($"State '{node.StateId.Value}' action movement variant '{variant}' animation binding is missing.");
        }

        static bool IsWildcardSource(string source)
        {
            return source == "*" || source.EndsWith("/*", System.StringComparison.Ordinal);
        }
    }

    public static class StateTimelinePolicyValidator
    {
        public static StateTimelinePolicyValidationResult Validate(IReadOnlyList<StateTimelinePolicyDefinition> policies)
        {
            StateTimelinePolicyValidationResult result = new StateTimelinePolicyValidationResult();
            if (policies == null || policies.Count == 0)
                return result;

            HashSet<CharacterStateId> states = new HashSet<CharacterStateId>();
            for (int i = 0; i < policies.Count; i++)
            {
                StateTimelinePolicyDefinition policy = policies[i];
                if (!policy.StateId.IsValid)
                {
                    result.AddError($"Timeline policy {i} state id is missing.");
                    continue;
                }

                if (!states.Add(policy.StateId))
                    result.AddWarning($"Timeline policy {i} duplicates state '{policy.StateId.Value}'.");

                ValidatePolicy(policy, i, result);
            }

            return result;
        }

        public static StateTimelinePolicyValidationResult Validate(params StateTimelinePolicyDefinition[] policies)
        {
            return Validate((IReadOnlyList<StateTimelinePolicyDefinition>)policies);
        }

        static void ValidatePolicy(StateTimelinePolicyDefinition policy, int index, StateTimelinePolicyValidationResult result)
        {
            HashSet<string> windowIds = new HashSet<string>();
            bool hasTurnBackMotion = false;
            bool hasTurnBackExit = false;

            for (int i = 0; i < policy.Windows.Count; i++)
            {
                StateTimelineWindowDefinition window = policy.Windows[i];
                if (string.IsNullOrWhiteSpace(window.WindowId))
                    result.AddError($"Timeline policy {index} window {i} id is missing.");
                else if (!windowIds.Add(window.WindowId))
                    result.AddWarning($"Timeline policy {index} window '{window.WindowId}' is duplicated.");

                if (!window.FactId.IsValid)
                    result.AddError($"Timeline policy {index} window '{window.WindowId}' fact id is missing.");

                if (window.Start < 0f)
                    result.AddError($"Timeline policy {index} window '{window.WindowId}' start is invalid.");

                if (window.End < window.Start)
                    result.AddError($"Timeline policy {index} window '{window.WindowId}' end is earlier than start.");

                if (window.TimeDomain == StateTimelineTimeDomain.Normalized && window.End > 1f)
                    result.AddError($"Timeline policy {index} window '{window.WindowId}' normalized end is invalid.");

                if (window.IsRequestWindow && window.RequestType == ActionRequestType.None)
                    result.AddError($"Timeline policy {index} window '{window.WindowId}' request type is missing.");

                if (policy.StateId == CharacterStateIds.TurnBack && window.Kind == StateTimelineWindowKind.Motion)
                    hasTurnBackMotion = true;
                if (policy.StateId == CharacterStateIds.TurnBack && window.Kind == StateTimelineWindowKind.Exit)
                    hasTurnBackExit = true;
            }

            if (policy.StateId == CharacterStateIds.TurnBack && !hasTurnBackMotion)
                result.AddError("TurnBack timeline policy is missing motion window.");
            if (policy.StateId == CharacterStateIds.TurnBack && !hasTurnBackExit)
                result.AddError("TurnBack timeline policy is missing exit window.");
        }
    }

    public static class StateTimelineSampler
    {
        public static StateTimelineWindowFacts Sample(
            in StateTimelinePolicyDefinition policy,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds,
            ActionRequestType requestType = ActionRequestType.None)
        {
            bool motion = false;
            bool inputLock = false;
            bool interrupt = false;
            bool exit = false;
            int priority = policy.Priority;
            int resistance = policy.Resistance;
            int minPriority = 0;
            bool force = false;
            List<string> activeIds = null;
            List<string> requestIds = null;
            List<string> activeFactIds = null;
            List<string> requestFactIds = null;

            for (int i = 0; i < policy.Windows.Count; i++)
            {
                StateTimelineWindowDefinition window = policy.Windows[i];
                if (!Contains(window, normalizedTime, hasValidNormalizedTime, elapsedSeconds))
                    continue;

                bool requestWindowAllowed = requestType != ActionRequestType.None &&
                                            window.IsRequestWindow &&
                                            window.AllowsRequest(requestType);
                if (window.IsRequestWindow && requestType != ActionRequestType.None && !requestWindowAllowed)
                    continue;

                activeIds ??= new List<string>();
                activeIds.Add(window.WindowId);
                if (window.FactId.IsValid)
                {
                    activeFactIds ??= new List<string>();
                    activeFactIds.Add(window.FactId.Value);
                }

                priority = Mathf.Max(priority, window.Priority);
                resistance = Mathf.Max(resistance, window.Resistance);

                if (requestWindowAllowed)
                {
                    requestIds ??= new List<string>();
                    requestIds.Add(window.WindowId);
                    if (window.FactId.IsValid)
                    {
                        requestFactIds ??= new List<string>();
                        requestFactIds.Add(window.FactId.Value);
                    }

                    minPriority = Mathf.Max(minPriority, window.MinPriority);
                    force = force || window.Force;
                }

                switch (window.Kind)
                {
                    case StateTimelineWindowKind.Motion:
                        motion = true;
                        break;
                    case StateTimelineWindowKind.InputLock:
                        inputLock = true;
                        break;
                    case StateTimelineWindowKind.Interrupt:
                    case StateTimelineWindowKind.Cancel:
                        interrupt = true;
                        break;
                    case StateTimelineWindowKind.Exit:
                        exit = true;
                        break;
                }
            }

            return new StateTimelineWindowFacts(
                policy.StateId,
                normalizedTime,
                hasValidNormalizedTime,
                elapsedSeconds,
                motion,
                inputLock,
                interrupt,
                exit,
                priority,
                resistance,
                minPriority,
                force,
                activeIds == null ? string.Empty : string.Join(",", activeIds),
                requestIds == null ? string.Empty : string.Join(",", requestIds),
                activeFactIds == null ? string.Empty : string.Join(",", activeFactIds),
                requestFactIds == null ? string.Empty : string.Join(",", requestFactIds));
        }

        public static StateTimelineWindowFacts None(CharacterStateId stateId)
        {
            return StateTimelineWindowFacts.None(stateId);
        }

        static bool Contains(
            StateTimelineWindowDefinition window,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds)
        {
            switch (window.TimeDomain)
            {
                case StateTimelineTimeDomain.Normalized:
                    return hasValidNormalizedTime &&
                           normalizedTime + 0.0001f >= window.Start &&
                           normalizedTime <= window.End + 0.0001f;
                case StateTimelineTimeDomain.Seconds:
                    return elapsedSeconds + 0.0001f >= window.Start &&
                           elapsedSeconds <= window.End + 0.0001f;
                default:
                    return false;
            }
        }
    }
}
