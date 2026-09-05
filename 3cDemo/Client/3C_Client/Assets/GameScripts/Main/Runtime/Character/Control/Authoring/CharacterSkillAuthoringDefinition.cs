using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
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

        public string SkillId => m_SkillId ?? string.Empty;
        public string EntryGraphAuthoringId => m_EntryGraphAuthoringId ?? string.Empty;
        public ActionProfile ActionProfile => m_ActionProfile;
        public ActionContextSlot ActionContext => m_ActionContext;
        public string SourceInputRequestId => m_SourceInputRequestId ?? string.Empty;
        public bool ConsumeSourceInputRequest => m_ConsumeSourceInputRequest;
        public string TargetInputValueId => m_TargetInputValueId ?? string.Empty;
        public string TargetKey => m_TargetKey ?? string.Empty;
    }
}
