using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        readonly ReadOnlyCollection<Float32AbilityRuntimeState> m_Abilities;

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
}
