#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ParadoxNotion;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public enum BtsmtlSkillFlowGraphRole
    {
        Skill,
        Subgraph,
        StateMachine,
        ConditionRule,
        StateBody,
        TimelineBody
    }

    public interface IBtsmtlSkillAuthoringGraph
    {
        string AuthoringId { get; }
        BtsmtlSkillFlowGraphRole Role { get; }
    }

    public interface IBtsmtlSkillFlowGraph : IBtsmtlSkillAuthoringGraph
    {
        new string AuthoringId { get; }
        new BtsmtlSkillFlowGraphRole Role { get; }
        IReadOnlyList<BtsmtlSkillBlackboardDeclaration> BlackboardDeclarations { get; }
        void SetBlackboardDeclarations(IEnumerable<BtsmtlSkillBlackboardDeclaration> declarations);
        List<Node> DuplicateStructure(List<Node> nodes, Vector2 position);
    }

    public static class BtsmtlSkillFlowGraphRules
    {
        public static bool Allows(Type nodeType, BtsmtlSkillFlowGraphRole role, bool macro)
        {
            if (!BtsmtlSkillCapabilityCatalog.TryGetKind(nodeType, out _))
                return false;
            if (typeof(MacroInputNode).IsAssignableFrom(nodeType) ||
                typeof(MacroOutputNode).IsAssignableFrom(nodeType))
                return macro;
            if (nodeType == typeof(MacroNodeWrapper))
                return role != BtsmtlSkillFlowGraphRole.StateMachine && role != BtsmtlSkillFlowGraphRole.ConditionRule;
            if (BtsmtlSkillNativeNodeCatalog.TryGet(nodeType, out _))
                return role != BtsmtlSkillFlowGraphRole.StateMachine;
            if (!typeof(BtsmtlSkillFlowNode).IsAssignableFrom(nodeType))
                return false;
            if (nodeType == typeof(BtsmtlSkillRootFlowNode))
                return role == BtsmtlSkillFlowGraphRole.Skill || role == BtsmtlSkillFlowGraphRole.StateBody || role == BtsmtlSkillFlowGraphRole.TimelineBody;
            if (typeof(BtsmtlSkillTimelineHookFlowNode).IsAssignableFrom(nodeType))
                return role == BtsmtlSkillFlowGraphRole.TimelineBody;
            if (typeof(BtsmtlSkillStateLifecycleFlowNode).IsAssignableFrom(nodeType))
                return (nodeType == typeof(BtsmtlSkillStateOnEnterFlowNode) ||
                        nodeType == typeof(BtsmtlSkillStateOnExitFlowNode)) &&
                    role == BtsmtlSkillFlowGraphRole.StateBody;
            if (nodeType == typeof(BtsmtlSkillSucceedFlowNode))
                return role != BtsmtlSkillFlowGraphRole.StateBody;
            if (nodeType == typeof(BtsmtlSkillConditionResultFlowNode))
                return role == BtsmtlSkillFlowGraphRole.ConditionRule;
            return role switch
            {
                BtsmtlSkillFlowGraphRole.ConditionRule => typeof(IBtsmtlSkillPureValueNode).IsAssignableFrom(nodeType),
                BtsmtlSkillFlowGraphRole.StateMachine => typeof(IBtsmtlSkillStateStructureNode).IsAssignableFrom(nodeType),
                _ => !typeof(IBtsmtlSkillStateStructureNode).IsAssignableFrom(nodeType)
            };
        }

        internal static void EnsureIdentities(FlowGraph graph)
        {
            foreach (Node node in graph.allNodes)
            {
                _ = node.UID;
                foreach (Connection connection in node.outConnections)
                    _ = connection.UID;
            }
        }
    }

    public static class BtsmtlSkillFlowGraphAuthoring
    {
        public static FlowNode ResolveNode(FlowGraph graph, string identity)
        {
            if (graph == null || string.IsNullOrWhiteSpace(identity))
                return null;
            return graph.allNodes
                .OfType<FlowNode>()
                .SingleOrDefault(value => string.Equals(value.UID, identity, StringComparison.Ordinal));
        }

        public static BinderConnection ResolveConnection(FlowGraph graph, string identity)
        {
            if (graph == null || string.IsNullOrWhiteSpace(identity))
                return null;
            return graph.allNodes
                .OfType<FlowNode>()
                .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                .SingleOrDefault(value => string.Equals(value.UID, identity, StringComparison.Ordinal));
        }

        public static FlowNode EnsureNode(
            FlowGraph graph,
            Type nodeType,
            string identity,
            string name,
            Vector2 position)
        {
            if (graph is not IBtsmtlSkillFlowGraph || nodeType == null || !graph.CanAuthorNodeType(nodeType))
                throw new InvalidOperationException("技能Graph节点类型不属于当前正式作者域。");
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("技能Graph节点identity不能为空。", nameof(identity));
            return BtsmtlSkillFlowEditorMutation.Execute(graph, "配置技能Graph节点", () =>
            {
                FlowNode node = ResolveNode(graph, identity);
                if (node == null && BtsmtlSkillCapabilityCatalog.IsAnchor(nodeType))
                    node = graph.allNodes.OfType<FlowNode>()
                        .SingleOrDefault(value => value.GetType() == nodeType);
                if (node == null)
                    node = (FlowNode)graph.AddNode(nodeType, position);
                else if (node.GetType() != nodeType)
                    throw new InvalidOperationException($"技能Graph节点identity '{identity}'的类型不一致。");
                node.ConfigureAuthoringIdentity(identity);
                node.name = string.IsNullOrWhiteSpace(name) ? node.name : name;
                node.position = position;
                node.GatherPorts();
                return node;
            });
        }

        public static BinderConnection EnsureConnection(
            FlowGraph graph,
            FlowNode source,
            string sourcePortId,
            FlowNode target,
            string targetPortId,
            string identity)
        {
            if (graph == null || source == null || target == null)
                throw new ArgumentNullException(nameof(graph));
            if (source.graph != graph || target.graph != graph)
                throw new InvalidOperationException("技能Graph连线端点必须属于同一正式Graph。");
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("技能Graph连线identity不能为空。", nameof(identity));
            Port sourcePort = source.GetOutputPort(sourcePortId);
            Port targetPort = target.GetInputPort(targetPortId);
            if (sourcePort == null || targetPort == null)
                throw new InvalidOperationException($"技能Graph连线identity '{identity}'引用了不存在的端口。");
            BinderConnection existing = ResolveConnection(graph, identity);
            if (existing != null &&
                (sourcePort.type != targetPort.type || sourcePort.IsFlowPort() != targetPort.IsFlowPort()))
                throw new InvalidOperationException($"技能Graph连线identity '{identity}'的端口类型不一致。");
            if (existing == null &&
                !BtsmtlSkillFlowEditorMutation.CanConnect(graph, sourcePort, targetPort, out string reason))
                throw new InvalidOperationException(reason);
            return BtsmtlSkillFlowEditorMutation.Execute(graph, "配置技能Graph连线", () =>
            {
                BinderConnection connection = existing;
                if (connection != null)
                {
                    if (connection.sourcePort != sourcePort)
                        connection.SetSourcePort(sourcePort);
                    if (connection.targetPort != targetPort)
                        connection.SetTargetPort(targetPort);
                    return connection;
                }
                connection = graph.CreatePortConnection(sourcePort, targetPort);
                if (connection == null)
                    throw new InvalidOperationException($"技能Graph连线identity '{identity}'创建失败。");
                connection.ConfigureAuthoringIdentity(identity);
                return connection;
            });
        }

        public static void Prune(
            FlowGraph graph,
            IEnumerable<string> nodeIdentities,
            IEnumerable<string> connectionIdentities)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            var keepNodes = new HashSet<string>(nodeIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var keepConnections = new HashSet<string>(connectionIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            BtsmtlSkillFlowEditorMutation.Execute(graph, "清理技能Graph输出", () =>
            {
                foreach (BinderConnection connection in graph.allNodes
                             .OfType<FlowNode>()
                             .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                             .ToArray())
                    if (!keepConnections.Contains(connection.UID))
                        graph.RemoveConnection(connection, false);
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>().ToArray())
                    if (!BtsmtlSkillCapabilityCatalog.IsAnchor(node.GetType()) &&
                        !keepNodes.Contains(node.UID))
                        graph.RemoveNode(node, false);
            });
        }
    }

    public sealed class BtsmtlSkillFlowGraph : FlowGraph, IBtsmtlSkillFlowGraph, ITimelineTreeGraphAsset, IBlackboardEditorAdapter
    {
        [Serializable]
        sealed class AuthoringData
        {
            public string Identity;
            public BtsmtlSkillFlowGraphRole Role;
            public List<BtsmtlSkillBlackboardDeclaration> Declarations = new();
        }

        [SerializeField, HideInInspector] string m_AuthoringId = Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] BtsmtlSkillFlowGraphRole m_Role;
        [SerializeField, HideInInspector] List<BtsmtlSkillBlackboardDeclaration> m_BlackboardDeclarations = new();

        public string AuthoringId => m_AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => m_Role;
        public IReadOnlyList<BtsmtlSkillBlackboardDeclaration> BlackboardDeclarations => m_BlackboardDeclarations;
        public bool IsTimelineTree => m_Role == BtsmtlSkillFlowGraphRole.TimelineBody;
        public override bool canAcceptVariableDrops => true;
        public override bool allowsPortIdentityAliases => false;
        public override bool allowBlackboardOverrides => false;
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool isTree => false;
        public override Type baseNodeType => typeof(FlowScriptNode);
        public override PlanarDirection flowDirection => PlanarDirection.Horizontal;

        public override bool CanAuthorNodeType(Type nodeType) => BtsmtlSkillFlowGraphRules.Allows(nodeType, m_Role, false);

        public void SetBlackboardDeclarations(IEnumerable<BtsmtlSkillBlackboardDeclaration> declarations)
        {
            var next = declarations.ToList();
            BtsmtlSkillBlackboardDeclarations.Validate(this, next);
            m_BlackboardDeclarations = next;
        }

        public void CollectTimelineContentClosure(TimelineContentClosureBuilder builder, string sourcePath)
        {
            try
            {
                var fingerprint = new BtsmtlSkillGraphFingerprint();
                foreach (FlowGraph graph in BtsmtlSkillGraphClosure.Validate(this, true))
                {
                    string identity = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
                    builder.AddDependency($"tree:{identity}", "timeline.tree", $"{sourcePath}/graph:{identity}", fingerprint.Compute(graph));
                }
            }
            catch (InvalidOperationException error)
            {
                builder.AddError("timeline_skill_graph_invalid", sourcePath, error.Message);
            }
        }

        public void ConfigureIdentity(string identity, BtsmtlSkillFlowGraphRole role)
        {
            if (string.IsNullOrWhiteSpace(identity) || !Enum.IsDefined(typeof(BtsmtlSkillFlowGraphRole), role) || role == BtsmtlSkillFlowGraphRole.Subgraph)
                throw new ArgumentException("Skill graph identity and role must be valid.");
            m_AuthoringId = identity;
            m_Role = role;
        }

        public override object OnDerivedDataSerialization()
        {
            BtsmtlSkillFlowGraphRules.EnsureIdentities(this);
            return new AuthoringData
            {
                Identity = m_AuthoringId,
                Role = m_Role,
                Declarations = m_BlackboardDeclarations
            };
        }

        public override void OnDerivedDataDeserialization(object data)
        {
            if (data is not AuthoringData authoring)
                throw new InvalidOperationException("Skill graph authoring data is missing.");
            ConfigureIdentity(authoring.Identity, authoring.Role);
            m_BlackboardDeclarations = authoring.Declarations ?? throw new InvalidOperationException("技能图缺少黑板声明列表。");
        }

        protected override void OnGraphInitialize() =>
            throw new InvalidOperationException("Skill graphs require compilation and must not start a FlowCanvas runtime.");

