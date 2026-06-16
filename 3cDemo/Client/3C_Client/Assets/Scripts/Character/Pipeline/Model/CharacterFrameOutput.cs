using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct CharacterFrameOutput
    {
        public CharacterFrameOutput(CharacterFrameSubmission submission)
        {
            Submission = submission;
        }

        public CharacterFrameSubmission Submission { get; }
        public CharacterFrameMovementSubmission Movement => Submission.Movement;
        public CharacterFrameAnimationSubmission Animation => Submission.Animation;
        public CharacterFrameInputConsumeSubmission InputConsume => Submission.InputConsume;
        public CharacterFrameRuntimeFactsSubmission RuntimeFacts => Submission.RuntimeFacts;
        public CharacterFrameDiagnosticsSubmission Diagnostics => Submission.Diagnostics;
        public CharacterFrameSnapshotEventsSubmission SnapshotEvents => Submission.SnapshotEvents;
        public bool HasSubmission => Submission.HasFrameOutput;
        public CharacterStateMachineFrame StateFrame => Submission.StateFrame;
        public BasicLocomotionFrame LocomotionFrame => Submission.LocomotionFrame;
        public ActionMotionResolveResult ActionMotionResult => Submission.ActionMotionResult;
        public bool ExitedToLocomotion => Submission.ExitedToLocomotion;
    }
}
