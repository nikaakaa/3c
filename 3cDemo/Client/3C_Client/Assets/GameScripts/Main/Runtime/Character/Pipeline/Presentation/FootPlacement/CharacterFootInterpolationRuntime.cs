using ThirdPersonCharacter.Pipeline.Animation;
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
                    target.SwingRotation,
                    0f,
                    false,
                    default);
            }
            if (!state.HasOutput)
            {
                state.HasOutput = true;
                state.EffectiveCorrection = target.SwingCorrection;
                state.EffectiveRotation = target.SwingRotation;
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
                    state.EffectiveRotation = target.Rotation;
                    state.RotationProgress = target.Progress;
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
                        state.EffectiveRotation = Advance(
                            state.EffectiveRotation,
                            target.Rotation,
                            frame.DeltaSeconds,
                            frame.Settings.EffectiveCorrectionHalfLifeSeconds);
                    }
                    state.PreviousTargetCorrection = target.Correction;
                    state.Residual =
                        state.EffectiveCorrection - target.Correction;
                    state.Progress = 1f;
                    state.StartResidual = 0f;
                    state.Completed = false;
                    state.RotationProgress = target.Progress;
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
            Quaternion rotation = state.EffectiveRotation;
            float rotationProgress = state.RotationProgress;
            state = default;
            state.HasOutput = true;
            state.EffectiveCorrection = correction;
            state.EffectiveRotation = rotation;
            state.RotationProgress = rotationProgress;
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
                        target.TimeToLandingSeconds,
                        target.SwingRotation,
                        target.SwingRotation),
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
                state.EffectiveRotation = target.SwingRotation;
                state.RotationProgress = 0f;
                state.Policy = CharacterFootInterpolationPolicy.AcquireByWeight;
                return Result(in state, false, in continuityFact);
            }
            state.Progress = Mathf.Max(state.Progress, target.Progress);
            state.PreviousTargetCorrection = target.Correction;
            state.EffectiveCorrection = target.Correction +
                                        state.Residual *
                                        (1f - state.Progress);
            state.EffectiveRotation = Quaternion.SlerpUnclamped(
                target.SwingRotation,
                target.Rotation,
                state.Progress);
            state.RotationProgress = state.Progress;
            state.Completed = state.Progress >=
                              1f - CharacterFootConstraintMath.GeometryEpsilon;
            if (state.Completed)
            {
                state.EffectiveCorrection = target.Correction;
                state.Residual = default;
                state.Progress = 1f;
                state.EffectiveRotation = target.Rotation;
                state.RotationProgress = target.Progress;
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
                state.RotationReleaseStartAngle = Quaternion.Angle(
                    state.EffectiveRotation,
                    target.SwingRotation);
                state.RotationResidual = (
                    Quaternion.Inverse(target.SwingRotation) *
                    state.EffectiveRotation).normalized;
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
                state.RotationResidual = Advance(
                    state.RotationResidual,
                    Quaternion.identity,
                    frame.DeltaSeconds,
                    frame.Settings.EffectiveCorrectionHalfLifeSeconds);
                state.EffectiveRotation = (
                    target.SwingRotation * state.RotationResidual).normalized;
            }
            float rotationResidual = Quaternion.Angle(
                state.RotationResidual,
                Quaternion.identity);
            state.RotationProgress = state.RotationReleaseStartAngle > 0.5f
                ? Mathf.Clamp01(
                    rotationResidual / state.RotationReleaseStartAngle)
                : 0f;
            state.Completed = frame.FormalMotion.Observation.LockWeight <=
                              CharacterFootConstraintMath.GeometryEpsilon &&
                              Vector3.Distance(
                                  state.EffectiveCorrection,
                                  target.SwingCorrection) <=
                              frame.Settings.ReleaseCompletionDistance &&
                              rotationResidual <= 0.5f;
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
                landingPointDelta > frame.Settings.SwingRevisionDistance)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.LandingPointChanged;
            }
            if (comparablePath &&
                targetDelta > frame.Settings.SwingRevisionDistance)
            {
                revisionReason |=
                    CharacterFootPathRevisionReason.SwingTargetChanged;
            }
            bool revised = revisionReason !=
                           CharacterFootPathRevisionReason.None;
            if (revised)
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
            state.EffectiveRotation = target.SwingRotation;
            state.RotationProgress = 0f;
            var continuityFact = new CharacterFootPathContinuityFact(
                true,
                revisionReason,
                revised,
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
                frame.Settings.SwingRevisionDistance,
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
                state.EffectiveRotation,
                state.RotationProgress,
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

        static Quaternion Advance(
            Quaternion current,
            Quaternion target,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return current;
            float alpha = 1f -
                          Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            return Quaternion.SlerpUnclamped(current, target, alpha).normalized;
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
                residualDistance <= settings.ResidualLandingTolerance ||
                !float.IsFinite(timeToLandingSeconds) ||
                timeToLandingSeconds <= 0f)
            {
                return halfLifeSeconds;
            }
            float halfLifeCount = Mathf.Log(
                residualDistance / settings.ResidualLandingTolerance,
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
