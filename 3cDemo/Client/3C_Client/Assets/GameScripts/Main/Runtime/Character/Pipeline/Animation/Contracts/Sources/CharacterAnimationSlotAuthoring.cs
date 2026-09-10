using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationSlotDefinition
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;

        public AnimationSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new AnimationSlotId(m_SlotId);
        public AnimationSlotGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new AnimationSlotGroupId(m_GroupId);
        public string DisplayName => m_DisplayName ?? string.Empty;

        public CharacterAnimationSlotDefinition() { }

        public CharacterAnimationSlotDefinition(
            AnimationSlotId slotId,
            AnimationSlotGroupId groupId,
            string displayName)
        {
            m_SlotId = slotId.IsValid
                ? slotId.Value
                : throw new ArgumentException("Animation Slot identity is invalid.", nameof(slotId));
            m_GroupId = groupId.IsValid
                ? groupId.Value
                : throw new ArgumentException("Animation Slot Group identity is invalid.", nameof(groupId));
            m_DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Animation Slot display name is missing.", nameof(displayName))
                : displayName.Trim();
        }

        public void RequireValid()
        {
            if (!SlotId.IsValid || !GroupId.IsValid || string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException("Animation Slot definition is incomplete.");
        }
    }
}
