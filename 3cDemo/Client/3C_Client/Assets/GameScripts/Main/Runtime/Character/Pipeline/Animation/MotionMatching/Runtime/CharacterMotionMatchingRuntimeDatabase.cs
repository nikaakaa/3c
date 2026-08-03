using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingFloatBuffer
    {
        readonly float[] m_Values;

        public MotionMatchingFloatBuffer(float[] values, int offset, int count)
        {
            if (values == null || offset < 0 || count < 0 || offset + count > values.Length)
                throw new ArgumentException("Motion Matching float buffer range is invalid.");
            m_Values = values;
            Offset = offset;
            Count = count;
        }

        public int Offset { get; }
        public int Count { get; }
        public float this[int index] => (uint)index < (uint)Count ? m_Values[Offset + index] : throw new ArgumentOutOfRangeException(nameof(index));
    }

    public readonly struct MotionMatchingExactCostComponents
    {
        public MotionMatchingExactCostComponents(
            float trajectoryPosition,
            float trajectoryFacing,
            float trajectoryVelocity,
            float posePosition,
            float poseVelocity,
            float contactSoft,
            float continuation,
            float jump)
        {
            if (!IsFiniteNonNegative(trajectoryPosition) || !IsFiniteNonNegative(trajectoryFacing) ||
                !IsFiniteNonNegative(trajectoryVelocity) || !IsFiniteNonNegative(posePosition) ||
                !IsFiniteNonNegative(poseVelocity) || !IsFiniteNonNegative(contactSoft) ||
                !IsFiniteNonNegative(continuation) || !IsFiniteNonNegative(jump))
                throw new ArgumentException("Motion Matching exact cost contains an invalid component.");
            float total = trajectoryPosition + trajectoryFacing + trajectoryVelocity + posePosition +
                          poseVelocity + contactSoft + continuation + jump;
            if (!float.IsFinite(total))
                throw new ArgumentException("Motion Matching exact cost total is non-finite.");
            TrajectoryPosition = trajectoryPosition;
            TrajectoryFacing = trajectoryFacing;
            TrajectoryVelocity = trajectoryVelocity;
            PosePosition = posePosition;
            PoseVelocity = poseVelocity;
            ContactSoft = contactSoft;
            Continuation = continuation;
            Jump = jump;
            Total = total;
        }

        public float TrajectoryPosition { get; }
        public float TrajectoryFacing { get; }
        public float TrajectoryVelocity { get; }
        public float PosePosition { get; }
        public float PoseVelocity { get; }
        public float ContactSoft { get; }
        public float Continuation { get; }
        public float Jump { get; }
        public float Total { get; }

        static bool IsFiniteNonNegative(float value) => float.IsFinite(value) && value >= 0f;
    }

    public readonly struct MotionMatchingExactCandidate
    {
        public MotionMatchingExactCandidate(int sampleIndex, CharacterMotionMatchingSampleId sampleId, MotionMatchingExactCostComponents cost)
        {
            if (sampleIndex < 0 || !sampleId.IsValid)
                throw new ArgumentException("Motion Matching exact candidate is invalid.");
            SampleIndex = sampleIndex;
            SampleId = sampleId;
            Cost = cost;
        }

        public int SampleIndex { get; }
        public CharacterMotionMatchingSampleId SampleId { get; }
        public MotionMatchingExactCostComponents Cost { get; }
    }

    public readonly struct MotionMatchingCandidateRejectDetail
    {
        public MotionMatchingCandidateRejectDetail(
            MotionMatchingCandidateRejectReason reason,
            float value,
            float limit,
            float secondaryValue = 0f,
            float secondaryLimit = 0f)
        {
            if (reason == MotionMatchingCandidateRejectReason.None ||
                !float.IsFinite(value) || !float.IsFinite(limit) ||
                !float.IsFinite(secondaryValue) || !float.IsFinite(secondaryLimit))
            {
                throw new ArgumentException("Motion Matching candidate rejection detail is invalid.");
            }
            Reason = reason;
            Value = value;
            Limit = limit;
            SecondaryValue = secondaryValue;
            SecondaryLimit = secondaryLimit;
        }

        public MotionMatchingCandidateRejectReason Reason { get; }
        public float Value { get; }
        public float Limit { get; }
        public float SecondaryValue { get; }
        public float SecondaryLimit { get; }
    }

    public sealed class CharacterMotionMatchingRuntimeDatabase : IDisposable
    {
        readonly MotionMatchingProjectionPayload m_Projection;
        readonly MotionMatchingDatabasePayload m_Database;
        readonly int m_ProjectionDatabaseIndex;
        readonly int[] m_TraversalStack;
        readonly MotionMatchingCandidateRejectDetail[] m_RejectDetails;
        readonly MotionMatchingExactCandidate[] m_TopK;
        readonly int[] m_PlanSamples;
        readonly float[] m_QueryFeatures;
        readonly int[] m_DiagnosticSamples;
        bool m_Disposed;

        public CharacterMotionMatchingRuntimeDatabase(MotionMatchingProjectionPayload projection, int databaseIndex)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            if ((uint)databaseIndex >= (uint)projection.DatabaseCount)
                throw new ArgumentOutOfRangeException(nameof(databaseIndex));
            m_ProjectionDatabaseIndex = databaseIndex;
            m_Database = projection.GetDatabase(databaseIndex) ?? throw new InvalidOperationException("Motion Matching Projection contains a null Database payload.");
            if (!m_Database.ArtifactIdentity.FeatureSchemaId.Equals(projection.FeatureSchema.SchemaId) ||
                m_Database.ArtifactIdentity.FeatureSchemaRevision != projection.FeatureSchema.Revision ||
                !string.Equals(m_Database.ArtifactIdentity.RigId, projection.FeatureSchema.RigId, StringComparison.Ordinal) ||
                !string.Equals(m_Database.ArtifactIdentity.RigRevision, projection.FeatureSchema.RigRevision, StringComparison.Ordinal) ||
                m_Database.Capacities.DenseFeatureCount != projection.FeatureSchema.DenseFeatureCount ||
                projection.CostProfile.DenseFeatureCount != projection.FeatureSchema.DenseFeatureCount ||
                m_Database.Capacities.TopK != projection.SearchPolicy.TopK ||
                m_Database.Capacities.PlanSampleCount != projection.SearchPolicy.PlanSampleCount ||
                m_Database.Capacities.HistoryCapacity != projection.SearchPolicy.HistoryCapacity)
                throw new InvalidOperationException("Motion Matching Runtime Database identity or fixed capacities do not match the Projection.");
            m_TraversalStack = new int[m_Database.Capacities.TraversalCapacity];
            m_RejectDetails = new MotionMatchingCandidateRejectDetail[m_Database.Capacities.SampleCount];
            m_TopK = new MotionMatchingExactCandidate[m_Database.Capacities.TopK];
            m_PlanSamples = new int[m_Database.Capacities.PlanSampleCount * m_Database.Capacities.TopK];
            m_QueryFeatures = new float[m_Database.Capacities.DenseFeatureCount];
            m_DiagnosticSamples = new int[m_Database.Capacities.DiagnosticDetailCapacity];
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity ArtifactIdentity => RequireAlive().m_Database.ArtifactIdentity;
        public int ProjectionDatabaseIndex => RequireAlive().m_ProjectionDatabaseIndex;
        public CharacterMotionMatchingSearchDomainId SearchDomainId => RequireAlive().m_Database.SearchDomainId;
        public MotionMatchingFeatureSchemaPayload FeatureSchema => RequireAlive().m_Projection.FeatureSchema;
        public MotionMatchingCostProfilePayload CostProfile => RequireAlive().m_Projection.CostProfile;
        public MotionMatchingSearchPolicyPayload SearchPolicy => RequireAlive().m_Projection.SearchPolicy;
        public MotionMatchingRuntimeCapacityPayload Capacities => RequireAlive().m_Database.Capacities;
        public float SampleRate => RequireAlive().m_Database.SampleRate;
        public int SampleCount => RequireAlive().m_Database.SampleCount;
        public int SearchNodeCount => RequireAlive().m_Database.SearchNodeCount;
        internal int[] TraversalStack => RequireAlive().m_TraversalStack;
        internal MotionMatchingCandidateRejectDetail[] RejectDetails => RequireAlive().m_RejectDetails;
        internal MotionMatchingExactCandidate[] TopK => RequireAlive().m_TopK;
        internal int[] PlanSamples => RequireAlive().m_PlanSamples;
        internal float[] QueryFeatures => RequireAlive().m_QueryFeatures;
        internal int[] DiagnosticSamples => RequireAlive().m_DiagnosticSamples;

        public MotionMatchingSamplePayload GetSample(int index) => RequireAlive().m_Database.GetSample(index);
        public MotionMatchingSegmentPayload GetSegment(int index) => RequireAlive().m_Database.GetSegment(index);
        public MotionMatchingClipBindingPayload GetClipBinding(int index) => RequireAlive().m_Database.GetClipBinding(index);
        public MotionMatchingSearchIndexNodePayload GetSearchNode(int index) => RequireAlive().m_Database.GetSearchNode(index);
        public int GetOrderedSampleIndex(int index) => RequireAlive().m_Database.GetOrderedSampleIndex(index);
        public float GetNormalizedFeature(int sampleIndex, int featureIndex) => RequireAlive().m_Database.GetNormalizedFeature(sampleIndex, featureIndex);
        public float GetNormalizationScale(int featureIndex) => RequireAlive().m_Database.GetNormalizationScale(featureIndex);
        public bool IsFeatureActive(int featureIndex) => RequireAlive().m_Database.IsFeatureActive(featureIndex);

        public float DenormalizeFeature(int featureIndex, float normalizedValue)
        {
            RequireAlive();
            if ((uint)featureIndex >= (uint)m_Database.Capacities.DenseFeatureCount || !float.IsFinite(normalizedValue))
                throw new ArgumentOutOfRangeException(nameof(featureIndex));
            if (!m_Database.IsFeatureActive(featureIndex))
                return m_Database.GetNormalizationMedian(featureIndex);
            return normalizedValue * m_Database.GetNormalizationScale(featureIndex) + m_Database.GetNormalizationMedian(featureIndex);
        }

        public float NormalizeFeature(int featureIndex, float rawValue)
        {
            RequireAlive();
            if ((uint)featureIndex >= (uint)m_Database.Capacities.DenseFeatureCount || !float.IsFinite(rawValue))
                throw new ArgumentOutOfRangeException(nameof(featureIndex));
            if (!m_Database.IsFeatureActive(featureIndex))
                return 0f;
            return (rawValue - m_Database.GetNormalizationMedian(featureIndex)) / m_Database.GetNormalizationScale(featureIndex);
        }

        public MotionMatchingFloatBuffer NormalizeQuery(MotionMatchingFloatBuffer rawFeatures, bool initialization)
        {
            RequireAlive();
            if (rawFeatures.Count != m_QueryFeatures.Length)
                throw new ArgumentException("Motion Matching query feature count does not match the Runtime Database.", nameof(rawFeatures));
            for (int i = 0; i < m_QueryFeatures.Length; i++)
            {
                if (initialization && !m_Projection.FeatureSchema.IsInitializationFeature(i))
                    m_QueryFeatures[i] = 0f;
                else
                    m_QueryFeatures[i] = NormalizeFeature(i, rawFeatures[i]);
            }
            return new MotionMatchingFloatBuffer(m_QueryFeatures, 0, m_QueryFeatures.Length);
        }

        public void ClearFrameWorkspace()
        {
            RequireAlive();
            Array.Clear(m_TraversalStack, 0, m_TraversalStack.Length);
            Array.Clear(m_RejectDetails, 0, m_RejectDetails.Length);
            Array.Clear(m_TopK, 0, m_TopK.Length);
            Array.Clear(m_PlanSamples, 0, m_PlanSamples.Length);
            Array.Clear(m_DiagnosticSamples, 0, m_DiagnosticSamples.Length);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Array.Clear(m_TraversalStack, 0, m_TraversalStack.Length);
            Array.Clear(m_RejectDetails, 0, m_RejectDetails.Length);
            Array.Clear(m_TopK, 0, m_TopK.Length);
            Array.Clear(m_PlanSamples, 0, m_PlanSamples.Length);
            Array.Clear(m_QueryFeatures, 0, m_QueryFeatures.Length);
            Array.Clear(m_DiagnosticSamples, 0, m_DiagnosticSamples.Length);
            m_Disposed = true;
        }

        CharacterMotionMatchingRuntimeDatabase RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingRuntimeDatabase));
            return this;
        }
    }
}
