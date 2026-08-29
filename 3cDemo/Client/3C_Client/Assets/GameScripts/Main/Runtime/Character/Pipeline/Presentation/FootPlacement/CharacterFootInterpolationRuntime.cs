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
                    CharacterFootPathContinuityFact.CreateUnevaluated(
                        target.TimeToLandingSeconds,
                        frame.Settings,
                        frame.ComponentUp.normalized),
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
                    ClearPlant(ref state, true);
                    return EvaluateSwing(ref state, in target, in frame);
                case CharacterFootInterpolationPolicy.PlantBlend:
                    return EvaluatePlant(ref state, in target, in frame);
                case CharacterFootInterpolationPolicy.ReleaseResidual:
                    return EvaluateRelease(ref state, in target, in frame);
                default:
                    throw new System.InvalidOperationException(
                        "Foot interpolation policy is invalid.");
            }
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
            CharacterFootCorrectionResponseInitializationReason reason =
                state.HasCorrectionResponse
                    ? CharacterFootCorrectionResponseInitializationReason
                        .PolicyExited
                    : state.PendingCorrectionResponseInitializationReason;
            FixedString128Bytes sourceLineage =
                state.CorrectionResponseSourceLineage;
            FixedString128Bytes profileRevision =
                state.CorrectionResponseProfileRevision;
            ulong worldRevision = state.CorrectionResponseWorldRevision;
            bool hasLineage = state.HasCorrectionResponseLineage;
            state = default;
            state.HasOutput = true;
            state.EffectiveCorrection = correction;
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
                target.PlantTargetEventIdentity == 0 ||
                target.PlantTargetKind == CharacterFootPlantTargetKind.None ||
                !CharacterFootConstraintMath.Finite(target.PlantTargetPoint))
            {
                throw new System.InvalidOperationException(
                    "Foot Plant target is invalid.");
            }
            Vector3 up = frame.ComponentUp.normalized;
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
                    false,
                    false,
                    0f,
                    target.TimeToLandingSeconds,
                    in supportIntent),
                in frame);
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
            Vector3 previousMixedWorldTarget =
                state.PreviousPlantMixedWorldTarget;
            float previousBlendWeight = state.PlantBlendWeight;
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
            if (!sameTarget)
                state.PlantBlendWeight = 0f;
            if (sameTarget &&
                target.PlantTakeoverProgress < previousBlendWeight)
            {
                throw new System.InvalidOperationException(
                    "Foot Plant takeover progress regressed.");
            }
            state.PlantBlendWeight = target.PlantTakeoverProgress;
            bool takeoverWeightAdvanced = sameTarget &&
                                           state.PlantBlendWeight >
                                           previousBlendWeight;
            bool retainTakeoverTracking = sameTarget &&
                                          previousTargetKind == CharacterFootPlantTargetKind
                                              .PreparedPrediction &&
                                          target.PlantTargetKind == CharacterFootPlantTargetKind
                                              .PreparedPrediction &&
                                          state.PlantWorldResidualTakeoverTrackingActive;
            Vector3 swingWorldTarget = originalSole + swing.Correction;
            Vector3 mixedWorldTarget = Vector3.LerpUnclamped(
                swingWorldTarget,
                state.PlantFilteredPoint,
                state.PlantBlendWeight);
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
            bool takeoverStarted = sameTarget &&
                                   previousBlendWeight <=
                                   CharacterFootConstraintMath.GeometryEpsilon &&
                                   state.PlantBlendWeight >
                                   CharacterFootConstraintMath.GeometryEpsilon;
            bool takeoverCompleted = sameTarget &&
                                     previousBlendWeight <
                                     1f - CharacterFootConstraintMath.GeometryEpsilon &&
                                     state.PlantBlendWeight >=
                                     1f - CharacterFootConstraintMath.GeometryEpsilon;
            if (takeoverStarted)
                captureReason |=
                    CharacterFootPlantResidualCaptureReason.TakeoverStarted;
            if (takeoverCompleted)
                captureReason |=
                    CharacterFootPlantResidualCaptureReason.TakeoverCompleted;
            if (takeoverWeightAdvanced)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TakeoverWeightAdvanced;
            if (targetRevised)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetPointRevised;
            if (targetForceRefreshed)
                captureReason |= CharacterFootPlantResidualCaptureReason
                    .TargetHeightForceRefreshed;
            Vector3 residualBeforeCapture = state.PlantWorldResidual;
            bool captureTransition = captureReason !=
                                     CharacterFootPlantResidualCaptureReason.None;
            if (captureTransition)
            {
                state.PlantWorldResidual =
                    currentOutputBefore - mixedWorldTarget;
                state.PlantWorldResidualTransitionActive =
                    state.PlantWorldResidual.sqrMagnitude >
                    CharacterFootConstraintMath.GeometryEpsilon *
                    CharacterFootConstraintMath.GeometryEpsilon;
            }
            state.PlantWorldResidualTakeoverTrackingActive =
                state.PlantWorldResidualTransitionActive &&
                (takeoverWeightAdvanced || retainTakeoverTracking);
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
                residualAppliedHalfLifeSeconds =
                    state.PlantWorldResidualTakeoverTrackingActive
                    ? residualBaseHalfLifeSeconds
                    : ResolveSwingResidualHalfLife(
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
                    state.PlantWorldResidualTakeoverTrackingActive = false;
                    residualClearedAtCompletionTolerance = true;
                }
            }
            Vector3 residualAfterDecay = state.PlantWorldResidual;
            Vector3 desiredOutputPoint =
                mixedWorldTarget + residualAfterDecay;
            float desiredResponse = Vector3.Dot(
                desiredOutputPoint - originalSole,
                up);
            bool correctionResponseInitializedBefore =
                state.HasCorrectionResponse;
            bool correctionResponseInitializedThisFrame =
                !correctionResponseInitializedBefore;
            CharacterFootCorrectionResponseInitializationReason
                correctionResponseInitializationReason =
                    CharacterFootCorrectionResponseInitializationReason.None;
            float previousResponse = correctionResponseInitializedBefore
                ? state.CorrectionResponseAlongUp
                : desiredResponse;
            float currentResponse = correctionResponseInitializedBefore
                ? previousResponse
                : desiredResponse;
            CharacterFootCorrectionResponseDirection responseDirection =
                CharacterFootCorrectionResponseDirection.None;
            float selectedResponseSpeed = 0f;
            float responseAppliedDelta = 0f;
            if (correctionResponseInitializedThisFrame)
            {
                correctionResponseInitializationReason =
                    state.PendingCorrectionResponseInitializationReason !=
                    CharacterFootCorrectionResponseInitializationReason.None
                        ? state.PendingCorrectionResponseInitializationReason
                        : CharacterFootCorrectionResponseInitializationReason
                            .FirstLegalInput;
            }
            else
            {
                float responseDelta = desiredResponse - previousResponse;
                if (responseDelta != 0f)
                {
                    responseDirection = responseDelta > 0f
                        ? CharacterFootCorrectionResponseDirection.Increase
                        : CharacterFootCorrectionResponseDirection.Decrease;
                    selectedResponseSpeed = responseDelta > 0f
                        ? frame.Settings.CorrectionResponseIncreaseSpeed
                        : frame.Settings.CorrectionResponseDecreaseSpeed;
                    float maximumResponseDelta =
                        selectedResponseSpeed * frame.DeltaSeconds;
                    responseAppliedDelta = Mathf.Clamp(
                        responseDelta,
                        -maximumResponseDelta,
                        maximumResponseDelta);
                    currentResponse = previousResponse +
                                      responseAppliedDelta;
                }
            }
            Vector3 responseOutputPoint = desiredOutputPoint +
                                          up *
                                          (currentResponse - desiredResponse);
            var correctionResponseFact =
                new CharacterFootCorrectionResponseFact(
                    true,
                    correctionResponseInitializedBefore,
                    correctionResponseInitializedThisFrame,
                    correctionResponseInitializationReason,
                    previousResponseOutputAvailable,
                    currentOutputBefore,
                    desiredOutputPoint,
                    responseOutputPoint,
                    desiredResponse,
                    previousResponse,
                    currentResponse,
                    responseDirection,
                    selectedResponseSpeed,
                    responseAppliedDelta);
            state.HasCorrectionResponse = true;
            state.CorrectionResponseAlongUp = currentResponse;
            state.PendingCorrectionResponseInitializationReason =
                CharacterFootCorrectionResponseInitializationReason.None;
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
            if (correctionResponseInitializedThisFrame ||
                !Mathf.Approximately(currentResponse, desiredResponse) ||
                !Mathf.Approximately(responseAppliedDelta, 0f))
            {
                verticalContinuityOwners |= CharacterFootVerticalContinuityOwner
                    .CorrectionResponseHistory;
            }
            if (!Mathf.Approximately(
                    previousBlendWeight,
                    state.PlantBlendWeight))
            {
                verticalContinuityOwners |=
                    CharacterFootVerticalContinuityOwner.PlantWeightBlend;
            }
            state.HasPlantTarget = true;
            state.PlantTargetEventIdentity = target.PlantTargetEventIdentity;
            state.PlantTargetKind = target.PlantTargetKind;
            state.PlantLockResponse = target.PlantLockResponse;
            state.PlantTargetVerified = target.PlantTargetVerified;
            state.PlantDirectFollow = target.DirectPlantFollow;
            state.PlantDesiredPoint = target.PlantTargetPoint;
            state.PreviousPlantMixedWorldTarget = mixedWorldTarget;
            state.HasPreviousResponseOutputPoint = true;
            state.PreviousResponseOutputPoint = responseOutputPoint;
            state.PreviousTargetCorrection = mixedWorldTarget - originalSole;
            state.Progress = state.PlantBlendWeight;
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
                              frame.LockRequest.RequestsLock &&
                              frame.LockRequest.EventIdentity ==
                              target.PlantTargetEventIdentity &&
                              frame.LockRequest.Weight >=
                              1f - CharacterFootConstraintMath.GeometryEpsilon &&
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
                previousBlendWeight,
                state.PlantBlendWeight,
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
                previousMixedWorldTarget,
                mixedWorldTarget,
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
                in correctionResponseFact,
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
                state.PreviousTargetCorrection = target.Correction;
                state.Residual = Advance(
                    state.Residual,
                    default,
                    frame.DeltaSeconds,
                    frame.Settings.EffectiveCorrectionHalfLifeSeconds);
                state.EffectiveCorrection =
                    target.Correction + state.Residual;
            }
            Vector3 releaseOutputPoint =
                originalSole + state.EffectiveCorrection;
            state.HasPreviousResponseOutputPoint = true;
            state.PreviousResponseOutputPoint = releaseOutputPoint;
            if (state.HasCorrectionResponse)
            {
                state.CorrectionResponseAlongUp = Vector3.Dot(
                    releaseOutputPoint - originalSole,
                    frame.ComponentUp.normalized);
            }
            state.Completed = frame.LockRequest.Weight <=
                              CharacterFootConstraintMath.GeometryEpsilon &&
                              Vector3.Distance(
                                  state.EffectiveCorrection,
                                  target.SwingCorrection) <=
                              frame.Settings.ReleaseCompletionTolerance;
            return Result(
                in state,
                state.Completed,
                Unevaluated(in target, in frame));
        }

        static CharacterFootInterpolationResult EvaluateSwing(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            bool pathAvailableBefore = state.HasSwingPath;
            ulong previousLandingEventIdentity =
                state.SwingLandingEventIdentity;
            ulong previousGroundPathInputIdentity =
                state.SwingGroundPathInputIdentity;
            Vector3 previousLandingPoint = state.SwingLandingPoint;
            Vector3 previousTargetCorrection =
                state.PreviousSwingTargetCorrection;
            Vector3 residualBeforeRevision = state.SwingResidual;
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            CharacterFootSwingPathReference swingPath =
                swing.SwingPathReference;
            bool hasPath = swing.Accepted &&
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
            float currentLandingHeightAlongUp = hasPath
                ? Vector3.Dot(swingPath.LandingPoint, up)
                : 0f;
            float rawTargetCorrectionAlongUp = hasPath
                ? Mathf.Max(0f, rawTargetHeightAlongUp - originalSoleHeight)
                : 0f;
            float landingPointDelta = comparablePath
                ? Vector3.Distance(
                    previousLandingPoint,
                    swingPath.LandingPoint)
                : 0f;
            float landingHeightDelta = comparablePath
                ? Mathf.Abs(Vector3.Dot(
                    swingPath.LandingPoint - previousLandingPoint,
                    up))
                : 0f;
            bool sameEvent = comparablePath &&
                             previousLandingEventIdentity ==
                             swing.LandingEventIdentity;
            bool groundPathInputChanged = sameEvent &&
                                          previousGroundPathInputIdentity !=
                                          swing.GroundPathInputIdentity;
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
                    currentLandingHeightAlongUp;
                state.TargetHeightRetargetActive = false;
            }
            if (hasPath && ownsSwingTargetHeight)
            {
                filteredTargetHeightBefore = rawTargetHeightAlongUp +
                                             state
                                                 .FilteredTargetHeightAlongUp -
                                             currentLandingHeightAlongUp;
            }
            else if (hasPath)
            {
                filteredTargetHeightBefore = rawTargetHeightAlongUp;
            }
            if (hasPath && ownsSwingTargetHeight)
            {
                float targetHeightDelta = currentLandingHeightAlongUp -
                                          state.FilteredTargetHeightAlongUp;
                if (directTargetHeightAdoption)
                {
                    state.FilteredTargetHeightAlongUp =
                        currentLandingHeightAlongUp;
                    state.TargetHeightRetargetActive = false;
                }
                else
                {
                    bool retargetRequested = groundPathInputChanged &&
                                             landingHeightDelta >
                                             frame.Settings.PathRevisionDistance;
                    if (retargetRequested &&
                        Mathf.Abs(targetHeightDelta) >=
                        frame.Settings.TargetHeightForceRefreshDistance)
                    {
                        state.FilteredTargetHeightAlongUp =
                            currentLandingHeightAlongUp;
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
                      currentLandingHeightAlongUp
                    : rawTargetHeightAlongUp
                : 0f;
            Vector3 swingTargetCorrection = hasPath
                ? up * Mathf.Max(
                    0f,
                    filteredTargetHeightAlongUp - originalSoleHeight)
                : default;
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
            if (comparablePath &&
                landingPointDelta > frame.Settings.PathRevisionDistance)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.LandingPointChanged;
            }
            bool revised = revisionReason !=
                           CharacterFootPathRevisionReason.None;
            bool targetTrackingApplied = groundPathInputChanged &&
                targetDelta > frame.Settings.PathRevisionDistance;
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
            state.SwingGroundPathInputIdentity = hasPath
                ? swing.GroundPathInputIdentity
                : 0;
            state.SwingLandingPoint = hasPath
                ? swingPath.LandingPoint
                : default;
            state.PreviousTargetCorrection = hasPath
                ? swingTargetCorrection
                : default;
            state.PreviousSwingTargetCorrection = hasPath
                ? swingTargetCorrection
                : default;
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
            if (state.Policy == CharacterFootInterpolationPolicy.SwingResidual)
            {
                state.EffectiveCorrection = swingCorrection;
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
                hasPath ? swingTargetCorrection : default,
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
                up);
            return new CharacterFootInterpolationResult(
                swingCorrection,
                false,
                in continuityFact,
                in state.PlantFact);
        }

        static CharacterFootInterpolationResult Result(
            in CharacterFootInterpolationState state,
            bool completed,
            in CharacterFootPathContinuityFact continuityFact) =>
            new CharacterFootInterpolationResult(
                state.EffectiveCorrection,
                completed,
                in continuityFact,
                in state.PlantFact);

        static CharacterFootPathContinuityFact Unevaluated(
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame) =>
            CharacterFootPathContinuityFact.CreateUnevaluated(
                target.TimeToLandingSeconds,
                frame.Settings,
                frame.ComponentUp.normalized);

        static void ClearPlant(
            ref CharacterFootInterpolationState state,
            bool exitCorrectionResponse)
        {
            state.HasPlantTarget = false;
            state.PlantTargetEventIdentity = 0;
            state.PlantTargetKind = CharacterFootPlantTargetKind.None;
            state.PlantLockResponse = CharacterFootLockResponse.None;
            state.PlantTargetVerified = false;
            state.PlantDirectFollow = false;
            state.PlantDesiredPoint = default;
            state.PlantFilteredPoint = default;
            state.PlantBlendWeight = 0f;
            state.PreviousPlantMixedWorldTarget = default;
            state.PlantWorldResidual = default;
            state.PlantWorldResidualTransitionActive = false;
            state.PlantWorldResidualTakeoverTrackingActive = false;
            state.PlantFact = default;
            if (exitCorrectionResponse && state.HasCorrectionResponse)
            {
                ClearCorrectionResponse(
                    ref state,
                    CharacterFootCorrectionResponseInitializationReason
                        .PolicyExited);
            }
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
            state.CorrectionResponseAlongUp = 0f;
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
