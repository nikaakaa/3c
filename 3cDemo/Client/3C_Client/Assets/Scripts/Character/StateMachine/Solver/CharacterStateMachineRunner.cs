using System;
using System.Collections.Generic;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineRunner
    {
        readonly CharacterStateMachineDefinition definition;
        CharacterStateNodeDefinition currentNode;
        CharacterStateId currentState;
        CharacterStateVariant currentVariant;
        Vector3 actionWorldDirection;
        string pendingTransitionPath;
        bool animationRequestedForState;
        bool consumeRequestOnStateEnter;
        bool resetRunLatchOnStateEnter;
        bool setRunLatchOnTransition;

        public CharacterStateMachineRunner(CharacterStateMachineDefinition definition)
        {
            this.definition = definition ?? CharacterStateMachineDefinition.CreateDefault();
            CharacterStateMachineValidationResult validation = this.definition.Validate();
            if (validation.HasErrors)
                throw new InvalidOperationException(validation.DescribeErrors());

            Reset();
        }

        public CharacterStateMachineSnapshot Snapshot => BuildSnapshot();
        public CharacterStateId CurrentState => currentState;
        public float StateTime { get; private set; }
        public CharacterStateVariant CurrentVariant => currentVariant;

        public void Reset()
        {
            SetState(definition.InitialState, CharacterStateVariant.None, Vector3.zero, string.Empty);
            StateTime = 0f;
        }

        public CharacterStateMachineFrame Tick(in CharacterStateMachineContext context)
        {
            pendingTransitionPath = string.Empty;
            consumeRequestOnStateEnter = false;
            resetRunLatchOnStateEnter = false;
            setRunLatchOnTransition = false;

            float projectedStateTime = StateTime + context.DeltaTime;
            if (TryResolveTransition(in context, projectedStateTime, out CharacterStateTransitionDefinition transition))
                ApplyTransition(transition, in context);

            StateTime += context.DeltaTime;
            return BuildFrame(in context);
        }

        bool TryResolveTransition(
            in CharacterStateMachineContext context,
            float projectedStateTime,
            out CharacterStateTransitionDefinition selected)
        {
            selected = null;
            IReadOnlyList<CharacterStateTransitionDefinition> transitions = definition.Transitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                CharacterStateTransitionDefinition candidate = transitions[i];
                if (candidate == null || !candidate.MatchesSource(currentState))
                    continue;

                if (!AllConditionsPass(candidate, in context, projectedStateTime))
                    continue;

                if (selected == null || candidate.Priority > selected.Priority)
                    selected = candidate;
            }

            return selected != null;
        }

        bool AllConditionsPass(
            CharacterStateTransitionDefinition transition,
            in CharacterStateMachineContext context,
            float projectedStateTime)
        {
            IReadOnlyList<CharacterStateTransitionCondition> conditions = transition.Conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!CharacterStateTransitionEvaluator.Evaluate(conditions[i], in context, currentNode, projectedStateTime))
                    return false;
            }

            return true;
        }

        void ApplyTransition(CharacterStateTransitionDefinition transition, in CharacterStateMachineContext context)
        {
            definition.TryGetNode(transition.ToStateId, out CharacterStateNodeDefinition targetNode);
            bool enteringActionState = targetNode != null && targetNode.HasTag(CharacterStateTag.Action);
            bool leavingActionState = currentNode != null && currentNode.HasTag(CharacterStateTag.Action) && !enteringActionState;
            CharacterStateVariant variant = CharacterStateVariant.None;
            Vector3 worldDirection = Vector3.zero;
            bool shouldLatchRun = leavingActionState &&
                                  currentNode.Output.TryResolveActionMovement(
                                      currentVariant,
                                      out CharacterActionMovementDefinition exitingMovement) &&
                                  exitingMovement.SetRunLatchOnComplete;

            if (enteringActionState && context.InputRequest.HasRequest)
            {
                variant = context.InputRequest.Variant;
                worldDirection = context.InputRequest.WorldDirection;
            }

            SetState(transition.ToStateId, variant, worldDirection, transition.TransitionPath);
            setRunLatchOnTransition = shouldLatchRun;
        }

        void SetState(
            CharacterStateId nextState,
            CharacterStateVariant variant,
            Vector3 worldDirection,
            string transitionPath)
        {
            if (!definition.TryGetNode(nextState, out currentNode))
                throw new InvalidOperationException($"Character state '{nextState.Value}' is not declared.");

            currentState = nextState;
            currentVariant = variant;
            actionWorldDirection = NormalizePlanarOrZero(worldDirection);
            StateTime = 0f;
            pendingTransitionPath = transitionPath ?? string.Empty;
            animationRequestedForState = false;
            consumeRequestOnStateEnter = currentNode.Output.ConsumeInputRequest;
            resetRunLatchOnStateEnter = currentNode.Output.ResetRunLatchOnEnter;
        }

        CharacterStateMachineFrame BuildFrame(in CharacterStateMachineContext context)
        {
            CharacterStateOutputDefinition output = currentNode.Output;
            bool executeBasicMovement = output.MotionOutput == CharacterStateMotionOutputKind.InputDrivenMovement;
            bool presentLocomotionAnimation = executeBasicMovement || currentState == CharacterStateIds.Idle;
            bool hasActionMovement = false;
            bool actionCompleted = false;
            bool setRunLatch = setRunLatchOnTransition;
            ActionMovementCommand actionCommand = default;
            bool hasAnimationRequest = TryBuildAnimationRequest(context.CurrentStep, out CharacterStateAnimationRequest animationRequest);

            if (output.MotionOutput == CharacterStateMotionOutputKind.ConfiguredActionMovement &&
                output.TryResolveActionMovement(currentVariant, out CharacterActionMovementDefinition movement))
            {
                float duration = movement.Duration;
                float distance = movement.Distance;
                float frameDistance = duration > 0f
                    ? distance * Mathf.Min(context.DeltaTime, Mathf.Max(0f, duration - Mathf.Max(0f, StateTime - context.DeltaTime))) / duration
                    : 0f;

                actionCommand = new ActionMovementCommand(
                    actionWorldDirection,
                    frameDistance,
                    context.DeltaTime,
                    movement.RotateToDirection);
                hasActionMovement = actionCommand.HasMovement;
                actionCompleted = duration <= 0f || StateTime >= duration;
                setRunLatch = setRunLatch || (actionCompleted && movement.SetRunLatchOnComplete);
            }

            CharacterStateMachineSnapshot snapshot = BuildSnapshot();
            bool consumeRequest = consumeRequestOnStateEnter;
            bool resetRunLatch = resetRunLatchOnStateEnter;
            consumeRequestOnStateEnter = false;
            resetRunLatchOnStateEnter = false;
            setRunLatchOnTransition = false;

            return new CharacterStateMachineFrame(
                snapshot,
                executeBasicMovement,
                presentLocomotionAnimation,
                consumeRequest,
                output.ConsumeRequestKind,
                setRunLatch,
                resetRunLatch,
                actionCommand,
                hasActionMovement,
                actionCompleted,
                animationRequest,
                hasAnimationRequest);
        }

        bool TryBuildAnimationRequest(int sourceStep, out CharacterStateAnimationRequest request)
        {
            CharacterStateAnimationBinding binding = currentNode.Animation;
            if (currentVariant != CharacterStateVariant.None && currentNode.TryResolveVariant(currentVariant, out CharacterStateVariantDefinition variant))
                binding = variant.Animation;

            if (!binding.HasKey || animationRequestedForState)
            {
                request = default;
                return false;
            }

            animationRequestedForState = true;
            request = new CharacterStateAnimationRequest(binding, sourceStep);
            return true;
        }

        CharacterStateMachineSnapshot BuildSnapshot()
        {
            CharacterStateTag[] tags = Array.Empty<CharacterStateTag>();
            if (currentNode != null)
            {
                tags = new CharacterStateTag[currentNode.Tags.Count];
                for (int i = 0; i < tags.Length; i++)
                    tags[i] = currentNode.Tags[i];
            }

            return new CharacterStateMachineSnapshot(
                currentState,
                StateTime,
                currentVariant,
                pendingTransitionPath,
                tags);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
