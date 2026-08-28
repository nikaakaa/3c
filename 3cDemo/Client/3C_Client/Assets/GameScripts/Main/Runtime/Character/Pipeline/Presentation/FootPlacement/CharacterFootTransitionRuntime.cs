namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootTransitionRuntime
    {
        internal static void Apply(
            ref CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision decision,
            in CharacterFootStateFrame frame)
        {
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
                    context.Contact.Anchor =
                        CharacterFootConstraintMath.ResolveContactAnchor(
                            frame.AnimatedFoot,
                            frame.ContactLanding.Point,
                            frame.ContactLanding.Normal);
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
    }
}
