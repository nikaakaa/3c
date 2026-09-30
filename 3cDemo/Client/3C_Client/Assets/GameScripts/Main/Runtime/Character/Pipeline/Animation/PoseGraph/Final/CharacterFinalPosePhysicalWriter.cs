using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterFinalPosePhysicalWriter
    {
        readonly Transform[] m_Bones;
        readonly int m_RootBoneIndex;
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly int m_LeftAnkleBoneIndex;
        readonly int m_RightAnkleBoneIndex;
        readonly int m_LeftToeBoneIndex;
        readonly int m_RightToeBoneIndex;
        readonly int m_PelvisBoneIndex;
        internal CharacterFootIkPhysicalCapture FootIkCapture { get; private set; }
#endif
        readonly CharacterAnimationRootBonePolicy m_RootBonePolicy;
        readonly AnimationLocalBonePose m_RootReferencePose;

        internal CharacterFinalPosePhysicalWriter(
            CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig,
            CharacterRootHierarchyBinding rootHierarchy)
        {
            if (!binding)
                throw new ArgumentNullException(nameof(binding));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (!rootHierarchy)
                throw new ArgumentNullException(nameof(rootHierarchy));
            if (binding.PhysicalBones.Count != rig.PhysicalBoneCount ||
                rig.RootPhysicalBoneIndex < 0 ||
                rig.RootPhysicalBoneIndex >= rig.PhysicalBoneCount)
            {
                throw new ArgumentException(
                    "Final animation physical writer binding is invalid.");
            }
            m_Bones = new Transform[rig.PhysicalBoneCount];
            for (int boneIndex = 0; boneIndex < m_Bones.Length; boneIndex++)
            {
                Transform bone = binding.PhysicalBones[boneIndex];
                if (!bone)
                    throw new ArgumentException(
                        $"Final animation physical writer Bone #{boneIndex} binding is missing.");
                m_Bones[boneIndex] = bone;
            }
            if (binding.Animator.transform != rootHierarchy.PoseRoot)
            {
                throw new ArgumentException(
                    "Final Pose writer must use the formal PoseRoot.",
                    nameof(rootHierarchy));
            }
            m_RootBoneIndex = rig.RootPhysicalBoneIndex;
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
            m_RootHierarchy = rootHierarchy;
            m_LeftAnkleBoneIndex = rig.LeftLeg.AnklePhysicalBoneIndex;
            m_RightAnkleBoneIndex = rig.RightLeg.AnklePhysicalBoneIndex;
            m_LeftToeBoneIndex = rig.LeftLeg.ToePhysicalBoneIndex;
            m_RightToeBoneIndex = rig.RightLeg.ToePhysicalBoneIndex;
            m_PelvisBoneIndex = rig.PelvisPhysicalBoneIndex;
#endif
            m_RootBonePolicy = rig.RootBonePolicy;
            CharacterAnimationPhysicalBonePayload root =
                rig.PhysicalBones[m_RootBoneIndex];
            m_RootReferencePose = new AnimationLocalBonePose(
                root.ReferenceLocalPosition,
                root.ReferenceLocalRotation,
                root.ReferenceLocalScale);
            if (!m_RootReferencePose.IsValid)
                throw new InvalidOperationException(
                    "Animation root reference pose is invalid.");
        }

        internal void Write(
            in ComposedAnimationPoseFrame frame,
            bool captureFootIkDiagnostics)
        {
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
            FootIkCapture = default;
#endif
            AnimationReadOnlyBuffer<AnimationLocalBonePose> poses = frame.DenseLocalPose;
            for (int boneIndex = 0; boneIndex < m_Bones.Length; boneIndex++)
            {
                if (!m_Bones[boneIndex])
                {
                    throw new InvalidOperationException(
                        $"Final animation physical writer Bone #{boneIndex} binding is missing.");
                }
            }

            for (int boneIndex = 0; boneIndex < m_Bones.Length; boneIndex++)
            {
                Transform bone = m_Bones[boneIndex];
                ref readonly AnimationLocalBonePose pose = ref
                    (m_RootBonePolicy == CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
                     boneIndex == m_RootBoneIndex
                        ? ref m_RootReferencePose
                        : ref poses.ElementAt(boneIndex));
                bone.SetLocalPositionAndRotation(pose.Position, pose.Rotation);
                bone.localScale = pose.Scale;
            }
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
            if (captureFootIkDiagnostics)
                FootIkCapture = CaptureFootIkPhysical(m_Bones[m_PelvisBoneIndex].position);
#endif
        }

#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
        CharacterFootIkPhysicalCapture CaptureFootIkPhysical(
            Vector3 pelvisWorldPosition)
        {
            Transform logicRoot = m_RootHierarchy.LogicRoot;
            Transform visualRoot = m_RootHierarchy.VisualRoot;
            Transform poseRoot = m_RootHierarchy.PoseRoot;
            Transform leftAnkle = m_Bones[m_LeftAnkleBoneIndex];
            Transform rightAnkle = m_Bones[m_RightAnkleBoneIndex];
            Transform leftToe = m_Bones[m_LeftToeBoneIndex];
            Transform rightToe = m_Bones[m_RightToeBoneIndex];
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
                leftToe.position,
                leftToe.rotation,
                rightAnkle.position,
                rightAnkle.rotation,
                rightToe.position,
                rightToe.rotation);
        }

#endif

    }
}
