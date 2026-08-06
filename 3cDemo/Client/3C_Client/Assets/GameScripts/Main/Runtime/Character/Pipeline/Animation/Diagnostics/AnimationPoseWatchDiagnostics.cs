using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public static class AnimationPoseWatchCapacity
    {
        public const int PerWindow = 8;
        public const int PerTarget = 16;
    }

    public enum AnimationPoseWatchAvailability : byte
    {
        Pose = 1,
        NoPose = 2,
        Invalid = 3,
        NotCompleted = 4,
        Stale = 5,
        Targets = 6,
        WorldContextUnavailable = 7
    }

    public readonly struct AnimationPoseWatchIdentity : IEquatable<AnimationPoseWatchIdentity>
    {
        public AnimationPoseWatchIdentity(
            string graphId,
            string graphRevision,
            PoseNodeId nodeId,
            string callSite)
        {
            if (string.IsNullOrWhiteSpace(graphId) || string.IsNullOrWhiteSpace(graphRevision) || !nodeId.IsValid)
                throw new ArgumentException("Pose Watch identity is incomplete.");
            GraphId = graphId.Trim();
            GraphRevision = graphRevision.Trim();
            NodeId = nodeId;
            CallSite = callSite?.Trim() ?? string.Empty;
        }

        public string GraphId { get; }
        public string GraphRevision { get; }
        public PoseNodeId NodeId { get; }
        public string CallSite { get; }
        public bool IsValid => !string.IsNullOrEmpty(GraphId) && !string.IsNullOrEmpty(GraphRevision) && NodeId.IsValid;

        public bool Equals(AnimationPoseWatchIdentity other) =>
            string.Equals(GraphId, other.GraphId, StringComparison.Ordinal) &&
            string.Equals(GraphRevision, other.GraphRevision, StringComparison.Ordinal) &&
            NodeId.Equals(other.NodeId) &&
            string.Equals(CallSite, other.CallSite, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is AnimationPoseWatchIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GraphId, GraphRevision, NodeId, CallSite);
        public override string ToString() => $"{GraphId}@{GraphRevision}/{NodeId}/{(string.IsNullOrEmpty(CallSite) ? "root" : CallSite)}";
    }

    public readonly struct AnimationFullBodyIkGoalSetSnapshot
    {
        internal AnimationFullBodyIkGoalSetSnapshot(
            in CharacterFullBodyIkGoalSetHeader header,
            int copiedGoalOffset)
        {
            FrameSequence = header.FrameSequence;
            CompletionIdentity = header.CompletionIdentity;
            RigId = header.RigId.ToString();
            RigRevision = header.RigRevision.ToString();
            ProducerOperationIndex = header.ProducerOperationIndex;
            ProducerCallSiteIndex = header.ProducerCallSiteIndex;
            GoalOffset = copiedGoalOffset;
            GoalCount = header.GoalCount;
            Availability = header.Availability;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public int ProducerOperationIndex { get; }
        public int ProducerCallSiteIndex { get; }
        internal int GoalOffset { get; }
        public int GoalCount { get; }
        public CharacterFullBodyIkGoalSetAvailability Availability { get; }
        public bool IsValid =>
            FrameSequence != 0 &&
            CompletionIdentity != 0 &&
            !string.IsNullOrEmpty(RigId) &&
            !string.IsNullOrEmpty(RigRevision) &&
            ProducerOperationIndex >= 0 &&
            ProducerCallSiteIndex >= 0 &&
            GoalOffset >= 0 &&
            GoalCount >= 0 &&
            GoalCount <= CharacterFullBodyIkGoalSetHeader.MaximumGoalCount &&
            (Availability == CharacterFullBodyIkGoalSetAvailability.Ready ||
             Availability == CharacterFullBodyIkGoalSetAvailability.WorldContextUnavailable && GoalCount == 0);
    }

    public readonly struct AnimationPoseWatchSnapshot
    {
        internal AnimationPoseWatchSnapshot(
            AnimationPoseWatchIdentity identity,
            int operationIndex,
            CharacterPoseOperationCode operationCode,
            int stageIndex,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace outputPoseSpace,
            AnimationLinkedPoseEntryRuntimeSnapshot linkedPoseEntry,
            AnimationFullBodyIkGoalSetSnapshot goalSet,
            int poseOffset,
            int boneCount,
            int contributionOffset,
            int contributionCount,
            AnimationPoseWatchAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            float outputWeight,
            ulong continuityIdentity,
            ulong completionIdentity)
        {
            Identity = identity;
            OperationIndex = operationIndex;
            OperationCode = operationCode;
            StageIndex = stageIndex;
            ExecutionDomain = executionDomain;
            OutputPoseSpace = outputPoseSpace;
            LinkedPoseEntry = linkedPoseEntry;
            GoalSet = goalSet;
            PoseOffset = poseOffset;
            BoneCount = boneCount;
            ContributionOffset = contributionOffset;
            ContributionCount = contributionCount;
            Availability = availability;
            InvalidReason = invalidReason;
            OutputWeight = outputWeight;
            ContinuityIdentity = continuityIdentity;
            CompletionIdentity = completionIdentity;
        }

        public AnimationPoseWatchIdentity Identity { get; }
        public int OperationIndex { get; }
        public CharacterPoseOperationCode OperationCode { get; }
        public int StageIndex { get; }
        public CharacterPoseExecutionDomain ExecutionDomain { get; }
        public CharacterPoseSpace OutputPoseSpace { get; }
        public AnimationLinkedPoseEntryRuntimeSnapshot LinkedPoseEntry { get; }
        public AnimationFullBodyIkGoalSetSnapshot GoalSet { get; }
        internal int PoseOffset { get; }
        public int BoneCount { get; }
        internal int ContributionOffset { get; }
        public int ContributionCount { get; }
        public AnimationPoseWatchAvailability Availability { get; }
        public AnimationPoseNativeInvalidReason InvalidReason { get; }
        public float OutputWeight { get; }
        public ulong ContinuityIdentity { get; }
        public ulong CompletionIdentity { get; }
    }
}
