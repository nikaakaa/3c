#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Serialization;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillNativeStateMachineCopy
    {
        static Node[] s_Clipboard = Array.Empty<Node>();

        public static bool HandleCommand(
            BtsmtlSkillNativeStateMachine machine,
            string command,
            Vector2 position)
        {
            if (command is not ("Copy" or "Cut" or "Paste" or "Duplicate" or "Delete" or "SoftDelete"))
                return false;
            try
            {
                if (machine.isEditorReadOnly && command != "Copy")
                    throw new InvalidOperationException("Play观察期间不能修改技能FSM。");
                IGraphElement[] selected = GraphEditorUtility.activeElements.Count != 0
                    ? GraphEditorUtility.activeElements.ToArray()
                    : GraphEditorUtility.activeElement != null
                        ? new[] { GraphEditorUtility.activeElement }
                        : Array.Empty<IGraphElement>();
                Node[] nodes = selected.OfType<Node>().ToArray();
                if (nodes.Any(node => node.graph != machine))
                    throw new InvalidOperationException("选择集合不属于当前技能FSM。");
                if (nodes.Any(IsSystemAnchor))
                    throw new InvalidOperationException("FSM系统锚点不能复制或删除。");
                if (command is "Copy" or "Cut")
                    s_Clipboard = Graph.CloneNodes(nodes.ToList()).ToArray();
                if (command is "Delete" or "SoftDelete" or "Cut")
                {
                    BtsmtlSkillFlowEditorMutation.Execute(machine, "删除技能FSM选择集合", () =>
                    {
                        foreach (Connection connection in selected.OfType<Connection>().ToArray())
                            if (connection.sourceNode?.graph == machine)
                                machine.RemoveConnection(connection);
                        foreach (Node node in nodes)
                            if (machine.allNodes.Contains(node))
                                machine.RemoveNode(node);
                    });
                    GraphEditorUtility.activeElement = null;
                    GraphEditorUtility.activeElements = null;
                }
                if (command == "Paste" && s_Clipboard.Length != 0)
                {
                    GraphEditorUtility.activeElements = BtsmtlSkillGraphCopy.CopyNative(machine, s_Clipboard.ToList(), position)
                        .Cast<IGraphElement>().ToList();
                }
                if (command == "Duplicate" && nodes.Length != 0)
                {
                    GraphEditorUtility.activeElements = BtsmtlSkillGraphCopy.CopyNative(machine, nodes.ToList(), position)
                        .Cast<IGraphElement>().ToList();
                }
            }
            catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
            return true;
        }

        static bool IsSystemAnchor(Node node) =>
            node is BtsmtlSkillNativeEntryState || node is BtsmtlSkillNativeAnyState ||
            node is BtsmtlSkillNativeExitState;
    }
}
#endif
