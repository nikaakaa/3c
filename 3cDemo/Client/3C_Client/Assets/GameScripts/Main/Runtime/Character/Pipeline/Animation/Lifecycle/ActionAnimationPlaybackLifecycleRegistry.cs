using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct ActionLifecycleMutationLease
    {
        internal ActionLifecycleMutationLease(ulong identity)
        {
            Identity = identity;
        }

        public ulong Identity { get; }
        public bool IsValid => Identity != 0;
    }

    internal readonly struct ActionAnimationPlaybackLifecycleFrame
    {
        internal ActionAnimationPlaybackLifecycleFrame(
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
            ulong backendReleaseRequestIdentity)
        {
            PlaybackId = playbackId;
            ActionInstanceId = actionInstanceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            AnimationChannelId = animationChannelId;
            ProgramProducerId = programProducerId;
            LatestEventId = latestEventId;
            LatestCommandSequence = latestCommandSequence;
            FirstSampleReadiness = firstSampleReadiness;
            LogicTerminal = logicTerminal;
            Phase = phase;
            LatestCommittedRawSample = latestCommittedRawSample;
            HasCommittedRawSample = hasCommittedRawSample;
            BackendReleaseRequestIdentity = backendReleaseRequestIdentity;
        }

        internal AnimationPlaybackId PlaybackId { get; }
        internal ulong ActionInstanceId { get; }
        internal ulong SourcePoseContinuityIdentity { get; }
        internal AnimationChannelId AnimationChannelId { get; }
        internal string ProgramProducerId { get; }
        internal EventId LatestEventId { get; }
        internal ulong LatestCommandSequence { get; }
        internal ActionFirstSampleReadiness FirstSampleReadiness { get; }
        internal ActionLogicTerminalKind LogicTerminal { get; }
        internal ActionAnimationPlaybackLifecyclePhase Phase { get; }
        internal ActionCommittedRawSample LatestCommittedRawSample { get; }
        internal bool HasCommittedRawSample { get; }
        internal ulong BackendReleaseRequestIdentity { get; }
    }

    public sealed class ActionAnimationPlaybackLifecycleRegistry
    {
        const int UsageCapacityPerPlayback = 4;
        static readonly Comparison<ActionAnimationPlaybackLifecycleFrame>
            s_FrameComparison = CompareFrames;

        sealed class Entry
        {
            internal readonly ActionSlotSourceUsage[] SlotUsages;
            internal readonly ActionBackendSourceIdentity[] PendingBackendSources;
            internal bool Occupied;
            internal AnimationPlaybackId PlaybackId;
            internal ulong ActionInstanceId;
            internal ulong SourcePoseContinuityIdentity;
            internal AnimationChannelId AnimationChannelId;
            internal string ProgramProducerId;
            internal EventId LatestEventId;
            internal ulong LatestCommandSequence;
            internal ActionFirstSampleReadiness FirstSampleReadiness;
            internal ActionLogicTerminalKind LogicTerminal;
            internal ActionAnimationPlaybackLifecyclePhase Phase;
            internal ActionCommittedRawSample LatestCommittedRawSample;
            internal bool HasCommittedRawSample;
            internal AnimationSlotId SlotOwner;
            internal bool HasSlotOwner;
            internal int SlotUsageCount;
            internal ActionRetirementPermission RetirementPermission;
            internal bool HasRetirementPermission;
            internal ulong BackendReleaseRequestIdentity;
            internal int PendingBackendSourceCount;

            internal Entry(int backendSourceCapacity)
            {
                SlotUsages = new ActionSlotSourceUsage[UsageCapacityPerPlayback];
                PendingBackendSources =
                    new ActionBackendSourceIdentity[backendSourceCapacity];
            }

            internal void CopyFrom(Entry source)
            {
                Occupied = source.Occupied;
                PlaybackId = source.PlaybackId;
                ActionInstanceId = source.ActionInstanceId;
                SourcePoseContinuityIdentity =
                    source.SourcePoseContinuityIdentity;
                AnimationChannelId = source.AnimationChannelId;
                ProgramProducerId = source.ProgramProducerId;
                LatestEventId = source.LatestEventId;
                LatestCommandSequence = source.LatestCommandSequence;
                FirstSampleReadiness = source.FirstSampleReadiness;
                LogicTerminal = source.LogicTerminal;
                Phase = source.Phase;
                LatestCommittedRawSample = source.LatestCommittedRawSample;
                HasCommittedRawSample = source.HasCommittedRawSample;
                SlotOwner = source.SlotOwner;
                HasSlotOwner = source.HasSlotOwner;
                SlotUsageCount = source.SlotUsageCount;
                Array.Copy(
                    source.SlotUsages,
                    SlotUsages,
                    source.SlotUsageCount);
                RetirementPermission = source.RetirementPermission;
                HasRetirementPermission = source.HasRetirementPermission;
                BackendReleaseRequestIdentity =
                    source.BackendReleaseRequestIdentity;
                PendingBackendSourceCount =
                    source.PendingBackendSourceCount;
                Array.Copy(
                    source.PendingBackendSources,
                    PendingBackendSources,
                    source.PendingBackendSourceCount);
            }

            internal void Clear()
            {
                if (SlotUsageCount > 0)
                    Array.Clear(SlotUsages, 0, SlotUsageCount);
                if (PendingBackendSourceCount > 0)
                {
                    Array.Clear(
                        PendingBackendSources,
                        0,
                        PendingBackendSourceCount);
                }
                Occupied = false;
                PlaybackId = default;
                ActionInstanceId = 0;
                SourcePoseContinuityIdentity = 0;
                AnimationChannelId = default;
                ProgramProducerId = null;
                LatestEventId = default;
                LatestCommandSequence = 0;
                FirstSampleReadiness = default;
                LogicTerminal = default;
                Phase = default;
                LatestCommittedRawSample = default;
                HasCommittedRawSample = false;
                SlotOwner = default;
                HasSlotOwner = false;
                SlotUsageCount = 0;
                RetirementPermission = default;
                HasRetirementPermission = false;
                BackendReleaseRequestIdentity = 0;
                PendingBackendSourceCount = 0;
            }
        }

        readonly Entry[] m_CommittedEntries;
        readonly Entry[] m_PendingEntries;
        readonly int[] m_PendingCommittedIndices;
        readonly int[] m_PendingTargetIndices;
        readonly bool[] m_ReservedCommittedSlots;
        readonly AnimationPresentationMutationJournalHeader[]
            m_CommandMutationHeaders;
        readonly ActionPlaybackInboxEntry[] m_CommandMutationPayloads;
        readonly FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame>
            m_FrameView;
        readonly List<ActionSlotSourceUsage> m_DiagnosticUsages;
        readonly List<AnimationSlotId> m_DiagnosticOwners;
        readonly List<ActionRetirementPermission> m_DiagnosticPermissions;
        readonly List<ActionBackendSourceIdentity> m_DiagnosticBackendSources;
        int m_PendingCount;
        int m_CommittedCount;
        int m_CommandMutationCount;
        ulong m_NextLeaseIdentity;
        ulong m_NextSourcePoseContinuityIdentity;
        ulong m_PendingSourcePoseContinuityIdentity;
        ActionLifecycleMutationLease m_ActiveLease;
        bool m_Validated;

        public ActionAnimationPlaybackLifecycleRegistry(
            int playbackCapacity,
            int backendSourceCapacity,
            int commandMutationCapacity)
        {
            if (playbackCapacity <= 0 ||
                backendSourceCapacity <= 0 ||
                commandMutationCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            }
            m_CommittedEntries = new Entry[playbackCapacity];
            m_PendingEntries = new Entry[playbackCapacity];
            m_PendingCommittedIndices = new int[playbackCapacity];
            m_PendingTargetIndices = new int[playbackCapacity];
            m_ReservedCommittedSlots = new bool[playbackCapacity];
            m_CommandMutationHeaders =
                new AnimationPresentationMutationJournalHeader[
                    commandMutationCapacity];
            m_CommandMutationPayloads =
                new ActionPlaybackInboxEntry[commandMutationCapacity];
            for (int i = 0; i < playbackCapacity; i++)
            {
                m_CommittedEntries[i] = new Entry(backendSourceCapacity);
                m_PendingEntries[i] = new Entry(backendSourceCapacity);
            }
            m_FrameView =
                new FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame>(
                    playbackCapacity);
            m_DiagnosticUsages =
                new List<ActionSlotSourceUsage>(UsageCapacityPerPlayback);
            m_DiagnosticOwners = new List<AnimationSlotId>(1);
            m_DiagnosticPermissions =
                new List<ActionRetirementPermission>(1);
            m_DiagnosticBackendSources =
                new List<ActionBackendSourceIdentity>(backendSourceCapacity);
        }

        public int Count => m_CommittedCount;
        public bool HasActiveMutation => m_ActiveLease.IsValid;
        internal int CommandMutationCapacity =>
            m_CommandMutationHeaders.Length;

        public ActionLifecycleMutationLease BeginMutation()
        {
            if (HasActiveMutation)
            {
                throw new InvalidOperationException(
                    "Action lifecycle registry already has an active mutation.");
            }
            m_NextLeaseIdentity++;
            if (m_NextLeaseIdentity == 0)
                m_NextLeaseIdentity++;
            m_ActiveLease =
                new ActionLifecycleMutationLease(m_NextLeaseIdentity);
            m_PendingCount = 0;
            m_CommandMutationCount = 0;
            m_Validated = false;
            m_PendingSourcePoseContinuityIdentity =
                m_NextSourcePoseContinuityIdentity;
            m_FrameView.Clear();
            return m_ActiveLease;
        }

        public void ApplyCommands(
            ActionLifecycleMutationLease lease,
            IReadOnlyList<ActionPlaybackInboxEntry> entries)
        {
            RequireLease(lease);
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            for (int i = 0; i < entries.Count; i++)
            {
                ActionPlaybackInboxEntry inboxEntry = entries[i];
                if (!inboxEntry.IsValid)
                {
                    throw new InvalidOperationException(
                        "Action lifecycle received an invalid inbox entry.");
                }
                int payloadIndex = AppendCommandMutation(inboxEntry);
                ApplyCommand(m_CommandMutationPayloads[payloadIndex]);
            }
        }

        public void ReplaceSlotUsageBatch(
            ActionLifecycleMutationLease lease,
            IReadOnlyList<ActionSlotSourceUsage> usages)
        {
            RequireLease(lease);
            if (usages == null)
                throw new ArgumentNullException(nameof(usages));
            for (int i = 0; i < m_CommittedEntries.Length; i++)
            {
                if (!m_CommittedEntries[i].Occupied)
                    continue;
                Entry entry = GetWritable(m_CommittedEntries[i].PlaybackId, false);
                entry.SlotUsageCount = 0;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingCommittedIndices[i] < 0)
                    m_PendingEntries[i].SlotUsageCount = 0;
            }
            for (int i = 0; i < usages.Count; i++)
            {
                ActionSlotSourceUsage usage = usages[i];
                Entry entry = FindReadable(usage.PlaybackId);
                if (!usage.IsValid ||
                    entry == null ||
                    entry.Phase == ActionAnimationPlaybackLifecyclePhase.Retired ||
                    !entry.HasSlotOwner ||
                    !entry.SlotOwner.Equals(usage.SlotId))
                {
                    throw new InvalidOperationException(
                        "Action Slot usage targets an invalid playback.");
                }
                Entry writable = GetWritable(usage.PlaybackId, false);
                for (int usageIndex = 0;
                     usageIndex < writable.SlotUsageCount;
                     usageIndex++)
                {
                    ActionSlotSourceUsage existing =
                        writable.SlotUsages[usageIndex];
                    if (existing.SlotId.Equals(usage.SlotId) &&
                        existing.Kind == usage.Kind)
                    {
                        throw new InvalidOperationException(
                            "Action Slot usage batch duplicates an exact consumer.");
                    }
                }
                if (writable.SlotUsageCount == writable.SlotUsages.Length)
                {
                    throw new InvalidOperationException(
                        "Action Slot usage capacity was exceeded.");
                }
                writable.SlotUsages[writable.SlotUsageCount++] = usage;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                Entry entry = m_PendingEntries[i];
                if (entry.Phase != ActionAnimationPlaybackLifecyclePhase.Selected &&
                    entry.Phase != ActionAnimationPlaybackLifecyclePhase.Retained)
                {
                    continue;
                }
                bool selected = false;
                for (int usageIndex = 0;
                     usageIndex < entry.SlotUsageCount;
                     usageIndex++)
                {
                    ActionSlotSourceUsageKind kind =
                        entry.SlotUsages[usageIndex].Kind;
                    if (kind == ActionSlotSourceUsageKind.Sample ||
                        kind == ActionSlotSourceUsageKind.IncomingHandoff)
                    {
                        selected = true;
                        break;
                    }
                }
                entry.Phase =
                    entry.LogicTerminal != ActionLogicTerminalKind.None ||
                    !selected && entry.SlotUsageCount > 0
                        ? ActionAnimationPlaybackLifecyclePhase.Retained
                        : ActionAnimationPlaybackLifecyclePhase.Selected;
            }
        }

        public void BindPlaybackSlot(
            ActionLifecycleMutationLease lease,
            AnimationPlaybackId playbackId,
            AnimationSlotId slotId)
        {
            RequireLease(lease);
            Entry entry = FindReadable(playbackId);
            if (!playbackId.IsValid ||
                !slotId.IsValid ||
                entry == null ||
                entry.Phase == ActionAnimationPlaybackLifecyclePhase.Retired)
            {
                throw new InvalidOperationException(
                    "Action playback Slot owner binding is invalid.");
            }
            Entry writable = GetWritable(playbackId, false);
            if (writable.HasSlotOwner &&
                !writable.SlotOwner.Equals(slotId))
            {
                throw new InvalidOperationException(
                    "Action playback Slot owner changed.");
            }
            writable.SlotOwner = slotId;
            writable.HasSlotOwner = true;
        }

        public void ApplyRetirementPermissions(
            ActionLifecycleMutationLease lease,
            IReadOnlyList<ActionRetirementPermission> permissions)
        {
            RequireLease(lease);
            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions));
            for (int i = 0; i < permissions.Count; i++)
            {
                ActionRetirementPermission permission = permissions[i];
                Entry entry = FindReadable(permission.PlaybackId);
                if (!permission.IsValid ||
                    entry == null ||
                    entry.Phase != ActionAnimationPlaybackLifecyclePhase.Retained ||
                    entry.LogicTerminal == ActionLogicTerminalKind.None ||
                    entry.SlotUsageCount != 0 ||
                    !entry.HasSlotOwner ||
                    !entry.SlotOwner.Equals(permission.SlotId))
                {
                    throw new InvalidOperationException(
                        "Action retirement permission is not eligible.");
                }
                Entry writable = GetWritable(permission.PlaybackId, false);
                if (writable.HasRetirementPermission &&
                    writable.RetirementPermission.CompletionIdentity !=
                    permission.CompletionIdentity)
                {
                    throw new InvalidOperationException(
                        "Action retirement permission changed identity.");
                }
                writable.RetirementPermission = permission;
                writable.HasRetirementPermission = true;
                writable.Phase =
                    ActionAnimationPlaybackLifecyclePhase.RetirementPermitted;
            }
        }

        public void RegisterBackendReleaseRequest(
            ActionLifecycleMutationLease lease,
            ActionBackendReleaseRequest request)
        {
            RequireLease(lease);
            Entry entry = request != null
                ? FindReadable(request.PlaybackId)
                : null;
            if (request == null ||
                entry == null ||
                entry.Phase !=
                    ActionAnimationPlaybackLifecyclePhase.RetirementPermitted ||
                entry.SlotUsageCount != 0 ||
                !entry.HasSlotOwner ||
                !entry.HasRetirementPermission ||
                entry.BackendReleaseRequestIdentity != 0 ||
                request.Sources.Count > entry.PendingBackendSources.Length)
            {
                throw new InvalidOperationException(
                    "Action backend release request is not eligible.");
            }
            Entry writable = GetWritable(request.PlaybackId, false);
            writable.BackendReleaseRequestIdentity = request.RequestIdentity;
            writable.PendingBackendSourceCount = request.Sources.Count;
            for (int i = 0; i < request.Sources.Count; i++)
            {
                ActionBackendSourceIdentity source = request.Sources[i];
                for (int existing = 0; existing < i; existing++)
                {
                    if (writable.PendingBackendSources[existing].Equals(source))
                    {
                        throw new InvalidOperationException(
                            "Action backend release request duplicates a source.");
                    }
                }
                writable.PendingBackendSources[i] = source;
            }
        }

        public void ApplyBackendReleaseCompletions(
            ActionLifecycleMutationLease lease,
            IReadOnlyList<ActionBackendReleaseCompletion> completions)
        {
            RequireLease(lease);
            if (completions == null)
                throw new ArgumentNullException(nameof(completions));
            for (int i = 0; i < completions.Count; i++)
            {
                ActionBackendReleaseCompletion completion = completions[i];
                Entry entry = FindReadable(completion.PlaybackId);
                if (!completion.IsValid ||
                    entry == null ||
                    entry.Phase !=
                        ActionAnimationPlaybackLifecyclePhase.RetirementPermitted ||
                    entry.BackendReleaseRequestIdentity !=
                        completion.RequestIdentity)
                {
                    throw new InvalidOperationException(
                        "Action backend release completion is not exact.");
                }
                Entry writable = GetWritable(completion.PlaybackId, false);
                int sourceIndex = FindBackendSource(writable, completion.Source);
                if (sourceIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action backend release completion is not exact.");
                }
                RemoveBackendSource(writable, sourceIndex);
                if (writable.PendingBackendSourceCount == 0)
                {
                    writable.Phase =
                        ActionAnimationPlaybackLifecyclePhase.Retired;
                }
            }
        }

        public void RetireWithoutBackendResources(
            ActionLifecycleMutationLease lease,
            AnimationPlaybackId playbackId)
        {
            RequireLease(lease);
            Entry entry = FindReadable(playbackId);
            if (entry == null ||
                entry.Phase !=
                    ActionAnimationPlaybackLifecyclePhase.RetirementPermitted ||
                entry.BackendReleaseRequestIdentity != 0 ||
                entry.PendingBackendSourceCount != 0 ||
                entry.SlotUsageCount != 0 ||
                !entry.HasSlotOwner ||
                !entry.HasRetirementPermission)
            {
                throw new InvalidOperationException(
                    "Action playback cannot retire without backend resources.");
            }
            GetWritable(playbackId, false).Phase =
                ActionAnimationPlaybackLifecyclePhase.Retired;
        }

        internal FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame>
            BuildFrameView(ActionLifecycleMutationLease lease)
        {
            RequireLease(lease);
            int projectedCount = m_CommittedCount;
            for (int i = 0; i < m_PendingCount; i++)
            {
                bool committed = m_PendingCommittedIndices[i] >= 0;
                bool retired = m_PendingEntries[i].Phase ==
                    ActionAnimationPlaybackLifecyclePhase.Retired;
                if (!committed && !retired)
                    projectedCount++;
                else if (committed && retired)
                    projectedCount--;
            }
            if (projectedCount > m_CommittedEntries.Length)
            {
                throw new InvalidOperationException(
                    "Action lifecycle committed capacity was exceeded.");
            }
            m_FrameView.Clear();
            for (int i = 0; i < m_CommittedEntries.Length; i++)
            {
                Entry committed = m_CommittedEntries[i];
                if (!committed.Occupied)
                    continue;
                int pendingIndex = FindPending(committed.PlaybackId);
                AddFrame(
                    pendingIndex >= 0
                        ? m_PendingEntries[pendingIndex]
                        : committed);
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingCommittedIndices[i] < 0)
                    AddFrame(m_PendingEntries[i]);
            }
            m_FrameView.Sort(s_FrameComparison);
            return m_FrameView;
        }

        internal void ValidateFrame(ActionLifecycleMutationLease lease)
        {
            ValidateCommandMutationJournal();
            BuildFrameView(lease);
            Array.Clear(
                m_ReservedCommittedSlots,
                0,
                m_ReservedCommittedSlots.Length);
            for (int i = 0; i < m_CommittedEntries.Length; i++)
            {
                if (m_CommittedEntries[i].Occupied)
                    m_ReservedCommittedSlots[i] = true;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                int committedIndex = m_PendingCommittedIndices[i];
                m_PendingTargetIndices[i] = committedIndex;
                if (committedIndex >= 0 &&
                    m_PendingEntries[i].Phase ==
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    m_ReservedCommittedSlots[committedIndex] = false;
                }
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingEntries[i].Phase ==
                        ActionAnimationPlaybackLifecyclePhase.Retired ||
                    m_PendingTargetIndices[i] >= 0)
                {
                    continue;
                }
                int targetIndex = FindFreeReservedSlot();
                if (targetIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action lifecycle committed capacity was exceeded.");
                }
                m_PendingTargetIndices[i] = targetIndex;
                m_ReservedCommittedSlots[targetIndex] = true;
            }
            m_Validated = true;
        }

        public void Commit(ActionLifecycleMutationLease lease)
        {
            RequireLease(lease);
            if (!m_Validated)
            {
                throw new InvalidOperationException(
                    "Action lifecycle mutation was not validated before Seal.");
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                Entry pending = m_PendingEntries[i];
                int committedIndex = m_PendingCommittedIndices[i];
                if (pending.Phase !=
                        ActionAnimationPlaybackLifecyclePhase.Retired ||
                    committedIndex < 0)
                    continue;
                m_CommittedEntries[committedIndex].Clear();
                m_CommittedCount--;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                Entry pending = m_PendingEntries[i];
                int committedIndex = m_PendingTargetIndices[i];
                if (pending.Phase ==
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                    continue;
                if (m_PendingCommittedIndices[i] < 0)
                    m_CommittedCount++;
                m_CommittedEntries[committedIndex].CopyFrom(pending);
            }
            m_NextSourcePoseContinuityIdentity =
                m_PendingSourcePoseContinuityIdentity;
            Close();
        }

        public void Discard(ActionLifecycleMutationLease lease)
        {
            RequireLease(lease);
            Close();
        }

        internal void BuildCommittedSnapshot(
            FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleSnapshot>
                destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (HasActiveMutation)
            {
                throw new InvalidOperationException(
                    "Action lifecycle committed diagnostics cannot read an active mutation.");
            }
            m_FrameView.Clear();
            for (int i = 0; i < m_CommittedEntries.Length; i++)
            {
                if (m_CommittedEntries[i].Occupied)
                    AddFrame(m_CommittedEntries[i]);
            }
            m_FrameView.Sort(s_FrameComparison);
            destination.Clear();
            for (int i = 0; i < m_FrameView.Count; i++)
            {
                int committedIndex =
                    FindCommitted(m_FrameView[i].PlaybackId);
                if (committedIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Committed Action lifecycle diagnostics lost its owner.");
                }
                Entry entry = m_CommittedEntries[committedIndex];
                FillDiagnosticCollections(entry);
                destination.Add(
                    new ActionAnimationPlaybackLifecycleSnapshot(
                        entry.PlaybackId,
                        entry.ActionInstanceId,
                        entry.SourcePoseContinuityIdentity,
                        entry.AnimationChannelId,
                        entry.ProgramProducerId,
                        entry.LatestEventId,
                        entry.LatestCommandSequence,
                        entry.FirstSampleReadiness,
                        entry.LogicTerminal,
                        entry.Phase,
                        entry.LatestCommittedRawSample,
                        entry.HasCommittedRawSample,
                        m_DiagnosticOwners,
                        m_DiagnosticUsages,
                        m_DiagnosticPermissions,
                        entry.BackendReleaseRequestIdentity,
                        m_DiagnosticBackendSources));
            }
        }

        public void Reset()
        {
            if (HasActiveMutation)
            {
                throw new InvalidOperationException(
                    "Action lifecycle registry cannot reset during mutation.");
            }
            for (int i = 0; i < m_CommittedEntries.Length; i++)
                m_CommittedEntries[i].Clear();
            m_CommittedCount = 0;
            m_FrameView.Clear();
        }

        void ApplyCommand(ActionPlaybackInboxEntry inboxEntry)
        {
            ActionAnimationPlaybackCommand command = inboxEntry.Command;
            Entry existing = FindReadable(command.PlaybackId);
            if (command.Kind == ActionAnimationPlaybackCommandKind.Select)
            {
                if (existing != null)
                {
                    RequireOwnership(existing, command);
                    if (existing.Phase ==
                            ActionAnimationPlaybackLifecyclePhase.Retired ||
                        existing.LogicTerminal != ActionLogicTerminalKind.None)
                    {
                        throw new InvalidOperationException(
                            "Action playback Select targets a retired entry.");
                    }
                    Entry writable = GetWritable(command.PlaybackId, false);
                    writable.LatestEventId = command.EventId;
                    writable.LatestCommandSequence = inboxEntry.Sequence;
                    return;
                }
                Entry created = GetWritable(command.PlaybackId, true);
                created.ActionInstanceId = command.ActionInstanceId;
                created.SourcePoseContinuityIdentity =
                    NextSourcePoseContinuityIdentity();
                created.AnimationChannelId = command.AnimationChannelId;
                created.ProgramProducerId = command.ProgramProducerId;
                created.LatestEventId = command.EventId;
                created.LatestCommandSequence = inboxEntry.Sequence;
                created.FirstSampleReadiness = ActionFirstSampleReadiness.Pending;
                created.LogicTerminal = ActionLogicTerminalKind.None;
                created.Phase =
                    ActionAnimationPlaybackLifecyclePhase.PendingFirstSample;
                return;
            }
            if (existing == null)
            {
                throw new InvalidOperationException(
                    "Action playback command has no matching Select.");
            }
            RequireOwnership(existing, command);
            if (existing.Phase ==
                ActionAnimationPlaybackLifecyclePhase.Retired)
            {
                throw new InvalidOperationException(
                    "Action playback command targets a retired entry.");
            }
            Entry entry = GetWritable(command.PlaybackId, false);
            entry.LatestEventId = command.EventId;
            entry.LatestCommandSequence = inboxEntry.Sequence;
            if (command.Kind == ActionAnimationPlaybackCommandKind.Sample)
            {
                if (entry.LogicTerminal != ActionLogicTerminalKind.None)
                {
                    throw new InvalidOperationException(
                        "Action playback Sample follows a terminal command.");
                }
                entry.LatestCommittedRawSample = command.CommittedRawSample;
                entry.HasCommittedRawSample = true;
                entry.FirstSampleReadiness = ActionFirstSampleReadiness.Ready;
                if (entry.Phase ==
                    ActionAnimationPlaybackLifecyclePhase.PendingFirstSample)
                {
                    entry.Phase =
                        ActionAnimationPlaybackLifecyclePhase.Selected;
                }
                return;
            }
            ActionLogicTerminalKind terminal =
                command.Kind == ActionAnimationPlaybackCommandKind.Complete
                    ? ActionLogicTerminalKind.Complete
                    : ActionLogicTerminalKind.Release;
            if (entry.LogicTerminal == ActionLogicTerminalKind.Release &&
                terminal != ActionLogicTerminalKind.Release)
            {
                throw new InvalidOperationException(
                    "Action playback terminal order is invalid.");
            }
            entry.LogicTerminal = terminal;
            if (entry.Phase ==
                ActionAnimationPlaybackLifecyclePhase.PendingFirstSample)
            {
                entry.FirstSampleReadiness =
                    ActionFirstSampleReadiness.Unavailable;
                entry.Phase = ActionAnimationPlaybackLifecyclePhase.Selected;
            }
            if (entry.Phase ==
                ActionAnimationPlaybackLifecyclePhase.Selected)
            {
                entry.Phase = ActionAnimationPlaybackLifecyclePhase.Retained;
            }
        }

        int AppendCommandMutation(ActionPlaybackInboxEntry inboxEntry)
        {
            if (m_CommandMutationCount == m_CommandMutationPayloads.Length)
            {
                throw new InvalidOperationException(
                    "Action lifecycle command mutation journal capacity was exceeded.");
            }
            for (int i = 0; i < m_CommandMutationCount; i++)
            {
                if (m_CommandMutationPayloads[i].Sequence == inboxEntry.Sequence)
                {
                    throw new InvalidOperationException(
                        "Action lifecycle command mutation journal duplicates a command sequence.");
                }
            }
            int payloadIndex = m_CommandMutationCount;
            m_CommandMutationPayloads[payloadIndex] = inboxEntry;
            m_CommandMutationHeaders[payloadIndex] =
                new AnimationPresentationMutationJournalHeader(
                    AnimationPresentationMutationOwnerDomain.ActionLifecycle,
                    MapCommandOperation(inboxEntry.Command.Kind),
                    payloadIndex,
                    payloadIndex);
            m_CommandMutationCount++;
            return payloadIndex;
        }

        void ValidateCommandMutationJournal()
        {
            ulong previousSequence = 0;
            for (int i = 0; i < m_CommandMutationCount; i++)
            {
                AnimationPresentationMutationJournalHeader header =
                    m_CommandMutationHeaders[i];
                ActionPlaybackInboxEntry payload =
                    m_CommandMutationPayloads[i];
                if (!header.IsValid ||
                    header.OwnerDomain !=
                        AnimationPresentationMutationOwnerDomain.ActionLifecycle ||
                    header.PayloadIndex != i ||
                    header.SequenceIndex != i ||
                    !payload.IsValid ||
                    header.OperationKind !=
                        MapCommandOperation(payload.Command.Kind) ||
                    payload.Sequence <= previousSequence)
                {
                    throw new InvalidOperationException(
                        "Action lifecycle command mutation journal order or identity is invalid.");
                }
                previousSequence = payload.Sequence;
            }
        }

        static AnimationPresentationMutationOperationKind MapCommandOperation(
            ActionAnimationPlaybackCommandKind kind) =>
                kind switch
                {
                    ActionAnimationPlaybackCommandKind.Select =>
                        AnimationPresentationMutationOperationKind.Select,
                    ActionAnimationPlaybackCommandKind.Sample =>
                        AnimationPresentationMutationOperationKind.Sample,
                    ActionAnimationPlaybackCommandKind.Complete =>
                        AnimationPresentationMutationOperationKind.Complete,
                    ActionAnimationPlaybackCommandKind.Release =>
                        AnimationPresentationMutationOperationKind.Release,
                    _ => throw new InvalidOperationException(
                        "Action lifecycle command mutation kind is invalid.")
                };

        Entry GetWritable(AnimationPlaybackId playbackId, bool create)
        {
            m_Validated = false;
            int pendingIndex = FindPending(playbackId);
            if (pendingIndex >= 0)
                return m_PendingEntries[pendingIndex];
            if (m_PendingCount == m_PendingEntries.Length)
            {
                throw new InvalidOperationException(
                    "Action lifecycle mutation journal capacity was exceeded.");
            }
            int committedIndex = FindCommitted(playbackId);
            if (committedIndex < 0 && !create)
            {
                throw new InvalidOperationException(
                    $"Action playback '{playbackId}' is unavailable.");
            }
            Entry pending = m_PendingEntries[m_PendingCount];
            pending.Clear();
            if (committedIndex >= 0)
                pending.CopyFrom(m_CommittedEntries[committedIndex]);
            else
            {
                pending.Occupied = true;
                pending.PlaybackId = playbackId;
            }
            m_PendingCommittedIndices[m_PendingCount] = committedIndex;
            m_PendingCount++;
            return pending;
        }

        Entry FindReadable(AnimationPlaybackId playbackId)
        {
            int pendingIndex = FindPending(playbackId);
            if (pendingIndex >= 0)
                return m_PendingEntries[pendingIndex];
            int committedIndex = FindCommitted(playbackId);
            return committedIndex >= 0
                ? m_CommittedEntries[committedIndex]
                : null;
        }

        int FindPending(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingEntries[i].PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }

        int FindCommitted(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_CommittedEntries.Length; i++)
            {
                if (m_CommittedEntries[i].Occupied &&
                    m_CommittedEntries[i].PlaybackId.Equals(playbackId))
                {
                    return i;
                }
            }
            return -1;
        }

        int FindFreeReservedSlot()
        {
            for (int i = 0; i < m_ReservedCommittedSlots.Length; i++)
            {
                if (!m_ReservedCommittedSlots[i])
                    return i;
            }
            return -1;
        }

        void AddFrame(Entry entry)
        {
            if (!entry.Occupied)
                return;
            m_FrameView.Add(
                new ActionAnimationPlaybackLifecycleFrame(
                    entry.PlaybackId,
                    entry.ActionInstanceId,
                    entry.SourcePoseContinuityIdentity,
                    entry.AnimationChannelId,
                    entry.ProgramProducerId,
                    entry.LatestEventId,
                    entry.LatestCommandSequence,
                    entry.FirstSampleReadiness,
                    entry.LogicTerminal,
                    entry.Phase,
                    entry.LatestCommittedRawSample,
                    entry.HasCommittedRawSample,
                    entry.BackendReleaseRequestIdentity));
        }

        void FillDiagnosticCollections(Entry entry)
        {
            m_DiagnosticOwners.Clear();
            m_DiagnosticUsages.Clear();
            m_DiagnosticPermissions.Clear();
            m_DiagnosticBackendSources.Clear();
            if (entry.HasSlotOwner)
                m_DiagnosticOwners.Add(entry.SlotOwner);
            for (int i = 0; i < entry.SlotUsageCount; i++)
                m_DiagnosticUsages.Add(entry.SlotUsages[i]);
            m_DiagnosticUsages.Sort(CompareUsages);
            if (entry.HasRetirementPermission)
            {
                m_DiagnosticPermissions.Add(entry.RetirementPermission);
            }
            for (int i = 0; i < entry.PendingBackendSourceCount; i++)
            {
                m_DiagnosticBackendSources.Add(
                    entry.PendingBackendSources[i]);
            }
            m_DiagnosticBackendSources.Sort();
        }

        void Close()
        {
            for (int i = 0; i < m_PendingCount; i++)
                m_PendingEntries[i].Clear();
            if (m_CommandMutationCount > 0)
            {
                Array.Clear(
                    m_CommandMutationHeaders,
                    0,
                    m_CommandMutationCount);
                Array.Clear(
                    m_CommandMutationPayloads,
                    0,
                    m_CommandMutationCount);
            }
            m_PendingCount = 0;
            m_CommandMutationCount = 0;
            m_FrameView.Clear();
            m_ActiveLease = default;
            m_Validated = false;
        }

        void RequireLease(ActionLifecycleMutationLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity)
            {
                throw new InvalidOperationException(
                    "Action lifecycle mutation lease is invalid.");
            }
        }

        ulong NextSourcePoseContinuityIdentity()
        {
            if (m_PendingSourcePoseContinuityIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Action source Pose continuity identity was exhausted.");
            }
            m_PendingSourcePoseContinuityIdentity++;
            return m_PendingSourcePoseContinuityIdentity;
        }

        static void RequireOwnership(
            Entry entry,
            ActionAnimationPlaybackCommand command)
        {
            if (entry.ActionInstanceId != command.ActionInstanceId ||
                !entry.AnimationChannelId.Equals(command.AnimationChannelId) ||
                !string.Equals(
                    entry.ProgramProducerId,
                    command.ProgramProducerId,
                    StringComparison.Ordinal) ||
                entry.PlaybackId.Generation != command.Generation)
            {
                throw new InvalidOperationException(
                    "Action playback command changed ActionInstance, producer, channel, or generation.");
            }
        }

        static int FindBackendSource(
            Entry entry,
            ActionBackendSourceIdentity source)
        {
            for (int i = 0; i < entry.PendingBackendSourceCount; i++)
            {
                if (entry.PendingBackendSources[i].Equals(source))
                    return i;
            }
            return -1;
        }

        static void RemoveBackendSource(Entry entry, int index)
        {
            int last = --entry.PendingBackendSourceCount;
            for (int i = index; i < last; i++)
            {
                entry.PendingBackendSources[i] =
                    entry.PendingBackendSources[i + 1];
            }
            entry.PendingBackendSources[last] = default;
        }

        static int CompareFrames(
            ActionAnimationPlaybackLifecycleFrame left,
            ActionAnimationPlaybackLifecycleFrame right)
        {
            int producer = string.Compare(
                left.ProgramProducerId,
                right.ProgramProducerId,
                StringComparison.Ordinal);
            return producer != 0
                ? producer
                : left.PlaybackId.Generation.CompareTo(
                    right.PlaybackId.Generation);
        }

        static int CompareUsages(
            ActionSlotSourceUsage left,
            ActionSlotSourceUsage right)
        {
            int slot = left.SlotId.CompareTo(right.SlotId);
            return slot != 0 ? slot : left.Kind.CompareTo(right.Kind);
        }
    }
}
