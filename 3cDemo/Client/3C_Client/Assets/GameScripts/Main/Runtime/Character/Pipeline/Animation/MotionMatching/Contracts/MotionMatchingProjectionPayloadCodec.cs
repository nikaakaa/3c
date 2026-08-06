using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public static class MotionMatchingProjectionPayloadCodec
    {
        const int SchemaVersion = 5;

        public static byte[] Encode(MotionMatchingProjectionPayload payload, out AnimationClip[] clips)
        {
            if (payload == null)
            {
                clips = Array.Empty<AnimationClip>();
                return Array.Empty<byte>();
            }
            var clipTable = new List<AnimationClip>();
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(SchemaVersion);
            writer.Write(payload.ProfileId.Value);
            writer.Write(payload.ProfileRevision);
            WriteFeatureSchema(writer, payload.FeatureSchema);
            WriteTrajectoryPolicy(writer, payload.TrajectoryPolicy);
            WriteCostProfile(writer, payload.CostProfile);
            WriteSearchPolicy(writer, payload.SearchPolicy);
            writer.Write(payload.DatabaseCount);
            for (int i = 0; i < payload.DatabaseCount; i++)
                WriteDatabase(writer, payload.GetDatabase(i), clipTable);
            writer.Write(payload.NodeBindingCount);
            for (int i = 0; i < payload.NodeBindingCount; i++)
                WriteNodeBinding(writer, payload.GetNodeBinding(i));
            clips = clipTable.ToArray();
            return stream.ToArray();
        }

        public static MotionMatchingProjectionPayload Decode(byte[] bytes, AnimationClip[] clips)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version != SchemaVersion)
                throw new InvalidOperationException($"Motion Matching Projection payload schema '{version}' is unsupported.");
            var profileId = new CharacterMotionMatchingProfileId(reader.ReadString());
            int profileRevision = reader.ReadInt32();
            MotionMatchingFeatureSchemaPayload featureSchema = ReadFeatureSchema(reader);
            MotionMatchingTrajectoryPolicyPayload trajectoryPolicy = ReadTrajectoryPolicy(reader);
            MotionMatchingCostProfilePayload costProfile = ReadCostProfile(reader);
            MotionMatchingSearchPolicyPayload searchPolicy = ReadSearchPolicy(reader);
            int databaseCount = RequireCount(reader.ReadInt32(), "Database", false);
            var databases = new MotionMatchingDatabasePayload[databaseCount];
            for (int i = 0; i < databaseCount; i++)
                databases[i] = ReadDatabase(reader, clips ?? Array.Empty<AnimationClip>());
            int providerCount = RequireCount(
                reader.ReadInt32(),
                "node binding",
                false);
            var providers =
                new MotionMatchingNodeBindingPayload[providerCount];
            for (int i = 0; i < providerCount; i++)
                providers[i] = ReadNodeBinding(reader);
            if (stream.Position != stream.Length)
                throw new InvalidOperationException("Motion Matching Projection payload contains trailing data.");
            return new MotionMatchingProjectionPayload(
                profileId,
                profileRevision,
                featureSchema,
                trajectoryPolicy,
                costProfile,
                searchPolicy,
                databases,
                providers);
        }

        static void WriteFeatureSchema(BinaryWriter writer, MotionMatchingFeatureSchemaPayload value)
        {
            writer.Write(value.SchemaId.Value);
            writer.Write(value.Revision);
            writer.Write(value.RigId);
            writer.Write(value.RigRevision);
            writer.Write(value.DenseFeatureCount);
            writer.Write(value.HistoryHorizonCount);
            for (int i = 0; i < value.HistoryHorizonCount; i++) writer.Write(value.GetHistoryHorizon(i));
            writer.Write(value.BoneCount);
            for (int i = 0; i < value.BoneCount; i++) writer.Write(value.GetBoneId(i));
            writer.Write(value.FeatureRangeCount);
            for (int i = 0; i < value.FeatureRangeCount; i++)
            {
                MotionMatchingFeatureRange range = value.GetFeatureRange(i);
                writer.Write((byte)range.Group);
                writer.Write(range.Offset);
                writer.Write(range.Count);
            }
            for (int i = 0; i < value.DenseFeatureCount; i++) writer.Write(value.IsInitializationFeature(i));
        }

        static MotionMatchingFeatureSchemaPayload ReadFeatureSchema(BinaryReader reader)
        {
            var schemaId = new CharacterMotionMatchingFeatureSchemaId(reader.ReadString());
            int revision = reader.ReadInt32();
            string rigId = reader.ReadString();
            string rigRevision = reader.ReadString();
            int denseFeatureCount = RequireCount(reader.ReadInt32(), "dense feature", false);
            int horizonCount = RequireCount(reader.ReadInt32(), "history horizon", false);
            var horizons = new float[horizonCount];
            for (int i = 0; i < horizons.Length; i++) horizons[i] = reader.ReadSingle();
            int boneCount = RequireCount(reader.ReadInt32(), "bone", false);
            var bones = new string[boneCount];
            for (int i = 0; i < bones.Length; i++) bones[i] = reader.ReadString();
            int rangeCount = RequireCount(reader.ReadInt32(), "feature range", false);
            var ranges = new MotionMatchingFeatureRange[rangeCount];
            for (int i = 0; i < ranges.Length; i++)
                ranges[i] = new MotionMatchingFeatureRange((MotionMatchingCostGroup)reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
            var mask = new bool[denseFeatureCount];
            for (int i = 0; i < mask.Length; i++) mask[i] = reader.ReadBoolean();
            return new MotionMatchingFeatureSchemaPayload(schemaId, revision, rigId, rigRevision, horizons, bones, ranges, mask, denseFeatureCount);
        }

        static void WriteTrajectoryPolicy(BinaryWriter writer, MotionMatchingTrajectoryPolicyPayload value)
        {
            writer.Write(value.PolicyId);
            writer.Write(value.Revision);
            writer.Write(value.MaximumAcceleration);
            writer.Write(value.MaximumTurnRateDegrees);
            writer.Write(value.SelectedAgePositionTolerancePerSecond);
            writer.Write(value.SelectedAgeFacingTolerancePerSecond);
            writer.Write(value.SelectedAgeConfidenceDecayPerSecond);
            writer.Write(value.PointCount);
            for (int i = 0; i < value.PointCount; i++)
            {
                MotionMatchingTrajectoryPolicyRuntimePoint point = value.GetPoint(i);
                writer.Write(point.TimeOffset);
                writer.Write(point.AcceptedPositionTolerance);
                writer.Write(point.AcceptedFacingToleranceDegrees);
                writer.Write(point.AcceptedConfidence);
                writer.Write(point.SelectedPositionTolerance);
                writer.Write(point.SelectedFacingToleranceDegrees);
                writer.Write(point.SelectedConfidence);
            }
        }

        static MotionMatchingTrajectoryPolicyPayload ReadTrajectoryPolicy(BinaryReader reader)
        {
            string policyId = reader.ReadString();
            int revision = reader.ReadInt32();
            float maximumAcceleration = reader.ReadSingle();
            float maximumTurnRate = reader.ReadSingle();
            float positionAge = reader.ReadSingle();
            float facingAge = reader.ReadSingle();
            float confidenceAge = reader.ReadSingle();
            int count = RequireCount(reader.ReadInt32(), "trajectory policy point", false);
            var points = new MotionMatchingTrajectoryPolicyRuntimePoint[count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new MotionMatchingTrajectoryPolicyRuntimePoint(
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            return new MotionMatchingTrajectoryPolicyPayload(
                policyId, revision, maximumAcceleration, maximumTurnRate, positionAge, facingAge, confidenceAge, points);
        }

        static void WriteCostProfile(BinaryWriter writer, MotionMatchingCostProfilePayload value)
        {
            writer.Write(value.ProfileId);
            writer.Write(value.Revision);
            writer.Write(value.DenseFeatureCount);
            for (int i = 0; i < value.DenseFeatureCount; i++) writer.Write(value.GetDenseFeatureWeight(i));
            int groupCount = Enum.GetValues(typeof(MotionMatchingCostGroup)).Length + 1;
            writer.Write(groupCount);
            for (int i = 0; i < groupCount; i++) writer.Write(value.GetGroupWeight((MotionMatchingCostGroup)i));
        }

        static MotionMatchingCostProfilePayload ReadCostProfile(BinaryReader reader)
        {
            string id = reader.ReadString();
            int revision = reader.ReadInt32();
            int denseCount = RequireCount(reader.ReadInt32(), "cost feature", false);
            var dense = new float[denseCount];
            for (int i = 0; i < dense.Length; i++) dense[i] = reader.ReadSingle();
            int groupCount = RequireCount(reader.ReadInt32(), "cost group", false);
            var groups = new float[groupCount];
            for (int i = 0; i < groups.Length; i++) groups[i] = reader.ReadSingle();
            return new MotionMatchingCostProfilePayload(id, revision, dense, groups);
        }

        static void WriteSearchPolicy(BinaryWriter writer, MotionMatchingSearchPolicyPayload value)
        {
            writer.Write(value.PolicyId);
            writer.Write(value.Revision);
            writer.Write(value.TopK);
            writer.Write(value.LeafCapacity);
            writer.Write(value.PlanSampleCount);
            writer.Write(value.PlanSampleInterval);
            writer.Write(value.SearchInterval);
            writer.Write(value.MinimumJumpInterval);
            writer.Write(value.MaximumAdmittedSampleCount);
            writer.Write(value.MaximumTreeDepth);
            writer.Write(value.CoverageNearDuplicateCostThreshold);
            writer.Write(value.HistoryCapacity);
            writer.Write(value.DiagnosticDetailCapacity);
            writer.Write(value.ProtectedFootPositionJumpLimit);
            writer.Write(value.ProtectedFootVelocityJumpLimit);
        }

        static MotionMatchingSearchPolicyPayload ReadSearchPolicy(BinaryReader reader) => new MotionMatchingSearchPolicyPayload(
            reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadInt32(), reader.ReadInt32(),
            reader.ReadSingle(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle());

        static void WriteDatabase(BinaryWriter writer, MotionMatchingDatabasePayload value, List<AnimationClip> clipTable)
        {
            WriteArtifactIdentity(writer, value.ArtifactIdentity);
            writer.Write(value.SearchDomainId.Value);
            writer.Write(value.SampleRate);
            WriteCapacities(writer, value.Capacities);
            writer.Write(value.ClipBindingCount);
            for (int i = 0; i < value.ClipBindingCount; i++) WriteClipBinding(writer, value.GetClipBinding(i), clipTable);
            writer.Write(value.SegmentCount);
            for (int i = 0; i < value.SegmentCount; i++) WriteSegment(writer, value.GetSegment(i));
            writer.Write(value.SampleCount);
            for (int i = 0; i < value.SampleCount; i++) WriteSample(writer, value.GetSample(i));
            int denseValueCount = checked(value.SampleCount * value.Capacities.DenseFeatureCount);
            writer.Write(denseValueCount);
            for (int sample = 0; sample < value.SampleCount; sample++)
                for (int feature = 0; feature < value.Capacities.DenseFeatureCount; feature++)
                    writer.Write(value.GetNormalizedFeature(sample, feature));
            for (int i = 0; i < value.Capacities.DenseFeatureCount; i++) writer.Write(value.GetNormalizationMedian(i));
            for (int i = 0; i < value.Capacities.DenseFeatureCount; i++) writer.Write(value.GetNormalizationScale(i));
            for (int i = 0; i < value.Capacities.DenseFeatureCount; i++) writer.Write(value.IsFeatureActive(i));
            writer.Write(value.SearchNodeCount);
            for (int i = 0; i < value.SearchNodeCount; i++) WriteSearchNode(writer, value.GetSearchNode(i));
            writer.Write(value.SampleCount);
            for (int i = 0; i < value.SampleCount; i++) writer.Write(value.GetOrderedSampleIndex(i));
            writer.Write(value.CoverageCount);
            for (int i = 0; i < value.CoverageCount; i++) WriteCoverage(writer, value.GetCoverage(i));
        }

        static MotionMatchingDatabasePayload ReadDatabase(BinaryReader reader, AnimationClip[] clipTable)
        {
            CharacterMotionMatchingDatabaseArtifactIdentity identity = ReadArtifactIdentity(reader);
            var domain = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
            float sampleRate = reader.ReadSingle();
            MotionMatchingRuntimeCapacityPayload capacities = ReadCapacities(reader);
            int clipCount = RequireCount(reader.ReadInt32(), "clip binding", false);
            var clips = new MotionMatchingClipBindingPayload[clipCount];
            for (int i = 0; i < clips.Length; i++) clips[i] = ReadClipBinding(reader, clipTable);
            int segmentCount = RequireCount(reader.ReadInt32(), "segment", false);
            var segments = new MotionMatchingSegmentPayload[segmentCount];
            for (int i = 0; i < segments.Length; i++) segments[i] = ReadSegment(reader);
            int sampleCount = RequireCount(reader.ReadInt32(), "sample", false);
            var samples = new MotionMatchingSamplePayload[sampleCount];
            for (int i = 0; i < samples.Length; i++) samples[i] = ReadSample(reader);
            int normalizedCount = RequireCount(reader.ReadInt32(), "normalized feature", false);
            var normalized = new float[normalizedCount];
            for (int i = 0; i < normalized.Length; i++) normalized[i] = reader.ReadSingle();
            var median = new float[capacities.DenseFeatureCount];
            var scale = new float[capacities.DenseFeatureCount];
            var active = new bool[capacities.DenseFeatureCount];
            for (int i = 0; i < median.Length; i++) median[i] = reader.ReadSingle();
            for (int i = 0; i < scale.Length; i++) scale[i] = reader.ReadSingle();
            for (int i = 0; i < active.Length; i++) active[i] = reader.ReadBoolean();
            int nodeCount = RequireCount(reader.ReadInt32(), "search node", false);
            var nodes = new MotionMatchingSearchIndexNodePayload[nodeCount];
            for (int i = 0; i < nodes.Length; i++) nodes[i] = ReadSearchNode(reader);
            int orderedCount = RequireCount(reader.ReadInt32(), "ordered sample", false);
            var ordered = new int[orderedCount];
            for (int i = 0; i < ordered.Length; i++) ordered[i] = reader.ReadInt32();
            int coverageCount = RequireCount(reader.ReadInt32(), "coverage", false);
            var coverage = new MotionMatchingCoverageSummaryPayload[coverageCount];
            for (int i = 0; i < coverage.Length; i++) coverage[i] = ReadCoverage(reader);
            return new MotionMatchingDatabasePayload(
                identity, domain, sampleRate, capacities, clips, segments, samples, normalized, median, scale, active, nodes, ordered, coverage);
        }

        static void WriteArtifactIdentity(BinaryWriter writer, CharacterMotionMatchingDatabaseArtifactIdentity value)
        {
            writer.Write(value.ArtifactSchemaVersion);
            writer.Write(value.AnalysisAlgorithmVersion);
            writer.Write(value.DatabaseId.Value);
            writer.Write(value.DatabaseRevision);
            writer.Write(value.FeatureSchemaId.Value);
            writer.Write(value.FeatureSchemaRevision);
            writer.Write(value.RigId);
            writer.Write(value.RigRevision);
            writer.Write(value.ClipDependencyCount);
            for (int i = 0; i < value.ClipDependencyCount; i++)
            {
                MotionMatchingClipDependencyIdentity dependency = value.GetClipDependency(i);
                writer.Write(dependency.SourceSetId.Value);
                writer.Write(dependency.SourceSetRevision);
                writer.Write(dependency.SourceClipId.Value);
                writer.Write(dependency.AssetGuid);
                writer.Write(dependency.LocalFileId);
                writer.Write(dependency.ImportDependencyHash);
                writer.Write(dependency.SamplingRigSignature);
                writer.Write(dependency.MotionRootBoneId.Value);
                writer.Write(dependency.FootArtifactHash.Value);
            }
            writer.Write(value.AnalysisInputHash.Value);
            writer.Write(value.OrderedClipDependencyHash.Value);
            writer.Write(value.ContentHash.Value);
        }

        static CharacterMotionMatchingDatabaseArtifactIdentity ReadArtifactIdentity(BinaryReader reader)
        {
            int schema = reader.ReadInt32();
            string algorithm = reader.ReadString();
            var databaseId = new CharacterMotionMatchingDatabaseId(reader.ReadString());
            int databaseRevision = reader.ReadInt32();
            var featureSchemaId = new CharacterMotionMatchingFeatureSchemaId(reader.ReadString());
            int featureRevision = reader.ReadInt32();
            string rigId = reader.ReadString();
            string rigRevision = reader.ReadString();
            int dependencyCount = RequireCount(reader.ReadInt32(), "clip dependency", false);
            var dependencies = new MotionMatchingClipDependencyIdentity[dependencyCount];
            for (int i = 0; i < dependencies.Length; i++)
            {
                dependencies[i] = new MotionMatchingClipDependencyIdentity(
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
                schema, algorithm, databaseId, databaseRevision, featureSchemaId, featureRevision, rigId, rigRevision,
                dependencies, new StableHash(reader.ReadString()), new StableHash(reader.ReadString()), new StableHash(reader.ReadString()));
        }

        static void WriteCapacities(BinaryWriter writer, MotionMatchingRuntimeCapacityPayload value)
        {
            writer.Write(value.DenseFeatureCount);
            writer.Write(value.SampleCount);
            writer.Write(value.TreeNodeCount);
            writer.Write(value.TraversalCapacity);
            writer.Write(value.TopK);
            writer.Write(value.PlanSampleCount);
            writer.Write(value.HistoryCapacity);
            writer.Write(value.DiagnosticDetailCapacity);
        }

        static MotionMatchingRuntimeCapacityPayload ReadCapacities(BinaryReader reader) => new MotionMatchingRuntimeCapacityPayload(
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

        static void WriteClipBinding(BinaryWriter writer, MotionMatchingClipBindingPayload value, List<AnimationClip> clipTable)
        {
            writer.Write(value.SourceClipId.Value);
            writer.Write(value.AssetGuid);
            writer.Write(value.LocalFileId);
            int clipIndex = clipTable.IndexOf(value.Clip);
            if (clipIndex < 0)
            {
                clipIndex = clipTable.Count;
                clipTable.Add(value.Clip);
            }
            writer.Write(clipIndex);
            writer.Write(value.RootLocked);
            MotionMatchingPoseParameterCurvePayload curve = value.FootPlacementWeightCurve;
            writer.Write(curve.ParameterId.Value);
            writer.Write(curve.KeyCount);
            for (int i = 0; i < curve.KeyCount; i++)
            {
                writer.Write(curve.GetNormalizedTime(i));
                writer.Write(curve.GetValue(i));
            }
        }

        static MotionMatchingClipBindingPayload ReadClipBinding(BinaryReader reader, AnimationClip[] clipTable)
        {
            var sourceClipId = new CharacterMotionMatchingSourceClipId(reader.ReadString());
            string guid = reader.ReadString();
            long localId = reader.ReadInt64();
            int clipIndex = reader.ReadInt32();
            if ((uint)clipIndex >= (uint)clipTable.Length || !clipTable[clipIndex])
                throw new InvalidOperationException($"Motion Matching Projection clip reference #{clipIndex} is missing.");
            bool rootLocked = reader.ReadBoolean();
            var parameterId = new PoseParameterId(reader.ReadString());
            int count = RequireCount(reader.ReadInt32(), "parameter curve key", false);
            var times = new float[count];
            var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                times[i] = reader.ReadSingle();
                values[i] = reader.ReadSingle();
            }
            return new MotionMatchingClipBindingPayload(
                sourceClipId, guid, localId, clipTable[clipIndex], rootLocked,
                new MotionMatchingPoseParameterCurvePayload(parameterId, times, values));
        }

        static void WriteSegment(BinaryWriter writer, MotionMatchingSegmentPayload value)
        {
            writer.Write(value.SegmentId.Value);
            writer.Write(value.SourceClipId.Value);
            writer.Write(value.FirstSampleIndex);
            writer.Write(value.SampleCount);
            writer.Write(value.StartTime);
            writer.Write(value.EndTime);
            writer.Write((byte)value.LoopMode);
            writer.Write(value.Terminal);
            writer.Write(value.ContinuationEntrySampleIndex);
        }

        static MotionMatchingSegmentPayload ReadSegment(BinaryReader reader) => new MotionMatchingSegmentPayload(
            new CharacterMotionMatchingSegmentId(reader.ReadString()),
            new CharacterMotionMatchingSourceClipId(reader.ReadString()),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(),
            (MotionMatchingSegmentLoopMode)reader.ReadByte(), reader.ReadBoolean(), reader.ReadInt32());

        static void WriteSample(BinaryWriter writer, MotionMatchingSamplePayload value)
        {
            writer.Write(value.SampleId.Value);
            writer.Write(value.SegmentId.Value);
            writer.Write(value.SearchDomainId.Value);
            writer.Write(value.ClipBindingIndex);
            writer.Write(value.SampleTime);
            writer.Write(value.CanInitialize);
            writer.Write(value.CanJumpInto);
            writer.Write(value.EntryExcluded);
            writer.Write(value.ExitExcluded);
            writer.Write(value.Terminal);
            writer.Write(value.NextSampleIndex);
            writer.Write((byte)value.ContactMask);
            WriteVector2(writer, value.RootPlanarVelocity);
            writer.Write(value.RootYawVelocityDegrees);
            WriteVector3(writer, value.LeftFootRootPosition);
            WriteVector3(writer, value.RightFootRootPosition);
            WriteFootSample(writer, value.LeftFoot);
            WriteFootSample(writer, value.RightFoot);
        }

        static MotionMatchingSamplePayload ReadSample(BinaryReader reader) => new MotionMatchingSamplePayload(
            new CharacterMotionMatchingSampleId(reader.ReadUInt32()),
            new CharacterMotionMatchingSegmentId(reader.ReadString()),
            new CharacterMotionMatchingSearchDomainId(reader.ReadString()),
            reader.ReadInt32(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(),
            reader.ReadBoolean(), reader.ReadInt32(), (MotionMatchingFootContactMask)reader.ReadByte(), ReadVector2(reader), reader.ReadSingle(),
            ReadVector3(reader), ReadVector3(reader), ReadFootSample(reader), ReadFootSample(reader));

        static void WriteSearchNode(BinaryWriter writer, MotionMatchingSearchIndexNodePayload value)
        {
            writer.Write(value.NodeId.Value);
            writer.Write(value.LeftChildIndex);
            writer.Write(value.RightChildIndex);
            writer.Write(value.OrderedSampleOffset);
            writer.Write(value.OrderedSampleCount);
            writer.Write(value.SearchDomainId.Value);
            writer.Write((byte)value.ContactMaskUnion);
            writer.Write(value.FeatureCount);
            for (int i = 0; i < value.FeatureCount; i++) writer.Write(value.GetMinimum(i));
            for (int i = 0; i < value.FeatureCount; i++) writer.Write(value.GetMaximum(i));
        }

        static MotionMatchingSearchIndexNodePayload ReadSearchNode(BinaryReader reader)
        {
            var nodeId = new CharacterMotionMatchingIndexNodeId(reader.ReadInt32());
            int left = reader.ReadInt32();
            int right = reader.ReadInt32();
            int offset = reader.ReadInt32();
            int count = reader.ReadInt32();
            var domain = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
            var contacts = (MotionMatchingFootContactMask)reader.ReadByte();
            int featureCount = RequireCount(reader.ReadInt32(), "search bound", false);
            var minimum = new float[featureCount];
            var maximum = new float[featureCount];
            for (int i = 0; i < minimum.Length; i++) minimum[i] = reader.ReadSingle();
            for (int i = 0; i < maximum.Length; i++) maximum[i] = reader.ReadSingle();
            return new MotionMatchingSearchIndexNodePayload(nodeId, left, right, offset, count, domain, contacts, minimum, maximum);
        }

        static void WriteCoverage(BinaryWriter writer, MotionMatchingCoverageSummaryPayload value)
        {
            writer.Write(value.RequirementId);
            writer.Write(value.Satisfied);
            writer.Write(value.SampleCount);
            writer.Write(value.MinimumObservedSpeed);
            writer.Write(value.MaximumObservedSpeed);
            writer.Write(value.MaximumObservedFacingChange);
            writer.Write(value.MinimumObservedPlanHorizon);
        }

        static MotionMatchingCoverageSummaryPayload ReadCoverage(BinaryReader reader) => new MotionMatchingCoverageSummaryPayload(
            reader.ReadString(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        static void WriteNodeBinding(
            BinaryWriter writer,
            MotionMatchingNodeBindingPayload value)
        {
            writer.Write(value.BindingId.Value);
            writer.Write(value.BindingRevision);
            writer.Write(value.PoseNodeId.Value);
            WriteChooser(writer, value.Chooser);
            writer.Write(value.FirstDatabaseIndex);
            writer.Write(value.DatabaseCount);
        }

        static MotionMatchingNodeBindingPayload ReadNodeBinding(
            BinaryReader reader) =>
            new MotionMatchingNodeBindingPayload(
                new CharacterMotionMatchingBindingId(reader.ReadString()),
                reader.ReadInt32(),
                new PoseNodeId(reader.ReadString()),
                ReadChooser(reader),
                reader.ReadInt32(),
                reader.ReadInt32());

        static void WriteChooser(
            BinaryWriter writer,
            MotionMatchingDatabaseChooserPayload value)
        {
            writer.Write(value.ChooserId.Value);
            writer.Write(value.ChooserRevision);
            writer.Write(value.SearchDomainId.Value);
            writer.Write(value.RuleCount);
            for (int ruleIndex = 0; ruleIndex < value.RuleCount; ruleIndex++)
            {
                MotionMatchingDatabaseChooserRulePayload rule = value.GetRule(ruleIndex);
                writer.Write(rule.Priority);
                writer.Write(rule.Exclusive);
                writer.Write(rule.ShouldSearch);
                writer.Write((byte)rule.InterruptMode);
                writer.Write(rule.SearchPolicyOverrideId);
                writer.Write(rule.PredicateCount);
                for (int predicateIndex = 0; predicateIndex < rule.PredicateCount; predicateIndex++)
                    WriteChooserPredicate(writer, rule.GetPredicate(predicateIndex));
                writer.Write(rule.DatabaseCount);
                for (int databaseIndex = 0; databaseIndex < rule.DatabaseCount; databaseIndex++)
                    writer.Write(rule.GetDatabaseIndex(databaseIndex));
            }
        }

        static MotionMatchingDatabaseChooserPayload ReadChooser(BinaryReader reader)
        {
            var chooserId = new CharacterMotionMatchingDatabaseChooserId(reader.ReadString());
            int chooserRevision = reader.ReadInt32();
            var searchDomainId = new CharacterMotionMatchingSearchDomainId(reader.ReadString());
            int ruleCount = RequireCount(reader.ReadInt32(), "chooser rule", false);
            var rules = new MotionMatchingDatabaseChooserRulePayload[ruleCount];
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                int priority = reader.ReadInt32();
                bool exclusive = reader.ReadBoolean();
                bool shouldSearch = reader.ReadBoolean();
                var interruptMode = (CharacterMotionMatchingChooserInterruptMode)reader.ReadByte();
                string searchPolicyOverrideId = reader.ReadString();
                int predicateCount = RequireCount(reader.ReadInt32(), "chooser predicate", false);
                var predicates = new MotionMatchingFactPredicatePayload[predicateCount];
                for (int predicateIndex = 0; predicateIndex < predicates.Length; predicateIndex++)
                    predicates[predicateIndex] = ReadChooserPredicate(reader);
                int databaseCount = RequireCount(reader.ReadInt32(), "chooser Database index", false);
                var databaseIndices = new int[databaseCount];
                for (int databaseIndex = 0; databaseIndex < databaseIndices.Length; databaseIndex++)
                    databaseIndices[databaseIndex] = reader.ReadInt32();
                rules[ruleIndex] = new MotionMatchingDatabaseChooserRulePayload(
                    priority,
                    exclusive,
                    predicates,
                    databaseIndices,
                    shouldSearch,
                    interruptMode,
                    searchPolicyOverrideId);
            }
            return new MotionMatchingDatabaseChooserPayload(
                chooserId,
                chooserRevision,
                searchDomainId,
                rules);
        }

        static void WriteChooserPredicate(
            BinaryWriter writer,
            MotionMatchingFactPredicatePayload value)
        {
            writer.Write(value.FactId.Value);
            writer.Write((byte)value.ValueKind);
            writer.Write((byte)value.Operator);
            writer.Write(value.BoolValue);
            writer.Write(value.FloatValue);
            WriteVector2(writer, value.Vector2Value);
            writer.Write(value.EnumValue);
            writer.Write(value.UInt64Value);
            writer.Write(value.IdentityValue);
        }

        static MotionMatchingFactPredicatePayload ReadChooserPredicate(
            BinaryReader reader) =>
            new MotionMatchingFactPredicatePayload(
                new PresentationFactId(reader.ReadString()),
                (PresentationFactValueKind)reader.ReadByte(),
                (CharacterMotionMatchingChooserPredicateOperator)reader.ReadByte(),
                reader.ReadBoolean(),
                reader.ReadSingle(),
                ReadVector2(reader),
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadString());

        static void WriteFootSample(BinaryWriter writer, AnimationFootFeatureSample value)
        {
            WriteVector3(writer, value.SoleLocalVelocity);
            writer.Write(value.SoleHeight);
            writer.Write(value.PlantConfidence);
            writer.Write(value.NextLandingConfidence);
            writer.Write(value.NextLandingDelaySeconds);
            WriteVector2(writer, value.NextLandingLocalOffset);
        }

        static AnimationFootFeatureSample ReadFootSample(BinaryReader reader) => new AnimationFootFeatureSample(
            ReadVector3(reader), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), ReadVector2(reader));
        static void WriteVector2(BinaryWriter writer, Vector2 value) { writer.Write(value.x); writer.Write(value.y); }
        static Vector2 ReadVector2(BinaryReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
        static void WriteVector3(BinaryWriter writer, Vector3 value) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
        static Vector3 ReadVector3(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        static int RequireCount(int value, string name, bool allowZero)
        {
            if (value < 0 || !allowZero && value == 0)
                throw new InvalidOperationException($"Motion Matching Projection {name} count is invalid.");
            return value;
        }
    }
}
