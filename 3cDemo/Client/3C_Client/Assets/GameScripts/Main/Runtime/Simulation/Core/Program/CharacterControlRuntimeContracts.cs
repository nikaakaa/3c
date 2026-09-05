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
        void Enter(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output);
        void Tick(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output);
        bool EvaluateTransition(
            in CharacterControlTickContext context,
            CharacterControlTransitionId transitionId,
            ICharacterControlReadPort read,
            ICharacterControlStateReadPort state);
        void Exit(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
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

    public interface ICharacterControlStateReadPort
    {
        CharacterControlStateId ReadState(CharacterControlStateFieldId field);
        CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field);
        int ReadInt32(CharacterControlStateFieldId field);
        ulong ReadUInt64(CharacterControlStateFieldId field);
    }

    public interface ICharacterControlStatePort : ICharacterControlStateReadPort
    {
        void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value);
        void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value);
        void WriteInt32(CharacterControlStateFieldId field, int value);
        void WriteUInt64(CharacterControlStateFieldId field, ulong value);
    }

    public readonly struct CharacterControlMotionRequest
    {
        public CharacterControlMotionRequest(
            SimulationExecutionSource source,
            CharacterControlMotionBindingId binding,
            SimulationInputValueId input,
            int continuousTicks,
            int phase)
        {
            if (!source.IsValid || !binding.IsValid || !input.IsValid || continuousTicks < 0 || phase < 0)
                throw new ArgumentException("Character control motion request is incomplete.");
            Source = source;
            Binding = binding;
            Input = input;
            ContinuousTicks = continuousTicks;
            Phase = phase;
        }

        public SimulationExecutionSource Source { get; }
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
