using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPresentationFramePhase : byte
    {
        Begin = 1,
        Prepare = 2,
        Validated = 3,
        EvaluateBarrier = 4,
        Sealed = 5,
        Discarded = 6,
        Faulted = 7
    }

    public enum AnimationPresentationFrameOutcome : byte
    {
        None = 0,
        Committed = 1,
        TypedInvalid = 2,
        Faulted = 3
    }

    internal enum AnimationPresentationMutationOwnerDomain : byte
    {
        ActionLifecycle = 1,
        ActionSampleHistory = 2
    }

    internal enum AnimationPresentationMutationOperationKind : byte
    {
        Select = 1,
        Sample = 2,
        Complete = 3,
        Release = 4,
        Upsert = 5,
        Remove = 6
    }

    internal readonly struct AnimationPresentationMutationJournalHeader
    {
        internal AnimationPresentationMutationJournalHeader(
            AnimationPresentationMutationOwnerDomain ownerDomain,
            AnimationPresentationMutationOperationKind operationKind,
            int payloadIndex,
            int sequenceIndex)
        {
            OwnerDomain = ownerDomain;
            OperationKind = operationKind;
            PayloadIndex = payloadIndex;
            SequenceIndex = sequenceIndex;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Animation Presentation mutation journal header is invalid.");
            }
        }

        internal AnimationPresentationMutationOwnerDomain OwnerDomain
        {
            get;
        }
        internal AnimationPresentationMutationOperationKind OperationKind
        {
            get;
        }
        internal int PayloadIndex { get; }
        internal int SequenceIndex { get; }
        internal bool IsValid =>
            OwnerDomain >=
                AnimationPresentationMutationOwnerDomain.ActionLifecycle &&
            OwnerDomain <=
                AnimationPresentationMutationOwnerDomain.ActionSampleHistory &&
            OperationKind >=
                AnimationPresentationMutationOperationKind.Select &&
            OperationKind <=
                AnimationPresentationMutationOperationKind.Remove &&
            PayloadIndex >= 0 &&
            SequenceIndex >= 0;
    }

    public readonly struct AnimationPresentationFault
    {
        public AnimationPresentationFault(
            ActorId actorId,
            ulong presentationFrame,
            ulong bodyTick,
            AnimationPresentationFramePhase phase,
            ulong completionIdentity)
        {
            ActorId = actorId;
            PresentationFrame = presentationFrame;
            BodyTick = bodyTick;
            Phase = phase;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
                throw new ArgumentException("Animation Presentation fault is invalid.");
        }

        public ActorId ActorId { get; }
        public ulong PresentationFrame { get; }
        public ulong BodyTick { get; }
        public AnimationPresentationFramePhase Phase { get; }
        public ulong CompletionIdentity { get; }
        public bool IsValid =>
            ActorId.IsValid &&
            PresentationFrame != 0 &&
            Phase >= AnimationPresentationFramePhase.EvaluateBarrier &&
            Phase <= AnimationPresentationFramePhase.Faulted;
    }

    public readonly struct AnimationPresentationRuntimeCapacityMetrics
    {
        internal AnimationPresentationRuntimeCapacityMetrics(
            long nativePoseDoublePagePayloadBytes,
            long inertializationDoublePagePayloadBytes,
            long finalPoseDenseDoublePagePayloadBytes,
            int actionJournalCapacity,
            int samplingJournalCapacity,
            int slotJournalCapacity,
            int sourceLifecycleJournalCapacity,
            int preparedSourceResourceCapacity,
            int preparedReleaseResourceCapacity)
        {
            if (nativePoseDoublePagePayloadBytes <= 0 ||
                inertializationDoublePagePayloadBytes < 0 ||
                finalPoseDenseDoublePagePayloadBytes <= 0 ||
                actionJournalCapacity <= 0 ||
                samplingJournalCapacity <= 0 ||
                slotJournalCapacity <= 0 ||
                sourceLifecycleJournalCapacity <= 0 ||
                preparedSourceResourceCapacity <= 0 ||
                preparedReleaseResourceCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nativePoseDoublePagePayloadBytes));
            }
            NativePoseDoublePagePayloadBytes =
                nativePoseDoublePagePayloadBytes;
            InertializationDoublePagePayloadBytes =
                inertializationDoublePagePayloadBytes;
            FinalPoseDenseDoublePagePayloadBytes =
                finalPoseDenseDoublePagePayloadBytes;
            DenseDoublePageResidentPayloadBytes = checked(
                nativePoseDoublePagePayloadBytes +
                inertializationDoublePagePayloadBytes +
                finalPoseDenseDoublePagePayloadBytes);
            ActionJournalCapacity = actionJournalCapacity;
            SamplingJournalCapacity = samplingJournalCapacity;
            SlotJournalCapacity = slotJournalCapacity;
            SourceLifecycleJournalCapacity =
                sourceLifecycleJournalCapacity;
            TotalJournalEntryCapacity = checked(
                (long)actionJournalCapacity +
                samplingJournalCapacity +
                slotJournalCapacity +
                sourceLifecycleJournalCapacity);
            PreparedSourceResourceCapacity =
                preparedSourceResourceCapacity;
            PreparedReleaseResourceCapacity =
                preparedReleaseResourceCapacity;
            TotalPreparedResourceCapacity = checked(
                (long)preparedSourceResourceCapacity +
                preparedReleaseResourceCapacity);
        }

        public long NativePoseDoublePagePayloadBytes { get; }
        public long InertializationDoublePagePayloadBytes { get; }
        public long FinalPoseDenseDoublePagePayloadBytes { get; }
        public long DenseDoublePageResidentPayloadBytes { get; }
        public int ActionJournalCapacity { get; }
        public int SamplingJournalCapacity { get; }
        public int SlotJournalCapacity { get; }
        public int SourceLifecycleJournalCapacity { get; }
        public long TotalJournalEntryCapacity { get; }
        public int PreparedSourceResourceCapacity { get; }
        public int PreparedReleaseResourceCapacity { get; }
        public long TotalPreparedResourceCapacity { get; }
        public bool IncludesManagedObjectHeaders => false;
        public bool IncludesReferencedObjectPayload => false;
    }

    public readonly struct AnimationPresentationRuntimeMetrics
    {
        internal AnimationPresentationRuntimeMetrics(
            in AnimationPresentationRuntimeCapacityMetrics capacity,
            AnimationPresentationFrameOutcome lastFrameOutcome,
            ulong discardCount,
            AnimationPresentationFramePhase faultPhase,
            ulong diagnosticsNoInterestSkipCount)
        {
            Capacity = capacity;
            LastFrameOutcome = lastFrameOutcome;
            DiscardCount = discardCount;
            FaultPhase = faultPhase;
            DiagnosticsNoInterestSkipCount =
                diagnosticsNoInterestSkipCount;
        }

        public AnimationPresentationRuntimeCapacityMetrics Capacity
        {
            get;
        }
        public AnimationPresentationFrameOutcome LastFrameOutcome
        {
            get;
        }
        public ulong DiscardCount { get; }
        public AnimationPresentationFramePhase FaultPhase { get; }
        public bool HasFault =>
            FaultPhase >= AnimationPresentationFramePhase.EvaluateBarrier &&
            FaultPhase <= AnimationPresentationFramePhase.Faulted;
        public ulong DiagnosticsNoInterestSkipCount { get; }
    }

    public readonly struct PoseSourceProviderStatus
    {
        public PoseSourceProviderStatus(
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceAvailability availability,
            PresentationPoseSourceFailureReason failureReason)
        {
            ProviderId = providerId;
            Availability = availability;
            FailureReason = failureReason;
            if (!IsValid)
                throw new ArgumentException("Pose source provider status is invalid.");
        }

        public PresentationPoseSourceProviderId ProviderId { get; }
        public PresentationPoseSourceAvailability Availability { get; }
        public PresentationPoseSourceFailureReason FailureReason { get; }
        public bool IsValid =>
            ProviderId.IsValid &&
            (byte)Availability >=
                (byte)PresentationPoseSourceAvailability.Pending &&
            (byte)Availability <=
                (byte)PresentationPoseSourceAvailability.Invalid &&
            (Availability == PresentationPoseSourceAvailability.Invalid
                ? FailureReason != PresentationPoseSourceFailureReason.None &&
                  (byte)FailureReason <=
                    (byte)PresentationPoseSourceFailureReason.BackendFailure
                : FailureReason == PresentationPoseSourceFailureReason.None);

        public static PoseSourceProviderStatus Pending(
            PresentationPoseSourceProviderId providerId) =>
            new PoseSourceProviderStatus(
                providerId,
                PresentationPoseSourceAvailability.Pending,
                PresentationPoseSourceFailureReason.None);

        public static PoseSourceProviderStatus Ready(
            PresentationPoseSourceProviderId providerId) =>
            new PoseSourceProviderStatus(
                providerId,
                PresentationPoseSourceAvailability.Ready,
                PresentationPoseSourceFailureReason.None);

        public static PoseSourceProviderStatus Invalid(
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceFailureReason failureReason) =>
            new PoseSourceProviderStatus(
                providerId,
                PresentationPoseSourceAvailability.Invalid,
                failureReason);
    }

    public enum PoseSourceProviderDemandKind : byte
    {
        Entry = 1,
        Active = 2,
        TransitionTarget = 3,
        TransitionSource = 4
    }

    public readonly struct PoseSourceProviderDemandGeneration :
        IEquatable<PoseSourceProviderDemandGeneration>
    {
        public PoseSourceProviderDemandGeneration(ulong value)
        {
            Value = value != 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(value));
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(
            PoseSourceProviderDemandGeneration other) =>
                Value == other.Value;
        public override bool Equals(object obj) =>
            obj is PoseSourceProviderDemandGeneration other &&
            Equals(other);
        public override int GetHashCode() =>
            Value.GetHashCode();
        public static bool operator ==(
            PoseSourceProviderDemandGeneration left,
            PoseSourceProviderDemandGeneration right) =>
                left.Equals(right);
        public static bool operator !=(
            PoseSourceProviderDemandGeneration left,
            PoseSourceProviderDemandGeneration right) =>
                !left.Equals(right);
    }

    public readonly struct PoseSourceProviderDemand
    {
        public PoseSourceProviderDemand(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            PoseSourceProviderDemandGeneration demandGeneration,
            PoseSourceProviderDemandKind kind,
            ulong frameSequence)
        {
            ProviderId = providerId;
            PlayerNodeId = playerNodeId;
            SourceIndex = sourceIndex;
            SourceKind = sourceKind;
            DemandGeneration = demandGeneration;
            Kind = kind;
            FrameSequence = frameSequence;
            if (!IsValid)
                throw new ArgumentException("Pose source provider demand is invalid.");
        }

        public PresentationPoseSourceProviderId ProviderId { get; }
        public PoseNodeId PlayerNodeId { get; }
        public PresentationPoseSourceIndex SourceIndex { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public PoseSourceProviderDemandGeneration DemandGeneration { get; }
        public PoseSourceProviderDemandKind Kind { get; }
        public ulong FrameSequence { get; }
        public bool IsValid =>
            ProviderId.IsValid &&
            PlayerNodeId.IsValid &&
            SourceIndex.IsValid &&
            (SourceKind == AnimationPoseSourceKind.Sequence ||
             SourceKind == AnimationPoseSourceKind.BlendSpace ||
             SourceKind == AnimationPoseSourceKind.MotionMatching) &&
            DemandGeneration.IsValid &&
            (byte)Kind >= (byte)PoseSourceProviderDemandKind.Entry &&
            (byte)Kind <=
                (byte)PoseSourceProviderDemandKind.TransitionSource &&
            FrameSequence != 0;
    }

    public enum PresentationFrameFailureKind : byte
    {
        None = 0,
        RequiredProviderPending = 1,
        ProviderInvalid = 2,
        ActionSampleInvalid = 3,
        SlotOperationInvalid = 4,
        PoseOperationInvalid = 5,
        SourceReleaseInvalid = 6
    }

    public readonly struct PresentationFrameFailure
    {
        public PresentationFrameFailure(
            PresentationFrameFailureKind kind,
            string ownerId,
            string detail)
        {
            Kind = kind;
            OwnerId = ownerId?.Trim() ?? string.Empty;
            Detail = detail?.Trim() ?? string.Empty;
            if (!IsValid)
                throw new ArgumentException("Presentation frame failure is invalid.");
        }

        public PresentationFrameFailureKind Kind { get; }
        public string OwnerId { get; }
        public string Detail { get; }
        public bool IsValid =>
            (byte)Kind >=
                (byte)PresentationFrameFailureKind.RequiredProviderPending &&
            (byte)Kind <=
                (byte)PresentationFrameFailureKind.SourceReleaseInvalid &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            !string.IsNullOrWhiteSpace(Detail);
    }
}
