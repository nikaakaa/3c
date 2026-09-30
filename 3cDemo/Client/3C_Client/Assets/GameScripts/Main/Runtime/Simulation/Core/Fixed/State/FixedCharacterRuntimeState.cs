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

    public sealed class FixedCharacterRuntimeState : ICommittedCharacterControlState
    {
        readonly FixedAbilityRuntimeState[] m_Abilities;
        readonly ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> m_TimelineSnapshots;

        static readonly ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> s_EmptyTimelineSnapshots =
            new ReadOnlyCollection<AbilityTimelineRuntimeSnapshot>(Array.Empty<AbilityTimelineRuntimeSnapshot>());

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
        public CharacterControlRuntimeState ControlState { get; }

        public bool IsAbilityActive(CharacterSkillId abilityId)
        {
            for (int i = 0; i < ActionInstances.Length; i++)
                if (ActionInstances[i].IsActive && ActionInstances[i].SkillId == abilityId)
                    return true;
            return false;
        }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }
        public IReadOnlyList<AbilityTimelineRuntimeSnapshot> TimelineSnapshots => m_TimelineSnapshots;
        internal ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> TimelineSnapshotCollection => m_TimelineSnapshots;

        internal FixedCharacterRuntimeState WithTimelineOutputs(
            IAbilityTimelineRuntime timelineRuntime,
            List<AbilityTimelineAdvancePending> advances,
            List<AbilityTimelineStopPending> stops,
            List<AbilityTimelineRuntimeSnapshot> snapshots)
        {
            snapshots.Clear();
            for (int i = 0; i < m_TimelineSnapshots.Count; i++)
                snapshots.Add(m_TimelineSnapshots[i]);
            bool changed = advances.Count != 0;
            for (int i = 0; i < advances.Count; i++)
            {
                AbilityTimelineRuntimeSnapshot snapshot = timelineRuntime.Capture(advances[i].RuntimeHandle);
                int replacedIndex = -1;
                for (int index = 0; index < snapshots.Count; index++)
                {
                    if (snapshots[index].RuntimeHandle == snapshot.RuntimeHandle)
                    {
                        replacedIndex = index;
                        break;
                    }
                }
                if (replacedIndex >= 0)
                    snapshots.RemoveAt(replacedIndex);
                snapshots.Add(snapshot);
            }
            for (int i = 0; i < stops.Count; i++)
                for (int index = 0; index < snapshots.Count; index++)
                {
                    if (snapshots[index].RuntimeHandle != stops[i].RuntimeHandle)
                        continue;
                    snapshots.RemoveAt(index);
                    changed = true;
                    break;
                }
            int retainedCount = 0;
            for (int index = 0; index < snapshots.Count; index++)
            {
                AbilityTimelineRuntimeSnapshot snapshot = snapshots[index];
                if (RetainTimelineSnapshot(snapshot))
                    snapshots[retainedCount++] = snapshot;
            }
            if (retainedCount != snapshots.Count)
            {
                snapshots.RemoveRange(retainedCount, snapshots.Count - retainedCount);
                changed = true;
            }
            if (!changed)
                return this;
            ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> committedSnapshots = snapshots.Count == 0
                ? s_EmptyTimelineSnapshots
                : new ReadOnlyCollection<AbilityTimelineRuntimeSnapshot>(snapshots.ToArray());
            return new FixedCharacterRuntimeState(
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
                committedSnapshots);
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
            ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
        {
            if (abilities == null)
                throw new ArgumentNullException(nameof(abilities));
            if (actionActivationRequests == null)
                throw new ArgumentNullException(nameof(actionActivationRequests));
            if (actionInstances == null)
                throw new ArgumentNullException(nameof(actionInstances));
            if (inputRequests == null)
                throw new ArgumentNullException(nameof(inputRequests));
            Array.Sort(abilities, AbilityStateComparer.Instance);
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
                timelineSnapshots);
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
            Array.Sort(abilities, AbilityStateComparer.Instance);
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
            Array.Sort(timelineSnapshots, TimelineSnapshotComparer.Instance);
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
                timelineSnapshots.Length == 0
                    ? s_EmptyTimelineSnapshots
                    : new ReadOnlyCollection<AbilityTimelineRuntimeSnapshot>(timelineSnapshots));
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
            ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> timelineSnapshots)
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
            m_TimelineSnapshots = timelineSnapshots;
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
                s_EmptyTimelineSnapshots);
        }

        sealed class AbilityStateComparer : IComparer<FixedAbilityRuntimeState>
        {
            internal static readonly AbilityStateComparer Instance = new AbilityStateComparer();

            public int Compare(FixedAbilityRuntimeState left, FixedAbilityRuntimeState right) =>
                left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId);
        }

        sealed class TimelineSnapshotComparer : IComparer<AbilityTimelineRuntimeSnapshot>
        {
            internal static readonly TimelineSnapshotComparer Instance = new TimelineSnapshotComparer();

            public int Compare(AbilityTimelineRuntimeSnapshot left, AbilityTimelineRuntimeSnapshot right) =>
                left.RuntimeHandle.CompareTo(right.RuntimeHandle);
        }
    }
}
