using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class LocomotionFrameSubmitter : ICharacterFrameRequestSubmitter, ICharacterFrameOutputSubmitter
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
            return true;
        }

        public bool TrySubmitFrameOutput(
            ICharacterFrameRuntimePort runtime,
            ref CharacterFrameContext context,
            out CharacterFrameSubmission submission)
        {
            submission = CharacterFrameSubmission.None(context.Step);
            if (runtime == null || !runtime.PrepareFrameRuntimeAdapters())
            {
                context.MarkFailed("runtime-not-ready");
                return false;
            }

            ILocomotionFrameRuntimePort locomotion = runtime.LocomotionFrameRuntime;
            CharacterStateMachineRunner stateMachine = runtime.StateMachine;
            if (locomotion == null || stateMachine == null || !context.HasLocomotionDecision)
            {
                context.MarkFailed("locomotion-output-prerequisite-missing");
                return false;
            }

            LocomotionDecisionFrame locomotionDecision = context.LocomotionDecision;
            CharacterStateMachineSnapshot previousSnapshot = runtime.CurrentStateSnapshot;
            if (!locomotion.TryEvaluatePreparedGameplayDecision(
                    in locomotionDecision,
                    stateMachine,
                    context.InputRequest,
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

            if (!locomotion.TryBuildMotionFromStateDecision(
                    in stateDecision,
                    context.Step,
                    out BasicLocomotionFrame locomotionFrame,
                    out CharacterStateMachineFrame stateFrame))
            {
                context.MarkFailed("motion-build-failed");
                return false;
            }

            context.SetLocomotionFrame(in locomotionFrame, in stateFrame);
            return false;
        }

        static StateTimelineFactsTrace PrepareCurrentTimelineFacts(
            ICharacterFrameSubmissionRuntimePort runtime,
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
