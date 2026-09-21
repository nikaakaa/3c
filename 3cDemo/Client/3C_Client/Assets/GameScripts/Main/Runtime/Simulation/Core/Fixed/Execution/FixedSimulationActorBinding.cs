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

    public sealed class SimulationActorBinding
    {
        readonly FixedCharacterControlMotionRuntime m_ControlMotion;
        readonly FixedMotionContributionScratch m_MotionContributions = new FixedMotionContributionScratch();
        readonly FixedAbilityInvocationRuntime[] m_InvocationScratch;
        readonly Dictionary<CharacterSkillId, IFixedAbilityActionControlPort> m_ActionRuntimes;
        readonly FixedGameplayEffectExecutionScratch m_EffectExecutionScratch = new FixedGameplayEffectExecutionScratch();

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
            m_InvocationScratch = new FixedAbilityInvocationRuntime[AbilityInstallations.Installations.Count];
            m_ActionRuntimes = new Dictionary<CharacterSkillId, IFixedAbilityActionControlPort>(
                AbilityInstallations.Installations.Count);
            for (int i = 0; i < AbilityInstallations.Installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData data = AbilityInstallations.Installations[i].Data;
                if (data.NumericProfile != FixedSimulationNumericProfile.Value)
                    throw new InvalidOperationException($"Ability '{data.AbilityId}' does not target Fixed.");
            }
            GameplayEffectRuntimeBinding = gameplayEffectRuntimeBinding;
            EquipmentRuntimeBinding = equipmentRuntimeBinding;
            TimelineRuntime = timelineRuntime;
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
        internal FixedAbilityInvocationRuntime[] InvocationScratch => m_InvocationScratch;
        internal Dictionary<CharacterSkillId, IFixedAbilityActionControlPort> ActionRuntimes => m_ActionRuntimes;
        internal FixedGameplayEffectExecutionScratch EffectExecutionScratch => m_EffectExecutionScratch;

        internal void ClearInvocationScratch(int count)
        {
            Array.Clear(m_InvocationScratch, 0, count);
        }

        internal void ClearActionRuntimes()
        {
            m_ActionRuntimes.Clear();
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
