namespace ThirdPersonCharacter.ActionSystem
{
    public enum ActionPhase
    {
        Startup,
        Active,
        Recovery,
        Cancel,
        Ended
    }

    public enum ActionState
    {
        Requested,
        Predicted,
        Confirmed,
        Rejected,
        Cancelled,
        Interrupted,
        Aborted,
        Ended,
        Corrected
    }

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
