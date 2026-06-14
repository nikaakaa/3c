using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonDiagnostics;
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
        Vector3 turnBackWorldDirection;
        Vector3 turnBackEntryBasisForward;
        string pendingTransitionPath;
        bool animationRequestedForState;
        bool consumeRequestOnStateEnter;
        bool resetRunLatchOnStateEnter;
        bool setRunLatchOnTransition;

        public CharacterStateMachineRunner(CharacterStateMachineDefinition definition)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CharacterStateMachineValidationResult validation = this.definition.Validate();
            if (validation.HasErrors)
                throw new InvalidOperationException(validation.DescribeErrors());

            Reset();
        }

        public CharacterStateMachineSnapshot Snapshot => BuildSnapshot();
        public CharacterStateId CurrentState => currentState;
        public float StateTime { get; private set; }
        public CharacterStateVariant CurrentVariant => currentVariant;
        public CharacterStateMachineRestoreState RestoreState => CaptureRestoreState();

        public void Reset()
        {
            SetState(definition.InitialState, CharacterStateVariant.None, Vector3.zero, string.Empty);
            StateTime = 0f;
        }

        public CharacterStateMachineRestoreState CaptureRestoreState()
        {
            return new CharacterStateMachineRestoreState(
                BuildSnapshot(),
                actionWorldDirection,
                turnBackWorldDirection,
                turnBackEntryBasisForward,
                animationRequestedForState,
                consumeRequestOnStateEnter,
                resetRunLatchOnStateEnter,
                setRunLatchOnTransition);
        }

        public bool Restore(in CharacterStateMachineRestoreState restoreState)
        {
            CharacterStateMachineSnapshot snapshot = restoreState.Snapshot;
            if (!snapshot.ActiveState.IsValid || !definition.TryGetNode(snapshot.ActiveState, out CharacterStateNodeDefinition node))
                return false;

            currentNode = node;
            currentState = snapshot.ActiveState;
            currentVariant = snapshot.Variant;
            actionWorldDirection = NormalizePlanarOrZero(restoreState.ActionWorldDirection);
            turnBackWorldDirection = NormalizePlanarOrZero(restoreState.TurnBackWorldDirection);
            turnBackEntryBasisForward = NormalizePlanarOrZero(restoreState.TurnBackEntryBasisForward);
            StateTime = Mathf.Max(0f, snapshot.StateTime);
            pendingTransitionPath = snapshot.PendingTransitionPath;
            animationRequestedForState = restoreState.AnimationRequestedForState;
            consumeRequestOnStateEnter = restoreState.ConsumeRequestOnStateEnter;
            resetRunLatchOnStateEnter = restoreState.ResetRunLatchOnStateEnter;
            setRunLatchOnTransition = restoreState.SetRunLatchOnTransition;
            return true;
        }

        public CharacterStateMachineFrame Tick(in CharacterStateMachineContext context)
        {
            pendingTransitionPath = string.Empty;
            consumeRequestOnStateEnter = false;
            resetRunLatchOnStateEnter = false;
            setRunLatchOnTransition = false;

            float projectedStateTime = StateTime + context.DeltaTime;
            CharacterStateMachineContext timelineContext = context.WithTimelineFacts(SampleCurrentTimelineFacts(in context, projectedStateTime, context.InputRequest.RequestType()));
            if (TryResolveTransition(in timelineContext, projectedStateTime, out CharacterStateTransitionDefinition transition))
            {
                ApplyTransition(transition, in timelineContext);
                timelineContext = context.WithTimelineFacts(SampleCurrentTimelineFacts(in context, StateTime, context.InputRequest.RequestType()));
            }

            StateTime += context.DeltaTime;
            return BuildFrame(in timelineContext);
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
                CharacterStateTransitionCondition condition = conditions[i];
                bool passed = CharacterStateTransitionEvaluator.Evaluate(
                        condition,
                        in context,
                        currentNode,
                        currentVariant,
                        projectedStateTime);

                LogTurnBackConditionProbe(transition, condition, in context, projectedStateTime, passed);

                if (!passed)
                    return false;
            }

            return true;
        }

        void LogTurnBackConditionProbe(
            CharacterStateTransitionDefinition transition,
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            float projectedStateTime,
            bool passed)
        {
            if (condition.Kind != CharacterStateTransitionConditionKind.MoveTurnBackRequested)
                return;

            ThirdPersonMovement.LocomotionTurnBackIntent intent = context.LocomotionFacts.TurnBackIntent;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-turnback-condition",
                currentState.Value,
                transition.ToStateId.Value,
                context.CurrentStep,
                Time.frameCount,
                $"from={transition.FromStateId} to={transition.ToStateId.Value} priority={transition.Priority} hasMove={context.HasMoveIntent} worldMove={context.WorldMoveDirection.ToString("F3")} facing={context.FacingForward.ToString("F3")} intentValid={intent.IsValidAt(context.CurrentStep)} intentOrigin={intent.OriginStep} intentExpire={intent.ExpireStep} angle={intent.Angle:F3} threshold={condition.MinSeconds:F3} passed={passed} stateTime={StateTime:F3} projectedStateTime={projectedStateTime:F3} phaseCanExit={context.StateCanExit} locomotionPhase={context.RuntimeBlackboard.Locomotion.Phase} blackboardHasMove={context.RuntimeBlackboard.Locomotion.HasMoveIntent} blackboardWorld={context.RuntimeBlackboard.Locomotion.WorldDirection.ToString("F3")}"));
        }

        void ApplyTransition(CharacterStateTransitionDefinition transition, in CharacterStateMachineContext context)
        {
            definition.TryGetNode(transition.ToStateId, out CharacterStateNodeDefinition targetNode);
            bool enteringActionState = targetNode != null && targetNode.HasTag(CharacterStateTag.Action);
            bool leavingActionState = currentNode != null && currentNode.HasTag(CharacterStateTag.Action) && !enteringActionState;
            CharacterStateVariant variant = CharacterStateVariant.None;
            Vector3 worldDirection = Vector3.zero;
            Vector3 nextTurnBackWorldDirection = Vector3.zero;
            Vector3 nextTurnBackEntryBasisForward = Vector3.zero;
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

            if (transition.ToStateId == CharacterStateIds.TurnBack)
            {
                nextTurnBackWorldDirection = context.InputRequest.HasRequest &&
                                              context.InputRequest.RequestKind == InputRequestKind.TurnBack &&
                                              context.InputRequest.HasWorldDirection
                    ? context.InputRequest.WorldDirection
                    : Vector3.zero;
                nextTurnBackEntryBasisForward = context.FacingForward;
            }

            SetState(transition.ToStateId, variant, worldDirection, transition.TransitionPath);
            if (transition.ToStateId == CharacterStateIds.TurnBack)
            {
                turnBackWorldDirection = NormalizePlanarOrZero(nextTurnBackWorldDirection);
                turnBackEntryBasisForward = NormalizePlanarOrZero(nextTurnBackEntryBasisForward);
            }
            else
            {
                turnBackWorldDirection = Vector3.zero;
                turnBackEntryBasisForward = Vector3.zero;
            }
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
            if (nextState != CharacterStateIds.TurnBack)
            {
                turnBackWorldDirection = Vector3.zero;
                turnBackEntryBasisForward = Vector3.zero;
            }
            StateTime = 0f;
            pendingTransitionPath = transitionPath ?? string.Empty;
            animationRequestedForState = false;
            consumeRequestOnStateEnter = currentNode.Output.ConsumeInputRequest;
            resetRunLatchOnStateEnter = currentNode.Output.ResetRunLatchOnEnter;
        }

        CharacterStateMachineFrame BuildFrame(in CharacterStateMachineContext context)
        {
            CharacterStateOutputDefinition output = currentNode.Output;
            bool executeBasicMovement =
                output.MotionOutput == CharacterStateMotionOutputKind.InputDrivenMovement ||
                output.MotionOutput == CharacterStateMotionOutputKind.AnimationDrivenLocomotion;
            bool presentLocomotionAnimation = executeBasicMovement || currentState == CharacterStateIds.Idle;
            bool hasActionMovement = false;
            bool actionCompleted = false;
            bool setRunLatch = setRunLatchOnTransition;
            ActionMovementCommand actionCommand = default;
            bool hasAnimationRequest = TryBuildAnimationRequest(context.CurrentStep, out CharacterStateAnimationRequest animationRequest);
            bool hasTurnBackMotionPolicy = currentState == CharacterStateIds.TurnBack && output.HasTurnBackMotionPolicy;
            ThirdPersonMovement.TurnBackMotionPolicy turnBackMotionPolicy = hasTurnBackMotionPolicy
                ? output.TurnBackMotionPolicy
                : default;

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
                hasAnimationRequest,
                turnBackMotionPolicy,
                hasTurnBackMotionPolicy,
                turnBackWorldDirection,
                turnBackEntryBasisForward,
                context.TimelineFacts);
        }

        public StateTimelineWindowFacts SampleCurrentTimelineFacts(
            in CharacterStateMachineContext context,
            float elapsedSeconds,
            ActionRequestType requestType)
        {
            if (!definition.TryGetTimelinePolicy(currentState, out StateTimelinePolicyDefinition policy))
                return StateTimelineSampler.None(currentState);

            ResolveCurrentPlaybackProgress(in context, out float normalizedTime, out bool hasValidNormalizedTime);
            StateTimelineWindowFacts facts = StateTimelineSampler.Sample(
                in policy,
                normalizedTime,
                hasValidNormalizedTime,
                elapsedSeconds,
                requestType);
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "state-timeline-window-facts",
                currentState.Value,
                string.Empty,
                context.CurrentStep,
                Time.frameCount,
                $"state={currentState.Value} normalized={facts.NormalizedTime:F3} normalizedValid={facts.HasValidNormalizedTime} elapsed={facts.ElapsedSeconds:F3} motion={facts.MotionWindowActive} inputLock={facts.InputLockWindowActive} interrupt={facts.InterruptWindowActive} exit={facts.ExitWindowActive} priority={facts.Priority} resistance={facts.Resistance} minPriority={facts.MinPriority} force={facts.Force} activeWindows={facts.ActiveWindowIds} requestWindows={facts.RequestWindowIds} request={requestType}"));
            return facts;
        }

        void ResolveCurrentPlaybackProgress(
            in CharacterStateMachineContext context,
            out float normalizedTime,
            out bool hasValidNormalizedTime)
        {
            normalizedTime = 0f;
            hasValidNormalizedTime = false;
            CharacterStateAnimationBinding binding = currentNode != null ? currentNode.Animation : default;
            if (currentVariant != CharacterStateVariant.None &&
                currentNode != null &&
                currentNode.TryResolveVariant(currentVariant, out CharacterStateVariantDefinition variant))
            {
                binding = variant.Animation;
            }

            CharacterRuntimeAnimationFacts animation = context.RuntimeBlackboard.Animation;
            if (currentState == CharacterStateIds.TurnBack || (currentNode != null && currentNode.HasTag(CharacterStateTag.Locomotion)))
            {
                AnimationPhasePlaybackProgress progress = animation.LocomotionProgress;
                if (!binding.HasKey || !progress.HasValidPlayback || progress.AliasKey != binding.KeyValue)
                    return;

                normalizedTime = progress.NormalizedTime;
                hasValidNormalizedTime = true;
                return;
            }

            if (currentNode == null || !currentNode.HasTag(CharacterStateTag.Action))
                return;

            ActionAnimationPlaybackProgress actionProgress = animation.ActionProgress;
            if (!binding.HasKey || !actionProgress.HasValidPlayback || actionProgress.Key != binding.Key)
                return;

            normalizedTime = actionProgress.NormalizedTime;
            hasValidNormalizedTime = true;
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
