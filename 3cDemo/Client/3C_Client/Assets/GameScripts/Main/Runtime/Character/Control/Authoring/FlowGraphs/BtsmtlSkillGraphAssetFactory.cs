#if UNITY_EDITOR
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BTSMTL.Timeline;
using FlowCanvas;
using ThirdPersonCharacter.ActionSystem;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphAssetFactory
    {
        public static string StableIdentity(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                throw new ArgumentException("技能图稳定身份种子缺失。", nameof(seed));
            byte[] hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return new Guid(hash.Take(16).ToArray()).ToString("N");
        }

        public static BtsmtlSkillFlowGraph CreatePrivatePage(FlowGraph owner, BtsmtlSkillFlowGraphRole role, string name)
        {
            if (role == BtsmtlSkillFlowGraphRole.Skill || role == BtsmtlSkillFlowGraphRole.Subgraph ||
                role == BtsmtlSkillFlowGraphRole.StateMachine)
                throw new ArgumentException("私有结构页面必须是状态机、状态内容或条件页面。", nameof(role));
            return CreatePrivate(owner, name, () =>
            {
                var graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                graph.ConfigureIdentity(Guid.NewGuid().ToString("N"), role);
                return graph;
            });
        }

        public static BtsmtlSkillNativeStateMachine CreatePrivateStateMachine(
            FlowGraph owner,
            string name,
            string ownerGraphId,
            string ownerNodeId)
        {
            BtsmtlSkillStateMachineFlowNode ownerNode = owner?.allNodes
                .OfType<BtsmtlSkillStateMachineFlowNode>()
                .SingleOrDefault(value => value.UID == ownerNodeId);
            if (ownerNode == null)
                throw new InvalidOperationException("私有状态机必须绑定现有的技能状态机调用节点。");
            if (ownerNode.StateMachine != null)
                throw new InvalidOperationException("技能状态机调用节点已经拥有原生状态机。");
            string path = AssetDatabase.GetAssetPath(owner);
            if (owner is not IBtsmtlSkillFlowGraph || string.IsNullOrEmpty(path))
                throw new InvalidOperationException("私有状态机必须属于已保存的技能根或共享Macro资产。");
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset is not IBtsmtlSkillFlowGraph &&
                (mainAsset is not GameplayAbilityDefinition ability || !IsAbilityOwnedGraph(ability, owner, path)))
                throw new InvalidOperationException("私有状态机必须属于已保存的技能根或共享Macro资产。");
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能原生状态机", () =>
            {
                BtsmtlSkillNativeStateMachine machine = ScriptableObject.CreateInstance<BtsmtlSkillNativeStateMachine>();
                machine.name = name;
                machine.ConfigureIdentity(Guid.NewGuid().ToString("N"));
                machine.ConfigureOwner(ownerGraphId, ownerNodeId);
                AssetDatabase.AddObjectToAsset(machine, path);
                Undo.RegisterCreatedObjectUndo(machine, "创建技能原生状态机");
                ownerNode.SetStateMachine(machine);
                BtsmtlSkillNativeStateMachineContract.Populate(machine);
                EditorUtility.SetDirty(machine);
                return machine;
            });
        }

        public static BtsmtlSkillFlowGraph CreatePrivateAbilityGraph(
            GameplayAbilityDefinition ability,
            string identity,
            string name)
        {
            if (!ability)
                throw new ArgumentNullException(nameof(ability));
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("Gameplay Ability graph identity is required.", nameof(identity));
            string path = AssetDatabase.GetAssetPath(ability);
            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadMainAssetAtPath(path) != ability)
                throw new InvalidOperationException("Gameplay Ability graph must belong to a saved Ability definition asset.");
            if (ability.AbilityGraph != null)
                throw new InvalidOperationException("Gameplay Ability already owns an Ability graph.");
            var graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
            graph.name = string.IsNullOrWhiteSpace(name) ? ability.AbilityId : name;
            graph.ConfigureIdentity(identity, BtsmtlSkillFlowGraphRole.Skill);
            AssetDatabase.AddObjectToAsset(graph, path);
            Undo.RegisterCreatedObjectUndo(graph, "创建Gameplay Ability图");
            ability.SetAbilityGraph(graph);
            EditorUtility.SetDirty(ability);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(path);
            graph = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<BtsmtlSkillFlowGraph>()
                .SingleOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            if (!ability || !graph)
                throw new InvalidOperationException("Gameplay Ability root or private AbilityGraph was not persisted.");
            if (ability.AbilityGraph != graph)
            {
                ability.SetAbilityGraph(graph);
                EditorUtility.SetDirty(ability);
                AssetDatabase.SaveAssets();
            }
            BtsmtlSkillFlowEditorMutation.Apply(
                graph,
                "初始化Gameplay Ability图",
                () => PopulateAnchors(graph),
                false);
            EditorUtility.SetDirty(ability);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        public static void CreatePrivateStateBody(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeState state)
        {
            FlowGraph owner = RootFlowGraph(machine);
            BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能StateBody", () =>
                state.SetBody(CreatePrivatePage(owner, BtsmtlSkillFlowGraphRole.StateBody, "状态内容")));
        }

        public static void CreatePrivateCondition(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeConnection connection)
        {
            FlowGraph owner = RootFlowGraph(machine);
            BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能转移条件", () =>
                connection.Configure(
                    CreatePrivatePage(owner, BtsmtlSkillFlowGraphRole.ConditionRule, "转移条件"),
                    connection.Priority,
                    connection.AbortPolicy,
                    connection.Order));
        }

        static FlowGraph RootFlowGraph(BtsmtlSkillNativeStateMachine machine)
        {
            string path = AssetDatabase.GetAssetPath(machine);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("原生Skill FSM必须属于正式技能图资产。");
            UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(path);
            if (main is FlowGraph owner)
                return owner;
            if (main is GameplayAbilityDefinition ability && ability.AbilityGraph != null)
                return ability.AbilityGraph;
            throw new InvalidOperationException("原生Skill FSM必须属于正式技能图资产。");
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
            if (owner is not IBtsmtlSkillFlowGraph || string.IsNullOrEmpty(path))
                throw new InvalidOperationException("私有页面必须属于已保存的技能根或共享Macro资产。");
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset is not IBtsmtlSkillFlowGraph &&
                (mainAsset is not GameplayAbilityDefinition ability || !IsAbilityOwnedGraph(ability, owner, path)))
                throw new InvalidOperationException("私有页面必须属于已保存的技能根或共享Macro资产。");
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

        static bool IsAbilityOwnedGraph(
            GameplayAbilityDefinition ability,
            FlowGraph owner,
            string path)
        {
            return ability &&
                ability.AbilityGraph != null &&
                string.Equals(AssetDatabase.GetAssetPath(owner), path, StringComparison.Ordinal) &&
                string.Equals(AssetDatabase.GetAssetPath(ability.AbilityGraph), path, StringComparison.Ordinal);
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
                    graph.AddNode<BtsmtlSkillMacroInputNode>(new Vector2(100, 180));
                    graph.AddNode<BtsmtlSkillMacroOutputNode>(new Vector2(650, 180));
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

        public static void EnsureRequiredAnchors(BtsmtlSkillFlowGraph graph)
        {
            if (!graph || graph.Role != BtsmtlSkillFlowGraphRole.StateBody)
                return;
            bool hasOnEnter = graph.allNodes.OfType<BtsmtlSkillStateOnEnterFlowNode>().Any();
            bool hasRoot = graph.allNodes.OfType<BtsmtlSkillRootFlowNode>().Any();
            bool hasOnExit = graph.allNodes.OfType<BtsmtlSkillStateOnExitFlowNode>().Any();
            if (hasOnEnter && hasRoot && hasOnExit)
                return;
            BtsmtlSkillFlowEditorMutation.Apply(graph, "补齐StateBody固定生命周期入口", () =>
            {
                if (!hasOnEnter)
                    graph.AddNode<BtsmtlSkillStateOnEnterFlowNode>(new Vector2(120, 60));
                if (!hasRoot)
                    graph.AddNode<BtsmtlSkillRootFlowNode>(new Vector2(120, 260));
                if (!hasOnExit)
                    graph.AddNode<BtsmtlSkillStateOnExitFlowNode>(new Vector2(120, 460));
            }, false);
        }

        internal static void CreateOwnedContent(FlowGraph owner, FlowNode node)
        {
            if (node is BtsmtlSkillStateMachineFlowNode machine)
                CreatePrivateStateMachine(
                    owner,
                    "技能状态机",
                    ((IBtsmtlSkillFlowGraph)owner).AuthoringId,
                    machine.UID);
            if (node is BtsmtlSkillStateFlowNode state)
                state.SetBody(CreatePrivatePage(owner, BtsmtlSkillFlowGraphRole.StateBody, "状态内容"));
            if (node is BtsmtlSkillTimelineFlowNode timeline)
                timeline.Configure(CreatePrivateTimeline(owner, "技能Timeline"), BtsmtlSkillTimelineOwnership.Private,
                    null, TimelinePlaybackMode.Once);
        }
    }

}
#endif
