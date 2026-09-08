using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
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
            CharacterPipelineHost host = EditorUtility.InstanceIDToObject(target.HostInstanceId) as CharacterPipelineHost;
            if (!host || !RuntimeDebugSession.Shared.AttachToTarget(instance.CharacterRuntimeId))
                return false;
            return Open(host.Definition, eventView.Source, instance);
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
        {
            if (!definition || !source.IsValid || !definition.RootTreeAsset)
                return false;

            BaseTree root = definition.RootTreeAsset.Tree;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(root, topologyErrors);
            if (!topology.IsValid)
                return false;

            if (!string.IsNullOrEmpty(source.GraphAuthoringId))
            {
                for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
                {
                    BaseTree graph = topology.Graphs[graphIndex].Graph;
                    if (!string.Equals(graph.GraphAuthoringId, source.GraphAuthoringId, StringComparison.Ordinal))
                        continue;
                    graph.RebindReadOnlyViewReferences();
                    return OpenGraph(graph, source, new CharacterPipelineAuthoringContext(definition, instance));
                }
                return false;
            }

            if (!string.IsNullOrEmpty(source.TimelineAuthoringId))
            {
                for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
                {
                    CharacterAuthoringTimelineEntry timeline = topology.Timelines[timelineIndex];
                    if (!string.Equals(timeline.Timeline.AuthoringId, source.TimelineAuthoringId, StringComparison.Ordinal))
                        continue;
                    timeline.Graph.RebindReadOnlyViewReferences();
                    BaseTreeWindow graphWindow = TreeWindowUtility.TreeWindowUtilityInstance.OpenBaseTreeWindow();
                    graphWindow.ReplaceNavigationRoot(timeline.Graph, new CharacterPipelineAuthoringContext(definition, instance));
                    TimelineEditorWindow timelineWindow = TimelineEditorWindow.Open(graphWindow, timeline.Node);
                    return timelineWindow != null && timelineWindow.FocusSource(source.TrackAuthoringId, source.ClipAuthoringId);
                }
            }
            return false;
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
