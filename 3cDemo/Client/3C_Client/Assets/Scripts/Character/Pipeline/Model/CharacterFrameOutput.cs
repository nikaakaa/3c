using System;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct CharacterFrameOutput
    {
        public CharacterFrameOutput(CharacterFramePlan plan)
            : this(CharacterFrameSubmission.None(plan.SourceStep), plan)
        {
        }

        public CharacterFrameOutput(
            CharacterFrameSubmission submission,
            CharacterFramePlan plan)
        {
            Plan = plan;
            Submission = submission;
        }

        public CharacterFramePlan Plan { get; }
        public CharacterFrameSubmission Submission { get; }
        public CharacterFrameMovementSubmission Movement
        {
            get
            {
                CharacterFrameMovementSubmission movement = Submission.Movement;
                return ResolveMovement(in movement, Plan);
            }
        }

        public CharacterFrameAnimationSubmission Animation
        {
            get
            {
                CharacterFrameAnimationSubmission animation = Submission.Animation;
                return ResolveAnimation(in animation, Plan);
            }
        }
        public CharacterFrameInputConsumeSubmission InputConsume => Submission.InputConsume;
        public CharacterFrameRuntimeFactsSubmission RuntimeFacts => Submission.RuntimeFacts.WithLocomotionPreemption(Plan.LocomotionPreemption);
        public CharacterFrameDiagnosticsSubmission Diagnostics => Submission.Diagnostics;
        public CharacterFrameSnapshotEventsSubmission SnapshotEvents => Submission.SnapshotEvents;
        public bool HasSubmission => Submission.HasFrameOutput;
        public CharacterStateMachineFrame StateFrame => Submission.StateFrame;
        public BasicLocomotionFrame LocomotionFrame => Submission.LocomotionFrame;
        public ActionMotionResolveResult ActionMotionResult => Submission.ActionMotionResult;
        public LocomotionPreemptionFact LocomotionPreemption => Plan.LocomotionPreemption;
        public bool ExitedToLocomotion => Submission.ExitedToLocomotion;

        static CharacterFrameMovementSubmission ResolveMovement(
            in CharacterFrameMovementSubmission movement,
            CharacterFramePlan plan)
        {
            return new CharacterFrameMovementSubmission(
                movement.LocomotionFrame,
                movement.ActionMotionResult,
                movement.ExecuteBasicMovement && !plan.SuppressesLocomotionMotion,
                movement.ExecuteActionMovement);
        }

        static CharacterFrameAnimationSubmission ResolveAnimation(
            in CharacterFrameAnimationSubmission animation,
            CharacterFramePlan plan)
        {
            return new CharacterFrameAnimationSubmission(
                animation.AnimationRequest,
                animation.HasAnimationRequest,
                animation.PresentLocomotionAnimation && !plan.SuppressesLocomotionAnimation,
                animation.ExitedToLocomotion);
        }
    }
}
