using System;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    public enum TreeAuthoringElementKind
    {
        None,
        Graph,
        Node,
        Edge
    }

    [Serializable]
    public struct TreeAuthoringElementKey : IEquatable<TreeAuthoringElementKey>
    {
        [SerializeField] TreeAuthoringElementKind m_Kind;
        [SerializeField] string m_GraphAuthoringId;
        [SerializeField] string m_ElementAuthoringId;

        public TreeAuthoringElementKey(
            TreeAuthoringElementKind kind,
            string graphAuthoringId,
            string elementAuthoringId = "")
        {
            m_Kind = kind;
            m_GraphAuthoringId = graphAuthoringId ?? string.Empty;
            m_ElementAuthoringId = elementAuthoringId ?? string.Empty;
        }

        public TreeAuthoringElementKind Kind => m_Kind;
        public string GraphAuthoringId => m_GraphAuthoringId ?? string.Empty;
        public string ElementAuthoringId => m_ElementAuthoringId ?? string.Empty;
        public bool IsValid => Enum.IsDefined(typeof(TreeAuthoringElementKind), Kind) &&
                               Kind != TreeAuthoringElementKind.None &&
                               !string.IsNullOrEmpty(GraphAuthoringId) &&
                               (Kind == TreeAuthoringElementKind.Graph
                                   ? string.IsNullOrEmpty(ElementAuthoringId)
                                   : !string.IsNullOrEmpty(ElementAuthoringId));

        public static TreeAuthoringElementKey Graph(string graphAuthoringId)
        {
            return new TreeAuthoringElementKey(TreeAuthoringElementKind.Graph, graphAuthoringId);
        }

        public static TreeAuthoringElementKey Node(string graphAuthoringId, string nodeAuthoringId)
        {
            return new TreeAuthoringElementKey(TreeAuthoringElementKind.Node, graphAuthoringId, nodeAuthoringId);
        }

        public static TreeAuthoringElementKey Edge(string graphAuthoringId, string edgeAuthoringId)
        {
            return new TreeAuthoringElementKey(TreeAuthoringElementKind.Edge, graphAuthoringId, edgeAuthoringId);
        }

        public bool Equals(TreeAuthoringElementKey other)
        {
            return Kind == other.Kind &&
                   string.Equals(GraphAuthoringId, other.GraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(ElementAuthoringId, other.ElementAuthoringId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is TreeAuthoringElementKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 31 + GraphAuthoringId.GetHashCode();
                hash = hash * 31 + ElementAuthoringId.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind == TreeAuthoringElementKind.Graph
                ? $"Graph:{GraphAuthoringId}"
                : $"{Kind}:{GraphAuthoringId}/{ElementAuthoringId}";
        }
    }

    public enum TreeAuthoringRouteSegmentKind
    {
        NodeGraph,
        EdgeGraph,
        TimelineTreeClip
    }

    public enum TreeGraphReferenceOwnership
    {
        Inline,
        Shared
    }

    [Serializable]
    public struct TreeAuthoringRouteSegment : IEquatable<TreeAuthoringRouteSegment>
    {
        [SerializeField] TreeAuthoringRouteSegmentKind m_Kind;
        [SerializeField] TreeAuthoringElementKey m_OwnerElement;
        [SerializeField] string m_ReferenceKey;
        [SerializeField] string m_ScopeId;
        [SerializeField] string m_ChildGraphAuthoringId;
        [SerializeField] TreeGraphReferenceOwnership m_Ownership;
        [SerializeField] string m_TimelineAuthoringId;
        [SerializeField] string m_TrackAuthoringId;
        [SerializeField] string m_ClipAuthoringId;

        public TreeAuthoringRouteSegment(
            TreeAuthoringRouteSegmentKind kind,
            TreeAuthoringElementKey ownerElement,
            string referenceKey,
            string scopeId,
            string childGraphAuthoringId,
            TreeGraphReferenceOwnership ownership,
            string timelineAuthoringId = "",
            string trackAuthoringId = "",
            string clipAuthoringId = "")
        {
            m_Kind = kind;
            m_OwnerElement = ownerElement;
            m_ReferenceKey = referenceKey ?? string.Empty;
            m_ScopeId = scopeId ?? string.Empty;
            m_ChildGraphAuthoringId = childGraphAuthoringId ?? string.Empty;
            m_Ownership = ownership;
            m_TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            m_TrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_ClipAuthoringId = clipAuthoringId ?? string.Empty;
        }

        public TreeAuthoringRouteSegmentKind Kind => m_Kind;
        public TreeAuthoringElementKey OwnerElement => m_OwnerElement;
        public string ReferenceKey => m_ReferenceKey ?? string.Empty;
        public string ScopeId => m_ScopeId ?? string.Empty;
        public string ChildGraphAuthoringId => m_ChildGraphAuthoringId ?? string.Empty;
        public TreeGraphReferenceOwnership Ownership => m_Ownership;
        public string TimelineAuthoringId => m_TimelineAuthoringId ?? string.Empty;
        public string TrackAuthoringId => m_TrackAuthoringId ?? string.Empty;
        public string ClipAuthoringId => m_ClipAuthoringId ?? string.Empty;
        public bool IsValid => Enum.IsDefined(typeof(TreeAuthoringRouteSegmentKind), Kind) &&
                               OwnerElement.IsValid &&
                               OwnerElement.Kind != TreeAuthoringElementKind.Graph &&
                               (Kind == TreeAuthoringRouteSegmentKind.EdgeGraph
                                   ? OwnerElement.Kind == TreeAuthoringElementKind.Edge
                                   : OwnerElement.Kind == TreeAuthoringElementKind.Node) &&
                               !string.IsNullOrEmpty(ReferenceKey) &&
                               !string.IsNullOrEmpty(ScopeId) &&
                               !string.IsNullOrEmpty(ChildGraphAuthoringId) &&
                               Enum.IsDefined(typeof(TreeGraphReferenceOwnership), Ownership) &&
                               (Kind != TreeAuthoringRouteSegmentKind.TimelineTreeClip ||
                                (!string.IsNullOrEmpty(TimelineAuthoringId) &&
                                 !string.IsNullOrEmpty(TrackAuthoringId) &&
                                 !string.IsNullOrEmpty(ClipAuthoringId)));

        public static TreeAuthoringRouteSegment NodeGraph(
            TreeAuthoringElementKey ownerNode,
            string referenceKey,
            string scopeId,
            string childGraphAuthoringId,
            TreeGraphReferenceOwnership ownership)
        {
            return new TreeAuthoringRouteSegment(
                TreeAuthoringRouteSegmentKind.NodeGraph,
                ownerNode,
                referenceKey,
                scopeId,
                childGraphAuthoringId,
                ownership);
        }

        public static TreeAuthoringRouteSegment EdgeGraph(
            TreeAuthoringElementKey ownerEdge,
            string referenceKey,
            string scopeId,
            string childGraphAuthoringId,
            TreeGraphReferenceOwnership ownership)
        {
            return new TreeAuthoringRouteSegment(
                TreeAuthoringRouteSegmentKind.EdgeGraph,
                ownerEdge,
                referenceKey,
                scopeId,
                childGraphAuthoringId,
                ownership);
        }

        public static TreeAuthoringRouteSegment TimelineTreeClip(
            TreeAuthoringElementKey ownerTimelineNode,
            string referenceKey,
            string scopeId,
            string childGraphAuthoringId,
            TreeGraphReferenceOwnership ownership,
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId)
        {
            return new TreeAuthoringRouteSegment(
                TreeAuthoringRouteSegmentKind.TimelineTreeClip,
                ownerTimelineNode,
                referenceKey,
                scopeId,
                childGraphAuthoringId,
                ownership,
                timelineAuthoringId,
                trackAuthoringId,
                clipAuthoringId);
        }

        public bool Equals(TreeAuthoringRouteSegment other)
        {
            return Kind == other.Kind &&
                   OwnerElement.Equals(other.OwnerElement) &&
                   Ownership == other.Ownership &&
                   string.Equals(ReferenceKey, other.ReferenceKey, StringComparison.Ordinal) &&
                   string.Equals(ScopeId, other.ScopeId, StringComparison.Ordinal) &&
                   string.Equals(ChildGraphAuthoringId, other.ChildGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(TimelineAuthoringId, other.TimelineAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(TrackAuthoringId, other.TrackAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(ClipAuthoringId, other.ClipAuthoringId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is TreeAuthoringRouteSegment other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 31 + OwnerElement.GetHashCode();
                hash = hash * 31 + ReferenceKey.GetHashCode();
                hash = hash * 31 + ScopeId.GetHashCode();
                hash = hash * 31 + ChildGraphAuthoringId.GetHashCode();
                hash = hash * 31 + (int)Ownership;
                hash = hash * 31 + TimelineAuthoringId.GetHashCode();
                hash = hash * 31 + TrackAuthoringId.GetHashCode();
                hash = hash * 31 + ClipAuthoringId.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{OwnerElement}/{ReferenceKey}/{ScopeId}->{ChildGraphAuthoringId}";
        }
    }

    [Serializable]
    public sealed class TreeAuthoringRouteId : IEquatable<TreeAuthoringRouteId>
    {
        [SerializeField] string m_RootGraphAuthoringId;
        [SerializeField] TreeAuthoringRouteSegment[] m_Segments = Array.Empty<TreeAuthoringRouteSegment>();

        public TreeAuthoringRouteId(string rootGraphAuthoringId, TreeAuthoringRouteSegment[] segments = null)
        {
            m_RootGraphAuthoringId = rootGraphAuthoringId ?? string.Empty;
            m_Segments = segments != null
                ? (TreeAuthoringRouteSegment[])segments.Clone()
                : Array.Empty<TreeAuthoringRouteSegment>();
        }

        public string RootGraphAuthoringId => m_RootGraphAuthoringId ?? string.Empty;
        public int Count => m_Segments?.Length ?? 0;
        public TreeAuthoringRouteSegment this[int index] => m_Segments[index];
        public string LeafGraphAuthoringId => Count > 0 ? m_Segments[Count - 1].ChildGraphAuthoringId : RootGraphAuthoringId;
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(RootGraphAuthoringId))
                    return false;

                string expectedOwnerGraph = RootGraphAuthoringId;
                for (int i = 0; i < Count; i++)
                {
                    TreeAuthoringRouteSegment segment = m_Segments[i];
                    if (!segment.IsValid ||
                        !string.Equals(segment.OwnerElement.GraphAuthoringId, expectedOwnerGraph, StringComparison.Ordinal))
                        return false;
                    expectedOwnerGraph = segment.ChildGraphAuthoringId;
                }
                return true;
            }
        }

        public static TreeAuthoringRouteId Root(string graphAuthoringId)
        {
            return new TreeAuthoringRouteId(graphAuthoringId);
        }

        public TreeAuthoringRouteId Append(TreeAuthoringRouteSegment segment)
        {
            if (!IsValid)
                throw new InvalidOperationException("Cannot append to an invalid Tree authoring route.");
            if (!segment.IsValid)
                throw new ArgumentException("Tree authoring route segment is invalid.", nameof(segment));
            if (!string.Equals(segment.OwnerElement.GraphAuthoringId, LeafGraphAuthoringId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Tree authoring route segment owner '{segment.OwnerElement.GraphAuthoringId}' does not match leaf Graph '{LeafGraphAuthoringId}'.");

            var segments = new TreeAuthoringRouteSegment[Count + 1];
            if (Count > 0)
                Array.Copy(m_Segments, segments, Count);
            segments[Count] = segment;
            return new TreeAuthoringRouteId(RootGraphAuthoringId, segments);
        }

        public bool Equals(TreeAuthoringRouteId other)
        {
            if (ReferenceEquals(other, null) ||
                !string.Equals(RootGraphAuthoringId, other.RootGraphAuthoringId, StringComparison.Ordinal) ||
                Count != other.Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (!m_Segments[i].Equals(other.m_Segments[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is TreeAuthoringRouteId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RootGraphAuthoringId.GetHashCode();
                for (int i = 0; i < Count; i++)
                    hash = hash * 31 + m_Segments[i].GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            string route = RootGraphAuthoringId;
            for (int i = 0; i < Count; i++)
                route += $"/{m_Segments[i]}";
            return route;
        }
    }

    public static class TreeAuthoringDiagnosticsProjection
    {
        public static RuntimeSourceElementKey ToRuntimeSourceElementKey(this TreeAuthoringElementKey key)
        {
            switch (key.Kind)
            {
                case TreeAuthoringElementKind.Graph:
                    return RuntimeSourceElementKey.Graph(key.GraphAuthoringId);
                case TreeAuthoringElementKind.Node:
                    return RuntimeSourceElementKey.Node(key.GraphAuthoringId, key.ElementAuthoringId);
                case TreeAuthoringElementKind.Edge:
                    return RuntimeSourceElementKey.Edge(key.GraphAuthoringId, key.ElementAuthoringId);
                default:
                    return default;
            }
        }
    }

    public static class TreeAuthoringRouteBuilder
    {
        public static TreeAuthoringRouteId AppendNodeGraph(
            BaseGraph ownerGraph,
            BaseNode ownerNode,
            string referenceKey,
            string scopeId,
            BaseGraph childGraph,
            TreeGraphReferenceOwnership ownership)
        {
            if (ownerGraph == null || ownerNode == null || childGraph == null)
                throw new ArgumentNullException(ownerGraph == null ? nameof(ownerGraph) : ownerNode == null ? nameof(ownerNode) : nameof(childGraph));
            if (ownerGraph.AuthoringRoute == null || !ownerGraph.AuthoringRoute.IsValid)
                throw new InvalidOperationException($"Owner Graph '{ownerGraph.name}/{ownerGraph.GraphAuthoringId}' has no valid authoring route.");

            return ownerGraph.AuthoringRoute.Append(TreeAuthoringRouteSegment.NodeGraph(
                TreeAuthoringElementKey.Node(ownerGraph.GraphAuthoringId, ownerNode.GUID),
                referenceKey,
                scopeId,
                childGraph.GraphAuthoringId,
                ownership));
        }

        public static TreeAuthoringRouteId AppendEdgeGraph(
            BaseGraph ownerGraph,
            BaseEdge ownerEdge,
            string referenceKey,
            string scopeId,
            BaseGraph childGraph,
            TreeGraphReferenceOwnership ownership)
        {
            if (ownerGraph == null || ownerEdge == null || childGraph == null)
                throw new ArgumentNullException(ownerGraph == null ? nameof(ownerGraph) : ownerEdge == null ? nameof(ownerEdge) : nameof(childGraph));
            if (ownerGraph.AuthoringRoute == null || !ownerGraph.AuthoringRoute.IsValid)
                throw new InvalidOperationException($"Owner Graph '{ownerGraph.name}/{ownerGraph.GraphAuthoringId}' has no valid authoring route.");

            return ownerGraph.AuthoringRoute.Append(TreeAuthoringRouteSegment.EdgeGraph(
                TreeAuthoringElementKey.Edge(ownerGraph.GraphAuthoringId, ownerEdge.GUID),
                referenceKey,
                scopeId,
                childGraph.GraphAuthoringId,
                ownership));
        }
    }
}
