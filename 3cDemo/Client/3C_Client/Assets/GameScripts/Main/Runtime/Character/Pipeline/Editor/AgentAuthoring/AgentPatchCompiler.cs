using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPatchCompiler
    {
        readonly AgentNodeEmitterRegistry m_Emitters = new AgentNodeEmitterRegistry();

        CharacterPipelineDefinition m_Definition;
        AgentGraphSnapshot m_Snapshot;
        AgentAssetResolver m_Resolver;
        AgentGraphAuthoringIndex m_Index;
        BaseTree m_RootTree;
        readonly HashSet<UnityEngine.Object> m_DirtyOwners = new HashSet<UnityEngine.Object>();
        readonly Dictionary<string, BaseGraph> m_OperationGraphs = new Dictionary<string, BaseGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, BaseNode> m_OperationNodes = new Dictionary<string, BaseNode>(StringComparer.Ordinal);
        readonly Dictionary<string, BaseEdge> m_OperationEdges = new Dictionary<string, BaseEdge>(StringComparer.Ordinal);
        readonly HashSet<string> m_OperationIds = new HashSet<string>(StringComparer.Ordinal);

        public AgentCompileReport Compile(CharacterPipelineDefinition definition, AgentGraphSnapshot snapshot, AgentPatchIR patch, bool apply)
        {
            AgentCompileReport report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            m_Definition = definition;
            m_Snapshot = snapshot;
            m_Resolver = new AgentAssetResolver(definition, snapshot);
            m_Index = new AgentGraphAuthoringIndex();
            m_DirtyOwners.Clear();
            m_OperationGraphs.Clear();
            m_OperationNodes.Clear();
            m_OperationEdges.Clear();
            m_OperationIds.Clear();

            if (patch == null)
            {
                report.Error("patch", "missing_patch", "AgentPatchIR 缺失。");
                return report;
            }

            if (!string.Equals(patch.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report.Error("patch.schemaVersion", "unsupported_schema_version", $"Patch schema 必须是 {AgentAuthoringSchema.Version}，当前为 {patch.schemaVersion}。");
                report.metrics.schemaInvalidCount++;
                return report;
            }

            if (!m_Resolver.TryGetRootTree(out m_RootTree, report, "definition"))
                return report;

            if (!RebuildIndex(report, "definition"))
                return report;
            if (patch.operations == null || patch.operations.Count == 0)
            {
                report.Error("patch.operations", "empty_patch", "Patch IR 没有任何操作。");
                return report;
            }

            for (int i = 0; i < patch.operations.Count; i++)
            {
                AgentPatchOperation operation = patch.operations[i];
                string path = $"patch.operations[{i}]";
                if (!ValidateOperationShape(operation, report, path))
                    continue;

                if (!apply)
                {
                    Plan(operation, report, path);
                    continue;
                }

                Apply(operation, report, path);
                if (!RebuildIndex(report, path))
                    break;
            }

            report.metrics.diffSize = apply ? report.appliedDiff.Count : report.plannedDiff.Count;
            report.applied = apply && !report.HasErrors();
            if (apply && !report.HasErrors())
                MarkDirty();
            report.success = !report.HasErrors();
            return report;
        }

        bool ValidateOperationShape(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (operation == null)
            {
                report.Error(path, "missing_operation", "Patch operation 为空。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (string.IsNullOrEmpty(operation.op))
            {
                report.Error(path, "missing_operation_type", "Patch operation 缺少 op。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (!IsSupportedOperation(operation.op))
            {
                report.Error(path, "unknown_operation", $"未知 Patch operation：{operation.op}");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (string.IsNullOrEmpty(operation.id) || !m_OperationIds.Add(operation.id))
            {
                report.Error(path, string.IsNullOrEmpty(operation.id) ? "operation_id_missing" : "operation_id_duplicate", "schema v6 要求每个 operation 使用唯一 id。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (string.Equals(operation.op, "ensure_condition_rule", StringComparison.Ordinal) &&
                !ValidateConditionGroups(operation.conditionGroups, report, path))
            {
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (string.Equals(operation.op, "ensure_action_exit_lifecycle", StringComparison.Ordinal) &&
                !ValidateConditionTerms(operation.cancelGuards, report, $"{path}.cancelGuards", false))
            {
                report.metrics.schemaInvalidCount++;
                return false;
            }

            if (!ValidateIdentityShape(operation, report, path))
            {
                report.metrics.schemaInvalidCount++;
                return false;
            }

            report.metrics.schemaValidCount++;
            return true;
        }

        static bool IsSupportedOperation(string operation)
        {
            switch (operation)
            {
                case "ensure_state_machine":
                case "ensure_state":
                case "ensure_transition":
                case "ensure_condition_rule":
                case "ensure_action_exit_lifecycle":
                case "delete_state_behavior_node":
                case "ensure_state_behavior_node":
                case "ensure_timeline_node":
                case "ensure_action_activation":
                case "ensure_action_lifecycle_transition":
                case "ensure_input_node":
                case "link_flow":
                case "link_property":
                case "bind_asset_reference":
                    return true;
                default:
                    return false;
            }
        }

        bool ValidateIdentityShape(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            bool valid = ValidateIdentityValues(operation, report, path);
            switch (operation.op)
            {
                case "ensure_state_machine":
                case "ensure_input_node":
                    valid &= RequireReference(operation.graphAuthoringId, operation.graphOperationId, report, path, "graph");
                    break;
                case "ensure_state":
                case "ensure_transition":
                case "ensure_condition_rule":
                    valid &= RequireReference(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, report, path, "stateMachine");
                    break;
                case "ensure_action_exit_lifecycle":
                case "delete_state_behavior_node":
                case "ensure_state_behavior_node":
                case "ensure_timeline_node":
                case "ensure_action_activation":
                case "ensure_action_lifecycle_transition":
                    bool hasDirectGraph = HasReference(operation.targetGraphAuthoringId, operation.targetGraphOperationId);
                    bool hasState = HasReference(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId) &&
                                    HasReference(operation.stateAuthoringId, operation.stateOperationId);
                    if (!hasDirectGraph && !hasState)
                    {
                        report.Error(path, "state_behavior_identity_missing", "Operation 必须用 target graph identity，或 StateMachine + State identity 指定 State body。");
                        valid = false;
                    }
                    if (operation.op == "delete_state_behavior_node")
                        valid &= RequireReference(operation.targetElementAuthoringId, operation.targetOperationId, report, path, "targetElement");
                    break;
                case "link_flow":
                case "link_property":
                    valid &= RequireReference(operation.graphAuthoringId, operation.graphOperationId, report, path, "graph");
                    valid &= RequireReference(operation.sourceElementAuthoringId, operation.sourceOperationId, report, path, "sourceElement");
                    valid &= RequireReference(operation.targetElementAuthoringId, operation.targetOperationId, report, path, "targetElement");
                    break;
            }

            if (operation.op == "ensure_transition" || operation.op == "ensure_condition_rule")
            {
                valid &= RequireReference(operation.fromElementAuthoringId, operation.fromOperationId, report, path, "fromElement");
                valid &= RequireReference(operation.toElementAuthoringId, operation.toOperationId, report, path, "toElement");
            }
            return valid;
        }

        bool ValidateIdentityValues(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            string[] identities =
            {
                operation.graphAuthoringId,
                operation.targetGraphAuthoringId,
                operation.stateMachineGraphAuthoringId,
                operation.stateAuthoringId,
                operation.fromElementAuthoringId,
                operation.toElementAuthoringId,
                operation.sourceElementAuthoringId,
                operation.targetElementAuthoringId,
                operation.timelineAuthoringId,
                operation.trackAuthoringId,
                operation.clipAuthoringId
            };
            bool valid = true;
            for (int i = 0; i < identities.Length; i++)
            {
                if (!string.IsNullOrEmpty(identities[i]) && !AuthoringIdentity.IsValid(identities[i]))
                {
                    report.Error(path, "authoring_identity_invalid", $"Authoring identity 格式无效：{identities[i]}");
                    valid = false;
                }
            }
            return valid;
        }

        bool RequireReference(string authoringId, string operationId, AgentCompileReport report, string path, string label)
        {
            if (HasReference(authoringId, operationId) && ValidateOperationReference(operationId, report, path, label))
                return true;
            report.Error(path, $"{label}_identity_missing", $"schema v6 operation 缺少 {label} authoring identity/operation reference。");
            return false;
        }

        bool ValidateOperationReference(string operationId, AgentCompileReport report, string path, string label)
        {
            if (string.IsNullOrEmpty(operationId))
                return true;
            int separator = operationId.IndexOf('#');
            string baseId = separator < 0 ? operationId : operationId.Substring(0, separator);
            if (m_OperationIds.Contains(baseId))
                return true;
            report.Error(path, $"{label}_operation_reference_invalid", $"Operation reference 必须指向当前 operation 或更早的 operation：{operationId}");
            return false;
        }

        static bool HasReference(string authoringId, string operationId) => !string.IsNullOrEmpty(authoringId) || !string.IsNullOrEmpty(operationId);

        static bool ValidateConditionGroups(List<AgentConditionGroup> groups, AgentCompileReport report, string path)
        {
            if (groups == null || groups.Count == 0)
            {
                report.Error($"{path}.conditionGroups", "condition_groups_empty", "ensure_condition_rule 必须包含至少一个条件组。");
                return false;
            }

            bool valid = true;
            for (int i = 0; i < groups.Count; i++)
            {
                AgentConditionGroup group = groups[i];
                if (group == null)
                {
                    report.Error($"{path}.conditionGroups[{i}]", "condition_group_missing", "Condition group 为空。");
                    valid = false;
                    continue;
                }

                valid &= ValidateConditionTerms(group.terms, report, $"{path}.conditionGroups[{i}].terms", false);
            }

            return valid;
        }

        static bool ValidateConditionTerms(
            List<AgentConditionTerm> terms,
            AgentCompileReport report,
            string path,
            bool allowEmpty)
        {
            if (terms == null || terms.Count == 0)
            {
                if (allowEmpty)
                    return true;

                report.Error(path, "condition_group_terms_empty", "Condition group 必须包含至少一个 term。");
                return false;
            }

            bool valid = true;
            for (int i = 0; i < terms.Count; i++)
            {
                AgentConditionTerm term = terms[i];
                string termPath = $"{path}[{i}]";
                if (term == null || string.IsNullOrWhiteSpace(term.kind))
                {
                    report.Error(termPath, "condition_term_kind_missing", "Condition term 缺少 kind。");
                    valid = false;
                    continue;
                }

                if (!IsSupportedConditionTermKind(term.kind))
                {
                    report.Error(termPath, "condition_term_unsupported", $"不支持的 Condition term：{term.kind}");
                    valid = false;
                }
            }

            return valid;
        }

        static bool IsSupportedConditionTermKind(string kind)
        {
            return string.Equals(kind, "move_stop", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "move_has", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "move_run", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "move_walk", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "turn_facing_angle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "blackboard_bool", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "state_root_completed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "action_request", StringComparison.OrdinalIgnoreCase);
        }

        void Plan(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            report.plannedDiff.Add(new AgentCompileDiffEntry
            {
                operationId = OperationId(operation, path),
                action = operation.op,
                graph = operation.graph,
                target = PrimaryTarget(operation),
                detail = "dry-run"
            });

            ValidateReferences(operation, report, path);
        }

        void Apply(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            switch (operation.op)
            {
                case "ensure_state_machine":
                    ApplyEnsureStateMachine(operation, report, path);
                    break;
                case "ensure_state":
                    ApplyEnsureState(operation, report, path);
                    break;
                case "ensure_transition":
                    ApplyEnsureTransition(operation, report, path);
                    break;
                case "ensure_condition_rule":
                    ApplyEnsureConditionRule(operation, report, path);
                    break;
                case "ensure_action_exit_lifecycle":
                    ApplyEnsureActionExitLifecycle(operation, report, path);
                    break;
                case "delete_state_behavior_node":
                    ApplyDeleteStateBehaviorNode(operation, report, path);
                    break;
                case "ensure_state_behavior_node":
                    ApplyEnsureStateBehaviorNode(operation, report, path);
                    break;
                case "ensure_timeline_node":
                    ApplyEnsureTimelineNode(operation, report, path);
                    break;
                case "ensure_action_activation":
                    ApplyEnsureActionActivation(operation, report, path);
                    break;
                case "ensure_action_lifecycle_transition":
                    ApplyEnsureLifecycle(operation, report, path);
                    break;
                case "ensure_input_node":
                    ApplyEnsureInputNode(operation, report, path);
                    break;
                case "link_flow":
                    ApplyLinkFlow(operation, report, path);
                    break;
                case "link_property":
                    ApplyLinkProperty(operation, report, path);
                    break;
                case "bind_asset_reference":
                    ValidateReferences(operation, report, path);
                    report.Info(path, "bind_asset_reference_noop", "bind_asset_reference 已由对应节点 emitter 处理。");
                    break;
                default:
                    report.Error(path, "unknown_operation", $"未知 Patch operation：{operation.op}");
                    break;
            }
        }

        void ApplyEnsureStateMachine(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveGraph(operation.graphAuthoringId, operation.graphOperationId, out BaseTree parentGraph, report, path))
                return;

            string displayName = Required(operation.displayName, operation.stateMachine, "State Machine");
            StateMachineNode existing = null;
            if (!string.IsNullOrEmpty(operation.targetElementAuthoringId) || !string.IsNullOrEmpty(operation.targetOperationId))
            {
                if (!ResolveNode(parentGraph, operation.targetElementAuthoringId, operation.targetOperationId, out BaseNode resolved, report, path) || resolved is not StateMachineNode stateMachine)
                {
                    report.Error(path, "state_machine_owner_invalid", "targetElementAuthoringId 未指向 StateMachineNode。");
                    return;
                }
                existing = stateMachine;
            }
            else if (!string.IsNullOrEmpty(operation.stateMachineGraphAuthoringId))
            {
                existing = parentGraph.Nodes.OfType<StateMachineNode>()
                    .SingleOrDefault(i => string.Equals(i.Graph?.GraphAuthoringId, operation.stateMachineGraphAuthoringId, StringComparison.Ordinal));
                if (existing == null)
                {
                    report.Error(path, "state_machine_identity_not_owned", "stateMachineGraphAuthoringId 不属于指定 parent Graph。");
                    return;
                }
            }

            if (existing != null)
            {
                StateMachineGraph existingGraph = existing.Graph;
                if (existingGraph != null && !string.IsNullOrEmpty(displayName))
                    existingGraph.name = displayName;
                TryLinkNestedStateMachine(parentGraph, operation.lifecycleSlot, existing, report, path);
                AddApplied(report, operation, parentGraph, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(parentGraph, "StateMachineNode", displayName, operation.position, out BaseNode created, report, path))
                return;

            StateMachineNode stateMachineNode = created as StateMachineNode;
            if (stateMachineNode?.Graph != null && !string.IsNullOrEmpty(displayName))
            {
                stateMachineNode.Graph.name = displayName;
                RemoveCompilerPlaceholderState(stateMachineNode.Graph);
            }
            TryLinkNestedStateMachine(parentGraph, operation.lifecycleSlot, stateMachineNode, report, path);
            AddApplied(report, operation, parentGraph, created, "created");
        }

        void ApplyEnsureState(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateMachineGraph(operation, out StateMachineGraph graph, report, path))
                return;

            string stateName = Required(operation.state, operation.displayName, string.Empty);
            if (string.IsNullOrEmpty(stateName))
            {
                report.Error(path, "missing_state_name", "ensure_state 缺少 state/displayName。");
                return;
            }

            StateNode existing = null;
            if (!string.IsNullOrEmpty(operation.stateAuthoringId) && !m_Index.TryFindState(graph, operation.stateAuthoringId, out existing))
            {
                report.Error(path, "state_identity_not_found", $"State authoring identity 无法解析：{operation.stateAuthoringId}");
                return;
            }
            if (!string.IsNullOrEmpty(operation.stateOperationId) && !string.Equals(operation.stateOperationId, operation.id, StringComparison.Ordinal))
            {
                if (!ResolveOperationNode(operation.stateOperationId, out BaseNode resolved) || resolved is not StateNode generated)
                {
                    report.Error(path, "state_operation_not_found", $"State operation reference 无法解析：{operation.stateOperationId}");
                    return;
                }
                existing = generated;
            }

            if (existing != null)
            {
                if (existing.SubTree != null)
                    existing.SubTree.name = $"{stateName} State Body";
                AddApplied(report, operation, graph, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(graph, "StateNode", stateName, operation.position, out BaseNode node, report, path))
                return;

            if (node is StateNode stateNode && stateNode.SubTree != null)
                stateNode.SubTree.name = $"{stateName} State Body";

            AddApplied(report, operation, graph, node, "created");
        }

        void TryLinkNestedStateMachine(
            BaseTree parentGraph,
            string lifecycleSlot,
            StateMachineNode node,
            AgentCompileReport report,
            string path)
        {
            if (string.IsNullOrEmpty(lifecycleSlot))
                return;
            if (!(parentGraph is StateBehaviorSubTree stateBehavior))
            {
                report.Error(path, "nested_state_machine_parent_invalid", "带 lifecycleSlot 的 StateMachineNode 必须位于 StateBehaviorSubTree。");
                return;
            }
            TryLinkLifecycleSlot(stateBehavior, lifecycleSlot, node, report, path);
        }

        static void RemoveCompilerPlaceholderState(StateMachineGraph graph)
        {
            if (graph == null || graph.StateNodes.Count() != 1)
                return;
            StateNode placeholder = graph.StateNodes.First();
            if (!string.Equals(placeholder.ResolvedDisplayName, "State", StringComparison.Ordinal))
                return;
            graph.DeleteNode(placeholder);
        }

        void ApplyEnsureTransition(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveTransitionEndpoints(operation, report, path, out StateMachineGraph graph, out BaseNode from, out BaseNode to))
                return;

            BaseEdge edge = EnsureSingleTransition(graph, from, to);

            if (edge == null)
            {
                report.Error(path, "transition_not_created", $"Transition 未创建：{operation.from} -> {operation.to}");
                return;
            }

            edge.TransitionPriority = operation.transitionPriority;
            AddApplied(report, operation, graph, edge, "transition");
        }

        void ApplyEnsureConditionRule(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveTransitionEndpoints(operation, report, path, out StateMachineGraph graph, out BaseNode from, out BaseNode to))
                return;

            BaseEdge edge = EnsureSingleTransition(graph, from, to);

            if (edge == null)
            {
                report.Error(path, "transition_not_created", $"Transition 未创建：{operation.from} -> {operation.to}");
                return;
            }

            edge.TransitionPriority = operation.transitionPriority;
            ComposeConditionRule(edge, operation, report, path);
            AddApplied(report, operation, graph, edge, "composed condition groups");
        }

        void ApplyEnsureActionExitLifecycle(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            if (!m_Resolver.TryResolveActionContext(operation, out ActionContextSlot actionContext) || !actionContext)
            {
                report.Error(path, "action_context_missing", "Action exit lifecycle 缺少 Action Context。");
                return;
            }

            BaseNode onExit = ResolveLifecycleAnchor(graph, "OnExit");
            if (onExit == null)
            {
                report.Error(path, "lifecycle_anchor_not_found", "Action exit lifecycle 无法解析 OnExit 入口。");
                return;
            }

            BaseNode lifecycleParent = onExit;
            if (!string.IsNullOrEmpty(operation.sourceElementAuthoringId) || !string.IsNullOrEmpty(operation.sourceOperationId))
            {
                if (!ResolveNode(graph, operation.sourceElementAuthoringId, operation.sourceOperationId, out lifecycleParent, report, path))
                {
                    report.Error(path, "lifecycle_parent_not_found", "Action exit lifecycle 无法解析 source element identity。");
                    return;
                }

                if (lifecycleParent is not CompositeNode)
                {
                    report.Error(path, "lifecycle_parent_invalid", $"Action exit lifecycle sourceNode 必须是 CompositeNode：{operation.sourceNode}");
                    return;
                }
            }

            RemoveOrphanLinks(graph);
            RemoveActionExitNodes(graph);

            Vector2 selectorPosition = operation.position == Vector2.zero
                ? new Vector2(lifecycleParent.Position.x + 240f, lifecycleParent.Position.y)
                : operation.position;
            float branchX = selectorPosition.x + 280f;

            if (!m_Emitters.TryCreateNode(graph, "SelectorNode", "Action Exit", selectorPosition, out BaseNode selectorNode, report, path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Window Cancel", new Vector2(branchX, selectorPosition.y - 120f), out BaseNode cancelNode, report, path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Tree Abort", new Vector2(branchX, selectorPosition.y - 20f), out BaseNode abortNode, report, path) ||
                !m_Emitters.TryCreateNode(graph, "SubmitActionLifecycleTransitionNode", "Submit Natural Complete", new Vector2(branchX, selectorPosition.y + 80f), out BaseNode completeNode, report, path) ||
                !m_Emitters.TryCreateNode(graph, "SucceedNode", "Succeed", new Vector2(branchX, selectorPosition.y + 180f), out BaseNode succeedNode, report, path))
                return;

            var cancelLifecycle = cancelNode as SubmitActionLifecycleTransitionNode;
            var abortLifecycle = abortNode as SubmitActionLifecycleTransitionNode;
            var completeLifecycle = completeNode as SubmitActionLifecycleTransitionNode;
            if (!m_Emitters.ConfigureLifecycleNode(
                    cancelLifecycle,
                    actionContext,
                    ActionLifecycleTransitionType.Cancel,
                    Required(operation.reason, "ComboWindow", string.Empty),
                    report,
                    path) ||
                !m_Emitters.ConfigureLifecycleNode(
                    abortLifecycle,
                    actionContext,
                    ActionLifecycleTransitionType.Abort,
                    Required(operation.abortReason, "TreeAbort", string.Empty),
                    report,
                    path) ||
                !m_Emitters.ConfigureLifecycleNode(
                    completeLifecycle,
                    actionContext,
                    ActionLifecycleTransitionType.Complete,
                    Required(operation.completeReason, "TimelineCompleted", string.Empty),
                    report,
                    path))
                return;

            graph.Link(lifecycleParent, selectorNode, "Output", "Input");
            BaseEdge cancelEdge = graph.Link(selectorNode, cancelNode, "Output", "Input");
            BaseEdge abortEdge = graph.Link(selectorNode, abortNode, "Output", "Input");
            BaseEdge completeEdge = graph.Link(selectorNode, completeNode, "Output", "Input");
            BaseEdge succeedEdge = graph.Link(selectorNode, succeedNode, "Output", "Input");
            if (cancelEdge == null || abortEdge == null || completeEdge == null || succeedEdge == null)
            {
                report.Error(path, "action_exit_edge_missing", "Action exit lifecycle 分支边创建失败。");
                return;
            }

            cancelEdge.SetConditionRuleGraph(CreateActionExitRule(
                $"{graph.name}/ActionExit/Cancel",
                actionContext,
                ActionExitRuleKind.Cancel,
                operation,
                report,
                path));
            abortEdge.SetConditionRuleGraph(CreateActionExitRule(
                $"{graph.name}/ActionExit/Abort",
                actionContext,
                ActionExitRuleKind.Abort,
                operation,
                report,
                path));
            completeEdge.SetConditionRuleGraph(CreateActionExitRule(
                $"{graph.name}/ActionExit/Complete",
                actionContext,
                ActionExitRuleKind.Complete,
                operation,
                report,
                path));

            if (selectorNode is CompositeNode selector)
                selector.OrderChildren();
            if (lifecycleParent is CompositeNode parent)
                parent.OrderChildren();
            AddApplied(report, operation, graph, selectorNode, "complete action exit lifecycle");
        }

        void ApplyDeleteStateBehaviorNode(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            if (string.IsNullOrEmpty(operation.targetElementAuthoringId) && string.IsNullOrEmpty(operation.targetOperationId))
            {
                report.Error(path, "target_identity_missing", "delete_state_behavior_node 缺少 targetElementAuthoringId/targetOperationId。");
                return;
            }

            if (!ResolveNode(graph, operation.targetElementAuthoringId, operation.targetOperationId, out BaseNode node, report, path))
            {
                report.Error(path, "target_identity_not_found", "delete_state_behavior_node 的 target identity 无法解析。");
                return;
            }

            foreach (PropertyEdge propertyEdge in graph.PropertyEdges.Where(i => i.StartNode == node || i.EndNode == node).ToList())
                graph.UnLinkProperty(propertyEdge);
            foreach (BaseEdge edge in graph.Edges.Where(i => i.StartNode == node || i.EndNode == node).ToList())
                graph.UnLink(edge);
            graph.DeleteNode(node);
            AddApplied(report, operation, graph, node, "deleted");
        }

        void ApplyEnsureStateBehaviorNode(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            string nodeType = Required(operation.nodeType, "SequenceNode", string.Empty);
            string displayName = Required(operation.displayName, nodeType, string.Empty);
            if (!TryResolveOptionalTargetNode(graph, operation, out BaseNode existing, report, path))
                return;
            if (existing != null)
            {
                AddApplied(report, operation, graph, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(graph, nodeType, displayName, operation.position, out BaseNode node, report, path))
                return;

            TryLinkLifecycleSlot(graph, operation.lifecycleSlot, node, report, path);
            AddApplied(report, operation, graph, node, "created");
        }

        void ApplyEnsureTimelineNode(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            string displayName = Required(operation.displayName, Required(operation.timeline, "Timeline", string.Empty), string.Empty);
            TimelineNode node = FindOrCreateStateBehaviorNode<TimelineNode>(graph, operation, "TimelineNode", displayName, operation.position, report, path);
            if (!node)
                return;

            if (!Enum.TryParse(operation.timelineOwnership, true, out AgentTimelineOwnership ownership))
            {
                report.Error(path, "timeline_ownership_invalid", $"Timeline ownership 无效：{operation.timelineOwnership}", "使用 Inline 或 Shared。");
                return;
            }

            m_Resolver.TryResolveActionContext(operation, out ActionContextSlot actionContext);
            bool requiresAsset = ownership == AgentTimelineOwnership.Shared ||
                                 !string.IsNullOrEmpty(operation.timelineAssetPath) ||
                                 !string.IsNullOrEmpty(operation.timelineAssetGuid);
            TimelineAsset timelineAsset = null;
            if (requiresAsset && !m_Resolver.TryResolveTimelineAsset(operation, out timelineAsset))
            {
                report.metrics.assetResolveFailureCount++;
                report.Error(path, "timeline_asset_not_found", $"TimelineAsset 无法解析：{operation.timelineAssetPath}");
                return;
            }
            if (timelineAsset)
                report.metrics.assetResolvedCount++;

            if (!m_Emitters.ConfigureTimelineNode(node, ownership, timelineAsset, actionContext, report, path))
                return;

            TryLinkLifecycleSlot(graph, string.IsNullOrEmpty(operation.lifecycleSlot) ? "Root" : operation.lifecycleSlot, node, report, path);
            AddApplied(report, operation, graph, node, "timeline");
        }

        void ApplyEnsureActionActivation(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            string displayName = Required(operation.displayName, $"Activate {operation.actionProfile}", "Activate Action");
            ActivateActionInstanceNode node = FindOrCreateStateBehaviorNode<ActivateActionInstanceNode>(graph, operation, "ActivateActionInstanceNode", displayName, operation.position, report, path);
            if (!node)
                return;

            if (!m_Resolver.TryResolveActionProfile(operation.actionProfile, out ActionProfile actionProfile))
            {
                report.metrics.assetResolveFailureCount++;
                report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{operation.actionProfile}");
                return;
            }
            report.metrics.assetResolvedCount++;

            m_Resolver.TryResolveActionContext(operation, out ActionContextSlot actionContext);
            if (!actionContext)
                report.Warning(path, "action_context_missing", "Action activation 没有配置 Action Context。", "为 Patch 提供 actionContextAssetPath/actionContextAssetGuid，或在 snapshot 中引用一个 Action Context。");

            string sourceRequestId = Required(operation.sourceInputRequestId, operation.inputId, operation.request);
            if (!string.IsNullOrEmpty(sourceRequestId) && !m_Resolver.TryResolveActionRequest(sourceRequestId, out _))
            {
                report.Error(path, "request_not_found", $"Action activation source request 未在当前 InputProfile 中找到：{sourceRequestId}");
                return;
            }

            PipelineBlackboardVariableReference targetSnapshotVariable = default;
            if (!string.IsNullOrWhiteSpace(operation.targetSnapshotBlackboardKey) &&
                !TryResolveBlackboardDeclaration(operation.targetSnapshotBlackboardKey, typeof(ActionTargetSnapshot), report, path, out targetSnapshotVariable))
                return;

            if (!m_Emitters.ConfigureActionActivationNode(node, actionProfile, sourceRequestId, operation.consumeSourceInputRequest, actionContext, operation.targetKey, targetSnapshotVariable, report, path))
                return;

            TryLinkLifecycleSlot(graph, string.IsNullOrEmpty(operation.lifecycleSlot) ? "OnEnter" : operation.lifecycleSlot, node, report, path);
            AddApplied(report, operation, graph, node, "action activation");
        }

        void ApplyEnsureLifecycle(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveStateBehaviorGraph(operation, report, path, out StateBehaviorSubTree graph))
                return;

            string displayName = Required(operation.displayName, $"Lifecycle {operation.lifecycleType}", "Submit Lifecycle");
            SubmitActionLifecycleTransitionNode node = FindOrCreateStateBehaviorNode<SubmitActionLifecycleTransitionNode>(graph, operation, "SubmitActionLifecycleTransitionNode", displayName, operation.position, report, path);
            if (!node)
                return;

            if (!Enum.TryParse(operation.lifecycleType, true, out ActionLifecycleTransitionType transitionType) ||
                !Enum.IsDefined(typeof(ActionLifecycleTransitionType), transitionType))
            {
                report.Error(path, "lifecycle_type_invalid", $"未知的 lifecycleType：{operation.lifecycleType}");
                return;
            }

            m_Resolver.TryResolveActionContext(operation, out ActionContextSlot actionContext);
            if (!actionContext)
                report.Warning(path, "action_context_missing", "Lifecycle transition 没有配置 Action Context。");

            if (!m_Emitters.ConfigureLifecycleNode(node, actionContext, transitionType, operation.reason, report, path))
                return;

            TryLinkLifecycleSlot(graph, string.IsNullOrEmpty(operation.lifecycleSlot) ? "OnExit" : operation.lifecycleSlot, node, report, path);
            AddApplied(report, operation, graph, node, "lifecycle");
        }

        void ApplyEnsureInputNode(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveGraph(operation.graphAuthoringId, operation.graphOperationId, out BaseTree graph, report, path))
                return;

            string nodeType = operation.nodeType;
            if (string.IsNullOrEmpty(nodeType))
            {
                if (m_Resolver.TryResolveActionRequest(operation.inputId, out _))
                    nodeType = "CharacterActionRequestInfoNode";
                else if (m_Resolver.TryResolveInputValue(operation.inputId, out CharacterInputValueDefinition inputValue))
                    nodeType = AgentNodeEmitterRegistry.ResolveInputNodeType(inputValue.ValueType);
            }

            if (string.IsNullOrEmpty(nodeType))
            {
                report.Error(path, "input_not_found", $"输入定义无法解析：{operation.inputId}");
                return;
            }

            string displayName = Required(operation.displayName, operation.inputId, nodeType);
            if (!TryResolveOptionalTargetNode(graph, operation, out BaseNode existing, report, path))
                return;
            if (existing != null)
            {
                AddApplied(report, operation, graph, existing, "exists");
                return;
            }

            if (!m_Emitters.TryCreateNode(graph, nodeType, displayName, operation.position, out BaseNode node, report, path))
                return;

            m_Emitters.ConfigureInputNode(node, operation.inputId, operation.inputValueType, report, path);
            AddApplied(report, operation, graph, node, "input");
        }

        void ApplyLinkFlow(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveGraph(operation.graphAuthoringId, operation.graphOperationId, out BaseTree graph, report, path))
                return;

            if (!ResolveNode(graph, operation.sourceElementAuthoringId, operation.sourceOperationId, out BaseNode source, report, path) ||
                !ResolveNode(graph, operation.targetElementAuthoringId, operation.targetOperationId, out BaseNode target, report, path))
            {
                report.Error(path, "flow_node_not_found", $"flow link 节点无法解析：{operation.sourceNode} -> {operation.targetNode}");
                return;
            }

            string startPort = Required(operation.startPort, "Output", string.Empty);
            string endPort = Required(operation.endPort, "Input", string.Empty);
            BaseEdge edge = FindFlowEdge(graph, source, target, startPort, endPort) ?? graph.Link(source, target, startPort, endPort);
            if (edge == null)
            {
                report.Error(path, "flow_not_created", $"flow link 未创建：{operation.sourceNode} -> {operation.targetNode}");
                return;
            }

            AddApplied(report, operation, graph, edge, "flow");
        }

        void ApplyLinkProperty(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            if (!ResolveGraph(operation.graphAuthoringId, operation.graphOperationId, out BaseTree graph, report, path))
                return;

            if (!ResolveNode(graph, operation.sourceElementAuthoringId, operation.sourceOperationId, out BaseNode source, report, path) ||
                !ResolveNode(graph, operation.targetElementAuthoringId, operation.targetOperationId, out BaseNode target, report, path))
            {
                report.Error(path, "property_node_not_found", $"property link 节点无法解析：{operation.sourceNode} -> {operation.targetNode}");
                return;
            }

            if (!source.PropertyPortMap.TryGetValue(operation.startPropertyPort, out PropertyPort startPort) ||
                !target.PropertyPortMap.TryGetValue(operation.endPropertyPort, out PropertyPort endPort))
            {
                report.Error(path, "property_port_not_found", $"property port 无法解析：{operation.startPropertyPort} -> {operation.endPropertyPort}");
                return;
            }

            PropertyEdge edge = graph.LinkProperty(source, target, startPort, endPort);
            if (edge == null)
                report.Info(path, "property_already_linked", "property link 已存在。");
            else
                AddApplied(report, operation, graph, edge, "property");
        }

        T FindOrCreateStateBehaviorNode<T>(StateBehaviorSubTree graph, AgentPatchOperation operation, string nodeType, string displayName, Vector2 position, AgentCompileReport report, string path)
            where T : BaseNode
        {
            if (!TryResolveOptionalTargetNode(graph, operation, out BaseNode existing, report, path))
                return null;
            if (existing != null)
            {
                if (existing is T typed)
                    return typed;
                report.Error(path, "target_element_type_mismatch", $"targetElementAuthoringId 不是 {typeof(T).Name}。");
                return null;
            }

            if (!m_Emitters.TryCreateNode(graph, nodeType, displayName, position, out BaseNode node, report, path))
                return null;

            return node as T;
        }

        bool ResolveTransitionEndpoints(AgentPatchOperation operation, AgentCompileReport report, string path, out StateMachineGraph graph, out BaseNode from, out BaseNode to)
        {
            graph = null;
            from = null;
            to = null;

            if (!ResolveStateMachineGraph(operation, out graph, report, path))
                return false;

            if (!ResolveNode(graph, operation.fromElementAuthoringId, operation.fromOperationId, out from, report, path))
            {
                report.Error(path, "transition_start_not_found", "Transition 起点 identity 无法解析。");
                return false;
            }

            if (!ResolveNode(graph, operation.toElementAuthoringId, operation.toOperationId, out to, report, path))
            {
                report.Error(path, "transition_end_not_found", "Transition 终点 identity 无法解析。");
                return false;
            }

            return true;
        }

        bool ResolveStateBehaviorGraph(AgentPatchOperation operation, AgentCompileReport report, string path, out StateBehaviorSubTree graph)
        {
            graph = null;
            if (!string.IsNullOrEmpty(operation.targetGraphAuthoringId) || !string.IsNullOrEmpty(operation.targetGraphOperationId))
            {
                if (!ResolveGraph(operation.targetGraphAuthoringId, operation.targetGraphOperationId, out BaseTree direct, report, path))
                    return false;

                graph = direct as StateBehaviorSubTree;
                if (graph != null)
                    return true;

                report.Error(path, "target_graph_wrong_type", "target graph identity 不是 StateBehaviorSubTree。");
                return false;
            }

            if (!ResolveStateMachineGraph(operation, out StateMachineGraph stateMachineGraph, report, path))
                return false;

            if (!ResolveNode(stateMachineGraph, operation.stateAuthoringId, operation.stateOperationId, out BaseNode resolved, report, path) || resolved is not StateNode state)
            {
                report.Error(path, "state_not_found", "State identity 无法解析。");
                return false;
            }

            graph = m_Index.GetStateBehaviorTree(state);
            if (!graph)
            {
                report.Error(path, "state_behavior_missing", $"状态缺少 StateBehaviorSubTree：{operation.state}");
                return false;
            }

            return true;
        }

        bool ResolveStateMachineGraph(AgentPatchOperation operation, out StateMachineGraph graph, AgentCompileReport report, string path)
        {
            graph = null;
            if (!string.IsNullOrEmpty(operation.stateMachineGraphAuthoringId) && m_Index.TryFindStateMachineGraph(operation.stateMachineGraphAuthoringId, out graph))
                return true;
            if (!string.IsNullOrEmpty(operation.stateMachineOperationId) &&
                m_OperationGraphs.TryGetValue(operation.stateMachineOperationId, out BaseGraph operationGraph) &&
                operationGraph is StateMachineGraph generated)
            {
                graph = generated;
                return true;
            }

            report.Error(path, "state_machine_not_found", "StateMachineGraph authoring identity/operation reference 无法解析。");
            return false;
        }

        bool ResolveGraph(string authoringId, string operationId, out BaseTree graph, AgentCompileReport report, string path)
        {
            if (!string.IsNullOrEmpty(authoringId) && m_Index.TryGetGraph(authoringId, out graph))
                return true;
            if (!string.IsNullOrEmpty(operationId) && m_OperationGraphs.TryGetValue(operationId, out BaseGraph operationGraph) && operationGraph is BaseTree generated)
            {
                graph = generated;
                return true;
            }

            graph = null;
            report.Error(path, "graph_not_found", "Graph authoring identity/operation reference 无法解析。");
            return false;
        }

        bool ResolveNode(BaseGraph graph, string authoringId, string operationId, out BaseNode node, AgentCompileReport report, string path)
        {
            node = null;
            if (!string.IsNullOrEmpty(authoringId))
                m_Index.TryFindNode(graph, authoringId, out node);
            else if (!string.IsNullOrEmpty(operationId))
                ResolveOperationNode(operationId, out node);

            if (node != null && graph.Nodes.Contains(node))
                return true;

            node = null;
            report?.Error(path, "element_identity_not_found", "Element authoring identity/operation reference 无法在目标 Graph 中解析。");
            return false;
        }

        bool ResolveOperationNode(string operationId, out BaseNode node)
        {
            node = null;
            if (string.IsNullOrEmpty(operationId))
                return false;
            int separator = operationId.LastIndexOf('#');
            if (separator < 0)
                return m_OperationNodes.TryGetValue(operationId, out node) && node != null;

            string graphOperationId = operationId.Substring(0, separator);
            string role = operationId.Substring(separator + 1);
            if (!m_OperationGraphs.TryGetValue(graphOperationId, out BaseGraph graph) || graph is not StateMachineGraph stateMachine)
                return false;
            node = role switch
            {
                "StateMachineEnterNode" => stateMachine.EnterNode,
                "StateMachineAnyStateNode" => stateMachine.AnyStateNode,
                "StateMachineExitNode" => stateMachine.ExitNode,
                _ => null
            };
            return node != null;
        }

        bool TryResolveOptionalTargetNode(BaseGraph graph, AgentPatchOperation operation, out BaseNode node, AgentCompileReport report, string path)
        {
            node = null;
            if (string.IsNullOrEmpty(operation.targetElementAuthoringId) && string.IsNullOrEmpty(operation.targetOperationId))
                return true;
            return ResolveNode(graph, operation.targetElementAuthoringId, operation.targetOperationId, out node, report, path);
        }

        bool RebuildIndex(AgentCompileReport report, string path)
        {
            try
            {
                m_Index.Rebuild(m_RootTree);
                return true;
            }
            catch (Exception exception)
            {
                report.Error(path, "authoring_identity_index_invalid", exception.Message);
                return false;
            }
        }

        bool TryLinkLifecycleSlot(StateBehaviorSubTree graph, string lifecycleSlot, BaseNode child, AgentCompileReport report, string path)
        {
            if (!child)
                return false;

            BaseNode anchor = ResolveLifecycleAnchor(graph, lifecycleSlot);
            if (!anchor)
            {
                report.Error(path, "lifecycle_anchor_not_found", $"生命周期入口无法解析：{lifecycleSlot}");
                return false;
            }

            BaseEdge existing = FindAnyOutputEdge(graph, anchor, "Output");
            if (existing != null)
            {
                if (existing.EndNode == child)
                    return true;

                report.Warning(path, "lifecycle_slot_occupied", $"{lifecycleSlot} 已连接到 {existing.EndNode?.ResolvedDisplayName}，未覆盖作者已有结构。");
                return false;
            }

            BaseEdge edge = graph.Link(anchor, child, "Output", "Input");
            if (edge == null)
            {
                report.Error(path, "lifecycle_link_failed", $"无法连接 {lifecycleSlot} -> {child.ResolvedDisplayName}");
                return false;
            }
            return true;
        }

        BaseNode ResolveLifecycleAnchor(StateBehaviorSubTree graph, string lifecycleSlot)
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

        void ValidateReferences(AgentPatchOperation operation, AgentCompileReport report, string path)
        {
            switch (operation.op)
            {
                case "ensure_action_activation":
                    if (!m_Resolver.TryResolveActionProfile(operation.actionProfile, out _))
                    {
                        report.metrics.assetResolveFailureCount++;
                        report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{operation.actionProfile}");
                    }
                    string sourceRequestId = Required(operation.sourceInputRequestId, operation.inputId, operation.request);
                    if (!string.IsNullOrEmpty(sourceRequestId) && !m_Resolver.TryResolveActionRequest(sourceRequestId, out _))
                        report.Error(path, "request_not_found", $"Action activation source request 未在当前 InputProfile 中找到：{sourceRequestId}");
                    break;
                case "ensure_timeline_node":
                    if (!Enum.TryParse(operation.timelineOwnership, true, out AgentTimelineOwnership timelineOwnership))
                    {
                        report.Error(path, "timeline_ownership_invalid", $"Timeline ownership 无效：{operation.timelineOwnership}", "使用 Inline 或 Shared。");
                        break;
                    }
                    bool requiresTimelineAsset = timelineOwnership == AgentTimelineOwnership.Shared ||
                                                 !string.IsNullOrEmpty(operation.timelineAssetPath) ||
                                                 !string.IsNullOrEmpty(operation.timelineAssetGuid);
                    if (requiresTimelineAsset && !m_Resolver.TryResolveTimelineAsset(operation, out _))
                    {
                        report.metrics.assetResolveFailureCount++;
                        report.Error(path, "timeline_asset_not_found", $"TimelineAsset 无法从 snapshot 或显式路径解析：{operation.timeline}");
                    }
                    break;
                case "ensure_input_node":
                    if (!m_Resolver.TryResolveActionRequest(operation.inputId, out _) &&
                        !m_Resolver.TryResolveInputValue(operation.inputId, out _))
                    {
                        report.Error(path, "input_not_found", $"输入定义无法解析：{operation.inputId}");
                    }
                    break;
                case "ensure_condition_rule":
                    for (int i = 0; i < operation.conditionGroups.Count; i++)
                        ValidateConditionTermReferences(operation.conditionGroups[i].terms, operation, report, $"{path}.conditionGroups[{i}].terms");
                    break;
                case "ensure_action_exit_lifecycle":
                    if (!m_Resolver.TryResolveActionContext(operation, out ActionContextSlot actionContext) || !actionContext)
                        report.Error(path, "action_context_missing", "Action exit lifecycle 缺少 Action Context。");
                    ValidateConditionTermReferences(operation.cancelGuards, operation, report, $"{path}.cancelGuards");
                    break;
            }
        }

        void ValidateConditionTermReferences(
            List<AgentConditionTerm> terms,
            AgentPatchOperation operation,
            AgentCompileReport report,
            string path)
        {
            if (terms == null)
                return;

            for (int i = 0; i < terms.Count; i++)
            {
                AgentConditionTerm term = terms[i];
                if (term == null)
                    continue;

                string termPath = $"{path}[{i}]";
                if (string.Equals(term.kind, "action_request", StringComparison.OrdinalIgnoreCase))
                {
                    string requestId = Required(term.request, operation.request, operation.inputId);
                    if (string.IsNullOrEmpty(requestId) || !m_Resolver.TryResolveActionRequest(requestId, out _))
                        report.Error(termPath, "request_not_found", $"Action request 未在当前 InputProfile 中找到：{requestId}");
                }

            }
        }

        void ComposeConditionRule(
            BaseEdge targetEdge,
            AgentPatchOperation operation,
            AgentCompileReport report,
            string path)
        {
            ConditionRuleGraph target = targetEdge.ConditionRuleGraph;
            if (!target)
            {
                report.Error(path, "condition_rule_graph_missing", "目标 ConditionRuleGraph 缺失。");
                return;
            }

            ClearConditionRule(target);
            var groupOutputs = new List<RuleTermOutput>();
            int layoutIndex = 0;
            for (int groupIndex = 0; groupIndex < operation.conditionGroups.Count; groupIndex++)
            {
                AgentConditionGroup group = operation.conditionGroups[groupIndex];
                var termOutputs = new List<RuleTermOutput>();
                for (int termIndex = 0; termIndex < group.terms.Count; termIndex++)
                {
                    RuleTermOutput output = CreateConditionTerm(
                        target,
                        group.terms[termIndex],
                        operation,
                        layoutIndex++,
                        report,
                        $"{path}.conditionGroups[{groupIndex}].terms[{termIndex}]");
                    if (output.IsValid)
                        termOutputs.Add(output);
                }

                RuleTermOutput groupOutput = CombineOutputs(
                    target,
                    termOutputs,
                    true,
                    $"Group {groupIndex + 1} And",
                    new Vector2(40f, groupIndex * 180f),
                    report,
                    path);
                if (groupOutput.IsValid)
                    groupOutputs.Add(groupOutput);
            }

            if (groupOutputs.Count == 0)
            {
                report.Error(path, "condition_groups_no_output", "结构化 ConditionRule 没有产生有效条件组输出。");
                return;
            }

            RuleTermOutput combined = CombineOutputs(
                target,
                groupOutputs,
                false,
                "Condition Groups Or",
                new Vector2(260f, 40f),
                report,
                path);

            ConditionRuleResultNode resultNode = target.ResultNode;
            if (!combined.IsValid || !resultNode || !resultNode.PropertyPortMap.TryGetValue("m_Result", out PropertyPort resultPort))
            {
                report.Error(path, "condition_rule_result_missing", "组合 ConditionRule 缺少 Rule Result。");
                return;
            }

            target.LinkProperty(combined.Node, resultNode, combined.Port, resultPort);
        }

        static RuleTermOutput CombineOutputs(
            ConditionRuleGraph graph,
            IReadOnlyList<RuleTermOutput> outputs,
            bool useAnd,
            string displayName,
            Vector2 position,
            AgentCompileReport report,
            string path)
        {
            if (outputs == null || outputs.Count == 0)
                return default;
            if (outputs.Count == 1)
                return outputs[0];

            RuleTermOutput combined = outputs[0];
            for (int i = 1; i < outputs.Count; i++)
            {
                BaseNode operationNode = graph.CreateNode(useAnd ? typeof(AndNode) : typeof(OrNode));
                operationNode.DisplayName = displayName;
                operationNode.Position = position + new Vector2((i - 1) * 180f, i * 35f);
                LinkProperty(graph, combined, operationNode, "m_Input1", report, path);
                LinkProperty(graph, outputs[i], operationNode, "m_Input2", report, path);
                combined = Output(operationNode, "m_Output");
            }

            return combined;
        }

        RuleTermOutput CreateConditionTerm(
            ConditionRuleGraph target,
            AgentConditionTerm term,
            AgentPatchOperation operation,
            int index,
            AgentCompileReport report,
            string path)
        {
            if (term == null)
                return default;

            if (string.Equals(term.kind, "move_stop", StringComparison.OrdinalIgnoreCase))
                return CreateMovementCompare(target, index, "StopThreshold", CompareNode.CompareType.Less, report, path);

            if (string.Equals(term.kind, "move_has", StringComparison.OrdinalIgnoreCase))
                return CreateMovementCompare(target, index, "StopThreshold", CompareNode.CompareType.Greater, report, path);

            if (string.Equals(term.kind, "move_run", StringComparison.OrdinalIgnoreCase))
                return CreateMovementCompare(target, index, "RunThreshold", CompareNode.CompareType.GreaterEqual, report, path);

            if (string.Equals(term.kind, "turn_facing_angle", StringComparison.OrdinalIgnoreCase))
                return CreateFacingAngleCompare(target, index, report, path);

            if (string.Equals(term.kind, "blackboard_bool", StringComparison.OrdinalIgnoreCase))
                return CreateBlackboardBool(target, term, index, report, path);

            if (string.Equals(term.kind, "move_walk", StringComparison.OrdinalIgnoreCase))
            {
                RuleTermOutput lower = CreateMovementCompare(target, index * 2, "WalkThreshold", CompareNode.CompareType.GreaterEqual, report, path);
                RuleTermOutput upper = CreateMovementCompare(target, index * 2 + 1, "RunThreshold", CompareNode.CompareType.Less, report, path);
                AndNode walk = target.CreateNode(typeof(AndNode)) as AndNode;
                walk.DisplayName = "Walk Range";
                walk.Position = new Vector2(-40f, index * 120f);
                LinkProperty(target, lower, walk, "m_Input1", report, path);
                LinkProperty(target, upper, walk, "m_Input2", report, path);
                return Output(walk, "m_Output");
            }

            if (string.Equals(term.kind, "state_root_completed", StringComparison.OrdinalIgnoreCase))
            {
                StateRootCompletedNode node = target.CreateNode(typeof(StateRootCompletedNode)) as StateRootCompletedNode;
                node.DisplayName = "State Root Completed";
                node.Position = new Vector2(-360f, index * 100f);
                return Output(node, "m_Output");
            }

            if (string.Equals(term.kind, "action_request", StringComparison.OrdinalIgnoreCase))
            {
                string requestId = Required(term.request, operation.request, operation.inputId);
                if (!m_Resolver.TryResolveActionRequest(requestId, out _))
                {
                    report.Error(path, "request_not_found", $"Action request 未找到：{requestId}");
                    return default;
                }

                CharacterActionRequestInfoNode node = target.CreateNode(typeof(CharacterActionRequestInfoNode)) as CharacterActionRequestInfoNode;
                node.DisplayName = $"Has {requestId} Request";
                node.Position = new Vector2(-360f, index * 100f);
                node.BindActionRequest(requestId);
                return Output(node, "m_Output");
            }

            report.Error(path, "condition_term_unsupported", $"不支持的 Condition term：{term.kind}");
            return default;
        }

        RuleTermOutput CreateMovementCompare(
            ConditionRuleGraph graph,
            int index,
            string blackboardKey,
            CompareNode.CompareType compareType,
            AgentCompileReport report,
            string path)
        {
            CharacterInputValueInfoNode inputNode = graph.CreateNode(typeof(CharacterInputVector2MagnitudeInfoNode)) as CharacterInputValueInfoNode;
            inputNode.DisplayName = "MoveAxis Magnitude";
            inputNode.Position = new Vector2(-520f, index * 100f);
            inputNode.BindInputValue("MoveAxis");

            PipelineBlackboardFloatInfoNode thresholdNode = graph.CreateNode(typeof(PipelineBlackboardFloatInfoNode)) as PipelineBlackboardFloatInfoNode;
            thresholdNode.DisplayName = blackboardKey;
            thresholdNode.Position = new Vector2(-520f, index * 100f + 45f);
            if (!TryResolveBlackboardDeclaration(blackboardKey, typeof(float), report, path, out PipelineBlackboardVariableReference thresholdReference))
                return default;
            thresholdNode.ConfigureAuthoring(ResolveDeclaration(thresholdReference));

            CompareNode compareNode = graph.CreateNode(typeof(CompareNode)) as CompareNode;
            compareNode.DisplayName = "Compare";
            compareNode.Position = new Vector2(-240f, index * 100f + 20f);
            compareNode.ConfigureAuthoring(compareType);

            graph.LinkProperty(inputNode, compareNode, inputNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(thresholdNode, compareNode, thresholdNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue2"]);
            return Output(compareNode, "m_Result");
        }

        RuleTermOutput CreateFacingAngleCompare(ConditionRuleGraph graph, int index, AgentCompileReport report, string path)
        {
            CharacterInputVector2InfoNode inputNode = graph.CreateNode(typeof(CharacterInputVector2InfoNode)) as CharacterInputVector2InfoNode;
            inputNode.DisplayName = "MoveAxis Input Value";
            inputNode.Position = new Vector2(-620f, index * 120f);
            inputNode.BindInputValue("MoveAxis");

            CharacterMoveFacingAngleInfoNode angleNode = graph.CreateNode(typeof(CharacterMoveFacingAngleInfoNode)) as CharacterMoveFacingAngleInfoNode;
            angleNode.DisplayName = "Move Facing Angle";
            angleNode.Position = new Vector2(-400f, index * 120f);

            PipelineBlackboardFloatInfoNode thresholdNode = graph.CreateNode(typeof(PipelineBlackboardFloatInfoNode)) as PipelineBlackboardFloatInfoNode;
            thresholdNode.DisplayName = "MovingTurnAngleThreshold";
            thresholdNode.Position = new Vector2(-400f, index * 120f + 60f);
            if (!TryResolveBlackboardDeclaration("MovingTurnAngleThreshold", typeof(float), report, path, out PipelineBlackboardVariableReference thresholdReference))
                return default;
            thresholdNode.ConfigureAuthoring(ResolveDeclaration(thresholdReference));

            CompareNode compareNode = graph.CreateNode(typeof(CompareNode)) as CompareNode;
            compareNode.DisplayName = "Facing Angle Threshold";
            compareNode.Position = new Vector2(-140f, index * 120f + 25f);
            compareNode.ConfigureAuthoring(CompareNode.CompareType.GreaterEqual);

            graph.LinkProperty(inputNode, angleNode, inputNode.PropertyPortMap["m_Output"], angleNode.PropertyPortMap["m_MoveInput"]);
            graph.LinkProperty(angleNode, compareNode, angleNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(thresholdNode, compareNode, thresholdNode.PropertyPortMap["m_Output"], compareNode.PropertyPortMap["m_InputValue2"]);
            return Output(compareNode, "m_Result");
        }

        RuleTermOutput CreateBlackboardBool(
            ConditionRuleGraph graph,
            AgentConditionTerm term,
            int index,
            AgentCompileReport report,
            string path)
        {
            if (string.IsNullOrWhiteSpace(term.blackboardKey))
            {
                report.Error(path, "blackboard_key_missing", "blackboard_bool condition 缺少 blackboardKey。");
                return default;
            }

            PipelineBlackboardBoolInfoNode valueNode = graph.CreateNode(typeof(PipelineBlackboardBoolInfoNode)) as PipelineBlackboardBoolInfoNode;
            valueNode.DisplayName = term.blackboardKey;
            valueNode.Position = new Vector2(-360f, index * 100f);
            if (!TryResolveBlackboardDeclaration(term.blackboardKey, typeof(bool), report, path, out PipelineBlackboardVariableReference reference))
                return default;
            valueNode.ConfigureAuthoring(ResolveDeclaration(reference));
            RuleTermOutput output = Output(valueNode, "m_Output");
            if (!term.negate)
                return output;

            NotNode notNode = graph.CreateNode(typeof(NotNode)) as NotNode;
            notNode.DisplayName = $"Not {term.blackboardKey}";
            notNode.Position = new Vector2(-120f, index * 100f);
            LinkProperty(graph, output, notNode, "m_Input", report, path);
            return Output(notNode, "m_Output");
        }

        bool TryResolveBlackboardDeclaration(
            string key,
            Type expectedType,
            AgentCompileReport report,
            string path,
            out PipelineBlackboardVariableReference reference)
        {
            reference = default;
            List<BaseExposedProperty> matches = m_RootTree.ExposedProperties
                .Where(i => string.Equals(i.BlackboardKey, key, StringComparison.Ordinal))
                .ToList();
            if (matches.Count != 1)
            {
                report.Error(path, matches.Count == 0 ? "blackboard_declaration_missing" : "blackboard_declaration_ambiguous", $"Pipeline Blackboard declaration 必须唯一：{key}");
                return false;
            }

            BaseExposedProperty declaration = matches[0];
            if (declaration.ValueType != expectedType)
            {
                report.Error(path, "blackboard_type_mismatch", $"Pipeline Blackboard declaration {key} 需要 {expectedType.Name}，当前为 {declaration.ValueType?.Name ?? "Unknown"}。");
                return false;
            }

            reference = declaration.CreateBlackboardReference();
            return true;
        }

        BaseExposedProperty ResolveDeclaration(PipelineBlackboardVariableReference reference)
        {
            return m_RootTree.ExposedProperties.Single(i =>
                string.Equals(i.DeclarationId, reference.DeclarationId, StringComparison.Ordinal) &&
                string.Equals(i.DeclarationOwnerId, reference.DeclarationOwnerId, StringComparison.Ordinal));
        }

        static void ClearConditionRule(ConditionRuleGraph graph)
        {
            foreach (PropertyEdge edge in graph.PropertyEdges.ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseNode node in graph.Nodes.Where(i => i is not ConditionRuleResultNode).ToList())
                graph.DeleteNode(node);
        }

        ConditionRuleGraph CreateActionExitRule(
            string graphName,
            ActionContextSlot actionContext,
            ActionExitRuleKind ruleKind,
            AgentPatchOperation operation,
            AgentCompileReport report,
            string path)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(graphName);
            var outputs = new List<RuleTermOutput>();

            ActionContextActiveInfoNode activeNode = graph.CreateNode(typeof(ActionContextActiveInfoNode)) as ActionContextActiveInfoNode;
            activeNode.DisplayName = "Action Context Active";
            activeNode.Position = new Vector2(-520f, -120f);
            activeNode.ConfigureAuthoring(actionContext);
            outputs.Add(Output(activeNode, "m_Output"));

            StateExitCauseInfoNode causeNode = graph.CreateNode(typeof(StateExitCauseInfoNode)) as StateExitCauseInfoNode;
            causeNode.DisplayName = "State Transition Exit";
            causeNode.Position = new Vector2(-520f, -20f);
            causeNode.ConfigureAuthoring(StateExitCause.StateTransition);
            RuleTermOutput causeOutput = Output(causeNode, "m_Output");

            if (ruleKind == ActionExitRuleKind.Abort)
            {
                NotNode notNode = graph.CreateNode(typeof(NotNode)) as NotNode;
                notNode.DisplayName = "Not State Transition";
                notNode.Position = new Vector2(-280f, -20f);
                LinkProperty(graph, causeOutput, notNode, "m_Input", report, path);
                outputs.Add(Output(notNode, "m_Output"));
            }
            else
            {
                outputs.Add(causeOutput);
            }

            if (ruleKind == ActionExitRuleKind.Cancel)
            {
                int cancelGuardCount = operation.cancelGuards != null ? operation.cancelGuards.Count : 0;
                for (int i = 0; i < cancelGuardCount; i++)
                {
                    RuleTermOutput guard = CreateConditionTerm(
                        graph,
                        operation.cancelGuards[i],
                        operation,
                        i + 2,
                        report,
                        $"{path}.cancelGuards[{i}]");
                    if (guard.IsValid)
                        outputs.Add(guard);
                }
            }

            RuleTermOutput combined = CombineOutputs(
                graph,
                outputs,
                true,
                "Action Exit And",
                new Vector2(-40f, 20f),
                report,
                path);
            if (!combined.IsValid || !graph.ResultNode || !graph.ResultNode.PropertyPortMap.TryGetValue("m_Result", out PropertyPort resultPort))
            {
                report.Error(path, "action_exit_rule_invalid", $"Action exit {ruleKind} 条件图无法生成最终输出。");
                return graph;
            }

            graph.LinkProperty(combined.Node, graph.ResultNode, combined.Port, resultPort);
            return graph;
        }

        static void LinkProperty(
            ConditionRuleGraph graph,
            RuleTermOutput output,
            BaseNode target,
            string targetPortId,
            AgentCompileReport report,
            string path)
        {
            if (!output.IsValid || target == null || !target.PropertyPortMap.TryGetValue(targetPortId, out PropertyPort targetPort))
            {
                report?.Error(path, "condition_rule_port_missing", $"ConditionRule property port 无法解析：{targetPortId}");
                return;
            }

            graph.LinkProperty(output.Node, target, output.Port, targetPort);
        }

        static RuleTermOutput Output(BaseNode node, string portId)
        {
            return node != null && node.PropertyPortMap.TryGetValue(portId, out PropertyPort port)
                ? new RuleTermOutput(node, port)
                : default;
        }

        static void RemoveActionExitNodes(BaseGraph graph)
        {
            HashSet<BaseNode> nodes = graph.Nodes.Where(node =>
                node is SubmitActionLifecycleTransitionNode ||
                node is SucceedNode && string.Equals(node.ResolvedDisplayName, "Succeed", StringComparison.Ordinal) ||
                node is SelectorNode && string.Equals(node.ResolvedDisplayName, "Action Exit", StringComparison.Ordinal)).ToHashSet();

            foreach (PropertyEdge propertyEdge in graph.PropertyEdges.Where(edge => nodes.Contains(edge.StartNode) || nodes.Contains(edge.EndNode)).ToList())
                graph.UnLinkProperty(propertyEdge);
            foreach (BaseEdge edge in graph.Edges.Where(edge => nodes.Contains(edge.StartNode) || nodes.Contains(edge.EndNode)).ToList())
                graph.UnLink(edge);
            foreach (BaseNode node in nodes)
            {
                graph.DeleteNode(node);
            }

            RemoveOrphanLinks(graph);
        }

        static void RemoveOrphanLinks(BaseGraph graph)
        {
            HashSet<string> nodeGuids = graph.Nodes.Where(node => node != null).Select(node => node.GUID).ToHashSet();
            graph.PropertyEdges.RemoveAll(edge => edge == null);
            graph.Edges.RemoveAll(edge => edge == null);
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) ||
                         !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(edge =>
                         !nodeGuids.Contains(edge.StartNodeGUID) ||
                         !nodeGuids.Contains(edge.EndNodeGUID)).ToList())
                graph.UnLink(edge);
        }

        readonly struct RuleTermOutput
        {
            public RuleTermOutput(BaseNode node, PropertyPort port)
            {
                Node = node;
                Port = port;
            }

            public BaseNode Node { get; }
            public PropertyPort Port { get; }
            public bool IsValid => Node != null && Port != null;
        }

        enum ActionExitRuleKind
        {
            Cancel,
            Abort,
            Complete
        }

        static BaseEdge EnsureSingleTransition(StateMachineGraph graph, BaseNode source, BaseNode target)
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

        static BaseEdge FindFlowEdge(BaseGraph graph, BaseNode source, BaseNode target, string startPort, string endPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null &&
                    edge.StartNode == source &&
                    edge.EndNode == target &&
                    edge.StartPortName == startPort &&
                    edge.EndPortName == endPort)
                    return edge;
            }
            return null;
        }

        static BaseEdge FindAnyOutputEdge(BaseGraph graph, BaseNode source, string startPort)
        {
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge != null && edge.StartNode == source && edge.StartPortName == startPort)
                    return edge;
            }
            return null;
        }

        void AddApplied(AgentCompileReport report, AgentPatchOperation operation, BaseGraph graph, BaseNode node, string detail)
        {
            MarkGraphOwnerDirty(graph);
            if (node != null && !string.IsNullOrEmpty(operation.id) && !string.Equals(operation.op, "delete_state_behavior_node", StringComparison.Ordinal))
            {
                m_OperationNodes[operation.id] = node;
                if (node is StateMachineNode stateMachineNode && stateMachineNode.Graph != null)
                    m_OperationGraphs[operation.id] = stateMachineNode.Graph;
                else if (node is StateNode stateNode && stateNode.SubTree != null)
                    m_OperationGraphs[operation.id] = stateNode.SubTree;
            }
            report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                operationId = OperationId(operation, operation.op),
                action = operation.op,
                graph = m_Index.GetGraphPath(graph),
                target = node != null ? node.ResolvedDisplayName : string.Empty,
                detail = detail
            });
        }

        void AddApplied(AgentCompileReport report, AgentPatchOperation operation, BaseGraph graph, BaseEdge edge, string detail)
        {
            MarkGraphOwnerDirty(graph);
            if (edge != null && !string.IsNullOrEmpty(operation.id))
                m_OperationEdges[operation.id] = edge;
            report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                operationId = OperationId(operation, operation.op),
                action = operation.op,
                graph = m_Index.GetGraphPath(graph),
                target = edge != null ? $"{edge.StartNode?.ResolvedDisplayName}->{edge.EndNode?.ResolvedDisplayName}" : string.Empty,
                detail = detail
            });
        }

        void MarkDirty()
        {
            if (m_Definition && m_Definition.RootTreeAsset)
                EditorUtility.SetDirty(m_Definition.RootTreeAsset);
            if (m_Definition)
                EditorUtility.SetDirty(m_Definition);

            foreach (UnityEngine.Object owner in m_DirtyOwners)
            {
                if (owner != null)
                    EditorUtility.SetDirty(owner);
            }
        }

        void MarkGraphOwnerDirty(BaseGraph graph)
        {
            if (graph?.SerializedOwner != null)
                m_DirtyOwners.Add(graph.SerializedOwner);
        }

        static string Required(string primary, string secondary, string fallback)
        {
            if (!string.IsNullOrEmpty(primary))
                return primary;
            if (!string.IsNullOrEmpty(secondary))
                return secondary;
            return fallback ?? string.Empty;
        }

        static string OperationId(AgentPatchOperation operation, string fallback)
        {
            return !string.IsNullOrEmpty(operation?.id) ? operation.id : fallback;
        }

        static string PrimaryTarget(AgentPatchOperation operation)
        {
            return Required(
                operation.state,
                operation.displayName,
                Required(
                    operation.to,
                    operation.targetNode,
                    operation.actionProfile));
        }
    }
}
