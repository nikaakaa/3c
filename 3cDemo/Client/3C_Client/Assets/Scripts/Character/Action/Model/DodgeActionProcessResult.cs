namespace ThirdPersonAction
{
    public readonly struct DodgeActionProcessResult
    {
        DodgeActionProcessResult(
            bool hasRequest,
            bool accepted,
            DodgeActionRequest request,
            ActionInterruptDecision decision,
            ActionAnimationCommand animationCommand)
        {
            HasRequest = hasRequest;
            Accepted = accepted;
            Request = request;
            Decision = decision;
            AnimationCommand = animationCommand;
        }

        public bool HasRequest { get; }
        public bool Accepted { get; }
        public DodgeActionRequest Request { get; }
        public ActionInterruptDecision Decision { get; }
        public ActionAnimationCommand AnimationCommand { get; }

        public static DodgeActionProcessResult NoRequest()
        {
            return new DodgeActionProcessResult(false, false, default, ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest), default);
        }

        public static DodgeActionProcessResult Rejected(DodgeActionRequest request, ActionInterruptDecision decision)
        {
            return new DodgeActionProcessResult(true, false, request, decision, default);
        }

        public static DodgeActionProcessResult AcceptedResult(
            DodgeActionRequest request,
            ActionInterruptDecision decision,
            ActionAnimationCommand animationCommand)
        {
            return new DodgeActionProcessResult(true, true, request, decision, animationCommand);
        }
    }
}
