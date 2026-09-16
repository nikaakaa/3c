using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    public sealed class Float32AbilityRuntimeState
    {
        readonly GameplayAbilityExecutionIdentity m_AbilityIdentity;

        internal Float32AbilityRuntimeState(
            GameplayAbilityExecutionIdentity abilityIdentity,
            IDictionary<int, AbilityStateValue> stateValues,
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState,
            IDictionary<int, Float32MotionWarpState> motionWarpStates)
        {
            if (!abilityIdentity.IsValid)
                throw new ArgumentException("Float32 Ability runtime state identity is incomplete.", nameof(abilityIdentity));
            m_AbilityIdentity = abilityIdentity;
            StateValues = CopyValues(stateValues);
            AbilityExecutionState = abilityExecutionState?.Clone() ??
                new GameplayAbilityExecutionAggregate<AbilityStateValue>();
            MotionWarpStates = motionWarpStates == null
                ? new Dictionary<int, Float32MotionWarpState>()
                : new Dictionary<int, Float32MotionWarpState>(motionWarpStates);
        }

        internal Dictionary<int, AbilityStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<AbilityStateValue> AbilityExecutionState { get; }
        internal Dictionary<int, Float32MotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_AbilityIdentity;

        internal Float32AbilityRuntimeState Clone() =>
            new Float32AbilityRuntimeState(
                m_AbilityIdentity,
                StateValues,
                AbilityExecutionState,
                MotionWarpStates);

        static Dictionary<int, AbilityStateValue> CopyValues(IDictionary<int, AbilityStateValue> values)
        {
            var result = new Dictionary<int, AbilityStateValue>();
            if (values == null)
                return result;
            foreach (KeyValuePair<int, AbilityStateValue> value in values)
            {
                if (value.Key < 0 || !result.TryAdd(value.Key, value.Value))
                    throw new ArgumentException("Ability runtime state values are invalid or duplicated.", nameof(values));
            }
            return result;
        }
    }

    public sealed class Float32CharacterRuntimeState
    {
        readonly ReadOnlyCollection<Float32AbilityRuntimeState> m_Abilities;

        internal Float32CharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            IEnumerable<Float32AbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<Float32ActionInstanceState> actionInstances,
            IReadOnlyDictionary<string, SimulationInputRequestState> inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            LastCompletedTick = lastCompletedTick;
            var copied = abilities == null
                ? new List<Float32AbilityRuntimeState>()
                : new List<Float32AbilityRuntimeState>(abilities);
            copied.Sort((left, right) => left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].AbilityIdentity.AbilityId == copied[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
            }
            m_Abilities = copied.AsReadOnly();
            ActionActivationRequests = new List<SimulationActionActivationRequestState>(
                actionActivationRequests ?? Array.Empty<SimulationActionActivationRequestState>());
            ActionInstances = new List<Float32ActionInstanceState>(
                actionInstances ?? Array.Empty<Float32ActionInstanceState>());
            InputRequests = inputRequests == null
                ? new Dictionary<string, SimulationInputRequestState>(StringComparer.Ordinal)
                : new Dictionary<string, SimulationInputRequestState>(inputRequests, StringComparer.Ordinal);
            EventSequence = eventSequence;
            ActionEventSequence = actionEventSequence;
            HandleAllocator = handleAllocator;
            ControlState = controlState;
            GameplayEffectState = gameplayEffectState;
            EquipmentState = equipmentState;
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<Float32AbilityRuntimeState> Abilities => m_Abilities;
        internal List<SimulationActionActivationRequestState> ActionActivationRequests { get; }
        internal List<Float32ActionInstanceState> ActionInstances { get; }
        internal Dictionary<string, SimulationInputRequestState> InputRequests { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }

        internal static Float32CharacterRuntimeState CreateInitial(
            IEnumerable<GameplayAbilityExecutionIdentity> abilityIdentities,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (abilityIdentities == null)
                throw new ArgumentNullException(nameof(abilityIdentities));
            var identities = new List<GameplayAbilityExecutionIdentity>(abilityIdentities);
            var abilities = new Float32AbilityRuntimeState[identities.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                abilities[i] = new Float32AbilityRuntimeState(
                    identities[i],
                    null,
                    null,
                    null);
            }
            return new Float32CharacterRuntimeState(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                0,
                abilities,
                null,
                null,
                null,
                0,
                0,
                0,
                controlState,
                gameplayEffectState,
                equipmentState);
        }
    }
}
