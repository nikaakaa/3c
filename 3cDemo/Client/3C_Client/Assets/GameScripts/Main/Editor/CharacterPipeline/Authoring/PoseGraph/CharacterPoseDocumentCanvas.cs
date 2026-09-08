using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseDocumentCanvas : NodeCanvas.Framework.Graph
    {
        // These are editor views of the original state/rule document, never another saved graph.
        [NonSerialized] internal CharacterPoseCanvasBinding Binding;
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
        public override bool isEditorReadOnly => Binding.StateMachineBinding?.Mutation.ReadOnly ?? Binding.ProjectionBinding.Mutation.ReadOnly;
        public override bool HandleEditorCommand(string command, Vector2 position) => Binding.HandleCommand(command, position);
        protected override GenericMenu OnNodesContextMenu(GenericMenu menu, Node[] nodes) =>
            CharacterPoseCanvasCommands.Menu(HandleEditorCommand, nodes.Length == 0 ? Vector2.zero : nodes[0].position);

        internal void RefreshDocument()
        {
            List<GraphAuthoringSelection> selection = Binding.GetStableSelection().ToList();
            var previous = allNodes.OfType<CharacterPoseDocumentCanvasNode>().ToDictionary(value => value.ElementId);
            var nodes = new List<CharacterPoseDocumentCanvasNode>();
            var edges = new List<GraphAuthoringEdgeProjection>();
            if (Binding.StateMachineBinding is GraphAuthoringStateMachineBinding state)
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
                    edges.Add(Edge(value.TransitionId, value.SourceStateId, value.TargetStateId));
            }
            else
            {
                GraphAuthoringProjectionCanvasBinding projection = Binding.ProjectionBinding;
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
                var connection = new CharacterPoseDocumentCanvasConnection(edge,
                    index[edge.SourceNodeId], index[edge.TargetNodeId]);
                connection.sourceNode.outConnections.Add(connection);
                connection.targetNode.inConnections.Add(connection);
            }
            GetGraphSource().Pack(this).Unpack(this);
            Binding.RestoreSelection(selection);

            void Add(GraphAuthoringElementId id, string label, Vector2 position, GraphAuthoringSelectionKind kind,
                GraphAuthoringDynamicPortProjection[] ports, bool entry, bool alias, bool deletable, bool child)
            {
                bool existed = previous.TryGetValue(id, out CharacterPoseDocumentCanvasNode node);
                if (!existed)
                    node = new CharacterPoseDocumentCanvasNode();
                if (existed && !Binding.PersistsLayout)
                    position = node.position;
                node.Configure(id, label, position, kind, ports, entry, alias, deletable, child);
                nodes.Add(node);
            }
        }

        static GraphAuthoringEdgeProjection Edge(GraphAuthoringElementId id, GraphAuthoringElementId source, GraphAuthoringElementId target) =>
            new(id, source, new GraphAuthoringPortId("output"), target, new GraphAuthoringPortId("input"));

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
            GraphAuthoringMutationKind kind = Binding.StateMachineBinding == null ? GraphAuthoringMutationKind.DeleteElement
                : value.IsAlias ? GraphAuthoringMutationKind.DeleteStateAlias : GraphAuthoringMutationKind.DeleteState;
            Binding.Apply(new GraphAuthoringMutationRequest(kind, value.ElementId));
        }

        public override Connection ConnectNodes(Node sourceNode, Node targetNode, int sourceIndex = -1, int targetIndex = -1)
        {
            var source = (CharacterPoseDocumentCanvasNode)sourceNode;
            var target = (CharacterPoseDocumentCanvasNode)targetNode;
            if (Binding.StateMachineBinding is GraphAuthoringStateMachineBinding state)
            {
                if (!state.Policy.CanCreateTransition(state.Document, source.ElementId, target.ElementId))
                    throw new InvalidOperationException("This state transition is not permitted.");
                object payload = state.Policy.CreateTransitionPayload(state.Document, source.ElementId, target.ElementId);
                if (!source.IsEntry && payload == null)
                    return null;
                Binding.Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.CreateTransition,
                    source.ElementId, secondaryTargetId: target.ElementId, value: payload));
            }
            else
            {
                GraphAuthoringProjectionCanvasBinding projection = Binding.ProjectionBinding;
                GraphAuthoringDynamicPortProjection output = RequirePort(source, GraphAuthoringPortDirection.Output, sourceIndex);
                GraphAuthoringDynamicPortProjection input = RequirePort(target, GraphAuthoringPortDirection.Input, targetIndex);
                if (!projection.ConnectionPolicy.CanConnect(projection.Document,
                    projection.Document.Nodes.Single(value => value.NodeId.Equals(source.ElementId)), output.PortId,
                    projection.Document.Nodes.Single(value => value.NodeId.Equals(target.ElementId)), input.PortId))
                    throw new InvalidOperationException("This rule connection is not permitted.");
                Binding.Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.ConnectPorts,
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
            Binding.Apply(new GraphAuthoringMutationRequest(Binding.StateMachineBinding == null
                ? GraphAuthoringMutationKind.DisconnectEdge : GraphAuthoringMutationKind.DeleteTransition, edge.ElementId));
        }

        public override Node AttachDuplicatedNode(Node newNode) =>
            throw new InvalidOperationException("Document node duplication uses the document clipboard.");

        public override Connection AttachDuplicatedConnection(Connection original, Node newSource, Node newTarget) =>
            throw new InvalidOperationException("Document connections are written through their authoring policy.");
    }

    [Serializable]
    internal sealed class CharacterPoseDocumentCanvasNode : Node
    {
        [NonSerialized] internal GraphAuthoringElementId ElementId;
        [NonSerialized] internal GraphAuthoringSelectionKind SelectionKind;
        [NonSerialized] internal GraphAuthoringDynamicPortProjection[] Ports;
        [NonSerialized] internal bool IsEntry;
        [NonSerialized] internal bool IsAlias;
        [NonSerialized] internal bool Deletable;
        [NonSerialized] internal bool HasChild;
        [NonSerialized] string m_Title;
        public override string name { get => m_Title; set { } }
        public override int maxInConnections => IsEntry ? 0 : -1;
        public override int maxOutConnections => -1;
        public override bool allowAsPrime => false;
        public override bool canSelfConnect => false;
        public override Alignment2x2 commentsAlignment => Alignment2x2.Default;
        public override Alignment2x2 iconAlignment => Alignment2x2.Default;
        public override Type outConnectionType => typeof(CharacterPoseDocumentCanvasConnection);
        protected override bool useDefaultInspectorHeader => false;

        public override Vector2 position
        {
            get => base.position;
            set
            {
                if (base.position == value)
                    return;
                if (graph is CharacterPoseDocumentCanvas canvas && canvas.Binding != null && canvas.Binding.PersistsLayout)
                    canvas.Binding.Apply(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.MoveElement,
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
            CharacterPoseCanvasPortsGUI.DrawBody(this);
            if (HasChild && GUILayout.Button("打开子图"))
                ((CharacterPoseDocumentCanvas)graph).Binding.OpenChild(ElementId);
        }

        protected override void DrawNodeConnections(Rect canvas, bool fullDrawPass, Vector2 mouse, float zoom) =>
            CharacterPoseCanvasPortsGUI.DrawConnections(this, canvas, fullDrawPass, mouse, zoom);

        protected override void OnNodeInspectorGUI() => EditorGUILayout.LabelField(m_Title);
        protected override GenericMenu OnContextMenu(GenericMenu menu) =>
            CharacterPoseCanvasCommands.Menu(((CharacterPoseDocumentCanvas)graph).HandleEditorCommand, position);
    }

    [Serializable]
    internal sealed class CharacterPoseDocumentCanvasConnection : Connection
    {
        [NonSerialized] internal GraphAuthoringElementId ElementId;
        [NonSerialized] internal string SourcePort;
        [NonSerialized] internal string TargetPort;
        internal CharacterPoseDocumentCanvasNode Source => (CharacterPoseDocumentCanvasNode)sourceNode;
        internal CharacterPoseDocumentCanvasNode Target => (CharacterPoseDocumentCanvasNode)targetNode;

        public CharacterPoseDocumentCanvasConnection() { }

        internal CharacterPoseDocumentCanvasConnection(GraphAuthoringEdgeProjection edge,
            CharacterPoseDocumentCanvasNode source, CharacterPoseDocumentCanvasNode target)
        {
            ElementId = edge.EdgeId;
            SourcePort = edge.SourcePortId.Value;
            TargetPort = edge.TargetPortId.Value;
            sourceNode = source;
            targetNode = target;
        }

        protected override string GetConnectionInfo() => ElementId.Value;
        public override int SetSourceNode(Node source, int index = -1) =>
            throw new InvalidOperationException("Edit the transition endpoints through the state machine fields.");
        public override int SetTargetNode(Node target, int index = -1) =>
            throw new InvalidOperationException("Edit the transition endpoints through the state machine fields.");
    }
}
