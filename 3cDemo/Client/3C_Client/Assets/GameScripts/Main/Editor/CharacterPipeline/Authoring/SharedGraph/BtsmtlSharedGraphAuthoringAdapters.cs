using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using GraphElement =
    UnityEditor.Experimental.GraphView.GraphElement;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class BtsmtlSharedAuthoringWorkspaceInstallation
    {
        static BtsmtlSharedAuthoringWorkspaceInstallation()
        {
            BtsmtlSharedAuthoringWorkspaceRegistry.Register(
                new BtsmtlSharedAuthoringWorkspaceFactory());
        }
    }

    sealed class BtsmtlSharedAuthoringWorkspaceFactory :
        IBtsmtlSharedAuthoringWorkspaceFactory
    {
        public BtsmtlSharedAuthoringWorkspaceBinding Create(
            BaseTreeWindow window,
            BaseTree graph,
            bool readOnly)
        {
            var catalog = new BtsmtlGraphAuthoringCapabilities();
            var document = new BtsmtlSharedGraphDocument(graph, catalog);
            var mutation = new BtsmtlSharedGraphMutation(
                catalog,
                window.ResolveVisibleTrees)
            {
                ReadOnly = readOnly
            };
            var navigator =
                new BtsmtlSharedGraphNavigatorDataSource(window);
            var diagnostics =
                new BtsmtlSharedGraphDiagnosticsProjection(catalog);
            if (!(graph is StateMachineGraph stateMachine))
            {
                return new BtsmtlSharedAuthoringWorkspaceBinding(
                    graph,
                    document,
                    catalog.SharedCatalog,
                    mutation,
                    new BtsmtlSharedGraphConnectionPolicy(),
                    new BtsmtlSharedGraphDetailsDataSource(
                        diagnostics),
                    navigator,
                    diagnostics,
                    new BtsmtlSharedGraphClipboardCodec(
                        mutation,
                        catalog),
                    setReadOnly: value =>
                        mutation.ReadOnly = value);
            }
            var stateDocument = new BtsmtlStateMachineDocument(
                stateMachine,
                GraphAuthoringFingerprint.Compute(stateMachine));
            var stateMutation = new BtsmtlStateMachineMutationAdapter
            {
                ReadOnly = readOnly
            };
            var statePolicy = new BtsmtlStateMachinePolicy(
                state => OpenState(window, state),
                edge => OpenTransition(window, edge));
            return new BtsmtlSharedAuthoringWorkspaceBinding(
                graph,
                document,
                catalog.SharedCatalog,
                mutation,
                new BtsmtlSharedGraphConnectionPolicy(),
                new BtsmtlSharedGraphDetailsDataSource(
                    diagnostics),
                navigator,
                diagnostics,
                new BtsmtlSharedGraphClipboardCodec(
                    mutation,
                    catalog),
                setReadOnly: value =>
                {
                    mutation.ReadOnly = value;
                    stateMutation.ReadOnly = value;
                },
                stateMachineDocument: stateDocument,
                stateMachineMutation: stateMutation,
                stateMachinePolicy: statePolicy,
                stateMachineDetails:
                new BtsmtlStateMachineDetailsDataSource());
        }

        static void OpenState(
            BaseTreeWindow window,
            StateNode state)
        {
            NodeGraphReference reference = state.GetGraphReferences()
                .Single(value =>
                    ReferenceEquals(value.Tree, state.SubTree));
            window.PushReferencedTree(state, reference);
        }

        static void OpenTransition(
            BaseTreeWindow window,
            BaseEdge edge)
        {
            ConditionRuleGraph graph = edge.ConditionRuleGraph ??
                throw new InvalidOperationException(
                    $"BTSMTL Transition '{edge.GUID}' has no resolved Condition Rule.");
            window.PushReferencedTree(
                edge,
                graph,
                "Condition Rule");
        }
    }

    sealed class BtsmtlSharedGraphNavigatorDataSource :
        IGraphAuthoringNavigatorDataSource
    {
        static readonly GraphAuthoringCommandId OpenReference =
            new GraphAuthoringCommandId("btsmtl.open-reference");
        readonly BaseTreeWindow m_Window;

        public BtsmtlSharedGraphNavigatorDataSource(
            BaseTreeWindow window)
        {
            m_Window = window ??
                       throw new ArgumentNullException(nameof(window));
        }

        public IReadOnlyList<GraphAuthoringNavigatorItem> GetItems(
            IGraphAuthoringDocumentProjection document)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            var result = new List<GraphAuthoringNavigatorItem>();
            foreach (BaseNode node in btsmtl.Graph.Nodes
                         .Where(value => value != null)
                         .OrderBy(value =>
                             value.ResolvedDisplayName,
                             StringComparer.Ordinal)
                         .ThenBy(value =>
                             value.GUID,
                             StringComparer.Ordinal))
            {
                foreach (NodeGraphReference reference in
                         node.GetGraphReferences())
                {
                    if (reference.Tree == null)
                        continue;
                    result.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId(
                            node.GUID + "/" + reference.Key),
                        "Graph References",
                        string.IsNullOrWhiteSpace(reference.Label)
                            ? reference.Tree.name
                            : reference.Label,
                        node.GUID,
                        reference.Tree.GraphAuthoringId,
                        OpenReference,
                        $"{node.ResolvedDisplayName} {reference.Tree.name}"));
                }
            }
            foreach (BaseExposedProperty declaration in
                     btsmtl.Graph.ExposedProperties
                         .Where(value => value != null)
                         .OrderBy(
                             value => value.BlackboardKey,
                             StringComparer.Ordinal))
            {
                result.Add(new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId(
                        "blackboard/" + declaration.DeclarationId),
                    "Blackboard",
                    declaration.BlackboardKey,
                    btsmtl.Graph.GraphAuthoringId,
                    declaration.DeclarationId,
                    default,
                    declaration.ValueType?.Name ?? string.Empty));
            }
            return result;
        }

        public void Open(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringNavigatorItem item)
        {
            if (!item.OpenCommandId.Equals(OpenReference))
                return;
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            BaseNode node = btsmtl.Graph.Nodes.Single(value =>
                value != null && value.GUID == item.OwnerId);
            NodeGraphReference reference = node.GetGraphReferences()
                .Single(value =>
                    value.Tree != null &&
                    value.Tree.GraphAuthoringId == item.ReferenceId);
            m_Window.PushReferencedTree(node, reference);
        }

        static BtsmtlSharedGraphDocument Require(
            IGraphAuthoringDocumentProjection document) =>
            document as BtsmtlSharedGraphDocument ??
            throw new ArgumentException(
                "BTSMTL Navigator requires the shared BTSMTL document.",
                nameof(document));
    }

    sealed class BtsmtlSharedGraphDiagnosticsProjection :
        IGraphAuthoringDomainDiagnostics,
        IDisposable
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;
        readonly Dictionary<string, RuntimeDebugViewBinding> m_Bindings =
            new Dictionary<string, RuntimeDebugViewBinding>(
                StringComparer.Ordinal);

        public BtsmtlSharedGraphDiagnosticsProjection(
            BtsmtlGraphAuthoringCapabilities catalog)
        {
            m_Catalog = catalog ??
                        throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<GraphAuthoringDiagnosticProjection>
            GetDiagnostics(
                IGraphAuthoringDocumentProjection document)
        {
            BtsmtlSharedGraphDocument btsmtl =
                document as BtsmtlSharedGraphDocument ??
                throw new ArgumentException(
                    "BTSMTL diagnostics require the shared BTSMTL document.",
                    nameof(document));
            var result =
                new List<GraphAuthoringDiagnosticProjection>();
            var identities = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (BaseNode node in btsmtl.Graph.Nodes)
            {
                if (node == null)
                {
                    result.Add(Diagnostic(
                        "missing-node",
                        "Graph contains a missing Node."));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(node.GUID) ||
                    !identities.Add(node.GUID))
                {
                    result.Add(Diagnostic(
                        "invalid-node-identity",
                        $"Node '{node.ResolvedDisplayName}' has a missing or duplicate identity."));
                    continue;
                }
                if (!m_Catalog.TryGetSharedCapability(node, out _))
                {
                    result.Add(Diagnostic(
                        "missing-node-capability",
                        $"Node type '{node.GetType().FullName}' has no formal capability.",
                        new GraphAuthoringElementId(node.GUID)));
                }
            }
            foreach (BaseEdge edge in btsmtl.Graph.Edges
                         .Cast<BaseEdge>()
                         .Concat(btsmtl.Graph.PropertyEdges))
            {
                if (edge == null ||
                    string.IsNullOrWhiteSpace(edge.GUID) ||
                    !identities.Add(edge.GUID))
                {
                    result.Add(Diagnostic(
                        "invalid-edge-identity",
                        "Graph contains an Edge with a missing or duplicate identity."));
                    continue;
                }
                if (!identities.Contains(edge.StartNodeGUID) ||
                    !identities.Contains(edge.EndNodeGUID))
                {
                    result.Add(Diagnostic(
                        "missing-edge-endpoint",
                        $"Edge '{edge.GUID}' has a missing endpoint.",
                        new GraphAuthoringElementId(edge.GUID)));
                }
            }
            return result;
        }

        public IReadOnlyList<GraphAuthoringRuntimeTraceProjection>
            GetRuntimeTrace(
                IGraphAuthoringDocumentProjection document)
        {
            BtsmtlSharedGraphDocument btsmtl =
                document as BtsmtlSharedGraphDocument ??
                throw new ArgumentException(
                    "BTSMTL runtime trace requires the shared BTSMTL document.",
                    nameof(document));
            if (!m_Bindings.TryGetValue(
                    document.DocumentId,
                    out RuntimeDebugViewBinding binding))
            {
                binding = new RuntimeDebugViewBinding(
                    RuntimeDebugViewKind.Graph);
                m_Bindings.Add(document.DocumentId, binding);
            }
            binding.Configure(new RuntimeDebugTargetRequest(
                RuntimeSourceElementKey.Graph(document.DocumentId),
                document.ContentRevision));
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugTargetResolution resolution = binding.Refresh(
                session,
                RuntimeTraceChannel.Graph |
                RuntimeTraceChannel.StateMachine);
            RuntimeDebugViewModel view = session.ViewModel;
            if (!resolution.CanReadSnapshot ||
                !view.Valid ||
                !binding.SelectedInstance.IsValid)
                return Array.Empty<
                    GraphAuthoringRuntimeTraceProjection>();
            var identities = new HashSet<string>(
                btsmtl.Graph.Nodes
                    .Where(value => value != null)
                    .Select(value => value.GUID)
                    .Concat(btsmtl.Graph.Edges
                        .Where(value => value != null)
                        .Select(value => value.GUID))
                    .Concat(btsmtl.Graph.PropertyEdges
                        .Where(value => value != null)
                        .Select(value => value.GUID)),
                StringComparer.Ordinal);
            return view.GetGraphStates(
                    document.DocumentId,
                    binding.SelectedInstance,
                    false)
                .Where(value =>
                    identities.Contains(
                        value.Source.ElementAuthoringId))
                .OrderBy(value =>
                    value.Source.ElementAuthoringId,
                    StringComparer.Ordinal)
                .Select(value =>
                    new GraphAuthoringRuntimeTraceProjection(
                        new GraphAuthoringElementId(
                            value.Source.ElementAuthoringId),
                        value.Status,
                        $"{value.Kind} · {value.Domain} {value.Position} · seq {value.Sequence}",
                        document.ContentRevision))
                .ToArray();
        }

        public void Dispose()
        {
            foreach (RuntimeDebugViewBinding binding in
                     m_Bindings.Values)
                binding.Dispose(RuntimeDebugSession.Shared);
            m_Bindings.Clear();
        }

        static GraphAuthoringDiagnosticProjection Diagnostic(
            string code,
            string message,
            GraphAuthoringElementId elementId = default) =>
            new GraphAuthoringDiagnosticProjection(
                code,
                GraphAuthoringDiagnosticSeverity.Error,
                message,
                elementId);
    }

    public sealed class BtsmtlSharedGraphDocument :
        IGraphAuthoringDocumentProjection
    {
        readonly BaseGraph m_Graph;
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;

        public BtsmtlSharedGraphDocument(
            BaseGraph graph,
            BtsmtlGraphAuthoringCapabilities catalog = null)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_Catalog = catalog ??
                        new BtsmtlGraphAuthoringCapabilities();
            if (string.IsNullOrWhiteSpace(graph.GraphAuthoringId))
                throw new InvalidOperationException(
                    $"BTSMTL Graph '{graph.name}' has no authoring identity.");
        }

        public GraphAuthoringDomainId DomainId =>
            BtsmtlGraphAuthoringCapabilities.SharedDomain;
        public GraphAuthoringDocumentRoleId DocumentRoleId =>
            BtsmtlGraphAuthoringCapabilities.SharedRoleId(m_Graph);
        public string DocumentId => m_Graph.GraphAuthoringId;
        public string DisplayName => m_Graph.name;
        public string ContentRevision =>
            GraphAuthoringFingerprint.Compute(m_Graph);
        public UnityEngine.Object SerializedOwner => m_Graph.SerializedOwner;
        public IReadOnlyList<GraphAuthoringPageProjection> Pages => new[]
        {
            new GraphAuthoringPageProjection(
                new GraphAuthoringElementId(DocumentId),
                DisplayName,
                DocumentRoleId.Value)
        };
        public IReadOnlyList<GraphAuthoringNodeProjection> Nodes =>
            ProjectNodes();
        public IReadOnlyList<GraphAuthoringEdgeProjection> Edges =>
            ProjectEdges();
        public BaseGraph Graph => m_Graph;
        public BtsmtlGraphAuthoringCapabilities Catalog =>
            m_Catalog;

        IReadOnlyList<GraphAuthoringNodeProjection> ProjectNodes()
        {
            var result = new List<GraphAuthoringNodeProjection>();
            foreach (BaseNode node in m_Graph.Nodes
                         .Where(value => value != null)
                         .OrderBy(value => value.GUID, StringComparer.Ordinal))
            {
                if (!m_Catalog.TryGetSharedCapability(
                        node,
                        out GraphAuthoringCapabilityId capabilityId))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Node type '{node.GetType().FullName}' has no formal capability.");
                }
                GraphAuthoringCapabilityDescriptor capability =
                    m_Catalog.SharedCatalog.Require(
                        capabilityId,
                        DomainId,
                        DocumentRoleId);
                result.Add(new GraphAuthoringNodeProjection(
                    Element(node.GUID, "Node"),
                    capabilityId,
                    node.ResolvedDisplayName,
                    node.Position,
                    ProjectDynamicPorts(node, capability)));
            }
            return result;
        }

        IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectDynamicPorts(
            BaseNode node,
            GraphAuthoringCapabilityDescriptor capability)
        {
            var fixedPorts = new HashSet<GraphAuthoringPortId>(
                capability.FixedPorts.Select(value => value.PortId));
            var result = new List<GraphAuthoringDynamicPortProjection>();
            int order = capability.FixedPorts.Count;
            foreach (FlowPortDeclaration port in
                     node.GetSupportedFlowPortDeclarations(m_Graph))
            {
                GraphAuthoringPortId id = BtsmtlSharedGraphPort.Flow(port.Name);
                if (fixedPorts.Contains(id))
                    continue;
                result.Add(new GraphAuthoringDynamicPortProjection(
                    id,
                    port.Name,
                    BtsmtlSharedGraphPort.FlowValueType,
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    BtsmtlSharedGraphPort.Capacity(port.Capacity),
                    port.Direction == PortDirection.Input,
                    order++));
            }
            foreach (PropertyPort port in node.PropertyPortMap.Values
                         .Where(value => value != null)
                         .OrderBy(value => value.Index)
                         .ThenBy(value => value.PortId, StringComparer.Ordinal))
            {
                GraphAuthoringPortId id =
                    BtsmtlSharedGraphPort.Property(port.PortId);
                if (fixedPorts.Contains(id))
                    continue;
                result.Add(new GraphAuthoringDynamicPortProjection(
                    id,
                    port.DisplayName,
                    BtsmtlSharedGraphPort.PropertyValueType,
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    port.Direction == PortDirection.Input
                        ? GraphAuthoringPortCapacity.Single
                        : GraphAuthoringPortCapacity.Multiple,
                    port.Direction == PortDirection.Input,
                    order++));
            }
            return result;
        }

        IReadOnlyList<GraphAuthoringEdgeProjection> ProjectEdges()
        {
            var result = new List<GraphAuthoringEdgeProjection>();
            foreach (BaseEdge edge in m_Graph.Edges
                         .Where(value => value != null)
                         .OrderBy(value => value.GUID, StringComparer.Ordinal))
            {
                result.Add(new GraphAuthoringEdgeProjection(
                    Element(edge.GUID, "Flow edge"),
                    Element(edge.StartNodeGUID, "Flow edge source"),
                    BtsmtlSharedGraphPort.Flow(edge.StartPortName),
                    Element(edge.EndNodeGUID, "Flow edge target"),
                    BtsmtlSharedGraphPort.Flow(edge.EndPortName)));
            }
            foreach (PropertyEdge edge in m_Graph.PropertyEdges
                         .Where(value => value != null)
                         .OrderBy(value => value.GUID, StringComparer.Ordinal))
            {
                result.Add(new GraphAuthoringEdgeProjection(
                    Element(edge.GUID, "Property edge"),
                    Element(edge.StartNodeGUID, "Property edge source"),
                    BtsmtlSharedGraphPort.Property(edge.StartPortName),
                    Element(edge.EndNodeGUID, "Property edge target"),
                    BtsmtlSharedGraphPort.Property(edge.EndPortName)));
            }
            return result;
        }

        static GraphAuthoringElementId Element(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{label} identity is missing.");
            return new GraphAuthoringElementId(value);
        }
    }

    public sealed class BtsmtlSharedGraphConnectionPolicy :
        IGraphAuthoringConnectionPolicy
    {
        public bool CanConnect(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringNodeProjection sourceNode,
            GraphAuthoringPortId sourcePortId,
            GraphAuthoringNodeProjection targetNode,
            GraphAuthoringPortId targetPortId)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            BaseNode source = Node(btsmtl.Graph, sourceNode.NodeId);
            BaseNode target = Node(btsmtl.Graph, targetNode.NodeId);
            if (!BtsmtlSharedGraphPort.TryParse(
                    sourcePortId,
                    out bool sourceProperty,
                    out string sourcePort) ||
                !BtsmtlSharedGraphPort.TryParse(
                    targetPortId,
                    out bool targetProperty,
                    out string targetPort) ||
                sourceProperty != targetProperty)
                return false;
            return sourceProperty
                ? CanConnectProperty(
                    btsmtl.Graph,
                    source,
                    sourcePort,
                    target,
                    targetPort)
                : CanConnectFlow(
                    btsmtl.Graph,
                    source,
                    sourcePort,
                    target,
                    targetPort);
        }

        static bool CanConnectFlow(
            BaseGraph graph,
            BaseNode source,
            string sourcePort,
            BaseNode target,
            string targetPort)
        {
            FlowPortDeclaration? output = Declaration(
                source,
                graph,
                sourcePort,
                PortDirection.Output);
            FlowPortDeclaration? input = Declaration(
                target,
                graph,
                targetPort,
                PortDirection.Input);
            if (!output.HasValue || !input.HasValue)
                return false;
            if (graph.Edges.Any(value =>
                    value != null &&
                    value.StartNodeGUID == source.GUID &&
                    value.EndNodeGUID == target.GUID &&
                    value.StartPortName == sourcePort &&
                    value.EndPortName == targetPort))
                return false;
            if (output.Value.Capacity == PortCapacity.Single &&
                graph.Edges.Any(value =>
                    value != null &&
                    value.StartNodeGUID == source.GUID &&
                    value.StartPortName == sourcePort))
                return false;
            if (input.Value.Capacity == PortCapacity.Single &&
                graph.Edges.Any(value =>
                    value != null &&
                    value.EndNodeGUID == target.GUID &&
                    value.EndPortName == targetPort))
                return false;
            if (!(graph is StateMachineGraph))
                return true;
            return sourcePort == StateMachinePorts.StateOut &&
                   targetPort == StateMachinePorts.StateIn &&
                   (source is StateMachineEnterNode && target is StateNode ||
                    source is StateMachineAnyStateNode &&
                    (target is StateNode || target is StateMachineExitNode) ||
                    source is StateNode &&
                    (target is StateNode || target is StateMachineExitNode));
        }

        static bool CanConnectProperty(
            BaseGraph graph,
            BaseNode source,
            string sourcePortId,
            BaseNode target,
            string targetPortId)
        {
            if (!source.PropertyPortMap.TryGetValue(
                    sourcePortId,
                    out PropertyPort sourcePort) ||
                !target.PropertyPortMap.TryGetValue(
                    targetPortId,
                    out PropertyPort targetPort) ||
                sourcePort.Direction != PortDirection.Output ||
                targetPort.Direction != PortDirection.Input ||
                sourcePort.ValueType == null ||
                graph.PropertyEdges.Any(value =>
                    value != null &&
                    value.EndNodeGUID == target.GUID &&
                    value.EndPortName == targetPortId))
                return false;
            if (targetPort.ValueType == null)
                return AcceptsVariableType(
                    target,
                    targetPort,
                    sourcePort.ValueType);
            if (targetPort.ValueType == sourcePort.ValueType ||
                targetPort.ValueType.IsAssignableFrom(sourcePort.ValueType))
                return true;
            CompatiblePortsAttribute compatible =
                targetPort.GetType().GetCustomAttribute<
                    CompatiblePortsAttribute>(true);
            return compatible != null &&
                   compatible.CompatibleTypes.Contains(sourcePort.ValueType);
        }

        static bool AcceptsVariableType(
            BaseNode node,
            PropertyPort port,
            Type sourceType)
        {
            NodeFieldAccessor? accessor =
                node.FindFieldAccessor(port.FieldKey);
            VariablePropertyPortAttribute attribute =
                accessor?.GetAttribute<VariablePropertyPortAttribute>();
            if (attribute == null)
                return false;
            IEnumerable<Type> types = attribute.AcceptableTypes ??
                                      ResolveAcceptableTypes(
                                          accessor.Value,
                                          attribute.AcceptableTypesMethodName,
                                          port.FieldKey);
            return types != null &&
                   types.Any(value => IsAssignable(value, sourceType));
        }

        static IEnumerable<Type> ResolveAcceptableTypes(
            NodeFieldAccessor accessor,
            string methodName,
            string fieldKey)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                return Array.Empty<Type>();
            MethodInfo method = accessor.TargetObject.GetType().GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            return method?.Invoke(
                       accessor.TargetObject,
                       new object[] { fieldKey }) as IEnumerable<Type> ??
                   Array.Empty<Type>();
        }

        static bool IsAssignable(Type accepted, Type source)
        {
            if (accepted == null || source == null)
                return false;
            if (!accepted.IsGenericTypeDefinition)
                return accepted.IsAssignableFrom(source);
            for (Type current = source;
                 current != null;
                 current = current.BaseType)
            {
                Type candidate = current.IsGenericType
                    ? current.GetGenericTypeDefinition()
                    : current;
                if (candidate == accepted)
                    return true;
            }
            return source.GetInterfaces().Any(value =>
                value.IsGenericType &&
                value.GetGenericTypeDefinition() == accepted);
        }

        static FlowPortDeclaration? Declaration(
            BaseNode node,
            BaseGraph graph,
            string name,
            PortDirection direction)
        {
            foreach (FlowPortDeclaration declaration in
                     node.GetSupportedFlowPortDeclarations(graph))
            {
                if (declaration.Name == name &&
                    declaration.Direction == direction)
                    return declaration;
            }
            return null;
        }

        static BaseNode Node(
            BaseGraph graph,
            GraphAuthoringElementId nodeId) =>
            graph.Nodes.SingleOrDefault(value =>
                value != null && value.GUID == nodeId.Value) ??
            throw new InvalidOperationException(
                $"BTSMTL Node '{nodeId}' is missing.");

        static BtsmtlSharedGraphDocument Require(
            IGraphAuthoringDocumentProjection document) =>
            document as BtsmtlSharedGraphDocument ??
            throw new ArgumentException(
                "BTSMTL connection policy requires the shared BTSMTL document.",
                nameof(document));
    }

    public sealed class BtsmtlSharedGraphMutation :
        IGraphAuthoringDomainMutation
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;
        readonly Func<IReadOnlyList<BaseTree>> m_VisibleTrees;

        public BtsmtlSharedGraphMutation(
            BtsmtlGraphAuthoringCapabilities catalog = null,
            Func<IReadOnlyList<BaseTree>> visibleTrees = null)
        {
            m_Catalog = catalog ??
                        new BtsmtlGraphAuthoringCapabilities();
            m_VisibleTrees = visibleTrees;
        }

        public bool ReadOnly { get; set; }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request) =>
            Apply(document, new[] { request });

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "BTSMTL Graph document is read-only.");
            BtsmtlSharedGraphDocument btsmtl =
                document as BtsmtlSharedGraphDocument ??
                throw new ArgumentException(
                    "BTSMTL mutation requires the shared BTSMTL document.",
                    nameof(document));
            if (btsmtl.Graph.SerializedOwner == null)
                throw new InvalidOperationException(
                    "BTSMTL Graph has no writable serialized owner.");
            GraphAuthoringMutationRequest[] values =
                (requests ?? throw new ArgumentNullException(nameof(requests)))
                .ToArray();
            btsmtl.Graph.ApplyModify(
                OperationName(values),
                () =>
                {
                    for (int i = 0; i < values.Length; i++)
                        ApplyInsideTransaction(btsmtl.Graph, values[i]);
                });
        }

        void ApplyInsideTransaction(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            switch (request.Kind)
            {
                case GraphAuthoringMutationKind.CreateNode:
                    CreateNode(graph, request);
                    return;
                case GraphAuthoringMutationKind.DeleteElement:
                    DeleteNode(graph, Node(graph, request.TargetId));
                    return;
                case GraphAuthoringMutationKind.MoveElement:
                    MoveElement(graph, request);
                    return;
                case GraphAuthoringMutationKind.CreateGroup:
                    CreateGroup(graph, request);
                    return;
                case GraphAuthoringMutationKind.DeleteGroup:
                    graph.DeleteNodeGroup(
                        RequireGroup(request));
                    return;
                case GraphAuthoringMutationKind.CreateStack:
                    CreateStack(graph, request);
                    return;
                case GraphAuthoringMutationKind.DeleteStack:
                    graph.DeleteStackNode(
                        RequireStack(graph, request));
                    return;
                case GraphAuthoringMutationKind.ConnectPorts:
                    Connect(graph, request);
                    return;
                case GraphAuthoringMutationKind.DisconnectEdge:
                    Disconnect(graph, request.TargetId);
                    return;
                case GraphAuthoringMutationKind.SetField:
                    BtsmtlSharedNodeFieldBinding.Set(
                        graph,
                        Node(graph, request.TargetId),
                        request.FieldId,
                        request.Value,
                        m_VisibleTrees?.Invoke());
                    return;
                case GraphAuthoringMutationKind.SetDisplayName:
                    Node(graph, request.TargetId).DisplayName =
                        request.Value?.ToString()?.Trim() ??
                        string.Empty;
                    return;
                case GraphAuthoringMutationKind.ExecuteCommand:
                {
                    BaseNode node = Node(graph, request.TargetId);
                    if (!m_Catalog.TryGetSharedCapability(
                            node,
                            out GraphAuthoringCapabilityId capabilityId) ||
                        !m_Catalog.SharedCatalog
                            .Require(capabilityId)
                            .Commands.Any(value =>
                                value.CommandId.Equals(
                                    request.CommandId)))
                    {
                        throw new InvalidOperationException(
                            $"BTSMTL Node '{node.GetType().Name}' does not declare command '{request.CommandId}'.");
                    }
                    BtsmtlSharedNodeCommandBinding.Execute(
                        node,
                        request.CommandId,
                        request.Value);
                    return;
                }
                case GraphAuthoringMutationKind.SetTransitionField:
                    BtsmtlTransitionMutation.Apply(
                        graph,
                        graph.Edges.SingleOrDefault(value =>
                            value != null &&
                            value.GUID ==
                            request.TargetId.Value) ??
                        throw new InvalidOperationException(
                            $"BTSMTL Edge '{request.TargetId}' is missing."),
                        request.FieldId,
                        request.Value);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"BTSMTL Graph mutation '{request.Kind}' has no formal handler.");
            }
        }

        void CreateNode(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            if (!m_Catalog.TryResolveSharedCapability(
                    request.CapabilityId,
                    out Type type) ||
                !graph.CanCreateNodeType(type))
            {
                throw new InvalidOperationException(
                    $"Capability '{request.CapabilityId}' cannot create a Node in '{graph.name}'.");
            }
            if (request.Value is BtsmtlSharedNodeCreationPayload creation)
            {
                BaseNode pasted = creation.Node;
                if (pasted == null || pasted.GetType() != type ||
                    pasted.Single &&
                    graph.Nodes.Any(value =>
                        value != null &&
                        value.GetType() == pasted.GetType()))
                {
                    throw new InvalidOperationException(
                        $"Capability '{request.CapabilityId}' received an invalid pasted Node.");
                }
                pasted.Position = request.Position;
                pasted.Refresh();
                graph.AddNode(pasted);
                return;
            }
            BaseNode node = graph.CreateNode(type);
            node.Position = request.Position;
            if (request.Value is IBtsmtlNodeCreationPayload
                configuration)
            {
                if (configuration.NodeType != type)
                {
                    throw new InvalidOperationException(
                        $"Capability '{request.CapabilityId}' received a mismatched creation payload.");
                }
                configuration.Configure(node);
            }
            if (request.Value is string displayName &&
                !string.IsNullOrWhiteSpace(displayName))
                node.DisplayName = displayName.Trim();
        }

        static void CreateGroup(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            NodeGroup group =
                request.Value is BtsmtlNodeGroupMutationPayload
                    payload
                    ? payload.Group
                    : graph.CreateNodeGroup();
            if (!graph.NodeGroups.Contains(group))
                graph.NodeGroups.Add(group);
            group.Position = request.Position;
        }

        static void CreateStack(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            StackNode stack =
                request.Value is BtsmtlStackMutationPayload
                    payload
                    ? payload.Stack
                    : graph.CreateStackNode();
            if (!graph.StackNodes.Contains(stack))
                graph.StackNodes.Add(stack);
            stack.Position = request.Position;
        }

        static void MoveElement(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            BaseNode node = graph.Nodes.SingleOrDefault(value =>
                value != null &&
                value.GUID == request.TargetId.Value);
            if (node != null)
            {
                node.Position = request.Position;
                node.OnMoved();
                return;
            }
            StackNode stack = graph.StackNodes.SingleOrDefault(value =>
                value != null &&
                value.GUID == request.TargetId.Value);
            if (stack != null)
            {
                stack.Position = request.Position;
                return;
            }
            NodeGroup group = RequireGroup(request);
            group.Position = request.Position;
        }

        static NodeGroup RequireGroup(
            GraphAuthoringMutationRequest request) =>
            request.Value is BtsmtlNodeGroupMutationPayload payload
                ? payload.Group
                : throw new InvalidOperationException(
                    $"BTSMTL mutation '{request.Kind}' requires a NodeGroup payload.");

        static StackNode RequireStack(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            if (request.Value is BtsmtlStackMutationPayload payload)
                return payload.Stack;
            return graph.StackNodes.SingleOrDefault(value =>
                       value != null &&
                       value.GUID == request.TargetId.Value) ??
                   throw new InvalidOperationException(
                       $"BTSMTL Stack '{request.TargetId}' is missing.");
        }

        static void DeleteNode(BaseGraph graph, BaseNode node)
        {
            foreach (PropertyEdge edge in graph.PropertyEdges
                         .Where(value =>
                             value != null &&
                             (value.StartNodeGUID == node.GUID ||
                              value.EndNodeGUID == node.GUID))
                         .ToArray())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges
                         .Where(value =>
                             value != null &&
                             (value.StartNodeGUID == node.GUID ||
                              value.EndNodeGUID == node.GUID))
                         .ToArray())
            {
                edge.ClearConditionRuleGraph();
                graph.UnLink(edge);
            }
            graph.DeleteNode(node);
        }

        static void Connect(
            BaseGraph graph,
            GraphAuthoringMutationRequest request)
        {
            BaseNode source = Node(graph, request.SourceNodeId);
            BaseNode target = Node(graph, request.TargetNodeId);
            if (!BtsmtlSharedGraphPort.TryParse(
                    request.SourcePortId,
                    out bool sourceProperty,
                    out string sourcePortId) ||
                !BtsmtlSharedGraphPort.TryParse(
                    request.TargetPortId,
                    out bool targetProperty,
                    out string targetPortId) ||
                sourceProperty != targetProperty)
                throw new InvalidOperationException(
                    "BTSMTL connection endpoints use different port families.");
            if (!sourceProperty)
            {
                BaseEdge edge = graph.Link(
                    source,
                    target,
                    sourcePortId,
                    targetPortId);
                if (edge == null)
                    throw new InvalidOperationException(
                        "BTSMTL Flow connection already exists.");
                if (request.Value is BtsmtlSharedFlowEdgeCreationPayload
                    creation)
                    creation.Apply(edge);
                return;
            }
            if (!source.PropertyPortMap.TryGetValue(
                    sourcePortId,
                    out PropertyPort sourcePort) ||
                !target.PropertyPortMap.TryGetValue(
                    targetPortId,
                    out PropertyPort targetPort))
                throw new InvalidOperationException(
                    "BTSMTL Property connection endpoint is missing.");
            if (sourcePort.ValueType == null)
                sourcePort = source.SetPropertyPort(
                    sourcePort.FieldKey,
                    targetPort.GetType(),
                    sourcePort.Direction);
            if (targetPort.ValueType == null)
                targetPort = target.SetPropertyPort(
                    targetPort.FieldKey,
                    sourcePort.GetType(),
                    targetPort.Direction);
            graph.LinkProperty(source, target, sourcePort, targetPort);
        }

        static void Disconnect(
            BaseGraph graph,
            GraphAuthoringElementId edgeId)
        {
            PropertyEdge property = graph.PropertyEdges.SingleOrDefault(
                value => value != null && value.GUID == edgeId.Value);
            if (property != null)
            {
                graph.UnLinkProperty(property);
                return;
            }
            BaseEdge flow = graph.Edges.SingleOrDefault(
                value => value != null && value.GUID == edgeId.Value) ??
                throw new InvalidOperationException(
                    $"BTSMTL Edge '{edgeId}' is missing.");
            flow.ClearConditionRuleGraph();
            graph.UnLink(flow);
        }

        static BaseNode Node(
            BaseGraph graph,
            GraphAuthoringElementId nodeId) =>
            graph.Nodes.SingleOrDefault(value =>
                value != null && value.GUID == nodeId.Value) ??
            throw new InvalidOperationException(
                $"BTSMTL Node '{nodeId}' is missing.");

        static string OperationName(
            IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (requests.Count == 1)
                return "BTSMTL " + requests[0].Kind;
            return "BTSMTL Graph Edit";
        }
    }

    public sealed class BtsmtlSharedNodeCreationPayload
    {
        public BtsmtlSharedNodeCreationPayload(BaseNode node)
        {
            Node = node ??
                   throw new ArgumentNullException(nameof(node));
        }

        public BaseNode Node { get; }
    }

    public sealed class BtsmtlSharedFlowEdgeCreationPayload
    {
        readonly int m_FlowOrder;
        readonly int m_TransitionPriority;
        readonly BTAbortPolicy m_AbortPolicy;
        readonly ConditionRuleGraphOwnership m_Ownership;
        readonly ConditionRuleGraph m_InlineRule;
        readonly BaseTreeAsset m_SharedRule;

        public BtsmtlSharedFlowEdgeCreationPayload(BaseEdge source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_FlowOrder = source.FlowOrder;
            m_TransitionPriority = source.TransitionPriority;
            m_AbortPolicy = source.AbortPolicy;
            m_Ownership = source.ConditionRuleGraphOwnership;
            m_InlineRule = source.InlineConditionRuleGraph;
            m_SharedRule = source.SharedConditionRuleGraphAsset;
        }

        public void Apply(BaseEdge target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            target.FlowOrder = m_FlowOrder;
            target.TransitionPriority = m_TransitionPriority;
            target.AbortPolicy = m_AbortPolicy;
            switch (m_Ownership)
            {
                case ConditionRuleGraphOwnership.Inline
                    when m_InlineRule != null:
                    target.SetConditionRuleGraph(
                        m_InlineRule.CloneForAuthoring());
                    return;
                case ConditionRuleGraphOwnership.Shared
                    when m_SharedRule:
                    target.SetConditionRuleGraphAsset(m_SharedRule);
                    return;
                case ConditionRuleGraphOwnership.Unspecified:
                    target.ClearConditionRuleGraph();
                    return;
                default:
                    throw new InvalidOperationException(
                        "Copied BTSMTL Flow Edge has invalid Condition Rule ownership.");
            }
        }
    }

    public sealed class BtsmtlSharedGraphClipboardCodec :
        IBtsmtlSharedClipboardCodec
    {
        const string Schema = "btsmtl-graph-clipboard.v1";
        readonly IGraphAuthoringDomainMutation m_Mutation;
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;

        public BtsmtlSharedGraphClipboardCodec(
            IGraphAuthoringDomainMutation mutation,
            BtsmtlGraphAuthoringCapabilities catalog)
        {
            m_Mutation = mutation ??
                         throw new ArgumentNullException(nameof(mutation));
            m_Catalog = catalog ??
                        throw new ArgumentNullException(nameof(catalog));
        }

        public string Serialize(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringSelection> selection)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            HashSet<string> selected = (selection ??
                                        throw new ArgumentNullException(
                                            nameof(selection)))
                .Where(value =>
                    value.Kind == GraphAuthoringSelectionKind.Node)
                .Select(value => value.ElementId.Value)
                .ToHashSet(StringComparer.Ordinal);
            if (selected.Count == 0)
                throw new InvalidOperationException(
                    "BTSMTL clipboard selection contains no Nodes.");
            var data = new CopyPasteHelper();
            foreach (BaseNode node in btsmtl.Graph.Nodes
                         .Where(value =>
                             value != null &&
                             selected.Contains(value.GUID))
                         .OrderBy(value =>
                             value.GUID,
                             StringComparer.Ordinal))
            {
                if (!m_Catalog.TryGetSharedCapability(node, out _))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Node type '{node.GetType().FullName}' is not copyable without a formal capability.");
                }
                data.copiedNodes.Add(JsonSerializer.SerializeNode(node));
                data.centerPosition += node.Position;
            }
            data.centerPosition /= data.copiedNodes.Count;
            foreach (BaseEdge edge in btsmtl.Graph.Edges
                         .Where(value =>
                             value != null &&
                             selected.Contains(value.StartNodeGUID) &&
                             selected.Contains(value.EndNodeGUID))
                         .OrderBy(value =>
                             value.GUID,
                             StringComparer.Ordinal))
                data.copiedEdges.Add(JsonSerializer.Serialize(edge));
            foreach (PropertyEdge edge in btsmtl.Graph.PropertyEdges
                         .Where(value =>
                             value != null &&
                             selected.Contains(value.StartNodeGUID) &&
                             selected.Contains(value.EndNodeGUID))
                         .OrderBy(value =>
                             value.GUID,
                             StringComparer.Ordinal))
                data.copiedPropertyEdges.Add(
                    JsonSerializer.Serialize(edge));
            return JsonUtility.ToJson(
                new BtsmtlSharedClipboardEnvelope
                {
                    schema = Schema,
                    data = JsonUtility.ToJson(data)
                });
        }

        public string SerializeElements(
            IGraphAuthoringDocumentProjection document,
            IEnumerable<GraphElement> elements)
        {
            Require(document);
            GraphElement[] selected =
                elements?.Where(value => value != null).ToArray() ??
                Array.Empty<GraphElement>();
            var data = new CopyPasteHelper();
            foreach (GraphElement element in selected)
            {
                if (element is BaseNodeView nodeView)
                {
                    if (!m_Catalog.TryGetSharedCapability(
                            nodeView.Node,
                            out _))
                    {
                        throw new InvalidOperationException(
                            $"BTSMTL Node type '{nodeView.Node.GetType().FullName}' is not copyable without a formal capability.");
                    }
                    data.copiedNodes.Add(
                        JsonSerializer.SerializeNode(
                            nodeView.Node));
                    data.centerPosition +=
                        nodeView.Node.Position +
                        nodeView.GetPosition().size / 2f;
                    continue;
                }
                if (element is StackNodeView stackView)
                {
                    data.copiedStacks.Add(
                        JsonSerializer.Serialize(
                            stackView.StackNode));
                    data.centerPosition +=
                        stackView.GetPosition().center;
                    continue;
                }
                if (element is NodeGroupView groupView)
                {
                    data.copiedGroups.Add(
                        JsonSerializer.Serialize(
                            groupView.NodeGroup));
                    data.centerPosition +=
                        groupView.GetPosition().center;
                    continue;
                }
                if (element is not BaseEdgeView edgeView ||
                    !selected.Contains(edgeView.StartNodeView) ||
                    !selected.Contains(edgeView.EndNodeView))
                    continue;
                if (edgeView.Edge is PropertyEdge property)
                {
                    data.copiedPropertyEdges.Add(
                        JsonSerializer.Serialize(property));
                }
                else if (edgeView.Edge != null)
                {
                    data.copiedEdges.Add(
                        JsonSerializer.Serialize(
                            edgeView.Edge));
                }
            }
            int centerCount =
                data.copiedNodes.Count +
                data.copiedGroups.Count +
                data.copiedStacks.Count;
            if (centerCount == 0)
            {
                throw new InvalidOperationException(
                    "BTSMTL clipboard selection contains no copyable elements.");
            }
            data.centerPosition /= centerCount;
            return JsonUtility.ToJson(
                new BtsmtlSharedClipboardEnvelope
                {
                    schema = Schema,
                    data = JsonUtility.ToJson(data)
                });
        }

        public bool CanPasteElements(
            IGraphAuthoringDocumentProjection document,
            string payload) =>
            CanPaste(document, payload);

        public void PasteElements(
            IGraphAuthoringDocumentProjection document,
            string operationName,
            string payload,
            Vector2 graphPosition) =>
            Paste(
                document,
                operationName,
                payload,
                graphPosition);

        public bool CanPaste(
            IGraphAuthoringDocumentProjection document,
            string payload)
        {
            try
            {
                Validate(Require(document), Parse(payload));
                return !m_Mutation.ReadOnly;
            }
            catch
            {
                return false;
            }
        }

        public void Paste(
            IGraphAuthoringDocumentProjection document,
            string operationName,
            string payload,
            Vector2 graphPosition)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            CopyPasteHelper data = Parse(payload);
            Validate(btsmtl, data);
            Vector2 offset = graphPosition - data.centerPosition;
            var nodeIds = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var requests =
                new List<GraphAuthoringMutationRequest>();
            foreach (JsonElement serialized in data.copiedNodes)
            {
                BaseNode node = JsonSerializer.DeserializeNode(serialized) ??
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Node.");
                string sourceId = node.GUID;
                node.GUID = Guid.NewGuid().ToString("N");
                node.RegenerateOwnedAuthoringIdentities();
                nodeIds.Add(sourceId, node.GUID);
                if (!m_Catalog.TryGetSharedCapability(
                        node,
                        out GraphAuthoringCapabilityId capabilityId))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Node type '{node.GetType().FullName}' has no formal capability.");
                }
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateNode,
                    capabilityId: capabilityId,
                    value: new BtsmtlSharedNodeCreationPayload(node),
                    position: node.Position + offset));
            }
            var stackIds = new Dictionary<string, string>(
                StringComparer.Ordinal);
            foreach (JsonElement serialized in data.copiedStacks)
            {
                StackNode stack =
                    JsonSerializer.Deserialize<StackNode>(
                        serialized) ??
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Stack.");
                string sourceId = stack.GUID;
                stack.GUID = Guid.NewGuid().ToString("N");
                RemapIds(stack.NodeGUIDs, nodeIds);
                stackIds.Add(sourceId, stack.GUID);
                requests.Add(
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.CreateStack,
                        new GraphAuthoringElementId(stack.GUID),
                        value:
                        new BtsmtlStackMutationPayload(stack),
                        position: stack.Position + offset));
            }
            foreach (JsonElement serialized in data.copiedGroups)
            {
                NodeGroup group =
                    JsonSerializer.Deserialize<NodeGroup>(
                        serialized) ??
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Group.");
                RemapIds(group.NodeGUIDs, nodeIds);
                RemapIds(group.StackGUIDs, stackIds);
                requests.Add(
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.CreateGroup,
                        value:
                        new BtsmtlNodeGroupMutationPayload(group),
                        position: group.Position + offset));
            }
            foreach (JsonElement serialized in data.copiedEdges)
            {
                BaseEdge edge =
                    JsonSerializer.Deserialize<BaseEdge>(serialized);
                requests.Add(Connect(
                    edge,
                    nodeIds,
                    new BtsmtlSharedFlowEdgeCreationPayload(edge),
                    false));
            }
            foreach (JsonElement serialized in
                     data.copiedPropertyEdges)
            {
                PropertyEdge edge =
                    JsonSerializer.Deserialize<PropertyEdge>(serialized);
                requests.Add(Connect(
                    edge,
                    nodeIds,
                    null,
                    true));
            }
            m_Mutation.Apply(btsmtl, requests);
        }

        void Validate(
            BtsmtlSharedGraphDocument document,
            CopyPasteHelper data)
        {
            if (data == null ||
                data.copiedNodes.Count +
                data.copiedGroups.Count +
                data.copiedStacks.Count == 0)
            {
                throw new InvalidOperationException(
                    "BTSMTL shared clipboard contains no copyable elements.");
            }
            var nodeIds = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (JsonElement serialized in data.copiedNodes)
            {
                BaseNode node = JsonSerializer.DeserializeNode(serialized) ??
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Node.");
                if (!nodeIds.Add(node.GUID) ||
                    !document.Graph.CanCreateNodeType(node.GetType()) ||
                    !m_Catalog.TryGetSharedCapability(
                        node,
                        out GraphAuthoringCapabilityId capabilityId))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Graph cannot paste Node type '{node.GetType().FullName}'.");
                }
                m_Catalog.SharedCatalog.Require(
                    capabilityId,
                    document.DomainId,
                    document.DocumentRoleId);
            }
            foreach (JsonElement serialized in data.copiedEdges)
                ValidateEdge(
                    JsonSerializer.Deserialize<BaseEdge>(serialized),
                    nodeIds);
            foreach (JsonElement serialized in
                     data.copiedPropertyEdges)
                ValidateEdge(
                    JsonSerializer.Deserialize<PropertyEdge>(serialized),
                    nodeIds);
            var stackIds = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (JsonElement serialized in data.copiedStacks)
            {
                StackNode stack =
                    JsonSerializer.Deserialize<StackNode>(
                        serialized);
                if (stack == null ||
                    string.IsNullOrWhiteSpace(stack.GUID) ||
                    !stackIds.Add(stack.GUID) ||
                    stack.NodeGUIDs.Any(value =>
                        !nodeIds.Contains(value)))
                {
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Stack.");
                }
            }
            foreach (JsonElement serialized in data.copiedGroups)
            {
                NodeGroup group =
                    JsonSerializer.Deserialize<NodeGroup>(
                        serialized);
                if (group == null ||
                    group.NodeGUIDs.Any(value =>
                        !nodeIds.Contains(value)) ||
                    group.StackGUIDs.Any(value =>
                        !stackIds.Contains(value)))
                {
                    throw new InvalidOperationException(
                        "BTSMTL clipboard contains an invalid Group.");
                }
            }
        }

        static void RemapIds(
            IList<string> values,
            IReadOnlyDictionary<string, string> map)
        {
            string[] source = values.ToArray();
            values.Clear();
            for (int i = 0; i < source.Length; i++)
            {
                if (map.TryGetValue(
                        source[i],
                        out string target))
                    values.Add(target);
            }
        }

        static GraphAuthoringMutationRequest Connect(
            BaseEdge edge,
            IReadOnlyDictionary<string, string> nodeIds,
            object payload,
            bool property)
        {
            if (edge == null ||
                !nodeIds.TryGetValue(
                    edge.StartNodeGUID,
                    out string sourceId) ||
                !nodeIds.TryGetValue(
                    edge.EndNodeGUID,
                    out string targetId))
            {
                throw new InvalidOperationException(
                    "BTSMTL clipboard Edge endpoint is missing.");
            }
            return new GraphAuthoringMutationRequest(
                GraphAuthoringMutationKind.ConnectPorts,
                sourceNodeId: new GraphAuthoringElementId(sourceId),
                sourcePortId: property
                    ? BtsmtlSharedGraphPort.Property(
                        edge.StartPortName)
                    : BtsmtlSharedGraphPort.Flow(
                        edge.StartPortName),
                targetNodeId: new GraphAuthoringElementId(targetId),
                targetPortId: property
                    ? BtsmtlSharedGraphPort.Property(
                        edge.EndPortName)
                    : BtsmtlSharedGraphPort.Flow(
                        edge.EndPortName),
                value: payload);
        }

        static void ValidateEdge(
            BaseEdge edge,
            ISet<string> nodeIds)
        {
            if (edge == null ||
                !nodeIds.Contains(edge.StartNodeGUID) ||
                !nodeIds.Contains(edge.EndNodeGUID) ||
                string.IsNullOrWhiteSpace(edge.StartPortName) ||
                string.IsNullOrWhiteSpace(edge.EndPortName))
            {
                throw new InvalidOperationException(
                    "BTSMTL clipboard contains an invalid Edge.");
            }
        }

        static CopyPasteHelper Parse(string payload)
        {
            BtsmtlSharedClipboardEnvelope envelope =
                JsonUtility.FromJson<BtsmtlSharedClipboardEnvelope>(
                    payload);
            if (envelope == null ||
                envelope.schema != Schema ||
                string.IsNullOrWhiteSpace(envelope.data))
            {
                throw new InvalidOperationException(
                    "BTSMTL clipboard schema is invalid.");
            }
            return JsonUtility.FromJson<CopyPasteHelper>(
                       envelope.data) ??
                   throw new InvalidOperationException(
                       "BTSMTL clipboard data is invalid.");
        }

        static BtsmtlSharedGraphDocument Require(
            IGraphAuthoringDocumentProjection document) =>
            document as BtsmtlSharedGraphDocument ??
            throw new ArgumentException(
                "BTSMTL clipboard requires the shared BTSMTL document.",
                nameof(document));

        [Serializable]
        sealed class BtsmtlSharedClipboardEnvelope
        {
            public string schema;
            public string data;
        }
    }

    static class BtsmtlSharedNodeFieldBinding
    {
        public static void Set(
            BaseGraph graph,
            BaseNode node,
            GraphAuthoringFieldId fieldId,
            object value,
            IReadOnlyList<BaseTree> visibleTrees)
        {
            string text = value?.ToString()?.Trim() ?? string.Empty;
            switch (fieldId.Value)
            {
                case "loopStopType" when node is LoopNode loop:
                    loop.ConfigureAuthoring(Parse<LoopNode.StopType>(
                        text,
                        fieldId));
                    return;
                case "compareType" when node is CompareNode compare:
                    compare.ConfigureAuthoring(Parse<CompareNode.CompareType>(
                        text,
                        fieldId));
                    return;
                case "moveSpeed" when node is LocomotionInputMotionNode motion:
                    motion.ConfigureAuthoring(
                        ReadFloat(value, fieldId),
                        motion.TurnSpeedDegrees,
                        motion.CameraRelative,
                        motion.Continuous);
                    return;
                case "turnSpeedDegrees" when node is LocomotionInputMotionNode motion:
                    motion.ConfigureAuthoring(
                        motion.MoveSpeed,
                        ReadFloat(value, fieldId),
                        motion.CameraRelative,
                        motion.Continuous);
                    return;
                case "cameraRelative" when node is LocomotionInputMotionNode motion:
                    motion.ConfigureAuthoring(
                        motion.MoveSpeed,
                        motion.TurnSpeedDegrees,
                        ReadBool(value, fieldId),
                        motion.Continuous);
                    return;
                case "continuous" when node is LocomotionInputMotionNode motion:
                    motion.ConfigureAuthoring(
                        motion.MoveSpeed,
                        motion.TurnSpeedDegrees,
                        motion.CameraRelative,
                        ReadBool(value, fieldId));
                    return;
                case "inputId" when node is CharacterInputValueInfoNode input:
                    RequireNonEmpty(text, fieldId);
                    input.BindInputValue(text);
                    return;
                case "requestId" when node is CharacterActionRequestInfoNode request:
                    RequireNonEmpty(text, fieldId);
                    request.BindActionRequest(text);
                    return;
                case "blackboardDeclarationId"
                    when node is PipelineBlackboardValueInfoNode blackboard:
                    blackboard.ConfigureAuthoring(
                        ResolveDeclaration(
                            graph,
                            visibleTrees,
                            text,
                            blackboard.BlackboardValueType));
                    return;
                case "stateExitCause" when node is StateExitCauseInfoNode exit:
                    exit.ConfigureAuthoring(Parse<StateExitCause>(
                        text,
                        fieldId));
                    return;
                case "actionContextId" when node is ActionContextActiveInfoNode context:
                    context.ConfigureAuthoring(
                        ResolveAssetGuid<ActionContextSlot>(
                            text,
                            fieldId));
                    return;
                case "windowType" when node is ActionWindowActiveInfoNode window:
                    RequireNonEmpty(text, fieldId);
                    window.ConfigureAuthoring(text);
                    return;
                case "actionProfileId" when node is CanActivateActionInfoNode action:
                    action.ConfigureAuthoring(
                        ResolveActionProfile(text, fieldId),
                        action.TargetSnapshotVariable);
                    return;
                case "targetSnapshotBlackboardDeclarationId"
                    when node is CanActivateActionInfoNode action:
                    action.ConfigureAuthoring(
                        action.ActionProfile,
                        ResolveDeclaration(
                                graph,
                                visibleTrees,
                                text,
                                null)
                            .CreateBlackboardReference());
                    return;
                default:
                    throw new InvalidOperationException(
                        $"BTSMTL Node '{node.GetType().Name}' does not declare writable field '{fieldId}'.");
            }
        }

        static BaseExposedProperty ResolveDeclaration(
            BaseGraph graph,
            IReadOnlyList<BaseTree> visibleTrees,
            string declarationId,
            Type valueType)
        {
            RequireNonEmpty(
                declarationId,
                new GraphAuthoringFieldId("blackboardDeclarationId"));
            IEnumerable<BaseTree> sources =
                visibleTrees ?? (graph is BaseTree tree
                    ? new[] { tree }
                    : Array.Empty<BaseTree>());
            BaseExposedProperty[] matches = sources
                .Where(value => value != null)
                .Distinct()
                .SelectMany(value => value.ExposedProperties)
                .Where(value =>
                    value != null &&
                    value.DeclarationId == declarationId &&
                    (valueType == null || value.ValueType == valueType))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Blackboard declaration '{declarationId}' resolved {matches.Length} exact matches.");
            }
            return matches[0];
        }

        static T ResolveAssetGuid<T>(
            string guid,
            GraphAuthoringFieldId fieldId)
            where T : UnityEngine.Object
        {
            RequireNonEmpty(guid, fieldId);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(path);
            return asset
                ? asset
                : throw new InvalidOperationException(
                    $"Field '{fieldId}' does not resolve exact asset GUID '{guid}'.");
        }

        static ActionProfile ResolveActionProfile(
            string actionId,
            GraphAuthoringFieldId fieldId)
        {
            RequireNonEmpty(actionId, fieldId);
            ActionProfile[] matches = AssetDatabase
                .FindAssets("t:ActionProfile")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActionProfile>)
                .Where(value =>
                    value &&
                    value.ActionId == actionId)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"ActionProfile identity '{actionId}' resolved {matches.Length} exact matches.");
            }
            return matches[0];
        }

        static T Parse<T>(
            string value,
            GraphAuthoringFieldId fieldId)
            where T : struct
        {
            if (Enum.TryParse(value, false, out T parsed))
                return parsed;
            throw new InvalidOperationException(
                $"Field '{fieldId}' has invalid value '{value}'.");
        }

        static float ReadFloat(object value, GraphAuthoringFieldId fieldId)
        {
            try
            {
                float result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                if (!float.IsNaN(result) && !float.IsInfinity(result))
                    return result;
            }
            catch (Exception)
            {
            }
            throw new InvalidOperationException(
                $"Field '{fieldId}' has invalid Float value '{value}'.");
        }

        static bool ReadBool(object value, GraphAuthoringFieldId fieldId)
        {
            if (value is bool result)
                return result;
            if (bool.TryParse(value?.ToString(), out result))
                return result;
            throw new InvalidOperationException(
                $"Field '{fieldId}' has invalid Boolean value '{value}'.");
        }

        static void RequireNonEmpty(
            string value,
            GraphAuthoringFieldId fieldId)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Field '{fieldId}' cannot be empty.");
        }
    }

    static class BtsmtlSharedNodeCommandBinding
    {
        public static void Execute(
            BaseNode node,
            GraphAuthoringCommandId commandId,
            object value)
        {
            if (node is TimelineNode timeline &&
                commandId.Equals(
                    TimelineAuthoringCommands.UseInline))
            {
                if (timeline.Timeline == null)
                    throw new InvalidOperationException(
                        "Timeline Node has no source to clone inline.");
                timeline.ConfigureAuthoring(
                    timeline.Timeline.CloneForAuthoring(),
                    timeline.ActionContext,
                    timeline.PlaybackMode);
                return;
            }
            if (node is TimelineNode shared &&
                commandId.Equals(
                    TimelineAuthoringCommands.UseShared))
            {
                TimelineAsset asset = value as TimelineAsset;
                if (!asset || asset.Data == null)
                    throw new InvalidOperationException(
                        "Use Shared Timeline requires one formal Timeline asset.");
                shared.ConfigureSharedAuthoring(
                    asset,
                    shared.ActionContext,
                    shared.PlaybackMode);
                return;
            }
            throw new InvalidOperationException(
                $"BTSMTL Node '{node.GetType().Name}' has no handler for command '{commandId}'.");
        }
    }

    public sealed class BtsmtlSharedGraphDetailsDataSource :
        IGraphAuthoringDetailsDataSource
    {
        readonly IGraphAuthoringDomainDiagnostics m_Diagnostics;

        public BtsmtlSharedGraphDetailsDataSource(
            IGraphAuthoringDomainDiagnostics diagnostics)
        {
            m_Diagnostics = diagnostics ??
                            throw new ArgumentNullException(
                                nameof(diagnostics));
        }

        public object ReadField(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            BaseNode node = btsmtl.Graph.Nodes.Single(value =>
                value != null && value.GUID == elementId.Value);
            switch (field.FieldId.Value)
            {
                case "loopStopType" when node is LoopNode loop:
                    return loop.LoopStopType.ToString();
                case "compareType" when node is CompareNode compare:
                    return compare.Comparison.ToString();
                case "moveSpeed" when node is LocomotionInputMotionNode motion:
                    return motion.MoveSpeed;
                case "turnSpeedDegrees" when node is LocomotionInputMotionNode motion:
                    return motion.TurnSpeedDegrees;
                case "cameraRelative" when node is LocomotionInputMotionNode motion:
                    return motion.CameraRelative;
                case "continuous" when node is LocomotionInputMotionNode motion:
                    return motion.Continuous;
                case "inputId" when node is CharacterInputValueInfoNode input:
                    return input.InputValueId;
                case "requestId" when node is CharacterActionRequestInfoNode request:
                    return request.RequestId;
                case "blackboardDeclarationId"
                    when node is PipelineBlackboardValueInfoNode blackboard:
                    return blackboard.BlackboardVariable.DeclarationId;
                case "stateExitCause" when node is StateExitCauseInfoNode exit:
                    return exit.Cause.ToString();
                case "actionContextId" when node is ActionContextActiveInfoNode context:
                    return AssetGuid(context.ActionContext);
                case "windowType" when node is ActionWindowActiveInfoNode window:
                    return window.WindowType;
                case "actionProfileId" when node is CanActivateActionInfoNode action:
                    return action.ActionProfile
                        ? action.ActionProfile.ActionId
                        : string.Empty;
                case "targetSnapshotBlackboardDeclarationId"
                    when node is CanActivateActionInfoNode action:
                    return action.TargetSnapshotVariable.DeclarationId;
                default:
                    return null;
            }
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetLive(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection) =>
            m_Diagnostics.GetRuntimeTrace(document)
                .Where(value =>
                    value.ElementId.Equals(selection.ElementId))
                .Select(value => new GraphAuthoringReadOnlyDetail(
                    value.Status,
                    value.Detail))
                .ToArray();

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetReferences(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection)
        {
            BtsmtlSharedGraphDocument btsmtl = Require(document);
            BaseNode node = btsmtl.Graph.Nodes.FirstOrDefault(value =>
                value != null && value.GUID == selection.ElementId.Value);
            if (node == null)
                return Array.Empty<GraphAuthoringReadOnlyDetail>();
            var result = new List<GraphAuthoringReadOnlyDetail>();
            foreach (NodeGraphReference reference in node.GetGraphReferences())
            {
                result.Add(new GraphAuthoringReadOnlyDetail(
                    reference.Label,
                    reference.Tree?.name ?? "Missing"));
            }
            foreach (NodeAssetReference reference in node.GetAssetReferences())
            {
                result.Add(new GraphAuthoringReadOnlyDetail(
                    reference.Label,
                    reference.Asset ? reference.Asset.name : "Missing"));
            }
            return result;
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetDiagnostics(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection) =>
            m_Diagnostics.GetDiagnostics(document)
                .Where(value =>
                    !value.ElementId.IsValid ||
                    value.ElementId.Equals(selection.ElementId))
                .Select(value => new GraphAuthoringReadOnlyDetail(
                    value.Code,
                    value.Message,
                    value.Severity.ToString()))
                .ToArray();

        static string AssetGuid(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(path);
        }

        static BtsmtlSharedGraphDocument Require(
            IGraphAuthoringDocumentProjection document) =>
            document as BtsmtlSharedGraphDocument ??
            throw new ArgumentException(
                "BTSMTL Details requires the shared BTSMTL document.",
                nameof(document));
    }
}
