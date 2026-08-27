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
            Vector3 swingTarget = ResolveSwingTarget(
                swingCorrection,
                in frame);
            if (transition.SuppressOutput)
            {
                return new CharacterFootStateTarget(
                    default,
                    swingTarget,
                    swingCorrection,
                    CharacterFootInterpolationPolicy.Suppressed,
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
                    return Target(
                        swingTarget,
                        swingTarget,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.SwingResidual,
                        transition,
                        0f,
                        timeToLandingSeconds);
                case CharacterFootConstraintState.Landing:
                    return Target(
                        CharacterFootConstraintMath.ResolveContactCorrection(
                            frame.AnimatedFoot,
                            context.Contact.Anchor),
                        swingTarget,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.AcquireByWeight,
                        transition,
                        ResolvePlantOwnership(
                            frame.SwingMotion.PlantConfidence),
                        timeToLandingSeconds);
                case CharacterFootConstraintState.Locked:
                    return ResolveLockedTarget(
                        in context,
                        in transition,
                        swingTarget,
                        swingCorrection,
                        timeToLandingSeconds,
                        in frame);
                case CharacterFootConstraintState.Releasing:
                    return Target(
                        swingTarget,
                        swingTarget,
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

        static CharacterFootStateTarget ResolveLockedTarget(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            Vector3 rawSwingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame)
        {
            Vector3 fullCorrection =
                CharacterFootConstraintMath.ResolveContactCorrection(
                    frame.AnimatedFoot,
                    context.Contact.Anchor);
            if (context.Discrete.LockResponse ==
                CharacterFootLockResponse.FullAnchor)
            {
                return Target(
                    fullCorrection,
                    swingCorrection,
                    rawSwingCorrection,
                    CharacterFootInterpolationPolicy.Direct,
                    transition,
                    1f,
                    timeToLandingSeconds);
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
                rawSwingCorrection,
                CharacterFootInterpolationPolicy.HalfLife,
                transition,
                1f,
                timeToLandingSeconds);
        }

        static Vector3 ResolveSwingTarget(
            Vector3 swingCorrection,
            in CharacterFootStateFrame frame)
        {
            if (!frame.CurrentGroundFloor.Accepted)
                return swingCorrection;
            Vector3 minimum =
                CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                    frame.AnimatedFoot,
                    frame.CurrentGroundFloor.Point,
                    frame.ComponentUp);
            return CharacterFootConstraintMath.RaiseToMinimum(
                swingCorrection,
                minimum,
                frame.ComponentUp);
        }

        static CharacterFootStateTarget Target(
            Vector3 correction,
            Vector3 swingCorrection,
            Vector3 rawSwingCorrection,
            CharacterFootInterpolationPolicy policy,
            in CharacterFootTransitionDecision transition,
            float progress,
            float timeToLandingSeconds) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                rawSwingCorrection,
                policy,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                progress,
                timeToLandingSeconds);

        static float ResolvePlantOwnership(float plantConfidence) =>
            Mathf.InverseLerp(
                AnimationFootConstraintFacts.GroundedMinimumConfidence,
                AnimationFootConstraintFacts.LockedMinimumConfidence,
                plantConfidence);
    }
}
