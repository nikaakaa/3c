namespace ThirdPersonCharacter.ActionSystem
{
    // 一次已接受的动作事务身份，不代表 Graph、Timeline、Tree 或节点身份。
    public sealed class ActionInstance
    {
        internal ActionInstance(
            ulong instanceId,
            string actionId,
            ulong predictionKey,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startLocalLogicTick,
            string targetKey,
            ActionTargetSnapshot targetSnapshot)
        {
            InstanceId = instanceId;
            ActionId = actionId ?? string.Empty;
            PredictionKey = predictionKey;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            StartLocalLogicTick = startLocalLogicTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            Phase = ActionPhase.Startup;
            State = ActionState.Predicted;
        }

        public ulong InstanceId { get; }
        public string ActionId { get; }
        public ulong PredictionKey { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartLocalLogicTick { get; }
        public string TargetKey { get; }
        public ActionTargetSnapshot TargetSnapshot { get; }
        public string SourceGraphId { get; private set; } = string.Empty;
        public string SourceNodeId { get; private set; } = string.Empty;
        public string SourceName { get; private set; } = string.Empty;
        public ActionPhase Phase { get; private set; }
        public ActionState State { get; private set; }
        public string LastReason { get; private set; } = string.Empty;
        public ActionLifecycleTransitionType LastTransitionType { get; private set; }
        public ulong LastTransitionLocalLogicTick { get; private set; }
        public ulong LastTransitionSourceTick { get; private set; }
        public string LastTransitionSourceGraphId { get; private set; } = string.Empty;
        public string LastTransitionSourceNodeId { get; private set; } = string.Empty;
        public string LastTransitionSourceName { get; private set; } = string.Empty;
        public bool IsTerminal => State == ActionState.Rejected ||
                                  State == ActionState.Cancelled ||
                                  State == ActionState.Interrupted ||
                                  State == ActionState.Aborted ||
                                  State == ActionState.Ended;

        internal void SetSourceIdentity(string sourceGraphId, string sourceNodeId, string sourceName)
        {
            SourceGraphId = sourceGraphId ?? string.Empty;
            SourceNodeId = sourceNodeId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
        }

        internal void SetPhase(ActionPhase phase)
        {
            Phase = phase;
        }

        internal void ApplyLifecycleTransition(ActionLifecycleTransition transition)
        {
            LastTransitionType = transition.TransitionType;
            LastTransitionLocalLogicTick = transition.LocalLogicTick;
            LastTransitionSourceTick = transition.SourceTick;
            LastTransitionSourceGraphId = transition.SourceGraphId;
            LastTransitionSourceNodeId = transition.SourceNodeId;
            LastTransitionSourceName = transition.SourceName;
            LastReason = transition.Reason;

            switch (transition.TransitionType)
            {
                case ActionLifecycleTransitionType.Confirm:
                    State = ActionState.Confirmed;
                    LastReason = string.Empty;
                    break;
                case ActionLifecycleTransitionType.Complete:
                    State = ActionState.Ended;
                    Phase = ActionPhase.Ended;
                    break;
                case ActionLifecycleTransitionType.Cancel:
                    State = ActionState.Cancelled;
                    Phase = ActionPhase.Cancel;
                    break;
                case ActionLifecycleTransitionType.Interrupt:
                    State = ActionState.Interrupted;
                    Phase = ActionPhase.Cancel;
                    break;
                case ActionLifecycleTransitionType.Reject:
                    State = ActionState.Rejected;
                    Phase = ActionPhase.Ended;
                    break;
                case ActionLifecycleTransitionType.Correct:
                    State = ActionState.Corrected;
                    break;
                case ActionLifecycleTransitionType.Abort:
                    State = ActionState.Aborted;
                    Phase = ActionPhase.Ended;
                    break;
            }
        }
    }
}
