using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationRigBinding : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] Transform[] m_PhysicalBones = Array.Empty<Transform>();

        public Animator Animator => m_Animator;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<Transform> PhysicalBones => m_PhysicalBones ?? Array.Empty<Transform>();

        public void Configure(Animator animator, CharacterAnimationRigPayload rig, Transform[] physicalBones)
        {
            if (!animator)
                throw new ArgumentNullException(nameof(animator));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_Animator = animator;
            m_RigId = rig.RigId;
            m_RigRevision = rig.RigRevision;
            m_PhysicalBones = physicalBones ?? throw new ArgumentNullException(nameof(physicalBones));
            RequireValid(rig);
        }

        public void RequireValid(CharacterAnimationRigPayload expected)
        {
            if (expected == null)
                throw new ArgumentNullException(nameof(expected));
            expected.RequireValid();
            if (!m_Animator ||
                !string.Equals(RigId, expected.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, expected.RigRevision, StringComparison.Ordinal) ||
                PhysicalBones.Count != expected.PhysicalBoneCount)
            {
                throw new InvalidOperationException($"Animation Rig Binding '{name}' does not match the compiled Projection Rig.");
            }
            var transforms = new HashSet<Transform>();
            for (int i = 0; i < PhysicalBones.Count; i++)
            {
                if (!PhysicalBones[i] || !transforms.Add(PhysicalBones[i]) ||
                    PhysicalBones[i] != m_Animator.transform && !PhysicalBones[i].IsChildOf(m_Animator.transform))
                {
                    throw new InvalidOperationException($"Animation Rig Binding '{name}' Bone #{i} is missing, duplicated, or outside the Animator hierarchy.");
                }
            }
        }
    }
}
