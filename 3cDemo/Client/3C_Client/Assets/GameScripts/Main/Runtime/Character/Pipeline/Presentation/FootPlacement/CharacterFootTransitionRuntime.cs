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
                    context.Contact.HasContact = true;
                    context.Contact.EventIdentity =
                        frame.ContactLanding.LandingEventIdentity;
                    context.Contact.SurfaceIdentity =
                        frame.ContactLanding.SurfaceIdentity;
                    context.Contact.Anchor = frame.ContactLanding.Point;
                    context.Contact.Normal = frame.ContactLanding.Normal;
                    break;
                case CharacterFootAnchorCommand.Release:
                    context.Contact.Clear();
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
        }
    }
}
