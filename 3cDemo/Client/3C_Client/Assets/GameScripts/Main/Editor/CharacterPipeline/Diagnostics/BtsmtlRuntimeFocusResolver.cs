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
        readonly Dictionary<RuntimeInstanceKey, RuntimeDebugEventView> m_Timelines = new();
        readonly List<RuntimeDebugEventView> m_Candidates = new();

        public IReadOnlyList<RuntimeDebugEventView> Candidates => m_Candidates;

        public void Refresh(RuntimeDebugViewModel view)
        {
            m_Candidates.Clear();
            m_Nodes.Clear();
            m_Graphs.Clear();
            m_Timelines.Clear();
            if (!view.Valid || view.HasCoverageGap)
                return;
            view.CopyCurrentEvents(RuntimeTraceChannel.Graph | RuntimeTraceChannel.Timeline, m_Events);
            for (int i = 0; i < m_Events.Count; i++)
            {
                RuntimeDebugEventView item = m_Events[i];
                RuntimeInstanceKey instance = item.Event.RuntimeInstance;
                if (instance.CharacterRuntimeId != view.Target.CharacterRuntimeId || !item.Source.IsValid)
                    continue;
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
                }
            }
            foreach (RuntimeDebugEventView item in m_Nodes.Values)
            {
                RuntimeNodeExecutionObservation.TryCreate(item, out RuntimeNodeExecutionObservation observation);
                if (observation.IsTerminal)
                    continue;
                RuntimeInstanceKey instance = item.Event.RuntimeInstance;
                if (!m_Graphs.TryGetValue(instance, out RuntimeDebugEventView previous) ||
                    item.Event.Sequence > previous.Event.Sequence)
                    m_Graphs[instance] = item;
            }
            foreach (RuntimeDebugEventView item in m_Graphs.Values)
                if (!HasActiveChildGraph(view, item.Event.RuntimeInstance) &&
                    !HasActiveTimeline(view, item.Event.RuntimeInstance))
                    m_Candidates.Add(item);
            foreach (RuntimeDebugEventView item in m_Timelines.Values)
                if (IsActiveTimeline(view, item) && !HasActiveTimelineGraph(view, item))
                    m_Candidates.Add(item);
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
            view.TryGetInvocation(child.CallSiteId, out RuntimeGraphInvocation invocation) &&
            string.Equals(invocation.ParentPath, parent.CallSiteId, StringComparison.Ordinal);

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
            RuntimeTimelinePlaybackProvenance provenance = timeline.Event.Payload.TimelinePlayback;
            foreach (RuntimeInstanceKey graph in m_Graphs.Keys)
            {
                if (graph.CharacterRuntimeId != timeline.Event.RuntimeInstance.CharacterRuntimeId ||
                    graph.ActionInstanceId != timeline.Event.RuntimeInstance.ActionInstanceId ||
                    graph.GraphRuntimeId != provenance.SourceGraphRuntimeId ||
                    graph.ActivationGeneration != provenance.SkillExecutionGeneration ||
                    !view.TryGetParentGeneration(graph, out ulong parent) || parent != provenance.SourceActivationGeneration ||
                    !view.TryGetInvocation(graph.CallSiteId, out RuntimeGraphInvocation invocation))
                    continue;
                if (!string.IsNullOrEmpty(invocation.CallerClipId) &&
                    string.Equals(invocation.ParentPath, provenance.SourceInvocationPath, StringComparison.Ordinal) &&
                    string.Equals(invocation.Caller.ElementAuthoringId, provenance.SourceNodeAuthoringId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static bool BelongsToGraph(RuntimeInstanceKey playback, RuntimeTimelinePlaybackProvenance provenance, RuntimeInstanceKey graph) =>
            playback.CharacterRuntimeId == graph.CharacterRuntimeId &&
            playback.ActionInstanceId == graph.ActionInstanceId &&
            provenance.SourceGraphRuntimeId == graph.GraphRuntimeId &&
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
}
