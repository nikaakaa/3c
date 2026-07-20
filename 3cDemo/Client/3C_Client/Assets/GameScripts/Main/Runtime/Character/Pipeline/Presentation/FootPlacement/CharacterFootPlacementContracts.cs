using System;
using ThirdPersonSimulation;
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
        ContactReleased = 13
    }

    public enum FootPredictionRejectReason : byte
    {
        None = 0,
        NoSupportEstimate = 1,
        AngularVelocityExceeded = 2,
        DistanceExceeded = 3,
        ReachExceeded = 4,
        NonFinite = 5
    }

    public enum FootPlacementSupportFoot : byte
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = 3
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
            Vector3 soleForward)
        {
            HipPosition = hipPosition;
            KneePosition = kneePosition;
            AnklePosition = anklePosition;
            AnkleRotation = ankleRotation;
            ToePosition = toePosition;
            ToeRotation = toeRotation;
            HeelPosition = heelPosition;
            SoleForward = soleForward;
        }

        public Vector3 HipPosition { get; }
        public Vector3 KneePosition { get; }
        public Vector3 AnklePosition { get; }
        public Quaternion AnkleRotation { get; }
        public Vector3 ToePosition { get; }
        public Quaternion ToeRotation { get; }
        public Vector3 HeelPosition { get; }
        public Vector3 SoleForward { get; }
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

    public readonly struct FootPlacementFootPlan
    {
        public FootPlacementFootPlan(
            CharacterFootSide side,
            Vector3 position,
            Quaternion rotation,
            float positionWeight,
            float rotationWeight,
            FootConstraintState constraintState,
            FootConstraintTransitionReason transitionReason)
        {
            Side = side;
            Position = position;
            Rotation = rotation;
            PositionWeight = Mathf.Clamp01(positionWeight);
            RotationWeight = Mathf.Clamp01(rotationWeight);
            ConstraintState = constraintState;
            TransitionReason = transitionReason;
        }

        public CharacterFootSide Side { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public FootConstraintState ConstraintState { get; }
        public FootConstraintTransitionReason TransitionReason { get; }
    }

    public readonly struct CharacterFootPlacementPlan
    {
        public CharacterFootPlacementPlan(
            ActorId actorId,
            ulong renderFrame,
            ulong resetSequence,
            FootPlacementFootPlan left,
            FootPlacementFootPlan right,
            float pelvisLocalVerticalOffset)
        {
            ActorId = actorId;
            RenderFrame = renderFrame;
            ResetSequence = resetSequence;
            Left = left;
            Right = right;
            PelvisLocalVerticalOffset = pelvisLocalVerticalOffset;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong ResetSequence { get; }
        public FootPlacementFootPlan Left { get; }
        public FootPlacementFootPlan Right { get; }
        public float PelvisLocalVerticalOffset { get; }
        public bool IsValid => ActorId.IsValid && RenderFrame != 0 &&
                               IsFinite(Left.Position) && IsFinite(Right.Position) &&
                               IsFinite(Left.Rotation) && IsFinite(Right.Rotation) &&
                               IsFinite(PelvisLocalVerticalOffset);

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }

    public readonly struct CharacterFootPlacementSolverContext
    {
        public CharacterFootPlacementSolverContext(
            ActorId actorId,
            CharacterFootPlacementRigBinding rig)
        {
            ActorId = actorId;
            Rig = rig ?? throw new ArgumentNullException(nameof(rig));
        }

        public ActorId ActorId { get; }
        public CharacterFootPlacementRigBinding Rig { get; }
    }

    public readonly struct CharacterFootPlacementSolverReset
    {
        public CharacterFootPlacementSolverReset(
            ulong renderFrame,
            ulong resetSequence,
            FootConstraintTransitionReason reason)
        {
            RenderFrame = renderFrame;
            ResetSequence = resetSequence;
            Reason = reason;
        }

        public ulong RenderFrame { get; }
        public ulong ResetSequence { get; }
        public FootConstraintTransitionReason Reason { get; }
    }

    public readonly struct CharacterFootPlacementSolverResult
    {
        public CharacterFootPlacementSolverResult(
            ulong renderFrame,
            bool applied,
            bool duplicateRejected,
            string detail)
        {
            RenderFrame = renderFrame;
            Applied = applied;
            DuplicateRejected = duplicateRejected;
            Detail = detail ?? string.Empty;
        }

        public ulong RenderFrame { get; }
        public bool Applied { get; }
        public bool DuplicateRejected { get; }
        public string Detail { get; }
    }

    public interface ICharacterFootPlacementSolver : IDisposable
    {
        bool IsInitialized { get; }
        void RequireValid(CharacterFootPlacementRigBinding rig);
        void Initialize(CharacterFootPlacementSolverContext context);
        CharacterFootPlacementAnimatedPose CaptureAnimatedPose(ulong renderFrame);
        CharacterFootPlacementSolverResult Apply(CharacterFootPlacementPlan plan);
        void ResetPose(CharacterFootPlacementSolverReset reset);
    }
}
