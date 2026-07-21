using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingContactProtection
    {
        public MotionMatchingContactProtection(
            MotionMatchingFootContactMask protectedMask,
            Vector3 leftRootPosition,
            Vector3 rightRootPosition,
            Vector3 leftRootVelocity,
            Vector3 rightRootVelocity)
        {
            if ((protectedMask & ~MotionMatchingFootContactMask.Both) != 0 ||
                !IsFinite(leftRootPosition) || !IsFinite(rightRootPosition) ||
                !IsFinite(leftRootVelocity) || !IsFinite(rightRootVelocity))
                throw new ArgumentException("Motion Matching contact protection is invalid.");
            ProtectedMask = protectedMask;
            LeftRootPosition = leftRootPosition;
            RightRootPosition = rightRootPosition;
            LeftRootVelocity = leftRootVelocity;
            RightRootVelocity = rightRootVelocity;
        }

        public MotionMatchingFootContactMask ProtectedMask { get; }
        public Vector3 LeftRootPosition { get; }
        public Vector3 RightRootPosition { get; }
        public Vector3 LeftRootVelocity { get; }
        public Vector3 RightRootVelocity { get; }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public readonly struct MotionMatchingQuery
    {
        public MotionMatchingQuery(
            CharacterMotionMatchingQueryId queryId,
            CharacterMotionMatchingProfileId profileId,
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            MotionMatchingTrajectorySourceIdentity trajectorySourceIdentity,
            MotionMatchingTrajectoryEnvelope trajectoryEnvelope,
            MotionMatchingFloatBuffer normalizedFeatures,
            MotionMatchingContactProtection contactProtection,
            int currentSampleIndex,
            CharacterMotionMatchingPlanId currentPlanId,
            bool initialization,
            float secondsSinceLastJump,
            ulong resetSequence)
        {
            if (!queryId.IsValid || !profileId.IsValid || databaseIdentity == null || !searchDomainId.IsValid ||
                !trajectorySourceIdentity.IsValid || trajectoryEnvelope == null || trajectoryEnvelope.Count == 0 ||
                normalizedFeatures.Count == 0 || currentSampleIndex < -1 ||
                !float.IsFinite(secondsSinceLastJump) || secondsSinceLastJump < 0f)
                throw new ArgumentException("Motion Matching Query is incomplete.");
            if (!initialization && currentSampleIndex < 0)
                throw new ArgumentException("Non-initialization Motion Matching Query has no current sample.");
            QueryId = queryId;
            ProfileId = profileId;
            DatabaseIdentity = databaseIdentity;
            SearchDomainId = searchDomainId;
            TrajectorySourceIdentity = trajectorySourceIdentity;
            TrajectoryEnvelope = trajectoryEnvelope;
            NormalizedFeatures = normalizedFeatures;
            ContactProtection = contactProtection;
            CurrentSampleIndex = currentSampleIndex;
            CurrentPlanId = currentPlanId;
            Initialization = initialization;
            SecondsSinceLastJump = secondsSinceLastJump;
            ResetSequence = resetSequence;
        }

        public CharacterMotionMatchingQueryId QueryId { get; }
        public CharacterMotionMatchingProfileId ProfileId { get; }
        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public MotionMatchingTrajectorySourceIdentity TrajectorySourceIdentity { get; }
        public MotionMatchingTrajectoryEnvelope TrajectoryEnvelope { get; }
        public MotionMatchingFloatBuffer NormalizedFeatures { get; }
        public MotionMatchingContactProtection ContactProtection { get; }
        public int CurrentSampleIndex { get; }
        public CharacterMotionMatchingPlanId CurrentPlanId { get; }
        public bool Initialization { get; }
        public float SecondsSinceLastJump { get; }
        public ulong ResetSequence { get; }
    }

    public sealed class MotionMatchingQueryBuilder
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;
        readonly float[] m_RawFeatures;

        public MotionMatchingQueryBuilder(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            m_RawFeatures = new float[database.Capacities.DenseFeatureCount];
        }

        public MotionMatchingQuery Build(
            CharacterMotionMatchingQueryId queryId,
            CharacterMotionMatchingProfileId profileId,
            MotionMatchingTrajectoryEnvelope envelope,
            CharacterMotionMatchingPoseHistory history,
            MotionMatchingContactProtection contactProtection,
            int currentSampleIndex,
            CharacterMotionMatchingPlanId currentPlanId,
            float secondsSinceLastJump,
            ulong resetSequence)
        {
            if (envelope == null || history == null)
                throw new ArgumentNullException(envelope == null ? nameof(envelope) : nameof(history));
            bool initialization = history.Count == 0 || history.HasGap || history.ResetSequence != resetSequence;
            if (!initialization)
            {
                float requiredHistory = 0f;
                for (int i = 0; i < m_Database.FeatureSchema.HistoryHorizonCount; i++)
                    requiredHistory = Mathf.Max(requiredHistory, -m_Database.FeatureSchema.GetHistoryHorizon(i));
                initialization = !history.CoversSecondsBeforeLatest(requiredHistory);
            }
            Array.Clear(m_RawFeatures, 0, m_RawFeatures.Length);
            for (int rangeIndex = 0; rangeIndex < m_Database.FeatureSchema.FeatureRangeCount; rangeIndex++)
            {
                MotionMatchingFeatureRange range = m_Database.FeatureSchema.GetFeatureRange(rangeIndex);
                switch (range.Group)
                {
                    case MotionMatchingCostGroup.TrajectoryPosition:
                        FillTrajectoryPosition(range, envelope);
                        break;
                    case MotionMatchingCostGroup.TrajectoryFacing:
                        FillTrajectoryFacing(range, envelope);
                        break;
                    case MotionMatchingCostGroup.TrajectoryVelocity:
                        FillTrajectoryVelocity(range, envelope);
                        break;
                    case MotionMatchingCostGroup.PosePosition:
                        FillPose(range, history, false, initialization);
                        break;
                    case MotionMatchingCostGroup.PoseVelocity:
                        FillPose(range, history, true, initialization);
                        break;
                    case MotionMatchingCostGroup.ContactSoft:
                        FillContact(range, history, initialization);
                        break;
                    default:
                        throw new InvalidOperationException($"Cost group '{range.Group}' cannot own dense query features.");
                }
            }
            MotionMatchingFloatBuffer normalized = m_Database.NormalizeQuery(
                new MotionMatchingFloatBuffer(m_RawFeatures, 0, m_RawFeatures.Length),
                initialization);
            return new MotionMatchingQuery(
                queryId,
                profileId,
                m_Database.ArtifactIdentity,
                m_Database.SearchDomainId,
                envelope.SourceIdentity,
                envelope,
                normalized,
                contactProtection,
                initialization ? -1 : currentSampleIndex,
                initialization ? default : currentPlanId,
                initialization,
                secondsSinceLastJump,
                resetSequence);
        }

        void FillTrajectoryPosition(MotionMatchingFeatureRange range, MotionMatchingTrajectoryEnvelope envelope)
        {
            RequireRangeCount(range, envelope.Count * 2);
            for (int i = 0; i < envelope.Count; i++)
            {
                m_RawFeatures[range.Offset + i * 2] = envelope[i].LocalPositionCenter.x;
                m_RawFeatures[range.Offset + i * 2 + 1] = envelope[i].LocalPositionCenter.y;
            }
        }

        void FillTrajectoryFacing(MotionMatchingFeatureRange range, MotionMatchingTrajectoryEnvelope envelope)
        {
            RequireRangeCount(range, envelope.Count * 2);
            for (int i = 0; i < envelope.Count; i++)
            {
                m_RawFeatures[range.Offset + i * 2] = envelope[i].LocalFacingCenter.x;
                m_RawFeatures[range.Offset + i * 2 + 1] = envelope[i].LocalFacingCenter.y;
            }
        }

        void FillTrajectoryVelocity(MotionMatchingFeatureRange range, MotionMatchingTrajectoryEnvelope envelope)
        {
            RequireRangeCount(range, envelope.Count * 2);
            for (int i = 0; i < envelope.Count; i++)
            {
                float time = envelope[i].TimeOffset;
                Vector2 velocity = time <= 0f ? Vector2.zero : envelope[i].LocalPositionCenter / time;
                m_RawFeatures[range.Offset + i * 2] = velocity.x;
                m_RawFeatures[range.Offset + i * 2 + 1] = velocity.y;
            }
        }

        void FillPose(MotionMatchingFeatureRange range, CharacterMotionMatchingPoseHistory history, bool velocity, bool initialization)
        {
            int expected = m_Database.FeatureSchema.BoneCount * m_Database.FeatureSchema.HistoryHorizonCount * 3;
            RequireRangeCount(range, expected);
            if (initialization)
                return;
            int cursor = range.Offset;
            for (int horizonIndex = 0; horizonIndex < m_Database.FeatureSchema.HistoryHorizonCount; horizonIndex++)
            {
                float horizon = m_Database.FeatureSchema.GetHistoryHorizon(horizonIndex);
                float secondsBeforeLatest = horizon < 0f ? -horizon : 0f;
                for (int boneIndex = 0; boneIndex < m_Database.FeatureSchema.BoneCount; boneIndex++)
                {
                    if (!history.TrySampleBone(secondsBeforeLatest, boneIndex, out Vector3 position, out Vector3 boneVelocity))
                        throw new InvalidOperationException("Motion Matching Pose History does not cover a required query horizon.");
                    Vector3 value = velocity ? boneVelocity : position;
                    m_RawFeatures[cursor++] = value.x;
                    m_RawFeatures[cursor++] = value.y;
                    m_RawFeatures[cursor++] = value.z;
                }
            }
        }

        void FillContact(MotionMatchingFeatureRange range, CharacterMotionMatchingPoseHistory history, bool initialization)
        {
            RequireRangeCount(range, 8);
            if (initialization)
                return;
            AnimationFootPlacementSample sample = history.LatestFootPlacement;
            m_RawFeatures[range.Offset] = sample.Left.PlantConfidence;
            m_RawFeatures[range.Offset + 1] = sample.Left.SoleHeight;
            m_RawFeatures[range.Offset + 2] = sample.Left.SoleLocalVelocity.magnitude;
            m_RawFeatures[range.Offset + 3] = sample.Left.NextLandingConfidence;
            m_RawFeatures[range.Offset + 4] = sample.Right.PlantConfidence;
            m_RawFeatures[range.Offset + 5] = sample.Right.SoleHeight;
            m_RawFeatures[range.Offset + 6] = sample.Right.SoleLocalVelocity.magnitude;
            m_RawFeatures[range.Offset + 7] = sample.Right.NextLandingConfidence;
        }

        static void RequireRangeCount(MotionMatchingFeatureRange range, int expected)
        {
            if (range.Count != expected)
                throw new InvalidOperationException($"Motion Matching dense feature range '{range.Group}' expects {expected} values but contains {range.Count}.");
        }
    }
}
