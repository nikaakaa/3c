using System;
using BTSMTL.Timeline;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinBranchAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        const string InputOwner = "asset:be650df85b1e49ab9d1cefc91c6cc809";
        const string TimelineFolder = "Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/";

        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var admission = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>(
                "Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinBranchAdmissionProfile.asset", 11400000L);
            const string effectFolder = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_";
            var effects = new[]
            {
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "01_01.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "01_02.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "02_01.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "02_02_01.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "02_02_02.asset", 11400000L),
                context.ResolveExternalAsset<GameplayEffectDefinition>(effectFolder + "03.asset", 11400000L)
            };
            BtsmtlSkillFlowGraph root = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(
                context, "BranchAttack", Id("root"), "BranchAttack");
            var entryNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                root, typeof(BtsmtlSkillRootFlowNode), Id("root.entry"), "技能入口", new Vector2(-360f, 0f));
            var machineNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                root, typeof(BtsmtlSkillStateMachineFlowNode), Id("root.machine-node"),
                "BranchAttack StateMachine", new Vector2(240f, 0f));
            BtsmtlSkillNativeStateMachine machine = BtsmtlSkillAuthoringCode.EnsureStateMachine(
                root, Id("machine"), "BranchAttack StateMachine", Id("root"), Id("root.machine-node"));
            BtsmtlSkillAuthoringContract.Apply(machineNode, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("graphId", machine)
            });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                root, entryNode, "Output", machineNode, "Input", Id("root.entry-machine"));

            var entry = State(machine, "Entry", typeof(BtsmtlSkillNativeEntryState), -320f);
            var start = State(machine, "Start", typeof(BtsmtlSkillNativeState), 0f);
            var loop = State(machine, "Loop", typeof(BtsmtlSkillNativeState), 300f);
            var walk = State(machine, "Walk", typeof(BtsmtlSkillNativeState), 600f);
            var explode = State(machine, "Explode", typeof(BtsmtlSkillNativeState), 900f);
            var end = State(machine, "End", typeof(BtsmtlSkillNativeState), 1200f);
            var exit = State(machine, "Exit", typeof(BtsmtlSkillNativeExitState), 1500f);

            BtsmtlSkillFlowGraph startBody = Body(root, context, "Start");
            BtsmtlSkillFlowGraph loopBody = Body(root, context, "Loop");
            BtsmtlSkillFlowGraph walkBody = Body(root, context, "Walk");
            BtsmtlSkillFlowGraph explodeBody = Body(root, context, "Explode");
            BtsmtlSkillFlowGraph endBody = Body(root, context, "End");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(start, startBody);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(loop, loopBody);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(walk, walkBody);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(explode, explodeBody);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(end, endBody);

            BtsmtlSkillFlowGraph startRelease = Release(root, "StartRelease", true);
            BtsmtlSkillFlowGraph release = Release(root, "Release", false);
            BtsmtlSkillFlowGraph moving = Movement(root, "Moving", true);
            BtsmtlSkillFlowGraph stopped = Movement(root, "Stopped", false);
            BtsmtlSkillFlowGraph startComplete = Completed(root, "StartComplete");
            BtsmtlSkillFlowGraph explodeComplete = Completed(root, "ExplodeComplete");
            BtsmtlSkillFlowGraph endComplete = Completed(root, "EndComplete");

            Connect(machine, entry, start, "EntryStart", null, 0, 0);
            Connect(machine, start, explode, "StartExplode", startRelease, 100, 0);
            Connect(machine, start, loop, "StartLoop", startComplete, 0, 1);
            Connect(machine, loop, explode, "LoopExplode", release, 100, 0);
            Connect(machine, loop, walk, "LoopWalk", moving, 0, 1);
            Connect(machine, walk, explode, "WalkExplode", release, 100, 0);
            Connect(machine, walk, loop, "WalkLoop", stopped, 0, 1);
            Connect(machine, explode, end, "ExplodeEnd", explodeComplete, 0, 0);
            Connect(machine, end, exit, "EndExit", endComplete, 0, 0);

            var abort = new GameplayAbilityEndRule();
            abort.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var interrupt = new GameplayAbilityEndRule();
            interrupt.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var complete = new GameplayAbilityEndRule();
            complete.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(
                context, "", Array.Empty<GameplayTagId>(), admission,
                effects, new[] { abort, interrupt, complete },
                Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(),
                "Branch", true, "", "", true);

            BtsmtlSkillAuthoringCode.PruneFlowGraph(root,
                new[] { Id("root.entry"), Id("root.machine-node") },
                new[] { Id("root.entry-machine") });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(machine,
                new[] { Id("state.Start"), Id("state.Loop"), Id("state.Walk"), Id("state.Explode"), Id("state.End") },
                new[]
                {
                    Id("edge.EntryStart"), Id("edge.StartExplode"), Id("edge.StartLoop"),
                    Id("edge.LoopExplode"), Id("edge.LoopWalk"), Id("edge.WalkExplode"),
                    Id("edge.WalkLoop"), Id("edge.ExplodeEnd"), Id("edge.EndExit")
                });
            BtsmtlSkillAuthoringCode.PruneBlackboard(root, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, root);
            return context.Complete(root);
        }

        static BtsmtlSkillNativeState State(
            BtsmtlSkillNativeStateMachine machine, string stage, Type type, float x) =>
            BtsmtlSkillAuthoringCode.EnsureNativeState(
                machine, type, Id("state." + stage), stage, new Vector2(x, 0f));

        static BtsmtlSkillFlowGraph Body(
            BtsmtlSkillFlowGraph root, BtsmtlAuthoringGenerationContext context, string stage)
        {
            var timeline = context.ResolveExternalAsset<TimelineAsset>(
                TimelineFolder + "CorinBranch" + stage + "Timeline.asset", 11400000L);
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                root, Id("body." + stage), typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.StateBody, stage + " State Body");
            var onEnter = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillStateOnEnterFlowNode), Id("body." + stage + ".enter"),
                "进入状态", new Vector2(120f, 60f));
            var entry = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillRootFlowNode), Id("body." + stage + ".root"),
                "技能入口", new Vector2(120f, 260f));
            var play = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillTimelineFlowNode), Id("body." + stage + ".timeline"),
                "Play " + stage, new Vector2(360f, 260f));
            var onExit = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillStateOnExitFlowNode), Id("body." + stage + ".exit"),
                "退出状态", new Vector2(120f, 460f));
            BtsmtlSkillAuthoringContract.Apply(play, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("actionContext", null),
                new BtsmtlSkillAuthoringFieldValue("timelineId", timeline),
                new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared),
                new BtsmtlSkillAuthoringFieldValue("playbackMode", stage == "Loop" || stage == "Walk" ? TimelinePlaybackMode.Loop : TimelinePlaybackMode.Once)
            });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, entry, "Output", play, "Input", Id("body." + stage + ".play"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                new[]
                {
                    Id("body." + stage + ".enter"), Id("body." + stage + ".root"),
                    Id("body." + stage + ".timeline"), Id("body." + stage + ".exit")
                }, new[] { Id("body." + stage + ".play") });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph, Array.Empty<string>());
            return graph;
        }

        static BtsmtlSkillFlowGraph Release(BtsmtlSkillFlowGraph root, string name, bool gated)
        {
            var graph = Condition(root, name);
            var held = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillBooleanInputFlowNode), Id(name + ".held"),
                "BranchHeld", new Vector2(-520f, 0f));
            var not = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), Id(name + ".not"),
                "NOT", new Vector2(-240f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillConditionResultFlowNode), Id(name + ".result"),
                "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(held, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("inputId", "BranchHeld"),
                new BtsmtlSkillAuthoringFieldValue("providerOwnerId", InputOwner)
            });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, held, "m_Output", not, "value", Id(name + ".held-not"));
            if (gated)
            {
                var releaseWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph, typeof(BtsmtlSkillActionWindowActiveFlowNode), Id(name + ".window"),
                    "Branch Release Window", new Vector2(-240f, 160f));
                var both = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph, typeof(BtsmtlSkillNativeNodeWrapper<AND>), Id(name + ".both"),
                    "AND", new Vector2(200f, 100f));
                BtsmtlSkillAuthoringContract.Apply(releaseWindow, new[]
                {
                    new BtsmtlSkillAuthoringFieldValue("windowType", "BranchRelease")
                });
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, releaseWindow, "m_Output", both, "a", Id(name + ".window-both"));
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, not, "Value", both, "b", Id(name + ".not-both"));
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, both, "Value", result, "m_Result", Id(name + ".both-result"));
                BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                    new[]
                    {
                        Id(name + ".held"), Id(name + ".not"), Id(name + ".window"),
                        Id(name + ".both"), Id(name + ".result")
                    }, new[]
                    {
                        Id(name + ".held-not"), Id(name + ".window-both"),
                        Id(name + ".not-both"), Id(name + ".both-result")
                    });
            }
            else
            {
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, not, "Value", result, "m_Result", Id(name + ".not-result"));
                BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                    new[] { Id(name + ".held"), Id(name + ".not"), Id(name + ".result") },
                    new[] { Id(name + ".held-not"), Id(name + ".not-result") });
            }
            return graph;
        }

        static BtsmtlSkillFlowGraph Movement(BtsmtlSkillFlowGraph root, string name, bool moving)
        {
            var graph = Condition(root, name);
            var magnitude = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillInputMagnitudeFlowNode), Id(name + ".magnitude"),
                "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var compare = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), Id(name + ".greater"),
                ">", new Vector2(-240f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillConditionResultFlowNode), Id(name + ".result"),
                "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(magnitude, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"),
                new BtsmtlSkillAuthoringFieldValue("providerOwnerId", InputOwner)
            });
            BtsmtlSkillAuthoringCode.SetValue(compare, "b", 0.05f);
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, magnitude, "m_Output", compare, "a", Id(name + ".magnitude-greater"));
            if (moving)
            {
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, compare, "Value", result, "m_Result", Id(name + ".greater-result"));
                BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                    new[] { Id(name + ".magnitude"), Id(name + ".greater"), Id(name + ".result") },
                    new[] { Id(name + ".magnitude-greater"), Id(name + ".greater-result") });
            }
            else
            {
                var not = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), Id(name + ".not"),
                    "NOT", new Vector2(40f, 0f));
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, compare, "Value", not, "value", Id(name + ".greater-not"));
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, not, "Value", result, "m_Result", Id(name + ".not-result"));
                BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                    new[]
                    {
                        Id(name + ".magnitude"), Id(name + ".greater"),
                        Id(name + ".not"), Id(name + ".result")
                    }, new[]
                    {
                        Id(name + ".magnitude-greater"), Id(name + ".greater-not"),
                        Id(name + ".not-result")
                    });
            }
            return graph;
        }

        static BtsmtlSkillFlowGraph Completed(BtsmtlSkillFlowGraph root, string name)
        {
            var graph = Condition(root, name);
            var completed = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillStateRootCompletedFlowNode), Id(name + ".completed"),
                "状态主体已完成", new Vector2(-360f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(BtsmtlSkillConditionResultFlowNode), Id(name + ".result"),
                "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, completed, "m_Output", result, "m_Result", Id(name + ".completed-result"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph,
                new[] { Id(name + ".completed"), Id(name + ".result") },
                new[] { Id(name + ".completed-result") });
            return graph;
        }

        static BtsmtlSkillFlowGraph Condition(BtsmtlSkillFlowGraph root, string name)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                root, Id("condition." + name), typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule, name + " Condition");
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, Array.Empty<string>(), Array.Empty<string>());
            return graph;
        }

        static void Connect(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target,
            string name,
            BtsmtlSkillFlowGraph condition,
            int priority,
            int order)
        {
            var edge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(
                machine, source, target, Id("edge." + name));
            BtsmtlSkillAuthoringCode.ConfigureNativeConnection(
                edge, condition, priority, ProgramAbortPolicy.None, order);
        }

        static string Id(string part) =>
            new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity("corin.branch.02." + part)).ToString("D");
    }
}
