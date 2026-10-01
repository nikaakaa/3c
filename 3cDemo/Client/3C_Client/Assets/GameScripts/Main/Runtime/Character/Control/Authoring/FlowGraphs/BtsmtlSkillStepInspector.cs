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
        static readonly GUIContent s_DeleteStep = new("删除", "删除此步骤");
        static readonly GUIContent s_DeleteConnectedStep = new("删除", "请先断开该步骤的连线");

        internal static void Draw(BtsmtlSkillCompositeFlowNode node)
        {
            var graph = (FlowGraph)node.graph;
            using var disabled = new EditorGUI.DisabledScope(graph.isEditorReadOnly);
            if (node is BtsmtlSkillParallelFlowNode parallel)
            {
                BtsmtlSkillParallelMode currentMode = parallel.Mode;
                var mode = (BtsmtlSkillParallelMode)EditorGUILayout.EnumPopup("完成方式", currentMode);
                if (mode != currentMode)
                    ChangeMode(parallel, mode);
            }
            EditorGUILayout.LabelField("执行步骤", EditorStyles.boldLabel);
            IReadOnlyList<BtsmtlSkillStepPort> steps = node.Steps;
            for (int index = 0; index < steps.Count; index++)
            {
                BtsmtlSkillStepPort step = steps[index];
                using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.DelayedTextField($"{index + 1}. 名称", step.Name);
                var condition = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField("条件页面", step.Condition, typeof(BtsmtlSkillFlowGraph), false);
                var abort = (ProgramAbortPolicy)EditorGUILayout.EnumPopup("条件失效时中断", step.AbortPolicy);
                if (EditorGUI.EndChangeCheck())
                {
                    ChangeStep(node, index, step, name, condition, abort);
                    return;
                }
                using var buttons = new EditorGUILayout.HorizontalScope();
                using (new EditorGUI.DisabledScope(index == 0))
                    if (GUILayout.Button("上移")) { Move(node, index, index - 1); return; }
                using (new EditorGUI.DisabledScope(index == steps.Count - 1))
                    if (GUILayout.Button("下移")) { Move(node, index, index + 1); return; }
                bool connected = false;
                for (int connectionIndex = 0; connectionIndex < node.outConnections.Count; connectionIndex++)
                {
                    if (node.outConnections[connectionIndex] is BinderConnection connection &&
                        connection.sourcePortID == step.Id)
                    {
                        connected = true;
                        break;
                    }
                }
                using (new EditorGUI.DisabledScope(connected))
                    if (GUILayout.Button(connected ? s_DeleteConnectedStep : s_DeleteStep))
                    {
                        DeleteStep(node, index);
                        return;
                    }
            }
            if (GUILayout.Button("添加执行步骤"))
                AddStep(node);
        }

        static void ChangeMode(BtsmtlSkillParallelFlowNode node, BtsmtlSkillParallelMode mode) =>
            Change((FlowGraph)node.graph, "修改并行完成方式", () => node.SetMode(mode));

        static void ChangeStep(BtsmtlSkillCompositeFlowNode node, int position, BtsmtlSkillStepPort step,
            string name, BtsmtlSkillFlowGraph condition, ProgramAbortPolicy abort)
        {
            int priority = step.Priority;
            Change((FlowGraph)node.graph, "修改技能步骤", () =>
            {
                var replacement = new BtsmtlSkillStepPort(step.Id, name);
                replacement.Configure(name, condition, priority, abort);
                List<BtsmtlSkillStepPort> next = node.Steps.ToList();
                next[position] = replacement;
                node.SetSteps(next);
            });
        }

        static void DeleteStep(BtsmtlSkillCompositeFlowNode node, int position) =>
            Change((FlowGraph)node.graph, "删除技能步骤", () =>
            {
                List<BtsmtlSkillStepPort> next = node.Steps.ToList();
                next.RemoveAt(position);
                node.SetSteps(next);
            });

        static void AddStep(BtsmtlSkillCompositeFlowNode node) =>
            Change((FlowGraph)node.graph, "添加技能步骤", () =>
            {
                List<BtsmtlSkillStepPort> next = node.Steps.ToList();
                next.Add(new BtsmtlSkillStepPort(Guid.NewGuid().ToString("N"), "步骤"));
                node.SetSteps(next);
            });

        static void Move(BtsmtlSkillCompositeFlowNode node, int from, int to) =>
            Change((FlowGraph)node.graph, "调整技能步骤顺序", () =>
            {
                List<BtsmtlSkillStepPort> next = node.Steps.ToList();
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
