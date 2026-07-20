using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public interface IAgentPatchCommandHandler
    {
        bool Preflight(AgentPatchCompileSession session, AgentPatchCommand command);
        void Apply(AgentPatchCompileSession session, AgentPatchCommand command);
    }

    public sealed class AgentPatchCommandHandlerCatalog
    {
        readonly Dictionary<AgentPatchCommandKind, IAgentPatchCommandHandler> m_Handlers =
            new Dictionary<AgentPatchCommandKind, IAgentPatchCommandHandler>();

        public AgentPatchCommandHandlerCatalog()
        {
            var emitters = new AgentNodeEmitterRegistry();
            var conditionBuilder = new AgentConditionRuleBuilder();
            Register(new AgentStateMachineCommandHandler(emitters, conditionBuilder),
                AgentPatchCommandKind.EnsureStateMachine,
                AgentPatchCommandKind.EnsureState,
                AgentPatchCommandKind.DeleteState,
                AgentPatchCommandKind.EnsureTransition,
                AgentPatchCommandKind.EnsureConditionRule);
            Register(new AgentStateBehaviorCommandHandler(emitters, conditionBuilder),
                AgentPatchCommandKind.EnsureActionExitLifecycle,
                AgentPatchCommandKind.DeleteStateBehaviorNode,
                AgentPatchCommandKind.EnsureStateBehaviorNode,
                AgentPatchCommandKind.EnsureTimelineNode,
                AgentPatchCommandKind.EnsureActionActivation,
                AgentPatchCommandKind.EnsureActionLifecycleTransition);
            Register(new AgentNodeAssetCommandHandler(emitters), AgentPatchCommandKind.EnsureInputNode);
            Register(new AgentActionEligibilityCommandHandler(),
                AgentPatchCommandKind.EnsureBlackboardDeclaration,
                AgentPatchCommandKind.MoveBlackboardDeclaration,
                AgentPatchCommandKind.DeleteBlackboardDeclaration,
                AgentPatchCommandKind.EnsureBlackboardWrite,
                AgentPatchCommandKind.EnsureTimelineTreeClip,
                AgentPatchCommandKind.EnsureMotionWarpTrack,
                AgentPatchCommandKind.EnsureMotionWarpClip,
                AgentPatchCommandKind.ConfigureMotionWarpSource,
                AgentPatchCommandKind.ConfigureMotionWarpParameters,
                AgentPatchCommandKind.MoveTimelineClip,
                AgentPatchCommandKind.ConfigureTimelineClipEase,
                AgentPatchCommandKind.ConfigureTimelineCurveChannel,
                AgentPatchCommandKind.ConfigureAnimationTrackMarkerSync,
                AgentPatchCommandKind.EnsureAnimationSyncMarker,
                AgentPatchCommandKind.MoveAnimationSyncMarker,
                AgentPatchCommandKind.DeleteAnimationSyncMarker,
                AgentPatchCommandKind.DeleteTimelineClip,
                AgentPatchCommandKind.EnsureTreeClipBlackboardWrite,
                AgentPatchCommandKind.DeleteTransition,
                AgentPatchCommandKind.EnsureGameplayTag,
                AgentPatchCommandKind.SetActionProfileGrantedTags,
                AgentPatchCommandKind.SetActionProfileCancelQuery,
                AgentPatchCommandKind.SetActionProfileTargetRequirement,
                AgentPatchCommandKind.SetActionRequestTimingClass);
            Register(new AgentGraphLinkCommandHandler(), AgentPatchCommandKind.DeleteFlowEdge, AgentPatchCommandKind.LinkFlow, AgentPatchCommandKind.LinkProperty);
        }

        public IAgentPatchCommandHandler Get(AgentPatchCommandKind kind)
        {
            if (m_Handlers.TryGetValue(kind, out IAgentPatchCommandHandler handler))
                return handler;
            throw new InvalidOperationException($"Agent Patch command handler is not registered: {kind}");
        }

        void Register(IAgentPatchCommandHandler handler, params AgentPatchCommandKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                if (m_Handlers.ContainsKey(kinds[i]))
                    throw new InvalidOperationException($"Duplicate Agent Patch command handler: {kinds[i]}");
                m_Handlers.Add(kinds[i], handler);
            }
        }
    }

    public static class AgentPatchGraphAuthoringUtility
    {
        public static bool TryLinkLifecycleSlot(
            AgentPatchCompileSession session,
            StateBehaviorSubTree graph,
            string lifecycleSlot,
            BaseNode child,
            string path)
        {
            if (!child)
                return false;
            BaseNode anchor = ResolveLifecycleAnchor(graph, lifecycleSlot);
            if (!anchor)
            {
                session.Report.Error(path, "lifecycle_anchor_not_found", $"生命周期入口无法解析：{lifecycleSlot}");
                return false;
            }

            BaseEdge existing = FindAnyOutputEdge(graph, anchor, "Output");
            if (existing != null)
            {
                if (existing.EndNode == child)
                    return true;
                session.Report.Warning(path, "lifecycle_slot_occupied", $"{lifecycleSlot} 已连接到 {existing.EndNode?.ResolvedDisplayName}，未覆盖作者已有结构。");
                return false;
            }

            BaseEdge edge = graph.Link(anchor, child, "Output", "Input");
            if (edge != null)
                return true;
            session.Report.Error(path, "lifecycle_link_failed", $"无法连接 {lifecycleSlot} -> {child.ResolvedDisplayName}");
            return false;
        }

        public static BaseNode ResolveLifecycleAnchor(StateBehaviorSubTree graph, string lifecycleSlot)
        {
            string slot = string.IsNullOrEmpty(lifecycleSlot) ? "Root" : lifecycleSlot;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                if (slot == "Root" && node is RootNode)
                    return node;
                if (slot == "OnEnter" && node is StateOnEnterNode)
                    return node;
                if (slot == "OnExit" && node is StateOnExitNode)
                    return node;
            }
            return null;
        }

        public static BaseEdge EnsureSingleTransition(StateMachineGraph graph, BaseNode source, BaseNode target)
        {
            List<BaseEdge> matches = graph.Edges.Where(edge =>
                edge != null &&
                (edge.StartNode == source || edge.StartNodeGUID == source.GUID) &&
                (edge.EndNode == target || edge.EndNodeGUID == target.GUID) &&
                edge.StartPortName == StateMachinePorts.StateOut &&
                edge.EndPortName == StateMachinePorts.StateIn).ToList();
            BaseEdge transition = matches.FirstOrDefault();
            for (int i = 1; i < matches.Count; i++)
                graph.UnLink(matches[i]);
            return transition ?? graph.Link(source, target, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
        }

        public static BaseEdge FindFlowEdge(BaseGraph graph, BaseNode source, BaseNode target, string startPort, string endPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null && edge.StartNode == source && edge.EndNode == target &&
                    edge.StartPortName == startPort && edge.EndPortName == endPort)
                    return edge;
            }
            return null;
        }

        public static BaseEdge FindAnyOutputEdge(BaseGraph graph, BaseNode source, string startPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null && edge.StartNode == source && edge.StartPortName == startPort)
                    return edge;
            }
            return null;
        }

        public static void RemoveOrphanLinks(BaseGraph graph)
        {
            HashSet<string> nodeGuids = graph.Nodes.Where(node => node != null).Select(node => node.GUID).ToHashSet();
            graph.PropertyEdges.RemoveAll(edge => edge == null);
            graph.Edges.RemoveAll(edge => edge == null);
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) || !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) || !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLink(edge);
        }
    }
}
