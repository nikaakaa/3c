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

    public sealed class Float32CharacterRuntimeState
    {
        readonly Float32AbilityRuntimeState[] m_Abilities;
        readonly ReadOnlyCollection<AbilityTimelineRuntimeSnapshot> m_TimelineSnapshots;

        internal Float32CharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            IEnumerable<Float32AbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<Float32ActionInstanceState> actionInstances,
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

        internal Float32CharacterRuntimeState(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            ulong lastCompletedTick,
            IEnumerable<Float32AbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<Float32ActionInstanceState> actionInstances,
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
            Float32AbilityRuntimeState[] copied = CopyArray(abilities);
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
        public IReadOnlyList<Float32AbilityRuntimeState> Abilities => m_Abilities;
        internal SimulationActionActivationRequestState[] ActionActivationRequests { get; }
        internal Float32ActionInstanceState[] ActionInstances { get; }
        internal KeyValuePair<string, SimulationInputRequestState>[] InputRequests { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }
        internal IReadOnlyList<AbilityTimelineRuntimeSnapshot> TimelineSnapshots => m_TimelineSnapshots;

        internal Float32CharacterRuntimeState WithTimelineSnapshot(AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                throw new ArgumentException("Float32 Character Timeline snapshot is invalid.", nameof(snapshot));
            var snapshots = new List<AbilityTimelineRuntimeSnapshot>(m_TimelineSnapshots.Count + 1);
            for (int i = 0; i < m_TimelineSnapshots.Count; i++)
                if (m_TimelineSnapshots[i].RuntimeHandle != snapshot.RuntimeHandle)
                    snapshots.Add(m_TimelineSnapshots[i]);
            snapshots.Add(snapshot);
            return CloneWithTimelineSnapshots(snapshots);
        }

        internal Float32CharacterRuntimeState WithoutTimelineSnapshot(int runtimeHandle)
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

        internal Float32CharacterRuntimeState WithoutUnownedTerminalTimelines()
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
                Float32ActionInstanceState action = ActionInstances[index];
                if (action.IsActive && action.InstanceId == snapshot.ActionContext.InstanceId &&
                    action.SkillExecutionGeneration == snapshot.ActionContext.SkillExecutionGeneration)
                    return true;
            }
            return false;
        }

        Float32CharacterRuntimeState CloneWithTimelineSnapshots(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots) =>
            new Float32CharacterRuntimeState(
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
                equipmentState,
                null);
        }

        static T[] CopyArray<T>(IEnumerable<T> values)
        {
            int count = values is ICollection<T> collection ? collection.Count : 0;
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

        static int CompareAbilities(Float32AbilityRuntimeState left, Float32AbilityRuntimeState right) =>
            left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId);
    }
}
