using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Equipment;
using ThirdPersonGameplay.Contracts;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonGameplay.Tick;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [Serializable]
    public sealed class CharacterControlParameterConfiguration
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] SemanticValueKind m_ValueKind = SemanticValueKind.Number;
        [SerializeField] double m_NumericValue;

        public CharacterControlParameterConfiguration() { }

        public CharacterControlParameterConfiguration(
            string parameterId,
            SemanticValueKind valueKind,
            double numericValue)
        {
            m_ParameterId = parameterId ?? string.Empty;
            m_ValueKind = valueKind;
            m_NumericValue = numericValue;
        }

        public string ParameterId => m_ParameterId ?? string.Empty;
        public SemanticValueKind ValueKind => m_ValueKind;
        public double NumericValue => m_NumericValue;
    }

    [CreateAssetMenu(fileName = "CharacterPipelineDefinition", menuName = "3C/Character/Pipeline Definition")]
    public sealed partial class CharacterPipelineDefinition : ScriptableObject
    {
        [SerializeField] string m_ControlModuleId;
        [SerializeField] CharacterControlParameterConfiguration[] m_ControlParameters = Array.Empty<CharacterControlParameterConfiguration>();
        [SerializeField] CharacterSkillAuthoringDefinition[] m_SkillDefinitions = Array.Empty<CharacterSkillAuthoringDefinition>();
#if UNITY_EDITOR
        [SerializeField] BtsmtlSkillFlowGraph[] m_SkillGraphs = Array.Empty<BtsmtlSkillFlowGraph>();
#endif
        [SerializeField] TimelineData[] m_ControlMotionTimelines = Array.Empty<TimelineData>();
        [SerializeField, Min(1)] int m_SimulationTickRate = GameplayTickSettings.DefaultLocalLogicTickRate;
        [SerializeField] CharacterSimulationProgramAsset m_SimulationProgram;
        [SerializeField] CharacterPresentationProjectionAsset m_PresentationProjection;
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] CharacterGameplayEffectProfile m_GameplayEffectProfile;
        [SerializeField] CharacterBodyMotionProfile m_BodyMotionProfile;
        [SerializeField] CharacterAnimationPresentationProfile m_AnimationPresentationProfile;
        [SerializeField] CharacterCameraProfile m_CameraProfile;
        [SerializeField] bool m_EquipmentCapabilityEnabled;
        [SerializeField] CharacterEquipmentProfile m_EquipmentProfile;
        [SerializeField] CharacterEquipmentPresentationProfile m_EquipmentPresentationProfile;
        [SerializeField] ActionProfile[] m_ActionProfiles = Array.Empty<ActionProfile>();
        [SerializeField] GameplayBehaviorProfile[] m_BehaviorProfiles = Array.Empty<GameplayBehaviorProfile>();

        public string ControlModuleId => string.IsNullOrWhiteSpace(m_ControlModuleId)
            ? string.Empty
            : m_ControlModuleId.Trim();
        public IReadOnlyList<CharacterControlParameterConfiguration> ControlParameters =>
            m_ControlParameters ?? Array.Empty<CharacterControlParameterConfiguration>();
        public IReadOnlyList<CharacterSkillAuthoringDefinition> SkillDefinitions =>
            m_SkillDefinitions ?? Array.Empty<CharacterSkillAuthoringDefinition>();
#if UNITY_EDITOR
        public IReadOnlyList<BtsmtlSkillFlowGraph> SkillGraphs => m_SkillGraphs;
