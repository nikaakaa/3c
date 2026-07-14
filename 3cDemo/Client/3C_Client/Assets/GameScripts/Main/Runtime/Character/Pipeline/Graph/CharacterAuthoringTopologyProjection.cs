using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    public sealed class CharacterAuthoringTopologyProjection
    {
        readonly List<CharacterAuthoringGraphEntry> m_Graphs = new List<CharacterAuthoringGraphEntry>();
        readonly List<CharacterAuthoringTimelineEntry> m_Timelines = new List<CharacterAuthoringTimelineEntry>();
        readonly HashSet<BaseGraph> m_FirstOccurrences = new HashSet<BaseGraph>();

        public bool IsValid { get; private set; }
        public IReadOnlyList<CharacterAuthoringGraphEntry> Graphs => m_Graphs;
        public IReadOnlyList<CharacterAuthoringTimelineEntry> Timelines => m_Timelines;

        public static CharacterAuthoringTopologyProjection Build(BaseTree rootTree, List<string> errors)
        {
            var projection = new CharacterAuthoringTopologyProjection();
            if (rootTree == null || string.IsNullOrEmpty(rootTree.GraphAuthoringId))
            {
                errors?.Add("Character authoring topology requires a RootTree with a GraphAuthoringId.");
                return projection;
            }

            projection.IsValid = projection.Visit(
                rootTree,
                TreeAuthoringRouteId.Root(rootTree.GraphAuthoringId),
                null,
                new List<BaseGraph>(),
                new HashSet<BaseGraph>(),
                errors);
            return projection;
        }

        bool Visit(
            BaseTree graph,
            TreeAuthoringRouteId route,
            object parentOwner,
            IReadOnlyList<BaseGraph> ancestors,
            HashSet<BaseGraph> recursionPath,
            List<string> errors)
        {
            if (graph == null || route == null || !route.IsValid ||
                !string.Equals(route.LeafGraphAuthoringId, graph.GraphAuthoringId, StringComparison.Ordinal))
            {
                errors?.Add("Character authoring topology encountered an invalid Graph route.");
                return false;
            }
            if (!recursionPath.Add(graph))
            {
                errors?.Add($"Character authoring topology contains a recursive Graph reference at '{route}'.");
                return false;
            }

            bool valid = true;
            bool firstOccurrence = m_FirstOccurrences.Add(graph);
            var visibleGraphs = new List<BaseGraph>(ancestors) { graph };
            m_Graphs.Add(new CharacterAuthoringGraphEntry(
                route,
                graph,
                parentOwner,
                firstOccurrence,
                visibleGraphs.ToArray()));
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                BaseNode node = graph.Nodes[nodeIndex];
                if (node == null)
                    continue;

                if (node is TimelineNode timelineNode && timelineNode.Timeline != null)
                {
                    TimelineData timeline = timelineNode.Timeline;
                    m_Timelines.Add(new CharacterAuthoringTimelineEntry(route, graph, timelineNode, timeline));
                    valid &= VisitTimelineTrees(
                        route,
                        graph,
                        timelineNode,
                        timeline,
                        visibleGraphs,
                        recursionPath,
                        errors);
                }

                var references = new List<NodeGraphReference>(node.GetGraphReferences());
                for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
                {
                    NodeGraphReference reference = references[referenceIndex];
                    if (reference.Tree == null)
                    {
                        if (reference.Required)
                        {
                            errors?.Add($"Graph reference '{graph.name}/{node.GUID}/{reference.Key}' is missing.");
                            valid = false;
                        }
                        continue;
                    }
                    if (string.IsNullOrEmpty(reference.ScopeId) && HasScopedReference(references, reference.Tree))
                        continue;
                    if (string.IsNullOrEmpty(reference.Key) || string.IsNullOrEmpty(reference.ScopeId))
                    {
                        errors?.Add($"Graph reference '{graph.name}/{node.GUID}' has no stable ReferenceKey or ScopeId.");
                        valid = false;
                        continue;
                    }

                    TreeAuthoringRouteId childRoute = route.Append(TreeAuthoringRouteSegment.NodeGraph(
                        TreeAuthoringElementKey.Node(graph.GraphAuthoringId, node.GUID),
                        reference.Key,
                        reference.ScopeId,
                        reference.Tree.GraphAuthoringId,
                        reference.Inline ? TreeGraphReferenceOwnership.Inline : TreeGraphReferenceOwnership.Shared));
                    valid &= Visit(reference.Tree, childRoute, node, visibleGraphs, recursionPath, errors);
                }
            }

            for (int edgeIndex = 0; edgeIndex < graph.Edges.Count; edgeIndex++)
            {
                BaseEdge edge = graph.Edges[edgeIndex];
                if (edge == null || !edge.TryResolveConditionRuleGraph(out ConditionRuleGraph conditionGraph, out _))
                    continue;
                TreeGraphReferenceOwnership ownership = edge.ConditionRuleGraphOwnership == ConditionRuleGraphOwnership.Shared
                    ? TreeGraphReferenceOwnership.Shared
                    : TreeGraphReferenceOwnership.Inline;
                TreeAuthoringRouteId conditionRoute = route.Append(TreeAuthoringRouteSegment.EdgeGraph(
                    TreeAuthoringElementKey.Edge(graph.GraphAuthoringId, edge.GUID),
                    "conditionRuleGraph",
                    edge.GUID,
                    conditionGraph.GraphAuthoringId,
                    ownership));
                valid &= Visit(conditionGraph, conditionRoute, edge, visibleGraphs, recursionPath, errors);
            }

            recursionPath.Remove(graph);
            return valid;
        }

        bool VisitTimelineTrees(
            TreeAuthoringRouteId ownerRoute,
            BaseTree ownerGraph,
            TimelineNode ownerNode,
            TimelineData timeline,
            IReadOnlyList<BaseGraph> ancestors,
            HashSet<BaseGraph> recursionPath,
            List<string> errors)
        {
            bool valid = true;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack treeTrack)
                    continue;
                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is not TreeClip treeClip)
                        continue;
                    TimelineRunningTree tree = treeClip.ResolvedTree;
                    if (tree == null || treeClip.Ownership == TimelineTreeOwnership.Missing)
                    {
                        errors?.Add($"Timeline '{timeline.Name}' TreeClip '{treeClip.AuthoringId}' is missing its graph.");
                        valid = false;
                        continue;
                    }
                    TreeAuthoringRouteId childRoute = ownerRoute.Append(TreeAuthoringRouteSegment.TimelineTreeClip(
                        TreeAuthoringElementKey.Node(ownerGraph.GraphAuthoringId, ownerNode.GUID),
                        "timeline.treeClip",
                        treeClip.AuthoringId,
                        tree.GraphAuthoringId,
                        treeClip.Ownership == TimelineTreeOwnership.Shared
                            ? TreeGraphReferenceOwnership.Shared
                            : TreeGraphReferenceOwnership.Inline,
                        timeline.AuthoringId,
                        treeTrack.AuthoringId,
                        treeClip.AuthoringId));
                    valid &= Visit(tree, childRoute, treeClip, ancestors, recursionPath, errors);
                }
            }
            return valid;
        }

        static bool HasScopedReference(IReadOnlyList<NodeGraphReference> references, BaseTree tree)
        {
            for (int i = 0; i < references.Count; i++)
            {
                if (ReferenceEquals(references[i].Tree, tree) && !string.IsNullOrEmpty(references[i].ScopeId))
                    return true;
            }
            return false;
        }
    }

    public readonly struct CharacterAuthoringGraphEntry
    {
        public CharacterAuthoringGraphEntry(
            TreeAuthoringRouteId route,
            BaseTree graph,
            object parentOwner,
            bool firstOccurrence,
            IReadOnlyList<BaseGraph> visibleGraphs)
        {
            Route = route;
            Graph = graph;
            ParentOwner = parentOwner;
            FirstOccurrence = firstOccurrence;
            VisibleGraphs = visibleGraphs ?? Array.Empty<BaseGraph>();
        }

        public TreeAuthoringRouteId Route { get; }
        public BaseTree Graph { get; }
        public object ParentOwner { get; }
        public bool FirstOccurrence { get; }
        public IReadOnlyList<BaseGraph> VisibleGraphs { get; }
    }

    public readonly struct CharacterAuthoringTimelineEntry
    {
        public CharacterAuthoringTimelineEntry(
            TreeAuthoringRouteId route,
            BaseTree graph,
            TimelineNode node,
            TimelineData timeline)
        {
            Route = route;
            Graph = graph;
            Node = node;
            Timeline = timeline;
        }

        public TreeAuthoringRouteId Route { get; }
        public BaseTree Graph { get; }
        public TimelineNode Node { get; }
        public TimelineData Timeline { get; }
    }
}
