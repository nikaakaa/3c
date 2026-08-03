using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum ActionFirstSampleReadiness : byte
    {
        Pending = 1,
        Ready = 2,
        Unavailable = 3
    }

    public enum ActionLogicTerminalKind : byte
    {
        None = 0,
        Complete = 1,
        Release = 2
    }

    public enum ActionSlotSourceUsageKind : byte
    {
        Sample = 1,
        OutgoingHandoff = 2,
        IncomingHandoff = 3,
        StoredPoseReference = 4
    }

    public readonly struct ActionSlotSourceUsage
    {
        public ActionSlotSourceUsage(
            AnimationSlotId slotId,
            AnimationPlaybackId playbackId,
            ActionSlotSourceUsageKind kind,
            ulong completionIdentity)
        {
            SlotId = slotId;
            PlaybackId = playbackId;
            Kind = kind;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
                throw new ArgumentException("Action Slot source usage is invalid.");
        }

        public AnimationSlotId SlotId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public ActionSlotSourceUsageKind Kind { get; }
        public ulong CompletionIdentity { get; }
        public bool IsValid =>
            SlotId.IsValid &&
            PlaybackId.IsValid &&
            (byte)Kind >= (byte)ActionSlotSourceUsageKind.Sample &&
            (byte)Kind <= (byte)ActionSlotSourceUsageKind.StoredPoseReference &&
            CompletionIdentity != 0;
    }

    public readonly struct ActionRetirementPermission
    {
        public ActionRetirementPermission(
            AnimationPlaybackId playbackId,
            AnimationSlotId slotId,
            ulong completionIdentity)
        {
            PlaybackId = playbackId;
            SlotId = slotId;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
                throw new ArgumentException("Action retirement permission is invalid.");
        }

        public AnimationPlaybackId PlaybackId { get; }
        public AnimationSlotId SlotId { get; }
        public ulong CompletionIdentity { get; }
        public bool IsValid =>
            PlaybackId.IsValid &&
            SlotId.IsValid &&
            CompletionIdentity != 0;
    }

    public enum ActionBackendSourceKind : byte
    {
        Playable = 1,
        StoredPoseCapture = 2
    }

    public readonly struct ActionBackendSourceIdentity :
        IEquatable<ActionBackendSourceIdentity>,
        IComparable<ActionBackendSourceIdentity>
    {
        public ActionBackendSourceIdentity(
            ActionBackendSourceKind kind,
            string resourceId,
            ulong generation)
        {
            Kind = kind;
            ResourceId = string.IsNullOrWhiteSpace(resourceId)
                ? throw new ArgumentException("Action backend resource id is required.", nameof(resourceId))
                : resourceId.Trim();
            Generation = generation != 0
                ? generation
                : throw new ArgumentOutOfRangeException(nameof(generation));
        }

        public ActionBackendSourceKind Kind { get; }
        public string ResourceId { get; }
        public ulong Generation { get; }
        public bool IsValid =>
            (byte)Kind >= (byte)ActionBackendSourceKind.Playable &&
            (byte)Kind <= (byte)ActionBackendSourceKind.StoredPoseCapture &&
            !string.IsNullOrWhiteSpace(ResourceId) &&
            Generation != 0;
        public int CompareTo(ActionBackendSourceIdentity other)
        {
            int kind = Kind.CompareTo(other.Kind);
            if (kind != 0)
                return kind;
            int resource = string.Compare(ResourceId, other.ResourceId, StringComparison.Ordinal);
            return resource != 0 ? resource : Generation.CompareTo(other.Generation);
        }
        public bool Equals(ActionBackendSourceIdentity other) =>
            Kind == other.Kind &&
            string.Equals(ResourceId, other.ResourceId, StringComparison.Ordinal) &&
            Generation == other.Generation;
        public override bool Equals(object obj) =>
            obj is ActionBackendSourceIdentity other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine((int)Kind, ResourceId, Generation);
        public override string ToString() => $"{Kind}:{ResourceId}@{Generation}";
    }

    public sealed class ActionBackendReleaseRequest
    {
        readonly ActionBackendSourceIdentity[] m_Sources;
        int m_SourceCount;

        internal ActionBackendReleaseRequest(int sourceCapacity)
        {
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            m_Sources = new ActionBackendSourceIdentity[sourceCapacity];
        }

        internal void Prepare(
            ulong requestIdentity,
            AnimationPlaybackId playbackId,
            IReadOnlyList<ActionBackendSourceIdentity> sources)
        {
            if (RequestIdentity != 0 ||
                requestIdentity == 0 ||
                !playbackId.IsValid ||
                sources == null ||
                sources.Count == 0 ||
                sources.Count > m_Sources.Length)
            {
                throw new ArgumentException(
                    "Action backend release request is invalid.");
            }
            for (int i = 0; i < sources.Count; i++)
            {
                ActionBackendSourceIdentity source = sources[i];
                if (!source.IsValid)
                {
                    throw new ArgumentException(
                        "Action backend release request contains an invalid source.");
                }
                m_Sources[i] = source;
            }
            Array.Sort(m_Sources, 0, sources.Count);
            for (int i = 1; i < sources.Count; i++)
            {
                if (m_Sources[i - 1].Equals(m_Sources[i]))
                {
                    Array.Clear(m_Sources, 0, sources.Count);
                    throw new ArgumentException(
                        "Action backend release request contains a duplicate source.");
                }
            }
            RequestIdentity = requestIdentity;
            PlaybackId = playbackId;
            m_SourceCount = sources.Count;
        }

        internal void Clear()
        {
            Array.Clear(m_Sources, 0, m_SourceCount);
            m_SourceCount = 0;
            RequestIdentity = 0;
            PlaybackId = default;
        }

        public ulong RequestIdentity { get; private set; }
        public AnimationPlaybackId PlaybackId { get; private set; }
        public AnimationReadOnlyBuffer<ActionBackendSourceIdentity> Sources =>
            new AnimationReadOnlyBuffer<ActionBackendSourceIdentity>(
                m_Sources,
                0,
                m_SourceCount);
    }

    public readonly struct ActionBackendReleaseCompletion
    {
        public ActionBackendReleaseCompletion(
            ulong requestIdentity,
            AnimationPlaybackId playbackId,
            ActionBackendSourceIdentity source,
            ulong completionIdentity)
        {
            RequestIdentity = requestIdentity;
            PlaybackId = playbackId;
            Source = source;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
                throw new ArgumentException("Action backend release completion is invalid.");
        }

        public ulong RequestIdentity { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public ActionBackendSourceIdentity Source { get; }
        public ulong CompletionIdentity { get; }
        public bool IsValid =>
            RequestIdentity != 0 &&
            PlaybackId.IsValid &&
            Source.IsValid &&
            CompletionIdentity != 0;
    }

    public readonly struct AnimationSlotSourceReleaseCompletion
    {
        public AnimationSlotSourceReleaseCompletion(
            AnimationSlotId slotId,
            AnimationPlaybackId playbackId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            SlotId = slotId;
            PlaybackId = playbackId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Animation Slot source release completion is invalid.");
            }
        }

        public AnimationSlotId SlotId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong CompletionIdentity { get; }
        public bool IsValid =>
            SlotId.IsValid &&
            PlaybackId.IsValid &&
            SourceId.IsValid &&
            SourceId.SourceKind ==
                AnimationPoseSourceKind.Timeline &&
            SourceId.PlaybackId.Equals(PlaybackId) &&
            SourceId.SourceActionInstanceId != 0 &&
            CompletionIdentity != 0;
    }

    public sealed class ActionAnimationPlaybackLifecycleSnapshot
    {
        readonly AnimationSlotId[] m_SlotOwners;
        readonly ActionSlotSourceUsage[] m_SlotUsages;
        readonly ActionRetirementPermission[] m_RetirementPermissions;
        readonly ActionBackendSourceIdentity[] m_PendingBackendSources;

        public ActionAnimationPlaybackLifecycleSnapshot(
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            ulong sourcePoseContinuityIdentity,
            AnimationChannelId animationChannelId,
            string programProducerId,
            EventId latestEventId,
            ulong latestCommandSequence,
            ActionFirstSampleReadiness firstSampleReadiness,
            ActionLogicTerminalKind logicTerminal,
            ActionAnimationPlaybackLifecyclePhase phase,
            ActionCommittedRawSample latestCommittedRawSample,
            bool hasCommittedRawSample,
            IReadOnlyList<AnimationSlotId> slotOwners,
            IReadOnlyList<ActionSlotSourceUsage> slotUsages,
            IReadOnlyList<ActionRetirementPermission>
                retirementPermissions,
            ulong backendReleaseRequestIdentity,
            IReadOnlyList<ActionBackendSourceIdentity> pendingBackendSources)
        {
            PlaybackId = playbackId;
            ActionInstanceId = actionInstanceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            AnimationChannelId = animationChannelId;
            ProgramProducerId = programProducerId?.Trim() ?? string.Empty;
            LatestEventId = latestEventId;
            LatestCommandSequence = latestCommandSequence;
            FirstSampleReadiness = firstSampleReadiness;
            LogicTerminal = logicTerminal;
            Phase = phase;
            LatestCommittedRawSample = latestCommittedRawSample;
            HasCommittedRawSample = hasCommittedRawSample;
            m_SlotOwners = Copy(slotOwners);
            m_SlotUsages = Copy(slotUsages);
            m_RetirementPermissions = Copy(retirementPermissions);
            BackendReleaseRequestIdentity = backendReleaseRequestIdentity;
            m_PendingBackendSources = Copy(pendingBackendSources);
        }

        public AnimationPlaybackId PlaybackId { get; }
        public ulong ActionInstanceId { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string ProgramProducerId { get; }
        public EventId LatestEventId { get; }
        public ulong LatestCommandSequence { get; }
        public ActionFirstSampleReadiness FirstSampleReadiness { get; }
        public ActionLogicTerminalKind LogicTerminal { get; }
        public ActionAnimationPlaybackLifecyclePhase Phase { get; }
        public ActionCommittedRawSample LatestCommittedRawSample { get; }
        public bool HasCommittedRawSample { get; }
        public IReadOnlyList<AnimationSlotId> SlotOwners => m_SlotOwners;
        public IReadOnlyList<ActionSlotSourceUsage> SlotUsages => m_SlotUsages;
        public IReadOnlyList<ActionRetirementPermission>
            RetirementPermissions => m_RetirementPermissions;
        public ulong BackendReleaseRequestIdentity { get; }
        public IReadOnlyList<ActionBackendSourceIdentity> PendingBackendSources =>
            m_PendingBackendSources;

        static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<T>();
            var result = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }
    }
}
