using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterFinalPosePhysicalWriter
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
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

        internal CharacterFinalPosePhysicalWriter(
            CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig,
            CharacterRootHierarchyBinding rootHierarchy)
        {
            if (!binding)
                throw new ArgumentNullException(nameof(binding));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_RootHierarchy = rootHierarchy
                ? rootHierarchy
                : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            binding.RequireValid(rig);
            m_Bones = binding.PhysicalBones;
            m_ComponentRoot = binding.Animator.transform;
            if (m_ComponentRoot != m_RootHierarchy.PoseRoot)
            {
                throw new ArgumentException(
                    "Final Pose writer must use the formal PoseRoot.",
                    nameof(rootHierarchy));
            }
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

        internal void WriteNative(
            in CharacterPoseNativePoseReadBinding output,
            in ComposedAnimationPoseFrame pending,
            bool hasCommitted,
            in ComposedAnimationPoseFrame committed,
            bool captureFootIkDiagnostics)
        {
            bool pendingValid = PendingNativeHeaderIsValid(
                in output,
                in pending);
            bool committedValid =
                hasCommitted &&
                CommittedHeaderIsValid(in committed);

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
            if (pendingValid)
            {
                Vector3 pelvisWorldPosition = m_Bones[m_PelvisBoneIndex].position;
                CharacterFootIkPhysicalCapture footIkCapture =
                    captureFootIkDiagnostics
                        ? CaptureFootIkPhysical(pelvisWorldPosition)
                        : default;
                m_Diagnostics = new AnimationPhysicalBoneWriteDiagnostics(
                    output.CompletionIdentity,
                    CaptureComponentPosition(m_LeftAnkleBoneIndex),
                    CaptureComponentRotation(m_LeftAnkleBoneIndex),
                    CaptureComponentPosition(m_RightAnkleBoneIndex),
                    CaptureComponentRotation(m_RightAnkleBoneIndex),
                    m_ComponentRoot.InverseTransformPoint(pelvisWorldPosition),
                    pelvisWorldPosition,
                    in footIkCapture);
            }
        }

        internal void Write(
            in CharacterPoseProgramOutputResult output,
            in ComposedAnimationPoseFrame pending,
            bool hasCommitted,
            in ComposedAnimationPoseFrame committed,
            bool captureFootIkDiagnostics)
        {
            bool pendingValid = PendingHeaderIsValid(
                in output,
                in pending);
            bool committedValid =
                hasCommitted &&
                CommittedHeaderIsValid(in committed);

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
            if (pendingValid)
            {
                Vector3 pelvisWorldPosition = m_Bones[m_PelvisBoneIndex].position;
                CharacterFootIkPhysicalCapture footIkCapture =
                    captureFootIkDiagnostics
                        ? CaptureFootIkPhysical(pelvisWorldPosition)
                        : default;
                m_Diagnostics = new AnimationPhysicalBoneWriteDiagnostics(
                    output.Lineage.CompletionIdentity,
                    CaptureComponentPosition(m_LeftAnkleBoneIndex),
                    CaptureComponentRotation(m_LeftAnkleBoneIndex),
                    CaptureComponentPosition(m_RightAnkleBoneIndex),
                    CaptureComponentRotation(m_RightAnkleBoneIndex),
                    m_ComponentRoot.InverseTransformPoint(pelvisWorldPosition),
                    pelvisWorldPosition,
                    in footIkCapture);
            }
        }

        CharacterFootIkPhysicalCapture CaptureFootIkPhysical(
            Vector3 pelvisWorldPosition)
        {
            Transform logicRoot = m_RootHierarchy.LogicRoot;
            Transform visualRoot = m_RootHierarchy.VisualRoot;
            Transform poseRoot = m_RootHierarchy.PoseRoot;
            Transform leftAnkle = m_Bones[m_LeftAnkleBoneIndex];
            Transform rightAnkle = m_Bones[m_RightAnkleBoneIndex];
            return new CharacterFootIkPhysicalCapture(
                logicRoot.position,
                logicRoot.rotation,
                visualRoot.localPosition,
                visualRoot.localRotation,
                visualRoot.position,
                visualRoot.rotation,
                poseRoot.localPosition,
                poseRoot.localRotation,
                poseRoot.position,
                poseRoot.rotation,
                pelvisWorldPosition,
                leftAnkle.position,
                leftAnkle.rotation,
                rightAnkle.position,
                rightAnkle.rotation);
        }

        Vector3 CaptureComponentPosition(int boneIndex) =>
            m_ComponentRoot.InverseTransformPoint(m_Bones[boneIndex].position);

        Quaternion CaptureComponentRotation(int boneIndex) =>
            (Quaternion.Inverse(m_ComponentRoot.rotation) *
             m_Bones[boneIndex].rotation).normalized;

        AnimationLocalBonePose ResolvePose(
            in ComposedAnimationPoseFrame pending,
            in ComposedAnimationPoseFrame committed,
            bool pendingValid,
            bool committedValid,
            int boneIndex) =>
            m_RootBonePolicy ==
                CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
            boneIndex == m_RootBoneIndex
                ? m_RootReferencePose
                : pendingValid
                    ? pending.DenseLocalPose[boneIndex]
                    : committedValid
                        ? committed.DenseLocalPose[boneIndex]
                : m_ReferencePoses[boneIndex];

        internal void ValidateBindingsBeforeEvaluate(
            bool hasCommitted,
            in ComposedAnimationPoseFrame committed)
        {
            if (m_Bones.Count != m_Rig.PhysicalBoneCount ||
                m_RootBoneIndex < 0 ||
                m_RootBoneIndex >= m_Bones.Count)
            {
                throw new ArgumentException(
                    "Final animation physical writer binding is invalid.");
            }
            if (hasCommitted && !CommittedHeaderIsValid(in committed))
            {
                throw new ArgumentException(
                    "Final animation physical writer committed Pose is invalid.");
            }
            for (int boneIndex = 0; boneIndex < m_Bones.Count; boneIndex++)
            {
                if (!m_Bones[boneIndex])
                {
                    throw new InvalidOperationException(
                        $"Final animation physical writer Bone #{boneIndex} binding is missing.");
                }
            }
        }

        bool PendingHeaderIsValid(
            in CharacterPoseProgramOutputResult output,
            in ComposedAnimationPoseFrame frame) =>
            output.IsCompleted &&
            frame.CompletionIdentity == output.Lineage.CompletionIdentity &&
            frame.Availability == AnimationPoseAvailability.Pose &&
            frame.ContinuityIdentity == output.ContinuityIdentity &&
            frame.DenseLocalPose.Count >= m_Bones.Count;

        bool PendingNativeHeaderIsValid(
            in CharacterPoseNativePoseReadBinding output,
            in ComposedAnimationPoseFrame frame) =>
            output.IsValid &&
            output.Space == CharacterPoseSpace.Local &&
            output.Availability[0] == AnimationPoseAvailability.Pose &&
            output.InvalidReason[0] == AnimationPoseNativeInvalidReason.None &&
            frame.CompletionIdentity == output.CompletionIdentity &&
            frame.Availability == AnimationPoseAvailability.Pose &&
            frame.ContinuityIdentity == output.ContinuityIdentity[0] &&
            frame.DenseLocalPose.Count >= m_Bones.Count;

        bool CommittedHeaderIsValid(
            in ComposedAnimationPoseFrame frame) =>
            frame.CompletionIdentity != 0 &&
            frame.Availability == AnimationPoseAvailability.Pose &&
            frame.ContinuityIdentity != 0 &&
            frame.DenseLocalPose.Count >= m_Bones.Count;

    }
}
