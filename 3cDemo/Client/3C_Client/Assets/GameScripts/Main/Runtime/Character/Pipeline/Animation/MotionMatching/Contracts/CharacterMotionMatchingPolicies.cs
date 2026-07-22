using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public enum MotionMatchingFootFeatureSource : byte
    {
        AnimationFootAnalysisArtifact = 1
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingFeatureSchema", menuName = "3C/Character/Motion Matching/Feature Schema")]
    public sealed class CharacterMotionMatchingFeatureSchema : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-feature-schema/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_FeatureSchemaId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] CharacterAnimationRigDefinition m_Rig;
        [SerializeField] MotionMatchingFeatureHorizon[] m_TrajectoryHorizons = Array.Empty<MotionMatchingFeatureHorizon>();
        [SerializeField] MotionMatchingBoneFeature[] m_BoneFeatures = Array.Empty<MotionMatchingBoneFeature>();
        [SerializeField] MotionMatchingFootFeatureSource m_FootFeatureSource;
        [SerializeField] MotionMatchingFeatureChannel m_InitializationFeatureMask;

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingFeatureSchemaId FeatureSchemaId => string.IsNullOrWhiteSpace(m_FeatureSchemaId) ? default : new CharacterMotionMatchingFeatureSchemaId(m_FeatureSchemaId);
        public int Revision => m_Revision;
        public CharacterAnimationRigDefinition Rig => m_Rig;
        public IReadOnlyList<MotionMatchingFeatureHorizon> TrajectoryHorizons => m_TrajectoryHorizons ?? Array.Empty<MotionMatchingFeatureHorizon>();
        public IReadOnlyList<MotionMatchingBoneFeature> BoneFeatures => m_BoneFeatures ?? Array.Empty<MotionMatchingBoneFeature>();
        public MotionMatchingFootFeatureSource FootFeatureSource => m_FootFeatureSource;
        public MotionMatchingFeatureChannel InitializationFeatureMask => m_InitializationFeatureMask;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !FeatureSchemaId.IsValid)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            if (!Rig)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' has no Rig.");
            Rig.RequireValid();
            if (TrajectoryHorizons.Count == 0)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' has no trajectory horizons.");
            bool hasZero = false;
            float previous = float.NegativeInfinity;
            for (int i = 0; i < TrajectoryHorizons.Count; i++)
            {
                MotionMatchingFeatureHorizon horizon = TrajectoryHorizons[i];
                if (horizon == null || !float.IsFinite(horizon.TimeOffset) || horizon.TimeOffset <= previous || horizon.Channels == MotionMatchingFeatureChannel.None)
                    throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' horizon #{i} is invalid or not strictly ordered.");
                hasZero |= horizon.TimeOffset == 0f;
                previous = horizon.TimeOffset;
            }
            if (!hasZero)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' must contain the zero-time horizon.");
            if (BoneFeatures.Count == 0)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' has no Bone features.");
            var boneIds = new HashSet<AnimationBoneId>();
            for (int i = 0; i < BoneFeatures.Count; i++)
            {
                MotionMatchingBoneFeature bone = BoneFeatures[i];
                if (bone == null || !bone.BoneId.IsValid || !boneIds.Add(bone.BoneId) || !bone.Position && !bone.Velocity)
                    throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' Bone feature #{i} is invalid or duplicated.");
                Rig.RequireBoneIndex(bone.BoneId);
            }
            if (FootFeatureSource != MotionMatchingFootFeatureSource.AnimationFootAnalysisArtifact)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' must use the formal Animation Foot Analysis Artifact.");
            if (InitializationFeatureMask == MotionMatchingFeatureChannel.None)
                throw new InvalidOperationException($"Motion Matching Feature Schema '{name}' has no Initialization Feature Mask.");
        }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingTrajectoryPolicy", menuName = "3C/Character/Motion Matching/Trajectory Policy")]
    public sealed class CharacterMotionMatchingTrajectoryPolicy : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-trajectory-policy/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] float m_MaximumAcceleration;
        [SerializeField] float m_MaximumTurnRateDegrees;
        [SerializeField] float m_SelectedAgePositionTolerancePerSecond;
        [SerializeField] float m_SelectedAgeFacingTolerancePerSecond;
        [SerializeField] float m_SelectedAgeConfidenceDecayPerSecond;
        [SerializeField] MotionMatchingTrajectoryPolicyPoint[] m_Points = Array.Empty<MotionMatchingTrajectoryPolicyPoint>();

        public string Schema => m_Schema ?? string.Empty;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public int Revision => m_Revision;
        public float MaximumAcceleration => m_MaximumAcceleration;
        public float MaximumTurnRateDegrees => m_MaximumTurnRateDegrees;
        public float SelectedAgePositionTolerancePerSecond => m_SelectedAgePositionTolerancePerSecond;
        public float SelectedAgeFacingTolerancePerSecond => m_SelectedAgeFacingTolerancePerSecond;
        public float SelectedAgeConfidenceDecayPerSecond => m_SelectedAgeConfidenceDecayPerSecond;
        public IReadOnlyList<MotionMatchingTrajectoryPolicyPoint> Points => m_Points ?? Array.Empty<MotionMatchingTrajectoryPolicyPoint>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal))
                throw new InvalidOperationException($"Motion Matching Trajectory Policy '{name}' has an invalid schema.");
            MotionMatchingAuthoringValidation.RequireIdentity(PolicyId, nameof(PolicyId));
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            MotionMatchingAuthoringValidation.RequireFinitePositive(MaximumAcceleration, nameof(MaximumAcceleration));
            MotionMatchingAuthoringValidation.RequireFinitePositive(MaximumTurnRateDegrees, nameof(MaximumTurnRateDegrees));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(SelectedAgePositionTolerancePerSecond, nameof(SelectedAgePositionTolerancePerSecond));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(SelectedAgeFacingTolerancePerSecond, nameof(SelectedAgeFacingTolerancePerSecond));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(SelectedAgeConfidenceDecayPerSecond, nameof(SelectedAgeConfidenceDecayPerSecond));
            if (Points.Count == 0)
                throw new InvalidOperationException($"Motion Matching Trajectory Policy '{name}' has no horizon points.");
            float previous = -1f;
            for (int i = 0; i < Points.Count; i++)
            {
                MotionMatchingTrajectoryPolicyPoint point = Points[i];
                if (point == null || !float.IsFinite(point.TimeOffset) || point.TimeOffset < 0f || point.TimeOffset <= previous)
                    throw new InvalidOperationException($"Motion Matching Trajectory Policy '{name}' point #{i} is invalid or not strictly ordered.");
                MotionMatchingAuthoringValidation.RequireFiniteNonNegative(point.AcceptedPositionTolerance, nameof(point.AcceptedPositionTolerance));
                MotionMatchingAuthoringValidation.RequireFiniteNonNegative(point.AcceptedFacingToleranceDegrees, nameof(point.AcceptedFacingToleranceDegrees));
                MotionMatchingAuthoringValidation.RequireFiniteNonNegative(point.SelectedPositionTolerance, nameof(point.SelectedPositionTolerance));
                MotionMatchingAuthoringValidation.RequireFiniteNonNegative(point.SelectedFacingToleranceDegrees, nameof(point.SelectedFacingToleranceDegrees));
                if (!float.IsFinite(point.AcceptedConfidence) || point.AcceptedConfidence < 0f || point.AcceptedConfidence > 1f ||
                    !float.IsFinite(point.SelectedConfidence) || point.SelectedConfidence < 0f || point.SelectedConfidence > 1f)
                    throw new InvalidOperationException($"Motion Matching Trajectory Policy '{name}' point #{i} has invalid confidence.");
                previous = point.TimeOffset;
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingCostProfile", menuName = "3C/Character/Motion Matching/Cost Profile")]
    public sealed class CharacterMotionMatchingCostProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-cost-profile/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_CostProfileId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] MotionMatchingCostWeightEntry[] m_Weights = Array.Empty<MotionMatchingCostWeightEntry>();

        public string Schema => m_Schema ?? string.Empty;
        public string CostProfileId => m_CostProfileId ?? string.Empty;
        public int Revision => m_Revision;
        public IReadOnlyList<MotionMatchingCostWeightEntry> Weights => m_Weights ?? Array.Empty<MotionMatchingCostWeightEntry>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal))
                throw new InvalidOperationException($"Motion Matching Cost Profile '{name}' has an invalid schema.");
            MotionMatchingAuthoringValidation.RequireIdentity(CostProfileId, nameof(CostProfileId));
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            int groupCount = Enum.GetValues(typeof(MotionMatchingCostGroup)).Length;
            if (Weights.Count != groupCount)
                throw new InvalidOperationException($"Motion Matching Cost Profile '{name}' must explicitly cover every cost group.");
            var groups = new HashSet<MotionMatchingCostGroup>();
            for (int i = 0; i < Weights.Count; i++)
            {
                MotionMatchingCostWeightEntry entry = Weights[i];
                if (entry == null || !Enum.IsDefined(typeof(MotionMatchingCostGroup), entry.Group) || !groups.Add(entry.Group) || !float.IsFinite(entry.Weight) || entry.Weight < 0f)
                    throw new InvalidOperationException($"Motion Matching Cost Profile '{name}' weight #{i} is invalid or duplicated.");
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingSearchPolicy", menuName = "3C/Character/Motion Matching/Search Policy")]
    public sealed class CharacterMotionMatchingSearchPolicy : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-search-policy/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_SearchPolicyId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] int m_TopK;
        [SerializeField] int m_LeafCapacity;
        [SerializeField] int m_PlanSampleCount;
        [SerializeField] float m_PlanSampleInterval;
        [SerializeField] float m_SearchInterval;
        [SerializeField] float m_MinimumJumpInterval;
        [SerializeField] int m_MaximumAdmittedSampleCount;
        [SerializeField] int m_MaximumTreeDepth;
        [SerializeField] float m_CoverageNearDuplicateCostThreshold;
        [SerializeField] int m_HistoryCapacity;
        [SerializeField] int m_DiagnosticDetailCapacity;
        [SerializeField] float m_ProtectedFootPositionJumpLimit;
        [SerializeField] float m_ProtectedFootVelocityJumpLimit;

        public string Schema => m_Schema ?? string.Empty;
        public string SearchPolicyId => m_SearchPolicyId ?? string.Empty;
        public int Revision => m_Revision;
        public int TopK => m_TopK;
        public int LeafCapacity => m_LeafCapacity;
        public int PlanSampleCount => m_PlanSampleCount;
        public float PlanSampleInterval => m_PlanSampleInterval;
        public float SearchInterval => m_SearchInterval;
        public float MinimumJumpInterval => m_MinimumJumpInterval;
        public int MaximumAdmittedSampleCount => m_MaximumAdmittedSampleCount;
        public int MaximumTreeDepth => m_MaximumTreeDepth;
        public float CoverageNearDuplicateCostThreshold => m_CoverageNearDuplicateCostThreshold;
        public int HistoryCapacity => m_HistoryCapacity;
        public int DiagnosticDetailCapacity => m_DiagnosticDetailCapacity;
        public float ProtectedFootPositionJumpLimit => m_ProtectedFootPositionJumpLimit;
        public float ProtectedFootVelocityJumpLimit => m_ProtectedFootVelocityJumpLimit;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal))
                throw new InvalidOperationException($"Motion Matching Search Policy '{name}' has an invalid schema.");
            MotionMatchingAuthoringValidation.RequireIdentity(SearchPolicyId, nameof(SearchPolicyId));
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            if (TopK <= 0 || LeafCapacity <= 0 || PlanSampleCount <= 0 || MaximumAdmittedSampleCount < TopK ||
                MaximumTreeDepth <= 0 || HistoryCapacity <= 0 || DiagnosticDetailCapacity < 0)
                throw new InvalidOperationException($"Motion Matching Search Policy '{name}' contains invalid fixed capacities.");
            MotionMatchingAuthoringValidation.RequireFinitePositive(PlanSampleInterval, nameof(PlanSampleInterval));
            MotionMatchingAuthoringValidation.RequireFinitePositive(SearchInterval, nameof(SearchInterval));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(MinimumJumpInterval, nameof(MinimumJumpInterval));
            MotionMatchingAuthoringValidation.RequireFinitePositive(CoverageNearDuplicateCostThreshold, nameof(CoverageNearDuplicateCostThreshold));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(ProtectedFootPositionJumpLimit, nameof(ProtectedFootPositionJumpLimit));
            MotionMatchingAuthoringValidation.RequireFiniteNonNegative(ProtectedFootVelocityJumpLimit, nameof(ProtectedFootVelocityJumpLimit));
        }
    }
}
