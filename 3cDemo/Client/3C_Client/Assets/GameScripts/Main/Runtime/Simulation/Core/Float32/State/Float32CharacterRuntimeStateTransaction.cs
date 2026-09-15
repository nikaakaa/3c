using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    internal readonly struct Float32CharacterRuntimeStateTransactionDiagnostics
    {
        public Float32CharacterRuntimeStateTransactionDiagnostics(int savepointDepth)
        {
            SavepointDepth = savepointDepth;
        }

        public int SavepointDepth { get; }
    }

    internal sealed class Float32CharacterRuntimeStateSavepoint
    {
        internal Float32CharacterRuntimeStateSavepoint(
            int depth,
            Float32CharacterRuntimeState snapshot)
        {
            Depth = depth;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        internal int Depth { get; }
        internal Float32CharacterRuntimeState Snapshot { get; }
    }

    internal interface IFloat32AbilityExecutionStateTransaction : IDisposable
    {
        Float32GameplayAbilityExecutionInstallation Installation { get; }
        CharacterStateValue Get(int slotIndex);
        CharacterStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, CharacterStateValue value);
        void Set(TypedStateAddress address, CharacterStateValue value);
        void Reset(int slotIndex);
        GameplayAbilityExecutionAggregate<CharacterStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<CharacterStateValue> state);
        Float32MotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, Float32MotionWarpState value);
        void Abort();
    }

    internal interface IFloat32InputRequestStatePort
    {
        SimulationTick Tick { get; }
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
    }

    internal interface IFloat32ActionRuntimeStatePort
    {
        ulong NextActionEventSequence();
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<Float32ActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<Float32ActionInstanceState> actions);
    }

    internal interface IFloat32HandleAllocatorStatePort
    {
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
    }

    internal interface IFloat32EventSequenceStatePort
    {
        ulong NextEventSequence();
    }

    internal interface IFloat32GameplayEffectStatePort
    {
        int TickRate { get; }
        SimulationGameplayEffectState GetGameplayEffectState(Float32GameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
    }

    internal interface IFloat32EquipmentStatePort
    {
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
    }

    internal interface IFloat32ControlRuntimeStatePort
    {
        CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema);
    }

    internal interface IFloat32AbilityTransactionControlPort
    {
        Float32CharacterRuntimeStateSavepoint CreateSavepoint();
        void Restore(Float32CharacterRuntimeStateSavepoint savepoint);
        void Release(Float32CharacterRuntimeStateSavepoint savepoint);
        Float32CharacterRuntimeStateTransactionDiagnostics Diagnostics();
    }

    internal sealed class Float32CharacterRuntimeStateTransaction : IFloat32AbilityTransactionControlPort, IFloat32HandleAllocatorStatePort, IFloat32GameplayEffectStatePort, IFloat32EquipmentStatePort, IFloat32ControlRuntimeStatePort
    {
        readonly Float32CharacterRuntimeState m_BaseState;
        readonly Dictionary<CharacterSkillId, Float32AbilityRuntimeState> m_AbilityStates;
        readonly Float32GameplayEffectRuntimeCatalog m_GameplayEffectCatalog;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Stack<Float32CharacterRuntimeStateSavepoint> m_Savepoints =
            new Stack<Float32CharacterRuntimeStateSavepoint>();
        readonly Float32CharacterActionRuntimeState m_ActionState;
        readonly Float32CharacterInputRequestState m_InputRequestState;
        readonly Float32CharacterEventSequenceState m_EventSequenceState;
        CharacterControlRuntimeStateTransaction m_ControlState;
        GameplayEffectStateAggregate m_GameplayEffectAggregate;
        SimulationGameplayEffectState m_GameplayEffectWorking;
        Float32GameplayEffectExecutionScratch m_GameplayEffectScratch;
        EquipmentStateAggregate m_EquipmentState;
        ulong m_HandleAllocator;
        bool m_Disposed;

        public Float32CharacterRuntimeStateTransaction(
            Float32CharacterRuntimeState baseState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog)
        {
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Float32 Character runtime transaction identity is incomplete.");
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_GameplayEffectCatalog = gameplayEffectCatalog;
            m_AbilityStates = new Dictionary<CharacterSkillId, Float32AbilityRuntimeState>();
            for (int i = 0; i < baseState.Abilities.Count; i++)
            {
                Float32AbilityRuntimeState state = baseState.Abilities[i];
                m_AbilityStates.Add(state.AbilityIdentity.AbilityId, state.Clone(tick.Value));
            }
            m_ActionState = new Float32CharacterActionRuntimeState(
                baseState.ActionActivationRequests,
                baseState.ActionInstances,
                baseState.ActionEventSequence);
            m_InputRequestState = new Float32CharacterInputRequestState(m_Tick, baseState.InputRequests);
            m_EventSequenceState = new Float32CharacterEventSequenceState(baseState.EventSequence);
            m_GameplayEffectAggregate = baseState.GameplayEffectState;
            m_EquipmentState = baseState.EquipmentState;
            m_HandleAllocator = baseState.HandleAllocator;
        }

        public ActorId ActorId => m_ActorId;
        public SimulationTick Tick => m_Tick;
        public int TickRate => m_TickRate;

        internal IFloat32AbilityExecutionStateTransaction BindAbility(
            Float32GameplayAbilityExecutionInstallation installation)
        {
            RequireActive();
            if (installation == null)
                throw new ArgumentNullException(nameof(installation));
            if (!m_AbilityStates.TryGetValue(installation.Data.AbilityId, out Float32AbilityRuntimeState state))
                throw new InvalidOperationException($"Ability '{installation.Data.AbilityId}' is not part of the Character runtime state.");
            return new Float32AbilityExecutionStateTransaction(this, installation, state);
        }

        public CharacterControlRuntimeStateTransaction BindControl(CharacterControlStateSchema schema)
        {
            RequireActive();
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (m_ControlState != null)
                throw new InvalidOperationException("Float32 Character Control state is already bound.");
            if (m_BaseState.ControlState == null)
                throw new InvalidOperationException("Float32 Character runtime has no Control state.");
            m_ControlState = CharacterControlRuntimeStateTransaction.Begin(
                m_BaseState.ControlState,
                schema,
                m_Tick);
            return m_ControlState;
        }

        internal void AcceptAbility(IFloat32AbilityExecutionStateTransaction transaction)
        {
            RequireActive();
            if (!(transaction is Float32AbilityExecutionStateTransaction ability) ||
                !ReferenceEquals(ability.Owner, this))
            {
                throw new InvalidOperationException("Float32 Ability transaction belongs to another Character runtime transaction.");
            }
            Float32AbilityRuntimeState state = ability.SnapshotState();
            m_AbilityStates[state.AbilityIdentity.AbilityId] = state;
        }

        internal Float32CharacterRuntimeState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            return Snapshot();
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

        internal IFloat32InputRequestStatePort InputRequests => m_InputRequestState;
        internal IFloat32ActionRuntimeStatePort ActionState => m_ActionState;
        internal IFloat32EventSequenceStatePort EventSequenceState => m_EventSequenceState;

        public SimulationGameplayEffectState GetGameplayEffectState(Float32GameplayEffectExecutionScratch scratch)
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

        public Float32CharacterRuntimeStateSavepoint CreateSavepoint()
        {
            RequireActive();
            var savepoint = new Float32CharacterRuntimeStateSavepoint(m_Savepoints.Count + 1, Snapshot());
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(Float32CharacterRuntimeStateSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            Apply(savepoint.Snapshot);
            m_Savepoints.Pop();
        }

        public void Release(Float32CharacterRuntimeStateSavepoint savepoint)
        {
            RequireActive();
            RequireTopSavepoint(savepoint);
            m_Savepoints.Pop();
        }

        public Float32CharacterRuntimeStateTransactionDiagnostics Diagnostics()
        {
            RequireActive();
            return new Float32CharacterRuntimeStateTransactionDiagnostics(m_Savepoints.Count);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Savepoints.Clear();
            m_ControlState?.Dispose();
            m_InputRequestState.Dispose();
            m_ActionState.Dispose();
            m_EventSequenceState.Dispose();
            m_Disposed = true;
        }

        Float32CharacterRuntimeState Snapshot()
        {
            return new Float32CharacterRuntimeState(
                m_BaseState.Installations,
                m_BaseState.NumericProfile,
                m_BaseState.GameplayContentHash,
                m_Tick.Value,
                m_AbilityStates.Values,
                m_ActionState.GetActionActivationRequests(),
                m_ActionState.GetActionInstances(),
                m_InputRequestState.Capture(),
                m_EventSequenceState.EventSequence,
                m_ActionState.ActionEventSequence,
                m_HandleAllocator,
                m_ControlState?.Capture() ?? m_BaseState.ControlState,
                m_GameplayEffectWorking?.Freeze() ?? m_GameplayEffectAggregate,
                m_EquipmentState);
        }

        void Apply(Float32CharacterRuntimeState state)
        {
            m_AbilityStates.Clear();
            for (int i = 0; i < state.Abilities.Count; i++)
            {
                Float32AbilityRuntimeState ability = state.Abilities[i];
                m_AbilityStates.Add(ability.AbilityIdentity.AbilityId, ability.Clone(m_Tick.Value));
            }
            m_ActionState.Restore(
                state.ActionActivationRequests,
                state.ActionInstances,
                state.ActionEventSequence);
            m_InputRequestState.Restore(state.InputRequests);
            m_EventSequenceState.Restore(state.EventSequence);
            m_HandleAllocator = state.HandleAllocator;
            if (m_ControlState != null)
            {
                if (state.ControlState == null)
                    throw new InvalidOperationException("Float32 Character runtime restore removed its bound Control state.");
                m_ControlState.Restore(state.ControlState);
            }
            m_GameplayEffectAggregate = state.GameplayEffectState;
            m_GameplayEffectWorking = m_GameplayEffectAggregate == null
                ? null
                : new SimulationGameplayEffectState(m_GameplayEffectCatalog, m_GameplayEffectAggregate, m_GameplayEffectScratch);
            m_EquipmentState = state.EquipmentState;
        }

        void RequireTopSavepoint(Float32CharacterRuntimeStateSavepoint savepoint)
        {
            if (savepoint == null || m_Savepoints.Count == 0 || !ReferenceEquals(savepoint, m_Savepoints.Peek()))
                throw new InvalidOperationException("Character runtime state savepoint is stale or unbalanced.");
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterRuntimeStateTransaction));
        }
    }

    internal sealed class Float32AbilityExecutionStateTransaction : IFloat32AbilityExecutionStateTransaction
    {
        readonly Float32CharacterRuntimeStateTransaction m_Owner;
        readonly Float32GameplayAbilityExecutionInstallation m_Installation;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly Float32GameplayAbilityExecutionData m_Ability;
        readonly Dictionary<int, CharacterStateValue> m_StateValues;
        readonly Dictionary<int, Float32MotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<CharacterStateValue> m_AbilityExecutionState;
        bool m_Aborted;
        bool m_Disposed;

        public Float32AbilityExecutionStateTransaction(
            Float32CharacterRuntimeStateTransaction owner,
            Float32GameplayAbilityExecutionInstallation installation,
            Float32AbilityRuntimeState state)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            m_Layout = installation.Layout;
            m_Ability = installation.Data;
            m_StateValues = new Dictionary<int, CharacterStateValue>(state.StateValues);
            m_MotionWarpStates = new Dictionary<int, Float32MotionWarpState>(state.MotionWarpStates);
            m_AbilityExecutionState = state.AbilityExecutionState.Clone();
        }

        internal Float32CharacterRuntimeStateTransaction Owner => m_Owner;

        public Float32GameplayAbilityExecutionInstallation Installation => m_Installation;

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

        internal Float32AbilityRuntimeState SnapshotState()
        {
            RequireActive();
            return new Float32AbilityRuntimeState(
                m_Installation,
                m_Owner.Tick.Value,
                m_StateValues,
                m_AbilityExecutionState,
                m_MotionWarpStates);
        }

        void RequireActive()
        {
            if (m_Aborted)
                throw new InvalidOperationException("Float32 Ability candidate was aborted.");
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32AbilityExecutionStateTransaction));
        }
    }
}
