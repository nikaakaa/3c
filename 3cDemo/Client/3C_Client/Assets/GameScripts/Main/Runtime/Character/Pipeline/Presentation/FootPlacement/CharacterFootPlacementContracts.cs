using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootSide : byte
    {
        Left = 1,
        Right = 2
    }

    public enum FootConstraintState : byte
    {
        Free = 1,
        Locked = 2,
        Sliding = 3
    }

    public enum CharacterFootPlantLockType : byte
    {
        Unlocked = 1,
        PivotAroundToe = 2,
        PivotAroundAnkle = 3,
        LockRotation = 4
    }

    public enum CharacterFootPlacementPelvisHeightMode : byte
    {
        AllLegs = 1,
        AllPlantedFeet = 2,
        DirectionalSlopeSupport = 3
    }

    public enum CharacterFootPlacementActorMovementCompensationMode : byte
    {
        FollowBody = 1,
        HoldWorldDuringInterpolation = 2
    }

    public enum FootConstraintTransitionReason : byte
    {
        None = 0,
        ContactCommitted = 1,
        AnimationDrift = 2,
        SlideSettled = 3,
        PolicyReleased = 4,
        BodyAirborne = 5,
        SurfaceInvalid = 6,
        ReplantThresholdExceeded = 7,
        LegUnreachable = 8,
        BodyReset = 9,
        PresentationReset = 10,
        MissingAnimationOutput = 11,
        InvalidPose = 12,
        ContactReleased = 13,
        LegCompressed = 14,
        AnkleTwistExceeded = 15,
        FootSeparationReleased = 16,
        PelvisRangeConflictReleased = 17
    }

    public enum FootPredictionRejectReason : byte
    {
        None = 0,
        NoSupportEstimate = 1,
        AngularVelocityExceeded = 2,
        DistanceExceeded = 3,
        ReachExceeded = 4,
        NonFinite = 5,
        NoFutureLanding = 6
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
            Quaternion semanticRotation)
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
