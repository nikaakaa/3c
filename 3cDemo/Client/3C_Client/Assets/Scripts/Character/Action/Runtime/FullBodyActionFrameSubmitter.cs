using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class FullBodyActionFrameSubmitter : ICharacterFrameRequestSubmitter, ICharacterFrameOutputSubmitter
    {
        public bool TrySubmitFrameRequests(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context)
        {
            if (runtime == null || !runtime.PrepareFrameRuntimeAdapters())
            {
                context.MarkFailed("runtime-not-ready");
                return false;
            }

            ILocomotionFrameRuntimePort locomotion = runtime.LocomotionFrameRuntime;
            CharacterStateMachineRunner stateMachine = runtime.StateMachine;
            if (locomotion == null || stateMachine == null)
            {
                context.MarkFailed("state-machine-or-locomotion-missing");
                return false;
            }

            LocomotionDecisionFrame locomotionDecision = context.LocomotionDecision;
            CharacterActionRequestSubmissionResult requestSubmission = ResolveActionRequest(runtime, locomotion, in context, in locomotionDecision);
            context.SetInputRequest(
                requestSubmission.Request,
                requestSubmission.Decision,
                requestSubmission.RequestSubmissions,
                requestSubmission.ResolvedAction);
            return true;
        }

        public bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission)
        {
            ILocomotionFrameRuntimePort locomotion = runtime != null ? runtime.LocomotionFrameRuntime : null;
            if (runtime == null || locomotion == null || !context.StateDecision.HasStateFrame)
            {
                submission = CharacterFrameSubmission.None(context.Step);
                context.MarkFailed("motion-build-prerequisite-missing");
                return false;
            }

            LocomotionStateDecisionFrame stateDecision = context.StateDecision;
            BasicLocomotionFrame locomotionFrame = context.LocomotionFrame;
            CharacterStateMachineFrame stateFrame = context.StateFrame;

            CharacterRuntimeActionFacts previousActionFacts = locomotion.RuntimeBlackboardSnapshot.Action;
            runtime.TryResolveActionCatalog(out CharacterActionCatalog actionCatalog);
            ActionLifecycleFrame actionLifecycle = runtime.TickActionLifecycle(
                context.ResolvedAction,
                in actionCatalog,
                context.Input.DeltaTime,
                context.Step);
            ActionMotionSpec actionMotionSpec = actionLifecycle.MotionSpec;
            ActionMotionResolveInput actionMotionInput = new ActionMotionResolveInput(
                actionMotionSpec,
                context.Input.DeltaTime,
                stateFrame.TimelineFacts,
                previousActionFacts,
                context.LocomotionDecision.Facts.HasMoveIntent);
            ActionMotionResolveResult actionMotionResult = ActionMotionResolver.Resolve(in actionMotionInput);
            bool requireAnimationEnded = RequiresActionAnimationEndBeforeLifecycleExit(
                in actionLifecycle,
                in actionMotionResult,
                context.LocomotionDecision.Facts.HasMoveIntent);
            runtime.CompleteActionLifecycle(in actionMotionResult, requireAnimationEnded);
            if (!TryBuildArbitrationInput(
                    runtime,
                    in stateFrame,
                    in actionLifecycle,
                    in actionMotionResult,
                    context.Step,
                    out CharacterFrameArbitrationInput arbitrationInput,
                    out string failureReason))
            {
                submission = CharacterFrameSubmission.None(context.Step);
                context.MarkFailed(failureReason);
                return false;
            }

            CharacterInputRequestFact inputRequest = context.InputRequest;
            CharacterFrameActionOutputSubmission actionOutput = BuildActionOutput(
                in actionLifecycle,
                in inputRequest,
                context.Step);
            LocomotionPreemptionFact locomotionPreemption = BuildLocomotionPreemptionCandidate(
                in stateFrame,
                in actionLifecycle,
                in arbitrationInput,
                context.Step);
            submission = new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.CharacterRuntimeGraph,
                context.Step,
                context.LocomotionDecision,
                stateDecision,
                locomotionFrame,
                stateFrame,
                actionMotionResult,
                context.InputRequest,
                context.ActionDecision,
                context.CurrentTimelineFactsTrace,
                context.PreviousStateSnapshot,
                context.ExitedToLocomotion || actionLifecycle.ExitedThisFrame,
                actionOutput,
                arbitrationInput,
                locomotionPreemption);
            context.SetFrameSubmission(in submission);
            return true;
        }

        static bool RequiresActionAnimationEndBeforeLifecycleExit(
            in ActionLifecycleFrame actionLifecycle,
            in ActionMotionResolveResult actionMotionResult,
            bool hasMoveIntentAtCompletion)
        {
            return actionLifecycle.HasAnimationRequest &&
                actionMotionResult.ActionCompleted &&
                !actionMotionResult.SetRunLatch &&
                !hasMoveIntentAtCompletion;
        }

        static bool TryBuildArbitrationInput(
            ICharacterFrameSubmissionRuntimePort runtime,
            in CharacterStateMachineFrame stateFrame,
            in ActionLifecycleFrame actionLifecycle,
            in ActionMotionResolveResult actionMotionResult,
            int sourceStep,
            out CharacterFrameArbitrationInput input,
            out string failureReason)
        {
            bool hasActionLifecycle =
                actionLifecycle.HasAction ||
                actionMotionResult.HasSpec ||
                actionMotionResult.HasActionMovement ||
                actionLifecycle.HasAnimationRequest ||
                actionLifecycle.ExitedThisFrame;
            CharacterFrameCandidateOutput locomotionCandidate = CharacterFrameCandidateOutput.Locomotion(
                stateFrame.ExecuteBasicMovement,
                stateFrame.PresentLocomotionAnimation,
                sourceStep);
            CharacterFrameCandidateOutput actionCandidate = CharacterFrameCandidateOutput.FullBodyAction(
                actionMotionResult.HasActionMovement,
                actionLifecycle.HasAnimationRequest,
                sourceStep);
            BodyOccupancyClaim claim = BodyOccupancyClaim.None(sourceStep);
            if (actionLifecycle.HasAction || actionMotionResult.HasSpec || actionMotionResult.HasActionMovement || actionLifecycle.HasAnimationRequest)
            {
                if (runtime == null || !runtime.TryResolveBodyClaimPolicy(out BodyClaimPolicy policy))
                {
                    input = CharacterFrameArbitrationInput.None(sourceStep);
                    failureReason = "body-claim-policy-missing";
                    return false;
                }

                ActionStateId actionState = actionMotionResult.HasSpec
                    ? actionMotionResult.Spec.ActionState
                    : actionLifecycle.ActionState;
                if (!policy.TryResolveClaim(actionState, sourceStep, out claim))
                {
                    input = CharacterFrameArbitrationInput.None(sourceStep);
                    failureReason = "body-claim-policy-action-missing";
                    return false;
                }
            }

            input = new CharacterFrameArbitrationInput(
                claim,
                locomotionCandidate,
                actionCandidate,
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, sourceStep),
                sourceStep);
            failureReason = string.Empty;
            return true;
        }

        static CharacterFrameActionOutputSubmission BuildActionOutput(
            in ActionLifecycleFrame actionLifecycle,
            in CharacterInputRequestFact inputRequest,
            int step)
        {
            bool consumeInputRequest =
                actionLifecycle.StartedThisFrame &&
                inputRequest.HasRequest &&
                inputRequest.RequestKind == InputRequestKind.Dodge;
            return new CharacterFrameActionOutputSubmission(
                actionLifecycle.AnimationRequest,
                actionLifecycle.HasAnimationRequest,
                consumeInputRequest,
                consumeInputRequest ? inputRequest.RequestKind : default,
                actionLifecycle.ExitedThisFrame,
                step,
                actionLifecycle.ActionBranchOutcome);
        }

        static LocomotionPreemptionFact BuildLocomotionPreemptionCandidate(
            in CharacterStateMachineFrame stateFrame,
            in ActionLifecycleFrame actionLifecycle,
            in CharacterFrameArbitrationInput arbitrationInput,
            int sourceStep)
        {
            if (!actionLifecycle.StartedThisFrame ||
                !arbitrationInput.OccupancyClaim.ClaimsFullBody ||
                !actionLifecycle.ActionState.IsValid ||
                actionLifecycle.ActionState == ActionStateIds.None ||
                stateFrame.LocomotionPhase != BasicMovementPhase.TurnBack ||
                stateFrame.Snapshot.ActiveState != CharacterStateIds.TurnBack)
            {
                return LocomotionPreemptionFact.None;
            }

            return LocomotionPreemptionFact.FullBodyActionStarted(
                stateFrame.Snapshot.ActiveState,
                actionLifecycle.ActionState,
                sourceStep);
        }

        static CharacterActionRequestSubmissionResult ResolveActionRequest(
            ICharacterFrameSubmissionRuntimePort runtime,
            ILocomotionFrameRuntimePort locomotion,
            in CharacterFrameContext context,
            in LocomotionDecisionFrame locomotionDecision)
        {
            bool hasActionCatalog = runtime.TryResolveActionCatalog(out CharacterActionCatalog actionCatalog);
            FullBodyActionRequestSubmissionResolverInput input = new FullBodyActionRequestSubmissionResolverInput(
                runtime.InputRequestBuffer,
                context.Step,
                context.Input.DeltaTime,
                runtime.CurrentStateSnapshot,
                context.Input.LocomotionInput,
                locomotion != null && locomotion.RunLatchActive,
                locomotionDecision.Facts,
                context.CurrentTimelineFacts,
                hasActionCatalog,
                actionCatalog,
                runtime.ResolveCurrentActionResistance(),
                runtime.ResolveInterruptPolicies(),
                context.Input.ExternalRequestSubmission);

            return FullBodyActionRequestSubmissionResolver.Resolve(in input);
        }
    }
}
