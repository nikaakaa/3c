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
                    false,
                    default,
                    transition.StateChanged,
                    false,
                    false,
                    true,
                    timeToLandingSeconds,
                    in supportIntent);
            }
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    return ResolveSwingTarget(
                        in transition,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.SwingResidual,
                        timeToLandingSeconds,
                        in frame,
                        in supportIntent);
                case CharacterFootConstraintState.Landing:
                    return ResolveContactPlant(
                        in context,
                        in transition,
                        swingCorrection,
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
                    return ResolveSwingTarget(
                        in transition,
                        swingCorrection,
                        CharacterFootInterpolationPolicy.ReleaseResidual,
                        timeToLandingSeconds,
                        in frame,
                        in supportIntent);
                default:
                    throw new System.InvalidOperationException(
                        "Foot state target is invalid.");
            }
        }

        static CharacterFootStateTarget ResolveSwingTarget(
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            CharacterFootInterpolationPolicy policy,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            Vector3 correction = swingCorrection;
            if (!frame.SwingMotion.Accepted && frame.CurrentSupport.Available)
                correction = frame.CurrentSupport.Target.Position - originalSole;
            bool targetAvailable = TryResolveSupportTarget(
                in frame,
                originalSole + correction,
                out CharacterFootSupportTarget supportTarget);
            return Target(
                correction,
                swingCorrection,
                policy,
                in transition,
                targetAvailable,
                in supportTarget,
                timeToLandingSeconds,
                in supportIntent);
        }

        static CharacterFootStateTarget ResolveContactPlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
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
                new CharacterFootSupportTarget(
                    frame.FrameSequence,
                    frame.CompletionIdentity,
                    frame.Side,
                    context.Contact.Anchor,
                    context.Contact.Normal,
                    context.Contact.SurfaceIdentity,
                    frame.WorldRevision),
                transition,
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
                new CharacterFootSupportTarget(
                    frame.FrameSequence,
                    frame.CompletionIdentity,
                    frame.Side,
                    originalSole + correction,
                    context.Contact.Normal,
                    context.Contact.SurfaceIdentity,
                    frame.WorldRevision),
                transition,
                timeToLandingSeconds,
                true,
                in supportIntent);
        }

        static CharacterFootStateTarget Target(
            Vector3 correction,
            Vector3 swingCorrection,
            CharacterFootInterpolationPolicy policy,
            in CharacterFootTransitionDecision transition,
            bool supportTargetAvailable,
            in CharacterFootSupportTarget supportTarget,
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
                supportTargetAvailable,
                in supportTarget,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                false,
                false,
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
            CharacterFootSupportTarget supportTarget,
            in CharacterFootTransitionDecision transition,
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
                true,
                in supportTarget,
                transition.StateChanged,
                transition.Reason ==
                CharacterFootTransitionReason.LockResponseChanged,
                directPlantFollow,
                false,
                timeToLandingSeconds,
                in supportIntent);

        static bool TryResolveSupportTarget(
            in CharacterFootStateFrame frame,
            Vector3 position,
            out CharacterFootSupportTarget target)
        {
            if (!frame.CurrentSupport.Available)
            {
                target = default;
                return false;
            }
            CharacterFootSupportTarget current = frame.CurrentSupport.Target;
            target = new CharacterFootSupportTarget(
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.Side,
                position,
                current.SupportNormal,
                current.SurfaceIdentity,
                frame.WorldRevision);
            return true;
        }

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
