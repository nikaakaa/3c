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
        bool HasInputRequest(string requestId);
        bool IsSkillActive(CharacterSkillId skillId);
        bool IsSkillCompleted(CharacterSkillId skillId);
        ulong CompletedSkillInstanceId(CharacterSkillId skillId);
        bool IsActionWindowActive(CharacterSkillId skillId, string windowType);
        bool TryReadEquipmentActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context);
        bool CompareInputVector2Magnitude(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison);
        bool CompareInputDirectionToBodyYaw(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison);
        bool IsInputDirectionBehindBodyYaw(SimulationInputValueId input);
    }

    public interface ICharacterControlStateReadPort
    {
        CharacterControlStateId ReadState(CharacterControlStateFieldId field);
        CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field);
        bool ReadBoolean(CharacterControlStateFieldId field);
        int ReadInt32(CharacterControlStateFieldId field);
        ulong ReadUInt64(CharacterControlStateFieldId field);
    }

    public interface ICharacterControlStatePort : ICharacterControlStateReadPort
    {
        void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value);
        void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value);
        void WriteBoolean(CharacterControlStateFieldId field, bool value);
        void WriteInt32(CharacterControlStateFieldId field, int value);
        void WriteUInt64(CharacterControlStateFieldId field, ulong value);
    }

    public readonly struct CharacterControlSkillRequest
    {
        public CharacterControlSkillRequest(
            SimulationExecutionSource source,
            CharacterSkillId skillId,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId = "",
            string targetKey = "",
            EquipmentActionContext equipmentContext = default)
        {
            if (!source.IsCharacterControl || !skillId.IsValid)
                throw new ArgumentException("Character control skill request identity is incomplete.");
            Source = source;
            SkillId = skillId;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            TargetInputValueId = targetInputValueId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            EquipmentContext = equipmentContext;
        }

        public SimulationExecutionSource Source { get; }
        public CharacterSkillId SkillId { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public EquipmentActionContext EquipmentContext { get; }
    }

    public enum CharacterControlSkillStopMode : byte
    {
        Graceful = 1,
        Force = 2
    }

    public readonly struct CharacterControlSkillStopRequest
    {
        public CharacterControlSkillStopRequest(
            SimulationExecutionSource source,
            CharacterSkillId skillId,
            CharacterControlSkillStopMode mode,
            string reason = "")
        {
            if (!source.IsCharacterControl || !skillId.IsValid || !Enum.IsDefined(typeof(CharacterControlSkillStopMode), mode))
                throw new ArgumentException("Character control skill stop request is incomplete.");
            Source = source;
            SkillId = skillId;
            Mode = mode;
            Reason = reason ?? string.Empty;
        }

        public SimulationExecutionSource Source { get; }
        public CharacterSkillId SkillId { get; }
        public CharacterControlSkillStopMode Mode { get; }
        public string Reason { get; }
    }

    public readonly struct CharacterControlMotionRequest
    {
        public CharacterControlMotionRequest(
            SimulationExecutionSource source,
            string binding,
            SimulationInputValueId input,
            int continuousTicks,
            int phase)
        {
            if (!source.IsCharacterControl || string.IsNullOrEmpty(binding) || !input.IsValid || continuousTicks < 0 || phase < 0)
                throw new ArgumentException("Character control motion request is incomplete.");
            Source = source;
            Binding = SimulationIdentity.Require(binding, nameof(binding));
            Input = input;
            ContinuousTicks = continuousTicks;
            Phase = phase;
        }

        public SimulationExecutionSource Source { get; }
        public string Binding { get; }
        public SimulationInputValueId Input { get; }
        public int ContinuousTicks { get; }
        public int Phase { get; }
    }

    public interface ICharacterControlOutputPort
    {
        void SubmitMotion(CharacterControlMotionRequest request);
        bool SubmitSkill(CharacterControlSkillRequest request);
        void SubmitSkillStop(CharacterControlSkillStopRequest request);
        void Trace(SimulationExecutionSource source, string code, string detail, ulong generation);
    }
}
