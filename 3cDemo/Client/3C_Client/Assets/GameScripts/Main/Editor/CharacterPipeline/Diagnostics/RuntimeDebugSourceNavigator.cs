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
        public static bool Open(RuntimeDebugEventView eventView)
        {
            RuntimeTraceEvent trace = eventView.Event;
            RuntimeInstanceKey instance = trace.RuntimeInstance;
            if (!instance.IsValid || !eventView.Source.IsValid ||
                !RuntimeDiagnosticsTargetRegistry.TryGet(instance.CharacterRuntimeId, out RuntimeDiagnosticsTarget target))
                return false;
            if (target.SessionId != trace.SessionId || !target.Revision.Equals(trace.ProgramRevision) ||
                !target.SourceMap.TryGet(trace.Source, out DebugSourceMapEntry entry) || !entry.Source.Equals(eventView.Source))
                return false;
            CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(EditorUtility.InstanceIDToObject(target.HostInstanceId));
            if (!definition || !RuntimeDebugSession.Shared.AttachToTarget(instance.CharacterRuntimeId))
                return false;
            return Open(definition, eventView.Source, instance);
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
        {
            if (!definition || !source.IsValid)
                return false;

            if (!string.IsNullOrEmpty(source.GraphAuthoringId))
            {
                FlowGraph[] nativeGraphs = definition.SkillGraphs
                    .Where(graph => graph != null)
                    .SelectMany(graph => BtsmtlSkillGraphClosure.Validate(graph, false))
                    .Distinct()
                    .Where(graph => ((IBtsmtlSkillFlowGraph)graph).AuthoringId == source.GraphAuthoringId)
                    .ToArray();
                if (nativeGraphs.Length != 0)
                    return nativeGraphs.Length == 1 && OpenSkillGraph(definition, nativeGraphs[0], source, instance);
            }
            return false;
        }

        static bool OpenSkillGraph(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeSourceElementKey source, RuntimeInstanceKey instance)
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
            if (element is BtsmtlSkillTimelineFlowNode timelineNode)
            {
                TimelineEditorWindow timelineWindow = TimelineEditorWindow.Open(
                    timelineNode.TimelineAsset,
                    ((IBtsmtlSkillFlowGraph)graph).AuthoringId,
                    timelineNode.UID);
                return timelineWindow != null && timelineWindow.FocusSource(
                    source.Kind == RuntimeSourceElementKind.Timeline ? string.Empty : source.TrackAuthoringId,
                    source.Kind is RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip ? source.ClipAuthoringId : string.Empty);
            }
            if (UnityEngine.Application.isPlaying && instance.IsValid)
                BtsmtlSkillObservationSession.Open(definition, graph, RuntimeDebugSession.Shared, instance);
            return true;
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
