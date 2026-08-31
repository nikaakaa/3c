using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootInterpolationRuntime
    {
        internal static CharacterFootInterpolationResult Evaluate(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            UpdateCorrectionResponseLineage(ref state, in frame);
            if (target.SuppressOutput ||
                target.InterpolationPolicy ==
                CharacterFootInterpolationPolicy.Suppressed)
            {
                CharacterFootCorrectionResponseInitializationReason reason =
                    (frame.OwnershipLossReason &
                     CharacterFootGoalOwnershipLossReason
                         .SourceLineageInvalidated) != 0
                        ? CharacterFootCorrectionResponseInitializationReason
                            .SourceLineageInvalidated
                        : CharacterFootCorrectionResponseInitializationReason
                            .PolicyExited;
                reason = ResolveSuppressedInitializationReason(
                    state.PendingCorrectionResponseInitializationReason,
                    reason);
                ResetInterpolation(ref state, in frame, reason);
                return new CharacterFootInterpolationResult(
                    default,
                    false,
                    default,
                    CharacterFootPathContinuityFact.CreateUnevaluated(
                        target.TimeToLandingSeconds,
                        frame.Settings,
                        frame.ComponentUp.normalized),
                    default,
                    default);
            }
            if (!state.HasOutput)
            {
                state.HasOutput = true;
                state.EffectiveCorrection = target.SwingCorrection;
            }
            state.Policy = target.InterpolationPolicy;
            switch (target.InterpolationPolicy)
            {
                case CharacterFootInterpolationPolicy.SwingResidual:
                    ClearPlant(ref state);
                    return EvaluateSwing(
                        ref state,
                        in target,
                        in frame,
                        true);
                case CharacterFootInterpolationPolicy.VerifiedSupport:
                    return EvaluatePlant(ref state, in target, in frame);
                case CharacterFootInterpolationPolicy.ReleaseResidual:
                    return EvaluateRelease(ref state, in target, in frame);
                default:
                    throw new System.InvalidOperationException(
                        "Foot interpolation policy is invalid.");
            }
        }

        internal static CharacterFootInterpolationResult AdvanceUnavailable(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            if (target.InterpolationPolicy !=
                    CharacterFootInterpolationPolicy.SwingResidual ||
                target.SupportTargetAvailable)
            {
                throw new System.InvalidOperationException(
                    "Unavailable Foot interpolation input is invalid.");
            }
            UpdateCorrectionResponseLineage(ref state, in frame);
            if (!state.HasOutput)
            {
                state.HasOutput = true;
                state.EffectiveCorrection = target.SwingCorrection;
            }
            state.Policy = CharacterFootInterpolationPolicy.SwingResidual;
            ClearPlant(ref state);
            return EvaluateSwing(
                ref state,
                in target,
                in frame,
                false);
        }

        internal static void ApplyPostTransition(
            ref CharacterFootInterpolationState state,
            in CharacterFootTransitionDecision transition)
        {
            if (!transition.ResetInterpolation ||
                transition.Phase !=
                CharacterFootTransitionPhase.PostInterpolation)
            {
                return;
            }
            Vector3 correction = state.EffectiveCorrection;
            bool hasCorrectionResponse = state.HasCorrectionResponse;
            float correctionResponse = state.CorrectionResponse;
            CharacterFootCorrectionResponseDomain responseDomain =
                state.CorrectionResponseDomain;
            CharacterFootCorrectionResponseFact correctionResponseFact =
                state.CorrectionResponseFact;
            bool hasPreviousResponseOutputPoint =
                state.HasPreviousResponseOutputPoint;
            Vector3 previousResponseOutputPoint =
                state.PreviousResponseOutputPoint;
            CharacterFootCorrectionResponseInitializationReason reason =
                state.PendingCorrectionResponseInitializationReason;
            FixedString128Bytes sourceLineage =
                state.CorrectionResponseSourceLineage;
            FixedString128Bytes profileRevision =
                state.CorrectionResponseProfileRevision;
            ulong worldRevision = state.CorrectionResponseWorldRevision;
            bool hasLineage = state.HasCorrectionResponseLineage;
            state = default;
            state.HasOutput = true;
            state.EffectiveCorrection = correction;
            state.HasCorrectionResponse = hasCorrectionResponse;
            state.CorrectionResponse = correctionResponse;
            state.CorrectionResponseDomain = responseDomain;
            state.CorrectionResponseFact = correctionResponseFact;
            state.HasPreviousResponseOutputPoint =
                hasPreviousResponseOutputPoint;
            state.PreviousResponseOutputPoint = previousResponseOutputPoint;
            state.HasCorrectionResponseLineage = hasLineage;
            state.CorrectionResponseSourceLineage = sourceLineage;
            state.CorrectionResponseProfileRevision = profileRevision;
            state.CorrectionResponseWorldRevision = worldRevision;
            state.PendingCorrectionResponseInitializationReason = reason;
        }

        static CharacterFootInterpolationResult EvaluatePlant(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            if (!target.PlantTargetAvailable ||
                !target.PlantTargetVerified ||
                target.PlantTargetEventIdentity == 0 ||
                (target.PlantTargetKind != CharacterFootPlantTargetKind.VerifiedAnchor &&
                 target.PlantTargetKind != CharacterFootPlantTargetKind.LockedFullAnchor &&
                 target.PlantTargetKind != CharacterFootPlantTargetKind.LockedSliding) ||
                !CharacterFootConstraintMath.Finite(target.PlantTargetPoint) ||
                !target.SupportTargetAvailable ||
                !target.SupportTarget.IsValid)
            {
                throw new System.InvalidOperationException(
                    "Foot Plant target is invalid.");
            }
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 supportNormal = target.SupportTarget.SupportNormal;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            CharacterFootSupportIntent supportIntent = target.SupportIntent;
            CharacterFootInterpolationResult swing = EvaluateSwing(
                ref state,
                new CharacterFootStateTarget(
                    target.SwingCorrection,
                    target.SwingCorrection,
                    CharacterFootInterpolationPolicy.SwingResidual,
                    false,
                    0,
                    false,
                    default,
                    CharacterFootPlantTargetKind.None,
                    CharacterFootLockResponse.None,
                    false,
                    false,
                    default,
                    false,
                    false,
                    false,
                    false,
                    target.TimeToLandingSeconds,
                    in supportIntent),
                in frame,
                false);
            bool previousResponseOutputAvailable =
                state.HasPreviousResponseOutputPoint;
            Vector3 currentOutputBefore = previousResponseOutputAvailable
                ? state.PreviousResponseOutputPoint
                : originalSole + swing.Correction;
            Vector3 effectiveCorrectionBefore =
                currentOutputBefore - originalSole;
            bool hadPlantTarget = state.HasPlantTarget;
            bool sameTarget = hadPlantTarget &&
                              state.PlantTargetEventIdentity ==
                              target.PlantTargetEventIdentity;
            CharacterFootPlantTargetKind previousTargetKind =
                state.PlantTargetKind;
            CharacterFootLockResponse previousLockResponse =
                state.PlantLockResponse;
            bool previousVerified = state.PlantTargetVerified;
            bool previousDirectFollow = state.PlantDirectFollow;
            Vector3 previousDesiredPoint = state.PlantDesiredPoint;
            Vector3 previousSelectedWorldTarget =
                state.PreviousPlantSelectedWorldTarget;
            float desiredTargetHeightAlongUp = Vector3.Dot(
                target.PlantTargetPoint,
                up);
            bool hadTargetHeight = state.HasTargetHeight;
            bool matchingTargetHeight = hadTargetHeight &&
                                        state.TargetHeightEventIdentity ==
                                        target.PlantTargetEventIdentity;
            bool targetHeightInitialized = !hadTargetHeight;
            bool targetHeightEventChanged = hadTargetHeight &&
                                            !matchingTargetHeight;
            float targetHeightBefore = hadTargetHeight
                ? state.FilteredTargetHeightAlongUp
                : desiredTargetHeightAlongUp;
            float targetVerticalDelta = desiredTargetHeightAlongUp -
                                        targetHeightBefore;
            bool verificationRefresh = matchingTargetHeight &&
                                       target.PlantTargetVerified &&
                                       !previousVerified;
            bool directAdoption = frame.Settings.TargetHeightAdoptionMode ==
                                  CharacterFootTargetHeightAdoptionMode.Direct;
            bool distanceForceRefresh = matchingTargetHeight &&
                                         !directAdoption &&
                                         !target.DirectPlantFollow &&
                                         !verificationRefresh &&
                                         Mathf.Abs(targetVerticalDelta) >=
                                         frame.Settings
                                             .TargetHeightForceRefreshDistance;
            bool targetForceRefreshed = verificationRefresh ||
                                        distanceForceRefresh;
            bool targetRevisionAdmitted =
                Mathf.Abs(targetVerticalDelta) >
                frame.Settings.PathRevisionDistance;
            float maximumTargetDelta =
                frame.Settings.MaximumVerticalTargetSpeed *
                frame.DeltaSeconds;
            float targetAppliedVerticalDelta = targetHeightInitialized ||
                                               targetHeightEventChanged ||
                                               directAdoption ||
                                               target.DirectPlantFollow ||
                                               targetForceRefreshed
                ? targetVerticalDelta
                : targetRevisionAdmitted
                    ? Mathf.Clamp(
                        targetVerticalDelta,
                        -maximumTargetDelta,
                        maximumTargetDelta)
                    : 0f;
            bool targetVerticalClamped = targetRevisionAdmitted &&
                                         !targetHeightInitialized &&
                                         !targetHeightEventChanged &&
                                         !directAdoption &&
                                         !target.DirectPlantFollow &&
                                         !targetForceRefreshed &&
                                         !Mathf.Approximately(
                                             targetVerticalDelta,
                                             targetAppliedVerticalDelta);
            state.HasTargetHeight = true;
            state.TargetHeightEventIdentity = target.PlantTargetEventIdentity;
            state.FilteredTargetHeightAlongUp =
                targetHeightBefore + targetAppliedVerticalDelta;
            state.TargetHeightRetargetActive = false;
            CharacterFootPlantTargetHeightUpdateReason targetHeightUpdateReason;
            if (targetHeightInitialized)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.Initialized;
            }
            else if (targetHeightEventChanged)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.EventChanged;
            }
            else if (verificationRefresh)
            {
                targetHeightUpdateReason = CharacterFootPlantTargetHeightUpdateReason
                    .VerificationRefresh;
            }
            else if (targetVerticalDelta == 0f)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.None;
            }
            else if (target.DirectPlantFollow)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.DirectFollow;
            }
            else if (directAdoption)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.DirectAdoption;
            }
            else if (distanceForceRefresh)
            {
                targetHeightUpdateReason = CharacterFootPlantTargetHeightUpdateReason
                    .ForceRefreshDistanceExceeded;
            }
            else if (!targetRevisionAdmitted)
            {
                targetHeightUpdateReason = CharacterFootPlantTargetHeightUpdateReason
                    .HeldWithinRevisionDistance;
            }
            else if (targetVerticalClamped)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.RateLimited;
            }
            else
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.WithinRate;
            }
            state.PlantFilteredPoint = Vector3.ProjectOnPlane(
                target.PlantTargetPoint,
                up) + up * state.FilteredTargetHeightAlongUp;
            Vector3 selectedWorldTarget = state.PlantFilteredPoint;
            bool targetRevised = sameTarget &&
                                  !target.DirectPlantFollow &&
                                 Vector3.Distance(
                                     previousDesiredPoint,
                                      target.PlantTargetPoint) >
                                  frame.Settings.LandingAcceptanceDistance;
            CharacterFootPlantResidualCaptureReason captureReason =
                CharacterFootPlantResidualCaptureReason.None;
            if (!sameTarget)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetEventChanged;
            if (hadPlantTarget && previousTargetKind != target.PlantTargetKind)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetKindChanged;
            if (hadPlantTarget &&
                previousLockResponse != target.PlantLockResponse)
            {
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .LockResponseChanged;
            }
            if (hadPlantTarget && previousVerified != target.PlantTargetVerified)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .VerificationChanged;
            if (hadPlantTarget &&
                previousDirectFollow != target.DirectPlantFollow)
            {
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .DirectFollowChanged;
            }
            if (target.StateEntered)
                captureReason |=
                    CharacterFootPlantResidualCaptureReason.StateEntered;
            if (target.ResponseEntered)
                captureReason |=
                    CharacterFootPlantResidualCaptureReason.ResponseEntered;
            if (targetRevised)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetPointRevised;
            if (targetForceRefreshed)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetHeightForceRefreshed;
            Vector3 residualBeforeCapture = state.PlantWorldResidual;
            bool captureTransition = captureReason !=
                                     CharacterFootPlantResidualCaptureReason.None;
            Vector3 continuityOutputBefore = currentOutputBefore;
            if (captureTransition)
            {
                state.PlantWorldResidual =
                    continuityOutputBefore - selectedWorldTarget;
                state.PlantWorldResidualTransitionActive =
                    state.PlantWorldResidual.sqrMagnitude >
                    CharacterFootConstraintMath.GeometryEpsilon *
                    CharacterFootConstraintMath.GeometryEpsilon;
            }
            Vector3 residualCapturedBeforeDecay = state.PlantWorldResidual;
            bool residualDecayApplied = false;
            float residualBaseHalfLifeSeconds =
                frame.Settings.EffectiveCorrectionHalfLifeSeconds;
            bool residualDeadlineHalfLifeAvailable = false;
            float residualDeadlineHalfLifeSeconds = 0f;
            float residualAppliedHalfLifeSeconds = 0f;
            bool residualClearedAtCompletionTolerance = false;
            if (state.PlantWorldResidualTransitionActive &&
                frame.DeltaSeconds > 0f)
            {
                residualAppliedHalfLifeSeconds = ResolveSwingResidualHalfLife(
                    state.PlantWorldResidual,
                    target.TimeToLandingSeconds,
                    frame.Settings,
                    out residualDeadlineHalfLifeAvailable,
                    out residualDeadlineHalfLifeSeconds);
                residualDecayApplied = true;
                state.PlantWorldResidual = Advance(
                    state.PlantWorldResidual,
                    default,
                    frame.DeltaSeconds,
                    residualAppliedHalfLifeSeconds);
                if (state.PlantWorldResidual.magnitude <=
                    frame.Settings.LandingLockCompletionTolerance)
                {
                    state.PlantWorldResidual = default;
                    state.PlantWorldResidualTransitionActive = false;
                    residualClearedAtCompletionTolerance = true;
                }
            }
            Vector3 residualAfterDecay = state.PlantWorldResidual;
            Vector3 desiredOutputPoint =
                selectedWorldTarget + residualAfterDecay;
            Vector3 responseOutputPoint = ApplyCorrectionResponse(
                ref state,
                desiredOutputPoint,
                supportNormal,
                CharacterFootCorrectionResponseDomain.ContactWorldResidual,
                captureTransition,
                false,
                default,
                in frame,
                out CharacterFootCorrectionResponseFact
                    correctionResponseFact);
            state.EffectiveCorrection = responseOutputPoint - originalSole;
            CharacterFootVerticalContinuityOwner verticalContinuityOwners =
                CharacterFootVerticalContinuityOwner.PlantTarget;
            if (targetHeightUpdateReason !=
                    CharacterFootPlantTargetHeightUpdateReason.None ||
                targetVerticalClamped || targetForceRefreshed)
            {
                verticalContinuityOwners |=
                    CharacterFootVerticalContinuityOwner.TargetHeightHistory;
            }
            if (captureTransition ||
                residualCapturedBeforeDecay.sqrMagnitude >
                CharacterFootConstraintMath.GeometryEpsilon *
                CharacterFootConstraintMath.GeometryEpsilon ||
                residualAfterDecay.sqrMagnitude >
                CharacterFootConstraintMath.GeometryEpsilon *
                CharacterFootConstraintMath.GeometryEpsilon)
            {
                verticalContinuityOwners |=
                    CharacterFootVerticalContinuityOwner.PlantWorldResidual;
            }
            state.HasPlantTarget = true;
            state.PlantTargetEventIdentity = target.PlantTargetEventIdentity;
            state.PlantTargetKind = target.PlantTargetKind;
            state.PlantLockResponse = target.PlantLockResponse;
            state.PlantTargetVerified = target.PlantTargetVerified;
            state.PlantDirectFollow = target.DirectPlantFollow;
            state.PlantDesiredPoint = target.PlantTargetPoint;
            state.PreviousPlantSelectedWorldTarget = selectedWorldTarget;
            state.SelectedSupportTarget = target.SupportTarget.WithSupportNormal(
                correctionResponseFact.ResponseDirection);
            state.HasPreviousResponseOutputPoint = true;
            state.PreviousResponseOutputPoint = responseOutputPoint;
            state.PreviousTargetCorrection = selectedWorldTarget - originalSole;
            state.Progress = 1f;
            state.StartResidual = 0f;
            float outputDistance = Vector3.Distance(
                responseOutputPoint,
                target.PlantTargetPoint);
            float penetrationDepth = Mathf.Max(
                0f,
                Vector3.Dot(
                    target.PlantTargetPoint - responseOutputPoint,
                    up));
            state.Completed = target.PlantTargetVerified &&
                              target.LockWeightCompleted &&
                              frame.LockRequest.RequestsLock &&
                              frame.LockRequest.EventIdentity ==
                              target.PlantTargetEventIdentity &&
                              outputDistance <=
                              frame.Settings.LandingLockCompletionTolerance &&
                              penetrationDepth <=
                              frame.Settings.GroundPenetrationTolerance;
            state.PlantFact = new CharacterFootPlantInterpolationFact(
                true,
                target.PlantTargetEventIdentity,
                target.PlantTargetVerified,
                target.PlantTargetKind,
                target.PlantLockResponse,
                target.PlantTargetPoint,
                state.PlantFilteredPoint,
                frame.Settings.TargetHeightAdoptionMode,
                frame.Settings.MaximumVerticalTargetSpeed,
                targetHeightBefore,
                desiredTargetHeightAlongUp,
                targetVerticalDelta,
                targetAppliedVerticalDelta,
                state.FilteredTargetHeightAlongUp,
                state.TargetHeightEventIdentity,
                targetHeightUpdateReason,
                targetForceRefreshed,
                frame.Settings.TargetHeightForceRefreshDistance,
                targetVerticalClamped,
                previousSelectedWorldTarget,
                selectedWorldTarget,
                captureReason,
                residualBeforeCapture,
                residualCapturedBeforeDecay,
                residualDecayApplied,
                residualBaseHalfLifeSeconds,
                residualDeadlineHalfLifeAvailable,
                residualDeadlineHalfLifeSeconds,
                residualAppliedHalfLifeSeconds,
                residualAfterDecay,
                frame.Settings.LandingLockCompletionTolerance,
                residualClearedAtCompletionTolerance,
                verticalContinuityOwners,
                effectiveCorrectionBefore,
                state.EffectiveCorrection,
                outputDistance,
                penetrationDepth);
            return Result(
                in state,
                state.Completed,
                swing.ContinuityFact);
        }

        static CharacterFootInterpolationResult EvaluateRelease(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            state.PlantFact = default;
            state.SelectedSupportTarget = target.SupportTargetAvailable
                ? target.SupportTarget
                : default;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            if (target.StateEntered)
            {
                if (state.HasPreviousResponseOutputPoint)
                {
                    state.EffectiveCorrection =
                        state.PreviousResponseOutputPoint - originalSole;
                }
                state.PreviousTargetCorrection = target.Correction;
                state.Residual =
                    state.EffectiveCorrection - target.Correction;
                state.StartResidual = state.Residual.magnitude;
                state.Progress = 0f;
            }
            else
            {
                state.Residual +=
                    state.PreviousTargetCorrection - target.Correction;
            }
            state.PreviousTargetCorrection = target.Correction;
            state.Residual = Advance(
                state.Residual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            Vector3 desiredCorrection = target.Correction + state.Residual;
            Vector3 releaseOutputPoint = ApplyCorrectionResponse(
                ref state,
                originalSole + desiredCorrection,
                target.SupportTarget.SupportNormal,
                CharacterFootCorrectionResponseDomain.AnimationRelativeScalar,
                target.StateEntered,
                false,
                default,
                in frame,
                out CharacterFootCorrectionResponseFact
                    correctionResponseFact);
            state.SelectedSupportTarget = target.SupportTarget.WithSupportNormal(
                correctionResponseFact.ResponseDirection);
            state.EffectiveCorrection = releaseOutputPoint - originalSole;
            state.Completed = frame.LockRequest.Weight <=
                              CharacterFootConstraintMath.GeometryEpsilon &&
                              Vector3.Distance(
                                  state.EffectiveCorrection,
                                  target.Correction) <=
                              frame.Settings.ReleaseCompletionTolerance;
            return Result(
                in state,
                state.Completed,
                Unevaluated(in target, in frame));
        }

        static CharacterFootInterpolationResult EvaluateSwing(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame,
            bool applyCorrectionResponse)
        {
            state.SelectedSupportTarget = target.SupportTargetAvailable
                ? target.SupportTarget
                : default;
            bool pathAvailableBefore = state.HasSwingPath;
            ulong previousLandingEventIdentity =
                state.SwingLandingEventIdentity;
            CharacterFootSupportTarget previousHeightReference =
                state.SwingHeightReference;
            Vector3 previousLandingPoint = state.SwingLandingPoint;
            Vector3 previousTargetCorrection =
                state.PreviousSwingTargetCorrection;
            Vector3 residualBeforeRevision = state.SwingResidual;
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            CharacterFootSwingPathReference swingPath =
                swing.SwingPathReference;
            CharacterFootSupportTarget heightReference =
                swing.SwingHeightReference;
            bool hasPath = target.SupportTarget.Kind ==
                           CharacterFootSupportTargetKind.SwingGround &&
                           swing.Accepted &&
                           heightReference.IsValid &&
                           swingPath.IsAvailable &&
                           swingPath.LandingEventIdentity ==
                           swing.LandingEventIdentity;
            bool comparablePath = pathAvailableBefore && hasPath;
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            float originalSoleHeight = Vector3.Dot(originalSole, up);
            float rawTargetHeightAlongUp = hasPath
                ? swing.FormalTargetHeightAlongUp
                : 0f;
            float currentSupportHeightAlongUp = hasPath
                ? Vector3.Dot(heightReference.Position, up)
                : 0f;
            float rawTargetCorrectionAlongUp = hasPath
                ? Mathf.Max(0f, rawTargetHeightAlongUp - originalSoleHeight)
                : 0f;
            float landingPointDelta = comparablePath
                ? Vector3.Distance(
                    previousLandingPoint,
                    swingPath.LandingPoint)
                : 0f;
            float supportHeightDelta = comparablePath
                ? Mathf.Abs(Vector3.Dot(
                    heightReference.Position - previousHeightReference.Position,
                    up))
                : 0f;
            bool sameEvent = comparablePath &&
                             previousLandingEventIdentity ==
                             swing.LandingEventIdentity;
            bool supportSurfaceChanged = comparablePath &&
                                         previousHeightReference.SurfaceIdentity !=
                                         heightReference.SurfaceIdentity;
            bool sameHeightTarget = hasPath &&
                                    state.HasTargetHeight &&
                                    state.TargetHeightEventIdentity ==
                                    swing.LandingEventIdentity;
            float filteredTargetHeightBefore =
                state.FilteredTargetHeightAlongUp;
            bool targetCorrectionRateLimited = false;
            bool targetCorrectionClamped = false;
            bool targetHeightForceRefreshed = false;
            bool targetHeightUpdateHeld = hasPath &&
                                          state.Policy !=
                                          CharacterFootInterpolationPolicy
                                              .SwingResidual;
            bool ownsSwingTargetHeight = state.Policy ==
                                         CharacterFootInterpolationPolicy
                                             .SwingResidual;
            bool directTargetHeightAdoption =
                frame.Settings.TargetHeightAdoptionMode ==
                CharacterFootTargetHeightAdoptionMode.Direct;
            if (hasPath && ownsSwingTargetHeight && !sameHeightTarget)
            {
                state.HasTargetHeight = true;
                state.TargetHeightEventIdentity =
                    swing.LandingEventIdentity;
                state.FilteredTargetHeightAlongUp =
                    currentSupportHeightAlongUp;
                state.TargetHeightRetargetActive = false;
            }
            if (hasPath && ownsSwingTargetHeight)
            {
                filteredTargetHeightBefore = rawTargetHeightAlongUp +
                                             state
                                                 .FilteredTargetHeightAlongUp -
                                             currentSupportHeightAlongUp;
            }
            else if (hasPath)
            {
                filteredTargetHeightBefore = rawTargetHeightAlongUp;
            }
            if (hasPath && ownsSwingTargetHeight)
            {
                float targetHeightDelta = currentSupportHeightAlongUp -
                                          state.FilteredTargetHeightAlongUp;
                if (directTargetHeightAdoption)
                {
                    state.FilteredTargetHeightAlongUp =
                        currentSupportHeightAlongUp;
                    state.TargetHeightRetargetActive = false;
                }
                else
                {
                    bool retargetRequested = supportSurfaceChanged ||
                                             supportHeightDelta >
                                             frame.Settings.PathRevisionDistance;
                    if (retargetRequested &&
                        Mathf.Abs(targetHeightDelta) >=
                        frame.Settings.TargetHeightForceRefreshDistance)
                    {
                        state.FilteredTargetHeightAlongUp =
                            currentSupportHeightAlongUp;
                        state.TargetHeightRetargetActive = false;
                        targetHeightForceRefreshed = true;
                    }
                    else if (retargetRequested)
                    {
                        state.TargetHeightRetargetActive = true;
                    }
                    float maximumHeightDelta = ResolveVerticalHistoryDelta(
                        frame.DeltaSeconds,
                        frame.Settings.MaximumVerticalTargetSpeed);
                    if (state.TargetHeightRetargetActive)
                    {
                        targetCorrectionRateLimited = true;
                        float appliedHeightDelta = Mathf.Clamp(
                            targetHeightDelta,
                            -maximumHeightDelta,
                            maximumHeightDelta);
                        targetCorrectionClamped = !Mathf.Approximately(
                            targetHeightDelta,
                            appliedHeightDelta);
                        state.FilteredTargetHeightAlongUp += appliedHeightDelta;
                        if (Mathf.Approximately(
                                targetHeightDelta,
                                appliedHeightDelta))
                        {
                            state.TargetHeightRetargetActive = false;
                        }
                    }
                }
            }
            else if (ownsSwingTargetHeight)
            {
                state.HasTargetHeight = false;
                state.TargetHeightEventIdentity = 0;
                state.FilteredTargetHeightAlongUp = 0f;
                state.TargetHeightRetargetActive = false;
            }
            float filteredTargetHeightAlongUp = hasPath
                ? ownsSwingTargetHeight
                    ? rawTargetHeightAlongUp +
                      state.FilteredTargetHeightAlongUp -
                      currentSupportHeightAlongUp
                    : rawTargetHeightAlongUp
                : 0f;
            Vector3 swingTargetCorrection = hasPath
                ? up * Mathf.Max(
                    0f,
                    filteredTargetHeightAlongUp - originalSoleHeight)
                : target.Correction;
            float targetDelta = comparablePath
                ? Vector3.Distance(
                    previousTargetCorrection,
                    up * rawTargetCorrectionAlongUp)
                : 0f;
            CharacterFootPathRevisionReason revisionReason =
                CharacterFootPathRevisionReason.None;
            if (hasPath != pathAvailableBefore)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.PathAvailabilityChanged;
            }
            if (comparablePath &&
                previousLandingEventIdentity != swing.LandingEventIdentity)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.LandingEventChanged;
            }
            if (supportSurfaceChanged)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.SupportSurfaceChanged;
            }
            bool revised = revisionReason !=
                           CharacterFootPathRevisionReason.None;
            bool targetTrackingApplied = sameEvent &&
                supportHeightDelta > frame.Settings.PathRevisionDistance;
            if (revised || targetTrackingApplied)
            {
                state.SwingResidual =
                    state.EffectiveCorrection - swingTargetCorrection;
            }
            Vector3 residualBeforeDecay = state.SwingResidual;
            state.HasSwingPath = hasPath;
            state.SwingLandingEventIdentity = hasPath
                ? swing.LandingEventIdentity
                : 0;
            state.SwingHeightReference = hasPath
                ? heightReference
                : default;
            state.SwingLandingPoint = hasPath
                ? swingPath.LandingPoint
                : default;
            state.PreviousTargetCorrection = swingTargetCorrection;
            state.PreviousSwingTargetCorrection = swingTargetCorrection;
            float halfLifeSeconds = ResolveSwingResidualHalfLife(
                state.SwingResidual,
                target.TimeToLandingSeconds,
                frame.Settings,
                out bool deadlineHalfLifeAvailable,
                out float deadlineHalfLifeSeconds);
            state.SwingResidual = Advance(
                state.SwingResidual,
                default,
                frame.DeltaSeconds,
                halfLifeSeconds);
            Vector3 swingCorrection =
                swingTargetCorrection + state.SwingResidual;
            if (applyCorrectionResponse)
            {
                Vector3 responseOutputPoint = ApplyCorrectionResponse(
                    ref state,
                    originalSole + swingCorrection,
                    target.SupportTarget.SupportNormal,
                    CharacterFootCorrectionResponseDomain.AnimationRelativeScalar,
                    false,
                    false,
                    default,
                    in frame,
                    out CharacterFootCorrectionResponseFact
                        correctionResponseFact);
                state.SelectedSupportTarget =
                    target.SupportTarget.WithSupportNormal(
                        correctionResponseFact.ResponseDirection);
                state.EffectiveCorrection = responseOutputPoint - originalSole;
                state.Residual = state.SwingResidual;
            }
            state.Progress = 0f;
            state.StartResidual = 0f;
            state.Completed = false;
            var continuityFact = new CharacterFootPathContinuityFact(
                true,
                revisionReason,
                revised,
                targetTrackingApplied,
                pathAvailableBefore,
                hasPath,
                previousLandingEventIdentity,
                hasPath ? swing.LandingEventIdentity : 0,
                previousTargetCorrection,
                swingTargetCorrection,
                landingPointDelta,
                targetDelta,
                residualBeforeRevision,
                residualBeforeDecay,
                state.SwingResidual,
                frame.Settings.LandingAcceptanceDistance,
                frame.Settings.PathRevisionDistance,
                frame.Settings.SwingResidualTolerance,
                target.TimeToLandingSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds,
                deadlineHalfLifeAvailable,
                deadlineHalfLifeSeconds,
                halfLifeSeconds,
                hasPath ? rawTargetHeightAlongUp : 0f,
                hasPath ? filteredTargetHeightBefore : 0f,
                hasPath
                    ? rawTargetHeightAlongUp - filteredTargetHeightBefore
                    : 0f,
                hasPath
                    ? filteredTargetHeightAlongUp -
                      filteredTargetHeightBefore
                    : 0f,
                hasPath && targetHeightUpdateHeld,
                hasPath && targetHeightForceRefreshed,
                hasPath && targetCorrectionRateLimited,
                hasPath && targetCorrectionClamped,
                frame.Settings.TargetHeightForceRefreshDistance,
                frame.Settings.MaximumVerticalTargetSpeed,
                frame.Settings.TargetHeightAdoptionMode,
                hasPath
                    ? filteredTargetHeightAlongUp
                    : 0f,
                up,
                previousHeightReference);
            return new CharacterFootInterpolationResult(
                state.Policy == CharacterFootInterpolationPolicy.SwingResidual
                    ? state.EffectiveCorrection
                    : swingCorrection,
                false,
                in state.SelectedSupportTarget,
                in continuityFact,
                in state.PlantFact,
                in state.CorrectionResponseFact);
        }

        static CharacterFootInterpolationResult Result(
            in CharacterFootInterpolationState state,
            bool completed,
            in CharacterFootPathContinuityFact continuityFact) =>
            new CharacterFootInterpolationResult(
                state.EffectiveCorrection,
                completed,
                in state.SelectedSupportTarget,
                in continuityFact,
                in state.PlantFact,
                in state.CorrectionResponseFact);

        static CharacterFootPathContinuityFact Unevaluated(
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame) =>
            CharacterFootPathContinuityFact.CreateUnevaluated(
                target.TimeToLandingSeconds,
                frame.Settings,
                frame.ComponentUp.normalized);

        static Vector3 ApplyCorrectionResponse(
            ref CharacterFootInterpolationState state,
            Vector3 desiredOutputPoint,
            Vector3 responseDirection,
            CharacterFootCorrectionResponseDomain domain,
            bool targetContinuityCaptured,
            bool visibleOutputTransferAvailable,
            Vector3 visibleOutputTransferPoint,
            in CharacterFootStateFrame frame,
            out CharacterFootCorrectionResponseFact fact)
        {
            if ((domain != CharacterFootCorrectionResponseDomain.AnimationRelativeScalar &&
                 domain != CharacterFootCorrectionResponseDomain.ContactWorldResidual) ||
                !CharacterFootConstraintMath.Finite(desiredOutputPoint) ||
                !CharacterFootConstraintMath.Finite(responseDirection) ||
                responseDirection.sqrMagnitude <=
                CharacterFootConstraintMath.GeometryEpsilon *
                CharacterFootConstraintMath.GeometryEpsilon)
            {
                throw new System.InvalidOperationException(
                    "Foot Correction Response input is invalid.");
            }
            Vector3 requestedDirection = responseDirection.normalized;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            bool previousOutputAvailable = visibleOutputTransferAvailable ||
                                           state.HasPreviousResponseOutputPoint;
            Vector3 previousOutputPoint = visibleOutputTransferAvailable
                ? visibleOutputTransferPoint
                : state.HasPreviousResponseOutputPoint
                    ? state.PreviousResponseOutputPoint
                    : desiredOutputPoint;
            float desiredResponse = Vector3.Dot(
                desiredOutputPoint - originalSole,
                requestedDirection);
            bool initializedBefore = state.HasCorrectionResponse;
            bool initializedThisFrame = !initializedBefore;
            CharacterFootCorrectionResponseDomain previousDomain =
                initializedBefore
                    ? state.CorrectionResponseDomain
                    : CharacterFootCorrectionResponseDomain.None;
            if (initializedBefore && previousDomain ==
                CharacterFootCorrectionResponseDomain.None)
            {
                throw new System.InvalidOperationException(
                    "Foot Correction Response history has no coordinate domain.");
            }
            bool domainTransferred = initializedBefore && previousDomain != domain;
            bool contactWorldResidual = domain ==
                CharacterFootCorrectionResponseDomain.ContactWorldResidual;
            bool exitingContact = domainTransferred && !contactWorldResidual;
            if (exitingContact && (!targetContinuityCaptured ||
                                   !state.HasPreviousResponseOutputPoint))
            {
                throw new System.InvalidOperationException(
                    "Contact response exit has no complete target residual capture.");
            }
            CharacterFootCorrectionResponseInitializationReason reason =
                CharacterFootCorrectionResponseInitializationReason.None;
            Vector3 previousResponseDirection =
                state.CorrectionResponseFact.Evaluated
                    ? state.CorrectionResponseFact.ResponseDirection
                    : requestedDirection;
            float maximumDirectionChangeDegrees = frame.Settings
                .CorrectionResponseMaximumDirectionChangeDegrees;
            float requestedDirectionChangeDegrees = initializedBefore
                ? Vector3.Angle(
                    previousResponseDirection,
                    requestedDirection)
                : 0f;
            bool directionLimited = initializedBefore &&
                                    requestedDirectionChangeDegrees >
                                    maximumDirectionChangeDegrees;
            Vector3 direction = directionLimited
                ? Vector3.RotateTowards(
                    previousResponseDirection,
                    requestedDirection,
                    maximumDirectionChangeDegrees * Mathf.Deg2Rad,
                    0f).normalized
                : requestedDirection;
            float appliedDirectionChangeDegrees = initializedBefore
                ? Vector3.Angle(previousResponseDirection, direction)
                : 0f;
            desiredResponse = Vector3.Dot(
                desiredOutputPoint - originalSole,
                direction);
            if (visibleOutputTransferAvailable && !previousOutputAvailable)
            {
                throw new System.InvalidOperationException(
                    "Foot Correction Response target transfer has no committed output.");
            }
            float responseBeforeRebase = contactWorldResidual || exitingContact
                ? 0f
                : initializedBefore
                    ? state.CorrectionResponse
                    : desiredResponse;
            float previousResponse = contactWorldResidual
                ? 0f
                : exitingContact
                    ? desiredResponse
                    : visibleOutputTransferAvailable
                        ? Vector3.Dot(
                            previousOutputPoint - originalSole,
                            direction)
                        : responseBeforeRebase;
            float currentResponse = previousResponse;
            CharacterFootCorrectionResponseDeltaDirection deltaDirection =
                CharacterFootCorrectionResponseDeltaDirection.None;
            float selectedSpeed = 0f;
            float appliedDelta = 0f;
            if (initializedThisFrame)
            {
                reason = state.PendingCorrectionResponseInitializationReason !=
                         CharacterFootCorrectionResponseInitializationReason.None
                    ? state.PendingCorrectionResponseInitializationReason
                    : CharacterFootCorrectionResponseInitializationReason
                        .FirstLegalInput;
            }
            if (contactWorldResidual)
            {
                desiredResponse = 0f;
            }
            else if (!initializedThisFrame && !exitingContact)
            {
                float delta = desiredResponse - previousResponse;
                if (delta != 0f)
                {
                    deltaDirection = delta > 0f
                        ? CharacterFootCorrectionResponseDeltaDirection.Increase
                        : CharacterFootCorrectionResponseDeltaDirection.Decrease;
                    selectedSpeed = delta > 0f
                        ? frame.Settings.CorrectionResponseIncreaseSpeed
                        : frame.Settings.CorrectionResponseDecreaseSpeed;
                    float maximumDelta = selectedSpeed * frame.DeltaSeconds;
                    appliedDelta = Mathf.Clamp(
                        delta,
                        -maximumDelta,
                        maximumDelta);
                    currentResponse = previousResponse + appliedDelta;
                }
            }
            Vector3 responseOutputPoint = contactWorldResidual
                ? desiredOutputPoint
                : desiredOutputPoint + direction *
                  (currentResponse - desiredResponse);
            fact = new CharacterFootCorrectionResponseFact(
                true,
                initializedBefore,
                initializedThisFrame,
                reason,
                previousOutputAvailable,
                previousOutputPoint,
                desiredOutputPoint,
                responseOutputPoint,
                desiredResponse,
                requestedDirection,
                previousResponseDirection,
                directionLimited,
                maximumDirectionChangeDegrees,
                appliedDirectionChangeDegrees,
                visibleOutputTransferAvailable,
                responseBeforeRebase,
                previousResponse,
                currentResponse,
                direction,
                deltaDirection,
                selectedSpeed,
                appliedDelta,
                domain,
                previousDomain,
                domainTransferred);
            state.HasCorrectionResponse = true;
            state.CorrectionResponse = currentResponse;
            state.CorrectionResponseDomain = domain;
            state.CorrectionResponseFact = fact;
            state.HasPreviousResponseOutputPoint = true;
            state.PreviousResponseOutputPoint = responseOutputPoint;
            state.PendingCorrectionResponseInitializationReason =
                CharacterFootCorrectionResponseInitializationReason.None;
            return responseOutputPoint;
        }

        static void ClearPlant(ref CharacterFootInterpolationState state)
        {
            state.HasPlantTarget = false;
            state.PlantTargetEventIdentity = 0;
            state.PlantTargetKind = CharacterFootPlantTargetKind.None;
            state.PlantLockResponse = CharacterFootLockResponse.None;
            state.PlantTargetVerified = false;
            state.PlantDirectFollow = false;
            state.PlantDesiredPoint = default;
            state.PlantFilteredPoint = default;
            state.PreviousPlantSelectedWorldTarget = default;
            state.PlantWorldResidual = default;
            state.PlantWorldResidualTransitionActive = false;
            state.PlantFact = default;
        }

        static void UpdateCorrectionResponseLineage(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateFrame frame)
        {
            if (!state.HasCorrectionResponseLineage)
            {
                SetCorrectionResponseLineage(ref state, in frame);
                return;
            }
            CharacterFootCorrectionResponseInitializationReason reason =
                CharacterFootCorrectionResponseInitializationReason.None;
            if (!state.CorrectionResponseSourceLineage.Equals(
                    frame.SourceLineage))
            {
                reason = CharacterFootCorrectionResponseInitializationReason
                    .SourceLineageInvalidated;
            }
            else if (!state.CorrectionResponseProfileRevision.Equals(
                         frame.ProfileRevision))
            {
                reason = CharacterFootCorrectionResponseInitializationReason
                    .ProfileLineageInvalidated;
            }
            else if (state.CorrectionResponseWorldRevision !=
                     frame.WorldRevision)
            {
                reason = CharacterFootCorrectionResponseInitializationReason
                    .WorldLineageInvalidated;
            }
            if (reason ==
                CharacterFootCorrectionResponseInitializationReason.None)
            {
                return;
            }
            ClearCorrectionResponse(ref state, reason);
            SetCorrectionResponseLineage(ref state, in frame);
        }

        static void SetCorrectionResponseLineage(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateFrame frame)
        {
            state.HasCorrectionResponseLineage = true;
            state.CorrectionResponseSourceLineage = frame.SourceLineage;
            state.CorrectionResponseProfileRevision = frame.ProfileRevision;
            state.CorrectionResponseWorldRevision = frame.WorldRevision;
        }

        static void ClearCorrectionResponse(
            ref CharacterFootInterpolationState state,
            CharacterFootCorrectionResponseInitializationReason reason)
        {
            state.HasCorrectionResponse = false;
            state.CorrectionResponse = 0f;
            state.CorrectionResponseDomain = CharacterFootCorrectionResponseDomain.None;
            state.CorrectionResponseFact = default;
            state.HasPreviousResponseOutputPoint = false;
            state.PreviousResponseOutputPoint = default;
            state.PendingCorrectionResponseInitializationReason = reason;
        }

        static void ResetInterpolation(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateFrame frame,
            CharacterFootCorrectionResponseInitializationReason reason)
        {
            state = default;
            SetCorrectionResponseLineage(ref state, in frame);
            state.PendingCorrectionResponseInitializationReason = reason;
        }

        static CharacterFootCorrectionResponseInitializationReason
            ResolveSuppressedInitializationReason(
                CharacterFootCorrectionResponseInitializationReason pending,
                CharacterFootCorrectionResponseInitializationReason current)
        {
            if (pending !=
                    CharacterFootCorrectionResponseInitializationReason.None &&
                pending != CharacterFootCorrectionResponseInitializationReason
                    .FirstLegalInput &&
                pending != CharacterFootCorrectionResponseInitializationReason
                    .PolicyExited)
            {
                return pending;
            }
            if (current == CharacterFootCorrectionResponseInitializationReason
                    .SourceLineageInvalidated)
            {
                return current;
            }
            return pending !=
                   CharacterFootCorrectionResponseInitializationReason.None
                ? pending
                : current;
        }

        static Vector3 Advance(
            Vector3 current,
            Vector3 target,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return current;
            float alpha = 1f -
                          Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            return Vector3.LerpUnclamped(current, target, alpha);
        }

        static float ResolveSwingResidualHalfLife(
            Vector3 residual,
            float timeToLandingSeconds,
            CharacterFootMotionSettings settings,
            out bool deadlineHalfLifeAvailable,
            out float deadlineHalfLifeSeconds)
        {
            deadlineHalfLifeAvailable = false;
            deadlineHalfLifeSeconds = 0f;
            float halfLifeSeconds =
                settings.EffectiveCorrectionHalfLifeSeconds;
            float residualDistance = residual.magnitude;
            if (!float.IsFinite(residualDistance) ||
                residualDistance <= settings.SwingResidualTolerance ||
                !float.IsFinite(timeToLandingSeconds) ||
                timeToLandingSeconds <= 0f)
            {
                return halfLifeSeconds;
            }
            float halfLifeCount = Mathf.Log(
                residualDistance / settings.SwingResidualTolerance,
                2f);
            if (!float.IsFinite(halfLifeCount) || halfLifeCount <= 0f)
                return halfLifeSeconds;
            float candidate = timeToLandingSeconds / halfLifeCount;
            if (!float.IsFinite(candidate) || candidate <= 0f)
                return halfLifeSeconds;
            deadlineHalfLifeAvailable = true;
            deadlineHalfLifeSeconds = candidate;
            return Mathf.Min(halfLifeSeconds, candidate);
        }

        static float ResolveVerticalHistoryDelta(
            float deltaSeconds,
            float maximumSpeed)
        {
            if (deltaSeconds <= 0f)
                return 0f;
            return maximumSpeed * deltaSeconds;
        }

    }
}
