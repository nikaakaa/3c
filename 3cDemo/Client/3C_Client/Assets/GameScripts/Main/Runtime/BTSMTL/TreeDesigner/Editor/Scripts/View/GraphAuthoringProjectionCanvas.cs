using System;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public interface IGraphAuthoringClipboardCodec
    {
        string Serialize(IGraphAuthoringDocumentProjection document, IReadOnlyList<GraphAuthoringSelection> selection);
        bool CanPaste(IGraphAuthoringDocumentProjection document, string payload);
        void Paste(IGraphAuthoringDocumentProjection document, string operationName, string payload, Vector2 graphPosition);
    }

    public sealed class GraphAuthoringProjectionCanvasBinding
    {
        public GraphAuthoringProjectionCanvasBinding(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringConnectionPolicy connectionPolicy,
            IGraphAuthoringClipboardCodec clipboard = null,
            bool persistsLayout = true)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            ConnectionPolicy = connectionPolicy ?? throw new ArgumentNullException(nameof(connectionPolicy));
            Clipboard = clipboard;
            PersistsLayout = persistsLayout;
        }

        public IGraphAuthoringDocumentProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringConnectionPolicy ConnectionPolicy { get; }
        public IGraphAuthoringClipboardCodec Clipboard { get; }
        public bool PersistsLayout { get; }
    }

    sealed class GraphAuthoringProjectedPortView :
        GraphAuthoringPortViewBase
    {
        public GraphAuthoringProjectedPortView(
            GraphAuthoringProjectedNodeView nodeView,
            GraphAuthoringPortId portId,
            string displayName,
            string valueTypeId,
            GraphAuthoringPortDirection direction,
            GraphAuthoringPortCapacity capacity,
            bool required,
            bool dynamic)
            : base(
                Orientation.Horizontal,
                direction == GraphAuthoringPortDirection.Input ? Direction.Input : Direction.Output,
                capacity == GraphAuthoringPortCapacity.Single ? Capacity.Single : Capacity.Multi,
                typeof(GraphAuthoringPortValue))
        {
            NodeView = nodeView ?? throw new ArgumentNullException(nameof(nodeView));
            PortId = portId.IsValid ? portId : throw new ArgumentException("Graph authoring port identity is missing.", nameof(portId));
            ValueTypeId = string.IsNullOrWhiteSpace(valueTypeId) ? throw new ArgumentException("Graph authoring port value type is missing.", nameof(valueTypeId)) : valueTypeId;
            Required = required;
            Dynamic = dynamic;
            portName = displayName ?? string.Empty;
            tooltip = $"{ValueTypeId} · {(Required ? "Required" : "Optional")}";
            portColor = ResolveColor(ValueTypeId);
            InstallConnector<Edge>();
        }

        public GraphAuthoringProjectedNodeView NodeView { get; }
        public GraphAuthoringPortId PortId { get; }
        public string ValueTypeId { get; }
        public bool Required { get; }
        public bool Dynamic { get; }

        static Color ResolveColor(string valueTypeId) => valueTypeId switch
        {
            "pose.local" => new Color32(93, 173, 255, 255),
            "pose.component" => new Color32(255, 170, 76, 255),
            "component.full-body-ik-goals" => new Color32(96, 220, 164, 255),
            _ => new Color32(180, 180, 180, 255)
        };

        sealed class GraphAuthoringPortValue { }
    }

    sealed class GraphAuthoringProjectedNodeView :
        GraphAuthoringNodeViewBase
    {
        readonly Dictionary<GraphAuthoringPortId, GraphAuthoringProjectedPortView> m_Ports =
            new Dictionary<GraphAuthoringPortId, GraphAuthoringProjectedPortView>();

        public GraphAuthoringProjectedNodeView(
            GraphAuthoringNodeProjection projection,
            GraphAuthoringCapabilityDescriptor capability)
            : base(AssetDatabase.GUIDToAssetPath(BaseNodeView.DefaultVisualTreeGUID))
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            BindAuthoringPresentation(
                projection.NodeId.Value,
                string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? capability.DisplayName
                    : projection.DisplayName,
                projection.Position,
                capability.Color);
            BindAuthoringDescriptor(
                capability,
                projection.DisplayName,
                projection.Status);
            AddToClassList($"graph-authoring-node-{capability.PresentationKind.ToString().ToLowerInvariant()}");
            BuildFixedPorts(capability.FixedPorts);
            BuildDynamicPorts(projection.DynamicPorts);
            RefreshExpandedState();
            RefreshPorts();
        }

        public GraphAuthoringNodeProjection Projection { get; }
        public GraphAuthoringCapabilityDescriptor Capability { get; }
        public IReadOnlyDictionary<GraphAuthoringPortId, GraphAuthoringProjectedPortView> Ports => m_Ports;

        public GraphAuthoringProjectedPortView RequirePort(GraphAuthoringPortId portId)
        {
            if (!m_Ports.TryGetValue(portId, out GraphAuthoringProjectedPortView port))
                throw new InvalidOperationException($"Node '{Projection.NodeId}' does not project port '{portId}'.");
            return port;
        }

        void BuildFixedPorts(IEnumerable<GraphAuthoringPortDescriptor> ports)
        {
            foreach (GraphAuthoringPortDescriptor descriptor in ports.OrderBy(value => value.Order))
            {
                AddPort(new GraphAuthoringProjectedPortView(
                    this,
                    descriptor.PortId,
                    descriptor.DisplayName,
                    descriptor.ValueTypeId,
                    descriptor.Direction,
                    descriptor.Capacity,
                    descriptor.Required,
                    false));
            }
        }

        void BuildDynamicPorts(IEnumerable<GraphAuthoringDynamicPortProjection> ports)
        {
            foreach (GraphAuthoringDynamicPortProjection descriptor in ports.OrderBy(value => value.Order))
            {
                AddPort(new GraphAuthoringProjectedPortView(
                    this,
                    descriptor.PortId,
                    descriptor.DisplayName,
                    descriptor.ValueTypeId,
                    descriptor.Direction,
                    descriptor.Capacity,
                    descriptor.Required,
                    true));
            }
        }

        void AddPort(GraphAuthoringProjectedPortView port)
        {
            if (!m_Ports.TryAdd(port.PortId, port))
                throw new InvalidOperationException($"Node '{Projection.NodeId}' contains duplicate port '{port.PortId}'.");
            if (port.direction == Direction.Input)
                inputContainer.Add(port);
            else
                outputContainer.Add(port);
        }
    }

    sealed class GraphAuthoringProjectedEdgeView : GraphAuthoringEdgeViewBase
    {
        public GraphAuthoringProjectedEdgeView(GraphAuthoringElementId edgeId)
        {
            EdgeId = edgeId.IsValid ? edgeId : throw new ArgumentException("Graph authoring edge identity is missing.", nameof(edgeId));
            BindAuthoringIdentity(edgeId.Value);
        }

        public void BindProjection(
            GraphAuthoringEdgeProjection projection)
        {
            if (!projection.EdgeId.Equals(EdgeId))
                throw new InvalidOperationException(
                    $"Projected edge '{EdgeId}' received mismatched projection '{projection.EdgeId}'.");
            BindAuthoringProjection(projection);
        }

        public GraphAuthoringElementId EdgeId { get; }
    }

    public partial class GraphAuthoringCanvasView
    {
        readonly Dictionary<GraphAuthoringElementId, GraphAuthoringProjectedNodeView> m_ProjectionNodes =
            new Dictionary<GraphAuthoringElementId, GraphAuthoringProjectedNodeView>();
        GraphAuthoringProjectionCanvasBinding m_ProjectionBinding;
        bool m_PopulatingProjection;
        bool m_ProjectionCallbacksBound;
        Vector2 m_ProjectionPastePosition;

        public GraphAuthoringProjectionCanvasBinding ProjectionBinding =>
            m_ProjectionBinding;
        public event Action<Vector2, IReadOnlyList<GraphAuthoringCapabilityDescriptor>> NodeCreationRequested;
        public event Action<
            GraphAuthoringNodeProjection,
            GraphAuthoringChildSurfaceDescriptor> ChildSurfaceRequested;

        public void BindProjection(
            GraphAuthoringProjectionCanvasBinding binding)
        {
            m_ProjectionBinding = binding ??
                throw new ArgumentNullException(nameof(binding));
            if (!m_ProjectionCallbacksBound)
            {
                m_ProjectionCallbacksBound = true;
                RegisterCallback<PointerMoveEvent>(evt =>
                    m_ProjectionPastePosition =
                        contentViewContainer.WorldToLocal(
                            evt.position));
            }
            graphViewChanged = ApplyProjectionGraphViewChange;
            nodeCreationRequest = context =>
                NodeCreationRequested?.Invoke(
                    context.screenMousePosition,
                    m_ProjectionBinding.Capabilities.GetAllowed(
                        m_ProjectionBinding.Document.DomainId,
                        m_ProjectionBinding.Document.DocumentRoleId));
            BindProjectionClipboard();
            PopulateProjection();
        }

        void BindProjectionClipboard()
        {
            GraphAuthoringClipboardController.Bind(
                this,
                () =>
                    m_ProjectionBinding.Document.DomainId.Value,
                elements =>
                {
                    if (m_ProjectionBinding.Clipboard == null)
                        return string.Empty;
                    var selected =
                        new List<GraphAuthoringSelection>();
                    foreach (GraphElement element in elements)
                    {
                        if (element is
                            GraphAuthoringProjectedNodeView node)
                        {
                            selected.Add(
                                new GraphAuthoringSelection(
                                    GraphAuthoringSelectionKind.Node,
                                    node.Projection.NodeId));
                        }
                        else if (element is
                                 GraphAuthoringProjectedEdgeView edge)
                        {
                            selected.Add(
                                new GraphAuthoringSelection(
                                    GraphAuthoringSelectionKind.Edge,
                                    edge.EdgeId));
                        }
                    }
                    return m_ProjectionBinding.Clipboard.Serialize(
                        m_ProjectionBinding.Document,
                        selected);
                },
                payload =>
                    m_ProjectionBinding.Clipboard != null &&
                    m_ProjectionBinding.Clipboard.CanPaste(
                        m_ProjectionBinding.Document,
                        payload),
                (operationName, payload) =>
                {
                    if (m_ProjectionBinding.Clipboard == null)
                    {
                        throw new InvalidOperationException(
                            "Graph authoring clipboard is unavailable for the current document.");
                    }
                    m_ProjectionBinding.Clipboard.Paste(
                        m_ProjectionBinding.Document,
                        operationName,
                        payload,
                        m_ProjectionPastePosition);
                    PopulateProjection();
                });
        }

        public void PopulateProjection()
        {
            if (m_ProjectionBinding == null)
                throw new InvalidOperationException("Graph authoring canvas is not bound.");
            m_PopulatingProjection = true;
            try
            {
                DeleteElements(graphElements.ToList());
                m_ProjectionNodes.Clear();
                IReadOnlyList<GraphAuthoringNodeProjection> nodes =
                    m_ProjectionBinding.Document.Nodes ??
                    Array.Empty<GraphAuthoringNodeProjection>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    GraphAuthoringNodeProjection projection = nodes[i] ?? throw new InvalidOperationException("Graph document contains a missing node projection.");
                    GraphAuthoringCapabilityDescriptor capability = m_ProjectionBinding.Capabilities.Require(
                        projection.CapabilityId,
                        m_ProjectionBinding.Document.DomainId,
                        m_ProjectionBinding.Document.DocumentRoleId);
                    var view = new GraphAuthoringProjectedNodeView(projection, capability);
                    if (!m_ProjectionBinding.PersistsLayout)
                        view.capabilities &= ~Capabilities.Movable;
                    if (capability.ChildSurfaces.Count > 0)
                    {
                        view.RegisterCallback<MouseDownEvent>(evt =>
                        {
                            if (evt.button != 0 || evt.clickCount != 2)
                                return;
                            ChildSurfaceRequested?.Invoke(
                                view.Projection,
                                view.Capability.ChildSurfaces[0]);
                            evt.StopPropagation();
                        });
                    }
                    if (!m_ProjectionNodes.TryAdd(projection.NodeId, view))
                        throw new InvalidOperationException($"Graph document contains duplicate node '{projection.NodeId}'.");
                    AddElement(view);
                }
                IReadOnlyList<GraphAuthoringEdgeProjection> edges =
                    m_ProjectionBinding.Document.Edges ??
                    Array.Empty<GraphAuthoringEdgeProjection>();
                for (int i = 0; i < edges.Count; i++)
                    AddProjectedEdge(edges[i]);
            }
            finally
            {
                m_PopulatingProjection = false;
            }
            SetRuntimeReadOnly(m_ProjectionBinding.Mutation.ReadOnly);
        }

        public IReadOnlyList<GraphAuthoringSearchEntry> Search(string query)
        {
            if (m_ProjectionBinding == null)
                return Array.Empty<GraphAuthoringSearchEntry>();
            string filter = query?.Trim() ?? string.Empty;
            var entries = new List<GraphAuthoringSearchEntry>();
            foreach (GraphAuthoringCapabilityDescriptor capability in
                     m_ProjectionBinding.Capabilities.GetAllowed(
                         m_ProjectionBinding.Document.DomainId,
                         m_ProjectionBinding.Document.DocumentRoleId))
            {
                if (Matches(capability.DisplayName, filter) || Matches(capability.Category, filter) || Matches(capability.CapabilityId.Value, filter))
                    entries.Add(GraphAuthoringSearchEntry.ForCapability(capability));
            }
            foreach (GraphAuthoringNodeProjection node in
                     m_ProjectionBinding.Document.Nodes ??
                     Array.Empty<GraphAuthoringNodeProjection>())
            {
                GraphAuthoringCapabilityDescriptor capability =
                    m_ProjectionBinding.Capabilities.Require(
                        node.CapabilityId,
                        m_ProjectionBinding.Document.DomainId,
                        m_ProjectionBinding.Document.DocumentRoleId);
                if (Matches(node.DisplayName, filter) || Matches(capability.DisplayName, filter) || Matches(node.NodeId.Value, filter))
                    entries.Add(GraphAuthoringSearchEntry.ForNode(node, capability));
            }
            return entries
                .OrderBy(value => value.Group, StringComparer.Ordinal)
                .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
                .ThenBy(value => value.Identity, StringComparer.Ordinal)
                .ToArray();
        }

        public void CreateNode(GraphAuthoringCapabilityId capabilityId, object typedPayload, Vector2 graphPosition)
        {
            if (m_ProjectionBinding == null ||
                m_ProjectionBinding.Mutation.ReadOnly)
                throw new InvalidOperationException("Graph authoring canvas cannot create a node in the current document.");
            m_ProjectionBinding.Capabilities.Require(
                capabilityId,
                m_ProjectionBinding.Document.DomainId,
                m_ProjectionBinding.Document.DocumentRoleId);
            m_ProjectionBinding.Mutation.Apply(
                m_ProjectionBinding.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateNode,
                    capabilityId: capabilityId,
                    value: typedPayload,
                    position: graphPosition));
            PopulateProjection();
        }

        static bool Matches(string value, string filter) =>
            string.IsNullOrEmpty(filter) || (!string.IsNullOrEmpty(value) && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

        List<Port> GetProjectionCompatiblePorts(
            Port startPort)
        {
            if (m_ProjectionBinding == null ||
                m_ProjectionBinding.Mutation.ReadOnly ||
                !(startPort is GraphAuthoringProjectedPortView source))
                return new List<Port>();
            return ports
                .OfType<GraphAuthoringProjectedPortView>()
                .Where(target => !ReferenceEquals(source.NodeView, target.NodeView) &&
                                 source.direction != target.direction &&
                                 string.Equals(source.ValueTypeId, target.ValueTypeId, StringComparison.Ordinal) &&
                                 CanConnect(source, target))
                .Cast<Port>()
                .ToList();
        }

        bool CanConnect(
            GraphAuthoringProjectedPortView first,
            GraphAuthoringProjectedPortView second)
        {
            GraphAuthoringProjectedPortView output =
                first.direction == Direction.Output ? first : second;
            GraphAuthoringProjectedPortView input =
                first.direction == Direction.Input ? first : second;
            return m_ProjectionBinding.ConnectionPolicy.CanConnect(
                m_ProjectionBinding.Document,
                output.NodeView.Projection,
                output.PortId,
                input.NodeView.Projection,
                input.PortId);
        }

        IReadOnlyList<GraphAuthoringSelection>
            GetProjectionStableSelection()
        {
            var result = new List<GraphAuthoringSelection>();
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is GraphAuthoringProjectedNodeView node)
                    result.Add(new GraphAuthoringSelection(GraphAuthoringSelectionKind.Node, node.Projection.NodeId));
                else if (selection[i] is GraphAuthoringProjectedEdgeView edge)
                    result.Add(new GraphAuthoringSelection(GraphAuthoringSelectionKind.Edge, edge.EdgeId));
            }
            return result;
        }

        void FocusProjectionElement(
            GraphAuthoringElementId elementId)
        {
            if (!m_ProjectionNodes.TryGetValue(
                    elementId,
                    out GraphAuthoringProjectedNodeView node))
                return;
            ClearSelection();
            AddToSelection(node);
            FrameSelection();
        }

        void AddProjectedEdge(GraphAuthoringEdgeProjection projection)
        {
            if (!m_ProjectionNodes.TryGetValue(
                    projection.SourceNodeId,
                    out GraphAuthoringProjectedNodeView sourceNode))
                throw new InvalidOperationException($"Edge '{projection.EdgeId}' source node '{projection.SourceNodeId}' is missing.");
            if (!m_ProjectionNodes.TryGetValue(
                    projection.TargetNodeId,
                    out GraphAuthoringProjectedNodeView targetNode))
                throw new InvalidOperationException($"Edge '{projection.EdgeId}' target node '{projection.TargetNodeId}' is missing.");
            GraphAuthoringProjectedPortView source =
                sourceNode.RequirePort(projection.SourcePortId);
            GraphAuthoringProjectedPortView target =
                targetNode.RequirePort(projection.TargetPortId);
            var edge = new GraphAuthoringProjectedEdgeView(
                projection.EdgeId)
            {
                output = source.direction == Direction.Output ? source : target,
                input = source.direction == Direction.Input ? source : target
            };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            edge.BindProjection(projection);
            AddElement(edge);
        }

        GraphViewChange ApplyProjectionGraphViewChange(
            GraphViewChange change)
        {
            if (m_PopulatingProjection ||
                m_ProjectionBinding == null)
                return change;
            if (m_ProjectionBinding.Mutation.ReadOnly)
            {
                change.edgesToCreate = null;
                change.elementsToRemove = new List<GraphElement>();
                change.movedElements = null;
                return change;
            }

            var requests = new List<GraphAuthoringMutationRequest>();
            if (change.elementsToRemove != null)
            {
                foreach (GraphAuthoringProjectedEdgeView edge in
                         change.elementsToRemove.OfType<
                             GraphAuthoringProjectedEdgeView>())
                    requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DisconnectEdge, edge.EdgeId));
                foreach (GraphAuthoringProjectedNodeView node in
                         change.elementsToRemove.OfType<
                             GraphAuthoringProjectedNodeView>())
                    requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DeleteElement, node.Projection.NodeId));
            }
            if (m_ProjectionBinding.PersistsLayout &&
                change.movedElements != null)
            {
                foreach (GraphAuthoringProjectedNodeView node in
                         change.movedElements.OfType<
                             GraphAuthoringProjectedNodeView>())
                {
                    requests.Add(new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.MoveElement,
                        node.Projection.NodeId,
                        position: node.GetPosition().position));
                }
            }
            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    if (!(edge.output is GraphAuthoringProjectedPortView output) ||
                        !(edge.input is GraphAuthoringProjectedPortView input))
                        throw new InvalidOperationException("Graph authoring edge endpoints are not shared authoring ports.");
                    requests.Add(new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.ConnectPorts,
                        sourceNodeId: output.NodeView.Projection.NodeId,
                        sourcePortId: output.PortId,
                        targetNodeId: input.NodeView.Projection.NodeId,
                        targetPortId: input.PortId));
                }
            }
            if (requests.Count > 0)
                m_ProjectionBinding.Mutation.Apply(
                    m_ProjectionBinding.Document,
                    requests);
            return change;
        }
    }

    public readonly struct GraphAuthoringSearchEntry
    {
        GraphAuthoringSearchEntry(string identity, string displayName, string group, GraphAuthoringCapabilityId capabilityId, GraphAuthoringElementId elementId)
        {
            Identity = identity;
            DisplayName = displayName;
            Group = group;
            CapabilityId = capabilityId;
            ElementId = elementId;
        }

        public string Identity { get; }
        public string DisplayName { get; }
        public string Group { get; }
        public GraphAuthoringCapabilityId CapabilityId { get; }
        public GraphAuthoringElementId ElementId { get; }
        public bool IsCreateCommand => CapabilityId.IsValid && !ElementId.IsValid;

        internal static GraphAuthoringSearchEntry ForCapability(GraphAuthoringCapabilityDescriptor capability) =>
            new GraphAuthoringSearchEntry("create/" + capability.CapabilityId.Value, capability.DisplayName, "Create/" + capability.Category, capability.CapabilityId, default);

        internal static GraphAuthoringSearchEntry ForNode(GraphAuthoringNodeProjection node, GraphAuthoringCapabilityDescriptor capability) =>
            new GraphAuthoringSearchEntry("node/" + node.NodeId.Value, string.IsNullOrWhiteSpace(node.DisplayName) ? capability.DisplayName : node.DisplayName, "Document", node.CapabilityId, node.NodeId);
    }
}




