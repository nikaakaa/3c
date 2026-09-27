using BTSMTL.Authoring.Editor;
using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseDocumentCanvas : FlowCanvas.FlowGraph
    {
        // These are editor views of the original state/rule document, never another saved graph.
        [NonSerialized] internal CharacterPoseCanvasBinding Binding;
        [NonSerialized] internal GraphAuthoringStateMachineBinding StateMachineBinding;
        [NonSerialized] internal GraphAuthoringProjectionCanvasBinding ProjectionBinding;
        internal bool PersistsLayout => StateMachineBinding?.Policy.PersistsLayout ?? ProjectionBinding.PersistsLayout;
        public override Type baseNodeType => typeof(CharacterPoseDocumentCanvasNode);
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool isTree => false;
        public override PlanarDirection flowDirection => PlanarDirection.Horizontal;
        public override bool allowBlackboardOverrides => false;
        public override bool canAcceptVariableDrops => false;
        public override bool persistsEditorGraph => false;
        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override UnityEngine.Object EditorUndoTarget => null;
        public override bool isEditorReadOnly => StateMachineBinding?.Mutation.ReadOnly ?? ProjectionBinding.Mutation.ReadOnly;
        internal void Apply(GraphAuthoringMutationRequest request)
        {
            if (StateMachineBinding != null) StateMachineBinding.Mutation.Apply(StateMachineBinding.Document, request);
            else ProjectionBinding.Mutation.Apply(ProjectionBinding.Document, request);
            RefreshDocument();
            NodeCanvas.Editor.GraphEditor.current?.Repaint();
        }

        public override bool HandleEditorCommand(string command, Vector2 position) => Binding.HandleCommand(command, position);
        protected override GenericMenu OnNodesContextMenu(GenericMenu menu, Node[] nodes) =>
            CharacterPoseCanvasCommands.Menu(HandleEditorCommand, nodes.Length == 0 ? Vector2.zero : nodes[0].position, CanExecuteCommand);

        internal bool CanExecuteCommand(string command) => StateMachineBinding != null
            ? (command is "Delete" or "SoftDelete") && !StateMachineBinding.Mutation.ReadOnly && CharacterPoseCanvasCommands.Selection(this).Count != 0
            : CharacterPoseCanvasCommands.CanExecute(ProjectionBinding, this, command);

        internal void RefreshDocument()
        {
            List<GraphAuthoringSelection> selection = CharacterPoseCanvasCommands.Selection(this).ToList();
            var previous = allNodes.OfType<CharacterPoseDocumentCanvasNode>().ToDictionary(value => value.ElementId);
            var previousEdges = allNodes.SelectMany(node => node.outConnections).OfType<CharacterPoseDocumentCanvasConnection>()
                .ToDictionary(value => value.ElementId);
            var labels = new Dictionary<GraphAuthoringElementId, string>();
            var nodes = new List<CharacterPoseDocumentCanvasNode>();
            var edges = new List<GraphAuthoringEdgeProjection>();
            if (StateMachineBinding is GraphAuthoringStateMachineBinding state)
            {
                GraphAuthoringStateMachineEntryProjection entry = state.Document.Entry;
                Add(entry.ElementId, "Entry", entry.Position, GraphAuthoringSelectionKind.State,
                    null, true, false, false, false);
                foreach (GraphAuthoringStateProjection value in state.Document.States)
                    Add(value.StateId, value.DisplayName, value.Position, GraphAuthoringSelectionKind.State,
                        null, false, false, true, value.ChildGraphId.IsValid);
                foreach (GraphAuthoringStateAliasProjection value in state.Document.Aliases)
                    Add(value.AliasId, value.DisplayName, value.Position, GraphAuthoringSelectionKind.State,
                        null, false, true, true, false);
                if (entry.TargetStateId.IsValid)
                    edges.Add(Edge(new GraphAuthoringElementId("entry-link"), entry.ElementId, entry.TargetStateId));
                foreach (GraphAuthoringTransitionProjection value in state.Document.Transitions)
                {
                    edges.Add(Edge(value.TransitionId, value.SourceStateId, value.TargetStateId));
                    var payload = (CharacterPoseTransitionPayload)value.Payload;
                    labels.Add(value.TransitionId, $"优先级 {value.Priority} · {payload.DurationSeconds:0.###} s");
                }
            }
            else
            {
                GraphAuthoringProjectionCanvasBinding projection = ProjectionBinding;
                foreach (GraphAuthoringNodeProjection value in projection.Document.Nodes)
                {
                    GraphAuthoringCapabilityDescriptor capability = projection.Capabilities.Require(value.CapabilityId,
                        projection.Document.DomainId, projection.Document.DocumentRoleId);
                    GraphAuthoringDynamicPortProjection[] ports = capability.FixedPorts.Select(port =>
                        new GraphAuthoringDynamicPortProjection(port.PortId, port.DisplayName, port.ValueTypeId,
                            port.Direction, port.Capacity, port.Required, port.Order, port.InterfacePortId))
                        .Concat(value.DynamicPorts).OrderBy(port => port.Order).ToArray();
                    Add(value.NodeId, value.DisplayName, value.Position, GraphAuthoringSelectionKind.Node,
                        ports, false, false, !capability.SystemOwned, capability.ChildSurfaces.Count != 0);
                }
                edges.AddRange(projection.Document.Edges);
            }
            allNodes = nodes.Cast<Node>().ToList();
            var index = nodes.ToDictionary(value => value.ElementId);
            foreach (GraphAuthoringEdgeProjection edge in edges)
            {
                if (!previousEdges.TryGetValue(edge.EdgeId, out CharacterPoseDocumentCanvasConnection connection))
                    connection = new CharacterPoseDocumentCanvasConnection();
                GraphAuthoringEdgeProjection[] parallel = edges.Where(value => value.SourceNodeId.Equals(edge.SourceNodeId) &&
                    value.TargetNodeId.Equals(edge.TargetNodeId)).ToArray();
                int ordinal = Array.FindIndex(parallel, value => value.EdgeId.Equals(edge.EdgeId));
                float labelPosition = parallel.Length == 1 ? 0.55f : 0.3f + 0.4f * ordinal / (parallel.Length - 1);
                connection.Configure(edge, index[edge.SourceNodeId], index[edge.TargetNodeId],
                    labels.TryGetValue(edge.EdgeId, out string label) ? label : null, labelPosition);
                connection.sourceNode.outConnections.Add(connection);
                connection.targetNode.inConnections.Add(connection);
            }
            GetGraphSource().Pack(this).Unpack(this);
            foreach (var node in nodes) node.GatherPorts();
            if (NodeCanvas.Editor.GraphEditor.currentGraph == this) Binding.RestoreSelection(selection);

            void Add(GraphAuthoringElementId id, string label, Vector2 position, GraphAuthoringSelectionKind kind,
                GraphAuthoringDynamicPortProjection[] ports, bool entry, bool alias, bool deletable, bool child)
            {
                bool existed = previous.TryGetValue(id, out CharacterPoseDocumentCanvasNode node);
                if (!existed)
                    node = new CharacterPoseDocumentCanvasNode();
                if (existed && !PersistsLayout)
                    position = node.position;
                node.Configure(id, label, position, kind, ports, entry, alias, deletable, child);
                nodes.Add(node);
            }
        }

        static GraphAuthoringEdgeProjection Edge(GraphAuthoringElementId id, GraphAuthoringElementId source, GraphAuthoringElementId target) =>
            new(id, source, new GraphAuthoringPortId("output"), target, new GraphAuthoringPortId("input"));

        public override GenericMenu GetNodesMenu(Vector2 position, FlowCanvas.Port context, UnityEngine.Object dropInstance)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("创建"), false, () => Binding.RequestCreation(position));
            return menu;
        }

        public override FlowCanvas.BinderConnection CreatePortConnection(FlowCanvas.Port source, FlowCanvas.Port target) =>
            (FlowCanvas.BinderConnection)ConnectNodes(source.parent, target.parent,
                CharacterPoseDocumentCanvasNode.PortIndex(source, GraphAuthoringPortDirection.Output),
                CharacterPoseDocumentCanvasNode.PortIndex(target, GraphAuthoringPortDirection.Input));

        public override void DisconnectPort(FlowCanvas.Port port)
        {
            var edges = port.GetPortConnections().Cast<CharacterPoseDocumentCanvasConnection>().ToArray();
            if (edges.Any(edge => edge.Source.IsEntry))
                throw new InvalidOperationException("Set the Entry target by connecting it to another state.");
            var requests = edges.Select(edge => new GraphAuthoringMutationRequest(StateMachineBinding == null
                ? GraphAuthoringMutationKind.DisconnectEdge : GraphAuthoringMutationKind.DeleteTransition, edge.ElementId)).ToArray();
            if (requests.Length == 0) return;
            if (StateMachineBinding != null)
                StateMachineBinding.Mutation.Apply(StateMachineBinding.Document, requests);
            else ProjectionBinding.Mutation.Apply(ProjectionBinding.Document, requests);
            RefreshDocument();
        }

        protected override void OnGraphInitialize() => throw new InvalidOperationException("Pose document views do not execute.");

        public override Node AddNode(Type nodeType, Vector2 pos = default) =>
            throw new InvalidOperationException("Create document nodes through their registered authoring menu.");

        public override GenericMenu GetNodeSelectionMenu(NodeCreationRequestContext request)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("创建"), false, () => Binding.RequestCreation(request.position));
            return menu;
        }

        public override void RemoveNode(Node node, bool recordUndo = true, bool force = false)
        {
            var value = (CharacterPoseDocumentCanvasNode)node;
            if (!value.Deletable)
                throw new InvalidOperationException("This document element is system owned.");
            GraphAuthoringMutationKind kind = StateMachineBinding == null ? GraphAuthoringMutationKind.DeleteElement
                : value.IsAlias ? GraphAuthoringMutationKind.DeleteStateAlias : GraphAuthoringMutationKind.DeleteState;
            Apply(new GraphAuthoringMutationRequest(kind, value.ElementId));
        }

        public override Connection ConnectNodes(Node sourceNode, Node targetNode, int sourceIndex = -1, int targetIndex = -1)
        {
            var source = (CharacterPoseDocumentCanvasNode)sourceNode;
            var target = (CharacterPoseDocumentCanvasNode)targetNode;
            if (StateMachineBinding is GraphAuthoringStateMachineBinding state)
            {
                if (!state.Policy.CanCreateTransition(state.Document, source.ElementId, target.ElementId))
                    throw new InvalidOperationException("This state transition is not permitted.");
                object payload = state.Policy.CreateTransitionPayload(state.Document, source.ElementId, target.ElementId);
                if (!source.IsEntry && payload == null)
                    return null;
                Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.CreateTransition,
                    source.ElementId, secondaryTargetId: target.ElementId, value: payload));
            }
            else
            {
                GraphAuthoringProjectionCanvasBinding projection = ProjectionBinding;
                GraphAuthoringDynamicPortProjection output = RequirePort(source, GraphAuthoringPortDirection.Output, sourceIndex);
                GraphAuthoringDynamicPortProjection input = RequirePort(target, GraphAuthoringPortDirection.Input, targetIndex);
                if (!projection.ConnectionPolicy.CanConnect(projection.Document,
                    projection.Document.Nodes.Single(value => value.NodeId.Equals(source.ElementId)), output.PortId,
                    projection.Document.Nodes.Single(value => value.NodeId.Equals(target.ElementId)), input.PortId))
                    throw new InvalidOperationException("This rule connection is not permitted.");
                Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.ConnectPorts,
                    sourceNodeId: source.ElementId, sourcePortId: output.PortId,
                    targetNodeId: target.ElementId, targetPortId: input.PortId));
            }
            return allNodes.OfType<CharacterPoseDocumentCanvasNode>().Single(value => value.ElementId.Equals(source.ElementId))
                .outConnections.LastOrDefault(value => ((CharacterPoseDocumentCanvasNode)value.targetNode).ElementId.Equals(target.ElementId));
        }

        internal static GraphAuthoringDynamicPortProjection RequirePort(CharacterPoseDocumentCanvasNode node,
            GraphAuthoringPortDirection direction, int index)
        {
            GraphAuthoringDynamicPortProjection[] ports = node.Ports.Where(value => value.Direction == direction).ToArray();
            if (index == -1 && ports.Length == 1)
                index = 0;
            if (index < 0 || index >= ports.Length)
                throw new InvalidOperationException("Select an explicit document port.");
            return ports[index];
        }

        public override void RemoveConnection(Connection connection, bool recordUndo = true)
        {
            var edge = (CharacterPoseDocumentCanvasConnection)connection;
            if (edge.Source.IsEntry)
                throw new InvalidOperationException("Set the Entry target by connecting it to another state.");
            Apply(new GraphAuthoringMutationRequest(StateMachineBinding == null
                ? GraphAuthoringMutationKind.DisconnectEdge : GraphAuthoringMutationKind.DeleteTransition, edge.ElementId));
        }

        public override Node AttachDuplicatedNode(Node newNode) =>
            throw new InvalidOperationException("Document node duplication uses the document clipboard.");

        public override Connection AttachDuplicatedConnection(Connection original, Node newSource, Node newTarget) =>
            throw new InvalidOperationException("Document connections are written through their authoring policy.");
    }

    [Serializable]
    internal sealed class CharacterPoseDocumentCanvasNode : FlowCanvas.FlowNode
    {
        [NonSerialized] internal GraphAuthoringElementId ElementId;
        public override string UID => ElementId.Value;
        [NonSerialized] internal GraphAuthoringSelectionKind SelectionKind;
        [NonSerialized] internal GraphAuthoringDynamicPortProjection[] Ports;
        [NonSerialized] internal bool IsEntry;
        [NonSerialized] internal bool IsAlias;
        [NonSerialized] internal bool Deletable;
        [NonSerialized] internal bool HasChild;
        [NonSerialized] string m_Title;
        public override string name { get => m_Title; set { } }
        public override Alignment2x2 iconAlignment => Alignment2x2.Default;
        protected override bool useDefaultInspectorHeader => false;

        public override Vector2 position
        {
            get => base.position;
            set
            {
                if (base.position == value)
                    return;
                if (graph is CharacterPoseDocumentCanvas canvas && canvas.Binding != null && canvas.PersistsLayout)
                    canvas.Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.MoveElement,
                        ElementId, position: value));
                else
                    base.position = value;
            }
        }

        internal void Configure(GraphAuthoringElementId id, string title, Vector2 position, GraphAuthoringSelectionKind kind,
            GraphAuthoringDynamicPortProjection[] ports, bool entry, bool alias, bool deletable, bool child)
        {
            ElementId = id;
            m_Title = title;
            SelectionKind = kind;
            IsEntry = entry;
            IsAlias = alias;
            Deletable = deletable;
            HasChild = child;
            Ports = ports ?? StatePorts(entry, alias);
            base.position = position;
            inConnections.Clear();
            outConnections.Clear();
        }

        static GraphAuthoringDynamicPortProjection[] StatePorts(bool entry, bool alias)
        {
            var ports = new List<GraphAuthoringDynamicPortProjection>();
            if (!entry && !alias)
                ports.Add(new GraphAuthoringDynamicPortProjection(new GraphAuthoringPortId("input"), "进入", "pose.state",
                    GraphAuthoringPortDirection.Input, GraphAuthoringPortCapacity.Multiple, false, 0));
            ports.Add(new GraphAuthoringDynamicPortProjection(new GraphAuthoringPortId("output"), "转移", "pose.state",
                GraphAuthoringPortDirection.Output, GraphAuthoringPortCapacity.Multiple, false, 0));
            return ports.ToArray();
        }

        protected override void OnNodeGUI()
        {
            base.OnNodeGUI();
            if (HasChild && GUILayout.Button("打开子图"))
                TryOpenEditorChild();
        }

        public override bool TryOpenEditorChild()
        {
            if (!HasChild) return false;
            NodeCanvas.Editor.GraphEditor.OpenEditorChild(this,
                () => ((CharacterPoseDocumentCanvas)graph).Binding.OpenChild(ElementId));
            return true;
        }

        sealed class StatePort { }
        sealed class RulePort { }
        public override bool allowPortIdentityAliases => false;
        public override bool ignoreSelfInstancePortAssignment => true;
        public override bool CanAcceptPortConnection(FlowCanvas.Port port) =>
            Ports.Single(value => value.PortId.Value == port.ID &&
                (value.Direction == GraphAuthoringPortDirection.Input) == port.IsInputPort()).Capacity == GraphAuthoringPortCapacity.Multiple || !port.isConnected;

        protected override void RegisterPorts()
        {
            foreach (var port in Ports ?? Array.Empty<GraphAuthoringDynamicPortProjection>())
            {
                Type type = PortType(port);
                if (port.Direction == GraphAuthoringPortDirection.Input) AddValueInput(port.DisplayName, type, port.PortId.Value);
                else AddValueOutput(port.DisplayName, type, () => throw new InvalidOperationException("Author ports do not execute."), port.PortId.Value);
            }
        }

        internal static Type PortType(GraphAuthoringDynamicPortProjection port) => port.ValueTypeId switch
        {
            "pose.state" => typeof(StatePort),
            "pose.rule.value" => typeof(RulePort),
            _ => throw new InvalidOperationException($"Unknown document port type '{port.ValueTypeId}'.")
        };

        internal static int PortIndex(FlowCanvas.Port port, GraphAuthoringPortDirection direction) =>
            Array.FindIndex(((CharacterPoseDocumentCanvasNode)port.parent).Ports.Where(value => value.Direction == direction).ToArray(),
                value => value.PortId.Value == port.ID);

        protected override void OnNodeInspectorGUI() => EditorGUILayout.LabelField(m_Title);
        protected override GenericMenu OnContextMenu(GenericMenu menu) =>
            CharacterPoseCanvasCommands.Menu(((CharacterPoseDocumentCanvas)graph).HandleEditorCommand, position, ((CharacterPoseDocumentCanvas)graph).CanExecuteCommand);
    }

    [Serializable]
    internal sealed class CharacterPoseDocumentCanvasConnection : FlowCanvas.BinderConnection
    {
        [NonSerialized] internal GraphAuthoringElementId ElementId;
        public override string UID => ElementId.Value;
        [NonSerialized] internal string SourcePort;
        [NonSerialized] internal string TargetPort;
        internal CharacterPoseDocumentCanvasNode Source => (CharacterPoseDocumentCanvasNode)sourceNode;
        internal CharacterPoseDocumentCanvasNode Target => (CharacterPoseDocumentCanvasNode)targetNode;

        public CharacterPoseDocumentCanvasConnection() { }

        [NonSerialized] string m_Label;
        [NonSerialized] float m_LabelPosition;
        internal void Configure(GraphAuthoringEdgeProjection edge, CharacterPoseDocumentCanvasNode source,
            CharacterPoseDocumentCanvasNode target, string label, float labelPosition)
        {
            ElementId = edge.EdgeId;
            SourcePort = edge.SourcePortId.Value;
            TargetPort = edge.TargetPortId.Value;
            sourceNode = source;
            targetNode = target;
            m_Label = label;
            m_LabelPosition = labelPosition;
            InvalidatePortReferences();
        }

        public override string editorLabel => m_Label == null ? null :
            NodeCanvas.Editor.GraphEditorUtility.activeElement == this ? m_Label : "◆";
        public override float editorLabelPosition => m_LabelPosition;

        protected override string serializedSourcePortID { get => SourcePort; set => SourcePort = value; }
        protected override string serializedTargetPortID { get => TargetPort; set => TargetPort = value; }
        public override Type bindingType => CharacterPoseDocumentCanvasNode.PortType(
            Source.Ports.Single(port => port.Direction == GraphAuthoringPortDirection.Output && port.PortId.Value == SourcePort));
        public override TipConnectionStyle tipConnectionStyle =>
            ((CharacterPoseDocumentCanvas)graph).StateMachineBinding != null ? TipConnectionStyle.Arrow : TipConnectionStyle.None;
        public override bool TryOpenEditorChild()
        {
            var binding = ((CharacterPoseDocumentCanvas)graph).StateMachineBinding;
            if (binding == null || Source.IsEntry) return false;
            binding.Policy.OpenTransitionRule(binding.Document, ElementId);
            return true;
        }

        public override void Bind() => throw new InvalidOperationException("Author connections do not execute.");
        public override void UnBind() { }
        public override void SetSourcePort(FlowCanvas.Port port) => CharacterPoseCanvasInteraction.Apply(() =>
            Relink((CharacterPoseDocumentCanvasNode)port.parent, port.ID, Target, TargetPort));
        public override void SetTargetPort(FlowCanvas.Port port) => CharacterPoseCanvasInteraction.Apply(() =>
            Relink(Source, SourcePort, (CharacterPoseDocumentCanvasNode)port.parent, port.ID));

        void Relink(CharacterPoseDocumentCanvasNode source, string sourcePort, CharacterPoseDocumentCanvasNode target, string targetPort)
        {
            var canvas = (CharacterPoseDocumentCanvas)graph;
            if (source.graph != canvas || target.graph != canvas) throw new InvalidOperationException("只能改接当前图中的节点。");
            if (canvas.StateMachineBinding is GraphAuthoringStateMachineBinding state)
            {
                if (Source.IsEntry)
                {
                    if (source != Source) throw new InvalidOperationException("Entry 连线的来源不能改变。");
                    canvas.ConnectNodes(source, target, 0, 0);
                    return;
                }
                if (source.IsEntry || target.IsEntry || target.IsAlias) throw new InvalidOperationException("转换来源必须是状态或别名，目标必须是状态。");
                var requests = new List<GraphAuthoringMutationRequest>();
                if (source != Source)
                {
                    CharacterPoseStateTransitionSource value = source.IsAlias
                        ? CharacterPoseStateTransitionSource.FromAlias(new PoseStateAliasId(source.ElementId.Value))
                        : CharacterPoseStateTransitionSource.FromState(new PoseStateId(source.ElementId.Value));
                    requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.SetTransitionField,
                        ElementId, fieldId: new GraphAuthoringFieldId("source"), value: value));
                }
                if (target != Target)
                    requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.SetTransitionField,
                        ElementId, fieldId: new GraphAuthoringFieldId("target-state-id"), value: target.ElementId.Value));
                if (requests.Count != 0) state.Mutation.Apply(state.Document, requests);
            }
            else
            {
                GraphAuthoringProjectionCanvasBinding projection = canvas.ProjectionBinding;
                var output = new GraphAuthoringPortId(sourcePort);
                var input = new GraphAuthoringPortId(targetPort);
                projection.Mutation.Apply(projection.Document, new[] {
                    new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DisconnectEdge, ElementId),
                    new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.ConnectPorts,
                        sourceNodeId: source.ElementId, sourcePortId: output, targetNodeId: target.ElementId, targetPortId: input)
                });
            }
            canvas.RefreshDocument();
        }

        public override int SetSourceNode(Node source, int index = -1)
        {
            var node = (CharacterPoseDocumentCanvasNode)source;
            var port = CharacterPoseDocumentCanvas.RequirePort(node, GraphAuthoringPortDirection.Output, index);
            SetSourcePort(node.GetOutputPort(port.PortId.Value));
            return node.outConnections.IndexOf(this);
        }
        public override int SetTargetNode(Node target, int index = -1)
        {
            var node = (CharacterPoseDocumentCanvasNode)target;
            var port = CharacterPoseDocumentCanvas.RequirePort(node, GraphAuthoringPortDirection.Input, index);
            SetTargetPort(node.GetInputPort(port.PortId.Value));
            return Source.outConnections.IndexOf(this);
        }

    }
}
