using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.rootId = RushId("root:RushAttack");
            parts.profile = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset", 11400000L);
            var effects = new[]
            {
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_01_01.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_01_02.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_02.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_Enhance_AttackProperty_01_01.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_Enhance_AttackProperty_01_02.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_Enhance_AttackProperty_01_03.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_Enhance_AttackProperty_01_04.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_Enhance_AttackProperty_02.asset", 11400000L)
            };

            var timelines = new Dictionary<string, TimelineAsset>
            {
                ["Attack_Rush"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushTimeline.asset", 11400000L),
                ["Attack_Rush_Explode"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushExplodeTimeline.asset", 11400000L),
                ["Attack_Rush_End"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEndTimeline.asset", 11400000L),
                ["Attack_Rush_Enhance"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceTimeline.asset", 11400000L),
                ["Attack_Rush_Enhance_Loop"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceLoopTimeline.asset", 11400000L),
                ["Attack_Rush_Enhance_End"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceEndTimeline.asset", 11400000L),
                ["Attack_Rush_Enhance_Explode"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeTimeline.asset", 11400000L),
                ["Attack_Rush_Enhance_Explode_End"] = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeEndTimeline.asset", 11400000L)
            };

            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "RushAttack", parts.rootId, "RushAttack");
            var rootFlowNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), RushId("node:RushAttack.root"), "技能入口", new Vector2(-360f, 0f));
            var machineNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), RushId("node:RushAttack.machine"), "RushAttack StateMachine", new Vector2(240f, 0f));
            parts.machine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, RushId("machine:RushAttack"), "RushAttack StateMachine", parts.rootId, RushId("node:RushAttack.machine"));

            parts.states["Entry"] = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.machine, typeof(BtsmtlSkillNativeEntryState), RushId("state:Entry"), "状态机入口", new Vector2(-320f, 0f));
            parts.states["Exit"] = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.machine, typeof(BtsmtlSkillNativeExitState), RushId("state:Exit"), "状态机出口", new Vector2(1320f, 0f));
            var stateNames = new[] { "Attack_Rush", "Attack_Rush_Explode", "Attack_Rush_End", "Attack_Rush_Enhance", "Attack_Rush_Enhance_Loop", "Attack_Rush_Enhance_End", "Attack_Rush_Enhance_Explode", "Attack_Rush_Enhance_Explode_End" };
            for (int i = 0; i < stateNames.Length; i++)
            {
                var name = stateNames[i];
                parts.states[name] = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.machine, typeof(BtsmtlSkillNativeState), RushId($"state:{name}"), name, new Vector2(120f + i * 150f, i % 2 == 0 ? 220f : 420f));
                parts.bodyGraphs[name] = BuildStateBody(parts, name, timelines[name]);
            }

            var endRules = new[] { CreateEndRule(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort"), CreateEndRule(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt"), CreateEndRule(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RushClosed"), CreateEndRule(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted") };
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), parts.profile, effects, endRules, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Rush", true, "", "", false);
            BtsmtlSkillAuthoringContract.Apply(machineNode, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.machine) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, rootFlowNode, "Output", machineNode, "Input", RushId("edge:RushAttack.root"));

            AddDeclaration(parts, "RushSelected", "RushSelected");
            AddDeclaration(parts, "RushEnhanceSelected", "RushEnhanceSelected");
            AddDeclaration(parts, "RushHoldReleased", "RushHoldReleased");
            AddDeclaration(parts, "RushSawExplode", "RushSawExplode");
            AddDeclaration(parts, "RushNormalHandoff", "RushNormalHandoff");

            AddEdge(parts, "Entry", "Attack_Rush", RequestCondition(parts, "entry-rush-request", "Entry To Attack_Rush"));
            AddEdge(parts, "Entry", "Attack_Rush_Enhance", BooleanCondition(parts, "entry-enhance", "Entry To Attack_Rush_Enhance", "RushEnhanceSelected"));
            AddEdge(parts, "Attack_Rush", "Attack_Rush_Explode", BooleanCondition(parts, "rush-release", "Attack_Rush Release", "RushHoldReleased"));
            AddEdge(parts, "Attack_Rush", "Attack_Rush_Explode", BooleanCondition(parts, "rush-saw", "Attack_Rush SawExplode", "RushSawExplode"));
            AddEdge(parts, "Attack_Rush", "Attack_Rush_Explode", CompletedCondition(parts, "rush-terminal", "Attack_Rush Terminal"));
            AddEdge(parts, "Attack_Rush_Explode", "Exit", BooleanCondition(parts, "explode-normal", "Attack_Rush_Explode NormalHandoff", "RushNormalHandoff"));
            AddEdge(parts, "Attack_Rush_Explode", "Attack_Rush_End", CompletedCondition(parts, "explode-terminal", "Attack_Rush_Explode Terminal"));
            AddEdge(parts, "Attack_Rush_End", "Exit", CompletedCondition(parts, "end-terminal", "Attack_Rush_End Terminal"));
            AddEdge(parts, "Attack_Rush_Enhance", "Attack_Rush_Enhance_Loop", CompletedCondition(parts, "enhance-terminal", "Attack_Rush_Enhance Terminal"));
            AddEdge(parts, "Attack_Rush_Enhance_Loop", "Attack_Rush_Enhance_Explode", BooleanCondition(parts, "loop-saw", "Enhance_Loop SawExplode", "RushSawExplode"));
            AddEdge(parts, "Attack_Rush_Enhance_Loop", "Attack_Rush_Enhance_Loop", CompletedCondition(parts, "loop-reenter", "Enhance_Loop Reenter"));
            AddEdge(parts, "Attack_Rush_Enhance_Loop", "Attack_Rush_Enhance_End", BooleanCondition(parts, "loop-release", "Enhance_Loop Release", "RushHoldReleased"));
            AddEdge(parts, "Attack_Rush_Enhance_End", "Exit", BooleanCondition(parts, "enhance-end-normal", "Enhance_End NormalHandoff", "RushNormalHandoff"));
            AddEdge(parts, "Attack_Rush_Enhance_End", "Attack_Rush_End", CompletedCondition(parts, "enhance-end-terminal", "Enhance_End Terminal"));
            AddEdge(parts, "Attack_Rush_Enhance_Explode", "Exit", BooleanCondition(parts, "enhance-explode-normal", "Enhance_Explode NormalHandoff", "RushNormalHandoff"));
            AddEdge(parts, "Attack_Rush_Enhance_Explode", "Attack_Rush_Enhance_Explode_End", CompletedCondition(parts, "enhance-explode-terminal", "Enhance_Explode Terminal"));
            AddEdge(parts, "Attack_Rush_Enhance_Explode_End", "Exit", CompletedCondition(parts, "enhance-explode-end-terminal", "Enhance_Explode_End Terminal"));

            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph, new[] { RushId("node:RushAttack.root"), RushId("node:RushAttack.machine") }, new[] { RushId("edge:RushAttack.root") });
            return parts;
        }

        static void AddDeclaration(RootParts parts, string seed, string name)
        {
            string id = RushId($"declaration:{seed}");
            parts.declarations[name] = id;
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, id, name, typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, $"Action/Rush/{name}", null, null);
        }

        static void AddEdge(RootParts parts, string source, string target, BtsmtlSkillFlowGraph condition)
        {
            string seed = $"connection:{source}->{target}:{condition.name}";
            var connection = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.machine, parts.states[source], parts.states[target], RushId(seed));
            BtsmtlSkillAuthoringContract.ConfigureConnection(connection, condition, 0, ProgramAbortPolicy.None, GetOrder(parts, source));
            parts.connections.Add(RushId(seed));
        }

        static BtsmtlSkillFlowGraph CompletedCondition(RootParts parts, string seed, string name)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, name);
            var completed = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillStateRootCompletedFlowNode), RushId($"node:{seed}:completed"), "状态主体已完成", new Vector2(-360f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, completed, "m_Output", result, "m_Result", RushId($"edge:{seed}"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { RushId($"node:{seed}:completed"), RushId($"node:{seed}:result") }, new[] { RushId($"edge:{seed}") });
            return graph;
        }

        static BtsmtlSkillFlowGraph BooleanCondition(RootParts parts, string seed, string name, string declarationName)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, name);
            var value = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillBlackboardBooleanFlowNode), RushId($"node:{seed}:value"), declarationName, new Vector2(-520f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(value, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", parts.declarations[declarationName]), new BtsmtlSkillAuthoringFieldValue("ownerId", parts.rootId) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, value, "m_Output", result, "m_Result", RushId($"edge:{seed}"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { RushId($"node:{seed}:value"), RushId($"node:{seed}:result") }, new[] { RushId($"edge:{seed}") });
            return graph;
        }

        static BtsmtlSkillFlowGraph BuildStateBody(RootParts parts, string stateName, TimelineAsset timeline)
        {
            string seed = $"body:{stateName}";
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, $"{stateName} State Body");
            var root = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), RushId($"node:{seed}:root"), "技能入口", new Vector2(120f, 260f));
            var playback = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillTimelineFlowNode), RushId($"node:{seed}:timeline"), $"Play {stateName} Timeline", new Vector2(360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(playback, new[] { new BtsmtlSkillAuthoringFieldValue("timelineId", timeline), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared), new BtsmtlSkillAuthoringFieldValue("playbackMode", TimelinePlaybackMode.Once) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, root, "Output", playback, "Input", RushId($"edge:{seed}"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { RushId($"node:{seed}:root"), RushId($"node:{seed}:timeline") }, new[] { RushId($"edge:{seed}") });
            return graph;
        }

        static void FinalizeAuthoring(RootParts parts, BtsmtlAuthoringGenerationContext context)
        {
            foreach (var body in parts.bodyGraphs)
                BtsmtlSkillAuthoringCode.ConfigureNativeState(parts.states[body.Key], body.Value);
            BtsmtlSkillAuthoringCode.PruneBlackboard(parts.graph, new List<string>(parts.declarations.Values).ToArray());
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(parts.machine, new List<string>
            {
                RushId("state:Entry"),
                RushId("state:Attack_Rush"),
                RushId("state:Attack_Rush_Explode"),
                RushId("state:Attack_Rush_End"),
                RushId("state:Attack_Rush_Enhance"),
                RushId("state:Attack_Rush_Enhance_Loop"),
                RushId("state:Attack_Rush_Enhance_End"),
                RushId("state:Attack_Rush_Enhance_Explode"),
                RushId("state:Attack_Rush_Enhance_Explode_End"),
                RushId("state:Exit")
            }, parts.connections.ToArray());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, parts.graph);
        }

        static BtsmtlSkillFlowGraph RequestCondition(RootParts parts, string seed, string name)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, name);
            var request = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillActionRequestFlowNode), RushId($"node:{seed}:request"), "Has Rush Request", new Vector2(-520f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(request, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Rush"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, request, "m_Output", result, "m_Result", RushId($"edge:{seed}"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { RushId($"node:{seed}:request"), RushId($"node:{seed}:result") }, new[] { RushId($"edge:{seed}") });
            return graph;
        }

        static GameplayAbilityEndRule CreateEndRule(GameplayAbilityEndTrigger trigger, ActionLifecycleTransitionType transition, string window, string source)
        {
            var rule = new GameplayAbilityEndRule();
            rule.Configure(trigger, transition, window, source);
            return rule;
        }

        static int GetOrder(RootParts parts, string source)
        {
            if (!parts.orders.TryGetValue(source, out int order))
                order = 0;
            parts.orders[source] = order + 1;
            return order;
        }

        static string RushId(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity($"corin.rush.ability:{seed}")).ToString("D");

        sealed class RootParts
        {
            internal string rootId;
            internal GameplayAbilityAdmissionProfile profile;
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine machine;
            internal Dictionary<string, BtsmtlSkillNativeState> states = new Dictionary<string, BtsmtlSkillNativeState>();
            internal Dictionary<string, BtsmtlSkillFlowGraph> bodyGraphs = new Dictionary<string, BtsmtlSkillFlowGraph>();
            internal Dictionary<string, string> declarations = new Dictionary<string, string>();
            internal List<string> connections = new List<string>();
            internal Dictionary<string, int> orders = new Dictionary<string, int>();
        }
    }
}








