namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public enum ServerAuthoritativeDomain
    {
        None,
        Motion,
        Action,
        GameplayResult,
        GameplayEffect,
        Presentation
    }

    public enum ServerAuthoritativePacketKind
    {
        None,
        MotionCommand,
        MotionSnapshot,
        MotionCorrection,
        MotionCorrectionAck,
        ActionActivation,
        ActionLifecycleTransition,
        ActionInstanceDecision,
        ActionWindowDigest,
        ActionMotionDigest,
        GameplayResult,
        GameplayEffectLifecycle,
        GameplayAttributeValue,
        GameplayCue
    }

    public enum ServerAuthoritativePacketDirection
    {
        Outgoing,
        Incoming,
        Pending,
        Dropped
    }

    public enum ServerAuthoritativeActionDecisionKind
    {
        Confirmed,
        Rejected,
        Corrected
    }

    public enum ServerAuthoritativeActionLifecycleTransitionKind
    {
        None,
        Confirm,
        Complete,
        Cancel,
        Interrupt,
        Reject,
        Correct,
        Abort
    }

    public static class GameplayDemoActorIdentityFields
    {
        public const string OwnerPlayerId = "ownerPlayerId";
        public const string TeamId = "teamId";
        public const string ActorId = "actorId";
        public const string ControlledActorId = "controlledActorId";
        public const string PerformerActorId = "performerActorId";
        public const string TargetActorId = "targetActorId";
    }

    public static class GameplayDemoWindowTypes
    {
        public const string HitWindow = "HitWindow";
        public const string IFrameWindow = "IFrameWindow";
        public const string ParryWindow = "ParryWindow";
        public const string CancelWindow = "CancelWindow";
    }

    public static class GameplayDemoResultTypes
    {
        public const string HitConfirmed = "HitConfirmed";
        public const string Blocked = "Blocked";
        public const string Interrupted = "Interrupted";
        public const string Knockback = "Knockback";
        public const string ObjectiveProgress = "ObjectiveProgress";
    }

}
