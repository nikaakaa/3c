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
        [SerializeField] Transform[] m_Bones = Array.Empty<Transform>();

        public Animator Animator => m_Animator;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<Transform> Bones => m_Bones ?? Array.Empty<Transform>();

        public void Configure(Animator animator, CharacterAnimationRigPayload rig, Transform[] bones)
        {
            if (!animator)
                throw new ArgumentNullException(nameof(animator));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_Animator = animator;
            m_RigId = rig.RigId;
            m_RigRevision = rig.RigRevision;
            m_Bones = bones ?? throw new ArgumentNullException(nameof(bones));
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
                Bones.Count != expected.Bones.Count)
            {
                throw new InvalidOperationException($"Animation Rig Binding '{name}' does not match the compiled Projection Rig.");
            }
            var transforms = new HashSet<Transform>();
            for (int i = 0; i < Bones.Count; i++)
            {
                if (!Bones[i] || !transforms.Add(Bones[i]) ||
                    Bones[i] != m_Animator.transform && !Bones[i].IsChildOf(m_Animator.transform))
                {
                    throw new InvalidOperationException($"Animation Rig Binding '{name}' Bone #{i} is missing, duplicated, or outside the Animator hierarchy.");
                }
            }
        }
    }
}
