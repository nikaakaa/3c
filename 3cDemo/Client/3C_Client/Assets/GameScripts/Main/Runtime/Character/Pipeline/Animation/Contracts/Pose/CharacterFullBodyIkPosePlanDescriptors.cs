using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPresentationPoseBoneIkGoalBindingDescriptor
    {
        [SerializeField] CharacterFullBodyIkEffectorSlot m_EffectorSlot;
        [SerializeField] int m_TargetPoseBoneIndex = -1;
        [SerializeField] Vector3 m_PositionOffset;
        [SerializeField] Quaternion m_RotationOffset = Quaternion.identity;
        [SerializeField] float m_PositionWeight = 1f;
        [SerializeField] float m_RotationWeight = 1f;

        public CharacterPresentationPoseBoneIkGoalBindingDescriptor(
            CharacterFullBodyIkEffectorSlot effectorSlot,
            int targetPoseBoneIndex,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            float positionWeight,
            float rotationWeight)
        {
            m_EffectorSlot = effectorSlot;
            m_TargetPoseBoneIndex = targetPoseBoneIndex;
            m_PositionOffset = positionOffset;
            m_RotationOffset = rotationOffset.normalized;
            m_PositionWeight = positionWeight;
            m_RotationWeight = rotationWeight;
            RequireValid();
        }

        public CharacterFullBodyIkEffectorSlot EffectorSlot => m_EffectorSlot;
        public int TargetPoseBoneIndex => m_TargetPoseBoneIndex;
        public Vector3 PositionOffset => m_PositionOffset;
        public Quaternion RotationOffset => m_RotationOffset;
        public float PositionWeight => m_PositionWeight;
        public float RotationWeight => m_RotationWeight;

        public void RequireValid()
        {
            if (EffectorSlot < CharacterFullBodyIkEffectorSlot.Body ||
                EffectorSlot > CharacterFullBodyIkEffectorSlot.RightFoot ||
                TargetPoseBoneIndex < 0 ||
                !CharacterPoseConstraintMath.IsFinite(PositionOffset) ||
                !CharacterPoseConstraintMath.IsFinite(RotationOffset) ||
                !IsWeight(PositionWeight) ||
                !IsWeight(RotationWeight))
            {
                throw new InvalidOperationException("Pose Bone IK Goal binding descriptor is invalid.");
            }
        }

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseBoneIkGoalsDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_ContributionGoalWorkspaceOffset = -1;
        [SerializeField] CharacterPresentationPoseBoneIkGoalBindingDescriptor[] m_Bindings =
            Array.Empty<CharacterPresentationPoseBoneIkGoalBindingDescriptor>();

        public CharacterPresentationPoseBoneIkGoalsDescriptor(
            int index,
            PoseNodeId nodeId,
            int contributionGoalWorkspaceOffset,
            CharacterPresentationPoseBoneIkGoalBindingDescriptor[] bindings)
        {
            m_Index = index;
            m_NodeId = nodeId.IsValid ? nodeId.Value : string.Empty;
            m_ContributionGoalWorkspaceOffset = contributionGoalWorkspaceOffset;
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            RequireValid();
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int ContributionGoalWorkspaceOffset =>
            m_ContributionGoalWorkspaceOffset;
        public IReadOnlyList<CharacterPresentationPoseBoneIkGoalBindingDescriptor> Bindings =>
            m_Bindings ?? Array.Empty<CharacterPresentationPoseBoneIkGoalBindingDescriptor>();
        public int GoalCount => Bindings.Count;

        public void RequireValid()
        {
            if (Index < 0 || !NodeId.IsValid ||
                ContributionGoalWorkspaceOffset < 0 ||
                GoalCount <= 0 || GoalCount > CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)
            {
                throw new InvalidOperationException("Pose Bone IK Goals descriptor is invalid.");
            }
            ushort slots = 0;
            for (int i = 0; i < GoalCount; i++)
            {
                CharacterPresentationPoseBoneIkGoalBindingDescriptor binding = Bindings[i] ??
                    throw new InvalidOperationException("Pose Bone IK Goals contains a null binding.");
                binding.RequireValid();
                int bit = 1 << ((int)binding.EffectorSlot - 1);
                if ((slots & bit) != 0)
                    throw new InvalidOperationException("Pose Bone IK Goals contains a duplicate Effector Slot.");
                slots = (ushort)(slots | bit);
            }
        }
    }

    [Serializable]
    public sealed class CharacterPresentationFootPlacementDescriptor
    {
        public const int GoalCount = 3;

        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] CharacterFootPlacementProfile m_Profile;
        [SerializeField] CharacterFootPlacementRigCalibration m_Calibration;
        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] string m_CalibrationRevision = string.Empty;
        [SerializeField] int m_ContributionGoalWorkspaceOffset = -1;

        public CharacterPresentationFootPlacementDescriptor(
            int index,
            PoseNodeId nodeId,
            CharacterFootPlacementProfile profile,
            CharacterFootPlacementRigCalibration calibration,
            int contributionGoalWorkspaceOffset)
        {
            m_Index = index;
            m_NodeId = nodeId.IsValid ? nodeId.Value : string.Empty;
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Calibration = calibration ? calibration : throw new ArgumentNullException(nameof(calibration));
            m_CalibrationId = calibration.CalibrationId.Value;
            m_CalibrationRevision = calibration.ContentRevision;
            m_ContributionGoalWorkspaceOffset = contributionGoalWorkspaceOffset;
            RequireValid(calibration.RigId, calibration.RigRevision);
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public CharacterFootPlacementProfile Profile => m_Profile;
        public CharacterFootPlacementRigCalibration Calibration => m_Calibration;
        public string CalibrationId => m_CalibrationId ?? string.Empty;
        public string CalibrationRevision => m_CalibrationRevision ?? string.Empty;
        public int ContributionGoalWorkspaceOffset =>
            m_ContributionGoalWorkspaceOffset;

        public void RequireValid(string rigId, string rigRevision)
        {
            if (Index < 0 || !NodeId.IsValid ||
                ContributionGoalWorkspaceOffset < 0 || !Profile || !Calibration)
                throw new InvalidOperationException("Foot Placement descriptor is invalid.");
            Profile.RequireValid();
            Calibration.RequireValid();
            string computedProfileRevision = Profile.ComputeRevision();
            if (!string.Equals(Profile.Revision, computedProfileRevision, StringComparison.Ordinal) ||
                !string.Equals(Calibration.RigId, rigId, StringComparison.Ordinal) ||
                !string.Equals(Calibration.RigRevision, rigRevision, StringComparison.Ordinal) ||
                !string.Equals(CalibrationId, Calibration.CalibrationId.Value, StringComparison.Ordinal) ||
                !string.Equals(CalibrationRevision, Calibration.ContentRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Foot Placement descriptor is stale. " +
                    $"Profile={Profile.Revision}/{computedProfileRevision} " +
                    $"CalibrationRig={Calibration.RigId}/{Calibration.RigRevision} ExpectedRig={rigId}/{rigRevision} " +
                    $"Calibration={CalibrationId}/{CalibrationRevision} ExpectedCalibration={Calibration.CalibrationId.Value}/{Calibration.ContentRevision}.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterPresentationFullBodyIkDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] CharacterFullBodyIkProfile m_Profile;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_ProfileRevision = string.Empty;
        [SerializeField] string m_BackendIdentity = string.Empty;
        [SerializeField] string m_BackendSourceRevision = string.Empty;

        public CharacterPresentationFullBodyIkDescriptor(
            int index,
            PoseNodeId nodeId,
            CharacterFullBodyIkProfile profile)
        {
            m_Index = index;
            m_NodeId = nodeId.IsValid ? nodeId.Value : string.Empty;
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_ProfileId = profile.ProfileId;
            m_ProfileRevision = profile.Revision;
            m_BackendIdentity = CharacterFinalIkPoseBufferBackend.SourceIdentity;
            m_BackendSourceRevision = CharacterFinalIkPoseBufferBackend.AuditedVendorSourceRevision;
            RequireValid();
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public CharacterFullBodyIkProfile Profile => m_Profile;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public string ProfileRevision => m_ProfileRevision ?? string.Empty;
        public string BackendIdentity => m_BackendIdentity ?? string.Empty;
        public string BackendSourceRevision => m_BackendSourceRevision ?? string.Empty;

        public void RequireValid()
        {
            if (Index < 0 || !NodeId.IsValid || !Profile ||
                !string.Equals(ProfileId, Profile.ProfileId, StringComparison.Ordinal) ||
                !string.Equals(ProfileRevision, Profile.Revision, StringComparison.Ordinal) ||
                !string.Equals(BackendIdentity, CharacterFinalIkPoseBufferBackend.SourceIdentity, StringComparison.Ordinal) ||
                !string.Equals(BackendSourceRevision, CharacterFinalIkPoseBufferBackend.AuditedVendorSourceRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Full Body IK descriptor is invalid.");
            Profile.RequireValid();
        }
    }
}
