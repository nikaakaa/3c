using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct ActionMarkerMutationLease
    {
        internal ActionMarkerMutationLease(ulong identity)
        {
            Identity = identity;
        }

        public ulong Identity { get; }
        public bool IsValid => Identity != 0;
    }

    public sealed class ActionMarkerEffectiveSampleState
    {
        static readonly Comparison<ActionMarkerPlaybackSnapshot>
            s_PlaybackSnapshotComparison = ComparePlaybackSnapshots;
        static readonly Comparison<ActionMarkerRelationSnapshot>
            s_RelationSnapshotComparison = CompareRelationSnapshots;

        sealed class Relation
        {
            internal readonly MarkerSegmentRelationCursor Cursor =
                new MarkerSegmentRelationCursor();
            internal bool Occupied;
            internal bool Remove;
            internal ActionMarkerRelationId RelationId;
            internal AnimationPlaybackId Source;
            internal AnimationPlaybackId Target;
            internal AnimationSyncTimeMapping TimeMapping;
            internal string PlanIdentity = string.Empty;
            internal float LeaderFraction;
            internal float FollowerFraction;
            internal int LeaderOccurrenceIndex = -1;
            internal int FollowerOccurrenceIndex = -1;

            internal void CopyFrom(Relation source)
            {
                Occupied = source.Occupied;
                Remove = source.Remove;
                RelationId = source.RelationId;
                Source = source.Source;
                Target = source.Target;
                TimeMapping = source.TimeMapping;
                PlanIdentity = source.PlanIdentity;
                LeaderFraction = source.LeaderFraction;
                FollowerFraction = source.FollowerFraction;
                LeaderOccurrenceIndex = source.LeaderOccurrenceIndex;
                FollowerOccurrenceIndex = source.FollowerOccurrenceIndex;
                Cursor.Initialized = source.Cursor.Initialized;
                Cursor.LeaderOrdinal = source.Cursor.LeaderOrdinal;
                Cursor.FollowerOrdinal = source.Cursor.FollowerOrdinal;
            }

            internal void Clear()
            {
                Occupied = false;
                Remove = false;
                RelationId = default;
                Source = default;
                Target = default;
                TimeMapping = AnimationSyncTimeMapping.Unspecified;
                PlanIdentity = string.Empty;
                LeaderFraction = 0f;
                FollowerFraction = 0f;
                LeaderOccurrenceIndex = -1;
                FollowerOccurrenceIndex = -1;
                Cursor.Initialized = false;
                Cursor.LeaderOrdinal = 0;
                Cursor.FollowerOrdinal = 0;
            }
        }

        struct Anchor
        {
            internal bool Occupied;
            internal bool Remove;
            internal AnimationPlaybackId PlaybackId;
            internal double ProjectedRawTime;
            internal double EffectiveTime;
        }

        struct SampleSlot
        {
            internal bool Occupied;
            internal ActionMarkerEffectiveSample Sample;
        }

        readonly Relation[] m_CommittedRelations;
        readonly Relation[] m_PendingRelations;
        readonly int[] m_PendingRelationCommittedIndices;
        readonly int[] m_PendingRelationTargetIndices;
        readonly bool[] m_ReservedRelationSlots;
        readonly Anchor[] m_CommittedAnchors;
        readonly Anchor[] m_PendingAnchors;
        readonly int[] m_PendingAnchorCommittedIndices;
        readonly int[] m_PendingAnchorTargetIndices;
        readonly bool[] m_ReservedAnchorSlots;
        SampleSlot[] m_CommittedSamples;
        SampleSlot[] m_PendingSamples;
        int m_PendingRelationCount;
        int m_PendingAnchorCount;
        int m_PendingSampleCount;
        int m_CommittedSampleCount;
        ulong m_NextLeaseIdentity;
        ActionMarkerMutationLease m_ActiveLease;
        bool m_Validated;

        public ActionMarkerEffectiveSampleState(
            int playbackCapacity,
            int relationCapacity)
        {
            if (playbackCapacity <= 0 || relationCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            m_CommittedRelations = new Relation[relationCapacity];
            m_PendingRelations = new Relation[relationCapacity];
            m_PendingRelationCommittedIndices = new int[relationCapacity];
            m_PendingRelationTargetIndices = new int[relationCapacity];
            m_ReservedRelationSlots = new bool[relationCapacity];
            for (int i = 0; i < relationCapacity; i++)
            {
                m_CommittedRelations[i] = new Relation();
                m_PendingRelations[i] = new Relation();
            }
            m_CommittedAnchors = new Anchor[playbackCapacity];
            m_PendingAnchors = new Anchor[playbackCapacity];
            m_PendingAnchorCommittedIndices = new int[playbackCapacity];
            m_PendingAnchorTargetIndices = new int[playbackCapacity];
            m_ReservedAnchorSlots = new bool[playbackCapacity];
            m_CommittedSamples = new SampleSlot[playbackCapacity];
            m_PendingSamples = new SampleSlot[playbackCapacity];
        }

        public ActionMarkerMutationLease BeginMutation()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action Marker state already has an active mutation.");
            }
            m_NextLeaseIdentity++;
            if (m_NextLeaseIdentity == 0)
                m_NextLeaseIdentity++;
            m_PendingRelationCount = 0;
            m_PendingAnchorCount = 0;
            m_PendingSampleCount = 0;
            m_Validated = false;
            m_ActiveLease =
                new ActionMarkerMutationLease(m_NextLeaseIdentity);
            return m_ActiveLease;
        }

        public ActionMarkerEffectiveSample ResolveIndependent(
            ActionMarkerMutationLease lease,
            AnimationPlaybackId playbackId,
            AnimationMarkerSyncBinding binding,
            in PresentationPoseSampleTime projectedRawSample)
        {
            RequireLease(lease);
            if (!playbackId.IsValid ||
                binding == null ||
                !binding.TryValidate(out _) ||
                !projectedRawSample.IsValid)
            {
                throw new ArgumentException(
                    "Independent Action Marker sample is invalid.");
            }
            if (!binding.IsMarkerGroup)
            {
                var independent = new ActionMarkerEffectiveSample(
                    playbackId,
                    projectedRawSample,
                    projectedRawSample,
                    string.Empty,
                    string.Empty,
                    0f,
                    false,
                    false);
                SetSample(in independent);
                return independent;
            }
            bool rebased = TryGetAnchor(playbackId, out Anchor anchor);
            double effective = rebased
                ? anchor.EffectiveTime +
                  projectedRawSample.ContinuousTime -
                  anchor.ProjectedRawTime
                : projectedRawSample.ContinuousTime;
            ActionMarkerEffectiveSample sample = CreateSample(
                playbackId,
                binding,
                in projectedRawSample,
                effective,
                string.Empty,
                string.Empty,
                0f,
                false,
                rebased);
            SetSample(in sample);
            return sample;
        }

        public ActionMarkerEffectiveSample ResolveRelated(
            ActionMarkerMutationLease lease,
            ActionMarkerRelationId relationId,
            AnimationPlaybackId sourcePlaybackId,
            AnimationMarkerSyncBinding sourceBinding,
            AnimationPlaybackId targetPlaybackId,
            AnimationMarkerSyncBinding targetBinding,
            in PresentationPoseSampleTime targetProjectedRawSample,
            AnimationFootPhaseTimeWarpPlan footPhaseWarp)
        {
            RequireLease(lease);
            if (!relationId.IsValid ||
                !sourcePlaybackId.IsValid ||
                sourceBinding == null ||
                !sourceBinding.IsMarkerGroup ||
                !targetPlaybackId.IsValid ||
                targetBinding == null ||
                !targetBinding.IsMarkerGroup ||
                !targetProjectedRawSample.IsValid ||
                sourcePlaybackId.Equals(targetPlaybackId) ||
                !TryGetPendingSample(
                    sourcePlaybackId,
                    out ActionMarkerEffectiveSample sourceSample))
            {
                throw new ArgumentException(
                    "Related Action Marker sample is invalid.");
            }
            Relation relation = GetWritableRelation(relationId);
            if (relation.Occupied &&
                (!relation.Source.Equals(sourcePlaybackId) ||
                 !relation.Target.Equals(targetPlaybackId)))
            {
                throw new InvalidOperationException(
                    $"Action Marker relation '{relationId}' changed identity without release.");
            }
            relation.Occupied = true;
            relation.Remove = false;
            relation.RelationId = relationId;
            relation.Source = sourcePlaybackId;
            relation.Target = targetPlaybackId;
            MarkerMappedTime mapped =
                MarkerSegmentTimeMapper.MapDetailed(
                    sourceBinding,
                    sourceSample.EffectiveSample.ContinuousTime,
                    targetBinding,
                    targetProjectedRawSample.ContinuousTime,
                    relation.Cursor,
                    footPhaseWarp);
            relation.TimeMapping = sourceBinding.TimeMapping;
            relation.PlanIdentity = footPhaseWarp?.PlanIdentity ?? string.Empty;
            relation.LeaderFraction = mapped.LeaderSegmentFraction;
            relation.FollowerFraction = mapped.FollowerSegmentFraction;
            relation.LeaderOccurrenceIndex = mapped.LeaderOccurrenceIndex;
            relation.FollowerOccurrenceIndex = mapped.FollowerOccurrenceIndex;
            ActionMarkerEffectiveSample sample = CreateSample(
                targetPlaybackId,
                targetBinding,
                in targetProjectedRawSample,
                mapped.ContinuousTime,
                mapped.PreviousMarkerId,
                mapped.NextMarkerId,
                mapped.SegmentFraction,
                true,
                false);
            SetSample(in sample);
            return sample;
        }

        public void RebaseReleasedSource(
            ActionMarkerMutationLease lease,
            AnimationPlaybackId sourcePlaybackId)
        {
            RequireLease(lease);
            if (!sourcePlaybackId.IsValid)
            {
                throw new ArgumentException(
                    "Released Action Marker source is invalid.");
            }
            for (int i = 0; i < m_CommittedRelations.Length; i++)
            {
                Relation committed = m_CommittedRelations[i];
                if (!committed.Occupied)
                    continue;
                int pendingIndex = FindPendingRelation(committed.RelationId);
                Relation relation = pendingIndex >= 0
                    ? m_PendingRelations[pendingIndex]
                    : committed;
                if (!relation.Remove)
                    RebaseRelation(relation, sourcePlaybackId);
            }
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                if (m_PendingRelationCommittedIndices[i] >= 0 ||
                    m_PendingRelations[i].Remove)
                {
                    continue;
                }
                RebaseRelation(m_PendingRelations[i], sourcePlaybackId);
            }
            RemovePendingSample(sourcePlaybackId);
            RemoveAnchor(sourcePlaybackId);
        }

        public bool TryGet(
            ActionMarkerMutationLease lease,
            AnimationPlaybackId playbackId,
            out ActionMarkerEffectiveSample sample)
        {
            RequireLease(lease);
            return TryGetPendingSample(playbackId, out sample);
        }

        internal void BuildCommittedSnapshots(
            FixedCapacityFrameBuffer<ActionMarkerPlaybackSnapshot>
                playbackDestination,
            FixedCapacityFrameBuffer<ActionMarkerRelationSnapshot>
                relationDestination)
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action Marker committed diagnostics cannot read an active mutation.");
            }
            if (playbackDestination == null ||
                relationDestination == null)
            {
                throw new ArgumentNullException(
                    playbackDestination == null
                        ? nameof(playbackDestination)
                        : nameof(relationDestination));
            }
            playbackDestination.Clear();
            relationDestination.Clear();
            for (int i = 0; i < m_CommittedSampleCount; i++)
            {
                if (!m_CommittedSamples[i].Occupied)
                    continue;
                ActionMarkerEffectiveSample sample =
                    m_CommittedSamples[i].Sample;
                playbackDestination.Add(
                    new ActionMarkerPlaybackSnapshot(
                        sample.PlaybackId,
                        sample.ProjectedRawSample,
                        sample.EffectiveSample,
                        sample.PreviousMarkerId,
                        sample.NextMarkerId,
                        sample.SegmentFraction,
                        sample.Mapped,
                        sample.Rebased));
            }
            playbackDestination.Sort(s_PlaybackSnapshotComparison);
            for (int i = 0; i < m_CommittedRelations.Length; i++)
            {
                Relation relation = m_CommittedRelations[i];
                if (!relation.Occupied)
                    continue;
                AddCommittedRelationSnapshot(
                    relation,
                    relationDestination);
            }
            relationDestination.Sort(s_RelationSnapshotComparison);
        }

        public void ValidateFrame(ActionMarkerMutationLease lease)
        {
            RequireLease(lease);
            Array.Clear(
                m_ReservedRelationSlots,
                0,
                m_ReservedRelationSlots.Length);
            for (int i = 0; i < m_CommittedRelations.Length; i++)
            {
                if (m_CommittedRelations[i].Occupied)
                    m_ReservedRelationSlots[i] = true;
            }
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                int committedIndex =
                    m_PendingRelationCommittedIndices[i];
                m_PendingRelationTargetIndices[i] =
                    committedIndex;
                if (m_PendingRelations[i].Remove &&
                    committedIndex >= 0)
                {
                    m_ReservedRelationSlots[committedIndex] = false;
                }
            }
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                if (m_PendingRelations[i].Remove ||
                    m_PendingRelationTargetIndices[i] >= 0)
                {
                    continue;
                }
                int targetIndex = FindFreeReservedSlot(
                    m_ReservedRelationSlots);
                if (targetIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action Marker committed relation capacity was exceeded.");
                }
                m_PendingRelationTargetIndices[i] = targetIndex;
                m_ReservedRelationSlots[targetIndex] = true;
            }
            Array.Clear(
                m_ReservedAnchorSlots,
                0,
                m_ReservedAnchorSlots.Length);
            for (int i = 0; i < m_CommittedAnchors.Length; i++)
            {
                if (m_CommittedAnchors[i].Occupied)
                    m_ReservedAnchorSlots[i] = true;
            }
            for (int i = 0; i < m_PendingAnchorCount; i++)
            {
                int committedIndex =
                    m_PendingAnchorCommittedIndices[i];
                m_PendingAnchorTargetIndices[i] = committedIndex;
                if (m_PendingAnchors[i].Remove &&
                    committedIndex >= 0)
                {
                    m_ReservedAnchorSlots[committedIndex] = false;
                }
            }
            for (int i = 0; i < m_PendingAnchorCount; i++)
            {
                if (m_PendingAnchors[i].Remove ||
                    m_PendingAnchorTargetIndices[i] >= 0)
                {
                    continue;
                }
                int targetIndex = FindFreeReservedSlot(
                    m_ReservedAnchorSlots);
                if (targetIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action Marker committed playback capacity was exceeded.");
                }
                m_PendingAnchorTargetIndices[i] = targetIndex;
                m_ReservedAnchorSlots[targetIndex] = true;
            }
            m_Validated = true;
        }

        public void Commit(ActionMarkerMutationLease lease)
        {
            RequireLease(lease);
            if (!m_Validated)
            {
                throw new InvalidOperationException(
                    "Action Marker state was not validated before Seal.");
            }
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                Relation pending = m_PendingRelations[i];
                int committedIndex =
                    m_PendingRelationCommittedIndices[i];
                if (pending.Remove && committedIndex >= 0)
                    m_CommittedRelations[committedIndex].Clear();
            }
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                Relation pending = m_PendingRelations[i];
                int committedIndex =
                    m_PendingRelationTargetIndices[i];
                if (pending.Remove)
                    continue;
                m_CommittedRelations[committedIndex].CopyFrom(pending);
                m_CommittedRelations[committedIndex].Remove = false;
            }
            for (int i = 0; i < m_PendingAnchorCount; i++)
            {
                Anchor pending = m_PendingAnchors[i];
                int committedIndex = m_PendingAnchorCommittedIndices[i];
                if (pending.Remove && committedIndex >= 0)
                    m_CommittedAnchors[committedIndex] = default;
            }
            for (int i = 0; i < m_PendingAnchorCount; i++)
            {
                Anchor pending = m_PendingAnchors[i];
                int committedIndex = m_PendingAnchorTargetIndices[i];
                if (pending.Remove)
                    continue;
                pending.Remove = false;
                m_CommittedAnchors[committedIndex] = pending;
            }
            SampleSlot[] previousCommitted = m_CommittedSamples;
            m_CommittedSamples = m_PendingSamples;
            m_PendingSamples = previousCommitted;
            int previousCommittedCount = m_CommittedSampleCount;
            m_CommittedSampleCount = m_PendingSampleCount;
            if (previousCommittedCount > 0)
            {
                Array.Clear(
                    m_PendingSamples,
                    0,
                    previousCommittedCount);
            }
            m_PendingSampleCount = 0;
            Close();
        }

        public void Discard(ActionMarkerMutationLease lease)
        {
            RequireLease(lease);
            Close();
        }

        public void Reset()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action Marker state cannot reset during a mutation.");
            }
            for (int i = 0; i < m_CommittedRelations.Length; i++)
                m_CommittedRelations[i].Clear();
            Array.Clear(
                m_CommittedAnchors,
                0,
                m_CommittedAnchors.Length);
            Array.Clear(
                m_CommittedSamples,
                0,
                m_CommittedSampleCount);
            m_CommittedSampleCount = 0;
        }

        Relation GetWritableRelation(ActionMarkerRelationId relationId)
        {
            m_Validated = false;
            int pendingIndex = FindPendingRelation(relationId);
            if (pendingIndex >= 0)
                return m_PendingRelations[pendingIndex];
            if (m_PendingRelationCount == m_PendingRelations.Length)
            {
                throw new InvalidOperationException(
                    "Action Marker pending relation capacity was exceeded.");
            }
            int committedIndex = FindCommittedRelation(relationId);
            Relation pending = m_PendingRelations[m_PendingRelationCount];
            pending.Clear();
            if (committedIndex >= 0)
                pending.CopyFrom(m_CommittedRelations[committedIndex]);
            else
            {
                pending.Occupied = true;
                pending.RelationId = relationId;
            }
            m_PendingRelationCommittedIndices[m_PendingRelationCount] =
                committedIndex;
            m_PendingRelationCount++;
            return pending;
        }

        ref Anchor GetWritableAnchor(AnimationPlaybackId playbackId)
        {
            m_Validated = false;
            int pendingIndex = FindPendingAnchor(playbackId);
            if (pendingIndex >= 0)
                return ref m_PendingAnchors[pendingIndex];
            if (m_PendingAnchorCount == m_PendingAnchors.Length)
            {
                throw new InvalidOperationException(
                    "Action Marker pending anchor capacity was exceeded.");
            }
            int committedIndex = FindCommittedAnchor(playbackId);
            m_PendingAnchors[m_PendingAnchorCount] = committedIndex >= 0
                ? m_CommittedAnchors[committedIndex]
                : new Anchor
                {
                    Occupied = true,
                    PlaybackId = playbackId
                };
            m_PendingAnchorCommittedIndices[m_PendingAnchorCount] =
                committedIndex;
            return ref m_PendingAnchors[m_PendingAnchorCount++];
        }

        void RebaseRelation(
            Relation relation,
            AnimationPlaybackId sourcePlaybackId)
        {
            if (!relation.Source.Equals(sourcePlaybackId))
                return;
            if (!TryGetPendingSample(
                    relation.Target,
                    out ActionMarkerEffectiveSample target) &&
                !TryGetCommittedSample(
                    relation.Target,
                    out target))
            {
                throw new InvalidOperationException(
                    $"Action Marker relation '{relation.RelationId}' has no target sample to rebase.");
            }
            ref Anchor anchor = ref GetWritableAnchor(relation.Target);
            anchor.Occupied = true;
            anchor.Remove = false;
            anchor.PlaybackId = relation.Target;
            anchor.ProjectedRawTime =
                target.ProjectedRawSample.ContinuousTime;
            anchor.EffectiveTime = target.EffectiveSample.ContinuousTime;
            GetWritableRelation(relation.RelationId).Remove = true;
        }

        bool TryGetAnchor(AnimationPlaybackId playbackId, out Anchor anchor)
        {
            int pendingIndex = FindPendingAnchor(playbackId);
            if (pendingIndex >= 0)
            {
                anchor = m_PendingAnchors[pendingIndex];
                return !anchor.Remove;
            }
            int committedIndex = FindCommittedAnchor(playbackId);
            if (committedIndex >= 0)
            {
                anchor = m_CommittedAnchors[committedIndex];
                return true;
            }
            anchor = default;
            return false;
        }

        void RemoveAnchor(AnimationPlaybackId playbackId)
        {
            int pendingIndex = FindPendingAnchor(playbackId);
            int committedIndex = FindCommittedAnchor(playbackId);
            if (pendingIndex < 0 && committedIndex < 0)
                return;
            ref Anchor anchor = ref GetWritableAnchor(playbackId);
            anchor.Remove = true;
        }

        void SetSample(in ActionMarkerEffectiveSample sample)
        {
            m_Validated = false;
            if (FindPendingSample(sample.PlaybackId) >= 0)
            {
                throw new InvalidOperationException(
                    $"Action playback '{sample.PlaybackId}' received multiple Marker samples in one frame.");
            }
            if (m_PendingSampleCount == m_PendingSamples.Length)
            {
                throw new InvalidOperationException(
                    "Action Marker sample capacity was exceeded.");
            }
            m_PendingSamples[m_PendingSampleCount++] = new SampleSlot
            {
                Occupied = true,
                Sample = sample
            };
        }

        void RemovePendingSample(AnimationPlaybackId playbackId)
        {
            m_Validated = false;
            int index = FindPendingSample(playbackId);
            if (index < 0)
                return;
            m_PendingSampleCount--;
            for (int i = index; i < m_PendingSampleCount; i++)
                m_PendingSamples[i] = m_PendingSamples[i + 1];
            m_PendingSamples[m_PendingSampleCount] = default;
        }

        bool TryGetPendingSample(
            AnimationPlaybackId playbackId,
            out ActionMarkerEffectiveSample sample)
        {
            int index = FindPendingSample(playbackId);
            if (index >= 0)
            {
                sample = m_PendingSamples[index].Sample;
                return true;
            }
            sample = default;
            return false;
        }

        internal bool TryGetCommittedSample(
            AnimationPlaybackId playbackId,
            out ActionMarkerEffectiveSample sample)
        {
            for (int i = 0; i < m_CommittedSampleCount; i++)
            {
                if (!m_CommittedSamples[i].Occupied ||
                    !m_CommittedSamples[i].Sample.PlaybackId.Equals(playbackId))
                {
                    continue;
                }
                sample = m_CommittedSamples[i].Sample;
                return true;
            }
            sample = default;
            return false;
        }

        int FindPendingSample(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PendingSampleCount; i++)
            {
                if (m_PendingSamples[i].Sample.PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }

        int FindPendingRelation(ActionMarkerRelationId relationId)
        {
            for (int i = 0; i < m_PendingRelationCount; i++)
            {
                if (m_PendingRelations[i].RelationId.Equals(relationId))
                    return i;
            }
            return -1;
        }

        int FindCommittedRelation(ActionMarkerRelationId relationId)
        {
            for (int i = 0; i < m_CommittedRelations.Length; i++)
            {
                if (m_CommittedRelations[i].Occupied &&
                    m_CommittedRelations[i].RelationId.Equals(relationId))
                {
                    return i;
                }
            }
            return -1;
        }

        static int FindFreeReservedSlot(bool[] reserved)
        {
            for (int i = 0; i < reserved.Length; i++)
            {
                if (!reserved[i])
                    return i;
            }
            return -1;
        }

        int FindPendingAnchor(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PendingAnchorCount; i++)
            {
                if (m_PendingAnchors[i].PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }

        int FindCommittedAnchor(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_CommittedAnchors.Length; i++)
            {
                if (m_CommittedAnchors[i].Occupied &&
                    m_CommittedAnchors[i].PlaybackId.Equals(playbackId))
                {
                    return i;
                }
            }
            return -1;
        }

        void AddCommittedRelationSnapshot(
            Relation relation,
            FixedCapacityFrameBuffer<ActionMarkerRelationSnapshot>
                destination)
        {
            if (!TryGetCommittedSample(
                    relation.Source,
                    out ActionMarkerEffectiveSample source) ||
                !TryGetCommittedSample(
                    relation.Target,
                    out ActionMarkerEffectiveSample target))
            {
                throw new InvalidOperationException(
                    $"Committed Action Marker relation '{relation.RelationId}' has no committed samples.");
            }
            destination.Add(
                new ActionMarkerRelationSnapshot(
                    relation.RelationId,
                    relation.Source,
                    relation.Target,
                    source.EffectiveSample,
                    target.ProjectedRawSample,
                    target.EffectiveSample,
                    target.PreviousMarkerId,
                    target.NextMarkerId,
                    relation.TimeMapping,
                    relation.PlanIdentity,
                    relation.LeaderFraction,
                    relation.FollowerFraction,
                    relation.LeaderOccurrenceIndex,
                    relation.FollowerOccurrenceIndex));
        }

        static ActionMarkerEffectiveSample CreateSample(
            AnimationPlaybackId playbackId,
            AnimationMarkerSyncBinding binding,
            in PresentationPoseSampleTime projectedRawSample,
            double effectiveContinuousTime,
            string previousMarkerId,
            string nextMarkerId,
            float segmentFraction,
            bool mapped,
            bool rebased)
        {
            if (binding == null ||
                binding.DurationSeconds <= 0f ||
                !double.IsFinite(effectiveContinuousTime) ||
                effectiveContinuousTime < 0d)
            {
                throw new InvalidOperationException(
                    "Action Marker mapping produced an invalid time.");
            }
            bool loop = projectedRawSample.Loop;
            double normalized = loop
                ? effectiveContinuousTime
                : Math.Clamp(
                    effectiveContinuousTime,
                    0d,
                    binding.DurationSeconds);
            int cycle = loop
                ? checked((int)Math.Floor(
                    normalized / binding.DurationSeconds))
                : 0;
            float localTime = loop
                ? (float)(normalized -
                          cycle * (double)binding.DurationSeconds)
                : (float)normalized;
            return new ActionMarkerEffectiveSample(
                playbackId,
                projectedRawSample,
                new PresentationPoseSampleTime(
                    localTime,
                    normalized,
                    cycle,
                    loop,
                    projectedRawSample.TimeScale),
                previousMarkerId,
                nextMarkerId,
                segmentFraction,
                mapped,
                rebased);
        }

        void RequireLease(ActionMarkerMutationLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity)
            {
                throw new InvalidOperationException(
                    "Action Marker mutation lease is invalid.");
            }
        }

        void Close()
        {
            for (int i = 0; i < m_PendingRelationCount; i++)
                m_PendingRelations[i].Clear();
            if (m_PendingAnchorCount > 0)
            {
                Array.Clear(
                    m_PendingAnchors,
                    0,
                    m_PendingAnchorCount);
            }
            if (m_PendingSampleCount > 0)
            {
                Array.Clear(
                    m_PendingSamples,
                    0,
                    m_PendingSampleCount);
            }
            m_PendingRelationCount = 0;
            m_PendingAnchorCount = 0;
            m_PendingSampleCount = 0;
            m_ActiveLease = default;
            m_Validated = false;
        }

        static int ComparePlaybackSnapshots(
            ActionMarkerPlaybackSnapshot left,
            ActionMarkerPlaybackSnapshot right) =>
            ComparePlayback(left.PlaybackId, right.PlaybackId);

        static int CompareRelationSnapshots(
            ActionMarkerRelationSnapshot left,
            ActionMarkerRelationSnapshot right) =>
            left.RelationId.CompareTo(right.RelationId);

        static int ComparePlayback(
            AnimationPlaybackId left,
            AnimationPlaybackId right)
        {
            int producer = string.Compare(
                left.ProducerId.ProgramProducerIdentity,
                right.ProducerId.ProgramProducerIdentity,
                StringComparison.Ordinal);
            return producer != 0
                ? producer
                : left.Generation.CompareTo(right.Generation);
        }
    }
}
