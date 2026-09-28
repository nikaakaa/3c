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

        FixedAbilityRuntimeState(
            GameplayAbilityExecutionIdentity abilityIdentity,
            Dictionary<int, AbilityStateValue> stateValues,
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState,
            Dictionary<int, FixedMotionWarpState> motionWarpStates)
        {
            if (!abilityIdentity.IsValid)
                throw new ArgumentException("Fixed Ability runtime state identity is incomplete.", nameof(abilityIdentity));
            m_AbilityIdentity = abilityIdentity;
            StateValues = stateValues;
            AbilityExecutionState = abilityExecutionState;
            MotionWarpStates = motionWarpStates;
        }

        internal Dictionary<int, AbilityStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<AbilityStateValue> AbilityExecutionState { get; }
        internal Dictionary<int, FixedMotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_AbilityIdentity;

        internal FixedAbilityRuntimeState Clone() =>
            Adopt(
                m_AbilityIdentity,
                StateValues,
                AbilityExecutionState,
                MotionWarpStates);

        internal static FixedAbilityRuntimeState Adopt(
            GameplayAbilityExecutionIdentity abilityIdentity,
            Dictionary<int, AbilityStateValue> stateValues,
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState,
            Dictionary<int, FixedMotionWarpState> motionWarpStates)
        {
            if (stateValues == null)
                throw new ArgumentNullException(nameof(stateValues));
            if (abilityExecutionState == null)
                throw new ArgumentNullException(nameof(abilityExecutionState));
            if (motionWarpStates == null)
                throw new ArgumentNullException(nameof(motionWarpStates));
            foreach (int slotIndex in stateValues.Keys)
                if (slotIndex < 0)
                    throw new ArgumentException("Ability runtime state values are invalid.", nameof(stateValues));
            foreach (int operation in motionWarpStates.Keys)
                if (operation < 0)
                    throw new ArgumentException("Ability runtime MotionWarp states are invalid.", nameof(motionWarpStates));
            return new FixedAbilityRuntimeState(
                abilityIdentity,
                stateValues,
                abilityExecutionState,
                motionWarpStates);
        }

        static Dictionary<int, AbilityStateValue> CopyValues(IDictionary<int, AbilityStateValue> values)
        {
            var result = new Dictionary<int, AbilityStateValue>(values?.Count ?? 0);
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
        readonly FixedAbilityRuntimeState[] m_Abilities;
        readonly ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> m_TimelineSnapshots;

        static readonly Comparison<AbilityTimelineRuntimeSnapshot> s_CompareTimelineSnapshots =
            (left, right) => left.RuntimeHandle.CompareTo(right.RuntimeHandle);

        internal FixedCharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            IEnumerable<FixedAbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<FixedActionInstanceState> actionInstances,
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            IEnumerable<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
            : this(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                lastCompletedTick,
                abilities,
                actionActivationRequests,
                actionInstances,
                inputRequests,
                eventSequence,
                actionEventSequence,
                handleAllocator,
                controlState,
                gameplayEffectState,
                equipmentState,
                timelineSnapshots,
                ownsInputRequests: false)
        {
        }

        internal FixedCharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            IEnumerable<FixedAbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<FixedActionInstanceState> actionInstances,
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            IEnumerable<AbilityTimelineRuntimeSnapshot> timelineSnapshots,
            bool ownsInputRequests)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            LastCompletedTick = lastCompletedTick;
            FixedAbilityRuntimeState[] copied = CopyArray(abilities);
            Array.Sort(copied, CompareAbilities);
            for (int i = 0; i < copied.Length; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].AbilityIdentity.AbilityId == copied[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
            }
            m_Abilities = copied;
            ActionActivationRequests = CopyArray(actionActivationRequests);
            ActionInstances = CopyArray(actionInstances);
            InputRequests = inputRequests == null
                ? Array.Empty<KeyValuePair<string, SimulationInputRequestState>>()
                : ownsInputRequests
                    ? inputRequests
                    : CopyArray(inputRequests);
            Array.Sort(InputRequests, InputRequestKeyComparer.Instance);
            for (int i = 1; i < InputRequests.Length; i++)
            {
                if (InputRequests[i].Key == null ||
                    string.CompareOrdinal(InputRequests[i - 1].Key, InputRequests[i].Key) >= 0)
                    throw new ArgumentException("Character runtime state Input request identities are null, duplicated, or not canonically ordered.", nameof(inputRequests));
            }
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
                if (!snapshots[i].IsValid || i > 0 && snapshots[i - 1].RuntimeHandle == snapshots[i].RuntimeHandle)
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
        internal FixedAbilityRuntimeState[] AbilitiesArray => m_Abilities;
        internal SimulationActionActivationRequestState[] ActionActivationRequests { get; }
        internal FixedActionInstanceState[] ActionInstances { get; }
        internal KeyValuePair<string, SimulationInputRequestState>[] InputRequests { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }
        internal IReadOnlyList<AbilityTimelineRuntimeSnapshot> TimelineSnapshots => m_TimelineSnapshots;

        internal FixedCharacterRuntimeState WithTimelineSnapshot(AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                throw new ArgumentException("Fixed Character Timeline snapshot is invalid.", nameof(snapshot));
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots.Count + 1);
            for (int i = 0; i < m_TimelineSnapshots.Count; i++)
                if (m_TimelineSnapshots[i].RuntimeHandle != snapshot.RuntimeHandle)
                    snapshots.Add(m_TimelineSnapshots[i]);
            snapshots.Add(snapshot);
            return CloneWithTimelineSnapshots(snapshots);
        }

        internal FixedCharacterRuntimeState WithoutTimelineSnapshot(int runtimeHandle)
        {
            if (runtimeHandle == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeHandle));
            int removedIndex = -1;
            for (int i = 0; i < m_TimelineSnapshots.Count; i++)
            {
                if (m_TimelineSnapshots[i].RuntimeHandle == runtimeHandle)
                {
                    removedIndex = i;
                    break;
                }
            }
            if (removedIndex < 0)
                return this;
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots.Count - 1);
            for (int i = 0; i < m_TimelineSnapshots.Count; i++)
                if (i != removedIndex)
                    snapshots.Add(m_TimelineSnapshots[i]);
            return CloneWithTimelineSnapshots(snapshots);
        }

        internal FixedCharacterRuntimeState WithoutUnownedTerminalTimelines()
        {
            int firstRemoved = -1;
            for (int index = 0; index < m_TimelineSnapshots.Count; index++)
                if (!RetainTimelineSnapshot(m_TimelineSnapshots[index]))
                {
                    firstRemoved = index;
                    break;
                }
            if (firstRemoved < 0)
                return this;
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots.Count - 1);
            for (int index = 0; index < m_TimelineSnapshots.Count; index++)
                if (index < firstRemoved || index > firstRemoved && RetainTimelineSnapshot(m_TimelineSnapshots[index]))
                    snapshots.Add(m_TimelineSnapshots[index]);
            return CloneWithTimelineSnapshots(snapshots);
        }

        bool RetainTimelineSnapshot(in AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (snapshot.State != AbilityTimelineSnapshotState.Completed && snapshot.State != AbilityTimelineSnapshotState.Stopped)
                return true;
            for (int index = 0; index < ActionInstances.Length; index++)
            {
                FixedActionInstanceState action = ActionInstances[index];
                if (action.IsActive && action.InstanceId == snapshot.ActionContext.InstanceId &&
                    action.SkillExecutionGeneration == snapshot.ActionContext.SkillExecutionGeneration)
                    return true;
            }
            return false;
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
        internal static FixedCharacterRuntimeState AdoptSnapshot(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            FixedAbilityRuntimeState[] abilities,
            SimulationActionActivationRequestState[] actionActivationRequests,
            FixedActionInstanceState[] actionInstances,
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            IReadOnlyList<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
        {
            if (abilities == null)
                throw new ArgumentNullException(nameof(abilities));
            if (actionActivationRequests == null)
                throw new ArgumentNullException(nameof(actionActivationRequests));
            if (actionInstances == null)
                throw new ArgumentNullException(nameof(actionInstances));
            if (inputRequests == null)
                throw new ArgumentNullException(nameof(inputRequests));
            Array.Sort(abilities, CompareAbilities);
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null || i > 0 && abilities[i - 1].AbilityIdentity.AbilityId == abilities[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
            }
            Array.Sort(inputRequests, InputRequestKeyComparer.Instance);
            for (int i = 1; i < inputRequests.Length; i++)
            {
                if (inputRequests[i].Key == null ||
                    string.CompareOrdinal(inputRequests[i - 1].Key, inputRequests[i].Key) >= 0)
                    throw new ArgumentException("Character runtime state Input request identities are null, duplicated, or not canonically ordered.", nameof(inputRequests));
            }
            return new FixedCharacterRuntimeState(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                lastCompletedTick,
                abilities,
                actionActivationRequests,
                actionInstances,
                inputRequests,
                eventSequence,
                actionEventSequence,
                handleAllocator,
                controlState,
                gameplayEffectState,
                equipmentState,
                timelineSnapshots ?? Array.Empty<AbilityTimelineRuntimeSnapshot>());
        }

        internal static FixedCharacterRuntimeState AdoptPrepared(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            FixedAbilityRuntimeState[] abilities,
            SimulationActionActivationRequestState[] actionActivationRequests,
            FixedActionInstanceState[] actionInstances,
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            AbilityTimelineRuntimeSnapshot[] timelineSnapshots)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            Array.Sort(abilities, CompareAbilities);
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null || i > 0 && abilities[i - 1].AbilityIdentity.AbilityId == abilities[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.");
            }
            Array.Sort(inputRequests, InputRequestKeyComparer.Instance);
            for (int i = 1; i < inputRequests.Length; i++)
            {
                if (inputRequests[i].Key == null ||
                    string.CompareOrdinal(inputRequests[i - 1].Key, inputRequests[i].Key) >= 0)
                    throw new ArgumentException("Character runtime state Input request identities are null, duplicated, or not canonically ordered.");
            }
            Array.Sort(timelineSnapshots, s_CompareTimelineSnapshots);
            for (int i = 0; i < timelineSnapshots.Length; i++)
            {
                if (!timelineSnapshots[i].IsValid || i > 0 && timelineSnapshots[i - 1].RuntimeHandle == timelineSnapshots[i].RuntimeHandle)
                    throw new ArgumentException("Character runtime state Timeline snapshots are missing or duplicated.");
            }
            return new FixedCharacterRuntimeState(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                lastCompletedTick,
                abilities,
                actionActivationRequests,
                actionInstances,
                inputRequests,
                eventSequence,
                actionEventSequence,
                handleAllocator,
                controlState,
                gameplayEffectState,
                equipmentState,
                new ReadOnlyCollection<AbilityTimelineRuntimeSnapshot>(timelineSnapshots));
        }

        private FixedCharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            FixedAbilityRuntimeState[] abilities,
            SimulationActionActivationRequestState[] actionActivationRequests,
            FixedActionInstanceState[] actionInstances,
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            IReadOnlyList<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            LastCompletedTick = lastCompletedTick;
            m_Abilities = abilities;
            ActionActivationRequests = actionActivationRequests;
            ActionInstances = actionInstances;
            InputRequests = inputRequests;
            EventSequence = eventSequence;
            ActionEventSequence = actionEventSequence;
            HandleAllocator = handleAllocator;
            ControlState = controlState;
            GameplayEffectState = gameplayEffectState;
            EquipmentState = equipmentState;
            m_TimelineSnapshots = timelineSnapshots as ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> ??
                new List<AbilityTimelineRuntimeSnapshot>(timelineSnapshots).AsReadOnly();
        }

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
                Array.Empty<SimulationActionActivationRequestState>(),
                Array.Empty<FixedActionInstanceState>(),
                Array.Empty<KeyValuePair<string, SimulationInputRequestState>>(),
                0,
                0,
                0,
                controlState,
                gameplayEffectState,
                equipmentState,
                Array.Empty<AbilityTimelineRuntimeSnapshot>());
        }

        static T[] CopyArray<T>(IEnumerable<T> values)
        {
            if (values == null || values is ICollection<T> collection && collection.Count == 0)
                return Array.Empty<T>();
            int count = values is ICollection<T> known ? known.Count : 0;
            var result = new T[count];
            int index = 0;
            if (values != null)
            {
                foreach (T value in values)
                {
                    if (index == result.Length)
                        Array.Resize(ref result, Math.Max(4, result.Length * 2));
                    result[index++] = value;
                }
            }
            if (index != result.Length)
                Array.Resize(ref result, index);
            return result;
        }

        static int CompareAbilities(FixedAbilityRuntimeState left, FixedAbilityRuntimeState right) =>
            left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId);
    }
}
