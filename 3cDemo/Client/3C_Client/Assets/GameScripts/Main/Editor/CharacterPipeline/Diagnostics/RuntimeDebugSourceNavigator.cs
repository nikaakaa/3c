using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class RuntimeDebugSourceNavigator
    {
        public static bool Open(RuntimeDebugEventView eventView, bool pin = false)
        {
            RuntimeTraceEvent trace = eventView.Event;
            RuntimeInstanceKey instance = trace.RuntimeInstance;
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            bool historical = session.AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended;
            if (!instance.IsValid || !eventView.Source.IsValid)
                return false;
            RuntimeDiagnosticsTarget target = null;
            RuntimeDebugTargetInfo targetInfo = session.ViewModel.Target;
            if (historical)
            {
                if (!session.TryResolveHistoricalSource(
                        trace.ContentRevision,
                        trace.Source,
                        out RuntimeSourceElementKey historicalSource,
                        out DebugSourceMapEntry historicalEntry) ||
                    !historicalSource.Equals(eventView.Source) ||
                    !historicalEntry.Source.Equals(eventView.Source))
                    return false;
            }
            else
            {
                if (!RuntimeDiagnosticsTargetRegistry.TryGet(instance.CharacterRuntimeId, out target) ||
                    target.SessionId != trace.SessionId || !target.Revision.Equals(trace.ContentRevision) ||
                    !target.SourceMap.TryGet(trace.Source, out DebugSourceMapEntry entry) ||
                    !entry.Source.Equals(eventView.Source))
                    return false;
                targetInfo = new RuntimeDebugTargetInfo(target);
            }
            if (targetInfo.CharacterRuntimeId != instance.CharacterRuntimeId)
                return false;
            CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(EditorUtility.InstanceIDToObject(targetInfo.HostInstanceId));
            if (!definition)
                return false;
            if (!historical &&
                !session.AttachToTarget(instance.CharacterRuntimeId))
                return false;
            if (eventView.Source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or
                RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
            {
                return OpenTimelineSource(definition, eventView.Source, instance, trace.Payload.TimelinePlayback, pin);
            }
            return Open(definition, eventView.Source, instance, default, string.Empty, pin);
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
            => Open(definition, source, instance, default, string.Empty, false);

        static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, string expectedTimelineId, bool pin)
        {
            if (!definition || !source.IsValid)
                return false;

            if (!string.IsNullOrEmpty(source.GraphAuthoringId))
            {
                IReadOnlyList<BtsmtlSkillFlowGraph> roots = definition.AbilityGraphs;
                FlowGraph[] nativeGraphs = roots
                    .Where(graph => graph != null)
                    .SelectMany(graph => BtsmtlSkillGraphClosure.Validate(graph, false))
                    .Distinct()
                    .Where(graph => ((IBtsmtlSkillFlowGraph)graph).AuthoringId == source.GraphAuthoringId)
                    .ToArray();
                if (nativeGraphs.Length != 0)
                    return nativeGraphs.Length == 1 && OpenSkillGraph(
                        definition, nativeGraphs[0], source, instance, playback, expectedTimelineId, pin);
            }
            return false;
        }

        static bool OpenTimelineSource(
            CharacterPipelineDefinition definition,
            RuntimeSourceElementKey source,
            RuntimeInstanceKey instance,
            RuntimeTimelinePlaybackProvenance provenance,
            bool pin)
        {
            if (!provenance.IsValid || string.IsNullOrEmpty(source.TimelineAuthoringId) ||
                string.IsNullOrEmpty(provenance.SourceGraphAuthoringId) ||
                string.IsNullOrEmpty(provenance.SourceNodeAuthoringId))
            {
                return false;
            }
            if (!TryResolveTimelineGraphInstance(
                    RuntimeDebugSession.Shared.ViewModel,
                    instance,
                    provenance,
                    new List<RuntimeInstanceKey>(),
                    out RuntimeInstanceKey graphInstance))
                return false;
            return Open(
                definition,
                RuntimeSourceElementKey.Node(
                    provenance.SourceGraphAuthoringId,
                    provenance.SourceNodeAuthoringId),
                graphInstance,
                instance,
                source.TimelineAuthoringId,
                pin);
        }

        internal static bool TryResolveTimelineGraphInstance(
            RuntimeDebugViewModel view,
            RuntimeInstanceKey playback,
            RuntimeTimelinePlaybackProvenance provenance,
            List<RuntimeInstanceKey> instances,
            out RuntimeInstanceKey graphInstance)
        {
            graphInstance = default;
            view.CopyGraphInstances(provenance.SourceGraphAuthoringId, instances);
            for (int i = 0; i < instances.Count; i++)
            {
                RuntimeInstanceKey candidate = instances[i];
                if (candidate.Kind != RuntimeInstanceKind.SkillExecution ||
                    candidate.CharacterRuntimeId != playback.CharacterRuntimeId ||
                    candidate.ActionInstanceId != playback.ActionInstanceId ||
                    (provenance.SourceGraphRuntimeId != Guid.Empty &&
                     candidate.GraphRuntimeId != provenance.SourceGraphRuntimeId) ||
                    candidate.ActivationGeneration != provenance.SkillExecutionGeneration ||
                    candidate.InvocationGeneration != provenance.SourceActivationGeneration ||
                    !string.Equals(candidate.CallSiteId, provenance.SourceInvocationPath, StringComparison.Ordinal))
                    continue;
                if (graphInstance.IsValid)
                    return false;
                graphInstance = candidate;
            }
            return graphInstance.IsValid;
        }

        static bool OpenSkillGraph(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, string expectedTimelineId, bool pin)
        {
            NodeCanvas.Framework.IGraphElement element = null;
            if (source.Kind is RuntimeSourceElementKind.Node or RuntimeSourceElementKind.Port)
                element = graph.allNodes.SingleOrDefault(node => node.UID == source.ElementAuthoringId);
            else if (source.Kind == RuntimeSourceElementKind.Edge)
                element = graph.allNodes.SelectMany(node => node.outConnections)
                    .SingleOrDefault(edge => edge.UID == source.ElementAuthoringId);
            else if (source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
            {
                BtsmtlSkillTimelineFlowNode[] timelines = graph.allNodes
                    .OfType<BtsmtlSkillTimelineFlowNode>()
                    .Where(node => node.Timeline != null &&
                                   string.Equals(node.Timeline.AuthoringId, source.TimelineAuthoringId, StringComparison.Ordinal))
                    .ToArray();
                if (timelines.Length != 1)
                    return false;
                element = timelines[0];
            }
            else if (source.Kind != RuntimeSourceElementKind.Graph)
                return false;
            if (source.Kind != RuntimeSourceElementKind.Graph && element == null)
                return false;
            if (playback.IsValid &&
                (element is not BtsmtlSkillTimelineFlowNode playbackNode ||
                 !string.Equals(playbackNode.Timeline?.AuthoringId, expectedTimelineId, StringComparison.Ordinal)))
                return false;

            bool graphAlreadyOpen = GraphEditor.current != null &&
                                    ReferenceEquals(GraphEditor.rootGraph, graph) &&
                                    ReferenceEquals(GraphEditor.currentGraph, graph);
            bool observationAlreadyOpen = graphAlreadyOpen &&
                                          BtsmtlSkillObservationSession.IsObserving(definition, graph, instance);
            if (!observationAlreadyOpen)
            {
                BtsmtlSkillObservationSession.Close();
                if (graph.editorObservation is BtsmtlSkillFlowObservation existing)
                    existing.Dispose();
            }
            if (!graphAlreadyOpen)
            {
                if (GraphEditor.currentGraph?.editorObservation is BtsmtlSkillFlowObservation previous)
                    previous.Dispose();
                graph.SetCurrentChildGraphAssignable(null);
                GraphEditor window = GraphEditor.OpenWindow(graph);
                window.Show();
                window.Focus();
            }
            if (element != null)
                GraphEditor.FocusElement(element, true);
            if (!observationAlreadyOpen && UnityEngine.Application.isPlaying &&
                instance.Kind == RuntimeInstanceKind.SkillExecution)
                BtsmtlSkillObservationSession.Open(definition, graph, RuntimeDebugSession.Shared, instance);
            if (element is BtsmtlSkillTimelineFlowNode timelineNode)
            {
                TimelineEditorWindow timelineWindow = TimelineEditorWindow.FindOpen(timelineNode.TimelineAsset);
                if (timelineWindow == null ||
                    !string.Equals(timelineWindow.SourceGraphAuthoringId, ((IBtsmtlSkillFlowGraph)graph).AuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(timelineWindow.SourceNodeAuthoringId, timelineNode.UID, StringComparison.Ordinal))
                {
                    BtsmtlSkillObservationSession.ExpectTimelineOpening(timelineNode);
                    timelineWindow = TimelineEditorWindow.Open(
                        timelineNode.TimelineAsset,
                        ((IBtsmtlSkillFlowGraph)graph).AuthoringId,
                        timelineNode.UID);
                    BtsmtlSkillObservationSession.ExpectTimelineOpening(null);
                }
                else
                    BtsmtlSkillObservationSession.BindOpenTimeline(timelineNode);
                if (timelineWindow != null && UnityEngine.Application.isPlaying)
                {
                    timelineWindow.SetRuntimeObservationReadOnly(true);
                    RuntimeInstanceKey selected = playback.IsValid ? playback : ResolveTimelinePlayback(instance);
                    if (selected.IsValid)
                    {
                        if (!timelineWindow.SelectRuntimeObservationPlayback(selected, pin))
                            return false;
                        TimelineRuntimeObservationBridge.RefreshWindow(timelineWindow);
                    }
                }
                if (timelineWindow == null)
                    return false;
                if (source.Kind is RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
                    timelineWindow.FocusSource(
                        source.TrackAuthoringId,
                        source.Kind is RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip ? source.ClipAuthoringId : string.Empty);
                return true;
            }
            return true;
        }

        static RuntimeInstanceKey ResolveTimelinePlayback(RuntimeInstanceKey instance)
        {
            if (instance.Kind == RuntimeInstanceKind.TimelinePlayback)
                return instance;
            return instance.Kind == RuntimeInstanceKind.TreeClip
                ? RuntimeInstanceKey.Timeline(
                    instance.CharacterRuntimeId,
                    instance.SourceOperationIndex,
                    instance.TimelinePlaybackId,
                    instance.ActionInstanceId)
                : default;
        }
    }
}
