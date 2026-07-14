using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    public readonly struct RuntimeDebugTargetInfo
    {
        public RuntimeDebugTargetInfo(RuntimeDiagnosticsTarget target)
        {
            DisplayName = target?.DisplayName ?? string.Empty;
            HostInstanceId = target?.HostInstanceId ?? 0;
            CharacterRuntimeId = target?.CharacterRuntimeId ?? Guid.Empty;
            SessionId = target?.SessionId ?? Guid.Empty;
            Revision = target?.Revision ?? default;
        }

        public string DisplayName { get; }
        public int HostInstanceId { get; }
        public Guid CharacterRuntimeId { get; }
        public Guid SessionId { get; }
        public RuntimeProgramRevision Revision { get; }
    }

    public readonly struct RuntimeDebugEventView
    {
        public RuntimeDebugEventView(RuntimeTraceEvent traceEvent, RuntimeSourceElementKey source, string sourceName)
        {
            Event = traceEvent;
            Source = source;
            SourceName = sourceName ?? string.Empty;
        }

        public RuntimeTraceEvent Event { get; }
        public RuntimeSourceElementKey Source { get; }
        public string SourceName { get; }
    }

    public readonly struct RuntimeElementDebugState
    {
        public RuntimeElementDebugState(RuntimeDebugEventView eventView)
        {
            Source = eventView.Source;
            SourceName = eventView.SourceName;
            Instance = eventView.Event.RuntimeInstance;
            Kind = eventView.Event.Kind;
            Domain = eventView.Event.Domain;
            Position = eventView.Event.Position;
            Sequence = eventView.Event.Sequence;
            Payload = eventView.Event.Payload;
        }

        public RuntimeSourceElementKey Source { get; }
        public string SourceName { get; }
        public RuntimeInstanceKey Instance { get; }
        public RuntimeTraceEventKind Kind { get; }
        public RuntimeTraceDomain Domain { get; }
        public ulong Position { get; }
        public ulong Sequence { get; }
        public RuntimeTracePayload Payload { get; }
        public string Status => !string.IsNullOrEmpty(Payload.Status) ? Payload.Status : Kind.ToString();
    }

    public readonly struct RuntimeTimelinePlaybackDebugSummary
    {
        public RuntimeTimelinePlaybackDebugSummary(
            RuntimeInstanceKey playback,
            RuntimeTimelinePlaybackProvenance provenance,
            ulong latestLogicTick,
            ulong latestPresentationFrame,
            float logicTime,
            float visualTime,
            int cycle,
            RuntimeTraceEventKind lifecycle,
            string lifecycleStatus,
            RuntimeTraceEventKind terminal,
            string terminalCause)
        {
            Playback = playback;
            Provenance = provenance;
            LatestLogicTick = latestLogicTick;
            LatestPresentationFrame = latestPresentationFrame;
            LogicTime = logicTime;
            VisualTime = visualTime;
            Cycle = cycle;
            Lifecycle = lifecycle;
            LifecycleStatus = lifecycleStatus ?? string.Empty;
            Terminal = terminal;
            TerminalCause = terminalCause ?? string.Empty;
        }

        public RuntimeInstanceKey Playback { get; }
        public RuntimeTimelinePlaybackProvenance Provenance { get; }
        public ulong LatestLogicTick { get; }
        public ulong LatestPresentationFrame { get; }
        public float LogicTime { get; }
        public float VisualTime { get; }
        public int Cycle { get; }
        public RuntimeTraceEventKind Lifecycle { get; }
        public string LifecycleStatus { get; }
        public RuntimeTraceEventKind Terminal { get; }
        public string TerminalCause { get; }
        public bool IsTerminal => Terminal is RuntimeTraceEventKind.TimelineCompleted or RuntimeTraceEventKind.TimelineCancelled or RuntimeTraceEventKind.TimelineStopped;
    }

    public sealed class RuntimeDebugChangeSet
    {
        readonly IReadOnlyCollection<RuntimeSourceElementKey> m_Sources;
        readonly IReadOnlyCollection<RuntimeInstanceKey> m_Instances;

        internal RuntimeDebugChangeSet(long revision, bool fullSync, ICollection<RuntimeSourceElementKey> sources, ICollection<RuntimeInstanceKey> instances, long captureVersion = 0)
        {
            Revision = revision;
            FullSync = fullSync;
            CaptureVersion = captureVersion;
            m_Sources = sources == null ? Array.Empty<RuntimeSourceElementKey>() : new List<RuntimeSourceElementKey>(sources);
            m_Instances = instances == null ? Array.Empty<RuntimeInstanceKey>() : new List<RuntimeInstanceKey>(instances);
        }

        public static RuntimeDebugChangeSet Empty { get; } = new RuntimeDebugChangeSet(0, false, null, null);
        public long Revision { get; }
        public bool FullSync { get; }
        public long CaptureVersion { get; }
        public IReadOnlyCollection<RuntimeSourceElementKey> Sources => m_Sources;
        public IReadOnlyCollection<RuntimeInstanceKey> Instances => m_Instances;

        public bool AffectsSource(RuntimeSourceElementKey source)
        {
            if (FullSync)
                return true;
            foreach (RuntimeSourceElementKey item in m_Sources)
            {
                if (item.Equals(source))
                    return true;
            }
            return false;
        }

        public bool AffectsGraph(string graphAuthoringId, RuntimeInstanceKey instance)
        {
            if (FullSync)
                return true;
            foreach (RuntimeSourceElementKey source in m_Sources)
            {
                if (string.Equals(source.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal))
                    return !instance.IsValid || ContainsInstance(instance);
            }
            return false;
        }

        public bool AffectsTimeline(string timelineAuthoringId, RuntimeInstanceKey playback)
        {
            if (FullSync)
                return true;
            foreach (RuntimeSourceElementKey source in m_Sources)
            {
                if (string.Equals(source.TimelineAuthoringId, timelineAuthoringId, StringComparison.Ordinal))
                    return !playback.IsValid || ContainsInstance(playback);
            }
            return false;
        }

        bool ContainsInstance(RuntimeInstanceKey instance)
        {
            foreach (RuntimeInstanceKey item in m_Instances)
            {
                if (item.Equals(instance))
                    return true;
            }
            return false;
        }
    }

    public sealed class RuntimeDebugViewModel
    {
        readonly RuntimeDebugSourceMapSnapshot m_SourceMap;
        readonly Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView> m_CurrentEvents = new Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>();
        readonly Dictionary<ElementInstanceKey, RuntimeElementDebugState> m_ElementStates = new Dictionary<ElementInstanceKey, RuntimeElementDebugState>();
        readonly Dictionary<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>> m_Instances = new Dictionary<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>>();
        readonly Dictionary<string, HashSet<RuntimeInstanceKey>> m_GraphInstanceMembership = new Dictionary<string, HashSet<RuntimeInstanceKey>>(StringComparer.Ordinal);
        readonly Dictionary<string, long> m_GraphInstanceRevisions = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder> m_TimelinePlayback = new Dictionary<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder>();
        readonly Dictionary<RuntimeInstanceKey, Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>> m_PlaybackEvents = new Dictionary<RuntimeInstanceKey, Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>>();
        readonly Dictionary<string, HashSet<RuntimeInstanceKey>> m_TimelinePlaybackMembership = new Dictionary<string, HashSet<RuntimeInstanceKey>>(StringComparer.Ordinal);
        readonly Dictionary<string, long> m_TimelinePlaybackRevisions = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly HashSet<RuntimeSourceElementKey> m_PendingSources = new HashSet<RuntimeSourceElementKey>();
        readonly HashSet<RuntimeInstanceKey> m_PendingInstances = new HashSet<RuntimeInstanceKey>();
        RuntimeDebugChangeSet m_Changes = RuntimeDebugChangeSet.Empty;
        RuntimeTraceChannel m_Channels;
        string m_Error = string.Empty;
        bool m_PendingFullSync;
        ulong m_LatestLogicTick;
        ulong m_LatestPresentationFrame;
        long m_Revision;

        internal RuntimeDebugViewModel(RuntimeDebugTargetInfo target, RuntimeDebugSourceMapSnapshot sourceMap, RuntimeTraceChannel channels)
        {
            Target = target;
            m_SourceMap = sourceMap ?? RuntimeDebugSourceMapSnapshot.Empty;
            m_Channels = channels;
        }

        public static RuntimeDebugViewModel Detached { get; } = new RuntimeDebugViewModel(default, RuntimeDebugSourceMapSnapshot.Empty, RuntimeTraceChannel.None);
        public RuntimeDebugTargetInfo Target { get; }
        public RuntimeTraceChannel Channels => m_Channels;
        public bool Attached => Target.CharacterRuntimeId != Guid.Empty;
        public bool Valid => Attached && string.IsNullOrEmpty(m_Error);
        public string Error => m_Error;
        public ulong LatestLogicTick => m_LatestLogicTick;
        public ulong LatestPresentationFrame => m_LatestPresentationFrame;
        public long Revision => m_Revision;
        public RuntimeDebugChangeSet Changes => m_Changes;

        public RuntimeDebugTargetMatch MatchSource(RuntimeDebugTargetRequest request)
        {
            return m_SourceMap.Match(request);
        }

        public IReadOnlyList<RuntimeInstanceKey> GetInstances(RuntimeSourceElementKey source)
        {
            if (!m_Instances.TryGetValue(source, out Dictionary<RuntimeInstanceKey, ulong> instances))
                return Array.Empty<RuntimeInstanceKey>();

            var result = new List<RuntimeInstanceKey>(instances.Keys);
            result.Sort((left, right) => instances[right].CompareTo(instances[left]));
            return result;
        }

        public IReadOnlyList<RuntimeInstanceKey> GetGraphInstances(string graphAuthoringId)
        {
            return CollectInstances(source => string.Equals(source.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal));
        }

        public long GetGraphInstanceRevision(string graphAuthoringId)
        {
            return !string.IsNullOrEmpty(graphAuthoringId) && m_GraphInstanceRevisions.TryGetValue(graphAuthoringId, out long revision)
                ? revision
                : 0;
        }

        public IReadOnlyList<RuntimeInstanceKey> GetTimelineInstances(string timelineAuthoringId)
        {
            var result = new List<RuntimeInstanceKey>();
            foreach (KeyValuePair<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder> pair in m_TimelinePlayback)
            {
                if (string.Equals(pair.Value.TimelineAuthoringId, timelineAuthoringId, StringComparison.Ordinal))
                    result.Add(pair.Key);
            }
            result.Sort((left, right) => GetTimelineSequence(right).CompareTo(GetTimelineSequence(left)));
            return result;
        }

        public long GetTimelinePlaybackRevision(string timelineAuthoringId)
        {
            return !string.IsNullOrEmpty(timelineAuthoringId) && m_TimelinePlaybackRevisions.TryGetValue(timelineAuthoringId, out long revision)
                ? revision
                : 0;
        }

        public IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> GetTimelinePlaybackSummaries(string timelineAuthoringId)
        {
            var result = new List<RuntimeTimelinePlaybackDebugSummary>();
            foreach (TimelinePlaybackSummaryBuilder builder in m_TimelinePlayback.Values)
            {
                if (string.Equals(builder.TimelineAuthoringId, timelineAuthoringId, StringComparison.Ordinal))
                    result.Add(builder.Build());
            }
            result.Sort((left, right) => right.LatestLogicTick != left.LatestLogicTick
                ? right.LatestLogicTick.CompareTo(left.LatestLogicTick)
                : right.LatestPresentationFrame.CompareTo(left.LatestPresentationFrame));
            return result;
        }

        public bool TryGetTimelinePlaybackSummary(string timelineAuthoringId, RuntimeInstanceKey playback, out RuntimeTimelinePlaybackDebugSummary summary)
        {
            if (m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder) &&
                string.Equals(builder.TimelineAuthoringId, timelineAuthoringId, StringComparison.Ordinal))
            {
                summary = builder.Build();
                return true;
            }

            summary = default;
            return false;
        }

        public IReadOnlyList<RuntimeDebugEventView> GetTimelineCurrentEvents(string timelineAuthoringId, RuntimeInstanceKey playback)
        {
            if (!m_PlaybackEvents.TryGetValue(playback, out Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView> events))
                return Array.Empty<RuntimeDebugEventView>();
            if (!m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder) ||
                !string.Equals(builder.TimelineAuthoringId, timelineAuthoringId, StringComparison.Ordinal))
                return Array.Empty<RuntimeDebugEventView>();

            var result = new List<RuntimeDebugEventView>(events.Values);
            result.Sort((left, right) => right.Event.Sequence.CompareTo(left.Event.Sequence));
            return result;
        }

        public IReadOnlyList<RuntimeDebugEventView> GetCurrentEvents(RuntimeTraceChannel channel, RuntimeInstanceKey instance = default)
        {
            var result = new List<RuntimeDebugEventView>();
            foreach (RuntimeDebugEventView eventView in m_CurrentEvents.Values)
            {
                if (eventView.Event.Channel != channel)
                    continue;
                if (instance.IsValid && !eventView.Event.RuntimeInstance.Equals(instance))
                    continue;
                result.Add(eventView);
            }
            result.Sort((left, right) => right.Event.Sequence.CompareTo(left.Event.Sequence));
            return result;
        }

        public IReadOnlyList<RuntimeElementDebugState> GetGraphStates(string graphAuthoringId, RuntimeInstanceKey instance, bool changedOnly)
        {
            var result = new List<RuntimeElementDebugState>();
            foreach (KeyValuePair<ElementInstanceKey, RuntimeElementDebugState> pair in m_ElementStates)
            {
                if (!string.Equals(pair.Key.Source.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal) ||
                    !pair.Key.Instance.Equals(instance))
                    continue;
                if (changedOnly && !m_Changes.AffectsSource(pair.Key.Source))
                    continue;
                result.Add(pair.Value);
            }
            return result;
        }

        public bool TryGetState(RuntimeSourceElementKey source, RuntimeInstanceKey instance, out RuntimeElementDebugState state)
        {
            state = default;
            return instance.IsValid && m_ElementStates.TryGetValue(new ElementInstanceKey(source, instance), out state);
        }

        internal void SetChannels(RuntimeTraceChannel channels)
        {
            m_Channels = channels;
        }

        internal void BeginUpdate(bool fullSync)
        {
            m_PendingFullSync = fullSync;
            m_PendingSources.Clear();
            m_PendingInstances.Clear();
            if (!fullSync)
                return;

            m_CurrentEvents.Clear();
            m_ElementStates.Clear();
            m_Instances.Clear();
            m_GraphInstanceMembership.Clear();
            m_GraphInstanceRevisions.Clear();
            m_TimelinePlayback.Clear();
            m_PlaybackEvents.Clear();
            m_TimelinePlaybackMembership.Clear();
            m_TimelinePlaybackRevisions.Clear();
            m_Error = string.Empty;
            m_LatestLogicTick = 0;
            m_LatestPresentationFrame = 0;
        }

        internal void Apply(RuntimeLiveStateKey key, RuntimeTraceEvent traceEvent)
        {
            if (!traceEvent.ProgramRevision.Equals(Target.Revision))
            {
                m_Error = $"Trace revision mismatch: {traceEvent.ProgramRevision} != {Target.Revision}";
                return;
            }

            RuntimeSourceElementKey source = default;
            string sourceName = string.Empty;
            if (traceEvent.Source.IsValid)
            {
                if (!m_SourceMap.TryResolve(traceEvent.Source, out source, out sourceName))
                {
                    m_Error = $"Trace source handle is absent from Source Map: {traceEvent.Source}";
                    return;
                }
            }

            if (traceEvent.Domain == RuntimeTraceDomain.Logic)
                m_LatestLogicTick = Math.Max(m_LatestLogicTick, traceEvent.Position);
            else if (traceEvent.Domain == RuntimeTraceDomain.Presentation)
                m_LatestPresentationFrame = Math.Max(m_LatestPresentationFrame, traceEvent.Position);

            var eventView = new RuntimeDebugEventView(traceEvent, source, sourceName);
            m_CurrentEvents[key] = eventView;
            if (source.IsValid)
                m_PendingSources.Add(source);
            if (traceEvent.RuntimeInstance.IsValid)
                m_PendingInstances.Add(traceEvent.RuntimeInstance);

            if (source.IsValid && traceEvent.RuntimeInstance.IsValid)
            {
                var elementKey = new ElementInstanceKey(source, traceEvent.RuntimeInstance);
                m_ElementStates[elementKey] = new RuntimeElementDebugState(eventView);
                if (!m_Instances.TryGetValue(source, out Dictionary<RuntimeInstanceKey, ulong> instances))
                {
                    instances = new Dictionary<RuntimeInstanceKey, ulong>();
                    m_Instances.Add(source, instances);
                }
                instances[traceEvent.RuntimeInstance] = traceEvent.Sequence;
                RegisterGraphInstance(source.GraphAuthoringId, traceEvent.RuntimeInstance);
            }

            ApplyTimeline(eventView, key);
        }

        internal void CommitUpdate(long captureVersion = 0)
        {
            m_Revision++;
            m_Changes = new RuntimeDebugChangeSet(m_Revision, m_PendingFullSync, m_PendingSources, m_PendingInstances, captureVersion);
            m_PendingFullSync = false;
        }

        void ApplyTimeline(RuntimeDebugEventView eventView, RuntimeLiveStateKey key)
        {
            RuntimeTraceEvent traceEvent = eventView.Event;
            RuntimeInstanceKey playback = traceEvent.RuntimeInstance;
            if (playback.Kind != RuntimeInstanceKind.TimelinePlayback)
                return;

            if (!m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder))
            {
                builder = new TimelinePlaybackSummaryBuilder(playback);
                m_TimelinePlayback.Add(playback, builder);
            }
            string previousTimelineAuthoringId = builder.TimelineAuthoringId;
            builder.Apply(eventView);
            if (!string.Equals(previousTimelineAuthoringId, builder.TimelineAuthoringId, StringComparison.Ordinal))
                RegisterTimelinePlayback(builder.TimelineAuthoringId, playback);

            if (!m_PlaybackEvents.TryGetValue(playback, out Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView> events))
            {
                events = new Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>();
                m_PlaybackEvents.Add(playback, events);
            }
            events[key] = eventView;
        }

        IReadOnlyList<RuntimeInstanceKey> CollectInstances(Func<RuntimeSourceElementKey, bool> predicate)
        {
            var sequences = new Dictionary<RuntimeInstanceKey, ulong>();
            foreach (KeyValuePair<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>> pair in m_Instances)
            {
                if (!predicate(pair.Key))
                    continue;
                foreach (KeyValuePair<RuntimeInstanceKey, ulong> instance in pair.Value)
                {
                    if (!sequences.TryGetValue(instance.Key, out ulong current) || instance.Value > current)
                        sequences[instance.Key] = instance.Value;
                }
            }
            var result = new List<RuntimeInstanceKey>(sequences.Keys);
            result.Sort((left, right) => sequences[right].CompareTo(sequences[left]));
            return result;
        }

        ulong GetTimelineSequence(RuntimeInstanceKey playback)
        {
            return m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder)
                ? Math.Max(builder.LatestLogicTick, builder.LatestPresentationFrame)
                : 0;
        }

        void RegisterGraphInstance(string graphAuthoringId, RuntimeInstanceKey instance)
        {
            if (string.IsNullOrEmpty(graphAuthoringId))
                return;
            if (!m_GraphInstanceMembership.TryGetValue(graphAuthoringId, out HashSet<RuntimeInstanceKey> instances))
            {
                instances = new HashSet<RuntimeInstanceKey>();
                m_GraphInstanceMembership.Add(graphAuthoringId, instances);
            }
            if (instances.Add(instance))
                m_GraphInstanceRevisions[graphAuthoringId] = GetGraphInstanceRevision(graphAuthoringId) + 1;
        }

        void RegisterTimelinePlayback(string timelineAuthoringId, RuntimeInstanceKey playback)
        {
            if (string.IsNullOrEmpty(timelineAuthoringId))
                return;
            if (!m_TimelinePlaybackMembership.TryGetValue(timelineAuthoringId, out HashSet<RuntimeInstanceKey> playbacks))
            {
                playbacks = new HashSet<RuntimeInstanceKey>();
                m_TimelinePlaybackMembership.Add(timelineAuthoringId, playbacks);
            }
            if (playbacks.Add(playback))
                m_TimelinePlaybackRevisions[timelineAuthoringId] = GetTimelinePlaybackRevision(timelineAuthoringId) + 1;
        }

        internal readonly struct ElementInstanceKey : IEquatable<ElementInstanceKey>
        {
            public ElementInstanceKey(RuntimeSourceElementKey source, RuntimeInstanceKey instance)
            {
                Source = source;
                Instance = instance;
            }

            public RuntimeSourceElementKey Source { get; }
            public RuntimeInstanceKey Instance { get; }
            public bool Equals(ElementInstanceKey other) => Source.Equals(other.Source) && Instance.Equals(other.Instance);
            public override bool Equals(object obj) => obj is ElementInstanceKey other && Equals(other);
            public override int GetHashCode() => Source.GetHashCode() * 397 ^ Instance.GetHashCode();
        }

        sealed class TimelinePlaybackSummaryBuilder
        {
            public TimelinePlaybackSummaryBuilder(RuntimeInstanceKey playback)
            {
                Playback = playback;
            }

            public RuntimeInstanceKey Playback { get; }
            public string TimelineAuthoringId { get; private set; } = string.Empty;
            public RuntimeTimelinePlaybackProvenance Provenance { get; private set; }
            public ulong LatestLogicTick { get; private set; }
            public ulong LatestPresentationFrame { get; private set; }
            public float LogicTime { get; private set; }
            public float VisualTime { get; private set; }
            public int Cycle { get; private set; }
            public RuntimeTraceEventKind Lifecycle { get; private set; }
            public string LifecycleStatus { get; private set; } = string.Empty;
            public RuntimeTraceEventKind Terminal { get; private set; }
            public string TerminalCause { get; private set; } = string.Empty;

            public void Apply(RuntimeDebugEventView eventView)
            {
                RuntimeTraceEvent traceEvent = eventView.Event;
                RuntimeTracePayload payload = traceEvent.Payload;
                if (!string.IsNullOrEmpty(eventView.Source.TimelineAuthoringId))
                    TimelineAuthoringId = eventView.Source.TimelineAuthoringId;
                if (payload.TimelinePlayback.IsValid)
                    Provenance = payload.TimelinePlayback;

                if (traceEvent.Domain == RuntimeTraceDomain.Logic && traceEvent.Position >= LatestLogicTick)
                {
                    LatestLogicTick = traceEvent.Position;
                    if (traceEvent.Kind == RuntimeTraceEventKind.TimelineLogicTime)
                    {
                        LogicTime = payload.Time;
                        Cycle = payload.Cycle;
                    }
                }
                else if (traceEvent.Domain == RuntimeTraceDomain.Presentation && traceEvent.Position >= LatestPresentationFrame)
                {
                    LatestPresentationFrame = traceEvent.Position;
                    if (traceEvent.Kind == RuntimeTraceEventKind.TimelineVisualTime)
                    {
                        VisualTime = payload.Time;
                        Cycle = payload.Cycle;
                    }
                }

                if (traceEvent.Kind is RuntimeTraceEventKind.TimelineRequested or RuntimeTraceEventKind.TimelineStarted or RuntimeTraceEventKind.TimelineLogicTime or RuntimeTraceEventKind.TimelineVisualTime or RuntimeTraceEventKind.TimelineCompleted or RuntimeTraceEventKind.TimelineCancelled or RuntimeTraceEventKind.TimelineStopped)
                {
                    Lifecycle = traceEvent.Kind;
                    LifecycleStatus = payload.Status;
                }

                if (traceEvent.Kind is RuntimeTraceEventKind.TimelineCompleted or RuntimeTraceEventKind.TimelineCancelled or RuntimeTraceEventKind.TimelineStopped)
                {
                    Terminal = traceEvent.Kind;
                    TerminalCause = payload.Cause;
                }
            }

            public RuntimeTimelinePlaybackDebugSummary Build()
            {
                return new RuntimeTimelinePlaybackDebugSummary(
                    Playback,
                    Provenance,
                    LatestLogicTick,
                    LatestPresentationFrame,
                    LogicTime,
                    VisualTime,
                    Cycle,
                    Lifecycle,
                    LifecycleStatus,
                    Terminal,
                    TerminalCause);
            }
        }
    }

    internal sealed class RuntimeDebugSourceMapSnapshot
    {
        readonly Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry> m_Entries;
        readonly Dictionary<RuntimeSourceElementKey, string[]> m_Hashes;

        RuntimeDebugSourceMapSnapshot(Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry> entries, Dictionary<RuntimeSourceElementKey, string[]> hashes)
        {
            m_Entries = entries ?? new Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry>();
            m_Hashes = hashes ?? new Dictionary<RuntimeSourceElementKey, string[]>();
        }

        public static RuntimeDebugSourceMapSnapshot Empty { get; } = new RuntimeDebugSourceMapSnapshot(null, null);

        public static RuntimeDebugSourceMapSnapshot Capture(IDebugSourceMap sourceMap)
        {
            if (sourceMap == null)
                return Empty;

            var entries = new Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry>();
            var collected = new Dictionary<RuntimeSourceElementKey, List<string>>();
            IReadOnlyList<DebugSourceMapEntry> sourceEntries = sourceMap.Entries;
            for (int i = 0; i < sourceEntries.Count; i++)
            {
                DebugSourceMapEntry entry = sourceEntries[i];
                entries[entry.Handle] = entry;
                if (!entry.Source.IsValid)
                    continue;
                if (!collected.TryGetValue(entry.Source, out List<string> hashes))
                {
                    hashes = new List<string>();
                    collected.Add(entry.Source, hashes);
                }
                hashes.Add(entry.ContentHash ?? string.Empty);
            }

            var frozen = new Dictionary<RuntimeSourceElementKey, string[]>();
            foreach (KeyValuePair<RuntimeSourceElementKey, List<string>> pair in collected)
                frozen.Add(pair.Key, pair.Value.ToArray());
            return new RuntimeDebugSourceMapSnapshot(entries, frozen);
        }

        public bool TryResolve(RuntimeSourceElementHandle handle, out RuntimeSourceElementKey source, out string sourceName)
        {
            if (m_Entries.TryGetValue(handle, out DebugSourceMapEntry entry))
            {
                source = entry.Source;
                sourceName = entry.DisplayName ?? string.Empty;
                return true;
            }
            source = default;
            sourceName = string.Empty;
            return false;
        }

        public RuntimeDebugTargetMatch Match(RuntimeDebugTargetRequest request)
        {
            if (!request.IsValid || !m_Hashes.TryGetValue(request.Source, out string[] hashes) || hashes.Length == 0)
                return RuntimeDebugTargetMatch.SourceMissing;

            for (int i = 0; i < hashes.Length; i++)
            {
                if (string.Equals(hashes[i], request.ContentHash, StringComparison.Ordinal))
                    return RuntimeDebugTargetMatch.Exact;
            }
            return RuntimeDebugTargetMatch.RevisionMismatch;
        }
    }
}
