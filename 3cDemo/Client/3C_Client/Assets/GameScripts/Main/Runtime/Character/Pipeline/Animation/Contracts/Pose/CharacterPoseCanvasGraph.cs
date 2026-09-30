using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.EventGraphs;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
#if UNITY_EDITOR
using NodeCanvas.Framework.Internal;
using UnityEditor;
#endif

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseCanvasGraph : FlowCanvas.FlowGraph
#if UNITY_EDITOR
        , NodeCanvas.Editor.IBlackboardEditorAdapter
#endif
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseAuthoringGraphRole m_Role = CharacterPoseAuthoringGraphRole.AnimGraph;
        [SerializeField] CharacterPoseParameterDeclaration[] m_Parameters =
            Array.Empty<CharacterPoseParameterDeclaration>();

        public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId)
            ? default
            : new PoseGraphId(m_GraphId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public CharacterPoseAuthoringGraphRole Role => m_Role;
        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters =>
            m_Parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
        public IReadOnlyList<CharacterPoseCanvasNode> Nodes =>
            (allNodes ?? new List<Node>()).OfType<CharacterPoseCanvasNode>().ToArray();
        public IReadOnlyList<CharacterPoseCanvasConnection> Connections =>
            Nodes.SelectMany(node => node.outConnections.OfType<CharacterPoseCanvasConnection>())
                .Distinct()
                .OrderBy(connection => connection.EdgeId, StringComparer.Ordinal)
                .ToArray();
        public IReadOnlyList<CharacterPoseCanvasConnection> Edges => Connections;
        public IReadOnlyList<CharacterPoseGraphLayoutEntry> Layout =>
            Nodes.OrderBy(node => node.NodeId)
                .Select(node => new CharacterPoseGraphLayoutEntry(node.NodeId, node.position))
                .ToArray();

        public override Type baseNodeType => typeof(CharacterPoseCanvasNode);
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool isTree => false;
        public override PlanarDirection flowDirection => PlanarDirection.Horizontal;
        public override bool allowBlackboardOverrides => false;
        public override bool canAcceptVariableDrops => true;

        [NonSerialized] ICharacterPoseCanvasNativeRuntime m_NativeRuntime;

        internal ICharacterPoseCanvasNativeRuntime NativeRuntime => m_NativeRuntime;

        internal void AttachNativeRuntime(ICharacterPoseCanvasNativeRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (isRunning)
                throw new InvalidOperationException("Pose native runtime cannot attach to a running graph.");
            if (m_NativeRuntime != null && !ReferenceEquals(m_NativeRuntime, runtime))
                throw new InvalidOperationException("Pose native runtime is already attached.");
            m_NativeRuntime = runtime;
        }

        internal void DetachNativeRuntime(ICharacterPoseCanvasNativeRuntime runtime)
        {
            if (runtime == null || !ReferenceEquals(m_NativeRuntime, runtime))
                throw new InvalidOperationException("Pose native runtime detach does not match the attached runtime.");
            if (isRunning)
                throw new InvalidOperationException("Pose native runtime cannot detach from a running graph.");
            m_NativeRuntime = null;
        }

        protected override void OnGraphInitialize()
        {
            ICharacterPoseCanvasNativeRuntime runtime = m_NativeRuntime ??
                throw new InvalidOperationException("Pose graph requires an attached native runtime instance.");
            CharacterPoseCanvasNode[] nodes = Nodes.ToArray();
            for (int i = 0; i < nodes.Length; i++)
                nodes[i].GatherPorts();
            for (int i = 0; i < nodes.Length; i++)
                nodes[i].BindPorts();
            runtime.Initialize(this);
        }

        protected override void OnGraphStarted() =>
            m_NativeRuntime?.Start(this);

        protected override void OnGraphStoped() =>
            m_NativeRuntime?.Stop(this);

        protected override void OnGraphObjectDestroy()
        {
            m_NativeRuntime?.Dispose();
            m_NativeRuntime = null;
        }

        internal T ReadNativeOutput<T>(
            CharacterPoseCanvasNode node,
            PosePortId portId)
            where T : CharacterPoseNativePortValue
        {
            ICharacterPoseCanvasNativeRuntime runtime = m_NativeRuntime ??
                throw new InvalidOperationException("Pose graph has no attached native runtime instance.");
            return runtime.Read<T>(node, portId);
        }

#if UNITY_EDITOR || KK_DIAGNOSTIC_SAMPLING
        internal bool TryObserveNativeNode(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation)
        {
            observation = default;
            return m_NativeRuntime != null &&
                m_NativeRuntime.TryObserve(nodeId, portId, out observation);
        }
#endif

#if UNITY_EDITOR
        [NonSerialized] BlackboardSource m_EditorBlackboard;
        [NonSerialized] CharacterAnimationVariableContract m_EditorAnimationVariables;

        public override IBlackboard editorBlackboard => GetEditorBlackboard();
        public bool IsReadOnly => true;
        public bool AllowVariablePick => true;

        public void SetEditorAnimationVariables(
            CharacterAnimationVariableContract animationVariables) =>
            m_EditorAnimationVariables = animationVariables;

        BlackboardSource GetEditorBlackboard()
        {
            m_EditorBlackboard ??= new BlackboardSource();
            m_EditorBlackboard.unityContextObject = this;
            var next = new Dictionary<string, Variable>(StringComparer.Ordinal);
            foreach (EventGraphVariableDescriptor descriptor in
                     m_EditorAnimationVariables?.Variables ??
                     Array.Empty<EventGraphVariableDescriptor>())
            {
                string id = descriptor.Reference.VariableId;
                if (!TryGetVariableType(descriptor.ValueKind, out Type variableType) ||
                    !CharacterPoseParameterAccess.IsBlackboardInput(
                        new PoseParameterId(id),
                        CharacterPoseParameterUsage.Control))
                    continue;
                if (!m_EditorBlackboard.variables.TryGetValue(id, out Variable variable) ||
                    variable == null || variable.varType != variableType ||
                    !string.Equals(variable.ID, id, StringComparison.Ordinal))
                    variable = CreateEditorVariable(descriptor);
                else
                {
                    variable.name = descriptor.Name;
                    variable.value = DefaultValue(descriptor);
                }
                next.Add(id, variable);
            }

            bool changed = m_EditorBlackboard.variables.Count != next.Count ||
                next.Any(pair => !m_EditorBlackboard.variables.TryGetValue(pair.Key, out Variable current) ||
                    !ReferenceEquals(current, pair.Value));
            if (changed)
                m_EditorBlackboard.variables = next;
            return m_EditorBlackboard;
        }

        public void DrawBlackboardExtensions(IBlackboard blackboard, UnityEngine.Object contextObject)
        {
            if (ReferenceEquals(blackboard, editorBlackboard))
                EditorGUILayout.LabelField("Animation Inputs · 只读", EditorStyles.miniLabel);
        }

        public GenericMenu GetAddVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject)
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("动画实例变量由 EventGraph 声明提供"));
            return menu;
        }

        public GenericMenu GetVariableMenu(
            IBlackboard blackboard,
            UnityEngine.Object contextObject,
            Variable variable,
            int index)
        {
            var menu = new GenericMenu();
            EventGraphVariableDescriptor descriptor =
                FindEditorVariable(variable?.ID);
            if (descriptor == null)
            {
                menu.AddDisabledItem(new GUIContent("缺少正式 EventGraph 输入声明"));
                return menu;
            }
            menu.AddDisabledItem(new GUIContent(
                $"名称: {descriptor.Name}"));
            menu.AddDisabledItem(new GUIContent(
                $"类型: {descriptor.ValueType.Name}"));
            menu.AddDisabledItem(new GUIContent(
                "来源: 动画实例变量"));
            menu.AddDisabledItem(new GUIContent(
                $"范围: {CharacterPoseParameterAccess.ScopeDisplayName(Role)}"));
            menu.AddDisabledItem(new GUIContent(
                $"使用: {ParameterConsumerCount(new PoseParameterId(descriptor.Reference.VariableId))} 个节点"));
            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("拖拽变量以创建 Get 节点"));
            return menu;
        }

        public void ExecuteMutation(string title, Action mutation) =>
            throw new InvalidOperationException("Pose 参数 Blackboard 只读。");

        public void ApplyVariableList(IBlackboard blackboard, IReadOnlyList<Variable> variables) { }

        protected override void OnVariableDropInGraph(IBlackboard blackboard, Variable variable, Vector2 mousePos)
        {
            if (!ReferenceEquals(blackboard, editorBlackboard) || variable == null)
                return;
            EventGraphVariableDescriptor descriptor = FindEditorVariable(variable.ID) ??
                throw new InvalidOperationException("Blackboard 变量没有对应的 EventGraph 声明。");
            if (isEditorReadOnly || EditorWriteRouter == null)
            {
                NodeCanvas.Editor.GraphEditor.current?.ShowNotification(new GUIContent("Pose Graph 当前不可编辑。"));
                return;
            }
            EditorWriteRouter.CreateParameterGet(
                new PoseParameterId(descriptor.Reference.VariableId),
                descriptor.Name,
                mousePos);
            Event.current.Use();
        }

        EventGraphVariableDescriptor FindEditorVariable(string variableId)
        {
            if (m_EditorAnimationVariables == null || string.IsNullOrWhiteSpace(variableId))
                return null;
            return m_EditorAnimationVariables.Variables.SingleOrDefault(value =>
                value != null &&
                string.Equals(value.Reference.VariableId, variableId, StringComparison.Ordinal));
        }

        int ParameterConsumerCount(PoseParameterId parameterId)
        {
            return Nodes
                .Where(node => node.Payload is CharacterProgramParameterInputPosePayload parameter &&
                               parameter.ParameterId.Equals(parameterId))
                .Sum(node => node.outConnections.Count);
        }

        static bool TryGetVariableType(
            EventGraphValueKind valueKind,
            out Type type)
        {
            switch (valueKind)
            {
                case EventGraphValueKind.Float32:
                    type = typeof(float);
                    return true;
                case EventGraphValueKind.Int32:
                    type = typeof(int);
                    return true;
                case EventGraphValueKind.Bool:
                    type = typeof(bool);
                    return true;
                default:
                    type = null;
                    return false;
            }
        }

        static Variable CreateEditorVariable(EventGraphVariableDescriptor descriptor) =>
            descriptor.ValueKind switch
            {
                EventGraphValueKind.Float32 => new Variable<float>(
                    descriptor.Name,
                    descriptor.Reference.VariableId)
                {
                    value = descriptor.InitialValue.Float32Value
                },
                EventGraphValueKind.Int32 => new Variable<int>(
                    descriptor.Name,
                    descriptor.Reference.VariableId)
                {
                    value = descriptor.InitialValue.Int32Value
                },
                EventGraphValueKind.Bool => new Variable<bool>(
                    descriptor.Name,
                    descriptor.Reference.VariableId)
                {
                    value = descriptor.InitialValue.BoolValue
                },
                _ => throw new InvalidOperationException("EventGraph 变量类型不能作为 Pose 输入。")
            };

        static object DefaultValue(EventGraphVariableDescriptor descriptor) =>
            descriptor.ValueKind switch
            {
                EventGraphValueKind.Float32 => descriptor.InitialValue.Float32Value,
                EventGraphValueKind.Int32 => descriptor.InitialValue.Int32Value,
                EventGraphValueKind.Bool => descriptor.InitialValue.BoolValue,
                _ => throw new InvalidOperationException("EventGraph 变量类型不能作为 Pose 输入.")
            };

        protected override void OnGraphEditorToolbar() =>
            PoseCanvasEditorBridge.Toolbar?.Invoke(this);

        public override FlowCanvas.BinderConnection CreatePortConnection(FlowCanvas.Port source, FlowCanvas.Port target) =>
            (FlowCanvas.BinderConnection)EditorWriteRouter.Connect(
                source.parent, target.parent,
                CharacterPoseCanvasNativePorts.Index(source, CharacterPosePortDirection.Output),
                CharacterPoseCanvasNativePorts.Index(target, CharacterPosePortDirection.Input));

        public override void DisconnectPort(FlowCanvas.Port port) => EditorWriteRouter.DisconnectPort(port);

        public override GenericMenu GetNodesMenu(Vector2 position, FlowCanvas.Port context, UnityEngine.Object dropInstance) =>
            EditorWriteRouter.BuildPortCreationMenu(position, context);

        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override bool isEditorReadOnly => EditorWriteRouter?.ReadOnly ?? true;
        public override bool HandleEditorCommand(string command, Vector2 position) =>
            EditorWriteRouter != null && EditorWriteRouter.HandleCommand(command, position);

        protected override GenericMenu OnNodesContextMenu(GenericMenu menu, Node[] nodes) =>
            EditorWriteRouter.BuildSelectionMenu(nodes.Length == 0 ? Vector2.zero : nodes[0].position);

        protected override GenericMenu OnCanvasContextMenu(GenericMenu menu, Vector2 position)
        {
            GenericMenu creation = EditorWriteRouter.BuildNodeCreationMenu(new NodeCreationRequestContext { position = position });
            creation.AddSeparator("");
            creation.AddItem(new GUIContent("粘贴"), false, () => EditorWriteRouter.HandleCommand("Paste", position));
            return creation;
        }
        [NonSerialized] internal CharacterPoseCanvasEditorWriteRouter EditorWriteRouter;

        public override Node AddNode(Type nodeType, Vector2 pos = default) =>
            EditorWriteRouter != null
                ? EditorWriteRouter.CreateNode(nodeType, pos)
                : throw new InvalidOperationException(
                    "Pose Canvas nodes must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");

        public override void RemoveNode(Node node, bool recordUndo = true, bool force = false)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas nodes must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            EditorWriteRouter.RemoveNode(node);
        }

        public override Connection ConnectNodes(
            Node sourceNode,
            Node targetNode,
            int sourceIndex = -1,
            int targetIndex = -1)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas connections must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            return EditorWriteRouter.Connect(sourceNode, targetNode, sourceIndex, targetIndex);
        }

        public override void RemoveConnection(Connection connection, bool recordUndo = true)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas connections must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            EditorWriteRouter.RemoveConnection(connection);
        }

        public override GenericMenu GetNodeSelectionMenu(NodeCreationRequestContext request)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas node creation requires the Pose Canvas editor session.");
            return EditorWriteRouter.BuildNodeCreationMenu(request);
        }
