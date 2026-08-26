using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class AnimationFinalPosePhysicalWriter
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly IReadOnlyList<Transform> m_Bones;
        readonly Transform m_ComponentRoot;
        readonly int m_RootBoneIndex;
        readonly int m_LeftAnkleBoneIndex;
        readonly int m_RightAnkleBoneIndex;
        readonly int m_PelvisBoneIndex;
        readonly CharacterAnimationRootBonePolicy m_RootBonePolicy;
        readonly AnimationLocalBonePose m_RootReferencePose;
        readonly AnimationLocalBonePose[] m_ReferencePoses;
        AnimationPhysicalBoneWriteDiagnostics m_Diagnostics;

        internal AnimationFinalPosePhysicalWriter(
            CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig)
        {
            if (!binding)
                throw new ArgumentNullException(nameof(binding));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            binding.RequireValid(rig);
            m_Bones = binding.PhysicalBones;
            m_ComponentRoot = binding.Animator.transform;
            m_RootBoneIndex = rig.RootPhysicalBoneIndex;
            m_LeftAnkleBoneIndex = rig.LeftLeg.AnklePhysicalBoneIndex;
            m_RightAnkleBoneIndex = rig.RightLeg.AnklePhysicalBoneIndex;
            m_PelvisBoneIndex = rig.PelvisPhysicalBoneIndex;
            m_RootBonePolicy = rig.RootBonePolicy;
            CharacterAnimationPhysicalBonePayload root =
                rig.PhysicalBones[m_RootBoneIndex];
            m_RootReferencePose = new AnimationLocalBonePose(
                root.ReferenceLocalPosition,
                root.ReferenceLocalRotation,
                root.ReferenceLocalScale);
            m_ReferencePoses =
                new AnimationLocalBonePose[rig.PhysicalBoneCount];
            for (int i = 0; i < m_ReferencePoses.Length; i++)
            {
                CharacterAnimationPhysicalBonePayload bone =
                    rig.PhysicalBones[i];
                m_ReferencePoses[i] = new AnimationLocalBonePose(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
            }
            if (!m_RootReferencePose.IsValid)
                throw new InvalidOperationException(
                    "Animation root reference pose is invalid.");
        }

        internal AnimationPhysicalBoneWriteDiagnostics Diagnostics =>
            m_Diagnostics;

        internal void Write(
            in AnimationFinalPoseNativeReadBinding pending,
            bool hasCommitted,
            in AnimationFinalPoseNativeReadBinding committed)
        {
            bool pendingValid = HeaderIsValid(in pending);
            bool committedValid =
                hasCommitted &&
                HeaderIsValid(in committed) &&
                committed.AppliedAt[0] == committed.CompletionIdentity &&
                committed.WriteOutcome[0] ==
                    AnimationFinalPoseWriteOutcome.Committed;

            for (int boneIndex = 0; boneIndex < m_Bones.Count; boneIndex++)
            {
                Transform bone = m_Bones[boneIndex];
                AnimationLocalBonePose pose = ResolvePose(
                    in pending,
                    in committed,
                    pendingValid,
                    committedValid,
                    boneIndex);
                if (!bone || !pose.IsValid)
                {
                    PublishFault(in pending);
                    throw new InvalidOperationException(
                        $"Final animation physical write Bone #{boneIndex} is invalid.");
                }
            }

            for (int boneIndex = 0; boneIndex < m_Bones.Count; boneIndex++)
            {
                Transform bone = m_Bones[boneIndex];
                AnimationLocalBonePose pose = ResolvePose(
                    in pending,
                    in committed,
                    pendingValid,
                    committedValid,
                    boneIndex);
                bone.localPosition = pose.Position;
                bone.localRotation = pose.Rotation;
                bone.localScale = pose.Scale;
            }
            NativeSlice<ulong> committedAt = pending.AppliedAt;
            NativeSlice<AnimationFinalPoseWriteOutcome> committedOutcome =
                pending.WriteOutcome;
            committedAt[0] = pendingValid
                ? pending.CompletionIdentity
                : 0;
            committedOutcome[0] = pendingValid
                ? AnimationFinalPoseWriteOutcome.Committed
                : AnimationFinalPoseWriteOutcome.TypedInvalid;
            if (pendingValid)
            {
                m_Diagnostics = new AnimationPhysicalBoneWriteDiagnostics(
                    pending.CompletionIdentity,
                    CaptureComponentPosition(m_LeftAnkleBoneIndex),
                    CaptureComponentRotation(m_LeftAnkleBoneIndex),
                    CaptureComponentPosition(m_RightAnkleBoneIndex),
                    CaptureComponentRotation(m_RightAnkleBoneIndex),
                    CaptureComponentPosition(m_PelvisBoneIndex));
            }
        }

        Vector3 CaptureComponentPosition(int boneIndex) =>
            m_ComponentRoot.InverseTransformPoint(m_Bones[boneIndex].position);

        Quaternion CaptureComponentRotation(int boneIndex) =>
            (Quaternion.Inverse(m_ComponentRoot.rotation) *
             m_Bones[boneIndex].rotation).normalized;

        AnimationLocalBonePose ResolvePose(
            in AnimationFinalPoseNativeReadBinding pending,
            in AnimationFinalPoseNativeReadBinding committed,
            bool pendingValid,
            bool committedValid,
            int boneIndex) =>
            m_RootBonePolicy ==
                CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
            boneIndex == m_RootBoneIndex
                ? m_RootReferencePose
                : pendingValid
                    ? pending.DenseLocalPoses[boneIndex]
                    : committedValid
                        ? committed.DenseLocalPoses[boneIndex]
                : m_ReferencePoses[boneIndex];

        internal void ValidateBindingsBeforeEvaluate(
            in AnimationFinalPoseNativeReadBinding pending,
            bool hasCommitted,
            in AnimationFinalPoseNativeReadBinding committed)
        {
            RequireBinding(in pending);
            if (hasCommitted)
                RequireBinding(in committed);
            for (int boneIndex = 0; boneIndex < m_Bones.Count; boneIndex++)
            {
                if (!m_Bones[boneIndex])
                {
                    throw new InvalidOperationException(
                        $"Final animation physical writer Bone #{boneIndex} binding is missing.");
                }
            }
        }

        void RequireBinding(in AnimationFinalPoseNativeReadBinding binding)
        {
            if (binding.CompletionIdentity == 0 ||
                binding.OutputValueIndex < 0 ||
                binding.DenseLocalPoses.Length < m_Bones.Count ||
                m_Bones.Count != m_Rig.PhysicalBoneCount ||
                m_RootBoneIndex < 0 ||
                m_RootBoneIndex >= m_Bones.Count ||
                !IsUnit(binding.Availability) ||
                !IsUnit(binding.ContinuityIdentity) ||
                !IsUnit(binding.OutputInvalidReason) ||
                !IsUnit(binding.PoseGraphInvalidReason) ||
                !IsUnit(binding.PoseGraphInvalidOperationIndex) ||
                !IsUnit(binding.PoseGraphCompletedAt) ||
                !IsUnit(binding.AppliedAt) ||
                !IsUnit(binding.WriteOutcome))
            {
                throw new ArgumentException(
                    "Final animation physical writer binding is invalid.");
            }
        }

        static bool HeaderIsValid(
            in AnimationFinalPoseNativeReadBinding binding) =>
            binding.PoseGraphCompletedAt[0] ==
                binding.CompletionIdentity &&
            binding.Availability[0] ==
                AnimationPoseAvailability.Pose &&
            binding.OutputInvalidReason[0] ==
                AnimationPoseNativeInvalidReason.None &&
            binding.PoseGraphInvalidReason[0] ==
                AnimationPoseNativeInvalidReason.None &&
            binding.PoseGraphInvalidOperationIndex[0] == -1 &&
            binding.ContinuityIdentity[0] != 0;

        static void PublishFault(
            in AnimationFinalPoseNativeReadBinding binding)
        {
            NativeSlice<AnimationPoseAvailability> availability =
                binding.Availability;
            NativeSlice<AnimationPoseNativeInvalidReason> invalidReason =
                binding.OutputInvalidReason;
            NativeSlice<ulong> appliedAt = binding.AppliedAt;
            NativeSlice<AnimationFinalPoseWriteOutcome> writeOutcome =
                binding.WriteOutcome;
            availability[0] = AnimationPoseAvailability.Invalid;
            invalidReason[0] =
                AnimationPoseNativeInvalidReason.FinalPhysicalWriteInvalid;
            appliedAt[0] = 0;
            writeOutcome[0] = AnimationFinalPoseWriteOutcome.Faulted;
        }

        static bool IsUnit<T>(NativeSlice<T> values)
            where T : struct => values.Length == 1;
    }
}
