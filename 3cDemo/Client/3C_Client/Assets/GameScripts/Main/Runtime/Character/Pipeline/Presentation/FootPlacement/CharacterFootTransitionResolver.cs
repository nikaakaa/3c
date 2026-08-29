using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootTransitionResolver
    {
        internal static bool RequiresPlantVerification(
            in CharacterFootLifecycleContext context,
            in CharacterFootLockRequest request)
        {
            if (!request.RequestsLock || request.EventIdentity == 0 ||
                context.LandingSnapshot.TryResolveVerifiedLanding(
                    request.EventIdentity,
                    out _))
            {
                return false;
            }
            CharacterFootContactEdge edge = ResolveContactEdge(
                in context.ContactTransition,
                in request);
            bool acquisitionEdge = edge == CharacterFootContactEdge.Rising ||
                                   edge == CharacterFootContactEdge.EventChanged;
            if (!acquisitionEdge)
                return false;
            return context.Discrete.State switch
            {
                CharacterFootConstraintState.Swing => true,
                CharacterFootConstraintState.UnlockedSupport => true,
                CharacterFootConstraintState.Releasing =>
                    request.EventIdentity != context.Contact.EventIdentity &&
                    request.EventIdentity != context.ContactTransition
                        .LatestReleasedContactEventIdentity,
                CharacterFootConstraintState.Landing =>
                    edge == CharacterFootContactEdge.EventChanged &&
                    request.EventIdentity != context.Contact.EventIdentity,
                CharacterFootConstraintState.Locked =>
                    edge == CharacterFootContactEdge.EventChanged &&
                    request.EventIdentity != context.Contact.EventIdentity,
                _ => false
            };
        }

        internal static CharacterFootTransitionDecision ResolvePreInterpolation(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            CharacterFootLockRequest request = frame.LockRequest;
            CharacterFootContactEdge edge = ResolveContactEdge(
                in context.ContactTransition,
                in request);
            if (frame.HardOwnershipLoss)
            {
                return Decision(
                    CharacterFootTransitionReason.OwnershipLost,
                    discrete.State,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Release,
                    true,
                    true);
            }

            return discrete.State switch
            {
                CharacterFootConstraintState.Swing =>
                    ResolveUnconstrained(in context, in frame, edge),
                CharacterFootConstraintState.UnlockedSupport =>
                    ResolveUnconstrained(in context, in frame, edge),
                CharacterFootConstraintState.Landing =>
                    ResolveLanding(in context, in frame, edge),
                CharacterFootConstraintState.Locked =>
                    ResolveLocked(in context, in frame, edge),
                CharacterFootConstraintState.Releasing =>
                    ResolveReleasing(in context, in frame, edge),
                _ => throw new System.InvalidOperationException(
                    "Foot constraint state is invalid.")
            };
        }

        internal static CharacterFootTransitionDecision ResolvePostInterpolation(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            bool interpolationCompleted,
            bool landingCompletionAllowed)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            if (!interpolationCompleted)
                return NoChange(in discrete, CharacterFootContactEdge.None,
                    CharacterFootTransitionPhase.PostInterpolation);
            if (discrete.State == CharacterFootConstraintState.Landing)
            {
                if (!landingCompletionAllowed)
                    return NoChange(in discrete, CharacterFootContactEdge.None,
                        CharacterFootTransitionPhase.PostInterpolation);
                if (!frame.LockRequest.RequestsLock ||
                    frame.LockRequest.EventIdentity != context.Contact.EventIdentity)
                {
                    return NoChange(in discrete, CharacterFootContactEdge.None,
                        CharacterFootTransitionPhase.PostInterpolation);
                }
                return Decision(
                    CharacterFootTransitionReason.LandingCompleted,
                    discrete.State,
                    CharacterFootConstraintState.Locked,
                    frame.LockRequest.Response,
                    CharacterFootContactEdge.None,
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
                    CharacterFootContactEdge.None,
                    CharacterFootAnchorCommand.Release,
                    false,
                    true,
                    CharacterFootTransitionPhase.PostInterpolation);
            }
            return NoChange(in discrete, CharacterFootContactEdge.None,
                CharacterFootTransitionPhase.PostInterpolation);
        }

        static CharacterFootTransitionDecision ResolveUnconstrained(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            CharacterFootContactEdge edge)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            CharacterFootLockRequest request = frame.LockRequest;
            bool wantsLock = request.Contact > 0f &&
                             request.Mode !=
                             AnimationFootStepObservationLockMode.Unlocked;
            if (wantsLock && request.Availability ==
                    CharacterFootLockRequestAvailability.ContactEventUnavailable)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactEventUnavailable,
                    discrete.State,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            if (!request.RequestsLock)
            {
                return Decision(
                    CharacterFootTransitionReason.SwingStarted,
                    discrete.State,
                    CharacterFootConstraintState.Swing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            if (context.ContactTransition.LatestReleasedContactEventIdentity ==
                request.EventIdentity)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactUnavailable,
                    discrete.State,
                    CharacterFootConstraintState.UnlockedSupport,
                    CharacterFootLockResponse.None,
                    edge,
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
                    edge,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            float horizontalError = ResolveHorizontalError(in frame);
            if (horizontalError > frame.Settings.LockDistance)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactOutOfLockRange,
                    discrete.State,
                    CharacterFootConstraintState.UnlockedSupport,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Release,
                    false,
                    false);
            }
            return Decision(
                CharacterFootTransitionReason.ContactAcquired,
                discrete.State,
                CharacterFootConstraintState.Landing,
                CharacterFootLockResponse.None,
                edge,
                CharacterFootAnchorCommand.Create,
                false,
                false);
        }

        static CharacterFootTransitionDecision ResolveLanding(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            CharacterFootContactEdge edge)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            if (!OwnsRequest(in context, in frame))
            {
                if (CanAcquire(in frame) && context.Contact.HasContact &&
                    frame.LockRequest.EventIdentity !=
                    context.Contact.EventIdentity)
                {
                    return Decision(
                        CharacterFootTransitionReason.NewEventContactAcquired,
                        discrete.State,
                        CharacterFootConstraintState.Landing,
                        CharacterFootLockResponse.None,
                        edge,
                        CharacterFootAnchorCommand.Create,
                        false,
                        false);
                }
                return Decision(
                    CharacterFootTransitionReason.ContactReleased,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            float horizontalError = ResolveAnchorHorizontalError(
                in context,
                in frame);
            if (horizontalError > frame.Settings.SlideDistance)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactOutOfSlideRange,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            return NoChange(in discrete, edge);
        }

        static CharacterFootTransitionDecision ResolveLocked(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            CharacterFootContactEdge edge)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            if (!OwnsRequest(in context, in frame))
            {
                if (CanAcquire(in frame) && context.Contact.HasContact &&
                    frame.LockRequest.EventIdentity !=
                    context.Contact.EventIdentity)
                {
                    return Decision(
                        CharacterFootTransitionReason.NewEventContactAcquired,
                        discrete.State,
                        CharacterFootConstraintState.Landing,
                        CharacterFootLockResponse.None,
                        edge,
                        CharacterFootAnchorCommand.Create,
                        false,
                        false);
                }
                return Decision(
                    CharacterFootTransitionReason.ContactReleased,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            float horizontalError = ResolveAnchorHorizontalError(
                in context,
                in frame);
            if (horizontalError > frame.Settings.SlideDistance)
            {
                return Decision(
                    CharacterFootTransitionReason.ContactOutOfSlideRange,
                    discrete.State,
                    CharacterFootConstraintState.Releasing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            CharacterFootLockResponse response = frame.LockRequest.Response;
            return Decision(
                response != discrete.LockResponse
                    ? CharacterFootTransitionReason.LockResponseChanged
                    : CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                response,
                edge,
                CharacterFootAnchorCommand.Retain,
                false,
                false);
        }

        static CharacterFootTransitionDecision ResolveReleasing(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            CharacterFootContactEdge edge)
        {
            CharacterFootDiscreteStateContext discrete = context.Discrete;
            CharacterFootLockRequest request = frame.LockRequest;
            if (!request.RequestsLock || !CanAcquire(in frame))
                return NoChange(in discrete, edge);
            float horizontalError = ResolveHorizontalError(in frame);
            if (horizontalError > frame.Settings.LockDistance)
                return NoChange(in discrete, edge);
            if (context.Contact.HasContact &&
                context.Contact.EventIdentity == request.EventIdentity &&
                edge == CharacterFootContactEdge.Rising)
            {
                return Decision(
                    CharacterFootTransitionReason.SameEventContactReentryRefresh,
                    discrete.State,
                    CharacterFootConstraintState.Landing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Retain,
                    false,
                    false);
            }
            if (request.EventIdentity != context.Contact.EventIdentity &&
                request.EventIdentity !=
                context.ContactTransition.LatestReleasedContactEventIdentity)
            {
                return Decision(
                    CharacterFootTransitionReason.NewEventContactAcquired,
                    discrete.State,
                    CharacterFootConstraintState.Landing,
                    CharacterFootLockResponse.None,
                    edge,
                    CharacterFootAnchorCommand.Create,
                    false,
                    false);
            }
            return NoChange(in discrete, edge);
        }

        static CharacterFootContactEdge ResolveContactEdge(
            in CharacterFootContactTransitionContext context,
            in CharacterFootLockRequest request)
        {
            if (!context.HasPreviousRequest)
                return request.RequestsLock
                    ? CharacterFootContactEdge.Rising
                    : CharacterFootContactEdge.None;
            if (request.RequestsLock)
            {
                if (!context.PreviousRequestedLock)
                    return CharacterFootContactEdge.Rising;
                return request.EventIdentity != context.PreviousEventIdentity
                    ? CharacterFootContactEdge.EventChanged
                    : CharacterFootContactEdge.None;
            }
            return context.PreviousRequestedLock
                ? CharacterFootContactEdge.Falling
                : CharacterFootContactEdge.None;
        }

        static CharacterFootTransitionDecision NoChange(
            in CharacterFootDiscreteStateContext discrete,
            CharacterFootContactEdge edge,
            CharacterFootTransitionPhase phase =
                CharacterFootTransitionPhase.PreInterpolation) =>
            Decision(
                CharacterFootTransitionReason.None,
                discrete.State,
                discrete.State,
                discrete.LockResponse,
                edge,
                CharacterFootAnchorCommand.None,
                false,
                false,
                phase);

        static CharacterFootTransitionDecision Decision(
            CharacterFootTransitionReason reason,
            CharacterFootConstraintState source,
            CharacterFootConstraintState target,
            CharacterFootLockResponse targetResponse,
            CharacterFootContactEdge edge,
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
                edge,
                anchorCommand,
                suppressOutput,
                resetInterpolation);

        static bool CanAcquire(in CharacterFootStateFrame frame) =>
            frame.LockRequest.RequestsLock &&
            frame.HasContactLanding &&
            frame.ContactLanding.LandingEventIdentity ==
                frame.LockRequest.EventIdentity;

        static bool OwnsRequest(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame) =>
            frame.LockRequest.RequestsLock &&
            context.Contact.HasContact &&
            frame.LockRequest.EventIdentity == context.Contact.EventIdentity;

        static float ResolveHorizontalError(in CharacterFootStateFrame frame)
        {
            var correction = CharacterFootConstraintMath.ResolveContactCorrection(
                frame.AnimatedFoot,
                frame.ContactLanding.Point);
            return CharacterFootConstraintMath.ResolveHorizontalError(
                correction,
                frame.ComponentUp);
        }

        static float ResolveAnchorHorizontalError(
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
    }
}
