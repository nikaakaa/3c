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
        [SerializeField] string m_ContentRevision = string.Empty;

        public CharacterPresentationPoseSourceSlot Slot => m_Slot;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public abstract CharacterAnimationRigDefinition Rig { get; }
        public abstract string FootAnalysisIdentity { get; }
        public abstract PresentationPoseSourceKind SourceKind { get; }
        public abstract UnityEngine.Object SourceAsset { get; }

        protected void ConfigureCommon(CharacterPresentationPoseSourceSlot slot)
        {
            if (!slot || !slot.Accepts(this))
                throw new ArgumentException("Presentation Pose source binding is incomplete.");
            m_Slot = slot;
            m_ContentRevision = Guid.NewGuid().ToString("N");
        }

        public virtual void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            if (!m_Slot || !m_Slot.Accepts(this) || m_Slot.SourceKind != SourceKind ||
                !Rig || Rig != profileRig || string.IsNullOrWhiteSpace(FootAnalysisIdentity) ||
                string.IsNullOrWhiteSpace(ContentRevision) || !SourceAsset)
            {
                throw new InvalidOperationException($"Presentation Pose source binding '{name}' is invalid.");
            }
            m_Slot.RequireValid();
        }
    }

}
