using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationRendererBinding
    {
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] SkinnedMeshRenderer m_Renderer;
        [SerializeField] Mesh m_ExpectedMesh;
        [SerializeField] string m_MeshContentHash = string.Empty;

        public string BindingId => m_BindingId ?? string.Empty;
        public SkinnedMeshRenderer Renderer => m_Renderer;
        public Mesh ExpectedMesh => m_ExpectedMesh;
        public string MeshContentHash => m_MeshContentHash ?? string.Empty;

        public CharacterAnimationRendererBinding() { }

        public CharacterAnimationRendererBinding(
            string bindingId,
            SkinnedMeshRenderer renderer,
            Mesh expectedMesh,
            string meshContentHash)
        {
            if (string.IsNullOrWhiteSpace(bindingId) || !renderer || !expectedMesh ||
                !CharacterAclHash.IsSha256(meshContentHash))
                throw new ArgumentException("Animation Renderer binding is invalid.");
            m_BindingId = bindingId.Trim();
            m_Renderer = renderer;
            m_ExpectedMesh = expectedMesh;
            m_MeshContentHash = meshContentHash.Trim().ToLowerInvariant();
        }

        public void RequireValid(Transform root)
        {
            if (root == null || string.IsNullOrWhiteSpace(BindingId) || !Renderer ||
                !ExpectedMesh || !CharacterAclHash.IsSha256(MeshContentHash) ||
                Renderer.sharedMesh != ExpectedMesh ||
                Renderer.transform != root && !Renderer.transform.IsChildOf(root))
                throw new InvalidOperationException("Animation Renderer binding is invalid.");
        }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterAnimationRigBinding : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] Transform[] m_PhysicalBones = Array.Empty<Transform>();
        [SerializeField] CharacterAnimationRendererBinding[] m_RendererBindings = Array.Empty<CharacterAnimationRendererBinding>();

        public Animator Animator => m_Animator;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<Transform> PhysicalBones => m_PhysicalBones ?? Array.Empty<Transform>();
        public IReadOnlyList<CharacterAnimationRendererBinding> RendererBindings =>
            m_RendererBindings ?? Array.Empty<CharacterAnimationRendererBinding>();

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

        public void ConfigureRendererBindings(
            CharacterAnimationRendererBinding[] rendererBindings)
        {
            m_RendererBindings = rendererBindings ?? Array.Empty<CharacterAnimationRendererBinding>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_RendererBindings.Length; i++)
            {
                CharacterAnimationRendererBinding binding = m_RendererBindings[i] ??
                    throw new ArgumentException("Animation Renderer binding is missing.", nameof(rendererBindings));
                if (!ids.Add(binding.BindingId))
                    throw new ArgumentException("Animation Renderer binding identity is duplicated.", nameof(rendererBindings));
                binding.RequireValid(transform);
            }
        }

        public CharacterAnimationRendererBinding FindRendererBinding(string bindingId)
        {
            if (string.IsNullOrWhiteSpace(bindingId))
                return null;
            for (int i = 0; i < RendererBindings.Count; i++)
            {
                if (string.Equals(RendererBindings[i].BindingId, bindingId, StringComparison.Ordinal))
                    return RendererBindings[i];
            }
            return null;
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
            var rendererIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < RendererBindings.Count; i++)
            {
                CharacterAnimationRendererBinding renderer = RendererBindings[i] ??
                    throw new InvalidOperationException($"Animation Rig Binding '{name}' Renderer #{i} is missing.");
                if (!rendererIds.Add(renderer.BindingId))
                    throw new InvalidOperationException($"Animation Rig Binding '{name}' has a duplicated Renderer binding.");
                renderer.RequireValid(m_Animator.transform);
            }
        }
    }
}
