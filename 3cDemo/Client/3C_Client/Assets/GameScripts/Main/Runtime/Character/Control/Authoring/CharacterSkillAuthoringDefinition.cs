using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Serializable]
    public sealed class CharacterSkillSubgraphDependencyConfiguration
    {
        [SerializeField] string m_SubgraphIdentity;
        [SerializeField] string m_CallSiteIdentity;

        public string SubgraphIdentity => m_SubgraphIdentity ?? string.Empty;
        public string CallSiteIdentity => m_CallSiteIdentity ?? string.Empty;

        public CharacterSkillSubgraphDependencyConfiguration() { }

        public CharacterSkillSubgraphDependencyConfiguration(string subgraphIdentity, string callSiteIdentity)
        {
            m_SubgraphIdentity = subgraphIdentity ?? string.Empty;
            m_CallSiteIdentity = callSiteIdentity ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class CharacterSkillAuthoringDefinition
    {
        [SerializeField] string m_SkillId;
        [SerializeField] string m_EntryGraphAuthoringId;
        [SerializeField] ActionProfile m_ActionProfile;
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] string m_SourceInputRequestId;
        [SerializeField] bool m_ConsumeSourceInputRequest = true;
        [SerializeField] string m_TargetInputValueId;
        [SerializeField] string m_TargetKey;
        [SerializeField] CharacterSkillSubgraphDependencyConfiguration[] m_SubgraphDependencies = Array.Empty<CharacterSkillSubgraphDependencyConfiguration>();
        [SerializeField] string[] m_AllowedFollowUpSkillIds = Array.Empty<string>();

        public string SkillId => m_SkillId ?? string.Empty;
        public string EntryGraphAuthoringId => m_EntryGraphAuthoringId ?? string.Empty;
        public ActionProfile ActionProfile => m_ActionProfile;
        public ActionContextSlot ActionContext => m_ActionContext;
        public string SourceInputRequestId => m_SourceInputRequestId ?? string.Empty;
        public bool ConsumeSourceInputRequest => m_ConsumeSourceInputRequest;
        public string TargetInputValueId => m_TargetInputValueId ?? string.Empty;
        public string TargetKey => m_TargetKey ?? string.Empty;
        public IReadOnlyList<CharacterSkillSubgraphDependencyConfiguration> SubgraphDependencies =>
            m_SubgraphDependencies ?? Array.Empty<CharacterSkillSubgraphDependencyConfiguration>();
        public IReadOnlyList<string> AllowedFollowUpSkillIds =>
            m_AllowedFollowUpSkillIds ?? Array.Empty<string>();

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            string skillId,
            string entryGraphAuthoringId,
            ActionProfile actionProfile,
            ActionContextSlot actionContext,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId,
            string targetKey)
        {
            m_SkillId = skillId ?? string.Empty;
            m_EntryGraphAuthoringId = entryGraphAuthoringId ?? string.Empty;
            m_ActionProfile = actionProfile;
            m_ActionContext = actionContext;
            m_SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            m_ConsumeSourceInputRequest = consumeSourceInputRequest;
            m_TargetInputValueId = targetInputValueId ?? string.Empty;
            m_TargetKey = targetKey ?? string.Empty;
        }

        public void ConfigureSkillRelations(
            IEnumerable<CharacterSkillSubgraphDependencyConfiguration> subgraphDependencies,
            IEnumerable<string> allowedFollowUpSkillIds)
        {
            m_SubgraphDependencies = subgraphDependencies == null
                ? Array.Empty<CharacterSkillSubgraphDependencyConfiguration>()
                : new List<CharacterSkillSubgraphDependencyConfiguration>(subgraphDependencies).ToArray();
            m_AllowedFollowUpSkillIds = allowedFollowUpSkillIds == null
                ? Array.Empty<string>()
                : new List<string>(allowedFollowUpSkillIds).ToArray();
        }
#endif
    }
}
