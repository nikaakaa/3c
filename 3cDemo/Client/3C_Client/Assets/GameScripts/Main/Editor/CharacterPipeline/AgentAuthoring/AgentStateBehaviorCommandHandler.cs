using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentStateBehaviorCommandHandler : IAgentPatchCommandHandler
    {
        readonly AgentNodeEmitterRegistry m_Emitters;
        readonly AgentConditionRuleBuilder m_ConditionBuilder;

        public AgentStateBehaviorCommandHandler(
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
                case AgentEnsureActionExitLifecycleCommand actionExit:
                    return PreflightActionExit(session, actionExit);
                case AgentDeleteStateBehaviorNodeCommand delete:
                    return PreflightDelete(session, delete);
                case AgentEnsureStateBehaviorNodeCommand node:
                    return PreflightNode(session, node);
                case AgentEnsureTimelineNodeCommand timeline:
                    return PreflightTimeline(session, timeline);
                case AgentEnsureActionActivationCommand activation:
                    return PreflightActivation(session, activation);
                case AgentEnsureActionLifecycleTransitionCommand lifecycle:
                    return PreflightLifecycle(session, lifecycle);
                default:
                    throw new InvalidOperationException($"Unsupported StateBehavior command: {command.Kind}");
            }
        }

        public void Apply(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureActionExitLifecycleCommand actionExit:
                    ApplyActionExit(session, actionExit);
                    break;
                case AgentDeleteStateBehaviorNodeCommand delete:
                    ApplyDelete(session, delete);
                    break;
                case AgentEnsureStateBehaviorNodeCommand node:
                    ApplyNode(session, node);
                    break;
                case AgentEnsureTimelineNodeCommand timeline:
                    ApplyTimeline(session, timeline);
                    break;
                case AgentEnsureActionActivationCommand activation:
                    ApplyActivation(session, activation);
                    break;
                case AgentEnsureActionLifecycleTransitionCommand lifecycle:
                    ApplyLifecycle(session, lifecycle);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported StateBehavior command: {command.Kind}");
            }
        }

        bool PreflightActionExit(AgentPatchCompileSession session, AgentEnsureActionExitLifecycleCommand command)
        {
            bool valid = session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph);
            if (!session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext) || !actionContext)
            {
                session.Report.Error(command.Path, "action_context_missing", "Action exit lifecycle 缺少 Action Context。");
                session.Report.metrics.assetResolveFailureCount++;
                valid = false;
            }
            else
            {
                session.Report.metrics.assetResolvedCount++;
            }
            valid &= m_ConditionBuilder.Preflight(session, command.CancelConditionGroups, $"{command.Path}.cancelConditionGroups");
            if (graph != null)
            {
                BaseNode lifecycleParent = AgentPatchGraphAuthoringUtility.ResolveLifecycleAnchor(graph, "OnExit");
                if (!lifecycleParent)
                {
                    session.Report.Error(command.Path, "lifecycle_anchor_not_found", "Action exit lifecycle 无法解析 OnExit 入口。");
                    valid = false;
                }
                if (command.Source.IsValid)
                {
                    if (!session.TryResolveNode(graph, command.Source, command.Path, out BaseNode source) ||
                        source != null && source is not CompositeNode)
                    {
                        session.Report.Error(command.Path, "lifecycle_parent_invalid", "Action exit lifecycle source element 必须是 CompositeNode。");
                        valid = false;
                    }
                    else
                    {
                        lifecycleParent = source;
                    }
                }
                if (command.ExistingElement.IsValid)
                {
                    if (!session.TryResolveNode(graph, command.ExistingElement, command.Path, out BaseNode existing) ||
                        existing is not SelectorNode existingSelector ||
                        !string.Equals(existing.ResolvedDisplayName, "Action Exit", StringComparison.Ordinal))
                    {
                        session.Report.Error(command.Path, "action_exit_existing_invalid", "已有 Action exit 分支必须由 stable identity 指向 Agent 生成的 Action Exit SelectorNode。");
                        valid = false;
                    }
                    else if (!TryValidateGeneratedActionExit(graph, lifecycleParent, existingSelector, out string errorCode, out string errorMessage))
                    {
                        session.Report.Error(command.Path, errorCode, errorMessage);
                        valid = false;
                    }
                }
            }
            session.AddPlanned(command, graph, "Action Exit", "rebuild action exit lifecycle");
            return valid;
        }

        bool PreflightDelete(AgentPatchCompileSession session, AgentDeleteStateBehaviorNodeCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return false;
            if (graph != null && !session.TryResolveNode(graph, command.Element, command.Path, out _))
                return false;
            session.AddPlanned(command, graph, command.Element.Identity, "delete node");
            return true;
        }

        bool PreflightNode(AgentPatchCompileSession session, AgentEnsureStateBehaviorNodeCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return false;
            if (!m_Emitters.TryResolveNodeType(command.NodeType, out Type nodeType))
            {
                session.Report.Error(command.Path, "unknown_node_type", $"节点类型未登记：{command.NodeType}");
                return false;
            }
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                    return false;
                if (existing == null && !graph.CanCreateNodeType(nodeType))
                {
                    session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 {nodeType.Name}。");
                    return false;
                }
            }
            session.AddPlanned(command, graph, command.DisplayName, "create or reuse state behavior node");
            return true;
        }

        bool PreflightTimeline(AgentPatchCompileSession session, AgentEnsureTimelineNodeCommand command)
        {
            bool valid = session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph);
            bool requiresAsset = command.Ownership == AgentTimelineOwnership.Shared || command.TimelineAsset.HasExplicitAsset;
            if (requiresAsset && !session.Resolver.TryResolveTimelineAsset(command.TimelineAsset, out _))
            {
                session.Report.metrics.assetResolveFailureCount++;
                session.Report.Error(command.Path, "timeline_asset_not_found", $"TimelineAsset 无法解析：{command.TimelineAsset.AssetPath}");
                valid = false;
            }
            else if (requiresAsset)
            {
                session.Report.metrics.assetResolvedCount++;
            }
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                    valid = false;
                else if (existing != null && existing is not TimelineNode)
                {
                    session.Report.Error(command.Path, "target_element_type_mismatch", "targetElement identity 不是 TimelineNode。");
                    valid = false;
                }
                else if (existing == null &&
                         (!m_Emitters.TryResolveNodeType("TimelineNode", out Type nodeType) || !graph.CanCreateNodeType(nodeType)))
                {
                    session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 TimelineNode。");
                    valid = false;
                }
            }
            session.AddPlanned(command, graph, command.DisplayName, $"ensure {command.Ownership} TimelineNode");
            return valid;
        }

        bool PreflightActivation(AgentPatchCompileSession session, AgentEnsureActionActivationCommand command)
        {
            bool valid = session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph);
            if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out _))
            {
                session.Report.metrics.assetResolveFailureCount++;
                session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{command.ActionProfile.LogicalId}");
                valid = false;
            }
            else
            {
                session.Report.metrics.assetResolvedCount++;
            }
            if (!string.IsNullOrEmpty(command.SourceRequestId) && !session.Resolver.TryResolveActionRequest(command.SourceRequestId, out _))
            {
                session.Report.Error(command.Path, "request_not_found", $"Action activation source request 未在当前 InputProfile 中找到：{command.SourceRequestId}");
                valid = false;
            }
            if (!session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext) || !actionContext)
                session.Report.Warning(command.Path, "action_context_missing", "Action activation 没有配置 Action Context。");
            if (!string.IsNullOrWhiteSpace(command.TargetSnapshotBlackboardKey))
                valid &= session.TryResolveBlackboardDeclaration(command.TargetSnapshotBlackboardKey, typeof(ActionTargetSnapshot), command.Path, out _, out _);
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                    valid = false;
                else if (existing != null && existing is not ActivateActionInstanceNode)
                {
                    session.Report.Error(command.Path, "target_element_type_mismatch", "targetElement identity 不是 ActivateActionInstanceNode。");
                    valid = false;
                }
                else if (existing == null &&
                         (!m_Emitters.TryResolveNodeType("ActivateActionInstanceNode", out Type nodeType) || !graph.CanCreateNodeType(nodeType)))
                {
                    session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 ActivateActionInstanceNode。");
                    valid = false;
                }
            }
            session.AddPlanned(command, graph, command.DisplayName, "ensure action activation");
            return valid;
        }

        bool PreflightLifecycle(AgentPatchCompileSession session, AgentEnsureActionLifecycleTransitionCommand command)
        {
            bool valid = session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph);
            if (!session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext) || !actionContext)
                session.Report.Warning(command.Path, "action_context_missing", "Lifecycle transition 没有配置 Action Context。");
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                    valid = false;
                else if (existing != null && existing is not SubmitActionLifecycleTransitionNode)
                {
                    session.Report.Error(command.Path, "target_element_type_mismatch", "targetElement identity 不是 SubmitActionLifecycleTransitionNode。");
                    valid = false;
                }
                else if (existing == null &&
                         (!m_Emitters.TryResolveNodeType("SubmitActionLifecycleTransitionNode", out Type nodeType) || !graph.CanCreateNodeType(nodeType)))
                {
                    session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 SubmitActionLifecycleTransitionNode。");
                    valid = false;
                }
            }
            session.AddPlanned(command, graph, command.DisplayName, "ensure action lifecycle transition");
            return valid;
        }

        void ApplyActionExit(AgentPatchCompileSession session, AgentEnsureActionExitLifecycleCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return;
            if (!session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext) || !actionContext)
            {
                session.Report.Error(command.Path, "action_context_missing", "Action exit lifecycle 缺少 Action Context。");
                return;
            }
            BaseNode onExit = AgentPatchGraphAuthoringUtility.ResolveLifecycleAnchor(graph, "OnExit");
            if (!onExit)
            {
                session.Report.Error(command.Path, "lifecycle_anchor_not_found", "Action exit lifecycle 无法解析 OnExit 入口。");
                return;
            }
            BaseNode lifecycleParent = onExit;
            if (command.Source.IsValid)
            {
                if (!session.TryResolveNode(graph, command.Source, command.Path, out lifecycleParent) || lifecycleParent is not CompositeNode)
                {
                    session.Report.Error(command.Path, "lifecycle_parent_invalid", "Action exit lifecycle source element 必须是 CompositeNode。");
                    return;
                }
            }

            AgentPatchGraphAuthoringUtility.RemoveOrphanLinks(graph);
            if (command.ExistingElement.IsValid)
            {
                if (!session.TryResolveNode(graph, command.ExistingElement, command.Path, out BaseNode existing) ||
                    existing is not SelectorNode existingSelector ||
                    !RemoveGeneratedActionExit(graph, lifecycleParent, existingSelector, session, command.Path))
                    return;
            }
            Vector2 selectorPosition = command.Position == Vector2.zero
                ? new Vector2(lifecycleParent.Position.x + 240f, lifecycleParent.Position.y)
                : command.Position;
            float branchX = selectorPosition.x + 280f;

            if (!m_Emitters.TryCreateNode(graph, "SelectorNode", "Action Exit", selectorPosition, out BaseNode selectorNode, session.Report, command.Path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Window Cancel", new Vector2(branchX, selectorPosition.y - 160f), out BaseNode cancelNode, session.Report, command.Path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Tree Interrupt", new Vector2(branchX, selectorPosition.y - 60f), out BaseNode interruptNode, session.Report, command.Path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Tree Abort", new Vector2(branchX, selectorPosition.y + 40f), out BaseNode abortNode, session.Report, command.Path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Natural Complete", new Vector2(branchX, selectorPosition.y + 140f), out BaseNode completeNode, session.Report, command.Path) ||
                !m_Emitters.TryCreateNode(graph, "SucceedNode", "Succeed", new Vector2(branchX, selectorPosition.y + 240f), out BaseNode succeedNode, session.Report, command.Path))
                return;

            if (!m_Emitters.ConfigureLifecycleNode(cancelNode as SubmitActionLifecycleTransitionNode, actionContext, ActionLifecycleTransitionType.Cancel, command.CancelReason, session.Report, command.Path) ||
                !m_Emitters.ConfigureLifecycleNode(interruptNode as SubmitActionLifecycleTransitionNode, actionContext, ActionLifecycleTransitionType.Interrupt, command.InterruptReason, session.Report, command.Path) ||
                !m_Emitters.ConfigureLifecycleNode(abortNode as SubmitActionLifecycleTransitionNode, actionContext, ActionLifecycleTransitionType.Abort, command.AbortReason, session.Report, command.Path) ||
                !m_Emitters.ConfigureLifecycleNode(completeNode as SubmitActionLifecycleTransitionNode, actionContext, ActionLifecycleTransitionType.Complete, command.CompleteReason, session.Report, command.Path))
                return;

            graph.Link(lifecycleParent, selectorNode, "Output", "Input");
            BaseEdge cancelEdge = graph.Link(selectorNode, cancelNode, "Output", "Input");
            BaseEdge interruptEdge = graph.Link(selectorNode, interruptNode, "Output", "Input");
            BaseEdge abortEdge = graph.Link(selectorNode, abortNode, "Output", "Input");
            BaseEdge completeEdge = graph.Link(selectorNode, completeNode, "Output", "Input");
            BaseEdge succeedEdge = graph.Link(selectorNode, succeedNode, "Output", "Input");
            if (cancelEdge == null || interruptEdge == null || abortEdge == null || completeEdge == null || succeedEdge == null)
            {
                session.Report.Error(command.Path, "action_exit_edge_missing", "Action exit lifecycle 分支边创建失败。");
                return;
            }

            cancelEdge.SetConditionRuleGraph(m_ConditionBuilder.BuildActionExitRule(session, $"{graph.name}/ActionExit/Cancel", actionContext, AgentActionExitRuleKind.Cancel, command.CancelConditionGroups, command.Path));
            interruptEdge.SetConditionRuleGraph(m_ConditionBuilder.BuildActionExitRule(session, $"{graph.name}/ActionExit/Interrupt", actionContext, AgentActionExitRuleKind.Interrupt, command.CancelConditionGroups, command.Path));
            abortEdge.SetConditionRuleGraph(m_ConditionBuilder.BuildActionExitRule(session, $"{graph.name}/ActionExit/Abort", actionContext, AgentActionExitRuleKind.Abort, command.CancelConditionGroups, command.Path));
            completeEdge.SetConditionRuleGraph(m_ConditionBuilder.BuildActionExitRule(session, $"{graph.name}/ActionExit/Complete", actionContext, AgentActionExitRuleKind.Complete, command.CancelConditionGroups, command.Path));
            if (selectorNode is CompositeNode selector)
                selector.OrderChildren();
            if (lifecycleParent is CompositeNode parent)
                parent.OrderChildren();
            session.AddApplied(command, graph, selectorNode, "complete action exit lifecycle");
        }

        void ApplyDelete(AgentPatchCompileSession session, AgentDeleteStateBehaviorNodeCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph) ||
                !session.TryResolveNode(graph, command.Element, command.Path, out BaseNode node))
                return;
            string target = node.ResolvedDisplayName;
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(value => value.StartNode == node || value.EndNode == node).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(value => value.StartNode == node || value.EndNode == node).ToList())
                graph.UnLink(edge);
            graph.DeleteNode(node);
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddAppliedWithoutOutput(command, graph, target, "deleted");
        }

        void ApplyNode(AgentPatchCompileSession session, AgentEnsureStateBehaviorNodeCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return;
            if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                return;
            if (existing != null)
            {
                session.AddApplied(command, graph, existing, "exists");
                return;
            }
            if (!m_Emitters.TryCreateNode(graph, command.NodeType, command.DisplayName, command.Position, out BaseNode node, session.Report, command.Path))
                return;
            AgentPatchGraphAuthoringUtility.TryLinkLifecycleSlot(session, graph, command.LifecycleSlot, node, command.Path);
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddApplied(command, graph, node, "created");
        }

        void ApplyTimeline(AgentPatchCompileSession session, AgentEnsureTimelineNodeCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return;
            TimelineNode node = FindOrCreate<TimelineNode>(session, graph, command, command.ExistingElement, "TimelineNode", command.DisplayName);
            if (!node)
                return;
            bool requiresAsset = command.Ownership == AgentTimelineOwnership.Shared || command.TimelineAsset.HasExplicitAsset;
            TimelineAsset timelineAsset = null;
            if (requiresAsset && !session.Resolver.TryResolveTimelineAsset(command.TimelineAsset, out timelineAsset))
            {
                session.Report.Error(command.Path, "timeline_asset_not_found", $"TimelineAsset 无法解析：{command.TimelineAsset.AssetPath}");
                return;
            }
            session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext);
            if (!m_Emitters.ConfigureTimelineNode(node, command.Ownership, timelineAsset, actionContext, session.Report, command.Path))
                return;
            AgentPatchGraphAuthoringUtility.TryLinkLifecycleSlot(session, graph, command.LifecycleSlot, node, command.Path);
            session.AddApplied(command, graph, node, "timeline");
        }

        void ApplyActivation(AgentPatchCompileSession session, AgentEnsureActionActivationCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return;
            ActivateActionInstanceNode node = FindOrCreate<ActivateActionInstanceNode>(session, graph, command, command.ExistingElement, "ActivateActionInstanceNode", command.DisplayName);
            if (!node)
                return;
            if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out ActionProfile profile))
            {
                session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{command.ActionProfile.LogicalId}");
                return;
            }
            session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext);
            PipelineBlackboardVariableReference targetSnapshot = default;
            if (!string.IsNullOrWhiteSpace(command.TargetSnapshotBlackboardKey) &&
                !session.TryResolveBlackboardDeclaration(command.TargetSnapshotBlackboardKey, typeof(ActionTargetSnapshot), command.Path, out targetSnapshot, out _))
                return;
            if (!m_Emitters.ConfigureActionActivationNode(
                    node,
                    profile,
                    command.SourceRequestId,
                    command.ConsumeSourceRequest,
                    actionContext,
                    command.TargetKey,
                    targetSnapshot,
                    session.Report,
                    command.Path))
                return;
            AgentPatchGraphAuthoringUtility.TryLinkLifecycleSlot(session, graph, command.LifecycleSlot, node, command.Path);
            session.AddApplied(command, graph, node, "action activation");
        }

        void ApplyLifecycle(AgentPatchCompileSession session, AgentEnsureActionLifecycleTransitionCommand command)
        {
            if (!session.TryResolveStateBehavior(command.Target, command.Path, out StateBehaviorSubTree graph))
                return;
            SubmitActionLifecycleTransitionNode node = FindOrCreate<SubmitActionLifecycleTransitionNode>(session, graph, command, command.ExistingElement, "SubmitActionLifecycleTransitionNode", command.DisplayName);
            if (!node)
                return;
            session.Resolver.TryResolveActionContext(command.ActionContext, out ActionContextSlot actionContext);
            if (!m_Emitters.ConfigureLifecycleNode(node, actionContext, command.TransitionType, command.Reason, session.Report, command.Path))
                return;
            AgentPatchGraphAuthoringUtility.TryLinkLifecycleSlot(session, graph, command.LifecycleSlot, node, command.Path);
            session.AddApplied(command, graph, node, "lifecycle");
        }

        T FindOrCreate<T>(
            AgentPatchCompileSession session,
            StateBehaviorSubTree graph,
            AgentPatchCommand command,
            AgentElementTargetReference existingReference,
            string nodeType,
            string displayName)
            where T : BaseNode
        {
            if (!session.TryResolveOptionalNode(graph, existingReference, command.Path, out BaseNode existing))
                return null;
            if (existing != null)
            {
                if (existing is T typed)
                    return typed;
                session.Report.Error(command.Path, "target_element_type_mismatch", $"targetElement identity 不是 {typeof(T).Name}。");
                return null;
            }
            if (!m_Emitters.TryCreateNode(graph, nodeType, displayName, command.Position, out BaseNode created, session.Report, command.Path))
                return null;
            return created as T;
        }

        static bool RemoveGeneratedActionExit(
            BaseGraph graph,
            BaseNode lifecycleParent,
            SelectorNode selector,
            AgentPatchCompileSession session,
            string path)
        {
            if (!TryValidateGeneratedActionExit(graph, lifecycleParent, selector, out string errorCode, out string errorMessage))
            {
                session.Report.Error(path, errorCode, errorMessage);
                return false;
            }

            List<BaseNode> children = graph.Edges
                .Where(edge => edge.StartNode == selector)
                .Select(edge => edge.EndNode)
                .Distinct()
                .ToList();
            var nodes = new HashSet<BaseNode>(children) { selector };
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(value => nodes.Contains(value.StartNode) || nodes.Contains(value.EndNode)).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(value => nodes.Contains(value.StartNode) || nodes.Contains(value.EndNode)).ToList())
                graph.UnLink(edge);
            foreach (BaseNode node in nodes)
                graph.DeleteNode(node);
            AgentPatchGraphAuthoringUtility.RemoveOrphanLinks(graph);
            return true;
        }

        static bool TryValidateGeneratedActionExit(
            BaseGraph graph,
            BaseNode lifecycleParent,
            SelectorNode selector,
            out string errorCode,
            out string errorMessage)
        {
            errorCode = string.Empty;
            errorMessage = string.Empty;
            if (!string.Equals(selector.ResolvedDisplayName, "Action Exit", StringComparison.Ordinal) ||
                graph.Edges.Count(edge => edge.EndNode == selector) != 1 ||
                !graph.Edges.Any(edge => edge.StartNode == lifecycleParent && edge.EndNode == selector))
            {
                errorCode = "action_exit_existing_invalid";
                errorMessage = "已有 Action Exit selector 不属于指定 lifecycle parent。";
                return false;
            }

            List<BaseEdge> outgoing = graph.Edges.Where(edge => edge.StartNode == selector).ToList();
            List<BaseNode> children = outgoing
                .Select(edge => edge.EndNode)
                .Distinct()
                .ToList();
            List<ActionLifecycleTransitionType> transitions = children
                .OfType<SubmitActionLifecycleTransitionNode>()
                .Select(node => node.TransitionType)
                .Distinct()
                .ToList();
            bool hasExactTerminals =
                outgoing.Count == 5 &&
                children.Count == 5 &&
                transitions.Count == 4 &&
                transitions.Contains(ActionLifecycleTransitionType.Cancel) &&
                transitions.Contains(ActionLifecycleTransitionType.Interrupt) &&
                transitions.Contains(ActionLifecycleTransitionType.Abort) &&
                transitions.Contains(ActionLifecycleTransitionType.Complete) &&
                children.Count(node => node is SucceedNode && string.Equals(node.ResolvedDisplayName, "Succeed", StringComparison.Ordinal)) == 1 &&
                !graph.Edges.Any(edge => children.Contains(edge.StartNode));
            if (hasExactTerminals)
                return true;

            errorCode = "action_exit_existing_shape_invalid";
            errorMessage = "已有 Action Exit selector 已被手动改写，不能由 ensure 宏猜测删除。";
            return false;
        }
    }
}
