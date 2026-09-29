using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootSide : byte
    {
        Left = 1,
        Right = 2
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
            Quaternion ankleRotation) =>
            CharacterFootPlacementSoleContactPose.Resolve(
                AnklePosition,
                AnkleRotation,
                HeelPosition,
                ToePosition,
                anklePosition,
                ankleRotation);
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

        public static CharacterFootPlacementSoleContactPose Resolve(
            Vector3 sourceAnklePosition,
            Quaternion sourceAnkleRotation,
            Vector3 sourceHeelPosition,
            Vector3 sourceToePosition,
            Vector3 finalAnklePosition,
            Quaternion finalAnkleRotation)
        {
            Quaternion rotationDelta =
                (finalAnkleRotation * Quaternion.Inverse(sourceAnkleRotation)).normalized;
            return new CharacterFootPlacementSoleContactPose(
                finalAnklePosition +
                rotationDelta * (sourceHeelPosition - sourceAnklePosition),
                finalAnklePosition +
                rotationDelta * (sourceToePosition - sourceAnklePosition));
        }
    }

    public readonly struct CharacterFootPlacementAnimatedPose
    {
        readonly Vector3 m_PelvisLocalPosition;
        readonly CharacterFootPlacementAnimatedFootPose m_Left;
        readonly CharacterFootPlacementAnimatedFootPose m_Right;

        public CharacterFootPlacementAnimatedPose(
            ulong renderFrame,
            Vector3 pelvisLocalPosition,
            CharacterFootPlacementAnimatedFootPose left,
            CharacterFootPlacementAnimatedFootPose right)
        {
            RenderFrame = renderFrame;
            m_PelvisLocalPosition = pelvisLocalPosition;
            m_Left = left;
            m_Right = right;
        }

        public ulong RenderFrame { get; }
        public ref readonly Vector3 PelvisLocalPosition => ref m_PelvisLocalPosition;
        public ref readonly CharacterFootPlacementAnimatedFootPose Left => ref m_Left;
        public ref readonly CharacterFootPlacementAnimatedFootPose Right => ref m_Right;
    }

    public enum CharacterFootConstraintState : byte
    {
        Swing = 0,
        Landing = 1,
        Locked = 2,
        Releasing = 3,
        UnlockedSupport = 4
    }

    public enum CharacterFootLockResponse : byte
    {
        None = 0,
        FullAnchor = 1,
        Sliding = 2
    }

    public enum CharacterFootSupportEligibility : byte
    {
        None = 0,
        RetainOnly = 1,
        AcquireAndRetain = 2
    }

    public enum CharacterFootResolvedOutcome : byte
    {
        Ready = 1,
        CurrentSupportUnavailable = 2,
        SupportTargetUnavailable = 3,
        RotationProjectionUnavailable = 4
    }

    internal readonly struct CharacterFootContactReference
    {
        internal CharacterFootContactReference(
            ulong eventIdentity,
            Vector3 point)
        {
            EventIdentity = eventIdentity;
            Point = point;
            IsAvailable = eventIdentity != 0;
        }

        internal bool IsAvailable { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Point { get; }
    }

    internal readonly struct CharacterFootPelvisReachReference
    {
        internal CharacterFootPelvisReachReference(
            ulong eventIdentity,
            Vector3 point)
        {
            EventIdentity = eventIdentity;
            Point = point;
            IsAvailable = eventIdentity != 0;
        }

        internal bool IsAvailable { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Point { get; }
    }

    internal readonly struct CharacterFootLandingReachRequest
    {
        internal CharacterFootLandingReachRequest(
            ulong eventIdentity,
            Vector3 hip,
            Vector3 targetAnkle,
            float legLength,
            float minimumCompressionReserve)
        {
            if (eventIdentity == 0 ||
                !CharacterPoseConstraintMath.IsFinite(hip) ||
                !CharacterPoseConstraintMath.IsFinite(targetAnkle) ||
                !float.IsFinite(legLength) ||
                !float.IsFinite(minimumCompressionReserve) ||
                minimumCompressionReserve <= 0f ||
                legLength <= minimumCompressionReserve)
            {
                throw new ArgumentException(
                    "Landing Reach request is invalid.");
            }
            EventIdentity = eventIdentity;
            Hip = hip;
            TargetAnkle = targetAnkle;
            LegLength = legLength;
            MinimumCompressionReserve = minimumCompressionReserve;
            IsAvailable = eventIdentity != 0;
        }

        internal bool IsAvailable { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Hip { get; }
        internal Vector3 TargetAnkle { get; }
        internal float LegLength { get; }
        internal float MinimumCompressionReserve { get; }
    }

    internal readonly struct CharacterFootPlacementIdentity
    {
        internal CharacterFootPlacementIdentity(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            CharacterFootSide side)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            Side = side;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootSide Side { get; }
    }

    internal readonly struct CharacterFootPlacementPose
    {
        internal CharacterFootPlacementPose(
            Vector3 finalSole,
            Vector3 effectiveSole,
            Vector3 finalAnkle,
            Quaternion finalRotation,
            Vector3 effectiveAnkle,
            Quaternion effectiveRotation,
            Vector3 goalTargetCorrection,
            float goalWeight,
            float rotationWeight)
        {
            FinalSole = finalSole;
            EffectiveSole = effectiveSole;
            FinalAnkle = finalAnkle;
            FinalRotation = finalRotation;
            EffectiveAnkle = effectiveAnkle;
            EffectiveRotation = effectiveRotation;
            GoalTargetCorrection = goalTargetCorrection;
            GoalWeight = goalWeight;
            RotationWeight = rotationWeight;
        }

        internal Vector3 FinalSole { get; }
        internal Vector3 EffectiveSole { get; }
        internal Vector3 FinalAnkle { get; }
        internal Quaternion FinalRotation { get; }
        internal Vector3 EffectiveAnkle { get; }
        internal Quaternion EffectiveRotation { get; }
        internal Vector3 GoalTargetCorrection { get; }
        internal float GoalWeight { get; }
        internal float RotationWeight { get; }
    }

    internal readonly struct CharacterFootSupportFacts
    {
        internal CharacterFootSupportFacts(
            CharacterFootSupportTarget target,
            CharacterFootContactReference contact,
            float contactOwnership,
            CharacterFootSupportEligibility eligibility,
            float weight,
            float horizontalError,
            ulong eventIdentity,
            CharacterFootPelvisReachReference reachReference)
        {
            Target = target;
            Contact = contact;
            ContactOwnership = contactOwnership;
            Eligibility = eligibility;
            Weight = weight;
            HorizontalError = horizontalError;
            EventIdentity = eventIdentity;
            ReachReference = reachReference;
        }

        internal CharacterFootSupportTarget Target { get; }
        internal CharacterFootContactReference Contact { get; }
        internal float ContactOwnership { get; }
        internal CharacterFootSupportEligibility Eligibility { get; }
        internal float Weight { get; }
        internal float HorizontalError { get; }
        internal ulong EventIdentity { get; }
        internal CharacterFootPelvisReachReference ReachReference { get; }
    }

    internal readonly struct CharacterFootGoalTarget
    {
        internal CharacterFootGoalTarget(
            Vector3 componentPosition,
            Quaternion componentRotation,
            float positionWeight,
            float rotationWeight,
            Vector3 effectiveSole)
        {
            ComponentPosition = componentPosition;
            ComponentRotation = componentRotation;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            EffectiveSole = effectiveSole;
        }

        internal Vector3 ComponentPosition { get; }
        internal Quaternion ComponentRotation { get; }
        internal float PositionWeight { get; }
        internal float RotationWeight { get; }
        internal Vector3 EffectiveSole { get; }
    }

    internal readonly struct CharacterFootStrideRequest
    {
        internal CharacterFootStrideRequest(
            in AnimationFootMotionRuntimeSample step,
            bool landingAvailable,
            in CharacterFootGroundPathLanding landing,
            bool pathAccepted)
        {
            AuthoritativeSwing = IsAuthoritativeSwing(in step);
            StepEventIdentity = step.LandingEventIdentity;
            LandingAvailable = landingAvailable;
            LandingPoint = landingAvailable ? landing.Point : default;
            LandingEventIdentity = landingAvailable ? landing.LandingEventIdentity : 0;
            PathAccepted = pathAccepted;
        }

        internal bool AuthoritativeSwing { get; }
        internal ulong StepEventIdentity { get; }
        internal bool LandingAvailable { get; }
        internal Vector3 LandingPoint { get; }
        internal ulong LandingEventIdentity { get; }
        internal bool PathAccepted { get; }

        internal static bool IsAuthoritativeSwing(in AnimationFootMotionRuntimeSample step) =>
            step.IsValid && step.IsAuthoritative && step.IsSwing &&
            step.HasConsistentLandingEventIdentity;
    }

    internal readonly struct CharacterFootPlacementRequest
    {
        internal CharacterFootPlacementRequest(
            CharacterFootPlacementIdentity identity,
            CharacterFootPlacementPose pose,
            CharacterFootSupportFacts support,
            CharacterFootLandingReachRequest landingReachRequest,
            CharacterFootGoalTarget goalTarget,
            CharacterFootResolvedOutcome outcome,
            bool landingReachAdmitted,
            CharacterFootStrideRequest stride)
        {
            Identity = identity;
            Pose = pose;
            Support = support;
            LandingReachRequest = landingReachRequest;
            GoalTarget = goalTarget;
            Outcome = outcome;
            LandingReachAdmitted = landingReachAdmitted;
            Stride = stride;
        }

        internal CharacterFootPlacementIdentity Identity { get; }
        internal CharacterFootPlacementPose Pose { get; }
        internal CharacterFootSupportFacts Support { get; }
        internal CharacterFootLandingReachRequest LandingReachRequest { get; }
        internal CharacterFootGoalTarget GoalTarget { get; }
        internal CharacterFootResolvedOutcome Outcome { get; }
        internal bool LandingReachAdmitted { get; }
        internal CharacterFootStrideRequest Stride { get; }
    }

    internal readonly struct CharacterResolvedFootResult
    {
        internal CharacterResolvedFootResult(
            CharacterFootPlacementIdentity identity,
            CharacterFootPlacementPose pose,
            CharacterFootSupportFacts support,
            CharacterFootLandingReachRequest landingReachRequest,
            CharacterFootGoalTarget goalTarget,
            CharacterFootResolvedOutcome outcome)
        {
            Identity = identity;
            Pose = pose;
            Support = support;
            LandingReachRequest = landingReachRequest;
            GoalTarget = goalTarget;
            Outcome = outcome;
        }

        internal CharacterFootPlacementIdentity Identity { get; }
        internal CharacterFootPlacementPose Pose { get; }
        internal CharacterFootSupportFacts Support { get; }
        internal CharacterFootLandingReachRequest LandingReachRequest { get; }
        internal CharacterFootGoalTarget GoalTarget { get; }
        internal CharacterFootResolvedOutcome Outcome { get; }
    }

    internal readonly struct CharacterFootPlacementRequestPair
    {
        internal CharacterFootPlacementRequestPair(
            in CharacterFootPlacementRequest left,
            in CharacterFootPlacementRequest right)
        {
            CharacterFootPlacementIdentity leftIdentity = left.Identity;
            CharacterFootPlacementIdentity rightIdentity = right.Identity;
            CharacterFootPlacementContract.RequirePair(
                in leftIdentity, in rightIdentity, left.Outcome, right.Outcome);
            FrameSequence = leftIdentity.FrameSequence;
            CompletionIdentity = leftIdentity.CompletionIdentity;
            RigId = leftIdentity.RigId;
            RigRevision = leftIdentity.RigRevision;
            Left = left;
            Right = right;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootPlacementRequest Left { get; }
        internal CharacterFootPlacementRequest Right { get; }
    }

    internal readonly struct CharacterResolvedFootPair
    {
        internal CharacterResolvedFootPair(
            in CharacterResolvedFootResult left,
            in CharacterResolvedFootResult right)
        {
            CharacterFootPlacementIdentity leftIdentity = left.Identity;
            CharacterFootPlacementIdentity rightIdentity = right.Identity;
            CharacterFootPlacementContract.RequirePair(
                in leftIdentity, in rightIdentity, left.Outcome, right.Outcome);
            FrameSequence = leftIdentity.FrameSequence;
            CompletionIdentity = leftIdentity.CompletionIdentity;
            RigId = leftIdentity.RigId;
            RigRevision = leftIdentity.RigRevision;
            Left = left;
            Right = right;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterResolvedFootResult Left { get; }
        internal CharacterResolvedFootResult Right { get; }
    }

    internal static class CharacterFootPlacementContract
    {
        internal static void RequirePair(
            in CharacterFootPlacementIdentity left,
            in CharacterFootPlacementIdentity right,
            CharacterFootResolvedOutcome leftOutcome,
            CharacterFootResolvedOutcome rightOutcome)
        {
            if (!ValidOutcome(leftOutcome) || !ValidOutcome(rightOutcome) ||
                left.FrameSequence == 0 ||
                left.FrameSequence != right.FrameSequence ||
                left.CompletionIdentity == 0 ||
                left.CompletionIdentity != right.CompletionIdentity ||
                !left.RigId.Equals(right.RigId) ||
                !left.RigRevision.Equals(right.RigRevision))
            {
                throw new InvalidOperationException(
                    "Foot Placement Pair lineage is inconsistent.");
            }
        }

        static bool ValidOutcome(CharacterFootResolvedOutcome outcome) =>
            outcome == CharacterFootResolvedOutcome.Ready ||
            outcome == CharacterFootResolvedOutcome.CurrentSupportUnavailable ||
            outcome == CharacterFootResolvedOutcome.SupportTargetUnavailable ||
            outcome == CharacterFootResolvedOutcome.RotationProjectionUnavailable;
    }
}
