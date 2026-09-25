using TreeDesigner.Authoring;
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
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class RuntimeDebugSourceNavigator
    {
        public static bool Open(RuntimeDebugEventView eventView, bool followGraph = false)
        {
            RuntimeTraceEvent trace = eventView.Event;
            RuntimeInstanceKey instance = trace.RuntimeInstance;
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            if (!instance.IsValid || !eventView.Source.IsValid ||
                !RuntimeDiagnosticsTargetRegistry.TryGet(instance.CharacterRuntimeId, out RuntimeDiagnosticsTarget target))
                return false;
            if (session.AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended)
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
            else if (target.SessionId != trace.SessionId || !target.Revision.Equals(trace.ContentRevision) ||
                     !target.SourceMap.TryGet(trace.Source, out DebugSourceMapEntry entry) ||
                     !entry.Source.Equals(eventView.Source))
            {
                return false;
            }
            CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(EditorUtility.InstanceIDToObject(target.HostInstanceId));
            if (!definition)
                return false;
            if (session.AttachmentState is not RuntimeDebugAttachmentState.CaptureHistory and
                not RuntimeDebugAttachmentState.Ended &&
                !session.AttachToTarget(instance.CharacterRuntimeId))
                return false;
            if (eventView.Source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or
                RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
            {
                return OpenTimelineSource(definition, instance, trace.Payload.TimelinePlayback);
            }
            return Open(definition, followGraph
                ? RuntimeSourceElementKey.Graph(eventView.Source.GraphAuthoringId)
                : eventView.Source, instance);
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
            => Open(definition, source, instance, default);

        static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback)
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
                    return nativeGraphs.Length == 1 && OpenSkillGraph(definition, nativeGraphs[0], source, instance, playback);
            }
            return false;
        }

        static bool OpenTimelineSource(
            CharacterPipelineDefinition definition,
            RuntimeInstanceKey instance,
            RuntimeTimelinePlaybackProvenance provenance)
        {
            if (!provenance.IsValid ||
                string.IsNullOrEmpty(provenance.SourceGraphAuthoringId) ||
                string.IsNullOrEmpty(provenance.SourceNodeAuthoringId))
            {
                return false;
            }
            var instances = new List<RuntimeInstanceKey>();
            RuntimeDebugSession.Shared.ViewModel.CopyGraphInstances(provenance.SourceGraphAuthoringId, instances);
            RuntimeInstanceKey graphInstance = default;
            for (int i = 0; i < instances.Count; i++)
            {
                RuntimeInstanceKey candidate = instances[i];
                if (candidate.Kind != RuntimeInstanceKind.SkillExecution ||
                    candidate.CharacterRuntimeId != instance.CharacterRuntimeId ||
                    candidate.ActionInstanceId != instance.ActionInstanceId ||
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
            if (!graphInstance.IsValid)
                return false;
            return Open(
                definition,
                RuntimeSourceElementKey.Node(
                    provenance.SourceGraphAuthoringId,
                    provenance.SourceNodeAuthoringId),
                graphInstance,
                instance);
        }

        static bool OpenSkillGraph(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback)
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

            BtsmtlSkillObservationSession.Close();
            if (graph.editorObservation is BtsmtlSkillFlowObservation existing)
                existing.Dispose();
            if (GraphEditor.currentGraph?.editorObservation is BtsmtlSkillFlowObservation previous)
                previous.Dispose();
            graph.SetCurrentChildGraphAssignable(null);
            GraphEditor window = GraphEditor.OpenWindow(graph);
            window.Show();
            window.Focus();
            if (element != null)
                GraphEditor.FocusElement(element, true);
            if (UnityEngine.Application.isPlaying && instance.Kind == RuntimeInstanceKind.SkillExecution)
                BtsmtlSkillObservationSession.Open(definition, graph, RuntimeDebugSession.Shared, instance);
            if (element is BtsmtlSkillTimelineFlowNode timelineNode)
            {
                BtsmtlSkillObservationSession.ExpectTimelineOpening(timelineNode);
                TimelineEditorWindow timelineWindow = TimelineEditorWindow.Open(
                    timelineNode.TimelineAsset,
                    ((IBtsmtlSkillFlowGraph)graph).AuthoringId,
                    timelineNode.UID);
                BtsmtlSkillObservationSession.ExpectTimelineOpening(null);
                if (timelineWindow != null && UnityEngine.Application.isPlaying)
                {
                    timelineWindow.SetRuntimeObservationReadOnly(true);
                    RuntimeInstanceKey selected = playback.IsValid ? playback : ResolveTimelinePlayback(instance);
                    if (selected.IsValid)
                        timelineWindow.SelectRuntimeObservationPlayback(selected);
                }
                return timelineWindow != null && timelineWindow.FocusSource(
                    source.Kind == RuntimeSourceElementKind.Timeline ? string.Empty : source.TrackAuthoringId,
                    source.Kind is RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip ? source.ClipAuthoringId : string.Empty);
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

        public static bool OpenGraph(BaseTree graph, string elementAuthoringId, object authoringContext = null)
        {
            RuntimeSourceElementKey source = string.IsNullOrEmpty(elementAuthoringId)
                ? RuntimeSourceElementKey.Graph(graph?.GraphAuthoringId)
                : RuntimeSourceElementKey.Node(graph?.GraphAuthoringId, elementAuthoringId);
            if (OpenGraph(graph, source, authoringContext))
                return true;
            if (string.IsNullOrEmpty(elementAuthoringId))
                return false;
            return OpenGraph(graph, RuntimeSourceElementKey.Edge(graph?.GraphAuthoringId, elementAuthoringId), authoringContext);
        }

        static bool OpenGraph(BaseTree graph, RuntimeSourceElementKey source, object authoringContext)
        {
            if (graph == null)
                return false;
            BaseTreeWindow window = TreeWindowUtility.TreeWindowUtilityInstance.OpenBaseTreeWindow();
            window.ReplaceNavigationRoot(graph, authoringContext);
            bool resolved = true;
            if (source.Kind == RuntimeSourceElementKind.Node)
            {
                resolved = graph.Nodes.Exists(value =>
                    value != null &&
                    string.Equals(
                        value.GUID,
                        source.ElementAuthoringId,
                        StringComparison.Ordinal));
                if (resolved)
                {
                    window.FocusSharedElement(
                        new GraphAuthoringElementId(
                            source.ElementAuthoringId));
                }
            }
            else if (source.Kind == RuntimeSourceElementKind.Edge)
            {
                resolved = graph.Edges.Exists(value =>
                               value != null &&
                               string.Equals(
                                   value.GUID,
                                   source.ElementAuthoringId,
                                   StringComparison.Ordinal)) ||
                           graph.PropertyEdges.Exists(value =>
                               value != null &&
                               string.Equals(
                                   value.GUID,
                                   source.ElementAuthoringId,
                                   StringComparison.Ordinal));
                if (resolved)
                {
                    window.FocusSharedElement(
                        new GraphAuthoringElementId(
                            source.ElementAuthoringId));
                }
            }
            else if (source.Kind == RuntimeSourceElementKind.BlackboardDeclaration)
                resolved = window.FocusBlackboardDeclaration(source.GraphAuthoringId, source.ElementAuthoringId);
            else if (source.Kind != RuntimeSourceElementKind.Graph)
                resolved = false;
            if (!resolved)
            {
                window.Close();
                return false;
            }
            window.Show();
            window.Focus();
            return true;
        }

    }
}
