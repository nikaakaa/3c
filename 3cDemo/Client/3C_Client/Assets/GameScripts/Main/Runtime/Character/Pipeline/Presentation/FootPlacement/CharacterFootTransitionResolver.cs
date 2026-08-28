using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootTransitionResolver
    {
        const float WeightEpsilon = 0.0001f;

        internal static CharacterFootTransitionDecision ResolvePreInterpolation(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            CharacterFootConstraintState sourceState = discrete.State;
            if (frame.HardOwnershipLoss)
            {
                return Decision(
                    CharacterFootTransitionReason.OwnershipLost,
                    sourceState,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Release,
                    true,
                    true);
            }

            switch (sourceState)
            {
                case CharacterFootConstraintState.Swing:
                case CharacterFootConstraintState.UnlockedSupport:
                    return ResolveUnconstrained(in context, in frame);
                case CharacterFootConstraintState.Landing:
                    return ResolveLanding(in context, in frame);
                case CharacterFootConstraintState.Locked:
                    return ResolveLocked(in context, in frame);
                case CharacterFootConstraintState.Releasing:
                    return NoChange(in discrete);
                default:
                    throw new System.InvalidOperationException(
                        "Foot constraint state is invalid.");
            }
        }

        internal static CharacterFootTransitionDecision ResolvePostInterpolation(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            bool interpolationCompleted)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            if (!interpolationCompleted)
                return NoChange(in discrete, CharacterFootTransitionPhase.PostInterpolation);
            if (discrete.State == CharacterFootConstraintState.Landing)
            {
                CharacterFootLockResponse response = ResolveResponse(
                    frame.FormalMotion.Observation.LockMode,
                    frame.FormalMotion.ContactStep.LandingEventIdentity,
                    in discrete);
                return Decision(
                    CharacterFootTransitionReason.LandingCompleted,
                    discrete.State,
                    CharacterFootConstraintState.Locked,
                    response,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false,
                    CharacterFootTransitionPhase.PostInterpolation);
            }
            if (discrete.State == CharacterFootConstraintState.Releasing)
            {
                return Decision(
                    CharacterFootTransitionReason.ReleaseCompleted,
                    discrete.State,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Release,
                    false,
                    true,
                    CharacterFootTransitionPhase.PostInterpolation);
            }
            return NoChange(in discrete, CharacterFootTransitionPhase.PostInterpolation);
        }

        static CharacterFootTransitionDecision ResolveUnconstrained(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            AnimationFootStepObservationSample formal =
                frame.FormalMotion.Observation;
            if (!RequestsContact(in formal))
            {
                CharacterFootConstraintState target =
                    formal.Support > WeightEpsilon
                        ? CharacterFootConstraintState.UnlockedSupport
                        : CharacterFootConstraintState.Swing;
                return Decision(
                    CharacterFootTransitionReason.SwingStarted,
                    discrete.State,
                    target,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            if (!CanAcquire(in frame))
            {
                return Decision(
                    CharacterFootTransitionReason.ContactUnavailable,
                    discrete.State,
                    CharacterFootConstraintState.UnlockedSupport,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            return Decision(
                CharacterFootTransitionReason.ContactAcquired,
                discrete.State,
                CharacterFootConstraintState.Landing,
                CharacterFootLockResponse.None,
                CharacterFootAnchorCommand.Create,
                false,
                false);
        }

        static CharacterFootTransitionDecision ResolveLanding(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            AnimationFootStepObservationSample formal =
                frame.FormalMotion.Observation;
            float horizontalError = ResolveHorizontalError(in context, in frame);
            if (!RequestsContact(in formal) ||
                horizontalError > frame.Settings.SlideDistance)
            {
                return Decision(
                    horizontalError > frame.Settings.SlideDistance
                        ? CharacterFootTransitionReason.ContactOutOfSlideRange
                        : CharacterFootTransitionReason.ContactReleased,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            return NoChange(in discrete);
        }

        static CharacterFootTransitionDecision ResolveLocked(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            float horizontalError = ResolveHorizontalError(in context, in frame);
            AnimationFootStepObservationSample formal =
                frame.FormalMotion.Observation;
            if (!RequestsContact(in formal) ||
                horizontalError > frame.Settings.SlideDistance)
            {
                return Decision(
                    horizontalError > frame.Settings.SlideDistance
                        ? CharacterFootTransitionReason.ContactOutOfSlideRange
                        : CharacterFootTransitionReason.ContactReleased,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            CharacterFootLockResponse response = ResolveResponse(
                formal.LockMode,
                frame.FormalMotion.ContactStep.LandingEventIdentity,
                in discrete);
            return Decision(
                response != discrete.LockResponse
                    ? CharacterFootTransitionReason.LockResponseChanged
                    : CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                response,
                CharacterFootAnchorCommand.Retain,
                false,
                false);
        }

        static float ResolveHorizontalError(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            var correction = CharacterFootConstraintMath.ResolveContactCorrection(
                frame.AnimatedFoot,
                context.Contact.Anchor);
            return CharacterFootConstraintMath.ResolveHorizontalError(
                correction,
                frame.ComponentUp);
        }

        static bool RequestsContact(
            in AnimationFootStepObservationSample formal) =>
            formal.Contact > WeightEpsilon &&
            formal.LockMode != AnimationFootStepObservationLockMode.Unlocked;

        static CharacterFootLockResponse ResolveResponse(
            AnimationFootStepObservationLockMode mode,
            ulong contactEventIdentity,
            in CharacterFootDiscreteStateContext discrete) =>
            mode == AnimationFootStepObservationLockMode.Locked &&
            discrete.LandingReachUnavailable &&
            discrete.LandingReachEventIdentity == contactEventIdentity
                ? CharacterFootLockResponse.Sliding
                : mode switch
            {
                AnimationFootStepObservationLockMode.Locked =>
                    CharacterFootLockResponse.FullAnchor,
                AnimationFootStepObservationLockMode.Sliding =>
                    CharacterFootLockResponse.Sliding,
                _ => throw new System.InvalidOperationException(
                    "Formal Foot Lock Mode cannot own an Anchor.")
            };

        static CharacterFootTransitionDecision NoChange(
            in CharacterFootDiscreteStateContext discrete,
            CharacterFootTransitionPhase phase =
                CharacterFootTransitionPhase.PreInterpolation) =>
            Decision(
                CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                discrete.LockResponse,
                CharacterFootAnchorCommand.None,
                false,
                false,
                phase);

        static CharacterFootTransitionDecision Decision(
            CharacterFootTransitionReason reason,
            CharacterFootConstraintState source,
            CharacterFootConstraintState target,
            CharacterFootLockResponse targetResponse,
            CharacterFootAnchorCommand anchorCommand,
            bool suppressOutput,
            bool resetInterpolation,
            CharacterFootTransitionPhase phase =
                CharacterFootTransitionPhase.PreInterpolation) =>
            new CharacterFootTransitionDecision(
                phase,
                reason,
                source,
                target,
                targetResponse,
                anchorCommand,
                suppressOutput,
                resetInterpolation);

        static bool CanAcquire(in CharacterFootStateFrame frame) =>
            frame.HasContactLanding &&
            frame.ContactLanding.LandingEventIdentity != 0 &&
            frame.FormalMotion.ContactStep.IsValid &&
            frame.ContactLanding.LandingEventIdentity ==
            frame.FormalMotion.ContactStep.LandingEventIdentity;
    }
}
