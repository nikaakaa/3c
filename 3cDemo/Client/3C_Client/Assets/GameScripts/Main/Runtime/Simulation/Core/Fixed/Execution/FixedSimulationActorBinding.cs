using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedMotionContributionScratch
    {
        SimulationMotionContribution[] m_Values = Array.Empty<SimulationMotionContribution>();

        public SimulationMotionContribution[] Values => m_Values;
        public int Count { get; private set; }

        public void Begin()
        {
            Array.Clear(m_Values, 0, Count);
            Count = 0;
        }

        public void Append(in SimulationMotionContribution value)
        {
            if (Count == m_Values.Length)
            {
                int capacity = Math.Max(4, m_Values.Length * 2);
                var values = new SimulationMotionContribution[capacity];
                Array.Copy(m_Values, values, Count);
                m_Values = values;
            }

            m_Values[Count++] = value;
        }

        public void Clear()
        {
            Array.Clear(m_Values, 0, Count);
            Count = 0;
        }
    }

    internal sealed class FixedCharacterEvaluationOutput
    {
        public List<GameplayFact> Facts { get; } = new List<GameplayFact>();
        public List<PresentationCommand> Presentation { get; } = new List<PresentationCommand>();
        public List<SimulationTraceRecord> Trace { get; } = new List<SimulationTraceRecord>();
        public List<SimulationTraceRecord> CharacterTrace { get; } = new List<SimulationTraceRecord>();

        public void Clear()
        {
            Facts.Clear();
            Presentation.Clear();
            Trace.Clear();
            CharacterTrace.Clear();
        }
    }

    public sealed class SimulationActorBinding
    {
        readonly FixedCharacterControlMotionRuntime m_ControlMotion;
        readonly FixedMotionContributionScratch m_MotionContributions = new FixedMotionContributionScratch();
        readonly FixedAbilityInvocationRuntime[] m_Invocations;
        readonly Dictionary<CharacterSkillId, IFixedAbilityActionControlPort> m_ActionRuntimes;
        readonly FixedGameplayEffectExecutionScratch m_EffectExecutionScratch = new FixedGameplayEffectExecutionScratch();
        readonly List<AbilityTimelineAdvancePending> m_TimelineAdvances = new List<AbilityTimelineAdvancePending>();
        readonly List<AbilityTimelineStopPending> m_TimelineStops = new List<AbilityTimelineStopPending>();
        readonly List<AbilityTimelineLogicMotion> m_TimelineLogicMotion = new List<AbilityTimelineLogicMotion>();
        readonly List<AbilityTimelineLogicMotionWarp> m_TimelineLogicMotionWarps =
            new List<AbilityTimelineLogicMotionWarp>();
        readonly FixedAbilityExecutionWorkspace[] m_Workspaces;
        readonly FixedCharacterEvaluationOutput m_EvaluationOutput = new FixedCharacterEvaluationOutput();
        readonly FixedCharacterTraceSink m_CharacterTraceSink;
        readonly FixedAbilityExecutionInput m_AbilityExecutionInput = new FixedAbilityExecutionInput();
        readonly FixedCharacterInputRequestState m_InputRequestState = new FixedCharacterInputRequestState();
        readonly FixedCharacterActionRuntimeState m_ActionState = new FixedCharacterActionRuntimeState();
        readonly CharacterControlRuntimeStateTransaction m_ControlState = new CharacterControlRuntimeStateTransaction();
        readonly FixedCharacterEventSequenceState m_EventSequenceState = new FixedCharacterEventSequenceState();
        readonly FixedCharacterHandleAllocatorState m_HandleAllocatorState = new FixedCharacterHandleAllocatorState();
        readonly FixedCharacterGameplayEffectRuntimeState m_GameplayEffectState = new FixedCharacterGameplayEffectRuntimeState();
        readonly FixedCharacterEquipmentRuntimeState m_EquipmentState = new FixedCharacterEquipmentRuntimeState();
        readonly FixedCharacterRuntimeStateTransaction m_RuntimeState = new FixedCharacterRuntimeStateTransaction();

        public SimulationActorBinding(
            ActorId actorId,
            string worldBodyBindingId,
            CharacterControlRuntimeBinding controlRuntimeBinding,
            CharacterBodyMotionBinding bodyMotionBinding,
            CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding,
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding,
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilityData,
            IAbilityTimelineRuntime timelineRuntime)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Actor identity is invalid.", nameof(actorId));
            AbilityTimelineMotionWarpCatalog timelineMotionWarpCatalog = GetMotionWarpCatalog(timelineRuntime);
            ActorId = actorId;
            WorldBodyBindingId = SimulationIdentity.Require(worldBodyBindingId, nameof(worldBodyBindingId));
            ControlRuntimeBinding = controlRuntimeBinding ?? throw new ArgumentNullException(nameof(controlRuntimeBinding));
            ControlMotionBindings = new FixedCharacterControlMotionBindingCatalog(controlRuntimeBinding.MotionBindings);
            m_ControlMotion = new FixedCharacterControlMotionRuntime(ControlMotionBindings);
            BodyMotionBinding = bodyMotionBinding ?? throw new ArgumentNullException(nameof(bodyMotionBinding));
            AbilityInstallations = new FixedGameplayAbilityExecutionInstallationSet(
                abilityData ?? throw new ArgumentNullException(nameof(abilityData)),
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                timelineMotionWarpCatalog);
            m_Invocations = new FixedAbilityInvocationRuntime[AbilityInstallations.Installations.Count];
            m_ActionRuntimes = new Dictionary<CharacterSkillId, IFixedAbilityActionControlPort>(
                AbilityInstallations.Installations.Count);
            m_Workspaces = new FixedAbilityExecutionWorkspace[AbilityInstallations.Installations.Count];
            for (int i = 0; i < m_Workspaces.Length; i++)
            {
                m_Workspaces[i] = new FixedAbilityExecutionWorkspace(
                    m_EffectExecutionScratch,
                    AbilityInstallations.Installations[i].Data,
                    AbilityInstallations.Installations[i].Layout,
                    m_TimelineAdvances,
                    m_TimelineStops);
            }
            for (int i = 0; i < AbilityInstallations.Installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = AbilityInstallations.Installations[i].Data;
                if (data.NumericProfile != FixedSimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Ability '{data.AbilityId}' does not target Fixed.");
            }
            GameplayEffectRuntimeBinding = gameplayEffectRuntimeBinding;
            EquipmentRuntimeBinding = equipmentRuntimeBinding;
            TimelineRuntime = timelineRuntime;
            IFixedAbilityDomainRuntimeFactory domainRuntimeFactory = new FixedAbilityDomainRuntimeFactory();
            IFixedAbilityExecutionServiceFactory serviceFactory = new FixedAbilityExecutionServiceFactory(timelineRuntime);
            for (int i = 0; i < AbilityInstallations.Installations.Count; i++)
            {
                FixedGameplayAbilityExecutionInstallation installation = AbilityInstallations.Installations[i];
                m_Invocations[i] = new FixedAbilityInvocationRuntime(
                    installation.Execution,
                    AbilityInstallations,
                    domainRuntimeFactory,
                    installation.EquipmentLayout,
                    m_Workspaces[i],
                    installation.Control,
                    serviceFactory);
            }

            m_CharacterTraceSink = new FixedCharacterTraceSink(m_EvaluationOutput.CharacterTrace);
            TimelineMotionReader = GetMotionReader(timelineRuntime);
            TimelineMotionWarpReader = GetMotionWarpReader(timelineRuntime);
            TimelineMotionWarpCatalog = timelineMotionWarpCatalog;
            StateSchemaHash = ComputeStateSchemaHash(
                controlRuntimeBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                AbilityInstallations,
                timelineMotionWarpCatalog);
            GameplayContentHash = ComputeGameplayContentHash(
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                AbilityInstallations,
                timelineMotionWarpCatalog);
        }

        public ActorId ActorId { get; }
        public string WorldBodyBindingId { get; }
        public CharacterControlRuntimeBinding ControlRuntimeBinding { get; }
        internal FixedCharacterControlMotionBindingCatalog ControlMotionBindings { get; }
        internal FixedCharacterControlMotionRuntime ControlMotion => m_ControlMotion;
        internal FixedMotionContributionScratch MotionContributions => m_MotionContributions;
        internal FixedAbilityInvocationRuntime[] Invocations => m_Invocations;
        internal Dictionary<CharacterSkillId, IFixedAbilityActionControlPort> ActionRuntimes => m_ActionRuntimes;
        internal FixedGameplayEffectExecutionScratch EffectExecutionScratch => m_EffectExecutionScratch;
        internal FixedAbilityExecutionWorkspace[] Workspaces => m_Workspaces;
        internal List<AbilityTimelineAdvancePending> TimelineAdvances => m_TimelineAdvances;
        internal List<AbilityTimelineStopPending> TimelineStops => m_TimelineStops;
        internal List<AbilityTimelineLogicMotion> TimelineLogicMotion => m_TimelineLogicMotion;
        internal List<AbilityTimelineLogicMotionWarp> TimelineLogicMotionWarps => m_TimelineLogicMotionWarps;
        internal FixedCharacterEvaluationOutput EvaluationOutput => m_EvaluationOutput;
        internal FixedCharacterTraceSink CharacterTraceSink => m_CharacterTraceSink;
        internal FixedAbilityExecutionInput AbilityExecutionInput => m_AbilityExecutionInput;
        internal FixedCharacterInputRequestState InputRequestState => m_InputRequestState;
        internal FixedCharacterActionRuntimeState ActionState => m_ActionState;
        internal CharacterControlRuntimeStateTransaction ControlState => m_ControlState;
        internal FixedCharacterEventSequenceState EventSequenceState => m_EventSequenceState;
        internal FixedCharacterHandleAllocatorState HandleAllocatorState => m_HandleAllocatorState;
        internal FixedCharacterGameplayEffectRuntimeState GameplayEffectState => m_GameplayEffectState;
        internal FixedCharacterEquipmentRuntimeState EquipmentState => m_EquipmentState;
        internal FixedCharacterRuntimeStateTransaction RuntimeState => m_RuntimeState;

        internal void ClearActionRuntimes()
        {
            m_ActionRuntimes.Clear();
        }

        internal void ClearWorkspaces()
        {
            for (int i = 0; i < m_Workspaces.Length; i++)
                m_Workspaces[i].Reset();
        }

        internal void ClearTimelineTransfers()
        {
            m_TimelineAdvances.Clear();
            m_TimelineStops.Clear();
        }

        internal void ClearTimelineMotionScratches()
        {
            m_TimelineLogicMotion.Clear();
            m_TimelineLogicMotionWarps.Clear();
        }

        public CharacterBodyMotionBinding BodyMotionBinding { get; }
        public CharacterGameplayEffectRuntimeBinding GameplayEffectRuntimeBinding { get; }
        public CharacterEquipmentRuntimeBinding EquipmentRuntimeBinding { get; }
        public IAbilityTimelineRuntime TimelineRuntime { get; }
        public IAbilityTimelineLogicMotionReader TimelineMotionReader { get; }
        public IAbilityTimelineLogicMotionWarpReader TimelineMotionWarpReader { get; }
        public AbilityTimelineMotionWarpCatalog TimelineMotionWarpCatalog { get; }
        public FixedGameplayAbilityExecutionInstallationSet AbilityInstallations { get; }
        public StableHash StateSchemaHash { get; }
        public StableHash GameplayContentHash { get; }

        static StableHash ComputeStateSchemaHash(
            CharacterControlRuntimeBinding control,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            FixedGameplayAbilityExecutionInstallationSet abilities,
            AbilityTimelineMotionWarpCatalog timelineMotionWarpCatalog)
        {
            var parts = new List<string>
            {
                "fixed-character-state-schema/1",
                control.BindingHash.ToString(),
                timelineMotionWarpCatalog.SchemaHash.ToString(),
                abilities.GameplayEffectCatalog != null ? gameplayEffects.BindingHash.ToString() : string.Empty,
                abilities.RequiresEquipment ? equipment.BindingHash.ToString() : string.Empty
            };
            for (int i = 0; i < abilities.Installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = abilities.Installations[i].Data;
                parts.Add(data.AbilityId.Value);
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.OperationSetVersion.Value);
                parts.Add(data.NumericProfile.Id.ToString());
            }
            return StableHash.Compute(parts.ToArray());
        }

        static StableHash ComputeGameplayContentHash(
            CharacterControlRuntimeBinding control,
            CharacterBodyMotionBinding bodyMotion,
            CharacterGameplayEffectRuntimeBinding gameplayEffects,
            CharacterEquipmentRuntimeBinding equipment,
            FixedGameplayAbilityExecutionInstallationSet abilities,
            AbilityTimelineMotionWarpCatalog timelineMotionWarpCatalog)
        {
            var parts = new List<string>
            {
                "fixed-simulation-actor-content/1",
                control.BindingHash.ToString(),
                bodyMotion.BindingHash.ToString(),
                timelineMotionWarpCatalog.ContentHash.ToString(),
                abilities.GameplayEffectCatalog != null ? gameplayEffects.BindingHash.ToString() : string.Empty,
                abilities.RequiresEquipment ? equipment.BindingHash.ToString() : string.Empty
            };
            for (int i = 0; i < abilities.Installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = abilities.Installations[i].Data;
                parts.Add(data.AbilityId.Value);
                parts.Add(data.ContentHash.ToString());
                parts.Add(data.StateSchemaHash.ToString());
                parts.Add(data.ExecutionIdentity);
            }
            return StableHash.Compute(parts.ToArray());
        }

        static IAbilityTimelineLogicMotionReader GetMotionReader(IAbilityTimelineRuntime timelineRuntime)
        {
            if (timelineRuntime == null)
                return null;
            return timelineRuntime is IAbilityTimelineLogicMotionReader motionReader
                ? motionReader
                : throw new ArgumentException("Fixed Timeline runtime must expose pending logic motion.", nameof(timelineRuntime));
        }

        static IAbilityTimelineLogicMotionWarpReader GetMotionWarpReader(IAbilityTimelineRuntime timelineRuntime)
        {
            if (timelineRuntime == null)
                return null;
            return timelineRuntime is IAbilityTimelineLogicMotionWarpReader motionWarpReader
                ? motionWarpReader
                : throw new ArgumentException("Fixed Timeline runtime must expose pending logic motion warps.", nameof(timelineRuntime));
        }

        static AbilityTimelineMotionWarpCatalog GetMotionWarpCatalog(IAbilityTimelineRuntime timelineRuntime)
        {
            if (timelineRuntime == null)
                return AbilityTimelineMotionWarpCatalog.Empty;
            return timelineRuntime is IAbilityTimelineMotionWarpCatalogProvider catalogProvider
                ? catalogProvider.MotionWarpCatalog
                : throw new ArgumentException("Fixed Timeline runtime must expose its MotionWarp state catalog.", nameof(timelineRuntime));
        }
    }
}
