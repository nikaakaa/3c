using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal sealed class CharacterAnimationSourcePhysicalBone
    {
        internal CharacterAnimationSourcePhysicalBone(
            AnimationBoneId boneId,
            int parentPhysicalIndex,
            AnimationLocalBonePose referenceLocalPose,
            string path)
        {
            if (!boneId.IsValid || parentPhysicalIndex < -1 ||
                !referenceLocalPose.IsValid || path == null)
                throw new ArgumentException("Animation source Physical Bone is invalid.");
            BoneId = boneId;
            ParentPhysicalIndex = parentPhysicalIndex;
            ReferenceLocalPose = referenceLocalPose;
            Path = path;
        }

        internal AnimationBoneId BoneId { get; }
        internal int ParentPhysicalIndex { get; }
        internal AnimationLocalBonePose ReferenceLocalPose { get; }
        internal string Path { get; }
    }

    internal sealed class CharacterAnimationSourceRig
    {
        internal CharacterAnimationSourceRig(
            string rigId,
            string rigRevision,
            int poseBoneCount,
            string referencePoseIdentity,
            CharacterAnimationSourcePhysicalBone[] physicalBones,
            Animator animator,
            Transform[] physicalTransforms,
            CharacterAnimationRendererBinding[] rendererBindings)
        {
            RigId = string.IsNullOrWhiteSpace(rigId)
                ? throw new ArgumentException("Animation source Rig identity is required.", nameof(rigId))
                : rigId.Trim();
            RigRevision = string.IsNullOrWhiteSpace(rigRevision)
                ? throw new ArgumentException("Animation source Rig revision is required.", nameof(rigRevision))
                : rigRevision.Trim();
            PoseBoneCount = poseBoneCount;
            ReferencePoseIdentity = referencePoseIdentity ?? string.Empty;
            PhysicalBones = physicalBones ?? throw new ArgumentNullException(nameof(physicalBones));
            Animator = animator;
            PhysicalTransforms = physicalTransforms ??
                throw new ArgumentNullException(nameof(physicalTransforms));
            RendererBindings = rendererBindings ??
                Array.Empty<CharacterAnimationRendererBinding>();
            RequireValid();
        }

        internal string RigId { get; }
        internal string RigRevision { get; }
        internal int PoseBoneCount { get; }
        internal string ReferencePoseIdentity { get; }
        internal IReadOnlyList<CharacterAnimationSourcePhysicalBone> PhysicalBones { get; }
        internal int PhysicalBoneCount => PhysicalBones.Count;
        internal Animator Animator { get; }
        internal IReadOnlyList<Transform> PhysicalTransforms { get; }
        internal IReadOnlyList<CharacterAnimationRendererBinding> RendererBindings { get; }

        internal bool HasRendererBindings => RendererBindings.Count > 0;

        internal CharacterAnimationRendererBinding FindRendererBinding(string bindingId)
        {
            if (string.IsNullOrWhiteSpace(bindingId))
                return null;
            for (int i = 0; i < RendererBindings.Count; i++)
            {
                CharacterAnimationRendererBinding binding = RendererBindings[i];
                if (binding != null &&
                    string.Equals(binding.BindingId, bindingId, StringComparison.Ordinal))
                    return binding;
            }
            return null;
        }

        internal static CharacterAnimationSourceRig FromRuntimeRig(
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding binding)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (!binding)
                throw new ArgumentNullException(nameof(binding));
            rig.RequireValid();
            binding.RequireValid(rig);
            var bones = new CharacterAnimationSourcePhysicalBone[rig.PhysicalBoneCount];
            for (int i = 0; i < bones.Length; i++)
            {
                Transform transform = binding.PhysicalBones[i];
                string path = AnimationUtility.CalculateTransformPath(
                    transform,
                    binding.Animator.transform);
                bones[i] = new CharacterAnimationSourcePhysicalBone(
                    rig.PhysicalBones[i].BoneId,
                    rig.PhysicalBones[i].ParentPhysicalIndex,
                    rig.GetReferenceLocalPose(i),
                    path);
            }
            return new CharacterAnimationSourceRig(
                rig.RigId,
                rig.RigRevision,
                rig.PoseBoneCount,
                CharacterAclAnimationIdentity.ComputeRigReferencePoseIdentity(rig),
                bones,
                binding.Animator,
                PathsToTransforms(binding),
                CopyRendererBindings(binding));
        }

        internal static CharacterAnimationSourceRig FromAnimatorHierarchy(
            Animator animator,
            string rigId,
            string rigRevision)
        {
            if (!animator)
                throw new ArgumentNullException(nameof(animator));
            Transform[] transforms = EnumerateHierarchy(animator.transform);
            var bones = new CharacterAnimationSourcePhysicalBone[transforms.Length];
            var indices = new Dictionary<Transform, int>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                string path = AnimationUtility.CalculateTransformPath(
                    transform,
                    animator.transform);
                int parentIndex = transform == animator.transform
                    ? -1
                    : indices[transform.parent];
                indices.Add(transform, i);
                bones[i] = new CharacterAnimationSourcePhysicalBone(
                    BuildBoneId(path),
                    parentIndex,
                    new AnimationLocalBonePose(
                        transform.localPosition,
                        transform.localRotation,
                        transform.localScale),
                    path);
            }
            string referencePoseIdentity = CharacterAclAnimationIdentity.ComputeRigReferencePoseIdentity(
                rigId,
                rigRevision,
                bones.Select(value => value.BoneId.Value).ToArray(),
                bones.Select(value => value.ParentPhysicalIndex).ToArray(),
                bones.Select(value => value.ReferenceLocalPose).ToArray());
            return new CharacterAnimationSourceRig(
                rigId,
                rigRevision,
                transforms.Length,
                referencePoseIdentity,
                bones,
                animator,
                transforms,
                BuildRendererBindings(animator.transform));
        }

        internal AnimationLocalBonePose GetReferenceLocalPose(int physicalBoneIndex)
        {
            if ((uint)physicalBoneIndex >= (uint)PhysicalBoneCount)
                throw new ArgumentOutOfRangeException(nameof(physicalBoneIndex));
            return PhysicalBones[physicalBoneIndex].ReferenceLocalPose;
        }

        internal void RequireValid()
        {
            if (string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) ||
                PoseBoneCount < PhysicalBoneCount || PhysicalBoneCount == 0 ||
                !CharacterAclHash.IsSha256(ReferencePoseIdentity) ||
                !Animator || PhysicalTransforms.Count != PhysicalBoneCount)
            {
                throw new InvalidOperationException("Animation source Rig is invalid.");
            }
            var ids = new HashSet<AnimationBoneId>();
            var transforms = new HashSet<Transform>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            int rootCount = 0;
            for (int i = 0; i < PhysicalBoneCount; i++)
            {
                CharacterAnimationSourcePhysicalBone bone = PhysicalBones[i];
                Transform transform = PhysicalTransforms[i];
                if (bone == null || !ids.Add(bone.BoneId) || !transform ||
                    !transforms.Add(transform) || !paths.Add(bone.Path) ||
                    !transform.IsChildOf(Animator.transform) && transform != Animator.transform ||
                    !string.Equals(
                        AnimationUtility.CalculateTransformPath(transform, Animator.transform),
                        bone.Path,
                        StringComparison.Ordinal) ||
                    !bone.ReferenceLocalPose.IsValid ||
                    bone.ParentPhysicalIndex < -1 || bone.ParentPhysicalIndex >= i)
                {
                    throw new InvalidOperationException(
                        $"Animation source Rig Physical Bone #{i} is invalid.");
                }
                if (bone.ParentPhysicalIndex >= 0 &&
                    transform.parent != PhysicalTransforms[bone.ParentPhysicalIndex])
                {
                    throw new InvalidOperationException(
                        $"Animation source Rig Physical Bone #{i} parent mapping is invalid.");
                }
                if (bone.ParentPhysicalIndex < 0)
                    rootCount++;
            }
            if (rootCount != 1)
                throw new InvalidOperationException("Animation source Rig requires one Physical root.");
            var rendererIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < RendererBindings.Count; i++)
            {
                CharacterAnimationRendererBinding binding = RendererBindings[i] ??
                    throw new InvalidOperationException("Animation source Rig Renderer binding is missing.");
                if (!rendererIds.Add(binding.BindingId))
                    throw new InvalidOperationException("Animation source Rig Renderer binding is duplicated.");
                binding.RequireValid(Animator.transform);
            }
        }

        static Transform[] PathsToTransforms(CharacterAnimationRigBinding binding)
        {
            var result = new Transform[binding.PhysicalBones.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = binding.PhysicalBones[i];
            return result;
        }

        static CharacterAnimationRendererBinding[] CopyRendererBindings(
            CharacterAnimationRigBinding binding)
        {
            var result = new CharacterAnimationRendererBinding[binding.RendererBindings.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = binding.RendererBindings[i];
            return result;
        }

        static CharacterAnimationRendererBinding[] BuildRendererBindings(Transform root)
        {
            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var result = new CharacterAnimationRendererBinding[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                Mesh mesh = renderer.sharedMesh ??
                    throw new InvalidOperationException(
                        $"Animation source Renderer '{renderer.name}' has no Mesh.");
                CharacterAnimationMeshContentIdentity identity =
                    CharacterAnimationMeshContentIdentity.Resolve(mesh);
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, root);
                result[i] = new CharacterAnimationRendererBinding(
                    "animation-renderer/" + (string.IsNullOrEmpty(path) ? "root" : path),
                    renderer,
                    mesh,
                    identity.ContentHash);
            }
            return result;
        }

        static Transform[] EnumerateHierarchy(Transform root)
        {
            var result = new List<Transform>();
            Collect(root, result);
            return result.ToArray();
        }

        static void Collect(Transform current, ICollection<Transform> result)
        {
            result.Add(current);
            for (int i = 0; i < current.childCount; i++)
                Collect(current.GetChild(i), result);
        }

        static AnimationBoneId BuildBoneId(string path)
        {
            string normalized = string.IsNullOrEmpty(path) ? "root" : path;
            var value = new System.Text.StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
                value.Append(char.IsWhiteSpace(normalized[i]) ? '_' : normalized[i]);
            return new AnimationBoneId("animation-bone/" + value);
        }
    }
}
