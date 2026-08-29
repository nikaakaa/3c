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
                    transition.StateChanged,
                    false,
                    true,
                    0f,
                    timeToLandingSeconds);
            }
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    return frame.ApproachPlantActive
                        ? ResolveApproachPlant(
                            in transition,
                            swingCorrection,
                            timeToLandingSeconds,
                            in frame)
                        : Target(
                            swingCorrection,
                            swingCorrection,
                            CharacterFootInterpolationPolicy.SwingResidual,
                            transition,
                            0f,
                            timeToLandingSeconds);
                case CharacterFootConstraintState.Landing:
                    return ResolveContactPlant(
                        in context,
                        in transition,
                        swingCorrection,
                        Mathf.Max(
                            frame.LockRequest.Contact,
                            frame.LockRequest.Weight),
                        timeToLandingSeconds,
                        in frame);
                case CharacterFootConstraintState.Locked:
                    return ResolveLockedPlant(
                        in context,
                        in transition,
                        swingCorrection,
                        timeToLandingSeconds,
                        in frame);
                case CharacterFootConstraintState.Releasing:
                    return Target(
                        swingCorrection,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.ReleaseResidual,
                        transition,
                        0f,
                        timeToLandingSeconds);
                default:
                    throw new System.InvalidOperationException(
                        "Foot state target is invalid.");
            }
        }

        static CharacterFootStateTarget ResolveApproachPlant(
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
        {
            CharacterFootGroundPathLanding plant = frame.ApproachPlantTarget;
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
                transition,
                frame.LockRequest.Contact,
                timeToLandingSeconds);
        }

        static CharacterFootStateTarget ResolveContactPlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float progress,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
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
                transition,
                progress,
                timeToLandingSeconds);
        }

        static CharacterFootStateTarget ResolveLockedPlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
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
                transition,
                1f,
                timeToLandingSeconds);
        }

        static CharacterFootStateTarget Target(
            Vector3 correction,
            Vector3 swingCorrection,
            CharacterFootInterpolationPolicy policy,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                policy,
                false,
                0,
                false,
                default,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                progress,
                timeToLandingSeconds);

        static CharacterFootStateTarget PlantTarget(
            Vector3 correction,
            Vector3 swingCorrection,
            ulong eventIdentity,
            bool verified,
            Vector3 point,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                CharacterFootInterpolationPolicy.PlantBlend,
                true,
                eventIdentity,
                verified,
                point,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                Mathf.Clamp01(progress),
                timeToLandingSeconds);
    }
}
