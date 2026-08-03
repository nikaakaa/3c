using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public abstract class CharacterPresentationPoseSourceSlot : ScriptableObject
    {
        public abstract PresentationPoseSourceKind SourceKind { get; }
        public abstract Type BindingType { get; }

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Pose Source Slot name is missing.");
        }

        public bool Accepts(CharacterPresentationPoseSourceBinding binding) =>
            binding && BindingType.IsInstanceOfType(binding);
    }

    public abstract class CharacterPresentationPoseSourceBinding : ScriptableObject
    {
        [SerializeField] CharacterPresentationPoseSourceSlot m_Slot;
        [SerializeField] CharacterAnimationRigDefinition m_Rig;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;

        public CharacterPresentationPoseSourceSlot Slot => m_Slot;
        public CharacterAnimationRigDefinition Rig => m_Rig;
        public string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public abstract PresentationPoseSourceKind SourceKind { get; }
        public abstract UnityEngine.Object SourceAsset { get; }

        protected void ConfigureCommon(
            CharacterPresentationPoseSourceSlot slot,
            CharacterAnimationRigDefinition rig,
            string footAnalysisIdentity)
        {
            if (!slot || !slot.Accepts(this) || !rig || string.IsNullOrWhiteSpace(footAnalysisIdentity))
                throw new ArgumentException("Presentation Pose source binding is incomplete.");
            m_Slot = slot;
            m_Rig = rig;
            m_FootAnalysisIdentity = footAnalysisIdentity.Trim();
            m_ContentRevision = Guid.NewGuid().ToString("N");
        }

        public virtual void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            if (!m_Slot || !m_Slot.Accepts(this) || m_Slot.SourceKind != SourceKind ||
                !m_Rig || m_Rig != profileRig || string.IsNullOrWhiteSpace(FootAnalysisIdentity) ||
                string.IsNullOrWhiteSpace(ContentRevision) || !SourceAsset)
            {
                throw new InvalidOperationException($"Presentation Pose source binding '{name}' is invalid.");
            }
            m_Slot.RequireValid();
        }
    }

}
