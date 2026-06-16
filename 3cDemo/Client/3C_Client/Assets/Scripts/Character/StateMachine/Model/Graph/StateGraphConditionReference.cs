namespace ThirdPersonCharacterStateMachine
{
    public readonly struct StateGraphConditionReference
    {
        public StateGraphConditionReference(
            string key,
            string requestKey,
            float numericParameter,
            int priorityParameter,
            string tagKey)
        {
            Key = key ?? string.Empty;
            RequestKey = requestKey ?? string.Empty;
            NumericParameter = numericParameter < 0f ? 0f : numericParameter;
            PriorityParameter = priorityParameter < 0 ? 0 : priorityParameter;
            TagKey = tagKey ?? string.Empty;
        }

        public string Key { get; }
        public string RequestKey { get; }
        public float NumericParameter { get; }
        public int PriorityParameter { get; }
        public string TagKey { get; }
        public bool HasKey => !string.IsNullOrWhiteSpace(Key);
    }
}
