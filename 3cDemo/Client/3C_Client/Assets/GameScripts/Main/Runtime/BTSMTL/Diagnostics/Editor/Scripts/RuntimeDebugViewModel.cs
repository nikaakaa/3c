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
            RuntimeEpoch = target?.RuntimeEpoch ?? 0;
        }

        public string DisplayName { get; }
        public int HostInstanceId { get; }
        public Guid CharacterRuntimeId { get; }
        public Guid SessionId { get; }
        public RuntimeContentRevision Revision { get; }
        public ulong RuntimeEpoch { get; }
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
        readonly Dictionary<RuntimeSourceElementKey, RuntimeNodeExecutionObservation> m_LatestGraphExecution = new();
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugSourceMapSnapshot> m_InstanceSourceMaps = new();
        readonly Dictionary<RuntimeInstanceKey, (ulong Parent, ulong Sequence)> m_InvocationParents = new();
        readonly Dictionary<ElementInstanceKey, RuntimeElementDebugState> m_ElementStates = new Dictionary<ElementInstanceKey, RuntimeElementDebugState>();
        readonly Dictionary<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>> m_Instances = new Dictionary<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>>();
        readonly Dictionary<string, HashSet<RuntimeInstanceKey>> m_GraphInstanceMembership = new Dictionary<string, HashSet<RuntimeInstanceKey>>(StringComparer.Ordinal);
        readonly Dictionary<string, long> m_GraphInstanceRevisions = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<RuntimeInstanceKey, ulong> m_GraphInstanceSequences = new Dictionary<RuntimeInstanceKey, ulong>();
        readonly InstanceSequenceOrder m_InstanceSequenceOrder;
        readonly Dictionary<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder> m_TimelinePlayback = new Dictionary<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder>();
        readonly Dictionary<RuntimeInstanceKey, Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>> m_PlaybackEvents = new Dictionary<RuntimeInstanceKey, Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>>();
        readonly Dictionary<TimelineSourceKey, HashSet<RuntimeInstanceKey>> m_TimelinePlaybackMembership = new Dictionary<TimelineSourceKey, HashSet<RuntimeInstanceKey>>();
        readonly Dictionary<TimelineSourceKey, long> m_TimelinePlaybackRevisions = new Dictionary<TimelineSourceKey, long>();
        readonly HashSet<RuntimeSourceElementKey> m_PendingSources = new HashSet<RuntimeSourceElementKey>();
        readonly HashSet<RuntimeInstanceKey> m_PendingInstances = new HashSet<RuntimeInstanceKey>();
        RuntimeDebugChangeSet m_Changes = RuntimeDebugChangeSet.Empty;
        RuntimeTraceChannel m_Channels;
        string m_Error = string.Empty;
        bool m_PendingFullSync;
        ulong m_LatestLogicTick;
        ulong m_LatestPresentationFrame;
        long m_Revision;
        bool m_HasCoverageGap;
        long m_EvictedStates;

        internal RuntimeDebugViewModel(RuntimeDebugTargetInfo target, RuntimeDebugSourceMapSnapshot sourceMap, RuntimeTraceChannel channels)
        {
            Target = target;
            m_SourceMap = sourceMap ?? RuntimeDebugSourceMapSnapshot.Empty;
            m_Channels = channels;
            m_InstanceSequenceOrder = new InstanceSequenceOrder(this);
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
        public bool HasCoverageGap => m_HasCoverageGap;
        public long EvictedStates => m_EvictedStates;

        public RuntimeExecutionTimeline BuildExecutionTimeline(
            RuntimeCaptureSnapshot capture,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default) =>
            RuntimeExecutionTimelineBuilder.Build(capture, m_SourceMap, historyOffset, instance);

        public RuntimeExecutionHistory BuildExecutionHistory(
            RuntimeCaptureSnapshot capture,
            int historyOffset = 0,
            RuntimeInstanceKey instance = default) =>
            RuntimeExecutionTimelineBuilder.BuildHistory(capture, m_SourceMap, null, historyOffset, instance);

        internal RuntimeExecutionHistory BuildExecutionHistory(
            RuntimeCaptureSnapshot capture,
            int historyOffset,
            RuntimeInstanceKey instance,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps) =>
            RuntimeExecutionTimelineBuilder.BuildHistory(capture, m_SourceMap, sourceMaps, historyOffset, instance);

        internal void SetCoverage(long evictedStates, bool missedChanges)
        {
            m_EvictedStates = evictedStates;
            m_HasCoverageGap |= missedChanges || evictedStates != 0;
        }
        public RuntimeDebugChangeSet Changes => m_Changes;
        public IReadOnlyList<RuntimeGraphInvocation> GetGraphInvocations(RuntimeInstanceKey instance) =>
            m_InstanceSourceMaps.TryGetValue(instance, out RuntimeDebugSourceMapSnapshot sourceMap)
                ? sourceMap.GraphInvocations
                : Array.Empty<RuntimeGraphInvocation>();

        public bool TryGetInvocation(RuntimeInstanceKey instance, string path, out RuntimeGraphInvocation invocation)
        {
            invocation = default;
            return m_InstanceSourceMaps.TryGetValue(instance, out RuntimeDebugSourceMapSnapshot sourceMap) &&
                   sourceMap.TryGetInvocation(path, out invocation);
        }
        public bool TryGetParentGeneration(RuntimeInstanceKey instance, out ulong generation)
        {
            bool found = m_InvocationParents.TryGetValue(instance, out var value);
            generation = value.Parent;
            return found;
        }
        public ulong InvocationSequence(RuntimeInstanceKey instance) => m_InvocationParents.TryGetValue(instance, out var value) ? value.Sequence : 0;

        public RuntimeDebugTargetMatch MatchSource(RuntimeDebugTargetRequest request)
        {
            return m_SourceMap.Match(request);
        }

        public void CopyGraphInstances(string graphAuthoringId, List<RuntimeInstanceKey> destination)
        {
            destination.Clear();
            m_GraphInstanceSequences.Clear();
            foreach (KeyValuePair<RuntimeSourceElementKey, Dictionary<RuntimeInstanceKey, ulong>> source in m_Instances)
            {
                if (!string.Equals(source.Key.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal))
                    continue;

                foreach (KeyValuePair<RuntimeInstanceKey, ulong> instance in source.Value)
                {
                    if (!m_GraphInstanceSequences.TryGetValue(instance.Key, out ulong current) || instance.Value > current)
                        m_GraphInstanceSequences[instance.Key] = instance.Value;
                }
            }

            foreach (KeyValuePair<RuntimeInstanceKey, ulong> instance in m_GraphInstanceSequences)
                destination.Add(instance.Key);
            destination.Sort(m_InstanceSequenceOrder);
        }

        public long GetGraphInstanceRevision(string graphAuthoringId)
        {
            return !string.IsNullOrEmpty(graphAuthoringId) && m_GraphInstanceRevisions.TryGetValue(graphAuthoringId, out long revision)
                ? revision
                : 0;
        }

        public IReadOnlyList<RuntimeInstanceKey> GetTimelineInstances(
            string timelineAuthoringId,
            string graphAuthoringId = "")
        {
            var result = new List<RuntimeInstanceKey>();
            foreach (KeyValuePair<RuntimeInstanceKey, TimelinePlaybackSummaryBuilder> pair in m_TimelinePlayback)
            {
                if (MatchesTimeline(
                        pair.Value,
                        timelineAuthoringId,
                        graphAuthoringId))
                    result.Add(pair.Key);
            }
            result.Sort((left, right) => GetTimelineSequence(right).CompareTo(GetTimelineSequence(left)));
            return result;
        }

        public long GetTimelinePlaybackRevision(
            string timelineAuthoringId,
            string graphAuthoringId = "")
        {
            TimelineSourceKey key = new TimelineSourceKey(timelineAuthoringId, graphAuthoringId);
            return !string.IsNullOrEmpty(timelineAuthoringId) && m_TimelinePlaybackRevisions.TryGetValue(key, out long revision)
                ? revision
                : 0;
        }

        public IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> GetTimelinePlaybackSummaries(
            string timelineAuthoringId,
            string graphAuthoringId = "")
        {
            var result = new List<RuntimeTimelinePlaybackDebugSummary>();
            foreach (TimelinePlaybackSummaryBuilder builder in m_TimelinePlayback.Values)
            {
                if (MatchesTimeline(builder, timelineAuthoringId, graphAuthoringId))
                    result.Add(builder.Build());
            }
            result.Sort((left, right) => right.LatestLogicTick != left.LatestLogicTick
                ? right.LatestLogicTick.CompareTo(left.LatestLogicTick)
                : right.LatestPresentationFrame.CompareTo(left.LatestPresentationFrame));
            return result;
        }

        public bool TryGetTimelinePlaybackSummary(
            string timelineAuthoringId,
            RuntimeInstanceKey playback,
            out RuntimeTimelinePlaybackDebugSummary summary,
            string graphAuthoringId = "")
        {
            if (m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder) &&
                MatchesTimeline(builder, timelineAuthoringId, graphAuthoringId))
            {
                summary = builder.Build();
                return true;
            }

            summary = default;
            return false;
        }

        public IReadOnlyList<RuntimeDebugEventView> GetTimelineCurrentEvents(
            string timelineAuthoringId,
            RuntimeInstanceKey playback,
            string graphAuthoringId = "")
        {
            if (!m_PlaybackEvents.TryGetValue(playback, out Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView> events))
                return Array.Empty<RuntimeDebugEventView>();
            if (!m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder) ||
                !MatchesTimeline(builder, timelineAuthoringId, graphAuthoringId))
                return Array.Empty<RuntimeDebugEventView>();

            var result = new List<RuntimeDebugEventView>(events.Values);
            result.Sort((left, right) => right.Event.Sequence.CompareTo(left.Event.Sequence));
            return result;
        }

        static bool MatchesTimeline(
            TimelinePlaybackSummaryBuilder builder,
            string timelineAuthoringId,
            string graphAuthoringId)
        {
            return string.Equals(
                       builder.TimelineAuthoringId,
                       timelineAuthoringId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       builder.Provenance.SourceGraphAuthoringId ?? string.Empty,
                       graphAuthoringId ?? string.Empty,
                       StringComparison.Ordinal);
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

        public void CopyCurrentEvents(RuntimeTraceChannel channels, List<RuntimeDebugEventView> destination)
        {
            destination.Clear();
            foreach (RuntimeDebugEventView eventView in m_CurrentEvents.Values)
                if ((eventView.Event.Channel & channels) != 0)
                    destination.Add(eventView);
        }

        public void CopyGraphStates(
            string graphAuthoringId,
            RuntimeInstanceKey instance,
            bool changedOnly,
            List<RuntimeElementDebugState> destination)
        {
            destination.Clear();
            foreach (KeyValuePair<ElementInstanceKey, RuntimeElementDebugState> pair in m_ElementStates)
            {
                if (!string.Equals(pair.Key.Source.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal) ||
                    !pair.Key.Instance.Equals(instance))
                    continue;
                if (changedOnly && !m_Changes.AffectsSource(pair.Key.Source))
                    continue;
                destination.Add(pair.Value);
            }
        }

        public void CopyGraphExecutionStates(
            string graphAuthoringId,
            RuntimeInstanceKey instance,
            List<RuntimeNodeExecutionObservation> destination)
        {
            destination.Clear();
            m_LatestGraphExecution.Clear();
            if (!instance.IsValid)
                return;
            foreach (RuntimeDebugEventView item in m_CurrentEvents.Values)
            {
                if (item.Source.Kind != RuntimeSourceElementKind.Node ||
                    !string.Equals(item.Source.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal) ||
                    !item.Event.RuntimeInstance.Equals(instance) ||
                    !RuntimeNodeExecutionObservation.TryCreate(item, out RuntimeNodeExecutionObservation observation))
                    continue;
                if (!m_LatestGraphExecution.TryGetValue(item.Source, out RuntimeNodeExecutionObservation previous) ||
                    item.Event.Position > previous.Event.Event.Position ||
                    item.Event.Position == previous.Event.Event.Position && item.Event.Sequence > previous.Event.Event.Sequence)
                    m_LatestGraphExecution[item.Source] = observation;
            }
            foreach (RuntimeNodeExecutionObservation observation in m_LatestGraphExecution.Values)
                destination.Add(observation);
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
            m_InstanceSourceMaps.Clear();
            m_InvocationParents.Clear();
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
            Apply(key, traceEvent, null);
        }

        internal void Apply(
            RuntimeLiveStateKey key,
            RuntimeTraceEvent traceEvent,
            RuntimeDebugSourceMapSnapshot sourceMapOverride)
        {
            bool historical = sourceMapOverride != null;
            if (!historical && !traceEvent.ContentRevision.Equals(Target.Revision))
            {
                m_Error = $"Trace revision mismatch: {traceEvent.ContentRevision} != {Target.Revision}";
                return;
            }

            RuntimeDebugSourceMapSnapshot sourceMap = sourceMapOverride ?? m_SourceMap;
            RuntimeSourceElementKey source = default;
            string sourceName = string.Empty;
            if (traceEvent.Source.IsValid)
            {
                if (!sourceMap.TryResolve(traceEvent.Source, out source, out sourceName))
                {
                    if (historical)
                    {
                        sourceName = "unmapped";
                    }
                    else
                    {
                        m_Error = $"Trace source handle is absent from Source Map: {traceEvent.Source}";
                        return;
                    }
                }
            }

            if (traceEvent.Domain == RuntimeTraceDomain.Logic)
                m_LatestLogicTick = Math.Max(m_LatestLogicTick, traceEvent.Position);
            else if (traceEvent.Domain == RuntimeTraceDomain.Presentation)
                m_LatestPresentationFrame = Math.Max(m_LatestPresentationFrame, traceEvent.Position);

            var eventView = new RuntimeDebugEventView(traceEvent, source, sourceName);
            m_CurrentEvents[key] = eventView;
            if (traceEvent.RuntimeInstance.IsValid)
                m_InstanceSourceMaps[traceEvent.RuntimeInstance] = sourceMap;
            if (traceEvent.RuntimeInstance.Kind == RuntimeInstanceKind.SkillExecution)
                if (!m_InvocationParents.TryGetValue(traceEvent.RuntimeInstance, out var previous) || traceEvent.Sequence > previous.Sequence)
                    m_InvocationParents[traceEvent.RuntimeInstance] = (traceEvent.Payload.ParentInvocationGeneration, traceEvent.Sequence);
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
            if (playback.Kind == RuntimeInstanceKind.TreeClip)
            {
                playback = RuntimeInstanceKey.Timeline(
                    playback.CharacterRuntimeId,
                    playback.SourceOperationIndex,
                    playback.TimelinePlaybackId,
                    playback.ActionInstanceId);
            }
            if (playback.Kind != RuntimeInstanceKind.TimelinePlayback)
                return;

            if (!m_TimelinePlayback.TryGetValue(playback, out TimelinePlaybackSummaryBuilder builder))
            {
                builder = new TimelinePlaybackSummaryBuilder(playback);
                m_TimelinePlayback.Add(playback, builder);
            }
            string previousTimelineAuthoringId = builder.TimelineAuthoringId;
            string previousGraphAuthoringId = builder.Provenance.SourceGraphAuthoringId ?? string.Empty;
            builder.Apply(eventView);
            string graphAuthoringId = builder.Provenance.SourceGraphAuthoringId ?? string.Empty;
            if (!string.Equals(previousTimelineAuthoringId, builder.TimelineAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(previousGraphAuthoringId, graphAuthoringId, StringComparison.Ordinal))
                RegisterTimelinePlayback(builder.TimelineAuthoringId, graphAuthoringId, playback);

            if (!m_PlaybackEvents.TryGetValue(playback, out Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView> events))
            {
                events = new Dictionary<RuntimeLiveStateKey, RuntimeDebugEventView>();
                m_PlaybackEvents.Add(playback, events);
            }
            events[key] = eventView;
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

        sealed class InstanceSequenceOrder : IComparer<RuntimeInstanceKey>
        {
            readonly RuntimeDebugViewModel m_Owner;

            internal InstanceSequenceOrder(RuntimeDebugViewModel owner) => m_Owner = owner;

            public int Compare(RuntimeInstanceKey left, RuntimeInstanceKey right) =>
                m_Owner.m_GraphInstanceSequences[right].CompareTo(m_Owner.m_GraphInstanceSequences[left]);
        }

        void RegisterTimelinePlayback(
            string timelineAuthoringId,
            string graphAuthoringId,
            RuntimeInstanceKey playback)
        {
            if (string.IsNullOrEmpty(timelineAuthoringId))
                return;
            var key = new TimelineSourceKey(timelineAuthoringId, graphAuthoringId);
            if (!m_TimelinePlaybackMembership.TryGetValue(key, out HashSet<RuntimeInstanceKey> playbacks))
            {
                playbacks = new HashSet<RuntimeInstanceKey>();
                m_TimelinePlaybackMembership.Add(key, playbacks);
            }
            if (playbacks.Add(playback))
                m_TimelinePlaybackRevisions[key] = GetTimelinePlaybackRevision(timelineAuthoringId, graphAuthoringId) + 1;
        }

        internal readonly struct TimelineSourceKey : IEquatable<TimelineSourceKey>
        {
            public TimelineSourceKey(string timelineAuthoringId, string graphAuthoringId)
            {
                TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
                GraphAuthoringId = graphAuthoringId ?? string.Empty;
            }

            public string TimelineAuthoringId { get; }
            public string GraphAuthoringId { get; }
            public bool Equals(TimelineSourceKey other) =>
                string.Equals(TimelineAuthoringId, other.TimelineAuthoringId, StringComparison.Ordinal) &&
                string.Equals(GraphAuthoringId, other.GraphAuthoringId, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is TimelineSourceKey other && Equals(other);
            public override int GetHashCode() =>
                (TimelineAuthoringId?.GetHashCode() ?? 0) * 397 ^ (GraphAuthoringId?.GetHashCode() ?? 0);
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
        readonly Dictionary<string, RuntimeGraphInvocation> m_Invocations = new(StringComparer.Ordinal);
        readonly IReadOnlyList<RuntimeGraphInvocation> m_GraphInvocations;

        RuntimeDebugSourceMapSnapshot(
            RuntimeContentRevision revision,
            Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry> entries,
            Dictionary<RuntimeSourceElementKey, string[]> hashes,
            IReadOnlyList<RuntimeGraphInvocation> invocations = null)
        {
            Revision = revision;
            m_Entries = entries ?? new Dictionary<RuntimeSourceElementHandle, DebugSourceMapEntry>();
            m_Hashes = hashes ?? new Dictionary<RuntimeSourceElementKey, string[]>();
            m_GraphInvocations = invocations ?? Array.Empty<RuntimeGraphInvocation>();
            foreach (RuntimeGraphInvocation invocation in m_GraphInvocations)
                m_Invocations.Add(invocation.Path, invocation);
        }

        public RuntimeContentRevision Revision { get; }
        public IReadOnlyList<RuntimeGraphInvocation> GraphInvocations => m_GraphInvocations;
        public bool TryGetInvocation(string path, out RuntimeGraphInvocation invocation) => m_Invocations.TryGetValue(path, out invocation);

        public static RuntimeDebugSourceMapSnapshot Empty { get; } = new RuntimeDebugSourceMapSnapshot(default, null, null);

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
            return new RuntimeDebugSourceMapSnapshot(
                sourceMap.Revision,
                entries,
                frozen,
                new List<RuntimeGraphInvocation>(sourceMap.GraphInvocations).AsReadOnly());
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

        public bool TryGet(RuntimeSourceElementHandle handle, out DebugSourceMapEntry entry) =>
            m_Entries.TryGetValue(handle, out entry);

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
