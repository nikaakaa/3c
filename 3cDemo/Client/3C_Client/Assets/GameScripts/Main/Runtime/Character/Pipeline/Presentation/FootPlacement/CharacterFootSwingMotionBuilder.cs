using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootSwingMotionBuilder
    {
        const float GeometryEpsilon = 0.0001f;
        const float EndpointTolerance = 0.005f;

        internal static CharacterFootSwingMotionResult Build(
            in CharacterFootPlacementAnimatedFootPose animatedFoot,
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
            in CharacterFootPlacementAnimatedFootPose animatedFoot,
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
            if (!TrySampleEnvelope(
                    groundPath,
                    progress,
                    up,
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
            float envelopeHeightAlongUp = Vector3.Dot(envelopeSample, up);
            float envelopeMinimumCorrection =
                envelopeHeightAlongUp - originalSoleHeight;
            float formalTargetHeightAlongUp =
                envelopeHeightAlongUp + formalFootHeight;
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
                in motion.SwingPathReference,
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
                in motion.PathContinuity,
                evaluated,
                available,
                in motion.LifecycleTransition);

        internal static CharacterFootSwingMotionResult WithPathContinuity(
            in CharacterFootSwingMotionResult motion,
            in CharacterFootPathContinuityFact continuity) =>
            new CharacterFootSwingMotionResult(
                motion.State,
                motion.RejectReason,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                in motion.SwingPathReference,
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
                in continuity,
                motion.LandingReachEvaluated,
                motion.LandingReachAvailable,
                in motion.LifecycleTransition);

        internal static CharacterFootSwingMotionResult WithLifecycleTransition(
            in CharacterFootSwingMotionResult motion,
            in CharacterFootLifecycleTransitionFact lifecycleTransition) =>
            new CharacterFootSwingMotionResult(
                motion.State,
                motion.RejectReason,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                in motion.SwingPathReference,
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
                in motion.PathContinuity,
                motion.LandingReachEvaluated,
                motion.LandingReachAvailable,
                in lifecycleTransition);

        static bool TrySampleEnvelope(
            in CharacterFootGroundPathResult groundPath,
            float progress,
            Vector3 up,
            out Vector3 sample,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            if (progress >= 1f)
            {
                sample = groundPath.EnvelopeVertexAt(groundPath.EnvelopeVertexCount - 1).Position;
                rejectReason = CharacterFootSwingMotionRejectReason.None;
                return true;
            }
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
