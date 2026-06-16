using System.Collections.Generic;
using ThirdPersonAction;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateMachineValidator
    {
        public static CharacterStateMachineValidationResult Validate(CharacterStateMachineDefinition definition)
        {
            return Validate(definition, CharacterStateTransitionEvaluatorCollection.Default);
        }

        public static CharacterStateMachineValidationResult Validate(
            CharacterStateMachineDefinition definition,
            params ICharacterStateTransitionConditionEvaluator[] evaluators)
        {
            if (!CharacterStateTransitionEvaluatorCollection.TryCreate(
                    evaluators,
                    out CharacterStateTransitionEvaluatorCollection collection,
                    out string error))
            {
                CharacterStateMachineValidationResult result = new CharacterStateMachineValidationResult();
                result.AddError(error);
                return result;
            }

            return Validate(definition, collection);
        }

        public static CharacterStateMachineValidationResult Validate(
            CharacterStateMachineDefinition definition,
            CharacterStateTransitionEvaluatorCollection transitionEvaluators)
        {
            CharacterStateMachineValidationResult result = new CharacterStateMachineValidationResult();
            if (definition == null)
            {
                result.AddError("Character state machine definition is missing.");
                return result;
            }

            if (transitionEvaluators == null)
                result.AddError("Transition condition evaluator collection is missing.");

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

                ValidateNodeModules(node, result);
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

                ValidateTransitionConditions(transition, i, transitionEvaluators, result);
            }

            return result;
        }

        static void ValidateTransitionConditions(
            CharacterStateTransitionDefinition transition,
            int index,
            CharacterStateTransitionEvaluatorCollection transitionEvaluators,
            CharacterStateMachineValidationResult result)
        {
            if (transitionEvaluators == null)
                return;

            IReadOnlyList<CharacterStateTransitionCondition> conditions = transition.Conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                CharacterStateTransitionCondition condition = conditions[i];
                if (!transitionEvaluators.Supports(condition.Kind))
                    result.AddError($"Transition[{index}] condition '{condition.Kind}' has no evaluator.");
            }
        }

        static void ValidateNodeModules(CharacterStateNodeDefinition node, CharacterStateMachineValidationResult result)
        {
            ValidateModuleAuthorities(node, result);

            if (!node.HasOutputModule && node.HasModule(CharacterStateModuleType.ActionAnimation))
                result.AddError($"State '{node.StateId.Value}' has action animation module without output module.");

            if (!node.HasModule(CharacterStateModuleType.ConfiguredActionMotion))
                return;

            if (!node.TryGetModule(CharacterStateModuleType.ConfiguredActionMotion, out CharacterStateModuleDefinition actionMotion) ||
                actionMotion.ActionMovements.Count == 0)
            {
                result.AddError($"State '{node.StateId.Value}' outputs action movement but has no movement definitions.");
                return;
            }

            if (!node.HasModule(CharacterStateModuleType.InputConsume))
                result.AddError($"State '{node.StateId.Value}' outputs action movement but has no input consume module.");

            if (!node.HasModule(CharacterStateModuleType.ActionAnimation))
                result.AddError($"State '{node.StateId.Value}' outputs action movement but has no action animation module.");

            for (int i = 0; i < actionMotion.ActionMovements.Count; i++)
                ValidateActionMovementAnimation(node, actionMotion.ActionMovements[i], result);
        }

        static void ValidateModuleAuthorities(CharacterStateNodeDefinition node, CharacterStateMachineValidationResult result)
        {
            int motionAuthorityCount = 0;
            int animationAuthorityCount = 0;
            IReadOnlyList<CharacterStateModuleDefinition> modules = node.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                CharacterStateModuleDefinition module = modules[i];
                if (module == null)
                    continue;

                if (IsMotionAuthority(module.ModuleType))
                    motionAuthorityCount++;
                if (IsAnimationAuthority(module.ModuleType))
                    animationAuthorityCount++;
            }

            if (motionAuthorityCount > 1)
                result.AddError($"State '{node.StateId.Value}' has duplicate motion authority modules.");
            if (animationAuthorityCount > 1)
                result.AddError($"State '{node.StateId.Value}' has duplicate animation authority modules.");
        }

        static bool IsMotionAuthority(CharacterStateModuleType moduleType)
        {
            return moduleType == CharacterStateModuleType.InputDrivenMotion ||
                   moduleType == CharacterStateModuleType.ConfiguredActionMotion ||
                   moduleType == CharacterStateModuleType.TurnBackMotionPolicy;
        }

        static bool IsAnimationAuthority(CharacterStateModuleType moduleType)
        {
            return moduleType == CharacterStateModuleType.ActionAnimation ||
                   moduleType == CharacterStateModuleType.LocomotionAnimationAlias;
        }

        static void ValidateActionMovementAnimation(
            CharacterStateNodeDefinition node,
            CharacterActionMovementDefinition movement,
            CharacterStateMachineValidationResult result)
        {
            CharacterStateVariant variant = movement.Variant;
            if (variant == CharacterStateVariant.None)
            {
                if (!node.TryResolveAnimationBinding(
                        CharacterStateVariant.None,
                        out CharacterStateAnimationBinding binding,
                        out CharacterStatePlaybackFactSource playbackFactSource) ||
                    playbackFactSource != CharacterStatePlaybackFactSource.Action ||
                    !binding.HasKey)
                {
                    result.AddError($"State '{node.StateId.Value}' action movement animation binding is missing.");
                }

                return;
            }

            if (!node.TryResolveAnimationBinding(
                    variant,
                    out CharacterStateAnimationBinding variantBinding,
                    out CharacterStatePlaybackFactSource variantPlaybackFactSource))
            {
                result.AddError($"State '{node.StateId.Value}' action movement variant '{variant}' is missing.");
                return;
            }

            if (variantPlaybackFactSource != CharacterStatePlaybackFactSource.Action || !variantBinding.HasKey)
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

}
