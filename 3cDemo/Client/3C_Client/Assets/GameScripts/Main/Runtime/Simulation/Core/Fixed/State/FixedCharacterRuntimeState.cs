using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityRuntimeState
    {
        readonly GameplayAbilityExecutionIdentity m_AbilityIdentity;

        internal FixedAbilityRuntimeState(
            GameplayAbilityExecutionIdentity abilityIdentity,
            IDictionary<int, AbilityStateValue> stateValues,
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState,
            IDictionary<int, FixedMotionWarpState> motionWarpStates)
        {
            if (!abilityIdentity.IsValid)
                throw new ArgumentException("Fixed Ability runtime state identity is incomplete.", nameof(abilityIdentity));
            m_AbilityIdentity = abilityIdentity;
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

        internal FixedAbilityRuntimeState Clone() =>
            new FixedAbilityRuntimeState(
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

    public sealed class FixedCharacterRuntimeState
    {
        readonly ReadOnlyCollection<FixedAbilityRuntimeState> m_Abilities;
        readonly ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> m_TimelineSnapshots;

        internal FixedCharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
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
            EquipmentStateAggregate equipmentState,
            IEnumerable<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
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
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(timelineSnapshots ?? Array.Empty<AbilityTimelineRuntimeSnapshot>());
            snapshots.Sort((left, right) => left.RuntimeHandle.CompareTo(right.RuntimeHandle));
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i] == null || snapshots[i].RuntimeHandle == 0 || i > 0 && snapshots[i - 1].RuntimeHandle == snapshots[i].RuntimeHandle)
                    throw new ArgumentException("Character runtime state Timeline snapshots are missing or duplicated.", nameof(timelineSnapshots));
            }
            m_TimelineSnapshots = snapshots.AsReadOnly();
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public ulong LastCompletedTick { get; }

        public bool TryGetEquipmentState(out EquipmentStateAggregate equipment)
        {
            equipment = EquipmentState;
            return equipment != null;
        }
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
        internal IReadOnlyList<AbilityTimelineRuntimeSnapshot> TimelineSnapshots => m_TimelineSnapshots;

        internal FixedCharacterRuntimeState WithTimelineSnapshot(AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RuntimeHandle == 0)
                throw new ArgumentException("Fixed Character Timeline snapshot is invalid.", nameof(snapshot));
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots);
            snapshots.RemoveAll(value => value.RuntimeHandle == snapshot.RuntimeHandle);
            snapshots.Add(snapshot);
            return CloneWithTimelineSnapshots(snapshots);
        }

        internal FixedCharacterRuntimeState WithoutTimelineSnapshot(int runtimeHandle)
        {
            if (runtimeHandle == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeHandle));
            if (m_TimelineSnapshots.All(value => value.RuntimeHandle != runtimeHandle))
                return this;
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots);
            snapshots.RemoveAll(value => value.RuntimeHandle == runtimeHandle);
            return CloneWithTimelineSnapshots(snapshots);
        }

        FixedCharacterRuntimeState CloneWithTimelineSnapshots(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots) =>
            new FixedCharacterRuntimeState(
                NumericProfile,
                GameplayContentHash,
                StateSchemaHash,
                LastCompletedTick,
                m_Abilities,
                ActionActivationRequests,
                ActionInstances,
                InputRequests,
                EventSequence,
                ActionEventSequence,
                HandleAllocator,
                ControlState,
                GameplayEffectState,
                EquipmentState,
                snapshots);
        internal static FixedCharacterRuntimeState CreateInitial(
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
            var abilities = new FixedAbilityRuntimeState[identities.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                abilities[i] = new FixedAbilityRuntimeState(
                    identities[i],
                    null,
                    null,
                    null);
            }
            return new FixedCharacterRuntimeState(
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
                equipmentState,
                null);
        }
    }
}
