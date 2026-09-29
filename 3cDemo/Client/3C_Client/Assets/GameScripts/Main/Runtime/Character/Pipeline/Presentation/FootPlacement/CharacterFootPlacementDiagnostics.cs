using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
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
            PlantWorldResidualZeroTolerance =
                path.PlantWorldResidualZeroTolerance;
            PlantWorldResidualClearedAtZeroTolerance =
                path.PlantWorldResidualClearedAtZeroTolerance;
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
        [DiagnosticKey("foot-motion-plant-world-residual-zero-tolerance")]
        [DiagnosticGroup("response-contact")]
        public float PlantWorldResidualZeroTolerance { get; }

        [DiagnosticField]
        [DiagnosticKey("foot-motion-plant-world-residual-cleared-at-zero-tolerance")]
        [DiagnosticGroup("response-contact")]
        public bool PlantWorldResidualClearedAtZeroTolerance { get; }

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
}
