using System;
using RootMotion.FinalIK;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterFinalIkPoseBufferBackend : IIndexedPoseBackend
    {
        public const string SourceIdentity = "rootmotion.finalik.full-body-biped-ik/indexed-pose-backend";
        public const string AuditedVendorSourceRevision = "7cd67a8e9ca9e22b68e466f60bf27aa29ea653cf3edc619566b0ac6d41ee3cb1";
        const float ScaleEpsilon = 0.000001f;

        readonly CharacterPoseBoneCounts m_Counts;
        readonly NativeArray<int> m_ParentIndices;
        readonly NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        readonly Vector3[] m_ReferenceComponentPositions;
        readonly Quaternion[] m_ReferenceComponentRotations;
        readonly int[] m_DescendantOffsets;
        readonly int[] m_DescendantIndices;
        NativeSlice<AnimationLocalBonePose> m_ComponentPose;

        public CharacterFinalIkPoseBufferBackend(
            CharacterAnimationRigPayload rig,
            NativeArray<int> parentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_Counts = rig.BoneCounts;
            if (!parentIndices.IsCreated || parentIndices.Length != m_Counts.PoseBoneCount)
                throw new ArgumentException("FinalIK Pose Buffer parent indices do not match the Animation Rig.", nameof(parentIndices));
            if (!virtualBones.IsCreated || virtualBones.Length != m_Counts.VirtualBoneCount)
                throw new ArgumentException("FinalIK Pose Buffer virtual descriptors do not match the Animation Rig.", nameof(virtualBones));
            m_ParentIndices = parentIndices;
            m_VirtualBones = virtualBones;
            m_ReferenceComponentPositions = new Vector3[m_Counts.PhysicalBoneCount];
            m_ReferenceComponentRotations = new Quaternion[m_Counts.PhysicalBoneCount];
            BuildReferencePose(rig);
            BuildDescendantIndex(out m_DescendantOffsets, out m_DescendantIndices);
        }

        public int BoneCount => m_Counts.PoseBoneCount;

        public static IndexedBipedReferences CreateBipedReferences(CharacterAnimationRigPayload rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            var spine = new IndexedBoneHandle[rig.OrderedSpinePhysicalBoneIndices.Count];
            for (int i = 0; i < spine.Length; i++)
                spine[i] = new IndexedBoneHandle(rig.OrderedSpinePhysicalBoneIndices[i]);
            return new IndexedBipedReferences(
                new IndexedBoneHandle(rig.RootPhysicalBoneIndex),
                new IndexedBoneHandle(rig.SolverRootPhysicalBoneIndex),
                new IndexedBoneHandle(rig.PelvisPhysicalBoneIndex),
                spine,
                rig.HasHead ? new IndexedBoneHandle(rig.HeadPhysicalBoneIndex) : IndexedBoneHandle.Invalid,
                rig.LeftArm.HasClavicle ? new IndexedBoneHandle(rig.LeftArm.ClaviclePhysicalBoneIndex) : IndexedBoneHandle.Invalid,
                new IndexedBoneHandle(rig.LeftArm.UpperArmPhysicalBoneIndex),
                new IndexedBoneHandle(rig.LeftArm.ForearmPhysicalBoneIndex),
                new IndexedBoneHandle(rig.LeftArm.HandPhysicalBoneIndex),
                rig.RightArm.HasClavicle ? new IndexedBoneHandle(rig.RightArm.ClaviclePhysicalBoneIndex) : IndexedBoneHandle.Invalid,
                new IndexedBoneHandle(rig.RightArm.UpperArmPhysicalBoneIndex),
                new IndexedBoneHandle(rig.RightArm.ForearmPhysicalBoneIndex),
                new IndexedBoneHandle(rig.RightArm.HandPhysicalBoneIndex),
                new IndexedBoneHandle(rig.LeftLeg.HipPhysicalBoneIndex),
                new IndexedBoneHandle(rig.LeftLeg.KneePhysicalBoneIndex),
                new IndexedBoneHandle(rig.LeftLeg.AnklePhysicalBoneIndex),
                new IndexedBoneHandle(rig.RightLeg.HipPhysicalBoneIndex),
                new IndexedBoneHandle(rig.RightLeg.KneePhysicalBoneIndex),
                new IndexedBoneHandle(rig.RightLeg.AnklePhysicalBoneIndex));
        }

        public void Bind(NativeSlice<AnimationLocalBonePose> componentPose)
        {
            if (componentPose.Length != m_Counts.PoseBoneCount)
                throw new ArgumentException("FinalIK Pose Buffer page does not match the Animation Rig.", nameof(componentPose));
            m_ComponentPose = componentPose;
        }

        public IndexedBoneHandle GetParent(IndexedBoneHandle bone)
        {
            int index = RequireBone(bone);
            int parent = m_ParentIndices[index];
            return parent >= 0 ? new IndexedBoneHandle(parent) : IndexedBoneHandle.Invalid;
        }

        public Vector3 GetComponentPosition(IndexedBoneHandle bone) => RequirePose(bone).Position;
        public Quaternion GetComponentRotation(IndexedBoneHandle bone) => RequirePose(bone).Rotation;

        public Vector3 GetLocalPosition(IndexedBoneHandle bone)
        {
            int index = RequireBone(bone);
            AnimationLocalBonePose value = RequirePose(bone);
            int parentIndex = m_ParentIndices[index];
            if (parentIndex < 0)
                return value.Position;
            AnimationLocalBonePose parent = m_ComponentPose[parentIndex];
            return Divide(Quaternion.Inverse(parent.Rotation) * (value.Position - parent.Position), parent.Scale);
        }

        public Quaternion GetLocalRotation(IndexedBoneHandle bone)
        {
            int index = RequireBone(bone);
            AnimationLocalBonePose value = RequirePose(bone);
            int parentIndex = m_ParentIndices[index];
            return parentIndex < 0
                ? value.Rotation
                : Quaternion.Inverse(m_ComponentPose[parentIndex].Rotation) * value.Rotation;
        }

        public Vector3 GetReferenceComponentPosition(IndexedBoneHandle bone)
        {
            int index = RequirePhysicalBone(bone);
            return m_ReferenceComponentPositions[index];
        }

        public Quaternion GetReferenceComponentRotation(IndexedBoneHandle bone)
        {
            int index = RequirePhysicalBone(bone);
            return m_ReferenceComponentRotations[index];
        }

        public void SetComponentPosition(IndexedBoneHandle bone, Vector3 position)
        {
            int index = RequireWritableBone(bone);
            RequireFinite(position, nameof(position));
            AnimationLocalBonePose current = m_ComponentPose[index];
            Vector3 delta = position - current.Position;
            m_ComponentPose[index] = new AnimationLocalBonePose(position, current.Rotation, current.Scale);
            if (delta == Vector3.zero)
                return;
            int end = m_DescendantOffsets[index + 1];
            for (int descendant = m_DescendantOffsets[index]; descendant < end; descendant++)
            {
                int child = m_DescendantIndices[descendant];
                AnimationLocalBonePose value = m_ComponentPose[child];
                m_ComponentPose[child] = new AnimationLocalBonePose(value.Position + delta, value.Rotation, value.Scale);
            }
        }

        public void SetComponentRotation(IndexedBoneHandle bone, Quaternion rotation)
        {
            int index = RequireWritableBone(bone);
            RequireFinite(rotation, nameof(rotation));
            AnimationLocalBonePose current = m_ComponentPose[index];
            Quaternion normalized = rotation.normalized;
            Quaternion delta = normalized * Quaternion.Inverse(current.Rotation);
            m_ComponentPose[index] = new AnimationLocalBonePose(current.Position, normalized, current.Scale);
            int end = m_DescendantOffsets[index + 1];
            for (int descendant = m_DescendantOffsets[index]; descendant < end; descendant++)
            {
                int child = m_DescendantIndices[descendant];
                AnimationLocalBonePose value = m_ComponentPose[child];
                m_ComponentPose[child] = new AnimationLocalBonePose(
                    current.Position + delta * (value.Position - current.Position),
                    delta * value.Rotation,
                    value.Scale);
            }
        }

        public void SetLocalPosition(IndexedBoneHandle bone, Vector3 position)
        {
            int index = RequireWritableBone(bone);
            RequireFinite(position, nameof(position));
            int parentIndex = m_ParentIndices[index];
            if (parentIndex < 0)
            {
                SetComponentPosition(bone, position);
                return;
            }
            AnimationLocalBonePose parent = m_ComponentPose[parentIndex];
            SetComponentPosition(
                bone,
                parent.Position + parent.Rotation * Vector3.Scale(parent.Scale, position));
        }

        public void SetLocalRotation(IndexedBoneHandle bone, Quaternion rotation)
        {
            int index = RequireWritableBone(bone);
            RequireFinite(rotation, nameof(rotation));
            int parentIndex = m_ParentIndices[index];
            SetComponentRotation(
                bone,
                parentIndex < 0 ? rotation : m_ComponentPose[parentIndex].Rotation * rotation);
        }

        public bool IsWritablePhysicalBone(IndexedBoneHandle bone) =>
            bone.IsValid && bone.Index < m_Counts.PhysicalBoneCount;

        public void RebuildVirtualBones()
        {
            RequireBound();
            for (int i = 0; i < m_VirtualBones.Length; i++)
            {
                CharacterVirtualBoneDescriptor descriptor = m_VirtualBones[i];
                if (!descriptor.IsValid ||
                    descriptor.SourcePhysicalBoneIndex >= m_Counts.PhysicalBoneCount ||
                    descriptor.TargetPhysicalBoneIndex >= m_Counts.PhysicalBoneCount ||
                    descriptor.PoseBoneIndex != m_Counts.PhysicalBoneCount + i)
                {
                    throw new InvalidOperationException($"FinalIK virtual bone descriptor #{i} is invalid.");
                }
                AnimationLocalBonePose source = m_ComponentPose[descriptor.SourcePhysicalBoneIndex];
                AnimationLocalBonePose target = m_ComponentPose[descriptor.TargetPhysicalBoneIndex];
                m_ComponentPose[descriptor.PoseBoneIndex] = new AnimationLocalBonePose(
                    target.Position,
                    target.Rotation,
                    source.Scale);
            }
        }

        void BuildReferencePose(CharacterAnimationRigPayload rig)
        {
            Vector3[] scales = new Vector3[m_Counts.PhysicalBoneCount];
            for (int i = 0; i < m_Counts.PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBonePayload bone = rig.PhysicalBones[i];
                int parent = bone.ParentPhysicalIndex;
                if (parent < -1 || parent >= i)
                    throw new ArgumentException($"FinalIK reference hierarchy bone #{i} is invalid.", nameof(rig));
                if (parent < 0)
                {
                    m_ReferenceComponentPositions[i] = bone.ReferenceLocalPosition;
                    m_ReferenceComponentRotations[i] = bone.ReferenceLocalRotation;
                    scales[i] = bone.ReferenceLocalScale;
                    continue;
                }
                m_ReferenceComponentPositions[i] = m_ReferenceComponentPositions[parent] +
                    m_ReferenceComponentRotations[parent] * Vector3.Scale(scales[parent], bone.ReferenceLocalPosition);
                m_ReferenceComponentRotations[i] = m_ReferenceComponentRotations[parent] * bone.ReferenceLocalRotation;
                scales[i] = Vector3.Scale(scales[parent], bone.ReferenceLocalScale);
            }
        }

        void BuildDescendantIndex(
            out int[] offsets,
            out int[] descendants)
        {
            int physicalBoneCount = m_Counts.PhysicalBoneCount;
            offsets = new int[physicalBoneCount + 1];
            for (int child = 0; child < m_Counts.PhysicalBoneCount; child++)
            {
                int parent = m_ParentIndices[child];
                while (parent >= 0)
                {
                    if (parent >= child)
                        throw new ArgumentException($"FinalIK Pose Buffer hierarchy bone #{child} is invalid.", nameof(m_ParentIndices));
                    offsets[parent + 1]++;
                    parent = m_ParentIndices[parent];
                }
            }
            for (int parent = 0; parent < physicalBoneCount; parent++)
                offsets[parent + 1] = checked(offsets[parent + 1] + offsets[parent]);
            descendants = new int[offsets[physicalBoneCount]];
            var writeOffsets = new int[physicalBoneCount];
            Array.Copy(offsets, writeOffsets, physicalBoneCount);
            for (int child = 0; child < physicalBoneCount; child++)
            {
                int parent = m_ParentIndices[child];
                while (parent >= 0)
                {
                    descendants[writeOffsets[parent]++] = child;
                    parent = m_ParentIndices[parent];
                }
            }
        }

        AnimationLocalBonePose RequirePose(IndexedBoneHandle bone)
        {
            int index = RequireBone(bone);
            RequireBound();
            AnimationLocalBonePose value = m_ComponentPose[index];
            if (!value.IsValid)
                throw new InvalidOperationException($"FinalIK Pose Buffer bone #{index} is invalid.");
            return value;
        }

        int RequireBone(IndexedBoneHandle bone)
        {
            if (!bone.IsValid || bone.Index >= m_Counts.PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(bone));
            return bone.Index;
        }

        int RequirePhysicalBone(IndexedBoneHandle bone)
        {
            int index = RequireBone(bone);
            if (index >= m_Counts.PhysicalBoneCount)
                throw new InvalidOperationException("FinalIK reference access requires a Physical Bone.");
            return index;
        }

        int RequireWritableBone(IndexedBoneHandle bone)
        {
            int index = RequirePhysicalBone(bone);
            if (!IsWritablePhysicalBone(bone))
                throw new InvalidOperationException("FinalIK cannot write a Virtual Bone.");
            RequireBound();
            return index;
        }

        void RequireBound()
        {
            if (m_ComponentPose.Length != m_Counts.PoseBoneCount)
                throw new InvalidOperationException("FinalIK Pose Buffer backend is not bound to a Component Pose page.");
        }

        static Vector3 Divide(Vector3 value, Vector3 divisor)
        {
            if (Mathf.Abs(divisor.x) <= ScaleEpsilon ||
                Mathf.Abs(divisor.y) <= ScaleEpsilon ||
                Mathf.Abs(divisor.z) <= ScaleEpsilon)
                throw new InvalidOperationException("FinalIK Pose Buffer contains a degenerate parent scale.");
            return new Vector3(value.x / divisor.x, value.y / divisor.y, value.z / divisor.z);
        }

        static void RequireFinite(Vector3 value, string parameter)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentException("FinalIK Pose Buffer position is not finite.", parameter);
        }

        static void RequireFinite(Quaternion value, string parameter)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) || Quaternion.Dot(value, value) <= 0f)
                throw new ArgumentException("FinalIK Pose Buffer rotation is invalid.", parameter);
        }
    }
}
