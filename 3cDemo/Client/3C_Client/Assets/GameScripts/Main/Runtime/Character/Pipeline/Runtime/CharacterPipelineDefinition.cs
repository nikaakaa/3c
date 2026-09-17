using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] AbilityGrant[] m_AbilityGrants = Array.Empty<AbilityGrant>();
        [SerializeField] TimelineAsset[] m_ControlMotionTimelines = Array.Empty<TimelineAsset>();
        [SerializeField, Min(1)] int m_SimulationTickRate = GameplayTickSettings.DefaultLocalLogicTickRate;
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] CharacterGameplayEffectProfile m_GameplayEffectProfile;
        [SerializeField] CharacterBodyMotionProfile m_BodyMotionProfile;
        [SerializeField] CharacterAnimationPresentationProfile m_AnimationPresentationProfile;
        [SerializeField] CharacterCameraProfile m_CameraProfile;
        [SerializeField] bool m_EquipmentCapabilityEnabled;
        [SerializeField] CharacterEquipmentProfile m_EquipmentProfile;
        [SerializeField] CharacterEquipmentPresentationProfile m_EquipmentPresentationProfile;
        [SerializeField] GameplayAbilityAdmissionProfile[] m_AdmissionProfiles = Array.Empty<GameplayAbilityAdmissionProfile>();
        [SerializeField] GameplayBehaviorProfile[] m_BehaviorProfiles = Array.Empty<GameplayBehaviorProfile>();

        public string ControlModuleId => string.IsNullOrWhiteSpace(m_ControlModuleId)
            ? string.Empty
            : m_ControlModuleId.Trim();
        public IReadOnlyList<CharacterControlParameterConfiguration> ControlParameters =>
            m_ControlParameters ?? Array.Empty<CharacterControlParameterConfiguration>();
        public IReadOnlyList<AbilityGrant> AbilityGrants =>
            m_AbilityGrants ?? Array.Empty<AbilityGrant>();
#if UNITY_EDITOR
        public IReadOnlyList<BtsmtlSkillFlowGraph> AbilityGraphs =>
            AbilityGrants
                .Where(value => value != null && value.Ability != null && value.Ability.AbilityGraph != null)
                .Select(value => value.Ability.AbilityGraph)
                .ToArray();
