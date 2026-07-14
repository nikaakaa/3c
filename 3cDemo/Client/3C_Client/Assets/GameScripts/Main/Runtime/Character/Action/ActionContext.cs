namespace ThirdPersonCharacter.ActionSystem
{
    public readonly struct ActionInstanceHandle
    {
        public ActionInstanceHandle(
            ulong actionInstanceId,
            string actionId,
            ulong predictionKey,
            ulong inputSequence,
            ulong startLocalLogicTick,
            string targetKey,
            ActionTargetSnapshot targetSnapshot)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId ?? string.Empty;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            StartLocalLogicTick = startLocalLogicTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
        }

        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public ulong StartLocalLogicTick { get; }
        public string TargetKey { get; }
        public ActionTargetSnapshot TargetSnapshot { get; }
        public bool IsValid => ActionInstanceId != 0;

        public static ActionInstanceHandle From(ActionInstance instance)
        {
            if (instance == null)
                return default;

            return new ActionInstanceHandle(
                instance.InstanceId,
                instance.ActionId,
                instance.PredictionKey,
                instance.InputSequence,
                instance.StartLocalLogicTick,
                instance.TargetKey,
                instance.TargetSnapshot);
        }
    }

    public readonly struct ActionContext
    {
        public ActionContext(ActionProfile profile, ActionInstance instance)
        {
            Profile = profile;
            Instance = instance;
        }

        public ActionProfile Profile { get; }
        public ActionInstance Instance { get; }
        public bool HasActiveInstance => Profile != null && Instance != null;
        public ulong ActionInstanceId => Instance != null ? Instance.InstanceId : 0;
        public string ActionId => Instance != null ? Instance.ActionId : string.Empty;
        public ulong PredictionKey => Instance != null ? Instance.PredictionKey : 0;
        public ulong InputSequence => Instance != null ? Instance.InputSequence : 0;
        public ulong StartLocalLogicTick => Instance != null ? Instance.StartLocalLogicTick : 0;
        public ActionPhase Phase => Instance != null ? Instance.Phase : ActionPhase.Ended;
        public ActionState State => Instance != null ? Instance.State : ActionState.Ended;
        public ActionTargetSnapshot TargetSnapshot => Instance != null ? Instance.TargetSnapshot : ActionTargetSnapshot.None;
        public ActionInstanceHandle ToHandle()
        {
            return ActionInstanceHandle.From(Instance);
        }
    }
}
