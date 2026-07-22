using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingClipDependencyIdentity
    {
        public MotionMatchingClipDependencyIdentity(
            CharacterMotionMatchingSourceSetId sourceSetId,
            int sourceSetRevision,
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            string importDependencyHash,
            string samplingRigSignature,
            AnimationBoneId motionRootBoneId,
            StableHash footArtifactHash)
        {
            if (!sourceSetId.IsValid || sourceSetRevision <= 0 || !sourceClipId.IsValid ||
                !MotionMatchingAuthoringValidation.IsAssetGuid(assetGuid) || localFileId == 0 ||
                string.IsNullOrWhiteSpace(importDependencyHash) || string.IsNullOrWhiteSpace(samplingRigSignature) ||
                !motionRootBoneId.IsValid || !footArtifactHash.IsValid)
                throw new ArgumentException("Motion Matching Clip dependency identity is invalid.");
            SourceSetId = sourceSetId;
            SourceSetRevision = sourceSetRevision;
            SourceClipId = sourceClipId;
            AssetGuid = assetGuid;
            LocalFileId = localFileId;
            ImportDependencyHash = importDependencyHash;
            SamplingRigSignature = samplingRigSignature;
            MotionRootBoneId = motionRootBoneId;
            FootArtifactHash = footArtifactHash;
        }

        public CharacterMotionMatchingSourceSetId SourceSetId { get; }
        public int SourceSetRevision { get; }
        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public string ImportDependencyHash { get; }
        public string SamplingRigSignature { get; }
        public AnimationBoneId MotionRootBoneId { get; }
        public StableHash FootArtifactHash { get; }
    }

    public sealed class CharacterMotionMatchingDatabaseArtifactIdentity
    {
        readonly MotionMatchingClipDependencyIdentity[] m_ClipDependencies;

        public CharacterMotionMatchingDatabaseArtifactIdentity(
            int artifactSchemaVersion,
            string analysisAlgorithmVersion,
            CharacterMotionMatchingDatabaseId databaseId,
            int databaseRevision,
            CharacterMotionMatchingFeatureSchemaId featureSchemaId,
            int featureSchemaRevision,
            string rigId,
            string rigRevision,
            MotionMatchingClipDependencyIdentity[] clipDependencies,
            StableHash analysisInputHash,
            StableHash orderedClipDependencyHash,
            StableHash contentHash)
        {
            if (artifactSchemaVersion <= 0 || string.IsNullOrWhiteSpace(analysisAlgorithmVersion) ||
                !databaseId.IsValid || databaseRevision <= 0 || !featureSchemaId.IsValid || featureSchemaRevision <= 0 ||
                string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision) ||
                clipDependencies == null || clipDependencies.Length == 0 || !analysisInputHash.IsValid ||
                !orderedClipDependencyHash.IsValid || !contentHash.IsValid)
                throw new ArgumentException("Motion Matching Database Artifact identity is incomplete.");
            m_ClipDependencies = (MotionMatchingClipDependencyIdentity[])clipDependencies.Clone();
            for (int i = 1; i < m_ClipDependencies.Length; i++)
            {
                if (m_ClipDependencies[i - 1].SourceClipId.CompareTo(m_ClipDependencies[i].SourceClipId) >= 0)
                    throw new ArgumentException("Motion Matching Clip dependencies are not in strict SourceClipId order.", nameof(clipDependencies));
            }
            ArtifactSchemaVersion = artifactSchemaVersion;
            AnalysisAlgorithmVersion = analysisAlgorithmVersion;
            DatabaseId = databaseId;
            DatabaseRevision = databaseRevision;
            FeatureSchemaId = featureSchemaId;
            FeatureSchemaRevision = featureSchemaRevision;
            RigId = rigId;
            RigRevision = rigRevision;
            AnalysisInputHash = analysisInputHash;
            OrderedClipDependencyHash = orderedClipDependencyHash;
            ContentHash = contentHash;
        }

        public int ArtifactSchemaVersion { get; }
        public string AnalysisAlgorithmVersion { get; }
        public CharacterMotionMatchingDatabaseId DatabaseId { get; }
        public int DatabaseRevision { get; }
        public CharacterMotionMatchingFeatureSchemaId FeatureSchemaId { get; }
        public int FeatureSchemaRevision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public int ClipDependencyCount => m_ClipDependencies.Length;
        public StableHash AnalysisInputHash { get; }
        public StableHash OrderedClipDependencyHash { get; }
        public StableHash ContentHash { get; }
        public MotionMatchingClipDependencyIdentity GetClipDependency(int index) => m_ClipDependencies[index];

        public bool EqualsExact(CharacterMotionMatchingDatabaseArtifactIdentity other)
        {
            if (other == null || ArtifactSchemaVersion != other.ArtifactSchemaVersion || DatabaseRevision != other.DatabaseRevision ||
                FeatureSchemaRevision != other.FeatureSchemaRevision || ClipDependencyCount != other.ClipDependencyCount ||
                !DatabaseId.Equals(other.DatabaseId) || !FeatureSchemaId.Equals(other.FeatureSchemaId) ||
                !string.Equals(AnalysisAlgorithmVersion, other.AnalysisAlgorithmVersion, StringComparison.Ordinal) ||
                !string.Equals(RigId, other.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) ||
                !AnalysisInputHash.Equals(other.AnalysisInputHash) ||
                !OrderedClipDependencyHash.Equals(other.OrderedClipDependencyHash) || !ContentHash.Equals(other.ContentHash))
                return false;
            for (int i = 0; i < ClipDependencyCount; i++)
            {
                MotionMatchingClipDependencyIdentity left = GetClipDependency(i);
                MotionMatchingClipDependencyIdentity right = other.GetClipDependency(i);
                if (!left.SourceSetId.Equals(right.SourceSetId) || left.SourceSetRevision != right.SourceSetRevision ||
                    !left.SourceClipId.Equals(right.SourceClipId) || !string.Equals(left.AssetGuid, right.AssetGuid, StringComparison.Ordinal) ||
                    left.LocalFileId != right.LocalFileId || !string.Equals(left.ImportDependencyHash, right.ImportDependencyHash, StringComparison.Ordinal) ||
                    !string.Equals(left.SamplingRigSignature, right.SamplingRigSignature, StringComparison.Ordinal) ||
                    !left.MotionRootBoneId.Equals(right.MotionRootBoneId) || !left.FootArtifactHash.Equals(right.FootArtifactHash))
                    return false;
            }
            return true;
        }
    }

    public sealed class MotionMatchingDatabasePayload
    {
        readonly MotionMatchingClipBindingPayload[] m_ClipBindings;
        readonly MotionMatchingSegmentPayload[] m_Segments;
        readonly MotionMatchingSamplePayload[] m_Samples;
        readonly float[] m_NormalizedFeatures;
        readonly float[] m_NormalizationMedian;
        readonly float[] m_NormalizationScale;
        readonly bool[] m_ActiveFeatureChannels;
        readonly MotionMatchingSearchIndexNodePayload[] m_SearchNodes;
        readonly int[] m_OrderedSampleIndices;
        readonly MotionMatchingCoverageSummaryPayload[] m_Coverage;

        public MotionMatchingDatabasePayload(
            CharacterMotionMatchingDatabaseArtifactIdentity artifactIdentity,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            float sampleRate,
            MotionMatchingRuntimeCapacityPayload capacities,
            MotionMatchingClipBindingPayload[] clipBindings,
            MotionMatchingSegmentPayload[] segments,
            MotionMatchingSamplePayload[] samples,
            float[] normalizedFeatures,
            float[] normalizationMedian,
            float[] normalizationScale,
            bool[] activeFeatureChannels,
            MotionMatchingSearchIndexNodePayload[] searchNodes,
            int[] orderedSampleIndices,
            MotionMatchingCoverageSummaryPayload[] coverage)
        {
            if (artifactIdentity == null || !searchDomainId.IsValid || !float.IsFinite(sampleRate) || sampleRate <= 0f ||
                clipBindings == null || clipBindings.Length == 0 || segments == null || segments.Length == 0 ||
                samples == null || samples.Length == 0 || normalizedFeatures == null ||
                normalizationMedian == null || normalizationScale == null || activeFeatureChannels == null ||
                searchNodes == null || searchNodes.Length == 0 || orderedSampleIndices == null ||
                orderedSampleIndices.Length != samples.Length || coverage == null || coverage.Length == 0)
                throw new ArgumentException("Motion Matching Database payload is incomplete.");
            if (samples.Length != capacities.SampleCount || searchNodes.Length != capacities.TreeNodeCount ||
                normalizedFeatures.Length != samples.Length * capacities.DenseFeatureCount ||
                normalizationMedian.Length != capacities.DenseFeatureCount || normalizationScale.Length != capacities.DenseFeatureCount ||
                activeFeatureChannels.Length != capacities.DenseFeatureCount)
                throw new ArgumentException("Motion Matching Database payload lengths do not match compiled capacities.");
            m_ClipBindings = (MotionMatchingClipBindingPayload[])clipBindings.Clone();
            m_Segments = (MotionMatchingSegmentPayload[])segments.Clone();
            m_Samples = (MotionMatchingSamplePayload[])samples.Clone();
            m_NormalizedFeatures = (float[])normalizedFeatures.Clone();
            m_NormalizationMedian = (float[])normalizationMedian.Clone();
            m_NormalizationScale = (float[])normalizationScale.Clone();
            m_ActiveFeatureChannels = (bool[])activeFeatureChannels.Clone();
            m_SearchNodes = (MotionMatchingSearchIndexNodePayload[])searchNodes.Clone();
            m_OrderedSampleIndices = (int[])orderedSampleIndices.Clone();
            m_Coverage = (MotionMatchingCoverageSummaryPayload[])coverage.Clone();
            ValidateCanonical(searchDomainId, capacities);
            ArtifactIdentity = artifactIdentity;
            SearchDomainId = searchDomainId;
            SampleRate = sampleRate;
            Capacities = capacities;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity ArtifactIdentity { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public float SampleRate { get; }
        public MotionMatchingRuntimeCapacityPayload Capacities { get; }
        public int ClipBindingCount => m_ClipBindings.Length;
        public int SegmentCount => m_Segments.Length;
        public int SampleCount => m_Samples.Length;
        public int SearchNodeCount => m_SearchNodes.Length;
        public int CoverageCount => m_Coverage.Length;
        public MotionMatchingClipBindingPayload GetClipBinding(int index) => m_ClipBindings[index];
        public MotionMatchingSegmentPayload GetSegment(int index) => m_Segments[index];
        public MotionMatchingSamplePayload GetSample(int index) => m_Samples[index];
        public float GetNormalizedFeature(int sampleIndex, int featureIndex) => m_NormalizedFeatures[sampleIndex * Capacities.DenseFeatureCount + featureIndex];
        public float GetNormalizationMedian(int featureIndex) => m_NormalizationMedian[featureIndex];
        public float GetNormalizationScale(int featureIndex) => m_NormalizationScale[featureIndex];
        public bool IsFeatureActive(int featureIndex) => m_ActiveFeatureChannels[featureIndex];
        public MotionMatchingSearchIndexNodePayload GetSearchNode(int index) => m_SearchNodes[index];
        public int GetOrderedSampleIndex(int index) => m_OrderedSampleIndices[index];
        public MotionMatchingCoverageSummaryPayload GetCoverage(int index) => m_Coverage[index];

        void ValidateCanonical(CharacterMotionMatchingSearchDomainId domain, MotionMatchingRuntimeCapacityPayload capacities)
        {
            for (int i = 0; i < m_NormalizedFeatures.Length; i++)
            {
                if (!float.IsFinite(m_NormalizedFeatures[i]))
                    throw new ArgumentException("Motion Matching Database payload contains a non-finite normalized feature.");
            }
            for (int i = 0; i < capacities.DenseFeatureCount; i++)
            {
                if (!float.IsFinite(m_NormalizationMedian[i]) || !float.IsFinite(m_NormalizationScale[i]) ||
                    m_NormalizationScale[i] < 0f || m_ActiveFeatureChannels[i] == (m_NormalizationScale[i] == 0f))
                    throw new ArgumentException("Motion Matching Database payload normalization is inconsistent.");
            }
            var seen = new bool[m_Samples.Length];
            CharacterMotionMatchingSampleId previous = default;
            for (int i = 0; i < m_Samples.Length; i++)
            {
                MotionMatchingSamplePayload sample = m_Samples[i];
                if (!sample.SearchDomainId.Equals(domain) || i > 0 && sample.SampleId.CompareTo(previous) <= 0 ||
                    sample.ClipBindingIndex >= m_ClipBindings.Length || sample.NextSampleIndex >= m_Samples.Length)
                    throw new ArgumentException($"Motion Matching Database sample #{i} is not canonical.");
                previous = sample.SampleId;
                int ordered = m_OrderedSampleIndices[i];
                if ((uint)ordered >= (uint)m_Samples.Length || seen[ordered])
                    throw new ArgumentException("Motion Matching Search Index sample order is not a permutation.");
                seen[ordered] = true;
            }
            ValidateSegments();
            ValidateSearchTree(domain, capacities);
            var coverageIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Coverage.Length; i++)
            {
                if (!coverageIds.Add(m_Coverage[i].RequirementId))
                    throw new ArgumentException($"Motion Matching Coverage Requirement '{m_Coverage[i].RequirementId}' is duplicated.");
            }
        }

        void ValidateSegments()
        {
            int expectedFirstSampleIndex = 0;
            CharacterMotionMatchingSegmentId previous = default;
            for (int segmentIndex = 0; segmentIndex < m_Segments.Length; segmentIndex++)
            {
                MotionMatchingSegmentPayload segment = m_Segments[segmentIndex];
                if (segmentIndex > 0 && segment.SegmentId.CompareTo(previous) <= 0 ||
                    segment.FirstSampleIndex != expectedFirstSampleIndex ||
                    segment.SampleCount > m_Samples.Length - segment.FirstSampleIndex)
                    throw new ArgumentException($"Motion Matching Segment #{segmentIndex} is not canonical.");
                previous = segment.SegmentId;
                for (int offset = 0; offset < segment.SampleCount; offset++)
                {
                    int sampleIndex = segment.FirstSampleIndex + offset;
                    MotionMatchingSamplePayload sample = m_Samples[sampleIndex];
                    if (!sample.SegmentId.Equals(segment.SegmentId) ||
                        sample.SampleTime < segment.StartTime || sample.SampleTime > segment.EndTime ||
                        !m_ClipBindings[sample.ClipBindingIndex].SourceClipId.Equals(segment.SourceClipId))
                        throw new ArgumentException($"Motion Matching Segment '{segment.SegmentId}' sample #{sampleIndex} is inconsistent.");
                }
                if (segment.LoopMode == MotionMatchingSegmentLoopMode.Loop && segment.ContinuationEntrySampleIndex != segment.FirstSampleIndex ||
                    segment.LoopMode == MotionMatchingSegmentLoopMode.Finite && segment.Terminal && segment.ContinuationEntrySampleIndex >= 0 ||
                    segment.LoopMode == MotionMatchingSegmentLoopMode.Finite && !segment.Terminal &&
                    ((uint)segment.ContinuationEntrySampleIndex >= (uint)m_Samples.Length ||
                     m_Samples[segment.ContinuationEntrySampleIndex].EntryExcluded))
                    throw new ArgumentException($"Motion Matching Segment '{segment.SegmentId}' continuation is inconsistent.");
                expectedFirstSampleIndex += segment.SampleCount;
            }
            if (expectedFirstSampleIndex != m_Samples.Length)
                throw new ArgumentException("Motion Matching Segment ranges do not cover every sample exactly once.");
        }

        void ValidateSearchTree(CharacterMotionMatchingSearchDomainId domain, MotionMatchingRuntimeCapacityPayload capacities)
        {
            var parentCounts = new int[m_SearchNodes.Length];
            var depths = new int[m_SearchNodes.Length];
            var coveredOrderedOffsets = new bool[m_OrderedSampleIndices.Length];
            int maximumDepth = 0;
            for (int i = 0; i < m_SearchNodes.Length; i++)
            {
                MotionMatchingSearchIndexNodePayload node = m_SearchNodes[i];
                if (node == null || node.NodeId.Value != i + 1 || node.FeatureCount != capacities.DenseFeatureCount ||
                    !node.SearchDomainId.Equals(domain) || node.LeftChildIndex >= m_SearchNodes.Length || node.RightChildIndex >= m_SearchNodes.Length ||
                    node.OrderedSampleOffset > m_OrderedSampleIndices.Length ||
                    node.OrderedSampleCount > m_OrderedSampleIndices.Length - node.OrderedSampleOffset)
                    throw new ArgumentException($"Motion Matching Search Index node #{i} is not canonical.");
                maximumDepth = Math.Max(maximumDepth, depths[i]);
                if (node.IsLeaf)
                {
                    for (int offset = 0; offset < node.OrderedSampleCount; offset++)
                    {
                        int orderedOffset = node.OrderedSampleOffset + offset;
                        if (coveredOrderedOffsets[orderedOffset])
                            throw new ArgumentException("Motion Matching Search Index leaf ranges overlap.");
                        coveredOrderedOffsets[orderedOffset] = true;
                        int sampleIndex = m_OrderedSampleIndices[orderedOffset];
                        for (int featureIndex = 0; featureIndex < capacities.DenseFeatureCount; featureIndex++)
                        {
                            float feature = m_NormalizedFeatures[sampleIndex * capacities.DenseFeatureCount + featureIndex];
                            if (feature < node.GetMinimum(featureIndex) || feature > node.GetMaximum(featureIndex))
                                throw new ArgumentException($"Motion Matching Search Index leaf #{i} does not bound sample #{sampleIndex}.");
                        }
                    }
                    continue;
                }

                if (node.LeftChildIndex <= i || node.RightChildIndex <= i || node.LeftChildIndex == node.RightChildIndex)
                    throw new ArgumentException($"Motion Matching Search Index node #{i} has a backward, self, or duplicate child.");
                MotionMatchingSearchIndexNodePayload left = m_SearchNodes[node.LeftChildIndex];
                MotionMatchingSearchIndexNodePayload right = m_SearchNodes[node.RightChildIndex];
                if (left == null || right == null || left.OrderedSampleOffset != node.OrderedSampleOffset ||
                    right.OrderedSampleOffset != left.OrderedSampleOffset + left.OrderedSampleCount ||
                    right.OrderedSampleCount != node.OrderedSampleCount - left.OrderedSampleCount)
                    throw new ArgumentException($"Motion Matching Search Index node #{i} children do not partition the parent range.");
                parentCounts[node.LeftChildIndex]++;
                parentCounts[node.RightChildIndex]++;
                depths[node.LeftChildIndex] = depths[i] + 1;
                depths[node.RightChildIndex] = depths[i] + 1;
                for (int featureIndex = 0; featureIndex < capacities.DenseFeatureCount; featureIndex++)
                {
                    if (left.GetMinimum(featureIndex) < node.GetMinimum(featureIndex) || left.GetMaximum(featureIndex) > node.GetMaximum(featureIndex) ||
                        right.GetMinimum(featureIndex) < node.GetMinimum(featureIndex) || right.GetMaximum(featureIndex) > node.GetMaximum(featureIndex))
                        throw new ArgumentException($"Motion Matching Search Index node #{i} does not contain its child bounds.");
                }
            }
            if (parentCounts[0] != 0)
                throw new ArgumentException("Motion Matching Search Index root has a parent.");
            for (int i = 1; i < parentCounts.Length; i++)
            {
                if (parentCounts[i] != 1)
                    throw new ArgumentException($"Motion Matching Search Index node #{i} is disconnected or has multiple parents.");
            }
            for (int i = 0; i < coveredOrderedOffsets.Length; i++)
            {
                if (!coveredOrderedOffsets[i])
                    throw new ArgumentException($"Motion Matching Search Index does not cover ordered sample offset #{i}.");
            }
            if (capacities.TraversalCapacity != maximumDepth + 1)
                throw new ArgumentException("Motion Matching Search Index traversal capacity does not match root edge-depth.");
        }
    }

    public readonly struct MotionMatchingProducerBindingPayload
    {
        public MotionMatchingProducerBindingPayload(
            string programProducerId,
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            int firstDatabaseIndex,
            int databaseCount)
        {
            ProgramProducerId = MotionMatchingIdentity.Require(programProducerId, nameof(programProducerId));
            if (!animationChannelId.IsValid || !poseSlotId.IsValid || !searchDomainId.IsValid || firstDatabaseIndex < 0 || databaseCount <= 0)
                throw new ArgumentException("Motion Matching producer binding payload is invalid.");
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            SearchDomainId = searchDomainId;
            FirstDatabaseIndex = firstDatabaseIndex;
            DatabaseCount = databaseCount;
        }

        public string ProgramProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public int FirstDatabaseIndex { get; }
        public int DatabaseCount { get; }
    }

    public sealed class MotionMatchingProjectionPayload
    {
        readonly MotionMatchingDatabasePayload[] m_Databases;
        readonly MotionMatchingProducerBindingPayload[] m_ProducerBindings;

        public MotionMatchingProjectionPayload(
            CharacterMotionMatchingProfileId profileId,
            int profileRevision,
            MotionMatchingFeatureSchemaPayload featureSchema,
            MotionMatchingTrajectoryPolicyPayload trajectoryPolicy,
            MotionMatchingCostProfilePayload costProfile,
            MotionMatchingSearchPolicyPayload searchPolicy,
            MotionMatchingDatabasePayload[] databases,
            MotionMatchingProducerBindingPayload[] producerBindings)
        {
            if (!profileId.IsValid || profileRevision <= 0 || featureSchema == null || trajectoryPolicy == null ||
                costProfile == null || searchPolicy == null || databases == null || databases.Length == 0 ||
                producerBindings == null || producerBindings.Length == 0 ||
                featureSchema.DenseFeatureCount != costProfile.DenseFeatureCount)
                throw new ArgumentException("Motion Matching Projection payload is incomplete.");
            m_Databases = (MotionMatchingDatabasePayload[])databases.Clone();
            m_ProducerBindings = (MotionMatchingProducerBindingPayload[])producerBindings.Clone();
            for (int i = 0; i < m_ProducerBindings.Length; i++)
            {
                MotionMatchingProducerBindingPayload binding = m_ProducerBindings[i];
                if (binding.FirstDatabaseIndex + binding.DatabaseCount > m_Databases.Length)
                    throw new ArgumentException($"Motion Matching producer binding #{i} exceeds the Database payload range.");
                for (int databaseIndex = 0; databaseIndex < binding.DatabaseCount; databaseIndex++)
                {
                    MotionMatchingDatabasePayload database = m_Databases[binding.FirstDatabaseIndex + databaseIndex];
                    if (database == null || !database.SearchDomainId.Equals(binding.SearchDomainId) ||
                        !database.ArtifactIdentity.FeatureSchemaId.Equals(featureSchema.SchemaId))
                        throw new ArgumentException($"Motion Matching producer binding #{i} references an incompatible Database payload.");
                }
            }
            ProfileId = profileId;
            ProfileRevision = profileRevision;
            FeatureSchema = featureSchema;
            TrajectoryPolicy = trajectoryPolicy;
            CostProfile = costProfile;
            SearchPolicy = searchPolicy;
        }

        public CharacterMotionMatchingProfileId ProfileId { get; }
        public int ProfileRevision { get; }
        public MotionMatchingFeatureSchemaPayload FeatureSchema { get; }
        public MotionMatchingTrajectoryPolicyPayload TrajectoryPolicy { get; }
        public MotionMatchingCostProfilePayload CostProfile { get; }
        public MotionMatchingSearchPolicyPayload SearchPolicy { get; }
        public int DatabaseCount => m_Databases.Length;
        public int ProducerBindingCount => m_ProducerBindings.Length;
        public MotionMatchingDatabasePayload GetDatabase(int index) => m_Databases[index];
        public MotionMatchingProducerBindingPayload GetProducerBinding(int index) => m_ProducerBindings[index];
    }
}
