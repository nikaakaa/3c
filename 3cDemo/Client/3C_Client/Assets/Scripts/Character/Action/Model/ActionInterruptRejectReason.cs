namespace ThirdPersonAction
{
    public enum ActionInterruptRejectReason
    {
        None = 0,
        NoRequest = 1,
        Expired = 2,
        NoPolicy = 3,
        PriorityTooLow = 4,
        BlockedByResistance = 5,
        TimingNotSatisfied = 6,
        InvalidPolicy = 7
    }
}
