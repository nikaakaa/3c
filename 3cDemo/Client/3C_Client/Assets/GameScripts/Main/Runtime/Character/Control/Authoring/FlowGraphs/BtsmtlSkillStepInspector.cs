#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    static class BtsmtlSkillStepInspector
    {
        internal static void Draw(BtsmtlSkillCompositeFlowNode node)
        {
            var graph = (FlowGraph)node.graph;
            using var disabled = new EditorGUI.DisabledScope(graph.isEditorReadOnly);
            if (node is BtsmtlSkillParallelFlowNode parallel)
            {
                BtsmtlSkillParallelMode currentMode =
                    (BtsmtlSkillParallelMode)BtsmtlSkillGraphAuthoringMetadata.ReadField(node, "mode");
                var mode = (BtsmtlSkillParallelMode)EditorGUILayout.EnumPopup("完成方式", currentMode);
                if (mode != currentMode)
                    Change(graph, "修改并行完成方式", () => parallel.SetMode(mode));
            }
            EditorGUILayout.LabelField("执行步骤", EditorStyles.boldLabel);
            IReadOnlyList<BtsmtlSkillStepAuthoringValue> steps =
                BtsmtlSkillGraphAuthoringMetadata.ReadSteps(node);
            for (int index = 0; index < steps.Count; index++)
            {
                BtsmtlSkillStepAuthoringValue step = steps[index];
                using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.DelayedTextField($"{index + 1}. 名称", step.Name);
                var condition = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField("条件页面", step.Condition, typeof(BtsmtlSkillFlowGraph), false);
                int priority = step.Priority;
                var abort = (ProgramAbortPolicy)EditorGUILayout.EnumPopup("条件失效时中断", step.AbortPolicy);
                if (EditorGUI.EndChangeCheck())
                {
                    int position = index;
                    Change(graph, "修改技能步骤", () =>
                    {
                        var replacement = new BtsmtlSkillStepPort(step.Id, name);
                        replacement.Configure(name, condition, priority, abort);
                        List<BtsmtlSkillStepPort> next =
                            BtsmtlSkillGraphAuthoringMetadata.ReadStepPorts(node).ToList();
                        next[position] = replacement;
                        node.SetSteps(next);
                    });
                    return;
                }
                using var buttons = new EditorGUILayout.HorizontalScope();
                using (new EditorGUI.DisabledScope(index == 0))
                    if (GUILayout.Button("上移")) { Move(node, index, index - 1); return; }
                using (new EditorGUI.DisabledScope(index == steps.Count - 1))
                    if (GUILayout.Button("下移")) { Move(node, index, index + 1); return; }
                bool connected = node.outConnections.OfType<BinderConnection>().Any(edge => edge.sourcePortID == step.Id);
                using (new EditorGUI.DisabledScope(connected))
                    if (GUILayout.Button(new GUIContent("删除", connected ? "请先断开该步骤的连线" : "删除此步骤")))
                    {
                        int position = index;
                        Change(graph, "删除技能步骤", () =>
                        {
                            List<BtsmtlSkillStepPort> next =
                                BtsmtlSkillGraphAuthoringMetadata.ReadStepPorts(node).ToList();
                            next.RemoveAt(position);
                            node.SetSteps(next);
                        });
                        return;
                    }
            }
            if (GUILayout.Button("添加执行步骤"))
                Change(graph, "添加技能步骤", () =>
                {
                    List<BtsmtlSkillStepPort> next =
                        BtsmtlSkillGraphAuthoringMetadata.ReadStepPorts(node).ToList();
                    next.Add(new BtsmtlSkillStepPort(Guid.NewGuid().ToString("N"), "步骤"));
                    node.SetSteps(next);
                });
        }

        static void Move(BtsmtlSkillCompositeFlowNode node, int from, int to) =>
            Change((FlowGraph)node.graph, "调整技能步骤顺序", () =>
            {
                List<BtsmtlSkillStepPort> next =
                    BtsmtlSkillGraphAuthoringMetadata.ReadStepPorts(node).ToList();
                (next[from], next[to]) = (next[to], next[from]);
                node.SetSteps(next);
            });

        static void Change(FlowGraph graph, string title, Action mutation)
        {
            try { BtsmtlSkillFlowEditorMutation.Apply(graph, title, mutation); }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }
}
#endif
