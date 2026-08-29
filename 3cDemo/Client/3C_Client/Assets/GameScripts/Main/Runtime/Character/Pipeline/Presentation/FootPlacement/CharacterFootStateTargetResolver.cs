using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootStateTargetResolver
    {
        internal static CharacterFootStateTarget Resolve(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
        {
            Vector3 swingCorrection =
                CharacterFootConstraintMath.ResolveSwingCorrection(
                    frame.AnimatedFoot,
                    frame.SwingMotion);
            CharacterFootSupportIntent supportIntent =
                ResolveSupportIntent(in frame);
            if (transition.SuppressOutput)
            {
                return new CharacterFootStateTarget(
                    default,
                    swingCorrection,
                    CharacterFootInterpolationPolicy.Suppressed,
                    false,
                    0,
                    false,
                    default,
                    CharacterFootPlantTargetKind.None,
                    CharacterFootLockResponse.None,
                    transition.StateChanged,
                    false,
                    false,
                    true,
                    0f,
                    timeToLandingSeconds,
                    in supportIntent);
            }
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    return frame.PreparedPlantActive
                        ? ResolvePreparedPlant(
                            in transition,
                            swingCorrection,
                            timeToLandingSeconds,
                            in frame,
                            in supportIntent)
                        : Target(
                            swingCorrection,
                            swingCorrection,
                            CharacterFootInterpolationPolicy.SwingResidual,
                            transition,
                            0f,
                            timeToLandingSeconds,
                            in supportIntent);
                case CharacterFootConstraintState.Landing:
                    return ResolveContactPlant(
                        in context,
                        in transition,
                        swingCorrection,
                        frame.LockRequest.Weight,
                        timeToLandingSeconds,
                        in frame,
                        in supportIntent);
                case CharacterFootConstraintState.Locked:
                    return ResolveLockedPlant(
                        in context,
                        in transition,
                        swingCorrection,
                        timeToLandingSeconds,
                        in frame,
                        in supportIntent);
                case CharacterFootConstraintState.Releasing:
                    return Target(
                        swingCorrection,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.ReleaseResidual,
                        transition,
                        0f,
                        timeToLandingSeconds,
                        in supportIntent);
                default:
                    throw new System.InvalidOperationException(
                        "Foot state target is invalid.");
            }
        }

        static CharacterFootStateTarget ResolvePreparedPlant(
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            CharacterFootGroundPathLanding plant = frame.PreparedPlantTarget;
            Vector3 correction =
                CharacterFootConstraintMath.ResolveContactCorrection(
                    frame.AnimatedFoot,
                    plant.Point);
            return PlantTarget(
                correction,
                swingCorrection,
                plant.LandingEventIdentity,
                false,
                plant.Point,
                CharacterFootPlantTargetKind.PreparedPrediction,
                CharacterFootLockResponse.None,
                transition,
                frame.PreparedPlantWeight,
                timeToLandingSeconds,
                false,
                in supportIntent);
        }

        static CharacterFootStateTarget ResolveContactPlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float progress,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            Vector3 correction =
                CharacterFootConstraintMath.ResolveContactCorrection(
                    frame.AnimatedFoot,
                    context.Contact.Anchor);
            return PlantTarget(
                correction,
                swingCorrection,
                context.Contact.EventIdentity,
                true,
                context.Contact.Anchor,
                CharacterFootPlantTargetKind.VerifiedAnchor,
                CharacterFootLockResponse.None,
                transition,
                progress,
                timeToLandingSeconds,
                false,
                in supportIntent);
        }

        static CharacterFootStateTarget ResolveLockedPlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            Vector3 fullCorrection =
                CharacterFootConstraintMath.ResolveContactCorrection(
                    frame.AnimatedFoot,
                    context.Contact.Anchor);
            Vector3 correction;
            if (context.Discrete.LockResponse ==
                CharacterFootLockResponse.FullAnchor)
            {
                correction = fullCorrection;
            }
            else if (context.Discrete.LockResponse ==
                     CharacterFootLockResponse.Sliding)
            {
                float horizontalError =
                    CharacterFootConstraintMath.ResolveHorizontalError(
                        fullCorrection,
                        frame.ComponentUp);
                correction =
                    CharacterFootConstraintMath.ResolveSlidingCorrection(
                        fullCorrection,
                        frame.ComponentUp,
                        horizontalError,
                        frame.Settings);
            }
            else
            {
                throw new System.InvalidOperationException(
                    "Locked Foot response is invalid.");
            }
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            return PlantTarget(
                correction,
                swingCorrection,
                context.Contact.EventIdentity,
                true,
                originalSole + correction,
                context.Discrete.LockResponse ==
                CharacterFootLockResponse.FullAnchor
                    ? CharacterFootPlantTargetKind.LockedFullAnchor
                    : CharacterFootPlantTargetKind.LockedSliding,
                context.Discrete.LockResponse,
                transition,
                1f,
                timeToLandingSeconds,
                true,
                in supportIntent);
        }

        static CharacterFootStateTarget Target(
            Vector3 correction,
            Vector3 swingCorrection,
            CharacterFootInterpolationPolicy policy,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds,
            in CharacterFootSupportIntent supportIntent) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                policy,
                false,
                0,
                false,
                default,
                CharacterFootPlantTargetKind.None,
                CharacterFootLockResponse.None,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                false,
                progress,
                timeToLandingSeconds,
                in supportIntent);

        static CharacterFootStateTarget PlantTarget(
            Vector3 correction,
            Vector3 swingCorrection,
            ulong eventIdentity,
            bool verified,
            Vector3 point,
            CharacterFootPlantTargetKind targetKind,
            CharacterFootLockResponse lockResponse,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds,
            bool directPlantFollow,
            in CharacterFootSupportIntent supportIntent) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                CharacterFootInterpolationPolicy.PlantBlend,
                true,
                eventIdentity,
                verified,
                point,
                targetKind,
                lockResponse,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                directPlantFollow,
                false,
                Mathf.Clamp01(progress),
                timeToLandingSeconds,
                in supportIntent);

        static CharacterFootSupportIntent ResolveSupportIntent(
            in CharacterFootStateFrame frame)
        {
            bool available = frame.FormalSupportEventIdentity != 0;
            return new CharacterFootSupportIntent(
                available,
                available ? frame.FormalSupportEventIdentity : 0,
                available ? frame.FormalSupport : 0f);
        }
    }
}
