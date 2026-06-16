namespace ThirdPersonCharacterStateMachine
{
    public readonly struct StateGraphSnapshot
    {
        public StateGraphSnapshot(
            StateGraphNodeId activeNodeId,
            float stateTime,
            string variantId,
            string pendingTransitionPath)
        {
            ActiveNodeId = activeNodeId;
            ActivePath = activeNodeId.Value;
            StateTime = stateTime < 0f ? 0f : stateTime;
            VariantId = variantId ?? string.Empty;
            PendingTransitionPath = pendingTransitionPath ?? string.Empty;
        }

        public StateGraphNodeId ActiveNodeId { get; }
        public string ActivePath { get; }
        public float StateTime { get; }
        public string VariantId { get; }
        public string PendingTransitionPath { get; }
        public bool HasPendingTransition => !string.IsNullOrEmpty(PendingTransitionPath);
    }
}
