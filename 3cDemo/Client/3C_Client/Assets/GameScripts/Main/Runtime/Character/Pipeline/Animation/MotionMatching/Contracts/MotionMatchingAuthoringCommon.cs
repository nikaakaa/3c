using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public enum MotionMatchingSamplingCompatibilityMode : byte
    {
        HumanoidRetargeted = 1,
        ExactGenericRig = 2
    }

    [Flags]
    public enum MotionMatchingFeatureChannel : ushort
    {
        None = 0,
        TrajectoryPosition = 1 << 0,
        TrajectoryFacing = 1 << 1,
        TrajectoryVelocity = 1 << 2,
        TrajectoryAngularVelocity = 1 << 3,
        PosePosition = 1 << 4,
        PoseVelocity = 1 << 5,
        LeftFoot = 1 << 6,
        RightFoot = 1 << 7
    }

    [Flags]
    public enum MotionMatchingFootContactMask : byte
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = Left | Right
    }

    public enum MotionMatchingSegmentLoopMode : byte
    {
        Loop = 1,
        Finite = 2
    }

    public enum MotionMatchingCostGroup : byte
    {
        TrajectoryPosition = 1,
        TrajectoryFacing = 2,
        TrajectoryVelocity = 3,
        PosePosition = 4,
        PoseVelocity = 5,
        ContactSoft = 6,
        Continuation = 7,
        Jump = 8,
        PlanTrajectoryPosition = 9,
        PlanTrajectoryFacing = 10,
        PlanContact = 11,
        PlanSegmentEnd = 12,
        PlanVelocityChange = 13
    }

    [Serializable]
    public sealed class MotionMatchingFeatureHorizon
    {
        [SerializeField] float m_TimeOffset;
        [SerializeField] MotionMatchingFeatureChannel m_Channels;

        public float TimeOffset => m_TimeOffset;
        public MotionMatchingFeatureChannel Channels => m_Channels;
    }

    [Serializable]
    public sealed class MotionMatchingBoneFeature
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] bool m_Position;
        [SerializeField] bool m_Velocity;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public bool Position => m_Position;
        public bool Velocity => m_Velocity;
    }

    [Serializable]
    public sealed class MotionMatchingTrajectoryPolicyPoint
    {
        [SerializeField] float m_TimeOffset;
        [SerializeField] float m_AcceptedPositionTolerance;
        [SerializeField] float m_AcceptedFacingToleranceDegrees;
        [SerializeField] float m_AcceptedConfidence;
        [SerializeField] float m_SelectedPositionTolerance;
        [SerializeField] float m_SelectedFacingToleranceDegrees;
        [SerializeField] float m_SelectedConfidence;

        public float TimeOffset => m_TimeOffset;
        public float AcceptedPositionTolerance => m_AcceptedPositionTolerance;
        public float AcceptedFacingToleranceDegrees => m_AcceptedFacingToleranceDegrees;
        public float AcceptedConfidence => m_AcceptedConfidence;
        public float SelectedPositionTolerance => m_SelectedPositionTolerance;
        public float SelectedFacingToleranceDegrees => m_SelectedFacingToleranceDegrees;
        public float SelectedConfidence => m_SelectedConfidence;
    }

    [Serializable]
    public sealed class MotionMatchingCostWeightEntry
    {
        [SerializeField] MotionMatchingCostGroup m_Group;
        [SerializeField] float m_Weight;

        public MotionMatchingCostGroup Group => m_Group;
        public float Weight => m_Weight;
    }

    [Serializable]
    public sealed class CharacterMotionMatchingProducerBinding
    {
        [SerializeField] string m_ProgramProducerId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] string m_PoseSlotId = string.Empty;
        [SerializeField] string m_SearchDomainId = string.Empty;
        [SerializeField] CharacterMotionMatchingDatabaseDefinition[] m_Databases = Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public string ProgramProducerId => m_ProgramProducerId ?? string.Empty;
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId) ? default : new AnimationChannelId(m_AnimationChannelId);
        public PoseSlotId PoseSlotId => string.IsNullOrWhiteSpace(m_PoseSlotId) ? default : new PoseSlotId(m_PoseSlotId);
        public CharacterMotionMatchingSearchDomainId SearchDomainId => string.IsNullOrWhiteSpace(m_SearchDomainId) ? default : new CharacterMotionMatchingSearchDomainId(m_SearchDomainId);
        public IReadOnlyList<CharacterMotionMatchingDatabaseDefinition> Databases => m_Databases ?? Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public void RequireValid()
        {
            MotionMatchingAuthoringValidation.RequireIdentity(ProgramProducerId, nameof(ProgramProducerId));
            if (!AnimationChannelId.IsValid || !PoseSlotId.IsValid || !SearchDomainId.IsValid || Databases.Count == 0)
                throw new InvalidOperationException("Motion Matching producer binding is incomplete.");
            var databaseIds = new HashSet<CharacterMotionMatchingDatabaseId>();
            for (int i = 0; i < Databases.Count; i++)
            {
                CharacterMotionMatchingDatabaseDefinition database = Databases[i];
                if (!database)
                    throw new InvalidOperationException($"Motion Matching producer binding database #{i} is missing.");
                database.RequireValid();
                if (!database.SearchDomainId.Equals(SearchDomainId) || !databaseIds.Add(database.DatabaseId))
                    throw new InvalidOperationException($"Motion Matching producer binding database #{i} has a mismatched or duplicate identity.");
            }
        }
    }

    static class MotionMatchingAuthoringValidation
    {
        public static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Motion Matching field '{field}' is not a canonical identity.");
            return value;
        }

        public static int RequireRevision(int value, string field)
        {
            if (value <= 0)
                throw new InvalidOperationException($"Motion Matching field '{field}' must be positive.");
            return value;
        }

        public static float RequireFinite(float value, string field)
        {
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"Motion Matching field '{field}' is non-finite.");
            return value;
        }

        public static float RequireFinitePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"Motion Matching field '{field}' must be finite and positive.");
            return value;
        }

        public static float RequireFiniteNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"Motion Matching field '{field}' must be finite and non-negative.");
            return value;
        }

        public static bool IsAssetGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    return false;
            }
            return true;
        }
    }
}
