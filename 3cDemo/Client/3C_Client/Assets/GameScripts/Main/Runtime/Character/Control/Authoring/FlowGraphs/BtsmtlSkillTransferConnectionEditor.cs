#if UNITY_EDITOR
using System;
using FlowCanvas;
using NodeCanvas.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    static class BtsmtlSkillTransferConnectionEditor
    {
        public static void Draw(BtsmtlSkillFlowConnection connection)
        {
            if (!(connection.graph is BtsmtlSkillFlowGraph graph))
                return;
            using var disabled = new EditorGUI.DisabledScope(graph.IsReadOnly);
            var condition = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField(
                "条件图", connection.Condition, typeof(BtsmtlSkillFlowGraph), false);
            var priority = EditorGUILayout.DelayedIntField("优先级", connection.Priority);
            var abort = (ProgramAbortPolicy)EditorGUILayout.EnumPopup("条件失效时中断", connection.AbortPolicy);
            if (condition == connection.Condition && priority == connection.Priority && abort == connection.AbortPolicy)
                return;
            Change(graph, "修改技能转移", () => connection.Configure(condition, priority, abort));
        }

        static void Change(BtsmtlSkillFlowGraph graph, string title, Action mutation)
        {
            try { BtsmtlSkillFlowEditorMutation.Apply(graph, title, mutation); }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }
}
#endif
