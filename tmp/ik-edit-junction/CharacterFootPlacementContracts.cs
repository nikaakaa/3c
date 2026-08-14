using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootSide : byte
    {
        Left = 1,
        Right = 2
    }

    public enum CharacterFootContactState : byte
    {
        Swing = 1,
        Contact = 2,
        Anchored = 3
    }

    public enum FootConstraintTransitionReason : byte
    {
        None = 0,
        ContactEntered = 1,
        ContactReleased = 2,
        AnchorCaptured = 3,
        PolicyReleased = 4,
        BodyAirborne = 5,
        SurfaceInvalid = 6,
        AnchorDistanceExceeded = 7,
        LegUnreachable = 8,
        BodyReset = 9,
        PresentationReset = 10,
        MissingAnimationOutput = 11,
        InvalidPose = 12,
        PelvisRangeConflictReleased = 17
    }

    public enum CharacterFootContactDecision : byte
    {
        None = 0,
        WaitingForSurface = 1,
        WaitingForDistance = 2,
        WaitingForCaptureSpeed = 3,
        WaitingForPlantConfidence = 4,
        AnchorFading = 5,
        ContactEntered = 6,
        ContactRetained = 7,
        ContactReleasedSurfaceInvalid = 8,
        ContactReleasedSurfaceDistance = 9,
        ContactReleasedAnimationSpeed = 10,
        ContactReleasedAnchorSurface = 11,
        ContactReleasedAnchorDistance = 12,
        ContactReleasedPelvisConflict = 13,
        ContactReleasedLegUnreachable = 14,
        Reset = 15,
        ContactReleasedAnimationConstraint = 16
    }

    public enum FootPredictionRejectReason : byte
    {
        None = 0,
        ReachExceeded = 1,
        NonFinite = 2,
        NoFutureLanding = 3,
        LandingConfidenceInsufficient = 5,
        NoClearanceRequired = 6,
        NoCommittedPlan = 7,
        LandingEventUnavailable = 8,
        LandingEventNotPreSwing = 9,
        LandingEventIdentityInvalid = 11,
        PlanWaitingForRelease = 12,
        CurrentSupportUnavailable = 13,
        StanceConstraintOwnsFoot = 14,
        MovementDirectionUnavailable = 15,
        FutureLandingNoCandidate = 16,
        FutureLandingHeightDiscontinuity = 17,
        FutureLandingEdgeGap = 18,
        FutureLandingReachExceeded = 19,
        FutureLandingStepExceeded = 20,
        FutureLandingUnsupportedCenter = 21,
        FutureLandingSlopeExceeded = 22,
        FutureLandingInvalidCandidate = 23,
        FootRateInvalid = 24,
        MotionTimelineUnavailable = 25
    }

    public readonly struct CharacterFootPlacementAnimatedFootPose
    {
        public CharacterFootPlacementAnimatedFootPose(
            Vector3 hipPosition,
            Vector3 kneePosition,
            Vector3 anklePosition,
            Quaternion ankleRotation,
            Vector3 toePosition,
            Quaternion toeRotation,
            Vector3 heelPosition,
            Vector3 soleForward,
            Vector3 soleUp,
            Quaternion semanticRotation,
            Quaternion soleFrameLocalRotation)
        {
            HipPosition = hipPosition;
            KneePosition = kneePosition;
            AnklePosition = anklePosition;
            AnkleRotation = ankleRotation;
            ToePosition = toePosition;
            ToeRotation = toeRotation;
            HeelPosition = heelPosition;
            SoleForward = soleForward;
            SoleUp = soleUp;
            SemanticRotation = semanticRotation;
            SoleFrameLocalRotation = soleFrameLocalRotation;
        }

        public Vector3 HipPosition { get; }
        public Vector3 KneePosition { get; }
        public Vector3 AnklePosition { get; }
        public Quaternion AnkleRotation { get; }
        public Vector3 ToePosition { get; }
        public Quaternion ToeRotation { get; }
        public Vector3 HeelPosition { get; }
        public Vector3 SoleForward { get; }
        public Vector3 SoleUp { get; }
        public Quaternion SemanticRotation { get; }
        public Quaternion SoleFrameLocalRotation { get; }

        internal CharacterFootPlacementSoleContactPose ResolveSoleContacts(
            Vector3 anklePosition,
            Quaternion ankleRotation)
        {
            Quaternion rotationDelta = (ankleRotation * Quaternion.Inverse(AnkleRotation)).normalized;
            return new CharacterFootPlacementSoleContactPose(
                anklePosition + rotationDelta * (HeelPosition - AnklePosition),
                anklePosition + rotationDelta * (ToePosition - AnklePosition));
        }
    }

    public readonly struct CharacterFootPlacementSoleContactPose
    {
        internal CharacterFootPlacementSoleContactPose(
            Vector3 heelPosition,
            Vector3 toePosition)
        {
            HeelPosition = heelPosition;
            ToePosition = toePosition;
        }

        public Vector3 HeelPosition { get; }
        public Vector3 ToePosition { get; }
    }

    public readonly struct CharacterFootPlacementAnimatedPose
    {
        public CharacterFootPlacementAnimatedPose(
            ulong renderFrame,
            Vector3 pelvisLocalPosition,
            CharacterFootPlacementAnimatedFootPose left,
            CharacterFootPlacementAnimatedFootPose right)
        {
            RenderFrame = renderFrame;
            PelvisLocalPosition = pelvisLocalPosition;
            Left = left;
            Right = right;
        }

        public ulong RenderFrame { get; }
        public Vector3 PelvisLocalPosition { get; }
        public CharacterFootPlacementAnimatedFootPose Left { get; }
        public CharacterFootPlacementAnimatedFootPose Right { get; }
    }

}
