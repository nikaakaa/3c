using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public struct CharacterFrameContext
    {
        public CharacterFrameContext(CharacterFrameInput input)
        {
            Input = input;
            CurrentStep = CharacterFramePipelineStep.None;
            LocomotionDecision = default;
            HasLocomotionDecision = false;
            CurrentTimelineFactsTrace = StateTimelineFactsTrace.None;
            RequestSubmissions = CharacterFrameRequestSubmissionSet.Empty;
            StateDecision = default;
            LocomotionFrame = default;
            StateFrame = default;
            ActionMotionResult = ActionMotionResolveResult.None(input.Step);
            FrameSubmission = CharacterFrameSubmission.None(input.Step);
            FramePlan = CharacterFramePlan.None(input.Step);
            Output = default;
            InputRequest = CharacterInputRequestFact.None(InputRequestKind.Dodge);
            ActionDecision = ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
            ResolvedAction = default;
            PreviousStateSnapshot = CharacterStateMachineSnapshot.Inactive;
            ExitedToLocomotion = false;
            InputRequestConsumed = false;
            ActionMovementExecuted = false;
            BasicMovementExecuted = false;
            ActionAnimationPresented = false;
            LocomotionAnimationPresented = false;
            AnimationFactsWritten = false;
            SnapshotEventsReady = false;
            Success = false;
            FailureReason = string.Empty;
        }

        public CharacterFrameInput Input { get; private set; }
        public CharacterFramePipelineStep CurrentStep { get; private set; }
        public LocomotionDecisionFrame LocomotionDecision { get; private set; }
        public bool HasLocomotionDecision { get; private set; }
        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; private set; }
        public StateTimelineWindowFacts CurrentTimelineFacts => CurrentTimelineFactsTrace.Facts;
        public CharacterFrameRequestSubmissionSet RequestSubmissions { get; private set; }
        public LocomotionStateDecisionFrame StateDecision { get; private set; }
        public BasicLocomotionFrame LocomotionFrame { get; private set; }
        public CharacterStateMachineFrame StateFrame { get; private set; }
        public ActionMotionResolveResult ActionMotionResult { get; private set; }
        public CharacterFrameSubmission FrameSubmission { get; private set; }
        public CharacterFramePlan FramePlan { get; private set; }
        public CharacterFrameOutput Output { get; private set; }
        public CharacterInputRequestFact InputRequest { get; private set; }
        public ActionInterruptDecision ActionDecision { get; private set; }
        public CharacterResolvedAction ResolvedAction { get; private set; }
        public CharacterStateMachineSnapshot PreviousStateSnapshot { get; private set; }
        public bool ExitedToLocomotion { get; private set; }
        public bool InputRequestConsumed { get; private set; }
        public bool ActionMovementExecuted { get; private set; }
        public bool BasicMovementExecuted { get; private set; }
        public bool ActionAnimationPresented { get; private set; }
        public bool LocomotionAnimationPresented { get; private set; }
        public bool AnimationFactsWritten { get; private set; }
        public bool SnapshotEventsReady { get; private set; }
        public bool Success { get; private set; }
        public string FailureReason { get; private set; }
        public int Step => Input.Step;

        internal void MarkStep(CharacterFramePipelineStep step)
        {
            CurrentStep = step;
        }

        internal void SetLocomotionDecision(in LocomotionDecisionFrame decision)
        {
            LocomotionDecision = decision;
            HasLocomotionDecision = true;
        }

        internal void SetCurrentTimelineFacts(StateTimelineFactsTrace trace)
        {
            CurrentTimelineFactsTrace = trace;
        }

        internal void SetInputRequest(
            in CharacterInputRequestFact request,
            in ActionInterruptDecision decision,
            CharacterFrameRequestSubmissionSet requestSubmissions)
        {
            SetInputRequest(in request, in decision, requestSubmissions, default);
        }

        internal void SetInputRequest(
            in CharacterInputRequestFact request,
            in ActionInterruptDecision decision,
            CharacterFrameRequestSubmissionSet requestSubmissions,
            in CharacterResolvedAction resolvedAction)
        {
            InputRequest = request;
            ActionDecision = decision;
            RequestSubmissions = requestSubmissions;
            ResolvedAction = resolvedAction;
        }

        internal void SetStateDecision(
            in LocomotionStateDecisionFrame decision,
            in CharacterStateMachineSnapshot previousSnapshot,
            bool previousActionCapabilityState)
        {
            StateDecision = decision;
            StateFrame = decision.StateFrame;
            PreviousStateSnapshot = previousSnapshot;
            bool hasActionState =
                decision.StateFrame.ActionState.IsValid &&
                decision.StateFrame.ActionState != ActionStateIds.None;
            ExitedToLocomotion = previousActionCapabilityState &&
                                  !hasActionState &&
                                  !decision.StateFrame.AnimationRequest.IsActionAnimation;
        }

        internal void SetLocomotionFrame(in BasicLocomotionFrame frame, in CharacterStateMachineFrame stateFrame)
        {
            LocomotionFrame = frame;
            StateFrame = stateFrame;
        }

        internal void SetActionMotionResult(in ActionMotionResolveResult result)
        {
            ActionMotionResult = result;
        }

        internal void SetFrameSubmission(in CharacterFrameSubmission submission)
        {
            FrameSubmission = submission;
            LocomotionDecision = submission.LocomotionDecision;
            HasLocomotionDecision = true;
            StateDecision = submission.StateDecision;
            LocomotionFrame = submission.LocomotionFrame;
            StateFrame = submission.StateFrame;
            ActionMotionResult = submission.ActionMotionResult;
            InputRequest = submission.InputRequest;
            ActionDecision = submission.ActionDecision;
            ResolvedAction = default;
            CurrentTimelineFactsTrace = submission.CurrentTimelineFactsTrace;
            PreviousStateSnapshot = submission.PreviousStateSnapshot;
            ExitedToLocomotion = submission.ExitedToLocomotion;
        }

        internal void SetOutput(in CharacterFrameOutput output)
        {
            Output = output;
            FramePlan = output.Plan;
        }

        internal void MarkInputRequestConsumed()
        {
            InputRequestConsumed = true;
        }

        internal void MarkMotionExecuted(bool actionMovementExecuted, bool basicMovementExecuted)
        {
            ActionMovementExecuted = actionMovementExecuted;
            BasicMovementExecuted = basicMovementExecuted;
        }

        internal void MarkPresentation(bool actionAnimationPresented, bool locomotionAnimationPresented, bool animationFactsWritten)
        {
            ActionAnimationPresented = actionAnimationPresented;
            LocomotionAnimationPresented = locomotionAnimationPresented;
            AnimationFactsWritten = animationFactsWritten;
        }

        internal void MarkSnapshotEventsReady()
        {
            SnapshotEventsReady = true;
        }

        internal void MarkCompleted()
        {
            CurrentStep = CharacterFramePipelineStep.Completed;
            Success = true;
            FailureReason = string.Empty;
        }

        internal void MarkFailed(string reason)
        {
            CurrentStep = CharacterFramePipelineStep.Failed;
            Success = false;
            FailureReason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
        }
    }
}
