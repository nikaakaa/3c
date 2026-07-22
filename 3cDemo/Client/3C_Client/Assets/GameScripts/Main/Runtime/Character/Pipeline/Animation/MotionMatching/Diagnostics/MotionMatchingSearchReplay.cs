using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public sealed class MotionMatchingSearchReplayArtifact
    {
        readonly MotionMatchingTrajectoryEnvelopePoint[] m_TrajectoryPoints;
        readonly float[] m_NormalizedFeatures;

        public MotionMatchingSearchReplayArtifact(
            string projectionIdentity,
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            string searchPolicyId,
            int searchPolicyRevision,
            CharacterMotionMatchingQueryId queryId,
            CharacterMotionMatchingProfileId profileId,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            MotionMatchingTrajectorySourceIdentity trajectorySourceIdentity,
            SimulationTick trajectorySourceTick,
            ulong trajectorySourceSequence,
            float trajectorySourceAge,
            MotionMatchingTrajectoryEnvelopePoint[] trajectoryPoints,
            float[] normalizedFeatures,
            MotionMatchingContactProtection contactProtection,
            int currentSampleIndex,
            CharacterMotionMatchingPlanId currentPlanId,
            bool initialization,
            float secondsSinceLastJump,
            ulong resetSequence,
            StableHash expectedDigest)
        {
            ProjectionIdentity = MotionMatchingIdentity.Require(projectionIdentity, nameof(projectionIdentity));
            DatabaseIdentity = databaseIdentity ?? throw new ArgumentNullException(nameof(databaseIdentity));
            SearchPolicyId = MotionMatchingIdentity.Require(searchPolicyId, nameof(searchPolicyId));
            if (searchPolicyRevision <= 0 || !queryId.IsValid || !profileId.IsValid || !searchDomainId.IsValid ||
                !trajectorySourceIdentity.IsValid || !trajectorySourceTick.IsValid || trajectorySourceSequence == 0 ||
                !float.IsFinite(trajectorySourceAge) || trajectorySourceAge < 0f || trajectoryPoints == null || trajectoryPoints.Length == 0 ||
                normalizedFeatures == null || normalizedFeatures.Length == 0 || currentSampleIndex < -1 ||
                !float.IsFinite(secondsSinceLastJump) || secondsSinceLastJump < 0f || !expectedDigest.IsValid)
                throw new ArgumentException("Motion Matching Search Replay Artifact is incomplete.");
            m_TrajectoryPoints = (MotionMatchingTrajectoryEnvelopePoint[])trajectoryPoints.Clone();
            m_NormalizedFeatures = (float[])normalizedFeatures.Clone();
            for (int i = 0; i < m_NormalizedFeatures.Length; i++)
            {
                if (!float.IsFinite(m_NormalizedFeatures[i]))
                    throw new ArgumentException("Motion Matching Search Replay contains a non-finite query feature.", nameof(normalizedFeatures));
            }
            SearchPolicyRevision = searchPolicyRevision;
            QueryId = queryId;
            ProfileId = profileId;
            SearchDomainId = searchDomainId;
            TrajectorySourceIdentity = trajectorySourceIdentity;
            TrajectorySourceTick = trajectorySourceTick;
            TrajectorySourceSequence = trajectorySourceSequence;
            TrajectorySourceAge = trajectorySourceAge;
            ContactProtection = contactProtection;
            CurrentSampleIndex = currentSampleIndex;
            CurrentPlanId = currentPlanId;
            Initialization = initialization;
            SecondsSinceLastJump = secondsSinceLastJump;
            ResetSequence = resetSequence;
            ExpectedDigest = expectedDigest;
        }

        public string ProjectionIdentity { get; }
        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public string SearchPolicyId { get; }
        public int SearchPolicyRevision { get; }
        public CharacterMotionMatchingQueryId QueryId { get; }
        public CharacterMotionMatchingProfileId ProfileId { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public MotionMatchingTrajectorySourceIdentity TrajectorySourceIdentity { get; }
        public SimulationTick TrajectorySourceTick { get; }
        public ulong TrajectorySourceSequence { get; }
        public float TrajectorySourceAge { get; }
        public int TrajectoryPointCount => m_TrajectoryPoints.Length;
        public int NormalizedFeatureCount => m_NormalizedFeatures.Length;
        public MotionMatchingContactProtection ContactProtection { get; }
        public int CurrentSampleIndex { get; }
        public CharacterMotionMatchingPlanId CurrentPlanId { get; }
        public bool Initialization { get; }
        public float SecondsSinceLastJump { get; }
        public ulong ResetSequence { get; }
        public StableHash ExpectedDigest { get; }
        public MotionMatchingTrajectoryEnvelopePoint GetTrajectoryPoint(int index) => m_TrajectoryPoints[index];
        public float GetNormalizedFeature(int index) => m_NormalizedFeatures[index];

        public static MotionMatchingSearchReplayArtifact Capture(
            string projectionIdentity,
            CharacterMotionMatchingRuntimeDatabase database,
            MotionMatchingQuery query,
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult plan)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            var trajectory = new MotionMatchingTrajectoryEnvelopePoint[query.TrajectoryEnvelope.Count];
            for (int i = 0; i < trajectory.Length; i++)
                trajectory[i] = query.TrajectoryEnvelope[i];
            var features = new float[query.NormalizedFeatures.Count];
            for (int i = 0; i < features.Length; i++)
                features[i] = query.NormalizedFeatures[i];
            return new MotionMatchingSearchReplayArtifact(
                projectionIdentity,
                query.DatabaseIdentity,
                database.SearchPolicy.PolicyId,
                database.SearchPolicy.Revision,
                query.QueryId,
                query.ProfileId,
                query.SearchDomainId,
                query.TrajectorySourceIdentity,
                query.TrajectoryEnvelope.SourceTick,
                query.TrajectoryEnvelope.SourceSequence,
                query.TrajectoryEnvelope.SourceAge,
                trajectory,
                features,
                query.ContactProtection,
                query.CurrentSampleIndex,
                query.CurrentPlanId,
                query.Initialization,
                query.SecondsSinceLastJump,
                query.ResetSequence,
                MotionMatchingSearchDigest.Compute(database, search, plan));
        }
    }

    public enum MotionMatchingSearchReplayFailure : byte
    {
        None = 0,
        ProjectionIdentityMismatch = 1,
        DatabaseIdentityMismatch = 2,
        SearchPolicyMismatch = 3,
        QueryLayoutMismatch = 4,
        ResultDigestMismatch = 5
    }

    public readonly struct MotionMatchingSearchReplayResult
    {
        public MotionMatchingSearchReplayResult(MotionMatchingSearchReplayFailure failure, StableHash expectedDigest, StableHash actualDigest)
        {
            Failure = failure;
            ExpectedDigest = expectedDigest;
            ActualDigest = actualDigest;
        }

        public MotionMatchingSearchReplayFailure Failure { get; }
        public StableHash ExpectedDigest { get; }
        public StableHash ActualDigest { get; }
        public bool Matches => Failure == MotionMatchingSearchReplayFailure.None && ExpectedDigest.Equals(ActualDigest);
    }

    public sealed class MotionMatchingSearchReplayRunner
    {
        readonly string m_ProjectionIdentity;
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;
        readonly MotionMatchingExactSearch m_Search;
        readonly MotionMatchingPlanEvaluator m_Plan;
        readonly float[] m_Features;
        readonly MotionMatchingTrajectoryEnvelope m_Envelope;

        public MotionMatchingSearchReplayRunner(string projectionIdentity, CharacterMotionMatchingRuntimeDatabase database, int trajectoryCapacity)
        {
            m_ProjectionIdentity = MotionMatchingIdentity.Require(projectionIdentity, nameof(projectionIdentity));
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            m_Search = new MotionMatchingExactSearch(database);
            m_Plan = new MotionMatchingPlanEvaluator(database);
            m_Features = new float[database.Capacities.DenseFeatureCount];
            m_Envelope = new MotionMatchingTrajectoryEnvelope(trajectoryCapacity);
        }

        public MotionMatchingSearchReplayResult Replay(MotionMatchingSearchReplayArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (!string.Equals(artifact.ProjectionIdentity, m_ProjectionIdentity, StringComparison.Ordinal))
                return new MotionMatchingSearchReplayResult(MotionMatchingSearchReplayFailure.ProjectionIdentityMismatch, artifact.ExpectedDigest, default);
            if (!artifact.DatabaseIdentity.EqualsExact(m_Database.ArtifactIdentity))
                return new MotionMatchingSearchReplayResult(MotionMatchingSearchReplayFailure.DatabaseIdentityMismatch, artifact.ExpectedDigest, default);
            if (!string.Equals(artifact.SearchPolicyId, m_Database.SearchPolicy.PolicyId, StringComparison.Ordinal) || artifact.SearchPolicyRevision != m_Database.SearchPolicy.Revision)
                return new MotionMatchingSearchReplayResult(MotionMatchingSearchReplayFailure.SearchPolicyMismatch, artifact.ExpectedDigest, default);
            if (artifact.NormalizedFeatureCount != m_Features.Length || artifact.TrajectoryPointCount > m_Envelope.Capacity)
                return new MotionMatchingSearchReplayResult(MotionMatchingSearchReplayFailure.QueryLayoutMismatch, artifact.ExpectedDigest, default);
            for (int i = 0; i < m_Features.Length; i++)
                m_Features[i] = artifact.GetNormalizedFeature(i);
            m_Envelope.RestoreIdentity(
                artifact.TrajectorySourceIdentity,
                artifact.TrajectorySourceTick,
                artifact.TrajectorySourceSequence,
                artifact.TrajectorySourceAge,
                artifact.ResetSequence);
            for (int i = 0; i < artifact.TrajectoryPointCount; i++)
                m_Envelope.Add(artifact.GetTrajectoryPoint(i));
            var query = new MotionMatchingQuery(
                artifact.QueryId,
                artifact.ProfileId,
                artifact.DatabaseIdentity,
                artifact.SearchDomainId,
                artifact.TrajectorySourceIdentity,
                m_Envelope,
                new MotionMatchingFloatBuffer(m_Features, 0, m_Features.Length),
                artifact.ContactProtection,
                artifact.CurrentSampleIndex,
                artifact.CurrentPlanId,
                artifact.Initialization,
                artifact.SecondsSinceLastJump,
                artifact.ResetSequence);
            MotionMatchingSearchResult search = m_Search.Search(query);
            MotionMatchingPlanEvaluationResult plan = m_Plan.Evaluate(query, search);
            StableHash actual = MotionMatchingSearchDigest.Compute(m_Database, search, plan);
            return new MotionMatchingSearchReplayResult(
                actual.Equals(artifact.ExpectedDigest) ? MotionMatchingSearchReplayFailure.None : MotionMatchingSearchReplayFailure.ResultDigestMismatch,
                artifact.ExpectedDigest,
                actual);
        }
    }

    public static class MotionMatchingSearchDigest
    {
        public static StableHash Compute(
            CharacterMotionMatchingRuntimeDatabase database,
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult plan)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            var parts = new List<string>
            {
                "motion-matching-search-digest/v1",
                database.ArtifactIdentity.ContentHash.Value,
                search.TopKCount.ToString(CultureInfo.InvariantCulture),
                search.AdmittedCount.ToString(CultureInfo.InvariantCulture),
                search.RejectedCount.ToString(CultureInfo.InvariantCulture),
                search.NodesVisited.ToString(CultureInfo.InvariantCulture),
                search.NodesPruned.ToString(CultureInfo.InvariantCulture),
                search.ExactSampleCount.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < database.SampleCount; i++)
                parts.Add(((byte)search.GetRejectReason(i)).ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < search.TopKCount; i++)
            {
                MotionMatchingExactCandidate candidate = search.GetCandidate(i);
                parts.Add(candidate.SampleId.Value.ToString(CultureInfo.InvariantCulture));
                AppendCost(parts, candidate.Cost);
            }
            parts.Add(plan.IsValid ? "valid" : ((byte)plan.InvalidReason).ToString(CultureInfo.InvariantCulture));
            if (plan.IsValid)
            {
                parts.Add(plan.Plan.EntrySampleId.Value.ToString(CultureInfo.InvariantCulture));
                parts.Add(Bits(plan.Plan.TotalCost));
                parts.Add(plan.Plan.HorizonEndSampleId.Value.ToString(CultureInfo.InvariantCulture));
                parts.Add(Bits(plan.Plan.EntryVisualAdvanceRate));
            }
            return StableHash.Compute(parts.ToArray());
        }

        static void AppendCost(List<string> parts, MotionMatchingExactCostComponents cost)
        {
            parts.Add(Bits(cost.TrajectoryPosition));
            parts.Add(Bits(cost.TrajectoryFacing));
            parts.Add(Bits(cost.TrajectoryVelocity));
            parts.Add(Bits(cost.PosePosition));
            parts.Add(Bits(cost.PoseVelocity));
            parts.Add(Bits(cost.ContactSoft));
            parts.Add(Bits(cost.Continuation));
            parts.Add(Bits(cost.Jump));
        }

        static string Bits(float value) => unchecked((uint)BitConverter.SingleToInt32Bits(value)).ToString("x8", CultureInfo.InvariantCulture);
    }
}
