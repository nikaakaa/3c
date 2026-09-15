using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedCharacterRuntimeStateTransactionDiagnostics
    {
        public FixedCharacterRuntimeStateTransactionDiagnostics(int savepointDepth)
        {
            SavepointDepth = savepointDepth;
        }

        public int SavepointDepth { get; }
    }

    internal sealed class FixedCharacterRuntimeStateSavepoint
    {
        internal FixedCharacterRuntimeStateSavepoint(
            int depth,
            FixedCharacterRuntimeState snapshot)
        {
            Depth = depth;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        internal int Depth { get; }
        internal FixedCharacterRuntimeState Snapshot { get; }
    }

    internal interface IFixedAbilityExecutionStateTransaction : IDisposable
    {
        FixedGameplayAbilityExecutionInstallation Installation { get; }
        CharacterStateValue Get(int slotIndex);
        CharacterStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, CharacterStateValue value);
        void Set(TypedStateAddress address, CharacterStateValue value);
        void Reset(int slotIndex);
        GameplayAbilityExecutionAggregate<CharacterStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<CharacterStateValue> state);
        FixedActionInstanceReference GetTimelineRetainedActionContext(OperationHandle operation);
        void SetTimelineRetainedActionContext(OperationHandle operation, FixedActionInstanceReference value);
        FixedMotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, FixedMotionWarpState value);
        void Abort();
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

    internal interface IFixedAbilityDomainStatePort
    {
        ActorId ActorId { get; }
        SimulationTick Tick { get; }
        int TickRate { get; }
        ulong NextEventSequence();
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
        SimulationGameplayEffectState GetGameplayEffectState(FixedGameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
        void Abort();
        FixedCharacterRuntimeStateSavepoint CreateSavepoint();
        void Restore(FixedCharacterRuntimeStateSavepoint savepoint);
        void Release(FixedCharacterRuntimeStateSavepoint savepoint);
        FixedCharacterRuntimeStateTransactionDiagnostics Diagnostics();
    }

    internal sealed class FixedCharacterRuntimeStateTransaction : IFixedAbilityDomainStatePort, IFixedInputRequestStatePort, IFixedActionRuntimeStatePort
    {
        readonly FixedCharacterRuntimeState m_BaseState;
        readonly Dictionary<CharacterSkillId, FixedAbilityRuntimeState> m_AbilityStates;
        readonly FixedGameplayEffectRuntimeCatalog m_GameplayEffectCatalog;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Stack<FixedCharacterRuntimeStateSavepoint> m_Savepoints =
            new Stack<FixedCharacterRuntimeStateSavepoint>();
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<FixedActionInstanceState> m_ActionInstances;
        readonly Dictionary<string, SimulationInputRequestState> m_InputRequests;
        CharacterControlRuntimeStateTransaction m_ControlState;
        GameplayEffectStateAggregate m_GameplayEffectAggregate;
        SimulationGameplayEffectState m_GameplayEffectWorking;
        FixedGameplayEffectExecutionScratch m_GameplayEffectScratch;
        EquipmentStateAggregate m_EquipmentState;
        ulong m_EventSequence;
        ulong m_ActionEventSequence;
        ulong m_HandleAllocator;
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
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_GameplayEffectCatalog = gameplayEffectCatalog;
            m_AbilityStates = new Dictionary<CharacterSkillId, FixedAbilityRuntimeState>();
            for (int i = 0; i < baseState.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState state = baseState.Abilities[i];
                m_AbilityStates.Add(state.AbilityIdentity.AbilityId, state.Clone(tick.Value));
            }
            m_ActionActivationRequests = new List<SimulationActionActivationRequestState>(baseState.ActionActivationRequests);
            m_ActionInstances = new List<FixedActionInstanceState>(baseState.ActionInstances);
            m_InputRequests = new Dictionary<string, SimulationInputRequestState>(baseState.InputRequests, StringComparer.Ordinal);
            m_GameplayEffectAggregate = baseState.GameplayEffectState;
            m_EquipmentState = baseState.EquipmentState;
            m_EventSequence = baseState.EventSequence;
            m_ActionEventSequence = baseState.ActionEventSequence;
            m_HandleAllocator = baseState.HandleAllocator;
        }

        public ActorId ActorId => m_ActorId;
        public SimulationTick Tick => m_Tick;
        public int TickRate => m_TickRate;

        internal IFixedAbilityExecutionStateTransaction BindAbility(
            FixedGameplayAbilityExecutionInstallation installation)
        {
            RequireActive();
            if (installation == null)
                throw new ArgumentNullException(nameof(installation));
            if (!m_AbilityStates.TryGetValue(installation.Data.AbilityId, out FixedAbilityRuntimeState state))
                throw new InvalidOperationException($"Ability '{installation.Data.AbilityId}' is not part of the Character runtime state.");
            return new FixedAbilityExecutionStateTransaction(this, installation, state);
        }

        internal CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema)
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

        internal void AcceptAbility(IFixedAbilityExecutionStateTransaction transaction)
        {
            RequireActive();
            if (!(transaction is FixedAbilityExecutionStateTransaction ability) ||
                !ReferenceEquals(ability.Owner, this))
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

        public ulong NextEventSequence()
        {
            RequireActive();
            m_EventSequence = checked(m_EventSequence + 1UL);
            if (m_EventSequence == 0)
                throw new OverflowException("Simulation event sequence overflowed.");
            return m_EventSequence;
        }

        public ulong NextActionEventSequence()
        {
            RequireActive();
            m_ActionEventSequence = checked(m_ActionEventSequence + 1UL);
            if (m_ActionEventSequence == 0)
                throw new OverflowException("Action event sequence overflowed.");
            return m_ActionEventSequence;
        }

        public ulong NextHandleAllocator()
        {
            RequireActive();
            m_HandleAllocator = checked(m_HandleAllocator + 1UL);
            if (m_HandleAllocator == 0)
                throw new OverflowException("Simulation handle allocator overflowed.");
            return m_HandleAllocator;
        }

        public ulong CaptureHandleAllocator()
        {
            RequireActive();
            return m_HandleAllocator;
        }

        public void RestoreHandleAllocator(ulong value)
        {
            RequireActive();
            m_HandleAllocator = value;
        }

        public SimulationInputRequestState GetInputRequest(string requestId)
        {
            RequireActive();
            return m_InputRequests.TryGetValue(requestId ?? string.Empty, out SimulationInputRequestState state)
                ? state
                : default;
        }

        public void SetInputRequest(string requestId, SimulationInputRequestState state)
        {
            RequireActive();
            m_InputRequests[requestId ?? string.Empty] = state;
        }

        public IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests()
        {
            RequireActive();
            return m_ActionActivationRequests;
        }

        public void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests)
        {
            RequireActive();
            m_ActionActivationRequests.Clear();
            if (requests != null)
                m_ActionActivationRequests.AddRange(requests);
        }

        public IReadOnlyList<FixedActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void SetActionInstances(IReadOnlyList<FixedActionInstanceState> actions)
        {
            RequireActive();
            m_ActionInstances.Clear();
            if (actions != null)
                m_ActionInstances.AddRange(actions);
        }

        public SimulationGameplayEffectState GetGameplayEffectState(FixedGameplayEffectExecutionScratch scratch)
        {
            RequireActive();
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));
            if (m_GameplayEffectWorking != null)
            {
                if (!ReferenceEquals(m_GameplayEffectScratch, scratch))
                    throw new InvalidOperationException("Gameplay Effect state is bound to another Actor workspace.");
                return m_GameplayEffectWorking;
            }
            if (m_GameplayEffectCatalog == null || m_GameplayEffectAggregate == null)
                throw new InvalidOperationException("Character does not install Gameplay Effect state.");
            m_GameplayEffectScratch = scratch;
            m_GameplayEffectWorking = new SimulationGameplayEffectState(
                m_GameplayEffectCatalog,
                m_GameplayEffectAggregate,
                scratch);
            return m_GameplayEffectWorking;
        }

        public GameplayEffectStateAggregate GetGameplayEffectAggregate()
        {
            RequireActive();
            return m_GameplayEffectWorking?.Freeze() ?? m_GameplayEffectAggregate ??
                throw new InvalidOperationException("Character does not install Gameplay Effect state.");
        }

        public EquipmentStateAggregate GetEquipmentState()
        {
            RequireActive();
            return m_EquipmentState ?? throw new InvalidOperationException("Character does not install Equipment state.");
        }

        public void SetEquipmentState(EquipmentStateAggregate state)
        {
            RequireActive();
            m_EquipmentState = state ?? throw new ArgumentNullException(nameof(state));
        }

        public FixedCharacterRuntimeStateSavepoint CreateSavepoint()
        {
            RequireActive();
            var savepoint = new FixedCharacterRuntimeStateSavepoint(m_Savepoints.Count + 1, Snapshot());
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(FixedCharacterRuntimeStateSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            Apply(savepoint.Snapshot);
            m_Savepoints.Pop();
        }

        public void Release(FixedCharacterRuntimeStateSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            m_Savepoints.Pop();
        }

        public FixedCharacterRuntimeStateTransactionDiagnostics Diagnostics()
        {
            RequireActive();
            return new FixedCharacterRuntimeStateTransactionDiagnostics(m_Savepoints.Count);
        }

        public void Abort()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            m_ControlState?.Abort();
            m_Disposed = true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Savepoints.Clear();
            m_ControlState?.Dispose();
            m_Disposed = true;
        }

        FixedCharacterRuntimeState Snapshot()
        {
            return new FixedCharacterRuntimeState(
                m_BaseState.Installations,
                m_BaseState.NumericProfile,
                m_BaseState.GameplayContentHash,
                m_Tick.Value,
                m_AbilityStates.Values,
                m_ActionActivationRequests,
                m_ActionInstances,
                m_InputRequests,
                m_EventSequence,
                m_ActionEventSequence,
                m_HandleAllocator,
                m_ControlState?.Capture() ?? m_BaseState.ControlState,
                m_GameplayEffectWorking?.Freeze() ?? m_GameplayEffectAggregate,
                m_EquipmentState);
        }

        void Apply(FixedCharacterRuntimeState state)
        {
            m_AbilityStates.Clear();
            for (int i = 0; i < state.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState ability = state.Abilities[i];
                m_AbilityStates.Add(ability.AbilityIdentity.AbilityId, ability.Clone(m_Tick.Value));
            }
            m_ActionActivationRequests.Clear();
            m_ActionActivationRequests.AddRange(state.ActionActivationRequests);
            m_ActionInstances.Clear();
            m_ActionInstances.AddRange(state.ActionInstances);
            m_InputRequests.Clear();
            foreach (KeyValuePair<string, SimulationInputRequestState> value in state.InputRequests)
                m_InputRequests.Add(value.Key, value.Value);
            m_EventSequence = state.EventSequence;
            m_ActionEventSequence = state.ActionEventSequence;
            m_HandleAllocator = state.HandleAllocator;
            if (m_ControlState != null)
            {
                if (state.ControlState == null)
                    throw new InvalidOperationException("Fixed Character runtime restore removed its bound Control state.");
                m_ControlState.Restore(state.ControlState);
            }
            m_GameplayEffectAggregate = state.GameplayEffectState;
            m_GameplayEffectWorking = m_GameplayEffectAggregate == null
                ? null
                : new SimulationGameplayEffectState(m_GameplayEffectCatalog, m_GameplayEffectAggregate, m_GameplayEffectScratch);
            m_EquipmentState = state.EquipmentState;
        }

        void RequireTopSavepoint(FixedCharacterRuntimeStateSavepoint savepoint)
        {
            if (savepoint == null || m_Savepoints.Count == 0 || !ReferenceEquals(savepoint, m_Savepoints.Peek()))
                throw new InvalidOperationException("Character runtime state savepoint is stale or unbalanced.");
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterRuntimeStateTransaction));
        }
    }

    internal sealed class FixedAbilityExecutionStateTransaction : IFixedAbilityExecutionStateTransaction
    {
        readonly FixedCharacterRuntimeStateTransaction m_Owner;
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly FixedGameplayAbilityExecutionData m_Ability;
        readonly Dictionary<int, CharacterStateValue> m_StateValues;
        readonly Dictionary<int, FixedActionInstanceReference> m_TimelineRetainedActionContexts;
        readonly Dictionary<int, FixedMotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<CharacterStateValue> m_AbilityExecutionState;
        bool m_Aborted;
        bool m_Disposed;

        public FixedAbilityExecutionStateTransaction(
            FixedCharacterRuntimeStateTransaction owner,
            FixedGameplayAbilityExecutionInstallation installation,
            FixedAbilityRuntimeState state)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            m_Layout = installation.Layout;
            m_Ability = installation.Data;
            m_StateValues = new Dictionary<int, CharacterStateValue>(state.StateValues);
            m_TimelineRetainedActionContexts = new Dictionary<int, FixedActionInstanceReference>(state.TimelineRetainedActionContexts);
            m_MotionWarpStates = new Dictionary<int, FixedMotionWarpState>(state.MotionWarpStates);
            m_AbilityExecutionState = state.AbilityExecutionState.Clone();
        }

        internal FixedCharacterRuntimeStateTransaction Owner => m_Owner;

        public FixedGameplayAbilityExecutionInstallation Installation => m_Installation;

        public CharacterStateValue Get(int slotIndex) => Get(m_Layout.Address(slotIndex));

        public CharacterStateValue Get(TypedStateAddress address)
        {
            RequireActive();
            ProgramStateSlot slot = m_Layout.StateSlots[address.SlotIndex];
            if (!m_StateValues.TryGetValue(address.SlotIndex, out CharacterStateValue value))
                value = slot.DefaultConstantIndex >= 0
                    ? CharacterStateValue.FromConstant(m_Ability.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                    : CharacterStateValue.Default(slot.ValueKind);
            if (value.Kind != address.ValueKind)
                throw new InvalidOperationException($"State slot '{address.SlotIndex}' expects '{address.ValueKind}', received '{value.Kind}'.");
            return value;
        }

        public void Set(int slotIndex, CharacterStateValue value) => Set(m_Layout.Address(slotIndex), value);

        public void Set(TypedStateAddress address, CharacterStateValue value)
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
                ? CharacterStateValue.FromConstant(m_Ability.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                : CharacterStateValue.Default(slot.ValueKind));
        }

        public GameplayAbilityExecutionAggregate<CharacterStateValue> GetAbilityExecutionState()
        {
            RequireActive();
            return m_AbilityExecutionState;
        }

        public void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<CharacterStateValue> state)
        {
            RequireActive();
            m_AbilityExecutionState = state ?? throw new ArgumentNullException(nameof(state));
        }

        public FixedActionInstanceReference GetTimelineRetainedActionContext(OperationHandle operation)
        {
            RequireActive();
            if (!m_Layout.HasTimelineRetention(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Timeline retention for '{operation}'.");
            return m_TimelineRetainedActionContexts.TryGetValue(operation.Value, out FixedActionInstanceReference value)
                ? value
                : default;
        }

        public void SetTimelineRetainedActionContext(OperationHandle operation, FixedActionInstanceReference value)
        {
            RequireActive();
            if (!m_Layout.HasTimelineRetention(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Timeline retention for '{operation}'.");
            if (value.IsValid)
                m_TimelineRetainedActionContexts[operation.Value] = value;
            else
                m_TimelineRetainedActionContexts.Remove(operation.Value);
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

        public void Abort()
        {
            RequireActive();
            m_Aborted = true;
            m_Disposed = true;
        }

        public void Dispose()
        {
            m_Disposed = true;
        }

        internal FixedAbilityRuntimeState SnapshotState()
        {
            RequireActive();
            return new FixedAbilityRuntimeState(
                m_Installation,
                m_Owner.Tick.Value,
                m_StateValues,
                m_AbilityExecutionState,
                m_TimelineRetainedActionContexts,
                m_MotionWarpStates);
        }

        void RequireActive()
        {
            if (m_Aborted)
                throw new InvalidOperationException("Fixed Ability candidate was aborted.");
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedAbilityExecutionStateTransaction));
        }
    }
}
