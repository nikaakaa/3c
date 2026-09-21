using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    public enum RuntimeExecutionSpanKind
    {
        Point,
        Graph,
        Node,
        State,
        Timeline,
        TreeClip,
        Wait,
        Branch,
        Track,
        Clip
    }

    public readonly struct RuntimeExecutionSpan
    {
        public RuntimeExecutionSpan(
            RuntimeExecutionSpanKind kind,
            RuntimeTraceEvent start,
            RuntimeTraceEvent end,
            RuntimeSourceElementHandle sourceHandle,
            RuntimeSourceElementKey source,
            bool hasSource,
            bool completed)
        {
            Kind = kind;
            Start = start;
            End = end;
            SourceHandle = sourceHandle;
            Source = source;
            HasSource = hasSource;
            Completed = completed;
        }

        public RuntimeExecutionSpanKind Kind { get; }
        public RuntimeTraceEvent Start { get; }
        public RuntimeTraceEvent End { get; }
        public RuntimeSourceElementHandle SourceHandle { get; }
        public RuntimeSourceElementKey Source { get; }
        public bool HasSource { get; }
        public bool Completed { get; }
        public RuntimeInstanceKey Instance => Start.RuntimeInstance;
        public Guid ExecutionBranchId => Start.ExecutionBranchId;
        public RuntimeTraceDomain Domain => Start.Domain;
        public ulong StartPosition => Start.Position;
        public ulong EndPosition => End.Position;
        public ulong StartSequence => Start.Sequence;
        public ulong EndSequence => End.Sequence;
        public ulong RuntimeEpoch => Start.RuntimeEpoch;
        public RuntimeContentRevision ContentRevision => Start.ContentRevision;
        public string SkillId => Start.Payload.SkillId;
        public ulong ActionInstanceId => Start.Payload.ActionInstanceId;
        public string CallSiteId => Start.Payload.CallSiteId;
        public ulong ActivationGeneration => Start.Payload.ActivationGeneration;
        public ulong SkillExecutionGeneration => Start.Payload.SkillExecutionGeneration;
        public ulong GraphInvocationGeneration => Start.Payload.GraphInvocationGeneration;
        public ulong ParentInvocationGeneration => Start.Payload.ParentInvocationGeneration;
        public int Cycle => Math.Max(Start.Payload.Cycle, End.Payload.Cycle);
    }

    public sealed class RuntimeExecutionTimeline
    {
        readonly IReadOnlyList<RuntimeExecutionSpan> m_Spans;

        internal RuntimeExecutionTimeline(
            Guid captureId,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            long version,
            long evictedEvents,
            bool complete,
            int unmappedEventCount,
            IReadOnlyList<RuntimeExecutionSpan> spans)
        {
            CaptureId = captureId;
            Channels = channels;
            Detail = detail;
            Version = version;
            EvictedEvents = evictedEvents;
            IsComplete = complete;
            UnmappedEventCount = unmappedEventCount;
            m_Spans = spans ?? Array.Empty<RuntimeExecutionSpan>();
        }

        public Guid CaptureId { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail Detail { get; }
        public long Version { get; }
        public long EvictedEvents { get; }
        public bool IsComplete { get; }
        public int UnmappedEventCount { get; }
        public IReadOnlyList<RuntimeExecutionSpan> Spans => m_Spans;
    }

    public readonly struct RuntimeExecutionSourceClock : IEquatable<RuntimeExecutionSourceClock>
    {
        public RuntimeExecutionSourceClock(string clockId, string tickKind)
        {
            if (string.IsNullOrWhiteSpace(clockId) || string.IsNullOrWhiteSpace(tickKind))
                throw new ArgumentException("Runtime execution source clock identity is incomplete.");
            ClockId = clockId;
            TickKind = tickKind;
        }

        public string ClockId { get; }
        public string TickKind { get; }
        public bool Equals(RuntimeExecutionSourceClock other) =>
            string.Equals(ClockId, other.ClockId, StringComparison.Ordinal) &&
            string.Equals(TickKind, other.TickKind, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RuntimeExecutionSourceClock other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ClockId, TickKind);
        public override string ToString() => $"{TickKind}:{ClockId}";
        public static bool operator ==(RuntimeExecutionSourceClock left, RuntimeExecutionSourceClock right) => left.Equals(right);
        public static bool operator !=(RuntimeExecutionSourceClock left, RuntimeExecutionSourceClock right) => !left.Equals(right);
    }

    public sealed class RuntimeExecutionTickRecord
    {
        readonly IReadOnlyList<RuntimeTraceEvent> m_Events;
        readonly RuntimeTraceEvent[] m_ExternalResults;
        readonly Guid[] m_SessionIds;
        readonly Guid[] m_ExecutionBranchIds;
        readonly RuntimeContentRevision[] m_ContentRevisions;
        readonly ulong[] m_RuntimeEpochs;
        readonly RuntimeExecutionSourceClock[] m_SourceClocks;
        readonly Guid[] m_CharacterRuntimeIds;
        readonly RuntimeInstanceKey[] m_RuntimeInstances;
        readonly string[] m_SkillIds;
        readonly ulong[] m_ActionInstanceIds;
        readonly string[] m_CallSiteIds;
        readonly ulong[] m_InputSequences;
        readonly string[] m_CharacterStateHashes;
        readonly string[] m_WorldHashes;

        internal RuntimeExecutionTickRecord(
            ulong tick,
            Guid executionBranchId,
            List<RuntimeTraceEvent> events)
        {
            Tick = tick;
            ExecutionBranchId = executionBranchId;
            m_Events = events;
            Collector<RuntimeTraceEvent> externalResults = default;
            Collector<Guid> sessionIds = default;
            Collector<Guid> executionBranchIds = default;
            Collector<RuntimeContentRevision> contentRevisions = default;
            Collector<ulong> runtimeEpochs = default;
            Collector<RuntimeExecutionSourceClock> sourceClocks = default;
            Collector<Guid> characterRuntimeIds = default;
            Collector<RuntimeInstanceKey> runtimeInstances = default;
            Collector<string> skillIds = default;
            Collector<ulong> actionInstanceIds = default;
            Collector<string> callSiteIds = default;
            Collector<ulong> inputSequences = default;
            Collector<string> characterStateHashes = default;
            Collector<string> worldHashes = default;
            for (int i = 0; i < m_Events.Count; i++)
            {
                RuntimeTraceEvent traceEvent = m_Events[i];
                if (traceEvent.Kind == RuntimeTraceEventKind.SimulationNetworkModel)
                    externalResults.Add(traceEvent);
                if (traceEvent.SessionId != Guid.Empty)
                    sessionIds.AddDistinct(traceEvent.SessionId);
                if (traceEvent.ExecutionBranchId != Guid.Empty)
                    executionBranchIds.AddDistinct(traceEvent.ExecutionBranchId);
                if (traceEvent.ContentRevision.IsValid)
                    contentRevisions.AddDistinct(traceEvent.ContentRevision);
                if (traceEvent.RuntimeEpoch != 0)
                    runtimeEpochs.AddDistinct(traceEvent.RuntimeEpoch);
                if (!string.IsNullOrEmpty(traceEvent.Payload.SourceClockId) &&
                    !string.IsNullOrEmpty(traceEvent.Payload.SourceTickKind))
                {
                    var sourceClock = new RuntimeExecutionSourceClock(
                        traceEvent.Payload.SourceClockId,
                        traceEvent.Payload.SourceTickKind);
                    sourceClocks.AddDistinct(sourceClock);
                }
                if (traceEvent.RuntimeInstance.CharacterRuntimeId != Guid.Empty)
                    characterRuntimeIds.AddDistinct(traceEvent.RuntimeInstance.CharacterRuntimeId);
                if (traceEvent.RuntimeInstance.IsValid)
                    runtimeInstances.AddDistinct(traceEvent.RuntimeInstance);
                if (!string.IsNullOrEmpty(traceEvent.Payload.SkillId))
                    skillIds.AddDistinct(traceEvent.Payload.SkillId);
                if (traceEvent.Payload.ActionInstanceId != 0)
                    actionInstanceIds.AddDistinct(traceEvent.Payload.ActionInstanceId);
                if (!string.IsNullOrEmpty(traceEvent.Payload.CallSiteId))
                    callSiteIds.AddDistinct(traceEvent.Payload.CallSiteId);
                if (traceEvent.Payload.InputSequence != 0)
                    inputSequences.AddDistinct(traceEvent.Payload.InputSequence);
                if (!string.IsNullOrEmpty(traceEvent.Payload.CharacterStateHash))
                    characterStateHashes.AddDistinct(traceEvent.Payload.CharacterStateHash);
                if (!string.IsNullOrEmpty(traceEvent.Payload.WorldHash))
                    worldHashes.AddDistinct(traceEvent.Payload.WorldHash);
            }
            m_ExternalResults = externalResults.ToArray();
            m_SessionIds = sessionIds.ToArray();
            m_ExecutionBranchIds = executionBranchIds.ToArray();
            m_ContentRevisions = contentRevisions.ToArray();
            m_RuntimeEpochs = runtimeEpochs.ToArray();
            m_SourceClocks = sourceClocks.ToArray();
            m_CharacterRuntimeIds = characterRuntimeIds.ToArray();
            m_RuntimeInstances = runtimeInstances.ToArray();
            m_SkillIds = skillIds.ToArray();
            m_ActionInstanceIds = actionInstanceIds.ToArray();
            m_CallSiteIds = callSiteIds.ToArray();
            m_InputSequences = inputSequences.ToArray();
            m_CharacterStateHashes = characterStateHashes.ToArray();
            m_WorldHashes = worldHashes.ToArray();
        }

        public ulong Tick { get; }
        public Guid ExecutionBranchId { get; }
        public IReadOnlyList<RuntimeTraceEvent> Events => m_Events;
        public IReadOnlyList<RuntimeTraceEvent> ExternalResults => m_ExternalResults;
        public IReadOnlyList<Guid> SessionIds => m_SessionIds;
        public IReadOnlyList<Guid> ExecutionBranchIds => m_ExecutionBranchIds;
        public IReadOnlyList<RuntimeContentRevision> ContentRevisions => m_ContentRevisions;
        public IReadOnlyList<ulong> RuntimeEpochs => m_RuntimeEpochs;
        public IReadOnlyList<RuntimeExecutionSourceClock> SourceClocks => m_SourceClocks;
        public IReadOnlyList<Guid> CharacterRuntimeIds => m_CharacterRuntimeIds;
        public IReadOnlyList<RuntimeInstanceKey> RuntimeInstances => m_RuntimeInstances;
        public IReadOnlyList<string> SkillIds => m_SkillIds;
        public IReadOnlyList<ulong> ActionInstanceIds => m_ActionInstanceIds;
        public IReadOnlyList<string> CallSiteIds => m_CallSiteIds;
        public IReadOnlyList<ulong> InputSequences => m_InputSequences;
        public IReadOnlyList<string> CharacterStateHashes => m_CharacterStateHashes;
        public IReadOnlyList<string> WorldHashes => m_WorldHashes;
        public bool HasSimulationTick => Contains(RuntimeTraceEventKind.SimulationTick);
        public bool HasStatePublished => Contains(RuntimeTraceEventKind.SimulationStatePublished);
        public bool HasExternalResult => m_ExternalResults.Length != 0;

        struct Collector<T>
        {
            T[] m_Values;
            int m_Count;

            public void Add(T value)
            {
                if (m_Count == (m_Values?.Length ?? 0))
                    Array.Resize(ref m_Values, m_Count == 0 ? 4 : m_Count * 2);
                m_Values[m_Count++] = value;
            }

            public void AddDistinct(T value)
            {
                for (int i = 0; i < m_Count; i++)
                    if (EqualityComparer<T>.Default.Equals(m_Values[i], value))
                        return;
                Add(value);
            }

            public T[] ToArray()
            {
                if (m_Count == 0)
                    return Array.Empty<T>();
                if (m_Count == m_Values.Length)
                {
                    T[] exact = m_Values;
                    m_Values = null;
                    return exact;
                }
                var result = new T[m_Count];
                Array.Copy(m_Values, result, m_Count);
                m_Values = null;
                return result;
            }
        }

        bool Contains(RuntimeTraceEventKind kind)
        {
            for (int i = 0; i < m_Events.Count; i++)
                if (m_Events[i].Kind == kind)
                    return true;
            return false;
        }

    }

    public sealed class RuntimeExecutionPresentationFrame
    {
        readonly IReadOnlyList<RuntimeTraceEvent> m_Events;

        internal RuntimeExecutionPresentationFrame(
            ulong frame,
            Guid executionBranchId,
            List<RuntimeTraceEvent> events)
        {
            Frame = frame;
            ExecutionBranchId = executionBranchId;
            m_Events = events;
        }

        public ulong Frame { get; }
        public Guid ExecutionBranchId { get; }
        public IReadOnlyList<RuntimeTraceEvent> Events => m_Events;
        public bool IsAvailable => Contains(RuntimeTraceEventKind.PresentationInterpolated);

        bool Contains(RuntimeTraceEventKind kind)
        {
            for (int i = 0; i < m_Events.Count; i++)
                if (m_Events[i].Kind == kind)
                    return true;
            return false;
        }
    }

    public sealed class RuntimeExecutionCheckpoint
    {
        internal RuntimeExecutionCheckpoint(
            ulong tick,
            RuntimeTraceEvent traceEvent,
            bool historyComplete)
        {
            Tick = tick;
            TraceEvent = traceEvent;
            SnapshotIdentity = traceEvent.Payload.RelatedElementId ?? string.Empty;
            HistoryComplete = historyComplete;
        }

        public ulong Tick { get; }
        public RuntimeTraceEvent TraceEvent { get; }
        public string SnapshotIdentity { get; }
        public RuntimeContentRevision Revision => TraceEvent.ContentRevision;
        public ulong RuntimeEpoch => TraceEvent.RuntimeEpoch;
        public Guid ExecutionBranchId => TraceEvent.ExecutionBranchId;
        public RuntimeInstanceKey Instance => TraceEvent.RuntimeInstance;
        public bool HistoryComplete { get; }
        public bool CanRestore => HistoryComplete && !string.IsNullOrEmpty(SnapshotIdentity);
    }

    public sealed class RuntimeExecutionHistory
    {
        readonly IReadOnlyList<RuntimeExecutionTickRecord> m_Ticks;
        readonly IReadOnlyList<RuntimeExecutionCheckpoint> m_Checkpoints;
        readonly IReadOnlyList<RuntimeExecutionPresentationFrame> m_PresentationFrames;

        internal RuntimeExecutionHistory(
            Guid captureId,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            long version,
            long evictedEvents,
            bool complete,
            int unmappedEventCount,
            List<RuntimeExecutionTickRecord> ticks,
            List<RuntimeExecutionCheckpoint> checkpoints,
            List<RuntimeExecutionPresentationFrame> presentationFrames)
        {
            CaptureId = captureId;
            Channels = channels;
            Detail = detail;
            Version = version;
            EvictedEvents = evictedEvents;
            IsComplete = complete;
            UnmappedEventCount = unmappedEventCount;
            m_Ticks = ticks;
            m_Checkpoints = checkpoints;
            m_PresentationFrames = presentationFrames;
        }

        public Guid CaptureId { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail Detail { get; }
        public long Version { get; }
        public long EvictedEvents { get; }
        public bool IsComplete { get; }
        public int UnmappedEventCount { get; }
        public IReadOnlyList<RuntimeExecutionTickRecord> Ticks => m_Ticks;
        public IReadOnlyList<RuntimeExecutionCheckpoint> Checkpoints => m_Checkpoints;
        public IReadOnlyList<RuntimeExecutionPresentationFrame> PresentationFrames => m_PresentationFrames;
        public bool HasExternalResults
        {
            get
            {
                for (int i = 0; i < m_Ticks.Count; i++)
                    if (m_Ticks[i].HasExternalResult)
                        return true;
                return false;
            }
        }
        public int ExternalResultCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_Ticks.Count; i++)
                    count += m_Ticks[i].ExternalResults.Count;
                return count;
            }
        }
        public bool HasPresentationFrames => m_PresentationFrames.Count > 0;
    }

    internal static class RuntimeExecutionTimelineBuilder
    {
        static readonly Comparison<RuntimeTraceEvent> CompareEventsComparer = CompareEvents;
        static readonly Comparison<RuntimeTraceEvent> CompareGroupedEventsComparer = CompareGroupedEvents;
        static readonly Comparison<RuntimeExecutionSpan> CompareSpansComparer = CompareSpans;
        static readonly SelectionScratch Selection = new();
        static readonly Dictionary<SpanKey, PendingSpan> Open = new();

        internal static RuntimeExecutionTimeline Build(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default)
        {
            return BuildCore(capture, sourceMap, null, historyOffset, instance);
        }

        internal static RuntimeExecutionTimeline Build(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default)
        {
            return BuildCore(capture, sourceMap, sourceMaps, historyOffset, instance);
        }

        static RuntimeExecutionTimeline BuildCore(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset,
            RuntimeInstanceKey instance)
        {
            if (capture == null)
                throw new ArgumentNullException(nameof(capture));
            if (sourceMap == null)
                throw new ArgumentNullException(nameof(sourceMap));
            if (historyOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(historyOffset));

            ReadOnlySpan<RuntimeTraceEvent> events = capture.GetEvents(historyOffset);
            List<RuntimeTraceEvent> selected = SelectEvents(events, instance);
            selected.Sort(CompareEventsComparer);

            Dictionary<SpanKey, PendingSpan> open = Open;
            open.Clear();
            var spans = new List<RuntimeExecutionSpan>();
            int unmappedEventCount = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                RuntimeTraceEvent value = selected[i];
                RuntimeSourceElementHandle handle = value.Source;
                RuntimeSourceElementKey source = default;
                RuntimeDebugSourceMapSnapshot eventSourceMap = null;
                bool hasRevision = sourceMaps != null
                    ? sourceMaps.TryGetValue(value.ContentRevision, out eventSourceMap)
                    : sourceMap.Revision.Equals(value.ContentRevision);
                if (sourceMaps == null && hasRevision)
                    eventSourceMap = sourceMap;
                bool hasSource = hasRevision && handle.IsValid && eventSourceMap.TryResolve(handle, out source, out _);
                if (handle.IsValid && !hasSource)
                    unmappedEventCount++;
                RuntimeExecutionSpanKind kind = ResolveKind(value.Kind, source, hasSource);
                SpanKey key = new SpanKey(
                    kind,
                    handle,
                    value.RuntimeInstance,
                    value.ExecutionBranchId,
                    value.RuntimeEpoch,
                    value.ContentRevision);
                if (value.Kind == RuntimeTraceEventKind.NodeWaiting)
                {
                    if (open.TryGetValue(key, out PendingSpan waiting))
                    {
                        waiting.Last = value;
                        open[key] = waiting;
                    }
                    else
                    {
                        open.Add(key, new PendingSpan(kind, value, handle, source, hasSource));
                    }

                    SpanKey nodeKey = new SpanKey(
                        RuntimeExecutionSpanKind.Node,
                        handle,
                        value.RuntimeInstance,
                        value.ExecutionBranchId,
                        value.RuntimeEpoch,
                        value.ContentRevision);
                    if (open.TryGetValue(nodeKey, out PendingSpan node))
                    {
                        node.Last = value;
                        open[nodeKey] = node;
                    }
                    continue;
                }
                SpanKey waitKey = new SpanKey(
                    RuntimeExecutionSpanKind.Wait,
                    handle,
                    value.RuntimeInstance,
                    value.ExecutionBranchId,
                    value.RuntimeEpoch,
                    value.ContentRevision);
                if (IsWaitEnd(value.Kind) && open.TryGetValue(waitKey, out PendingSpan activeWait))
                {
                    spans.Add(activeWait.Close(value));
                    open.Remove(waitKey);
                }
                if (IsPoint(value.Kind, kind))
                {
                    if (open.TryGetValue(key, out PendingSpan activePoint))
                    {
                        activePoint.Last = value;
                        open[key] = activePoint;
                    }
                    spans.Add(new RuntimeExecutionSpan(kind, value, value, handle, source, hasSource, true));
                    continue;
                }

                if (IsStart(value.Kind, kind))
                {
                    if (open.TryGetValue(key, out PendingSpan previous))
                    {
                        if (kind == RuntimeExecutionSpanKind.Timeline &&
                            previous.Start.Kind == RuntimeTraceEventKind.TimelineRequested &&
                            value.Kind == RuntimeTraceEventKind.TimelineStarted)
                        {
                            previous.Last = value;
                            open[key] = previous;
                            continue;
                        }
                        spans.Add(previous.Close(previous.Last));
                        open.Remove(key);
                    }
                    open.Add(key, new PendingSpan(kind, value, handle, source, hasSource));
                    continue;
                }

                if (IsEnd(value.Kind, kind) && open.TryGetValue(key, out PendingSpan pending))
                {
                    spans.Add(pending.Close(value));
                    open.Remove(key);
                    continue;
                }

                if (open.TryGetValue(key, out PendingSpan active))
                    active.Last = value;
                else
                    spans.Add(new RuntimeExecutionSpan(kind, value, value, handle, source, hasSource, false));
            }

            foreach (PendingSpan pending in open.Values)
                spans.Add(pending.Close(pending.Last, false));

            spans.Sort(CompareSpansComparer);
            bool complete = capture.EvictedEvents == 0 &&
                            selected.Count != 0 &&
                            unmappedEventCount == 0 &&
                            open.Count == 0;
            return new RuntimeExecutionTimeline(
                capture.CaptureId,
                capture.Channels,
                capture.Detail,
                capture.Version,
                capture.EvictedEvents,
                complete,
                unmappedEventCount,
                spans);
        }

        internal static RuntimeExecutionHistory BuildHistory(
            RuntimeCaptureSnapshot capture,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default)
        {
            return BuildHistory(capture, null, null, historyOffset, instance);
        }

        internal static RuntimeExecutionHistory BuildHistory(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default)
        {
            if (capture == null)
                throw new ArgumentNullException(nameof(capture));
            if (historyOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(historyOffset));

            ReadOnlySpan<RuntimeTraceEvent> allEvents = capture.GetEvents(historyOffset);
            List<RuntimeTraceEvent> selectedEvents = SelectEvents(allEvents, instance);
            if (instance.IsValid && selectedEvents.Count > 0)
                AddSessionBoundaryEvents(
                    allEvents,
                    selectedEvents,
                    selectedEvents);

            selectedEvents.Sort(CompareGroupedEventsComparer);
            var grouped = new List<EventGroup<TickKey>>();
            var presentation = new List<EventGroup<PresentationFrameKey>>();
            int unmappedEventCount = 0;
            bool checkSourceCoverage = sourceMap != null || sourceMaps != null;
            for (int i = 0; i < selectedEvents.Count; i++)
            {
                RuntimeTraceEvent value = selectedEvents[i];
                if (checkSourceCoverage && value.Source.IsValid)
                {
                    RuntimeDebugSourceMapSnapshot resolvedMap = sourceMap;
                    bool hasRevision = sourceMaps != null
                        ? sourceMaps.TryGetValue(value.ContentRevision, out resolvedMap)
                        : sourceMap != null && sourceMap.Revision.Equals(value.ContentRevision);
                    if (!hasRevision || resolvedMap == null || !resolvedMap.TryGet(value.Source, out _))
                        unmappedEventCount++;
                }
                if (value.Domain == RuntimeTraceDomain.Presentation)
                {
                    var presentationKey = new PresentationFrameKey(value.Position, value.ExecutionBranchId);
                    if (presentation.Count == 0 ||
                        !EqualityComparer<PresentationFrameKey>.Default.Equals(
                            presentation[presentation.Count - 1].Key,
                            presentationKey))
                    {
                        presentation.Add(new EventGroup<PresentationFrameKey>(presentationKey));
                    }
                    presentation[presentation.Count - 1].Events.Add(value);
                    continue;
                }
                TickKey key = new TickKey(value.Position, value.ExecutionBranchId);
                if (grouped.Count == 0 ||
                    !EqualityComparer<TickKey>.Default.Equals(
                        grouped[grouped.Count - 1].Key,
                        key))
                {
                    grouped.Add(new EventGroup<TickKey>(key));
                }
                grouped[grouped.Count - 1].Events.Add(value);
            }

            var ticks = new List<RuntimeExecutionTickRecord>(grouped.Count);
            var checkpoints = new List<RuntimeExecutionCheckpoint>();
            var presentationFrames = new List<RuntimeExecutionPresentationFrame>(presentation.Count);
            var checkpointKeys = new HashSet<CheckpointKey>();
            bool complete = capture.EvictedEvents == 0 && grouped.Count != 0;
            foreach (EventGroup<TickKey> group in grouped)
            {
                group.Events.Sort(CompareEventsComparer);
                var record = new RuntimeExecutionTickRecord(
                    group.Key.Tick,
                    group.Key.ExecutionBranchId,
                    group.Events);
                complete &= record.HasSimulationTick && record.HasStatePublished;
                ticks.Add(record);
            }
            bool historyComplete = complete && unmappedEventCount == 0;
            foreach (EventGroup<TickKey> group in grouped)
            {
                for (int i = 0; i < group.Events.Count; i++)
                {
                    RuntimeTraceEvent traceEvent = group.Events[i];
                    if (traceEvent.Kind != RuntimeTraceEventKind.SimulationCheckpointCaptured)
                        continue;
                    var checkpointKey = new CheckpointKey(
                        group.Key.Tick,
                        group.Key.ExecutionBranchId,
                        traceEvent.RuntimeEpoch,
                        traceEvent.ContentRevision,
                        traceEvent.Payload.RelatedElementId);
                    if (!checkpointKeys.Add(checkpointKey))
                        continue;
                    RuntimeExecutionCheckpoint checkpoint = new RuntimeExecutionCheckpoint(
                        group.Key.Tick,
                        traceEvent,
                        historyComplete);
                    checkpoints.Add(checkpoint);
                }
            }
            foreach (EventGroup<PresentationFrameKey> group in presentation)
            {
                group.Events.Sort(CompareEventsComparer);
                presentationFrames.Add(new RuntimeExecutionPresentationFrame(
                    group.Key.Frame,
                    group.Key.ExecutionBranchId,
                    group.Events));
            }
            return new RuntimeExecutionHistory(
                capture.CaptureId,
                capture.Channels,
                capture.Detail,
                capture.Version,
                capture.EvictedEvents,
                historyComplete,
                unmappedEventCount,
                ticks,
                checkpoints,
                presentationFrames);
        }

        static void AddSessionBoundaryEvents(
            ReadOnlySpan<RuntimeTraceEvent> allEvents,
            IReadOnlyList<RuntimeTraceEvent> selectedEvents,
            List<RuntimeTraceEvent> historyEvents)
        {
            ulong first = ulong.MaxValue;
            ulong last = 0;
            for (int i = 0; i < selectedEvents.Count; i++)
            {
                RuntimeTraceEvent value = selectedEvents[i];
                if (value.Domain == RuntimeTraceDomain.Presentation || value.Position == 0)
                    continue;
                first = Math.Min(first, value.Position);
                last = Math.Max(last, value.Position);
            }
            if (first == ulong.MaxValue)
                return;

            var selectedSequences = new HashSet<ulong>();
            var selectedBranches = new HashSet<Guid>();
            for (int i = 0; i < historyEvents.Count; i++)
            {
                selectedSequences.Add(historyEvents[i].Sequence);
                selectedBranches.Add(historyEvents[i].ExecutionBranchId);
            }
            bool hasBaselineCheckpoint = false;
            ulong baselineCheckpointTick = 0;
            for (int i = 0; i < allEvents.Length; i++)
            {
                RuntimeTraceEvent value = allEvents[i];
                if (value.Kind != RuntimeTraceEventKind.SimulationCheckpointCaptured ||
                    value.Position > first ||
                    !selectedBranches.Contains(value.ExecutionBranchId) ||
                    hasBaselineCheckpoint && value.Position <= baselineCheckpointTick)
                    continue;
                hasBaselineCheckpoint = true;
                baselineCheckpointTick = value.Position;
            }
            if (hasBaselineCheckpoint)
                first = baselineCheckpointTick;
            for (int i = 0; i < allEvents.Length; i++)
            {
                RuntimeTraceEvent value = allEvents[i];
                if (value.Position < first || value.Position > last ||
                    !IsSessionBoundaryEvent(value.Kind) ||
                    !selectedBranches.Contains(value.ExecutionBranchId) ||
                    !selectedSequences.Add(value.Sequence))
                {
                    continue;
                }
                historyEvents.Add(value);
            }
        }

        static bool IsSessionBoundaryEvent(RuntimeTraceEventKind kind)
        {
            return kind is RuntimeTraceEventKind.SimulationTick or
                RuntimeTraceEventKind.SimulationRestore or
                RuntimeTraceEventKind.SimulationFailure or
                RuntimeTraceEventKind.SimulationNetworkModel or
                RuntimeTraceEventKind.SimulationStatePublished or
                RuntimeTraceEventKind.SimulationCheckpointCaptured;
        }

        readonly struct EventGroup<TKey>
        {
            internal EventGroup(TKey key)
            {
                Key = key;
                Events = new List<RuntimeTraceEvent>();
            }

            internal TKey Key { get; }
            internal List<RuntimeTraceEvent> Events { get; }
        }

        static List<RuntimeTraceEvent> SelectEvents(
            ReadOnlySpan<RuntimeTraceEvent> events,
            RuntimeInstanceKey instance)
        {
            List<RuntimeTraceEvent> selected = Selection.Events;
            selected.Clear();
            if (!instance.IsValid)
            {
                if (selected.Capacity < events.Length)
                    selected.Capacity = events.Length;
                for (int i = 0; i < events.Length; i++)
                    selected.Add(events[i]);
                return selected;
            }

            HashSet<ulong> selectedSequences = Selection.Sequences;
            HashSet<GraphBranchKey> relatedGraphs = Selection.RelatedGraphs;
            HashSet<PresentationFrameKey> selectedPresentationFrames = Selection.PresentationFrames;
            selectedSequences.Clear();
            relatedGraphs.Clear();
            selectedPresentationFrames.Clear();
            Guid selectedBranch = ResolveSelectionBranch(events, instance);
            for (int i = 0; i < events.Length; i++)
            {
                RuntimeTraceEvent value = events[i];
                if (selectedBranch != Guid.Empty && value.ExecutionBranchId != selectedBranch)
                    continue;
                RuntimeInstanceKey candidate = value.RuntimeInstance;
                bool actionMatch = MatchesAction(instance, value);
                if (IsTimelineFamily(instance.Kind) &&
                    IsTimelineFamily(candidate.Kind))
                {
                    actionMatch = MatchesTimelineRuntime(instance, candidate);
                }
                bool include = candidate.Equals(instance) || actionMatch;
                if (actionMatch && value.Domain == RuntimeTraceDomain.Presentation)
                    selectedPresentationFrames.Add(new PresentationFrameKey(value.Position, value.ExecutionBranchId));
                if (actionMatch)
                    AddRelatedGraph(relatedGraphs, candidate, value.ExecutionBranchId);
                if (instance.Kind == RuntimeInstanceKind.TimelinePlayback &&
                    candidate.SourceOperationIndex == instance.SourceOperationIndex &&
                    candidate.TimelinePlaybackId == instance.TimelinePlaybackId &&
                    (instance.ActionInstanceId == 0 ||
                     candidate.ActionInstanceId == instance.ActionInstanceId) &&
                    candidate.TimelinePlaybackId != 0)
                {
                    include = true;
                    AddRelatedGraph(relatedGraphs, candidate, value.ExecutionBranchId);
                }
                else if (instance.Kind == RuntimeInstanceKind.TreeClip &&
                         candidate.Kind == RuntimeInstanceKind.TreeClip &&
                         candidate.SourceOperationIndex == instance.SourceOperationIndex &&
                         candidate.TreeClipOperationIndex == instance.TreeClipOperationIndex &&
                         candidate.TimelinePlaybackId == instance.TimelinePlaybackId &&
                         candidate.TreeClipCycle == instance.TreeClipCycle &&
                         (instance.ActionInstanceId == 0 ||
                          candidate.ActionInstanceId == instance.ActionInstanceId) &&
                         candidate.TimelinePlaybackId != 0)
                {
                    include = true;
                    AddRelatedGraph(relatedGraphs, candidate, value.ExecutionBranchId);
                }

                if (include && value.Domain == RuntimeTraceDomain.Presentation)
                    selectedPresentationFrames.Add(new PresentationFrameKey(value.Position, value.ExecutionBranchId));

                if (include && selectedSequences.Add(value.Sequence))
                    selected.Add(value);
            }

            for (int i = 0; i < events.Length; i++)
            {
                RuntimeTraceEvent value = events[i];
                RuntimeInstanceKey candidate = value.RuntimeInstance;
                bool presentationMatch =
                    value.Domain == RuntimeTraceDomain.Presentation &&
                    candidate.CharacterRuntimeId == instance.CharacterRuntimeId &&
                    selectedPresentationFrames.Contains(new PresentationFrameKey(value.Position, value.ExecutionBranchId));
                if (presentationMatch &&
                    IsTimelineFamily(instance.Kind) &&
                    IsTimelineFamily(candidate.Kind) &&
                    !MatchesTimelineRuntime(instance, candidate))
                {
                    presentationMatch = false;
                }
                bool graphMatch = candidate.CharacterRuntimeId == instance.CharacterRuntimeId &&
                    relatedGraphs.Contains(new GraphBranchKey(candidate.GraphRuntimeId, value.ExecutionBranchId));
                if (graphMatch &&
                    IsTimelineFamily(instance.Kind) &&
                    IsTimelineFamily(candidate.Kind) &&
                    !MatchesTimelineRuntime(instance, candidate))
                {
                    graphMatch = false;
                }
                if ((!presentationMatch && !graphMatch) ||
                    !selectedSequences.Add(value.Sequence))
                {
                    continue;
                }
                selected.Add(value);
            }

            return selected;
        }

        sealed class SelectionScratch
        {
            internal readonly List<RuntimeTraceEvent> Events = new();
            internal readonly HashSet<ulong> Sequences = new();
            internal readonly HashSet<GraphBranchKey> RelatedGraphs = new();
            internal readonly HashSet<PresentationFrameKey> PresentationFrames = new();
        }

        static Guid ResolveSelectionBranch(
            ReadOnlySpan<RuntimeTraceEvent> events,
            RuntimeInstanceKey instance)
        {
            Guid branch = Guid.Empty;
            ulong latestSequence = 0;
            for (int i = 0; i < events.Length; i++)
            {
                RuntimeTraceEvent value = events[i];
                bool matchesInstance = value.RuntimeInstance.Equals(instance) || MatchesAction(instance, value);
                if (value.ExecutionBranchId == Guid.Empty ||
                    !matchesInstance ||
                    branch != Guid.Empty && value.Sequence <= latestSequence)
                    continue;
                branch = value.ExecutionBranchId;
                latestSequence = value.Sequence;
            }
            return branch;
        }

        static void AddRelatedGraph(
            HashSet<GraphBranchKey> relatedGraphs,
            RuntimeInstanceKey instance,
            Guid executionBranchId)
        {
            if (instance.GraphRuntimeId != Guid.Empty)
                relatedGraphs.Add(new GraphBranchKey(instance.GraphRuntimeId, executionBranchId));
        }

        static bool MatchesAction(RuntimeInstanceKey instance, RuntimeTraceEvent value)
        {
            if (instance.ActionInstanceId == 0 ||
                value.Payload.ActionInstanceId != instance.ActionInstanceId &&
                value.RuntimeInstance.ActionInstanceId != instance.ActionInstanceId)
            {
                return false;
            }
            if (instance.Kind != RuntimeInstanceKind.SkillExecution)
                return true;
            if (!string.IsNullOrEmpty(instance.CallSiteId) &&
                !string.IsNullOrEmpty(value.Payload.CallSiteId) &&
                !string.Equals(instance.CallSiteId, value.Payload.CallSiteId, StringComparison.Ordinal))
            {
                return false;
            }
            if (instance.InvocationGeneration != 0 &&
                value.RuntimeInstance.InvocationGeneration != 0 &&
                instance.InvocationGeneration != value.RuntimeInstance.InvocationGeneration &&
                instance.InvocationGeneration != value.Payload.GraphInvocationGeneration)
            {
                return false;
            }
            return true;
        }

        static bool IsTimelineFamily(RuntimeInstanceKind kind) =>
            kind == RuntimeInstanceKind.TimelinePlayback ||
            kind == RuntimeInstanceKind.TreeClip;

        static bool MatchesTimelineRuntime(
            RuntimeInstanceKey selected,
            RuntimeInstanceKey candidate)
        {
            if (!IsTimelineFamily(selected.Kind) ||
                !IsTimelineFamily(candidate.Kind) ||
                selected.CharacterRuntimeId != candidate.CharacterRuntimeId ||
                selected.SourceOperationIndex != candidate.SourceOperationIndex ||
                selected.TimelinePlaybackId == 0 ||
                selected.TimelinePlaybackId != candidate.TimelinePlaybackId ||
                selected.ActionInstanceId != 0 &&
                candidate.ActionInstanceId != 0 &&
                selected.ActionInstanceId != candidate.ActionInstanceId)
            {
                return false;
            }
            if (selected.Kind == RuntimeInstanceKind.TreeClip &&
                candidate.Kind == RuntimeInstanceKind.TreeClip)
            {
                return selected.TreeClipCycle == candidate.TreeClipCycle &&
                       selected.TreeClipOperationIndex == candidate.TreeClipOperationIndex;
            }
            return true;
        }

        static int CompareEvents(RuntimeTraceEvent left, RuntimeTraceEvent right)
        {
            int position = left.Position.CompareTo(right.Position);
            return position != 0 ? position : left.Sequence.CompareTo(right.Sequence);
        }

        static int CompareGroupedEvents(RuntimeTraceEvent left, RuntimeTraceEvent right)
        {
            int position = left.Position.CompareTo(right.Position);
            if (position != 0)
                return position;
            int branch = left.ExecutionBranchId.CompareTo(right.ExecutionBranchId);
            return branch != 0 ? branch : left.Sequence.CompareTo(right.Sequence);
        }

        static int CompareSpans(RuntimeExecutionSpan left, RuntimeExecutionSpan right)
        {
            int position = left.StartPosition.CompareTo(right.StartPosition);
            if (position != 0)
                return position;
            int end = left.EndPosition.CompareTo(right.EndPosition);
            return end != 0 ? end : left.StartSequence.CompareTo(right.StartSequence);
        }

        readonly struct TickKey : IEquatable<TickKey>, IComparable<TickKey>
        {
            public TickKey(ulong tick, Guid executionBranchId)
            {
                Tick = tick;
                ExecutionBranchId = executionBranchId;
            }

            public ulong Tick { get; }
            public Guid ExecutionBranchId { get; }

            public int CompareTo(TickKey other)
            {
                int tick = Tick.CompareTo(other.Tick);
                return tick != 0 ? tick : ExecutionBranchId.CompareTo(other.ExecutionBranchId);
            }

            public bool Equals(TickKey other) =>
                Tick == other.Tick && ExecutionBranchId == other.ExecutionBranchId;

            public override bool Equals(object obj) => obj is TickKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Tick, ExecutionBranchId);
        }

        readonly struct PresentationFrameKey : IEquatable<PresentationFrameKey>, IComparable<PresentationFrameKey>
        {
            public PresentationFrameKey(ulong frame, Guid executionBranchId)
            {
                Frame = frame;
                ExecutionBranchId = executionBranchId;
            }

            public ulong Frame { get; }
            public Guid ExecutionBranchId { get; }

            public int CompareTo(PresentationFrameKey other)
            {
                int frame = Frame.CompareTo(other.Frame);
                return frame != 0 ? frame : ExecutionBranchId.CompareTo(other.ExecutionBranchId);
            }

            public bool Equals(PresentationFrameKey other) =>
                Frame == other.Frame && ExecutionBranchId == other.ExecutionBranchId;

            public override bool Equals(object obj) => obj is PresentationFrameKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Frame, ExecutionBranchId);
        }

        readonly struct CheckpointKey : IEquatable<CheckpointKey>
        {
            public CheckpointKey(
                ulong tick,
                Guid executionBranchId,
                ulong runtimeEpoch,
                RuntimeContentRevision contentRevision,
                string snapshotIdentity)
            {
                Tick = tick;
                ExecutionBranchId = executionBranchId;
                RuntimeEpoch = runtimeEpoch;
                ContentRevision = contentRevision;
                SnapshotIdentity = snapshotIdentity ?? string.Empty;
            }

            readonly ulong Tick;
            readonly Guid ExecutionBranchId;
            readonly ulong RuntimeEpoch;
            readonly RuntimeContentRevision ContentRevision;
            readonly string SnapshotIdentity;

            public bool Equals(CheckpointKey other) =>
                Tick == other.Tick &&
                ExecutionBranchId == other.ExecutionBranchId &&
                RuntimeEpoch == other.RuntimeEpoch &&
                ContentRevision.Equals(other.ContentRevision) &&
                string.Equals(SnapshotIdentity, other.SnapshotIdentity, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is CheckpointKey other && Equals(other);
            public override int GetHashCode()
            {
                int hash = HashCode.Combine(Tick, ExecutionBranchId, RuntimeEpoch, ContentRevision);
                return hash * 397 ^ (SnapshotIdentity?.GetHashCode() ?? 0);
            }
        }

        readonly struct GraphBranchKey : IEquatable<GraphBranchKey>
        {
            public GraphBranchKey(Guid graphRuntimeId, Guid executionBranchId)
            {
                GraphRuntimeId = graphRuntimeId;
                ExecutionBranchId = executionBranchId;
            }

            public Guid GraphRuntimeId { get; }
            public Guid ExecutionBranchId { get; }

            public bool Equals(GraphBranchKey other) =>
                GraphRuntimeId == other.GraphRuntimeId && ExecutionBranchId == other.ExecutionBranchId;

            public override bool Equals(object obj) => obj is GraphBranchKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(GraphRuntimeId, ExecutionBranchId);
        }

        static RuntimeExecutionSpanKind ResolveKind(
            RuntimeTraceEventKind eventKind,
            RuntimeSourceElementKey source,
            bool hasSource)
        {
            if (eventKind is RuntimeTraceEventKind.TimelineRequested or
                RuntimeTraceEventKind.TimelineStarted or
                RuntimeTraceEventKind.TimelineLogicTime or
                RuntimeTraceEventKind.TimelineVisualTime or
                RuntimeTraceEventKind.TimelineCompleted or
                RuntimeTraceEventKind.TimelineCancelled or
                RuntimeTraceEventKind.TimelineStopped)
                return RuntimeExecutionSpanKind.Timeline;
            if (eventKind is RuntimeTraceEventKind.TreeClipEntered or
                RuntimeTraceEventKind.TreeClipUpdated or
                RuntimeTraceEventKind.TreeClipExited or
                RuntimeTraceEventKind.TreeClipDestroyed)
                return RuntimeExecutionSpanKind.TreeClip;
            if (eventKind is RuntimeTraceEventKind.StateScopeEntered or RuntimeTraceEventKind.StateScopeExited)
                return RuntimeExecutionSpanKind.State;
            if (eventKind is RuntimeTraceEventKind.EdgeEvaluated or
                RuntimeTraceEventKind.EdgeSelected or
                RuntimeTraceEventKind.ConditionGraphEvaluated or
                RuntimeTraceEventKind.StateTransitionEvaluated or
                RuntimeTraceEventKind.StateTransitionSelected)
                return RuntimeExecutionSpanKind.Branch;
            if (eventKind == RuntimeTraceEventKind.NodeWaiting)
                return RuntimeExecutionSpanKind.Wait;
            if (hasSource && source.Kind == RuntimeSourceElementKind.Track)
                return RuntimeExecutionSpanKind.Track;
            if (hasSource && source.Kind == RuntimeSourceElementKind.Clip)
                return RuntimeExecutionSpanKind.Clip;
            if (hasSource && source.Kind == RuntimeSourceElementKind.TreeClip)
                return RuntimeExecutionSpanKind.TreeClip;
            if (hasSource && source.Kind == RuntimeSourceElementKind.Graph)
                return RuntimeExecutionSpanKind.Graph;
            if (hasSource && source.Kind == RuntimeSourceElementKind.Node)
                return RuntimeExecutionSpanKind.Node;
            return RuntimeExecutionSpanKind.Point;
        }

        static bool IsPoint(RuntimeTraceEventKind eventKind, RuntimeExecutionSpanKind kind)
        {
            return kind is RuntimeExecutionSpanKind.Point or
                RuntimeExecutionSpanKind.Branch or
                RuntimeExecutionSpanKind.Track or
                RuntimeExecutionSpanKind.Clip ||
                eventKind is RuntimeTraceEventKind.TimelineLogicTime or RuntimeTraceEventKind.TimelineVisualTime or RuntimeTraceEventKind.TreeClipUpdated or RuntimeTraceEventKind.ClipActive;
        }

        static bool IsStart(RuntimeTraceEventKind eventKind, RuntimeExecutionSpanKind kind)
        {
            return kind switch
            {
                RuntimeExecutionSpanKind.Graph => eventKind == RuntimeTraceEventKind.GraphCreated,
                RuntimeExecutionSpanKind.Node => eventKind == RuntimeTraceEventKind.NodeEntered,
                RuntimeExecutionSpanKind.Wait => eventKind == RuntimeTraceEventKind.NodeWaiting,
                RuntimeExecutionSpanKind.State => eventKind == RuntimeTraceEventKind.StateScopeEntered,
                RuntimeExecutionSpanKind.Timeline => eventKind is RuntimeTraceEventKind.TimelineRequested or RuntimeTraceEventKind.TimelineStarted,
                RuntimeExecutionSpanKind.TreeClip => eventKind == RuntimeTraceEventKind.TreeClipEntered,
                _ => false
            };
        }

        static bool IsWaitEnd(RuntimeTraceEventKind eventKind)
        {
            return eventKind is RuntimeTraceEventKind.NodeEntered or
                RuntimeTraceEventKind.NodeStatus or
                RuntimeTraceEventKind.NodeRunning or
                RuntimeTraceEventKind.NodeCompleted or
                RuntimeTraceEventKind.NodeStopRequested or
                RuntimeTraceEventKind.NodeStopping or
                RuntimeTraceEventKind.NodeStopped or
                RuntimeTraceEventKind.NodeForceStopped;
        }

        static bool IsEnd(RuntimeTraceEventKind eventKind, RuntimeExecutionSpanKind kind)
        {
            return kind switch
            {
                RuntimeExecutionSpanKind.Graph => eventKind == RuntimeTraceEventKind.GraphDestroyed,
                RuntimeExecutionSpanKind.Node => eventKind is RuntimeTraceEventKind.NodeCompleted or RuntimeTraceEventKind.NodeStopped or RuntimeTraceEventKind.NodeForceStopped,
                RuntimeExecutionSpanKind.State => eventKind == RuntimeTraceEventKind.StateScopeExited,
                RuntimeExecutionSpanKind.Timeline => eventKind is RuntimeTraceEventKind.TimelineCompleted or RuntimeTraceEventKind.TimelineCancelled or RuntimeTraceEventKind.TimelineStopped,
                RuntimeExecutionSpanKind.TreeClip => eventKind is RuntimeTraceEventKind.TreeClipExited or RuntimeTraceEventKind.TreeClipDestroyed,
                _ => false
            };
        }

        readonly struct SpanKey : IEquatable<SpanKey>
        {
            public SpanKey(
                RuntimeExecutionSpanKind kind,
                RuntimeSourceElementHandle source,
                RuntimeInstanceKey instance,
                Guid executionBranchId,
                ulong runtimeEpoch,
                RuntimeContentRevision contentRevision)
            {
                Kind = kind;
                Source = source;
                Instance = instance;
                ExecutionBranchId = executionBranchId;
                RuntimeEpoch = runtimeEpoch;
                ContentRevision = contentRevision;
            }

            readonly RuntimeExecutionSpanKind Kind;
            readonly RuntimeSourceElementHandle Source;
            readonly RuntimeInstanceKey Instance;
            readonly Guid ExecutionBranchId;
            readonly ulong RuntimeEpoch;
            readonly RuntimeContentRevision ContentRevision;

            public bool Equals(SpanKey other) =>
                Kind == other.Kind && Source.Equals(other.Source) && Instance.Equals(other.Instance) &&
                ExecutionBranchId == other.ExecutionBranchId &&
                RuntimeEpoch == other.RuntimeEpoch &&
                ContentRevision.Equals(other.ContentRevision);
            public override bool Equals(object obj) => obj is SpanKey other && Equals(other);
            public override int GetHashCode()
            {
                int hash = HashCode.Combine((int)Kind, Source, Instance, ExecutionBranchId);
                hash = hash * 397 + RuntimeEpoch.GetHashCode();
                hash = hash * 397 + ContentRevision.GetHashCode();
                return hash;
            }
        }

        struct PendingSpan
        {
            public PendingSpan(RuntimeExecutionSpanKind kind, RuntimeTraceEvent start, RuntimeSourceElementHandle handle, RuntimeSourceElementKey source, bool hasSource)
            {
                Kind = kind;
                Start = start;
                Last = start;
                Handle = handle;
                Source = source;
                HasSource = hasSource;
            }

            readonly RuntimeExecutionSpanKind Kind;
            public readonly RuntimeTraceEvent Start;
            readonly RuntimeSourceElementHandle Handle;
            readonly RuntimeSourceElementKey Source;
            readonly bool HasSource;
            public RuntimeTraceEvent Last;

            public RuntimeExecutionSpan Close(RuntimeTraceEvent end, bool completed = true) =>
                new RuntimeExecutionSpan(Kind, Start, end, Handle, Source, HasSource, completed);
        }
    }
}
