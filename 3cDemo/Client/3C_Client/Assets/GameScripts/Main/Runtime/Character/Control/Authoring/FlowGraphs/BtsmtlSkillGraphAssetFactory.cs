#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphAssetFactory
    {
        public static BtsmtlSkillFlowGraph CreatePrivatePage(FlowGraph owner, BtsmtlSkillFlowGraphRole role, string name)
        {
            if (role == BtsmtlSkillFlowGraphRole.Skill || role == BtsmtlSkillFlowGraphRole.Subgraph)
                throw new ArgumentException("私有结构页面必须是状态机、状态内容或条件页面。", nameof(role));
            return CreatePrivate(owner, name, () =>
            {
                var graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                graph.ConfigureIdentity(Guid.NewGuid().ToString("N"), role);
                return graph;
            });
        }

        public static BtsmtlSkillMacroGraph CreatePrivateMacro(FlowGraph owner, string name) =>
            CreatePrivate(owner, name, () =>
            {
                var graph = ScriptableObject.CreateInstance<BtsmtlSkillMacroGraph>();
                BtsmtlSkillMacroInterface.Initialize(graph);
                return graph;
            });

        public static BtsmtlSkillMacroGraph CreateSharedMacroAsset(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                throw new InvalidOperationException("共享技能Macro目标资产路径无效或已存在。");
            var graph = ScriptableObject.CreateInstance<BtsmtlSkillMacroGraph>();
            graph.name = name;
            BtsmtlSkillMacroInterface.Initialize(graph);
            graph.ConfigureIdentity(Guid.NewGuid().ToString("N"));
            AssetDatabase.CreateAsset(graph, path);
            Undo.RegisterCreatedObjectUndo(graph, "创建共享技能Macro");
            BtsmtlSkillFlowEditorMutation.Apply(graph, "初始化共享技能Macro", () => PopulateAnchors(graph), false);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        public static TimelineAsset CreatePrivateTimeline(FlowGraph owner, string name)
        {
            string path = AssetDatabase.GetAssetPath(owner);
            if (owner is not IBtsmtlSkillFlowGraph || string.IsNullOrEmpty(path))
                throw new InvalidOperationException("私有Timeline必须属于已保存的技能图。");
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能私有Timeline", () =>
            {
                var asset = ScriptableObject.CreateInstance<TimelineAsset>();
                asset.name = name;
                AssetDatabase.AddObjectToAsset(asset, path);
                Undo.RegisterCreatedObjectUndo(asset, "创建技能私有Timeline");
                asset.SetData(TimelineData.CreateDefault(name));
                EditorUtility.SetDirty(asset);
                return asset;
            });
        }

        static T CreatePrivate<T>(FlowGraph owner, string name, Func<T> create) where T : FlowGraph, IBtsmtlSkillFlowGraph
        {
            string path = AssetDatabase.GetAssetPath(owner);
            if (owner is not IBtsmtlSkillFlowGraph || string.IsNullOrEmpty(path) ||
                AssetDatabase.LoadMainAssetAtPath(path) is not IBtsmtlSkillFlowGraph)
                throw new InvalidOperationException("私有页面必须属于已保存的技能根或共享 Macro 资产。");
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能私有页面", () =>
            {
                T graph = create();
                graph.name = name;
                AssetDatabase.AddObjectToAsset(graph, path);
                Undo.RegisterCreatedObjectUndo(graph, "创建技能私有页面");
                BtsmtlSkillFlowEditorMutation.Apply(graph, "初始化技能页面", () => PopulateAnchors(graph), false);
                return graph;
            });
        }

        public static void PopulateAnchors(FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || graph.allNodes.Count != 0)
                throw new InvalidOperationException("系统入口只能初始化到空的正式技能图。");
            switch (authoring.Role)
            {
                case BtsmtlSkillFlowGraphRole.Skill:
                    graph.AddNode<BtsmtlSkillRootFlowNode>(new Vector2(120, 180));
                    break;
                case BtsmtlSkillFlowGraphRole.StateBody:
                    graph.AddNode<BtsmtlSkillStateOnEnterFlowNode>(new Vector2(120, 60));
                    graph.AddNode<BtsmtlSkillRootFlowNode>(new Vector2(120, 260));
                    graph.AddNode<BtsmtlSkillStateOnExitFlowNode>(new Vector2(120, 460));
                    break;
                case BtsmtlSkillFlowGraphRole.StateMachine:
                    graph.AddNode<BtsmtlSkillStateEnterFlowNode>(new Vector2(100, 100));
                    graph.AddNode<BtsmtlSkillStateAnyFlowNode>(new Vector2(100, 400));
                    graph.AddNode<BtsmtlSkillStateExitFlowNode>(new Vector2(750, 250));
                    break;
                case BtsmtlSkillFlowGraphRole.ConditionRule:
                    graph.AddNode<BtsmtlSkillConditionResultFlowNode>(new Vector2(600, 180));
                    break;
                case BtsmtlSkillFlowGraphRole.Subgraph:
                    graph.AddNode<MacroInputNode>(new Vector2(100, 180));
                    graph.AddNode<MacroOutputNode>(new Vector2(650, 180));
                    break;
                case BtsmtlSkillFlowGraphRole.TimelineBody:
                    graph.AddNode<BtsmtlSkillTimelineEnableFlowNode>(new Vector2(120, 60));
                    graph.AddNode<BtsmtlSkillRootFlowNode>(new Vector2(120, 260));
                    graph.AddNode<BtsmtlSkillTimelineDisableFlowNode>(new Vector2(120, 460));
                    graph.AddNode<BtsmtlSkillTimelineDestroyFlowNode>(new Vector2(120, 660));
                    break;
                default:
                    throw new InvalidOperationException("未知技能图页面类型。");
            }
        }

        internal static void CreateOwnedContent(FlowGraph owner, FlowNode node)
        {
            if (node is BtsmtlSkillStateMachineFlowNode machine)
                machine.SetStateMachine(CreatePrivatePage(owner, BtsmtlSkillFlowGraphRole.StateMachine, "技能状态机"));
            if (node is BtsmtlSkillStateFlowNode state)
                state.SetBody(CreatePrivatePage(owner, BtsmtlSkillFlowGraphRole.StateBody, "状态内容"));
            if (node is BtsmtlSkillTimelineFlowNode timeline)
                timeline.Configure(CreatePrivateTimeline(owner, "技能Timeline"), BtsmtlSkillTimelineOwnership.Private,
                    null, TimelinePlaybackMode.Once);
        }
    }
}
#endif
