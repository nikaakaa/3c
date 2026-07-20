using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
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
        public static bool Open(RuntimeSourceElementKey source)
        {
            if (!source.IsValid)
                return false;

            string[] definitionGuids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
            for (int i = 0; i < definitionGuids.Length; i++)
            {
                CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(AssetDatabase.GUIDToAssetPath(definitionGuids[i]));
                if (Open(definition, source))
                    return true;
            }

            if (!string.IsNullOrEmpty(source.TimelineAuthoringId))
            {
                string[] timelineGuids = AssetDatabase.FindAssets("t:TimelineAsset");
                for (int i = 0; i < timelineGuids.Length; i++)
                {
                    TimelineAsset asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(timelineGuids[i]));
                    if (!asset || !string.Equals(asset.Data?.AuthoringId, source.TimelineAuthoringId, StringComparison.Ordinal))
                        continue;
                    TimelineEditorWindow window = TimelineEditorWindow.Open(asset);
                    return window != null && window.FocusSource(source.TrackAuthoringId, source.ClipAuthoringId);
                }
            }
            return false;
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source)
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
                    return OpenGraph(graph, source, new CharacterPipelineAuthoringContext(definition));
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
                    graphWindow.ReplaceNavigationRoot(timeline.Graph, new CharacterPipelineAuthoringContext(definition));
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
                BaseNodeView nodeView = window.TreeView.NodeViews.FirstOrDefault(i => string.Equals(i.Node.GUID, source.ElementAuthoringId, StringComparison.Ordinal));
                if (nodeView != null)
                {
                    window.TreeView.ClearSelection();
                    window.TreeView.AddToSelection(nodeView);
                    window.PopulateSelectionInspector(window.TreeView.selection);
                }
                else resolved = false;
            }
            else if (source.Kind == RuntimeSourceElementKind.Edge)
            {
                BaseEdgeView edgeView = window.TreeView.edges.ToList().OfType<BaseEdgeView>().FirstOrDefault(i => string.Equals(i.Edge.GUID, source.ElementAuthoringId, StringComparison.Ordinal));
                if (edgeView != null)
                {
                    window.TreeView.ClearSelection();
                    window.TreeView.AddToSelection(edgeView);
                    window.PopulateSelectionInspector(window.TreeView.selection);
                }
                else resolved = false;
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
