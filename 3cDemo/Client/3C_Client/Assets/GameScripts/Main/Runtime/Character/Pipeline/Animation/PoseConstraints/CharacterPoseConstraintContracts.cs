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
            m_Handle = handle;
            m_Contribution = contribution;
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
        readonly CharacterFootPlacementConstraintHandle m_Handle;
        readonly CharacterFullBodyIkGoalContributionHeader m_Contribution;
        internal bool IsValid => m_IsValid;
        internal CharacterFullBodyIkGoalContributionHeader Contribution =>
            m_Contribution;
        internal CharacterFullBodyIkGoalContributionAvailability Availability =>
            m_Contribution.Availability;
        internal bool Matches(
            in CharacterFootPlacementConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            m_Handle.Equals(handle) &&
            m_Contribution.FrameSequence == frameSequence &&
            m_Contribution.CompletionIdentity == completionIdentity;
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
            m_Handle = handle;
            m_Contribution = contribution;
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
        readonly CharacterPoseBoneContributionConstraintHandle m_Handle;
        readonly CharacterFullBodyIkGoalContributionHeader m_Contribution;
        internal bool IsValid => m_IsValid;
        internal CharacterFullBodyIkGoalContributionHeader Contribution =>
            m_Contribution;
        internal bool Matches(
            in CharacterPoseBoneContributionConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            m_Handle.Equals(handle) &&
            m_Contribution.FrameSequence == frameSequence &&
            m_Contribution.CompletionIdentity == completionIdentity;
    }

    internal readonly struct CharacterFullBodyIkGoalAssemblerConstraintHandle :
        IEquatable<CharacterFullBodyIkGoalAssemblerConstraintHandle>
    {
        internal CharacterFullBodyIkGoalAssemblerConstraintHandle(
            int operationIndex,
            int callSiteIndex,
            int goalSetValueIndex,
            int contributionInputStart,
            int contributionInputCount)
        {
            if (operationIndex < 0 ||
                callSiteIndex < 0 ||
                goalSetValueIndex < 0 ||
                contributionInputStart < -1 ||
                contributionInputCount < 0 ||
                (contributionInputCount == 0) !=
                (contributionInputStart == -1))
            {
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            }
            OperationIndex = operationIndex;
            CallSiteIndex = callSiteIndex;
            GoalSetValueIndex = goalSetValueIndex;
            ContributionInputStart = contributionInputStart;
            ContributionInputCount = contributionInputCount;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal int OperationIndex { get; }
        internal int CallSiteIndex { get; }
        internal int GoalSetValueIndex { get; }
        internal int ContributionInputStart { get; }
        internal int ContributionInputCount { get; }
        internal bool IsValid => m_IsValid;

        public bool Equals(
            CharacterFullBodyIkGoalAssemblerConstraintHandle other) =>
            OperationIndex == other.OperationIndex &&
            CallSiteIndex == other.CallSiteIndex &&
            GoalSetValueIndex == other.GoalSetValueIndex &&
            ContributionInputStart == other.ContributionInputStart &&
            ContributionInputCount == other.ContributionInputCount &&
            m_IsValid == other.m_IsValid;

        public override bool Equals(object obj) =>
            obj is CharacterFullBodyIkGoalAssemblerConstraintHandle other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            OperationIndex,
            CallSiteIndex,
            GoalSetValueIndex,
            ContributionInputStart,
            ContributionInputCount,
            m_IsValid);
    }

    internal readonly struct CharacterFullBodyIkGoalAssemblerCatalog
    {
        internal CharacterFullBodyIkGoalAssemblerCatalog(
            NativeArray<int> contributionValueIndices,
            int goalSetValueCount)
        {
            if (!contributionValueIndices.IsCreated || goalSetValueCount < 0)
            {
                throw new ArgumentException(
                    "Full Body IK Goal Assembler catalog is invalid.",
                    nameof(contributionValueIndices));
            }
            m_ContributionValueIndices = contributionValueIndices;
            m_GoalSetValueCount = goalSetValueCount;
        }

        readonly NativeArray<int> m_ContributionValueIndices;
        readonly int m_GoalSetValueCount;
        internal bool IsValid =>
            m_ContributionValueIndices.IsCreated &&
            m_GoalSetValueCount >= 0;

        internal NativeSlice<int> Resolve(
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle)
        {
            if (!Contains(in handle))
            {
                throw new ArgumentException(
                    "Full Body IK Goal Assembler handle is outside its catalog.",
                    nameof(handle));
            }
            return new NativeSlice<int>(
                m_ContributionValueIndices,
                handle.ContributionInputCount == 0
                    ? 0
                    : handle.ContributionInputStart,
                handle.ContributionInputCount);
        }

        internal bool Contains(
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle) =>
            IsValid &&
            handle.IsValid &&
            (uint)handle.GoalSetValueIndex <
            (uint)m_GoalSetValueCount &&
            (handle.ContributionInputCount == 0 ||
             handle.ContributionInputStart <=
             m_ContributionValueIndices.Length -
             handle.ContributionInputCount);
    }

    internal readonly struct CharacterFullBodyIkGoalAssemblerOperationResult
    {
        internal CharacterFullBodyIkGoalAssemblerOperationResult(
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle,
            in CharacterFullBodyIkResult assembly,
            in CharacterFullBodyIkGoalSetHeader goalSet)
        {
            m_Handle = handle;
            m_Assembly = assembly;
            m_GoalSet = goalSet;
            m_IsValid =
                handle.IsValid &&
                assembly.Succeeded &&
                goalSet.IsValid &&
                goalSet.Availability ==
                CharacterFullBodyIkGoalSetAvailability.Ready &&
                goalSet.ProducerOperationIndex == handle.OperationIndex &&
                goalSet.ProducerCallSiteIndex == handle.CallSiteIndex;
        }

        readonly bool m_IsValid;
        readonly CharacterFullBodyIkGoalAssemblerConstraintHandle m_Handle;
        readonly CharacterFullBodyIkResult m_Assembly;
        readonly CharacterFullBodyIkGoalSetHeader m_GoalSet;
        internal bool IsValid => m_IsValid;
        internal CharacterFullBodyIkResult Assembly => m_Assembly;
        internal CharacterFullBodyIkGoalSetHeader GoalSet => m_GoalSet;
        internal bool Matches(
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            m_Assembly.Succeeded &&
            m_Handle.Equals(handle) &&
            m_GoalSet.FrameSequence == frameSequence &&
            m_GoalSet.CompletionIdentity == completionIdentity;
    }

    internal readonly struct CharacterFullBodyIkConstraintHandle :
        IEquatable<CharacterFullBodyIkConstraintHandle>
    {
        internal CharacterFullBodyIkConstraintHandle(
            int operationIndex,
            int callSiteIndex,
            int fullBodyIkIndex,
            int inputPoseValueIndex,
            int outputPoseValueIndex,
            int inputGoalSetValueIndex)
        {
            if (operationIndex < 0 ||
                callSiteIndex < 0 ||
                fullBodyIkIndex < 0 ||
                inputPoseValueIndex < 0 ||
                outputPoseValueIndex < 0 ||
                inputGoalSetValueIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            }
            OperationIndex = operationIndex;
            CallSiteIndex = callSiteIndex;
            FullBodyIkIndex = fullBodyIkIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            OutputPoseValueIndex = outputPoseValueIndex;
            InputGoalSetValueIndex = inputGoalSetValueIndex;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal int OperationIndex { get; }
        internal int CallSiteIndex { get; }
        internal int FullBodyIkIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int OutputPoseValueIndex { get; }
        internal int InputGoalSetValueIndex { get; }
        internal bool IsValid => m_IsValid;

        public bool Equals(CharacterFullBodyIkConstraintHandle other) =>
            OperationIndex == other.OperationIndex &&
            CallSiteIndex == other.CallSiteIndex &&
            FullBodyIkIndex == other.FullBodyIkIndex &&
            InputPoseValueIndex == other.InputPoseValueIndex &&
            OutputPoseValueIndex == other.OutputPoseValueIndex &&
            InputGoalSetValueIndex == other.InputGoalSetValueIndex &&
            m_IsValid == other.m_IsValid;

        public override bool Equals(object obj) =>
            obj is CharacterFullBodyIkConstraintHandle other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            OperationIndex,
            CallSiteIndex,
            FullBodyIkIndex,
            InputPoseValueIndex,
            OutputPoseValueIndex,
            InputGoalSetValueIndex,
            m_IsValid);
    }

    internal readonly struct CharacterFullBodyIkConstraintOperationResult
    {
        internal CharacterFullBodyIkConstraintOperationResult(
            in CharacterFullBodyIkConstraintHandle handle,
            in CharacterFullBodyIkResult solve,
            ulong frameSequence,
            ulong completionIdentity)
        {
            m_Handle = handle;
            m_Solve = solve;
            m_FrameSequence = frameSequence;
            m_CompletionIdentity = completionIdentity;
            m_IsValid =
                handle.IsValid &&
                solve.Succeeded &&
                frameSequence != 0 &&
                completionIdentity != 0;
        }

        readonly bool m_IsValid;
        readonly CharacterFullBodyIkConstraintHandle m_Handle;
        readonly CharacterFullBodyIkResult m_Solve;
        readonly ulong m_FrameSequence;
        readonly ulong m_CompletionIdentity;
        internal bool IsValid => m_IsValid;
        internal CharacterFullBodyIkResult Solve => m_Solve;
        internal bool Matches(
            in CharacterFullBodyIkConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity) =>
            m_IsValid &&
            m_Solve.Succeeded &&
            m_Handle.Equals(handle) &&
            m_FrameSequence == frameSequence &&
            m_CompletionIdentity == completionIdentity;
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
        internal CharacterComponentBonePose(in AnimationLocalBonePose pose)
        {
            Position = pose.Position;
            Rotation = pose.Rotation;
            Scale = pose.Scale;
        }

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

        internal static CharacterComponentBonePose CreateNormalized(
            Vector3 position,
            Quaternion normalizedRotation,
            Vector3 scale)
        {
            Position = position;
            Rotation = normalizedRotation;
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
                component = new CharacterComponentBonePose(in local);
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
                component = new CharacterComponentBonePose(in local);
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
            Quaternion rotation = parent.Rotation * local.Rotation;
            Vector3 scale = Vector3.Scale(parent.Scale, local.Scale);
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                Quaternion.Dot(rotation, rotation) <= 0f)
            {
                return false;
            }
            component = CharacterComponentBonePose.CreateNormalized(
                position,
                rotation.normalized,
                scale);
            return true;
        }

        internal static CharacterComponentBonePose CreateVirtualComponent(
            CharacterComponentBonePose source, CharacterComponentBonePose target) =>
            CharacterComponentBonePose.CreateNormalized(
                target.Position,
                target.Rotation,
                source.Scale);

        internal static CharacterComponentBonePose CreateVirtualComponent(
            AnimationLocalBonePose source, AnimationLocalBonePose target) =>
            CharacterComponentBonePose.CreateNormalized(
                target.Position,
                target.Rotation,
                source.Scale);

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
            Quaternion rotation = inverseParent * component.Rotation;
            Vector3 scale = new Vector3(
                component.Scale.x / parent.Scale.x,
                component.Scale.y / parent.Scale.y,
                component.Scale.z / parent.Scale.z);
            local = new AnimationLocalBonePose(position, rotation, scale);
            return true;
        }
    }
}
