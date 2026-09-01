using System;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseBoneKind : byte
    {
        Physical = 1,
        Virtual = 2
    }

    public readonly struct CharacterPoseBoneCounts
    {
        public CharacterPoseBoneCounts(int physicalBoneCount, int virtualBoneCount)
        {
            if (physicalBoneCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(physicalBoneCount));
            if (virtualBoneCount < 0)
                throw new ArgumentOutOfRangeException(nameof(virtualBoneCount));
            PhysicalBoneCount = physicalBoneCount;
            VirtualBoneCount = virtualBoneCount;
            PoseBoneCount = checked(physicalBoneCount + virtualBoneCount);
        }

        public int PhysicalBoneCount { get; }
        public int VirtualBoneCount { get; }
        public int PoseBoneCount { get; }
        public bool IsValid =>
            PhysicalBoneCount > 0 &&
            VirtualBoneCount >= 0 &&
            PoseBoneCount == PhysicalBoneCount + VirtualBoneCount;
    }

    internal readonly struct CharacterFootPlacementConstraintHandle :
        IEquatable<CharacterFootPlacementConstraintHandle>
    {
        internal CharacterFootPlacementConstraintHandle(
            int operationIndex,
            int callSiteIndex,
            int footPlacementIndex,
            int contributionValueIndex,
            int contributionGoalOffset)
        {
            if (operationIndex < 0 ||
                callSiteIndex < 0 ||
                footPlacementIndex < 0 ||
                contributionValueIndex < 0 ||
                contributionGoalOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            }
            OperationIndex = operationIndex;
            CallSiteIndex = callSiteIndex;
            FootPlacementIndex = footPlacementIndex;
            ContributionValueIndex = contributionValueIndex;
            ContributionGoalOffset = contributionGoalOffset;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal int OperationIndex { get; }
        internal int CallSiteIndex { get; }
        internal int FootPlacementIndex { get; }
        internal int ContributionValueIndex { get; }
        internal int ContributionGoalOffset { get; }
        internal bool IsValid => m_IsValid;

        public bool Equals(
            CharacterFootPlacementConstraintHandle other) =>
            OperationIndex == other.OperationIndex &&
            CallSiteIndex == other.CallSiteIndex &&
            FootPlacementIndex == other.FootPlacementIndex &&
            ContributionValueIndex == other.ContributionValueIndex &&
            ContributionGoalOffset == other.ContributionGoalOffset &&
            m_IsValid == other.m_IsValid;

        public override bool Equals(object obj) =>
            obj is CharacterFootPlacementConstraintHandle other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            OperationIndex,
            CallSiteIndex,
            FootPlacementIndex,
            ContributionValueIndex,
            ContributionGoalOffset,
            m_IsValid);
    }

    internal readonly struct CharacterFootPlacementConstraintOperationResult
    {
        internal CharacterFootPlacementConstraintOperationResult(
            in CharacterFootPlacementConstraintHandle handle,
            in CharacterFullBodyIkGoalContributionHeader contribution)
        {
            Handle = handle;
            Contribution = contribution;
            bool ready =
                contribution.Availability ==
                CharacterFullBodyIkGoalContributionAvailability.Ready &&
                contribution.GoalCount > 0;
            bool unavailable =
                contribution.Availability ==
                CharacterFullBodyIkGoalContributionAvailability
                    .WorldContextUnavailable &&
                contribution.GoalCount == 0;
            m_IsValid =
                handle.IsValid &&
                contribution.IsValid &&
                contribution.ProducerOperationIndex ==
                handle.OperationIndex &&
                contribution.ProducerCallSiteIndex ==
                handle.CallSiteIndex &&
                contribution.GoalOffset ==
                handle.ContributionGoalOffset &&
                (ready || unavailable);
        }

        readonly bool m_IsValid;
        internal CharacterFootPlacementConstraintHandle Handle { get; }
        internal CharacterFullBodyIkGoalContributionHeader Contribution
        {
            get;
        }
        internal bool IsValid => m_IsValid;
        internal bool Matches(
            in CharacterFootPlacementConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            Handle.Equals(handle) &&
            Contribution.FrameSequence == frameSequence &&
            Contribution.CompletionIdentity == completionIdentity;
    }

    internal readonly struct CharacterPoseBoneContributionConstraintHandle :
        IEquatable<CharacterPoseBoneContributionConstraintHandle>
    {
        internal CharacterPoseBoneContributionConstraintHandle(
            int operationIndex,
            int callSiteIndex,
            int inputPoseValueIndex,
            int contributionValueIndex,
            int contributionGoalOffset,
            int descriptorOffset,
            int descriptorCount)
        {
            if (operationIndex < 0 ||
                callSiteIndex < 0 ||
                inputPoseValueIndex < 0 ||
                contributionValueIndex < 0 ||
                contributionGoalOffset < 0 ||
                descriptorOffset < 0 ||
                descriptorCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            }
            OperationIndex = operationIndex;
            CallSiteIndex = callSiteIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            ContributionValueIndex = contributionValueIndex;
            ContributionGoalOffset = contributionGoalOffset;
            DescriptorOffset = descriptorOffset;
            GoalCount = descriptorCount;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal int OperationIndex { get; }
        internal int CallSiteIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int ContributionValueIndex { get; }
        internal int ContributionGoalOffset { get; }
        internal int DescriptorOffset { get; }
        internal int GoalCount { get; }
        internal bool IsValid => m_IsValid;

        public bool Equals(
            CharacterPoseBoneContributionConstraintHandle other) =>
            OperationIndex == other.OperationIndex &&
            CallSiteIndex == other.CallSiteIndex &&
            InputPoseValueIndex == other.InputPoseValueIndex &&
            ContributionValueIndex == other.ContributionValueIndex &&
            ContributionGoalOffset == other.ContributionGoalOffset &&
            DescriptorOffset == other.DescriptorOffset &&
            GoalCount == other.GoalCount &&
            m_IsValid == other.m_IsValid;

        public override bool Equals(object obj) =>
            obj is CharacterPoseBoneContributionConstraintHandle other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            OperationIndex,
            CallSiteIndex,
            InputPoseValueIndex,
            ContributionValueIndex,
            ContributionGoalOffset,
            DescriptorOffset,
            GoalCount,
            m_IsValid);
    }

    internal readonly struct CharacterPoseBoneContributionCatalog
    {
        internal CharacterPoseBoneContributionCatalog(
            NativeArray<CharacterPoseBoneIkGoalDescriptor> descriptors)
        {
            if (!descriptors.IsCreated)
                throw new ArgumentException(
                    "Pose Bone Contribution catalog is invalid.",
                    nameof(descriptors));
            m_Descriptors = descriptors;
        }

        readonly NativeArray<CharacterPoseBoneIkGoalDescriptor> m_Descriptors;
        internal bool IsValid => m_Descriptors.IsCreated;

        internal NativeSlice<CharacterPoseBoneIkGoalDescriptor> Resolve(
            in CharacterPoseBoneContributionConstraintHandle handle)
        {
            if (!IsValid ||
                !handle.IsValid ||
                handle.DescriptorOffset >
                m_Descriptors.Length - handle.GoalCount)
            {
                throw new ArgumentException(
                    "Pose Bone Contribution handle is outside its catalog.",
                    nameof(handle));
            }
            return new NativeSlice<CharacterPoseBoneIkGoalDescriptor>(
                m_Descriptors,
                handle.DescriptorOffset,
                handle.GoalCount);
        }
    }

    internal readonly struct CharacterPoseBoneContributionOperationResult
    {
        internal CharacterPoseBoneContributionOperationResult(
            in CharacterPoseBoneContributionConstraintHandle handle,
            in CharacterFullBodyIkGoalContributionHeader contribution)
        {
            Handle = handle;
            Contribution = contribution;
            m_IsValid =
                handle.IsValid &&
                contribution.IsValid &&
                contribution.Availability ==
                CharacterFullBodyIkGoalContributionAvailability.Ready &&
                contribution.ProducerOperationIndex ==
                handle.OperationIndex &&
                contribution.ProducerCallSiteIndex ==
                handle.CallSiteIndex &&
                contribution.GoalOffset ==
                handle.ContributionGoalOffset &&
                contribution.GoalCount == handle.GoalCount;
        }

        readonly bool m_IsValid;
        internal CharacterPoseBoneContributionConstraintHandle Handle { get; }
        internal CharacterFullBodyIkGoalContributionHeader Contribution
        {
            get;
        }
        internal bool IsValid => m_IsValid;
        internal bool Matches(
            in CharacterPoseBoneContributionConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            Handle.Equals(handle) &&
            Contribution.FrameSequence == frameSequence &&
            Contribution.CompletionIdentity == completionIdentity;
    }

    public readonly struct CharacterPoseBoneRuntimeId : IEquatable<CharacterPoseBoneRuntimeId>
    {
        public CharacterPoseBoneRuntimeId(string value)
        {
            Value = new FixedString128Bytes(PoseIdentity.Require(value, nameof(value)));
        }

        public CharacterPoseBoneRuntimeId(AnimationBoneId value)
            : this(value.IsValid ? value.Value : throw new ArgumentException("Pose Bone identity is invalid.", nameof(value)))
        {
        }

        public FixedString128Bytes Value { get; }
        public bool IsValid => Value.Length > 0;
        public bool Equals(CharacterPoseBoneRuntimeId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is CharacterPoseBoneRuntimeId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct CharacterPoseConstraintId : IEquatable<CharacterPoseConstraintId>
    {
        public CharacterPoseConstraintId(string value)
        {
            Value = new FixedString128Bytes(PoseIdentity.Require(value, nameof(value)));
        }

        public FixedString128Bytes Value { get; }
        public bool IsValid => Value.Length > 0;
        public bool Equals(CharacterPoseConstraintId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is CharacterPoseConstraintId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct CharacterVirtualBoneDescriptor
    {
        public CharacterVirtualBoneDescriptor(
            CharacterPoseBoneRuntimeId virtualBoneId,
            int sourcePhysicalBoneIndex,
            int targetPhysicalBoneIndex,
            int poseBoneIndex)
        {
            if (!virtualBoneId.IsValid)
                throw new ArgumentException("Virtual Bone identity is invalid.", nameof(virtualBoneId));
            if (sourcePhysicalBoneIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourcePhysicalBoneIndex));
            if (targetPhysicalBoneIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(targetPhysicalBoneIndex));
            if (sourcePhysicalBoneIndex == targetPhysicalBoneIndex)
                throw new ArgumentException("Virtual Bone Source and Target must be different.");
            if (poseBoneIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIndex));
            VirtualBoneId = virtualBoneId;
            SourcePhysicalBoneIndex = sourcePhysicalBoneIndex;
            TargetPhysicalBoneIndex = targetPhysicalBoneIndex;
            PoseBoneIndex = poseBoneIndex;
        }

        public CharacterPoseBoneRuntimeId VirtualBoneId { get; }
        public int SourcePhysicalBoneIndex { get; }
        public int TargetPhysicalBoneIndex { get; }
        public int PoseBoneIndex { get; }
        public bool IsValid =>
            VirtualBoneId.IsValid &&
            SourcePhysicalBoneIndex >= 0 &&
            TargetPhysicalBoneIndex >= 0 &&
            SourcePhysicalBoneIndex != TargetPhysicalBoneIndex &&
            PoseBoneIndex >= 0;
    }

    public readonly struct CharacterComponentBonePose
    {
        public CharacterComponentBonePose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!CharacterPoseConstraintMath.IsFinite(position) ||
                !CharacterPoseConstraintMath.IsFinite(rotation) ||
                !CharacterPoseConstraintMath.IsFinite(scale) ||
                Quaternion.Dot(rotation, rotation) <= 0f)
            {
                throw new ArgumentException("Component Bone pose is invalid.");
            }
            Position = position;
            Rotation = rotation.normalized;
            Scale = scale;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public bool IsValid =>
            CharacterPoseConstraintMath.IsFinite(Position) &&
            CharacterPoseConstraintMath.IsFinite(Rotation) &&
            CharacterPoseConstraintMath.IsFinite(Scale) &&
            Quaternion.Dot(Rotation, Rotation) > 0f;
    }

    public enum CharacterVirtualBonePoseFailure : byte
    {
        None = 0,
        InvalidCounts = 1,
        InvalidPhysicalHierarchy = 2,
        InvalidPhysicalPose = 3,
        InvalidVirtualDescriptor = 4,
        DuplicateVirtualBoneIdentity = 5,
        DegenerateSourceScale = 6,
        NonFiniteResult = 7
    }

    public readonly struct CharacterVirtualBonePoseResult
    {
        CharacterVirtualBonePoseResult(
            bool completed,
            CharacterVirtualBonePoseFailure failure,
            int virtualBoneIndex,
            CharacterPoseBoneRuntimeId virtualBoneId)
        {
            Completed = completed;
            Failure = failure;
            VirtualBoneIndex = virtualBoneIndex;
            VirtualBoneId = virtualBoneId;
        }

        public bool Completed { get; }
        public CharacterVirtualBonePoseFailure Failure { get; }
        public int VirtualBoneIndex { get; }
        public CharacterPoseBoneRuntimeId VirtualBoneId { get; }
        public bool Succeeded => Completed && Failure == CharacterVirtualBonePoseFailure.None;

        internal static CharacterVirtualBonePoseResult Success() =>
            new CharacterVirtualBonePoseResult(true, CharacterVirtualBonePoseFailure.None, -1, default);

        internal static CharacterVirtualBonePoseResult Fail(
            CharacterVirtualBonePoseFailure failure,
            int virtualBoneIndex = -1,
            CharacterPoseBoneRuntimeId virtualBoneId = default) =>
            new CharacterVirtualBonePoseResult(true, failure, virtualBoneIndex, virtualBoneId);
    }

    internal static class CharacterPoseConstraintMath
    {
        internal const float Epsilon = 0.000001f;

        internal static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        internal static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z) &&
            float.IsFinite(value.w);

        internal static bool IsUsableScale(Vector3 value) =>
            IsFinite(value) &&
            Mathf.Abs(value.x) > Epsilon &&
            Mathf.Abs(value.y) > Epsilon &&
            Mathf.Abs(value.z) > Epsilon;

        internal static bool TryCreateComponent(
            AnimationLocalBonePose local,
            int parentIndex,
            NativeArray<CharacterComponentBonePose> componentPoses,
            out CharacterComponentBonePose component)
        {
            component = default;
            if (!local.IsValid)
                return false;
            if (parentIndex < 0)
            {
                component = new CharacterComponentBonePose(local.Position, local.Rotation, local.Scale);
                return true;
            }
            return TryCreateComponent(local, componentPoses[parentIndex], out component);
        }

        internal static bool TryCreateComponent(
            AnimationLocalBonePose local,
            int parentIndex,
            CharacterComponentBonePose[] componentPoses,
            int componentOffset,
            out CharacterComponentBonePose component)
        {
            component = default;
            if (!local.IsValid)
                return false;
            if (parentIndex < 0)
            {
                component = new CharacterComponentBonePose(local.Position, local.Rotation, local.Scale);
                return true;
            }
            return TryCreateComponent(
                local,
                componentPoses[componentOffset + parentIndex],
                out component);
        }

        internal static bool TryCreateComponent(
            AnimationLocalBonePose local,
            CharacterComponentBonePose parent,
            out CharacterComponentBonePose component)
        {
            component = default;
            if (!parent.IsValid)
                return false;
            Vector3 position = parent.Position +
                               parent.Rotation * Vector3.Scale(parent.Scale, local.Position);
            Quaternion rotation = (parent.Rotation * local.Rotation).normalized;
            Vector3 scale = Vector3.Scale(parent.Scale, local.Scale);
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                Quaternion.Dot(rotation, rotation) <= 0f)
            {
                return false;
            }
            component = new CharacterComponentBonePose(position, rotation, scale);
            return true;
        }

        internal static Vector3 TransformPoint(CharacterComponentBonePose pose, Vector3 localPoint) =>
            pose.Position + pose.Rotation * Vector3.Scale(pose.Scale, localPoint);

        internal static bool TryCreateLocal(
            CharacterComponentBonePose component,
            CharacterComponentBonePose parent,
            out AnimationLocalBonePose local)
        {
            local = default;
            if (!component.IsValid || !parent.IsValid || !IsUsableScale(parent.Scale))
                return false;
            Quaternion inverseParent = Quaternion.Inverse(parent.Rotation);
            Vector3 position = inverseParent * (component.Position - parent.Position);
            position = new Vector3(
                position.x / parent.Scale.x,
                position.y / parent.Scale.y,
                position.z / parent.Scale.z);
            Quaternion rotation = (inverseParent * component.Rotation).normalized;
            Vector3 scale = new Vector3(
                component.Scale.x / parent.Scale.x,
                component.Scale.y / parent.Scale.y,
                component.Scale.z / parent.Scale.z);
            local = new AnimationLocalBonePose(position, rotation, scale);
            return local.IsValid;
        }
    }
}
