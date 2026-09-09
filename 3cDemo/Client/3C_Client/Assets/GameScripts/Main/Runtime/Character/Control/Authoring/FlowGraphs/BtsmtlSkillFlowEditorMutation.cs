#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillFlowEditorMutation
    {
        sealed class Depth { internal int Value; }
        static readonly ConditionalWeakTable<FlowGraph, Depth> s_Depth = new();

        public static void RequireActive(FlowGraph graph)
        {
            if (!s_Depth.TryGetValue(graph, out Depth depth) || depth.Value == 0)
                throw new InvalidOperationException("技能图写入必须加入现有编辑事务。");
        }

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

        public static bool HandleCommand(FlowGraph graph, string command, Vector2 position)
        {
            if (command is not ("Copy" or "Cut" or "Paste" or "Duplicate" or "Delete" or "SoftDelete"))
                return false;
            try
            {
                if (graph.isEditorReadOnly && command != "Copy")
                    throw new InvalidOperationException("Play观察期间不能修改技能图。");
                var selected = GraphEditorUtility.activeElements.Count != 0
                    ? GraphEditorUtility.activeElements.ToArray()
                    : GraphEditorUtility.activeElement != null
                        ? new[] { GraphEditorUtility.activeElement }
                        : Array.Empty<IGraphElement>();
                if (selected.Any(element => element.graph != graph))
                    throw new InvalidOperationException("选择集合不属于当前技能图。");
                Node[] nodes = selected.OfType<Node>().ToArray();
                if (command is "Delete" or "SoftDelete" or "Cut")
                {
                    foreach (Node node in nodes)
                        RequireRemovable(graph, node);
                    foreach (Connection edge in selected.OfType<Connection>())
                        if (!edge.sourceNode.outConnections.Contains(edge))
                            throw new InvalidOperationException("选择的连线已不属于当前拓扑。");
                }
                if (command is "Copy" or "Cut")
                {
                    if (nodes.Length == 0)
                        return true;
                    BtsmtlSkillGraphCopy.CaptureClipboard(nodes.ToList());
                }
                if (command is "Delete" or "SoftDelete" or "Cut")
                {
                    Execute(graph, "删除技能选择集合", () =>
                    {
                        foreach (Connection edge in selected.OfType<Connection>())
                            graph.RemoveConnection(edge);
                        foreach (Node node in nodes)
                            graph.RemoveNode(node);
                    });
                    GraphEditorUtility.activeElement = null;
                    GraphEditorUtility.activeElements = null;
                }
                if (command == "Paste" && CopyBuffer.TryGetCache<Node[]>(out Node[] copied))
                    GraphEditorUtility.activeElements = graph.DuplicateNodes(copied.ToList(), position).Cast<IGraphElement>().ToList();
                if (command == "Duplicate" && nodes.Length != 0)
                    GraphEditorUtility.activeElements = graph.DuplicateNodes(nodes.ToList()).Cast<IGraphElement>().ToList();
            }
            catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
            return true;
        }

        public static void Clear(FlowGraph graph)
        {
            Execute(graph, "清空技能页面内容", () =>
            {
                foreach (Connection edge in graph.allNodes.SelectMany(node => node.outConnections).ToArray())
                    graph.RemoveConnection(edge);
                foreach (Node node in graph.allNodes.Where(node => node is not IBtsmtlSkillSystemNode &&
                    node is not MacroInputNode && node is not MacroOutputNode).ToArray())
                    graph.RemoveNode(node);
                graph.canvasGroups.Clear();
            });
            GraphEditorUtility.activeElement = null;
            GraphEditorUtility.activeElements = null;
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
                        : source.IsFlowPort() != target.IsFlowPort()
                            ? "Skill flow and value ports cannot be mixed."
                            : source is FlowOutput && source.connections != 0
                                ? "Skill flow output already has a connection."
                                : target is ValueInput && target.connections != 0
                                    ? "Skill value input already has a connection."
                                    : BinderConnection.CanBeBoundVerbosed(source, target, null, out string bindingReason)
                                        ? null
                                        : bindingReason;
            return reason == null;
        }

        public static void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo = true) =>
            Apply(graph, title, mutation, recordUndo, Array.Empty<UnityEngine.Object>());

        public static void Apply(
            FlowGraph graph,
            string title,
            Action mutation,
            bool recordUndo,
            IEnumerable<UnityEngine.Object> additionalOwners)
        {
            if (graph is not IBtsmtlSkillFlowGraph || graph.isEditorReadOnly)
                throw new InvalidOperationException("The skill authoring graph is not writable.");
            Depth depth = s_Depth.GetValue(graph, _ => new Depth());
            if (depth.Value != 0)
                throw new InvalidOperationException("A skill mutation must join its existing transaction instead of nesting another one.");
            graph.SelfSerialize();
            var undoOwners = new List<UnityEngine.Object> { graph };
            foreach (UnityEngine.Object owner in additionalOwners ?? Array.Empty<UnityEngine.Object>())
                if (owner && !undoOwners.Contains(owner))
                    undoOwners.Add(owner);
            foreach (UnityEngine.Object owner in undoOwners)
                if (owner is FlowGraph ownerGraph)
                    ownerGraph.SelfSerialize();
            var previousOwnedAssets = BtsmtlSkillOwnedAssets.Collect(graph);
            int group = -1;
            if (recordUndo)
            {
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(title);
                Undo.RegisterCompleteObjectUndo(undoOwners.ToArray(), title);
            }
            depth.Value++;
            try
            {
                mutation();
                BtsmtlSkillGraphClosure.Validate(graph, false);
                BtsmtlSkillOwnedAssets.ReleaseUnreferenced(graph, previousOwnedAssets);
                graph.SelfSerialize();
                foreach (UnityEngine.Object owner in undoOwners)
                    EditorUtility.SetDirty(owner);
                if (recordUndo)
                    Undo.CollapseUndoOperations(group);
            }
            catch
            {
                if (recordUndo)
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(group);
                    foreach (UnityEngine.Object owner in undoOwners)
                        if (owner is FlowGraph ownerGraph)
                            ownerGraph.SelfDeserialize();
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
            menu.AddItem(new GUIContent("BTSMTL/子图/新建共享Macro"), false, () =>
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "创建共享技能Macro",
                    "SharedSkillMacro",
                    "asset",
                    "选择共享技能Macro的正式资产路径。");
                if (string.IsNullOrEmpty(path))
                    return;
                BtsmtlSkillMacroGraph shared = null;
                try
                {
                    shared = BtsmtlSkillGraphAssetFactory.CreateSharedMacroAsset(path, "共享技能子图");
                    MacroNodeWrapper call = Execute(graph, "引用共享技能子图", () =>
                    {
                        var created = (MacroNodeWrapper)graph.AddNode(typeof(MacroNodeWrapper), position);
                        created.macro = shared;
                        if (context != null)
                        {
                            Port source = context.parent.GetOutputPort(context.ID);
                            Port target = created.GetInputPort(created.macro.inputDefinitions.Single(value => value.type == typeof(Flow)).ID);
                            if (source == null || !CanConnect(graph, source, target, out _) ||
                                BinderConnection.Create(source, target) == null)
                                throw new InvalidOperationException("原执行端口已变化，无法连接共享技能子图。");
                        }
                        return created;
                    });
                    GraphEditorUtility.activeElement = call;
                }
                catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
                {
                    if (shared)
                        AssetDatabase.DeleteAsset(path);
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
