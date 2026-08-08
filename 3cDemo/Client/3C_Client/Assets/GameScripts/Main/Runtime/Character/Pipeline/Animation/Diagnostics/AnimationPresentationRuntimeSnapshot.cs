using System;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct AnimationPoseBoneSnapshot
    {
        internal AnimationPoseBoneSnapshot(
            AnimationBoneId boneId,
            CharacterPoseBoneKind kind,
            int parentPoseBoneIndex,
            AnimationBoneId sourcePhysicalBoneId,
            AnimationBoneId targetPhysicalBoneId)
        {
            BoneId = boneId;
            Kind = kind;
            ParentPoseBoneIndex = parentPoseBoneIndex;
            SourcePhysicalBoneId = sourcePhysicalBoneId;
            TargetPhysicalBoneId = targetPhysicalBoneId;
        }

        public AnimationBoneId BoneId { get; }
        public CharacterPoseBoneKind Kind { get; }
        public int ParentPoseBoneIndex { get; }
        public AnimationBoneId SourcePhysicalBoneId { get; }
        public AnimationBoneId TargetPhysicalBoneId { get; }
        public bool IsVirtual => Kind == CharacterPoseBoneKind.Virtual;
    }

    public readonly struct AnimationBlendStackEntrySnapshot
    {
        internal AnimationBlendStackEntrySnapshot(
            AnimationChannelId animationChannelId,
            PresentationPoseSourceProviderId presentationPoseSourceProviderId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            PoseNodeId poseNodeId,
            AnimationBlendEntryId entryId,
            int order,
            int sourceOwnerIndex,
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
            PresentationPoseSourceProviderId = presentationPoseSourceProviderId;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            PoseNodeId = poseNodeId;
            EntryId = entryId;
            Order = order;
            SourceOwnerIndex = sourceOwnerIndex;
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
        public PresentationPoseSourceProviderId PresentationPoseSourceProviderId { get; }
        public PresentationPoseSourceIndex PresentationPoseSourceIndex { get; }
        public PoseNodeId PoseNodeId { get; }
        public AnimationBlendEntryId EntryId { get; }
        public int Order { get; }
        public int SourceOwnerIndex { get; }
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
            PresentationPoseSourceProviderId presentationPoseSourceProviderId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            PoseNodeId poseNodeId,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            int entryOffset,
            int entryCount,
            AnimationPoseAvailability availability,
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
            AnimationFootFeatureSample storedRightFootFeatures)
        {
            AnimationChannelId = animationChannelId;
            PresentationPoseSourceProviderId = presentationPoseSourceProviderId;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            PoseNodeId = poseNodeId;
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
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PresentationPoseSourceProviderId PresentationPoseSourceProviderId { get; }
        public PresentationPoseSourceIndex PresentationPoseSourceIndex { get; }
        public PoseNodeId PoseNodeId { get; }
        public AnimationSelectionAvailabilityPolicy OutputPolicy { get; }
        public int EntryOffset { get; }
        public int EntryCount { get; }
        public AnimationPoseAvailability Availability { get; }
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
    }

    public readonly struct PoseInertializationSnapshot
    {
        internal PoseInertializationSnapshot(
            PoseNodeId nodeId,
            PoseInertializationTemporalOwnerKind temporalOwnerKind,
            PoseNodeId inputOwnerNodeId,
            int inputOwnerIndex,
            PoseInertializationRuntimeState state,
            ulong eventIdentity,
            PoseDiscontinuityReason reason,
            PoseDiscontinuityResetReason resetReason,
            ulong resetSequence,
            string policyId,
            string policyRevision,
            int sourceEndpointIndex,
            int targetEndpointIndex,
            int curveIndex,
            int profileIndex,
            PoseDiscontinuityEndpoint previousEndpoint,
            PoseDiscontinuityEndpoint currentEndpoint,
            ulong previousContinuityIdentity,
            ulong currentContinuityIdentity,
            PoseInertializationMode ruleMode,
            float elapsedSeconds,
            float durationSeconds,
            ulong accumulatorGeneration,
            ulong historyCompletionIdentity,
            ulong outputCompletionIdentity)
        {
            NodeId = nodeId;
            TemporalOwnerKind = temporalOwnerKind;
            InputOwnerNodeId = inputOwnerNodeId;
            InputOwnerIndex = inputOwnerIndex;
            State = state;
            EventIdentity = eventIdentity;
            Reason = reason;
            ResetReason = resetReason;
            ResetSequence = resetSequence;
            PolicyId = policyId ?? string.Empty;
            PolicyRevision = policyRevision ?? string.Empty;
            SourceEndpointIndex = sourceEndpointIndex;
            TargetEndpointIndex = targetEndpointIndex;
            CurveIndex = curveIndex;
            ProfileIndex = profileIndex;
            PreviousEndpoint = previousEndpoint;
            CurrentEndpoint = currentEndpoint;
            PreviousContinuityIdentity = previousContinuityIdentity;
            CurrentContinuityIdentity = currentContinuityIdentity;
            RuleMode = ruleMode;
            ElapsedSeconds = elapsedSeconds;
            DurationSeconds = durationSeconds;
            AccumulatorGeneration = accumulatorGeneration;
            HistoryCompletionIdentity = historyCompletionIdentity;
            OutputCompletionIdentity = outputCompletionIdentity;
        }

        public PoseNodeId NodeId { get; }
        public PoseInertializationTemporalOwnerKind TemporalOwnerKind { get; }
        public PoseNodeId InputOwnerNodeId { get; }
        public int InputOwnerIndex { get; }
        public PoseInertializationRuntimeState State { get; }
        public ulong EventIdentity { get; }
        public PoseDiscontinuityReason Reason { get; }
        public PoseDiscontinuityResetReason ResetReason { get; }
        public ulong ResetSequence { get; }
        public string PolicyId { get; }
        public string PolicyRevision { get; }
        public int SourceEndpointIndex { get; }
        public int TargetEndpointIndex { get; }
        public int CurveIndex { get; }
        public int ProfileIndex { get; }
        public PoseDiscontinuityEndpoint PreviousEndpoint { get; }
        public PoseDiscontinuityEndpoint CurrentEndpoint { get; }
        public ulong PreviousContinuityIdentity { get; }
        public ulong CurrentContinuityIdentity { get; }
        public PoseInertializationMode RuleMode { get; }
        public float ElapsedSeconds { get; }
        public float DurationSeconds { get; }
        public ulong AccumulatorGeneration { get; }
        public ulong HistoryCompletionIdentity { get; }
        public ulong OutputCompletionIdentity { get; }
    }

    public readonly struct AnimationPoseOperationSnapshot
    {
        internal AnimationPoseOperationSnapshot(
            int operationIndex,
            string graphId,
            PoseNodeId nodeId,
            string callSite,
            CharacterPoseOperationCode code,
            AnimationPoseAvailability availability,
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
        public AnimationPoseAvailability Availability { get; }
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
        public string PosePlanHash => m_Snapshot.PosePlanHash;
        public ulong CompletionIdentity => m_Snapshot.CompletionIdentity;
        public AnimationPoseAvailability FinalAvailability => m_Snapshot.FinalAvailability;
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
        internal AnimationPoseParameterSnapshot(PoseParameterId parameterId, float value, bool available)
        {
            ParameterId = parameterId;
            Value = value;
            Available = available;
        }

        public PoseParameterId ParameterId { get; }
        public float Value { get; }
        public bool Available { get; }
    }

    public readonly struct AnimationBlendSpaceSampleRuntimeSnapshot
    {
        internal AnimationBlendSpaceSampleRuntimeSnapshot(
            CharacterAnimationBlendSpaceSampleId sampleId,
            float weight,
            float clipTime,
            float normalizedTime,
            bool hasFootFeatures,
            string footAnalysisSourceId,
            int footAnalysisVersion,
            string footArtifactContentHash,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures)
        {
            SampleId = sampleId;
            Weight = weight;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            HasFootFeatures = hasFootFeatures;
            FootAnalysisSourceId = footAnalysisSourceId ?? string.Empty;
            FootAnalysisVersion = footAnalysisVersion;
            FootArtifactContentHash = footArtifactContentHash ?? string.Empty;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public float Weight { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public bool HasFootFeatures { get; }
        public string FootAnalysisSourceId { get; }
        public int FootAnalysisVersion { get; }
        public string FootArtifactContentHash { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
    }

    public readonly struct AnimationBlendSpacePlayerRuntimeSnapshot
    {
        internal AnimationBlendSpacePlayerRuntimeSnapshot(
            PoseNodeId nodeId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            AnimationPoseSourceId sourceId,
            CharacterAnimationBlendSpaceId blendSpaceId,
            string contentRevision,
            CharacterAnimationBlendSpaceMode mode,
            float rawX,
            float rawY,
            float x,
            float y,
            CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            bool hasFootFeatures,
            int sampleOffset,
            int sampleCount,
            AnimationPoseAvailability poseAvailability = default,
            AnimationPoseNativeInvalidReason invalidReason = default)
        {
            NodeId = nodeId;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            SourceId = sourceId;
            BlendSpaceId = blendSpaceId;
            ContentRevision = contentRevision ?? string.Empty;
            Mode = mode;
            RawX = rawX;
            RawY = rawY;
            X = x;
            Y = y;
            CanonicalPhase = canonicalPhase;
            HasFootFeatures = hasFootFeatures;
            SampleOffset = sampleOffset;
            SampleCount = sampleCount;
            PoseAvailability = poseAvailability;
            InvalidReason = invalidReason;
        }

        internal AnimationBlendSpacePlayerRuntimeSnapshot WithPoseResult(
            AnimationPoseAvailability poseAvailability,
            AnimationPoseNativeInvalidReason invalidReason) =>
            new AnimationBlendSpacePlayerRuntimeSnapshot(
                NodeId,
                PresentationPoseSourceIndex,
                SourceId,
                BlendSpaceId,
                ContentRevision,
                Mode,
                RawX,
                RawY,
                X,
                Y,
                CanonicalPhase,
                HasFootFeatures,
                SampleOffset,
                SampleCount,
                poseAvailability,
                invalidReason);

        public PoseNodeId NodeId { get; }
        public PresentationPoseSourceIndex PresentationPoseSourceIndex { get; }
        public AnimationPoseSourceId SourceId { get; }
        public CharacterAnimationBlendSpaceId BlendSpaceId { get; }
        public string ContentRevision { get; }
        public CharacterAnimationBlendSpaceMode Mode { get; }
        public float RawX { get; }
        public float RawY { get; }
        public float X { get; }
        public float Y { get; }
        public CharacterAnimationBlendSpaceCanonicalPhase CanonicalPhase { get; }
        public bool HasFootFeatures { get; }
        public AnimationPoseAvailability PoseAvailability { get; }
        public AnimationPoseNativeInvalidReason InvalidReason { get; }
        internal int SampleOffset { get; }
        public int SampleCount { get; }
    }

    public readonly struct AnimationReleasedPoseSourceSnapshot
    {
        internal AnimationReleasedPoseSourceSnapshot(PoseNodeId poseNodeId, AnimationPoseSourceId sourceId, ulong completionIdentity)
        {
            PoseNodeId = poseNodeId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        public PoseNodeId PoseNodeId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong CompletionIdentity { get; }
    }

    public enum AnimationSlotTransitionExecution : byte
    {
        None = 0,
        StandardBlend = 1,
        Inertialization = 2
    }

    public readonly struct AnimationSlotRuntimeSnapshot
    {
        internal AnimationSlotRuntimeSnapshot(
            AnimationSlotId slotId,
            PoseNodeId nodeId,
            AnimationChannelId animationChannelId,
            bool hasCurrentAction,
            AnimationPoseSourceId currentActionSourceId,
            bool keepSourcePoseUpdating,
            int sourcePoseValueIndex,
            AnimationPoseAvailability actionAvailability,
            float actionOutputWeight,
            AnimationSlotTransitionExecution transitionExecution,
            bool releasePermission,
            bool pendingReleaseCompletion,
            TransitionRoutingRuntimeSnapshot routing)
        {
            SlotId = slotId;
            NodeId = nodeId;
            AnimationChannelId = animationChannelId;
            HasCurrentAction = hasCurrentAction;
            CurrentActionSourceId = currentActionSourceId;
            KeepSourcePoseUpdating = keepSourcePoseUpdating;
            SourcePoseValueIndex = sourcePoseValueIndex;
            ActionAvailability = actionAvailability;
            ActionOutputWeight = actionOutputWeight;
            TransitionExecution = transitionExecution;
            ReleasePermission = releasePermission;
            PendingReleaseCompletion = pendingReleaseCompletion;
            Routing = routing;
        }

        public AnimationSlotId SlotId { get; }
        public PoseNodeId NodeId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public bool HasCurrentAction { get; }
        public AnimationPoseSourceId CurrentActionSourceId { get; }
        public ulong SourceActionInstanceId => CurrentActionSourceId.SourceActionInstanceId;
        public bool KeepSourcePoseUpdating { get; }
        public int SourcePoseValueIndex { get; }
        public AnimationPoseAvailability ActionAvailability { get; }
        public float ActionOutputWeight { get; }
        public AnimationSlotTransitionExecution TransitionExecution { get; }
        public bool ReleasePermission { get; }
        public bool PendingReleaseCompletion { get; }
        public TransitionRoutingRuntimeSnapshot Routing { get; }
    }

    public readonly struct PoseStateMachineRuntimeSnapshot
    {
        internal PoseStateMachineRuntimeSnapshot(
            PoseStateMachineId stateMachineId,
            PoseNodeId nodeId,
            PoseStateId activeStateId,
            PoseStateId targetStateId,
            PoseStateTransitionId activeTransitionId,
            PoseStateTransitionId evaluatedTransitionId,
            bool hasTransitionRuleResult,
            bool transitionRuleResult,
            float timeInState,
            float transitionProgress,
            AnimationTransitionBlendLogic blendLogic,
            CharacterAnimationBlendMode blendMode,
            float blendDurationSeconds,
            float blendElapsedSeconds,
            int curveIndex,
            int blendProfileIndex,
            TransitionRoutingRuntimeSnapshot routing)
        {
            StateMachineId = stateMachineId;
            NodeId = nodeId;
            ActiveStateId = activeStateId;
            TargetStateId = targetStateId;
            ActiveTransitionId = activeTransitionId;
            EvaluatedTransitionId = evaluatedTransitionId;
            HasTransitionRuleResult = hasTransitionRuleResult;
            TransitionRuleResult = transitionRuleResult;
            TimeInState = timeInState;
            TransitionProgress = transitionProgress;
            BlendLogic = blendLogic;
            BlendMode = blendMode;
            BlendDurationSeconds = blendDurationSeconds;
            BlendElapsedSeconds = blendElapsedSeconds;
            CurveIndex = curveIndex;
            BlendProfileIndex = blendProfileIndex;
            Routing = routing;
        }

        public PoseStateMachineId StateMachineId { get; }
        public PoseNodeId NodeId { get; }
        public PoseStateId ActiveStateId { get; }
        public PoseStateId TargetStateId { get; }
        public PoseStateTransitionId ActiveTransitionId { get; }
        public PoseStateTransitionId EvaluatedTransitionId { get; }
        public bool HasTransitionRuleResult { get; }
        public bool TransitionRuleResult { get; }
        public float TimeInState { get; }
        public float TransitionProgress { get; }
        public AnimationTransitionBlendLogic BlendLogic { get; }
        public CharacterAnimationBlendMode BlendMode { get; }
        public float BlendDurationSeconds { get; }
        public float BlendElapsedSeconds { get; }
        public int CurveIndex { get; }
        public int BlendProfileIndex { get; }
        public TransitionRoutingRuntimeSnapshot Routing { get; }
    }

    public readonly struct RootOrientationWarpRuntimeSnapshot
    {
        internal RootOrientationWarpRuntimeSnapshot(
            PoseNodeId nodeId,
            bool isRelevant,
            float currentFacingError,
            float capturedTargetAngle,
            float sourceYaw,
            float rootYawOffset)
        {
            NodeId = nodeId;
            IsRelevant = isRelevant;
            CurrentFacingError = currentFacingError;
            CapturedTargetAngle = capturedTargetAngle;
            SourceYaw = sourceYaw;
            RootYawOffset = rootYawOffset;
        }

        public PoseNodeId NodeId { get; }
        public bool IsRelevant { get; }
        public float CurrentFacingError { get; }
        public float CapturedTargetAngle { get; }
        public float SourceYaw { get; }
        public float RootYawOffset { get; }
    }

    public readonly struct AnimationLinkedPoseEntryRuntimeSnapshot
    {
        internal AnimationLinkedPoseEntryRuntimeSnapshot(
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            StableHash interfaceSignature,
            LinkedPoseEntryId entryId,
            PoseNodeId callNodeId,
            LinkedPoseImplementationId implementationId,
            ulong generation,
            bool stateReset,
            int fragmentIndex,
            int operationStart,
            int operationCount,
            int stageStart,
            int stageCount,
            int sourceDemandCount,
            ulong completionIdentity,
            bool completed,
            CharacterFullBodyIkGoalSetAvailability goalAvailability,
            int goalCount,
            string goalRigId,
            string goalRigRevision,
            ulong goalCompletionIdentity)
        {
            GroupId = groupId;
            InterfaceId = interfaceId;
            InterfaceSignature = interfaceSignature;
            EntryId = entryId;
            CallNodeId = callNodeId;
            ImplementationId = implementationId;
            Generation = generation;
            StateReset = stateReset;
            FragmentIndex = fragmentIndex;
            OperationStart = operationStart;
            OperationCount = operationCount;
            StageStart = stageStart;
            StageCount = stageCount;
            SourceDemandCount = sourceDemandCount;
            CompletionIdentity = completionIdentity;
            Completed = completed;
            GoalAvailability = goalAvailability;
            GoalCount = goalCount;
            GoalRigId = goalRigId ?? string.Empty;
            GoalRigRevision = goalRigRevision ?? string.Empty;
            GoalCompletionIdentity = goalCompletionIdentity;
        }

        public LinkedPoseGroupId GroupId { get; }
        public LinkedPoseInterfaceId InterfaceId { get; }
        public StableHash InterfaceSignature { get; }
        public LinkedPoseEntryId EntryId { get; }
        public PoseNodeId CallNodeId { get; }
        public LinkedPoseImplementationId ImplementationId { get; }
        public ulong Generation { get; }
        public bool StateReset { get; }
        public int FragmentIndex { get; }
        public int OperationStart { get; }
        public int OperationCount { get; }
        public int StageStart { get; }
        public int StageCount { get; }
        public int SourceDemandCount { get; }
        public ulong CompletionIdentity { get; }
        public bool Completed { get; }
        public CharacterFullBodyIkGoalSetAvailability GoalAvailability { get; }
        public int GoalCount { get; }
        public string GoalRigId { get; }
        public string GoalRigRevision { get; }
        public ulong GoalCompletionIdentity { get; }
    }

    public readonly struct AnimationFootIkRuntimeSnapshot
    {
        internal AnimationFootIkRuntimeSnapshot(
            CharacterFootGroundingDiagnostics grounding,
            CharacterPredictiveFootPlacementModifierDiagnostics modifier,
            CharacterFullBodyIkSolverDiagnostics solver,
            CharacterFullBodyIkEffectorDiagnostics leftFoot,
            CharacterFullBodyIkEffectorDiagnostics rightFoot)
        {
            Grounding = grounding;
            Modifier = modifier;
            Solver = solver;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
        }

        public CharacterFootGroundingDiagnostics Grounding { get; }
        public CharacterPredictiveFootPlacementModifierDiagnostics Modifier { get; }
        public CharacterFullBodyIkSolverDiagnostics Solver { get; }
        public CharacterFullBodyIkEffectorDiagnostics LeftFoot { get; }
        public CharacterFullBodyIkEffectorDiagnostics RightFoot { get; }
        public CharacterFullBodyIkGoal LeftGoal =>
            Modifier.IsCompleted ? Modifier.Left.FinalGoal : Grounding.Left.Goal;
        public CharacterFullBodyIkGoal RightGoal =>
            Modifier.IsCompleted ? Modifier.Right.FinalGoal : Grounding.Right.Goal;
        public bool IsAvailable => Grounding.IsCompleted;
    }

    public readonly struct AnimationPresentationRuntimeSnapshot
    {
        readonly FinalAnimationPoseFramePageLease m_Lease;
        readonly ulong m_LeaseIdentity;
        readonly AnimationBlendStackSnapshot[] m_Stacks;
        readonly PoseInertializationSnapshot[] m_Inertializations;
        readonly AnimationBlendStackEntrySnapshot[] m_Entries;
        readonly AnimationPoseOperationSnapshot[] m_Operations;
        readonly AnimationPoseParameterSnapshot[] m_Parameters;
        readonly AnimationBlendSpacePlayerRuntimeSnapshot[] m_BlendSpacePlayers;
        readonly AnimationBlendSpaceSampleRuntimeSnapshot[] m_BlendSpaceSamples;
        readonly AnimationPoseSourceContribution[] m_SlotContributions;
        readonly AnimationPoseSourceContribution[] m_OperationContributions;
        readonly AnimationPoseSourceContribution[] m_FinalContributions;
        readonly AnimationReleasedPoseSourceSnapshot[] m_Releases;
        readonly AnimationSlotRuntimeSnapshot[] m_AnimationSlots;
        readonly PoseStateMachineRuntimeSnapshot[] m_PoseStateMachines;
        readonly RootOrientationWarpRuntimeSnapshot[] m_RootOrientationWarps;
        readonly CharacterLinkedPoseRuntimeGroupSnapshot[] m_LinkedPoseGroups;
        readonly AnimationLinkedPoseEntryRuntimeSnapshot[] m_LinkedPoseEntries;
        readonly AnimationPoseWatchSnapshot[] m_PoseWatches;
        readonly CharacterFullBodyIkGoal[] m_PoseWatchFullBodyIkGoals;
        readonly CharacterFootGroundingDiagnostics[] m_PoseWatchFootGroundings;
        readonly CharacterPredictiveFootPlacementModifierDiagnostics[] m_PoseWatchFootPlacementModifiers;
        readonly CharacterFullBodyIkSolverDiagnostics[] m_PoseWatchFullBodyIkSolvers;
        readonly CharacterFullBodyIkEffectorDiagnostics[] m_PoseWatchFullBodyIkEffectors;
        readonly CharacterFullBodyIkLimbDiagnostics[] m_PoseWatchFullBodyIkLimbs;
        readonly AnimationLocalBonePose[] m_PoseWatchLocalPoses;
        readonly CharacterComponentBonePose[] m_PoseWatchComponentPoses;
        readonly AnimationPoseSourceContribution[] m_PoseWatchContributions;
        readonly AnimationBoneId[] m_BoneIds;
        readonly AnimationPoseBoneSnapshot[] m_PoseBones;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_StoredBoneWeights;
        readonly Vector3[] m_InertialPositionResiduals;
        readonly Vector3[] m_InertialRotationResiduals;
        readonly Vector3[] m_InertialScaleResiduals;
        readonly float[] m_InertialBoneEnvelopes;
        readonly float[] m_PoseStateMachineBoneWeights;
        readonly float[] m_SlotContributionBoneWeights;
        readonly float[] m_OperationContributionBoneWeights;
        readonly float[] m_FinalContributionBoneWeights;
        readonly AnimationFootIkRuntimeSnapshot m_FootIk;
        readonly int m_StackCount;
        readonly int m_InertializationCount;
        readonly int m_EntryCount;
        readonly int m_OperationCount;
        readonly int m_ParameterCount;
        readonly int m_BlendSpacePlayerCount;
        readonly int m_BlendSpaceSampleCount;
        readonly int m_SlotContributionCount;
        readonly int m_OperationContributionCount;
        readonly int m_FinalContributionCount;
        readonly int m_ReleaseCount;
        readonly int m_AnimationSlotCount;
        readonly int m_PoseStateMachineCount;
        readonly int m_RootOrientationWarpCount;
        readonly int m_LinkedPoseGroupCount;
        readonly int m_LinkedPoseEntryCount;
        readonly int m_PoseWatchCount;

        internal AnimationPresentationRuntimeSnapshot(
            string projectionRevision,
            string rigId,
            string rigRevision,
            string poseGraphId,
            string poseGraphRevision,
            string posePlanHash,
            ulong completionIdentity,
            AnimationPoseAvailability finalAvailability,
            AnimationPoseNativeInvalidReason finalInvalidReason,
            int invalidOperationIndex,
            ulong poseGraphCompletedAt,
            ulong finalAppliedAt,
            ulong continuityIdentity,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            AnimationFootIkRuntimeSnapshot footIk,
            int physicalBoneCount,
            int virtualBoneCount,
            int poseBoneCount,
            FinalAnimationPoseFramePageLease lease,
            ulong leaseIdentity,
            AnimationBlendStackSnapshot[] stacks,
            int stackCount,
            PoseInertializationSnapshot[] inertializations,
            int inertializationCount,
            AnimationBlendStackEntrySnapshot[] entries,
            int entryCount,
            AnimationPoseOperationSnapshot[] operations,
            int operationCount,
            AnimationPoseParameterSnapshot[] parameters,
            int parameterCount,
            AnimationBlendSpacePlayerRuntimeSnapshot[] blendSpacePlayers,
            int blendSpacePlayerCount,
            AnimationBlendSpaceSampleRuntimeSnapshot[] blendSpaceSamples,
            int blendSpaceSampleCount,
            AnimationPoseSourceContribution[] slotContributions,
            int slotContributionCount,
            AnimationPoseSourceContribution[] operationContributions,
            int operationContributionCount,
            AnimationPoseSourceContribution[] finalContributions,
            int finalContributionCount,
            AnimationReleasedPoseSourceSnapshot[] releases,
            int releaseCount,
            AnimationSlotRuntimeSnapshot[] animationSlots,
            int animationSlotCount,
            PoseStateMachineRuntimeSnapshot[] poseStateMachines,
            int poseStateMachineCount,
            RootOrientationWarpRuntimeSnapshot[] rootOrientationWarps,
            int rootOrientationWarpCount,
            CharacterLinkedPoseRuntimeGroupSnapshot[] linkedPoseGroups,
            int linkedPoseGroupCount,
            AnimationLinkedPoseEntryRuntimeSnapshot[] linkedPoseEntries,
            int linkedPoseEntryCount,
            AnimationPoseWatchSnapshot[] poseWatches,
            int poseWatchCount,
            CharacterFullBodyIkGoal[] poseWatchFullBodyIkGoals,
            CharacterFootGroundingDiagnostics[] poseWatchFootGroundings,
            CharacterPredictiveFootPlacementModifierDiagnostics[] poseWatchFootPlacementModifiers,
            CharacterFullBodyIkSolverDiagnostics[] poseWatchFullBodyIkSolvers,
            CharacterFullBodyIkEffectorDiagnostics[] poseWatchFullBodyIkEffectors,
            CharacterFullBodyIkLimbDiagnostics[] poseWatchFullBodyIkLimbs,
            AnimationLocalBonePose[] poseWatchLocalPoses,
            CharacterComponentBonePose[] poseWatchComponentPoses,
            AnimationPoseSourceContribution[] poseWatchContributions,
            AnimationBoneId[] boneIds,
            AnimationPoseBoneSnapshot[] poseBones,
            float[] entryBoneWeights,
            float[] storedBoneWeights,
            Vector3[] inertialPositionResiduals,
            Vector3[] inertialRotationResiduals,
            Vector3[] inertialScaleResiduals,
            float[] inertialBoneEnvelopes,
            float[] poseStateMachineBoneWeights,
            float[] slotContributionBoneWeights,
            float[] operationContributionBoneWeights,
            float[] finalContributionBoneWeights)
        {
            ProjectionRevision = projectionRevision ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            PoseGraphId = poseGraphId ?? string.Empty;
            PoseGraphRevision = poseGraphRevision ?? string.Empty;
            PosePlanHash = posePlanHash ?? string.Empty;
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
            m_FootIk = footIk;
            PhysicalBoneCount = physicalBoneCount;
            VirtualBoneCount = virtualBoneCount;
            PoseBoneCount = poseBoneCount;
            m_Lease = lease ?? throw new ArgumentNullException(nameof(lease));
            m_LeaseIdentity = leaseIdentity;
            m_Stacks = stacks;
            m_StackCount = stackCount;
            m_Inertializations = inertializations;
            m_InertializationCount = inertializationCount;
            m_Entries = entries;
            m_EntryCount = entryCount;
            m_Operations = operations;
            m_OperationCount = operationCount;
            m_Parameters = parameters;
            m_ParameterCount = parameterCount;
            m_BlendSpacePlayers = blendSpacePlayers;
            m_BlendSpacePlayerCount = blendSpacePlayerCount;
            m_BlendSpaceSamples = blendSpaceSamples;
            m_BlendSpaceSampleCount = blendSpaceSampleCount;
            m_SlotContributions = slotContributions;
            m_SlotContributionCount = slotContributionCount;
            m_OperationContributions = operationContributions;
            m_OperationContributionCount = operationContributionCount;
            m_FinalContributions = finalContributions;
            m_FinalContributionCount = finalContributionCount;
            m_Releases = releases;
            m_ReleaseCount = releaseCount;
            m_AnimationSlots = animationSlots;
            m_AnimationSlotCount = animationSlotCount;
            m_PoseStateMachines = poseStateMachines;
            m_PoseStateMachineCount = poseStateMachineCount;
            m_RootOrientationWarps = rootOrientationWarps;
            m_RootOrientationWarpCount = rootOrientationWarpCount;
            m_LinkedPoseGroups = linkedPoseGroups;
            m_LinkedPoseGroupCount = linkedPoseGroupCount;
            m_LinkedPoseEntries = linkedPoseEntries;
            m_LinkedPoseEntryCount = linkedPoseEntryCount;
            m_PoseWatches = poseWatches;
            m_PoseWatchCount = poseWatchCount;
            m_PoseWatchFullBodyIkGoals = poseWatchFullBodyIkGoals;
            m_PoseWatchFootGroundings = poseWatchFootGroundings;
            m_PoseWatchFootPlacementModifiers = poseWatchFootPlacementModifiers;
            m_PoseWatchFullBodyIkSolvers = poseWatchFullBodyIkSolvers;
            m_PoseWatchFullBodyIkEffectors = poseWatchFullBodyIkEffectors;
            m_PoseWatchFullBodyIkLimbs = poseWatchFullBodyIkLimbs;
            m_PoseWatchLocalPoses = poseWatchLocalPoses;
            m_PoseWatchComponentPoses = poseWatchComponentPoses;
            m_PoseWatchContributions = poseWatchContributions;
            m_BoneIds = boneIds;
            m_PoseBones = poseBones;
            m_EntryBoneWeights = entryBoneWeights;
            m_StoredBoneWeights = storedBoneWeights;
            m_InertialPositionResiduals = inertialPositionResiduals;
            m_InertialRotationResiduals = inertialRotationResiduals;
            m_InertialScaleResiduals = inertialScaleResiduals;
            m_InertialBoneEnvelopes = inertialBoneEnvelopes;
            m_PoseStateMachineBoneWeights = poseStateMachineBoneWeights;
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
        public string PosePlanHash { get; }
        public ulong CompletionIdentity { get; }
        public AnimationPoseAvailability FinalAvailability { get; }
        public AnimationPoseNativeInvalidReason FinalInvalidReason { get; }
        public int InvalidOperationIndex { get; }
        public ulong PoseGraphCompletedAt { get; }
        public ulong FinalAppliedAt { get; }
        public ulong ContinuityIdentity { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public AnimationFootIkRuntimeSnapshot FootIk => m_FootIk;
        public int PhysicalBoneCount { get; }
        public int VirtualBoneCount { get; }
        public int PoseBoneCount { get; }
        public bool StackWeightsArePrePoseGraphMask => true;
        public AnimationReadOnlyBuffer<AnimationBlendStackSnapshot> Stacks => Buffer(m_Stacks, m_StackCount);
        public AnimationReadOnlyBuffer<PoseInertializationSnapshot> Inertializations =>
            Buffer(m_Inertializations, m_InertializationCount);
        public AnimationReadOnlyBuffer<AnimationBlendStackEntrySnapshot> Entries => Buffer(m_Entries, m_EntryCount);
        public AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> Operations => Buffer(m_Operations, m_OperationCount);
        public AnimationReadOnlyBuffer<AnimationPoseParameterSnapshot> Parameters => Buffer(m_Parameters, m_ParameterCount);
        public AnimationReadOnlyBuffer<AnimationBlendSpacePlayerRuntimeSnapshot> BlendSpacePlayers =>
            Buffer(m_BlendSpacePlayers, m_BlendSpacePlayerCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> SlotContributions => Buffer(m_SlotContributions, m_SlotContributionCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> OperationContributions =>
            Buffer(m_OperationContributions, m_OperationContributionCount);
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> FinalContributions => Buffer(m_FinalContributions, m_FinalContributionCount);
        public AnimationReadOnlyBuffer<AnimationReleasedPoseSourceSnapshot> Releases => Buffer(m_Releases, m_ReleaseCount);
        public AnimationReadOnlyBuffer<AnimationSlotRuntimeSnapshot> AnimationSlots =>
            Buffer(m_AnimationSlots, m_AnimationSlotCount);
        public AnimationReadOnlyBuffer<PoseStateMachineRuntimeSnapshot> PoseStateMachines =>
            Buffer(m_PoseStateMachines, m_PoseStateMachineCount);
        public AnimationReadOnlyBuffer<RootOrientationWarpRuntimeSnapshot> RootOrientationWarps =>
            Buffer(m_RootOrientationWarps, m_RootOrientationWarpCount);
        public AnimationReadOnlyBuffer<CharacterLinkedPoseRuntimeGroupSnapshot> LinkedPoseGroups =>
            Buffer(m_LinkedPoseGroups, m_LinkedPoseGroupCount);
        public AnimationReadOnlyBuffer<AnimationLinkedPoseEntryRuntimeSnapshot> LinkedPoseEntries =>
            Buffer(m_LinkedPoseEntries, m_LinkedPoseEntryCount);
        public AnimationReadOnlyBuffer<AnimationPoseWatchSnapshot> PoseWatches => Buffer(m_PoseWatches, m_PoseWatchCount);
        public AnimationReadOnlyBuffer<AnimationBoneId> BoneIds => Buffer(m_BoneIds, m_BoneIds.Length);
        public AnimationReadOnlyBuffer<AnimationPoseBoneSnapshot> PoseBones =>
            Buffer(m_PoseBones, m_PoseBones.Length);

        public AnimationReadOnlyBuffer<AnimationBlendSpaceSampleRuntimeSnapshot> GetBlendSpaceSamples(int playerIndex)
        {
            RequireIndex(playerIndex, m_BlendSpacePlayerCount, nameof(playerIndex));
            AnimationBlendSpacePlayerRuntimeSnapshot player = m_BlendSpacePlayers[playerIndex];
            return new AnimationReadOnlyBuffer<AnimationBlendSpaceSampleRuntimeSnapshot>(
                m_BlendSpaceSamples,
                player.SampleOffset,
                player.SampleCount,
                m_Lease,
                m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<AnimationLocalBonePose> GetPoseWatchLocalPoses(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            AnimationPoseWatchSnapshot watch = m_PoseWatches[watchIndex];
            return new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                m_PoseWatchLocalPoses,
                watch.PoseOffset,
                watch.Availability == AnimationPoseWatchAvailability.Pose ? watch.BoneCount : 0,
                m_Lease,
                m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> GetPoseWatchContributions(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            AnimationPoseWatchSnapshot watch = m_PoseWatches[watchIndex];
            return new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                m_PoseWatchContributions,
                watch.ContributionOffset,
                watch.ContributionCount,
                m_Lease,
                m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<CharacterComponentBonePose> GetPoseWatchComponentPoses(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            AnimationPoseWatchSnapshot watch = m_PoseWatches[watchIndex];
            return new AnimationReadOnlyBuffer<CharacterComponentBonePose>(
                m_PoseWatchComponentPoses,
                watch.PoseOffset,
                watch.Availability == AnimationPoseWatchAvailability.Pose ? watch.BoneCount : 0,
                m_Lease,
                m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<CharacterFullBodyIkGoal> GetPoseWatchFullBodyIkGoals(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            AnimationPoseWatchSnapshot watch = m_PoseWatches[watchIndex];
            int count = watch.Availability == AnimationPoseWatchAvailability.Targets
                ? watch.GoalSet.GoalCount
                : 0;
            return new AnimationReadOnlyBuffer<CharacterFullBodyIkGoal>(
                m_PoseWatchFullBodyIkGoals,
                watch.GoalSet.GoalOffset,
                count,
                m_Lease,
                m_LeaseIdentity);
        }

        public bool TryGetPoseWatchFootGrounding(
            int watchIndex,
            out CharacterFootGroundingDiagnostics diagnostics)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            diagnostics = m_PoseWatchFootGroundings[watchIndex];
            return diagnostics.IsCompleted;
        }

        public bool TryGetPoseWatchPredictiveFootPlacementModifier(
            int watchIndex,
            out CharacterPredictiveFootPlacementModifierDiagnostics diagnostics)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            diagnostics = m_PoseWatchFootPlacementModifiers[watchIndex];
            return diagnostics.IsCompleted;
        }

        public bool TryGetPoseWatchFullBodyIkSolver(
            int watchIndex,
            out CharacterFullBodyIkSolverDiagnostics diagnostics)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            diagnostics = m_PoseWatchFullBodyIkSolvers[watchIndex];
            return diagnostics.IsCompleted;
        }

        public AnimationReadOnlyBuffer<CharacterFullBodyIkEffectorDiagnostics>
            GetPoseWatchFullBodyIkEffectors(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            CharacterFullBodyIkSolverDiagnostics diagnostics =
                m_PoseWatchFullBodyIkSolvers[watchIndex];
            return new AnimationReadOnlyBuffer<CharacterFullBodyIkEffectorDiagnostics>(
                m_PoseWatchFullBodyIkEffectors,
                watchIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount,
                diagnostics.IsCompleted ? diagnostics.EffectorCount : 0,
                m_Lease,
                m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<CharacterFullBodyIkLimbDiagnostics>
            GetPoseWatchFullBodyIkLimbs(int watchIndex)
        {
            RequireIndex(watchIndex, m_PoseWatchCount, nameof(watchIndex));
            CharacterFullBodyIkSolverDiagnostics diagnostics =
                m_PoseWatchFullBodyIkSolvers[watchIndex];
            return new AnimationReadOnlyBuffer<CharacterFullBodyIkLimbDiagnostics>(
                m_PoseWatchFullBodyIkLimbs,
                watchIndex * 4,
                diagnostics.IsCompleted ? diagnostics.LimbCount : 0,
                m_Lease,
                m_LeaseIdentity);
        }

        public float GetEntryBoneWeight(int entryIndex, int boneIndex) =>
            GetWeight(m_EntryBoneWeights, m_EntryCount, entryIndex, boneIndex);
        public float GetStoredBoneWeight(int stackIndex, int boneIndex) =>
            GetWeight(m_StoredBoneWeights, m_StackCount, stackIndex, boneIndex);
        public Vector3 GetInertialPositionResidual(int nodeIndex, int boneIndex) =>
            GetBoneValue(m_InertialPositionResiduals, m_InertializationCount, nodeIndex, boneIndex);
        public Vector3 GetInertialRotationResidual(int nodeIndex, int boneIndex) =>
            GetBoneValue(m_InertialRotationResiduals, m_InertializationCount, nodeIndex, boneIndex);
        public Vector3 GetInertialScaleResidual(int nodeIndex, int boneIndex) =>
            GetBoneValue(m_InertialScaleResiduals, m_InertializationCount, nodeIndex, boneIndex);
        public float GetInertialBoneEnvelope(int nodeIndex, int boneIndex) =>
            GetWeight(m_InertialBoneEnvelopes, m_InertializationCount, nodeIndex, boneIndex);
        public float GetPoseStateMachineBoneWeight(int stateMachineIndex, int boneIndex) =>
            GetWeight(
                m_PoseStateMachineBoneWeights,
                m_PoseStateMachineCount,
                stateMachineIndex,
                boneIndex);
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

        void RequireIndex(int index, int count, string parameterName)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            if ((uint)index >= (uint)count)
                throw new ArgumentOutOfRangeException(parameterName);
        }

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

        Vector3 GetBoneValue(Vector3[] values, int rowCount, int row, int boneIndex)
        {
            m_Lease.RequireValid(m_LeaseIdentity);
            if ((uint)boneIndex >= (uint)m_BoneIds.Length || (uint)row >= (uint)rowCount)
                throw new ArgumentOutOfRangeException();
            int index = checked(row * m_BoneIds.Length + boneIndex);
            if ((uint)index >= (uint)values.Length)
                throw new ArgumentOutOfRangeException();
            return values[index];
        }
    }
}
