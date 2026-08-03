using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.AI;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentDocumentReconciler
    {
        static readonly BtsmtlGraphAuthoringCapabilities
            s_Capabilities =
                new BtsmtlGraphAuthoringCapabilities();

        public AgentDocumentPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot current,
            AgentAuthoringTarget target)
        {
            AgentMutationDraftSet mutations = CreateMutationSet(current, target);
            var report = ValidateEnvelope(current, target);
            if (!report.HasErrors())
                BuildCharacterMutations(current, target.editable, mutations, report);
            AgentPresentationMutationPlan presentationPlan = null;
            if (!report.HasErrors())
            {
                new AgentAuthoringPresentationReconciler().TryCreatePlan(
                    definition,
                    target.editable,
                    target.context,
                    report,
                    out presentationPlan);
            }
            if (report.HasErrors())
                return new AgentDocumentPreparation(null, current, null, report);
            AgentDocumentPreparation preparation =
                new AgentDocumentMutationCompiler().Prepare(
                    definition,
                    current,
                    mutations);
            preparation.Report.messages.AddRange(report.messages);
            preparation.Report.plannedDiff.AddRange(report.plannedDiff);
            preparation.Report.metrics.diffSize =
                preparation.Report.plannedDiff.Count;
            preparation.Report.success = !preparation.Report.HasErrors();
            return new AgentDocumentPreparation(
                preparation.Plan,
                preparation.Snapshot,
                preparation.Boundary,
                preparation.Report,
                presentationPlan);
        }

        public AgentDocumentPreparation Prepare(
            AIControllerDefinition definition,
            AgentGraphSnapshot current,
            AgentAuthoringTarget target)
        {
            AgentMutationDraftSet mutations = CreateMutationSet(current, target);
            var report = ValidateEnvelope(current, target);
            if (!report.HasErrors())
                BuildAIMutations(current, target.editable, mutations, report);
            if (report.HasErrors())
                return new AgentDocumentPreparation(null, current, null, report);
            return new AgentDocumentMutationCompiler().Prepare(definition, current, mutations);
        }

        static AgentMutationDraftSet CreateMutationSet(AgentGraphSnapshot current, AgentAuthoringTarget target)
        {
            return new AgentMutationDraftSet
            {
                schemaVersion = AgentAuthoringSchema.Version,
                domain = target?.domain,
                rootIdentity = target?.rootIdentity,
                sourceRevision = current?.sourceRevision,
                mutations = new List<AgentMutationDraft>()
            };
        }

        static AgentCompileReport ValidateEnvelope(AgentGraphSnapshot current, AgentAuthoringTarget target)
        {
            var report = new AgentCompileReport
            {
                success = true,
                domain = current?.domain ?? string.Empty,
                rootIdentity = current?.rootIdentity ?? string.Empty
            };
            if (target == null)
            {
                report.Error("document", "document_missing", "Agent Authoring Document缺失。");
                return report;
            }
            if (!string.Equals(target.domain, current.domain, StringComparison.Ordinal))
                report.Error("document.domain", "document_domain_mismatch", "Document domain与当前root不一致。");
            if (!string.Equals(target.rootIdentity, current.rootIdentity, StringComparison.Ordinal))
                report.Error("document.rootIdentity", "document_root_mismatch", "Document rootIdentity与当前root不一致。");
            if (target.editable == null)
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
            return report;
        }

        static void BuildCharacterMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            BuildCharacterBlackboardMutations(current.blackboardDeclarations, target.blackboardDeclarations, target.graphs, mutations, report);
            BuildStateMachineMutations(current.stateMachines, target.stateMachines, target.graphs, mutations, report);
            BuildCharacterGraphMutations(current.graphs, target.graphs, target.stateMachines, mutations, report);
            BuildTimelineMutations(current, target, mutations, report);
            BuildActionMutations(current, target, mutations, report);
        }

        static void BuildStateMachineMutations(
            IReadOnlyList<AgentSnapshotStateMachineSummary> current,
            IReadOnlyList<AgentSnapshotStateMachineSummary> target,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var currentMachines = Index(current, value => value.graphAuthoringId, "document.editable.stateMachines", report);
            var targetMachines = Index(target, value => value.graphAuthoringId, "document.editable.stateMachines", report);
            foreach (AgentSnapshotStateMachineSummary machine in target ?? Array.Empty<AgentSnapshotStateMachineSummary>())
            {
                string machinePath = $"document.editable.stateMachines[{Escape(machine.graphAuthoringId)}]";
                AgentSnapshotGraph machineGraph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                    .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, machine.graphAuthoringId, StringComparison.Ordinal));
                currentMachines.TryGetValue(machine.graphAuthoringId, out AgentSnapshotStateMachineSummary oldMachine);
                if (IsLocal(machine.graphAuthoringId))
                {
                    if (!TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out AgentSnapshotGraph newParent, out AgentSnapshotNode newOwner))
                    {
                        report.Error(machinePath, "state_machine_local_parent_missing", "新StateMachine需要由Graph中的StateMachineNode明确声明owner与parent。");
                        continue;
                    }
                    Add(mutations, machinePath, AgentMutationKind.EnsureStateMachine, operation =>
                    {
                        operation.id = LocalIdentity(newOwner.elementAuthoringId);
                        SetGraph(operation, newParent);
                        operation.displayName = machine.name;
                        operation.position = ToVector(newOwner.position);
                    });
                }
                else if (oldMachine == null)
                {
                    report.Error(machinePath, "state_machine_identity_unknown", "StateMachine identity不在当前树中；新StateMachine必须使用document-local identity。");
                    continue;
                }
                else if (!string.Equals(oldMachine.name, machine.name, StringComparison.Ordinal) &&
                         TryFindGraphOwner(targetGraphs, machine.graphAuthoringId, out AgentSnapshotGraph existingParent, out AgentSnapshotNode existingOwner))
                {
                    Add(mutations, machinePath, AgentMutationKind.EnsureStateMachine, operation =>
                    {
                        SetGraph(operation, existingParent.graphAuthoringId);
                        operation.targetElementAuthoringId = existingOwner.elementAuthoringId;
                        operation.stateMachineGraphAuthoringId = machine.graphAuthoringId;
                        operation.displayName = machine.name;
                        operation.position = ToVector(existingOwner.position);
                    });
                }

                oldMachine ??= new AgentSnapshotStateMachineSummary();

                var oldStates = Index(oldMachine.states, value => value.stateAuthoringId, machinePath + ".states", report);
                var newStates = Index(machine.states, value => value.stateAuthoringId, machinePath + ".states", report);
                foreach (AgentSnapshotStateSummary state in machine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    string path = $"{machinePath}.states[{Escape(state.stateAuthoringId)}]";
                    oldStates.TryGetValue(state.stateAuthoringId, out AgentSnapshotStateSummary oldState);
                    if (IsLocal(state.stateAuthoringId))
                    {
                        Add(mutations, path, AgentMutationKind.EnsureState, operation =>
                        {
                            operation.id = LocalIdentity(state.stateAuthoringId);
                            SetStateMachine(operation, machineGraph);
                            operation.state = state.state;
                            operation.position = FindNodePosition(targetGraphs, state.stateAuthoringId);
                        });
                    }
                    else if (oldState == null)
                    {
                        report.Error(path, "state_identity_unknown", "State identity不在当前树中；新State必须使用local:前缀。");
                    }
                    else if (!string.Equals(oldState.state, state.state, StringComparison.Ordinal))
                    {
                        Add(mutations, path, AgentMutationKind.EnsureState, operation =>
                        {
                            SetStateMachine(operation, machineGraph);
                            operation.stateAuthoringId = state.stateAuthoringId;
                            operation.state = state.state;
                            operation.position = FindNodePosition(targetGraphs, state.stateAuthoringId);
                        });
                    }
                    oldState ??= new AgentSnapshotStateSummary();
                    AgentSnapshotGraph behaviorGraph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                        .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, state.behaviorGraphAuthoringId, StringComparison.Ordinal));
                    var oldActivations = Index(oldState.actionActivations, value => value.nodeAuthoringId, path + ".actionActivations", report);
                    foreach (AgentSnapshotActionActivationSummary activation in state.actionActivations ?? new List<AgentSnapshotActionActivationSummary>())
                    {
                        string activationPath = $"{path}.actionActivations[{Escape(activation.nodeAuthoringId)}]";
                        if (oldActivations.TryGetValue(activation.nodeAuthoringId, out AgentSnapshotActionActivationSummary oldActivation) &&
                            Same(oldActivation, activation))
                            continue;
                        Add(mutations, activationPath, AgentMutationKind.EnsureActionActivation, operation =>
                        {
                            if (IsLocal(activation.nodeAuthoringId))
                                operation.id = LocalIdentity(activation.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, activation.nodeAuthoringId, false);
                            operation.displayName = activation.displayName;
                            operation.lifecycleSlot = "OnEnter";
                            operation.actionProfile = activation.actionProfile;
                            operation.actionContext = activation.actionContext;
                            operation.sourceInputRequestId = activation.sourceRequest;
                            operation.consumeSourceInputRequest = true;
                            operation.targetKey = activation.targetKey;
                            operation.targetSnapshotBlackboardKey = activation.targetSnapshotBlackboardKey;
                            operation.position = FindNodePosition(targetGraphs, activation.nodeAuthoringId);
                        });
                    }
                    var oldLifecycleTransitions = Index(
                        oldState.lifecycleTransitions,
                        value => value.nodeAuthoringId,
                        path + ".lifecycleTransitions",
                        report);
                    foreach (AgentSnapshotLifecycleSummary lifecycle in state.lifecycleTransitions ?? new List<AgentSnapshotLifecycleSummary>())
                    {
                        string lifecyclePath = $"{path}.lifecycleTransitions[{Escape(lifecycle.nodeAuthoringId)}]";
                        if (oldLifecycleTransitions.TryGetValue(lifecycle.nodeAuthoringId, out AgentSnapshotLifecycleSummary oldLifecycle) &&
                            Same(oldLifecycle, lifecycle))
                            continue;
                        Add(mutations, lifecyclePath, AgentMutationKind.EnsureActionLifecycleTransition, operation =>
                        {
                            if (IsLocal(lifecycle.nodeAuthoringId))
                                operation.id = LocalIdentity(lifecycle.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, lifecycle.nodeAuthoringId, false);
                            operation.displayName = lifecycle.displayName;
                            operation.lifecycleType = lifecycle.transitionType;
                            operation.reason = lifecycle.reason;
                            operation.actionContext = lifecycle.actionContext;
                            operation.position = FindNodePosition(targetGraphs, lifecycle.nodeAuthoringId);
                        });
                    }
                    var oldTimelines = Index(oldState.timelines, value => value.nodeAuthoringId, path + ".timelines", report);
                    foreach (AgentSnapshotTimelineBindingSummary timeline in state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                    {
                        string timelinePath = $"{path}.timelines[{Escape(timeline.nodeAuthoringId)}]";
                        if (oldTimelines.TryGetValue(timeline.nodeAuthoringId, out AgentSnapshotTimelineBindingSummary oldTimeline) &&
                            Same(oldTimeline, timeline))
                            continue;
                        Add(mutations, timelinePath, AgentMutationKind.EnsureTimelineNode, operation =>
                        {
                            if (IsLocal(timeline.nodeAuthoringId))
                                operation.id = LocalIdentity(timeline.nodeAuthoringId);
                            if (behaviorGraph != null)
                                SetTargetGraph(operation, behaviorGraph);
                            else
                                operation.targetGraphAuthoringId = state.behaviorGraphAuthoringId;
                            SetOptionalExisting(operation, timeline.nodeAuthoringId, false);
                            operation.displayName = timeline.displayName;
                            operation.timeline = timeline.timeline;
                            operation.timelineAuthoringId = timeline.timelineAuthoringId;
                            operation.timelineOwnership = timeline.ownership;
                            operation.timelineAssetPath = timeline.timelineAssetPath;
                            operation.timelineAssetGuid = timeline.timelineAssetGuid;
                            operation.actionContext = timeline.actionContext;
                            operation.position = FindNodePosition(targetGraphs, timeline.nodeAuthoringId);
                        });
                    }
                }
                foreach (AgentSnapshotStateSummary state in oldMachine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (newStates.ContainsKey(state.stateAuthoringId))
                        continue;
                    Add(mutations, $"{machinePath}.states[{Escape(state.stateAuthoringId)}]", AgentMutationKind.DeleteState, operation =>
                    {
                        SetStateMachine(operation, machineGraph);
                        operation.stateAuthoringId = state.stateAuthoringId;
                    });
                }

                var removedStates = new HashSet<string>(oldStates.Keys.Except(newStates.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
                var oldTransitions = Index(oldMachine.transitions, value => value.edgeAuthoringId, machinePath + ".transitions", report);
                var newTransitions = Index(machine.transitions, value => value.edgeAuthoringId, machinePath + ".transitions", report);
                foreach (AgentSnapshotTransitionSummary transition in machine.transitions ?? new List<AgentSnapshotTransitionSummary>())
                {
                    string path = $"{machinePath}.transitions[{Escape(transition.edgeAuthoringId)}]";
                    bool changed = !oldTransitions.TryGetValue(transition.edgeAuthoringId, out AgentSnapshotTransitionSummary oldTransition) ||
                                   !Same(oldTransition, transition);
                    if (!changed)
                        continue;
                    if (oldTransition != null &&
                        !SameTransitionEndpoints(oldTransition, transition))
                    {
                        if (!SameTransitionExceptEndpoints(oldTransition, transition))
                        {
                            report.Error(
                                path,
                                "transition_rewire_mixed_change",
                                "已有Transition改接端点时只能迁移端点；条件与priority必须保持不变。请先完成端点迁移，再单独修改其它业务语义。");
                            continue;
                        }
                        Add(
                            mutations,
                            path + ".endpoints",
                            AgentMutationKind.RewireTransition,
                            operation =>
                            {
                                SetStateMachine(operation, machineGraph);
                                SetElement(operation, transition.fromElementAuthoringId, true);
                                SetElement(operation, transition.toElementAuthoringId, false);
                                operation.targetElementAuthoringId =
                                    transition.edgeAuthoringId;
                                operation.transitionPriority =
                                    transition.priority;
                            });
                        continue;
                    }
                    if (oldTransition != null && SameTransitionExceptActionAdmission(oldTransition, transition))
                    {
                        List<AgentSnapshotConditionTerm> admissionTerms = (transition.conditionTerms ?? new List<AgentSnapshotConditionTerm>())
                            .Where(term => string.Equals(term.kind, "action_can_activate", StringComparison.Ordinal))
                            .ToList();
                        List<AgentSnapshotGraph> conditionGraphs = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                            .Where(graph => string.Equals(graph.ownerElementAuthoringId, transition.edgeAuthoringId, StringComparison.Ordinal) &&
                                            string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                            .ToList();
                        List<AgentSnapshotNode> admissionNodes = conditionGraphs
                            .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                            .Where(node => node.typeName?.EndsWith(".CanActivateActionInfoNode", StringComparison.Ordinal) == true)
                            .ToList();
                        if (admissionTerms.Count != 1 || string.IsNullOrWhiteSpace(admissionTerms[0].actionProfile) ||
                            conditionGraphs.Count != 1 || admissionNodes.Count != 1)
                        {
                            report.Error(path, "transition_action_admission_ambiguous", "Transition的Action准入必须对应唯一Condition Rule Graph、CanActivateActionInfoNode和ActionProfile。");
                            continue;
                        }
                        Add(mutations, path + ".conditionTerms[action_can_activate]", AgentMutationKind.ConfigureActionAdmission, operation =>
                        {
                            SetGraph(operation, conditionGraphs[0].graphAuthoringId);
                            operation.targetElementAuthoringId = admissionNodes[0].elementAuthoringId;
                            operation.actionProfile = admissionTerms[0].actionProfile;
                        });
                        continue;
                    }
                    Add(mutations, path, transition.conditionTerms != null && transition.conditionTerms.Count > 0 ? AgentMutationKind.EnsureConditionRule : AgentMutationKind.EnsureTransition, operation =>
                    {
                        SetStateMachine(operation, machineGraph);
                        SetElement(operation, transition.fromElementAuthoringId, true);
                        SetElement(operation, transition.toElementAuthoringId, false);
                        operation.targetElementAuthoringId = transition.edgeAuthoringId;
                        operation.transitionPriority = transition.priority;
                        operation.conditionGroups = ToConditionGroups(transition.conditionTerms);
                    });
                }
                foreach (AgentSnapshotTransitionSummary transition in oldMachine.transitions ?? new List<AgentSnapshotTransitionSummary>())
                {
                    if (newTransitions.ContainsKey(transition.edgeAuthoringId))
                        continue;
                    if (removedStates.Contains(transition.fromElementAuthoringId) ||
                        removedStates.Contains(transition.toElementAuthoringId))
                        continue;
                    Add(mutations, $"{machinePath}.transitions[{Escape(transition.edgeAuthoringId)}]", AgentMutationKind.DeleteTransition, operation =>
                    {
                        SetStateMachine(operation, machineGraph);
                        operation.targetElementAuthoringId = transition.edgeAuthoringId;
                    });
                }
            }
            foreach (string removedMachine in currentMachines.Keys.Except(targetMachines.Keys, StringComparer.Ordinal))
                report.Error($"document.editable.stateMachines[{Escape(removedMachine)}]", "state_machine_delete_unsupported", "当前正式authoring API不支持删除整个StateMachine；请删除其owner节点。");
        }

        static void BuildCharacterBlackboardMutations(
            IReadOnlyList<AgentSnapshotBlackboardDeclaration> current,
            IReadOnlyList<AgentSnapshotBlackboardDeclaration> target,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldValues = Index(current, value => value.declarationId, "document.editable.blackboardDeclarations", report);
            var newValues = Index(target, value => value.declarationId, "document.editable.blackboardDeclarations", report);
            foreach (AgentSnapshotBlackboardDeclaration declaration in target ?? Array.Empty<AgentSnapshotBlackboardDeclaration>())
            {
                string path = $"document.editable.blackboardDeclarations[{Escape(declaration.declarationId)}]";
                if (IsLocal(declaration.declarationId) || !oldValues.TryGetValue(declaration.declarationId, out AgentSnapshotBlackboardDeclaration oldValue) || !Same(oldValue, declaration))
                {
                    Add(mutations, path, AgentMutationKind.EnsureBlackboardDeclaration, operation =>
                    {
                        if (IsLocal(declaration.declarationId))
                            operation.id = LocalIdentity(declaration.declarationId);
                        AgentSnapshotGraph graph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                            .FirstOrDefault(value => string.Equals(value.graphAuthoringId, declaration.ownerId, StringComparison.Ordinal));
                        if (graph != null)
                            SetGraph(operation, graph);
                        else
                            SetGraph(operation, declaration.ownerId);
                        if (!IsLocal(declaration.declarationId))
                            operation.declarationAuthoringId = declaration.declarationId;
                        operation.blackboardKey = declaration.key;
                        operation.blackboardValueType = declaration.valueType;
                        operation.blackboardDefaultValue = declaration.defaultValue?.DeepClone();
                        operation.blackboardScope = declaration.scope;
                        operation.blackboardLifetime = declaration.lifetime;
                        operation.blackboardAuthority = declaration.authority;
                        operation.blackboardSyncPolicy = declaration.syncPolicy;
                        operation.inputId = declaration.inputValueId;
                        operation.factProjection = declaration.factProjection;
                        operation.windowType = declaration.windowType;
                        operation.windowId = declaration.windowId;
                        operation.digest = declaration.digest;
                        operation.categoryPath = declaration.categoryPath;
                    });
                }
            }
            foreach (AgentSnapshotBlackboardDeclaration declaration in current ?? Array.Empty<AgentSnapshotBlackboardDeclaration>())
            {
                if (newValues.ContainsKey(declaration.declarationId))
                    continue;
                Add(mutations, $"document.editable.blackboardDeclarations[{Escape(declaration.declarationId)}]", AgentMutationKind.DeleteBlackboardDeclaration, operation =>
                {
                    SetGraph(operation, declaration.ownerId);
                    operation.declarationAuthoringId = declaration.declarationId;
                });
            }
        }

        static void BuildActionMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldRequests = Index(current.actionRequests, value => value.requestId, "document.editable.actionRequests", report);
            var newRequests = Index(target.actionRequests, value => value.requestId, "document.editable.actionRequests", report);
            foreach (AgentSnapshotActionRequest request in target.actionRequests ?? new List<AgentSnapshotActionRequest>())
            {
                string path = $"document.editable.actionRequests[{Escape(request.requestId)}]";
                if (!oldRequests.TryGetValue(request.requestId, out AgentSnapshotActionRequest oldRequest))
                {
                    report.Error(path, "action_request_create_unsupported", "Action Request catalog不能由Agent Document创建。");
                    continue;
                }
                if (oldRequest.bufferSeconds != request.bufferSeconds || oldRequest.priority != request.priority)
                    report.Error(path, "action_request_readonly_field_modified", "bufferSeconds与priority来自正式Action catalog，不可由Document修改。");
                if (!string.Equals(oldRequest.timingClass, request.timingClass, StringComparison.Ordinal))
                {
                    Add(mutations, path, AgentMutationKind.SetActionRequestTimingClass, operation =>
                    {
                        operation.request = request.requestId;
                        operation.requestTimingClass = request.timingClass;
                    });
                }
            }
            foreach (string removed in oldRequests.Keys.Except(newRequests.Keys, StringComparer.Ordinal))
                report.Error($"document.editable.actionRequests[{Escape(removed)}]", "action_request_delete_unsupported", "Action Request catalog不可由Document删除。");

            var oldProfiles = Index(current.actionProfiles, value => value.actionId, "document.editable.actionProfiles", report);
            var newProfiles = Index(target.actionProfiles, value => value.actionId, "document.editable.actionProfiles", report);
            foreach (AgentSnapshotActionProfile profile in target.actionProfiles ?? new List<AgentSnapshotActionProfile>())
            {
                string path = $"document.editable.actionProfiles[{Escape(profile.actionId)}]";
                if (!oldProfiles.TryGetValue(profile.actionId, out AgentSnapshotActionProfile oldProfile))
                {
                    report.Error(path, "action_profile_create_unsupported", "ActionProfile资产不能由Agent Document创建。");
                    continue;
                }
                if (!string.Equals(AgentAuthoringDocumentCodec.Hash(oldProfile.blockQuery), AgentAuthoringDocumentCodec.Hash(profile.blockQuery), StringComparison.Ordinal))
                    report.Error(path + ".blockQuery", "action_profile_block_query_readonly", "当前正式authoring API未开放blockQuery写入。");
                if (!SameList(oldProfile.grantedTags, profile.grantedTags))
                {
                    Add(mutations, path + ".grantedTags", AgentMutationKind.SetActionProfileGrantedTags, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.grantedTags = profile.grantedTags;
                    });
                }
                if (!Same(oldProfile.cancelQuery, profile.cancelQuery))
                {
                    Add(mutations, path + ".cancelQuery", AgentMutationKind.SetActionProfileCancelQuery, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.queryAll = profile.cancelQuery.all;
                        operation.queryAny = profile.cancelQuery.any;
                        operation.queryNone = profile.cancelQuery.none;
                    });
                }
                if (!string.Equals(oldProfile.targetRequirement, profile.targetRequirement, StringComparison.Ordinal))
                {
                    Add(mutations, path + ".targetRequirement", AgentMutationKind.SetActionProfileTargetRequirement, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.targetRequirement = profile.targetRequirement;
                    });
                }
            }
            foreach (string removed in oldProfiles.Keys.Except(newProfiles.Keys, StringComparer.Ordinal))
                report.Error($"document.editable.actionProfiles[{Escape(removed)}]", "action_profile_delete_unsupported", "ActionProfile资产不可由Document删除。");
        }

        static void BuildAIMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            AgentSnapshotAIController oldController = current.aiController ?? new AgentSnapshotAIController();
            AgentDocumentAIEditable controller = target.aiController;
            if (controller == null)
            {
                report.Error("document.editable.aiController", "ai_controller_missing", "AI Document缺少aiController正文。");
                return;
            }
            if (!string.Equals(oldController.controllerId, controller.controllerId, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.controllerId", AgentMutationKind.EnsureAIControllerDefinition, operation =>
                    operation.controllerId = controller.controllerId);
            }
            if (!string.Equals(oldController.treeAssetPath, controller.treeAssetPath, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.treeAssetPath", AgentMutationKind.EnsureAIControllerTree, operation =>
                    operation.rootTreeAssetPath = controller.treeAssetPath);
            }
            if (!string.Equals(oldController.controlledCharacterAssetPath, controller.controlledCharacterAssetPath, StringComparison.Ordinal) ||
                !string.Equals(oldController.controlledCharacterAssetGuid, controller.controlledCharacterAssetGuid, StringComparison.Ordinal) ||
                !string.Equals(oldController.perceptionAssetPath, controller.perceptionAssetPath, StringComparison.Ordinal) ||
                !string.Equals(oldController.perceptionAssetGuid, controller.perceptionAssetGuid, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.assets", AgentMutationKind.BindAIControllerAssets, operation =>
                {
                    operation.controlledCharacterAssetPath = controller.controlledCharacterAssetPath;
                    operation.controlledCharacterAssetGuid = controller.controlledCharacterAssetGuid;
                    operation.perceptionProfileAssetPath = controller.perceptionAssetPath;
                    operation.perceptionProfileAssetGuid = controller.perceptionAssetGuid;
                });
            }
            if (!string.Equals(oldController.candidateOrdering, controller.candidateOrdering, StringComparison.Ordinal) ||
                !SameList(oldController.candidateActorIds, controller.candidateActorIds))
            {
                Add(mutations, "document.editable.aiController.candidates", AgentMutationKind.ConfigureAICandidates, operation =>
                {
                    operation.candidateOrdering = controller.candidateOrdering;
                    operation.candidateActorIds = controller.candidateActorIds;
                });
            }
            BuildAIBlackboardMutations(oldController.blackboardDeclarations, controller.blackboardDeclarations, mutations, report);
            HashSet<string> removedNodes = BuildAINodeMutations(
                oldController.nodes,
                controller.nodes,
                current.graphs,
                target.graphs,
                mutations,
                report);
            IReadOnlyList<AgentSnapshotGraph> currentGraphs = (current.graphs ?? new List<AgentSnapshotGraph>())
                .Select(graph =>
                {
                    AgentSnapshotGraph clone = AgentAuthoringDocumentCodec.Clone(graph);
                    clone.flowEdges = (clone.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                        .Where(edge => !removedNodes.Contains(edge.startElementAuthoringId) && !removedNodes.Contains(edge.endElementAuthoringId))
                        .ToList();
                    clone.propertyEdges = (clone.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                        .Where(edge => !removedNodes.Contains(edge.startElementAuthoringId) && !removedNodes.Contains(edge.endElementAuthoringId))
                        .ToList();
                    return clone;
                })
                .ToList();
            foreach (AgentSnapshotGraph graph in target.graphs ?? new List<AgentSnapshotGraph>())
            {
                if (string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal))
                    continue;
                AgentSnapshotGraph oldGraph = currentGraphs.FirstOrDefault(value =>
                    string.Equals(value.graphAuthoringId, graph.graphAuthoringId, StringComparison.Ordinal));
                if (oldGraph == null)
                {
                    report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_create_unsupported", "AI子Graph必须由拥有它的正式Node或Edge创建。");
                    continue;
                }
                BuildGenericGraphMutations(oldGraph, graph, mutations, report);
            }
            BuildGraphEdgeMutations(
                currentGraphs.Where(graph => string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal)).ToList(),
                (target.graphs ?? new List<AgentSnapshotGraph>())
                    .Where(graph => string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal))
                    .ToList(),
                mutations,
                report);
        }

        static void BuildAIBlackboardMutations(
            IReadOnlyList<AgentSnapshotAIBlackboardDeclaration> current,
            IReadOnlyList<AgentSnapshotAIBlackboardDeclaration> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldValues = Index(current, value => value.declarationAuthoringId, "document.editable.aiController.blackboardDeclarations", report);
            var newValues = Index(target, value => value.declarationAuthoringId, "document.editable.aiController.blackboardDeclarations", report);
            foreach (AgentSnapshotAIBlackboardDeclaration declaration in target ?? Array.Empty<AgentSnapshotAIBlackboardDeclaration>())
            {
                string path = $"document.editable.aiController.blackboardDeclarations[{Escape(declaration.declarationAuthoringId)}]";
                if (!IsLocal(declaration.declarationAuthoringId) &&
                    oldValues.TryGetValue(declaration.declarationAuthoringId, out AgentSnapshotAIBlackboardDeclaration oldValue) &&
                    Same(oldValue, declaration))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureAIBlackboardDeclaration, operation =>
                {
                    if (IsLocal(declaration.declarationAuthoringId))
                        operation.id = LocalIdentity(declaration.declarationAuthoringId);
                    SetGraph(operation, declaration.ownerGraphAuthoringId);
                    SetOptionalExisting(operation, declaration.declarationAuthoringId, true);
                    operation.blackboardKey = declaration.displayName;
                    operation.blackboardValueType = declaration.valueType;
                    operation.blackboardScope = declaration.scope;
                    SetDefaultValue(operation, declaration.valueType, declaration.defaultValue);
                });
            }
            foreach (AgentSnapshotAIBlackboardDeclaration removed in (current ?? Array.Empty<AgentSnapshotAIBlackboardDeclaration>())
                         .Where(value => !newValues.ContainsKey(value.declarationAuthoringId)))
            {
                Add(mutations, $"document.editable.aiController.blackboardDeclarations[{Escape(removed.declarationAuthoringId)}]", AgentMutationKind.DeleteBlackboardDeclaration, operation =>
                {
                    SetGraph(operation, removed.ownerGraphAuthoringId);
                    operation.declarationAuthoringId = removed.declarationAuthoringId;
                });
            }
        }

        static HashSet<string> BuildAINodeMutations(
            IReadOnlyList<AgentSnapshotAINode> current,
            IReadOnlyList<AgentSnapshotAINode> target,
            IReadOnlyList<AgentSnapshotGraph> currentGraphs,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldValues = Index(current, value => value.nodeAuthoringId, "document.editable.aiController.nodes", report);
            var newValues = Index(target, value => value.nodeAuthoringId, "document.editable.aiController.nodes", report);
            var positions = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().position, StringComparer.Ordinal);
            var currentPositions = (currentGraphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().position, StringComparer.Ordinal);
            foreach (AgentSnapshotAINode node in target ?? Array.Empty<AgentSnapshotAINode>())
            {
                string path = $"document.editable.aiController.nodes[{Escape(node.nodeAuthoringId)}]";
                if (!IsLocal(node.nodeAuthoringId) &&
                    oldValues.TryGetValue(node.nodeAuthoringId, out AgentSnapshotAINode oldNode) &&
                    SameAINodeEditable(oldNode, node) &&
                    Same(
                        currentPositions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 oldPosition) ? oldPosition : null,
                        positions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 newPosition) ? newPosition : null))
                    continue;
                AgentMutationKind? mutationKind = ResolveAINodeMutationKind(node);
                if (!mutationKind.HasValue)
                {
                    report.Error(path, "ai_node_kind_unsupported", $"AI nodeType未映射到正式Mutation：{node.nodeType}");
                    continue;
                }
                Add(mutations, path, mutationKind.Value, operation =>
                {
                    if (IsLocal(node.nodeAuthoringId))
                        operation.id = LocalIdentity(node.nodeAuthoringId);
                    SetGraph(operation, node.graphAuthoringId);
                    SetOptionalExisting(operation, node.nodeAuthoringId, false);
                    operation.aiNodeKind = ResolveAINodeKind(node);
                    operation.aiMemoryValueKind = node.memoryValueKind;
                    if (IsLocal(node.memoryDeclarationAuthoringId))
                        operation.declarationPlannedIdentity = LocalIdentity(node.memoryDeclarationAuthoringId);
                    else
                        operation.declarationAuthoringId = node.memoryDeclarationAuthoringId;
                    operation.inputId = node.inputId;
                    operation.request = node.requestId;
                    operation.requestBufferSeconds = node.requestBufferSeconds;
                    operation.requestPriority = node.requestPriority;
                    operation.aiRequestRepeatPolicy = node.requestRepeatPolicy;
                    if (positions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 position))
                        operation.position = new Vector2(position.x, position.y);
                });
            }
            var removed = new HashSet<string>(oldValues.Keys.Except(newValues.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (AgentSnapshotAINode node in current ?? Array.Empty<AgentSnapshotAINode>())
            {
                if (!removed.Contains(node.nodeAuthoringId))
                    continue;
                Add(mutations, $"document.editable.aiController.nodes[{Escape(node.nodeAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, node.graphAuthoringId);
                    operation.targetElementAuthoringId = node.nodeAuthoringId;
                });
            }
            return removed;
        }

        static bool SameAINodeEditable(AgentSnapshotAINode left, AgentSnapshotAINode right)
        {
            object Project(AgentSnapshotAINode node) => new
            {
                node.graphAuthoringId,
                node.nodeAuthoringId,
                node.nodeType,
                node.memoryDeclarationAuthoringId,
                node.memoryValueKind,
                node.inputId,
                node.requestId,
                node.requestBufferSeconds,
                node.requestPriority,
                node.requestRepeatPolicy
            };
            return Same(Project(left), Project(right));
        }

        static void BuildGraphEdgeMutations(
            IReadOnlyList<AgentSnapshotGraph> current,
            IReadOnlyList<AgentSnapshotGraph> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldGraphs = Index(current, value => value.graphAuthoringId, "document.editable.graphs", report);
            foreach (AgentSnapshotGraph graph in target ?? Array.Empty<AgentSnapshotGraph>())
            {
                string graphPath = $"document.editable.graphs[{Escape(graph.graphAuthoringId)}]";
                if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldGraph))
                {
                    report.Error(graphPath, "graph_create_unsupported", "当前Document版本不允许直接创建裸Graph；Graph必须由拥有它的State或节点创建。");
                    continue;
                }
                var oldFlow = Index(oldGraph.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
                var newFlow = Index(graph.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    string path = $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]";
                    if (oldFlow.TryGetValue(edge.elementAuthoringId, out AgentSnapshotFlowEdge oldEdge) && SameFlowEdge(oldEdge, edge))
                        continue;
                    if (oldFlow.ContainsKey(edge.elementAuthoringId))
                    {
                        Add(mutations, path, AgentMutationKind.DeleteFlowEdge, operation =>
                        {
                            SetGraph(operation, graph);
                            operation.targetElementAuthoringId = edge.elementAuthoringId;
                        });
                    }
                    Add(mutations, path, AgentMutationKind.LinkFlow, operation =>
                    {
                        SetGraph(operation, graph);
                        SetLinkElement(operation, edge.startElementAuthoringId, true);
                        SetLinkElement(operation, edge.endElementAuthoringId, false);
                        operation.startPort = edge.startPort;
                        operation.endPort = edge.endPort;
                        operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                    });
                }
                foreach (AgentSnapshotFlowEdge edge in oldGraph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    if (newFlow.ContainsKey(edge.elementAuthoringId))
                        continue;
                    Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                    {
                        SetGraph(operation, graph);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                BuildPropertyEdgeMutations(oldGraph, graph, mutations, report);
            }
        }

        static void BuildCharacterGraphMutations(
            IReadOnlyList<AgentSnapshotGraph> current,
            IReadOnlyList<AgentSnapshotGraph> target,
            IReadOnlyList<AgentSnapshotStateMachineSummary> targetStateMachines,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldGraphs = Index(current, value => value.graphAuthoringId, "document.editable.graphs", report);
            var targetGraphs = Index(target, value => value.graphAuthoringId, "document.editable.graphs", report);
            var specializedNodeIds = new HashSet<string>(
                (targetStateMachines ?? Array.Empty<AgentSnapshotStateMachineSummary>())
                    .SelectMany(machine => machine.states ?? new List<AgentSnapshotStateSummary>())
                    .SelectMany(state =>
                        (state.nestedStateMachines ?? new List<AgentSnapshotNestedStateMachineSummary>())
                            .Select(value => value.nodeAuthoringId)
                            .Concat((state.actionActivations ?? new List<AgentSnapshotActionActivationSummary>())
                                .Select(value => value.nodeAuthoringId))
                            .Concat((state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                                .Select(value => value.nodeAuthoringId))
                            .Concat((state.lifecycleTransitions ?? new List<AgentSnapshotLifecycleSummary>())
                                .Select(value => value.nodeAuthoringId)))
                    .Where(value => !string.IsNullOrEmpty(value)),
                StringComparer.Ordinal);
            foreach (AgentSnapshotGraph graph in target ?? Array.Empty<AgentSnapshotGraph>())
            {
                if (string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                    continue;
                if (string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                {
                    if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldConditionGraph))
                    {
                        report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "condition_graph_identity_unknown", "Condition Rule Graph identity不在当前树中。");
                        continue;
                    }
                    BuildConditionRuleGraphMutations(oldConditionGraph, graph, mutations, report);
                    continue;
                }
                if (!oldGraphs.TryGetValue(graph.graphAuthoringId, out AgentSnapshotGraph oldGraph))
                {
                    if (IsLocal(graph.graphAuthoringId) &&
                        (string.Equals(graph.kind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal) ||
                         string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal)))
                    {
                        oldGraph = AgentAuthoringDocumentCodec.Clone(graph);
                        oldGraph.nodes = new List<AgentSnapshotNode>();
                        oldGraph.flowEdges = new List<AgentSnapshotFlowEdge>();
                        oldGraph.propertyEdges = new List<AgentSnapshotPropertyEdge>();
                    }
                    else
                    {
                        report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_create_unsupported", "裸Graph不能直接创建，必须由拥有它的StateMachine、State或正式节点创建。");
                        continue;
                    }
                }
                if (string.Equals(graph.kind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                    BuildStateBehaviorGraphMutations(oldGraph, graph, specializedNodeIds, mutations, report);
                else
                    BuildGenericGraphMutations(oldGraph, graph, mutations, report);
            }
            foreach (AgentSnapshotGraph graph in current ?? Array.Empty<AgentSnapshotGraph>())
            {
                if (targetGraphs.ContainsKey(graph.graphAuthoringId) ||
                    string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(graph.ownerElementAuthoringId) &&
                    !(target ?? Array.Empty<AgentSnapshotGraph>())
                        .SelectMany(value => value.nodes ?? new List<AgentSnapshotNode>())
                        .Any(node => string.Equals(node.elementAuthoringId, graph.ownerElementAuthoringId, StringComparison.Ordinal)))
                    continue;
                report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_delete_requires_owner", "Graph只能通过删除拥有它的State或节点级联删除。");
            }
        }

        static void BuildPropertyEdgeMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            ISet<string> removedNodes = null)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            var oldEdges = Index(current.propertyEdges, value => value.elementAuthoringId, graphPath + ".propertyEdges", report);
            var newEdges = Index(target.propertyEdges, value => value.elementAuthoringId, graphPath + ".propertyEdges", report);
            foreach (AgentSnapshotPropertyEdge edge in target.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                string path = $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]";
                if (oldEdges.TryGetValue(edge.elementAuthoringId, out AgentSnapshotPropertyEdge oldEdge) && Same(oldEdge, edge))
                    continue;
                if (oldEdges.ContainsKey(edge.elementAuthoringId))
                {
                    Add(mutations, path, AgentMutationKind.DeletePropertyEdge, operation =>
                    {
                        SetGraph(operation, target);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                Add(mutations, path, AgentMutationKind.LinkProperty, operation =>
                {
                    SetGraph(operation, target);
                    SetLinkElement(operation, edge.startElementAuthoringId, true);
                    SetLinkElement(operation, edge.endElementAuthoringId, false);
                    operation.startPropertyPort = edge.startPortId;
                    operation.endPropertyPort = edge.endPortId;
                    operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotPropertyEdge edge in current.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                if (newEdges.ContainsKey(edge.elementAuthoringId) ||
                    removedNodes?.Contains(edge.startElementAuthoringId) == true ||
                    removedNodes?.Contains(edge.endElementAuthoringId) == true)
                    continue;
                Add(mutations, $"{graphPath}.propertyEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeletePropertyEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }
        }

        static void BuildConditionRuleGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "condition_graph_metadata_modified", "Condition Rule Graph元数据不能从底层镜像直接修改。");
                return;
            }
            var oldNodes = Index(current.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var newNodes = Index(target.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var removed = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (AgentSnapshotNode node in oldNodes.Values.Where(node => removed.Contains(node.elementAuthoringId)))
            {
                if (!s_Capabilities.TryGetKind(node.typeName, out string kind) ||
                    !IsInputNodeKind(kind) &&
                    !TryResolveConditionValueConfiguration(kind, out _) &&
                    !s_Capabilities.SupportsGenericMutation(node.typeName))
                {
                    report.Error(
                        $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]",
                        "condition_node_delete_unsupported",
                        "该Condition节点没有完整typed delete capability。");
                    continue;
                }
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }

            foreach (AgentSnapshotNode node in target.nodes ?? new List<AgentSnapshotNode>())
            {
                oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode);
                if (Same(oldNode, node))
                    continue;
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (oldNode == null)
                {
                    if (!ValidateGenericNodeChange(null, node, report, path))
                        continue;
                    Add(mutations, path, AgentMutationKind.EnsureGraphNode, operation =>
                    {
                        if (IsLocal(node.elementAuthoringId))
                            operation.id = LocalIdentity(node.elementAuthoringId);
                        SetGraph(operation, target);
                        SetOptionalExisting(operation, node.elementAuthoringId, false);
                        operation.nodeType = node.typeName;
                        operation.displayName = node.displayName;
                        operation.loopStopType = node.loopStopType;
                        operation.compareType = node.compareType;
                        SetMotionConfiguration(operation, node);
                        operation.position = ToVector(node.position);
                    });
                    continue;
                }
                report.Error(path, "condition_node_reconcile_unsupported", "该Condition节点变化没有对应的正式typed Mutation。");
            }
            AgentSnapshotGraph currentEdges = AgentAuthoringDocumentCodec.Clone(current);
            currentEdges.flowEdges = (currentEdges.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                .Where(edge => !removed.Contains(edge.startElementAuthoringId) && !removed.Contains(edge.endElementAuthoringId))
                .ToList();
            currentEdges.propertyEdges = (currentEdges.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                .Where(edge => !removed.Contains(edge.startElementAuthoringId) && !removed.Contains(edge.endElementAuthoringId))
                .ToList();
            BuildGraphEdgeMutations(new[] { currentEdges }, new[] { target }, mutations, report);
        }

        static void BuildGenericGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "graph_metadata_modified", "Graph kind、ownership与owner不能原地修改。");
                return;
            }

            bool Ignored(AgentSnapshotNode node) =>
                node?.typeName?.EndsWith(".StateMachineNode", StringComparison.Ordinal) == true;
            var oldNodes = Index(
                (current.nodes ?? new List<AgentSnapshotNode>()).Where(node => !Ignored(node)),
                value => value.elementAuthoringId,
                graphPath + ".nodes",
                report);
            var newNodes = Index(
                (target.nodes ?? new List<AgentSnapshotNode>()).Where(node => !Ignored(node)),
                value => value.elementAuthoringId,
                graphPath + ".nodes",
                report);
            var removed = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (AgentSnapshotNode node in oldNodes.Values.Where(node => removed.Contains(node.elementAuthoringId)))
            {
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotNode node in newNodes.Values)
            {
                if (oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode) && Same(oldNode, node))
                    continue;
                if (node.exposedProperty != null)
                {
                    AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                    continue;
                }
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (!ValidateGenericNodeChange(oldNode, node, report, path))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureGraphNode, operation =>
                {
                    if (IsLocal(node.elementAuthoringId))
                        operation.id = LocalIdentity(node.elementAuthoringId);
                    SetGraph(operation, target);
                    SetOptionalExisting(operation, node.elementAuthoringId, false);
                    operation.nodeType = node.typeName;
                    operation.displayName = node.displayName;
                    operation.loopStopType = node.loopStopType;
                    operation.compareType = node.compareType;
                    SetMotionConfiguration(operation, node);
                    operation.position = ToVector(node.position);
                });
            }

            HashSet<string> ignoredIds = new HashSet<string>(
                (current.nodes ?? new List<AgentSnapshotNode>())
                    .Concat(target.nodes ?? new List<AgentSnapshotNode>())
                    .Where(Ignored)
                    .Select(node => node.elementAuthoringId),
                StringComparer.Ordinal);
            AgentSnapshotGraph Filter(AgentSnapshotGraph source, bool removeDeletedIncident)
            {
                AgentSnapshotGraph clone = AgentAuthoringDocumentCodec.Clone(source);
                clone.nodes = (clone.nodes ?? new List<AgentSnapshotNode>())
                    .Where(node => !ignoredIds.Contains(node.elementAuthoringId))
                    .ToList();
                clone.flowEdges = (clone.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                    .Where(edge =>
                        !ignoredIds.Contains(edge.startElementAuthoringId) &&
                        !ignoredIds.Contains(edge.endElementAuthoringId) &&
                        (!removeDeletedIncident ||
                         !removed.Contains(edge.startElementAuthoringId) &&
                         !removed.Contains(edge.endElementAuthoringId)))
                    .ToList();
                clone.propertyEdges = (clone.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                    .Where(edge =>
                        !ignoredIds.Contains(edge.startElementAuthoringId) &&
                        !ignoredIds.Contains(edge.endElementAuthoringId) &&
                        (!removeDeletedIncident ||
                         !removed.Contains(edge.startElementAuthoringId) &&
                         !removed.Contains(edge.endElementAuthoringId)))
                    .ToList();
                return clone;
            }
            BuildGraphEdgeMutations(
                new[] { Filter(current, true) },
                new[] { Filter(target, false) },
                mutations,
                report);
        }

        static bool SameTransitionExceptActionAdmission(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            object Project(AgentSnapshotTransitionSummary transition) => new
            {
                transition.edgeAuthoringId,
                transition.fromElementAuthoringId,
                transition.toElementAuthoringId,
                transition.from,
                transition.to,
                transition.priority,
                transition.requests,
                conditionTerms = (transition.conditionTerms ?? new List<AgentSnapshotConditionTerm>())
                    .Select(term => new
                    {
                        term.kind,
                        term.negate,
                        term.request,
                        term.blackboardKey,
                        term.windowType,
                        term.targetSnapshotBlackboardKey,
                        term.compareType
                    })
                    .ToList()
            };
            return Same(Project(left), Project(right));
        }

        static bool SameTransitionEndpoints(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            return string.Equals(
                       left?.fromElementAuthoringId,
                       right?.fromElementAuthoringId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       left?.toElementAuthoringId,
                       right?.toElementAuthoringId,
                       StringComparison.Ordinal);
        }

        static bool SameTransitionExceptEndpoints(
            AgentSnapshotTransitionSummary left,
            AgentSnapshotTransitionSummary right)
        {
            object Project(AgentSnapshotTransitionSummary transition) => new
            {
                transition.edgeAuthoringId,
                transition.priority,
                transition.requests,
                transition.conditionTerms
            };
            return Same(Project(left), Project(right));
        }

        static void BuildStateBehaviorGraphMutations(
            AgentSnapshotGraph current,
            AgentSnapshotGraph target,
            ISet<string> specializedNodeIds,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            string graphPath = $"document.editable.graphs[{Escape(target.graphAuthoringId)}]";
            if (!SameGraphMetadata(current, target))
            {
                report.Error(graphPath, "state_behavior_metadata_modified", "State behavior Graph元数据不能从底层镜像直接修改。");
                return;
            }

            var oldNodes = Index(current.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var newNodes = Index(target.nodes, value => value.elementAuthoringId, graphPath + ".nodes", report);
            var removedNodes = new HashSet<string>(oldNodes.Keys.Except(newNodes.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (AgentSnapshotNode node in current.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!removedNodes.Contains(node.elementAuthoringId))
                    continue;
                Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.DeleteStateBehaviorNode, operation =>
                {
                    SetTargetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                });
            }

            foreach (AgentSnapshotNode node in target.nodes ?? new List<AgentSnapshotNode>())
            {
                if (specializedNodeIds?.Contains(node.elementAuthoringId) == true)
                    continue;
                string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]";
                if (!oldNodes.TryGetValue(node.elementAuthoringId, out AgentSnapshotNode oldNode))
                {
                    if (node.exposedProperty != null)
                    {
                        AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                        continue;
                    }
                    if (TryAddInputNodeMutation(null, node, target, mutations, report, path))
                        continue;
                    if (TryAddConditionValueNodeMutation(null, node, target, mutations, report, path))
                        continue;
                    if (!ValidateGenericNodeChange(null, node, report, path))
                        continue;
                    Add(mutations, path, AgentMutationKind.EnsureStateBehaviorNode, operation =>
                    {
                        operation.id = IsLocal(node.elementAuthoringId) ? LocalIdentity(node.elementAuthoringId) : null;
                        SetTargetGraph(operation, target);
                        operation.targetElementAuthoringId = IsLocal(node.elementAuthoringId) ? null : node.elementAuthoringId;
                        operation.nodeType = node.typeName;
                        operation.displayName = node.displayName;
                        operation.loopStopType = node.loopStopType;
                        operation.compareType = node.compareType;
                        SetMotionConfiguration(operation, node);
                        operation.position = ToVector(node.position);
                    });
                    continue;
                }
                if (Same(oldNode, node))
                    continue;
                if (node.exposedProperty != null)
                {
                    AddExposedPropertyMutation(mutations, graphPath, target, node, report);
                    continue;
                }
                if (TryAddInputNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (TryAddConditionValueNodeMutation(oldNode, node, target, mutations, report, path))
                    continue;
                if (!ValidateGenericNodeChange(oldNode, node, report, path))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureStateBehaviorNode, operation =>
                {
                    SetTargetGraph(operation, target);
                    operation.targetElementAuthoringId = node.elementAuthoringId;
                    operation.nodeType = node.typeName;
                    operation.displayName = node.displayName;
                    operation.loopStopType = node.loopStopType;
                    operation.compareType = node.compareType;
                    SetMotionConfiguration(operation, node);
                    operation.position = ToVector(node.position);
                });
            }

            var oldFlow = Index(current.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
            var newFlow = Index(target.flowEdges, value => value.elementAuthoringId, graphPath + ".flowEdges", report);
            foreach (AgentSnapshotFlowEdge edge in target.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                string path = $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]";
                if (oldFlow.TryGetValue(edge.elementAuthoringId, out AgentSnapshotFlowEdge oldEdge) && SameFlowEdge(oldEdge, edge))
                    continue;
                if (oldFlow.ContainsKey(edge.elementAuthoringId))
                {
                    Add(mutations, path, AgentMutationKind.DeleteFlowEdge, operation =>
                    {
                        SetGraph(operation, target);
                        operation.targetElementAuthoringId = edge.elementAuthoringId;
                    });
                }
                Add(mutations, path, AgentMutationKind.LinkFlow, operation =>
                {
                    SetGraph(operation, target);
                    SetLinkElement(operation, edge.startElementAuthoringId, true);
                    SetLinkElement(operation, edge.endElementAuthoringId, false);
                    operation.startPort = edge.startPort;
                    operation.endPort = edge.endPort;
                    operation.flowEdgeAuthoringId = edge.elementAuthoringId;
                });
            }
            foreach (AgentSnapshotFlowEdge edge in current.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                if (newFlow.ContainsKey(edge.elementAuthoringId) ||
                    removedNodes.Contains(edge.startElementAuthoringId) ||
                    removedNodes.Contains(edge.endElementAuthoringId))
                    continue;
                Add(mutations, $"{graphPath}.flowEdges[{Escape(edge.elementAuthoringId)}]", AgentMutationKind.DeleteFlowEdge, operation =>
                {
                    SetGraph(operation, target);
                    operation.targetElementAuthoringId = edge.elementAuthoringId;
                });
            }

            BuildPropertyEdgeMutations(current, target, mutations, report, removedNodes);
        }

        static bool ValidateGenericNodeChange(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentCompileReport report,
            string path)
        {
            if (current == null)
            {
                if (s_Capabilities.SupportsGenericMutation(target.typeName) &&
                    (target.graphReferences?.Count ?? 0) == 0 &&
                    (target.assetReferences?.Count ?? 0) == 0)
                    return true;
                report.Error(path, "authoring_capability_incomplete", "该Node没有完整typed create capability，或带有尚未闭合的Graph/Asset reference。");
                return false;
            }
            if (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return false;
            }
            if (!Same(current.graphReferences, target.graphReferences) ||
                !Same(current.assetReferences, target.assetReferences))
            {
                report.Error(path + ".properties", "authoring_capability_incomplete", "该Node的Graph或Asset reference发生变化，但没有对应的typed configure capability。");
                return false;
            }
            return true;
        }

        static bool TryAddInputNodeMutation(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentSnapshotGraph graph,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (!s_Capabilities.TryGetKind(target.typeName, out string kind))
                return false;
            bool request = string.Equals(kind, "character-action-request", StringComparison.Ordinal);
            if (!IsInputNodeKind(kind))
                return false;
            if (current != null && !string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return true;
            }
            if (current != null &&
                (!Same(current.graphReferences, target.graphReferences) ||
                 !Same(current.assetReferences, target.assetReferences)))
            {
                report.Error(path + ".properties", "input_node_reference_modified", "Input Node不能携带Graph或Asset reference变化。");
                return true;
            }
            string binding = request ? target.requestId : target.inputId;
            if (string.IsNullOrWhiteSpace(binding))
            {
                report.Error(path + ".properties", "input_binding_required", $"{kind}必须声明非空{(request ? "requestId" : "inputId")}。");
                return true;
            }
            Add(mutations, path, AgentMutationKind.EnsureInputNode, operation =>
            {
                if (IsLocal(target.elementAuthoringId))
                    operation.id = LocalIdentity(target.elementAuthoringId);
                SetGraph(operation, graph);
                SetOptionalExisting(operation, target.elementAuthoringId, false);
                operation.nodeType = target.typeName;
                operation.displayName = target.displayName;
                operation.inputId = binding;
                operation.position = ToVector(target.position);
            });
            return true;
        }

        static bool IsInputNodeKind(string kind)
        {
            return string.Equals(kind, "character-action-request", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-bool", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-float", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-vector2", StringComparison.Ordinal) ||
                   string.Equals(kind, "character-input-vector2-magnitude", StringComparison.Ordinal);
        }

        static bool TryAddConditionValueNodeMutation(
            AgentSnapshotNode current,
            AgentSnapshotNode target,
            AgentSnapshotGraph graph,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (!s_Capabilities.TryGetKind(target.typeName, out string kind) ||
                !TryResolveConditionValueConfiguration(kind, out AgentConditionValueNodeConfigurationKind configuration))
                return false;
            if (current != null && !string.Equals(current.typeName, target.typeName, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "node_kind_changed", "Node kind不能原地改变。");
                return true;
            }
            if (current != null &&
                (!Same(current.graphReferences, target.graphReferences) ||
                 !Same(current.assetReferences, target.assetReferences)))
            {
                report.Error(path + ".properties", "condition_value_reference_modified", "Condition Value Node不能携带未登记的Graph或Asset reference变化。");
                return true;
            }
            Add(mutations, path, AgentMutationKind.EnsureConditionValueNode, operation =>
            {
                if (IsLocal(target.elementAuthoringId))
                    operation.id = LocalIdentity(target.elementAuthoringId);
                SetGraph(operation, graph);
                SetOptionalExisting(operation, target.elementAuthoringId, false);
                operation.nodeType = target.typeName;
                operation.displayName = target.displayName;
                operation.conditionValueConfiguration = configuration.ToString();
                operation.position = ToVector(target.position);
                switch (configuration)
                {
                    case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration:
                        SetDeclarationReference(operation, target.blackboardDeclarationId, false);
                        break;
                    case AgentConditionValueNodeConfigurationKind.StateExitCause:
                        operation.stateExitCause = target.stateExitCause;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionContext:
                        operation.actionContext = target.actionContextId;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionWindow:
                        operation.windowType = target.windowType;
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionAdmission:
                        operation.actionProfile = target.actionProfileId;
                        SetTargetSnapshotDeclarationReference(operation, target.targetSnapshotBlackboardDeclarationId);
                        break;
                }
            });
            return true;
        }

        static bool TryResolveConditionValueConfiguration(
            string kind,
            out AgentConditionValueNodeConfigurationKind configuration)
        {
            configuration = kind switch
            {
                "character-move-facing-angle" => AgentConditionValueNodeConfigurationKind.None,
                "pipeline-blackboard-bool" => AgentConditionValueNodeConfigurationKind.BlackboardDeclaration,
                "pipeline-blackboard-float" => AgentConditionValueNodeConfigurationKind.BlackboardDeclaration,
                "state-exit-cause" => AgentConditionValueNodeConfigurationKind.StateExitCause,
                "action-context-active" => AgentConditionValueNodeConfigurationKind.ActionContext,
                "action-window-active" => AgentConditionValueNodeConfigurationKind.ActionWindow,
                "can-activate-action" => AgentConditionValueNodeConfigurationKind.ActionAdmission,
                _ => AgentConditionValueNodeConfigurationKind.None
            };
            return string.Equals(kind, "character-move-facing-angle", StringComparison.Ordinal) ||
                   string.Equals(kind, "pipeline-blackboard-bool", StringComparison.Ordinal) ||
                   string.Equals(kind, "pipeline-blackboard-float", StringComparison.Ordinal) ||
                   string.Equals(kind, "state-exit-cause", StringComparison.Ordinal) ||
                   string.Equals(kind, "action-context-active", StringComparison.Ordinal) ||
                   string.Equals(kind, "action-window-active", StringComparison.Ordinal) ||
                   string.Equals(kind, "can-activate-action", StringComparison.Ordinal);
        }

        static bool SameGraphMetadata(AgentSnapshotGraph left, AgentSnapshotGraph right)
        {
            return Same(
                new
                {
                    left.graphAuthoringId,
                    left.path,
                    left.name,
                    left.kind,
                    left.ownership,
                    left.ownerElementAuthoringId,
                    left.referenceKey,
                    left.sharedAssetPath,
                    left.routes
                },
                new
                {
                    right.graphAuthoringId,
                    right.path,
                    right.name,
                    right.kind,
                    right.ownership,
                    right.ownerElementAuthoringId,
                    right.referenceKey,
                    right.sharedAssetPath,
                    right.routes
                });
        }

        static void BuildTimelineMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            const string timelinesPath = "document.editable.timelines";
            var oldTimelines = Index(current.timelines, value => value.timelineAuthoringId, timelinesPath, report);
            var newTimelines = Index(target.timelines, value => value.timelineAuthoringId, timelinesPath, report);
            var targetNodeIds = new HashSet<string>(
                (target.graphs ?? new List<AgentSnapshotGraph>())
                    .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                    .Select(node => node.elementAuthoringId),
                StringComparer.Ordinal);
            var currentTreeClips = Index(
                current.timelineTreeClips,
                value => value.clipAuthoringId,
                "current.timelineTreeClips",
                report);
            var targetTreeClips = Index(
                target.timelineTreeClips,
                value => value.clipAuthoringId,
                "document.editable.timelineTreeClips",
                report);
            var removedTimelineIds = new HashSet<string>(oldTimelines.Keys.Except(newTimelines.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            var removedTrackKeys = new HashSet<string>(StringComparer.Ordinal);
            var localTimelineIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
            var localTrackIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
            var localClipIdentities = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (AgentSnapshotTimeline timeline in current.timelines ?? new List<AgentSnapshotTimeline>())
            {
                if (!removedTimelineIds.Contains(timeline.timelineAuthoringId))
                    continue;
                string path = $"{timelinesPath}[{Escape(timeline.timelineAuthoringId)}]";
                if (timeline.callSites == null || timeline.callSites.Count == 0 ||
                    timeline.callSites.Any(callSite => targetNodeIds.Contains(callSite.nodeAuthoringId)))
                    report.Error(path, "timeline_delete_requires_owner_node", "Timeline只能通过删除全部拥有它的Timeline节点级联删除。");
            }

            foreach (AgentSnapshotTimeline timeline in target.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string path = $"{timelinesPath}[{Escape(timeline.timelineAuthoringId)}]";
                if (!oldTimelines.TryGetValue(timeline.timelineAuthoringId, out AgentSnapshotTimeline oldTimeline))
                {
                    AgentSnapshotTimelineCallSite callSite = timeline.callSites?.Count == 1
                        ? timeline.callSites[0]
                        : null;
                    List<AgentSnapshotTimelineBindingSummary> bindings = (target.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
                        .SelectMany(machine => machine.states ?? new List<AgentSnapshotStateSummary>())
                        .SelectMany(state => state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                        .Where(binding =>
                            callSite != null &&
                            string.Equals(binding.nodeAuthoringId, callSite.nodeAuthoringId, StringComparison.Ordinal) &&
                            string.Equals(binding.timelineAuthoringId, timeline.timelineAuthoringId, StringComparison.Ordinal))
                        .ToList();
                    if (!IsLocal(timeline.timelineAuthoringId) ||
                        callSite == null ||
                        !IsLocal(callSite.nodeAuthoringId) ||
                        !targetNodeIds.Contains(callSite.nodeAuthoringId) ||
                        bindings.Count != 1 ||
                        !string.Equals(bindings[0].ownership, AgentTimelineOwnership.Inline.ToString(), StringComparison.Ordinal))
                    {
                        report.Error(path, "timeline_create_requires_inline_node", "新增Timeline必须由同一事务中的唯一local Inline TimelineNode拥有，并在controller、graph与callSite中一致声明。");
                        continue;
                    }
                    string plannedIdentity = LocalIdentity(timeline.timelineAuthoringId);
                    localTimelineIdentities[timeline.timelineAuthoringId] = plannedIdentity;
                    Add(mutations, path, AgentMutationKind.EnsureInlineTimeline, operation =>
                    {
                        operation.id = plannedIdentity;
                        operation.targetPlannedIdentity = LocalIdentity(callSite.nodeAuthoringId);
                        operation.displayName = timeline.name;
                    });
                    oldTimeline = new AgentSnapshotTimeline
                    {
                        timelineAuthoringId = timeline.timelineAuthoringId,
                        name = timeline.name,
                        callSites = AgentAuthoringDocumentCodec.Clone(timeline.callSites),
                        tracks = new List<AgentSnapshotTimelineTrack>()
                    };
                }
                if (!string.Equals(oldTimeline.name, timeline.name, StringComparison.Ordinal) ||
                    !Same(oldTimeline.callSites, timeline.callSites))
                {
                    report.Error(path, "timeline_metadata_modified", "Timeline名称与调用点只能通过拥有它的Timeline节点修改。");
                    continue;
                }

                var oldTracks = Index(oldTimeline.tracks, value => value.trackAuthoringId, path + ".tracks", report);
                var newTracks = Index(timeline.tracks, value => value.trackAuthoringId, path + ".tracks", report);
                var removedTracks = (oldTimeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                    .Where(track => !newTracks.ContainsKey(track.trackAuthoringId))
                    .ToList();
                foreach (AgentSnapshotTimelineTrack track in removedTracks)
                {
                    removedTrackKeys.Add(timeline.timelineAuthoringId + "\0" + track.trackAuthoringId);
                    Add(mutations, $"{path}.tracks[{Escape(track.trackAuthoringId)}]", AgentMutationKind.DeleteTimelineTrack, operation =>
                    {
                        operation.timelineAuthoringId = timeline.timelineAuthoringId;
                        operation.trackAuthoringId = track.trackAuthoringId;
                    });
                }

                foreach (AgentSnapshotTimelineTrack track in timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    string trackPath = $"{path}.tracks[{Escape(track.trackAuthoringId)}]";
                    if (!oldTracks.TryGetValue(track.trackAuthoringId, out AgentSnapshotTimelineTrack oldTrack))
                    {
                        bool motionCurveTrack = track.typeName?.EndsWith("MotionCurveTrack", StringComparison.Ordinal) == true;
                        if ((!track.motionWarpTrack && !motionCurveTrack) || !IsLocal(track.trackAuthoringId))
                        {
                            report.Error(trackPath, "timeline_track_create_unsupported", "当前正式Document能力只允许创建local MotionCurve Track或MotionWarp Track；其它Track必须先补齐对应的typed capability。");
                            continue;
                        }
                        string plannedIdentity = LocalIdentity(track.trackAuthoringId);
                        localTrackIdentities[track.trackAuthoringId] = plannedIdentity;
                        BuildTimelineTrackMutations(
                            timeline.timelineAuthoringId,
                            null,
                            track,
                            currentTreeClips,
                            targetTreeClips,
                            localTimelineIdentities,
                            localTrackIdentities,
                            localClipIdentities,
                            mutations,
                            report,
                            trackPath);
                        continue;
                    }
                    int expectedIndex = oldTrack.index - removedTracks.Count(value => value.index < oldTrack.index);
                    if (track.index != expectedIndex)
                    {
                        report.Error(trackPath + ".index", "timeline_track_reorder_unsupported", "Track顺序只能由删除前序Track自然收拢，当前正式API不支持任意重排。");
                        continue;
                    }
                    BuildTimelineTrackMutations(
                        timeline.timelineAuthoringId,
                        oldTrack,
                        track,
                        currentTreeClips,
                        targetTreeClips,
                        localTimelineIdentities,
                        localTrackIdentities,
                        localClipIdentities,
                        mutations,
                        report,
                        trackPath);
                }
            }

        }

        static void BuildTimelineTrackMutations(
            string timelineId,
            AgentSnapshotTimelineTrack current,
            AgentSnapshotTimelineTrack target,
            IReadOnlyDictionary<string, AgentSnapshotTimelineTreeClip> currentTreeClips,
            IReadOnlyDictionary<string, AgentSnapshotTimelineTreeClip> targetTreeClips,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current != null &&
                (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal) ||
                 current.motionWarpTrack != target.motionWarpTrack))
            {
                report.Error(path, "timeline_track_kind_changed", "Track kind不能原地改变；请删除旧Track并创建受支持的新Track。");
                return;
            }

            bool motionCurveTrack = target.typeName?.EndsWith("MotionCurveTrack", StringComparison.Ordinal) == true;
            if (motionCurveTrack &&
                (current == null || !string.Equals(current.name, target.name, StringComparison.Ordinal)))
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionCurveTrack, operation =>
                {
                    if (current == null && IsLocal(target.trackAuthoringId))
                        operation.id = localTrackIdentities.TryGetValue(target.trackAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.trackAuthoringId);
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    if (current != null && !IsLocal(target.trackAuthoringId))
                        operation.trackAuthoringId = target.trackAuthoringId;
                    operation.displayName = target.name;
                });
            }
            else if (target.motionWarpTrack &&
                (current == null || !string.Equals(current.name, target.name, StringComparison.Ordinal)))
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionWarpTrack, operation =>
                {
                    if (current == null && IsLocal(target.trackAuthoringId))
                        operation.id = localTrackIdentities.TryGetValue(target.trackAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.trackAuthoringId);
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    if (current != null && !IsLocal(target.trackAuthoringId))
                        operation.trackAuthoringId = target.trackAuthoringId;
                    operation.displayName = target.name;
                });
            }
            else if (current != null && !string.Equals(current.name, target.name, StringComparison.Ordinal))
            {
                report.Error(path + ".name", "timeline_track_name_unsupported", "当前Track类型没有正式rename Mutation。");
            }

            bool isAnimationTrack = !string.IsNullOrEmpty(target.animationChannelId) ||
                                    target.typeName?.EndsWith("AnimationTrack", StringComparison.Ordinal) == true;
            if (isAnimationTrack)
            {
                if (current == null || !string.Equals(current.animationChannelId, target.animationChannelId, StringComparison.Ordinal))
                {
                    Add(mutations, path + ".animationChannelId", AgentMutationKind.ConfigureAnimationTrackChannel, operation =>
                    {
                        operation.timelineAuthoringId = timelineId;
                        SetTimelineTrackReference(operation, target.trackAuthoringId, localTrackIdentities);
                        operation.animationChannelId = target.animationChannelId;
                    });
                }
                if (current == null ||
                    !SameAnimationTrackSync(current, target))
                {
                    Add(mutations, path + ".markerSync", AgentMutationKind.ConfigureAnimationTrackMarkerSync, operation =>
                    {
                        operation.timelineAuthoringId = timelineId;
                        SetTimelineTrackReference(operation, target.trackAuthoringId, localTrackIdentities);
                        operation.animationSyncMode = target.syncMode;
                        operation.animationSyncGroupId = target.syncGroupId;
                        operation.animationMarkerSequenceTopology = target.sequenceTopology;
                        operation.animationMarkerSyncRole = target.syncRole;
                    });
                }
                BuildTimelineMarkerMutations(
                    timelineId,
                    current,
                    target,
                    localTrackIdentities,
                    mutations,
                    report,
                    path);
            }
            else if (current != null &&
                     (!SameOptionalText(current.animationChannelId, target.animationChannelId) ||
                      !SameOptionalText(current.syncMode, target.syncMode) ||
                      !SameOptionalText(current.syncGroupId, target.syncGroupId) ||
                      !SameOptionalText(current.sequenceTopology, target.sequenceTopology) ||
                      !SameOptionalText(current.syncRole, target.syncRole) ||
                      !Same(current.markers, target.markers) ||
                      !SameList(current.directedMarkerPairs, target.directedMarkerPairs)))
            {
                report.Error(path, "timeline_track_animation_fields_invalid", "非AnimationTrack不能携带Animation channel、marker或sync配置。");
            }

            var oldClips = Index(current?.clips, value => value.clipAuthoringId, path + ".clips", report);
            var newClips = Index(target.clips, value => value.clipAuthoringId, path + ".clips", report);
            foreach (AgentSnapshotTimelineClip removed in current?.clips ?? new List<AgentSnapshotTimelineClip>())
            {
                if (newClips.ContainsKey(removed.clipAuthoringId))
                    continue;
                Add(mutations, $"{path}.clips[{Escape(removed.clipAuthoringId)}]", AgentMutationKind.DeleteTimelineClip, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    operation.trackAuthoringId = target.trackAuthoringId;
                    operation.clipAuthoringId = removed.clipAuthoringId;
                });
            }

            foreach (AgentSnapshotTimelineClip clip in target.clips ?? new List<AgentSnapshotTimelineClip>())
            {
                oldClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineClip oldClip);
                currentTreeClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineTreeClip oldTreeClip);
                targetTreeClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineTreeClip treeClip);
                BuildTimelineClipMutations(
                    timelineId,
                    target,
                    oldClip,
                    clip,
                    oldTreeClip,
                    treeClip,
                    localTimelineIdentities,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    report,
                    $"{path}.clips[{Escape(clip.clipAuthoringId)}]");
            }
        }

        static void BuildTimelineMarkerMutations(
            string timelineId,
            AgentSnapshotTimelineTrack current,
            AgentSnapshotTimelineTrack target,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            List<string> expectedPairs = BuildDirectedMarkerPairs(target);
            if (!SameList(expectedPairs, target.directedMarkerPairs))
            {
                report.Error(path + ".directedMarkerPairs", "timeline_marker_pairs_invalid", "directedMarkerPairs必须与目标Marker的frame顺序和sequenceTopology一致。");
                return;
            }
            var oldMarkers = Index(current?.markers, value => value.authoringId, path + ".markers", report);
            var newMarkers = Index(target.markers, value => value.authoringId, path + ".markers", report);
            foreach (AgentSnapshotAnimationMarker marker in target.markers ?? new List<AgentSnapshotAnimationMarker>())
            {
                string markerPath = $"{path}.markers[{Escape(marker.authoringId)}]";
                oldMarkers.TryGetValue(marker.authoringId, out AgentSnapshotAnimationMarker oldMarker);
                if (oldMarker == null && !IsLocal(marker.authoringId))
                {
                    report.Error(markerPath, "timeline_marker_identity_unknown", "新增Marker必须使用local: identity。");
                    continue;
                }
                if (oldMarker != null &&
                    string.Equals(oldMarker.markerId, marker.markerId, StringComparison.Ordinal) &&
                    oldMarker.frame == marker.frame)
                    continue;
                Add(mutations, markerPath, AgentMutationKind.EnsureAnimationSyncMarker, operation =>
                {
                    if (IsLocal(marker.authoringId))
                        operation.id = LocalIdentity(marker.authoringId);
                    else
                        operation.markerAuthoringId = marker.authoringId;
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineTrackReference(operation, target.trackAuthoringId, localTrackIdentities);
                    operation.markerId = marker.markerId;
                    operation.markerFrame = marker.frame;
                });
            }
            foreach (AgentSnapshotAnimationMarker marker in current?.markers ?? new List<AgentSnapshotAnimationMarker>())
            {
                if (newMarkers.ContainsKey(marker.authoringId))
                    continue;
                Add(mutations, $"{path}.markers[{Escape(marker.authoringId)}]", AgentMutationKind.DeleteAnimationSyncMarker, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineTrackReference(operation, target.trackAuthoringId, localTrackIdentities);
                    operation.markerAuthoringId = marker.authoringId;
                });
            }
        }

        static List<string> BuildDirectedMarkerPairs(AgentSnapshotTimelineTrack track)
        {
            List<AgentSnapshotAnimationMarker> markers = (track.markers ?? new List<AgentSnapshotAnimationMarker>())
                .OrderBy(value => value.frame)
                .ThenBy(value => value.authoringId, StringComparer.Ordinal)
                .ToList();
            var result = new List<string>();
            for (int i = 1; i < markers.Count; i++)
                result.Add(AnimationMarkerSyncAuthoring.PairKey(markers[i - 1].markerId, markers[i].markerId));
            if (string.Equals(track.sequenceTopology, AnimationMarkerSequenceTopology.Cyclic.ToString(), StringComparison.Ordinal) &&
                markers.Count > 1)
                result.Add(AnimationMarkerSyncAuthoring.PairKey(markers[markers.Count - 1].markerId, markers[0].markerId));
            return result;
        }

        static void BuildTimelineClipMutations(
            string timelineId,
            AgentSnapshotTimelineTrack track,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            AgentSnapshotTimelineTreeClip currentTreeClip,
            AgentSnapshotTimelineTreeClip targetTreeClip,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current != null &&
                (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal) ||
                 current.motionWarpClip != target.motionWarpClip))
            {
                report.Error(path, "timeline_clip_kind_changed", "Clip kind不能原地改变；请删除旧Clip并创建受支持的新Clip。");
                return;
            }
            if (current == null && !IsLocal(target.clipAuthoringId))
            {
                report.Error(path, "timeline_clip_identity_unknown", "新增Clip必须使用local: identity。");
                return;
            }

            bool treeClip = targetTreeClip != null || target.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) == true;
            bool motionCurveClip = target.typeName?.EndsWith("MotionCurveClip", StringComparison.Ordinal) == true;
            if (treeClip)
            {
                if (targetTreeClip == null)
                {
                    report.Error(path, "timeline_tree_clip_summary_missing", "TreeClip必须在controller分片提供对应的目标状态。");
                    return;
                }
                if (!string.Equals(targetTreeClip.ownership, "Inline", StringComparison.OrdinalIgnoreCase))
                {
                    report.Error(path + ".ownership", "timeline_tree_clip_inline_required", "当前正式TreeClip authoring只接受Inline ownership。");
                    return;
                }
                bool ensure = current == null ||
                              current.startFrame != target.startFrame ||
                              current.endFrame != target.endFrame ||
                              currentTreeClip == null ||
                              !string.Equals(currentTreeClip.phase, targetTreeClip.phase, StringComparison.Ordinal) ||
                              !string.Equals(currentTreeClip.ownership, targetTreeClip.ownership, StringComparison.Ordinal);
                if (ensure)
                {
                    Add(mutations, path, AgentMutationKind.EnsureTimelineTreeClip, operation =>
                    {
                        if (IsLocal(target.clipAuthoringId))
                        {
                            operation.id = LocalIdentity(target.clipAuthoringId);
                            localClipIdentities[target.clipAuthoringId] = operation.id;
                        }
                        operation.timelineAuthoringId = timelineId;
                        if (!IsLocal(track.trackAuthoringId))
                            operation.trackAuthoringId = track.trackAuthoringId;
                        operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                        operation.startFrame = target.startFrame;
                        operation.endFrame = target.endFrame;
                        operation.timelinePhase = targetTreeClip.phase;
                    });
                }
                BuildTreeClipWriteMutations(
                    timelineId,
                    track.trackAuthoringId,
                    target.clipAuthoringId,
                    currentTreeClip,
                    targetTreeClip,
                    localClipIdentities,
                    mutations,
                    report,
                    path);
            }
            else if (motionCurveClip)
            {
                BuildMotionCurveClipMutations(
                    timelineId,
                    track.trackAuthoringId,
                    current,
                    target,
                    localTimelineIdentities,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    path);
            }
            else if (target.motionWarpClip)
            {
                BuildMotionWarpClipMutations(
                    timelineId,
                    track.trackAuthoringId,
                    current,
                    target,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    report,
                    path);
            }
            else if (current == null)
            {
                report.Error(path, "timeline_clip_create_unsupported", "当前Clip类型没有正式create capability。");
            }
            else
            {
                if (!SameGenericClipConfiguration(current, target))
                {
                    report.Error(path, "timeline_clip_configuration_unsupported", "该Clip的资产引用或typed配置发生变化，但当前类型没有对应的正式Mutation。");
                    return;
                }
                int currentDuration = current.endFrame - current.startFrame;
                int targetDuration = target.endFrame - target.startFrame;
                if ((current.startFrame != target.startFrame || current.endFrame != target.endFrame) &&
                    currentDuration == targetDuration)
                {
                    Add(mutations, path, AgentMutationKind.MoveTimelineClip, operation =>
                    {
                        operation.timelineAuthoringId = timelineId;
                        operation.trackAuthoringId = track.trackAuthoringId;
                        operation.clipAuthoringId = target.clipAuthoringId;
                        operation.frameOffset = target.startFrame - current.startFrame;
                    });
                }
                else if (currentDuration != targetDuration)
                {
                    report.Error(path, "timeline_clip_resize_unsupported", "当前Clip类型没有正式resize Mutation。");
                }
            }

            if ((current == null && (target.selfEaseInFrame != 0 || target.selfEaseOutFrame != 0)) ||
                current != null &&
                (current.selfEaseInFrame != target.selfEaseInFrame ||
                 current.selfEaseOutFrame != target.selfEaseOutFrame))
            {
                Add(mutations, path + ".ease", AgentMutationKind.ConfigureTimelineClipEase, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineClipReference(operation, track.trackAuthoringId, target.clipAuthoringId, localClipIdentities);
                    operation.selfEaseInFrame = target.selfEaseInFrame;
                    operation.selfEaseOutFrame = target.selfEaseOutFrame;
                });
            }

            BuildTimelineCurveMutations(
                timelineId,
                track.trackAuthoringId,
                current,
                target,
                localTimelineIdentities,
                localClipIdentities,
                mutations,
                report,
                path);
        }

        static bool SameGenericClipConfiguration(
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target)
        {
            return SameOptionalText(current.animationClipAssetPath, target.animationClipAssetPath) &&
                   SameOptionalText(current.animationClipAssetGuid, target.animationClipAssetGuid) &&
                   SameOptionalText(current.curveId, target.curveId) &&
                   current.curveEndFrame == target.curveEndFrame &&
                   SameOptionalText(current.motionSpace, target.motionSpace) &&
                   SameOptionalText(current.motionChannel, target.motionChannel) &&
                   SameOptionalText(current.motionBlendMode, target.motionBlendMode) &&
                   current.motionPriority == target.motionPriority &&
                   current.consumeLowerChannels == target.consumeLowerChannels &&
                   SameOptionalText(current.sourceMotionClipAuthoringId, target.sourceMotionClipAuthoringId) &&
                   SameOptionalText(current.sourceMotionClipPath, target.sourceMotionClipPath) &&
                   SameOptionalText(current.translationMode, target.translationMode) &&
                   SameOptionalText(current.targetOffsetSpace, target.targetOffsetSpace) &&
                   SameOptionalText(current.rotationMode, target.rotationMode) &&
                   SameOptionalText(current.rotationMethod, target.rotationMethod) &&
                   Same(current.targetPlanarOffset, target.targetPlanarOffset) &&
                   current.targetYawOffsetDegrees.Equals(target.targetYawOffsetDegrees) &&
                   current.maxTotalPositionCorrection.Equals(target.maxTotalPositionCorrection) &&
                   current.maxTotalYawCorrectionDegrees.Equals(target.maxTotalYawCorrectionDegrees) &&
                   current.maximumYawRateDegreesPerSecond.Equals(target.maximumYawRateDegreesPerSecond) &&
                   SameOptionalText(current.limitPolicy, target.limitPolicy);
        }

        static void BuildTreeClipWriteMutations(
            string timelineId,
            string trackId,
            string clipId,
            AgentSnapshotTimelineTreeClip current,
            AgentSnapshotTimelineTreeClip target,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            List<AgentSnapshotTreeClipWrite> oldWrites = current?.writes ?? new List<AgentSnapshotTreeClipWrite>();
            List<AgentSnapshotTreeClipWrite> writes = target?.writes ?? new List<AgentSnapshotTreeClipWrite>();
            if (writes.Count > 1)
            {
                report.Error(path + ".writes", "timeline_tree_clip_writes_unsupported", "当前正式TreeClip只支持一个Bool Blackboard write。");
                return;
            }
            if (writes.Count == 0)
            {
                if (oldWrites.Count > 0)
                    report.Error(path + ".writes", "timeline_tree_clip_write_delete_unsupported", "当前正式API不支持单独删除TreeClip write。");
                return;
            }
            if (oldWrites.Count == 1 && Same(oldWrites[0], writes[0]))
                return;
            AgentSnapshotTreeClipWrite write = writes[0];
            Add(mutations, path + ".writes[0]", AgentMutationKind.EnsureTreeClipBlackboardWrite, operation =>
            {
                operation.timelineAuthoringId = timelineId;
                if (!IsLocal(trackId))
                    operation.trackAuthoringId = trackId;
                if (IsLocal(clipId))
                    operation.clipPlannedIdentity = localClipIdentities.TryGetValue(clipId, out string plannedIdentity)
                        ? plannedIdentity
                        : LocalIdentity(clipId);
                else
                    operation.clipAuthoringId = clipId;
                if (IsLocal(write.declarationId))
                    operation.declarationPlannedIdentity = LocalIdentity(write.declarationId);
                else
                    operation.declarationAuthoringId = write.declarationId;
            });
        }

        static void BuildMotionCurveClipMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            string path)
        {
            if (current == null || current.startFrame != target.startFrame || current.endFrame != target.endFrame)
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionCurveClip, operation =>
                {
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.id = LocalIdentity(target.clipAuthoringId);
                        localClipIdentities[target.clipAuthoringId] = operation.id;
                    }
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineTrackReference(operation, trackId, localTrackIdentities);
                    operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                });
            }
            if (current == null || !SameMotionCurveConfiguration(current, target))
            {
                Add(mutations, path + ".motion", AgentMutationKind.ConfigureMotionCurveClip, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                    operation.curveId = target.curveId;
                    operation.curveEndFrame = target.curveEndFrame;
                    operation.motionSpace = target.motionSpace;
                    operation.motionChannel = target.motionChannel;
                    operation.motionBlendMode = target.motionBlendMode;
                    operation.motionPriority = target.motionPriority;
                    operation.consumeLowerChannels = target.consumeLowerChannels;
                });
            }
        }

        static bool SameMotionCurveConfiguration(AgentSnapshotTimelineClip left, AgentSnapshotTimelineClip right)
        {
            return string.Equals(left.curveId, right.curveId, StringComparison.Ordinal) &&
                   left.curveEndFrame == right.curveEndFrame &&
                   string.Equals(left.motionSpace, right.motionSpace, StringComparison.Ordinal) &&
                   string.Equals(left.motionChannel, right.motionChannel, StringComparison.Ordinal) &&
                   string.Equals(left.motionBlendMode, right.motionBlendMode, StringComparison.Ordinal) &&
                   left.motionPriority == right.motionPriority &&
                   left.consumeLowerChannels == right.consumeLowerChannels;
        }

        static void BuildMotionWarpClipMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current == null || current.startFrame != target.startFrame || current.endFrame != target.endFrame)
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionWarpClip, operation =>
                {
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.id = LocalIdentity(target.clipAuthoringId);
                        localClipIdentities[target.clipAuthoringId] = operation.id;
                    }
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineTrackReference(operation, trackId, localTrackIdentities);
                    operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                });
            }
            if (current == null ||
                !string.Equals(current.sourceMotionClipAuthoringId, target.sourceMotionClipAuthoringId, StringComparison.Ordinal))
            {
                Add(mutations, path + ".source", AgentMutationKind.ConfigureMotionWarpSource, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.sourceMotionClipAuthoringId = target.sourceMotionClipAuthoringId;
                });
            }
            if (current == null || !SameMotionWarpParameters(current, target))
            {
                Add(mutations, path + ".parameters", AgentMutationKind.ConfigureMotionWarpParameters, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.translationMode = target.translationMode;
                    operation.targetOffsetSpace = target.targetOffsetSpace;
                    operation.rotationMode = target.rotationMode;
                    operation.rotationMethod = target.rotationMethod;
                    operation.targetPlanarOffset = ToVector(target.targetPlanarOffset);
                    operation.targetYawOffsetDegrees = target.targetYawOffsetDegrees;
                    operation.maxTotalPositionCorrection = target.maxTotalPositionCorrection;
                    operation.maxTotalYawCorrectionDegrees = target.maxTotalYawCorrectionDegrees;
                    operation.maximumYawRateDegreesPerSecond = target.maximumYawRateDegreesPerSecond;
                    operation.limitPolicy = target.limitPolicy;
                    operation.positionProgressCurve = CurveKeys(target, "motion-warp.position-progress");
                    operation.yawProgressCurve = CurveKeys(target, "motion-warp.yaw-progress");
                });
            }
        }

        static bool SameMotionWarpParameters(AgentSnapshotTimelineClip left, AgentSnapshotTimelineClip right)
        {
            return string.Equals(left.translationMode, right.translationMode, StringComparison.Ordinal) &&
                   string.Equals(left.targetOffsetSpace, right.targetOffsetSpace, StringComparison.Ordinal) &&
                   string.Equals(left.rotationMode, right.rotationMode, StringComparison.Ordinal) &&
                   string.Equals(left.rotationMethod, right.rotationMethod, StringComparison.Ordinal) &&
                   Same(left.targetPlanarOffset, right.targetPlanarOffset) &&
                   left.targetYawOffsetDegrees.Equals(right.targetYawOffsetDegrees) &&
                   left.maxTotalPositionCorrection.Equals(right.maxTotalPositionCorrection) &&
                   left.maxTotalYawCorrectionDegrees.Equals(right.maxTotalYawCorrectionDegrees) &&
                   left.maximumYawRateDegreesPerSecond.Equals(right.maximumYawRateDegreesPerSecond) &&
                   string.Equals(left.limitPolicy, right.limitPolicy, StringComparison.Ordinal);
        }

        static List<AgentAnimationCurveKey> CurveKeys(AgentSnapshotTimelineClip clip, string channelId)
        {
            AgentSnapshotTimelineCurveChannel channel = (clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                .FirstOrDefault(value => string.Equals(value.channelId, channelId, StringComparison.Ordinal));
            if (channel?.keys != null && channel.keys.Count >= 2)
                return channel.keys;
            return new List<AgentAnimationCurveKey>
            {
                new AgentAnimationCurveKey { time = 0f, value = 0f },
                new AgentAnimationCurveKey { time = 1f, value = 1f }
            };
        }

        static void SetTimelineTrackReference(
            AgentMutationDraft operation,
            string trackId,
            IReadOnlyDictionary<string, string> localTrackIdentities)
        {
            if (IsLocal(trackId))
                operation.trackPlannedIdentity = localTrackIdentities.TryGetValue(trackId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(trackId);
            else
                operation.trackAuthoringId = trackId;
        }

        static void SetTimelineReference(
            AgentMutationDraft operation,
            string timelineId,
            IReadOnlyDictionary<string, string> localTimelineIdentities)
        {
            if (IsLocal(timelineId))
                operation.timelinePlannedIdentity = localTimelineIdentities.TryGetValue(timelineId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(timelineId);
            else
                operation.timelineAuthoringId = timelineId;
        }

        static void SetTimelineClipReference(
            AgentMutationDraft operation,
            string trackId,
            string clipId,
            IDictionary<string, string> localClipIdentities)
        {
            if (!IsLocal(trackId))
                operation.trackAuthoringId = trackId;
            if (IsLocal(clipId))
                operation.clipPlannedIdentity = localClipIdentities.TryGetValue(clipId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(clipId);
            else
                operation.clipAuthoringId = clipId;
        }

        static void BuildTimelineCurveMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            var oldChannels = Index(current?.curveChannels, value => value.channelId, path + ".curves", report);
            var newChannels = Index(target.curveChannels, value => value.channelId, path + ".curves", report);
            foreach (AgentSnapshotTimelineCurveChannel channel in target.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
            {
                if (oldChannels.TryGetValue(channel.channelId, out AgentSnapshotTimelineCurveChannel oldChannel) &&
                    Same(oldChannel, channel))
                    continue;
                Add(mutations, $"{path}.curves[{Escape(channel.channelId)}]", AgentMutationKind.ConfigureTimelineCurveChannel, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    operation.trackAuthoringId = IsLocal(trackId) ? string.Empty : trackId;
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.clipPlannedIdentity = localClipIdentities.TryGetValue(target.clipAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.clipAuthoringId);
                    }
                    else
                        operation.clipAuthoringId = target.clipAuthoringId;
                    operation.curveChannelId = channel.channelId;
                    operation.curve = new AgentAnimationCurvePayload
                    {
                        preWrapMode = channel.preWrapMode,
                        postWrapMode = channel.postWrapMode,
                        keys = channel.keys
                    };
                });
            }
            foreach (string removed in oldChannels.Keys.Except(newChannels.Keys, StringComparer.Ordinal))
                report.Error($"{path}.curves[{Escape(removed)}]", "timeline_curve_delete_unsupported", "Registered Curve Channel不能从Document删除，只能完整替换payload。");
        }

        static List<AgentConditionGroup> ToConditionGroups(IReadOnlyList<AgentSnapshotConditionTerm> terms)
        {
            var group = new AgentConditionGroup();
            foreach (AgentSnapshotConditionTerm term in terms ?? Array.Empty<AgentSnapshotConditionTerm>())
            {
                group.terms.Add(new AgentConditionTerm
                {
                    kind = term.kind,
                    negate = term.negate,
                    request = term.request,
                    blackboardKey = term.blackboardKey,
                    windowType = term.windowType,
                    actionProfile = term.actionProfile,
                    actionProfileAssetPath = term.actionProfileAssetPath,
                    actionProfileAssetGuid = term.actionProfileAssetGuid,
                    targetSnapshotBlackboardKey = term.targetSnapshotBlackboardKey,
                    compareType = term.compareType
                });
            }
            return new List<AgentConditionGroup> { group };
        }

        static AgentMutationKind? ResolveAINodeMutationKind(AgentSnapshotAINode node)
        {
            string type = node.nodeType ?? string.Empty;
            if (type.EndsWith("ReadAIMemoryNode", StringComparison.Ordinal) || type.EndsWith("WriteAIMemoryNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIMemoryNode;
            if (type.EndsWith("WriteContinuousInputNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIContinuousInput;
            if (type.EndsWith("WriteActionTargetSnapshotNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIActionTarget;
            if (type.EndsWith("SubmitActionRequestNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIActionRequest;
            if (type.EndsWith("ReadSelfObservationNode", StringComparison.Ordinal) ||
                type.EndsWith("EnumerateConfiguredCandidatesNode", StringComparison.Ordinal) ||
                type.EndsWith("SelectNearestCandidateNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadTargetDistanceNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadTargetDirectionNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadSelectedTargetSnapshotNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIObservationNode;
            if (type.EndsWith("SequenceNode", StringComparison.Ordinal) ||
                type.EndsWith("SelectorNode", StringComparison.Ordinal) ||
                type.EndsWith("LoopNode", StringComparison.Ordinal) ||
                type.EndsWith("CompareNode", StringComparison.Ordinal) ||
                type.EndsWith("AIWaitTicksNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAISharedNode;
            return null;
        }

        static string ResolveAINodeKind(AgentSnapshotAINode node)
        {
            string type = node.nodeType ?? string.Empty;
            if (type.EndsWith("ReadAIMemoryNode", StringComparison.Ordinal)) return AgentAIMemoryNodeKind.Read.ToString();
            if (type.EndsWith("WriteAIMemoryNode", StringComparison.Ordinal)) return AgentAIMemoryNodeKind.Write.ToString();
            if (type.EndsWith("ReadSelfObservationNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadSelf.ToString();
            if (type.EndsWith("EnumerateConfiguredCandidatesNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.EnumerateConfiguredCandidates.ToString();
            if (type.EndsWith("SelectNearestCandidateNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.SelectNearestCandidate.ToString();
            if (type.EndsWith("ReadTargetDistanceNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadTargetDistance.ToString();
            if (type.EndsWith("ReadTargetDirectionNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadTargetDirection.ToString();
            if (type.EndsWith("ReadSelectedTargetSnapshotNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadSelectedTargetSnapshot.ToString();
            if (type.EndsWith("AIWaitTicksNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.WaitTicks.ToString();
            if (type.EndsWith("LoopNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Loop.ToString();
            if (type.EndsWith("SequenceNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Sequence.ToString();
            if (type.EndsWith("SelectorNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Selector.ToString();
            if (type.EndsWith("CompareNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Compare.ToString();
            return string.Empty;
        }

        static bool TryFindGraphOwner(
            IReadOnlyList<AgentSnapshotGraph> graphs,
            string childGraphIdentity,
            out AgentSnapshotGraph parent,
            out AgentSnapshotNode owner)
        {
            parent = null;
            owner = null;
            AgentSnapshotGraph child = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, childGraphIdentity, StringComparison.Ordinal));
            if (child == null || string.IsNullOrEmpty(child.ownerElementAuthoringId))
                return false;
            foreach (AgentSnapshotGraph candidate in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                AgentSnapshotNode node = candidate.nodes?.FirstOrDefault(value =>
                    string.Equals(value.elementAuthoringId, child.ownerElementAuthoringId, StringComparison.Ordinal));
                if (node == null)
                    continue;
                parent = candidate;
                owner = node;
                return true;
            }
            return false;
        }

        static Vector2 ToVector(AgentSnapshotVector2 value)
        {
            return value == null ? Vector2.zero : new Vector2(value.x, value.y);
        }

        static void SetMotionConfiguration(
            AgentMutationDraft operation,
            AgentSnapshotNode node)
        {
            operation.moveSpeed = node.moveSpeed;
            operation.turnSpeedDegrees = node.turnSpeedDegrees;
            operation.cameraRelative = node.cameraRelative;
            operation.continuous = node.continuous;
        }

        static Vector2 FindNodePosition(IReadOnlyList<AgentSnapshotGraph> graphs, string identity)
        {
            AgentSnapshotNode node = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .FirstOrDefault(value => string.Equals(value.elementAuthoringId, identity, StringComparison.Ordinal));
            return ToVector(node?.position);
        }

        static void SetDefaultValue(AgentMutationDraft operation, string type, string value)
        {
            if (string.Equals(type, typeof(bool).FullName, StringComparison.Ordinal) && bool.TryParse(value, out bool boolValue))
                operation.blackboardBoolValue = boolValue;
            else if (string.Equals(type, typeof(int).FullName, StringComparison.Ordinal) && int.TryParse(value, out int intValue))
                operation.blackboardIntValue = intValue;
            else if (string.Equals(type, typeof(float).FullName, StringComparison.Ordinal) && float.TryParse(value, out float floatValue))
                operation.blackboardFloatValue = floatValue;
        }

        static void Add(AgentMutationDraftSet mutations, string path, AgentMutationKind kind, Action<AgentMutationDraft> configure)
        {
            var operation = new AgentMutationDraft
            {
                id = "mutation-" + mutations.mutations.Count.ToString("D4"),
                sourcePath = path,
                kind = kind
            };
            configure(operation);
            mutations.mutations.Add(operation);
        }

        static void AddExposedPropertyMutation(
            AgentMutationDraftSet mutations,
            string graphPath,
            AgentSnapshotGraph graph,
            AgentSnapshotNode node,
            AgentCompileReport report)
        {
            AgentSnapshotExposedProperty exposed = node.exposedProperty;
            string path = $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}].exposedProperty";
            if (!string.Equals(exposed.mode, ExposedPropertyNodeType.Set.ToString(), StringComparison.Ordinal) ||
                !string.Equals(exposed.valueType, typeof(bool).FullName, StringComparison.Ordinal) ||
                exposed.value?.Type != Newtonsoft.Json.Linq.JTokenType.Boolean)
            {
                report.Error(
                    path,
                    "exposed_property_mutation_unsupported",
                    "当前正式Mutation只允许修改Bool Set；Get与其它ValueType可完整读取，但不能作为目标修改。");
                return;
            }
            Add(mutations, $"{graphPath}.nodes[{Escape(node.elementAuthoringId)}]", AgentMutationKind.EnsureBlackboardWrite, operation =>
            {
                if (IsLocal(node.elementAuthoringId))
                    operation.id = LocalIdentity(node.elementAuthoringId);
                SetTargetGraph(operation, graph);
                SetOptionalExisting(operation, node.elementAuthoringId, false);
                if (IsLocal(exposed.declarationAuthoringId))
                    operation.declarationPlannedIdentity = LocalIdentity(exposed.declarationAuthoringId);
                else
                    operation.declarationAuthoringId = exposed.declarationAuthoringId;
                operation.blackboardBoolValue = exposed.value.ToObject<bool>();
                operation.displayName = node.displayName;
                operation.position = ToVector(node.position);
            });
        }

        static void SetDeclarationReference(AgentMutationDraft operation, string identity, bool targetSnapshot)
        {
            if (targetSnapshot)
            {
                SetTargetSnapshotDeclarationReference(operation, identity);
                return;
            }
            if (IsLocal(identity))
                operation.declarationPlannedIdentity = LocalIdentity(identity);
            else
                operation.declarationAuthoringId = identity;
        }

        static void SetTargetSnapshotDeclarationReference(AgentMutationDraft operation, string identity)
        {
            if (string.IsNullOrEmpty(identity))
                return;
            if (IsLocal(identity))
                operation.targetSnapshotBlackboardDeclarationPlannedIdentity = LocalIdentity(identity);
            else
                operation.targetSnapshotBlackboardDeclarationId = identity;
        }

        static void SetGraph(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.graphPlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            SetGraph(operation, graph?.graphAuthoringId);
        }

        static void SetTargetGraph(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.targetGraphPlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            if (IsLocal(graph?.graphAuthoringId))
                operation.targetGraphPlannedIdentity = LocalIdentity(graph.graphAuthoringId);
            else
                operation.targetGraphAuthoringId = graph?.graphAuthoringId;
        }

        static void SetGraph(AgentMutationDraft operation, string identity)
        {
            if (IsLocal(identity))
                operation.graphPlannedIdentity = LocalIdentity(identity);
            else
                operation.graphAuthoringId = identity;
        }

        static void SetStateMachine(AgentMutationDraft operation, string identity)
        {
            if (IsLocal(identity))
                operation.stateMachinePlannedIdentity = LocalIdentity(identity);
            else
                operation.stateMachineGraphAuthoringId = identity;
        }

        static void SetStateMachine(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.stateMachinePlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            SetStateMachine(operation, graph?.graphAuthoringId);
        }

        static void SetElement(AgentMutationDraft operation, string identity, bool source)
        {
            if (source)
            {
                if (IsLocal(identity))
                    operation.fromPlannedIdentity = LocalIdentity(identity);
                else
                    operation.fromElementAuthoringId = identity;
            }
            else
            {
                if (IsLocal(identity))
                    operation.toPlannedIdentity = LocalIdentity(identity);
                else
                    operation.toElementAuthoringId = identity;
            }
        }

        static void SetLinkElement(AgentMutationDraft operation, string identity, bool source)
        {
            if (source)
            {
                if (IsLocal(identity))
                    operation.sourcePlannedIdentity = LocalIdentity(identity);
                else
                    operation.sourceElementAuthoringId = identity;
            }
            else
            {
                if (IsLocal(identity))
                    operation.targetPlannedIdentity = LocalIdentity(identity);
                else
                    operation.targetElementAuthoringId = identity;
            }
        }

        static void SetOptionalExisting(AgentMutationDraft operation, string identity, bool declaration)
        {
            if (IsLocal(identity))
                return;
            if (declaration)
                operation.declarationAuthoringId = identity;
            else
                operation.targetElementAuthoringId = identity;
        }

        static string LocalIdentity(string identity)
        {
            return identity;
        }

        static bool IsLocal(string identity)
        {
            return !string.IsNullOrEmpty(identity) && identity.StartsWith("local:", StringComparison.Ordinal);
        }

        static Dictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> identity,
            string path,
            AgentCompileReport report)
            where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            int index = 0;
            foreach (T value in values ?? Array.Empty<T>())
            {
                string key = value == null ? string.Empty : identity(value);
                if (string.IsNullOrWhiteSpace(key))
                    report.Error($"{path}[{index}]", "entity_identity_missing", "Document entity缺少identity。");
                else if (!result.TryAdd(key, value))
                    report.Error($"{path}[{index}]", "entity_identity_duplicate", $"Document entity identity重复：{key}");
                index++;
            }
            return result;
        }

        static bool Same(object left, object right)
        {
            return string.Equals(
                AgentAuthoringDocumentCodec.Hash(left),
                AgentAuthoringDocumentCodec.Hash(right),
                StringComparison.Ordinal);
        }

        static bool SameList(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return (left ?? Array.Empty<string>()).SequenceEqual(right ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        static bool SameOptionalText(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        static bool SameFlowEdge(AgentSnapshotFlowEdge left, AgentSnapshotFlowEdge right)
        {
            return SameOptionalText(left?.elementAuthoringId, right?.elementAuthoringId) &&
                   SameOptionalText(left?.startElementAuthoringId, right?.startElementAuthoringId) &&
                   SameOptionalText(left?.endElementAuthoringId, right?.endElementAuthoringId) &&
                   SameOptionalText(left?.startPort, right?.startPort) &&
                   SameOptionalText(left?.endPort, right?.endPort) &&
                   left?.flowOrder == right?.flowOrder &&
                   left?.transitionPriority == right?.transitionPriority &&
                   SameOptionalText(left?.abortPolicy, right?.abortPolicy) &&
                   SameOptionalText(left?.conditionRuleGraphAuthoringId, right?.conditionRuleGraphAuthoringId);
        }

        static bool SameAnimationTrackSync(
            AgentSnapshotTimelineTrack left,
            AgentSnapshotTimelineTrack right)
        {
            if (!SameOptionalText(left?.syncMode, right?.syncMode))
                return false;
            if (!string.Equals(
                    right?.syncMode,
                    AnimationSyncMode.MarkerGroup.ToString(),
                    StringComparison.Ordinal))
            {
                return true;
            }
            return SameOptionalText(left.syncGroupId, right.syncGroupId) &&
                   SameOptionalText(
                       left.sequenceTopology,
                       right.sequenceTopology) &&
                   SameOptionalText(left.syncRole, right.syncRole);
        }

        static string Escape(string identity)
        {
            return "'" + (identity ?? string.Empty).Replace("'", "\\'") + "'";
        }
    }
}
