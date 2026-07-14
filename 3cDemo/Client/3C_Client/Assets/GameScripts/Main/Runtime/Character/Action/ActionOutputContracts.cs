namespace ThirdPersonCharacter.ActionSystem
{
    public interface IActionRuntimeService
    {
        ActionActivationOutcome ActivateAction(ActionActivationRequest request);
        bool ApplyActionLifecycleTransition(ActionLifecycleTransition transition);
    }

    public readonly struct ActionActivationOutcome
    {
        public ActionActivationOutcome(ActionActivationResult result, ActionInstanceHandle handle, ActionLifecycleTransition replacedTransition = default)
        {
            Result = result;
            Handle = handle;
            ReplacedTransition = replacedTransition;
        }

        public ActionActivationResult Result { get; }
        public ActionInstanceHandle Handle { get; }
        public ActionLifecycleTransition ReplacedTransition { get; }
        public bool HasReplacedTransition => ReplacedTransition.IsValid;
    }

    public readonly struct ActionActivationOutput
    {
        public ActionActivationOutput(ActionActivationRequest request, ActionInstanceHandle handle)
        {
            Handle = handle;
            ActionInstanceId = handle.ActionInstanceId;
            ActionId = handle.IsValid ? handle.ActionId : request.ActionId;
            PredictionKey = handle.PredictionKey;
            SourceInputRequestId = request.SourceInputRequestId;
            InputSequence = handle.IsValid ? handle.InputSequence : request.InputSequence;
            LocalLogicTick = handle.IsValid ? handle.StartLocalLogicTick : request.LocalLogicTick;
            TargetKey = handle.IsValid ? handle.TargetKey : request.TargetKey;
            TargetSnapshot = handle.IsValid ? handle.TargetSnapshot : request.TargetSnapshot;
            SourceGraphId = request.SourceGraphId;
            SourceNodeId = request.SourceNodeId;
            SourceName = request.SourceName;
        }

        public ActionInstanceHandle Handle { get; }
        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public ulong PredictionKey { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public string TargetKey { get; }
        public ActionTargetSnapshot TargetSnapshot { get; }
        public string SourceGraphId { get; }
        public string SourceNodeId { get; }
        public string SourceName { get; }
    }

    public readonly struct ActionWindowSample
    {
        public ActionWindowSample(ulong actionInstanceId, string windowId, string windowType, ulong startLocalLogicTick, ulong endLocalLogicTick, ulong digest)
        {
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            WindowType = windowType ?? string.Empty;
            StartLocalLogicTick = startLocalLogicTick;
            EndLocalLogicTick = endLocalLogicTick;
            Digest = digest;
        }

        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string WindowType { get; }
        public ulong StartLocalLogicTick { get; }
        public ulong EndLocalLogicTick { get; }
        public ulong Digest { get; }
    }

    public readonly struct ActionMotionSample
    {
        public ActionMotionSample(
            ulong actionInstanceId,
            ulong inputSequence,
            ulong localLogicTick,
            ActionMotionSourceType sourceType)
        {
            ActionInstanceId = actionInstanceId;
            InputSequence = inputSequence;
            LocalLogicTick = localLogicTick;
            SourceType = sourceType;
        }

        public ulong ActionInstanceId { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public ActionMotionSourceType SourceType { get; }
    }

    public readonly struct GameplayResultEvent
    {
        public GameplayResultEvent(
            ulong resultId,
            ulong actionInstanceId,
            string windowId,
            string targetId,
            string resultType,
            ulong localLogicTick)
            : this(string.Empty, resultId, actionInstanceId, windowId, targetId, resultType, localLogicTick)
        {
        }

        public GameplayResultEvent(
            string behaviorId,
            ulong resultId,
            ulong actionInstanceId,
            string windowId,
            string targetId,
            string resultType,
            ulong localLogicTick)
        {
            BehaviorId = behaviorId ?? string.Empty;
            ResultId = resultId;
            ActionInstanceId = actionInstanceId;
            WindowId = windowId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            ResultType = resultType ?? string.Empty;
            LocalLogicTick = localLogicTick;
        }

        public string BehaviorId { get; }
        public ulong ResultId { get; }
        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string TargetId { get; }
        public string ResultType { get; }
        public ulong LocalLogicTick { get; }

        public GameplayResultEvent WithBehaviorId(string behaviorId)
        {
            return new GameplayResultEvent(
                behaviorId,
                ResultId,
                ActionInstanceId,
                WindowId,
                TargetId,
                ResultType,
                LocalLogicTick);
        }
    }
}
