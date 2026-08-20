using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterLocomotionSyncGroup
    {
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] AnimationClip[] m_Members = Array.Empty<AnimationClip>();

        public string GroupId => m_GroupId ?? string.Empty;
        public IReadOnlyList<AnimationClip> Members => m_Members ?? Array.Empty<AnimationClip>();

        public CharacterLocomotionSyncGroup()
        {
        }

        public CharacterLocomotionSyncGroup(string groupId, AnimationClip[] members)
        {
            m_GroupId = groupId?.Trim() ?? string.Empty;
            m_Members = members == null ? Array.Empty<AnimationClip>() : (AnimationClip[])members.Clone();
            RequireValid();
        }

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(GroupId) || !string.Equals(GroupId, GroupId.Trim(), StringComparison.Ordinal) ||
                Members.Count < 2)
            {
                throw new InvalidOperationException("Locomotion Sync Group is incomplete.");
            }
            var unique = new HashSet<AnimationClip>();
            for (int i = 0; i < Members.Count; i++)
            {
                if (!Members[i] || !unique.Add(Members[i]))
                    throw new InvalidOperationException($"Locomotion Sync Group '{GroupId}' member #{i} is invalid or duplicated.");
            }
        }

        public bool Contains(AnimationClip clip)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i] == clip)
                    return true;
            }
            return false;
        }
    }
}