#endif

        public CharacterPoseCanvasNode RequireNode(PoseNodeId nodeId) =>
            Nodes.SingleOrDefault(node => node.NodeId == nodeId) ??
            throw new InvalidOperationException(
                $"Pose Canvas node '{nodeId}' does not exist in graph '{GraphId}'.");

        public bool TryGetNode(PoseNodeId nodeId, out CharacterPoseCanvasNode node)
        {
            node = Nodes.SingleOrDefault(value => value.NodeId == nodeId);
            return node != null;
        }

        internal CharacterPoseCanvasGraph SetAuthoring(
            PoseGraphId graphId,
            string contentRevision,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPoseCanvasNode[] nodes,
            CharacterPoseCanvasConnection[] connections,
            CharacterPoseAuthoringGraphRole role = CharacterPoseAuthoringGraphRole.AnimGraph)
        {
            m_GraphId = graphId.IsValid
                ? graphId.Value
                : throw new ArgumentException("Pose Canvas graph identity is invalid.", nameof(graphId));
            m_ContentRevision = PoseIdentity.Require(contentRevision, nameof(contentRevision));
            if (!Enum.IsDefined(typeof(CharacterPoseAuthoringGraphRole), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            m_Role = role;
            m_Parameters = parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            var parameterIds = new HashSet<PoseParameterId>();
            foreach (CharacterPoseParameterDeclaration parameter in m_Parameters)
            {
                if (parameter == null)
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains a missing parameter.");
                parameter.RequireValid();
                if (!parameterIds.Add(parameter.ParameterId))
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains duplicate parameter '{parameter.ParameterId}'.");
            }
            CharacterPoseCanvasNode[] nodeValues = nodes ?? Array.Empty<CharacterPoseCanvasNode>();
            CharacterPoseCanvasConnection[] connectionValues =
                connections ?? Array.Empty<CharacterPoseCanvasConnection>();
            RequireNodeSet(nodeValues);
            foreach (CharacterPoseCanvasNode node in nodeValues)
                node.outConnections.Clear();
            foreach (CharacterPoseCanvasNode node in nodeValues)
                node.inConnections.Clear();
            allNodes = nodeValues.Cast<Node>().ToList();
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasConnection connection in connectionValues)
            {
                if (connection == null || !edges.Add(connection.EdgeId))
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains an invalid or duplicate connection.");
                }
                CharacterPoseCanvasNode source = nodeValues.SingleOrDefault(
                    node => node.NodeId == connection.SourceNodeId);
                CharacterPoseCanvasNode target = nodeValues.SingleOrDefault(
                    node => node.NodeId == connection.TargetNodeId);
                if (source == null || target == null)
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' connection '{connection.EdgeId}' targets an unknown node.");
                connection.BindNodes(source, target);
                source.outConnections.Add(connection);
                target.inConnections.Add(connection);
            }
            GetGraphSource().Pack(this).Unpack(this);
#if UNITY_EDITOR
            foreach (CharacterPoseCanvasNode node in nodeValues)
                node.GatherPorts();
#endif
            return this;
        }

        public static CharacterPoseCanvasGraph CreateAuthoring(
            PoseGraphId graphId,
            string contentRevision,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPoseCanvasNode[] nodes,
            CharacterPoseCanvasConnection[] connections,
            IReadOnlyList<CharacterPoseGraphLayoutEntry> layout = null,
            CharacterPoseAuthoringGraphRole role = CharacterPoseAuthoringGraphRole.AnimGraph)
        {
            CharacterPoseCanvasGraph graph = CreateInstance<CharacterPoseCanvasGraph>();
            CharacterPoseCanvasNode[] values = (nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                .Select(node => node?.CloneAuthoring() ??
                    throw new InvalidOperationException("Pose Canvas node is missing."))
                .ToArray();
            CharacterPoseCanvasConnection[] edges = (connections ?? Array.Empty<CharacterPoseCanvasConnection>())
                .Select(connection => connection == null
                    ? throw new InvalidOperationException("Pose Canvas connection is missing.")
                    : new CharacterPoseCanvasConnection(
                        connection.EdgeId,
                        connection.SourceNodeId,
                        connection.SourcePortId,
                        connection.TargetNodeId,
                        connection.TargetPortId))
                .ToArray();
            graph.SetAuthoring(graphId, contentRevision, parameters, values, edges, role);
            foreach (CharacterPoseGraphLayoutEntry entry in layout ?? Array.Empty<CharacterPoseGraphLayoutEntry>())
                graph.SetNodePosition(entry.NodeId, entry.Position);
            return graph;
        }

        internal void SetNodePosition(PoseNodeId nodeId, Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentException("Pose Canvas node position must be finite.", nameof(position));
            RequireNode(nodeId).SetAuthoringPosition(position);
        }

        internal void ApplyAuthoringState(CharacterPoseCanvasGraph candidate)
        {
            var existingNodes = Nodes.ToDictionary(value => value.NodeId);
            CharacterPoseCanvasNode[] nodes = candidate.Nodes.Select(value =>
            {
                if (!existingNodes.TryGetValue(value.NodeId, out CharacterPoseCanvasNode current))
                    return value;
                current.SetAuthoring(value.NodeId, value.DisplayName, value.Payload, value.DynamicPorts.ToArray());
                current.SetAuthoringPosition(value.position);
                return current;
            }).ToArray();
            var nodesById = nodes.ToDictionary(value => value.NodeId);
            var existingEdges = Connections.ToDictionary(value => value.EdgeId, StringComparer.Ordinal);
            CharacterPoseCanvasConnection[] edges = candidate.Connections.Select(value =>
            {
                if (!existingEdges.TryGetValue(value.EdgeId, out CharacterPoseCanvasConnection current))
                    return value;
                current.SetAuthoring(value.EdgeId, nodesById[value.SourceNodeId], value.SourcePortId,
                    nodesById[value.TargetNodeId], value.TargetPortId);
                return current;
            }).ToArray();
            SetAuthoring(candidate.GraphId, candidate.ContentRevision, candidate.Parameters.ToArray(), nodes, edges, candidate.Role);
        }

        public void RequireValid()
        {
            if (!GraphId.IsValid || string.IsNullOrWhiteSpace(ContentRevision))
                throw new InvalidOperationException("Pose Canvas graph identity or revision is invalid.");
            if (!Enum.IsDefined(typeof(CharacterPoseAuthoringGraphRole), Role))
                throw new InvalidOperationException($"Pose Canvas graph '{GraphId}' has an invalid authoring role.");
            CharacterPoseCanvasNode[] nodes = Nodes.ToArray();
            if (allNodes == null || allNodes.Count != nodes.Length)
                throw new InvalidOperationException(
                    $"Pose Canvas graph '{GraphId}' contains a node outside the Pose Canvas node catalog.");
            RequireNodeSet(nodes);
            var nodeSet = new HashSet<CharacterPoseCanvasNode>(nodes);
            var parameterIds = new HashSet<PoseParameterId>();
            foreach (CharacterPoseParameterDeclaration parameter in Parameters)
            {
                if (parameter == null)
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains a missing parameter.");
                parameter.RequireValid();
                if (!parameterIds.Add(parameter.ParameterId))
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains duplicate parameter '{parameter.ParameterId}'.");
            }
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasConnection connection in Connections)
            {
                if (connection == null || string.IsNullOrWhiteSpace(connection.EdgeId) ||
                    !edges.Add(connection.EdgeId) || connection.SourceNode == null ||
                    connection.TargetNode == null || !connection.SourcePortId.IsValid ||
                    !connection.TargetPortId.IsValid ||
                    !nodeSet.Contains(connection.SourceNode) ||
                    !nodeSet.Contains(connection.TargetNode) ||
                    connection.SourceNodeId != connection.SourceNode.NodeId ||
                    connection.TargetNodeId != connection.TargetNode.NodeId)
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains an invalid connection.");
                }
            }
        }

        static void RequireNodeSet(IReadOnlyList<CharacterPoseCanvasNode> nodes)
        {
            var identities = new HashSet<PoseNodeId>();
            for (int i = 0; i < nodes.Count; i++)
            {
                CharacterPoseCanvasNode node = nodes[i];
                if (node == null || !node.NodeId.IsValid || !identities.Add(node.NodeId) ||
                    node.Payload == null || node.Payload.Kind != node.Kind)
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas node #{i} is missing, duplicated or has an invalid payload.");
                }
            }
        }

