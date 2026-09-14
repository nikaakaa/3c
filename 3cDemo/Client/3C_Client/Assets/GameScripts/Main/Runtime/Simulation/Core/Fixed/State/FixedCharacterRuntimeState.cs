using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityRuntimeState
    {
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;

        internal FixedAbilityRuntimeState(
            FixedGameplayAbilityExecutionInstallation installation,
            ulong lastCompletedTick,
            IDictionary<int, CharacterStateValue> stateValues,
            GameplayAbilityExecutionAggregate<CharacterStateValue> abilityExecutionState,
            IDictionary<string, SimulationInputRequestState> inputRequests,
            IDictionary<int, FixedActionInstanceReference> timelineRetainedActionContexts,
            IDictionary<int, FixedMotionWarpState> motionWarpStates)
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
                ? new Dictionary<int, FixedActionInstanceReference>()
                : new Dictionary<int, FixedActionInstanceReference>(timelineRetainedActionContexts);
            MotionWarpStates = motionWarpStates == null
                ? new Dictionary<int, FixedMotionWarpState>()
                : new Dictionary<int, FixedMotionWarpState>(motionWarpStates);
        }

        internal FixedGameplayAbilityExecutionInstallation Installation => m_Installation;
        internal Dictionary<int, CharacterStateValue> StateValues { get; }
        internal GameplayAbilityExecutionAggregate<CharacterStateValue> AbilityExecutionState { get; }
        internal Dictionary<string, SimulationInputRequestState> InputRequests { get; }
        internal Dictionary<int, FixedActionInstanceReference> TimelineRetainedActionContexts { get; }
        internal Dictionary<int, FixedMotionWarpState> MotionWarpStates { get; }
        public GameplayAbilityExecutionIdentity AbilityIdentity => m_Installation.Identity;
        public ulong LastCompletedTick { get; }

        internal FixedAbilityRuntimeState Clone(ulong lastCompletedTick) =>
            new FixedAbilityRuntimeState(
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

    public sealed class FixedCharacterRuntimeState
    {
        readonly FixedGameplayAbilityExecutionInstallationSet m_Installations;
        readonly System.Collections.ObjectModel.ReadOnlyCollection<FixedAbilityRuntimeState> m_Abilities;

        internal FixedCharacterRuntimeState(
            FixedGameplayAbilityExecutionInstallationSet installations,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            ulong lastCompletedTick,
            IEnumerable<FixedAbilityRuntimeState> abilities,
            IEnumerable<SimulationActionActivationRequestState> actionActivationRequests,
            IEnumerable<FixedActionInstanceState> actionInstances,
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
                ? new List<FixedAbilityRuntimeState>()
                : new List<FixedAbilityRuntimeState>(abilities);
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
            ActionInstances = new List<FixedActionInstanceState>(
                actionInstances ?? Array.Empty<FixedActionInstanceState>());
            EventSequence = eventSequence;
            ActionEventSequence = actionEventSequence;
            HandleAllocator = handleAllocator;
            ControlState = controlState;
            GameplayEffectState = gameplayEffectState;
            EquipmentState = equipmentState;
        }

        internal FixedGameplayAbilityExecutionInstallationSet Installations => m_Installations;
        internal FixedAbilityRuntimeState RequireAbility(CharacterSkillId abilityId)
        {
            for (int i = 0; i < m_Abilities.Count; i++)
                if (m_Abilities[i].AbilityIdentity.AbilityId == abilityId)
                    return m_Abilities[i];
            throw new InvalidOperationException($"Character runtime state Ability '{abilityId}' is missing.");
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<FixedAbilityRuntimeState> Abilities => m_Abilities;
        internal List<SimulationActionActivationRequestState> ActionActivationRequests { get; }
        internal List<FixedActionInstanceState> ActionInstances { get; }
        internal ulong EventSequence { get; }
        internal ulong ActionEventSequence { get; }
        internal ulong HandleAllocator { get; }
        internal CharacterControlRuntimeState ControlState { get; }
        internal GameplayEffectStateAggregate GameplayEffectState { get; }
        internal EquipmentStateAggregate EquipmentState { get; }

        internal static FixedCharacterRuntimeState CreateInitial(
            FixedGameplayAbilityExecutionInstallationSet installations,
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            CharacterControlRuntimeState controlState,
            GameplayEffectStateAggregate gameplayEffectState,
            EquipmentStateAggregate equipmentState)
        {
            if (installations == null)
                throw new ArgumentNullException(nameof(installations));
            var abilities = new FixedAbilityRuntimeState[installations.Installations.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                FixedGameplayAbilityExecutionInstallation installation = installations.Installations[i];
                abilities[i] = new FixedAbilityRuntimeState(
                    installation,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null);
            }
            return new FixedCharacterRuntimeState(
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
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
        FixedActionInstanceReference GetTimelineRetainedActionContext(OperationHandle operation);
        void SetTimelineRetainedActionContext(OperationHandle operation, FixedActionInstanceReference value);
        FixedMotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, FixedMotionWarpState value);
        void Abort();
    }

    internal interface IFixedAbilityDomainStatePort
    {
        ActorId ActorId { get; }
        SimulationTick Tick { get; }
        int TickRate { get; }
        ulong NextEventSequence();
        ulong NextActionEventSequence();
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<FixedActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<FixedActionInstanceState> actions);
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

    internal sealed class FixedCharacterRuntimeStateTransaction :
        IFixedAbilityExecutionStateTransaction,
        IFixedAbilityDomainStatePort
    {
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;
        readonly FixedGameplayAbilityExecutionData m_Ability;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly FixedCharacterRuntimeState m_BaseState;
        readonly FixedAbilityRuntimeState m_BaseAbilityState;
        readonly FixedGameplayEffectRuntimeCatalog m_GameplayEffectCatalog;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly Dictionary<int, CharacterStateValue> m_StateValues;
        readonly Stack<FixedCharacterRuntimeStateSavepoint> m_Savepoints =
            new Stack<FixedCharacterRuntimeStateSavepoint>();
        readonly Dictionary<string, SimulationInputRequestState> m_InputRequests;
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<FixedActionInstanceState> m_ActionInstances;
        readonly Dictionary<int, FixedActionInstanceReference> m_TimelineRetainedActionContexts;
        readonly Dictionary<int, FixedMotionWarpState> m_MotionWarpStates;
        GameplayAbilityExecutionAggregate<CharacterStateValue> m_AbilityExecutionState;
        GameplayEffectStateAggregate m_GameplayEffectAggregate;
        SimulationGameplayEffectState m_GameplayEffectWorking;
        FixedGameplayEffectExecutionScratch m_GameplayEffectScratch;
        EquipmentStateAggregate m_EquipmentState;
        ulong m_EventSequence;
        ulong m_ActionEventSequence;
        ulong m_HandleAllocator;
        bool m_Disposed;

        public FixedCharacterRuntimeStateTransaction(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedCharacterRuntimeState baseState,
            ActorId actorId,
            SimulationTick tick,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog)
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
            m_ActionInstances = new List<FixedActionInstanceState>(baseState.ActionInstances);
            m_TimelineRetainedActionContexts = new Dictionary<int, FixedActionInstanceReference>(m_BaseAbilityState.TimelineRetainedActionContexts);
            m_MotionWarpStates = new Dictionary<int, FixedMotionWarpState>(m_BaseAbilityState.MotionWarpStates);
            m_GameplayEffectAggregate = baseState.GameplayEffectState;
            m_EquipmentState = baseState.EquipmentState;
            m_EventSequence = baseState.EventSequence;
            m_ActionEventSequence = baseState.ActionEventSequence;
            m_HandleAllocator = baseState.HandleAllocator;
        }

        public FixedGameplayAbilityExecutionInstallation Installation => m_Installation;
        public GameplayAbilityExecutionLayout Layout => m_Layout;
        public FixedCharacterRuntimeState BaseState => m_BaseState;
        public ActorId ActorId => m_ActorId;
        public SimulationTick Tick => m_Tick;
        public int TickRate => m_Installation.Data.TickRate;

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

        public FixedCharacterRuntimeStateSavepoint CreateSavepoint()
        {
            RequireActive();
            var savepoint = new FixedCharacterRuntimeStateSavepoint(
                m_Savepoints.Count + 1,
                Snapshot());
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

        public FixedCharacterRuntimeState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character runtime state transaction has active savepoints.");
            return Snapshot();
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
            m_Disposed = true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Savepoints.Clear();
            m_Disposed = true;
        }

        FixedCharacterRuntimeState Snapshot()
        {
            var abilities = new FixedAbilityRuntimeState[m_BaseState.Abilities.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                FixedAbilityRuntimeState source = m_BaseState.Abilities[i];
                abilities[i] = source.AbilityIdentity.AbilityId == m_Installation.Data.AbilityId
                    ? new FixedAbilityRuntimeState(
                        m_Installation,
                        m_Tick.Value,
                        m_StateValues,
                        m_AbilityExecutionState,
                        m_InputRequests,
                        m_TimelineRetainedActionContexts,
                        m_MotionWarpStates)
                    : source.Clone(m_Tick.Value);
            }
            return new FixedCharacterRuntimeState(
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

        void Apply(FixedCharacterRuntimeState state)
        {
            FixedAbilityRuntimeState abilityState = state.RequireAbility(m_Installation.Data.AbilityId);
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
            foreach (KeyValuePair<int, FixedActionInstanceReference> value in abilityState.TimelineRetainedActionContexts)
                m_TimelineRetainedActionContexts.Add(value.Key, value.Value);
            m_MotionWarpStates.Clear();
            foreach (KeyValuePair<int, FixedMotionWarpState> value in abilityState.MotionWarpStates)
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
}

