namespace ThirdPersonSimulation.Fixed
{
    public readonly struct FixedBlackboardWriteCommand
    {
        public FixedBlackboardWriteCommand(GameplayAbilityExecutionIdentity ability, ulong actionInstanceId,
            ulong skillGeneration, int stateSlot, BlackboardOwnerToken owner, AbilityStateValue value)
        {
            Ability = ability;
            ActionInstanceId = actionInstanceId;
            SkillGeneration = skillGeneration;
            StateSlot = stateSlot;
            Owner = owner;
            Value = value;
        }

        public GameplayAbilityExecutionIdentity Ability { get; }
        public ulong ActionInstanceId { get; }
        public ulong SkillGeneration { get; }
        public int StateSlot { get; }
        public BlackboardOwnerToken Owner { get; }
        public AbilityStateValue Value { get; }
    }

    public enum FixedBlackboardWriteStatus : byte
    {
        Applied = 1,
        InstanceEnded = 2,
        ScopeEnded = 3,
        ScopeChanged = 4
    }

    public readonly struct FixedBlackboardWriteResult
    {
        internal FixedBlackboardWriteResult(ulong sequence, FixedBlackboardWriteCommand command, ulong tick,
            FixedBlackboardWriteStatus status)
        {
            Sequence = sequence;
            Command = command;
            Tick = tick;
            Status = status;
        }

        public ulong Sequence { get; }
        public FixedBlackboardWriteCommand Command { get; }
        public ulong Tick { get; }
        public FixedBlackboardWriteStatus Status { get; }
    }
}
