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
                    return EvaluateSwing(ref state, in target, in frame);
                case CharacterFootInterpolationPolicy.AcquireByWeight:
                    return EvaluateAcquire(ref state, in target, in frame);
                case CharacterFootInterpolationPolicy.Direct:
                    state.PreviousTargetCorrection = target.Correction;
                    state.Residual = default;
                    state.Progress = 1f;
                    state.StartResidual = 0f;
                    state.Completed = false;
                    state.EffectiveCorrection = target.Correction;
                    return Result(
                        in state,
                        false,
                        Unevaluated(in target, in frame));
                case CharacterFootInterpolationPolicy.HalfLife:
                    if (!target.ResponseEntered)
                    {
                        state.EffectiveCorrection = Advance(
                            state.EffectiveCorrection,
                            target.Correction,
                            frame.DeltaSeconds,
                            frame.Settings.EffectiveCorrectionHalfLifeSeconds);
                    }
                    state.PreviousTargetCorrection = target.Correction;
                    state.Residual =
                        state.EffectiveCorrection - target.Correction;
                    state.Progress = 1f;
                    state.StartResidual = 0f;
                    state.Completed = false;
                    return Result(
                        in state,
                        false,
                        Unevaluated(in target, in frame));
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

        static CharacterFootInterpolationResult EvaluateAcquire(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            CharacterFootPathContinuityFact continuityFact =
                Unevaluated(in target, in frame);
            if (target.StateEntered)
            {
                CharacterFootInterpolationResult swing = EvaluateSwing(
                    ref state,
                    new CharacterFootStateTarget(
                        target.SwingCorrection,
                        target.SwingCorrection,
                        CharacterFootInterpolationPolicy.SwingResidual,
                        false,
                        false,
                        false,
                        0f,
                        target.TimeToLandingSeconds),
                    in frame);
                continuityFact = swing.ContinuityFact;
                state.EffectiveCorrection =
                    CharacterFootConstraintMath.RaiseToMinimum(
                        state.EffectiveCorrection,
                        target.Correction,
                        frame.ComponentUp);
                state.Residual =
                    state.EffectiveCorrection - target.Correction;
                state.PreviousTargetCorrection = target.Correction;
                state.Progress = 0f;
                state.StartResidual = 0f;
                state.Completed = false;
                state.Policy = CharacterFootInterpolationPolicy.AcquireByWeight;
                return Result(in state, false, in continuityFact);
            }
            state.Progress = Mathf.Max(state.Progress, target.Progress);
            state.PreviousTargetCorrection = target.Correction;
            state.EffectiveCorrection = target.Correction +
                                        state.Residual *
                                        (1f - state.Progress);
            state.Completed = state.Progress >=
                              1f - CharacterFootConstraintMath.GeometryEpsilon;
            if (state.Completed)
            {
                state.EffectiveCorrection = target.Correction;
                state.Residual = default;
                state.Progress = 1f;
            }
            return Result(in state, state.Completed, in continuityFact);
        }

        static CharacterFootInterpolationResult EvaluateRelease(
            ref CharacterFootInterpolationState state,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
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
            Vector3 residualBeforeRevision = state.Residual;
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            CharacterFootSwingPathReference swingPath =
                swing.SwingPathReference;
            bool hasPath = swing.Accepted &&
                           swingPath.IsAvailable &&
                           swingPath.LandingEventIdentity ==
                           swing.LandingEventIdentity;
            bool comparablePath = pathAvailableBefore && hasPath;
            float landingPointDelta = comparablePath
                ? Vector3.Distance(
                    previousLandingPoint,
                    swingPath.LandingPoint)
                : 0f;
            float targetDelta = comparablePath
                ? Vector3.Distance(
                    previousTargetCorrection,
                    target.SwingCorrection)
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
                state.Residual =
                    state.EffectiveCorrection - target.Correction;
            }
            Vector3 residualBeforeDecay = state.Residual;
            state.HasSwingPath = hasPath;
            state.SwingLandingEventIdentity = hasPath
                ? swing.LandingEventIdentity
                : 0;
            state.SwingLandingPoint = hasPath
                ? swingPath.LandingPoint
                : default;
            state.PreviousTargetCorrection = hasPath
                ? target.Correction
                : default;
            state.PreviousSwingTargetCorrection = hasPath
                ? target.SwingCorrection
                : default;
            float halfLifeSeconds = ResolveSwingResidualHalfLife(
                state.Residual,
                target.TimeToLandingSeconds,
                frame.Settings,
                out bool deadlineHalfLifeAvailable,
                out float deadlineHalfLifeSeconds);
            state.Residual = Advance(
                state.Residual,
                default,
                frame.DeltaSeconds,
                halfLifeSeconds);
            state.EffectiveCorrection = target.Correction + state.Residual;
            state.Residual =
                state.EffectiveCorrection - target.Correction;
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
                hasPath ? target.SwingCorrection : default,
                landingPointDelta,
                targetDelta,
                residualBeforeRevision,
                residualBeforeDecay,
                state.Residual,
                frame.Settings.LandingAcceptanceDistance,
                frame.Settings.PathRevisionDistance,
                frame.Settings.SwingResidualTolerance,
                target.TimeToLandingSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds,
                deadlineHalfLifeAvailable,
                deadlineHalfLifeSeconds,
                halfLifeSeconds);
            return Result(in state, false, in continuityFact);
        }

        static CharacterFootInterpolationResult Result(
            in CharacterFootInterpolationState state,
            bool completed,
            in CharacterFootPathContinuityFact continuityFact) =>
            new CharacterFootInterpolationResult(
                state.EffectiveCorrection,
                completed,
                in continuityFact);

        static CharacterFootPathContinuityFact Unevaluated(
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame) =>
            CharacterFootPathContinuityFact.CreateUnevaluated(
                target.TimeToLandingSeconds,
                frame.Settings);

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
    }
}
