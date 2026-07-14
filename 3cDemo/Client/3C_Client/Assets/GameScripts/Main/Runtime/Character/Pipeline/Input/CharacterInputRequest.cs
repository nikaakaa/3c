namespace ThirdPersonCharacter.Pipeline.Input
{
    public struct CharacterInputRequest
    {
        public CharacterInputRequest(string requestId, ulong createdLocalLogicTick, ulong inputSequence, ulong expireLocalLogicTick, float bufferSeconds, int priority)
        {
            RequestId = requestId;
            CreatedLocalLogicTick = createdLocalLogicTick;
            InputSequence = inputSequence;
            ExpireLocalLogicTick = expireLocalLogicTick;
            BufferSeconds = bufferSeconds;
            Priority = priority;
            Consumed = false;
        }

        public string RequestId { get; }
        public ulong CreatedLocalLogicTick { get; }
        public ulong InputSequence { get; }
        public ulong ExpireLocalLogicTick { get; }
        public float BufferSeconds { get; }
        public int Priority { get; }
        public bool Consumed { get; private set; }

        public bool IsExpired(ulong localLogicTick)
        {
            return localLogicTick > ExpireLocalLogicTick;
        }

        public bool IsAvailable(ulong localLogicTick)
        {
            return !Consumed && !IsExpired(localLogicTick);
        }

        public void MarkConsumed()
        {
            Consumed = true;
        }
    }
}
