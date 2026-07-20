namespace ThirdPersonCharacter.ActionSystem
{
    public enum ActionLifecycleTransitionType
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

    public enum ActionActivationResult
    {
        Activated,
        InvalidRequest,
        MissingProfile,
        Blocked,
        AlreadyActive
    }
}
