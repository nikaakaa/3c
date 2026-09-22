using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionSavepoint
        : IFloat32AbilityExecutionSavepoint
    {
        internal Float32AbilityExecutionSavepoint()
        {
        }

        internal Float32AbilityExecutionSavepoint Begin(
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

    internal sealed class Float32CharacterRuntimeStateTransaction : IFloat32AbilityExecutionSavepointPort, IFloat32ControlRuntimeStatePort
    {
        readonly Float32CharacterRuntimeState m_BaseState;
        readonly Dictionary<CharacterSkillId, Float32AbilityRuntimeState> m_AbilityStates;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Stack<Float32AbilityExecutionSavepoint> m_Savepoints =
            new Stack<Float32AbilityExecutionSavepoint>();
        readonly Stack<Float32AbilityExecutionSavepoint> m_SavepointPool =
            new Stack<Float32AbilityExecutionSavepoint>();
        readonly Float32CharacterActionRuntimeState m_ActionState;
        readonly Float32CharacterInputRequestState m_InputRequestState;
        readonly Float32CharacterEventSequenceState m_EventSequenceState;
        readonly Float32CharacterHandleAllocatorState m_HandleAllocatorState;
        readonly Float32CharacterGameplayEffectRuntimeState m_GameplayEffectState;
        readonly Float32CharacterEquipmentRuntimeState m_EquipmentState;
        readonly CharacterControlRuntimeStateTransaction m_ControlState;
        bool m_ControlStateBound;
        bool m_Disposed;

        public Float32CharacterRuntimeStateTransaction(
            Float32CharacterRuntimeState baseState,
            SimulationTick tick,
            int tickRate,
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog,
            Float32CharacterInputRequestState inputRequestState,
            Float32CharacterActionRuntimeState actionState,
            CharacterControlRuntimeStateTransaction controlState,
            Float32CharacterEventSequenceState eventSequenceState,
            Float32CharacterHandleAllocatorState handleAllocatorState,
            Float32CharacterGameplayEffectRuntimeState gameplayEffectState,
            Float32CharacterEquipmentRuntimeState equipmentState)
        {
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Float32 Character runtime transaction timing is incomplete.");
            m_Tick = tick;
            m_TickRate = tickRate;
            m_AbilityStates = new Dictionary<CharacterSkillId, Float32AbilityRuntimeState>(
                baseState.Abilities.Count);
            for (int i = 0; i < baseState.Abilities.Count; i++)
            {
                Float32AbilityRuntimeState state = baseState.Abilities[i];
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
        }

        internal IFloat32SkillExecutionState BindAbility(
            GameplayAbilityExecutionIdentity identity,
            GameplayAbilityExecutionLayout layout,
            Float32GameplayAbilityExecutionData ability)
        {
            RequireActive();
            if (!identity.IsValid)
                throw new ArgumentException("Float32 Ability execution identity is incomplete.", nameof(identity));
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));
            if (ability.AbilityId != identity.AbilityId)
                throw new ArgumentException("Float32 Ability execution data does not match its identity.", nameof(ability));
            if (!m_AbilityStates.TryGetValue(identity.AbilityId, out Float32AbilityRuntimeState state))
                throw new InvalidOperationException($"Ability '{identity.AbilityId}' is not part of the Character runtime state.");
            return new Float32SkillExecutionState(this, identity, layout, ability, state);
        }

        public CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema)
        {
            RequireActive();
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (m_ControlStateBound)
                throw new InvalidOperationException("Float32 Character Control state is already bound.");
            if (m_BaseState.ControlState == null)
                throw new InvalidOperationException("Float32 Character runtime has no Control state.");
            m_ControlStateBound = true;
            return m_ControlState.Restart(
                m_BaseState.ControlState,
                schema,
                m_Tick);
        }

        internal void AcceptAbility(IFloat32SkillExecutionState skillState)
        {
            RequireActive();
            if (!(skillState is Float32SkillExecutionState ability) ||
                !ReferenceEquals(ability.BindingIdentity, this))
            {
                throw new InvalidOperationException("Float32 Ability transaction belongs to another Character runtime transaction.");
            }
            Float32AbilityRuntimeState state = ability.TakeSnapshot();
            m_AbilityStates[state.AbilityIdentity.AbilityId] = state;
        }

        internal Float32CharacterRuntimeState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            return Snapshot();
        }

        internal IFloat32InputRequestStatePort InputRequests => m_InputRequestState;
        internal IFloat32ActionRuntimeStatePort ActionState => m_ActionState;
        internal IFloat32EventSequenceStatePort EventSequenceState => m_EventSequenceState;
        internal IFloat32HandleAllocatorStatePort HandleAllocatorState => m_HandleAllocatorState;
        internal IFloat32GameplayEffectStatePort GameplayEffectState => m_GameplayEffectState;
        internal IFloat32EquipmentStatePort EquipmentState => m_EquipmentState;

        public IFloat32AbilityExecutionSavepoint CreateSavepoint()
        {
            RequireActive();
            GameplayEffectStateAggregate gameplayEffectState = m_GameplayEffectState.Capture();
            EquipmentStateAggregate equipmentState = m_EquipmentState.Capture();
            Float32AbilityExecutionSavepoint savepoint = m_SavepointPool.Count > 0
                ? m_SavepointPool.Pop()
                : new Float32AbilityExecutionSavepoint();
            savepoint.Begin(
                m_Savepoints.Count + 1,
                gameplayEffectState,
                equipmentState,
                m_HandleAllocatorState.HandleAllocator,
                m_EventSequenceState.EventSequence);
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(IFloat32AbilityExecutionSavepoint savepoint)
        {
            RequireActive();
            Float32AbilityExecutionSavepoint executionSavepoint = RequireTopSavepoint(savepoint);
            m_GameplayEffectState.Restore(executionSavepoint.GameplayEffectState);
            m_EquipmentState.Restore(executionSavepoint.EquipmentState);
            m_HandleAllocatorState.RestoreHandleAllocator(executionSavepoint.HandleAllocator);
            m_EventSequenceState.Restore(executionSavepoint.EventSequence);
            RecycleTopSavepoint();
        }

        public void Release(IFloat32AbilityExecutionSavepoint savepoint)
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

        Float32CharacterRuntimeState Snapshot()
        {
            return new Float32CharacterRuntimeState(
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
                m_BaseState.TimelineSnapshots,
                ownsInputRequests: true);
        }

        Float32AbilityExecutionSavepoint RequireTopSavepoint(IFloat32AbilityExecutionSavepoint savepoint)
        {
            if (!(savepoint is Float32AbilityExecutionSavepoint executionSavepoint) ||
                m_Savepoints.Count == 0 || !ReferenceEquals(executionSavepoint, m_Savepoints.Peek()))
                throw new InvalidOperationException("Float32 Ability execution savepoint is stale or unbalanced.");
            return executionSavepoint;
        }

        void RecycleTopSavepoint()
        {
            Float32AbilityExecutionSavepoint savepoint = m_Savepoints.Pop();
            savepoint.Clear();
            m_SavepointPool.Push(savepoint);
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterRuntimeStateTransaction));
        }
    }

    internal sealed class Float32SkillExecutionState : IFloat32SkillExecutionState
    {
        readonly object m_BindingIdentity;
        readonly GameplayAbilityExecutionIdentity m_Identity;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly Float32GameplayAbilityExecutionData m_Ability;
        Dictionary<int, AbilityStateValue> m_StateValues;
        Dictionary<int, Float32MotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<AbilityStateValue> m_AbilityExecutionState;
        bool m_Disposed;

        public Float32SkillExecutionState(
            object bindingIdentity,
            GameplayAbilityExecutionIdentity identity,
            GameplayAbilityExecutionLayout layout,
            Float32GameplayAbilityExecutionData ability,
            Float32AbilityRuntimeState state)
        {
            m_BindingIdentity = bindingIdentity ?? throw new ArgumentNullException(nameof(bindingIdentity));
            if (!identity.IsValid)
                throw new ArgumentException("Float32 Ability execution identity is incomplete.", nameof(identity));
            m_Identity = identity;
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Ability = ability ?? throw new ArgumentNullException(nameof(ability));
            if (m_Ability.AbilityId != m_Identity.AbilityId)
                throw new ArgumentException("Float32 Ability execution data does not match its identity.", nameof(ability));
            state = state ?? throw new ArgumentNullException(nameof(state));
            m_StateValues = new Dictionary<int, AbilityStateValue>(state.StateValues);
            m_MotionWarpStates = new Dictionary<int, Float32MotionWarpState>(state.MotionWarpStates);
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

        public Float32MotionWarpState GetMotionWarpState(OperationHandle operation)
        {
            RequireActive();
            if (!m_Layout.HasMotionWarp(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no MotionWarp state for '{operation}'.");
            return m_MotionWarpStates.TryGetValue(operation.Value, out Float32MotionWarpState value)
                ? value
                : default;
        }

        public void SetMotionWarpState(OperationHandle operation, Float32MotionWarpState value)
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

        internal Float32AbilityRuntimeState TakeSnapshot()
        {
            RequireActive();
            Float32AbilityRuntimeState snapshot = Float32AbilityRuntimeState.Adopt(
                m_Identity,
                m_StateValues,
                m_AbilityExecutionState,
                m_MotionWarpStates);
            m_StateValues = null;
            m_MotionWarpStates = null;
            m_AbilityExecutionState = null;
            m_Disposed = true;
            return snapshot;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32SkillExecutionState));
        }
    }
}
