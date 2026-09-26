using FlowCanvas.Nodes;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static RushEnhanceParts BuildRushEnhance(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushEnhanceParts();
            var enhance = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceTimeline.asset", 11400000L);
            var loop = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceLoopTimeline.asset", 11400000L);
            var end = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceEndTimeline.asset", 11400000L);
            var explode = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeTimeline.asset", 11400000L);
            var explodeEnd = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeEndTimeline.asset", 11400000L);

            parts.bodyEnhance = BuildTimelineBody(rootParts, rootParts.enhanceStart, "Attack_Rush_Enhance", enhance, TimelinePlaybackMode.Once);
            parts.bodyLoop = BuildTimelineBody(rootParts, rootParts.enhanceLoop, "Attack_Rush_Enhance_Loop", loop, TimelinePlaybackMode.Loop);
            parts.bodyEnd = BuildTimelineBody(rootParts, rootParts.enhanceEnd, "Attack_Rush_Enhance_End", end, TimelinePlaybackMode.Once);
            parts.bodyExplode = BuildTimelineBody(rootParts, rootParts.enhanceExplode, "Attack_Rush_Enhance_Explode", explode, TimelinePlaybackMode.Once);
            parts.bodyExplodeEnd = BuildTimelineBody(rootParts, rootParts.enhanceExplodeEnd, "Attack_Rush_Enhance_Explode_End", explodeEnd, TimelinePlaybackMode.Once);
            parts.badge = BuildBadgeCondition(rootParts, rootParts.enhanceEntryEdge, true);
            parts.noBadge = BuildBadgeCondition(rootParts, rootParts.stateEdge, false);
            parts.startComplete = BuildTimelineTimeCondition(rootParts, rootParts.enhanceStartEdge, "enhance.start-complete", 38f / 60f);
            parts.loopReleased = BuildHeldCondition(rootParts, rootParts.enhanceLoopEndEdge, false);
            parts.sawExplode = BuildActionEventCondition(rootParts, rootParts.enhanceLoopExplodeEdge, "enhance.saw-explode", "SawExplode");
            parts.complete = BuildCompletedCondition(rootParts, rootParts.enhanceEndEdge, "enhance.complete");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceExplodeEdge, parts.complete.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceExplodeEndEdge, parts.complete.Graph, 0, ProgramAbortPolicy.None, 0);
            return parts;
        }

        static RushGraphPart BuildTimelineBody(
            RootParts rootParts,
            BtsmtlSkillNativeState state,
            string stateName,
            TimelineAsset timeline,
            TimelinePlaybackMode playbackMode)
        {
            string seed = $"enhance.body.{stateName}";
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph,
                RushId($"graph:{seed}"),
                typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.StateBody,
                $"{stateName} State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(state, graph);
            var root = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillRootFlowNode),
                RushId($"node:{seed}:root"),
                "技能入口",
                new Vector2(120f, 260f));
            var playback = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillTimelineFlowNode),
                RushId($"node:{seed}:timeline"),
                $"Play {stateName} Timeline",
                new Vector2(360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(playback, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("timelineId", timeline),
                new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared),
                new BtsmtlSkillAuthoringFieldValue("playbackMode", playbackMode)
            });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph,
                root,
                "Output",
                playback,
                "Input",
                RushId($"edge:{seed}"));
            return new RushGraphPart(graph, new[] { root.UID, playback.UID }, new[] { edge.UID });
        }

        static RushGraphPart BuildBadgeCondition(
            RootParts rootParts,
            BtsmtlSkillNativeConnection connection,
            bool expected)
        {
            string seed = expected ? "entry.badge" : "entry.no-badge";
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph,
                RushId($"graph:{seed}"),
                typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule,
                expected ? "Entry Badge_S01" : "Entry Without Badge_S01");
            BtsmtlSkillAuthoringContract.ConfigureConnection(
                connection,
                graph,
                0,
                ProgramAbortPolicy.None,
                expected ? 1 : 0);
            var tag = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillGameplayTagFlowNode),
                RushId($"node:{seed}:tag"),
                "Badge_S01",
                new Vector2(-520f, 0f));
            FlowCanvas.FlowNode not = null;
            if (!expected)
            {
                not = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph,
                    typeof(BtsmtlSkillNativeNodeWrapper<NOT>),
                    RushId($"node:{seed}:not"),
                    "NOT",
                    new Vector2(-240f, 0f));
            }
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillConditionResultFlowNode),
                RushId($"node:{seed}:result"),
                "条件结果",
                new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(tag, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("tagId", "Badge_S01"),
                new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:4a39b2d9de6248bc99baf9561c18716d")
            });
            var tagEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph,
                tag,
                "m_Result",
                expected ? result : not,
                expected ? "m_Result" : "value",
                RushId($"edge:{seed}:tag"));
            var output = expected
                ? tagEdge
                : BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, not, "Value", result, "m_Result", RushId($"edge:{seed}:result"));
            return new RushGraphPart(
                graph,
                not == null
                    ? new[] { tag.UID, result.UID }
                    : new[] { tag.UID, not.UID, result.UID },
                not == null
                    ? new[] { output.UID }
                    : new[] { tagEdge.UID, output.UID });
        }

        static RushGraphPart BuildHeldCondition(
            RootParts rootParts,
            BtsmtlSkillNativeConnection connection,
            bool held)
        {
            string seed = held ? "loop.held" : "loop.released";
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph,
                RushId($"graph:{seed}"),
                typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule,
                held ? "Enhance Loop Held" : "Enhance Loop Released");
            BtsmtlSkillAuthoringContract.ConfigureConnection(connection, graph, 0, ProgramAbortPolicy.None, 1);
            var input = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillBooleanInputFlowNode),
                RushId($"node:{seed}:input"),
                "AttackHeld",
                new Vector2(-520f, 180f));
            FlowCanvas.FlowNode not = null;
            if (!held)
            {
                not = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph,
                    typeof(BtsmtlSkillNativeNodeWrapper<NOT>),
                    RushId($"node:{seed}:not"),
                    "NOT",
                    new Vector2(-240f, 180f));
            }
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph,
                typeof(BtsmtlSkillConditionResultFlowNode),
                RushId($"node:{seed}:result"),
                "条件结果",
                new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(input, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("inputId", "AttackHeld"),
                new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809")
            });
            var inputEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, input, "m_Output", held ? result : not, held ? "m_Result" : "value", RushId($"edge:{seed}:input"));
            var edges = held
                ? new[] { inputEdge.UID }
                : new[]
                {
                    inputEdge.UID,
                    BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, not, "Value", result, "m_Result", RushId($"edge:{seed}:result")).UID
                };
            return new RushGraphPart(
                graph,
                not == null
                    ? new[] { input.UID, result.UID }
                    : new[] { input.UID, not.UID, result.UID },
                edges);
        }

        static RushGraphPart BuildTimelineTimeCondition(
            RootParts rootParts,
            BtsmtlSkillNativeConnection connection,
            string seed,
            float seconds)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到指定时间");
            BtsmtlSkillAuthoringContract.ConfigureConnection(connection, graph, 0, ProgramAbortPolicy.None, 0);
            var time = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillTimelineTimeFlowNode), RushId($"node:{seed}:time"), "Timeline时间", new Vector2(-360f, 0f));
            var reached = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), RushId($"node:{seed}:reached"), "到达指定时间", new Vector2(-100f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.SetValue(reached, "b", seconds);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, time, "m_Output", reached, "a", RushId($"edge:{seed}:time"));
            var resultEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, reached, "Value", result, "m_Result", RushId($"edge:{seed}:result"));
            return new RushGraphPart(graph, new[] { time.UID, reached.UID, result.UID }, new[] { edge.UID, resultEdge.UID });
        }

        static RushGraphPart BuildActionEventCondition(
            RootParts rootParts,
            BtsmtlSkillNativeConnection connection,
            string seed,
            string eventId)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph, RushId($"graph:{seed}"), typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule, eventId);
            BtsmtlSkillAuthoringContract.ConfigureConnection(connection, graph, 0, ProgramAbortPolicy.None, 0);
            var actionEvent = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillActionEventReceivedFlowNode), RushId($"node:{seed}:event"), eventId, new Vector2(-360f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(actionEvent, new[] { new BtsmtlSkillAuthoringFieldValue("eventId", eventId) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, actionEvent, "m_Output", result, "m_Result", RushId($"edge:{seed}"));
            return new RushGraphPart(graph, new[] { actionEvent.UID, result.UID }, new[] { edge.UID });
        }

        static RushGraphPart BuildCompletedCondition(
            RootParts rootParts,
            BtsmtlSkillNativeConnection connection,
            string seed)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                rootParts.graph,
                RushId($"graph:{seed}"),
                typeof(BtsmtlSkillFlowGraph),
                BtsmtlSkillFlowGraphRole.ConditionRule,
                "状态主体已完成");
            BtsmtlSkillAuthoringContract.ConfigureConnection(connection, graph, 0, ProgramAbortPolicy.None, 0);
            var completed = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillStateRootCompletedFlowNode), RushId($"node:{seed}:completed"), "状态主体已完成", new Vector2(-360f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), RushId($"node:{seed}:result"), "条件结果", new Vector2(600f, 180f));
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, completed, "m_Output", result, "m_Result", RushId($"edge:{seed}"));
            return new RushGraphPart(graph, new[] { completed.UID, result.UID }, new[] { edge.UID });
        }

        sealed class RushGraphPart
        {
            readonly string[] m_NodeIds;
            readonly string[] m_EdgeIds;

            internal RushGraphPart(BtsmtlSkillFlowGraph graph, string[] nodeIds, string[] edgeIds)
            {
                Graph = graph;
                m_NodeIds = nodeIds;
                m_EdgeIds = edgeIds;
            }

            internal BtsmtlSkillFlowGraph Graph { get; }

            internal void Prune() =>
                BtsmtlSkillAuthoringCode.PruneFlowGraph(Graph, m_NodeIds, m_EdgeIds);
        }

        sealed class RushEnhanceParts
        {
            internal RushGraphPart bodyEnhance;
            internal RushGraphPart bodyLoop;
            internal RushGraphPart bodyEnd;
            internal RushGraphPart bodyExplode;
            internal RushGraphPart bodyExplodeEnd;
            internal RushGraphPart badge;
            internal RushGraphPart noBadge;
            internal RushGraphPart loopReleased;
            internal RushGraphPart startComplete;
            internal RushGraphPart sawExplode;
            internal RushGraphPart complete;

            internal void Prune()
            {
                bodyEnhance.Prune();
                bodyLoop.Prune();
                bodyEnd.Prune();
                bodyExplode.Prune();
                bodyExplodeEnd.Prune();
                badge.Prune();
                noBadge.Prune();
                loopReleased.Prune();
                startComplete.Prune();
                sawExplode.Prune();
                complete.Prune();
            }
        }

    }
}
