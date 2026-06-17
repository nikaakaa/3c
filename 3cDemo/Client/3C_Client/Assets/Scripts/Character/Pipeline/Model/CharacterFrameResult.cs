using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public readonly struct CharacterFrameResult
    {
        public CharacterFrameResult(in CharacterFrameContext context)
        {
            Success = context.Success;
            Step = context.Step;
            CompletedStep = context.CurrentStep;
            LocomotionFrame = context.LocomotionFrame;
            CurrentTimelineFactsTrace = context.CurrentTimelineFactsTrace;
            RequestSubmissions = context.RequestSubmissions;
            StateFrame = context.StateFrame;
            ActionMotionResult = context.ActionMotionResult;
            FrameSubmission = context.FrameSubmission;
            FramePlan = context.FramePlan;
            Output = context.Output;
            InputRequest = context.InputRequest;
            InputRequestConsumed = context.InputRequestConsumed;
            ActionMovementExecuted = context.ActionMovementExecuted;
            BasicMovementExecuted = context.BasicMovementExecuted;
            ActionAnimationPresented = context.ActionAnimationPresented;
            LocomotionAnimationPresented = context.LocomotionAnimationPresented;
            AnimationFactsWritten = context.AnimationFactsWritten;
            SnapshotEventsReady = context.SnapshotEventsReady;
            FailureReason = context.FailureReason;
            DiagnosticSummary = CharacterFrameDiagnosticsSummary.Build(in context);
        }

        public bool Success { get; }
        public int Step { get; }
        public CharacterFramePipelineStep CompletedStep { get; }
        public BasicLocomotionFrame LocomotionFrame { get; }
        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; }
        public StateTimelineWindowFacts CurrentTimelineFacts => CurrentTimelineFactsTrace.Facts;
        public CharacterFrameRequestSubmissionSet RequestSubmissions { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public ActionMotionResolveResult ActionMotionResult { get; }
        public CharacterFrameSubmission FrameSubmission { get; }
        public CharacterFramePlan FramePlan { get; }
        public CharacterFrameOutput Output { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public bool InputRequestConsumed { get; }
        public bool ActionMovementExecuted { get; }
        public bool BasicMovementExecuted { get; }
        public bool ActionAnimationPresented { get; }
        public bool LocomotionAnimationPresented { get; }
        public bool AnimationFactsWritten { get; }
        public bool SnapshotEventsReady { get; }
        public string FailureReason { get; }
        public string DiagnosticSummary { get; }
    }
}