#endif
        public IReadOnlyList<TimelineData> ControlMotionTimelines =>
            m_ControlMotionTimelines ?? Array.Empty<TimelineData>();
        public int SimulationTickRate => Math.Max(1, m_SimulationTickRate);
        public CharacterSimulationProgramAsset SimulationProgram => m_SimulationProgram;
        public CharacterPresentationProjectionAsset PresentationProjection => m_PresentationProjection;
        public CharacterInputProfile InputProfile => m_InputProfile;
        public CharacterGameplayEffectProfile GameplayEffectProfile => m_GameplayEffectProfile;
        public CharacterBodyMotionProfile BodyMotionProfile => m_BodyMotionProfile;
        public CharacterAnimationPresentationProfile AnimationPresentationProfile => m_AnimationPresentationProfile;
        public CharacterCameraProfile CameraProfile => m_CameraProfile;
        public bool EquipmentCapabilityEnabled => m_EquipmentCapabilityEnabled;
        public CharacterEquipmentProfile EquipmentProfile => m_EquipmentProfile;
        public CharacterEquipmentPresentationProfile EquipmentPresentationProfile => m_EquipmentPresentationProfile;
        public IReadOnlyList<ActionProfile> ActionProfiles =>
            m_ActionProfiles ?? Array.Empty<ActionProfile>();
        public IReadOnlyList<GameplayBehaviorProfile> BehaviorProfiles =>
            m_BehaviorProfiles ?? Array.Empty<GameplayBehaviorProfile>();

        public bool TryGetBehaviorProfile(string behaviorId, out IGameplayBehaviorProfile profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(behaviorId))
                return false;

            IReadOnlyList<ActionProfile> actionProfiles = ActionProfiles;
            for (int i = 0; i < actionProfiles.Count; i++)
            {
                ActionProfile actionProfile = actionProfiles[i];
                if (actionProfile && string.Equals(actionProfile.BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    profile = actionProfile;
                    return true;
                }
            }

            IReadOnlyList<GameplayBehaviorProfile> behaviorProfiles = BehaviorProfiles;
            for (int i = 0; i < behaviorProfiles.Count; i++)
            {
                GameplayBehaviorProfile behaviorProfile = behaviorProfiles[i];
                if (behaviorProfile && string.Equals(behaviorProfile.BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    profile = behaviorProfile;
                    return true;
                }
            }

            IReadOnlyList<GameplayEffectDefinition> effectDefinitions = GameplayEffectProfile
                ? GameplayEffectProfile.EffectDefinitions
                : Array.Empty<GameplayEffectDefinition>();
            for (int i = 0; i < effectDefinitions.Count; i++)
            {
                GameplayEffectDefinition effectDefinition = effectDefinitions[i];
                if (effectDefinition && string.Equals(effectDefinition.BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    profile = effectDefinition;
                    return true;
                }
            }

            return false;
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            IReadOnlyList<ActionProfile> compiledActionProfiles = BuildCompiledActionProfileCatalog();
            if (string.IsNullOrEmpty(ControlModuleId))
            {
                errors?.Add($"{name}: control module id is missing.");
                valid = false;
            }
            HashSet<string> skillIds = new HashSet<string>(StringComparer.Ordinal);
            if (SkillDefinitions.Count == 0)
            {
                errors?.Add($"{name}: skill definition list is missing.");
                valid = false;
            }
            for (int i = 0; i < SkillDefinitions.Count; i++)
            {
                CharacterSkillAuthoringDefinition skill = SkillDefinitions[i];
                if (skill == null)
                    continue;
                if (string.IsNullOrEmpty(skill.SkillId) || !skillIds.Add(skill.SkillId))
                {
                    errors?.Add($"{name}: skill definition '{skill.SkillId}' is missing or duplicated.");
                    valid = false;
                }
            }
            for (int i = 0; i < SkillDefinitions.Count; i++)
            {
                CharacterSkillAuthoringDefinition skill = SkillDefinitions[i];
                if (skill == null)
                {
                    errors?.Add($"{name}: skill definition #{i} is missing.");
                    valid = false;
                    continue;
                }
                if (string.IsNullOrEmpty(skill.EntryGraphAuthoringId))
                {
                    errors?.Add($"{name}: skill '{skill.SkillId}' entry graph identity is missing.");
                    valid = false;
                }
                if (!skill.ActionProfile)
                {
                    errors?.Add($"{name}: skill '{skill.SkillId}' ActionProfile is missing.");
                    valid = false;
                }
                else
                {
                    bool actionProfileRegistered = false;
                    for (int profileIndex = 0; profileIndex < compiledActionProfiles.Count; profileIndex++)
                    {
                        if (ReferenceEquals(compiledActionProfiles[profileIndex], skill.ActionProfile))
                        {
                            actionProfileRegistered = true;
                            break;
                        }
                    }
                    if (!actionProfileRegistered)
                    {
                        errors?.Add($"{name}: skill '{skill.SkillId}' ActionProfile '{skill.ActionProfile.ActionId}' is not registered by the Definition catalog.");
                        valid = false;
                    }
                }
                if (!skill.ActionContext)
                {
                    errors?.Add($"{name}: skill '{skill.SkillId}' ActionContext is missing.");
                    valid = false;
                }

                var dependencyIds = new HashSet<string>(StringComparer.Ordinal);
                for (int dependencyIndex = 0; dependencyIndex < skill.SubgraphDependencies.Count; dependencyIndex++)
                {
                    CharacterSkillSubgraphDependencyConfiguration dependency = skill.SubgraphDependencies[dependencyIndex];
                    if (dependency == null ||
                        string.IsNullOrWhiteSpace(dependency.SubgraphIdentity) ||
                        string.IsNullOrWhiteSpace(dependency.CallSiteIdentity))
                    {
                        errors?.Add($"{name}: skill '{skill.SkillId}' contains an incomplete subgraph dependency.");
                        valid = false;
                        continue;
                    }
                    string dependencyId = $"{dependency.SubgraphIdentity}\u001f{dependency.CallSiteIdentity}";
                    if (!dependencyIds.Add(dependencyId))
                    {
                        errors?.Add($"{name}: skill '{skill.SkillId}' contains duplicate subgraph dependency '{dependency.SubgraphIdentity}/{dependency.CallSiteIdentity}'.");
                        valid = false;
                    }
                }

                var followUps = new HashSet<string>(StringComparer.Ordinal);
                for (int followUpIndex = 0; followUpIndex < skill.AllowedFollowUpSkillIds.Count; followUpIndex++)
                {
                    string followUpId = skill.AllowedFollowUpSkillIds[followUpIndex];
                    if (string.IsNullOrWhiteSpace(followUpId) ||
                        string.Equals(followUpId, skill.SkillId, StringComparison.Ordinal) ||
                        !followUps.Add(followUpId))
                    {
                        errors?.Add($"{name}: skill '{skill.SkillId}' contains an invalid, recursive, or duplicate follow-up skill '{followUpId}'.");
                        valid = false;
                        continue;
                    }
                    if (!skillIds.Contains(followUpId))
                    {
                        errors?.Add($"{name}: skill '{skill.SkillId}' references missing follow-up skill '{followUpId}'.");
                        valid = false;
                    }
                }
            }
            IReadOnlyList<ActionProfile> profiles = ActionProfiles;
            IReadOnlyList<GameplayBehaviorProfile> behaviorProfiles = BehaviorProfiles;
            if (profiles.Count == 0)
            {
                errors?.Add($"{name}: action profile list is missing.");
                valid = false;
            }

            if (!m_InputProfile)
            {
                errors?.Add($"{name}: input profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_InputProfile.CollectConfigurationErrors(errors);
            }

            if (!m_BodyMotionProfile)
            {
                errors?.Add($"{name}: Body Motion profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_BodyMotionProfile.CollectConfigurationErrors(errors);
            }

            if (!m_AnimationPresentationProfile)
            {
                errors?.Add($"{name}: Animation Presentation profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_AnimationPresentationProfile.CollectConfigurationErrors(errors);
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            GameplayTagCatalogRuntimeData gameplayTagCatalog = null;
            if (!m_GameplayEffectProfile)
            {
                errors?.Add($"{name}: Gameplay Effect profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_GameplayEffectProfile.CollectConfigurationErrors(out gameplayTagCatalog, errors);
            }
            IReadOnlyList<GameplayEffectDefinition> effectDefinitions = m_GameplayEffectProfile
                ? m_GameplayEffectProfile.EffectDefinitions
                : Array.Empty<GameplayEffectDefinition>();
            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                if (!profile)
                {
                    errors?.Add($"{name}: action profile #{i} is missing.");
                    valid = false;
                    continue;
                }

                valid &= profile.CollectConfigurationErrors(errors);
                if (gameplayTagCatalog != null)
                    valid &= profile.CollectTagConfigurationErrors(gameplayTagCatalog, errors);
                if (string.IsNullOrEmpty(profile.ActionId))
                    continue;

                if (!ids.Add(profile.ActionId))
                {
                    errors?.Add($"{name}: duplicate behavior id '{profile.ActionId}'.");
                    valid = false;
                }
            }

            for (int i = 0; i < behaviorProfiles.Count; i++)
            {
                GameplayBehaviorProfile profile = behaviorProfiles[i];
                if (!profile)
                {
                    errors?.Add($"{name}: behavior profile #{i} is missing.");
                    valid = false;
                    continue;
                }

                valid &= profile.CollectConfigurationErrors(errors);
                if (gameplayTagCatalog != null)
                    valid &= profile.CollectTagConfigurationErrors(gameplayTagCatalog, errors);
                if (string.IsNullOrEmpty(profile.BehaviorId))
                    continue;

                if (!ids.Add(profile.BehaviorId))
                {
                    errors?.Add($"{name}: duplicate behavior id '{profile.BehaviorId}'.");
                    valid = false;
                }
            }

            for (int i = 0; i < effectDefinitions.Count; i++)
            {
                GameplayEffectDefinition effect = effectDefinitions[i];
                if (!effect || !effect.EffectId.IsValid)
                    continue;
                if (!ids.Add(effect.BehaviorId))
                {
                    errors?.Add($"{name}: duplicate behavior id '{effect.BehaviorId}'.");
                    valid = false;
                }
            }

            valid &= CollectEquipmentConfigurationErrors(ids, gameplayTagCatalog, errors);

            return valid;
        }

#if UNITY_EDITOR
        public void SetControlConfiguration(
            CharacterControlModuleCatalog catalog,
            string controlModuleId,
            IEnumerable<CharacterControlParameterConfiguration> parameters)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            string moduleId = string.IsNullOrWhiteSpace(controlModuleId)
                ? string.Empty
                : controlModuleId.Trim();
            ICharacterControlModule module = catalog.Require(new CharacterControlModuleId(moduleId));
            var configurations = new List<CharacterControlParameterConfiguration>();
            var values = new List<CharacterControlParameterValue>();
            foreach (CharacterControlParameterConfiguration configuration in parameters ?? Array.Empty<CharacterControlParameterConfiguration>())
            {
                if (configuration == null)
                    throw new ArgumentException("Character control configuration contains a missing parameter.", nameof(parameters));
                if (string.IsNullOrWhiteSpace(configuration.ParameterId))
                    throw new ArgumentException("Character control configuration contains a parameter without an identity.", nameof(parameters));
                configurations.Add(new CharacterControlParameterConfiguration(
                    configuration.ParameterId.Trim(),
                    configuration.ValueKind,
                    configuration.NumericValue));
                values.Add(new CharacterControlParameterValue(
                    new CharacterControlParameterId(configuration.ParameterId.Trim()),
                    configuration.ValueKind,
                    configuration.NumericValue));
            }
            if (!module.Contract.TryResolveParameterSet(values, out _, out IReadOnlyList<string> errors))
                throw new ArgumentException(string.Join(" ", errors), nameof(parameters));
            m_ControlModuleId = moduleId;
            m_ControlParameters = configurations.ToArray();
        }

        public void SetSimulationProgram(CharacterSimulationProgramAsset simulationProgram)
        {
            m_SimulationProgram = simulationProgram;
        }

        public void SetPresentationProjection(CharacterPresentationProjectionAsset presentationProjection)
        {
            m_PresentationProjection = presentationProjection;
        }

        public void SetSkillDefinitions(CharacterSkillAuthoringDefinition[] skillDefinitions)
        {
            m_SkillDefinitions = skillDefinitions ?? Array.Empty<CharacterSkillAuthoringDefinition>();
        }

        public void SetSkillGraphs(BtsmtlSkillFlowGraph[] graphs)
        {
            if (graphs == null)
                throw new ArgumentNullException(nameof(graphs));
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillFlowGraph graph in graphs)
                if (graph == null || graph.Role != BtsmtlSkillFlowGraphRole.Skill || !identities.Add(graph.AuthoringId))
                    throw new ArgumentException("技能根图必须是身份唯一的正式Skill页面。", nameof(graphs));
            m_SkillGraphs = (BtsmtlSkillFlowGraph[])graphs.Clone();
        }
#endif
    }
}
