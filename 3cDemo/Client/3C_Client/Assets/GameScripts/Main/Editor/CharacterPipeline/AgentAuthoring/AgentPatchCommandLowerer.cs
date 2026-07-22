using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPatchCommandLowerer
    {
        public bool TryLower(AgentPatchIR patch, AgentCompileReport report, out AgentPatchCommandPlan plan)
        {
            plan = null;
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (patch == null)
            {
                report.Error("patch", "missing_patch", "AgentPatchIR 缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (!string.Equals(patch.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report.Error(
                    "patch.schemaVersion",
                    "unsupported_schema_version",
                    $"Patch schema 必须是 {AgentAuthoringSchema.Version}，当前为 {patch.schemaVersion}。",
                    "重新导出 v16 Snapshot 并生成新的 v16 Patch。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            report.domain = patch.domain ?? string.Empty;
            report.rootIdentity = patch.rootIdentity ?? string.Empty;
            if (!AgentAuthoringSchema.IsDomain(patch.domain))
            {
                report.Error("patch.domain", "unsupported_domain", $"Patch domain 无效：{patch.domain}");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (string.IsNullOrWhiteSpace(patch.rootIdentity) || string.IsNullOrWhiteSpace(patch.sourceRevision))
            {
                report.Error("patch", "patch_source_identity_missing", "v16 Patch 必须显式提供 rootIdentity 和 sourceRevision。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (patch.operations == null || patch.operations.Count == 0)
            {
                report.Error("patch.operations", "empty_patch", "Patch IR 没有任何操作。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            var commands = new List<AgentPatchCommand>(patch.operations.Count);
            var symbols = new Dictionary<string, AgentPlannedOutputSymbol>(StringComparer.Ordinal);
            var operationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < patch.operations.Count; i++)
            {
                AgentPatchOperation operation = patch.operations[i];
                string path = $"patch.operations[{i}]";
                if (operation == null)
                {
                    report.Error(path, "missing_operation", "Patch operation 为空。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(operation.id))
                {
                    report.Error(path, "operation_id_missing", "schema v16 要求每个 operation 使用唯一 id。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!operationIds.Add(operation.id))
                {
                    report.Error(path, "operation_id_duplicate", $"Operation id 重复：{operation.id}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(operation.op) || !AgentPatchOperationCatalog.TryGet(operation.op, out AgentPatchOperationDescriptor descriptor))
                {
                    report.Error(path, "unknown_operation", $"未知 Patch operation：{operation.op}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!descriptor.Allows(patch.domain))
                {
                    report.Error(path, "operation_domain_mismatch", $"Operation '{operation.op}' 不允许用于 {patch.domain} domain。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                var context = new AgentPatchLoweringContext(report, path, operation.id, symbols);
                AgentPatchCommand command = descriptor.Lower(context, operation);
                if (command == null || context.HasErrors)
                {
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                context.ValidateOwnedReferences(command);
                if (context.HasErrors)
                {
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                commands.Add(command);
                symbols.Add(operation.id, new AgentPlannedOutputSymbol(operation.id, descriptor.OutputKind, command.OwnerScope));
                report.metrics.schemaValidCount++;
            }

            if (report.HasErrors())
                return false;

            plan = new AgentPatchCommandPlan(commands, symbols, patch.domain, patch.rootIdentity, patch.sourceRevision, patch.sourceMacro, patch.sourceMacroVersion);
            return true;
        }
    }

    public static class AgentPatchOperationCatalog
    {
        static readonly Dictionary<string, AgentPatchOperationDescriptor> s_Descriptors =
            new Dictionary<string, AgentPatchOperationDescriptor>(StringComparer.Ordinal)
            {
                ["ensure_state_machine"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureStateMachine, AgentPatchOutputKind.StateMachine, LowerEnsureStateMachine),
                ["ensure_state"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureState, AgentPatchOutputKind.State, LowerEnsureState),
                ["delete_state"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteState, AgentPatchOutputKind.None, LowerDeleteState),
                ["ensure_transition"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureTransition, AgentPatchOutputKind.Transition, LowerEnsureTransition),
                ["ensure_condition_rule"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureConditionRule, AgentPatchOutputKind.Transition, LowerEnsureConditionRule),
                ["ensure_action_exit_lifecycle"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureActionExitLifecycle, AgentPatchOutputKind.Node, LowerEnsureActionExitLifecycle),
                ["delete_state_behavior_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteStateBehaviorNode, AgentPatchOutputKind.None, LowerDeleteStateBehaviorNode),
                ["ensure_state_behavior_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureStateBehaviorNode, AgentPatchOutputKind.Node, LowerEnsureStateBehaviorNode),
                ["ensure_timeline_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureTimelineNode, AgentPatchOutputKind.Node, LowerEnsureTimelineNode),
                ["ensure_action_activation"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureActionActivation, AgentPatchOutputKind.Node, LowerEnsureActionActivation),
                ["ensure_action_lifecycle_transition"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureActionLifecycleTransition, AgentPatchOutputKind.Node, LowerEnsureActionLifecycleTransition),
                ["ensure_input_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureInputNode, AgentPatchOutputKind.Node, LowerEnsureInputNode),
                ["ensure_blackboard_declaration"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureBlackboardDeclaration, AgentPatchOutputKind.BlackboardDeclaration, LowerEnsureBlackboardDeclaration),
                ["move_blackboard_declaration"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.MoveBlackboardDeclaration, AgentPatchOutputKind.BlackboardDeclaration, LowerMoveBlackboardDeclaration),
                ["delete_blackboard_declaration"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteBlackboardDeclaration, AgentPatchOutputKind.None, LowerDeleteBlackboardDeclaration),
                ["ensure_blackboard_write"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureBlackboardWrite, AgentPatchOutputKind.Node, LowerEnsureBlackboardWrite),
                ["ensure_timeline_tree_clip"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureTimelineTreeClip, AgentPatchOutputKind.TimelineClip, LowerEnsureTimelineTreeClip),
                ["ensure_motion_warp_track"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureMotionWarpTrack, AgentPatchOutputKind.TimelineTrack, LowerEnsureMotionWarpTrack),
                ["ensure_motion_warp_clip"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureMotionWarpClip, AgentPatchOutputKind.TimelineClip, LowerEnsureMotionWarpClip),
                ["configure_motion_warp_source"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureMotionWarpSource, AgentPatchOutputKind.None, LowerConfigureMotionWarpSource),
                ["configure_motion_warp_parameters"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureMotionWarpParameters, AgentPatchOutputKind.None, LowerConfigureMotionWarpParameters),
                ["move_timeline_clip"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.MoveTimelineClip, AgentPatchOutputKind.None, LowerMoveTimelineClip),
                ["configure_timeline_clip_ease"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureTimelineClipEase, AgentPatchOutputKind.None, LowerConfigureTimelineClipEase),
                ["configure_timeline_curve_channel"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureTimelineCurveChannel, AgentPatchOutputKind.None, LowerConfigureTimelineCurveChannel),
                ["configure_animation_track_channel"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureAnimationTrackChannel, AgentPatchOutputKind.None, LowerConfigureAnimationTrackChannel),
                ["configure_animation_track_marker_sync"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureAnimationTrackMarkerSync, AgentPatchOutputKind.None, LowerConfigureAnimationTrackMarkerSync),
                ["ensure_animation_sync_marker"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAnimationSyncMarker, AgentPatchOutputKind.TimelineMarker, LowerEnsureAnimationSyncMarker),
                ["move_animation_sync_marker"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.MoveAnimationSyncMarker, AgentPatchOutputKind.None, LowerMoveAnimationSyncMarker),
                ["delete_animation_sync_marker"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteAnimationSyncMarker, AgentPatchOutputKind.None, LowerDeleteAnimationSyncMarker),
                ["delete_timeline_clip"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteTimelineClip, AgentPatchOutputKind.None, LowerDeleteTimelineClip),
                ["ensure_tree_clip_blackboard_write"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureTreeClipBlackboardWrite, AgentPatchOutputKind.None, LowerEnsureTreeClipBlackboardWrite),
                ["delete_transition"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteTransition, AgentPatchOutputKind.None, LowerDeleteTransition),
                ["ensure_gameplay_tag"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureGameplayTag, AgentPatchOutputKind.None, LowerEnsureGameplayTag),
                ["set_action_profile_granted_tags"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.SetActionProfileGrantedTags, AgentPatchOutputKind.None, LowerSetActionProfileGrantedTags),
                ["set_action_profile_cancel_query"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.SetActionProfileCancelQuery, AgentPatchOutputKind.None, LowerSetActionProfileCancelQuery),
                ["set_action_profile_target_requirement"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.SetActionProfileTargetRequirement, AgentPatchOutputKind.None, LowerSetActionProfileTargetRequirement),
                ["set_action_request_timing_class"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.SetActionRequestTimingClass, AgentPatchOutputKind.None, LowerSetActionRequestTimingClass),
                ["ensure_ai_controller_definition"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIControllerDefinition, AgentPatchOutputKind.None, LowerEnsureAIControllerDefinition, AgentPatchDomainMask.AIController),
                ["ensure_ai_controller_tree"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIControllerTree, AgentPatchOutputKind.None, LowerEnsureAIControllerTree, AgentPatchDomainMask.AIController),
                ["bind_ai_controller_assets"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.BindAIControllerAssets, AgentPatchOutputKind.None, LowerBindAIControllerAssets, AgentPatchDomainMask.AIController),
                ["configure_ai_candidates"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.ConfigureAICandidates, AgentPatchOutputKind.None, LowerConfigureAICandidates, AgentPatchDomainMask.AIController),
                ["ensure_ai_blackboard_declaration"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIBlackboardDeclaration, AgentPatchOutputKind.BlackboardDeclaration, LowerEnsureAIBlackboardDeclaration, AgentPatchDomainMask.AIController),
                ["ensure_ai_shared_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAISharedNode, AgentPatchOutputKind.Node, LowerEnsureAISharedNode, AgentPatchDomainMask.AIController),
                ["ensure_ai_observation_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIObservationNode, AgentPatchOutputKind.Node, LowerEnsureAIObservationNode, AgentPatchDomainMask.AIController),
                ["ensure_ai_memory_node"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIMemoryNode, AgentPatchOutputKind.Node, LowerEnsureAIMemoryNode, AgentPatchDomainMask.AIController),
                ["ensure_ai_continuous_input"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIContinuousInput, AgentPatchOutputKind.Node, LowerEnsureAIContinuousInput, AgentPatchDomainMask.AIController),
                ["ensure_ai_action_target"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIActionTarget, AgentPatchOutputKind.Node, LowerEnsureAIActionTarget, AgentPatchDomainMask.AIController),
                ["ensure_ai_action_request"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureAIActionRequest, AgentPatchOutputKind.Node, LowerEnsureAIActionRequest, AgentPatchDomainMask.AIController),
                ["ensure_bt_condition_rule"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.EnsureBTConditionRule, AgentPatchOutputKind.FlowEdge, LowerEnsureBTConditionRule, AgentPatchDomainMask.AIController),
                ["delete_flow_edge"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.DeleteFlowEdge, AgentPatchOutputKind.None, LowerDeleteFlowEdge),
                ["link_flow"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.LinkFlow, AgentPatchOutputKind.FlowEdge, LowerLinkFlow, AgentPatchDomainMask.Both),
                ["link_property"] = new AgentPatchOperationDescriptor(AgentPatchCommandKind.LinkProperty, AgentPatchOutputKind.PropertyEdge, LowerLinkProperty, AgentPatchDomainMask.Both)
            };

        public static bool TryGet(string operationName, out AgentPatchOperationDescriptor descriptor)
        {
            descriptor = null;
            return !string.IsNullOrEmpty(operationName) && s_Descriptors.TryGetValue(operationName, out descriptor);
        }

        public static IReadOnlyCollection<string> OperationNames => s_Descriptors.Keys;

        static AgentPatchCommand LowerEnsureStateMachine(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference parent = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existingOwner = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string existingGraphId = context.OptionalAuthoringId(operation.stateMachineGraphAuthoringId, "stateMachineGraphAuthoringId");
            string displayName = context.RequiredText(operation.displayName, operation.stateMachine, "displayName", "ensure_state_machine 缺少 displayName/stateMachine。");
            return context.IsValid
                ? new AgentEnsureStateMachineCommand(operation.id, context.Path, parent, existingOwner, existingGraphId, displayName, operation.lifecycleSlot, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureState(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, "stateMachine", true);
            AgentStateTargetReference existingState = context.OptionalState(operation.stateAuthoringId, operation.stateOperationId, "state", true);
            string stateName = context.RequiredText(operation.state, operation.displayName, "state", "ensure_state 缺少 state/displayName。");
            return context.IsValid
                ? new AgentEnsureStateCommand(operation.id, context.Path, stateMachine, existingState, stateName, operation.position)
                : null;
        }

        static AgentPatchCommand LowerDeleteState(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, "stateMachine");
            AgentStateTargetReference state = context.RequiredState(operation.stateAuthoringId, operation.stateOperationId, "state");
            return context.IsValid
                ? new AgentDeleteStateCommand(operation.id, context.Path, stateMachine, state)
                : null;
        }

        static AgentPatchCommand LowerEnsureTransition(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "ensure_transition 缺少 stable edge identity。"), "targetElementAuthoringId");
            return context.IsValid
                ? new AgentEnsureTransitionCommand(operation.id, AgentPatchCommandKind.EnsureTransition, "ensure_transition", context.Path, stateMachine, from, to, edge, operation.transitionPriority, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureConditionRule(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "ensure_condition_rule 缺少 stable edge identity。"), "targetElementAuthoringId");
            List<AgentConditionGroupCommand> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureConditionRuleCommand(operation.id, context.Path, stateMachine, from, to, edge, operation.transitionPriority, groups, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureActionExitLifecycle(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference source = context.OptionalElement(operation.sourceElementAuthoringId, operation.sourceOperationId, "sourceElement");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            AgentAssetReference actionContext = ReadActionContext(operation);
            List<AgentConditionGroupCommand> cancelConditionGroups = context.RequiredConditionGroups(operation.cancelConditionGroups, operation, "cancelConditionGroups");
            string cancelReason = context.RequiredText(operation.reason, string.Empty, "reason", "ensure_action_exit_lifecycle 必须显式提供 cancel reason。");
            string interruptReason = context.RequiredText(operation.interruptReason, string.Empty, "interruptReason", "ensure_action_exit_lifecycle 必须显式提供 interrupt reason。");
            string abortReason = context.RequiredText(operation.abortReason, string.Empty, "abortReason", "ensure_action_exit_lifecycle 必须显式提供 abort reason。");
            string completeReason = context.RequiredText(operation.completeReason, string.Empty, "completeReason", "ensure_action_exit_lifecycle 必须显式提供 complete reason。");
            return context.IsValid
                ? new AgentEnsureActionExitLifecycleCommand(
                    operation.id,
                    context.Path,
                    target,
                    source,
                    existing,
                    actionContext,
                    cancelReason,
                    interruptReason,
                    abortReason,
                    completeReason,
                    cancelConditionGroups,
                    operation.position)
                : null;
        }

        static AgentPatchCommand LowerDeleteStateBehaviorNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement");
            return context.IsValid
                ? new AgentDeleteStateBehaviorNodeCommand(operation.id, context.Path, target, element)
                : null;
        }

        static AgentPatchCommand LowerEnsureStateBehaviorNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string nodeType = First(operation.nodeType, "SequenceNode");
            string displayName = First(operation.displayName, nodeType);
            return context.IsValid
                ? new AgentEnsureStateBehaviorNodeCommand(operation.id, context.Path, target, existing, nodeType, displayName, operation.lifecycleSlot, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureTimelineNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            if (!Enum.TryParse(operation.timelineOwnership, true, out AgentTimelineOwnership ownership) || !Enum.IsDefined(typeof(AgentTimelineOwnership), ownership))
            {
                context.Error("timelineOwnership", "timeline_ownership_invalid", $"Timeline ownership 无效：{operation.timelineOwnership}", "使用 Inline 或 Shared。");
                ownership = AgentTimelineOwnership.Inline;
            }
            string displayName = First(operation.displayName, First(operation.timeline, "Timeline"));
            var timelineAsset = new AgentAssetReference(operation.timeline, operation.timelineAssetPath, operation.timelineAssetGuid);
            var timelineTarget = new AgentTimelineTargetReference(operation.timelineAuthoringId, operation.trackAuthoringId, operation.clipAuthoringId);
            return context.IsValid
                ? new AgentEnsureTimelineNodeCommand(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    displayName,
                    First(operation.lifecycleSlot, "Root"),
                    ownership,
                    timelineAsset,
                    ReadActionContext(operation),
                    timelineTarget,
                    operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureActionActivation(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ensure_action_activation 缺少 ActionProfile 引用。");
            string sourceRequest = First(operation.sourceInputRequestId, First(operation.inputId, operation.request));
            return context.IsValid
                ? new AgentEnsureActionActivationCommand(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    First(operation.displayName, $"Activate {actionProfile}"),
                    First(operation.lifecycleSlot, "OnEnter"),
                    new AgentAssetReference(actionProfile, string.Empty, string.Empty),
                    ReadActionContext(operation),
                    sourceRequest,
                    operation.consumeSourceInputRequest,
                    operation.targetKey,
                    operation.targetSnapshotBlackboardKey,
                    operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureActionLifecycleTransition(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            if (!Enum.TryParse(operation.lifecycleType, true, out ActionLifecycleTransitionType transitionType) ||
                !Enum.IsDefined(typeof(ActionLifecycleTransitionType), transitionType))
            {
                context.Error("lifecycleType", "lifecycle_type_invalid", $"未知的 lifecycleType：{operation.lifecycleType}");
                transitionType = default;
            }
            return context.IsValid
                ? new AgentEnsureActionLifecycleTransitionCommand(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    First(operation.displayName, $"Lifecycle {operation.lifecycleType}"),
                    First(operation.lifecycleSlot, "OnExit"),
                    transitionType,
                    operation.reason,
                    ReadActionContext(operation),
                    operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureInputNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, operation.request, "inputId", "ensure_input_node 缺少 inputId。");
            return context.IsValid
                ? new AgentEnsureInputNodeCommand(
                    operation.id,
                    context.Path,
                    graph,
                    existing,
                    operation.nodeType,
                    First(operation.displayName, First(inputId, operation.nodeType)),
                    inputId,
                    operation.inputValueType,
                    operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureBlackboardDeclaration(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            string declarationAuthoringId = context.OptionalAuthoringId(operation.declarationAuthoringId, "declarationAuthoringId");
            string key = context.RequiredText(operation.blackboardKey, string.Empty, "blackboardKey", "ensure_blackboard_declaration 缺少 blackboardKey。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            bool valid = TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope) &
                         TryParseEnum(context, operation.blackboardLifetime, "blackboardLifetime", out PipelineBlackboardVariableLifetime lifetime) &
                         TryParseEnum(context, operation.blackboardAuthority, "blackboardAuthority", out PipelineBlackboardVariableAuthority authority) &
                         TryParseEnum(context, operation.blackboardSyncPolicy, "blackboardSyncPolicy", out PipelineBlackboardVariableSyncPolicy syncPolicy) &
                         TryParseEnum(context, First(operation.factProjection, "None"), "factProjection", out PipelineBlackboardFactProjectionKind projection);
            if (projection == PipelineBlackboardFactProjectionKind.ActionWindow && string.IsNullOrWhiteSpace(operation.windowType))
                context.Error("windowType", "window_type_missing", "ActionWindow declaration 必须显式提供 WindowType。");
            ValidateInputDerived(context, syncPolicy, operation.inputId);
            return context.IsValid && valid && valueType != null
                ? new AgentEnsureBlackboardDeclarationCommand(operation.id, context.Path, graph, declarationAuthoringId, key, valueType, scope, lifetime, authority, syncPolicy, operation.inputId, projection, operation.windowType, operation.windowId, operation.digest, operation.categoryPath)
                : null;
        }

        static AgentPatchCommand LowerDeleteBlackboardDeclaration(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            string declaration = context.OptionalAuthoringId(context.RequiredText(operation.declarationAuthoringId, string.Empty, "declarationAuthoringId", "delete_blackboard_declaration 缺少 declaration identity。"), "declarationAuthoringId");
            return context.IsValid ? new AgentDeleteBlackboardDeclarationCommand(operation.id, context.Path, graph, declaration) : null;
        }

        static AgentPatchCommand LowerMoveBlackboardDeclaration(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference sourceGraph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentGraphTargetReference targetGraph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphOperationId, "targetGraph");
            string declaration = context.OptionalAuthoringId(context.RequiredText(operation.declarationAuthoringId, string.Empty, "declarationAuthoringId", "move_blackboard_declaration 缺少 declaration identity。"), "declarationAuthoringId");
            string key = context.RequiredText(operation.blackboardKey, string.Empty, "blackboardKey", "move_blackboard_declaration 缺少 blackboardKey。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            bool valid = TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope) &
                         TryParseEnum(context, operation.blackboardLifetime, "blackboardLifetime", out PipelineBlackboardVariableLifetime lifetime) &
                         TryParseEnum(context, operation.blackboardAuthority, "blackboardAuthority", out PipelineBlackboardVariableAuthority authority) &
                         TryParseEnum(context, operation.blackboardSyncPolicy, "blackboardSyncPolicy", out PipelineBlackboardVariableSyncPolicy syncPolicy) &
                         TryParseEnum(context, First(operation.factProjection, "None"), "factProjection", out PipelineBlackboardFactProjectionKind projection);
            if (projection == PipelineBlackboardFactProjectionKind.ActionWindow && string.IsNullOrWhiteSpace(operation.windowType))
                context.Error("windowType", "window_type_missing", "ActionWindow declaration 必须显式提供 WindowType。");
            ValidateInputDerived(context, syncPolicy, operation.inputId);
            return context.IsValid && valid && valueType != null
                ? new AgentMoveBlackboardDeclarationCommand(operation.id, context.Path, sourceGraph, targetGraph, declaration, key, valueType, scope, lifetime, authority, syncPolicy, operation.inputId, projection, operation.windowType, operation.windowId, operation.digest, operation.categoryPath)
                : null;
        }

        static AgentPatchCommand LowerEnsureBlackboardWrite(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphOperationId, "targetGraph");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationOperationId, "declaration");
            string displayName = First(operation.displayName, operation.blackboardBoolValue ? "Set Blackboard True" : "Set Blackboard False");
            return context.IsValid
                ? new AgentEnsureBlackboardWriteCommand(operation.id, context.Path, graph, operation.targetElementAuthoringId, declaration, operation.blackboardBoolValue, displayName, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureTimelineTreeClip(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_timeline_tree_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            if (operation.endFrame <= operation.startFrame)
                context.Error("endFrame", "timeline_clip_range_invalid", "TreeClip endFrame 必须大于 startFrame。");
            AgentOperationOutputReference output = context.OptionalOutput(operation.clipOperationId, "clipOperationId", AgentPatchOutputKind.TimelineClip);
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTimelineTreeClipCommand(operation.id, context.Path, target, operation.startFrame, operation.endFrame, First(operation.timelinePhase, "Decision")) : null;
        }

        static AgentPatchCommand LowerEnsureMotionWarpTrack(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_track 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            return context.IsValid
                ? new AgentEnsureMotionWarpTrackCommand(operation.id, context.Path, timeline, track, First(operation.displayName, "Motion Warp"))
                : null;
        }

        static AgentPatchCommand LowerEnsureMotionWarpClip(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentOperationOutputReference trackOutput = context.OptionalOutput(operation.trackOperationId, "trackOperationId", AgentPatchOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "motion_warp_track_reference_invalid", "ensure_motion_warp_clip 必须且只能提供 trackAuthoringId 或 trackOperationId。", "引用已有 MotionWarpTrack 或前序 ensure_motion_warp_track output。");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, track, trackOutput, clip, default);
            return context.IsValid ? new AgentEnsureMotionWarpClipCommand(operation.id, context.Path, target, operation.startFrame, operation.endFrame) : null;
        }

        static AgentPatchCommand LowerConfigureMotionWarpSource(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerMotionWarpClipTarget(context, operation);
            string source = context.OptionalAuthoringId(
                context.RequiredText(operation.sourceMotionClipAuthoringId, string.Empty, "sourceMotionClipAuthoringId", "configure_motion_warp_source 缺少 source MotionCurve identity。"),
                "sourceMotionClipAuthoringId");
            return context.IsValid ? new AgentConfigureMotionWarpSourceCommand(operation.id, context.Path, target, source) : null;
        }

        static AgentPatchCommand LowerConfigureMotionWarpParameters(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerMotionWarpClipTarget(context, operation);
            if (!Enum.TryParse(operation.translationMode, true, out MotionWarpTranslationMode translationMode) || !Enum.IsDefined(typeof(MotionWarpTranslationMode), translationMode))
                context.Error("translationMode", "motion_warp_translation_mode_invalid", $"MotionWarp translation mode 无效：{operation.translationMode}");
            if (!Enum.TryParse(operation.targetOffsetSpace, true, out MotionWarpTargetOffsetSpace targetOffsetSpace) || !Enum.IsDefined(typeof(MotionWarpTargetOffsetSpace), targetOffsetSpace))
                context.Error("targetOffsetSpace", "motion_warp_target_offset_space_invalid", $"MotionWarp target offset space 无效：{operation.targetOffsetSpace}");
            if (!Enum.TryParse(operation.rotationMode, true, out MotionWarpRotationMode rotationMode) || !Enum.IsDefined(typeof(MotionWarpRotationMode), rotationMode))
                context.Error("rotationMode", "motion_warp_rotation_mode_invalid", $"MotionWarp rotation mode 无效：{operation.rotationMode}");
            if (!Enum.TryParse(operation.rotationMethod, true, out MotionWarpRotationMethod rotationMethod) || !Enum.IsDefined(typeof(MotionWarpRotationMethod), rotationMethod))
                context.Error("rotationMethod", "motion_warp_rotation_method_invalid", $"MotionWarp rotation method 无效：{operation.rotationMethod}");
            if (!Enum.TryParse(operation.limitPolicy, true, out MotionWarpLimitPolicy limitPolicy) || !Enum.IsDefined(typeof(MotionWarpLimitPolicy), limitPolicy))
                context.Error("limitPolicy", "motion_warp_limit_policy_invalid", $"MotionWarp limit policy 无效：{operation.limitPolicy}");
            AnimationCurve positionCurve = LowerAnimationCurve(context, operation.positionProgressCurve, "positionProgressCurve", 2, false);
            AnimationCurve yawCurve = LowerAnimationCurve(context, operation.yawProgressCurve, "yawProgressCurve", 2, false);
            return context.IsValid
                ? new AgentConfigureMotionWarpParametersCommand(
                    operation.id,
                    context.Path,
                    target,
                    translationMode,
                    targetOffsetSpace,
                    rotationMode,
                    rotationMethod,
                    operation.targetPlanarOffset,
                    operation.targetYawOffsetDegrees,
                    operation.maxTotalPositionCorrection,
                    operation.maxTotalYawCorrectionDegrees,
                    operation.maximumYawRateDegreesPerSecond,
                    limitPolicy,
                    positionCurve,
                    yawCurve)
                : null;
        }

        static AgentTimelineTargetReference LowerMotionWarpClipTarget(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.op} 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentOperationOutputReference clipOutput = context.OptionalOutput(operation.clipOperationId, "clipOperationId", AgentPatchOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "motion_warp_clip_reference_invalid", $"{operation.op} 必须且只能提供 clipAuthoringId 或 clipOperationId。");
            return new AgentTimelineTargetReference(timeline, track, default, clip, clipOutput);
        }

        static AnimationCurve LowerAnimationCurve(
            AgentPatchLoweringContext context,
            List<AgentAnimationCurveKey> source,
            string field,
            int minimumKeyCount,
            bool requireNormalized)
        {
            if (source == null || source.Count < minimumKeyCount)
            {
                context.Error(field, "animation_curve_missing", $"{field} 至少需要 {minimumKeyCount} 个 key。");
                return null;
            }
            var keys = new Keyframe[source.Count];
            float previousTime = -1f;
            for (int i = 0; i < source.Count; i++)
            {
                AgentAnimationCurveKey value = source[i];
                if (value == null || !Enum.TryParse(value.weightedMode, true, out WeightedMode weightedMode) || !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    context.Error($"{field}[{i}]", "animation_curve_key_invalid", $"{field}[{i}] 缺失或 weightedMode 无效。");
                    continue;
                }
                if (requireNormalized && (!IsNormalized(value.time) || !IsNormalized(value.value) ||
                                          value.time < previousTime))
                {
                    context.Error($"{field}[{i}]", "animation_curve_not_normalized", $"{field}[{i}] 必须按时间有序，且time/value位于[0,1]。");
                    continue;
                }
                previousTime = value.time;
                keys[i] = new Keyframe(value.time, value.value, value.inTangent, value.outTangent, value.inWeight, value.outWeight)
                {
                    weightedMode = weightedMode
                };
            }
            return context.IsValid ? new AnimationCurve(keys) : null;
        }

        static bool IsNormalized(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;

        static AgentPatchCommand LowerDeleteTimelineClip(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "delete_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "delete_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            return context.IsValid ? new AgentDeleteTimelineClipCommand(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip)) : null;
        }

        static AgentPatchCommand LowerMoveTimelineClip(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "move_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "move_timeline_clip 缺少 Track identity。"), "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "move_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            if (operation.frameOffset == 0)
                context.Error("frameOffset", "timeline_clip_offset_zero", "move_timeline_clip 的 frameOffset 不能为 0。");
            return context.IsValid
                ? new AgentMoveTimelineClipCommand(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip), operation.frameOffset)
                : null;
        }

        static AgentPatchCommand LowerConfigureTimelineClipEase(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "configure_timeline_clip_ease 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "configure_timeline_clip_ease 缺少 Track identity。"), "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "configure_timeline_clip_ease 缺少 Clip identity。"), "clipAuthoringId");
            if (operation.selfEaseInFrame < 0)
                context.Error("selfEaseInFrame", "timeline_clip_ease_negative", "selfEaseInFrame 不能小于 0。");
            if (operation.selfEaseOutFrame < 0)
                context.Error("selfEaseOutFrame", "timeline_clip_ease_negative", "selfEaseOutFrame 不能小于 0。");
            return context.IsValid
                ? new AgentConfigureTimelineClipEaseCommand(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, track, clip),
                    operation.selfEaseInFrame,
                    operation.selfEaseOutFrame)
                : null;
        }

        static AgentPatchCommand LowerConfigureTimelineCurveChannel(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "configure_timeline_curve_channel 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "configure_timeline_curve_channel 缺少 Track identity。"), "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "configure_timeline_curve_channel 缺少 Clip identity。"), "clipAuthoringId");
            string channelId = context.RequiredText(operation.curveChannelId, string.Empty, "curveChannelId", "configure_timeline_curve_channel 缺少registered ChannelId。");
            if (!TimelineCurveChannelCatalog.TryGet(channelId, out TimelineCurveChannelDescriptor descriptor))
                context.Error("curveChannelId", "timeline_curve_channel_unknown", $"未知 Timeline Curve ChannelId：{channelId}");
            AnimationCurve curve = LowerTimelineCurvePayload(context, operation.curve, "curve");
            return context.IsValid
                ? new AgentConfigureTimelineCurveChannelCommand(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, track, clip),
                    descriptor.ChannelId,
                    curve)
                : null;
        }

        static AnimationCurve LowerTimelineCurvePayload(
            AgentPatchLoweringContext context,
            AgentAnimationCurvePayload payload,
            string field)
        {
            if (payload == null)
            {
                context.Error(field, "timeline_curve_payload_missing", "Timeline curve payload不能为空。");
                return null;
            }
            if (!Enum.TryParse(payload.preWrapMode, true, out WrapMode preWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), preWrapMode))
                context.Error($"{field}.preWrapMode", "timeline_curve_wrap_mode_invalid", $"无效preWrapMode：{payload.preWrapMode}");
            if (!Enum.TryParse(payload.postWrapMode, true, out WrapMode postWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), postWrapMode))
                context.Error($"{field}.postWrapMode", "timeline_curve_wrap_mode_invalid", $"无效postWrapMode：{payload.postWrapMode}");
            if (payload.keys == null || payload.keys.Count == 0)
            {
                context.Error($"{field}.keys", "timeline_curve_keys_missing", "Timeline curve至少需要一个key。");
                return null;
            }
            var keys = new Keyframe[payload.keys.Count];
            float previousTime = -1f;
            for (int i = 0; i < payload.keys.Count; i++)
            {
                AgentAnimationCurveKey value = payload.keys[i];
                if (value == null ||
                    !Enum.TryParse(value.weightedMode, true, out WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    context.Error($"{field}.keys[{i}]", "timeline_curve_key_invalid", "Curve key缺失或weightedMode无效。");
                    continue;
                }
                if (!IsNormalized(value.time) || value.time <= previousTime ||
                    float.IsNaN(value.value) || float.IsInfinity(value.value) ||
                    float.IsNaN(value.inWeight) || float.IsInfinity(value.inWeight) ||
                    float.IsNaN(value.outWeight) || float.IsInfinity(value.outWeight))
                {
                    context.Error($"{field}.keys[{i}]", "timeline_curve_key_payload_invalid", "Curve key必须按normalized time严格递增，value与weight必须是有限数值。");
                    continue;
                }
                previousTime = value.time;
                keys[i] = new Keyframe(value.time, value.value, value.inTangent, value.outTangent, value.inWeight, value.outWeight)
                {
                    weightedMode = weightedMode
                };
            }
            if (!context.IsValid)
                return null;
            return new AnimationCurve(keys) { preWrapMode = preWrapMode, postWrapMode = postWrapMode };
        }

        static AgentPatchCommand LowerConfigureAnimationTrackMarkerSync(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            if (!Enum.TryParse(operation.animationSyncMode, true, out AnimationSyncMode mode) ||
                mode != AnimationSyncMode.None && mode != AnimationSyncMode.MarkerGroup)
            {
                context.Error("animationSyncMode", "animation_marker_sync_mode_invalid", $"Animation Sync mode 无效：{operation.animationSyncMode}");
            }
            string groupId = AnimationMarkerSyncAuthoring.NormalizeId(operation.animationSyncGroupId);
            AnimationMarkerSequenceTopology topology = AnimationMarkerSequenceTopology.Unspecified;
            AnimationMarkerSyncRole syncRole = AnimationMarkerSyncRole.Unspecified;
            if (mode == AnimationSyncMode.None)
            {
                if (!string.IsNullOrEmpty(groupId) ||
                    !string.IsNullOrEmpty(operation.animationMarkerSequenceTopology) ||
                    !string.IsNullOrEmpty(operation.animationMarkerSyncRole))
                    context.Error("animationSyncMode", "animation_marker_sync_none_residue", "None 模式不能携带 group、topology 或 role。");
            }
            else
            {
                if (string.IsNullOrEmpty(groupId) || !string.Equals(groupId, operation.animationSyncGroupId, StringComparison.Ordinal))
                    context.Error("animationSyncGroupId", "animation_marker_sync_group_invalid", "MarkerGroup 必须提供已规范化且无首尾空白的 SyncGroupId。");
                if (!Enum.TryParse(operation.animationMarkerSequenceTopology, true, out topology) ||
                    topology != AnimationMarkerSequenceTopology.Finite && topology != AnimationMarkerSequenceTopology.Cyclic)
                {
                    context.Error("animationMarkerSequenceTopology", "animation_marker_sync_topology_invalid", $"Animation marker topology 无效：{operation.animationMarkerSequenceTopology}");
                }
                if (!Enum.TryParse(operation.animationMarkerSyncRole, true, out syncRole) ||
                    syncRole != AnimationMarkerSyncRole.CanBeLeader &&
                    syncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                    syncRole != AnimationMarkerSyncRole.AlwaysFollower)
                {
                    context.Error("animationMarkerSyncRole", "animation_marker_sync_role_invalid", $"Animation marker sync role 无效：{operation.animationMarkerSyncRole}");
                }
            }
            return context.IsValid
                ? new AgentConfigureAnimationTrackMarkerSyncCommand(operation.id, context.Path, target, mode, groupId, topology, syncRole)
                : null;
        }

        static AgentPatchCommand LowerConfigureAnimationTrackChannel(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            string value = context.RequiredText(
                operation.animationChannelId,
                string.Empty,
                "animationChannelId",
                "configure_animation_track_channel 缺少 AnimationChannelId。");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                context.Error("animationChannelId", "animation_channel_id_invalid", "AnimationChannelId 不能包含首尾空白。");
            var animationChannelId = new AnimationChannelId(value);
            if (!animationChannelId.IsValid)
                context.Error("animationChannelId", "animation_channel_id_invalid", "AnimationChannelId 必须是非空稳定 identity。");
            return context.IsValid
                ? new AgentConfigureAnimationTrackChannelCommand(operation.id, context.Path, target, animationChannelId)
                : null;
        }

        static AgentPatchCommand LowerEnsureAnimationSyncMarker(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            string markerAuthoringId = context.OptionalAuthoringId(
                context.RequiredText(operation.markerAuthoringId, string.Empty, "markerAuthoringId", "ensure_animation_sync_marker 缺少 Marker stable identity。"),
                "markerAuthoringId");
            string markerId = context.RequiredText(operation.markerId, string.Empty, "markerId", "ensure_animation_sync_marker 缺少 MarkerId。");
            if (!string.Equals(markerId, markerId.Trim(), StringComparison.Ordinal))
                context.Error("markerId", "animation_marker_id_invalid", "MarkerId 不能包含首尾空白。");
            if (operation.markerFrame < 0)
                context.Error("markerFrame", "animation_marker_frame_negative", "Marker frame 不能小于 0。");
            return context.IsValid
                ? new AgentEnsureAnimationSyncMarkerCommand(operation.id, context.Path, target, markerAuthoringId, markerId, operation.markerFrame)
                : null;
        }

        static AgentPatchCommand LowerMoveAnimationSyncMarker(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            AgentAuthoringReference marker = context.RequiredMarker(operation.markerAuthoringId, operation.markerOperationId, "marker");
            if (operation.markerFrame < 0)
                context.Error("markerFrame", "animation_marker_frame_negative", "Marker frame 不能小于 0。");
            return context.IsValid
                ? new AgentMoveAnimationSyncMarkerCommand(operation.id, context.Path, target, marker, operation.markerFrame)
                : null;
        }

        static AgentPatchCommand LowerDeleteAnimationSyncMarker(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            AgentAuthoringReference marker = context.RequiredMarker(operation.markerAuthoringId, operation.markerOperationId, "marker");
            return context.IsValid
                ? new AgentDeleteAnimationSyncMarkerCommand(operation.id, context.Path, target, marker)
                : null;
        }

        static AgentTimelineTargetReference LowerAnimationTrackTarget(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(
                context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.op} 缺少 Timeline identity。"),
                "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentOperationOutputReference trackOutput = context.OptionalOutput(operation.trackOperationId, "trackOperationId", AgentPatchOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "animation_track_reference_invalid", $"{operation.op} 必须且只能提供 trackAuthoringId 或 trackOperationId。");
            return new AgentTimelineTargetReference(timeline, track, trackOutput, string.Empty, default);
        }

        static AgentPatchCommand LowerEnsureTreeClipBlackboardWrite(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_tree_clip_blackboard_write 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationOperationId, "declaration");
            AgentOperationOutputReference output = context.OptionalOutput(operation.clipOperationId, "clipOperationId", AgentPatchOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(operation.clipAuthoringId) && !output.IsValid)
                context.Error("clipAuthoringId", "clip_identity_missing", "ensure_tree_clip_blackboard_write 必须使用 stable Clip identity 或前序 TimelineClip output。");
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTreeClipBlackboardWriteCommand(operation.id, context.Path, target, declaration) : null;
        }

        static AgentPatchCommand LowerDeleteTransition(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, "stateMachine");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_transition 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteTransitionCommand(operation.id, context.Path, stateMachine, edge) : null;
        }

        static AgentPatchCommand LowerEnsureGameplayTag(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string tag = context.RequiredText(operation.gameplayTag, string.Empty, "gameplayTag", "ensure_gameplay_tag 缺少 tag id。");
            return context.IsValid ? new AgentEnsureGameplayTagCommand(operation.id, context.Path, tag, operation.parentGameplayTag, First(operation.displayName, tag), operation.debugCategory) : null;
        }

        static AgentPatchCommand LowerSetActionProfileGrantedTags(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            return context.IsValid ? new AgentSetActionProfileGrantedTagsCommand(operation.id, context.Path, profile, ReadTags(context, operation.grantedTags, "grantedTags")) : null;
        }

        static AgentPatchCommand LowerSetActionProfileCancelQuery(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            List<GameplayTagId> all = ReadTags(context, operation.queryAll, "queryAll");
            List<GameplayTagId> any = ReadTags(context, operation.queryAny, "queryAny");
            List<GameplayTagId> none = ReadTags(context, operation.queryNone, "queryNone");
            return context.IsValid ? new AgentSetActionProfileCancelQueryCommand(operation.id, context.Path, profile, all, any, none) : null;
        }

        static AgentPatchCommand LowerSetActionProfileTargetRequirement(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            TryParseEnum(context, operation.targetRequirement, "targetRequirement", out ActionTargetRequirement requirement);
            return context.IsValid
                ? new AgentSetActionProfileTargetRequirementCommand(operation.id, context.Path, profile, requirement)
                : null;
        }

        static AgentPatchCommand LowerSetActionRequestTimingClass(
            AgentPatchLoweringContext context,
            AgentPatchOperation operation)
        {
            string requestId = context.RequiredText(
                operation.request,
                string.Empty,
                "request",
                "set_action_request_timing_class 缺少 request id。");
            TryParseEnum(
                context,
                operation.requestTimingClass,
                "requestTimingClass",
                out CharacterActionRequestTimingClass timingClass);
            return context.IsValid
                ? new AgentSetActionRequestTimingClassCommand(operation.id, context.Path, requestId, timingClass)
                : null;
        }

        static AgentPatchCommand LowerEnsureAIControllerDefinition(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string controllerId = context.RequiredText(operation.controllerId, string.Empty, "controllerId", "ensure_ai_controller_definition 缺少 ControllerId。");
            if (!string.Equals(controllerId, controllerId.Trim(), StringComparison.Ordinal))
                context.Error("controllerId", "ai_controller_id_invalid", "ControllerId 不能包含首尾空白。");
            return context.IsValid ? new AgentEnsureAIControllerDefinitionCommand(operation.id, context.Path, controllerId) : null;
        }

        static AgentPatchCommand LowerEnsureAIControllerTree(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string path = context.RequiredText(operation.rootTreeAssetPath, string.Empty, "rootTreeAssetPath", "ensure_ai_controller_tree 缺少精确的 RootTree 资产路径。");
            return context.IsValid ? new AgentEnsureAIControllerTreeCommand(operation.id, context.Path, path) : null;
        }

        static AgentPatchCommand LowerBindAIControllerAssets(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            if (string.IsNullOrEmpty(operation.controlledCharacterAssetPath) && string.IsNullOrEmpty(operation.controlledCharacterAssetGuid))
                context.Error("controlledCharacter", "controlled_character_reference_missing", "bind_ai_controller_assets 缺少 Character Definition 资产引用。");
            if (string.IsNullOrEmpty(operation.perceptionProfileAssetPath) && string.IsNullOrEmpty(operation.perceptionProfileAssetGuid))
                context.Error("perceptionProfile", "perception_profile_reference_missing", "bind_ai_controller_assets 缺少 Perception Profile 资产引用。");
            return context.IsValid
                ? new AgentBindAIControllerAssetsCommand(
                    operation.id,
                    context.Path,
                    new AgentAssetReference(string.Empty, operation.controlledCharacterAssetPath, operation.controlledCharacterAssetGuid),
                    new AgentAssetReference(string.Empty, operation.perceptionProfileAssetPath, operation.perceptionProfileAssetGuid))
                : null;
        }

        static AgentPatchCommand LowerConfigureAICandidates(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            TryParseEnum(context, operation.candidateOrdering, "candidateOrdering", out AICandidateOrdering ordering);
            var values = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (operation.candidateActorIds == null)
            {
                context.Error("candidateActorIds", "candidate_actor_ids_missing", "configure_ai_candidates 缺少显式候选 ActorId 列表。");
            }
            else
            {
                for (int i = 0; i < operation.candidateActorIds.Count; i++)
                {
                    string actorId = operation.candidateActorIds[i];
                    if (string.IsNullOrWhiteSpace(actorId) || !string.Equals(actorId, actorId.Trim(), StringComparison.Ordinal) || !unique.Add(actorId))
                        context.Error($"candidateActorIds[{i}]", "candidate_actor_id_invalid", $"候选 ActorId 缺失、重复或包含首尾空白：{actorId}");
                    else
                        values.Add(actorId);
                }
            }
            return context.IsValid ? new AgentConfigureAICandidatesCommand(operation.id, context.Path, ordering, values) : null;
        }

        static AgentPatchCommand LowerEnsureAIBlackboardDeclaration(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.declarationAuthoringId, operation.declarationOperationId, "declaration", true);
            string key = context.RequiredText(operation.blackboardKey, operation.displayName, "blackboardKey", "ensure_ai_blackboard_declaration 缺少 Blackboard key。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope);
            if (scope != PipelineBlackboardVariableScope.AIController && scope != PipelineBlackboardVariableScope.AITick && scope != PipelineBlackboardVariableScope.Graph)
                context.Error("blackboardScope", "ai_blackboard_scope_invalid", $"AI Blackboard 不允许 scope：{scope}");
            return context.IsValid
                ? new AgentEnsureAIBlackboardDeclarationCommand(operation.id, context.Path, graph, existing, key, valueType, scope, AIBlackboardDefault(operation, valueType))
                : null;
        }

        static AgentPatchCommand LowerEnsureAISharedNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAISharedNodeKind nodeKind);
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (nodeKind == AgentAISharedNodeKind.Loop)
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (nodeKind == AgentAISharedNodeKind.Compare)
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            return context.IsValid
                ? new AgentEnsureAISharedNodeCommand(operation.id, context.Path, graph, existing, nodeKind, loopStopType, compareType, operation.position)
                : null;
        }

        static AgentPatchCommand LowerEnsureAIObservationNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIObservationNodeKind kind);
            return context.IsValid ? new AgentEnsureAIObservationNodeCommand(operation.id, context.Path, graph, existing, kind, operation.position) : null;
        }

        static AgentPatchCommand LowerEnsureAIMemoryNode(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationOperationId, "declaration");
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIMemoryNodeKind nodeKind);
            TryParseEnum(context, operation.aiMemoryValueKind, "aiMemoryValueKind", out AIMemoryValueKind valueKind);
            return context.IsValid ? new AgentEnsureAIMemoryNodeCommand(operation.id, context.Path, graph, existing, declaration, nodeKind, valueKind, operation.position) : null;
        }

        static AgentPatchCommand LowerEnsureAIContinuousInput(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_continuous_input 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIContinuousInputCommand(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        static AgentPatchCommand LowerEnsureAIActionTarget(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_action_target 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIActionTargetCommand(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        static AgentPatchCommand LowerEnsureAIActionRequest(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement", true);
            string requestId = context.RequiredText(operation.request, string.Empty, "request", "ensure_ai_action_request 缺少 RequestId。");
            TryParseEnum(context, operation.aiRequestRepeatPolicy, "aiRequestRepeatPolicy", out AIRequestRepeatPolicy repeatPolicy);
            if (operation.requestBufferSeconds < 0f)
                context.Error("requestBufferSeconds", "ai_request_buffer_invalid", "Action Request buffer seconds 不能小于 0。");
            return context.IsValid
                ? new AgentEnsureAIActionRequestCommand(operation.id, context.Path, graph, existing, requestId, operation.requestBufferSeconds, operation.requestPriority, repeatPolicy, operation.position)
                : null;
        }

        static AgentPatchCommand LowerDeleteFlowEdge(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_flow_edge 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteFlowEdgeCommand(operation.id, context.Path, graph, edge) : null;
        }

        static AgentPatchCommand LowerLinkFlow(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourceOperationId, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement");
            return context.IsValid
                ? new AgentLinkFlowCommand(operation.id, context.Path, graph, source, target, First(operation.startPort, "Output"), First(operation.endPort, "Input"))
                : null;
        }

        static AgentPatchCommand LowerLinkProperty(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourceOperationId, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetOperationId, "targetElement");
            string startPort = context.RequiredText(operation.startPropertyPort, string.Empty, "startPropertyPort", "link_property 缺少 startPropertyPort。");
            string endPort = context.RequiredText(operation.endPropertyPort, string.Empty, "endPropertyPort", "link_property 缺少 endPropertyPort。");
            return context.IsValid
                ? new AgentLinkPropertyCommand(operation.id, context.Path, graph, source, target, startPort, endPort)
                : null;
        }

        static AgentPatchCommand LowerEnsureBTConditionRule(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphOperationId, "graph");
            AgentFlowEdgeTargetReference edge = context.RequiredFlowEdge(operation.flowEdgeAuthoringId, operation.flowEdgeOperationId, "flowEdge");
            TryParseEnum(context, operation.abortPolicy, "abortPolicy", out BTAbortPolicy abortPolicy);
            List<AgentConditionGroupCommand> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureBTConditionRuleCommand(operation.id, context.Path, graph, edge, abortPolicy, groups)
                : null;
        }

        static AgentAssetReference ReadActionContext(AgentPatchOperation operation)
        {
            return new AgentAssetReference(operation.actionContext, operation.actionContextAssetPath, operation.actionContextAssetGuid);
        }

        static AgentAssetReference ReadActionProfile(AgentPatchLoweringContext context, AgentPatchOperation operation)
        {
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ActionProfile identity 缺失。");
            return new AgentAssetReference(actionProfile, string.Empty, string.Empty);
        }

        static List<GameplayTagId> ReadTags(AgentPatchLoweringContext context, List<string> values, string field)
        {
            var result = new List<GameplayTagId>();
            var unique = new HashSet<GameplayTagId>();
            if (values == null)
                return result;
            for (int i = 0; i < values.Count; i++)
            {
                var tag = new GameplayTagId(values[i]);
                if (!tag.IsValid || !unique.Add(tag))
                {
                    context.Error($"{field}[{i}]", "gameplay_tag_invalid", $"GameplayTag 缺失或重复：{values[i]}");
                    continue;
                }
                result.Add(tag);
            }
            return result;
        }

        static Type ParseBlackboardValueType(AgentPatchLoweringContext context, string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "bool": case "boolean": return typeof(bool);
                case "int": case "int32": return typeof(int);
                case "float": case "single": return typeof(float);
                case "string": return typeof(string);
                case "vector2": return typeof(Vector2);
                case "vector3": return typeof(Vector3);
                case "actiontargetsnapshot": case "action_target_snapshot": return typeof(ActionTargetSnapshot);
                case "aiactorid": case "ai_actor_id": return typeof(AIActorIdValue);
                case "aiactiontargetsnapshot": case "ai_action_target_snapshot": return typeof(AIActionTargetSnapshotValue);
                default:
                    context.Error("blackboardValueType", "blackboard_value_type_invalid", $"不支持的 Blackboard value type：{value}");
                    return null;
            }
        }

        static object AIBlackboardDefault(AgentPatchOperation operation, Type valueType)
        {
            if (valueType == typeof(bool)) return operation.blackboardBoolValue;
            if (valueType == typeof(int)) return operation.blackboardIntValue;
            if (valueType == typeof(float)) return operation.blackboardFloatValue;
            if (valueType == typeof(Vector2)) return operation.blackboardVector2Value;
            if (valueType == typeof(Vector3)) return operation.blackboardVector3Value;
            if (valueType == typeof(AIActorIdValue)) return new AIActorIdValue(operation.blackboardActorIdValue);
            if (valueType == typeof(AIActionTargetSnapshotValue))
            {
                return new AIActionTargetSnapshotValue(
                    new AIActorIdValue(operation.blackboardTargetActorIdValue),
                    operation.blackboardTargetPositionValue,
                    operation.blackboardTargetYawValue);
            }
            throw new InvalidOperationException($"Unsupported AI Blackboard value type: {valueType?.FullName}");
        }

        static void ValidateInputDerived(
            AgentPatchLoweringContext context,
            PipelineBlackboardVariableSyncPolicy syncPolicy,
            string inputValueId)
        {
            if (syncPolicy == PipelineBlackboardVariableSyncPolicy.InputDerived)
            {
                if (string.IsNullOrWhiteSpace(inputValueId))
                    context.Error("inputId", "input_value_id_missing", "InputDerived declaration 必须显式提供 inputId。");
            }
            else if (!string.IsNullOrWhiteSpace(inputValueId))
            {
                context.Error("inputId", "input_value_id_forbidden", "非 InputDerived declaration 不得保留 inputId。");
            }
        }

        static bool TryParseEnum<T>(AgentPatchLoweringContext context, string value, string field, out T result) where T : struct
        {
            if (Enum.TryParse(value, true, out result) && Enum.IsDefined(typeof(T), result))
                return true;
            context.Error(field, $"{field}_invalid", $"{field} 无效：{value}");
            return false;
        }

        static string First(string value, string fallback)
        {
            return !string.IsNullOrEmpty(value) ? value : fallback ?? string.Empty;
        }
    }

    [Flags]
    public enum AgentPatchDomainMask
    {
        CharacterController = 1,
        AIController = 2,
        Both = CharacterController | AIController
    }

    public sealed class AgentPatchOperationDescriptor
    {
        readonly Func<AgentPatchLoweringContext, AgentPatchOperation, AgentPatchCommand> m_Lower;

        internal AgentPatchOperationDescriptor(
            AgentPatchCommandKind kind,
            AgentPatchOutputKind outputKind,
            Func<AgentPatchLoweringContext, AgentPatchOperation, AgentPatchCommand> lower,
            AgentPatchDomainMask domains = AgentPatchDomainMask.CharacterController)
        {
            Kind = kind;
            OutputKind = outputKind;
            m_Lower = lower;
            Domains = domains;
        }

        public AgentPatchCommandKind Kind { get; }
        public AgentPatchOutputKind OutputKind { get; }
        public AgentPatchDomainMask Domains { get; }
        public bool Allows(string domain)
        {
            return string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal)
                ? (Domains & AgentPatchDomainMask.CharacterController) != 0
                : string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal) &&
                  (Domains & AgentPatchDomainMask.AIController) != 0;
        }
        internal AgentPatchCommand Lower(AgentPatchLoweringContext context, AgentPatchOperation operation) => m_Lower(context, operation);
    }

    public sealed class AgentPatchLoweringContext
    {
        static readonly IReadOnlyDictionary<string, AgentConditionTermKind> s_TermKinds =
            new Dictionary<string, AgentConditionTermKind>(StringComparer.OrdinalIgnoreCase)
            {
                ["move_stop"] = AgentConditionTermKind.MoveStop,
                ["move_has"] = AgentConditionTermKind.MoveHas,
                ["move_run"] = AgentConditionTermKind.MoveRun,
                ["move_walk"] = AgentConditionTermKind.MoveWalk,
                ["turn_facing_angle"] = AgentConditionTermKind.TurnFacingAngle,
                ["blackboard_bool"] = AgentConditionTermKind.BlackboardBool,
                ["state_root_completed"] = AgentConditionTermKind.StateRootCompleted,
                ["action_request"] = AgentConditionTermKind.ActionRequest,
                ["action_window_active"] = AgentConditionTermKind.ActionWindowActive,
                ["action_can_activate"] = AgentConditionTermKind.CanActivateAction,
                ["ai_target_distance_compare_blackboard"] = AgentConditionTermKind.AITargetDistanceCompareBlackboard
            };

        readonly AgentCompileReport m_Report;
        readonly string m_OperationId;
        readonly IReadOnlyDictionary<string, AgentPlannedOutputSymbol> m_PreviousSymbols;
        int m_ErrorCount;

        internal AgentPatchLoweringContext(
            AgentCompileReport report,
            string path,
            string operationId,
            IReadOnlyDictionary<string, AgentPlannedOutputSymbol> previousSymbols)
        {
            m_Report = report;
            Path = path;
            m_OperationId = operationId;
            m_PreviousSymbols = previousSymbols;
        }

        public string Path { get; }
        public bool HasErrors => m_ErrorCount > 0;
        public bool IsValid => !HasErrors;

        public void ReadTransition(
            AgentPatchOperation operation,
            out AgentStateMachineTargetReference stateMachine,
            out AgentElementTargetReference from,
            out AgentElementTargetReference to)
        {
            stateMachine = RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, "stateMachine");
            from = RequiredElement(operation.fromElementAuthoringId, operation.fromOperationId, "fromElement");
            to = RequiredElement(operation.toElementAuthoringId, operation.toOperationId, "toElement");
        }

        public AgentStateBehaviorTargetReference RequiredStateBehaviorTarget(AgentPatchOperation operation)
        {
            bool hasDirect = HasValue(operation.targetGraphAuthoringId) || HasValue(operation.targetGraphOperationId);
            bool hasState = HasValue(operation.stateMachineGraphAuthoringId) || HasValue(operation.stateMachineOperationId) ||
                            HasValue(operation.stateAuthoringId) || HasValue(operation.stateOperationId);
            if (hasDirect && hasState)
            {
                Error(string.Empty, "state_behavior_target_ambiguous", "State behavior target 不能同时使用 direct graph 与 StateMachine/State reference。");
                return default;
            }
            if (hasDirect)
                return new AgentStateBehaviorTargetReference(RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphOperationId, "targetGraph"), default, default);
            if (!hasState)
            {
                Error(string.Empty, "state_behavior_identity_missing", "Operation 必须用 target graph identity，或 StateMachine + State identity 指定 State body。");
                return default;
            }
            return new AgentStateBehaviorTargetReference(
                default,
                RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachineOperationId, "stateMachine"),
                RequiredState(operation.stateAuthoringId, operation.stateOperationId, "state"));
        }

        public AgentGraphTargetReference RequiredGraph(string authoringId, string operationId, string label)
        {
            return new AgentGraphTargetReference(RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.StateMachine, AgentPatchOutputKind.State));
        }

        public AgentStateMachineTargetReference RequiredStateMachine(string authoringId, string operationId, string label, bool allowSelf = false)
        {
            return new AgentStateMachineTargetReference(RequiredReference(authoringId, operationId, label, allowSelf, AgentPatchOutputKind.StateMachine));
        }

        public AgentStateTargetReference RequiredState(string authoringId, string operationId, string label)
        {
            return new AgentStateTargetReference(RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.State));
        }

        public AgentStateTargetReference OptionalState(string authoringId, string operationId, string label, bool allowSelf = false)
        {
            return new AgentStateTargetReference(OptionalReference(authoringId, operationId, label, allowSelf, AgentPatchOutputKind.State));
        }

        public AgentElementTargetReference RequiredElement(string authoringId, string operationId, string label)
        {
            return new AgentElementTargetReference(RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.StateMachine, AgentPatchOutputKind.State, AgentPatchOutputKind.Node));
        }

        public AgentElementTargetReference OptionalElement(string authoringId, string operationId, string label, bool allowSelf = false)
        {
            return new AgentElementTargetReference(OptionalReference(authoringId, operationId, label, allowSelf, AgentPatchOutputKind.StateMachine, AgentPatchOutputKind.State, AgentPatchOutputKind.Node));
        }

        public AgentFlowEdgeTargetReference RequiredFlowEdge(string authoringId, string operationId, string label)
        {
            return new AgentFlowEdgeTargetReference(RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.FlowEdge));
        }

        public AgentAuthoringReference RequiredDeclaration(string authoringId, string operationId, string label)
        {
            return RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.BlackboardDeclaration);
        }

        public AgentAuthoringReference RequiredMarker(string authoringId, string operationId, string label)
        {
            return RequiredReference(authoringId, operationId, label, AgentPatchOutputKind.TimelineMarker);
        }

        public AgentOperationOutputReference OptionalOutput(string operationId, string label, AgentPatchOutputKind expectedKind)
        {
            AgentAuthoringReference reference = OptionalReference(string.Empty, operationId, label, false, expectedKind);
            return reference.OperationOutput;
        }

        public string OptionalAuthoringId(string authoringId, string label)
        {
            if (!HasValue(authoringId))
                return string.Empty;
            if (!AuthoringIdentity.IsValid(authoringId))
                Error(label, "authoring_identity_invalid", $"Authoring identity 格式无效：{authoringId}");
            return authoringId;
        }

        public string RequiredText(string primary, string secondary, string field, string message)
        {
            string value = HasValue(primary) ? primary : secondary;
            if (!HasValue(value))
                Error(field, $"{field}_missing", message);
            return value ?? string.Empty;
        }

        public List<AgentConditionGroupCommand> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentPatchOperation operation)
        {
            return RequiredConditionGroups(groups, operation, "conditionGroups");
        }

        public List<AgentConditionGroupCommand> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentPatchOperation operation,
            string field)
        {
            var result = new List<AgentConditionGroupCommand>();
            if (groups == null || groups.Count == 0)
            {
                Error(field, "condition_groups_empty", $"{operation.op} 必须包含至少一个条件组。");
                return result;
            }
            for (int i = 0; i < groups.Count; i++)
            {
                AgentConditionGroup group = groups[i];
                if (group == null)
                {
                    Error($"{field}[{i}]", "condition_group_missing", "Condition group 为空。");
                    continue;
                }
                List<AgentConditionTermCommand> terms = ConditionTerms(group.terms, operation, $"{field}[{i}].terms", false);
                if (terms.Count > 0)
                    result.Add(new AgentConditionGroupCommand(terms));
            }
            return result;
        }

        public List<AgentConditionTermCommand> ConditionTerms(
            List<AgentConditionTerm> terms,
            AgentPatchOperation operation,
            string field,
            bool allowEmpty)
        {
            var result = new List<AgentConditionTermCommand>();
            if (terms == null || terms.Count == 0)
            {
                if (!allowEmpty)
                    Error(field, "condition_group_terms_empty", "Condition group 必须包含至少一个 term。");
                return result;
            }
            for (int i = 0; i < terms.Count; i++)
            {
                AgentConditionTerm source = terms[i];
                string termField = $"{field}[{i}]";
                if (source == null || !TryParseTermKind(source.kind, out AgentConditionTermKind kind))
                {
                    Error(termField, "condition_term_unsupported", $"不支持的 Condition term：{source?.kind}");
                    continue;
                }
                string blackboardKey = source.blackboardKey ?? string.Empty;
                string request = HasValue(source.request) ? source.request : HasValue(operation.request) ? operation.request : operation.inputId;
                if (kind == AgentConditionTermKind.BlackboardBool && !HasValue(blackboardKey))
                {
                    Error($"{termField}.blackboardKey", "blackboard_key_missing", "blackboard_bool condition 缺少 blackboardKey。");
                    continue;
                }
                if (kind == AgentConditionTermKind.ActionRequest && !HasValue(request))
                {
                    Error($"{termField}.request", "request_missing", "action_request condition 缺少 request。");
                    continue;
                }
                if (kind == AgentConditionTermKind.ActionWindowActive && !HasValue(source.windowType))
                {
                    Error($"{termField}.windowType", "window_type_missing", "action_window_active condition 缺少 windowType。");
                    continue;
                }
                if (kind == AgentConditionTermKind.CanActivateAction && !HasValue(source.actionProfile))
                {
                    Error($"{termField}.actionProfile", "action_profile_missing", "action_can_activate condition 缺少 ActionProfile identity。");
                    continue;
                }
                CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
                if (kind == AgentConditionTermKind.AITargetDistanceCompareBlackboard)
                {
                    if (!HasValue(blackboardKey))
                    {
                        Error($"{termField}.blackboardKey", "blackboard_key_missing", "ai_target_distance_compare_blackboard condition 缺少 blackboardKey。");
                        continue;
                    }
                    if (!Enum.TryParse(source.compareType, true, out compareType))
                    {
                        Error($"{termField}.compareType", "compare_type_invalid", $"CompareType 无效：{source.compareType}");
                        continue;
                    }
                }
                result.Add(new AgentConditionTermCommand(
                    kind,
                    blackboardKey,
                    source.negate,
                    request,
                    source.windowType,
                    new AgentAssetReference(source.actionProfile, source.actionProfileAssetPath, source.actionProfileAssetGuid),
                    source.targetSnapshotBlackboardKey,
                    compareType));
            }
            return result;
        }

        public void Error(string field, string code, string message, string suggestion = "")
        {
            m_ErrorCount++;
            m_Report.Error(string.IsNullOrEmpty(field) ? Path : $"{Path}.{field}", code, message, suggestion);
        }

        public void ValidateOwnedReferences(AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineCommand stateMachine:
                    ValidateOwnedReference(stateMachine.ExistingOwner.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureStateCommand state:
                    ValidateOwnedReference(state.ExistingState.Value, command.OwnerScope, "state");
                    break;
                case AgentDeleteStateCommand deleteState:
                    ValidateOwnedReference(deleteState.State.Value, command.OwnerScope, "state");
                    break;
                case AgentEnsureTransitionCommand transition:
                    ValidateOwnedReference(transition.From.Value, command.OwnerScope, "fromElement");
                    ValidateOwnedReference(transition.To.Value, command.OwnerScope, "toElement");
                    break;
                case AgentEnsureActionExitLifecycleCommand actionExit:
                    ValidateOwnedReference(actionExit.Source.Value, command.OwnerScope, "sourceElement");
                    break;
                case AgentDeleteStateBehaviorNodeCommand delete:
                    ValidateOwnedReference(delete.Element.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureStateBehaviorNodeCommand node:
                    ValidateOwnedReference(node.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureTimelineNodeCommand timeline:
                    ValidateOwnedReference(timeline.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionActivationCommand activation:
                    ValidateOwnedReference(activation.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionLifecycleTransitionCommand lifecycle:
                    ValidateOwnedReference(lifecycle.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureInputNodeCommand input:
                    ValidateOwnedReference(input.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentGraphLinkCommand link:
                    ValidateOwnedReference(link.Source.Value, command.OwnerScope, "sourceElement");
                    ValidateOwnedReference(link.Target.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIBlackboardDeclarationCommand declaration:
                    ValidateOwnedReference(declaration.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(declaration.ExistingDeclaration.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAISharedNodeCommand shared:
                    ValidateOwnedReference(shared.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(shared.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIObservationNodeCommand observation:
                    ValidateOwnedReference(observation.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(observation.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIMemoryNodeCommand memory:
                    ValidateOwnedReference(memory.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(memory.ExistingNode.Value, command.OwnerScope, "targetElement");
                    ValidateOwnedReference(memory.Declaration, command.OwnerScope, "declaration");
                    break;
                case AgentEnsureAIContinuousInputCommand continuousInput:
                    ValidateOwnedReference(continuousInput.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(continuousInput.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionTargetCommand actionTarget:
                    ValidateOwnedReference(actionTarget.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionTarget.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionRequestCommand actionRequest:
                    ValidateOwnedReference(actionRequest.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionRequest.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
            }
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string operationId,
            string label,
            params AgentPatchOutputKind[] expectedKinds)
        {
            return RequiredReference(authoringId, operationId, label, false, expectedKinds);
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string operationId,
            string label,
            bool allowSelf,
            params AgentPatchOutputKind[] expectedKinds)
        {
            AgentAuthoringReference reference = OptionalReference(authoringId, operationId, label, allowSelf, expectedKinds);
            if (!reference.IsValid && !(allowSelf && IsSelfReference(operationId)))
                Error(label, $"{label}_identity_missing", $"schema v16 operation 缺少 {label} authoring identity/operation reference。");
            return reference;
        }

        AgentAuthoringReference OptionalReference(
            string authoringId,
            string operationId,
            string label,
            bool allowSelf,
            params AgentPatchOutputKind[] expectedKinds)
        {
            bool hasAuthoring = HasValue(authoringId);
            bool hasOperation = HasValue(operationId);
            if (!hasAuthoring && !hasOperation)
                return default;
            if (hasAuthoring && hasOperation)
            {
                Error(label, $"{label}_reference_ambiguous", $"{label} 不能同时包含 authoring identity 与 operation reference。");
                return default;
            }
            if (hasAuthoring)
            {
                if (!AuthoringIdentity.IsValid(authoringId))
                {
                    Error(label, "authoring_identity_invalid", $"Authoring identity 格式无效：{authoringId}");
                    return default;
                }
                return new AgentAuthoringReference(authoringId, default);
            }

            AgentOperationOutputReference output = AgentOperationOutputReference.Parse(operationId);
            if (allowSelf && IsSelfReference(output.OperationId) && string.IsNullOrEmpty(output.Role))
                return default;
            if (!m_PreviousSymbols.TryGetValue(output.OperationId, out AgentPlannedOutputSymbol symbol))
            {
                Error(label, $"{label}_operation_reference_invalid", $"Operation reference 必须指向更早的 operation：{operationId}");
                return default;
            }
            if (!Contains(expectedKinds, symbol.Kind))
            {
                Error(label, $"{label}_operation_output_kind_invalid", $"Operation {output.OperationId} 输出 {symbol.Kind}，不能作为 {label}。");
                return default;
            }
            if (!string.IsNullOrEmpty(output.Role) &&
                (symbol.Kind != AgentPatchOutputKind.StateMachine || !IsStateMachineControlRole(output.Role)))
            {
                Error(label, $"{label}_operation_role_invalid", $"Operation output role 无效：{operationId}");
                return default;
            }
            return new AgentAuthoringReference(string.Empty, output);
        }

        void ValidateOwnedReference(AgentAuthoringReference reference, string expectedOwnerScope, string label)
        {
            AgentOperationOutputReference output = reference.OperationOutput;
            if (!output.IsValid || !m_PreviousSymbols.TryGetValue(output.OperationId, out AgentPlannedOutputSymbol symbol))
                return;

            string actualOwnerScope = !string.IsNullOrEmpty(output.Role) && symbol.Kind == AgentPatchOutputKind.StateMachine
                ? output.OperationId
                : symbol.OwnerScope;
            if (!string.Equals(actualOwnerScope, expectedOwnerScope, StringComparison.Ordinal))
            {
                Error(
                    label,
                    $"{label}_operation_owner_mismatch",
                    $"Operation output {output.Value} 属于 {actualOwnerScope}，不能用于 owner {expectedOwnerScope}。");
            }
        }

        bool IsSelfReference(string operationId)
        {
            return string.Equals(operationId, m_OperationId, StringComparison.Ordinal);
        }

        static bool Contains(AgentPatchOutputKind[] values, AgentPatchOutputKind value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                    return true;
            }
            return false;
        }

        static bool IsStateMachineControlRole(string role)
        {
            return string.Equals(role, "StateMachineEnterNode", StringComparison.Ordinal) ||
                   string.Equals(role, "StateMachineAnyStateNode", StringComparison.Ordinal) ||
                   string.Equals(role, "StateMachineExitNode", StringComparison.Ordinal);
        }

        static bool TryParseTermKind(string value, out AgentConditionTermKind kind)
        {
            kind = default;
            return value != null && s_TermKinds.TryGetValue(value, out kind);
        }

        static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);
    }
}
