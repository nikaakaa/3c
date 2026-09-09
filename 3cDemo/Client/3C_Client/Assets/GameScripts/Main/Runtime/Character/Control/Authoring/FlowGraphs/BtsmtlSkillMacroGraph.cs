#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using UnityEngine;
using UnityEditor;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillMacroGraph : Macro, IBtsmtlSkillFlowGraph
    {
        [Serializable]
        sealed class AuthoringData
        {
            public object Macro;
            public string Identity;
            public List<BtsmtlSkillBlackboardDeclaration> Declarations = new();
        }

        [SerializeField, HideInInspector] string m_AuthoringId = Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] List<BtsmtlSkillBlackboardDeclaration> m_BlackboardDeclarations = new();
        public string AuthoringId => m_AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => BtsmtlSkillFlowGraphRole.Subgraph;
        public IReadOnlyList<BtsmtlSkillBlackboardDeclaration> BlackboardDeclarations => m_BlackboardDeclarations;
        public override bool canAcceptVariableDrops => false;
        public override bool allowsPortIdentityAliases => false;
        public override bool usesExternalExecution => true;
        public override bool CanAuthorNodeType(Type nodeType) => BtsmtlSkillFlowGraphRules.Allows(nodeType, Role, true);

        public void ConfigureIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("A skill Macro requires a stable identity.", nameof(identity));
            m_AuthoringId = identity;
        }

        public void SetBlackboardDeclarations(IEnumerable<BtsmtlSkillBlackboardDeclaration> declarations)
        {
            var next = declarations.ToList();
            BtsmtlSkillBlackboardDeclarations.Validate(this, next);
            m_BlackboardDeclarations = next;
        }

        public override object OnDerivedDataSerialization()
        {
            BtsmtlSkillFlowGraphRules.EnsureIdentities(this);
            return new AuthoringData { Macro = base.OnDerivedDataSerialization(), Identity = m_AuthoringId, Declarations = m_BlackboardDeclarations };
        }

        public override void OnDerivedDataDeserialization(object data)
        {
            if (data is not AuthoringData authoring)
                throw new InvalidOperationException("Skill Macro authoring data is missing.");
            base.OnDerivedDataDeserialization(authoring.Macro);
            ConfigureIdentity(authoring.Identity);
            m_BlackboardDeclarations = authoring.Declarations ?? throw new InvalidOperationException("技能Macro缺少黑板声明列表。");
        }

        protected override void OnGraphInitialize() =>
            throw new InvalidOperationException("Skill Macros execute through the compiled Skill Program.");

#if UNITY_EDITOR
        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override bool isEditorReadOnly => Application.isPlaying;
        public override bool usesExplicitPortSelection => true;
        public override bool HandleEditorCommand(string command, Vector2 position) =>
            BtsmtlSkillFlowEditorMutation.HandleCommand(this, command, position);
        protected override void OnGraphEditorToolbar()
        {
            GUILayout.Label(AssetDatabase.IsMainAsset(this) ? "共享 Macro" : "私有 Macro", EditorStyles.miniLabel);
            if (GUILayout.Button("编辑接口", EditorStyles.toolbarButton))
                BtsmtlSkillMacroInterface.OpenEditor(this);
            BtsmtlSkillObservationToolbar.Draw(this);
        }
        public override UnityEngine.Object EditorUndoTarget => BtsmtlSkillFlowEditorMutation.UndoTarget(this);
        public override bool CanAuthorConnection(Port source, Port target, out string reason) =>
            BtsmtlSkillFlowEditorMutation.CanConnect(this, source, target, out reason);

        public override Node AddNode(Type nodeType, Vector2 position = default)
        {
            if (!CanAuthorNodeType(nodeType))
                throw new InvalidOperationException("The node type does not belong in a skill Macro.");
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
            return this.AppendSimplexNodesMenu(menu, "原生逻辑", position, context, instance);
        }

        public override void AppendNodeCreationItem(UnityEditor.GenericMenu menu, string category, Type type, Vector2 position, Port context, object instance) =>
            BtsmtlSkillFlowEditorMutation.AppendCreationItem(this, menu, category, type, position, context, instance);
#endif
    }

    public sealed class BtsmtlSkillMacroInputNode : MacroInputNode
    {
        protected override void OnNodeInspectorGUI()
        {
            EditorGUILayout.HelpBox("技能Macro接口由工具栏统一编辑。", MessageType.Info);
            if (GUILayout.Button("打开接口编辑器"))
                BtsmtlSkillMacroInterface.OpenEditor((BtsmtlSkillMacroGraph)graph);
        }
    }

    public sealed class BtsmtlSkillMacroOutputNode : MacroOutputNode
    {
        protected override void OnNodeInspectorGUI()
        {
            EditorGUILayout.HelpBox("技能Macro接口由工具栏统一编辑。", MessageType.Info);
            if (GUILayout.Button("打开接口编辑器"))
                BtsmtlSkillMacroInterface.OpenEditor((BtsmtlSkillMacroGraph)graph);
        }
    }
}
#endif
