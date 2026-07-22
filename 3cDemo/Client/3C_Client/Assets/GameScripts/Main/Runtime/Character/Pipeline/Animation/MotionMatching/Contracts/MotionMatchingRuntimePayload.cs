using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public static class MotionMatchingPoseSourceParameterContract
    {
        public const string FootPlacementWeightName = "animation.foot-placement-weight";
        public static readonly PoseParameterId FootPlacementWeightId = new PoseParameterId(FootPlacementWeightName);
    }

    public sealed class MotionMatchingPoseParameterCurvePayload
    {
        readonly float[] m_NormalizedTimes;
        readonly float[] m_Values;

        public MotionMatchingPoseParameterCurvePayload(PoseParameterId parameterId, float[] normalizedTimes, float[] values)
        {
            if (!parameterId.IsValid || normalizedTimes == null || values == null || normalizedTimes.Length < 2 || normalizedTimes.Length != values.Length)
                throw new ArgumentException("Motion Matching Pose Parameter curve is incomplete.");
            m_NormalizedTimes = (float[])normalizedTimes.Clone();
            m_Values = (float[])values.Clone();
            float previous = -1f;
            for (int i = 0; i < m_NormalizedTimes.Length; i++)
            {
                if (!float.IsFinite(m_NormalizedTimes[i]) || m_NormalizedTimes[i] < 0f || m_NormalizedTimes[i] > 1f || m_NormalizedTimes[i] <= previous ||
                    !float.IsFinite(m_Values[i]) || m_Values[i] < 0f || m_Values[i] > 1f)
                    throw new ArgumentException($"Motion Matching Pose Parameter curve key #{i} is invalid.");
                previous = m_NormalizedTimes[i];
            }
            if (m_NormalizedTimes[0] != 0f || m_NormalizedTimes[m_NormalizedTimes.Length - 1] != 1f)
                throw new ArgumentException("Motion Matching Pose Parameter curve must preserve normalized endpoints.");
            ParameterId = parameterId;
        }

        public PoseParameterId ParameterId { get; }
        public int KeyCount => m_NormalizedTimes.Length;
        public float GetNormalizedTime(int index) => m_NormalizedTimes[index];
        public float GetValue(int index) => m_Values[index];

        public float Sample(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            for (int i = 1; i < m_NormalizedTimes.Length; i++)
            {
                if (time > m_NormalizedTimes[i])
                    continue;
                float alpha = Mathf.InverseLerp(m_NormalizedTimes[i - 1], m_NormalizedTimes[i], time);
                return Mathf.LerpUnclamped(m_Values[i - 1], m_Values[i], alpha);
            }
            return m_Values[m_Values.Length - 1];
        }
    }

    public readonly struct MotionMatchingFeatureRange
    {
        public MotionMatchingFeatureRange(MotionMatchingCostGroup group, int offset, int count)
        {
            if (!Enum.IsDefined(typeof(MotionMatchingCostGroup), group) || offset < 0 || count <= 0)
                throw new ArgumentException("Motion Matching feature range is invalid.");
            Group = group;
            Offset = offset;
            Count = count;
        }

        public MotionMatchingCostGroup Group { get; }
        public int Offset { get; }
        public int Count { get; }
    }

    public sealed class MotionMatchingFeatureSchemaPayload
    {
        readonly float[] m_HistoryHorizons;
        readonly string[] m_BoneIds;
        readonly MotionMatchingFeatureRange[] m_FeatureRanges;
        readonly bool[] m_InitializationFeatureMask;

        public MotionMatchingFeatureSchemaPayload(
            CharacterMotionMatchingFeatureSchemaId schemaId,
            int revision,
            string rigId,
            string rigRevision,
            float[] historyHorizons,
            string[] boneIds,
            MotionMatchingFeatureRange[] featureRanges,
            bool[] initializationFeatureMask,
            int denseFeatureCount)
        {
            if (!schemaId.IsValid || revision <= 0 || string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision) ||
                historyHorizons == null || historyHorizons.Length == 0 || boneIds == null || boneIds.Length == 0 ||
                featureRanges == null || featureRanges.Length == 0 || initializationFeatureMask == null ||
                denseFeatureCount <= 0 || initializationFeatureMask.Length != denseFeatureCount)
                throw new ArgumentException("Compiled Motion Matching Feature Schema is incomplete.");
            m_HistoryHorizons = (float[])historyHorizons.Clone();
            m_BoneIds = (string[])boneIds.Clone();
            m_FeatureRanges = (MotionMatchingFeatureRange[])featureRanges.Clone();
            m_InitializationFeatureMask = (bool[])initializationFeatureMask.Clone();
            float previous = float.NegativeInfinity;
            for (int i = 0; i < m_HistoryHorizons.Length; i++)
            {
                if (!float.IsFinite(m_HistoryHorizons[i]) || m_HistoryHorizons[i] <= previous)
                    throw new ArgumentException("Compiled Motion Matching history horizons are not strictly ordered.", nameof(historyHorizons));
                previous = m_HistoryHorizons[i];
            }
            int expectedOffset = 0;
            for (int i = 0; i < m_FeatureRanges.Length; i++)
            {
                if (m_FeatureRanges[i].Offset != expectedOffset)
                    throw new ArgumentException("Compiled Motion Matching feature ranges are not dense and ordered.", nameof(featureRanges));
                expectedOffset += m_FeatureRanges[i].Count;
            }
            if (expectedOffset != denseFeatureCount)
                throw new ArgumentException("Compiled Motion Matching feature ranges do not cover the dense layout.", nameof(featureRanges));
            for (int i = 0; i < m_BoneIds.Length; i++)
                MotionMatchingIdentity.Require(m_BoneIds[i], nameof(boneIds));
            SchemaId = schemaId;
            Revision = revision;
            RigId = rigId;
            RigRevision = rigRevision;
            DenseFeatureCount = denseFeatureCount;
        }

        public CharacterMotionMatchingFeatureSchemaId SchemaId { get; }
        public int Revision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public int DenseFeatureCount { get; }
        public int HistoryHorizonCount => m_HistoryHorizons.Length;
        public int BoneCount => m_BoneIds.Length;
        public int FeatureRangeCount => m_FeatureRanges.Length;
        public float GetHistoryHorizon(int index) => m_HistoryHorizons[index];
        public string GetBoneId(int index) => m_BoneIds[index];
        public MotionMatchingFeatureRange GetFeatureRange(int index) => m_FeatureRanges[index];
        public bool IsInitializationFeature(int index) => m_InitializationFeatureMask[index];
    }

    public sealed class MotionMatchingCostProfilePayload
    {
        readonly float[] m_DenseFeatureWeights;
        readonly float[] m_GroupWeights;

        public MotionMatchingCostProfilePayload(string profileId, int revision, float[] denseFeatureWeights, float[] groupWeights)
        {
            ProfileId = MotionMatchingIdentity.Require(profileId, nameof(profileId));
            if (revision <= 0 || denseFeatureWeights == null || denseFeatureWeights.Length == 0 ||
                groupWeights == null || groupWeights.Length != Enum.GetValues(typeof(MotionMatchingCostGroup)).Length + 1)
                throw new ArgumentException("Compiled Motion Matching Cost Profile is incomplete.");
            m_DenseFeatureWeights = (float[])denseFeatureWeights.Clone();
            m_GroupWeights = (float[])groupWeights.Clone();
            for (int i = 0; i < m_DenseFeatureWeights.Length; i++)
            {
                if (!float.IsFinite(m_DenseFeatureWeights[i]) || m_DenseFeatureWeights[i] < 0f)
                    throw new ArgumentException("Compiled Motion Matching dense feature weight is invalid.", nameof(denseFeatureWeights));
            }
            for (int i = 1; i < m_GroupWeights.Length; i++)
            {
                if (!float.IsFinite(m_GroupWeights[i]) || m_GroupWeights[i] < 0f)
                    throw new ArgumentException("Compiled Motion Matching group weight is invalid.", nameof(groupWeights));
            }
            Revision = revision;
        }

        public string ProfileId { get; }
        public int Revision { get; }
        public int DenseFeatureCount => m_DenseFeatureWeights.Length;
        public float GetDenseFeatureWeight(int index) => m_DenseFeatureWeights[index];
        public float GetGroupWeight(MotionMatchingCostGroup group) => m_GroupWeights[(int)group];
    }

    public sealed class MotionMatchingSearchPolicyPayload
    {
        public MotionMatchingSearchPolicyPayload(
            string policyId,
            int revision,
            int topK,
            int leafCapacity,
            int planSampleCount,
            float planSampleInterval,
            float searchInterval,
            float minimumJumpInterval,
            int maximumAdmittedSampleCount,
            int maximumTreeDepth,
            float coverageNearDuplicateCostThreshold,
            int historyCapacity,
            int diagnosticDetailCapacity,
            float protectedFootPositionJumpLimit,
            float protectedFootVelocityJumpLimit)
        {
            PolicyId = MotionMatchingIdentity.Require(policyId, nameof(policyId));
            if (revision <= 0 || topK <= 0 || leafCapacity <= 0 || planSampleCount <= 0 ||
                !float.IsFinite(planSampleInterval) || planSampleInterval <= 0f ||
                !float.IsFinite(searchInterval) || searchInterval <= 0f ||
                !float.IsFinite(minimumJumpInterval) || minimumJumpInterval < 0f ||
                maximumAdmittedSampleCount < topK || maximumTreeDepth <= 0 || historyCapacity <= 0 ||
                !float.IsFinite(coverageNearDuplicateCostThreshold) || coverageNearDuplicateCostThreshold <= 0f ||
                diagnosticDetailCapacity < 0 ||
                !float.IsFinite(protectedFootPositionJumpLimit) || protectedFootPositionJumpLimit < 0f ||
                !float.IsFinite(protectedFootVelocityJumpLimit) || protectedFootVelocityJumpLimit < 0f)
                throw new ArgumentException("Compiled Motion Matching Search Policy is invalid.");
            Revision = revision;
            TopK = topK;
            LeafCapacity = leafCapacity;
            PlanSampleCount = planSampleCount;
            PlanSampleInterval = planSampleInterval;
            SearchInterval = searchInterval;
            MinimumJumpInterval = minimumJumpInterval;
            MaximumAdmittedSampleCount = maximumAdmittedSampleCount;
            MaximumTreeDepth = maximumTreeDepth;
            CoverageNearDuplicateCostThreshold = coverageNearDuplicateCostThreshold;
            HistoryCapacity = historyCapacity;
            DiagnosticDetailCapacity = diagnosticDetailCapacity;
            ProtectedFootPositionJumpLimit = protectedFootPositionJumpLimit;
            ProtectedFootVelocityJumpLimit = protectedFootVelocityJumpLimit;
        }

        public string PolicyId { get; }
        public int Revision { get; }
        public int TopK { get; }
        public int LeafCapacity { get; }
        public int PlanSampleCount { get; }
        public float PlanSampleInterval { get; }
        public float SearchInterval { get; }
        public float MinimumJumpInterval { get; }
        public int MaximumAdmittedSampleCount { get; }
        public int MaximumTreeDepth { get; }
        public float CoverageNearDuplicateCostThreshold { get; }
        public int HistoryCapacity { get; }
        public int DiagnosticDetailCapacity { get; }
        public float ProtectedFootPositionJumpLimit { get; }
        public float ProtectedFootVelocityJumpLimit { get; }
    }

    public readonly struct MotionMatchingRuntimeCapacityPayload
    {
        public MotionMatchingRuntimeCapacityPayload(
            int denseFeatureCount,
            int sampleCount,
            int treeNodeCount,
            int traversalCapacity,
            int topK,
            int planSampleCount,
            int historyCapacity,
            int diagnosticDetailCapacity)
        {
            if (denseFeatureCount <= 0 || sampleCount <= 0 || treeNodeCount <= 0 || traversalCapacity <= 0 ||
                topK <= 0 || topK > sampleCount || planSampleCount <= 0 || historyCapacity <= 0 || diagnosticDetailCapacity < 0)
                throw new ArgumentException("Motion Matching runtime capacity payload is invalid.");
            DenseFeatureCount = denseFeatureCount;
            SampleCount = sampleCount;
            TreeNodeCount = treeNodeCount;
            TraversalCapacity = traversalCapacity;
            TopK = topK;
            PlanSampleCount = planSampleCount;
            HistoryCapacity = historyCapacity;
            DiagnosticDetailCapacity = diagnosticDetailCapacity;
        }

        public int DenseFeatureCount { get; }
        public int SampleCount { get; }
        public int TreeNodeCount { get; }
        public int TraversalCapacity { get; }
        public int TopK { get; }
        public int PlanSampleCount { get; }
        public int HistoryCapacity { get; }
        public int DiagnosticDetailCapacity { get; }
    }

    public sealed class MotionMatchingClipBindingPayload
    {
        public MotionMatchingClipBindingPayload(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            AnimationClip clip,
            bool rootLocked,
            MotionMatchingPoseParameterCurvePayload footPlacementWeightCurve)
        {
            if (!sourceClipId.IsValid || !MotionMatchingAuthoringValidation.IsAssetGuid(assetGuid) || localFileId == 0 || !clip || !rootLocked ||
                footPlacementWeightCurve == null || !footPlacementWeightCurve.ParameterId.Equals(MotionMatchingPoseSourceParameterContract.FootPlacementWeightId))
                throw new ArgumentException("Motion Matching Clip binding is invalid.");
            SourceClipId = sourceClipId;
            AssetGuid = assetGuid;
            LocalFileId = localFileId;
            Clip = clip;
            RootLocked = rootLocked;
            FootPlacementWeightCurve = footPlacementWeightCurve;
        }

        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public AnimationClip Clip { get; }
        public bool RootLocked { get; }
        public MotionMatchingPoseParameterCurvePayload FootPlacementWeightCurve { get; }
    }

    public readonly struct MotionMatchingSegmentPayload
    {
        public MotionMatchingSegmentPayload(
            CharacterMotionMatchingSegmentId segmentId,
            CharacterMotionMatchingSourceClipId sourceClipId,
            int firstSampleIndex,
            int sampleCount,
            float startTime,
            float endTime,
            MotionMatchingSegmentLoopMode loopMode,
            bool terminal,
            int continuationEntrySampleIndex)
        {
            if (!segmentId.IsValid || !sourceClipId.IsValid || firstSampleIndex < 0 || sampleCount <= 0 ||
                !float.IsFinite(startTime) || !float.IsFinite(endTime) || startTime < 0f || endTime <= startTime ||
                !Enum.IsDefined(typeof(MotionMatchingSegmentLoopMode), loopMode) || continuationEntrySampleIndex < -1)
                throw new ArgumentException("Motion Matching Segment payload is invalid.");
            if (loopMode == MotionMatchingSegmentLoopMode.Finite && !terminal && continuationEntrySampleIndex < 0)
                throw new ArgumentException("Finite Motion Matching Segment payload has no ending semantics.");
            SegmentId = segmentId;
            SourceClipId = sourceClipId;
            FirstSampleIndex = firstSampleIndex;
            SampleCount = sampleCount;
            StartTime = startTime;
            EndTime = endTime;
            LoopMode = loopMode;
            Terminal = terminal;
            ContinuationEntrySampleIndex = continuationEntrySampleIndex;
        }

        public CharacterMotionMatchingSegmentId SegmentId { get; }
        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public int FirstSampleIndex { get; }
        public int SampleCount { get; }
        public float StartTime { get; }
        public float EndTime { get; }
        public float Duration => EndTime - StartTime;
        public MotionMatchingSegmentLoopMode LoopMode { get; }
        public bool Terminal { get; }
        public int ContinuationEntrySampleIndex { get; }
    }

    public readonly struct MotionMatchingSamplePayload
    {
        public MotionMatchingSamplePayload(
            CharacterMotionMatchingSampleId sampleId,
            CharacterMotionMatchingSegmentId segmentId,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            int clipBindingIndex,
            float sampleTime,
            bool canInitialize,
            bool canJumpInto,
            bool entryExcluded,
            bool exitExcluded,
            bool terminal,
            int nextSampleIndex,
            MotionMatchingFootContactMask contactMask,
            Vector2 rootPlanarVelocity,
            float rootYawVelocityDegrees,
            Vector3 leftFootRootPosition,
            Vector3 rightFootRootPosition,
            AnimationFootFeatureSample leftFoot,
            AnimationFootFeatureSample rightFoot)
        {
            if (!sampleId.IsValid || !segmentId.IsValid || !searchDomainId.IsValid || clipBindingIndex < 0 ||
                !float.IsFinite(sampleTime) || sampleTime < 0f || nextSampleIndex < -1 ||
                (contactMask & ~MotionMatchingFootContactMask.Both) != 0 || !IsFinite(rootPlanarVelocity) ||
                !float.IsFinite(rootYawVelocityDegrees) || !IsFinite(leftFootRootPosition) || !IsFinite(rightFootRootPosition) ||
                !leftFoot.IsValid || !rightFoot.IsValid)
                throw new ArgumentException("Motion Matching Sample payload is invalid.");
            SampleId = sampleId;
            SegmentId = segmentId;
            SearchDomainId = searchDomainId;
            ClipBindingIndex = clipBindingIndex;
            SampleTime = sampleTime;
            CanInitialize = canInitialize;
            CanJumpInto = canJumpInto;
            EntryExcluded = entryExcluded;
            ExitExcluded = exitExcluded;
            Terminal = terminal;
            NextSampleIndex = nextSampleIndex;
            ContactMask = contactMask;
            RootPlanarVelocity = rootPlanarVelocity;
            RootYawVelocityDegrees = rootYawVelocityDegrees;
            LeftFootRootPosition = leftFootRootPosition;
            RightFootRootPosition = rightFootRootPosition;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
        }

        public CharacterMotionMatchingSampleId SampleId { get; }
        public CharacterMotionMatchingSegmentId SegmentId { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public int ClipBindingIndex { get; }
        public float SampleTime { get; }
        public bool CanInitialize { get; }
        public bool CanJumpInto { get; }
        public bool EntryExcluded { get; }
        public bool ExitExcluded { get; }
        public bool Terminal { get; }
        public int NextSampleIndex { get; }
        public MotionMatchingFootContactMask ContactMask { get; }
        public Vector2 RootPlanarVelocity { get; }
        public float RootYawVelocityDegrees { get; }
        public Vector3 LeftFootRootPosition { get; }
        public Vector3 RightFootRootPosition { get; }
        public AnimationFootFeatureSample LeftFoot { get; }
        public AnimationFootFeatureSample RightFoot { get; }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public sealed class MotionMatchingSearchIndexNodePayload
    {
        readonly float[] m_MinBounds;
        readonly float[] m_MaxBounds;

        public MotionMatchingSearchIndexNodePayload(
            CharacterMotionMatchingIndexNodeId nodeId,
            int leftChildIndex,
            int rightChildIndex,
            int orderedSampleOffset,
            int orderedSampleCount,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            MotionMatchingFootContactMask contactMaskUnion,
            float[] minBounds,
            float[] maxBounds)
        {
            if (!nodeId.IsValid || leftChildIndex < -1 || rightChildIndex < -1 || orderedSampleOffset < 0 || orderedSampleCount <= 0 ||
                !searchDomainId.IsValid || (contactMaskUnion & ~MotionMatchingFootContactMask.Both) != 0 ||
                minBounds == null || maxBounds == null || minBounds.Length == 0 || minBounds.Length != maxBounds.Length ||
                (leftChildIndex < 0) != (rightChildIndex < 0))
                throw new ArgumentException("Motion Matching Search Index node is invalid.");
            m_MinBounds = (float[])minBounds.Clone();
            m_MaxBounds = (float[])maxBounds.Clone();
            for (int i = 0; i < m_MinBounds.Length; i++)
            {
                if (!float.IsFinite(m_MinBounds[i]) || !float.IsFinite(m_MaxBounds[i]) || m_MinBounds[i] > m_MaxBounds[i])
                    throw new ArgumentException("Motion Matching Search Index bounds are invalid.");
            }
            NodeId = nodeId;
            LeftChildIndex = leftChildIndex;
            RightChildIndex = rightChildIndex;
            OrderedSampleOffset = orderedSampleOffset;
            OrderedSampleCount = orderedSampleCount;
            SearchDomainId = searchDomainId;
            ContactMaskUnion = contactMaskUnion;
        }

        public CharacterMotionMatchingIndexNodeId NodeId { get; }
        public int LeftChildIndex { get; }
        public int RightChildIndex { get; }
        public int OrderedSampleOffset { get; }
        public int OrderedSampleCount { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public MotionMatchingFootContactMask ContactMaskUnion { get; }
        public int FeatureCount => m_MinBounds.Length;
        public bool IsLeaf => LeftChildIndex < 0;
        public float GetMinimum(int featureIndex) => m_MinBounds[featureIndex];
        public float GetMaximum(int featureIndex) => m_MaxBounds[featureIndex];
    }

    public readonly struct MotionMatchingCoverageSummaryPayload
    {
        public MotionMatchingCoverageSummaryPayload(
            string requirementId,
            bool satisfied,
            int sampleCount,
            float minimumObservedSpeed,
            float maximumObservedSpeed,
            float maximumObservedFacingChange,
            float minimumObservedPlanHorizon)
        {
            RequirementId = MotionMatchingIdentity.Require(requirementId, nameof(requirementId));
            if (sampleCount < 0 || !float.IsFinite(minimumObservedSpeed) || !float.IsFinite(maximumObservedSpeed) ||
                !float.IsFinite(maximumObservedFacingChange) || !float.IsFinite(minimumObservedPlanHorizon))
                throw new ArgumentException("Motion Matching Coverage summary is invalid.");
            Satisfied = satisfied;
            SampleCount = sampleCount;
            MinimumObservedSpeed = minimumObservedSpeed;
            MaximumObservedSpeed = maximumObservedSpeed;
            MaximumObservedFacingChange = maximumObservedFacingChange;
            MinimumObservedPlanHorizon = minimumObservedPlanHorizon;
        }

        public string RequirementId { get; }
        public bool Satisfied { get; }
        public int SampleCount { get; }
        public float MinimumObservedSpeed { get; }
        public float MaximumObservedSpeed { get; }
        public float MaximumObservedFacingChange { get; }
        public float MinimumObservedPlanHorizon { get; }
    }

    public readonly struct MotionMatchingDatabaseCoverageDiagnosticsPayload
    {
        public MotionMatchingDatabaseCoverageDiagnosticsPayload(
            int totalSampleCount,
            int reachableSampleCount,
            int unreachableSampleCount,
            int totalSegmentCount,
            int reachableSegmentCount,
            int unreachableSegmentCount,
            int exactDuplicateSampleCount,
            float exactDuplicateSampleRatio,
            long nearDuplicatePairCount,
            long totalUnorderedNonExactPairCount,
            float nearDuplicatePairRatio,
            int protectedContactEmptyRegionCount,
            int evaluatedNonEmptyRawProtectedContactRegionCount,
            float protectedContactEmptyRegionRatio,
            int maximumAdmittedCandidateSetUpperBound,
            int searchIndexMaximumDepth)
        {
            if (totalSampleCount <= 0 || reachableSampleCount < 0 || unreachableSampleCount < 0 ||
                reachableSampleCount + unreachableSampleCount != totalSampleCount ||
                totalSegmentCount <= 0 || reachableSegmentCount < 0 || unreachableSegmentCount < 0 ||
                reachableSegmentCount + unreachableSegmentCount != totalSegmentCount ||
                exactDuplicateSampleCount < 0 || exactDuplicateSampleCount > totalSampleCount ||
                nearDuplicatePairCount < 0 || totalUnorderedNonExactPairCount < 0 ||
                nearDuplicatePairCount > totalUnorderedNonExactPairCount ||
                protectedContactEmptyRegionCount < 0 || evaluatedNonEmptyRawProtectedContactRegionCount < 0 ||
                protectedContactEmptyRegionCount > evaluatedNonEmptyRawProtectedContactRegionCount ||
                maximumAdmittedCandidateSetUpperBound < 0 || maximumAdmittedCandidateSetUpperBound > totalSampleCount ||
                searchIndexMaximumDepth < 0 ||
                !Ratio(exactDuplicateSampleRatio) || !Ratio(nearDuplicatePairRatio) || !Ratio(protectedContactEmptyRegionRatio) ||
                exactDuplicateSampleRatio != Divide(exactDuplicateSampleCount, totalSampleCount) ||
                nearDuplicatePairRatio != Divide(nearDuplicatePairCount, totalUnorderedNonExactPairCount) ||
                protectedContactEmptyRegionRatio != Divide(protectedContactEmptyRegionCount, evaluatedNonEmptyRawProtectedContactRegionCount))
                throw new ArgumentException("Motion Matching Database coverage diagnostics are inconsistent.");
            TotalSampleCount = totalSampleCount;
            ReachableSampleCount = reachableSampleCount;
            UnreachableSampleCount = unreachableSampleCount;
            TotalSegmentCount = totalSegmentCount;
            ReachableSegmentCount = reachableSegmentCount;
            UnreachableSegmentCount = unreachableSegmentCount;
            ExactDuplicateSampleCount = exactDuplicateSampleCount;
            ExactDuplicateSampleRatio = exactDuplicateSampleRatio;
            NearDuplicatePairCount = nearDuplicatePairCount;
            TotalUnorderedNonExactPairCount = totalUnorderedNonExactPairCount;
            NearDuplicatePairRatio = nearDuplicatePairRatio;
            ProtectedContactEmptyRegionCount = protectedContactEmptyRegionCount;
            EvaluatedNonEmptyRawProtectedContactRegionCount = evaluatedNonEmptyRawProtectedContactRegionCount;
            ProtectedContactEmptyRegionRatio = protectedContactEmptyRegionRatio;
            MaximumAdmittedCandidateSetUpperBound = maximumAdmittedCandidateSetUpperBound;
            SearchIndexMaximumDepth = searchIndexMaximumDepth;
        }

        public int TotalSampleCount { get; }
        public int ReachableSampleCount { get; }
        public int UnreachableSampleCount { get; }
        public int TotalSegmentCount { get; }
        public int ReachableSegmentCount { get; }
        public int UnreachableSegmentCount { get; }
        public int ExactDuplicateSampleCount { get; }
        public float ExactDuplicateSampleRatio { get; }
        public long NearDuplicatePairCount { get; }
        public long TotalUnorderedNonExactPairCount { get; }
        public float NearDuplicatePairRatio { get; }
        public int ProtectedContactEmptyRegionCount { get; }
        public int EvaluatedNonEmptyRawProtectedContactRegionCount { get; }
        public float ProtectedContactEmptyRegionRatio { get; }
        public int MaximumAdmittedCandidateSetUpperBound { get; }
        public int SearchIndexMaximumDepth { get; }

        static bool Ratio(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
        static float Divide(long numerator, long denominator) => denominator == 0 ? 0f : numerator / (float)denominator;
    }
}
