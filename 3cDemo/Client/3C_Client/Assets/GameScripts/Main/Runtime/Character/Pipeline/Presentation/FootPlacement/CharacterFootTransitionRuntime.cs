namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootTransitionRuntime
    {
        internal static void Apply(
            ref CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision decision,
            in CharacterFootStateFrame frame)
        {
            if (decision.Phase == CharacterFootTransitionPhase.PreInterpolation)
            {
                UpdateContactTransition(
                    ref context.ContactTransition,
                    in decision,
                    in frame);
            }
            context.Discrete.State = decision.TargetState;
            context.Discrete.LockResponse = decision.TargetLockResponse;
            context.Discrete.LastTransitionPhase = decision.Phase;
            context.Discrete.LastTransitionReason = decision.Reason;
            switch (decision.AnchorCommand)
            {
                case CharacterFootAnchorCommand.None:
                case CharacterFootAnchorCommand.Retain:
                    break;
                case CharacterFootAnchorCommand.Create:
                    context.ContactTransition.UnloadingEventIdentity = 0;
                    context.ContactTransition.UnloadingReentryProtectedEventIdentity = 0;
                    context.Contact.HasContact = true;
                    context.Contact.EventIdentity =
                        frame.ContactLanding.LandingEventIdentity;
                    context.Contact.AcquiredFrameSequence =
                        frame.FrameSequence;
                    context.Contact.AcquiredCompletionIdentity =
                        frame.CompletionIdentity;
                    context.Contact.WorldRevision = frame.WorldRevision;
                    context.Contact.SurfaceIdentity =
                        frame.ContactLanding.SurfaceIdentity;
                    context.Contact.Anchor = frame.ContactLanding.Point;
                    context.Contact.Normal = frame.ContactLanding.Normal;
                    break;
                case CharacterFootAnchorCommand.Release:
                    context.Contact.Clear();
                    context.ContactTransition.CompletedLockWeightEventIdentity = 0;
                    context.ContactTransition.UnloadingEventIdentity = 0;
                    context.ContactTransition.UnloadingReentryProtectedEventIdentity = 0;
                    break;
                default:
                    throw new System.InvalidOperationException(
                        "Foot anchor command is invalid.");
            }
        }

        static void UpdateContactTransition(
            ref CharacterFootContactTransitionContext context,
            in CharacterFootTransitionDecision decision,
            in CharacterFootStateFrame frame)
        {
            ulong eventIdentity = frame.LockRequest.EventIdentity;
            if (eventIdentity != 0 &&
                context.UnloadingEventIdentity != eventIdentity)
            {
                context.UnloadingEventIdentity = 0;
            }
            if (eventIdentity != 0 &&
                context.UnloadingReentryProtectedEventIdentity != eventIdentity)
            {
                context.UnloadingReentryProtectedEventIdentity = 0;
            }
            if (decision.Reason == CharacterFootTransitionReason.SourceLiftUnloading)
            {
                context.UnloadingEventIdentity = eventIdentity;
            }
            else if (context.UnloadingEventIdentity == eventIdentity &&
                     eventIdentity != 0 &&
                     (decision.Reason == CharacterFootTransitionReason.SameEventContactReentryRefresh ||
                      decision.Reason == CharacterFootTransitionReason.UnloadingLockRestored))
            {
                context.UnloadingEventIdentity = 0;
                context.UnloadingReentryProtectedEventIdentity = eventIdentity;
            }
            if (eventIdentity != 0 &&
                context.CompletedLockWeightEventIdentity != 0 &&
                context.CompletedLockWeightEventIdentity != eventIdentity)
            {
                context.CompletedLockWeightEventIdentity = 0;
            }
            if (decision.ContactEdge == CharacterFootContactEdge.None)
            {
                context.SecondsSinceEdge += frame.DeltaSeconds;
            }
            else
            {
                context.SecondsSinceEdge = 0f;
                if (decision.ContactEdge == CharacterFootContactEdge.Falling ||
                    decision.ContactEdge == CharacterFootContactEdge.EventChanged)
                {
                    context.LatestReleasedContactEventIdentity =
                        context.PreviousEventIdentity;
                }
                if (decision.ContactEdge == CharacterFootContactEdge.Rising ||
                    decision.ContactEdge == CharacterFootContactEdge.EventChanged)
                {
                    context.LatestContactEventIdentity =
                        frame.LockRequest.EventIdentity;
                }
            }
            context.HasPreviousRequest = true;
            context.PreviousRequestedLock = frame.LockRequest.RequestsLock;
            context.PreviousEventIdentity = frame.LockRequest.EventIdentity;
            context.PreviousMode = frame.LockRequest.Mode;
            context.PreviousWeight = frame.LockRequest.Weight;
            context.LastEdge = decision.ContactEdge;
            if (frame.LockRequest.RequestsLock && eventIdentity != 0 &&
                frame.LockRequest.Weight >=
                1f - CharacterFootConstraintMath.GeometryEpsilon)
            {
                context.CompletedLockWeightEventIdentity = eventIdentity;
            }
        }
    }
}
