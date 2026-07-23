using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            MotionMatchingSelectionIdentity currentSelection,
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
                normalizedFeatures == null || normalizedFeatures.Length == 0 ||
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
            CurrentSelection = currentSelection;
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
        public MotionMatchingSelectionIdentity CurrentSelection { get; }
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
                query.CurrentSelection,
                query.Initialization,
                query.SecondsSinceLastJump,
                query.ResetSequence,
                MotionMatchingSearchDigest.Compute(database, search, plan));
        }
    }

    public static class MotionMatchingSearchReplayArtifactCodec
    {
        const int Magic = 0x4d4d5352;
        const int Version = 1;

        public static byte[] Encode(MotionMatchingSearchReplayArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(artifact.ProjectionIdentity);
            WriteDatabaseIdentity(writer, artifact.DatabaseIdentity);
            writer.Write(artifact.SearchPolicyId);
            writer.Write(artifact.SearchPolicyRevision);
            writer.Write(artifact.QueryId.Value);
            writer.Write(artifact.ProfileId.Value);
            writer.Write(artifact.SearchDomainId.Value);
            writer.Write(artifact.TrajectorySourceIdentity.Value);
            writer.Write(artifact.TrajectorySourceTick.Value);
            writer.Write(artifact.TrajectorySourceSequence);
            writer.Write(artifact.TrajectorySourceAge);
            writer.Write(artifact.TrajectoryPointCount);
            for (int i = 0; i < artifact.TrajectoryPointCount; i++)
            {
                MotionMatchingTrajectoryEnvelopePoint point = artifact.GetTrajectoryPoint(i);
                writer.Write(point.TimeOffset);
                WriteVector2(writer, point.LocalPositionCenter);
                WriteVector2(writer, point.LocalFacingCenter);
                writer.Write(point.PositionToleranceRadius);
                writer.Write(point.FacingToleranceDegrees);
                writer.Write(point.Confidence);
            }
            writer.Write(artifact.NormalizedFeatureCount);
            for (int i = 0; i < artifact.NormalizedFeatureCount; i++)
                writer.Write(artifact.GetNormalizedFeature(i));
            WriteContactProtection(writer, artifact.ContactProtection);
            writer.Write(artifact.CurrentSelection.IsValid);
            if (artifact.CurrentSelection.IsValid)
                WriteSelectionIdentity(writer, artifact.CurrentSelection);
            writer.Write(artifact.Initialization);
            writer.Write(artifact.SecondsSinceLastJump);
            writer.Write(artifact.ResetSequence);
            writer.Write(artifact.ExpectedDigest.Value);
            writer.Flush();
            return stream.ToArray();
        }

        public static MotionMatchingSearchReplayArtifact Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Motion Matching Search Replay bytes are empty.", nameof(bytes));
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Motion Matching Search Replay schema is unsupported.");
            string projectionIdentity = reader.ReadString();
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity = ReadDatabaseIdentity(reader);
            string searchPolicyId = reader.ReadString();
            int searchPolicyRevision = reader.ReadInt32();
            var queryId = new CharacterMotionMatchingQueryId(reader.ReadUInt64());
            var profileId = new CharacterMotionMatchingProfileId(reader.ReadString());
            var searchDomainId = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
            var trajectorySourceIdentity = new MotionMatchingTrajectorySourceIdentity(reader.ReadString());
            var trajectorySourceTick = new SimulationTick(reader.ReadUInt64());
            ulong trajectorySourceSequence = reader.ReadUInt64();
            float trajectorySourceAge = reader.ReadSingle();
            int trajectoryCount = RequireCount(reader.ReadInt32(), "trajectory");
            var trajectory = new MotionMatchingTrajectoryEnvelopePoint[trajectoryCount];
            for (int i = 0; i < trajectory.Length; i++)
            {
                trajectory[i] = new MotionMatchingTrajectoryEnvelopePoint(
                    reader.ReadSingle(),
                    ReadVector2(reader),
                    ReadVector2(reader),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }
            int featureCount = RequireCount(reader.ReadInt32(), "feature");
            var features = new float[featureCount];
            for (int i = 0; i < features.Length; i++)
                features[i] = reader.ReadSingle();
            MotionMatchingContactProtection contact = ReadContactProtection(reader);
            MotionMatchingSelectionIdentity currentSelection = reader.ReadBoolean()
                ? ReadSelectionIdentity(reader)
                : default;
            bool initialization = reader.ReadBoolean();
            float secondsSinceLastJump = reader.ReadSingle();
            ulong resetSequence = reader.ReadUInt64();
            var expectedDigest = new StableHash(reader.ReadString());
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Motion Matching Search Replay contains trailing data.");
            return new MotionMatchingSearchReplayArtifact(
                projectionIdentity,
                databaseIdentity,
                searchPolicyId,
                searchPolicyRevision,
                queryId,
                profileId,
                searchDomainId,
                trajectorySourceIdentity,
                trajectorySourceTick,
                trajectorySourceSequence,
                trajectorySourceAge,
                trajectory,
                features,
                contact,
                currentSelection,
                initialization,
                secondsSinceLastJump,
                resetSequence,
                expectedDigest);
        }

        static void WriteDatabaseIdentity(BinaryWriter writer, CharacterMotionMatchingDatabaseArtifactIdentity identity)
        {
            writer.Write(identity.ArtifactSchemaVersion);
            writer.Write(identity.AnalysisAlgorithmVersion);
            writer.Write(identity.DatabaseId.Value);
            writer.Write(identity.DatabaseRevision);
            writer.Write(identity.FeatureSchemaId.Value);
            writer.Write(identity.FeatureSchemaRevision);
            writer.Write(identity.RigId);
            writer.Write(identity.RigRevision);
            writer.Write(identity.ClipDependencyCount);
            for (int i = 0; i < identity.ClipDependencyCount; i++)
            {
                MotionMatchingClipDependencyIdentity clip = identity.GetClipDependency(i);
                writer.Write(clip.SourceSetId.Value);
                writer.Write(clip.SourceSetRevision);
                writer.Write(clip.SourceClipId.Value);
                writer.Write(clip.AssetGuid);
                writer.Write(clip.LocalFileId);
                writer.Write(clip.ImportDependencyHash);
                writer.Write(clip.SamplingRigSignature);
                writer.Write(clip.MotionRootBoneId.Value);
                writer.Write(clip.FootArtifactHash.Value);
            }
            writer.Write(identity.AnalysisInputHash.Value);
            writer.Write(identity.OrderedClipDependencyHash.Value);
            writer.Write(identity.ContentHash.Value);
        }

        static CharacterMotionMatchingDatabaseArtifactIdentity ReadDatabaseIdentity(BinaryReader reader)
        {
            int artifactSchemaVersion = reader.ReadInt32();
            string algorithmVersion = reader.ReadString();
            var databaseId = new CharacterMotionMatchingDatabaseId(reader.ReadString());
            int databaseRevision = reader.ReadInt32();
            var schemaId = new CharacterMotionMatchingFeatureSchemaId(reader.ReadString());
            int schemaRevision = reader.ReadInt32();
            string rigId = reader.ReadString();
            string rigRevision = reader.ReadString();
            int clipCount = RequireCount(reader.ReadInt32(), "clip dependency");
            var clips = new MotionMatchingClipDependencyIdentity[clipCount];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = new MotionMatchingClipDependencyIdentity(
                    new CharacterMotionMatchingSourceSetId(reader.ReadString()),
                    reader.ReadInt32(),
                    new CharacterMotionMatchingSourceClipId(reader.ReadString()),
                    reader.ReadString(),
                    reader.ReadInt64(),
                    reader.ReadString(),
                    reader.ReadString(),
                    new AnimationBoneId(reader.ReadString()),
                    new StableHash(reader.ReadString()));
            }
            return new CharacterMotionMatchingDatabaseArtifactIdentity(
                artifactSchemaVersion,
                algorithmVersion,
                databaseId,
                databaseRevision,
                schemaId,
                schemaRevision,
                rigId,
                rigRevision,
                clips,
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()));
        }

        static void WriteSelectionIdentity(BinaryWriter writer, MotionMatchingSelectionIdentity selection)
        {
            WriteDatabaseIdentity(writer, selection.DatabaseIdentity);
            writer.Write(selection.Generation.Value);
            writer.Write(selection.PlanId.Value);
            writer.Write(selection.SampleId.Value);
            writer.Write(selection.SampleIndex);
        }

        static MotionMatchingSelectionIdentity ReadSelectionIdentity(BinaryReader reader) =>
            new MotionMatchingSelectionIdentity(
                ReadDatabaseIdentity(reader),
                new MotionMatchingSelectionGeneration(reader.ReadUInt64()),
                new CharacterMotionMatchingPlanId(reader.ReadUInt64()),
                new CharacterMotionMatchingSampleId(reader.ReadUInt32()),
                reader.ReadInt32());

        static void WriteContactProtection(BinaryWriter writer, MotionMatchingContactProtection contact)
        {
            writer.Write((byte)contact.ProtectedMask);
            WriteVector3(writer, contact.LeftRootPosition);
            WriteVector3(writer, contact.RightRootPosition);
            WriteVector3(writer, contact.LeftRootVelocity);
            WriteVector3(writer, contact.RightRootVelocity);
        }

        static MotionMatchingContactProtection ReadContactProtection(BinaryReader reader) =>
            new MotionMatchingContactProtection(
                (MotionMatchingFootContactMask)reader.ReadByte(),
                ReadVector3(reader),
                ReadVector3(reader),
                ReadVector3(reader),
                ReadVector3(reader));

        static void WriteVector2(BinaryWriter writer, UnityEngine.Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        static UnityEngine.Vector2 ReadVector2(BinaryReader reader) =>
            new UnityEngine.Vector2(reader.ReadSingle(), reader.ReadSingle());

        static void WriteVector3(BinaryWriter writer, UnityEngine.Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        static UnityEngine.Vector3 ReadVector3(BinaryReader reader) =>
            new UnityEngine.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        static int RequireCount(int count, string name)
        {
            if (count <= 0 || count > 1_000_000)
                throw new InvalidDataException($"Motion Matching Search Replay {name} count is invalid.");
            return count;
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
                artifact.CurrentSelection,
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
                "motion-matching-search-digest/v3",
                database.ArtifactIdentity.ContentHash.Value,
                search.TopKCount.ToString(CultureInfo.InvariantCulture),
                search.AdmittedCount.ToString(CultureInfo.InvariantCulture),
                search.RejectedCount.ToString(CultureInfo.InvariantCulture),
                search.NodesVisited.ToString(CultureInfo.InvariantCulture),
                search.NodesPruned.ToString(CultureInfo.InvariantCulture),
                search.ExactSampleCount.ToString(CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < database.SampleCount; i++)
            {
                MotionMatchingCandidateRejectDetail detail = search.GetRejectDetail(i);
                parts.Add(((byte)detail.Reason).ToString(CultureInfo.InvariantCulture));
                parts.Add(Bits(detail.Value));
                parts.Add(Bits(detail.Limit));
                parts.Add(Bits(detail.SecondaryValue));
                parts.Add(Bits(detail.SecondaryLimit));
            }
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
                parts.Add(Bits(plan.Plan.NextMandatorySearchTime));
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
