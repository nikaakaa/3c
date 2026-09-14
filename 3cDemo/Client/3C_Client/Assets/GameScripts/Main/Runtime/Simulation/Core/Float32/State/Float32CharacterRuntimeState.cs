using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    public sealed class Float32AbilityRuntimeState
    {
        readonly Float32GameplayAbilityExecutionInstallation m_Installation;

        internal Float32AbilityRuntimeState(
            Float32GameplayAbilityExecutionInstallation installation,
            ulong lastCompletedTick,
            IDictionary<int, CharacterStateValue> stateValues,
            GameplayAbilityExecutionAggregate<CharacterStateValue> abilityExecutionState,
            IDictionary<string, SimulationInputRequestState> inputRequests,
            IDictionary<int, Float32ActionInstanceReference> timelineRetainedActionContexts,
            IDictionary<int, Float32MotionWarpState> motionWarpStates)
        {
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            LastCompletedTick = lastCompletedTick;
            StateValues = CopyValues(stateValues);
            AbilityExecutionState = abilityExecutionState?.Clone() ??
                new GameplayAbilityExecutionAggregate<CharacterStateValue>();
            InputRequests = inputRequests == null
                ? new Dictionary<string, SimulationInputRequestState>(StringComparer.Ordinal)
                : new Dictionary<string, SimulationInputRequestState>(inputRequests, StringComparer.Ordinal);
            TimelineRetainedActionContexts = timelineRetainedActionContexts == null
                ? new Dictionary<int, Float32ActionInstanceReference>()
                : new Dictionary<int, Float32ActionInstanceReference>(timelineRetainedActionContexts);
            MotionWarpStates = motionWarpStates == null
                ? new Dictionary<int, Float32MotionWarpState>()
                : new Dictionary<int, Float32MotionWarpState>(motionWarpStates);
        }

        internal Float32GameplayAbilityExecutionInstallation Installation => m_Installation;
        internal Dictionary<int, CharacterStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<CharacterStateValue> AbilityExecutionState { get; }
        internal Dictionary<string, SimulationInputRequestState> InputRequests { get; }
        internal Dictionary<int, Float32ActionInstanceReference> TimelineRetainedActionContexts { get; }
        internal Dictionary<int, Float32MotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_Installation.Identity;
        public ulong LastCompletedTick { get; }

        internal Float32AbilityRuntimeState Clone(ulong lastCompletedTick) =>
            new Float32AbilityRuntimeState(
                m_Installation,
                lastCompletedTick,
                StateValues,
                AbilityExecutionState,
                InputRequests,
                TimelineRetainedActionContexts,
                MotionWarpStates);

        static Dictionary<int, CharacterStateValue> CopyValues(IDictionary<int, CharacterStateValue> values)
        {
            var result = new Dictionary<int, CharacterStateValue>();
            if (values == null)
                return result;
            foreach (KeyValuePair<int, CharacterStateValue> value in values)
            {
                if (value.Key < 0 || !result.TryAdd(value.Key, value.Value))
                    throw new ArgumentException("Character runtime state values are invalid or duplicated.", nameof(values));
            }
            return result;
        }
    }

    public sealed class Float32CharacterRuntimeState
    {
        readonly Float32GameplayAbilityExecutionInstallationSet m_Installations;
        readonly System.Collections.ObjectModel.ReadOnlyCollection<Float32AbilityRuntimeState> m_Abilities;

        internal Float32CharacterRuntimeState(
            Float32GameplayAbilityExecutionInstallationSet installations,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            ulong lastCompletedTick,
            IEnumerable<Float32AbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<Float32ActionInstanceState> actionInstances,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            m_Installations = installations ?? throw new ArgumentNullException(nameof(installations));
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid)
                throw new ArgumentException("Character runtime state identity is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            LastCompletedTick = lastCompletedTick;
            var copied = abilities == null
                ? new List<Float32AbilityRuntimeState>()
                : new List<Float32AbilityRuntimeState>(abilities);
            copied.Sort((left, right) => left.AbilityIdentity.AbilityId.CompareTo(right.AbilityIdentity.AbilityId));
            if (copied.Count != m_Installations.Installations.Count)
                throw new ArgumentException("Character runtime state must contain one partition for every installed Ability.", nameof(abilities));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].AbilityIdentity.AbilityId == copied[i].AbilityIdentity.AbilityId)
                    throw new ArgumentException("Character runtime state Ability partitions are missing or duplicated.", nameof(abilities));
                m_Installations.Require(copied[i].AbilityIdentity.AbilityId).Identity.Require(copied[i].AbilityIdentity);
            }
            m_Abilities = copied.AsReadOnly();
            ActionActivationRequests = new List<SimulationActionActivationRequestState>(
                actionActivationRequests ?? Array.Empty<SimulationActionActivationRequestState>());
            ActionInstances = new List<Float32ActionInstanceState>(
                actionInstances ?? Array.Empty<Float32ActionInstanceState>());
            EventSequence = eventSequence;
            ActionEventSequence = actionEventSequence;
            HandleAllocator = handleAllocator;
            ControlState = controlState;
            GameplayEffectState = gameplayEffectState;
            EquipmentState = equipmentState;
        }

        internal Float32GameplayAbilityExecutionInstallationSet Installations => m_Installations;
        internal Float32AbilityRuntimeState RequireAbility(CharacterSkillId abilityId)
        {
            for (int i = 0; i < m_Abilities.Count; i++)
                if (m_Abilities[i].AbilityIdentity.AbilityId == abilityId)
                    return m_Abilities[i];
            throw new InvalidOperationException($"Character runtime state Ability '{abilityId}' is missing.");
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<Float32AbilityRuntimeState> Abilities => m_Abilities;
        internal List<SimulationActionActivationRequestState> ActionActivationRequests { get; }
        internal List<Float32ActionInstanceState> ActionInstances { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }

        internal static Float32CharacterRuntimeState CreateInitial(
            Float32GameplayAbilityExecutionInstallationSet installations,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (installations == null)
                throw new ArgumentNullException(nameof(installations));
            var abilities = new Float32AbilityRuntimeState[installations.Installations.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                Float32GameplayAbilityExecutionInstallation installation = installations.Installations[i];
                abilities[i] = new Float32AbilityRuntimeState(
                    installation,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null);
            }
            return new Float32CharacterRuntimeState(
                installations,
                numericProfile,
                gameplayContentHash,
                0,
                abilities,
                null,
                null,
                0,
                0,
                0,
                controlState,
                gameplayEffectState,
                equipmentState);
        }
    }

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
        ActorId ActorId { get; }
        SimulationTick Tick { get; }
        CharacterStateValue Get(int slotIndex);
        CharacterStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, CharacterStateValue value);
        void Set(TypedStateAddress address, CharacterStateValue value);
        void Reset(int slotIndex);
        ulong NextEventSequence();
        ulong NextActionEventSequence();
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
        GameplayAbilityExecutionAggregate<CharacterStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<CharacterStateValue> state);
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<Float32ActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<Float32ActionInstanceState> actions);
        Float32ActionInstanceReference GetTimelineRetainedActionContext(OperationHandle operation);
        void SetTimelineRetainedActionContext(OperationHandle operation, Float32ActionInstanceReference value);
        Float32MotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, Float32MotionWarpState value);
        SimulationGameplayEffectState GetGameplayEffectState(Float32GameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
        void Abort();
        Float32CharacterRuntimeStateSavepoint CreateSavepoint();
        void Restore(Float32CharacterRuntimeStateSavepoint savepoint);
        void Release(Float32CharacterRuntimeStateSavepoint savepoint);
        Float32CharacterRuntimeStateTransactionDiagnostics Diagnostics();
    }

    internal sealed class Float32CharacterRuntimeStateTransaction : IFloat32AbilityExecutionStateTransaction
    {
        readonly Float32GameplayAbilityExecutionInstallation m_Installation;
        readonly Float32GameplayAbilityExecutionData m_Ability;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly Float32CharacterRuntimeState m_BaseState;
        readonly Float32AbilityRuntimeState m_BaseAbilityState;
        readonly Float32GameplayEffectRuntimeCatalog m_GameplayEffectCatalog;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly Dictionary<int, CharacterStateValue> m_StateValues;
        readonly Stack<Float32CharacterRuntimeStateSavepoint> m_Savepoints =
            new Stack<Float32CharacterRuntimeStateSavepoint>();
        readonly Dictionary<string, SimulationInputRequestState> m_InputRequests;
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<Float32ActionInstanceState> m_ActionInstances;
        readonly Dictionary<int, Float32ActionInstanceReference> m_TimelineRetainedActionContexts;
        readonly Dictionary<int, Float32MotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<CharacterStateValue> m_AbilityExecutionState;
        GameplayEffectStateAggregate m_GameplayEffectAggregate;
        SimulationGameplayEffectState m_GameplayEffectWorking;
        Float32GameplayEffectExecutionScratch m_GameplayEffectScratch;
        EquipmentStateAggregate m_EquipmentState;
        ulong m_EventSequence;
        ulong m_ActionEventSequence;
        ulong m_HandleAllocator;
        bool m_Disposed;

        public Float32CharacterRuntimeStateTransaction(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32CharacterRuntimeState baseState,
            ActorId actorId,
            SimulationTick tick,
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog)
        {
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            m_Ability = installation.Data;
            m_Layout = installation.Layout;
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            m_BaseAbilityState = m_BaseState.RequireAbility(installation.Data.AbilityId);
            m_GameplayEffectCatalog = gameplayEffectCatalog;
            m_ActorId = actorId;
            m_Tick = tick;
            m_StateValues = new Dictionary<int, CharacterStateValue>(m_BaseAbilityState.StateValues);
            m_AbilityExecutionState = m_BaseAbilityState.AbilityExecutionState.Clone();
            m_InputRequests = new Dictionary<string, SimulationInputRequestState>(m_BaseAbilityState.InputRequests, StringComparer.Ordinal);
            m_ActionActivationRequests = new List<SimulationActionActivationRequestState>(baseState.ActionActivationRequests);
            m_ActionInstances = new List<Float32ActionInstanceState>(baseState.ActionInstances);
            m_TimelineRetainedActionContexts = new Dictionary<int, Float32ActionInstanceReference>(m_BaseAbilityState.TimelineRetainedActionContexts);
            m_MotionWarpStates = new Dictionary<int, Float32MotionWarpState>(m_BaseAbilityState.MotionWarpStates);
            m_GameplayEffectAggregate = baseState.GameplayEffectState;
            m_EquipmentState = baseState.EquipmentState;
            m_EventSequence = baseState.EventSequence;
            m_ActionEventSequence = baseState.ActionEventSequence;
            m_HandleAllocator = baseState.HandleAllocator;
        }

        public Float32GameplayAbilityExecutionInstallation Installation => m_Installation;
        public GameplayAbilityExecutionLayout Layout => m_Layout;
        public Float32CharacterRuntimeState BaseState => m_BaseState;
        public ActorId ActorId => m_ActorId;
        public SimulationTick Tick => m_Tick;

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

        public SimulationInputRequestState GetInputRequest(string requestId)
        {
            RequireActive();
            if (!m_Layout.HasInputRequest(requestId))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Input request '{requestId}'.");
            return m_InputRequests.TryGetValue(requestId ?? string.Empty, out SimulationInputRequestState state)
                ? state
                : default;
        }

        public void SetInputRequest(string requestId, SimulationInputRequestState state)
        {
            RequireActive();
            if (!m_Layout.HasInputRequest(requestId))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Input request '{requestId}'.");
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

        public IReadOnlyList<Float32ActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void SetActionInstances(IReadOnlyList<Float32ActionInstanceState> actions)
        {
            RequireActive();
            m_ActionInstances.Clear();
            if (actions != null)
                m_ActionInstances.AddRange(actions);
        }

        public Float32ActionInstanceReference GetTimelineRetainedActionContext(OperationHandle operation)
        {
            RequireActive();
            if (!m_Layout.HasTimelineRetention(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Timeline retention for '{operation}'.");
            return m_TimelineRetainedActionContexts.TryGetValue(operation.Value, out Float32ActionInstanceReference value)
                ? value
                : default;
        }

        public void SetTimelineRetainedActionContext(OperationHandle operation, Float32ActionInstanceReference value)
        {
            RequireActive();
            if (!m_Layout.HasTimelineRetention(operation))
                throw new InvalidOperationException($"Ability '{m_Ability.AbilityId}' has no Timeline retention for '{operation}'.");
            if (value.IsValid)
                m_TimelineRetainedActionContexts[operation.Value] = value;
            else
                m_TimelineRetainedActionContexts.Remove(operation.Value);
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
                throw new InvalidOperationException("Ability does not install Gameplay Effect state.");
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
                throw new InvalidOperationException("Ability does not install Gameplay Effect state.");
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
            var savepoint = new Float32CharacterRuntimeStateSavepoint(
                m_Savepoints.Count + 1,
                Snapshot());
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

        public Float32CharacterRuntimeState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            return Snapshot();
        }

        public Float32CharacterRuntimeStateTransactionDiagnostics Diagnostics()
        {
            RequireActive();
            return new Float32CharacterRuntimeStateTransactionDiagnostics(m_Savepoints.Count);
        }

        public void Abort()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            m_Disposed = true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Savepoints.Clear();
            m_Disposed = true;
        }

        Float32CharacterRuntimeState Snapshot()
        {
            var abilities = new Float32AbilityRuntimeState[m_BaseState.Abilities.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                Float32AbilityRuntimeState source = m_BaseState.Abilities[i];
                abilities[i] = source.AbilityIdentity.AbilityId == m_Installation.Data.AbilityId
                    ? new Float32AbilityRuntimeState(
                        m_Installation,
                        m_Tick.Value,
                        m_StateValues,
                        m_AbilityExecutionState,
                        m_InputRequests,
                        m_TimelineRetainedActionContexts,
                        m_MotionWarpStates)
                    : source.Clone(m_Tick.Value);
            }
            return new Float32CharacterRuntimeState(
                m_BaseState.Installations,
                m_BaseState.NumericProfile,
                m_BaseState.GameplayContentHash,
                m_Tick.Value,
                abilities,
                m_ActionActivationRequests,
                m_ActionInstances,
                m_EventSequence,
                m_ActionEventSequence,
                m_HandleAllocator,
                m_BaseState.ControlState,
                m_GameplayEffectWorking?.Freeze() ?? m_GameplayEffectAggregate,
                m_EquipmentState);
        }

        void Apply(Float32CharacterRuntimeState state)
        {
            Float32AbilityRuntimeState abilityState = state.RequireAbility(m_Installation.Data.AbilityId);
            m_StateValues.Clear();
            foreach (KeyValuePair<int, CharacterStateValue> value in abilityState.StateValues)
                m_StateValues.Add(value.Key, value.Value);
            m_AbilityExecutionState = abilityState.AbilityExecutionState.Clone();
            m_InputRequests.Clear();
            foreach (KeyValuePair<string, SimulationInputRequestState> value in abilityState.InputRequests)
                m_InputRequests.Add(value.Key, value.Value);
            m_ActionActivationRequests.Clear();
            m_ActionActivationRequests.AddRange(state.ActionActivationRequests);
            m_ActionInstances.Clear();
            m_ActionInstances.AddRange(state.ActionInstances);
            m_TimelineRetainedActionContexts.Clear();
            foreach (KeyValuePair<int, Float32ActionInstanceReference> value in abilityState.TimelineRetainedActionContexts)
                m_TimelineRetainedActionContexts.Add(value.Key, value.Value);
            m_MotionWarpStates.Clear();
            foreach (KeyValuePair<int, Float32MotionWarpState> value in abilityState.MotionWarpStates)
                m_MotionWarpStates.Add(value.Key, value.Value);
            m_EventSequence = state.EventSequence;
            m_ActionEventSequence = state.ActionEventSequence;
            m_HandleAllocator = state.HandleAllocator;
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
}
