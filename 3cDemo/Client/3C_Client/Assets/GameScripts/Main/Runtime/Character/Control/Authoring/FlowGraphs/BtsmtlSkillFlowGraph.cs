#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ParadoxNotion;
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

    public interface IBtsmtlSkillFlowGraph
    {
        string AuthoringId { get; }
        BtsmtlSkillFlowGraphRole Role { get; }
        IReadOnlyList<BtsmtlSkillBlackboardDeclaration> BlackboardDeclarations { get; }
        void SetBlackboardDeclarations(IEnumerable<BtsmtlSkillBlackboardDeclaration> declarations);
        List<Node> DuplicateStructure(List<Node> nodes, Vector2 position);
    }

    static class BtsmtlSkillFlowGraphRules
    {
        internal static bool Allows(Type nodeType, BtsmtlSkillFlowGraphRole role, bool macro)
        {
            if (nodeType == null || nodeType.IsAbstract)
                return false;
            if (nodeType == typeof(MacroInputNode) || nodeType == typeof(MacroOutputNode))
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
                return role == BtsmtlSkillFlowGraphRole.StateBody;
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

    public sealed class BtsmtlSkillFlowGraph : FlowGraph, IBtsmtlSkillFlowGraph, ITimelineTreeGraphAsset
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
        public override bool canAcceptVariableDrops => false;
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
        public override bool HandleEditorCommand(string command, Vector2 position) =>
            BtsmtlSkillFlowEditorMutation.HandleCommand(this, command, position);
        public override UnityEngine.Object EditorUndoTarget => BtsmtlSkillFlowEditorMutation.UndoTarget(this);
        public override bool CanAuthorConnection(Port source, Port target, out string reason) =>
            BtsmtlSkillFlowEditorMutation.CanConnect(this, source, target, out reason);

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
            BtsmtlSkillFlowEditorMutation.Execute(this, "连接技能端口", () => base.CreatePortConnection(source, target));

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
            return this.AppendSimplexNodesMenu(menu, "原生逻辑", position, context, instance);
        }

        public override void AppendNodeCreationItem(UnityEditor.GenericMenu menu, string category, Type type, Vector2 position, Port context, object instance) =>
            BtsmtlSkillFlowEditorMutation.AppendCreationItem(this, menu, category, type, position, context, instance);
#endif
    }
}
#endif
