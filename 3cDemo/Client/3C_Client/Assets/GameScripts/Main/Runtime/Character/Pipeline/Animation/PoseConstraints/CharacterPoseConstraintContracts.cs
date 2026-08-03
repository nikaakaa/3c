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

    public enum CharacterTwoBoneIkEndRotationMode : byte
    {
        PreserveInput = 1,
        MatchEffector = 2
    }

    public readonly struct CharacterTwoBoneIkDescriptor
    {
        public CharacterTwoBoneIkDescriptor(
            CharacterPoseConstraintId constraintId,
            int rootPhysicalBoneIndex,
            int jointPhysicalBoneIndex,
            int endPhysicalBoneIndex,
            int effectorPoseBoneIndex,
            Vector3 effectorLocalPositionOffset,
            Quaternion effectorLocalRotationOffset,
            int jointTargetPoseBoneIndex,
            Vector3 jointTargetLocalOffset,
            CharacterTwoBoneIkEndRotationMode endRotationMode,
            float weight)
        {
            if (!constraintId.IsValid)
                throw new ArgumentException("Two Bone IK constraint identity is invalid.", nameof(constraintId));
            if (rootPhysicalBoneIndex < 0 || jointPhysicalBoneIndex < 0 || endPhysicalBoneIndex < 0 ||
                rootPhysicalBoneIndex == jointPhysicalBoneIndex ||
                rootPhysicalBoneIndex == endPhysicalBoneIndex ||
                jointPhysicalBoneIndex == endPhysicalBoneIndex)
            {
                throw new ArgumentException("Two Bone IK Physical chain is invalid.");
            }
            if (effectorPoseBoneIndex < 0 ||
                effectorPoseBoneIndex == rootPhysicalBoneIndex ||
                effectorPoseBoneIndex == jointPhysicalBoneIndex ||
                effectorPoseBoneIndex == endPhysicalBoneIndex)
            {
                throw new ArgumentException("Two Bone IK Effector is invalid.", nameof(effectorPoseBoneIndex));
            }
            if (jointTargetPoseBoneIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(jointTargetPoseBoneIndex));
            if (!CharacterPoseConstraintMath.IsFinite(effectorLocalPositionOffset) ||
                !CharacterPoseConstraintMath.IsFinite(effectorLocalRotationOffset) ||
                Quaternion.Dot(effectorLocalRotationOffset, effectorLocalRotationOffset) <= 0f)
            {
                throw new ArgumentException("Two Bone IK Effector offset is invalid.");
            }
            if (!CharacterPoseConstraintMath.IsFinite(jointTargetLocalOffset) ||
                jointTargetLocalOffset.sqrMagnitude <= 0f)
            {
                throw new ArgumentException("Two Bone IK Joint Target offset is invalid.", nameof(jointTargetLocalOffset));
            }
            if (endRotationMode != CharacterTwoBoneIkEndRotationMode.PreserveInput &&
                endRotationMode != CharacterTwoBoneIkEndRotationMode.MatchEffector)
                throw new ArgumentOutOfRangeException(nameof(endRotationMode));
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentOutOfRangeException(nameof(weight));
            ConstraintId = constraintId;
            RootPhysicalBoneIndex = rootPhysicalBoneIndex;
            JointPhysicalBoneIndex = jointPhysicalBoneIndex;
            EndPhysicalBoneIndex = endPhysicalBoneIndex;
            EffectorPoseBoneIndex = effectorPoseBoneIndex;
            EffectorLocalPositionOffset = effectorLocalPositionOffset;
            EffectorLocalRotationOffset = effectorLocalRotationOffset.normalized;
            JointTargetPoseBoneIndex = jointTargetPoseBoneIndex;
            JointTargetLocalOffset = jointTargetLocalOffset;
            EndRotationMode = endRotationMode;
            Weight = weight;
        }

        public CharacterPoseConstraintId ConstraintId { get; }
        public int RootPhysicalBoneIndex { get; }
        public int JointPhysicalBoneIndex { get; }
        public int EndPhysicalBoneIndex { get; }
        public int EffectorPoseBoneIndex { get; }
        public Vector3 EffectorLocalPositionOffset { get; }
        public Quaternion EffectorLocalRotationOffset { get; }
        public int JointTargetPoseBoneIndex { get; }
        public Vector3 JointTargetLocalOffset { get; }
        public CharacterTwoBoneIkEndRotationMode EndRotationMode { get; }
        public float Weight { get; }
        public bool IsValid =>
            ConstraintId.IsValid &&
            RootPhysicalBoneIndex >= 0 &&
            JointPhysicalBoneIndex >= 0 &&
            EndPhysicalBoneIndex >= 0 &&
            RootPhysicalBoneIndex != JointPhysicalBoneIndex &&
            RootPhysicalBoneIndex != EndPhysicalBoneIndex &&
            JointPhysicalBoneIndex != EndPhysicalBoneIndex &&
            EffectorPoseBoneIndex >= 0 &&
            EffectorPoseBoneIndex != RootPhysicalBoneIndex &&
            EffectorPoseBoneIndex != JointPhysicalBoneIndex &&
            EffectorPoseBoneIndex != EndPhysicalBoneIndex &&
            JointTargetPoseBoneIndex >= 0 &&
            CharacterPoseConstraintMath.IsFinite(EffectorLocalPositionOffset) &&
            CharacterPoseConstraintMath.IsFinite(EffectorLocalRotationOffset) &&
            Quaternion.Dot(EffectorLocalRotationOffset, EffectorLocalRotationOffset) > 0f &&
            CharacterPoseConstraintMath.IsFinite(JointTargetLocalOffset) &&
            JointTargetLocalOffset.sqrMagnitude > 0f &&
            (EndRotationMode == CharacterTwoBoneIkEndRotationMode.PreserveInput ||
             EndRotationMode == CharacterTwoBoneIkEndRotationMode.MatchEffector) &&
            float.IsFinite(Weight) &&
            Weight >= 0f &&
            Weight <= 1f;
    }

    public enum CharacterTwoBoneIkFailure : byte
    {
        None = 0,
        InvalidCounts = 1,
        InvalidHierarchy = 2,
        InvalidDescriptor = 3,
        InvalidPose = 4,
        NonFiniteInput = 5,
        ZeroLengthChain = 6,
        DegenerateEffectorDirection = 7,
        DegenerateJointTargetPlane = 8,
        NumericalFailure = 9
    }

    public enum CharacterTwoBoneIkReachState : byte
    {
        InRange = 1,
        ClampedMinimum = 2,
        ClampedMaximum = 3
    }

    public readonly struct CharacterTwoBoneIkResult
    {
        CharacterTwoBoneIkResult(
            bool completed,
            CharacterTwoBoneIkFailure failure,
            CharacterTwoBoneIkReachState reachState,
            Vector3 positionResidual,
            float targetDistance,
            float solveDistance,
            float upperLength,
            float lowerLength)
        {
            Completed = completed;
            Failure = failure;
            ReachState = reachState;
            PositionResidual = positionResidual;
            TargetDistance = targetDistance;
            SolveDistance = solveDistance;
            UpperLength = upperLength;
            LowerLength = lowerLength;
        }

        public bool Completed { get; }
        public CharacterTwoBoneIkFailure Failure { get; }
        public CharacterTwoBoneIkReachState ReachState { get; }
        public Vector3 PositionResidual { get; }
        public float PositionResidualMagnitude => PositionResidual.magnitude;
        public float TargetDistance { get; }
        public float SolveDistance { get; }
        public float UpperLength { get; }
        public float LowerLength { get; }
        public bool Succeeded => Completed && Failure == CharacterTwoBoneIkFailure.None;
        public bool ReachClamped =>
            Succeeded &&
            (ReachState == CharacterTwoBoneIkReachState.ClampedMinimum ||
             ReachState == CharacterTwoBoneIkReachState.ClampedMaximum);

        internal static CharacterTwoBoneIkResult Success(
            CharacterTwoBoneIkReachState reachState,
            Vector3 positionResidual,
            float targetDistance,
            float solveDistance,
            float upperLength,
            float lowerLength) =>
            new CharacterTwoBoneIkResult(
                true,
                CharacterTwoBoneIkFailure.None,
                reachState,
                positionResidual,
                targetDistance,
                solveDistance,
                upperLength,
                lowerLength);

        internal static CharacterTwoBoneIkResult Fail(CharacterTwoBoneIkFailure failure) =>
            new CharacterTwoBoneIkResult(true, failure, default, default, 0f, 0f, 0f, 0f);
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
