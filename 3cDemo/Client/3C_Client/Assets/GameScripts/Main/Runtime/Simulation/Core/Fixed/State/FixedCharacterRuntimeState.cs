using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityRuntimeState
    {
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;

        internal FixedAbilityRuntimeState(
            FixedGameplayAbilityExecutionInstallation installation,
            ulong lastCompletedTick,
            IDictionary<int, CharacterStateValue> stateValues,
            GameplayAbilityExecutionAggregate<CharacterStateValue> abilityExecutionState,
            IDictionary<int, FixedMotionWarpState> motionWarpStates)
        {
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            LastCompletedTick = lastCompletedTick;
            StateValues = CopyValues(stateValues);
            AbilityExecutionState = abilityExecutionState?.Clone() ??
                new GameplayAbilityExecutionAggregate<CharacterStateValue>();
            MotionWarpStates = motionWarpStates == null
                ? new Dictionary<int, FixedMotionWarpState>()
                : new Dictionary<int, FixedMotionWarpState>(motionWarpStates);
        }

        internal FixedGameplayAbilityExecutionInstallation Installation => m_Installation;
        internal Dictionary<int, CharacterStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<CharacterStateValue> AbilityExecutionState { get; }
        internal Dictionary<int, FixedMotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_Installation.Identity;
        public ulong LastCompletedTick { get; }

        internal FixedAbilityRuntimeState Clone(ulong lastCompletedTick) =>
            new FixedAbilityRuntimeState(
                m_Installation,
                lastCompletedTick,
                StateValues,
                AbilityExecutionState,
                MotionWarpStates);

        static Dictionary<int, CharacterStateValue> CopyValues(IDictionary<int, CharacterStateValue> values)
        {
            var result = new Dictionary<int, CharacterStateValue>();
            if (values == null)
                return result;
            foreach (KeyValuePair<int, CharacterStateValue> value in values)
            {
                if (value.Key < 0 || !result.TryAdd(value.Key, value.Value))
                    throw new ArgumentException("Character runtime state values are invalid or duplicated.", nameof(values));
            }
            return result;
        }
    }

    public sealed class FixedCharacterRuntimeState
    {
        readonly FixedGameplayAbilityExecutionInstallationSet m_Installations;
        readonly ReadOnlyCollection<FixedAbilityRuntimeState> m_Abilities;

        internal FixedCharacterRuntimeState(
            FixedGameplayAbilityExecutionInstallationSet installations,
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
            m_Installations = installations ?? throw new ArgumentNullException(nameof(installations));
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            LastCompletedTick = lastCompletedTick;
            var copied = abilities == null
                ? new List<FixedAbilityRuntimeState>()
                : new List<FixedAbilityRuntimeState>(abilities);
            copied.Sort((left, right) => left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId));
            if (copied.Count != m_Installations.Installations.Count)
                throw new ArgumentException("Character runtime state must contain one partition for every installed Ability.", nameof(abilities));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].AbilityIdentity.AbilityId == copied[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
                m_Installations.Require(copied[i].AbilityIdentity.AbilityId).Identity.Require(copied[i].AbilityIdentity);
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

        internal FixedGameplayAbilityExecutionInstallationSet Installations => m_Installations;

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
                    installation,
                    0,
                    null,
                    null,
                    null);
            }
            return new FixedCharacterRuntimeState(
                installations,
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
