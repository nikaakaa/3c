using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityRuntimeState
    {
        readonly GameplayAbilityExecutionIdentity m_AbilityIdentity;

        internal FixedAbilityRuntimeState(
            GameplayAbilityExecutionIdentity abilityIdentity,
            ulong lastCompletedTick,
            IDictionary<int, AbilityStateValue> stateValues,
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState,
            IDictionary<int, FixedMotionWarpState> motionWarpStates)
        {
            if (!abilityIdentity.IsValid)
                throw new ArgumentException("Fixed Ability runtime state identity is incomplete.", nameof(abilityIdentity));
            m_AbilityIdentity = abilityIdentity;
            LastCompletedTick = lastCompletedTick;
            StateValues = CopyValues(stateValues);
            AbilityExecutionState = abilityExecutionState?.Clone() ??
                new GameplayAbilityExecutionAggregate<AbilityStateValue>();
            MotionWarpStates = motionWarpStates == null
                ? new Dictionary<int, FixedMotionWarpState>()
                : new Dictionary<int, FixedMotionWarpState>(motionWarpStates);
        }

        internal Dictionary<int, AbilityStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<AbilityStateValue> AbilityExecutionState { get; }
        internal Dictionary<int, FixedMotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_AbilityIdentity;
        public ulong LastCompletedTick { get; }

        internal FixedAbilityRuntimeState Clone(ulong lastCompletedTick) =>
            new FixedAbilityRuntimeState(
                m_AbilityIdentity,
                lastCompletedTick,
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

    public sealed class FixedCharacterRuntimeState
    {
        readonly ReadOnlyCollection<FixedAbilityRuntimeState> m_Abilities;

        internal FixedCharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            ulong lastCompletedTick,
            IEnumerable<FixedAbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<FixedActionInstanceState> actionInstances,
            IReadOnlyDictionary<string, SimulationInputRequestState> inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            LastCompletedTick = lastCompletedTick;
            var copied = abilities == null
                ? new List<FixedAbilityRuntimeState>()
                : new List<FixedAbilityRuntimeState>(abilities);
            copied.Sort((left, right) => left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].AbilityIdentity.AbilityId == copied[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
            }
            m_Abilities = copied.AsReadOnly();
            ActionActivationRequests = new List<SimulationActionActivationRequestState>(
                actionActivationRequests ?? Array.Empty<SimulationActionActivationRequestState>());
            ActionInstances = new List<FixedActionInstanceState>(
                actionInstances ?? Array.Empty<FixedActionInstanceState>());
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

        internal FixedAbilityRuntimeState RequireAbility(CharacterSkillId abilityId)
        {
            for (int i = 0; i < m_Abilities.Count; i++)
                if (m_Abilities[i].AbilityIdentity.AbilityId == abilityId)
                    return m_Abilities[i];
            throw new InvalidOperationException($"Character runtime state Ability '{abilityId}' is missing.");
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<FixedAbilityRuntimeState> Abilities => m_Abilities;
        internal List<SimulationActionActivationRequestState> ActionActivationRequests { get; }
        internal List<FixedActionInstanceState> ActionInstances { get; }
        internal Dictionary<string, SimulationInputRequestState> InputRequests { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }

        internal static FixedCharacterRuntimeState CreateInitial(
            FixedGameplayAbilityExecutionInstallationSet installations,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (installations == null)
                throw new ArgumentNullException(nameof(installations));
            var abilities = new FixedAbilityRuntimeState[installations.Installations.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                FixedGameplayAbilityExecutionInstallation installation = installations.Installations[i];
                abilities[i] = new FixedAbilityRuntimeState(
                    installation.Identity,
                    0,
                    null,
                    null,
                    null);
            }
            return new FixedCharacterRuntimeState(
                numericProfile,
                gameplayContentHash,
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
