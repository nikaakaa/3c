using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterRuntimeStateSavepoint
        : IFixedAbilityExecutionSavepoint
    {
        internal FixedCharacterRuntimeStateSavepoint(
            int depth,
            FixedCharacterRuntimeState snapshot)
        {
            Depth = depth;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public int Depth { get; }
        internal FixedCharacterRuntimeState Snapshot { get; }
    }

    internal interface IFixedSkillExecutionState : IDisposable
    {
        AbilityStateValue Get(int slotIndex);
        AbilityStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, AbilityStateValue value);
        void Set(TypedStateAddress address, AbilityStateValue value);
        void Reset(int slotIndex);
        GameplayAbilityExecutionAggregate<AbilityStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<AbilityStateValue> state);
        FixedMotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, FixedMotionWarpState value);
    }

    internal interface IFixedInputRequestStatePort
    {
        SimulationTick Tick { get; }
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
    }

    internal interface IFixedActionRuntimeStatePort
    {
        ulong NextActionEventSequence();
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<FixedActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<FixedActionInstanceState> actions);
    }

    internal interface IFixedHandleAllocatorStatePort
    {
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
    }

    internal interface IFixedEventSequenceStatePort
    {
        ulong NextEventSequence();
    }

    internal interface IFixedGameplayEffectStatePort
    {
        int TickRate { get; }
        SimulationGameplayEffectState GetGameplayEffectState(FixedGameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
    }

    internal interface IFixedEquipmentStatePort
    {
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
    }

    internal interface IFixedControlRuntimeStatePort
    {
        CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema);
    }

    internal sealed class FixedCharacterRuntimeStateTransaction : IFixedAbilityExecutionSavepointPort, IFixedControlRuntimeStatePort
    {
        readonly FixedCharacterRuntimeState m_BaseState;
        readonly Dictionary<CharacterSkillId, FixedAbilityRuntimeState> m_AbilityStates;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Stack<FixedCharacterRuntimeStateSavepoint> m_Savepoints =
            new Stack<FixedCharacterRuntimeStateSavepoint>();
        readonly FixedCharacterActionRuntimeState m_ActionState;
        readonly FixedCharacterInputRequestState m_InputRequestState;
        readonly FixedCharacterEventSequenceState m_EventSequenceState;
        readonly FixedCharacterHandleAllocatorState m_HandleAllocatorState;
        readonly FixedCharacterGameplayEffectRuntimeState m_GameplayEffectState;
        readonly FixedCharacterEquipmentRuntimeState m_EquipmentState;
        readonly object m_AbilityBindingIdentity = new object();
        CharacterControlRuntimeStateTransaction m_ControlState;
        bool m_Disposed;

        public FixedCharacterRuntimeStateTransaction(
            FixedCharacterRuntimeState baseState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog)
        {
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character runtime transaction identity is incomplete.");
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
            FixedGameplayAbilityExecutionInstallation installation)
        {
            RequireActive();
            if (installation == null)
                throw new ArgumentNullException(nameof(installation));
            if (!m_AbilityStates.TryGetValue(installation.Data.AbilityId, out FixedAbilityRuntimeState state))
                throw new InvalidOperationException($"Ability '{installation.Data.AbilityId}' is not part of the Character runtime state.");
            return new FixedSkillExecutionState(m_AbilityBindingIdentity, m_Tick, installation, state);
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
                !ReferenceEquals(ability.BindingIdentity, m_AbilityBindingIdentity))
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
            var savepoint = new FixedCharacterRuntimeStateSavepoint(m_Savepoints.Count + 1, Snapshot());
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(IFixedAbilityExecutionSavepoint savepoint)
        {
            RequireActive();
            FixedCharacterRuntimeStateSavepoint characterSavepoint = RequireTopSavepoint(savepoint);
            Apply(characterSavepoint.Snapshot);
            m_Savepoints.Pop();
        }

        public void Release(IFixedAbilityExecutionSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            m_Savepoints.Pop();
        }

        public int SavepointDepth => m_Savepoints.Count;

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Savepoints.Clear();
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
                m_EquipmentState.Capture());
        }

        void Apply(FixedCharacterRuntimeState state)
        {
            m_AbilityStates.Clear();
            for (int i = 0; i < state.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState ability = state.Abilities[i];
                m_AbilityStates.Add(ability.AbilityIdentity.AbilityId, ability.Clone());
            }
            m_ActionState.Restore(
                state.ActionActivationRequests,
                state.ActionInstances,
                state.ActionEventSequence);
            m_InputRequestState.Restore(state.InputRequests);
            m_EventSequenceState.Restore(state.EventSequence);
            m_HandleAllocatorState.RestoreHandleAllocator(state.HandleAllocator);
            if (m_ControlState != null)
            {
                if (state.ControlState == null)
                    throw new InvalidOperationException("Fixed Character runtime restore removed its bound Control state.");
                m_ControlState.Restore(state.ControlState);
            }
            m_GameplayEffectState.Restore(state.GameplayEffectState);
            m_EquipmentState.Restore(state.EquipmentState);
        }

        FixedCharacterRuntimeStateSavepoint RequireTopSavepoint(IFixedAbilityExecutionSavepoint savepoint)
        {
            if (!(savepoint is FixedCharacterRuntimeStateSavepoint characterSavepoint) ||
                m_Savepoints.Count == 0 || !ReferenceEquals(characterSavepoint, m_Savepoints.Peek()))
                throw new InvalidOperationException("Character runtime state savepoint is stale or unbalanced.");
            return characterSavepoint;
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
        readonly SimulationTick m_Tick;
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly FixedGameplayAbilityExecutionData m_Ability;
        readonly Dictionary<int, AbilityStateValue> m_StateValues;
        readonly Dictionary<int, FixedMotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<AbilityStateValue> m_AbilityExecutionState;
        bool m_Disposed;

        public FixedSkillExecutionState(
            object bindingIdentity,
            SimulationTick tick,
            FixedGameplayAbilityExecutionInstallation installation,
            FixedAbilityRuntimeState state)
        {
            m_BindingIdentity = bindingIdentity ?? throw new ArgumentNullException(nameof(bindingIdentity));
            if (!tick.IsValid)
                throw new ArgumentException("Fixed Ability state transaction Tick is invalid.", nameof(tick));
            m_Tick = tick;
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            m_Layout = installation.Layout;
            m_Ability = installation.Data;
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
                m_Installation.Identity,
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
