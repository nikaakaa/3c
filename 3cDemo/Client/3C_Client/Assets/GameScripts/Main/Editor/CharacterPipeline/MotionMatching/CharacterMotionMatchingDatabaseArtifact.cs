using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public enum CharacterMotionMatchingArtifactStatus : byte
    {
        Missing = 0,
        Ready = 1,
        Stale = 2,
        Invalid = 3
    }

    public sealed class CharacterMotionMatchingDatabaseArtifact
    {
        readonly MotionMatchingSegmentPayload[] m_Segments;
        readonly MotionMatchingSamplePayload[] m_Samples;
        readonly float[] m_NormalizedFeatures;
        readonly float[] m_NormalizationMedian;
        readonly float[] m_NormalizationScale;
        readonly bool[] m_ActiveFeatureChannels;
        readonly MotionMatchingSearchIndexNodePayload[] m_SearchNodes;
        readonly int[] m_OrderedSampleIndices;
        readonly MotionMatchingCoverageSummaryPayload[] m_Coverage;

        public CharacterMotionMatchingDatabaseArtifact(
            CharacterMotionMatchingDatabaseArtifactIdentity identity,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            float sampleRate,
            MotionMatchingRuntimeCapacityPayload capacities,
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
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (!searchDomainId.IsValid || !float.IsFinite(sampleRate) || sampleRate <= 0f ||
                segments == null || segments.Length == 0 || samples == null || samples.Length == 0 ||
                normalizedFeatures == null || normalizationMedian == null || normalizationScale == null ||
                activeFeatureChannels == null || searchNodes == null || searchNodes.Length == 0 ||
                orderedSampleIndices == null || coverage == null || coverage.Length == 0)
                throw new ArgumentException("Motion Matching Database Artifact is incomplete.");
            if (samples.Length != capacities.SampleCount || searchNodes.Length != capacities.TreeNodeCount ||
                normalizedFeatures.Length != samples.Length * capacities.DenseFeatureCount ||
                normalizationMedian.Length != capacities.DenseFeatureCount || normalizationScale.Length != capacities.DenseFeatureCount ||
                activeFeatureChannels.Length != capacities.DenseFeatureCount || orderedSampleIndices.Length != samples.Length)
                throw new ArgumentException("Motion Matching Database Artifact sections do not match compiled capacities.");
            m_Segments = (MotionMatchingSegmentPayload[])segments.Clone();
            m_Samples = (MotionMatchingSamplePayload[])samples.Clone();
            m_NormalizedFeatures = (float[])normalizedFeatures.Clone();
            m_NormalizationMedian = (float[])normalizationMedian.Clone();
            m_NormalizationScale = (float[])normalizationScale.Clone();
            m_ActiveFeatureChannels = (bool[])activeFeatureChannels.Clone();
            m_SearchNodes = (MotionMatchingSearchIndexNodePayload[])searchNodes.Clone();
            m_OrderedSampleIndices = (int[])orderedSampleIndices.Clone();
            m_Coverage = (MotionMatchingCoverageSummaryPayload[])coverage.Clone();
            SearchDomainId = searchDomainId;
            SampleRate = sampleRate;
            Capacities = capacities;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity Identity { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public float SampleRate { get; }
        public MotionMatchingRuntimeCapacityPayload Capacities { get; }
        public int SegmentCount => m_Segments.Length;
        public int SampleCount => m_Samples.Length;
        public int NormalizedFeatureCount => m_NormalizedFeatures.Length;
        public int SearchNodeCount => m_SearchNodes.Length;
        public int OrderedSampleIndexCount => m_OrderedSampleIndices.Length;
        public int CoverageCount => m_Coverage.Length;
        public MotionMatchingSegmentPayload GetSegment(int index) => m_Segments[index];
        public MotionMatchingSamplePayload GetSample(int index) => m_Samples[index];
        public float GetNormalizedFeature(int index) => m_NormalizedFeatures[index];
        public float GetNormalizationMedian(int index) => m_NormalizationMedian[index];
        public float GetNormalizationScale(int index) => m_NormalizationScale[index];
        public bool IsFeatureActive(int index) => m_ActiveFeatureChannels[index];
        public MotionMatchingSearchIndexNodePayload GetSearchNode(int index) => m_SearchNodes[index];
        public int GetOrderedSampleIndex(int index) => m_OrderedSampleIndices[index];
        public MotionMatchingCoverageSummaryPayload GetCoverage(int index) => m_Coverage[index];
    }

    public readonly struct CharacterMotionMatchingArtifactInspection
    {
        public CharacterMotionMatchingArtifactInspection(
            CharacterMotionMatchingArtifactStatus status,
            string path,
            CharacterMotionMatchingDatabaseArtifact artifact,
            string diagnostic)
        {
            Status = status;
            Path = path ?? string.Empty;
            Artifact = artifact;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public CharacterMotionMatchingArtifactStatus Status { get; }
        public string Path { get; }
        public CharacterMotionMatchingDatabaseArtifact Artifact { get; }
        public string Diagnostic { get; }
    }
}
