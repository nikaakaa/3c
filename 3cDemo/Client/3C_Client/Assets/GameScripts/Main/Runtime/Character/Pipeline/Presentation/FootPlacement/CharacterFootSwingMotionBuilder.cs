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

        [DiagnosticField("character-foot-ik/main/resolved-frame-sequence", 1, "frame", "resolved-core")]
        public ulong FrameSequence { get; }

        [DiagnosticField("character-foot-ik/main/resolved-completion-identity", 1, "identity", "resolved-core")]
        public ulong CompletionIdentity { get; }

        [DiagnosticField("character-foot-ik/main/resolved-rig-id", 1, "identity", "resolved-core")]
        public string RigId { get; }

        [DiagnosticField("character-foot-ik/main/resolved-rig-revision", 1, "identity", "resolved-core")]
        public string RigRevision { get; }

        [DiagnosticField("character-foot-ik/main/resolved-side", 1, "category", "resolved-core")]
        public CharacterFootSide Side { get; }

        [DiagnosticField("character-foot-ik/main/resolved-outcome", 1, "category", "resolved-core")]
        public CharacterFootResolvedOutcome Outcome { get; }

        [DiagnosticField("character-foot-ik/main/resolved-final-sole", 1, "metres", "resolved-core")]
        public Vector3 FinalSole { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-sole", 1, "metres", "resolved-core")]
        public Vector3 EffectiveSole { get; }

        [DiagnosticField("character-foot-ik/main/resolved-goal-target-ankle", 1, "metres", "resolved-core")]
        public Vector3 GoalTargetAnkle { get; }

        [DiagnosticField("character-foot-ik/main/resolved-goal-target-rotation", 1, "unitless", "resolved-core")]
        public Quaternion GoalTargetRotation { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-ankle", 1, "metres", "resolved-core")]
        public Vector3 EffectiveAnkle { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-rotation", 1, "unitless", "resolved-core")]
        public Quaternion EffectiveRotation { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-heel", 1, "metres", "resolved-core")]
        public Vector3 EffectiveHeel { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-toe", 1, "metres", "resolved-core")]
        public Vector3 EffectiveToe { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-sole-from-contacts", 1, "metres", "resolved-core")]
        public Vector3 EffectiveSoleFromContacts { get; }

        [DiagnosticField("character-foot-ik/main/resolved-source-sole-forward", 1, "direction", "resolved-core")]
        public Vector3 SourceSoleForward { get; }

        [DiagnosticField("character-foot-ik/main/resolved-source-sole-frame-local-rotation", 1, "unitless", "resolved-core")]
        public Quaternion SourceSoleFrameLocalRotation { get; }

        [DiagnosticField("character-foot-ik/main/resolved-goal-target-correction", 1, "metres", "resolved-core")]
        public Vector3 GoalTargetCorrection { get; }

        [DiagnosticField("character-foot-ik/main/resolved-effective-sole-correction", 1, "metres", "resolved-core")]
        public Vector3 EffectiveSoleCorrection { get; }

        [DiagnosticField("character-foot-ik/main/resolved-position-weight", 1, "unitless", "resolved-core")]
        public float PositionWeight { get; }

        [DiagnosticField("character-foot-ik/main/resolved-rotation-weight", 1, "unitless", "resolved-core")]
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

        [DiagnosticField("character-foot-ik/main/resolved-contact-available", 1, "none", "resolved-contact")]
        public bool Available { get; }

        [DiagnosticField("character-foot-ik/main/resolved-contact-event-identity", 1, "identity", "resolved-contact")]
        public ulong EventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/resolved-contact-point", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-contact-available")]
        public Vector3 Point { get; }

        [DiagnosticField("character-foot-ik/main/resolved-contact-ownership", 1, "unitless", "resolved-contact")]
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

        [DiagnosticField("character-foot-ik/main/resolved-support-eligibility", 1, "category", "resolved-contact")]
        public CharacterFootSupportEligibility Eligibility { get; }

        [DiagnosticField(1, "unitless", "resolved-contact")]
        public float Weight { get; }

        [DiagnosticField("character-foot-ik/main/resolved-support-horizontal-error", 1, "metres", "resolved-contact")]
        public float HorizontalError { get; }

        [DiagnosticField("character-foot-ik/main/resolved-support-event-identity", 1, "identity", "resolved-contact")]
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

        [DiagnosticField("character-foot-ik/main/resolved-pelvis-reach-available", 1, "none", "resolved-contact")]
        public bool PelvisAvailable { get; }

        [DiagnosticField("character-foot-ik/main/resolved-pelvis-reach-event-identity", 1, "identity", "resolved-contact")]
        public ulong PelvisEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/resolved-pelvis-reach-point", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-pelvis-reach-available")]
        public Vector3 PelvisPoint { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-available", 1, "none", "resolved-contact")]
        public bool LandingAvailable { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-event-identity", 1, "identity", "resolved-contact")]
        public ulong LandingEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-hip", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        public Vector3 LandingHip { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-target-ankle", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        public Vector3 LandingTargetAnkle { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-leg-length", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        public float LandingLegLength { get; }

        [DiagnosticField("character-foot-ik/main/resolved-landing-reach-minimum-compression-reserve", 1, "metres", "resolved-contact", AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
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

        [DiagnosticField("character-foot-ik/main/foot-motion-state", 1, "category", "motion-core")]
        public CharacterFootSwingMotionState State { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-reject-reason", 1, "category", "motion-core")]
        public CharacterFootSwingMotionRejectReason RejectReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-landing-event-identity", 1, "identity", "motion-core")]
        public ulong LandingEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-ground-path-input-identity", 1, "identity", "motion-core")]
        public ulong GroundPathInputIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-original-sole", 1, "metres", "motion-core")]
        public Vector3 OriginalSole { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-original-ankle", 1, "metres", "motion-core")]
        public Vector3 OriginalAnkle { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-distance", 1, "metres", "motion-core")]
        public float Distance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-progress", 1, "unitless", "motion-core")]
        public float Progress { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-baseline-sample", 1, "metres", "motion-core")]
        public Vector3 BaselineSample { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-envelope-sample", 1, "metres", "motion-core")]
        public Vector3 EnvelopeSample { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-landing-prediction-error", 1, "metres", "motion-core")]
        public float LandingPredictionError { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-corrected-sole", 1, "metres", "motion-core")]
        public Vector3 CorrectedSole { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-corrected-ankle", 1, "metres", "motion-core")]
        public Vector3 CorrectedAnkle { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-position-weight", 1, "unitless", "motion-core")]
        public float PositionWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-rotation-weight", 1, "unitless", "motion-core")]
        public float RotationWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-constraint-state", 1, "category", "motion-core")]
        public CharacterFootConstraintState ConstraintState { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-lock-response", 1, "category", "motion-core")]
        public CharacterFootLockResponse LockResponse { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-support-horizontal-error", 1, "metres", "motion-core")]
        public float SupportHorizontalError { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-contact-ownership", 1, "unitless", "motion-core")]
        public float ContactOwnership { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-support-weight", 1, "unitless", "motion-core")]
        public float SupportWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-support-contact-anchor", 1, "metres", "motion-core")]
        public Vector3 SupportContactAnchor { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-desired-correction", 1, "metres", "motion-core")]
        public Vector3 DesiredCorrection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-contact-plane-available", 1, "none", "motion-core")]
        public bool ContactPlaneAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-contact-surface-identity", 1, "identity", "motion-core", AvailabilityFieldId = "character-foot-ik/main/foot-motion-contact-plane-available")]
        public int ContactSurfaceIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-contact-plane-normal", 1, "direction", "motion-core", AvailabilityFieldId = "character-foot-ik/main/foot-motion-contact-plane-available")]
        public Vector3 ContactPlaneNormal { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-landing-reach-evaluated", 1, "none", "motion-core")]
        public bool LandingReachEvaluated { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-landing-reach-available", 1, "none", "motion-core")]
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

        [DiagnosticField("character-foot-ik/main/foot-motion-lifecycle-transition-evaluated", 1, "none", "lifecycle")]
        public bool LifecycleTransitionEvaluated { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-lock-request-available", 1, "none", "lifecycle")]
        public bool PreviousLockRequestAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-lock-requested", 1, "none", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-lock-request-available")]
        public bool PreviousLockRequested { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-lock-request-event-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-lock-request-available")]
        public ulong PreviousLockRequestEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-lock-request-mode", 1, "category", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-lock-request-available")]
        public AnimationFootStepObservationLockMode PreviousLockRequestMode { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-lock-request-weight", 1, "unitless", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-lock-request-available")]
        public float PreviousLockRequestWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-edge-seconds", 1, "seconds", "lifecycle")]
        public float PreviousContactEdgeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-latest-contact-event-identity", 1, "identity", "lifecycle")]
        public ulong PreviousLatestContactEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-latest-released-contact-event-identity", 1, "identity", "lifecycle")]
        public ulong PreviousLatestReleasedContactEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-completed-lock-weight-event-identity", 1, "identity", "lifecycle")]
        public ulong PreviousCompletedLockWeightEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-available", 1, "none", "lifecycle")]
        public bool PreviousContactAnchorAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-event-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-acquired-frame-sequence", 1, "frame", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorAcquiredFrameSequence { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-acquired-completion-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorAcquiredCompletionIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-world-revision", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public ulong PreviousContactAnchorWorldRevision { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-surface-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public int PreviousContactAnchorSurfaceIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-point", 1, "metres", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public Vector3 PreviousContactAnchorPoint { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-contact-anchor-normal", 1, "direction", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-contact-anchor-available")]
        public Vector3 PreviousContactAnchorNormal { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-lock-requested", 1, "none", "lifecycle")]
        public bool CurrentLockRequested { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-lock-request-event-identity", 1, "identity", "lifecycle")]
        public ulong CurrentLockRequestEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-lock-request-mode", 1, "category", "lifecycle")]
        public AnimationFootStepObservationLockMode CurrentLockRequestMode { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-lock-request-weight", 1, "unitless", "lifecycle")]
        public float CurrentLockRequestWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-lock-request-availability", 1, "category", "lifecycle")]
        public CharacterFootLockRequestAvailability CurrentLockRequestAvailability { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-contact-edge", 1, "category", "lifecycle")]
        public CharacterFootContactEdge ContactEdge { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-edge-seconds", 1, "seconds", "lifecycle")]
        public float CurrentContactEdgeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-latest-contact-event-identity", 1, "identity", "lifecycle")]
        public ulong CurrentLatestContactEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-latest-released-contact-event-identity", 1, "identity", "lifecycle")]
        public ulong CurrentLatestReleasedContactEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-completed-lock-weight-event-identity", 1, "identity", "lifecycle")]
        public ulong CurrentCompletedLockWeightEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-available", 1, "none", "lifecycle")]
        public bool CurrentContactAnchorAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-event-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-acquired-frame-sequence", 1, "frame", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorAcquiredFrameSequence { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-acquired-completion-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorAcquiredCompletionIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-world-revision", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public ulong CurrentContactAnchorWorldRevision { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-surface-identity", 1, "identity", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public int CurrentContactAnchorSurfaceIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-point", 1, "metres", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public Vector3 CurrentContactAnchorPoint { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-current-contact-anchor-normal", 1, "direction", "lifecycle", AvailabilityFieldId = "character-foot-ik/main/foot-motion-current-contact-anchor-available")]
        public Vector3 CurrentContactAnchorNormal { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-same-event-contact-reentry-refreshed", 1, "none", "lifecycle")]
        public bool SameEventContactReentryRefreshed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-same-event-contact-reentry-unavailable", 1, "none", "lifecycle")]
        public bool SameEventContactReentryUnavailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-retained-verified-anchor", 1, "none", "lifecycle")]
        public bool RetainedVerifiedAnchor { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-reentry-interpolation-history-retained", 1, "none", "lifecycle")]
        public bool ReentryInterpolationHistoryRetained { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-formal-foot-placement-weight", 1, "unitless", "lifecycle")]
        public float FormalFootPlacementWeight { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-hard-ownership-loss", 1, "none", "lifecycle")]
        public bool HardOwnershipLoss { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-hard-ownership-loss-reason", 1, "category", "lifecycle")]
        public CharacterFootGoalOwnershipLossReason HardOwnershipLossReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-suppress-output", 1, "none", "lifecycle")]
        public bool PreTransitionSuppressOutput { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-reset-interpolation", 1, "none", "lifecycle")]
        public bool PreTransitionResetInterpolation { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-evaluated", 1, "none", "lifecycle")]
        public bool PostTransitionEvaluated { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-suppress-output", 1, "none", "lifecycle")]
        public bool PostTransitionSuppressOutput { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-reset-interpolation", 1, "none", "lifecycle")]
        public bool PostTransitionResetInterpolation { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-reason", 1, "category", "lifecycle")]
        public CharacterFootTransitionReason PreTransitionReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-source", 1, "category", "lifecycle")]
        public CharacterFootConstraintState PreTransitionSource { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-target", 1, "category", "lifecycle")]
        public CharacterFootConstraintState PreTransitionTarget { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-pre-transition-anchor-command", 1, "category", "lifecycle")]
        public CharacterFootAnchorCommand PreTransitionAnchorCommand { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-reason", 1, "category", "lifecycle")]
        public CharacterFootTransitionReason PostTransitionReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-source", 1, "category", "lifecycle")]
        public CharacterFootConstraintState PostTransitionSource { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-target", 1, "category", "lifecycle")]
        public CharacterFootConstraintState PostTransitionTarget { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-post-transition-anchor-command", 1, "category", "lifecycle")]
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

        [DiagnosticField("character-foot-ik/main/foot-motion-path-continuity-evaluated", 1, "none", "path-continuity")]
        public bool PathContinuityEvaluated { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-revision-reason", 1, "category", "path-continuity")]
        public CharacterFootPathRevisionReason PathRevisionReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-residual-rebuilt", 1, "none", "path-continuity")]
        public bool PathResidualRebuilt { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-target-tracking-applied", 1, "none", "path-continuity")]
        public bool TargetTrackingApplied { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-available-before", 1, "none", "path-continuity")]
        public bool PathAvailableBefore { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-available-after", 1, "none", "path-continuity")]
        public bool PathAvailableAfter { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-previous-landing-event-identity", 1, "identity", "path-continuity")]
        public ulong PathPreviousLandingEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-current-landing-event-identity", 1, "identity", "path-continuity")]
        public ulong PathCurrentLandingEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-previous-target-correction", 1, "metres", "path-continuity", AvailabilityFieldId = "character-foot-ik/main/foot-motion-path-available-before")]
        public Vector3 PathPreviousTargetCorrection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-current-target-correction", 1, "metres", "path-continuity", AvailabilityFieldId = "character-foot-ik/main/foot-motion-path-available-after")]
        public Vector3 PathCurrentTargetCorrection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-landing-point-delta-meters", 1, "metres", "path-continuity")]
        public float PathLandingPointDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-target-delta-meters", 1, "metres", "path-continuity")]
        public float PathTargetDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-residual-before-revision", 1, "metres", "path-continuity")]
        public Vector3 SwingResidualBeforeRevision { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-residual-before-decay", 1, "metres", "path-continuity")]
        public Vector3 SwingResidualBeforeDecay { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-residual-after-decay", 1, "metres", "path-continuity")]
        public Vector3 SwingResidualAfterDecay { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-output-correction", 1, "metres", "path-continuity")]
        public Vector3 ResidualOutputCorrection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-landing-acceptance-distance", 1, "metres", "path-continuity")]
        public float LandingAcceptanceDistance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-path-revision-distance", 1, "metres", "path-continuity")]
        public float PathRevisionDistance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-residual-tolerance", 1, "metres", "path-continuity")]
        public float SwingResidualTolerance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-time-to-landing-seconds", 1, "seconds", "path-continuity")]
        public float ResidualTimeToLandingSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-base-half-life-seconds", 1, "seconds", "path-continuity")]
        public float ResidualBaseHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-deadline-half-life-available", 1, "none", "path-continuity")]
        public bool ResidualDeadlineHalfLifeAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-deadline-half-life-seconds", 1, "seconds", "path-continuity", AvailabilityFieldId = "character-foot-ik/main/foot-motion-residual-deadline-half-life-available")]
        public float ResidualDeadlineHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-residual-applied-half-life-seconds", 1, "seconds", "path-continuity")]
        public float ResidualAppliedHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-raw-target-height-along-up", 1, "metres", "path-continuity")]
        public float SwingRawTargetHeightAlongUp { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-filtered-target-height-before", 1, "metres", "path-continuity")]
        public float SwingFilteredTargetHeightBefore { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-delta", 1, "metres", "path-continuity")]
        public float SwingTargetHeightDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-applied-delta", 1, "metres", "path-continuity")]
        public float SwingTargetHeightAppliedDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-update-held", 1, "none", "path-continuity")]
        public bool SwingTargetHeightUpdateHeld { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-force-refreshed", 1, "none", "path-continuity")]
        public bool SwingTargetHeightForceRefreshed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-rate-limited", 1, "none", "path-continuity")]
        public bool SwingTargetHeightRateLimited { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-clamped", 1, "none", "path-continuity")]
        public bool SwingTargetHeightClamped { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-force-refresh-distance", 1, "metres", "path-continuity")]
        public float SwingTargetHeightForceRefreshDistance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-maximum-vertical-speed", 1, "metres-per-second", "path-continuity")]
        public float SwingTargetMaximumVerticalSpeed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-target-height-adoption-mode", 1, "category", "path-continuity")]
        public CharacterFootTargetHeightAdoptionMode SwingTargetHeightAdoptionMode { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-swing-filtered-target-height-along-up", 1, "metres", "path-continuity")]
        public float SwingFilteredTargetHeightAlongUp { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-target-height-component-up", 1, "direction", "path-continuity")]
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

        [DiagnosticField(1, "metres", "output-stages")]
        public Vector3 StateTargetCorrection { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public Vector3 InterpolationOutputCorrection { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool InterpolationCompleted { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootConstraintState ConstraintStateBefore { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootLockResponse LockResponseBefore { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool OutputStagesAvailable { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool ReleasingCompletedToSwing { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool SafetyFloorAvailable { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootSafetyFloorOwner SafetyFloorOwner { get; }
        [DiagnosticField(1, "identity", "output-stages")]
        public int SafetyFloorOwnerSurfaceIdentity { get; }
        [DiagnosticField(1, "identity", "output-stages")]
        public ulong SafetyFloorOwnerPathIdentity { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public Vector3 CorrectionBeforeSafetyFloor { get; }
        [DiagnosticField(1, "metres", "output-stages", AvailabilityMember = nameof(SafetyFloorAvailable))]
        public Vector3 SafetyFloorMinimumCorrection { get; }
        [DiagnosticField(1, "metres", "output-stages", AvailabilityMember = nameof(SafetyFloorAvailable))]
        public Vector3 SafetyFloorOutputCorrection { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public Vector3 FinalEffectiveCorrection { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool SafetyFloorClamped { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public float SafetyFloorClampMeters { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public float SafetyFloorClearanceBeforeMeters { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public float SafetyFloorClearanceAfterMeters { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool PlantInterpolationEvaluated { get; }
        [DiagnosticField(1, "identity", "output-stages")]
        public ulong PlantTargetEventIdentity { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool PlantTargetVerified { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootPlantTargetKind PlantTargetKind { get; }
        [DiagnosticField(1, "category", "output-stages")]
        public CharacterFootLockResponse PlantLockResponse { get; }
        [DiagnosticField(1, "none", "output-stages")]
        public bool PlantLockWeightCompleted { get; }
        [DiagnosticField(1, "metres", "output-stages")]
        public Vector3 PlantDesiredPoint { get; }
        [DiagnosticField(1, "metres", "output-stages")]
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

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-adoption-mode", 1, "category", "response-contact")]
        public CharacterFootTargetHeightAdoptionMode PlantTargetHeightAdoptionMode { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-maximum-vertical-speed", 1, "metres-per-second", "response-contact")]
        public float PlantTargetMaximumVerticalSpeed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-before", 1, "metres", "response-contact")]
        public float PlantTargetHeightBefore { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-target", 1, "metres", "response-contact")]
        public float PlantTargetHeightTarget { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-vertical-delta", 1, "metres", "response-contact")]
        public float PlantTargetVerticalDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-applied-vertical-delta", 1, "metres", "response-contact")]
        public float PlantTargetAppliedVerticalDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-after", 1, "metres", "response-contact")]
        public float PlantTargetHeightAfter { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-event-identity", 1, "identity", "response-contact")]
        public ulong PlantTargetHeightEventIdentity { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-height-update-reason", 1, "category", "response-contact")]
        public CharacterFootPlantTargetHeightUpdateReason PlantTargetHeightUpdateReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-force-refreshed", 1, "none", "response-contact")]
        public bool PlantTargetForceRefreshed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-force-refresh-distance", 1, "metres", "response-contact")]
        public float PlantTargetForceRefreshDistance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-target-vertical-clamped", 1, "none", "response-contact")]
        public bool PlantTargetVerticalClamped { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-previous-selected-world-target", 1, "metres", "response-contact")]
        public Vector3 PlantPreviousSelectedWorldTarget { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-selected-world-target", 1, "metres", "response-contact")]
        public Vector3 PlantSelectedWorldTarget { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-response-output-available", 1, "none", "response-contact")]
        public bool PreviousResponseOutputAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-previous-response-output-point", 1, "metres", "response-contact", AvailabilityFieldId = "character-foot-ik/main/foot-motion-previous-response-output-available")]
        public Vector3 PreviousResponseOutputPoint { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-desired-output-point", 1, "metres", "response-contact")]
        public Vector3 DesiredOutputPoint { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-response-output-point", 1, "metres", "response-contact")]
        public Vector3 ResponseOutputPoint { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-residual-capture-reason", 1, "category", "response-contact")]
        public CharacterFootPlantResidualCaptureReason PlantResidualCaptureReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-before-capture", 1, "metres", "response-contact")]
        public Vector3 PlantWorldResidualBeforeCapture { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-captured-before-decay", 1, "metres", "response-contact")]
        public Vector3 PlantWorldResidualCapturedBeforeDecay { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-decay-applied", 1, "none", "response-contact")]
        public bool PlantWorldResidualDecayApplied { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-base-half-life-seconds", 1, "seconds", "response-contact")]
        public float PlantWorldResidualBaseHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-deadline-half-life-available", 1, "none", "response-contact")]
        public bool PlantWorldResidualDeadlineHalfLifeAvailable { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-deadline-half-life-seconds", 1, "seconds", "response-contact", AvailabilityFieldId = "character-foot-ik/main/foot-motion-plant-world-residual-deadline-half-life-available")]
        public float PlantWorldResidualDeadlineHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-applied-half-life-seconds", 1, "seconds", "response-contact")]
        public float PlantWorldResidualAppliedHalfLifeSeconds { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-after-decay", 1, "metres", "response-contact")]
        public Vector3 PlantWorldResidualAfterDecay { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-completion-tolerance", 1, "metres", "response-contact")]
        public float PlantWorldResidualCompletionTolerance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-world-residual-cleared-at-completion-tolerance", 1, "none", "response-contact")]
        public bool PlantWorldResidualClearedAtCompletionTolerance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-evaluated", 1, "none", "response-contact")]
        public bool CorrectionResponseEvaluated { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-initialized-before", 1, "none", "response-contact")]
        public bool CorrectionResponseInitializedBefore { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-initialized-this-frame", 1, "none", "response-contact")]
        public bool CorrectionResponseInitializedThisFrame { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-initialization-reason", 1, "category", "response-contact")]
        public CharacterFootCorrectionResponseInitializationReason CorrectionResponseInitializationReason { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-desired", 1, "metres", "response-contact")]
        public float CorrectionResponseDesired { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-requested-direction", 1, "direction", "response-contact")]
        public Vector3 CorrectionResponseRequestedDirection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-previous-direction", 1, "direction", "response-contact")]
        public Vector3 CorrectionResponsePreviousDirection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-direction-limited", 1, "none", "response-contact")]
        public bool CorrectionResponseDirectionLimited { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-maximum-direction-change-degrees", 1, "degrees", "response-contact")]
        public float CorrectionResponseMaximumDirectionChangeDegrees { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-applied-direction-change-degrees", 1, "degrees", "response-contact")]
        public float CorrectionResponseAppliedDirectionChangeDegrees { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-visible-output-transferred", 1, "none", "response-contact")]
        public bool CorrectionResponseVisibleOutputTransferred { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-before-rebase", 1, "metres", "response-contact")]
        public float CorrectionResponseBeforeRebase { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-previous", 1, "metres", "response-contact")]
        public float CorrectionResponsePrevious { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-current", 1, "metres", "response-contact")]
        public float CorrectionResponseCurrent { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-direction", 1, "direction", "response-contact")]
        public Vector3 CorrectionResponseDirection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-delta-direction", 1, "category", "response-contact")]
        public CharacterFootCorrectionResponseDeltaDirection CorrectionResponseDeltaDirection { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-selected-speed", 1, "metres-per-second", "response-contact")]
        public float CorrectionResponseSelectedSpeed { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-applied-delta", 1, "metres", "response-contact")]
        public float CorrectionResponseAppliedDelta { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-domain", 1, "category", "response-contact")]
        public CharacterFootCorrectionResponseDomain CorrectionResponseDomain { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-previous-domain", 1, "category", "response-contact")]
        public CharacterFootCorrectionResponseDomain CorrectionResponsePreviousDomain { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-correction-response-domain-transferred", 1, "none", "response-contact")]
        public bool CorrectionResponseDomainTransferred { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-vertical-continuity-owners", 1, "category", "response-contact")]
        public CharacterFootVerticalContinuityOwner PlantVerticalContinuityOwners { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-effective-correction-before", 1, "metres", "response-contact")]
        public Vector3 PlantEffectiveCorrectionBefore { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-effective-correction-after", 1, "metres", "response-contact")]
        public Vector3 PlantEffectiveCorrectionAfter { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-output-distance", 1, "metres", "response-contact")]
        public float PlantOutputDistance { get; }

        [DiagnosticField("character-foot-ik/main/foot-motion-plant-penetration-depth", 1, "metres", "response-contact")]
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
            if (!TryResolveSwingPhaseWeight(in step, out float trajectoryProgress))
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

            float progress = trajectoryProgress;
            float distance = pathLength * progress;
            Vector3 baselineSample = Vector3.Lerp(
                groundPath.LastLanding,
                groundPath.NextSwingLanding,
                progress);
            if (!TrySampleEnvelope(
                    groundPath,
                    progress,
                    out Vector3 envelopeSample,
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
            float formalTargetHeightAlongUp = Vector3.Dot(
                envelopeSample,
                up) + formalFootHeight;
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
            float verticalCorrection = Mathf.Max(
                0f,
                formalTargetCorrection);
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

        static bool TryResolveSwingPhaseWeight(
            in AnimationFootMotionRuntimeSample step,
            out float weight)
        {
            if (!step.HasPredictiveLanding ||
                !float.IsFinite(step.SwingProgress))
            {
                weight = 0f;
                return false;
            }
            weight = Mathf.SmoothStep(0f, 1f, step.SwingProgress);
            return float.IsFinite(weight);
        }

        static bool TrySampleEnvelope(
            in CharacterFootGroundPathResult groundPath,
            float progress,
            out Vector3 sample,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            Vector3 previous = groundPath.EnvelopeVertexAt(0).Position;
            if (!Finite(previous))
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                return false;
            }
            float totalLength = 0f;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                if (!Finite(current))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                float segmentLength = Vector3.Distance(previous, current);
                if (!float.IsFinite(segmentLength))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                totalLength += segmentLength;
                previous = current;
            }
            if (!float.IsFinite(totalLength) || totalLength <= GeometryEpsilon)
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.DegeneratePath;
                return false;
            }

            float targetDistance = Mathf.Clamp01(progress) * totalLength;
            if (targetDistance >= totalLength - GeometryEpsilon)
            {
                sample = groundPath.EnvelopeVertexAt(
                    groundPath.EnvelopeVertexCount - 1).Position;
                rejectReason = CharacterFootSwingMotionRejectReason.None;
                return true;
            }

            float accumulatedLength = 0f;
            previous = groundPath.EnvelopeVertexAt(0).Position;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                float segmentLength = Vector3.Distance(previous, current);
                if (segmentLength <= GeometryEpsilon)
                {
                    previous = current;
                    continue;
                }
                if (targetDistance <= accumulatedLength + segmentLength)
                {
                    float t = Mathf.Clamp01(
                        (targetDistance - accumulatedLength) / segmentLength);
                    sample = Vector3.Lerp(previous, current, t);
                    rejectReason = Finite(sample)
                        ? CharacterFootSwingMotionRejectReason.None
                        : CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return rejectReason == CharacterFootSwingMotionRejectReason.None;
                }
                accumulatedLength += segmentLength;
                previous = current;
            }
            sample = groundPath.EnvelopeVertexAt(
                groundPath.EnvelopeVertexCount - 1).Position;
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
