using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionInterruptDecision
    {
        ActionInterruptDecision(
            bool accepted,
            ActionInterruptRequest selectedRequest,
            ActionStateId targetState,
            ActionInterruptRejectReason rejectReason)
        {
            Accepted = accepted;
            SelectedRequest = selectedRequest;
            TargetState = targetState;
            RejectReason = rejectReason;
        }

        public bool Accepted { get; }
        public ActionInterruptRequest SelectedRequest { get; }
        public ActionStateId TargetState { get; }
        public ActionInterruptRejectReason RejectReason { get; }

        public static ActionInterruptDecision Accept(ActionInterruptRequest request)
        {
            return new ActionInterruptDecision(true, request, request.TargetState, ActionInterruptRejectReason.None);
        }

        public static ActionInterruptDecision Reject(ActionInterruptRejectReason reason)
        {
            return new ActionInterruptDecision(false, default, ActionStateId.Empty, reason);
        }
    }
}
