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
            if (context.Discrete.State == CharacterFootConstraintState.Releasing)
            {
                swingCorrection = CharacterFootConstraintMath.RaiseToMinimum(
                    swingCorrection, default, frame.ComponentUp);
            }
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
                case CharacterFootConstraintState.Locked:
                    return ResolvePlant(
                        in context,
                        in transition,
                        swingCorrection,
                        timeToLandingSeconds,
                        in frame,
                        in supportIntent);
                case CharacterFootConstraintState.Releasing:
                    return ResolveReleaseTarget(
                        in context,
                        in transition,
                        swingCorrection,
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
            bool currentContactOwnsTarget =
                frame.PreparedPlantActive &&
                frame.PreparedPlantTarget.LandingEventIdentity != 0 &&
                frame.SwingMotion.Accepted &&
                frame.SwingMotion.LandingEventIdentity !=
                frame.PreparedPlantTarget.LandingEventIdentity;
            if ((!frame.SwingMotion.Accepted || currentContactOwnsTarget) &&
                frame.CurrentSupport.Available)
            {
                Vector3 supportCorrection =
                    frame.CurrentSupport.Target.Position - originalSole;
                correction = frame.LockRequest.Contact > 0f
                    ? supportCorrection
                    : CharacterFootConstraintMath.RaiseToMinimum(
                        swingCorrection,
                        supportCorrection,
                        frame.ComponentUp);
            }
            bool targetAvailable = TryResolveSupportTarget(
                in frame,
                originalSole + correction,
                currentContactOwnsTarget,
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

        static CharacterFootStateTarget ResolveReleaseTarget(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            bool targetAvailable = context.Contact.HasContact;
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(
                    frame.AnimatedFoot);
            CharacterFootSupportTarget target = targetAvailable
                ? new CharacterFootSupportTarget(
                    frame.FrameSequence,
                    frame.CompletionIdentity,
                    frame.Side,
                    originalSole + swingCorrection,
                    context.Contact.Normal,
                    context.Contact.SurfaceIdentity,
                    context.Contact.WorldRevision,
                    CharacterFootSupportTargetKind.Releasing,
                    CharacterFootSupportPositionSource.ReleasingSwing,
                    frame.FrameSequence,
                    frame.CompletionIdentity,
                    frame.SwingMotion.Accepted
                        ? frame.SwingMotion.LandingEventIdentity
                        : 0,
                    frame.SwingMotion.Accepted
                        ? frame.SwingMotion.GroundPathInputIdentity
                        : 0,
                    CharacterFootSupportNormalSource.RetainedContactAnchor,
                    context.Contact.AcquiredFrameSequence,
                    context.Contact.AcquiredCompletionIdentity,
                    context.Contact.EventIdentity)
                : default;
            return Target(
                swingCorrection,
                swingCorrection,
                CharacterFootInterpolationPolicy.ReleaseResidual,
                in transition,
                targetAvailable,
                in target,
                timeToLandingSeconds,
                in supportIntent);
        }

        internal static CharacterFootStateTarget ConstrainStateTarget(
            in CharacterFootStateTarget target,
            in CharacterFootCurrentSupportObservation support,
            in CharacterFootStateFrame frame)
        {
            if (!support.TryResolveHeightConstraint(out float displacement, out _))
                return target;
            if (displacement <= 0f)
                return target;
            Vector3 adjustment = frame.ComponentUp.normalized *
                (displacement / frame.FootPlacementWeight);
            Vector3 correction = target.Correction + adjustment;
            CharacterFootSupportTarget supportTarget = target.SupportTarget.WithPosition(
                CharacterFootConstraintMath.ResolveOriginalSole(frame.AnimatedFoot) + correction);
            return new CharacterFootStateTarget(
                correction, target.SwingCorrection, target.InterpolationPolicy,
                target.PlantTargetAvailable, target.PlantTargetEventIdentity,
                target.PlantTargetVerified,
                target.PlantTargetAvailable ? target.PlantTargetPoint + adjustment : target.PlantTargetPoint,
                target.PlantTargetKind, target.PlantLockResponse, target.LockWeightCompleted,
                target.SupportTargetAvailable, in supportTarget, target.StateEntered,
                target.ResponseEntered, target.DirectPlantFollow, target.SuppressOutput,
                target.TimeToLandingSeconds, in target.SupportIntent);
        }

        static CharacterFootStateTarget ResolvePlant(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision transition,
            Vector3 swingCorrection,
            float timeToLandingSeconds,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportIntent supportIntent)
        {
            Vector3 originalSole =
                CharacterFootConstraintMath.ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 fullCorrection = context.Contact.Anchor - originalSole;
            bool landing = context.Discrete.State == CharacterFootConstraintState.Landing;
            CharacterFootLockResponse response = landing
                ? frame.LockRequest.Response
                : context.Discrete.LockResponse;
            Vector3 up = default;
            Vector3 correction;
            if (response ==
                CharacterFootLockResponse.FullAnchor)
            {
                correction = fullCorrection;
            }
            else if (response ==
                     CharacterFootLockResponse.Sliding)
            {
                up = frame.ComponentUp.normalized;
                float horizontalError =
                    CharacterFootConstraintMath.ResolveHorizontalError(
                        fullCorrection,
                        up);
                correction =
                    CharacterFootConstraintMath.ResolveSlidingCorrection(
                        fullCorrection,
                        up,
                        horizontalError,
                        in frame.Settings);
            }
            else
            {
                throw new System.InvalidOperationException(
                    "Contact Foot response is invalid.");
            }
            var supportTarget = new CharacterFootSupportTarget(
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.Side,
                originalSole + correction,
                context.Contact.Normal,
                context.Contact.SurfaceIdentity,
                context.Contact.WorldRevision,
                landing
                    ? CharacterFootSupportTargetKind.VerifiedAnchor
                    : response == CharacterFootLockResponse.FullAnchor
                        ? CharacterFootSupportTargetKind.LockedFullAnchor
                        : CharacterFootSupportTargetKind.LockedSliding,
                CharacterFootSupportPositionSource.ContactAnchor,
                context.Contact.AcquiredFrameSequence,
                context.Contact.AcquiredCompletionIdentity,
                context.Contact.EventIdentity,
                0,
                CharacterFootSupportNormalSource.ContactAnchor,
                context.Contact.AcquiredFrameSequence,
                context.Contact.AcquiredCompletionIdentity,
                context.Contact.EventIdentity);
            return PlantTarget(
                correction,
                swingCorrection,
                context.Contact.EventIdentity,
                true,
                originalSole + correction,
                landing
                    ? CharacterFootPlantTargetKind.VerifiedAnchor
                    : response == CharacterFootLockResponse.FullAnchor
                        ? CharacterFootPlantTargetKind.LockedFullAnchor
                        : CharacterFootPlantTargetKind.LockedSliding,
                response,
                context.ContactTransition.HasCompletedLockWeight(
                    context.Contact.EventIdentity),
                in supportTarget,
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
                false,
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
            bool lockWeightCompleted,
            in CharacterFootSupportTarget supportTarget,
            in CharacterFootTransitionDecision transition,
            float timeToLandingSeconds,
            bool directPlantFollow,
            in CharacterFootSupportIntent supportIntent) =>
            new CharacterFootStateTarget(
                correction,
                swingCorrection,
                CharacterFootInterpolationPolicy.VerifiedSupport,
                true,
                eventIdentity,
                verified,
                point,
                targetKind,
                lockResponse,
                lockWeightCompleted,
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
            bool currentContactOwnsTarget,
            out CharacterFootSupportTarget target)
        {
            if (!frame.SwingMotion.Accepted || currentContactOwnsTarget)
            {
                target = frame.CurrentSupport.Available
                    ? frame.CurrentSupport.Target
                    : default;
                return target.IsValid;
            }
            ref readonly CharacterFootGroundPathInput path =
                ref frame.GroundPath.Page.Input;
            target = new CharacterFootSupportTarget(
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.Side,
                position,
                path.NextSwingLandingNormal,
                path.NextSwingLandingSurfaceIdentity,
                frame.GroundPath.Page.Contacts.SurfaceCoverage.WorldRevision,
                CharacterFootSupportTargetKind.SwingGround,
                CharacterFootSupportPositionSource.SwingMotion,
                frame.FrameSequence,
                frame.CompletionIdentity,
                frame.SwingMotion.LandingEventIdentity,
                frame.SwingMotion.GroundPathInputIdentity,
                CharacterFootSupportNormalSource.PredictedLanding,
                frame.FrameSequence,
                frame.CompletionIdentity,
                path.Key.NextSwingLandingEventIdentity);
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
