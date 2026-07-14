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
                BaseTree root = definition?.RootTreeAsset ? definition.RootTreeAsset.Tree : null;
                if (root == null)
                    continue;

                var topologyErrors = new List<string>();
                CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(root, topologyErrors);
                if (!topology.IsValid)
                    continue;

                if (!string.IsNullOrEmpty(source.GraphAuthoringId))
                {
                    for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
                    {
                        BaseTree graph = topology.Graphs[graphIndex].Graph;
                        if (!string.Equals(graph.GraphAuthoringId, source.GraphAuthoringId, StringComparison.Ordinal))
                            continue;
                        graph.RebindReadOnlyViewReferences();
                        return OpenGraph(graph, source.ElementAuthoringId, new CharacterPipelineAuthoringContext(definition));
                    }
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
                        graphWindow.ReplaceNavigationRoot(
                            timeline.Graph,
                            new CharacterPipelineAuthoringContext(definition));
                        TimelineEditorWindow timelineWindow = TimelineEditorWindow.Open(graphWindow, timeline.Node);
                        return timelineWindow != null && timelineWindow.FocusSource(source.TrackAuthoringId, source.ClipAuthoringId);
                    }
                }
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

        public static bool OpenGraph(BaseTree graph, string elementAuthoringId, object authoringContext = null)
        {
            if (graph == null)
                return false;
            BaseTreeWindow window = TreeWindowUtility.TreeWindowUtilityInstance.OpenBaseTreeWindow();
            window.ReplaceNavigationRoot(graph, authoringContext);
            if (!string.IsNullOrEmpty(elementAuthoringId))
            {
                BaseNodeView nodeView = window.TreeView.NodeViews.FirstOrDefault(i => string.Equals(i.Node.GUID, elementAuthoringId, StringComparison.Ordinal));
                if (nodeView != null)
                {
                    window.TreeView.ClearSelection();
                    window.TreeView.AddToSelection(nodeView);
                    window.PopulateSelectionInspector(window.TreeView.selection);
                }
                else
                {
                    BaseEdgeView edgeView = window.TreeView.edges.ToList().OfType<BaseEdgeView>().FirstOrDefault(i => string.Equals(i.Edge.GUID, elementAuthoringId, StringComparison.Ordinal));
                    if (edgeView != null)
                    {
                        window.TreeView.ClearSelection();
                        window.TreeView.AddToSelection(edgeView);
                        window.PopulateSelectionInspector(window.TreeView.selection);
                    }
                }
            }
            window.Show();
            window.Focus();
            return true;
        }

    }
}
