using System;
using System.Collections.Generic;
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
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [CreateAssetMenu(fileName = "CharacterPipelineDefinition", menuName = "3C/Character/Pipeline Definition")]
    public sealed partial class CharacterPipelineDefinition : ScriptableObject
    {
        [SerializeField] BaseTreeAsset m_RootTreeAsset;
        [SerializeField, Min(1)] int m_SimulationTickRate = GameplayTickSettings.DefaultLocalLogicTickRate;
        [SerializeField] CharacterSimulationProgramAsset m_SimulationProgram;
        [SerializeField] CharacterPresentationProjectionAsset m_PresentationProjection;
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] CharacterGameplayEffectProfile m_GameplayEffectProfile;
        [SerializeField] CharacterBodyMotionProfile m_BodyMotionProfile;
        [SerializeField] CharacterAnimationPresentationProfile m_AnimationPresentationProfile;
        [SerializeField] bool m_EquipmentCapabilityEnabled;
        [SerializeField] CharacterEquipmentProfile m_EquipmentProfile;
        [SerializeField] CharacterEquipmentPresentationProfile m_EquipmentPresentationProfile;
        [SerializeField] ActionProfile[] m_ActionProfiles = Array.Empty<ActionProfile>();
        [SerializeField] GameplayBehaviorProfile[] m_BehaviorProfiles = Array.Empty<GameplayBehaviorProfile>();

        public BaseTreeAsset RootTreeAsset => m_RootTreeAsset;
        public int SimulationTickRate => Math.Max(1, m_SimulationTickRate);
        public CharacterSimulationProgramAsset SimulationProgram => m_SimulationProgram;
        public CharacterPresentationProjectionAsset PresentationProjection => m_PresentationProjection;
        public CharacterInputProfile InputProfile => m_InputProfile;
        public CharacterGameplayEffectProfile GameplayEffectProfile => m_GameplayEffectProfile;
        public CharacterBodyMotionProfile BodyMotionProfile => m_BodyMotionProfile;
        public CharacterAnimationPresentationProfile AnimationPresentationProfile => m_AnimationPresentationProfile;
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

            if (m_EquipmentCapabilityEnabled && m_EquipmentProfile)
            {
                IReadOnlyList<CharacterEquipmentFeatureDefinition> features = m_EquipmentProfile.Features;
                for (int featureIndex = 0; featureIndex < features.Count; featureIndex++)
                {
                    CharacterEquipmentFeatureDefinition feature = features[featureIndex];
                    if (!feature)
                        continue;
                    IReadOnlyList<EquipmentFeatureRouteImplementation> routes = feature.RouteImplementations;
                    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                    {
                        ActionProfile actionProfile = routes[routeIndex]?.ActionProfile;
                        if (actionProfile && string.Equals(actionProfile.BehaviorId, behaviorId, StringComparison.Ordinal))
                        {
                            profile = actionProfile;
                            return true;
                        }
                    }
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
        public void SetSimulationProgram(CharacterSimulationProgramAsset simulationProgram)
        {
            m_SimulationProgram = simulationProgram;
        }

        public void SetPresentationProjection(CharacterPresentationProjectionAsset presentationProjection)
        {
            m_PresentationProjection = presentationProjection;
        }
#endif
    }
}
