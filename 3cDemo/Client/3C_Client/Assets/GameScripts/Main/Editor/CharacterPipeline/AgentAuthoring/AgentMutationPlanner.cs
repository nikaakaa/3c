using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationPlanner
    {
        public bool TryCreatePlan(AgentMutationDraftSet drafts, AgentCompileReport report, out AgentMutationPlan plan)
        {
            plan = null;
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (drafts == null)
            {
                report.Error("document.editable", "mutation_draft_missing", "Reconciler内部Mutation Draft缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (!string.Equals(drafts.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                report.Error(
                    "document.schemaVersion",
                    "unsupported_schema_version",
                    $"Mutation Draft schema必须是{AgentAuthoringSchema.Version}，当前为{drafts.schemaVersion}。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            report.domain = drafts.domain ?? string.Empty;
            report.rootIdentity = drafts.rootIdentity ?? string.Empty;
            if (!AgentAuthoringSchema.IsDomain(drafts.domain))
            {
                report.Error("document.domain", "unsupported_domain", $"Mutation domain无效：{drafts.domain}");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (string.IsNullOrWhiteSpace(drafts.rootIdentity) || string.IsNullOrWhiteSpace(drafts.sourceRevision))
            {
                report.Error("document", "document_source_identity_missing", "Mutation Draft缺少rootIdentity或sourceRevision。");
                report.metrics.schemaInvalidCount++;
                return false;
            }
            if (drafts.mutations == null)
            {
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
                report.metrics.schemaInvalidCount++;
                return false;
            }

            var commands = new List<AgentMutation>(drafts.mutations.Count);
            var plannedIdentities = new Dictionary<string, AgentPlannedIdentitySymbol>(StringComparer.Ordinal);
            var mutationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < drafts.mutations.Count; i++)
            {
                AgentMutationDraft operation = drafts.mutations[i];
                string path = string.IsNullOrEmpty(operation?.sourcePath)
                    ? $"document.mutations[{i}]"
                    : operation.sourcePath;
                if (operation == null)
                {
                    report.Error(path, "mutation_missing", "内部Mutation Draft为空。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(operation.id))
                {
                    report.Error(path, "mutation_id_missing", "每个内部Mutation必须使用唯一id。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!mutationIds.Add(operation.id))
                {
                    report.Error(path, "mutation_id_duplicate", $"Mutation id重复：{operation.id}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!AgentTypedMutationLoweringCatalog.TryGet(operation.kind, out AgentMutationDraftDescriptor descriptor))
                {
                    report.Error(path, "unknown_mutation", $"内部Mutation kind没有typed lowering：{operation.kind}");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }
                if (!descriptor.Allows(drafts.domain))
                {
                    report.Error(path, "mutation_domain_mismatch", $"Mutation '{operation.kind}'不允许用于{drafts.domain} domain。");
                    report.metrics.schemaInvalidCount++;
                    continue;
                }

                var context = new AgentMutationPlanningContext(report, path, operation.id, plannedIdentities);
                AgentMutation command = descriptor.Lower(context, operation);
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
                plannedIdentities.Add(operation.id, new AgentPlannedIdentitySymbol(operation.id, descriptor.OutputKind, command.OwnerScope));
                report.metrics.schemaValidCount++;
            }

            if (report.HasErrors())
                return false;

            plan = new AgentMutationPlan(commands, drafts.domain, drafts.rootIdentity, drafts.sourceRevision);
            return true;
        }
    }

    public static class AgentTypedMutationLoweringCatalog
    {
        static readonly Dictionary<AgentMutationKind, AgentMutationDraftDescriptor> s_Descriptors =
            new Dictionary<AgentMutationKind, AgentMutationDraftDescriptor>()
            {
                [AgentMutationKind.EnsureStateMachine] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureStateMachine, AgentMutationOutputKind.StateMachine, LowerEnsureStateMachine),
                [AgentMutationKind.EnsureState] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureState, AgentMutationOutputKind.State, LowerEnsureState),
                [AgentMutationKind.DeleteState] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteState, AgentMutationOutputKind.None, LowerDeleteState),
                [AgentMutationKind.EnsureTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTransition, AgentMutationOutputKind.Transition, LowerEnsureTransition),
                [AgentMutationKind.RewireTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.RewireTransition, AgentMutationOutputKind.None, LowerRewireTransition),
                [AgentMutationKind.EnsureConditionRule] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureConditionRule, AgentMutationOutputKind.Transition, LowerEnsureConditionRule),
                [AgentMutationKind.EnsureActionExitLifecycle] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionExitLifecycle, AgentMutationOutputKind.Node, LowerEnsureActionExitLifecycle),
                [AgentMutationKind.DeleteStateBehaviorNode] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteStateBehaviorNode, AgentMutationOutputKind.None, LowerDeleteStateBehaviorNode),
                [AgentMutationKind.EnsureStateBehaviorNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureStateBehaviorNode, AgentMutationOutputKind.Node, LowerEnsureStateBehaviorNode),
                [AgentMutationKind.EnsureTimelineNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTimelineNode, AgentMutationOutputKind.Node, LowerEnsureTimelineNode),
                [AgentMutationKind.EnsureInlineTimeline] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureInlineTimeline, AgentMutationOutputKind.Timeline, LowerEnsureInlineTimeline),
                [AgentMutationKind.EnsureActionActivation] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionActivation, AgentMutationOutputKind.Node, LowerEnsureActionActivation),
                [AgentMutationKind.EnsureActionLifecycleTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureActionLifecycleTransition, AgentMutationOutputKind.Node, LowerEnsureActionLifecycleTransition),
                [AgentMutationKind.EnsureInputNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureInputNode, AgentMutationOutputKind.Node, LowerEnsureInputNode),
                [AgentMutationKind.EnsureConditionValueNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureConditionValueNode, AgentMutationOutputKind.Node, LowerEnsureConditionValueNode),
                [AgentMutationKind.ConfigureActionAdmission] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureActionAdmission, AgentMutationOutputKind.None, LowerConfigureActionAdmission),
                [AgentMutationKind.EnsureBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, LowerEnsureBlackboardDeclaration),
                [AgentMutationKind.MoveBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.MoveBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, LowerMoveBlackboardDeclaration),
                [AgentMutationKind.DeleteBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteBlackboardDeclaration, AgentMutationOutputKind.None, LowerDeleteBlackboardDeclaration, AgentMutationDomainMask.Both),
                [AgentMutationKind.EnsureBlackboardWrite] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureBlackboardWrite, AgentMutationOutputKind.Node, LowerEnsureBlackboardWrite),
                [AgentMutationKind.EnsureTimelineTreeClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTimelineTreeClip, AgentMutationOutputKind.TimelineClip, LowerEnsureTimelineTreeClip),
                [AgentMutationKind.EnsureMotionCurveTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionCurveTrack, AgentMutationOutputKind.TimelineTrack, LowerEnsureMotionCurveTrack),
                [AgentMutationKind.EnsureMotionCurveClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionCurveClip, AgentMutationOutputKind.TimelineClip, LowerEnsureMotionCurveClip),
                [AgentMutationKind.ConfigureMotionCurveClip] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionCurveClip, AgentMutationOutputKind.None, LowerConfigureMotionCurveClip),
                [AgentMutationKind.EnsureMotionWarpTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionWarpTrack, AgentMutationOutputKind.TimelineTrack, LowerEnsureMotionWarpTrack),
                [AgentMutationKind.DeleteTimelineTrack] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTimelineTrack, AgentMutationOutputKind.None, LowerDeleteTimelineTrack),
                [AgentMutationKind.EnsureMotionWarpClip] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureMotionWarpClip, AgentMutationOutputKind.TimelineClip, LowerEnsureMotionWarpClip),
                [AgentMutationKind.ConfigureMotionWarpSource] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionWarpSource, AgentMutationOutputKind.None, LowerConfigureMotionWarpSource),
                [AgentMutationKind.ConfigureMotionWarpParameters] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureMotionWarpParameters, AgentMutationOutputKind.None, LowerConfigureMotionWarpParameters),
                [AgentMutationKind.MoveTimelineClip] = new AgentMutationDraftDescriptor(AgentMutationKind.MoveTimelineClip, AgentMutationOutputKind.None, LowerMoveTimelineClip),
                [AgentMutationKind.ConfigureTimelineClipEase] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureTimelineClipEase, AgentMutationOutputKind.None, LowerConfigureTimelineClipEase),
                [AgentMutationKind.ConfigureTimelineCurveChannel] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureTimelineCurveChannel, AgentMutationOutputKind.None, LowerConfigureTimelineCurveChannel),
                [AgentMutationKind.ConfigureAnimationTrackChannel] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAnimationTrackChannel, AgentMutationOutputKind.None, LowerConfigureAnimationTrackChannel),
                [AgentMutationKind.ConfigureAnimationTrackMarkerSync] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAnimationTrackMarkerSync, AgentMutationOutputKind.None, LowerConfigureAnimationTrackMarkerSync),
                [AgentMutationKind.EnsureAnimationSyncMarker] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAnimationSyncMarker, AgentMutationOutputKind.TimelineMarker, LowerEnsureAnimationSyncMarker),
                [AgentMutationKind.MoveAnimationSyncMarker] = new AgentMutationDraftDescriptor(AgentMutationKind.MoveAnimationSyncMarker, AgentMutationOutputKind.None, LowerMoveAnimationSyncMarker),
                [AgentMutationKind.DeleteAnimationSyncMarker] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteAnimationSyncMarker, AgentMutationOutputKind.None, LowerDeleteAnimationSyncMarker),
                [AgentMutationKind.DeleteTimelineClip] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTimelineClip, AgentMutationOutputKind.None, LowerDeleteTimelineClip),
                [AgentMutationKind.EnsureTreeClipBlackboardWrite] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureTreeClipBlackboardWrite, AgentMutationOutputKind.None, LowerEnsureTreeClipBlackboardWrite),
                [AgentMutationKind.DeleteTransition] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteTransition, AgentMutationOutputKind.None, LowerDeleteTransition),
                [AgentMutationKind.EnsureGameplayTag] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureGameplayTag, AgentMutationOutputKind.None, LowerEnsureGameplayTag),
                [AgentMutationKind.SetActionProfileGrantedTags] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileGrantedTags, AgentMutationOutputKind.None, LowerSetActionProfileGrantedTags),
                [AgentMutationKind.SetActionProfileCancelQuery] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileCancelQuery, AgentMutationOutputKind.None, LowerSetActionProfileCancelQuery),
                [AgentMutationKind.SetActionProfileTargetRequirement] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionProfileTargetRequirement, AgentMutationOutputKind.None, LowerSetActionProfileTargetRequirement),
                [AgentMutationKind.SetActionRequestTimingClass] = new AgentMutationDraftDescriptor(AgentMutationKind.SetActionRequestTimingClass, AgentMutationOutputKind.None, LowerSetActionRequestTimingClass),
                [AgentMutationKind.EnsureAIControllerDefinition] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIControllerDefinition, AgentMutationOutputKind.None, LowerEnsureAIControllerDefinition, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIControllerTree] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIControllerTree, AgentMutationOutputKind.None, LowerEnsureAIControllerTree, AgentMutationDomainMask.AIController),
                [AgentMutationKind.BindAIControllerAssets] = new AgentMutationDraftDescriptor(AgentMutationKind.BindAIControllerAssets, AgentMutationOutputKind.None, LowerBindAIControllerAssets, AgentMutationDomainMask.AIController),
                [AgentMutationKind.ConfigureAICandidates] = new AgentMutationDraftDescriptor(AgentMutationKind.ConfigureAICandidates, AgentMutationOutputKind.None, LowerConfigureAICandidates, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIBlackboardDeclaration] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIBlackboardDeclaration, AgentMutationOutputKind.BlackboardDeclaration, LowerEnsureAIBlackboardDeclaration, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAISharedNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAISharedNode, AgentMutationOutputKind.Node, LowerEnsureAISharedNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIObservationNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIObservationNode, AgentMutationOutputKind.Node, LowerEnsureAIObservationNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIMemoryNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIMemoryNode, AgentMutationOutputKind.Node, LowerEnsureAIMemoryNode, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIContinuousInput] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIContinuousInput, AgentMutationOutputKind.Node, LowerEnsureAIContinuousInput, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIActionTarget] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIActionTarget, AgentMutationOutputKind.Node, LowerEnsureAIActionTarget, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureAIActionRequest] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureAIActionRequest, AgentMutationOutputKind.Node, LowerEnsureAIActionRequest, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureBTConditionRule] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureBTConditionRule, AgentMutationOutputKind.FlowEdge, LowerEnsureBTConditionRule, AgentMutationDomainMask.AIController),
                [AgentMutationKind.EnsureGraphNode] = new AgentMutationDraftDescriptor(AgentMutationKind.EnsureGraphNode, AgentMutationOutputKind.Node, LowerEnsureGraphNode, AgentMutationDomainMask.Both),
                [AgentMutationKind.DeleteGraphNode] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteGraphNode, AgentMutationOutputKind.None, LowerDeleteGraphNode, AgentMutationDomainMask.Both),
                [AgentMutationKind.DeleteFlowEdge] = new AgentMutationDraftDescriptor(AgentMutationKind.DeleteFlowEdge, AgentMutationOutputKind.None, LowerDeleteFlowEdge, AgentMutationDomainMask.Both),
                [AgentMutationKind.DeletePropertyEdge] = new AgentMutationDraftDescriptor(AgentMutationKind.DeletePropertyEdge, AgentMutationOutputKind.None, LowerDeletePropertyEdge, AgentMutationDomainMask.Both),
                [AgentMutationKind.LinkFlow] = new AgentMutationDraftDescriptor(AgentMutationKind.LinkFlow, AgentMutationOutputKind.FlowEdge, LowerLinkFlow, AgentMutationDomainMask.Both),
                [AgentMutationKind.LinkProperty] = new AgentMutationDraftDescriptor(AgentMutationKind.LinkProperty, AgentMutationOutputKind.PropertyEdge, LowerLinkProperty, AgentMutationDomainMask.Both)
            };

        public static bool TryGet(AgentMutationKind kind, out AgentMutationDraftDescriptor descriptor)
        {
            descriptor = null;
            return s_Descriptors.TryGetValue(kind, out descriptor);
        }

        public static IReadOnlyCollection<AgentMutationKind> Kinds => s_Descriptors.Keys;

        static AgentMutation LowerEnsureStateMachine(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference parent = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existingOwner = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string existingGraphId = context.OptionalAuthoringId(operation.stateMachineGraphAuthoringId, "stateMachineGraphAuthoringId");
            string displayName = context.RequiredText(operation.displayName, operation.stateMachine, "displayName", "ensure_state_machine 缺少 displayName/stateMachine。");
            return context.IsValid
                ? new AgentEnsureStateMachineMutation(operation.id, context.Path, parent, existingOwner, existingGraphId, displayName, operation.lifecycleSlot, operation.position)
                : null;
        }

        static AgentMutation LowerEnsureState(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine", true);
            AgentStateTargetReference existingState = context.OptionalState(operation.stateAuthoringId, operation.statePlannedIdentity, "state", true);
            string stateName = context.RequiredText(operation.state, operation.displayName, "state", "ensure_state 缺少 state/displayName。");
            return context.IsValid
                ? new AgentEnsureStateMutation(operation.id, context.Path, stateMachine, existingState, stateName, operation.position)
                : null;
        }

        static AgentMutation LowerDeleteState(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            AgentStateTargetReference state = context.RequiredState(operation.stateAuthoringId, operation.statePlannedIdentity, "state");
            return context.IsValid
                ? new AgentDeleteStateMutation(operation.id, context.Path, stateMachine, state)
                : null;
        }

        static AgentMutation LowerEnsureTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = ReadEnsureTransitionIdentity(context, operation, "ensure_transition 缺少 stable edge identity。");
            return context.IsValid
                ? new AgentEnsureTransitionMutation(operation.id, AgentMutationKind.EnsureTransition, "ensure_transition", context.Path, stateMachine, from, to, edge, operation.transitionPriority, operation.position)
                : null;
        }

        static AgentMutation LowerRewireTransition(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            context.ReadTransition(
                operation,
                out AgentStateMachineTargetReference stateMachine,
                out AgentElementTargetReference from,
                out AgentElementTargetReference to);
            string edge = context.OptionalAuthoringId(
                context.RequiredText(
                    operation.targetElementAuthoringId,
                    string.Empty,
                    "targetElementAuthoringId",
                    "rewire_transition 缺少 stable edge identity。"),
                "targetElementAuthoringId");
            return context.IsValid
                ? new AgentRewireTransitionMutation(
                    operation.id,
                    context.Path,
                    stateMachine,
                    from,
                    to,
                    edge,
                    operation.transitionPriority)
                : null;
        }

        static AgentMutation LowerEnsureConditionRule(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            context.ReadTransition(operation, out AgentStateMachineTargetReference stateMachine, out AgentElementTargetReference from, out AgentElementTargetReference to);
            string edge = ReadEnsureTransitionIdentity(context, operation, "ensure_condition_rule 缺少 stable edge identity。");
            List<AgentConditionGroupMutation> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureConditionRuleMutation(operation.id, context.Path, stateMachine, from, to, edge, operation.transitionPriority, groups, operation.position)
                : null;
        }

        static string ReadEnsureTransitionIdentity(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation,
            string missingMessage)
        {
            string identity = context.RequiredText(
                operation.targetElementAuthoringId,
                string.Empty,
                "targetElementAuthoringId",
                missingMessage);
            return identity.StartsWith("local:", StringComparison.Ordinal)
                ? identity
                : context.OptionalAuthoringId(identity, "targetElementAuthoringId");
        }

        static AgentMutation LowerEnsureActionExitLifecycle(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference source = context.OptionalElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            AgentAssetReference actionContext = ReadActionContext(operation);
            List<AgentConditionGroupMutation> cancelConditionGroups = context.RequiredConditionGroups(operation.cancelConditionGroups, operation, "cancelConditionGroups");
            string cancelReason = context.RequiredText(operation.reason, string.Empty, "reason", "ensure_action_exit_lifecycle 必须显式提供 cancel reason。");
            string interruptReason = context.RequiredText(operation.interruptReason, string.Empty, "interruptReason", "ensure_action_exit_lifecycle 必须显式提供 interrupt reason。");
            string abortReason = context.RequiredText(operation.abortReason, string.Empty, "abortReason", "ensure_action_exit_lifecycle 必须显式提供 abort reason。");
            string completeReason = context.RequiredText(operation.completeReason, string.Empty, "completeReason", "ensure_action_exit_lifecycle 必须显式提供 complete reason。");
            return context.IsValid
                ? new AgentEnsureActionExitLifecycleMutation(
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

        static AgentMutation LowerDeleteStateBehaviorNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid
                ? new AgentDeleteStateBehaviorNodeMutation(operation.id, context.Path, target, element)
                : null;
        }

        static AgentMutation LowerEnsureStateBehaviorNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = First(operation.nodeType, "SequenceNode");
            string displayName = First(operation.displayName, nodeType);
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (!string.IsNullOrEmpty(operation.loopStopType))
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (!string.IsNullOrEmpty(operation.compareType))
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            if (string.Equals(nodeType, typeof(LocomotionInputMotionNode).FullName, StringComparison.Ordinal))
            {
                if (float.IsNaN(operation.moveSpeed) || float.IsInfinity(operation.moveSpeed) || operation.moveSpeed < 0f)
                    context.Error("moveSpeed", "move_speed_invalid", "locomotion-input-motion 的 moveSpeed 必须大于等于0且为有限值。");
                if (float.IsNaN(operation.turnSpeedDegrees) || float.IsInfinity(operation.turnSpeedDegrees) || operation.turnSpeedDegrees <= 0f)
                    context.Error("turnSpeedDegrees", "turn_speed_invalid", "locomotion-input-motion 的 turnSpeedDegrees 必须大于0且为有限值。");
            }
            return context.IsValid
                ? new AgentEnsureStateBehaviorNodeMutation(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    nodeType,
                    displayName,
                    operation.lifecycleSlot,
                    loopStopType,
                    compareType,
                    operation.moveSpeed,
                    operation.turnSpeedDegrees,
                    operation.cameraRelative,
                    operation.continuous,
                    operation.position)
                : null;
        }

        static AgentMutation LowerEnsureTimelineNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            if (!Enum.TryParse(operation.timelineOwnership, true, out AgentTimelineOwnership ownership) || !Enum.IsDefined(typeof(AgentTimelineOwnership), ownership))
            {
                context.Error("timelineOwnership", "timeline_ownership_invalid", $"Timeline ownership 无效：{operation.timelineOwnership}", "使用 Inline 或 Shared。");
                ownership = AgentTimelineOwnership.Inline;
            }
            string displayName = First(operation.displayName, First(operation.timeline, "Timeline"));
            var timelineAsset = new AgentAssetReference(operation.timeline, operation.timelineAssetPath, operation.timelineAssetGuid);
            var timelineTarget = new AgentTimelineTargetReference(operation.timelineAuthoringId, operation.trackAuthoringId, operation.clipAuthoringId);
            return context.IsValid
                ? new AgentEnsureTimelineNodeMutation(
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

        static AgentMutation LowerEnsureInlineTimeline(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentElementTargetReference timelineNode = context.RequiredElement(
                operation.targetElementAuthoringId,
                operation.targetPlannedIdentity,
                "timelineNode");
            string displayName = context.RequiredText(
                operation.displayName,
                operation.timeline,
                "displayName",
                "ensure_inline_timeline 缺少 Timeline 名称。");
            return context.IsValid
                ? new AgentEnsureInlineTimelineMutation(operation.id, context.Path, timelineNode, displayName)
                : null;
        }

        static AgentMutation LowerEnsureActionActivation(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ensure_action_activation 缺少 ActionProfile 引用。");
            string sourceRequest = First(operation.sourceInputRequestId, First(operation.inputId, operation.request));
            return context.IsValid
                ? new AgentEnsureActionActivationMutation(
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

        static AgentMutation LowerEnsureActionLifecycleTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            if (!Enum.TryParse(operation.lifecycleType, true, out ActionLifecycleTransitionType transitionType) ||
                !Enum.IsDefined(typeof(ActionLifecycleTransitionType), transitionType))
            {
                context.Error("lifecycleType", "lifecycle_type_invalid", $"未知的 lifecycleType：{operation.lifecycleType}");
                transitionType = default;
            }
            return context.IsValid
                ? new AgentEnsureActionLifecycleTransitionMutation(
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

        static AgentMutation LowerConfigureActionAdmission(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            AgentAssetReference actionProfile = ReadActionProfile(context, operation);
            return context.IsValid
                ? new AgentConfigureActionAdmissionMutation(operation.id, context.Path, graph, element, actionProfile)
                : null;
        }

        static AgentMutation LowerEnsureInputNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, operation.request, "inputId", "ensure_input_node 缺少 inputId。");
            return context.IsValid
                ? new AgentEnsureInputNodeMutation(
                    operation.id,
                    context.Path,
                    graph,
                    existing,
                    operation.nodeType,
                    First(operation.displayName, First(inputId, operation.nodeType)),
                    inputId,
                    operation.position)
                : null;
        }

        static AgentMutation LowerEnsureBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string declarationAuthoringId = context.OptionalAuthoringId(operation.declarationAuthoringId, "declarationAuthoringId");
            string key = context.RequiredText(operation.blackboardKey, string.Empty, "blackboardKey", "ensure_blackboard_declaration 缺少 blackboardKey。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            object defaultValue = ReadBlackboardDefault(context, operation.blackboardDefaultValue, valueType);
            bool valid = TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope) &
                         TryParseEnum(context, operation.blackboardLifetime, "blackboardLifetime", out PipelineBlackboardVariableLifetime lifetime) &
                         TryParseEnum(context, operation.blackboardAuthority, "blackboardAuthority", out PipelineBlackboardVariableAuthority authority) &
                         TryParseEnum(context, operation.blackboardSyncPolicy, "blackboardSyncPolicy", out PipelineBlackboardVariableSyncPolicy syncPolicy) &
                         TryParseEnum(context, First(operation.factProjection, "None"), "factProjection", out PipelineBlackboardFactProjectionKind projection);
            if (projection == PipelineBlackboardFactProjectionKind.ActionWindow && string.IsNullOrWhiteSpace(operation.windowType))
                context.Error("windowType", "window_type_missing", "ActionWindow declaration 必须显式提供 WindowType。");
            ValidateInputDerived(context, syncPolicy, operation.inputId);
            return context.IsValid && valid && valueType != null
                ? new AgentEnsureBlackboardDeclarationMutation(operation.id, context.Path, graph, declarationAuthoringId, key, valueType, defaultValue, scope, lifetime, authority, syncPolicy, operation.inputId, projection, operation.windowType, operation.windowId, operation.digest, operation.categoryPath)
                : null;
        }

        static AgentMutation LowerDeleteBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string declaration = context.OptionalAuthoringId(context.RequiredText(operation.declarationAuthoringId, string.Empty, "declarationAuthoringId", "delete_blackboard_declaration 缺少 declaration identity。"), "declarationAuthoringId");
            return context.IsValid ? new AgentDeleteBlackboardDeclarationMutation(operation.id, context.Path, graph, declaration) : null;
        }

        static AgentMutation LowerMoveBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference sourceGraph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentGraphTargetReference targetGraph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph");
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
                ? new AgentMoveBlackboardDeclarationMutation(operation.id, context.Path, sourceGraph, targetGraph, declaration, key, valueType, scope, lifetime, authority, syncPolicy, operation.inputId, projection, operation.windowType, operation.windowId, operation.digest, operation.categoryPath)
                : null;
        }

        static AgentMutation LowerEnsureBlackboardWrite(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            string displayName = First(operation.displayName, operation.blackboardBoolValue ? "Set Blackboard True" : "Set Blackboard False");
            return context.IsValid
                ? new AgentEnsureBlackboardWriteMutation(operation.id, context.Path, graph, operation.targetElementAuthoringId, declaration, operation.blackboardBoolValue, displayName, operation.position)
                : null;
        }

        static AgentMutation LowerEnsureTimelineTreeClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_timeline_tree_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            if (operation.endFrame <= operation.startFrame)
                context.Error("endFrame", "timeline_clip_range_invalid", "TreeClip endFrame 必须大于 startFrame。");
            AgentPlannedIdentityReference output = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTimelineTreeClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame, First(operation.timelinePhase, "Decision")) : null;
        }

        static AgentMutation LowerEnsureMotionCurveTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_motion_curve_track", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, default, string.Empty, default);
            return context.IsValid
                ? new AgentEnsureMotionCurveTrackMutation(operation.id, context.Path, target, First(operation.displayName, "Motion Curve"))
                : null;
        }

        static AgentMutation LowerEnsureMotionCurveClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_motion_curve_clip", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "motion_curve_track_reference_invalid", "ensure_motion_curve_clip 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            if (operation.endFrame <= operation.startFrame)
                context.Error("endFrame", "motion_curve_clip_range_invalid", "MotionCurveClip endFrame 必须大于 startFrame。");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, trackOutput, clip, default);
            return context.IsValid
                ? new AgentEnsureMotionCurveClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame)
                : null;
        }

        static AgentMutation LowerConfigureMotionCurveClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_motion_curve_clip", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "motion_curve_clip_reference_invalid", "configure_motion_curve_clip 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            string curveId = context.RequiredText(operation.curveId, string.Empty, "curveId", "configure_motion_curve_clip 缺少 curveId。");
            if (operation.curveEndFrame <= operation.startFrame || operation.curveEndFrame > operation.endFrame)
                context.Error("curveEndFrame", "motion_curve_end_frame_invalid", "MotionCurveClip 必须满足 startFrame < curveEndFrame <= endFrame。");
            if (!Enum.TryParse(operation.motionSpace, true, out TimelineMotionContributionSpace space) || !Enum.IsDefined(typeof(TimelineMotionContributionSpace), space))
                context.Error("motionSpace", "motion_curve_space_invalid", $"MotionCurve space 无效：{operation.motionSpace}");
            if (!Enum.TryParse(operation.motionChannel, true, out TimelineMotionChannel channel) || !Enum.IsDefined(typeof(TimelineMotionChannel), channel))
                context.Error("motionChannel", "motion_curve_channel_invalid", $"MotionCurve channel 无效：{operation.motionChannel}");
            if (!Enum.TryParse(operation.motionBlendMode, true, out TimelineMotionBlendMode blendMode) || !Enum.IsDefined(typeof(TimelineMotionBlendMode), blendMode))
                context.Error("motionBlendMode", "motion_curve_blend_mode_invalid", $"MotionCurve blend mode 无效：{operation.motionBlendMode}");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput);
            return context.IsValid
                ? new AgentConfigureMotionCurveClipMutation(
                    operation.id,
                    context.Path,
                    target,
                    curveId,
                    operation.curveEndFrame,
                    space,
                    channel,
                    blendMode,
                    operation.motionPriority,
                    operation.consumeLowerChannels)
                : null;
        }

        static void ReadTimelineReference(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation,
            string operationName,
            out string timeline,
            out AgentPlannedIdentityReference timelineOutput)
        {
            timeline = context.OptionalAuthoringId(operation.timelineAuthoringId, "timelineAuthoringId");
            timelineOutput = context.OptionalPlannedIdentity(operation.timelinePlannedIdentity, "timelinePlannedIdentity", AgentMutationOutputKind.Timeline);
            if (string.IsNullOrEmpty(timeline) == !timelineOutput.IsValid)
                context.Error("timeline", "timeline_reference_invalid", $"{operationName} 必须且只能提供 timelineAuthoringId 或 timelinePlannedIdentity。");
        }

        static AgentMutation LowerEnsureMotionWarpTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_track 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            return context.IsValid
                ? new AgentEnsureMotionWarpTrackMutation(operation.id, context.Path, timeline, track, First(operation.displayName, "Motion Warp"))
                : null;
        }

        static AgentMutation LowerEnsureMotionWarpClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "motion_warp_track_reference_invalid", "ensure_motion_warp_clip 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。", "引用已有 MotionWarpTrack 或前序 ensure_motion_warp_track output。");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, track, trackOutput, clip, default);
            return context.IsValid ? new AgentEnsureMotionWarpClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame) : null;
        }

        static AgentMutation LowerConfigureMotionWarpSource(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerMotionWarpClipTarget(context, operation);
            string source = context.OptionalAuthoringId(
                context.RequiredText(operation.sourceMotionClipAuthoringId, string.Empty, "sourceMotionClipAuthoringId", "configure_motion_warp_source 缺少 source MotionCurve identity。"),
                "sourceMotionClipAuthoringId");
            return context.IsValid ? new AgentConfigureMotionWarpSourceMutation(operation.id, context.Path, target, source) : null;
        }

        static AgentMutation LowerConfigureMotionWarpParameters(AgentMutationPlanningContext context, AgentMutationDraft operation)
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
                ? new AgentConfigureMotionWarpParametersMutation(
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

        static AgentTimelineTargetReference LowerMotionWarpClipTarget(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.kind} 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "motion_warp_clip_reference_invalid", $"{operation.kind} 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            return new AgentTimelineTargetReference(timeline, track, default, clip, clipOutput);
        }

        static AnimationCurve LowerAnimationCurve(
            AgentMutationPlanningContext context,
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

        static AgentMutation LowerDeleteTimelineClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "delete_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "delete_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            return context.IsValid ? new AgentDeleteTimelineClipMutation(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip)) : null;
        }

        static AgentMutation LowerDeleteTimelineTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "delete_timeline_track 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "delete_timeline_track 缺少 Track identity。"), "trackAuthoringId");
            return context.IsValid ? new AgentDeleteTimelineTrackMutation(operation.id, context.Path, timeline, track) : null;
        }

        static AgentMutation LowerMoveTimelineClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "move_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "move_timeline_clip 缺少 Track identity。"), "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "move_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            if (operation.frameOffset == 0)
                context.Error("frameOffset", "timeline_clip_offset_zero", "move_timeline_clip 的 frameOffset 不能为 0。");
            return context.IsValid
                ? new AgentMoveTimelineClipMutation(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip), operation.frameOffset)
                : null;
        }

        static AgentMutation LowerConfigureTimelineClipEase(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_timeline_clip_ease", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "timeline_clip_reference_invalid", "configure_timeline_clip_ease 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            if (operation.selfEaseInFrame < 0)
                context.Error("selfEaseInFrame", "timeline_clip_ease_negative", "selfEaseInFrame 不能小于 0。");
            if (operation.selfEaseOutFrame < 0)
                context.Error("selfEaseOutFrame", "timeline_clip_ease_negative", "selfEaseOutFrame 不能小于 0。");
            return context.IsValid
                ? new AgentConfigureTimelineClipEaseMutation(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput),
                    operation.selfEaseInFrame,
                    operation.selfEaseOutFrame)
                : null;
        }

        static AgentMutation LowerConfigureTimelineCurveChannel(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_timeline_curve_channel", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "timeline_clip_reference_invalid", "configure_timeline_curve_channel 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            string channelId = context.RequiredText(operation.curveChannelId, string.Empty, "curveChannelId", "configure_timeline_curve_channel 缺少registered ChannelId。");
            if (!TimelineCurveChannelCatalog.TryGet(channelId, out TimelineCurveChannelDescriptor descriptor))
                context.Error("curveChannelId", "timeline_curve_channel_unknown", $"未知 Timeline Curve ChannelId：{channelId}");
            AnimationCurve curve = LowerTimelineCurvePayload(context, operation.curve, "curve");
            return context.IsValid
                ? new AgentConfigureTimelineCurveChannelMutation(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput),
                    descriptor.ChannelId,
                    curve)
                : null;
        }

        static AgentMutation LowerEnsureConditionValueNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = context.RequiredText(operation.nodeType, string.Empty, "nodeType", "ensure_condition_value_node缺少nodeType。");
            bool configurationValid = TryParseEnum(
                context,
                operation.conditionValueConfiguration,
                "conditionValueConfiguration",
                out AgentConditionValueNodeConfigurationKind configuration);
            AgentAuthoringReference declaration = default;
            StateExitCause exitCause = default;
            AgentAssetReference actionContext = default;
            string windowType = string.Empty;
            AgentAssetReference actionProfile = default;
            AgentAuthoringReference targetSnapshotDeclaration = default;
            if (configurationValid)
            {
                switch (configuration)
                {
                    case AgentConditionValueNodeConfigurationKind.None:
                        break;
                    case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration:
                        declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "blackboardDeclaration");
                        break;
                    case AgentConditionValueNodeConfigurationKind.StateExitCause:
                        configurationValid = TryParseEnum(context, operation.stateExitCause, "stateExitCause", out exitCause);
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionContext:
                        actionContext = ReadActionContext(operation);
                        if (string.IsNullOrEmpty(actionContext.LogicalId) &&
                            string.IsNullOrEmpty(actionContext.AssetPath) &&
                            string.IsNullOrEmpty(actionContext.AssetGuid))
                            context.Error("actionContext", "action_context_required", "Action Context identity缺失。");
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionWindow:
                        windowType = context.RequiredText(operation.windowType, string.Empty, "windowType", "Action Window type缺失。");
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionAdmission:
                        actionProfile = ReadActionProfile(context, operation);
                        if (!string.IsNullOrEmpty(operation.targetSnapshotBlackboardDeclarationId) ||
                            !string.IsNullOrEmpty(operation.targetSnapshotBlackboardDeclarationPlannedIdentity))
                        {
                            targetSnapshotDeclaration = context.RequiredDeclaration(
                                operation.targetSnapshotBlackboardDeclarationId,
                                operation.targetSnapshotBlackboardDeclarationPlannedIdentity,
                                "targetSnapshotBlackboardDeclaration");
                        }
                        break;
                    default:
                        context.Error("conditionValueConfiguration", "condition_value_configuration_invalid", "未知Condition Value配置类型。");
                        break;
                }
            }
            return context.IsValid && configurationValid
                ? new AgentEnsureConditionValueNodeMutation(
                    operation.id,
                    context.Path,
                    graph,
                    existing,
                    nodeType,
                    First(operation.displayName, nodeType),
                    operation.position,
                    configuration,
                    declaration,
                    exitCause,
                    actionContext,
                    windowType,
                    actionProfile,
                    targetSnapshotDeclaration)
                : null;
        }

        static AnimationCurve LowerTimelineCurvePayload(
            AgentMutationPlanningContext context,
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

        static AgentMutation LowerConfigureAnimationTrackMarkerSync(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
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
                ? new AgentConfigureAnimationTrackMarkerSyncMutation(operation.id, context.Path, target, mode, groupId, topology, syncRole)
                : null;
        }

        static AgentMutation LowerConfigureAnimationTrackChannel(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
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
                ? new AgentConfigureAnimationTrackChannelMutation(operation.id, context.Path, target, animationChannelId)
                : null;
        }

        static AgentMutation LowerEnsureAnimationSyncMarker(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            string markerAuthoringId = context.OptionalAuthoringId(operation.markerAuthoringId, "markerAuthoringId");
            string markerId = context.RequiredText(operation.markerId, string.Empty, "markerId", "ensure_animation_sync_marker 缺少 MarkerId。");
            if (!string.Equals(markerId, markerId.Trim(), StringComparison.Ordinal))
                context.Error("markerId", "animation_marker_id_invalid", "MarkerId 不能包含首尾空白。");
            if (operation.markerFrame < 0)
                context.Error("markerFrame", "animation_marker_frame_negative", "Marker frame 不能小于 0。");
            return context.IsValid
                ? new AgentEnsureAnimationSyncMarkerMutation(operation.id, context.Path, target, markerAuthoringId, markerId, operation.markerFrame)
                : null;
        }

        static AgentMutation LowerMoveAnimationSyncMarker(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            AgentAuthoringReference marker = context.RequiredMarker(operation.markerAuthoringId, operation.markerPlannedIdentity, "marker");
            if (operation.markerFrame < 0)
                context.Error("markerFrame", "animation_marker_frame_negative", "Marker frame 不能小于 0。");
            return context.IsValid
                ? new AgentMoveAnimationSyncMarkerMutation(operation.id, context.Path, target, marker, operation.markerFrame)
                : null;
        }

        static AgentMutation LowerDeleteAnimationSyncMarker(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            AgentAuthoringReference marker = context.RequiredMarker(operation.markerAuthoringId, operation.markerPlannedIdentity, "marker");
            return context.IsValid
                ? new AgentDeleteAnimationSyncMarkerMutation(operation.id, context.Path, target, marker)
                : null;
        }

        static AgentTimelineTargetReference LowerAnimationTrackTarget(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(
                context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.kind} 缺少 Timeline identity。"),
                "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "animation_track_reference_invalid", $"{operation.kind} 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。");
            return new AgentTimelineTargetReference(timeline, track, trackOutput, string.Empty, default);
        }

        static AgentMutation LowerEnsureTreeClipBlackboardWrite(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_tree_clip_blackboard_write 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            AgentPlannedIdentityReference output = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(operation.clipAuthoringId) && !output.IsValid)
                context.Error("clipAuthoringId", "clip_identity_missing", "ensure_tree_clip_blackboard_write 必须使用 stable Clip identity 或前序 TimelineClip output。");
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTreeClipBlackboardWriteMutation(operation.id, context.Path, target, declaration) : null;
        }

        static AgentMutation LowerDeleteTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateMachineTargetReference stateMachine = context.RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_transition 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteTransitionMutation(operation.id, context.Path, stateMachine, edge) : null;
        }

        static AgentMutation LowerEnsureGameplayTag(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string tag = context.RequiredText(operation.gameplayTag, string.Empty, "gameplayTag", "ensure_gameplay_tag 缺少 tag id。");
            return context.IsValid ? new AgentEnsureGameplayTagMutation(operation.id, context.Path, tag, operation.parentGameplayTag, First(operation.displayName, tag), operation.debugCategory) : null;
        }

        static AgentMutation LowerSetActionProfileGrantedTags(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            return context.IsValid ? new AgentSetActionProfileGrantedTagsMutation(operation.id, context.Path, profile, ReadTags(context, operation.grantedTags, "grantedTags")) : null;
        }

        static AgentMutation LowerSetActionProfileCancelQuery(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            List<GameplayTagId> all = ReadTags(context, operation.queryAll, "queryAll");
            List<GameplayTagId> any = ReadTags(context, operation.queryAny, "queryAny");
            List<GameplayTagId> none = ReadTags(context, operation.queryNone, "queryNone");
            return context.IsValid ? new AgentSetActionProfileCancelQueryMutation(operation.id, context.Path, profile, all, any, none) : null;
        }

        static AgentMutation LowerSetActionProfileTargetRequirement(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            TryParseEnum(context, operation.targetRequirement, "targetRequirement", out ActionTargetRequirement requirement);
            return context.IsValid
                ? new AgentSetActionProfileTargetRequirementMutation(operation.id, context.Path, profile, requirement)
                : null;
        }

        static AgentMutation LowerSetActionRequestTimingClass(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
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
                ? new AgentSetActionRequestTimingClassMutation(operation.id, context.Path, requestId, timingClass)
                : null;
        }

        static AgentMutation LowerEnsureAIControllerDefinition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string controllerId = context.RequiredText(operation.controllerId, string.Empty, "controllerId", "ensure_ai_controller_definition 缺少 ControllerId。");
            if (!string.Equals(controllerId, controllerId.Trim(), StringComparison.Ordinal))
                context.Error("controllerId", "ai_controller_id_invalid", "ControllerId 不能包含首尾空白。");
            return context.IsValid ? new AgentEnsureAIControllerDefinitionMutation(operation.id, context.Path, controllerId) : null;
        }

        static AgentMutation LowerEnsureAIControllerTree(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string path = context.RequiredText(operation.rootTreeAssetPath, string.Empty, "rootTreeAssetPath", "ensure_ai_controller_tree 缺少精确的 RootTree 资产路径。");
            return context.IsValid ? new AgentEnsureAIControllerTreeMutation(operation.id, context.Path, path) : null;
        }

        static AgentMutation LowerBindAIControllerAssets(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            if (string.IsNullOrEmpty(operation.controlledCharacterAssetPath) && string.IsNullOrEmpty(operation.controlledCharacterAssetGuid))
                context.Error("controlledCharacter", "controlled_character_reference_missing", "bind_ai_controller_assets 缺少 Character Definition 资产引用。");
            if (string.IsNullOrEmpty(operation.perceptionProfileAssetPath) && string.IsNullOrEmpty(operation.perceptionProfileAssetGuid))
                context.Error("perceptionProfile", "perception_profile_reference_missing", "bind_ai_controller_assets 缺少 Perception Profile 资产引用。");
            return context.IsValid
                ? new AgentBindAIControllerAssetsMutation(
                    operation.id,
                    context.Path,
                    new AgentAssetReference(string.Empty, operation.controlledCharacterAssetPath, operation.controlledCharacterAssetGuid),
                    new AgentAssetReference(string.Empty, operation.perceptionProfileAssetPath, operation.perceptionProfileAssetGuid))
                : null;
        }

        static AgentMutation LowerConfigureAICandidates(AgentMutationPlanningContext context, AgentMutationDraft operation)
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
            return context.IsValid ? new AgentConfigureAICandidatesMutation(operation.id, context.Path, ordering, values) : null;
        }

        static AgentMutation LowerEnsureAIBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration", true);
            string key = context.RequiredText(operation.blackboardKey, operation.displayName, "blackboardKey", "ensure_ai_blackboard_declaration 缺少 Blackboard key。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope);
            if (scope != PipelineBlackboardVariableScope.AIController && scope != PipelineBlackboardVariableScope.AITick && scope != PipelineBlackboardVariableScope.Graph)
                context.Error("blackboardScope", "ai_blackboard_scope_invalid", $"AI Blackboard 不允许 scope：{scope}");
            return context.IsValid
                ? new AgentEnsureAIBlackboardDeclarationMutation(operation.id, context.Path, graph, existing, key, valueType, scope, AIBlackboardDefault(operation, valueType))
                : null;
        }

        static AgentMutation LowerEnsureAISharedNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAISharedNodeKind nodeKind);
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (nodeKind == AgentAISharedNodeKind.Loop)
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (nodeKind == AgentAISharedNodeKind.Compare)
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            return context.IsValid
                ? new AgentEnsureAISharedNodeMutation(operation.id, context.Path, graph, existing, nodeKind, loopStopType, compareType, operation.position)
                : null;
        }

        static AgentMutation LowerEnsureAIObservationNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIObservationNodeKind kind);
            return context.IsValid ? new AgentEnsureAIObservationNodeMutation(operation.id, context.Path, graph, existing, kind, operation.position) : null;
        }

        static AgentMutation LowerEnsureAIMemoryNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            TryParseEnum(context, operation.aiNodeKind, "aiNodeKind", out AgentAIMemoryNodeKind nodeKind);
            TryParseEnum(context, operation.aiMemoryValueKind, "aiMemoryValueKind", out AIMemoryValueKind valueKind);
            return context.IsValid ? new AgentEnsureAIMemoryNodeMutation(operation.id, context.Path, graph, existing, declaration, nodeKind, valueKind, operation.position) : null;
        }

        static AgentMutation LowerEnsureAIContinuousInput(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_continuous_input 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIContinuousInputMutation(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        static AgentMutation LowerEnsureAIActionTarget(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, string.Empty, "inputId", "ensure_ai_action_target 缺少 InputId。");
            return context.IsValid ? new AgentEnsureAIActionTargetMutation(operation.id, context.Path, graph, existing, inputId, operation.position) : null;
        }

        static AgentMutation LowerEnsureAIActionRequest(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string requestId = context.RequiredText(operation.request, string.Empty, "request", "ensure_ai_action_request 缺少 RequestId。");
            TryParseEnum(context, operation.aiRequestRepeatPolicy, "aiRequestRepeatPolicy", out AIRequestRepeatPolicy repeatPolicy);
            if (operation.requestBufferSeconds < 0f)
                context.Error("requestBufferSeconds", "ai_request_buffer_invalid", "Action Request buffer seconds 不能小于 0。");
            return context.IsValid
                ? new AgentEnsureAIActionRequestMutation(operation.id, context.Path, graph, existing, requestId, operation.requestBufferSeconds, operation.requestPriority, repeatPolicy, operation.position)
                : null;
        }

        static AgentMutation LowerDeleteFlowEdge(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_flow_edge 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteFlowEdgeMutation(operation.id, context.Path, graph, edge) : null;
        }

        static AgentMutation LowerEnsureGraphNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = context.RequiredText(operation.nodeType, string.Empty, "nodeType", "ensure_graph_node缺少Node类型。");
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (!string.IsNullOrEmpty(operation.loopStopType))
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (!string.IsNullOrEmpty(operation.compareType))
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            return context.IsValid
                ? new AgentEnsureGraphNodeMutation(operation.id, context.Path, graph, existing, nodeType, operation.displayName, loopStopType, compareType, operation.position)
                : null;
        }

        static AgentMutation LowerDeleteGraphNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid ? new AgentDeleteGraphNodeMutation(operation.id, context.Path, graph, element) : null;
        }

        static AgentMutation LowerDeletePropertyEdge(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_property_edge 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeletePropertyEdgeMutation(operation.id, context.Path, graph, edge) : null;
        }

        static AgentMutation LowerLinkFlow(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid
                ? new AgentLinkFlowMutation(operation.id, context.Path, graph, source, target, First(operation.startPort, "Output"), First(operation.endPort, "Input"), operation.flowEdgeAuthoringId)
                : null;
        }

        static AgentMutation LowerLinkProperty(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            string startPort = context.RequiredText(operation.startPropertyPort, string.Empty, "startPropertyPort", "link_property 缺少 startPropertyPort。");
            string endPort = context.RequiredText(operation.endPropertyPort, string.Empty, "endPropertyPort", "link_property 缺少 endPropertyPort。");
            return context.IsValid
                ? new AgentLinkPropertyMutation(operation.id, context.Path, graph, source, target, startPort, endPort, operation.flowEdgeAuthoringId)
                : null;
        }

        static AgentMutation LowerEnsureBTConditionRule(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentFlowEdgeTargetReference edge = context.RequiredFlowEdge(operation.flowEdgeAuthoringId, operation.flowEdgePlannedIdentity, "flowEdge");
            TryParseEnum(context, operation.abortPolicy, "abortPolicy", out BTAbortPolicy abortPolicy);
            List<AgentConditionGroupMutation> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureBTConditionRuleMutation(operation.id, context.Path, graph, edge, abortPolicy, groups)
                : null;
        }

        static AgentAssetReference ReadActionContext(AgentMutationDraft operation)
        {
            return new AgentAssetReference(operation.actionContext, operation.actionContextAssetPath, operation.actionContextAssetGuid);
        }

        static AgentAssetReference ReadActionProfile(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ActionProfile identity 缺失。");
            return new AgentAssetReference(actionProfile, string.Empty, string.Empty);
        }

        static List<GameplayTagId> ReadTags(AgentMutationPlanningContext context, List<string> values, string field)
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

        static Type ParseBlackboardValueType(AgentMutationPlanningContext context, string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "bool": case "boolean": case "system.boolean": return typeof(bool);
                case "int": case "int32": case "system.int32": return typeof(int);
                case "float": case "single": case "system.single": return typeof(float);
                case "string": case "system.string": return typeof(string);
                case "vector2": case "unityengine.vector2": return typeof(Vector2);
                case "vector3": case "unityengine.vector3": return typeof(Vector3);
                case "actiontargetsnapshot": case "action_target_snapshot": case "thirdpersoncharacter.actionsystem.actiontargetsnapshot": return typeof(ActionTargetSnapshot);
                case "aiactorid": case "ai_actor_id": case "thirdpersoncharacter.ai.aiactoridvalue": return typeof(AIActorIdValue);
                case "aiactiontargetsnapshot": case "ai_action_target_snapshot": case "thirdpersoncharacter.ai.aiactiontargetsnapshotvalue": return typeof(AIActionTargetSnapshotValue);
                default:
                    context.Error("blackboardValueType", "blackboard_value_type_invalid", $"不支持的 Blackboard value type：{value}");
                    return null;
            }
        }

        static object AIBlackboardDefault(AgentMutationDraft operation, Type valueType)
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

        static object ReadBlackboardDefault(
            AgentMutationPlanningContext context,
            Newtonsoft.Json.Linq.JToken token,
            Type valueType)
        {
            if (valueType == null)
                return null;
            if (token == null ||
                token.Type == Newtonsoft.Json.Linq.JTokenType.Null && valueType.IsValueType)
            {
                context.Error("blackboardDefaultValue", "blackboard_default_missing", "Blackboard declaration 必须显式声明与ValueType一致的defaultValue。");
                return null;
            }
            try
            {
                return token.Type == Newtonsoft.Json.Linq.JTokenType.Null
                    ? null
                    : token.ToObject(valueType);
            }
            catch (Exception exception)
            {
                context.Error("blackboardDefaultValue", "blackboard_default_invalid", $"Blackboard defaultValue无效：{exception.Message}");
                return null;
            }
        }

        static void ValidateInputDerived(
            AgentMutationPlanningContext context,
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

        static bool TryParseEnum<T>(AgentMutationPlanningContext context, string value, string field, out T result) where T : struct
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
    public enum AgentMutationDomainMask
    {
        CharacterController = 1,
        AIController = 2,
        Both = CharacterController | AIController
    }

    public sealed class AgentMutationDraftDescriptor
    {
        readonly Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> m_Lower;

        internal AgentMutationDraftDescriptor(
            AgentMutationKind kind,
            AgentMutationOutputKind outputKind,
            Func<AgentMutationPlanningContext, AgentMutationDraft, AgentMutation> lower,
            AgentMutationDomainMask domains = AgentMutationDomainMask.CharacterController)
        {
            Kind = kind;
            OutputKind = outputKind;
            m_Lower = lower;
            Domains = domains;
        }

        public AgentMutationKind Kind { get; }
        public AgentMutationOutputKind OutputKind { get; }
        public AgentMutationDomainMask Domains { get; }
        public bool Allows(string domain)
        {
            return string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal)
                ? (Domains & AgentMutationDomainMask.CharacterController) != 0
                : string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal) &&
                  (Domains & AgentMutationDomainMask.AIController) != 0;
        }
        internal AgentMutation Lower(AgentMutationPlanningContext context, AgentMutationDraft operation) => m_Lower(context, operation);
    }

    public sealed class AgentMutationPlanningContext
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
        readonly string m_MutationId;
        readonly IReadOnlyDictionary<string, AgentPlannedIdentitySymbol> m_PlannedIdentities;
        int m_ErrorCount;

        internal AgentMutationPlanningContext(
            AgentCompileReport report,
            string path,
            string plannedIdentity,
            IReadOnlyDictionary<string, AgentPlannedIdentitySymbol> plannedIdentities)
        {
            m_Report = report;
            Path = path;
            m_MutationId = plannedIdentity;
            m_PlannedIdentities = plannedIdentities;
        }

        public string Path { get; }
        public bool HasErrors => m_ErrorCount > 0;
        public bool IsValid => !HasErrors;

        public void ReadTransition(
            AgentMutationDraft operation,
            out AgentStateMachineTargetReference stateMachine,
            out AgentElementTargetReference from,
            out AgentElementTargetReference to)
        {
            stateMachine = RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine");
            from = RequiredElement(operation.fromElementAuthoringId, operation.fromPlannedIdentity, "fromElement");
            to = RequiredElement(operation.toElementAuthoringId, operation.toPlannedIdentity, "toElement");
        }

        public AgentStateBehaviorTargetReference RequiredStateBehaviorTarget(AgentMutationDraft operation)
        {
            bool hasDirect = HasValue(operation.targetGraphAuthoringId) || HasValue(operation.targetGraphPlannedIdentity);
            bool hasState = HasValue(operation.stateMachineGraphAuthoringId) || HasValue(operation.stateMachinePlannedIdentity) ||
                            HasValue(operation.stateAuthoringId) || HasValue(operation.statePlannedIdentity);
            if (hasDirect && hasState)
            {
                Error(string.Empty, "state_behavior_target_ambiguous", "State behavior target 不能同时使用 direct graph 与 StateMachine/State reference。");
                return default;
            }
            if (hasDirect)
                return new AgentStateBehaviorTargetReference(RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph"), default, default);
            if (!hasState)
            {
                Error(string.Empty, "state_behavior_identity_missing", "Operation 必须用 target graph identity，或 StateMachine + State identity 指定 State body。");
                return default;
            }
            return new AgentStateBehaviorTargetReference(
                default,
                RequiredStateMachine(operation.stateMachineGraphAuthoringId, operation.stateMachinePlannedIdentity, "stateMachine"),
                RequiredState(operation.stateAuthoringId, operation.statePlannedIdentity, "state"));
        }

        public AgentGraphTargetReference RequiredGraph(string authoringId, string plannedIdentity, string label)
        {
            return new AgentGraphTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State));
        }

        public AgentStateMachineTargetReference RequiredStateMachine(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentStateMachineTargetReference(RequiredReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.StateMachine));
        }

        public AgentStateTargetReference RequiredState(string authoringId, string plannedIdentity, string label)
        {
            return new AgentStateTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.State));
        }

        public AgentStateTargetReference OptionalState(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentStateTargetReference(OptionalReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.State));
        }

        public AgentElementTargetReference RequiredElement(string authoringId, string plannedIdentity, string label)
        {
            return new AgentElementTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node));
        }

        public AgentElementTargetReference OptionalElement(string authoringId, string plannedIdentity, string label, bool allowSelf = false)
        {
            return new AgentElementTargetReference(OptionalReference(authoringId, plannedIdentity, label, allowSelf, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node));
        }

        public AgentFlowEdgeTargetReference RequiredFlowEdge(string authoringId, string plannedIdentity, string label)
        {
            return new AgentFlowEdgeTargetReference(RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.FlowEdge));
        }

        public AgentAuthoringReference RequiredDeclaration(string authoringId, string plannedIdentity, string label)
        {
            return RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.BlackboardDeclaration);
        }

        public AgentAuthoringReference RequiredMarker(string authoringId, string plannedIdentity, string label)
        {
            return RequiredReference(authoringId, plannedIdentity, label, AgentMutationOutputKind.TimelineMarker);
        }

        public AgentPlannedIdentityReference OptionalPlannedIdentity(string plannedIdentity, string label, AgentMutationOutputKind expectedKind)
        {
            AgentAuthoringReference reference = OptionalReference(string.Empty, plannedIdentity, label, false, expectedKind);
            return reference.PlannedIdentity;
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

        public List<AgentConditionGroupMutation> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentMutationDraft operation)
        {
            return RequiredConditionGroups(groups, operation, "conditionGroups");
        }

        public List<AgentConditionGroupMutation> RequiredConditionGroups(
            List<AgentConditionGroup> groups,
            AgentMutationDraft operation,
            string field)
        {
            var result = new List<AgentConditionGroupMutation>();
            if (groups == null || groups.Count == 0)
            {
                Error(field, "condition_groups_empty", $"{operation.kind} 必须包含至少一个条件组。");
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
                List<AgentConditionTermMutation> terms = ConditionTerms(group.terms, operation, $"{field}[{i}].terms", false);
                if (terms.Count > 0)
                    result.Add(new AgentConditionGroupMutation(terms));
            }
            return result;
        }

        public List<AgentConditionTermMutation> ConditionTerms(
            List<AgentConditionTerm> terms,
            AgentMutationDraft operation,
            string field,
            bool allowEmpty)
        {
            var result = new List<AgentConditionTermMutation>();
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
                if ((kind == AgentConditionTermKind.BlackboardBool ||
                     kind == AgentConditionTermKind.TurnFacingAngle) &&
                    !HasValue(blackboardKey))
                {
                    Error($"{termField}.blackboardKey", "blackboard_key_missing", $"{source.kind} condition 缺少 blackboardKey。");
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
                result.Add(new AgentConditionTermMutation(
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

        public void ValidateOwnedReferences(AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureStateMachineMutation stateMachine:
                    ValidateOwnedReference(stateMachine.ExistingOwner.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureStateMutation state:
                    ValidateOwnedReference(state.ExistingState.Value, command.OwnerScope, "state");
                    break;
                case AgentDeleteStateMutation deleteState:
                    ValidateOwnedReference(deleteState.State.Value, command.OwnerScope, "state");
                    break;
                case AgentEnsureTransitionMutation transition:
                    ValidateOwnedReference(transition.From.Value, command.OwnerScope, "fromElement");
                    ValidateOwnedReference(transition.To.Value, command.OwnerScope, "toElement");
                    break;
                case AgentEnsureActionExitLifecycleMutation actionExit:
                    ValidateOwnedReference(actionExit.Source.Value, command.OwnerScope, "sourceElement");
                    break;
                case AgentDeleteStateBehaviorNodeMutation delete:
                    ValidateOwnedReference(delete.Element.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureStateBehaviorNodeMutation node:
                    ValidateOwnedReference(node.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureTimelineNodeMutation timeline:
                    ValidateOwnedReference(timeline.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionActivationMutation activation:
                    ValidateOwnedReference(activation.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureActionLifecycleTransitionMutation lifecycle:
                    ValidateOwnedReference(lifecycle.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureInputNodeMutation input:
                    ValidateOwnedReference(input.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureConditionValueNodeMutation conditionValue:
                    ValidateOwnedReference(conditionValue.ExistingElement.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentGraphLinkMutation link:
                    ValidateOwnedReference(link.Source.Value, command.OwnerScope, "sourceElement");
                    ValidateOwnedReference(link.Target.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIBlackboardDeclarationMutation declaration:
                    ValidateOwnedReference(declaration.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(declaration.ExistingDeclaration.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAISharedNodeMutation shared:
                    ValidateOwnedReference(shared.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(shared.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIObservationNodeMutation observation:
                    ValidateOwnedReference(observation.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(observation.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIMemoryNodeMutation memory:
                    ValidateOwnedReference(memory.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(memory.ExistingNode.Value, command.OwnerScope, "targetElement");
                    ValidateOwnedReference(memory.Declaration, command.OwnerScope, "declaration");
                    break;
                case AgentEnsureAIContinuousInputMutation continuousInput:
                    ValidateOwnedReference(continuousInput.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(continuousInput.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionTargetMutation actionTarget:
                    ValidateOwnedReference(actionTarget.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionTarget.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
                case AgentEnsureAIActionRequestMutation actionRequest:
                    ValidateOwnedReference(actionRequest.Graph.Value, command.OwnerScope, "graph");
                    ValidateOwnedReference(actionRequest.ExistingNode.Value, command.OwnerScope, "targetElement");
                    break;
            }
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string plannedIdentity,
            string label,
            params AgentMutationOutputKind[] expectedKinds)
        {
            return RequiredReference(authoringId, plannedIdentity, label, false, expectedKinds);
        }

        AgentAuthoringReference RequiredReference(
            string authoringId,
            string plannedIdentity,
            string label,
            bool allowSelf,
            params AgentMutationOutputKind[] expectedKinds)
        {
            AgentAuthoringReference reference = OptionalReference(authoringId, plannedIdentity, label, allowSelf, expectedKinds);
            if (!reference.IsValid && !(allowSelf && IsSelfIdentity(plannedIdentity)))
                Error(label, $"{label}_identity_missing", $"内部Mutation缺少{label} stable/local identity。");
            return reference;
        }

        AgentAuthoringReference OptionalReference(
            string authoringId,
            string plannedIdentity,
            string label,
            bool allowSelf,
            params AgentMutationOutputKind[] expectedKinds)
        {
            bool hasAuthoring = HasValue(authoringId);
            bool hasPlannedIdentity = HasValue(plannedIdentity);
            if (!hasAuthoring && !hasPlannedIdentity)
                return default;
            if (hasAuthoring && hasPlannedIdentity)
            {
                Error(label, $"{label}_reference_ambiguous", $"{label}不能同时包含stable identity与local identity。");
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

            AgentPlannedIdentityReference reference = AgentPlannedIdentityReference.Parse(plannedIdentity);
            if (!IsLocalIdentity(reference.Identity))
            {
                Error(label, $"{label}_planned_identity_invalid", $"计划内引用必须使用Document local identity：{plannedIdentity}");
                return default;
            }
            if (allowSelf && IsSelfIdentity(reference.Identity) && string.IsNullOrEmpty(reference.Role))
                return default;
            if (!m_PlannedIdentities.TryGetValue(reference.Identity, out AgentPlannedIdentitySymbol symbol))
            {
                Error(label, $"{label}_planned_identity_unresolved", $"Local identity必须由更早的typed Mutation创建：{plannedIdentity}");
                return default;
            }
            if (!Contains(expectedKinds, symbol.Kind))
            {
                Error(label, $"{label}_planned_identity_kind_invalid", $"Local identity {reference.Identity} 的typed kind是{symbol.Kind}，不能作为{label}。");
                return default;
            }
            if (!string.IsNullOrEmpty(reference.Role) &&
                (symbol.Kind != AgentMutationOutputKind.StateMachine || !IsStateMachineControlRole(reference.Role)))
            {
                Error(label, $"{label}_planned_identity_role_invalid", $"Local identity role无效：{plannedIdentity}");
                return default;
            }
            return new AgentAuthoringReference(string.Empty, reference);
        }

        void ValidateOwnedReference(AgentAuthoringReference reference, string expectedOwnerScope, string label)
        {
            AgentPlannedIdentityReference plannedIdentity = reference.PlannedIdentity;
            if (!plannedIdentity.IsValid || !m_PlannedIdentities.TryGetValue(plannedIdentity.Identity, out AgentPlannedIdentitySymbol symbol))
                return;

            string actualOwnerScope = !string.IsNullOrEmpty(plannedIdentity.Role) && symbol.Kind == AgentMutationOutputKind.StateMachine
                ? plannedIdentity.Identity
                : symbol.OwnerScope;
            if (!string.Equals(actualOwnerScope, expectedOwnerScope, StringComparison.Ordinal))
            {
                Error(
                    label,
                    $"{label}_planned_identity_owner_mismatch",
                    $"Local identity {plannedIdentity.Value}属于{actualOwnerScope}，不能用于owner {expectedOwnerScope}。");
            }
        }

        bool IsSelfIdentity(string plannedIdentity)
        {
            return string.Equals(plannedIdentity, m_MutationId, StringComparison.Ordinal);
        }

        static bool Contains(AgentMutationOutputKind[] values, AgentMutationOutputKind value)
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

        static bool IsLocalIdentity(string identity)
        {
            return !string.IsNullOrEmpty(identity) && identity.StartsWith("local:", StringComparison.Ordinal);
        }

        static bool TryParseTermKind(string value, out AgentConditionTermKind kind)
        {
            kind = default;
            return value != null && s_TermKinds.TryGetValue(value, out kind);
        }

        static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);
    }
}
