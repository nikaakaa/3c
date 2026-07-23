using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingSearchResult
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;

        public MotionMatchingSearchResult(
            CharacterMotionMatchingRuntimeDatabase database,
            int topKCount,
            int admittedCount,
            int rejectedCount,
            int nodesVisited,
            int nodesPruned,
            int exactSampleCount)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            if (topKCount < 0 || topKCount > database.Capacities.TopK || admittedCount < 0 || rejectedCount < 0 ||
                nodesVisited < 0 || nodesPruned < 0 || exactSampleCount < 0)
                throw new ArgumentOutOfRangeException();
            TopKCount = topKCount;
            AdmittedCount = admittedCount;
            RejectedCount = rejectedCount;
            NodesVisited = nodesVisited;
            NodesPruned = nodesPruned;
            ExactSampleCount = exactSampleCount;
        }

        public int TopKCount { get; }
        public int AdmittedCount { get; }
        public int RejectedCount { get; }
        public int NodesVisited { get; }
        public int NodesPruned { get; }
        public int ExactSampleCount { get; }
        public bool IsValid => TopKCount > 0;
        public MotionMatchingExactCandidate GetCandidate(int index) => (uint)index < (uint)TopKCount ? m_Database.TopK[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public MotionMatchingCandidateRejectDetail GetRejectDetail(int sampleIndex) => m_Database.RejectDetails[sampleIndex];
    }

    public sealed class MotionMatchingExactSearch
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;
        readonly MotionMatchingCandidateAdmission m_Admission;
        readonly MotionMatchingExactCostEvaluator m_Cost;

        public MotionMatchingExactSearch(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            m_Admission = new MotionMatchingCandidateAdmission(database);
            m_Cost = new MotionMatchingExactCostEvaluator(database);
        }

        public MotionMatchingSearchResult Search(MotionMatchingQuery query)
        {
            if (!query.DatabaseIdentity.EqualsExact(m_Database.ArtifactIdentity) || !query.SearchDomainId.Equals(m_Database.SearchDomainId))
                throw new InvalidOperationException("Motion Matching Query does not match the Runtime Database.");
            if (query.NormalizedFeatures.Count != m_Database.Capacities.DenseFeatureCount ||
                !query.Initialization && (uint)query.CurrentSampleIndex >= (uint)m_Database.SampleCount)
                throw new InvalidOperationException("Motion Matching Query layout or current sample does not match the Runtime Database.");
            m_Database.ClearFrameWorkspace();
            int[] stack = m_Database.TraversalStack;
            int stackCount = 0;
            stack[stackCount++] = 0;
            int topKCount = 0;
            int admittedCount = 0;
            int rejectedCount = 0;
            int nodesVisited = 0;
            int nodesPruned = 0;
            int exactSampleCount = 0;

            while (stackCount > 0)
            {
                int nodeIndex = stack[--stackCount];
                MotionMatchingSearchIndexNodePayload node = m_Database.GetSearchNode(nodeIndex);
                nodesVisited++;
                if (!node.SearchDomainId.Equals(query.SearchDomainId) ||
                    (node.ContactMaskUnion & query.ContactProtection.ProtectedMask) != query.ContactProtection.ProtectedMask)
                {
                    nodesPruned++;
                    continue;
                }
                float threshold = topKCount < m_Database.Capacities.TopK
                    ? float.PositiveInfinity
                    : m_Database.TopK[topKCount - 1].Cost.Total;
                float lowerBound = ComputeLowerBound(query, node);
                if (lowerBound > threshold)
                {
                    nodesPruned++;
                    continue;
                }
                if (!node.IsLeaf)
                {
                    if (stackCount + 2 > stack.Length)
                        throw new InvalidOperationException("Motion Matching tree traversal exceeded compiled capacity.");
                    stack[stackCount++] = node.RightChildIndex;
                    stack[stackCount++] = node.LeftChildIndex;
                    continue;
                }
                for (int offset = 0; offset < node.OrderedSampleCount; offset++)
                {
                    int sampleIndex = m_Database.GetOrderedSampleIndex(node.OrderedSampleOffset + offset);
                    if (!m_Admission.Admit(query, sampleIndex, out MotionMatchingCandidateRejectDetail rejectDetail))
                    {
                        m_Database.RejectDetails[sampleIndex] = rejectDetail;
                        rejectedCount++;
                        continue;
                    }
                    admittedCount++;
                    MotionMatchingSamplePayload sample = m_Database.GetSample(sampleIndex);
                    MotionMatchingExactCandidate candidate = new MotionMatchingExactCandidate(
                        sampleIndex,
                        sample.SampleId,
                        m_Cost.Evaluate(query, sampleIndex));
                    exactSampleCount++;
                    InsertTopK(candidate, ref topKCount);
                }
            }
            return new MotionMatchingSearchResult(
                m_Database,
                topKCount,
                admittedCount,
                rejectedCount,
                nodesVisited,
                nodesPruned,
                exactSampleCount);
        }

        float ComputeLowerBound(MotionMatchingQuery query, MotionMatchingSearchIndexNodePayload node)
        {
            float lowerBound = 0f;
            for (int rangeIndex = 0; rangeIndex < m_Database.FeatureSchema.FeatureRangeCount; rangeIndex++)
            {
                MotionMatchingFeatureRange range = m_Database.FeatureSchema.GetFeatureRange(rangeIndex);
                if (range.Group == MotionMatchingCostGroup.TrajectoryPosition ||
                    range.Group == MotionMatchingCostGroup.TrajectoryFacing)
                    continue;
                for (int offset = 0; offset < range.Count; offset++)
                {
                    int featureIndex = range.Offset + offset;
                    if (!m_Database.IsFeatureActive(featureIndex) || query.Initialization && !m_Database.FeatureSchema.IsInitializationFeature(featureIndex))
                        continue;
                    float value = query.NormalizedFeatures[featureIndex];
                    float distance = value < node.GetMinimum(featureIndex)
                        ? node.GetMinimum(featureIndex) - value
                        : value > node.GetMaximum(featureIndex)
                            ? value - node.GetMaximum(featureIndex)
                            : 0f;
                    lowerBound += distance * distance *
                        m_Database.CostProfile.GetDenseFeatureWeight(featureIndex) *
                        m_Database.CostProfile.GetGroupWeight(range.Group);
                }
            }
            return lowerBound;
        }

        void InsertTopK(MotionMatchingExactCandidate candidate, ref int count)
        {
            MotionMatchingExactCandidate[] topK = m_Database.TopK;
            int insert = count;
            while (insert > 0 && Compare(candidate, topK[insert - 1]) < 0)
                insert--;
            if (count == topK.Length && insert == count)
                return;
            int last = Math.Min(count, topK.Length - 1);
            for (int i = last; i > insert; i--)
                topK[i] = topK[i - 1];
            topK[insert] = candidate;
            if (count < topK.Length)
                count++;
        }

        static int Compare(MotionMatchingExactCandidate left, MotionMatchingExactCandidate right)
        {
            int cost = left.Cost.Total.CompareTo(right.Cost.Total);
            return cost != 0 ? cost : left.SampleId.CompareTo(right.SampleId);
        }
    }
}