#if UNITY_EDITOR
        public override Node AttachDuplicatedNode(Node newNode)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas nodes must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            return EditorWriteRouter.DuplicateNode(newNode);
        }

        public override Connection AttachDuplicatedConnection(Connection original, Node newSource, Node newTarget)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas connections must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            return EditorWriteRouter.DuplicateConnection(original, newSource, newTarget);
        }
#endif
    }

#if UNITY_EDITOR
    internal static class PoseCanvasEditorBridge
    {
        internal static System.Action<CharacterPoseCanvasNode> InspectorOverride;
        internal static System.Action<CharacterPoseCanvasGraph> VisualsRefresh;
        internal static System.Action<CharacterPoseCanvasGraph> Toolbar;
        internal static System.Action<CharacterPoseCanvasNode> BodyGUI;
        internal static Func<CharacterPoseCanvasNode, GenericMenu> ContextMenu;
        internal static Func<CharacterPoseCanvasNode, bool> ChildSurface;
    }

    internal interface CharacterPoseCanvasEditorWriteRouter
    {
        bool ReadOnly { get; }
        Node CreateNode(Type nodeType, Vector2 position);
        Node CreateNodeFromCapability(string capabilityIdentity, Vector2 position, Node connectSource, int connectSourcePortIndex, int targetPortIndex = -1);
        void RemoveNode(Node node);
        Connection Connect(Node sourceNode, Node targetNode, int sourceIndex, int targetIndex);
        void RemoveConnection(Connection connection);
        void DisconnectPort(FlowCanvas.Port port);
        Node DuplicateNode(Node clonedNode);
        Connection DuplicateConnection(Connection original, Node newSource, Node newTarget);
        void SetNodeField(Node node, string fieldId, object value);
        void MoveNode(Node node, Vector2 position);
        void RenameNode(Node node, string name);
        Connection Reconnect(Connection connection, Node source, int sourceIndex, Node target, int targetIndex);
        Node CreateParameterGet(PoseParameterId parameterId, string displayName, Vector2 position);
        GenericMenu BuildNodeCreationMenu(NodeCanvas.Framework.Graph.NodeCreationRequestContext request);
        GenericMenu BuildPortCreationMenu(Vector2 position, FlowCanvas.Port context);
        bool HandleCommand(string command, Vector2 position);
        GenericMenu BuildSelectionMenu(Vector2 position);
    }
#endif
}
