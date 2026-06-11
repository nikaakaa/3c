using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionInterruptRequest
    {
        public const int NeverExpires = int.MaxValue;

        public ActionInterruptRequest(
            int requestId,
            ActionRequestType requestType,
            ActionStateId targetState,
            int priority,
            int sourceOrder = 0,
            int originTick = 0,
            int expireTick = NeverExpires)
        {
            RequestId = requestId;
            RequestType = requestType;
            TargetState = targetState;
            Priority = priority;
            SourceOrder = sourceOrder;
            OriginTick = originTick < 0 ? 0 : originTick;
            ExpireTick = expireTick < 0 ? originTick : expireTick;
        }

        public int RequestId { get; }
        public ActionRequestType RequestType { get; }
        public ActionStateId TargetState { get; }
        public int Priority { get; }
        public int SourceOrder { get; }
        public int OriginTick { get; }
        public int ExpireTick { get; }
        public bool HasExpiration => ExpireTick != NeverExpires;

        public bool IsExpired(int currentTick)
        {
            return HasExpiration && currentTick > ExpireTick;
        }
    }
}
