using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseCanvasView : GraphView
    {
        sealed class PosePortView : GraphAuthoringPortViewBase
        {
            public PosePortView(
                Direction direction,
                Port.Capacity capacity,
                Type portType)
                : base(Orientation.Horizontal, direction, capacity, portType)
            {
                InstallConnector<Edge>();
            }
        }

        sealed class PosePortBinding
        {
            public GraphAuthoringElementId NodeId;
            public GraphAuthoringPortId PortId;
            public GraphAuthoringPortDirection Direction;
        }

        sealed class StatePortBinding
        {
            public GraphAuthoringElementId OwnerId;
            public GraphAuthoringPortDirection Direction;
        }

        sealed class PoseNodeView : Node
        {
            readonly Dictionary<GraphAuthoringPortId, Port> m_Ports =
                new Dictionary<GraphAuthoringPortId, Port>();

            public PoseNodeView(
                GraphAuthoringNodeProjection projection,
                GraphAuthoringCapabilityDescriptor capability,
                IReadOnlyList<GraphAuthoringDynamicPortProjection> ports)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                Capability = capability ?? throw new ArgumentNullException(nameof(capability));
                title = string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? capability.DisplayName
                    : projection.DisplayName;
                viewDataKey = projection.NodeId.Value;
                capabilities = Capabilities.Selectable |
                               Capabilities.Movable |
                               Capabilities.Deletable |
                               Capabilities.Ascendable;
                SetPosition(new Rect(projection.Position, new Vector2(260f, 120f)));
                titleContainer.style.backgroundColor = capability.Color;
                foreach (GraphAuthoringDynamicPortProjection descriptor in
                         ports.OrderBy(value => value.Order))
                {
                    if (!m_Ports.TryAdd(descriptor.PortId, CreatePort(descriptor)))
                        throw new InvalidOperationException(
                            $"Pose Canvas node '{projection.NodeId}' contains duplicate port '{descriptor.PortId}'.");
                }
                if (!string.IsNullOrWhiteSpace(projection.Status))
                    extensionContainer.Add(new Label(projection.Status));
                if (!string.IsNullOrWhiteSpace(capability.ExecutionDomainId))
                    extensionContainer.Add(new Label($"Execution: {capability.ExecutionDomainId}"));
                RefreshExpandedState();
                RefreshPorts();
            }

            public GraphAuthoringNodeProjection Projection { get; }
            public GraphAuthoringCapabilityDescriptor Capability { get; }

            public Port RequirePort(GraphAuthoringPortId portId) =>
                m_Ports.TryGetValue(portId, out Port port)
                    ? port
                    : throw new InvalidOperationException(
                        $"Pose Canvas node '{Projection.NodeId}' has no port '{portId}'.");

            Port CreatePort(GraphAuthoringDynamicPortProjection descriptor)
            {
                Direction direction = descriptor.Direction == GraphAuthoringPortDirection.Input
                    ? Direction.Input
                    : Direction.Output;
                Port.Capacity capacity =
                    descriptor.Capacity == GraphAuthoringPortCapacity.Single
                        ? Port.Capacity.Single
                        : Port.Capacity.Multi;
                Port port = new PosePortView(direction, capacity, typeof(PoseLink));
                port.portName = descriptor.DisplayName;
                port.tooltip = descriptor.ValueTypeId;
                port.userData = new PosePortBinding
                {
                    NodeId = Projection.NodeId,
                    PortId = descriptor.PortId,
                    Direction = descriptor.Direction
                };
                return port;
            }

            sealed class PoseLink { }
        }

        sealed class PoseEdgeView : Edge
        {
            public PoseEdgeView(GraphAuthoringEdgeProjection projection)
            {
                Projection = projection;
                viewDataKey = projection.EdgeId.Value;
            }

            public GraphAuthoringEdgeProjection Projection { get; }
        }

        sealed class StateEntryView : Node
        {
            public StateEntryView(GraphAuthoringStateMachineEntryProjection projection)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                title = "Entry";
                viewDataKey = projection.ElementId.Value;
                capabilities = Capabilities.Selectable |
                               Capabilities.Movable |
                               Capabilities.Ascendable;
                SetPosition(new Rect(projection.Position, new Vector2(190f, 80f)));
                Output = CreatePort(Direction.Output);
                outputContainer.Add(Output);
                RefreshPorts();
            }

            public GraphAuthoringStateMachineEntryProjection Projection { get; }
            public Port Output { get; }

            Port CreatePort(Direction direction)
            {
                Port port = new PosePortView(direction, Port.Capacity.Multi, typeof(StateLink));
                port.portName = string.Empty;
                port.userData = new StatePortBinding
                {
                    OwnerId = Projection.ElementId,
                    Direction = GraphAuthoringPortDirection.Output
                };
                return port;
            }
        }

        sealed class StateNodeView : Node
        {
            public StateNodeView(
                GraphAuthoringStateProjection projection,
                GraphAuthoringCapabilityDescriptor capability,
                bool persistsLayout)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                Capability = capability ?? throw new ArgumentNullException(nameof(capability));
                title = string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? capability.DisplayName
                    : projection.DisplayName;
                viewDataKey = projection.StateId.Value;
                capabilities = Capabilities.Selectable |
                               Capabilities.Ascendable |
                               Capabilities.Deletable;
                if (persistsLayout)
                    capabilities |= Capabilities.Movable;
                SetPosition(new Rect(projection.Position, new Vector2(230f, 100f)));
                titleContainer.style.backgroundColor = capability.Color;
                Input = CreatePort(Direction.Input);
                Output = CreatePort(Direction.Output);
                inputContainer.Add(Input);
                outputContainer.Add(Output);
                if (projection.ChildGraphId.IsValid)
                    extensionContainer.Add(new Label("Open Graph"));
                if (!string.IsNullOrWhiteSpace(projection.Status))
                    extensionContainer.Add(new Label(projection.Status));
                RefreshExpandedState();
                RefreshPorts();
            }

            public GraphAuthoringStateProjection Projection { get; }
            public GraphAuthoringCapabilityDescriptor Capability { get; }
            public Port Input { get; }
            public Port Output { get; }

            Port CreatePort(Direction direction)
            {
                Port port = new PosePortView(direction, Port.Capacity.Multi, typeof(StateLink));
                port.portName = string.Empty;
                port.userData = new StatePortBinding
                {
                    OwnerId = Projection.StateId,
                    Direction = direction == Direction.Input
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output
                };
                return port;
            }
        }

        sealed class StateAliasView : Node
        {
            public StateAliasView(
                GraphAuthoringStateAliasProjection projection,
                bool persistsLayout)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                title = string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? "Alias"
                    : projection.DisplayName;
                viewDataKey = projection.AliasId.Value;
                capabilities = Capabilities.Selectable |
                               Capabilities.Ascendable |
                               Capabilities.Deletable;
                if (persistsLayout)
                    capabilities |= Capabilities.Movable;
                SetPosition(new Rect(projection.Position, new Vector2(210f, 90f)));
                Input = CreatePort(Direction.Input);
                Output = CreatePort(Direction.Output);
                inputContainer.Add(Input);
                outputContainer.Add(Output);
                RefreshPorts();
            }

            public GraphAuthoringStateAliasProjection Projection { get; }
            public Port Input { get; }
            public Port Output { get; }

            Port CreatePort(Direction direction)
            {
                Port port = new PosePortView(direction, Port.Capacity.Multi, typeof(StateLink));
                port.portName = string.Empty;
                port.userData = new StatePortBinding
                {
                    OwnerId = Projection.AliasId,
                    Direction = direction == Direction.Input
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output
                };
                return port;
            }
        }

        sealed class StateTransitionView : Edge
        {
            public StateTransitionView(GraphAuthoringTransitionProjection projection)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                viewDataKey = projection.TransitionId.Value;
                tooltip = projection.DisplayName ?? string.Empty;
            }

            public GraphAuthoringTransitionProjection Projection { get; }
        }

        sealed class StateLink { }

        readonly Dictionary<GraphAuthoringElementId, PoseNodeView> m_PoseNodes =
            new Dictionary<GraphAuthoringElementId, PoseNodeView>();
        readonly Dictionary<GraphAuthoringElementId, StateNodeView> m_StateNodes =
            new Dictionary<GraphAuthoringElementId, StateNodeView>();
        readonly Dictionary<GraphAuthoringElementId, StateAliasView> m_StateAliases =
            new Dictionary<GraphAuthoringElementId, StateAliasView>();
        readonly Dictionary<GraphAuthoringElementId, Port> m_StateInputs =
            new Dictionary<GraphAuthoringElementId, Port>();
        readonly Dictionary<GraphAuthoringElementId, Port> m_StateOutputs =
            new Dictionary<GraphAuthoringElementId, Port>();
        GraphAuthoringProjectionCanvasBinding m_ProjectionBinding;
        GraphAuthoringStateMachineBinding m_StateMachineBinding;
        bool m_Populating;
        bool m_RuntimeReadOnly;
        Vector2 m_PastePosition;
        bool m_ClipboardBound;

        public CharacterPoseCanvasView()
        {
            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            RegisterCallback<PointerMoveEvent>(evt => m_PastePosition = contentViewContainer.WorldToLocal(evt.position));
        }

        public bool RuntimeReadOnly => m_RuntimeReadOnly;
        public GraphAuthoringProjectionCanvasBinding ProjectionBinding => m_ProjectionBinding;
        public GraphAuthoringStateMachineBinding StateMachineBinding => m_StateMachineBinding;
        public event Action<Vector2, IReadOnlyList<GraphAuthoringCapabilityDescriptor>> NodeCreationRequested;
        public event Action<Vector2> StateMachineNodeCreationRequested;
        public event Action<GraphAuthoringNodeProjection, GraphAuthoringChildSurfaceDescriptor> ChildSurfaceRequested;

        public void BindProjection(GraphAuthoringProjectionCanvasBinding binding)
        {
            m_ProjectionBinding = binding ?? throw new ArgumentNullException(nameof(binding));
            m_StateMachineBinding = null;
            graphViewChanged = ApplyProjectionChange;
            nodeCreationRequest = context =>
                NodeCreationRequested?.Invoke(
                    context.screenMousePosition,
                    m_ProjectionBinding.Capabilities.GetAllowed(
                        m_ProjectionBinding.Document.DomainId,
                        m_ProjectionBinding.Document.DocumentRoleId));
            BindProjectionClipboard();
            PopulateProjection();
        }

        public void BindStateMachine(GraphAuthoringStateMachineBinding binding)
        {
            m_StateMachineBinding = binding ?? throw new ArgumentNullException(nameof(binding));
            m_ProjectionBinding = null;
            graphViewChanged = ApplyStateMachineChange;
            nodeCreationRequest = context => StateMachineNodeCreationRequested?.Invoke(context.screenMousePosition);
            PopulateStateMachine();
        }

        public void PopulateProjection()
        {
            if (m_ProjectionBinding == null)
                throw new InvalidOperationException("Pose Canvas projection binding is missing.");
            m_Populating = true;
            try
            {
                ClearGraphElements();
                foreach (GraphAuthoringNodeProjection projection in
                         m_ProjectionBinding.Document.Nodes ??
                         Array.Empty<GraphAuthoringNodeProjection>())
                {
                    GraphAuthoringCapabilityDescriptor capability =
                        m_ProjectionBinding.Capabilities.Require(
                            projection.CapabilityId,
                            m_ProjectionBinding.Document.DomainId,
                            m_ProjectionBinding.Document.DocumentRoleId);
                    IReadOnlyList<GraphAuthoringDynamicPortProjection> ports =
                        ProjectPorts(projection);
                    var view = new PoseNodeView(projection, capability, ports);
                    if (capability.ChildSurfaces.Count != 0)
                    {
                        view.RegisterCallback<MouseDownEvent>(evt =>
                        {
                            if (evt.button == 0 && evt.clickCount == 2)
                                ChildSurfaceRequested?.Invoke(projection, capability.ChildSurfaces[0]);
                        });
                    }
                    m_PoseNodes.Add(projection.NodeId, view);
                    AddElement(view);
                }
                foreach (GraphAuthoringEdgeProjection projection in
                         m_ProjectionBinding.Document.Edges ??
                         Array.Empty<GraphAuthoringEdgeProjection>())
                    AddPoseEdge(projection);
            }
            finally
            {
                m_Populating = false;
            }
            SetRuntimeReadOnly(m_RuntimeReadOnly);
        }

        public void PopulateStateMachine()
        {
            if (m_StateMachineBinding == null)
                throw new InvalidOperationException("Pose StateMachine binding is missing.");
            m_Populating = true;
            try
            {
                ClearGraphElements();
                GraphAuthoringStateMachineEntryProjection entry =
                    m_StateMachineBinding.Document.Entry ??
                    throw new InvalidOperationException("Pose StateMachine Entry is missing.");
                var entryView = new StateEntryView(entry);
                m_StateOutputs.Add(entry.ElementId, entryView.Output);
                AddElement(entryView);
                foreach (GraphAuthoringStateProjection projection in
                         m_StateMachineBinding.Document.States ??
                         Array.Empty<GraphAuthoringStateProjection>())
                {
                    GraphAuthoringCapabilityDescriptor capability =
                        m_StateMachineBinding.Capabilities.Require(
                            projection.CapabilityId,
                            m_StateMachineBinding.Document.DomainId,
                            m_StateMachineBinding.Document.DocumentRoleId);
                    var view = new StateNodeView(
                        projection,
                        capability,
                        m_StateMachineBinding.Policy.PersistsLayout);
                    view.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button == 0 && evt.clickCount == 2 && projection.ChildGraphId.IsValid)
                            m_StateMachineBinding.Policy.OpenStateChildGraph(
                                m_StateMachineBinding.Document,
                                projection.StateId);
                    });
                    m_StateNodes.Add(projection.StateId, view);
                    m_StateInputs.Add(projection.StateId, view.Input);
                    m_StateOutputs.Add(projection.StateId, view.Output);
                    AddElement(view);
                }
                foreach (GraphAuthoringStateAliasProjection projection in
                         m_StateMachineBinding.Document.Aliases ??
                         Array.Empty<GraphAuthoringStateAliasProjection>())
                {
                    var view = new StateAliasView(
                        projection,
                        m_StateMachineBinding.Policy.PersistsLayout);
                    m_StateAliases.Add(projection.AliasId, view);
                    m_StateInputs.Add(projection.AliasId, view.Input);
                    m_StateOutputs.Add(projection.AliasId, view.Output);
                    AddElement(view);
                }
                if (entry.TargetStateId.IsValid)
                    AddStateLink(entry.ElementId, entry.TargetStateId, null);
                foreach (GraphAuthoringTransitionProjection projection in
                         m_StateMachineBinding.Document.Transitions ??
                         Array.Empty<GraphAuthoringTransitionProjection>())
                    AddStateLink(
                        projection.SourceStateId,
                        projection.TargetStateId,
                        projection);
            }
            finally
            {
                m_Populating = false;
            }
            SetRuntimeReadOnly(m_RuntimeReadOnly);
        }

        public void CreateNode(
            GraphAuthoringCapabilityId capabilityId,
            object typedPayload,
            Vector2 graphPosition)
        {
            if (m_ProjectionBinding == null || m_ProjectionBinding.Mutation.ReadOnly)
                throw new InvalidOperationException("Pose Canvas cannot create a node while read-only.");
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

        public IReadOnlyList<GraphAuthoringSelection> GetStableSelection()
        {
            var result = new List<GraphAuthoringSelection>();
            if (m_StateMachineBinding != null)
            {
                foreach (GraphElement element in selection)
                {
                    if (element is StateNodeView state)
                        result.Add(new GraphAuthoringSelection(
                            GraphAuthoringSelectionKind.State,
                            state.Projection.StateId));
                    else if (element is StateAliasView alias)
                        result.Add(new GraphAuthoringSelection(
                            GraphAuthoringSelectionKind.State,
                            alias.Projection.AliasId));
                    else if (element is StateTransitionView transition)
                        result.Add(new GraphAuthoringSelection(
                            GraphAuthoringSelectionKind.Transition,
                            transition.Projection.TransitionId));
                }
                return result;
            }
            foreach (GraphElement element in selection)
            {
                if (element is PoseNodeView node)
                    result.Add(new GraphAuthoringSelection(
                        GraphAuthoringSelectionKind.Node,
                        node.Projection.NodeId));
                else if (element is PoseEdgeView edge)
                    result.Add(new GraphAuthoringSelection(
                        GraphAuthoringSelectionKind.Edge,
                        edge.Projection.EdgeId));
            }
            return result;
        }

        public void FocusElement(GraphAuthoringElementId elementId)
        {
            GraphElement element = graphElements.FirstOrDefault(value =>
                string.Equals(value.viewDataKey, elementId.Value, StringComparison.Ordinal));
            if (element == null)
                return;
            ClearSelection();
            AddToSelection(element);
            FrameSelection();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            if (m_RuntimeReadOnly)
                return new List<Port>();
            if (m_ProjectionBinding != null && startPort.userData is PosePortBinding source)
            {
                var result = new List<Port>();
                foreach (Port port in ports)
                {
                    if (!(port.userData is PosePortBinding target) ||
                        target.NodeId.Equals(source.NodeId) ||
                        target.Direction == source.Direction)
                        continue;
                    PosePortBinding output = source.Direction == GraphAuthoringPortDirection.Output
                        ? source
                        : target;
                    PosePortBinding input = source.Direction == GraphAuthoringPortDirection.Input
                        ? source
                        : target;
                    GraphAuthoringNodeProjection outputNode =
                        m_PoseNodes[output.NodeId].Projection;
                    GraphAuthoringNodeProjection inputNode =
                        m_PoseNodes[input.NodeId].Projection;
                    if (m_ProjectionBinding.ConnectionPolicy.CanConnect(
                            m_ProjectionBinding.Document,
                            outputNode,
                            output.PortId,
                            inputNode,
                            input.PortId))
                        result.Add(port);
                }
                return result;
            }
            if (m_StateMachineBinding != null && startPort.userData is StatePortBinding stateSource)
            {
                var result = new List<Port>();
                foreach (Port port in ports)
                {
                    if (!(port.userData is StatePortBinding stateTarget) ||
                        stateTarget.OwnerId.Equals(stateSource.OwnerId) ||
                        stateTarget.Direction == stateSource.Direction)
                        continue;
                    GraphAuthoringElementId output =
                        stateSource.Direction == GraphAuthoringPortDirection.Output
                            ? stateSource.OwnerId
                            : stateTarget.OwnerId;
                    GraphAuthoringElementId input =
                        stateSource.Direction == GraphAuthoringPortDirection.Input
                            ? stateSource.OwnerId
                            : stateTarget.OwnerId;
                    if (m_StateMachineBinding.Policy.CanCreateTransition(
                            m_StateMachineBinding.Document,
                            output,
                            input))
                        result.Add(port);
                }
                return result;
            }
            return new List<Port>();
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            foreach (GraphElement element in graphElements)
            {
                if (readOnly)
                    element.capabilities &= Capabilities.Selectable | Capabilities.Ascendable;
                else if (element is PoseNodeView)
                    element.capabilities |= Capabilities.Selectable |
                                             Capabilities.Movable |
                                             Capabilities.Deletable |
                                             Capabilities.Ascendable;
                else if (element is StateNodeView || element is StateAliasView)
                    element.capabilities |= Capabilities.Selectable |
                                             Capabilities.Deletable |
                                             Capabilities.Ascendable;
            }
        }

        void BindProjectionClipboard()
        {
            if (m_ClipboardBound)
                return;
            m_ClipboardBound = true;
            GraphAuthoringClipboardController.Bind(
                this,
                () => m_ProjectionBinding.Document.DomainId.Value,
                elements =>
                {
                    if (m_ProjectionBinding.Clipboard == null)
                        return string.Empty;
                    var selected = new List<GraphAuthoringSelection>();
                    foreach (GraphElement element in elements)
                    {
                        if (element is PoseNodeView node)
                            selected.Add(new GraphAuthoringSelection(
                                GraphAuthoringSelectionKind.Node,
                                node.Projection.NodeId));
                        else if (element is PoseEdgeView edge)
                            selected.Add(new GraphAuthoringSelection(
                                GraphAuthoringSelectionKind.Edge,
                                edge.Projection.EdgeId));
                    }
                    return m_ProjectionBinding.Clipboard.Serialize(
                        m_ProjectionBinding.Document,
                        selected);
                },
                payload => m_ProjectionBinding.Clipboard != null &&
                           m_ProjectionBinding.Clipboard.CanPaste(
                               m_ProjectionBinding.Document,
                               payload),
                (operationName, payload) =>
                {
                    if (m_ProjectionBinding.Clipboard == null)
                        throw new InvalidOperationException(
                            "Pose Canvas clipboard is unavailable for the current document.");
                    m_ProjectionBinding.Clipboard.Paste(
                        m_ProjectionBinding.Document,
                        operationName,
                        payload,
                        m_PastePosition);
                    PopulateProjection();
                });
        }

        IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectPorts(
            GraphAuthoringNodeProjection projection)
        {
            if (m_ProjectionBinding.Document is CharacterPoseCanvasGraphDocument poseDocument)
            {
                CharacterPoseCanvasNode node = poseDocument.Graph.Nodes.Single(
                    value => value.NodeId.Value == projection.NodeId.Value);
                return CharacterPoseCanvasDefinitionProjection.For(node).Ports;
            }
            GraphAuthoringCapabilityDescriptor capability =
                m_ProjectionBinding.Capabilities.Require(
                    projection.CapabilityId,
                    m_ProjectionBinding.Document.DomainId,
                    m_ProjectionBinding.Document.DocumentRoleId);
            return capability.FixedPorts
                .OrderBy(value => value.Order)
                .Select(value => new GraphAuthoringDynamicPortProjection(
                    value.PortId,
                    value.DisplayName,
                    value.ValueTypeId,
                    value.Direction,
                    value.Capacity,
                    value.Required,
                    value.Order,
                    value.InterfacePortId))
                .Concat(projection.DynamicPorts ??
                        Array.Empty<GraphAuthoringDynamicPortProjection>())
                .OrderBy(value => value.Order)
                .ToArray();
        }

        void AddPoseEdge(GraphAuthoringEdgeProjection projection)
        {
            PoseNodeView source = m_PoseNodes[projection.SourceNodeId];
            PoseNodeView target = m_PoseNodes[projection.TargetNodeId];
            Port output = source.RequirePort(projection.SourcePortId);
            Port input = target.RequirePort(projection.TargetPortId);
            var edge = new PoseEdgeView(projection)
            {
                output = output,
                input = input
            };
            output.Connect(edge);
            input.Connect(edge);
            AddElement(edge);
        }

        void AddStateLink(
            GraphAuthoringElementId sourceId,
            GraphAuthoringElementId targetId,
            GraphAuthoringTransitionProjection projection)
        {
            if (!m_StateOutputs.TryGetValue(sourceId, out Port output) ||
                !m_StateInputs.TryGetValue(targetId, out Port input))
                throw new InvalidOperationException(
                    $"Pose StateMachine link '{sourceId}' to '{targetId}' has a missing endpoint.");
            Edge edge = projection == null
                ? new Edge()
                : new StateTransitionView(projection);
            if (edge is StateTransitionView transition)
            {
                transition.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0 && evt.clickCount == 2)
                        m_StateMachineBinding.Policy.OpenTransitionRule(
                            m_StateMachineBinding.Document,
                            transition.Projection.TransitionId);
                });
            }
            edge.output = output;
            edge.input = input;
            output.Connect(edge);
            input.Connect(edge);
            AddElement(edge);
        }

        GraphViewChange ApplyProjectionChange(GraphViewChange change)
        {
            if (m_Populating || m_ProjectionBinding == null)
                return change;
            if (m_ProjectionBinding.Mutation.ReadOnly)
                return EmptyChange();
            var removedNodes = new HashSet<GraphAuthoringElementId>(
                (change.elementsToRemove ?? new List<GraphElement>())
                    .OfType<PoseNodeView>()
                    .Select(value => value.Projection.NodeId));
            var requests = new List<GraphAuthoringMutationRequest>();
            foreach (PoseEdgeView edge in
                     (change.elementsToRemove ?? new List<GraphElement>()).OfType<PoseEdgeView>())
            {
                if (!removedNodes.Contains(edge.Projection.SourceNodeId) &&
                    !removedNodes.Contains(edge.Projection.TargetNodeId))
                    requests.Add(new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.DisconnectEdge,
                        edge.Projection.EdgeId));
            }
            foreach (PoseNodeView node in
                     (change.elementsToRemove ?? new List<GraphElement>()).OfType<PoseNodeView>())
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteElement,
                    node.Projection.NodeId));
            foreach (PoseNodeView node in
                     (change.movedElements ?? new List<GraphElement>())
                         .OfType<PoseNodeView>()
                         .GroupBy(value => value.Projection.NodeId)
                         .Select(value => value.Last()))
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.MoveElement,
                    node.Projection.NodeId,
                    position: node.GetPosition().position));
            foreach (Edge edge in change.edgesToCreate ?? new List<Edge>())
            {
                if (!(edge.output?.userData is PosePortBinding first) ||
                    !(edge.input?.userData is PosePortBinding second))
                    throw new InvalidOperationException("Pose Canvas edge endpoints are invalid.");
                PosePortBinding output = first.Direction == GraphAuthoringPortDirection.Output
                    ? first
                    : second;
                PosePortBinding input = first.Direction == GraphAuthoringPortDirection.Input
                    ? first
                    : second;
                GraphAuthoringNodeProjection outputNode = m_PoseNodes[output.NodeId].Projection;
                GraphAuthoringNodeProjection inputNode = m_PoseNodes[input.NodeId].Projection;
                if (!m_ProjectionBinding.ConnectionPolicy.CanConnect(
                        m_ProjectionBinding.Document,
                        outputNode,
                        output.PortId,
                        inputNode,
                        input.PortId))
                    throw new InvalidOperationException(
                        $"Pose Canvas connection '{output.NodeId}:{output.PortId}' to '{input.NodeId}:{input.PortId}' was rejected by the connection policy.");
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.ConnectPorts,
                    sourceNodeId: output.NodeId,
                    sourcePortId: output.PortId,
                    targetNodeId: input.NodeId,
                    targetPortId: input.PortId));
            }
            if (requests.Count != 0)
            {
                m_ProjectionBinding.Mutation.Apply(
                    m_ProjectionBinding.Document,
                    requests);
                schedule.Execute(PopulateProjection);
            }
            return EmptyChange();
        }

        GraphViewChange ApplyStateMachineChange(GraphViewChange change)
        {
            if (m_Populating || m_StateMachineBinding == null)
                return change;
            if (m_StateMachineBinding.Mutation.ReadOnly)
                return EmptyChange();
            var requests = new List<GraphAuthoringMutationRequest>();
            foreach (StateTransitionView transition in
                     (change.elementsToRemove ?? new List<GraphElement>()).OfType<StateTransitionView>())
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteTransition,
                    transition.Projection.TransitionId));
            foreach (StateNodeView state in
                     (change.elementsToRemove ?? new List<GraphElement>()).OfType<StateNodeView>())
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteState,
                    state.Projection.StateId));
            foreach (StateAliasView alias in
                     (change.elementsToRemove ?? new List<GraphElement>()).OfType<StateAliasView>())
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteStateAlias,
                    alias.Projection.AliasId));
            if (m_StateMachineBinding.Policy.PersistsLayout)
            {
                foreach (Node node in
                         (change.movedElements ?? new List<GraphElement>()).OfType<Node>())
                {
                    GraphAuthoringElementId elementId = node switch
                    {
                        StateEntryView entry => entry.Projection.ElementId,
                        StateNodeView state => state.Projection.StateId,
                        StateAliasView alias => alias.Projection.AliasId,
                        _ => default
                    };
                    if (elementId.IsValid)
                        requests.Add(new GraphAuthoringMutationRequest(
                            GraphAuthoringMutationKind.MoveElement,
                            elementId,
                            position: node.GetPosition().position));
                }
            }
            foreach (Edge edge in change.edgesToCreate ?? new List<Edge>())
            {
                if (!(edge.output?.userData is StatePortBinding first) ||
                    !(edge.input?.userData is StatePortBinding second))
                    throw new InvalidOperationException("Pose StateMachine transition endpoints are invalid.");
                GraphAuthoringElementId source =
                    first.Direction == GraphAuthoringPortDirection.Output
                        ? first.OwnerId
                        : second.OwnerId;
                GraphAuthoringElementId target =
                    first.Direction == GraphAuthoringPortDirection.Input
                        ? first.OwnerId
                        : second.OwnerId;
                if (!m_StateMachineBinding.Policy.CanCreateTransition(
                        m_StateMachineBinding.Document,
                        source,
                        target))
                    throw new InvalidOperationException(
                        $"Pose StateMachine transition '{source}' to '{target}' was rejected by the policy.");
                object payload = m_StateMachineBinding.Policy.CreateTransitionPayload(
                    m_StateMachineBinding.Document,
                    source,
                    target);
                bool entryLink = source.Equals(
                    m_StateMachineBinding.Document.Entry.ElementId);
                if (!entryLink && payload == null)
                    continue;
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateTransition,
                    source,
                    secondaryTargetId: target,
                    value: payload));
            }
            if (requests.Count != 0)
            {
                m_StateMachineBinding.Mutation.Apply(
                    m_StateMachineBinding.Document,
                    requests);
                schedule.Execute(PopulateStateMachine);
            }
            return EmptyChange();
        }

        void ClearGraphElements()
        {
            m_PoseNodes.Clear();
            m_StateNodes.Clear();
            m_StateAliases.Clear();
            m_StateInputs.Clear();
            m_StateOutputs.Clear();
            DeleteElements(graphElements.ToList());
        }

        static GraphViewChange EmptyChange() => new GraphViewChange
        {
            elementsToRemove = new List<GraphElement>(),
            edgesToCreate = new List<Edge>(),
            movedElements = new List<GraphElement>()
        };
    }
}
