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
        Clip,
        Loop
    }

    public enum RuntimeExecutionSpanState
    {
        Open,
        Completed,
        MissingStart
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
            RuntimeExecutionSpanState state)
        {
            Kind = kind;
            Start = start;
            End = end;
            SourceHandle = sourceHandle;
            Source = source;
            HasSource = hasSource;
            State = state;
        }

        public RuntimeExecutionSpanKind Kind { get; }
        public RuntimeTraceEvent Start { get; }
        public RuntimeTraceEvent End { get; }
        public RuntimeSourceElementHandle SourceHandle { get; }
        public RuntimeSourceElementKey Source { get; }
        public bool HasSource { get; }
        public RuntimeExecutionSpanState State { get; }
        public bool Completed => State == RuntimeExecutionSpanState.Completed;
        public bool IsOpen => State == RuntimeExecutionSpanState.Open;
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
        public int LoopIteration => Start.Payload.LoopIteration;
    }

    public sealed class RuntimeExecutionTimeline
    {
        readonly IReadOnlyList<RuntimeExecutionSpan> m_Spans;
        readonly IReadOnlyDictionary<(RuntimeTraceDomain, Guid, ulong), ulong> m_ClockPositions;

        internal RuntimeExecutionTimeline(
            Guid captureId,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            long version,
            long evictedEvents,
            bool complete,
            int unmappedEventCount,
            ulong latestLogicPosition,
            ulong latestPresentationPosition,
            IReadOnlyDictionary<(RuntimeTraceDomain, Guid, ulong), ulong> clockPositions,
            IReadOnlyList<RuntimeExecutionSpan> spans)
        {
            CaptureId = captureId;
            Channels = channels;
            Detail = detail;
            Version = version;
            EvictedEvents = evictedEvents;
            IsComplete = complete;
            UnmappedEventCount = unmappedEventCount;
            LatestLogicPosition = latestLogicPosition;
            LatestPresentationPosition = latestPresentationPosition;
            m_ClockPositions = clockPositions;
            m_Spans = spans;
        }

        public Guid CaptureId { get; }
        public RuntimeTraceChannel Channels { get; private set; }
        public RuntimeDiagnosticsCaptureDetail Detail { get; private set; }
        public long Version { get; private set; }
        public long EvictedEvents { get; private set; }
        public bool IsComplete { get; private set; }
        public int UnmappedEventCount { get; private set; }
        public ulong LatestLogicPosition { get; private set; }
        public ulong LatestPresentationPosition { get; private set; }
        public IReadOnlyList<RuntimeExecutionSpan> Spans => m_Spans;

        public ulong GetObservedEndPosition(RuntimeExecutionSpan span)
        {
            if (!span.IsOpen)
                return span.EndPosition;
            RuntimeTraceDomain domain = span.Domain == RuntimeTraceDomain.Lifecycle ? RuntimeTraceDomain.Logic : span.Domain;
            return Math.Max(span.EndPosition, m_ClockPositions[(domain, span.ExecutionBranchId, span.RuntimeEpoch)]);
        }

        internal void Update(RuntimeCaptureRead capture, bool complete, int unmappedEventCount,
            ulong latestLogicPosition, ulong latestPresentationPosition)
        {
            Channels = capture.Channels;
            Detail = capture.Detail;
            Version = capture.Version;
            EvictedEvents = capture.EvictedEvents;
            IsComplete = complete;
            UnmappedEventCount = unmappedEventCount;
            LatestLogicPosition = latestLogicPosition;
            LatestPresentationPosition = latestPresentationPosition;
        }
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
        readonly RuntimeTraceEvent[] m_Events;
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
            RuntimeTraceEvent[] events)
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
            for (int i = 0; i < m_Events.Length; i++)
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
            for (int i = 0; i < m_Events.Length; i++)
                if (m_Events[i].Kind == kind)
                    return true;
            return false;
        }

    }

    public sealed class RuntimeExecutionPresentationFrame
    {
        readonly RuntimeTraceEvent[] m_Events;

        internal RuntimeExecutionPresentationFrame(
            ulong frame,
            Guid executionBranchId,
            RuntimeTraceEvent[] events)
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
            for (int i = 0; i < m_Events.Length; i++)
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
        readonly RuntimeExecutionTickRecord[] m_Ticks;
        readonly RuntimeExecutionCheckpoint[] m_Checkpoints;
        readonly RuntimeExecutionPresentationFrame[] m_PresentationFrames;

        internal RuntimeExecutionHistory(
            Guid captureId,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            long version,
            long evictedEvents,
            bool complete,
            int unmappedEventCount,
            RuntimeExecutionTickRecord[] ticks,
            RuntimeExecutionCheckpoint[] checkpoints,
            RuntimeExecutionPresentationFrame[] presentationFrames)
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
                for (int i = 0; i < m_Ticks.Length; i++)
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
                for (int i = 0; i < m_Ticks.Length; i++)
                    count += m_Ticks[i].ExternalResults.Count;
                return count;
            }
        }
        public bool HasPresentationFrames => m_PresentationFrames.Length > 0;
    }

    internal static class RuntimeExecutionTimelineBuilder
    {
        static readonly Comparison<RuntimeTraceEvent> CompareEventsComparer = CompareEvents;
        static readonly Comparison<RuntimeTraceEvent> CompareGroupedEventsComparer = CompareGroupedEvents;
        static readonly SelectionScratch Selection = new();
        static readonly SpanAccumulator FrozenSpans = new();
        static readonly HashSet<CheckpointKey> CheckpointKeys = new();
        static readonly HashSet<ulong> BoundarySequences = new();
        static readonly HashSet<Guid> BoundaryBranches = new();
        static readonly List<EventGroup<TickKey>> HistoryGroups = new();
        static readonly List<EventGroup<PresentationFrameKey>> PresentationGroups = new();
        static readonly List<RuntimeExecutionTickRecord> TickResults = new();
        static readonly List<RuntimeExecutionCheckpoint> CheckpointResults = new();
        static readonly List<RuntimeExecutionPresentationFrame> PresentationFrameResults = new();

        internal static RuntimeExecutionTimeline Build(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default,
            ulong throughSequence = ulong.MaxValue)
        {
            return BuildCore(capture, sourceMap, null, historyOffset, instance, throughSequence);
        }

        internal static RuntimeExecutionTimeline Build(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default,
            ulong throughSequence = ulong.MaxValue)
        {
            return BuildCore(capture, sourceMap, sourceMaps, historyOffset, instance, throughSequence);
        }

        static RuntimeExecutionTimeline BuildCore(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset,
            RuntimeInstanceKey instance,
            ulong throughSequence)
        {
            if (capture == null)
                throw new ArgumentNullException(nameof(capture));
            if (sourceMap == null)
                throw new ArgumentNullException(nameof(sourceMap));
            if (historyOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(historyOffset));

            ReadOnlySpan<RuntimeTraceEvent> events = capture.GetEvents(historyOffset, throughSequence);
            List<RuntimeTraceEvent> selected = SelectEvents(events, instance);
            selected.Sort(CompareEventsComparer);

            FrozenSpans.Reset();
            for (int i = 0; i < selected.Count; i++)
                FrozenSpans.Append(selected[i], sourceMap, sourceMaps);
            return new RuntimeExecutionTimeline(
                capture.CaptureId, capture.Channels, capture.Detail, capture.Version, capture.EvictedEvents,
                FrozenSpans.IsComplete(capture.EvictedEvents), FrozenSpans.UnmappedEventCount,
                FrozenSpans.LatestLogicPosition, FrozenSpans.LatestPresentationPosition,
                new Dictionary<(RuntimeTraceDomain, Guid, ulong), ulong>(FrozenSpans.ClockPositions), FrozenSpans.Spans.ToArray());
        }

        internal sealed class SpanAccumulator
        {
            readonly Dictionary<SpanKey, int> m_Open;
            int m_MissingStartCount;
            internal readonly List<RuntimeExecutionSpan> Spans;
            internal readonly Dictionary<(RuntimeTraceDomain, Guid, ulong), ulong> ClockPositions;
            internal int UnmappedEventCount { get; private set; }
            internal ulong LatestLogicPosition { get; private set; }
            internal ulong LatestPresentationPosition { get; private set; }

            internal SpanAccumulator(int capacity = 0)
            {
                m_Open = new Dictionary<SpanKey, int>(capacity);
                Spans = new List<RuntimeExecutionSpan>(capacity);
                ClockPositions = new Dictionary<(RuntimeTraceDomain, Guid, ulong), ulong>(capacity);
            }

            internal void Reset()
            {
                m_Open.Clear();
                Spans.Clear();
                ClockPositions.Clear();
                UnmappedEventCount = 0;
                m_MissingStartCount = 0;
                LatestLogicPosition = 0;
                LatestPresentationPosition = 0;
            }

            internal bool IsComplete(long evictedEvents) =>
                evictedEvents == 0 && Spans.Count != 0 && UnmappedEventCount == 0 && m_MissingStartCount == 0 && m_Open.Count == 0;

            internal void Append(RuntimeTraceEvent value, RuntimeDebugSourceMapSnapshot sourceMap,
                IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps)
            {
                RuntimeTraceDomain clockDomain = value.Domain == RuntimeTraceDomain.Lifecycle ? RuntimeTraceDomain.Logic : value.Domain;
                ClockPositions[(clockDomain, value.ExecutionBranchId, value.RuntimeEpoch)] = value.Position;
                if (value.Domain == RuntimeTraceDomain.Presentation)
                    LatestPresentationPosition = value.Position;
                else
                    LatestLogicPosition = value.Position;
                if (value.Kind == RuntimeTraceEventKind.BlackboardSnapshot)
                    return;
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
                    UnmappedEventCount++;
                RuntimeExecutionSpanKind kind = ResolveKind(value.Kind, source, hasSource);
                SpanKey key = Key(kind, value);
                if (value.Kind == RuntimeTraceEventKind.NodeWaiting)
                {
                    if (m_Open.TryGetValue(key, out int waiting))
                        UpdateSpan(waiting, value, false);
                    else
                        OpenSpan(key, kind, value, source, hasSource);
                    if (m_Open.TryGetValue(Key(RuntimeExecutionSpanKind.Node, value), out int node))
                        UpdateSpan(node, value, false);
                    return;
                }
                if (value.Payload.LoopIteration > 0 && value.Kind is RuntimeTraceEventKind.NodeCompleted or
                    RuntimeTraceEventKind.NodeStopped or RuntimeTraceEventKind.NodeForceStopped)
                {
                    SpanKey loopKey = Key(RuntimeExecutionSpanKind.Loop, value);
                    if (m_Open.TryGetValue(loopKey, out int activeLoop))
                    {
                        UpdateSpan(activeLoop, value, true);
                        m_Open.Remove(loopKey);
                    }
                }
                SpanKey waitKey = Key(RuntimeExecutionSpanKind.Wait, value);
                if (IsWaitEnd(value.Kind) && m_Open.TryGetValue(waitKey, out int activeWait))
                {
                    UpdateSpan(activeWait, value, true);
                    m_Open.Remove(waitKey);
                }
                if (IsPoint(value.Kind, kind))
                {
                    if (m_Open.TryGetValue(key, out int activePoint))
                        UpdateSpan(activePoint, value, false);
                    Spans.Add(new RuntimeExecutionSpan(kind, value, value, handle, source, hasSource, RuntimeExecutionSpanState.Completed));
                    return;
                }
                if (IsStart(value.Kind, kind))
                {
                    if (m_Open.TryGetValue(key, out int previous))
                    {
                        RuntimeExecutionSpan previousSpan = Spans[previous];
                        if (kind == RuntimeExecutionSpanKind.Timeline &&
                            previousSpan.Start.Kind == RuntimeTraceEventKind.TimelineRequested &&
                            value.Kind == RuntimeTraceEventKind.TimelineStarted)
                        {
                            UpdateSpan(previous, value, false);
                            return;
                        }
                        UpdateSpan(previous, previousSpan.End, true);
                        m_Open.Remove(key);
                    }
                    OpenSpan(key, kind, value, source, hasSource);
                    return;
                }
                if (m_Open.TryGetValue(key, out int active))
                {
                    bool completed = IsEnd(value.Kind, kind);
                    UpdateSpan(active, value, completed);
                    if (completed)
                        m_Open.Remove(key);
                }
                else
                {
                    m_MissingStartCount++;
                    Spans.Add(new RuntimeExecutionSpan(kind, value, value, handle, source, hasSource, RuntimeExecutionSpanState.MissingStart));
                }
            }

            static SpanKey Key(RuntimeExecutionSpanKind kind, RuntimeTraceEvent value) =>
                new SpanKey(kind, value.Source, value.RuntimeInstance, value.ExecutionBranchId,
                    value.RuntimeEpoch, value.ContentRevision, value.Domain,
                    kind is RuntimeExecutionSpanKind.Node or RuntimeExecutionSpanKind.Wait or RuntimeExecutionSpanKind.Loop
                        ? value.Payload.ActivationGeneration : 0,
                    kind == RuntimeExecutionSpanKind.Loop ? value.Payload.LoopIteration : 0);

            void OpenSpan(SpanKey key, RuntimeExecutionSpanKind kind, RuntimeTraceEvent value,
                RuntimeSourceElementKey source, bool hasSource)
            {
                m_Open.Add(key, Spans.Count);
                Spans.Add(new RuntimeExecutionSpan(kind, value, value, value.Source, source, hasSource, RuntimeExecutionSpanState.Open));
            }

            void UpdateSpan(int index, RuntimeTraceEvent end, bool completed)
            {
                RuntimeExecutionSpan span = Spans[index];
                Spans[index] = new RuntimeExecutionSpan(span.Kind, span.Start, end,
                    span.SourceHandle, span.Source, span.HasSource,
                    completed ? RuntimeExecutionSpanState.Completed : RuntimeExecutionSpanState.Open);
            }
        }

        internal static RuntimeExecutionHistory BuildHistory(
            RuntimeCaptureSnapshot capture,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default,
            ulong throughSequence = ulong.MaxValue)
        {
            return BuildHistory(capture, null, null, historyOffset, instance, throughSequence);
        }

        internal static RuntimeExecutionHistory BuildHistory(
            RuntimeCaptureSnapshot capture,
            RuntimeDebugSourceMapSnapshot sourceMap,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default,
            ulong throughSequence = ulong.MaxValue)
        {
            if (capture == null)
                throw new ArgumentNullException(nameof(capture));
            if (historyOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(historyOffset));

            ReadOnlySpan<RuntimeTraceEvent> allEvents = capture.GetEvents(historyOffset, throughSequence);
            List<RuntimeTraceEvent> selectedEvents = SelectEvents(allEvents, instance);
            if (instance.IsValid && selectedEvents.Count > 0)
                AddSessionBoundaryEvents(
                    allEvents,
                    selectedEvents,
                    selectedEvents);

            selectedEvents.Sort(CompareGroupedEventsComparer);
            List<EventGroup<TickKey>> grouped = HistoryGroups;
            List<EventGroup<PresentationFrameKey>> presentation = PresentationGroups;
            grouped.Clear();
            presentation.Clear();
            for (int i = 0; i < selectedEvents.Count; i++)
            {
                RuntimeTraceEvent value = selectedEvents[i];
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
                    EventGroup<PresentationFrameKey> presentationGroup = presentation[presentation.Count - 1];
                    presentationGroup.AddCount();
                    presentation[presentation.Count - 1] = presentationGroup;
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
                EventGroup<TickKey> tickGroup = grouped[grouped.Count - 1];
                tickGroup.AddCount();
                grouped[grouped.Count - 1] = tickGroup;
            }

            for (int i = 0; i < grouped.Count; i++)
            {
                EventGroup<TickKey> group = grouped[i];
                group.PrepareEvents();
                grouped[i] = group;
            }
            for (int i = 0; i < presentation.Count; i++)
            {
                EventGroup<PresentationFrameKey> group = presentation[i];
                group.PrepareEvents();
                presentation[i] = group;
            }

            int tickGroupIndex = 0;
            int presentationGroupIndex = 0;
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
                    EventGroup<PresentationFrameKey> presentationWriter = presentation[presentationGroupIndex];
                    presentationWriter.Write(value);
                    presentation[presentationGroupIndex] = presentationWriter;
                    if (presentationWriter.IsFull)
                        presentationGroupIndex++;
                    continue;
                }

                EventGroup<TickKey> tickWriter = grouped[tickGroupIndex];
                tickWriter.Write(value);
                grouped[tickGroupIndex] = tickWriter;
                if (tickWriter.IsFull)
                    tickGroupIndex++;
            }

            List<RuntimeExecutionTickRecord> ticks = TickResults;
            List<RuntimeExecutionCheckpoint> checkpoints = CheckpointResults;
            List<RuntimeExecutionPresentationFrame> presentationFrames = PresentationFrameResults;
            ticks.Clear();
            checkpoints.Clear();
            presentationFrames.Clear();
            HashSet<CheckpointKey> checkpointKeys = CheckpointKeys;
            checkpointKeys.Clear();
            bool complete = capture.EvictedEvents == 0 && grouped.Count != 0;
            foreach (EventGroup<TickKey> group in grouped)
            {
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
                for (int i = 0; i < group.Events.Length; i++)
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
                presentationFrames.Add(new RuntimeExecutionPresentationFrame(
                    group.Key.Frame,
                    group.Key.ExecutionBranchId,
                    group.Events));
            }
            RuntimeExecutionHistory history = new(
                capture.CaptureId,
                capture.Channels,
                capture.Detail,
                capture.Version,
                capture.EvictedEvents,
                historyComplete,
                unmappedEventCount,
                ticks.ToArray(),
                checkpoints.ToArray(),
                presentationFrames.ToArray());
            grouped.Clear();
            presentation.Clear();
            ticks.Clear();
            checkpoints.Clear();
            presentationFrames.Clear();
            return history;
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

            HashSet<ulong> selectedSequences = BoundarySequences;
            HashSet<Guid> selectedBranches = BoundaryBranches;
            selectedSequences.Clear();
            selectedBranches.Clear();
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

        struct EventGroup<TKey>
        {
            TKey m_Key;
            RuntimeTraceEvent[] m_Events;
            int m_Count;

            internal EventGroup(TKey key)
            {
                m_Key = key;
                m_Events = Array.Empty<RuntimeTraceEvent>();
                m_Count = 0;
            }

            internal TKey Key => m_Key;
            internal RuntimeTraceEvent[] Events => m_Events;
            internal bool IsFull => m_Count == m_Events.Length;

            internal void AddCount()
            {
                m_Count++;
            }

            internal void PrepareEvents()
            {
                m_Events = new RuntimeTraceEvent[m_Count];
                m_Count = 0;
            }

            internal void Write(RuntimeTraceEvent value)
            {
                m_Events[m_Count++] = value;
            }
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
            return left.Sequence.CompareTo(right.Sequence);
        }

        static int CompareGroupedEvents(RuntimeTraceEvent left, RuntimeTraceEvent right)
        {
            int position = left.Position.CompareTo(right.Position);
            if (position != 0)
                return position;
            int branch = left.ExecutionBranchId.CompareTo(right.ExecutionBranchId);
            return branch != 0 ? branch : left.Sequence.CompareTo(right.Sequence);
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
            if (eventKind is RuntimeTraceEventKind.LoopIterationEntered or RuntimeTraceEventKind.LoopIterationCompleted)
                return RuntimeExecutionSpanKind.Loop;
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
                RuntimeExecutionSpanKind.Loop => eventKind == RuntimeTraceEventKind.LoopIterationEntered,
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
                RuntimeExecutionSpanKind.Loop => eventKind == RuntimeTraceEventKind.LoopIterationCompleted,
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
                RuntimeContentRevision contentRevision,
                RuntimeTraceDomain domain,
                ulong activationGeneration,
                int loopIteration)
            {
                Kind = kind;
                ActivationGeneration = activationGeneration;
                LoopIteration = loopIteration;
                Domain = domain == RuntimeTraceDomain.Lifecycle ? RuntimeTraceDomain.Logic : domain;
                Source = source;
                Instance = instance;
                ExecutionBranchId = executionBranchId;
                RuntimeEpoch = runtimeEpoch;
                ContentRevision = contentRevision;
            }

            readonly RuntimeExecutionSpanKind Kind;
            readonly ulong ActivationGeneration;
            readonly int LoopIteration;
            readonly RuntimeTraceDomain Domain;
            readonly RuntimeSourceElementHandle Source;
            readonly RuntimeInstanceKey Instance;
            readonly Guid ExecutionBranchId;
            readonly ulong RuntimeEpoch;
            readonly RuntimeContentRevision ContentRevision;

            public bool Equals(SpanKey other) =>
                Kind == other.Kind && ActivationGeneration == other.ActivationGeneration && LoopIteration == other.LoopIteration &&
                Domain == other.Domain && Source.Equals(other.Source) && Instance.Equals(other.Instance) &&
                ExecutionBranchId == other.ExecutionBranchId &&
                RuntimeEpoch == other.RuntimeEpoch &&
                ContentRevision.Equals(other.ContentRevision);
            public override bool Equals(object obj) => obj is SpanKey other && Equals(other);
            public override int GetHashCode()
            {
                int hash = HashCode.Combine((int)Kind, (int)Domain, Source, Instance, ExecutionBranchId);
                hash = hash * 397 + RuntimeEpoch.GetHashCode();
                hash = hash * 397 + ContentRevision.GetHashCode();
                hash = hash * 397 + ActivationGeneration.GetHashCode();
                hash = hash * 397 + LoopIteration;
                return hash;
            }
        }

    }
}
