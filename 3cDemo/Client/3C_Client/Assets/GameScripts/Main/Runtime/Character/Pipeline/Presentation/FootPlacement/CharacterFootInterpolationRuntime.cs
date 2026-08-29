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
                !CharacterFootConstraintMath.Finite(target.PlantTargetPoint))
            {
                throw new System.InvalidOperationException(
                    "Foot Plant target is invalid.");
            }
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
                    false,
                    false,
                    false,
                    false,
                    0f,
                    target.TimeToLandingSeconds,
                    in supportIntent),
                in frame);
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            bool sameTarget = state.HasPlantTarget &&
                              state.PlantTargetEventIdentity ==
                              target.PlantTargetEventIdentity;
            Vector3 previousDesiredPoint = state.PlantDesiredPoint;
            if (!sameTarget)
            {
                state.HasPlantTarget = true;
                state.PlantTargetEventIdentity =
                    target.PlantTargetEventIdentity;
                state.PlantFilteredPoint =
                    originalSole + state.EffectiveCorrection;
                state.PlantBlendWeight = 0f;
                state.PlantResponseTransitionActive = false;
            }
            Vector3 targetDelta =
                target.PlantTargetPoint - state.PlantFilteredPoint;
            float targetVerticalDelta = Vector3.Dot(targetDelta, up);
            float maximumTargetDelta =
                frame.Settings.MaximumVerticalTargetSpeed *
                frame.DeltaSeconds;
            float targetAppliedVerticalDelta = target.DirectPlantFollow
                ? targetVerticalDelta
                : Mathf.Clamp(
                    targetVerticalDelta,
                    -maximumTargetDelta,
                    maximumTargetDelta);
            bool targetVerticalClamped = !Mathf.Approximately(
                targetVerticalDelta,
                targetAppliedVerticalDelta);
            state.PlantFilteredPoint +=
                Vector3.ProjectOnPlane(targetDelta, up) +
                up * targetAppliedVerticalDelta;
            state.PlantDesiredPoint = target.PlantTargetPoint;
            state.PlantBlendWeight = Mathf.Max(
                state.PlantBlendWeight,
                Mathf.Clamp01(target.Progress));
            Vector3 filteredCorrection =
                state.PlantFilteredPoint - originalSole;
            Vector3 blendedCorrection = Vector3.LerpUnclamped(
                swing.Correction,
                filteredCorrection,
                state.PlantBlendWeight);
            bool targetRevised = sameTarget &&
                                 !target.DirectPlantFollow &&
                                 Vector3.Distance(
                                     previousDesiredPoint,
                                     target.PlantTargetPoint) >
                                 frame.Settings.LandingAcceptanceDistance;
            bool captureTransition = !sameTarget ||
                                     target.ResponseEntered ||
                                     targetRevised;
            if (captureTransition)
            {
                state.Residual =
                    state.EffectiveCorrection - blendedCorrection;
                state.PlantResponseTransitionActive =
                    state.Residual.sqrMagnitude >
                    CharacterFootConstraintMath.GeometryEpsilon *
                    CharacterFootConstraintMath.GeometryEpsilon;
            }
            if (state.PlantResponseTransitionActive)
            {
                float halfLifeSeconds = ResolveSwingResidualHalfLife(
                    state.Residual,
                    target.TimeToLandingSeconds,
                    frame.Settings,
                    out _,
                    out _);
                state.Residual = Advance(
                    state.Residual,
                    default,
                    frame.DeltaSeconds,
                    halfLifeSeconds);
                if (state.Residual.magnitude <=
                    frame.Settings.LandingLockCompletionTolerance)
                {
                    state.Residual = default;
                    state.PlantResponseTransitionActive = false;
                }
            }
            else
            {
                state.Residual = default;
            }
            Vector3 correctionTarget =
                blendedCorrection + state.Residual;
            Vector3 correctionDelta =
                correctionTarget - state.EffectiveCorrection;
            float correctionVerticalDelta =
                Vector3.Dot(correctionDelta, up);
            float maximumCorrectionDelta =
                frame.Settings.MaximumVerticalCorrectionSpeed *
                frame.DeltaSeconds;
            float correctionAppliedVerticalDelta = target.DirectPlantFollow
                ? correctionVerticalDelta
                : Mathf.Clamp(
                    correctionVerticalDelta,
                    -maximumCorrectionDelta,
                    maximumCorrectionDelta);
            bool correctionVerticalClamped = !Mathf.Approximately(
                correctionVerticalDelta,
                correctionAppliedVerticalDelta);
            state.EffectiveCorrection +=
                Vector3.ProjectOnPlane(correctionDelta, up) +
                up * correctionAppliedVerticalDelta;
            state.PreviousTargetCorrection = blendedCorrection;
            state.Progress = state.PlantBlendWeight;
            state.StartResidual = 0f;
            Vector3 outputPoint = originalSole + state.EffectiveCorrection;
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
                target.PlantTargetPoint,
                state.PlantFilteredPoint,
                state.PlantBlendWeight,
                frame.Settings.MaximumVerticalTargetSpeed,
                targetVerticalDelta,
                targetAppliedVerticalDelta,
                targetVerticalClamped,
                blendedCorrection,
                frame.Settings.MaximumVerticalCorrectionSpeed,
                correctionVerticalDelta,
                correctionAppliedVerticalDelta,
                correctionVerticalClamped,
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
            bool sameHeightTarget = hasPath &&
                                    state.HasSwingTargetHeight &&
                                    state.SwingTargetHeightEventIdentity ==
                                    swing.LandingEventIdentity;
            float filteredTargetHeightBefore =
                state.SwingFilteredTargetHeightAlongUp;
            if (hasPath && !sameHeightTarget)
            {
                state.HasSwingTargetHeight = true;
                state.SwingTargetHeightEventIdentity =
                    swing.LandingEventIdentity;
                state.SwingFilteredTargetHeightAlongUp =
                    originalSoleHeight +
                    Vector3.Dot(state.EffectiveCorrection, up);
            }
            if (hasPath)
            {
                filteredTargetHeightBefore =
                    state.SwingFilteredTargetHeightAlongUp;
                float targetHeightDelta =
                    swing.FormalTargetHeightAlongUp -
                    state.SwingFilteredTargetHeightAlongUp;
                float maximumHeightDelta = ResolveVerticalHistoryDelta(
                    frame.DeltaSeconds,
                    frame.Settings.MaximumVerticalTargetSpeed);
                float appliedHeightDelta = Mathf.Clamp(
                    targetHeightDelta,
                    -maximumHeightDelta,
                    maximumHeightDelta);
                state.SwingFilteredTargetHeightAlongUp += appliedHeightDelta;
            }
            else
            {
                state.HasSwingTargetHeight = false;
                state.SwingTargetHeightEventIdentity = 0;
                state.SwingFilteredTargetHeightAlongUp = 0f;
            }
            Vector3 swingTargetCorrection = hasPath
                ? up * Mathf.Max(
                    0f,
                    state.SwingFilteredTargetHeightAlongUp -
                    originalSoleHeight)
                : default;
            float landingPointDelta = comparablePath
                ? Vector3.Distance(
                    previousLandingPoint,
                    swingPath.LandingPoint)
                : 0f;
            float targetDelta = comparablePath
                ? Vector3.Distance(
                    previousTargetCorrection,
                    swingTargetCorrection)
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
            bool targetTrackingApplied = comparablePath &&
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
                hasPath ? swing.FormalTargetHeightAlongUp : 0f,
                hasPath ? filteredTargetHeightBefore : 0f,
                hasPath
                    ? swing.FormalTargetHeightAlongUp -
                      filteredTargetHeightBefore
                    : 0f,
                hasPath
                    ? state.SwingFilteredTargetHeightAlongUp -
                      filteredTargetHeightBefore
                    : 0f,
                hasPath && !Mathf.Approximately(
                    swing.FormalTargetHeightAlongUp -
                    filteredTargetHeightBefore,
                    state.SwingFilteredTargetHeightAlongUp -
                    filteredTargetHeightBefore),
                frame.Settings.MaximumVerticalTargetSpeed,
                hasPath ? state.SwingFilteredTargetHeightAlongUp : 0f);
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
            state.PlantDesiredPoint = default;
            state.PlantFilteredPoint = default;
            state.PlantBlendWeight = 0f;
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
