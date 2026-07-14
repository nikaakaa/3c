namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public enum ServerAuthoritativePredictionPolicy
    {
        None,
        LocalPredicted,
        AuthorityConfirmed
    }

    public enum ServerAuthoritativeAuthorityPolicy
    {
        LocalOnly,
        AuthorityConfirmed,
        ServerAuthoritative
    }

    public enum ServerAuthoritativeReplicationPolicy
    {
        None,
        OwnerOnly,
        RemoteInterpolated,
        Broadcast
    }

    public enum ServerAuthoritativeSnapshotPolicy
    {
        None,
        OwnerInputOnly,
        ServerSnapshot
    }

    public enum ServerAuthoritativeRemotePresentationPolicy
    {
        None,
        RemoteInterpolated
    }

    public enum ServerAuthoritativeHistoryPolicy
    {
        None,
        IncludeDigestOnly,
        IncludeInGameplayHistory
    }

    public enum ServerAuthoritativeCommandSendPolicy
    {
        None,
        EveryTick
    }

    public enum ServerAuthoritativeWindowAuthorityPolicy
    {
        LocalPredicted,
        ServerCorrectable,
        ServerAuthoritative
    }

    public enum ServerAuthoritativeWindowHistoryPolicy
    {
        None,
        IncludeDigestOnly,
        IncludeInCombatHistory
    }

    public enum ServerAuthoritativeWindowReplicationPolicy
    {
        None,
        OwnerOnly,
        DigestOnly,
        Broadcast
    }

    public enum ServerAuthoritativeCuePlaybackPolicy
    {
        LocalOnly,
        LocalPredicted,
        AuthorityConfirmed
    }

    public enum ServerAuthoritativeGameplayResultProposalPolicy
    {
        AuthorityOnly,
        ClientProposal
    }

    public enum ServerAuthoritativeGameplayResultHistoryPolicy
    {
        None,
        IncludeDigestOnly,
        IncludeInCombatHistory
    }

    public enum ServerAuthoritativeGameplayResultReplicationPolicy
    {
        None,
        OwnerOnly,
        Broadcast
    }

    public enum ServerAuthoritativeFactKind
    {
        None,
        MotionCommand,
        MotionCorrectionAcknowledgement,
        GameplayAttributeValue
    }
}
