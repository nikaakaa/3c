using System;
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

    public readonly struct CharacterResolvedFootDiagnostics
    {
        internal CharacterResolvedFootDiagnostics(
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
            Vector3 effectiveAnkle = result.Pose.EffectiveAnkle;
            Quaternion effectiveRotation = result.Pose.EffectiveRotation;
            CharacterFootPlacementSoleContactPose contacts =
                source.ResolveSoleContacts(
                    effectiveAnkle,
                    effectiveRotation);
            EffectiveHeel = contacts.HeelPosition;
            EffectiveToe = contacts.ToePosition;
            EffectiveSoleFromContacts =
                (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
            SourceSoleForward = source.SoleForward;
            SourceSoleFrameLocalRotation =
                source.SoleFrameLocalRotation;
            GoalTargetCorrection = result.Pose.GoalTargetCorrection;
            EffectiveSoleCorrection =
                EffectiveSoleFromContacts -
                (source.HeelPosition + source.ToePosition) * 0.5f;
            PositionWeight = result.Pose.GoalWeight;
            RotationWeight = result.Pose.RotationWeight;
            CharacterFootSupportTarget supportTarget = result.Support.Target;
            SupportTarget = new CharacterFootSupportTargetDiagnostics(
                in supportTarget);
            ContactAvailable = result.Support.Contact.IsAvailable;
            ContactEventIdentity = result.Support.Contact.EventIdentity;
            ContactPoint = result.Support.Contact.Point;
            ContactOwnership = result.Support.ContactOwnership;
            SupportEligibility = result.Support.Eligibility;
            SupportWeight = result.Support.Weight;
            SupportIntentWeight = result.Support.Weight;
            SupportHorizontalError = result.Support.HorizontalError;
            SupportEventIdentity = result.Support.EventIdentity;
            PelvisReachAvailable = result.Support.ReachReference.IsAvailable;
            PelvisReachEventIdentity =
                result.Support.ReachReference.EventIdentity;
            PelvisReachPoint = result.Support.ReachReference.Point;
            LandingReachAvailable = result.LandingReachRequest.IsAvailable;
            LandingReachEventIdentity =
                result.LandingReachRequest.EventIdentity;
            LandingReachHip = result.LandingReachRequest.Hip;
            LandingReachTargetAnkle = result.LandingReachRequest.TargetAnkle;
            LandingReachLegLength = result.LandingReachRequest.LegLength;
            LandingReachMinimumCompressionReserve =
                result.LandingReachRequest.MinimumCompressionReserve;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public CharacterFootSide Side { get; }
        public CharacterFootResolvedOutcome Outcome { get; }
        public Vector3 FinalSole { get; }
        public Vector3 EffectiveSole { get; }
        public Vector3 GoalTargetAnkle { get; }
        public Quaternion GoalTargetRotation { get; }
        public Vector3 EffectiveAnkle { get; }
        public Quaternion EffectiveRotation { get; }
        public Vector3 EffectiveHeel { get; }
        public Vector3 EffectiveToe { get; }
        public Vector3 EffectiveSoleFromContacts { get; }
        public Vector3 SourceSoleForward { get; }
        public Quaternion SourceSoleFrameLocalRotation { get; }
        public Vector3 GoalTargetCorrection { get; }
        public Vector3 EffectiveSoleCorrection { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFootSupportTargetDiagnostics SupportTarget { get; }
        public bool ContactAvailable { get; }
        public ulong ContactEventIdentity { get; }
        public Vector3 ContactPoint { get; }
        public float ContactOwnership { get; }
        public CharacterFootSupportEligibility SupportEligibility { get; }
        public float SupportWeight { get; }
        public float SupportIntentWeight { get; }
        public float SupportHorizontalError { get; }
        public ulong SupportEventIdentity { get; }
        public bool PelvisReachAvailable { get; }
        public ulong PelvisReachEventIdentity { get; }
        public Vector3 PelvisReachPoint { get; }
        public bool LandingReachAvailable { get; }
        public ulong LandingReachEventIdentity { get; }
        public Vector3 LandingReachHip { get; }
        public Vector3 LandingReachTargetAnkle { get; }
        public float LandingReachLegLength { get; }
        public float LandingReachMinimumCompressionReserve { get; }
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

    public readonly struct CharacterFootSwingMotionDiagnostics
    {
        internal CharacterFootSwingMotionDiagnostics(
            in CharacterFootSwingMotionResult result)
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
            FormalTargetHeightAlongUp = result.FormalTargetHeightAlongUp;
            VerticalCorrection = result.VerticalCorrection;
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
            CharacterFootLifecycleTransitionFact lifecycle =
                result.LifecycleTransition;
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
            PreviousLockRequestMode = previousContext.RequestMode.ToString();
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
            CurrentLockRequestMode = request.Mode.ToString();
            CurrentLockRequestWeight = request.Weight;
            CurrentLockRequestAvailability = request.Availability.ToString();
            ContactEdge = preTransition.ContactEdge.ToString();
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
            HardOwnershipLossReason = lifecycle.OwnershipLossReason.ToString();
            PreTransitionReason = preTransition.Reason.ToString();
            PreTransitionSource = preTransition.SourceState;
            PreTransitionTarget = preTransition.TargetState;
            PreTransitionAnchorCommand = preTransition.AnchorCommand.ToString();
            PreTransitionSuppressOutput = preTransition.SuppressOutput;
            PreTransitionResetInterpolation = preTransition.ResetInterpolation;
            PostTransitionEvaluated = lifecycle.PostTransitionEvaluated;
            PostTransitionReason = postTransition.Reason.ToString();
            PostTransitionSource = postTransition.SourceState;
            PostTransitionTarget = postTransition.TargetState;
            PostTransitionAnchorCommand = postTransition.AnchorCommand.ToString();
            PostTransitionSuppressOutput = postTransition.SuppressOutput;
            PostTransitionResetInterpolation = postTransition.ResetInterpolation;
            ConstraintStateBefore = preTransition.SourceState;
            LockResponseBefore = lifecycle.LockResponseBefore;
            CharacterFootPathContinuityFact path = result.PathContinuity;
            PathContinuityEvaluated = path.Evaluated;
            PathRevisionReason = path.RevisionReason.ToString();
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
                path.SwingTargetHeightAdoptionMode.ToString();
            SwingFilteredTargetHeightAlongUp =
                path.SwingFilteredTargetHeightAlongUp;
            TargetHeightComponentUp = path.TargetHeightComponentUp;
            StateTargetCorrection = path.StateTargetCorrection;
            InterpolationPolicy = path.InterpolationPolicy.ToString();
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
            PlantTargetKind = path.PlantTargetKind.ToString();
            PlantLockResponse = path.PlantLockResponse;
            PlantLockWeightCompleted = path.PlantLockWeightCompleted;
            PlantDesiredPoint = path.PlantDesiredPoint;
            PlantFilteredPoint = path.PlantFilteredPoint;
            CharacterFootSupportTarget selectedSupportTarget =
                path.SelectedSupportTarget;
            SelectedSupportTarget =
                new CharacterFootSupportTargetDiagnostics(
                    in selectedSupportTarget);
            PlantTargetHeightAdoptionMode =
                path.PlantTargetHeightAdoptionMode.ToString();
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
                path.PlantTargetHeightUpdateReason.ToString();
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
                path.PlantResidualCaptureReason.ToString();
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
                path.CorrectionResponseInitializationReason.ToString();
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
                path.CorrectionResponseDeltaDirection.ToString();
            CorrectionResponseSelectedSpeed =
                path.CorrectionResponseSelectedSpeed;
            CorrectionResponseAppliedDelta =
                path.CorrectionResponseAppliedDelta;
            CorrectionResponseDomain = path.CorrectionResponseDomain.ToString();
            CorrectionResponsePreviousDomain = path.CorrectionResponsePreviousDomain.ToString();
            CorrectionResponseDomainTransferred = path.CorrectionResponseDomainTransferred;
            PlantVerticalContinuityOwners =
                path.PlantVerticalContinuityOwners.ToString();
            PlantEffectiveCorrectionBefore =
                path.PlantEffectiveCorrectionBefore;
            PlantEffectiveCorrectionAfter =
                path.PlantEffectiveCorrectionAfter;
            PlantOutputDistance = path.PlantOutputDistance;
            PlantPenetrationDepth = path.PlantPenetrationDepth;
        }

        public CharacterFootSwingMotionState State { get; }
        public CharacterFootSwingMotionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong GroundPathInputIdentity { get; }
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
        public bool LifecycleTransitionEvaluated { get; }
        public bool PreviousLockRequestAvailable { get; }
        public bool PreviousLockRequested { get; }
        public ulong PreviousLockRequestEventIdentity { get; }
        public string PreviousLockRequestMode { get; }
        public float PreviousLockRequestWeight { get; }
        public float PreviousContactEdgeSeconds { get; }
        public ulong PreviousLatestContactEventIdentity { get; }
        public ulong PreviousLatestReleasedContactEventIdentity { get; }
        public ulong PreviousCompletedLockWeightEventIdentity { get; }
        public bool PreviousContactAnchorAvailable { get; }
        public ulong PreviousContactAnchorEventIdentity { get; }
        public ulong PreviousContactAnchorAcquiredFrameSequence { get; }
        public ulong PreviousContactAnchorAcquiredCompletionIdentity { get; }
        public ulong PreviousContactAnchorWorldRevision { get; }
        public int PreviousContactAnchorSurfaceIdentity { get; }
        public Vector3 PreviousContactAnchorPoint { get; }
        public Vector3 PreviousContactAnchorNormal { get; }
        public bool CurrentLockRequested { get; }
        public ulong CurrentLockRequestEventIdentity { get; }
        public string CurrentLockRequestMode { get; }
        public float CurrentLockRequestWeight { get; }
        public string CurrentLockRequestAvailability { get; }
        public string ContactEdge { get; }
        public float CurrentContactEdgeSeconds { get; }
        public ulong CurrentLatestContactEventIdentity { get; }
        public ulong CurrentLatestReleasedContactEventIdentity { get; }
        public ulong CurrentCompletedLockWeightEventIdentity { get; }
        public bool CurrentContactAnchorAvailable { get; }
        public ulong CurrentContactAnchorEventIdentity { get; }
        public ulong CurrentContactAnchorAcquiredFrameSequence { get; }
        public ulong CurrentContactAnchorAcquiredCompletionIdentity { get; }
        public ulong CurrentContactAnchorWorldRevision { get; }
        public int CurrentContactAnchorSurfaceIdentity { get; }
        public Vector3 CurrentContactAnchorPoint { get; }
        public Vector3 CurrentContactAnchorNormal { get; }
        public bool SameEventContactReentryRefreshed { get; }
        public bool SameEventContactReentryUnavailable { get; }
        public bool RetainedVerifiedAnchor { get; }
        public bool ReentryInterpolationHistoryRetained { get; }
        public float FormalFootPlacementWeight { get; }
        public bool HardOwnershipLoss { get; }
        public string HardOwnershipLossReason { get; }
        public bool PreTransitionSuppressOutput { get; }
        public bool PreTransitionResetInterpolation { get; }
        public bool PostTransitionEvaluated { get; }
        public bool PostTransitionSuppressOutput { get; }
        public bool PostTransitionResetInterpolation { get; }
        public bool PathContinuityEvaluated { get; }
        public string PathRevisionReason { get; }
        public bool PathResidualRebuilt { get; }
        public bool TargetTrackingApplied { get; }
        public bool PathAvailableBefore { get; }
        public bool PathAvailableAfter { get; }
        public ulong PathPreviousLandingEventIdentity { get; }
        public ulong PathCurrentLandingEventIdentity { get; }
        public Vector3 PathPreviousTargetCorrection { get; }
        public Vector3 PathCurrentTargetCorrection { get; }
        public float PathLandingPointDelta { get; }
        public float PathTargetDelta { get; }
        public Vector3 SwingResidualBeforeRevision { get; }
        public Vector3 SwingResidualBeforeDecay { get; }
        public Vector3 SwingResidualAfterDecay { get; }
        public Vector3 ResidualOutputCorrection { get; }
        public float LandingAcceptanceDistance { get; }
        public float PathRevisionDistance { get; }
        public float SwingResidualTolerance { get; }
        public float ResidualTimeToLandingSeconds { get; }
        public float ResidualBaseHalfLifeSeconds { get; }
        public bool ResidualDeadlineHalfLifeAvailable { get; }
        public float ResidualDeadlineHalfLifeSeconds { get; }
        public float ResidualAppliedHalfLifeSeconds { get; }
        public float SwingRawTargetHeightAlongUp { get; }
        public float SwingFilteredTargetHeightBefore { get; }
        public float SwingTargetHeightDelta { get; }
        public float SwingTargetHeightAppliedDelta { get; }
        public bool SwingTargetHeightUpdateHeld { get; }
        public bool SwingTargetHeightForceRefreshed { get; }
        public bool SwingTargetHeightRateLimited { get; }
        public bool SwingTargetHeightClamped { get; }
        public float SwingTargetHeightForceRefreshDistance { get; }
        public float SwingTargetMaximumVerticalSpeed { get; }
        public string SwingTargetHeightAdoptionMode { get; }
        public float SwingFilteredTargetHeightAlongUp { get; }
        public Vector3 TargetHeightComponentUp { get; }
        public string PreTransitionReason { get; }
        public CharacterFootConstraintState PreTransitionSource { get; }
        public CharacterFootConstraintState PreTransitionTarget { get; }
        public string PreTransitionAnchorCommand { get; }
        public string PostTransitionReason { get; }
        public CharacterFootConstraintState PostTransitionSource { get; }
        public CharacterFootConstraintState PostTransitionTarget { get; }
        public string PostTransitionAnchorCommand { get; }
        public Vector3 StateTargetCorrection { get; }
        public string InterpolationPolicy { get; }
        public Vector3 InterpolationOutputCorrection { get; }
        public bool InterpolationCompleted { get; }
        public CharacterFootConstraintState ConstraintStateBefore { get; }
        public CharacterFootLockResponse LockResponseBefore { get; }
        public bool OutputStagesAvailable { get; }
        public bool ReleasingCompletedToSwing { get; }
        public bool SafetyFloorAvailable { get; }
        public CharacterFootSafetyFloorOwner SafetyFloorOwner { get; }
        public int SafetyFloorOwnerSurfaceIdentity { get; }
        public ulong SafetyFloorOwnerPathIdentity { get; }
        public Vector3 CorrectionBeforeSafetyFloor { get; }
        public Vector3 SafetyFloorMinimumCorrection { get; }
        public Vector3 SafetyFloorOutputCorrection { get; }
        public Vector3 FinalEffectiveCorrection { get; }
        public bool SafetyFloorClamped { get; }
        public float SafetyFloorClampMeters { get; }
        public float SafetyFloorClearanceBeforeMeters { get; }
        public float SafetyFloorClearanceAfterMeters { get; }
        public bool PlantInterpolationEvaluated { get; }
        public ulong PlantTargetEventIdentity { get; }
        public bool PlantTargetVerified { get; }
        public string PlantTargetKind { get; }
        public CharacterFootLockResponse PlantLockResponse { get; }
        public bool PlantLockWeightCompleted { get; }
        public Vector3 PlantDesiredPoint { get; }
        public Vector3 PlantFilteredPoint { get; }
        public CharacterFootSupportTargetDiagnostics SelectedSupportTarget
        {
            get;
        }
        public string PlantTargetHeightAdoptionMode { get; }
        public float PlantTargetMaximumVerticalSpeed { get; }
        public float PlantTargetHeightBefore { get; }
        public float PlantTargetHeightTarget { get; }
        public float PlantTargetVerticalDelta { get; }
        public float PlantTargetAppliedVerticalDelta { get; }
        public float PlantTargetHeightAfter { get; }
        public ulong PlantTargetHeightEventIdentity { get; }
        public string PlantTargetHeightUpdateReason { get; }
        public bool PlantTargetForceRefreshed { get; }
        public float PlantTargetForceRefreshDistance { get; }
        public bool PlantTargetVerticalClamped { get; }
        public Vector3 PlantPreviousSelectedWorldTarget { get; }
        public Vector3 PlantSelectedWorldTarget { get; }
        public bool PreviousResponseOutputAvailable { get; }
        public Vector3 PreviousResponseOutputPoint { get; }
        public Vector3 DesiredOutputPoint { get; }
        public Vector3 ResponseOutputPoint { get; }
        public string PlantResidualCaptureReason { get; }
        public Vector3 PlantWorldResidualBeforeCapture { get; }
        public Vector3 PlantWorldResidualCapturedBeforeDecay { get; }
        public bool PlantWorldResidualDecayApplied { get; }
        public float PlantWorldResidualBaseHalfLifeSeconds { get; }
        public bool PlantWorldResidualDeadlineHalfLifeAvailable { get; }
        public float PlantWorldResidualDeadlineHalfLifeSeconds { get; }
        public float PlantWorldResidualAppliedHalfLifeSeconds { get; }
        public Vector3 PlantWorldResidualAfterDecay { get; }
        public float PlantWorldResidualCompletionTolerance { get; }
        public bool PlantWorldResidualClearedAtCompletionTolerance { get; }
        public bool CorrectionResponseEvaluated { get; }
        public bool CorrectionResponseInitializedBefore { get; }
        public bool CorrectionResponseInitializedThisFrame { get; }
        public string CorrectionResponseInitializationReason { get; }
        public float CorrectionResponseDesired { get; }
        public Vector3 CorrectionResponseRequestedDirection { get; }
        public Vector3 CorrectionResponsePreviousDirection { get; }
        public bool CorrectionResponseDirectionLimited { get; }
        public float CorrectionResponseMaximumDirectionChangeDegrees { get; }
        public float CorrectionResponseAppliedDirectionChangeDegrees { get; }
        public bool CorrectionResponseVisibleOutputTransferred { get; }
        public float CorrectionResponseBeforeRebase { get; }
        public float CorrectionResponsePrevious { get; }
        public float CorrectionResponseCurrent { get; }
        public Vector3 CorrectionResponseDirection { get; }
        public string CorrectionResponseDeltaDirection { get; }
        public float CorrectionResponseSelectedSpeed { get; }
        public float CorrectionResponseAppliedDelta { get; }
        public string CorrectionResponseDomain { get; }
        public string CorrectionResponsePreviousDomain { get; }
        public bool CorrectionResponseDomainTransferred { get; }
        public string PlantVerticalContinuityOwners { get; }
        public Vector3 PlantEffectiveCorrectionBefore { get; }
        public Vector3 PlantEffectiveCorrectionAfter { get; }
        public float PlantOutputDistance { get; }
        public float PlantPenetrationDepth { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;
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
