using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentStateMachineCommandHandler : IAgentPatchCommandHandler
    {
        readonly AgentNodeEmitterRegistry m_Emitters;
        readonly AgentConditionRuleBuilder m_ConditionBuilder;

        public AgentStateMachineCommandHandler(
            AgentNodeEmitterRegistry emitters,
            AgentConditionRuleBuilder conditionBuilder)
        {
            m_Emitters = emitters;
            m_ConditionBuilder = conditionBuilder;
        }

        public bool Preflight(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineCommand stateMachine:
                    return PreflightStateMachine(session, stateMachine);
                case AgentEnsureStateCommand state:
                    return PreflightState(session, state);
                case AgentDeleteStateCommand deleteState:
                    return PreflightDeleteState(session, deleteState);
                case AgentEnsureConditionRuleCommand condition:
                    return PreflightConditionRule(session, condition);
                case AgentEnsureTransitionCommand transition:
                    return PreflightTransition(session, transition);
                default:
                    throw new InvalidOperationException($"Unsupported StateMachine command: {command.Kind}");
            }
        }

        public void Apply(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineCommand stateMachine:
                    ApplyStateMachine(session, stateMachine);
                    break;
                case AgentEnsureStateCommand state:
                    ApplyState(session, state);
                    break;
                case AgentDeleteStateCommand deleteState:
                    ApplyDeleteState(session, deleteState);
                    break;
                case AgentEnsureConditionRuleCommand condition:
                    ApplyConditionRule(session, condition);
                    break;
                case AgentEnsureTransitionCommand transition:
                    ApplyTransition(session, transition);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported StateMachine command: {command.Kind}");
            }
        }

        bool PreflightStateMachine(AgentPatchCompileSession session, AgentEnsureStateMachineCommand command)
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

        bool PreflightState(AgentPatchCompileSession session, AgentEnsureStateCommand command)
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

        bool PreflightTransition(AgentPatchCompileSession session, AgentEnsureTransitionCommand command)
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

        bool PreflightDeleteState(AgentPatchCompileSession session, AgentDeleteStateCommand command)
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

        bool PreflightConditionRule(AgentPatchCompileSession session, AgentEnsureConditionRuleCommand command)
        {
            bool valid = PreflightTransition(session, command);
            valid &= m_ConditionBuilder.Preflight(session, command.Groups, command.Path);
            return valid;
        }

        void ApplyStateMachine(AgentPatchCompileSession session, AgentEnsureStateMachineCommand command)
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

        void ApplyState(AgentPatchCompileSession session, AgentEnsureStateCommand command)
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

        void ApplyTransition(AgentPatchCompileSession session, AgentEnsureTransitionCommand command)
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

        void ApplyDeleteState(AgentPatchCompileSession session, AgentDeleteStateCommand command)
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
            AgentPatchGraphAuthoringUtility.RemoveOrphanLinks(graph);
            graph.CheckInit();
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(command, graph, state, "deleted State and owned transitions/body");
        }

        void ApplyConditionRule(AgentPatchCompileSession session, AgentEnsureConditionRuleCommand command)
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
            AgentPatchCompileSession session,
            AgentEnsureTransitionCommand command,
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

        static BaseEdge ResolveOrCreateTransition(StateMachineGraph graph, BaseNode from, BaseNode to, string edgeAuthoringId)
        {
            BaseEdge edge = graph.Edges.FirstOrDefault(value => value != null && string.Equals(value.GUID, edgeAuthoringId, StringComparison.Ordinal));
            if (edge != null)
                return edge;
            edge = graph.Link(from, to, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
            if (edge != null)
            {
                edge.SetAuthoringId(edgeAuthoringId);
                graph.CheckInit();
            }
            return edge;
        }

        static void TryLinkNested(
            AgentPatchCompileSession session,
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
            AgentPatchGraphAuthoringUtility.TryLinkLifecycleSlot(session, stateBehavior, lifecycleSlot, node, path);
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