#endif
        public IReadOnlyList<TimelineAsset> ControlMotionTimelines =>
            m_ControlMotionTimelines ?? Array.Empty<TimelineAsset>();
        public int SimulationTickRate => Math.Max(1, m_SimulationTickRate);
        public CharacterInputProfile InputProfile => m_InputProfile;
        public CharacterGameplayEffectProfile GameplayEffectProfile => m_GameplayEffectProfile;
        public CharacterBodyMotionProfile BodyMotionProfile => m_BodyMotionProfile;
        public CharacterAnimationPresentationProfile AnimationPresentationProfile => m_AnimationPresentationProfile;
        public CharacterCameraProfile CameraProfile => m_CameraProfile;
        public bool EquipmentCapabilityEnabled => m_EquipmentCapabilityEnabled;
        public CharacterEquipmentProfile EquipmentProfile => m_EquipmentProfile;
        public CharacterEquipmentPresentationProfile EquipmentPresentationProfile => m_EquipmentPresentationProfile;
        public IReadOnlyList<GameplayAbilityAdmissionProfile> AdmissionProfiles =>
            m_AdmissionProfiles ?? Array.Empty<GameplayAbilityAdmissionProfile>();
        public IReadOnlyList<GameplayBehaviorProfile> BehaviorProfiles =>
            m_BehaviorProfiles ?? Array.Empty<GameplayBehaviorProfile>();

        public bool TryGetBehaviorProfile(string behaviorId, out IGameplayBehaviorProfile profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(behaviorId))
                return false;

            IReadOnlyList<GameplayAbilityAdmissionProfile> admissionProfiles = AdmissionProfiles;
            for (int i = 0; i < admissionProfiles.Count; i++)
            {
                GameplayAbilityAdmissionProfile admissionProfile = admissionProfiles[i];
                if (admissionProfile && string.Equals(admissionProfile.BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    profile = admissionProfile;
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
            IReadOnlyList<GameplayAbilityAdmissionProfile> compiledAdmissionProfiles = BuildCompiledAdmissionProfileCatalog();
            if (string.IsNullOrEmpty(ControlModuleId))
            {
                errors?.Add($"{name}: control module id is missing.");
                valid = false;
            }
            if (AbilityGrants.Count == 0)
            {
                errors?.Add($"{name}: ability grant list is missing.");
                valid = false;
            }
            IReadOnlyList<GameplayAbilityAdmissionProfile> profiles = AdmissionProfiles;
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

            if (m_CameraProfile)
                valid &= m_CameraProfile.CollectConfigurationErrors(errors);

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
                GameplayAbilityAdmissionProfile profile = profiles[i];
                if (!profile)
                {
                    errors?.Add($"{name}: admission profile #{i} is missing.");
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
            if (AbilityGrants.Count != 0)
                valid &= CollectAbilityGrantConfigurationErrors(compiledAdmissionProfiles, gameplayTagCatalog, errors);

            return valid;
        }

        bool CollectAbilityGrantConfigurationErrors(
            IReadOnlyList<GameplayAbilityAdmissionProfile> compiledAdmissionProfiles,
            GameplayTagCatalogRuntimeData gameplayTagCatalog,
            List<string> errors)
        {
            bool valid = true;
            var abilityIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < AbilityGrants.Count; i++)
            {
                AbilityGrant grant = AbilityGrants[i];
                if (grant == null)
                {
                    errors?.Add($"{name}: AbilityGrant #{i} is missing.");
                    valid = false;
                    continue;
                }
                valid &= grant.CollectConfigurationErrors(name, abilityIds, errors);
                GameplayAbilityDefinition ability = grant.Ability;
                if (!ability || !ability.AdmissionProfile)
                    continue;
                if (gameplayTagCatalog != null)
                    valid &= ability.CollectTagConfigurationErrors(gameplayTagCatalog, errors);
                bool registered = false;
                for (int profileIndex = 0; profileIndex < compiledAdmissionProfiles.Count; profileIndex++)
                {
                    if (ReferenceEquals(compiledAdmissionProfiles[profileIndex], ability.AdmissionProfile))
                    {
                        registered = true;
                        break;
                    }
                }
                if (!registered)
                {
                    errors?.Add($"{name}: AbilityGrant '{grant.AbilityId}' admission profile '{ability.AdmissionProfile.ActionId}' is not registered by the Definition catalog.");
                    valid = false;
                }
                for (int effectIndex = 0; effectIndex < ability.Effects.Count; effectIndex++)
                {
                    GameplayEffectDefinition effect = ability.Effects[effectIndex];
                    if (!effect || !GameplayEffectProfile || !GameplayEffectProfile.EffectDefinitions.Contains(effect))
                    {
                        errors?.Add($"{name}: AbilityGrant '{grant.AbilityId}' effect #{effectIndex} is not registered by the Definition catalog.");
                        valid = false;
                    }
                }
            }
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

        public void SetAbilityGrants(IEnumerable<AbilityGrant> grants)
        {
            m_AbilityGrants = (grants ?? System.Array.Empty<AbilityGrant>()).ToArray();
        }

        public void SetControlMotionTimelines(IEnumerable<TimelineAsset> timelines)
        {
            var values = new List<TimelineAsset>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (TimelineAsset timeline in timelines ?? Enumerable.Empty<TimelineAsset>())
            {
                if (!timeline || timeline.Data == null)
                    throw new ArgumentException("Character Timeline content contains an invalid Timeline asset.", nameof(timelines));
                if (!identities.Add(timeline.Data.AuthoringId))
                    throw new ArgumentException($"Character Timeline content contains duplicated AuthoringId '{timeline.Data.AuthoringId}'.", nameof(timelines));
                values.Add(timeline);
            }
            m_ControlMotionTimelines = values.ToArray();
        }

        public void SetAdmissionProfiles(IEnumerable<GameplayAbilityAdmissionProfile> profiles)
        {
            m_AdmissionProfiles = (profiles ?? System.Array.Empty<GameplayAbilityAdmissionProfile>()).ToArray();
        }

#endif
    }
}
