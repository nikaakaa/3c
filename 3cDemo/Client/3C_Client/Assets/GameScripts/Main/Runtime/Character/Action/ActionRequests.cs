namespace ThirdPersonCharacter.ActionSystem
{
    public readonly struct ActionActivationRequest
    {
        public ActionActivationRequest(
            string actionId,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong localLogicTick,
            string targetKey,
            ActionTargetSnapshot targetSnapshot,
            string sourceGraphId,
            string sourceNodeId,
            string sourceName = "")
        {
            ActionId = actionId ?? string.Empty;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            LocalLogicTick = localLogicTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            SourceGraphId = sourceGraphId ?? string.Empty;
            SourceNodeId = sourceNodeId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
        }

        public string ActionId { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public string TargetKey { get; }
        public ActionTargetSnapshot TargetSnapshot { get; }
        public string SourceGraphId { get; }
        public string SourceNodeId { get; }
        public string SourceName { get; }
        public bool HasSourceInputRequest => !string.IsNullOrEmpty(SourceInputRequestId);
        public bool IsValid => !string.IsNullOrEmpty(ActionId);
    }

    public readonly struct ActionLifecycleTransition
    {
        public ActionLifecycleTransition(
            ulong actionInstanceId,
            ActionLifecycleTransitionType transitionType,
            ulong localLogicTick,
            ulong inputSequence = 0,
            string reason = "",
            string sourceGraphId = "",
            string sourceNodeId = "",
            string sourceName = "",
            ulong sourceTick = 0,
            ulong correctionId = 0)
        {
            ActionInstanceId = actionInstanceId;
            TransitionType = transitionType;
            LocalLogicTick = localLogicTick;
            InputSequence = inputSequence;
            Reason = reason ?? string.Empty;
            SourceGraphId = sourceGraphId ?? string.Empty;
            SourceNodeId = sourceNodeId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceTick = sourceTick;
            CorrectionId = correctionId;
        }

        public ulong ActionInstanceId { get; }
        public ActionLifecycleTransitionType TransitionType { get; }
        public ulong LocalLogicTick { get; }
        public ulong InputSequence { get; }
        public string Reason { get; }
        public string SourceGraphId { get; }
        public string SourceNodeId { get; }
        public string SourceName { get; }
        public ulong SourceTick { get; }
        public ulong CorrectionId { get; }
        public bool IsTerminal => ActionLifecycleTransitionFacts.IsTerminal(TransitionType);
        public bool IsValid => ActionInstanceId != 0 && TransitionType != ActionLifecycleTransitionType.None;
    }

    public static class ActionLifecycleTransitionFacts
    {
        public static bool IsTerminal(ActionLifecycleTransitionType transitionType)
        {
            return transitionType == ActionLifecycleTransitionType.Complete ||
                   transitionType == ActionLifecycleTransitionType.Cancel ||
                   transitionType == ActionLifecycleTransitionType.Interrupt ||
                   transitionType == ActionLifecycleTransitionType.Reject ||
                   transitionType == ActionLifecycleTransitionType.Abort;
        }
    }
}
