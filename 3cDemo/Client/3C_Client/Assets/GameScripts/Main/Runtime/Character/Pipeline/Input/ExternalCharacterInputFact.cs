using System;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public readonly struct ExternalCharacterInputFact
    {
        public ExternalCharacterInputFact(
            ulong inputSequence,
            CharacterInputValue[] inputValues,
            CharacterInputRequest[] actionRequests)
        {
            InputSequence = inputSequence;
            InputValues = inputValues ?? Array.Empty<CharacterInputValue>();
            ActionRequests = actionRequests ?? Array.Empty<CharacterInputRequest>();
        }

        public ulong InputSequence { get; }
        public CharacterInputValue[] InputValues { get; }
        public CharacterInputRequest[] ActionRequests { get; }
        public bool IsValid => InputSequence != 0;
    }
}
