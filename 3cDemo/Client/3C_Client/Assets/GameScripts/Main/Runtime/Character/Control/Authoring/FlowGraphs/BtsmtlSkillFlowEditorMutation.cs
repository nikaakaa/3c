#if UNITY_EDITOR
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillFlowEditorMutation
    {
        sealed class Depth { internal int Value; }
        static readonly ConditionalWeakTable<FlowGraph, Depth> s_Depth = new();

        public static UnityEngine.Object UndoTarget(FlowGraph graph) =>
            s_Depth.TryGetValue(graph, out Depth depth) && depth.Value != 0 ? null : graph;

        public static T Execute<T>(FlowGraph graph, string title, Func<T> mutation)
        {
            if (s_Depth.TryGetValue(graph, out Depth depth) && depth.Value != 0)
                return mutation();
            T result = default;
            Apply(graph, title, () => result = mutation());
            return result;
        }

        public static void Execute(FlowGraph graph, string title, Action mutation) =>
            Execute(graph, title, () => { mutation(); return true; });

        public static void RequireRemovable(FlowGraph graph, Node node)
        {
            if (node == null || !graph.allNodes.Contains(node))
                throw new InvalidOperationException("删除目标不属于当前技能图。");
            if (node is IBtsmtlSkillSystemNode || node is MacroInputNode || node is MacroOutputNode)
                throw new InvalidOperationException("系统入口随所属页面创建和删除，不能单独删除。");
        }

        public static bool CanConnect(FlowGraph graph, Port source, Port target, out string reason)
        {
            reason = graph.isEditorReadOnly
                ? "Skill authoring is read-only while observing Play Mode."
                : source.parent.graph != graph || target.parent.graph != graph ||
                  !graph.allNodes.Contains(source.parent) || !graph.allNodes.Contains(target.parent)
                    ? "Skill connections must stay within their formal graph."
                    : source.type != target.type
                        ? "Skill ports require matching declared types; implicit conversions are not allowed."
                        : null;
            return reason == null;
        }

        public static void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo = true)
        {
            if (graph is not IBtsmtlSkillFlowGraph || graph.isEditorReadOnly)
                throw new InvalidOperationException("The skill authoring graph is not writable.");
            Depth depth = s_Depth.GetValue(graph, _ => new Depth());
            if (depth.Value != 0)
                throw new InvalidOperationException("A skill mutation must join its existing transaction instead of nesting another one.");
            graph.SelfSerialize();
            var previousOwnedAssets = BtsmtlSkillOwnedAssets.Collect(graph);
            int group = -1;
            if (recordUndo)
            {
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(title);
                Undo.RegisterCompleteObjectUndo(graph, title);
            }
            depth.Value++;
            try
            {
                mutation();
                BtsmtlSkillGraphClosure.Validate(graph, false);
                BtsmtlSkillOwnedAssets.ReleaseUnreferenced(graph, previousOwnedAssets);
                graph.SelfSerialize();
                EditorUtility.SetDirty(graph);
                if (recordUndo)
                    Undo.CollapseUndoOperations(group);
            }
            catch
            {
                if (recordUndo)
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(group);
                    graph.SelfDeserialize();
                    GraphEditorUtility.activeElement = null;
                }
                throw;
            }
            finally
            {
                depth.Value--;
            }
        }

        public static void AppendCreationItem(FlowGraph graph, GenericMenu menu, string category, Type type,
            Vector2 position, Port context, object dropInstance)
        {
            if (!graph.CanAuthorNodeType(type) || graph.isEditorReadOnly)
                return;
            if (dropInstance != null)
                throw new InvalidOperationException("Skill resource creation requires an explicit registered field binding.");
            if (context == null)
            {
                menu.AddItem(new GUIContent(category), false, () => Create(graph, type, position, null, -1));
                return;
            }
            var prototype = (FlowNode)Activator.CreateInstance(type);
            prototype.GatherPorts();
            bool input = context.IsOutputPort();
            Port[] ports = Ports(prototype, input);
            for (int i = 0; i < ports.Length; i++)
            {
                if (ports[i].type != context.type || ports[i].IsFlowPort() != context.IsFlowPort())
                    continue;
                int index = i;
                menu.AddItem(new GUIContent(category + "/" + ports[i].name), false,
                    () => Create(graph, type, position, context, index));
            }
        }

        public static void AppendPrivateMacroCreationItem(FlowGraph graph, GenericMenu menu, Vector2 position, Port context)
        {
            if (graph.isEditorReadOnly || !graph.CanAuthorNodeType(typeof(MacroNodeWrapper)) ||
                context != null && context is not FlowOutput)
                return;
            menu.AddItem(new GUIContent("BTSMTL/子图/新建私有Macro"), false, () =>
            {
                try
                {
                    MacroNodeWrapper call = Execute(graph, "创建私有技能子图", () =>
                    {
                        var created = (MacroNodeWrapper)graph.AddNode(typeof(MacroNodeWrapper), position);
                        created.macro = BtsmtlSkillGraphAssetFactory.CreatePrivateMacro(graph, "技能子图");
                        if (context != null)
                        {
                            Port source = context.parent.GetOutputPort(context.ID);
                            Port target = created.GetInputPort(created.macro.inputDefinitions.Single(value => value.type == typeof(Flow)).ID);
                            if (source == null || !CanConnect(graph, source, target, out _) ||
                                !BinderConnection.CanBeBoundVerbosed(source, target, null, out _) || BinderConnection.Create(source, target) == null)
                                throw new InvalidOperationException("原执行端口已变化，无法连接新建子图。");
                        }
                        return created;
                    });
                    GraphEditorUtility.activeElement = call;
                }
                catch (InvalidOperationException error)
                {
                    GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
                }
            });
        }

        static Port[] Ports(FlowNode node, bool input) => input
            ? node.GetInputFlowPorts().Cast<Port>().Concat(node.GetInputValuePorts()).ToArray()
            : node.GetOutputFlowPorts().Cast<Port>().Concat(node.GetOutputValuePorts()).ToArray();

        static void Create(FlowGraph graph, Type type, Vector2 position, Port context, int index)
        {
            FlowNode created = null;
            try
            {
                Apply(graph, "创建技能节点", () =>
                {
                    created = (FlowNode)graph.AddNode(type, position);
                    BtsmtlSkillGraphAssetFactory.CreateOwnedContent(graph, created);
                    if (context == null)
                        return;
                    Port endpoint = context.IsInputPort()
                        ? context.parent.GetInputPort(context.ID)
                        : context.parent.GetOutputPort(context.ID);
                    Port port = Ports(created, context.IsOutputPort())[index];
                    Port source = context.IsOutputPort() ? endpoint : port;
                    Port target = context.IsOutputPort() ? port : endpoint;
                    if (source == null || target == null || source.parent.graph != graph || target.parent.graph != graph ||
                        !graph.allNodes.Contains(source.parent) || !graph.allNodes.Contains(target.parent) ||
                        source.type != target.type || !BinderConnection.CanBeBoundVerbosed(source, target, null, out _))
                        throw new InvalidOperationException("The selected creation port is no longer compatible.");
                    if (BinderConnection.Create(source, target) == null)
                        throw new InvalidOperationException("The native skill connection could not be created.");
                });
                GraphEditorUtility.activeElement = created;
            }
            catch (InvalidOperationException error)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }
    }
}
#endif
