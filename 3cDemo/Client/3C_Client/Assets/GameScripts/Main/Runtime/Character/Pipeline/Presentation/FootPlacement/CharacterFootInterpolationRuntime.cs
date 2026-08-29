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
            if (target.SuppressOutput ||
                target.InterpolationPolicy ==
                CharacterFootInterpolationPolicy.Suppressed)
            {
                state = default;
                return new CharacterFootInterpolationResult(
                    default,
                    false,
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
            state = default;
            state.HasOutput = true;
            state.EffectiveCorrection = correction;
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
            Vector3 effectiveCorrectionBefore = state.EffectiveCorrection;
            Vector3 currentOutputBefore =
                originalSole + effectiveCorrectionBefore;
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
            bool matchingTargetHeight = state.HasTargetHeight &&
                                        state.TargetHeightEventIdentity ==
                                        target.PlantTargetEventIdentity;
            bool targetHeightInitialized = !matchingTargetHeight;
            if (targetHeightInitialized)
            {
                state.HasTargetHeight = true;
                state.TargetHeightEventIdentity =
                    target.PlantTargetEventIdentity;
                state.FilteredTargetHeightAlongUp = Vector3.Dot(
                    currentOutputBefore,
                    up);
                state.TargetHeightRetargetActive = false;
            }
            float targetHeightBefore = state.FilteredTargetHeightAlongUp;
            float desiredTargetHeightAlongUp = Vector3.Dot(
                target.PlantTargetPoint,
                up);
            float targetVerticalDelta = desiredTargetHeightAlongUp -
                                        targetHeightBefore;
            float maximumTargetDelta =
                frame.Settings.MaximumVerticalTargetSpeed *
                frame.DeltaSeconds;
            bool verificationRefresh = target.PlantTargetVerified &&
                                       (!sameTarget || !previousVerified);
            bool distanceForceRefresh = !target.DirectPlantFollow &&
                                        !verificationRefresh &&
                                        Mathf.Abs(targetVerticalDelta) >=
                                        frame.Settings
                                            .TargetHeightForceRefreshDistance;
            bool targetForceRefreshed = verificationRefresh ||
                                        distanceForceRefresh;
            float targetAppliedVerticalDelta = target.DirectPlantFollow ||
                                               targetForceRefreshed
                ? targetVerticalDelta
                : Mathf.Clamp(
                    targetVerticalDelta,
                    -maximumTargetDelta,
                    maximumTargetDelta);
            bool targetVerticalClamped = !Mathf.Approximately(
                targetVerticalDelta,
                targetAppliedVerticalDelta);
            state.FilteredTargetHeightAlongUp += targetAppliedVerticalDelta;
            CharacterFootPlantTargetHeightUpdateReason targetHeightUpdateReason;
            if (target.DirectPlantFollow)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.DirectFollow;
            }
            else if (verificationRefresh)
            {
                targetHeightUpdateReason = CharacterFootPlantTargetHeightUpdateReason
                    .VerificationRefresh;
            }
            else if (distanceForceRefresh)
            {
                targetHeightUpdateReason = CharacterFootPlantTargetHeightUpdateReason
                    .ForceRefreshDistanceExceeded;
            }
            else if (targetVerticalClamped)
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.RateLimited;
            }
            else if (!Mathf.Approximately(targetVerticalDelta, 0f))
            {
                targetHeightUpdateReason =
                    CharacterFootPlantTargetHeightUpdateReason.WithinRate;
            }
            else
            {
                targetHeightUpdateReason = targetHeightInitialized
                    ? CharacterFootPlantTargetHeightUpdateReason.Initialized
                    : CharacterFootPlantTargetHeightUpdateReason.None;
            }
            state.PlantFilteredPoint = Vector3.ProjectOnPlane(
                target.PlantTargetPoint,
                up) + up * state.FilteredTargetHeightAlongUp;
            if (!sameTarget)
                state.PlantBlendWeight = 0f;
            state.PlantBlendWeight = Mathf.Max(
                state.PlantBlendWeight,
                Mathf.Clamp01(target.Progress));
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
            Vector3 previousWeightTarget = Vector3.LerpUnclamped(
                swingWorldTarget,
                state.PlantFilteredPoint,
                previousBlendWeight);
            if (sameTarget &&
                Vector3.Distance(previousWeightTarget, mixedWorldTarget) >
                frame.Settings.SwingResidualTolerance)
            {
                captureReason |=
                    CharacterFootPlantResidualCaptureReason.WeightChanged;
            }
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
                state.PlantResponseTransitionActive =
                    state.PlantWorldResidual.sqrMagnitude >
                    CharacterFootConstraintMath.GeometryEpsilon *
                    CharacterFootConstraintMath.GeometryEpsilon;
            }
            Vector3 residualAfterCapture = state.PlantWorldResidual;
            if (!captureTransition && state.PlantResponseTransitionActive)
            {
                float halfLifeSeconds = ResolveSwingResidualHalfLife(
                    state.PlantWorldResidual,
                    target.TimeToLandingSeconds,
                    frame.Settings,
                    out _,
                    out _);
                state.PlantWorldResidual = Advance(
                    state.PlantWorldResidual,
                    default,
                    frame.DeltaSeconds,
                    halfLifeSeconds);
                if (state.PlantWorldResidual.magnitude <=
                    frame.Settings.LandingLockCompletionTolerance)
                {
                    state.PlantWorldResidual = default;
                    state.PlantResponseTransitionActive = false;
                }
            }
            Vector3 outputPoint =
                mixedWorldTarget + state.PlantWorldResidual;
            state.EffectiveCorrection = outputPoint - originalSole;
            CharacterFootVerticalContinuityOwner verticalContinuityOwner;
            CharacterFootCorrectionStageDisposition correctionStageDisposition;
            if (state.PlantResponseTransitionActive)
            {
                verticalContinuityOwner =
                    CharacterFootVerticalContinuityOwner.PlantWorldResidual;
                correctionStageDisposition = CharacterFootCorrectionStageDisposition
                    .BypassedByWorldResidualOwner;
            }
            else if (target.DirectPlantFollow)
            {
                verticalContinuityOwner =
                    CharacterFootVerticalContinuityOwner.DirectPlantTarget;
                correctionStageDisposition =
                    CharacterFootCorrectionStageDisposition.DirectFollow;
            }
            else
            {
                verticalContinuityOwner =
                    CharacterFootVerticalContinuityOwner.TargetHeightHistory;
                correctionStageDisposition = CharacterFootCorrectionStageDisposition
                    .BypassedByTargetHeightOwner;
            }
            state.HasPlantTarget = true;
            state.PlantTargetEventIdentity = target.PlantTargetEventIdentity;
            state.PlantTargetKind = target.PlantTargetKind;
            state.PlantLockResponse = target.PlantLockResponse;
            state.PlantTargetVerified = target.PlantTargetVerified;
            state.PlantDirectFollow = target.DirectPlantFollow;
            state.PlantDesiredPoint = target.PlantTargetPoint;
            state.PreviousPlantMixedWorldTarget = mixedWorldTarget;
            state.PreviousTargetCorrection = mixedWorldTarget - originalSole;
            state.Progress = state.PlantBlendWeight;
            state.StartResidual = 0f;
            float outputDistance = Vector3.Distance(
                outputPoint,
                target.PlantTargetPoint);
            float penetrationDepth = Mathf.Max(
                0f,
                Vector3.Dot(target.PlantTargetPoint - outputPoint, up));
            state.Completed = target.PlantTargetVerified &&
                              state.PlantBlendWeight >=
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
                frame.Settings.MaximumVerticalTargetSpeed,
                targetHeightBefore,
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
                residualAfterCapture,
                state.PlantWorldResidual,
                verticalContinuityOwner,
                correctionStageDisposition,
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
            if (target.StateEntered)
            {
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
                else
                {
                    state.FilteredTargetHeightAlongUp =
                        currentLandingHeightAlongUp;
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
                hasPath
                    ? filteredTargetHeightAlongUp
                    : 0f);
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
                frame.Settings);

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
            state.PlantBlendWeight = 0f;
            state.PreviousPlantMixedWorldTarget = default;
            state.PlantWorldResidual = default;
            state.PlantResponseTransitionActive = false;
            state.PlantFact = default;
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
