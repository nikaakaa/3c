using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentStateMachineMutationHandler : IAgentMutationHandler
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Emitters;
        readonly AgentConditionRuleBuilder m_ConditionBuilder;

        public AgentStateMachineMutationHandler(
            BtsmtlGraphAuthoringCapabilities emitters,
            AgentConditionRuleBuilder conditionBuilder)
        {
            m_Emitters = emitters;
            m_ConditionBuilder = conditionBuilder;
        }

        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineMutation stateMachine:
                    return PreflightStateMachine(session, stateMachine);
                case AgentEnsureStateMutation state:
                    return PreflightState(session, state);
                case AgentDeleteStateMutation deleteState:
                    return PreflightDeleteState(session, deleteState);
                case AgentRewireTransitionMutation rewire:
                    return PreflightRewireTransition(session, rewire);
                case AgentEnsureConditionRuleMutation condition:
                    return PreflightConditionRule(session, condition);
                case AgentEnsureTransitionMutation transition:
                    return PreflightTransition(session, transition);
                default:
                    throw new InvalidOperationException($"Unsupported StateMachine command: {command.Kind}");
            }
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineMutation stateMachine:
                    ApplyStateMachine(session, stateMachine);
                    break;
                case AgentEnsureStateMutation state:
                    ApplyState(session, state);
                    break;
                case AgentDeleteStateMutation deleteState:
                    ApplyDeleteState(session, deleteState);
                    break;
                case AgentRewireTransitionMutation rewire:
                    ApplyRewireTransition(session, rewire);
                    break;
                case AgentEnsureConditionRuleMutation condition:
                    ApplyConditionRule(session, condition);
                    break;
                case AgentEnsureTransitionMutation transition:
                    ApplyTransition(session, transition);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported StateMachine command: {command.Kind}");
            }
        }

        bool PreflightStateMachine(AgentMutationSession session, AgentEnsureStateMachineMutation command)
        {
            if (!session.TryResolveGraph(command.ParentGraph, command.Path, out BaseTree parent))
                return false;
            if (parent == null)
            {
                session.AddPlanned(command, null, command.DisplayName, "create or reuse StateMachine");
                return true;
            }

            StateMachineNode existing = null;
            if (command.ExistingOwner.IsValid)
            {
                if (!session.TryResolveNode(parent, command.ExistingOwner, command.Path, out BaseNode node))
                    return false;
                existing = node as StateMachineNode;
                if (node != null && existing == null)
                {
                    session.Report.Error(command.Path, "state_machine_owner_invalid", "targetElement identity 未指向 StateMachineNode。");
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(command.ExistingGraphAuthoringId))
            {
                existing = parent.Nodes.OfType<StateMachineNode>()
                    .SingleOrDefault(node => string.Equals(node.Graph?.GraphAuthoringId, command.ExistingGraphAuthoringId, StringComparison.Ordinal));
                if (existing == null)
                {
                    session.Report.Error(command.Path, "state_machine_identity_not_owned", "stateMachineGraphAuthoringId 不属于指定 parent Graph。");
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(command.LifecycleSlot) && parent is not StateBehaviorSubTree)
            {
                session.Report.Error(command.Path, "nested_state_machine_parent_invalid", "带 lifecycleSlot 的 StateMachineNode 必须位于 StateBehaviorSubTree。");
                return false;
            }
            if (existing == null &&
                (!m_Emitters.TryResolveNodeType("StateMachineNode", out Type nodeType) || !parent.CanCreateNodeType(nodeType)))
            {
                session.Report.Error(command.Path, "node_type_rejected", $"{parent.GetType().Name} 不能创建 StateMachineNode。");
                return false;
            }
            session.AddPlanned(command, parent, command.DisplayName, existing != null ? "reuse StateMachine" : "create StateMachine");
            return true;
        }

        bool PreflightState(AgentMutationSession session, AgentEnsureStateMutation command)
        {
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out StateMachineGraph graph))
                return false;
            if (graph == null)
            {
                session.AddPlanned(command, null, command.StateName, "create or reuse State");
                return true;
            }

            StateNode existing = null;
            if (command.ExistingState.IsValid)
            {
                if (!session.TryResolveNode(graph, new AgentElementTargetReference(command.ExistingState.Value), command.Path, out BaseNode node))
                    return false;
                existing = node as StateNode;
                if (node != null && existing == null)
                {
                    session.Report.Error(command.Path, "state_identity_wrong_type", "State identity 没有指向 StateNode。");
                    return false;
                }
            }
            if (existing == null &&
                (!m_Emitters.TryResolveNodeType("StateNode", out Type nodeType) || !graph.CanCreateNodeType(nodeType)))
            {
                session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 StateNode。");
                return false;
            }
            session.AddPlanned(command, graph, command.StateName, existing != null ? "reuse State" : "create State");
            return true;
        }

        bool PreflightTransition(AgentMutationSession session, AgentEnsureTransitionMutation command)
        {
            if (!session.TryResolveTransitionEndpoints(command, out StateMachineGraph graph, out BaseNode from, out BaseNode to))
                return false;
            if (!ValidateTransitionIdentity(session, command, graph, from, to))
                return false;
            session.AddPlanned(
                command,
                graph,
                from != null && to != null ? $"{from.ResolvedDisplayName}->{to.ResolvedDisplayName}" : $"{command.From.Identity}->{command.To.Identity}",
                "ensure transition");
            return true;
        }

        bool PreflightRewireTransition(
            AgentMutationSession session,
            AgentRewireTransitionMutation command)
        {
            if (!TryResolveRewire(
                    session,
                    command,
                    out StateMachineGraph graph,
                    out BaseNode from,
                    out BaseNode to,
                    out BaseEdge edge))
                return false;
            session.AddPlanned(
                command,
                graph,
                $"{from.ResolvedDisplayName}->{to.ResolvedDisplayName}",
                $"retarget transition {edge.GUID} and preserve its condition graph, flow order and abort policy");
            return true;
        }

        bool PreflightDeleteState(AgentMutationSession session, AgentDeleteStateMutation command)
        {
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out StateMachineGraph graph))
                return false;
            if (!session.TryResolveNode(graph, new AgentElementTargetReference(command.State.Value), command.Path, out BaseNode node))
                return false;
            if (node is not StateNode state)
            {
                session.Report.Error(command.Path, "state_identity_wrong_type", "delete_state 的 state identity 没有指向 StateNode。");
                return false;
            }
            session.AddPlanned(command, graph, state.ResolvedDisplayName, "delete State and owned transitions/body");
            return true;
        }

        bool PreflightConditionRule(AgentMutationSession session, AgentEnsureConditionRuleMutation command)
        {
            bool valid = PreflightTransition(session, command);
            valid &= m_ConditionBuilder.Preflight(session, command.Groups, command.Path);
            return valid;
        }

        void ApplyStateMachine(AgentMutationSession session, AgentEnsureStateMachineMutation command)
        {
            if (!session.TryResolveGraph(command.ParentGraph, command.Path, out BaseTree parent))
                return;

            StateMachineNode existing = null;
            if (command.ExistingOwner.IsValid)
            {
                if (!session.TryResolveNode(parent, command.ExistingOwner, command.Path, out BaseNode node) || node is not StateMachineNode stateMachine)
                {
                    session.Report.Error(command.Path, "state_machine_owner_invalid", "targetElement identity 未指向 StateMachineNode。");
                    return;
                }
                existing = stateMachine;
            }
            else if (!string.IsNullOrEmpty(command.ExistingGraphAuthoringId))
            {
                existing = parent.Nodes.OfType<StateMachineNode>()
                    .SingleOrDefault(node => string.Equals(node.Graph?.GraphAuthoringId, command.ExistingGraphAuthoringId, StringComparison.Ordinal));
                if (existing == null)
                {
                    session.Report.Error(command.Path, "state_machine_identity_not_owned", "stateMachineGraphAuthoringId 不属于指定 parent Graph。");
                    return;
                }
            }

            if (existing != null)
            {
                existing.DisplayName = command.DisplayName;
                existing.Position = command.Position;
                if (existing.Graph != null)
                    existing.Graph.name = command.DisplayName;
                TryLinkNested(session, parent, command.LifecycleSlot, existing, command.Path);
                session.AddApplied(command, parent, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(parent, "StateMachineNode", command.DisplayName, command.Position, out BaseNode created, session.Report, command.Path))
                return;
            StateMachineNode createdStateMachine = created as StateMachineNode;
            if (createdStateMachine?.Graph != null)
            {
                createdStateMachine.Graph.name = command.DisplayName;
                RemoveCompilerPlaceholderState(createdStateMachine.Graph);
            }
            TryLinkNested(session, parent, command.LifecycleSlot, createdStateMachine, command.Path);
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(command, parent, created, "created");
        }

        void ApplyState(AgentMutationSession session, AgentEnsureStateMutation command)
        {
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out StateMachineGraph graph))
                return;

            StateNode existing = null;
            if (command.ExistingState.IsValid)
            {
                if (!session.TryResolveNode(graph, new AgentElementTargetReference(command.ExistingState.Value), command.Path, out BaseNode node) || node is not StateNode state)
                {
                    session.Report.Error(command.Path, "state_identity_wrong_type", "State identity 没有指向 StateNode。");
                    return;
                }
                existing = state;
            }
            if (existing != null)
            {
                existing.DisplayName = command.StateName;
                existing.Position = command.Position;
                if (existing.SubTree != null)
                    existing.SubTree.name = $"{command.StateName} State Body";
                session.AddApplied(command, graph, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(graph, "StateNode", command.StateName, command.Position, out BaseNode created, session.Report, command.Path))
                return;
            if (created is StateNode createdState && createdState.SubTree != null)
                createdState.SubTree.name = $"{command.StateName} State Body";
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(command, graph, created, "created");
        }

        void ApplyTransition(AgentMutationSession session, AgentEnsureTransitionMutation command)
        {
            if (!session.TryResolveTransitionEndpoints(command, out StateMachineGraph graph, out BaseNode from, out BaseNode to))
                return;
            BaseEdge edge = ResolveOrCreateTransition(graph, from, to, command.EdgeAuthoringId);
            if (edge == null)
            {
                session.Report.Error(command.Path, "transition_not_created", $"Transition 未创建：{from?.ResolvedDisplayName} -> {to?.ResolvedDisplayName}");
                return;
            }
            edge.TransitionPriority = command.Priority;
            session.AddApplied(command, graph, edge, "transition");
        }

        void ApplyRewireTransition(
            AgentMutationSession session,
            AgentRewireTransitionMutation command)
        {
            if (!TryResolveRewire(
                    session,
                    command,
                    out StateMachineGraph graph,
                    out BaseNode from,
                    out BaseNode to,
                    out BaseEdge edge))
                return;
            graph.RetargetTransition(edge, from, to);
            edge.TransitionPriority = command.Priority;
            graph.CheckInit();
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(
                command,
                graph,
                edge,
                "retargeted transition with preserved condition graph, flow order and abort policy");
        }

        void ApplyDeleteState(AgentMutationSession session, AgentDeleteStateMutation command)
        {
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out StateMachineGraph graph))
                return;
            if (!session.TryResolveNode(graph, new AgentElementTargetReference(command.State.Value), command.Path, out BaseNode node) || node is not StateNode state)
            {
                session.Report.Error(command.Path, "state_identity_wrong_type", "delete_state 的 state identity 没有指向 StateNode。");
                return;
            }

            foreach (BaseEdge edge in graph.Edges.Where(edge => edge != null && (edge.StartNode == state || edge.EndNode == state)).ToList())
                graph.UnLink(edge);
            graph.DeleteNode(state);
            AgentMutationGraphAuthoringUtility.RemoveOrphanLinks(graph);
            graph.CheckInit();
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(command, graph, state, "deleted State and owned transitions/body");
        }

        void ApplyConditionRule(AgentMutationSession session, AgentEnsureConditionRuleMutation command)
        {
            if (!session.TryResolveTransitionEndpoints(command, out StateMachineGraph graph, out BaseNode from, out BaseNode to))
                return;
            BaseEdge edge = ResolveOrCreateTransition(graph, from, to, command.EdgeAuthoringId);
            if (edge == null)
            {
                session.Report.Error(command.Path, "transition_not_created", $"Transition 未创建：{from?.ResolvedDisplayName} -> {to?.ResolvedDisplayName}");
                return;
            }
            edge.TransitionPriority = command.Priority;
            edge.ConditionRuleGraph.name = $"{from.ResolvedDisplayName} To {to.ResolvedDisplayName} Condition";
            if (!m_ConditionBuilder.BuildTransitionRule(session, edge, command))
                return;
            session.AddApplied(command, graph, edge, "composed condition groups");
        }

        static bool ValidateTransitionIdentity(
            AgentMutationSession session,
            AgentEnsureTransitionMutation command,
            StateMachineGraph graph,
            BaseNode from,
            BaseNode to)
        {
            BaseEdge existing = graph.Edges.FirstOrDefault(edge => edge != null && string.Equals(edge.GUID, command.EdgeAuthoringId, StringComparison.Ordinal));
            if (existing == null)
                return true;
            if ((existing.StartNode == from || existing.StartNodeGUID == from.GUID) &&
                (existing.EndNode == to || existing.EndNodeGUID == to.GUID) &&
                string.Equals(existing.StartPortName, StateMachinePorts.StateOut, StringComparison.Ordinal) &&
                string.Equals(existing.EndPortName, StateMachinePorts.StateIn, StringComparison.Ordinal))
                return true;
            session.Report.Error(command.Path, "transition_identity_endpoint_mismatch", $"Transition identity {command.EdgeAuthoringId} 已属于另一组端点。");
            return false;
        }

        static bool TryResolveRewire(
            AgentMutationSession session,
            AgentRewireTransitionMutation command,
            out StateMachineGraph graph,
            out BaseNode from,
            out BaseNode to,
            out BaseEdge edge)
        {
            graph = null;
            from = null;
            to = null;
            edge = null;
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out graph))
                return false;
            if (graph == null)
            {
                session.Report.Error(
                    command.Path,
                    "transition_state_machine_missing",
                    "已有Transition改接端点必须引用当前已存在的StateMachine。");
                return false;
            }
            if (!session.TryResolveNode(graph, command.From, command.Path, out from) ||
                !session.TryResolveNode(graph, command.To, command.Path, out to))
                return false;
            edge = graph.Edges.FirstOrDefault(
                value => value != null &&
                         string.Equals(
                             value.GUID,
                             command.EdgeAuthoringId,
                             StringComparison.Ordinal));
            if (edge == null)
            {
                session.Report.Error(
                    command.Path,
                    "transition_identity_not_found",
                    $"Transition identity 不存在，不能改接端点：{command.EdgeAuthoringId}");
                return false;
            }
            if (!graph.IsTransitionEdge(edge))
            {
                session.Report.Error(
                    command.Path,
                    "transition_identity_wrong_type",
                    $"Transition identity 没有指向 StateMachine transition：{command.EdgeAuthoringId}");
                return false;
            }
            bool validStart =
                from is StateMachineEnterNode ||
                from is StateMachineAnyStateNode ||
                from is StateNode;
            bool validEnd = to is StateNode || to is StateMachineExitNode;
            if (validStart && validEnd)
                return true;
            session.Report.Error(
                command.Path,
                "transition_endpoint_invalid",
                $"StateMachine transition端点无效：{from?.ResolvedDisplayName}->{to?.ResolvedDisplayName}");
            return false;
        }

        static BaseEdge ResolveOrCreateTransition(StateMachineGraph graph, BaseNode from, BaseNode to, string edgeAuthoringId)
        {
            bool localIdentity = edgeAuthoringId?.StartsWith("local:", StringComparison.Ordinal) == true;
            BaseEdge edge = localIdentity
                ? null
                : graph.Edges.FirstOrDefault(value => value != null && string.Equals(value.GUID, edgeAuthoringId, StringComparison.Ordinal));
            if (edge != null)
                return edge;
            edge = graph.Link(from, to, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
            if (edge != null)
            {
                if (!localIdentity)
                    edge.SetAuthoringId(edgeAuthoringId);
                graph.CheckInit();
            }
            return edge;
        }

        static void TryLinkNested(
            AgentMutationSession session,
            BaseTree parent,
            string lifecycleSlot,
            StateMachineNode node,
            string path)
        {
            if (string.IsNullOrEmpty(lifecycleSlot))
                return;
            if (parent is not StateBehaviorSubTree stateBehavior)
            {
                session.Report.Error(path, "nested_state_machine_parent_invalid", "带 lifecycleSlot 的 StateMachineNode 必须位于 StateBehaviorSubTree。");
                return;
            }
            AgentMutationGraphAuthoringUtility.TryLinkLifecycleSlot(session, stateBehavior, lifecycleSlot, node, path);
        }

        static void RemoveCompilerPlaceholderState(StateMachineGraph graph)
        {
            if (graph == null || graph.StateNodes.Count() != 1)
                return;
            StateNode placeholder = graph.StateNodes.First();
            if (string.Equals(placeholder.ResolvedDisplayName, "State", StringComparison.Ordinal))
                graph.DeleteNode(placeholder);
        }
    }
}
