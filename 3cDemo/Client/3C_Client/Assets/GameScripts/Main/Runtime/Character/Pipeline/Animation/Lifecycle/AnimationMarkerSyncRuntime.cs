using System;
using System.Collections.Generic;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public enum AnimationMarkerSyncNotApplicableReason
    {
        None,
        SamePlayback,
        MissingCurrent,
        SyncDisabled,
        DifferentLayer,
        DifferentGroup
    }

    public enum AnimationMarkerSyncInvalidReason
    {
        None,
        ProjectionInvalid,
        SourceSampleMissing,
        SourceSegmentMissing,
        TargetPairMissing,
        FiniteCoverageExceeded,
        MultipleSources,
        RelationCycle,
        CrossLayerRelation,
        SourceTimeRegressed,
        NonFiniteResult,
        RoleConflict
    }

    public sealed class AnimationMarkerSyncException : InvalidOperationException
    {
        public AnimationMarkerSyncException(
            AnimationMarkerSyncInvalidReason reason,
            AnimationPlaybackId playbackId)
            : base($"Animation Marker Sync failed: reason={reason}; playback={playbackId}.")
        {
            Reason = reason;
            PlaybackId = playbackId;
        }

        public AnimationMarkerSyncInvalidReason Reason { get; }
        public AnimationPlaybackId PlaybackId { get; }
    }

    public enum AnimationMarkerSyncSnapshotReason
    {
        NoCurrentSource,
        SourceExplicitNone,
        TargetExplicitNone,
        DifferentLayer,
        DifferentGroup,
        RelationCreated,
        RelationContinued,
        SourceRetiredRebased,
        InvalidProjection,
        MissingSegmentPair,
        FiniteCoverageExceeded,
        RelationCycle
    }

    public readonly struct AnimationMarkerSyncRawSample
    {
        public AnimationMarkerSyncRawSample(
            AnimationPlaybackId playbackId,
            string layerId,
            AnimationMarkerSyncBinding binding,
            float localTime,
            double continuousTime,
            int cycle)
        {
            PlaybackId = playbackId;
            LayerId = layerId ?? string.Empty;
            Binding = binding;
            LocalTime = localTime;
            ContinuousTime = continuousTime;
            Cycle = cycle;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public string LayerId { get; }
        public AnimationMarkerSyncBinding Binding { get; }
        public float LocalTime { get; }
        public double ContinuousTime { get; }
        public int Cycle { get; }
    }

    public readonly struct AnimationMarkerSyncEffectiveSample
    {
        public AnimationMarkerSyncEffectiveSample(
            AnimationPlaybackId playbackId,
            float localTime,
            double continuousTime,
            int cycle,
            string previousMarkerId,
            string nextMarkerId,
            float segmentFraction,
            bool mapped,
            bool rebased)
        {
            PlaybackId = playbackId;
            LocalTime = localTime;
            ContinuousTime = continuousTime;
            Cycle = cycle;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            SegmentFraction = segmentFraction;
            Mapped = mapped;
            Rebased = rebased;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public float LocalTime { get; }
        public double ContinuousTime { get; }
        public int Cycle { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float SegmentFraction { get; }
        public bool Mapped { get; }
        public bool Rebased { get; }
    }

    public readonly struct AnimationMarkerSyncPlaybackSnapshot
    {
        public AnimationMarkerSyncPlaybackSnapshot(
            AnimationPlaybackId playbackId,
            string layerId,
            string syncGroupId,
            double rawTime,
            double effectiveTime,
            int effectiveCycle,
            string previousMarkerId,
            string nextMarkerId,
            float fraction,
            bool mapped,
            bool rebased)
        {
            PlaybackId = playbackId;
            LayerId = layerId ?? string.Empty;
            SyncGroupId = syncGroupId ?? string.Empty;
            RawTime = rawTime;
            EffectiveTime = effectiveTime;
            EffectiveCycle = effectiveCycle;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            Fraction = fraction;
            Mapped = mapped;
            Rebased = rebased;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public string LayerId { get; }
        public string SyncGroupId { get; }
        public double RawTime { get; }
        public double EffectiveTime { get; }
        public int EffectiveCycle { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float Fraction { get; }
        public bool Mapped { get; }
        public bool Rebased { get; }
    }

    public readonly struct AnimationMarkerSyncRelationSnapshot
    {
        public AnimationMarkerSyncRelationSnapshot(
            string layerId,
            string syncGroupId,
            AnimationPlaybackId source,
            AnimationPlaybackId target,
            string previousMarkerId,
            string nextMarkerId,
            float fraction,
            int targetOccurrenceIndex,
            double sourceRawTime,
            double sourceEffectiveTime,
            double targetRawTime,
            double targetEffectiveTime,
            int targetEffectiveCycle,
            int relationDepth,
            AnimationMarkerSyncSnapshotReason reason,
            AnimationPlaybackLifecyclePhase targetLifecyclePhase = AnimationPlaybackLifecyclePhase.Retired)
        {
            LayerId = layerId ?? string.Empty;
            SyncGroupId = syncGroupId ?? string.Empty;
            Source = source;
            Target = target;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            Fraction = fraction;
            TargetOccurrenceIndex = targetOccurrenceIndex;
            SourceRawTime = sourceRawTime;
            SourceEffectiveTime = sourceEffectiveTime;
            TargetRawTime = targetRawTime;
            TargetEffectiveTime = targetEffectiveTime;
            TargetEffectiveCycle = targetEffectiveCycle;
            RelationDepth = relationDepth;
            Reason = reason;
            TargetLifecyclePhase = targetLifecyclePhase;
        }

        public string LayerId { get; }
        public string SyncGroupId { get; }
        public AnimationPlaybackId Source { get; }
        public AnimationPlaybackId Target { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float Fraction { get; }
        public int TargetOccurrenceIndex { get; }
        public double SourceRawTime { get; }
        public double SourceEffectiveTime { get; }
        public double TargetRawTime { get; }
        public double TargetEffectiveTime { get; }
        public int TargetEffectiveCycle { get; }
        public int RelationDepth { get; }
        public AnimationMarkerSyncSnapshotReason Reason { get; }
        public AnimationPlaybackLifecyclePhase TargetLifecyclePhase { get; }

        public AnimationMarkerSyncRelationSnapshot WithLifecyclePhase(AnimationPlaybackLifecyclePhase phase)
        {
            return new AnimationMarkerSyncRelationSnapshot(
                LayerId,
                SyncGroupId,
                Source,
                Target,
                PreviousMarkerId,
                NextMarkerId,
                Fraction,
                TargetOccurrenceIndex,
                SourceRawTime,
                SourceEffectiveTime,
                TargetRawTime,
                TargetEffectiveTime,
                TargetEffectiveCycle,
                RelationDepth,
                Reason,
                phase);
        }
    }

    public sealed class AnimationMarkerSyncRuntime
    {
        readonly Dictionary<AnimationPlaybackId, SyncRelation> m_Relations =
            new Dictionary<AnimationPlaybackId, SyncRelation>();
        readonly Dictionary<AnimationPlaybackId, ContinuationAnchor> m_Anchors =
            new Dictionary<AnimationPlaybackId, ContinuationAnchor>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample> m_Effective =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample>();
        readonly Dictionary<AnimationPlaybackId, ApplicabilityRecord> m_Applicability =
            new Dictionary<AnimationPlaybackId, ApplicabilityRecord>();
        readonly HashSet<AnimationPlaybackId> m_Visiting = new HashSet<AnimationPlaybackId>();
        readonly HashSet<AnimationPlaybackId> m_Evaluated = new HashSet<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_Order = new List<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_Remove = new List<AnimationPlaybackId>();

        IReadOnlyDictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> m_Raw;

        public void BeginFrame()
        {
            m_Applicability.Clear();
        }

        public void RecordNoCurrentSource(AnimationMarkerSyncRawSample target)
        {
            if (m_Relations.ContainsKey(target.PlaybackId) || m_Anchors.ContainsKey(target.PlaybackId))
                return;
            m_Applicability[target.PlaybackId] = new ApplicabilityRecord(
                default,
                target.PlaybackId,
                AnimationMarkerSyncSnapshotReason.NoCurrentSource);
        }

        public AnimationMarkerSyncNotApplicableReason EnsureHandoff(
            AnimationMarkerSyncRawSample outgoing,
            AnimationMarkerSyncRawSample incoming)
        {
            AnimationMarkerSyncNotApplicableReason applicability = ValidateHandoff(outgoing, incoming);
            if (applicability != AnimationMarkerSyncNotApplicableReason.None)
                return applicability;

            AnimationMarkerSyncRole outgoingRole = outgoing.Binding.SyncRole;
            AnimationMarkerSyncRole incomingRole = incoming.Binding.SyncRole;
            if ((outgoingRole == AnimationMarkerSyncRole.AlwaysLeader &&
                 incomingRole == AnimationMarkerSyncRole.AlwaysLeader) ||
                (outgoingRole == AnimationMarkerSyncRole.AlwaysFollower &&
                 incomingRole == AnimationMarkerSyncRole.AlwaysFollower))
                throw Invalid(AnimationMarkerSyncInvalidReason.RoleConflict, incoming.PlaybackId);

            bool incomingLeads = incomingRole == AnimationMarkerSyncRole.AlwaysLeader ||
                                 outgoingRole == AnimationMarkerSyncRole.AlwaysFollower;
            if (incomingLeads)
            {
                if (m_Relations.TryGetValue(outgoing.PlaybackId, out SyncRelation existing) &&
                    existing.Source.Equals(incoming.PlaybackId))
                    return AnimationMarkerSyncNotApplicableReason.None;
                m_Relations.Remove(outgoing.PlaybackId);
                m_Anchors.Remove(outgoing.PlaybackId);
                return AddRelation(incoming, outgoing);
            }
            return AddRelation(outgoing, incoming);
        }

        AnimationMarkerSyncNotApplicableReason ValidateHandoff(
            AnimationMarkerSyncRawSample outgoing,
            AnimationMarkerSyncRawSample incoming)
        {
            if (!outgoing.PlaybackId.IsValid)
            {
                RecordNoCurrentSource(incoming);
                return AnimationMarkerSyncNotApplicableReason.MissingCurrent;
            }
            if (outgoing.PlaybackId.Equals(incoming.PlaybackId))
            {
                RecordNoCurrentSource(incoming);
                return AnimationMarkerSyncNotApplicableReason.SamePlayback;
            }
            if (outgoing.Binding == null || !outgoing.Binding.IsMarkerGroup)
            {
                m_Applicability[incoming.PlaybackId] = new ApplicabilityRecord(
                    outgoing.PlaybackId,
                    incoming.PlaybackId,
                    AnimationMarkerSyncSnapshotReason.SourceExplicitNone);
                return AnimationMarkerSyncNotApplicableReason.SyncDisabled;
            }
            if (incoming.Binding == null || !incoming.Binding.IsMarkerGroup)
            {
                m_Applicability[incoming.PlaybackId] = new ApplicabilityRecord(
                    outgoing.PlaybackId,
                    incoming.PlaybackId,
                    AnimationMarkerSyncSnapshotReason.TargetExplicitNone);
                return AnimationMarkerSyncNotApplicableReason.SyncDisabled;
            }
            if (!string.Equals(outgoing.LayerId, incoming.LayerId, StringComparison.Ordinal))
            {
                m_Applicability[incoming.PlaybackId] = new ApplicabilityRecord(
                    outgoing.PlaybackId,
                    incoming.PlaybackId,
                    AnimationMarkerSyncSnapshotReason.DifferentLayer);
                return AnimationMarkerSyncNotApplicableReason.DifferentLayer;
            }
            if (!string.Equals(outgoing.Binding.CanonicalGroupId, incoming.Binding.CanonicalGroupId, StringComparison.Ordinal))
            {
                m_Applicability[incoming.PlaybackId] = new ApplicabilityRecord(
                    outgoing.PlaybackId,
                    incoming.PlaybackId,
                    AnimationMarkerSyncSnapshotReason.DifferentGroup);
                return AnimationMarkerSyncNotApplicableReason.DifferentGroup;
            }
            return AnimationMarkerSyncNotApplicableReason.None;
        }

        AnimationMarkerSyncNotApplicableReason AddRelation(
            AnimationMarkerSyncRawSample source,
            AnimationMarkerSyncRawSample target)
        {
            if (m_Relations.TryGetValue(target.PlaybackId, out SyncRelation existing))
            {
                if (!existing.Source.Equals(source.PlaybackId))
                    throw Invalid(AnimationMarkerSyncInvalidReason.MultipleSources, target.PlaybackId);
                return AnimationMarkerSyncNotApplicableReason.None;
            }
            m_Anchors.Remove(target.PlaybackId);
            m_Relations.Add(target.PlaybackId, new SyncRelation(
                source.PlaybackId,
                target.PlaybackId,
                source.LayerId,
                source.Binding,
                target.Binding)
            {
                Created = true
            });
            return AnimationMarkerSyncNotApplicableReason.None;
        }

        public void Evaluate(
            IReadOnlyDictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> rawSamples,
            Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample> destination)
        {
            if (rawSamples == null)
                throw new ArgumentNullException(nameof(rawSamples));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            m_Raw = rawSamples;
            m_Effective.Clear();
            m_Evaluated.Clear();
            m_Visiting.Clear();
            m_Order.Clear();
            foreach (AnimationPlaybackId playbackId in rawSamples.Keys)
                m_Order.Add(playbackId);
            m_Order.Sort(ComparePlayback);
            for (int i = 0; i < m_Order.Count; i++)
                EvaluatePlayback(m_Order[i]);

            destination.Clear();
            for (int i = 0; i < m_Order.Count; i++)
            {
                AnimationPlaybackId playbackId = m_Order[i];
                if (m_Effective.TryGetValue(playbackId, out AnimationMarkerSyncEffectiveSample sample))
                    destination.Add(playbackId, sample);
            }
            m_Raw = null;
        }

        public void Retire(IReadOnlyList<AnimationPlaybackId> retiredPlaybacks)
        {
            if (retiredPlaybacks == null || retiredPlaybacks.Count == 0)
                return;

            m_Remove.Clear();
            foreach (KeyValuePair<AnimationPlaybackId, SyncRelation> pair in m_Relations)
            {
                SyncRelation relation = pair.Value;
                bool sourceRetired = Contains(retiredPlaybacks, relation.Source);
                bool targetRetired = Contains(retiredPlaybacks, relation.Target);
                if (sourceRetired && !targetRetired &&
                    m_RawSampleCache.TryGetValue(relation.Target, out AnimationMarkerSyncRawSample raw) &&
                    m_Effective.TryGetValue(relation.Target, out AnimationMarkerSyncEffectiveSample effective))
                {
                    m_Anchors[relation.Target] = new ContinuationAnchor(raw.ContinuousTime, effective.ContinuousTime);
                }
                if (sourceRetired || targetRetired)
                    m_Remove.Add(pair.Key);
            }
            for (int i = 0; i < m_Remove.Count; i++)
                m_Relations.Remove(m_Remove[i]);
            for (int i = 0; i < retiredPlaybacks.Count; i++)
            {
                AnimationPlaybackId playbackId = retiredPlaybacks[i];
                m_Anchors.Remove(playbackId);
                m_Effective.Remove(playbackId);
                m_RawSampleCache.Remove(playbackId);
            }
        }

        public void BuildRelationSnapshot(List<AnimationMarkerSyncRelationSnapshot> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (SyncRelation relation in m_Relations.Values)
            {
                m_RawSampleCache.TryGetValue(relation.Source, out AnimationMarkerSyncRawSample sourceRaw);
                m_Effective.TryGetValue(relation.Source, out AnimationMarkerSyncEffectiveSample sourceEffective);
                m_RawSampleCache.TryGetValue(relation.Target, out AnimationMarkerSyncRawSample targetRaw);
                m_Effective.TryGetValue(relation.Target, out AnimationMarkerSyncEffectiveSample targetEffective);
                destination.Add(new AnimationMarkerSyncRelationSnapshot(
                    relation.LayerId,
                    relation.TargetBinding.CanonicalGroupId,
                    relation.Source,
                    relation.Target,
                    relation.PreviousMarkerId,
                    relation.NextMarkerId,
                    relation.Fraction,
                    relation.TargetOccurrenceIndex,
                    sourceRaw.ContinuousTime,
                    sourceEffective.ContinuousTime,
                    targetRaw.ContinuousTime,
                    targetEffective.ContinuousTime,
                    targetEffective.Cycle,
                    ResolveDepth(relation),
                    relation.Created
                        ? AnimationMarkerSyncSnapshotReason.RelationCreated
                        : AnimationMarkerSyncSnapshotReason.RelationContinued));
                relation.Created = false;
            }
            foreach (KeyValuePair<AnimationPlaybackId, ContinuationAnchor> item in m_Anchors)
            {
                if (!m_RawSampleCache.TryGetValue(item.Key, out AnimationMarkerSyncRawSample targetRaw) ||
                    !m_Effective.TryGetValue(item.Key, out AnimationMarkerSyncEffectiveSample targetEffective))
                    continue;
                destination.Add(new AnimationMarkerSyncRelationSnapshot(
                    targetRaw.LayerId,
                    targetRaw.Binding?.CanonicalGroupId,
                    default,
                    item.Key,
                    targetEffective.PreviousMarkerId,
                    targetEffective.NextMarkerId,
                    targetEffective.SegmentFraction,
                    -1,
                    0d,
                    0d,
                    targetRaw.ContinuousTime,
                    targetEffective.ContinuousTime,
                    targetEffective.Cycle,
                    0,
                    AnimationMarkerSyncSnapshotReason.SourceRetiredRebased));
            }
            foreach (ApplicabilityRecord applicability in m_Applicability.Values)
            {
                if (!m_RawSampleCache.TryGetValue(applicability.Target, out AnimationMarkerSyncRawSample targetRaw) ||
                    !m_Effective.TryGetValue(applicability.Target, out AnimationMarkerSyncEffectiveSample targetEffective))
                    continue;
                destination.Add(new AnimationMarkerSyncRelationSnapshot(
                    targetRaw.LayerId,
                    targetRaw.Binding?.CanonicalGroupId,
                    applicability.Source,
                    applicability.Target,
                    targetEffective.PreviousMarkerId,
                    targetEffective.NextMarkerId,
                    targetEffective.SegmentFraction,
                    -1,
                    0d,
                    0d,
                    targetRaw.ContinuousTime,
                    targetEffective.ContinuousTime,
                    targetEffective.Cycle,
                    0,
                    applicability.Reason));
            }
            destination.Sort((left, right) => ComparePlayback(left.Target, right.Target));
        }

        public void BuildPlaybackSnapshot(List<AnimationMarkerSyncPlaybackSnapshot> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            m_Order.Clear();
            foreach (AnimationPlaybackId playbackId in m_RawSampleCache.Keys)
                m_Order.Add(playbackId);
            m_Order.Sort(ComparePlayback);
            for (int i = 0; i < m_Order.Count; i++)
            {
                AnimationPlaybackId playbackId = m_Order[i];
                if (!m_RawSampleCache.TryGetValue(playbackId, out AnimationMarkerSyncRawSample raw) ||
                    !m_Effective.TryGetValue(playbackId, out AnimationMarkerSyncEffectiveSample effective))
                    continue;
                destination.Add(new AnimationMarkerSyncPlaybackSnapshot(
                    playbackId,
                    raw.LayerId,
                    raw.Binding?.CanonicalGroupId,
                    raw.ContinuousTime,
                    effective.ContinuousTime,
                    effective.Cycle,
                    effective.PreviousMarkerId,
                    effective.NextMarkerId,
                    effective.SegmentFraction,
                    effective.Mapped,
                    effective.Rebased));
            }
        }

        int ResolveDepth(SyncRelation relation)
        {
            int depth = 1;
            AnimationPlaybackId source = relation.Source;
            while (m_Relations.TryGetValue(source, out SyncRelation parent))
            {
                depth++;
                if (depth > m_Relations.Count)
                    throw Invalid(AnimationMarkerSyncInvalidReason.RelationCycle, relation.Target);
                source = parent.Source;
            }
            return depth;
        }

        public void Reset()
        {
            m_Relations.Clear();
            m_Anchors.Clear();
            m_Effective.Clear();
            m_Applicability.Clear();
            m_RawSampleCache.Clear();
            m_Visiting.Clear();
            m_Evaluated.Clear();
            m_Order.Clear();
            m_Remove.Clear();
            m_Raw = null;
        }

        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> m_RawSampleCache =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample>();

        void EvaluatePlayback(AnimationPlaybackId playbackId)
        {
            if (m_Evaluated.Contains(playbackId))
                return;
            if (!m_Raw.TryGetValue(playbackId, out AnimationMarkerSyncRawSample raw))
                throw Invalid(AnimationMarkerSyncInvalidReason.SourceSampleMissing, playbackId);
            if (!m_Visiting.Add(playbackId))
                throw Invalid(AnimationMarkerSyncInvalidReason.RelationCycle, playbackId);

            AnimationMarkerSyncEffectiveSample effective;
            if (m_Relations.TryGetValue(playbackId, out SyncRelation relation))
            {
                if (!string.Equals(raw.LayerId, relation.LayerId, StringComparison.Ordinal))
                    throw Invalid(AnimationMarkerSyncInvalidReason.CrossLayerRelation, playbackId);
                EvaluatePlayback(relation.Source);
                if (!m_Effective.TryGetValue(relation.Source, out AnimationMarkerSyncEffectiveSample source))
                    throw Invalid(AnimationMarkerSyncInvalidReason.SourceSampleMissing, relation.Source);
                effective = Map(relation, source, raw);
            }
            else if (m_Anchors.TryGetValue(playbackId, out ContinuationAnchor anchor))
            {
                double continuous = anchor.EffectiveTime + (raw.ContinuousTime - anchor.RawTime);
                effective = Normalize(raw, continuous, false, true, string.Empty, string.Empty, 0f);
            }
            else
            {
                if (raw.Binding != null && raw.Binding.IsMarkerGroup &&
                    TryLocateSegment(raw.Binding, raw.ContinuousTime, raw.PlaybackId, out SegmentPosition position))
                {
                    effective = new AnimationMarkerSyncEffectiveSample(
                        playbackId,
                        raw.LocalTime,
                        raw.ContinuousTime,
                        raw.Cycle,
                        position.Segment.PreviousMarkerId,
                        position.Segment.NextMarkerId,
                        position.Fraction,
                        false,
                        false);
                }
                else
                {
                    effective = new AnimationMarkerSyncEffectiveSample(
                        playbackId,
                        raw.LocalTime,
                        raw.ContinuousTime,
                        raw.Cycle,
                        string.Empty,
                        string.Empty,
                        0f,
                        false,
                        false);
                }
            }

            m_Visiting.Remove(playbackId);
            m_Evaluated.Add(playbackId);
            m_Effective[playbackId] = effective;
            m_RawSampleCache[playbackId] = raw;
        }

        AnimationMarkerSyncEffectiveSample Map(
            SyncRelation relation,
            AnimationMarkerSyncEffectiveSample source,
            AnimationMarkerSyncRawSample targetRaw)
        {
            if (!TryLocateSegment(relation.SourceBinding, source.ContinuousTime, relation.Source, out SegmentPosition sourcePosition))
                throw Invalid(AnimationMarkerSyncInvalidReason.SourceSegmentMissing, relation.Source);

            if (!relation.Initialized)
            {
                relation.TargetOrdinal = SelectInitialTargetOrdinal(
                    relation.TargetBinding,
                    sourcePosition.Segment.PreviousMarkerId,
                    sourcePosition.Segment.NextMarkerId,
                    sourcePosition.Fraction,
                    targetRaw.ContinuousTime,
                    targetRaw.PlaybackId);
                relation.Initialized = true;
            }
            else if (sourcePosition.Ordinal != relation.SourceOrdinal)
            {
                if (sourcePosition.Ordinal < relation.SourceOrdinal)
                    throw Invalid(AnimationMarkerSyncInvalidReason.SourceTimeRegressed, relation.Source);
                for (long ordinal = relation.SourceOrdinal + 1; ordinal <= sourcePosition.Ordinal; ordinal++)
                {
                    AnimationMarkerSyncSegmentOccurrence sourceSegment = SegmentAtOrdinal(
                        relation.SourceBinding,
                        ordinal,
                        relation.Source,
                        out _);
                    relation.TargetOrdinal = AdvanceTargetOrdinal(
                        relation.TargetBinding,
                        relation.TargetOrdinal,
                        sourceSegment.PreviousMarkerId,
                        sourceSegment.NextMarkerId,
                        relation.Target);
                }
            }

            relation.SourceOrdinal = sourcePosition.Ordinal;
            relation.PreviousMarkerId = sourcePosition.Segment.PreviousMarkerId;
            relation.NextMarkerId = sourcePosition.Segment.NextMarkerId;
            relation.Fraction = sourcePosition.Fraction;
            AnimationMarkerSyncSegmentOccurrence target = SegmentAtOrdinal(
                relation.TargetBinding,
                relation.TargetOrdinal,
                relation.Target,
                out long targetCycle);
            relation.TargetOccurrenceIndex = target.OccurrenceIndex;
            double mapped = targetCycle * relation.TargetBinding.DurationSeconds +
                            target.StartTimeSeconds +
                            sourcePosition.Fraction * target.DurationSeconds;
            return Normalize(
                targetRaw,
                mapped,
                true,
                false,
                sourcePosition.Segment.PreviousMarkerId,
                sourcePosition.Segment.NextMarkerId,
                sourcePosition.Fraction);
        }

        static AnimationMarkerSyncEffectiveSample Normalize(
            AnimationMarkerSyncRawSample raw,
            double continuous,
            bool mapped,
            bool rebased,
            string previousMarkerId,
            string nextMarkerId,
            float fraction)
        {
            if (double.IsNaN(continuous) || double.IsInfinity(continuous))
                throw Invalid(AnimationMarkerSyncInvalidReason.NonFiniteResult, raw.PlaybackId);
            AnimationMarkerSyncBinding binding = raw.Binding;
            if (binding == null || !binding.IsMarkerGroup)
            {
                return new AnimationMarkerSyncEffectiveSample(
                    raw.PlaybackId,
                    (float)Math.Max(0d, continuous),
                    Math.Max(0d, continuous),
                    raw.Cycle,
                    previousMarkerId,
                    nextMarkerId,
                    fraction,
                    mapped,
                    rebased);
            }
            if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
            {
                if (continuous < -0.000001d || continuous > binding.DurationSeconds + 0.000001d)
                    throw Invalid(AnimationMarkerSyncInvalidReason.FiniteCoverageExceeded, raw.PlaybackId);
                double clamped = Math.Clamp(continuous, 0d, binding.DurationSeconds);
                return new AnimationMarkerSyncEffectiveSample(
                    raw.PlaybackId,
                    (float)clamped,
                    clamped,
                    0,
                    previousMarkerId,
                    nextMarkerId,
                    fraction,
                    mapped,
                    rebased);
            }
            if (binding.DurationSeconds <= 0f)
                throw Invalid(AnimationMarkerSyncInvalidReason.ProjectionInvalid, raw.PlaybackId);
            double positive = Math.Max(0d, continuous);
            int cycle = (int)Math.Floor(positive / binding.DurationSeconds);
            double local = positive - cycle * binding.DurationSeconds;
            if (local >= binding.DurationSeconds)
            {
                cycle++;
                local = 0d;
            }
            return new AnimationMarkerSyncEffectiveSample(
                raw.PlaybackId,
                (float)local,
                positive,
                cycle,
                previousMarkerId,
                nextMarkerId,
                fraction,
                mapped,
                rebased);
        }

        static bool TryLocateSegment(
            AnimationMarkerSyncBinding binding,
            double continuousTime,
            AnimationPlaybackId playbackId,
            out SegmentPosition position)
        {
            position = default;
            if (binding == null || !binding.IsMarkerGroup || binding.Segments.Count == 0)
                return false;
            if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
            {
                if (continuousTime < 0d || continuousTime > binding.DurationSeconds)
                    return false;
                for (int i = 0; i < binding.Segments.Count; i++)
                {
                    AnimationMarkerSyncSegmentOccurrence segment = binding.Segments[i];
                    if (continuousTime < segment.StartTimeSeconds ||
                        continuousTime > segment.EndTimeSeconds ||
                        i < binding.Segments.Count - 1 && continuousTime == segment.EndTimeSeconds)
                        continue;
                    position = new SegmentPosition(
                        segment,
                        i,
                        Fraction(continuousTime, segment.StartTimeSeconds, segment.EndTimeSeconds, playbackId));
                    return true;
                }
                return false;
            }

            double duration = binding.DurationSeconds;
            if (duration <= 0d || continuousTime < 0d)
                return false;
            long cycle = (long)Math.Floor(continuousTime / duration);
            double local = continuousTime - cycle * duration;
            int lastIndex = binding.Segments.Count - 1;
            AnimationMarkerSyncSegmentOccurrence wrap = binding.Segments[lastIndex];
            float firstMarkerTime = binding.Markers[0].TimeSeconds;
            if (local < firstMarkerTime)
            {
                double start = (cycle - 1) * duration + wrap.StartTimeSeconds;
                double end = (cycle - 1) * duration + wrap.EndTimeSeconds;
                position = new SegmentPosition(
                    wrap,
                    (cycle - 1) * binding.Segments.Count + lastIndex,
                    Fraction(continuousTime, start, end, playbackId));
                return true;
            }
            for (int i = 0; i < lastIndex; i++)
            {
                AnimationMarkerSyncSegmentOccurrence segment = binding.Segments[i];
                if (local < segment.StartTimeSeconds || local >= segment.EndTimeSeconds)
                    continue;
                position = new SegmentPosition(
                    segment,
                    cycle * binding.Segments.Count + i,
                    Fraction(local, segment.StartTimeSeconds, segment.EndTimeSeconds, playbackId));
                return true;
            }
            double wrapStart = cycle * duration + wrap.StartTimeSeconds;
            double wrapEnd = cycle * duration + wrap.EndTimeSeconds;
            position = new SegmentPosition(
                wrap,
                cycle * binding.Segments.Count + lastIndex,
                Fraction(continuousTime, wrapStart, wrapEnd, playbackId));
            return true;
        }

        static long SelectInitialTargetOrdinal(
            AnimationMarkerSyncBinding binding,
            string previousMarkerId,
            string nextMarkerId,
            float fraction,
            double rawTime,
            AnimationPlaybackId playbackId)
        {
            if (!binding.TryGetOccurrences(previousMarkerId, nextMarkerId, out AnimationMarkerSyncSegmentOccurrence[] occurrences) ||
                occurrences.Length == 0)
                throw Invalid(AnimationMarkerSyncInvalidReason.TargetPairMissing, playbackId);

            long bestOrdinal = -1;
            double bestDistance = double.MaxValue;
            AnimationMarkerSyncSegmentOccurrence best = null;
            for (int i = 0; i < occurrences.Length; i++)
            {
                AnimationMarkerSyncSegmentOccurrence occurrence = occurrences[i];
                if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
                {
                    double candidate = occurrence.StartTimeSeconds + fraction * occurrence.DurationSeconds;
                    SelectCandidate(binding, occurrence, occurrence.OccurrenceIndex, candidate, rawTime, ref bestOrdinal, ref bestDistance, ref best);
                    continue;
                }
                double baseTime = occurrence.StartTimeSeconds + fraction * occurrence.DurationSeconds;
                long center = (long)Math.Round((rawTime - baseTime) / binding.DurationSeconds, MidpointRounding.AwayFromZero);
                for (long cycle = center - 1; cycle <= center + 1; cycle++)
                {
                    double candidate = cycle * binding.DurationSeconds + baseTime;
                    if (candidate < 0d)
                        continue;
                    long ordinal = cycle * binding.Segments.Count + occurrence.OccurrenceIndex;
                    SelectCandidate(binding, occurrence, ordinal, candidate, rawTime, ref bestOrdinal, ref bestDistance, ref best);
                }
            }
            if (bestOrdinal < 0)
                throw Invalid(AnimationMarkerSyncInvalidReason.TargetPairMissing, playbackId);
            return bestOrdinal;
        }

        static void SelectCandidate(
            AnimationMarkerSyncBinding binding,
            AnimationMarkerSyncSegmentOccurrence occurrence,
            long ordinal,
            double candidate,
            double rawTime,
            ref long bestOrdinal,
            ref double bestDistance,
            ref AnimationMarkerSyncSegmentOccurrence best)
        {
            double distance = Math.Abs(candidate - rawTime);
            bool replace = distance < bestDistance - 0.0000001d;
            if (!replace && Math.Abs(distance - bestDistance) <= 0.0000001d && best != null)
            {
                AnimationMarkerSyncMarkerBinding candidateMarker = binding.Markers[occurrence.PreviousMarkerIndex];
                AnimationMarkerSyncMarkerBinding bestMarker = binding.Markers[best.PreviousMarkerIndex];
                replace = candidateMarker.Frame < bestMarker.Frame ||
                          candidateMarker.Frame == bestMarker.Frame &&
                          string.CompareOrdinal(candidateMarker.AuthoringId, bestMarker.AuthoringId) < 0;
            }
            if (!replace && best != null)
                return;
            bestOrdinal = ordinal;
            bestDistance = distance;
            best = occurrence;
        }

        static long AdvanceTargetOrdinal(
            AnimationMarkerSyncBinding binding,
            long currentOrdinal,
            string previousMarkerId,
            string nextMarkerId,
            AnimationPlaybackId playbackId)
        {
            int segmentCount = binding.Segments.Count;
            int attempts = binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Cyclic
                ? segmentCount
                : segmentCount - (int)currentOrdinal - 1;
            for (int offset = 1; offset <= attempts; offset++)
            {
                long ordinal = currentOrdinal + offset;
                AnimationMarkerSyncSegmentOccurrence segment = SegmentAtOrdinal(binding, ordinal, playbackId, out _);
                if (string.Equals(segment.PreviousMarkerId, previousMarkerId, StringComparison.Ordinal) &&
                    string.Equals(segment.NextMarkerId, nextMarkerId, StringComparison.Ordinal))
                    return ordinal;
            }
            throw Invalid(AnimationMarkerSyncInvalidReason.FiniteCoverageExceeded, playbackId);
        }

        static AnimationMarkerSyncSegmentOccurrence SegmentAtOrdinal(
            AnimationMarkerSyncBinding binding,
            long ordinal,
            AnimationPlaybackId playbackId,
            out long cycle)
        {
            int count = binding.Segments.Count;
            if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
            {
                if (ordinal < 0 || ordinal >= count)
                    throw Invalid(AnimationMarkerSyncInvalidReason.FiniteCoverageExceeded, playbackId);
                cycle = 0;
                return binding.Segments[(int)ordinal];
            }
            cycle = FloorDiv(ordinal, count);
            int index = (int)(ordinal - cycle * count);
            return binding.Segments[index];
        }

        static long FloorDiv(long value, int divisor)
        {
            long result = value / divisor;
            if (value < 0 && value % divisor != 0)
                result--;
            return result;
        }

        static float Fraction(
            double value,
            double start,
            double end,
            AnimationPlaybackId playbackId)
        {
            double duration = end - start;
            if (duration <= 0d)
                throw Invalid(AnimationMarkerSyncInvalidReason.ProjectionInvalid, playbackId);
            return (float)Math.Clamp((value - start) / duration, 0d, 1d);
        }

        static bool Contains(IReadOnlyList<AnimationPlaybackId> values, AnimationPlaybackId target)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Equals(target))
                    return true;
            }
            return false;
        }

        static int ComparePlayback(AnimationPlaybackId left, AnimationPlaybackId right)
        {
            int generation = left.Generation.CompareTo(right.Generation);
            if (generation != 0)
                return generation;
            int timeline = string.CompareOrdinal(left.ProducerId.TimelineAuthoringId, right.ProducerId.TimelineAuthoringId);
            return timeline != 0
                ? timeline
                : string.CompareOrdinal(left.ProducerId.TrackAuthoringId, right.ProducerId.TrackAuthoringId);
        }

        static AnimationMarkerSyncException Invalid(
            AnimationMarkerSyncInvalidReason reason,
            AnimationPlaybackId playbackId)
        {
            return new AnimationMarkerSyncException(reason, playbackId);
        }

        readonly struct SegmentPosition
        {
            public SegmentPosition(AnimationMarkerSyncSegmentOccurrence segment, long ordinal, float fraction)
            {
                Segment = segment;
                Ordinal = ordinal;
                Fraction = fraction;
            }

            public AnimationMarkerSyncSegmentOccurrence Segment { get; }
            public long Ordinal { get; }
            public float Fraction { get; }
        }

        readonly struct ApplicabilityRecord
        {
            public ApplicabilityRecord(
                AnimationPlaybackId source,
                AnimationPlaybackId target,
                AnimationMarkerSyncSnapshotReason reason)
            {
                Source = source;
                Target = target;
                Reason = reason;
            }

            public AnimationPlaybackId Source { get; }
            public AnimationPlaybackId Target { get; }
            public AnimationMarkerSyncSnapshotReason Reason { get; }
        }

        readonly struct ContinuationAnchor
        {
            public ContinuationAnchor(double rawTime, double effectiveTime)
            {
                RawTime = rawTime;
                EffectiveTime = effectiveTime;
            }

            public double RawTime { get; }
            public double EffectiveTime { get; }
        }

        sealed class SyncRelation
        {
            public SyncRelation(
                AnimationPlaybackId source,
                AnimationPlaybackId target,
                string layerId,
                AnimationMarkerSyncBinding sourceBinding,
                AnimationMarkerSyncBinding targetBinding)
            {
                Source = source;
                Target = target;
                LayerId = layerId;
                SourceBinding = sourceBinding;
                TargetBinding = targetBinding;
            }

            public AnimationPlaybackId Source { get; }
            public AnimationPlaybackId Target { get; }
            public string LayerId { get; }
            public AnimationMarkerSyncBinding SourceBinding { get; }
            public AnimationMarkerSyncBinding TargetBinding { get; }
            public bool Initialized { get; set; }
            public long SourceOrdinal { get; set; } = long.MinValue;
            public long TargetOrdinal { get; set; }
            public int TargetOccurrenceIndex { get; set; }
            public string PreviousMarkerId { get; set; } = string.Empty;
            public string NextMarkerId { get; set; } = string.Empty;
            public float Fraction { get; set; }
            public bool Created { get; set; }
        }
    }
}
