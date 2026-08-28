using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootTransitionResolver
    {
        internal static CharacterFootTransitionDecision ResolvePreInterpolation(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            CharacterFootConstraintState sourceState = discrete.State;
            if (frame.HardOwnershipLoss)
            {
                bool consumed = frame.SwingMotion.PlantConfidence >=
                                AnimationFootConstraintFacts
                                    .GroundedMinimumConfidence;
                return Decision(
                    CharacterFootTransitionReason.OwnershipLost,
                    sourceState,
                    consumed
                        ? CharacterFootConstraintState.UnlockedSupport
                        : CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    consumed,
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
            bool interpolationCompleted)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            if (!interpolationCompleted)
                return NoChange(in discrete, CharacterFootTransitionPhase.PostInterpolation);
            if (discrete.State == CharacterFootConstraintState.Landing)
            {
                return Decision(
                    CharacterFootTransitionReason.LandingCompleted,
                    discrete.State,
                    CharacterFootConstraintState.Locked,
                    CharacterFootLockResponse.FullAnchor,
                    discrete.PlantCycleConsumed,
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
                    false,
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
            float plantConfidence = frame.SwingMotion.PlantConfidence;
            if (plantConfidence <
                AnimationFootConstraintFacts.GroundedMinimumConfidence)
            {
                return Decision(
                    CharacterFootTransitionReason.SwingStarted,
                    discrete.State,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    false,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            if (discrete.PlantCycleConsumed)
            {
                return Decision(
                    CharacterFootTransitionReason.PlantCycleConsumed,
                    discrete.State,
                    CharacterFootConstraintState.UnlockedSupport,
                    CharacterFootLockResponse.None,
                    true,
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
                    true,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            var correction = CharacterFootConstraintMath.ResolveContactCorrection(
                frame.AnimatedFoot,
                frame.ContactLanding.Point);
            float horizontalError =
                CharacterFootConstraintMath.ResolveHorizontalError(
                    correction,
                    frame.ComponentUp);
            if (horizontalError > frame.Settings.LockDistance)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactOutOfLockRange,
                    discrete.State,
                    CharacterFootConstraintState.UnlockedSupport,
                    CharacterFootLockResponse.None,
                    true,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            return Decision(
                CharacterFootTransitionReason.ContactAcquired,
                discrete.State,
                CharacterFootConstraintState.Landing,
                CharacterFootLockResponse.None,
                true,
                CharacterFootAnchorCommand.Create,
                false,
                false);
        }

        static CharacterFootTransitionDecision ResolveLanding(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            var correction = CharacterFootConstraintMath.ResolveContactCorrection(
                frame.AnimatedFoot,
                context.Contact.Anchor);
            float horizontalError =
                CharacterFootConstraintMath.ResolveHorizontalError(
                    correction,
                    frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence >=
                    AnimationFootConstraintFacts.GroundedMinimumConfidence &&
                horizontalError <= frame.Settings.SlideDistance)
            {
                return NoChange(in discrete);
            }
            return Decision(
                horizontalError > frame.Settings.SlideDistance
                    ? CharacterFootTransitionReason.ContactOutOfSlideRange
                    : CharacterFootTransitionReason.ContactReleased,
                discrete.State,
                CharacterFootConstraintState.Releasing,
                CharacterFootLockResponse.None,
                discrete.PlantCycleConsumed,
                CharacterFootAnchorCommand.Retain,
                false,
                false);
        }

        static CharacterFootTransitionDecision ResolveLocked(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            var fullCorrection = CharacterFootConstraintMath.ResolveContactCorrection(
                frame.AnimatedFoot,
                context.Contact.Anchor);
            float horizontalError =
                CharacterFootConstraintMath.ResolveHorizontalError(
                    fullCorrection,
                    frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence <
                    AnimationFootConstraintFacts.LockedMinimumConfidence ||
                horizontalError > frame.Settings.SlideDistance)
            {
                return Decision(
                    horizontalError > frame.Settings.SlideDistance
                        ? CharacterFootTransitionReason.ContactOutOfSlideRange
                        : CharacterFootTransitionReason.ContactReleased,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    discrete.PlantCycleConsumed,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            CharacterFootLockResponse response =
                horizontalError > frame.Settings.LockDistance
                    ? CharacterFootLockResponse.Sliding
                    : CharacterFootLockResponse.FullAnchor;
            return Decision(
                response != discrete.LockResponse
                    ? CharacterFootTransitionReason.LockResponseChanged
                    : CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                response,
                discrete.PlantCycleConsumed,
                CharacterFootAnchorCommand.Retain,
                false,
                false);
        }

        static CharacterFootTransitionDecision NoChange(
            in CharacterFootDiscreteStateContext discrete,
            CharacterFootTransitionPhase phase =
                CharacterFootTransitionPhase.PreInterpolation) =>
            Decision(
                CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                discrete.LockResponse,
                discrete.PlantCycleConsumed,
                CharacterFootAnchorCommand.None,
                false,
                false,
                phase);

        static CharacterFootTransitionDecision Decision(
            CharacterFootTransitionReason reason,
            CharacterFootConstraintState source,
            CharacterFootConstraintState target,
            CharacterFootLockResponse targetResponse,
            bool plantCycleConsumed,
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
                plantCycleConsumed,
                anchorCommand,
                suppressOutput,
                resetInterpolation);

        static bool CanAcquire(in CharacterFootStateFrame frame) =>
            frame.HasContactLanding &&
            frame.ContactLanding.LandingEventIdentity != 0;
    }
}
