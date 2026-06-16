using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class FullBodySubmissionBuilder : ICharacterFrameRequestSubmitter, ICharacterFrameOutputSubmitter
    {
        public bool TrySubmitFrameRequests(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context)
        {
            return TryBuildStateSubmission(runtime, ref context);
        }

        public bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission)
        {
            return TryBuildFrameSubmission(runtime, ref context, out submission);
        }

        public bool TryBuildStateSubmission(
            IFullBodySubmissionRuntimePort runtime,
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

            CharacterFrameInput input = context.Input;
            BasicLocomotionInputSnapshot locomotionInput = input.LocomotionInput;
            if (!locomotion.TryPrepareDecisionFrame(
                    in locomotionInput,
                    stateMachine,
                    context.Step,
                    out LocomotionDecisionFrame locomotionDecision))
            {
                context.MarkFailed("locomotion-facts-not-ready");
                return false;
            }

            context.SetLocomotionDecision(in locomotionDecision);
            StateTimelineFactsTrace currentTimelineFacts = PrepareCurrentTimelineFacts(runtime, locomotion, in context, in locomotionDecision);
            context.SetCurrentTimelineFacts(currentTimelineFacts);
            CharacterActionRequestSubmissionResult requestSubmission = ResolveActionRequest(runtime, locomotion, in context, in locomotionDecision);
            context.SetInputRequest(requestSubmission.Request, requestSubmission.Decision, requestSubmission.RequestSubmissions);
            CharacterInputRequestFact inputRequest = requestSubmission.Request;
            CharacterStateMachineSnapshot previousSnapshot = runtime.CurrentStateSnapshot;
            if (!locomotion.TryEvaluatePreparedGameplayDecision(
                    in locomotionDecision,
                    stateMachine,
                    in inputRequest,
                    context.CurrentTimelineFacts,
                    context.Step,
                    out LocomotionStateDecisionFrame stateDecision))
            {
                context.MarkFailed("state-decision-failed");
                return false;
            }

            bool previousActionCapabilityState =
                stateMachine.Definition != null &&
                stateMachine.Definition.TryGetNode(previousSnapshot.ActiveState, out CharacterStateNodeDefinition previousNode) &&
                previousNode.IsActionCapabilityState;
            context.SetStateDecision(in stateDecision, in previousSnapshot, previousActionCapabilityState);
            FullBodyDiagnostics.LogTimelineFactsTrace(context.CurrentTimelineFactsTrace);
            FullBodyDiagnostics.LogTimelineFactsTrace(stateDecision.StateFrame.ProjectedTimelineFactsTrace);
            FullBodyDiagnostics.LogTimelineFactsTrace(stateDecision.StateFrame.TargetTimelineFactsTrace);
            return true;
        }

        public bool TryBuildFrameSubmission(
            IFullBodySubmissionRuntimePort runtime,
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
            if (!locomotion.TryBuildMotionFromStateDecision(
                    in stateDecision,
                    context.Step,
                    out BasicLocomotionFrame locomotionFrame,
                    out CharacterStateMachineFrame stateFrame))
            {
                submission = CharacterFrameSubmission.None(context.Step);
                context.MarkFailed("motion-build-failed");
                return false;
            }

            CharacterRuntimeActionFacts previousActionFacts = locomotion.RuntimeBlackboardSnapshot.Action;
            bool hasDodgeConfig = runtime.TryResolveDodgeActionConfig(out DodgeActionConfig dodgeConfig);
            ActionMotionSpec actionMotionSpec = DodgeActionMotionSpecAdapter.Resolve(
                stateFrame.ActionMotionSpec,
                hasDodgeConfig,
                in dodgeConfig);
            ActionMotionResolveInput actionMotionInput = new ActionMotionResolveInput(
                actionMotionSpec,
                context.Input.DeltaTime,
                stateFrame.TimelineFacts,
                previousActionFacts);
            ActionMotionResolveResult actionMotionResult = ActionMotionResolver.Resolve(in actionMotionInput);

            submission = new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.FullBody,
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
                context.ExitedToLocomotion);
            context.SetFrameSubmission(in submission);
            return true;
        }

        CharacterActionRequestSubmissionResult ResolveActionRequest(
            IFullBodySubmissionRuntimePort runtime,
            ILocomotionFrameRuntimePort locomotion,
            in CharacterFrameContext context,
            in LocomotionDecisionFrame locomotionDecision)
        {
            bool hasDodgeConfig = runtime.TryResolveDodgeActionConfig(out DodgeActionConfig config);
            FullBodyActionRequestSubmissionResolverInput input = new FullBodyActionRequestSubmissionResolverInput(
                runtime.InputRequestBuffer,
                context.Step,
                context.Input.DeltaTime,
                runtime.CurrentStateSnapshot,
                context.Input.LocomotionInput,
                locomotion != null && locomotion.RunLatchActive,
                locomotionDecision.Facts,
                context.CurrentTimelineFacts,
                hasDodgeConfig,
                config,
                runtime.ResolveCurrentActionResistance(),
                runtime.ResolveInterruptPolicies(),
                context.Input.ExternalRequestSubmission);

            return FullBodyActionRequestSubmissionResolver.Resolve(in input);
        }

        StateTimelineFactsTrace PrepareCurrentTimelineFacts(
            IFullBodySubmissionRuntimePort runtime,
            ILocomotionFrameRuntimePort locomotion,
            in CharacterFrameContext context,
            in LocomotionDecisionFrame locomotionDecision)
        {
            CharacterStateMachineRunner stateMachine = runtime != null ? runtime.StateMachine : null;
            CharacterStateMachineDefinition definition = stateMachine != null ? stateMachine.Definition : null;
            CharacterStateMachineSnapshot snapshot = runtime != null ? runtime.CurrentStateSnapshot : CharacterStateMachineSnapshot.Inactive;
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard = locomotion != null
                ? locomotion.RuntimeBlackboardSnapshot
                : default;
            CharacterInputRequestFact emptyRequest = CharacterInputRequestFact.None(InputRequestKind.Dodge);
            LocomotionDecisionFacts facts = locomotionDecision.Facts;
            CharacterStateMachineContext samplerContext = new CharacterStateMachineContext(
                context.Input.DeltaTime,
                context.Step,
                in facts,
                emptyRequest,
                runtimeBlackboard);
            StateTimelineWindowFacts timelineFacts = CharacterStateTimelineFactSampler.SampleCurrent(
                definition,
                snapshot,
                in samplerContext,
                snapshot.StateTime,
                ActionRequestType.None);
            return StateTimelineFactsTrace.Current(timelineFacts, context.Step, ActionRequestType.None);
        }
    }
}
