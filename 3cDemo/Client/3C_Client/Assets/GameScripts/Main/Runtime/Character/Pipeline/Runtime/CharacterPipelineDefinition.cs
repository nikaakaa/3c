using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonGameplay.Contracts;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tick;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [CreateAssetMenu(fileName = "CharacterPipelineDefinition", menuName = "3C/Character/Pipeline Definition")]
    public sealed class CharacterPipelineDefinition : ScriptableObject
    {
        [SerializeField] BaseTreeAsset m_RootTreeAsset;
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] CharacterGameplayEffectProfile m_GameplayEffectProfile;
        [SerializeField] CharacterAnimationPresentationDefinition m_AnimationPresentation = new CharacterAnimationPresentationDefinition();
        [SerializeField] ActionProfile[] m_ActionProfiles = Array.Empty<ActionProfile>();
        [SerializeField] GameplayBehaviorProfile[] m_BehaviorProfiles = Array.Empty<GameplayBehaviorProfile>();

        public BaseTreeAsset RootTreeAsset => m_RootTreeAsset;
        public RunnableTree RootTree => m_RootTreeAsset ? m_RootTreeAsset.Tree as RunnableTree : null;
        public CharacterInputProfile InputProfile => m_InputProfile;
        public CharacterGameplayEffectProfile GameplayEffectProfile => m_GameplayEffectProfile;
        public CharacterAnimationPresentationDefinition AnimationPresentation => m_AnimationPresentation;
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

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            GameplayEffectRuntimeDefinition gameplayEffectDefinition = null;
            if (!m_GameplayEffectProfile)
            {
                errors?.Add($"{name}: Gameplay Effect profile is missing.");
                valid = false;
            }
            else
            {
                valid &= m_GameplayEffectProfile.TryBuildRuntimeDefinition(
                    GameplayTickSettings.DefaultLocalLogicTickRate,
                    out gameplayEffectDefinition,
                    errors);
            }
            IReadOnlyList<GameplayEffectDefinition> effectDefinitions = m_GameplayEffectProfile
                ? m_GameplayEffectProfile.EffectDefinitions
                : Array.Empty<GameplayEffectDefinition>();
            valid &= CharacterAnimationPresentationBindingIndex.Build(
                m_AnimationPresentation,
                RootTree,
                errors).IsValid;
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
                if (gameplayEffectDefinition != null)
                    valid &= profile.CollectTagConfigurationErrors(gameplayEffectDefinition.TagCatalog, errors);
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
                if (gameplayEffectDefinition != null)
                    valid &= profile.CollectTagConfigurationErrors(gameplayEffectDefinition.TagCatalog, errors);
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

            return valid;
        }
    }
}
