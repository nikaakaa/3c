using System;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterControlTickContext
    {
        public CharacterControlTickContext(ActorId actorId, SimulationTick tick, int tickRate)
        {
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Character control tick context is incomplete.");
            ActorId = actorId;
            Tick = tick;
            TickRate = tickRate;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public int TickRate { get; }
    }

    public interface ICharacterControlModule
    {
        CharacterControlModuleContract Contract { get; }
        void Tick(
            in CharacterControlTickContext context,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output);
    }

    public interface ICharacterControlReadPort
    {
        bool CompareInputVector2Magnitude(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison);
        bool CompareInputDirectionToBodyYaw(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison);
    }

    public interface ICharacterControlStatePort
    {
        CharacterControlStateId ReadState(CharacterControlStateFieldId field);
        void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value);
        CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field);
        void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value);
        int ReadInt32(CharacterControlStateFieldId field);
        void WriteInt32(CharacterControlStateFieldId field, int value);
        ulong ReadUInt64(CharacterControlStateFieldId field);
        void WriteUInt64(CharacterControlStateFieldId field, ulong value);
    }

    public readonly struct CharacterControlMotionRequest
    {
        public CharacterControlMotionRequest(
            CharacterControlMotionBindingId binding,
            SimulationInputValueId input,
            int continuousTicks,
            int phase)
        {
            if (!binding.IsValid || !input.IsValid || continuousTicks < 0 || phase < 0)
                throw new ArgumentException("Character control motion request is incomplete.");
            Binding = binding;
            Input = input;
            ContinuousTicks = continuousTicks;
            Phase = phase;
        }

        public CharacterControlMotionBindingId Binding { get; }
        public SimulationInputValueId Input { get; }
        public int ContinuousTicks { get; }
        public int Phase { get; }
    }

    public interface ICharacterControlOutputPort
    {
        void SubmitMotion(CharacterControlMotionRequest request);
    }
}
