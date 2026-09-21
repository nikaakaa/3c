using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics
{
    public enum RuntimeDiagnosticsInterestKind
    {
        LiveState,
        Capture
    }

    public enum RuntimeDiagnosticsCaptureDetail
    {
        None,
        Boundary,
        Evaluation,
        Continuous
    }

    public readonly struct RuntimeDiagnosticsInterest
    {
        public RuntimeDiagnosticsInterest(
            RuntimeDiagnosticsInterestKind kind,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail captureDetail = RuntimeDiagnosticsCaptureDetail.None)
        {
            Kind = kind;
            Channels = channels & RuntimeTraceChannel.All;
            CaptureDetail = kind == RuntimeDiagnosticsInterestKind.Capture
                ? captureDetail
                : RuntimeDiagnosticsCaptureDetail.None;
        }

        public RuntimeDiagnosticsInterestKind Kind { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail CaptureDetail { get; }
        public bool IsValid => Channels != RuntimeTraceChannel.None &&
                               (Kind != RuntimeDiagnosticsInterestKind.Capture || CaptureDetail != RuntimeDiagnosticsCaptureDetail.None);
    }

    public readonly struct RuntimeDiagnosticsInterestHandle : IEquatable<RuntimeDiagnosticsInterestHandle>
    {
        internal RuntimeDiagnosticsInterestHandle(Guid value)
        {
            Value = value;
        }

        internal Guid Value { get; }
        public bool IsValid => Value != Guid.Empty;
        public bool Equals(RuntimeDiagnosticsInterestHandle other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is RuntimeDiagnosticsInterestHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct RuntimeLiveStateKey : IEquatable<RuntimeLiveStateKey>
    {
        public RuntimeLiveStateKey(RuntimeTraceChannel channel, RuntimeSourceElementHandle source, RuntimeInstanceKey instance, RuntimeTraceEventKind kind)
        {
            Channel = channel;
            Source = source;
            Instance = instance;
            Kind = kind;
        }

        public RuntimeTraceChannel Channel { get; }
        public RuntimeSourceElementHandle Source { get; }
        public RuntimeInstanceKey Instance { get; }
        public RuntimeTraceEventKind Kind { get; }

        public bool Equals(RuntimeLiveStateKey other)
        {
            return Channel == other.Channel && Source.Equals(other.Source) && Instance.Equals(other.Instance) && Kind == other.Kind;
        }

        public override bool Equals(object obj) => obj is RuntimeLiveStateKey other && Equals(other);
        public override int GetHashCode() => (((int)Channel * 397) ^ Source.GetHashCode()) * 397 ^ Instance.GetHashCode() ^ (int)Kind;
    }

    public readonly struct RuntimeLiveStateChange
    {
        public RuntimeLiveStateChange(long revision, RuntimeLiveStateKey key, RuntimeTraceEvent traceEvent)
        {
            Revision = revision;
            Key = key;
            TraceEvent = traceEvent;
        }

        public long Revision { get; }
        public RuntimeLiveStateKey Key { get; }
        public RuntimeTraceEvent TraceEvent { get; }
    }

    public readonly struct RuntimeLiveStateRead
    {
        public RuntimeLiveStateRead(long version, bool requiresFullSync, IReadOnlyList<RuntimeLiveStateChange> changes, long evictedStates = 0)
        {
            Version = version;
            RequiresFullSync = requiresFullSync;
            Changes = changes ?? Array.Empty<RuntimeLiveStateChange>();
            EvictedStates = evictedStates;
        }

        public long Version { get; }
        public bool RequiresFullSync { get; }
        public IReadOnlyList<RuntimeLiveStateChange> Changes { get; }
        public long EvictedStates { get; }
    }

    public readonly struct RuntimeCaptureChange
    {
        public RuntimeCaptureChange(long revision, RuntimeTraceEvent traceEvent)
        {
            Revision = revision;
            TraceEvent = traceEvent;
        }

        public long Revision { get; }
        public RuntimeTraceEvent TraceEvent { get; }
    }

    public readonly struct RuntimeCaptureRead
    {
        public RuntimeCaptureRead(long version, bool requiresFullSync, IReadOnlyList<RuntimeCaptureChange> changes)
        {
            Version = version;
            RequiresFullSync = requiresFullSync;
            Changes = changes ?? Array.Empty<RuntimeCaptureChange>();
        }

        public long Version { get; }
        public bool RequiresFullSync { get; }
        public IReadOnlyList<RuntimeCaptureChange> Changes { get; }
    }

    public sealed class RuntimeCaptureSegmentSnapshot
    {
        public RuntimeCaptureSegmentSnapshot(RuntimeTraceDomain domain, ulong position, IReadOnlyList<RuntimeTraceEvent> events)
        {
            Domain = domain;
            Position = position;
            Events = events ?? Array.Empty<RuntimeTraceEvent>();
        }

        public RuntimeTraceDomain Domain { get; }
        public ulong Position { get; }
        public IReadOnlyList<RuntimeTraceEvent> Events { get; }
    }

    public sealed class RuntimeCaptureSnapshot
    {
        readonly IReadOnlyList<RuntimeCaptureSegmentSnapshot> m_Segments;

        public RuntimeCaptureSnapshot(Guid captureId, RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, IReadOnlyList<RuntimeCaptureSegmentSnapshot> segments, long version, long evictedEvents = 0)
        {
            CaptureId = captureId;
            Channels = channels & RuntimeTraceChannel.All;
            Detail = detail;
            m_Segments = segments ?? Array.Empty<RuntimeCaptureSegmentSnapshot>();
            Version = version;
            EvictedEvents = evictedEvents;
        }

        public Guid CaptureId { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail Detail { get; }
        public long Version { get; }
        public IReadOnlyList<RuntimeCaptureSegmentSnapshot> Segments => m_Segments;
        public long EvictedEvents { get; }
        public int SegmentCount => m_Segments.Count;

        public IReadOnlyList<RuntimeTraceEvent> GetEvents(int historyOffset)
        {
            int visibleSegments = Math.Max(0, m_Segments.Count - Math.Max(0, historyOffset));
            var events = new List<RuntimeTraceEvent>();
            for (int i = 0; i < visibleSegments; i++)
            {
                IReadOnlyList<RuntimeTraceEvent> segmentEvents = m_Segments[i].Events;
                for (int j = 0; j < segmentEvents.Count; j++)
                    events.Add(segmentEvents[j]);
            }
            return events;
        }
    }

    sealed class RuntimeLiveStateStore
    {
        readonly Dictionary<RuntimeLiveStateKey, RuntimeTraceEvent> m_Current = new Dictionary<RuntimeLiveStateKey, RuntimeTraceEvent>();
        readonly Queue<RuntimeLiveStateChange> m_Changes = new();
        readonly LinkedList<RuntimeLiveStateKey> m_Recency = new();
        readonly Dictionary<RuntimeLiveStateKey, LinkedListNode<RuntimeLiveStateKey>> m_RecencyNodes = new();
        readonly int m_MaxChanges;
        long m_Version;
        long m_LastEvictionVersion;
        long m_EvictedStates;

        public RuntimeLiveStateStore(int maxChanges = 4096)
        {
            if (maxChanges < 64)
                throw new ArgumentOutOfRangeException(nameof(maxChanges));
            m_MaxChanges = maxChanges;
        }

        public long Version => m_Version;

        public bool Upsert(RuntimeTraceEvent traceEvent)
        {
            var key = new RuntimeLiveStateKey(traceEvent.Channel, traceEvent.Source, traceEvent.RuntimeInstance, traceEvent.Kind);
            if (m_Current.TryGetValue(key, out RuntimeTraceEvent current))
            {
                LinkedListNode<RuntimeLiveStateKey> recent = m_RecencyNodes[key];
                m_Recency.Remove(recent);
                m_Recency.AddLast(recent);
                if (StateEquivalent(current, traceEvent))
                    return false;
            }
            else
            {
                LinkedListNode<RuntimeLiveStateKey> recent;
                if (m_Current.Count == m_MaxChanges)
                {
                    recent = m_Recency.First;
                    RuntimeLiveStateKey oldest = recent.Value;
                    m_Recency.RemoveFirst();
                    m_RecencyNodes.Remove(oldest);
                    m_Current.Remove(oldest);
                    m_LastEvictionVersion = m_Version + 1;
                    m_EvictedStates++;
                    recent.Value = key;
                    m_Recency.AddLast(recent);
                }
                else
                {
                    recent = m_Recency.AddLast(key);
                }
                m_RecencyNodes.Add(key, recent);
            }

            m_Version++;
            m_Current[key] = traceEvent;
            m_Changes.Enqueue(new RuntimeLiveStateChange(m_Version, key, traceEvent));
            if (m_Changes.Count > m_MaxChanges)
                m_Changes.Dequeue();
            return true;
        }

        public RuntimeLiveStateRead ReadSince(long cursor)
        {
            if (cursor >= m_Version)
                return new RuntimeLiveStateRead(m_Version, false, Array.Empty<RuntimeLiveStateChange>(), m_EvictedStates);

            long earliestAvailable = m_Changes.Count > 0 ? m_Changes.Peek().Revision : m_Version + 1;
            if (cursor < earliestAvailable - 1 || cursor < m_LastEvictionVersion)
            {
                var current = new RuntimeLiveStateChange[m_Current.Count];
                int index = 0;
                foreach (KeyValuePair<RuntimeLiveStateKey, RuntimeTraceEvent> pair in m_Current)
                    current[index++] = new RuntimeLiveStateChange(m_Version, pair.Key, pair.Value);
                return new RuntimeLiveStateRead(m_Version, true, current, m_EvictedStates);
            }

            var changes = new RuntimeLiveStateChange[checked((int)(m_Version - cursor))];
            int changeIndex = 0;
            foreach (RuntimeLiveStateChange change in m_Changes)
            {
                if (change.Revision > cursor)
                    changes[changeIndex++] = change;
            }
            return new RuntimeLiveStateRead(m_Version, false, changes, m_EvictedStates);
        }

        public void Clear()
        {
            m_Current.Clear();
            m_Changes.Clear();
            m_Recency.Clear();
            m_RecencyNodes.Clear();
            m_EvictedStates = 0;
            m_Version++;
            m_LastEvictionVersion = m_Version;
        }

        static bool StateEquivalent(in RuntimeTraceEvent left, in RuntimeTraceEvent right)
        {
            if ((left.Kind is RuntimeTraceEventKind.ValueSampled or RuntimeTraceEventKind.ValueSamplingLimited or
                RuntimeTraceEventKind.NodeRunning or RuntimeTraceEventKind.NodeWaiting or
                RuntimeTraceEventKind.EdgeSelected or RuntimeTraceEventKind.EdgeEvaluated or
                RuntimeTraceEventKind.StateTransitionSelected or RuntimeTraceEventKind.StateTransitionEvaluated) && left.Position != right.Position)
                return false;
            RuntimeTracePayload leftPayload = left.Payload;
            RuntimeTracePayload rightPayload = right.Payload;
            return left.Channel == right.Channel &&
                   left.Domain == right.Domain &&
                   left.ExecutionBranchId == right.ExecutionBranchId &&
                   left.RuntimeInstance.Equals(right.RuntimeInstance) &&
                   left.Source.Equals(right.Source) &&
                   left.Kind == right.Kind &&
                   PayloadEquivalent(in leftPayload, in rightPayload);
        }

        static bool PayloadEquivalent(in RuntimeTracePayload left, in RuntimeTracePayload right)
        {
            return string.Equals(left.Status, right.Status, StringComparison.Ordinal) &&
                   string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
                   string.Equals(left.Detail, right.Detail, StringComparison.Ordinal) &&
                   string.Equals(left.Cause, right.Cause, StringComparison.Ordinal) &&
                   string.Equals(left.SourceClockId, right.SourceClockId, StringComparison.Ordinal) &&
                   string.Equals(left.SourceTickKind, right.SourceTickKind, StringComparison.Ordinal) &&
                   string.Equals(left.AnimationChannelId, right.AnimationChannelId, StringComparison.Ordinal) &&
                   string.Equals(left.OwnerId, right.OwnerId, StringComparison.Ordinal) &&
                   string.Equals(left.RelatedElementId, right.RelatedElementId, StringComparison.Ordinal) &&
                   string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal) &&
                   left.InputSequence == right.InputSequence &&
                   string.Equals(left.CharacterStateHash, right.CharacterStateHash, StringComparison.Ordinal) &&
                   string.Equals(left.WorldHash, right.WorldHash, StringComparison.Ordinal) &&
                   left.ActionInstanceId == right.ActionInstanceId &&
                   string.Equals(left.CallSiteId, right.CallSiteId, StringComparison.Ordinal) &&
                   left.ActivationGeneration == right.ActivationGeneration &&
                   left.SkillExecutionGeneration == right.SkillExecutionGeneration &&
                   left.GraphInvocationGeneration == right.GraphInvocationGeneration &&
                   left.ParentInvocationGeneration == right.ParentInvocationGeneration &&
                   left.Time.Equals(right.Time) &&
                   left.SecondaryTime.Equals(right.SecondaryTime) &&
                   left.NormalizedTime.Equals(right.NormalizedTime) &&
                   left.Weight.Equals(right.Weight) &&
                   left.FinalWeight.Equals(right.FinalWeight) &&
                   left.Priority == right.Priority &&
                   left.Cycle == right.Cycle &&
                   left.TrackIndex == right.TrackIndex &&
                   left.ClipIndex == right.ClipIndex &&
                   left.Flag == right.Flag &&
                   DebugValueEquivalent(in left.Value, in right.Value) &&
                   TimelineProvenanceEquivalent(in left.TimelinePlayback, in right.TimelinePlayback) &&
                   FootPlacementEquivalent(in left.FootPlacement, in right.FootPlacement);
        }

        static bool FootPlacementEquivalent(
            in RuntimeFootPlacementTraceSnapshot left,
            in RuntimeFootPlacementTraceSnapshot right)
        {
            return left.IsAvailable == right.IsAvailable &&
                   left.FrameSequence == right.FrameSequence &&
                   left.CompletionIdentity == right.CompletionIdentity &&
                   FootPlacementFootEquivalent(in left.Left, in right.Left) &&
                   FootPlacementFootEquivalent(in left.Right, in right.Right);
        }

        static bool FootPlacementFootEquivalent(
            in RuntimeFootPlacementFootTraceSnapshot left,
            in RuntimeFootPlacementFootTraceSnapshot right)
        {
            return left.IsAvailable == right.IsAvailable &&
                   string.Equals(left.Side, right.Side, StringComparison.Ordinal) &&
                   string.Equals(left.State, right.State, StringComparison.Ordinal) &&
                   string.Equals(left.RejectReason, right.RejectReason, StringComparison.Ordinal) &&
                   string.Equals(left.StepSource, right.StepSource, StringComparison.Ordinal) &&
                   left.LandingEventIdentity == right.LandingEventIdentity &&
                   left.TrajectoryGeneration == right.TrajectoryGeneration &&
                   left.LandingConfidence.Equals(right.LandingConfidence) &&
                   left.TimeToLandingSeconds.Equals(right.TimeToLandingSeconds) &&
                   left.RootLocalLanding.Equals(right.RootLocalLanding) &&
                   left.CurrentAnimatedSole.Equals(right.CurrentAnimatedSole) &&
                   left.RawLanding.Equals(right.RawLanding) &&
                   left.QueryAvailable == right.QueryAvailable &&
                   string.Equals(left.QueryShape, right.QueryShape, StringComparison.Ordinal) &&
                   string.Equals(left.QueryPurpose, right.QueryPurpose, StringComparison.Ordinal) &&
                   left.QueryOrigin.Equals(right.QueryOrigin) &&
                   left.QueryDirection.Equals(right.QueryDirection) &&
                   left.QueryRadius.Equals(right.QueryRadius) &&
                   left.QueryMaximumDistance.Equals(right.QueryMaximumDistance) &&
                   left.QueryLayerMask == right.QueryLayerMask &&
                   left.QueryMinimumGroundNormalDot.Equals(right.QueryMinimumGroundNormalDot) &&
                   left.Accepted == right.Accepted &&
                   left.SurfaceIdentity == right.SurfaceIdentity &&
                   left.LandingPoint.Equals(right.LandingPoint) &&
                   left.LandingNormal.Equals(right.LandingNormal) &&
                   left.QueryDistance.Equals(right.QueryDistance);
        }

        static bool DebugValueEquivalent(in DebugValueSnapshot left, in DebugValueSnapshot right)
        {
            return left.Kind == right.Kind &&
                   left.Boolean == right.Boolean &&
                   left.Signed == right.Signed &&
                   left.Unsigned == right.Unsigned &&
                   left.Number.Equals(right.Number) &&
                   string.Equals(left.Text, right.Text, StringComparison.Ordinal) &&
                   left.Vector.Equals(right.Vector);
        }

        static bool TimelineProvenanceEquivalent(in RuntimeTimelinePlaybackProvenance left, in RuntimeTimelinePlaybackProvenance right)
        {
            return string.Equals(left.SourceGraphAuthoringId, right.SourceGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(left.SourceNodeAuthoringId, right.SourceNodeAuthoringId, StringComparison.Ordinal) &&
                   left.SourceGraphRuntimeId.Equals(right.SourceGraphRuntimeId) &&
                   left.SourceActivationGeneration == right.SourceActivationGeneration &&
                   string.Equals(left.StateMachineGraphAuthoringId, right.StateMachineGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(left.StateId, right.StateId, StringComparison.Ordinal) &&
                   left.StateMachineGraphRuntimeId.Equals(right.StateMachineGraphRuntimeId) &&
                   left.StateActivationGeneration == right.StateActivationGeneration &&
                   string.Equals(left.SourceName, right.SourceName, StringComparison.Ordinal) &&
                   string.Equals(left.SourceInvocationPath, right.SourceInvocationPath, StringComparison.Ordinal) &&
                   left.SourceOperationIndex == right.SourceOperationIndex &&
                   left.SkillExecutionGeneration == right.SkillExecutionGeneration;
        }
    }

    sealed class RuntimeCaptureStore : IDisposable
    {
        readonly List<Segment> m_Segments = new List<Segment>();
        readonly Segment[] m_SegmentPool;
        readonly RuntimeCaptureChange[] m_ChangeBuffer;
        readonly int m_MaxSegments;
        readonly Guid m_CaptureId;
        RuntimeDiagnosticsCaptureDetail m_Detail;
        long m_Version;

        readonly int m_MaxEvents;
        long m_EvictedEvents;
        long m_LastEvictionVersion;
        int m_SegmentPoolCount;
        int m_ChangeHead;
        int m_ChangeCount;

        public RuntimeCaptureStore(Guid captureId, RuntimeDiagnosticsCaptureDetail detail, int maxSegments = 512, int maxEvents = 32768)
        {
            if (maxSegments < 8)
                throw new ArgumentOutOfRangeException(nameof(maxSegments));
            if (maxEvents < 64)
                throw new ArgumentOutOfRangeException(nameof(maxEvents));
            m_CaptureId = captureId;
            m_Detail = detail;
            m_MaxSegments = maxSegments;
            m_MaxEvents = maxEvents;
            m_SegmentPool = new Segment[m_MaxSegments];
            m_ChangeBuffer = new RuntimeCaptureChange[m_MaxEvents + 1];
        }

        public Guid CaptureId => m_CaptureId;
        public RuntimeDiagnosticsCaptureDetail Detail => m_Detail;
        public long Version => m_Version;
        public int SegmentCount => m_Segments.Count;
        public int MaxSegments => m_MaxSegments;

        public void UpgradeDetail(RuntimeDiagnosticsCaptureDetail detail)
        {
            if (detail > m_Detail)
                m_Detail = detail;
        }

        public void Publish(RuntimeTraceEvent traceEvent)
        {
            Segment segment = m_Segments.Count > 0 ? m_Segments[m_Segments.Count - 1] : null;
            if (segment == null || segment.Domain != traceEvent.Domain || segment.Position != traceEvent.Position)
            {
                segment = AcquireSegment(traceEvent.Domain, traceEvent.Position);
                m_Segments.Add(segment);
            }

            m_Version++;
            var change = new RuntimeCaptureChange(m_Version, traceEvent);
            if (segment.Events.Count == m_MaxEvents)
            {
                m_EvictedEvents++;
                m_LastEvictionVersion = m_Version;
                return;
            }
            segment.Events.Add(change);
            AppendChange(change);
            TrimToCapacity();
        }

        public RuntimeCaptureRead ReadSince(long cursor)
        {
            if (cursor >= m_Version)
                return new RuntimeCaptureRead(m_Version, false, Array.Empty<RuntimeCaptureChange>());

            long earliestAvailable = m_ChangeCount > 0 ? GetChange(0).Revision : m_Version + 1;
            if (cursor < earliestAvailable - 1 || cursor < m_LastEvictionVersion)
                return new RuntimeCaptureRead(m_Version, true, CopyAllChanges());

            int first = 0;
            int end = m_ChangeCount;
            while (first < end)
            {
                int middle = first + (end - first) / 2;
                if (GetChange(middle).Revision <= cursor)
                    first = middle + 1;
                else
                    end = middle;
            }
            var changes = new RuntimeCaptureChange[m_ChangeCount - first];
            for (int i = first; i < m_ChangeCount; i++)
                changes[i - first] = GetChange(i);
            return new RuntimeCaptureRead(m_Version, false, changes);
        }

        public RuntimeCaptureSnapshot Freeze(RuntimeTraceChannel channels)
        {
            var segments = new List<RuntimeCaptureSegmentSnapshot>(m_Segments.Count);
            for (int i = 0; i < m_Segments.Count; i++)
            {
                Segment source = m_Segments[i];
                var events = new RuntimeTraceEvent[source.Events.Count];
                for (int j = 0; j < source.Events.Count; j++)
                    events[j] = source.Events[j].TraceEvent;
                segments.Add(new RuntimeCaptureSegmentSnapshot(source.Domain, source.Position, events));
            }
            return new RuntimeCaptureSnapshot(m_CaptureId, channels, m_Detail, segments, m_Version, m_EvictedEvents);
        }

        public void Dispose()
        {
            m_Segments.Clear();
            ClearChanges();
        }

        void TrimToCapacity()
        {
            while (m_Segments.Count > m_MaxSegments || m_ChangeCount > m_MaxEvents)
            {
                Segment removed = m_Segments[0];
                m_Segments.RemoveAt(0);
                m_EvictedEvents += removed.Events.Count;
                m_LastEvictionVersion = m_Version;
                if (removed.Events.Count > 0)
                    RemoveFirstChanges(removed.Events.Count);
                ReturnSegment(removed);
            }
        }

        RuntimeCaptureChange[] CopyAllChanges()
        {
            var changes = new RuntimeCaptureChange[m_ChangeCount];
            for (int i = 0; i < m_ChangeCount; i++)
                changes[i] = GetChange(i);
            return changes;
        }

        void AppendChange(RuntimeCaptureChange change)
        {
            int index = (m_ChangeHead + m_ChangeCount) % m_ChangeBuffer.Length;
            m_ChangeBuffer[index] = change;
            m_ChangeCount++;
        }

        RuntimeCaptureChange GetChange(int index) =>
            m_ChangeBuffer[(m_ChangeHead + index) % m_ChangeBuffer.Length];

        void RemoveFirstChanges(int count)
        {
            for (int i = 0; i < count; i++)
            {
                m_ChangeBuffer[m_ChangeHead] = default;
                m_ChangeHead = (m_ChangeHead + 1) % m_ChangeBuffer.Length;
                m_ChangeCount--;
            }
        }

        void ClearChanges()
        {
            Array.Clear(m_ChangeBuffer, 0, m_ChangeBuffer.Length);
            m_ChangeHead = 0;
            m_ChangeCount = 0;
        }

        Segment AcquireSegment(RuntimeTraceDomain domain, ulong position)
        {
            Segment segment = m_SegmentPoolCount > 0 ? m_SegmentPool[--m_SegmentPoolCount] : new Segment();
            segment.Reset(domain, position);
            return segment;
        }

        void ReturnSegment(Segment segment)
        {
            segment.Events.Clear();
            m_SegmentPool[m_SegmentPoolCount++] = segment;
        }

        sealed class Segment
        {
            public RuntimeTraceDomain Domain { get; private set; }
            public ulong Position { get; private set; }
            public List<RuntimeCaptureChange> Events { get; } = new List<RuntimeCaptureChange>();

            public void Reset(RuntimeTraceDomain domain, ulong position)
            {
                Domain = domain;
                Position = position;
            }
        }
    }

    public sealed class RuntimeDiagnosticsStore : IDisposable
    {
        readonly object m_Gate = new object();
        readonly Dictionary<Guid, RuntimeDiagnosticsInterest> m_Interests = new Dictionary<Guid, RuntimeDiagnosticsInterest>();
        readonly RuntimeLiveStateStore m_LiveState = new RuntimeLiveStateStore();
        RuntimeCaptureStore m_Capture;
        RuntimeTraceChannel m_LiveChannels;
        RuntimeTraceChannel m_CaptureChannels;
        RuntimeDiagnosticsCaptureDetail m_CaptureDetail;
        bool m_Terminated;
        bool m_Disposed;

        public RuntimeTraceChannel EffectiveChannels
        {
            get
            {
                lock (m_Gate)
                    return m_LiveChannels | m_CaptureChannels;
            }
        }

        public bool IsCaptureRecording
        {
            get
            {
                lock (m_Gate)
                    return m_Capture != null;
            }
        }

        public int CaptureSegmentCount
        {
            get
            {
                lock (m_Gate)
                    return m_Capture?.SegmentCount ?? 0;
            }
        }

        public int CaptureSegmentCapacity
        {
            get
            {
                lock (m_Gate)
                    return m_Capture?.MaxSegments ?? 0;
            }
        }

        public RuntimeDiagnosticsInterestHandle AcquireInterest(RuntimeDiagnosticsInterest interest)
        {
            if (!interest.IsValid)
                throw new ArgumentException("A diagnostics interest requires channels and a valid capture detail.", nameof(interest));

            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return default;
                Guid value = Guid.NewGuid();
                m_Interests.Add(value, interest);
                RecalculateLiveChannels();
                return new RuntimeDiagnosticsInterestHandle(value);
            }
        }

        public bool ReleaseInterest(RuntimeDiagnosticsInterestHandle handle)
        {
            if (!handle.IsValid)
                return false;

            lock (m_Gate)
            {
                if (!m_Interests.Remove(handle.Value))
                    return false;
                RecalculateLiveChannels();
                return true;
            }
        }

        public bool IsInterested(RuntimeTraceChannel channel, RuntimeTraceEventKind kind)
        {
            lock (m_Gate)
                return !m_Disposed && !m_Terminated && ((m_LiveChannels & channel) != 0 ||
                    m_Capture != null && (m_CaptureChannels & channel) != 0 && m_CaptureDetail >= RequiredCaptureDetail(kind));
        }

        public bool BeginCapture(RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, out Guid captureId)
        {
            channels &= RuntimeTraceChannel.All;
            if (channels == RuntimeTraceChannel.None || detail == RuntimeDiagnosticsCaptureDetail.None)
            {
                captureId = Guid.Empty;
                return false;
            }

            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                {
                    captureId = Guid.Empty;
                    return false;
                }

                if (m_Capture == null)
                {
                    captureId = Guid.NewGuid();
                    m_Capture = new RuntimeCaptureStore(captureId, detail);
                    m_CaptureChannels = channels;
                    m_CaptureDetail = detail;
                    return true;
                }

                captureId = m_Capture.CaptureId;
                m_CaptureChannels |= channels;
                if (detail > m_CaptureDetail)
                {
                    m_CaptureDetail = detail;
                    m_Capture.UpgradeDetail(detail);
                }
                return true;
            }
        }

        public RuntimeCaptureSnapshot EndCapture()
        {
            lock (m_Gate)
            {
                if (m_Capture == null)
                    return null;

                RuntimeCaptureSnapshot snapshot = m_Capture.Freeze(m_CaptureChannels);
                m_Capture.Dispose();
                m_Capture = null;
                m_CaptureChannels = RuntimeTraceChannel.None;
                m_CaptureDetail = RuntimeDiagnosticsCaptureDetail.None;
                return snapshot;
            }
        }

        public RuntimeCaptureSnapshot FreezeActiveCapture()
        {
            lock (m_Gate)
                return m_Capture?.Freeze(m_CaptureChannels);
        }

        public RuntimeLiveStateRead ReadLiveStateSince(long cursor)
        {
            lock (m_Gate)
                return m_LiveState.ReadSince(cursor);
        }

        public RuntimeCaptureRead ReadCaptureSince(long cursor)
        {
            lock (m_Gate)
            {
                return m_Capture != null
                    ? m_Capture.ReadSince(cursor)
                    : new RuntimeCaptureRead(0, false, Array.Empty<RuntimeCaptureChange>());
            }
        }

        public bool ShouldPublish(RuntimeTraceChannel channel, RuntimeTraceEventKind kind)
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return false;
                bool live = (m_LiveChannels & channel) != 0;
                bool capture = m_Capture != null &&
                               (m_CaptureChannels & channel) != 0 &&
                               m_CaptureDetail >= RequiredCaptureDetail(kind);
                return live || capture;
            }
        }

        public bool ShouldCapture(RuntimeTraceChannel channel, RuntimeTraceEventKind kind)
        {
            lock (m_Gate)
            {
                return !m_Disposed &&
                       !m_Terminated &&
                       m_Capture != null &&
                       (m_CaptureChannels & channel) != 0 &&
                       m_CaptureDetail >= RequiredCaptureDetail(kind);
            }
        }

        public void Publish(RuntimeTraceEvent traceEvent)
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return;

                bool live = (m_LiveChannels & traceEvent.Channel) != 0;
                bool capture = m_Capture != null &&
                               (m_CaptureChannels & traceEvent.Channel) != 0 &&
                               m_CaptureDetail >= RequiredCaptureDetail(traceEvent.Kind);
                if (!live && !capture)
                    return;

                if (live)
                    m_LiveState.Upsert(traceEvent);
                if (capture)
                    m_Capture.Publish(traceEvent);
            }
        }

        public void Terminate()
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return;
                m_Terminated = true;
                m_Interests.Clear();
                m_LiveChannels = RuntimeTraceChannel.None;
            }
        }

        public void ClearLiveState()
        {
            lock (m_Gate)
            {
                if (m_Disposed)
                    return;
                m_LiveState.Clear();
            }
        }

        public void Dispose()
        {
            lock (m_Gate)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Terminated = true;
                m_Interests.Clear();
                m_LiveChannels = RuntimeTraceChannel.None;
                m_CaptureChannels = RuntimeTraceChannel.None;
                m_CaptureDetail = RuntimeDiagnosticsCaptureDetail.None;
                m_Capture?.Dispose();
                m_Capture = null;
                m_LiveState.Clear();
            }
        }

        static RuntimeDiagnosticsCaptureDetail RequiredCaptureDetail(RuntimeTraceEventKind kind)
        {
            return kind switch
            {
                RuntimeTraceEventKind.EdgeEvaluated or
                RuntimeTraceEventKind.ConditionGraphEvaluated or
                RuntimeTraceEventKind.StateTransitionEvaluated => RuntimeDiagnosticsCaptureDetail.Evaluation,
                RuntimeTraceEventKind.NodeStatus or
                RuntimeTraceEventKind.NodeRunning or
                RuntimeTraceEventKind.NodeWaiting or
                RuntimeTraceEventKind.ValueSampled or
                RuntimeTraceEventKind.ValueSamplingLimited or
                RuntimeTraceEventKind.TimelineLogicTime or
                RuntimeTraceEventKind.TimelineVisualTime or
                RuntimeTraceEventKind.TrackActive or
                RuntimeTraceEventKind.ClipActive or
                RuntimeTraceEventKind.TreeClipUpdated or
                RuntimeTraceEventKind.MotionContribution or
                RuntimeTraceEventKind.MotionResolved or
                RuntimeTraceEventKind.ActionSnapshot or
                RuntimeTraceEventKind.AnimationProducerSampled or
                RuntimeTraceEventKind.AnimationPlaybackPending or
                RuntimeTraceEventKind.AnimationPlaybackSelected or
                RuntimeTraceEventKind.AnimationPlaybackRetained or
                RuntimeTraceEventKind.AnimationPlaybackRetired or
                RuntimeTraceEventKind.AnimationPhaseSync or
                RuntimeTraceEventKind.MotionMatchingQuery or
                RuntimeTraceEventKind.MotionMatchingTrajectory or
                RuntimeTraceEventKind.MotionMatchingPoseHistory or
                RuntimeTraceEventKind.MotionMatchingAdmission or
                RuntimeTraceEventKind.MotionMatchingCandidateRejected or
                RuntimeTraceEventKind.MotionMatchingSearchTraversal or
                RuntimeTraceEventKind.MotionMatchingTopK or
                RuntimeTraceEventKind.MotionMatchingPlan or
                RuntimeTraceEventKind.MotionMatchingSelection or
                RuntimeTraceEventKind.MotionMatchingPoseSource or
                RuntimeTraceEventKind.MotionMatchingFrame or
                RuntimeTraceEventKind.PresentationInterpolated or
                RuntimeTraceEventKind.LocomotionPresentation or
                RuntimeTraceEventKind.FootPlacementSnapshot or
                RuntimeTraceEventKind.CameraSnapshot or
                RuntimeTraceEventKind.CameraRequest => RuntimeDiagnosticsCaptureDetail.Continuous,
                _ => RuntimeDiagnosticsCaptureDetail.Boundary
            };
        }

        void RecalculateLiveChannels()
        {
            RuntimeTraceChannel channels = RuntimeTraceChannel.None;
            foreach (RuntimeDiagnosticsInterest interest in m_Interests.Values)
            {
                if (interest.Kind == RuntimeDiagnosticsInterestKind.LiveState)
                    channels |= interest.Channels;
            }
            m_LiveChannels = channels;
        }
    }
}
