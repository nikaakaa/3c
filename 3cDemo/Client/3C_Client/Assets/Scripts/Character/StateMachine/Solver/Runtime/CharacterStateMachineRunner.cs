using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonInput;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMachineRunner
    {
        readonly CharacterStateMachineDefinition definition;
        readonly ICharacterStateLifecycle lifecycle;
        readonly CharacterStateTransitionEvaluatorCollection transitionEvaluators;
        StateGraphNode currentGraphNode;
        CharacterStateNodeDefinition currentNode;
        CharacterStateId currentState;
        CharacterStateVariant currentVariant;
        CharacterStatePayload statePayload;
        string pendingTransitionPath;
        bool animationRequestedForState;

        public CharacterStateMachineRunner(CharacterStateMachineDefinition definition)
            : this(definition, CharacterStateNodeLifecycle.Instance, CharacterStateTransitionEvaluatorCollection.Default)
        {
        }

        public CharacterStateMachineRunner(
            CharacterStateMachineDefinition definition,
            ICharacterStateLifecycle lifecycle)
            : this(definition, lifecycle, CharacterStateTransitionEvaluatorCollection.Default)
        {
        }

        public CharacterStateMachineRunner(
            CharacterStateMachineDefinition definition,
            ICharacterStateLifecycle lifecycle,
            CharacterStateTransitionEvaluatorCollection transitionEvaluators)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.transitionEvaluators = transitionEvaluators ?? throw new ArgumentNullException(nameof(transitionEvaluators));
            CharacterStateMachineValidationResult validation = CharacterStateMachineValidator.Validate(this.definition, this.transitionEvaluators);
            if (validation.HasErrors)
                throw new InvalidOperationException(validation.DescribeErrors());

            Reset();
        }

        public CharacterStateMachineSnapshot Snapshot => BuildSnapshot();
        public CharacterStateMachineDefinition Definition => definition;
        public CharacterStateId CurrentState => currentState;
        public float StateTime { get; private set; }
        public CharacterStateVariant CurrentVariant => currentVariant;
        public CharacterStateMachineRestoreState RestoreState => CaptureRestoreState();

        public void Reset()
        {
            SetState(definition.InitialState, CharacterStateVariant.None, CharacterStatePayload.Empty, string.Empty);
            StateTime = 0f;
        }

        public CharacterStateMachineRestoreState CaptureRestoreState()
        {
            return new CharacterStateMachineRestoreState(
                BuildSnapshot(),
                statePayload,
                animationRequestedForState);
        }

        public bool Restore(in CharacterStateMachineRestoreState restoreState)
        {
            CharacterStateMachineSnapshot snapshot = restoreState.Snapshot;
            StateGraphNodeId graphNodeId = new StateGraphNodeId(snapshot.ActiveState.Value);
            if (!snapshot.ActiveState.IsValid ||
                !definition.Graph.TryGetNode(graphNodeId, out StateGraphNode graphNode) ||
                !definition.TryGetNode(snapshot.ActiveState, out CharacterStateNodeDefinition node))
            {
                return false;
            }

            currentGraphNode = graphNode;
            currentNode = node;
            currentState = snapshot.ActiveState;
            currentVariant = snapshot.Variant;
            statePayload = restoreState.StatePayload;
            StateTime = Mathf.Max(0f, snapshot.StateTime);
            pendingTransitionPath = snapshot.PendingTransitionPath;
            animationRequestedForState = restoreState.AnimationRequestedForState;
            return true;
        }

        public CharacterStateMachineFrame Tick(in CharacterStateMachineContext context)
        {
            pendingTransitionPath = string.Empty;
            CharacterStateMachineFrameBuilder builder = new CharacterStateMachineFrameBuilder();
            List<CharacterStateTransitionConditionTrace> conditionTraces = new List<CharacterStateTransitionConditionTrace>();

            float projectedStateTime = StateTime + context.DeltaTime;
            ActionRequestType requestType = context.InputRequest.RequestType();
            StateTimelineWindowFacts projectedFacts = CharacterStateTimelineFactSampler.SampleCurrent(
                definition,
                currentNode,
                currentState,
                currentVariant,
                in context,
                projectedStateTime,
                requestType);
            CharacterStateMachineContext transitionContext = context.WithProjectedTimelineFacts(projectedFacts, requestType);
            CharacterStateMachineContext activeTimelineContext = context;
            if (TryResolveTransition(in transitionContext, projectedStateTime, conditionTraces, out CharacterStateTransitionDefinition transition))
            {
                CharacterStateNodeDefinition sourceNode = currentNode;
                CharacterStateId sourceState = currentState;
                CharacterStateVariant sourceVariant = currentVariant;
                CharacterStatePayload sourcePayload = statePayload;
                definition.TryGetNode(transition.ToStateId, out CharacterStateNodeDefinition targetNode);
                CharacterStateLifecycleContext exitContext = BuildLifecycleContext(
                    sourceNode,
                    targetNode,
                    sourceState,
                    sourceVariant,
                    StateTime,
                    sourcePayload.PrimaryWorldDirection,
                    in transitionContext);
                lifecycle.Exit(in exitContext, builder);

                ApplyTransition(transition, in transitionContext);
                StateTimelineWindowFacts targetFacts = CharacterStateTimelineFactSampler.SampleCurrent(
                    definition,
                    currentNode,
                    currentState,
                    currentVariant,
                    in context,
                    StateTime,
                    requestType);
                activeTimelineContext = transitionContext.WithTargetTimelineFacts(targetFacts, requestType);
                CharacterStateLifecycleContext enterContext = BuildLifecycleContext(
                    currentNode,
                    null,
                    currentState,
                    currentVariant,
                    StateTime,
                    statePayload.PrimaryWorldDirection,
                    in activeTimelineContext);
                lifecycle.Enter(in enterContext, builder);
            }

            StateTime += context.DeltaTime;
            CharacterStateLifecycleContext tickContext = BuildLifecycleContext(
                currentNode,
                null,
                currentState,
                currentVariant,
                StateTime,
                statePayload.PrimaryWorldDirection,
                in activeTimelineContext);
            lifecycle.Tick(in tickContext, builder);
            if (builder.HasAnimationRequest)
                animationRequestedForState = true;

            return BuildFrame(in activeTimelineContext, builder, conditionTraces.ToArray());
        }

        bool TryResolveTransition(
            in CharacterStateMachineContext context,
            float projectedStateTime,
            List<CharacterStateTransitionConditionTrace> conditionTraces,
            out CharacterStateTransitionDefinition selected)
        {
            selected = null;
            IReadOnlyList<StateGraphTransition> graphTransitions = definition.Graph.Transitions;
            IReadOnlyList<CharacterStateTransitionDefinition> transitions = definition.Transitions;
            int count = Mathf.Min(graphTransitions.Count, transitions.Count);
            StateGraphNodeId currentNodeId = currentGraphNode != null
                ? currentGraphNode.Id
                : new StateGraphNodeId(currentState.Value);
            for (int i = 0; i < count; i++)
            {
                StateGraphTransition graphTransition = graphTransitions[i];
                CharacterStateTransitionDefinition candidate = transitions[i];
                if (candidate == null || graphTransition == null || !graphTransition.MatchesSource(currentNodeId))
                    continue;

                if (!AllConditionsPass(candidate, in context, projectedStateTime, conditionTraces))
                    continue;

                if (selected == null || candidate.Priority > selected.Priority)
                    selected = candidate;
            }

            return selected != null;
        }

        bool AllConditionsPass(
            CharacterStateTransitionDefinition transition,
            in CharacterStateMachineContext context,
            float projectedStateTime,
            List<CharacterStateTransitionConditionTrace> conditionTraces)
        {
            IReadOnlyList<CharacterStateTransitionCondition> conditions = transition.Conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                CharacterStateTransitionCondition condition = conditions[i];
                CharacterStateTransitionConditionEvaluationInput input = new CharacterStateTransitionConditionEvaluationInput(
                        condition,
                        in context,
                        currentNode,
                        currentVariant,
                        currentState,
                        transition,
                        StateTime,
                        projectedStateTime);
                CharacterStateTransitionConditionEvaluationResult result = transitionEvaluators.Evaluate(in input);
                conditionTraces?.Add(result.Trace);

                if (!result.Passed)
                    return false;
            }

            return true;
        }

        void ApplyTransition(CharacterStateTransitionDefinition transition, in CharacterStateMachineContext context)
        {
            definition.TryGetNode(transition.ToStateId, out CharacterStateNodeDefinition targetNode);
            bool enteringActionState = targetNode != null && targetNode.IsActionCapabilityState;
            CharacterStateVariant variant = CharacterStateVariant.None;
            CharacterStatePayload nextPayload = CharacterStatePayload.Empty;

            if (enteringActionState && context.InputRequest.HasRequest)
            {
                variant = context.InputRequest.Variant;
                nextPayload = new CharacterStatePayload(
                    context.InputRequest.WorldDirection,
                    Vector3.zero,
                    Vector3.zero);
            }
            else if (targetNode != null &&
                     targetNode.TryGetTurnBackMotionPolicy(out _) &&
                     context.InputRequest.HasRequest &&
                     context.InputRequest.HasWorldDirection)
            {
                nextPayload = new CharacterStatePayload(
                    Vector3.zero,
                    context.InputRequest.WorldDirection,
                    context.FacingForward);
            }

            SetState(transition.ToStateId, variant, nextPayload, transition.TransitionPath);
        }

        void SetState(
            CharacterStateId nextState,
            CharacterStateVariant variant,
            CharacterStatePayload payload,
            string transitionPath)
        {
            StateGraphNodeId graphNodeId = new StateGraphNodeId(nextState.Value);
            if (!definition.Graph.TryGetNode(graphNodeId, out currentGraphNode) ||
                !definition.TryGetNode(nextState, out currentNode))
            {
                throw new InvalidOperationException($"Character state '{nextState.Value}' is not declared.");
            }

            currentState = nextState;
            currentVariant = variant;
            statePayload = payload;
            StateTime = 0f;
            pendingTransitionPath = transitionPath ?? string.Empty;
            animationRequestedForState = false;
        }

        CharacterStateLifecycleContext BuildLifecycleContext(
            CharacterStateNodeDefinition node,
            CharacterStateNodeDefinition targetNode,
            CharacterStateId stateId,
            CharacterStateVariant variant,
            float stateTime,
            Vector3 stateActionWorldDirection,
            in CharacterStateMachineContext context)
        {
            return new CharacterStateLifecycleContext(
                node,
                targetNode,
                stateId,
                variant,
                stateTime,
                stateActionWorldDirection,
                in context,
                animationRequestedForState);
        }

        CharacterStateMachineFrame BuildFrame(
            in CharacterStateMachineContext context,
            CharacterStateMachineFrameBuilder builder,
            CharacterStateTransitionConditionTrace[] conditionTraces)
        {
            CharacterStateMachineSnapshot snapshot = BuildSnapshot();
            CharacterStateOutputResolverInput input = new CharacterStateOutputResolverInput(
                currentNode,
                currentState,
                currentVariant,
                StateTime,
                statePayload,
                in context,
                in snapshot,
                builder,
                conditionTraces);
            return CharacterStateOutputResolver.Resolve(in input);
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

    }

    static class CharacterInputRequestFactTimelineExtensions
    {
        public static ActionRequestType RequestType(this CharacterInputRequestFact fact)
        {
            if (!fact.HasRequest)
                return ActionRequestType.None;

            return fact.RequestKind switch
            {
                InputRequestKind.Dodge => ActionRequestType.Dodge,
                InputRequestKind.Attack => ActionRequestType.Attack,
                InputRequestKind.TurnBack => ActionRequestType.Locomotion,
                _ => ActionRequestType.Custom
            };
        }
    }
}
