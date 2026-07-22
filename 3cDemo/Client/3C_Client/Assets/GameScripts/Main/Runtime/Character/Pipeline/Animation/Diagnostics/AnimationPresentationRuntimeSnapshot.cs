using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct AnimationBlendStackEntrySnapshot
    {
        internal AnimationBlendStackEntrySnapshot(
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            AnimationBlendEntryId entryId,
            int order,
            int programProducerIndex,
            AnimationBlendTechnique technique,
            int canonicalCurveIndex,
            string canonicalCurveHash,
            int blendProfileIndex,
            string blendProfileId,
            int pushDepth,
            float durationSeconds,
            float elapsedSeconds,
            float rawAlpha,
            float easedAlpha,
            float outputWeight,
            ulong contributionContinuityIdentity)
        {
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            EntryId = entryId;
            Order = order;
            ProgramProducerIndex = programProducerIndex;
            Technique = technique;
            CanonicalCurveIndex = canonicalCurveIndex;
            CanonicalCurveHash = canonicalCurveHash ?? string.Empty;
            BlendProfileIndex = blendProfileIndex;
            BlendProfileId = blendProfileId ?? string.Empty;
            PushDepth = pushDepth;
            DurationSeconds = durationSeconds;
            ElapsedSeconds = elapsedSeconds;
            RawAlpha = rawAlpha;
            EasedAlpha = easedAlpha;
            OutputWeight = outputWeight;
            ContributionContinuityIdentity = contributionContinuityIdentity;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public AnimationBlendEntryId EntryId { get; }
        public int Order { get; }
        public int ProgramProducerIndex { get; }
        public AnimationBlendTechnique Technique { get; }
        public int CanonicalCurveIndex { get; }
        public string CanonicalCurveHash { get; }
        public int BlendProfileIndex { get; }
        public string BlendProfileId { get; }
        public int PushDepth { get; }
        public float DurationSeconds { get; }
        public float ElapsedSeconds { get; }
        public float RawAlpha { get; }
        public float EasedAlpha { get; }
        public float OutputWeight { get; }
        public ulong ContributionContinuityIdentity { get; }
    }

    public readonly struct AnimationBlendStackSnapshot
    {
        internal AnimationBlendStackSnapshot(
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            PoseSlotOutputPolicy outputPolicy,
            int entryOffset,
            int entryCount,
            PoseSlotFrameAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            float outputWeight,
            ulong continuityIdentity,
            ulong completionIdentity,
            bool hasStoredPose,
            bool hasPendingStoredCapture,
            float storedOutputWeight,
            ulong storedContributionIdentity,
            ulong storedCapturedAt,
            ulong storedSourceHistoryCompletedAt,
            bool storedHasFootFeatures,
            AnimationFootFeatureSample storedLeftFootFeatures,
            AnimationFootFeatureSample storedRightFootFeatures,
            bool hasInertialBlend,
            bool hasPendingInertialCapture,
            float inertialOutputWeight,
            ulong inertialContributionIdentity,
            ulong inertialCapturedAt,
            ulong inertialSourceHistoryCompletedAt,
            bool inertialHasFootFeatures,
            AnimationFootFeatureSample inertialLeftFootFeatures,
            AnimationFootFeatureSample inertialRightFootFeatures)
        {
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            OutputPolicy = outputPolicy;
            EntryOffset = entryOffset;
            EntryCount = entryCount;
            Availability = availability;
            InvalidReason = invalidReason;
            OutputWeight = outputWeight;
            ContinuityIdentity = continuityIdentity;
            CompletionIdentity = completionIdentity;
            HasStoredPose = hasStoredPose;
            HasPendingStoredCapture = hasPendingStoredCapture;
            StoredOutputWeight = storedOutputWeight;
            StoredContributionIdentity = storedContributionIdentity;
            StoredCapturedAt = storedCapturedAt;
            StoredSourceHistoryCompletedAt = storedSourceHistoryCompletedAt;
            StoredHasFootFeatures = storedHasFootFeatures;
            StoredLeftFootFeatures = storedLeftFootFeatures;
            StoredRightFootFeatures = storedRightFootFeatures;
            HasInertialBlend = hasInertialBlend;
            HasPendingInertialCapture = hasPendingInertialCapture;
            InertialOutputWeight = inertialOutputWeight;
            InertialContributionIdentity = inertialContributionIdentity;
            InertialCapturedAt = inertialCapturedAt;
            InertialSourceHistoryCompletedAt = inertialSourceHistoryCompletedAt;
            InertialHasFootFeatures = inertialHasFootFeatures;
            InertialLeftFootFeatures = inertialLeftFootFeatures;
            InertialRightFootFeatures = inertialRightFootFeatures;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public PoseSlotOutputPolicy OutputPolicy { get; }
        public int EntryOffset { get; }
        public int EntryCount { get; }
        public PoseSlotFrameAvailability Availability { get; }
        public AnimationPoseNativeInvalidReason InvalidReason { get; }
        public float OutputWeight { get; }
        public ulong ContinuityIdentity { get; }
        public ulong CompletionIdentity { get; }
        public bool HasStoredPose { get; }
        public bool HasPendingStoredCapture { get; }
        public float StoredOutputWeight { get; }
        public ulong StoredContributionIdentity { get; }
        public ulong StoredCapturedAt { get; }
        public ulong StoredSourceHistoryCompletedAt { get; }
        public bool StoredHasFootFeatures { get; }
        public AnimationFootFeatureSample StoredLeftFootFeatures { get; }
        public AnimationFootFeatureSample StoredRightFootFeatures { get; }
        public bool HasInertialBlend { get; }
        public bool HasPendingInertialCapture { get; }
        public float InertialOutputWeight { get; }
        public ulong InertialContributionIdentity { get; }
        public ulong InertialCapturedAt { get; }
        public ulong InertialSourceHistoryCompletedAt { get; }
        public bool InertialHasFootFeatures { get; }
        public AnimationFootFeatureSample InertialLeftFootFeatures { get; }
        public AnimationFootFeatureSample InertialRightFootFeatures { get; }
    }

    public readonly struct AnimationPoseOperationSnapshot
    {
        internal AnimationPoseOperationSnapshot(
            int operationIndex,
            string graphId,
            PoseNodeId nodeId,
            string callSite,
            CharacterPoseOperationCode code,
            PoseSlotFrameAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            float outputWeight,
            ulong continuityIdentity,
            ulong completionIdentity,
            int contributionOffset,
            int contributionCount)
        {
            OperationIndex = operationIndex;
            GraphId = graphId ?? string.Empty;
            NodeId = nodeId;
            CallSite = callSite ?? string.Empty;
            Code = code;
            Availability = availability;
            InvalidReason = invalidReason;
            OutputWeight = outputWeight;
            ContinuityIdentity = continuityIdentity;
            CompletionIdentity = completionIdentity;
            ContributionOffset = contributionOffset;
            ContributionCount = contributionCount;
        }

        public int OperationIndex { get; }
        public string GraphId { get; }
        public PoseNodeId NodeId { get; }
        public string CallSite { get; }
        public CharacterPoseOperationCode Code { get; }
        public PoseSlotFrameAvailability Availability { get; }
        public AnimationPoseNativeInvalidReason InvalidReason { get; }
        public float OutputWeight { get; }
        public ulong ContinuityIdentity { get; }
        public ulong CompletionIdentity { get; }
        public int ContributionOffset { get; }
        public int ContributionCount { get; }
    }

    public readonly struct AnimationPoseOperationTrace
    {
        readonly AnimationPresentationRuntimeSnapshot m_Snapshot;

        internal AnimationPoseOperationTrace(
            in AnimationPresentationRuntimeSnapshot snapshot,
            AnimationPoseOperationSnapshot operation)
        {
            m_Snapshot = snapshot;
            Operation = operation;
        }

        public string ProjectionRevision => m_Snapshot.ProjectionRevision;
        public string PoseGraphId => m_Snapshot.PoseGraphId;
        public string PoseGraphRevision => m_Snapshot.PoseGraphRevision;
        public string PoseProgramHash => m_Snapshot.PoseProgramHash;
        public ulong CompletionIdentity => m_Snapshot.CompletionIdentity;
        public PoseSlotFrameAvailability FinalAvailability => m_Snapshot.FinalAvailability;
        public AnimationPoseNativeInvalidReason FinalInvalidReason => m_Snapshot.FinalInvalidReason;
        public ulong FinalAppliedAt => m_Snapshot.FinalAppliedAt;
        public ulong FinalContinuityIdentity => m_Snapshot.ContinuityIdentity;
        public AnimationPoseOperationSnapshot Operation { get; }
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> Contributions =>
            m_Snapshot.GetOperationContributions(Operation);

        public float GetContributionBoneWeight(int contributionIndex, int boneIndex) =>
            m_Snapshot.GetOperationContributionBoneWeight(Operation, contributionIndex, boneIndex);
    }

    public readonly struct AnimationPoseParameterSnapshot
    {
        internal AnimationPoseParameterSnapshot(PoseParameterId parameterId, float value)
        {
            ParameterId = parameterId;
            Value = value;
        }

        public PoseParameterId ParameterId { get; }
        public float Value { get; }
    }

    public readonly struct AnimationReleasedPoseSourceSnapshot
    {
        internal AnimationReleasedPoseSourceSnapshot(PoseSlotId poseSlotId, AnimationPoseSourceId sourceId, ulong completionIdentity)
        {
            PoseSlotId = poseSlotId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        public PoseSlotId PoseSlotId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong CompletionIdentity { get; }
    }

    public readonly struct AnimationPresentationRuntimeSnapshot
    {
        readonly FinalAnimationPoseFramePageLease m_Lease;
        readonly ulong m_LeaseIdentity;
        readonly AnimationBlendStackSnapshot[] m_Stacks;
        readonly AnimationBlendStackEntrySnapshot[] m_Entries;
        readonly AnimationPlaybackLifecycleSnapshot[] m_Lifecycle;
        readonly AnimationPoseOperationSnapshot[] m_Operations;
        readonly AnimationPoseParameterSnapshot[] m_Parameters;
        readonly AnimationPoseSourceContribution[] m_SlotContributions;
        readonly AnimationPoseSourceContribution[] m_OperationContributions;
        readonly AnimationPoseSourceContribution[] m_FinalContributions;
        readonly AnimationReleasedPoseSourceSnapshot[] m_Releases;
        readonly AnimationBoneId[] m_BoneIds;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_StoredBoneWeights;
        readonly float[] m_InertialBoneWeights;
        readonly float[] m_SlotContributionBoneWeights;
        readonly float[] m_OperationContributionBoneWeights;
        readonly float[] m_FinalContributionBoneWeights;
        readonly int m_StackCount;
        readonly int m_EntryCount;
        readonly int m_LifecycleCount;
        readonly int m_OperationCount;
        readonly int m_ParameterCount;
        readonly int m_SlotContributionCount;
        readonly int m_OperationContributionCount;
        readonly int m_FinalContributionCount;
        readonly int m_ReleaseCount;

        internal AnimationPresentationRuntimeSnapshot(
            string projectionRevision,
            string rigId,
            string rigRevision,
            string poseGraphId,
            string poseGraphRevision,
            string poseProgramHash,
            ulong completionIdentity,
            PoseSlotFrameAvailability finalAvailability,
            AnimationPoseNativeInvalidReason finalInvalidReason,
            int invalidOperationIndex,
            ulong poseGraphCompletedAt,
            ulong finalAppliedAt,
            ulong continuityIdentity,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            FinalAnimationPoseFramePageLease lease,
            ulong leaseIdentity,
            AnimationBlendStackSnapshot[] stacks,
            int stackCount,
            AnimationBlendStackEntrySnapshot[] entries,
            int entryCount,
            AnimationPlaybackLifecycleSnapshot[] lifecycle,
            int lifecycleCount,
            AnimationPoseOperationSnapshot[] operations,
            int operationCount,
            AnimationPoseParameterSnapshot[] parameters,
            int parameterCount,
            AnimationPoseSourceContribution[] slotContributions,
            int slotContributionCount,
            AnimationPoseSourceContribution[] operationContributions,
            int operationContributionCount,
            AnimationPoseSourceContribution[] finalContributions,
            int finalContributionCount,
            AnimationReleasedPoseSourceSnapshot[] releases,
            int releaseCount,
            AnimationBoneId[] boneIds,
            float[] entryBoneWeights,
            float[] storedBoneWeights,
            float[] inertialBoneWeights,
            float[] slotContributionBoneWeights,
            float[] operationContributionBoneWeights,
            float[] finalContributionBoneWeights)
        {
            ProjectionRevision = projectionRevision ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            PoseGraphId = poseGraphId ?? string.Empty;
            PoseGraphRevision = poseGraphRevision ?? string.Empty;
            PoseProgramHash = poseProgramHash ?? string.Empty;
            CompletionIdentity = completionIdentity;
            FinalAvailability = finalAvailability;
            FinalInvalidReason = finalInvalidReason;
            InvalidOperationIndex = invalidOperationIndex;
            PoseGraphCompletedAt = poseGraphCompletedAt;
            FinalAppliedAt = finalAppliedAt;
            ContinuityIdentity = continuityIdentity;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            m_Lease = lease ?? throw new ArgumentNullException(nameof(lease));
            m_LeaseIdentity = leaseIdentity;
            m_Stacks = stacks;
            m_StackCount = stackCount;
            m_Entries = entries;
            m_EntryCount = entryCount;
            m_Lifecycle = lifecycle;
            m_LifecycleCount = lifecycleCount;
            m_Operations = operations;
            m_OperationCount = operationCount;
            m_Parameters = parameters;
            m_ParameterCount = parameterCount;
            m_SlotContributions = slotContributions;
            m_SlotContributionCount = slotContributionCount;
            m_OperationContributions = operationContributions;
            m_OperationContributionCount = operationContributionCount;
            m_FinalContributions = finalContributions;
            m_FinalContributionCount = finalContributionCount;
            m_Releases = releases;
            m_ReleaseCount = releaseCount;
            m_BoneIds = boneIds;
            m_EntryBoneWeights = entryBoneWeights;
            m_StoredBoneWeights = storedBoneWeights;
            m_InertialBoneWeights = inertialBoneWeights;
            m_SlotContributionBoneWeights = slotContributionBoneWeights;
            m_OperationContributionBoneWeights = operationContributionBoneWeights;
            m_FinalContributionBoneWeights = finalContributionBoneWeights;
            m_Lease.RequireValid(m_LeaseIdentity);
        }

        public string ProjectionRevision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public string PoseGraphId { get; }
        public string PoseGraphRevision { get; }
        public string PoseProgramHash { get; }
        public ulong CompletionIdentity { get; }
        public PoseSlotFrameAvailability FinalAvailability { get; }
        public AnimationPoseNativeInvalidReason FinalInvalidReason { get; }
        public int InvalidOperationIndex { get; }
        public ulong PoseGraphCompletedAt { get; }
        public ulong FinalAppliedAt { get; }
        public ulong ContinuityIdentity { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public bool StackWeightsArePrePoseGraphMask => true;
        public AnimationReadOnlyBuffer<AnimationBlendStackSnapshot> Stacks => Buffer(m_Stacks, m_StackCount);
        public AnimationReadOnlyBuffer<AnimationBlendStackEntrySnapshot> Entries => Buffer(m_Entries, m_EntryCount);
        public AnimationReadOnlyBuffer<AnimationPlaybackLifecycleSnapshot> Lifecycle => Buffer(m_Lifecycle, m_LifecycleCount);
        public AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> Operations => Buffer(m_Operations, m_OperationCount);
        public AnimationReadOnlyBuffer<AnimationPoseParameterSnapshot> Parameters => Buffer(m_Parameters, m_ParameterCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> SlotContributions => Buffer(m_SlotContributions, m_SlotContributionCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> OperationContributions =>
            Buffer(m_OperationContributions, m_OperationContributionCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> FinalContributions => Buffer(m_FinalContributions, m_FinalContributionCount);
        public AnimationReadOnlyBuffer<AnimationReleasedPoseSourceSnapshot> Releases => Buffer(m_Releases, m_ReleaseCount);
        public AnimationReadOnlyBuffer<AnimationBoneId> BoneIds => Buffer(m_BoneIds, m_BoneIds.Length);

        public float GetEntryBoneWeight(int entryIndex, int boneIndex) =>
            GetWeight(m_EntryBoneWeights, m_EntryCount, entryIndex, boneIndex);
        public float GetStoredBoneWeight(int stackIndex, int boneIndex) =>
            GetWeight(m_StoredBoneWeights, m_StackCount, stackIndex, boneIndex);
        public float GetInertialBoneWeight(int stackIndex, int boneIndex) =>
            GetWeight(m_InertialBoneWeights, m_StackCount, stackIndex, boneIndex);
        public float GetSlotContributionBoneWeight(int contributionIndex, int boneIndex) =>
            GetWeight(m_SlotContributionBoneWeights, m_SlotContributionCount, contributionIndex, boneIndex);
        public float GetOperationContributionBoneWeight(int contributionIndex, int boneIndex) =>
            GetWeight(m_OperationContributionBoneWeights, m_OperationContributionCount, contributionIndex, boneIndex);
        public float GetFinalContributionBoneWeight(int contributionIndex, int boneIndex) =>
            GetWeight(m_FinalContributionBoneWeights, m_FinalContributionCount, contributionIndex, boneIndex);

        public int GetOperationMatchCount(string graphId, PoseNodeId nodeId)
        {
            RequireOperationQuery(graphId, nodeId);
            int count = 0;
            for (int i = 0; i < m_OperationCount; i++)
            {
                if (Matches(m_Operations[i], graphId, nodeId))
                    count++;
            }
            return count;
        }

        public bool TryGetOperationTrace(
            string graphId,
            PoseNodeId nodeId,
            int occurrence,
            out AnimationPoseOperationTrace trace)
        {
            RequireOperationQuery(graphId, nodeId);
            if (occurrence < 0)
                throw new ArgumentOutOfRangeException(nameof(occurrence));
            int match = 0;
            for (int i = 0; i < m_OperationCount; i++)
            {
                AnimationPoseOperationSnapshot operation = m_Operations[i];
                if (!Matches(operation, graphId, nodeId))
                    continue;
                if (match++ != occurrence)
                    continue;
                trace = new AnimationPoseOperationTrace(this, operation);
                return true;
            }
            trace = default;
            return false;
        }

        AnimationReadOnlyBuffer<T> Buffer<T>(T[] values, int count)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            return new AnimationReadOnlyBuffer<T>(values, 0, count, m_Lease, m_LeaseIdentity);
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceContribution> GetOperationContributions(
            AnimationPoseOperationSnapshot operation)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            if (operation.ContributionOffset < 0 || operation.ContributionCount < 0 ||
                operation.ContributionOffset > m_OperationContributionCount - operation.ContributionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }
            return new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                m_OperationContributions,
                operation.ContributionOffset,
                operation.ContributionCount,
                m_Lease,
                m_LeaseIdentity);
        }

        internal float GetOperationContributionBoneWeight(
            AnimationPoseOperationSnapshot operation,
            int contributionIndex,
            int boneIndex)
        {
            if ((uint)contributionIndex >= (uint)operation.ContributionCount)
                throw new ArgumentOutOfRangeException(nameof(contributionIndex));
            return GetWeight(
                m_OperationContributionBoneWeights,
                m_OperationContributionCount,
                operation.ContributionOffset + contributionIndex,
                boneIndex);
        }

        void RequireOperationQuery(string graphId, PoseNodeId nodeId)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            if (string.IsNullOrWhiteSpace(graphId) || !nodeId.IsValid)
                throw new ArgumentException("Animation Pose operation query identity is invalid.");
        }

        static bool Matches(AnimationPoseOperationSnapshot operation, string graphId, PoseNodeId nodeId) =>
            string.Equals(operation.GraphId, graphId, StringComparison.Ordinal) && operation.NodeId.Equals(nodeId);

        float GetWeight(float[] weights, int rowCount, int row, int boneIndex)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            if ((uint)boneIndex >= (uint)m_BoneIds.Length || (uint)row >= (uint)rowCount)
                throw new ArgumentOutOfRangeException();
            int index = checked(row * m_BoneIds.Length + boneIndex);
            if ((uint)index >= (uint)weights.Length)
                throw new ArgumentOutOfRangeException();
            return weights[index];
        }
    }
}
