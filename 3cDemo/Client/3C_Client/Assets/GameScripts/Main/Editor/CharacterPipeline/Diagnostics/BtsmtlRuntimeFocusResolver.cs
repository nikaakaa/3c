using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    sealed class BtsmtlRuntimeFocusResolver
    {
        readonly List<RuntimeDebugEventView> m_Events = new();
        readonly Dictionary<(RuntimeInstanceKey, RuntimeSourceElementKey), RuntimeDebugEventView> m_Nodes = new();
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> m_Graphs = new();
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> m_CompletedGraphs = new();
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> m_Timelines = new();
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> m_CompletedTimelines = new();
        readonly Dictionary<RuntimeInstanceKey, ulong> m_DestroyedGraphs = new();
        readonly List<RuntimeDebugEventView> m_Candidates = new();

        public IReadOnlyList<RuntimeDebugEventView> Candidates => m_Candidates;
        public int ActiveCandidateCount { get; private set; }

        public void Refresh(RuntimeDebugViewModel view)
        {
            m_Candidates.Clear();
            m_Nodes.Clear();
            m_Graphs.Clear();
            m_CompletedGraphs.Clear();
            m_Timelines.Clear();
            m_CompletedTimelines.Clear();
            m_DestroyedGraphs.Clear();
            ActiveCandidateCount = 0;
            if (!view.Valid || view.HasCoverageGap)
                return;
            view.CopyCurrentEvents(RuntimeTraceChannel.Graph | RuntimeTraceChannel.Timeline, m_Events);
            for (int i = 0; i < m_Events.Count; i++)
            {
                RuntimeDebugEventView item = m_Events[i];
                RuntimeInstanceKey instance = item.Event.RuntimeInstance;
                if (instance.CharacterRuntimeId != view.Target.CharacterRuntimeId || !item.Source.IsValid)
                    continue;
                if (instance.Kind == RuntimeInstanceKind.SkillExecution && item.Event.Kind == RuntimeTraceEventKind.GraphDestroyed)
                {
                    if (!m_DestroyedGraphs.TryGetValue(instance, out ulong sequence) || item.Event.Sequence > sequence)
                        m_DestroyedGraphs[instance] = item.Event.Sequence;
                }
                if (instance.Kind == RuntimeInstanceKind.SkillExecution &&
                    item.Source.Kind == RuntimeSourceElementKind.Node &&
                    RuntimeNodeExecutionObservation.TryCreate(item, out _))
                {
                    var key = (instance, item.Source);
                    if (!m_Nodes.TryGetValue(key, out RuntimeDebugEventView previous) ||
                        item.Event.Sequence > previous.Event.Sequence)
                        m_Nodes[key] = item;
                }
                else if (instance.Kind == RuntimeInstanceKind.TimelinePlayback &&
                         item.Event.Payload.TimelinePlayback.IsValid &&
                         !string.IsNullOrEmpty(item.Source.TimelineAuthoringId))
                {
                    if (!m_Timelines.TryGetValue(instance, out RuntimeDebugEventView previous) ||
                        item.Event.Sequence > previous.Event.Sequence)
                        m_Timelines[instance] = item;
                    if (item.Event.Kind is RuntimeTraceEventKind.TimelineCompleted or RuntimeTraceEventKind.TimelineCancelled or
                        RuntimeTraceEventKind.TimelineStopped)
                        if (!m_CompletedTimelines.TryGetValue(instance, out RuntimeDebugEventView terminal) ||
                            item.Event.Sequence > terminal.Event.Sequence)
                            m_CompletedTimelines[instance] = item;
                }
            }
            foreach (RuntimeDebugEventView item in m_Nodes.Values)
            {
                RuntimeNodeExecutionObservation.TryCreate(item, out RuntimeNodeExecutionObservation observation);
                RuntimeInstanceKey instance = item.Event.RuntimeInstance;
                if (m_DestroyedGraphs.TryGetValue(instance, out ulong destroyed) && destroyed >= item.Event.Sequence)
                    continue;
                Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> destination = observation.IsTerminal
                    ? m_CompletedGraphs
                    : m_Graphs;
                if (!destination.TryGetValue(instance, out RuntimeDebugEventView previous) ||
                    item.Event.Sequence > previous.Event.Sequence)
                    destination[instance] = item;
            }
            foreach (RuntimeDebugEventView item in m_Graphs.Values)
                if (!HasActiveChildGraph(view, item.Event.RuntimeInstance) &&
                    !HasActiveTimeline(view, item.Event.RuntimeInstance))
                    m_Candidates.Add(item);
            foreach (RuntimeDebugEventView item in m_Timelines.Values)
                if (IsActiveTimeline(view, item) && !HasActiveTimelineGraph(view, item))
                    m_Candidates.Add(item);
            ActiveCandidateCount = m_Candidates.Count;
            if (m_Candidates.Count == 0)
                AddLatestCompleted(view);
        }

        void AddLatestCompleted(RuntimeDebugViewModel view)
        {
            RuntimeDebugEventView latest = default;
            foreach (RuntimeDebugEventView item in m_CompletedGraphs.Values)
                if (!m_Graphs.ContainsKey(item.Event.RuntimeInstance) && item.Event.Sequence > latest.Event.Sequence)
                    latest = item;
            foreach (RuntimeDebugEventView item in m_CompletedTimelines.Values)
                if (item.Event.Sequence > latest.Event.Sequence)
                    latest = item;
            if (latest.Event.Sequence == 0)
                return;
            foreach (RuntimeDebugEventView item in m_CompletedGraphs.Values)
                if (!m_Graphs.ContainsKey(item.Event.RuntimeInstance) && AtCompletionPosition(item, latest) &&
                    !HasReturnedParent(view, item, latest))
                    m_Candidates.Add(item);
            foreach (RuntimeDebugEventView item in m_CompletedTimelines.Values)
                if (AtCompletionPosition(item, latest) && !HasReturnedParent(view, item, latest))
                    m_Candidates.Add(item);
        }

        static bool AtCompletionPosition(RuntimeDebugEventView item, RuntimeDebugEventView latest) =>
            item.Event.Domain == latest.Event.Domain && item.Event.Position == latest.Event.Position;

        bool HasReturnedParent(RuntimeDebugViewModel view, RuntimeDebugEventView child, RuntimeDebugEventView latest)
        {
            foreach (RuntimeDebugEventView parent in m_CompletedGraphs.Values)
            {
                if (m_Graphs.ContainsKey(parent.Event.RuntimeInstance) || !AtCompletionPosition(parent, latest) ||
                    parent.Event.Sequence <= child.Event.Sequence)
                    continue;
                if (child.Event.RuntimeInstance.Kind == RuntimeInstanceKind.SkillExecution
                    ? IsChild(view, child.Event.RuntimeInstance, parent.Event.RuntimeInstance)
                    : BelongsToGraph(child.Event.RuntimeInstance, child.Event.Payload.TimelinePlayback, parent.Event.RuntimeInstance))
                    return true;
            }
            if (child.Event.RuntimeInstance.Kind == RuntimeInstanceKind.SkillExecution)
                foreach (RuntimeDebugEventView parent in m_CompletedTimelines.Values)
                    if (AtCompletionPosition(parent, latest) && parent.Event.Sequence > child.Event.Sequence &&
                        IsTimelineChildGraph(view, child.Event.RuntimeInstance, parent))
                        return true;
            return false;
        }

        bool HasActiveChildGraph(RuntimeDebugViewModel view, RuntimeInstanceKey parent)
        {
            foreach (RuntimeInstanceKey child in m_Graphs.Keys)
                if (IsChild(view, child, parent))
                    return true;
            return false;
        }

        static bool IsChild(RuntimeDebugViewModel view, RuntimeInstanceKey child, RuntimeInstanceKey parent) =>
            child.CharacterRuntimeId == parent.CharacterRuntimeId &&
            child.GraphRuntimeId == parent.GraphRuntimeId &&
            child.ActionInstanceId == parent.ActionInstanceId &&
            child.ActivationGeneration == parent.ActivationGeneration &&
            view.TryGetParentGeneration(child, out ulong generation) && generation == parent.InvocationGeneration &&
            view.TryGetInvocation(child, child.CallSiteId, out RuntimeGraphInvocation invocation) &&
            BtsmtlRuntimeInvocationPath.MatchesParent(view, child, invocation.ParentPath, parent.CallSiteId);

        bool HasActiveTimeline(RuntimeDebugViewModel view, RuntimeInstanceKey graph)
        {
            foreach (RuntimeDebugEventView item in m_Timelines.Values)
            {
                RuntimeTimelinePlaybackProvenance provenance = item.Event.Payload.TimelinePlayback;
                if (BelongsToGraph(item.Event.RuntimeInstance, provenance, graph) && IsActiveTimeline(view, item))
                    return true;
            }
            return false;
        }

        bool HasActiveTimelineGraph(RuntimeDebugViewModel view, RuntimeDebugEventView timeline)
        {
            foreach (RuntimeInstanceKey graph in m_Graphs.Keys)
                if (IsTimelineChildGraph(view, graph, timeline))
                    return true;
            return false;
        }

        static bool IsTimelineChildGraph(RuntimeDebugViewModel view, RuntimeInstanceKey graph, RuntimeDebugEventView timeline)
        {
            RuntimeTimelinePlaybackProvenance provenance = timeline.Event.Payload.TimelinePlayback;
            return graph.CharacterRuntimeId == timeline.Event.RuntimeInstance.CharacterRuntimeId &&
                   (graph.TimelinePlaybackId == 0 || graph.TimelinePlaybackId == timeline.Event.RuntimeInstance.TimelinePlaybackId) &&
                   graph.ActionInstanceId == timeline.Event.RuntimeInstance.ActionInstanceId &&
                   (provenance.SourceGraphRuntimeId == Guid.Empty || graph.GraphRuntimeId == provenance.SourceGraphRuntimeId) &&
                   graph.ActivationGeneration == provenance.SkillExecutionGeneration &&
                   view.TryGetParentGeneration(graph, out ulong parent) && parent == provenance.SourceActivationGeneration &&
                   view.TryGetInvocation(graph, graph.CallSiteId, out RuntimeGraphInvocation invocation) &&
                   !string.IsNullOrEmpty(invocation.CallerClipId) &&
                   BtsmtlRuntimeInvocationPath.MatchesParent(view, graph, invocation.ParentPath, provenance.SourceInvocationPath) &&
                   string.Equals(invocation.Caller.ElementAuthoringId, provenance.SourceNodeAuthoringId, StringComparison.Ordinal);
        }

        static bool BelongsToGraph(RuntimeInstanceKey playback, RuntimeTimelinePlaybackProvenance provenance, RuntimeInstanceKey graph) =>
            playback.CharacterRuntimeId == graph.CharacterRuntimeId &&
            playback.ActionInstanceId == graph.ActionInstanceId &&
            (provenance.SourceGraphRuntimeId == Guid.Empty ||
             provenance.SourceGraphRuntimeId == graph.GraphRuntimeId) &&
            provenance.SkillExecutionGeneration == graph.ActivationGeneration &&
            provenance.SourceActivationGeneration == graph.InvocationGeneration &&
            string.Equals(provenance.SourceInvocationPath, graph.CallSiteId, StringComparison.Ordinal);

        static bool IsActiveTimeline(RuntimeDebugViewModel view, RuntimeDebugEventView item) =>
            view.TryGetTimelinePlaybackSummary(
                item.Source.TimelineAuthoringId,
                item.Event.RuntimeInstance,
                out RuntimeTimelinePlaybackDebugSummary summary,
                item.Event.Payload.TimelinePlayback.SourceGraphAuthoringId) && !summary.IsTerminal;
    }

    static class BtsmtlRuntimeInvocationPath
    {
        public static bool MatchesParent(RuntimeDebugViewModel view, RuntimeInstanceKey instance,
            string declaredPath, string actualPath)
        {
            if (string.Equals(declaredPath, actualPath, StringComparison.Ordinal))
                return true;
            return !string.IsNullOrEmpty(declaredPath) &&
                   actualPath != null && actualPath.Length > declaredPath.Length &&
                   actualPath[declaredPath.Length] == '/' &&
                   actualPath.StartsWith(declaredPath, StringComparison.Ordinal) &&
                   view.TryGetInvocation(instance, actualPath, out RuntimeGraphInvocation parent) &&
                   !string.IsNullOrEmpty(parent.CallerClipId);
        }
    }
}
