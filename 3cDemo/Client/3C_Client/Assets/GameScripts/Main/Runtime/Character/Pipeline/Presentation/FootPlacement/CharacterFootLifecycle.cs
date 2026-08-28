using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootLifecycle
    {
        internal static void ApplyLandingReach(
            ref CharacterFootLifecycleContext context,
            ulong landingEventIdentity,
            bool unavailable)
        {
            if (landingEventIdentity == 0)
                throw new System.ArgumentOutOfRangeException(nameof(landingEventIdentity));
            context.Discrete.LandingReachEventIdentity = landingEventIdentity;
            context.Discrete.LandingReachUnavailable = unavailable;
            if (unavailable &&
                context.Contact.EventIdentity == landingEventIdentity &&
                context.Discrete.LockResponse == CharacterFootLockResponse.FullAnchor)
            {
                context.Discrete.LockResponse = CharacterFootLockResponse.Sliding;
            }
        }

        internal static CharacterResolvedFootResult Evaluate(
            ref CharacterFootLifecycleContext context,
            in CharacterFootStateEvaluation evaluation,
            out CharacterFootSwingMotionResult result)
        {
            CharacterFootStateFrame frame = evaluation.Frame;
            var currentStep = evaluation.CurrentStep;
            var selectedStep = evaluation.SelectedStep;
            var landingPrediction = evaluation.LandingPrediction;
            ulong reachEventIdentity =
                context.Discrete.LandingReachEventIdentity;
            if (reachEventIdentity != 0 &&
                currentStep.LandingEventIdentity != reachEventIdentity &&
                selectedStep.LandingEventIdentity != reachEventIdentity)
            {
                context.Discrete.LandingReachEventIdentity = 0;
                context.Discrete.LandingReachUnavailable = false;
            }
            CharacterFootMotionSettings settings = frame.Settings;
            CharacterFootLandingRuntime.Evaluate(
                ref context.Landing,
                in currentStep,
                in selectedStep,
                in landingPrediction,
                in settings);
            return Resolve(
                ref context,
                evaluation.Side,
                selectedStep.IsValid
                    ? selectedStep.TimeToLandingSeconds
                    : 0f,
                in frame,
                out result);
        }

        static CharacterResolvedFootResult Resolve(
            ref CharacterFootLifecycleContext context,
            CharacterFootSide side,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            out CharacterFootSwingMotionResult result)
        {
            RequireValid(in frame);
            CharacterFootConstraintState stateBefore = context.Discrete.State;
            CharacterFootLockResponse lockResponseBefore =
                context.Discrete.LockResponse;
            CharacterFootTransitionDecision preTransition =
                CharacterFootTransitionResolver.ResolvePreInterpolation(
                    in context,
                    in frame);
            CharacterFootTransitionRuntime.Apply(
                ref context,
                in preTransition,
                in frame);
            CharacterFootStateTarget target =
                CharacterFootStateTargetResolver.Resolve(
                    in context,
                    in preTransition,
                    timeToLandingSeconds,
                    in frame);
            CharacterFootInterpolationResult interpolation =
                CharacterFootInterpolationRuntime.Evaluate(
                    ref context.Interpolation,
                    in target,
                    in frame);
            CharacterFootTransitionDecision postTransition =
                CharacterFootTransitionResolver.ResolvePostInterpolation(
                    in context,
                    in frame,
                    interpolation.Completed);
            CharacterFootTransitionRuntime.Apply(
                ref context,
                in postTransition,
                in frame);
            CharacterFootInterpolationRuntime.ApplyPostTransition(
                ref context.Interpolation,
                in postTransition);

            CharacterFootSwingMotionResult frameSwing = frame.SwingMotion;
            CharacterFootSwingMotionResult outputSwing = preTransition.SuppressOutput
                ? CharacterFootSwingMotionBuilder.SuppressUnselected(
                    in frameSwing)
                : frameSwing;
            CharacterFootHardConstraintResult hardConstraint =
                preTransition.SuppressOutput
                    ? new CharacterFootHardConstraintResult(
                        false,
                        false,
                        CharacterFootSafetyFloorOwner.None,
                        0,
                        0,
                        default,
                        default,
                        default)
                    : CharacterFootHardConstraintResolver.Resolve(
                        in context,
                        in frame,
                        context.Interpolation.EffectiveCorrection);
            CharacterFootPathContinuityFact continuityFact =
                interpolation.ContinuityFact;
            if (!preTransition.SuppressOutput)
            {
                continuityFact = CompleteContinuity(
                    in continuityFact,
                    in preTransition,
                    in postTransition,
                    in target,
                    in interpolation,
                    stateBefore,
                    context.Discrete.State,
                    lockResponseBefore,
                    context.Discrete.LockResponse,
                    in hardConstraint,
                    frame.ComponentUp);
            }
            Vector3 desiredCorrection = ResolveDiagnosticDesiredCorrection(
                in context,
                in target,
                in frame);
            return BuildOutput(
                in context,
                side,
                in frame,
                in outputSwing,
                desiredCorrection,
                hardConstraint.OutputCorrection,
                in continuityFact,
                out result);
        }

        static Vector3 ResolveDiagnosticDesiredCorrection(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateTarget target,
            in CharacterFootStateFrame frame)
        {
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                case CharacterFootConstraintState.Releasing:
                    return CharacterFootConstraintMath.ResolveSwingCorrection(
                        frame.AnimatedFoot,
                        frame.SwingMotion);
                default:
                    return target.Correction;
            }
        }

        static CharacterFootPathContinuityFact CompleteContinuity(
            in CharacterFootPathContinuityFact fact,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget target,
            in CharacterFootInterpolationResult interpolation,
            CharacterFootConstraintState stateBefore,
            CharacterFootConstraintState stateAfter,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootLockResponse lockResponseAfter,
            in CharacterFootHardConstraintResult hardConstraint,
            Vector3 componentUp)
        {
            bool available = hardConstraint.Resolved && hardConstraint.Available;
            Vector3 up = componentUp.normalized;
            float clampMeters = available
                ? Mathf.Max(
                    0f,
                    Vector3.Dot(
                        hardConstraint.OutputCorrection -
                        hardConstraint.InputCorrection,
                        up))
                : 0f;
            float clearanceBefore = available
                ? Vector3.Dot(
                    hardConstraint.InputCorrection -
                    hardConstraint.MinimumCorrection,
                    up)
                : 0f;
            float clearanceAfter = available
                ? Vector3.Dot(
                    hardConstraint.OutputCorrection -
                    hardConstraint.MinimumCorrection,
                    up)
                : 0f;
            return fact.Complete(
                in preTransition,
                in postTransition,
                in target,
                in interpolation,
                stateBefore,
                stateAfter,
                lockResponseBefore,
                lockResponseAfter,
                available,
                hardConstraint.Resolved
                    ? hardConstraint.Owner
                    : CharacterFootSafetyFloorOwner.None,
                hardConstraint.Resolved ? hardConstraint.SurfaceIdentity : 0,
                hardConstraint.Resolved ? hardConstraint.PathIdentity : 0,
                hardConstraint.InputCorrection,
                available ? hardConstraint.MinimumCorrection : default,
                hardConstraint.OutputCorrection,
                hardConstraint.OutputCorrection,
                clampMeters > 0f,
                clampMeters,
                clearanceBefore,
                clearanceAfter);
        }

        static CharacterResolvedFootResult BuildOutput(
            in CharacterFootLifecycleContext context,
            CharacterFootSide side,
            in CharacterFootStateFrame frame,
            in CharacterFootSwingMotionResult swing,
            Vector3 desiredCorrection,
            Vector3 outputCorrection,
            in CharacterFootPathContinuityFact continuityFact,
            out CharacterFootSwingMotionResult result)
        {
            bool hasContact = context.Contact.HasContact;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            Vector3 originalAnkle = frame.AnimatedFoot.AnklePosition;
            float horizontalError = hasContact
                ? Vector3.ProjectOnPlane(
                    context.Contact.Anchor - originalSole,
                    frame.ComponentUp.normalized).magnitude
                : 0f;
            float contactOwnership = ResolveContactOwnership(in context);
            float supportIntent = frame.FormalMotion.Observation.Support;
            ulong reachEventIdentity = 0;
            Vector3 reachPoint = default;
            if (hasContact)
            {
                reachEventIdentity = context.Contact.EventIdentity;
                reachPoint = context.Contact.Anchor;
            }
            else if (frame.HasContactLanding)
            {
                reachEventIdentity = frame.ContactLanding.LandingEventIdentity;
                reachPoint = frame.ContactLanding.Point;
            }
            else if (swing.SwingPathReference.IsAvailable)
            {
                reachEventIdentity = swing.SwingPathReference.LandingEventIdentity;
                reachPoint = swing.SwingPathReference.LandingPoint;
            }
            var pelvisReachReference =
                supportIntent > CharacterFootConstraintMath.GeometryEpsilon &&
                reachEventIdentity != 0
                    ? new CharacterFootPelvisReachReference(
                        reachEventIdentity,
                        reachPoint)
                    : default;
            CharacterFootSupportEligibility supportEligibility =
                pelvisReachReference.IsAvailable
                    ? CharacterFootSupportEligibility.AcquireAndRetain
                    : CharacterFootSupportEligibility.None;
            float positionWeight = outputCorrection.sqrMagnitude >
                                   CharacterFootConstraintMath.GeometryEpsilon *
                                   CharacterFootConstraintMath.GeometryEpsilon
                ? frame.FootPlacementWeight * ResolveGoalWeight(
                    context.Discrete.State,
                    frame.FormalMotion.Observation.LockWeight,
                    contactOwnership)
                : 0f;
            CharacterFootSwingMotionState outputState = hasContact
                ? CharacterFootSwingMotionState.Accepted
                : swing.State;
            CharacterFootSwingMotionRejectReason rejectReason = hasContact
                ? CharacterFootSwingMotionRejectReason.None
                : swing.RejectReason;
            ulong landingEventIdentity = hasContact
                ? context.Contact.EventIdentity
                : swing.LandingEventIdentity;
            result = new CharacterFootSwingMotionResult(
                outputState,
                rejectReason,
                landingEventIdentity,
                swing.GroundPathInputIdentity,
                swing.SwingPathReference,
                originalSole,
                originalAnkle,
                swing.Distance,
                swing.Progress,
                swing.BaselineSample,
                swing.EnvelopeSample,
                Vector3.Dot(
                    outputCorrection,
                    frame.ComponentUp.normalized),
                swing.LandingPredictionError,
                originalSole + outputCorrection,
                originalAnkle + outputCorrection,
                positionWeight,
                0f,
                context.Discrete.State,
                context.Discrete.LockResponse,
                horizontalError,
                contactOwnership,
                supportIntent,
                pelvisReachReference.IsAvailable ? pelvisReachReference.Point : default,
                desiredCorrection,
                hasContact,
                hasContact ? context.Contact.SurfaceIdentity : 0,
                hasContact ? context.Contact.Normal : default,
                continuityFact);
            var contactReference = hasContact
                ? new CharacterFootContactReference(
                    context.Contact.EventIdentity,
                    context.Contact.Anchor)
                : default;
            return new CharacterResolvedFootResult(
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.RigId,
                frame.RigRevision,
                side,
                originalSole + outputCorrection,
                originalAnkle + outputCorrection,
                outputCorrection,
                positionWeight,
                in contactReference,
                contactOwnership,
                supportEligibility,
                supportIntent,
                supportIntent,
                horizontalError,
                pelvisReachReference.IsAvailable
                    ? pelvisReachReference.EventIdentity
                    : 0,
                in pelvisReachReference);
        }

        static float ResolveContactOwnership(
            in CharacterFootLifecycleContext context)
        {
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Landing:
                    return context.Interpolation.Progress;
                case CharacterFootConstraintState.Locked:
                    return 1f;
                case CharacterFootConstraintState.Releasing:
                    if (context.Interpolation.StartResidual <=
                        CharacterFootConstraintMath.GeometryEpsilon)
                    {
                        return 0f;
                    }
                    return Mathf.Clamp01(
                        context.Interpolation.Residual.magnitude /
                        context.Interpolation.StartResidual);
                default:
                    return 0f;
            }
        }

        static float ResolveGoalWeight(
            CharacterFootConstraintState state,
            float lockWeight,
            float contactOwnership) =>
            state switch
            {
                CharacterFootConstraintState.Landing => lockWeight,
                CharacterFootConstraintState.Locked => lockWeight,
                CharacterFootConstraintState.Releasing => contactOwnership,
                _ => 1f
            };

        static void RequireValid(in CharacterFootStateFrame frame)
        {
            if (frame.FrameSequence == 0 ||
                frame.CompletionIdentity == 0 ||
                frame.RigId.Length == 0 ||
                frame.RigRevision.Length == 0 ||
                !CharacterFootConstraintMath.Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <=
                CharacterFootConstraintMath.GeometryEpsilon ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f ||
                frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) ||
                frame.DeltaSeconds < 0f ||
                !frame.FormalMotion.IsValid ||
                frame.FormalMotion.Observation.LockMode ==
                    ThirdPersonCharacter.Pipeline.Animation
                        .AnimationFootStepObservationLockMode.Unlocked &&
                frame.FormalMotion.Observation.LockWeight >
                    CharacterFootConstraintMath.GeometryEpsilon ||
                frame.SwingMotion.Accepted !=
                frame.SwingMotion.SwingPathReference.IsAvailable ||
                frame.SwingMotion.Accepted &&
                frame.SwingMotion.SwingPathReference.LandingEventIdentity !=
                    frame.SwingMotion.LandingEventIdentity ||
                frame.HasContactLanding &&
                frame.ContactLanding.LandingEventIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Foot lifecycle frame is invalid.");
            }
        }
    }
}
