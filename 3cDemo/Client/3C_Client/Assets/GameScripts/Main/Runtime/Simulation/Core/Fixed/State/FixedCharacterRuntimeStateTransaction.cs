using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionSavepoint
        : IFixedAbilityExecutionSavepoint
    {
        FixedAbilityExecutionSavepoint()
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
        readonly FixedCharacterRuntimeState m_BaseState;
        readonly Dictionary<CharacterSkillId, FixedAbilityRuntimeState> m_AbilityStates;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Stack<FixedAbilityExecutionSavepoint> m_Savepoints =
            new Stack<FixedAbilityExecutionSavepoint>();
        readonly Stack<FixedAbilityExecutionSavepoint> m_SavepointPool =
            new Stack<FixedAbilityExecutionSavepoint>();
        readonly FixedCharacterActionRuntimeState m_ActionState;
        readonly FixedCharacterInputRequestState m_InputRequestState;
        readonly FixedCharacterEventSequenceState m_EventSequenceState;
        readonly FixedCharacterHandleAllocatorState m_HandleAllocatorState;
        readonly FixedCharacterGameplayEffectRuntimeState m_GameplayEffectState;
        readonly FixedCharacterEquipmentRuntimeState m_EquipmentState;
        CharacterControlRuntimeStateTransaction m_ControlState;
        bool m_Disposed;

        public FixedCharacterRuntimeStateTransaction(
            FixedCharacterRuntimeState baseState,
            SimulationTick tick,
            int tickRate,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog)
        {
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character runtime transaction timing is incomplete.");
            m_Tick = tick;
            m_TickRate = tickRate;
            m_AbilityStates = new Dictionary<CharacterSkillId, FixedAbilityRuntimeState>();
            for (int i = 0; i < baseState.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState state = baseState.Abilities[i];
                m_AbilityStates.Add(state.AbilityIdentity.AbilityId, state.Clone());
            }
            m_ActionState = new FixedCharacterActionRuntimeState(
                baseState.ActionActivationRequests,
                baseState.ActionInstances,
                baseState.ActionEventSequence);
            m_InputRequestState = new FixedCharacterInputRequestState(m_Tick, baseState.InputRequests);
            m_EventSequenceState = new FixedCharacterEventSequenceState(baseState.EventSequence);
            m_HandleAllocatorState = new FixedCharacterHandleAllocatorState(baseState.HandleAllocator);
            m_GameplayEffectState = new FixedCharacterGameplayEffectRuntimeState(
                m_TickRate,
                gameplayEffectCatalog,
                baseState.GameplayEffectState);
            m_EquipmentState = new FixedCharacterEquipmentRuntimeState(baseState.EquipmentState);
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
            return new FixedSkillExecutionState(this, identity, layout, ability, state);
        }

        public CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema)
        {
            RequireActive();
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (m_ControlState != null)
                throw new InvalidOperationException("Fixed Character Control state is already bound.");
            if (m_BaseState.ControlState == null)
                throw new InvalidOperationException("Fixed Character runtime has no Control state.");
            m_ControlState = CharacterControlRuntimeStateTransaction.Begin(
                m_BaseState.ControlState,
                schema,
                m_Tick);
            return m_ControlState;
        }

        internal void AcceptAbility(IFixedSkillExecutionState skillState)
        {
            RequireActive();
            if (!(skillState is FixedSkillExecutionState ability) ||
                !ReferenceEquals(ability.BindingIdentity, this))
            {
                throw new InvalidOperationException("Fixed Ability transaction belongs to another Character runtime transaction.");
            }
            FixedAbilityRuntimeState state = ability.SnapshotState();
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
            return new FixedCharacterRuntimeState(
                m_BaseState.NumericProfile,
                m_BaseState.GameplayContentHash,
                m_BaseState.StateSchemaHash,
                m_Tick.Value,
                m_AbilityStates.Values,
                m_ActionState.GetActionActivationRequests(),
                m_ActionState.GetActionInstances(),
                m_InputRequestState.Capture(),
                m_EventSequenceState.EventSequence,
                m_ActionState.ActionEventSequence,
                m_HandleAllocatorState.HandleAllocator,
                m_ControlState?.Capture() ?? m_BaseState.ControlState,
                m_GameplayEffectState.Capture(),
                m_EquipmentState.Capture(),
                m_BaseState.TimelineSnapshots);
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
        readonly Dictionary<int, AbilityStateValue> m_StateValues;
        readonly Dictionary<int, FixedMotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<AbilityStateValue> m_AbilityExecutionState;
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
            m_StateValues = new Dictionary<int, AbilityStateValue>(state.StateValues);
            m_MotionWarpStates = new Dictionary<int, FixedMotionWarpState>(state.MotionWarpStates);
            m_AbilityExecutionState = state.AbilityExecutionState.Clone();
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
                m_MotionWarpStates[operation.Value] = value;
            else
                m_MotionWarpStates.Remove(operation.Value);
        }

        public void Dispose()
        {
            m_Disposed = true;
        }

        internal FixedAbilityRuntimeState SnapshotState()
        {
            RequireActive();
            return new FixedAbilityRuntimeState(
                m_Identity,
                m_StateValues,
                m_AbilityExecutionState,
                m_MotionWarpStates);
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedSkillExecutionState));
        }
    }
}
