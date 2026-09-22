using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionSavepoint
        : IFixedAbilityExecutionSavepoint
    {
        internal FixedAbilityExecutionSavepoint()
        {
        }

        internal FixedAbilityExecutionSavepoint Begin(
            int depth,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState,
            ulong handleAllocator,
            ulong eventSequence)
        {
            Depth = depth;
            GameplayEffectState = gameplayEffectState;
            EquipmentState = equipmentState;
            HandleAllocator = handleAllocator;
            EventSequence = eventSequence;
            return this;
        }

        internal void Clear()
        {
            Depth = 0;
            GameplayEffectState = null;
            EquipmentState = null;
            HandleAllocator = 0;
            EventSequence = 0;
        }

        public int Depth { get; private set; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; private set; }
        internal EquipmentStateAggregate EquipmentState { get; private set; }
        internal ulong HandleAllocator { get; private set; }
        internal ulong EventSequence { get; private set; }
    }

    internal sealed class FixedCharacterRuntimeStateTransaction : IFixedAbilityExecutionSavepointPort, IFixedControlRuntimeStatePort
    {
        FixedCharacterRuntimeState m_BaseState;
        Dictionary<CharacterSkillId, FixedAbilityRuntimeState> m_AbilityStates;
        Dictionary<CharacterSkillId, FixedSkillExecutionState> m_SkillStates;
        SimulationTick m_Tick;
        int m_TickRate;
        readonly Stack<FixedAbilityExecutionSavepoint> m_Savepoints =
            new Stack<FixedAbilityExecutionSavepoint>();
        readonly Stack<FixedAbilityExecutionSavepoint> m_SavepointPool =
            new Stack<FixedAbilityExecutionSavepoint>();
        FixedCharacterActionRuntimeState m_ActionState;
        FixedCharacterInputRequestState m_InputRequestState;
        FixedCharacterEventSequenceState m_EventSequenceState;
        FixedCharacterHandleAllocatorState m_HandleAllocatorState;
        FixedCharacterGameplayEffectRuntimeState m_GameplayEffectState;
        FixedCharacterEquipmentRuntimeState m_EquipmentState;
        CharacterControlRuntimeStateTransaction m_ControlState;
        bool m_ControlStateBound;
        bool m_AbilitiesChanged;
        bool m_Disposed;

        public FixedCharacterRuntimeStateTransaction()
        {
            m_Disposed = true;
        }

        public FixedCharacterRuntimeStateTransaction Restart(
            FixedCharacterRuntimeState baseState,
            SimulationTick tick,
            int tickRate,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog,
            FixedCharacterInputRequestState inputRequestState,
            FixedCharacterActionRuntimeState actionState,
            CharacterControlRuntimeStateTransaction controlState,
            FixedCharacterEventSequenceState eventSequenceState,
            FixedCharacterHandleAllocatorState handleAllocatorState,
            FixedCharacterGameplayEffectRuntimeState gameplayEffectState,
            FixedCharacterEquipmentRuntimeState equipmentState)
        {
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character runtime transaction timing is incomplete.");
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Fixed Character runtime transaction reuse found active savepoints.");
            m_Tick = tick;
            m_TickRate = tickRate;
            if (m_AbilityStates == null)
                m_AbilityStates = new Dictionary<CharacterSkillId, FixedAbilityRuntimeState>(baseState.Abilities.Count);
            if (m_SkillStates == null)
                m_SkillStates = new Dictionary<CharacterSkillId, FixedSkillExecutionState>(baseState.Abilities.Count);
            m_AbilityStates.Clear();
            for (int i = 0; i < baseState.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState state = baseState.Abilities[i];
                m_AbilityStates.Add(state.AbilityIdentity.AbilityId, state);
            }
            m_ActionState = actionState.Restart(
                baseState.ActionActivationRequests,
                baseState.ActionInstances,
                baseState.ActionEventSequence);
            m_InputRequestState = inputRequestState.Restart(m_Tick, baseState.InputRequests);
            m_EventSequenceState = eventSequenceState.Restart(baseState.EventSequence);
            m_HandleAllocatorState = handleAllocatorState.Restart(baseState.HandleAllocator);
            m_GameplayEffectState = gameplayEffectState.Restart(
                m_TickRate,
                gameplayEffectCatalog,
                baseState.GameplayEffectState);
            m_EquipmentState = equipmentState.Restart(baseState.EquipmentState);
            m_ControlState = controlState;
            m_ControlStateBound = false;
            m_AbilitiesChanged = false;
            m_Disposed = false;
            return this;
        }

        internal IFixedSkillExecutionState BindAbility(
            GameplayAbilityExecutionIdentity identity,
            GameplayAbilityExecutionLayout layout,
            FixedGameplayAbilityExecutionData ability)
        {
            RequireActive();
            if (!identity.IsValid)
                throw new ArgumentException("Fixed Ability execution identity is incomplete.", nameof(identity));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));
            if (ability.AbilityId != identity.AbilityId)
                throw new ArgumentException("Fixed Ability execution data does not match its identity.", nameof(ability));
            if (!m_AbilityStates.TryGetValue(identity.AbilityId, out FixedAbilityRuntimeState state))
                throw new InvalidOperationException($"Ability '{identity.AbilityId}' is not part of the Character runtime state.");
            if (!m_SkillStates.TryGetValue(identity.AbilityId, out FixedSkillExecutionState skillState))
            {
                skillState = new FixedSkillExecutionState(this, identity, layout, ability, state);
                m_SkillStates.Add(identity.AbilityId, skillState);
            }
            else
            {
                skillState.Restart(state);
            }
            return skillState;
        }

        public CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema)
        {
            RequireActive();
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (m_ControlStateBound)
                throw new InvalidOperationException("Fixed Character Control state is already bound.");
            if (m_BaseState.ControlState == null)
                throw new InvalidOperationException("Fixed Character runtime has no Control state.");
            m_ControlStateBound = true;
            return m_ControlState.Restart(
                m_BaseState.ControlState,
                schema,
                m_Tick);
        }

        internal void AcceptAbility(IFixedSkillExecutionState skillState)
        {
            RequireActive();
            if (!(skillState is FixedSkillExecutionState ability) ||
                !ReferenceEquals(ability.BindingIdentity, this))
            {
                throw new InvalidOperationException("Fixed Ability transaction belongs to another Character runtime transaction.");
            }
            FixedAbilityRuntimeState state = ability.TakeSnapshot();
            if (!ReferenceEquals(m_AbilityStates[state.AbilityIdentity.AbilityId], state))
                m_AbilitiesChanged = true;
            m_AbilityStates[state.AbilityIdentity.AbilityId] = state;
        }

        internal FixedCharacterRuntimeState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            return Snapshot();
        }

        internal IFixedInputRequestStatePort InputRequests => m_InputRequestState;
        internal IFixedActionRuntimeStatePort ActionState => m_ActionState;
        internal IFixedEventSequenceStatePort EventSequenceState => m_EventSequenceState;
        internal IFixedHandleAllocatorStatePort HandleAllocatorState => m_HandleAllocatorState;
        internal IFixedGameplayEffectStatePort GameplayEffectState => m_GameplayEffectState;
        internal IFixedEquipmentStatePort EquipmentState => m_EquipmentState;

        public IFixedAbilityExecutionSavepoint CreateSavepoint()
        {
            RequireActive();
            GameplayEffectStateAggregate gameplayEffectState = m_GameplayEffectState.Capture();
            EquipmentStateAggregate equipmentState = m_EquipmentState.Capture();
            FixedAbilityExecutionSavepoint savepoint = m_SavepointPool.Count > 0
                ? m_SavepointPool.Pop()
                : new FixedAbilityExecutionSavepoint();
            savepoint.Begin(
                m_Savepoints.Count + 1,
                gameplayEffectState,
                equipmentState,
                m_HandleAllocatorState.HandleAllocator,
                m_EventSequenceState.EventSequence);
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(IFixedAbilityExecutionSavepoint savepoint)
        {
            RequireActive();
            FixedAbilityExecutionSavepoint executionSavepoint = RequireTopSavepoint(savepoint);
            m_GameplayEffectState.Restore(executionSavepoint.GameplayEffectState);
            m_EquipmentState.Restore(executionSavepoint.EquipmentState);
            m_HandleAllocatorState.RestoreHandleAllocator(executionSavepoint.HandleAllocator);
            m_EventSequenceState.Restore(executionSavepoint.EventSequence);
            RecycleTopSavepoint();
        }

        public void Release(IFixedAbilityExecutionSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            RecycleTopSavepoint();
        }

        public int SavepointDepth => m_Savepoints.Count;

        public void Dispose()
        {
            if (m_Disposed)
                return;
            while (m_Savepoints.Count > 0)
                m_Savepoints.Pop().Clear();
            m_ControlState?.Dispose();
            m_InputRequestState.Dispose();
            m_ActionState.Dispose();
            m_EventSequenceState.Dispose();
            m_HandleAllocatorState.Dispose();
            m_GameplayEffectState.Dispose();
            m_EquipmentState.Dispose();
            m_Disposed = true;
        }

        FixedCharacterRuntimeState Snapshot()
        {
            FixedAbilityRuntimeState[] abilities;
            if (m_AbilitiesChanged)
            {
                abilities = new FixedAbilityRuntimeState[m_AbilityStates.Count];
                int abilityIndex = 0;
                foreach (KeyValuePair<CharacterSkillId, FixedAbilityRuntimeState> pair in m_AbilityStates)
                    abilities[abilityIndex++] = pair.Value;
            }
            else
            {
                abilities = m_BaseState.AbilitiesArray;
            }
            SimulationActionActivationRequestState[] activationRequests = m_ActionState.AreActionActivationRequestsUnchanged
                ? m_BaseState.ActionActivationRequests
                : ToArray(m_ActionState.GetActionActivationRequests());
            FixedActionInstanceState[] actionInstances = m_ActionState.AreActionInstancesUnchanged
                ? m_BaseState.ActionInstances
                : ToArray(m_ActionState.GetActionInstances());
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests = m_InputRequestState.IsUnchanged
                ? m_BaseState.InputRequests
                : m_InputRequestState.Capture();
            return FixedCharacterRuntimeState.AdoptSnapshot(
                m_BaseState.NumericProfile,
                m_BaseState.GameplayContentHash,
                m_BaseState.StateSchemaHash,
                m_Tick.Value,
                abilities,
                activationRequests,
                actionInstances,
                inputRequests,
                m_EventSequenceState.EventSequence,
                m_ActionState.ActionEventSequence,
                m_HandleAllocatorState.HandleAllocator,
                m_ControlState?.Capture() ?? m_BaseState.ControlState,
                m_GameplayEffectState.Commit(),
                m_EquipmentState.Capture(),
                m_BaseState.TimelineSnapshots);
        }

        static T[] ToArray<T>(IReadOnlyList<T> values)
        {
            var result = new T[values.Count];
            for (int index = 0; index < values.Count; index++)
                result[index] = values[index];
            return result;
        }

        FixedAbilityExecutionSavepoint RequireTopSavepoint(IFixedAbilityExecutionSavepoint savepoint)
        {
            if (!(savepoint is FixedAbilityExecutionSavepoint executionSavepoint) ||
                m_Savepoints.Count == 0 || !ReferenceEquals(executionSavepoint, m_Savepoints.Peek()))
                throw new InvalidOperationException("Fixed Ability execution savepoint is stale or unbalanced.");
            return executionSavepoint;
        }

        void RecycleTopSavepoint()
        {
            FixedAbilityExecutionSavepoint savepoint = m_Savepoints.Pop();
            savepoint.Clear();
            m_SavepointPool.Push(savepoint);
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterRuntimeStateTransaction));
        }
    }

    internal sealed class FixedSkillExecutionState : IFixedSkillExecutionState
    {
        readonly object m_BindingIdentity;
        readonly GameplayAbilityExecutionIdentity m_Identity;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly FixedGameplayAbilityExecutionData m_Ability;
        FixedAbilityRuntimeState m_CommittedState;
        Dictionary<int, AbilityStateValue> m_StateValues;
        Dictionary<int, FixedMotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<AbilityStateValue> m_AbilityExecutionState;
        bool m_StateValuesChanged;
        bool m_MotionWarpStatesChanged;
        bool m_AbilityExecutionStateChanged;
        bool m_Disposed;

        public FixedSkillExecutionState(
            object bindingIdentity,
            GameplayAbilityExecutionIdentity identity,
            GameplayAbilityExecutionLayout layout,
            FixedGameplayAbilityExecutionData ability,
            FixedAbilityRuntimeState state)
        {
            m_BindingIdentity = bindingIdentity ?? throw new ArgumentNullException(nameof(bindingIdentity));
            if (!identity.IsValid)
                throw new ArgumentException("Fixed Ability execution identity is incomplete.", nameof(identity));
            m_Identity = identity;
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Ability = ability ?? throw new ArgumentNullException(nameof(ability));
            if (m_Ability.AbilityId != m_Identity.AbilityId)
                throw new ArgumentException("Fixed Ability execution data does not match its identity.", nameof(ability));
            state = state ?? throw new ArgumentNullException(nameof(state));
            m_CommittedState = state;
            LoadCommittedCollections();
            m_AbilityExecutionState = state.AbilityExecutionState;
        }

        internal FixedSkillExecutionState Restart(FixedAbilityRuntimeState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.AbilityIdentity.AbilityId != m_Identity.AbilityId)
                throw new ArgumentException("Fixed Ability runtime state identity does not match its workspace.", nameof(state));
            m_CommittedState = state;
            LoadCommittedCollections();
            m_AbilityExecutionState = state.AbilityExecutionState;
            m_StateValuesChanged = false;
            m_MotionWarpStatesChanged = false;
            m_AbilityExecutionStateChanged = false;
            m_Disposed = false;
            return this;
        }

        internal object BindingIdentity => m_BindingIdentity;

        public AbilityStateValue Get(int slotIndex) => Get(m_Layout.Address(slotIndex));

        public AbilityStateValue Get(TypedStateAddress address)
        {
            RequireActive();
            ProgramStateSlot slot = m_Layout.StateSlots[address.SlotIndex];
            if (!m_StateValues.TryGetValue(address.SlotIndex, out AbilityStateValue value))
                value = slot.DefaultConstantIndex >= 0
                    ? AbilityStateValue.FromConstant(m_Ability.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                    : AbilityStateValue.Default(slot.ValueKind);
            if (value.Kind != address.ValueKind)
                throw new InvalidOperationException($"State slot '{address.SlotIndex}' expects '{address.ValueKind}', received '{value.Kind}'.");
            return value;
        }

        public void Set(int slotIndex, AbilityStateValue value) => Set(m_Layout.Address(slotIndex), value);

        public void Set(TypedStateAddress address, AbilityStateValue value)
        {
            RequireActive();
            if (value.Kind != address.ValueKind)
                throw new InvalidOperationException($"State slot '{address.SlotIndex}' expects '{address.ValueKind}', received '{value.Kind}'.");
            ProgramStateSlot slot = m_Layout.StateSlots[address.SlotIndex];
            AbilityStateValue current = m_StateValues.TryGetValue(address.SlotIndex, out AbilityStateValue existing)
                ? existing
                : slot.DefaultConstantIndex >= 0
                    ? AbilityStateValue.FromConstant(m_Ability.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                    : AbilityStateValue.Default(slot.ValueKind);
            if (current.Equals(value))
                return;
            m_StateValuesChanged = true;
            m_StateValues[address.SlotIndex] = value;
        }

        public void Reset(int slotIndex)
        {
            RequireActive();
            ProgramStateSlot slot = m_Layout.StateSlots[slotIndex];
            Set(slotIndex, slot.DefaultConstantIndex >= 0
                ? AbilityStateValue.FromConstant(m_Ability.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                : AbilityStateValue.Default(slot.ValueKind));
        }

        public GameplayAbilityExecutionAggregate<AbilityStateValue> GetAbilityExecutionState()
        {
            RequireActive();
            return m_AbilityExecutionState;
        }

        public void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<AbilityStateValue> state)
        {
            RequireActive();
            m_AbilityExecutionState = state ?? throw new ArgumentNullException(nameof(state));
            m_AbilityExecutionStateChanged =
                !m_AbilityExecutionState.Equals(m_CommittedState.AbilityExecutionState);
        }

        public FixedMotionWarpState GetMotionWarpState(OperationHandle operation)
        {
            RequireActive();
            if (!m_Layout.HasMotionWarp(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no MotionWarp state for '{operation}'.");
            return m_MotionWarpStates.TryGetValue(operation.Value, out FixedMotionWarpState value)
                ? value
                : default;
        }

        public void SetMotionWarpState(OperationHandle operation, FixedMotionWarpState value)
        {
            RequireActive();
            if (!m_Layout.HasMotionWarp(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no MotionWarp state for '{operation}'.");
            if (value.Active)
            {
                if (m_MotionWarpStates.TryGetValue(operation.Value, out FixedMotionWarpState current) &&
                    current.Equals(value))
                    return;
                m_MotionWarpStatesChanged = true;
                m_MotionWarpStates[operation.Value] = value;
            }
            else
            {
                if (!m_MotionWarpStates.TryGetValue(operation.Value, out FixedMotionWarpState current))
                    return;
                if (current.Equals(default))
                    return;
                m_MotionWarpStatesChanged = true;
                m_MotionWarpStates.Remove(operation.Value);
            }
        }

        public void Dispose()
        {
            m_Disposed = true;
        }

        internal FixedAbilityRuntimeState TakeSnapshot()
        {
            RequireActive();
            if (!m_StateValuesChanged && !m_MotionWarpStatesChanged && !m_AbilityExecutionStateChanged)
            {
                FixedAbilityRuntimeState committed = m_CommittedState;
                Clear(false, false);
                return committed;
            }
            Dictionary<int, AbilityStateValue> stateValues = m_StateValuesChanged
                ? m_StateValues
                : m_CommittedState.StateValues;
            Dictionary<int, FixedMotionWarpState> motionWarpStates = m_MotionWarpStatesChanged
                ? m_MotionWarpStates
                : m_CommittedState.MotionWarpStates;
            GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState = m_AbilityExecutionStateChanged
                ? m_AbilityExecutionState
                : m_CommittedState.AbilityExecutionState;
            FixedAbilityRuntimeState snapshot = FixedAbilityRuntimeState.Adopt(
                m_Identity,
                stateValues,
                abilityExecutionState,
                motionWarpStates);
            m_CommittedState = snapshot;
            Clear(m_StateValuesChanged, m_MotionWarpStatesChanged);
            return snapshot;
        }

        void LoadCommittedCollections()
        {
            if (m_StateValues == null)
                m_StateValues = new Dictionary<int, AbilityStateValue>(m_CommittedState.StateValues.Count);
            if (m_MotionWarpStates == null)
                m_MotionWarpStates = new Dictionary<int, FixedMotionWarpState>(m_CommittedState.MotionWarpStates.Count);
            m_StateValues.Clear();
            foreach (KeyValuePair<int, AbilityStateValue> value in m_CommittedState.StateValues)
                m_StateValues.Add(value.Key, value.Value);
            m_MotionWarpStates.Clear();
            foreach (KeyValuePair<int, FixedMotionWarpState> value in m_CommittedState.MotionWarpStates)
                m_MotionWarpStates.Add(value.Key, value.Value);
        }

        void Clear(bool releaseStateValues, bool releaseMotionWarpStates)
        {
            if (releaseStateValues)
                m_StateValues = null;
            if (releaseMotionWarpStates)
                m_MotionWarpStates = null;
            m_AbilityExecutionState = null;
            m_Disposed = true;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedSkillExecutionState));
        }
    }
}
