using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public static class MotionMatchingContinuationCompiler
    {
        public static MotionMatchingSegmentPayload[] Compile(
            MotionMatchingDatabaseBuildRequest request,
            MotionMatchingSampleBuildRecord[] samples)
        {
            if (request == null || samples == null || samples.Length == 0)
                throw new ArgumentException("Motion Matching continuation inputs are incomplete.");
            var firstBySegment = new Dictionary<CharacterMotionMatchingSegmentId, int>();
            var countBySegment = new Dictionary<CharacterMotionMatchingSegmentId, int>();
            for (int i = 0; i < samples.Length; i++)
            {
                CharacterMotionMatchingSegmentId id = samples[i].SegmentId;
                if (!firstBySegment.ContainsKey(id))
                    firstBySegment.Add(id, i);
                countBySegment[id] = countBySegment.TryGetValue(id, out int count) ? count + 1 : 1;
            }
            var segments = new MotionMatchingSegmentPayload[request.SegmentCount];
            for (int segmentIndex = 0; segmentIndex < request.SegmentCount; segmentIndex++)
            {
                MotionMatchingSegmentBuildInput segment = request.GetSegment(segmentIndex);
                if (!firstBySegment.TryGetValue(segment.SegmentId, out int first) || !countBySegment.TryGetValue(segment.SegmentId, out int count))
                    throw new InvalidOperationException($"Segment '{segment.SegmentId}' has no sampled range.");
                int continuation = -1;
                if (segment.LoopMode == MotionMatchingSegmentLoopMode.Loop)
                    continuation = first;
                else if (!segment.Terminal && !firstBySegment.TryGetValue(segment.ContinuationTarget, out continuation))
                    throw new InvalidOperationException($"Segment '{segment.SegmentId}' continuation target '{segment.ContinuationTarget}' has no sampled entry.");
                for (int ordinal = 0; ordinal < count; ordinal++)
                {
                    int sampleIndex = first + ordinal;
                    samples[sampleIndex].NextSampleIndex = ordinal + 1 < count ? sampleIndex + 1 : continuation;
                }
                segments[segmentIndex] = new MotionMatchingSegmentPayload(
                    segment.SegmentId, segment.SourceClipId, first, count,
                    segment.LoopMode, segment.Terminal, continuation);
            }
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i].NextSampleIndex >= samples.Length)
                    throw new InvalidOperationException($"Sample '{samples[i].SampleId}' has a dangling continuation.");
            }
            return segments;
        }
    }

    public sealed class MotionMatchingNormalizationBuildState
    {
        readonly MotionMatchingSampleBuildRecord[] m_Samples;
        readonly int m_FeatureCount;
        readonly float[] m_Work;
        int m_FeatureIndex;

        public MotionMatchingNormalizationBuildState(MotionMatchingSampleBuildRecord[] samples, int featureCount)
        {
            if (samples == null || samples.Length == 0 || featureCount <= 0)
                throw new ArgumentException("Motion Matching normalization inputs are incomplete.");
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] == null || samples[i].RawFeatures.Length != featureCount)
                    throw new ArgumentException($"Motion Matching sample #{i} raw feature layout is inconsistent.");
            }
            m_Samples = samples;
            m_FeatureCount = featureCount;
            m_Work = new float[samples.Length];
            NormalizedFeatures = new float[samples.Length * featureCount];
            Medians = new float[featureCount];
            Scales = new float[featureCount];
            Active = new bool[featureCount];
        }

        public int CompletedFeatures => m_FeatureIndex;
        public int TotalFeatures => m_FeatureCount;
        public bool IsComplete => m_FeatureIndex == m_FeatureCount;
        public float[] NormalizedFeatures { get; }
        public float[] Medians { get; }
        public float[] Scales { get; }
        public bool[] Active { get; }

        public void Step(int maximumFeatures)
        {
            if (maximumFeatures <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumFeatures));
            int end = Math.Min(m_FeatureCount, m_FeatureIndex + maximumFeatures);
            while (m_FeatureIndex < end)
            {
                CompileFeature(m_FeatureIndex);
                m_FeatureIndex++;
            }
        }

        void CompileFeature(int featureIndex)
        {
            for (int i = 0; i < m_Samples.Length; i++)
                m_Work[i] = m_Samples[i].RawFeatures[featureIndex];
            Array.Sort(m_Work);
            float median = Median(m_Work);
            for (int i = 0; i < m_Samples.Length; i++)
                m_Work[i] = Mathf.Abs(m_Samples[i].RawFeatures[featureIndex] - median);
            Array.Sort(m_Work);
            float scale = Median(m_Work) * 1.4826f;
            bool active = float.IsFinite(scale) && scale > 0.000001f;
            Medians[featureIndex] = median;
            Scales[featureIndex] = active ? scale : 0f;
            Active[featureIndex] = active;
            for (int sampleIndex = 0; sampleIndex < m_Samples.Length; sampleIndex++)
            {
                float value = active ? (m_Samples[sampleIndex].RawFeatures[featureIndex] - median) / scale : 0f;
                if (!float.IsFinite(value))
                    throw new InvalidOperationException($"Motion Matching normalized feature #{featureIndex} is non-finite.");
                NormalizedFeatures[sampleIndex * m_FeatureCount + featureIndex] = value;
            }
        }

        static float Median(float[] sorted)
        {
            int middle = sorted.Length / 2;
            return (sorted.Length & 1) != 0 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) * 0.5f;
        }
    }

    public sealed class MotionMatchingSearchIndexBuildState
    {
        sealed class Node
        {
            public int Offset;
            public int Count;
            public int Depth;
            public int Left = -1;
            public int Right = -1;
            public MotionMatchingFootContactMask ContactUnion;
            public float[] Minimum;
            public float[] Maximum;
        }

        readonly MotionMatchingSampleBuildRecord[] m_Samples;
        readonly float[] m_Normalized;
        readonly bool[] m_Active;
        readonly int m_FeatureCount;
        readonly int m_LeafCapacity;
        readonly int m_MaximumDepth;
        readonly List<Node> m_Nodes = new List<Node>();
        readonly int[] m_Ordered;
        int m_Cursor;
        int m_MaximumObservedDepth;

        public MotionMatchingSearchIndexBuildState(
            MotionMatchingSampleBuildRecord[] samples,
            float[] normalized,
            bool[] active,
            int featureCount,
            int leafCapacity,
            int maximumDepth)
        {
            if (samples == null || samples.Length == 0 || normalized == null || normalized.Length != samples.Length * featureCount ||
                active == null || active.Length != featureCount || featureCount <= 0 || leafCapacity <= 0 || maximumDepth <= 0)
                throw new ArgumentException("Motion Matching Search Index inputs are incomplete.");
            m_Samples = samples;
            m_Normalized = normalized;
            m_Active = active;
            m_FeatureCount = featureCount;
            m_LeafCapacity = leafCapacity;
            m_MaximumDepth = maximumDepth;
            m_Ordered = Enumerable.Range(0, samples.Length).ToArray();
            m_Nodes.Add(new Node { Offset = 0, Count = samples.Length, Depth = 1 });
        }

        public int CompletedNodes => m_Cursor;
        public int DiscoveredNodes => m_Nodes.Count;
        public bool IsComplete => m_Cursor == m_Nodes.Count;
        public int MaximumDepth => m_MaximumObservedDepth;

        public void Step(int maximumNodes)
        {
            if (maximumNodes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumNodes));
            int processed = 0;
            while (m_Cursor < m_Nodes.Count && processed++ < maximumNodes)
            {
                CompileNode(m_Cursor);
                m_Cursor++;
            }
        }

        public int[] GetOrderedSampleIndices()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Motion Matching Search Index is not complete.");
            return (int[])m_Ordered.Clone();
        }

        public MotionMatchingSearchIndexNodePayload[] CreatePayload(CharacterMotionMatchingSearchDomainId domain)
        {
            if (!IsComplete)
                throw new InvalidOperationException("Motion Matching Search Index is not complete.");
            var values = new MotionMatchingSearchIndexNodePayload[m_Nodes.Count];
            for (int i = 0; i < values.Length; i++)
            {
                Node node = m_Nodes[i];
                values[i] = new MotionMatchingSearchIndexNodePayload(
                    new CharacterMotionMatchingIndexNodeId(i + 1), node.Left, node.Right,
                    node.Offset, node.Count, domain, node.ContactUnion, node.Minimum, node.Maximum);
            }
            return values;
        }

        void CompileNode(int nodeIndex)
        {
            Node node = m_Nodes[nodeIndex];
            node.Minimum = new float[m_FeatureCount];
            node.Maximum = new float[m_FeatureCount];
            for (int feature = 0; feature < m_FeatureCount; feature++)
            {
                node.Minimum[feature] = float.PositiveInfinity;
                node.Maximum[feature] = float.NegativeInfinity;
            }
            for (int offset = 0; offset < node.Count; offset++)
            {
                int sampleIndex = m_Ordered[node.Offset + offset];
                node.ContactUnion |= m_Samples[sampleIndex].ContactMask;
                for (int feature = 0; feature < m_FeatureCount; feature++)
                {
                    float value = m_Normalized[sampleIndex * m_FeatureCount + feature];
                    node.Minimum[feature] = Mathf.Min(node.Minimum[feature], value);
                    node.Maximum[feature] = Mathf.Max(node.Maximum[feature], value);
                }
            }
            m_MaximumObservedDepth = Math.Max(m_MaximumObservedDepth, node.Depth);
            if (node.Count <= m_LeafCapacity)
                return;
            if (node.Depth >= m_MaximumDepth)
                throw new InvalidOperationException($"Motion Matching Search Index exceeds maximum tree depth {m_MaximumDepth}.");
            int splitFeature = SelectSplitFeature(node);
            Array.Sort(m_Ordered, node.Offset, node.Count, Comparer<int>.Create((left, right) =>
            {
                int order = m_Normalized[left * m_FeatureCount + splitFeature].CompareTo(m_Normalized[right * m_FeatureCount + splitFeature]);
                return order != 0 ? order : m_Samples[left].SampleId.CompareTo(m_Samples[right].SampleId);
            }));
            int leftCount = node.Count / 2;
            node.Left = m_Nodes.Count;
            m_Nodes.Add(new Node { Offset = node.Offset, Count = leftCount, Depth = node.Depth + 1 });
            node.Right = m_Nodes.Count;
            m_Nodes.Add(new Node { Offset = node.Offset + leftCount, Count = node.Count - leftCount, Depth = node.Depth + 1 });
        }

        int SelectSplitFeature(Node node)
        {
            int selected = 0;
            float largest = float.NegativeInfinity;
            for (int feature = 0; feature < m_FeatureCount; feature++)
            {
                if (!m_Active[feature])
                    continue;
                float span = node.Maximum[feature] - node.Minimum[feature];
                if (span > largest)
                {
                    largest = span;
                    selected = feature;
                }
            }
            return selected;
        }
    }

    public sealed class MotionMatchingCoverageBuildState
    {
        readonly MotionMatchingDatabaseBuildRequest m_Request;
        readonly MotionMatchingSampleBuildRecord[] m_Samples;
        readonly MotionMatchingCoverageSummaryPayload[] m_Summaries;
        int m_Index;

        public MotionMatchingCoverageBuildState(MotionMatchingDatabaseBuildRequest request, MotionMatchingSampleBuildRecord[] samples)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            m_Summaries = new MotionMatchingCoverageSummaryPayload[request.CoverageCount];
        }

        public int CompletedRequirements => m_Index;
        public int TotalRequirements => m_Summaries.Length;
        public bool IsComplete => m_Index == m_Summaries.Length;

        public void Step(int maximumRequirements)
        {
            if (maximumRequirements <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumRequirements));
            int end = Math.Min(m_Summaries.Length, m_Index + maximumRequirements);
            while (m_Index < end)
            {
                m_Summaries[m_Index] = Evaluate(m_Request.GetCoverage(m_Index));
                m_Index++;
            }
        }

        public MotionMatchingCoverageSummaryPayload[] GetSummaries()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Motion Matching Coverage compilation is incomplete.");
            for (int i = 0; i < m_Summaries.Length; i++)
            {
                if (!m_Summaries[i].Satisfied)
                    throw new InvalidOperationException($"Motion Matching Coverage Requirement '{m_Summaries[i].RequirementId}' is missing.");
            }
            return (MotionMatchingCoverageSummaryPayload[])m_Summaries.Clone();
        }

        MotionMatchingCoverageSummaryPayload Evaluate(MotionMatchingCoverageBuildInput requirement)
        {
            float minimumSpeed = float.PositiveInfinity;
            float maximumSpeed = 0f;
            float minimumFacing = float.PositiveInfinity;
            float maximumFacing = 0f;
            float minimumPlan = float.PositiveInfinity;
            int count = 0;
            var contacts = new HashSet<MotionMatchingFootContactMask>();
            for (int i = 0; i < m_Samples.Length; i++)
            {
                MotionMatchingSampleBuildRecord sample = m_Samples[i];
                if (requirement.RequireInitialization && !sample.CanInitialize)
                    continue;
                float plan = MeasurePlanHorizon(i, requirement.MinimumPlanHorizon);
                if (plan + 0.00001f < requirement.MinimumPlanHorizon)
                    continue;
                float speed = sample.RootPlanarVelocity.magnitude;
                float facing = Mathf.Abs(sample.RootYawVelocityDegrees) * requirement.MinimumPlanHorizon;
                minimumSpeed = Mathf.Min(minimumSpeed, speed);
                maximumSpeed = Mathf.Max(maximumSpeed, speed);
                minimumFacing = Mathf.Min(minimumFacing, facing);
                maximumFacing = Mathf.Max(maximumFacing, facing);
                minimumPlan = Mathf.Min(minimumPlan, plan);
                contacts.Add(sample.ContactMask);
                count++;
            }
            bool contactSatisfied = true;
            for (int i = 0; i < requirement.ContactCount; i++)
                contactSatisfied &= contacts.Contains(requirement.GetContact(i));
            bool satisfied = count > 0 && contactSatisfied &&
                             minimumSpeed <= requirement.MinimumSpeed + 0.0001f &&
                             maximumSpeed + 0.0001f >= requirement.MaximumSpeed &&
                             minimumFacing <= requirement.MinimumFacingChangeDegrees + 0.0001f &&
                             maximumFacing + 0.0001f >= requirement.MaximumFacingChangeDegrees;
            if (count == 0)
            {
                minimumSpeed = 0f;
                minimumFacing = 0f;
                minimumPlan = 0f;
            }
            return new MotionMatchingCoverageSummaryPayload(
                requirement.RequirementId, satisfied, count,
                minimumSpeed, maximumSpeed, maximumFacing, minimumPlan);
        }

        float MeasurePlanHorizon(int sampleIndex, float stopAt)
        {
            float elapsed = 0f;
            int current = sampleIndex;
            int maximumSteps = Mathf.CeilToInt(stopAt * m_Request.Database.SampleRate) + m_Samples.Length + 1;
            for (int step = 0; step < maximumSteps && elapsed + 0.00001f < stopAt; step++)
            {
                int next = m_Samples[current].NextSampleIndex;
                if (next < 0)
                    break;
                elapsed += 1f / m_Request.Database.SampleRate;
                current = next;
            }
            return elapsed;
        }
    }

    public static class MotionMatchingDatabaseArtifactFactory
    {
        public static CharacterMotionMatchingDatabaseArtifact Create(
            MotionMatchingDatabaseBuildRequest request,
            MotionMatchingSampleBuildRecord[] sampleRecords,
            MotionMatchingSegmentPayload[] segments,
            MotionMatchingNormalizationBuildState normalization,
            MotionMatchingSearchIndexBuildState index,
            MotionMatchingCoverageSummaryPayload[] coverage)
        {
            if (request == null || sampleRecords == null || segments == null || normalization == null || index == null || coverage == null)
                throw new ArgumentNullException();
            var samples = new MotionMatchingSamplePayload[sampleRecords.Length];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = sampleRecords[i].CreatePayload();
            MotionMatchingSearchIndexNodePayload[] nodes = index.CreatePayload(request.Database.SearchDomainId);
            int traversalCapacity = Math.Max(1, index.MaximumDepth + 1);
            var capacities = new MotionMatchingRuntimeCapacityPayload(
                request.FeatureSchema.DenseFeatureCount, samples.Length, nodes.Length,
                traversalCapacity, Math.Min(request.SearchPolicy.TopK, samples.Length),
                request.SearchPolicy.PlanSampleCount, request.SearchPolicy.HistoryCapacity,
                request.SearchPolicy.DiagnosticDetailCapacity);
            CharacterMotionMatchingDatabaseArtifact preliminary = CreateWithHash(
                request, StableHash.Compute("motion-matching-content-pending"), capacities,
                segments, samples, normalization, nodes, index.GetOrderedSampleIndices(), coverage);
            StableHash contentHash = CharacterMotionMatchingDatabaseArtifactCodec.ComputeContentHash(preliminary);
            return CreateWithHash(
                request, contentHash, capacities, segments, samples, normalization,
                nodes, index.GetOrderedSampleIndices(), coverage);
        }

        static CharacterMotionMatchingDatabaseArtifact CreateWithHash(
            MotionMatchingDatabaseBuildRequest request,
            StableHash contentHash,
            MotionMatchingRuntimeCapacityPayload capacities,
            MotionMatchingSegmentPayload[] segments,
            MotionMatchingSamplePayload[] samples,
            MotionMatchingNormalizationBuildState normalization,
            MotionMatchingSearchIndexNodePayload[] nodes,
            int[] ordered,
            MotionMatchingCoverageSummaryPayload[] coverage)
        {
            CharacterMotionMatchingExpectedArtifactIdentity expected = request.ExpectedIdentity;
            var dependencies = new MotionMatchingClipDependencyIdentity[expected.DependencyCount];
            for (int i = 0; i < dependencies.Length; i++)
                dependencies[i] = expected.GetDependency(i);
            var identity = new CharacterMotionMatchingDatabaseArtifactIdentity(
                expected.ArtifactSchemaVersion, expected.AlgorithmVersion, expected.DatabaseId,
                expected.DatabaseRevision, expected.FeatureSchemaId, expected.FeatureSchemaRevision,
                expected.RigId, expected.RigRevision, dependencies, expected.OrderedDependencyHash, contentHash);
            return new CharacterMotionMatchingDatabaseArtifact(
                identity, request.Database.SearchDomainId, request.Database.SampleRate, capacities,
                segments, samples, normalization.NormalizedFeatures, normalization.Medians,
                normalization.Scales, normalization.Active, nodes, ordered, coverage);
        }
    }
}
