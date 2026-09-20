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

        // 模块自治推进:内部状态机先跑当前状态逻辑、后判转移(与库时序一致),
        // 并自行维护 Control 语义槽位(active-state / entered-tick / transition)与 trace。
        void Tick(
            in CharacterControlTickContext context,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output);
    }

    public interface ICharacterControlReadPort
    {
        bool HasInputRequest(string requestId);
        bool IsAbilityActive(CharacterSkillId abilityId);
        bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId);
        bool IsAbilityCompleted(CharacterSkillId abilityId);
        ulong CompletedAbilityInstanceId(CharacterSkillId abilityId);
        bool IsAbilityWindowActive(CharacterSkillId abilityId, string windowType);
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

    public readonly struct CharacterControlAbilityRequest
    {
        public CharacterControlAbilityRequest(
            SimulationExecutionSource source,
            CharacterSkillId abilityId,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId = "",
            string targetKey = "",
            EquipmentActionContext equipmentContext = default,
            ulong replacementActionInstanceId = 0)
        {
            if (!source.IsCharacterControl || !abilityId.IsValid)
                throw new ArgumentException("Character control Ability request identity is incomplete.");
            Source = source;
            AbilityId = abilityId;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            TargetInputValueId = targetInputValueId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            EquipmentContext = equipmentContext;
            ReplacementActionInstanceId = replacementActionInstanceId;
        }

        public SimulationExecutionSource Source { get; }
        public CharacterSkillId AbilityId { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public ulong ReplacementActionInstanceId { get; }
    }

    public enum CharacterControlAbilityStopMode : byte
    {
        Graceful = 1,
        Force = 2
    }

    public readonly struct CharacterControlAbilityStopRequest
    {
        public CharacterControlAbilityStopRequest(
            SimulationExecutionSource source,
            CharacterSkillId abilityId,
            CharacterControlAbilityStopMode mode,
            string reason = "",
            ulong actionInstanceId = 0,
            string actionWindowType = "")
        {
            if (!source.IsCharacterControl || !abilityId.IsValid ||
                mode != CharacterControlAbilityStopMode.Graceful &&
                mode != CharacterControlAbilityStopMode.Force)
                throw new ArgumentException("Character control Ability stop request is incomplete.");
            Source = source;
            AbilityId = abilityId;
            Mode = mode;
            Reason = reason ?? string.Empty;
            ActionInstanceId = actionInstanceId;
            ActionWindowType = actionWindowType ?? string.Empty;
        }

        public SimulationExecutionSource Source { get; }
        public CharacterSkillId AbilityId { get; }
        public CharacterControlAbilityStopMode Mode { get; }
        public string Reason { get; }
        public ulong ActionInstanceId { get; }
        public string ActionWindowType { get; }
    }

    public readonly struct CharacterControlMotionRequest
    {
        public CharacterControlMotionRequest(
            SimulationExecutionSource source,
            string binding,
            SimulationInputValueId input,
            int continuousTicks,
            int phase,
            ulong playbackGeneration)
        {
            if (!source.IsCharacterControl || string.IsNullOrEmpty(binding) || !input.IsValid || continuousTicks < 0 || phase < 0)
                throw new ArgumentException("Character control motion request is incomplete.");
            if (playbackGeneration == 0)
                throw new ArgumentException("Character control motion request playback generation is invalid.", nameof(playbackGeneration));
            Source = source;
            Binding = SimulationIdentity.Require(binding, nameof(binding));
            Input = input;
            ContinuousTicks = continuousTicks;
            Phase = phase;
            PlaybackGeneration = playbackGeneration;
        }

        public SimulationExecutionSource Source { get; }
        public string Binding { get; }
        public SimulationInputValueId Input { get; }
        public int ContinuousTicks { get; }
        public int Phase { get; }
        public ulong PlaybackGeneration { get; }
    }

    public interface ICharacterControlOutputPort
    {
        void SubmitMotion(CharacterControlMotionRequest request);
        bool SubmitAbility(CharacterControlAbilityRequest request);
        void SubmitAbilityStop(CharacterControlAbilityStopRequest request);
        void Trace(SimulationExecutionSource source, string code, string detail, ulong generation);
    }
}