#if UNITY_EDITOR
        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override bool isEditorReadOnly => Application.isPlaying;
        public override bool usesExplicitPortSelection => true;
        public bool IsReadOnly => isEditorReadOnly;
        public bool AllowVariablePick => !IsReadOnly;
        public void DrawBlackboardExtensions(IBlackboard blackboard, UnityEngine.Object contextObject) =>
            BtsmtlSkillBlackboardEditorAdapter.Draw(this, blackboard);
        public GenericMenu GetAddVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject) =>
            BtsmtlSkillBlackboardEditorAdapter.GetAddVariableMenu(this, blackboard);
        public GenericMenu GetVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject, Variable variable, int index) =>
            BtsmtlSkillBlackboardEditorAdapter.GetVariableMenu(this, blackboard, variable);
        public void ExecuteMutation(string title, Action mutation) =>
            BtsmtlSkillBlackboardEditorAdapter.ExecuteMutation(this, title, mutation);
        public void ApplyVariableList(IBlackboard blackboard, IReadOnlyList<Variable> variables) =>
            BtsmtlSkillBlackboardEditorAdapter.ApplyVariableList(this, blackboard, variables);
        public override bool HandleEditorCommand(string command, Vector2 position) =>
            BtsmtlSkillFlowEditorMutation.HandleCommand(this, command, position);
        protected override void OnGraphEditorToolbar() => BtsmtlSkillObservationToolbar.Draw(this);
        public override UnityEngine.Object EditorUndoTarget => BtsmtlSkillFlowEditorMutation.UndoTarget(this);
        public override bool CanAuthorConnection(Port source, Port target, out string reason) =>
            BtsmtlSkillFlowEditorMutation.CanConnect(this, source, target, out reason);

        protected override void OnVariableDropInGraph(IBlackboard blackboard, Variable variable, Vector2 mousePos) =>
            BtsmtlSkillFlowEditorMutation.HandleBlackboardVariableDrop(this, blackboard, variable, mousePos);

        public override Node AddNode(Type nodeType, Vector2 position = default)
        {
            if (!CanAuthorNodeType(nodeType))
                throw new InvalidOperationException("Only registered skill nodes and native Macro nodes belong in a skill graph.");
            return BtsmtlSkillFlowEditorMutation.Execute(this, "创建技能节点", () =>
            {
                Node node = base.AddNode(nodeType, position);
                _ = node.UID;
                return node;
            });
        }

        public override BinderConnection CreatePortConnection(Port source, Port target) =>
            BtsmtlSkillFlowEditorMutation.Execute(this, "连接技能端口", () =>
            {
                if (!BtsmtlSkillFlowEditorMutation.CanConnect(this, source, target, out string reason))
                    throw new InvalidOperationException(reason);
                if (m_Role == BtsmtlSkillFlowGraphRole.StateMachine && source is FlowOutput && target is FlowInput)
                    return BtsmtlSkillFlowConnection.Create(source, target);
                return base.CreatePortConnection(source, target);
            });

        public override List<Node> DuplicateNodes(List<Node> nodes, Vector2 position = default) =>
            BtsmtlSkillGraphCopy.Copy(this, nodes, position);

        public override void ClearGraph() => BtsmtlSkillFlowEditorMutation.Clear(this);

        List<Node> IBtsmtlSkillFlowGraph.DuplicateStructure(List<Node> nodes, Vector2 position)
        {
            BtsmtlSkillFlowEditorMutation.RequireActive(this);
            return base.DuplicateNodes(nodes, position);
        }

        public override void RemoveNode(Node node, bool recordUndo = true, bool force = false)
        {
            BtsmtlSkillFlowEditorMutation.RequireRemovable(this, node);
            BtsmtlSkillFlowEditorMutation.Execute(this, "删除技能节点", () => base.RemoveNode(node, false, force));
        }

        public override void RemoveConnection(Connection connection, bool recordUndo = true) =>
            BtsmtlSkillFlowEditorMutation.Execute(this, "删除技能连线", () => base.RemoveConnection(connection, false));

        public override void DisconnectPort(Port port) =>
            BtsmtlSkillFlowEditorMutation.Execute(this, "断开技能端口", () => base.DisconnectPort(port));

        public override UnityEditor.GenericMenu GetNodesMenu(Vector2 position, Port context, UnityEngine.Object instance)
        {
            var menu = AppendFlowNodesMenu(new UnityEditor.GenericMenu(), string.Empty, position, context, instance);
            BtsmtlSkillFlowEditorMutation.AppendPrivateMacroCreationItem(this, menu, position, context);
            BtsmtlSkillProviderNodeMenu.Append(this, menu, position, context, instance);
            return this.AppendSimplexNodesMenu(menu, "原生逻辑", position, context, instance);
        }

        public override void AppendNodeCreationItem(UnityEditor.GenericMenu menu, string category, Type type, Vector2 position, Port context, object instance) =>
            BtsmtlSkillFlowEditorMutation.AppendCreationItem(this, menu, category, type, position, context, instance);
#endif
    }
}
#endif
