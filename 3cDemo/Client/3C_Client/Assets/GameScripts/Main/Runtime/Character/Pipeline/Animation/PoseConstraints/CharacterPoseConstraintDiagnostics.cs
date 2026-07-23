using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterVirtualBoneDiagnostic
    {
        public CharacterVirtualBoneDiagnostic(
            CharacterVirtualBoneDescriptor descriptor,
            AnimationLocalBonePose localPose,
            CharacterComponentBonePose componentPose,
            float maskContribution)
        {
            if (!descriptor.IsValid || !localPose.IsValid || !componentPose.IsValid ||
                !float.IsFinite(maskContribution) || maskContribution < 0f || maskContribution > 1f)
            {
                throw new ArgumentException("Virtual Bone diagnostic is invalid.");
            }
            VirtualBoneId = descriptor.VirtualBoneId;
            SourcePhysicalBoneIndex = descriptor.SourcePhysicalBoneIndex;
            TargetPhysicalBoneIndex = descriptor.TargetPhysicalBoneIndex;
            PoseBoneIndex = descriptor.PoseBoneIndex;
            LocalPose = localPose;
            ComponentPose = componentPose;
            MaskContribution = maskContribution;
        }

        public CharacterPoseBoneRuntimeId VirtualBoneId { get; }
        public int SourcePhysicalBoneIndex { get; }
        public int TargetPhysicalBoneIndex { get; }
        public int PoseBoneIndex { get; }
        public AnimationLocalBonePose LocalPose { get; }
        public CharacterComponentBonePose ComponentPose { get; }
        public float MaskContribution { get; }
    }

    public readonly struct CharacterTwoBoneIkDiagnostic
    {
        public CharacterTwoBoneIkDiagnostic(
            CharacterTwoBoneIkDescriptor descriptor,
            CharacterTwoBoneIkResult result,
            CharacterComponentBonePose inputEndPose,
            CharacterComponentBonePose outputEndPose)
        {
            if (!descriptor.IsValid || !result.Completed)
                throw new ArgumentException("Two Bone IK diagnostic is invalid.");
            ConstraintId = descriptor.ConstraintId;
            RootPhysicalBoneIndex = descriptor.RootPhysicalBoneIndex;
            JointPhysicalBoneIndex = descriptor.JointPhysicalBoneIndex;
            EndPhysicalBoneIndex = descriptor.EndPhysicalBoneIndex;
            EffectorPoseBoneIndex = descriptor.EffectorPoseBoneIndex;
            JointTargetPoseBoneIndex = descriptor.JointTargetPoseBoneIndex;
            Weight = descriptor.Weight;
            EndRotationMode = descriptor.EndRotationMode;
            Failure = result.Failure;
            ReachState = result.ReachState;
            PositionResidual = result.PositionResidual;
            TargetDistance = result.TargetDistance;
            SolveDistance = result.SolveDistance;
            UpperLength = result.UpperLength;
            LowerLength = result.LowerLength;
            InputEndPose = inputEndPose;
            OutputEndPose = outputEndPose;
            HasCompletedPoses = inputEndPose.IsValid && outputEndPose.IsValid;
        }

        public CharacterPoseConstraintId ConstraintId { get; }
        public int RootPhysicalBoneIndex { get; }
        public int JointPhysicalBoneIndex { get; }
        public int EndPhysicalBoneIndex { get; }
        public int EffectorPoseBoneIndex { get; }
        public int JointTargetPoseBoneIndex { get; }
        public float Weight { get; }
        public CharacterTwoBoneIkEndRotationMode EndRotationMode { get; }
        public CharacterTwoBoneIkFailure Failure { get; }
        public CharacterTwoBoneIkReachState ReachState { get; }
        public Vector3 PositionResidual { get; }
        public float TargetDistance { get; }
        public float SolveDistance { get; }
        public float UpperLength { get; }
        public float LowerLength { get; }
        public CharacterComponentBonePose InputEndPose { get; }
        public CharacterComponentBonePose OutputEndPose { get; }
        public bool HasCompletedPoses { get; }
    }

    public sealed class CharacterPoseConstraintDiagnosticsPage
    {
        readonly CharacterVirtualBoneDiagnostic[] m_VirtualBones;
        readonly CharacterTwoBoneIkDiagnostic[] m_TwoBoneIkConstraints;

        public CharacterPoseConstraintDiagnosticsPage(
            int virtualBoneCapacity,
            int twoBoneIkCapacity)
        {
            if (virtualBoneCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(virtualBoneCapacity));
            if (twoBoneIkCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(twoBoneIkCapacity));
            if (virtualBoneCapacity == 0 && twoBoneIkCapacity == 0)
                throw new ArgumentException("Pose constraint diagnostics page must have capacity.");
            m_VirtualBones = new CharacterVirtualBoneDiagnostic[virtualBoneCapacity];
            m_TwoBoneIkConstraints = new CharacterTwoBoneIkDiagnostic[twoBoneIkCapacity];
        }

        public int VirtualBoneCapacity => m_VirtualBones.Length;
        public int TwoBoneIkCapacity => m_TwoBoneIkConstraints.Length;
        public int VirtualBoneCount { get; private set; }
        public int TwoBoneIkCount { get; private set; }

        public CharacterVirtualBoneDiagnostic GetVirtualBone(int index) =>
            index >= 0 && index < VirtualBoneCount
                ? m_VirtualBones[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        public CharacterTwoBoneIkDiagnostic GetTwoBoneIk(int index) =>
            index >= 0 && index < TwoBoneIkCount
                ? m_TwoBoneIkConstraints[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        public bool TryAdd(CharacterVirtualBoneDiagnostic diagnostic)
        {
            if (VirtualBoneCount >= m_VirtualBones.Length)
                return false;
            m_VirtualBones[VirtualBoneCount++] = diagnostic;
            return true;
        }

        public bool TryAdd(CharacterTwoBoneIkDiagnostic diagnostic)
        {
            if (TwoBoneIkCount >= m_TwoBoneIkConstraints.Length)
                return false;
            m_TwoBoneIkConstraints[TwoBoneIkCount++] = diagnostic;
            return true;
        }

        public void Reset()
        {
            Array.Clear(m_VirtualBones, 0, VirtualBoneCount);
            Array.Clear(m_TwoBoneIkConstraints, 0, TwoBoneIkCount);
            VirtualBoneCount = 0;
            TwoBoneIkCount = 0;
        }
    }
}
