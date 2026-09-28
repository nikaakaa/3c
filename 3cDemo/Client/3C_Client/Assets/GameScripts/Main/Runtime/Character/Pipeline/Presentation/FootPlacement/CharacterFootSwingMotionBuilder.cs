using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
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

    public readonly struct CharacterResolvedFootCoreDiagnostics
    {
        internal CharacterResolvedFootCoreDiagnostics(
            in CharacterResolvedFootResult result,
            in CharacterFootPlacementAnimatedFootPose source)
        {
            FrameSequence = result.Identity.FrameSequence;
            CompletionIdentity = result.Identity.CompletionIdentity;
            RigId = result.Identity.RigId.ToString();
            RigRevision = result.Identity.RigRevision.ToString();
            Side = result.Identity.Side;
            Outcome = result.Outcome;
            FinalSole = result.Pose.FinalSole;
            EffectiveSole = result.Pose.EffectiveSole;
            GoalTargetAnkle = result.Pose.FinalAnkle;
            GoalTargetRotation = result.Pose.FinalRotation;
            EffectiveAnkle = result.Pose.EffectiveAnkle;
            EffectiveRotation = result.Pose.EffectiveRotation;
            CharacterFootPlacementSoleContactPose contacts =
                source.ResolveSoleContacts(EffectiveAnkle, EffectiveRotation);
            EffectiveHeel = contacts.HeelPosition;
            EffectiveToe = contacts.ToePosition;
            EffectiveSoleFromContacts =
                (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
            SourceSoleForward = source.SoleForward;
            SourceSoleFrameLocalRotation = source.SoleFrameLocalRotation;
            GoalTargetCorrection = result.Pose.GoalTargetCorrection;
            EffectiveSoleCorrection =
                EffectiveSoleFromContacts -
                (source.HeelPosition + source.ToePosition) * 0.5f;
            PositionWeight = result.Pose.GoalWeight;
            RotationWeight = result.Pose.RotationWeight;
        }

        [DiagnosticField]
        [DiagnosticKey("resolved-frame-sequence")]
        [DiagnosticGroup("resolved-core")]
        public ulong FrameSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-completion-identity")]
        [DiagnosticGroup("resolved-core")]
        public ulong CompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-rig-id")]
        [DiagnosticGroup("resolved-core")]
        public string RigId { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-rig-revision")]
        [DiagnosticGroup("resolved-core")]
        public string RigRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-side")]
        [DiagnosticGroup("resolved-core")]
        public CharacterFootSide Side { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-outcome")]
        [DiagnosticGroup("resolved-core")]
        public CharacterFootResolvedOutcome Outcome { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-final-sole")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 FinalSole { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-sole")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveSole { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-goal-target-ankle")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 GoalTargetAnkle { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-goal-target-rotation")]
        [DiagnosticGroup("resolved-core")]
        public Quaternion GoalTargetRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-ankle")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveAnkle { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-rotation")]
        [DiagnosticGroup("resolved-core")]
        public Quaternion EffectiveRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-heel")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveHeel { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-toe")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveToe { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-sole-from-contacts")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveSoleFromContacts { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-source-sole-forward")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 SourceSoleForward { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-source-sole-frame-local-rotation")]
        [DiagnosticGroup("resolved-core")]
        public Quaternion SourceSoleFrameLocalRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-goal-target-correction")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 GoalTargetCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-effective-sole-correction")]
        [DiagnosticGroup("resolved-core")]
        public Vector3 EffectiveSoleCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-position-weight")]
        [DiagnosticGroup("resolved-core")]
        public float PositionWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-rotation-weight")]
        [DiagnosticGroup("resolved-core")]
        public float RotationWeight { get; }
    }

    public readonly struct CharacterResolvedFootContactDiagnostics
    {
        internal CharacterResolvedFootContactDiagnostics(in CharacterFootSupportFacts support)
        {
            Available = support.Contact.IsAvailable;
            EventIdentity = support.Contact.EventIdentity;
            Point = support.Contact.Point;
            Ownership = support.ContactOwnership;
        }

        [DiagnosticField]
        [DiagnosticKey("resolved-contact-available")]
        [DiagnosticGroup("resolved-contact")]
        public bool Available { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-contact-event-identity")]
        [DiagnosticGroup("resolved-contact")]
        public ulong EventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-contact-point")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-contact-available")]
        public Vector3 Point { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-contact-ownership")]
        [DiagnosticGroup("resolved-contact")]
        public float Ownership { get; }
    }

    public readonly struct CharacterResolvedFootSupportDiagnostics
    {
        internal CharacterResolvedFootSupportDiagnostics(in CharacterFootSupportFacts support)
        {
            Eligibility = support.Eligibility;
            Weight = support.Weight;
            HorizontalError = support.HorizontalError;
            EventIdentity = support.EventIdentity;
        }

        [DiagnosticField]
        [DiagnosticKey("resolved-support-eligibility")]
        [DiagnosticGroup("resolved-contact")]
        public CharacterFootSupportEligibility Eligibility { get; }

        [DiagnosticField]
        [DiagnosticGroup("resolved-contact")]
        public float Weight { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-support-horizontal-error")]
        [DiagnosticGroup("resolved-contact")]
        public float HorizontalError { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-support-event-identity")]
        [DiagnosticGroup("resolved-contact")]
        public ulong EventIdentity { get; }
    }

    public readonly struct CharacterResolvedFootReachDiagnostics
    {
        internal CharacterResolvedFootReachDiagnostics(in CharacterResolvedFootResult result)
        {
            PelvisAvailable = result.Support.ReachReference.IsAvailable;
            PelvisEventIdentity = result.Support.ReachReference.EventIdentity;
            PelvisPoint = result.Support.ReachReference.Point;
            LandingAvailable = result.LandingReachRequest.IsAvailable;
            LandingEventIdentity = result.LandingReachRequest.EventIdentity;
            LandingHip = result.LandingReachRequest.Hip;
            LandingTargetAnkle = result.LandingReachRequest.TargetAnkle;
            LandingLegLength = result.LandingReachRequest.LegLength;
            LandingMinimumCompressionReserve =
                result.LandingReachRequest.MinimumCompressionReserve;
        }

        [DiagnosticField]
        [DiagnosticKey("resolved-pelvis-reach-available")]
        [DiagnosticGroup("resolved-contact")]
        public bool PelvisAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-pelvis-reach-event-identity")]
        [DiagnosticGroup("resolved-contact")]
        public ulong PelvisEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-pelvis-reach-point")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-pelvis-reach-available")]
        public Vector3 PelvisPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-available")]
        [DiagnosticGroup("resolved-contact")]
        public bool LandingAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-event-identity")]
        [DiagnosticGroup("resolved-contact")]
        public ulong LandingEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-hip")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-landing-reach-available")]
        public Vector3 LandingHip { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-target-ankle")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-landing-reach-available")]
        public Vector3 LandingTargetAnkle { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-leg-length")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-landing-reach-available")]
        public float LandingLegLength { get; }

        [DiagnosticField]
        [DiagnosticKey("resolved-landing-reach-minimum-compression-reserve")]
        [DiagnosticGroup("resolved-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "resolved-landing-reach-available")]
        public float LandingMinimumCompressionReserve { get; }
    }

    public readonly struct CharacterResolvedFootDiagnostics
    {
        internal CharacterResolvedFootDiagnostics(
            in CharacterResolvedFootResult result,
            in CharacterFootPlacementAnimatedFootPose source)
        {
            Core = new CharacterResolvedFootCoreDiagnostics(in result, in source);
            CharacterFootSupportFacts support = result.Support;
            Contact = new CharacterResolvedFootContactDiagnostics(in support);
            Support = new CharacterResolvedFootSupportDiagnostics(in support);
            Reach = new CharacterResolvedFootReachDiagnostics(in result);
            CharacterFootSupportTarget supportTarget = support.Target;
            SupportTarget = new CharacterFootSupportTargetDiagnostics(in supportTarget);
        }

        public CharacterResolvedFootCoreDiagnostics Core { get; }
        public CharacterFootSupportTargetDiagnostics SupportTarget { get; }
        public CharacterResolvedFootContactDiagnostics Contact { get; }
        public CharacterResolvedFootSupportDiagnostics Support { get; }
        public CharacterResolvedFootReachDiagnostics Reach { get; }
    }

    public enum CharacterFootSwingMotionState : byte
    {
        None = 0,
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootSwingMotionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepNotSwing = 2,
        InvalidComponentUp = 3,
        InvalidWeight = 4,
        GroundPathRejected = 5,
        UnreachableEdge = 6,
        LandingEventMismatch = 7,
        InvalidEnvelope = 8,
        EnvelopeEndpointMismatch = 9,
        EnvelopeUnordered = 10,
        DegeneratePath = 11,
        EnvelopeSampleUnavailable = 12,
        NegativeVerticalCorrection = 13,
        InvalidSwingPhase = 14,
        UnselectedSwing = 15,
        FormalFootHeightUnavailable = 16
    }

    internal readonly struct CharacterFootSwingPathReference
    {
        internal CharacterFootSwingPathReference(
            ulong landingEventIdentity,
            Vector3 landingPoint)
        {
            if (landingEventIdentity == 0 ||
                !float.IsFinite(landingPoint.x) ||
                !float.IsFinite(landingPoint.y) ||
                !float.IsFinite(landingPoint.z))
            {
                throw new ArgumentException(
                    "Swing Path reference is invalid.");
            }
            LandingEventIdentity = landingEventIdentity;
            LandingPoint = landingPoint;
            IsAvailable = true;
        }

        internal bool IsAvailable { get; }
        internal ulong LandingEventIdentity { get; }
        internal Vector3 LandingPoint { get; }
    }

    internal readonly struct CharacterFootSwingMotionResult
    {
        internal CharacterFootSwingMotionResult(
            CharacterFootSwingMotionState state,
            CharacterFootSwingMotionRejectReason rejectReason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            CharacterFootSwingPathReference swingPathReference,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance,
            float progress,
            Vector3 baselineSample,
            Vector3 envelopeSample,
            float formalTargetHeightAlongUp,
            float verticalCorrection,
            float landingPredictionError,
            Vector3 correctedSole,
            Vector3 correctedAnkle,
            float positionWeight,
            float rotationWeight,
            CharacterFootConstraintState constraintState = CharacterFootConstraintState.Swing,
            CharacterFootLockResponse lockResponse = CharacterFootLockResponse.None,
            float supportHorizontalError = 0f,
            float contactOwnership = 0f,
            float supportWeight = 0f,
            Vector3 supportContactAnchor = default,
            Vector3 desiredCorrection = default,
            bool contactPlaneAvailable = false,
            int contactSurfaceIdentity = 0,
            Vector3 contactPlaneNormal = default,
            CharacterFootPathContinuityFact pathContinuity = default,
            bool landingReachEvaluated = false,
            bool landingReachAvailable = false,
            CharacterFootLifecycleTransitionFact lifecycleTransition = default)
        {
            State = state;
            RejectReason = rejectReason;
            LandingEventIdentity = landingEventIdentity;
            GroundPathInputIdentity = groundPathInputIdentity;
            SwingPathReference = swingPathReference;
            OriginalSole = originalSole;
            OriginalAnkle = originalAnkle;
            Distance = distance;
            Progress = progress;
            BaselineSample = baselineSample;
            EnvelopeSample = envelopeSample;
            FormalTargetHeightAlongUp = formalTargetHeightAlongUp;
            VerticalCorrection = verticalCorrection;
            LandingPredictionError = landingPredictionError;
            CorrectedSole = correctedSole;
            CorrectedAnkle = correctedAnkle;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            ConstraintState = constraintState;
            LockResponse = lockResponse;
            SupportHorizontalError = supportHorizontalError;
            ContactOwnership = contactOwnership;
            SupportWeight = supportWeight;
            SupportContactAnchor = supportContactAnchor;
            DesiredCorrection = desiredCorrection;
            ContactPlaneAvailable = contactPlaneAvailable;
            ContactSurfaceIdentity = contactSurfaceIdentity;
            ContactPlaneNormal = contactPlaneNormal;
            PathContinuity = pathContinuity;
            LandingReachEvaluated = landingReachEvaluated;
            LandingReachAvailable = landingReachAvailable;
            LifecycleTransition = lifecycleTransition;
        }

        public CharacterFootSwingMotionState State { get; }
        public CharacterFootSwingMotionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong GroundPathInputIdentity { get; }
        internal CharacterFootSwingPathReference SwingPathReference { get; }
        public Vector3 OriginalSole { get; }
        public Vector3 OriginalAnkle { get; }
        public float Distance { get; }
        public float Progress { get; }
        public Vector3 BaselineSample { get; }
        public Vector3 EnvelopeSample { get; }
        public float FormalTargetHeightAlongUp { get; }
        public float VerticalCorrection { get; }
        public float LandingPredictionError { get; }
        public Vector3 CorrectedSole { get; }
        public Vector3 CorrectedAnkle { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFootConstraintState ConstraintState { get; }
        public CharacterFootLockResponse LockResponse { get; }
        public float SupportHorizontalError { get; }
        public float ContactOwnership { get; }
        public float SupportWeight { get; }
        public Vector3 SupportContactAnchor { get; }
        public Vector3 DesiredCorrection { get; }
        public bool ContactPlaneAvailable { get; }
        public int ContactSurfaceIdentity { get; }
        public Vector3 ContactPlaneNormal { get; }
        public bool LandingReachEvaluated { get; }
        public bool LandingReachAvailable { get; }
        internal CharacterFootPathContinuityFact PathContinuity { get; }
        internal CharacterFootLifecycleTransitionFact LifecycleTransition { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;
    }

    public readonly struct CharacterFootSwingCoreDiagnostics
    {
        internal CharacterFootSwingCoreDiagnostics(in CharacterFootSwingMotionResult result)
        {
            State = result.State;
            RejectReason = result.RejectReason;
            LandingEventIdentity = result.LandingEventIdentity;
            GroundPathInputIdentity = result.GroundPathInputIdentity;
            OriginalSole = result.OriginalSole;
            OriginalAnkle = result.OriginalAnkle;
            Distance = result.Distance;
            Progress = result.Progress;
            BaselineSample = result.BaselineSample;
            EnvelopeSample = result.EnvelopeSample;
            LandingPredictionError = result.LandingPredictionError;
            CorrectedSole = result.CorrectedSole;
            CorrectedAnkle = result.CorrectedAnkle;
            PositionWeight = result.PositionWeight;
            RotationWeight = result.RotationWeight;
            ConstraintState = result.ConstraintState;
            LockResponse = result.LockResponse;
            SupportHorizontalError = result.SupportHorizontalError;
            ContactOwnership = result.ContactOwnership;
            SupportWeight = result.SupportWeight;
            SupportContactAnchor = result.SupportContactAnchor;
            DesiredCorrection = result.DesiredCorrection;
            ContactPlaneAvailable = result.ContactPlaneAvailable;
            ContactSurfaceIdentity = result.ContactSurfaceIdentity;
            ContactPlaneNormal = result.ContactPlaneNormal;
            LandingReachEvaluated = result.LandingReachEvaluated;
            LandingReachAvailable = result.LandingReachAvailable;
        }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-state")]
        [DiagnosticGroup("motion-core")]
        public CharacterFootSwingMotionState State { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-reject-reason")]
        [DiagnosticGroup("motion-core")]
        public CharacterFootSwingMotionRejectReason RejectReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-landing-event-identity")]
        [DiagnosticGroup("motion-core")]
        public ulong LandingEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-ground-path-input-identity")]
        [DiagnosticGroup("motion-core")]
        public ulong GroundPathInputIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-original-sole")]
        [DiagnosticGroup("motion-core")]
        public Vector3 OriginalSole { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-original-ankle")]
        [DiagnosticGroup("motion-core")]
        public Vector3 OriginalAnkle { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-distance")]
        [DiagnosticGroup("motion-core")]
        public float Distance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-progress")]
        [DiagnosticGroup("motion-core")]
        public float Progress { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-baseline-sample")]
        [DiagnosticGroup("motion-core")]
        public Vector3 BaselineSample { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-envelope-sample")]
        [DiagnosticGroup("motion-core")]
        public Vector3 EnvelopeSample { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-landing-prediction-error")]
        [DiagnosticGroup("motion-core")]
        public float LandingPredictionError { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-corrected-sole")]
        [DiagnosticGroup("motion-core")]
        public Vector3 CorrectedSole { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-corrected-ankle")]
        [DiagnosticGroup("motion-core")]
        public Vector3 CorrectedAnkle { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-position-weight")]
        [DiagnosticGroup("motion-core")]
        public float PositionWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-rotation-weight")]
        [DiagnosticGroup("motion-core")]
        public float RotationWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-constraint-state")]
        [DiagnosticGroup("motion-core")]
        public CharacterFootConstraintState ConstraintState { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-lock-response")]
        [DiagnosticGroup("motion-core")]
        public CharacterFootLockResponse LockResponse { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-support-horizontal-error")]
        [DiagnosticGroup("motion-core")]
        public float SupportHorizontalError { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-contact-ownership")]
        [DiagnosticGroup("motion-core")]
        public float ContactOwnership { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-support-weight")]
        [DiagnosticGroup("motion-core")]
        public float SupportWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-support-contact-anchor")]
        [DiagnosticGroup("motion-core")]
        public Vector3 SupportContactAnchor { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-desired-correction")]
        [DiagnosticGroup("motion-core")]
        public Vector3 DesiredCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-contact-plane-available")]
        [DiagnosticGroup("motion-core")]
        public bool ContactPlaneAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-contact-surface-identity")]
        [DiagnosticGroup("motion-core")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-contact-plane-available")]
        public int ContactSurfaceIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-contact-plane-normal")]
        [DiagnosticGroup("motion-core")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-contact-plane-available")]
        public Vector3 ContactPlaneNormal { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-landing-reach-evaluated")]
        [DiagnosticGroup("motion-core")]
        public bool LandingReachEvaluated { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-landing-reach-available")]
        [DiagnosticGroup("motion-core")]
        public bool LandingReachAvailable { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;
    }

    public readonly struct CharacterFootLifecycleTransitionDiagnostics
    {
        internal CharacterFootLifecycleTransitionDiagnostics(in CharacterFootLifecycleTransitionFact lifecycle)
        {
            CharacterFootContactHistoryFact previousContext =
                lifecycle.PreviousContext;
            CharacterFootContactHistoryFact currentContext =
                lifecycle.CurrentContext;
            CharacterFootContactAnchorFact previousAnchor =
                lifecycle.PreviousAnchor;
            CharacterFootContactAnchorFact currentAnchor =
                lifecycle.CurrentAnchor;
            CharacterFootLockRequest request = lifecycle.Request;
            CharacterFootTransitionDecision preTransition =
                lifecycle.PreTransition;
            CharacterFootTransitionDecision postTransition =
                lifecycle.PostTransition;
            LifecycleTransitionEvaluated = lifecycle.Evaluated;
            PreviousLockRequestAvailable = previousContext.RequestAvailable;
            PreviousLockRequested = previousContext.RequestedLock;
            PreviousLockRequestEventIdentity =
                previousContext.RequestEventIdentity;
            PreviousLockRequestMode = previousContext.RequestMode;
            PreviousLockRequestWeight = previousContext.RequestWeight;
            PreviousContactEdgeSeconds = previousContext.SecondsSinceEdge;
            PreviousLatestContactEventIdentity =
                previousContext.LatestContactEventIdentity;
            PreviousLatestReleasedContactEventIdentity =
                previousContext.LatestReleasedContactEventIdentity;
            PreviousCompletedLockWeightEventIdentity =
                previousContext.CompletedLockWeightEventIdentity;
            PreviousContactAnchorAvailable = previousAnchor.Available;
            PreviousContactAnchorEventIdentity = previousAnchor.EventIdentity;
            PreviousContactAnchorAcquiredFrameSequence =
                previousAnchor.AcquiredFrameSequence;
            PreviousContactAnchorAcquiredCompletionIdentity =
                previousAnchor.AcquiredCompletionIdentity;
            PreviousContactAnchorWorldRevision = previousAnchor.WorldRevision;
            PreviousContactAnchorSurfaceIdentity = previousAnchor.SurfaceIdentity;
            PreviousContactAnchorPoint = previousAnchor.Point;
            PreviousContactAnchorNormal = previousAnchor.Normal;
            CurrentLockRequested = request.RequestsLock;
            CurrentLockRequestEventIdentity = request.EventIdentity;
            CurrentLockRequestMode = request.Mode;
            CurrentLockRequestWeight = request.Weight;
            CurrentLockRequestAvailability = request.Availability;
            ContactEdge = preTransition.ContactEdge;
            CurrentContactEdgeSeconds = currentContext.SecondsSinceEdge;
            CurrentLatestContactEventIdentity =
                currentContext.LatestContactEventIdentity;
            CurrentLatestReleasedContactEventIdentity =
                currentContext.LatestReleasedContactEventIdentity;
            CurrentCompletedLockWeightEventIdentity =
                currentContext.CompletedLockWeightEventIdentity;
            CurrentContactAnchorAvailable = currentAnchor.Available;
            CurrentContactAnchorEventIdentity = currentAnchor.EventIdentity;
            CurrentContactAnchorAcquiredFrameSequence =
                currentAnchor.AcquiredFrameSequence;
            CurrentContactAnchorAcquiredCompletionIdentity =
                currentAnchor.AcquiredCompletionIdentity;
            CurrentContactAnchorWorldRevision = currentAnchor.WorldRevision;
            CurrentContactAnchorSurfaceIdentity = currentAnchor.SurfaceIdentity;
            CurrentContactAnchorPoint = currentAnchor.Point;
            CurrentContactAnchorNormal = currentAnchor.Normal;
            SameEventContactReentryRefreshed =
                lifecycle.SameEventContactReentryRefreshed;
            SameEventContactReentryUnavailable =
                lifecycle.SameEventContactReentryUnavailable;
            RetainedVerifiedAnchor = lifecycle.RetainedVerifiedAnchor;
            ReentryInterpolationHistoryRetained =
                lifecycle.ReentryInterpolationHistoryRetained;
            FormalFootPlacementWeight = lifecycle.FormalFootPlacementWeight;
            HardOwnershipLoss = lifecycle.HardOwnershipLoss;
            HardOwnershipLossReason = lifecycle.OwnershipLossReason;
            PreTransitionReason = preTransition.Reason;
            PreTransitionSource = preTransition.SourceState;
            PreTransitionTarget = preTransition.TargetState;
            PreTransitionAnchorCommand = preTransition.AnchorCommand;
            PreTransitionSuppressOutput = preTransition.SuppressOutput;
            PreTransitionResetInterpolation = preTransition.ResetInterpolation;
            PostTransitionEvaluated = lifecycle.PostTransitionEvaluated;
            PostTransitionReason = postTransition.Reason;
            PostTransitionSource = postTransition.SourceState;
            PostTransitionTarget = postTransition.TargetState;
            PostTransitionAnchorCommand = postTransition.AnchorCommand;
            PostTransitionSuppressOutput = postTransition.SuppressOutput;
            PostTransitionResetInterpolation = postTransition.ResetInterpolation;
        }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-lifecycle-transition-evaluated")]
        [DiagnosticGroup("lifecycle")]
        public bool LifecycleTransitionEvaluated { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-lock-request-available")]
        [DiagnosticGroup("lifecycle")]
        public bool PreviousLockRequestAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-lock-requested")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-lock-request-available")]
        public bool PreviousLockRequested { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-lock-request-event-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-lock-request-available")]
        public ulong PreviousLockRequestEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-lock-request-mode")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-lock-request-available")]
        public AnimationFootStepObservationLockMode PreviousLockRequestMode { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-lock-request-weight")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-lock-request-available")]
        public float PreviousLockRequestWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-edge-seconds")]
        [DiagnosticGroup("lifecycle")]
        public float PreviousContactEdgeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-latest-contact-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong PreviousLatestContactEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-latest-released-contact-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong PreviousLatestReleasedContactEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-completed-lock-weight-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong PreviousCompletedLockWeightEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-available")]
        [DiagnosticGroup("lifecycle")]
        public bool PreviousContactAnchorAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-event-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-acquired-frame-sequence")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorAcquiredFrameSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-acquired-completion-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorAcquiredCompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-world-revision")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorWorldRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-surface-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public int PreviousContactAnchorSurfaceIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-point")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public Vector3 PreviousContactAnchorPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-contact-anchor-normal")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-contact-anchor-available")]
        public Vector3 PreviousContactAnchorNormal { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-lock-requested")]
        [DiagnosticGroup("lifecycle")]
        public bool CurrentLockRequested { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-lock-request-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong CurrentLockRequestEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-lock-request-mode")]
        [DiagnosticGroup("lifecycle")]
        public AnimationFootStepObservationLockMode CurrentLockRequestMode { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-lock-request-weight")]
        [DiagnosticGroup("lifecycle")]
        public float CurrentLockRequestWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-lock-request-availability")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootLockRequestAvailability CurrentLockRequestAvailability { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-contact-edge")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootContactEdge ContactEdge { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-edge-seconds")]
        [DiagnosticGroup("lifecycle")]
        public float CurrentContactEdgeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-latest-contact-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong CurrentLatestContactEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-latest-released-contact-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong CurrentLatestReleasedContactEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-completed-lock-weight-event-identity")]
        [DiagnosticGroup("lifecycle")]
        public ulong CurrentCompletedLockWeightEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-available")]
        [DiagnosticGroup("lifecycle")]
        public bool CurrentContactAnchorAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-event-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-acquired-frame-sequence")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorAcquiredFrameSequence { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-acquired-completion-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorAcquiredCompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-world-revision")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorWorldRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-surface-identity")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public int CurrentContactAnchorSurfaceIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-point")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public Vector3 CurrentContactAnchorPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-current-contact-anchor-normal")]
        [DiagnosticGroup("lifecycle")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-current-contact-anchor-available")]
        public Vector3 CurrentContactAnchorNormal { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-same-event-contact-reentry-refreshed")]
        [DiagnosticGroup("lifecycle")]
        public bool SameEventContactReentryRefreshed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-same-event-contact-reentry-unavailable")]
        [DiagnosticGroup("lifecycle")]
        public bool SameEventContactReentryUnavailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-retained-verified-anchor")]
        [DiagnosticGroup("lifecycle")]
        public bool RetainedVerifiedAnchor { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-reentry-interpolation-history-retained")]
        [DiagnosticGroup("lifecycle")]
        public bool ReentryInterpolationHistoryRetained { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-formal-foot-placement-weight")]
        [DiagnosticGroup("lifecycle")]
        public float FormalFootPlacementWeight { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-hard-ownership-loss")]
        [DiagnosticGroup("lifecycle")]
        public bool HardOwnershipLoss { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-hard-ownership-loss-reason")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootGoalOwnershipLossReason HardOwnershipLossReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-suppress-output")]
        [DiagnosticGroup("lifecycle")]
        public bool PreTransitionSuppressOutput { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-reset-interpolation")]
        [DiagnosticGroup("lifecycle")]
        public bool PreTransitionResetInterpolation { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-evaluated")]
        [DiagnosticGroup("lifecycle")]
        public bool PostTransitionEvaluated { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-suppress-output")]
        [DiagnosticGroup("lifecycle")]
        public bool PostTransitionSuppressOutput { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-reset-interpolation")]
        [DiagnosticGroup("lifecycle")]
        public bool PostTransitionResetInterpolation { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-reason")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootTransitionReason PreTransitionReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-source")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootConstraintState PreTransitionSource { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-target")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootConstraintState PreTransitionTarget { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-pre-transition-anchor-command")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootAnchorCommand PreTransitionAnchorCommand { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-reason")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootTransitionReason PostTransitionReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-source")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootConstraintState PostTransitionSource { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-target")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootConstraintState PostTransitionTarget { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-post-transition-anchor-command")]
        [DiagnosticGroup("lifecycle")]
        public CharacterFootAnchorCommand PostTransitionAnchorCommand { get; }
    }

    public readonly struct CharacterFootPathContinuityDiagnostics
    {
        internal CharacterFootPathContinuityDiagnostics(in CharacterFootPathContinuityFact path)
        {
            PathContinuityEvaluated = path.Evaluated;
            PathRevisionReason = path.RevisionReason;
            PathResidualRebuilt = path.ResidualRebuilt;
            TargetTrackingApplied = path.TargetTrackingApplied;
            PathAvailableBefore = path.PathAvailableBefore;
            PathAvailableAfter = path.PathAvailableAfter;
            PathPreviousLandingEventIdentity = path.PreviousLandingEventIdentity;
            PathCurrentLandingEventIdentity = path.CurrentLandingEventIdentity;
            PathPreviousTargetCorrection = path.PreviousTargetCorrection;
            PathCurrentTargetCorrection = path.CurrentTargetCorrection;
            PathLandingPointDelta = path.LandingPointDelta;
            PathTargetDelta = path.TargetDelta;
            SwingResidualBeforeRevision = path.ResidualBeforeRevision;
            SwingResidualBeforeDecay = path.ResidualBeforeDecay;
            SwingResidualAfterDecay = path.ResidualAfterDecay;
            ResidualOutputCorrection = path.ResidualOutputCorrection;
            LandingAcceptanceDistance = path.LandingAcceptanceDistance;
            PathRevisionDistance = path.PathRevisionDistance;
            SwingResidualTolerance = path.SwingResidualTolerance;
            ResidualTimeToLandingSeconds = path.TimeToLandingSeconds;
            ResidualBaseHalfLifeSeconds = path.BaseHalfLifeSeconds;
            ResidualDeadlineHalfLifeAvailable = path.DeadlineHalfLifeAvailable;
            ResidualDeadlineHalfLifeSeconds = path.DeadlineHalfLifeSeconds;
            ResidualAppliedHalfLifeSeconds = path.AppliedHalfLifeSeconds;
            SwingRawTargetHeightAlongUp = path.SwingRawTargetHeightAlongUp;
            SwingFilteredTargetHeightBefore =
                path.SwingFilteredTargetHeightBefore;
            SwingTargetHeightDelta = path.SwingTargetHeightDelta;
            SwingTargetHeightAppliedDelta =
                path.SwingTargetHeightAppliedDelta;
            SwingTargetHeightUpdateHeld =
                path.SwingTargetHeightUpdateHeld;
            SwingTargetHeightForceRefreshed =
                path.SwingTargetHeightForceRefreshed;
            SwingTargetHeightRateLimited =
                path.SwingTargetHeightRateLimited;
            SwingTargetHeightClamped = path.SwingTargetHeightClamped;
            SwingTargetHeightForceRefreshDistance =
                path.SwingTargetHeightForceRefreshDistance;
            SwingTargetMaximumVerticalSpeed =
                path.SwingTargetMaximumVerticalSpeed;
            SwingTargetHeightAdoptionMode =
                path.SwingTargetHeightAdoptionMode;
            SwingFilteredTargetHeightAlongUp =
                path.SwingFilteredTargetHeightAlongUp;
            TargetHeightComponentUp = path.TargetHeightComponentUp;
        }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-continuity-evaluated")]
        [DiagnosticGroup("path-continuity")]
        public bool PathContinuityEvaluated { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-revision-reason")]
        [DiagnosticGroup("path-continuity")]
        public CharacterFootPathRevisionReason PathRevisionReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-residual-rebuilt")]
        [DiagnosticGroup("path-continuity")]
        public bool PathResidualRebuilt { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-target-tracking-applied")]
        [DiagnosticGroup("path-continuity")]
        public bool TargetTrackingApplied { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-available-before")]
        [DiagnosticGroup("path-continuity")]
        public bool PathAvailableBefore { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-available-after")]
        [DiagnosticGroup("path-continuity")]
        public bool PathAvailableAfter { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-previous-landing-event-identity")]
        [DiagnosticGroup("path-continuity")]
        public ulong PathPreviousLandingEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-current-landing-event-identity")]
        [DiagnosticGroup("path-continuity")]
        public ulong PathCurrentLandingEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-previous-target-correction")]
        [DiagnosticGroup("path-continuity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-path-available-before")]
        public Vector3 PathPreviousTargetCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-current-target-correction")]
        [DiagnosticGroup("path-continuity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-path-available-after")]
        public Vector3 PathCurrentTargetCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-landing-point-delta-meters")]
        [DiagnosticGroup("path-continuity")]
        public float PathLandingPointDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-target-delta-meters")]
        [DiagnosticGroup("path-continuity")]
        public float PathTargetDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-residual-before-revision")]
        [DiagnosticGroup("path-continuity")]
        public Vector3 SwingResidualBeforeRevision { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-residual-before-decay")]
        [DiagnosticGroup("path-continuity")]
        public Vector3 SwingResidualBeforeDecay { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-residual-after-decay")]
        [DiagnosticGroup("path-continuity")]
        public Vector3 SwingResidualAfterDecay { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-output-correction")]
        [DiagnosticGroup("path-continuity")]
        public Vector3 ResidualOutputCorrection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-landing-acceptance-distance")]
        [DiagnosticGroup("path-continuity")]
        public float LandingAcceptanceDistance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-path-revision-distance")]
        [DiagnosticGroup("path-continuity")]
        public float PathRevisionDistance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-residual-tolerance")]
        [DiagnosticGroup("path-continuity")]
        public float SwingResidualTolerance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-time-to-landing-seconds")]
        [DiagnosticGroup("path-continuity")]
        public float ResidualTimeToLandingSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-base-half-life-seconds")]
        [DiagnosticGroup("path-continuity")]
        public float ResidualBaseHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-deadline-half-life-available")]
        [DiagnosticGroup("path-continuity")]
        public bool ResidualDeadlineHalfLifeAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-deadline-half-life-seconds")]
        [DiagnosticGroup("path-continuity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-residual-deadline-half-life-available")]
        public float ResidualDeadlineHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-residual-applied-half-life-seconds")]
        [DiagnosticGroup("path-continuity")]
        public float ResidualAppliedHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-raw-target-height-along-up")]
        [DiagnosticGroup("path-continuity")]
        public float SwingRawTargetHeightAlongUp { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-filtered-target-height-before")]
        [DiagnosticGroup("path-continuity")]
        public float SwingFilteredTargetHeightBefore { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-delta")]
        [DiagnosticGroup("path-continuity")]
        public float SwingTargetHeightDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-applied-delta")]
        [DiagnosticGroup("path-continuity")]
        public float SwingTargetHeightAppliedDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-update-held")]
        [DiagnosticGroup("path-continuity")]
        public bool SwingTargetHeightUpdateHeld { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-force-refreshed")]
        [DiagnosticGroup("path-continuity")]
        public bool SwingTargetHeightForceRefreshed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-rate-limited")]
        [DiagnosticGroup("path-continuity")]
        public bool SwingTargetHeightRateLimited { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-clamped")]
        [DiagnosticGroup("path-continuity")]
        public bool SwingTargetHeightClamped { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-force-refresh-distance")]
        [DiagnosticGroup("path-continuity")]
        public float SwingTargetHeightForceRefreshDistance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-maximum-vertical-speed")]
        [DiagnosticGroup("path-continuity")]
        public float SwingTargetMaximumVerticalSpeed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-target-height-adoption-mode")]
        [DiagnosticGroup("path-continuity")]
        public CharacterFootTargetHeightAdoptionMode SwingTargetHeightAdoptionMode { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-swing-filtered-target-height-along-up")]
        [DiagnosticGroup("path-continuity")]
        public float SwingFilteredTargetHeightAlongUp { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-target-height-component-up")]
        [DiagnosticGroup("path-continuity")]
        public Vector3 TargetHeightComponentUp { get; }
    }

    public readonly struct CharacterFootOutputStagesDiagnostics
    {
        internal CharacterFootOutputStagesDiagnostics(
            in CharacterFootLifecycleTransitionFact lifecycle,
            in CharacterFootPathContinuityFact path)
        {
            ConstraintStateBefore = lifecycle.PreTransition.SourceState;
            LockResponseBefore = lifecycle.LockResponseBefore;
            StateTargetCorrection = path.StateTargetCorrection;
            InterpolationPolicy = path.InterpolationPolicy;
            InterpolationOutputCorrection =
                path.InterpolationOutputCorrection;
            InterpolationCompleted = path.InterpolationCompleted;
            OutputStagesAvailable = path.OutputStagesAvailable;
            ReleasingCompletedToSwing = path.ReleasingCompletedToSwing;
            SafetyFloorAvailable = path.SafetyFloorAvailable;
            SafetyFloorOwner = path.SafetyFloorOwner;
            SafetyFloorOwnerSurfaceIdentity =
                path.SafetyFloorOwnerSurfaceIdentity;
            SafetyFloorOwnerPathIdentity =
                path.SafetyFloorOwnerPathIdentity;
            CorrectionBeforeSafetyFloor = path.CorrectionBeforeSafetyFloor;
            SafetyFloorMinimumCorrection =
                path.SafetyFloorMinimumCorrection;
            SafetyFloorOutputCorrection = path.SafetyFloorOutputCorrection;
            FinalEffectiveCorrection = path.FinalEffectiveCorrection;
            SafetyFloorClamped = path.SafetyFloorClamped;
            SafetyFloorClampMeters = path.SafetyFloorClampMeters;
            SafetyFloorClearanceBeforeMeters =
                path.SafetyFloorClearanceBeforeMeters;
            SafetyFloorClearanceAfterMeters =
                path.SafetyFloorClearanceAfterMeters;
            PlantInterpolationEvaluated = path.PlantInterpolationEvaluated;
            PlantTargetEventIdentity = path.PlantTargetEventIdentity;
            PlantTargetVerified = path.PlantTargetVerified;
            PlantTargetKind = path.PlantTargetKind;
            PlantLockResponse = path.PlantLockResponse;
            PlantLockWeightCompleted = path.PlantLockWeightCompleted;
            PlantDesiredPoint = path.PlantDesiredPoint;
            PlantFilteredPoint = path.PlantFilteredPoint;
        }

        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 StateTargetCorrection { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 InterpolationOutputCorrection { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool InterpolationCompleted { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootConstraintState ConstraintStateBefore { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootLockResponse LockResponseBefore { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool OutputStagesAvailable { get; }
        [DiagnosticField]
        [DiagnosticKey("foot-motion-releasing-completed-to-swing")]
        [DiagnosticGroup("output-stages")]
        public bool ReleasingCompletedToSwing { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool SafetyFloorAvailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootSafetyFloorOwner SafetyFloorOwner { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public int SafetyFloorOwnerSurfaceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public ulong SafetyFloorOwnerPathIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 CorrectionBeforeSafetyFloor { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(SafetyFloorAvailable))]
        public Vector3 SafetyFloorMinimumCorrection { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(SafetyFloorAvailable))]
        public Vector3 SafetyFloorOutputCorrection { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 FinalEffectiveCorrection { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool SafetyFloorClamped { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public float SafetyFloorClampMeters { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public float SafetyFloorClearanceBeforeMeters { get; }
        [DiagnosticField]
        [DiagnosticKey("foot-motion-safety-floor-clearance-after")]
        [DiagnosticGroup("output-stages")]
        public float SafetyFloorClearanceAfterMeters { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool PlantInterpolationEvaluated { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public ulong PlantTargetEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool PlantTargetVerified { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootPlantTargetKind PlantTargetKind { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public CharacterFootLockResponse PlantLockResponse { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public bool PlantLockWeightCompleted { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 PlantDesiredPoint { get; }
        [DiagnosticField]
        [DiagnosticGroup("output-stages")]
        public Vector3 PlantFilteredPoint { get; }
    }

    public readonly struct CharacterFootCorrectionResponseDiagnostics
    {
        internal CharacterFootCorrectionResponseDiagnostics(in CharacterFootPathContinuityFact path)
        {
            PlantTargetHeightAdoptionMode =
                path.PlantTargetHeightAdoptionMode;
            PlantTargetMaximumVerticalSpeed =
                path.PlantTargetMaximumVerticalSpeed;
            PlantTargetHeightBefore = path.PlantTargetHeightBefore;
            PlantTargetHeightTarget = path.PlantTargetHeightTarget;
            PlantTargetVerticalDelta = path.PlantTargetVerticalDelta;
            PlantTargetAppliedVerticalDelta =
                path.PlantTargetAppliedVerticalDelta;
            PlantTargetHeightAfter = path.PlantTargetHeightAfter;
            PlantTargetHeightEventIdentity =
                path.PlantTargetHeightEventIdentity;
            PlantTargetHeightUpdateReason =
                path.PlantTargetHeightUpdateReason;
            PlantTargetForceRefreshed =
                path.PlantTargetForceRefreshed;
            PlantTargetForceRefreshDistance =
                path.PlantTargetForceRefreshDistance;
            PlantTargetVerticalClamped = path.PlantTargetVerticalClamped;
            PlantPreviousSelectedWorldTarget =
                path.PlantPreviousSelectedWorldTarget;
            PlantSelectedWorldTarget = path.PlantSelectedWorldTarget;
            PreviousResponseOutputAvailable =
                path.PreviousResponseOutputAvailable;
            PreviousResponseOutputPoint =
                path.PreviousResponseOutputPoint;
            DesiredOutputPoint = path.DesiredOutputPoint;
            ResponseOutputPoint = path.ResponseOutputPoint;
            PlantResidualCaptureReason =
                path.PlantResidualCaptureReason;
            PlantWorldResidualBeforeCapture =
                path.PlantWorldResidualBeforeCapture;
            PlantWorldResidualCapturedBeforeDecay =
                path.PlantWorldResidualCapturedBeforeDecay;
            PlantWorldResidualDecayApplied =
                path.PlantWorldResidualDecayApplied;
            PlantWorldResidualBaseHalfLifeSeconds =
                path.PlantWorldResidualBaseHalfLifeSeconds;
            PlantWorldResidualDeadlineHalfLifeAvailable =
                path.PlantWorldResidualDeadlineHalfLifeAvailable;
            PlantWorldResidualDeadlineHalfLifeSeconds =
                path.PlantWorldResidualDeadlineHalfLifeSeconds;
            PlantWorldResidualAppliedHalfLifeSeconds =
                path.PlantWorldResidualAppliedHalfLifeSeconds;
            PlantWorldResidualAfterDecay =
                path.PlantWorldResidualAfterDecay;
            PlantWorldResidualCompletionTolerance =
                path.PlantWorldResidualCompletionTolerance;
            PlantWorldResidualClearedAtCompletionTolerance =
                path.PlantWorldResidualClearedAtCompletionTolerance;
            CorrectionResponseEvaluated =
                path.CorrectionResponseEvaluated;
            CorrectionResponseInitializedBefore =
                path.CorrectionResponseInitializedBefore;
            CorrectionResponseInitializedThisFrame =
                path.CorrectionResponseInitializedThisFrame;
            CorrectionResponseInitializationReason =
                path.CorrectionResponseInitializationReason;
            CorrectionResponseDesired =
                path.CorrectionResponseDesired;
            CorrectionResponseRequestedDirection =
                path.CorrectionResponseRequestedDirection;
            CorrectionResponsePreviousDirection =
                path.CorrectionResponsePreviousDirection;
            CorrectionResponseDirectionLimited =
                path.CorrectionResponseDirectionLimited;
            CorrectionResponseMaximumDirectionChangeDegrees =
                path.CorrectionResponseMaximumDirectionChangeDegrees;
            CorrectionResponseAppliedDirectionChangeDegrees =
                path.CorrectionResponseAppliedDirectionChangeDegrees;
            CorrectionResponseVisibleOutputTransferred =
                path.CorrectionResponseVisibleOutputTransferred;
            CorrectionResponseBeforeRebase =
                path.CorrectionResponseBeforeRebase;
            CorrectionResponsePrevious =
                path.CorrectionResponsePrevious;
            CorrectionResponseCurrent =
                path.CorrectionResponseCurrent;
            CorrectionResponseDirection =
                path.CorrectionResponseDirection;
            CorrectionResponseDeltaDirection =
                path.CorrectionResponseDeltaDirection;
            CorrectionResponseSelectedSpeed =
                path.CorrectionResponseSelectedSpeed;
            CorrectionResponseAppliedDelta =
                path.CorrectionResponseAppliedDelta;
            CorrectionResponseDomain = path.CorrectionResponseDomain;
            CorrectionResponsePreviousDomain = path.CorrectionResponsePreviousDomain;
            CorrectionResponseDomainTransferred = path.CorrectionResponseDomainTransferred;
            PlantVerticalContinuityOwners =
                path.PlantVerticalContinuityOwners;
            PlantEffectiveCorrectionBefore =
                path.PlantEffectiveCorrectionBefore;
            PlantEffectiveCorrectionAfter =
                path.PlantEffectiveCorrectionAfter;
            PlantOutputDistance = path.PlantOutputDistance;
            PlantPenetrationDepth = path.PlantPenetrationDepth;
        }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-adoption-mode")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootTargetHeightAdoptionMode PlantTargetHeightAdoptionMode { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-maximum-vertical-speed")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetMaximumVerticalSpeed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-before")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetHeightBefore { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-target")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetHeightTarget { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-vertical-delta")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetVerticalDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-applied-vertical-delta")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetAppliedVerticalDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-after")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetHeightAfter { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-event-identity")]
        [DiagnosticGroup("response-contact")]
        public ulong PlantTargetHeightEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-height-update-reason")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootPlantTargetHeightUpdateReason PlantTargetHeightUpdateReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-force-refreshed")]
        [DiagnosticGroup("response-contact")]
        public bool PlantTargetForceRefreshed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-force-refresh-distance")]
        [DiagnosticGroup("response-contact")]
        public float PlantTargetForceRefreshDistance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-target-vertical-clamped")]
        [DiagnosticGroup("response-contact")]
        public bool PlantTargetVerticalClamped { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-previous-selected-world-target")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantPreviousSelectedWorldTarget { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-selected-world-target")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantSelectedWorldTarget { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-response-output-available")]
        [DiagnosticGroup("response-contact")]
        public bool PreviousResponseOutputAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-previous-response-output-point")]
        [DiagnosticGroup("response-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-previous-response-output-available")]
        public Vector3 PreviousResponseOutputPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-desired-output-point")]
        [DiagnosticGroup("response-contact")]
        public Vector3 DesiredOutputPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-response-output-point")]
        [DiagnosticGroup("response-contact")]
        public Vector3 ResponseOutputPoint { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-residual-capture-reason")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootPlantResidualCaptureReason PlantResidualCaptureReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-before-capture")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantWorldResidualBeforeCapture { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-captured-before-decay")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantWorldResidualCapturedBeforeDecay { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-decay-applied")]
        [DiagnosticGroup("response-contact")]
        public bool PlantWorldResidualDecayApplied { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-base-half-life-seconds")]
        [DiagnosticGroup("response-contact")]
        public float PlantWorldResidualBaseHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-deadline-half-life-available")]
        [DiagnosticGroup("response-contact")]
        public bool PlantWorldResidualDeadlineHalfLifeAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-deadline-half-life-seconds")]
        [DiagnosticGroup("response-contact")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Key, "foot-motion-plant-world-residual-deadline-half-life-available")]
        public float PlantWorldResidualDeadlineHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-applied-half-life-seconds")]
        [DiagnosticGroup("response-contact")]
        public float PlantWorldResidualAppliedHalfLifeSeconds { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-after-decay")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantWorldResidualAfterDecay { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-completion-tolerance")]
        [DiagnosticGroup("response-contact")]
        public float PlantWorldResidualCompletionTolerance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-cleared-at-completion-tolerance")]
        [DiagnosticGroup("response-contact")]
        public bool PlantWorldResidualClearedAtCompletionTolerance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-evaluated")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseEvaluated { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-initialized-before")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseInitializedBefore { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-initialized-this-frame")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseInitializedThisFrame { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-initialization-reason")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootCorrectionResponseInitializationReason CorrectionResponseInitializationReason { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-desired")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseDesired { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-requested-direction")]
        [DiagnosticGroup("response-contact")]
        public Vector3 CorrectionResponseRequestedDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-previous-direction")]
        [DiagnosticGroup("response-contact")]
        public Vector3 CorrectionResponsePreviousDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-direction-limited")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseDirectionLimited { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-maximum-direction-change-degrees")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseMaximumDirectionChangeDegrees { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-applied-direction-change-degrees")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseAppliedDirectionChangeDegrees { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-visible-output-transferred")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseVisibleOutputTransferred { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-before-rebase")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseBeforeRebase { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-previous")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponsePrevious { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-current")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseCurrent { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-direction")]
        [DiagnosticGroup("response-contact")]
        public Vector3 CorrectionResponseDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-delta-direction")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootCorrectionResponseDeltaDirection CorrectionResponseDeltaDirection { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-selected-speed")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseSelectedSpeed { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-applied-delta")]
        [DiagnosticGroup("response-contact")]
        public float CorrectionResponseAppliedDelta { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-domain")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootCorrectionResponseDomain CorrectionResponseDomain { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-previous-domain")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootCorrectionResponseDomain CorrectionResponsePreviousDomain { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-correction-response-domain-transferred")]
        [DiagnosticGroup("response-contact")]
        public bool CorrectionResponseDomainTransferred { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-vertical-continuity-owners")]
        [DiagnosticGroup("response-contact")]
        public CharacterFootVerticalContinuityOwner PlantVerticalContinuityOwners { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-effective-correction-before")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantEffectiveCorrectionBefore { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-effective-correction-after")]
        [DiagnosticGroup("response-contact")]
        public Vector3 PlantEffectiveCorrectionAfter { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-output-distance")]
        [DiagnosticGroup("response-contact")]
        public float PlantOutputDistance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-penetration-depth")]
        [DiagnosticGroup("response-contact")]
        public float PlantPenetrationDepth { get; }
    }

    public readonly struct CharacterFootSwingMotionDiagnostics
    {
        internal CharacterFootSwingMotionDiagnostics(in CharacterFootSwingMotionResult result)
        {
            Core = new CharacterFootSwingCoreDiagnostics(in result);
            CharacterFootLifecycleTransitionFact lifecycle =
                result.LifecycleTransition;
            Lifecycle = new CharacterFootLifecycleTransitionDiagnostics(in lifecycle);
            CharacterFootPathContinuityFact path = result.PathContinuity;
            PathContinuity = new CharacterFootPathContinuityDiagnostics(in path);
            OutputStages = new CharacterFootOutputStagesDiagnostics(
                in lifecycle, in path);
            Response = new CharacterFootCorrectionResponseDiagnostics(in path);
            CharacterFootSupportTarget selectedSupportTarget = path.SelectedSupportTarget;
            SelectedSupportTarget = new CharacterFootSupportTargetDiagnostics(in selectedSupportTarget);
        }

        public CharacterFootSwingCoreDiagnostics Core { get; }
        public CharacterFootLifecycleTransitionDiagnostics Lifecycle { get; }
        public CharacterFootPathContinuityDiagnostics PathContinuity { get; }
        public CharacterFootOutputStagesDiagnostics OutputStages { get; }
        public CharacterFootCorrectionResponseDiagnostics Response { get; }
        public CharacterFootSupportTargetDiagnostics SelectedSupportTarget { get; }
        public bool Accepted => Core.Accepted;
    }

    internal static class CharacterFootSwingMotionBuilder
    {
        const float GeometryEpsilon = 0.0001f;
        const float EndpointTolerance = 0.005f;

        internal static CharacterFootSwingMotionResult Build(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationFootMotionRuntimeSample step,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathResult groundPath,
            bool formalFootHeightAvailable,
            float formalFootHeight,
            float landingPredictionError)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            ulong landingEventIdentity = step.IsValid ? step.LandingEventIdentity : 0;
            if (!step.IsValid || !step.IsAuthoritative)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepUnavailable,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.IsPreSwing && !step.IsSwing)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepNotSwing,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.HasConsistentLandingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!formalFootHeightAvailable ||
                !float.IsFinite(formalFootHeight) ||
                formalFootHeight < 0f)
            {
                return Rejected(
                    CharacterFootSwingMotionRejectReason.FormalFootHeightUnavailable,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            }
            return BuildForSwing(
                animatedFoot,
                in step,
                landingEventIdentity,
                footPlacementWeight,
                componentUp,
                in groundPath,
                formalFootHeight,
                landingPredictionError);
        }

        internal static CharacterFootSwingMotionResult BuildForSwing(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationFootMotionRuntimeSample step,
            ulong landingEventIdentity,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathResult groundPath,
            float formalFootHeight,
            float landingPredictionError)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidComponentUp,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(footPlacementWeight) || footPlacementWeight < 0f || footPlacementWeight > 1f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(landingPredictionError) || landingPredictionError < 0f ||
                !float.IsFinite(formalFootHeight) || formalFootHeight < 0f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.HasPredictiveLanding || !float.IsFinite(step.SwingProgress))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidSwingPhase,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!groundPath.Accepted)
                return Rejected(
                    groundPath.RejectReason == CharacterFootGroundPathRejectReason.UnreachableEdge
                        ? CharacterFootSwingMotionRejectReason.UnreachableEdge
                        : CharacterFootSwingMotionRejectReason.GroundPathRejected,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.NextSwingLandingEventIdentity != landingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.EnvelopeVertexCount < 2 ||
                !Finite(groundPath.LastLanding) ||
                !Finite(groundPath.NextSwingLanding))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidEnvelope,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                groundPath.NextSwingLanding - groundPath.LastLanding,
                up);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.DegeneratePath,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (Vector3.Distance(
                    groundPath.EnvelopeVertexAt(0).Position,
                    groundPath.LastLanding) > EndpointTolerance ||
                Vector3.Distance(
                    groundPath.EnvelopeVertexAt(groundPath.EnvelopeVertexCount - 1).Position,
                    groundPath.NextSwingLanding) > EndpointTolerance)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.EnvelopeEndpointMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            float progress = Mathf.Clamp01(Vector3.Dot(
                originalSole - groundPath.LastLanding,
                horizontal) / horizontal.sqrMagnitude);
            float distance = pathLength * progress;
            Vector3 baselineSample = Vector3.Lerp(
                groundPath.LastLanding,
                groundPath.NextSwingLanding,
                progress);
            if (!TrySampleFootEnvelope(
                    groundPath,
                    in animatedFoot,
                    progress,
                    up,
                    out Vector3 envelopeSample,
                    out float centerEnvelopeHeight,
                    out CharacterFootSwingMotionRejectReason sampleRejectReason))
                return Rejected(
                    sampleRejectReason,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample);

            float originalSoleHeight = Vector3.Dot(originalSole, up);
            float envelopeMinimumCorrection = Vector3.Dot(
                envelopeSample,
                up) - originalSoleHeight;
            float formalTargetHeightAlongUp = Mathf.Max(
                centerEnvelopeHeight + formalFootHeight,
                Vector3.Dot(envelopeSample, up));
            float formalTargetCorrection =
                formalTargetHeightAlongUp - originalSoleHeight;
            if (!float.IsFinite(envelopeMinimumCorrection) ||
                !float.IsFinite(formalTargetCorrection))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.NegativeVerticalCorrection,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample,
                    envelopeSample);
            float verticalCorrection = formalTargetCorrection;
            Vector3 correctedSole = originalSole + up * verticalCorrection;
            Vector3 correctedAnkle = originalAnkle + up * verticalCorrection;
            float positionWeight = footPlacementWeight;
            return new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                landingEventIdentity,
                groundPath.InputIdentity,
                new CharacterFootSwingPathReference(
                    landingEventIdentity,
                    groundPath.NextSwingLanding),
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                formalTargetHeightAlongUp,
                verticalCorrection,
                landingPredictionError,
                correctedSole,
                correctedAnkle,
                positionWeight,
                0f);
        }

        internal static CharacterFootSwingMotionResult SuppressUnselected(
            in CharacterFootSwingMotionResult motion)
        {
            if (!motion.Accepted)
                return motion;
            return new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Rejected,
                CharacterFootSwingMotionRejectReason.UnselectedSwing,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                default,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.FormalTargetHeightAlongUp,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.OriginalSole,
                motion.OriginalAnkle,
                0f,
                0f);
        }

        internal static CharacterFootSwingMotionResult WithLandingReach(
            in CharacterFootSwingMotionResult motion,
            bool evaluated,
            bool available) =>
            new CharacterFootSwingMotionResult(
                motion.State,
                motion.RejectReason,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                motion.SwingPathReference,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.FormalTargetHeightAlongUp,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.CorrectedSole,
                motion.CorrectedAnkle,
                motion.PositionWeight,
                motion.RotationWeight,
                motion.ConstraintState,
                motion.LockResponse,
                motion.SupportHorizontalError,
                motion.ContactOwnership,
                motion.SupportWeight,
                motion.SupportContactAnchor,
                motion.DesiredCorrection,
                motion.ContactPlaneAvailable,
                motion.ContactSurfaceIdentity,
                motion.ContactPlaneNormal,
                motion.PathContinuity,
                evaluated,
                available,
                motion.LifecycleTransition);

        internal static CharacterFootSwingMotionResult WithPathContinuity(
            in CharacterFootSwingMotionResult motion,
            in CharacterFootPathContinuityFact continuity) =>
            new CharacterFootSwingMotionResult(
                motion.State,
                motion.RejectReason,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                motion.SwingPathReference,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.FormalTargetHeightAlongUp,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.CorrectedSole,
                motion.CorrectedAnkle,
                motion.PositionWeight,
                motion.RotationWeight,
                motion.ConstraintState,
                motion.LockResponse,
                motion.SupportHorizontalError,
                motion.ContactOwnership,
                motion.SupportWeight,
                motion.SupportContactAnchor,
                motion.DesiredCorrection,
                motion.ContactPlaneAvailable,
                motion.ContactSurfaceIdentity,
                motion.ContactPlaneNormal,
                continuity,
                motion.LandingReachEvaluated,
                motion.LandingReachAvailable,
                motion.LifecycleTransition);

        internal static CharacterFootSwingMotionResult WithLifecycleTransition(
            in CharacterFootSwingMotionResult motion,
            in CharacterFootLifecycleTransitionFact lifecycleTransition) =>
            new CharacterFootSwingMotionResult(
                motion.State,
                motion.RejectReason,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                motion.SwingPathReference,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.FormalTargetHeightAlongUp,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.CorrectedSole,
                motion.CorrectedAnkle,
                motion.PositionWeight,
                motion.RotationWeight,
                motion.ConstraintState,
                motion.LockResponse,
                motion.SupportHorizontalError,
                motion.ContactOwnership,
                motion.SupportWeight,
                motion.SupportContactAnchor,
                motion.DesiredCorrection,
                motion.ContactPlaneAvailable,
                motion.ContactSurfaceIdentity,
                motion.ContactPlaneNormal,
                motion.PathContinuity,
                motion.LandingReachEvaluated,
                motion.LandingReachAvailable,
                lifecycleTransition);

        static bool TrySampleFootEnvelope(
            in CharacterFootGroundPathResult groundPath,
            in CharacterFootPlacementAnimatedFootPose foot,
            float progress,
            Vector3 up,
            out Vector3 sample,
            out float centerHeight,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            centerHeight = 0f;
            if (!TrySampleEnvelope(in groundPath, progress, up, out sample, out rejectReason))
                return false;
            centerHeight = Vector3.Dot(sample, up);
            Vector3 sole = (foot.HeelPosition + foot.ToePosition) * 0.5f;
            Vector3 axis = Vector3.ProjectOnPlane(
                groundPath.NextSwingLanding - groundPath.LastLanding, up);
            float heelProgress = Vector3.Dot(foot.HeelPosition - groundPath.LastLanding, axis) /
                axis.sqrMagnitude;
            float toeProgress = Vector3.Dot(foot.ToePosition - groundPath.LastLanding, axis) /
                axis.sqrMagnitude;
            if (!TrySampleEnvelope(in groundPath, heelProgress, up,
                    out Vector3 heel, out rejectReason) ||
                !TrySampleEnvelope(in groundPath, toeProgress, up,
                    out Vector3 toe, out rejectReason))
                return false;
            float height = Mathf.Max(centerHeight,
                Mathf.Max(Vector3.Dot(heel - (foot.HeelPosition - sole), up),
                    Vector3.Dot(toe - (foot.ToePosition - sole), up)));
            sample += up * (height - Vector3.Dot(sample, up));
            return true;
        }

        static bool TrySampleEnvelope(
            in CharacterFootGroundPathResult groundPath,
            float progress,
            Vector3 up,
            out Vector3 sample,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            Vector3 start = groundPath.LastLanding;
            Vector3 axis = Vector3.ProjectOnPlane(
                groundPath.NextSwingLanding - start, up);
            float pathLength = axis.magnitude;
            axis /= pathLength;
            float targetDistance = Mathf.Clamp01(progress) * pathLength;
            Vector3 previous = groundPath.EnvelopeVertexAt(0).Position;
            float previousDistance = Vector3.Dot(previous - start, axis);
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                float currentDistance = Vector3.Dot(current - start, axis);
                if (!Finite(previous) || !Finite(current) ||
                    currentDistance < previousDistance - GeometryEpsilon)
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                if (targetDistance <= currentDistance)
                {
                    float span = currentDistance - previousDistance;
                    sample = span > GeometryEpsilon
                        ? Vector3.Lerp(previous, current,
                            Mathf.Clamp01((targetDistance - previousDistance) / span))
                        : Vector3.Dot(previous, up) >= Vector3.Dot(current, up)
                            ? previous : current;
                    rejectReason = CharacterFootSwingMotionRejectReason.None;
                    return true;
                }
                previous = current;
                previousDistance = currentDistance;
            }
            sample = previous;
            rejectReason = CharacterFootSwingMotionRejectReason.None;
            return true;
        }

        static CharacterFootSwingMotionResult Rejected(
            CharacterFootSwingMotionRejectReason reason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance = 0f,
            float progress = 0f,
            Vector3 baselineSample = default,
            Vector3 envelopeSample = default,
            float formalTargetHeightAlongUp = 0f,
            float verticalCorrection = 0f,
            float landingPredictionError = 0f) =>
            new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Rejected,
                reason,
                landingEventIdentity,
                groundPathInputIdentity,
                default,
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                formalTargetHeightAlongUp,
                verticalCorrection,
                landingPredictionError,
                originalSole,
                originalAnkle,
                0f,
                0f);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
