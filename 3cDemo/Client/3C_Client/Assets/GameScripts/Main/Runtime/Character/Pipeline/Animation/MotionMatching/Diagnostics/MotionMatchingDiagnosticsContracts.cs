using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    [Flags]
    public enum MotionMatchingDiagnosticsInterest : uint
    {
        None = 0,
        QuerySummary = 1 << 0,
        TrajectoryEnvelope = 1 << 1,
        PoseHistory = 1 << 2,
        AdmissionAggregate = 1 << 3,
        CandidateRejectDetail = 1 << 4,
        SearchTraversal = 1 << 5,
        TopKCosts = 1 << 6,
        PlanCosts = 1 << 7,
        Selection = 1 << 8,
        PoseSource = 1 << 9,
        Reset = 1 << 10,
        All = uint.MaxValue
    }

    public readonly struct MotionMatchingQuerySummaryTrace
    {
        public MotionMatchingQuerySummaryTrace(
            CharacterMotionMatchingQueryId queryId,
            CharacterMotionMatchingProfileId profileId,
            CharacterMotionMatchingDatabaseId databaseId,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            MotionMatchingTrajectorySourceIdentity trajectorySourceIdentity,
            bool initialization,
            ulong resetSequence)
        {
            if (!queryId.IsValid || !profileId.IsValid || !databaseId.IsValid || !searchDomainId.IsValid || !trajectorySourceIdentity.IsValid)
                throw new ArgumentException("Motion Matching Query summary trace is invalid.");
            QueryId = queryId;
            ProfileId = profileId;
            DatabaseId = databaseId;
            SearchDomainId = searchDomainId;
            TrajectorySourceIdentity = trajectorySourceIdentity;
            Initialization = initialization;
            ResetSequence = resetSequence;
        }

        public CharacterMotionMatchingQueryId QueryId { get; }
        public CharacterMotionMatchingProfileId ProfileId { get; }
        public CharacterMotionMatchingDatabaseId DatabaseId { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public MotionMatchingTrajectorySourceIdentity TrajectorySourceIdentity { get; }
        public bool Initialization { get; }
        public ulong ResetSequence { get; }
    }

    public readonly struct MotionMatchingPoseHistoryTrace
    {
        public MotionMatchingPoseHistoryTrace(int count, int capacity, bool hasGap, float latestPresentationTime)
        {
            if (count < 0 || capacity <= 0 || count > capacity || !float.IsFinite(latestPresentationTime) || latestPresentationTime < 0f)
                throw new ArgumentException("Motion Matching Pose History trace is invalid.");
            Count = count;
            Capacity = capacity;
            HasGap = hasGap;
            LatestPresentationTime = latestPresentationTime;
        }

        public int Count { get; }
        public int Capacity { get; }
        public bool HasGap { get; }
        public float LatestPresentationTime { get; }
    }

    public readonly struct MotionMatchingAdmissionTrace
    {
        public MotionMatchingAdmissionTrace(int admitted, int rejected)
        {
            if (admitted < 0 || rejected < 0)
                throw new ArgumentOutOfRangeException();
            Admitted = admitted;
            Rejected = rejected;
        }

        public int Admitted { get; }
        public int Rejected { get; }
    }

    public readonly struct MotionMatchingCandidateRejectTrace
    {
        public MotionMatchingCandidateRejectTrace(CharacterMotionMatchingSampleId sampleId, MotionMatchingCandidateRejectReason reason)
        {
            if (!sampleId.IsValid || reason == MotionMatchingCandidateRejectReason.None)
                throw new ArgumentException("Motion Matching candidate rejection trace is invalid.");
            SampleId = sampleId;
            Reason = reason;
        }

        public CharacterMotionMatchingSampleId SampleId { get; }
        public MotionMatchingCandidateRejectReason Reason { get; }
    }

    public readonly struct MotionMatchingSearchTraversalTrace
    {
        public MotionMatchingSearchTraversalTrace(int nodesVisited, int nodesPruned, int exactSampleCount)
        {
            if (nodesVisited < 0 || nodesPruned < 0 || exactSampleCount < 0 || nodesPruned > nodesVisited)
                throw new ArgumentOutOfRangeException();
            NodesVisited = nodesVisited;
            NodesPruned = nodesPruned;
            ExactSampleCount = exactSampleCount;
        }

        public int NodesVisited { get; }
        public int NodesPruned { get; }
        public int ExactSampleCount { get; }
    }

    public readonly struct MotionMatchingTopKCostTrace
    {
        public MotionMatchingTopKCostTrace(CharacterMotionMatchingSampleId sampleId, MotionMatchingExactCostComponents cost)
        {
            if (!sampleId.IsValid)
                throw new ArgumentException("Motion Matching Top-K trace Sample identity is invalid.");
            SampleId = sampleId;
            Cost = cost;
        }

        public CharacterMotionMatchingSampleId SampleId { get; }
        public MotionMatchingExactCostComponents Cost { get; }
    }

    public readonly struct MotionMatchingPlanCostTrace
    {
        public MotionMatchingPlanCostTrace(CharacterMotionMatchingPlanId planId, CharacterMotionMatchingSampleId entrySampleId, MotionMatchingPlanCostComponents cost)
        {
            if (!planId.IsValid || !entrySampleId.IsValid)
                throw new ArgumentException("Motion Matching Plan cost trace is invalid.");
            PlanId = planId;
            EntrySampleId = entrySampleId;
            Cost = cost;
        }

        public CharacterMotionMatchingPlanId PlanId { get; }
        public CharacterMotionMatchingSampleId EntrySampleId { get; }
        public MotionMatchingPlanCostComponents Cost { get; }
    }

    public readonly struct MotionMatchingSelectionTrace
    {
        public MotionMatchingSelectionTrace(MotionMatchingSelectionDecision decision)
        {
            Kind = decision.Kind;
            Generation = decision.Generation;
            PlanId = decision.Plan.PlanId;
            SampleIndex = decision.SampleIndex;
            TriggerReason = decision.TriggerReason;
            InvalidReason = decision.InvalidReason;
        }

        public MotionMatchingSelectionDecisionKind Kind { get; }
        public MotionMatchingSelectionGeneration Generation { get; }
        public CharacterMotionMatchingPlanId PlanId { get; }
        public int SampleIndex { get; }
        public MotionMatchingSearchTriggerReason TriggerReason { get; }
        public MotionMatchingInvalidReason InvalidReason { get; }
    }

    public readonly struct MotionMatchingPoseSourceTrace
    {
        public MotionMatchingPoseSourceTrace(MotionMatchingPoseSourceOutput output)
        {
            PlaybackId = output.PlaybackId;
            SelectionGeneration = output.SelectionGeneration;
            RequestSequence = output.PresentationRequestSequence;
            SourceClipId = output.ClipSamplePlan.SourceClipId;
            SampleTime = output.ClipSamplePlan.SampleTime;
            FootPlacementWeightParameterId = output.FootPlacementWeight.ParameterId;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public MotionMatchingSelectionGeneration SelectionGeneration { get; }
        public ulong RequestSequence { get; }
        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public float SampleTime { get; }
        public PoseParameterId FootPlacementWeightParameterId { get; }
    }

    public readonly struct MotionMatchingResetTrace
    {
        public MotionMatchingResetTrace(ulong previousResetSequence, ulong currentResetSequence, MotionMatchingSearchTriggerReason reason)
        {
            if (currentResetSequence == previousResetSequence || reason != MotionMatchingSearchTriggerReason.PresentationReset)
                throw new ArgumentException("Motion Matching Reset trace is invalid.");
            PreviousResetSequence = previousResetSequence;
            CurrentResetSequence = currentResetSequence;
            Reason = reason;
        }

        public ulong PreviousResetSequence { get; }
        public ulong CurrentResetSequence { get; }
        public MotionMatchingSearchTriggerReason Reason { get; }
    }

    public sealed class MotionMatchingRuntimeSnapshot
    {
        readonly MotionMatchingTrajectoryEnvelopePoint[] m_TrajectoryPoints;
        readonly MotionMatchingCandidateRejectTrace[] m_RejectDetails;
        readonly MotionMatchingTopKCostTrace[] m_TopK;

        public MotionMatchingRuntimeSnapshot(int trajectoryCapacity, int rejectDetailCapacity, int topKCapacity)
        {
            if (trajectoryCapacity <= 0 || rejectDetailCapacity < 0 || topKCapacity <= 0)
                throw new ArgumentOutOfRangeException();
            m_TrajectoryPoints = new MotionMatchingTrajectoryEnvelopePoint[trajectoryCapacity];
            m_RejectDetails = new MotionMatchingCandidateRejectTrace[rejectDetailCapacity];
            m_TopK = new MotionMatchingTopKCostTrace[topKCapacity];
        }

        public MotionMatchingDiagnosticsInterest Interest { get; private set; }
        public MotionMatchingQuerySummaryTrace Query { get; private set; }
        public MotionMatchingPoseHistoryTrace PoseHistory { get; private set; }
        public MotionMatchingAdmissionTrace Admission { get; private set; }
        public MotionMatchingSearchTraversalTrace Traversal { get; private set; }
        public MotionMatchingPlanCostTrace Plan { get; private set; }
        public MotionMatchingSelectionTrace Selection { get; private set; }
        public int TrajectoryPointCount { get; private set; }
        public int RejectDetailCount { get; private set; }
        public int TopKCount { get; private set; }
        public MotionMatchingTrajectoryEnvelopePoint GetTrajectoryPoint(int index) => m_TrajectoryPoints[index];
        public MotionMatchingCandidateRejectTrace GetRejectDetail(int index) => m_RejectDetails[index];
        public MotionMatchingTopKCostTrace GetTopK(int index) => m_TopK[index];

        public void Capture(
            MotionMatchingDiagnosticsInterest interest,
            MotionMatchingQuery query,
            CharacterMotionMatchingPoseHistory history,
            CharacterMotionMatchingRuntimeDatabase database,
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult plan,
            MotionMatchingSelectionDecision selection)
        {
            if (history == null || database == null)
                throw new ArgumentNullException(history == null ? nameof(history) : nameof(database));
            Interest = interest;
            Query = (interest & MotionMatchingDiagnosticsInterest.QuerySummary) != 0
                ? new MotionMatchingQuerySummaryTrace(query.QueryId, query.ProfileId, query.DatabaseIdentity.DatabaseId, query.SearchDomainId, query.TrajectorySourceIdentity, query.Initialization, query.ResetSequence)
                : default;
            PoseHistory = (interest & MotionMatchingDiagnosticsInterest.PoseHistory) != 0
                ? new MotionMatchingPoseHistoryTrace(history.Count, history.Capacity, history.HasGap, history.LatestPresentationTime)
                : default;
            Admission = (interest & MotionMatchingDiagnosticsInterest.AdmissionAggregate) != 0
                ? new MotionMatchingAdmissionTrace(search.AdmittedCount, search.RejectedCount)
                : default;
            Traversal = (interest & MotionMatchingDiagnosticsInterest.SearchTraversal) != 0
                ? new MotionMatchingSearchTraversalTrace(search.NodesVisited, search.NodesPruned, search.ExactSampleCount)
                : default;
            Plan = (interest & MotionMatchingDiagnosticsInterest.PlanCosts) != 0 && plan.IsValid
                ? new MotionMatchingPlanCostTrace(plan.Plan.PlanId, plan.Plan.EntrySampleId, plan.Plan.HorizonCost)
                : default;
            Selection = (interest & MotionMatchingDiagnosticsInterest.Selection) != 0
                ? new MotionMatchingSelectionTrace(selection)
                : default;
            TrajectoryPointCount = 0;
            RejectDetailCount = 0;
            TopKCount = 0;
            if ((interest & MotionMatchingDiagnosticsInterest.TrajectoryEnvelope) != 0)
            {
                if (query.TrajectoryEnvelope.Count > m_TrajectoryPoints.Length)
                    throw new InvalidOperationException("Motion Matching diagnostics trajectory capacity is exceeded.");
                for (int i = 0; i < query.TrajectoryEnvelope.Count; i++)
                    m_TrajectoryPoints[TrajectoryPointCount++] = query.TrajectoryEnvelope[i];
            }
            if ((interest & MotionMatchingDiagnosticsInterest.CandidateRejectDetail) != 0)
            {
                for (int i = 0; i < database.SampleCount && RejectDetailCount < m_RejectDetails.Length; i++)
                {
                    MotionMatchingCandidateRejectReason reason = search.GetRejectReason(i);
                    if (reason != MotionMatchingCandidateRejectReason.None)
                        m_RejectDetails[RejectDetailCount++] = new MotionMatchingCandidateRejectTrace(database.GetSample(i).SampleId, reason);
                }
            }
            if ((interest & MotionMatchingDiagnosticsInterest.TopKCosts) != 0)
            {
                if (search.TopKCount > m_TopK.Length)
                    throw new InvalidOperationException("Motion Matching diagnostics Top-K capacity is exceeded.");
                for (int i = 0; i < search.TopKCount; i++)
                {
                    MotionMatchingExactCandidate candidate = search.GetCandidate(i);
                    m_TopK[TopKCount++] = new MotionMatchingTopKCostTrace(candidate.SampleId, candidate.Cost);
                }
            }
        }
    }
}
