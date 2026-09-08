using System;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillMacroGraph : Macro, IBtsmtlSkillFlowGraph
    {
        [Serializable]
        sealed class AuthoringData
        {
            public object Macro;
            public string Identity;
        }

        [SerializeField, HideInInspector] string m_AuthoringId = Guid.NewGuid().ToString("N");
        public string AuthoringId => m_AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => BtsmtlSkillFlowGraphRole.Subgraph;
        public override bool canAcceptVariableDrops => false;
        public override bool usesExternalExecution => true;
        public override bool CanAuthorNodeType(Type nodeType) => BtsmtlSkillFlowGraphRules.Allows(nodeType, Role, true);

        public void ConfigureIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("A skill Macro requires a stable identity.", nameof(identity));
            m_AuthoringId = identity;
        }

        public override object OnDerivedDataSerialization()
        {
            BtsmtlSkillFlowGraphRules.EnsureIdentities(this);
            return new AuthoringData { Macro = base.OnDerivedDataSerialization(), Identity = m_AuthoringId };
        }

        public override void OnDerivedDataDeserialization(object data)
        {
            if (data is not AuthoringData authoring)
                throw new InvalidOperationException("Skill Macro authoring data is missing.");
            base.OnDerivedDataDeserialization(authoring.Macro);
            ConfigureIdentity(authoring.Identity);
        }

        protected override void OnGraphInitialize() =>
            throw new InvalidOperationException("Skill Macros execute through the compiled Skill Program.");

#if UNITY_EDITOR
        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override bool isEditorReadOnly => Application.isPlaying;
        public override bool usesExplicitPortSelection => true;
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
            BtsmtlSkillFlowEditorMutation.Execute(this, "连接技能端口", () => base.CreatePortConnection(source, target));

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
            return this.AppendSimplexNodesMenu(menu, "原生逻辑", position, context, instance);
        }

        public override void AppendNodeCreationItem(UnityEditor.GenericMenu menu, string category, Type type, Vector2 position, Port context, object instance) =>
            BtsmtlSkillFlowEditorMutation.AppendCreationItem(this, menu, category, type, position, context, instance);
#endif
    }
}
