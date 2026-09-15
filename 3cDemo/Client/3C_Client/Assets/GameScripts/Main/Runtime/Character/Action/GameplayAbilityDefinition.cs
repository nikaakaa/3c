using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem
{
    [Serializable]
    public sealed class GameplayAbilitySubgraphDependencyConfiguration
    {
        [SerializeField] string m_SubgraphIdentity;
        [SerializeField] string m_CallSiteIdentity;

        public string SubgraphIdentity => m_SubgraphIdentity ?? string.Empty;
        public string CallSiteIdentity => m_CallSiteIdentity ?? string.Empty;

        public GameplayAbilitySubgraphDependencyConfiguration() { }

        public GameplayAbilitySubgraphDependencyConfiguration(string subgraphIdentity, string callSiteIdentity)
        {
            m_SubgraphIdentity = subgraphIdentity ?? string.Empty;
            m_CallSiteIdentity = callSiteIdentity ?? string.Empty;
        }
    }

    public enum GameplayAbilityEndTrigger : byte
    {
        ExecutionCompleted = 0,
        CancelRequested = 1,
        InterruptRequested = 2,
        AbortRequested = 3,
        ActionWindowClosed = 4
    }

    [Serializable]
    public sealed class GameplayAbilityEndRule
    {
        [SerializeField] GameplayAbilityEndTrigger m_Trigger;
        [SerializeField] ActionLifecycleTransitionType m_TransitionType;
        [SerializeField] string m_ActionWindowType;
        [SerializeField] string m_Reason;

        public GameplayAbilityEndTrigger Trigger => m_Trigger;
        public ActionLifecycleTransitionType TransitionType => m_TransitionType;
        public string ActionWindowType => string.IsNullOrWhiteSpace(m_ActionWindowType) ? string.Empty : m_ActionWindowType.Trim();
        public string Reason => string.IsNullOrWhiteSpace(m_Reason) ? string.Empty : m_Reason.Trim();

#if UNITY_EDITOR
        public void Configure(
            GameplayAbilityEndTrigger trigger,
            ActionLifecycleTransitionType transitionType,
            string actionWindowType,
            string reason)
        {
            if (!Enum.IsDefined(typeof(GameplayAbilityEndTrigger), trigger) ||
                !GameplayAbilityDefinition.IsTerminalTransition(transitionType) ||
                (trigger == GameplayAbilityEndTrigger.ActionWindowClosed && string.IsNullOrWhiteSpace(actionWindowType)))
                throw new ArgumentException("Gameplay Ability end rule is invalid.");
            m_Trigger = trigger;
            m_TransitionType = transitionType;
            m_ActionWindowType = actionWindowType ?? string.Empty;
            m_Reason = reason ?? string.Empty;
        }
#endif
    }

    [CreateAssetMenu(fileName = "GameplayAbilityDefinition", menuName = "3C/Character/Gameplay Ability Definition")]
    public sealed class GameplayAbilityDefinition : ScriptableObject, IGameplayBehaviorProfile
    {
        [SerializeField] string m_AbilityId;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_DebugCategory;
        [SerializeField] GameplayTagId[] m_Tags = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayAbilityAdmissionProfile m_AdmissionProfile;
        [SerializeField] GameplayEffectDefinition[] m_Effects = Array.Empty<GameplayEffectDefinition>();
        [SerializeField] GameplayAbilityEndRule[] m_EndRules = Array.Empty<GameplayAbilityEndRule>();
        [SerializeField] GameplayAbilitySubgraphDependencyConfiguration[] m_SubgraphDependencies = Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>();
        [SerializeField] string[] m_AllowedFollowUpAbilityIds = Array.Empty<string>();
#if UNITY_EDITOR
        [SerializeField] BtsmtlSkillFlowGraph m_AbilityGraph;
#endif

        public string AbilityId => string.IsNullOrWhiteSpace(m_AbilityId) ? string.Empty : m_AbilityId.Trim();
        public string ExecutionContextId => string.IsNullOrEmpty(AbilityId) ? string.Empty : $"ability:{AbilityId}";
        public string BehaviorId => AbilityId;
        public GameplayBehaviorKind BehaviorKind => GameplayBehaviorKind.Transaction;
        public string DisplayName => string.IsNullOrWhiteSpace(m_DisplayName) ? string.Empty : m_DisplayName.Trim();
        public string DebugCategory => string.IsNullOrWhiteSpace(m_DebugCategory) ? string.Empty : m_DebugCategory.Trim();
        public IReadOnlyList<GameplayTagId> Tags => m_Tags ?? Array.Empty<GameplayTagId>();
        public GameplayAbilityAdmissionProfile AdmissionProfile => m_AdmissionProfile;
        public ActionTargetRequirement TargetRequirement =>
            m_AdmissionProfile ? m_AdmissionProfile.TargetRequirement : ActionTargetRequirement.None;
        public IReadOnlyList<GameplayEffectDefinition> Effects => m_Effects ?? Array.Empty<GameplayEffectDefinition>();
        public IReadOnlyList<GameplayAbilityEndRule> EndRules => m_EndRules ?? Array.Empty<GameplayAbilityEndRule>();
        public IReadOnlyList<GameplayAbilitySubgraphDependencyConfiguration> SubgraphDependencies =>
            m_SubgraphDependencies ?? Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>();
        public IReadOnlyList<string> AllowedFollowUpAbilityIds => m_AllowedFollowUpAbilityIds ?? Array.Empty<string>();
#if UNITY_EDITOR
        public BtsmtlSkillFlowGraph AbilityGraph => m_AbilityGraph;
#endif

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrEmpty(AbilityId))
            {
                errors?.Add($"{name}: Gameplay Ability identity is missing.");
                valid = false;
            }
            if (string.IsNullOrEmpty(DisplayName))
            {
                errors?.Add($"{name}: Gameplay Ability display name is missing.");
                valid = false;
            }
            if (!m_AdmissionProfile)
            {
                errors?.Add($"{name}: Gameplay Ability admission profile is missing.");
                valid = false;
            }
            valid &= ValidateUniqueTags(Tags, errors);
#if UNITY_EDITOR
            if (!m_AbilityGraph)
            {
                errors?.Add($"{name}: Gameplay Ability private graph is missing.");
                valid = false;
            }
#endif
            for (int i = 0; i < Effects.Count; i++)
            {
                if (!Effects[i])
                {
                    errors?.Add($"{name}: Gameplay Ability effect #{i} is missing.");
                    valid = false;
                }
            }
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < SubgraphDependencies.Count; i++)
            {
                GameplayAbilitySubgraphDependencyConfiguration dependency = SubgraphDependencies[i];
                string key = dependency == null
                    ? string.Empty
                    : $"{dependency.SubgraphIdentity}\u001f{dependency.CallSiteIdentity}";
                if (dependency == null ||
                    string.IsNullOrEmpty(dependency.SubgraphIdentity) ||
                    string.IsNullOrEmpty(dependency.CallSiteIdentity) ||
                    !dependencies.Add(key))
                {
                    errors?.Add($"{name}: Gameplay Ability subgraph dependency #{i} is invalid or duplicated.");
                    valid = false;
                }
            }
            var followUps = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < AllowedFollowUpAbilityIds.Count; i++)
            {
                string followUp = AllowedFollowUpAbilityIds[i];
                if (string.IsNullOrEmpty(followUp) ||
                    string.Equals(followUp, AbilityId, StringComparison.Ordinal) ||
                    !followUps.Add(followUp))
                {
                    errors?.Add($"{name}: Gameplay Ability follow-up '{followUp}' is invalid or duplicated.");
                    valid = false;
                }
            }
            for (int i = 0; i < EndRules.Count; i++)
            {
                GameplayAbilityEndRule rule = EndRules[i];
                if (rule == null ||
                    !Enum.IsDefined(typeof(GameplayAbilityEndTrigger), rule.Trigger) ||
                    !IsTerminalTransition(rule.TransitionType) ||
                    (rule.Trigger == GameplayAbilityEndTrigger.ActionWindowClosed && string.IsNullOrEmpty(rule.ActionWindowType)))
                {
                    errors?.Add($"{name}: Gameplay Ability end rule #{i} is invalid.");
                    valid = false;
                }
            }
            return valid;
        }

        public bool CollectTagConfigurationErrors(
            GameplayTagCatalogRuntimeData catalog,
            List<string> errors)
        {
            if (catalog == null)
                return false;
            bool valid = true;
            for (int i = 0; i < Tags.Count; i++)
            {
                if (!catalog.Contains(Tags[i]))
                {
                    errors?.Add($"{name}: Gameplay Ability tag '{Tags[i]}' is not registered.");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateUniqueTags(IReadOnlyList<GameplayTagId> values, List<string> errors)
        {
            bool valid = true;
            var seen = new HashSet<GameplayTagId>();
            for (int i = 0; i < values.Count; i++)
            {
                GameplayTagId tag = values[i];
                if (!tag.IsValid || !seen.Add(tag))
                {
                    errors?.Add($"Gameplay Ability tag #{i} is invalid or duplicated.");
                    valid = false;
                }
            }
            return valid;
        }

        internal static bool IsTerminalTransition(ActionLifecycleTransitionType transitionType) =>
            transitionType == ActionLifecycleTransitionType.Complete ||
            transitionType == ActionLifecycleTransitionType.Cancel ||
            transitionType == ActionLifecycleTransitionType.Interrupt ||
            transitionType == ActionLifecycleTransitionType.Abort;

#if UNITY_EDITOR
        public void ConfigureIdentity(string abilityId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                throw new ArgumentException("Gameplay Ability identity is required.", nameof(abilityId));
            m_AbilityId = abilityId.Trim();
            m_DisplayName = string.IsNullOrWhiteSpace(displayName) ? m_AbilityId : displayName.Trim();
        }

        public void ConfigureMetadata(string debugCategory, IEnumerable<GameplayTagId> tags)
        {
            m_DebugCategory = debugCategory ?? string.Empty;
            m_Tags = (tags ?? Enumerable.Empty<GameplayTagId>()).ToArray();
        }

        public void ConfigureAdmissionProfile(GameplayAbilityAdmissionProfile admissionProfile)
        {
            m_AdmissionProfile = admissionProfile;
        }

        public void ConfigureEffects(IEnumerable<GameplayEffectDefinition> effects)
        {
            m_Effects = (effects ?? Enumerable.Empty<GameplayEffectDefinition>()).ToArray();
        }

        public void ConfigureEndRules(IEnumerable<GameplayAbilityEndRule> endRules)
        {
            m_EndRules = (endRules ?? Enumerable.Empty<GameplayAbilityEndRule>()).ToArray();
        }

        public void ConfigureSubgraphDependencies(
            IEnumerable<GameplayAbilitySubgraphDependencyConfiguration> dependencies)
        {
            m_SubgraphDependencies = (dependencies ??
                Enumerable.Empty<GameplayAbilitySubgraphDependencyConfiguration>()).ToArray();
        }

        public void ConfigureFollowUps(IEnumerable<string> abilityIds)
        {
            m_AllowedFollowUpAbilityIds = (abilityIds ?? Enumerable.Empty<string>())
                .Select(value => value?.Trim() ?? string.Empty)
                .ToArray();
        }

        public void SetAbilityGraph(BtsmtlSkillFlowGraph graph)
        {
            if (graph != null && graph.Role != BtsmtlSkillFlowGraphRole.Skill)
                throw new ArgumentException("Gameplay Ability graph must be a Skill-role FlowGraph.", nameof(graph));
            m_AbilityGraph = graph;
        }
#endif
    }

    [Serializable]
    public sealed class AbilityGrant
    {
        [SerializeField] GameplayAbilityDefinition m_Ability;
        [SerializeField] string m_SourceInputRequestId;
        [SerializeField] bool m_ConsumeSourceInputRequest = true;
        [SerializeField] string m_TargetInputValueId;
        [SerializeField] string m_TargetKey;

        public GameplayAbilityDefinition Ability => m_Ability;
        public string AbilityId => m_Ability ? m_Ability.AbilityId : string.Empty;
        public string SourceInputRequestId => string.IsNullOrWhiteSpace(m_SourceInputRequestId) ? string.Empty : m_SourceInputRequestId.Trim();
        public bool ConsumeSourceInputRequest => m_ConsumeSourceInputRequest;
        public string TargetInputValueId => string.IsNullOrWhiteSpace(m_TargetInputValueId) ? string.Empty : m_TargetInputValueId.Trim();
        public string TargetKey => string.IsNullOrWhiteSpace(m_TargetKey) ? string.Empty : m_TargetKey.Trim();

        public bool CollectConfigurationErrors(string owner, HashSet<string> abilityIds, List<string> errors)
        {
            bool valid = true;
            if (!m_Ability || string.IsNullOrEmpty(AbilityId))
            {
                errors?.Add($"{owner}: AbilityGrant has no Gameplay Ability.");
                return false;
            }
            valid &= m_Ability.CollectConfigurationErrors(errors);
            if (!abilityIds.Add(AbilityId))
            {
                errors?.Add($"{owner}: AbilityGrant '{AbilityId}' is duplicated.");
                valid = false;
            }
            return valid;
        }

#if UNITY_EDITOR
        public void Configure(
            GameplayAbilityDefinition ability,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId,
            string targetKey)
        {
            m_Ability = ability;
            m_SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            m_ConsumeSourceInputRequest = consumeSourceInputRequest;
            m_TargetInputValueId = targetInputValueId ?? string.Empty;
            m_TargetKey = targetKey ?? string.Empty;
        }
#endif
    }
}
