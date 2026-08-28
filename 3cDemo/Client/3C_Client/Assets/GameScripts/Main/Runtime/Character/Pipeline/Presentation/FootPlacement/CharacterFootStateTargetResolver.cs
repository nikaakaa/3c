using ThirdPersonCharacter.Pipeline.Animation;
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
            Quaternion swingRotation = frame.AnimatedFoot.AnkleRotation;
            if (transition.SuppressOutput)
            {
                return new CharacterFootStateTarget(
                    default,
                    swingCorrection,
                    CharacterFootInterpolationPolicy.Suppressed,
                    transition.StateChanged,
                    false,
                    true,
                    0f,
                    timeToLandingSeconds,
                    swingRotation,
                    swingRotation);
            }
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    return Target(
                        swingCorrection,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.SwingResidual,
                        transition,
                        0f,
                        timeToLandingSeconds,
                        swingRotation,
                        swingRotation);
                case CharacterFootConstraintState.Landing:
                    Quaternion landingRotation =
                        CharacterFootConstraintMath.ResolveContactRotation(
                            frame.AnimatedFoot,
                            context.Contact.Normal,
                            frame.ComponentUp);
                    return Target(
                        CharacterFootConstraintMath.ResolveContactCorrection(
                            frame.AnimatedFoot,
                            context.Contact.Anchor),
                        swingCorrection,
                        CharacterFootInterpolationPolicy.AcquireByWeight,
                        transition,
                        frame.FormalMotion.Observation.LockWeight,
                        timeToLandingSeconds,
                        landingRotation,
                        swingRotation);
                case CharacterFootConstraintState.Locked:
                    return ResolveLockedTarget(
                        in context,
                        in transition,
                        swingCorrection,
                        swingRotation,
                        timeToLandingSeconds,
                        in frame);
                case CharacterFootConstraintState.Releasing:
                    return Target(
                        swingCorrection,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.ReleaseResidual,
                        transition,
                        0f,
                        timeToLandingSeconds,
                        swingRotation,
                        swingRotation);
                default:
                    throw new System.InvalidOperationException(
                        "Foot state target is invalid.");
            }
        }

        static CharacterFootStateTarget ResolveLockedTarget(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            Quaternion swingRotation,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
        {
            Vector3 fullCorrection =
                CharacterFootConstraintMath.ResolveContactCorrection(
                    frame.AnimatedFoot,
                    context.Contact.Anchor);
            Quaternion contactRotation =
                CharacterFootConstraintMath.ResolveContactRotation(
                    frame.AnimatedFoot,
                    context.Contact.Normal,
                    frame.ComponentUp);
            if (context.Discrete.LockResponse ==
                CharacterFootLockResponse.FullAnchor)
            {
                return Target(
                    fullCorrection,
                    swingCorrection,
                    CharacterFootInterpolationPolicy.Direct,
                    transition,
                    frame.FormalMotion.Observation.LockWeight,
                    timeToLandingSeconds,
                    contactRotation,
                    swingRotation);
            }
            if (context.Discrete.LockResponse !=
                CharacterFootLockResponse.Sliding)
            {
                throw new System.InvalidOperationException(
                    "Locked Foot response is invalid.");
            }
            float horizontalError =
                CharacterFootConstraintMath.ResolveHorizontalError(
                    fullCorrection,
                    frame.ComponentUp);
            Vector3 correction =
                CharacterFootConstraintMath.ResolveSlidingCorrection(
                    fullCorrection,
                    frame.ComponentUp,
                    horizontalError,
                    frame.Settings);
            return Target(
                correction,
                swingCorrection,
                CharacterFootInterpolationPolicy.HalfLife,
                transition,
                frame.FormalMotion.Observation.LockWeight,
                timeToLandingSeconds,
                contactRotation,
                swingRotation);
        }

        static CharacterFootStateTarget Target(
            Vector3 correction,
            Vector3 swingCorrection,
            CharacterFootInterpolationPolicy policy,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds,
            Quaternion rotation,
            Quaternion swingRotation) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                policy,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                progress,
                timeToLandingSeconds,
                rotation,
                swingRotation);

    }
}
